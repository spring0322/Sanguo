namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateResult
{
    public DebateOutcome Outcome { get; init; } = DebateOutcome.None;

    public int RoundCount { get; init; }

    public int LeftPersonId { get; init; }

    public int RightPersonId { get; init; }

    public int Seed { get; init; }
}

