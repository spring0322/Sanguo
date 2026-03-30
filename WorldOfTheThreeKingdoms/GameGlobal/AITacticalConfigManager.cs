using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// AI鎴樻湳閰嶇疆绠＄悊鍣?
/// 璐熻矗鍔犺浇鍜岀鐞嗘垬鏈畾浣嶃€佸啗鍥㈢紪鍒剁瓑閰嶇疆
/// 鏃ユ湡锛?026-03-10 閲嶆瀯锛氫娇鐢?ConfigManagerBase 缁熶竴鐑噸杞芥満鍒?
/// </summary>
public static class AITacticalConfigManager
{
    // 鍐呴儴绠＄悊鍣ㄥ疄渚嬶紙缁ф壙 ConfigManagerBase锛?
    private static readonly TacticalConfigManager _manager = new();

    /// <summary>
    /// 鑾峰彇閰嶇疆瀹炰緥锛堢嚎绋嬪畨鍏級
    /// </summary>
    public static AITacticalConfig Config => _manager.Config;
    
    /// <summary>
    /// 鍒濆鍖栭厤缃鐞嗗櫒锛堟父鎴忓惎鍔ㄦ椂璋冪敤锛?
    /// </summary>
    public static void Initialize() => _manager.Initialize();
    
    /// <summary>
    /// 鐑噸杞芥洿鏂帮紙涓荤嚎绋?Update 涓皟鐢級
    /// </summary>
    public static void Update() => _manager.Update();
    
    /// <summary>
    /// 鎵嬪姩閲嶆柊鍔犺浇閰嶇疆
    /// </summary>
    public static void ReloadConfig() => _manager.ReloadConfig();

    // ==================== 鍐呴儴绠＄悊鍣ㄧ被 ====================
    
    private class TacticalConfigManager : ConfigManagerBase<AITacticalConfig>
    {
        private AITacticalConfig _validatedConfig;

        protected override string ConfigFileName => "AITacticalConfig.json";
        
        protected override AITacticalConfig CreateDefaultConfig() => CreateDefaultTacticalConfig();

        public new void Update()
        {
            base.Update();
            ValidateLoadedConfig();
        }

        public new void ReloadConfig()
        {
            base.ReloadConfig();
            ValidateLoadedConfig();
        }

        private void ValidateLoadedConfig()
        {
            if (ReferenceEquals(_validatedConfig, Config))
            {
                return;
            }

            ValidateConfig(Config);
            _validatedConfig = Config;
        }

        /// <summary>
        /// 鍒濆鍖栭厤缃鐞嗗櫒锛堟父鎴忓惎鍔ㄦ椂璋冪敤锛?
        /// 閲嶅啓浠ユ坊鍔犻厤缃獙璇侊紙ANTI-BAND-AID锛欶ail Fast锛?
        /// </summary>
        public new void Initialize()
        {
            base.Initialize();
            
            // 馃敟 鍏抽敭锛氬垵濮嬪寲鍚庣珛鍗抽獙璇侀厤缃畬鏁存€?
            ValidateLoadedConfig();
        }

        /// <summary>
        /// 楠岃瘉閰嶇疆瀹屾暣鎬э紙ANTI-BAND-AID锛欶ail Fast锛?
        /// 馃 COLD PATH锛氬垵濮嬪寲闃舵锛屽彲璇绘€т紭鍏?
        /// </summary>
        private static void ValidateConfig(AITacticalConfig config)
        {
            // 馃敟 鍏抽敭锛氶厤缃繀椤诲畬鏁达紝缂哄け浠讳綍鍏抽敭瀛楁閮藉簲璇ユ姏鍑哄紓甯?
            if (config.TacticalPositioning == null)
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛欰ITacticalConfig.TacticalPositioning 涓?null锛屾鏌?AITacticalConfig.json");

            if (config.TacticalPositioning.TerrainScores == null)
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛歍acticalPositioning.TerrainScores 涓?null锛屾鏌?AITacticalConfig.json");

            if (config.TacticalPositioning.StrategicPosture == null)
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛歍acticalPositioning.StrategicPosture 涓?null锛屾鏌?AITacticalConfig.json");

            if (config.TacticalPositioning.Scores == null)
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛歍acticalPositioning.Scores 涓?null锛屾鏌?AITacticalConfig.json");

            if (config.TacticalPositioning.SmartSiegeHysteresis == null)
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛歍acticalPositioning.SmartSiegeHysteresis 涓?null锛屾鏌?AITacticalConfig.json");

            var smartSiegeHysteresis = config.TacticalPositioning.SmartSiegeHysteresis;
            if (smartSiegeHysteresis.CommittedDestinationScoreBonus < 0 ||
                smartSiegeHysteresis.ReassignThresholdPercent < 0 ||
                smartSiegeHysteresis.MinimumScoreGain < 0 ||
                smartSiegeHysteresis.NearDestinationDistance < 0 ||
                smartSiegeHysteresis.NearDestinationThresholdPercentBonus < 0 ||
                smartSiegeHysteresis.NearDestinationMinimumScoreGainBonus < 0 ||
                smartSiegeHysteresis.StuckRelaxPerStackPercent < 0 ||
                smartSiegeHysteresis.StuckRelaxPerStackScore < 0 ||
                smartSiegeHysteresis.MaxStuckRelaxStacks < 0)
            {
                throw new InvalidOperationException(
                    "閰嶇疆鎹熷潖锛歍acticalPositioning.SmartSiegeHysteresis 瀛樺湪璐熸暟閰嶇疆锛屾鏌?AITacticalConfig.json");
            }

            // 楠岃瘉鎴樼暐鎬佸娍閰嶇疆鐨勫畬鏁存€?
            string[] postures = ["Attack", "Defense", "Garrison"];
            foreach (var posture in postures)
            {
                if (!config.TacticalPositioning.StrategicPosture.ContainsKey(posture))
                    throw new InvalidOperationException(
                        $"閰嶇疆鎹熷潖锛歍acticalPositioning.StrategicPosture 缂哄皯 '{posture}' 閰嶇疆");

                var postureConfig = config.TacticalPositioning.StrategicPosture[posture];
                if (postureConfig.ChokePointBonus == null)
                    throw new InvalidOperationException(
                        $"閰嶇疆鎹熷潖锛歍acticalPositioning.StrategicPosture.{posture}.ChokePointBonus 涓?null");
            }

            if (config.FoodStrategy != null)
            {
                ValidateFoodStrategyConfig(config.FoodStrategy);
            }
        }
    }

