using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameObjects.AI;

#if false
using GameObjects.AI.Helper;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI
{
    /// <summary>
    /// AI地图导航集成示例
    /// 展示如何将MapNavigationHelper与现有AI系统集成使用
    /// </summary>
    public static class AIMapNavigationIntegrationExample
    {
        /// <summary>
        /// 完整的AI战术决策示例
        /// 结合角色选择、战术定位和地图导航
        /// </summary>
        /// <param name="troop">AI控制的部队</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteAITacticalDecision(Troop troop, List<Troop> allTroops)
        {
            try
            {
                Console.WriteLine($"=== 开始为部队 {troop.ID} 执行AI战术决策 ===");

                // 1. 确保部队有分配的战术角色
                if (troop.CurrentRole == TroopRole.None)
                {
                    troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
                    Console.WriteLine($"为部队 {troop.ID} 分配角色: {GetRoleDescription(troop.CurrentRole)}");
                }

                // 2. 分析战场态势
                var battlefieldAnalysis = AnalyzeBattlefield(troop, allTroops);
                Console.WriteLine($"战场分析完成 - 敌军: {battlefieldAnalysis.Enemies.Count}, 友军: {battlefieldAnalysis.Allies.Count}");

                // 3. 根据角色执行相应的战术行为
                switch (troop.CurrentRole)
                {
                    case TroopRole.Tank:
                        ExecuteTankTactics(troop, battlefieldAnalysis);
                        break;
                    case TroopRole.DPS:
                        ExecuteDPSTactics(troop, battlefieldAnalysis);
                        break;
                    case TroopRole.Mage:
                        ExecuteMageTactics(troop, battlefieldAnalysis);
                        break;
                    case TroopRole.Support:
                        ExecuteSupportTactics(troop, battlefieldAnalysis);
                        break;
                    case TroopRole.Logistics:
                        ExecuteLogisticsTactics(troop, battlefieldAnalysis);
                        break;
                    default:
                        ExecuteDefaultTactics(troop, battlefieldAnalysis);
                        break;
                }

                Console.WriteLine($"=== 部队 {troop.ID} 战术决策执行完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIMapNavigationIntegrationExample] 执行AI战术决策时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 战场态势分析结果
        /// </summary>
        public class BattlefieldAnalysis
        {
            public List<Troop> Enemies { get; set; } = new List<Troop>();
            public List<Troop> Allies { get; set; } = new List<Troop>();
            public Troop NearestEnemy { get; set; }
            public Troop MostVulnerableAlly { get; set; }
            public Point SafePosition { get; set; }
            public bool InDanger { get; set; }
        }

        /// <summary>
        /// 分析战场态势
        /// </summary>
        /// <param name="troop">分析的部队</param>
        /// <param name="allTroops">所有部队</param>
        /// <returns>战场分析结果</returns>
        private static BattlefieldAnalysis AnalyzeBattlefield(Troop troop, List<Troop> allTroops)
        {
            var analysis = new BattlefieldAnalysis();

            try
            {
                // 使用MapNavigationHelper获取周围的敌军和友军
                int viewRange = Math.Max(troop.ViewRadius, 10);
                analysis.Enemies = MapNavigationHelper.GetEnemyTroopsInRange(troop, troop.Position, viewRange);
                analysis.Allies = MapNavigationHelper.GetFriendlyTroopsInRange(troop, troop.Position, viewRange);

                // 使用AITargetSelector选择最佳目标
                if (analysis.Enemies.Count > 0)
                {
                    analysis.NearestEnemy = AITargetSelector.GetBestTarget(troop, analysis.Enemies, analysis.Allies);
                    
                    // 如果没有找到最佳目标，回退到最近敌军
                    if (analysis.NearestEnemy == null)
                    {
                        analysis.NearestEnemy = analysis.Enemies
                            .OrderBy(e => MapNavigationHelper.GetManhattanDistance(troop.Position, e.Position))
                            .First();
                    }
                }

                // 找到最脆弱的友军（需要保护的）
                analysis.MostVulnerableAlly = analysis.Allies
                    .Where(a => a.CurrentRole == TroopRole.Mage || a.CurrentRole == TroopRole.Support)
                    .OrderBy(a => a.Quantity) // 兵力最少的最脆弱
                    .FirstOrDefault();

                // 计算安全位置
                if (analysis.Enemies.Count > 0)
                {
                    analysis.SafePosition = MapNavigationHelper.GetBestRetreatPosition(
                        troop, analysis.Enemies, analysis.Allies);
                }
                else
                {
                    analysis.SafePosition = troop.Position;
                }

                // 判断是否处于危险中
                analysis.InDanger = analysis.Enemies.Any(e => 
                    MapNavigationHelper.GetManhattanDistance(troop.Position, e.Position) <= 3);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattlefieldAnalysis] 分析战场时发生错误: {ex.Message}");
            }

            return analysis;
        }

        /// <summary>
        /// 执行坦克战术
        /// </summary>
        private static void ExecuteTankTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[Tank] 部队 {troop.ID} 执行坦克战术");

            try
            {
                if (analysis.NearestEnemy != null)
                {
                    // 坦克的主要任务：冲向敌军，保护友军
                    var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                    var bestPosition = AITacticalPositioner.GetBestPosition(
                        troop, analysis.NearestEnemy, analysis.Allies, moveablePositions);

                    if (bestPosition != troop.Position)
                    {
                        // 计算移动路径
                        var path = MapNavigationHelper.FindPath(troop, troop.Position, bestPosition);
                        if (path.Count > 0)
                        {
                            Console.WriteLine($"[Tank] 部队 {troop.ID} 移动到位置 ({bestPosition.X},{bestPosition.Y})");
                            // 这里可以设置部队的移动目标
                            // troop.Destination = bestPosition;
                        }
                    }

                    // 如果在攻击范围内，攻击敌军
                    int distanceToEnemy = MapNavigationHelper.GetManhattanDistance(troop.Position, analysis.NearestEnemy.Position);
                    if (distanceToEnemy <= troop.OffenceRadius)
                    {
                        Console.WriteLine($"[Tank] 部队 {troop.ID} 攻击敌军 {analysis.NearestEnemy.ID}");
                        // troop.AttackTroop(analysis.NearestEnemy);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tank] 执行坦克战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行DPS战术
        /// </summary>
        private static void ExecuteDPSTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[DPS] 部队 {troop.ID} 执行DPS战术");

            try
            {
                if (analysis.NearestEnemy != null)
                {
                    int distanceToEnemy = MapNavigationHelper.GetManhattanDistance(troop.Position, analysis.NearestEnemy.Position);
                    int attackRange = GetTroopAttackRange(troop);

                    if (distanceToEnemy <= 1 && analysis.InDanger)
                    {
                        // 太危险了，需要撤退
                        Console.WriteLine($"[DPS] 部队 {troop.ID} 处于危险中，执行撤退");
                        var path = MapNavigationHelper.FindPath(troop, troop.Position, analysis.SafePosition);
                        if (path.Count > 0)
                        {
                            // troop.Destination = analysis.SafePosition;
                        }
                    }
                    else if (distanceToEnemy <= attackRange)
                    {
                        // 在攻击范围内，进行攻击
                        Console.WriteLine($"[DPS] 部队 {troop.ID} 攻击敌军 {analysis.NearestEnemy.ID}");
                        // troop.AttackTroop(analysis.NearestEnemy);
                    }
                    else
                    {
                        // 移动到攻击范围内
                        var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                        var bestPosition = AITacticalPositioner.GetBestPosition(
                            troop, analysis.NearestEnemy, analysis.Allies, moveablePositions);

                        if (bestPosition != troop.Position)
                        {
                            var path = MapNavigationHelper.FindPath(troop, troop.Position, bestPosition);
                            if (path.Count > 0)
                            {
                                Console.WriteLine($"[DPS] 部队 {troop.ID} 移动到攻击位置 ({bestPosition.X},{bestPosition.Y})");
                                // troop.Destination = bestPosition;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DPS] 执行DPS战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行法师战术
        /// </summary>
        private static void ExecuteMageTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[Mage] 部队 {troop.ID} 执行法师战术");

            try
            {
                if (analysis.InDanger)
                {
                    // 法师优先保命
                    Console.WriteLine($"[Mage] 部队 {troop.ID} 处于危险中，寻找安全位置");
                    var path = MapNavigationHelper.FindPath(troop, troop.Position, analysis.SafePosition);
                    if (path.Count > 0)
                    {
                        // troop.Destination = analysis.SafePosition;
                    }
                }
                else if (analysis.NearestEnemy != null)
                {
                    // 检查是否有视线进行远程攻击
                    bool hasLineOfSight = MapNavigationHelper.HasLineOfSight(troop, troop.Position, analysis.NearestEnemy.Position);
                    int distanceToEnemy = MapNavigationHelper.GetManhattanDistance(troop.Position, analysis.NearestEnemy.Position);
                    int attackRange = GetTroopAttackRange(troop);

                    if (hasLineOfSight && distanceToEnemy <= attackRange)
                    {
                        Console.WriteLine($"[Mage] 部队 {troop.ID} 远程攻击敌军 {analysis.NearestEnemy.ID}");
                        // 可以使用策略攻击
                        // troop.CastStratagem(analysis.NearestEnemy);
                    }
                    else
                    {
                        // 寻找更好的攻击位置
                        var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                        var bestPosition = AITacticalPositioner.GetBestPosition(
                            troop, analysis.NearestEnemy, analysis.Allies, moveablePositions);

                        if (bestPosition != troop.Position)
                        {
                            var path = MapNavigationHelper.FindPath(troop, troop.Position, bestPosition);
                            if (path.Count > 0)
                            {
                                Console.WriteLine($"[Mage] 部队 {troop.ID} 移动到施法位置 ({bestPosition.X},{bestPosition.Y})");
                                // troop.Destination = bestPosition;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mage] 执行法师战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行辅助战术
        /// </summary>
        private static void ExecuteSupportTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[Support] 部队 {troop.ID} 执行辅助战术");

            try
            {
                if (analysis.InDanger)
                {
                    // 辅助优先保命
                    Console.WriteLine($"[Support] 部队 {troop.ID} 寻找安全位置");
                    var path = MapNavigationHelper.FindPath(troop, troop.Position, analysis.SafePosition);
                    if (path.Count > 0)
                    {
                        // troop.Destination = analysis.SafePosition;
                    }
                }
                else
                {
                    // 寻找需要治疗的友军或提供光环支持的位置
                    var injuredAllies = analysis.Allies.Where(a => a.InjuryQuantity > 0).ToList();
                    
                    if (injuredAllies.Count > 0)
                    {
                        // 移动到受伤友军附近
                        var nearestInjured = injuredAllies
                            .OrderBy(a => MapNavigationHelper.GetManhattanDistance(troop.Position, a.Position))
                            .First();

                        Console.WriteLine($"[Support] 部队 {troop.ID} 移动到受伤友军 {nearestInjured.ID} 附近");
                        
                        // 寻找在治疗范围内的最佳位置
                        var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                        var bestSupportPosition = moveablePositions
                            .Where(p => MapNavigationHelper.GetManhattanDistance(p, nearestInjured.Position) <= 2)
                            .OrderBy(p => MapNavigationHelper.GetManhattanDistance(p, nearestInjured.Position))
                            .FirstOrDefault();

                        if (bestSupportPosition != Point.Zero && bestSupportPosition != troop.Position)
                        {
                            var path = MapNavigationHelper.FindPath(troop, troop.Position, bestSupportPosition);
                            if (path.Count > 0)
                            {
                                // troop.Destination = bestSupportPosition;
                            }
                        }
                    }
                    else
                    {
                        // 没有受伤友军，保持在友军中心提供光环支持
                        if (analysis.Allies.Count > 0)
                        {
                            var centerPosition = CalculateCenterPosition(analysis.Allies);
                            var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                            var bestPosition = moveablePositions
                                .OrderBy(p => MapNavigationHelper.GetManhattanDistance(p, centerPosition))
                                .FirstOrDefault();

                            if (bestPosition != Point.Zero && bestPosition != troop.Position)
                            {
                                var path = MapNavigationHelper.FindPath(troop, troop.Position, bestPosition);
                                if (path.Count > 0)
                                {
                                    Console.WriteLine($"[Support] 部队 {troop.ID} 移动到友军中心位置");
                                    // troop.Destination = bestPosition;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Support] 执行辅助战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行后勤战术
        /// </summary>
        private static void ExecuteLogisticsTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[Logistics] 部队 {troop.ID} 执行后勤战术");

            try
            {
                // 后勤部队应该远离战斗，寻找最安全的位置
                if (analysis.Enemies.Count > 0)
                {
                    var path = MapNavigationHelper.FindPath(troop, troop.Position, analysis.SafePosition);
                    if (path.Count > 0)
                    {
                        Console.WriteLine($"[Logistics] 部队 {troop.ID} 撤退到安全位置");
                        // troop.Destination = analysis.SafePosition;
                    }
                }

                // 后勤部队可以执行建造、运输等任务
                // 这里可以添加具体的后勤逻辑
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logistics] 执行后勤战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行默认战术
        /// </summary>
        private static void ExecuteDefaultTactics(Troop troop, BattlefieldAnalysis analysis)
        {
            Console.WriteLine($"[Default] 部队 {troop.ID} 执行默认战术");

            try
            {
                if (analysis.NearestEnemy != null)
                {
                    // 简单的接敌逻辑
                    var path = MapNavigationHelper.FindPath(troop, troop.Position, analysis.NearestEnemy.Position);
                    if (path.Count > 0)
                    {
                        Console.WriteLine($"[Default] 部队 {troop.ID} 向敌军移动");
                        // troop.Destination = analysis.NearestEnemy.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Default] 执行默认战术时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取部队的攻击范围
        /// </summary>
        private static int GetTroopAttackRange(Troop troop)
        {
            try
            {
                return troop.OffenceRadius;
            }
            catch
            {
                // 根据角色返回默认值
                switch (troop.CurrentRole)
                {
                    case TroopRole.Mage:
                        return 3;
                    case TroopRole.DPS:
                        return 2;
                    case TroopRole.Tank:
                        return 1;
                    default:
                        return 2;
                }
            }
        }

        /// <summary>
        /// 计算部队列表的中心位置
        /// </summary>
        private static Point CalculateCenterPosition(List<Troop> troops)
        {
            if (troops.Count == 0) return Point.Zero;

            int totalX = troops.Sum(t => t.Position.X);
            int totalY = troops.Sum(t => t.Position.Y);

            return new Point(totalX / troops.Count, totalY / troops.Count);
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        private static string GetRoleDescription(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.Tank: return "肉盾";
                case TroopRole.DPS: return "输出";
                case TroopRole.Mage: return "法师";
                case TroopRole.Support: return "辅助";
                case TroopRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 批量执行AI决策（用于回合制或实时战斗）
        /// </summary>
        /// <param name="aiTroops">需要AI控制的部队列表</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteBatchAIDecisions(List<Troop> aiTroops, List<Troop> allTroops)
        {
            Console.WriteLine($"=== 开始批量执行AI决策，共 {aiTroops.Count} 个部队 ===");

            try
            {
                foreach (var troop in aiTroops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        ExecuteAITacticalDecision(troop, allTroops);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIMapNavigationIntegrationExample] 批量执行AI决策时发生错误: {ex.Message}");
            }

            Console.WriteLine("=== 批量AI决策执行完成 ===");
        }

        /// <summary>
        /// 执行优化序列的AI决策（推荐使用）
        /// 使用AIActionSequencer优化行动顺序，实现更好的战术协同
        /// </summary>
        /// <param name="aiTroops">需要AI控制的部队列表</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteOptimizedAIDecisions(List<Troop> aiTroops, List<Troop> allTroops)
        {
            Console.WriteLine($"=== 开始执行优化序列AI决策 ===");

            try
            {
                // 使用AIActionSequencer优化行动顺序
                AIActionSequencer.ExecuteSequencedAIDecisions(aiTroops, allTroops);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIMapNavigationIntegrationExample] 执行优化AI决策时发生错误: {ex.Message}");
            }

            Console.WriteLine("=== 优化序列AI决策执行完成 ===");
        }

        /// <summary>
        /// 执行分组AI决策
        /// 按角色分组执行，同角色并行，不同角色按优先级顺序
        /// </summary>
        /// <param name="aiTroops">需要AI控制的部队列表</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteGroupedAIDecisions(List<Troop> aiTroops, List<Troop> allTroops)
        {
            Console.WriteLine($"=== 开始执行分组AI决策 ===");

            try
            {
                // 使用AIActionSequencer按角色分组执行
                AIActionSequencer.ExecuteGroupedAIDecisions(aiTroops, allTroops);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIMapNavigationIntegrationExample] 执行分组AI决策时发生错误: {ex.Message}");
            }

            Console.WriteLine("=== 分组AI决策执行完成 ===");
        }

        /// <summary>
        /// 执行集火战术（多个部队协同攻击同一目标）
        /// </summary>
        /// <param name="aiTroops">AI控制的部队列表</param>
        /// <param name="enemies">敌军列表</param>
        public static void ExecuteFocusFireTactic(List<Troop> aiTroops, List<Troop> enemies)
        {
            Console.WriteLine($"=== 开始执行集火战术 ===");

            try
            {
                if (aiTroops == null || enemies == null || aiTroops.Count == 0 || enemies.Count == 0)
                {
                    Console.WriteLine("[FocusFireTactic] 没有可用的部队或目标");
                    return;
                }

                // 1. 选择集火目标
                var focusTarget = AITargetSelector.GetFocusFireTarget(aiTroops, enemies);
                
                if (focusTarget == null)
                {
                    Console.WriteLine("[FocusFireTactic] 没有找到合适的集火目标，执行常规目标分配");
                    ExecuteRegularTargetAssignment(aiTroops, enemies);
                    return;
                }

                Console.WriteLine($"[FocusFireTactic] 选定集火目标: 敌军 {focusTarget.ID}");

                // 2. 为每个部队分配行动
                int attackerCount = 0;
                foreach (var troop in aiTroops)
                {
                    if (troop == null || troop.Destroyed) continue;

                    // 检查是否能攻击集火目标
                    int distance = MapNavigationHelper.GetManhattanDistance(troop.Position, focusTarget.Position);
                    int attackRange = GetTroopAttackRange(troop);

                    if (distance <= attackRange)
                    {
                        // 在攻击范围内，直接攻击
                        Console.WriteLine($"[FocusFireTactic] 部队 {troop.ID} 攻击集火目标");
                        // troop.AttackTroop(focusTarget);
                        attackerCount++;
                    }
                    else
                    {
                        // 不在攻击范围内，移动到攻击位置
                        var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(troop);
                        var attackPositions = moveablePositions
                            .Where(p => MapNavigationHelper.GetManhattanDistance(p, focusTarget.Position) <= attackRange)
                            .OrderBy(p => MapNavigationHelper.GetManhattanDistance(p, focusTarget.Position))
                            .ToList();

                        if (attackPositions.Count > 0)
                        {
                            var bestPosition = attackPositions.First();
                            var path = MapNavigationHelper.FindPath(troop, troop.Position, bestPosition);
                            
                            if (path.Count > 0)
                            {
                                Console.WriteLine($"[FocusFireTactic] 部队 {troop.ID} 移动到攻击位置 ({bestPosition.X},{bestPosition.Y})");
                                // troop.Destination = bestPosition;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[FocusFireTactic] 部队 {troop.ID} 无法到达攻击位置，执行常规行动");
                            ExecuteAITacticalDecision(troop, aiTroops.Concat(enemies).ToList());
                        }
                    }
                }

                Console.WriteLine($"[FocusFireTactic] 集火战术执行完成，共 {attackerCount} 个部队参与攻击");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FocusFireTactic] 执行集火战术时发生错误: {ex.Message}");
            }

            Console.WriteLine("=== 集火战术执行完成 ===");
        }

        /// <summary>
        /// 执行常规目标分配（每个部队选择自己的最佳目标）
        /// </summary>
        /// <param name="aiTroops">AI控制的部队列表</param>
        /// <param name="enemies">敌军列表</param>
        public static void ExecuteRegularTargetAssignment(List<Troop> aiTroops, List<Troop> enemies)
        {
            Console.WriteLine($"=== 开始执行常规目标分配 ===");

            try
            {
                var targetAssignments = AITargetSelector.AssignTargetsToTroops(aiTroops, enemies);

                foreach (var assignment in targetAssignments)
                {
                    var attacker = assignment.Key;
                    var target = assignment.Value;

                    Console.WriteLine($"[RegularTargetAssignment] 部队 {attacker.ID} 攻击目标 {target.ID}");

                    // 检查攻击距离
                    int distance = MapNavigationHelper.GetManhattanDistance(attacker.Position, target.Position);
                    int attackRange = GetTroopAttackRange(attacker);

                    if (distance <= attackRange)
                    {
                        // 直接攻击
                        // attacker.AttackTroop(target);
                    }
                    else
                    {
                        // 移动到攻击范围
                        var moveablePositions = MapNavigationHelper.GetUnitMoveableArea(attacker);
                        var bestPosition = AITacticalPositioner.GetBestPosition(
                            attacker, target, aiTroops, moveablePositions);

                        if (bestPosition != attacker.Position)
                        {
                            var path = MapNavigationHelper.FindPath(attacker, attacker.Position, bestPosition);
                            if (path.Count > 0)
                            {
                                Console.WriteLine($"[RegularTargetAssignment] 部队 {attacker.ID} 移动到位置 ({bestPosition.X},{bestPosition.Y})");
                                // attacker.Destination = bestPosition;
                            }
                        }
                    }
                }

                Console.WriteLine($"[RegularTargetAssignment] 常规目标分配完成，共分配 {targetAssignments.Count} 个目标");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegularTargetAssignment] 执行常规目标分配时发生错误: {ex.Message}");
            }

            Console.WriteLine("=== 常规目标分配完成 ===");
        }
    }
}

#endif