namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public readonly record struct DuelPlaybackBeat(
    string Clip,
    float Duration,
    bool AllowSkip);

