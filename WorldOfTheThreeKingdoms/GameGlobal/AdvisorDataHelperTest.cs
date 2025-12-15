using System;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// AdvisorDataHelper 测试类
    /// 演示通用观测系统的各种功能
    /// </summary>
    public static class AdvisorDataHelperTest
    {
        /// <summary>
        /// 运行完整的通用观测系统测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== AdvisorDataHelper 通用观测系统测试 ===");
            Console.WriteLine();

            TestBasicObservation();
            Console.WriteLine();

            TestMultipleProperties();
            Console.WriteLine();

            TestConsistencyAcrossProperties();
            Console.WriteLine();

            TestIntelligenceEffect();
            Console.WriteLine();

            TestPersonalityBias();
            Console.WriteLine();

            TestDisplayFormats();
            Console.WriteLine();

            TestBatchObservation();
        }

        /// <summary>
        /// 测试基础观测功能
        /// </summary>
        public static void TestBasicObservation()
        {
            Console.WriteLine("=== 基础观测功能测试 ===");

            var advisor = CreateTestPerson("测试军师", 75, 0);
            var target = CreateTestPersonWithStats("测试武将", 80, 70, 85, 60, 75);

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"目标: {target.Name}");
            Console.WriteLine($"真实忠诚度: {target.Loyalty}");
            Console.WriteLine();

            // 测试多次观测的一致性
            Console.WriteLine("连续5次观测忠诚度:");
            for (int i = 1; i <= 5; i++)
            {
                int observed = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
                string display = AdvisorDataHelper.GetLoyaltyString(advisor, target);
                Console.WriteLine($"第{i}次: 观测值 {observed}, 显示为 '{display}'");
            }

            Console.WriteLine();
            Console.WriteLine("✅ 如果所有结果相同，说明固定种子机制正常工作");
        }

        /// <summary>
        /// 测试多属性观测
        /// </summary>
        public static void TestMultipleProperties()
        {
            Console.WriteLine("=== 多属性观测测试 ===");

            var advisor = CreateTestPerson("诸葛亮", 85, 0);
            var target = CreateTestPersonWithStats("关羽", 95, 98, 85, 70, 90);

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"目标: {target.Name}");
            Console.WriteLine();

            // 测试各种属性的观测
            var properties = new[]
            {
                ("忠诚度", target.Loyalty, "Loyalty"),
                ("智力", target.Intelligence, "Intelligence"),
                ("统率", target.Command, "Command"),
                ("武力", target.Strength, "Strength"),
                ("政治", target.Politics, "Politics")
            };

            Console.WriteLine("属性观测结果:");
            foreach (var (name, realValue, propertyName) in properties)
            {
                int observed = AdvisorDataHelper.GetObservedValue(advisor, target, realValue, propertyName);
                int error = Math.Abs(observed - realValue);
                Console.WriteLine($"{name}: 真实值 {realValue} → 观测值 {observed} (误差 {error})");
            }

            Console.WriteLine();
            Console.WriteLine("专用方法测试:");
            Console.WriteLine($"忠诚度显示: {AdvisorDataHelper.GetLoyaltyString(advisor, target)}");
            Console.WriteLine($"野心度观测: {AdvisorDataHelper.GetObservedAmbition(advisor, target)}");
            Console.WriteLine($"智力观测: {AdvisorDataHelper.GetObservedAbility(advisor, target, "Intelligence")}");
        }

        /// <summary>
        /// 测试不同属性间的一致性
        /// </summary>
        public static void TestConsistencyAcrossProperties()
        {
            Console.WriteLine("=== 属性间一致性测试 ===");

            var advisor = CreateTestPerson("庞统", 80, 0);
            var target = CreateTestPersonWithStats("张飞", 85, 65, 95, 40, 70);

            Console.WriteLine($"军师: {advisor.Name}");
            Console.WriteLine($"目标: {target.Name}");
            Console.WriteLine();

            Console.WriteLine("验证同一属性多次观测的一致性:");
            
            // 测试忠诚度
            int loyalty1 = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
            int loyalty2 = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
            Console.WriteLine($"忠诚度: 第1次 {loyalty1}, 第2次 {loyalty2} - {(loyalty1 == loyalty2 ? "一致" : "不一致")}");

            // 测试智力
            int intel1 = AdvisorDataHelper.GetObservedAbility(advisor, target, "Intelligence");
            int intel2 = AdvisorDataHelper.GetObservedAbility(advisor, target, "Intelligence");
            Console.WriteLine($"智力: 第1次 {intel1}, 第2次 {intel2} - {(intel1 == intel2 ? "一致" : "不一致")}");

            Console.WriteLine();
            Console.WriteLine("验证不同属性有不同的偏差:");
            Console.WriteLine($"忠诚度偏差: {loyalty1 - target.Loyalty}");
            Console.WriteLine($"智力偏差: {intel1 - target.Intelligence}");
            Console.WriteLine($"统率偏差: {AdvisorDataHelper.GetObservedAbility(advisor, target, "Command") - target.Command}");
        }

        /// <summary>
        /// 测试智力对观测准确度的影响
        /// </summary>
        public static void TestIntelligenceEffect()
        {
            Console.WriteLine("=== 智力影响测试 ===");

            var target = CreateTestPersonWithStats("测试目标", 75, 80, 70, 85, 65);
            
            var advisors = new[]
            {
                CreateTestPerson("神算", 100, 0),
                CreateTestPerson("高智", 90, 0),
                CreateTestPerson("中智", 75, 0),
                CreateTestPerson("低智", 60, 0),
                CreateTestPerson("愚钝", 40, 0)
            };

            Console.WriteLine($"目标真实忠诚度: {target.Loyalty}");
            Console.WriteLine();

            foreach (var advisor in advisors)
            {
                int observed = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
                string display = AdvisorDataHelper.GetLoyaltyString(advisor, target);
                string accuracy = AdvisorDataHelper.GetAccuracyAssessment(advisor);
                int error = Math.Abs(observed - target.Loyalty);

                Console.WriteLine($"{advisor.Name} (智力 {advisor.Intelligence}):");
                Console.WriteLine($"  观测值: {observed} (误差 {error})");
                Console.WriteLine($"  显示为: '{display}'");
                Console.WriteLine($"  准确度: {accuracy}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试性格偏向影响
        /// </summary>
        public static void TestPersonalityBias()
        {
            Console.WriteLine("=== 性格偏向测试 ===");

            var target = CreateTestPersonWithStats("测试目标", 60, 75, 80, 70, 65);
            
            var advisors = new[]
            {
                CreateTestPerson("仁德军师", 80, 0), // 仁德型
                CreateTestPerson("多疑军师", 80, 1), // 多疑型
                CreateTestPerson("莽撞军师", 80, 2), // 莽撞型
                CreateTestPerson("普通军师", 80, 99)  // 其他性格
            };

            Console.WriteLine($"目标真实忠诚度: {target.Loyalty} (较低值，测试仁德型高估效果)");
            Console.WriteLine();

            foreach (var advisor in advisors)
            {
                int baseObserved = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
                string personality = GetPersonalityName(advisor.CharacterKindID);
                int bias = baseObserved - target.Loyalty;

                Console.WriteLine($"{advisor.Name} ({personality}):");
                Console.WriteLine($"  观测值: {baseObserved}");
                Console.WriteLine($"  偏差: {bias:+0;-0;0}");
                Console.WriteLine($"  显示: '{AdvisorDataHelper.GetLoyaltyString(advisor, target)}'");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试不同显示格式
        /// </summary>
        public static void TestDisplayFormats()
        {
            Console.WriteLine("=== 显示格式测试 ===");

            var target = CreateTestPersonWithStats("测试目标", 75, 80, 70, 85, 65);

            var testCases = new[]
            {
                (CreateTestPerson("神算", 100, 0), "应显示精确数值"),
                (CreateTestPerson("高智", 85, 0), "应显示数值"),
                (CreateTestPerson("中智", 70, 0), "应显示区间"),
                (CreateTestPerson("低智", 50, 0), "应显示文字描述")
            };

            Console.WriteLine($"目标真实忠诚度: {target.Loyalty}");
            Console.WriteLine();

            foreach (var (advisor, expected) in testCases)
            {
                string display = AdvisorDataHelper.GetLoyaltyString(advisor, target);
                int observed = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
                
                Console.WriteLine($"{advisor.Name} (智力 {advisor.Intelligence}):");
                Console.WriteLine($"  观测值: {observed}");
                Console.WriteLine($"  显示为: '{display}'");
                Console.WriteLine($"  说明: {expected}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试批量观测功能
        /// </summary>
        public static void TestBatchObservation()
        {
            Console.WriteLine("=== 批量观测测试 ===");

            var advisor = CreateTestPerson("测试军师", 80, 1); // 多疑型
            var faction = new Faction();
            faction.Name = "测试势力";
            faction.Persons = new PersonList();

            // 创建测试人员
            var persons = new[]
            {
                CreateTestPersonWithStats("忠臣甲", 95, 85, 75, 80, 90),
                CreateTestPersonWithStats("忠臣乙", 85, 90, 80, 75, 85),
                CreateTestPersonWithStats("普通武将", 70, 75, 85, 70, 75),
                CreateTestPersonWithStats("可疑人员", 45, 80, 70, 85, 60),
                CreateTestPersonWithStats("叛徒", 20, 70, 75, 90, 50)
            };

            foreach (var person in persons)
            {
                faction.Persons.Add(person);
            }

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence}, 多疑型)");
            Console.WriteLine($"势力: {faction.Name}");
            Console.WriteLine();

            var observations = AdvisorDataHelper.GetFactionObservationData(advisor, faction);

            Console.WriteLine("批量观测结果:");
            Console.WriteLine("姓名\t\t真实忠诚\t观测忠诚\t显示格式\t\t观测智力\t观测统率");
            Console.WriteLine("".PadRight(80, '-'));

            foreach (var kvp in observations)
            {
                var person = kvp.Key;
                var data = kvp.Value;
                
                Console.WriteLine($"{person.Name}\t\t{person.Loyalty}\t\t{data.ObservedLoyalty}\t\t{data.LoyaltyDisplay}\t\t{data.ObservedIntelligence}\t\t{data.ObservedCommand}");
            }
        }

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
            return person;
        }

        /// <summary>
        /// 创建带有完整属性的测试人物
        /// </summary>
        private static Person CreateTestPersonWithStats(string name, int loyalty, int intelligence, int command, int politics, int strength)
        {
            var person = new Person();
            person.Name = name;
            person.PersonalLoyalty = loyalty / 20; // 简化的忠诚度设置
            person.Intelligence = intelligence;
            person.Command = command;
            person.Politics = politics;
            person.Strength = strength;
            person.ID = name.GetHashCode();
            return person;
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

        /// <summary>
        /// 演示与现有系统的对比
        /// </summary>
        public static void CompareWithExistingSystem()
        {
            Console.WriteLine("=== 系统对比演示 ===");

            var advisor = CreateTestPerson("对比军师", 80, 1);
            var target = CreateTestPersonWithStats("对比目标", 75, 85, 80, 70, 90);

            Console.WriteLine($"军师: {advisor.Name} (智力 {advisor.Intelligence})");
            Console.WriteLine($"目标: {target.Name} (真实忠诚度 {target.Loyalty})");
            Console.WriteLine();

            // 使用新系统
            int newObserved = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
            string newDisplay = AdvisorDataHelper.GetLoyaltyString(advisor, target);

            Console.WriteLine("AdvisorDataHelper (新系统):");
            Console.WriteLine($"  观测值: {newObserved}");
            Console.WriteLine($"  显示为: '{newDisplay}'");
            Console.WriteLine($"  准确度评估: {AdvisorDataHelper.GetAccuracyAssessment(advisor)}");
            Console.WriteLine();

            // 如果需要，也可以调用原有系统进行对比
            Console.WriteLine("两个系统的特点对比:");
            Console.WriteLine("AdvisorDataHelper:");
            Console.WriteLine("  + 支持多种属性观测");
            Console.WriteLine("  + 更简洁的API设计");
            Console.WriteLine("  + 统一的种子机制");
            Console.WriteLine("  + 性格影响更直接");
            Console.WriteLine();
            Console.WriteLine("AdvisorPerceptionSystem:");
            Console.WriteLine("  + 更详细的分析功能");
            Console.WriteLine("  + 复杂的关系判断");
            Console.WriteLine("  + 信息准确性评估");
            Console.WriteLine("  + 完整的测试覆盖");
        }
    }
}