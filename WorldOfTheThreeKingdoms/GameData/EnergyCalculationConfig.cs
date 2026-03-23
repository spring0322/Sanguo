#nullable disable

using System;
using System.Text.Json.Serialization;

namespace Zhsan.GameLogic.Config;

/// <summary>
/// 能量计算配置（部队与城池）
/// 日期：2026-03-16
/// 功能：功过分离机制 + 动态门槛
/// </summary>
public sealed class EnergyCalculationConfig
{
    [JsonPropertyName("Version")]
    public required string Version { get; set; }
    
    [JsonPropertyName("Description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("LastModified")]
    public required string LastModified { get; set; }
    
    [JsonPropertyName("TroopEnergy")]
    public required TroopEnergyConfig TroopEnergy { get; set; }
    
    [JsonPropertyName("ArchitectureEnergy")]
    public required ArchitectureEnergyConfig ArchitectureEnergy { get; set; }
    
    [JsonPropertyName("PerformanceSettings")]
    public required PerformanceSettings PerformanceSettings { get; set; }
}

/// <summary>
/// 部队能量计算配置
/// </summary>
public sealed class TroopEnergyConfig
{
    [JsonPropertyName("Description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("EnergyMultiplier")]
    public required float EnergyMultiplier { get; set; }
    
    [JsonPropertyName("EnergyRange")]
    public required EnergyRange EnergyRange { get; set; }
    
    [JsonPropertyName("BaseEnergy")]
    public required int BaseEnergy { get; set; }
    
    [JsonPropertyName("QuantityWeight")]
    public required float QuantityWeight { get; set; }
    
    [JsonPropertyName("MoraleWeight")]
    public required float MoraleWeight { get; set; }
    
    [JsonPropertyName("CombatWeights")]
    public required CombatWeights CombatWeights { get; set; }
    
    [JsonPropertyName("OfficerMeritSystem")]
    public required OfficerMeritSystem OfficerMeritSystem { get; set; }
}

/// <summary>
/// 城池能量计算配置
/// </summary>
public sealed class ArchitectureEnergyConfig
{
    [JsonPropertyName("Description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("EnergyRange")]
    public required EnergyRange EnergyRange { get; set; }
    
    [JsonPropertyName("BaseEnergyOffset")]
    public required int BaseEnergyOffset { get; set; }
    
    [JsonPropertyName("BaseStatsWeights")]
    public required BaseStatsWeights BaseStatsWeights { get; set; }
    
    [JsonPropertyName("PopulationWeight")]
    public required float PopulationWeight { get; set; }
    
    [JsonPropertyName("GarrisonWeights")]
    public required GarrisonWeights GarrisonWeights { get; set; }
    
    [JsonPropertyName("OfficerMeritSystem")]
    public required ArchitectureOfficerMeritSystem OfficerMeritSystem { get; set; }
    
    [JsonPropertyName("FinalFormulaMultiplier")]
    public required float FinalFormulaMultiplier { get; set; }
}

/// <summary>
/// 能量范围
/// </summary>
public sealed class EnergyRange
{
    [JsonPropertyName("Minimum")]
    public required int Minimum { get; set; }
    
    [JsonPropertyName("Maximum")]
    public required int Maximum { get; set; }
}

/// <summary>
/// 战斗属性权重
/// </summary>
public sealed class CombatWeights
{
    [JsonPropertyName("OffenceMultiplier")]
    public required float OffenceMultiplier { get; set; }
    
    [JsonPropertyName("DefenceMultiplier")]
    public required float DefenceMultiplier { get; set; }
    
    [JsonPropertyName("MovabilityMultiplier")]
    public required float MovabilityMultiplier { get; set; }
    
    [JsonPropertyName("TotalDivisor")]
    public required float TotalDivisor { get; set; }
    
    [JsonPropertyName("FinalDivisor")]
    public required float FinalDivisor { get; set; }
}

/// <summary>
/// 武将功过分离机制（部队版）
/// </summary>
public sealed class OfficerMeritSystem
{
    [JsonPropertyName("Description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("BloatPenaltyPerDeputy")]
    public required int BloatPenaltyPerDeputy { get; set; }
    
    [JsonPropertyName("BaselineRatio")]
    public required float BaselineRatio { get; set; }
    
    [JsonPropertyName("PositiveBonusMultiplier")]
    public required float PositiveBonusMultiplier { get; set; }
    
    [JsonPropertyName("NegativePenaltyMultiplier")]
    public required float NegativePenaltyMultiplier { get; set; }
    
    [JsonPropertyName("MinimumEffectiveValue")]
    public required int MinimumEffectiveValue { get; set; }
}

/// <summary>
/// 城池基础属性权重
/// </summary>
public sealed class BaseStatsWeights
{
    [JsonPropertyName("ScaleMultiplier")]
    public required int ScaleMultiplier { get; set; }
    
    [JsonPropertyName("DominationWeight")]
    public required float DominationWeight { get; set; }
    
    [JsonPropertyName("MoraleWeight")]
    public required float MoraleWeight { get; set; }
    
    [JsonPropertyName("AgricultureWeight")]
    public required float AgricultureWeight { get; set; }
    
    [JsonPropertyName("CommerceWeight")]
    public required float CommerceWeight { get; set; }
}

/// <summary>
/// 驻军权重
/// </summary>
public sealed class GarrisonWeights
{
    [JsonPropertyName("QuantityWeight")]
    public required float QuantityWeight { get; set; }
    
    [JsonPropertyName("MoraleWeight")]
    public required float MoraleWeight { get; set; }
}

/// <summary>
/// 武将功过分离机制（城池版）
/// </summary>
public sealed class ArchitectureOfficerMeritSystem
{
    [JsonPropertyName("Description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("BloatPenaltyPerOfficer")]
    public required int BloatPenaltyPerOfficer { get; set; }
    
    [JsonPropertyName("FiveStatsBaselineRatio")]
    public required float FiveStatsBaselineRatio { get; set; }
    
    [JsonPropertyName("PoliticsBaselineRatio")]
    public required float PoliticsBaselineRatio { get; set; }
    
    [JsonPropertyName("PositiveBonusMultiplier")]
    public required float PositiveBonusMultiplier { get; set; }
    
    [JsonPropertyName("NegativePenaltyMultiplier")]
    public required float NegativePenaltyMultiplier { get; set; }
    
    [JsonPropertyName("MinimumEffectiveFiveStats")]
    public required int MinimumEffectiveFiveStats { get; set; }
    
    [JsonPropertyName("MinimumEffectivePolitics")]
    public required int MinimumEffectivePolitics { get; set; }
    
    [JsonPropertyName("FiveStatsDivisor")]
    public required int FiveStatsDivisor { get; set; }
    
    [JsonPropertyName("PoliticsWeight")]
    public required float PoliticsWeight { get; set; }
}

/// <summary>
/// 性能设置
/// </summary>
public sealed class PerformanceSettings
{
    [JsonPropertyName("EnableCaching")]
    public required bool EnableCaching { get; set; }
    
    [JsonPropertyName("CacheInvalidationFrames")]
    public required int CacheInvalidationFrames { get; set; }
    
    [JsonPropertyName("EnableDebugLogging")]
    public required bool EnableDebugLogging { get; set; }
}
