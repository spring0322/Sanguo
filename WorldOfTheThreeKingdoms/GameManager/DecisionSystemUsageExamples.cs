using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 决策系统使用示例
    /// 展示如何在实际游戏场景中使用增强决策系统
    /// </summary>
    public static class DecisionSystemUsageExamples
    {
        /// <summary>
        /// 示例1: 军事决策 - AI决定是否攻城
        /// </summary>
        public static void ExampleMilitaryDecision()
        {
            Console.WriteLine("=== 示例1: 军事决策 - 是否攻城 ===");
            Console.WriteLine();

            // 创建AI势力
            var caocao = CreateExampleFaction("魏", "曹操", 96, "荀彧", 90);
            
            // 创建决策上下文
            var context = new DecisionContext
            {
                DecisionType = DecisionType.Military,
                AdviceType = AdviceType.Military,
                AdditionalData = new Dictionary<string, object>
                {
                    ["TargetCity"] = "新野",
                    ["EnemyDefense"] = 3000,
                    ["OurForces"] = 5000,
                    ["Season"] = "春季"
                }
            };

            // 执行决策
            var result = DecisionSystemIntegration.ExecuteAIDecision(caocao, context);

            // 显示结果
            Console.WriteLine($"势力: {caocao.Name}");
            Console.WriteLine($"决策者: {result.DecisionMaker} ({result.DecisionType})");
            Console.WriteLine($"有效智力: {result.EffectiveIntelligence}");
            Console.WriteLine($"决策: {(result.Decision ? "攻城" : "暂缓攻城")}");
            Console.WriteLine($"理由: {result.Reason}");
            Console.WriteLine($"信心度: {result.Confidence}%");
            Console.WriteLine();
            Console.WriteLine("建议内容:");
            Console.WriteLine(result.DisplayText);
            Console.WriteLine();
        }

        /// <summary>
        /// 示例2: 招募决策 - AI决定是否招募武将
        /// </summary>
        public static void ExampleRecruitmentDecision()
        {
            Console.WriteLine("=== 示例2: 招募决策 - 是否招募关羽 ===");
            Console.WriteLine();

            // 创建不同类型的AI势力
            var liubei = CreateExampleFaction("蜀", "刘备", 75, "诸葛亮", 100);
            var caocao = CreateExampleFaction("魏", "曹操", 96, "程昱", 90);

            // 创建目标武将
            var guanyu = new Person
            {
                Name = "关羽",
                Intelligence = 75,
                Strength = 97,
                Command = 93,
                Politics = 62,
                Charm = 87,
                Loyalty = 95
            };

            // 测试不同势力的招募决策
            var factions = new[] { liubei, caocao };
            
            foreach (var faction in factions)
            {
                var context = new DecisionContext
                {
                    DecisionType = DecisionType.Recruitment,
                    AdviceType = AdviceType.Recruitment,
                    TargetPerson = guanyu
                };

                var result = DecisionSystemIntegration.ExecuteAIDecision(faction, context);

                Console.WriteLine($"--- {faction.Name} 势力的招募决策 ---");
                Console.WriteLine($"决策者: {result.DecisionMaker} ({result.DecisionType})");
                Console.WriteLine($"预测成功率: {result.SuccessRate}%");
                Console.WriteLine($"决策: {(result.Decision ? "尝试招募" : "放弃招募")}");
                Console.WriteLine($"理由: {result.Reason}");
                Console.WriteLine();
                Console.WriteLine("建议内容:");
                Console.WriteLine(result.DisplayText);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 示例3: 战斗决策 - AI决定使用何种战斗技能
        /// </summary>
        public static void ExampleBattleDecision()
        {
            Console.WriteLine("=== 示例3: 战斗决策 - 选择战斗技能 ===");
            Console.WriteLine();

            var zhuge = CreateExampleFaction("蜀", "刘备", 75, "诸葛亮", 100);
            
            // 创建敌军部队
            var enemyTroop = new Troop
            {
                Leader = new Person { Name = "夏侯惇", Intelligence = 70, Command = 85 },
                Quantity = 4000,
                Morale = 75
            };

            // 测试不同技能的决策
            var skills = new[]
            {
                StrategistManager.SkillType.FirePlot,
                StrategistManager.SkillType.Ambush,
                StrategistManager.SkillType.Confuse
            };

            foreach (var skill in skills)
            {
                var context = new DecisionContext
                {
                    DecisionType = DecisionType.Battle,
                    AdviceType = AdviceType.Battle,
                    TargetTroop = enemyTroop,
                    SkillType = skill
                };

                var result = DecisionSystemIntegration.ExecuteAIDecision(zhuge, context);

                Console.WriteLine($"--- {GetSkillName(skill)} 技能决策 ---");
                Console.WriteLine($"预测成功率: {result.SuccessRate}%");
                Console.WriteLine($"决策: {(result.Decision ? "使用技能" : "不使用技能")}");
                Console.WriteLine($"建议: {result.DisplayText}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 示例4: 外交决策 - AI决定是否结盟
        /// </summary>
        public static void ExampleDiplomaticDecision()
        {
            Console.WriteLine("=== 示例4: 外交决策 - 是否与东吴结盟 ===");
            Console.WriteLine();

            var liubei = CreateExampleFaction("蜀", "刘备", 75, "诸葛亮", 100);
            var sunquan = CreateExampleFaction("吴", "孙权", 80, "周瑜", 90);

            var context = new DecisionContext
            {
                DecisionType = DecisionType.Diplomatic,
                AdviceType = AdviceType.Diplomatic,
                TargetFaction = sunquan,
                DiplomaticAction = "结盟"
            };

            var result = DecisionSystemIntegration.ExecuteAIDecision(liubei, context);

            Console.WriteLine($"势力: {liubei.Name}");
            Console.WriteLine($"目标: 与 {sunquan.Name} 结盟");
            Console.WriteLine($"决策者: {result.DecisionMaker} ({result.DecisionType})");
            Console.WriteLine($"预测成功率: {result.SuccessRate}%");
            Console.WriteLine($"决策: {(result.Decision ? "同意结盟" : "拒绝结盟")}");
            Console.WriteLine($"理由: {result.Reason}");
            Console.WriteLine();
            Console.WriteLine("建议内容:");
            Console.WriteLine(result.DisplayText);
            Console.WriteLine();
        }

        /// <summary>
        /// 示例5: 批量决策处理 - 回合制游戏中的AI决策
        /// </summary>
        public static void ExampleBatchDecisions()
        {
            Console.WriteLine("=== 示例5: 批量决策处理 - AI回合 ===");
            Console.WriteLine();

            // 创建多个AI势力
            var factions = new List<Faction>
            {
                CreateExampleFaction("魏", "曹操", 96, "荀彧", 90),
                CreateExampleFaction("蜀", "刘备", 75, "诸葛亮", 100),
                CreateExampleFaction("吴", "孙权", 80, "周瑜", 90)
            };

            // 创建决策上下文
            var contexts = new List<DecisionContext>
            {
                new DecisionContext { DecisionType = DecisionType.Military, AdviceType = AdviceType.Military },
                new DecisionContext { DecisionType = DecisionType.Internal, AdviceType = AdviceType.Internal },
                new DecisionContext { DecisionType = DecisionType.Diplomatic, AdviceType = AdviceType.Diplomatic }
            };

            // 批量执行决策
            var results = DecisionSystemIntegration.ExecuteBatchDecisions(factions, contexts);

            // 显示结果
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var faction = factions[i];
                
                Console.WriteLine($"--- {faction.Name} 势力决策 ---");
                Console.WriteLine($"决策类型: {result.Context.DecisionType}");
                Console.WriteLine($"决策者: {result.DecisionMaker} ({result.DecisionType})");
                Console.WriteLine($"决策: {(result.Decision ? "采纳" : "拒绝")}");
                Console.WriteLine($"信心度: {result.Confidence}%");
                Console.WriteLine($"时间: {result.Timestamp:HH:mm:ss}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 示例6: 决策统计分析
        /// </summary>
        public static void ExampleDecisionStatistics()
        {
            Console.WriteLine("=== 示例6: 决策统计分析 ===");
            Console.WriteLine();

            var factions = new[]
            {
                CreateExampleFaction("魏", "曹操", 96, "荀彧", 90),
                CreateExampleFaction("蜀", "刘备", 75, "诸葛亮", 100),
                CreateExampleFaction("吴", "孙权", 80, "周瑜", 90),
                CreateExampleFaction("袁", "袁绍", 70, "田丰", 85)
            };

            foreach (var faction in factions)
            {
                // 模拟一些决策历史
                for (int i = 0; i < 5; i++)
                {
                    var context = new DecisionContext
                    {
                        DecisionType = (DecisionType)(i % 3),
                        AdviceType = AdviceType.General
                    };
                    DecisionSystemIntegration.ExecuteAIDecision(faction, context);
                }

                // 获取统计信息
                var stats = DecisionSystemIntegration.GetDecisionStatistics(faction);

                Console.WriteLine($"--- {stats.FactionName} 势力统计 ---");
                Console.WriteLine($"有效智力: {stats.EffectiveIntelligence}");
                Console.WriteLine($"决策模式: {stats.DecisionMode}");
                Console.WriteLine($"近期决策: {stats.RecentDecisions} 次");
                Console.WriteLine();
                Console.WriteLine("明主效应分析:");
                Console.WriteLine(stats.WiseRulerAnalysis);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 示例7: 明主识破不良建议的实际场景
        /// </summary>
        public static void ExampleWiseRulerDetection()
        {
            Console.WriteLine("=== 示例7: 明主识破不良建议 ===");
            Console.WriteLine();

            // 创建曹操 + 杨修的组合（杨修忠诚度低）
            var caocao = CreateExampleFaction("魏", "曹操", 96, "杨修", 85);
            caocao.Advisor.Loyalty = 55; // 设置低忠诚度

            Console.WriteLine("场景: 杨修向曹操建议泄露军事机密");
            Console.WriteLine($"曹操智力: {caocao.Leader.Intelligence}");
            Console.WriteLine($"杨修智力: {caocao.Advisor.Intelligence}");
            Console.WriteLine($"杨修忠诚: {caocao.Advisor.Loyalty}");
            Console.WriteLine($"理论识破率: {caocao.Leader.Intelligence}%");
            Console.WriteLine();

            // 进行多次测试
            int detectionCount = 0;
            int totalTests = 20;

            Console.WriteLine("测试结果:");
            for (int i = 1; i <= totalTests; i++)
            {
                var context = new DecisionContext
                {
                    DecisionType = DecisionType.Military,
                    AdviceType = AdviceType.Military
                };

                var result = DecisionSystemIntegration.ExecuteAIDecision(caocao, context);
                
                Console.WriteLine($"第{i,2}次: {(result.Decision ? "采纳建议" : "识破并拒绝")} - {result.Reason}");
                
                if (!result.Decision)
                    detectionCount++;
            }

            Console.WriteLine();
            Console.WriteLine($"识破统计: {detectionCount}/{totalTests} ({detectionCount * 100.0 / totalTests:F1}%)");
            
            if (detectionCount > totalTests * 0.8) // 期望80%以上的识破率
                Console.WriteLine("✓ 明主效应正常工作 - 曹操成功识破了杨修的大部分不良建议");
            else
                Console.WriteLine("? 识破率偏低，可能需要调整参数");
        }

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("=== 决策系统完整使用示例 ===");
            Console.WriteLine();

            ExampleMilitaryDecision();
            ExampleRecruitmentDecision();
            ExampleBattleDecision();
            ExampleDiplomaticDecision();
            ExampleBatchDecisions();
            ExampleDecisionStatistics();
            ExampleWiseRulerDetection();

            Console.WriteLine("=== 所有示例执行完毕 ===");
        }

        /// <summary>
        /// 创建示例势力
        /// </summary>
        private static Faction CreateExampleFaction(string name, string leaderName, int leaderInt, string advisorName, int advisorInt)
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
                Loyalty = 90,
                BelongedFaction = faction
            };

            return faction;
        }

        /// <summary>
        /// 获取技能名称
        /// </summary>
        private static string GetSkillName(StrategistManager.SkillType skillType)
        {
            switch (skillType)
            {
                case StrategistManager.SkillType.FirePlot: return "火计";
                case StrategistManager.SkillType.WaterPlot: return "水计";
                case StrategistManager.SkillType.Ambush: return "伏兵";
                case StrategistManager.SkillType.Provoke: return "挑衅";
                case StrategistManager.SkillType.Confuse: return "混乱";
                case StrategistManager.SkillType.Retreat: return "撤退";
                case StrategistManager.SkillType.Rally: return "鼓舞";
                default: return "未知技能";
            }
        }
    }
}