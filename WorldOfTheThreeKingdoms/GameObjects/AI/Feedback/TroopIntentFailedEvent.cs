using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public readonly record struct TroopIntentFailedEvent(
    TroopIntentId IntentId,
    int TroopId,
    TroopIntentFailureReason Reason,
    int Tick,
    Point FailedAt,
    int RelatedObjectId
);
