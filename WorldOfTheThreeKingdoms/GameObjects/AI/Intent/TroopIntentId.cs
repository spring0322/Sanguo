using System;

namespace GameObjects.AI;

public readonly record struct TroopIntentId(int TroopId, int Version, int IssuedTick)
{
    public static readonly TroopIntentId Empty = new(-1, 0, 0);

    public bool IsValid => TroopId >= 0 && Version > 0;

    public override string ToString()
    {
        return $"T{TroopId}:V{Version}@{IssuedTick}";
    }
}
