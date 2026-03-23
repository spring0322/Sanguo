#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// 天气系统配置（对应 EnvironmentConfig.json）
/// 日期：2026-03-09
/// </summary>
[JsonSerializable(typeof(GameEnvironmentConfig))]
[JsonSerializable(typeof(WeatherConfig))]
[JsonSerializable(typeof(WeatherMovementConfig))]
[JsonSerializable(typeof(WeatherStratagemConfig))]
[JsonSerializable(typeof(WeatherStratagemModifier))]
[JsonSerializable(typeof(WindConfig))]
[JsonSerializable(typeof(WindSeasonalDirections))]
[JsonSerializable(typeof(WindDirectionProbabilities))]
[JsonSerializable(typeof(WindForceProbabilities))]
[JsonSerializable(typeof(WindSeasonModifiers))]
[JsonSerializable(typeof(WindWeatherModifiers))]
[JsonSerializable(typeof(WeatherWindRules))]
[JsonSerializable(typeof(WeatherViewConfig))]
[JsonSerializable(typeof(WeatherViewModifiers))]
[JsonSerializable(typeof(WeatherCombatConfig))]
[JsonSerializable(typeof(WeatherCombatModifier))]
[JsonSerializable(typeof(WindStratagemConfig))]
[JsonSerializable(typeof(WindStratagemForceModifiers))]
[JsonSerializable(typeof(WindDirectionModifier))]
[JsonSerializable(typeof(ClimateSeasonRule))]
[JsonSerializable(typeof(WeatherMovementPenalty))]
[JsonSerializable(typeof(StateWeatherZone))]
[JsonSerializable(typeof(WeatherType))]
[JsonSerializable(typeof(ClimateZone))]
[JsonSerializable(typeof(SeasonType))]
[JsonSerializable(typeof(WindDirection))]
[JsonSerializable(typeof(WindForce))]
internal partial class EnvironmentConfigContext : JsonSerializerContext { }

/// <summary>
/// 顶层环境配置
/// </summary>
public sealed record GameEnvironmentConfig
{
    public required WeatherConfig Weather { get; init; }
    public required WeatherMovementConfig WeatherMovement { get; init; }
    public WeatherStratagemConfig? WeatherStratagem { get; init; }  // 🔥 2026-03-10 新增：计略天气影响
    public required WindConfig Wind { get; init; }  // 🔥 2026-03-10 新增：风向风力配置
    public required WeatherViewConfig WeatherView { get; init; }  // 🔥 2026-03-10 新增：视野天气影响
    public WeatherCombatConfig? WeatherCombat { get; init; }  // 🔥 2026-03-10 新增：物理伤害天气影响
    public WindStratagemConfig? WindStratagem { get; init; }  // 🔥 2026-03-10 新增：风向计略影响
}

/// <summary>
/// 天气推演配置
/// </summary>
public sealed class WeatherConfig
{
    /// <summary>雨天惯性概率（0-100）</summary>
    public int RainInertiaChance { get; init; } = 40;
    
    /// <summary>雪天惯性概率（0-100）</summary>
    public int SnowInertiaChance { get; init; } = 50;
    
    /// <summary>气候带与季节的天气规则</summary>
    public List<ClimateSeasonRule> ClimateRules { get; init; } = [];
    
    /// <summary>州域与气候带的映射（用于按州域设置不同天气概率）</summary>
    public List<StateWeatherZone> StateZones { get; init; } = [];
}

/// <summary>
/// 气候带与季节的天气概率规则
/// </summary>
public sealed class ClimateSeasonRule
{
    public required ClimateZone Zone { get; init; }
    public required SeasonType Season { get; init; }
    
    /// <summary>雨天概率（0-100）</summary>
    public int RainChance { get; init; }
    
    /// <summary>雪天概率（0-100）</summary>
    public int SnowChance { get; init; }
    
    /// <summary>阴天概率（0-100）</summary>
    public int CloudyChance { get; init; }
    
    /// <summary>雾天概率（0-100）</summary>
    public int FogChance { get; init; }
    
    // 剩余概率自动为晴天
}

/// <summary>
/// 州域与气候带的映射（用于按州域设置不同天气）
/// </summary>
public sealed class StateWeatherZone
{
    /// <summary>州域 ID（🔥 注意：ID >= 0 是有效的，ID < 0 表示无效）</summary>
    public required int StateID { get; init; }
    
    /// <summary>该州域所属的气候带</summary>
    public required ClimateZone Zone { get; init; }
}

/// <summary>
/// 天气移动惩罚配置
/// </summary>
public sealed class WeatherMovementConfig
{
    public List<WeatherMovementPenalty> Penalties { get; init; } = [];
}

/// <summary>
/// 单条天气移动惩罚规则
/// </summary>
public sealed class WeatherMovementPenalty
{
    public required WeatherType Weather { get; init; }
    public required TerrainType Terrain { get; init; }
    
    /// <summary>
    /// 兵种类型（null 表示所有兵种）
    /// </summary>
    public UnitType? Unit { get; init; }
    
    /// <summary>额外移动消耗</summary>
    public int ExtraCost { get; init; }
}

