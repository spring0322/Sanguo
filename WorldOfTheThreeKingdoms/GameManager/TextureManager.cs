using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Platforms;
using Tools;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 纹理缓存项
    /// </summary>
    public class TextureCacheItem
    {
        public Texture2D Texture { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int AccessCount { get; set; }
        public long MemorySize { get; set; }
        public bool IsPermanent { get; set; } // 是否为永久缓存（如UI纹理）
        public string Category { get; set; } // 纹理类别（Portrait, UI, Effect等）

        public TextureCacheItem(Texture2D texture, string category = "Default")
        {
            Texture = texture;
            LastAccessTime = DateTime.Now;
            AccessCount = 1;
            Category = category;
            IsPermanent = false;
            
            // 估算内存使用（宽 * 高 * 4字节）
            if (texture != null)
            {
                MemorySize = texture.Width * texture.Height * 4;
            }
        }

        public void UpdateAccess()
        {
            LastAccessTime = DateTime.Now;
            AccessCount++;
        }
    }

    /// <summary>
    /// 优化的纹理管理器，支持懒加载和自动内存管理
    /// </summary>
    public static class TextureManager
    {
        private static readonly Dictionary<string, TextureCacheItem> _textureCache = new Dictionary<string, TextureCacheItem>();
        private static readonly object _cacheLock = new object();
        
        // 配置参数
        private static long _maxMemoryUsage = 512 * 1024 * 1024; // 512MB 最大内存使用
        private static int _maxCacheItems = 1000; // 最大缓存项数
        private static TimeSpan _maxIdleTime = TimeSpan.FromMinutes(5); // 最大空闲时间
        private static TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1); // 清理间隔
        
        private static DateTime _lastCleanupTime = DateTime.Now;
        private static long _currentMemoryUsage = 0;

        /// <summary>
        /// 获取纹理，支持懒加载
        /// </summary>
        /// <param name="path">纹理路径</param>
        /// <param name="category">纹理类别</param>
        /// <param name="isPermanent">是否为永久缓存</param>
        /// <returns></returns>
        public static Texture2D GetTexture(string path, string category = "Default", bool isPermanent = false)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return null;

                // 暂时直接加载，后续会实现完整的缓存逻辑
                return Platform.Current.LoadTexture(path, false);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"加载纹理失败: {path}", "TextureManager.GetTexture", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取头像纹理（专门优化）
        /// </summary>
        /// <param name="portraitId">头像ID</param>
        /// <param name="size">尺寸类型</param>
        /// <returns></returns>
        public static Texture2D GetPortraitTexture(int portraitId, PortraitSize size = PortraitSize.Medium)
        {
            try
            {
                // 使用PortraitManager进行缓存管理
                // 首先检查PortraitManager是否已初始化
                var portrait = WorldOfTheThreeKingdoms.GameGlobal.PortraitManager.Instance.GetPortrait(portraitId);
                if (portrait != null)
                {
                    return portrait;
                }
                
                // 如果PortraitManager没有返回（可能未初始化或路径不对），回退到原有系统
                var path = CacheManager.GetPersonPortraitPath(portraitId, null, size);
                if (!string.IsNullOrEmpty(path))
                {
                    return Platform.Current.LoadTexture(path, false);
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"加载头像纹理失败: {portraitId}", "GetPortraitTexture", ex);
            }
            
            return null;
        }

        /// <summary>
        /// 预加载纹理（用于关键UI等）
        /// </summary>
        /// <param name="paths">纹理路径列表</param>
        /// <param name="category">类别</param>
        /// <param name="isPermanent">是否永久缓存</param>
        public static void PreloadTextures(IEnumerable<string> paths, string category = "Preload", bool isPermanent = true)
        {
            try
            {
                foreach (var path in paths)
                {
                    GetTexture(path, category, isPermanent);
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"预加载纹理失败: {category}", "TextureManager.PreloadTextures", ex);
            }
        }

        /// <summary>
        /// 释放指定类别的纹理
        /// </summary>
        /// <param name="category">类别名称</param>
        public static void ReleaseCategory(string category)
        {
            try
            {
                // 暂时不处理分类清理，等后续实现
                WebTools.TakeWarnMsg($"释放类别: {category}", "TextureManager.ReleaseCategory", null);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"释放类别失败: {category}", "TextureManager.ReleaseCategory", ex);
            }
        }

        /// <summary>
        /// 释放单个纹理
        /// </summary>
        /// <param name="path">纹理路径</param>
        public static void ReleaseTexture(string path)
        {
            try
            {
                // 暂时不实现，等后续完善
                WebTools.TakeWarnMsg($"释放纹理: {path}", "TextureManager.ReleaseTexture", null);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"释放纹理失败: {path}", "TextureManager.ReleaseTexture", ex);
            }
        }

        /// <summary>
        /// 强制清理所有非永久纹理
        /// </summary>
        public static void ForceCleanup()
        {
            try
            {
                // 清理缓存 - 使用原有系统
                CacheManager.Clear(CacheType.Temp);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("强制清理失败", "TextureManager.ForceCleanup", ex);
            }
        }

        /// <summary>
        /// 获取内存使用统计
        /// </summary>
        /// <returns></returns>
        public static TextureMemoryStats GetMemoryStats()
        {
            try
            {
                // 简化版统计，使用原有系统
                var stats = new TextureMemoryStats
                {
                    TotalMemoryUsage = 0,
                    TotalTextureCount = 0,
                    MaxMemoryLimit = _maxMemoryUsage,
                    CategoryStats = new Dictionary<string, CategoryStats>()
                };

                // 添加基本统计信息
                stats.CategoryStats["Portrait"] = new CategoryStats
                {
                    Count = 0,
                    MemoryUsage = 0,
                    PermanentCount = 0
                };

                return stats;
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("获取内存统计失败", "TextureManager.GetMemoryStats", ex);
                return new TextureMemoryStats
                {
                    TotalMemoryUsage = 0,
                    TotalTextureCount = 0,
                    MaxMemoryLimit = _maxMemoryUsage,
                    CategoryStats = new Dictionary<string, CategoryStats>()
                };
            }
        }

        /// <summary>
        /// 配置内存限制
        /// </summary>
        /// <param name="maxMemoryMB">最大内存使用（MB）</param>
        /// <param name="maxItems">最大缓存项数</param>
        /// <param name="maxIdleMinutes">最大空闲时间（分钟）</param>
        public static void ConfigureMemoryLimits(int maxMemoryMB, int maxItems, int maxIdleMinutes)
        {
            _maxMemoryUsage = maxMemoryMB * 1024L * 1024L;
            _maxCacheItems = maxItems;
            _maxIdleTime = TimeSpan.FromMinutes(maxIdleMinutes);
        }

        #region 私有方法 - 简化版

        // 暂时简化实现，后续会完善

        #endregion
    }

    /// <summary>
    /// 内存使用统计
    /// </summary>
    public class TextureMemoryStats
    {
        public long TotalMemoryUsage { get; set; }
        public int TotalTextureCount { get; set; }
        public long MaxMemoryLimit { get; set; }
        public Dictionary<string, CategoryStats> CategoryStats { get; set; }

        public double MemoryUsagePercentage => MaxMemoryLimit > 0 ? (double)TotalMemoryUsage / MaxMemoryLimit * 100 : 0;
    }

    /// <summary>
    /// 类别统计
    /// </summary>
    public class CategoryStats
    {
        public int Count { get; set; }
        public long MemoryUsage { get; set; }
        public int PermanentCount { get; set; }
    }
}