    // ==================== 榛樿閰嶇疆鍒涘缓 ====================

    private static AITacticalConfig CreateDefaultTacticalConfig()
    {
        return new AITacticalConfig
        {
            TacticalPositioning = new TacticalPositioningConfig
            {
                BoundingBoxExpansion = 3,
                DistancePenaltyWeight = 0.1f,
                SmartSiegeHysteresis = new SmartSiegeHysteresisConfig
                {
                    Description = "SmartSiege hysteresis tuning",
                    CommittedDestinationScoreBonus = 180,
                    ReassignThresholdPercent = 20,
                    MinimumScoreGain = 30,
                    NearDestinationDistance = 2,
                    NearDestinationThresholdPercentBonus = 10,
                    NearDestinationMinimumScoreGainBonus = 15,
                    StuckRelaxPerStackPercent = 5,
                    StuckRelaxPerStackScore = 5,
                    MaxStuckRelaxStacks = 3
                },
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
                        ["妫灄"] = 150f,
                        ["灞卞湴"] = 200f,
                        ["宄诲箔"] = 180f,
                        ["姘村煙"] = -5000f
                    },
                    ["DPS"] = new()
                    {
                        ["骞冲師"] = 80f,
                        ["鑽夊師"] = 70f,
                        ["姘村煙"] = -5000f
                    },
                    ["Mage"] = new()
                    {
                        ["妫灄"] = 200f,
                        ["灞卞湴"] = 150f,
                        ["姘村煙"] = -5000f
                    },
                    ["Support"] = new()
                    {
                        ["妫灄"] = 200f,
                        ["灞卞湴"] = 150f,
                        ["姘村煙"] = -5000f
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
                    // 馃敟 C# 12 闆嗗悎琛ㄨ揪寮忥細灞炴€х被鍨?List<int> 宸叉槑纭紝鐩存帴浣跨敤 []
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
                CategoryModifiers = new()
                {
                    ["EnemyTargetedOffense"] = 1.0f,
                    ["SelfBuffOffense"] = 1.0f,
                    ["SelfBuffDefense"] = 0.75f,
                    ["FriendlySupport"] = 0.9f,
                    ["Utility"] = 0.5f,
                    ["Passive"] = 0.0f,
                    ["Disabled"] = 0.0f
                },
                RoleCategoryModifiers = new()
                {
                    ["Tank"] = new Dictionary<string, float>
                    {
                        ["SelfBuffDefense"] = 1.4f,
                        ["SelfBuffOffense"] = 1.1f
                    },
                    ["DPS"] = new Dictionary<string, float>
                    {
                        ["EnemyTargetedOffense"] = 1.2f,
                        ["SelfBuffOffense"] = 1.3f
                    },
                    ["Mage"] = new Dictionary<string, float>
                    {
                        ["EnemyTargetedOffense"] = 1.1f
                    },
                    ["Support"] = new Dictionary<string, float>
                    {
                        ["FriendlySupport"] = 1.3f,
                        ["SelfBuffDefense"] = 1.1f
                    }
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
            SkillScoring = new SkillScoringConfig
            {
                LevelMultiplier = 0.2f,
                RoleModifiers = new()
                {
                    ["Mage"] = 1.5f,
                    ["Support"] = 1.3f,
                    ["Tank"] = 1.0f,
                    ["DPS"] = 1.0f,
                    ["Logistics"] = 1.0f,
                    ["Balanced"] = 1.0f
                },
                CategoryModifiers = new()
                {
                    ["EnemyTargetedOffense"] = 1.0f,
                    ["SelfBuffOffense"] = 0.9f,
                    ["SelfBuffDefense"] = 0.7f,
                    ["FriendlySupport"] = 0.6f,
                    ["Utility"] = 0.2f,
                    ["Passive"] = 0.0f,
                    ["Disabled"] = 0.0f
                },
                RoleCategoryModifiers = new()
                {
                    ["DPS"] = new Dictionary<string, float>
                    {
                        ["EnemyTargetedOffense"] = 1.2f
                    },
                    ["Mage"] = new Dictionary<string, float>
                    {
                        ["EnemyTargetedOffense"] = 1.1f
                    }
                },
                InfluenceCountModifier = 0.1f,
                ExecutionModifier = new ExecutionModifierConfig
                {
                    ThresholdMultiplier = 1.0f,
                    Penalty = 0.5f
                }
            },
            StuntScoring = new StuntScoringConfig
            {
                PowerMultiplierBase = 1.0f,
                PowerMultiplierPerInfluence = 0.5f,
                PowerMultiplierCombativityPenalty = 0.01f,
                MinPowerMultiplier = 0.5f,
                RoleModifiers = new()
                {
                    ["Mage"] = 1.6f,
                    ["DPS"] = 1.4f,
                    ["Tank"] = 1.0f,
                    ["Support"] = 1.0f,
                    ["Logistics"] = 1.0f,
                    ["Balanced"] = 1.0f
                },
                CategoryModifiers = new()
                {
                    ["EnemyTargetedOffense"] = 1.0f,
                    ["SelfBuffOffense"] = 1.0f,
                    ["SelfBuffDefense"] = 0.8f,
                    ["FriendlySupport"] = 0.9f,
                    ["Utility"] = 0.6f,
                    ["Passive"] = 0.0f,
                    ["Disabled"] = 0.0f
                },
                RoleCategoryModifiers = new()
                {
                    ["Tank"] = new Dictionary<string, float>
                    {
                        ["SelfBuffDefense"] = 1.3f
                    },
                    ["DPS"] = new Dictionary<string, float>
                    {
                        ["EnemyTargetedOffense"] = 1.2f,
                        ["SelfBuffOffense"] = 1.2f
                    }
                },
                InfluenceCountModifier = 0.15f,
                ExecutionModifier = new ExecutionModifierConfig
                {
                    ThresholdMultiplier = 1.0f,
                    Penalty = 0.4f
                }
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
            },
            FoodStrategy = CreateDefaultFoodStrategyConfig()
        };
    }

    private static FoodStrategyConfig CreateDefaultFoodStrategyConfig()
    {
        return new FoodStrategyConfig
        {
            Description = "AI food strategy tuning",
            EmergencySignalDays = 5,
            EmergencyReserveDaysRear = 3,
            EmergencyReserveDaysFrontLine = 5,
            EmergencyReserveDaysHot = 7,
            EmergencyFundCeilingReserveRatio = 0.2f,
            EmergencyEnoughFundReserveRatio = 0.5f,
            EmergencySpendRatioNormal = 0.65f,
            EmergencySpendRatioHot = 0.8f,
            PreWarReserveDaysFrontLine = 10,
            PreWarReserveDaysHot = 14,
            PreWarReserveDaysActiveOffense = 20,
            PreWarReserveDaysPlannedOffense = 24,
            PreWarMaxFoodReserveRatio = 0.9f,
            PreWarFundCeilingReserveRatio = 0.5f,
            PreWarEnoughFundReserveRatio = 1.0f,
            PreWarSpendRatioNormal = 0.4f,
            PreWarSpendRatioOffense = 0.55f,
            OffensiveBudgetDistanceMultiplier = 1.5f,
            OffensiveBudgetMinDays = 6,
            OffensiveBudgetFallbackDays = 15,
            SortieReserveDays = 12,
            SortieStorageReserveRatio = 0.1f,
            DefensiveTroopFoodDays = 3,
            DefensiveStartCityFoodDays = 10,
            OffensiveTroopFoodDays = 5,
            RetreatOutOfFoodScoreBonus = 200f,
            RetreatSupplyCutOffScoreBonus = 90f
        };
    }

    private static void ValidateFoodStrategyConfig(FoodStrategyConfig config)
    {
        ValidatePositiveInt(config.EmergencySignalDays, "FoodStrategy.EmergencySignalDays");
        ValidatePositiveInt(config.EmergencyReserveDaysRear, "FoodStrategy.EmergencyReserveDaysRear");
        ValidatePositiveInt(config.EmergencyReserveDaysFrontLine, "FoodStrategy.EmergencyReserveDaysFrontLine");
        ValidatePositiveInt(config.EmergencyReserveDaysHot, "FoodStrategy.EmergencyReserveDaysHot");
        ValidatePositiveRatio(config.EmergencyFundCeilingReserveRatio, "FoodStrategy.EmergencyFundCeilingReserveRatio");
        ValidatePositiveRatio(config.EmergencyEnoughFundReserveRatio, "FoodStrategy.EmergencyEnoughFundReserveRatio");
        ValidatePositiveRatio(config.EmergencySpendRatioNormal, "FoodStrategy.EmergencySpendRatioNormal");
        ValidatePositiveRatio(config.EmergencySpendRatioHot, "FoodStrategy.EmergencySpendRatioHot");
        ValidatePositiveInt(config.PreWarReserveDaysFrontLine, "FoodStrategy.PreWarReserveDaysFrontLine");
        ValidatePositiveInt(config.PreWarReserveDaysHot, "FoodStrategy.PreWarReserveDaysHot");
        ValidatePositiveInt(config.PreWarReserveDaysActiveOffense, "FoodStrategy.PreWarReserveDaysActiveOffense");
        ValidatePositiveInt(config.PreWarReserveDaysPlannedOffense, "FoodStrategy.PreWarReserveDaysPlannedOffense");
        ValidatePositiveRatio(config.PreWarMaxFoodReserveRatio, "FoodStrategy.PreWarMaxFoodReserveRatio");
        ValidatePositiveRatio(config.PreWarFundCeilingReserveRatio, "FoodStrategy.PreWarFundCeilingReserveRatio");
        ValidatePositiveRatio(config.PreWarEnoughFundReserveRatio, "FoodStrategy.PreWarEnoughFundReserveRatio");
        ValidatePositiveRatio(config.PreWarSpendRatioNormal, "FoodStrategy.PreWarSpendRatioNormal");
        ValidatePositiveRatio(config.PreWarSpendRatioOffense, "FoodStrategy.PreWarSpendRatioOffense");
        ValidatePositiveFloat(config.OffensiveBudgetDistanceMultiplier, "FoodStrategy.OffensiveBudgetDistanceMultiplier");
        ValidatePositiveInt(config.OffensiveBudgetMinDays, "FoodStrategy.OffensiveBudgetMinDays");
        ValidatePositiveInt(config.OffensiveBudgetFallbackDays, "FoodStrategy.OffensiveBudgetFallbackDays");
        ValidatePositiveInt(config.SortieReserveDays, "FoodStrategy.SortieReserveDays");
        ValidatePositiveRatio(config.SortieStorageReserveRatio, "FoodStrategy.SortieStorageReserveRatio");
        ValidatePositiveInt(config.DefensiveTroopFoodDays, "FoodStrategy.DefensiveTroopFoodDays");
        ValidatePositiveInt(config.DefensiveStartCityFoodDays, "FoodStrategy.DefensiveStartCityFoodDays");
        ValidatePositiveInt(config.OffensiveTroopFoodDays, "FoodStrategy.OffensiveTroopFoodDays");
        ValidatePositiveFloat(config.RetreatOutOfFoodScoreBonus, "FoodStrategy.RetreatOutOfFoodScoreBonus");
        ValidatePositiveFloat(config.RetreatSupplyCutOffScoreBonus, "FoodStrategy.RetreatSupplyCutOffScoreBonus");
    }

    private static void ValidatePositiveInt(int value, string path)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"Data corrupted: {path} must be > 0. Check FoodStrategy in AITacticalConfig.json.");
        }
    }

    private static void ValidatePositiveFloat(float value, string path)
    {
        if (!(value > 0f) || float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new InvalidOperationException($"Data corrupted: {path} must be a finite value > 0. Check FoodStrategy in AITacticalConfig.json.");
        }
    }

    private static void ValidatePositiveRatio(float value, string path)
    {
        if (!(value > 0f && value <= 1f) || float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new InvalidOperationException($"Data corrupted: {path} must be in range (0, 1]. Check FoodStrategy in AITacticalConfig.json.");
        }
    }



    /// <summary>
    /// 鑾峰彇瑙掕壊鐨勬垬鏈瘎鍒嗛厤缃?
    /// </summary>
    public static RoleScoreConfig GetRoleScoreConfig(string roleName)
    {
        // ANTI-BAND-AID锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetRoleScoreConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.TacticalPositioning?.Scores != null,
            "[GetRoleScoreConfig] TacticalPositioning.Scores 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        if (Config.TacticalPositioning.Scores.TryGetValue(roleName, out var config))
        {
            return config;
        }

        // ANTI-BAND-AID锛氭壘涓嶅埌瑙掕壊閰嶇疆鏃舵姏鍑哄紓甯革紝鑰岄潪杩斿洖绌哄璞?
        throw new InvalidOperationException(
            $"Data corrupted: missing tactical score config for role '{roleName}'. Check TacticalPositioning.Scores in AITacticalConfig.json.");
    }

    /// <summary>
    /// 鑾峰彇鍐涘洟缂栧埗閰嶇疆
    /// </summary>
    public static FormationConfig GetFormationConfig(int legionSize)
    {
        // ANTI-BAND-AID锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetFormationConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.LegionFormation?.Formations != null,
            "[GetFormationConfig] LegionFormation.Formations 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        if (legionSize >= 4 && Config.LegionFormation.Formations.TryGetValue("Large", out var large))
            return large;
        if (legionSize >= 2 && Config.LegionFormation.Formations.TryGetValue("Medium", out var medium))
            return medium;
        if (Config.LegionFormation.Formations.TryGetValue("Small", out var small))
            return small;

        // ANTI-BAND-AID锛氭壘涓嶅埌缂栧埗閰嶇疆鏃舵姏鍑哄紓甯?
        throw new InvalidOperationException(
            $"Data corrupted: missing legion formation config for size {legionSize}. Check LegionFormation.Formations in AITacticalConfig.json.");
    }

    /// <summary>
    /// 妫€鏌ュ叺绉嶆槸鍚﹂渶瑕?ZOC 鍘嬪埗
    /// </summary>
    public static FoodStrategyConfig GetFoodStrategyConfig()
    {
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetFoodStrategyConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");

        if (Config.FoodStrategy == null)
        {
            Config.FoodStrategy = CreateDefaultFoodStrategyConfig();
        }

        return Config.FoodStrategy;
    }

    public static bool RequiresZocSuppression(int troopKindID)
    {
        // ANTI-BAND-AID锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[RequiresZocSuppression] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");

        return Config.TacticalPositioning?.SpecialTroopKinds?.RequireZocSuppression?.Contains(troopKindID) ?? false;
    }

    /// <summary>
    /// 鑾峰彇璁＄暐璇勫垎閰嶇疆
    /// 鏃ユ湡锛?026-03-09
    /// </summary>
    public static StratagemScoringConfig GetStratagemScoringConfig()
    {
        // 鈿狅笍 鏁版嵁瀹屾暣鎬ф柇瑷€锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null, 
            "[GetStratagemScoringConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.StratagemScoring != null, 
            "[GetStratagemScoringConfig] StratagemScoring 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        return Config.StratagemScoring;
    }

    /// <summary>
    /// 鑾峰彇鍙嬫柟澧炵泭璁＄暐璇勫垎閰嶇疆
    /// 鏃ユ湡锛?026-03-09
    /// </summary>
    public static FriendlyStratagemScoringConfig GetFriendlyStratagemScoringConfig()
    {
        // 鈿狅笍 鏁版嵁瀹屾暣鎬ф柇瑷€锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetFriendlyStratagemScoringConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.FriendlyStratagemScoring != null,
            "[GetFriendlyStratagemScoringConfig] FriendlyStratagemScoring 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        return Config.FriendlyStratagemScoring;
    }

    /// <summary>
    /// 鑾峰彇璁＄暐鎴愬姛鐜囬鍒ら厤缃?
    /// 鏃ユ湡锛?026-03-09
    /// </summary>
    public static StratagemSuccessRateConfig GetStratagemSuccessRateConfig()
    {
        // 鈿狅笍 鏁版嵁瀹屾暣鎬ф柇瑷€锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetStratagemSuccessRateConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.StratagemSuccessRate != null,
            "[GetStratagemSuccessRateConfig] StratagemSuccessRate 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        return Config.StratagemSuccessRate;
    }

    /// <summary>
    /// 鑾峰彇鎴樻硶璇勫垎閰嶇疆
    /// 鏃ユ湡锛?026-03-09
    /// </summary>
    public static CombatMethodScoringConfig GetCombatMethodScoringConfig()
    {
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetCombatMethodScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.CombatMethodScoring != null,
            "[GetCombatMethodScoringConfig] CombatMethodScoring 配置缺失，检查 AITacticalConfig.json");

        var scoring = Config.CombatMethodScoring;
        if (scoring.CategoryModifiers == null)
        {
            scoring.CategoryModifiers = new Dictionary<string, float>
            {
                ["EnemyTargetedOffense"] = 1.0f,
                ["SelfBuffOffense"] = 1.0f,
                ["SelfBuffDefense"] = 0.75f,
                ["FriendlySupport"] = 0.9f,
                ["Utility"] = 0.5f,
                ["Passive"] = 0.0f,
                ["Disabled"] = 0.0f
            };
        }

        if (scoring.RoleCategoryModifiers == null)
        {
            scoring.RoleCategoryModifiers = new Dictionary<string, Dictionary<string, float>>
            {
                ["Tank"] = new Dictionary<string, float>
                {
                    ["SelfBuffDefense"] = 1.4f,
                    ["SelfBuffOffense"] = 1.1f
                },
                ["DPS"] = new Dictionary<string, float>
                {
                    ["EnemyTargetedOffense"] = 1.2f,
                    ["SelfBuffOffense"] = 1.3f
                },
                ["Mage"] = new Dictionary<string, float>
                {
                    ["EnemyTargetedOffense"] = 1.1f
                },
                ["Support"] = new Dictionary<string, float>
                {
                    ["FriendlySupport"] = 1.3f,
                    ["SelfBuffDefense"] = 1.1f
                }
            };
        }

        return scoring;
    }

    /// <summary>
    /// 鑾峰彇鎴樻硶鎴愬姛鐜囬鍒ら厤缃?
    /// 鏃ユ湡锛?026-03-09
    /// </summary>
    public static SkillScoringConfig GetSkillScoringConfig()
    {
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetSkillScoringConfig] Config 为 null，检查配置加载逻辑");

        if (Config.SkillScoring == null)
        {
            Config.SkillScoring = new SkillScoringConfig
            {
                LevelMultiplier = 0.2f,
                RoleModifiers = new Dictionary<string, float>(),
                CategoryModifiers = new Dictionary<string, float>(),
                RoleCategoryModifiers = new Dictionary<string, Dictionary<string, float>>(),
                InfluenceCountModifier = 0.1f,
                ExecutionModifier = new ExecutionModifierConfig
                {
                    ThresholdMultiplier = 1.0f,
                    Penalty = 0.5f
                }
            };
        }

        var scoring = Config.SkillScoring;
        if (scoring.RoleModifiers == null)
        {
            scoring.RoleModifiers = new Dictionary<string, float>
            {
                ["Mage"] = 1.5f,
                ["Support"] = 1.3f,
                ["Tank"] = 1.0f,
                ["DPS"] = 1.0f,
                ["Logistics"] = 1.0f,
                ["Balanced"] = 1.0f
            };
        }

        if (scoring.CategoryModifiers == null)
        {
            scoring.CategoryModifiers = new Dictionary<string, float>
            {
                ["EnemyTargetedOffense"] = 1.0f,
                ["SelfBuffOffense"] = 0.9f,
                ["SelfBuffDefense"] = 0.7f,
                ["FriendlySupport"] = 0.6f,
                ["Utility"] = 0.2f,
                ["Passive"] = 0.0f,
                ["Disabled"] = 0.0f
            };
        }

        if (scoring.RoleCategoryModifiers == null)
        {
            scoring.RoleCategoryModifiers = new Dictionary<string, Dictionary<string, float>>
            {
                ["DPS"] = new Dictionary<string, float>
                {
                    ["EnemyTargetedOffense"] = 1.2f
                },
                ["Mage"] = new Dictionary<string, float>
                {
                    ["EnemyTargetedOffense"] = 1.1f
                }
            };
        }

        if (scoring.ExecutionModifier == null)
        {
            scoring.ExecutionModifier = new ExecutionModifierConfig
            {
                ThresholdMultiplier = 1.0f,
                Penalty = 0.5f
            };
        }

        return scoring;
    }

    public static CombatMethodSuccessRateConfig GetCombatMethodSuccessRateConfig()
    {
        // 鈿狅笍 鏁版嵁瀹屾暣鎬ф柇瑷€锛氶厤缃簲璇ュ湪鍒濆鍖栨椂鍔犺浇
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetCombatMethodSuccessRateConfig] Config 涓?null锛屾鏌ラ厤缃姞杞介€昏緫");
        System.Diagnostics.Debug.Assert(Config.CombatMethodSuccessRate != null,
            "[GetCombatMethodSuccessRateConfig] CombatMethodSuccessRate 閰嶇疆缂哄け锛屾鏌?AITacticalConfig.json");

        return Config.CombatMethodSuccessRate;
    }

    /// <summary>
    /// 鑾峰彇鐗规妧璇勫垎閰嶇疆
    /// 鏃ユ湡锛?026-03-10
    /// </summary>
    public static StuntScoringConfig GetStuntScoringConfig()
    {
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetStuntScoringConfig] Config 为 null，检查配置加载逻辑");
        System.Diagnostics.Debug.Assert(Config.StuntScoring != null,
            "[GetStuntScoringConfig] StuntScoring 配置缺失，检查 AITacticalConfig.json");

        var scoring = Config.StuntScoring;
        if (scoring.CategoryModifiers == null)
        {
            scoring.CategoryModifiers = new Dictionary<string, float>
            {
                ["EnemyTargetedOffense"] = 1.0f,
                ["SelfBuffOffense"] = 1.0f,
                ["SelfBuffDefense"] = 0.8f,
                ["FriendlySupport"] = 0.9f,
                ["Utility"] = 0.6f,
                ["Passive"] = 0.0f,
                ["Disabled"] = 0.0f
            };
        }

        if (scoring.RoleCategoryModifiers == null)
        {
            scoring.RoleCategoryModifiers = new Dictionary<string, Dictionary<string, float>>
            {
                ["Tank"] = new Dictionary<string, float>
                {
                    ["SelfBuffDefense"] = 1.3f
                },
                ["DPS"] = new Dictionary<string, float>
                {
                    ["EnemyTargetedOffense"] = 1.2f,
                    ["SelfBuffOffense"] = 1.2f
                }
            };
        }

        return scoring;
    }
}

