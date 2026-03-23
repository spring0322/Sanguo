using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

#nullable enable

namespace GameObjects.AI.Pathfinding
{
    /// <summary>
    /// 动态障碍物快照（纯值类型，零分配）
    /// </summary>
    public readonly struct DynamicObstacle(int x, int y, int penaltyScore)
    {
        public readonly int X = x;
        public readonly int Y = y;
        public readonly int PenaltyScore = penaltyScore;
    }

    /// <summary>
    /// 寻路请求快照 (C# 12 record struct，零堆分配)
    /// </summary>
    public readonly record struct PathRequest(
        int TroopId,
        int TrackingId,              // 寻路版本号（防幽灵折返）- 使用 int 代替 Guid
        Point StartPosition,
        Point TargetPosition,
        int TerrainAdaptability,     // 快照化地形适应性
        DynamicObstacle[] PooledObstacles,  // 从 ArrayPool 租借的障碍物数组
        int ObstacleCount,           // 实际障碍物数量
        CancellationToken CancellationToken
    );

    /// <summary>
    /// 寻路结果快照
    /// </summary>
    public readonly record struct PathResult(
        int TroopId,
        int TrackingId,              // 寻路版本号（防幽灵折返）- 使用 int 代替 Guid
        List<Point>? Path,
        bool IsSuccess,
        bool IsCancelled
    );

    /// <summary>
    /// 极简路径内存池 (消除 GC Spikes)
    /// </summary>
    public static class PathPool
    {
        private static readonly ConcurrentBag<List<Point>> _pool = [];

        public static List<Point> Rent()
        {
            if (_pool.TryTake(out var list))
            {
                list.Clear(); // 确保清空
                return list;
            }
            return new List<Point>(64);
        }

        public static void Return(List<Point>? list)
        {
            if (list == null) return;
            list.Clear();
            _pool.Add(list);
        }
    }
}
