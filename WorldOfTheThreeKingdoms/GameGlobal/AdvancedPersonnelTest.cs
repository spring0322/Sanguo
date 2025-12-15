using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 高级人事管理系统测试
    /// </summary>
    public static class AdvancedPersonnelTest
    {
        /// <summary>
        /// 运行完整的高级人事管理测试
        /// </summary>
        public static void RunAdvancedTests()
        {
            Console.WriteLine("=== 高级人事管理系统测试 ===\n");

            try
            {
                TestSeasonalAssessment();
                TestRiskPrediction();
                TestRotationSystem();
                TestTalentDevelopment();
                TestEmergencyResponse();
                TestPersonnelStrategies();
                TestConfigurableParameters();
                TestLongTermSimulation();

                Console.WriteLine("\n🎉 高级人事管理系统测试完成！所有功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 高级人事管理测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试季节性评估
        /// </summary>
        private static void TestSeasonalAssessment()
        {
            Console.WriteLine("--- 季节性评估测试 ---");

            var faction = CreateAdvancedTestFaction("蜀国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            // 设置初始太守
            var chengdu = faction.Cities.First(c => c.Name == "成都");
            var zhuge = faction.Officers.First(o => o.Name == "诸葛亮");
            chengdu.AppointPrefect(zhuge);

            Console.WriteLine("  执行季节性评估:");
            for (int season = 1; season <= 4; season++)
            {
                Console.WriteLine($"    第{season}季度评估...");
                advancedManager.SeasonalAssessment();
            }

            Console.WriteLine("  ✓ 季节性评估测试完成\n");
        }

        /// <summary>
        /// 测试风险预测
        /// </summary>
        private static void TestRiskPrediction()
        {
            Console.WriteLine("--- 风险预测测试 ---");

            var faction = CreateAdvancedTestFaction("魏国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            // 创建高风险武将
            var riskyOfficer = new Officer
            {
                Name = "高风险武将",
                ID = 9999,
                Politics = 60,
                Leadership = 70,
                Intelligence = 65,
                War = 75,
                Loyalty = 55,      // 低忠诚
                Ambition = 90,     // 高野心
                Righteousness = 40, // 低义理
                Age = 35,
                Health = 80,
                State = OfficerState.Active,
                Traits = new HashSet<Trait> { Trait.Rebellious, Trait.Ambitious },
                Relations = new Dictionary<int, RelationType>(),
                Experiences = new List<string>()
            };

            // 添加与君主的厌恶关系
            if (faction.Ruler != null)
            {
                riskyOfficer.AddRelation(faction.Ruler, RelationType.Hated);
            }

            faction.Officers.Add(riskyOfficer);

            Console.WriteLine("  执行风险预测:");
            advancedManager.SeasonalAssessment();

            Console.WriteLine("  ✓ 风险预测测试完成\n");
        }

        /// <summary>
        /// 测试轮岗系统
        /// </summary>
        private static void TestRotationSystem()
        {
            Console.WriteLine("--- 轮岗系统测试 ---");

            var faction = CreateAdvancedTestFaction("吴国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            // 模拟长期任职
            var officer = faction.Officers.First();
            var city = faction.Cities.First();
            city.AppointPrefect(officer);

            // 模拟多个季度以触发轮岗
            for (int i = 0; i < 8; i++)
            {
                advancedManager.SeasonalAssessment();
            }

            Console.WriteLine("  执行轮岗系统:");
            advancedManager.ImplementRotationSystem();

            Console.WriteLine("  ✓ 轮岗系统测试完成\n");
        }

        /// <summary>
        /// 测试人才培养
        /// </summary>
        private static void TestTalentDevelopment()
        {
            Console.WriteLine("--- 人才培养测试 ---");

            var faction = CreateAdvancedTestFaction("蜀国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            // 添加年轻武将
            var youngTalent = new Officer
            {
                Name = "年轻才俊",
                ID = 8888,
                Politics = 60,
                Leadership = 65,
                Intelligence = 70,
                War = 55,
                Age = 25,  // 年轻
                Health = 95,
                Loyalty = 85,
                Ambition = 70,
                Righteousness = 80,
                State = OfficerState.Active,
                Traits = new HashSet<Trait> { Trait.Ambitious, Trait.Scholar },
                Relations = new Dictionary<int, RelationType>(),
                Experiences = new List<string>()
            };

            faction.Officers.Add(youngTalent);

            Console.WriteLine($"  培养前能力: 政治{youngTalent.Politics}, 统率{youngTalent.Leadership}, 智力{youngTalent.Intelligence}, 武力{youngTalent.War}");

            Console.WriteLine("  执行人才培养:");
            advancedManager.DevelopTalent();

            Console.WriteLine($"  培养后能力: 政治{youngTalent.Politics}, 统率{youngTalent.Leadership}, 智力{youngTalent.Intelligence}, 武力{youngTalent.War}");
            Console.WriteLine($"  获得经历: {string.Join(", ", youngTalent.Experiences)}");

            Console.WriteLine("  ✓ 人才培养测试完成\n");
        }

        /// <summary>
        /// 测试应急响应
        /// </summary>
        private static void TestEmergencyResponse()
        {
            Console.WriteLine("--- 应急响应测试 ---");

            var faction = CreateAdvancedTestFaction("蜀国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            var testCity = faction.Cities.First();

            Console.WriteLine("  测试叛乱应急响应:");
            advancedManager.EmergencyResponse("rebellion", testCity);

            Console.WriteLine("  测试入侵应急响应:");
            advancedManager.EmergencyResponse("invasion", testCity);

            Console.WriteLine("  测试自然灾害应急响应:");
            advancedManager.EmergencyResponse("natural_disaster", testCity);

            Console.WriteLine("  测试经济危机应急响应:");
            advancedManager.EmergencyResponse("economic_crisis");

            Console.WriteLine("  ✓ 应急响应测试完成\n");
        }

        /// <summary>
        /// 测试人事策略
        /// </summary>
        private static void TestPersonnelStrategies()
        {
            Console.WriteLine("--- 人事策略测试 ---");

            var config = new PersonnelConfig();

            var strategies = Enum.GetValues<PersonnelStrategy>();
            foreach (var strategy in strategies)
            {
                Console.WriteLine($"  测试策略: {strategy}");
                Console.WriteLine($"    描述: {PersonnelStrategyManager.GetStrategyDescription(strategy)}");
                
                PersonnelStrategyManager.ApplyStrategy(config, strategy);
                Console.WriteLine($"    配置: {config.GetConfigSummary()}");
                
                if (!config.ValidateConfig())
                {
                    Console.WriteLine($"    ✗ 配置验证失败");
                }
                else
                {
                    Console.WriteLine($"    ✓ 配置验证通过");
                }
            }

            Console.WriteLine("  ✓ 人事策略测试完成\n");
        }

        /// <summary>
        /// 测试可配置参数
        /// </summary>
        private static void TestConfigurableParameters()
        {
            Console.WriteLine("--- 可配置参数测试 ---");

            var config = new PersonnelConfig();

            Console.WriteLine("  默认配置:");
            Console.WriteLine($"    {config.GetConfigSummary()}");

            Console.WriteLine("  根据势力状态调整配置:");
            var states = Enum.GetValues<FactionState>();
            foreach (var state in states)
            {
                config.ResetToDefaults();
                config.AdjustForFactionState(state);
                Console.WriteLine($"    {state}: {config.GetConfigSummary()}");
            }

            Console.WriteLine("  ✓ 可配置参数测试完成\n");
        }

        /// <summary>
        /// 测试长期模拟
        /// </summary>
        private static void TestLongTermSimulation()
        {
            Console.WriteLine("--- 长期模拟测试 ---");

            var faction = CreateAdvancedTestFaction("蜀国");
            var advancedManager = new AdvancedPersonnelManager(faction);

            // 初始任命
            advancedManager.UpdateAssignments();

            Console.WriteLine("  模拟2年(8个季度)的人事管理:");
            
            var appointmentHistory = new List<string>();
            
            for (int season = 1; season <= 8; season++)
            {
                Console.WriteLine($"    === 第{season}季度 ===");
                
                // 随机改变势力状态
                if (season % 3 == 0)
                {
                    var states = Enum.GetValues<FactionState>();
                    faction.State = states[new Random().Next(states.Length)];
                    Console.WriteLine($"      势力状态变更为: {faction.State}");
                }

                // 执行季节性评估
                advancedManager.SeasonalAssessment();

                // 记录当前任命情况
                var currentAppointments = string.Join(", ", 
                    faction.Cities.Where(c => c.Prefect != null)
                              .Select(c => $"{c.Name}:{c.Prefect.Name}"));
                appointmentHistory.Add($"S{season}: {currentAppointments}");

                // 随机执行轮岗
                if (season % 4 == 0)
                {
                    Console.WriteLine($"      执行轮岗检查");
                    advancedManager.ImplementRotationSystem();
                }

                // 随机执行人才培养
                if (season % 2 == 0)
                {
                    Console.WriteLine($"      执行人才培养");
                    advancedManager.DevelopTalent();
                }

                // 随机应急事件
                if (new Random().NextDouble() < 0.3) // 30%概率
                {
                    var emergencies = new[] { "rebellion", "invasion", "natural_disaster" };
                    var emergency = emergencies[new Random().Next(emergencies.Length)];
                    var city = faction.Cities[new Random().Next(faction.Cities.Count)];
                    
                    Console.WriteLine($"      应急事件: {emergency} 在 {city.Name}");
                    advancedManager.EmergencyResponse(emergency, city);
                }
            }

            Console.WriteLine("\n  任命历史:");
            foreach (var record in appointmentHistory)
            {
                Console.WriteLine($"    {record}");
            }

            // 生成最终统计
            var finalStats = GenerateFinalStatistics(faction, advancedManager);
            Console.WriteLine($"\n  最终统计:\n{finalStats}");

            Console.WriteLine("  ✓ 长期模拟测试完成\n");
        }

        /// <summary>
        /// 创建高级测试势力
        /// </summary>
        private static Faction CreateAdvancedTestFaction(string name)
        {
            var faction = new Faction
            {
                Name = name,
                Gold = 50000,
                Food = 25000,
                FiscalHealth = 0.8f,
                TotalTroops = 5000,
                State = FactionState.Stable,
                Cities = new List<City>(),
                Officers = new List<Officer>()
            };

            // 创建君主
            var ruler = new Officer
            {
                Name = $"{name}君主",
                ID = 1,
                Politics = 85,
                Leadership = 80,
                Intelligence = 90,
                War = 70,
                Age = 45,
                Health = 90,
                Loyalty = 100,
                Ambition = 80,
                Righteousness = 95,
                State = OfficerState.Active,
                Traits = new HashSet<Trait> { Trait.Loyal, Trait.Pragmatic },
                Relations = new Dictionary<int, RelationType>(),
                Experiences = new List<string> { "Leadership", "Diplomacy" }
            };

            faction.Ruler = ruler;
            faction.Officers.Add(ruler);

            // 创建多样化的武将
            var officers = new[]
            {
                CreateAdvancedTestOfficer("诸葛亮", 95, 85, 100, 60, 35, new[] { Trait.Scholar, Trait.Loyal }, new[] { "CityManagement", "Intelligence" }),
                CreateAdvancedTestOfficer("关羽", 70, 95, 75, 90, 40, new[] { Trait.Loyal, Trait.Rash }, new[] { "SiegeDefense", "FieldBattle" }),
                CreateAdvancedTestOfficer("张飞", 40, 90, 50, 95, 38, new[] { Trait.Rash, Trait.Loyal }, new[] { "FieldBattle" }),
                CreateAdvancedTestOfficer("赵云", 65, 85, 70, 85, 35, new[] { Trait.Cautious, Trait.Loyal }, new[] { "SiegeDefense" }),
                CreateAdvancedTestOfficer("马超", 55, 88, 60, 92, 32, new[] { Trait.Ambitious, Trait.Rash }, new[] { "FieldBattle" }),
                CreateAdvancedTestOfficer("黄忠", 50, 80, 65, 88, 55, new[] { Trait.Cautious }, new[] { "SiegeDefense" }),
                CreateAdvancedTestOfficer("魏延", 60, 85, 65, 90, 42, new[] { Trait.Ambitious }, new[] { "FieldBattle" }),
                CreateAdvancedTestOfficer("姜维", 75, 80, 85, 80, 28, new[] { Trait.Scholar, Trait.Ambitious }, new[] { "Intelligence" })
            };

            // 设置特殊关系
            officers[1].AddRelation(ruler, RelationType.SwornBrother); // 关羽
            officers[2].AddRelation(ruler, RelationType.SwornBrother); // 张飞
            officers[1].AddRelation(officers[2], RelationType.SwornBrother); // 关张
            officers[0].AddRelation(ruler, RelationType.Friend); // 诸葛亮
            officers[6].AddRelation(officers[0], RelationType.Hated); // 魏延厌恶诸葛亮

            faction.Officers.AddRange(officers);

            // 创建多样化的城市
            var cities = new[]
            {
                CreateAdvancedTestCity("成都", false, true, false, 0.2f),   // 首都
                CreateAdvancedTestCity("汉中", true, false, true, 0.8f),    // 前线要塞
                CreateAdvancedTestCity("江州", true, false, true, 0.7f),    // 前线
                CreateAdvancedTestCity("永昌", false, false, false, 0.1f),  // 后方
                CreateAdvancedTestCity("南中", true, false, false, 0.5f),   // 边境
                CreateAdvancedTestCity("梓潼", false, false, false, 0.3f),  // 后方
                CreateAdvancedTestCity("涪城", true, false, false, 0.6f),   // 边境
                CreateAdvancedTestCity("绵竹", false, false, false, 0.2f)   // 后方
            };

            faction.Cities.AddRange(cities);
            faction.InitializePersonnelManager();

            return faction;
        }

        /// <summary>
        /// 创建高级测试武将
        /// </summary>
        private static Officer CreateAdvancedTestOfficer(string name, int politics, int leadership, 
            int intelligence, int war, int age, Trait[] traits, string[] experiences)
        {
            var officer = new Officer
            {
                Name = name,
                ID = new Random().Next(1000, 9999),
                Politics = politics,
                Leadership = leadership,
                Intelligence = intelligence,
                War = war,
                Age = age,
                Health = Math.Max(50, 100 - (age - 30) * 2), // 年龄影响健康
                Loyalty = new Random().Next(70, 95),
                Ambition = new Random().Next(30, 80),
                Righteousness = new Random().Next(60, 90),
                State = OfficerState.Active,
                Traits = new HashSet<Trait>(traits),
                Relations = new Dictionary<int, RelationType>(),
                Experiences = new List<string>(experiences)
            };

            return officer;
        }

        /// <summary>
        /// 创建高级测试城市
        /// </summary>
        private static City CreateAdvancedTestCity(string name, bool isBorder, bool isCapital, 
            bool hasRecentBattle, float baseThreat)
        {
            var random = new Random();
            return new City
            {
                Name = name,
                IsBorderCity = isBorder,
                IsCapital = isCapital,
                HasRecentBattle = hasRecentBattle,
                IsPortCity = random.NextDouble() < 0.2, // 20%概率是港口
                Population = random.Next(10000, 50000),
                MaxPopulation = 80000,
                Prosperity = (float)(random.NextDouble() * 0.4 + 0.4), // 0.4-0.8
                Security = (float)(random.NextDouble() * 0.3 + 0.6),   // 0.6-0.9
                Loyalty = (float)(random.NextDouble() * 0.2 + 0.7),    // 0.7-0.9
                Coordinates = new Point(random.Next(0, 100), random.Next(0, 100)),
                Buildings = new List<BuildingType>(),
                FreeSlots = random.Next(1, 4)
            };
        }

        /// <summary>
        /// 生成最终统计
        /// </summary>
        private static string GenerateFinalStatistics(Faction faction, AdvancedPersonnelManager manager)
        {
            var stats = "=== 最终统计报告 ===\n";

            // 基础统计
            stats += $"势力: {faction.Name}\n";
            stats += $"总武将数: {faction.Officers.Count}\n";
            stats += $"在职太守数: {faction.Cities.Count(c => c.Prefect != null)}\n";
            stats += $"空缺城市数: {faction.Cities.Count(c => c.Prefect == null)}\n";

            // 能力分布
            var avgPolitics = faction.Officers.Average(o => o.Politics);
            var avgLeadership = faction.Officers.Average(o => o.Leadership);
            var avgIntelligence = faction.Officers.Average(o => o.Intelligence);
            var avgWar = faction.Officers.Average(o => o.War);

            stats += $"平均能力: 政治{avgPolitics:F1}, 统率{avgLeadership:F1}, 智力{avgIntelligence:F1}, 武力{avgWar:F1}\n";

            // 忠诚度分布
            var loyalOfficers = faction.Officers.Count(o => o.Loyalty >= 80);
            var riskyOfficers = faction.Officers.Count(o => o.Loyalty < 60);
            stats += $"忠诚武将(≥80): {loyalOfficers}, 风险武将(<60): {riskyOfficers}\n";

            // 城市状况
            var avgProsperity = faction.Cities.Average(c => c.Prosperity);
            var avgSecurity = faction.Cities.Average(c => c.Security);
            var avgLoyalty = faction.Cities.Average(c => c.Loyalty);
            stats += $"城市平均状况: 繁荣{avgProsperity:F2}, 治安{avgSecurity:F2}, 忠诚{avgLoyalty:F2}\n";

            // 调动统计
            var activeTransfers = manager.GetActiveTransfers().Count;
            stats += $"进行中调动: {activeTransfers}\n";

            return stats;
        }

        /// <summary>
        /// 运行性能基准测试
        /// </summary>
        public static void RunPerformanceBenchmark()
        {
            Console.WriteLine("--- 高级人事管理性能基准测试 ---");

            var faction = CreateAdvancedTestFaction("测试势力");

            // 扩展到更大规模
            for (int i = 0; i < 100; i++)
            {
                var officer = CreateAdvancedTestOfficer($"武将{i}", 
                    new Random().Next(30, 100),
                    new Random().Next(30, 100),
                    new Random().Next(30, 100),
                    new Random().Next(30, 100),
                    new Random().Next(25, 65),
                    new[] { (Trait)(i % 12) },
                    new[] { "Experience" + (i % 5) });
                
                faction.Officers.Add(officer);
            }

            for (int i = 0; i < 50; i++)
            {
                var city = CreateAdvancedTestCity($"城市{i}", 
                    i % 3 == 0,  // 1/3 是边境
                    i == 0,      // 第一个是首都
                    i % 5 == 0,  // 1/5 有战斗
                    (float)(new Random().NextDouble()));
                
                faction.Cities.Add(city);
            }

            var advancedManager = new AdvancedPersonnelManager(faction);

            var startTime = DateTime.Now;

            // 执行完整的季节性评估
            for (int i = 0; i < 5; i++)
            {
                advancedManager.SeasonalAssessment();
                advancedManager.ImplementRotationSystem();
                advancedManager.DevelopTalent();
            }

            var elapsed = DateTime.Now - startTime;

            Console.WriteLine($"  大规模性能测试结果:");
            Console.WriteLine($"    武将数量: {faction.Officers.Count}");
            Console.WriteLine($"    城市数量: {faction.Cities.Count}");
            Console.WriteLine($"    季度数量: 5");
            Console.WriteLine($"    总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"    平均每季度: {elapsed.TotalMilliseconds / 5:F2} ms");

            if (elapsed.TotalMilliseconds / 5 < 50)
                Console.WriteLine($"    性能评价: 优秀 ⭐⭐⭐");
            else if (elapsed.TotalMilliseconds / 5 < 200)
                Console.WriteLine($"    性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine($"    性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能基准测试完成\n");
        }
    }
}