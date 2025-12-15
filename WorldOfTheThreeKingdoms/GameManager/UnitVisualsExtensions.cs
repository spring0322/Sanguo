using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 单位视觉系统扩展功能
    /// 🎯 提供高级视觉效果和实用工具
    /// </summary>
    public static class UnitVisualsExtensions
    {
        // ==========================================
        // 高级移动效果
        // ==========================================
        
        /// <summary>
        /// 平滑移动到目标位置（带缓动效果）
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="gridPos">目标网格位置</param>
        /// <param name="easeType">缓动类型</param>
        public static void MoveToWithEasing(this UnitVisuals visuals, Point gridPos, EaseType easeType = EaseType.Linear)
        {
            // 这里可以实现不同的缓动效果
            // 目前先使用标准移动
            visuals.MoveTo(gridPos);
        }
        
        /// <summary>
        /// 沿路径移动
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="path">路径点列表</param>
        /// <param name="speed">移动速度</param>
        public static void MoveAlongPath(this UnitVisuals visuals, List<Point> path, float speed = 200f)
        {
            if (path == null || path.Count == 0) return;
            
            // 设置移动速度
            visuals.SetMoveSpeed(speed);
            
            // 开始移动到第一个点
            // 实际实现需要在 UnitVisuals 中添加路径跟随功能
            visuals.MoveTo(path[0]);
        }
        
        // ==========================================
        // 视觉效果
        // ==========================================
        
        /// <summary>
        /// 添加移动轨迹效果
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="trailLength">轨迹长度</param>
        /// <param name="trailColor">轨迹颜色</param>
        public static void EnableMovementTrail(this UnitVisuals visuals, int trailLength = 5, Color? trailColor = null)
        {
            // 实现移动轨迹效果
            // 需要在 UnitVisuals 中添加轨迹系统
            System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 启用移动轨迹效果: 长度={trailLength}");
        }
        
        /// <summary>
        /// 添加选中高亮效果
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="highlightColor">高亮颜色</param>
        public static void SetHighlight(this UnitVisuals visuals, Color highlightColor)
        {
            var renderData = visuals.GetRenderData();
            renderData.Color = highlightColor;
        }
        
        /// <summary>
        /// 移除高亮效果
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        public static void RemoveHighlight(this UnitVisuals visuals)
        {
            var renderData = visuals.GetRenderData();
            renderData.Color = Color.White;
        }
        
        // ==========================================
        // 动画控制
        // ==========================================
        
        /// <summary>
        /// 播放攻击动画
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="targetPosition">攻击目标位置</param>
        public static void PlayAttackAnimation(this UnitVisuals visuals, Vector2 targetPosition)
        {
            // 实现攻击动画
            // 需要在 UnitVisuals 中添加动画状态控制
            System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 播放攻击动画，目标: {targetPosition}");
        }
        
        /// <summary>
        /// 播放受击动画
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="damage">伤害值</param>
        public static void PlayHitAnimation(this UnitVisuals visuals, int damage)
        {
            // 实现受击动画（闪烁、震动等）
            System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 播放受击动画，伤害: {damage}");
        }
        
        // ==========================================
        // 状态指示器
        // ==========================================
        
        /// <summary>
        /// 显示血条
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="currentHP">当前血量</param>
        /// <param name="maxHP">最大血量</param>
        public static void ShowHealthBar(this UnitVisuals visuals, int currentHP, int maxHP)
        {
            // 实现血条显示
            float healthPercent = (float)currentHP / maxHP;
            System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 显示血条: {healthPercent:P}");
        }
        
        /// <summary>
        /// 显示状态图标
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="statusIcons">状态图标列表</param>
        public static void ShowStatusIcons(this UnitVisuals visuals, List<string> statusIcons)
        {
            // 实现状态图标显示
            System.Diagnostics.Debug.WriteLine($"[UnitVisuals] 显示状态图标: {string.Join(", ", statusIcons)}");
        }
    }
    
    /// <summary>
    /// 缓动类型枚举
    /// </summary>
    public enum EaseType
    {
        Linear,     // 线性
        EaseIn,     // 缓入
        EaseOut,    // 缓出
        EaseInOut   // 缓入缓出
    }
    
    /// <summary>
    /// 视觉效果管理器
    /// 🎯 管理全局视觉效果和特效
    /// </summary>
    public class VisualEffectsManager
    {
        private static VisualEffectsManager _instance;
        public static VisualEffectsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new VisualEffectsManager();
                return _instance;
            }
        }
        
        private readonly List<VisualEffect> _activeEffects = new List<VisualEffect>();
        
        /// <summary>
        /// 添加视觉效果
        /// </summary>
        /// <param name="effect">视觉效果</param>
        public void AddEffect(VisualEffect effect)
        {
            _activeEffects.Add(effect);
        }
        
        /// <summary>
        /// 更新所有视觉效果
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void Update(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);
                
                if (effect.IsFinished)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 渲染所有视觉效果
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        public void Render(SpriteBatch spriteBatch)
        {
            foreach (var effect in _activeEffects)
            {
                effect.Render(spriteBatch);
            }
        }
        
        /// <summary>
        /// 创建移动轨迹效果
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="endPos">结束位置</param>
        /// <param name="color">轨迹颜色</param>
        /// <param name="duration">持续时间</param>
        public void CreateMovementTrail(Vector2 startPos, Vector2 endPos, Color color, float duration = 1.0f)
        {
            var trail = new MovementTrailEffect(startPos, endPos, color, duration);
            AddEffect(trail);
        }
        
        /// <summary>
        /// 创建伤害数字效果
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="damage">伤害值</param>
        /// <param name="color">颜色</param>
        public void CreateDamageNumber(Vector2 position, int damage, Color color)
        {
            var damageEffect = new DamageNumberEffect(position, damage, color);
            AddEffect(damageEffect);
        }
    }
    
    /// <summary>
    /// 视觉效果基类
    /// </summary>
    public abstract class VisualEffect
    {
        public bool IsFinished { get; protected set; }
        
        public abstract void Update(float deltaTime);
        public abstract void Render(SpriteBatch spriteBatch);
    }
    
    /// <summary>
    /// 移动轨迹效果
    /// </summary>
    public class MovementTrailEffect : VisualEffect
    {
        private readonly Vector2 _startPos;
        private readonly Vector2 _endPos;
        private readonly Color _color;
        private readonly float _duration;
        private float _elapsed;
        
        public MovementTrailEffect(Vector2 startPos, Vector2 endPos, Color color, float duration)
        {
            _startPos = startPos;
            _endPos = endPos;
            _color = color;
            _duration = duration;
        }
        
        public override void Update(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= _duration)
            {
                IsFinished = true;
            }
        }
        
        public override void Render(SpriteBatch spriteBatch)
        {
            // 实现轨迹渲染
            // 这里需要绘制从起点到终点的轨迹线
        }
    }
    
    /// <summary>
    /// 伤害数字效果
    /// </summary>
    public class DamageNumberEffect : VisualEffect
    {
        private readonly Vector2 _startPosition;
        private readonly int _damage;
        private readonly Color _color;
        private Vector2 _currentPosition;
        private float _elapsed;
        private const float DURATION = 2.0f;
        
        public DamageNumberEffect(Vector2 position, int damage, Color color)
        {
            _startPosition = position;
            _currentPosition = position;
            _damage = damage;
            _color = color;
        }
        
        public override void Update(float deltaTime)
        {
            _elapsed += deltaTime;
            
            // 向上飘动
            _currentPosition.Y -= 50f * deltaTime;
            
            if (_elapsed >= DURATION)
            {
                IsFinished = true;
            }
        }
        
        public override void Render(SpriteBatch spriteBatch)
        {
            // 实现伤害数字渲染
            // 这里需要绘制伤害数字文本
        }
    }
    
    /// <summary>
    /// 单位视觉调试工具
    /// </summary>
    public static class UnitVisualsDebugger
    {
        /// <summary>
        /// 绘制单位的调试信息
        /// </summary>
        /// <param name="visuals">视觉组件</param>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="font">字体</param>
        public static void DrawDebugInfo(UnitVisuals visuals, SpriteBatch spriteBatch, SpriteFont font)
        {
            if (font == null) return;
            
            var position = visuals.GetVisualPosition();
            var gridPos = visuals.GetVisualGridPosition();
            var isMoving = visuals.IsMoving();
            
            string debugText = $"Pos: {gridPos}\nMoving: {isMoving}\nAnim: {visuals.CurrentAnimation}";
            
            spriteBatch.DrawString(font, debugText, position + new Vector2(0, -40), Color.Yellow);
        }
        
        /// <summary>
        /// 绘制移动路径
        /// </summary>
        /// <param name="path">路径点列表</param>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="texture">1像素白色纹理</param>
        public static void DrawPath(List<Point> path, SpriteBatch spriteBatch, Texture2D texture)
        {
            if (path == null || path.Count < 2 || texture == null) return;
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                var start = new Vector2(path[i].X * 32, path[i].Y * 32);
                var end = new Vector2(path[i + 1].X * 32, path[i + 1].Y * 32);
                
                DrawLine(spriteBatch, texture, start, end, Color.Green, 2f);
            }
        }
        
        /// <summary>
        /// 绘制线段
        /// </summary>
        private static void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            spriteBatch.Draw(
                texture,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f
            );
        }
    }
}