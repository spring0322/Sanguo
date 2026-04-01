namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelResult
{
    public DuelOutcome Outcome { get; init; } = DuelOutcome.None;

    public int LegacyResult { get; init; }

    public int RoundCount { get; init; }

    public int LeftPersonId { get; init; }

    public int RightPersonId { get; init; }

    public int Seed { get; init; }
}
