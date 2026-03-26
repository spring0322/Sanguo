using System;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public static class TheaterPlanningService
{
    private const int TravelPenaltyFactor = 2;
    private const int EnemyNetEnergyWeight = 3;
    private const int FriendlyNetEnergyWeight = 4;
    private const int SupplyRiskPenaltyFactor = 2;

    public static TheaterStrategicSnapshot BuildFactionSnapshot(GameScenario scenario, Faction faction, FactionIntentKind intentKind, int tick)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (faction == null) throw new ArgumentNullException(nameof(faction));

        Point fallbackPosition = ResolveFactionFallbackPosition(faction);
        var architectureObjects = scenario.Architectures?.GameObjects;
        if (architectureObjects == null || architectureObjects.Count == 0)
        {
            return TheaterStrategicSnapshot.CreateHold(tick, faction.ID, fallbackPosition);
        }

        TheaterTargetScore bestAssault = TheaterTargetScore.None;
        TheaterTargetScore bestDefense = TheaterTargetScore.None;
        int totalVisibility = 0;
        int totalNetEnergy = 0;
        int scoredCount = 0;

        for (int i = 0; i < architectureObjects.Count; i++)
        {
            Architecture architecture = architectureObjects[i] as Architecture;
            if (architecture == null) continue;

            bool isFriendly = architecture.BelongedFaction == faction;
            TheaterFocusMode focusMode = isFriendly ? TheaterFocusMode.Defend : TheaterFocusMode.Assault;
            TheaterTargetScore score = EvaluateArchitecture(
                scenario,
                faction,
                ResolveFactionFallbackPosition(faction),
                architecture,
                focusMode,
                intentKind);

            totalVisibility += score.VisibilityConfidence;
            totalNetEnergy += score.NetEnergy;
            scoredCount++;

            if (isFriendly)
            {
                if (!bestDefense.IsValid || score.Score > bestDefense.Score)
                {
                    bestDefense = score;
                }
            }
            else if (!bestAssault.IsValid || score.Score > bestAssault.Score)
            {
                bestAssault = score;
            }
        }

        if (!bestDefense.IsValid && faction.Capital != null)
        {
            bestDefense = EvaluateArchitecture(
                scenario,
                faction,
                ResolveFactionFallbackPosition(faction),
                faction.Capital,
                TheaterFocusMode.Defend,
                intentKind);
        }

        if (!bestAssault.IsValid)
        {
            bestAssault = bestDefense;
        }

        int averageVisibility = scoredCount > 0 ? totalVisibility / scoredCount : 0;
        int averageNetEnergy = scoredCount > 0 ? totalNetEnergy / scoredCount : 0;

        return new TheaterStrategicSnapshot(
            tick,
            faction.ID,
            bestAssault,
            bestDefense,
            averageVisibility,
            averageNetEnergy);
    }

    public static TheaterTargetScore ResolveSecondaryAssaultTarget(
        GameScenario scenario,
        Faction faction,
        FactionIntentKind intentKind,
        int primaryArchitectureId)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (faction == null) throw new ArgumentNullException(nameof(faction));

        var architectureObjects = scenario.Architectures?.GameObjects;
        if (architectureObjects == null || architectureObjects.Count == 0)
        {
            return TheaterTargetScore.None;
        }

        Point origin = ResolveFactionFallbackPosition(faction);
        TheaterTargetScore secondaryAssault = TheaterTargetScore.None;

        for (int i = 0; i < architectureObjects.Count; i++)
        {
            Architecture architecture = architectureObjects[i] as Architecture;
            if (architecture == null) continue;
            if (architecture.BelongedFaction == faction) continue;
            if (architecture.ID == primaryArchitectureId) continue;

            TheaterTargetScore candidate = EvaluateArchitecture(
                scenario,
                faction,
                origin,
                architecture,
                TheaterFocusMode.Assault,
                intentKind);

            if (!secondaryAssault.IsValid || candidate.Score > secondaryAssault.Score)
            {
                secondaryAssault = candidate;
            }
        }

        return secondaryAssault;
    }

    public static IntentTargetRef ResolveLegionTarget(
        GameScenario scenario,
        Legion legion,
        FactionIntent factionIntent,
        out TheaterTargetScore targetScore)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (legion == null) throw new ArgumentNullException(nameof(legion));
        if (factionIntent == null) throw new ArgumentNullException(nameof(factionIntent));

        Faction faction = legion.BelongedFaction;
        if (faction == null)
        {
            targetScore = TheaterTargetScore.None;
            return IntentTargetRef.None;
        }

        TheaterFocusMode focusMode = MapMissionToFocusMode(legion.Mission);
        Point legionOrigin = ResolveLegionOrigin(legion, faction);

        int candidateArchitectureId = ResolveLegionCandidateArchitectureId(legion, factionIntent, focusMode);
        targetScore = EvaluateArchitectureById(
            scenario,
            faction,
            legionOrigin,
            candidateArchitectureId,
            focusMode,
            factionIntent.Kind);

        if (!targetScore.IsValid)
        {
            int fallbackId = focusMode is TheaterFocusMode.Defend or TheaterFocusMode.Recover
                ? factionIntent.DefensiveFocusArchitectureId
                : factionIntent.StrategicFocusArchitectureId;
            targetScore = EvaluateArchitectureById(
                scenario,
                faction,
                legionOrigin,
                fallbackId,
                focusMode,
                factionIntent.Kind);
        }

        if (!targetScore.IsValid)
        {
            return IntentTargetRef.None;
        }

        Architecture targetArchitecture = scenario.Architectures.GetGameObject(targetScore.ArchitectureId) as Architecture;
        if (targetArchitecture == null)
        {
            return IntentTargetRef.None;
        }

        return IntentTargetRef.ForArchitecture(
            targetArchitecture.ID,
            targetArchitecture.Position,
            targetArchitecture.BelongedFaction?.ID ?? -1);
    }

    public static TheaterTargetScore EvaluateLegionTarget(
        GameScenario scenario,
        Legion legion,
        int architectureId,
        TheaterFocusMode focusMode,
        FactionIntentKind intentKind)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (legion == null) throw new ArgumentNullException(nameof(legion));
        if (architectureId < 0) return TheaterTargetScore.None;

        Faction faction = legion.BelongedFaction;
        if (faction == null) return TheaterTargetScore.None;

        Point origin = ResolveLegionOrigin(legion, faction);
        return EvaluateArchitectureById(scenario, faction, origin, architectureId, focusMode, intentKind);
    }

    private static TheaterTargetScore EvaluateArchitectureById(
        GameScenario scenario,
        Faction faction,
        Point origin,
        int architectureId,
        TheaterFocusMode focusMode,
        FactionIntentKind intentKind)
    {
        if (architectureId < 0) return TheaterTargetScore.None;

        Architecture architecture = scenario.Architectures.GetGameObject(architectureId) as Architecture;
        if (architecture == null)
        {
            return TheaterTargetScore.None;
        }

        return EvaluateArchitecture(scenario, faction, origin, architecture, focusMode, intentKind);
    }

    private static TheaterTargetScore EvaluateArchitecture(
        GameScenario scenario,
        Faction faction,
        Point origin,
        Architecture architecture,
        TheaterFocusMode focusMode,
        FactionIntentKind intentKind)
    {
        bool isFriendly = architecture.BelongedFaction == faction;
        int travelCost = EstimateTravelCost(scenario, origin, architecture.Position);
        int netEnergy = TheaterSpatialCosting.CalculateNetInfluenceAt(faction, architecture.Position);
        int visibilityConfidence = TheaterSpatialCosting.CalculateVisibilityConfidence(faction, architecture.Position);
        int supplyRisk = ResolveSupplyRisk(travelCost, netEnergy, isFriendly);
        int strategicValue = ResolveStrategicValue(architecture, isFriendly);
        int focusBias = ResolveFocusBias(focusMode, intentKind, isFriendly);

        int netEnergyScore = isFriendly
            ? netEnergy / FriendlyNetEnergyWeight
            : netEnergy / EnemyNetEnergyWeight;

        int score =
            strategicValue +
            focusBias +
            netEnergyScore +
            visibilityConfidence -
            travelCost * TravelPenaltyFactor -
            supplyRisk * SupplyRiskPenaltyFactor;

        return new TheaterTargetScore(
            architecture.ID,
            architecture.Position,
            score,
            travelCost,
            netEnergy,
            visibilityConfidence,
            supplyRisk,
            focusMode);
    }

    private static int ResolveStrategicValue(Architecture architecture, bool isFriendly)
    {
        int frontlineBonus = (architecture.FrontLine || architecture.HostileLine) ? 20 : 8;
        int resourceScore = Math.Clamp(architecture.Food / 8000 + architecture.Fund / 6000, 0, 30);
        int enduranceScore = Math.Clamp(architecture.Endurance / 4000, 0, 25);
        int populationScore = Math.Clamp(architecture.Population / 60000, 0, 25);
        int capitalBonus = architecture.BelongedFaction?.Capital == architecture ? 20 : 0;

        if (isFriendly)
        {
            return frontlineBonus + enduranceScore + resourceScore + populationScore + capitalBonus;
        }

        int breachValue = 28 - enduranceScore;
        return frontlineBonus + breachValue + resourceScore + populationScore + capitalBonus;
    }

    private static int ResolveFocusBias(TheaterFocusMode focusMode, FactionIntentKind intentKind, bool isFriendlyTarget)
    {
        int modeBias = focusMode switch
        {
            TheaterFocusMode.Assault => isFriendlyTarget ? -20 : 18,
            TheaterFocusMode.Defend => isFriendlyTarget ? 18 : -20,
            TheaterFocusMode.Recover => isFriendlyTarget ? 12 : -15,
            _ => 0
        };

        int factionBias = intentKind switch
        {
            FactionIntentKind.Crisis => isFriendlyTarget ? 18 : -10,
            FactionIntentKind.Defense => isFriendlyTarget ? 12 : -8,
            FactionIntentKind.Expansion => isFriendlyTarget ? -6 : 14,
            FactionIntentKind.Opportunistic => isFriendlyTarget ? -2 : 10,
            FactionIntentKind.Stabilize => isFriendlyTarget ? 8 : -4,
            _ => 0
        };

        return modeBias + factionBias;
    }

    private static int ResolveSupplyRisk(int travelCost, int netEnergy, bool isFriendly)
    {
        int risk = 0;

        if (travelCost > 12)
        {
            risk += (travelCost - 12) / 2;
        }

        if (netEnergy < 0)
        {
            risk += Math.Min(12, (-netEnergy + 24) / 25);
        }

        if (!isFriendly && travelCost > 18 && netEnergy <= 0)
        {
            risk += 4;
        }

        return risk;
    }

    private static int EstimateTravelCost(GameScenario scenario, Point from, Point to)
    {
        double distance = scenario.GetDistance(from, to);
        if (distance <= 0) return 0;
        return Math.Max(1, (int)Math.Round(distance / 3.0));
    }

    private static int ResolveLegionCandidateArchitectureId(Legion legion, FactionIntent factionIntent, TheaterFocusMode focusMode)
    {
        if (legion.WillArchitecture != null)
        {
            return legion.WillArchitecture.ID;
        }

        if (legion.Target != null)
        {
            return legion.Target.ID;
        }

        return focusMode is TheaterFocusMode.Defend or TheaterFocusMode.Recover
            ? factionIntent.DefensiveFocusArchitectureId
            : factionIntent.StrategicFocusArchitectureId;
    }

    private static Point ResolveLegionOrigin(Legion legion, Faction faction)
    {
        if (legion.CoreTroop != null && !legion.CoreTroop.Destroyed)
        {
            return legion.CoreTroop.Position;
        }

        if (legion.Troops != null)
        {
            for (int i = 0; i < legion.Troops.Count; i++)
            {
                Troop troop = legion.Troops[i] as Troop;
                if (troop != null && !troop.Destroyed)
                {
                    return troop.Position;
                }
            }
        }

        if (legion.StartArchitecture != null)
        {
            return legion.StartArchitecture.Position;
        }

        return ResolveFactionFallbackPosition(faction);
    }

    private static Point ResolveFactionFallbackPosition(Faction faction)
    {
        if (faction.Capital != null)
        {
            return faction.Capital.Position;
        }

        if (faction.Architectures != null && faction.Architectures.Count > 0)
        {
            Architecture architecture = faction.Architectures[0] as Architecture;
            if (architecture != null)
            {
                return architecture.Position;
            }
        }

        return Point.Zero;
    }

    private static TheaterFocusMode MapMissionToFocusMode(LegionMission mission)
    {
        return mission switch
        {
            LegionMission.Attack => TheaterFocusMode.Assault,
            LegionMission.Defend => TheaterFocusMode.Defend,
            LegionMission.Retreat => TheaterFocusMode.Recover,
            _ => TheaterFocusMode.Hold
        };
    }
}
