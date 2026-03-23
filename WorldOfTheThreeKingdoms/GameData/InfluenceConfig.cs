#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameData;

/// <summary>
/// 势力范围系统配置（可通过 JSON 文件加载）
/// 日期：2026-03-11
/// 更新：2026-03-21 改用 InfluenceConfigManager 支持热重载
/// </summary>
public class InfluenceConfig
{
    /// <summary>
    /// 当前配置实例（通过 InfluenceConfigManager 访问）
    /// </summary>
    public static InfluenceConfig Current => InfluenceConfigManager.Current;
    
    // ===== 能量公式参数 =====
    
    /// <summary>基础能量偏移量</summary>
    [JsonPropertyName("BaseEnergyOffset")]
    public double BaseEnergyOffset { get; set; }
    
    /// <summary>规模倍率（每级规模提供的能量）</summary>
    [JsonPropertyName("ScaleMultiplier")]
    public double ScaleMultiplier { get; set; }
    
    /// <summary>统治倍率</summary>
    [JsonPropertyName("DominationMultiplier")]
    public double DominationMultiplier { get; set; }
    
    /// <summary>民心倍率</summary>
    [JsonPropertyName("MoraleMultiplier")]
    public double MoraleMultiplier { get; set; }
    
    /// <summary>资源倍率（农业+商业+耐久）</summary>
    [JsonPropertyName("ResourceMultiplier")]
    public double ResourceMultiplier { get; set; }
    
    /// <summary>最小能量值（保底）</summary>
    [JsonPropertyName("MinimumEnergy")]
    public int MinimumEnergy { get; set; }
    
    // ===== 地形阻力参数 =====
    
    /// <summary>是否使用默认地形阻力</summary>
    [JsonPropertyName("UseDefaultCosts")]
    public bool UseDefaultTerrainCosts { get; set; }
    
    /// <summary>地形阻力覆盖表（TerrainID -> Cost）</summary>
    [JsonPropertyName("Overrides")]
    public Dictionary<int, int> TerrainCostOverrides { get; set; } = [];
    
    /// <summary>🆕 城池能量传播倍率配置（日期：2026-03-20）</summary>
    [JsonPropertyName("CityEnergySpreadConfig")]
    public CityEnergySpreadConfig CityEnergySpreadConfig { get; set; }
    
    /// <summary>🆕 部队能量传播配置（日期：2026-03-21）</summary>
    [JsonPropertyName("TroopEnergySpreadConfig")]
    public TroopEnergySpreadConfig TroopEnergySpreadConfig { get; set; }
    
    /// <summary>🆕 能量衰减配置（日期：2026-03-21）</summary>
    [JsonPropertyName("EnergyDecayConfig")]
    public EnergyDecayConfig EnergyDecayConfig { get; set; }
    
    /// <summary>🆕 能量叠加配置（日期：2026-03-21）</summary>
    [JsonPropertyName("EnergyStackingConfig")]
    public EnergyStackingConfig EnergyStackingConfig { get; set; }
    
    /// <summary>🆕 残留能量配置（日期：2026-03-21）</summary>
    [JsonPropertyName("ResidualEnergyConfig")]
    public ResidualEnergyConfig ResidualEnergyConfig { get; set; }
    
    // ===== 天气影响参数 =====
    
    /// <summary>是否启用天气影响</summary>
    [JsonPropertyName("EnableWeatherEffects")]
    public bool EnableWeatherEffects { get; set; }
    
    /// <summary>天气效果配置表（天气名称 -> 效果）</summary>
    [JsonPropertyName("WeatherEffects")]
    public Dictionary<string, WeatherEffect> WeatherEffects { get; set; }
    
    // ===== 更新设置 =====
    
    /// <summary>是否启用自动更新</summary>
    [JsonPropertyName("EnableAutoUpdate")]
    public bool EnableAutoUpdate { get; set; }
    
