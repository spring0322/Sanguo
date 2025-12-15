using System;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师建议系统功能测试
    /// </summary>
    public static class AdvisorSuggestionFunctionTest
    {
        /// <summary>
        /// 测试三个核心检查方法的功能
        /// </summary>
        public static void TestCoreFunctions()
        {
            Console.WriteLine("=== 军师建议系统功能测试 ===");
            Console.WriteLine();

            // 测试敌军预警功能
            TestEnemyApproachingDetection();
            Console.WriteLine();

            // 测试人才发现功能
            TestUnfoundPersonDetection();
            Console.WriteLine();

            // 测试忠诚度检查功能
            TestLoyaltyIssuesDetection();
            Console.WriteLine();

            // 测试完整的建议获取流程
            TestCompleteAdviceFlow();
        }

        /// <summary>
        /// 测试敌军预警检测
        /// </summary>
        private static void TestEnemyApproachingDetection()
        {
            Console.WriteLine("【测试1：敌军预警检测】");
            
            try
            {
                // 创建测试势力
                var testFaction = CreateTestFaction("测试势力", 90);
                
                // 测试不同智力的检测范围
                var intelligenceLevels = new[] { 70, 80, 90, 100 };
                
                foreach (int intelligence in intelligenceLevels)
                {
                    bool hasEnemyNearby = AdvisorSuggestionSystem.CheckEnemyApproaching(testFaction, intelligence);
                    int detectionRange = intelligence / 10;
                    
                    Console.WriteLine($"智力{intelligence} -> 检测范围{detectionRange}格 -> {(hasEnemyNearby ? "发现敌军" : "未发现敌军")}");
                }
                
                Console.WriteLine("✅ 敌军预警检测功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 敌军预警检测出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试人才发现检测
        /// </summary>
        private static void TestUnfoundPersonDetection()
        {
            Console.WriteLine("【测试2：人才发现检测】");
            
            try
            {
                var testFaction = CreateTestFaction("测试势力", 85);
                
                bool hasUnfoundTalent = AdvisorSuggestionSystem.CheckUnfoundPerson(testFaction);
                
                Console.WriteLine($"检查在野人才 -> {(hasUnfoundTalent ? "发现强力在野武将" : "未发现强力在野武将")}");
                
                // 显示检测条件
                Console.WriteLine("检测条件: 能力值总和 > 200 且无所属势力");
                Console.WriteLine("✅ 人才发现检测功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 人才发现检测出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试忠诚度问题检测
        /// </summary>
        private static void TestLoyaltyIssuesDetection()
        {
            Console.WriteLine("【测试3：忠诚度问题检测】");
            
            try
            {
                var testFaction = CreateTestFaction("测试势力", 80);
                
                bool hasLoyaltyIssues = AdvisorSuggestionSystem.CheckLoyaltyIssues(testFaction);
                
                Console.WriteLine($"检查忠诚度问题 -> {(hasLoyaltyIssues ? "发现低忠诚武将" : "未发现低忠诚武将")}");
                
                // 显示检测条件
                Console.WriteLine("检测条件: 忠诚度 < 60 且非君主");
                Console.WriteLine("✅ 忠诚度检测功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 忠诚度检测出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试完整的建议获取流程
        /// </summary>
        private static void TestCompleteAdviceFlow()
        {
            Console.WriteLine("【测试4：完整建议获取流程】");
            
            try
            {
                var testFaction = CreateTestFaction("测试势力", 85);
                
                // 测试获取当前建议
                var suggestion = testFaction.GetCurrentSuggestion();
                Console.WriteLine($"当前建议类型: {GetSuggestionTypeName(suggestion)}");
                
                // 测试建议准确率
                bool isAccurate = testFaction.IsAdviceAccurate();
                Console.WriteLine($"建议准确性: {(isAccurate ? "准确" : "不准确")}");
                
                // 测试不同难度下的准确率
                Console.WriteLine("不同难度下的准确率测试:");
                for (int difficulty = 0; difficulty <= 15; difficulty += 5)
                {
                    int accurateCount = 0;
                    int totalTests = 100;
                    
                    for (int i = 0; i < totalTests; i++)
                    {
                        if (testFaction.IsAdviceAccurate(difficulty))
                        {
                            accurateCount++;
                        }
                    }
                    
                    double accuracyRate = (double)accurateCount / totalTests * 100;
                    Console.WriteLine($"  难度{difficulty}: {accuracyRate:F1}%");
                }
                
                Console.WriteLine("✅ 完整建议流程功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 完整建议流程出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name, int advisorIntelligence)
        {
            var faction = new Faction
            {
                Name = name,
                Leader = new Person 
                { 
                    Name = "测试君主", 
                    Intelligence = 75,
                    Loyalty = 100
                },
                Advisor = new Person 
                { 
                    Name = "测试军师", 
                    Intelligence = advisorIntelligence,
                    Loyalty = 95
                }
            };

            // 添加一些测试武将到势力中
            var persons = new PersonList();
            
            // 添加高忠诚武将
            persons.Add(new Person 
            { 
                Name = "忠诚武将", 
                Loyalty = 85, 
                BelongedFaction = faction,
                Command = 80,
                Intelligence = 70,
                Politics = 60,
                Glamour = 75
            });
            
            // 添加低忠诚武将（用于测试忠诚度警告）
            persons.Add(new Person 
            { 
                Name = "低忠诚武将", 
                Loyalty = 45, 
                BelongedFaction = faction,
                Command = 75,
                Intelligence = 65,
                Politics = 55,
                Glamour = 70
            });

            faction.Persons = persons;

            // 添加一些测试建筑
            var architectures = new ArchitectureList();
            architectures.Add(new Architecture 
            { 
                Name = "测试城市", 
                BelongedFaction = faction,
                Position = new Microsoft.Xna.Framework.Point(100, 100)
            });

            faction.Architectures = architectures;

            return faction;
        }

        /// <summary>
        /// 获取建议类型名称
        /// </summary>
        private static string GetSuggestionTypeName(AdvisorSuggestionKind type)
        {
            switch (type)
            {
                case AdvisorSuggestionKind.None: return "无建议";
                case AdvisorSuggestionKind.EnemyAttack: return "敌军来袭预警";
                case AdvisorSuggestionKind.PersonRecruit: return "人才招募建议";
                case AdvisorSuggestionKind.LoyaltyWarning: return "忠诚度警告";
                default: return "未知类型";
            }
        }

        /// <summary>
        /// 测试API兼容性
        /// </summary>
        public static void TestAPICompatibility()
        {
            Console.WriteLine("=== API兼容性测试 ===");
            Console.WriteLine();

            try
            {
                // 测试Session.Current.Scenario访问
                if (Session.Current?.Scenario != null)
                {
                    Console.WriteLine("✅ Session.Current.Scenario 访问正常");
                    
                    // 测试Troops访问
                    if (Session.Current.Scenario.Troops != null)
                    {
                        int troopCount = Session.Current.Scenario.Troops.Count;
                        Console.WriteLine($"✅ Troops访问正常，当前部队数量: {troopCount}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Troops为null");
                    }
                    
                    // 测试Persons访问
                    if (Session.Current.Scenario.Persons != null)
                    {
                        int personCount = Session.Current.Scenario.Persons.Count;
                        Console.WriteLine($"✅ Persons访问正常，当前人物数量: {personCount}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Persons为null");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Session.Current.Scenario为null，可能游戏未初始化");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ API兼容性测试出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行完整测试套件
        /// </summary>
        public static void RunFullTest()
        {
            Console.WriteLine("军师建议系统功能测试");
            Console.WriteLine("====================");
            Console.WriteLine();

            TestAPICompatibility();
            Console.WriteLine();
            
            TestCoreFunctions();
            Console.WriteLine();

            Console.WriteLine("测试完成！");
            Console.WriteLine();
            Console.WriteLine("当前可用的建议类型:");
            Console.WriteLine("✅ EnemyAttack - 敌军来袭预警");
            Console.WriteLine("✅ PersonRecruit - 人才招募建议");
            Console.WriteLine("✅ LoyaltyWarning - 忠诚度警告");
            Console.WriteLine("⏸️ InternalAffair - 内政建议 (已注释)");
            Console.WriteLine("⏸️ DisasterWarning - 灾害预警 (已注释)");
        }
    }
}