using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 主线程调度器，用于在主线程执行操作
/// 处理 MonoGame 的线程限制（如纹理创建必须在主线程）
/// </summary>
public class MainThreadDispatcher
{
    private readonly ConcurrentQueue<Action> _actionQueue = new();
    
    /// <summary>
    /// 在主线程执行操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="action">要执行的操作</param>
    /// <returns>操作结果的异步任务</returns>
    public Task<T> InvokeAsync<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>();
        
        _actionQueue.Enqueue(() =>
        {
            try
            {
                var result = action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    /// <summary>
    /// 在主线程执行操作（无返回值）
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>完成的异步任务</returns>
    public Task InvokeAsync(Action action)
    {
        return InvokeAsync(() =>
        {
            action();
            return 0; // Dummy return
        });
    }
    
    /// <summary>
    /// 在 Update 循环中调用，执行队列中的操作
    /// 限制每帧处理数量，避免卡顿
    /// </summary>
    public void ProcessQueue()
    {
        // 限制每帧处理的操作数量，避免卡顿
        const int maxActionsPerFrame = 10;
        int processed = 0;
        
        while (processed < maxActionsPerFrame && _actionQueue.TryDequeue(out var action))
        {
            action();
            processed++;
        }
    }
    
    /// <summary>
    /// 获取待处理操作数量
    /// </summary>
    public int PendingCount => _actionQueue.Count;
}
