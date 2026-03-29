#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameObjects.Commands;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager;

// 🔥 CommandBuffer 调度器：接管执行阶段调度层，复用旧执行原语
// 日期：2026-03-24（性能优化版）
public class CommandBufferScheduler
{
    private readonly CommandBuffer _commandBuffer = new();
    private readonly ExecutionFrame _executionFrame = new(1024);
    private readonly DeterministicArbitrator _arbitrator = new(512);
    private readonly List<ExecutionCommand> _executionProposals = new(1024);
    private readonly List<ExecutionCommand> _executionBatch = new(256);
    private readonly List<ExecutionCommand> _arbitrationAccepted = new(256);
    private readonly List<ExecutionCommand> _arbitrationRejected = new(256);
    private readonly Dictionary<Guid, ActiveExecutionAudit> _activeExecutionAuditByTroop = new(512);
    private readonly Dictionary<int, int> _normalizedSiegeTargetByLegionId = new(128);
    private readonly List<FrameAuditRecord> _frameAuditRecords = new(1024);

    private const int PriorityTierEnter = 5000;
    private const int PriorityTierAttackTroop = 4300;
    private const int PriorityTierAttackArchitecture = 4200;
    private const int PriorityTierStratagem = 4100;
    private const int PriorityTierMove = 3500;
    
    // 🔥 性能优化：使用预分配数组代替 Queue（避免堆分配）
    private Troop[] _activeQueue = new Troop[256];
    private int _queueHead;
    private int _queueTail;
    private int _queueCount;
    
    private Troop _currentTroop;
    private int _safetyCounter;
    private bool _queueEndedPassCompleted;
    private int _buildIssuedTick;
    private int _executionCursor;
    private bool _executionFrameModeActive;
    
    // 本回合 CommandBuffer 是否可用（失败时必须整回合回退旧调度器）
    public bool HasValidBuffer { get; private set; }
    public IReadOnlyList<FrameAuditRecord> FrameAuditRecords => _frameAuditRecords;
    
    public int ProcessedMoveCommands { get; private set; }
    public int ProcessedEnterCommands { get; private set; }
    public int ProcessedAttackTroopCommands { get; private set; }
    public int ProcessedAttackArchCommands { get; private set; }
    public int ProcessedStratagemCommands { get; private set; }
    
    // 🔥 新增：细分攻击类型统计
    public int ProcessedDirectAttacks { get; private set; }
    public int ProcessedCombatMethodAttacks { get; private set; }
    public int ProcessedStratagemAttacks { get; private set; }
    public int ArbitrationRejectedCommands { get; private set; }

    private readonly record struct ActiveExecutionAudit(
        ExecutionCommand Command,
        Point SourcePosition,
        Point ProjectedDestination,
        int InitialMovabilityLeft
    );

    private enum ArbitrationConflictKind : byte
    {
        None = 0,
        Cell = 1,
        Edge = 2
    }
    
