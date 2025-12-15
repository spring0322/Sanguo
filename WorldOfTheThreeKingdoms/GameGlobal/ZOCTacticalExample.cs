using System;
using System.Collections.Generic;
using System.Drawing;
using GameObjects;
using GameManager;

namespace GameGlobal
{
    /// <summary>
    /// ZOC战术系统使用示例
    /// 展示如何在实际游戏中集成和使用ZOC功能
    /// </summary>
    public static class ZOCTacticalExample
    {
        /// <summary>
        /// 示例1：Tank角色执行智能卡位
        /// 这是用户提供的核心逻辑
        /// </summary>
        /// <param name="tankTroop">肉盾部队</param>
        /// <returns>是否成功执行卡位</returns>
        public static bool ExecuteTankZOCBlocking(Troop tankTroop)
        {
            try
            {
                // 只有 Tank 角色才执行这种复杂的卡位运算
                if (AIRoleSelector.DetermineRole(tankTroop) != TroopRole.Tank)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC示例] {tankTroop.Leader?.Name} 不是Tank角色，跳过ZOC卡位");
                    return false;
                }

                // 1. 获取所有能走到的格子
                List<Point> moveRange = tankTroop.GetMoveRange();
                if (moveRange.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC示例] {tankTroop.Leader?.Name} 无可移动位置");
                    return false;
                }

