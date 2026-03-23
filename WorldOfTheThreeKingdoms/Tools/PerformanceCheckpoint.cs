using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WorldOfTheThreeKingdoms.Tools;

/// <summary>
/// 性能检查点 - Release 模式下也能工作的轻量级诊断工具
/// 用于定位死循环和长时间卡顿的位置
/// </summary>
public static class PerformanceCheckpoint
{
    private static string _lastCheckpoint = "程序启动";
    private static long _lastCheckpointTicks;
    
    /// <summary>
    /// 记录检查点（Release 模式下也会执行）
    /// 性能：< 0.01ms，使用 Stopwatch.GetTimestamp() 而非 DateTime.Now
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mark(string checkpointName)
    {
        _lastCheckpoint = checkpointName;
        _lastCheckpointTicks = Stopwatch.GetTimestamp();
    }
    
    /// <summary>
    /// 获取最后一个检查点信息（用于崩溃报告）
    /// </summary>
    public static string GetLastCheckpoint()
    {
        long currentTicks = Stopwatch.GetTimestamp();
        long elapsedTicks = currentTicks - _lastCheckpointTicks;
        double elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;
        
        return $"最后检查点: {_lastCheckpoint}, 已过时间: {elapsedSeconds:F1}秒";
    }
    
    /// <summary>
    /// 重置检查点（用于新的操作序列）
    /// </summary>
    public static void Reset()
    {
        _lastCheckpoint = "已重置";
        _lastCheckpointTicks = Stopwatch.GetTimestamp();
    }
}
