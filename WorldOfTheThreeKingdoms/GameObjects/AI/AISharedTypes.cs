using System;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameObjects.AI;

/// <summary>
/// 基础坐标点（零分配，AOT友好）
/// 提供与 MonoGame Point 的隐式转换
/// </summary>
public readonly record struct Point2D(int X, int Y)
{
    public int ManhattanDistance(Point2D other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    
    public static implicit operator Point2D(Point p) => new(p.X, p.Y);
    public static implicit operator Point(Point2D p) => new(p.X, p.Y);
}

/// <summary>
/// AI行动类型枚举
/// </summary>
public enum ActionType
{
    Wait,           // 等待
    NormalAttack,   // 普通攻击
    Tactic,         // 战法
    Stratagem,      // 计略
    Move            // 移动
}

/// <summary>
/// 综合行动提案（零分配，AOT友好）
/// </summary>
public readonly record struct ActionProposal(
    Point2D MovePosition, 
    Troop Target, 
    ActionType Type, 
    int SkillId, 
    float UtilityScore
);

/// <summary>
/// 部队能力倾向向量（零分配，AOT友好）
/// 分数越高，说明该部队执行该类任务的潜力越大
/// </summary>
public readonly record struct RoleProfile(
    float TankAptitude, 
    float DpsAptitude, 
    float MageAptitude, 
    float SupportAptitude
)
{
    /// <summary>
    /// 找出当前最具优势的倾向，用于快速剪枝
    /// </summary>
    public float MaxAptitude => Math.Max(
        Math.Max(TankAptitude, DpsAptitude), 
        Math.Max(MageAptitude, SupportAptitude)
    );

    /// <summary>
    /// 判断是否为脆皮（法师或辅助倾向高）
    /// </summary>
    public bool IsFragile => MageAptitude > 60 || SupportAptitude > 60;

    /// <summary>
    /// 判断是否为肉盾倾向
    /// </summary>
    public bool IsTankOriented => TankAptitude > 70;
}
