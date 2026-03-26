namespace GameObjects.AI;

public enum TroopIntentFailureReason : byte
{
    TargetLost = 0,
    TargetOwnerChanged = 1,
    VisibilityLost = 2,
    PathBlocked = 3,
    ArbitrationLost = 4,
    ThreatSpike = 5,
    InvalidDestination = 6,
    ExecutorConflict = 7
}
