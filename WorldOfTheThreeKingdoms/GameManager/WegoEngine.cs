#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.Commands;
using GameObjects.Snapshots;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager;

// 🔥 WEGO 引擎（We Go 同时行动引擎）
// 日期：2026-03-16
// 状态：实验性功能，阶段 3.1 测试中
// 架构：两阶段执行
//   阶段 1：决策阶段（多线程并行，纯读取快照，写入指令队列）
//   阶段 2：结算阶段（单线程顺序执行，从队列读取指令并修改游戏状态）

public class WegoEngine
{
    // 🔥 指令缓冲区
    private readonly CommandBuffer _commandBuffer = new();
    
    // 🔥 部队注册表（使用 ConcurrentDictionary 支持并发读取）
    // 注意：使用 Guid 类型（Troop.Id），不是 int 类型（Troop.ID）
    private readonly ConcurrentDictionary<Guid, Troop> _troopRegistry = new();
    
    // 🔥 复用的部队数组（避免每回合分配）
    private Troop[] _troopArray = new Troop[1024];  // 预分配合理大小
    
    // 🔥 复用的冲突检测集合（避免每回合分配）
    private readonly HashSet<Point> _occupiedPositions = new(capacity: 1024);
    
    // 🔥 复用的移动指令列表（避免每回合分配）
    private readonly List<MoveCommand> _sortedMoves = new(capacity: 1024);
    
    // 🔥 统计信息
    public int ProcessedMoveCommands { get; private set; }
    public int ProcessedAttackCommands { get; private set; }
    public int ProcessedStratagemCommands { get; private set; }
    public int ProcessedSiegeCommands { get; private set; }
    
    // 🔥 主更新循环（每回合调用一次）
    public void Update()
    {
        // 重置统计
        ProcessedMoveCommands = 0;
        ProcessedAttackCommands = 0;
        ProcessedStratagemCommands = 0;
        ProcessedSiegeCommands = 0;
        
        // --------------------------------------------------------
        // 阶段 1：决策阶段（多线程并行，纯读取）
        // --------------------------------------------------------
        AIDecisionPhase();
        
        #if DEBUG
        _commandBuffer.LogCommands();
        #endif
        
        // --------------------------------------------------------
        // 阶段 2：结算阶段（单线程顺序执行，无锁）
        // --------------------------------------------------------
        ExecutionPhase();
        
        // 清理指令缓冲区
        _commandBuffer.Clear();
        
        #if DEBUG
        System.Diagnostics.Debug.WriteLine($"[WegoEngine] 本回合处理: 移动={ProcessedMoveCommands}, 战斗={ProcessedAttackCommands}, 计略={ProcessedStratagemCommands}, 攻城={ProcessedSiegeCommands}");
        #endif
    }
    
    // 🔥 决策阶段：AI 并行决策，写入指令队列
    private void AIDecisionPhase()
    {
        // 🔥 创建快照（简化版：不使用锁）
        // 注意：阶段 3.1 是测试模式，不修改游戏状态，所以不需要锁
        // 后续阶段需要添加适当的同步机制
        GameStateSnapshot snapshot = CreateSnapshot();
        
        // 🔥 并行决策（无锁，纯读取快照）
        // 注意：ConcurrentDictionary 的枚举器分配是不可避免的
        // 但这是冷路径（每回合 1 次），可以接受
        // 如果需要极致性能，应该使用自定义的无锁数据结构
        
        // 复用数组避免每回合分配
        int troopCount = _troopRegistry.Count;
        if (_troopArray.Length < troopCount)
        {
            _troopArray = new Troop[troopCount * 2];  // 预留空间避免频繁扩容
        }
        
        // 🔥 权衡：使用 foreach 简化代码
        // 枚举器分配：~40 字节/回合（可接受）
        // 如果需要极致性能，应该重构为自定义的 Troop[] 数组 + 并发索引
        int index = 0;
        foreach (var kvp in _troopRegistry)
        {
            _troopArray[index++] = kvp.Value;
        }
        
        Parallel.For(0, troopCount, i =>
        {
            var troop = _troopArray[i];
            
            // 🔥 根据部队状态生成指令
            ProcessTroopDecision(troop, snapshot);
        });
    }
    
    // 🔥 处理单个部队的决策
    private void ProcessTroopDecision(Troop troop, GameStateSnapshot snapshot)
    {
        // 跳过已操作或已消灭的部队
        if (troop.Operated || troop.Destroyed) return;
        
        // 根据部队命令生成指令
        switch (troop.Command)
        {
            case TroopCommand.Move:
                ProcessMoveDecision(troop, snapshot);
                break;
                
            case TroopCommand.Attack:
                ProcessAttackDecision(troop, snapshot);
                break;
                
            case TroopCommand.Stratagem:
                ProcessStratagemDecision(troop, snapshot);
                break;
                
            case TroopCommand.None:
            default:
                // 无命令，跳过
                break;
        }
    }
    
