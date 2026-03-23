using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.Tools;

/// <summary>
/// 统一的调试日志管理器
/// 根据日志级别和类别控制输出
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>详细信息（默认不输出）</summary>
        Verbose = 0,
        /// <summary>一般信息（默认不输出）</summary>
        Info = 1,
        /// <summary>警告信息（输出）</summary>
        Warning = 2,
        /// <summary>错误信息（输出）</summary>
        Error = 3,
        /// <summary>严重错误（输出）</summary>
        Critical = 4
    }

    /// <summary>
    /// 日志类别
    /// </summary>
    public enum LogCategory
    {
        /// <summary>通用</summary>
        General,
        /// <summary>序列化</summary>
        Serialization,
        /// <summary>寻路系统</summary>
        Pathfinding,
        /// <summary>军区调配</summary>
        MilitaryArea,
        /// <summary>AI系统</summary>
        AI,
        /// <summary>游戏逻辑</summary>
        GameLogic,
        /// <summary>性能分析</summary>
        Performance,
        /// <summary>数据验证</summary>
        Validation,
        /// <summary>一次性诊断（始终输出）</summary>
        Diagnostic
    }

    // 🔥 配置：最小输出级别（低于此级别的日志不输出）
    private static LogLevel _minLevel = LogLevel.Warning;

    // 🔥 配置：始终输出的类别（即使是Info级别也输出）
    private static readonly HashSet<LogCategory> _alwaysLogCategories = new()
    {
        LogCategory.Pathfinding,      // 寻路相关始终输出
        LogCategory.MilitaryArea,     // 军区调配始终输出
        LogCategory.Diagnostic        // 一次性诊断始终输出
    };

    /// <summary>
    /// 设置最小日志级别
    /// </summary>
    public static void SetMinLevel(LogLevel level)
    {
        _minLevel = level;
    }

    /// <summary>
    /// 记录详细信息（默认不输出）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Verbose(LogCategory category, string message)
    {
        Log(LogLevel.Verbose, category, message);
    }

    /// <summary>
    /// 记录一般信息（默认不输出，除非是特殊类别）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Info(LogCategory category, string message)
    {
        Log(LogLevel.Info, category, message);
    }

    /// <summary>
    /// 记录警告信息（始终输出）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Warning(LogCategory category, string message)
    {
        Log(LogLevel.Warning, category, message);
    }

    /// <summary>
    /// 记录错误信息（始终输出）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Error(LogCategory category, string message)
    {
        Log(LogLevel.Error, category, message);
    }

    /// <summary>
    /// 记录严重错误（始终输出）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Critical(LogCategory category, string message)
    {
        Log(LogLevel.Critical, category, message);
    }

    /// <summary>
    /// 一次性诊断日志（始终输出，用于临时调试特定问题）
    /// </summary>
    [Conditional("DEBUG")]
    public static void Diagnostic(string message)
    {
        Log(LogLevel.Info, LogCategory.Diagnostic, message);
    }

    /// <summary>
    /// 核心日志方法
    /// </summary>
    private static void Log(LogLevel level, LogCategory category, string message)
    {
        // 🔥 规则1：特殊类别始终输出
        if (_alwaysLogCategories.Contains(category))
        {
            WriteLog(level, category, message);
            return;
        }

        // 🔥 规则2：级别过滤
        if (level >= _minLevel)
        {
            WriteLog(level, category, message);
        }
    }

    /// <summary>
    /// 实际写入日志
    /// </summary>
    private static void WriteLog(LogLevel level, LogCategory category, string message)
    {
#if DEBUG
        string levelIcon = level switch
        {
            LogLevel.Verbose => "📝",
            LogLevel.Info => "ℹ️",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            LogLevel.Critical => "🔥",
            _ => "  "
        };

        Debug.WriteLine($"{levelIcon} [{category}] {message}");
#endif
    }

    /// <summary>
    /// 条件日志：仅在条件为true时输出
    /// </summary>
    [Conditional("DEBUG")]
    public static void LogIf(bool condition, LogLevel level, LogCategory category, string message)
    {
        if (condition)
        {
            Log(level, category, message);
        }
    }
}
