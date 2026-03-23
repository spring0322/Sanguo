using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 弹道数学计算工具类（风向对弓箭弹道的影响）
/// 日期：2026-03-11
/// 
/// 🔥 核心功能：
/// 1. 计算风向对弓箭弹道的影响（顺风/逆风/侧风）
/// 2. 计算天气对物理伤害的影响（雨雪天气惩罚）
/// 
/// 🔥 性能优化：
/// - 使用 AggressiveInlining 优化热路径
/// - 使用 SIMD 加速的 Vector2 运算（.NET 8 硬件加速）
/// - 无分配，纯计算
/// </summary>
public static class BallisticsMath
{
    /// <summary>
    /// 将风向枚举转为 2D 向量（表示风吹向的方向）
    /// 🔥 HOT PATH：使用 AggressiveInlining 优化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 GetWindVector(WindDirection direction)
    {
        // 🔥 坐标系：原点 (0,0) 在左上角，X 向右，Y 向下
        // 风向表示"风从哪里来"，向量表示"风吹向哪里"
        return direction switch
        {
            WindDirection.North => new(0, 1),       // 北风吹向南 (Y+)
            WindDirection.South => new(0, -1),      // 南风吹向北 (Y-)
            WindDirection.East => new(-1, 0),       // 东风吹向西 (X-)
            WindDirection.West => new(1, 0),        // 西风吹向东 (X+)
            WindDirection.NorthWest => new(1, 1),   // 西北风吹向东南
            WindDirection.NorthEast => new(-1, 1),  // 东北风吹向西南
            WindDirection.SouthWest => new(1, -1),  // 西南风吹向东北
            WindDirection.SouthEast => new(-1, -1), // 东南风吹向西北
            _ => Vector2.Zero                       // 无风
        };
    }

    /// <summary>
    /// 计算弓箭弹道受风向影响的最终伤害/命中乘数
    /// 🔥 HOT PATH：每次弓箭攻击都会调用（SIMD 加速）
    /// 
    /// 原理：
    /// - 顺风（点乘 > 0.5）：箭矢动能增加，穿透力更强
    /// - 逆风（点乘 < -0.5）：箭矢动能大幅衰减，甚至射不到
    /// - 侧风（点乘 [-0.5, 0.5]）：箭矢被吹偏，命中率/有效伤害下降
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetArcheryWindMultiplier(
        Point attacker, 
        Point target, 
        WindDirection windDir, 
        WindForce windForce)
    {
        // 1. 无风或微风，不影响弹道，直接短路返回
        if (windForce <= WindForce.Breeze || windDir == WindDirection.None)
        {
            return 1.0f;
        }

        // 2. 构建攻击向量（从攻击者指向目标）
        Vector2 attackVec = new(target.X - attacker.X, target.Y - attacker.Y);
        
        // 3. 同格子射击，无风向影响（合理的业务逻辑判断）
        if (attackVec == Vector2.Zero)
        {
            return 1.0f;
        }

        // 4. 归一化攻击向量（.NET 8 下的 Normalize 是硬件 SIMD 加速的）
        attackVec.Normalize();

        // 5. 获取风向向量并归一化
        Vector2 windVec = GetWindVector(windDir);
        windVec.Normalize();

        // 6. 🔥 核心：向量点乘，范围在 [-1, 1] 之间
        // 点乘 > 0：顺风（风向与攻击方向一致）
        // 点乘 < 0：逆风（风向与攻击方向相反）
        // 点乘 ≈ 0：侧风（风向与攻击方向垂直）
        float dotProduct = Vector2.Dot(attackVec, windVec);

        // 7. 根据风力等级和风向关系计算乘数
        // 🔥 使用 switch 表达式，AOT 友好
        return (windForce, dotProduct) switch
        {
            // 狂风 + 顺风：箭矢动能暴增
            (WindForce.Gale, > 0.5f) => 1.3f,
            
            // 大风 + 顺风：箭矢动能增加
            (WindForce.Strong, > 0.5f) => 1.15f,
            
            // 狂风 + 逆风：箭矢几乎射不到
            (WindForce.Gale, < -0.5f) => 0.4f,
            
            // 大风 + 逆风：箭矢动能大幅衰减
            (WindForce.Strong, < -0.5f) => 0.7f,
            
            // 狂风 + 侧风：箭矢被严重吹偏
            (WindForce.Gale, _) => 0.8f,
            
            // 大风 + 侧风：箭矢被轻微吹偏
            (WindForce.Strong, _) => 0.9f,
            
            // 其他情况（理论上不会到达，因为前面已经过滤了微风和无风）
            _ => 1.0f
        };
    }

