using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace WorldOfTheThreeKingdoms.GameGlobal; // 使用 C# 10+ 文件范围命名空间，减少缩进层级

public enum TerrainKind
{
    无,
    平原,
    草原,
    森林,
    湿地,
    山地,
    水域,
    峻岭,
    荒地,
    沙漠,
    栈道
}

/// <summary>
/// 针对 .NET 8 优化的地形数据辅助类
/// 利用 FrozenDictionary 提升高频查找性能
/// </summary>
public static class TerrainMeta
{
    // FrozenDictionary 针对读取操作进行了极致优化，初始化后不可修改
    // 适合替代传统的 static readonly Dictionary
    private static readonly FrozenDictionary<string, TerrainKind> _nameToEnum;
    private static readonly FrozenDictionary<int, string> _idToName;

    static TerrainMeta()
    {
        // 预热数据：将所有枚举值映射并冻结
        var values = Enum.GetValues<TerrainKind>();

        _nameToEnum = values.ToFrozenDictionary(k => k.ToString(), k => k);
        
        // 如果需要由 int 安全查找名称（避免反射开销）
        _idToName = values.ToFrozenDictionary(k => (int)k, k => k.ToString());
    }

    /// <summary>
    /// 高速且零内存分配（无反射）的字符串转枚举
    /// 比 Enum.Parse 快得多，适合在读取大量地图数据时使用
    /// </summary>
    public static TerrainKind Parse(string name)
    {
        return _nameToEnum.TryGetValue(name, out var kind) ? kind : TerrainKind.无;
    }

    /// <summary>
    /// 检查名称是否有效
    /// </summary>
    public static bool Contains(string name) => _nameToEnum.ContainsKey(name);
}