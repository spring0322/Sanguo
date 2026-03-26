using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public readonly record struct IntentTargetRef(
    IntentTargetKind Kind,
    int TargetId,
    Point Position,
    int OwnerFactionId)
{
    public static readonly IntentTargetRef None = new(IntentTargetKind.None, -1, new Point(-1, -1), -1);

    public static IntentTargetRef ForPosition(Point position)
    {
        return new(IntentTargetKind.Position, -1, position, -1);
    }

    public static IntentTargetRef ForTroop(int troopId, Point position, int ownerFactionId)
    {
        return new(IntentTargetKind.Troop, troopId, position, ownerFactionId);
    }

    public static IntentTargetRef ForArchitecture(int architectureId, Point position, int ownerFactionId)
    {
        return new(IntentTargetKind.Architecture, architectureId, position, ownerFactionId);
    }
}
