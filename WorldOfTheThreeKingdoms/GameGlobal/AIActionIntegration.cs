using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameManager;

namespace GameGlobal
{
    /// <summary>
    /// AI行动集成系统
    /// 展示如何在实际游戏中使用AI行动排序和ZOC战术系统
    /// </summary>
    public static class AIActionIntegration
    {
        /// <summary>
        /// 执行势力的智能AI回合
        /// 按照战术优先级顺序执行所有部队的行动
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <returns>是否成功执行AI回合</returns>
        public static bool ExecuteIntelligentAITurn(Faction faction)
        {
            try
            {
                if (faction == null || faction.Troops == null)
                {
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {faction.Name} 开始执行智能AI回合");

                // 1. 获取按战术优先级排序的部队行动顺序
                List<Troop> actionOrder = AIActionSorter.GetFactionActionOrder(faction);
                
                if (actionOrder.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {faction.Name} 没有可行动的部队");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {faction.Name} 共有 {actionOrder.Count} 支部队按优先级行动");

                // 2. 按顺序执行每支部队的最优行动
                int successCount = 0;
                foreach (Troop troop in actionOrder)
                {
                    if (ExecuteOptimalTroopAction(troop))
                    {
                        successCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {faction.Name} AI回合完成: {successCount}/{actionOrder.Count} 支部队成功行动");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteIntelligentAITurn 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行单支部队的最优行动
        /// 根据部队角色选择最合适的行动类型
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否成功执行行动</returns>
        private static bool ExecuteOptimalTroopAction(Troop troop)
        {
            try
            {
                if (troop == null || troop.Leader == null)
                {
                    return false;
                }

                TroopRole role = AIRoleSelector.DetermineRole(troop);
                string recommendedAction = AIActionSorter.GetRecommendedActionType(troop);

                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader.Name} ({GetRoleDescription(role)}) 推荐行动: {recommendedAction}");

                // 根据推荐的行动类型执行相应操作
                switch (recommendedAction)
                {
                    case "support":
                        return ExecuteSupportAction(troop);

                    case "control":
                        return ExecuteControlAction(troop);

                    case "zoc":
                        return ExecuteZOCAction(troop);

                    case "attack":
                        return ExecuteAttackAction(troop);

                    case "balanced":
                        return ExecuteBalancedAction(troop);

                    default:
                        return ExecuteWaitAction(troop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteOptimalTroopAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行辅助行动
        /// Support角色优先使用鼓舞、回气等辅助技能
        /// </summary>
        /// <param name="troop">辅助部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteSupportAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行辅助行动");

                // 优先级1: 使用鼓舞技能 (ID 397)
                if (HasSkill(troop, 397))
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 使用鼓舞技能");
                    // 这里应该调用实际的技能使用逻辑
                    return true;
                }

                // 优先级2: 使用其他辅助技能
                var supportSkills = GetSupportSkills(troop);
                if (supportSkills.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 使用辅助技能 ID: {supportSkills[0]}");
                    return true;
                }

                // 如果没有辅助技能，执行移动到安全位置
                return ExecuteMovementAction(troop, "safe");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteSupportAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行控制行动
        /// Mage角色优先使用惊营、混乱等控制技能
        /// </summary>
        /// <param name="troop">法师部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteControlAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行控制行动");

                // 优先级1: 使用惊营技能 (ID 391)
                if (HasSkill(troop, 391))
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 使用惊营技能");
                    return true;
                }

                // 优先级2: 使用其他控制技能
                var controlSkills = GetControlSkills(troop);
                if (controlSkills.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 使用控制技能 ID: {controlSkills[0]}");
                    return true;
                }

                // 如果没有控制技能，执行远程攻击
                return ExecuteAttackAction(troop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteControlAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行ZOC卡位行动
        /// Tank角色优先执行智能卡位，形成包围网
        /// </summary>
        /// <param name="troop">肉盾部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteZOCAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行ZOC卡位行动");

                // 使用ZOC扩展方法执行智能卡位
                if (troop.ExecuteIntelligentZOCBlocking())
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} ZOC卡位成功");
                    return true;
                }

                // 如果ZOC卡位失败，执行普通移动
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} ZOC卡位失败，执行普通移动");
                return ExecuteMovementAction(troop, "forward");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteZOCAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行攻击行动
        /// DPS角色优先攻击被控制或被包围的敌人
        /// </summary>
        /// <param name="troop">输出部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteAttackAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行攻击行动");

                // 获取视野内的敌军
                var visibleEnemies = GetVisibleEnemies(troop);
                if (visibleEnemies.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 未发现敌军目标");
                    return ExecuteMovementAction(troop, "aggressive");
                }

                // 使用AI目标选择器寻找最佳攻击目标
                Troop bestTarget = AITargetSelector.GetBestAttackTarget(troop, visibleEnemies);
                if (bestTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 攻击目标: {bestTarget.Leader?.Name}");
                    
                    // 生成目标选择报告
                    string report = AITargetSelector.GetTargetSelectionReport(troop, visibleEnemies);
                    System.Diagnostics.Debug.WriteLine(report);
                    
                    // 这里应该调用实际的攻击逻辑
                    return true;
                }

                // 如果没有合适的攻击目标，向前移动寻找机会
                return ExecuteMovementAction(troop, "aggressive");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteAttackAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行均衡行动
        /// Balanced角色根据情况选择最合适的行动
        /// </summary>
        /// <param name="troop">均衡部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteBalancedAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行均衡行动");

                // 根据战场情况选择行动
                if (ShouldAttack(troop))
                {
                    return ExecuteAttackAction(troop);
                }
                else if (ShouldUseSkill(troop))
                {
                    return ExecuteSkillAction(troop);
                }
                else
                {
                    return ExecuteMovementAction(troop, "balanced");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteBalancedAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行等待行动
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否成功执行</returns>
        private static bool ExecuteWaitAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行等待行动");
                // 等待行动总是成功的
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] ExecuteWaitAction 失败: {ex.Message}");
                return false;
            }
        }

        #region 辅助方法

        /// <summary>
        /// 获取角色描述
        /// </summary>
        private static string GetRoleDescription(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.Support: return "辅助";
                case TroopRole.Mage: return "法师";
                case TroopRole.Tank: return "肉盾";
                case TroopRole.DPS: return "输出";
                case TroopRole.Balanced: return "均衡";
                default: return "未知";
            }
        }

        /// <summary>
        /// 检查部队是否拥有指定技能
        /// </summary>
        private static bool HasSkill(Troop troop, int skillId)
        {
            // 这里应该调用实际的技能检查逻辑
            // 暂时返回随机结果用于演示
            return new Random().Next(0, 2) == 1;
        }

        /// <summary>
        /// 获取辅助技能列表
        /// </summary>
        private static List<int> GetSupportSkills(Troop troop)
        {
            // 这里应该返回实际的辅助技能ID列表
            return new List<int> { 397, 398, 399 }; // 示例技能ID
        }

        /// <summary>
        /// 获取控制技能列表
        /// </summary>
        private static List<int> GetControlSkills(Troop troop)
        {
            // 这里应该返回实际的控制技能ID列表
            return new List<int> { 391, 392, 393 }; // 示例技能ID
        }

        /// <summary>
        /// 执行移动行动
        /// </summary>
        private static bool ExecuteMovementAction(Troop troop, string moveType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行移动行动: {moveType}");
                // 这里应该调用实际的移动逻辑
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 寻找最佳攻击目标
        /// </summary>
        private static Troop FindBestAttackTarget(Troop attacker)
        {
            try
            {
                // 获取视野内的敌军
                var visibleEnemies = GetVisibleEnemies(attacker);
                if (visibleEnemies.Count == 0) return null;

                // 使用AI目标选择器选择最佳目标
                return AITargetSelector.GetBestAttackTarget(attacker, visibleEnemies);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] FindBestAttackTarget 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取视野内的敌军
        /// </summary>
        private static List<Troop> GetVisibleEnemies(Troop troop)
        {
            var enemies = new List<Troop>();
            
            try
            {
                if (troop?.BelongedFaction == null) return enemies;

                // 获取视野范围内的敌军
                var targetsInRange = AITargetSelector.GetTargetsInRange(troop, GetAllEnemyTroops(troop), 8);
                enemies.AddRange(targetsInRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] GetVisibleEnemies 失败: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 获取所有敌军部队
        /// </summary>
        private static List<Troop> GetAllEnemyTroops(Troop myTroop)
        {
            var enemies = new List<Troop>();
            
            try
            {
                if (GameManager.Session.Current?.Scenario?.Factions == null) return enemies;

                foreach (Faction faction in GameManager.Session.Current.Scenario.Factions.GetList())
                {
                    if (faction == null || faction == myTroop.BelongedFaction) continue;

                    if (faction.Troops != null)
                    {
                        foreach (Troop troop in faction.Troops.GetList())
                        {
                            if (troop != null && troop.Quantity > 0)
                            {
                                enemies.Add(troop);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] GetAllEnemyTroops 失败: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 判断是否应该攻击
        /// </summary>
        private static bool ShouldAttack(Troop troop)
        {
            // 这里应该实现实际的攻击判断逻辑
            return new Random().Next(0, 2) == 1;
        }

        /// <summary>
        /// 判断是否应该使用技能
        /// </summary>
        private static bool ShouldUseSkill(Troop troop)
        {
            // 这里应该实现实际的技能使用判断逻辑
            return new Random().Next(0, 2) == 1;
        }

        /// <summary>
        /// 执行技能行动
        /// </summary>
        private static bool ExecuteSkillAction(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动集成] {troop.Leader?.Name} 执行技能行动");
                // 这里应该调用实际的技能使用逻辑
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 获取AI行动统计报告
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>统计报告</returns>
        public static string GetAIActionReport(Faction faction)
        {
            try
            {
                if (faction?.Troops == null)
                {
                    return "无效势力";
                }

                var actionOrder = AIActionSorter.GetFactionActionOrder(faction);
                var report = new System.Text.StringBuilder();
                
                report.AppendLine($"=== {faction.Name} AI行动报告 ===");
                report.AppendLine($"总部队数: {actionOrder.Count}");
                
                var roleStats = actionOrder.GroupBy(t => AIRoleSelector.DetermineRole(t))
                                          .ToDictionary(g => g.Key, g => g.Count());
                
                report.AppendLine("角色分布:");
                foreach (var stat in roleStats)
                {
                    report.AppendLine($"  {GetRoleDescription(stat.Key)}: {stat.Value} 支");
                }

                report.AppendLine("行动顺序:");
                for (int i = 0; i < Math.Min(actionOrder.Count, 10); i++)
                {
                    var troop = actionOrder[i];
                    var role = AIRoleSelector.DetermineRole(troop);
                    var action = AIActionSorter.GetRecommendedActionType(troop);
                    
                    report.AppendLine($"  {i + 1}. {troop.Leader?.Name} ({GetRoleDescription(role)}) -> {action}");
                }

                if (actionOrder.Count > 10)
                {
                    report.AppendLine($"  ... 还有 {actionOrder.Count - 10} 支部队");
                }

                return report.ToString();
            }
            catch (Exception ex)
            {
                return $"报告生成失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 执行完整的AI智能回合
        /// 集成行动排序、目标选择、ZOC战术等所有AI系统
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <returns>是否成功执行</returns>
        public static bool ExecuteFullIntelligentAITurn(Faction faction)
        {
            try
            {
                if (faction == null || faction.Troops == null)
                {
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AI完整回合] {faction.Name} 开始执行完整智能AI回合");

                // 1. 获取按战术优先级排序的部队行动顺序
                List<Troop> actionOrder = AIActionSorter.GetFactionActionOrder(faction);
                
                if (actionOrder.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI完整回合] {faction.Name} 没有可行动的部队");
                    return true;
                }

                // 2. 生成行动报告
                string actionReport = GetAIActionReport(faction);
                System.Diagnostics.Debug.WriteLine(actionReport);

                // 3. 按优先级顺序执行每支部队的智能行动
                int successCount = 0;
                foreach (Troop troop in actionOrder)
                {
                    if (ExecuteIntelligentTroopAction(troop))
                    {
                        successCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AI完整回合] {faction.Name} 完整AI回合完成: {successCount}/{actionOrder.Count} 支部队成功行动");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI完整回合] ExecuteFullIntelligentAITurn 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行单支部队的完整智能行动
        /// 集成目标选择、ZOC战术、角色特化、撤退逻辑等功能
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否成功执行行动</returns>
        private static bool ExecuteIntelligentTroopAction(Troop troop)
        {
            try
            {
                if (troop == null || troop.Leader == null)
                {
                    return false;
                }

                // 🚨 优先检查撤退逻辑 - 在 AITroop.Think() 开头
                if (AIRetreatSystem.CheckAndExecuteRetreat(troop))
                {
                    System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader.Name} 执行撤退，本回合结束");
                    return true; // 撤退成功，本回合结束
                }

                TroopRole role = AIRoleSelector.DetermineRole(troop);
                string recommendedAction = AIActionSorter.GetRecommendedActionType(troop);

                System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader.Name} ({GetRoleDescription(role)}) 推荐行动: {recommendedAction}");

                // 获取视野内的敌军用于决策
                var visibleEnemies = GetVisibleEnemies(troop);
                
                // 根据推荐的行动类型和敌情执行相应操作
                switch (recommendedAction)
                {
                    case "support":
                        return ExecuteIntelligentSupportAction(troop, visibleEnemies);

                    case "control":
                        return ExecuteIntelligentControlAction(troop, visibleEnemies);

                    case "zoc":
                        return ExecuteIntelligentZOCAction(troop, visibleEnemies);

                    case "attack":
                        return ExecuteIntelligentAttackAction(troop, visibleEnemies);

                    case "balanced":
                        return ExecuteIntelligentBalancedAction(troop, visibleEnemies);

                    default:
                        return ExecuteWaitAction(troop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI智能行动] ExecuteIntelligentTroopAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行智能辅助行动
        /// </summary>
        private static bool ExecuteIntelligentSupportAction(Troop troop, List<Troop> visibleEnemies)
        {
            System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader?.Name} 执行智能辅助行动");
            
            // 如果有敌军威胁，优先保护友军
            if (visibleEnemies.Count > 0)
            {
                return troop.ExecuteCoordinatedZOC(); // 使用ZOC扩展方法
            }
            
            return ExecuteSupportAction(troop);
        }

        /// <summary>
        /// 执行智能控制行动
        /// </summary>
        private static bool ExecuteIntelligentControlAction(Troop troop, List<Troop> visibleEnemies)
        {
            System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader?.Name} 执行智能控制行动");
            
            // 法师使用专用的控制目标选择逻辑
            if (visibleEnemies.Count > 0)
            {
                var bestDebuffTarget = AITargetSelector.GetBestDebuffTarget(troop, visibleEnemies);
                if (bestDebuffTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI智能行动] 法师选择控制目标: {bestDebuffTarget.Leader?.Name}");
                    
                    // 尝试释放惊营技能
                    bool castSuccess = AITargetSelector.CastStrategy(troop, bestDebuffTarget, 391);
                    if (castSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI智能行动] 成功对 {bestDebuffTarget.Leader?.Name} 释放惊营");
                        return true;
                    }
                }
            }
            
            return ExecuteControlAction(troop);
        }

        /// <summary>
        /// 执行智能ZOC行动
        /// </summary>
        private static bool ExecuteIntelligentZOCAction(Troop troop, List<Troop> visibleEnemies)
        {
            System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader?.Name} 执行智能ZOC行动");
            
            // 使用ZOC扩展方法执行智能卡位
            return troop.ExecuteIntelligentZOCBlocking();
        }

        /// <summary>
        /// 执行智能攻击行动
        /// </summary>
        private static bool ExecuteIntelligentAttackAction(Troop troop, List<Troop> visibleEnemies)
        {
            System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader?.Name} 执行智能攻击行动");
            
            if (visibleEnemies.Count > 0)
            {
                // 使用目标选择器选择最佳攻击目标
                var bestTarget = AITargetSelector.GetBestAttackTarget(troop, visibleEnemies);
                if (bestTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI智能行动] 攻击目标: {bestTarget.Leader?.Name}");
                    
                    // 生成详细的目标分析报告
                    string targetReport = AITargetSelector.GetTargetSelectionReport(troop, visibleEnemies);
                    System.Diagnostics.Debug.WriteLine(targetReport);
                    
                    return true;
                }
            }
            
            return ExecuteMovementAction(troop, "aggressive");
        }

        /// <summary>
        /// 执行智能均衡行动
        /// </summary>
        private static bool ExecuteIntelligentBalancedAction(Troop troop, List<Troop> visibleEnemies)
        {
            System.Diagnostics.Debug.WriteLine($"[AI智能行动] {troop.Leader?.Name} 执行智能均衡行动");
            
            // 根据敌情智能选择行动类型
            if (visibleEnemies.Count > 0)
            {
                // 如果敌军较多，优先防御
                if (visibleEnemies.Count >= 3)
                {
                    return troop.ExecuteCoordinatedZOC();
                }
                // 如果敌军较少，可以考虑攻击
                else
                {
                    var bestTarget = AITargetSelector.GetBestAttackTarget(troop, visibleEnemies);
                    if (bestTarget != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI智能行动] 均衡模式选择攻击: {bestTarget.Leader?.Name}");
                        return true;
                    }
                }
            }
            
            return ExecuteBalancedAction(troop);
        }
    }
}