namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public readonly record struct DebateCommand(
    DebateCommandType CommandType,
    int TargetId = 0,
    int Payload = 0,
    int FrameLock = 0)
{
    public static DebateCommand Auto => new(DebateCommandType.Auto);
}