// ==================== 閰嶇疆鏁版嵁缁撴瀯 ====================

public class AITacticalConfig
{
    public TacticalPositioningConfig TacticalPositioning { get; set; }
    public StratagemScoringConfig StratagemScoring { get; set; }
    public FriendlyStratagemScoringConfig FriendlyStratagemScoring { get; set; }
    public StratagemSuccessRateConfig StratagemSuccessRate { get; set; }
    public CombatMethodScoringConfig CombatMethodScoring { get; set; }
    public CombatMethodSuccessRateConfig CombatMethodSuccessRate { get; set; }
    public SkillScoringConfig SkillScoring { get; set; }
    public StuntScoringConfig StuntScoring { get; set; }
    public LegionFormationConfig LegionFormation { get; set; }
    public FoodStrategyConfig FoodStrategy { get; set; }
}

public class TacticalPositioningConfig
{
    public string Description { get; set; }
    public int BoundingBoxExpansion { get; set; }
    public float DistancePenaltyWeight { get; set; }
    public SmartSiegeHysteresisConfig SmartSiegeHysteresis { get; set; }
    public PostureEvaluationConfig PostureEvaluation { get; set; }
    public Dictionary<string, int> ActionPriority { get; set; }
    public TargetSelectionConfig TargetSelection { get; set; }
    public Dictionary<string, Dictionary<string, float>> TerrainScores { get; set; }
    public Dictionary<string, StrategicPostureConfig> StrategicPosture { get; set; }
    public Dictionary<string, RoleScoreConfig> Scores { get; set; }
    public SpecialTroopKindsConfig SpecialTroopKinds { get; set; }
}

