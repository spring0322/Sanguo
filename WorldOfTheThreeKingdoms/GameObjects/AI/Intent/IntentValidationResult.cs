using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public readonly record struct IntentValidationResult(
    bool IsValid,
    TroopIntentFailureReason FailureReason,
    int RelatedObjectId,
    Point FailedAt,
    IntentCheckpointKind FailedAtCheckpoint,
    FallbackPolicy SuggestedFallback,
    bool ShouldReplanNow)
{
    public static IntentValidationResult Valid(IntentCheckpointKind checkpoint, Point at)
    {
        return new IntentValidationResult(
            true,
            TroopIntentFailureReason.ExecutorConflict,
            -1,
            at,
            checkpoint,
            FallbackPolicy.Hold,
            false);
    }

    public static IntentValidationResult Invalid(
        TroopIntentFailureReason reason,
        int relatedObjectId,
        Point at,
        IntentCheckpointKind checkpoint,
        FallbackPolicy suggestedFallback,
        bool shouldReplanNow)
    {
        return new IntentValidationResult(
            false,
            reason,
            relatedObjectId,
            at,
            checkpoint,
            suggestedFallback,
            shouldReplanNow);
    }
}
