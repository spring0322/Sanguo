namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateSideState
{
    public DebateParticipantSeed Snapshot { get; init; }

    public float CurrentMomentum { get; set; }

    public float MaxMomentum { get; init; }

    public int CurrentFocus { get; set; }

    public int MaxFocus { get; init; }

    public DebateCommand LockedCommand { get; set; } = DebateCommand.Auto;

    public DebateCommand LastCommand { get; set; } = DebateCommand.Auto;

    public bool IsPlayerControlled { get; init; }

    public bool CommandLocked { get; set; }

    public bool IsAlive => CurrentMomentum > 0f;
}

