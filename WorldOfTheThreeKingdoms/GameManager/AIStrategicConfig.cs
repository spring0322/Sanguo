using System;

namespace GameManager
{
    /// <summary>
    /// AI战略系统配置
    /// </summary>
    public static class AIStrategicConfig
    {
        #region 势力画像计算参数

        /// <summary>
        /// 君主激进度计算基准
        /// </summary>
        public static int RulerAggressionBase = 200; // (统率 + 野心) / 200

        /// <summary>
        /// 军师智力计算基准
        /// </summary>
        public static int AdvisorWisdomBase = 100; // 智力 / 100

        /// <summary>
        /// 无军师时的基础智力修正
        /// </summary>
        public static float NoAdvisorWisdomBase = 0.3f;

        /// <summary>
        /// 武将好战度计算中义理的权重
        /// </summary>
        public static int PersonalLoyaltyWeight = 20; // 义理 * 20

        /// <summary>
        /// 决策稳定性计算基准
        /// </summary>
        public static int DecisivenessBase = 300; // (智力 + 政治 + 魅力) / 300

        #endregion

        #region 资源快照计算参数

        /// <summary>
        /// 每城期望的经济资源基准
        /// </summary>
        public static int EconomicHealthPerCity = 10000;

        /// <summary>
        /// 威胁检测半径
        /// </summary>
        public static int ThreatDetectionRadius = 3;

        /// <summary>
        /// 邻居检测半径
        /// </summary>
        public static int NeighborDetectionRadius = 5;

        /// <summary>
        /// 军事力量标准化基准
        /// </summary>
        public static float MilitaryPowerNormalization = 10000.0f;

        /// <summary>
        /// 实力优势判定阈值
        /// </summary>
        public static float PowerAdvantageThreshold = 1.5f;

        #endregion

        #region 战略姿态判定阈值

        /// <summary>
        /// 危机模式触发条件
        /// </summary>
        public static class CrisisThresholds
        {
            public static int MinArchitectures = 1;        // 最少城池数
            public static float MinEconomicHealth = 0.2f;  // 最低经济健康度
            public static float MaxThreatLevel = 0.7f;     // 最高威胁等级
        }

        /// <summary>
        /// 防御模式触发条件
        /// </summary>
        public static class DefenseThresholds
        {
            public static float MinThreatLevel = 0.5f;     // 最低威胁等级
            public static float MaxAverageFatigue = 70f;   // 最高平均疲劳度
        }

        /// <summary>
        /// 扩张模式触发条件
        /// </summary>
        public static class ExpansionThresholds
        {
            public static float MinRulerAggression = 0.7f;    // 最低君主激进度
            public static float MinEconomicHealth = 0.6f;     // 最低经济健康度
            public static float MinOpportunityLevel = 0.4f;   // 最低机会等级
            public static float MaxAverageFatigue = 50f;      // 最高平均疲劳度
        }

        /// <summary>
        /// 机会主义模式触发条件
        /// </summary>
        public static class OpportunisticThresholds
        {
            public static float MinAdvisorWisdom = 0.8f;      // 最低军师智力
            public static float MinOpportunityLevel = 0.6f;   // 最低机会等级
        }

        /// <summary>
        /// 休养生息模式触发条件
        /// </summary>
        public static class StabilizationThresholds
        {
            public static float MaxEconomicHealth = 0.5f;     // 最高经济健康度
            public static float MinAverageFatigue = 60f;      // 最低平均疲劳度
        }

        /// <summary>
        /// 摸鱼模式触发条件
        /// </summary>
        public static class IdleThresholds
        {
            public static float MaxDecisiveness = 0.4f;       // 最高决策稳定性
            public static int IdleChance = 30;                // 摸鱼概率(%)
        }

        /// <summary>
        /// 武将压力触发扩张的阈值
        /// </summary>
        public static float GeneralPressureExpansionThreshold = 0.6f;

        #endregion

        #region 行动执行参数

        /// <summary>
        /// 扩张行动配置
        /// </summary>
        public static class ExpansionActions
        {
            public static int RecruitmentPriority = 80;       // 招募优先级
            public static int TrainingPriority = 70;         // 训练优先级
            public static int AttackPriority = 90;           // 攻击优先级
            public static float MinAttackPowerRatio = 1.2f;  // 最小攻击实力比
        }

        /// <summary>
        /// 稳定化行动配置
        /// </summary>
        public static class StabilizationActions
        {
            public static int EconomicDevelopmentPriority = 90;  // 经济发展优先级
            public static int FacilityBuildingPriority = 80;    // 设施建设优先级
            public static int InternalAffairsPriority = 70;     // 内政优先级
            public static int MoraleRecoveryPriority = 60;      // 士气恢复优先级
        }

