using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// AI战术配置管理器
/// 负责加载和管理战术定位、军团编制等配置
/// 日期：2026-03-10 重构：使用 ConfigManagerBase 统一热重载机制
/// </summary>
public static class AITacticalConfigManager
{
    // 内部管理器实例（继承 ConfigManagerBase）
    private static readonly TacticalConfigManager _manager = new();

    /// <summary>
    /// 获取配置实例（线程安全）
    /// </summary>
    public static AITacticalConfig Config => _manager.Config;
    
    /// <summary>
    /// 初始化配置管理器（游戏启动时调用）
    /// </summary>
    public static void Initialize() => _manager.Initialize();
    
    /// <summary>
    /// 热重载更新（主线程 Update 中调用）
    /// </summary>
    public static void Update() => _manager.Update();
    
    /// <summary>
    /// 手动重新加载配置
    /// </summary>
    public static void ReloadConfig() => _manager.ReloadConfig();

    // ==================== 内部管理器类 ====================
    
    private class TacticalConfigManager : ConfigManagerBase<AITacticalConfig>
    {
        protected override string ConfigFileName => "AITacticalConfig.json";
        
        protected override AITacticalConfig CreateDefaultConfig() => CreateDefaultTacticalConfig();

        /// <summary>
        /// 初始化配置管理器（游戏启动时调用）
        /// 重写以添加配置验证（ANTI-BAND-AID：Fail Fast）
        /// </summary>
        public new void Initialize()
        {
            base.Initialize();
            
            // 🔥 关键：初始化后立即验证配置完整性
            ValidateConfig(Config);
        }

        /// <summary>
        /// 验证配置完整性（ANTI-BAND-AID：Fail Fast）
        /// 🧊 COLD PATH：初始化阶段，可读性优先
        /// </summary>
        private static void ValidateConfig(AITacticalConfig config)
        {
            // 🔥 关键：配置必须完整，缺失任何关键字段都应该抛出异常
            if (config.TacticalPositioning == null)
                throw new InvalidOperationException(
                    "配置损坏：AITacticalConfig.TacticalPositioning 为 null，检查 AITacticalConfig.json");

            if (config.TacticalPositioning.TerrainScores == null)
                throw new InvalidOperationException(
                    "配置损坏：TacticalPositioning.TerrainScores 为 null，检查 AITacticalConfig.json");

            if (config.TacticalPositioning.StrategicPosture == null)
                throw new InvalidOperationException(
                    "配置损坏：TacticalPositioning.StrategicPosture 为 null，检查 AITacticalConfig.json");

            if (config.TacticalPositioning.Scores == null)
                throw new InvalidOperationException(
                    "配置损坏：TacticalPositioning.Scores 为 null，检查 AITacticalConfig.json");

            // 验证战略态势配置的完整性
            string[] postures = ["Attack", "Defense", "Garrison"];
            foreach (var posture in postures)
            {
                if (!config.TacticalPositioning.StrategicPosture.ContainsKey(posture))
                    throw new InvalidOperationException(
                        $"配置损坏：TacticalPositioning.StrategicPosture 缺少 '{posture}' 配置");

                var postureConfig = config.TacticalPositioning.StrategicPosture[posture];
                if (postureConfig.ChokePointBonus == null)
                    throw new InvalidOperationException(
                        $"配置损坏：TacticalPositioning.StrategicPosture.{posture}.ChokePointBonus 为 null");
            }
        }
    }

    // ==================== 默认配置创建 ====================

