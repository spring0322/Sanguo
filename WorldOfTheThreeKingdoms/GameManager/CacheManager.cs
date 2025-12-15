using FontStashSharp;
using GameGlobal;
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
// using WorldOfTheThreeKingdoms.Helpers;
namespace GameManager
{
    public class PlatformTexture
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
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
        None
    }

    public static class CacheManager
    {
        public static object CacheLock = new Object();

        // 保留旧的字典作为后备（用于兼容性）
        static Dictionary<string, Texture2D> TextureDics = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture2D> TextureTempDics = new Dictionary<string, Texture2D>();

        // 新的 LRU 缓存系统 (暂时禁用)
        // private static LRUTextureCache _lruCache;
        // private static CacheConfig _config;
        // private static bool _useLRUCache = false;

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
            // LRU 缓存暂时禁用
            /*
            _config = CacheConfig.Instance;
            
            // 如果没有指定内存预算，使用配置中的值
            if (maxMemoryMB == 1024 && _config.MaxMemoryMB != 1024)
            {
                maxMemoryMB = _config.MaxMemoryMB;
            }
            
            _lruCache = new LRUTextureCache(graphicsDevice, maxMemoryMB);
            _useLRUCache = _config.EnableLRUCache;
            
            // 设置性能监控显示状态
            PerformanceMonitor.IsEnabled = _config.ShowPerformanceMonitor;
            
            System.Diagnostics.Debug.WriteLine($"[CacheManager] LRU缓存已初始化 - {_config}");
            */
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static string GetCacheStats()
        {
            // LRU 缓存暂时禁用
            /*
            if (_lruCache != null && _useLRUCache)
            {
                return _lruCache.GetStats().ToString();
            }
            */
            return $"Legacy Cache - Items: {TextureDics.Count + TextureTempDics.Count}";
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

                // 如果使用 LRU 缓存，根据类型进行智能清理 (暂时禁用)
                /*
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
                */

                // 保留原有的清理逻辑作为后备
                //CoreGame.Current.AudioContent.Unload();
                foreach (var tex in TextureTempDics)
                {
                    if (tex.Value != null && !tex.Value.IsDisposed)
                    {
                        tex.Value.Dispose();
                    }
                }

                //TextureTempDics.ToList().ForEach(te => te.Value.Dispose());
                TextureTempDics.Clear();

                var dics = TextureDics.ToList();
                for (int i = dics.Count - 1; i >= 0; i--)
                {
                    var tex = dics[i];
                    var rec = Session.TextureRecs.FirstOrDefault(te => te.Key.Split('#')[0] == tex.Key).Value;
                    if (type == CacheType.Live && (rec.CacheType == "Live" || rec.CacheType == "Scene" || rec.CacheType == "Page" || rec.CacheType == "Temp") ||
                        type == CacheType.Scene && (rec.CacheType == "Scene" || rec.CacheType == "Page" || rec.CacheType == "Temp") ||
                        type == CacheType.Page && (rec.CacheType == "Page" || rec.CacheType == "Temp") ||
                        type == CacheType.Temp && (rec.CacheType == "Temp"))
                    {
                        if (tex.Value != null && !tex.Value.IsDisposed)
                        {
                            tex.Value.Dispose();
                        }
                        TextureDics.Remove(tex.Key);
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(rec.CacheType))
                        {
                            //這應該是用戶材質
                            //TextureDics.Remove(tex.Key);
                        }
                        //去除空或已經失效的材質
                        if (tex.Value == null || tex.Value.IsDisposed)
                        {
                            TextureDics.Remove(tex.Key);
                        }
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

        public static PlatformTexture GetTempTexture(string name)
        {
            return new PlatformTexture()
            {
                Name = name
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
            try
            {
                // 验证输入参数
                if (String.IsNullOrEmpty(name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] LoadTexture: 纹理名称为空");
                    return null;
                }

                // 如果启用了 LRU 缓存且不是临时纹理，使用 LRU 缓存 (暂时禁用)
                /*
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
                */

                // 回退到原有的缓存逻辑（用于临时纹理或禁用 LRU 时）
                var dics = isTemp ? TextureTempDics : TextureDics;
                Texture2D legacyTex = null;
                bool reload = false;
                
                lock (dics)
                {
                    if (dics != null && (!dics.TryGetValue(name, out legacyTex) || legacyTex != null && legacyTex.IsDisposed))
                    {
                        reload = true;
                        if (legacyTex != null) dics.Remove(name);
                    }
                }
                
                if (reload)
                {
                    string res = PrepareTexturePath(name, isUser);
                    legacyTex = Platform.Current.LoadTexture(res, isUser);

                    // 验证加载的纹理
                    if (legacyTex != null && !legacyTex.IsDisposed)
                    {
                        // 验证纹理尺寸
                        if (legacyTex.Width <= 0 || legacyTex.Height <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CacheManager] 加载的纹理尺寸无效: {name} ({legacyTex.Width}x{legacyTex.Height})");
                            legacyTex?.Dispose();
                            legacyTex = null;
                        }
                        else
                        {
                            GameTools.ProcessTextureShape(legacyTex, shape, shapeParms);
                        }
                    }

                    lock (dics)
                    {
                        // 总是添加到缓存，即使是null，避免重复尝试加载失败的纹理
                        if (!dics.ContainsKey(name))
                        {
                            dics.Add(name, legacyTex);
                        }
                    }
                }
                
                return legacyTex;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheManager] LoadTexture发生异常: {name}, 错误: {ex.Message}");
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
                    Session.Current.SpriteBatch.Draw(tex, pos, color);
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
                
                if (String.IsNullOrEmpty(platformTexture.Name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture.Name为空，跳过绘制");
                    return;
                }
                
                // 验证SpriteBatch状态
                if (Session.Current?.SpriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] SpriteBatch为null，跳过绘制");
                    return;
                }
                
                Texture2D tex = null;
                
                // 首先检查临时纹理字典（用于DDS等特殊加载的纹理）
                if (TextureTempDics.ContainsKey(platformTexture.Name))
                {
                    tex = TextureTempDics[platformTexture.Name];
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 从TextureTempDics找到纹理: {platformTexture.Name}");
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
                
                if (String.IsNullOrEmpty(platformTexture.Name))
                {
                    System.Diagnostics.Debug.WriteLine("[CacheManager] platformTexture.Name为空，跳过绘制");
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
                
                // 首先检查临时纹理字典（用于DDS等特殊加载的纹理）
                if (TextureTempDics.ContainsKey(platformTexture.Name))
                {
                    tex = TextureTempDics[platformTexture.Name];
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 从TextureTempDics找到纹理: {platformTexture.Name}");
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
                    
                    // 验证绘制区域
                    if (rec.Width <= 0 || rec.Height <= 0)
                    {
                        // 静默跳过无效绘制，避免错误信息
                        return;
                    }
                    
                    // 应用缩放
                    if (Scale != Vector2.One)
                    {
                        // 使用Math.Max确保尺寸至少为1，避免零尺寸矩形
                        int scaledWidth = Math.Max(1, Convert.ToInt32(rec.Width * Scale.X));
                        int scaledHeight = Math.Max(1, Convert.ToInt32(rec.Height * Scale.Y));
                        
                        rec = new Rectangle(
                            Convert.ToInt32(rec.X * Scale.X), 
                            Convert.ToInt32(rec.Y * Scale.Y), 
                            scaledWidth, 
                            scaledHeight
                        );
                        
                        // 再次验证缩放后的绘制区域，自动修复无效尺寸
                        if (rec.Width <= 0 || rec.Height <= 0)
                        {
                            // 自动修复：将无效尺寸设为1像素
                            rec = new Rectangle(rec.X, rec.Y, Math.Max(1, rec.Width), Math.Max(1, rec.Height));
                            System.Diagnostics.Debug.WriteLine($"[CacheManager] 已修复缩放后绘制区域: {rec}");
                        }
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
                    System.Diagnostics.Debug.WriteLine($"[CacheManager] 纹理无效或已释放: {platformTexture.Name}");
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
                        return fullPath;
                }
            }

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
