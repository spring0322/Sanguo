using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameObjects.AI;

public sealed class AIAdvisorFacade
{
    private readonly Dictionary<int, FactionStrategyBrain> _strategyBrainsByFaction = new();
    private readonly Dictionary<int, FactionStrategicTelemetry> _telemetryByFaction = new();

    public TacticalDecision GetTacticalAdvice(Troop troop)
    {
        return AITacticalExecution.MakeTacticalDecision(troop);
    }

    public StrategicStance GetStrategicStanceAdvice(Faction faction)
    {
        FactionProfile profile = FactionProfile.CalculateProfile(faction);
        ResourceSnapshot snapshot = ResourceSnapshot.CalculateSnapshot(faction);
        global::GameManager.FactionNeighbors neighbors = global::GameManager.FactionNeighbors.AnalyzeNeighbors(faction);
        return global::GameManager.AIStrategicDecisionSystem.DetermineStrategicStance(profile, snapshot, neighbors, faction?.ID ?? 0);
    }

    public FactionStrategicAnalysis BuildStrategicAnalysis(
        GameScenario scenario,
        Faction faction,
        in TheaterStrategicSnapshot theaterSnapshot,
        in TheaterTargetScore secondaryAssault,
        int issuedTick)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (faction == null) throw new ArgumentNullException(nameof(faction));

        FactionProfile profile = FactionProfile.CalculateProfile(faction);
        ResourceSnapshot resourceSnapshot = ResourceSnapshot.CalculateSnapshot(faction);
        global::GameManager.FactionNeighbors neighbors = global::GameManager.FactionNeighbors.AnalyzeNeighbors(faction);
        StrategicStance legacyStance = global::GameManager.AIStrategicDecisionSystem.DetermineStrategicStance(
            profile,
            resourceSnapshot,
            neighbors,
            faction.ID);

        FactionStrategicTelemetry telemetry = GetOrCreateTelemetry(faction.ID);
        telemetry.ApplyDecay(issuedTick);

        FactionStrategyAssessmentSnapshot assessment = BuildAssessmentSnapshot(
            faction,
            legacyStance,
            resourceSnapshot,
            theaterSnapshot,
            secondaryAssault,
            telemetry,
            issuedTick);

