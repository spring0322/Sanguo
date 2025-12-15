using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.AI;
using GameManager;

namespace GameObjects.AI.Examples
{
    /// <summary>
    /// AI系统集成使用示例
    /// 展示如何在现有代码中使用新的AI系统
    /// </summary>
    public static class AISystemIntegrationExamples
    {
        /// <summary>
        /// 示例1: 在Faction回合中自动使用AI系统（已集成）
        /// 这是推荐的使用方式，系统会自动在每个AI势力的回合中执行
        /// </summary>
        public static void Example1_AutomaticIntegration()
        {
            // 🎯 无需手动调用！系统已自动集成到 Faction.AI() 方法中
            // 
            // 在 Faction.AI() 方法中，系统会自动执行：
            // 1. 原有的AI逻辑（外交、技术、建筑等）
            // 2. AILegions() - 原有的军团AI
            // 3. AISmartTroops() - 🆕 新的智能部队管理
            //
            // 整个过程对玩家和现有代码完全透明

            Console.WriteLine("AI系统已自动集成到游戏回合逻辑中");
            Console.WriteLine("每个AI势力的回合都会自动执行智能部队管理");
        }

        /// <summary>
        /// 示例2: 手动为特定势力执行AI回合（高级用法）
        /// </summary>
        public static void Example2_ManualFactionTurn()
        {
            try
            {
                // 获取目标势力（例如：第一个AI势力）
                Faction targetFaction = null;
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (!Session.Current.Scenario.IsPlayer(faction))
                    {
                        targetFaction = faction;
                        break;
                    }
                }

                if (targetFaction != null)
                {
                    Console.WriteLine($"手动执行势力 {targetFaction.Name} 的AI回合");
                    
                    // 🎯 手动调用完整的AI回合系统
                    AIFactionTurnIntegration.RunFactionTurn(targetFaction);
                    
                    Console.WriteLine("AI回合执行完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"手动执行AI回合时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例3: 为单个部队执行AI决策
        /// </summary>
        public static void Example3_SingleTroopAI()
        {
            try
            {
                // 获取第一个AI部队
                Troop targetTroop = null;
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (!Session.Current.Scenario.IsPlayer(faction) && faction.Troops.Count > 0)
                    {
                        targetTroop = faction.Troops.GetList()[0];
                        break;
                    }
                }

                if (targetTroop != null)
                {
                    Console.WriteLine($"为部队 {targetTroop.ID} 执行AI决策");

                    // 获取所有部队用于分析
                    var allTroops = new List<Troop>();
                    foreach (Faction f in Session.Current.Scenario.Factions.GetList())
                    {
                        allTroops.AddRange(f.Troops.GetList());
                    }

                    // 🎯 使用集成示例执行单个部队的AI决策
                    AIMapNavigationIntegrationExample.ExecuteAITacticalDecision(targetTroop, allTroops);
                    
                    Console.WriteLine("单个部队AI决策完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"单个部队AI决策时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例4: 批量执行优化的AI决策
        /// </summary>
        public static void Example4_BatchOptimizedAI()
        {
            try
            {
                // 获取第一个AI势力的所有部队
                List<Troop> aiTroops = null;
                List<Troop> allTroops = new List<Troop>();

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    allTroops.AddRange(faction.Troops.GetList());
                    
                    if (!Session.Current.Scenario.IsPlayer(faction) && aiTroops == null)
                    {
                        aiTroops = faction.Troops.GetList();
                    }
                }

                if (aiTroops != null && aiTroops.Count > 0)
                {
                    Console.WriteLine($"批量执行 {aiTroops.Count} 个AI部队的优化决策");

                    // 🎯 执行优化的AI决策（使用行动序列器）
                    AIMapNavigationIntegrationExample.ExecuteOptimizedAIDecisions(aiTroops, allTroops);
                    
                    Console.WriteLine("批量AI决策执行完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量AI决策时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例5: 分析和显示AI系统状态
        /// </summary>
        public static void Example5_AnalyzeAIStatus()
        {
            try
            {
                Console.WriteLine("=== AI系统状态分析 ===");

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (!Session.Current.Scenario.IsPlayer(faction))
                    {
                        Console.WriteLine($"\n势力: {faction.Name}");
                        Console.WriteLine($"部队数量: {faction.Troops.Count}");

                        if (faction.Troops.Count > 0)
                        {
                            // 🎯 分析战术组合
                            string analysis = AIActionSequencer.AnalyzeTacticalComposition(faction.Troops.GetList());
                            Console.WriteLine(analysis);

                            // 显示各角色部队数量
                            var roleCount = new Dictionary<AIRole, int>();
                            foreach (Troop troop in faction.Troops.GetList())
                            {
                                if (troop.CurrentRole == AIRole.None)
                                {
                                    troop.CurrentRole = AIRoleSelector.GetBestRole(troop);
                                }

                                if (roleCount.ContainsKey(troop.CurrentRole))
                                    roleCount[troop.CurrentRole]++;
                                else
                                    roleCount[troop.CurrentRole] = 1;
                            }

                            Console.WriteLine("角色分布:");
                            foreach (var kvp in roleCount)
                            {
                                Console.WriteLine($"  {GetRoleDescription(kvp.Key)}: {kvp.Value}个");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析AI状态时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例6: 测试AI系统各个组件
        /// </summary>
        public static void Example6_TestAIComponents()
        {
            try
            {
                Console.WriteLine("=== AI系统组件测试 ===");

                // 获取测试部队
                Troop testTroop = null;
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (faction.Troops.Count > 0)
                    {
                        testTroop = faction.Troops.GetList()[0];
                        break;
                    }
                }

                if (testTroop != null)
                {
                    Console.WriteLine($"使用部队 {testTroop.ID} 进行组件测试");

                    // 🎯 测试角色选择器
                    var role = AIRoleSelector.GetBestRole(testTroop);
                    Console.WriteLine($"1. 角色选择器: {GetRoleDescription(role)}");

                    // 🎯 测试地图导航
                    var moveArea = MapNavigationHelper.GetUnitMoveableArea(testTroop);
                    Console.WriteLine($"2. 地图导航: 可移动到 {moveArea.Count} 个位置");

                    // 🎯 测试目标选择（如果有敌军）
                    var enemies = new List<Troop>();
                    var allies = new List<Troop>();
                    
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                    {
                        foreach (Troop troop in faction.Troops.GetList())
                        {
                            if (troop.BelongedFaction == testTroop.BelongedFaction)
                            {
                                allies.Add(troop);
                            }
                            else if (!testTroop.BelongedFaction.IsFriendly(troop.BelongedFaction))
                            {
                                enemies.Add(troop);
                            }
                        }
                    }

                    if (enemies.Count > 0)
                    {
                        var target = AITargetSelector.GetBestTarget(testTroop, enemies, allies);
                        Console.WriteLine($"3. 目标选择器: {(target != null ? $"选择敌军 {target.ID}" : "无合适目标")}");
                    }
                    else
                    {
                        Console.WriteLine("3. 目标选择器: 无敌军可供测试");
                    }

                    // 🎯 测试行动序列器
                    var sortedTroops = AIActionSequencer.GetSortedTurnOrder(allies);
                    Console.WriteLine($"4. 行动序列器: 优化了 {sortedTroops.Count} 个部队的行动顺序");

                    Console.WriteLine("所有组件测试完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试AI组件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>中文描述</returns>
        private static string GetRoleDescription(AIRole role)
        {
            switch (role)
            {
                case AIRole.Tank: return "肉盾";
                case AIRole.DPS: return "输出";
                case AIRole.Mage: return "法师";
                case AIRole.Support: return "辅助";
                case AIRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 主测试方法 - 可以在游戏中调用来测试AI系统
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("开始运行AI系统集成示例...\n");

            try
            {
                Example1_AutomaticIntegration();
                Console.WriteLine();

                Example2_ManualFactionTurn();
                Console.WriteLine();

                Example3_SingleTroopAI();
                Console.WriteLine();

                Example4_BatchOptimizedAI();
                Console.WriteLine();

                Example5_AnalyzeAIStatus();
                Console.WriteLine();

                Example6_TestAIComponents();
                Console.WriteLine();

                Console.WriteLine("所有AI系统示例运行完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"运行示例时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 简化的AI系统调用接口
    /// 为常用操作提供简单的静态方法
    /// </summary>
    public static class SimpleAI
    {
        /// <summary>
        /// 为势力执行一次完整的AI回合
        /// </summary>
        /// <param name="faction">目标势力</param>
        public static void ExecuteFactionTurn(Faction faction)
        {
            if (faction != null && !faction.Destroyed)
            {
                AIFactionTurnIntegration.RunFactionTurn(faction);
            }
        }

        /// <summary>
        /// 为部队分配最佳角色
        /// </summary>
        /// <param name="troop">目标部队</param>
        /// <returns>分配的角色</returns>
        public static AIRole AssignBestRole(Troop troop)
        {
            if (troop != null && !troop.Destroyed)
            {
                troop.CurrentRole = AIRoleSelector.GetBestRole(troop);
                return troop.CurrentRole;
            }
            return AIRole.None;
        }

        /// <summary>
        /// 获取部队的最佳攻击目标
        /// </summary>
        /// <param name="troop">攻击者</param>
        /// <param name="enemies">敌军列表</param>
        /// <param name="allies">友军列表</param>
        /// <returns>最佳目标</returns>
        public static Troop GetBestTarget(Troop troop, List<Troop> enemies, List<Troop> allies)
        {
            if (troop != null && enemies != null && allies != null)
            {
                return AITargetSelector.GetBestTarget(troop, enemies, allies);
            }
            return null;
        }

        /// <summary>
        /// 获取部队的可移动区域
        /// </summary>
        /// <param name="troop">目标部队</param>
        /// <returns>可移动位置列表</returns>
        public static List<Microsoft.Xna.Framework.Point> GetMoveableArea(Troop troop)
        {
            if (troop != null && !troop.Destroyed)
            {
                return MapNavigationHelper.GetUnitMoveableArea(troop);
            }
            return new List<Microsoft.Xna.Framework.Point>();
        }

        /// <summary>
        /// 优化部队的行动顺序
        /// </summary>
        /// <param name="troops">部队列表</param>
        /// <returns>优化后的行动顺序</returns>
        public static List<Troop> OptimizeTurnOrder(List<Troop> troops)
        {
            if (troops != null && troops.Count > 0)
            {
                return AIActionSequencer.GetSortedTurnOrder(troops);
            }
            return new List<Troop>();
        }
    }
}