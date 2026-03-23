using FontStashSharp;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tools;
using WorldOfTheThreeKingdoms.Helpers;
namespace GameManager
{
    public class PlatformTexture
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
        
        // 🔥 添加 Texture 属性以持有实际的 Texture2D 对象
        public Texture2D Texture { get; set; }

        public PlatformTexture() { }

        public PlatformTexture(Texture2D texture)
        {
            this.Texture = texture;
            if (texture != null)
            {
                this.Width = texture.Width;
                this.Height = texture.Height;
                // 🔥 修复：从 Texture2D.Name 提取文件名作为 PlatformTexture.Name
                // 如果 Texture2D.Name 为空，使用占位符避免 CacheManager 跳过绘制
                this.Name = !string.IsNullOrEmpty(texture.Name) ? texture.Name : $"Texture_{texture.GetHashCode()}";
            }
        }
    }

    public struct PlatformColor
    {
        public static Color DarkRed = new Color(139, 0, 0); //; new Color(179, 53, 26);
        public static Color DarkRed2 = new Color(204, 0, 0); //; new Color(179, 53, 26);
        public static Color DarkBlue = new Color(18, 58, 83); //; new Color(179, 53, 26);
        public static Color DarkGreen = new Color(64, 71, 44);
        public static Color DarkGreen2 = new Color(24, 52, 36);
        public static Color DarkOrange = new Color(149, 80, 30);
        public static Color WhiteSilver = new Color(238, 231, 205);
        public static Color ShadowBlack = new Color(29, 29, 13);
        public static Color ShadowBlack2 = new Color(112, 114, 113);
        public static Color Orange = new Color(170, 92, 26);
        public static Color DarkCyan = new Color(62, 126, 144);
        public static Color CyanWhite = new Color(204, 204, 0);
        public static Color PinkRed = new Color(217, 51, 51);
        public static Color ShadowYellow = new Color(239, 224, 175);
    }

    public enum TextureShape
    {
        None,
        Circle,
        Diamond,
        Ellipse
    }

    public enum CacheType
    {
        Live,
        Scene,
        Page,
        Temp,
        Normal,
        None
    }

    public static class CacheManager
    {
        public static object CacheLock = new Object();

        // 保留旧的字典作为后备（用于临时纹理和兼容性）
        public static Dictionary<string, Texture2D> TextureDics = [];
        public static Dictionary<string, Texture2D> TextureTempDics = [];

        // LRU 缓存系统
        private static LRUTextureCache _lruCache;
        private static bool _useLRUCache = true;  // 默认启用LRU缓存
        private static long _maxMemoryMB = 512;  // 降低到512MB显存预算，更积极地清理

        // 回退纹理和设备丢失处理
        public static Texture2D _fallbackTexture;
        private static bool _isDeviceLost = false;

        // === GPU 设备丢失处理 ===
        /// <summary>
        /// GPU设备丢失状态（供外部检查）
        /// </summary>
        public static bool IsDeviceLost => _isDeviceLost;

        public static void MarkDeviceLost(Exception ex)
        {
            if (!_isDeviceLost)
            {
                _isDeviceLost = true;
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Device Lost Detected: {ex.Message}");
                Reset();
            }
        }

        public static bool TryRecoverDevice()
        {
             return !_isDeviceLost; 
        }

        /// <summary>
        /// 重置缓存管理器
        /// </summary>
        public static void Reset()
        {
            lock(CacheLock)
            {
                // 清理所有纹理资源
                if (TextureDics != null)
                {
                    try
                    {
                        foreach (var tex in TextureDics.Values)
                        {
                            if (tex != null && !tex.IsDisposed)
                                tex.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    TextureDics.Clear();
                }

                if (TextureTempDics != null)
                {
                    try
                    {
                        foreach (var tex in TextureTempDics.Values)
                        {
                            if (tex != null && !tex.IsDisposed)
                                tex.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    TextureTempDics.Clear();
                }

                // 同样需要清理回退纹理，因为它绑定在旧的设备上
                if (_fallbackTexture != null)
                {
                    if (!_fallbackTexture.IsDisposed)
                    {
                        try { _fallbackTexture.Dispose(); } catch { }
                    }
                    _fallbackTexture = null;
                }

                if (_lruCache != null && _useLRUCache)
                {
                    _lruCache.Clear();
                }
                
                _loadsSinceLastCheck = 0;
            }

        }

        /// <summary>
        /// 安全验证纹理状态
        /// </summary>
        private static bool IsTextureValid(Texture2D texture)
        {
            try
            {
                return texture != null && !texture.IsDisposed && texture.Width > 0 && texture.Height > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全处理纹理异常并尝试恢复
        /// </summary>
        private static void HandleTextureException(string textureName, Exception ex)
        {


            // 根据异常类型采取不同的恢复策略
            if (ex is InvalidOperationException)
            {

            }
            else if (ex is ObjectDisposedException)
            {

                
                // 尝试从缓存中移除已释放的纹理
                try
                {
                    lock (TextureDics)
                    {
                        if (TextureDics.ContainsKey(textureName))
                        {
                            TextureDics.Remove(textureName);

                        }
                    }
                    
                    lock (TextureTempDics)
                    {
                        if (TextureTempDics.ContainsKey(textureName))
                        {
                            TextureTempDics.Remove(textureName);

                        }
                    }
                }
                catch (Exception cleanupEx)
                {

                }
            }
        }

        /// <summary>
        /// 智能纹理路径解析 - 处理tupianwenzi系统的特殊路径需求
        /// </summary>
        private static string ResolveTexturePath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return originalPath;

            // 检查是否是tupianwenzi系统尝试加载人物头像
            if (originalPath.Contains(@"tupianwenzi\Data\tupian\") || originalPath.Contains("tupianwenzi/Data/tupian/"))
            {
                // 提取文件名（如7001.jpg）
                string fileName = System.IO.Path.GetFileName(originalPath);
                
                // 检查是否是数字开头的文件名（人物头像）
                if (!string.IsNullOrEmpty(fileName) && char.IsDigit(fileName[0]))
                {
                    // 尝试解析为人物ID
                    string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    if (int.TryParse(nameWithoutExt, out int portraitId))
                    {
                        // 构建新的头像路径基础
                        var portraitPack = Setting.Current.PortraitPack;
                        var defaultDir = String.IsNullOrWhiteSpace(portraitPack) ? @"Content/Textures/GameComponents/PersonPortrait/Images/Default/"
                                                                                 : $"Portraits/{portraitPack}/";
                        
                        var basePath = $"{defaultDir}{portraitId}";
                        
                        // 按优先级尝试不同格式：DDS > PNG > JPG
                        var extensions = new[] { ".dds", ".png", ".jpg" };
                        
                        foreach (var ext in extensions)
                        {
                            var testPath = basePath + ext;
                            if (Platform.Current.FileExists(testPath))
                            {
                                string originalExtension = System.IO.Path.GetExtension(originalPath).ToUpper();
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 重定向tupianwenzi路径: {originalPath} -> {testPath}");
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 格式优化: {originalExtension} -> {ext.ToUpper()} (优先级: DDS > PNG > JPG)");
                                return testPath;
                            }
                        }
                        
                        // 如果默认目录没找到，尝试自定义目录
                        var customDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Player/";
                        var customBasePath = $"{customDir}{portraitId}";
                        
                        foreach (var ext in extensions)
                        {
                            var testPath = customBasePath + ext;
                            if (Platform.Current.FileExists(testPath))
                            {
                                string originalExtension = System.IO.Path.GetExtension(originalPath).ToUpper();
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 重定向tupianwenzi路径(自定义): {originalPath} -> {testPath}");
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 格式优化: {originalExtension} -> {ext.ToUpper()} (优先级: DDS > PNG > JPG)");
                                return testPath;
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] tupianwenzi重定向失败: 未找到头像ID {portraitId} 的任何格式文件");
                    }
                }
            }

            return originalPath;
        }

        /// <summary>
        /// 强制重新加载指定纹理
        /// </summary>
        public static bool ForceReloadTexture(string textureName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 强制重新加载纹理: {textureName}");
                
                // 清理现有缓存
                Remove(textureName);
                RemoveTempDics(textureName);
                
                // 重新加载
                var texture = LoadTexture(textureName);
                bool success = IsTextureValid(texture);
                
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 强制重新加载结果: {textureName} = {success}");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 强制重新加载失败: {textureName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据缓存类型移除指定纹理
        /// </summary>
        /// <param name="textureName">纹理名称</param>
        /// <param name="cacheType">缓存类型</param>
        public static void RemoveTexture(string textureName, CacheType cacheType)
        {
            if (string.IsNullOrEmpty(textureName))
                return;

            try
            {
                lock (CacheLock)
                {
                    switch (cacheType)
                    {
                        case CacheType.Live:
                            // 从主纹理字典中移除
                            if (TextureDics.ContainsKey(textureName))
                            {
                                var texture = TextureDics[textureName];
                                if (texture != null && !texture.IsDisposed)
                                {
                                    texture.Dispose();
                                }
                                TextureDics.Remove(textureName);
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 已从Live缓存移除纹理: {textureName}");
                            }
                            break;

                        case CacheType.Temp:
                            // 从临时纹理字典中移除
                            if (TextureTempDics.ContainsKey(textureName))
                            {
                                var texture = TextureTempDics[textureName];
                                if (texture != null && !texture.IsDisposed)
                                {
                                    texture.Dispose();
                                }
                                TextureTempDics.Remove(textureName);
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 已从Temp缓存移除纹理: {textureName}");
                            }
                            break;

                        case CacheType.Scene:
                        case CacheType.Page:
                            // 对于其他类型，检查纹理记录并移除
                            var matchingKeys = TextureDics.Keys.Where(key => 
                            {
                                var rec = Session.TextureRecs.FirstOrDefault(te => te.Key.Split('#')[0] == key).Value;
                                return rec.CacheType == cacheType.ToString();
                            }).ToList();

                            foreach (var key in matchingKeys)
                            {
                                if (key.Contains(textureName))
                                {
                                    var texture = TextureDics[key];
                                    if (texture != null && !texture.IsDisposed)
                                    {
                                        texture.Dispose();
                                    }
                                    TextureDics.Remove(key);
                                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 已从{cacheType}缓存移除纹理: {key}");
                                }
                            }
                            break;

                        default:
                            // 默认情况下，尝试从所有缓存中移除
                            RemoveTexture(textureName, CacheType.Live);
                            RemoveTexture(textureName, CacheType.Temp);
                            break;
                    }

                    // 如果使用LRU缓存，也从LRU缓存中移除
                    if (_lruCache != null && _useLRUCache)
                    {
                        // 注意：LRUTextureCache可能没有Remove方法，这里先注释掉
                        // 如果需要从LRU缓存中移除，可以考虑其他方式
                        // _lruCache.Remove(textureName);
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] LRU缓存暂不支持单独移除纹理: {textureName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] RemoveTexture失败: {textureName}, 类型: {cacheType}, 错误: {ex.Message}");
            }
        }


        // 内存管理参数（用于临时纹理的后备清理）
        // 🔥 性能修复：提高阈值，避免在初始化阶段过早清理
        // 初始化阶段会加载 ~500 个纹理（插件51 + 建筑216 + 其他~233）
        private static readonly int MaxTextureCount = 1000;  // 最大纹理数量（提高到1000）
        private static readonly int CleanupThreshold = 800;  // 触发清理的阈值（提高到800）
        private static int _loadsSinceLastCheck = 0;
        private static readonly int CheckInterval = 100;     // 每加载100个纹理检查一次（降低检查频率）

        public static Dictionary<string, string> DicTexts = new Dictionary<string, string>();

        public static Vector2 Scale = Vector2.One;

        public static FontPair FontPair = new FontPair()
        {
            Name = @"Content\Font\FZLB_GBK.TTF",
            Size = 30,
            Style = "",
            Width = 30,
            Height = 32
        };

        /// <summary>
        /// 初始化 LRU 缓存系统
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="maxMemoryMB">最大显存预算（MB）</param>
        public static void InitializeLRUCache(GraphicsDevice graphicsDevice, long maxMemoryMB = 1024)
        {
            // 🔥 技术性修复：防御式检查 GraphicsDevice
            if (graphicsDevice == null || graphicsDevice.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[CacheManager] InitializeLRUCache: GraphicsDevice 为 null 或已释放，保持设备丢失状态");
                _isDeviceLost = true;
                return;
            }

            // 设备已重置或初始化，清除丢失标志
            _isDeviceLost = false;
            
            try
            {
                _maxMemoryMB = maxMemoryMB;
                _lruCache = new LRUTextureCache(graphicsDevice, maxMemoryMB);
                _useLRUCache = true;
                
                System.Diagnostics.Debug.WriteLine($"[CacheManager] LRU缓存已初始化 - 最大显存: {maxMemoryMB} MB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] LRU缓存初始化失败: {ex.Message}，使用传统缓存");
                _useLRUCache = false;
            }

            // 初始化回退纹理 (1x1 白色像素)
            try
            {
                if (_fallbackTexture == null || _fallbackTexture.IsDisposed)
                {
                    _fallbackTexture = new Texture2D(graphicsDevice, 1, 1);
                    _fallbackTexture.SetData(new Color[] { Color.White });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 无法创建回退纹理: {ex.Message}");
            }
        }


        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static string GetCacheStats()
        {
            if (_lruCache != null && _useLRUCache)
            {
                return _lruCache.GetStats().ToString();
            }
            return $"Legacy Cache - Items: {TextureDics.Count + TextureTempDics.Count}";
        }


        /// <summary>
        /// 检查并清理内存，防止OutOfMemoryException
        /// </summary>
        private static void CheckAndCleanupMemory()
        {
            _loadsSinceLastCheck++;
            
            // 每隔一定次数检查一次
            if (_loadsSinceLastCheck < CheckInterval)
                return;
            
            _loadsSinceLastCheck = 0;
            
            int totalCount = TextureDics.Count + TextureTempDics.Count;
            
            if (totalCount > CleanupThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理数量({totalCount})超过阈值({CleanupThreshold})，开始自动清理...");
                
                try
                {
                    // 🔥 性能修复：不要清空所有临时纹理，只清理已释放的
                    lock (TextureTempDics)
                    {
                        int tempCount = TextureTempDics.Count;
                        int cleanedCount = 0;
                        
                        // 🔥 使用 for 循环代替 LINQ（可能在 Hot Path 中调用）
                        List<string> keysToRemove = [];
                        foreach (var kvp in TextureTempDics)
                        {
                            if (kvp.Value == null || kvp.Value.IsDisposed)
                            {
                                keysToRemove.Add(kvp.Key);
                            }
                        }
                        
                        foreach (var key in keysToRemove)
                        {
                            TextureTempDics.Remove(key);
                            cleanedCount++;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 已清理 {cleanedCount}/{tempCount} 个无效临时纹理");
                    }
                    
                    // 如果仍然超过最大值，清理一部分永久纹理
                    if (TextureDics.Count > MaxTextureCount)
                    {
                        lock (TextureDics)
                        {
                            // 🔥 使用 for 循环代替 LINQ
                            List<string> keysToRemove = [];
                            foreach (var kvp in TextureDics)
                            {
                                if (kvp.Value == null || kvp.Value.IsDisposed)
                                {
                                    keysToRemove.Add(kvp.Key);
                                }
                            }
                            
                            foreach (var key in keysToRemove)
                            {
                                TextureDics.Remove(key);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[CacheManager] 已清理 {keysToRemove.Count} 个无效纹理引用");
                            
                            // 如果还是太多，强制释放一些纹理（保留最近的）
                            int excessCount = TextureDics.Count - MaxTextureCount;
                            if (excessCount > 0)
                            {
                                // 🔥 使用 for 循环代替 LINQ
                                List<string> keysToEvict = [];
                                int count = 0;
                                foreach (var key in TextureDics.Keys)
                                {
                                    if (count >= excessCount) break;
                                    keysToEvict.Add(key);
                                    count++;
                                }
                                
                                foreach (var key in keysToEvict)
                                {
                                    var tex = TextureDics[key];
                                    if (tex != null && !tex.IsDisposed)
                                    {
                                        tex.Dispose();
                                    }
                                    TextureDics.Remove(key);
                                }
                                System.Diagnostics.Debug.WriteLine($"[CacheManager] 已强制释放 {keysToEvict.Count} 个纹理");
                            }
                        }
                    }
                    
                    // 尝试触发垃圾回收
                    GC.Collect(0, GCCollectionMode.Optimized);
                    
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 清理完成，当前纹理数量: {TextureDics.Count + TextureTempDics.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 清理过程发生异常: {ex.Message}");
                }
            }
        }

        public static void Clear(CacheType type)
        {

            lock (CacheLock)
            {
                if (type != CacheType.Temp && DicTexts != null)
                {
                    lock (DicTexts)
                    {
                        DicTexts.Clear();
                    }
                }

                // 如果使用 LRU 缓存，根据类型进行智能清理
                if (_lruCache != null && _useLRUCache)
                {
                    switch (type)
                    {
                        case CacheType.Live:
                            _lruCache.Clear(); // 清空所有缓存
                            break;
                        case CacheType.Scene:
                        case CacheType.Page:
                            _lruCache.ForceClean(20); // 清理一些最久未使用的纹理
                            break;
                        case CacheType.Temp:
                            _lruCache.ForceClean(10); // 轻度清理
                            break;
                    }
                }

                // 🔥 关键修复：清理临时纹理时保护正在使用的动画纹理
                // 日期：2026-03-10
                // 问题：战法动画纹理被错误释放（IsDisposed=True），导致动画不显示
                // 原因：Clear(CacheType.Temp) 会释放所有临时纹理，包括正在播放的战法动画纹理
                // 🔥 关键修复：清理临时纹理时保护正在使用的动画纹理
                // 日期：2026-03-10
                // 问题：战法动画纹理被错误释放（IsDisposed=True），导致动画不显示
                // 原因：Clear(CacheType.Temp) 会释放所有临时纹理，包括正在播放的战法动画纹理
                // 解决：在清理前收集正在使用的动画纹理，避免释放它们
                
                // 🔥 C# 12 集合表达式：冷路径代码，优先可读性
                List<Texture2D> protectedTextures = [];
                
                // 收集正在播放的战法动画纹理
                try
                {
                    if (Session.Current?.Scenario?.GeneratorOfTileAnimation?.TileAnimations != null)
                    {
                        // 🔥 性能优化：直接遍历字典，避免 .Values 的 LINQ 调用
                        foreach (var kvp in Session.Current.Scenario.GeneratorOfTileAnimation.TileAnimations)
                        {
                            var animation = kvp.Value;
                            if (animation?.Drawing == true && 
                                animation.LinkedAnimation?.Texture?.Texture != null &&
                                !animation.LinkedAnimation.Texture.Texture.IsDisposed)
                            {
                                protectedTextures.Add(animation.LinkedAnimation.Texture.Texture);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager.Clear] 收集保护纹理时发生异常: {ex.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[CacheManager.Clear] 保护 {protectedTextures.Count} 个正在使用的动画纹理");
                
                // 安全清理临时纹理（跳过受保护的纹理）
                int disposedCount = 0;
                int protectedSkipCount = 0;
                
                foreach (var tex in TextureTempDics)
                {
                    if (tex.Value != null && !tex.Value.IsDisposed)
                    {
                        // 🔥 性能优化：使用 for 循环和 ReferenceEquals 进行快速查找
                        bool isProtected = false;
                        for (int i = 0; i < protectedTextures.Count; i++)
                        {
                            if (ReferenceEquals(protectedTextures[i], tex.Value))
                            {
                                isProtected = true;
                                break;
                            }
                        }
                        
                        if (isProtected)
                        {
                            protectedSkipCount++;
                            System.Diagnostics.Debug.WriteLine($"[CacheManager.Clear] 跳过受保护的动画纹理: {tex.Key}");
                        }
                        else
                        {
                            tex.Value.Dispose();
                            disposedCount++;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[CacheManager.Clear] 临时纹理清理完成 - 释放: {disposedCount}, 保护: {protectedSkipCount}");
                
                // 🔥 C# 12 集合表达式：收集要移除的键
                List<string> tempKeysToRemove = [];
                
                foreach (var kvp in TextureTempDics)
                {
                    if (kvp.Value == null || kvp.Value.IsDisposed)
                    {
                        tempKeysToRemove.Add(kvp.Key);
                    }
                }
                
                for (int i = 0; i < tempKeysToRemove.Count; i++)
                {
                    TextureTempDics.Remove(tempKeysToRemove[i]);
                }

                // ✅ 性能优化：预先构建查找字典，避免 O(n*m) 嵌套循环
                // 阶段 1：构建查找字典（O(m)）
                Dictionary<string, string> recLookup = [];
                foreach (var recKvp in Session.TextureRecs)
                {
                    string key = recKvp.Key.Split('#')[0];
                    recLookup[key] = recKvp.Value.CacheType;
                }
                
                // 阶段 2：收集需要删除的键（O(n)）
                List<string> keysToRemove = [];
                
                foreach (var kvp in TextureDics)
                {
                    bool shouldRemove = false;
                    
                    if (recLookup.TryGetValue(kvp.Key, out string recCacheType))
                    {
                        // 根据缓存类型判断是否需要移除
                        shouldRemove = type switch
                        {
                            CacheType.Live => recCacheType == "Live" || recCacheType == "Scene" || recCacheType == "Page" || recCacheType == "Temp",
                            CacheType.Scene => recCacheType == "Scene" || recCacheType == "Page" || recCacheType == "Temp",
                            CacheType.Page => recCacheType == "Page" || recCacheType == "Temp",
                            CacheType.Temp => recCacheType == "Temp",
                            _ => false
                        };
                    }
                    else
                    {
                        // 去除空或已经失效的材质
                        if (kvp.Value == null || kvp.Value.IsDisposed)
                        {
                            shouldRemove = true;
                        }
                    }
                    
                    if (shouldRemove)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                // 阶段 3：批量删除（O(k)）
                foreach (string key in keysToRemove)
                {
                    if (TextureDics.TryGetValue(key, out Texture2D tex))
                    {
                        if (tex != null && !tex.IsDisposed)
                        {
                            tex.Dispose();
                        }
                        TextureDics.Remove(key);
                    }
                }
            }
            if (type == CacheType.Page || type == CacheType.Scene)
            {
                try
                {
                    Session.Current.SoundContent.Unload();
                    //Session.Current.MusicContent.Unload();
                    Session.Current.Content.Unload();
                }
                catch (Exception ex)
                {
                    WebTools.TakeWarnMsg("清空声音缓存失败:" + type, "SoundContent.Unload:", ex);
                }
            }
        }

        public static void RemoveTempDics(string key)
        {
            if (!String.IsNullOrEmpty(key))
            {
                lock (CacheLock)
                {
                    if (TextureTempDics.ContainsKey(key))
                    {
                        Texture2D tex = TextureTempDics[key];
                        TextureTempDics.Remove(key);
                        if (tex != null && tex.IsDisposed)
                        {
                            tex.Dispose();
                        }
                        tex = null;
                    }
                }
            }
        }

        public static void Remove(string key)
        {
            if (!String.IsNullOrEmpty(key))
            {
                lock (CacheLock)
                {
                    if (TextureDics.ContainsKey(key))
                    {
                        Texture2D tex = TextureDics[key];
                        TextureDics.Remove(key);
                        if (tex != null && tex.IsDisposed)
                        {
                            tex.Dispose();
                        }
                        tex = null;
                    }
                }
            }
        }

        //static void Remove(Texture2D tex)
        //{
        //    if (tex != null)
        //    {
        //        if (TextureDics.ContainsValue(tex))
        //        {
        //            var pa = (from pair in TextureDics where pair.Value == tex select pair).FirstOrDefault();
        //            TextureDics.Remove(pa.Key);
        //        }
        //        if (!tex.IsDisposed)
        //        {
        //            tex.Dispose();
        //        }
        //        tex = null;
        //    }
        //}

        /// <summary>
        /// 🔥 性能优化：批量预加载纹理到缓存（顺序加载，主线程安全）
        /// </summary>
        /// <param name="texturePaths">纹理路径列表（不能为 null）</param>
        /// <param name="isTemp">是否加载到临时缓存</param>
        public static void PreloadTextures(IEnumerable<string> texturePaths, bool isTemp = true)
        {
            // 🔥 不添加空检查：如果调用方传入 null，应该让它崩溃，暴露调用错误
            // 调用方（GamePlugin.InitializePlugins）传入的是静态只读数组，永远不会为 null
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var paths = texturePaths.ToList();
            int successCount = 0;
            int failCount = 0;
            
            System.Diagnostics.Debug.WriteLine($"[CacheManager] 开始批量预加载 {paths.Count} 个纹理...");
            
            // 🔥 顺序加载（MonoGame 要求纹理加载必须在主线程）
            foreach (var path in paths)
            {
                try
                {
                    // 预加载到缓存（会触发文件读取和解码）
                    var tex = LoadTexture(path, false, isTemp, TextureShape.None, null);
                    if (tex != null && !tex.IsDisposed)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 预加载失败: {path}, 错误: {ex.Message}");
                    failCount++;
                }
            }
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[CacheManager] 批量预加载完成: 成功 {successCount}, 失败 {failCount}, 耗时 {sw.ElapsedMilliseconds} ms");
        }

        public static PlatformTexture GetTempTexture(string name)
        {
            // 立即尝试加载到临时缓存
            Texture2D tex = LoadTexture(name, false, true, TextureShape.None, null);
            
            // 🔥 关键修复：如果加载失败（返回null或回退纹理），则返回null
            // 这样UI层 (tupianwenziPlugin) 可以知道图片缺失，从而不绘制任何东西，而不是绘制一个拉伸的白框
            if (tex == null || (_fallbackTexture != null && tex == _fallbackTexture))
            {
                return null;
            }
            
            return new PlatformTexture()
            {
                Name = name,
                Width = tex.Width,
                Height = tex.Height,
                Texture = tex
            };
        }

        //public static Texture2D LoadTempTexture(string name)
        //{
        //    return Platform.Current.LoadTexture(name, false);
        //}

        public static Texture2D LoadAvatar(string name, bool isUser, bool isTemp, TextureShape shape, float[] shapeParms)
        {
            return LoadTexture(name, isUser, isTemp, shape, shapeParms);
        }

        public static Texture2D LoadTexture(string name)
        {
            return LoadTexture(name, false, false, TextureShape.None, null);
        }

        static Texture2D LoadTexture(string name, bool isUser, bool isTemp, TextureShape shape, float[] shapeParms)
        {
            // 如果设备已丢失，直接返回null，不尝试加载，防止进一步崩溃
            if (_isDeviceLost) return null;

            // 🔥 技术性修复：如果在AI计算过程中（多线程环境），禁止加载纹理
            // 这能防止 VertexBuffer NRE 和 InvalidOperationException (Image format not supported)
            if (Session.Current.IsWorking)
            {
                // System.Diagnostics.Debug.WriteLine($"[CacheManager] AI运行时跳过纹理加载: {name}");
                return null;
            }

            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {

                    return null;
                }

                // 如果启用了 LRU 缓存且不是临时纹理，使用 LRU 缓存
                if (_lruCache != null && _useLRUCache && !isTemp)
                {
                    string res = PrepareTexturePath(name, isUser);
                    Texture2D tex = _lruCache.Get(res);
                    
                    if (tex != null)
                    {
                        GameTools.ProcessTextureShape(tex, shape, shapeParms);
                        return tex;
                    }
                }

                // 回退到原有的缓存逻辑（用于临时纹理或禁用 LRU 时）
                var dics = isTemp ? TextureTempDics : TextureDics;
                Texture2D legacyTex = null;
                bool reload = false;

                if (dics == null) return null;

                lock (dics)
                {
                    if (dics != null && (!dics.TryGetValue(name, out legacyTex) || legacyTex != null && legacyTex.IsDisposed))
                    {
                        reload = true;
                        if (legacyTex != null) dics.Remove(name);

                        // 即将加载新纹理，检查是否需要清理内存
                        CheckAndCleanupMemory();
                    }
                }

                if (reload)
                {
                    string res = PrepareTexturePath(name, isUser);
                    legacyTex = Platform.Current.LoadTexture(res, isUser);

                    // 🔥 技术性修复：重新检查设备状态
                    // 如果 LoadTexture 内部触发了设备丢失，这里应该立即感知并停止
                    if (_isDeviceLost) return null;

                    // 验证加载的纹理
                    if (legacyTex != null && !legacyTex.IsDisposed)
                    {
                        // 验证纹理尺寸
                        if (legacyTex.Width <= 0 || legacyTex.Height <= 0)
                        {

                            legacyTex?.Dispose();
                            legacyTex = null;
                        }
                        else
                        {
                            GameTools.ProcessTextureShape(legacyTex, shape, shapeParms);
                        }
                    }

                    // 🔥 关键修复：只有成功加载的纹理才添加到缓存
                    // 失败的纹理（null）不添加到缓存，允许后续重试
                    if (legacyTex != null)
                    {
                        lock (dics)
                        {
                            if (!dics.ContainsKey(name))
                            {
                                dics.Add(name, legacyTex);
                            }
                        }
                    }
                }

                if (legacyTex == null)
                {
                    // 🔥 使用回退纹理防止崩溃 (特别是设备丢失时)
                    // 但不要将 fallback 纹理添加到缓存，避免污染缓存
                    // 确保回退纹理可用且未释放
                    if (_fallbackTexture == null || _fallbackTexture.IsDisposed || _fallbackTexture.GraphicsDevice != Platform.GraphicsDevice)
                    {
                        // 如果设备已丢失，不要尝试创建回退纹理
                        if (_isDeviceLost) return null;

                        try 
                        {
                            if (Platform.GraphicsDevice != null && !Platform.GraphicsDevice.IsDisposed)
                            {

                                _fallbackTexture = new Texture2D(Platform.GraphicsDevice, 1, 1);
                                _fallbackTexture.SetData(new Color[] { Color.White });
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    if (_fallbackTexture != null && !_fallbackTexture.IsDisposed)
                    {

                        return _fallbackTexture;
                    }
                    return null;
                }

                return legacyTex;
            }
            catch (Exception ex)
            {

                
                 // 🔥 发生异常也返回回退纹理
                 if (_fallbackTexture != null && !_fallbackTexture.IsDisposed)
                {
                     // 确保设备一致
                     if (_fallbackTexture.GraphicsDevice == Platform.GraphicsDevice)
                        return _fallbackTexture;
                }
                return null;
            }
        }

        /// <summary>
        /// 准备纹理路径，处理扩展名和路径格式
        /// </summary>
        private static string PrepareTexturePath(string name, bool isUser)
        {
            string res = "";

            if (isUser)
            {
                res = name;
            }
            else
            {
                //res = "Textures" + Platform.Current.GetSlash + name;
                res = name;
                if (!name.Contains("."))
                {
                    // 检查是否是地图文件路径，如果是则不添加扩展名，让LoadTextureWithFormatPriority处理
                    if (name.Contains("ditu/") || name.Contains("ditu\\"))
                    {
                        // 地图文件，保持原路径不变，让Platform.LoadTexture使用DDS优先级
                        res = name;
                    }
                    else
                    {
                        // 非地图文件，使用TextureRecs中的扩展名
                        TextureRecs rec = Session.TextureRecs.FirstOrDefault(te => te.Key.Split('#')[0] == name).Value;
                        if (rec.Ext != null)
                        {
                            res = res + "." + rec.Ext;
                        }
                    }
                }
            }

            return res;
        }

        public static Texture2D LoadShapeTexture(string name, bool isTemp, TextureShape shape, float[] shapeParms)
        {
            var dics = isTemp ? TextureTempDics : TextureDics;
            Texture2D tex = null;
            bool reload = false;
            lock (dics)
            {
                if (dics != null && (!dics.TryGetValue(name, out tex) || tex != null && tex.IsDisposed))
                {
                    reload = true;
                    if (tex != null) dics.Remove(name);
                }
            }
            if (reload)
            {
                tex = GameTools.CreateShapeTexture(shape, Color.White, shapeParms);

                lock (dics)
                {
                    if (tex == null)
                    {
                        dics.Add(name, null);
                    }
                    else
                    {
                        dics.Add(name, tex);
                    }
                }
            }
            return tex;
        }

        public static void Draw(string name, Vector2 pos, Color color)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = LoadTexture(name);
                
                // 使用新的安全验证方法
                if (IsTextureValid(tex))
                {
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, pos, color);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                    
                    // 如果是tupianwenzi系统的纹理加载失败，尝试静默处理
                    if (name.Contains("tupianwenzi"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] tupianwenzi纹理加载失败，静默跳过: {name}");
                        return; // 静默跳过，不显示错误
                    }
                }
            }
            catch (Exception ex)
            {
                // 使用新的异常处理方法
                HandleTextureException(name, ex);
            }
        }

        // 🔥 临时诊断计数器（类级别）- 重置为 0 以重新开始调试
        private static int _drawCallCount = 0;
        
        // 🔥 新增：直接接受 Texture2D 的重载（用于战法动画等已加载的纹理）
        // 日期：2026-03-09
        // 原因：TileAnimation.Draw 直接传入 LinkedAnimation.Texture（Texture2D 类型）
        // 性能：Hot Path - 无 LINQ、无分配、无 try-catch、无字符串插值
        // 🔥 修复：source 参数改为 Rectangle 类型以匹配 TileAnimation.Draw 的调用
        public static void Draw(Texture2D texture, Rectangle destination, Rectangle source, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth)
        {
            // 🔥 Anti-Band-Aid：不添加空检查，让它崩溃
            // 如果 texture 为 null，说明 TileAnimationGenerator.AddTileAnimation 初始化错误
            // 如果 SpriteBatch 为 null，说明 Session 初始化错误
            // 解决：追溯到数据源（AllTileAnimations 加载或 Session.Initialize）
            
            // 🔥 应用缩放并对齐像素边界（避免摩尔纹）
            // 日期：2026-03-19
            // 原因：独立取整 X/Width 会导致相邻矩形边界不对齐，产生摩尔纹
            // 方案：先计算浮点边界，统一取整后通过差值计算宽高
            // 性能：Hot Path - 4 次 Math.Round（JIT 内联，~8 CPU 周期）比字典缓存更快
            if (Scale != Vector2.One)
            {
                // 1. 计算浮点边界
                float exactLeft = destination.X * Scale.X;
                float exactTop = destination.Y * Scale.Y;
                float exactRight = (destination.X + destination.Width) * Scale.X;
                float exactBottom = (destination.Y + destination.Height) * Scale.Y;

                // 2. 统一取整（四舍五入）
                int left = (int)Math.Round(exactLeft);
                int top = (int)Math.Round(exactTop);
                int right = (int)Math.Round(exactRight);
                int bottom = (int)Math.Round(exactBottom);

                // 3. 通过边界差值计算宽高，避免独立取整误差
                destination = new Rectangle(
                    left,
                    top,
                    Math.Max(1, right - left),
                    Math.Max(1, bottom - top)
                );
            }
            
            // 🔥 ANTI-BAND-AID：直接绘制，不捕获异常（让 SpriteBatch 状态错误崩溃）
            // Hot Path 性能：不使用 try-catch，让异常直接崩溃以追溯数据源
            Session.Current.SpriteBatch.Draw(texture, destination, source, color, rotation, origin, effect, depth);
        }


        public static void Draw(string name, Vector2 pos, Rectangle? source, Color color)
        {
            Draw(name, pos, source, color, SpriteEffects.None, 1f);
        }

        public static Bounds Draw(string name, Vector2 pos, Rectangle? source, Color color, SpriteEffects effect, float scale, float depth = 0f)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称为空，跳过绘制");
                    return new Bounds();
                }
                
                // 验证Session状态
                if (Session.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] Session.Current为null，跳过绘制");
                    return new Bounds();
                }
                
                // 验证SpriteBatch状态
                if (Session.Current.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return new Bounds();
                }
                
                // 验证GraphicsDevice状态
                if (Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] GraphicsDevice无效，跳过绘制");
                    return new Bounds();
                }
                
                Texture2D tex = LoadTexture(name);

                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {name} ({tex.Width}x{tex.Height})");
                        return new Bounds();
                    }
                    
                    // 验证缩放值
                    if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 缩放值无效: {scale}，使用默认值1.0");
                        scale = 1.0f;
                    }
                    
                    // 验证深度值
                    if (float.IsNaN(depth) || float.IsInfinity(depth))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 深度值无效: {depth}，使用默认值0.0");
                        depth = 0.0f;
                    }
                    
                    // 验证位置值
                    if (float.IsNaN(pos.X) || float.IsNaN(pos.Y) || 
                        float.IsInfinity(pos.X) || float.IsInfinity(pos.Y))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 位置值无效: {pos}，使用默认值(0,0)");
                        pos = Vector2.Zero;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, pos, source, color, 0f, Vector2.Zero, scale, effect, depth);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                }

                if (source == null)
                    return new Bounds();
                return new Bounds() { X = pos.X, Y = pos.Y, X2 = pos.X + source.Value.Width * scale, Y2 = pos.Y + source.Value.Height * scale };
            }
            catch (ObjectDisposedException odEx)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 对象已释放: {odEx.Message}");
                return new Bounds();
            }
            catch (InvalidOperationException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] SpriteBatch状态错误: {ioEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 可能SpriteBatch未调用Begin()或已调用End()");
                return new Bounds();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {name}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
                return new Bounds();
            }
        }

        public static void Draw(string name, Vector2 pos, Rectangle? source, Color color, SpriteEffects effect, Vector2 scale, float depth = 0f)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = LoadTexture(name);
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 验证缩放值
                    if (float.IsNaN(scale.X) || float.IsInfinity(scale.X) || scale.X <= 0 ||
                        float.IsNaN(scale.Y) || float.IsInfinity(scale.Y) || scale.Y <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 缩放值无效: {scale}，使用默认值(1,1)");
                        scale = Vector2.One;
                    }
                    
                    // 验证深度值
                    if (float.IsNaN(depth) || float.IsInfinity(depth))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 深度值无效: {depth}，使用默认值0.0");
                        depth = 0.0f;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, pos, source, color, 0f, Vector2.Zero, scale, effect, depth);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {name}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void Draw(string name, Vector2 pos, Rectangle? source, Color color, float rotation, SpriteEffects effect, Vector2 scale)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = LoadTexture(name);
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 验证缩放值
                    if (float.IsNaN(scale.X) || float.IsInfinity(scale.X) || scale.X <= 0 ||
                        float.IsNaN(scale.Y) || float.IsInfinity(scale.Y) || scale.Y <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 缩放值无效: {scale}，使用默认值(1,1)");
                        scale = Vector2.One;
                    }
                    
                    // 验证旋转值
                    if (float.IsNaN(rotation) || float.IsInfinity(rotation))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 旋转值无效: {rotation}，使用默认值0.0");
                        rotation = 0.0f;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, pos, source, color, rotation, Vector2.Zero, scale, effect, 0f);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {name}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void Draw(PlatformTexture platformTexture, Vector2 pos, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effect, float depth)
        {
            try
            {
                // 验证输入参数
                if (platformTexture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture为null，跳过绘制");
                    return;
                }
                
                // 🔥 修复：如果 Texture 已存在，Name 可以为空
                if (platformTexture.Texture == null && String.IsNullOrEmpty(platformTexture.Name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture.Texture和Name都为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = null;

                // 0. 🔥 优先检查 PlatformTexture 是否直接持有纹理 (修复: 避免重复加载/无法找到手动加载的纹理)
                if (platformTexture.Texture != null && !platformTexture.Texture.IsDisposed)
                {
                    tex = platformTexture.Texture;
                }
                
                // 1. 检查临时纹理字典（用于DDS等特殊加载的纹理）
                if (tex == null && TextureTempDics.ContainsKey(platformTexture.Name))
                {
                    tex = TextureTempDics[platformTexture.Name];
                    // System.Diagnostics.Debug.WriteLine($"[CacheManager] 从TextureTempDics找到纹理: {platformTexture.Name}");
                }
                
                // 如果临时字典中没有，使用正常的LoadTexture
                if (tex == null)
                {
                    tex = LoadTexture(platformTexture.Name);
                }
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {platformTexture.Name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 验证缩放值
                    if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 缩放值无效: {scale}，使用默认值1.0");
                        scale = 1.0f;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, pos, source, color, rotation, origin, scale, effect, depth);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {platformTexture.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {platformTexture?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void Draw(string text, Rectangle rec, Rectangle? source, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth)
        {
            var texture = new PlatformTexture()
            {
                Name = text
            };
            Draw(texture, rec, source, color, rotation, origin, effect, depth);
        }


        public static void Draw(PlatformTexture platformTexture, Rectangle rec, Rectangle? source, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth)
        {
            try
            {
                // 验证输入参数
                if (platformTexture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture为null，跳过绘制");
                    return;
                }
                
                // 🔥 修复：如果 Texture 已存在，Name 可以为空
                if (platformTexture.Texture == null && String.IsNullOrEmpty(platformTexture.Name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture.Texture和Name都为空，跳过绘制");
                    return;
                }
                
                // 验证Session状态
                if (Session.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] Session.Current为null，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                // 验证GraphicsDevice状态
                if (Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] GraphicsDevice无效，跳过绘制");
                    return;
                }
                
                Texture2D tex = null;
                
                // 🔥 优先检查 PlatformTexture 是否直接持有纹理
                if (platformTexture.Texture != null && !platformTexture.Texture.IsDisposed)
                {
                    tex = platformTexture.Texture;
                }
                
                // 首先检查临时纹理字典（用于DDS等特殊加载的纹理）
                if (tex == null && !String.IsNullOrEmpty(platformTexture.Name) && TextureTempDics.ContainsKey(platformTexture.Name))
                {
                    tex = TextureTempDics[platformTexture.Name];
                }
                
                // 如果临时字典中没有，使用正常的LoadTexture
                if (tex == null && !String.IsNullOrEmpty(platformTexture.Name))
                {
                    tex = LoadTexture(platformTexture.Name);
                }
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {platformTexture.Name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 验证绘制区域
                    if (rec.Width <= 0 || rec.Height <= 0)
                    {
                        // 静默跳过无效绘制，避免错误信息
                        return;
                    }
                    
                    // 🔥 应用缩放并对齐像素边界（避免摩尔纹）
                    // 日期：2026-03-19
                    // 原因：独立取整 X/Width 会导致相邻矩形边界不对齐，产生摩尔纹
                    // 方案：先计算浮点边界，统一取整后通过差值计算宽高
                    if (Scale != Vector2.One)
                    {
                        // 1. 计算浮点边界
                        float exactLeft = rec.X * Scale.X;
                        float exactTop = rec.Y * Scale.Y;
                        float exactRight = (rec.X + rec.Width) * Scale.X;
                        float exactBottom = (rec.Y + rec.Height) * Scale.Y;

                        // 2. 统一取整（四舍五入）
                        int left = (int)Math.Round(exactLeft);
                        int top = (int)Math.Round(exactTop);
                        int right = (int)Math.Round(exactRight);
                        int bottom = (int)Math.Round(exactBottom);

                        // 3. 通过边界差值计算宽高，避免独立取整误差
                        rec = new Rectangle(
                            left,
                            top,
                            Math.Max(1, right - left),
                            Math.Max(1, bottom - top)
                        );
                    }
                    
                    // 验证参数有效性
                    if (float.IsNaN(rotation) || float.IsInfinity(rotation))
                    {
                        rotation = 0f;
                    }
                    
                    if (float.IsNaN(depth) || float.IsInfinity(depth))
                    {
                        depth = 0f;
                    }
                    
                    if (float.IsNaN(origin.X) || float.IsNaN(origin.Y) || 
                        float.IsInfinity(origin.X) || float.IsInfinity(origin.Y))
                    {
                        origin = Vector2.Zero;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, rec, source, color, rotation, origin, effect, depth);
                }
                else
                {
                    // 静默处理纹理无效的情况
                }
            }
            catch (ObjectDisposedException odEx)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 对象已释放: {odEx.Message}");
            }
            catch (InvalidOperationException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] SpriteBatch状态错误: {ioEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 可能SpriteBatch未调用Begin()或已调用End()");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {platformTexture?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void Draw(string name, Rectangle dest, Color color)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                // 验证绘制区域
                if (dest.Width <= 0 || dest.Height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 绘制区域无效: {dest}");
                    return;
                }
                
                Texture2D tex = LoadTexture(name);
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 安全绘制
                    Session.Current.SpriteBatch.Draw(tex, dest, color);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {name}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void Draw(string name, string sec, Vector2 pos, Color color)
        {
            Draw(name, sec, pos, color, new Vector2(1, 1));
        }

        public static void Draw(string name, string sec, Vector2 pos, Color color, Vector2 scale)
        {
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(sec))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] 纹理名称或区段为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = LoadTexture(name);
                
                // 严格验证纹理
                if (tex != null && !tex.IsDisposed)
                {
                    // 验证纹理尺寸
                    if (tex.Width <= 0 || tex.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理尺寸无效: {name} ({tex.Width}x{tex.Height})");
                        return;
                    }
                    
                    // 验证缩放值
                    if (float.IsNaN(scale.X) || float.IsInfinity(scale.X) || scale.X <= 0 ||
                        float.IsNaN(scale.Y) || float.IsInfinity(scale.Y) || scale.Y <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 缩放值无效: {scale}，使用默认值(1,1)");
                        scale = Vector2.One;
                    }
                    
                    // 验证纹理记录
                    string key = name + "#" + sec;
                    if (Session.TextureRecs.ContainsKey(key))
                    {
                        var textureRec = Session.TextureRecs[key];
                        if (textureRec.Recs != null && textureRec.Recs.Length > 0)
                        {
                            // 安全绘制
                            Session.Current.SpriteBatch.Draw(tex, pos, textureRec.Recs[0], color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理记录区域为空: {key}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CacheManager] 未找到纹理记录: {key}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理名称: {name}, 区段: {sec}");
                System.Diagnostics.Debug.WriteLine($"[CacheManager] 异常堆栈: {ex.StackTrace}");
            }
        }

        public static void DrawGeneralAvatar(string id, Vector2 pos, float alpha, float? scale = null, bool isTemp = true, TextureShape shape = TextureShape.None, float[] shapeParams = null)
        {
            if (String.IsNullOrEmpty(id))
            {
                CacheManager.DrawAvatar(@"Avatars\General\Custom.jpg", pos + new Vector2(3, 3), Color.White * alpha, scale == null ? 1f : (float)scale, false, isTemp, shape, shapeParams);
            }
            else if (id.StartsWith("Avatar"))
            {
                if (!id.Contains("."))
                {
                    id = id + ".jpg";
                }
                CacheManager.DrawAvatar(id, pos + new Vector2(3, 3), Color.White * alpha, scale == null ? 0.5f : 0.5f * (float)scale, true, isTemp, shape, shapeParams);
            }
            else
            {
                if (id.Contains('-'))
                {
                    id = id.Split('-')[1];
                }
                CacheManager.DrawAvatar(@"Avatars\General\" + id + ".jpg", pos + new Vector2(3, 3), Color.White * alpha, scale == null ? 1f : (float)scale, false, isTemp, shape, shapeParams);
            }
        }

        public static void DrawAvatar(string name, Vector2 pos, Color color, float scale, bool isUser = false, bool isTemp = true, TextureShape shape = TextureShape.None, float[] shapeParams = null)
        {
            if (!String.IsNullOrEmpty(name))
            {
                Texture2D tex = LoadAvatar(name, isUser, isTemp, shape, shapeParams);
                if (tex != null && !tex.IsDisposed)
                {
                    Session.Current.SpriteBatch.Draw(tex, pos, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }

        public static void DrawAvatar(string name, Rectangle pos, Color color, bool isUser = false, bool isTemp = true, TextureShape shape = TextureShape.None, float[] shapeParams = null, float depth = 0f)
        {
            if (!String.IsNullOrEmpty(name))
            {
                try
                {
                    Texture2D tex = LoadAvatar(name, isUser, isTemp, shape, shapeParams);
                    if (tex != null && !tex.IsDisposed)
                    {
                        // 计算保持宽高比的绘制矩形
                        Rectangle drawRect = CalculateAspectRatioRectangle(tex.Width, tex.Height, pos);
                        
                        if (Scale != Vector2.One)
                        {
                            drawRect = new Rectangle(Convert.ToInt16(drawRect.X * Scale.X), Convert.ToInt16(drawRect.Y * Scale.Y), Convert.ToInt16(drawRect.Width * Scale.X), Convert.ToInt16(drawRect.Height * Scale.Y));
                        }
                        Session.Current.SpriteBatch.Draw(tex, drawRect, null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
                    }
                }
                catch (Exception ex)
                {
                    // 如果绘制失败，记录错误但不崩溃游戏
                    WebTools.TakeWarnMsg($"绘制纹理失败: {name}", "DrawAvatar", ex);
                }
            }
        }

        /// <summary>
        /// 计算保持宽高比的矩形，在目标矩形内居中显示
        /// </summary>
        /// <param name="textureWidth">纹理宽度</param>
        /// <param name="textureHeight">纹理高度</param>
        /// <param name="targetRect">目标矩形</param>
        /// <returns>保持宽高比的绘制矩形</returns>
        private static Rectangle CalculateAspectRatioRectangle(int textureWidth, int textureHeight, Rectangle targetRect)
        {
            if (textureWidth <= 0 || textureHeight <= 0)
                return targetRect;

            float textureAspect = (float)textureWidth / textureHeight;
            float targetAspect = (float)targetRect.Width / targetRect.Height;

            int drawWidth, drawHeight;
            int drawX, drawY;

            if (textureAspect > targetAspect)
            {
                // 纹理更宽，以宽度为准
                drawWidth = targetRect.Width;
                drawHeight = (int)(targetRect.Width / textureAspect);
                drawX = targetRect.X;
                drawY = targetRect.Y + (targetRect.Height - drawHeight) / 2;
            }
            else
            {
                // 纹理更高，以高度为准
                drawHeight = targetRect.Height;
                drawWidth = (int)(targetRect.Height * textureAspect);
                drawX = targetRect.X + (targetRect.Width - drawWidth) / 2;
                drawY = targetRect.Y;
            }

            return new Rectangle(drawX, drawY, drawWidth, drawHeight);
        }

        public static void DrawAvatar(string name, Vector2 pos, Color color, Vector2 scale, Rectangle? source = null, bool isUser = false, bool isTemp = true)
        {
            Texture2D tex = LoadAvatar(name, isUser, isTemp, TextureShape.None, null);
            if (tex != null && !tex.IsDisposed)
            {
                Session.Current.SpriteBatch.Draw(tex, pos, source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 获取人物头像
        /// </summary>


        /// <summary>
        /// 绘制人物头像（支持形状）
        /// </summary>
        /// <param name="person">人物</param>
        /// <param name="pos">位置</param>
        /// <param name="depth">深度</param>
        /// <param name="size">尺寸</param>
        /// <param name="color">颜色</param>
        /// <param name="type">默认类型</param>
        /// <param name="shape">形状</param>
        // 性能优化：减少频繁的缓存检查
        private static int _lastCacheCheckFrame = 0;
        private static readonly int CACHE_CHECK_INTERVAL = 60; // 每60帧检查一次缓存
        
        // 预加载机制
        private static readonly HashSet<string> _preloadedTextures = new HashSet<string>();
        private static readonly object _preloadLock = new object();

        public static void DrawZhsanAvatar(Person person, Rectangle pos, float depth, PortraitSize size = PortraitSize.Medium, Color? color = null, PortraitDefaultType? type = null, TextureShape shape = TextureShape.None)
        {
            try
            {
                var path = GetPersonPortraitPath(person, type, size);
                var drawColor = color ?? Color.White;

                // 性能优化：减少频繁的缓存清理检查
                var currentFrame = Environment.TickCount;
                if (currentFrame - _lastCacheCheckFrame > CACHE_CHECK_INTERVAL)
                {
                    _lastCacheCheckFrame = currentFrame;
                    
                    // 只有在缓存真的很大时才清理
                    if (TextureTempDics.Count > 100)
                    {
                        // 异步清理，避免阻塞渲染
                        Task.Run(() =>
                        {
                            try
                            {
                                Clear(CacheType.Temp);
                            }
                            catch (Exception cleanEx)
                            {
                                WebTools.TakeWarnMsg("异步缓存清理失败", "DrawZhsanAvatar", cleanEx);
                            }
                        });
                    }
                }

                // 直接使用原有系统，避免任何可能导致设备丢失的新代码
                DrawAvatar(path, pos, drawColor, false, true, shape, null, depth);
            }
            catch (Exception ex)
            {
                // 如果绘制头像失败，记录错误但不崩溃游戏
                WebTools.TakeWarnMsg($"绘制人物头像失败: {person?.Name ?? "Unknown"}", "DrawZhsanAvatar", ex);
            }
        }

        /// <summary>
        /// 获取人物头像
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pos"></param>


        /// <summary>
        /// 绘制人物头像（支持形状）
        /// </summary>
        /// <param name="index">头像索引</param>
        /// <param name="pos">位置</param>
        /// <param name="depth">深度</param>
        /// <param name="size">尺寸</param>
        /// <param name="color">颜色</param>
        /// <param name="type">默认类型</param>
        /// <param name="shape">形状</param>
        public static void DrawZhsanAvatar(int index, Rectangle pos, float depth, PortraitSize size = PortraitSize.Medium, Color? color = null, PortraitDefaultType? type = null, TextureShape shape = TextureShape.None)
        {
            try
            {
                var path = GetPersonPortraitPath(index, type, size);
                var drawColor = color ?? Color.White;

                // 使用相同的优化缓存检查逻辑
                var currentFrame = Environment.TickCount;
                if (currentFrame - _lastCacheCheckFrame > CACHE_CHECK_INTERVAL)
                {
                    _lastCacheCheckFrame = currentFrame;
                    
                    if (TextureTempDics.Count > 100)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                Clear(CacheType.Temp);
                            }
                            catch (Exception cleanEx)
                            {
                                WebTools.TakeWarnMsg("异步缓存清理失败", "DrawZhsanAvatar", cleanEx);
                            }
                        });
                    }
                }

                // 直接使用原有系统，避免任何可能导致设备丢失的新代码
                DrawAvatar(path, pos, drawColor, false, true, shape, null, depth);
            }
            catch (Exception ex)
            {
                // 如果绘制头像失败，记录错误但不崩溃游戏
                WebTools.TakeWarnMsg($"绘制头像失败: {index}", "DrawZhsanAvatar", ex);
            }
        }



        /// <summary>
        /// 获取头像路径
        /// </summary>
        /// <param name="person"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static string GetPersonPortraitPath(Person person, PortraitDefaultType? type = null, PortraitSize size = PortraitSize.Medium)
        {
            var id = person == null ? (int)PortraitDefaultType.Military : person.PictureIndex;

            var defaultType = type ?? (person == null ? PortraitDefaultType.Military : person.GetPortraitDefaultType());

            var path = GetPersonPortraitPath(id, defaultType, size);

            return path;
        }

        // 性能优化：缓存头像路径查找结果，避免重复的文件IO操作
        private static readonly Dictionary<string, string> _portraitPathCache = new Dictionary<string, string>();
        private static readonly object _portraitPathCacheLock = new object();

        /// <summary>
        /// 获取头像路径（带缓存优化）
        /// </summary>
        /// <param name="index"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetPersonPortraitPath(int index, PortraitDefaultType? type = null, PortraitSize size = PortraitSize.Medium)
        {
            var defaultIndex = (int)(type ?? PortraitDefaultType.Military);
            
            // 创建缓存键
            var cacheKey = $"{index}_{(int)size}_{defaultIndex}_{Setting.Current?.PortraitPack ?? "default"}_{Setting.Current?.MODRuntime ?? "none"}";
            
            // 先检查缓存
            lock (_portraitPathCacheLock)
            {
                if (_portraitPathCache.TryGetValue(cacheKey, out string cachedPath))
                {
                    return cachedPath;
                }
            }

            // 缓存未命中，执行实际的路径查找
            var result = GetPersonPortraitPathInternal(index, type, size);
            
            // 缓存结果
            lock (_portraitPathCacheLock)
            {
                // 限制缓存大小，避免内存泄漏
                if (_portraitPathCache.Count > 1000)
                {
                    // 清理一半的缓存项
                    var keysToRemove = _portraitPathCache.Keys.Take(_portraitPathCache.Count / 2).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _portraitPathCache.Remove(key);
                    }
                }
                
                _portraitPathCache[cacheKey] = result;
            }
            
            return result;
        }

        /// <summary>
        /// 内部头像路径查找方法（不使用缓存）
        /// </summary>
        private static string GetPersonPortraitPathInternal(int index, PortraitDefaultType? type = null, PortraitSize size = PortraitSize.Medium)
        {
            var defaultIndex = (int)(type ?? PortraitDefaultType.Military);

            var customDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Player/";
            
            var portraitPack = Setting.Current.PortraitPack;
            var defaultDir = String.IsNullOrWhiteSpace(portraitPack) ? @"Content/Textures/GameComponents/PersonPortrait/Images/Default/"
                                                                     : $"Portraits/{portraitPack}/";

            var suffix = size == PortraitSize.Medium ? string.Empty : "s";

            // 支持的图片格式，按优先级排序：DDS > PNG > JPG
            var extensions = new[] { ".dds", ".png", ".jpg" };

            // 构建所有可能的路径组合
            var basePaths = new[]
            {
                $"{customDir}{index}{suffix}",          // 自定义
                ReplaceModPath($"{defaultDir}{index}{suffix}"), // mod头像
                $"{defaultDir}{index}{suffix}",         // 头像包
                $"{defaultDir}{index}",                 // 原尺寸
                $"{defaultDir}{defaultIndex}"           // 通用默认头像
            };

            // 对每个基础路径，按格式优先级检查
            foreach (var basePath in basePaths)
            {
                foreach (var ext in extensions)
                {
                    var fullPath = basePath + ext;
                    if (Platform.Current.FileExists(fullPath))
                    {
                        // 添加调试输出，显示找到的文件格式
                        // System.Diagnostics.Debug.WriteLine($"[CacheManager] 找到头像文件: {fullPath} (格式: {ext.ToUpper()})");
                        return fullPath;
                    }
                }
            }

            // 如果没有找到任何文件，输出调试信息
            System.Diagnostics.Debug.WriteLine($"[CacheManager] 未找到头像文件: ID={index}, 尺寸={size}, 默认类型={type}");
            return string.Empty;
        }

        /// <summary>
        /// 清理头像路径缓存（在MOD或头像包更改时调用）
        /// </summary>
        public static void ClearPortraitPathCache()
        {
            lock (_portraitPathCacheLock)
            {
                _portraitPathCache.Clear();
            }
        }

        /// <summary>
        /// 预加载常用头像纹理（异步）
        /// </summary>
        /// <param name="portraitIds">要预加载的头像ID列表</param>
        public static void PreloadPortraitsAsync(IEnumerable<int> portraitIds)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var id in portraitIds)
                    {
                        var path = GetPersonPortraitPath(id, null, PortraitSize.Small);
                        if (!string.IsNullOrEmpty(path))
                        {
                            lock (_preloadLock)
                            {
                                if (!_preloadedTextures.Contains(path))
                                {
                                    // 预加载到永久缓存
                                    var texture = LoadTexture(path, false, false, TextureShape.None, null);
                                    if (texture != null)
                                    {
                                        _preloadedTextures.Add(path);
                                    }
                                }
                            }
                        }
                        
                        // 避免阻塞主线程
                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    WebTools.TakeWarnMsg("预加载头像失败", "PreloadPortraitsAsync", ex);
                }
            });
        }

        /// <summary>
        /// 预加载当前场景中的人物头像
        /// </summary>
        public static void PreloadCurrentScenePortraits()
        {
            try
            {
                if (Session.Current?.Scenario?.Persons != null)
                {
                    // 获取当前可见的人物ID
                    var visiblePersonIds = new List<int>();
                    var count = 0;
                    foreach (Person p in Session.Current.Scenario.Persons)
                    {
                        if (p.Available && p.Alive && count < 50)
                        {
                            if (!visiblePersonIds.Contains(p.PictureIndex))
                            {
                                visiblePersonIds.Add(p.PictureIndex);
                                count++;
                            }
                        }
                    }
                    
                    PreloadPortraitsAsync(visiblePersonIds);
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("预加载当前场景头像失败", "PreloadCurrentScenePortraits", ex);
            }
        }

        /// <summary>
        /// 替换mod路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ReplaceModPath(string path)
        {
            var mod = Setting.Current.MODRuntime;
            if (Setting.Current != null && !String.IsNullOrEmpty(mod))
                path = path.Replace("Content", $"MODs/{mod}");

            return path;
        }

        public static void DrawShape(string name, TextureShape shape, float[] shapeParms, Vector2 pos, Color color, Vector2 scale, Rectangle? source = null, bool isTemp = true)
        {
            Texture2D tex = LoadShapeTexture(name, isTemp, shape, shapeParms);
            if (tex != null && !tex.IsDisposed)
            {
                Session.Current.SpriteBatch.Draw(tex, pos, source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        public static string CheckTextCache(SpriteFont font, string text, bool checkTradition, bool upload)
        {
            text = text.NullToString();

            lock (DicTexts)
            {
                if (DicTexts == null)
                {
                    DicTexts = new Dictionary<string, string>();
                }

                if (!DicTexts.ContainsKey(text))
                {
                    string origin = text;

                    if (checkTradition)
                    {
                        //繁轉成簡
                        //text = text.TranslationWords(false, true);
                    }

                    DicTexts.Add(origin, text);
                }
                if (DicTexts.ContainsKey(text))
                {
                    string tex = DicTexts[text];
                    return !String.IsNullOrEmpty(tex) ? tex : text;
                }
                else
                {
                    return text;
                }
            }
        }

        public static void DrawString(SpriteFont font, string text, Vector2 pos, Color color, bool checkTradition = false, bool upload = false)
        {
            if (!String.IsNullOrEmpty(text))
            {
                try
                {
                    text = CheckTextCache(font, text, checkTradition, upload);
                    //Session.Current.SpriteBatch.DrawString(font, text, pos, color);

                    TextManager.DrawTexts(text, FontPair, pos, color);
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"[CacheManager] DrawString Exception: {ex.Message}");
                }
            }
        }

        public static void DrawString(SpriteFont font, string text, Vector2 pos, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, bool checkTradition = false, bool upload = false)
        {
            if (!String.IsNullOrEmpty(text))
            {
                // 绘制文本
                try
                {
                    TextManager.DrawTexts(text, FontPair, pos, color, 0, scale, layerDepth);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] DrawString (overload) Exception: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 画文字并返回文字范围的矩形列表（支持多行文字）
        /// </summary>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="color"></param>
        /// <param name="rotation"></param>
        /// <param name="origin"></param>
        /// <param name="scale"></param>
        /// <param name="effects"></param>
        /// <param name="layerDepth"></param>
        /// <param name="checkTradition"></param>
        /// <param name="upload"></param>
        /// <returns>返回文字范围的矩形列表</returns>
        public static List<Bounds> DrawStringReturnBounds(SpriteFont font, string text, Vector2 pos, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, bool checkTradition = false, bool upload = false)
        {
            List<Bounds> bounds = new List<Bounds>();
            if (!String.IsNullOrEmpty(text))
            {
                text = CheckTextCache(font, text, checkTradition, upload);
                //Session.Current.SpriteBatch.DrawString(font, text, pos * Scale, color, rotation, origin, scale * Scale, effects, layerDepth);

                try
                {
                    bounds = TextManager.DrawTextsReturnBounds(text, FontPair, pos, color, 0, scale, layerDepth);
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"[CacheManager] DrawStringReturnBounds Exception: {ex.Message}");
                }
            }
            return bounds;
        }

        public static List<Bounds> DrawStringReturnBounds(SpriteBatch batch,SpriteFont font, string text, Vector2 pos, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, bool checkTradition = false, bool upload = false)
        {
            List<Bounds> bounds = new List<Bounds>();
            if (!String.IsNullOrEmpty(text))
            {
                text = CheckTextCache(font, text, checkTradition, upload);
                //Session.Current.SpriteBatch.DrawString(font, text, pos * Scale, color, rotation, origin, scale * Scale, effects, layerDepth);

                bounds = TextManager.DrawTextsReturnBounds(batch, text, FontPair, pos, color, 0, scale, layerDepth);
            }
            return bounds;
        }

        /// <summary>
        /// 计算文字的边界范围
        /// </summary>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="scale"></param>
        /// <param name="checkTradition"></param>
        /// <param name="upload"></param>
        /// <returns></returns>
        public static List<Bounds> CalculateTextBounds(SpriteFont font, string text, Vector2 pos, float scale,  bool checkTradition = false, bool upload = false)
        {
            List<Bounds> bounds = new List<Bounds>();
            if (!String.IsNullOrEmpty(text))
            {
                text = CheckTextCache(font, text, checkTradition, upload);
                //Session.Current.SpriteBatch.DrawString(font, text, pos * Scale, color, rotation, origin, scale * Scale, effects, layerDepth);

                bounds = TextManager.CalcTextsBounds(text, FontPair, pos, 0, scale);
            }
            return bounds;
        }

        /// <summary>
        /// 将文字处理成自动换行
        /// </summary>
        /// <param name="font">字体</param>
        /// <param name="text">要处理的文字</param>
        /// <param name="lineWidth">行宽度</param>
        /// <param name="scale">缩放倍数</param>
        /// <param name="checkTradition"></param>
        /// <param name="upload"></param>
        /// <returns>返回经过自动换行处理过的文字</returns>
        public static string AutoWrap(SpriteFont font,string text,float lineWidth,float scale, bool checkTradition = false, bool upload = false)
        {
            if (!String.IsNullOrEmpty(text))
            {
                text = CheckTextCache(font, text, checkTradition, upload);
                //Session.Current.SpriteBatch.DrawString(font, text, pos * Scale, color, rotation, origin, scale * Scale, effects, layerDepth);

                return TextManager.HandleAutoWrap(text, FontPair, lineWidth, scale);
            }

            return null;
        }
    }
}
