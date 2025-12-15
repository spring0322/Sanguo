using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师感知系统测试类
    /// 演示不同智力和性格的军师如何感知忠诚度
    /// </summary>
    public static class AdvisorPerceptionSystemTest
    {
        /// <summary>
        /// 测试信息准确性判断功能
        /// </summary>
        public static void TestInfoAccuracy()
        {
            Console.WriteLine("=== 信息准确性测试 ===");
            Console.WriteLine("测试IsInfoAccurate方法的各种情况");
            Console.WriteLine();

            // 创建测试军师和目标
            var advisor = CreateTestPerson("测试军师", 85, 0);
            var target = CreateTestPersonWithLoyalty("测试武将", 75);
            
            // 创建测试势力
            var faction = new Faction();
            faction.Name = "测试势力";
            advisor.BelongedFaction = faction;
            faction.Advisor = advisor;
            
            var leader = CreateTestPerson("测试君主", 70, 0);
            faction.Leader = leader;

            Console.WriteLine("测试场景:");
            Console.WriteLine();

            // 场景1: 军师看自己
            bool accurate1 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, advisor);
            Console.WriteLine($"1. 军师看自己: {accurate1} (应该为true - 关系亲密)");

            // 场景2: 同势力高智力军师
            target.BelongedFaction = faction;
            advisor.Intelligence = 95;
            bool accurate2 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"2. 高智力军师看同势力人员: {accurate2} (应该为true - 智力95+)");

            // 场景3: 中等智力军师看同势力
            advisor.Intelligence = 80;
            bool accurate3 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"3. 中等智力军师看同势力人员: {accurate3} (可能为false - 智力不够高)");

            // 场景4: 看陌生势力人员
            target.BelongedFaction = null;
            bool accurate4 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"4. 军师看陌生势力人员: {accurate4} (应该为false - 无情报)");

            // 场景5: 目标是君主亲属
            target.Father = leader;
            bool accurate5 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"5. 目标是君主之子: {accurate5} (应该为true - 君主直系)");

            // 场景6: 目标在己方建筑
            target.Father = null;
            var architecture = new Architecture();
            architecture.BelongedFaction = faction;
            target.LocationArchitecture = architecture;
            bool accurate6 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"6. 目标在己方建筑: {accurate6} (应该为true - 在己方领土)");

            Console.WriteLine();
        }

        /// <summary>
        /// 测试最终感知值功能
        /// </summary>
        public static void TestFinalPerception()
        {
            Console.WriteLine("=== 最终感知值测试 ===");
            Console.WriteLine("测试GetFinalPerceivedLoyalty方法");
            Console.WriteLine();

            var advisor = CreateTestPerson("测试军师", 80, 1); // 多疑型
            var target = CreateTestPersonWithLoyalty("测试武将", 85);

            // 创建势力关系
            var faction = new Faction();
            advisor.BelongedFaction = faction;
            target.BelongedFaction = faction;
            advisor.Intelligence = 95; // 高智力，信息准确

            Console.WriteLine($"目标真实忠诚度: {target.Loyalty}");
            Console.WriteLine($"军师智力: {advisor.Intelligence} (多疑型)");
            Console.WriteLine();

            // 测试准确信息情况
            int finalAccurate = AdvisorPerceptionSystem.GetFinalPerceivedLoyalty(advisor, target);
            bool isAccurate = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            Console.WriteLine($"信息准确性: {isAccurate}");
            Console.WriteLine($"最终感知值: {finalAccurate}");
            Console.WriteLine($"说明: {(isAccurate ? "信息准确，返回真实值" : "信息有偏差，返回修正值")}");
            Console.WriteLine();

            // 测试有偏差信息情况
            advisor.Intelligence = 70; // 降低智力
            target.BelongedFaction = null; // 移除势力关系
            
            int finalBiased = AdvisorPerceptionSystem.GetFinalPerceivedLoyalty(advisor, target);
            bool isAccurate2 = AdvisorPerceptionSystem.IsInfoAccurate(advisor, target);
            int biasedValue = AdvisorPerceptionSystem.GetBiasedLoyalty(advisor, target);
            
            Console.WriteLine("--- 降低智力和移除关系后 ---");
            Console.WriteLine($"信息准确性: {isAccurate2}");
            Console.WriteLine($"性格偏向感知值: {biasedValue}");
            Console.WriteLine($"最终感知值: {finalBiased}");
            Console.WriteLine($"说明: {(isAccurate2 ? "信息准确，返回真实值" : "信息有偏差，返回修正值")}");
        }

        /// <summary>
        /// 运行完整的军师感知系统测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== 军师感知系统完整测试 ===");
            Console.WriteLine();

            TestIntelligenceEffect();
            Console.WriteLine();
            
            TestPersonalityBias();
            Console.WriteLine();
            
            TestInfoAccuracy();
            Console.WriteLine();
            
            TestFinalPerception();
            Console.WriteLine();
            
            TestConsistency();
            Console.WriteLine();
            
            TestLoyaltyStringDisplay();
            Console.WriteLine();
            
            TestRealWorldScenarios();
        }

        /// <summary>
        /// 测试智力对感知准确度的影响
        /// </summary>
        public static void TestIntelligenceEffect()
        {
            Console.WriteLine("=== 智力影响测试 ===");
            
            // 创建不同智力的军师
            var advisors = new[]
            {
                CreateTestPerson("诸葛亮", 100, 0), // 神算级别
                CreateTestPerson("庞统", 90, 0),   // 高智力
                CreateTestPerson("荀彧", 80, 0),   // 中等智力
                CreateTestPerson("田丰", 70, 0),   // 较低智力
                CreateTestPerson("郭图", 50, 0)    // 低智力
            };

            // 创建测试目标（真实忠诚度75）
            var target = CreateTestPersonWithLoyalty("测试武将", 75);

            Console.WriteLine($"目标武将真实忠诚度: {target.Loyalty}");
            Console.WriteLine();

            foreach (var advisor in advisors)
            {
                int perceived = AdvisorPerceptionSystem.GetPerceivedLoyalty(advisor, target);
                string display = AdvisorPerceptionSystem.GetLoyaltyString(advisor, target);
                int error = Math.Abs(perceived - target.Loyalty);

                Console.WriteLine($"{advisor.Name} (智力{advisor.Intelligence}):");
                Console.WriteLine($"  感知忠诚度: {perceived}");
                Console.WriteLine($"  显示为: {display}");
                Console.WriteLine($"  误差: {error}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试性格偏向对感知的影响
        /// </summary>
        public static void TestPersonalityBias()
        {
            Console.WriteLine("=== 性格偏向测试 ===");

            // 创建不同性格的军师（相同智力）
            var advisors = new[]
            {
                CreateTestPerson("仁德军师", 80, 0), // 仁德型
                CreateTestPerson("多疑军师", 80, 1), // 多疑型
                CreateTestPerson("莽撞军师", 80, 2), // 莽撞型
                CreateTestPerson("狡诈军师", 80, 3)  // 狡诈型
            };

            // 测试不同忠诚度的目标
            var targets = new[]
            {
                CreateTestPersonWithLoyalty("高忠诚武将", 95),
                CreateTestPersonWithLoyalty("中忠诚武将", 75),
                CreateTestPersonWithLoyalty("低忠诚武将", 30)
            };

            foreach (var target in targets)
            {
                Console.WriteLine($"=== 目标: {target.Name} (真实忠诚度{target.Loyalty}) ===");
                
                foreach (var advisor in advisors)
                {
                    int basePerceived = AdvisorPerceptionSystem.GetPerceivedLoyalty(advisor, target);
                    int biasedPerceived = AdvisorPerceptionSystem.GetBiasedLoyalty(advisor, target);
                    int bias = biasedPerceived - basePerceived;

                    Console.WriteLine($"{advisor.Name}: {basePerceived} → {biasedPerceived} (偏向{bias:+0;-0;0})");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试感知结果的一致性（固定种子效果）
        /// </summary>
        public static void TestConsistency()
        {
            Console.WriteLine("=== 一致性测试 ===");
            Console.WriteLine("验证同一军师多次观察同一人物的结果是否一致");
            Console.WriteLine();

            var advisor = CreateTestPerson("测试军师", 75, 0);
            var target = CreateTestPersonWithLoyalty("测试武将", 60);

            Console.WriteLine($"军师: {advisor.Name} (智力{advisor.Intelligence})");
            Console.WriteLine($"目标: {target.Name} (真实忠诚度{target.Loyalty})");
            Console.WriteLine();

            Console.WriteLine("连续5次观察结果:");
            for (int i = 1; i <= 5; i++)
            {
                int perceived = AdvisorPerceptionSystem.GetPerceivedLoyalty(advisor, target);
                int biased = AdvisorPerceptionSystem.GetBiasedLoyalty(advisor, target);
                string display = AdvisorPerceptionSystem.GetLoyaltyString(advisor, target);

                Console.WriteLine($"第{i}次: 基础感知{perceived}, 性格修正{biased}, 显示'{display}'");
            }

            Console.WriteLine();
            Console.WriteLine("✅ 如果所有结果都相同，说明固定种子机制工作正常");
        }

        /// <summary>
        /// 测试不同智力等级的显示方式
        /// </summary>
        public static void TestLoyaltyStringDisplay()
        {
            Console.WriteLine("=== 显示方式测试 ===");

            var target = CreateTestPersonWithLoyalty("测试武将", 75);

            var testCases = new[]
            {
                (CreateTestPerson("神算军师", 100, 0), "应显示精确数值"),
                (CreateTestPerson("高智军师", 90, 0), "应显示区间范围"),
                (CreateTestPerson("中智军师", 80, 0), "应显示区间范围"),
                (CreateTestPerson("低智军师", 60, 0), "应显示文字描述")
            };

            Console.WriteLine($"目标武将真实忠诚度: {target.Loyalty}");
            Console.WriteLine();

            foreach (var (advisor, expected) in testCases)
            {
                string display = AdvisorPerceptionSystem.GetLoyaltyString(advisor, target);
                Console.WriteLine($"{advisor.Name} (智力{advisor.Intelligence}): '{display}' - {expected}");
            }
        }

        /// <summary>
        /// 测试真实游戏场景
        /// </summary>
        public static void TestRealWorldScenarios()
        {
            Console.WriteLine("=== 真实场景测试 ===");

            // 场景1：刘备和诸葛亮
            Console.WriteLine("场景1: 刘备势力，诸葛亮观察关羽");
            var zhuge = CreateTestPerson("诸葛亮", 100, 0);
            var guanyu = CreateTestPersonWithLoyalty("关羽", 99);
            
            string analysis = AdvisorPerceptionSystem.GetLoyaltyAnalysis(zhuge, guanyu);
            Console.WriteLine(analysis);

            // 场景2：袁绍和郭图
            Console.WriteLine("场景2: 袁绍势力，郭图观察颜良");
            var guotu = CreateTestPerson("郭图", 65, 1); // 多疑型
            var yanliang = CreateTestPersonWithLoyalty("颜良", 85);
            
            analysis = AdvisorPerceptionSystem.GetLoyaltyAnalysis(guotu, yanliang);
            Console.WriteLine(analysis);

            // 场景3：董卓和李儒
            Console.WriteLine("场景3: 董卓势力，李儒观察吕布");
            var liru = CreateTestPerson("李儒", 80, 3); // 狡诈型
            var lvbu = CreateTestPersonWithLoyalty("吕布", 40);
            
            analysis = AdvisorPerceptionSystem.GetLoyaltyAnalysis(liru, lvbu);
            Console.WriteLine(analysis);
        }

        /// <summary>
        /// 创建测试用的人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int characterKindID)
        {
            var person = new Person();
            person.Name = name;
            person.Intelligence = intelligence;
            person.CharacterKindID = characterKindID;
            person.ID = name.GetHashCode(); // 简单的ID生成
            return person;
        }

        /// <summary>
        /// 创建带有指定忠诚度的测试人物
        /// </summary>
        private static Person CreateTestPersonWithLoyalty(string name, int loyalty)
        {
            var person = new Person();
            person.Name = name;
            person.PersonalLoyalty = loyalty / 20; // 简化的忠诚度设置
            person.ID = name.GetHashCode();
            
            // 由于Loyalty是计算属性，我们需要设置相关的基础属性
            // 这里简化处理，实际游戏中Loyalty的计算更复杂
            
            return person;
        }

        /// <summary>
        /// 演示批量感知功能
        /// </summary>
        public static void DemonstrateBatchPerception()
        {
            Console.WriteLine("=== 批量感知演示 ===");

            // 创建测试势力和军师
            var faction = new Faction();
            faction.Name = "测试势力";
            
            var advisor = CreateTestPerson("测试军师", 80, 1); // 多疑型

            // 创建测试人员
            var persons = new[]
            {
                CreateTestPersonWithLoyalty("忠臣甲", 95),
                CreateTestPersonWithLoyalty("忠臣乙", 85),
                CreateTestPersonWithLoyalty("普通武将", 70),
                CreateTestPersonWithLoyalty("可疑人员", 45),
                CreateTestPersonWithLoyalty("叛徒", 20)
            };

            // 模拟势力人员列表
            faction.Persons = new PersonList();
            foreach (var person in persons)
            {
                faction.Persons.Add(person);
            }

            // 获取批量感知结果
            var perceptions = AdvisorPerceptionSystem.GetFactionLoyaltyPerception(advisor, faction);

            Console.WriteLine($"军师 {advisor.Name} 对势力全员的忠诚度感知:");
            Console.WriteLine();

            foreach (var kvp in perceptions)
            {
                var person = kvp.Key;
                var (perceived, display) = kvp.Value;
                
                Console.WriteLine($"{person.Name}: {display} (感知值: {perceived})");
            }
        }
    }
}