    /// <summary>更新间隔（帧数）</summary>
    [JsonPropertyName("UpdateIntervalFrames")]
    public int UpdateIntervalFrames { get; set; }
    
    /// <summary>每批更新的建筑数量</summary>
    [JsonPropertyName("BatchSize")]
    public int BatchSize { get; set; }
    
    // ===== 渲染设置 =====
    
    /// <summary>是否启用渲染</summary>
    [JsonPropertyName("EnableRendering")]
    public bool EnableRendering { get; set; }
    
    /// <summary>核心区域透明度</summary>
    [JsonPropertyName("CoreAlpha")]
    public float CoreAlpha { get; set; }
    
    /// <summary>边缘区域透明度</summary>
    [JsonPropertyName("EdgeAlpha")]
    public float EdgeAlpha { get; set; }
    
    /// <summary>核心区域能量阈值（部队）</summary>
    [JsonPropertyName("EnergyThresholdForCore")]
    public int EnergyThresholdForCore { get; set; }
    
    /// <summary>核心区域能量阈值（城池）</summary>
    [JsonPropertyName("ArchitectureEnergyThresholdForCore")]
    public int ArchitectureEnergyThresholdForCore { get; set; }
    
    // ===== Buff 设置 =====
    
    /// <summary>是否启用势力范围 Buff</summary>
    [JsonPropertyName("EnableInfluenceBuff")]
    public bool EnableInfluenceBuff { get; set; }
    
    /// <summary>最大攻击加成（比例）- 部队最高5%</summary>
    [JsonPropertyName("MaxAttackBonus")]
    public float MaxAttackBonus { get; set; }
    
    /// <summary>最大防御加成（比例）- 部队最高5%</summary>
    [JsonPropertyName("MaxDefenseBonus")]
    public float MaxDefenseBonus { get; set; }
    
    /// <summary>最大攻击加成（比例）- 城池最高10%</summary>
    [JsonPropertyName("MaxArchitectureAttackBonus")]
    public float MaxArchitectureAttackBonus { get; set; }
    
    /// <summary>最大防御加成（比例）- 城池最高10%</summary>
    [JsonPropertyName("MaxArchitectureDefenseBonus")]
    public float MaxArchitectureDefenseBonus { get; set; }
    
    /// <summary>最大粮食消耗减免（比例）- 最高15%</summary>
    [JsonPropertyName("MaxFoodReduction")]
    public float MaxFoodReduction { get; set; }
    
    /// <summary>敌方区域粮食惩罚（比例）- 最高15%</summary>
    [JsonPropertyName("EnemyFoodPenalty")]
    public float EnemyFoodPenalty { get; set; }
    
    /// <summary>分级Buff步长（每N点能量增加一次）</summary>
    [JsonPropertyName("TieredBuffStepSize")]
    public int TieredBuffStepSize { get; set; }
    
    // ===== 🆕 能量分级配置 =====
    // 日期：2026-03-16
    
    /// <summary>能量分级阈值</summary>
    [JsonPropertyName("EnergyTiers")]
    public EnergyTiers EnergyTiers { get; set; }
    
    /// <summary>Buff 配置（分级解锁）</summary>
    [JsonPropertyName("BuffConfig")]
    public BuffConfig BuffConfig { get; set; }
    
    /// <summary>视野/情报配置</summary>
    [JsonPropertyName("VisionConfig")]
    public VisionConfig VisionConfig { get; set; }
    
    // ===== 天气效果查询 =====
    
    /// <summary>
    /// 获取当前天气的能量倍率
    /// </summary>
    public double GetWeatherEnergyMultiplier(string weatherName)
    {
        if (!EnableWeatherEffects) return 1.0;
        
        if (WeatherEffects.TryGetValue(weatherName, out var effect))
        {
            return effect.EnergyMultiplier;
        }
        
        // 🚫 ANTI-BAND-AID：记录未知天气，但提供兜底值避免崩溃
        System.Diagnostics.Debug.WriteLine(
            $"[InfluenceConfig] ⚠️ 未知天气类型：{weatherName}，请检查配置文件或 WeatherType 枚举");
        return 1.0;
    }
    