    // 🔥 处理移动决策
    private void ProcessMoveDecision(Troop troop, GameStateSnapshot snapshot)
    {
        // 如果部队有目标位置
        if (troop.RealDestination.X >= 0 && troop.RealDestination.Y >= 0)
        {
            // 计算下一步位置（简化版，实际应该使用寻路）
            var nextPoint = CalculateNextStep(troop.Position, troop.RealDestination, snapshot);
            
            // 判断优先级
            int priority = MoveCommand.PriorityNormalMove;
            var targetArch = Session.Current.Scenario.GetArchitectureByPositionNoCheck(nextPoint);
            if (targetArch is not null && troop.IsFriendly(targetArch.BelongedFaction))
            {
                priority = MoveCommand.PriorityEnterCity;  // 进城优先
            }
            
            // 生成移动指令
            _commandBuffer.MoveQueue.Enqueue(new MoveCommand(troop.Id, nextPoint, priority));
        }
    }
    
    // 🔥 处理战斗决策
    private void ProcessAttackDecision(Troop troop, GameStateSnapshot snapshot)
    {
        // 查找目标（简化版）
        var target = FindNearestEnemy(troop, snapshot);
        if (target is not null)
        {
            // 计算伤害
            int damage = CalculateDamage(troop, target);
            
            // 生成战斗指令
            _commandBuffer.AttackQueue.Enqueue(new AttackCommand(troop.Id, target.Id, damage));
        }
    }
    
    // 🔥 处理计略决策
    private void ProcessStratagemDecision(Troop troop, GameStateSnapshot snapshot)
    {
        // 查找目标
        var target = FindStratagemTarget(troop, snapshot);
        if (target is not null && troop.CurrentStratagem is not null)
        {
            // 生成计略指令
            _commandBuffer.StratagemQueue.Enqueue(
                new StratagemCommand(troop.Id, target.Id, troop.CurrentStratagem.ID));
        }
    }
    
    // 🔥 结算阶段：单线程顺序执行指令
    private void ExecutionPhase()
    {
        // 🔥 第 1 轮：处理攻城指令
        while (_commandBuffer.SiegeQueue.TryDequeue(out var siegeCmd))
        {
            if (siegeCmd.IsValid() && _troopRegistry.TryGetValue(siegeCmd.AttackerId, out var attacker))
            {
                ExecuteSiegeCommand(attacker, siegeCmd);
                ProcessedSiegeCommands++;
            }
        }
        
        // 🔥 第 2 轮：处理战斗指令（可能导致部队消灭）
        while (_commandBuffer.AttackQueue.TryDequeue(out var attackCmd))
        {
            if (attackCmd.IsValid() && 
                _troopRegistry.TryGetValue(attackCmd.AttackerId, out var attacker) &&
                _troopRegistry.TryGetValue(attackCmd.TargetId, out var target))
            {
                ExecuteAttackCommand(attacker, target, attackCmd);
                ProcessedAttackCommands++;
            }
        }
        
        // 🔥 第 3 轮：处理移动指令（自动跳过已消灭的部队）
        _occupiedPositions.Clear();  // 复用 HashSet，避免分配
        _sortedMoves.Clear();        // 复用 List，避免分配
        
        // 收集所有移动指令
        while (_commandBuffer.MoveQueue.TryDequeue(out var moveCmd))
        {
            _sortedMoves.Add(moveCmd);
        }
        
        // 按优先级排序（进城 > 出城 > 普通移动）
        // 🔥 性能优化：使用静态比较器避免 Lambda 分配
        // 日期：2026-03-16
        _sortedMoves.Sort(MoveCommandPriorityComparer.Instance);
        
        // 执行移动指令
        for (int i = 0; i < _sortedMoves.Count; i++)
        {
            var moveCmd = _sortedMoves[i];
            
            // 验证部队是否仍然存在
            if (_troopRegistry.TryGetValue(moveCmd.TroopId, out var troop))
            {
                ExecuteMoveCommand(troop, moveCmd);
                ProcessedMoveCommands++;
            }
        }
        
        // 🔥 第 4 轮：处理计略指令
        while (_commandBuffer.StratagemQueue.TryDequeue(out var stratagemCmd))
        {
            if (stratagemCmd.IsValid() &&
                _troopRegistry.TryGetValue(stratagemCmd.CasterId, out var caster) &&
                _troopRegistry.TryGetValue(stratagemCmd.TargetId, out var target))
            {
                ExecuteStratagemCommand(caster, target, stratagemCmd);
                ProcessedStratagemCommands++;
            }
        }
    }
    
