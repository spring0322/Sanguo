using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 有效智力系统集成测试
    /// 验证明主效应在各个预测系统中的正确应用
    /// </summary>
    public static class EffectiveIntelligenceIntegrationTest
    {
        /// <summary>
        /// 运行完整的集成测试
        /// </summary>
        public static void RunIntegrationTest()
        {
            Console.WriteLine("=== 有效智力系统集成测试 ===");
            Console.WriteLine();

            // 创建测试势力
            var testScenarios = CreateTestScenarios();

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"=== 测试场景: {scenario.Name} ===");
                TestScenario(scenario);
                Console.WriteLine();
            }

            // 对比测试
            Console.WriteLine("=== 明主效应对比测试 ===");
            CompareWiseRulerEffect();
        }

        /// <summary>
        /// 测试场景数据结构
        /// </summary>
        public class TestScenario
        {
            public string Name { get; set; }
            public Faction PlayerFaction { get; set; }
            public Person TargetPerson { get; set; }
            public Troop EnemyTroop { get; set; }
            public Faction EnemyFaction { get; set; }
        }

        /// <summary>
        /// 创建测试场景
        /// </summary>
        private static List<TestScenario> CreateTestScenarios()
        {
            var scenarios = new List<TestScenario>();

            // 场景1: 明主型 - 曹操 + 程昱
            var caoFaction = CreateTestFaction("魏", "曹操", 96, "程昱", 90);
            scenarios.Add(new TestScenario
            {
                Name = "明主型势力 (曹操+程昱)",
                PlayerFaction = caoFaction,
                TargetPerson = CreateTestPerson("关羽", 85, 95),
                EnemyTroop = CreateTestTroop("张飞", 90, 98, 5000),
                EnemyFaction = CreateTestFaction("蜀", "刘备", 75, "诸葛亮", 100)
            });

            // 场景2: 军师型 - 刘备 + 诸葛亮
            var liuFaction = CreateTestFaction("蜀", "刘备", 75, "诸葛亮", 100);
            scenarios.Add(new TestScenario
            {
                Name = "军师型势力 (刘备+诸葛亮)",
                PlayerFaction = liuFaction,
                TargetPerson = CreateTestPerson("马超", 70, 92),
                EnemyTroop = CreateTestTroop("夏侯惇", 85, 90, 4500),
                EnemyFaction = CreateTestFaction("魏", "曹操", 96, "荀彧", 90)
            });

            // 场景3: 均衡型 - 孙权 + 周瑜
            var sunFaction = CreateTestFaction("吴", "孙权", 80, "周瑜", 90);
            scenarios.Add(new TestScenario
            {
                Name = "均衡型势力 (孙权+周瑜)",
                PlayerFaction = sunFaction,
                TargetPerson = CreateTestPerson("甘宁", 65, 88),
                EnemyTroop = CreateTestTroop("张辽", 88, 85, 4000),
                EnemyFaction = CreateTestFaction("袁", "袁绍", 70, "田丰", 85)
            });

            // 场景4: 无军师 - 张飞单独
            var zhangFaction = CreateTestFaction("张", "张飞", 65, null, 0);
            scenarios.Add(new TestScenario
            {
                Name = "无军师势力 (张飞)",
                PlayerFaction = zhangFaction,
                TargetPerson = CreateTestPerson("魏延", 60, 85),
                EnemyTroop = CreateTestTroop("许褚", 75, 95, 3500),
                EnemyFaction = CreateTestFaction("董", "董卓", 60, "李儒", 75)
            });

            return scenarios;
        }

        /// <summary>
        /// 测试单个场景
        /// </summary>
        private static void TestScenario(TestScenario scenario)
        {
            var faction = scenario.PlayerFaction;
            
            // 显示势力基本信息
            Console.WriteLine($"势力: {faction.Name}");
            Console.WriteLine($"君主: {faction.Leader?.Name} (智{faction.Leader?.Intelligence})");
            Console.WriteLine($"军师: {faction.Advisor?.Name ?? "无"} (智{faction.Advisor?.Intelligence ?? 0})");
            
            int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
            Console.WriteLine($"有效智力: {effectiveInt}");
            Console.WriteLine();

            // 测试招募预测
            if (scenario.TargetPerson != null)
            {
                Console.WriteLine("--- 招募预测测试 ---");
                var (predictedRate, comment) = StrategistManager.GetRecruitPrediction(faction, scenario.TargetPerson);
                Console.WriteLine($"目标: {scenario.TargetPerson.Name}");
                Console.WriteLine($"预测成功率: {predictedRate}%");
                Console.WriteLine($"军师评语: {comment}");
                Console.WriteLine();
            }

            // 测试战斗技能预测
            if (scenario.EnemyTroop != null)
            {
                Console.WriteLine("--- 战斗技能预测测试 ---");
                var skillTypes = new[] 
                { 
                    StrategistManager.SkillType.FirePlot, 
                    StrategistManager.SkillType.Ambush, 
                    StrategistManager.SkillType.Confuse 
                };

                foreach (var skill in skillTypes)
                {
                    int prediction = StrategistManager.GetBattlePrediction(faction, scenario.EnemyTroop, skill);
                    string skillName = GetSkillName(skill);
                    
                    if (prediction >= 0)
                        Console.WriteLine($"{skillName}: {prediction}%");
                    else
                        Console.WriteLine($"{skillName}: ??% (无军师)");
                }
                Console.WriteLine();
            }

            // 测试外交预测
            if (scenario.EnemyFaction != null)
            {
                Console.WriteLine("--- 外交预测测试 ---");
                var allianceResult = StrategistManager.PredictDiplomacy(faction, scenario.EnemyFaction, "结盟");
                var threatResult = StrategistManager.PredictDiplomacy(faction, scenario.EnemyFaction, "威胁");
                
                Console.WriteLine($"结盟成功率: {allianceResult.SuccessRate}%");
                Console.WriteLine($"威胁成功率: {threatResult.SuccessRate}%");
                Console.WriteLine();
            }

            // 显示明主效应分析
            Console.WriteLine("--- 明主效应分析 ---");
            string analysis = EffectiveIntelligenceSystem.GetWiseRulerAnalysis(faction);
            Console.WriteLine(analysis);
        }

        /// <summary>
        /// 对比明主效应
        /// </summary>
        private static void CompareWiseRulerEffect()
        {
            // 创建对比势力
            var mingzhu = CreateTestFaction("明主", "曹操", 96, "程昱", 90);    // 明主型
            var junshi = CreateTestFaction("军师", "刘备", 75, "诸葛亮", 100);  // 军师型
            var junhun = CreateTestFaction("昏君", "刘禅", 40, "姜维", 80);     // 昏君型

            var targetPerson = CreateTestPerson("关羽", 85, 95);
            var enemyTroop = CreateTestTroop("张飞", 90, 98, 5000);

            Console.WriteLine("同样的招募任务，不同势力的预测对比:");
            Console.WriteLine($"目标: {targetPerson.Name} (忠诚{targetPerson.Loyalty})");
            Console.WriteLine();

            var factions = new[] { mingzhu, junshi, junhun };
            foreach (var faction in factions)
            {
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                var (rate, comment) = StrategistManager.GetRecruitPrediction(faction, targetPerson);
                
                Console.WriteLine($"{faction.Name}势力:");
                Console.WriteLine($"  君主: {faction.Leader.Name}(智{faction.Leader.Intelligence})");
                Console.WriteLine($"  军师: {faction.Advisor?.Name ?? "无"}(智{faction.Advisor?.Intelligence ?? 0})");
                Console.WriteLine($"  有效智力: {effectiveInt}");
                Console.WriteLine($"  预测成功率: {rate}%");
                Console.WriteLine($"  评语: {comment}");
                Console.WriteLine();
            }

            Console.WriteLine("同样的火计技能，不同势力的预测对比:");
            Console.WriteLine($"目标: {enemyTroop.Leader?.Name} 部队");
            Console.WriteLine();

            foreach (var faction in factions)
            {
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                int prediction = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.FirePlot);
                
                Console.WriteLine($"{faction.Name}势力:");
                Console.WriteLine($"  有效智力: {effectiveInt}");
                
                if (prediction >= 0)
                    Console.WriteLine($"  火计成功率: {prediction}%");
                else
                    Console.WriteLine($"  火计成功率: ??% (无军师)");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name, string leaderName, int leaderInt, string advisorName, int advisorInt)
        {
            var faction = new Faction { Name = name };

            if (!string.IsNullOrEmpty(leaderName))
            {
                faction.Leader = new Person
                {
                    Name = leaderName,
                    Intelligence = leaderInt,
                    Command = leaderInt - 10,
                    Charm = leaderInt - 5,
                    Politics = leaderInt - 8,
                    BelongedFaction = faction
                };
            }

            if (!string.IsNullOrEmpty(advisorName) && advisorInt > 0)
            {
                faction.Advisor = new Person
                {
                    Name = advisorName,
                    Intelligence = advisorInt,
                    Command = advisorInt - 15,
                    Charm = advisorInt - 10,
                    Politics = advisorInt - 5,
                    BelongedFaction = faction
                };
            }

            return faction;
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int loyalty, int intelligence)
        {
            return new Person
            {
                Name = name,
                Loyalty = loyalty,
                Intelligence = intelligence,
                Command = intelligence - 5,
                Strength = intelligence + 10,
                Charm = intelligence - 10,
                Politics = intelligence - 15
            };
        }

        /// <summary>
        /// 创建测试部队
        /// </summary>
        private static Troop CreateTestTroop(string leaderName, int command, int strength, int quantity)
        {
            var leader = new Person
            {
                Name = leaderName,
                Command = command,
                Strength = strength,
                Intelligence = (command + strength) / 2,
                Calmness = command - 10
            };

            return new Troop
            {
                Leader = leader,
                Quantity = quantity,
                Morale = 80
            };
        }

        /// <summary>
        /// 获取技能名称
        /// </summary>
        private static string GetSkillName(StrategistManager.SkillType skillType)
        {
            switch (skillType)
            {
                case StrategistManager.SkillType.FirePlot: return "火计";
                case StrategistManager.SkillType.WaterPlot: return "水计";
                case StrategistManager.SkillType.Ambush: return "伏兵";
                case StrategistManager.SkillType.Provoke: return "挑衅";
                case StrategistManager.SkillType.Confuse: return "混乱";
                case StrategistManager.SkillType.Retreat: return "撤退";
                case StrategistManager.SkillType.Rally: return "鼓舞";
                default: return "未知技能";
            }
        }

        /// <summary>
        /// 测试明主效应的准确性提升
        /// </summary>
        public static void TestAccuracyImprovement()
        {
            Console.WriteLine("=== 明主效应准确性提升测试 ===");
            Console.WriteLine();

            // 创建对比势力：同样的军师，不同的君主
            var weakRuler = CreateTestFaction("弱主", "刘禅", 40, "诸葛亮", 100);   // 弱君主+强军师
            var strongRuler = CreateTestFaction("明主", "曹操", 96, "诸葛亮", 100);  // 强君主+强军师

            var targetPerson = CreateTestPerson("关羽", 85, 95);

            Console.WriteLine("同样的军师诸葛亮(智100)，不同君主的预测准确性对比:");
            Console.WriteLine();

            // 进行多次预测，统计误差
            int testCount = 100;
            var weakRulerErrors = new List<int>();
            var strongRulerErrors = new List<int>();

            for (int i = 0; i < testCount; i++)
            {
                // 假设真实成功率为60%
                int realRate = 60;

                // 弱君主势力预测
                var (weakRate, _) = StrategistManager.GetRecruitPrediction(weakRuler, targetPerson);
                weakRulerErrors.Add(Math.Abs(weakRate - realRate));

                // 强君主势力预测
                var (strongRate, _) = StrategistManager.GetRecruitPrediction(strongRuler, targetPerson);
                strongRulerErrors.Add(Math.Abs(strongRate - realRate));
            }

            // 计算平均误差
            double weakAvgError = weakRulerErrors.Average();
            double strongAvgError = strongRulerErrors.Average();

            Console.WriteLine($"弱君主势力 (刘禅智40 + 诸葛亮智100):");
            Console.WriteLine($"  有效智力: {EffectiveIntelligenceSystem.GetEffectiveIntelligence(weakRuler)}");
            Console.WriteLine($"  平均预测误差: {weakAvgError:F1}%");
            Console.WriteLine();

            Console.WriteLine($"明主势力 (曹操智96 + 诸葛亮智100):");
            Console.WriteLine($"  有效智力: {EffectiveIntelligenceSystem.GetEffectiveIntelligence(strongRuler)}");
            Console.WriteLine($"  平均预测误差: {strongAvgError:F1}%");
            Console.WriteLine();

            double improvement = weakAvgError - strongAvgError;
            Console.WriteLine($"明主效应带来的准确性提升: {improvement:F1}%");
            
            if (improvement > 0)
                Console.WriteLine("✓ 明主效应正常工作 - 高智力君主确实提升了预测准确性");
            else
                Console.WriteLine("✗ 明主效应异常 - 需要检查实现");
        }
    }
}