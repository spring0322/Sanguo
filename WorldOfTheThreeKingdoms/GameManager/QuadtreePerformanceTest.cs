using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.TroopDetail;

namespace GameManager
{
    /// <summary>
    /// Performance testing utility for Quadtree optimization
    /// </summary>
    public static class QuadtreePerformanceTest
    {
        /// <summary>
        /// Run a performance comparison between Quadtree and brute force methods
        /// </summary>
        /// <param name="troops">List of troops to test with</param>
        /// <param name="visibleArea">Visible area rectangle</param>
        /// <param name="iterations">Number of test iterations</param>
        /// <returns>Performance test results</returns>
        public static PerformanceTestResult RunPerformanceTest(List<Troop> troops, Rectangle visibleArea, int iterations = 1000)
        {
            var result = new PerformanceTestResult();
            
            // Create quadtree for testing
            Rectangle mapBounds = new Rectangle(0, 0, 4000, 3000); // Default map size
            var quadtree = new Quadtree(0, mapBounds);
            
            // Populate quadtree
            foreach (var troop in troops)
            {
                if (!troop.Destroyed && troop.DrawAnimation)
                {
                    quadtree.Insert(troop);
                }
            }
            
            // Test Quadtree method
            var quadtreeResults = new List<Troop>();
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                quadtreeResults.Clear();
                quadtree.Retrieve(quadtreeResults, visibleArea);
            }
            
            stopwatch.Stop();
            result.QuadtreeTime = stopwatch.Elapsed.TotalMilliseconds;
            result.QuadtreeResultCount = quadtreeResults.Count;
            
            // Test brute force method
            var bruteForceResults = new List<Troop>();
            stopwatch.Restart();
            
            for (int i = 0; i < iterations; i++)
            {
                bruteForceResults.Clear();
                foreach (var troop in troops)
                {
                    if (!troop.Destroyed && troop.DrawAnimation)
                    {
                        Rectangle troopBounds = GetTroopBounds(troop);
                        if (visibleArea.Intersects(troopBounds))
                        {
                            bruteForceResults.Add(troop);
                        }
                    }
                }
            }
            
            stopwatch.Stop();
            result.BruteForceTime = stopwatch.Elapsed.TotalMilliseconds;
            result.BruteForceResultCount = bruteForceResults.Count;
            
            // Calculate performance metrics
            result.TotalTroops = troops.Count;
            result.Iterations = iterations;
            result.SpeedupFactor = result.BruteForceTime / result.QuadtreeTime;
            result.EfficiencyGain = ((result.BruteForceTime - result.QuadtreeTime) / result.BruteForceTime) * 100;
            
            return result;
        }
        
        private static Rectangle GetTroopBounds(Troop troop)
        {
            int worldX = troop.Position.X * QuadtreeConfig.TileWidth;
            int worldY = troop.Position.Y * QuadtreeConfig.TileHeight;
            return new Rectangle(worldX, worldY, QuadtreeConfig.TileWidth, QuadtreeConfig.TileHeight);
        }
        
        /// <summary>
        /// Run automated performance tests with different troop counts
        /// </summary>
        /// <returns>List of test results for different scenarios</returns>
        public static List<PerformanceTestResult> RunScalabilityTest()
        {
            var results = new List<PerformanceTestResult>();
            var testCounts = new[] { 50, 100, 250, 500, 1000, 2000 };
            
            Rectangle visibleArea = new Rectangle(1000, 1000, 800, 600); // Typical visible area
            
            foreach (int troopCount in testCounts)
            {
                // Generate test troops
                var testTroops = GenerateTestTroops(troopCount);
                
                // Run performance test
                var result = RunPerformanceTest(testTroops, visibleArea, 100);
                result.TestScenario = $"{troopCount} troops";
                results.Add(result);
                
                System.Diagnostics.Debug.WriteLine($"[QuadtreePerformanceTest] {result.TestScenario}: {result.SpeedupFactor:F2}x speedup, {result.EfficiencyGain:F1}% efficiency gain");
            }
            
            return results;
        }
        
        private static List<Troop> GenerateTestTroops(int count)
        {
            var troops = new List<Troop>();
            var random = new Random(42); // Fixed seed for consistent results
            
            for (int i = 0; i < count; i++)
            {
                // Create a mock troop for testing
                var troop = new Troop();
                troop.Position = new Point(random.Next(0, 100), random.Next(0, 75)); // Random position on 100x75 map
                troop.Destroyed = false;
                troop.DrawAnimation = true;
                troops.Add(troop);
            }
            
            return troops;
        }
    }
    
    /// <summary>
    /// Results from a performance test
    /// </summary>
    public class PerformanceTestResult
    {
        public string TestScenario { get; set; }
        public int TotalTroops { get; set; }
        public int Iterations { get; set; }
        public double QuadtreeTime { get; set; }
        public double BruteForceTime { get; set; }
        public int QuadtreeResultCount { get; set; }
        public int BruteForceResultCount { get; set; }
        public double SpeedupFactor { get; set; }
        public double EfficiencyGain { get; set; }
        
        public override string ToString()
        {
            return $"Performance Test Results ({TestScenario}):\n" +
                   $"  Total Troops: {TotalTroops}\n" +
                   $"  Iterations: {Iterations}\n" +
                   $"  Quadtree Time: {QuadtreeTime:F2}ms\n" +
                   $"  Brute Force Time: {BruteForceTime:F2}ms\n" +
                   $"  Speedup Factor: {SpeedupFactor:F2}x\n" +
                   $"  Efficiency Gain: {EfficiencyGain:F1}%\n" +
                   $"  Results Match: {QuadtreeResultCount == BruteForceResultCount}";
        }
    }
}