public class SmartSiegeHysteresisConfig
{
    public string Description { get; set; }
    public int CommittedDestinationScoreBonus { get; set; }
    public int ReassignThresholdPercent { get; set; }
    public int MinimumScoreGain { get; set; }
    public int NearDestinationDistance { get; set; }
    public int NearDestinationThresholdPercentBonus { get; set; }
    public int NearDestinationMinimumScoreGainBonus { get; set; }
    public int StuckRelaxPerStackPercent { get; set; }
    public int StuckRelaxPerStackScore { get; set; }
    public int MaxStuckRelaxStacks { get; set; }
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

public class FoodStrategyConfig
{
    public string Description { get; set; }
    public int EmergencySignalDays { get; set; }
    public int EmergencyReserveDaysRear { get; set; }
    public int EmergencyReserveDaysFrontLine { get; set; }
    public int EmergencyReserveDaysHot { get; set; }
    public float EmergencyFundCeilingReserveRatio { get; set; }
    public float EmergencyEnoughFundReserveRatio { get; set; }
    public float EmergencySpendRatioNormal { get; set; }
    public float EmergencySpendRatioHot { get; set; }
    public int PreWarReserveDaysFrontLine { get; set; }
    public int PreWarReserveDaysHot { get; set; }
    public int PreWarReserveDaysActiveOffense { get; set; }
    public int PreWarReserveDaysPlannedOffense { get; set; }
    public float PreWarMaxFoodReserveRatio { get; set; }
    public float PreWarFundCeilingReserveRatio { get; set; }
    public float PreWarEnoughFundReserveRatio { get; set; }
    public float PreWarSpendRatioNormal { get; set; }
    public float PreWarSpendRatioOffense { get; set; }
    public float OffensiveBudgetDistanceMultiplier { get; set; }
    public int OffensiveBudgetMinDays { get; set; }
    public int OffensiveBudgetFallbackDays { get; set; }
    public int SortieReserveDays { get; set; }
    public float SortieStorageReserveRatio { get; set; }
    public int DefensiveTroopFoodDays { get; set; }
    public int DefensiveStartCityFoodDays { get; set; }
    public int OffensiveTroopFoodDays { get; set; }
    public float RetreatOutOfFoodScoreBonus { get; set; }
    public float RetreatSupplyCutOffScoreBonus { get; set; }
}

/// <summary>
/// 璁＄暐璇勫垎閰嶇疆
/// 鏃ユ湡锛?026-03-09
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
/// 鍙嬫柟澧炵泭璁＄暐璇勫垎閰嶇疆
/// 鏃ユ湡锛?026-03-09
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
/// 璁＄暐鎴愬姛鐜囬鍒ら厤缃紙鏅哄姏瀵规姉绯荤粺锛?
/// 鏃ユ湡锛?026-03-09
/// </summary>
public class StratagemSuccessRateConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 鍩虹鎴愬姛鐜囷紙鏅哄姏鐩哥瓑鏃讹級
    /// </summary>
    public float BaseSuccessRate { get; set; }
    
