using System;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 玩家腐败系统 - 通过行政效率衰减平衡玩家扩张
    /// 防止玩家无限扩张导致游戏失去挑战性，与AI难度系统形成双向平衡
    /// </summary>
    public static class PlayerCorruptionSystem
    {
        // --- 配置参数 ---
        // 首都圈：前 5 座城市由君主直辖，行政效率 100%
        private const int DIRECT_CONTROL_LIMIT = 5;
        
        // 腐败率：每多一座城，全局效率下降 0.5%
        private const float CORRUPTION_PER_CITY = 0.005f;
        
        // 最低保底：效率最低降至 50%，防止后期彻底没钱玩不下去
        private const float MIN_EFFICIENCY_FLOOR = 0.5f;

        // 太守政治能力对腐败的抵抗系数
        private const float GOVERNOR_RESISTANCE_FACTOR = 0.2f;

        /// <summary>
        /// 获取玩家当前的行政效率
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <returns>返回 0.5 ~ 1.0 之间的系数</returns>
        public static float GetPlayerEfficiency(int playerCityCount)
        {
            // 1. 首都圈保护
            if (playerCityCount <= DIRECT_CONTROL_LIMIT)
            {
                return 1.0f;
            }

            // 2. 计算超出的城市带来的行政负担
            int excessCities = playerCityCount - DIRECT_CONTROL_LIMIT;
            float totalPenalty = excessCities * CORRUPTION_PER_CITY;

            // 3. 计算剩余效率 (1 - 罚款)，并确保不低于保底值
            float currentEfficiency = 1.0f - totalPenalty;
            return Math.Max(currentEfficiency, MIN_EFFICIENCY_FLOOR);
        }

        /// <summary>
        /// 获取城市的实际收入（考虑太守政治能力）
        /// </summary>
        /// <param name="baseIncome">基础收入</param>
        /// <param name="globalEfficiency">全局效率</param>
        /// <param name="governorPolitics">太守政治能力</param>
        /// <returns>实际收入</returns>
        public static float GetCityActualIncome(float baseIncome, float globalEfficiency, int governorPolitics)
        {
            // 政治 100 的太守可以抵抗 20% 的腐败影响
            float governorBonus = (governorPolitics / 100f) * GOVERNOR_RESISTANCE_FACTOR;
            float finalEfficiency = Math.Min(1.0f, Math.Max(0f, globalEfficiency + governorBonus));
            return baseIncome * finalEfficiency;
        }

        /// <summary>
        /// 获取玩家腐败状态信息
        /// </summary>
        /// <returns>腐败状态信息</returns>
        public static PlayerCorruptionStatus GetPlayerCorruptionStatus()
        {
            if (Session.Current?.Scenario == null)
                return new PlayerCorruptionStatus();

            // 计算玩家城池数量
            int playerCityCount = 0;
            Faction playerFaction = null;

            if (Session.Current.Scenario.Factions != null)
            {
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
                {
                    if (Session.Current.Scenario.IsPlayer(faction))
                    {
                        playerFaction = faction;
                        playerCityCount = faction.Architectures?.Count ?? 0;
                        break;
                    }
                }
            }

            float efficiency = GetPlayerEfficiency(playerCityCount);
            int excessCities = Math.Max(0, playerCityCount - DIRECT_CONTROL_LIMIT);
            float corruptionRate = excessCities * CORRUPTION_PER_CITY;

            return new PlayerCorruptionStatus
            {
                PlayerCityCount = playerCityCount,
                DirectControlLimit = DIRECT_CONTROL_LIMIT,
                ExcessCities = excessCities,
                CorruptionRate = corruptionRate,
                Efficiency = efficiency,
                IsInDirectControl = playerCityCount <= DIRECT_CONTROL_LIMIT,
                IsAtMinimumFloor = Math.Abs(efficiency - MIN_EFFICIENCY_FLOOR) < 0.001f,
                PlayerFaction = playerFaction
            };
        }

        /// <summary>
        /// 计算势力的实际收入（应用腐败系统）
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="baseIncome">基础收入</param>
        /// <returns>实际收入</returns>
        public static int CalculateActualIncome(Faction faction, int baseIncome)
        {
            if (faction == null || Session.Current?.Scenario == null)
                return baseIncome;

            // 只对玩家势力应用腐败系统
            if (!Session.Current.Scenario.IsPlayer(faction))
                return baseIncome;

            var status = GetPlayerCorruptionStatus();
            
            // 如果有详细的城市和太守信息，可以逐城计算
            if (faction.Architectures != null && AIStrategicConfig.EnableDetailedCorruptionCalculation)
            {
                return CalculateDetailedIncome(faction, status);
            }
            else
            {
                // 简化计算：直接应用全局效率
                return (int)(baseIncome * status.Efficiency);
            }
        }

        /// <summary>
        /// 详细计算收入（考虑每个城市的太守能力）
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="status">腐败状态</param>
        /// <returns>实际收入</returns>
        private static int CalculateDetailedIncome(Faction faction, PlayerCorruptionStatus status)
        {
            float totalIncome = 0f;

            foreach (Architecture arch in faction.Architectures.GetList().Cast<Architecture>())
            {
                // 计算城市基础收入
                float cityBaseIncome = arch.Fund + arch.Food; // 简化计算
                
                // 获取太守政治能力
                int governorPolitics = 0;
                if (arch.Mayor != null)
                {
                    governorPolitics = arch.Mayor.Politics;
                }
                else if (arch.BelongedFaction?.Leader != null)
                {
                    // 如果没有太守，使用君主政治能力
                    governorPolitics = arch.BelongedFaction.Leader.Politics;
                }

                // 计算实际收入
                float actualIncome = GetCityActualIncome(cityBaseIncome, status.Efficiency, governorPolitics);
                totalIncome += actualIncome;
            }

            return (int)totalIncome;
        }

        /// <summary>
        /// 获取腐败系统的描述信息
        /// </summary>
        /// <param name="status">腐败状态</param>
        /// <returns>描述信息</returns>
        public static string GetCorruptionDescription(PlayerCorruptionStatus status)
        {
            if (status.IsInDirectControl)
            {
                return $"首都圈直辖 ({status.PlayerCityCount}/{DIRECT_CONTROL_LIMIT}城) - 行政效率100%";
            }
            else if (status.IsAtMinimumFloor)
            {
                return $"领土过大，行政效率已降至最低保底 ({status.Efficiency:P0})";
            }
            else
            {
                float efficiencyLoss = (1.0f - status.Efficiency) * 100f;
                return $"领土扩张导致行政效率下降 {efficiencyLoss:F1}% (当前效率: {status.Efficiency:P0})";
            }
        }

        /// <summary>
        /// 获取腐败改善建议
        /// </summary>
        /// <param name="status">腐败状态</param>
        /// <returns>改善建议</returns>
        public static string GetCorruptionAdvice(PlayerCorruptionStatus status)
        {
            if (status.IsInDirectControl)
            {
                return "建议：继续扩张，但注意超过5城后会出现行政效率衰减";
            }
            else if (status.IsAtMinimumFloor)
            {
                return "建议：任命高政治能力的太守，或考虑放弃部分偏远城池";
            }
            else
            {
                return "建议：任命政治能力强的太守来减轻腐败影响，或控制扩张速度";
            }
        }

        /// <summary>
        /// 计算太守对腐败的抵抗效果
        /// </summary>
        /// <param name="governorPolitics">太守政治能力</param>
        /// <returns>抵抗效果百分比</returns>
        public static float CalculateGovernorResistance(int governorPolitics)
        {
            return (governorPolitics / 100f) * GOVERNOR_RESISTANCE_FACTOR * 100f;
        }

        /// <summary>
        /// 预测扩张后的效率变化
        /// </summary>
        /// <param name="currentCities">当前城池数</param>
        /// <param name="additionalCities">计划增加的城池数</param>
        /// <returns>扩张后的效率</returns>
        public static float PredictEfficiencyAfterExpansion(int currentCities, int additionalCities)
        {
            return GetPlayerEfficiency(currentCities + additionalCities);
        }

        /// <summary>
        /// 获取腐败系统的详细分析
        /// </summary>
        /// <returns>分析报告</returns>
        public static CorruptionAnalysis AnalyzeCorruptionSystem(int maxCities = 50)
        {
            var analysis = new CorruptionAnalysis();
            analysis.DirectControlLimit = DIRECT_CONTROL_LIMIT;
            analysis.CorruptionPerCity = CORRUPTION_PER_CITY;
            analysis.MinEfficiencyFloor = MIN_EFFICIENCY_FLOOR;
            analysis.EfficiencyPoints = new System.Collections.Generic.List<CorruptionPoint>();

            for (int cities = 1; cities <= maxCities; cities++)
            {
                var point = new CorruptionPoint
                {
                    CityCount = cities,
                    Efficiency = GetPlayerEfficiency(cities),
                    CorruptionRate = Math.Max(0, (cities - DIRECT_CONTROL_LIMIT) * CORRUPTION_PER_CITY),
                    IsInDirectControl = cities <= DIRECT_CONTROL_LIMIT,
                    IsAtMinimumFloor = Math.Abs(GetPlayerEfficiency(cities) - MIN_EFFICIENCY_FLOOR) < 0.001f
                };
                analysis.EfficiencyPoints.Add(point);
            }

            return analysis;
        }
    }

    /// <summary>
    /// 玩家腐败状态信息
    /// </summary>
    public class PlayerCorruptionStatus
    {
        public int PlayerCityCount { get; set; }
        public int DirectControlLimit { get; set; }
        public int ExcessCities { get; set; }
        public float CorruptionRate { get; set; }
        public float Efficiency { get; set; }
        public bool IsInDirectControl { get; set; }
        public bool IsAtMinimumFloor { get; set; }
        public Faction PlayerFaction { get; set; }

        public PlayerCorruptionStatus()
        {
            DirectControlLimit = 5;
            Efficiency = 1.0f;
        }
    }

    /// <summary>
    /// 腐败系统分析数据
    /// </summary>
    public class CorruptionAnalysis
    {
        public int DirectControlLimit { get; set; }
        public float CorruptionPerCity { get; set; }
        public float MinEfficiencyFloor { get; set; }
        public System.Collections.Generic.List<CorruptionPoint> EfficiencyPoints { get; set; }
    }

    /// <summary>
    /// 腐败曲线上的单个点
    /// </summary>
    public class CorruptionPoint
    {
        public int CityCount { get; set; }
        public float Efficiency { get; set; }
        public float CorruptionRate { get; set; }
        public bool IsInDirectControl { get; set; }
        public bool IsAtMinimumFloor { get; set; }
    }
}