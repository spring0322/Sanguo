using System;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// 天气类型枚举
/// 日期：2026-03-09
/// </summary>
public enum WeatherType
{
    Sunny = 0,      // ☀️ 晴天
    Cloudy = 1,     // ☁️ 阴天
    Rain = 2,       // 🌧️ 雨天
    Snow = 3,       // ❄️ 雪天
    Fog = 4,        // 🌫️ 雾天
    Count = 5       // 🔥 枚举数量标记（用于数组分配）
}

/// <summary>
/// 气候带枚举（用于天气概率配置）
/// 日期：2026-03-09
/// </summary>
public enum ClimateZone
{
    Northern = 0,   // 北方（如幽州、并州）
    Central = 1,    // 中原（如豫州、兖州）
    Southern = 2,   // 南方（如荆州、扬州）
    Western = 3,    // 西部（如凉州、益州）
    Count = 4
}

/// <summary>
/// 季节类型枚举
/// 日期：2026-03-09
/// </summary>
public enum SeasonType
{
    Spring = 0,     // 春季
    Summer = 1,     // 夏季
    Autumn = 2,     // 秋季
    Winter = 3,     // 冬季
    Count = 4
}

/// <summary>
/// 风向枚举（使用 byte 极致压缩内存）
/// 日期：2026-03-10
/// </summary>
public enum WindDirection : byte
{
    None = 0,       // 无风
    North = 1,      // 北风（吹向南）
    South = 2,      // 南风（吹向北）
    East = 3,       // 东风（吹向西）
    West = 4,       // 西风（吹向东）
    NorthWest = 5,  // 西北风
    NorthEast = 6,  // 东北风
    SouthWest = 7,  // 西南风
    SouthEast = 8   // 东南风
}

/// <summary>
/// 风力等级枚举（使用 byte 极致压缩内存）
/// 日期：2026-03-10
/// </summary>
public enum WindForce : byte
{
    None = 0,       // 无风
    Breeze = 1,     // 微风（仅表现效果）
    Strong = 2,     // 大风（影响射箭与航行）
    Gale = 3        // 狂风（引发火烧连营）
}

/// <summary>
/// WeatherType 扩展方法
/// 日期：2026-03-12
/// </summary>
public static class WeatherTypeExtensions
{
    /// <summary>
    /// 是否抑制火焰（雨雪天气）
    /// 🔥 热路径：每次火系计略/战法判定都会调用
    /// </summary>
    public static bool SuppressFire(this WeatherType weather)
    {
        return weather == WeatherType.Rain || weather == WeatherType.Snow;
    }

    /// <summary>
    /// 是否增强水系效果（雨雪天气）
    /// 🔥 热路径：每次水系计略判定都会调用
    /// </summary>
    public static bool EnhanceWater(this WeatherType weather)
    {
        return weather == WeatherType.Rain || weather == WeatherType.Snow;
    }
}

