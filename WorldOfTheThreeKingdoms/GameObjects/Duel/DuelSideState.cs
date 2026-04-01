namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelSideState
{
    public DuelParticipantSnapshot Snapshot { get; init; }

    public float CurrentLife { get; set; }

    public float MaxLife { get; init; }

    public DuelCommand LockedCommand { get; set; } = DuelCommand.Auto;

    public DuelCommand LastCommand { get; set; } = DuelCommand.Auto;

    public int GuardValue { get; set; }

    public int StaggerValue { get; set; }

    public bool IsPlayerControlled { get; init; }

    public bool IsEscaped { get; set; }

    public bool CommandLocked { get; set; }

    public bool IsAlive => !IsEscaped && CurrentLife > 0f;
}