/// <summary>
/// 天气对计略的影响配置
/// 日期：2026-03-10
/// 更新：2026-03-12 新增基于 Influence 的火水系判定
/// </summary>
public sealed class WeatherStratagemConfig
{
    public string? Description { get; init; }
    public List<WeatherStratagemModifier> Modifiers { get; init; } = [];
    
    /// <summary>
    /// 火系 InfluenceKind ID 集合（用于判断纯火系计略）
    /// 日期：2026-03-12
    /// 🔥 性能优化：使用 HashSet 实现 O(1) 查找
    /// 示例：[394] 表示 InfluenceKind394 是火焰伤害
    /// </summary>
    public HashSet<int> FireInfluenceKindIDs { get; init; } = [];
    
    /// <summary>
    /// 水系 InfluenceKind ID 集合（用于判断纯水系计略）
    /// 日期：2026-03-12
    /// 🔥 性能优化：使用 HashSet 实现 O(1) 查找
    /// </summary>
    public HashSet<int> WaterInfluenceKindIDs { get; init; } = [];
    
    /// <summary>
    /// 雨雪天气对纯火系的抑制率（0.0 = 完全失效，1.0 = 无影响）
    /// 日期：2026-03-12
    /// </summary>
    public float PureFireSuppressionRate { get; init; } = 0f;
    
    /// <summary>
    /// 雨雪天气对附带火焰的抑制率（0.0 = 完全失效，1.0 = 无影响）
    /// 日期：2026-03-12
    /// </summary>
    public float AttachedFireSuppressionRate { get; init; } = 0.3f;
    
    /// <summary>
    /// 雨雪天气对水系的增强率（1.0 = 无影响，>1.0 = 增强）
    /// 日期：2026-03-12
    /// </summary>
    public float WaterEnhancementRate { get; init; } = 1.3f;
}

/// <summary>
/// 单条天气计略修正规则
/// 日期：2026-03-10
/// </summary>
public sealed class WeatherStratagemModifier
{
    /// <summary>计略动画类型（用于匹配计略）</summary>
    public required string AnimationKind { get; init; }
    
    /// <summary>天气类型</summary>
    public required string Weather { get; init; }
    
    /// <summary>效果倍率（0.0 = 完全失效，1.0 = 无影响，>1.0 = 增强）</summary>
    public float Multiplier { get; init; } = 1.0f;
    
    /// <summary>说明</summary>
    public string? Description { get; init; }
}

/// <summary>
/// 风向风力系统配置
/// 日期：2026-03-10
/// </summary>
public sealed class WindConfig
{
    public string? Description { get; init; }
    
    /// <summary>按季节配置的风向概率（0-100）</summary>
    public required WindSeasonalDirections DirectionProbabilities { get; init; }
    
    /// <summary>全局风力概率（0-100）</summary>
    public required WindForceProbabilities ForceProbabilities { get; init; }
    
    /// <summary>季节对风力的修正值（-100 到 +100）</summary>
    public required WindSeasonModifiers SeasonModifiers { get; init; }
    
    /// <summary>天气对风力的修正值（-100 到 +100）</summary>
    public required WindWeatherModifiers WeatherModifiers { get; init; }
    
    /// <summary>
    /// 风向惯性概率（0-100）
    /// 日期：2026-03-10
    /// 说明：风向保持不变的概率，类似于天气惯性
    /// </summary>
    public int DirectionInertiaChance { get; init; } = 60;
    
    /// <summary>
    /// 气象互斥规则配置
    /// 日期：2026-03-10
    /// </summary>
    public required WeatherWindRules Rules { get; init; }
}

/// <summary>
/// 按季节配置的风向概率（AOT 友好结构）
/// </summary>
public sealed class WindSeasonalDirections
{
    public required WindDirectionProbabilities Spring { get; init; }
    public required WindDirectionProbabilities Summer { get; init; }
    public required WindDirectionProbabilities Autumn { get; init; }
    public required WindDirectionProbabilities Winter { get; init; }
}

/// <summary>
/// 风向概率配置（8 个方向，总和应为 100）
/// </summary>
public sealed class WindDirectionProbabilities
{
    public int North { get; init; }
    public int South { get; init; }
    public int East { get; init; }
    public int West { get; init; }
    public int NorthWest { get; init; }
    public int NorthEast { get; init; }
    public int SouthWest { get; init; }
    public int SouthEast { get; init; }
}

/// <summary>
/// 风力概率配置（4 个等级，总和应为 100）
/// </summary>
public sealed class WindForceProbabilities
{
    public int None { get; init; }
    public int Breeze { get; init; }
    public int Strong { get; init; }
    public int Gale { get; init; }
}

/// <summary>
/// 季节对风力的修正值（AOT 友好结构）
/// </summary>
public sealed class WindSeasonModifiers
{
    public int Spring { get; init; }
    public int Summer { get; init; }
    public int Autumn { get; init; }
    public int Winter { get; init; }
}

