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
    /// 全局异步寻路管理器（单例）
    /// 运算界：后台线程死循环处理寻路请求
    /// </summary>
    public sealed class AsyncPathfindingManager : IDisposable
    {
        public static AsyncPathfindingManager Instance { get; } = new();

        private readonly Channel<PathRequest> _requestChannel;
        private readonly ConcurrentQueue<PathResult> _resultQueue;
        private readonly CancellationTokenSource _systemCts = new();
        private const int WorkerCount = 2; // 维持2个后台线程即可

        // 🔥 性能优化：静态地形成本数组（全局共享，零分配）
        private const int MapWidth = 200;
        private const int MapHeight = 200;
        private static byte[] _staticTerrainKinds = new byte[MapWidth * MapHeight];
        private static bool[] _staticTerrainCannotExtend = new bool[MapWidth * MapHeight];
        private readonly object _terrainSnapshotLock = new();
        private volatile bool _terrainSnapshotReady;
        private object? _terrainSnapshotIdentity;
        private const int HardBlockPenalty = 1000000;
        private const int DefaultTerrainCost = 1;
        private static readonly int[] NeighborDx = [0, 1, 0, -1, -1, 1, 1, -1];
        private static readonly int[] NeighborDy = [-1, 0, 1, 0, -1, -1, 1, 1];

        private AsyncPathfindingManager()
        {
            _requestChannel = Channel.CreateUnbounded<PathRequest>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
            _resultQueue = new ConcurrentQueue<PathResult>();

            // 启动后台工作线程（使用 LongRunning 创建独立线程）
            for (int i = 0; i < WorkerCount; i++)
            {
                Task.Factory.StartNew(WorkerLoop, TaskCreationOptions.LongRunning);
            }

            System.Diagnostics.Debug.WriteLine($"[AsyncPathfindingManager] 初始化完成，启动 {WorkerCount} 个工作线程");
        }

        /// <summary>
        /// 入队寻路请求（主线程调用）
        /// </summary>
        public void EnqueueRequest(PathRequest request)
        {
            _requestChannel.Writer.TryWrite(request);
        }

        public void EnsureTerrainSnapshot(global::GameObjects.GameScenario scenario)
        {
            var scenarioMap = scenario.ScenarioMap;
            var mapData = scenarioMap.MapData;
            if (mapData == null)
            {
                throw new InvalidOperationException("ScenarioMap.MapData is null while initializing async terrain snapshot.");
            }

            if (_terrainSnapshotReady && ReferenceEquals(_terrainSnapshotIdentity, mapData))
            {
                return;
            }

            lock (_terrainSnapshotLock)
            {
                if (_terrainSnapshotReady && ReferenceEquals(_terrainSnapshotIdentity, mapData))
                {
                    return;
                }

                int width = scenarioMap.MapDimensions.X;
                int height = scenarioMap.MapDimensions.Y;
                if (width != MapWidth || height != MapHeight)
                {
                    throw new InvalidOperationException(
                        $"Async pathfinding map size mismatch. Expected {MapWidth}x{MapHeight}, actual {width}x{height}.");
                }

                var terrainKinds = new byte[MapWidth * MapHeight];
                var terrainCannotExtend = new bool[MapWidth * MapHeight];

                for (int y = 0; y < MapHeight; y++)
                {
                    int rowOffset = y * MapWidth;
                    for (int x = 0; x < MapWidth; x++)
                    {
                        int terrainId = mapData[x, y];
                        if ((uint)terrainId > byte.MaxValue)
                        {
                            throw new InvalidOperationException(
                                $"Terrain id out of byte range at ({x},{y}): {terrainId}");
                        }

                        int index = rowOffset + x;
                        terrainKinds[index] = (byte)terrainId;
                        var terrainDetail = scenario.GameCommonData.AllTerrainDetails.GetTerrainDetail(terrainId);
                        terrainCannotExtend[index] = !terrainDetail.CanExtendInto;
                    }
                }

                Volatile.Write(ref _staticTerrainKinds, terrainKinds);
                Volatile.Write(ref _staticTerrainCannotExtend, terrainCannotExtend);
                _terrainSnapshotIdentity = mapData;
                _terrainSnapshotReady = true;
            }
        }

        /// <summary>
        /// 主线程调用：每帧处理完成的寻路结果
        /// </summary>
        public void ProcessCompletedPaths(Action<PathResult> onResultProcessed)
        {
            int maxProcessPerFrame = 30; // 错峰处理，防止卡顿
            int count = 0;

            while (count < maxProcessPerFrame && _resultQueue.TryDequeue(out var result))
            {
                onResultProcessed(result);
                count++;
            }
        }

        /// <summary>
        /// 后台工作线程循环（纯同步阻塞式 + 极致性能优化）
        /// 注意：去掉 async，因为本身就在 LongRunning 独立线程里，阻塞是绝对安全的
        /// </summary>
        private void WorkerLoop()
        {
            // 🔥 性能优化：线程本地动态地图（每个工作线程独立，避免竞争）
            int[] threadLocalMap = new int[MapWidth * MapHeight];

            try
            {
                // 纯同步阻塞式读取，性能最优
                while (_requestChannel.Reader.WaitToReadAsync(_systemCts.Token).AsTask().GetAwaiter().GetResult())
                {
                    // 批量读取所有可用请求
                    while (_requestChannel.Reader.TryRead(out var req))
                    {
                        if (req.CancellationToken.IsCancellationRequested)
                        {
                            // 取消时回收 ArrayPool 数组（不清理内容以压榨性能）
                            ArrayPool<int>.Shared.Return(req.PooledTraversalCosts, clearArray: false);
                            ArrayPool<int>.Shared.Return(req.PooledTraversalPenalties, clearArray: false);
                            ArrayPool<DynamicObstacle>.Shared.Return(req.PooledObstacles, clearArray: false);
                            _resultQueue.Enqueue(new PathResult(
                                req.TroopId,
                                req.TrackingId,
                                null,
                                false,
                                true
                            ));
                            continue;
                        }

                        // 🔥 性能优化：使用 Span 零拷贝遍历
                        ReadOnlySpan<int> traversalCosts = req.PooledTraversalCosts.AsSpan(0, req.TraversalCostCount);
                        ReadOnlySpan<int> traversalPenalties = req.PooledTraversalPenalties.AsSpan(0, req.TraversalPenaltyCount);
                        ReadOnlySpan<DynamicObstacle> obstacles = req.PooledObstacles.AsSpan(0, req.ObstacleCount);
                        List<Point>? path = null;

                        try
                        {
                            // Step 1: 涂抹（Apply）- 使用 ref readonly 避免结构体值拷贝
                            foreach (ref readonly var obs in obstacles)
                            {
                                // 极简边界防御（如果在组装期保证了安全，这里也可省去）
                                if (obs.X >= 0 && obs.X < MapWidth && obs.Y >= 0 && obs.Y < MapHeight)
                                {
                                    int index = obs.Y * MapWidth + obs.X;
                                    threadLocalMap[index] += obs.PenaltyScore;
                                }
                            }

                            // Step 2: 纯净 A* 寻路
                            var terrainKinds = Volatile.Read(ref _staticTerrainKinds);
                            var terrainCannotExtend = Volatile.Read(ref _staticTerrainCannotExtend);
                            path = ExecuteAStar(req, threadLocalMap, traversalCosts, traversalPenalties, terrainKinds, terrainCannotExtend);
                            if (req.CancellationToken.IsCancellationRequested)
                            {
                                PathPool.Return(path);
                                _resultQueue.Enqueue(new PathResult(
                                    req.TroopId,
                                    req.TrackingId,
                                    null,
                                    false,
                                    true
                                ));
                            }
                            else
                            {
                                // 入队结果
                                _resultQueue.Enqueue(new PathResult(
                                    req.TroopId,
                                    req.TrackingId,
                                    path,
                                    path != null && path.Count > 0,
                                    false
                                ));
                            }
                        }
                        catch (Exception ex)
                        {
                            // 记录后台异常，并封装为失败结果
                            System.Diagnostics.Debug.WriteLine($"[AsyncPathfindingManager] A* 异常: {ex.Message}");
                            _resultQueue.Enqueue(new PathResult(
                                req.TroopId,
                                req.TrackingId,
                                null,
                                false,
                                false
                            ));
                        }
                        finally
                        {
                            // Step 3: 擦除（Revert）- 无论 A* 成功、失败还是抛错，绝对保证地图纯净！
                            foreach (ref readonly var obs in obstacles)
                            {
                                if (obs.X >= 0 && obs.X < MapWidth && obs.Y >= 0 && obs.Y < MapHeight)
                                {
                                    int index = obs.Y * MapWidth + obs.X;
                                    threadLocalMap[index] -= obs.PenaltyScore;
                                }
                            }

                            // 归还快照数组给内存池（绝不执行 clearArray）
                            ArrayPool<int>.Shared.Return(req.PooledTraversalCosts, clearArray: false);
                            ArrayPool<int>.Shared.Return(req.PooledTraversalPenalties, clearArray: false);
                            ArrayPool<DynamicObstacle>.Shared.Return(req.PooledObstacles, clearArray: false);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常退出
                System.Diagnostics.Debug.WriteLine("[AsyncPathfindingManager] 工作线程正常退出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AsyncPathfindingManager] 工作线程异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 A* 寻路算法（纯计算，不访问游戏状态）
        /// </summary>
        /// <param name="req">寻路请求</param>
        /// <param name="dynamicMap">线程本地动态地图（已涂抹障碍物）</param>
        /// <param name="staticTerrain">静态地形成本数组</param>
        /// <returns>路径点列表，失败返回 null</returns>
        private List<Point>? ExecuteAStar(
            PathRequest req,
            int[] dynamicMap,
            ReadOnlySpan<int> traversalCosts,
            ReadOnlySpan<int> traversalPenalties,
            byte[] terrainKinds,
            bool[] terrainCannotExtend)
        {
            int startX = req.StartPosition.X;
            int startY = req.StartPosition.Y;
            int targetX = req.TargetPosition.X;
            int targetY = req.TargetPosition.Y;

            if ((uint)startX >= MapWidth || (uint)startY >= MapHeight ||
                (uint)targetX >= MapWidth || (uint)targetY >= MapHeight)
            {
                return null;
            }

            if (startX == targetX && startY == targetY)
            {
                return null;
            }

            int nodeCount = MapWidth * MapHeight;
            if (!_terrainSnapshotReady || terrainKinds.Length != nodeCount || terrainCannotExtend.Length != nodeCount)
            {
                throw new InvalidOperationException("Async terrain snapshot is not ready or has invalid size.");
            }
            if (req.TraversalCostCount != nodeCount || traversalCosts.Length != nodeCount)
            {
                throw new InvalidOperationException("Async traversal-cost snapshot has invalid size.");
            }
            if (req.TraversalPenaltyCount != nodeCount || traversalPenalties.Length != nodeCount)
            {
                throw new InvalidOperationException("Async traversal-penalty snapshot has invalid size.");
            }

            int[] gScore = ArrayPool<int>.Shared.Rent(nodeCount);
            int[] cameFrom = ArrayPool<int>.Shared.Rent(nodeCount);
            byte[] nodeState = ArrayPool<byte>.Shared.Rent(nodeCount);
            int[] openSet = ArrayPool<int>.Shared.Rent(nodeCount);
            int openCount = 0;

            var path = PathPool.Rent();
            int startIndex = startY * MapWidth + startX;
            int targetIndex = targetY * MapWidth + targetX;

            try
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    gScore[i] = int.MaxValue;
                    cameFrom[i] = -1;
                    nodeState[i] = 0;
                }

                gScore[startIndex] = 0;
                nodeState[startIndex] = 1;
                openSet[openCount++] = startIndex;

                bool found = false;

                while (openCount > 0)
                {
                    if (req.CancellationToken.IsCancellationRequested)
                    {
                        PathPool.Return(path);
                        return null;
                    }

                    int bestPos = 0;
                    int currentIndex = openSet[0];
                    int currentX = currentIndex % MapWidth;
                    int currentY = currentIndex / MapWidth;
                    int bestF = gScore[currentIndex] + Heuristic(currentX, currentY, targetX, targetY);

                    for (int i = 1; i < openCount; i++)
                    {
                        int candidateIndex = openSet[i];
                        int candidateX = candidateIndex % MapWidth;
                        int candidateY = candidateIndex / MapWidth;
                        int candidateF = gScore[candidateIndex] + Heuristic(candidateX, candidateY, targetX, targetY);
                        if (candidateF < bestF)
                        {
                            bestF = candidateF;
                            bestPos = i;
                            currentIndex = candidateIndex;
                        }
                    }

                    openCount--;
                    openSet[bestPos] = openSet[openCount];
                    nodeState[currentIndex] = 2;

                    if (currentIndex == targetIndex)
                    {
                        found = true;
                        break;
                    }

                    currentX = currentIndex % MapWidth;
                    currentY = currentIndex / MapWidth;
                    int currentG = gScore[currentIndex];

                    for (int dir = 0; dir < NeighborDx.Length; dir++)
                    {
                        int nx = currentX + NeighborDx[dir];
                        int ny = currentY + NeighborDy[dir];

                        if ((uint)nx >= MapWidth || (uint)ny >= MapHeight)
                        {
                            continue;
                        }

                        int neighborIndex = ny * MapWidth + nx;
                        if (nodeState[neighborIndex] == 2)
                        {
                            continue;
                        }

                        if (!IsTilePassable(
                                neighborIndex,
                                targetIndex,
                                req.TargetHasArchitecture,
                                dynamicMap,
                                traversalCosts,
                                traversalPenalties))
                        {
                            continue;
                        }

                        int stepTraversalCost = GetStepTraversalCost(
                            neighborIndex,
                            currentX,
                            currentY,
                            nx,
                            ny,
                            targetIndex,
                            req.TargetHasArchitecture,
                            traversalCosts);
                        if (stepTraversalCost >= TerrainCostProfile.UnreachableTerrainCost)
                        {
                            continue;
                        }

                        int dynamicPenalty = dynamicMap[neighborIndex];
                        int traversalPenalty = traversalPenalties[neighborIndex];
                        int moveWeight = NeighborDx[dir] != 0 && NeighborDy[dir] != 0 ? 7 : 5;
                        long tentativeG64 = (long)currentG + (long)moveWeight * stepTraversalCost + traversalPenalty + dynamicPenalty;
                        if (tentativeG64 >= gScore[neighborIndex] || tentativeG64 >= int.MaxValue)
                        {
                            continue;
                        }
                        int tentativeG = (int)tentativeG64;

                        cameFrom[neighborIndex] = currentIndex;
                        gScore[neighborIndex] = tentativeG;

                        if (nodeState[neighborIndex] != 1)
                        {
                            nodeState[neighborIndex] = 1;
                            openSet[openCount++] = neighborIndex;
                        }
                    }
                }

                if (!found)
                {
                    PathPool.Return(path);
                    return null;
                }

                int walk = targetIndex;
                while (walk != startIndex)
                {
                    int walkX = walk % MapWidth;
                    int walkY = walk / MapWidth;
                    path.Add(new Point(walkX, walkY));
                    walk = cameFrom[walk];
                    if (walk < 0)
                    {
                        PathPool.Return(path);
                        return null;
                    }
                }

                path.Reverse();
                if (path.Count > 0)
                {
                    return path;
                }

                PathPool.Return(path);
                return null;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(gScore, clearArray: false);
                ArrayPool<int>.Shared.Return(cameFrom, clearArray: false);
                ArrayPool<byte>.Shared.Return(nodeState, clearArray: false);
                ArrayPool<int>.Shared.Return(openSet, clearArray: false);
            }
        }

        private static int GetStepTraversalCost(
            int tileIndex,
            int currentX,
            int currentY,
            int targetX,
            int targetY,
            int targetIndex,
            bool targetHasArchitecture,
            ReadOnlySpan<int> traversalCosts)
        {
            int traversalCost = traversalCosts[tileIndex];
            bool targetArchitectureBypass = tileIndex == targetIndex && targetHasArchitecture;
            if (traversalCost >= TerrainCostProfile.UnreachableTerrainCost && targetArchitectureBypass)
            {
                traversalCost = DefaultTerrainCost;
            }

            if (currentX != targetX && currentY != targetY)
            {
                int horizontalIndex = currentY * MapWidth + targetX;
                int verticalIndex = targetY * MapWidth + currentX;
                int directionCost = Math.Min(traversalCosts[horizontalIndex], traversalCosts[verticalIndex]);
                traversalCost = Math.Max(traversalCost, directionCost);
            }

            return traversalCost <= 0 ? DefaultTerrainCost : traversalCost;
        }

        private static bool IsTilePassable(
            int tileIndex,
            int targetIndex,
            bool targetHasArchitecture,
            int[] dynamicMap,
            ReadOnlySpan<int> traversalCosts,
            ReadOnlySpan<int> traversalPenalties)
        {
            bool targetArchitectureBypass = tileIndex == targetIndex && targetHasArchitecture;
            if (dynamicMap[tileIndex] >= HardBlockPenalty && !targetArchitectureBypass)
            {
                return false;
            }

            int traversalCost = traversalCosts[tileIndex];
            if (traversalCost >= TerrainCostProfile.UnreachableTerrainCost && !targetArchitectureBypass)
            {
                return false;
            }

            int traversalPenalty = traversalPenalties[tileIndex];
            if (traversalPenalty >= TerrainCostProfile.UnreachableTerrainCost)
            {
                return false;
            }

            return true;
        }

        private static int Heuristic(int x, int y, int targetX, int targetY)
        {
            int dx = Math.Abs(targetX - x);
            int dy = Math.Abs(targetY - y);
            int min = Math.Min(dx, dy);
            int max = Math.Max(dx, dy);
            return max * 5 + min * 2;
        }

        public void Dispose()
        {
            _systemCts.Cancel();
            _systemCts.Dispose();
            System.Diagnostics.Debug.WriteLine("[AsyncPathfindingManager] 已释放");
        }
    }
}
