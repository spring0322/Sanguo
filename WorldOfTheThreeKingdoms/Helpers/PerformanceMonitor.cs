using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameManager;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// 性能监控器 - 显示缓存状态、帧率、内存使用等信息
    /// </summary>
    public static class PerformanceMonitor
    {
        private static bool _isEnabled = false;
        private static Stopwatch _frameTimer = new Stopwatch();
        private static int _frameCount = 0;
        private static double _fps = 0;
        private static DateTime _lastUpdate = DateTime.Now;
        
        // 显示位置
        private static Vector2 _displayPosition = new Vector2(10, 10);
        
        /// <summary>
        /// 启用/禁用性能监控显示
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 设置显示位置
        /// </summary>
        public static Vector2 DisplayPosition
        {
            get => _displayPosition;
            set => _displayPosition = value;
        }

        /// <summary>
        /// 更新性能统计（每帧调用）
        /// </summary>
        public static void Update(GameTime gameTime)
        {
            if (!_isEnabled) return;

            _frameCount++;
            
            // 每秒更新一次 FPS
            var now = DateTime.Now;
            var elapsed = (now - _lastUpdate).TotalSeconds;
            if (elapsed >= 1.0)
            {
                _fps = _frameCount / elapsed;
                _frameCount = 0;
                _lastUpdate = now;
            }
        }

        /// <summary>
        /// 绘制性能信息
        /// </summary>
        public static void Draw(GameTime gameTime)
        {
            if (!_isEnabled) return;

            try
            {
                var info = GetPerformanceInfo();
                var lines = info.Split('\n');
                
                var position = _displayPosition;
                var lineHeight = 20f;
                
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line.Trim()))
                    {
                        CacheManager.DrawString(
                            Session.Current.Font, 
                            line, 
                            position, 
                            Color.Yellow, 
                            0f, 
                            Vector2.Zero, 
                            0.8f, 
                            SpriteEffects.None, 
                            1f
                        );
                        position.Y += lineHeight;
                    }
                }
            }
            catch (Exception ex)
            {
                // 避免性能监控本身导致崩溃
                System.Diagnostics.Debug.WriteLine($"[PerformanceMonitor] 绘制错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取性能信息字符串
        /// </summary>
        public static string GetPerformanceInfo()
        {
            var info = new System.Text.StringBuilder();
            
            // FPS 信息
            info.AppendLine($"FPS: {_fps:F1}");
            
            // 缓存信息
            var cacheStats = CacheManager.GetCacheStats();
            info.AppendLine($"Cache: {cacheStats}");
            
            // 内存信息
            var gcMemory = GC.GetTotalMemory(false) / 1024 / 1024;
            info.AppendLine($"GC Memory: {gcMemory} MB");
            
            // 垃圾回收信息
            info.AppendLine($"GC Gen0: {GC.CollectionCount(0)}");
            info.AppendLine($"GC Gen1: {GC.CollectionCount(1)}");
            info.AppendLine($"GC Gen2: {GC.CollectionCount(2)}");
            
            return info.ToString();
        }

        /// <summary>
        /// 切换显示状态
        /// </summary>
        public static void Toggle()
        {
            _isEnabled = !_isEnabled;
            System.Diagnostics.Debug.WriteLine($"[PerformanceMonitor] 性能监控显示: {(_isEnabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 强制垃圾回收（调试用）
        /// </summary>
        public static void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            System.Diagnostics.Debug.WriteLine("[PerformanceMonitor] 强制垃圾回收完成");
        }
    }
}