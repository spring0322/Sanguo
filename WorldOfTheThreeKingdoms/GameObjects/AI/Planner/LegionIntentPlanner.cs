using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed class LegionIntentPlanner
{
    private readonly Dictionary<int, AttackFoodReservation> _attackReservationsByLegion = new();
    private readonly Dictionary<int, int> _lockedFoodByArchitecture = new();

    public LegionIntent BuildLegionIntent(GameScenario scenario, Legion legion, FactionIntent factionIntent, int issuedTick, int version)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (legion == null) throw new ArgumentNullException(nameof(legion));
        if (factionIntent == null) throw new ArgumentNullException(nameof(factionIntent));

        ReleaseAttackLock(legion.ID);

        FactionDecisionPersonalityProfile personalityProfile = FactionDecisionPersonalityResolver.Resolve(in factionIntent);
        LegionFrontRole frontRole = ResolveFrontRole(legion, factionIntent, issuedTick, personalityProfile);
        LegionIntentKind kind = ResolveStrategicKind(legion, factionIntent, frontRole);
        bool attackAuthorized = ResolveAttackAuthorization(kind, frontRole, factionIntent);
        Architecture sourceArchitecture = AssaultSortiePrecheckService.ResolveSourceArchitecture(legion);

        IntentTargetRef target = IntentTargetRef.None;
        TheaterTargetScore targetScore = TheaterTargetScore.None;

        if (!TryResolveStrategicTarget(
                scenario,
                legion,
                factionIntent,
                frontRole,
                issuedTick,
                personalityProfile,
                sourceArchitecture,
                out target,
                out targetScore))
        {
            target = TheaterPlanningService.ResolveLegionTarget(
                scenario,
                legion,
                factionIntent,
                out targetScore);
        }

        if (target.Kind == IntentTargetKind.None)
        {
            target = ResolveTargetFallback(scenario, legion, factionIntent, frontRole);
            targetScore = target.Kind == IntentTargetKind.Architecture
                ? TheaterPlanningService.EvaluateLegionTarget(
                    scenario,
                    legion,
                    target.TargetId,
                    ResolveFocusMode(frontRole, legion.Mission),
                    factionIntent.Kind)
                : TheaterTargetScore.None;
        }

        int logisticsPriorityBias = 0;
        if (ShouldEvaluateAssaultGate(legion, kind, frontRole, target))
        {
            AssaultSortiePrecheckResult sortieGate = EvaluateAssaultGate(
                scenario,
                sourceArchitecture,
                target,
                factionIntent);

            if (!sortieGate.Passed)
            {
                attackAuthorized = false;
                frontRole = LegionFrontRole.Reserve;
                kind = LegionIntentKind.Recover;
                targetScore = TheaterTargetScore.None;
                target = ResolveRecoveryTarget(sourceArchitecture, factionIntent.SourceFactionId);
            }
            else if (attackAuthorized)
            {
                AcquireAttackLock(legion.ID, sortieGate.SourceArchitectureId, sortieGate.RequiredFoodLock);
                Architecture targetArchitecture = scenario.Architectures.GetGameObject(target.TargetId) as Architecture;
                logisticsPriorityBias = sortieGate.ResolveScoreBias(targetArchitecture?.Morale ?? 0);
            }
        }

        int priority = Math.Max(ResolvePriority(kind), factionIntent.Priority - 5);
        priority += ResolvePriorityBias(targetScore);
        priority += ResolveFrontRolePriorityBias(frontRole);
        priority += logisticsPriorityBias;
        if (!attackAuthorized)
        {
            priority -= 6;
        }

        priority = Math.Max(1, priority);

        int commitUntilTick = Math.Max(
            issuedTick + ResolveCommitTicks(kind),
            factionIntent.StrategyCommitUntilTick);

        return new LegionIntent(
            new LegionIntentId(legion.ID, version, issuedTick),
            kind,
            target,
            priority,
            commitUntilTick,
            legion.BelongedFaction?.ID ?? factionIntent.SourceFactionId,
            legion.ID,
            factionIntent.Id,
            targetScore.IsValid ? targetScore.FocusMode : ResolveFocusMode(frontRole, legion.Mission),
            targetScore.IsValid ? targetScore.Position : Point.Zero,
            targetScore.IsValid ? targetScore.Score : 0,
            targetScore.IsValid ? targetScore.TravelCost : 0,
            targetScore.IsValid ? targetScore.VisibilityConfidence : factionIntent.TheaterVisibilityConfidence,
            targetScore.IsValid ? targetScore.NetEnergy : factionIntent.TheaterEnergyBalance,
            factionIntent.StrategyMode,
            frontRole,
            factionIntent.StrategyRiskTolerance,
            attackAuthorized,
            factionIntent.ReserveRatioPermille);
    }

    public void ResetPlanningState()
    {
        _attackReservationsByLegion.Clear();
        _lockedFoodByArchitecture.Clear();
    }

    private bool TryResolveStrategicTarget(
        GameScenario scenario,
        Legion legion,
        FactionIntent factionIntent,
        LegionFrontRole frontRole,
        int issuedTick,
        in FactionDecisionPersonalityProfile personalityProfile,
        Architecture sourceArchitecture,
        out IntentTargetRef target,
        out TheaterTargetScore targetScore)
    {
        target = IntentTargetRef.None;
        targetScore = TheaterTargetScore.None;

        TheaterFocusMode focusMode = ResolveFocusMode(frontRole, legion.Mission);
        int expectedTargetId = legion.WillArchitecture?.ID ?? legion.Target?.ID ?? -1;

        Span<int> candidateIds = stackalloc int[6];
        Span<int> candidateScores = stackalloc int[6];
        Span<TheaterTargetScore> candidateBaseScores = stackalloc TheaterTargetScore[6];
        int candidateCount = 0;

        TryAddCandidateArchitecture(candidateIds, ref candidateCount, ResolvePreferredArchitectureId(frontRole, factionIntent));
        TryAddCandidateArchitecture(candidateIds, ref candidateCount, factionIntent.StrategicFocusArchitectureId);
        TryAddCandidateArchitecture(candidateIds, ref candidateCount, factionIntent.SecondaryFocusArchitectureId);
        TryAddCandidateArchitecture(candidateIds, ref candidateCount, factionIntent.DefensiveFocusArchitectureId);
        TryAddCandidateArchitecture(candidateIds, ref candidateCount, legion.WillArchitecture?.ID ?? -1);
        TryAddCandidateArchitecture(candidateIds, ref candidateCount, legion.Target?.ID ?? -1);

        int validCount = 0;
        for (int i = 0; i < candidateCount; i++)
        {
            int architectureId = candidateIds[i];
            Architecture targetArchitecture = scenario.Architectures.GetGameObject(architectureId) as Architecture;
            if (targetArchitecture == null)
            {
                continue;
            }

            TheaterTargetScore baseScore = TheaterPlanningService.EvaluateLegionTarget(
                scenario,
                legion,
                architectureId,
                focusMode,
                factionIntent.Kind);
            if (!baseScore.IsValid)
            {
                continue;
            }

            int adjustedScore = baseScore.Score;
            adjustedScore += ResolveFrontRoleTargetBias(frontRole, architectureId, in factionIntent);
            adjustedScore += ResolveTargetPersonalityBias(frontRole, in personalityProfile, in baseScore);

            if (focusMode == TheaterFocusMode.Assault && targetArchitecture.BelongedFaction != legion.BelongedFaction)
            {
                if (sourceArchitecture == null)
                {
                    continue;
                }

                AssaultSortiePrecheckResult sortieGate = AssaultSortiePrecheckService.EvaluateOffensiveSortie(
                    sourceArchitecture,
                    targetArchitecture,
                    factionIntent.SortieBudgetPermille,
                    GetLockedFood(sourceArchitecture.ID));

                if (!sortieGate.Passed)
                {
                    continue;
                }

                adjustedScore += sortieGate.ResolveScoreBias(targetArchitecture.Morale);
            }

            if (expectedTargetId >= 0 && architectureId == expectedTargetId)
            {
                adjustedScore += 6;
            }

            candidateIds[validCount] = architectureId;
            candidateScores[validCount] = adjustedScore;
            candidateBaseScores[validCount] = baseScore;
            validCount++;
        }

        if (validCount <= 0)
        {
            return false;
        }

        int bestIndex = 0;
        for (int i = 1; i < validCount; i++)
        {
            if (candidateScores[i] > candidateScores[bestIndex])
            {
                bestIndex = i;
            }
        }

        int bestScore = candidateScores[bestIndex];
        int secondScore = int.MinValue;
        for (int i = 0; i < validCount; i++)
        {
            if (i == bestIndex) continue;
            if (candidateScores[i] > secondScore)
            {
                secondScore = candidateScores[i];
            }
        }

        int selectedIndex = bestIndex;
        if (secondScore != int.MinValue && bestScore - secondScore < 14)
        {
            Span<int> allScores = stackalloc int[validCount];
            for (int i = 0; i < validCount; i++)
            {
                allScores[i] = candidateScores[i];
            }

            int tieGap = ResolveTargetTieGap(personalityProfile);
            int temperaturePermille = ResolveTargetTemperaturePermille(personalityProfile);
            float random01 = AIDecisionSampler.Sample01(
                issuedTick,
                factionIntent.SourceFactionId,
                legion.ID,
                821);

            Span<int> sampleIndices = stackalloc int[6];
            Span<float> sampleWeights = stackalloc float[6];
            int sampled = AIDecisionSampler.SelectNearTieSoftmaxIndex(
                allScores,
                bestIndex,
                tieGap,
                temperaturePermille,
                random01,
                sampleIndices,
                sampleWeights);
            if (sampled >= 0 && sampled < validCount)
            {
                selectedIndex = sampled;
            }
        }

        targetScore = candidateBaseScores[selectedIndex] with { Score = candidateScores[selectedIndex] };
        Architecture architecture = scenario.Architectures.GetGameObject(candidateIds[selectedIndex]) as Architecture;
        if (architecture == null)
        {
            targetScore = TheaterTargetScore.None;
            return false;
        }

        target = IntentTargetRef.ForArchitecture(
            architecture.ID,
            architecture.Position,
            architecture.BelongedFaction?.ID ?? -1);
        return true;
    }

    private static LegionIntentKind MapKind(LegionMission mission)
    {
        return mission switch
        {
            LegionMission.Attack => LegionIntentKind.Assault,
            LegionMission.Defend => LegionIntentKind.Defend,
            LegionMission.Retreat => LegionIntentKind.Withdraw,
            LegionMission.Patrol => LegionIntentKind.Patrol,
            _ => LegionIntentKind.Hold
        };
    }

    private static LegionIntentKind ResolveStrategicKind(Legion legion, FactionIntent factionIntent, LegionFrontRole frontRole)
    {
        if (frontRole == LegionFrontRole.Reserve)
        {
            if (legion.Mission == LegionMission.Retreat) return LegionIntentKind.Withdraw;
            return factionIntent.StrategyMode == FactionStrategyMode.Rest
                ? LegionIntentKind.Recover
                : LegionIntentKind.Hold;
        }

        if (frontRole == LegionFrontRole.Defense)
        {
            return legion.Mission == LegionMission.Retreat
                ? LegionIntentKind.Withdraw
                : LegionIntentKind.Defend;
        }

        if (frontRole == LegionFrontRole.MainAssault)
        {
            return factionIntent.AttackFrozen
                ? LegionIntentKind.Patrol
                : LegionIntentKind.Assault;
        }

        if (frontRole == LegionFrontRole.Diversion)
        {
            return factionIntent.AttackFrozen
                ? LegionIntentKind.Patrol
                : LegionIntentKind.Patrol;
        }

        return factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Rest => LegionIntentKind.Recover,
            FactionStrategyMode.Fortify => LegionIntentKind.Defend,
            _ => MapKind(legion.Mission)
        };
    }

    private static LegionFrontRole ResolveFrontRole(
        Legion legion,
        FactionIntent factionIntent,
        int issuedTick,
        in FactionDecisionPersonalityProfile personalityProfile)
    {
        if (legion.Mission == LegionMission.Retreat)
        {
            return LegionFrontRole.Reserve;
        }

        if (legion.Mission == LegionMission.Defend)
        {
            return LegionFrontRole.Defense;
        }

        if (factionIntent.AttackFrozen)
        {
            return legion.Mission == LegionMission.Attack
                ? LegionFrontRole.Reserve
                : LegionFrontRole.Defense;
        }

        if (legion.Mission == LegionMission.Attack)
        {
            int legionExpectedTargetId = legion.WillArchitecture?.ID ?? legion.Target?.ID ?? -1;
            if (legionExpectedTargetId >= 0)
            {
                if (legionExpectedTargetId == factionIntent.StrategicFocusArchitectureId)
                {
                    return LegionFrontRole.MainAssault;
                }

                if (legionExpectedTargetId == factionIntent.SecondaryFocusArchitectureId)
                {
                    return LegionFrontRole.Diversion;
                }

                if (legionExpectedTargetId == factionIntent.DefensiveFocusArchitectureId)
                {
                    return LegionFrontRole.Defense;
                }
            }

            return ResolveProbabilisticFrontRoleForAttack(
                legion,
                factionIntent,
                issuedTick,
                personalityProfile);
        }

        if (legion.Mission == LegionMission.Patrol)
        {
            return ResolveProbabilisticFrontRoleForPatrol(
                legion,
                factionIntent,
                issuedTick,
                personalityProfile);
        }

        return LegionFrontRole.Support;
    }

    private static LegionFrontRole ResolveProbabilisticFrontRoleForAttack(
        Legion legion,
        FactionIntent factionIntent,
        int issuedTick,
        in FactionDecisionPersonalityProfile personalityProfile)
    {
        Span<int> roleScores = stackalloc int[5];
        for (int i = 0; i < roleScores.Length; i++)
        {
            roleScores[i] = int.MinValue;
        }

        int aggressionBias = Math.Clamp((personalityProfile.AggressionPermille - 1000) / 40, -10, 16);
        int defenseBias = Math.Clamp((personalityProfile.DefensePermille - 1000) / 40, -10, 16);
        int riskBias = Math.Clamp((personalityProfile.RiskPermille - 1000) / 45, -8, 14);
        int volatilityBias = Math.Clamp((personalityProfile.VolatilityPermille - 900) / 50, -8, 12);

        int mainScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.MainAssault => 88,
            FactionStrategyMode.Pressure => 76,
            FactionStrategyMode.Diversion => 68,
            FactionStrategyMode.Fortify => 38,
            FactionStrategyMode.Rest => 22,
            _ => 72
        };
        mainScore += aggressionBias + riskBias;

        int diversionScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Diversion => 82,
            FactionStrategyMode.MainAssault => 72,
            FactionStrategyMode.Pressure => 66,
            FactionStrategyMode.Fortify => 36,
            _ => 60
        };
        diversionScore += aggressionBias / 2 + volatilityBias;

        int defenseScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Fortify => 74,
            FactionStrategyMode.Rest => 64,
            _ => 44
        };
        defenseScore += defenseBias - Math.Clamp(riskBias / 2, -4, 6);

        int supportScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Rest => 68,
            FactionStrategyMode.Fortify => 56,
            _ => 52
        };
        supportScore += Math.Clamp((personalityProfile.DisciplinePermille - 1000) / 55, -6, 10);

        roleScores[(int)LegionFrontRole.MainAssault] = mainScore;
        roleScores[(int)LegionFrontRole.Diversion] = diversionScore;
        roleScores[(int)LegionFrontRole.Defense] = defenseScore;
        roleScores[(int)LegionFrontRole.Support] = supportScore;

        int bestIndex = AIDecisionSampler.FindBestIndex(roleScores);
        if (bestIndex < 0)
        {
            return LegionFrontRole.MainAssault;
        }

        int bestScore = roleScores[bestIndex];
        int secondScore = int.MinValue;
        for (int i = 0; i < roleScores.Length; i++)
        {
            if (i == bestIndex || roleScores[i] == int.MinValue) continue;
            if (roleScores[i] > secondScore)
            {
                secondScore = roleScores[i];
            }
        }

        if (secondScore == int.MinValue || bestScore - secondScore >= 14)
        {
            return (LegionFrontRole)bestIndex;
        }

        float random01 = AIDecisionSampler.Sample01(
            issuedTick,
            factionIntent.SourceFactionId,
            legion.ID,
            811);
        Span<int> candidateBuffer = stackalloc int[5];
        Span<float> weightBuffer = stackalloc float[5];
        int selectedIndex = AIDecisionSampler.SelectNearTieSoftmaxIndex(
            roleScores,
            bestIndex,
            personalityProfile.ResolveTieGap(),
            personalityProfile.ResolveTemperaturePermille(),
            random01,
            candidateBuffer,
            weightBuffer);

        return selectedIndex >= 0
            ? (LegionFrontRole)selectedIndex
            : (LegionFrontRole)bestIndex;
    }

    private static LegionFrontRole ResolveProbabilisticFrontRoleForPatrol(
        Legion legion,
        FactionIntent factionIntent,
        int issuedTick,
        in FactionDecisionPersonalityProfile personalityProfile)
    {
        Span<int> roleScores = stackalloc int[5];
        for (int i = 0; i < roleScores.Length; i++)
        {
            roleScores[i] = int.MinValue;
        }

        int aggressionBias = Math.Clamp((personalityProfile.AggressionPermille - 1000) / 45, -8, 12);
        int defenseBias = Math.Clamp((personalityProfile.DefensePermille - 1000) / 45, -8, 12);
        int disciplineBias = Math.Clamp((personalityProfile.DisciplinePermille - 1000) / 55, -6, 10);
        int volatilityBias = Math.Clamp((personalityProfile.VolatilityPermille - 900) / 55, -6, 10);

        int supportScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Pressure => 68,
            FactionStrategyMode.MainAssault => 64,
            _ => 58
        };
        supportScore += disciplineBias;

        int diversionScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Diversion => 72,
            FactionStrategyMode.MainAssault => 64,
            _ => 48
        };
        diversionScore += aggressionBias / 2 + volatilityBias;

        int defenseScore = factionIntent.StrategyMode switch
        {
            FactionStrategyMode.Fortify => 74,
            FactionStrategyMode.Rest => 70,
            _ => 52
        };
        defenseScore += defenseBias;

        int reserveScore = factionIntent.StrategyMode == FactionStrategyMode.Rest ? 68 : 42;
        reserveScore += defenseBias / 2 - aggressionBias / 2;

        roleScores[(int)LegionFrontRole.Support] = supportScore;
        roleScores[(int)LegionFrontRole.Diversion] = diversionScore;
        roleScores[(int)LegionFrontRole.Defense] = defenseScore;
        roleScores[(int)LegionFrontRole.Reserve] = reserveScore;

        int bestIndex = AIDecisionSampler.FindBestIndex(roleScores);
        if (bestIndex < 0)
        {
            return LegionFrontRole.Support;
        }

        int bestScore = roleScores[bestIndex];
        int secondScore = int.MinValue;
        for (int i = 0; i < roleScores.Length; i++)
        {
            if (i == bestIndex || roleScores[i] == int.MinValue) continue;
            if (roleScores[i] > secondScore)
            {
                secondScore = roleScores[i];
            }
        }

        if (secondScore == int.MinValue || bestScore - secondScore >= 12)
        {
            return (LegionFrontRole)bestIndex;
        }

        float random01 = AIDecisionSampler.Sample01(
            issuedTick,
            factionIntent.SourceFactionId,
            legion.ID,
            812);
        Span<int> candidateBuffer = stackalloc int[5];
        Span<float> weightBuffer = stackalloc float[5];
        int selectedIndex = AIDecisionSampler.SelectNearTieSoftmaxIndex(
            roleScores,
            bestIndex,
            Math.Max(8, personalityProfile.ResolveTieGap() - 2),
            personalityProfile.ResolveTemperaturePermille(),
            random01,
            candidateBuffer,
            weightBuffer);

        return selectedIndex >= 0
            ? (LegionFrontRole)selectedIndex
            : (LegionFrontRole)bestIndex;
    }

    private static int ResolveTargetTieGap(in FactionDecisionPersonalityProfile personalityProfile)
    {
        return Math.Clamp(6 + personalityProfile.VolatilityPermille / 220, 6, 16);
    }

    private static int ResolveTargetTemperaturePermille(in FactionDecisionPersonalityProfile personalityProfile)
    {
        int temperature = personalityProfile.ResolveTemperaturePermille() - 120;
        return Math.Clamp(temperature, 420, 1650);
    }

    private static int ResolveFrontRoleTargetBias(
        LegionFrontRole frontRole,
        int architectureId,
        in FactionIntent factionIntent)
    {
        int primaryId = factionIntent.StrategicFocusArchitectureId;
        int secondaryId = factionIntent.SecondaryFocusArchitectureId;
        int defensiveId = factionIntent.DefensiveFocusArchitectureId;

        return frontRole switch
        {
            LegionFrontRole.MainAssault => architectureId == primaryId ? 10 :
                architectureId == secondaryId ? 5 :
                architectureId == defensiveId ? -6 : 0,
            LegionFrontRole.Diversion => architectureId == secondaryId ? 10 :
                architectureId == primaryId ? 4 :
                architectureId == defensiveId ? -5 : 0,
            LegionFrontRole.Defense => architectureId == defensiveId ? 10 :
                architectureId == primaryId ? -4 : 0,
            LegionFrontRole.Reserve => architectureId == defensiveId ? 8 :
                architectureId == primaryId ? -5 : 0,
            _ => architectureId == primaryId ? 4 : 0
        };
    }

    private static int ResolveTargetPersonalityBias(
        LegionFrontRole frontRole,
        in FactionDecisionPersonalityProfile personalityProfile,
        in TheaterTargetScore baseScore)
    {
        int aggressionBias = Math.Clamp((personalityProfile.AggressionPermille - 1000) / 45, -8, 12);
        int defenseBias = Math.Clamp((personalityProfile.DefensePermille - 1000) / 45, -8, 12);
        int riskBias = Math.Clamp((personalityProfile.RiskPermille - 1000) / 50, -8, 10);

        int bias = frontRole switch
        {
            LegionFrontRole.MainAssault => aggressionBias + riskBias,
            LegionFrontRole.Diversion => aggressionBias / 2 + Math.Clamp((personalityProfile.VolatilityPermille - 900) / 60, -6, 9),
            LegionFrontRole.Defense => defenseBias - riskBias / 2,
            LegionFrontRole.Reserve => defenseBias / 2 - riskBias / 2,
            _ => 0
        };

        if (baseScore.TravelCost > 12 && frontRole is LegionFrontRole.Defense or LegionFrontRole.Reserve)
        {
            bias -= 3;
        }

        return bias;
    }

    private static void TryAddCandidateArchitecture(Span<int> candidateIds, ref int count, int architectureId)
    {
        if (architectureId < 0 || count >= candidateIds.Length)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (candidateIds[i] == architectureId)
            {
                return;
            }
        }

        candidateIds[count] = architectureId;
        count++;
    }

    private static bool ResolveAttackAuthorization(
        LegionIntentKind kind,
        LegionFrontRole frontRole,
        FactionIntent factionIntent)
    {
        if (factionIntent.AttackFrozen)
        {
            return false;
        }

        return frontRole == LegionFrontRole.MainAssault ||
               frontRole == LegionFrontRole.Diversion ||
               kind == LegionIntentKind.Assault;
    }

    private static IntentTargetRef ResolveTargetFallback(
        GameScenario scenario,
        Legion legion,
        FactionIntent factionIntent,
        LegionFrontRole frontRole)
    {
        if (legion.WillArchitecture != null)
        {
            return IntentTargetRef.ForArchitecture(
                legion.WillArchitecture.ID,
                legion.WillArchitecture.Position,
                legion.WillArchitecture.BelongedFaction?.ID ?? -1);
        }

        if (legion.Target != null)
        {
            return IntentTargetRef.ForArchitecture(
                legion.Target.ID,
                legion.Target.Position,
                legion.Target.BelongedFaction?.ID ?? -1);
        }

        int fallbackArchitectureId = ResolvePreferredArchitectureId(frontRole, factionIntent);
        if (fallbackArchitectureId < 0)
        {
            fallbackArchitectureId = legion.Mission is LegionMission.Defend or LegionMission.Retreat
                ? factionIntent.DefensiveFocusArchitectureId
                : factionIntent.StrategicFocusArchitectureId;
        }

        if (fallbackArchitectureId >= 0)
        {
            Architecture fallbackArchitecture = scenario.Architectures.GetGameObject(fallbackArchitectureId) as Architecture;
            if (fallbackArchitecture != null)
            {
                return IntentTargetRef.ForArchitecture(
                    fallbackArchitecture.ID,
                    fallbackArchitecture.Position,
                    fallbackArchitecture.BelongedFaction?.ID ?? -1);
            }
        }

        return IntentTargetRef.None;
    }

    private static int ResolvePreferredArchitectureId(LegionFrontRole frontRole, FactionIntent factionIntent)
    {
        return frontRole switch
        {
            LegionFrontRole.MainAssault => factionIntent.StrategicFocusArchitectureId,
            LegionFrontRole.Diversion => factionIntent.SecondaryFocusArchitectureId >= 0
                ? factionIntent.SecondaryFocusArchitectureId
                : factionIntent.StrategicFocusArchitectureId,
            LegionFrontRole.Defense => factionIntent.DefensiveFocusArchitectureId,
            LegionFrontRole.Reserve => factionIntent.DefensiveFocusArchitectureId,
            _ => factionIntent.StrategicFocusArchitectureId
        };
    }

    private static int ResolvePriority(LegionIntentKind kind)
    {
        return kind switch
        {
            LegionIntentKind.Withdraw => 100,
            LegionIntentKind.Defend => 82,
            LegionIntentKind.Assault => 74,
            LegionIntentKind.Patrol => 46,
            LegionIntentKind.Recover => 36,
            _ => 10
        };
    }

    private static int ResolveCommitTicks(LegionIntentKind kind)
    {
        return kind switch
        {
            LegionIntentKind.Withdraw => 1,
            LegionIntentKind.Defend => 2,
            LegionIntentKind.Assault => 2,
            LegionIntentKind.Patrol => 1,
            LegionIntentKind.Recover => 2,
            _ => 1
        };
    }

    private static TheaterFocusMode ResolveFocusMode(LegionFrontRole frontRole, LegionMission mission)
    {
        return frontRole switch
        {
            LegionFrontRole.MainAssault => TheaterFocusMode.Assault,
            LegionFrontRole.Diversion => TheaterFocusMode.Assault,
            LegionFrontRole.Defense => TheaterFocusMode.Defend,
            LegionFrontRole.Reserve => TheaterFocusMode.Recover,
            _ => MapMissionToFocusMode(mission)
        };
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

    private static int ResolvePriorityBias(in TheaterTargetScore targetScore)
    {
        if (!targetScore.IsValid) return 0;

        int energyBias = targetScore.NetEnergy > 0 ? 2 : -2;
        int visibilityBias = targetScore.VisibilityConfidence >= 60 ? 2 : -2;
        int travelPenalty = targetScore.TravelCost > 12 ? -3 : 0;
        return energyBias + visibilityBias + travelPenalty;
    }

    private static int ResolveFrontRolePriorityBias(LegionFrontRole frontRole)
    {
        return frontRole switch
        {
            LegionFrontRole.MainAssault => 8,
            LegionFrontRole.Defense => 5,
            LegionFrontRole.Diversion => 3,
            LegionFrontRole.Reserve => -6,
            _ => 0
        };
    }

    private AssaultSortiePrecheckResult EvaluateAssaultGate(
        GameScenario scenario,
        Architecture sourceArchitecture,
        IntentTargetRef target,
        FactionIntent factionIntent)
    {
        if (sourceArchitecture == null)
        {
            return AssaultSortiePrecheckService.EvaluateOffensiveSortie(null, null, factionIntent.SortieBudgetPermille, 0);
        }

        Architecture targetArchitecture = scenario.Architectures.GetGameObject(target.TargetId) as Architecture;
        return AssaultSortiePrecheckService.EvaluateOffensiveSortie(
            sourceArchitecture,
            targetArchitecture,
            factionIntent.SortieBudgetPermille,
            GetLockedFood(sourceArchitecture.ID));
    }

    private void ReleaseAttackLock(int legionId)
    {
        if (!_attackReservationsByLegion.TryGetValue(legionId, out AttackFoodReservation reservation))
        {
            return;
        }

        _attackReservationsByLegion.Remove(legionId);
        if (!_lockedFoodByArchitecture.TryGetValue(reservation.SourceArchitectureId, out int currentLockedFood))
        {
            return;
        }

        int remainingLockedFood = currentLockedFood - reservation.ReservedFood;
        if (remainingLockedFood > 0)
        {
            _lockedFoodByArchitecture[reservation.SourceArchitectureId] = remainingLockedFood;
        }
        else
        {
            _lockedFoodByArchitecture.Remove(reservation.SourceArchitectureId);
        }
    }

    private void AcquireAttackLock(int legionId, int sourceArchitectureId, int reservedFood)
    {
        if (sourceArchitectureId < 0 || reservedFood <= 0)
        {
            return;
        }

        _attackReservationsByLegion[legionId] = new AttackFoodReservation(sourceArchitectureId, reservedFood);
        if (_lockedFoodByArchitecture.TryGetValue(sourceArchitectureId, out int currentLockedFood))
        {
            _lockedFoodByArchitecture[sourceArchitectureId] = currentLockedFood + reservedFood;
        }
        else
        {
            _lockedFoodByArchitecture[sourceArchitectureId] = reservedFood;
        }
    }

    private int GetLockedFood(int sourceArchitectureId)
    {
        if (sourceArchitectureId < 0)
        {
            return 0;
        }

        return _lockedFoodByArchitecture.TryGetValue(sourceArchitectureId, out int lockedFood)
            ? lockedFood
            : 0;
    }

    private static bool ShouldEvaluateAssaultGate(
        Legion legion,
        LegionIntentKind kind,
        LegionFrontRole frontRole,
        IntentTargetRef target)
    {
        if (target.Kind != IntentTargetKind.Architecture || target.TargetId < 0)
        {
            return false;
        }

        if (target.OwnerFactionId == legion.BelongedFaction?.ID)
        {
            return false;
        }

        return legion.Mission == LegionMission.Attack ||
               kind == LegionIntentKind.Assault ||
               frontRole is LegionFrontRole.MainAssault or LegionFrontRole.Diversion;
    }

    private static IntentTargetRef ResolveRecoveryTarget(Architecture sourceArchitecture, int ownerFactionId)
    {
        if (sourceArchitecture == null)
        {
            return IntentTargetRef.None;
        }

        return IntentTargetRef.ForArchitecture(
            sourceArchitecture.ID,
            sourceArchitecture.Position,
            sourceArchitecture.BelongedFaction?.ID ?? ownerFactionId);
    }

    private readonly record struct AttackFoodReservation(int SourceArchitectureId, int ReservedFood);
}
