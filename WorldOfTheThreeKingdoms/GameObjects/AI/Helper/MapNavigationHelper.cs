using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.TroopDetail;
using GameManager;

namespace GameObjects.AI.Helper
{
    /// <summary>
    /// 地图导航助手 - 为AI系统提供地图导航和路径规划功能
    /// 集成现有的游戏寻路系统，提供统一的导航接口
    /// </summary>
    public static class MapNavigationHelper
    {
        // ================= 常量定义 =================
        private const int MAX_SEARCH_DISTANCE = 50;        // 最大搜索距离
        private const int IMPASSABLE_COST = 0xdac;         // 不可通行的代价值（与游戏原有系统一致）
        private const int DEFAULT_MOVEMENT_RANGE = 10;      // 默认移动范围
        
        // 缓存常用的四个方向 (上, 下, 左, 右)
        private static readonly Point[] Directions = new Point[]
        {
            new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0)
        };

        /// <summary>
        /// 精确获取部队当前回合可到达的所有坐标 (考虑地形消耗和阻挡)
        /// 使用广度优先搜索算法，提供更准确的可移动区域计算
        /// </summary>
        /// <param name="troop">当前行动的部队</param>
        /// <returns>可移动的坐标列表</returns>
        public static List<Point> GetUnitMoveableArea(Troop troop, int? maxDistance = null)
        {
            var results = new List<Point>();
            
            try
            {
                if (troop == null || troop.Destroyed) 
                {
                    Console.WriteLine("[MapNavigationHelper] 警告: 部队无效或已被摧毁");
                    return results;
                }

                // 1. 获取基础数据
                int maxMove = maxDistance ?? Math.Min(troop.MovabilityLeft > 0 ? troop.MovabilityLeft : troop.Movability, DEFAULT_MOVEMENT_RANGE);
                if (maxMove <= 0) 
                {
                    Console.WriteLine("[MapNavigationHelper] 警告: 部队无移动力");
                    return results;
                }

                Point startPos = troop.Position;

                // 2. 初始化算法容器
                // Dictionary 记录到达某点的 [剩余移动力]
                // 如果一个点有了记录，且新的路径剩余移动力更少，则跳过
                Dictionary<Point, int> visited = new Dictionary<Point, int>();
                Queue<Point> queue = new Queue<Point>();

                // 3. 初始节点入队
                visited[startPos] = maxMove;
                queue.Enqueue(startPos);
                results.Add(startPos); // 原地也是一个合法的"移动"目标（比如为了卡位不动）

                // 4. 开始广度优先搜索 (Flood Fill)
                while (queue.Count > 0)
                {
                    Point current = queue.Dequeue();
                    int currentMobility = visited[current];

                    // 遍历四个方向
                    foreach (var dir in Directions)
                    {
                        Point next = new Point(current.X + dir.X, current.Y + dir.Y);

                        // --- A. 边界与硬阻挡检查 ---
                        if (!IsPositionValid(next)) continue;

                        // --- B. 敌军/障碍物阻挡检查 ---
                        if (IsBlockedByEnemy(next, troop)) continue;

                        // --- C. 地形消耗计算 ---
                        int cost = GetTerrainCost(next, troop);
                        
                        // 如果地形不可通行或剩余移动力不足
                        if (cost >= IMPASSABLE_COST || cost > currentMobility) continue;

                        int nextMobility = currentMobility - cost;

                        // --- D. 优化路径记录 ---
                        // 如果该点之前没访问过，或者我们发现了一条剩余移动力更多的路径到达此点
                        if (!visited.ContainsKey(next) || visited[next] < nextMobility)
                        {
                            visited[next] = nextMobility;
                            queue.Enqueue(next);

                            // 只有当该点没有被"完全占据"时，才加入可行列表
                            if (!IsOccupied(next))
                            {
                                results.Add(next);
                            }
                        }
                    }
                }

                Console.WriteLine($"[MapNavigationHelper] 为部队 {troop.ID} 找到 {results.Count} 个可移动位置（使用BFS算法）");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 获取可移动区域时发生错误: {ex.Message}");
                
                // 如果出错，回退到简化版本
                return GetUnitMoveableAreaFallback(troop, maxDistance);
            }

            return results;
        }

