using System;
using System.Collections.Generic;
using GameObjects;
using GameManager;
using Microsoft.Xna.Framework;
using static GameManager.StrategistManager;

namespace GameManager
{
    /// <summary>
    /// 战斗技能预测系统测试
    /// </summary>
    public static class BattleSkillPredictionTest
    {
        /// <summary>
        /// 运行完整的战斗技能预测测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 战斗技能预测系统测试 ===");
            Console.WriteLine();

            // 创建测试数据
            var testScenarios = CreateTestScenarios();

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"=== 测试场景: {scenario.Name} ===");
                TestSkillPredictions(scenario);
                Console.WriteLine();
            }

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 创建测试场景
        /// </summary>
        private static List<TestScenario> CreateTestScenarios()
        {
            return new List<TestScenario>
            {
                new TestScenario
                {
                    Name = "诸葛亮 vs 普通敌将",
                    PlayerLeader = CreateTestPerson("诸葛亮", 100, 95, 90, 95),
                    PlayerAdvisor = CreateTestPerson("诸葛亮", 100, 95, 90, 95), // 自己当军师
                    EnemyLeader = CreateTestPerson("普通敌将", 60, 70, 65, 50),
                    Description = "高智力军师预测高智力技能"
                },
                new TestScenario
                {
                    Name = "刘备 + 诸葛亮 vs 曹操",
                    PlayerLeader = CreateTestPerson("刘备", 75, 85, 95, 80),
                    PlayerAdvisor = CreateTestPerson("诸葛亮", 100, 95, 90, 95),
                    EnemyLeader = CreateTestPerson("曹操", 95, 90, 85, 75),
                    Description = "有军师的君主对战强敌"
                },
                new TestScenario
                {
                    Name = "张飞无军师 vs 吕布",
                    PlayerLeader = CreateTestPerson("张飞", 65, 98, 75, 40),
                    PlayerAdvisor = null, // 无军师
                    EnemyLeader = CreateTestPerson("吕布", 70, 100, 65, 30),
                    Description = "无军师情况下的技能预测"
                },
                new TestScenario
                {
                    Name = "袁绍 + 田丰 vs 关羽",
                    PlayerLeader = CreateTestPerson("袁绍", 70, 75, 80, 60),
                    PlayerAdvisor = CreateTestPerson("田丰", 85, 70, 75, 85),
                    EnemyLeader = CreateTestPerson("关羽", 80, 95, 85, 80),
                    Description = "中等智力军师的预测准确度"
                }
            };
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int calmness)
        {
            var person = new Person();
            // 使用反射或直接设置属性（根据Person类的实际实现）
            // 这里假设Person类有公共属性设置器
            person.Name = name;
            person.Intelligence = intelligence;
            person.Command = command;
            person.Charm = charm;
            person.Calmness = calmness;
            person.Strength = 80; // 默认武力
            return person;
        }

        /// <summary>
        /// 测试技能预测
        /// </summary>
        private static void TestSkillPredictions(TestScenario scenario)
        {
            Console.WriteLine($"描述: {scenario.Description}");
            Console.WriteLine($"君主: {scenario.PlayerLeader.Name} (智{scenario.PlayerLeader.Intelligence} 统{scenario.PlayerLeader.Command} 魅{scenario.PlayerLeader.Charm})");
            
            if (scenario.PlayerAdvisor != null)
            {
                Console.WriteLine($"军师: {scenario.PlayerAdvisor.Name} (智{scenario.PlayerAdvisor.Intelligence})");
            }
            else
            {
                Console.WriteLine("军师: 无");
            }
            
            Console.WriteLine($"敌将: {scenario.EnemyLeader.Name} (智{scenario.EnemyLeader.Intelligence} 冷静{scenario.EnemyLeader.Calmness})");
            Console.WriteLine();

            // 创建测试势力和部队
            var playerFaction = CreateTestFaction(scenario.PlayerLeader, scenario.PlayerAdvisor);
            var enemyTroop = CreateTestTroop(scenario.EnemyLeader);

            // 测试所有技能类型
            var skillTypes = new[]
            {
                SkillType.FirePlot,
                SkillType.WaterPlot,
                SkillType.Ambush,
                SkillType.Provoke,
                SkillType.Confuse,
                SkillType.Retreat,
                SkillType.Rally
            };

            Console.WriteLine("技能预测结果:");
            foreach (var skill in skillTypes)
            {
                int prediction = StrategistManager.GetBattlePrediction(playerFaction, enemyTroop, skill);
                string skillName = GetSkillDisplayName(skill);
                
                if (prediction == -1)
                {
                    Console.WriteLine($"  {skillName}: ??% (无军师)");
                }
                else
                {
                    string confidence = GetConfidenceLevel(prediction);
                    Console.WriteLine($"  {skillName}: {prediction}% {confidence}");
                }
            }

            // 如果有军师，显示详细分析
            if (scenario.PlayerAdvisor != null)
            {
                Console.WriteLine();
                Console.WriteLine("军师详细分析 (火计示例):");
                string analysis = StrategistManager.GetSkillPredictionAnalysis(playerFaction, enemyTroop, SkillType.FirePlot);
                Console.WriteLine(analysis);
            }
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(Person leader, Person advisor)
        {
            var faction = new Faction();
            faction.Leader = leader;
            faction.Advisor = advisor;
            
            // 设置人物所属势力
            if (leader != null) leader.BelongedFaction = faction;
            if (advisor != null) advisor.BelongedFaction = faction;
            
            return faction;
        }

        /// <summary>
        /// 创建测试部队
        /// </summary>
        private static Troop CreateTestTroop(Person leader)
        {
            var troop = new Troop();
            troop.Leader = leader;
            troop.Quantity = 5000; // 默认兵力
            troop.Morale = 80;     // 默认士气
            troop.RealDestination = new Vector2(100, 100); // 默认位置
            return troop;
        }

        /// <summary>
        /// 获取技能显示名称
        /// </summary>
        private static string GetSkillDisplayName(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.FirePlot: return "火计";
                case SkillType.WaterPlot: return "水计";
                case SkillType.Ambush: return "伏兵";
                case SkillType.Provoke: return "挑衅";
                case SkillType.Confuse: return "混乱";
                case SkillType.Retreat: return "撤退";
                case SkillType.Rally: return "鼓舞";
                default: return skill.ToString();
            }
        }

        /// <summary>
        /// 获取置信度描述
        /// </summary>
        private static string GetConfidenceLevel(int prediction)
        {
            if (prediction >= 80) return "🟢 极高";
            else if (prediction >= 60) return "🟡 较高";
            else if (prediction >= 40) return "🟠 一般";
            else if (prediction >= 20) return "🔴 较低";
            else return "⚫ 极低";
        }

        /// <summary>
        /// 测试军师误差系统
        /// </summary>
        public static void TestAdvisorErrorSystem()
        {
            Console.WriteLine("=== 军师误差系统测试 ===");
            Console.WriteLine();

            var advisors = new[]
            {
                CreateTestPerson("诸葛亮", 100, 95, 90, 95),
                CreateTestPerson("庞统", 95, 90, 85, 90),
                CreateTestPerson("荀彧", 85, 80, 80, 85),
                CreateTestPerson("田丰", 75, 70, 75, 80),
                CreateTestPerson("郭图", 60, 65, 70, 60)
            };

            var enemy = CreateTestTroop(CreateTestPerson("测试敌将", 70, 75, 65, 70));

            foreach (var advisor in advisors)
            {
                Console.WriteLine($"=== 军师: {advisor.Name} (智力{advisor.Intelligence}) ===");
                
                var faction = CreateTestFaction(advisor, advisor);
                
                Console.WriteLine("10次火计预测结果:");
                for (int i = 1; i <= 10; i++)
                {
                    int prediction = StrategistManager.GetBattlePrediction(faction, enemy, SkillType.FirePlot);
                    Console.WriteLine($"  第{i}次: {prediction}%");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试场景结构
        /// </summary>
        private struct TestScenario
        {
            public string Name;
            public Person PlayerLeader;
            public Person PlayerAdvisor;
            public Person EnemyLeader;
            public string Description;
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("=== 性能测试 ===");
            
            var faction = CreateTestFaction(
                CreateTestPerson("测试君主", 80, 85, 75, 70),
                CreateTestPerson("测试军师", 90, 80, 75, 85)
            );
            
            var enemy = CreateTestTroop(CreateTestPerson("测试敌将", 70, 75, 65, 70));

            var startTime = DateTime.Now;
            
            // 执行1000次预测
            for (int i = 0; i < 1000; i++)
            {
                StrategistManager.GetBattlePrediction(faction, enemy, SkillType.FirePlot);
            }
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            Console.WriteLine($"1000次预测耗时: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"平均每次预测: {duration.TotalMilliseconds / 1000:F2}ms");
        }
    }
}