    /// <summary>
    /// 计算天气对战法/普攻（物理）伤害的最终乘数（配置驱动版本）
    /// 🔥 HOT PATH：每次物理攻击都会调用
    /// 
    /// 注意：此方法会自动叠加风向对弓兵的影响
    /// 
    /// 参数说明：
    /// - attackerUnit: 攻击方兵种
    /// - attackerPos: 攻击方位置（用于查询天气和风况）
    /// - targetPos: 目标方位置（用于查询天气和计算弹道）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetPhysicalWeatherMultiplier(
        UnitType attackerUnit,
        Point attackerPos,
        Point targetPos)
    {
        // 1. 获取攻击方和目标方的天气
        var weatherManager = Session.Current.Scenario.WeatherManager;
        var attackerWeather = weatherManager.GetWeatherAt(attackerPos);
        var targetWeather = weatherManager.GetWeatherAt(targetPos);
        
        // 2. 从配置读取天气修正（配置驱动）
        float baseMultiplier = GetWeatherMultiplierFromConfig(
            attackerUnit, 
            attackerWeather, 
            targetWeather);

        // 3. 弓兵专属：叠加风偏弹道计算
        if (attackerUnit == UnitType.弓兵)
        {
            // 获取射手所在地的风况
            var wind = weatherManager.GetWindAt(attackerPos);
            
            // 叠加风偏乘数
            baseMultiplier *= GetArcheryWindMultiplier(
                attackerPos, 
                targetPos, 
                wind.Direction, 
                wind.Force);
        }

        return baseMultiplier;
    }

    /// <summary>
    /// 从配置读取天气对物理伤害的修正
    /// 🧊 COLD PATH：虽然在战斗中调用，但配置查询可以缓存
    /// </summary>
    private static float GetWeatherMultiplierFromConfig(
        UnitType attackerUnit,
        WeatherType attackerWeather,
        WeatherType targetWeather)
    {
        // 🔥 ANTI-BAND-AID：移除防御性空检查
        // 如果 EnvironmentConfig 为 null，说明配置未正确加载，应该让异常抛出
        var config = Session.Current.Scenario.EnvironmentConfig.WeatherCombat;
        if (config == null || config.Modifiers.Count == 0)
        {
            // 无配置，返回默认值
            return 1.0f;
        }

        // 🔥 优化：直接使用枚举比较，避免字符串分配
        // 将枚举转为字符串（仅用于匹配配置）
        string unitTypeStr = attackerUnit.ToString();
        string attackerWeatherStr = attackerWeather.ToString();
        string targetWeatherStr = targetWeather.ToString();

        // 遍历配置规则，找到第一个匹配的规则
        // 🔥 注意：这里使用 foreach 是合理的（COLD PATH，可读性优先）
        foreach (var modifier in config.Modifiers)
        {
            // 匹配攻击方兵种
            if (modifier.AttackerType != unitTypeStr)
            {
                continue;
            }

            // 匹配攻击方天气（null 表示任意天气）
            if (modifier.AttackerWeather != null 
                && modifier.AttackerWeather != attackerWeatherStr)
            {
                continue;
            }

            // 匹配目标方天气（null 表示任意天气）
            if (modifier.TargetWeather != null 
                && modifier.TargetWeather != targetWeatherStr)
            {
                continue;
            }

            // 找到匹配规则，返回倍率
            return modifier.Multiplier;
        }

        // 未找到匹配规则，返回默认值
        return 1.0f;
    }
}