        /// <summary>
        /// 回退版本的可移动区域计算（当BFS算法出错时使用）
        /// </summary>
        private static List<Point> GetUnitMoveableAreaFallback(Troop troop, int? maxDistance)
        {
            var moveablePoints = new List<Point>();
            
            try
            {
                if (troop?.Army?.Kind == null)
                {
                    return moveablePoints;
                }

                int searchDistance = maxDistance ?? Math.Min(troop.Movability / 5, DEFAULT_MOVEMENT_RANGE);
                if (searchDistance <= 0) searchDistance = DEFAULT_MOVEMENT_RANGE;

                Point startPos = troop.Position;
                MilitaryKind kind = troop.Army.Kind;

                // 保存原始移动力
                int originalMovability = troop.MovabilityLeft;
                troop.MovabilityLeft = troop.Movability;

                // 使用方形搜索区域
                for (int x = startPos.X - searchDistance; x <= startPos.X + searchDistance; x++)
                {
                    for (int y = startPos.Y - searchDistance; y <= startPos.Y + searchDistance; y++)
                    {
                        Point testPoint = new Point(x, y);
                        
                        if (testPoint == startPos) continue;
                        
                        if (IsPositionInMapBounds(testPoint))
                        {
                            int moveCost = troop.GetPossibleMoveByPosition(testPoint, kind);
                            
                            if (moveCost < IMPASSABLE_COST)
                            {
                                int distance = GetManhattanDistance(startPos, testPoint);
                                if (distance <= searchDistance)
                                {
                                    moveablePoints.Add(testPoint);
                                }
                            }
                        }
                    }
                }

                // 恢复原始移动力
                troop.MovabilityLeft = originalMovability;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 回退算法也出错: {ex.Message}");
            }

            return moveablePoints;
        }

