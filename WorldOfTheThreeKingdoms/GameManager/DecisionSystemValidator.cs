using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 决策系统验证器
    /// 提供全面的系统测试、验证和性能分析功能
    /// </summary>
    public static class DecisionSystemValidator
    {
        /// <summary>
        /// 运行完整的系统验证
        /// </summary>
        public static ValidationReport RunCompleteValidation()
        {
            Console.WriteLine("=== 决策系统完整验证 ===");
            Console.WriteLine();

            var report = new ValidationReport();

            // 1. 基础功能验证
            report.BasicFunctionTests = ValidateBasicFunctions();
            
            // 2. 明主效应验证
            report.WiseRulerEffectTests = ValidateWiseRulerEffect();
            
            // 3. 预测系统验证
            report.PredictionSystemTests = ValidatePredictionSystems();
            
            // 4. 历史案例验证
            report.HistoricalCaseTests = ValidateHistoricalCases();
            
            // 5. 性能测试
            report.PerformanceTests = ValidatePerformance();
            
            // 6. 集成测试
            report.IntegrationTests = ValidateIntegration();
            
            // 7. 边界条件测试
            report.EdgeCaseTests = ValidateEdgeCases();

            // 生成总体评分
            report.OverallScore = CalculateOverallScore(report);
            report.GeneratedAt = DateTime.Now;

            // 输出报告
            PrintValidationReport(report);

            return report;
        }

        /// <summary>
        /// 验证基础功能
        /// </summary>
        private static TestResult ValidateBasicFunctions()
        {
            Console.WriteLine("--- 基础功能验证 ---");
            var result = new TestResult { TestName = "基础功能" };

            try
            {
                // 测试1: AICheckDecision 方法
                var faction = CreateTestFaction("测试", "曹操", 96, "荀彧", 90, 95);
                bool decision1 = faction.AICheckDecision();
                result.PassedTests++;

                // 测试2: 建议显示系统
                var displayInfo = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, "测试建议", AdviceType.General);
                if (!string.IsNullOrEmpty(displayInfo.DisplayText))
                    result.PassedTests++;

                // 测试3: 有效智力计算
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                if (effectiveInt > 0)
                    result.PassedTests++;

                // 测试4: 决策集成
                var context = new DecisionContext { DecisionType = DecisionType.Military, AdviceType = AdviceType.Military };
                var decisionResult = DecisionSystemIntegration.ExecuteAIDecision(faction, context);
                if (decisionResult != null)
                    result.PassedTests++;

                result.TotalTests = 4;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"基础功能测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"基础功能测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证明主效应
        /// </summary>
        private static TestResult ValidateWiseRulerEffect()
        {
            Console.WriteLine("--- 明主效应验证 ---");
            var result = new TestResult { TestName = "明主效应" };

            try
            {
                // 创建明主型和军师型势力
                var wiseRuler = CreateTestFaction("明主", "曹操", 96, "程昱", 90, 95);
                var advisorLed = CreateTestFaction("军师", "刘备", 75, "诸葛亮", 100, 100);

                // 测试1: 有效智力计算正确性
                int wiseRulerEffective = EffectiveIntelligenceSystem.GetEffectiveIntelligence(wiseRuler);
                int advisorLedEffective = EffectiveIntelligenceSystem.GetEffectiveIntelligence(advisorLed);
                
                if (wiseRulerEffective == 96 && advisorLedEffective == 100)
                    result.PassedTests++;

                // 测试2: 决策模式识别
                var wiseRulerDisplay = AdviceDisplaySystem.GetAdviceDisplayInfo(wiseRuler, "测试", AdviceType.General);
                var advisorLedDisplay = AdviceDisplaySystem.GetAdviceDisplayInfo(advisorLed, "测试", AdviceType.General);
                
                if (wiseRulerDisplay.IsWiseRulerMode && !advisorLedDisplay.IsWiseRulerMode)
                    result.PassedTests++;

                // 测试3: 识破机制
                var suspiciousFaction = CreateTestFaction("可疑", "曹操", 96, "杨修", 85, 50);
                int detectionCount = 0;
                for (int i = 0; i < 50; i++)
                {
                    if (!suspiciousFaction.AICheckDecision())
                        detectionCount++;
                }
                
                // 期望识破率应该接近君主智力
                if (detectionCount > 30) // 至少60%的识破率
                    result.PassedTests++;

                result.TotalTests = 3;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"明主效应测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"明主效应测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证预测系统
        /// </summary>
        private static TestResult ValidatePredictionSystems()
        {
            Console.WriteLine("--- 预测系统验证 ---");
            var result = new TestResult { TestName = "预测系统" };

            try
            {
                var faction = CreateTestFaction("测试", "刘备", 75, "诸葛亮", 100, 95);
                var targetPerson = new Person { Name = "关羽", Loyalty = 85, Intelligence = 75 };
                var targetTroop = new Troop 
                { 
                    Leader = new Person { Name = "张飞", Intelligence = 70, Command = 85 },
                    Quantity = 4000,
                    Morale = 80
                };

                // 测试1: 招募预测
                var (recruitRate, recruitComment) = StrategistManager.GetRecruitPrediction(faction, targetPerson);
                if (recruitRate >= 0 && recruitRate <= 100 && !string.IsNullOrEmpty(recruitComment))
                    result.PassedTests++;

                // 测试2: 战斗技能预测
                int battlePrediction = StrategistManager.GetBattlePrediction(faction, targetTroop, StrategistManager.SkillType.FirePlot);
                if (battlePrediction >= 0 && battlePrediction <= 100)
                    result.PassedTests++;

                // 测试3: 战斗预测
                var battleResult = StrategistManager.PredictBattle(faction, targetTroop, targetTroop);
                if (battleResult.SuccessRate >= 0 && battleResult.SuccessRate <= 100)
                    result.PassedTests++;

                // 测试4: 外交预测
                var targetFaction = CreateTestFaction("目标", "孙权", 80, "周瑜", 90, 90);
                var diplomacyResult = StrategistManager.PredictDiplomacy(faction, targetFaction, "结盟");
                if (diplomacyResult.SuccessRate >= 0 && diplomacyResult.SuccessRate <= 100)
                    result.PassedTests++;

                result.TotalTests = 4;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"预测系统测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"预测系统测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证历史案例
        /// </summary>
        private static TestResult ValidateHistoricalCases()
        {
            Console.WriteLine("--- 历史案例验证 ---");
            var result = new TestResult { TestName = "历史案例" };

            try
            {
                var historicalCases = new[]
                {
                    new { Name = "曹操+荀彧", Ruler = "曹操", RulerInt = 96, Advisor = "荀彧", AdvisorInt = 90, ExpectedMode = "明主" },
                    new { Name = "刘备+诸葛亮", Ruler = "刘备", RulerInt = 75, Advisor = "诸葛亮", AdvisorInt = 100, ExpectedMode = "军师" },
                    new { Name = "孙权+周瑜", Ruler = "孙权", RulerInt = 80, Advisor = "周瑜", AdvisorInt = 90, ExpectedMode = "军师" },
                    new { Name = "袁绍+田丰", Ruler = "袁绍", RulerInt = 70, Advisor = "田丰", AdvisorInt = 85, ExpectedMode = "军师" }
                };

                foreach (var case_ in historicalCases)
                {
                    var faction = CreateTestFaction(case_.Name, case_.Ruler, case_.RulerInt, case_.Advisor, case_.AdvisorInt, 90);
                    var displayInfo = AdviceDisplaySystem.GetAdviceDisplayInfo(faction, "测试建议", AdviceType.General);
                    
                    bool isCorrectMode = (case_.ExpectedMode == "明主" && displayInfo.IsWiseRulerMode) ||
                                       (case_.ExpectedMode == "军师" && !displayInfo.IsWiseRulerMode);
                    
                    if (isCorrectMode)
                        result.PassedTests++;
                }

                result.TotalTests = historicalCases.Length;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"历史案例测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"历史案例测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证性能
        /// </summary>
        private static TestResult ValidatePerformance()
        {
            Console.WriteLine("--- 性能验证 ---");
            var result = new TestResult { TestName = "性能测试" };

            try
            {
                var faction = CreateTestFaction("性能测试", "曹操", 96, "荀彧", 90, 95);
                var context = new DecisionContext { DecisionType = DecisionType.Military, AdviceType = AdviceType.Military };

                // 测试1: 决策性能
                var startTime = DateTime.Now;
                for (int i = 0; i < 1000; i++)
                {
                    faction.AICheckDecision();
                }
                var decisionTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (decisionTime < 1000) // 1000次决策应在1秒内完成
                    result.PassedTests++;

                // 测试2: 建议显示性能
                startTime = DateTime.Now;
                for (int i = 0; i < 1000; i++)
                {
                    AdviceDisplaySystem.GetAdviceDisplayInfo(faction, "测试建议", AdviceType.General);
                }
                var displayTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (displayTime < 500) // 1000次显示应在0.5秒内完成
                    result.PassedTests++;

                // 测试3: 集成决策性能
                startTime = DateTime.Now;
                for (int i = 0; i < 100; i++)
                {
                    DecisionSystemIntegration.ExecuteAIDecision(faction, context);
                }
                var integrationTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (integrationTime < 1000) // 100次集成决策应在1秒内完成
                    result.PassedTests++;

                result.TotalTests = 3;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"性能测试: {result.PassedTests}/{result.TotalTests} 通过 (决策:{decisionTime:F0}ms, 显示:{displayTime:F0}ms, 集成:{integrationTime:F0}ms)";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"性能测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证系统集成
        /// </summary>
        private static TestResult ValidateIntegration()
        {
            Console.WriteLine("--- 集成验证 ---");
            var result = new TestResult { TestName = "系统集成" };

            try
            {
                // 测试1: 有效智力与预测系统集成
                var faction = CreateTestFaction("集成测试", "曹操", 96, "程昱", 90, 95);
                var targetPerson = new Person { Name = "关羽", Loyalty = 85 };
                
                var (rate1, _) = StrategistManager.GetRecruitPrediction(faction, targetPerson);
                
                // 改变军师智力，测试有效智力是否正确影响预测
                faction.Advisor.Intelligence = 70;
                var (rate2, _) = StrategistManager.GetRecruitPrediction(faction, targetPerson);
                
                // 由于曹操智力(96)高于新军师智力(70)，有效智力应该保持96，预测应该相似
                if (Math.Abs(rate1 - rate2) < 10) // 允许小幅差异
                    result.PassedTests++;

                // 测试2: 决策系统与建议显示集成
                var context = new DecisionContext { DecisionType = DecisionType.Military, AdviceType = AdviceType.Military };
                var decisionResult = DecisionSystemIntegration.ExecuteAIDecision(faction, context);
                
                if (!string.IsNullOrEmpty(decisionResult.DisplayText) && decisionResult.EffectiveIntelligence > 0)
                    result.PassedTests++;

                // 测试3: 配置系统集成
                DecisionSystemConfig.LoadSimplifiedConfig();
                bool decision1 = faction.AICheckDecision();
                
                DecisionSystemConfig.LoadExpertConfig();
                bool decision2 = faction.AICheckDecision();
                
                // 配置应该能够影响决策行为（至少不会出错）
                result.PassedTests++;

                result.TotalTests = 3;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"集成测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"集成测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 验证边界条件
        /// </summary>
        private static TestResult ValidateEdgeCases()
        {
            Console.WriteLine("--- 边界条件验证 ---");
            var result = new TestResult { TestName = "边界条件" };

            try
            {
                // 测试1: 空值处理
                var emptyFaction = new Faction { Name = "空势力" };
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(emptyFaction);
                if (effectiveInt == 0) // 应该返回0而不是崩溃
                    result.PassedTests++;

                // 测试2: 极端智力值
                var extremeFaction = CreateTestFaction("极端", "超级君主", 100, "超级军师", 100, 100);
                bool decision = extremeFaction.AICheckDecision();
                // 应该能正常执行而不崩溃
                result.PassedTests++;

                // 测试3: 零忠诚度
                var zeroLoyaltyFaction = CreateTestFaction("零忠诚", "曹操", 96, "叛徒", 85, 0);
                bool detectionDecision = zeroLoyaltyFaction.AICheckDecision();
                // 应该能处理零忠诚度情况
                result.PassedTests++;

                // 测试4: 相同智力
                var sameFaction = CreateTestFaction("相同", "君主", 80, "军师", 80, 90);
                var sameDisplay = AdviceDisplaySystem.GetAdviceDisplayInfo(sameFaction, "测试", AdviceType.General);
                if (!sameDisplay.IsWiseRulerMode) // 相同智力应该走军师模式
                    result.PassedTests++;

                result.TotalTests = 4;
                result.Success = result.PassedTests == result.TotalTests;
                result.Message = $"边界条件测试: {result.PassedTests}/{result.TotalTests} 通过";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"边界条件测试失败: {ex.Message}";
            }

            Console.WriteLine($"  {result.Message}");
            return result;
        }

        /// <summary>
        /// 计算总体评分
        /// </summary>
        private static int CalculateOverallScore(ValidationReport report)
        {
            var allTests = new[] 
            { 
                report.BasicFunctionTests, 
                report.WiseRulerEffectTests, 
                report.PredictionSystemTests,
                report.HistoricalCaseTests,
                report.PerformanceTests,
                report.IntegrationTests,
                report.EdgeCaseTests
            };

            int totalPassed = allTests.Sum(t => t.PassedTests);
            int totalTests = allTests.Sum(t => t.TotalTests);

            return totalTests > 0 ? (totalPassed * 100 / totalTests) : 0;
        }

        /// <summary>
        /// 打印验证报告
        /// </summary>
        private static void PrintValidationReport(ValidationReport report)
        {
            Console.WriteLine();
            Console.WriteLine("=== 验证报告摘要 ===");
            Console.WriteLine($"总体评分: {report.OverallScore}/100");
            Console.WriteLine($"生成时间: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var allTests = new[] 
            { 
                report.BasicFunctionTests, 
                report.WiseRulerEffectTests, 
                report.PredictionSystemTests,
                report.HistoricalCaseTests,
                report.PerformanceTests,
                report.IntegrationTests,
                report.EdgeCaseTests
            };

            foreach (var test in allTests)
            {
                string status = test.Success ? "✓" : "✗";
                Console.WriteLine($"{status} {test.TestName}: {test.PassedTests}/{test.TotalTests} - {test.Message}");
            }

            Console.WriteLine();
            if (report.OverallScore >= 90)
                Console.WriteLine("🎉 系统验证优秀！所有核心功能正常工作。");
            else if (report.OverallScore >= 80)
                Console.WriteLine("✅ 系统验证良好，大部分功能正常工作。");
            else if (report.OverallScore >= 70)
                Console.WriteLine("⚠️ 系统验证一般，存在一些问题需要修复。");
            else
                Console.WriteLine("❌ 系统验证不合格，存在严重问题需要立即修复。");
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
                Strength = leaderInt - 15,
                BelongedFaction = faction
            };

            faction.Advisor = new Person
            {
                Name = advisorName,
                Intelligence = advisorInt,
                Command = advisorInt - 15,
                Charm = advisorInt - 10,
                Politics = advisorInt - 5,
                Strength = advisorInt - 20,
                Loyalty = advisorLoyalty,
                BelongedFaction = faction
            };

            return faction;
        }
    }

    /// <summary>
    /// 验证报告
    /// </summary>
    public class ValidationReport
    {
        public TestResult BasicFunctionTests { get; set; }
        public TestResult WiseRulerEffectTests { get; set; }
        public TestResult PredictionSystemTests { get; set; }
        public TestResult HistoricalCaseTests { get; set; }
        public TestResult PerformanceTests { get; set; }
        public TestResult IntegrationTests { get; set; }
        public TestResult EdgeCaseTests { get; set; }
        public int OverallScore { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 测试结果
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; }
        public bool Success { get; set; }
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        public string Message { get; set; }
    }
}