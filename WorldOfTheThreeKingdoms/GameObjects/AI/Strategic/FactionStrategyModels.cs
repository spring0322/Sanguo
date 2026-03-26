using GameManager;
using Microsoft.Xna.Framework;
using System;

namespace GameObjects.AI;

public enum FactionStrategyMode : byte
{
    Rest = 0,
    Fortify = 1,
    Pressure = 2,
    MainAssault = 3,
    Diversion = 4
}

public enum LegionFrontRole : byte
{
    Reserve = 0,
    MainAssault = 1,
    Diversion = 2,
    Defense = 3,
    Support = 4
}

public readonly record struct FactionDecisionPersonalityProfile(
    int AggressionPermille,
    int DefensePermille,
    int RiskPermille,
    int DisciplinePermille,
    int VolatilityPermille)
{
    public static readonly FactionDecisionPersonalityProfile Balanced = new(1000, 1000, 1000, 1000, 900);

    public int ResolveTieGap()
    {
        return Math.Clamp(8 + VolatilityPermille / 180, 8, 20);
    }

    public int ResolveTemperaturePermille()
    {
        int temperature = 820 + (VolatilityPermille - 900) - (DisciplinePermille - 1000) / 2;
        return Math.Clamp(temperature, 450, 1800);
    }
}

public readonly record struct FactionStrategyAssessmentSnapshot(
    int Tick,
    int FactionId,
    StrategicStance LegacyStance,
    FactionDecisionPersonalityProfile PersonalityProfile,
    int ArchitectureCount,
    int TotalTroopCount,
    int ReadyTroopCount,
    int ReserveTroopCount,
    int EconomicHealthPermille,
    int FatiguePermille,
    int ThreatPermille,
    int OpportunityPermille,
    int VisibilityConfidence,
    int NetEnergy,
    int StrategicFocusArchitectureId,
    Point StrategicFocusPosition,
    int StrategicFocusScore,
    int DefensiveFocusArchitectureId,
    Point DefensiveFocusPosition,
    int DefensiveFocusScore,
    int SecondaryAssaultArchitectureId,
    Point SecondaryAssaultPosition,
    int SecondaryAssaultScore,
    int FailurePressure,
    int ThreatPressure,
    int BlockedPressure,
    int FailureCount)
{
    public int ReadyRatioPermille =>
        TotalTroopCount > 0
            ? Math.Clamp(ReadyTroopCount * 1000 / TotalTroopCount, 0, 1000)
            : 0;

    public int ReserveRatioPermille =>
        TotalTroopCount > 0
            ? Math.Clamp(ReserveTroopCount * 1000 / TotalTroopCount, 0, 1000)
            : 1000;
}

public readonly record struct FactionStrategyDirective(
    FactionStrategyMode Mode,
    FactionIntentKind IntentKind,
    int Priority,
    int CommitUntilTick,
    int ReevaluateAfterTick,
    int PrimaryArchitectureId,
    Point PrimaryPosition,
    int PrimaryScore,
    int SecondaryArchitectureId,
    Point SecondaryPosition,
    int SecondaryScore,
    int DefensiveArchitectureId,
    Point DefensivePosition,
    int DefensiveScore,
    int ReserveRatioPermille,
    int SortieBudgetPermille,
    int SiegeBudgetPermille,
    int RiskTolerance,
    bool AttackFrozen,
    int StrategyPressure,
    int FailurePressure,
    int ThreatPressure,
    int PersonalityAggressionPermille,
    int PersonalityDefensePermille,
    int PersonalityRiskPermille,
    int PersonalityDisciplinePermille,
    int PersonalityVolatilityPermille)
{
    public static FactionStrategyDirective CreateHold(int tick, Point fallbackPosition)
    {
        return new FactionStrategyDirective(
            FactionStrategyMode.Rest,
            FactionIntentKind.Hold,
            10,
            tick + 1,
            tick + 1,
            -1,
            fallbackPosition,
            0,
            -1,
            fallbackPosition,
            0,
            -1,
            fallbackPosition,
            0,
            700,
            200,
            100,
            40,
            true,
            0,
            0,
            0,
            1000,
            1000,
            1000,
            1000,
            900);
    }
}

public readonly record struct FactionStrategicAnalysis(
    StrategicStance LegacyStance,
    FactionStrategyDirective Directive);

public sealed class FactionStrategicTelemetry
{
    private const int FailureDecayPerTick = 5;
    private const int ThreatDecayPerTick = 6;
    private const int BlockDecayPerTick = 4;

    public int LastUpdatedTick { get; private set; } = -1;
    public int FailurePressure { get; private set; }
    public int ThreatPressure { get; private set; }
    public int BlockedPressure { get; private set; }
    public int FailureCount { get; private set; }

    public void ApplyDecay(int currentTick)
    {
        if (currentTick < 0) return;
        if (LastUpdatedTick < 0)
        {
            LastUpdatedTick = currentTick;
            return;
        }

        if (currentTick <= LastUpdatedTick) return;

        int delta = currentTick - LastUpdatedTick;
        LastUpdatedTick = currentTick;

        FailurePressure = Math.Max(0, FailurePressure - delta * FailureDecayPerTick);
        ThreatPressure = Math.Max(0, ThreatPressure - delta * ThreatDecayPerTick);
        BlockedPressure = Math.Max(0, BlockedPressure - delta * BlockDecayPerTick);
        FailureCount = Math.Max(0, FailureCount - delta);
    }

    public void RegisterFailure(TroopIntentFailureReason reason)
    {
        FailureCount = Math.Min(999, FailureCount + 1);

        int failureDelta = 4;
        int threatDelta = 0;
        int blockedDelta = 0;

        switch (reason)
        {
            case TroopIntentFailureReason.ThreatSpike:
                failureDelta = 10;
                threatDelta = 16;
                break;

            case TroopIntentFailureReason.PathBlocked:
            case TroopIntentFailureReason.ArbitrationLost:
                failureDelta = 8;
                blockedDelta = 12;
                break;

            case TroopIntentFailureReason.VisibilityLost:
            case TroopIntentFailureReason.TargetLost:
            case TroopIntentFailureReason.TargetOwnerChanged:
                failureDelta = 6;
                break;

            case TroopIntentFailureReason.InvalidDestination:
                failureDelta = 7;
                blockedDelta = 6;
                break;
        }

        FailurePressure = Math.Clamp(FailurePressure + failureDelta, 0, 100);
        ThreatPressure = Math.Clamp(ThreatPressure + threatDelta, 0, 100);
        BlockedPressure = Math.Clamp(BlockedPressure + blockedDelta, 0, 100);
    }
}