    private static AITacticalConfig CreateDefaultTacticalConfig()
    {
        return new AITacticalConfig
        {
            TacticalPositioning = new TacticalPositioningConfig
            {
                BoundingBoxExpansion = 3,
                DistancePenaltyWeight = 0.1f,
                ActionPriority = new()
                {
                    ["Support"] = 1,
                    ["Mage"] = 2,
                    ["Tank"] = 3,
                    ["DPS"] = 4,
                    ["Logistics"] = 5,
                    ["Balanced"] = 99
                },
                TargetSelection = new()
                {
                    ExecutionThresholdBonus = 500f,
                    StatusAbnormalBonus = 2000f,
                    BlockedByTankBonus = 1500f,
                    LowIntelligenceMultiplier = 10f,
                    AlreadyConfusedPenalty = -5000f,
                    SupportAttackPenalty = -1000f
                },
                TerrainScores = new()
                {
                    ["Tank"] = new()
                    {
                        ["森林"] = 150f,
                        ["山地"] = 200f,
                        ["峻岭"] = 180f,
                        ["水域"] = -5000f
                    },
                    ["DPS"] = new()
                    {
                        ["平原"] = 80f,
                        ["草原"] = 70f,
                        ["水域"] = -5000f
                    },
                    ["Mage"] = new()
                    {
                        ["森林"] = 200f,
                        ["山地"] = 150f,
                        ["水域"] = -5000f
                    },
                    ["Support"] = new()
                    {
                        ["森林"] = 200f,
                        ["山地"] = 150f,
                        ["水域"] = -5000f
                    }
                },
                StrategicPosture = new()
                {
                    ["Attack"] = new()
                    {
                        DistancePenaltyMultiplier = 0.1f,
                        ChokePointBonus = new()
                    },
                    ["Defense"] = new()
                    {
                        DistancePenaltyMultiplier = 0.2f,
                        ChokePointBonus = new()
                        {
                            ["Tank"] = 1500f,
                            ["DPS"] = 500f
                        }
                    },
                    ["Garrison"] = new()
                    {
                        DistancePenaltyMultiplier = 100f,
                        ChokePointBonus = new()
                        {
                            ["Tank"] = 1500f,
                            ["DPS"] = 800f
                        }
                    }
                },
                Scores = new()
                {
                    ["Tank"] = new RoleScoreConfig
                    {
                        ZocLockBonus = 100f,
                        ProtectLineBonus = 50f,
                        ClusterBonus = 20f,
                        MinDistanceToEnemy = 1,
                        OptimalDistanceToEnemy = 1
                    },
                    ["DPS"] = new RoleScoreConfig
                    {
                        ZocLockBonus = 80f,
                        MeleeRiskPenalty = -200f,
                        SafeDistanceMin = 2,
                        OptimalRange = 2
                    },
                    ["Mage"] = new RoleScoreConfig
                    {
                        SafeDistanceMin = 3,
                        DangerZonePenalty = -200f,
                        TankProximityBonus = 50f,
                        OptimalRange = 3
                    },
                    ["Support"] = new RoleScoreConfig
                    {
                        SafeDistanceMin = 3,
                        DangerZonePenalty = -200f,
                        TankProximityBonus = 50f,
                        AllyProximityBonus = 5f,
                        AllyProximityRadius = 2
                    }
                },
                SpecialTroopKinds = new SpecialTroopKindsConfig
                {
                    // 🔥 C# 12 集合表达式：属性类型 List<int> 已明确，直接使用 []
                    RequireZocSuppression = [15, 2]
                }
            },
            StratagemScoring = new StratagemScoringConfig
            {
                BaseScoreMultiplier = 10.0f,
                ConservativeModifier = 0.8f,
                RoleModifiers = new()
                {
                    ["Mage"] = 1.5f,
                    ["Support"] = 1.2f,
                    ["Tank"] = 1.0f,
                    ["DPS"] = 1.0f,
                    ["Logistics"] = 1.0f,
                    ["Balanced"] = 1.0f
                },
                MoraleModifiers = new MoraleModifiersConfig
                {
                    HighMoraleThreshold = 80,
                    HighMoraleBonus = 1.2f,
                    LowMoraleThreshold = 30,
                    LowMoralePenalty = 0.5f
                },
                ExecutionModifier = new ExecutionModifierConfig
                {
                    ThresholdMultiplier = 2.0f,
                    Penalty = 0.3f
                },
                AOEScoring = new AOEScoringConfig
                {
                    DefaultEffectRadius = 0,
                    SecondaryTargetMultiplier = 0.5f,
                    MageSecondaryTargetMultiplier = 0.7f,
                    EnableAOEBonus = true
                }
            },
            FriendlyStratagemScoring = new FriendlyStratagemScoringConfig
            {
                BaseScoreMultiplier = 10.0f,
                ConservativeModifier = 0.8f,
                RoleModifiers = new()
                {
                    ["Support"] = 1.5f,
                    ["Mage"] = 1.2f,
                    ["Tank"] = 1.0f,
                    ["DPS"] = 1.0f,
                    ["Logistics"] = 1.0f,
                    ["Balanced"] = 1.0f
                },
                TargetNeedModifiers = new TargetNeedModifiersConfig
                {
                    LowMoraleThreshold = 40,
                    LowMoraleBonus = 1.5f,
                    CriticalHealthThreshold = 0.3f,
                    CriticalHealthBonus = 1.3f
                },
                TargetValueModifiers = new TargetValueModifiersConfig
                {
                    DPSBonus = 1.2f,
                    TankBonus = 1.1f,
                    SupportBonus = 1.0f,
                    MageBonus = 1.0f
                },
                AOEScoring = new FriendlyAOEScoringConfig
                {
                    SecondaryTargetMultiplier = 0.5f
                }
            },
            StratagemSuccessRate = new StratagemSuccessRateConfig
            {
                BaseSuccessRate = 0.6f,
                IntelligenceInfluence = 0.015f,
                CounterThreshold = -20,
                MinSuccessRate = 0.05f,
                MaxSuccessRate = 0.95f,
                EnableSuccessRatePrediction = true
            },
            CombatMethodScoring = new CombatMethodScoringConfig
            {
                BaseScoreMultiplier = 10.0f,
                ConservativeModifier = 1.0f,
                RoleModifiers = new()
                {
                    ["Mage"] = 1.5f,
                    ["DPS"] = 1.2f,
                    ["Tank"] = 1.0f,
                    ["Support"] = 1.0f,
                    ["Logistics"] = 1.0f,
                    ["Balanced"] = 1.0f
                },
                CombativityModifiers = new CombativityModifiersConfig
                {
                    HighCombativityThreshold = 80,
                    HighCombativityBonus = 1.2f,
                    LowCombativityThreshold = 30,
                    LowCombativityPenalty = 0.5f
                },
                ExecutionModifier = new ExecutionModifierConfig
                {
                    ThresholdMultiplier = 1.0f,
                    Penalty = 0.5f
                }
            },
            CombatMethodSuccessRate = new CombatMethodSuccessRateConfig
            {
                BaseSuccessRate = 0.7f,
                CommandInfluence = 0.015f,
                CounterThreshold = -20,
                MinSuccessRate = 0.1f,
                MaxSuccessRate = 0.95f,
                EnableSuccessRatePrediction = true
            },
            LegionFormation = new LegionFormationConfig
            {
                MinTroopsForFullFormation = 4,
                MinTroopsForTank = 2,
                Formations = new Dictionary<string, FormationConfig>
                {
                    ["Large"] = new FormationConfig { MinSize = 4, TankSlots = 1, SupportSlots = 1 },
                    ["Medium"] = new FormationConfig { MinSize = 2, MaxSize = 3, TankSlots = 1, SupportSlots = 0 },
                    ["Small"] = new FormationConfig { MinSize = 1, MaxSize = 1, TankSlots = 0, SupportSlots = 0 }
                },
                RoleThresholds = new Dictionary<string, RoleThresholdConfig>
                {
                    ["Support"] = new RoleThresholdConfig { MinScore = 50f },
                    ["Mage"] = new RoleThresholdConfig { MinScore = 60f }
                }
            }
        };
    }



