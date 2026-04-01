namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public readonly record struct DuelExchangeResult(
    DuelCommand LeftCommand,
    DuelCommand RightCommand,
    bool LeftActsFirst,
    float LeftDamageDealt,
    float RightDamageDealt,
    bool LeftBlocked,
    bool RightBlocked,
    bool LeftCritical,
    bool RightCritical,
    bool LeftEscaped,
    bool RightEscaped,
    DuelOutcome OutcomeAfterExchange);
