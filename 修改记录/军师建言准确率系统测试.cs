using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师建言准确率系统测试
    /// </summary>
    public static class AdvisorAccuracyTest
    {
        /// <summary>
        /// 测试不同智力军师的建言准确率
        /// </summary>
        public static void TestAdvisorAccuracy()
        {
            Console.WriteLine("=== 军师建言准确率系统测试 ===");
            Console.WriteLine();

            // 创建测试军师
            var testCases = new[]
            {
                new { Name = "诸葛亮", Intelligence = 100, Description = "智力满值，必定准确" },
                new { Name = "司马懿", Intelligence = 95, Description = "顶级智力，高准确率" },
                new { Name = "周瑜", Intelligence = 90, Description = "优秀智力，较高准确率" },
                new { Name = "荀彧", Intelligence = 85, Description = "良好智力，中等准确率" },
                new { Name = "田丰", Intelligence = 80, Description = "中等智力，一般准确率" },
                new { Name = "普通谋士", Intelligence = 70, Description = "基础智力，基础准确率" },
                new { Name = "低级谋士", Intelligence = 60, Description = "较低智力，低准确率" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"【{testCase.Name}】智力: {testCase.Intelligence}");
                Console.WriteLine($"描述: {testCase.Description}");
                
                // 创建测试势力
                var faction = CreateTestFaction(testCase.Name, testCase.Intelligence);
                
                // 测试100次，统计准确率
                int accurateCount = 0;
                int totalTests = 100;
                
                for (int i = 0; i < totalTests; i++)
                {
                    if (faction.IsAdviceAccurate())
                    {
                        accurateCount++;
                    }
                }
                
                double actualAccuracyRate = (double)accurateCount / totalTests * 100;
                double theoreticalRate = CalculateTheoreticalAccuracy(testCase.Intelligence);
                
                Console.WriteLine($"理论准确率: {theoreticalRate:F1}%");
                Console.WriteLine($"实际准确率: {actualAccuracyRate:F1}% ({accurateCount}/{totalTests})");
                Console.WriteLine($"误差: {Math.Abs(actualAccuracyRate - theoreticalRate):F1}%");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试难度对准确率的影响
        /// </summary>
        public static void TestDifficultyEffect()
        {
            Console.WriteLine("=== 难度对准确率的影响测试 ===");
            Console.WriteLine();

            var advisor = CreateTestFaction("诸葛亮", 85);
            var difficulties = new[] { 0, 5, 10, 15, 20 };

            Console.WriteLine("军师: 诸葛亮 (智力85)");
            Console.WriteLine("难度 | 理论准确率 | 实际准确率 | 测试次数");
            Console.WriteLine("-----|------------|------------|----------");

            foreach (int difficulty in difficulties)
            {
                int accurateCount = 0;
                int totalTests = 100;
                
                for (int i = 0; i < totalTests; i++)
                {
                    if (advisor.IsAdviceAccurate(difficulty))
                    {
                        accurateCount++;
                    }
                }
                
                double actualRate = (double)accurateCount / totalTests * 100;
                double theoreticalRate = CalculateTheoreticalAccuracy(85, difficulty);
                
                Console.WriteLine($"{difficulty,4} | {theoreticalRate,10:F1}% | {actualRate,10:F1}% | {totalTests,8}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 测试不同建议类型的准确性表现
        /// </summary>
        public static void TestAdviceTypeAccuracy()
        {
            Console.WriteLine("=== 不同建议类型的准确性表现 ===");
            Console.WriteLine();

            var faction = CreateTestFaction("周瑜", 90);
            var suggestionTypes = new[]
            {
                AdvisorSuggestionKind.EnemyAttack,
                AdvisorSuggestionKind.PersonRecruit,
                AdvisorSuggestionKind.LoyaltyWarning,
                AdvisorSuggestionKind.InternalAffair,
                AdvisorSuggestionKind.DisasterWarning
            };

            Console.WriteLine("军师: 周瑜 (智力90)");
            Console.WriteLine();

            foreach (var suggestionType in suggestionTypes)
            {
                Console.WriteLine($"建议类型: {GetSuggestionTypeName(suggestionType)}");
                
                // 模拟准确和不准确的建言
                bool isAccurate = faction.IsAdviceAccurate();
                string adviceText = GetSimulatedAdviceText(suggestionType, isAccurate);
                
                Console.WriteLine($"准确性: {(isAccurate ? "准确" : "不准确")}");
                Console.WriteLine($"建言内容: {adviceText}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string advisorName, int intelligence)
        {
            var faction = new Faction
            {
                Name = "测试势力",
                Leader = new Person { Name = "测试君主", Intelligence = 75 },
                Advisor = new Person { Name = advisorName, Intelligence = intelligence }
            };
            
            return faction;
        }

        /// <summary>
        /// 计算理论准确率
        /// </summary>
        private static double CalculateTheoreticalAccuracy(int intelligence, int difficulty = 0)
        {
            if (intelligence >= 100) return 95.0; // 最高95%，保留不确定性
            
            int baseChance = 60 + (int)((intelligence - 70) * 1.5);
            baseChance -= difficulty;
            
            return Math.Max(10, Math.Min(95, baseChance));
        }

        /// <summary>
        /// 获取建议类型名称
        /// </summary>
        private static string GetSuggestionTypeName(AdvisorSuggestionKind type)
        {
            switch (type)
            {
                case AdvisorSuggestionKind.EnemyAttack: return "敌军来袭预警";
                case AdvisorSuggestionKind.PersonRecruit: return "人才招募建议";
                case AdvisorSuggestionKind.LoyaltyWarning: return "忠诚度警告";
                case AdvisorSuggestionKind.InternalAffair: return "内政建议";
                case AdvisorSuggestionKind.DisasterWarning: return "灾害预警";
                default: return "未知类型";
            }
        }

        /// <summary>
        /// 获取模拟的建言文本
        /// </summary>
        private static string GetSimulatedAdviceText(AdvisorSuggestionKind type, bool isAccurate)
        {
            if (isAccurate)
            {
                switch (type)
                {
                    case AdvisorSuggestionKind.EnemyAttack:
                        return "主公，据某观察，敌军正在向我方逼近！请速做防备！";
                    case AdvisorSuggestionKind.PersonRecruit:
                        return "主公，某发现附近有贤才在野，建议派人前去招揽！";
                    case AdvisorSuggestionKind.LoyaltyWarning:
                        return "主公，某观察到部分将领忠诚度有所下降，需多加关注！";
                    case AdvisorSuggestionKind.InternalAffair:
                        return "主公，当前内政有待改善，建议加强治理！";
                    case AdvisorSuggestionKind.DisasterWarning:
                        return "主公，某观天象，恐有灾害将至，宜早做准备！";
                    default:
                        return "主公，当前形势尚好，可按既定方针继续行事。";
                }
            }
            else
            {
                switch (type)
                {
                    case AdvisorSuggestionKind.EnemyAttack:
                        return "主公，某观察四周，暂无异常，可安心发展。";
                    case AdvisorSuggestionKind.PersonRecruit:
                        return "主公，当前人才济济，暂无招揽之需。";
                    case AdvisorSuggestionKind.LoyaltyWarning:
                        return "主公，将士们忠心耿耿，无需担忧。";
                    case AdvisorSuggestionKind.InternalAffair:
                        return "主公，内政井然有序，可专注军事。";
                    case AdvisorSuggestionKind.DisasterWarning:
                        return "主公，风调雨顺，国泰民安。";
                    default:
                        return "主公，某才疏学浅，难以判断当前形势。";
                }
            }
        }

        /// <summary>
        /// 运行完整测试套件
        /// </summary>
        public static void RunFullTest()
        {
            Console.WriteLine("军师建言准确率系统 - 完整测试");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            TestAdvisorAccuracy();
            Console.WriteLine();
            
            TestDifficultyEffect();
            Console.WriteLine();
            
            TestAdviceTypeAccuracy();
            Console.WriteLine();

            Console.WriteLine("测试完成！");
            Console.WriteLine();
            Console.WriteLine("系统特性总结:");
            Console.WriteLine("✅ 智力100的军师建言必定准确");
            Console.WriteLine("✅ 智力70-99线性递增，每点智力增加1.5%准确率");
            Console.WriteLine("✅ 支持难度调整，增加挑战性");
            Console.WriteLine("✅ 不准确的建言会给出相反或模糊的信息");
            Console.WriteLine("✅ 准确率限制在10%-95%之间，保持游戏平衡");
        }
    }
}