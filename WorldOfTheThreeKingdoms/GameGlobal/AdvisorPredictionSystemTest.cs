using System;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 军师预测系统测试类
    /// 验证各种预测功能的准确性和一致性
    /// </summary>
    public static class AdvisorPredictionSystemTest
    {
        /// <summary>
        /// 运行所有预测系统测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== 军师预测系统完整测试 ===");
            Console.WriteLine();

            TestRecruitPrediction();
            Console.WriteLine();

            TestDiplomacyPrediction();
            Console.WriteLine();

            TestBattlePrediction();
            Console.WriteLine();

            TestStratagemPrediction();
            Console.WriteLine();

            TestTechResearchPrediction();
            Console.WriteLine();

            TestPredictionConsistency();
            Console.WriteLine();

            TestPersonalityInfluence();
            Console.WriteLine();

            TestPredictionReports();
        }

        /// <summary>
        /// 测试招募预测功能
        /// </summary>
        public static void TestRecruitPrediction()
        {
            Console.WriteLine("=== 招募预测测试 ===");

            var advisors = new[]
            {
                CreateTestPerson("诸葛亮", 100, 0), // 神算
                CreateTestPerson("庞统", 85, 1),   // 多疑
                CreateTestPerson("杨修", 70, 2),   // 莽撞
                CreateTestPerson("郭图", 55, 0)    // 低智力
            };

            var targets = new[]
            {
                CreateTestPersonWithLoyalty("高忠诚武将", 90),
                CreateTestPersonWithLoyalty("中忠诚武将", 60),
                CreateTestPersonWithLoyalty("低忠诚武将", 30)
            };

            Console.WriteLine("招募成功率预测对比:");
            Console.WriteLine("军师\t\t目标\t\t预测成功率\t描述");
            Console.WriteLine("".PadRight(60, '-'));

            foreach (var advisor in advisors)
            {
                foreach (var target in targets)
                {
                    int predictedChance = AdvisorPredictionSystem.GetRecruitChanceDisplay(advisor, target);
                    string description = AdvisorPredictionSystem.GetRecruitChanceDescription(advisor, target);
                    
                    Console.WriteLine($"{advisor.Name}\t{target.Name}\t{predictedChance}%\t\t{description}");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试外交预测功能
        /// </summary>
        public static void TestDiplomacyPrediction()
        {
            Console.WriteLine("=== 外交预测测试 ===");

            var advisor = CreateTestPerson("测试军师", 80, 0);
            var targetFaction = CreateTestFaction("目标势力");

            var diplomacyTypes = new[] { "Alliance", "Trade", "NonAggression" };

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"目标势力: {targetFaction.Name}");
            Console.WriteLine();

            foreach (string dipType in diplomacyTypes)
            {
                int chance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(advisor, targetFaction, dipType);
                Console.WriteLine($"{dipType}: {chance}%");
            }
        }

        /// <summary>
        /// 测试战斗预测功能
        /// </summary>
        public static void TestBattlePrediction()
        {
            Console.WriteLine("=== 战斗预测测试 ===");

            var advisor = CreateTestPerson("军师", 85, 0);
            var ourLeader = CreateTestPerson("我方主将", 80, 0);
            var enemyLeader = CreateTestPerson("敌方主将", 75, 0);

            var ourTroop = CreateTestTroop("我方部队", ourLeader, 1000);
            var enemyTroop = CreateTestTroop("敌方部队", enemyLeader, 800);

            Console.WriteLine($"军师: {advisor.Name}");
            Console.WriteLine($"我方部队: {ourTroop.Name} (兵力 {ourTroop.Quantity}, 主将 {ourLeader.Name})");
            Console.WriteLine($"敌方部队: {enemyTroop.Name} (兵力 {enemyTroop.Quantity}, 主将 {enemyLeader.Name})");
            Console.WriteLine();

            int battleChance = AdvisorPredictionSystem.GetBattleChanceDisplay(advisor, ourTroop, enemyTroop);
            Console.WriteLine($"预测胜率: {battleChance}%");

            // 测试不同智力军师的预测差异
            var advisors = new[]
            {
                CreateTestPerson("神算", 100, 0),
                CreateTestPerson("高智", 90, 0),
                CreateTestPerson("中智", 70, 0),
                CreateTestPerson("低智", 50, 0)
            };

            Console.WriteLine();
            Console.WriteLine("不同军师的战斗预测:");
            foreach (var testAdvisor in advisors)
            {
                int chance = AdvisorPredictionSystem.GetBattleChanceDisplay(testAdvisor, ourTroop, enemyTroop);
                Console.WriteLine($"{testAdvisor.Name} (智力{testAdvisor.Intelligence}): {chance}%");
            }
        }

        /// <summary>
        /// 测试计谋预测功能
        /// </summary>
        public static void TestStratagemPrediction()
        {
            Console.WriteLine("=== 计谋预测测试 ===");

            var advisor = CreateTestPerson("军师", 85, 0);
            var target = CreateTestPerson("目标武将", 70, 0);

            var stratagemTypes = new[] { "Confusion", "Persuade", "Sabotage" };

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"目标: {target.Name} (智力 {target.Intelligence})");
            Console.WriteLine();

            foreach (string stratagem in stratagemTypes)
            {
                int chance = AdvisorPredictionSystem.GetStratagemChanceDisplay(advisor, target, stratagem);
                Console.WriteLine($"{stratagem}: {chance}%");
            }
        }

        /// <summary>
        /// 测试技术研发预测功能
        /// </summary>
        public static void TestTechResearchPrediction()
        {
            Console.WriteLine("=== 技术研发预测测试 ===");

            var advisor = CreateTestPerson("研发军师", 90, 0);
            var technique = CreateTestTechnique("测试技术");

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"技术: {technique.Name}");
            Console.WriteLine();

            int researchChance = AdvisorPredictionSystem.GetTechResearchChanceDisplay(advisor, technique);
            Console.WriteLine($"预测研发成功率: {researchChance}%");

            // 测试不同智力的影响
            var advisors = new[]
            {
                CreateTestPerson("天才", 100, 0),
                CreateTestPerson("聪明", 85, 0),
                CreateTestPerson("普通", 70, 0),
                CreateTestPerson("愚钝", 50, 0)
            };

            Console.WriteLine();
            Console.WriteLine("不同智力军师的研发预测:");
            foreach (var testAdvisor in advisors)
            {
                int chance = AdvisorPredictionSystem.GetTechResearchChanceDisplay(testAdvisor, technique);
                Console.WriteLine($"{testAdvisor.Name} (智力{testAdvisor.Intelligence}): {chance}%");
            }
        }

        /// <summary>
        /// 测试预测一致性
        /// </summary>
        public static void TestPredictionConsistency()
        {
            Console.WriteLine("=== 预测一致性测试 ===");

            var advisor = CreateTestPerson("测试军师", 75, 0);
            var target = CreateTestPersonWithLoyalty("测试目标", 60);

            Console.WriteLine($"军师: {advisor.Name}");
            Console.WriteLine($"目标: {target.Name}");
            Console.WriteLine();

            Console.WriteLine("连续5次招募预测:");
            for (int i = 1; i <= 5; i++)
            {
                int chance = AdvisorPredictionSystem.GetRecruitChanceDisplay(advisor, target);
                string desc = AdvisorPredictionSystem.GetRecruitChanceDescription(advisor, target);
                Console.WriteLine($"第{i}次: {chance}% ({desc})");
            }

            Console.WriteLine();
            Console.WriteLine("✅ 如果所有结果相同，说明固定种子机制正常工作");
        }

        /// <summary>
        /// 测试性格影响
        /// </summary>
        public static void TestPersonalityInfluence()
        {
            Console.WriteLine("=== 性格影响测试 ===");

            var target = CreateTestPersonWithLoyalty("测试目标", 50);
            
            var advisors = new[]
            {
                CreateTestPerson("仁德军师", 80, 0), // 仁德型
                CreateTestPerson("多疑军师", 80, 1), // 多疑型
                CreateTestPerson("莽撞军师", 80, 2), // 莽撞型
                CreateTestPerson("狡诈军师", 80, 3), // 狡诈型
                CreateTestPerson("普通军师", 80, 99)  // 其他性格
            };

            Console.WriteLine($"目标: {target.Name} (忠诚度 {target.Loyalty})");
            Console.WriteLine("相同智力不同性格的招募预测:");
            Console.WriteLine();

            foreach (var advisor in advisors)
            {
                int chance = AdvisorPredictionSystem.GetRecruitChanceDisplay(advisor, target);
                string desc = AdvisorPredictionSystem.GetRecruitChanceDescription(advisor, target);
                string personality = GetPersonalityName(advisor.CharacterKindID);
                
                Console.WriteLine($"{advisor.Name} ({personality}):");
                Console.WriteLine($"  预测成功率: {chance}%");
                Console.WriteLine($"  描述: {desc}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试预测报告功能
        /// </summary>
        public static void TestPredictionReports()
        {
            Console.WriteLine("=== 预测报告测试 ===");

            var advisor = CreateTestPerson("诸葛亮", 95, 0);
            var recruitTarget = CreateTestPersonWithLoyalty("关羽", 80);

            Console.WriteLine("招募预测报告:");
            string recruitReport = AdvisorPredictionSystem.GetPredictionReport(advisor, "recruit", recruitTarget);
            Console.WriteLine(recruitReport);

            Console.WriteLine();
            Console.WriteLine("外交预测报告:");
            var targetFaction = CreateTestFaction("东吴");
            string diplomacyReport = AdvisorPredictionSystem.GetPredictionReport(advisor, "diplomacy", targetFaction);
            Console.WriteLine(diplomacyReport);
        }

        #region 辅助方法

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int characterKindID)
        {
            var person = new Person();
            person.Name = name;
            person.Intelligence = intelligence;
            person.CharacterKindID = characterKindID;
            person.ID = name.GetHashCode();
            person.Command = intelligence - 10; // 简化设置
            return person;
        }

        /// <summary>
        /// 创建带忠诚度的测试人物
        /// </summary>
        private static Person CreateTestPersonWithLoyalty(string name, int loyalty)
        {
            var person = CreateTestPerson(name, 70, 0);
            person.PersonalLoyalty = loyalty / 20; // 简化设置
            return person;
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name)
        {
            var faction = new Faction();
            faction.Name = name;
            faction.ID = name.GetHashCode();
            faction.Leader = CreateTestPerson($"{name}君主", 75, 0);
            return faction;
        }

        /// <summary>
        /// 创建测试部队
        /// </summary>
        private static Troop CreateTestTroop(string name, Person leader, int quantity)
        {
            var troop = new Troop();
            troop.Name = name;
            troop.Leader = leader;
            troop.Quantity = quantity;
            troop.ID = name.GetHashCode();
            return troop;
        }

        /// <summary>
        /// 创建测试技术
        /// </summary>
        private static Technique CreateTestTechnique(string name)
        {
            var technique = new Technique();
            technique.Name = name;
            technique.ID = name.GetHashCode();
            return technique;
        }

        /// <summary>
        /// 获取性格名称
        /// </summary>
        private static string GetPersonalityName(int characterKindID)
        {
            return characterKindID switch
            {
                0 => "仁德型",
                1 => "多疑型",
                2 => "莽撞型",
                3 => "狡诈型",
                _ => "普通型"
            };
        }

        #endregion

        /// <summary>
        /// 演示实际游戏场景
        /// </summary>
        public static void DemonstrateGameScenarios()
        {
            Console.WriteLine("=== 实际游戏场景演示 ===");

            // 场景1: 刘备想招募关羽
            Console.WriteLine("场景1: 刘备势力，诸葛亮预测招募关羽");
            var zhuge = CreateTestPerson("诸葛亮", 100, 0);
            var guanyu = CreateTestPersonWithLoyalty("关羽", 85);
            
            int recruitChance = AdvisorPredictionSystem.GetRecruitChanceDisplay(zhuge, guanyu);
            string recruitDesc = AdvisorPredictionSystem.GetRecruitChanceDescription(zhuge, guanyu);
            
            Console.WriteLine($"诸葛亮预测: 招募关羽成功率 {recruitChance}% ({recruitDesc})");
            Console.WriteLine();

            // 场景2: 袁绍想与曹操结盟
            Console.WriteLine("场景2: 袁绍势力，田丰预测与曹操结盟");
            var tianfeng = CreateTestPerson("田丰", 85, 1); // 多疑型
            var caocao = CreateTestFaction("曹操");
            
            int allianceChance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(tianfeng, caocao, "Alliance");
            Console.WriteLine($"田丰预测: 与曹操结盟成功率 {allianceChance}%");
            Console.WriteLine();

            // 场景3: 完整预测报告
            Console.WriteLine("场景3: 完整的招募预测报告");
            string fullReport = AdvisorPredictionSystem.GetPredictionReport(zhuge, "recruit", guanyu);
            Console.WriteLine(fullReport);
        }
    }
}