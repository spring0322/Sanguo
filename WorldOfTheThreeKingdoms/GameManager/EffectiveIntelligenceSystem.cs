using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 有效智力系统 - 实现"明主效应"
    /// 高智力君主可以弥补军师不足，低智力君主会被军师提升
    /// </summary>
    public static class EffectiveIntelligenceSystem
    {
        /// <summary>
        /// 获取势力的有效谋略值
        /// 核心逻辑：明主可以亲自把关，弥补军师不足
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>有效智力值</returns>
        public static int GetEffectiveIntelligence(Faction faction)
        {
            if (faction == null) return 0;
            
            // 如果没有军师，只能依靠君主自己
            if (faction.Advisor == null)
            {
                int leaderInt = faction.Leader?.Intelligence ?? 0;
                System.Diagnostics.Debug.WriteLine($"[有效智力] {faction.Name} 无军师，仅依靠君主智力: {leaderInt}");
                return leaderInt;
            }
            
            // 如果没有君主，只能依靠军师
            if (faction.Leader == null)
            {
                int advisorInt = faction.Advisor.Intelligence;
                System.Diagnostics.Debug.WriteLine($"[有效智力] {faction.Name} 无君主，仅依靠军师智力: {advisorInt}");
                return advisorInt;
            }
            
            // 核心逻辑：取君主和军师的较高值
            // 这体现了"明主效应" - 高智力君主可以识别和纠正军师的错误判断
            int leaderIntelligence = faction.Leader.Intelligence;
            int advisorIntelligence = faction.Advisor.Intelligence;
            int effectiveIntelligence = Math.Max(leaderIntelligence, advisorIntelligence);
            
            System.Diagnostics.Debug.WriteLine($"[有效智力] {faction.Name}: 君主{leaderIntelligence} vs 军师{advisorIntelligence} -> 有效智力{effectiveIntelligence}");
            
            return effectiveIntelligence;
        }

        /// <summary>
        /// 判定谏言是否准确
        /// 基于有效智力计算准确率
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>是否准确</returns>
        public static bool IsAdviceAccurate(Faction faction)
        {
            int effectiveInt = GetEffectiveIntelligence(faction);
            
            // 智力100的势力，谏言必定准确
            if (effectiveInt >= 100) 
            {
                System.Diagnostics.Debug.WriteLine($"[谏言准确性] {faction.Name} 有效智力{effectiveInt}，谏言必定准确");
                return true;
            }
            
            // 智力越高，准确率越高
            // 基础60%，智力每高于70一点增加1.5%
            int baseChance = 60;
            int intelligenceBonus = (int)((effectiveInt - 70) * 1.5);
            int totalChance = baseChance + intelligenceBonus;
            
            // 限制在合理范围内
            totalChance = Math.Max(10, Math.Min(95, totalChance));
            
            bool isAccurate = GameObject.Random(100) < totalChance;
            
            System.Diagnostics.Debug.WriteLine($"[谏言准确性] {faction.Name} 有效智力{effectiveInt}，准确率{totalChance}% -> {(isAccurate ? "准确" : "不准确")}");
            
            return isAccurate;
        }

        /// <summary>
        /// 获取明主效应的详细分析
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>分析报告</returns>
        public static string GetWiseRulerAnalysis(Faction faction)
        {
            if (faction?.Leader == null || faction.Advisor == null)
                return "缺少君主或军师，无法分析明主效应";

            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== {faction.Name} 明主效应分析 ===");
            
            int leaderInt = faction.Leader.Intelligence;
            int advisorInt = faction.Advisor.Intelligence;
            int effectiveInt = Math.Max(leaderInt, advisorInt);
            
            analysis.AppendLine($"君主智力: {leaderInt}");
            analysis.AppendLine($"军师智力: {advisorInt}");
            analysis.AppendLine($"有效智力: {effectiveInt}");
            analysis.AppendLine();
            
            // 分析明主效应类型
            if (leaderInt > advisorInt)
            {
                int advantage = leaderInt - advisorInt;
                analysis.AppendLine("【明主效应】君主智力超越军师");
                analysis.AppendLine($"智力优势: +{advantage}");
                analysis.AppendLine("效果: 君主可以识别并纠正军师的错误判断");
                
                if (advantage >= 20)
                    analysis.AppendLine("评价: 明主慧眼，军师辅佐有余");
                else if (advantage >= 10)
                    analysis.AppendLine("评价: 君主略胜一筹，能够把关");
                else
                    analysis.AppendLine("评价: 君主稍有优势，偶能纠错");
            }
            else if (advisorInt > leaderInt)
            {
                int advantage = advisorInt - leaderInt;
                analysis.AppendLine("【军师效应】军师智力超越君主");
                analysis.AppendLine($"智力优势: +{advantage}");
                analysis.AppendLine("效果: 军师提升势力整体谋略水平");
                
                if (advantage >= 30)
                    analysis.AppendLine("评价: 卧龙凤雏，君主得一可安天下");
                else if (advantage >= 20)
                    analysis.AppendLine("评价: 军师才华横溢，大幅提升势力智谋");
                else if (advantage >= 10)
                    analysis.AppendLine("评价: 军师学识渊博，有效辅佐君主");
                else
                    analysis.AppendLine("评价: 军师略有专长，小有助益");
            }
            else
            {
                analysis.AppendLine("【君臣相得】智力相当");
                analysis.AppendLine("效果: 君主与军师智力相当，配合默契");
                analysis.AppendLine("评价: 君臣一心，其利断金");
            }
            
            analysis.AppendLine();
            
            // 计算谏言准确率
            int accuracyRate = CalculateAccuracyRate(effectiveInt);
            analysis.AppendLine($"谏言准确率: {accuracyRate}%");
            
            if (accuracyRate >= 90)
                analysis.AppendLine("谏言质量: 料事如神，几无差错");
            else if (accuracyRate >= 80)
                analysis.AppendLine("谏言质量: 深谋远虑，多有先见");
            else if (accuracyRate >= 70)
                analysis.AppendLine("谏言质量: 颇有见地，值得参考");
            else if (accuracyRate >= 60)
                analysis.AppendLine("谏言质量: 偶有妙计，需要甄别");
            else
                analysis.AppendLine("谏言质量: 见识有限，多有偏差");
            
            return analysis.ToString();
        }

        /// <summary>
        /// 计算谏言准确率
        /// </summary>
        /// <param name="effectiveIntelligence">有效智力</param>
        /// <returns>准确率百分比</returns>
        private static int CalculateAccuracyRate(int effectiveIntelligence)
        {
            if (effectiveIntelligence >= 100) return 95; // 最高95%，保留一点不确定性
            
            int baseChance = 60;
            int intelligenceBonus = (int)((effectiveIntelligence - 70) * 1.5);
            int totalChance = baseChance + intelligenceBonus;
            
            return Math.Max(10, Math.Min(95, totalChance));
        }

        /// <summary>
        /// 获取历史上的明主效应案例
        /// </summary>
        /// <returns>历史案例说明</returns>
        public static string GetHistoricalExamples()
        {
            var examples = new System.Text.StringBuilder();
            examples.AppendLine("=== 历史上的明主效应案例 ===");
            examples.AppendLine();
            
            examples.AppendLine("【明主超越军师】");
            examples.AppendLine("曹操(96) + 程昱(90) = 有效智力96");
            examples.AppendLine("  - 曹操智力超群，能够识别程昱建议的优劣");
            examples.AppendLine("  - 即使程昱偶有失误，曹操也能及时纠正");
            examples.AppendLine();
            
            examples.AppendLine("司马懿(95) + 普通谋士(75) = 有效智力95");
            examples.AppendLine("  - 司马懿本身就是顶级谋士，不依赖他人");
            examples.AppendLine("  - 手下谋士主要起辅助作用");
            examples.AppendLine();
            
            examples.AppendLine("【军师提升君主】");
            examples.AppendLine("刘备(75) + 诸葛亮(100) = 有效智力100");
            examples.AppendLine("  - 诸葛亮的超凡智慧大幅提升蜀汉谋略水平");
            examples.AppendLine("  - 刘备善于纳谏，充分发挥诸葛亮的才能");
            examples.AppendLine();
            
            examples.AppendLine("袁绍(70) + 田丰(85) = 有效智力85");
            examples.AppendLine("  - 田丰智力超过袁绍，本应提升势力谋略");
            examples.AppendLine("  - 但袁绍刚愎自用，经常不听建议，效果打折");
            examples.AppendLine();
            
            examples.AppendLine("【君臣相得】");
            examples.AppendLine("孙权(80) + 周瑜(90) = 有效智力90");
            examples.AppendLine("  - 孙权年轻有为，周瑜才华横溢");
            examples.AppendLine("  - 君臣配合默契，发挥出最大效果");
            examples.AppendLine();
            
            examples.AppendLine("【反面案例】");
            examples.AppendLine("刘禅(40) + 姜维(80) = 有效智力80");
            examples.AppendLine("  - 理论上姜维应该大幅提升蜀汉智谋");
            examples.AppendLine("  - 但刘禅昏庸，无法有效利用姜维才能");
            examples.AppendLine("  - 实际效果可能需要打折扣");
            
            return examples.ToString();
        }

        /// <summary>
        /// 比较不同势力的有效智力
        /// </summary>
        /// <param name="factions">势力列表</param>
        /// <returns>比较报告</returns>
        public static string CompareFactionIntelligence(params Faction[] factions)
        {
            if (factions == null || factions.Length == 0)
                return "没有提供势力进行比较";

            var comparison = new System.Text.StringBuilder();
            comparison.AppendLine("=== 势力有效智力对比 ===");
            comparison.AppendLine();

            var factionData = new List<(Faction faction, int effectiveInt, string analysis)>();

            foreach (var faction in factions)
            {
                if (faction == null) continue;

                int effectiveInt = GetEffectiveIntelligence(faction);
                string analysis = GetIntelligenceAdvantageType(faction);
                factionData.Add((faction, effectiveInt, analysis));
            }

            // 按有效智力排序
            factionData.Sort((a, b) => b.effectiveInt.CompareTo(a.effectiveInt));

            comparison.AppendLine("排名 | 势力 | 有效智力 | 君主 | 军师 | 优势类型");
            comparison.AppendLine("-----|------|----------|------|------|----------");

            for (int i = 0; i < factionData.Count; i++)
            {
                var data = factionData[i];
                string leaderName = data.faction.Leader?.Name ?? "无";
                string advisorName = data.faction.Advisor?.Name ?? "无";
                int leaderInt = data.faction.Leader?.Intelligence ?? 0;
                int advisorInt = data.faction.Advisor?.Intelligence ?? 0;

                comparison.AppendLine($"{i + 1,4} | {data.faction.Name,-8} | {data.effectiveInt,8} | {leaderName}({leaderInt}) | {advisorName}({advisorInt}) | {data.analysis}");
            }

            comparison.AppendLine();
            comparison.AppendLine("分析说明:");
            comparison.AppendLine("- 明主型: 君主智力 > 军师智力，君主能够把关纠错");
            comparison.AppendLine("- 军师型: 军师智力 > 君主智力，军师提升势力水平");
            comparison.AppendLine("- 均衡型: 君主与军师智力相当，配合默契");
            comparison.AppendLine("- 独立型: 无军师或无君主，单独决策");

            return comparison.ToString();
        }

        /// <summary>
        /// 获取智力优势类型
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>优势类型描述</returns>
        private static string GetIntelligenceAdvantageType(Faction faction)
        {
            if (faction?.Leader == null && faction?.Advisor == null)
                return "无领导";
            if (faction.Leader == null)
                return "纯军师";
            if (faction.Advisor == null)
                return "无军师";

            int leaderInt = faction.Leader.Intelligence;
            int advisorInt = faction.Advisor.Intelligence;

            if (leaderInt > advisorInt + 10)
                return "明主型";
            else if (advisorInt > leaderInt + 10)
                return "军师型";
            else
                return "均衡型";
        }

        /// <summary>
        /// 测试明主效应系统
        /// </summary>
        public static void TestWiseRulerEffect()
        {
            Console.WriteLine("=== 明主效应系统测试 ===");
            Console.WriteLine();

            // 创建测试势力
            var testFactions = new[]
            {
                CreateTestFaction("魏", "曹操", 96, "荀彧", 90),      // 明主型
                CreateTestFaction("蜀", "刘备", 75, "诸葛亮", 100),   // 军师型
                CreateTestFaction("吴", "孙权", 80, "周瑜", 90),      // 军师略强
                CreateTestFaction("袁", "袁绍", 70, "田丰", 85),      // 军师型但君主刚愎
                CreateTestFaction("董", "董卓", 60, "李儒", 75),      // 军师型
                CreateTestFaction("张", "张飞", 65, null, 0)          // 无军师
            };

            foreach (var faction in testFactions)
            {
                Console.WriteLine(GetWiseRulerAnalysis(faction));
                Console.WriteLine();
            }

            // 对比分析
            Console.WriteLine(CompareFactionIntelligence(testFactions));
        }

        /// <summary>
        /// 创建测试势力
        /// </summary>
        private static Faction CreateTestFaction(string name, string leaderName, int leaderInt, string advisorName, int advisorInt)
        {
            var faction = new Faction { Name = name };

            if (!string.IsNullOrEmpty(leaderName))
            {
                faction.Leader = new Person
                {
                    Name = leaderName,
                    Intelligence = leaderInt,
                    BelongedFaction = faction
                };
            }

            if (!string.IsNullOrEmpty(advisorName) && advisorInt > 0)
            {
                faction.Advisor = new Person
                {
                    Name = advisorName,
                    Intelligence = advisorInt,
                    BelongedFaction = faction
                };
            }

            return faction;
        }
    }
}