        /// <summary>
        /// 获取视野内（或一定距离内）的友军列表
        /// </summary>
        /// <param name="me">当前部队</param>
        /// <param name="radius">搜索半径</param>
        /// <returns>友军列表</returns>
        public static List<Troop> GetNearbyAllies(Troop me, int radius = 10)
        {
            var allies = new List<Troop>();
            
            try
            {
                if (me?.BelongedFaction == null) return allies;

                // 遍历场景中的所有部队，筛选友军
                foreach (Troop t in Session.Current.Scenario.Troops.GetList())
                {
                    if (t != me && !t.Destroyed && t.BelongedFaction != null)
                    {
                        // 检查是否为友军
                        if (me.BelongedFaction.IsFriendly(t.BelongedFaction))
                        {
                            // 使用曼哈顿距离做快速筛选
                            if (GetManhattanDistance(t.Position, me.Position) <= radius)
                            {
                                allies.Add(t);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 获取友军时发生错误: {ex.Message}");
            }

            return allies;
        }

        // ================== 游戏底层对接方法 ==================

        /// <summary>
        /// 检查位置是否在地图有效范围内
        /// </summary>
        private static bool IsPositionValid(Point p)
        {
            try
            {
                // 使用游戏的地图边界检查
                if (Session.Current?.Scenario?.ScenarioMap == null)
                {
                    return false;
                }

                return !Session.Current.Scenario.PositionOutOfRange(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查位置有效性时出错: {ex.Message}");
                // 回退到简单边界检查
                return p.X >= 0 && p.Y >= 0 && p.X < 200 && p.Y < 200;
            }
        }

        /// <summary>
        /// 获取地形移动消耗
        /// </summary>
        private static int GetTerrainCost(Point p, Troop troop)
        {
            try
            {
                if (troop?.Army?.Kind == null)
                {
                    return IMPASSABLE_COST;
                }

                // 使用游戏原有的移动代价计算系统
                return troop.GetCostByPosition(p, false, -1, troop.Army.Kind);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 计算地形消耗时出错: {ex.Message}");
                // 默认消耗 1
                return 1;
            }
        }

        /// <summary>
        /// 检查位置是否被敌军阻挡
        /// </summary>
        private static bool IsBlockedByEnemy(Point p, Troop me)
        {
            try
            {
                // 检查该位置是否有其他部队
                var troopAtPosition = Session.Current.Scenario.GetTroopByPosition(p);
                if (troopAtPosition != null && troopAtPosition != me)
                {
                    // 如果是敌军，则阻挡
                    if (me.BelongedFaction != null && troopAtPosition.BelongedFaction != null)
                    {
                        return !me.BelongedFaction.IsFriendly(troopAtPosition.BelongedFaction);
                    }
                    // 如果无法判断势力关系，默认阻挡
                    return true;
                }

                // 检查是否有建筑阻挡
                var architectureAtPosition = Session.Current.Scenario.GetArchitectureByPosition(p);
                if (architectureAtPosition != null)
                {
                    // 如果是敌方建筑，则阻挡
                    if (me.BelongedFaction != null && architectureAtPosition.BelongedFaction != null)
                    {
                        return !me.BelongedFaction.IsFriendly(architectureAtPosition.BelongedFaction);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查敌军阻挡时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查位置是否被占据（无法作为终点）
        /// </summary>
        private static bool IsOccupied(Point p)
        {
            try
            {
                // 检查是否有部队占据该位置
                var troopAtPosition = Session.Current.Scenario.GetTroopByPosition(p);
                return troopAtPosition != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查位置占据时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用游戏原有寻路系统计算路径
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="start">起始位置</param>
        /// <param name="end">目标位置</param>
        /// <returns>路径点列表，如果无法到达则返回空列表</returns>
        public static List<Point> FindPath(Troop troop, Point start, Point end)
        {
            var path = new List<Point>();
            
            try
            {
                if (troop?.pathFinder == null || troop.Army?.Kind == null)
                {
                    Console.WriteLine("[MapNavigationHelper] 警告: 部队寻路器或兵种信息无效");
                    return path;
                }

                // 使用游戏原有的寻路系统
                MilitaryKind kind = troop.Army.Kind;
                
                // 保存当前状态
                int originalMovability = troop.MovabilityLeft;
                Point originalDestination = troop.Destination;
                
                // 临时设置移动力以进行路径计算
                troop.MovabilityLeft = troop.Movability;
                
                // 使用游戏的寻路系统
                if (troop.pathFinder.GetFirstTierPath(start, end, kind))
                {
                    if (troop.FirstTierPath != null && troop.FirstTierPath.Count > 0)
                    {
                        path.AddRange(troop.FirstTierPath);
                        Console.WriteLine($"[MapNavigationHelper] 成功计算路径，包含 {path.Count} 个节点");
                    }
                }
                else
                {
                    Console.WriteLine($"[MapNavigationHelper] 无法找到从 ({start.X},{start.Y}) 到 ({end.X},{end.Y}) 的路径");
                }
                
                // 恢复原始状态
                troop.MovabilityLeft = originalMovability;
                troop.Destination = originalDestination;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 寻路时发生错误: {ex.Message}");
                // 恢复状态
                if (troop != null)
                {
                    troop.MovabilityLeft = troop.Movability;
                }
            }

            return path;
        }

        /// <summary>
        /// 获取两点之间的移动代价
        /// 使用游戏原有的地形适应性系统
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <returns>移动代价，如果无法通行返回IMPASSABLE_COST</returns>
        public static int GetMovementCost(Troop troop, Point from, Point to)
        {
            try
            {
                if (troop?.Army?.Kind == null)
                {
                    return IMPASSABLE_COST;
                }

                // 检查是否为斜向移动
                bool oblique = (from.X != to.X) && (from.Y != to.Y);
                
                // 使用游戏原有的代价计算系统
                int cost = troop.GetCostByPosition(to, oblique, -1, troop.Army.Kind);
                
                return cost;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 计算移动代价时发生错误: {ex.Message}");
                return IMPASSABLE_COST;
            }
        }

        /// <summary>
        /// 检查位置是否可通行
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="position">位置</param>
        /// <returns>是否可通行</returns>
        public static bool IsPositionPassable(Troop troop, Point position)
        {
            try
            {
                if (troop?.Army?.Kind == null)
                {
                    return false;
                }

                // 检查地图边界
                if (!IsPositionInMapBounds(position))
                {
                    return false;
                }

                // 使用游戏原有的移动检查系统
                int cost = troop.GetPossibleMoveByPosition(position, troop.Army.Kind);
                return cost < IMPASSABLE_COST;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查位置通行性时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取位置周围的可通行邻居
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="position">中心位置</param>
        /// <param name="includeOblique">是否包含斜向位置</param>
        /// <returns>可通行的邻居位置列表</returns>
        public static List<Point> GetPassableNeighbors(Troop troop, Point position, bool includeOblique = true)
        {
            var neighbors = new List<Point>();
            
            try
            {
                // 四个基本方向
                var directions = new[]
                {
                    new Point(0, -1),  // 北
                    new Point(1, 0),   // 东
                    new Point(0, 1),   // 南
                    new Point(-1, 0)   // 西
                };

                // 四个斜向
                var obliqueDirections = new[]
                {
                    new Point(-1, -1), // 西北
                    new Point(1, -1),  // 东北
                    new Point(1, 1),   // 东南
                    new Point(-1, 1)   // 西南
                };

                // 检查基本方向
                foreach (var dir in directions)
                {
                    Point neighbor = new Point(position.X + dir.X, position.Y + dir.Y);
                    if (IsPositionPassable(troop, neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }

                // 检查斜向（如果启用）
                if (includeOblique)
                {
                    foreach (var dir in obliqueDirections)
                    {
                        Point neighbor = new Point(position.X + dir.X, position.Y + dir.Y);
                        if (IsPositionPassable(troop, neighbor))
                        {
                            neighbors.Add(neighbor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 获取邻居位置时发生错误: {ex.Message}");
            }

            return neighbors;
        }

        /// <summary>
        /// 获取指定范围内的所有敌对部队
        /// </summary>
        /// <param name="troop">己方部队</param>
        /// <param name="center">搜索中心</param>
        /// <param name="range">搜索范围</param>
        /// <returns>敌对部队列表</returns>
        public static List<Troop> GetEnemyTroopsInRange(Troop troop, Point center, int range)
        {
            var enemies = new List<Troop>();
            
            try
            {
                if (troop?.BelongedFaction == null)
                {
                    return enemies;
                }

                // 遍历场景中的所有部队
                foreach (Troop otherTroop in Session.Current.Scenario.Troops.GetList())
                {
                    if (otherTroop == null || otherTroop.Destroyed || otherTroop == troop)
                        continue;

                    // 检查是否为敌对势力
                    if (otherTroop.BelongedFaction != null && 
                        !troop.BelongedFaction.IsFriendly(otherTroop.BelongedFaction))
                    {
                        // 检查距离
                        int distance = GetManhattanDistance(center, otherTroop.Position);
                        if (distance <= range)
                        {
                            enemies.Add(otherTroop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 搜索敌军时发生错误: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 获取指定范围内的友军部队
        /// </summary>
        /// <param name="troop">己方部队</param>
        /// <param name="center">搜索中心</param>
        /// <param name="range">搜索范围</param>
        /// <returns>友军部队列表</returns>
        public static List<Troop> GetFriendlyTroopsInRange(Troop troop, Point center, int range)
        {
            var allies = new List<Troop>();
            
            try
            {
                if (troop?.BelongedFaction == null)
                {
                    return allies;
                }

                // 使用优化的GetNearbyAllies方法，然后过滤距离
                var nearbyAllies = GetNearbyAllies(troop, range * 2); // 扩大搜索范围以确保不遗漏
                
                foreach (var ally in nearbyAllies)
                {
                    int distance = GetManhattanDistance(center, ally.Position);
                    if (distance <= range)
                    {
                        allies.Add(ally);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 搜索友军时发生错误: {ex.Message}");
            }

            return allies;
        }

        /// <summary>
        /// 计算曼哈顿距离
        /// </summary>
        /// <param name="p1">位置1</param>
        /// <param name="p2">位置2</param>
        /// <returns>曼哈顿距离</returns>
        public static int GetManhattanDistance(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }

        /// <summary>
        /// 计算欧几里得距离
        /// </summary>
        /// <param name="p1">位置1</param>
        /// <param name="p2">位置2</param>
        /// <returns>欧几里得距离</returns>
        public static float GetEuclideanDistance(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 检查位置是否在地图边界内
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在边界内</returns>
        public static bool IsPositionInMapBounds(Point position)
        {
            try
            {
                if (Session.Current?.Scenario?.ScenarioMap == null)
                {
                    return false;
                }

                return position.X >= 0 && position.Y >= 0 &&
                       position.X < Session.Current.Scenario.ScenarioMap.MapDimensions.X &&
                       position.Y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查地图边界时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取最佳撤退位置
        /// 寻找远离敌军、靠近友军的安全位置
        /// </summary>
        /// <param name="troop">需要撤退的部队</param>
        /// <param name="enemies">敌军列表</param>
        /// <param name="allies">友军列表</param>
        /// <returns>最佳撤退位置</returns>
        public static Point GetBestRetreatPosition(Troop troop, List<Troop> enemies, List<Troop> allies)
        {
            Point bestPosition = troop.Position;
            float bestScore = float.MinValue;

            try
            {
                // 获取可移动区域
                var moveablePositions = GetUnitMoveableArea(troop, troop.Movability / 5);

                foreach (var position in moveablePositions)
                {
                    float score = 0f;

                    // 计算与敌军的距离（越远越好）
                    foreach (var enemy in enemies)
                    {
                        int distance = GetManhattanDistance(position, enemy.Position);
                        score += distance * 10f; // 远离敌军的奖励
                    }

                    // 计算与友军的距离（适中最好）
                    foreach (var ally in allies)
                    {
                        int distance = GetManhattanDistance(position, ally.Position);
                        if (distance >= 2 && distance <= 5) // 理想距离范围
                        {
                            score += 50f;
                        }
                        else if (distance > 5)
                        {
                            score -= (distance - 5) * 5f; // 太远的惩罚
                        }
                    }

                    // 更新最佳位置
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPosition = position;
                    }
                }

                Console.WriteLine($"[MapNavigationHelper] 为部队 {troop.ID} 找到撤退位置 ({bestPosition.X},{bestPosition.Y})，评分: {bestScore:F1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 计算撤退位置时发生错误: {ex.Message}");
            }

            return bestPosition;
        }

        /// <summary>
        /// 检查两个位置之间是否有直线视野（无障碍物阻挡）
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <returns>是否有直线视野</returns>
        public static bool HasLineOfSight(Troop troop, Point from, Point to)
        {
            try
            {
                // 使用Bresenham直线算法检查路径上的每个点
                var linePoints = GetLinePoints(from, to);
                
                foreach (var point in linePoints)
                {
                    // 跳过起点和终点
                    if (point == from || point == to) continue;
                    
                    // 检查是否有障碍物
                    if (!IsPositionPassable(troop, point))
                    {
                        return false;
                    }
                    
                    // 检查是否有其他部队阻挡
                    var troopAtPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (troopAtPosition != null && troopAtPosition != troop)
                    {
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapNavigationHelper] 检查视线时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Bresenham算法获取两点之间的直线上的所有点
        /// </summary>
        /// <param name="from">起始点</param>
        /// <param name="to">结束点</param>
        /// <returns>直线上的点列表</returns>
        private static List<Point> GetLinePoints(Point from, Point to)
        {
            var points = new List<Point>();
            
            int x0 = from.X, y0 = from.Y;
            int x1 = to.X, y1 = to.Y;
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                points.Add(new Point(x0, y0));
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
            
            return points;
        }
    }
}