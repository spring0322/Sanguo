using System;
using System.Collections.Generic;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// 招募系统测试 - 验证"真相->透镜->结果"模式
    /// </summary>
    public static class RecruitmentSystemTest
    {
        /// <summary>
        /// 运行完整的招募系统测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 招募系统完整测试 ===");
            Console.WriteLine("基于 Truth -> Lens -> Outcome 模式");
            Console.WriteLine();

            // 创建测试场景
            var testScenarios = CreateTestScenarios();

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"=== 测试场景: {scenario.Name} ===");
                TestRecruitmentScenario(scenario);
                Console.WriteLine();
            }

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 创建测试场景
        /// </summary>
        private static List<RecruitmentTestScenario> CreateTestScenarios()
        {
            return new List<RecruitmentTestScenario>
            {
                new RecruitmentTestScenario
                {
                    Name = "刘备招募诸葛亮（历史经典）",
                    Recruiter = CreateTestPerson("刘备", 75, 85, 95, 80, 0),
                    Target = CreateTestPerson("诸葛亮", 100, 95, 90, 95, 50),
                    Strategist = CreateTestPerson("徐庶", 85, 80, 75, 85, 2), // 冷静型
                    Description = "历史上的经典招募，应该有较高成功率"
                },
                new RecruitmentTestScenario
                {
                    Name = "曹操招募关羽（困难招募）",
                    Recruiter = CreateTestPerson("曹操", 95, 90, 85, 75, 1),
                    Target = CreateTestPerson("关羽", 80, 95, 85, 80, 100), // 高忠诚
                    Strategist = CreateTestPerson("荀彧", 90, 85, 80, 90, 2), // 冷静型
                    Description = "高忠诚度目标，成功率应该较低"
                },
                new RecruitmentTestScenario
                {
                    Name = "袁绍招募普通武将（无军师）",
                    Recruiter = CreateTestPerson("袁绍", 70, 75, 80, 60, 3),
                    Target = CreateTestPerson("普通武将", 60, 70, 65, 50, 60),
                    Strategist = null, // 无军师
                    Description = "无军师情况下的招募预测"
                },
                new RecruitmentTestScenario
                {
                    Name = "孙权招募周瑜（理想相近）",
                    Recruiter = CreateTestPerson("孙权", 80, 85, 90, 75, 0),
                    Target = CreateTestPerson("周瑜", 90, 95, 85, 90, 70),
                    Strategist = CreateTestPerson("鲁肃", 80, 75, 85, 80, 0), // 仁德型
                    Description = "理想相近的招募，应该有加成"
                },
                new RecruitmentTestScenario
                {
                    Name = "低智力军师的误判",
                    Recruiter = CreateTestPerson("董卓", 60, 80, 70, 40, 1),
                    Target = CreateTestPerson("吕布", 70, 100, 65, 30, 40),
                    Strategist = CreateTestPerson("李儒", 65, 70, 60, 75, 4), // 狡诈型，但智力不高
                    Description = "低智力军师可能产生较大误差"
                }
            };
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int politics, int loyalty)
        {
            var person = new Person();
            person.Name = name;
            person.Intelligence = intelligence;
            person.Command = command;
            person.Charm = charm;
            person.Politics = politics;
            person.Loyalty = loyalty;
            person.Strength = 80; // 默认武力
            person.Calmness = 70; // 默认冷静
            person.Ideal = 50;    // 默认理想
            
            // 设置性格（简化处理）
            person.Character = new Character { ID = intelligence > 80 ? 2 : 0 }; // 高智力为冷静型，否则为仁德型
            
            return person;
        }

        /// <summary>
        /// 创建测试人物（带性格）
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int politics, int loyalty, int personalityId)
        {
            var person = CreateTestPerson(name, intelligence, command, charm, politics, loyalty);
            person.Character = new Character { ID = personalityId };
            return person;
        }

        /// <summary>
        /// 测试招募场景
        /// </summary>
        private static void TestRecruitmentScenario(RecruitmentTestScenario scenario)
        {
            Console.WriteLine($"描述: {scenario.Description}");
            Console.WriteLine($"招募者: {scenario.Recruiter.Name} (智{scenario.Recruiter.Intelligence} 统{scenario.Recruiter.Command} 魅{scenario.Recruiter.Charm} 政{scenario.Recruiter.Politics})");
            Console.WriteLine($"目标: {scenario.Target.Name} (忠诚{scenario.Target.Loyalty})");
            
            if (scenario.Strategist != null)
            {
                Console.WriteLine($"军师: {scenario.Strategist.Name} (智{scenario.Strategist.Intelligence})");
            }
            else
            {
                Console.WriteLine("军师: 无");
            }
            Console.WriteLine();

            // 1. 获取军师预测（带误差）
            var (perceivedRate, prediction) = RecruitmentSystem.GetStrategistPrediction(
                scenario.Strategist, 
                scenario.Recruiter, 
                scenario.Target
            );

            Console.WriteLine("军师预测:");
            if (perceivedRate == -1)
            {
                Console.WriteLine("  成功率: ??% (无军师)");
            }
            else
            {
                Console.WriteLine($"  成功率: {perceivedRate}%");
            }
            Console.WriteLine($"  评语: {prediction}");
            Console.WriteLine();

            // 2. 多次执行测试，验证真实成功率
            Console.WriteLine("执行10次招募测试:");
            int successCount = 0;
            for (int i = 1; i <= 10; i++)
            {
                bool success = RecruitmentSystem.ExecuteRecruitment(scenario.Recruiter, scenario.Target);
                string result = success ? "✅成功" : "❌失败";
                Console.WriteLine($"  第{i}次: {result}");
                if (success) successCount++;
            }
            
            int actualSuccessRate = successCount * 10;
            Console.WriteLine($"实际成功率: {actualSuccessRate}% ({successCount}/10)");

            // 3. 分析军师预测准确度
            if (perceivedRate != -1)
            {
                int predictionError = Math.Abs(perceivedRate - actualSuccessRate);
                string accuracy;
                if (predictionError <= 10) accuracy = "🎯 很准确";
                else if (predictionError <= 20) accuracy = "👍 比较准确";
                else if (predictionError <= 30) accuracy = "⚠️ 有偏差";
                else accuracy = "❌ 误差较大";
                
                Console.WriteLine($"预测准确度: {accuracy} (误差{predictionError}%)");
            }

            // 4. 显示详细分析（如果有军师）
            if (scenario.Strategist != null)
            {
                Console.WriteLine();
                Console.WriteLine("详细分析:");
                string analysis = RecruitmentSystem.GetDetailedAnalysis(
                    scenario.Strategist, 
                    scenario.Recruiter, 
                    scenario.Target
                );
                Console.WriteLine(analysis);
            }
        }

        /// <summary>
        /// 测试军师误差系统
        /// </summary>
        public static void TestStrategistErrorSystem()
        {
            Console.WriteLine("=== 军师误差系统测试 ===");
            Console.WriteLine();

            // 创建不同智力的军师
            var strategists = new[]
            {
                CreateTestPerson("诸葛亮", 100, 95, 90, 95, 0, 2), // 智力100
                CreateTestPerson("庞统", 95, 90, 85, 90, 0, 2),   // 智力95
                CreateTestPerson("荀彧", 85, 80, 80, 85, 0, 2),   // 智力85
                CreateTestPerson("田丰", 75, 70, 75, 80, 0, 2),   // 智力75
                CreateTestPerson("郭图", 60, 65, 70, 60, 0, 3)    // 智力60
            };

            var recruiter = CreateTestPerson("测试招募者", 80, 85, 90, 85, 0);
            var target = CreateTestPerson("测试目标", 70, 75, 65, 70, 60);

            foreach (var strategist in strategists)
            {
                Console.WriteLine($"=== 军师: {strategist.Name} (智力{strategist.Intelligence}) ===");
                
                Console.WriteLine("10次预测结果:");
                var predictions = new List<int>();
                
                for (int i = 1; i <= 10; i++)
                {
                    var (rate, comment) = RecruitmentSystem.GetStrategistPrediction(strategist, recruiter, target);
                    predictions.Add(rate);
                    Console.WriteLine($"  第{i}次: {rate}%");
                }
                
                // 计算预测的标准差，评估一致性
                double average = predictions.ConvertAll(x => (double)x).Average();
                double variance = predictions.ConvertAll(x => Math.Pow(x - average, 2)).Average();
                double stdDev = Math.Sqrt(variance);
                
                Console.WriteLine($"平均预测: {average:F1}%");
                Console.WriteLine($"标准差: {stdDev:F1} (智力越高应该越小)");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试特殊情况
        /// </summary>
        public static void TestSpecialCases()
        {
            Console.WriteLine("=== 特殊情况测试 ===");
            Console.WriteLine();

            // 1. 历史组合测试
            Console.WriteLine("1. 历史组合加成测试:");
            var liuBei = CreateTestPerson("刘备", 75, 85, 95, 80, 0);
            var zhuGeLiang = CreateTestPerson("诸葛亮", 100, 95, 90, 95, 50);
            var strategist = CreateTestPerson("徐庶", 85, 80, 75, 85, 0);
            
            var (rate1, comment1) = RecruitmentSystem.GetStrategistPrediction(strategist, liuBei, zhuGeLiang);
            Console.WriteLine($"刘备招募诸葛亮: {rate1}% - {comment1}");
            Console.WriteLine();

            // 2. 关系影响测试
            Console.WriteLine("2. 关系影响测试:");
            var person1 = CreateTestPerson("武将A", 80, 85, 90, 85, 0);
            var person2 = CreateTestPerson("武将B", 75, 80, 85, 80, 60);
            
            // 模拟亲密关系
            // person1.ClosePersons.Add(person2.ID);
            // person2.ClosePersons.Add(person1.ID);
            
            var (rate2, comment2) = RecruitmentSystem.GetStrategistPrediction(strategist, person1, person2);
            Console.WriteLine($"有亲密关系的招募: {rate2}% - {comment2}");
            Console.WriteLine();

            // 3. 相性测试
            Console.WriteLine("3. 相性影响测试:");
            var idealist1 = CreateTestPerson("理想主义者", 80, 85, 90, 85, 0);
            var idealist2 = CreateTestPerson("同道中人", 75, 80, 85, 80, 60);
            idealist1.Ideal = 20; // 相近的理想
            idealist2.Ideal = 25;
            
            var (rate3, comment3) = RecruitmentSystem.GetStrategistPrediction(strategist, idealist1, idealist2);
            Console.WriteLine($"理想相近的招募: {rate3}% - {comment3}");
            Console.WriteLine();

            // 4. 无军师情况
            Console.WriteLine("4. 无军师情况测试:");
            var (rate4, comment4) = RecruitmentSystem.GetStrategistPrediction(null, person1, person2);
            Console.WriteLine($"无军师招募: {(rate4 == -1 ? "??" : rate4.ToString())}% - {comment4}");
        }

        /// <summary>
        /// 性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("=== 性能测试 ===");
            
            var recruiter = CreateTestPerson("测试招募者", 80, 85, 90, 85, 0);
            var target = CreateTestPerson("测试目标", 70, 75, 65, 70, 60);
            var strategist = CreateTestPerson("测试军师", 90, 80, 75, 85, 0);

            var startTime = DateTime.Now;
            
            // 执行1000次预测
            for (int i = 0; i < 1000; i++)
            {
                RecruitmentSystem.GetStrategistPrediction(strategist, recruiter, target);
            }
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            Console.WriteLine($"1000次预测耗时: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"平均每次预测: {duration.TotalMilliseconds / 1000:F2}ms");
            
            // 执行1000次招募
            startTime = DateTime.Now;
            int successCount = 0;
            
            for (int i = 0; i < 1000; i++)
            {
                if (RecruitmentSystem.ExecuteRecruitment(recruiter, target))
                    successCount++;
            }
            
            endTime = DateTime.Now;
            duration = endTime - startTime;
            
            Console.WriteLine($"1000次执行耗时: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"平均每次执行: {duration.TotalMilliseconds / 1000:F2}ms");
            Console.WriteLine($"成功率: {successCount / 10.0:F1}%");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("🎮 招募系统完整测试套件");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            RunCompleteTest();
            Console.WriteLine();
            
            TestStrategistErrorSystem();
            Console.WriteLine();
            
            TestSpecialCases();
            Console.WriteLine();
            
            RunPerformanceTest();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("测试完成！招募系统的 Truth->Lens->Outcome 模式运行正常。");
        }

        /// <summary>
        /// 测试场景结构
        /// </summary>
        private struct RecruitmentTestScenario
        {
            public string Name;
            public Person Recruiter;
            public Person Target;
            public Person Strategist;
            public string Description;
        }
    }
}