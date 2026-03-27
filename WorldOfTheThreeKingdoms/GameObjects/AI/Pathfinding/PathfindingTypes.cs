using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;

#nullable enable

namespace GameObjects.AI.Pathfinding
{
    public readonly struct DynamicObstacle(int x, int y, int penaltyScore)
    {
        public readonly int X = x;
        public readonly int Y = y;
        public readonly int PenaltyScore = penaltyScore;
    }

    public readonly struct TerrainCostProfile(
        int plain,
        int grassland,
        int forest,
        int marsh,
        int mountain,
        int water,
        int ridge,
        int wasteland,
        int desert,
        int cliff)
    {
        public const int UnreachableTerrainCost = 0xDAC;

        public readonly int Plain = plain;
        public readonly int Grassland = grassland;
        public readonly int Forest = forest;
        public readonly int Marsh = marsh;
        public readonly int Mountain = mountain;
        public readonly int Water = water;
        public readonly int Ridge = ridge;
        public readonly int Wasteland = wasteland;
        public readonly int Desert = desert;
        public readonly int Cliff = cliff;

        public int GetCost(byte terrainKindId)
        {
            return terrainKindId switch
            {
                1 => Plain,
                2 => Grassland,
                3 => Forest,
                4 => Marsh,
                5 => Mountain,
                6 => Water,
                7 => Ridge,
                8 => Wasteland,
                9 => Desert,
                10 => Cliff,
                _ => UnreachableTerrainCost
            };
        }
    }

    public readonly record struct PathRequest(
        Guid TroopId,
        int TrackingId,
        Point StartPosition,
        Point TargetPosition,
        TerrainCostProfile TerrainCosts,
        bool TargetHasArchitecture,
        int[] PooledTraversalCosts,
        int TraversalCostCount,
        int[] PooledTraversalPenalties,
        int TraversalPenaltyCount,
        DynamicObstacle[] PooledObstacles,
        int ObstacleCount,
        CancellationToken CancellationToken
    );

    public readonly record struct PathResult(
        Guid TroopId,
        int TrackingId,
        List<Point>? Path,
        bool IsSuccess,
        bool IsCancelled
    );

    public static class PathPool
    {
        private static readonly ConcurrentBag<List<Point>> _pool = [];

        public static int PoolSize => _pool.Count;

        public static List<Point> Rent()
        {
            if (_pool.TryTake(out var list))
            {
                list.Clear();
                return list;
            }

            return new List<Point>(64);
        }

        public static void Return(List<Point>? list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            _pool.Add(list);
        }
    }
}
