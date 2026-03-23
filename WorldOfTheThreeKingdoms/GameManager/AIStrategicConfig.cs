using System;

namespace GameManager
{
    /// <summary>
    /// AI战略配置
    /// </summary>
    public static class AIStrategicConfig
    {
        // 战略姿态阈值
        public static readonly float ExpansionThreshold = 0.7f;
        public static readonly float AggressiveThreshold = 0.6f;
        public static readonly float StabilizationThreshold = 0.4f;
        public static readonly float DefensiveThreshold = 0.3f;
        public static readonly float ConsolidationThreshold = 0.2f;

        // 威胁评估参数
        public static readonly float ThreatDistanceWeight = 0.3f;
        public static readonly float ThreatStrengthWeight = 0.4f;
        public static readonly float ThreatRelationWeight = 0.3f;

        // 资源管理参数
        public static readonly float MinResourceReserve = 0.2f;
        public static readonly float MaxResourceUsage = 0.8f;

        // 军事参数
        public static readonly int MinTroopCount = 3;
        public static readonly int MaxTroopCount = 20;
        public static readonly float TroopStrengthThreshold = 0.6f;

        // 外交参数
        public static readonly int MinDiplomacyRelation = -50;
        public static readonly int MaxDiplomacyRelation = 100;
        public static readonly int AllianceThreshold = 70;
        public static readonly int WarThreshold = -30;

        // 学习参数
        public static readonly float LearningRate = 0.1f;
        public static readonly int MaxLearningHistory = 100;
        public static readonly float AdaptationThreshold = 0.05f;

        // 调试开关
        public static bool EnableDebugLog = true;
        public static bool EnablePerformanceLog = false;
        public static bool EnableDetailedAnalysis = true;

        /// <summary>
        /// 获取战略姿态权重
        /// </summary>
        public static float GetStrategicStanceWeight(int stanceId)
        {
            // 【临时修复】使用int参数避免StrategicStance枚举依赖问题
            switch (stanceId)
            {
                case 0: // Neutral
                    return 0.8f;
                case 1: // Aggressive
                    return 1.0f;
                case 2: // Defensive
                    return 0.4f;
                case 3: // Consolidation
                    return 0.6f;
                case 4: // Panic
                    return 0.2f;
                case 5: // Cautious
                    return 0.3f;
                case 6: // CoalitionCrusade
                    return 1.2f;
                case 7: // CoalitionSupport
                    return 0.9f;
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// 获取难度修正系数
        /// </summary>
        public static float GetDifficultyModifier(int difficulty)
        {
            return Math.Max(0.5f, Math.Min(2.0f, 1.0f + (difficulty - 3) * 0.2f));
        }

        /// <summary>
        /// 获取时间修正系数
        /// </summary>
        public static float GetTimeModifier(int currentTurn, int maxTurns)
        {
            if (maxTurns <= 0) return 1.0f;
            float progress = (float)currentTurn / maxTurns;
            return 0.8f + progress * 0.4f; // 随时间推进变得更激进
        }
    }
}