        /// <summary>
        /// 防御行动配置
        /// </summary>
        public static class DefenseActions
        {
            public static int TroopRecallPriority = 95;       // 军队召回优先级
            public static int FortificationPriority = 85;    // 城防加强优先级
            public static int SupplyStockingPriority = 75;   // 物资储备优先级
            public static int DiplomacyPriority = 65;        // 外交求援优先级
        }

        /// <summary>
        /// 危机处理行动配置
        /// </summary>
        public static class CrisisActions
        {
            public static int PeaceNegotiationPriority = 100; // 求和谈判优先级
            public static int CapitalRelocationPriority = 90; // 迁都优先级
            public static int CaptiveReleasePriority = 80;    // 释放俘虏优先级
            public static int EmergencyRecruitmentPriority = 70; // 紧急征兵优先级
        }

        /// <summary>
        /// 机会主义行动配置
        /// </summary>
        public static class OpportunisticActions
        {
            public static int SneakAttackPriority = 95;       // 偷袭优先级
            public static int OpportunismPriority = 85;       // 趁火打劫优先级
            public static int BetrayalPriority = 75;          // 外交背叛优先级
            public static int RapidExpansionPriority = 80;    // 快速扩张优先级
        }

        #endregion

        #region 调试和日志配置

        /// <summary>
        /// 是否启用AI战略调试日志
        /// </summary>
        public static bool EnableDebugLog = true;

        /// <summary>
        /// 是否启用详细的决策过程日志
        /// </summary>
        public static bool EnableDetailedDecisionLog = false;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public static bool EnablePerformanceMonitoring = false;

        /// <summary>
        /// 是否启用详细腐败计算
        /// </summary>
        public static bool EnableDetailedCorruptionCalculation = false;

        /// <summary>
        /// 是否启用招募对战略的影响
        /// </summary>
        public static bool EnableRecruitmentImpactOnStrategy = true;

        #endregion

        #region 历史君主特性预设

        /// <summary>
        /// 历史君主的特殊战略倾向
        /// </summary>
        public static class HistoricalRulerProfiles
        {
            /// <summary>
            /// 曹操：激进扩张型
            /// </summary>
            public static FactionProfile CaoCao => new FactionProfile
            {
                RulerAggression = 0.9f,
                AdvisorWisdom = 0.8f,
                GeneralWarPressure = 0.7f,
                Decisiveness = 0.9f
            };

            /// <summary>
            /// 刘备：稳健发展型
            /// </summary>
            public static FactionProfile LiuBei => new FactionProfile
            {
                RulerAggression = 0.6f,
                AdvisorWisdom = 0.9f,  // 诸葛亮加成
                GeneralWarPressure = 0.5f,
                Decisiveness = 0.7f
            };

            /// <summary>
            /// 孙权：机会主义型
            /// </summary>
            public static FactionProfile SunQuan => new FactionProfile
            {
                RulerAggression = 0.7f,
                AdvisorWisdom = 0.8f,
                GeneralWarPressure = 0.6f,
                Decisiveness = 0.8f
            };

            /// <summary>
            /// 袁绍：优柔寡断型
            /// </summary>
            public static FactionProfile YuanShao => new FactionProfile
            {
                RulerAggression = 0.5f,
                AdvisorWisdom = 0.6f,
                GeneralWarPressure = 0.8f,  // 武将压力大
                Decisiveness = 0.3f         // 决策不稳定
            };

            /// <summary>
            /// 董卓：暴虐扩张型
            /// </summary>
            public static FactionProfile DongZhuo => new FactionProfile
            {
                RulerAggression = 1.0f,
                AdvisorWisdom = 0.4f,
                GeneralWarPressure = 0.9f,
                Decisiveness = 0.6f
            };
        }

        #endregion

        /// <summary>
        /// 根据君主姓名获取历史特性（如果有的话）
        /// </summary>
        public static FactionProfile? GetHistoricalProfile(string rulerName)
        {
            switch (rulerName)
            {
                case "曹操":
                    return HistoricalRulerProfiles.CaoCao;
                case "刘备":
                    return HistoricalRulerProfiles.LiuBei;
                case "孙权":
                case "孙坚":
                case "孙策":
                    return HistoricalRulerProfiles.SunQuan;
                case "袁绍":
                    return HistoricalRulerProfiles.YuanShao;
                case "董卓":
                    return HistoricalRulerProfiles.DongZhuo;
                default:
                    return null; // 使用动态计算
            }
        }
    }
}