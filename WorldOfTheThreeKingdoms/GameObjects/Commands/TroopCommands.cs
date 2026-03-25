using System;
using Microsoft.Xna.Framework;

namespace GameObjects.Commands;

// 🔥 基础移动指令（使用 record struct，栈上分配）
// 日期：2026-03-16
// 用途：AI 决策阶段生成，结算阶段执行
public readonly record struct MoveCommand(
    Guid TroopId,           // 部队 ID
    Point TargetPosition,   // 目标位置
    int Priority = 10)      // 优先级（1=最高，10=普通）
{
    // 🔥 优先级常量
    public const int PriorityEnterCity = 1;      // 进城（最高优先级）
    public const int PriorityLeaveCity = 5;      // 出城
    public const int PriorityNormalMove = 10;    // 普通移动
}

// 🔥 入城指令（区分于普通移动）
// 日期：2026-03-23
public readonly record struct EnterCommand(
    Guid TroopId,           // 部队 ID
    int ArchitectureId,     // 目标城池 ID（ID >= 0 有效，ID=0 是洛阳）
    Point TargetPosition)   // 目标位置（城池区域内的某个格子）
{
    public bool IsValid() => TroopId != Guid.Empty && ArchitectureId >= 0;
}

// 🔥 攻击部队指令（区分于攻击城池）
// 日期：2026-03-23
public readonly record struct AttackTroopCommand(
    Guid AttackerId,        // 攻击者 ID
    Guid TargetTroopId,     // 目标部队 ID
    Point OptimalPosition)  // 最佳攻击位置（由战术评分系统计算）
{
    public bool IsValid() => AttackerId != Guid.Empty && TargetTroopId != Guid.Empty;
}

// 🔥 攻击城池指令（区分于攻击部队）
// 日期：2026-03-23
public readonly record struct AttackArchCommand(
    Guid AttackerId,        // 攻击者 ID
    int ArchitectureId,     // 目标城池 ID（ID >= 0 有效，ID=0 是洛阳）
    Point SiegePosition)    // 攻城位置（由 SmartSiege 分配）
{
    public bool IsValid() => AttackerId != Guid.Empty && ArchitectureId >= 0;
}

// 🔥 基础战斗指令
public readonly record struct AttackCommand(
    Guid AttackerId,        // 攻击者 ID
    Guid TargetId,          // 目标 ID
    int Damage)             // 伤害值
{
    // 🔥 验证指令有效性
    public bool IsValid() => AttackerId != Guid.Empty && TargetId != Guid.Empty && Damage > 0;
}

// 🔥 计略指令
public readonly record struct StratagemCommand(
    Guid CasterId,          // 施放者 ID
    Guid TargetId,          // 目标 ID
    int StratagemId)        // 计略 ID
{
    // 🔥 验证指令有效性
    public bool IsValid() => CasterId != Guid.Empty && TargetId != Guid.Empty && StratagemId >= 0;
}

// 🔥 攻城指令
public readonly record struct SiegeCommand(
    Guid AttackerId,        // 攻击者 ID
    int ArchitectureId,     // 城池 ID（使用 int，因为 Architecture 使用 int ID）
    int Damage)             // 伤害值
{
    // 🔥 验证指令有效性（ID >= 0 是有效的，ID=0 是洛阳）
    public bool IsValid() => AttackerId != Guid.Empty && ArchitectureId >= 0 && Damage > 0;
}
