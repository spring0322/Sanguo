using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// UI头像显示系统 - 将军师头像固定显示在屏幕上
    /// </summary>
    public class UIPortraitSystem
    {
        #region 私有字段
        
        private static Texture2D _cachedPortrait = null;
        private static Texture2D _fallbackTexture = null;
        private static int _lastAdvisorID = -1;
        
        private Vector2 _position = new Vector2(300, 150); // 默认位置，避开左上角日期面板
        private float _alpha = 1.0f;
        private bool _visible = true;
        private float _scale = 1.0f;
        
        #endregion
        
        #region 构造函数
        
        public UIPortraitSystem()
        {
            System.Diagnostics.Debug.WriteLine("[UIPortraitSystem] 系统已初始化");
        }
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 头像显示位置
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }
        
        /// <summary>
        /// 透明度 (0.0 - 1.0)
        /// </summary>
        public float Alpha
        {
            get { return _alpha; }
            set { _alpha = MathHelper.Clamp(value, 0f, 1f); }
        }
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }
        
        /// <summary>
        /// 缩放比例
        /// </summary>
        public float Scale
        {
            get { return _scale; }
            set { _scale = Math.Max(0.1f, value); }
        }
        
        #endregion
        
        #region 核心方法
        
        /// <summary>
        /// 绘制UI头像
        /// </summary>
        public void Draw()
        {
            if (!_visible || !ShouldDraw())
                return;
                
            var spriteBatch = Session.Current?.SpriteBatch;
            if (spriteBatch == null)
                return;
                
            bool wasDrawing = false;
            
            try
            {
                // 安全地管理SpriteBatch状态
                try 
                { 
                    spriteBatch.End(); 
                    wasDrawing = true; 
                } 
                catch { }
                
                // 开始屏幕坐标系绘制 - 关键：不使用Camera.Transform
                spriteBatch.Begin(
                    SpriteSortMode.Deferred, 
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise
                    // 注意：这里不传Camera.Transform，使用默认的屏幕坐标系
                );
                
                var texture = GetAdvisorPortrait();
                if (texture != null)
                {
                    var color = Color.White * _alpha;
                    var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
                    
                    spriteBatch.Draw(
                        texture, 
                        _position, 
                        null, 
                        color, 
                        0f, 
                        origin, 
                        _scale, 
                        SpriteEffects.None, 
                        0f
                    );
                }
                
                spriteBatch.End();
                
                // 恢复之前的绘制状态
                if (wasDrawing)
                {
                    spriteBatch.Begin();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIPortraitSystem] 绘制错误: {ex.Message}");
                
                // 确保SpriteBatch状态正确
                try { spriteBatch.End(); } catch { }
                if (wasDrawing)
                {
                    try { spriteBatch.Begin(); } catch { }
                }
            }
        }
        
        /// <summary>
        /// 获取军师头像纹理
        /// </summary>
        private Texture2D GetAdvisorPortrait()
        {
            try
            {
                if (Session.Current?.CurrentPlayerFaction?.AdvisorID != null)
                {
                    int advisorID = Session.Current.CurrentPlayerFaction.AdvisorID;
                    
                    // 缓存机制，避免重复加载
                    if (_lastAdvisorID != advisorID || _cachedPortrait == null)
                    {
                        var advisor = Session.Current.Scenario.Persons.GetGameObject(advisorID) as Person;
                        if (advisor != null)
                        {
                            _cachedPortrait = ResourceManager.GetPortrait(advisor.ID);
                            _lastAdvisorID = advisorID;
                            
                            if (_cachedPortrait != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[UIPortraitSystem] 加载军师头像: {advisor.Name}");
                                return _cachedPortrait;
                            }
                        }
                    }
                    else if (_cachedPortrait != null)
                    {
                        return _cachedPortrait;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIPortraitSystem] 获取头像失败: {ex.Message}");
            }
            
            return GetFallbackTexture();
        }
        
        /// <summary>
        /// 获取兜底纹理（红色方块）
        /// </summary>
        private Texture2D GetFallbackTexture()
        {
            if (_fallbackTexture == null)
            {
                try
                {
                    _fallbackTexture = new Texture2D(Platform.GraphicsDevice, 64, 64);
                    Color[] colors = new Color[64 * 64];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = Color.Red;
                    _fallbackTexture.SetData(colors);
                    
                    System.Diagnostics.Debug.WriteLine("[UIPortraitSystem] 创建兜底纹理");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UIPortraitSystem] 创建兜底纹理失败: {ex.Message}");
                }
            }
            return _fallbackTexture;
        }
        
        /// <summary>
        /// 检查是否应该绘制
        /// </summary>
        private bool ShouldDraw()
        {
            return Session.Current?.CurrentPlayerFaction?.AdvisorID != null &&
                   Session.Current?.SpriteBatch != null &&
                   Platform.GraphicsDevice != null;
        }
        
        #endregion
        
        #region 便捷方法
        
        /// <summary>
        /// 设置位置到屏幕角落
        /// </summary>
        public void SetCornerPosition(Corner corner, Vector2 offset = default)
        {
            var viewport = Session.Current?.ViewportSize ?? new Point(1024, 768);
            
            switch (corner)
            {
                case Corner.TopLeft:
                    _position = new Vector2(50, 50) + offset;
                    break;
                case Corner.TopRight:
                    _position = new Vector2(viewport.X - 100, 50) + offset;
                    break;
                case Corner.BottomLeft:
                    _position = new Vector2(50, viewport.Y - 100) + offset;
                    break;
                case Corner.BottomRight:
                    _position = new Vector2(viewport.X - 100, viewport.Y - 100) + offset;
                    break;
                case Corner.Center:
                    _position = new Vector2(viewport.X / 2, viewport.Y / 2) + offset;
                    break;
            }
        }
        
        /// <summary>
        /// 淡入效果
        /// </summary>
        public void FadeIn(float speed = 2.0f)
        {
            // 这里可以实现淡入动画，暂时直接设置为可见
            _alpha = 1.0f;
            _visible = true;
        }
        
        /// <summary>
        /// 淡出效果
        /// </summary>
        public void FadeOut(float speed = 2.0f)
        {
            // 这里可以实现淡出动画，暂时直接设置为不可见
            _alpha = 0.0f;
            _visible = false;
        }
        
        /// <summary>
        /// 重置缓存（当军师更换时调用）
        /// </summary>
        public static void ResetCache()
        {
            _cachedPortrait = null;
            _lastAdvisorID = -1;
            System.Diagnostics.Debug.WriteLine("[UIPortraitSystem] 缓存已重置");
        }
        
        #endregion
        
        #region 清理资源
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            try
            {
                if (_fallbackTexture != null && !_fallbackTexture.IsDisposed)
                {
                    _fallbackTexture.Dispose();
                    _fallbackTexture = null;
                }
                
                // 注意：_cachedPortrait 由ResourceManager管理，不需要手动释放
                _cachedPortrait = null;
                _lastAdvisorID = -1;
                
                System.Diagnostics.Debug.WriteLine("[UIPortraitSystem] 资源已清理");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIPortraitSystem] 清理资源时出错: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 屏幕角落枚举
    /// </summary>
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }
}