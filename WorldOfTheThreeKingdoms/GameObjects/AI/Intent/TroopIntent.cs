namespace GameObjects.AI;

public sealed record TroopIntent(
    TroopIntentId Id,
    TroopIntentKind Kind,
    TacticalPosture PreferredPosture,
    IntentTargetRef Target,
    int Priority,
    int CommitUntilTick,
    int CooldownUntilTick,
    int SourceLegionId,
    int SourceFactionId,
    int RiskBudget,
    int VisibilityStamp,
    int TargetOwnerStamp,
    FallbackPolicy FallbackPolicy)
{
    public static TroopIntent CreateHold(int troopId, int issuedTick, int sourceLegionId, int sourceFactionId)
    {
        return new TroopIntent(
            new TroopIntentId(troopId, 1, issuedTick),
            TroopIntentKind.Hold,
            TacticalPosture.Hold,
            IntentTargetRef.None,
            0,
            issuedTick,
            issuedTick,
            sourceLegionId,
            sourceFactionId,
            0,
            0,
            -1,
            FallbackPolicy.Hold);
    }
}