    /// <summary>
    /// 获取当前天气的地形阻力倍率
    /// </summary>
    public double GetWeatherTerrainCostMultiplier(string weatherName)
    {
        if (!EnableWeatherEffects) return 1.0;
        
        if (WeatherEffects.TryGetValue(weatherName, out var effect))
        {
            return effect.TerrainCostMultiplier;
        }
        
        // 🚫 ANTI-BAND-AID：记录未知天气
        System.Diagnostics.Debug.WriteLine(
            $"[InfluenceConfig] ⚠️ 未知天气类型：{weatherName}，请检查配置文件");
        return 1.0;
    }
    
    /// <summary>
    /// 🆕 获取当前天气的部队地形阻力倍率
    /// 日期：2026-03-20
    /// 说明：部队能量扩散受天气影响更大（雾、雨、雪等恶劣天气会大幅增加扩散阻力）
    /// </summary>
    public double GetWeatherTroopTerrainCostMultiplier(string weatherName)
    {
        if (!EnableWeatherEffects) return 1.0;
        
        if (WeatherEffects.TryGetValue(weatherName, out var effect))
        {
            return effect.TroopTerrainCostMultiplier;
        }
        
        // 🚫 ANTI-BAND-AID：记录未知天气
        System.Diagnostics.Debug.WriteLine(
            $"[InfluenceConfig] ⚠️ 未知天气类型：{weatherName}，请检查配置文件");
        return 1.0;
    }
    
    /// <summary>
    /// 获取地形阻力（考虑覆盖配置）
    /// </summary>
    public int GetTerrainCost(int terrainId, float routewayConsumptionRate)
    {
        // 优先使用覆盖配置
        if (TerrainCostOverrides.TryGetValue(terrainId, out int overrideCost))
        {
            return overrideCost;
        }
        
        // 使用默认计算（基于 RoutewayConsumptionRate）
        if (UseDefaultTerrainCosts)
        {
            return (int)(routewayConsumptionRate * 100);
        }
        
        // 兜底值
        return 100;
    }
}

/// <summary>
/// 🆕 天气效果配置
/// 日期：2026-03-20 更新：添加部队专用地形代价倍率
/// </summary>
public class WeatherEffect
{
    /// <summary>能量倍率（影响势力范围扩散距离）- 城池专用</summary>
    [JsonPropertyName("EnergyMultiplier")]
    public double EnergyMultiplier { get; set; }
    
    /// <summary>地形阻力倍率（影响扩散速度）- 城池专用</summary>
    [JsonPropertyName("TerrainCostMultiplier")]
    public double TerrainCostMultiplier { get; set; }
    
    /// <summary>🆕 部队地形阻力倍率（影响部队能量扩散）- 部队专用</summary>
    [JsonPropertyName("TroopTerrainCostMultiplier")]
    public double TroopTerrainCostMultiplier { get; set; }
    
    /// <summary>效果描述</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; } = "";
}

/// <summary>
/// 🆕 能量分级阈值配置（AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class EnergyTiers
{
    [JsonPropertyName("Tier1")]
    public int Tier1 { get; init; }
    
    [JsonPropertyName("Tier2")]
    public int Tier2 { get; init; }
    
    [JsonPropertyName("Tier3")]
    public int Tier3 { get; init; }
}

/// <summary>
/// 🆕 Buff 配置（分级解锁，AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class BuffConfig
{
    [JsonPropertyName("Tier1Buffs")]
    public Tier1BuffConfig Tier1Buffs { get; init; }
    
    [JsonPropertyName("Tier2Buffs")]
    public Tier2BuffConfig Tier2Buffs { get; init; }
    
    [JsonPropertyName("Tier3Buffs")]
    public Tier3BuffConfig Tier3Buffs { get; init; }
}

