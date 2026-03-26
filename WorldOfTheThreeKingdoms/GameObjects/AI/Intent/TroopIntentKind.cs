namespace GameObjects.AI;

public enum TroopIntentKind : byte
{
    None = 0,
    Hold = 1,
    March = 2,
    EnterCity = 3,
    AttackTroop = 4,
    AttackArchitecture = 5,
    Withdraw = 6,
    Recover = 7,
    BlockRetry = 8
}