    /// <summary>
    /// 获取角色的战术评分配置
    /// </summary>
    public static RoleScoreConfig GetRoleScoreConfig(string roleName)
    {
        // ANTI-BAND-AID：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetRoleScoreConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.TacticalPositioning?.Scores != null,
            "[GetRoleScoreConfig] TacticalPositioning.Scores 配置缺失，检查 AITacticalConfig.json");

        if (Config.TacticalPositioning.Scores.TryGetValue(roleName, out var config))
        {
            return config;
        }

        // ANTI-BAND-AID：找不到角色配置时抛出异常，而非返回空对象
        throw new InvalidOperationException(
            $"数据损坏：找不到角色 '{roleName}' 的战术评分配置！检查 AITacticalConfig.json 中的 TacticalPositioning.Scores 配置。");
    }

    /// <summary>
    /// 获取军团编制配置
    /// </summary>
    public static FormationConfig GetFormationConfig(int legionSize)
    {
        // ANTI-BAND-AID：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetFormationConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.LegionFormation?.Formations != null,
            "[GetFormationConfig] LegionFormation.Formations 配置缺失，检查 AITacticalConfig.json");

        if (legionSize >= 4 && Config.LegionFormation.Formations.TryGetValue("Large", out var large))
            return large;
        if (legionSize >= 2 && Config.LegionFormation.Formations.TryGetValue("Medium", out var medium))
            return medium;
        if (Config.LegionFormation.Formations.TryGetValue("Small", out var small))
            return small;

        // ANTI-BAND-AID：找不到编制配置时抛出异常
        throw new InvalidOperationException(
            $"数据损坏：找不到军团规模 {legionSize} 的编制配置！检查 AITacticalConfig.json 中的 LegionFormation.Formations 配置。");
    }

    /// <summary>
    /// 检查兵种是否需要 ZOC 压制
    /// </summary>
    public static bool RequiresZocSuppression(int troopKindID)
    {
        // ANTI-BAND-AID：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[RequiresZocSuppression] Config 为 null，检查配置加载逻辑");

        return Config.TacticalPositioning?.SpecialTroopKinds?.RequireZocSuppression?.Contains(troopKindID) ?? false;
    }

    /// <summary>
    /// 获取计略评分配置
    /// 日期：2026-03-09
    /// </summary>
    public static StratagemScoringConfig GetStratagemScoringConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null, 
            "[GetStratagemScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.StratagemScoring != null, 
            "[GetStratagemScoringConfig] StratagemScoring 配置缺失，检查 AITacticalConfig.json");

        return Config.StratagemScoring;
    }

    /// <summary>
    /// 获取友方增益计略评分配置
    /// 日期：2026-03-09
    /// </summary>
    public static FriendlyStratagemScoringConfig GetFriendlyStratagemScoringConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetFriendlyStratagemScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.FriendlyStratagemScoring != null,
            "[GetFriendlyStratagemScoringConfig] FriendlyStratagemScoring 配置缺失，检查 AITacticalConfig.json");

        return Config.FriendlyStratagemScoring;
    }

    /// <summary>
    /// 获取计略成功率预判配置
    /// 日期：2026-03-09
    /// </summary>
    public static StratagemSuccessRateConfig GetStratagemSuccessRateConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetStratagemSuccessRateConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.StratagemSuccessRate != null,
            "[GetStratagemSuccessRateConfig] StratagemSuccessRate 配置缺失，检查 AITacticalConfig.json");

        return Config.StratagemSuccessRate;
    }

    /// <summary>
    /// 获取战法评分配置
    /// 日期：2026-03-09
    /// </summary>
    public static CombatMethodScoringConfig GetCombatMethodScoringConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetCombatMethodScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.CombatMethodScoring != null,
            "[GetCombatMethodScoringConfig] CombatMethodScoring 配置缺失，检查 AITacticalConfig.json");

        return Config.CombatMethodScoring;
    }

    /// <summary>
    /// 获取战法成功率预判配置
    /// 日期：2026-03-09
    /// </summary>
    public static CombatMethodSuccessRateConfig GetCombatMethodSuccessRateConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetCombatMethodSuccessRateConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.CombatMethodSuccessRate != null,
            "[GetCombatMethodSuccessRateConfig] CombatMethodSuccessRate 配置缺失，检查 AITacticalConfig.json");

        return Config.CombatMethodSuccessRate;
    }

    /// <summary>
    /// 获取特技评分配置
    /// 日期：2026-03-10
    /// </summary>
    public static StuntScoringConfig GetStuntScoringConfig()
    {
        // ⚠️ 数据完整性断言：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetStuntScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.StuntScoring != null,
            "[GetStuntScoringConfig] StuntScoring 配置缺失，检查 AITacticalConfig.json");

        return Config.StuntScoring;
    }
}