    public bool BuildCommandBuffer(GameScenario scenario)
    {
        _commandBuffer.Clear();
        _queueHead = 0;
        _queueTail = 0;
        _queueCount = 0;
        _currentTroop = null;
        _safetyCounter = 0;
        _queueEndedPassCompleted = false;
        _buildIssuedTick = scenario.DaySince;
        _executionCursor = 0;
        _executionFrameModeActive = false;
        HasValidBuffer = false;
        _executionFrame.Begin(_buildIssuedTick);
        _executionProposals.Clear();
        _executionBatch.Clear();
        _arbitrationAccepted.Clear();
        _arbitrationRejected.Clear();
        _activeExecutionAuditByTroop.Clear();
        _normalizedSiegeTargetByLegionId.Clear();
        _frameAuditRecords.Clear();
        
        ProcessedMoveCommands = 0;
        ProcessedEnterCommands = 0;
        ProcessedAttackTroopCommands = 0;
        ProcessedAttackArchCommands = 0;
        ProcessedStratagemCommands = 0;
        ProcessedDirectAttacks = 0;
        ProcessedCombatMethodAttacks = 0;
        ProcessedStratagemAttacks = 0;
        ArbitrationRejectedCommands = 0;
        
        int totalTroops = 0;
        int validCommands = 0;
        int playerTroops = 0;
        int aiTroops = 0;
        
        try
        {
            for (int i = 0; i < scenario.Troops.Count; i++)
            {
                GameObject obj = scenario.Troops[i];
                if (obj is not Troop troop) continue;
                if (troop.Destroyed || !troop.CanMoveAnyway()) continue;
                
                totalTroops++;
                if (troop.ManualControl) playerTroops++; else aiTroops++;
                
                switch (troop.Command)
                {
                    case TroopCommand.Move:
                        GenerateMoveCommand(troop);
                        validCommands++;
                        break;
                        
                    case TroopCommand.Enter:
                        GenerateEnterCommand(troop);
                        validCommands++;
                        break;
                        
                    case TroopCommand.AttackTroop:
                        GenerateAttackTroopCommand(troop);
                        validCommands++;
                        break;
                        
                    case TroopCommand.AttackArch:
                        GenerateAttackArchCommand(troop);
                        validCommands++;
                        break;
                    
                    // 兼容旧指令：Attack 仍然可能来自 UI / 存档 / mingling 同步
                    case TroopCommand.Attack:
                        GenerateLegacyAttackCommand(troop);
                        validCommands++;
                        break;
                        
                    case TroopCommand.Stratagem:
                        GenerateStratagemCommand(troop);
                        validCommands++;
                        break;
                }
            }
            
            validCommands = _commandBuffer.TotalCommandCount;
            _executionProposals.Sort(static (left, right) =>
            {
                int commitCompare = left.CommitTick.CompareTo(right.CommitTick);
                if (commitCompare != 0) return commitCompare;

                int priorityCompare = right.Priority.CompareTo(left.Priority);
                if (priorityCompare != 0) return priorityCompare;

                return left.TroopId.CompareTo(right.TroopId);
            });
            _executionCursor = 0;
            _executionFrameModeActive = _executionProposals.Count > 0;
            HasValidBuffer = true;
            System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] Build Summary: proposals={_executionProposals.Count}, mode={(_executionFrameModeActive ? "ExecutionFrame" : "LegacyQueue")}");
            System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] 构建完成: 总部队={totalTroops}(玩家={playerTroops}, AI={aiTroops}), 有效指令={validCommands}, 总指令={_commandBuffer.TotalCommandCount}");
            return true;
        }
        catch (Exception ex)
        {
            // 关键：构建失败时必须清空半成品，确保整回合回退旧调度器
            _commandBuffer.Clear();
            _queueHead = 0;
            _queueTail = 0;
            _queueCount = 0;
            _currentTroop = null;
            _queueEndedPassCompleted = false;
            _executionCursor = 0;
            _executionFrameModeActive = false;
            HasValidBuffer = false;
            _executionFrame.Clear();
            _executionProposals.Clear();
            _executionBatch.Clear();
            _arbitrationAccepted.Clear();
            _arbitrationRejected.Clear();
            _activeExecutionAuditByTroop.Clear();
            _normalizedSiegeTargetByLegionId.Clear();
            _frameAuditRecords.Clear();
            
            System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] ❌ 构建失败，已清空缓冲并回退旧调度器: {ex.Message}");
            return false;
        }
    }
    
    public bool UpdateFrame(GameTime gameTime, GameScenario scenario)
    {
        if (++_safetyCounter > 10000)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ CommandBufferScheduler: 死循环检测触发，强制退出");
            return false;
        }
        
        if (_currentTroop != null)
        {
            if (!ProcessCurrentTroop(gameTime, scenario))
            {
                return true;
            }
        }
        
        while (_currentTroop == null)
        {
            // 先处理伏兵（对齐旧队列：每帧优先检查 AmbushList）
            if (TryProcessAmbushList(scenario.Troops))
            {
                return true;
            }
            
            if (_queueCount == 0)
            {
                if (RefillQueue())
                {
                    // 命令缓冲已补充，继续取活动部队
                }
                else if (!_queueEndedPassCompleted && RefillQueueFromQueueEnded(scenario))
                {
                    _queueEndedPassCompleted = true;
                    // QueueEnded 二次回填成功，继续处理
                }
                else
                {
                    _queueEndedPassCompleted = true;
                    
                    // 🔥 关键修复：回合结束前清理全体部队的瞬时执行态
                    // 日期：2026-03-24
                    // 原因：新调度器只处理有命令的部队，受击/受术目标部队通常没有命令
                    //       导致它们的 BeAttacked/BeCasted 动作残留，泄漏到战略阶段
                    // 解决：在 DateStop() 前统一清理所有部队的瞬时执行态
                    ClearAllTroopsTransientState(scenario);
                    
                    System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] 回合结束统计:");
                    System.Diagnostics.Debug.WriteLine($"  - 移动={ProcessedMoveCommands}, 入城={ProcessedEnterCommands}");
                    System.Diagnostics.Debug.WriteLine($"  - 攻击部队={ProcessedAttackTroopCommands}(直接={ProcessedDirectAttacks}, 战法={ProcessedCombatMethodAttacks}, 计略={ProcessedStratagemAttacks})");
                    System.Diagnostics.Debug.WriteLine($"  - 攻击城池={ProcessedAttackArchCommands}, 计略={ProcessedStratagemCommands}");
                    _activeExecutionAuditByTroop.Clear();
                    _normalizedSiegeTargetByLegionId.Clear();
                    return false;
                }
            }
            
            if (_queueCount == 0)
            {
                continue;
            }
            
            _currentTroop = DequeueTroop();
            
            if (_currentTroop.Destroyed)
            {
                FinalizeExecutionAuditForCurrentTroop(
                    scenario,
                    FrameAuditResultCode.DestroyedBeforeCompletion,
                    verifySpatialConsistency: false);
                _currentTroop = null;
                continue;
            }

            if (Session.GlobalVariables != null && Session.GlobalVariables.EnableAIAuthorityPhase1)
            {
                AIAuthorityContext authorityContext = scenario.EnsureAIAuthorityContext();
                if (!authorityContext.ValidateAndHandleIntentCheckpoint(scenario, _currentTroop, IntentCheckpointKind.QueuePickup))
                {
                    _currentTroop.OperationDone = true;
                    FinalizeExecutionAuditForCurrentTroop(
                        scenario,
                        FrameAuditResultCode.AbortedByQueue,
                        verifySpatialConsistency: false);
                    _currentTroop = null;
                    continue;
                }
            }
            
            QueueAction action = TroopStateMachineRouter.DetermineQueueAction(_currentTroop);
            if (!HandleQueueAction(action, gameTime))
            {
                FrameAuditResultCode queueResult = action is QueueAction.ForceCombatCheck or QueueAction.ExecuteStratagemDirectly
                    ? FrameAuditResultCode.NonSpatialCompleted
                    : FrameAuditResultCode.AbortedByQueue;
                FinalizeExecutionAuditForCurrentTroop(
                    scenario,
                    queueResult,
                    verifySpatialConsistency: false);
                _currentTroop = null;
                continue;
            }
            
            break;
        }
        
        return true;
    }
    
    private bool TryProcessAmbushList(TroopListWithQueue troopList)
    {
        Troop finishedAmbushTroop = null;
        
        foreach (Troop troop in troopList.AmbushList)
        {
            if (troop.ToDoCombatAction())
            {
                troop.DoCombatAction();
                if (troop.OperationDone)
                {
                    finishedAmbushTroop = troop;
                    break;
                }
            }
        }
        
        troopList.AmbushList.Remove(finishedAmbushTroop);
        return finishedAmbushTroop != null;
    }
    
    private bool RefillQueueFromQueueEnded(GameScenario scenario)
    {
        TroopList list = new();
        
        for (int i = 0; i < scenario.Troops.Count; i++)
        {
            if (scenario.Troops[i] is Troop troop && troop.QueueEnded)
            {
                list.Add(troop);
            }
        }
        
        foreach (Troop troop in list.GetRandomList())
        {
            EnqueueTroop(troop);
        }
        
        return _queueCount > 0;
    }
    
    // 🔥 性能优化：数组队列操作（Zero-Allocation）
    private void EnqueueTroop(Troop troop)
    {
        if (_queueCount == _activeQueue.Length)
        {
            Array.Resize(ref _activeQueue, _activeQueue.Length * 2);
        }
        
        _activeQueue[_queueTail] = troop;
        _queueTail = (_queueTail + 1) % _activeQueue.Length;
        _queueCount++;
    }
    
    private Troop DequeueTroop()
    {
        Troop troop = _activeQueue[_queueHead];
        _activeQueue[_queueHead] = null;
        _queueHead = (_queueHead + 1) % _activeQueue.Length;
        _queueCount--;
        return troop;
    }
    
    private bool ProcessCurrentTroop(GameTime gameTime, GameScenario scenario)
    {
        if (_currentTroop.Destroyed)
        {
            FinalizeExecutionAuditForCurrentTroop(
                scenario,
                FrameAuditResultCode.DestroyedBeforeCompletion,
                verifySpatialConsistency: false);
            _currentTroop = null;
            return true;
        }
        
        if (_currentTroop.IsAnimationPlaying)
        {
            bool isBlockedByFriendly = _currentTroop.Action == TroopAction.Stop && 
                                       _currentTroop.HasCachedPath &&
                                       !_currentTroop.IsPathfinding;
            
            if (isBlockedByFriendly)
            {
                EnqueueTroop(_currentTroop);
                _currentTroop = null;
                return true;
            }
            else
            {
                _currentTroop.UpdateMovementLogic(gameTime);
                return false;
            }
        }
        
        bool canExecuteCombat = _currentTroop.ToDoCombatAction();
        
        if (canExecuteCombat)
        {
            if (!_currentTroop.HasToDoCombatAction)
            {
                _currentTroop.HasToDoCombatAction = true;
                _currentTroop.DoCombatAction();
            }
            
            _currentTroop.OperationDone = true;
            FinalizeExecutionAuditForCurrentTroop(
                scenario,
                FrameAuditResultCode.NonSpatialCompleted,
                verifySpatialConsistency: false);
            _currentTroop = null;
            return true;
        }
        else
        {
            _currentTroop.UpdateMovementLogic(gameTime);
            
            bool isActuallyMoving = _currentTroop.IsAnimationPlaying || 
                                   _currentTroop.IsPathfinding ||
                                   (_currentTroop.HasCachedPath && _currentTroop.MovabilityLeft > 0);
            
            if (!isActuallyMoving)
            {
                _currentTroop.OperationDone = true;
                FinalizeExecutionAuditForCurrentTroop(
                    scenario,
                    FrameAuditResultCode.Success,
                    verifySpatialConsistency: true);
                _currentTroop = null;
                return true;
            }
            
            return false;
        }
    }
    
    private bool HandleQueueAction(QueueAction action, GameTime gameTime)
    {
        switch (action)
        {
            case QueueAction.SkipTurn:
                _currentTroop.MovabilityLeft = -1;
                _currentTroop.OperationDone = true;
                return false;
                
            case QueueAction.EndTroop:
                return false;
                
            case QueueAction.ForceCombatCheck:
                if (!_currentTroop.HasToDoCombatAction && _currentTroop.ToDoCombatAction())
                {
                    _currentTroop.HasToDoCombatAction = true;
                    _currentTroop.DoCombatAction();
                }
                return false;
                
            case QueueAction.ExecuteStratagemDirectly:
                if (!_currentTroop.HasToDoCombatAction && _currentTroop.ToDoCombatAction())
                {
                    _currentTroop.HasToDoCombatAction = true;
                    _currentTroop.DoCombatAction();
                }
                return false;
                
            case QueueAction.EnterMovementPipeline:
                EnterMovementPipeline(gameTime);
                return true;
                
            default:
                return false;
        }
    }
    
    private void EnterMovementPipeline(GameTime gameTime)
    {
        GameScenario scenario = Session.Current?.Scenario;
        AIAuthorityContext authorityContext = null;
        if (scenario != null &&
            Session.GlobalVariables != null &&
            Session.GlobalVariables.EnableAIAuthorityPhase1)
        {
            authorityContext = scenario.EnsureAIAuthorityContext();
            if (!authorityContext.ValidateAndHandleIntentCheckpoint(scenario, _currentTroop, IntentCheckpointKind.BeforeProjection))
            {
                _currentTroop.OperationDone = true;
                FinalizeExecutionAuditForCurrentTroop(
                    scenario,
                    FrameAuditResultCode.AbortedByQueue,
                    verifySpatialConsistency: false);
                return;
            }
        }

        TroopListWithQueue troopList = Session.Current.Scenario.Troops;
        troopList.TroopChangeRealDestination(_currentTroop, skipAuthorityProjection: true);

        bool projectedByAuthority = authorityContext != null && authorityContext.ApplyIntentProjection(scenario, _currentTroop);
        if (!projectedByAuthority)
        {
            if (_currentTroop.Command == TroopCommand.Enter)
            {
                _currentTroop.CurrentAIState = TroopAIState.EnterCity;
            }
            else if (_currentTroop.Command != TroopCommand.None)
            {
                _currentTroop.CurrentAIState = TroopAIState.Marching;
            }
        }
        
        _currentTroop.UpdateMovementLogic(gameTime);
        
        bool shouldAttackImmediately = CheckImmediateAttack();
        
        if (shouldAttackImmediately)
        {
            if (!_currentTroop.HasToDoCombatAction && _currentTroop.ToDoCombatAction())
            {
                _currentTroop.HasToDoCombatAction = true;
                _currentTroop.DoCombatAction();
            }
            
            _currentTroop.OperationDone = true;
        }
    }
    
    private bool CheckImmediateAttack()
    {
        if (_currentTroop.Command == TroopCommand.AttackArch)
        {
            var targetArchitecture = _currentTroop.TargetArchitecture;
            if (targetArchitecture == null || targetArchitecture.Endurance <= 0)
            {
                _currentTroop.SetCommand(TroopCommand.None);
                _currentTroop.SelectedAttack = false;
                return false;
            }

            int distToCity = int.MaxValue;
            GameArea cityArea = targetArchitecture.ArchitectureArea;
            
            for (int i = 0; i < cityArea.Area.Count; i++)
            {
                Point cityTile = cityArea.Area[i];
                int dx = Math.Abs(_currentTroop.Position.X - cityTile.X);
                int dy = Math.Abs(_currentTroop.Position.Y - cityTile.Y);
                int d = Math.Max(dx, dy);
                if (d < distToCity) distToCity = d;
            }
            
            return distToCity <= _currentTroop.OffenceRadius;
        }
        else if (_currentTroop.Command == TroopCommand.AttackTroop)
        {
            Troop targetTroop = _currentTroop.TargetTroop;
            if (targetTroop == null || targetTroop.Destroyed)
            {
                _currentTroop.SetCommand(TroopCommand.None);
                _currentTroop.SelectedAttack = false;
                return false;
            }

            int dx = Math.Abs(_currentTroop.Position.X - targetTroop.Position.X);
            int dy = Math.Abs(_currentTroop.Position.Y - targetTroop.Position.Y);
            int distToTroop = Math.Max(dx, dy);
            
            bool inRange = (distToTroop <= _currentTroop.OffenceRadius);
            
            if (inRange)
            {
                bool needsPositionAdjustment = (_currentTroop.RealDestination != _currentTroop.Position);
                
                if (needsPositionAdjustment)
                {
                    float currentPosScore = AITacticalPositioner.EvaluateRangedPosition(
                        _currentTroop, _currentTroop.Position, targetTroop);
                    float targetPosScore = AITacticalPositioner.EvaluateRangedPosition(
                        _currentTroop, _currentTroop.RealDestination, targetTroop);
                    
                    return targetPosScore <= currentPosScore + 50.0f;
                }
                
                return true;
            }
        }
        
        return false;
    }

    private bool RefillQueue()
    {
        if (_executionFrameModeActive)
        {
            try
            {
                return RefillQueueFromExecutionFrame();
            }
            catch (Exception ex)
            {
                _executionFrameModeActive = false;
                System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] ExecutionFrame refill failed, fallback to legacy queues: {ex.Message}");
            }
        }

        return RefillQueueLegacy();
    }

    private bool RefillQueueFromExecutionFrame()
    {
        if (_executionCursor >= _executionProposals.Count)
        {
            return false;
        }

        _executionBatch.Clear();
        _arbitrationAccepted.Clear();
        _arbitrationRejected.Clear();

        int commitTick = _executionProposals[_executionCursor].CommitTick;
        while (_executionCursor < _executionProposals.Count)
        {
            ExecutionCommand proposal = _executionProposals[_executionCursor];
            if (proposal.CommitTick != commitTick) break;

            _executionBatch.Add(proposal);
            _executionCursor++;
        }

        _arbitrator.ResolveBatch(_executionBatch, _arbitrationAccepted, _arbitrationRejected);
        EnqueueArbitrationWinners();
        ProcessArbitrationRejected();

        return _queueCount > 0 || _executionCursor < _executionProposals.Count;
    }

    private void EnqueueArbitrationWinners()
    {
        for (int i = 0; i < _arbitrationAccepted.Count; i++)
        {
            ExecutionCommand command = _arbitrationAccepted[i];
            Troop troop = Session.Current.GetTroopByGuid(command.TroopId);
            if (troop == null)
                throw new InvalidOperationException($"ExecutionCommand references missing troop: GUID={command.TroopId}");

            if (troop.Destroyed)
            {
                continue;
            }

            BeginExecutionAudit(troop, command);
            EnqueueTroop(troop);
            TrackProcessedCommand(troop, command.ActionKind);
        }
    }

    private void ProcessArbitrationRejected()
    {
        if (_arbitrationRejected.Count == 0)
        {
            return;
        }

        ArbitrationRejectedCommands += _arbitrationRejected.Count;

        GameScenario scenario = Session.Current?.Scenario;
        if (scenario == null)
        {
            return;
        }

        bool authorityEnabled = Session.GlobalVariables != null && Session.GlobalVariables.EnableAIAuthorityPhase1;
        AIAuthorityContext authorityContext = authorityEnabled ? scenario.EnsureAIAuthorityContext() : null;

        for (int i = 0; i < _arbitrationRejected.Count; i++)
        {
            ExecutionCommand rejected = _arbitrationRejected[i];
            Troop troop = Session.Current.GetTroopByGuid(rejected.TroopId);
            if (troop == null || troop.Destroyed)
            {
                continue;
            }

            if (authorityContext != null)
            {
                Point failedAt = rejected.ResolveSpatialPosition();
                ArbitrationConflictKind conflictKind = ResolveArbitrationConflictKind(in rejected, out int relatedObjectId);
                authorityContext.HandleExecutorFailure(
                    scenario,
                    troop,
                    TroopIntentFailureReason.ArbitrationLost,
                    failedAt.X >= 0 && failedAt.Y >= 0 ? failedAt : troop.Position,
                    relatedObjectId,
                    IntentCheckpointKind.AfterBlocked,
                    shouldReplanNow: false);

                AppendFrameAuditRecord(
                    troop,
                    rejected,
                    troop.Position,
                    MapArbitrationResultCode(conflictKind),
                    (int)TroopIntentFailureReason.ArbitrationLost,
                    relatedObjectId);
            }
            else
            {
                troop.CurrentAIState = TroopAIState.Waiting;
                troop.SetCommand(TroopCommand.None);

                AppendFrameAuditRecord(
                    troop,
                    rejected,
                    troop.Position,
                    MapArbitrationResultCode(ResolveArbitrationConflictKind(in rejected, out _)),
                    -1,
                    -1);
            }

            troop.OperationDone = true;
        }
    }

    private ArbitrationConflictKind ResolveArbitrationConflictKind(
        in ExecutionCommand rejected,
        out int relatedObjectId)
    {
        relatedObjectId = -1;

        if (!rejected.IsSpatialConflictAction)
        {
            return ArbitrationConflictKind.None;
        }

        Point rejectedPosition = rejected.ResolveSpatialPosition();
        if (rejectedPosition.X < 0 || rejectedPosition.Y < 0)
        {
            return ArbitrationConflictKind.None;
        }

        int key = rejected.ResolveCellKey();
        for (int i = 0; i < _arbitrationAccepted.Count; i++)
        {
            ExecutionCommand winner = _arbitrationAccepted[i];
            if (!winner.IsSpatialConflictAction) continue;

            Point winnerPosition = winner.ResolveSpatialPosition();
            if (winnerPosition.X < 0 || winnerPosition.Y < 0) continue;
            if (winner.ResolveCellKey() == key)
            {
                Troop winnerTroop = Session.Current.GetTroopByGuid(winner.TroopId);
                relatedObjectId = winnerTroop?.ID ?? -1;
                return ArbitrationConflictKind.Cell;
            }

            if (!HasReverseEdgeConflict(in rejected, in winner)) continue;

            Troop edgeWinnerTroop = Session.Current.GetTroopByGuid(winner.TroopId);
            relatedObjectId = edgeWinnerTroop?.ID ?? -1;
            return ArbitrationConflictKind.Edge;
        }

        return ArbitrationConflictKind.None;
    }

    private static bool HasReverseEdgeConflict(in ExecutionCommand rejected, in ExecutionCommand winner)
    {
        return rejected.IsReverseEdgeOf(in winner);
    }

    private static FrameAuditResultCode MapArbitrationResultCode(ArbitrationConflictKind conflictKind)
    {
        return conflictKind switch
        {
            ArbitrationConflictKind.Edge => FrameAuditResultCode.RejectedByEdgeConflict,
            _ => FrameAuditResultCode.RejectedByCellConflict
        };
    }

    private void BeginExecutionAudit(Troop troop, in ExecutionCommand command)
    {
        Point projectedDestination = troop.RealDestination;
        _activeExecutionAuditByTroop[troop.Id] = new ActiveExecutionAudit(
            command,
            troop.Position,
            projectedDestination,
            troop.MovabilityLeft);
    }

    private void FinalizeExecutionAuditForCurrentTroop(
        GameScenario scenario,
        FrameAuditResultCode fallbackResult,
        bool verifySpatialConsistency)
    {
        FinalizeExecutionAudit(scenario, _currentTroop, fallbackResult, verifySpatialConsistency);
    }

    private void FinalizeExecutionAudit(
        GameScenario scenario,
        Troop troop,
        FrameAuditResultCode fallbackResult,
        bool verifySpatialConsistency)
    {
        if (troop == null) return;
        if (!_activeExecutionAuditByTroop.TryGetValue(troop.Id, out ActiveExecutionAudit activeAudit)) return;

        _activeExecutionAuditByTroop.Remove(troop.Id);

        FrameAuditResultCode resultCode = fallbackResult;
        int failureReasonCode = -1;
        int relatedObjectId = -1;

        if (verifySpatialConsistency && ShouldVerifySpatialConsistency(activeAudit.Command))
        {
            if (HasSpatialProgressMismatch(troop, activeAudit))
            {
                bool failureForwarded = false;
                if (scenario != null &&
                    Session.GlobalVariables != null &&
                    Session.GlobalVariables.EnableAIAuthorityPhase1)
                {
                    AIAuthorityContext authorityContext = scenario.EnsureAIAuthorityContext();
                    failureForwarded = authorityContext.HandleExecutorFailure(
                        scenario,
                        troop,
                        TroopIntentFailureReason.ExecutorConflict,
                        troop.Position,
                        relatedObjectId,
                        IntentCheckpointKind.AfterBlocked,
                        shouldReplanNow: false);
                }

                resultCode = failureForwarded
                    ? FrameAuditResultCode.ExecutorFailureForwarded
                    : FrameAuditResultCode.MismatchWithoutAuthority;
                failureReasonCode = (int)TroopIntentFailureReason.ExecutorConflict;
            }
            else
            {
                resultCode = FrameAuditResultCode.Success;
            }
        }

        AppendFrameAuditRecord(
            troop,
            activeAudit.Command,
            troop.Position,
            resultCode,
            failureReasonCode,
            relatedObjectId,
            activeAudit.SourcePosition,
            activeAudit.ProjectedDestination);
    }

    private static bool ShouldVerifySpatialConsistency(in ExecutionCommand command)
    {
        return command.ActionKind is ExecutionActionKind.Move or ExecutionActionKind.Enter;
    }

    private static bool HasSpatialProgressMismatch(Troop troop, in ActiveExecutionAudit activeAudit)
    {
        if (activeAudit.InitialMovabilityLeft <= 0)
        {
            return false;
        }

        Point expected = activeAudit.Command.ResolveSpatialPosition();
        if (expected.X < 0 || expected.Y < 0)
        {
            return false;
        }

        if (expected == activeAudit.SourcePosition)
        {
            return false;
        }

        if (troop.Position != activeAudit.SourcePosition)
        {
            return false;
        }

        return true;
    }

    private void AppendFrameAuditRecord(
        Troop troop,
        in ExecutionCommand command,
        Point finalPosition,
        FrameAuditResultCode resultCode,
        int failureReasonCode,
        int relatedObjectId,
        Point? sourcePositionOverride = null,
        Point? projectedDestinationOverride = null)
    {
        if (troop == null)
        {
            return;
        }

        Point sourcePosition = sourcePositionOverride ?? command.SourcePosition;
        Point projectedDestination = projectedDestinationOverride ?? command.TargetPosition;
        FrameAuditRecord record = new(
            troop.ID,
            command.ActionKind,
            command.IssuedTick,
            command.CommitTick,
            sourcePosition,
            command.ResolveSpatialPosition(),
            projectedDestination,
            finalPosition,
            resultCode,
            failureReasonCode,
            relatedObjectId);
        _frameAuditRecords.Add(record);
    }
    
    private bool RefillQueueLegacy()
    {
        if (_commandBuffer.EnterQueue.TryDequeue(out EnterCommand enterCmd))
        {
            Troop troop = Session.Current.GetTroopByGuid(enterCmd.TroopId);
            if (troop == null)
                throw new InvalidOperationException($"数据损坏：EnterCommand 引用了不存在的部队 GUID={enterCmd.TroopId}");
            
            if (!troop.Destroyed)
            {
                EnqueueTroop(troop);
                ProcessedEnterCommands++;
                return true;
            }
        }
        
        if (_commandBuffer.AttackTroopQueue.TryDequeue(out AttackTroopCommand attackTroopCmd))
        {
            Troop troop = Session.Current.GetTroopByGuid(attackTroopCmd.AttackerId);
            if (troop == null)
                throw new InvalidOperationException($"数据损坏：AttackTroopCommand 引用了不存在的部队 GUID={attackTroopCmd.AttackerId}");
            
            if (!troop.Destroyed)
            {
                EnqueueTroop(troop);
                ProcessedAttackTroopCommands++;
                
                // 🔥 细分攻击类型统计
                if (troop.CurrentStratagem != null)
                    ProcessedStratagemAttacks++;
                else if (troop.CurrentCombatMethod != null)
                    ProcessedCombatMethodAttacks++;
                else
                    ProcessedDirectAttacks++;
                
                return true;
            }
        }
        
        if (_commandBuffer.AttackArchQueue.TryDequeue(out AttackArchCommand attackArchCmd))
        {
            Troop troop = Session.Current.GetTroopByGuid(attackArchCmd.AttackerId);
            if (troop == null)
                throw new InvalidOperationException($"数据损坏：AttackArchCommand 引用了不存在的部队 GUID={attackArchCmd.AttackerId}");
            
            if (!troop.Destroyed)
            {
                if (IsValidCommandPosition(attackArchCmd.SiegePosition))
                {
                    troop.RealDestination = attackArchCmd.SiegePosition;
                }

                EnqueueTroop(troop);
                ProcessedAttackArchCommands++;
                return true;
            }
        }
        
        if (_commandBuffer.MoveQueue.TryDequeue(out MoveCommand moveCmd))
        {
            Troop troop = Session.Current.GetTroopByGuid(moveCmd.TroopId);
            if (troop == null)
                throw new InvalidOperationException($"数据损坏：MoveCommand 引用了不存在的部队 GUID={moveCmd.TroopId}");
            
            if (!troop.Destroyed)
            {
                EnqueueTroop(troop);
                ProcessedMoveCommands++;
                return true;
            }
        }
        
        if (_commandBuffer.StratagemQueue.TryDequeue(out StratagemCommand stratagemCmd))
        {
            Troop troop = Session.Current.GetTroopByGuid(stratagemCmd.CasterId);
            if (troop == null)
                throw new InvalidOperationException($"数据损坏：StratagemCommand 引用了不存在的部队 GUID={stratagemCmd.CasterId}");
            
            if (!troop.Destroyed)
            {
                EnqueueTroop(troop);
                ProcessedStratagemCommands++;
                return true;
            }
        }
        
        return false;
    }

    private void TrackProcessedCommand(Troop troop, ExecutionActionKind actionKind)
    {
        switch (actionKind)
        {
            case ExecutionActionKind.Enter:
                ProcessedEnterCommands++;
                break;
            case ExecutionActionKind.Move:
                ProcessedMoveCommands++;
                break;
            case ExecutionActionKind.AttackTroop:
                ProcessedAttackTroopCommands++;
                if (troop.CurrentStratagem != null)
                    ProcessedStratagemAttacks++;
                else if (troop.CurrentCombatMethod != null)
                    ProcessedCombatMethodAttacks++;
                else
                    ProcessedDirectAttacks++;
                break;
            case ExecutionActionKind.AttackArchitecture:
                ProcessedAttackArchCommands++;
                break;
            case ExecutionActionKind.Stratagem:
                ProcessedStratagemCommands++;
                break;
        }
    }

    private void RecordExecutionCommand(
        Troop troop,
        ExecutionActionKind actionKind,
        Point targetPosition,
        Point conflictPosition,
        Guid targetTroopId,
        int targetArchitectureId,
        int priorityTier,
        int riskBudget = 0)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));

        int priority = ResolveExecutionPriority(troop, priorityTier);
        int commitTick = ResolveCommitTick(actionKind, _buildIssuedTick);
        int sourceFactionId = troop.BelongedFaction?.ID ?? -1;
        int sourceLegionId = troop.BelongedLegion?.ID ?? -1;

        ExecutionCommand command = new(
            troop.Id,
            actionKind,
            troop.Position,
            targetPosition,
            conflictPosition,
            targetTroopId,
            targetArchitectureId,
            priority,
            _buildIssuedTick,
            commitTick,
            Math.Max(0, riskBudget),
            sourceFactionId,
            sourceLegionId);

        _executionProposals.Add(command);
        _executionFrame.Add(command);
    }

    private static int ResolveCommitTick(ExecutionActionKind actionKind, int issuedTick)
    {
        return actionKind switch
        {
            ExecutionActionKind.Enter => issuedTick,
            ExecutionActionKind.AttackTroop => issuedTick + 1,
            ExecutionActionKind.AttackArchitecture => issuedTick + 1,
            ExecutionActionKind.Stratagem => issuedTick + 1,
            ExecutionActionKind.Move => issuedTick + 2,
            _ => issuedTick + 2
        };
    }

    private static int ResolveExecutionPriority(Troop troop, int priorityTier)
    {
        int mobilityScore = Math.Clamp(troop.RealMovability, 0, 255);
        int commandScore = troop.Leader?.Command ?? 0;
        int moraleScore = Math.Clamp(troop.Morale / 2, 0, 100);
        return priorityTier + mobilityScore * 4 + commandScore * 2 + moraleScore;
    }

    private void NormalizeAttackTroopExecutionPosition(Troop troop, Troop targetTroop)
    {
        Point optimalPosition = troop.GetOptimalAttackPosition(targetTroop);
        if (IsValidCommandPosition(optimalPosition))
        {
            troop.RealDestination = optimalPosition;
            return;
        }

        if (troop.CanAttack(targetTroop))
        {
            troop.RealDestination = troop.Position;
            return;
        }

        troop.RealDestination = troop.Position;
    }

    private void NormalizeAttackArchitectureExecutionPosition(Troop troop, Architecture targetArchitecture)
    {
        troop.WillArchitecture = targetArchitecture;

        if (troop.CanAttack(targetArchitecture))
        {
            troop.ApplySmartSiegePosition(troop.Position);
            return;
        }

        Legion legion = troop.BelongedLegion;
        if (legion != null &&
            (!_normalizedSiegeTargetByLegionId.TryGetValue(legion.ID, out int normalizedTargetId) ||
             normalizedTargetId != targetArchitecture.ID))
        {
            legion.SetOperationalTarget(targetArchitecture);
            _normalizedSiegeTargetByLegionId[legion.ID] = targetArchitecture.ID;
            legion.AssignSmartSiegePositions();
        }

        if (troop.RealDestination.X >= 0 &&
            troop.RealDestination.Y >= 0 &&
            (troop.RealDestination != Point.Zero ||
             troop.Position == Point.Zero ||
             troop.CanAttack(targetArchitecture)))
        {
            return;
        }

        Point siegePosition = troop.GetSmartSiegePosition(targetArchitecture);
        if (IsValidCommandPosition(siegePosition))
        {
            troop.ApplySmartSiegePosition(siegePosition);
            return;
        }

        if (troop.CanAttack(targetArchitecture))
        {
            troop.ApplySmartSiegePosition(troop.Position);
            return;
        }

        troop.RealDestination = troop.Position;
    }

    private static bool IsValidCommandPosition(Point position)
    {
        return position.X >= 0 && position.Y >= 0;
    }
    
    private void GenerateMoveCommand(Troop troop)
    {
        if (troop.RealDestination.X >= 0 && troop.RealDestination.Y >= 0)
        {
            _commandBuffer.MoveQueue.Enqueue(new MoveCommand(
                troop.Id, 
                troop.RealDestination, 
                MoveCommand.PriorityNormalMove));

            RecordExecutionCommand(
                troop,
                ExecutionActionKind.Move,
                troop.RealDestination,
                troop.ResolveImmediateConflictPosition(troop.RealDestination),
                Guid.Empty,
                -1,
                PriorityTierMove,
                troop.stuckedFor);
        }
    }
    
    private void GenerateEnterCommand(Troop troop)
    {
        if (troop.TargetArchitecture == null)
        {
            troop.SetCommand(TroopCommand.None);
            troop.OperationDone = true;
            return;
        }

        if (troop.TargetArchitecture == null)
            throw new InvalidOperationException($"部队 {troop.ID} 的 Enter 命令缺少目标城池");
        
        _commandBuffer.EnterQueue.Enqueue(new EnterCommand(
            troop.Id,
            troop.TargetArchitecture.ID,
            troop.RealDestination));

        RecordExecutionCommand(
            troop,
            ExecutionActionKind.Enter,
            troop.RealDestination,
            troop.ResolveImmediateConflictPosition(troop.RealDestination),
            Guid.Empty,
            troop.TargetArchitecture.ID,
            PriorityTierEnter);
    }
    
    private void GenerateAttackTroopCommand(Troop troop)
    {
        Troop targetTroop = troop.TargetTroop;
        if (targetTroop == null || targetTroop.Destroyed)
        {
            troop.SetCommand(TroopCommand.None);
            troop.SelectedAttack = false;
            return;
        }

        if (troop.TargetTroop == null)
            throw new InvalidOperationException($"部队 {troop.ID} 的 AttackTroop 命令缺少目标部队");

        // 战法/计略攻击在执行阶段依赖 OrientationTroop，而不是只看 TargetTroop。
        if (troop.CurrentCombatMethod != null || troop.CurrentStratagem != null)
        {
            troop.OrientationTroop = targetTroop;
            troop.OrientationArchitecture = null;
            targetTroop.OrientationTroop = troop;
        }

        // 关键：旧 UI 仍可能留下 Attack 遗留命令，进入 CommandBuffer 前统一归一化。
        troop.SetCommand(TroopCommand.AttackTroop);
        
        NormalizeAttackTroopExecutionPosition(troop, targetTroop);

        _commandBuffer.AttackTroopQueue.Enqueue(new AttackTroopCommand(
            troop.Id,
            troop.TargetTroop.Id,
            troop.RealDestination));

        RecordExecutionCommand(
            troop,
            ExecutionActionKind.AttackTroop,
            troop.RealDestination,
            troop.ResolveImmediateConflictPosition(troop.RealDestination),
            troop.TargetTroop.Id,
            -1,
            PriorityTierAttackTroop);
    }
    
    private void GenerateAttackArchCommand(Troop troop)
    {
        var targetArchitecture = troop.TargetArchitecture;
        if (targetArchitecture == null || targetArchitecture.Endurance <= 0)
        {
            Troop targetTroop = troop.TargetTroop;
            if (targetTroop != null && !targetTroop.Destroyed)
            {
                troop.SetCommand(TroopCommand.AttackTroop);
                GenerateAttackTroopCommand(troop);
                return;
            }

            troop.SetCommand(TroopCommand.None);
            troop.SelectedAttack = false;
            return;
        }

        if (troop.TargetArchitecture == null)
            throw new InvalidOperationException($"部队 {troop.ID} 的 AttackArch 命令缺少目标城池");

        if (troop.CurrentCombatMethod != null)
        {
            troop.OrientationTroop = null;
            troop.OrientationArchitecture = targetArchitecture;
        }

        troop.SetCommand(TroopCommand.AttackArch);
        
        NormalizeAttackArchitectureExecutionPosition(troop, targetArchitecture);

        _commandBuffer.AttackArchQueue.Enqueue(new AttackArchCommand(
            troop.Id,
            troop.TargetArchitecture.ID,
            troop.RealDestination));

        RecordExecutionCommand(
            troop,
            ExecutionActionKind.AttackArchitecture,
            troop.RealDestination,
            troop.ResolveImmediateConflictPosition(troop.RealDestination),
            Guid.Empty,
            troop.TargetArchitecture.ID,
            PriorityTierAttackArchitecture);
    }
    
    private void GenerateLegacyAttackCommand(Troop troop)
    {
        bool hasTroopTarget = troop.TargetTroop != null && !troop.TargetTroop.Destroyed;
        bool hasArchitectureTarget = troop.TargetArchitecture != null && troop.TargetArchitecture.Endurance > 0;
        
        if (hasTroopTarget && (!hasArchitectureTarget || troop.Army.Kind.AirOffence || troop.CurrentStratagem != null || troop.CurrentCombatMethod != null))
        {
            GenerateAttackTroopCommand(troop);
            return;
        }
        
        if (hasArchitectureTarget)
        {
            GenerateAttackArchCommand(troop);
            return;
        }
        
        Point fallbackTarget = (troop.RealDestination.X >= 0 && troop.RealDestination.Y >= 0)
            ? troop.RealDestination
            : troop.Position;

        troop.SetCommand(TroopCommand.Move);
        
        _commandBuffer.MoveQueue.Enqueue(new MoveCommand(
            troop.Id,
            fallbackTarget,
            MoveCommand.PriorityNormalMove));

        RecordExecutionCommand(
            troop,
            ExecutionActionKind.Move,
            fallbackTarget,
            troop.ResolveImmediateConflictPosition(fallbackTarget),
            Guid.Empty,
            -1,
            PriorityTierMove,
            troop.stuckedFor);
    }
    
    private void GenerateStratagemCommand(Troop troop)
    {
        if (troop.CurrentStratagem == null)
            throw new InvalidOperationException($"部队 {troop.ID} 的 Stratagem 命令缺少计略");
        
        Guid targetId = troop.TargetTroop?.Id ?? troop.OrientationTroop?.Id ?? Guid.Empty;
        
        _commandBuffer.StratagemQueue.Enqueue(new StratagemCommand(
            troop.Id,
            targetId,
            troop.CurrentStratagem.ID));

        RecordExecutionCommand(
            troop,
            ExecutionActionKind.Stratagem,
            troop.Position,
            new Point(-1, -1),
            targetId,
            -1,
            PriorityTierStratagem);
    }
    
    /// <summary>
    /// 清理全体部队的瞬时执行态
    /// 日期：2026-03-24
    /// 用途：在执行阶段结束前，统一清理所有部队的临时动作状态
    /// 
    /// 为什么需要这个方法：
    /// - 新调度器只处理有命令的部队
    /// - 受击/受术目标部队通常没有自己的命令
    /// - 因此不会进入旧系统那条"顺手清理执行态"的路径
    /// - 必须在回合结束前统一扫全体部队
    /// 
    /// 为什么是"扫全体"而不是"只清 touched targets"：
    /// - 战斗影响对象不只是一对一（普通攻击、计略、范围计略、朝向、连带对象）
    /// - 维护 touched set 容易漏
    /// - 全量扫一次成本低、鲁棒性高
    /// </summary>
    private void ClearAllTroopsTransientState(GameScenario scenario)
    {
        int clearedCount = 0;
        
        for (int i = 0; i < scenario.Troops.Count; i++)
        {
            if (scenario.Troops[i] is Troop troop && !troop.Destroyed)
            {
                // 记录清理前的状态（仅用于统计）
                bool hadTransientState = (troop.Action == TroopAction.Move ||
                                         troop.Action == TroopAction.Attack ||
                                         troop.Action == TroopAction.Cast ||
                                         troop.Action == TroopAction.BeAttacked ||
                                         troop.Action == TroopAction.BeCasted);
                
                // 调用统一清理方法
                troop.ClearTransientExecutionState();
                
                if (hadTransientState)
                {
                    clearedCount++;
                }
            }
        }
        
        #if DEBUG
        if (clearedCount > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandBufferScheduler] 回合结束清理: 清理了 {clearedCount} 个部队的瞬时执行态");
        }
        #endif
    }
}
