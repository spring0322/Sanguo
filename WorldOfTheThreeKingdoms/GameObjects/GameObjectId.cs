using System;
using System.Diagnostics.CodeAnalysis;

namespace GameObjects;

/// <summary>
/// 游戏对象 ID 的强类型包装器
/// 🔥 设计目标：在编译时防止 ID=0 的歧义问题
/// 
/// 核心原则：
/// - ID >= 0 是有效的（包括洛阳=0、步兵=0、阿会喃=0）
/// - ID = -1 表示"无引用"
/// - 禁止使用 > 0 判断（编译器会强制使用 IsValid 属性）
/// </summary>
public readonly struct GameObjectId : IEquatable<GameObjectId>, IComparable<GameObjectId>
{
    /// <summary>
    /// 表示"无引用"的特殊值
    /// </summary>
    public static readonly GameObjectId None = new(-1);
    
    /// <summary>
    /// 内部存储的 ID 值
    /// </summary>
    public int Value { get; }
    
    /// <summary>
    /// 是否是有效的引用（>= 0）
    /// 🔥 关键：ID=0 是有效的！
    /// </summary>
    public bool IsValid => Value >= 0;
    
    /// <summary>
    /// 是否是"无引用"（-1）
    /// </summary>
    public bool IsNone => Value == -1;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public GameObjectId(int value)
    {
        Value = value;
    }
    
    /// <summary>
    /// 从 int 隐式转换
    /// </summary>
    public static implicit operator GameObjectId(int value) => new(value);
    
    /// <summary>
    /// 到 int 显式转换（强制开发者意识到这是 ID）
    /// </summary>
    public static explicit operator int(GameObjectId id) => id.Value;
    
    /// <summary>
    /// 相等比较
    /// </summary>
    public bool Equals(GameObjectId other) => Value == other.Value;
    
    public override bool Equals([NotNullWhen(true)] object obj) 
        => obj is GameObjectId other && Equals(other);
    
    public override int GetHashCode() => Value.GetHashCode();
    
    public int CompareTo(GameObjectId other) => Value.CompareTo(other.Value);
    
    /// <summary>
    /// 运算符重载
    /// </summary>
    public static bool operator ==(GameObjectId left, GameObjectId right) => left.Equals(right);
    public static bool operator !=(GameObjectId left, GameObjectId right) => !left.Equals(right);
    public static bool operator <(GameObjectId left, GameObjectId right) => left.Value < right.Value;
    public static bool operator >(GameObjectId left, GameObjectId right) => left.Value > right.Value;
    public static bool operator <=(GameObjectId left, GameObjectId right) => left.Value <= right.Value;
    public static bool operator >=(GameObjectId left, GameObjectId right) => left.Value >= right.Value;
    
    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => IsNone ? "None" : Value.ToString();
}

/// <summary>
/// 专用的 Architecture ID 类型
/// </summary>
public readonly struct ArchitectureId : IEquatable<ArchitectureId>
{
    public static readonly ArchitectureId None = new(-1);
    
    public int Value { get; }
    public bool IsValid => Value >= 0;
    public bool IsNone => Value == -1;
    
    public ArchitectureId(int value) => Value = value;
    
    public static implicit operator ArchitectureId(int value) => new(value);
    public static explicit operator int(ArchitectureId id) => id.Value;
    public static implicit operator GameObjectId(ArchitectureId id) => new(id.Value);
    
    public bool Equals(ArchitectureId other) => Value == other.Value;
    public override bool Equals([NotNullWhen(true)] object obj) 
        => obj is ArchitectureId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    
    public static bool operator ==(ArchitectureId left, ArchitectureId right) => left.Equals(right);
    public static bool operator !=(ArchitectureId left, ArchitectureId right) => !left.Equals(right);
    
    public override string ToString() => IsNone ? "None" : Value.ToString();
}

/// <summary>
/// 专用的 Person ID 类型
/// </summary>
public readonly struct PersonId : IEquatable<PersonId>
{
    public static readonly PersonId None = new(-1);
    
    public int Value { get; }
    public bool IsValid => Value >= 0;
    public bool IsNone => Value == -1;
    
    public PersonId(int value) => Value = value;
    
    public static implicit operator PersonId(int value) => new(value);
    public static explicit operator int(PersonId id) => id.Value;
    public static implicit operator GameObjectId(PersonId id) => new(id.Value);
    
    public bool Equals(PersonId other) => Value == other.Value;
    public override bool Equals([NotNullWhen(true)] object obj) 
        => obj is PersonId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    
    public static bool operator ==(PersonId left, PersonId right) => left.Equals(right);
    public static bool operator !=(PersonId left, PersonId right) => !left.Equals(right);
    
    public override string ToString() => IsNone ? "None" : Value.ToString();
}

/// <summary>
/// 专用的 Military ID 类型
/// </summary>
public readonly struct MilitaryId : IEquatable<MilitaryId>
{
    public static readonly MilitaryId None = new(-1);
    
    public int Value { get; }
    public bool IsValid => Value >= 0;
    public bool IsNone => Value == -1;
    
    public MilitaryId(int value) => Value = value;
    
    public static implicit operator MilitaryId(int value) => new(value);
    public static explicit operator int(MilitaryId id) => id.Value;
    public static implicit operator GameObjectId(MilitaryId id) => new(id.Value);
    
    public bool Equals(MilitaryId other) => Value == other.Value;
    public override bool Equals([NotNullWhen(true)] object obj) 
        => obj is MilitaryId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    
    public static bool operator ==(MilitaryId left, MilitaryId right) => left.Equals(right);
    public static bool operator !=(MilitaryId left, MilitaryId right) => !left.Equals(right);
    
    public override string ToString() => IsNone ? "None" : Value.ToString();
}
