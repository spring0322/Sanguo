using System;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 难度管理器 - 动态平衡系统
    /// 根据玩家势力大小自动调整AI的资源和能力，确保游戏始终具有挑战性
    /// 使用对数曲线实现平滑的难度增长
    /// </summary>
    public class DifficultyManager
    {
        private static DifficultyManager _instance;
        public static DifficultyManager Instance => _instance ?? (_instance = new DifficultyManager());

        // --- 配置参数 ---
        // 触发阈值：玩家占领多少座城之前，AI不获得加成（保护新手期）
        private const int SAFE_THRESHOLD = 8;
        
        // 硬上限：无论玩家多强，AI 加成绝不超过 30% (1.3倍)
        private const float MAX_AI_BONUS = 0.3f;
        
        // 曲线斜率：控制增长快慢，数值越小增长越平缓
        // 计算参考：ln(100城) ≈ 4.6。如果希望100城达到上限0.3，则斜率约为 0.065
        private const float CURVE_SLOPE = 0.07f;

        // 不同类型修正的相对强度
        private const float MILITARY_MODIFIER_RATIO = 0.7f;    // 军事修正为资源修正的70%
        private const float RECRUITMENT_MODIFIER_RATIO = 0.8f; // 招募修正为资源修正的80%
        private const float DIPLOMACY_MODIFIER_RATIO = 1.2f;   // 外交修正为资源修正的120%

        /// <summary>
        /// 获取当前 AI 的资源倍率
        /// </summary>
        /// <param name="playerCityCount">玩家当前持有的城市数量</param>
        /// <returns>返回 1.0 ~ 1.3 之间的倍率</returns>
        public static float GetAIResourceMultiplier(int playerCityCount)
        {
            // 1. 新手保护期：如果玩家地盘很小，直接返回 1.0 (无加成)
            if (playerCityCount <= SAFE_THRESHOLD)
            {
                return 1.0f;
            }

            // 2. 计算超出部分的对数增长
            // 使用 Log (自然对数) 让增长随着基数变大而越来越慢
            int effectiveCount = playerCityCount - SAFE_THRESHOLD;
            float logBonus = (float)(Math.Log(effectiveCount) * CURVE_SLOPE);

            // 3. 钳制结果 (Clamp)，确保不超过硬上限
            float finalBonus = Math.Max(0f, Math.Min(logBonus, MAX_AI_BONUS));
            return 1.0f + finalBonus;
        }

        /// <summary>
        /// 获取AI资源修正系数（兼容旧接口）
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <param name="totalCities">总城池数量</param>
        /// <returns>资源修正系数</returns>
        public float GetAIResourceModifier(int playerCityCount, int totalCities)
        {
            return GetAIResourceMultiplier(playerCityCount);
        }

        /// <summary>
        /// 获取AI军事力量修正系数
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <param name="totalCities">总城池数量</param>
        /// <returns>军事力量修正系数</returns>
        public float GetAIMilitaryModifier(int playerCityCount, int totalCities)
        {
            float baseMultiplier = GetAIResourceMultiplier(playerCityCount);
            float bonus = (baseMultiplier - 1.0f) * MILITARY_MODIFIER_RATIO;
            return 1.0f + bonus;
        }

        /// <summary>
        /// 获取AI招募成功率修正
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <param name="totalCities">总城池数量</param>
        /// <returns>招募成功率修正系数</returns>
        public float GetAIRecruitmentModifier(int playerCityCount, int totalCities)
        {
            // 招募修正有更高的阈值，避免AI在早期垄断人才
            if (playerCityCount <= SAFE_THRESHOLD + 3)
            {
                return 1.0f;
            }

            float baseMultiplier = GetAIResourceMultiplier(playerCityCount);
            float bonus = (baseMultiplier - 1.0f) * RECRUITMENT_MODIFIER_RATIO;
            return 1.0f + bonus;
        }

        /// <summary>
        /// 获取AI外交成功率修正
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <param name="totalCities">总城池数量</param>
        /// <returns>外交成功率修正系数</returns>
        public float GetAIDiplomacyModifier(int playerCityCount, int totalCities)
        {
            // 外交修正从更低的阈值开始，鼓励AI结盟对抗强大的玩家
            if (playerCityCount <= SAFE_THRESHOLD - 2)
            {
                return 1.0f;
            }

            float baseMultiplier = GetAIResourceMultiplier(playerCityCount);
            float bonus = (baseMultiplier - 1.0f) * DIPLOMACY_MODIFIER_RATIO;
            return 1.0f + bonus;
        }

        /// <summary>
        /// 获取当前游戏阶段描述
        /// </summary>
        /// <param name="playerCityCount">玩家城池数量</param>
        /// <param name="totalCities">总城池数量</param>
        /// <returns>游戏阶段描述</returns>
        public string GetGamePhaseDescription(int playerCityCount, int totalCities)
        {
            if (playerCityCount <= SAFE_THRESHOLD)
                return "新手保护期";
            
            float multiplier = GetAIResourceMultiplier(playerCityCount);
            
            if (multiplier < 1.05f)
                return "群雄割据";
            else if (multiplier < 1.15f)
                return "一方之霸";
            else if (multiplier < 1.25f)
                return "问鼎中原";
            else
                return "天下公敌";
        }

        /// <summary>
        /// 获取完整的难度状态信息
        /// </summary>
        /// <returns>难度状态信息</returns>
        public DifficultyStatus GetCurrentDifficultyStatus()
        {
            if (Session.Current?.Scenario == null)
                return new DifficultyStatus();

            // 计算玩家城池数量
            int playerCityCount = 0;
            int totalCities = 0;

            if (Session.Current.Scenario.Architectures != null)
            {
                totalCities = Session.Current.Scenario.Architectures.Count;
                
                foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList().Cast<Architecture>())
                {
                    if (arch.BelongedFaction != null && Session.Current.Scenario.IsPlayer(arch.BelongedFaction))
                    {
                        playerCityCount++;
                    }
                }
            }

            return new DifficultyStatus
            {
                PlayerCityCount = playerCityCount,
                TotalCities = totalCities,
                DominanceRatio = totalCities > 0 ? (float)playerCityCount / totalCities : 0f,
                GamePhase = GetGamePhaseDescription(playerCityCount, totalCities),
                ResourceModifier = GetAIResourceModifier(playerCityCount, totalCities),
                MilitaryModifier = GetAIMilitaryModifier(playerCityCount, totalCities),
                RecruitmentModifier = GetAIRecruitmentModifier(playerCityCount, totalCities),
                DiplomacyModifier = GetAIDiplomacyModifier(playerCityCount, totalCities),
                IsInSafeThreshold = playerCityCount <= SAFE_THRESHOLD,
                EffectiveCityCount = Math.Max(0, playerCityCount - SAFE_THRESHOLD)
            };
        }

        /// <summary>
        /// 应用资源修正到AI势力
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <param name="modifier">修正系数</param>
        public void ApplyResourceModifier(Faction faction, float modifier)
        {
            if (faction == null || modifier <= 1.0f) return;

            try
            {
                // 计算额外资源
                int bonusFund = (int)(faction.Fund * (modifier - 1.0f));
                int bonusFood = (int)(faction.Food * (modifier - 1.0f));

                // 应用资源加成
                faction.Fund += bonusFund;
                faction.Food += bonusFood;

                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[难度管理] {faction.Name} 获得资源加成: 资金+{bonusFund}, 粮食+{bonusFood} (修正系数: {modifier:F3})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[难度管理] 应用资源修正时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 每回合应用难度调整
        /// </summary>
        public void ApplyDifficultyAdjustments()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions == null)
                    return;

                var status = GetCurrentDifficultyStatus();
                
                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[难度管理] 当前游戏阶段: {status.GamePhase}");
                    System.Diagnostics.Debug.WriteLine($"[难度管理] 玩家城池数: {status.PlayerCityCount} (安全阈值: {SAFE_THRESHOLD})");
                    System.Diagnostics.Debug.WriteLine($"[难度管理] 有效城池数: {status.EffectiveCityCount} (超出阈值部分)");
                    System.Diagnostics.Debug.WriteLine($"[难度管理] 玩家统治度: {status.DominanceRatio:P1} ({status.PlayerCityCount}/{status.TotalCities})");
                    System.Diagnostics.Debug.WriteLine($"[难度管理] AI修正系数 - 资源:{status.ResourceModifier:F3}, 军事:{status.MilitaryModifier:F3}, 招募:{status.RecruitmentModifier:F3}, 外交:{status.DiplomacyModifier:F3}");
                    
                    if (status.IsInSafeThreshold)
                    {
                        System.Diagnostics.Debug.WriteLine($"[难度管理] 玩家处于新手保护期，AI不获得加成");
                    }
                    else
                    {
                        float bonusPercentage = (status.ResourceModifier - 1.0f) * 100f;
                        System.Diagnostics.Debug.WriteLine($"[难度管理] AI获得 {bonusPercentage:F1}% 资源加成");
                    }
                }

                // 对所有AI势力应用难度调整
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
                {
                    if (faction.IsAlive && !Session.Current.Scenario.IsPlayer(faction))
                    {
                        // 应用资源修正
                        ApplyResourceModifier(faction, status.ResourceModifier);
                        
                        // 存储修正系数供其他系统使用
                        SetFactionDifficultyModifiers(faction, status);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[难度管理] 应用难度调整时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 为势力设置难度修正系数（供其他系统使用）
        /// </summary>
        private void SetFactionDifficultyModifiers(Faction faction, DifficultyStatus status)
        {
            // 这里可以将修正系数存储到势力的自定义属性中
            // 供招募系统、外交系统等使用
            // 由于不确定Faction类的具体结构，这里用注释说明用途
            
            /*
            faction.CustomProperties["DifficultyResourceModifier"] = status.ResourceModifier;
            faction.CustomProperties["DifficultyMilitaryModifier"] = status.MilitaryModifier;
            faction.CustomProperties["DifficultyRecruitmentModifier"] = status.RecruitmentModifier;
            faction.CustomProperties["DifficultyDiplomacyModifier"] = status.DiplomacyModifier;
            */
        }

        /// <summary>
        /// 获取势力的军事力量修正系数
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>军事力量修正系数</returns>
        public float GetFactionMilitaryModifier(Faction faction)
        {
            if (faction == null || Session.Current?.Scenario == null)
                return 1.0f;

            // 如果是玩家势力，不应用修正
            if (Session.Current.Scenario.IsPlayer(faction))
                return 1.0f;

            var status = GetCurrentDifficultyStatus();
            return status.MilitaryModifier;
        }

        /// <summary>
        /// 获取势力的招募成功率修正系数
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>招募成功率修正系数</returns>
        public float GetFactionRecruitmentModifier(Faction faction)
        {
            if (faction == null || Session.Current?.Scenario == null)
                return 1.0f;

            // 如果是玩家势力，不应用修正
            if (Session.Current.Scenario.IsPlayer(faction))
                return 1.0f;

            var status = GetCurrentDifficultyStatus();
            return status.RecruitmentModifier;
        }

        /// <summary>
        /// 检查是否应该触发AI联盟
        /// </summary>
        /// <returns>是否应该触发AI联盟</returns>
        public bool ShouldTriggerAIAlliance()
        {
            var status = GetCurrentDifficultyStatus();
            
            // 玩家超出安全阈值且AI获得显著加成时，更容易结盟
            return !status.IsInSafeThreshold && status.ResourceModifier > 1.1f;
        }

        /// <summary>
        /// 获取AI联盟倾向修正
        /// </summary>
        /// <returns>联盟倾向修正系数</returns>
        public float GetAIAllianceTendency()
        {
            var status = GetCurrentDifficultyStatus();
            return status.DiplomacyModifier;
        }

        /// <summary>
        /// 获取难度曲线的详细信息（用于调试和分析）
        /// </summary>
        /// <param name="maxCities">要分析的最大城池数</param>
        /// <returns>难度曲线数据</returns>
        public DifficultyAnalysis AnalyzeDifficultyCurve(int maxCities = 50)
        {
            var analysis = new DifficultyAnalysis();
            analysis.SafeThreshold = SAFE_THRESHOLD;
            analysis.MaxBonus = MAX_AI_BONUS;
            analysis.CurveSlope = CURVE_SLOPE;
            analysis.CurvePoints = new System.Collections.Generic.List<DifficultyPoint>();

            for (int cities = 1; cities <= maxCities; cities++)
            {
                var point = new DifficultyPoint
                {
                    PlayerCities = cities,
                    ResourceModifier = GetAIResourceMultiplier(cities),
                    MilitaryModifier = GetAIMilitaryModifier(cities, maxCities),
                    RecruitmentModifier = GetAIRecruitmentModifier(cities, maxCities),
                    DiplomacyModifier = GetAIDiplomacyModifier(cities, maxCities),
                    GamePhase = GetGamePhaseDescription(cities, maxCities)
                };
                analysis.CurvePoints.Add(point);
            }

            return analysis;
        }
    }

    /// <summary>
    /// 难度状态信息
    /// </summary>
    public class DifficultyStatus
    {
        public int PlayerCityCount { get; set; }
        public int TotalCities { get; set; }
        public float DominanceRatio { get; set; }
        public string GamePhase { get; set; }
        public float ResourceModifier { get; set; }
        public float MilitaryModifier { get; set; }
        public float RecruitmentModifier { get; set; }
        public float DiplomacyModifier { get; set; }
        public bool IsInSafeThreshold { get; set; }
        public int EffectiveCityCount { get; set; }

        public DifficultyStatus()
        {
            GamePhase = "未知阶段";
            ResourceModifier = 1.0f;
            MilitaryModifier = 1.0f;
            RecruitmentModifier = 1.0f;
            DiplomacyModifier = 1.0f;
        }
    }

    /// <summary>
    /// 难度分析数据（用于调试和平衡性分析）
    /// </summary>
    public class DifficultyAnalysis
    {
        public int SafeThreshold { get; set; }
        public float MaxBonus { get; set; }
        public float CurveSlope { get; set; }
        public System.Collections.Generic.List<DifficultyPoint> CurvePoints { get; set; }
    }

    /// <summary>
    /// 难度曲线上的单个点
    /// </summary>
    public class DifficultyPoint
    {
        public int PlayerCities { get; set; }
        public float ResourceModifier { get; set; }
        public float MilitaryModifier { get; set; }
        public float RecruitmentModifier { get; set; }
        public float DiplomacyModifier { get; set; }
        public string GamePhase { get; set; }
    }
}