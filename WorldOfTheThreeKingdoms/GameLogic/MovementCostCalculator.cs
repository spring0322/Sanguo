using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using static WorldOfTheThreeKingdoms.GameGlobal.WeatherType;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 移动消耗计算器（天气影响寻路）
/// 日期：2026-03-09
/// 性能：使用 3D 数组烘焙配置，O(1) 查表
/// </summary>
public static class MovementCostCalculator
{
    // 🔥 预分配的 3D 查表数组（天气 × 地形 × 兵种）
    // 内存占用：5 × 7 × 5 × 4字节 = 700 字节（极其轻量）
    private static readonly int[,,] _penaltyMap = new int[
        (int)WeatherType.Count,
        (int)TerrainType.Other + 1, // TerrainType 没有 Count，使用最大值 + 1
        5 // UnitType 枚举数量（步兵、骑兵、弓兵、攻城器械、水军）
    ];

    /// <summary>
    /// 当配置加载或热重载时被调用（冷路径，允许 LINQ）
    /// </summary>
    public static void BakeConfig(WeatherMovementConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        Array.Clear(_penaltyMap, 0, _penaltyMap.Length);
        
        if (config.Penalties == null)
            throw new InvalidOperationException("WeatherMovementConfig.Penalties 不能为 null");

        // 第一轮：通用规则（Unit == null）
        foreach (var rule in config.Penalties)
        {
            if (rule.Unit.HasValue) continue; // 跳过特定兵种规则

            int w = (int)rule.Weather;
            int t = (int)rule.Terrain;
            
            for (int u = 0; u < _penaltyMap.GetLength(2); u++)
            {
                _penaltyMap[w, t, u] = rule.ExtraCost;
            }
        }

        // 第二轮：特定规则覆盖（Unit != null）
        foreach (var rule in config.Penalties)
        {
            if (!rule.Unit.HasValue) continue;

            int w = (int)rule.Weather;
            int t = (int)rule.Terrain;
            int u = (int)rule.Unit.Value;
            _penaltyMap[w, t, u] = rule.ExtraCost;
        }
        
        System.Diagnostics.Debug.WriteLine("[MovementCostCalculator] 天气寻路惩罚矩阵已烘焙完毕。");
    }

    /// <summary>
    /// 获取天气移动惩罚
    /// 🔥 HOT PATH：每帧可能被调用数千次（寻路系统）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetWeatherPenalty(Point targetTile, UnitType unitType, TerrainType terrain)
    {
        // 1. 获取天气（断言：WeatherManager 必须在 Scenario 初始化时创建）
        var weatherManager = Session.Current.Scenario.WeatherManager!;
        var weather = weatherManager.GetWeatherAt(targetTile);
        
        // 2. 好天气短路（无惩罚）
        if (weather is WeatherType.Sunny or WeatherType.Cloudy) return 0;

        // 3. O(1) 查表获取天气惩罚
        return _penaltyMap[(int)weather, (int)terrain, (int)unitType];
    }

    /// <summary>
    /// 计算目标地块的最终移动消耗（结合地形、兵种与实时天气）
    /// 🔥 HOT PATH：标注为 AggressiveInlining 强制内联，消除函数调用开销
    /// 日期：2026-03-10
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFinalMovementCost(Point targetTile, UnitType unitType, TerrainType terrain, int baseCost)
    {
        // 1. 不可通行地形短路
        if (baseCost >= 9999) return baseCost;

        // 2. ANTI-BAND-AID：运行时断言（仅 DEBUG 模式）
        // Release 模式下编译器会移除此检查，保持性能
        System.Diagnostics.Debug.Assert(
            Session.Current?.Scenario?.WeatherManager != null,
            "数据损坏：WeatherManager 未初始化！请检查 Scenario.Init() 是否正确调用。");

        // 3. 获取天气（假设已初始化，Release 模式下无检查）
        var weatherManager = Session.Current.Scenario.WeatherManager;
        var weather = weatherManager.GetWeatherAt(targetTile);

        // 4. 好天气短路
        if (weather is WeatherType.Sunny or WeatherType.Cloudy) return baseCost;

        // 5. O(1) 查表获取天气惩罚并叠加
        int extraCost = _penaltyMap[(int)weather, (int)terrain, (int)unitType];
        return baseCost + extraCost;
    }
}
