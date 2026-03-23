#nullable enable
using System;
using GameObjects;

namespace GameObjects.Events;

/// <summary>
/// 全局剧本事件中心 - AOT 兼容的强类型事件系统
/// 
/// 生命周期管理：
/// - 注册时机：GameScenario.Init() 或剧本加载完成后
/// - 注销时机：返回主菜单前调用 ClearAllHandlers()
/// - 跨剧本游玩：必须在切换剧本时清理，防止事件重复触发和内存泄漏
/// </summary>
public static class ScenarioEvents
{
    // ==================== 剧本生命周期事件 ====================
    
    /// <summary>剧本加载完成（替代 ExtensionInterface.call("Load")）</summary>
    public static event Action<GameScenario>? OnScenarioLoaded;
    
    /// <summary>每日开始前（替代 GameDate.OnDayStarting）</summary>
    public static event Func<GameScenario, bool>? OnDayStarting;
    
    /// <summary>每日事件（替代 ExtensionInterface.call("DayEvent")）</summary>
    public static event Action<GameScenario>? OnDayPassed;
    
    /// <summary>每日事件后处理（替代 ExtensionInterface.call("PostDayEvent")）</summary>
    public static event Action<GameScenario>? OnPostDayEvent;
    
    /// <summary>每月开始前（替代 GameDate.OnMonthStarting）</summary>
    public static event Func<GameScenario, bool>? OnMonthStarting;
    
    /// <summary>每月事件（替代 ExtensionInterface.call("MonthEvent")）</summary>
    public static event Action<GameScenario>? OnMonthPassed;
    
    /// <summary>每季事件（替代 ExtensionInterface.call("SeasonEvent")）</summary>
    public static event Action<GameScenario>? OnSeasonPassed;
    
    /// <summary>季节变化（替代 GameDate.OnSeasonChange）</summary>
    public static event Action<GameScenario, GameSeason>? OnSeasonChanged;
    
    /// <summary>每年开始前（替代 GameDate.OnYearStarting）</summary>
    public static event Func<GameScenario, bool>? OnYearStarting;
    
    /// <summary>每年事件（替代 ExtensionInterface.call("YearEvent")）</summary>
    public static event Action<GameScenario>? OnYearPassed;
    
    /// <summary>游戏结束（替代 ExtensionInterface.call("GameEnd")）</summary>
    public static event Action<GameScenario>? OnGameEnd;

    // ==================== 势力事件 ====================
    
    /// <summary>新势力创建（替代 ExtensionInterface.call("CreateNewFaction")）</summary>
    public static event Action<GameScenario, Faction, Faction, Architecture>? OnNewFactionCreated;
    
    // ==================== 剧本加载事件 ====================
    
    /// <summary>剧本加载后处理完成（替代 GameScenario.OnAfterLoadScenario）</summary>
    public static event Action<GameScenario>? OnAfterScenarioLoaded;
    
    /// <summary>剧本保存后处理完成（替代 GameScenario.OnAfterSaveScenario）</summary>
    public static event Action<GameScenario>? OnAfterScenarioSaved;
    
    // ==================== 武将生命周期事件 ====================
    
    /// <summary>武将出仕（替代 ExtensionInterface.call("PersonBecomeAvailable")）</summary>
    public static event Action<GameScenario, Person>? OnPersonBecomeAvailable;
    
    /// <summary>武将死亡（替代 ExtensionInterface.call("PersonDie")）</summary>
    public static event Action<GameScenario, Person>? OnPersonDied;
    
    /// <summary>子女加入势力（替代 ExtensionInterface.call("ChildrenJoinFaction")）</summary>
    public static event Action<GameScenario, Person>? OnChildrenJoinedFaction;

    // ==================== 武将事件触发器 ====================
    
    internal static void RaiseScenarioLoaded(GameScenario scenario) 
        => OnScenarioLoaded?.Invoke(scenario);
    