    // 🔥 静态比较器：避免 Lambda 分配
    // 日期：2026-03-16
    private sealed class MoveCommandPriorityComparer : IComparer<MoveCommand>
    {
        public static readonly MoveCommandPriorityComparer Instance = new();
        private MoveCommandPriorityComparer() { }
        
        public int Compare(MoveCommand a, MoveCommand b)
        {
            return a.Priority.CompareTo(b.Priority);
        }
    }
    
    // 🔥 执行移动指令
    private void ExecuteMoveCommand(Troop troop, MoveCommand cmd)
    {
        // 检测冲突
        if (!_occupiedPositions.Contains(cmd.TargetPosition) && 
            CanMoveTo(cmd.TargetPosition))
        {
            troop.Position = cmd.TargetPosition;
            _occupiedPositions.Add(cmd.TargetPosition);
            troop.OperationDone = true;
        }
        else
        {
            // 冲突处理：保持原位
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[WegoEngine] 移动冲突: {troop.DisplayName} 无法移动到 {cmd.TargetPosition}");
            #endif
            troop.OperationDone = true;
        }
    }
    
    // 🔥 执行战斗指令
    private void ExecuteAttackCommand(Troop attacker, Troop target, AttackCommand cmd)
    {
        // 扣除目标兵力
        target.Quantity -= cmd.Damage;
        
        if (target.Quantity <= 0)
        {
            target.Quantity = 0;
            target.Destroyed = true;
            _troopRegistry.TryRemove(target.Id, out _);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[WegoEngine] {target.DisplayName} 被 {attacker.DisplayName} 消灭");
            #endif
        }
        
        attacker.OperationDone = true;
    }
    
    // 🔥 执行计略指令
    private void ExecuteStratagemCommand(Troop caster, Troop target, StratagemCommand cmd)
    {
        // 查找计略
        var stratagem = Session.Current.Scenario.GameCommonData.AllStratagems.GetStratagem(cmd.StratagemId);
        if (stratagem is not null)
        {
            // 🔥 简化版：计略效果需要通过现有的计略系统执行
            // TODO: 集成现有的计略执行逻辑
            // stratagem.ApplyEffect(caster, target);  // 此方法不存在
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[WegoEngine] {caster.DisplayName} 对 {target.DisplayName} 使用 {stratagem.Name}（简化版，未实际执行效果）");
            #endif
        }
        
        caster.OperationDone = true;
    }
    
    // 🔥 执行攻城指令
    private void ExecuteSiegeCommand(Troop attacker, SiegeCommand cmd)
    {
        // 🔥 关键：ID >= 0 是有效的，ID=0 是洛阳
        var architecture = Session.Current.Scenario.Architectures.GetGameObject(cmd.ArchitectureId) as Architecture;
        if (architecture is not null)
        {
            architecture.Endurance -= cmd.Damage;
            
            if (architecture.Endurance <= 0)
            {
                architecture.Endurance = 0;
                architecture.BelongedFaction = attacker.BelongedFaction;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[WegoEngine] {attacker.DisplayName} 攻破 {architecture.Name}");
                #endif
            }
        }
        
        attacker.OperationDone = true;
    }
    
    // ========================================
    // 辅助方法（简化版，实际应该更复杂）
    // ========================================
    