/// <summary>
/// 🆕 Tier 1 Buff 配置（AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class Tier1BuffConfig
{
    [JsonPropertyName("DefenseBonus")]
    public float DefenseBonus { get; init; }
    
    [JsonPropertyName("ResistBonus")]
    public float ResistBonus { get; init; }
}

/// <summary>
/// 🆕 Tier 2 Buff 配置（AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class Tier2BuffConfig
{
    [JsonPropertyName("AttackBonusBase")]
    public float AttackBonusBase { get; init; }
    
    [JsonPropertyName("AttackBonusStep")]
    public float AttackBonusStep { get; init; }
    
    [JsonPropertyName("CritBonusBase")]
    public float CritBonusBase { get; init; }
    
    [JsonPropertyName("CritBonusStep")]
    public float CritBonusStep { get; init; }
}

/// <summary>
/// 🆕 Tier 3 Buff 配置（AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class Tier3BuffConfig
{
    [JsonPropertyName("ApRecoveryBase")]
    public float ApRecoveryBase { get; init; }
    
    [JsonPropertyName("ApRecoveryStep")]
    public float ApRecoveryStep { get; init; }
    
    [JsonPropertyName("FatigueRecoveryBase")]
    public float FatigueRecoveryBase { get; init; }
    
    [JsonPropertyName("FatigueRecoveryStep")]
    public float FatigueRecoveryStep { get; init; }
    
    [JsonPropertyName("FoodDiscountBase")]
    public float FoodDiscountBase { get; init; }
    
    [JsonPropertyName("FoodDiscountStep")]
    public float FoodDiscountStep { get; init; }
}

/// <summary>
/// 🆕 视野/情报配置（AOT 友好）
/// 日期：2026-03-16
/// </summary>
public sealed class VisionConfig
{
    [JsonPropertyName("EnableDynamicVision")]
    public bool EnableDynamicVision { get; init; }
    
    [JsonPropertyName("VisionRangeByEnergy")]
    public Dictionary<string, int> VisionRangeByEnergy { get; init; }
    
    [JsonPropertyName("InformationLevelByEnergy")]
    public Dictionary<string, int> InformationLevelByEnergy { get; init; }
}


/// <summary>
/// 🆕 城池能量传播倍率配置（AOT 友好）
/// 日期：2026-03-20
/// 说明：细粒度控制城池能量传播的各种因素
/// </summary>
public sealed class CityEnergySpreadConfig
{
    /// <summary>整体倍率（统一调整所有因素）</summary>
    [JsonPropertyName("OverallMultiplier")]
    public double OverallMultiplier { get; init; }
    
    /// <summary>地形代价倍率（影响基础地形消耗）</summary>
    [JsonPropertyName("TerrainCostMultiplier")]
    public double TerrainCostMultiplier { get; init; }
    
    /// <summary>天气影响倍率（影响天气对地形代价的额外消耗）</summary>
    [JsonPropertyName("WeatherEffectMultiplier")]
    public double WeatherEffectMultiplier { get; init; }
    
    /// <summary>敌军阻断倍率（影响敌军部队的阻断效果）</summary>
    [JsonPropertyName("EnemyBlockingMultiplier")]
    public double EnemyBlockingMultiplier { get; init; }
    
    /// <summary>友军壁垒倍率（影响友军城池的行政壁垒）</summary>
    [JsonPropertyName("AllyBarrierMultiplier")]
    public double AllyBarrierMultiplier { get; init; }
    
    /// <summary>敌城阻力倍率（影响敌方城池的阻力）</summary>
    [JsonPropertyName("EnemyCityResistanceMultiplier")]
    public double EnemyCityResistanceMultiplier { get; init; }
    
    /// <summary>配置说明</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";
}

