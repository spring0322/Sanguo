using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// Performance monitoring system for Quadtree and rendering optimizations
    /// </summary>
    public static class PerformanceMonitor
    {
        private static Stopwatch _frameTimer = new Stopwatch();
        private static Queue<double> _frameTimes = new Queue<double>();
        private static Queue<int> _troopCounts = new Queue<int>();
        private static Queue<int> _culledCounts = new Queue<int>();
        
        private const int SAMPLE_SIZE = 60; // Monitor last 60 frames
        
        public static double AverageFrameTime { get; private set; }
        public static double AverageTroopCount { get; private set; }
        public static double AverageCulledCount { get; private set; }
        public static double CullingEfficiency { get; private set; }
        
        public static bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Start timing a frame
        /// </summary>
        public static void StartFrame()
        {
            if (!IsEnabled) return;
            _frameTimer.Restart();
        }

        /// <summary>
        /// End timing a frame and record troop statistics
        /// </summary>
        /// <param name="totalTroops">Total number of troops in scenario</param>
        /// <param name="culledTroops">Number of troops culled by optimization</param>
        public static void EndFrame(int totalTroops, int culledTroops)
        {
            if (!IsEnabled) return;
            
            _frameTimer.Stop();
            
            // Record frame time
            _frameTimes.Enqueue(_frameTimer.Elapsed.TotalMilliseconds);
            if (_frameTimes.Count > SAMPLE_SIZE)
                _frameTimes.Dequeue();
            
            // Record troop statistics
            _troopCounts.Enqueue(totalTroops);
            if (_troopCounts.Count > SAMPLE_SIZE)
                _troopCounts.Dequeue();
                
            _culledCounts.Enqueue(culledTroops);
            if (_culledCounts.Count > SAMPLE_SIZE)
                _culledCounts.Dequeue();
            
            // Calculate averages
            UpdateAverages();
        }

        private static void UpdateAverages()
        {
            if (_frameTimes.Count == 0) return;
            
            double totalFrameTime = 0;
            double totalTroops = 0;
            double totalCulled = 0;
            
            foreach (double frameTime in _frameTimes)
                totalFrameTime += frameTime;
                
            foreach (int troopCount in _troopCounts)
                totalTroops += troopCount;
                
            foreach (int culledCount in _culledCounts)
                totalCulled += culledCount;
            
            AverageFrameTime = totalFrameTime / _frameTimes.Count;
            AverageTroopCount = totalTroops / _troopCounts.Count;
            AverageCulledCount = totalCulled / _culledCounts.Count;
            
            CullingEfficiency = AverageTroopCount > 0 ? (AverageCulledCount / AverageTroopCount) * 100 : 0;
        }

        /// <summary>
        /// Get performance report as string
        /// </summary>
        public static string GetPerformanceReport()
        {
            if (!IsEnabled || _frameTimes.Count == 0)
                return "Performance monitoring disabled or no data available";
            
            return $"Performance Report:\n" +
                   $"  Average Frame Time: {AverageFrameTime:F2}ms\n" +
                   $"  Average FPS: {1000.0 / AverageFrameTime:F1}\n" +
                   $"  Average Troops: {AverageTroopCount:F0}\n" +
                   $"  Average Culled: {AverageCulledCount:F0}\n" +
                   $"  Culling Efficiency: {CullingEfficiency:F1}%";
        }

        /// <summary>
        /// Reset all performance statistics
        /// </summary>
        public static void Reset()
        {
            _frameTimes.Clear();
            _troopCounts.Clear();
            _culledCounts.Clear();
            AverageFrameTime = 0;
            AverageTroopCount = 0;
            AverageCulledCount = 0;
            CullingEfficiency = 0;
        }

        /// <summary>
        /// Log performance data to debug output
        /// </summary>
        public static void LogPerformanceData()
        {
            if (!IsEnabled) return;
            
            System.Diagnostics.Debug.WriteLine($"[PerformanceMonitor] {GetPerformanceReport()}");
        }
    }
}