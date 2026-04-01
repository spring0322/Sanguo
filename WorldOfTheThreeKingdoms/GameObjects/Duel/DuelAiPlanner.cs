using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelAiPlanner(DuelRuleSet ruleSet)
{
    private readonly DuelRuleSet duelRuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));

    public DuelCommand ChooseCommand(DuelSessionState sessionState, bool isLeftSide, DuelRandom duelRandom)
    {
        if (sessionState == null)
        {
            throw new ArgumentNullException(nameof(sessionState));
        }

        if (duelRandom == null)
        {
            throw new ArgumentNullException(nameof(duelRandom));
        }

        DuelSideState self = isLeftSide ? sessionState.LeftSide : sessionState.RightSide;
        float lifeRatio = self.MaxLife <= 0f ? 0f : self.CurrentLife / self.MaxLife;
        DuelCommandType commandType;

        if (lifeRatio <= duelRuleSet.DefensiveLifeRatioThreshold)
        {
            commandType = duelRandom.Next(0, 100) < duelRuleSet.DefensiveBiasPercent
                ? DuelCommandType.Defensive
                : DuelCommandType.Normal;
        }
        else
        {
            int roll = duelRandom.Next(0, 100);
            if (roll < duelRuleSet.AggressiveBiasPercent)
            {
                commandType = DuelCommandType.Aggressive;
            }
            else if (roll < duelRuleSet.AggressiveBiasPercent + duelRuleSet.DefensiveBiasPercent / 2)
            {
                commandType = DuelCommandType.Defensive;
            }
            else
            {
                commandType = DuelCommandType.Normal;
            }
        }

        return new DuelCommand(commandType);
    }
}
