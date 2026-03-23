using System.Collections.Generic;
using GameObjects;
using Microsoft.Xna.Framework;

namespace GameObjects.AI.Pathfinding
{
    /// <summary>
    /// 空间划分网格：用于快速查询动态障碍物
    /// 使用 HashSet 实现 O(1) 查询性能
    /// 设计原则：回合级快照，Cold Path 可读性优先
    /// </summary>
    public class SpatialGrid
    {
        // ✅ C# 12 集合表达式
        private readonly HashSet<Point> _obstaclePositions = [];
        private readonly Dictionary<Point, int> _factionControlZones = [];

        /// <summary>
        /// 从 GameScenario 创建空间网格快照
        /// 🧊 Cold Path - 回合开始时执行一次，可读性优先，允许使用 LINQ
        /// </summary>
        /// <param name="scenario">游戏场景</param>
        /// <returns>空间网格实例</returns>
        public static SpatialGrid CreateFromScenario(GameScenario scenario)
        {
            var grid = new SpatialGrid();

            // 收集所有部队位置作为障碍物
            foreach (Troop troop in scenario.Troops)
            {
                grid._obstaclePositions.Add(troop.Position);
            }

            // 收集所有建筑控制区域
            foreach (Architecture arch in scenario.Architectures)
            {
                // BelongedFaction 为 null 是正常状态（中立建筑）
                int factionId = arch.BelongedFaction?.ID ?? -1;

                foreach (var point in arch.ArchitectureArea.Area)
                {
                    grid._factionControlZones[point] = factionId;
                }
            }

            return grid;
        }

        /// <summary>
        /// 检查指定位置是否为障碍物
        /// 🔥 Hot Path - 寻路算法中频繁调用，O(1) 查询
        /// </summary>
        /// <param name="position">位置坐标</param>
        /// <returns>是否为障碍物</returns>
        public bool IsObstacle(Point position) => _obstaclePositions.Contains(position);

        /// <summary>
        /// 获取指定位置的控制势力 ID
        /// 🔥 Hot Path - 寻路算法中频繁调用，O(1) 查询
        /// </summary>
        /// <param name="position">位置坐标</param>
        /// <returns>势力 ID，如果无控制势力则返回 -1</returns>
        public int GetControllingFaction(Point position)
        {
            return _factionControlZones.TryGetValue(position, out var factionId) ? factionId : -1;
        }
    }
}
