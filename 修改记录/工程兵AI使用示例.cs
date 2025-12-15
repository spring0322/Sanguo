using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameManager;

namespace GameObjects.AI.Examples
{
    /// <summary>
    /// 工程兵AI系统使用示例
    /// 展示如何使用工程兵AI进行智能建造
    /// </summary>
    public static class ConstructionAIExamples
    {
        /// <summary>
        /// 示例1: 自动工程兵AI（已集成到游戏回合系统）
        /// </summary>
        public static void Example1_AutomaticConstructionAI()
        {
            Console.WriteLine("=== 示例1: 自动工程兵AI ===");
            Console.WriteLine("工程兵AI已自动集成到游戏的AI回合系统中");
            Console.WriteLine("每个AI势力回合都会自动检测工程兵并执行建造AI");
            Console.WriteLine("无需手动调用，系统会自动：");
            Console.WriteLine("1. 识别工程兵部队");
            Console.WriteLine("2. 评估建造条件");
            Console.WriteLine("3. 选择最佳建造位置");
            Console.WriteLine("4. 执行建造或移动到目标位置");
        }

        /// <summary>
        /// 示例2: 手动执行工程兵AI决策
        /// </summary>
        public static void Example2_ManualConstructionAI()
        {
            try
            {
                Console.WriteLine("\n=== 示例2: 手动执行工程兵AI决策 ===");

                // 查找第一个工程兵部队
                Troop engineerTroop = FindFirstEngineerTroop();
                
                if (engineerTroop != null)
                {
                    Console.WriteLine($"找到工程兵部队: {engineerTroop.ID} ({AIConstructionPlanner.GetConstructionTroopDescription(engineerTroop)})");

                    // 获取战场信息
                    var enemies = GetEnemyTroops(engineerTroop.BelongedFaction);
                    var allies = GetAllyTroops(engineerTroop.BelongedFaction);

                    Console.WriteLine($"战场态势 - 敌军: {enemies.Count}, 友军: {allies.Count}");

                    // 执行工程兵AI决策
                    AIConstructionPlanner.ExecuteConstructionAI(engineerTroop, enemies, allies);
                }
                else
                {
                    Console.WriteLine("未找到工程兵部队");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行手动工程兵AI时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例3: 建造位置评估
        /// </summary>
        public static void Example3_BuildPositionEvaluation()
        {
            try
            {
                Console.WriteLine("\n=== 示例3: 建造位置评估 ===");

                var engineerTroop = FindFirstEngineerTroop();
                if (engineerTroop == null)
                {
                    Console.WriteLine("未找到工程兵部队");
                    return;
                }

                // 获取可移动区域
                var moveableArea = MapNavigationHelper.GetUnitMoveableArea(engineerTroop);
                Console.WriteLine($"工程兵可移动到 {moveableArea.Count} 个位置");

                // 获取敌军信息
                var enemies = GetEnemyTroops(engineerTroop.BelongedFaction);

                // 评估最佳建造位置
                var bestPosition = AIConstructionPlanner.GetBestBuildPosition(
                    engineerTroop, 
                    moveableArea, 
                    enemies
                );

                Console.WriteLine($"推荐建造位置: ({bestPosition.X}, {bestPosition.Y})");

                // 显示位置详细信息
                ShowPositionDetails(bestPosition);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"评估建造位置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例4: 建造条件检查
        /// </summary>
        public static void Example4_BuildConditionCheck()
        {
            try
            {
                Console.WriteLine("\n=== 示例4: 建造条件检查 ===");

                var engineerTroop = FindFirstEngineerTroop();
                if (engineerTroop == null)
                {
                    Console.WriteLine("未找到工程兵部队");
                    return;
                }

                Console.WriteLine($"检查工程兵 {engineerTroop.ID} 的建造条件:");

                // 检查是否为工程兵
                bool isEngineer = AIConstructionPlanner.IsConstructionTroop(engineerTroop);
                Console.WriteLine($"1. 是否为工程兵: {(isEngineer ? "是" : "否")}");

                if (isEngineer)
                {
                    // 检查建造条件
                    bool canBuild = AIConstructionPlanner.ShouldTransformNow(engineerTroop);
                    Console.WriteLine($"2. 是否满足建造条件: {(canBuild ? "是" : "否")}");

                    // 显示详细条件
                    ShowBuildConditions(engineerTroop);

                    if (canBuild)
                    {
                        Console.WriteLine("✅ 可以立即执行建造");
                    }
                    else
                    {
                        Console.WriteLine("❌ 暂时无法建造，需要满足更多条件");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查建造条件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例5: 批量工程兵管理
        /// </summary>
        public static void Example5_BatchEngineerManagement()
        {
            try
            {
                Console.WriteLine("\n=== 示例5: 批量工程兵管理 ===");

                var allEngineers = FindAllEngineerTroops();
                Console.WriteLine($"找到 {allEngineers.Count} 个工程兵部队");

                foreach (var engineer in allEngineers)
                {
                    Console.WriteLine($"\n工程兵 {engineer.ID}:");
                    Console.WriteLine($"  所属势力: {engineer.BelongedFaction?.Name ?? "未知"}");
                    Console.WriteLine($"  当前位置: ({engineer.Position.X}, {engineer.Position.Y})");
                    Console.WriteLine($"  部队描述: {AIConstructionPlanner.GetConstructionTroopDescription(engineer)}");

                    // 检查建造状态
                    bool canBuild = AIConstructionPlanner.ShouldTransformNow(engineer);
                    Console.WriteLine($"  建造状态: {(canBuild ? "可以建造" : "等待条件")}");

                    // 如果可以建造，显示推荐位置
                    if (canBuild)
                    {
                        var moveableArea = MapNavigationHelper.GetUnitMoveableArea(engineer);
                        var enemies = GetEnemyTroops(engineer.BelongedFaction);
                        var bestPos = AIConstructionPlanner.GetBestBuildPosition(engineer, moveableArea, enemies);
                        Console.WriteLine($"  推荐位置: ({bestPos.X}, {bestPos.Y})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量管理工程兵时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例6: 工程兵战术分析
        /// </summary>
        public static void Example6_EngineerTacticalAnalysis()
        {
            try
            {
                Console.WriteLine("\n=== 示例6: 工程兵战术分析 ===");

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (!Session.Current.Scenario.IsPlayer(faction))
                    {
                        var engineers = GetFactionEngineers(faction);
                        if (engineers.Count > 0)
                        {
                            Console.WriteLine($"\n势力 {faction.Name} 的工程兵分析:");
                            Console.WriteLine($"  工程兵数量: {engineers.Count}");
                            Console.WriteLine($"  势力资金: {faction.Fund}");

                            // 分析建造潜力
                            int readyToBuild = 0;
                            int needMovement = 0;
                            int lackResources = 0;

                            foreach (var engineer in engineers)
                            {
                                if (AIConstructionPlanner.ShouldTransformNow(engineer))
                                {
                                    readyToBuild++;
                                }
                                else if (faction.Fund < 500) // 假设建造成本
                                {
                                    lackResources++;
                                }
                                else
                                {
                                    needMovement++;
                                }
                            }

                            Console.WriteLine($"  可立即建造: {readyToBuild}");
                            Console.WriteLine($"  需要移动: {needMovement}");
                            Console.WriteLine($"  缺乏资源: {lackResources}");

                            // 战术建议
                            if (readyToBuild > 0)
                            {
                                Console.WriteLine("  🏗️ 建议: 立即执行建造任务");
                            }
                            else if (needMovement > 0)
                            {
                                Console.WriteLine("  🚶 建议: 移动到更好的建造位置");
                            }
                            else if (lackResources > 0)
                            {
                                Console.WriteLine("  💰 建议: 积累更多资源后再建造");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析工程兵战术时出错: {ex.Message}");
            }
        }

        // ================= 辅助方法 =================

        /// <summary>
        /// 查找第一个工程兵部队
        /// </summary>
        /// <returns>工程兵部队或null</returns>
        private static Troop FindFirstEngineerTroop()
        {
            try
            {
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    foreach (Troop troop in faction.Troops.GetList())
                    {
                        if (AIConstructionPlanner.IsConstructionTroop(troop))
                        {
                            return troop;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找工程兵时出错: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 查找所有工程兵部队
        /// </summary>
        /// <returns>工程兵部队列表</returns>
        private static List<Troop> FindAllEngineerTroops()
        {
            var engineers = new List<Troop>();

            try
            {
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    foreach (Troop troop in faction.Troops.GetList())
                    {
                        if (AIConstructionPlanner.IsConstructionTroop(troop))
                        {
                            engineers.Add(troop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找所有工程兵时出错: {ex.Message}");
            }

            return engineers;
        }

        /// <summary>
        /// 获取指定势力的工程兵
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>工程兵列表</returns>
        private static List<Troop> GetFactionEngineers(Faction faction)
        {
            var engineers = new List<Troop>();

            try
            {
                if (faction?.Troops != null)
                {
                    foreach (Troop troop in faction.Troops.GetList())
                    {
                        if (AIConstructionPlanner.IsConstructionTroop(troop))
                        {
                            engineers.Add(troop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取势力工程兵时出错: {ex.Message}");
            }

            return engineers;
        }

        /// <summary>
        /// 获取敌军部队
        /// </summary>
        /// <param name="faction">己方势力</param>
        /// <returns>敌军列表</returns>
        private static List<Troop> GetEnemyTroops(Faction faction)
        {
            var enemies = new List<Troop>();

            try
            {
                if (faction == null) return enemies;

                foreach (Faction otherFaction in Session.Current.Scenario.Factions.GetList())
                {
                    if (otherFaction != faction && !faction.IsFriendly(otherFaction))
                    {
                        enemies.AddRange(otherFaction.Troops.GetList());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取敌军时出错: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 获取友军部队
        /// </summary>
        /// <param name="faction">己方势力</param>
        /// <returns>友军列表</returns>
        private static List<Troop> GetAllyTroops(Faction faction)
        {
            var allies = new List<Troop>();

            try
            {
                if (faction?.Troops != null)
                {
                    allies.AddRange(faction.Troops.GetList());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取友军时出错: {ex.Message}");
            }

            return allies;
        }

        /// <summary>
        /// 显示位置详细信息
        /// </summary>
        /// <param name="position">位置</param>
        private static void ShowPositionDetails(Point position)
        {
            try
            {
                Console.WriteLine($"位置详细信息 ({position.X}, {position.Y}):");

                // 地形信息
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(position);
                Console.WriteLine($"  地形类型: {terrainKind}");

                // 建筑信息
                var architecture = Session.Current.Scenario.GetArchitectureByPosition(position);
                if (architecture != null)
                {
                    Console.WriteLine($"  已有建筑: {architecture.Name}");
                }
                else
                {
                    Console.WriteLine($"  建筑状态: 空地");
                }

                // 部队信息
                var troop = Session.Current.Scenario.GetTroopByPosition(position);
                if (troop != null)
                {
                    Console.WriteLine($"  部队占用: {troop.ID} ({troop.BelongedFaction?.Name})");
                }
                else
                {
                    Console.WriteLine($"  部队状态: 无部队");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"显示位置信息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示建造条件详情
        /// </summary>
        /// <param name="troop">工程兵部队</param>
        private static void ShowBuildConditions(Troop troop)
        {
            try
            {
                Console.WriteLine("建造条件详情:");

                // 资金检查
                int requiredFund = 500; // 假设建造成本
                bool hasFund = troop.BelongedFaction.Fund >= requiredFund;
                Console.WriteLine($"  资金条件: {(hasFund ? "✅" : "❌")} (需要: {requiredFund}, 当前: {troop.BelongedFaction.Fund})");

                // 位置检查
                var existingArch = Session.Current.Scenario.GetArchitectureByPosition(troop.Position);
                bool positionFree = existingArch == null;
                Console.WriteLine($"  位置条件: {(positionFree ? "✅" : "❌")} {(positionFree ? "位置空闲" : $"已有建筑: {existingArch.Name}")}");

                // 移动力检查
                bool hasMovement = troop.MovabilityLeft > 0;
                Console.WriteLine($"  移动力条件: {(hasMovement ? "✅" : "❌")} (剩余: {troop.MovabilityLeft})");

                // 地形检查
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(troop.Position);
                bool terrainSuitable = terrainKind != TerrainKind.水域;
                Console.WriteLine($"  地形条件: {(terrainSuitable ? "✅" : "❌")} (地形: {terrainKind})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"显示建造条件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有工程兵AI示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("开始运行工程兵AI系统示例...\n");

            try
            {
                Example1_AutomaticConstructionAI();
                Example2_ManualConstructionAI();
                Example3_BuildPositionEvaluation();
                Example4_BuildConditionCheck();
                Example5_BatchEngineerManagement();
                Example6_EngineerTacticalAnalysis();

                Console.WriteLine("\n所有工程兵AI示例运行完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"运行示例时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 简化的工程兵AI接口
    /// </summary>
    public static class SimpleConstructionAI
    {
        /// <summary>
        /// 检查部队是否为工程兵
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否为工程兵</returns>
        public static bool IsEngineer(Troop troop)
        {
            return AIConstructionPlanner.IsConstructionTroop(troop);
        }

        /// <summary>
        /// 为工程兵执行AI决策
        /// </summary>
        /// <param name="engineer">工程兵部队</param>
        public static void ExecuteEngineerAI(Troop engineer)
        {
            if (IsEngineer(engineer))
            {
                var enemies = GetEnemies(engineer.BelongedFaction);
                var allies = GetAllies(engineer.BelongedFaction);
                AIConstructionPlanner.ExecuteConstructionAI(engineer, enemies, allies);
            }
        }

        /// <summary>
        /// 获取最佳建造位置
        /// </summary>
        /// <param name="engineer">工程兵部队</param>
        /// <returns>最佳建造位置</returns>
        public static Point GetBestBuildLocation(Troop engineer)
        {
            if (IsEngineer(engineer))
            {
                var moveableArea = MapNavigationHelper.GetUnitMoveableArea(engineer);
                var enemies = GetEnemies(engineer.BelongedFaction);
                return AIConstructionPlanner.GetBestBuildPosition(engineer, moveableArea, enemies);
            }
            return engineer.Position;
        }

        /// <summary>
        /// 检查是否可以立即建造
        /// </summary>
        /// <param name="engineer">工程兵部队</param>
        /// <returns>是否可以建造</returns>
        public static bool CanBuildNow(Troop engineer)
        {
            return IsEngineer(engineer) && AIConstructionPlanner.ShouldTransformNow(engineer);
        }

        // 辅助方法
        private static List<Troop> GetEnemies(Faction faction)
        {
            var enemies = new List<Troop>();
            try
            {
                foreach (Faction f in Session.Current.Scenario.Factions.GetList())
                {
                    if (f != faction && !faction.IsFriendly(f))
                    {
                        enemies.AddRange(f.Troops.GetList());
                    }
                }
            }
            catch { }
            return enemies;
        }

        private static List<Troop> GetAllies(Faction faction)
        {
            return faction?.Troops?.GetList() ?? new List<Troop>();
        }
    }
}