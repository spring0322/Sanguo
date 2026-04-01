namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateSessionState
{
    public DebateMode Mode { get; init; } = DebateMode.Battle;

    public int Seed { get; init; }

    public DebateSideState LeftSide { get; init; } = default!;

    public DebateSideState RightSide { get; init; } = default!;

    public DebateStage Stage { get; set; } = DebateStage.Intro;

    public DebateOutcome Outcome { get; set; } = DebateOutcome.None;

    public int Round { get; set; }

    public int LogicTick { get; set; }

    public int StageTick { get; set; }

    public int StageTickBudget { get; set; }

    public bool AwaitingPlayerInput { get; set; }

    public bool PlaybackRunning { get; set; }

    public bool Completed { get; set; }
}

