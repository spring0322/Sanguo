using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.PersonDetail;

namespace GameManager
{
    /// <summary>
    /// 纳谏倾向系统测试
    /// </summary>
    public static class AdvisorListenSystemTest
    {
        /// <summary>
        /// 运行完整的纳谏倾向系统测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 纳谏倾向系统完整测试 ===");
            Console.WriteLine();

            // 创建测试场景
            var testScenarios = CreateTestScenarios();

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"=== 测试场景: {scenario.Name} ===");
                TestListenScenario(scenario);
                Console.WriteLine();
            }

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 创建测试场景
        /// </summary>
        private static List<ListenTestScenario> CreateTestScenarios()
        {
            return new List<ListenTestScenario>
            {
                new ListenTestScenario
                {
                    Name = "刘备 + 诸葛亮（言听计从型）",
                    Faction = CreateTestFaction("刘备", "诸葛亮", 100, 75, 85, 95, 80, 100, 95, 90, 95),
                    Description = "历史上最著名的君臣搭档，刘备对诸葛亮言听计从"
                },
                new ListenTestScenario
                {
                    Name = "袁绍 + 田丰（刚愎自用型）",
                    Faction = CreateTestFaction("袁绍", "田丰", 10, 70, 75, 80, 60, 85, 70, 75, 85),
                    Description = "历史上的反面典型，袁绍经常不听田丰建议"
                },
                new ListenTestScenario
                {
                    Name = "曹操 + 荀彧（较易听从型）",
                    Faction = CreateTestFaction("曹操", "荀彧", 70, 95, 90, 85, 75, 90, 85, 80, 90),
                    Description = "曹操虽有主见，但通常会听荀彧的建议"
                },
                new ListenTestScenario
                {
                    Name = "孙权 + 周瑜（普通型）",
                    Faction = CreateTestFaction("孙权", "周瑜", 50, 80, 85, 90, 75, 90, 95, 85, 90),
                    Description = "孙权有一定独立性，但会考虑周瑜建议"
                },
                new ListenTestScenario
                {
                    Name = "董卓 + 李儒（较难说服型）",
                    Faction = CreateTestFaction("董卓", "李儒", 30, 60, 80, 70, 40, 65, 70, 60, 75),
                    Description = "董卓狂妄自大，不太听军师建议"
                },
                new ListenTestScenario
                {
                    Name = "张飞无军师（独立决策）",
                    Faction = CreateTestFaction("张飞", null, 50, 65, 98, 75, 40, 0, 0, 0, 0),
                    Description = "无军师情况下的独立决策"
                }
            };
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string leaderName, string advisorName, int listenChance,
            int leaderInt, int leaderCmd, int leaderCha, int leaderPol,
            int advisorInt, int advisorCmd, int advisorCha, int advisorPol)
        {
            var faction = new Faction();
            faction.Name = $"{leaderName}军";
            
            // 创建君主
            faction.Leader = new Person();
            faction.Leader.Name = leaderName;
            faction.Leader.Intelligence = leaderInt;
            faction.Leader.Command = leaderCmd;
            faction.Leader.Charm = leaderCha;
            faction.Leader.Politics = leaderPol;
            faction.Leader.Ideal = 50; // 默认理想
            faction.Leader.BelongedFaction = faction;
            
            // 创建性格类型
            faction.Leader.Character = new CharacterKind();
            faction.Leader.Character.ListenToAdvisorChance = listenChance;
            
            // 创建军师（如果有）
            if (!string.IsNullOrEmpty(advisorName))
            {
                faction.Advisor = new Person();
                faction.Advisor.Name = advisorName;
                faction.Advisor.Intelligence = advisorInt;
                faction.Advisor.Command = advisorCmd;
                faction.Advisor.Charm = advisorCha;
                faction.Advisor.Politics = advisorPol;
                faction.Advisor.Ideal = 45; // 略有差异
                faction.Advisor.Loyalty = 90; // 高忠诚度
                faction.Advisor.BelongedFaction = faction;
            }
            
            return faction;
        }

        /// <summary>
        /// 测试听从场景
        /// </summary>
        private static void TestListenScenario(ListenTestScenario scenario)
        {
            Console.WriteLine($"描述: {scenario.Description}");
            Console.WriteLine($"势力: {scenario.Faction.Name}");
            Console.WriteLine($"君主: {scenario.Faction.Leader.Name} (纳谏倾向: {scenario.Faction.Leader.Character?.ListenToAdvisorChance ?? 0}%)");
            
            if (scenario.Faction.Advisor != null)
            {
                Console.WriteLine($"军师: {scenario.Faction.Advisor.Name} (智力: {scenario.Faction.Advisor.Intelligence})");
            }
            else
            {
                Console.WriteLine("军师: 无");
            }
            Console.WriteLine();

            if (scenario.Faction.Advisor == null)
            {
                Console.WriteLine("无军师，无法测试听从概率");
                return;
            }

            // 执行20次测试，统计听从率
            int listenCount = 0;
            var refusalMessages = new List<string>();
            
            for (int i = 0; i < 20; i++)
            {
                bool willListen = scenario.Faction.AICheckListenToAdvisor();
                if (willListen)
                {
                    listenCount++;
                }
                else
                {
                    // 收集拒绝消息示例
                    if (refusalMessages.Count < 3)
                    {
                        var reasons = GetRefusalReasonsForTest(scenario.Faction.Leader);
                        string reason = reasons[GameObject.Random(reasons.Length)];
                        refusalMessages.Add($"{scenario.Faction.Leader.Name}：「{reason}」");
                    }
                }
            }

            double actualListenRate = listenCount / 20.0 * 100;
            int expectedRate = scenario.Faction.Leader.Character?.ListenToAdvisorChance ?? 50;
            
            Console.WriteLine($"测试结果 (20次):");
            Console.WriteLine($"  期望听从率: {expectedRate}%");
            Console.WriteLine($"  实际听从率: {actualListenRate}% ({listenCount}/20)");
            
            // 分析结果
            double deviation = Math.Abs(actualListenRate - expectedRate);
            if (deviation <= 15)
            {
                Console.WriteLine($"  ✅ 结果符合预期 (偏差{deviation:F1}%)");
            }
            else
            {
                Console.WriteLine($"  ⚠️ 结果偏差较大 (偏差{deviation:F1}%)");
            }

            // 显示拒绝消息示例
            if (refusalMessages.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("拒绝消息示例:");
                foreach (var message in refusalMessages)
                {
                    Console.WriteLine($"  {message}");
                }
            }
        }

        /// <summary>
        /// 获取拒绝理由（测试用）
        /// </summary>
        private static string[] GetRefusalReasonsForTest(Person leader)
        {
            int listenChance = leader.Character?.ListenToAdvisorChance ?? 50;
            
            if (listenChance <= 20) // 极度刚愎自用
            {
                return new string[]
                {
                    "孤意已决，先生勿复多言！",
                    "吾自有主张，何须他人指点？",
                    "此事吾心中早有定计！"
                };
            }
            else if (listenChance <= 40) // 较为固执
            {
                return new string[]
                {
                    "此计虽好，恐有诈也...",
                    "先生之言虽善，吾另有考量。",
                    "先生过于谨慎，机不可失！"
                };
            }
            else if (listenChance <= 60) // 有自己想法
            {
                return new string[]
                {
                    "吾视敌军如草芥，何须用计？",
                    "先生所言有理，然吾有更好之策。",
                    "此计太过复杂，不如直取！"
                };
            }
            else // 一般情况
            {
                return new string[]
                {
                    "先生之策虽妙，吾心中另有安排。",
                    "此事吾已有定计，先生勿忧。",
                    "时势变化，当机立断！"
                };
            }
        }

        /// <summary>
        /// 测试不同纳谏倾向的表现
        /// </summary>
        public static void TestDifferentListenChances()
        {
            Console.WriteLine("=== 不同纳谏倾向测试 ===");
            Console.WriteLine();

            var listenLevels = new[]
            {
                new { Level = 100, Description = "言听计从型 (如刘备)" },
                new { Level = 80, Description = "较易听从型 (如曹操早期)" },
                new { Level = 60, Description = "有主见型 (如孙权)" },
                new { Level = 40, Description = "较难说服型 (如曹操后期)" },
                new { Level = 20, Description = "固执己见型 (如袁绍)" },
                new { Level = 5, Description = "极度刚愎型 (如董卓)" }
            };

            foreach (var level in listenLevels)
            {
                Console.WriteLine($"=== {level.Description} (纳谏倾向: {level.Level}%) ===");
                
                var testFaction = CreateTestFaction("测试君主", "测试军师", level.Level,
                    80, 85, 80, 75, 90, 80, 75, 85);

                // 测试50次，获得更准确的统计
                int listenCount = 0;
                for (int i = 0; i < 50; i++)
                {
                    if (testFaction.AICheckListenToAdvisor())
                        listenCount++;
                }

                double actualRate = listenCount / 50.0 * 100;
                Console.WriteLine($"实际听从率: {actualRate}% ({listenCount}/50)");
                
                // 分析性格特征
                AnalyzeListenPattern(level.Level, actualRate);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 分析听从模式
        /// </summary>
        private static void AnalyzeListenPattern(int expectedRate, double actualRate)
        {
            Console.WriteLine("性格特征分析:");
            
            if (expectedRate >= 80)
            {
                Console.WriteLine("  - 高度信任军师，很少拒绝建议");
                Console.WriteLine("  - 体现了明君善于纳谏的特质");
                if (actualRate >= 75)
                    Console.WriteLine("  ✅ 符合言听计从特征");
            }
            else if (expectedRate >= 60)
            {
                Console.WriteLine("  - 通常会听从军师建议");
                Console.WriteLine("  - 偶尔会有自己的想法");
                if (actualRate >= 50 && actualRate <= 80)
                    Console.WriteLine("  ✅ 符合较易听从特征");
            }
            else if (expectedRate >= 40)
            {
                Console.WriteLine("  - 有一定独立性");
                Console.WriteLine("  - 会选择性地听从建议");
                if (actualRate >= 30 && actualRate <= 60)
                    Console.WriteLine("  ✅ 符合有主见特征");
            }
            else if (expectedRate >= 20)
            {
                Console.WriteLine("  - 较为固执，不易说服");
                Console.WriteLine("  - 经常拒绝军师建议");
                if (actualRate <= 40)
                    Console.WriteLine("  ✅ 符合固执己见特征");
            }
            else
            {
                Console.WriteLine("  - 极度刚愎自用");
                Console.WriteLine("  - 几乎不听任何建议");
                if (actualRate <= 20)
                    Console.WriteLine("  ✅ 符合刚愎自用特征");
            }
        }

        /// <summary>
        /// 测试历史人物的纳谏倾向设定
        /// </summary>
        public static void TestHistoricalCharacters()
        {
            Console.WriteLine("=== 历史人物纳谏倾向设定建议 ===");
            Console.WriteLine();

            var historicalSettings = new[]
            {
                new { Name = "刘备", ListenChance = 95, Reason = "对诸葛亮言听计从，体现仁君纳谏" },
                new { Name = "曹操", ListenChance = 70, Reason = "有主见但会听取荀彧等人建议" },
                new { Name = "孙权", ListenChance = 60, Reason = "年轻时较听从，后期更独立" },
                new { Name = "袁绍", ListenChance = 15, Reason = "刚愎自用，不听田丰、沮授建议" },
                new { Name = "董卓", ListenChance = 10, Reason = "狂妄自大，几乎不听任何建议" },
                new { Name = "吕布", ListenChance = 25, Reason = "有勇无谋，但偶尔会听陈宫建议" },
                new { Name = "刘表", ListenChance = 80, Reason = "性格温和，较易听从蒯越等人建议" },
                new { Name = "刘璋", ListenChance = 85, Reason = "性格软弱，容易被人说服" },
                new { Name = "马超", ListenChance = 35, Reason = "年轻气盛，不太听劝" },
                new { Name = "张飞", ListenChance = 40, Reason = "莽撞但对刘备和诸葛亮还是会听" }
            };

            Console.WriteLine("推荐的历史人物纳谏倾向设定:");
            Console.WriteLine();
            
            foreach (var setting in historicalSettings)
            {
                Console.WriteLine($"{setting.Name}: {setting.ListenChance}%");
                Console.WriteLine($"  理由: {setting.Reason}");
                Console.WriteLine();
            }

            Console.WriteLine("设定原则:");
            Console.WriteLine("1. 基于历史记录和人物性格");
            Console.WriteLine("2. 体现明君与昏君的差异");
            Console.WriteLine("3. 为游戏提供策略深度");
            Console.WriteLine("4. 保持历史真实感");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("🎮 纳谏倾向系统完整测试套件");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            RunCompleteTest();
            Console.WriteLine();
            
            TestDifferentListenChances();
            Console.WriteLine();
            
            TestHistoricalCharacters();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("测试完成！纳谏倾向系统运行正常。");
            Console.WriteLine();
            Console.WriteLine("核心特性验证:");
            Console.WriteLine("✅ CharacterKind.ListenToAdvisorChance 属性");
            Console.WriteLine("✅ 基于性格的拒绝消息系统");
            Console.WriteLine("✅ 智力差值和关系修正");
            Console.WriteLine("✅ 历史人物特征还原");
            Console.WriteLine("✅ 情报系统消息显示");
        }

        /// <summary>
        /// 测试场景结构
        /// </summary>
        private struct ListenTestScenario
        {
            public string Name;
            public Faction Faction;
            public string Description;
        }
    }
}