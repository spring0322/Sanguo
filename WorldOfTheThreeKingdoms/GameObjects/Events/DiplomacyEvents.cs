#nullable enable
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameManager;
using System;
using GameObjects.FactionDetail;

namespace WorldOfTheThreeKingdoms.GameObjects.Events;

/// <summary>
/// 外交事件系统（AOT 兼容的强类型事件）
/// 用于替代 ExtensionInterface.call 的动态反射调用
/// 
/// 注意：此处暴露的事件需要在游戏初始化（如 Session 加载处）进行显式订阅，否则逻辑将静默断裂
/// </summary>
public static class DiplomacyEvents
{
    // ==================== 首都相关事件 ====================
    
    /// <summary>
    /// 主动迁都时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnChangeCapital;

    /// <summary>
    /// 被迫迁都时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnForceChangeCapital;

    // ==================== 势力相关事件 ====================
    
    /// <summary>
    /// 势力变更时触发（建筑归属变化）
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnChangeFaction;

    /// <summary>
    /// 势力灭亡时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnFactionDestroyed;

    // ==================== 君主相关事件 ====================
    
    /// <summary>
    /// 君主更替时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnChangeKing;

    /// <summary>
    /// 合法称帝时触发（禅让）
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnBecomeEmperorLegally;

    /// <summary>
    /// 自立称帝时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnSelfBecomeEmperor;

    // ==================== 官爵相关事件 ====================
    
    /// <summary>
    /// 朝廷晋升官爵时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnAdvancement;

    /// <summary>
    /// 自封官爵时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnSelfAdvancement;

    // ==================== 技术相关事件 ====================
    
    /// <summary>
    /// 技术研发完成时触发
    /// 参数: (GameScenario scenario, Faction faction, Technique technique)
    /// </summary>
    public static event Action<GameScenario, Faction, Technique>? OnTechniqueUpgradeComplete;

    /// <summary>
    /// 开始研发技术时触发
    /// 参数: (GameScenario scenario, Faction faction)
    /// </summary>
    public static event Action<GameScenario, Faction>? OnUpgradeTechnique;

    // ==================== 事件触发方法 ====================

    public static void RaiseChangeCapital(GameScenario scenario, Faction faction)
    {
        OnChangeCapital?.Invoke(scenario, faction);
    }

    public static void RaiseForceChangeCapital(GameScenario scenario, Faction faction)
    {
        OnForceChangeCapital?.Invoke(scenario, faction);
    }

    public static void RaiseChangeFaction(GameScenario scenario, Faction faction)
    {
        OnChangeFaction?.Invoke(scenario, faction);
    }

    public static void RaiseFactionDestroyed(GameScenario scenario, Faction faction)
    {
        OnFactionDestroyed?.Invoke(scenario, faction);
    }

    public static void RaiseChangeKing(GameScenario scenario, Faction faction)
    {
        OnChangeKing?.Invoke(scenario, faction);
    }

    public static void RaiseBecomeEmperorLegally(GameScenario scenario, Faction faction)
    {
        OnBecomeEmperorLegally?.Invoke(scenario, faction);
    }

    public static void RaiseSelfBecomeEmperor(GameScenario scenario, Faction faction)
    {
        OnSelfBecomeEmperor?.Invoke(scenario, faction);
    }

    public static void RaiseAdvancement(GameScenario scenario, Faction faction)
    {
        OnAdvancement?.Invoke(scenario, faction);
    }

    public static void RaiseSelfAdvancement(GameScenario scenario, Faction faction)
    {
        OnSelfAdvancement?.Invoke(scenario, faction);
    }

    public static void RaiseTechniqueUpgradeComplete(GameScenario scenario, Faction faction, Technique technique)
    {
        OnTechniqueUpgradeComplete?.Invoke(scenario, faction, technique);
    }

    public static void RaiseUpgradeTechnique(GameScenario scenario, Faction faction)
    {
        OnUpgradeTechnique?.Invoke(scenario, faction);
    }
}