    /// <summary>
    /// 鏅哄姏褰卞搷绯绘暟锛堟瘡鐐规櫤鍔涘樊鐨勫奖鍝嶏級
    /// </summary>
    public float IntelligenceInfluence { get; set; }
    
    /// <summary>
    /// 鐪嬬牬闃堝€硷紙鏅哄姏宸綆浜庢鍊兼椂缁濆澶辫触锛?
    /// </summary>
    public int CounterThreshold { get; set; }
    
    /// <summary>
    /// 鏈€浣庢垚鍔熺巼锛堜繚搴曞杩规鐜囷級
    /// </summary>
    public float MinSuccessRate { get; set; }
    
    /// <summary>
    /// 鏈€楂樻垚鍔熺巼锛堢暀涓嬪け璇彲鑳斤級
    /// </summary>
    public float MaxSuccessRate { get; set; }
    
    /// <summary>
    /// 鏄惁鍚敤鎴愬姛鐜囬鍒?
    /// </summary>
    public bool EnableSuccessRatePrediction { get; set; }
}

/// <summary>
/// 鎴樻硶璇勫垎閰嶇疆
/// 鏃ユ湡锛?026-03-09
/// </summary>
public class CombatMethodScoringConfig
{
    public string Description { get; set; }
    public float BaseScoreMultiplier { get; set; }
    public float ConservativeModifier { get; set; }
    public Dictionary<string, float> RoleModifiers { get; set; }
    public Dictionary<string, float> CategoryModifiers { get; set; }
    public Dictionary<string, Dictionary<string, float>> RoleCategoryModifiers { get; set; }
    public CombativityModifiersConfig CombativityModifiers { get; set; }
    public ExecutionModifierConfig ExecutionModifier { get; set; }
}

