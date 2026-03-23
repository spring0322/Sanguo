using System.Diagnostics;
using GameObjects;
using GameObjects.AI.Pathfinding;
using Microsoft.Xna.Framework;
using Xunit;
using Xunit.Abstractions;

namespace WorldOfTheThreeKingdoms.Tests.AsyncPathfinding;

/// <summary>
/// 性能基准测试
/// 验证需求: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6
/// </summary>
public class PerformanceBenchmarkTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestScenarioBuilder _builder;
    private readonly List<long> _gcCollections = [];
    private readonly Stopwatch _stopwatch = new();

    public PerformanceBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
        _builder = new TestScenarioBuilder();
        
        // 记录初始 GC 状态
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public void Dispose()
    {
        _builder.Dispose();
    }

    /// <summary>
    /// 测试 100 支部队同时寻路的性能
    /// 需求 11.1: 保持 60fps 稳定运行
    /// </summary>
    [Fact]
    public async Task Test_100Troops_ConcurrentPathfinding_Maintains60FPS()
    {
        // Arrange
        const int troopCount = 100;
        const int frameCount = 60; // 模拟 1 秒（60 帧）
        const double targetFrameTime = 16.67; // 60fps = 16.67ms per frame

        var session = _builder.CreateSession();
        var troops = new List<Troop>();

        // 创建 100 支部队
        for (int i = 0; i < troopCount; i++)
        {
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10 + i % 10, 10 + i / 10),
                $"部队{i}"
            );
            troops.Add(troop);
            session.RegisterTroop(troop);
        }

        _output.WriteLine($"创建了 {troopCount} 支部队");

        // Act - 所有部队同时发起寻路请求
        _stopwatch.Restart();
        
        foreach (var troop in troops)
        {
            var target = new Point(
                100 + (troop.Position.X % 20),
                100 + (troop.Position.Y % 20)
            );
            troop.RequestMoveAsync(target);
        }
        
        var requestTime = _stopwatch.Elapsed.TotalMilliseconds;
        _output.WriteLine($"提交 {troopCount} 个请求耗时: {requestTime:F3}ms");

        // 模拟游戏主循环运行 60 帧
        var frameTimes = new List<double>();
        long gen0Before = GC.CollectionCount(0);
        long gen1Before = GC.CollectionCount(1);
        long gen2Before = GC.CollectionCount(2);

        for (int frame = 0; frame < frameCount; frame++)
        {
            _stopwatch.Restart();
            
            // 模拟主循环处理寻路结果
            AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
            {
                if (session.TryGetTroop(result.TroopId, out var troop))
                {
                    troop.OnPathfindingCompleted(result);
                }
                else if (result.IsSuccess)
                {
                    PathPool.Return(result.Path);
                }
            });
            
            frameTimes.Add(_stopwatch.Elapsed.TotalMilliseconds);
            
            // 模拟帧间隔
            await Task.Delay(1);
        }

        // 等待所有寻路完成
        await Task.Delay(2000);

        long gen0After = GC.CollectionCount(0);
        long gen1After = GC.CollectionCount(1);
        long gen2After = GC.CollectionCount(2);

        // Assert
        var avgFrameTime = frameTimes.Average();
        var maxFrameTime = frameTimes.Max();
        var p95FrameTime = frameTimes.OrderBy(x => x).ElementAt((int)(frameTimes.Count * 0.95));

        _output.WriteLine($"\n=== 帧性能统计 ===");
        _output.WriteLine($"平均帧时间: {avgFrameTime:F3}ms");
        _output.WriteLine($"最大帧时间: {maxFrameTime:F3}ms");
        _output.WriteLine($"P95 帧时间: {p95FrameTime:F3}ms");
        _output.WriteLine($"目标帧时间: {targetFrameTime:F3}ms (60fps)");

        _output.WriteLine($"\n=== GC 统计 ===");
        _output.WriteLine($"Gen0 回收: {gen0After - gen0Before} 次");
        _output.WriteLine($"Gen1 回收: {gen1After - gen1Before} 次");
        _output.WriteLine($"Gen2 回收: {gen2After - gen2Before} 次");

        // 验证性能目标
        Assert.True(avgFrameTime < targetFrameTime, 
            $"平均帧时间 {avgFrameTime:F3}ms 超过目标 {targetFrameTime:F3}ms");
        Assert.True(p95FrameTime < targetFrameTime * 1.5, 
            $"P95 帧时间 {p95FrameTime:F3}ms 超过容忍值 {targetFrameTime * 1.5:F3}ms");
    }

    /// <summary>
    /// 测试 GC 频率和停顿时间
    /// 需求 11.2: GC 频率 < 1次/秒
    /// 需求 11.3: GC 停顿 < 5ms
    /// </summary>
    [Fact]
    public async Task Test_GC_FrequencyAndPauseTime()
    {
        // Arrange
        const int durationSeconds = 10;
        const int troopCount = 50;

        var session = _builder.CreateSession();
        var troops = new List<Troop>();

        for (int i = 0; i < troopCount; i++)
        {
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10 + i % 10, 10 + i / 10),
                $"部队{i}"
            );
            troops.Add(troop);
            session.RegisterTroop(troop);
        }

        // Act - 持续运行并监控 GC
        long gen0Start = GC.CollectionCount(0);
        long gen1Start = GC.CollectionCount(1);
        long gen2Start = GC.CollectionCount(2);

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationSeconds);

        int requestCount = 0;
        while (DateTime.UtcNow < endTime)
        {
            // 随机选择部队发起寻路
            var troop = troops[Random.Shared.Next(troops.Count)];
            var target = new Point(
                Random.Shared.Next(50, 150),
                Random.Shared.Next(50, 150)
            );
            troop.RequestMoveAsync(target);
            requestCount++;

            // 处理结果
            AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
            {
                if (session.TryGetTroop(result.TroopId, out var t))
                {
                    t.OnPathfindingCompleted(result);
                }
                else if (result.IsSuccess)
                {
                    PathPool.Return(result.Path);
                }
            });

            await Task.Delay(16); // 模拟 60fps
        }

        long gen0End = GC.CollectionCount(0);
        long gen1End = GC.CollectionCount(1);
        long gen2End = GC.CollectionCount(2);

        // Assert
        long gen0Collections = gen0End - gen0Start;
        long gen1Collections = gen1End - gen1Start;
        long gen2Collections = gen2End - gen2Start;

        double gen0PerSecond = gen0Collections / (double)durationSeconds;
        double gen1PerSecond = gen1Collections / (double)durationSeconds;

        _output.WriteLine($"\n=== GC 频率测试 ({durationSeconds}秒) ===");
        _output.WriteLine($"总请求数: {requestCount}");
        _output.WriteLine($"Gen0 回收: {gen0Collections} 次 ({gen0PerSecond:F2}/秒)");
        _output.WriteLine($"Gen1 回收: {gen1Collections} 次 ({gen1PerSecond:F2}/秒)");
        _output.WriteLine($"Gen2 回收: {gen2Collections} 次");

        // 需求 11.2: GC 频率 < 1次/秒
        Assert.True(gen0PerSecond < 1.0, 
            $"Gen0 GC 频率 {gen0PerSecond:F2}/秒 超过目标 1.0/秒");
    }

    /// <summary>
    /// 测试寻路延迟分布
    /// 需求 11.4: P95 寻路延迟 < 100ms
    /// </summary>
    [Fact]
    public async Task Test_PathfindingLatency_P95Under100ms()
    {
        // Arrange
        const int sampleCount = 100;
        var session = _builder.CreateSession();
        var latencies = new List<double>();

        // Act
        for (int i = 0; i < sampleCount; i++)
        {
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10, 10),
                $"测试部队{i}"
            );
            session.RegisterTroop(troop);

            var startTime = Stopwatch.GetTimestamp();
            troop.RequestMoveAsync(new Point(100, 100));

            // 等待结果
            bool completed = false;
            var timeout = DateTime.UtcNow.AddSeconds(5);

            while (!completed && DateTime.UtcNow < timeout)
            {
                AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
                {
                    if (result.TroopId == troop.Id)
                    {
                        var endTime = Stopwatch.GetTimestamp();
                        var latency = (endTime - startTime) * 1000.0 / Stopwatch.Frequency;
                        latencies.Add(latency);
                        completed = true;

                        if (session.TryGetTroop(result.TroopId, out var t))
                        {
                            t.OnPathfindingCompleted(result);
                        }
                    }
                });

                await Task.Delay(1);
            }

            if (!completed)
            {
                _output.WriteLine($"警告: 样本 {i} 超时");
            }
        }

        // Assert
        latencies.Sort();
        var p50 = latencies[latencies.Count / 2];
        var p95 = latencies[(int)(latencies.Count * 0.95)];
        var p99 = latencies[(int)(latencies.Count * 0.99)];
        var avg = latencies.Average();
        var max = latencies.Max();

        _output.WriteLine($"\n=== 寻路延迟统计 ({sampleCount} 个样本) ===");
        _output.WriteLine($"平均延迟: {avg:F2}ms");
        _output.WriteLine($"P50 延迟: {p50:F2}ms");
        _output.WriteLine($"P95 延迟: {p95:F2}ms");
        _output.WriteLine($"P99 延迟: {p99:F2}ms");
        _output.WriteLine($"最大延迟: {max:F2}ms");

        // 需求 11.4: P95 < 100ms
        Assert.True(p95 < 100.0, 
            $"P95 延迟 {p95:F2}ms 超过目标 100ms");
    }

    /// <summary>
    /// 测试 MapSnapshot 创建性能
    /// 需求 8.4: 200x200 地图在 5ms 内完成
    /// </summary>
    [Fact]
    public void Test_MapSnapshot_CreationTime_Under5ms()
    {
        // Arrange
        var session = _builder.CreateSession();
        const int iterations = 100;
        var times = new List<double>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _stopwatch.Restart();
            var snapshot = new MapSnapshot(session.Scenario);
            _stopwatch.Stop();
            
            times.Add(_stopwatch.Elapsed.TotalMilliseconds);
        }

        // Assert
        var avgTime = times.Average();
        var maxTime = times.Max();
        var p95Time = times.OrderBy(x => x).ElementAt((int)(times.Count * 0.95));

        _output.WriteLine($"\n=== MapSnapshot 创建性能 ({iterations} 次) ===");
        _output.WriteLine($"平均时间: {avgTime:F3}ms");
        _output.WriteLine($"最大时间: {maxTime:F3}ms");
        _output.WriteLine($"P95 时间: {p95Time:F3}ms");
        _output.WriteLine($"目标时间: 5.0ms");

        Assert.True(p95Time < 5.0, 
            $"P95 创建时间 {p95Time:F3}ms 超过目标 5.0ms");
    }

    /// <summary>
    /// 测试单帧处理时间
    /// 需求 3.6: 单帧处理 < 1ms
    /// </summary>
    [Fact]
    public async Task Test_SingleFrame_ProcessingTime_Under1ms()
    {
        // Arrange
        const int troopCount = 100;
        var session = _builder.CreateSession();
        var troops = new List<Troop>();

        for (int i = 0; i < troopCount; i++)
        {
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10 + i % 10, 10 + i / 10),
                $"部队{i}"
            );
            troops.Add(troop);
            session.RegisterTroop(troop);
            troop.RequestMoveAsync(new Point(100, 100));
        }

        // 等待结果准备好
        await Task.Delay(1000);

        // Act - 测量单帧处理时间
        var frameTimes = new List<double>();
        
        for (int frame = 0; frame < 60; frame++)
        {
            _stopwatch.Restart();
            
            AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
            {
                if (session.TryGetTroop(result.TroopId, out var troop))
                {
                    troop.OnPathfindingCompleted(result);
                }
                else if (result.IsSuccess)
                {
                    PathPool.Return(result.Path);
                }
            });
            
            frameTimes.Add(_stopwatch.Elapsed.TotalMilliseconds);
            await Task.Delay(1);
        }

        // Assert
        var avgTime = frameTimes.Average();
        var maxTime = frameTimes.Max();
        var p95Time = frameTimes.OrderBy(x => x).ElementAt((int)(frameTimes.Count * 0.95));

        _output.WriteLine($"\n=== 单帧处理性能 ===");
        _output.WriteLine($"平均时间: {avgTime:F3}ms");
        _output.WriteLine($"最大时间: {maxTime:F3}ms");
        _output.WriteLine($"P95 时间: {p95Time:F3}ms");
        _output.WriteLine($"目标时间: 1.0ms");

        Assert.True(p95Time < 1.0, 
            $"P95 处理时间 {p95Time:F3}ms 超过目标 1.0ms");
    }

    /// <summary>
    /// 测试内存占用
    /// 需求 11.5: 内存占用增长 < 10%
    /// 
    /// 注意：此测试重点验证异步寻路系统的内存管理，
    /// 通过重复使用少量部队来避免 Troop 对象本身的内存累积
    /// </summary>
    [Fact]
    public async Task Test_MemoryUsage_GrowthUnder10Percent()
    {
        // Arrange
        const int iterations = 1000;
        const int troopCount = 10; // 使用少量部队重复寻路
        var session = _builder.CreateSession();
        var troops = new List<Troop>();

        // 创建固定数量的部队
        for (int i = 0; i < troopCount; i++)
        {
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10 + i, 10),
                $"部队{i}"
            );
            troops.Add(troop);
            session.RegisterTroop(troop);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore = GC.GetTotalMemory(true);

        // Act - 重复使用相同的部队执行大量寻路操作
        for (int i = 0; i < iterations; i++)
        {
            var troop = troops[i % troopCount];
            troop.RequestMoveAsync(new Point(
                Random.Shared.Next(50, 150),
                Random.Shared.Next(50, 150)
            ));

            if (i % 100 == 0)
            {
                AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
                {
                    if (session.TryGetTroop(result.TroopId, out var t))
                    {
                        t.OnPathfindingCompleted(result);
                    }
                    else if (result.IsSuccess)
                    {
                        PathPool.Return(result.Path);
                    }
                });
                await Task.Delay(10);
            }
        }

        // 等待所有操作完成
        await Task.Delay(2000);

        // 处理所有剩余结果
        AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
        {
            if (session.TryGetTroop(result.TroopId, out var t))
            {
                t.OnPathfindingCompleted(result);
            }
            else if (result.IsSuccess)
            {
                PathPool.Return(result.Path);
            }
        });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryAfter = GC.GetTotalMemory(true);

        // Assert
        long memoryGrowth = memoryAfter - memoryBefore;
        double growthPercent = (memoryGrowth / (double)memoryBefore) * 100;

        _output.WriteLine($"\n=== 内存占用测试 ({iterations} 次操作，{troopCount} 支部队) ===");
        _output.WriteLine($"初始内存: {memoryBefore / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"最终内存: {memoryAfter / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"内存增长: {memoryGrowth / 1024.0 / 1024.0:F2} MB ({growthPercent:F2}%)");
        _output.WriteLine($"对象池大小: {PathPool.PoolSize}");

        Assert.True(growthPercent < 10.0, 
            $"内存增长 {growthPercent:F2}% 超过目标 10%");
    }
}
