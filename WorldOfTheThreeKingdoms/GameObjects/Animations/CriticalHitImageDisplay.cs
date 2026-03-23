using GameObjects;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GameManager;  // 🔥 修复：PlatformTexture 在 GameManager 命名空间
using WorldOfTheThreeKingdoms.GameManager;

namespace WorldOfTheThreeKingdoms.GameObjects.Animations
{
    /// <summary>
    /// 暴击图显示类型
    /// </summary>
    public enum CriticalHitType
    {
        普通攻击暴击 = 0,
        战法攻击暴击 = 1
    }

    /// <summary>
    /// 暴击图显示项 - 单个暴击图的显示实例（对象池复用）
    /// </summary>
    public class CriticalHitImageItem
    {
        public string ImagePath { get; private set; } = string.Empty;
        public Vector2 Position { get; private set; }
        public double StartTime { get; private set; }
        public double Duration { get; private set; } = 1500; // 默认显示1.5秒
        public float Alpha { get; private set; } = 1.0f;
        public CriticalHitType HitType { get; private set; }
        public bool IsActive { get; private set; }
        
        private PlatformTexture _texture;
        private bool _textureLoaded;

        /// <summary>
        /// 无参构造函数 - 用于对象池预分配
        /// </summary>
        public CriticalHitImageItem()
        {
            // 对象池使用：预分配空对象，后续通过 Activate() 激活
        }

        /// <summary>
        /// 激活对象池中的实例（复用对象，避免 GC）
        /// </summary>
        public void Activate(string imagePath, Vector2 position, double startTime, CriticalHitType hitType)
        {
            ImagePath = imagePath;
            Position = position;
            StartTime = startTime;
            HitType = hitType;
            IsActive = true;
            Alpha = 1.0f;
            _textureLoaded = false;
            _texture = null;
        }

        /// <summary>
        /// 停用对象（返回对象池）
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            _texture = null;
            _textureLoaded = false;
        }

        /// <summary>
        /// 更新透明度（淡入淡出效果）
        /// 🧊 Cold Path - 每帧调用但不在热循环中
        /// </summary>
        public void Update(double currentTime)
        {
            double elapsed = currentTime - StartTime;
            
            if (elapsed < 200) // 前200ms淡入
            {
                Alpha = (float)(elapsed / 200.0);
            }
            else if (elapsed > Duration - 300) // 后300ms淡出
            {
                Alpha = (float)((Duration - elapsed) / 300.0);
            }
            else
            {
                Alpha = 1.0f;
            }
            
            Alpha = Math.Clamp(Alpha, 0f, 1f);
        }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired(double currentTime) => IsActive && (currentTime - StartTime) >= Duration;