// ==================== 配置数据结构 ====================

public class AITacticalConfig
{
    public TacticalPositioningConfig TacticalPositioning { get; set; }
    public StratagemScoringConfig StratagemScoring { get; set; }
    public FriendlyStratagemScoringConfig FriendlyStratagemScoring { get; set; }
    public StratagemSuccessRateConfig StratagemSuccessRate { get; set; }
    public CombatMethodScoringConfig CombatMethodScoring { get; set; }
    public CombatMethodSuccessRateConfig CombatMethodSuccessRate { get; set; }
    public StuntScoringConfig StuntScoring { get; set; }
    public LegionFormationConfig LegionFormation { get; set; }
}

public class TacticalPositioningConfig
{
    public string Description { get; set; }
    public int BoundingBoxExpansion { get; set; }
    public float DistancePenaltyWeight { get; set; }
    public PostureEvaluationConfig PostureEvaluation { get; set; }
    public Dictionary<string, int> ActionPriority { get; set; }
    public TargetSelectionConfig TargetSelection { get; set; }
    public Dictionary<string, Dictionary<string, float>> TerrainScores { get; set; }
    public Dictionary<string, StrategicPostureConfig> StrategicPosture { get; set; }
    public Dictionary<string, RoleScoreConfig> Scores { get; set; }
    public SpecialTroopKindsConfig SpecialTroopKinds { get; set; }
}

