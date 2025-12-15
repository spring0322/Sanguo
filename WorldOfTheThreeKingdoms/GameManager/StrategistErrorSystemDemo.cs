using System;
using GameObjects;
using GameManager;
using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// 军师误差系统演示
    /// </summary>
    public static class StrategistErrorSystemDemo
    {
        /// <summary>
        /// 演示军师预测误差系统
        /// </summary>
        public static void DemonstrateErrorSystem()
        {
            Console.WriteLine("=== 军师预测误差系统演示 ===");
            Console.WriteLine();

            // 模拟不同智力的军师
            var scenarios = new[]
            {
                new { Name = "诸葛亮", Intelligence = 100, Description = "智力100，预测极准确" },
                new { Name = "庞统", Intelligence = 95, Description = "智力95，预测很准确" },
                new { Name = "荀彧", Intelligence = 85, Description = "智力85，预测较准确" },
                new { Name = "田丰", Intelligence = 75, Description = "智力75，预测一般" },
                new { Name = "郭图", Intelligence = 60, Description = "智力60，预测误差较大" },
                new { Name = "普通谋士", Intelligence = 45, Description = "智力45，预测误差很大" }
            };

            Console.WriteLine("假设真实招募成功率为 60%，不同军师的预测结果：");
            Console.WriteLine();

            foreach (var scenario in scenarios)
            {
                Console.WriteLine($"=== {scenario.Name} ({scenario.Description}) ===");
                
                // 计算误差范围
                int errorMargin = (100 - scenario.Intelligence) / 2;
                Console.WriteLine($"理论误差范围: ±{errorMargin}%");
                
                // 模拟多次预测
                Console.WriteLine("10次预测结果:");
                for (int i = 1; i <= 10; i++)
                {
                    int realRate = 60; // 假设真实成功率
                    Random random = new Random(i * 1000 + scenario.Intelligence); // 固定种子便于演示
                    int randomError = random.Next(-errorMargin, errorMargin + 1);
                    int predictedRate = Math.Max(0, Math.Min(100, realRate + randomError));
                    
                    string accuracy = Math.Abs(predictedRate - realRate) <= 5 ? "✅准确" : 
                                     Math.Abs(predictedRate - realRate) <= 15 ? "⚠️偏差" : "❌误差大";
                    
                    Console.WriteLine($"  第{i}次: {predictedRate}% (误差{randomError:+#;-#;0}%) {accuracy}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 演示"头铁"机制的游戏体验
        /// </summary>
        public static void DemonstrateHeadstrongMechanism()
        {
            Console.WriteLine("=== \"头铁\"机制演示 ===");
            Console.WriteLine();
            
            var testCases = new[]
            {
                new { PredictedRate = 85, RealRate = 90, Scenario = "军师略微低估" },
                new { PredictedRate = 75, RealRate = 60, Scenario = "军师高估了" },
                new { PredictedRate = 25, RealRate = 45, Scenario = "军师过于悲观" },
                new { PredictedRate = 15, RealRate = 10, Scenario = "军师预测准确，但玩家头铁" },
                new { PredictedRate = 5, RealRate = 80, Scenario = "军师大错特错！" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"场景: {testCase.Scenario}");
                Console.WriteLine($"军师预测: {testCase.PredictedRate}%");
                Console.WriteLine($"真实成功率: {testCase.RealRate}%");
                
                // UI反馈
                string buttonText = GetButtonText(testCase.PredictedRate);
                Color buttonColor = StrategistManager.GetSuccessRateColor(testCase.PredictedRate);
                string expression = GetExpressionDescription(testCase.PredictedRate);
                
                Console.WriteLine($"按钮显示: [{buttonColor}] {buttonText}");
                Console.WriteLine($"军师表情: {expression}");
                
                // 玩家选择的心理分析
                if (testCase.PredictedRate >= 70)
                {
                    Console.WriteLine("玩家心理: 军师都说成功率高，当然要试试！");
                }
                else if (testCase.PredictedRate >= 40)
                {
                    Console.WriteLine("玩家心理: 成功率一般，但值得一试。");
                }
                else if (testCase.PredictedRate >= 20)
                {
                    Console.WriteLine("玩家心理: 成功率低，但万一成功了呢？");
                }
                else
                {
                    Console.WriteLine("玩家心理: 军师说不行，但我就是要试试！（头铁）");
                }
                
                // 结果分析
                if (testCase.RealRate > testCase.PredictedRate + 20)
                {
                    Console.WriteLine("结果: 🎉 意外之喜！军师低估了，玩家头铁成功！");
                }
                else if (testCase.RealRate < testCase.PredictedRate - 20)
                {
                    Console.WriteLine("结果: 😅 军师高估了，但至少给了玩家信心。");
                }
                else
                {
                    Console.WriteLine("结果: 😐 军师预测基本准确。");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 演示不同军师性格的评语风格
        /// </summary>
        public static void DemonstratePersonalityComments()
        {
            Console.WriteLine("=== 军师性格评语演示 ===");
            Console.WriteLine();
            
            var personalities = new[]
            {
                new { ID = 0, Name = "仁德型", Example = "诸葛亮" },
                new { ID = 1, Name = "霸道型", Example = "贾诩" },
                new { ID = 2, Name = "冷静型", Example = "荀彧" },
                new { ID = 3, Name = "莽撞型", Example = "许攸" },
                new { ID = 4, Name = "狡诈型", Example = "郭嘉" }
            };

            int[] successRates = { 90, 70, 50, 30, 10 };
            
            foreach (var personality in personalities)
            {
                Console.WriteLine($"=== {personality.Name}军师 (如{personality.Example}) ===");
                
                foreach (int rate in successRates)
                {
                    string baseComment = GetBaseComment(rate);
                    string personalizedComment = AddPersonalityStyle(baseComment, personality.ID, rate);
                    
                    Console.WriteLine($"成功率{rate}%: {personalizedComment}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 根据预测成功率获取按钮文字
        /// </summary>
        private static string GetButtonText(int predictedRate)
        {
            if (predictedRate >= 70) return "执行招募";
            else if (predictedRate >= 40) return "尝试招募";
            else if (predictedRate >= 20) return "冒险招募";
            else return "强行招募";
        }

        /// <summary>
        /// 获取表情描述
        /// </summary>
        private static string GetExpressionDescription(int predictedRate)
        {
            if (predictedRate >= 80) return "😊 自信微笑";
            else if (predictedRate >= 60) return "😐 表情中性";
            else if (predictedRate >= 40) return "😟 略显担忧";
            else if (predictedRate >= 20) return "😰 紧张流汗";
            else return "😵 摇头叹气";
        }

        /// <summary>
        /// 获取基础评语
        /// </summary>
        private static string GetBaseComment(int rate)
        {
            if (rate >= 90) return "此事必成";
            else if (rate >= 70) return "成功在望";
            else if (rate >= 50) return "或有一线生机";
            else if (rate >= 30) return "此事颇为困难";
            else return "成功希望渺茫";
        }

        /// <summary>
        /// 添加性格风格
        /// </summary>
        private static string AddPersonalityStyle(string baseComment, int personalityId, int rate)
        {
            switch (personalityId)
            {
                case 0: // 仁德型
                    if (rate >= 70) return baseComment + "，以德服人，此乃上策。";
                    else return baseComment + "，强求不得，当以诚待之。";
                    
                case 1: // 霸道型
                    if (rate >= 70) return baseComment + "，展现实力，令其折服！";
                    else return baseComment + "，既然软的不行，或可考虑其他手段。";
                    
                case 2: // 冷静型
                    return baseComment + $"，据臣分析，成功概率约为 {rate}%。";
                    
                case 3: // 莽撞型
                    if (rate >= 50) return baseComment + "，管他三七二十一，试试再说！";
                    else return baseComment + "，这...臣也没什么好办法。";
                    
                case 4: // 狡诈型
                    if (rate >= 70) return baseComment + "，嘿嘿，此人必入我彀中。";
                    else return baseComment + "，此人不好对付，需另想妙计。";
                    
                default:
                    return baseComment + "。";
            }
        }

        /// <summary>
        /// 运行完整演示
        /// </summary>
        public static void RunFullDemo()
        {
            Console.WriteLine("🎮 军师预测误差系统完整演示");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            DemonstrateErrorSystem();
            Console.WriteLine();
            
            DemonstrateHeadstrongMechanism();
            Console.WriteLine();
            
            DemonstratePersonalityComments();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("演示结束！这个系统让军师预测更真实，");
            Console.WriteLine("同时给玩家\"头铁\"尝试的机会，增加了游戏的趣味性！");
        }
    }
}