/// <summary>
/// 天气对风力的修正值（AOT 友好结构）
/// </summary>
public sealed class WindWeatherModifiers
{
    public int Rain { get; init; }
    public int Snow { get; init; }
    public int Fog { get; init; }
    public int Cloudy { get; init; }
    public int Sunny { get; init; }
}

/// <summary>
/// 气象互斥规则配置
/// 日期：2026-03-10
/// 说明：定义天气与风力之间的互斥关系
/// </summary>
public sealed class WeatherWindRules
{
    /// <summary>
    /// 大风吹散大雾的最小风力阈值
    /// 说明：当风力 >= 此值时，雾天会被吹散
    /// </summary>
    public WindForce FogDispersalMinWindForce { get; init; } = WindForce.Strong;
    
    /// <summary>
    /// 雾被吹散后转为晴天的概率（0-100）
    /// 说明：剩余概率转为阴天
    /// </summary>
    public int FogToSunnyChance { get; init; } = 50;
    
    /// <summary>
    /// 极端天气（雨/雪）的最小风力保证
    /// 说明：雨天/雪天至少有此风力
    /// </summary>
    public WindForce ExtremeWeatherMinWindForce { get; init; } = WindForce.Breeze;
    
    /// <summary>
    /// 是否启用气象互斥规则
    /// 说明：可用于调试或禁用特定规则
    /// </summary>
    public bool EnableFogDispersal { get; init; } = true;
    public bool EnableExtremeWeatherWind { get; init; } = true;
}

/// <summary>
/// 天气对视野的影响配置
/// 日期：2026-03-10
/// </summary>
public sealed class WeatherViewConfig
{
    public string? Description { get; init; }
    
    /// <summary>视野最小值（无论天气多恶劣，至少保留此视野）</summary>
    public int MinViewRadius { get; init; } = 1;
    
    /// <summary>各天气对视野的修正</summary>
    public required WeatherViewModifiers Modifiers { get; init; }
}

/// <summary>
/// 天气对视野的修正值（AOT 友好结构）
/// 日期：2026-03-10
/// 说明：负值表示减少视野，0 表示无影响
/// </summary>
public sealed class WeatherViewModifiers
{
    /// <summary>大雾天视野修正（建议 -50% 或更多）</summary>
    public int Fog { get; init; }
    
    /// <summary>大雪天视野修正</summary>
    public int Snow { get; init; }
    
    /// <summary>暴雨天视野修正</summary>
    public int Rain { get; init; }
    
    /// <summary>阴天视野修正</summary>
    public int Cloudy { get; init; }
    
    /// <summary>晴天视野修正</summary>
    public int Sunny { get; init; }
}

/// <summary>
/// 天气对物理伤害（战法/普攻）的影响配置
/// 日期：2026-03-10
/// </summary>
public sealed class WeatherCombatConfig
{
    public string? Description { get; init; }
    
    /// <summary>物理伤害天气修正规则列表</summary>
    public List<WeatherCombatModifier> Modifiers { get; init; } = [];
}

/// <summary>
/// 单条物理伤害天气修正规则
/// 日期：2026-03-10
/// 说明：根据攻击方兵种、攻击方天气、目标方天气计算伤害修正
/// </summary>
public sealed class WeatherCombatModifier
{
    /// <summary>攻击方兵种类型（步兵、弩兵、骑兵、水军、器械）</summary>
    public required string AttackerType { get; init; }
    
    /// <summary>攻击方所在位置的天气（可选，null 表示任意天气）</summary>
    public string? AttackerWeather { get; init; }
    
    /// <summary>目标方所在位置的天气（可选，null 表示任意天气）</summary>
    public string? TargetWeather { get; init; }
    
    /// <summary>伤害倍率（0.0 = 完全无效，1.0 = 无影响，>1.0 = 增强）</summary>
    public float Multiplier { get; init; } = 1.0f;
    
    /// <summary>说明</summary>
    public string? Description { get; init; }
}

/// <summary>
/// 风向对计略的影响配置
/// 日期：2026-03-10
/// </summary>
public sealed class WindStratagemConfig
{
    public string? Description { get; init; }
    
    /// <summary>火系计略的风向修正规则</summary>
    public required WindStratagemForceModifiers FireStratagemModifiers { get; init; }
}

/// <summary>
/// 不同风力等级的修正值（AOT 友好结构）
/// 日期：2026-03-10
/// </summary>
public sealed class WindStratagemForceModifiers
{
    /// <summary>狂风修正</summary>
    public required WindDirectionModifier Gale { get; init; }
    
    /// <summary>大风修正</summary>
    public required WindDirectionModifier Strong { get; init; }
    
    /// <summary>微风修正（通常为 1.0，不影响）</summary>
    public required WindDirectionModifier Breeze { get; init; }
}

/// <summary>
/// 单个风力等级的顺风/逆风修正
/// 日期：2026-03-10
/// </summary>
public sealed class WindDirectionModifier
{
    /// <summary>顺风时的伤害倍率</summary>
    public float Downwind { get; init; } = 1.0f;
    
    /// <summary>逆风时的伤害倍率</summary>
    public float Upwind { get; init; } = 1.0f;
    
    /// <summary>说明</summary>
    public string? Description { get; init; }
}
