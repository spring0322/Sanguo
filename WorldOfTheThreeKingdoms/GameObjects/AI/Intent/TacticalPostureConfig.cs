namespace GameObjects.AI;

public static class TacticalPostureConfig
{
    public const int WithdrawEnterThreatScore = 80;
    public const int WithdrawExitThreatScore = 45;

    public const int CommitEnterThreatCap = 55;
    public const int CommitExitThreatScore = 70;

    public const int ThreatSpikeScore = 85;
    public const int ThreatSpikeReplanThreshold = 2;

    public const int HoldCommitTicks = 1;
    public const int AdvanceCommitTicks = 1;
    public const int CommitCommitTicks = 2;
    public const int WithdrawCommitTicks = 2;
    public const int RecoverCommitTicks = 2;

    public const int HoldCooldownTicks = 1;
    public const int AdvanceCooldownTicks = 1;
    public const int CommitCooldownTicks = 1;
    public const int WithdrawCooldownTicks = 2;
    public const int RecoverCooldownTicks = 1;

    public const int ReplanBlockedThreshold = 2;

    public static int GetCommitTicks(TacticalPosture posture)
    {
        return posture switch
        {
            TacticalPosture.Withdraw => WithdrawCommitTicks,
            TacticalPosture.Commit => CommitCommitTicks,
            TacticalPosture.Advance => AdvanceCommitTicks,
            TacticalPosture.Recover => RecoverCommitTicks,
            _ => HoldCommitTicks
        };
    }

    public static int GetCooldownTicks(TacticalPosture posture)
    {
        return posture switch
        {
            TacticalPosture.Withdraw => WithdrawCooldownTicks,
            TacticalPosture.Commit => CommitCooldownTicks,
            TacticalPosture.Advance => AdvanceCooldownTicks,
            TacticalPosture.Recover => RecoverCooldownTicks,
            _ => HoldCooldownTicks
        };
    }
}
