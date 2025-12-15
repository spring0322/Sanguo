using System;
using System.Collections.Generic;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// AI战略决策权重 - 扩展AI行为模式
    /// </summary>
    public class StrategicDecisionWeights
    {
        public float Aggression { get; set; } = 1.0f;        // 进攻欲望
        public float Defense { get; set; } = 1.0f;           // 防守倾向
        public float ExpansionDistance { get; set; } = 1.0f; // 远征意愿
        public float RiskTolerance { get; set; } = 1.0f;     // 风险承受度(敢不敢以少打多)
        public float DiplomacyBias { get; set; } = 1.0f;     // 外交友好度

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public override string ToString()
        {
            return $"进攻:{Aggression:F1} 防守:{Defense:F1} 远征:{ExpansionDistance:F1} 风险:{RiskTolerance:F1} 外交:{DiplomacyBias:F1}";
        }
    }

    /// <summary>
    /// 战略决策管理器 - 基于势力领袖策略倾向计算决策权重
    /// </summary>
    public static class StrategicDecisionManager
    {
        // 默认权重配置（基于PersonStrategyTendency）
        private static readonly Dictionary<PersonStrategyTendency, StrategicDecisionWeights> _defaultWeights
            = new Dictionary<PersonStrategyTendency, StrategicDecisionWeights>
        {
            {
                PersonStrategyTendency.统一全国, new StrategicDecisionWeights
                {
                    Aggression = 1.5f,        // 高进攻欲望
                    Defense = 0.8f,           // 较低防守
                    ExpansionDistance = 1.5f, // 愿意远征
                    RiskTolerance = 1.3f,     // 敢于冒险
                    DiplomacyBias = 0.5f      // 不太重视外交
                }
            },
            {
                PersonStrategyTendency.统一地区, new StrategicDecisionWeights
                {
                    Aggression = 1.2f,        // 中等进攻
                    Defense = 1.0f,           // 平衡防守
                    ExpansionDistance = 1.0f, // 中等远征意愿
                    RiskTolerance = 1.0f,     // 正常风险
                    DiplomacyBias = 1.0f      // 正常外交
                }
            },
            {
                PersonStrategyTendency.统一州, new StrategicDecisionWeights
                {
                    Aggression = 1.0f,        // 较低进攻
                    Defense = 1.2f,           // 中等防守
                    ExpansionDistance = 0.7f, // 不愿远征
                    RiskTolerance = 0.8f,     // 较保守
                    DiplomacyBias = 1.3f      // 重视外交
                }
            },
            {
                PersonStrategyTendency.维持现状, new StrategicDecisionWeights
                {
                    Aggression = 0.5f,        // 低进攻
                    Defense = 1.5f,           // 高防守
                    ExpansionDistance = 0.3f, // 极不愿远征
                    RiskTolerance = 0.5f,     // 非常保守
                    DiplomacyBias = 1.5f      // 非常重视外交
                }
            }
        };

        /// <summary>
        /// 获取势力的战略决策权重
        /// (Delegates to AIStrategicEvaluator for unified calculation)
        /// </summary>
        public static StrategicDecisionWeights GetWeights(Faction faction)
        {
            // Use AIStrategicEvaluator for unified weight calculation
            // This integrates StrategyTendency, ValuationOnGovernment, and Army Quality
            return AIStrategicEvaluator.CalculateFactionWeights(faction);
        }

        /// <summary>
        /// 基于领袖属性微调权重
        /// </summary>
        private static StrategicDecisionWeights AdjustWeightsByLeader(StrategicDecisionWeights baseWeights, Person leader)
        {
            var adjusted = new StrategicDecisionWeights
            {
                Aggression = baseWeights.Aggression,
                Defense = baseWeights.Defense,
                ExpansionDistance = baseWeights.ExpansionDistance,
                RiskTolerance = baseWeights.RiskTolerance,
                DiplomacyBias = baseWeights.DiplomacyBias
            };

            // 勇猛影响进攻欲望
            if (leader.Braveness >= 80)
                adjusted.Aggression *= 1.2f;
            else if (leader.Braveness <= 40)
                adjusted.Aggression *= 0.8f;

            // 冷静影响风险承受度
            if (leader.Calmness >= 80)
                adjusted.RiskTolerance *= 0.8f; // 冷静者更谨慎
            else if (leader.Calmness <= 40)
                adjusted.RiskTolerance *= 1.2f;

            // 智力影响外交
            if (leader.Intelligence >= 80)
                adjusted.DiplomacyBias *= 1.2f;

            // 魅力影响外交
            if (leader.Glamour >= 80)
                adjusted.DiplomacyBias *= 1.1f;

            return adjusted;
        }

        /// <summary>
        /// 判断是否应该发起进攻
        /// </summary>
        public static bool ShouldAttack(Faction faction, float enemyStrengthRatio)
        {
            var weights = GetWeights(faction);
            
            // 基础进攻意愿
            float attackDesire = weights.Aggression * 0.5f;
            
            // 风险评估：敌我力量对比
            if (enemyStrengthRatio < 1.0f) // 我方更强
                attackDesire += (1.0f - enemyStrengthRatio) * weights.RiskTolerance;
            else // 敌方更强
                attackDesire -= (enemyStrengthRatio - 1.0f) * (2.0f - weights.RiskTolerance);

            return attackDesire > 0.5f;
        }

        /// <summary>
        /// 判断是否应该远征
        /// </summary>
        public static bool ShouldExpand(Faction faction, int distance)
        {
            var weights = GetWeights(faction);
            
            // 距离惩罚
            float distancePenalty = distance * 0.1f;
            
            return weights.ExpansionDistance > distancePenalty;
        }

        /// <summary>
        /// 获取外交友好度评分
        /// </summary>
        public static float GetDiplomacyScore(Faction faction)
        {
            var weights = GetWeights(faction);
            return weights.DiplomacyBias;
        }

        /// <summary>
        /// 获取势力战略摘要
        /// </summary>
        public static string GetStrategySummary(Faction faction)
        {
            if (faction == null || faction.Leader == null)
                return "无势力信息";

            var tendency = faction.Leader.StrategyTendency;
            var weights = GetWeights(faction);

            string tendencyDesc;
            switch (tendency)
            {
                case PersonStrategyTendency.统一全国:
                    tendencyDesc = "统一全国 - 雄心壮志";
                    break;
                case PersonStrategyTendency.统一地区:
                    tendencyDesc = "统一地区 - 稳扎稳打";
                    break;
                case PersonStrategyTendency.统一州:
                    tendencyDesc = "统一州 - 割据一方";
                    break;
                case PersonStrategyTendency.维持现状:
                    tendencyDesc = "维持现状 - 自保为主";
                    break;
                default:
                    tendencyDesc = "未知";
                    break;
            }

            return $"{faction.Name}: {tendencyDesc}\n权重: {weights}";
        }
    }
}
