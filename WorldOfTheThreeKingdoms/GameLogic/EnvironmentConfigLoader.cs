using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 环境配置加载器。
/// </summary>
public static class EnvironmentConfigLoader
{
    private static GameEnvironmentConfig _cachedConfig;

    /// <summary>
    /// 加载环境配置。
    /// </summary>
    public static GameEnvironmentConfig LoadConfig(string configPath = "Content/Data/EnvironmentConfig.json")
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            string jsonContent = File.ReadAllText(configPath);
            GameEnvironmentConfig config = JsonSerializer.Deserialize(
                jsonContent,
                EnvironmentConfigContext.Default.GameEnvironmentConfig)
                ?? throw new InvalidOperationException($"配置文件 {configPath} 反序列化返回 null");

            ConfigValidator.ValidateWeatherConfig(config.Weather);
            ConfigValidator.ValidateWeatherMovementConfig(config.WeatherMovement);

            _cachedConfig = config;

            System.Diagnostics.Debug.WriteLine(
                $"[EnvironmentConfigLoader] 配置加载成功: {config.Weather.ClimateRules.Count} 条天气规则");

            return config;
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"环境配置文件未找到: {configPath}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"环境配置文件格式错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清除缓存。
    /// </summary>
    public static void ClearCache()
    {
        _cachedConfig = null;
    }
}
