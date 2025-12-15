using System;
using System.IO;
using Newtonsoft.Json;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// 缓存配置管理
    /// </summary>
    public class CacheConfig
    {
        /// <summary>
        /// 是否启用 LRU 缓存
        /// </summary>
        public bool EnableLRUCache { get; set; } = true;

        /// <summary>
        /// 最大显存预算（MB）
        /// </summary>
        public long MaxMemoryMB { get; set; } = 1024;

        /// <summary>
        /// 是否启用性能监控显示
        /// </summary>
        public bool ShowPerformanceMonitor { get; set; } = false;

        /// <summary>
        /// 自动清理阈值（当缓存项目数超过此值时触发清理）
        /// </summary>
        public int AutoCleanThreshold { get; set; } = 1000;

        /// <summary>
        /// 预加载常用纹理
        /// </summary>
        public bool PreloadCommonTextures { get; set; } = true;

        /// <summary>
        /// 纹理质量设置（0=低，1=中，2=高）
        /// </summary>
        public int TextureQuality { get; set; } = 1;

        private static CacheConfig _instance;
        private static readonly string ConfigPath = "cache_config.json";

        /// <summary>
        /// 单例实例
        /// </summary>
        public static CacheConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static CacheConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonConvert.DeserializeObject<CacheConfig>(json);
                    System.Diagnostics.Debug.WriteLine("[CacheConfig] 配置加载成功");
                    return config ?? new CacheConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheConfig] 加载配置失败: {ex.Message}");
            }

            // 返回默认配置
            var defaultConfig = new CacheConfig();
            defaultConfig.Save(); // 保存默认配置
            return defaultConfig;
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                System.Diagnostics.Debug.WriteLine("[CacheConfig] 配置保存成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheConfig] 保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据系统配置自动调整设置
        /// </summary>
        public void AutoAdjust()
        {
            try
            {
                // 根据系统内存调整显存预算
                var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
                
                if (totalMemory < 1024) // 小于 1GB 系统内存
                {
                    MaxMemoryMB = 256;
                    TextureQuality = 0; // 低质量
                    PreloadCommonTextures = false;
                }
                else if (totalMemory < 2048) // 1-2GB 系统内存
                {
                    MaxMemoryMB = 512;
                    TextureQuality = 1; // 中等质量
                    PreloadCommonTextures = true;
                }
                else if (totalMemory < 4096) // 2-4GB 系统内存
                {
                    MaxMemoryMB = 1024;
                    TextureQuality = 1; // 中等质量
                    PreloadCommonTextures = true;
                }
                else // 4GB+ 系统内存
                {
                    MaxMemoryMB = 2048;
                    TextureQuality = 2; // 高质量
                    PreloadCommonTextures = true;
                }

                System.Diagnostics.Debug.WriteLine($"[CacheConfig] 自动调整完成 - 内存预算: {MaxMemoryMB}MB, 质量: {TextureQuality}");
                Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheConfig] 自动调整失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public override string ToString()
        {
            return $"LRU: {(EnableLRUCache ? "开启" : "关闭")}, " +
                   $"内存: {MaxMemoryMB}MB, " +
                   $"质量: {TextureQuality}, " +
                   $"监控: {(ShowPerformanceMonitor ? "开启" : "关闭")}";
        }
    }
}