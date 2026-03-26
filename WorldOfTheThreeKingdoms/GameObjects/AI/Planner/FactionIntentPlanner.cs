using System;
using GameManager;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed class FactionIntentPlanner
{
    public FactionIntent BuildFactionIntent(GameScenario scenario, Faction faction, AIAdvisorFacade advisorFacade, int issuedTick, int version)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (faction == null) throw new ArgumentNullException(nameof(faction));
        if (advisorFacade == null) throw new ArgumentNullException(nameof(advisorFacade));

        StrategicStance legacyStance = advisorFacade.GetStrategicStanceAdvice(faction);
        FactionIntentKind baselineKind = MapKind(legacyStance);

        TheaterStrategicSnapshot snapshot = TheaterPlanningService.BuildFactionSnapshot(
            scenario,
            faction,
            baselineKind,
            issuedTick);
        TheaterTargetScore secondaryAssault = TheaterPlanningService.ResolveSecondaryAssaultTarget(
            scenario,
            faction,
            baselineKind,
            snapshot.PrimaryAssault.ArchitectureId);

        FactionStrategicAnalysis strategicAnalysis = advisorFacade.BuildStrategicAnalysis(
            scenario,
            faction,
            snapshot,
            secondaryAssault,
            issuedTick);
        FactionStrategyDirective directive = strategicAnalysis.Directive;

        FactionIntentKind kind = directive.IntentKind;
        if (kind != baselineKind)
        {
            snapshot = TheaterPlanningService.BuildFactionSnapshot(
                scenario,
                faction,
                kind,
                issuedTick);
            secondaryAssault = TheaterPlanningService.ResolveSecondaryAssaultTarget(
                scenario,
                faction,
                kind,
                snapshot.PrimaryAssault.ArchitectureId);
        }

        int fallbackArchitectureId = faction.Capital?.ID ?? -1;
        Point fallbackPosition = faction.Capital?.Position ?? Point.Zero;

        TheaterTargetScore assaultFocus = snapshot.PrimaryAssault;
        TheaterTargetScore defenseFocus = snapshot.PrimaryDefense;

        int focusArchitectureId = ResolveFocusArchitectureId(
            directive.PrimaryArchitectureId,
            assaultFocus,
            fallbackArchitectureId);
        Point focusPosition = ResolveFocusPosition(
            scenario,
            focusArchitectureId,
            directive.PrimaryPosition,
            assaultFocus,
            fallbackPosition);
        int focusScore = ResolveFocusScore(
            focusArchitectureId,
            directive.PrimaryArchitectureId,
            directive.PrimaryScore,
            assaultFocus);

        int defensiveFocusArchitectureId = ResolveFocusArchitectureId(
            directive.DefensiveArchitectureId,
            defenseFocus,
            fallbackArchitectureId);
        Point defensiveFocusPosition = ResolveFocusPosition(
            scenario,
            defensiveFocusArchitectureId,
            directive.DefensivePosition,
            defenseFocus,
            fallbackPosition);
        int defensiveFocusScore = ResolveFocusScore(
            defensiveFocusArchitectureId,
            directive.DefensiveArchitectureId,
            directive.DefensiveScore,
            defenseFocus);

        int secondaryFocusArchitectureId = ResolveFocusArchitectureId(
            directive.SecondaryArchitectureId,
            secondaryAssault,
            defensiveFocusArchitectureId);
        Point secondaryFocusPosition = ResolveFocusPosition(
            scenario,
            secondaryFocusArchitectureId,
            directive.SecondaryPosition,
            secondaryAssault,
            defensiveFocusPosition);
        int secondaryFocusScore = ResolveFocusScore(
            secondaryFocusArchitectureId,
            directive.SecondaryArchitectureId,
            directive.SecondaryScore,
            secondaryAssault);

        int priority = Math.Max(ResolvePriority(kind), directive.Priority);
        int commitUntilTick = Math.Max(issuedTick + ResolveCommitTicks(kind), directive.CommitUntilTick);

        return new FactionIntent(
            new FactionIntentId(faction.ID, version, issuedTick),
            kind,
            priority,
            commitUntilTick,
            faction.ID,
            focusArchitectureId,
            focusPosition,
            focusScore,
            defensiveFocusArchitectureId,
            defensiveFocusPosition,
            defensiveFocusScore,
            snapshot.AverageVisibilityConfidence,
            snapshot.AverageNetEnergy,
            directive.Mode,
            directive.CommitUntilTick,
            directive.ReevaluateAfterTick,
            secondaryFocusArchitectureId,
            secondaryFocusPosition,
            secondaryFocusScore,
            directive.ReserveRatioPermille,
            directive.SortieBudgetPermille,
            directive.SiegeBudgetPermille,
            directive.RiskTolerance,
            directive.AttackFrozen,
            directive.StrategyPressure,
            directive.FailurePressure,
            directive.ThreatPressure,
            directive.PersonalityAggressionPermille,
            directive.PersonalityDefensePermille,
            directive.PersonalityRiskPermille,
            directive.PersonalityDisciplinePermille,
            directive.PersonalityVolatilityPermille);
    }

    private static FactionIntentKind MapKind(StrategicStance stance)
    {
        return stance switch
        {
            StrategicStance.Expansion => FactionIntentKind.Expansion,
            StrategicStance.Stabilization => FactionIntentKind.Stabilize,
            StrategicStance.Defense => FactionIntentKind.Defense,
            StrategicStance.Crisis => FactionIntentKind.Crisis,
            StrategicStance.Opportunistic => FactionIntentKind.Opportunistic,
            _ => FactionIntentKind.Hold
        };
    }

    private static int ResolvePriority(FactionIntentKind kind)
    {
        return kind switch
        {
            FactionIntentKind.Crisis => 100,
            FactionIntentKind.Defense => 80,
            FactionIntentKind.Expansion => 70,
            FactionIntentKind.Opportunistic => 65,
            FactionIntentKind.Stabilize => 50,
            _ => 10
        };
    }

    private static int ResolveCommitTicks(FactionIntentKind kind)
    {
        return kind switch
        {
            FactionIntentKind.Crisis => 1,
            FactionIntentKind.Defense => 2,
            FactionIntentKind.Expansion => 2,
            FactionIntentKind.Opportunistic => 1,
            FactionIntentKind.Stabilize => 3,
            _ => 1
        };
    }

    private static int ResolveFocusArchitectureId(int directiveArchitectureId, in TheaterTargetScore fallbackScore, int fallbackArchitectureId)
    {
        if (directiveArchitectureId >= 0) return directiveArchitectureId;
        if (fallbackScore.IsValid) return fallbackScore.ArchitectureId;
        return fallbackArchitectureId;
    }

    private static Point ResolveFocusPosition(
        GameScenario scenario,
        int architectureId,
        Point directivePosition,
        in TheaterTargetScore fallbackScore,
        Point fallbackPosition)
    {
        if (architectureId >= 0)
        {
            Architecture architecture = scenario.Architectures.GetGameObject(architectureId) as Architecture;
            if (architecture != null) return architecture.Position;
        }

        if (IsValidPosition(directivePosition)) return directivePosition;
        if (fallbackScore.IsValid) return fallbackScore.Position;
        return fallbackPosition;
    }

    private static int ResolveFocusScore(
        int architectureId,
        int directiveArchitectureId,
        int directiveScore,
        in TheaterTargetScore fallbackScore)
    {
        if (directiveArchitectureId == architectureId) return directiveScore;
        if (fallbackScore.IsValid && fallbackScore.ArchitectureId == architectureId) return fallbackScore.Score;
        return 0;
    }

    private static bool IsValidPosition(Point position)
    {
        return position.X >= 0 && position.Y >= 0 && position != Point.Zero;
    }
}
