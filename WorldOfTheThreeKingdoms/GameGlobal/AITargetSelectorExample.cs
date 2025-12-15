using System;
using System.Collections.Generic;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// AI目标选择器使用示例
    /// 展示如何在实际战斗中使用智能目标选择系统
    /// </summary>
    public static class AITargetSelectorExample
    {
        /// <summary>
        /// 示例1：基础目标选择
        /// </summary>
        public static void BasicTargetSelectionExample()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== AI目标选择器基础示例 ===");

                // 假设我们有一个攻击者
                // Troop attacker = GetMyTroop();
                
                // 获取视野内的敌军
                // List<Troop> enemies = GetVisibleEnemies(attacker);
                
                // 使用AI目标选择器选择最佳目标
                // Troop bestTarget = AITargetSelector.GetBestAttackTarget(attacker, enemies);
                
                // if (bestTarget != null)
                // {
                //     System.Diagnostics.Debug.WriteLine($"选择攻击目标: {bestTarget.Leader?.Name}");
                //     // 执行攻击
                //     ExecuteAttack(attacker, bestTarget);
                // }
                
                System.Diagnostics.Debug.WriteLine("基础目标选择示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"基础目标选择示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例2：多部队协同目标选择
        /// </summary>
        public static void CoordinatedTargetSelectionExample(List<Troop> myTroops)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 多部队协同目标选择示例 ===");

                if (myTroops == null || myTroops.Count == 0) return;

                // 获取所有敌军
                var allEnemies = GetAllEnemyTroops(myTroops[0]);
                
                // 为每个部队选择最佳目标
                var targetAssignments = new Dictionary<Troop, Troop>();
                
                foreach (var troop in myTroops)
                {
                    if (troop == null) continue;

                    // 获取该部队范围内的敌军
                    var targetsInRange = AITargetSelector.GetTargetsInRange(troop, allEnemies, 5);
                    
                    if (targetsInRange.Count > 0)
                    {
                        var bestTarget = AITargetSelector.GetBestAttackTarget(troop, targetsInRange);
                        if (bestTarget != null)
                        {
                            targetAssignments[troop] = bestTarget;
                            System.Diagnostics.Debug.WriteLine($"{troop.Leader?.Name} 选择攻击 {bestTarget.Leader?.Name}");
                        }
                    }
                }

                // 执行协同攻击
                ExecuteCoordinatedAttacks(targetAssignments);
                
                System.Diagnostics.Debug.WriteLine("多部队协同目标选择示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"多部队协同目标选择示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例3：基于角色的目标选择策略
        /// </summary>
        public static void RoleBasedTargetSelectionExample(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 基于角色的目标选择示例 ===");

                if (troop == null) return;

                // 获取部队角色
                TroopRole role = AIRoleSelector.DetermineRole(troop);
                
                // 获取所有敌军
                var allEnemies = GetAllEnemyTroops(troop);
                var targetsInRange = AITargetSelector.GetTargetsInRange(troop, allEnemies, 8);

                Troop selectedTarget = null;

                switch (role)
                {
                    case TroopRole.DPS:
                        // DPS优先攻击最脆弱的目标（残血收割）
                        selectedTarget = AITargetSelector.GetWeakestTarget(targetsInRange);
                        System.Diagnostics.Debug.WriteLine($"DPS部队 {troop.Leader?.Name} 选择攻击最脆弱目标");
                        break;

                    case TroopRole.Tank:
                        // Tank优先攻击最有价值的目标（吸引火力）
                        selectedTarget = AITargetSelector.GetMostValuableTarget(targetsInRange);
                        System.Diagnostics.Debug.WriteLine($"Tank部队 {troop.Leader?.Name} 选择攻击最有价值目标");
                        break;

                    case TroopRole.Mage:
                        // Mage使用标准目标选择（考虑兵种克制）
                        selectedTarget = AITargetSelector.GetBestAttackTarget(troop, targetsInRange);
                        System.Diagnostics.Debug.WriteLine($"Mage部队 {troop.Leader?.Name} 使用标准目标选择");
                        break;

                    case TroopRole.Support:
                        // Support通常不主动攻击，但如果必须攻击则选择最安全的目标
                        var weakTargets = new List<Troop>();
                        foreach (var enemy in targetsInRange)
                        {
                            if (enemy != null && enemy.Quantity < 500) // 兵力较少的目标
                            {
                                weakTargets.Add(enemy);
                            }
                        }
                        selectedTarget = AITargetSelector.GetBestAttackTarget(troop, weakTargets);
                        System.Diagnostics.Debug.WriteLine($"Support部队 {troop.Leader?.Name} 选择安全目标");
                        break;

                    default:
                        selectedTarget = AITargetSelector.GetBestAttackTarget(troop, targetsInRange);
                        break;
                }

                if (selectedTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"最终选择目标: {selectedTarget.Leader?.Name}");
                    // ExecuteAttack(troop, selectedTarget);
                }

                System.Diagnostics.Debug.WriteLine("基于角色的目标选择示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"基于角色的目标选择示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例4：目标选择统计分析
        /// </summary>
        public static void TargetSelectionAnalysisExample(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 目标选择统计分析示例 ===");

                if (troop == null) return;

                var allEnemies = GetAllEnemyTroops(troop);
                var targetsInRange = AITargetSelector.GetTargetsInRange(troop, allEnemies, 6);

                // 生成详细的目标选择报告
                string report = AITargetSelector.GetTargetSelectionReport(troop, targetsInRange);
                System.Diagnostics.Debug.WriteLine(report);

                // 分析不同类型的目标
                var rangedTargets = new List<Troop>();
                var meleeTargets = new List<Troop>();
                var cavalryTargets = new List<Troop>();

                foreach (var target in targetsInRange)
                {
                    if (target?.Army?.Kind == null) continue;

                    int kindID = target.Army.Kind.ID;
                    if (kindID == 1 || kindID == 15 || kindID == 32 || kindID == 301)
                    {
                        rangedTargets.Add(target);
                    }
                    else if (kindID == 2 || kindID == 3 || kindID == 16 || kindID == 17)
                    {
                        cavalryTargets.Add(target);
                    }
                    else
                    {
                        meleeTargets.Add(target);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"目标分析: 远程{rangedTargets.Count}, 骑兵{cavalryTargets.Count}, 近战{meleeTargets.Count}");

                // 针对不同类型目标的最佳选择
                if (rangedTargets.Count > 0)
                {
                    var bestRanged = AITargetSelector.GetBestAttackTarget(troop, rangedTargets);
                    System.Diagnostics.Debug.WriteLine($"最佳远程目标: {bestRanged?.Leader?.Name}");
                }

                if (cavalryTargets.Count > 0)
                {
                    var bestCavalry = AITargetSelector.GetBestAttackTarget(troop, cavalryTargets);
                    System.Diagnostics.Debug.WriteLine($"最佳骑兵目标: {bestCavalry?.Leader?.Name}");
                }

                if (meleeTargets.Count > 0)
                {
                    var bestMelee = AITargetSelector.GetBestAttackTarget(troop, meleeTargets);
                    System.Diagnostics.Debug.WriteLine($"最佳近战目标: {bestMelee?.Leader?.Name}");
                }

                System.Diagnostics.Debug.WriteLine("目标选择统计分析示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"目标选择统计分析示例失败: {ex.Message}");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 获取所有敌军部队
        /// </summary>
        private static List<Troop> GetAllEnemyTroops(Troop myTroop)
        {
            var enemies = new List<Troop>();
            
            try
            {
                if (Session.Current?.Scenario?.Factions == null) return enemies;

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
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
                System.Diagnostics.Debug.WriteLine($"GetAllEnemyTroops 失败: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 执行协同攻击
        /// </summary>
        private static void ExecuteCoordinatedAttacks(Dictionary<Troop, Troop> targetAssignments)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始执行协同攻击:");

                foreach (var assignment in targetAssignments)
                {
                    var attacker = assignment.Key;
                    var target = assignment.Value;

                    if (attacker != null && target != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {attacker.Leader?.Name} 攻击 {target.Leader?.Name}");
                        // 这里应该调用实际的攻击逻辑
                        // ExecuteAttack(attacker, target);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteCoordinatedAttacks 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行攻击（占位符方法）
        /// </summary>
        private static void ExecuteAttack(Troop attacker, Troop target)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"执行攻击: {attacker.Leader?.Name} -> {target.Leader?.Name}");
                // 这里应该调用游戏的实际攻击逻辑
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteAttack 失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始运行AI目标选择器所有示例 ===");

                // 示例1：基础目标选择
                BasicTargetSelectionExample();

                // 如果有可用的部队数据，运行其他示例
                if (Session.Current?.Scenario?.CurrentPlayer?.Troops != null)
                {
                    var myTroops = new List<Troop>();
                    foreach (Troop troop in Session.Current.Scenario.CurrentPlayer.Troops.GetList())
                    {
                        if (troop != null && troop.Quantity > 0)
                        {
                            myTroops.Add(troop);
                        }
                    }

                    if (myTroops.Count > 0)
                    {
                        // 示例2：多部队协同目标选择
                        CoordinatedTargetSelectionExample(myTroops);

                        // 示例3：基于角色的目标选择策略
                        RoleBasedTargetSelectionExample(myTroops[0]);

                        // 示例4：目标选择统计分析
                        TargetSelectionAnalysisExample(myTroops[0]);
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== AI目标选择器所有示例运行完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunAllExamples 失败: {ex.Message}");
            }
        }
    }
}