public class PostureEvaluationConfig
{
    public float PowerRatioOverwhelming { get; set; }
    public float PowerRatioDisadvantage { get; set; }
    public int CriticalFund { get; set; }
    public int CriticalFood { get; set; }
    public float CriticalHealthRatio { get; set; }
    public float CriticalPowerRatio { get; set; }
    public float SafeHealthRatio { get; set; }
    public string Description { get; set; }
}

public class TargetSelectionConfig
{
    public float ExecutionThresholdBonus { get; set; }
    public float StatusAbnormalBonus { get; set; }
    public float BlockedByTankBonus { get; set; }
    public float LowIntelligenceMultiplier { get; set; }
    public float AlreadyConfusedPenalty { get; set; }
    public float SupportAttackPenalty { get; set; }
    public string Description { get; set; }
}

public class StrategicPostureConfig
{
    public string Description { get; set; }
    public float DistancePenaltyMultiplier { get; set; }
    public Dictionary<string, float> ChokePointBonus { get; set; }
}

public class RoleScoreConfig
{
    public float ZocLockBonus { get; set; }
    public float ProtectLineBonus { get; set; }
    public float ClusterBonus { get; set; }
    public float MeleeRiskPenalty { get; set; }
    public float DangerZonePenalty { get; set; }
    public float TankProximityBonus { get; set; }
    public float AllyProximityBonus { get; set; }
    public int MinDistanceToEnemy { get; set; }
    public int OptimalDistanceToEnemy { get; set; }
    public int SafeDistanceMin { get; set; }
    public int OptimalRange { get; set; }
    public int AllyProximityRadius { get; set; }
}

public class SpecialTroopKindsConfig
{
    public List<int> RequireZocSuppression { get; set; }
    public Dictionary<string, string> RequireZocSuppressionNames { get; set; }
}

public class LegionFormationConfig
{
    public string Description { get; set; }
    public int MinTroopsForFullFormation { get; set; }
    public int MinTroopsForTank { get; set; }
    public Dictionary<string, FormationConfig> Formations { get; set; }
    public Dictionary<string, RoleThresholdConfig> RoleThresholds { get; set; }
}

public class FormationConfig
{
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public int TankSlots { get; set; }
    public int SupportSlots { get; set; }
    public string Description { get; set; }
}