        FactionStrategyBrain brain = GetOrCreateStrategyBrain(faction.ID);
        FactionStrategyDirective directive = brain.Evaluate(in assessment);
        return new FactionStrategicAnalysis(legacyStance, directive);
    }

    public void ReportTroopIntentFailure(int factionId, TroopIntentFailureReason reason, int tick)
    {
        if (factionId <= 0) return;

        FactionStrategicTelemetry telemetry = GetOrCreateTelemetry(factionId);
        telemetry.ApplyDecay(tick);
        telemetry.RegisterFailure(reason);
    }

    private FactionStrategyBrain GetOrCreateStrategyBrain(int factionId)
    {
        if (!_strategyBrainsByFaction.TryGetValue(factionId, out FactionStrategyBrain brain))
        {
            brain = new FactionStrategyBrain(factionId);
            _strategyBrainsByFaction[factionId] = brain;
        }

        return brain;
    }

    private FactionStrategicTelemetry GetOrCreateTelemetry(int factionId)
    {
        if (!_telemetryByFaction.TryGetValue(factionId, out FactionStrategicTelemetry telemetry))
        {
            telemetry = new FactionStrategicTelemetry();
            _telemetryByFaction[factionId] = telemetry;
        }

        return telemetry;
    }

    private static FactionStrategyAssessmentSnapshot BuildAssessmentSnapshot(
        Faction faction,
        StrategicStance legacyStance,
        ResourceSnapshot resourceSnapshot,
        in TheaterStrategicSnapshot theaterSnapshot,
        in TheaterTargetScore secondaryAssault,
        FactionStrategicTelemetry telemetry,
        int issuedTick)
    {
        CountTroopReadiness(faction, out int totalTroopCount, out int readyTroopCount, out int reserveTroopCount);
        FactionDecisionPersonalityProfile personalityProfile = FactionDecisionPersonalityResolver.Resolve(faction);

        TheaterTargetScore primary = theaterSnapshot.PrimaryAssault;
        TheaterTargetScore defense = theaterSnapshot.PrimaryDefense;
        Point fallbackPosition = ResolveFactionFallbackPosition(faction);

        int primaryArchitectureId = primary.IsValid ? primary.ArchitectureId : -1;
        Point primaryPosition = primary.IsValid ? primary.Position : fallbackPosition;
        int primaryScore = primary.IsValid ? primary.Score : 0;

        int defensiveArchitectureId = defense.IsValid ? defense.ArchitectureId : faction.Capital?.ID ?? -1;
        Point defensivePosition = defense.IsValid ? defense.Position : fallbackPosition;
        int defensiveScore = defense.IsValid ? defense.Score : 0;

        int secondaryArchitectureId = secondaryAssault.IsValid ? secondaryAssault.ArchitectureId : -1;
        Point secondaryPosition = secondaryAssault.IsValid ? secondaryAssault.Position : fallbackPosition;
        int secondaryScore = secondaryAssault.IsValid ? secondaryAssault.Score : 0;

        return new FactionStrategyAssessmentSnapshot(
            issuedTick,
            faction.ID,
            legacyStance,
            personalityProfile,
            faction.Architectures?.Count ?? 0,
            totalTroopCount,
            readyTroopCount,
            reserveTroopCount,
            ToPermille(resourceSnapshot.EconomicHealth),
            ToPercentPermille(resourceSnapshot.AverageFatigue),
            ToPermille(resourceSnapshot.ThreatLevel),
            ToPermille(resourceSnapshot.OpportunityLevel),
            Math.Clamp(theaterSnapshot.AverageVisibilityConfidence, 0, 100),
            theaterSnapshot.AverageNetEnergy,
            primaryArchitectureId,
            primaryPosition,
            primaryScore,
            defensiveArchitectureId,
            defensivePosition,
            defensiveScore,
            secondaryArchitectureId,
            secondaryPosition,
            secondaryScore,
            telemetry.FailurePressure,
            telemetry.ThreatPressure,
            telemetry.BlockedPressure,
            telemetry.FailureCount);
    }

    private static void CountTroopReadiness(Faction faction, out int totalTroopCount, out int readyTroopCount, out int reserveTroopCount)
    {
        totalTroopCount = 0;
        readyTroopCount = 0;
        reserveTroopCount = 0;

        if (faction?.Troops == null) return;

        var troopObjects = faction.Troops.GetList();
        if (troopObjects == null) return;

        for (int i = 0; i < troopObjects.Count; i++)
        {
            Troop troop = troopObjects[i] as Troop;
            if (troop == null || troop.Destroyed) continue;

            totalTroopCount++;

            bool hasFood = troop.FoodMax <= 0 || troop.Food > troop.FoodMax / 8;
            bool isReady = troop.Scales > 0 &&
                           troop.Morale >= 45 &&
                           hasFood;
            if (isReady)
            {
                readyTroopCount++;
            }

            bool isReserve = troop.CurrentAIState is TroopAIState.Idle or TroopAIState.Waiting or TroopAIState.EnterCity;
            if (isReserve)
            {
                reserveTroopCount++;
            }
        }
    }

    private static int ToPermille(float normalizedValue)
    {
        return Math.Clamp((int)Math.Round(normalizedValue * 1000f), 0, 1000);
    }

    private static int ToPercentPermille(float percentageValue)
    {
        return Math.Clamp((int)Math.Round(percentageValue * 10f), 0, 1000);
    }

    private static Point ResolveFactionFallbackPosition(Faction faction)
    {
        if (faction.Capital != null) return faction.Capital.Position;

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
}
