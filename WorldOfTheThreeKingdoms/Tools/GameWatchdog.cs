using System;
using System.Diagnostics;
using System.Threading;

namespace WorldOfTheThreeKingdoms.Tools;

/// <summary>
/// 游戏看门狗 - 监控主线程心跳，检测死锁/无限循环/未响应
/// </summary>
public sealed class GameWatchdog : IDisposable
{
    private readonly Thread _watchdogThread;
    private readonly int _timeoutSeconds;
    private long _lastHeartbeatTicks;  // 使用 Interlocked 操作，不需要 volatile
    private volatile bool _isRunning;
    private volatile bool _isDisposed;

    /// <summary>
    /// 创建看门狗实例
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒），默认 10 秒</param>
    public GameWatchdog(int timeoutSeconds = 10)
    {
        _timeoutSeconds = timeoutSeconds;
        Interlocked.Exchange(ref _lastHeartbeatTicks, Stopwatch.GetTimestamp());
        _isRunning = true;

        // 创建后台线程
        _watchdogThread = new Thread(WatchdogLoop)
        {
            Name = "GameWatchdog",
            IsBackground = true, // 后台线程，不会阻止进程退出
            Priority = ThreadPriority.AboveNormal // 高优先级，确保能及时检测
        };

        _watchdogThread.Start();

        DebugLogger.Info(DebugLogger.LogCategory.General, 
            $"[GameWatchdog] 已启动，超时阈值: {timeoutSeconds} 秒");
    }

    /// <summary>
    /// 更新心跳（必须在主线程的 Update 方法中每帧调用）
    /// </summary>
    public void UpdateHeartbeat()
    {
        // 🔥 使用 Interlocked.Exchange 确保线程安全
        // 性能：原子操作，< 0.01ms
        Interlocked.Exchange(ref _lastHeartbeatTicks, Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// 看门狗循环（在独立线程中运行）
    /// </summary>
    private void WatchdogLoop()
    {
        try
        {
            while (_isRunning && !_isDisposed)
            {
                // 每秒检查一次
                Thread.Sleep(1000);

                if (_isDisposed) break;

                // 计算距离上次心跳的时间
                long currentTicks = Stopwatch.GetTimestamp();
                long lastTicks = Interlocked.Read(ref _lastHeartbeatTicks);  // 线程安全读取
                long elapsedTicks = currentTicks - lastTicks;
                double elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;

                // 🔥 超时检测
                if (elapsedSeconds > _timeoutSeconds)
                {
                    // 主线程已经超过阈值时间没有响应
                    string lastCheckpoint = PerformanceCheckpoint.GetLastCheckpoint();
                    string message = $"[GameWatchdog] 主线程未响应超过 {elapsedSeconds:F1} 秒（阈值: {_timeoutSeconds} 秒）\n{lastCheckpoint}";
                    
                    Console.WriteLine("==================== 致命错误 ====================");
                    Console.WriteLine(message);
                    Console.WriteLine("检测到主线程死锁/无限循环/未响应");
                    Console.WriteLine("正在生成崩溃日志...");
                    Console.WriteLine("=================================================");

                    // 🔥 生成崩溃日志（包含最后检查点信息）
                    CrashReporter.ReportCrashAndTerminate(null, $"主线程超时 ({elapsedSeconds:F1}秒)\n{lastCheckpoint}");

                    // ReportCrashAndTerminate 会调用 Environment.FailFast，不会返回
                    // 但为了代码完整性，这里也添加一个后备终止
                    Environment.Exit(-1);
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            // 正常中断，不需要处理
            DebugLogger.Info(DebugLogger.LogCategory.General, "[GameWatchdog] 线程被中断，正常退出");
        }
        catch (Exception ex)
        {
            // 看门狗自身出错（不应该发生）
            Console.WriteLine($"[GameWatchdog] 看门狗线程异常: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 停止看门狗
    /// </summary>
    public void Stop()
    {
        if (_isDisposed) return;

        _isRunning = false;

        // 等待线程退出（最多 2 秒）
        if (_watchdogThread.IsAlive)
        {
            _watchdogThread.Join(2000);
        }

        DebugLogger.Info(DebugLogger.LogCategory.General, "[GameWatchdog] 已停止");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        Stop();
    }
}
