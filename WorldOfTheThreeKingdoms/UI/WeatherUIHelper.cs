#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.Influences;
using GameObjects.TroopDetail;
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.UI;

/// <summary>
/// 计略天气 UI 状态机（值类型，0 GC 分配）
/// 日期：2026-03-12
/// </summary>
public readonly record struct StratagemWeatherState(
    string Hint, 
    Color TextColor, 
    bool IsBlocked
);

/// <summary>
/// 天气 UI 提示辅助类
/// 日期：2026-03-12
/// 用途：为计略/战法选择界面提供天气影响提示
/// </summary>
public static class WeatherUIHelper
{
    // 🔥 优化：将 Color 实例化缓存为 readonly 字段，避免每次调用重复 new Color()
    private static readonly Color FireDisabledColor = new(255, 68, 68);  // #FF4444
    private static readonly Color WarningColor = new(255, 170, 0);       // #FFAA00
    private static readonly Color WaterEnhancedColor = new(68, 68, 255); // #4444FF
    private static readonly Color DefaultColor = Color.White;

    /// <summary>
    /// 一次性获取计略在当前天气下的所有 UI 状态
    /// 🔥 Cold Path：UI 事件触发，允许使用可读代码
    /// </summary>
    public static StratagemWeatherState GetStratagemWeatherState(
        Stratagem stratagem, 
        WeatherType weather)
    {
        // 🔥 ANTI-BAND-AID：Fail Fast，不使用防御性检查
        Debug.Assert(stratagem != null, "计略不应为 null");
        Debug.Assert(stratagem.Influences != null, "计略的 Influences 不应为 null");

        // 如果天气对水火都没有影响，直接返回默认状态
        if (!weather.SuppressFire() && !weather.EnhanceWater())
        {
            return new StratagemWeatherState(string.Empty, DefaultColor, false);
        }

        var config = Session.Current.Scenario.EnvironmentConfig.WeatherStratagem;
        Debug.Assert(config != null, "WeatherStratagem 配置不应为 null");

        var (hasFire, hasOther, hasWater, _) = AnalyzeInfluences(
            stratagem.Influences.Influences.Values, 
            config.FireInfluenceKindIDs, 
            config.WaterInfluenceKindIDs
        );

        // 🔥 优化：使用 C# 8+ 的元组模式匹配 (Tuple Pattern Matching)
        // 逻辑更紧凑，消除了冗长的 if-else 链
        return (hasFire, hasOther, hasWater) switch
        {
            // 纯火系：完全失效
            (true, false, false) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "❌ 雨天无法使用" : "❌ 雪天无法使用",
                FireDisabledColor,
                true
            ),

            // 混合类型：部分抑制
            (true, true, _) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "⚠️ 雨天效果减弱" : "⚠️ 雪天效果减弱",
                WarningColor,
                false
            ),

            // 纯水系：效果增强
            (false, false, true) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "✨ 雨天效果增强" : "✨ 雪天效果增强",
                WaterEnhancedColor,
                false
            ),

            // 默认兜底
            _ => new StratagemWeatherState(string.Empty, DefaultColor, false)
        };
    }

    /// <summary>
    /// 一次性获取战法在当前天气下的所有 UI 状态
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static StratagemWeatherState GetCombatMethodWeatherState(
        CombatMethod combatMethod, 
        WeatherType weather)
    {
        Debug.Assert(combatMethod != null, "战法不应为 null");
        Debug.Assert(combatMethod.Influences != null, "战法的 Influences 不应为 null");

        if (!weather.SuppressFire() && !weather.EnhanceWater())
        {
            return new StratagemWeatherState(string.Empty, DefaultColor, false);
        }

        var config = Session.Current.Scenario.EnvironmentConfig.WeatherStratagem;
        Debug.Assert(config != null, "WeatherStratagem 配置不应为 null");

        var (hasFire, hasOther, hasWater, _) = AnalyzeInfluences(
            combatMethod.Influences.Influences.Values, 
            config.FireInfluenceKindIDs, 
            config.WaterInfluenceKindIDs
        );

        return (hasFire, hasOther, hasWater) switch
        {
            (true, false, false) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "❌ 雨天无法使用" : "❌ 雪天无法使用",
                FireDisabledColor,
                true
            ),
            (true, true, _) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "⚠️ 雨天效果减弱" : "⚠️ 雪天效果减弱",
                WarningColor,
                false
            ),
            (false, false, true) => new StratagemWeatherState(
                weather == WeatherType.Rain ? "✨ 雨天效果增强" : "✨ 雪天效果增强",
                WaterEnhancedColor,
                false
            ),
            _ => new StratagemWeatherState(string.Empty, DefaultColor, false)
        };
    }

    /// <summary>
    /// 获取点火操作的天气提示文本（用于弹窗）
    /// 🔥 Cold Path：UI 事件触发
    /// </summary>
    public static string GetFireIgnitionBlockedMessage(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Rain => "⚠️ 无法点火\n\n当前天气：🌧️ 暴雨\n\n雨天无法点燃火焰\n请等待天气转晴后再尝试",
            WeatherType.Snow => "⚠️ 无法点火\n\n当前天气：❄️ 大雪\n\n雪天无法点燃火焰\n请等待天气转晴后再尝试",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 分析 Influence 组成
    /// 🔥 Cold Path：UI 事件触发，允许使用可读代码
    /// 🔥 关键：ID >= 0 是有效的（ID=0 可能是有效的 InfluenceKind）
    /// </summary>
    private static (bool hasFire, bool hasOther, bool hasWater, int totalCount) AnalyzeInfluences(
        IEnumerable<Influence> influences,
        HashSet<int> fireIDs,
        HashSet<int> waterIDs)
    {
        Debug.Assert(influences != null, "Influences 不应为 null");
        Debug.Assert(fireIDs != null, "FireInfluenceKindIDs 不应为 null");
        Debug.Assert(waterIDs != null, "WaterInfluenceKindIDs 不应为 null");

        bool hasFire = false;
        bool hasWater = false;
        bool hasOther = false;
        int count = 0;

        // 🔥 Cold Path：使用 foreach 提高可读性
        foreach (Influence influence in influences)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast，不检查 influence.Kind 是否为 null
            // 如果为 null，说明数据加载错误，应该在 Debug 模式下立即崩溃
            Debug.Assert(influence != null, "Influence 不应为 null");
            Debug.Assert(influence.Kind.ID >= 0, "InfluenceKind ID 应 >= 0（ID=0 是有效的）");

            count++;

            // 🔥 性能优化：HashSet.Contains 是 O(1)
            if (fireIDs.Contains(influence.Kind.ID))
            {
                hasFire = true;
            }
            else if (waterIDs.Contains(influence.Kind.ID))
            {
                hasWater = true;
            }
            else
            {
                hasOther = true;
            }
        }

        return (hasFire, hasOther, hasWater, count);
    }
}
