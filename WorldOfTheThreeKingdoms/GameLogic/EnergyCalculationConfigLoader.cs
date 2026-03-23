#nullable disable

using System;
using System.IO;
using System.Text.Json;
using Zhsan.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 能量计算配置加载器
/// 日期：2026-03-16
/// 🧊 COLD PATH：游戏启动时加载
/// </summary>
public static class EnergyCalculationConfigLoader
{
    private const string DefaultConfigPath = "Content/Data/EnergyCalculationConfig.json";
    private static EnergyCalculationConfig? _cachedConfig;
    
    /// <summary>
    /// 加载能量计算配置（冷路径，允许使用异常处理）
    /// </summary>
    public static EnergyCalculationConfig LoadConfig(string configPath = DefaultConfigPath)
    {
        // 如果已缓存，直接返回
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }
        
        // 🔥 ANTI-BAND-AID：配置文件必须存在
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                $"能量计算配置文件不存在: {configPath}",
                configPath);
        }
        
        try
        {
            string jsonText = File.ReadAllText(configPath);
            
            // 🔥 AOT 兼容：使用 Source Generator 上下文
            // 日期：2026-03-17
            var config = JsonSerializer.Deserialize(
                jsonText, 
                WorldOfTheThreeKingdoms.GameData.EnergyCalculationConfigContext.Default.EnergyCalculationConfig);
            
            // 🔥 ANTI-BAND-AID：反序列化失败时 Fail Fast
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"能量计算配置反序列化失败: {configPath}");
            }
            
            // 验证配置有效性
            ValidateConfig(config);
            
            // 缓存配置
            _cachedConfig = config;
            
            System.Diagnostics.Debug.WriteLine(
                $"[EnergyCalculationConfigLoader] 配置加载成功: " +
                $"部队能量范围 {config.TroopEnergy.EnergyRange.Minimum}-{config.TroopEnergy.EnergyRange.Maximum}, " +
                $"城池能量范围 {config.ArchitectureEnergy.EnergyRange.Minimum}-{config.ArchitectureEnergy.EnergyRange.Maximum}");
            
            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"能量计算配置 JSON 格式错误: {configPath}",
                ex);
        }
    }
    
    /// <summary>
    /// 验证配置有效性
    /// </summary>
    private static void ValidateConfig(EnergyCalculationConfig config)
    {
        // 验证部队能量范围
        if (config.TroopEnergy.EnergyRange.Minimum >= config.TroopEnergy.EnergyRange.Maximum)
        {
            throw new InvalidOperationException(
                $"部队能量范围无效: Minimum={config.TroopEnergy.EnergyRange.Minimum}, " +
                $"Maximum={config.TroopEnergy.EnergyRange.Maximum}");
        }
        
        // 验证城池能量范围
        if (config.ArchitectureEnergy.EnergyRange.Minimum >= config.ArchitectureEnergy.EnergyRange.Maximum)
        {
            throw new InvalidOperationException(
                $"城池能量范围无效: Minimum={config.ArchitectureEnergy.EnergyRange.Minimum}, " +
                $"Maximum={config.ArchitectureEnergy.EnergyRange.Maximum}");
        }
        
        // 验证权重为正数
        if (config.TroopEnergy.QuantityWeight <= 0)
        {
            throw new InvalidOperationException(
                $"部队兵力权重必须为正数: {config.TroopEnergy.QuantityWeight}");
        }
        
        if (config.ArchitectureEnergy.PopulationWeight <= 0)
        {
            throw new InvalidOperationException(
                $"城池人口权重必须为正数: {config.ArchitectureEnergy.PopulationWeight}");
        }
    }
    
    /// <summary>
    /// 清除缓存（用于热更新）
    /// </summary>
    public static void ClearCache()
    {
        _cachedConfig = null;
    }
}
