using System;

namespace GameManager
{
    /// <summary>
    /// AI人才推荐系统配置类
    /// </summary>
    public static class AIRecommendationConfig
    {
        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public static bool EnableDebugLog { get; set; } = false;

        /// <summary>
        /// AI执行推荐所需的最低资金
        /// </summary>
        public static int MinGoldThreshold { get; set; } = 200;

        /// <summary>
        /// 每个城池对应的武将数量比例（缺人判断）
        /// </summary>
        public static int PersonsPerArchitectureRatio { get; set; } = 3;

        /// <summary>
        /// 录用人才的最低四维总和
        /// </summary>
        public static int MinTotalStatsThreshold { get; set; } = 150;

        /// <summary>
        /// 录用人才的最高单项属性最低要求
        /// </summary>
        public static int MinMaxStatThreshold { get; set; } = 65;

        /// <summary>
        /// 最大可接受的相性差距
        /// </summary>
        public static int MaxAffinityDifference { get; set; } = 60;

        /// <summary>
        /// 相性差时要求的最低魅力值
        /// </summary>
        public static int MinGlamourForBadAffinity { get; set; } = 50;

        /// <summary>
        /// 冒险招募所需的最低资金
        /// </summary>
        public static int MinGoldForRiskyRecruitment { get; set; } = 1000;

        /// <summary>
        /// 触发赏赐的忠诚度阈值
        /// </summary>
        public static int LoyaltyThresholdForReward { get; set; } = 80;

        /// <summary>
        /// 赏赐所需的最低资金
        /// </summary>
        public static int MinGoldForReward { get; set; } = 500;

        /// <summary>
        /// 基础赏赐金额
        /// </summary>
        public static int BaseRewardAmount { get; set; } = 100;

        /// <summary>
        /// 基础忠诚度提升
        /// </summary>
        public static int BaseLoyaltyIncrease { get; set; } = 10;

        /// <summary>
        /// 名将阈值（触发全局消息）
        /// </summary>
        public static int FamousPersonThreshold { get; set; } = 80;

        /// <summary>
        /// 需要武将的平均武力阈值
        /// </summary>
        public static int NeedFighterThreshold { get; set; } = 60;

        /// <summary>
        /// 需要文官的平均政治阈值
        /// </summary>
        public static int NeedAdminThreshold { get; set; } = 60;

        /// <summary>
        /// 武将加成倍数
        /// </summary>
        public static float FighterBonusMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// 文官加成倍数
        /// </summary>
        public static float AdminBonusMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// 相性惩罚倍数
        /// </summary>
        public static float AffinityPenaltyMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 重置所有配置为默认值
        /// </summary>
        public static void ResetToDefaults()
        {
            EnableDebugLog = false;
            MinGoldThreshold = 200;
            PersonsPerArchitectureRatio = 3;
            MinTotalStatsThreshold = 150;
            MinMaxStatThreshold = 65;
            MaxAffinityDifference = 60;
            MinGlamourForBadAffinity = 50;
            MinGoldForRiskyRecruitment = 1000;
            LoyaltyThresholdForReward = 80;
            MinGoldForReward = 500;
            BaseRewardAmount = 100;
            BaseLoyaltyIncrease = 10;
            FamousPersonThreshold = 80;
            NeedFighterThreshold = 60;
            NeedAdminThreshold = 60;
            FighterBonusMultiplier = 1.5f;
            AdminBonusMultiplier = 1.5f;
            AffinityPenaltyMultiplier = 1.0f;
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static string GetConfigSummary()
        {
            return $"AI推荐配置: 最低资金{MinGoldThreshold}, 最低四维{MinTotalStatsThreshold}, " +
                   $"最高单项{MinMaxStatThreshold}, 相性阈值{MaxAffinityDifference}, 名将阈值{FamousPersonThreshold}";
        }

        /// <summary>
        /// 根据难度调整配置
        /// </summary>
        public static void AdjustForDifficulty(GameManager.Difficulty difficulty)
        {
            switch (difficulty)
            {
                case GameManager.Difficulty.easy:
                    MinTotalStatsThreshold = 120;
                    MinMaxStatThreshold = 55;
                    MaxAffinityDifference = 75;
                    BaseRewardAmount = 80;
                    break;
                case GameManager.Difficulty.normal:
                    // 使用默认值
                    break;
                case GameManager.Difficulty.hard:
                    MinTotalStatsThreshold = 180;
                    MinMaxStatThreshold = 70;
                    MaxAffinityDifference = 45;
                    BaseRewardAmount = 150;
                    MinGoldForRiskyRecruitment = 800;
                    break;
                case GameManager.Difficulty.veryhard:
                    MinTotalStatsThreshold = 200;
                    MinMaxStatThreshold = 75;
                    MaxAffinityDifference = 30;
                    BaseRewardAmount = 200;
                    MinGoldForRiskyRecruitment = 600;
                    FighterBonusMultiplier = 2.0f;
                    AdminBonusMultiplier = 2.0f;
                    break;
            }
        }
    }
}