#nullable disable

using System;
using System.IO;
using System.Text.Json;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战术配置加载器 - AOT 兼容
/// 日期：2026-03-11
/// 
/// 🧊 COLD PATH：初始化阶段，可读性优先
/// </summary>
public static class CityTacticalConfigLoader
{
    private const string DefaultConfigPath = "Content/Data/CityTacticalConfig.json";
    private static CityTacticalConfig? _cachedConfig;

    /// <summary>
    /// 🧊 COLD PATH：加载配置文件
    /// </summary>
    public static CityTacticalConfig LoadConfig(string configPath = DefaultConfigPath)
    {
        // 如果已缓存，直接返回
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            if (!File.Exists(configPath))
            {
                System.Diagnostics.Debug.WriteLine($"[城市战术AI] 配置文件不存在: {configPath}，使用默认配置");
                _cachedConfig = new CityTacticalConfig();
                return _cachedConfig;
            }

            string jsonContent = File.ReadAllText(configPath);
            
            // 🔥 AOT 兼容：使用源生成器上下文
            _cachedConfig = JsonSerializer.Deserialize(
                jsonContent, 
                CityTacticalConfigContext.Default.CityTacticalConfig);

            if (_cachedConfig == null)
            {
                throw new InvalidOperationException($"配置文件反序列化失败: {configPath}");
            }

            System.Diagnostics.Debug.WriteLine($"[城市战术AI] 成功加载配置: {configPath}");
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[城市战术AI] 加载配置失败: {ex.Message}");
            
            // ANTI-BAND-AID：配置加载失败应该暴露问题，而不是静默回退
            throw new InvalidOperationException($"城市战术AI配置加载失败: {configPath}", ex);
        }
    }

    /// <summary>
    /// 清除缓存，强制重新加载
    /// </summary>
    public static void ClearCache()
    {
        _cachedConfig = null;
    }
}
