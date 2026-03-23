using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// AI决策配置管理器
/// 负责加载和管理混合AI决策系统的配置
/// 日期：2026-03-10 重构：使用 ConfigManagerBase 统一热重载机制
/// </summary>
public static class AIDecisionConfigManager
{
    // 内部管理器实例（继承 ConfigManagerBase）
    private static readonly DecisionConfigManager _manager = new();

    /// <summary>
    /// 获取配置实例（线程安全）
    /// </summary>
    public static AIDecisionConfig Config => _manager.Config;
    
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
    
    private class DecisionConfigManager : ConfigManagerBase<AIDecisionConfig>
    {
        protected override string ConfigFileName => "AIDecisionConfig.json";
        
        protected override AIDecisionConfig CreateDefaultConfig() => CreateDefaultDecisionConfig();
    }

    // ==================== 默认配置创建 ====================

    private static AIDecisionConfig CreateDefaultDecisionConfig()
    {
        return new AIDecisionConfig
        {
            DecisionMode = "Hybrid",
            KeyTroopCriteria = new KeyTroopCriteriaConfig
            {
                StatThresholds = new StatThresholdsConfig
                {
                    Command = 80,
                    Intelligence = 80,
                    Strength = 80
                },
                TitleLevelThreshold = new TitleLevelThresholdConfig
                {
                    MinLevel = 6
                },
                QuantityThreshold = new QuantityThresholdConfig
                {
                    MinQuantity = 5000
                },
                EliteTroopKinds = new EliteTroopKindsConfig
                {
                    TroopKindIDs = [400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415],
                    TroopKindNames = new Dictionary<string, string>
                    {
                        ["400"] = "虎豹骑",
                        ["401"] = "白耳兵",
                        ["402"] = "陷阵营",
                        ["403"] = "先登死士",
                        ["404"] = "西凉铁骑",
                        ["405"] = "无当飞军",
                        ["406"] = "白马义从",
                        ["407"] = "丹阳兵",
                        ["408"] = "青州兵",
                        ["409"] = "锦帆军",
                        ["410"] = "虎卫军",
                        ["411"] = "解烦兵",
                        ["412"] = "飞熊军",
                        ["413"] = "背嵬军",
                        ["414"] = "玄甲军",
                        ["415"] = "虎贲军"
                    },
                    AutoDetect = new AutoDetectConfig
                    {
                        Enabled = true,
                        MaxScaleThreshold = 3000,
                        ExcludeTypes = ["骑兵"]
                    }
                },
                DifficultyBased = new DifficultyBasedConfig
                {
                    EnableForEnemyInHard = true,
                    EnableForEnemyInVeryHard = true,
                    MinDifficultyLevel = 3
                },
                PlayerFaction = new PlayerFactionConfig
                {
                    AlwaysUseUtilityAI = false,
                    UseUtilityAIForImportantOnly = true
                }
            },
            PerformanceLimits = new PerformanceLimitsConfig
            {
                MaxUtilityAITroopsPerTurn = 20,
                UtilityAITimeoutMs = 100,
                FallbackToTraditionalOnTimeout = true
            }
        };
    }



    /// <summary>
    /// 检查兵种ID是否为精锐兵种
    /// </summary>
    public static bool IsEliteTroopKind(int troopKindID)
    {
        // ANTI-BAND-AID：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[IsEliteTroopKind] Config 为 null，检查配置加载逻辑");

        return Config.KeyTroopCriteria?.EliteTroopKinds?.TroopKindIDs?.Contains(troopKindID) ?? false;
    }

    /// <summary>
    /// 获取精锐兵种名称
    /// </summary>
    public static string GetEliteTroopKindName(int troopKindID)
    {
        // ANTI-BAND-AID：配置应该在初始化时加载
        System.Diagnostics.Debug.Assert(Config != null,
            "[GetEliteTroopKindName] Config 为 null，检查配置加载逻辑");

        if (Config.KeyTroopCriteria?.EliteTroopKinds?.TroopKindNames != null &&
            Config.KeyTroopCriteria.EliteTroopKinds.TroopKindNames.TryGetValue(troopKindID.ToString(), out string name))
        {
            return name;
        }
        return "未知精锐兵种";
    }
}

// ==================== 配置数据结构 ====================

public class AIDecisionConfig
{
    public string DecisionMode { get; set; }
    public KeyTroopCriteriaConfig KeyTroopCriteria { get; set; }
    public PerformanceLimitsConfig PerformanceLimits { get; set; }
    public UtilityAIWeightsConfig UtilityAIWeights { get; set; }
    public TraditionalAISettingsConfig TraditionalAISettings { get; set; }
    public DebugSettingsConfig DebugSettings { get; set; }
}

public class KeyTroopCriteriaConfig
{
    public string Description { get; set; }
    public StatThresholdsConfig StatThresholds { get; set; }
    public TitleLevelThresholdConfig TitleLevelThreshold { get; set; }
    public QuantityThresholdConfig QuantityThreshold { get; set; }
    public EliteTroopKindsConfig EliteTroopKinds { get; set; }
    public DifficultyBasedConfig DifficultyBased { get; set; }
    public PlayerFactionConfig PlayerFaction { get; set; }
}

public class StatThresholdsConfig
{
    public string Description { get; set; }
    public int Command { get; set; }
    public int Intelligence { get; set; }
    public int Strength { get; set; }
}

public class TitleLevelThresholdConfig
{
    public string Description { get; set; }
    public int MinLevel { get; set; }
}

public class QuantityThresholdConfig
{
    public string Description { get; set; }
    public int MinQuantity { get; set; }
}

public class EliteTroopKindsConfig
{
    public string Description { get; set; }
    public List<int> TroopKindIDs { get; set; }
    public Dictionary<string, string> TroopKindNames { get; set; }
    public AutoDetectConfig AutoDetect { get; set; }
}

public class AutoDetectConfig
{
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public int MaxScaleThreshold { get; set; }
    public List<string> ExcludeTypes { get; set; }
}

public class DifficultyBasedConfig
{
    public string Description { get; set; }
    public bool EnableForEnemyInHard { get; set; }
    public bool EnableForEnemyInVeryHard { get; set; }
    public int MinDifficultyLevel { get; set; }
}

public class PlayerFactionConfig
{
    public string Description { get; set; }
    public bool AlwaysUseUtilityAI { get; set; }
    public bool UseUtilityAIForImportantOnly { get; set; }
}

public class PerformanceLimitsConfig
{
    public string Description { get; set; }
    public int MaxUtilityAITroopsPerTurn { get; set; }
    public int UtilityAITimeoutMs { get; set; }
    public bool FallbackToTraditionalOnTimeout { get; set; }
}

public class UtilityAIWeightsConfig
{
    public string Description { get; set; }
    public float PositionScore { get; set; }
    public float TargetPriority { get; set; }
    public Dictionary<string, float> ActionTypeBonus { get; set; }
}

public class TraditionalAISettingsConfig
{
    public string Description { get; set; }
    public bool UseRoleBasedDecision { get; set; }
    public bool SimplifiedTargetSelection { get; set; }
    public bool FastPathfinding { get; set; }
}

public class DebugSettingsConfig
{
    public bool EnableLogging { get; set; }
    public bool LogKeyTroopDecisions { get; set; }
    public bool LogPerformanceMetrics { get; set; }
    public bool ShowDecisionReason { get; set; }
}
