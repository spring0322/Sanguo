using System;
using GameManager;

namespace GameObjects.AI;

public static class CommanderEnergyScoring
{
    private const int EnergyWeight = 8;
    private const int CommandWeight = 2;
    private const int IntelligenceWeight = 1;
    private const int ReputationWeight = 1;

    public static int CalculateCandidateScore(
        Troop simulatedTroop,
        bool offensive,
        Architecture sourceArchitecture,
        Architecture targetArchitecture)
    {
        if (simulatedTroop == null) throw new ArgumentNullException(nameof(simulatedTroop));

        int baseCombatScore = Math.Max(0, simulatedTroop.SimulatingFightingForce);
        int energyPotential = EstimateEnergyPotential(simulatedTroop);
        int leaderScore = CalculateLeaderScore(simulatedTroop.Leader);
        int logisticsPenalty = CalculateLogisticsPenalty(
            simulatedTroop,
            offensive,
            sourceArchitecture,
            targetArchitecture,
            energyPotential);

        return baseCombatScore +
               energyPotential * EnergyWeight +
               leaderScore -
               logisticsPenalty;
    }

    public static int EstimateEnergyPotential(Troop troop)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        if (troop.Army == null || troop.Persons == null) return 0;

        return WorldOfTheThreeKingdoms.GameObjects.EnergyCalculator.CalculateTroopZocEnergy(
            troop.Persons,
            troop.Quantity,
            troop.Morale,
            troop.Offence,
            troop.Defence,
            troop.Movability);
    }

    private static int CalculateLeaderScore(Person leader)
    {
        if (leader == null) return 0;

        int commandScore = leader.Command * CommandWeight;
        int intelligenceScore = leader.Intelligence * IntelligenceWeight;
        int reputationScore = (int)Math.Sqrt(Math.Max(0, leader.Reputation)) * ReputationWeight;
        return commandScore + intelligenceScore + reputationScore;
    }

    private static int CalculateLogisticsPenalty(
        Troop simulatedTroop,
        bool offensive,
        Architecture sourceArchitecture,
        Architecture targetArchitecture,
        int energyPotential)
    {
        if (!offensive || sourceArchitecture == null || targetArchitecture == null) return 0;

        GameScenario scenario = Session.Current?.Scenario;
        if (scenario == null) return 0;

        double distance = scenario.GetDistance(sourceArchitecture.Position, targetArchitecture.Position);
        if (distance <= 20.0) return 0;

        int distancePenalty = (int)Math.Min(40, (distance - 20.0) * 0.8);
        if (energyPotential >= 60)
        {
            distancePenalty = Math.Max(0, distancePenalty - 8);
        }

        int conservativeFoodCostPerDay = Troop.GetConservativePlanningFoodCostPerDay(simulatedTroop.Army);
        if (conservativeFoodCostPerDay > 0 && simulatedTroop.Food < conservativeFoodCostPerDay * 10)
        {
            distancePenalty += 12;
        }

        return distancePenalty;
    }
}
