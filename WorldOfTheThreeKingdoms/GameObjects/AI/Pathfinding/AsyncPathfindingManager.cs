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
        private static readonly byte[] StaticTerrainCosts = new byte[40000];
        private const int MapWidth = 200;
        private const int MapHeight = 200;

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
                            ArrayPool<DynamicObstacle>.Shared.Return(req.PooledObstacles, clearArray: false);
                            continue;
                        }

                        // 🔥 性能优化：使用 Span 零拷贝遍历
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
                            path = ExecuteAStar(req, threadLocalMap, StaticTerrainCosts);

                            // 入队结果
                            _resultQueue.Enqueue(new PathResult(
                                req.TroopId,
                                req.TrackingId,
                                path,
                                path != null && path.Count > 0,
                                false
                            ));
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
            byte[] staticTerrain)
        {
            // TODO: 替换为实际的 A* 实现
            // 当前是临时实现：简单的直线路径

            var path = PathPool.Rent();

            int dx = Math.Sign(req.TargetPosition.X - req.StartPosition.X);
            int dy = Math.Sign(req.TargetPosition.Y - req.StartPosition.Y);

            Point current = req.StartPosition;
            int maxSteps = 100; // 防止死循环
            int steps = 0;

            while (current != req.TargetPosition && steps < maxSteps)
            {
                if (req.CancellationToken.IsCancellationRequested)
                {
                    PathPool.Return(path);
                    return null;
                }

                if (current.X != req.TargetPosition.X)
                {
                    current.X += dx;
                }
                else if (current.Y != req.TargetPosition.Y)
                {
                    current.Y += dy;
                }

                path.Add(current);
                steps++;
            }

            return path.Count > 0 ? path : null;
        }

        public void Dispose()
        {
            _systemCts.Cancel();
            _systemCts.Dispose();
            System.Diagnostics.Debug.WriteLine("[AsyncPathfindingManager] 已释放");
        }
    }
}
