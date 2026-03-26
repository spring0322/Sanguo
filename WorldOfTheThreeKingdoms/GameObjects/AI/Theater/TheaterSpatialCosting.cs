using System;
using GameManager;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameObjects.AI;

public static class TheaterSpatialCosting
{
    private const int UnknownVisibilityConfidence = 15;

    public static int CalculateVisibilityConfidence(Faction faction, Point position)
    {
        if (faction == null) throw new ArgumentNullException(nameof(faction));

        if (!faction.IsPositionKnown(position))
        {
            return UnknownVisibilityConfidence;
        }

        InformationLevel knownLevel = faction.GetKnownAreaDataNoCheck(position);
        int levelValue = Math.Max(1, (int)knownLevel);
        return Math.Clamp(levelValue * 20, 20, 100);
    }

    public static int CalculateNetInfluenceAt(Faction faction, Point position)
    {
        if (faction == null) throw new ArgumentNullException(nameof(faction));

        GameScenario scenario = Session.Current?.Scenario;
        if (!TryGetInfluenceIndex(scenario, position, out int index))
        {
            return 0;
        }

        int friendlyEnergy = GetFactionEnergyAt(faction, index);
        int maxEnemyEnergy = 0;

        var factions = scenario.Factions?.GameObjects;
        if (factions == null) return friendlyEnergy;

        for (int i = 0; i < factions.Count; i++)
        {
            Faction otherFaction = factions[i] as Faction;
            if (otherFaction == null || otherFaction == faction || faction.IsFriendly(otherFaction))
            {
                continue;
            }

            int enemyEnergy = GetFactionEnergyAt(otherFaction, index);
            if (enemyEnergy > maxEnemyEnergy)
            {
                maxEnemyEnergy = enemyEnergy;
            }
        }

        return friendlyEnergy - maxEnemyEnergy;
    }

    public static int EvaluateAdditionalTacticalCost(Troop troop, Point neighbor)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));

        Faction faction = troop.BelongedFaction;
        if (faction == null) return 0;

        int visibilityConfidence = CalculateVisibilityConfidence(faction, neighbor);
        int netInfluence = CalculateNetInfluenceAt(faction, neighbor);
        int additionalCost = 0;

        if (visibilityConfidence < 35)
        {
            additionalCost += 8;
        }
        else if (visibilityConfidence >= 80)
        {
            additionalCost -= 2;
        }

        if (netInfluence < 0)
        {
            additionalCost += Math.Min(24, (-netInfluence + 9) / 10);
        }
        else if (netInfluence > 0)
        {
            additionalCost -= Math.Min(6, netInfluence / 18);
        }

        if (troop.StartingArchitecture != null)
        {
            GameScenario scenario = Session.Current?.Scenario;
            if (scenario != null)
            {
                double distance = scenario.GetDistance(troop.StartingArchitecture.Position, neighbor);
                if (distance > 22.0 && netInfluence <= 0)
                {
                    additionalCost += Math.Min(10, (int)Math.Ceiling((distance - 22.0) / 4.0));
                }
            }
        }

        return additionalCost;
    }

    public static int EvaluatePositionBonus(Troop troop, Point position)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));

        Faction faction = troop.BelongedFaction;
        if (faction == null) return 0;

        int netInfluence = CalculateNetInfluenceAt(faction, position);
        int visibilityConfidence = CalculateVisibilityConfidence(faction, position);
        int bonus = 0;

        if (netInfluence > 0)
        {
            bonus += Math.Min(12, netInfluence / 12);
        }
        else if (netInfluence < 0)
        {
            bonus -= Math.Min(8, (-netInfluence) / 16);
        }

        bonus += visibilityConfidence / 25;

        if (troop.StartingArchitecture != null)
        {
            GameScenario scenario = Session.Current?.Scenario;
            if (scenario != null)
            {
                double distance = scenario.GetDistance(troop.StartingArchitecture.Position, position);
                if (distance <= 18.0)
                {
                    bonus += 2;
                }
                else if (distance >= 30.0)
                {
                    bonus -= 2;
                }
            }
        }

        return bonus;
    }

    private static int GetFactionEnergyAt(Faction faction, int index)
    {
        TileInfluenceState[] influenceMap = faction.GlobalInfluenceMap;
        if (influenceMap == null || index < 0 || index >= influenceMap.Length)
        {
            return 0;
        }

        return influenceMap[index].EffectiveTotalEnergy;
    }

    private static bool TryGetInfluenceIndex(GameScenario scenario, Point position, out int index)
    {
        index = -1;
        if (scenario == null || scenario.ScenarioMap == null) return false;

        Point mapSize = scenario.ScenarioMap.MapDimensions;
        if (position.X < 0 || position.Y < 0 || position.X >= mapSize.X || position.Y >= mapSize.Y)
        {
            return false;
        }

        index = position.Y * mapSize.X + position.X;
        return true;
    }
}
