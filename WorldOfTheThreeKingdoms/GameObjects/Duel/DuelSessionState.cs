namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelSessionState
{
    public DuelMode Mode { get; init; } = DuelMode.Battle;

    public int Seed { get; init; }

    public DuelParticipantSnapshot Left { get; init; }

    public DuelParticipantSnapshot Right { get; init; }

    public DuelSideState LeftSide { get; init; } = default!;

    public DuelSideState RightSide { get; init; } = default!;

    public DuelStage Stage { get; set; } = DuelStage.Intro;

    public DuelOutcome Outcome { get; set; } = DuelOutcome.None;

    public int Round { get; set; }

    public int LogicTick { get; set; }

    public int StageTick { get; set; }

    public int StageTickBudget { get; set; }

    public bool AwaitingPlayerInput { get; set; }

    public bool PlaybackRunning { get; set; }

    public bool Completed { get; set; }
}