public class SkillScoringConfig
{
    public string Description { get; set; }
    public float LevelMultiplier { get; set; }
    public Dictionary<string, float> RoleModifiers { get; set; }
    public Dictionary<string, float> CategoryModifiers { get; set; }
    public Dictionary<string, Dictionary<string, float>> RoleCategoryModifiers { get; set; }
    public float InfluenceCountModifier { get; set; }
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
/// 鎴樻硶鎴愬姛鐜囬鍒ら厤缃紙缁熺巼瀵规姉绯荤粺锛?
/// 鏃ユ湡锛?026-03-09
/// </summary>
public class CombatMethodSuccessRateConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 鍩虹鎴愬姛鐜囷紙缁熺巼鐩哥瓑鏃讹級
    /// </summary>
    public float BaseSuccessRate { get; set; }
    
    /// <summary>
    /// 缁熺巼褰卞搷绯绘暟锛堟瘡鐐圭粺鐜囧樊鐨勫奖鍝嶏級
    /// </summary>
    public float CommandInfluence { get; set; }
    
    /// <summary>
    /// 鐪嬬牬闃堝€硷紙缁熺巼宸綆浜庢鍊兼椂缁濆澶辫触锛?
    /// </summary>
    public int CounterThreshold { get; set; }
    
    /// <summary>
    /// 鏈€浣庢垚鍔熺巼锛堜繚搴曞杩规鐜囷級
    /// </summary>
    public float MinSuccessRate { get; set; }
    
