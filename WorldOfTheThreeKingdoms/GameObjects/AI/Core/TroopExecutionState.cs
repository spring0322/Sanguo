namespace GameObjects.AI;

public sealed class TroopExecutionState
{
    public TroopIntentId ActiveIntentId { get; set; } = TroopIntentId.Empty;
    public TacticalPosture CurrentPosture { get; set; } = TacticalPosture.None;
    public int PostureEnteredTick { get; set; } = -1;
    public int PostureCommitUntilTick { get; set; } = -1;
    public int PostureCooldownUntilTick { get; set; } = -1;
    public IntentCheckpointKind LastCheckpoint { get; set; } = IntentCheckpointKind.None;
    public int LastValidatedTick { get; set; } = -1;
    public int BlockedTicks { get; set; }
    public int FriendlyBlockEscalationTicks { get; set; }
    public int SwapCooldownUntilTick { get; set; } = -1;
    public int PathFailStreak { get; set; }
    public int NoProgressTicks { get; set; }
    public int LegacyStuckCounterMirror { get; set; }
    public int LegacyWaitCounterMirror { get; set; }
    public int ConsecutiveThreatSpikeTicks { get; set; }
    public bool PendingReplan { get; set; }
    public int LocalCooldownUntilTick { get; set; } = -1;
    public TroopIntentFailureReason LastFailureReason { get; set; } = TroopIntentFailureReason.ExecutorConflict;
}