                // 2. 获取视野内的敌人
                List<Troop> visibleEnemies = tankTroop.GetVisibleEnemies();
                if (visibleEnemies.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC示例] {tankTroop.Leader?.Name} 未发现敌军");
                    return false;
                }

                // 3. 确定要保护的大哥 (比如己方的主将、或者兵力最少的队友)
                Troop vip = tankTroop.GetLowestTroopAlly();

                // 4. 计算最佳卡位点
                Point blockPoint = ZOCEvaluator.GetBestBlockingPosition(tankTroop, moveRange, visibleEnemies, vip);

                // 5. 执行移动
                bool success = tankTroop.MoveTo(blockPoint);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC示例] {tankTroop.Leader?.Name} 成功执行Tank卡位到 ({blockPoint.X}, {blockPoint.Y})");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC示例] ExecuteTankZOCBlocking 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 示例2：整个势力的协同ZOC战术
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="enemies">敌军列表</param>
        /// <returns>执行成功的部队数量</returns>
        public static int ExecuteFactionZOCTactics(Faction faction, List<Troop> enemies)
        {
            int successCount = 0;
            
            try
            {
                if (faction?.Troops == null) return 0;

                System.Diagnostics.Debug.WriteLine($"[ZOC示例] {faction.Name} 开始执行势力级ZOC战术");

                // 获取关键保护目标
                var keyTargets = GetKeyProtectionTargets(faction);

                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop == null) continue;

                    TroopRole role = AIRoleSelector.DetermineRole(troop);
                    bool executed = false;

                    switch (role)
                    {
                        case TroopRole.Tank:
                            // Tank执行前线卡位
                            executed = troop.ExecuteIntelligentZOCBlocking();
                            break;

                        case TroopRole.Support:
                            // Support执行保护卡位
                            executed = ExecuteSupportZOC(troop, keyTargets, enemies);
                            break;

                        case TroopRole.DPS:
                            // DPS执行侧翼控制（简化版）
                            executed = ExecuteSimplifiedZOC(troop, enemies);
                            break;

                        case TroopRole.Mage:
                            // Mage执行区域控制（简化版）
                            executed = ExecuteSimplifiedZOC(troop, enemies);
                            break;

                        default:
                            // 其他角色不执行ZOC战术
                            break;
                    }

                    if (executed)
                    {
                        successCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[ZOC示例] {faction.Name} ZOC战术执行完成: {successCount}/{faction.Troops.Count} 支部队成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC示例] ExecuteFactionZOCTactics 失败: {ex.Message}");
            }

            return successCount;
        }

        /// <summary>
        /// 示例3：战术AI集成ZOC系统
        /// </summary>
        /// <param name="tacticalAI">战术AI</param>
        /// <param name="battleContext">战斗上下文</param>
        /// <returns>ZOC战术计划</returns>
        public static ZOCTacticalPlan IntegrateZOCWithTacticalAI(TacticalAI tacticalAI, BattleContext battleContext)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ZOC示例] 开始集成ZOC系统到战术AI");

                // 创建ZOC战术计划
                var zocPlan = tacticalAI.CreateZOCTacticalPlan(battleContext.Enemies, battleContext.KeyTargets);

                // 执行ZOC任务分配
                foreach (var assignment in zocPlan.Assignments)
                {
                    if (assignment.Troop == null) continue;

                    System.Diagnostics.Debug.WriteLine($"[ZOC示例] 分配任务: {assignment.Troop.Leader?.Name} -> {assignment.TaskType} (优先级: {assignment.Priority})");

                    // 根据任务类型执行相应的ZOC行为
                    switch (assignment.TaskType)
                    {
                        case ZOCTaskType.Frontline:
                            assignment.Troop.ExecuteIntelligentZOCBlocking();
                            break;

                        case ZOCTaskType.Protection:
                            if (assignment.ProtectionTarget != null)
                            {
                                ExecuteProtectionZOC(assignment.Troop, assignment.ProtectionTarget, battleContext.Enemies);
                            }
                            break;

                        case ZOCTaskType.Flanking:
                            ExecuteFlankingZOC(assignment.Troop, battleContext.Enemies);
                            break;

                        case ZOCTaskType.AreaControl:
                            ExecuteAreaControlZOC(assignment.Troop, battleContext.Enemies);
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[ZOC示例] ZOC战术AI集成完成: {zocPlan.Assignments.Count} 个任务");
                return zocPlan;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC示例] IntegrateZOCWithTacticalAI 失败: {ex.Message}");
                return new ZOCTacticalPlan();
            }
        }

        #region 辅助方法

        /// <summary>
        /// 获取关键保护目标
        /// </summary>
        private static List<Troop> GetKeyProtectionTargets(Faction faction)
        {
            var targets = new List<Troop>();
            
            try
            {
                if (faction?.Troops == null) return targets;

                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop == null) continue;

                    // 主将优先保护
                    if (troop.Leader == faction.Leader)
                    {
                        targets.Insert(0, troop); // 插入到最前面
                        continue;
                    }

                    // 兵力少的部队需要保护
                    if (troop.Quantity < 1000)
                    {
                        targets.Add(troop);
                    }

                    // 重要兵种需要保护（如攻城器械等）
                    if (troop.Kind?.ID == 32) // 投石车
                    {
                        targets.Add(troop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC示例] GetKeyProtectionTargets 失败: {ex.Message}");
            }

            return targets;
        }

        /// <summary>
        /// 执行辅助ZOC战术
        /// </summary>
        private static bool ExecuteSupportZOC(Troop supportTroop, List<Troop> keyTargets, List<Troop> enemies)
        {
            try
            {
                if (keyTargets.Count == 0) return false;

                // 选择最需要保护的目标
                Troop mostVulnerable = keyTargets[0]; // 简化选择逻辑
                
                var moveRange = supportTroop.GetMoveRange();
                var bestPosition = ZOCEvaluator.GetBestBlockingPosition(supportTroop, moveRange, enemies, mostVulnerable);
                
                return supportTroop.MoveTo(bestPosition);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行简化ZOC战术（适用于DPS和Mage）
        /// </summary>
        private static bool ExecuteSimplifiedZOC(Troop troop, List<Troop> enemies)
        {
            try
            {
                var moveRange = troop.GetMoveRange();
                var bestPosition = ZOCEvaluator.GetBestBlockingPosition(troop, moveRange, enemies, null);
                
                // 只有当移动能显著改善位置时才移动
                int currentDistance = GetMinDistanceToEnemies(troop.Position, enemies);
                int newDistance = GetMinDistanceToEnemies(bestPosition, enemies);
                
                if (newDistance < currentDistance - 1) // 能够更接近敌人
                {
                    return troop.MoveTo(bestPosition);
                }

                return true; // 当前位置已经不错，不需要移动
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行保护ZOC战术
        /// </summary>
        private static bool ExecuteProtectionZOC(Troop protector, Troop target, List<Troop> enemies)
        {
            try
            {
                var moveRange = protector.GetMoveRange();
                var bestPosition = ZOCEvaluator.GetBestBlockingPosition(protector, moveRange, enemies, target);
                
                return protector.MoveTo(bestPosition);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行侧翼ZOC战术
        /// </summary>
        private static bool ExecuteFlankingZOC(Troop flanker, List<Troop> enemies)
        {
            try
            {
                // 侧翼部队尝试从侧面控制敌军
                var moveRange = flanker.GetMoveRange();
                Point bestFlankPosition = flanker.Position;
                float bestScore = 0f;

                foreach (var position in moveRange)
                {
                    float score = 0f;
                    
                    // 计算侧翼控制效果
                    foreach (var enemy in enemies)
                    {
                        if (enemy == null) continue;
                        
                        int distance = Math.Abs(position.X - enemy.Position.X) + Math.Abs(position.Y - enemy.Position.Y);
                        if (distance == 1) // 贴脸控制
                        {
                            score += 30f;
                        }
                        else if (distance == 2) // 威胁范围
                        {
                            score += 10f;
                        }
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFlankPosition = position;
                    }
                }

                return flanker.MoveTo(bestFlankPosition);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行区域控制ZOC战术
        /// </summary>
        private static bool ExecuteAreaControlZOC(Troop controller, List<Troop> enemies)
        {
            try
            {
                // 区域控制部队尝试控制最多的敌军
                var moveRange = controller.GetMoveRange();
                Point bestControlPosition = controller.Position;
                int maxControlled = 0;

                foreach (var position in moveRange)
                {
                    var controlledEnemies = ZOCEvaluator.GetEnemiesInZOC(position, enemies, 2);
                    if (controlledEnemies.Count > maxControlled)
                    {
                        maxControlled = controlledEnemies.Count;
                        bestControlPosition = position;
                    }
                }

                return controller.MoveTo(bestControlPosition);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 计算到敌军的最小距离
        /// </summary>
        private static int GetMinDistanceToEnemies(Point position, List<Troop> enemies)
        {
            int minDistance = int.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                
                int distance = Math.Abs(position.X - enemy.Position.X) + Math.Abs(position.Y - enemy.Position.Y);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance == int.MaxValue ? 0 : minDistance;
        }

        #endregion
    }

    /// <summary>
    /// 战斗上下文信息
    /// </summary>
    public class BattleContext
    {
        public List<Troop> Enemies { get; set; } = new List<Troop>();
        public List<Troop> KeyTargets { get; set; } = new List<Troop>();
        public Architecture BattleLocation { get; set; }
        public BattlePhase CurrentPhase { get; set; } = BattlePhase.Opening;
    }
}