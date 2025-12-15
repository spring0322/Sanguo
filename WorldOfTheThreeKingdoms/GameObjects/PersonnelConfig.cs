using System;
using System.Collections.Generic;

namespace GameObjects
{
    /// <summary>
    /// 人事管理配置类 - 可调整的参数和策略
    /// </summary>
    public class PersonnelConfig
    {
        // 评分权重配置
        public float LeadershipWeight { get; set; } = 2.0f;
        public float WarWeight { get; set; } = 1.5f;
        public float PoliticsWeight { get; set; } = 2.5f;
        public float IntelligenceWeight { get; set; } = 1.0f;

        // 关系加成配置
        public float SwornBrotherMultiplier { get; set; } = 1.5f;
        public float SpouseMultiplier { get; set; } = 1.5f;
        public float ParentChildMultiplier { get; set; } = 1.3f;
        public float FriendMultiplier { get; set; } = 1.2f;

        // 风险阈值配置
        public float DangerousStabilityThreshold { get; set; } = 0.3f;
        public float MinimumAppointmentScore { get; set; } = 50.0f;
        public int MinimumLoyaltyForCapital { get; set; } = 85;
        public int MinimumLoyaltyForBorder { get; set; } = 75;
        public int MinimumLoyaltyForNormal { get; set; } = 70;

        // 性格特质修正配置
        public Dictionary<Trait, float> FrontlineTraitModifiers { get; set; }
        public Dictionary<Trait, float> BacklineTraitModifiers { get; set; }

        // 经验加成配置
        public Dictionary<string, float> ExperienceModifiers { get; set; }

        // 年龄影响配置
        public float OldAgeThreshold { get; set; } = 60;
        public float YoungAgeThreshold { get; set; } = 30;
        public float OldAgePenalty { get; set; } = 0.9f;
        public float YoungAgeBonus { get; set; } = 1.05f;

        // 轮岗配置
        public int MaxTenureSeasons { get; set; } = 6;
        public float RotationScoreThreshold { get; set; } = 1.1f;

        // 人才培养配置
        public int MaxTraineesPerSeason { get; set; } = 5;
        public int MaxTrainingAge { get; set; } = 35;
        public int MinTrainingImprovement { get; set; } = 1;
        public int MaxTrainingImprovement { get; set; } = 3;

        public PersonnelConfig()
        {
            InitializeDefaultTraitModifiers();
            InitializeDefaultExperienceModifiers();
        }

        private void InitializeDefaultTraitModifiers()
        {
            FrontlineTraitModifiers = new Dictionary<Trait, float>
            {
                { Trait.Timid, 0.7f },
                { Trait.Rash, 1.2f },
                { Trait.Cautious, 1.1f },
                { Trait.Ambitious, 1.1f },
                { Trait.Loyal, 1.1f }
            };

            BacklineTraitModifiers = new Dictionary<Trait, float>
            {
                { Trait.Greedy, 0.8f },
                { Trait.Scholar, 1.3f },
                { Trait.Pragmatic, 1.1f },
                { Trait.Conservative, 1.15f },
                { Trait.Loyal, 1.1f }
            };
        }

        private void InitializeDefaultExperienceModifiers()
        {
            ExperienceModifiers = new Dictionary<string, float>
            {
                { "SiegeDefense", 1.2f },
                { "FieldBattle", 1.15f },
                { "CityManagement", 1.25f },
                { "EconomicReform", 1.2f },
                { "Diplomacy", 1.1f },
                { "Intelligence", 1.15f }
            };
        }

