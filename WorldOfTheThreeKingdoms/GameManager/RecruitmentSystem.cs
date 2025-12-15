using System;
using GameObjects;
using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// 招募系统 - 基于"真相->透镜->结果"的设计模式
    /// Truth -> Lens -> Outcome Pattern
    /// </summary>
    public static class RecruitmentSystem
    {
        // --- 第一步：设计客观公式 (The Truth) ---
        /// <summary>
        /// 计算真实的招募成功率（客观事实，玩家不知道）
        /// </summary>
        /// <param name="recruiter">招募者</param>
        /// <param name="target">目标武将</param>
        /// <returns>真实成功率 0-100</returns>
        private static int CalculateRealSuccessRate(Person recruiter, Person target)
        {
            if (recruiter == null || target == null) return 0;

            // 基础公式：魅力差值 + 政治能力 - 忠诚度 + 基础分
            int baseRate = (recruiter.Charm + recruiter.Politics) / 2 - target.Loyalty + 30;

            // 特殊关系修正
            int relationBonus = GetRelationBonus(recruiter, target);
            baseRate += relationBonus;

            // 相性修正：理想相近的人更容易说服
            int compatibilityBonus = GetCompatibilityBonus(recruiter, target);
            baseRate += compatibilityBonus;

            // 势力实力修正：强势力更容易招募
            int powerBonus = GetPowerBonus(recruiter.BelongedFaction, target.BelongedFaction);
            baseRate += powerBonus;

            // 特殊情况修正
            int specialBonus = GetSpecialBonus(recruiter, target);
            baseRate += specialBonus;

            // 钳制在 0% - 100% 之间
            return Math.Max(0, Math.Min(100, baseRate));
        }

        /// <summary>
        /// 获取关系加成
        /// </summary>
        private static int GetRelationBonus(Person recruiter, Person target)
        {
            int relationStatus = recruiter.CheckRelation(target);
            
            if (relationStatus == 1) // 亲密关系
                return 25;
            else if (relationStatus == -1) // 厌恶关系
                return -35;
            
            // 检查特殊关系（父子、兄弟、配偶等）
            if (recruiter.Father == target || target.Father == recruiter)
                return 30; // 父子关系
            if (recruiter.Spouse == target || target.Spouse == recruiter)
                return 35; // 配偶关系
            if (recruiter.Brothers.Contains(target))
                return 25; // 兄弟关系

            return 0;
        }

        /// <summary>
        /// 获取相性加成
        /// </summary>
        private static int GetCompatibilityBonus(Person recruiter, Person target)
        {
            int idealDiff = Math.Abs(recruiter.Ideal - target.Ideal);
            
            if (idealDiff <= 10) return 20;      // 理想非常相近
            else if (idealDiff <= 20) return 10; // 理想比较相近
            else if (idealDiff >= 50) return -15; // 理想差异很大
            
            return 0;
        }

        /// <summary>
        /// 获取势力实力加成
        /// </summary>
        private static int GetPowerBonus(Faction recruiterFaction, Faction targetFaction)
        {
            if (recruiterFaction == null || targetFaction == null) return 0;

            // 简化的实力计算：城市数量 + 武将数量
            int recruiterPower = recruiterFaction.Architectures.Count * 10 + recruiterFaction.Persons.Count;
            int targetPower = targetFaction.Architectures.Count * 10 + targetFaction.Persons.Count;
            
            int powerDiff = recruiterPower - targetPower;
            
            // 实力强的势力更容易招募人才
            if (powerDiff > 50) return 15;      // 实力远超
            else if (powerDiff > 20) return 10; // 实力较强
            else if (powerDiff < -50) return -15; // 实力远弱
            else if (powerDiff < -20) return -10; // 实力较弱
            
            return 0;
        }

        /// <summary>
        /// 获取特殊情况加成
        /// </summary>
        private static int GetSpecialBonus(Person recruiter, Person target)
        {
            int bonus = 0;

            // 历史上的著名组合
            if (IsHistoricalPair(recruiter, target))
                bonus += 25;

            // 如果目标武将的主公已死或被俘
            if (target.BelongedFaction?.Leader == null || target.BelongedFaction.Leader.BelongedCaptive != null)
                bonus += 20;

            // 如果目标武将在敌对势力中不受重用（官职低）
            // if (target.Position < 3) bonus += 10;

            // 如果招募者是君主亲自出马
            if (recruiter == recruiter.BelongedFaction?.Leader)
                bonus += 15;

            return bonus;
        }

        /// <summary>
        /// 检查是否是历史上的著名组合
        /// </summary>
        private static bool IsHistoricalPair(Person recruiter, Person target)
        {
            // 这里可以添加历史上的著名君臣组合
            // 例如：刘备-诸葛亮、曹操-荀彧等
            var famousPairs = new[]
            {
                ("刘备", "诸葛亮"), ("刘备", "关羽"), ("刘备", "张飞"),
                ("曹操", "荀彧"), ("曹操", "郭嘉"), ("曹操", "司马懿"),
                ("孙权", "周瑜"), ("孙权", "鲁肃"), ("孙权", "陆逊")
            };

            string recruiterName = recruiter.Name;
            string targetName = target.Name;

            foreach (var (lord, retainer) in famousPairs)
            {
                if ((recruiterName.Contains(lord) && targetName.Contains(retainer)) ||
                    (recruiterName.Contains(retainer) && targetName.Contains(lord)))
                {
                    return true;
                }
            }

            return false;
        }

        // --- 第二步：军师进行观测 (The Lens) ---
        /// <summary>
        /// 军师预测招募成功率（带有观测误差）
        /// </summary>
        /// <param name="strategist">军师</param>
        /// <param name="recruiter">招募者</param>
        /// <param name="target">目标武将</param>
        /// <returns>军师的预测评语</returns>
        public static (int perceivedRate, string prediction) GetStrategistPrediction(Person strategist, Person recruiter, Person target)
        {
            // 1. 获取真实概率
            int realRate = CalculateRealSuccessRate(recruiter, target);

            // 2. 如果没有军师，直接返回未知
            if (strategist == null) 
                return (-1, "（无人参谋，吉凶未卜）");

            // 3. 计算"观测误差"
            // 智力100 -> 误差 0
            // 智力10  -> 误差 +/- 45
            int errorRange = (100 - strategist.Intelligence) / 2;

            // 生成"感知概率"：在真实概率基础上加减随机误差
            Random random = new Random();
            int randomError = random.Next(-errorRange, errorRange + 1);
            int perceivedRate = realRate + randomError;
            perceivedRate = Math.Max(0, Math.Min(100, perceivedRate));

            // Debug输出，方便调试
            System.Diagnostics.Debug.WriteLine($"[招募预测] 真实率:{realRate}%, 军师智力:{strategist.Intelligence}, 误差范围:±{errorRange}, 军师认为:{perceivedRate}%");

            // 4. 根据"感知概率"生成台词
            string prediction = GenerateStrategistText(strategist, perceivedRate, recruiter, target);

            return (perceivedRate, prediction);
        }

        /// <summary>
        /// 根据军师性格和预测成功率生成评语
        /// </summary>
        private static string GenerateStrategistText(Person strategist, int perceivedRate, Person recruiter, Person target)
        {
            string baseComment = GetBaseComment(perceivedRate);
            string personalizedComment = AddStrategistPersonality(strategist, baseComment, perceivedRate, recruiter, target);
            
            return $"{strategist.Name}：{personalizedComment}";
        }

        /// <summary>
        /// 获取基础评语
        /// </summary>
        private static string GetBaseComment(int rate)
        {
            if (rate >= 95) return "此事万无一失";
            else if (rate >= 80) return "胜算极大，值得一试";
            else if (rate >= 60) return "成功在望，可以尝试";
            else if (rate >= 40) return "胜负五五之数";
            else if (rate >= 20) return "风险颇高，恐难成功";
            else if (rate >= 5) return "希望渺茫，不宜轻试";
            else return "万万不可！此去必败无疑";
        }

        /// <summary>
        /// 根据军师性格添加个人风格
        /// </summary>
        private static string AddStrategistPersonality(Person strategist, string baseComment, int rate, Person recruiter, Person target)
        {
            // 根据军师的性格ID添加不同风格的评语
            int personalityId = strategist.Character?.ID ?? 0;
            
            switch (personalityId)
            {
                case 0: // 仁德型军师
                    if (rate >= 70)
                        return baseComment + "，以德服人，必能感化其心。";
                    else if (rate >= 40)
                        return baseComment + "，当以诚待之，切勿强求。";
                    else
                        return baseComment + "，强求不得，恐伤和气。";
                        
                case 1: // 霸道型军师
                    if (rate >= 70)
                        return baseComment + "，展现我军威势，令其心悦诚服！";
                    else if (rate >= 40)
                        return baseComment + "，可先示之以威，再动之以情。";
                    else
                        return baseComment + "，既然软的不行，不如考虑其他手段。";
                        
                case 2: // 冷静型军师
                    return baseComment + $"，据臣分析，成功概率约为 {rate}%。";
                    
                case 3: // 莽撞型军师
                    if (rate >= 50)
                        return baseComment + "，管他三七二十一，试试再说！";
                    else if (rate >= 20)
                        return baseComment + "，不过富贵险中求，或可一试。";
                    else
                        return baseComment + "，这...臣也没什么好办法。";
                        
                case 4: // 狡诈型军师
                    if (rate >= 70)
                        return baseComment + "，嘿嘿，此人必入我彀中。";
                    else if (rate >= 40)
                        return baseComment + "，需用些手段，方能成事。";
                    else
                        return baseComment + "，此人不好对付，需另想妙计。";
                        
                default:
                    // 添加一些通用的三国风格评语
                    if (rate >= 80)
                        return baseComment + "，主公可放心！";
                    else if (rate >= 60)
                        return baseComment + "，不妨一试。";
                    else if (rate >= 40)
                        return baseComment + "，需谨慎行事。";
                    else
                        return baseComment + "，请主公三思。";
            }
        }

        // --- 第三步：真正执行 (The Outcome) ---
        /// <summary>
        /// 执行招募（基于真实成功率）
        /// </summary>
        /// <param name="recruiter">招募者</param>
        /// <param name="target">目标武将</param>
        /// <returns>是否成功</returns>
        public static bool ExecuteRecruitment(Person recruiter, Person target)
        {
            int realRate = CalculateRealSuccessRate(recruiter, target);
            Random random = new Random();
            int roll = random.Next(0, 100);
            
            bool success = roll < realRate;
            
            System.Diagnostics.Debug.WriteLine($"[招募执行] {recruiter.Name} 招募 {target.Name}: 真实成功率{realRate}%, 骰子{roll}, 结果:{(success ? "成功" : "失败")}");
            
            return success;
        }

        /// <summary>
        /// 获取招募的详细分析报告
        /// </summary>
        public static string GetDetailedAnalysis(Person strategist, Person recruiter, Person target)
        {
            if (strategist == null)
                return "未设军师，无法提供详细分析。";

            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== 军师 {strategist.Name} 的招募分析 ===");
            analysis.AppendLine($"招募者: {recruiter.Name} (魅力{recruiter.Charm} 政治{recruiter.Politics})");
            analysis.AppendLine($"目标: {target.Name} (忠诚{target.Loyalty} 理想{target.Ideal})");
            analysis.AppendLine();

            // 分析各种因素
            analysis.AppendLine("影响因素分析:");
            
            // 基础能力对比
            int baseDiff = (recruiter.Charm + recruiter.Politics) / 2 - target.Loyalty;
            analysis.AppendLine($"  能力对比: {baseDiff:+#;-#;0}% (招募者魅力政治 vs 目标忠诚)");

            // 关系因素
            int relationBonus = GetRelationBonus(recruiter, target);
            if (relationBonus != 0)
                analysis.AppendLine($"  关系修正: {relationBonus:+#;-#;0}%");

            // 相性因素
            int compatibilityBonus = GetCompatibilityBonus(recruiter, target);
            if (compatibilityBonus != 0)
                analysis.AppendLine($"  相性修正: {compatibilityBonus:+#;-#;0}%");

            // 势力实力
            int powerBonus = GetPowerBonus(recruiter.BelongedFaction, target.BelongedFaction);
            if (powerBonus != 0)
                analysis.AppendLine($"  势力实力: {powerBonus:+#;-#;0}%");

            // 特殊情况
            int specialBonus = GetSpecialBonus(recruiter, target);
            if (specialBonus != 0)
                analysis.AppendLine($"  特殊情况: {specialBonus:+#;-#;0}%");

            analysis.AppendLine();

            // 军师的分析能力
            int intelligence = strategist.Intelligence;
            if (intelligence >= 90)
                analysis.AppendLine("军师分析能力: ★★★★★ (洞察秋毫，分析精准)");
            else if (intelligence >= 80)
                analysis.AppendLine("军师分析能力: ★★★★☆ (智谋过人，分析可靠)");
            else if (intelligence >= 70)
                analysis.AppendLine("军师分析能力: ★★★☆☆ (颇有见地，分析尚可)");
            else if (intelligence >= 60)
                analysis.AppendLine("军师分析能力: ★★☆☆☆ (略有小智，分析有限)");
            else
                analysis.AppendLine("军师分析能力: ★☆☆☆☆ (见识有限，分析可能有误)");

            // 获取军师预测
            var (perceivedRate, prediction) = GetStrategistPrediction(strategist, recruiter, target);
            analysis.AppendLine();
            analysis.AppendLine($"军师预测: {prediction}");
            analysis.AppendLine($"预测成功率: {perceivedRate}%");

            // 添加军师的个人见解
            analysis.AppendLine();
            analysis.AppendLine($"军师 {strategist.Name} 的个人见解:");
            
            int personalityId = strategist.Character?.ID ?? 0;
            switch (personalityId)
            {
                case 0: // 仁德型
                    analysis.AppendLine("  「以诚待人，以德服人，方为长久之计。」");
                    break;
                case 1: // 霸道型
                    analysis.AppendLine("  「展现我军实力，令其心悦诚服！」");
                    break;
                case 2: // 冷静型
                    analysis.AppendLine("  「需理性分析各种因素，不可感情用事。」");
                    break;
                case 3: // 莽撞型
                    analysis.AppendLine("  「管他成不成功，试试再说！」");
                    break;
                case 4: // 狡诈型
                    analysis.AppendLine("  「此人心思，某已看透七八分...」");
                    break;
                default:
                    analysis.AppendLine("  「此事成败，还需主公定夺。」");
                    break;
            }

            return analysis.ToString();
        }

        /// <summary>
        /// 获取推荐的招募者
        /// </summary>
        public static Person GetRecommendedRecruiter(Faction faction, Person target)
        {
            if (faction?.Persons == null || target == null) return null;

            Person bestRecruiter = null;
            int highestRate = -1;

            foreach (var person in faction.Persons)
            {
                if (person == null || person.BelongedFaction != faction || person == target) continue;

                int rate = CalculateRealSuccessRate(person, target);
                if (rate > highestRate)
                {
                    highestRate = rate;
                    bestRecruiter = person;
                }
            }

            return bestRecruiter;
        }

        /// <summary>
        /// 处理招募结果
        /// </summary>
        public static void HandleRecruitmentResult(Person recruiter, Person target, bool success)
        {
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"🎉 招募成功！{recruiter.Name} 成功说服了 {target.Name}");
                
                // 执行招募逻辑
                if (target.BelongedFaction != null)
                {
                    target.BelongedFaction.Persons.Remove(target);
                }
                
                target.BelongedFaction = recruiter.BelongedFaction;
                recruiter.BelongedFaction.Persons.Add(target);
                target.Loyalty = 80; // 新招募的武将忠诚度
                
                // 可以触发招募成功事件
                // OnRecruitmentSuccess?.Invoke(recruiter, target);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"😞 招募失败！{target.Name} 拒绝了 {recruiter.Name} 的招募");
                
                // 可能的后果
                target.Loyalty = Math.Min(100, target.Loyalty + 10); // 拒绝招募后忠诚度可能提升
                
                // 可以触发招募失败事件
                // OnRecruitmentFailure?.Invoke(recruiter, target);
            }
        }
    }
}