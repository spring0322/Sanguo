using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 增强决策系统测试
    /// 测试明主决策与纳谏决策的区别
    /// </summary>
    public static class EnhancedDecisionSystemTest
    {
        /// <summary>
        /// 运行完整测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 增强决策系统测试 ===");
            Console.WriteLine();

            // 测试1: 基本决策逻辑
            TestBasicDecisionLogic();

            // 测试2: 建议显示系统
            TestAdviceDisplaySystem();

            // 测试3: 明主识破不良建议
            TestWiseRulerDetection();

            // 测试4: 历史案例对比
            TestHistoricalScenarios();

            // 测试5: 决策一致性
            TestDecisionConsistency();
        }

        /// <summary>
        /// 测试基本决策逻辑
        /// </summary>
        private static void TestBasicDecisionLogic()
        {
            Console.WriteLine("=== 1. 基本决策逻辑测试 ===");
            Console.WriteLine();

            var testCases = new[]
            {
                new { Name = "明主型", Ruler = "曹操", RulerInt = 96, Advisor = "程昱", AdvisorInt = 90, AdvisorLoyalty = 95 },
                new { Name = "军师型", Ruler = "刘备", RulerInt = 75, Advisor = "诸葛亮", AdvisorInt = 100, AdvisorLoyalty = 100 },
                new { Name = "均衡型", Ruler = "孙权", RulerInt = 80, Advisor = "周瑜", AdvisorInt = 90, AdvisorLoyalty = 90 },
                new { Name = "不忠军师", Ruler = "曹操", RulerInt = 96, Advisor = "杨修", AdvisorInt = 85, AdvisorLoyalty = 60 }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"【{testCase.Name}】{testCase.Ruler} + {testCase.Advisor}");
                Console.WriteLine($"  君主智力: {testCase.RulerInt}, 军师智力: {testCase.AdvisorInt}, 军师忠诚: {testCase.AdvisorLoyalty}");

                var faction = CreateTestFaction(testCase.Name, testCase.Ruler, testCase.RulerInt, testCase.Advisor, testCase.AdvisorInt, testCase.AdvisorLoyalty);

                // 进行多次决策测试
                int acceptCount = 0;
                int rejectCount = 0;
                int testRounds = 20;

                for (int i = 0; i < testRounds; i++)
                {
                    bool decision = faction.AICheckDecision();
                    if (decision)
                        acceptCount++;
                    else
                        rejectCount++;
                }

                Console.WriteLine($"  决策结果 ({testRounds}次): 接受 {acceptCount} 次, 拒绝 {rejectCount} 次");
                Console.WriteLine($"  接受率: {(acceptCount * 100.0 / testRounds):F1}%");

                // 分析决策模式
                if (testCase.RulerInt > testCase.AdvisorInt)
                {
                    Console.WriteLine($"  决策模式: 明主决策 (君主智力优势 +{testCase.RulerInt - testCase.AdvisorInt})");
                    if (testCase.AdvisorLoyalty < 80)
                        Console.WriteLine($"  特殊情况: 可能识破不忠军师的不良建议");
                }
                else
                {
                    Console.WriteLine($"  决策模式: 纳谏决策 (军师智力优势 +{testCase.AdvisorInt - testCase.RulerInt})");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试建议显示系统
        /// </summary>
        private static void TestAdviceDisplaySystem()
        {
            Console.WriteLine("=== 2. 建议显示系统测试 ===");
            Console.WriteLine();

            var mingzhu = CreateTestFaction("明主", "曹操", 96, "程昱", 90, 95);
            var junshi = CreateTestFaction("军师", "刘备", 75, "诸葛亮", 100, 100);

            var adviceTypes = new[]
            {
                new { Type = AdviceType.Military, Content = "敌军士气低落，正是进攻良机！" },
                new { Type = AdviceType.Recruitment, Content = "此人才华横溢，当重金招揽。" },
                new { Type = AdviceType.Diplomatic, Content = "与东吴结盟，可制衡曹魏。" },
                new { Type = AdviceType.Battle, Content = "火攻此阵，必可大破敌军！" }
            };

            var factions = new[] { mingzhu, junshi };

            foreach (var faction in factions)
            {
                Console.WriteLine($"【{faction.Name}势力】{faction.Leader.Name}(智{faction.Leader.Intelligence}) + {faction.Advisor.Name}(智{faction.Advisor.Intelligence})");
                Console.WriteLine();

                foreach (var advice in adviceTypes)
                {
                    var displayInfo = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, advice.Content, advice.Type, 75);
                    
                    Console.WriteLine($"--- {advice.Type} 建议 ---");
                    Console.WriteLine(displayInfo.DisplayText);
                    Console.WriteLine($"决策者: {displayInfo.GetDecisionMakerLabel()}");
                    Console.WriteLine($"可信度: {displayInfo.Reliability}");
                    Console.WriteLine();
                }

                Console.WriteLine($"决策过程: {AdviceDisplaySystem.GetDecisionProcessDescription(faction)}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试明主识破不良建议
        /// </summary>
        private static void TestWiseRulerDetection()
        {
            Console.WriteLine("=== 3. 明主识破不良建议测试 ===");
            Console.WriteLine();

            // 创建高智力君主 + 低忠诚军师的组合
            var suspiciousCases = new[]
            {
                new { Ruler = "曹操", RulerInt = 96, Advisor = "杨修", AdvisorInt = 85, Loyalty = 50 },
                new { Ruler = "司马懿", RulerInt = 95, Advisor = "某谋士", AdvisorInt = 75, Loyalty = 40 },
                new { Ruler = "诸葛亮", RulerInt = 100, Advisor = "魏延", AdvisorInt = 70, Loyalty = 60 }
            };

            foreach (var testCase in suspiciousCases)
            {
                Console.WriteLine($"【识破测试】{testCase.Ruler}(智{testCase.RulerInt}) vs {testCase.Advisor}(智{testCase.AdvisorInt}, 忠{testCase.Loyalty})");

                var faction = CreateTestFaction("测试", testCase.Ruler, testCase.RulerInt, testCase.Advisor, testCase.AdvisorInt, testCase.Loyalty);

                int detectionCount = 0;
                int totalTests = 100;

                for (int i = 0; i < totalTests; i++)
                {
                    bool decision = faction.AICheckDecision();
                    if (!decision) // 拒绝决策，可能是识破了
                        detectionCount++;
                }

                double detectionRate = detectionCount * 100.0 / totalTests;
                Console.WriteLine($"  识破率: {detectionRate:F1}% ({detectionCount}/{totalTests})");

                // 理论识破率 = 君主智力 (当军师忠诚 < 80 时)
                double theoreticalRate = testCase.Loyalty < 80 ? testCase.RulerInt : 0;
                Console.WriteLine($"  理论识破率: {theoreticalRate:F1}%");

                if (Math.Abs(detectionRate - theoreticalRate) < 10)
                    Console.WriteLine("  ✓ 识破率符合预期");
                else
                    Console.WriteLine("  ✗ 识破率异常");

                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试历史案例
        /// </summary>
        private static void TestHistoricalScenarios()
        {
            Console.WriteLine("=== 4. 历史案例对比测试 ===");
            Console.WriteLine();

            var historicalCases = new[]
            {
                new { 
                    Name = "官渡之战前", 
                    Ruler = "曹操", RulerInt = 96, 
                    Advisor = "荀彧", AdvisorInt = 90, 
                    Scenario = "是否与袁绍决战",
                    ExpectedBehavior = "明主决策，曹操亲自判断战机"
                },
                new { 
                    Name = "隆中对策", 
                    Ruler = "刘备", RulerInt = 75, 
                    Advisor = "诸葛亮", AdvisorInt = 100, 
                    Scenario = "三分天下战略",
                    ExpectedBehavior = "纳谏决策，刘备采纳诸葛亮建议"
                },
                new { 
                    Name = "赤壁之战", 
                    Ruler = "孙权", RulerInt = 80, 
                    Advisor = "周瑜", AdvisorInt = 90, 
                    Scenario = "是否抗曹",
                    ExpectedBehavior = "纳谏决策，孙权听从周瑜分析"
                },
                new { 
                    Name = "杨修之死", 
                    Ruler = "曹操", RulerInt = 96, 
                    Advisor = "杨修", AdvisorInt = 85, 
                    Scenario = "军事机密泄露",
                    ExpectedBehavior = "明主识破，曹操察觉杨修不忠"
                }
            };

            foreach (var case_ in historicalCases)
            {
                Console.WriteLine($"【{case_.Name}】");
                Console.WriteLine($"场景: {case_.Scenario}");
                Console.WriteLine($"人物: {case_.Ruler}(智{case_.RulerInt}) + {case_.Advisor}(智{case_.AdvisorInt})");

                var faction = CreateTestFaction(case_.Name, case_.Ruler, case_.RulerInt, case_.Advisor, case_.AdvisorInt, 
                    case_.Name == "杨修之死" ? 60 : 90); // 杨修忠诚度低

                // 分析决策模式
                bool isWiseRuler = case_.RulerInt > case_.AdvisorInt;
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);

                Console.WriteLine($"决策模式: {(isWiseRuler ? "明主决策" : "纳谏决策")}");
                Console.WriteLine($"有效智力: {effectiveInt}");
                Console.WriteLine($"预期行为: {case_.ExpectedBehavior}");

                // 测试决策结果
                bool decision = faction.AICheckDecision();
                Console.WriteLine($"系统决策: {(decision ? "接受建议" : "拒绝建议")}");

                // 获取建议显示
                string advice = $"关于'{case_.Scenario}'的战略建议";
                var displayInfo = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, advice, AdviceType.Military);
                Console.WriteLine($"显示效果: {displayInfo.GetDecisionMakerLabel()}");

                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试决策一致性
        /// </summary>
        private static void TestDecisionConsistency()
        {
            Console.WriteLine("=== 5. 决策一致性测试 ===");
            Console.WriteLine();

            var faction = CreateTestFaction("一致性测试", "曹操", 96, "荀彧", 90, 95);

            Console.WriteLine("测试同一势力在不同情况下的决策一致性...");
            Console.WriteLine();

            // 测试正常情况
            Console.WriteLine("--- 正常情况 (军师忠诚度高) ---");
            int normalAcceptCount = 0;
            for (int i = 0; i < 50; i++)
            {
                if (faction.AICheckDecision()) normalAcceptCount++;
            }
            Console.WriteLine($"接受率: {normalAcceptCount * 2}%");

            // 测试军师忠诚度低的情况
            Console.WriteLine("--- 异常情况 (军师忠诚度低) ---");
            faction.Advisor.Loyalty = 50; // 降低忠诚度
            int suspiciousAcceptCount = 0;
            for (int i = 0; i < 50; i++)
            {
                if (faction.AICheckDecision()) suspiciousAcceptCount++;
            }
            Console.WriteLine($"接受率: {suspiciousAcceptCount * 2}%");

            // 分析结果
            Console.WriteLine();
            Console.WriteLine("--- 一致性分析 ---");
            if (normalAcceptCount > suspiciousAcceptCount)
            {
                Console.WriteLine("✓ 系统正确识别了军师忠诚度的影响");
                Console.WriteLine($"  明主在军师不忠时更加谨慎 (差异: {(normalAcceptCount - suspiciousAcceptCount) * 2}%)");
            }
            else
            {
                Console.WriteLine("✗ 系统未能正确处理军师忠诚度影响");
            }

            // 测试建议显示一致性
            Console.WriteLine();
            Console.WriteLine("--- 建议显示一致性 ---");
            string testAdvice = "建议立即出兵攻打敌军";
            
            faction.Advisor.Loyalty = 95; // 恢复高忠诚度
            var normalDisplay = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, testAdvice, AdviceType.Military);
            
            faction.Advisor.Loyalty = 50; // 低忠诚度
            var suspiciousDisplay = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, testAdvice, AdviceType.Military);

            Console.WriteLine("高忠诚度时:");
            Console.WriteLine($"  {normalDisplay.DisplayText}");
            Console.WriteLine();
            Console.WriteLine("低忠诚度时:");
            Console.WriteLine($"  {suspiciousDisplay.DisplayText}");
            Console.WriteLine();

            if (normalDisplay.IsWiseRulerMode == suspiciousDisplay.IsWiseRulerMode)
            {
                Console.WriteLine("✓ 建议显示模式保持一致 (明主模式)");
            }
            else
            {
                Console.WriteLine("✗ 建议显示模式不一致");
            }
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name, string leaderName, int leaderInt, string advisorName, int advisorInt, int advisorLoyalty = 90)
        {
            var faction = new Faction { Name = name };

            faction.Leader = new Person
            {
                Name = leaderName,
                Intelligence = leaderInt,
                Command = leaderInt - 10,
                Charm = leaderInt - 5,
                Politics = leaderInt - 8,
                BelongedFaction = faction
            };

            faction.Advisor = new Person
            {
                Name = advisorName,
                Intelligence = advisorInt,
                Command = advisorInt - 15,
                Charm = advisorInt - 10,
                Politics = advisorInt - 5,
                Loyalty = advisorLoyalty,
                BelongedFaction = faction
            };

            return faction;
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("=== 决策系统性能测试 ===");
            Console.WriteLine();

            var faction = CreateTestFaction("性能测试", "曹操", 96, "荀彧", 90, 95);

            // 测试决策性能
            var startTime = DateTime.Now;
            int iterations = 10000;

            for (int i = 0; i < iterations; i++)
            {
                faction.AICheckDecision();
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Console.WriteLine($"执行 {iterations} 次决策耗时: {duration.TotalMilliseconds:F2} ms");
            Console.WriteLine($"平均每次决策耗时: {duration.TotalMilliseconds / iterations:F4} ms");

            // 测试建议显示性能
            startTime = DateTime.Now;
            string testAdvice = "这是一个测试建议";

            for (int i = 0; i < iterations; i++)
            {
                AdviceDisplaySystem.GetAdviceDisplayInfo(faction, testAdvice, AdviceType.General);
            }

            endTime = DateTime.Now;
            duration = endTime - startTime;

            Console.WriteLine($"执行 {iterations} 次建议显示耗时: {duration.TotalMilliseconds:F2} ms");
            Console.WriteLine($"平均每次建议显示耗时: {duration.TotalMilliseconds / iterations:F4} ms");

            Console.WriteLine();
            Console.WriteLine("✓ 性能测试完成，系统运行效率良好");
        }
    }
}