using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 有效智力系统演示
    /// 展示明主效应如何影响各种预测系统
    /// </summary>
    public static class EffectiveIntelligenceDemo
    {
        /// <summary>
        /// 运行完整演示
        /// </summary>
        public static void RunDemo()
        {
            Console.WriteLine("=== 有效智力系统 - 明主效应演示 ===");
            Console.WriteLine();
            Console.WriteLine("本系统实现了《三国志11》中的'明主效应'概念：");
            Console.WriteLine("- 高智力君主可以识别并纠正军师的错误判断");
            Console.WriteLine("- 低智力君主会被优秀军师大幅提升");
            Console.WriteLine("- 有效智力 = Max(君主智力, 军师智力)");
            Console.WriteLine();

            // 演示1: 明主效应基本概念
            DemonstrateBasicConcept();

            // 演示2: 预测系统集成
            DemonstratePredictionIntegration();

            // 演示3: 历史案例对比
            DemonstrateHistoricalCases();
        }

        /// <summary>
        /// 演示基本概念
        /// </summary>
        private static void DemonstrateBasicConcept()
        {
            Console.WriteLine("=== 1. 明主效应基本概念演示 ===");
            Console.WriteLine();

            // 创建不同类型的势力
            var scenarios = new[]
            {
                new { Name = "明主型", Ruler = "曹操", RulerInt = 96, Advisor = "程昱", AdvisorInt = 90 },
                new { Name = "军师型", Ruler = "刘备", RulerInt = 75, Advisor = "诸葛亮", AdvisorInt = 100 },
                new { Name = "均衡型", Ruler = "孙权", RulerInt = 80, Advisor = "周瑜", AdvisorInt = 90 },
                new { Name = "昏君型", Ruler = "刘禅", RulerInt = 40, Advisor = "姜维", AdvisorInt = 80 }
            };

            foreach (var scenario in scenarios)
            {
                var faction = CreateTestFaction(scenario.Name, scenario.Ruler, scenario.RulerInt, scenario.Advisor, scenario.AdvisorInt);
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);

                Console.WriteLine($"【{scenario.Name}】{scenario.Ruler} + {scenario.Advisor}");
                Console.WriteLine($"  君主智力: {scenario.RulerInt}");
                Console.WriteLine($"  军师智力: {scenario.AdvisorInt}");
                Console.WriteLine($"  有效智力: {effectiveInt}");
                
                if (scenario.RulerInt > scenario.AdvisorInt)
                    Console.WriteLine($"  效应: 明主把关，纠正军师不足 (+{scenario.RulerInt - scenario.AdvisorInt})");
                else if (scenario.AdvisorInt > scenario.RulerInt)
                    Console.WriteLine($"  效应: 军师提升，弥补君主不足 (+{scenario.AdvisorInt - scenario.RulerInt})");
                else
                    Console.WriteLine($"  效应: 君臣相得，智力相当");
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 演示预测系统集成
        /// </summary>
        private static void DemonstratePredictionIntegration()
        {
            Console.WriteLine("=== 2. 预测系统集成演示 ===");
            Console.WriteLine();

            // 创建对比势力
            var mingzhu = CreateTestFaction("明主", "曹操", 96, "程昱", 90);
            var junshi = CreateTestFaction("军师", "刘备", 75, "诸葛亮", 100);

            var targetPerson = CreateTestPerson("关羽", 85, 95);
            var enemyTroop = CreateTestTroop("张飞", 90, 98, 5000);

            Console.WriteLine("同样的任务，不同势力的预测对比:");
            Console.WriteLine();

            // 招募预测对比
            Console.WriteLine("--- 招募预测对比 ---");
            Console.WriteLine($"目标: {targetPerson.Name} (忠诚{targetPerson.Loyalty})");
            Console.WriteLine();

            var factions = new[] { mingzhu, junshi };
            foreach (var faction in factions)
            {
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                var (rate, comment) = StrategistManager.GetRecruitPrediction(faction, targetPerson);

                Console.WriteLine($"{faction.Name}势力 (有效智力{effectiveInt}):");
                Console.WriteLine($"  预测成功率: {rate}%");
                Console.WriteLine($"  军师评语: {comment}");
                Console.WriteLine();
            }

            // 战斗技能预测对比
            Console.WriteLine("--- 战斗技能预测对比 ---");
            Console.WriteLine($"目标: {enemyTroop.Leader?.Name} 部队");
            Console.WriteLine();

            var skills = new[]
            {
                StrategistManager.SkillType.FirePlot,
                StrategistManager.SkillType.Ambush,
                StrategistManager.SkillType.Confuse
            };

            foreach (var faction in factions)
            {
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                Console.WriteLine($"{faction.Name}势力 (有效智力{effectiveInt}):");

                foreach (var skill in skills)
                {
                    int prediction = StrategistManager.GetBattlePrediction(faction, enemyTroop, skill);
                    string skillName = GetSkillName(skill);
                    Console.WriteLine($"  {skillName}: {prediction}%");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 演示历史案例
        /// </summary>
        private static void DemonstrateHistoricalCases()
        {
            Console.WriteLine("=== 3. 历史案例对比演示 ===");
            Console.WriteLine();

            var historicalCases = new[]
            {
                new { Name = "魏武帝", Ruler = "曹操", RulerInt = 96, Advisor = "荀彧", AdvisorInt = 90, Description = "明主超越军师，亲自把关" },
                new { Name = "蜀昭烈", Ruler = "刘备", RulerInt = 75, Advisor = "诸葛亮", AdvisorInt = 100, Description = "军师提升君主，卧龙辅佐" },
                new { Name = "吴大帝", Ruler = "孙权", RulerInt = 80, Advisor = "周瑜", AdvisorInt = 90, Description = "君臣相得，配合默契" },
                new { Name = "袁本初", Ruler = "袁绍", RulerInt = 70, Advisor = "田丰", AdvisorInt = 85, Description = "刚愎自用，浪费人才" },
                new { Name = "蜀后主", Ruler = "刘禅", RulerInt = 40, Advisor = "姜维", AdvisorInt = 80, Description = "昏君误国，军师难救" }
            };

            foreach (var case_ in historicalCases)
            {
                var faction = CreateTestFaction(case_.Name, case_.Ruler, case_.RulerInt, case_.Advisor, case_.AdvisorInt);
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                bool isAccurate = EffectiveIntelligenceSystem.IsAdviceAccurate(faction);

                Console.WriteLine($"【{case_.Name}】{case_.Ruler}({case_.RulerInt}) + {case_.Advisor}({case_.AdvisorInt})");
                Console.WriteLine($"  有效智力: {effectiveInt}");
                Console.WriteLine($"  谏言准确: {(isAccurate ? "是" : "否")}");
                Console.WriteLine($"  历史评价: {case_.Description}");
                Console.WriteLine();
            }

            // 显示详细分析
            Console.WriteLine("--- 详细分析报告 ---");
            var caocao = CreateTestFaction("魏", "曹操", 96, "荀彧", 90);
            var liubei = CreateTestFaction("蜀", "刘备", 75, "诸葛亮", 100);
            
            Console.WriteLine(EffectiveIntelligenceSystem.GetWiseRulerAnalysis(caocao));
            Console.WriteLine(EffectiveIntelligenceSystem.GetWiseRulerAnalysis(liubei));
        }

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

            if (!string.IsNullOrEmpty(advisorName) && advisorInt > 0)
            {
                faction.Advisor = new Person
                {
                    Name = advisorName,
                    Intelligence = advisorInt,
                    Command = advisorInt - 15,
                    Charm = advisorInt - 10,
                    Politics = advisorInt - 5,
                    BelongedFaction = faction
                };
            }

            return faction;
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int loyalty, int intelligence)
        {
            return new Person
            {
                Name = name,
                Loyalty = loyalty,
                Intelligence = intelligence,
                Command = intelligence - 5,
                Strength = intelligence + 10,
                Charm = intelligence - 10,
                Politics = intelligence - 15
            };
        }

        /// <summary>
        /// 创建测试部队
        /// </summary>
        private static Troop CreateTestTroop(string leaderName, int command, int strength, int quantity)
        {
            var leader = new Person
            {
                Name = leaderName,
                Command = command,
                Strength = strength,
                Intelligence = (command + strength) / 2,
                Calmness = command - 10
            };

            return new Troop
            {
                Leader = leader,
                Quantity = quantity,
                Morale = 80
            };
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