    internal static bool RaiseDayStarting(GameScenario scenario)
    {
        if (OnDayStarting == null) return true;
        
        // 🔥 调用所有订阅者，如果任何一个返回false，则返回false
        foreach (var handler in OnDayStarting.GetInvocationList())
        {
            if (handler is Func<GameScenario, bool> func && !func(scenario))
            {
                return false;
            }
        }
        return true;
    }
    
    internal static void RaiseDayPassed(GameScenario scenario) 
        => OnDayPassed?.Invoke(scenario);
    
    internal static void RaisePostDayEvent(GameScenario scenario) 
        => OnPostDayEvent?.Invoke(scenario);
    
    internal static bool RaiseMonthStarting(GameScenario scenario)
    {
        if (OnMonthStarting == null) return true;
        
        foreach (var handler in OnMonthStarting.GetInvocationList())
        {
            if (handler is Func<GameScenario, bool> func && !func(scenario))
            {
                return false;
            }
        }
        return true;
    }
    
    internal static void RaiseMonthPassed(GameScenario scenario) 
        => OnMonthPassed?.Invoke(scenario);
    
    internal static void RaiseSeasonPassed(GameScenario scenario) 
        => OnSeasonPassed?.Invoke(scenario);
    
    internal static void RaiseSeasonChanged(GameScenario scenario, GameSeason newSeason) 
        => OnSeasonChanged?.Invoke(scenario, newSeason);
    
    internal static bool RaiseYearStarting(GameScenario scenario)
    {
        if (OnYearStarting == null) return true;
        
        foreach (var handler in OnYearStarting.GetInvocationList())
        {
            if (handler is Func<GameScenario, bool> func && !func(scenario))
            {
                return false;
            }
        }
        return true;
    }
    
    internal static void RaiseYearPassed(GameScenario scenario) 
        => OnYearPassed?.Invoke(scenario);
    
    internal static void RaiseGameEnd(GameScenario scenario) 
        => OnGameEnd?.Invoke(scenario);
    
    internal static void RaiseNewFactionCreated(GameScenario scenario, Faction oldFaction, Faction newFaction, Architecture capital) 
        => OnNewFactionCreated?.Invoke(scenario, oldFaction, newFaction, capital);
    
    internal static void RaiseAfterScenarioLoaded(GameScenario scenario) 
        => OnAfterScenarioLoaded?.Invoke(scenario);
    
    internal static void RaiseAfterScenarioSaved(GameScenario scenario) 
        => OnAfterScenarioSaved?.Invoke(scenario);
    
    internal static void RaisePersonBecomeAvailable(GameScenario scenario, Person person) 
        => OnPersonBecomeAvailable?.Invoke(scenario, person);
    
    internal static void RaisePersonDied(GameScenario scenario, Person person) 
        => OnPersonDied?.Invoke(scenario, person);
    
    internal static void RaiseChildrenJoinedFaction(GameScenario scenario, Person child) 
        => OnChildrenJoinedFaction?.Invoke(scenario, child);

    /// <summary>
    /// 清理所有事件订阅（跨剧本游玩时必须调用）
    /// 调用时机：返回主菜单前、切换剧本前
    /// </summary>
    public static void ClearAllHandlers()
    {
        OnScenarioLoaded = null;
        OnDayStarting = null;
        OnDayPassed = null;
        OnPostDayEvent = null;
        OnMonthStarting = null;
        OnMonthPassed = null;
        OnSeasonPassed = null;
        OnSeasonChanged = null;
        OnYearStarting = null;
        OnYearPassed = null;
        OnGameEnd = null;
        OnNewFactionCreated = null;
        OnAfterScenarioLoaded = null;
        OnAfterScenarioSaved = null;
        OnPersonBecomeAvailable = null;
        OnPersonDied = null;
        OnChildrenJoinedFaction = null;
        
        // 同时清理武将事件
        PersonEvents.ClearAllHandlers();
    }
}