    private GameStateSnapshot CreateSnapshot()
    {
        // 🔥 ANTI-BAND-AID：Session.Current.Scenario 必须存在
        var scenario = Session.Current.Scenario;
        if (scenario == null)
            throw new InvalidOperationException("CreateSnapshot: Scenario 为 null");
        
        // 🔥 收集所有部队快照
        List<TroopSnapshot> allTroops = [];
        foreach (var kvp in _troopRegistry)
        {
            var troop = kvp.Value;
            // 🔥 ANTI-BAND-AID：注册表中的 Troop 必须非 null
            // 如果为 null，说明注册表数据损坏，应该 Fail Fast
            if (troop == null)
                throw new InvalidOperationException($"CreateSnapshot: 注册表中存在 null Troop (ID: {kvp.Key})");
            
            if (!troop.Destroyed)
            {
                allTroops.Add(new TroopSnapshot(troop));
            }
        }
        
        // 🔥 创建地形代价地图（简化版）
        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;
        int[,] terrainCosts = new int[mapWidth, mapHeight];
        
        // 🔥 简化版：所有地形代价为 1（实际应该从 ScenarioMap 读取）
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                terrainCosts[x, y] = 1;
            }
        }
        
        // 🔥 使用 C# 12 集合表达式
        return new GameStateSnapshot(
            [.. allTroops],  // myTroops（简化版：所有部队）
            [.. allTroops],  // enemyTroops（简化版：所有部队）
            [],              // myArchitectures（TODO）
            [],              // enemyArchitectures（TODO）
            terrainCosts);
    }
    
    private Point CalculateNextStep(Point current, Point destination, GameStateSnapshot snapshot)
    {
        // 🔥 简化版寻路：直接朝目标移动一格
        // TODO: 实现完整的 A* 寻路逻辑
        
        int dx = Math.Sign(destination.X - current.X);
        int dy = Math.Sign(destination.Y - current.Y);
        
        Point nextPoint = new Point(current.X + dx, current.Y + dy);
        
        // 🔥 边界检查（Scenario 已在 CreateSnapshot 中验证）
        var scenario = Session.Current.Scenario;
        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;
        
        nextPoint.X = Math.Clamp(nextPoint.X, 0, mapWidth - 1);
        nextPoint.Y = Math.Clamp(nextPoint.Y, 0, mapHeight - 1);
        
        return nextPoint;
    }
    
    private Troop? FindNearestEnemy(Troop troop, GameStateSnapshot snapshot)
    {
        // 🔥 简化版：查找最近的敌人
        Troop? nearestEnemy = null;
        int minDistanceSquared = int.MaxValue;
        
        // 🔥 使用 for 循环避免 LINQ（Hot Path 优化）
        for (int i = 0; i < snapshot.EnemyTroops.Length; i++)
        {
            var enemySnapshot = snapshot.EnemyTroops[i];
            
            // 🔥 关键：ID >= 0 是有效的，ID=0 是有效部队
            if (enemySnapshot.FactionId >= 0 && 
                enemySnapshot.FactionId != troop.BelongedFaction?.ID)
            {
                // 计算距离平方（避免开方运算）
                int dx = enemySnapshot.Position.X - troop.Position.X;
                int dy = enemySnapshot.Position.Y - troop.Position.Y;
                int distanceSquared = dx * dx + dy * dy;
                
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    
                    // 从注册表中获取实际的 Troop 对象
                    // 🔥 修复：使用 TroopID 而不是 ID（TroopSnapshot 的字段名）
                    if (_troopRegistry.TryGetValue(enemySnapshot.TroopID, out var enemy))
                    {
                        nearestEnemy = enemy;
                    }
                }
            }
        }
        
        return nearestEnemy;
    }
    
    private Troop? FindStratagemTarget(Troop troop, GameStateSnapshot snapshot)
    {
        // 🔥 简化版：查找最近的敌人作为计略目标
        // TODO: 实现更智能的目标选择（考虑计略类型、成功率等）
        return FindNearestEnemy(troop, snapshot);
    }
    
    private int CalculateDamage(Troop attacker, Troop target)
    {
        // 🔥 简化版伤害计算
        // TODO: 实现完整的伤害计算逻辑（考虑攻击力、防御力、地形、天气等）
        // 🔥 ANTI-BAND-AID：调用者保证 attacker 和 target 非 null
        
        // 基础伤害 = 攻击方兵力 * 0.1
        int baseDamage = (int)(attacker.Quantity * 0.1f);
        
        // 考虑攻击力和防御力
        float offence = attacker.Offence;
        float defence = target.Defence;
        
        // 伤害修正
        float damageMultiplier = offence / Math.Max(defence, 1.0f);
        int finalDamage = (int)(baseDamage * damageMultiplier);
        
        // 最小伤害为 1
        return Math.Max(finalDamage, 1);
    }
    
    private bool CanMoveTo(Point position)
    {
        // 🔥 简化版地形检测
        // TODO: 实现完整的地形检测逻辑（考虑地形类型、障碍物等）
        
        var scenario = Session.Current.Scenario;
        if (scenario == null) return false;
        
        // 边界检查
        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;
        
        if (position.X < 0 || position.X >= mapWidth ||
            position.Y < 0 || position.Y >= mapHeight)
        {
            return false;
        }
        
        // 简化版：所有地形都可通行
        return true;
    }
    
    // ========================================
    // 公共接口
    // ========================================
    
    // 🔥 注册部队
    public void RegisterTroop(Troop troop)
    {
        _troopRegistry.TryAdd(troop.Id, troop);
    }
    
    // 🔥 添加部队（RegisterTroop 的别名，用于测试代码）
    public void AddTroop(Troop troop)
    {
        RegisterTroop(troop);
    }
    
    // 🔥 注销部队
    public void UnregisterTroop(Troop troop)
    {
        _troopRegistry.TryRemove(troop.Id, out _);
    }
    
    // 🔥 获取部队数量
    public int TroopCount => _troopRegistry.Count;
}
