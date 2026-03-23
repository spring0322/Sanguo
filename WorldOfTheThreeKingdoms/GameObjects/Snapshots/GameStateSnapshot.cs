using System;
using Microsoft.Xna.Framework;

namespace GameObjects.Snapshots;

// 🔥 游戏状态快照（用于 AI 决策，使用 C# 12 主构造函数）
// 日期：2026-03-16
// 设计原则：
// 1. 只包含 AI 决策所需的最小数据集
// 2. 使用数组而非 List，避免分配
// 3. 使用值类型，避免引用共享
public readonly struct GameStateSnapshot(
    TroopSnapshot[] myTroops,
    TroopSnapshot[] enemyTroops,
    ArchitectureSnapshot[] myArchitectures,
    ArchitectureSnapshot[] enemyArchitectures,
    int[,] terrainCosts)
{
    public TroopSnapshot[] MyTroops { get; } = myTroops;
    public TroopSnapshot[] EnemyTroops { get; } = enemyTroops;
    public ArchitectureSnapshot[] MyArchitectures { get; } = myArchitectures;
    public ArchitectureSnapshot[] EnemyArchitectures { get; } = enemyArchitectures;
    public int[,] TerrainCosts { get; } = terrainCosts;
}

// 🔥 部队快照（值类型，使用 C# 12 主构造函数）
public readonly struct TroopSnapshot(Troop troop)
{
    // 🔥 关键：使用 Guid 类型的 Id（不是 int 类型的 ID）
    public Guid TroopID { get; } = troop.Id;
    public Point Position { get; } = troop.Position;
    public int Quantity { get; } = troop.Quantity;
    public int Morale { get; } = troop.Morale;
    public TroopCommand Command { get; } = troop.Command;
    
    // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳势力），-1 表示中立
    // 日期：2026-03-16
    // 原因：游戏中存在中立单位（BelongedFaction 为 null）
    public int FactionId { get; } = troop.BelongedFaction?.ID ?? -1;
    
    // 🔥 辅助方法：判断是否是敌人
    // 关键：FactionId >= 0 确保势力有效（包括 ID=0 的洛阳）
    public bool IsEnemy(int myFactionId) => FactionId >= 0 && FactionId != myFactionId;
}

// 🔥 城池快照（值类型，使用 C# 12 主构造函数）
public readonly struct ArchitectureSnapshot(Architecture arch)
{
    public int ID { get; } = arch.ID;
    public Point Position { get; } = arch.Position;
    public int Fund { get; } = arch.Fund;
    public int Food { get; } = arch.Food;
    public int Endurance { get; } = arch.Endurance;
    
    // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳势力），-1 表示中立
    // 日期：2026-03-16
    // 原因：游戏中存在中立城池（BelongedFaction 为 null）
    public int FactionId { get; } = arch.BelongedFaction?.ID ?? -1;
    
    // 🔥 辅助方法：判断是否是敌人
    // 关键：FactionId >= 0 确保势力有效（包括 ID=0 的洛阳）
    public bool IsEnemy(int myFactionId) => FactionId >= 0 && FactionId != myFactionId;
}

// 🔥 地图快照（用于异步寻路）
public readonly struct MapSnapshot(
    int width,
    int height,
    int[,] terrainCosts,
    bool[,] obstacles)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int[,] TerrainCosts { get; } = terrainCosts;
    public bool[,] Obstacles { get; } = obstacles;
}