        /// <summary>
        /// 绘制暴击图
        /// 🔥 Hot Path - 每帧调用
        /// </summary>
        public void Draw()
        {
            if (!IsActive) return;

            // 🔍 调试：每次绘制时输出（仅在激活时）
            if (!_textureLoaded)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图绘制] 首次绘制 - 路径: {ImagePath}");
            }

            // 🔥 Anti-Band-Aid: 不添加 try-catch 掩盖错误
            // 如果纹理加载失败，应该在数据源层面解决
            
            // 懒加载纹理（只在第一次调用时执行）
            if (!_textureLoaded)
            {
                _texture = CacheManager.GetTempTexture(ImagePath);
                _textureLoaded = true;
                
                if (_texture == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[暴击图绘制] ❌ 纹理加载失败 - {ImagePath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[暴击图绘制] ✅ 纹理加载成功 - {ImagePath} ({_texture.Width}x{_texture.Height})");
                }
            }

            // 🔥 Anti-Band-Aid: 不使用 if (_texture != null) 掩盖问题
            // 如果纹理为null，说明资源文件缺失，应该在初始化时检查
            // 这里只做基本验证，避免崩溃，但会输出警告
            if (_texture == null || _texture.Width <= 0 || _texture.Height <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CriticalHitImageItem] 警告：纹理无效 - {ImagePath}");
                return;
            }

            // 计算屏幕中央偏下位置（使用传入的偏移位置）
            Rectangle screenRect = new(0, 0, 
                Session.MainGame.Window.ClientBounds.Width, 
                Session.MainGame.Window.ClientBounds.Height);
            
            int imageWidth = _texture.Width;
            int imageHeight = _texture.Height;
            
            // 屏幕中央偏下（60%高度位置）+ 偏移量（避免重叠）
            int x = (screenRect.Width - imageWidth) / 2 + (int)Position.X;
            int y = (int)(screenRect.Height * 0.6f) - imageHeight / 2 + (int)Position.Y;
            
            Rectangle destRect = new(x, y, imageWidth, imageHeight);
            
            CacheManager.Draw(
                _texture,
                destRect,
                null,
                Color.White * Alpha,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0.001f); // 🎯 最高层级（仅次于鼠标箭头），确保在所有UI之上显示
        }
    }

    /// <summary>
    /// 暴击图显示管理器 - 管理所有暴击图的显示（对象池优化）
    /// </summary>
    public class CriticalHitImageManager
    {
        // 🔥 性能优化：使用固定数组对象池，避免 GC
        private readonly CriticalHitImageItem[] _itemPool;
        private const int MAX_CONCURRENT_ITEMS = 3; // 最多同时显示3个暴击图
        
        // 🔥 性能优化：路径缓存，避免重复字符串拼接和文件查询
        private readonly Dictionary<(int PersonID, bool IsFemale, CriticalHitType HitType, string MilitaryType), string> _pathCache = [];
        
        // 🔥 性能优化：错开显示位置，避免重叠
        private static readonly Vector2[] _positionOffsets = 
        [
            new Vector2(0, 0),        // 第1张：中央
            new Vector2(-50, -50),    // 第2张：左上偏移
            new Vector2(50, 50)       // 第3张：右下偏移
        ];

        /// <summary>
        /// 构造函数 - 初始化对象池
        /// </summary>
        public CriticalHitImageManager()
        {
            // 🔥 预分配对象池，避免运行时 GC
            _itemPool = new CriticalHitImageItem[MAX_CONCURRENT_ITEMS];
            for (int i = 0; i < MAX_CONCURRENT_ITEMS; i++)
            {
                _itemPool[i] = new CriticalHitImageItem();
            }
        }

        /// <summary>
        /// 添加暴击图显示
        /// 🧊 Cold Path - 暴击触发时调用（非每帧）
        /// </summary>
        public void AddCriticalHitImage(Troop troop, CriticalHitType hitType, GameTime gameTime)
        {
            System.Diagnostics.Debug.WriteLine($"[暴击图] AddCriticalHitImage 被调用 - 部队: {troop.DisplayName}, 类型: {hitType}");
            
            // 🔥 Anti-Band-Aid: 不使用 troop?.Leader 掩盖问题
            // 调用方应该保证 troop 和 Leader 不为 null
            // 这里只做断言式检查
            if (troop.Leader == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CriticalHitImageManager] 错误：部队 {troop.DisplayName} 没有主将");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[暴击图] 主将: {troop.Leader.Name} (ID={troop.Leader.ID}, 性别={(troop.Leader.Sex ? "女" : "男")})");
            System.Diagnostics.Debug.WriteLine($"[暴击图] 兵种: {troop.Army.RealMilitaryKind.Type}");

            // 🔥 性能优化：从对象池中查找可用槽位
            int availableSlot = -1;
            for (int i = 0; i < MAX_CONCURRENT_ITEMS; i++)
            {
                if (!_itemPool[i].IsActive)
                {
                    availableSlot = i;
                    break;
                }
            }

            // 如果没有可用槽位，跳过（已达到最大并发数）
            if (availableSlot == -1)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图] 警告：没有可用槽位（已达到最大并发数 {MAX_CONCURRENT_ITEMS}）");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[暴击图] 找到可用槽位: {availableSlot}");

            // 🔥 性能优化：使用缓存获取路径，避免重复字符串拼接
            string imagePath = GetCriticalHitImagePathCached(troop, hitType);
            
            System.Diagnostics.Debug.WriteLine($"[暴击图] 解析的图片路径: {imagePath}");
            
            // 如果路径为空，说明资源配置有问题
            if (string.IsNullOrEmpty(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"[CriticalHitImageManager] 错误：无法获取暴击图路径 - {troop.Leader.Name}");
                return;
            }

            double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
            
            System.Diagnostics.Debug.WriteLine($"[暴击图] 当前时间: {currentTime}ms");
            
            // 🔥 性能优化：使用预定义的偏移位置，避免重叠
            Vector2 position = _positionOffsets[availableSlot];
            
            System.Diagnostics.Debug.WriteLine($"[暴击图] 显示位置偏移: ({position.X}, {position.Y})");
            
            // 🔥 性能优化：复用对象池中的对象，避免 GC
            _itemPool[availableSlot].Activate(imagePath, position, currentTime, hitType);
            
            System.Diagnostics.Debug.WriteLine($"[暴击图] ✅ 成功激活槽位 {availableSlot} - {troop.DisplayName} 触发{hitType}");
        }

        /// <summary>
        /// 获取暴击图路径（带缓存优化）
        /// 🧊 Cold Path - 暴击触发时调用
        /// </summary>
        private string GetCriticalHitImagePathCached(Troop troop, CriticalHitType hitType)
        {
            Person leader = troop.Leader;
            string militaryTypeFolder = GetMilitaryTypeFolder(troop);
            
            // 🔥 性能优化：构建缓存键
            var cacheKey = (leader.ID, leader.Sex, hitType, militaryTypeFolder);
            
            // 🔥 性能优化：先查缓存，O(1) 复杂度
            if (_pathCache.TryGetValue(cacheKey, out string cachedPath))
            {
                return cachedPath;
            }
            
            // 缓存未命中，执行完整查找
            string resolvedPath = GetCriticalHitImagePath(troop, hitType);
            
            // 🔥 性能优化：存入缓存，下次直接使用
            _pathCache[cacheKey] = resolvedPath;
            
            return resolvedPath;
        }

        /// <summary>
        /// 获取暴击图路径（核心查找逻辑）
        /// 🧊 Cold Path - 仅在缓存未命中时调用
        /// </summary>
        private string GetCriticalHitImagePath(Troop troop, CriticalHitType hitType)
        {
            Person leader = troop.Leader;

            // 性别判断：true=女，false=男
            string genderFolder = leader.Sex ? "Female" : "Male";
            
            // 类型文件夹
            string typeFolder = hitType == CriticalHitType.普通攻击暴击 ? "Normal" : "Skill";
            
            // 🎯 获取兵种类型
            string militaryTypeFolder = GetMilitaryTypeFolder(troop);
            
            System.Diagnostics.Debug.WriteLine($"[暴击图路径] 性别: {genderFolder}, 类型: {typeFolder}, 兵种: {militaryTypeFolder}");
            
            // 🧊 Cold Path：按优先级尝试加载（可读性优先）
            // 优先级1：主将ID + 兵种类型
            string path = $"Content/Textures/CriticalHit/{genderFolder}/{typeFolder}/{militaryTypeFolder}/{leader.ID}.png";
            System.Diagnostics.Debug.WriteLine($"[暴击图路径] 尝试优先级1: {path}");
            if (CacheManager.GetTempTexture(path) != null)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图路径] ✅ 找到优先级1图片");
                return path;
            }
            
            // 优先级2：主将ID（不区分兵种）
            path = $"Content/Textures/CriticalHit/{genderFolder}/{typeFolder}/{leader.ID}.png";
            System.Diagnostics.Debug.WriteLine($"[暴击图路径] 尝试优先级2: {path}");
            if (CacheManager.GetTempTexture(path) != null)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图路径] ✅ 找到优先级2图片");
                return path;
            }
            
            // 优先级3：兵种默认图片
            path = $"Content/Textures/CriticalHit/{genderFolder}/{typeFolder}/{militaryTypeFolder}/Default.png";
            System.Diagnostics.Debug.WriteLine($"[暴击图路径] 尝试优先级3: {path}");
            if (CacheManager.GetTempTexture(path) != null)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图路径] ✅ 找到优先级3图片");
                return path;
            }
            
            // 优先级4：总默认图片（最终回退）
            path = $"Content/Textures/CriticalHit/{genderFolder}/{typeFolder}/Default.png";
            System.Diagnostics.Debug.WriteLine($"[暴击图路径] 尝试优先级4（最终回退）: {path}");
            
            PlatformTexture texture = CacheManager.GetTempTexture(path);
            if (texture != null)
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图路径] ✅ 找到优先级4图片");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[暴击图路径] ❌ 所有优先级都未找到图片！");
            }
            
            return path;
        }

        /// <summary>
        /// 获取兵种类型文件夹名称
        /// 🧊 Cold Path
        /// </summary>
        private string GetMilitaryTypeFolder(Troop troop)
        {
            // 获取真实兵种类型（处理变身等特殊情况）
            MilitaryType militaryType = troop.Army.RealMilitaryKind.Type;
            
            return militaryType switch
            {
                MilitaryType.步兵 => "Infantry",      // 步兵
                MilitaryType.弩兵 => "Archer",        // 弓兵（弩兵）
                MilitaryType.骑兵 => "Cavalry",       // 骑兵
                MilitaryType.器械 => "Siege",         // 器械
                MilitaryType.水军 => "Navy",          // 水军
                _ => "Infantry"                       // 默认为步兵
            };
        }

        /// <summary>
        /// 更新所有暴击图
        /// 🧊 Cold Path - 每帧调用但不在热循环中
        /// </summary>
        public void Update(GameTime gameTime)
        {
            double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
            
            // 🔥 性能优化：使用固定数组遍历，零分配
            for (int i = 0; i < MAX_CONCURRENT_ITEMS; i++)
            {
                CriticalHitImageItem item = _itemPool[i];
                
                if (!item.IsActive) continue;
                
                // 更新透明度
                item.Update(currentTime);
                
                // 🔥 性能优化：检查过期并停用，避免 RemoveAll 的闭包开销
                if (item.IsExpired(currentTime))
                {
                    item.Deactivate();
                }
            }
        }

        /// <summary>
        /// 绘制所有暴击图
        /// 🔥 Hot Path - 每帧调用
        /// </summary>
        public void Draw()
        {
            // 🔥 性能优化：使用固定数组遍历，零分配
            for (int i = 0; i < MAX_CONCURRENT_ITEMS; i++)
            {
                _itemPool[i].Draw();
            }
        }

        /// <summary>
        /// 清空所有暴击图
        /// 🧊 Cold Path - 场景切换时调用
        /// </summary>
        public void Clear()
        {
            // 🔥 性能优化：停用所有对象池中的对象
            for (int i = 0; i < MAX_CONCURRENT_ITEMS; i++)
            {
                _itemPool[i].Deactivate();
            }
            
            // 清空路径缓存
            _pathCache.Clear();
        }
    }
}
