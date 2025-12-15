using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 招募忠诚度计算器 - 计算武将被招募后的初始忠诚度
    /// </summary>
    public class RecruitmentCalculator
    {
        /// <summary>
        /// 计算初始忠诚度
        /// </summary>
        /// <param name="ruler">君主（决定相性基准）</param>
        /// <param name="recruiter">招募执行人（决定魅力加成）</param>
        /// <param name="target">被招募武将</param>
        /// <param name="method">招募手段 (枚举)</param>
        /// <returns>0-100 的忠诚度数值</returns>
        public static int CalculateInitialLoyalty(Person ruler, Person recruiter, Person target, RecruitMethod method)
        {
            try
            {
                if (ruler == null || recruiter == null || target == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RecruitmentCalculator] 参数为空，返回默认忠诚度50");
                    return 50;
                }

                // 1. 基础相性分 (Ideal Base)
                // 使用游戏的 Ideal 系统计算相性距离 (0-75)
                int affinityDiff = GetAffinityDistance(ruler.Ideal, target.Ideal);
                int baseLoyalty = 100 - (affinityDiff / 2); // 距离越远，基础分越低(最低62)

                // 2. 招募者魅力加成 (Charisma Bonus)
                // 魅力每高 10 点，+1 忠诚；魅力低则扣分
                int charmBonus = (recruiter.Glamour - 60) / 10;

                // 3. 招募方式修正 (Method Correction)
                int methodCorrection = GetMethodCorrection(method);

                // 4. 目标性格修正 (Personality Correction)
                int personalityCorrection = CalculatePersonalityCorrection(target);

                // 5. 综合计算与截断
                int finalLoyalty = baseLoyalty + charmBonus + methodCorrection + personalityCorrection;

                // 6. 硬性限制
                // 即使是死敌投降，最少给 40（否则下回合直接跑了）
                // 即使是亲兄弟，如果通过利诱，也可能不满 100
                int result = Math.Max(40, Math.Min(100, finalLoyalty));

                if (RecruitmentConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[RecruitmentCalculator] 招募忠诚度计算:");
                    System.Diagnostics.Debug.WriteLine($"  君主: {ruler.Name} (理想{ruler.Ideal})");
                    System.Diagnostics.Debug.WriteLine($"  招募者: {recruiter.Name} (魅力{recruiter.Glamour})");
                    System.Diagnostics.Debug.WriteLine($"  目标: {target.Name} (理想{target.Ideal})");
                    System.Diagnostics.Debug.WriteLine($"  方式: {method}");
                    System.Diagnostics.Debug.WriteLine($"  相性差距: {affinityDiff} -> 基础忠诚: {baseLoyalty}");
                    System.Diagnostics.Debug.WriteLine($"  魅力加成: {charmBonus}");
                    System.Diagnostics.Debug.WriteLine($"  方式修正: {methodCorrection}");
                    System.Diagnostics.Debug.WriteLine($"  性格修正: {personalityCorrection}");
                    System.Diagnostics.Debug.WriteLine($"  最终忠诚度: {result}");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecruitmentCalculator] 计算异常: {ex.Message}");
                return 50; // 异常时返回中等忠诚度
            }
        }

        /// <summary>
        /// 计算环形相性距离
        /// </summary>
        private static int GetAffinityDistance(int a, int b)
        {
            int diff = Math.Abs(a - b);
            if (diff > 75) return 150 - diff;
            return diff;
        }

        /// <summary>
        /// 获取招募方式修正值
        /// </summary>
        private static int GetMethodCorrection(RecruitMethod method)
        {
            switch (method)
            {
                case RecruitMethod.PersonalVisit: return 10;  // 君主亲临
                case RecruitMethod.Recommendation: return 5;  // 军师举荐
                case RecruitMethod.Discussion: return 0;      // 舌战/说服
                case RecruitMethod.Money: return -10;         // 金钱利诱
                case RecruitMethod.Captive: return -15;       // 俘虏招降
                default: return 0;
            }
        }

        /// <summary>
        /// 计算目标性格修正
        /// </summary>
        private static int CalculatePersonalityCorrection(Person target)
        {
            int personalityCorrection = 0;

            // 义理越高，初始越稳定
            personalityCorrection += (target.PersonalLoyalty - 2) * 3; // 假设 PersonalLoyalty 是 0(吕布)-4(关羽)

            // 野心越高，初始越低
            personalityCorrection -= (target.Ambition - 2) * 3;

            return personalityCorrection;
        }

        /// <summary>
        /// 获取招募方式的描述
        /// </summary>
        public static string GetMethodDescription(RecruitMethod method)
        {
            switch (method)
            {
                case RecruitMethod.PersonalVisit: return "君主亲自登门拜访";
                case RecruitMethod.Recommendation: return "军师举荐推荐";
                case RecruitMethod.Discussion: return "舌战说服";
                case RecruitMethod.Money: return "金钱利诱";
                case RecruitMethod.Captive: return "俘虏招降";
                default: return "未知方式";
            }
        }

        /// <summary>
        /// 预测招募结果
        /// </summary>
        public static RecruitmentPrediction PredictRecruitment(Person ruler, Person recruiter, Person target, RecruitMethod method)
        {
            int predictedLoyalty = CalculateInitialLoyalty(ruler, recruiter, target, method);
            
            return new RecruitmentPrediction
            {
                PredictedLoyalty = predictedLoyalty,
                Method = method,
                MethodDescription = GetMethodDescription(method),
                RecommendedMethod = GetRecommendedMethod(ruler, recruiter, target),
                RiskLevel = GetRiskLevel(predictedLoyalty)
            };
        }

        /// <summary>
        /// 获取推荐的招募方式
        /// </summary>
        private static RecruitMethod GetRecommendedMethod(Person ruler, Person recruiter, Person target)
        {
            // 计算各种方式的忠诚度，推荐最高的
            var methods = new[] { 
                RecruitMethod.PersonalVisit, 
                RecruitMethod.Recommendation, 
                RecruitMethod.Discussion, 
                RecruitMethod.Money, 
                RecruitMethod.Captive 
            };

            RecruitMethod bestMethod = RecruitMethod.Discussion;
            int bestLoyalty = 0;

            foreach (var method in methods)
            {
                int loyalty = CalculateInitialLoyalty(ruler, recruiter, target, method);
                if (loyalty > bestLoyalty)
                {
                    bestLoyalty = loyalty;
                    bestMethod = method;
                }
            }

            return bestMethod;
        }

        /// <summary>
        /// 获取风险等级
        /// </summary>
        private static RecruitmentRiskLevel GetRiskLevel(int loyalty)
        {
            if (loyalty >= 80) return RecruitmentRiskLevel.Low;
            if (loyalty >= 60) return RecruitmentRiskLevel.Medium;
            if (loyalty >= 45) return RecruitmentRiskLevel.High;
            return RecruitmentRiskLevel.VeryHigh;
        }
    }

    /// <summary>
    /// 招募方式枚举
    /// </summary>
    public enum RecruitMethod
    {
        PersonalVisit,  // 三顾茅庐/亲自登庸
        Recommendation, // 举荐
        Discussion,     // 说服
        Money,          // 利诱
        Captive         // 俘虏
    }

    /// <summary>
    /// 招募预测结果
    /// </summary>
    public class RecruitmentPrediction
    {
        public int PredictedLoyalty { get; set; }
        public RecruitMethod Method { get; set; }
        public string MethodDescription { get; set; }
        public RecruitMethod RecommendedMethod { get; set; }
        public RecruitmentRiskLevel RiskLevel { get; set; }

        public string GetSummary()
        {
            return $"使用{MethodDescription}，预计忠诚度{PredictedLoyalty}，风险等级：{RiskLevel}";
        }
    }

    /// <summary>
    /// 招募风险等级枚举
    /// </summary>
    public enum RecruitmentRiskLevel
    {
        Low,      // 低风险 (80+)
        Medium,   // 中等风险 (60-79)
        High,     // 高风险 (45-59)
        VeryHigh  // 极高风险 (<45)
    }
}