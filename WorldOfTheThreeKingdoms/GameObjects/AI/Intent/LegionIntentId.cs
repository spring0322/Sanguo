namespace GameObjects.AI;

public readonly record struct LegionIntentId(int LegionId, int Version, int IssuedTick)
{
    public static readonly LegionIntentId Empty = new(-1, 0, 0);

    public bool IsValid => LegionId >= 0 && Version > 0;

    public override string ToString()
    {
        return $"L{LegionId}:V{Version}@{IssuedTick}";
    }
}
