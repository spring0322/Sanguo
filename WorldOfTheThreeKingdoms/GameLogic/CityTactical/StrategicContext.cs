namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战略决策上下文 (栈内存传递，C# 12 主构造函数)
/// 日期：2026-03-11
/// </summary>
public readonly struct StrategicContext(
    int gold,
    int food,
    int currentTroops,
    int maxTroops,
    int averageMorale,
    float threatLevel)
{
    public readonly int Gold = gold;
    public readonly int Food = food;
    public readonly int CurrentTroops = currentTroops;
    public readonly int MaxTroops = maxTroops;
    public readonly int AverageMorale = averageMorale;
    
    /// <summary>外部环境压力 (0.0 ~ 1.0)，比如被重兵围城时接近 1.0</summary>
    public readonly float ThreatLevel = threatLevel;

    /// <summary>极速获取兵力饱和度 (0.0 ~ 1.0)</summary>
    public float TroopSaturation => MaxTroops > 0 ? (float)CurrentTroops / MaxTroops : 1.0f;
}
