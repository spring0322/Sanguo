#nullable enable

using System.Diagnostics;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.UI;
using GameObjects.TroopDetail;
using GameManager;

namespace GameObjects;

/// <summary>
/// Troop 类的天气 UI 扩展方法
/// 日期：2026-03-12
/// 用途：为 Troop 提供天气相关的 UI 提示功能
/// </summary>
public static class TroopWeatherUIExtensions
{
    /// <summary>
    /// 获取计略的天气提示状态
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static StratagemWeatherState GetStratagemWeatherState(this Troop troop, Stratagem stratagem)
    {
        Debug.Assert(troop != null, "Troop 不应为 null");
        Debug.Assert(stratagem != null, "Stratagem 不应为 null");
        
        var weather = Session.Current.Scenario.WeatherManager.GetWeatherAt(troop.Position);
        return WeatherUIHelper.GetStratagemWeatherState(stratagem, weather);
    }
    
    /// <summary>
    /// 获取战法的天气提示状态
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static StratagemWeatherState GetCombatMethodWeatherState(this Troop troop, CombatMethod combatMethod)
    {
        Debug.Assert(troop != null, "Troop 不应为 null");
        Debug.Assert(combatMethod != null, "CombatMethod 不应为 null");
        
        var weather = Session.Current.Scenario.WeatherManager.GetWeatherAt(troop.Position);
        return WeatherUIHelper.GetCombatMethodWeatherState(combatMethod, weather);
    }
    
    /// <summary>
    /// 获取计略的天气提示文本（用于 FreeRichText）
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static string GetStratagemWeatherHint(this Troop troop, Stratagem stratagem)
    {
        var state = troop.GetStratagemWeatherState(stratagem);
        return state.Hint;
    }
    
    /// <summary>
    /// 获取战法的天气提示文本（用于 FreeRichText）
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static string GetCombatMethodWeatherHint(this Troop troop, CombatMethod combatMethod)
    {
        var state = troop.GetCombatMethodWeatherState(combatMethod);
        return state.Hint;
    }
    
    /// <summary>
    /// 获取计略的天气提示颜色（用于 FreeRichText）
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static Color GetStratagemWeatherColor(this Troop troop, Stratagem stratagem)
    {
        var state = troop.GetStratagemWeatherState(stratagem);
        return state.TextColor;
    }
    
    /// <summary>
    /// 获取战法的天气提示颜色（用于 FreeRichText）
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static Color GetCombatMethodWeatherColor(this Troop troop, CombatMethod combatMethod)
    {
        var state = troop.GetCombatMethodWeatherState(combatMethod);
        return state.TextColor;
    }
    
    /// <summary>
    /// 判断计略是否被天气禁用
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static bool IsStratagemBlockedByWeather(this Troop troop, Stratagem stratagem)
    {
        var state = troop.GetStratagemWeatherState(stratagem);
        return state.IsBlocked;
    }
    
    /// <summary>
    /// 判断战法是否被天气禁用
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static bool IsCombatMethodBlockedByWeather(this Troop troop, CombatMethod combatMethod)
    {
        var state = troop.GetCombatMethodWeatherState(combatMethod);
        return state.IsBlocked;
    }
}
