using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WorldOfTheThreeKingdoms.Tools
{
    /// <summary>
    /// 异步警告修复方案 - 彻底解决异步方法缺少 await 运算符的警告
    /// </summary>
    public static class AsyncWarningsFix
    {
        /// <summary>
        /// 修复方案1: 正确的 Fire-and-Forget 异步调用模式
        /// 用于替换 Task.Run(() => ...) 不被 await 的情况
        /// </summary>
        public static void RunInBackground(Action action, string operationName = "Background Operation")
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Run(action).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 后台操作异常: {ex.Message}");
                    // 记录异常但不重新抛出，避免未处理异常
                }
            });
        }

        /// <summary>
        /// 修复方案2: 正确的异步 Fire-and-Forget 调用模式
        /// 用于替换异步方法不被 await 的情况
        /// </summary>
        public static void RunInBackgroundAsync(Func<Task> asyncAction, string operationName = "Background Async Operation")
        {
            Task.Run(async () =>
            {
                try
                {
                    await asyncAction().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 后台异步操作异常: {ex.Message}");
                    // 记录异常但不重新抛出，避免未处理异常
                }
            });
        }

        /// <summary>
        /// 修复方案3: 带延迟的后台操作
        /// 用于替换需要延迟执行的 Fire-and-Forget 操作
        /// </summary>
        public static void RunInBackgroundWithDelay(Action action, int delayMs, string operationName = "Delayed Background Operation")
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                    await Task.Run(action).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 延迟后台操作异常: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 修复方案4: 带延迟的异步后台操作
        /// </summary>
        public static void RunInBackgroundWithDelayAsync(Func<Task> asyncAction, int delayMs, string operationName = "Delayed Background Async Operation")
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                    await asyncAction().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 延迟异步后台操作异常: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 修复方案9: 异步初始化模式
        /// 用于替换在构造函数或初始化方法中的异步调用
        /// </summary>
        public static void InitializeAsync(Func<Task> initializeFunc, string operationName = "Async Initialization")
        {
            Task.Run(async () =>
            {
                try
                {
                    await initializeFunc().ConfigureAwait(false);
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 异步初始化完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {operationName} 异步初始化失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 修复方案10: 事件处理器中的异步操作
        /// 用于在事件处理器中安全地执行异步操作
        /// </summary>
        public static void HandleEventAsync(Func<Task> eventHandler, string eventName = "Event Handler")
        {
            Task.Run(async () =>
            {
                try
                {
                    await eventHandler().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncWarningsFix] {eventName} 事件处理异常: {ex.Message}");
                }
            });
        }
    }
}

/// <summary>
/// 异步方法修复扩展方法
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    /// 安全的 Fire-and-Forget 扩展方法
    /// 用法: someTask.SafeFireAndForget("操作名称");
    /// </summary>
    public static void SafeFireAndForget(this Task task, string operationName = "Fire and Forget Operation")
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                Debug.WriteLine($"[AsyncExtensions] {operationName} Fire-and-Forget 操作异常: {t.Exception.GetBaseException().Message}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// 安全的 Fire-and-Forget 扩展方法（泛型版本）
    /// </summary>
    public static void SafeFireAndForget<T>(this Task<T> task, Action<T> onSuccess = null, string operationName = "Fire and Forget Operation")
    {
        task.ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                try
                {
                    onSuccess?.Invoke(t.Result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AsyncExtensions] {operationName} 成功回调异常: {ex.Message}");
                }
            }
            else if (t.IsFaulted && t.Exception != null)
            {
                Debug.WriteLine($"[AsyncExtensions] {operationName} Fire-and-Forget 操作异常: {t.Exception.GetBaseException().Message}");
            }
        });
    }

    /// <summary>
    /// 配置 ConfigureAwait(false) 的便捷扩展
    /// </summary>
    public static ConfiguredTaskAwaitable NoContext(this Task task)
    {
        return task.ConfigureAwait(false);
    }

    /// <summary>
    /// 配置 ConfigureAwait(false) 的便捷扩展（泛型版本）
    /// </summary>
    public static ConfiguredTaskAwaitable<T> NoContext<T>(this Task<T> task)
    {
        return task.ConfigureAwait(false);
    }
}