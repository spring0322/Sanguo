using System;

namespace GameManager
{
    /// <summary>
    /// 军师推荐系统配置类
    /// </summary>
    public static class AdvisorRecommendationConfig
    {
        /// <summary>
        /// 军师智力门槛（低于此值无法进行推荐）
        /// </summary>
        public static int MinIntelligenceThreshold = 75;

        /// <summary>
        /// 基础成功率（百分比）
        /// </summary>
        public static int BaseSuccessRate = 50;

        /// <summary>
        /// 每点智力增加的成功率（百分比）
        /// </summary>
        public static int IntelligenceBonus = 2;

        /// <summary>
        /// 失败补偿：军师功绩奖励
        /// </summary>
        public static int FailureCompensationMerit = 10;

        /// <summary>
        /// 失败补偿：城市治安奖励
        /// </summary>
        public static int FailureCompensationMorale = 2;

        /// <summary>
        /// 推荐冷却时间（年）
        /// </summary>
        public static int RecommendationCooldownYears = 1;

        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public static bool EnableDebugLog = true;

        /// <summary>
        /// 义理对劝说的惩罚值（每点义理的惩罚）
        /// </summary>
        public static int PersonalLoyaltyPenalty = 5;

        /// <summary>
        /// 最低劝说成功率
        /// </summary>
        public static int MinPersuadeChance = 10;

        /// <summary>
        /// 最高劝说成功率
        /// </summary>
        public static int MaxPersuadeChance = 85;

        /// <summary>
        /// 计算推荐成功率
        /// </summary>
        /// <param name="advisorIntelligence">军师智力</param>
        /// <returns>成功率（0-100）</returns>
        public static int CalculateSuccessRate(int advisorIntelligence)
        {
            if (advisorIntelligence < MinIntelligenceThreshold)
                return 0;

            int successRate = BaseSuccessRate + (advisorIntelligence - MinIntelligenceThreshold) * IntelligenceBonus;
            return Math.Min(100, Math.Max(0, successRate));
        }

        /// <summary>
        /// 获取配置摘要信息
        /// </summary>
        /// <returns>配置信息字符串</returns>
        public static string GetConfigSummary()
        {
            return $"军师推荐系统配置:\n" +
                   $"- 智力门槛: {MinIntelligenceThreshold}\n" +
                   $"- 基础成功率: {BaseSuccessRate}%\n" +
                   $"- 智力加成: 每点+{IntelligenceBonus}%\n" +
                   $"- 功绩补偿: {FailureCompensationMerit}点\n" +
                   $"- 治安补偿: {FailureCompensationMorale}点\n" +
                   $"- 冷却时间: {RecommendationCooldownYears}年";
        }
    }
}