public class RoleThresholdConfig
{
    public float MinScore { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// 计略评分配置
/// 日期：2026-03-09
/// </summary>
public class StratagemScoringConfig
{
    public string Description { get; set; }
    public float BaseScoreMultiplier { get; set; }
    public float ConservativeModifier { get; set; }
    public Dictionary<string, float> RoleModifiers { get; set; }
    public MoraleModifiersConfig MoraleModifiers { get; set; }
    public ExecutionModifierConfig ExecutionModifier { get; set; }
    public AOEScoringConfig AOEScoring { get; set; }
}

public class MoraleModifiersConfig
{
    public int HighMoraleThreshold { get; set; }
    public float HighMoraleBonus { get; set; }
    public int LowMoraleThreshold { get; set; }
    public float LowMoralePenalty { get; set; }
}

public class ExecutionModifierConfig
{
    public float ThresholdMultiplier { get; set; }
    public float Penalty { get; set; }
    public string Description { get; set; }
}

public class AOEScoringConfig
{
    public string Description { get; set; }
    public int DefaultEffectRadius { get; set; }
    public float SecondaryTargetMultiplier { get; set; }
    public float MageSecondaryTargetMultiplier { get; set; }
    public bool EnableAOEBonus { get; set; }
}

/// <summary>
/// 友方增益计略评分配置
/// 日期：2026-03-09
/// </summary>
public class FriendlyStratagemScoringConfig
{
    public string Description { get; set; }
    public float BaseScoreMultiplier { get; set; }
    public float ConservativeModifier { get; set; }
    public Dictionary<string, float> RoleModifiers { get; set; }
    public TargetNeedModifiersConfig TargetNeedModifiers { get; set; }
    public TargetValueModifiersConfig TargetValueModifiers { get; set; }
    public FriendlyAOEScoringConfig AOEScoring { get; set; }
}

public class TargetNeedModifiersConfig
{
    public string Description { get; set; }
    public int LowMoraleThreshold { get; set; }
    public float LowMoraleBonus { get; set; }
    public float CriticalHealthThreshold { get; set; }
    public float CriticalHealthBonus { get; set; }
}

public class TargetValueModifiersConfig
{
    public string Description { get; set; }
    public float DPSBonus { get; set; }
    public float TankBonus { get; set; }
    public float SupportBonus { get; set; }
    public float MageBonus { get; set; }
}

public class FriendlyAOEScoringConfig
{
    public float SecondaryTargetMultiplier { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// 计略成功率预判配置（智力对抗系统）
/// 日期：2026-03-09
/// </summary>
public class StratagemSuccessRateConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 基础成功率（智力相等时）
    /// </summary>
    public float BaseSuccessRate { get; set; }
    
    /// <summary>
    /// 智力影响系数（每点智力差的影响）
    /// </summary>
    public float IntelligenceInfluence { get; set; }
    
    /// <summary>
    /// 看破阈值（智力差低于此值时绝对失败）
    /// </summary>
    public int CounterThreshold { get; set; }
    
    /// <summary>
    /// 最低成功率（保底奇迹概率）
    /// </summary>
    public float MinSuccessRate { get; set; }
    
    /// <summary>
    /// 最高成功率（留下失误可能）
    /// </summary>
    public float MaxSuccessRate { get; set; }
    
    /// <summary>
    /// 是否启用成功率预判
    /// </summary>
    public bool EnableSuccessRatePrediction { get; set; }
}

/// <summary>
/// 战法评分配置
/// 日期：2026-03-09
/// </summary>
public class CombatMethodScoringConfig
{
    public string Description { get; set; }
    public float BaseScoreMultiplier { get; set; }
    public float ConservativeModifier { get; set; }
    public Dictionary<string, float> RoleModifiers { get; set; }
    public CombativityModifiersConfig CombativityModifiers { get; set; }
    public ExecutionModifierConfig ExecutionModifier { get; set; }
}

public class CombativityModifiersConfig
{
    public int HighCombativityThreshold { get; set; }
    public float HighCombativityBonus { get; set; }
    public int LowCombativityThreshold { get; set; }
    public float LowCombativityPenalty { get; set; }
}

/// <summary>
/// 战法成功率预判配置（统率对抗系统）
/// 日期：2026-03-09
/// </summary>
public class CombatMethodSuccessRateConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 基础成功率（统率相等时）
    /// </summary>
    public float BaseSuccessRate { get; set; }
    
    /// <summary>
    /// 统率影响系数（每点统率差的影响）
    /// </summary>
    public float CommandInfluence { get; set; }
    
    /// <summary>
    /// 看破阈值（统率差低于此值时绝对失败）
    /// </summary>
    public int CounterThreshold { get; set; }
    
    /// <summary>
    /// 最低成功率（保底奇迹概率）
    /// </summary>
    public float MinSuccessRate { get; set; }
    
    /// <summary>
    /// 最高成功率（留下失误可能）
    /// </summary>
    public float MaxSuccessRate { get; set; }
    
    /// <summary>
    /// 是否启用成功率预判
    /// </summary>
    public bool EnableSuccessRatePrediction { get; set; }
}

/// <summary>
/// 特技评分配置
/// 日期：2026-03-10
/// </summary>
public class StuntScoringConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 威力系数基础值
    /// </summary>
    public float PowerMultiplierBase { get; set; }
    
    /// <summary>
    /// 每个影响效果的威力加成
    /// </summary>
    public float PowerMultiplierPerInfluence { get; set; }
    
    /// <summary>
    /// 战意消耗惩罚系数
    /// </summary>
    public float PowerMultiplierCombativityPenalty { get; set; }
    
    /// <summary>
    /// 最小威力系数
    /// </summary>
    public float MinPowerMultiplier { get; set; }
    
    /// <summary>
    /// 战术角色修正系数
    /// </summary>
    public Dictionary<string, float> RoleModifiers { get; set; }
    
    /// <summary>
    /// 影响数量修正系数
    /// </summary>
    public float InfluenceCountModifier { get; set; }
    
    /// <summary>
    /// 斩杀修正配置
    /// </summary>
    public ExecutionModifierConfig ExecutionModifier { get; set; }
}
