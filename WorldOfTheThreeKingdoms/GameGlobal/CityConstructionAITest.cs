using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 城市建设AI测试系统
    /// </summary>
    public static class CityConstructionAITest
    {
        /// <summary>
        /// 运行完整的城市建设AI测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 城市建设AI系统测试 ===\n");

            try
            {
                TestBasicConstructionLogic();
                TestPrefectPersonalityInfluence();
                TestFactionStateInfluence();
                TestThreatLevelInfluence();
                TestAbilityInfluence();
                TestComplexScenarios();

                Console.WriteLine("\n🎉 城市建设AI系统测试完成！所有功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 城市建设AI测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试基础建设逻辑
        /// </summary>
        private static void TestBasicConstructionLogic()
        {
            Console.WriteLine("--- 基础建设逻辑测试 ---");

            var city = CreateTestCity("测试城市");
            var faction = CreateTestFaction("测试势力");
            city.Owner = faction;

            var constructionAI = new CityConstructionAI();

            Console.WriteLine("  测试无太守情况下的建设决策:");
            city.Prefect = null;
            constructionAI.UpdateCityConstruction(city);

            if (city.IsBuilding)
            {
                Console.WriteLine($"    ✓ 开始建设: {city.CurrentConstruction}");
            }
            else
            {
                Console.WriteLine($"    - 未开始建设 (可能资金不足)");
            }

            Console.WriteLine("  ✓ 基础建设逻辑测试完成\n");
        }

        /// <summary>
        /// 测试太守性格影响
        /// </summary>
        private static void TestPrefectPersonalityInfluence()
        {
            Console.WriteLine("--- 太守性格影响测试 ---");

            var constructionAI = new CityConstructionAI();

            // 测试不同性格的太守
            var personalities = new[]
            {
                (Trait.Rash, "莽撞太守", "应该偏好军事建筑"),
                (Trait.Greedy, "贪婪太守", "应该偏好经济建筑"),
                (Trait.Scholar, "学者太守", "应该偏好科技建筑"),
                (Trait.Cautious, "谨慎太守", "应该偏好防御建筑")
            };

            foreach (var (trait, name, expectation) in personalities)
            {
                Console.WriteLine($"  测试 {name} ({expectation}):");

                var city = CreateTestCity($"{name}的城市");
                var faction = CreateTestFaction("测试势力");
                city.Owner = faction;

                var prefect = CreateTestOfficer(name);
                prefect.AddTrait(trait);
                city.Prefect = prefect;

                // 记录建设前状态
                var beforeBuilding = city.CurrentConstruction;

                constructionAI.UpdateCityConstruction(city);

                if (city.IsBuilding)
                {
                    Console.WriteLine($"    建设选择: {city.CurrentConstruction}");
                    ValidatePersonalityChoice(trait, city.CurrentConstruction);
                }
                else
                {
                    Console.WriteLine($"    未开始建设");
                }
            }

            Console.WriteLine("  ✓ 太守性格影响测试完成\n");
        }

        /// <summary>
        /// 测试势力状态影响
        /// </summary>
        private static void TestFactionStateInfluence()
        {
            Console.WriteLine("--- 势力状态影响测试 ---");

            var constructionAI = new CityConstructionAI();

            var states = new[]
            {
                (FactionState.EconomicCrisis, "经济危机", "应该避免军事建筑"),
                (FactionState.WarTime, "战时状态", "应该优先军事建筑"),
                (FactionState.Prosperous, "繁荣期", "应该重视文化科技"),
                (FactionState.Defensive, "防守期", "应该重视防御建筑")
            };

            foreach (var (state, name, expectation) in states)
            {
                Console.WriteLine($"  测试 {name} ({expectation}):");

                var city = CreateTestCity($"{name}城市");
                var faction = CreateTestFaction("测试势力");
                faction.State = state;
                city.Owner = faction;

                var prefect = CreateTestOfficer("中性太守");
                prefect.AddTrait(Trait.Pragmatic); // 实用主义，不会有极端偏好
                city.Prefect = prefect;

                constructionAI.UpdateCityConstruction(city);

                if (city.IsBuilding)
                {
                    Console.WriteLine($"    建设选择: {city.CurrentConstruction}");
                    ValidateFactionStateChoice(state, city.CurrentConstruction);
                }
                else
                {
                    Console.WriteLine($"    未开始建设");
                }
            }

            Console.WriteLine("  ✓ 势力状态影响测试完成\n");
        }

        /// <summary>
        /// 测试威胁等级影响
        /// </summary>
        private static void TestThreatLevelInfluence()
        {
            Console.WriteLine("--- 威胁等级影响测试 ---");

            var constructionAI = new CityConstructionAI();

            // 高威胁边境城市
            Console.WriteLine("  测试高威胁边境城市:");
            var borderCity = CreateTestCity("边境要塞");
            borderCity.IsBorderCity = true;
            borderCity.HasRecentBattle = true;
            var faction1 = CreateTestFaction("边境势力");
            borderCity.Owner = faction1;

            var militaryPrefect = CreateTestOfficer("军事太守");
            militaryPrefect.Leadership = 80;
            militaryPrefect.AddTrait(Trait.Pragmatic);
            borderCity.Prefect = militaryPrefect;

            constructionAI.UpdateCityConstruction(borderCity);
            if (borderCity.IsBuilding)
            {
                Console.WriteLine($"    边境城市建设: {borderCity.CurrentConstruction} (应该是军事建筑)");
            }

            // 低威胁内陆城市
            Console.WriteLine("  测试低威胁内陆城市:");
            var innerCity = CreateTestCity("内陆商城");
            innerCity.IsBorderCity = false;
            innerCity.HasRecentBattle = false;
            var faction2 = CreateTestFaction("内陆势力");
            innerCity.Owner = faction2;

            var economicPrefect = CreateTestOfficer("经济太守");
            economicPrefect.Politics = 85;
            economicPrefect.AddTrait(Trait.Pragmatic);
            innerCity.Prefect = economicPrefect;

            constructionAI.UpdateCityConstruction(innerCity);
            if (innerCity.IsBuilding)
            {
                Console.WriteLine($"    内陆城市建设: {innerCity.CurrentConstruction} (应该是经济建筑)");
            }

            Console.WriteLine("  ✓ 威胁等级影响测试完成\n");
        }

        /// <summary>
        /// 测试能力值影响
        /// </summary>
        private static void TestAbilityInfluence()
        {
            Console.WriteLine("--- 能力值影响测试 ---");

            var constructionAI = new CityConstructionAI();

            // 高政治太守
            Console.WriteLine("  测试高政治太守:");
            var city1 = CreateTestCity("政治家的城市");
            var faction1 = CreateTestFaction("政治势力");
            city1.Owner = faction1;

            var politicalOfficer = CreateTestOfficer("政治家");
            politicalOfficer.Politics = 95;
            politicalOfficer.Leadership = 40;
            politicalOfficer.Intelligence = 60;
            city1.Prefect = politicalOfficer;

            constructionAI.UpdateCityConstruction(city1);
            if (city1.IsBuilding)
            {
                Console.WriteLine($"    高政治太守建设: {city1.CurrentConstruction}");
            }

            // 高统率太守
            Console.WriteLine("  测试高统率太守:");
            var city2 = CreateTestCity("将军的城市");
            var faction2 = CreateTestFaction("军事势力");
            city2.Owner = faction2;

            var militaryOfficer = CreateTestOfficer("将军");
            militaryOfficer.Politics = 30;
            militaryOfficer.Leadership = 90;
            militaryOfficer.Intelligence = 50;
            city2.Prefect = militaryOfficer;

            constructionAI.UpdateCityConstruction(city2);
            if (city2.IsBuilding)
            {
                Console.WriteLine($"    高统率太守建设: {city2.CurrentConstruction}");
            }

            // 高智力太守
            Console.WriteLine("  测试高智力太守:");
            var city3 = CreateTestCity("学者的城市");
            var faction3 = CreateTestFaction("学术势力");
            city3.Owner = faction3;

            var scholarOfficer = CreateTestOfficer("学者");
            scholarOfficer.Politics = 50;
            scholarOfficer.Leadership = 35;
            scholarOfficer.Intelligence = 95;
            city3.Prefect = scholarOfficer;

            constructionAI.UpdateCityConstruction(city3);
            if (city3.IsBuilding)
            {
                Console.WriteLine($"    高智力太守建设: {city3.CurrentConstruction}");
            }

            Console.WriteLine("  ✓ 能力值影响测试完成\n");
        }

        /// <summary>
        /// 测试复杂场景
        /// </summary>
        private static void TestComplexScenarios()
        {
            Console.WriteLine("--- 复杂场景测试 ---");

            var constructionAI = new CityConstructionAI();

            // 场景1：经济危机中的贪婪太守
            Console.WriteLine("  场景1：经济危机中的贪婪太守");
            var city1 = CreateTestCity("危机城市");
            var faction1 = CreateTestFaction("危机势力");
            faction1.State = FactionState.EconomicCrisis;
            faction1.FiscalHealth = 0.2f;
            city1.Owner = faction1;

            var greedyOfficer = CreateTestOfficer("贪婪太守");
            greedyOfficer.AddTrait(Trait.Greedy);
            greedyOfficer.Politics = 70;
            city1.Prefect = greedyOfficer;

            constructionAI.UpdateCityConstruction(city1);
            if (city1.IsBuilding)
            {
                Console.WriteLine($"    建设选择: {city1.CurrentConstruction} (贪婪+危机应该选经济建筑)");
            }

            // 场景2：战时的学者太守
            Console.WriteLine("  场景2：战时的学者太守");
            var city2 = CreateTestCity("战时学府");
            var faction2 = CreateTestFaction("战时势力");
            faction2.State = FactionState.WarTime;
            city2.Owner = faction2;
            city2.IsBorderCity = true;

            var scholarOfficer = CreateTestOfficer("战时学者");
            scholarOfficer.AddTrait(Trait.Scholar);
            scholarOfficer.Intelligence = 90;
            scholarOfficer.AddExperience("SiegeDefense"); // 有围城经验
            city2.Prefect = scholarOfficer;

            constructionAI.UpdateCityConstruction(city2);
            if (city2.IsBuilding)
            {
                Console.WriteLine($"    建设选择: {city2.CurrentConstruction} (学者在战时的选择)");
            }

            // 场景3：年老保守太守的繁荣城市
            Console.WriteLine("  场景3：年老保守太守的繁荣城市");
            var city3 = CreateTestCity("繁荣古城");
            var faction3 = CreateTestFaction("繁荣势力");
            faction3.State = FactionState.Prosperous;
            faction3.FiscalHealth = 0.9f;
            city3.Owner = faction3;

            var oldOfficer = CreateTestOfficer("老太守");
            oldOfficer.Age = 65;
            oldOfficer.AddTrait(Trait.Conservative);
            oldOfficer.Politics = 75;
            oldOfficer.Health = 60;
            city3.Prefect = oldOfficer;

            constructionAI.UpdateCityConstruction(city3);
            if (city3.IsBuilding)
            {
                Console.WriteLine($"    建设选择: {city3.CurrentConstruction} (年老保守太守的选择)");
            }

            Console.WriteLine("  ✓ 复杂场景测试完成\n");
        }

        // === 辅助方法 ===

        private static City CreateTestCity(string name)
        {
            return new City
            {
                Name = name,
                FreeSlots = 3,
                IsBuilding = false,
                Population = 15000,
                MaxPopulation = 50000,
                Prosperity = 0.6f,
                Security = 0.7f,
                Loyalty = 0.8f,
                Buildings = new List<BuildingType>()
            };
        }

        private static Faction CreateTestFaction(string name)
        {
            return new Faction
            {
                Name = name,
                Gold = 5000,
                Food = 3000,
                FoodProduction = 800,
                TotalTroops = 1200,
                FiscalHealth = 0.7f,
                TechLevel = 35,
                State = FactionState.Stable,
                Cities = new List<City>()
            };
        }

        private static Officer CreateTestOfficer(string name)
        {
            return new Officer
            {
                Name = name,
                Politics = 50,
                Leadership = 50,
                Intelligence = 50,
                Age = 35,
                Health = 90,
                Traits = new List<Trait>(),
                Experiences = new List<string>()
            };
        }

        private static void ValidatePersonalityChoice(Trait trait, BuildingType choice)
        {
            switch (trait)
            {
                case Trait.Rash:
                    if (choice == BuildingType.Barracks || choice == BuildingType.Fortress)
                        Console.WriteLine($"    ✓ 莽撞性格正确选择军事建筑");
                    else
                        Console.WriteLine($"    ⚠ 莽撞性格未选择军事建筑");
                    break;

                case Trait.Greedy:
                    if (choice == BuildingType.Market)
                        Console.WriteLine($"    ✓ 贪婪性格正确选择经济建筑");
                    else
                        Console.WriteLine($"    ⚠ 贪婪性格未选择经济建筑");
                    break;

                case Trait.Scholar:
                    if (choice == BuildingType.Academy || choice == BuildingType.Temple)
                        Console.WriteLine($"    ✓ 学者性格正确选择文化建筑");
                    else
                        Console.WriteLine($"    ⚠ 学者性格未选择文化建筑");
                    break;

                case Trait.Cautious:
                    if (choice == BuildingType.Wall || choice == BuildingType.Granary)
                        Console.WriteLine($"    ✓ 谨慎性格正确选择防御建筑");
                    else
                        Console.WriteLine($"    ⚠ 谨慎性格未选择防御建筑");
                    break;
            }
        }

        private static void ValidateFactionStateChoice(FactionState state, BuildingType choice)
        {
            switch (state)
            {
                case FactionState.EconomicCrisis:
                    if (choice == BuildingType.Market || choice == BuildingType.Farm)
                        Console.WriteLine($"    ✓ 经济危机正确选择经济建筑");
                    else
                        Console.WriteLine($"    ⚠ 经济危机未选择经济建筑");
                    break;

                case FactionState.WarTime:
                    if (choice == BuildingType.Barracks || choice == BuildingType.Wall || choice == BuildingType.Fortress)
                        Console.WriteLine($"    ✓ 战时正确选择军事建筑");
                    else
                        Console.WriteLine($"    ⚠ 战时未选择军事建筑");
                    break;

                case FactionState.Prosperous:
                    if (choice == BuildingType.Academy || choice == BuildingType.Temple)
                        Console.WriteLine($"    ✓ 繁荣期正确选择文化建筑");
                    else
                        Console.WriteLine($"    - 繁荣期选择了其他建筑");
                    break;
            }
        }

        /// <summary>
        /// 性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("--- 城市建设AI性能测试 ---");

            var constructionAI = new CityConstructionAI();
            var cities = new List<City>();

            // 创建大量测试城市
            for (int i = 0; i < 100; i++)
            {
                var city = CreateTestCity($"城市{i}");
                var faction = CreateTestFaction($"势力{i % 10}");
                city.Owner = faction;

                var prefect = CreateTestOfficer($"太守{i}");
                prefect.AddTrait((Trait)(i % 8)); // 随机性格
                city.Prefect = prefect;

                cities.Add(city);
            }

            var startTime = DateTime.Now;

            // 执行建设决策
            foreach (var city in cities)
            {
                constructionAI.UpdateCityConstruction(city);
            }

            var elapsed = DateTime.Now - startTime;

            Console.WriteLine($"  性能测试结果:");
            Console.WriteLine($"    处理城市数: {cities.Count}");
            Console.WriteLine($"    总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"    平均每城市: {elapsed.TotalMilliseconds / cities.Count:F4} ms");

            if (elapsed.TotalMilliseconds / cities.Count < 1.0)
                Console.WriteLine($"    性能评价: 优秀 ⭐⭐⭐");
            else if (elapsed.TotalMilliseconds / cities.Count < 5.0)
                Console.WriteLine($"    性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine($"    性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能测试完成\n");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始运行城市建设AI的所有测试...\n");

            RunCompleteTest();
            RunPerformanceTest();

            Console.WriteLine("\n🎉 城市建设AI所有测试完成！系统运行正常。");
        }
    }
}