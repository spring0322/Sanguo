using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 环境配置加载器（天气系统配置）
/// 日期：2026-03-09
/// </summary>
public static class EnvironmentConfigLoader
{
    private static GameEnvironmentConfig _cachedConfig;

    /// <summary>
    /// 加载环境配置（冷路径，允许使用异常处理）
    /// </summary>
    public static GameEnvironmentConfig LoadConfig(string configPath = "Content/Data/EnvironmentConfig.json")
    {
        // 如果已缓存，直接返回
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            // 读取 JSON 文件
            string jsonContent = File.ReadAllText(configPath);
            
            // 🔥 配置 JSON 选项：支持字符串形式的枚举值
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            
            // 🔥 添加字符串枚举转换器（支持 "Southern", "Spring" 等字符串形式）
            options.Converters.Add(new JsonStringEnumConverter());
            
            // 添加 AOT 序列化上下文
            options.TypeInfoResolverChain.Add(EnvironmentConfigContext.Default);
            
            // 反序列化（如果失败会抛出 JsonException）
            var config = JsonSerializer.Deserialize<GameEnvironmentConfig>(jsonContent, options)
                ?? throw new InvalidOperationException($"配置文件 {configPath} 反序列化返回 null");
            
            // 验证配置
            ConfigValidator.ValidateWeatherConfig(config.Weather);
            ConfigValidator.ValidateWeatherMovementConfig(config.WeatherMovement);
            
            // 缓存配置
            _cachedConfig = config;
            
            System.Diagnostics.Debug.WriteLine($"[EnvironmentConfigLoader] 配置加载成功: {config.Weather.ClimateRules.Count} 条天气规则");
            
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
    /// 清除缓存（用于热重载）
    /// </summary>
    public static void ClearCache()
    {
        _cachedConfig = null;
    }
}
