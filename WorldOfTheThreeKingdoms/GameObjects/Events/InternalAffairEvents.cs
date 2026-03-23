#nullable enable
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameManager;
using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Events;

/// <summary>
/// 内政事件系统（AOT 兼容的强类型事件）
/// 用于替代 ExtensionInterface.call 的动态反射调用
/// 
/// 注意：此处暴露的事件需要在游戏初始化（如 Session 加载处）进行显式订阅，否则逻辑将静默断裂
/// </summary>
public static class InternalAffairEvents
{
    // ==================== 设施相关事件 ====================
    
    /// <summary>
    /// 开始建造设施时触发
    /// 参数: (GameScenario scenario, Architecture architecture, FacilityKind facilityKind)
    /// </summary>
    public static event Action<GameScenario, Architecture, FacilityKind>? OnStartBuildFacility;

    /// <summary>
    /// 设施建造完成时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Facility facility)
    /// </summary>
    public static event Action<GameScenario, Architecture, Facility>? OnFacilityCompleted;

    /// <summary>
    /// 设施被拆除时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Facility facility)
    /// </summary>
    public static event Action<GameScenario, Architecture, Facility>? OnFacilityDemolished;

    // ==================== 资源相关事件 ====================
    
    /// <summary>
    /// 购买粮食时触发
    /// 参数: (GameScenario scenario, Architecture architecture)
    /// </summary>
    public static event Action<GameScenario, Architecture>? OnBuyFood;

    /// <summary>
    /// 出售粮食时触发
    /// 参数: (GameScenario scenario, Architecture architecture)
    /// </summary>
    public static event Action<GameScenario, Architecture>? OnSellFood;

    // ==================== 灾难相关事件 ====================
    
    /// <summary>
    /// 灾难发生时触发
    /// 参数: (GameScenario scenario, Architecture architecture, int disasterType)
    /// </summary>
    public static event Action<GameScenario, Architecture, int>? OnDisasterHappened;

    // ==================== 军事相关事件 ====================
    
    /// <summary>
    /// 创建盗贼部队时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Troop troop)
    /// </summary>
    public static event Action<GameScenario, Architecture, Troop>? OnCreateRobberTroop;

    /// <summary>
    /// 解散编队时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Military military)
    /// </summary>
    public static event Action<GameScenario, Architecture, Military>? OnDisbandMilitary;

    /// <summary>
    /// 编队升级时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Military military)
    /// </summary>
    public static event Action<GameScenario, Architecture, Military>? OnLevelUpMilitary;

    /// <summary>
    /// 创建编队时触发
    /// 参数: (GameScenario scenario, Architecture architecture, Military military)
    /// </summary>
    public static event Action<GameScenario, Architecture, Military>? OnMilitaryCreate;

    // ==================== AI 相关事件 ====================
    
    /// <summary>
    /// AI 建筑决策时触发（用于扩展 AI 逻辑）
    /// 参数: (GameScenario scenario, Architecture architecture)
    /// </summary>
    public static event Action<GameScenario, Architecture>? OnAIArchitecture;

    // ==================== 事件触发方法 ====================

    public static void RaiseStartBuildFacility(GameScenario scenario, Architecture architecture, FacilityKind facilityKind)
    {
        OnStartBuildFacility?.Invoke(scenario, architecture, facilityKind);
    }

    public static void RaiseFacilityCompleted(GameScenario scenario, Architecture architecture, Facility facility)
    {
        OnFacilityCompleted?.Invoke(scenario, architecture, facility);
    }

    public static void RaiseFacilityDemolished(GameScenario scenario, Architecture architecture, Facility facility)
    {
        OnFacilityDemolished?.Invoke(scenario, architecture, facility);
    }

    public static void RaiseBuyFood(GameScenario scenario, Architecture architecture)
    {
        OnBuyFood?.Invoke(scenario, architecture);
    }

    public static void RaiseSellFood(GameScenario scenario, Architecture architecture)
    {
        OnSellFood?.Invoke(scenario, architecture);
    }

    public static void RaiseDisasterHappened(GameScenario scenario, Architecture architecture, int disasterType)
    {
        OnDisasterHappened?.Invoke(scenario, architecture, disasterType);
    }

    public static void RaiseCreateRobberTroop(GameScenario scenario, Architecture architecture, Troop troop)
    {
        OnCreateRobberTroop?.Invoke(scenario, architecture, troop);
    }

    public static void RaiseDisbandMilitary(GameScenario scenario, Architecture architecture, Military military)
    {
        OnDisbandMilitary?.Invoke(scenario, architecture, military);
    }

    public static void RaiseLevelUpMilitary(GameScenario scenario, Architecture architecture, Military military)
    {
        OnLevelUpMilitary?.Invoke(scenario, architecture, military);
    }

    public static void RaiseMilitaryCreate(GameScenario scenario, Architecture architecture, Military military)
    {
        OnMilitaryCreate?.Invoke(scenario, architecture, military);
    }

    public static void RaiseAIArchitecture(GameScenario scenario, Architecture architecture)
    {
        OnAIArchitecture?.Invoke(scenario, architecture);
    }
}
