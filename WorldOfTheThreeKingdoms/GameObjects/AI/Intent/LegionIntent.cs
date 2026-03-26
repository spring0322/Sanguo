using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed record LegionIntent(
    LegionIntentId Id,
    LegionIntentKind Kind,
    IntentTargetRef Target,
    int Priority,
    int CommitUntilTick,
    int SourceFactionId,
    int SourceLegionId,
    FactionIntentId ParentFactionIntentId,
    TheaterFocusMode TheaterFocusMode,
    Point TheaterAnchorPosition,
    int TheaterScore,
    int TheaterTravelCost,
    int TheaterVisibilityConfidence,
    int TheaterEnergyBalance,
    FactionStrategyMode StrategyMode,
    LegionFrontRole FrontRole,
    int StrategyRiskTolerance,
    bool AttackAuthorized,
    int ReserveRatioPermille)
{
    public static LegionIntent CreateHold(int legionId, int factionId, int issuedTick, in FactionIntentId parentFactionIntentId)
    {
        return new LegionIntent(
            new LegionIntentId(legionId, 1, issuedTick),
            LegionIntentKind.Hold,
            IntentTargetRef.None,
            0,
            issuedTick,
            factionId,
            legionId,
            parentFactionIntentId,
            TheaterFocusMode.Hold,
            Point.Zero,
            0,
            0,
            0,
            0,
            FactionStrategyMode.Rest,
            LegionFrontRole.Reserve,
            40,
            false,
            700);
    }
}
