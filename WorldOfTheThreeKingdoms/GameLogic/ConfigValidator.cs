using System;
using System.IO;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 配置验证工具（在加载配置后立即调用）
/// 日期：2026-03-09
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// 验证天气配置的合法性
    /// </summary>
    public static void ValidateWeatherConfig(WeatherConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        if (config.ClimateRules == null)
        {
            throw new InvalidDataException("WeatherConfig.ClimateRules 不能为 null");
        }
        
        foreach (var rule in config.ClimateRules)
        {
            int total = rule.RainChance + rule.CloudyChance + rule.FogChance + rule.SnowChance;
            
            if (total > 100)
            {
                throw new InvalidDataException(
                    $"天气规则 [{rule.Zone}, {rule.Season}] 的概率总和为 {total}，超过 100！");
            }
            
            if (total < 0)
            {
                throw new InvalidDataException(
                    $"天气规则 [{rule.Zone}, {rule.Season}] 的概率总和为负数！");
            }
            
            // 检查单个概率是否在合理范围内
            if (rule.RainChance < 0 || rule.RainChance > 100 ||
                rule.CloudyChance < 0 || rule.CloudyChance > 100 ||
                rule.FogChance < 0 || rule.FogChance > 100 ||
                rule.SnowChance < 0 || rule.SnowChance > 100)
            {
                throw new InvalidDataException(
                    $"天气规则 [{rule.Zone}, {rule.Season}] 的概率值必须在 0-100 之间！");
            }
        }
        
        System.Diagnostics.Debug.WriteLine("[ConfigValidator] 天气配置验证通过。");
    }

    /// <summary>
    /// 验证移动惩罚配置的合法性
    /// </summary>
    public static void ValidateWeatherMovementConfig(WeatherMovementConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        if (config.Penalties == null)
        {
            throw new InvalidDataException("WeatherMovementConfig.Penalties 不能为 null");
        }
        
        foreach (var penalty in config.Penalties)
        {
            if (penalty.ExtraCost < 0)
            {
                throw new InvalidDataException(
                    $"移动惩罚 [{penalty.Weather}, {penalty.Terrain}, {penalty.Unit}] 的额外消耗不能为负数！");
            }
        }
        
        System.Diagnostics.Debug.WriteLine("[ConfigValidator] 移动惩罚配置验证通过。");
    }
}
