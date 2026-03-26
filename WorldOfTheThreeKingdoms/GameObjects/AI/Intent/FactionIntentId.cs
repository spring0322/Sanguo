namespace GameObjects.AI;

public readonly record struct FactionIntentId(int FactionId, int Version, int IssuedTick)
{
    public static readonly FactionIntentId Empty = new(-1, 0, 0);

    public bool IsValid => FactionId >= 0 && Version > 0;

    public override string ToString()
    {
        return $"F{FactionId}:V{Version}@{IssuedTick}";
    }
}
