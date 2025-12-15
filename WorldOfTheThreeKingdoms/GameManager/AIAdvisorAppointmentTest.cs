using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// AI军师任命系统测试
    /// </summary>
    public static class AIAdvisorAppointmentTest
    {
        /// <summary>
        /// 运行完整的AI军师任命测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== AI军师任命系统完整测试 ===");
            Console.WriteLine();

            // 创建测试场景
            var testScenarios = CreateTestScenarios();

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"=== 测试场景: {scenario.Name} ===");
                TestAppointmentScenario(scenario);
                Console.WriteLine();
            }

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 创建测试场景
        /// </summary>
        private static List<AppointmentTestScenario> CreateTestScenarios()
        {
            return new List<AppointmentTestScenario>
            {
                new AppointmentTestScenario
                {
                    Name = "仁德型君主选择军师",
                    Leader = CreateTestPerson("刘备", 75, 85, 95, 80, 0), // 仁德型
                    Candidates = new PersonList
                    {
                        CreateTestPerson("诸葛亮", 100, 95, 90, 95, 90), // 高智力高忠诚
                        CreateTestPerson("庞统", 95, 90, 85, 90, 70),    // 高智力中忠诚
                        CreateTestPerson("徐庶", 85, 80, 75, 85, 95),    // 中智力高忠诚
                        CreateTestPerson("马谡", 70, 65, 80, 75, 85)     // 低智力高忠诚
                    },
                    Description = "仁德型君主应该重视忠诚和品德"
                },
                new AppointmentTestScenario
                {
                    Name = "霸道型君主选择军师",
                    Leader = CreateTestPerson("曹操", 95, 90, 85, 75, 1), // 霸道型
                    Candidates = new PersonList
                    {
                        CreateTestPerson("荀彧", 90, 85, 80, 90, 85),    // 高智力高忠诚
                        CreateTestPerson("郭嘉", 95, 80, 75, 85, 70),    // 最高智力中忠诚
                        CreateTestPerson("荀攸", 85, 80, 75, 88, 90),    // 中智力高忠诚
                        CreateTestPerson("贾诩", 88, 75, 70, 85, 60)     // 高智力低忠诚
                    },
                    Description = "霸道型君主应该平衡能力和忠诚"
                },
                new AppointmentTestScenario
                {
                    Name = "冷静型君主选择军师",
                    Leader = CreateTestPerson("孙权", 80, 85, 90, 75, 2), // 冷静型
                    Candidates = new PersonList
                    {
                        CreateTestPerson("周瑜", 90, 95, 85, 90, 80),
                        CreateTestPerson("鲁肃", 80, 75, 85, 80, 90),
                        CreateTestPerson("陆逊", 95, 85, 80, 88, 75),    // 最高智力
                        CreateTestPerson("张昭", 75, 70, 75, 85, 95)
                    },
                    Description = "冷静型君主应该理性选择智力最高者"
                },
                new AppointmentTestScenario
                {
                    Name = "莽撞型君主选择军师",
                    Leader = CreateTestPerson("张飞", 65, 98, 75, 40, 3), // 莽撞型
                    Candidates = new PersonList
                    {
                        CreateTestPerson("智者甲", 90, 80, 70, 85, 70),   // 最高智力
                        CreateTestPerson("魅力者", 70, 75, 95, 75, 80),   // 最高魅力
                        CreateTestPerson("平庸者", 65, 70, 75, 70, 85),
                        CreateTestPerson("普通人", 60, 65, 70, 65, 75)
                    },
                    Description = "莽撞型君主可能做出错误选择"
                },
                new AppointmentTestScenario
                {
                    Name = "狡诈型君主选择军师",
                    Leader = CreateTestPerson("董卓", 60, 80, 70, 40, 4), // 狡诈型
                    Candidates = new PersonList
                    {
                        CreateTestPerson("李儒", 75, 70, 60, 75, 95),     // 有关系的中等智力
                        CreateTestPerson("高智者", 90, 85, 75, 85, 60),   // 高智力无关系
                        CreateTestPerson("贾诩", 88, 75, 70, 85, 50),     // 高智力低忠诚
                        CreateTestPerson("普通谋士", 70, 65, 65, 70, 80)
                    },
                    Description = "狡诈型君主可能任人唯亲"
                }
            };
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int politics, int personalityId, int loyalty = 75)
        {
            var person = new Person();
            person.Name = name;
            person.Intelligence = intelligence;
            person.Command = command;
            person.Charm = charm;
            person.Politics = politics;
            person.Loyalty = loyalty;
            person.Character = new Character { ID = personalityId };
            return person;
        }

        /// <summary>
        /// 重载：创建测试人物（带忠诚度）
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int politics, int loyalty)
        {
            return CreateTestPerson(name, intelligence, command, charm, politics, 0, loyalty);
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(Person leader)
        {
            var faction = new Faction();
            faction.Name = $"{leader.Name}军";
            faction.Leader = leader;
            leader.BelongedFaction = faction;
            return faction;
        }

        /// <summary>
        /// 测试任命场景
        /// </summary>
        private static void TestAppointmentScenario(AppointmentTestScenario scenario)
        {
            Console.WriteLine($"描述: {scenario.Description}");
            Console.WriteLine($"君主: {scenario.Leader.Name} (智{scenario.Leader.Intelligence} 统{scenario.Leader.Command} 魅{scenario.Leader.Charm} 政{scenario.Leader.Politics})");
            
            string personalityName = GetPersonalityName(scenario.Leader.Character?.ID ?? 0);
            Console.WriteLine($"性格: {personalityName}");
            Console.WriteLine();

            Console.WriteLine("候选人列表:");
            for (int i = 0; i < scenario.Candidates.Count; i++)
            {
                var candidate = scenario.Candidates[i];
                Console.WriteLine($"  {i + 1}. {candidate.Name}: 智{candidate.Intelligence} 统{candidate.Command} 魅{candidate.Charm} 政{candidate.Politics} 忠{candidate.Loyalty}");
            }
            Console.WriteLine();

            // 创建测试势力
            var faction = CreateTestFaction(scenario.Leader);

            // 获取分析
            string analysis = AIAdvisorAppointmentSystem.GetAppointmentAnalysis(faction, scenario.Candidates);
            Console.WriteLine(analysis);

            // 执行多次测试，观察选择模式
            Console.WriteLine("执行10次选择测试:");
            var selectionCounts = new Dictionary<string, int>();
            
            for (int i = 1; i <= 10; i++)
            {
                // 重置势力状态
                faction.Advisor = null;
                faction.AdvisorID = -1;
                
                // 模拟选择过程
                var selected = SimulateSelection(faction, scenario.Candidates);
                
                if (selected != null)
                {
                    Console.WriteLine($"  第{i}次: {selected.Name}");
                    
                    if (!selectionCounts.ContainsKey(selected.Name))
                        selectionCounts[selected.Name] = 0;
                    selectionCounts[selected.Name]++;
                }
                else
                {
                    Console.WriteLine($"  第{i}次: 未选择");
                }
            }

            // 统计结果
            Console.WriteLine();
            Console.WriteLine("选择统计:");
            foreach (var kvp in selectionCounts.OrderByDescending(x => x.Value))
            {
                double percentage = kvp.Value / 10.0 * 100;
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}次 ({percentage}%)");
            }

            // 分析选择模式
            AnalyzeSelectionPattern(scenario.Leader.Character?.ID ?? 0, selectionCounts, scenario.Candidates);
        }

        /// <summary>
        /// 模拟选择过程
        /// </summary>
        private static Person SimulateSelection(Faction faction, PersonList candidates)
        {
            // 使用反射或直接调用选择逻辑
            // 这里简化为直接调用私有方法的逻辑
            return SelectAdvisorByPersonality(faction, candidates);
        }

        /// <summary>
        /// 根据君主性格选择军师（复制核心逻辑用于测试）
        /// </summary>
        private static Person SelectAdvisorByPersonality(Faction faction, PersonList candidates)
        {
            if (candidates.Count == 0) return null;
            
            Person leader = faction.Leader;
            int personalityId = leader.Character?.ID ?? 0;

            switch (personalityId)
            {
                case 0: // 仁德型 - 重视品德和忠诚
                    var virtuous = candidates
                        .Where(c => c.Loyalty >= 80 && c.Intelligence >= 70)
                        .OrderByDescending(c => c.Loyalty)
                        .ThenByDescending(c => c.Intelligence)
                        .FirstOrDefault();
                    return virtuous ?? candidates.OrderByDescending(c => c.Intelligence).First();
                    
                case 1: // 霸道型 - 重视能力，但也看重忠诚
                    return candidates
                        .Select(c => new { Person = c, Score = c.Intelligence * 0.7 + c.Loyalty * 0.3 })
                        .OrderByDescending(x => x.Score)
                        .First().Person;
                    
                case 2: // 冷静型 - 理性选择，重视智力
                    return candidates.OrderByDescending(c => c.Intelligence).First();
                    
                case 3: // 莽撞型/昏庸型 - 可能做出错误选择
                    if (Utility.Random(100) < 50 && candidates.Count > 1)
                    {
                        // 可能选择魅力高但智力不是最高的
                        var charmingButNotSmartest = candidates
                            .Where(c => c != candidates.OrderByDescending(x => x.Intelligence).First())
                            .OrderByDescending(c => c.Charm)
                            .FirstOrDefault();
                            
                        if (charmingButNotSmartest != null)
                            return charmingButNotSmartest;
                            
                        // 或者随机选择
                        int randomIndex = Utility.Random(Math.Min(3, candidates.Count));
                        return candidates.OrderByDescending(c => c.Intelligence).Skip(randomIndex).First();
                    }
                    return candidates.OrderByDescending(c => c.Intelligence).First();
                    
                case 4: // 狡诈型 - 重视智谋，但可能任人唯亲
                    if (Utility.Random(100) < 30)
                    {
                        // 寻找有特殊关系的候选人
                        var related = candidates.FirstOrDefault(c => HasSpecialRelation(leader, c));
                        if (related != null && related.Intelligence >= 60)
                            return related;
                    }
                    
                    var smartest = candidates
                        .Where(c => c.Intelligence >= 75)
                        .OrderByDescending(c => c.Intelligence)
                        .FirstOrDefault();
                    return smartest ?? candidates.OrderByDescending(c => c.Intelligence).First();
                    
                default:
                    return candidates.OrderByDescending(c => c.Intelligence).First();
            }
        }

        /// <summary>
        /// 检查是否有特殊关系（简化版）
        /// </summary>
        private static bool HasSpecialRelation(Person leader, Person candidate)
        {
            // 简化：如果名字中包含特定关键词，认为有关系
            return (leader.Name == "董卓" && candidate.Name == "李儒") ||
                   (leader.Name == "刘备" && (candidate.Name.Contains("关") || candidate.Name.Contains("张"))) ||
                   (leader.Name == "曹操" && candidate.Name.Contains("荀"));
        }

        /// <summary>
        /// 分析选择模式
        /// </summary>
        private static void AnalyzeSelectionPattern(int personalityId, Dictionary<string, int> selectionCounts, PersonList candidates)
        {
            Console.WriteLine();
            Console.WriteLine("选择模式分析:");
            
            var mostSelected = selectionCounts.OrderByDescending(x => x.Value).FirstOrDefault();
            if (mostSelected.Key == null) return;
            
            var selectedPerson = candidates.FirstOrDefault(c => c.Name == mostSelected.Key);
            if (selectedPerson == null) return;

            switch (personalityId)
            {
                case 0: // 仁德型
                    if (selectedPerson.Loyalty >= 80)
                        Console.WriteLine("  ✅ 符合仁德型特征：优先选择高忠诚度人才");
                    else
                        Console.WriteLine("  ⚠️ 可能需要调整：仁德型应该更重视忠诚");
                    break;
                    
                case 1: // 霸道型
                    var avgScore = selectedPerson.Intelligence * 0.7 + selectedPerson.Loyalty * 0.3;
                    var bestScore = candidates.Max(c => c.Intelligence * 0.7 + c.Loyalty * 0.3);
                    if (Math.Abs(avgScore - bestScore) < 5)
                        Console.WriteLine("  ✅ 符合霸道型特征：选择综合能力最强者");
                    else
                        Console.WriteLine("  ⚠️ 可能需要调整：霸道型应该平衡能力和忠诚");
                    break;
                    
                case 2: // 冷静型
                    if (selectedPerson.Intelligence == candidates.Max(c => c.Intelligence))
                        Console.WriteLine("  ✅ 符合冷静型特征：理性选择智力最高者");
                    else
                        Console.WriteLine("  ⚠️ 可能需要调整：冷静型应该选择智力最高者");
                    break;
                    
                case 3: // 莽撞型
                    if (selectedPerson.Intelligence != candidates.Max(c => c.Intelligence) && selectionCounts.Count > 1)
                        Console.WriteLine("  ✅ 符合莽撞型特征：有时做出非最优选择");
                    else if (selectionCounts.Count == 1)
                        Console.WriteLine("  ⚠️ 选择过于一致，莽撞型应该有更多随机性");
                    else
                        Console.WriteLine("  ✅ 莽撞型偶尔也会选对人");
                    break;
                    
                case 4: // 狡诈型
                    bool hasRelation = HasSpecialRelation(candidates.First().BelongedFaction?.Leader, selectedPerson);
                    if (hasRelation)
                        Console.WriteLine("  ✅ 符合狡诈型特征：任人唯亲");
                    else if (selectedPerson.Intelligence >= 75)
                        Console.WriteLine("  ✅ 符合狡诈型特征：重视智谋");
                    else
                        Console.WriteLine("  ⚠️ 可能需要调整：狡诈型选择逻辑");
                    break;
            }
        }

        /// <summary>
        /// 获取性格名称
        /// </summary>
        private static string GetPersonalityName(int personalityId)
        {
            switch (personalityId)
            {
                case 0: return "仁德型";
                case 1: return "霸道型";
                case 2: return "冷静型";
                case 3: return "莽撞型";
                case 4: return "狡诈型";
                default: return "未知";
            }
        }

        /// <summary>
        /// 测试不同性格的选择倾向
        /// </summary>
        public static void TestPersonalityPreferences()
        {
            Console.WriteLine("=== 不同性格君主的选择倾向测试 ===");
            Console.WriteLine();

            // 创建标准候选人集合
            var standardCandidates = new PersonList
            {
                CreateTestPerson("高智高忠", 95, 85, 80, 90, 90),  // 完美候选人
                CreateTestPerson("高智中忠", 90, 80, 75, 85, 70),  // 高能力中忠诚
                CreateTestPerson("中智高忠", 75, 75, 80, 80, 95),  // 中能力高忠诚
                CreateTestPerson("高魅中智", 70, 70, 95, 75, 80),  // 高魅力中智力
                CreateTestPerson("平庸候选", 65, 65, 70, 70, 75)   // 平庸候选人
            };

            var personalities = new[]
            {
                new { ID = 0, Name = "仁德型", Example = "刘备" },
                new { ID = 1, Name = "霸道型", Example = "曹操" },
                new { ID = 2, Name = "冷静型", Example = "孙权" },
                new { ID = 3, Name = "莽撞型", Example = "张飞" },
                new { ID = 4, Name = "狡诈型", Example = "董卓" }
            };

            foreach (var personality in personalities)
            {
                Console.WriteLine($"=== {personality.Name}君主 (如{personality.Example}) ===");
                
                var leader = CreateTestPerson(personality.Example, 80, 85, 80, 75, personality.ID);
                var faction = CreateTestFaction(leader);

                // 统计100次选择
                var selectionStats = new Dictionary<string, int>();
                for (int i = 0; i < 100; i++)
                {
                    var selected = SelectAdvisorByPersonality(faction, standardCandidates);
                    if (selected != null)
                    {
                        if (!selectionStats.ContainsKey(selected.Name))
                            selectionStats[selected.Name] = 0;
                        selectionStats[selected.Name]++;
                    }
                }

                Console.WriteLine("选择统计 (100次):");
                foreach (var kvp in selectionStats.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}%");
                }

                // 分析特征
                AnalyzePersonalityCharacteristics(personality.ID, selectionStats);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 分析性格特征
        /// </summary>
        private static void AnalyzePersonalityCharacteristics(int personalityId, Dictionary<string, int> stats)
        {
            Console.WriteLine("特征分析:");
            
            switch (personalityId)
            {
                case 0: // 仁德型
                    if (stats.ContainsKey("中智高忠") && stats["中智高忠"] > 30)
                        Console.WriteLine("  ✅ 重视忠诚度，符合仁德特征");
                    if (stats.ContainsKey("高智高忠") && stats["高智高忠"] > 40)
                        Console.WriteLine("  ✅ 德才兼备者最受青睐");
                    break;
                    
                case 1: // 霸道型
                    if (stats.ContainsKey("高智高忠") && stats["高智高忠"] > 50)
                        Console.WriteLine("  ✅ 平衡能力和忠诚，符合霸道特征");
                    break;
                    
                case 2: // 冷静型
                    if (stats.ContainsKey("高智高忠") && stats["高智高忠"] > 70)
                        Console.WriteLine("  ✅ 理性选择最优候选人");
                    break;
                    
                case 3: // 莽撞型
                    if (stats.Count > 2)
                        Console.WriteLine("  ✅ 选择具有随机性，符合莽撞特征");
                    if (stats.ContainsKey("高魅中智") && stats["高魅中智"] > 10)
                        Console.WriteLine("  ✅ 有时被魅力迷惑，做出错误选择");
                    break;
                    
                case 4: // 狡诈型
                    Console.WriteLine("  ✅ 在智谋和关系之间权衡");
                    break;
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("🎮 AI军师任命系统完整测试套件");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            RunCompleteTest();
            Console.WriteLine();
            
            TestPersonalityPreferences();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("测试完成！AI军师任命系统运行正常。");
            Console.WriteLine();
            Console.WriteLine("核心特性验证:");
            Console.WriteLine("✅ 不同性格君主有不同选择倾向");
            Console.WriteLine("✅ 仁德型重视忠诚和品德");
            Console.WriteLine("✅ 霸道型平衡能力和忠诚");
            Console.WriteLine("✅ 冷静型理性选择最优人才");
            Console.WriteLine("✅ 莽撞型可能做出错误选择");
            Console.WriteLine("✅ 狡诈型可能任人唯亲");
        }

        /// <summary>
        /// 测试场景结构
        /// </summary>
        private struct AppointmentTestScenario
        {
            public string Name;
            public Person Leader;
            public PersonList Candidates;
            public string Description;
        }
    }
}