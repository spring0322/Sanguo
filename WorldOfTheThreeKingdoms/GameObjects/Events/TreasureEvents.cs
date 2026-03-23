#nullable enable
using GameObjects;
using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Events;

/// <summary>
/// 宝物交易事件系统（AOT 兼容的强类型事件）
/// 🔥 2026-03-03 新增：用于宝物买卖系统
/// </summary>
public static class TreasureEvents
{
    /// <summary>
    /// 出售宝物时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Treasure treasure)
    /// </summary>
    public static event Action<GameScenario, Architecture, Treasure>? OnSellTreasure;

    /// <summary>
    /// 购买宝物时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Treasure treasure)
    /// </summary>
    public static event Action<GameScenario, Architecture, Treasure>? OnBuyTreasure;

    /// <summary>
    /// 触发出售宝物事件
    /// </summary>
    public static void RaiseSellTreasure(GameScenario scenario, Architecture architecture, Treasure treasure)
    {
        OnSellTreasure?.Invoke(scenario, architecture, treasure);
    }

    /// <summary>
    /// 触发购买宝物事件
    /// </summary>
    public static void RaiseBuyTreasure(GameScenario scenario, Architecture architecture, Treasure treasure)
    {
        OnBuyTreasure?.Invoke(scenario, architecture, treasure);
    }
}
