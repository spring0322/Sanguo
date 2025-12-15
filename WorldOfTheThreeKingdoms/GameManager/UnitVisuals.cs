using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 单位视觉组件 - 基于你的设计实现
    /// 🎯 核心功能：
    /// 1. 分离逻辑位置和视觉位置
    /// 2. 平滑的视觉移动动画
    /// 3. 挂在单位身上的独立组件
    /// </summary>
    public class UnitVisuals
    {
        // ==========================================
        // 视觉位置系统
        // ==========================================
        
        /// <summary>
        /// 当前视觉位置 - 屏幕像素坐标
        /// </summary>
        private Vector2 _currentVisualPos;
        
        /// <summary>
        /// 目标视觉位置 - 目标像素坐标
        /// </summary>
        private Vector2 _targetVisualPos;
        
        /// <summary>
        /// 移动速度 - 像素/秒
        /// </summary>
        private float _moveSpeed = 200f;
        
        /// <summary>
        /// 所属单位
        /// </summary>
        private readonly Troop _owner;
        
        /// <summary>
        /// 精灵渲染信息
        /// </summary>
        private SpriteRenderData _sprite;
        
        // ==========================================
        // 坐标转换系统
        // ==========================================
        
        /// <summary>
        /// 瓦片大小（像素）
        /// </summary>
        private const int TILE_SIZE = 32;
        
        /// <summary>
        /// 地图偏移量
        /// </summary>
        private Vector2 _mapOffset = Vector2.Zero;
        
        // ==========================================
        // 动画系统
        // ==========================================
        
        /// <summary>
        /// 移动动画状态
        /// </summary>
        public enum AnimationState
        {
            Idle,       // 静止
            Moving,     // 移动中
            Attacking,  // 攻击中
            Defending   // 防御中
        }
        
        public AnimationState CurrentAnimation { get; private set; } = AnimationState.Idle;
        
        /// <summary>
        /// 移动方向（用于选择动画帧）
        /// </summary>
        private Vector2 _moveDirection = Vector2.Zero;
        
        public UnitVisuals(Troop owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            
            // 初始化视觉位置
            var initialGridPos = owner.Position;
            _currentVisualPos = GridToWorld(initialGridPos);
            _targetVisualPos = _currentVisualPos;
            
            // 初始化精灵数据
            _sprite = new SpriteRenderData
            {
                Position = _currentVisualPos,
                Texture = null, // 需要从 TextureManager 获取
                Scale = Vector2.One,
                Rotation = 0f,
                Color = Color.White,
                LayerDepth = 0.5f
            };
            
            LoadTexture();
        }
        
        /// <summary>
        /// 🎯 核心方法：移动到指定网格位置
        /// 逻辑坐标瞬间改变，但视觉目标慢慢追
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        public void MoveTo(Point gridPos)
        {
            // 计算新的目标视觉位置
            Vector2 newTargetPos = GridToWorld(gridPos);
            
            // 如果位置发生变化，开始移动动画
            if (Vector2.Distance(_targetVisualPos, newTargetPos) > 0.1f)
            {
                _targetVisualPos = newTargetPos;
                
                // 计算移动方向
                _moveDirection = _targetVisualPos - _currentVisualPos;
                if (_moveDirection.Length() > 0)
                {
                    _moveDirection.Normalize();
                }
                
                // 切换到移动动画
                CurrentAnimation = AnimationState.Moving;
                
                System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 单位 {_owner.ID} 开始移动到 {gridPos}");
            }
        }
        
        /// <summary>
        /// 🎯 核心更新方法：平滑视觉移动
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void Update(float deltaTime)
        {
            // 简单的线性插值 (Lerp) 或 MoveTowards
            if (_currentVisualPos != _targetVisualPos)
            {
                // MoveTowards 保证匀速移动，Lerp 会有快慢
                Vector2 direction = _targetVisualPos - _currentVisualPos;
                float distance = direction.Length();
                
                if (distance > 0)
                {
                    direction.Normalize();
                    float moveDistance = _moveSpeed * deltaTime;
                    
                    if (moveDistance >= distance)
                    {
                        _currentVisualPos = _targetVisualPos;
                    }
                    else
                    {
                        _currentVisualPos += direction * moveDistance;
                    }
                }
                
                // 更新 Sprite 绘制坐标
                _sprite.Position = _currentVisualPos;
                
                // 检查是否到达目标
                if (Vector2.Distance(_currentVisualPos, _targetVisualPos) < 0.1f)
                {
                    _currentVisualPos = _targetVisualPos;
                    CurrentAnimation = AnimationState.Idle;
                    
                    System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 单位 {_owner.ID} 视觉移动完成");
                }
            }
            
            // 更新动画帧
            UpdateAnimation(deltaTime);
        }
        
        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <returns>世界像素坐标</returns>
        private Vector2 GridToWorld(Point gridPos)
        {
            return new Vector2(
                gridPos.X * TILE_SIZE + _mapOffset.X,
                gridPos.Y * TILE_SIZE + _mapOffset.Y
            );
        }
        
        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        /// <param name="worldPos">世界像素坐标</param>
        /// <returns>网格坐标</returns>
        private Point WorldToGrid(Vector2 worldPos)
        {
            return new Point(
                (int)((worldPos.X - _mapOffset.X) / TILE_SIZE),
                (int)((worldPos.Y - _mapOffset.Y) / TILE_SIZE)
            );
        }
        
        /// <summary>
        /// 设置地图偏移量（用于摄像机跟随）
        /// </summary>
        /// <param name="offset">偏移量</param>
        public void SetMapOffset(Vector2 offset)
        {
            Vector2 deltaOffset = offset - _mapOffset;
            _mapOffset = offset;
            
            // 同步更新视觉位置
            _currentVisualPos += deltaOffset;
            _targetVisualPos += deltaOffset;
            _sprite.Position = _currentVisualPos;
        }
        
        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="pixelsPerSecond">像素/秒</param>
        public void SetMoveSpeed(float pixelsPerSecond)
        {
            _moveSpeed = Math.Max(1f, pixelsPerSecond);
        }
        
        /// <summary>
        /// 获取当前视觉位置
        /// </summary>
        /// <returns>当前视觉位置（像素坐标）</returns>
        public Vector2 GetVisualPosition()
        {
            return _currentVisualPos;
        }
        
        /// <summary>
        /// 获取当前视觉网格位置
        /// </summary>
        /// <returns>当前视觉网格位置</returns>
        public Point GetVisualGridPosition()
        {
            return WorldToGrid(_currentVisualPos);
        }
        
        /// <summary>
        /// 检查是否正在移动
        /// </summary>
        /// <returns>true表示正在移动</returns>
        public bool IsMoving()
        {
            return CurrentAnimation == AnimationState.Moving;
        }
        
        /// <summary>
        /// 立即同步到目标位置（跳过动画）
        /// </summary>
        public void SnapToTarget()
        {
            _currentVisualPos = _targetVisualPos;
            _sprite.Position = _currentVisualPos;
            CurrentAnimation = AnimationState.Idle;
        }
        
        /// <summary>
        /// 强制同步到逻辑位置
        /// </summary>
        public void SyncToLogicPosition()
        {
            var logicPos = GridToWorld(_owner.Position);
            _currentVisualPos = logicPos;
            _targetVisualPos = logicPos;
            _sprite.Position = _currentVisualPos;
            CurrentAnimation = AnimationState.Idle;
        }
        
        // ==========================================
        // 动画系统
        // ==========================================
        
        /// <summary>
        /// 动画计时器
        /// </summary>
        private float _animationTimer = 0f;
        
        /// <summary>
        /// 动画帧索引
        /// </summary>
        private int _animationFrame = 0;
        
        /// <summary>
        /// 动画帧间隔
        /// </summary>
        private const float ANIMATION_FRAME_TIME = 0.2f; // 200ms per frame
        
        /// <summary>
        /// 更新动画
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        private void UpdateAnimation(float deltaTime)
        {
            _animationTimer += deltaTime;
            
            if (_animationTimer >= ANIMATION_FRAME_TIME)
            {
                _animationTimer = 0f;
                
                switch (CurrentAnimation)
                {
                    case AnimationState.Moving:
                        // 移动动画：循环播放移动帧
                        _animationFrame = (_animationFrame + 1) % 4; // 假设有4帧移动动画
                        break;
                        
                    case AnimationState.Idle:
                        // 静止动画：可能有呼吸效果
                        _animationFrame = (_animationFrame + 1) % 2; // 假设有2帧静止动画
                        break;
                }
                
                // 更新纹理区域（如果使用精灵表）
                UpdateTextureRegion();
            }
        }
        
        /// <summary>
        /// 更新纹理区域
        /// </summary>
        private void UpdateTextureRegion()
        {
            // 根据动画状态和方向选择纹理区域
            // 这里需要根据实际的精灵表布局调整
            
            int frameWidth = 32;  // 单帧宽度
            int frameHeight = 32; // 单帧高度
            
            // 根据移动方向选择行
            int row = GetAnimationRow();
            
            _sprite.SourceRectangle = new Rectangle(
                _animationFrame * frameWidth,
                row * frameHeight,
                frameWidth,
                frameHeight
            );
        }
        
        /// <summary>
        /// 根据移动方向获取动画行
        /// </summary>
        /// <returns>动画行索引</returns>
        private int GetAnimationRow()
        {
            if (CurrentAnimation == AnimationState.Idle)
                return 0; // 静止动画在第0行
            
            // 根据移动方向选择动画行
            if (Math.Abs(_moveDirection.X) > Math.Abs(_moveDirection.Y))
            {
                // 水平移动
                return _moveDirection.X > 0 ? 1 : 3; // 右移动第1行，左移动第3行
            }
            else
            {
                // 垂直移动
                return _moveDirection.Y > 0 ? 2 : 4; // 下移动第2行，上移动第4行
            }
        }
        
        /// <summary>
        /// 加载纹理
        /// </summary>
        private void LoadTexture()
        {
            try
            {
                // 根据单位类型加载对应的纹理
                // 这里需要根据实际的纹理管理系统调整
                
                string textureName = GetTextureNameForUnit();
                // _sprite.Texture = TextureManager.LoadTexture(textureName);
                
                System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 为单位 {_owner.ID} 加载纹理: {textureName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 加载纹理失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取单位对应的纹理名称
        /// </summary>
        /// <returns>纹理名称</returns>
        private string GetTextureNameForUnit()
        {
            // 根据单位类型返回对应的纹理名称
            if (_owner.Army?.Kind != null)
            {
                string kindName = _owner.Army.Kind.ToString();
                
                if (kindName.Contains("骑兵"))
                    return "cavalry_sprite";
                else if (kindName.Contains("弓兵"))
                    return "archer_sprite";
                else if (kindName.Contains("水军"))
                    return "navy_sprite";
                else if (kindName.Contains("器械"))
                    return "siege_sprite";
            }
            
            return "infantry_sprite"; // 默认步兵
        }
        
        // ==========================================
        // 渲染接口
        // ==========================================
        
        /// <summary>
        /// 获取渲染数据
        /// </summary>
        /// <returns>精灵渲染数据</returns>
        public SpriteRenderData GetRenderData()
        {
            return _sprite;
        }
        
        /// <summary>
        /// 渲染单位（如果需要自定义渲染）
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        public void Render(SpriteBatch spriteBatch)
        {
            if (_sprite.Texture != null)
            {
                spriteBatch.Draw(
                    _sprite.Texture,
                    _sprite.Position,
                    _sprite.SourceRectangle,
                    _sprite.Color,
                    _sprite.Rotation,
                    Vector2.Zero,
                    _sprite.Scale,
                    SpriteEffects.None,
                    _sprite.LayerDepth
                );
            }
        }
    }
    
    /// <summary>
    /// 精灵渲染数据
    /// </summary>
    public class SpriteRenderData
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public Rectangle? SourceRectangle { get; set; }
        public Color Color { get; set; } = Color.White;
        public float Rotation { get; set; } = 0f;
        public Vector2 Scale { get; set; } = Vector2.One;
        public float LayerDepth { get; set; } = 0f;
    }
    
    /// <summary>
    /// UnitMovement 的扩展方法 - 集成视觉系统
    /// </summary>
    public static class UnitMovementVisualsExtensions
    {
        /// <summary>
        /// 为 UnitMovement 添加视觉组件
        /// </summary>
        /// <param name="unitMovement">单位移动组件</param>
        /// <param name="visuals">视觉组件</param>
        public static void AttachVisuals(this UnitMovement unitMovement, UnitVisuals visuals)
        {
            // 这里可以添加事件监听，当逻辑位置改变时自动更新视觉位置
            // 实际实现需要在 UnitMovement 中添加位置改变事件
        }
    }
}