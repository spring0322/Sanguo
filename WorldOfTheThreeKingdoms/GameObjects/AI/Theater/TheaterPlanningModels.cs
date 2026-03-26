using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public enum TheaterFocusMode : byte
{
    Hold = 0,
    Assault = 1,
    Defend = 2,
    Recover = 3
}

public readonly record struct TheaterTargetScore(
    int ArchitectureId,
    Point Position,
    int Score,
    int TravelCost,
    int NetEnergy,
    int VisibilityConfidence,
    int SupplyRisk,
    TheaterFocusMode FocusMode)
{
    public static readonly TheaterTargetScore None = new(
        -1,
        new Point(-1, -1),
        int.MinValue,
        int.MaxValue,
        0,
        0,
        0,
        TheaterFocusMode.Hold);

    public bool IsValid => ArchitectureId >= 0;
}

public readonly record struct TheaterStrategicSnapshot(
    int Tick,
    int FactionId,
    TheaterTargetScore PrimaryAssault,
    TheaterTargetScore PrimaryDefense,
    int AverageVisibilityConfidence,
    int AverageNetEnergy)
{
    public static TheaterStrategicSnapshot CreateHold(int tick, int factionId, Point fallbackPosition)
    {
        TheaterTargetScore hold = new(
            -1,
            fallbackPosition,
            0,
            0,
            0,
            0,
            0,
            TheaterFocusMode.Hold);

        return new TheaterStrategicSnapshot(
            tick,
            factionId,
            hold,
            hold,
            0,
            0);
    }
}
