using System.Diagnostics;
using GameObjects;
using GameObjects.AI.Pathfinding;
using Microsoft.Xna.Framework;
using Xunit;
using Xunit.Abstractions;

namespace WorldOfTheThreeKingdoms.Tests.AsyncPathfinding;

/// <summary>
/// 压力测试
/// 验证需求: 6.6, 11.5
/// </summary>
public class StressTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestScenarioBuilder _builder;

    public StressTests(ITestOutputHelper output)
    {
        _output = output;
        _builder = new TestScenarioBuilder();
    }

    public void Dispose()
    {
        _builder.Dispose();
    }

    /// <summary>
    /// 长时间运行测试（简化版 - 5分钟而非24小时）
    /// 需求 11.5: 验证性能不退化
    /// </summary>
    [Fact(Skip = "长时间运行测试，手动执行")]
    public async Task Test_LongRunning_5Minutes_NoPerformanceDegradation()
    {
        // Arrange
        const int durationMinutes = 5;
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

        _output.WriteLine($"开始 {durationMinutes} 分钟压力测试...");

        // Act
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(durationMinutes);
        
        var frameTimesFirstMinute = new List<double>();
        var frameTimesLastMinute = new List<double>();
        var stopwatch = Stopwatch.StartNew();
        
        long totalRequests = 0;
        long totalFrames = 0;
        long gen0Start = GC.CollectionCount(0);
        long gen1Start = GC.CollectionCount(1);
        long gen2Start = GC.CollectionCount(2);

        while (DateTime.UtcNow < endTime)
        {
            var frameStart = Stopwatch.GetTimestamp();
            
            // 随机发起寻路请求
            if (Random.Shared.Next(100) < 20) // 20% 概率
            {
                var troop = troops[Random.Shared.Next(troops.Count)];
                var target = new Point(
                    Random.Shared.Next(50, 150),
                    Random.Shared.Next(50, 150)
                );
                troop.RequestMoveAsync(target);
                totalRequests++;
            }

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

            var frameEnd = Stopwatch.GetTimestamp();
            var frameTime = (frameEnd - frameStart) * 1000.0 / Stopwatch.Frequency;

            // 记录第一分钟和最后一分钟的帧时间
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMinutes < 1)
            {
                frameTimesFirstMinute.Add(frameTime);
            }
            else if (elapsed.TotalMinutes >= durationMinutes - 1)
            {
                frameTimesLastMinute.Add(frameTime);
            }

            totalFrames++;

            // 每分钟输出一次进度
            if (totalFrames % 3600 == 0) // 60fps * 60s = 3600 frames
            {
                var minutesElapsed = (DateTime.UtcNow - startTime).TotalMinutes;
                _output.WriteLine($"已运行 {minutesElapsed:F1} 分钟, 总请求: {totalRequests}, 总帧数: {totalFrames}");
            }

            await Task.Delay(16); // 模拟 60fps
        }

        long gen0End = GC.CollectionCount(0);
        long gen1End = GC.CollectionCount(1);
        long gen2End = GC.CollectionCount(2);

        // Assert
        var avgFirstMinute = frameTimesFirstMinute.Average();
        var avgLastMinute = frameTimesLastMinute.Average();
        var degradation = ((avgLastMinute - avgFirstMinute) / avgFirstMinute) * 100;

        _output.WriteLine($"\n=== 长时间运行测试结果 ===");
        _output.WriteLine($"运行时长: {durationMinutes} 分钟");
        _output.WriteLine($"总请求数: {totalRequests}");
        _output.WriteLine($"总帧数: {totalFrames}");
        _output.WriteLine($"第一分钟平均帧时间: {avgFirstMinute:F3}ms");
        _output.WriteLine($"最后一分钟平均帧时间: {avgLastMinute:F3}ms");
        _output.WriteLine($"性能退化: {degradation:F2}%");
        _output.WriteLine($"GC 统计: Gen0={gen0End - gen0Start}, Gen1={gen1End - gen1Start}, Gen2={gen2End - gen2Start}");
        _output.WriteLine($"对象池大小: {PathPool.PoolSize}");

        // 验证性能退化 < 5%
        Assert.True(Math.Abs(degradation) < 5.0, 
            $"性能退化 {degradation:F2}% 超过容忍值 5%");
    }

    /// <summary>
    /// 频繁创建/销毁部队测试
    /// 需求 6.6: 验证无内存泄漏
    /// </summary>
    [Fact]
    public async Task Test_FrequentTroopCreationDestruction_NoMemoryLeak()
    {
        // Arrange
        const int iterations = 500;
        var session = _builder.CreateSession();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryStart = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            // 创建部队
            var troop = _builder.CreateTroop(
                session.Scenario,
                new Point(10, 10),
                $"临时部队{i}"
            );
            session.RegisterTroop(troop);

            // 发起寻路
            troop.RequestMoveAsync(new Point(100, 100));

            // 等待一小段时间
            await Task.Delay(5);

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

            // 销毁部队
            session.UnregisterTroop(troop);
            troop.Destroy(true, true, true);

            // 每 100 次迭代输出进度
            if ((i + 1) % 100 == 0)
            {
                _output.WriteLine($"已完成 {i + 1}/{iterations} 次创建/销毁循环");
            }
        }

        // 等待所有异步操作完成
        await Task.Delay(1000);

        // 强制 GC 并测量内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryEnd = GC.GetTotalMemory(true);

        // Assert
        long memoryGrowth = memoryEnd - memoryStart;
        double growthMB = memoryGrowth / 1024.0 / 1024.0;

        _output.WriteLine($"\n=== 频繁创建/销毁测试结果 ===");
        _output.WriteLine($"迭代次数: {iterations}");
        _output.WriteLine($"初始内存: {memoryStart / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"最终内存: {memoryEnd / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"内存增长: {growthMB:F2} MB");
        _output.WriteLine($"对象池大小: {PathPool.PoolSize}");

        // 验证内存增长 < 5MB（考虑对象池缓存）
        Assert.True(growthMB < 5.0, 
            $"内存增长 {growthMB:F2} MB 超过容忍值 5 MB，可能存在内存泄漏");
    }

    /// <summary>
    /// 高频取消操作测试
    /// 需求 4.3: 验证取消响应性
    /// </summary>
    [Fact]
    public async Task Test_HighFrequencyCancellation_NoResourceLeak()
    {
        // Arrange
        const int iterations = 1000;
        var session = _builder.CreateSession();
        var troop = _builder.CreateTroop(
            session.Scenario,
            new Point(10, 10),
            "测试部队"
        );
        session.RegisterTroop(troop);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryStart = GC.GetTotalMemory(true);

        // Act - 快速发起和取消寻路
        for (int i = 0; i < iterations; i++)
        {
            // 发起寻路
            troop.RequestMoveAsync(new Point(
                Random.Shared.Next(50, 150),
                Random.Shared.Next(50, 150)
            ));

            // 立即取消（模拟玩家快速改变主意）
            await Task.Delay(1);
            troop.CancelCurrentPathfinding();

            // 处理可能的结果
            if (i % 10 == 0)
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
            }

            if ((i + 1) % 200 == 0)
            {
                _output.WriteLine($"已完成 {i + 1}/{iterations} 次取消操作");
            }
        }

        // 等待清理
        await Task.Delay(1000);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryEnd = GC.GetTotalMemory(true);

        // Assert
        long memoryGrowth = memoryEnd - memoryStart;
        double growthMB = memoryGrowth / 1024.0 / 1024.0;

        _output.WriteLine($"\n=== 高频取消测试结果 ===");
        _output.WriteLine($"取消次数: {iterations}");
        _output.WriteLine($"初始内存: {memoryStart / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"最终内存: {memoryEnd / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"内存增长: {growthMB:F2} MB");
        _output.WriteLine($"对象池大小: {PathPool.PoolSize}");

        Assert.True(growthMB < 2.0, 
            $"内存增长 {growthMB:F2} MB 超过容忍值 2 MB");
    }

    /// <summary>
    /// 混合压力测试：同时进行创建、销毁、寻路、取消
    /// </summary>
    [Fact]
    public async Task Test_MixedStress_AllOperationsCombined()
    {
        // Arrange
        const int durationSeconds = 30;
        const int maxTroops = 100;
        
        var session = _builder.CreateSession();
        var activeTroops = new List<Troop>();

        _output.WriteLine($"开始 {durationSeconds} 秒混合压力测试...");

        // Act
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationSeconds);
        
        long operations = 0;
        long gen0Start = GC.CollectionCount(0);

        while (DateTime.UtcNow < endTime)
        {
            var action = Random.Shared.Next(100);

            if (action < 30 && activeTroops.Count < maxTroops)
            {
                // 30% 概率：创建新部队
                var troop = _builder.CreateTroop(
                    session.Scenario,
                    new Point(Random.Shared.Next(10, 50), Random.Shared.Next(10, 50)),
                    $"部队{operations}"
                );
                session.RegisterTroop(troop);
                activeTroops.Add(troop);
                operations++;
            }
            else if (action < 50 && activeTroops.Count > 0)
            {
                // 20% 概率：销毁部队
                var index = Random.Shared.Next(activeTroops.Count);
                var troop = activeTroops[index];
                session.UnregisterTroop(troop);
                troop.Destroy(true, true, true);
                activeTroops.RemoveAt(index);
                operations++;
            }
            else if (action < 80 && activeTroops.Count > 0)
            {
                // 30% 概率：发起寻路
                var troop = activeTroops[Random.Shared.Next(activeTroops.Count)];
                troop.RequestMoveAsync(new Point(
                    Random.Shared.Next(50, 150),
                    Random.Shared.Next(50, 150)
                ));
                operations++;
            }
            else if (activeTroops.Count > 0)
            {
                // 20% 概率：取消寻路
                var troop = activeTroops[Random.Shared.Next(activeTroops.Count)];
                troop.CancelCurrentPathfinding();
                operations++;
            }

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

            await Task.Delay(10);
        }

        long gen0End = GC.CollectionCount(0);

        // 清理剩余部队
        foreach (var troop in activeTroops.ToList())
        {
            session.UnregisterTroop(troop);
            troop.Destroy(true, true, true);
        }

        // Assert
        _output.WriteLine($"\n=== 混合压力测试结果 ===");
        _output.WriteLine($"运行时长: {durationSeconds} 秒");
        _output.WriteLine($"总操作数: {operations}");
        _output.WriteLine($"Gen0 GC: {gen0End - gen0Start} 次");
        _output.WriteLine($"对象池大小: {PathPool.PoolSize}");

        // 验证系统仍然正常运行
        Assert.True(operations > 0, "应该执行了一些操作");
        Assert.True(PathPool.PoolSize < 200, "对象池大小应该保持合理");
    }
}
