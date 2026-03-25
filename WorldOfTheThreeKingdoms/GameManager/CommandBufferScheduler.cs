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
    
    // 🔥 性能优化：使用预分配数组代替 Queue（避免堆分配）
    private Troop[] _activeQueue = new Troop[256];
    private int _queueHead;
    private int _queueTail;
    private int _queueCount;
    
    private Troop _currentTroop;
    private int _safetyCounter;
    private bool _queueEndedPassCompleted;
    
    // 本回合 CommandBuffer 是否可用（失败时必须整回合回退旧调度器）
    public bool HasValidBuffer { get; private set; }
    
    public int ProcessedMoveCommands { get; private set; }
    public int ProcessedEnterCommands { get; private set; }
    public int ProcessedAttackTroopCommands { get; private set; }
    public int ProcessedAttackArchCommands { get; private set; }
    public int ProcessedStratagemCommands { get; private set; }
    
    // 🔥 新增：细分攻击类型统计
    public int ProcessedDirectAttacks { get; private set; }
    public int ProcessedCombatMethodAttacks { get; private set; }
    public int ProcessedStratagemAttacks { get; private set; }
    
    public bool BuildCommandBuffer(GameScenario scenario)
    {
        _commandBuffer.Clear();
        _queueHead = 0;
        _queueTail = 0;
        _queueCount = 0;
        _currentTroop = null;
        _safetyCounter = 0;
        _queueEndedPassCompleted = false;
        HasValidBuffer = false;
        
        ProcessedMoveCommands = 0;
        ProcessedEnterCommands = 0;
        ProcessedAttackTroopCommands = 0;
        ProcessedAttackArchCommands = 0;
        ProcessedStratagemCommands = 0;
        ProcessedDirectAttacks = 0;
        ProcessedCombatMethodAttacks = 0;
        ProcessedStratagemAttacks = 0;
        
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
            HasValidBuffer = true;
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
            HasValidBuffer = false;
            
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
            if (!ProcessCurrentTroop(gameTime))
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
                _currentTroop = null;
                continue;
            }
            
            QueueAction action = TroopStateMachineRouter.DetermineQueueAction(_currentTroop);
            if (!HandleQueueAction(action, gameTime))
            {
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
    
    private bool ProcessCurrentTroop(GameTime gameTime)
    {
        if (_currentTroop.Destroyed)
        {
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
        TroopListWithQueue troopList = Session.Current.Scenario.Troops;
        troopList.TroopChangeRealDestination(_currentTroop);
        
        if (_currentTroop.Command == TroopCommand.Enter)
        {
            _currentTroop.CurrentAIState = TroopAIState.EnterCity;
        }
        else if (_currentTroop.Command != TroopCommand.None)
        {
            _currentTroop.CurrentAIState = TroopAIState.Marching;
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
    
    private void GenerateMoveCommand(Troop troop)
    {
        if (troop.RealDestination.X >= 0 && troop.RealDestination.Y >= 0)
        {
            _commandBuffer.MoveQueue.Enqueue(new MoveCommand(
                troop.Id, 
                troop.RealDestination, 
                MoveCommand.PriorityNormalMove));
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
        
        _commandBuffer.AttackTroopQueue.Enqueue(new AttackTroopCommand(
            troop.Id,
            troop.TargetTroop.Id,
            troop.RealDestination));
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
        
        _commandBuffer.AttackArchQueue.Enqueue(new AttackArchCommand(
            troop.Id,
            troop.TargetArchitecture.ID,
            troop.RealDestination));
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
