using System;

namespace GameManager
{
    /// <summary>
    /// 招募系统配置类
    /// </summary>
    public static class RecruitmentConfig
    {
        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public static bool EnableDebugLog = true;

        /// <summary>
        /// 最低忠诚度保障
        /// </summary>
        public static int MinimumLoyalty = 40;

        /// <summary>
        /// 最高忠诚度上限
        /// </summary>
        public static int MaximumLoyalty = 100;

        /// <summary>
        /// 魅力基准值（用于计算加成）
        /// </summary>
        public static int CharismaBaseline = 60;

        /// <summary>
        /// 魅力加成系数（每10点魅力的忠诚度加成）
        /// </summary>
        public static int CharismaBonusRate = 1;

        /// <summary>
        /// 相性基准值（用于计算基础忠诚度）
        /// </summary>
        public static int AffinityBaseline = 100;

        /// <summary>
        /// 相性距离惩罚系数
        /// </summary>
        public static int AffinityPenaltyRate = 2;

        /// <summary>
        /// 性格修正系数
        /// </summary>
        public static int PersonalityModifier = 3;

        /// <summary>
        /// 招募方式修正值
        /// </summary>
        public static class MethodModifiers
        {
            public static int PersonalVisit = 10;   // 君主亲临
            public static int Recommendation = 5;   // 军师举荐
            public static int Discussion = 0;       // 舌战/说服
            public static int Money = -10;          // 金钱利诱
            public static int Captive = -15;        // 俘虏招降
        }

        /// <summary>
        /// 风险等级阈值
        /// </summary>
        public static class RiskThresholds
        {
            public static int LowRisk = 80;      // 低风险阈值
            public static int MediumRisk = 60;   // 中等风险阈值
            public static int HighRisk = 45;     // 高风险阈值
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static string GetConfigSummary()
        {
            return $"招募系统配置:\n" +
                   $"- 忠诚度范围: {MinimumLoyalty}-{MaximumLoyalty}\n" +
                   $"- 魅力基准: {CharismaBaseline} (每10点+{CharismaBonusRate}忠诚)\n" +
                   $"- 相性惩罚: 距离/{AffinityPenaltyRate}\n" +
                   $"- 性格系数: ±{PersonalityModifier}\n" +
                   $"- 君主亲临: +{MethodModifiers.PersonalVisit}\n" +
                   $"- 军师举荐: +{MethodModifiers.Recommendation}\n" +
                   $"- 金钱利诱: {MethodModifiers.Money}\n" +
                   $"- 俘虏招降: {MethodModifiers.Captive}";
        }

        /// <summary>
        /// 验证配置参数的合理性
        /// </summary>
        public static bool ValidateConfig()
        {
            if (MinimumLoyalty < 0 || MinimumLoyalty > MaximumLoyalty)
                return false;
            
            if (MaximumLoyalty > 100)
                return false;

            if (CharismaBaseline < 0 || CharismaBaseline > 100)
                return false;

            return true;
        }
    }
}