using System;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed class FactionStrategyBrain
{
    private readonly int _factionId;
    private FactionStrategyDirective _currentDirective;
    private bool _hasDirective;

    public FactionStrategyBrain(int factionId)
    {
        _factionId = factionId;
    }

    public FactionStrategyDirective Evaluate(in FactionStrategyAssessmentSnapshot snapshot)
    {
        FactionStrategyDirective candidate = BuildDirective(in snapshot);

        if (!_hasDirective)
        {
            _currentDirective = candidate;
            _hasDirective = true;
            return _currentDirective;
        }

        if (!ShouldReevaluate(in snapshot, in candidate))
        {
            return ClampDirectiveToCurrentTick(in snapshot, in _currentDirective);
        }

        _currentDirective = candidate;
        return _currentDirective;
    }

    private bool ShouldReevaluate(in FactionStrategyAssessmentSnapshot snapshot, in FactionStrategyDirective candidate)
    {
        if (snapshot.Tick >= _currentDirective.ReevaluateAfterTick) return true;
        if (snapshot.Tick > _currentDirective.CommitUntilTick) return true;
        if (IsEmergency(in snapshot)) return true;

        int pressureDelta = candidate.StrategyPressure - _currentDirective.StrategyPressure;
        if (pressureDelta >= 20) return true;

        return false;
    }

    private static bool IsEmergency(in FactionStrategyAssessmentSnapshot snapshot)
    {
        return snapshot.ArchitectureCount <= 1 ||
               snapshot.ThreatPermille >= 900 ||
               snapshot.ThreatPressure >= 80 ||
               snapshot.FailurePressure >= 90;
    }

    private FactionStrategyDirective BuildDirective(in FactionStrategyAssessmentSnapshot snapshot)
    {
        FactionStrategyMode mode = SelectMode(in snapshot);
        bool attackFrozen = ShouldFreezeAttack(in snapshot, mode);

        FactionIntentKind intentKind = ResolveIntentKind(mode, snapshot.LegacyStance, snapshot.ThreatPermille);
        int pressure = ResolveStrategyPressure(in snapshot, mode);
        int priority = ResolvePriority(intentKind, pressure);
        int commitTicks = ResolveCommitTicks(mode, snapshot);
        int reevaluateTicks = ResolveReevaluateTicks(mode, snapshot);

        int reserveRatio = ResolveReserveRatio(mode, snapshot, attackFrozen);
        int sortieBudget = ResolveSortieBudget(mode, attackFrozen);
        int siegeBudget = ResolveSiegeBudget(mode, attackFrozen);
        int riskTolerance = ResolveRiskTolerance(mode, snapshot, attackFrozen);

        (int primaryArchitectureId, Point primaryPosition, int primaryScore) =
            ResolvePrimaryFocus(in snapshot, mode);
        (int secondaryArchitectureId, Point secondaryPosition, int secondaryScore) =
            ResolveSecondaryFocus(in snapshot, mode, primaryArchitectureId);

        return new FactionStrategyDirective(
            mode,
            intentKind,
            priority,
            snapshot.Tick + commitTicks,
            snapshot.Tick + reevaluateTicks,
            primaryArchitectureId,
            primaryPosition,
            primaryScore,
            secondaryArchitectureId,
            secondaryPosition,
            secondaryScore,
            snapshot.DefensiveFocusArchitectureId,
            snapshot.DefensiveFocusPosition,
            snapshot.DefensiveFocusScore,
            reserveRatio,
            sortieBudget,
            siegeBudget,
            riskTolerance,
            attackFrozen,
            pressure,
            snapshot.FailurePressure,
            snapshot.ThreatPressure,
            snapshot.PersonalityProfile.AggressionPermille,
            snapshot.PersonalityProfile.DefensePermille,
            snapshot.PersonalityProfile.RiskPermille,
            snapshot.PersonalityProfile.DisciplinePermille,
            snapshot.PersonalityProfile.VolatilityPermille);
    }

    private static FactionStrategyDirective ClampDirectiveToCurrentTick(
        in FactionStrategyAssessmentSnapshot snapshot,
        in FactionStrategyDirective directive)
    {
        int commitUntilTick = Math.Max(directive.CommitUntilTick, snapshot.Tick + 1);
        int reevaluateAfterTick = Math.Max(directive.ReevaluateAfterTick, snapshot.Tick + 1);

        return directive with
        {
            CommitUntilTick = commitUntilTick,
            ReevaluateAfterTick = reevaluateAfterTick,
            FailurePressure = snapshot.FailurePressure,
            ThreatPressure = snapshot.ThreatPressure
        };
    }

    private static FactionStrategyMode SelectMode(in FactionStrategyAssessmentSnapshot snapshot)
    {
        if (snapshot.ArchitectureCount <= 1 || snapshot.ThreatPermille >= 860)
        {
            return FactionStrategyMode.Fortify;
        }

        if (snapshot.EconomicHealthPermille <= 220 ||
            snapshot.ReadyRatioPermille <= 260 ||
            snapshot.FatiguePermille >= 820)
        {
            return FactionStrategyMode.Rest;
        }

        Span<int> modeScores = stackalloc int[5];
        BuildModeScores(in snapshot, modeScores);

        int bestIndex = AIDecisionSampler.FindBestIndex(modeScores);
        if (bestIndex < 0)
        {
            return FactionStrategyMode.Pressure;
        }

        int bestScore = modeScores[bestIndex];
        int secondScore = int.MinValue;

        for (int i = 0; i < modeScores.Length; i++)
        {
            if (i == bestIndex) continue;
            if (modeScores[i] > secondScore)
            {
                secondScore = modeScores[i];
            }
        }

        if (secondScore == int.MinValue || bestScore - secondScore >= 18)
        {
            return (FactionStrategyMode)bestIndex;
        }

        int tieGap = snapshot.PersonalityProfile.ResolveTieGap();
        int temperaturePermille = snapshot.PersonalityProfile.ResolveTemperaturePermille();
        float random01 = AIDecisionSampler.Sample01(
            snapshot.Tick,
            snapshot.FactionId,
            snapshot.FailureCount,
            701);

        Span<int> candidateBuffer = stackalloc int[5];
        Span<float> weightBuffer = stackalloc float[5];
        int selectedIndex = AIDecisionSampler.SelectNearTieSoftmaxIndex(
            modeScores,
            bestIndex,
            tieGap,
            temperaturePermille,
            random01,
            candidateBuffer,
            weightBuffer);

        return selectedIndex >= 0
            ? (FactionStrategyMode)selectedIndex
            : (FactionStrategyMode)bestIndex;
    }

    private static void BuildModeScores(
        in FactionStrategyAssessmentSnapshot snapshot,
        Span<int> modeScores)
    {
        bool hasPrimaryAssault = snapshot.StrategicFocusArchitectureId >= 0;
        bool hasSecondaryAssault = snapshot.SecondaryAssaultArchitectureId >= 0;
        bool hasDefense = snapshot.DefensiveFocusArchitectureId >= 0;

        int opportunityAdvantage = snapshot.OpportunityPermille - snapshot.ThreatPermille;

        int restScore = 42
            + Math.Clamp((320 - snapshot.EconomicHealthPermille) / 18, -10, 24)
            + Math.Clamp((380 - snapshot.ReadyRatioPermille) / 16, -8, 22)
            + Math.Clamp((snapshot.FatiguePermille - 520) / 16, -6, 20)
            + Math.Clamp((snapshot.ThreatPermille - 550) / 25, -6, 12)
            - Math.Clamp((snapshot.OpportunityPermille - 520) / 28, 0, 10);

        int fortifyScore = 46
            + Math.Clamp((snapshot.ThreatPermille - 520) / 14, -10, 26)
            + Math.Clamp(snapshot.ThreatPressure / 5, 0, 18)
            + Math.Clamp((45 - snapshot.VisibilityConfidence) / 2, 0, 16)
            + Math.Clamp((-snapshot.NetEnergy) / 30, 0, 10)
            + (hasDefense ? 8 : 0);

        int pressureScore = 52
            + Math.Clamp(opportunityAdvantage / 20, -16, 20)
            + Math.Clamp((snapshot.ReadyRatioPermille - 430) / 20, -10, 12)
            + Math.Clamp((snapshot.EconomicHealthPermille - 420) / 22, -10, 12)
            - Math.Clamp(snapshot.FailurePressure / 6, 0, 12);

        int mainAssaultScore = (hasPrimaryAssault ? 50 : 32)
            + Math.Clamp((snapshot.OpportunityPermille - 560) / 12, -20, 24)
            + Math.Clamp((620 - snapshot.ThreatPermille) / 16, -14, 16)
            + Math.Clamp((snapshot.ReadyRatioPermille - 480) / 18, -12, 14)
            + Math.Clamp((snapshot.NetEnergy + 20) / 28, -10, 12)
            + Math.Clamp((snapshot.VisibilityConfidence - 52) / 5, -8, 10)
            - Math.Clamp(snapshot.FailurePressure / 5, 0, 16);

        int diversionScore = (hasSecondaryAssault ? 46 : 30)
            + Math.Clamp((snapshot.OpportunityPermille - 480) / 16, -18, 18)
            + Math.Clamp((680 - snapshot.ThreatPermille) / 20, -12, 10)
            + Math.Clamp((snapshot.VisibilityConfidence - 42) / 4, -8, 12)
            + Math.Clamp((snapshot.ReadyRatioPermille - 420) / 22, -8, 10)
            - Math.Clamp(snapshot.ThreatPressure / 6, 0, 10);

        if (snapshot.EconomicHealthPermille <= 260 ||
            snapshot.ReadyRatioPermille <= 320 ||
            snapshot.FatiguePermille >= 730)
        {
            restScore += 12;
            pressureScore -= 8;
            mainAssaultScore -= 10;
            diversionScore -= 6;
        }

        if (snapshot.ThreatPermille >= 620 || hasDefense && snapshot.VisibilityConfidence < 45)
        {
            fortifyScore += 12;
            mainAssaultScore -= 10;
            diversionScore -= 4;
        }

        int aggressionBias = Math.Clamp((snapshot.PersonalityProfile.AggressionPermille - 1000) / 35, -12, 20);
        int defenseBias = Math.Clamp((snapshot.PersonalityProfile.DefensePermille - 1000) / 35, -12, 20);
        int riskBias = Math.Clamp((snapshot.PersonalityProfile.RiskPermille - 1000) / 40, -10, 18);
        int disciplineBias = Math.Clamp((snapshot.PersonalityProfile.DisciplinePermille - 1000) / 45, -8, 12);
        int volatilityBias = Math.Clamp((snapshot.PersonalityProfile.VolatilityPermille - 900) / 45, -10, 14);

        mainAssaultScore += aggressionBias + riskBias;
        pressureScore += aggressionBias / 2 + riskBias / 2;
        diversionScore += aggressionBias / 3 + volatilityBias;
        fortifyScore += defenseBias + disciplineBias / 2;
        restScore += defenseBias / 2 + Math.Clamp(-riskBias, -6, 12) + disciplineBias / 2;

        modeScores[(int)FactionStrategyMode.Rest] = restScore;
        modeScores[(int)FactionStrategyMode.Fortify] = fortifyScore;
        modeScores[(int)FactionStrategyMode.Pressure] = pressureScore;
        modeScores[(int)FactionStrategyMode.MainAssault] = mainAssaultScore;
        modeScores[(int)FactionStrategyMode.Diversion] = diversionScore;
    }

    private static bool ShouldFreezeAttack(in FactionStrategyAssessmentSnapshot snapshot, FactionStrategyMode mode)
    {
        if (mode == FactionStrategyMode.Rest) return true;
        if (snapshot.EconomicHealthPermille <= 220) return true;
        if (snapshot.ReserveRatioPermille <= 180) return true;
        if (snapshot.FailurePressure >= 88) return true;
        return mode == FactionStrategyMode.Fortify && snapshot.ThreatPermille >= 760;
    }

    private static FactionIntentKind ResolveIntentKind(FactionStrategyMode mode, GameManager.StrategicStance legacyStance, int threatPermille)
    {
        return mode switch
        {
            FactionStrategyMode.Rest => FactionIntentKind.Stabilize,
            FactionStrategyMode.Fortify => threatPermille >= 860 ? FactionIntentKind.Crisis : FactionIntentKind.Defense,
            FactionStrategyMode.MainAssault => FactionIntentKind.Expansion,
            FactionStrategyMode.Diversion => FactionIntentKind.Opportunistic,
            FactionStrategyMode.Pressure => legacyStance == GameManager.StrategicStance.Expansion
                ? FactionIntentKind.Expansion
                : FactionIntentKind.Opportunistic,
            _ => FactionIntentKind.Hold
        };
    }

    private static int ResolveStrategyPressure(in FactionStrategyAssessmentSnapshot snapshot, FactionStrategyMode mode)
    {
        int basePressure = mode switch
        {
            FactionStrategyMode.MainAssault => 72,
            FactionStrategyMode.Diversion => 56,
            FactionStrategyMode.Pressure => 48,
            FactionStrategyMode.Fortify => 42,
            _ => 25
        };

        basePressure += Math.Clamp((snapshot.OpportunityPermille - snapshot.ThreatPermille) / 30, -12, 12);
        basePressure -= Math.Clamp(snapshot.FailurePressure / 6, 0, 12);
        return Math.Clamp(basePressure, 10, 95);
    }

    private static int ResolvePriority(FactionIntentKind kind, int pressure)
    {
        int basePriority = kind switch
        {
            FactionIntentKind.Crisis => 100,
            FactionIntentKind.Defense => 82,
            FactionIntentKind.Expansion => 74,
            FactionIntentKind.Opportunistic => 66,
            FactionIntentKind.Stabilize => 52,
            _ => 18
        };

        return Math.Clamp(basePriority + Math.Clamp((pressure - 50) / 5, -8, 8), 10, 100);
    }

    private static int ResolveCommitTicks(FactionStrategyMode mode, in FactionStrategyAssessmentSnapshot snapshot)
    {
        int ticks = mode switch
        {
            FactionStrategyMode.MainAssault => 3,
            FactionStrategyMode.Rest => 3,
            FactionStrategyMode.Fortify => 2,
            FactionStrategyMode.Diversion => 2,
            _ => 2
        };

        if (snapshot.FailurePressure >= 80) ticks = Math.Min(ticks, 2);
        return ticks;
    }

    private static int ResolveReevaluateTicks(FactionStrategyMode mode, in FactionStrategyAssessmentSnapshot snapshot)
    {
        int ticks = mode switch
        {
            FactionStrategyMode.MainAssault => 2,
            FactionStrategyMode.Diversion => 2,
            FactionStrategyMode.Pressure => 2,
            FactionStrategyMode.Fortify => 1,
            _ => 1
        };

        if (snapshot.ThreatPressure >= 70) ticks = 1;
        return ticks;
    }

    private static int ResolveReserveRatio(FactionStrategyMode mode, in FactionStrategyAssessmentSnapshot snapshot, bool attackFrozen)
    {
        int baseRatio = mode switch
        {
            FactionStrategyMode.MainAssault => 320,
            FactionStrategyMode.Diversion => 480,
            FactionStrategyMode.Pressure => 420,
            FactionStrategyMode.Fortify => 640,
            _ => 700
        };

        baseRatio = Math.Max(baseRatio, snapshot.ReserveRatioPermille);
        if (attackFrozen)
        {
            baseRatio = Math.Max(baseRatio, 620);
        }

        return Math.Clamp(baseRatio, 200, 900);
    }

    private static int ResolveSortieBudget(FactionStrategyMode mode, bool attackFrozen)
    {
        int budget = mode switch
        {
            FactionStrategyMode.MainAssault => 820,
            FactionStrategyMode.Diversion => 560,
            FactionStrategyMode.Pressure => 520,
            FactionStrategyMode.Fortify => 300,
            _ => 220
        };

        if (attackFrozen)
        {
            budget = Math.Min(budget, 260);
        }

        return Math.Clamp(budget, 100, 900);
    }

    private static int ResolveSiegeBudget(FactionStrategyMode mode, bool attackFrozen)
    {
        int budget = mode switch
        {
            FactionStrategyMode.MainAssault => 700,
            FactionStrategyMode.Diversion => 380,
            FactionStrategyMode.Pressure => 420,
            FactionStrategyMode.Fortify => 180,
            _ => 120
        };

        if (attackFrozen)
        {
            budget = Math.Min(budget, 150);
        }

        return Math.Clamp(budget, 80, 800);
    }

    private static int ResolveRiskTolerance(FactionStrategyMode mode, in FactionStrategyAssessmentSnapshot snapshot, bool attackFrozen)
    {
        int baseRisk = mode switch
        {
            FactionStrategyMode.MainAssault => 74,
            FactionStrategyMode.Diversion => 62,
            FactionStrategyMode.Pressure => 58,
            FactionStrategyMode.Fortify => 46,
            _ => 40
        };

        baseRisk += Math.Clamp((snapshot.NetEnergy - 40) / 30, -8, 8);
        baseRisk -= Math.Clamp(snapshot.FailurePressure / 7, 0, 10);

        if (attackFrozen)
        {
            baseRisk = Math.Min(baseRisk, 52);
        }

        return Math.Clamp(baseRisk, 30, 90);
    }

    private static (int architectureId, Point position, int score) ResolvePrimaryFocus(
        in FactionStrategyAssessmentSnapshot snapshot,
        FactionStrategyMode mode)
    {
        if (mode == FactionStrategyMode.Rest || mode == FactionStrategyMode.Fortify)
        {
            if (snapshot.DefensiveFocusArchitectureId >= 0)
            {
                return (
                    snapshot.DefensiveFocusArchitectureId,
                    snapshot.DefensiveFocusPosition,
                    snapshot.DefensiveFocusScore);
            }
        }

        if (snapshot.StrategicFocusArchitectureId >= 0)
        {
            return (
                snapshot.StrategicFocusArchitectureId,
                snapshot.StrategicFocusPosition,
                snapshot.StrategicFocusScore);
        }

        return (
            snapshot.DefensiveFocusArchitectureId,
            snapshot.DefensiveFocusPosition,
            snapshot.DefensiveFocusScore);
    }

    private static (int architectureId, Point position, int score) ResolveSecondaryFocus(
        in FactionStrategyAssessmentSnapshot snapshot,
        FactionStrategyMode mode,
        int primaryArchitectureId)
    {
        if (mode == FactionStrategyMode.MainAssault || mode == FactionStrategyMode.Diversion || mode == FactionStrategyMode.Pressure)
        {
            if (snapshot.SecondaryAssaultArchitectureId >= 0 &&
                snapshot.SecondaryAssaultArchitectureId != primaryArchitectureId)
            {
                return (
                    snapshot.SecondaryAssaultArchitectureId,
                    snapshot.SecondaryAssaultPosition,
                    snapshot.SecondaryAssaultScore);
            }
        }

        return (
            snapshot.DefensiveFocusArchitectureId,
            snapshot.DefensiveFocusPosition,
            snapshot.DefensiveFocusScore);
    }
}
