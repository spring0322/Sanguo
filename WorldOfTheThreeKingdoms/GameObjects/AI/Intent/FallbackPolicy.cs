namespace GameObjects.AI;

public enum FallbackPolicy : byte
{
    Hold = 0,
    Replan = 1,
    Withdraw = 2,
    RetargetSameMission = 3,
    Abort = 4
}
