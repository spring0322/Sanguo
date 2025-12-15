using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// 增强的 A* 寻路器 - 基于你的反馈实现
    /// 🎯 核心改进：
    /// 1. 传入 unit 信息，计算具体的消耗 (包含 ZOC 和地形)
    /// 2. 如果不可通行 (-1)，直接跳过
    /// 3. 完整的 A* 算法实现
    /// </summary>
    public class EnhancedPathfinder
    {
        private readonly MovementCalculator _movementCalculator;
        private readonly IMapInfoProvider _mapProvider;
        
        public EnhancedPathfinder(IMapInfoProvider mapProvider = null)
        {
            _mapProvider = mapProvider ?? World.MapProvider;
            _movementCalculator = new MovementCalculator(_mapProvider);
        }
        
        /// <summary>
        /// 🎯 你提供的核心寻路方法 - 完整实现
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="unit">单位信息</param>
        /// <returns>路径点列表，null表示无路径</returns>
        public List<Point> FindPath(Point start, Point end, Unit unit)
        {
            var openSet = new PriorityQueue<Node>();
            var closedSet = new HashSet<Point>();
            var allNodes = new Dictionary<Point, Node>();
            
            // 初始化起点
            var startNode = new Node(start, 0, GetHeuristic(start, end));
            openSet.Enqueue(startNode, startNode.FCost);
            allNodes[start] = startNode;
            
            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                
                if (current.Pos == end) 
                    return RetracePath(current);
                
                closedSet.Add(current.Pos);
                
                foreach (var neighborPos in GetNeighbors(current.Pos))
                {
                    if (closedSet.Contains(neighborPos)) 
                        continue;
                    
                    // === 关键修改点 ===
                    // 传入 unit 信息，计算具体的消耗 (包含 ZOC 和地形)
                    int moveCost = _movementCalculator.GetMoveCost(current.Pos, neighborPos, unit);
                    
                    // 如果不可通行 (-1)，直接跳过
                    if (moveCost < 0) 
                        continue;
                    // =================
                    
                    int newGCost = current.GCost + moveCost;
                    
                    // 检查是否已经在 OpenSet 中，且有更优路径
                    if (allNodes.TryGetValue(neighborPos, out var existingNode))
                    {
                        if (newGCost < existingNode.GCost)
                        {
                            // 找到更优路径，更新节点
                            existingNode.GCost = newGCost;
                            existingNode.Parent = current;
                            
                            // 重新加入优先队列（简化实现）
                            openSet.Enqueue(existingNode, existingNode.FCost);
                        }
                    }
                    else
                    {
                        // 新节点，加入 OpenSet
                        var newNode = new Node(neighborPos, newGCost, GetHeuristic(neighborPos, end))
                        {
                            Parent = current
                        };
                        
                        openSet.Enqueue(newNode, newNode.FCost);
                        allNodes[neighborPos] = newNode;
                    }
                }
            }
            
            return null; // 没路
        }
        
        /// <summary>
        /// 获取邻居节点（8方向）
        /// </summary>
        private IEnumerable<Point> GetNeighbors(Point pos)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过自己
                    
                    Point neighbor = new Point(pos.X + dx, pos.Y + dy);
                    
                    // 边界检查
                    if (_mapProvider.IsInBounds(neighbor.X, neighbor.Y))
                    {
                        yield return neighbor;
                    }
                }
            }
        }
        
        /// <summary>
        /// 启发式函数 - 曼哈顿距离
        /// </summary>
        private int GetHeuristic(Point from, Point to)
        {
            return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        }
        
        /// <summary>
        /// 回溯路径
        /// </summary>
        private List<Point> RetracePath(Node endNode)
        {
            var path = new List<Point>();
            var current = endNode;
            
            while (current != null)
            {
                path.Add(current.Pos);
                current = current.Parent;
            }
            
            path.Reverse();
            return path;
        }
    }
    
    /// <summary>
    /// A* 节点
    /// </summary>
    public class Node
    {
        public Point Pos { get; }
        public int GCost { get; set; }  // 从起点到当前点的实际消耗
        public int HCost { get; }       // 从当前点到终点的启发式消耗
        public int FCost => GCost + HCost; // 总消耗
        public Node Parent { get; set; }
        
        public Node(Point pos, int gCost, int hCost)
        {
            Pos = pos;
            GCost = gCost;
            HCost = hCost;
        }
    }
    
    /// <summary>
    /// 简化的优先队列实现
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<(T item, int priority)> _items = new List<(T, int)>();
        
        public int Count => _items.Count;
        
        public void Enqueue(T item, int priority)
        {
            _items.Add((item, priority));
        }
        
        public T Dequeue()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            
            int bestIndex = 0;
            for (int i = 1; i < _items.Count; i++)
            {
                if (_items[i].priority < _items[bestIndex].priority)
                {
                    bestIndex = i;
                }
            }
            
            var result = _items[bestIndex].item;
            _items.RemoveAt(bestIndex);
            return result;
        }
    }
    
    /// <summary>
    /// PathfindingManager 的扩展方法 - 展示如何集成
    /// </summary>
    public static class PathfindingManagerExtensions
    {
        /// <summary>
        /// 为 Troop 寻路的便捷方法
        /// </summary>
        public static List<Point> FindPathForTroop(this PathfindingManager manager, Troop troop, Point destination)
        {
            if (troop == null) return null;
            
            var pathfinder = new EnhancedPathfinder();
            var unit = Unit.FromTroop(troop);
            
            return pathfinder.FindPath(troop.Position, destination, unit);
        }
        
        /// <summary>
        /// 批量寻路 - 考虑单位类型
        /// </summary>
        public static Dictionary<int, List<Point>> FindPathBatchWithUnits(
            this PathfindingManager manager, 
            List<(Troop troop, Point destination)> requests)
        {
            var results = new Dictionary<int, List<Point>>();
            var pathfinder = new EnhancedPathfinder();
            
            foreach (var (troop, destination) in requests)
            {
                if (troop != null)
                {
                    var unit = Unit.FromTroop(troop);
                    var path = pathfinder.FindPath(troop.Position, destination, unit);
                    results[troop.ID] = path;
                }
            }
            
            return results;
        }
    }
}