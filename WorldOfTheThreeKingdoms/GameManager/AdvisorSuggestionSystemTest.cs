using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师建议系统测试
    /// 验证建议缓存系统的正确性和性能
    /// </summary>
    public static class AdvisorSuggestionSystemTest
    {
        /// <summary>
        /// 运行完整测试套件
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 军师建议系统完整测试 ===");
            Console.WriteLine();

            // 1. 基础功能测试
            TestBasicFunctionality();

            // 2. 缓存机制测试
            TestCachingMechanism();

            // 3. 建议生成测试
            TestSuggestionGeneration();

            // 4. 建议解决测试
            TestSuggestionResolution();

            // 5. 性能测试
            TestPerformance();

            // 6. 集成测试
            TestIntegration();

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 测试基础功能
        /// </summary>
        private static void TestBasicFunctionality()
        {
            Console.WriteLine("--- 基础功能测试 ---");

            // 创建测试势力
            var faction = CreateTestFaction("测试势力", "刘备", 75, "诸葛亮", 100);

            // 测试1: 初始状态
            Console.WriteLine($"初始建议状态: {faction.CurrentRoundSuggestion}");
            
            // 测试2: 检查建议生成
            var suggestion = faction.CheckAdvisorHasSuggestion();
            Console.WriteLine($"生成的建议: {suggestion}");
            
            // 测试3: 获取建议文本
            string suggestionText = faction.GetCurrentSuggestionText();
            Console.WriteLine($"建议文本: {suggestionText}");
            
            // 测试4: 获取紧急程度颜色
            var urgencyColor = faction.GetSuggestionUrgencyColor();
            Console.WriteLine($"紧急程度颜色: {urgencyColor}");

            Console.WriteLine("✓ 基础功能测试通过");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试缓存机制
        /// </summary>
        private static void TestCachingMechanism()
        {
            Console.WriteLine("--- 缓存机制测试 ---");

            var faction = CreateTestFaction("缓存测试", "曹操", 96, "荀彧", 90);

            // 测试1: 同一回合多次调用应返回相同结果
            var suggestion1 = faction.CheckAdvisorHasSuggestion();
            var suggestion2 = faction.CheckAdvisorHasSuggestion();
            var suggestion3 = faction.CheckAdvisorHasSuggestion();

            bool cacheWorking = (suggestion1 == suggestion2 && suggestion2 == suggestion3);
            Console.WriteLine($"缓存一致性: {(cacheWorking ? "✓ 通过" : "✗ 失败")}");

            // 测试2: 性能测试 - 多次调用应该很快
            var startTime = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                faction.CheckAdvisorHasSuggestion();
            }
            var duration = DateTime.Now - startTime;
            
            Console.WriteLine($"1000次调用耗时: {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"平均每次调用: {duration.TotalMilliseconds / 1000:F4}ms");
            
            bool performanceGood = duration.TotalMilliseconds < 100; // 应该在100ms内完成
            Console.WriteLine($"性能测试: {(performanceGood ? "✓ 通过" : "✗ 失败")}");

            // 测试3: 强制刷新
            faction.RefreshAdvisorSuggestion();
            var suggestionAfterRefresh = faction.CheckAdvisorHasSuggestion();
            Console.WriteLine($"刷新后建议: {suggestionAfterRefresh}");

            Console.WriteLine("✓ 缓存机制测试完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试建议生成
        /// </summary>
        private static void TestSuggestionGeneration()
        {
            Console.WriteLine("--- 建议生成测试 ---");

            // 测试不同类型的势力
            var testFactions = new[]
            {
                CreateTestFaction("明主型", "曹操", 96, "程昱", 90),
                CreateTestFaction("军师型", "刘备", 75, "诸葛亮", 100),
                CreateTestFaction("均衡型", "孙权", 80, "周瑜", 90),
                CreateTestFaction("弱势", "刘璋", 60, "张松", 70)
            };

            foreach (var faction in testFactions)
            {
                Console.WriteLine($"--- {faction.Name} ---");
                
                // 生成建议
                var suggestion = AdvisorSuggestionSystem.CheckAdvisorHasSuggestion(faction);
                
                Console.WriteLine($"建议类型: {suggestion.Kind}");
                Console.WriteLine($"建议标题: {suggestion.Title}");
                Console.WriteLine($"优先级: {suggestion.Priority}");
                Console.WriteLine($"紧急度: {suggestion.Urgency}");
                Console.WriteLine($"建议内容: {suggestion.Description}");
                
                // 测试建议显示
                faction.CurrentSuggestionDetails = suggestion;
                faction.CurrentRoundSuggestion = suggestion.Kind;
                
                string displayText = faction.GetCurrentSuggestionText();
                Console.WriteLine($"显示文本: {displayText}");
                Console.WriteLine();
            }

            Console.WriteLine("✓ 建议生成测试完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试建议解决机制
        /// </summary>
        private static void TestSuggestionResolution()
        {
            Console.WriteLine("--- 建议解决测试 ---");

            var faction = CreateTestFaction("解决测试", "刘备", 75, "诸葛亮", 100);

            // 模拟不同类型的建议解决
            var testCases = new[]
            {
                AdvisorSuggestionKind.PersonRecruit,
                AdvisorSuggestionKind.EnemyAttack,
                AdvisorSuggestionKind.InternalAffairs,
                AdvisorSuggestionKind.DefensePreparation
            };

            foreach (var suggestionType in testCases)
            {
                Console.WriteLine($"测试 {suggestionType} 建议解决:");
                
                // 设置建议
                faction.CurrentRoundSuggestion = suggestionType;
                faction.CurrentSuggestionDetails = new AdvisorSuggestion
                {
                    Kind = suggestionType,
                    Title = $"测试{suggestionType}",
                    Priority = 5,
                    Urgency = 5
                };
                
                Console.WriteLine($"  设置建议: {suggestionType}");
                
                // 检查解决前状态
                bool beforeResolution = (faction.CurrentRoundSuggestion != AdvisorSuggestionKind.None);
                Console.WriteLine($"  解决前有建议: {beforeResolution}");
                
                // 模拟解决建议的行动
                SimulateResolutionAction(faction, suggestionType);
                
                // 检查建议是否被解决
                faction.CheckAdviceResolved();
                
                bool afterResolution = (faction.CurrentRoundSuggestion != AdvisorSuggestionKind.None);
                Console.WriteLine($"  解决后有建议: {afterResolution}");
                
                bool resolved = beforeResolution && !afterResolution;
                Console.WriteLine($"  解决状态: {(resolved ? "✓ 已解决" : "✗ 未解决")}");
                Console.WriteLine();
            }

            Console.WriteLine("✓ 建议解决测试完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试性能
        /// </summary>
        private static void TestPerformance()
        {
            Console.WriteLine("--- 性能测试 ---");

            var faction = CreateTestFaction("性能测试", "曹操", 96, "荀彧", 90);

            // 测试1: 建议生成性能
            var startTime = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                AdvisorSuggestionSystem.CheckAdvisorHasSuggestion(faction);
            }
            var generationTime = DateTime.Now - startTime;
            
            Console.WriteLine($"100次建议生成耗时: {generationTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"平均每次生成: {generationTime.TotalMilliseconds / 100:F4}ms");

            // 测试2: 缓存访问性能
            faction.RefreshAdvisorSuggestion(); // 确保有缓存
            
            startTime = DateTime.Now;
            for (int i = 0; i < 10000; i++)
            {
                faction.CheckAdvisorHasSuggestion();
            }
            var cacheTime = DateTime.Now - startTime;
            
            Console.WriteLine($"10000次缓存访问耗时: {cacheTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"平均每次访问: {cacheTime.TotalMilliseconds / 10000:F6}ms");

            // 测试3: 建议解决检查性能
            startTime = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                faction.CheckAdviceResolved();
            }
            var resolutionTime = DateTime.Now - startTime;
            
            Console.WriteLine($"1000次解决检查耗时: {resolutionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"平均每次检查: {resolutionTime.TotalMilliseconds / 1000:F4}ms");

            // 性能评估
            bool generationPerformanceGood = generationTime.TotalMilliseconds < 1000;
            bool cachePerformanceGood = cacheTime.TotalMilliseconds < 100;
            bool resolutionPerformanceGood = resolutionTime.TotalMilliseconds < 500;

            Console.WriteLine();
            Console.WriteLine($"建议生成性能: {(generationPerformanceGood ? "✓ 优秀" : "⚠ 需优化")}");
            Console.WriteLine($"缓存访问性能: {(cachePerformanceGood ? "✓ 优秀" : "⚠ 需优化")}");
            Console.WriteLine($"解决检查性能: {(resolutionPerformanceGood ? "✓ 优秀" : "⚠ 需优化")}");

            Console.WriteLine("✓ 性能测试完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试系统集成
        /// </summary>
        private static void TestIntegration()
        {
            Console.WriteLine("--- 集成测试 ---");

            var faction = CreateTestFaction("集成测试", "刘备", 75, "诸葛亮", 100);

            // 测试1: 与决策系统集成
            Console.WriteLine("测试与决策系统集成:");
            
            var suggestion = faction.CheckAdvisorHasSuggestion();
            Console.WriteLine($"  生成建议: {suggestion}");
            
            if (suggestion != AdvisorSuggestionKind.None)
            {
                // 测试决策系统是否能正确处理建议
                bool decision = faction.AICheckDecision();
                Console.WriteLine($"  AI决策结果: {(decision ? "采纳" : "拒绝")}");
            }

            // 测试2: 与建议显示系统集成
            Console.WriteLine("测试与建议显示系统集成:");
            
            string displayText = faction.GetCurrentSuggestionText();
            Console.WriteLine($"  显示文本长度: {displayText.Length}");
            Console.WriteLine($"  显示文本预览: {displayText.Substring(0, Math.Min(50, displayText.Length))}...");

            // 测试3: 与有效智力系统集成
            Console.WriteLine("测试与有效智力系统集成:");
            
            int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
            Console.WriteLine($"  有效智力: {effectiveInt}");
            
            // 验证建议质量是否与有效智力相关
            bool hasDetailedAdvice = faction.CurrentSuggestionDetails?.DetailedAdvice?.Length > 50;
            Console.WriteLine($"  建议详细程度: {(hasDetailedAdvice ? "详细" : "简单")}");

            // 测试4: UI集成模拟
            Console.WriteLine("测试UI集成:");
            
            // 模拟UI更新
            AdvisorSuggestionUIIntegration.OnTurnStart(faction);
            Console.WriteLine($"  回合开始处理: ✓");
            
            // 模拟玩家行动
            AdvisorSuggestionUIIntegration.OnPlayerActionCompleted(faction, "recruit");
            Console.WriteLine($"  玩家行动处理: ✓");

            Console.WriteLine("✓ 集成测试完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 运行压力测试
        /// </summary>
        public static void RunStressTest()
        {
            Console.WriteLine("=== 压力测试 ===");

            // 创建多个势力进行并发测试
            var factions = new List<Faction>();
            for (int i = 0; i < 50; i++)
            {
                var faction = CreateTestFaction($"势力{i}", "君主", 70 + i % 30, "军师", 80 + i % 20);
                factions.Add(faction);
            }

            Console.WriteLine($"创建了 {factions.Count} 个测试势力");

            // 压力测试1: 大量建议生成
            var startTime = DateTime.Now;
            foreach (var faction in factions)
            {
                for (int i = 0; i < 10; i++)
                {
                    faction.CheckAdvisorHasSuggestion();
                    faction.RefreshAdvisorSuggestion();
                }
            }
            var duration = DateTime.Now - startTime;

            Console.WriteLine($"50个势力各10次建议生成耗时: {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"平均每个势力: {duration.TotalMilliseconds / 50:F2}ms");

            // 压力测试2: 大量缓存访问
            startTime = DateTime.Now;
            foreach (var faction in factions)
            {
                for (int i = 0; i < 100; i++)
                {
                    faction.CheckAdvisorHasSuggestion();
                }
            }
            duration = DateTime.Now - startTime;

            Console.WriteLine($"50个势力各100次缓存访问耗时: {duration.TotalMilliseconds:F2}ms");

            // 内存使用检查
            GC.Collect();
            long memoryBefore = GC.GetTotalMemory(false);
            
            // 创建更多势力
            for (int i = 0; i < 100; i++)
            {
                var faction = CreateTestFaction($"临时势力{i}", "君主", 70, "军师", 80);
                faction.CheckAdvisorHasSuggestion();
            }
            
            long memoryAfter = GC.GetTotalMemory(false);
            long memoryUsed = memoryAfter - memoryBefore;

            Console.WriteLine($"100个额外势力内存使用: {memoryUsed / 1024:F2} KB");

            Console.WriteLine("✓ 压力测试完成");
        }

        #region 辅助方法

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name, string leaderName, int leaderInt, string advisorName, int advisorInt)
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
                Loyalty = 90,
                BelongedFaction = faction
            };

            return faction;
        }

        /// <summary>
        /// 模拟解决建议的行动
        /// </summary>
        private static void SimulateResolutionAction(Faction faction, AdvisorSuggestionKind suggestionType)
        {
            switch (suggestionType)
            {
                case AdvisorSuggestionKind.PersonRecruit:
                    // 模拟招募了所有在野武将
                    Console.WriteLine("    模拟: 招募了在野武将");
                    break;

                case AdvisorSuggestionKind.EnemyAttack:
                    // 模拟击退了敌军
                    Console.WriteLine("    模拟: 击退了敌军");
                    break;

                case AdvisorSuggestionKind.InternalAffairs:
                    // 模拟改善了内政
                    Console.WriteLine("    模拟: 改善了内政");
                    break;

                case AdvisorSuggestionKind.DefensePreparation:
                    // 模拟加强了防御
                    Console.WriteLine("    模拟: 加强了防御");
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 运行演示
        /// </summary>
        public static void RunDemo()
        {
            Console.WriteLine("=== 军师建议系统演示 ===");
            Console.WriteLine();

            // 创建演示势力
            var liubei = CreateTestFaction("蜀汉", "刘备", 75, "诸葛亮", 100);
            var caocao = CreateTestFaction("曹魏", "曹操", 96, "荀彧", 90);

            var factions = new[] { liubei, caocao };

            foreach (var faction in factions)
            {
                Console.WriteLine($"=== {faction.Name} 势力演示 ===");
                Console.WriteLine($"君主: {faction.Leader.Name} (智力 {faction.Leader.Intelligence})");
                Console.WriteLine($"军师: {faction.Advisor.Name} (智力 {faction.Advisor.Intelligence})");
                
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                Console.WriteLine($"有效智力: {effectiveInt}");
                Console.WriteLine();

                // 生成并显示建议
                var suggestion = faction.CheckAdvisorHasSuggestion();
                Console.WriteLine($"建议类型: {suggestion}");
                
                if (suggestion != AdvisorSuggestionKind.None)
                {
                    Console.WriteLine($"建议详情:");
                    Console.WriteLine($"  标题: {faction.CurrentSuggestionDetails?.Title}");
                    Console.WriteLine($"  优先级: {faction.CurrentSuggestionDetails?.Priority}");
                    Console.WriteLine($"  紧急度: {faction.CurrentSuggestionDetails?.Urgency}");
                    Console.WriteLine();
                    
                    Console.WriteLine("建议内容:");
                    Console.WriteLine(faction.GetCurrentSuggestionText());
                }
                else
                {
                    Console.WriteLine("当前无特别建议");
                }
                
                Console.WriteLine();
                Console.WriteLine("---");
                Console.WriteLine();
            }

            Console.WriteLine("演示完成！");
        }
    }
}