    /// <summary>
    /// 鏈€楂樻垚鍔熺巼锛堢暀涓嬪け璇彲鑳斤級
    /// </summary>
    public float MaxSuccessRate { get; set; }
    
    /// <summary>
    /// 鏄惁鍚敤鎴愬姛鐜囬鍒?
    /// </summary>
    public bool EnableSuccessRatePrediction { get; set; }
}

/// <summary>
/// 鐗规妧璇勫垎閰嶇疆
/// 鏃ユ湡锛?026-03-10
/// </summary>
public class StuntScoringConfig
{
    public string Description { get; set; }
    
    /// <summary>
    /// 濞佸姏绯绘暟鍩虹鍊?
    /// </summary>
    public float PowerMultiplierBase { get; set; }
    
    /// <summary>
    /// 姣忎釜褰卞搷鏁堟灉鐨勫▉鍔涘姞鎴?
    /// </summary>
    public float PowerMultiplierPerInfluence { get; set; }
    
    /// <summary>
    /// 鎴樻剰娑堣€楁儵缃氱郴鏁?
    /// </summary>
    public float PowerMultiplierCombativityPenalty { get; set; }
    
    /// <summary>
    /// 鏈€灏忓▉鍔涚郴鏁?
    /// </summary>
    public float MinPowerMultiplier { get; set; }
    
    /// <summary>
    /// 鎴樻湳瑙掕壊淇绯绘暟
    /// </summary>
    public Dictionary<string, float> RoleModifiers { get; set; }

    public Dictionary<string, float> CategoryModifiers { get; set; }
    public Dictionary<string, Dictionary<string, float>> RoleCategoryModifiers { get; set; }
    
    /// <summary>
    /// 褰卞搷鏁伴噺淇绯绘暟
    /// </summary>
    public float InfluenceCountModifier { get; set; }
    
    /// <summary>
    /// 鏂╂潃淇閰嶇疆
    /// </summary>
    public ExecutionModifierConfig ExecutionModifier { get; set; }
}


