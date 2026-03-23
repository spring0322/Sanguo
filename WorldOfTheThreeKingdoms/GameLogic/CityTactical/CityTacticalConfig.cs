#nullable enable

using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战术AI配置 - 效用理论参数
/// 日期：2026-03-11
/// </summary>
public class CityTacticalConfig
{
    public ResourceThresholds ResourceThresholds { get; set; } = new();
    public ActionCosts ActionCosts { get; set; } = new();
    public UtilityWeights UtilityWeights { get; set; } = new();
    public BaseScores BaseScores { get; set; } = new();
    public CampaignRequirements CampaignRequirements { get; set; } = new();
}

public class ResourceThresholds
{
    public int FoodSafetyLine { get; set; } = 100000;
    public int GoldReserve { get; set; } = 2000;
    public float TroopSaturationWarning { get; set; } = 0.8f;
    public int MoraleLowThreshold { get; set; } = 80;
}

public class ActionCosts
{
    public int DraftMinGold { get; set; } = 500;
    public int DraftMinFood { get; set; } = 2000;
    public float TrainMinTroopSaturation { get; set; } = 0.2f;
}

public class UtilityWeights
{
    public float AgricultureSharpness { get; set; } = 2.0f;
    public float DraftSharpness { get; set; } = 3.0f;
    public float TrainSharpness { get; set; } = 4.0f;
    public float ThreatMultiplier { get; set; } = 80.0f;
}

public class BaseScores
{
    public float Agriculture { get; set; } = 100.0f;
    public float Draft { get; set; } = 100.0f;
    public float Train { get; set; } = 80.0f;
    public float Campaign { get; set; } = 60.0f;
}

public class CampaignRequirements
{
    public float MinTroopSaturation { get; set; } = 0.8f;
    public int MinMorale { get; set; } = 80;
    public float MaxThreatLevel { get; set; } = 0.3f;
}