/// <summary>
/// 🆕 部队能量传播配置（AOT 友好）
/// 日期：2026-03-21
/// 说明：控制部队能量传播的各种参数
/// </summary>
public sealed class TroopEnergySpreadConfig
{
    /// <summary>最小地形代价（避免在官道上无限蔓延）</summary>
    [JsonPropertyName("MinimumTerrainCost")]
    public int MinimumTerrainCost { get; init; }
    
    /// <summary>最小能量阈值（低于此值停止扩散）</summary>
    [JsonPropertyName("MinimumEnergyThreshold")]
    public int MinimumEnergyThreshold { get; init; }
    
    /// <summary>敌城阻力（部队接近敌城时的额外阻力）</summary>
    [JsonPropertyName("EnemyCityResistance")]
    public int EnemyCityResistance { get; init; }
    
    /// <summary>友军壁垒（进入盟友城池范围的阻力）</summary>
    [JsonPropertyName("AllyBarrier")]
    public int AllyBarrier { get; init; }
    
    /// <summary>敌军部队阻断（遇到敌军部队的阻力）</summary>
    [JsonPropertyName("EnemyBlocking")]
    public int EnemyBlocking { get; init; }
    
    /// <summary>配置说明</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";
}

/// <summary>
/// 🆕 能量衰减配置（AOT 友好）
/// 日期：2026-03-21
/// 说明：控制能量自然衰减的参数（修复部队能量永久残留 Bug）
/// 
/// 核心逻辑（水流系统类比）：
/// - 活跃部队能量不衰减（部队在场，持续注入能量 = 活水）
/// - 城池能量不衰减（城池在场，持续注入能量 = 活水）
/// - 残留能量逐渐衰减（部队/城池离开后，能量转为残留 = 死水，逐渐蒸发）
/// </summary>
public sealed class EnergyDecayConfig
{
    /// <summary>保持归属的最小能量（低于此值则失去归属）</summary>
    [JsonPropertyName("MinimumEnergyToRetainOwnership")]
    public int MinimumEnergyToRetainOwnership { get; init; }
    
    /// <summary>配置说明</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";
}

/// <summary>
/// 🆕 能量叠加配置（AOT 友好）
/// 日期：2026-03-21
/// 说明：控制能量叠加的参数（己方协同 + 友军支援 + 残留能量效果）
/// </summary>
public sealed class EnergyStackingConfig
{
    /// <summary>己方协同叠加倍率（副能量 × 30%）</summary>
    [JsonPropertyName("AllySynergyMultiplier")]
    public float AllySynergyMultiplier { get; init; }
    
    /// <summary>友军支援加成倍率（副能量 × 15%）</summary>
    [JsonPropertyName("FriendlySupportMultiplier")]
    public float FriendlySupportMultiplier { get; init; }
    
    /// <summary>残留能量效果倍率（残留能量 × 50%）</summary>
    [JsonPropertyName("ResidualEffectiveness")]
    public float ResidualEffectiveness { get; init; }
    
    /// <summary>配置说明</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";
}

/// <summary>
/// 🆕 残留能量配置（AOT 友好）
/// 日期：2026-03-21
/// 说明：控制残留能量机制的参数（余威能量系统）
/// </summary>
public sealed class ResidualEnergyConfig
{
    /// <summary>是否启用残留能量机制</summary>
    [JsonPropertyName("EnableResidualEnergy")]
    public bool EnableResidualEnergy { get; init; }
    
    /// <summary>残留能量每回合衰减值（死水蒸发速度）</summary>
    [JsonPropertyName("ResidualEnergyDecayPerTurn")]
    public int ResidualEnergyDecayPerTurn { get; init; }
    
    /// <summary>残留能量效果倍率（残留能量只有 50% 效果）</summary>
    [JsonPropertyName("ResidualEffectiveness")]
    public float ResidualEffectiveness { get; init; }
    
    /// <summary>配置说明</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";
}

