using System;
using System.Collections.Frozen; // .NET 8 新特性
using System.Collections.Generic;
using System.Linq;

namespace WorldOfTheThreeKingdoms.GameGlobal;

public enum ArchitectureWorkKind
{
    无,
    农业,
    商业,
    技术,
    统治,
    民心,
    耐久,
    训练,
    补充,
    赈灾
}

/// <summary>
/// 针对 .NET 8 优化的扩展类，提供高性能的查找和转换
/// </summary>
public static class ArchitectureWorkKindExtensions
{
    // 使用 FrozenDictionary 替代 Dictionary
    // FrozenDictionary 在创建时会花费稍多时间进行优化，但之后的读取速度极快，
    // 且线程安全，非常适合游戏运行时的静态配置表读取。
    private static readonly FrozenDictionary<string, ArchitectureWorkKind> _nameToEnumMap = 
        Enum.GetValues<ArchitectureWorkKind>()
            .ToFrozenDictionary(k => k.ToString());

    /// <summary>
    /// 高性能的字符串转枚举方法 (替代 Enum.Parse)
    /// </summary>
    public static ArchitectureWorkKind FromStringFast(string name)
    {
        // TryGetValue 在 FrozenDictionary 中经过了高度优化
        return _nameToEnumMap.TryGetValue(name, out var kind) ? kind : ArchitectureWorkKind.无;
    }

    /// <summary>
    /// 检查是否为有效的内政工作 (非“无”)
    /// </summary>
    public static bool IsValidWork(this ArchitectureWorkKind kind)
    {
        return kind != ArchitectureWorkKind.无;
    }
}