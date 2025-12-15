using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameManager
{
    /// <summary>
    /// 视觉系统性能监控器
    /// 🎯 监控和优化视觉系统性能
    /// </summary>
    public class VisualsPerformanceMonitor
    {
        private static VisualsPerformanceMonitor _instance;
        public static VisualsPerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new VisualsPerformanceMonitor();
                return _instance;
            }
        }
        
        // ==========================================
        // 性能计数器
        // ==========================================
        
        /// <summary>
        /// 总单位数量
        /// </summary>
        public int TotalUnits { get; private set; }
        
        /// <summary>
        /// 可见单位数量
        /// </summary>
        public int VisibleUnits { get; private set; }
        
        /// <summary>
        /// 移动中的单位数量
        /// </summary>
        public int MovingUnits { get; private set; }
        
        /// <summary>
        /// 更新耗时（毫秒）
        /// </summary>
        public double UpdateTime { get; private set; }
        
        /// <summary>
        /// 渲染耗时（毫秒）
        /// </summary>
        public double RenderTime { get; private set; }
        
        /// <summary>
        /// 帧率
        /// </summary>
        public float FrameRate { get; private set; }
        
        // ==========================================
        // 性能历史记录
        // ==========================================
        
        private readonly Queue<PerformanceSnapshot> _performanceHistory = new Queue<PerformanceSnapshot>();
        private const int MAX_HISTORY_SIZE = 300; // 5秒历史（60fps）
        
        private readonly Stopwatch _updateStopwatch = new Stopwatch();
        private readonly Stopwatch _renderStopwatch = new Stopwatch();
        private readonly Stopwatch _frameStopwatch = new Stopwatch();
        
        private float _frameTimeAccumulator = 0f;
        private int _frameCount = 0;
        
        public VisualsPerformanceMonitor()
        {
            _frameStopwatch.Start();
        }
        
        // ==========================================
        // 性能监控方法
        // ==========================================
        
        /// <summary>
        /// 开始更新性能监控
        /// </summary>
        public void BeginUpdate()
        {
            _updateStopwatch.Restart();
        }
        
        /// <summary>
        /// 结束更新性能监控
        /// </summary>
        /// <param name="totalUnits">总单位数</param>
        /// <param name="visibleUnits">可见单位数</param>
        /// <param name="movingUnits">移动单位数</param>
        public void EndUpdate(int totalUnits, int visibleUnits, int movingUnits)
        {
            _updateStopwatch.Stop();
            
            TotalUnits = totalUnits;
            VisibleUnits = visibleUnits;
            MovingUnits = movingUnits;
            UpdateTime = _updateStopwatch.Elapsed.TotalMilliseconds;
        }
        
        /// <summary>
        /// 开始渲染性能监控
        /// </summary>
        public void BeginRender()
        {
            _renderStopwatch.Restart();
        }
        
        /// <summary>
        /// 结束渲染性能监控
        /// </summary>
        public void EndRender()
        {
            _renderStopwatch.Stop();
            RenderTime = _renderStopwatch.Elapsed.TotalMilliseconds;
        }
        
        /// <summary>
        /// 更新帧率计算
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void UpdateFrameRate(float deltaTime)
        {
            _frameTimeAccumulator += deltaTime;
            _frameCount++;
            
            // 每秒更新一次帧率
            if (_frameTimeAccumulator >= 1.0f)
            {
                FrameRate = _frameCount / _frameTimeAccumulator;
                _frameTimeAccumulator = 0f;
                _frameCount = 0;
                
                // 记录性能快照
                RecordPerformanceSnapshot();
            }
        }
        
        /// <summary>
        /// 记录性能快照
        /// </summary>
        private void RecordPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                TotalUnits = TotalUnits,
                VisibleUnits = VisibleUnits,
                MovingUnits = MovingUnits,
                UpdateTime = UpdateTime,
                RenderTime = RenderTime,
                FrameRate = FrameRate,
                MemoryUsage = GC.GetTotalMemory(false) / 1024 / 1024 // MB
            };
            
            _performanceHistory.Enqueue(snapshot);
            
            // 保持历史记录大小
            while (_performanceHistory.Count > MAX_HISTORY_SIZE)
            {
                _performanceHistory.Dequeue();
            }
        }
        
        // ==========================================
        // 性能分析
        // ==========================================
        
        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计</returns>
        public PerformanceStats GetPerformanceStats()
        {
            if (_performanceHistory.Count == 0)
            {
                return new PerformanceStats();
            }
            
            var snapshots = _performanceHistory.ToArray();
            
            return new PerformanceStats
            {
                AverageFrameRate = snapshots.Average(s => s.FrameRate),
                MinFrameRate = snapshots.Min(s => s.FrameRate),
                MaxFrameRate = snapshots.Max(s => s.FrameRate),
                
                AverageUpdateTime = snapshots.Average(s => s.UpdateTime),
                MaxUpdateTime = snapshots.Max(s => s.UpdateTime),
                
                AverageRenderTime = snapshots.Average(s => s.RenderTime),
                MaxRenderTime = snapshots.Max(s => s.RenderTime),
                
                AverageVisibleUnits = (int)snapshots.Average(s => s.VisibleUnits),
                MaxVisibleUnits = snapshots.Max(s => s.VisibleUnits),
                
                AverageMovingUnits = (int)snapshots.Average(s => s.MovingUnits),
                MaxMovingUnits = snapshots.Max(s => s.MovingUnits),
                
                CurrentMemoryUsage = snapshots.Length > 0 ? snapshots[snapshots.Length - 1].MemoryUsage : 0,
                PeakMemoryUsage = snapshots.Max(s => s.MemoryUsage)
            };
        }
        
        /// <summary>
        /// 检测性能问题
        /// </summary>
        /// <returns>性能问题列表</returns>
        public List<PerformanceIssue> DetectPerformanceIssues()
        {
            var issues = new List<PerformanceIssue>();
            var stats = GetPerformanceStats();
            
            // 帧率过低
            if (stats.AverageFrameRate < 30)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.LowFrameRate,
                    Severity = PerformanceIssueSeverity.High,
                    Description = $"平均帧率过低: {stats.AverageFrameRate:F1} FPS",
                    Suggestion = "考虑减少可见单位数量或优化渲染"
                });
            }
            
            // 更新时间过长
            if (stats.MaxUpdateTime > 16.67) // 超过60fps的帧时间
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.SlowUpdate,
                    Severity = PerformanceIssueSeverity.Medium,
                    Description = $"更新时间过长: {stats.MaxUpdateTime:F2} ms",
                    Suggestion = "优化单位更新逻辑或使用更激进的视锥剔除"
                });
            }
            
            // 渲染时间过长
            if (stats.MaxRenderTime > 10)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.SlowRender,
                    Severity = PerformanceIssueSeverity.Medium,
                    Description = $"渲染时间过长: {stats.MaxRenderTime:F2} ms",
                    Suggestion = "考虑使用批量渲染或减少绘制调用"
                });
            }
            
            // 内存使用过高
            if (stats.CurrentMemoryUsage > 500) // 500MB
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.HighMemoryUsage,
                    Severity = PerformanceIssueSeverity.Low,
                    Description = $"内存使用过高: {stats.CurrentMemoryUsage:F1} MB",
                    Suggestion = "检查是否有内存泄漏或考虑释放不必要的资源"
                });
            }
            
            return issues;
        }
        
        /// <summary>
        /// 获取性能建议
        /// </summary>
        /// <returns>性能优化建议</returns>
        public List<string> GetOptimizationSuggestions()
        {
            var suggestions = new List<string>();
            var stats = GetPerformanceStats();
            
            // 基于可见单位数量的建议
            if (stats.MaxVisibleUnits > 100)
            {
                suggestions.Add("可见单位数量较多，考虑增大视锥剔除边界或使用LOD系统");
            }
            
            // 基于移动单位数量的建议
            if (stats.MaxMovingUnits > 50)
            {
                suggestions.Add("移动单位数量较多，考虑降低移动更新频率或使用分帧处理");
            }
            
            // 基于帧率的建议
            if (stats.AverageFrameRate < 45)
            {
                suggestions.Add("帧率偏低，建议启用更激进的性能优化选项");
            }
            
            return suggestions;
        }
        
        // ==========================================
        // 调试渲染
        // ==========================================
        
        /// <summary>
        /// 渲染性能信息
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="font">字体</param>
        /// <param name="position">显示位置</param>
        public void RenderPerformanceInfo(SpriteBatch spriteBatch, SpriteFont font, Vector2 position)
        {
            if (font == null) return;
            
            var stats = GetPerformanceStats();
            
            var lines = new[]
            {
                $"FPS: {FrameRate:F1} (Avg: {stats.AverageFrameRate:F1})",
                $"Units: {VisibleUnits}/{TotalUnits} (Moving: {MovingUnits})",
                $"Update: {UpdateTime:F2}ms (Max: {stats.MaxUpdateTime:F2}ms)",
                $"Render: {RenderTime:F2}ms (Max: {stats.MaxRenderTime:F2}ms)",
                $"Memory: {stats.CurrentMemoryUsage:F1}MB"
            };
            
            for (int i = 0; i < lines.Length; i++)
            {
                var linePos = position + new Vector2(0, i * 20);
                spriteBatch.DrawString(font, lines[i], linePos, Color.Yellow);
            }
        }
        
        /// <summary>
        /// 渲染性能图表
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="texture">1像素白色纹理</param>
        /// <param name="bounds">图表边界</param>
        public void RenderPerformanceGraph(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds)
        {
            if (texture == null || _performanceHistory.Count < 2) return;
            
            var snapshots = _performanceHistory.ToArray();
            
            // 绘制帧率图表
            DrawGraph(spriteBatch, texture, bounds, snapshots.Select(s => s.FrameRate).ToArray(), 
                     Color.Green, 0f, 60f);
        }
        
        /// <summary>
        /// 绘制图表
        /// </summary>
        private void DrawGraph(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds, 
                              float[] values, Color color, float minValue, float maxValue)
        {
            if (values.Length < 2) return;
            
            float width = bounds.Width;
            float height = bounds.Height;
            float stepX = width / (values.Length - 1);
            
            for (int i = 0; i < values.Length - 1; i++)
            {
                float x1 = bounds.X + i * stepX;
                float y1 = bounds.Y + height - ((values[i] - minValue) / (maxValue - minValue)) * height;
                
                float x2 = bounds.X + (i + 1) * stepX;
                float y2 = bounds.Y + height - ((values[i + 1] - minValue) / (maxValue - minValue)) * height;
                
                DrawLine(spriteBatch, texture, new Vector2(x1, y1), new Vector2(x2, y2), color, 1f);
            }
        }
        
        /// <summary>
        /// 绘制线段
        /// </summary>
        private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, float thickness)
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
    
    // ==========================================
    // 数据结构
    // ==========================================
    
    /// <summary>
    /// 性能快照
    /// </summary>
    public struct PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public int TotalUnits { get; set; }
        public int VisibleUnits { get; set; }
        public int MovingUnits { get; set; }
        public double UpdateTime { get; set; }
        public double RenderTime { get; set; }
        public float FrameRate { get; set; }
        public long MemoryUsage { get; set; }
    }
    
    /// <summary>
    /// 性能统计
    /// </summary>
    public struct PerformanceStats
    {
        public float AverageFrameRate { get; set; }
        public float MinFrameRate { get; set; }
        public float MaxFrameRate { get; set; }
        
        public double AverageUpdateTime { get; set; }
        public double MaxUpdateTime { get; set; }
        
        public double AverageRenderTime { get; set; }
        public double MaxRenderTime { get; set; }
        
        public int AverageVisibleUnits { get; set; }
        public int MaxVisibleUnits { get; set; }
        
        public int AverageMovingUnits { get; set; }
        public int MaxMovingUnits { get; set; }
        
        public long CurrentMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
    }
    
    /// <summary>
    /// 性能问题
    /// </summary>
    public struct PerformanceIssue
    {
        public PerformanceIssueType Type { get; set; }
        public PerformanceIssueSeverity Severity { get; set; }
        public string Description { get; set; }
        public string Suggestion { get; set; }
    }
    
    /// <summary>
    /// 性能问题类型
    /// </summary>
    public enum PerformanceIssueType
    {
        LowFrameRate,
        SlowUpdate,
        SlowRender,
        HighMemoryUsage
    }
    
    /// <summary>
    /// 性能问题严重程度
    /// </summary>
    public enum PerformanceIssueSeverity
    {
        Low,
        Medium,
        High
    }
}