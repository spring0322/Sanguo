using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// Configuration settings for Quadtree spatial partitioning
    /// </summary>
    public static class QuadtreeConfig
    {
        /// <summary>
        /// Maximum objects per node before splitting (default: 10)
        /// </summary>
        public static int MaxObjectsPerNode { get; set; } = 10;

        /// <summary>
        /// Maximum tree depth (default: 5)
        /// </summary>
        public static int MaxTreeDepth { get; set; } = 5;

        /// <summary>
        /// Padding around visible area in pixels (default: 100)
        /// </summary>
        public static int VisibleAreaPadding { get; set; } = 100;

        /// <summary>
        /// Tile width in pixels for coordinate conversion (default: 60)
        /// </summary>
        public static int TileWidth { get; set; } = 60;

        /// <summary>
        /// Tile height in pixels for coordinate conversion (default: 40)
        /// </summary>
        public static int TileHeight { get; set; } = 40;

        /// <summary>
        /// Enable performance monitoring (default: false)
        /// </summary>
        public static bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// Enable debug visualization of Quadtree bounds (default: false)
        /// </summary>
        public static bool EnableDebugVisualization { get; set; } = false;

        /// <summary>
        /// Performance profile presets
        /// </summary>
        public enum PerformanceProfile
        {
            /// <summary>
            /// Optimized for low-end hardware
            /// </summary>
            LowEnd,
            /// <summary>
            /// Balanced settings for mid-range hardware
            /// </summary>
            Balanced,
            /// <summary>
            /// High performance settings for powerful hardware
            /// </summary>
            HighEnd,
            /// <summary>
            /// Custom user-defined settings
            /// </summary>
            Custom
        }

        /// <summary>
        /// Apply a performance profile preset
        /// </summary>
        /// <param name="profile">The performance profile to apply</param>
        public static void ApplyPerformanceProfile(PerformanceProfile profile)
        {
            switch (profile)
            {
                case PerformanceProfile.LowEnd:
                    MaxObjectsPerNode = 15;  // Fewer subdivisions
                    MaxTreeDepth = 4;        // Shallower tree
                    VisibleAreaPadding = 50; // Less padding
                    EnablePerformanceMonitoring = false;
                    EnableDebugVisualization = false;
                    break;

                case PerformanceProfile.Balanced:
                    MaxObjectsPerNode = 10;  // Default settings
                    MaxTreeDepth = 5;
                    VisibleAreaPadding = 100;
                    EnablePerformanceMonitoring = false;
                    EnableDebugVisualization = false;
                    break;

                case PerformanceProfile.HighEnd:
                    MaxObjectsPerNode = 8;   // More subdivisions
                    MaxTreeDepth = 6;        // Deeper tree
                    VisibleAreaPadding = 150; // More padding
                    EnablePerformanceMonitoring = true;
                    EnableDebugVisualization = false;
                    break;

                case PerformanceProfile.Custom:
                    // Keep current settings
                    break;
            }
        }

        /// <summary>
        /// Auto-detect optimal settings based on system performance
        /// </summary>
        /// <param name="totalTroops">Total number of troops in the scenario</param>
        /// <param name="mapSize">Size of the game map</param>
        public static void AutoConfigureForScenario(int totalTroops, Point mapSize)
        {
            // Calculate map area
            int mapArea = mapSize.X * mapSize.Y;
            
            // Determine appropriate settings based on scenario size
            if (totalTroops < 100 && mapArea < 10000)
            {
                // Small scenario - use balanced settings
                ApplyPerformanceProfile(PerformanceProfile.Balanced);
            }
            else if (totalTroops < 500 && mapArea < 50000)
            {
                // Medium scenario - use balanced settings with slight optimization
                ApplyPerformanceProfile(PerformanceProfile.Balanced);
                MaxObjectsPerNode = 12;
            }
            else
            {
                // Large scenario - use high-end settings
                ApplyPerformanceProfile(PerformanceProfile.HighEnd);
                
                // Further optimize for very large scenarios
                if (totalTroops > 1000)
                {
                    MaxObjectsPerNode = 6;
                    MaxTreeDepth = 7;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[QuadtreeConfig] Auto-configured for {totalTroops} troops on {mapSize.X}x{mapSize.Y} map");
            System.Diagnostics.Debug.WriteLine($"[QuadtreeConfig] Settings: MaxObjects={MaxObjectsPerNode}, MaxDepth={MaxTreeDepth}, Padding={VisibleAreaPadding}");
        }

        /// <summary>
        /// Get current configuration as string
        /// </summary>
        public static string GetConfigurationInfo()
        {
            return $"Quadtree Configuration:\n" +
                   $"  Max Objects Per Node: {MaxObjectsPerNode}\n" +
                   $"  Max Tree Depth: {MaxTreeDepth}\n" +
                   $"  Visible Area Padding: {VisibleAreaPadding}px\n" +
                   $"  Tile Size: {TileWidth}x{TileHeight}px\n" +
                   $"  Performance Monitoring: {EnablePerformanceMonitoring}\n" +
                   $"  Debug Visualization: {EnableDebugVisualization}";
        }
    }
}