namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public readonly record struct DuelCommand(
    DuelCommandType CommandType,
    int TargetId = 0,
    int Payload = 0,
    int FrameLock = 0)
{
    public static DuelCommand Auto => new(DuelCommandType.Auto);
}
