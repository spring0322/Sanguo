using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using GameManager;

namespace WorldOfTheThreeKingdoms.Tools
{
    /// <summary>
    /// 增强的纹理缓存管理器
    /// 提供引用计数、缓存统计和智能缓存管理功能
    /// </summary>
    public static class EnhancedCacheManager
    {
        private static readonly ConcurrentDictionary<string, CachedTexture> _textureCache = [];
        private static readonly object _cacheLock = new();
        
        /// <summary>
        /// 缓存的纹理信息
        /// </summary>
        private class CachedTexture
        {
            public Texture2D Texture { get; set; }
            public int ReferenceCount { get; set; }
            public DateTime LastAccessTime { get; set; }
            public long SizeInBytes { get; set; }
        }
        
        /// <summary>
        /// 获取纹理（带缓存优化）
        /// </summary>
        /// <param name="path">纹理路径，不能为空</param>
        /// <returns>纹理对象</returns>
        /// <exception cref="ArgumentException">路径为空时抛出</exception>
        /// <exception cref="FileNotFoundException">纹理文件不存在时抛出</exception>
        public static Texture2D GetTexture(string path)
        {
            // 🔥 Anti-Band-Aid: 空路径是调用者的错误，必须抛出异常
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("纹理路径不能为空", nameof(path));
            }
            
            // 尝试从缓存获取
            if (_textureCache.TryGetValue(path, out var cached))
            {
                cached.ReferenceCount++;
                cached.LastAccessTime = DateTime.Now;
                
                Debug.WriteLine($"[缓存命中] {path} (引用计数: {cached.ReferenceCount})");
                return cached.Texture;
            }
            
            // 缓存未命中，加载纹理
            Debug.WriteLine($"[缓存未命中] 加载纹理: {path}");
            
            // 🔥 Anti-Band-Aid: 不掩盖 CacheManager.LoadTexture 的失败
            // 如果返回 null，说明数据源有问题，让异常向上传播
            Texture2D texture = CacheManager.LoadTexture(path);
            
            // 🔥 验证纹理有效性（这不是防御性检查，而是数据验证）
            if (texture.IsDisposed)
            {
                throw new InvalidOperationException($"加载的纹理已被释放: {path}");
            }
            
            // 添加到缓存
            var cachedTexture = new CachedTexture
            {
                Texture = texture,
                ReferenceCount = 1,
                LastAccessTime = DateTime.Now,
                SizeInBytes = EstimateTextureSize(texture)
            };
            
            _textureCache[path] = cachedTexture;
            
            Debug.WriteLine($"[缓存添加] {path} (大小: {cachedTexture.SizeInBytes / 1024.0:F2} KB)");
            
            return texture;
        }
        
        /// <summary>
        /// 释放纹理引用
        /// </summary>
        /// <param name="path">纹理路径，不能为空</param>
        /// <exception cref="ArgumentException">路径为空时抛出</exception>
        public static void ReleaseTexture(string path)
        {
            // 🔥 Anti-Band-Aid: 空路径是调用者的错误，必须抛出异常
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("纹理路径不能为空", nameof(path));
            }
            
            if (_textureCache.TryGetValue(path, out var cached))
            {
                cached.ReferenceCount--;
                
                Debug.WriteLine($"[引用释放] {path} (引用计数: {cached.ReferenceCount})");
                
                // 引用计数为 0 时，可以考虑卸载（可选）
                if (cached.ReferenceCount <= 0)
                {
                    Debug.WriteLine($"[缓存] 纹理引用计数为 0: {path}");
                    // 暂不卸载，保留在缓存中以便后续使用
                }
            }
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// 🧊 Cold Path: 这是调试/统计功能，允许使用 LINQ
        /// </summary>
        /// <returns>缓存统计对象</returns>
        public static CacheStats GetStats()
        {
            lock (_cacheLock)
            {
                return new CacheStats
                {
                    TotalTextures = _textureCache.Count,
                    TotalSizeInBytes = _textureCache.Values.Sum(c => c.SizeInBytes),
                    TotalReferences = _textureCache.Values.Sum(c => c.ReferenceCount)
                };
            }
        }
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var cached in _textureCache.Values)
                {
                    cached.Texture?.Dispose();
                }
                _textureCache.Clear();
                Debug.WriteLine("[缓存] 已清除所有纹理缓存");
            }
        }
        
        /// <summary>
        /// 估算纹理大小
        /// </summary>
        /// <param name="texture">纹理对象，不能为 null</param>
        /// <returns>估算的字节大小</returns>
        private static long EstimateTextureSize(Texture2D texture)
        {
            // 🔥 Anti-Band-Aid: texture 为 null 是调用者的错误
            // 让 NullReferenceException 自然抛出，不掩盖问题
            
            // 简单估算：宽 * 高 * 4 字节（RGBA）
            return texture.Width * texture.Height * 4;
        }
        
        /// <summary>
        /// 缓存统计信息
        /// </summary>
        public class CacheStats
        {
            public int TotalTextures { get; set; }
            public long TotalSizeInBytes { get; set; }
            public int TotalReferences { get; set; }
            
            public double TotalSizeInMB => TotalSizeInBytes / (1024.0 * 1024.0);
            
            public override string ToString()
            {
                return $"纹理数量: {TotalTextures}, 总大小: {TotalSizeInMB:F2} MB, 总引用: {TotalReferences}";
            }
        }
    }
}
