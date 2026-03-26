namespace GameObjects.AI;

public enum IntentCheckpointKind : byte
{
    None = 0,
    QueuePickup = 1,
    BeforeProjection = 2,
    BeforePathRequest = 3,
    AfterPathResolved = 4,
    BeforeStepMove = 5,
    AfterBlocked = 6,
    BeforeCombatCheck = 7,
    BeforeAttackCommit = 8
}
