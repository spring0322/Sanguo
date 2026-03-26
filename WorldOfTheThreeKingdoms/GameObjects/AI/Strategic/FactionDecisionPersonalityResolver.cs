using System;
using GameManager;

namespace GameObjects.AI;

public static class FactionDecisionPersonalityResolver
{
    public static FactionDecisionPersonalityProfile Resolve(Faction faction)
    {
        if (faction == null || faction.Leader == null)
        {
            return FactionDecisionPersonalityProfile.Balanced;
        }

        StrategicDecisionWeights weights = StrategicDecisionManager.GetWeights(faction);
        Person leader = faction.Leader;

        int braveness = Math.Clamp(leader.Braveness, 1, 100);
        int calmness = Math.Clamp(leader.Calmness, 1, 100);
        int intellect = Math.Clamp((leader.Intelligence + leader.Politics) / 2, 1, 100);
        int ambition = Math.Clamp((int)leader.Ambition, 0, 4);

        int aggression = Math.Clamp((int)Math.Round(weights.Aggression * 1000f), 650, 1650);
        int defense = Math.Clamp((int)Math.Round(weights.Defense * 1000f), 650, 1650);
        int risk = Math.Clamp((int)Math.Round(weights.RiskTolerance * 1000f), 650, 1600);

        aggression = Math.Clamp(aggression + (braveness - calmness) * 4 + ambition * 60, 600, 1800);
        defense = Math.Clamp(defense + (calmness - braveness) * 4 + (100 - braveness), 600, 1800);
        risk = Math.Clamp(risk + (braveness - calmness) * 3 + ambition * 45, 600, 1800);

        int discipline = Math.Clamp(780 + intellect * 5 + calmness * 3 - ambition * 40, 650, 1600);
        int volatility = Math.Clamp(1050 + (braveness - calmness) * 5 + ambition * 55 - intellect * 2, 350, 1500);

        return new FactionDecisionPersonalityProfile(
            aggression,
            defense,
            risk,
            discipline,
            volatility);
    }

    public static FactionDecisionPersonalityProfile Resolve(in FactionIntent factionIntent)
    {
        if (factionIntent.PersonalityAggressionPermille <= 0 ||
            factionIntent.PersonalityDefensePermille <= 0 ||
            factionIntent.PersonalityRiskPermille <= 0)
        {
            return FactionDecisionPersonalityProfile.Balanced;
        }

        return new FactionDecisionPersonalityProfile(
            factionIntent.PersonalityAggressionPermille,
            factionIntent.PersonalityDefensePermille,
            factionIntent.PersonalityRiskPermille,
            factionIntent.PersonalityDisciplinePermille <= 0 ? 1000 : factionIntent.PersonalityDisciplinePermille,
            factionIntent.PersonalityVolatilityPermille <= 0 ? 900 : factionIntent.PersonalityVolatilityPermille);
    }
}
