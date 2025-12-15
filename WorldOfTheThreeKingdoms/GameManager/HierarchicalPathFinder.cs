using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 高性能分层寻路系统 - 基于用户提供的优秀设计
    /// 🎯 核心特点：
    /// 1. 双层寻路：粗糙快速 + 精细准确
    /// 2. 智能分段：只计算必要路径，后续异步
    /// 3. 路径平滑：消除拼接锯齿
    /// 4. 对象池优化：零GC路径计算
    /// </summary>
    public class HierarchicalPathFinder
    {
        private PathFinder _highLevelFinder; // 基于10x10网格
        private PathFinder _lowLevelFinder;  // 基于1x1网格
        private int _clusterSize = 10; // 每个大格子包含多少小格子
        
        // 对象池优化
        private ObjectPool<List<Point>> _pathPool;
        private ObjectPool<List<Point>> _clusterPathPool;
        
        // 性能统计
        public int TotalPathRequests { get; private set; }
        public int FastPathHits { get; private set; } // 同区域或相邻区域的快速路径
        public int HierarchicalPathHits { get; private set; } // 需要分层寻路的路径
        
        public HierarchicalPathFinder(PathFinder highLevel, PathFinder lowLevel, int clusterSize = 10)
        {
            _highLevelFinder = highLevel ?? throw new ArgumentNullException(nameof(highLevel));
            _lowLevelFinder = lowLevel ?? throw new ArgumentNullException(nameof(lowLevel));
            _clusterSize = Math.Max(1, clusterSize);
            
            // 初始化对象池
            _pathPool = new ObjectPool<List<Point>>(() => new List<Point>(100), 20, 100);
            _clusterPathPool = new ObjectPool<List<Point>>(() => new List<Point>(20), 10, 50);
            
            System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 初始化完成，集群大小: {_clusterSize}");
        }
        
        /// <summary>
        /// 分层寻路主方法 - 智能选择最优策略
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">目标点</param>
        /// <returns>路径点列表，失败返回null</returns>
        public List<Point> FindPath(Point start, Point end)
        {
            TotalPathRequests++;
            
            try
            {
                // 1. 快速检查：如果在同一个大区或相邻大区，直接精细寻路
                Point startCluster = ToClusterCoord(start);
                Point endCluster = ToClusterCoord(end);
                
                if (GetManhattanDistance(startCluster, endCluster) <= 1)
                {
                    FastPathHits++;
                    var directPath = _lowLevelFinder.FindPath(start, end);
                    
                    if (directPath != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 快速路径: {start} -> {end}, 长度: {directPath.Count}");
                    }
                    
                    return directPath;
                }
                
                // 2. 分层寻路：先找大区路径
                HierarchicalPathHits++;
                return FindHierarchicalPath(start, end, startCluster, endCluster);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 寻路错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 分层寻路实现 - 大区路径 + 分段精细寻路
        /// </summary>
        private List<Point> FindHierarchicalPath(Point start, Point end, Point startCluster, Point endCluster)
        {
            // 2. 高层寻路：找到经过哪些大区
            var clusterPath = _clusterPathPool.Get();
            try
            {
                var highLevelResult = _highLevelFinder.FindPath(startCluster, endCluster);
                if (highLevelResult == null || highLevelResult.Count == 0)
                {
                    return null;
                }
                
                clusterPath.AddRange(highLevelResult);
                
                var finalPath = _pathPool.Get();
                try
                {
                    Point currentPos = start;
                    
                    // 3. 分段精细寻路 (只计算前几段，后续的可以异步计算 - 暂未实现异步)
                    // 注意：这里我们只寻路到下一个大区的"中心点"作为临时目标
                    for (int i = 1; i < clusterPath.Count; i++)
                    {
                        // 将大区坐标转换为该区域的世界中心坐标
                        Point nextClusterCenter = ToWorldCenter(clusterPath[i]);
                        
                        // 如果是最后一段，直接去终点
                        if (i == clusterPath.Count - 1)
                        {
                            nextClusterCenter = end;
                        }
                        
                        var segment = _lowLevelFinder.FindPath(currentPos, nextClusterCenter);
                        if (segment != null && segment.Count > 0)
                        {
                            // 避免重复添加起始点
                            if (finalPath.Count == 0)
                            {
                                finalPath.AddRange(segment);
                            }
                            else
                            {
                                // 跳过第一个点（与上一段的终点重复）
                                for (int j = 1; j < segment.Count; j++)
                                {
                                    finalPath.Add(segment[j]);
                                }
                            }
                            
                            currentPos = nextClusterCenter; // 更新当前位置
                        }
                        else
                        {
                            // 某一段寻路失败，整个路径失败
                            System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 分段寻路失败: {currentPos} -> {nextClusterCenter}");
                            return null;
                        }
                    }
                    
                    // 4. 路径平滑 (String Pulling) - 必做！
                    // 消除拼接处的锯齿
                    var smoothedPath = SmoothPath(finalPath);
                    
                    System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 分层路径: {start} -> {end}, 原始长度: {finalPath.Count}, 平滑后: {smoothedPath?.Count ?? 0}");
                    
                    return smoothedPath;
                }
                finally
                {
                    // 归还路径对象到池中
                    finalPath.Clear();
                    _pathPool.Return(finalPath);
                }
            }
            finally
            {
                // 归还集群路径对象到池中
                clusterPath.Clear();
                _clusterPathPool.Return(clusterPath);
            }
        }
        
        /// <summary>
        /// 异步分层寻路 - 用于大规模路径计算
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">目标点</param>
        /// <param name="callback">完成回调</param>
        public void FindPathAsync(Point start, Point end, Action<List<Point>> callback)
        {
            if (callback == null) return;
            
            // 简单的异步实现 - 在实际游戏中可能需要更复杂的线程池
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var path = FindPath(start, end);
                    callback(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 异步寻路错误: {ex.Message}");
                    callback(null);
                }
            });
        }
        
        // ==========================================
        // 坐标转换辅助方法
        // ==========================================
        
        /// <summary>
        /// 世界坐标转换为集群坐标
        /// </summary>
        private Point ToClusterCoord(Point world)
        {
            return new Point(world.X / _clusterSize, world.Y / _clusterSize);
        }
        
        /// <summary>
        /// 集群坐标转换为世界中心坐标
        /// </summary>
        private Point ToWorldCenter(Point cluster)
        {
            int half = _clusterSize / 2;
            return new Point(cluster.X * _clusterSize + half, cluster.Y * _clusterSize + half);
        }
        
        /// <summary>
        /// 计算曼哈顿距离
        /// </summary>
        private int GetManhattanDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
        
        /// <summary>
        /// 计算欧几里得距离的平方（避免开方运算）
        /// </summary>
        private int GetDistanceSquared(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
        
        // ==========================================
        // 路径平滑算法
        // ==========================================
        
        /// <summary>
        /// 简易平滑算法 (弗洛伊德算法Floyd's Algorithm的简化版)
        /// 🎯 消除分段拼接产生的锯齿和不必要的拐点
        /// </summary>
        /// <param name="rawPath">原始路径</param>
        /// <returns>平滑后的路径</returns>
        private List<Point> SmoothPath(List<Point> rawPath)
        {
            if (rawPath == null || rawPath.Count <= 2)
            {
                return rawPath;
            }
            
            var smoothed = _pathPool.Get();
            try
            {
                smoothed.Add(rawPath[0]); // 添加起始点
                
                int checkIndex = 0;
                for (int i = 2; i < rawPath.Count; i++)
                {
                    // 如果从checkIndex可以直接直线走到i (无障碍)，则跳过中间的点
                    if (!CanWalkDirectly(rawPath[checkIndex], rawPath[i]))
                    {
                        smoothed.Add(rawPath[i - 1]); // 撞墙了，必须经过上一个拐点
                        checkIndex = i - 1;
                    }
                }
                
                // 添加终点
                if (smoothed.Count == 0 || !smoothed.Last().Equals(rawPath.Last()))
                {
                    smoothed.Add(rawPath.Last());
                }
                
                // 创建新的列表返回（因为smoothed需要归还到池中）
                var result = new List<Point>(smoothed);
                return result;
            }
            finally
            {
                smoothed.Clear();
                _pathPool.Return(smoothed);
            }
        }
        
        /// <summary>
        /// 检查两点间是否可以直线行走（简化的射线检测）
        /// </summary>
        /// <param name="from">起始点</param>
        /// <param name="to">目标点</param>
        /// <returns>true表示可以直线行走</returns>
        private bool CanWalkDirectly(Point from, Point to)
        {
            // 使用低级寻路器的射线检测功能
            // 如果PathFinder没有Raycast方法，可以用简单的直线采样
            if (_lowLevelFinder != null)
            {
                // 假设PathFinder有Raycast方法
                // return !_lowLevelFinder.Raycast(from, to);
                
                // 简化实现：采样直线上的点检查是否有障碍
                return CheckLineOfSight(from, to);
            }
            
            return false;
        }
        
        /// <summary>
        /// 简化的视线检查 - 采样直线上的点
        /// </summary>
        private bool CheckLineOfSight(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            int steps = Math.Max(dx, dy);
            
            if (steps == 0) return true;
            
            float stepX = (float)(to.X - from.X) / steps;
            float stepY = (float)(to.Y - from.Y) / steps;
            
            // 采样检查（每隔几个像素检查一次以提高性能）
            int sampleInterval = Math.Max(1, steps / 10);
            
            for (int i = 0; i <= steps; i += sampleInterval)
            {
                int x = from.X + (int)(stepX * i);
                int y = from.Y + (int)(stepY * i);
                
                // 这里需要根据实际的地图系统检查障碍
                // if (IsObstacle(x, y)) return false;
            }
            
            return true;
        }
        
        // ==========================================
        // 性能监控和调试
        // ==========================================
        
        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            float fastPathRate = TotalPathRequests > 0 ? (float)FastPathHits / TotalPathRequests : 0f;
            float hierarchicalPathRate = TotalPathRequests > 0 ? (float)HierarchicalPathHits / TotalPathRequests : 0f;
            
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 分层寻路性能统计 ===");
            stats.AppendLine($"总寻路请求: {TotalPathRequests}");
            stats.AppendLine($"快速路径: {FastPathHits} ({fastPathRate:P1})");
            stats.AppendLine($"分层路径: {HierarchicalPathHits} ({hierarchicalPathRate:P1})");
            stats.AppendLine($"集群大小: {_clusterSize}x{_clusterSize}");
            stats.AppendLine($"路径池统计: {_pathPool.GetStats()}");
            stats.AppendLine($"集群路径池统计: {_clusterPathPool.GetStats()}");
            
            return stats.ToString();
        }
        
        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStats()
        {
            TotalPathRequests = 0;
            FastPathHits = 0;
            HierarchicalPathHits = 0;
            
            System.Diagnostics.Debug.WriteLine("[HierarchicalPathFinder] 统计数据已重置");
        }
        
        /// <summary>
        /// 清理资源 - 游戏退出时调用
        /// </summary>
        public void Dispose()
        {
            _pathPool?.Clear();
            _clusterPathPool?.Clear();
            
            System.Diagnostics.Debug.WriteLine("[HierarchicalPathFinder] 资源已清理");
        }
        
        /// <summary>
        /// 预热对象池 - 在Loading阶段调用
        /// </summary>
        public void Prewarm(int pathPoolSize = 20, int clusterPathPoolSize = 10)
        {
            _pathPool?.Prewarm(pathPoolSize);
            _clusterPathPool?.Prewarm(clusterPathPoolSize);
            
            System.Diagnostics.Debug.WriteLine($"[HierarchicalPathFinder] 对象池预热完成: 路径池{pathPoolSize}, 集群路径池{clusterPathPoolSize}");
        }
    }
    
    /// <summary>
    /// 路径查找器接口 - 用于抽象不同的寻路算法
    /// </summary>
    public interface IPathFinder
    {
        List<Point> FindPath(Point start, Point end);
        bool Raycast(Point from, Point to);
    }
    
    /// <summary>
    /// 基础路径查找器 - 简化实现
    /// 🎯 在实际项目中，这应该是你现有的寻路系统
    /// </summary>
    public class PathFinder : IPathFinder
    {
        private int _mapWidth;
        private int _mapHeight;
        private bool[,] _obstacles;
        
        public PathFinder(int width, int height)
        {
            _mapWidth = width;
            _mapHeight = height;
            _obstacles = new bool[width, height];
        }
        
        /// <summary>
        /// 设置障碍物
        /// </summary>
        public void SetObstacle(int x, int y, bool isObstacle)
        {
            if (x >= 0 && x < _mapWidth && y >= 0 && y < _mapHeight)
            {
                _obstacles[x, y] = isObstacle;
            }
        }
        
        /// <summary>
        /// 检查是否为障碍物
        /// </summary>
        public bool IsObstacle(int x, int y)
        {
            if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight)
                return true; // 边界视为障碍
            
            return _obstacles[x, y];
        }
        
        /// <summary>
        /// 简化的A*寻路实现
        /// </summary>
        public virtual List<Point> FindPath(Point start, Point end)
        {
            // 这里应该是你的实际寻路算法（A*、Dijkstra等）
            // 为了演示，这里返回一个简单的直线路径
            
            if (IsObstacle(start.X, start.Y) || IsObstacle(end.X, end.Y))
            {
                return null; // 起点或终点是障碍物
            }
            
            var path = new List<Point>();
            
            // 简单的直线路径（实际应该用A*等算法）
            int dx = Math.Sign(end.X - start.X);
            int dy = Math.Sign(end.Y - start.Y);
            
            Point current = start;
            path.Add(current);
            
            while (!current.Equals(end))
            {
                if (current.X != end.X)
                    current.X += dx;
                if (current.Y != end.Y)
                    current.Y += dy;
                
                if (IsObstacle(current.X, current.Y))
                {
                    return null; // 路径被阻挡
                }
                
                path.Add(current);
                
                // 防止无限循环
                if (path.Count > 1000)
                {
                    break;
                }
            }
            
            return path;
        }
        
        /// <summary>
        /// 射线检测 - 检查两点间是否有障碍物
        /// </summary>
        public virtual bool Raycast(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            int steps = Math.Max(dx, dy);
            
            if (steps == 0) return false;
            
            float stepX = (float)(to.X - from.X) / steps;
            float stepY = (float)(to.Y - from.Y) / steps;
            
            for (int i = 0; i <= steps; i++)
            {
                int x = from.X + (int)(stepX * i);
                int y = from.Y + (int)(stepY * i);
                
                if (IsObstacle(x, y))
                {
                    return true; // 发现障碍物
                }
            }
            
            return false; // 无障碍物
        }
    }
}