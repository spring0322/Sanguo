using System;
using System.Threading;
using GameObjects;
using Microsoft.Xna.Framework;

namespace GameObjects.AI.Pathfinding
{
    /// <summary>
    /// 地图快照：静态数据共享 + 动态数据快照
    /// 设计原则：静态数据（地形）只读引用零拷贝，动态数据（敌军位置）回合级快照
    /// 🧊 Cold Path - 回合开始时创建一次，可读性优先
    /// </summary>
    /// <param name="scenario">游戏场景</param>
    public class MapSnapshot(GameScenario scenario)
    {
        // ====== 回合级快照缓存（多个寻路任务共享） ======
        
        // 🔥 2026-03-16 AOT 修复：使用 Lazy<T> + 双重检查锁定保证线程安全
        // 原因：WEGO 机制下多个 AI 部队可能同时调用 GetOrCreateSnapshot
        private static Lazy<MapSnapshot> _cachedSnapshot;
        private static int _cachedTurnNumber = -1;
        private static readonly object _snapshotLock = new();
        
        /// <summary>
        /// 获取或创建地图快照（回合级缓存）
        /// 🧊 Cold Path - 每回合最多创建一次
        /// </summary>
        /// <param name="scenario">游戏场景</param>
        /// <returns>地图快照</returns>
        public static MapSnapshot GetOrCreateSnapshot(GameScenario scenario)
        {
            int currentTurn = scenario.Date.Year * 12 + scenario.Date.Month;
            
            // 🔥 快速路径：无锁检查（大部分情况走这里）
            if (_cachedSnapshot != null && _cachedTurnNumber == currentTurn)
            {
                return _cachedSnapshot.Value;
            }
            
            // 🔥 慢速路径：加锁创建
            lock (_snapshotLock)
            {
                // 🔥 再次检查：防止重复创建
                if (_cachedSnapshot != null && _cachedTurnNumber == currentTurn)
                {
                    return _cachedSnapshot.Value;
                }
                
                // 创建新快照（Lazy<T> 内部保证线程安全）
                _cachedSnapshot = new Lazy<MapSnapshot>(
                    () => new MapSnapshot(scenario), 
                    LazyThreadSafetyMode.ExecutionAndPublication);
                _cachedTurnNumber = currentTurn;
                
                System.Diagnostics.Debug.WriteLine($"[MapSnapshot] 创建新快照，回合 {currentTurn}");
                
                return _cachedSnapshot.Value;
            }
        }
        
        // ====== 静态数据（只读共享，多线程安全，零拷贝） ======
        
        /// <summary>
        /// 地形数据（只读引用，不拷贝）
        /// 多线程安全：地形数据在游戏运行期间不会改变
        /// </summary>
        public int[,] TerrainData { get; } = scenario.ScenarioMap.MapData;
        
        /// <summary>
        /// 地图宽度
        /// </summary>
        public int Width { get; } = scenario.ScenarioMap.MapDimensions.X;
        
        /// <summary>
        /// 地图高度
        /// </summary>
        public int Height { get; } = scenario.ScenarioMap.MapDimensions.Y;

        // ====== 动态数据（回合级快照） ======
        
        /// <summary>
        /// 动态障碍物空间网格（部队位置、建筑控制区）
        /// 回合级快照：在回合开始时生成一次
        /// </summary>
        public SpatialGrid DynamicObstacles { get; } = SpatialGrid.CreateFromScenario(scenario);
        
        /// <summary>
        /// 当前回合编号（简化计算：年 * 12 + 月）
        /// </summary>
        public int TurnNumber { get; } = scenario.Date.Year * 12 + scenario.Date.Month;
    }
}
