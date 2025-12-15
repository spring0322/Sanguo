using System;
using System.Collections.Generic;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// AI撤退系统使用示例
    /// 展示如何使用智能撤退逻辑
    /// </summary>
    public static class AIRetreatExample
    {
        /// <summary>
        /// 撤退系统基础示例
        /// </summary>
        public static void BasicRetreatExample(Troop troop)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== AI撤退系统基础示例 ===");

                if (troop == null) return;

                // 生成撤退状态报告
                string statusReport = AIRetreatSystem.GetRetreatStatusReport(troop);
                System.Diagnostics.Debug.WriteLine(statusReport);

                // 检查并执行撤退
                bool shouldRetreat = AIRetreatSystem.CheckAndExecuteRetreat(troop);
                
                if (shouldRetreat)
                {
                    System.Diagnostics.Debug.WriteLine($"{troop.Leader?.Name} 执行撤退，本回合结束");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{troop.Leader?.Name} 继续战斗");
                }

                System.Diagnostics.Debug.WriteLine("撤退系统基础示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"撤退系统基础示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 势力撤退分析示例
        /// </summary>
        public static void FactionRetreatAnalysisExample(Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 势力撤退分析示例 ===");

                if (faction?.Troops == null) return;

                var retreatingTroops = new List<Troop>();
                var fightingTroops = new List<Troop>();

                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop == null) continue;

                    float troopRatio = (float)troop.Quantity / troop.MaxQuantity;
                    if (troopRatio < 0.3f)
                    {
                        retreatingTroops.Add(troop);
                    }
                    else
                    {
                        fightingTroops.Add(troop);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"势力 {faction.Name} 撤退分析:");
                System.Diagnostics.Debug.WriteLine($"  需要撤退的部队: {retreatingTroops.Count}");
                System.Diagnostics.Debug.WriteLine($"  继续战斗的部队: {fightingTroops.Count}");

                // 为需要撤退的部队生成详细报告
                foreach (var troop in retreatingTroops)
                {
                    string report = AIRetreatSystem.GetRetreatStatusReport(troop);
                    System.Diagnostics.Debug.WriteLine(report);
                }

                System.Diagnostics.Debug.WriteLine("势力撤退分析示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"势力撤退分析示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 集成AI回合示例
        /// 展示撤退系统如何与其他AI系统协同工作
        /// </summary>
        public static void IntegratedAITurnExample(Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 集成AI回合示例 ===");

                if (faction?.Troops == null) return;

                // 获取按优先级排序的部队
                var actionOrder = AIActionSorter.GetFactionActionOrder(faction);
                
                System.Diagnostics.Debug.WriteLine($"{faction.Name} 开始执行集成AI回合");

                int retreatedCount = 0;
                int actionCount = 0;

                foreach (var troop in actionOrder)
                {
                    if (troop == null) continue;

                    System.Diagnostics.Debug.WriteLine($"\n--- {troop.Leader?.Name} 的回合 ---");

                    // 优先检查撤退
                    if (AIRetreatSystem.CheckAndExecuteRetreat(troop))
                    {
                        retreatedCount++;
                        System.Diagnostics.Debug.WriteLine($"{troop.Leader?.Name} 执行撤退");
                        continue; // 撤退后本回合结束
                    }

                    // 如果不需要撤退，执行正常AI行动
                    TroopRole role = AIRoleSelector.DetermineRole(troop);
                    string action = AIActionSorter.GetRecommendedActionType(troop);
                    
                    System.Diagnostics.Debug.WriteLine($"{troop.Leader?.Name} ({GetRoleDescription(role)}) 执行: {action}");
                    actionCount++;

                    // 这里可以继续执行其他AI逻辑
                    // 如目标选择、ZOC战术等
                }

                System.Diagnostics.Debug.WriteLine($"\n{faction.Name} AI回合完成:");
                System.Diagnostics.Debug.WriteLine($"  撤退部队: {retreatedCount}");
                System.Diagnostics.Debug.WriteLine($"  行动部队: {actionCount}");

                System.Diagnostics.Debug.WriteLine("集成AI回合示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"集成AI回合示例失败: {ex.Message}");
            }
        }

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
        /// 运行所有撤退系统示例
        /// </summary>
        public static void RunAllRetreatExamples()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始运行AI撤退系统所有示例 ===");

                // 如果有可用的游戏数据，运行示例
                if (Session.Current?.Scenario?.CurrentPlayer != null)
                {
                    var faction = Session.Current.Scenario.CurrentPlayer;
                    
                    // 势力撤退分析
                    FactionRetreatAnalysisExample(faction);

                    // 集成AI回合示例
                    IntegratedAITurnExample(faction);

                    // 如果有具体部队，运行基础示例
                    if (faction.Troops != null)
                    {
                        foreach (Troop troop in faction.Troops.GetList())
                        {
                            if (troop != null)
                            {
                                BasicRetreatExample(troop);
                                break; // 只演示第一个部队
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== AI撤退系统所有示例运行完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunAllRetreatExamples 失败: {ex.Message}");
            }
        }
    }
}