        /// <summary>
        /// 根据势力状态调整配置
        /// </summary>
        public void AdjustForFactionState(FactionState state)
        {
            switch (state)
            {
                case FactionState.WarTime:
                    // 战时提高军事能力权重，降低忠诚度要求
                    LeadershipWeight = 2.5f;
                    WarWeight = 2.0f;
                    MinimumLoyaltyForNormal = 65;
                    MinimumAppointmentScore = 45.0f;
                    break;

                case FactionState.EconomicCrisis:
                    // 经济危机时提高政治能力权重
                    PoliticsWeight = 3.0f;
                    IntelligenceWeight = 1.5f;
                    BacklineTraitModifiers[Trait.Scholar] = 1.5f;
                    break;

                case FactionState.Expansion:
                    // 扩张期平衡发展，提高野心武将的评价
                    FrontlineTraitModifiers[Trait.Ambitious] = 1.3f;
                    BacklineTraitModifiers[Trait.Ambitious] = 1.2f;
                    break;

                case FactionState.Defensive:
                    // 防御期提高谨慎武将的评价
                    FrontlineTraitModifiers[Trait.Cautious] = 1.4f;
                    MinimumLoyaltyForBorder = 80;
                    break;

                case FactionState.Prosperous:
                    // 繁荣期可以更严格的标准
                    MinimumAppointmentScore = 60.0f;
                    MinimumLoyaltyForCapital = 90;
                    break;
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            LeadershipWeight = 2.0f;
            WarWeight = 1.5f;
            PoliticsWeight = 2.5f;
            IntelligenceWeight = 1.0f;

            SwornBrotherMultiplier = 1.5f;
            SpouseMultiplier = 1.5f;
            ParentChildMultiplier = 1.3f;
            FriendMultiplier = 1.2f;

            DangerousStabilityThreshold = 0.3f;
            MinimumAppointmentScore = 50.0f;
            MinimumLoyaltyForCapital = 85;
            MinimumLoyaltyForBorder = 75;
            MinimumLoyaltyForNormal = 70;

            InitializeDefaultTraitModifiers();
            InitializeDefaultExperienceModifiers();
        }

        /// <summary>
        /// 验证配置的合理性
        /// </summary>
        public bool ValidateConfig()
        {
            if (LeadershipWeight < 0 || WarWeight < 0 || PoliticsWeight < 0 || IntelligenceWeight < 0)
                return false;

            if (MinimumLoyaltyForCapital < 0 || MinimumLoyaltyForCapital > 100)
                return false;

            if (DangerousStabilityThreshold < 0 || DangerousStabilityThreshold > 1)
                return false;

            return true;
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public string GetConfigSummary()
        {
            return $"人事配置 - 军事权重:{LeadershipWeight:F1}, 政治权重:{PoliticsWeight:F1}, " +
                   $"最低评分:{MinimumAppointmentScore:F1}, 风险阈值:{DangerousStabilityThreshold:F2}";
        }
    }

    /// <summary>
    /// 人事策略枚举
    /// </summary>
    public enum PersonnelStrategy
    {
        Balanced,       // 平衡策略
        MilitaryFirst,  // 军事优先
        EconomicFirst,  // 经济优先
        LoyaltyFirst,   // 忠诚优先
        MeritBased,     // 唯才是举
        Conservative,   // 保守策略
        Aggressive      // 激进策略
    }

    /// <summary>
    /// 人事策略管理器
    /// </summary>
    public static class PersonnelStrategyManager
    {
        /// <summary>
        /// 应用策略到配置
        /// </summary>
        public static void ApplyStrategy(PersonnelConfig config, PersonnelStrategy strategy)
        {
            config.ResetToDefaults();

            switch (strategy)
            {
                case PersonnelStrategy.MilitaryFirst:
                    config.LeadershipWeight = 3.0f;
                    config.WarWeight = 2.5f;
                    config.PoliticsWeight = 1.5f;
                    config.MinimumLoyaltyForNormal = 65;
                    break;

                case PersonnelStrategy.EconomicFirst:
                    config.PoliticsWeight = 3.5f;
                    config.IntelligenceWeight = 2.0f;
                    config.LeadershipWeight = 1.5f;
                    config.BacklineTraitModifiers[Trait.Scholar] = 1.5f;
                    break;

                case PersonnelStrategy.LoyaltyFirst:
                    config.MinimumLoyaltyForCapital = 95;
                    config.MinimumLoyaltyForBorder = 85;
                    config.MinimumLoyaltyForNormal = 80;
                    config.DangerousStabilityThreshold = 0.5f;
                    config.SwornBrotherMultiplier = 2.0f;
                    break;

                case PersonnelStrategy.MeritBased:
                    config.MinimumLoyaltyForCapital = 70;
                    config.MinimumLoyaltyForBorder = 65;
                    config.MinimumLoyaltyForNormal = 60;
                    config.MinimumAppointmentScore = 70.0f;
                    config.SwornBrotherMultiplier = 1.2f; // 降低关系加成
                    break;

                case PersonnelStrategy.Conservative:
                    config.MinimumAppointmentScore = 60.0f;
                    config.DangerousStabilityThreshold = 0.4f;
                    config.FrontlineTraitModifiers[Trait.Cautious] = 1.3f;
                    config.BacklineTraitModifiers[Trait.Conservative] = 1.3f;
                    break;

                case PersonnelStrategy.Aggressive:
                    config.MinimumAppointmentScore = 40.0f;
                    config.MinimumLoyaltyForNormal = 60;
                    config.FrontlineTraitModifiers[Trait.Ambitious] = 1.4f;
                    config.FrontlineTraitModifiers[Trait.Rash] = 1.3f;
                    break;
            }
        }

        /// <summary>
        /// 获取策略描述
        /// </summary>
        public static string GetStrategyDescription(PersonnelStrategy strategy)
        {
            return strategy switch
            {
                PersonnelStrategy.Balanced => "平衡发展，综合考虑各项因素",
                PersonnelStrategy.MilitaryFirst => "军事优先，重视统率和武力",
                PersonnelStrategy.EconomicFirst => "经济优先，重视政治和智力",
                PersonnelStrategy.LoyaltyFirst => "忠诚优先，严格控制风险",
                PersonnelStrategy.MeritBased => "唯才是举，能力至上",
                PersonnelStrategy.Conservative => "保守稳健，谨慎任命",
                PersonnelStrategy.Aggressive => "激进扩张，敢于用人",
                _ => "未知策略"
            };
        }
    }
}