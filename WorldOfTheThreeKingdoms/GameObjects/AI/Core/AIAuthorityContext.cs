using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed class AIAuthorityContext
{
    private readonly Dictionary<int, int> _factionIntentVersionByFaction = new();
    private readonly Dictionary<int, int> _legionIntentVersionByLegion = new();
    private readonly Dictionary<int, int> _troopIntentVersionByTroop = new();
    private readonly Dictionary<int, TroopExecutionState> _executionStateByTroop = new();

    private readonly Dictionary<int, FactionIntent> _factionIntents = new();
    private readonly Dictionary<int, LegionIntent> _legionIntents = new();

    private readonly FactionIntentPlanner _factionIntentPlanner = new();
    private readonly LegionIntentPlanner _legionIntentPlanner = new();
    private readonly TroopIntentPlanner _troopIntentPlanner = new();
    private readonly TroopIntentFailedEvent[] _drainBuffer;

    public TroopIntentRegistry IntentRegistry { get; }
    public TroopIntentFeedbackQueue FeedbackQueue { get; }
    public AIAdvisorFacade AdvisorFacade { get; }

    public IReadOnlyDictionary<int, FactionIntent> FactionIntents => _factionIntents;
    public IReadOnlyDictionary<int, LegionIntent> LegionIntents => _legionIntents;

    public AIAuthorityContext(int feedbackCapacity = 4096)
    {
        IntentRegistry = new TroopIntentRegistry();
        FeedbackQueue = new TroopIntentFeedbackQueue(feedbackCapacity);
        AdvisorFacade = new AIAdvisorFacade();
        _drainBuffer = new TroopIntentFailedEvent[Math.Clamp(feedbackCapacity, 64, 2048)];
    }

    public void BeginTurn(GameScenario scenario)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        IntentRegistry.Clear();
        _factionIntents.Clear();
        _legionIntents.Clear();
        _executionStateByTroop.Clear();

        BuildIntentHierarchy(scenario);
        FeedbackQueue.Clear();
    }

    public void BeginLogicFrame()
    {
    }

    public int BeginLogicFrame(GameScenario scenario)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        return DrainFeedbackAndReplan(scenario);
    }

    public void EndLogicFrame()
    {
    }

    public bool TryGetIntent(int troopId, out TroopIntent intent)
    {
        return IntentRegistry.TryGetCurrentIntent(troopId, out intent);
    }

    public bool TryGetExecutionState(int troopId, out TroopExecutionState state)
    {
        return _executionStateByTroop.TryGetValue(troopId, out state);
    }

    public int GetFriendlyBlockEscalation(Troop troop)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        if (!_executionStateByTroop.TryGetValue(troop.ID, out TroopExecutionState state)) return 0;
        return Math.Max(state.FriendlyBlockEscalationTicks, state.LegacyWaitCounterMirror);
    }

    public void SetFriendlyBlockEscalation(Troop troop, int value)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        int normalized = Math.Max(0, value);
        TroopExecutionState state = GetOrCreateExecutionState(troop.ID);
        state.FriendlyBlockEscalationTicks = normalized;
        state.LegacyWaitCounterMirror = normalized;
        if (normalized == 0)
        {
            state.BlockedTicks = 0;
        }
    }

    public bool IsSwapCooldownActive(Troop troop, int currentTick)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        if (!_executionStateByTroop.TryGetValue(troop.ID, out TroopExecutionState state)) return false;
        return state.SwapCooldownUntilTick > currentTick;
    }

    public void SetSwapCooldown(Troop troop, int cooldownUntilTick)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        TroopExecutionState state = GetOrCreateExecutionState(troop.ID);
        state.SwapCooldownUntilTick = Math.Max(state.SwapCooldownUntilTick, cooldownUntilTick);
    }

    public void SyncStuckCounter(Troop troop, int stuckCounter, bool pathFailEvent)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        TroopExecutionState state = GetOrCreateExecutionState(troop.ID);
        int normalized = Math.Max(0, stuckCounter);
        state.LegacyStuckCounterMirror = normalized;
        state.NoProgressTicks = normalized;
        if (pathFailEvent)
        {
            state.PathFailStreak++;
        }
    }

    public void ClearStuckCounter(Troop troop)
    {
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        TroopExecutionState state = GetOrCreateExecutionState(troop.ID);
        state.LegacyStuckCounterMirror = 0;
        state.NoProgressTicks = 0;
        state.PathFailStreak = 0;
    }

    public bool ApplyIntentProjection(GameScenario scenario, Troop troop)
    {
        if (scenario == null || troop == null || troop.Destroyed) return false;
        if (!ShouldAuthorityOwnTroop(scenario, troop)) return false;
        if (!IntentRegistry.TryGetCurrentIntent(troop.ID, out TroopIntent intent)) return false;

        TroopExecutionState executionState = GetOrCreateExecutionState(troop.ID);
        SyncLegacyCompatibilityState(scenario.DaySince, troop, executionState);
        RefreshPostureState(scenario, troop, intent, executionState, IntentCheckpointKind.BeforeProjection);
        bool preserveExplicitDestination = ShouldPreserveExplicitCommandDestination(troop);

        switch (intent.Target.Kind)
        {
            case IntentTargetKind.Position:
                if (!preserveExplicitDestination && IsValidPosition(intent.Target.Position))
                {
                    troop.RealDestination = intent.Target.Position;
                }
                break;

            case IntentTargetKind.Troop:
                Troop targetTroop = scenario.Troops.GetGameObject(intent.Target.TargetId) as Troop;
                if (targetTroop != null && !targetTroop.Destroyed)
                {
                    troop.TargetTroop = targetTroop;
                    bool projectAsAttackTroop =
                        intent.Kind == TroopIntentKind.AttackTroop ||
                        troop.Command is TroopCommand.AttackTroop or TroopCommand.Attack or TroopCommand.Stratagem;
                    if (projectAsAttackTroop)
                    {
                        Point attackPosition = troop.GetOptimalAttackPosition(targetTroop);
                        if (IsValidPosition(attackPosition))
                        {
                            troop.RealDestination = attackPosition;
                        }
                        else if (IsValidPosition(targetTroop.Position))
                        {
                            troop.RealDestination = targetTroop.Position;
                        }
                    }
                    else if (IsValidPosition(targetTroop.Position))
                    {
                        troop.RealDestination = targetTroop.Position;
                    }
                }
                break;

            case IntentTargetKind.Architecture:
                Architecture targetArchitecture = scenario.Architectures.GetGameObject(intent.Target.TargetId) as Architecture;
                if (targetArchitecture != null)
                {
                    troop.TargetArchitecture = targetArchitecture;

                    Point projectedPosition = IsValidPosition(intent.Target.Position)
                        ? intent.Target.Position
                        : targetArchitecture.Position;

                    bool preserveLegacySiegePosition =
                        troop.Command == TroopCommand.AttackArch &&
                        !IsValidPosition(intent.Target.Position) &&
                        IsValidPosition(troop.RealDestination);

                    if (!preserveLegacySiegePosition && IsValidPosition(projectedPosition))
                    {
                        troop.RealDestination = projectedPosition;
                    }
                }
                break;
        }

        TroopAIState projectedState = ResolveProjectedAIState(intent.Kind, executionState.CurrentPosture, troop.CurrentAIState);
        if (TryResolveCommandProjectedState(troop.Command, out TroopAIState commandProjectedState))
        {
            projectedState = commandProjectedState;
        }

        troop.CurrentAIState = projectedState;

        return true;
    }

    public bool ReportIntentFailure(in TroopIntentFailedEvent eventData)
    {
        return FeedbackQueue.Publish(in eventData);
    }

    public bool HandleExecutorFailure(
        GameScenario scenario,
        Troop troop,
        TroopIntentFailureReason reason,
        Point failedAt,
        int relatedObjectId,
        IntentCheckpointKind checkpoint = IntentCheckpointKind.AfterBlocked,
        bool shouldReplanNow = true)
    {
        if (scenario == null || troop == null || troop.Destroyed) return false;
        if (!ShouldAuthorityOwnTroop(scenario, troop)) return false;
        if (!TryGetIntent(troop.ID, out TroopIntent intent)) return false;

        TroopExecutionState executionState = GetOrCreateExecutionState(troop.ID);
        SyncLegacyCompatibilityState(scenario.DaySince, troop, executionState);
        RefreshPostureState(scenario, troop, intent, executionState, checkpoint);
        troop.CurrentAIState = ResolveProjectedAIState(intent.Kind, executionState.CurrentPosture, troop.CurrentAIState);

        IntentValidationResult validation = IntentValidationResult.Invalid(
            reason,
            relatedObjectId,
            failedAt,
            checkpoint,
            ResolveSuggestedFallback(reason),
            shouldReplanNow);

        HandleIntentFailure(scenario, troop, intent, executionState, validation);
        return true;
    }

    public bool ValidateAndHandleIntentCheckpoint(GameScenario scenario, Troop troop, IntentCheckpointKind checkpoint)
    {
        if (scenario == null || troop == null || troop.Destroyed) return false;
        if (!ShouldAuthorityOwnTroop(scenario, troop)) return false;
        if (!TryGetIntent(troop.ID, out TroopIntent intent)) return true;

        TroopExecutionState executionState = GetOrCreateExecutionState(troop.ID);
        SyncLegacyCompatibilityState(scenario.DaySince, troop, executionState);
        RefreshPostureState(scenario, troop, intent, executionState, checkpoint);
        troop.CurrentAIState = ResolveProjectedAIState(intent.Kind, executionState.CurrentPosture, troop.CurrentAIState);

        IntentValidationResult validation = ValidateTroopIntent(
            scenario,
            troop,
            intent,
            executionState,
            checkpoint);

        if (validation.IsValid) return true;

        HandleIntentFailure(scenario, troop, intent, executionState, validation);
        return false;
    }

    public int DrainFeedbackAndReplan(GameScenario scenario)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        int totalDrained = 0;
        int drained;
        while ((drained = FeedbackQueue.Drain(_drainBuffer)) > 0)
        {
            totalDrained += drained;
            for (int i = 0; i < drained; i++)
            {
                TroopIntentFailedEvent eventData = _drainBuffer[i];

                if (!TryGetOrCreateTroopById(scenario, eventData.TroopId, out Troop troop)) continue;
                if (troop.Destroyed) continue;
                if (!ShouldAuthorityOwnTroop(scenario, troop)) continue;

                if (!_executionStateByTroop.TryGetValue(troop.ID, out TroopExecutionState state)) continue;
                if (!state.PendingReplan) continue;

                if (eventData.Reason == TroopIntentFailureReason.TargetOwnerChanged && troop.BelongedLegion != null)
                {
                    ReplanLegion(scenario, troop.BelongedLegion, eventData.Reason);
                }
                else
                {
                    ReplanTroop(scenario, troop, eventData.Reason);
                }
            }
        }

        return totalDrained;
    }

    public bool ReplanTroop(GameScenario scenario, Troop troop, TroopIntentFailureReason reason)
    {
        if (scenario == null || troop == null || troop.Destroyed) return false;
        if (!ShouldAuthorityOwnTroop(scenario, troop)) return false;

        if (troop.BelongedFaction == null || troop.BelongedLegion == null)
        {
            if (_executionStateByTroop.TryGetValue(troop.ID, out TroopExecutionState orphanState))
            {
                orphanState.PendingReplan = false;
            }
            return false;
        }

        FactionIntent factionIntent = EnsureFactionIntent(scenario, troop.BelongedFaction);
        LegionIntent legionIntent = EnsureLegionIntent(
            scenario,
            troop.BelongedLegion,
            factionIntent,
            scenario.DaySince,
            reason == TroopIntentFailureReason.TargetOwnerChanged);

        int troopVersion = GetNextTroopIntentVersion(troop.ID);
        TroopIntent rebuiltIntent = _troopIntentPlanner.BuildTroopIntent(
            scenario,
            troop,
            legionIntent,
            scenario.DaySince,
            troopVersion);

        TroopIntent finalIntent = ApplyFailureBias(rebuiltIntent, reason, scenario.DaySince);
        IntentRegistry.SetCurrentIntent(finalIntent);

        TroopExecutionState state = GetOrCreateExecutionState(troop.ID);
        state.ActiveIntentId = finalIntent.Id;
        state.LastValidatedTick = scenario.DaySince;
        state.PendingReplan = false;
        state.BlockedTicks = 0;
        state.FriendlyBlockEscalationTicks = 0;
        state.ConsecutiveThreatSpikeTicks = 0;
        state.LocalCooldownUntilTick = scenario.DaySince;
        state.PathFailStreak = 0;
        state.NoProgressTicks = Math.Max(0, troop.stuckedFor);
        state.LegacyStuckCounterMirror = Math.Max(0, troop.stuckedFor);
        state.LegacyWaitCounterMirror = 0;
        state.SwapCooldownUntilTick = troop.LastSwapTurn >= 0 ? troop.LastSwapTurn + 3 : -1;
        if (state.CurrentPosture == TacticalPosture.None)
        {
            SetPosture(state, ResolveIntentPreferredPosture(finalIntent), scenario.DaySince);
        }

        ApplyIntentProjection(scenario, troop);
        return true;
    }

    public int ReplanLegion(GameScenario scenario, Legion legion, TroopIntentFailureReason reason)
    {
        if (scenario == null || legion == null) return 0;

        Faction faction = legion.BelongedFaction;
        if (faction == null) return 0;
        if (!ShouldAuthorityOwnFaction(scenario, faction)) return 0;

        FactionIntent factionIntent = EnsureFactionIntent(scenario, faction);
        LegionIntent legionIntent = EnsureLegionIntent(scenario, legion, factionIntent, scenario.DaySince, true);

        int replanned = 0;
        foreach (Troop troop in legion.Troops)
        {
            if (troop == null || troop.Destroyed) continue;
            if (ReplanTroop(scenario, troop, reason))
            {
                replanned++;
            }
        }

        return replanned;
    }

    private static void SyncLegacyCompatibilityState(int currentTick, Troop troop, TroopExecutionState state)
    {
        if (troop == null || state == null) return;

        int legacyStuckCounter = Math.Max(0, troop.stuckedFor);
        state.LegacyStuckCounterMirror = legacyStuckCounter;
        if (legacyStuckCounter == 0)
        {
            state.NoProgressTicks = 0;
            state.PathFailStreak = 0;
        }
        else
        {
            state.NoProgressTicks = Math.Max(state.NoProgressTicks, legacyStuckCounter);
        }

        if (troop.LastSwapTurn >= 0)
        {
            int legacySwapCooldownUntilTick = troop.LastSwapTurn + 3;
            if (legacySwapCooldownUntilTick > state.SwapCooldownUntilTick)
            {
                state.SwapCooldownUntilTick = legacySwapCooldownUntilTick;
            }
        }
        else if (state.SwapCooldownUntilTick <= currentTick)
        {
            state.SwapCooldownUntilTick = -1;
        }
    }

    private static void SyncLegacyRetreatLock(Troop troop, TacticalPosture posture)
    {
        if (troop == null) return;
        troop.IsRetreatLocked = posture == TacticalPosture.Withdraw;
    }

    private void BuildIntentHierarchy(GameScenario scenario)
    {
        if (scenario.Factions == null) return;

        int issuedTick = scenario.DaySince;

        foreach (Faction faction in scenario.Factions)
        {
            if (faction == null || faction.Destroyed) continue;
            if (!ShouldAuthorityOwnFaction(scenario, faction)) continue;

            int factionVersion = GetNextFactionIntentVersion(faction.ID);
            FactionIntent factionIntent = _factionIntentPlanner.BuildFactionIntent(
                scenario,
                faction,
                AdvisorFacade,
                issuedTick,
                factionVersion);
            _factionIntents[faction.ID] = factionIntent;

            if (faction.Legions == null) continue;

            foreach (GameObject legionObject in faction.Legions.GetList())
            {
                Legion legion = legionObject as Legion;
                if (legion == null) continue;

                int legionVersion = GetNextLegionIntentVersion(legion.ID);
                LegionIntent legionIntent = _legionIntentPlanner.BuildLegionIntent(
                    scenario,
                    legion,
                    factionIntent,
                    issuedTick,
                    legionVersion);
                _legionIntents[legion.ID] = legionIntent;

                if (legion.Troops == null) continue;

                foreach (Troop troop in legion.Troops)
                {
                    if (troop == null || troop.Destroyed) continue;
                    if (!ShouldAuthorityOwnTroop(scenario, troop)) continue;

                    int troopVersion = GetNextTroopIntentVersion(troop.ID);
                    TroopIntent troopIntent = _troopIntentPlanner.BuildTroopIntent(
                        scenario,
                        troop,
                        legionIntent,
                        issuedTick,
                        troopVersion);

                    IntentRegistry.SetCurrentIntent(troopIntent);

                    TroopExecutionState executionState = GetOrCreateExecutionState(troop.ID);
                    executionState.ActiveIntentId = troopIntent.Id;
                    SetPosture(executionState, ResolveIntentPreferredPosture(troopIntent), issuedTick);
                    executionState.LastCheckpoint = IntentCheckpointKind.None;
                    executionState.LastValidatedTick = issuedTick;
                    executionState.BlockedTicks = 0;
                    executionState.FriendlyBlockEscalationTicks = 0;
                    executionState.PathFailStreak = 0;
                    executionState.NoProgressTicks = Math.Max(0, troop.stuckedFor);
                    executionState.LegacyStuckCounterMirror = Math.Max(0, troop.stuckedFor);
                    executionState.LegacyWaitCounterMirror = 0;
                    executionState.SwapCooldownUntilTick = troop.LastSwapTurn >= 0 ? troop.LastSwapTurn + 3 : -1;
                    executionState.ConsecutiveThreatSpikeTicks = 0;
                    executionState.PendingReplan = false;
                    executionState.LocalCooldownUntilTick = issuedTick;
                    executionState.LastFailureReason = TroopIntentFailureReason.ExecutorConflict;
                }
            }
        }
    }

    private IntentValidationResult ValidateTroopIntent(
        GameScenario scenario,
        Troop troop,
        TroopIntent intent,
        TroopExecutionState executionState,
        IntentCheckpointKind checkpoint)
    {
        int currentTick = scenario.DaySince;
        executionState.ActiveIntentId = intent.Id;
        executionState.LastCheckpoint = checkpoint;
        executionState.LastValidatedTick = currentTick;

        int activeCooldownUntilTick = Math.Max(
            Math.Max(executionState.LocalCooldownUntilTick, executionState.PostureCooldownUntilTick),
            executionState.SwapCooldownUntilTick);
        if (checkpoint == IntentCheckpointKind.QueuePickup &&
            activeCooldownUntilTick > currentTick)
        {
            return IntentValidationResult.Invalid(
                TroopIntentFailureReason.PathBlocked,
                -1,
                troop.Position,
                checkpoint,
                FallbackPolicy.Hold,
                false);
        }

        if (checkpoint == IntentCheckpointKind.AfterBlocked)
        {
            int blockedEstimate = executionState.BlockedTicks + 1;
            return IntentValidationResult.Invalid(
                TroopIntentFailureReason.PathBlocked,
                -1,
                troop.Position,
                checkpoint,
                FallbackPolicy.Hold,
                blockedEstimate >= TacticalPostureConfig.ReplanBlockedThreshold);
        }

        bool isCombatCommitCheckpoint =
            checkpoint == IntentCheckpointKind.BeforeCombatCheck ||
            checkpoint == IntentCheckpointKind.BeforeAttackCommit;
        if (isCombatCommitCheckpoint &&
            executionState.ConsecutiveThreatSpikeTicks >= TacticalPostureConfig.ThreatSpikeReplanThreshold &&
            executionState.CurrentPosture != TacticalPosture.Withdraw &&
            currentTick > intent.CommitUntilTick)
        {
            return IntentValidationResult.Invalid(
                TroopIntentFailureReason.ThreatSpike,
                -1,
                troop.Position,
                checkpoint,
                FallbackPolicy.Withdraw,
                true);
        }

        if (isCombatCommitCheckpoint &&
            intent.RiskBudget > 0 &&
            executionState.CurrentPosture != TacticalPosture.Withdraw &&
            currentTick > intent.CommitUntilTick)
        {
            int threatScore = CalculateThreatScore(troop, executionState.CurrentPosture);
            if (threatScore > intent.RiskBudget + 5)
            {
                return IntentValidationResult.Invalid(
                    TroopIntentFailureReason.ThreatSpike,
                    -1,
                    troop.Position,
                    checkpoint,
                    FallbackPolicy.Withdraw,
                    true);
            }
        }

        if (intent.Target.Kind == IntentTargetKind.None)
        {
            if (intent.Kind is TroopIntentKind.Hold or TroopIntentKind.Recover or TroopIntentKind.BlockRetry)
            {
                return IntentValidationResult.Valid(checkpoint, troop.Position);
            }

            return IntentValidationResult.Invalid(
                TroopIntentFailureReason.InvalidDestination,
                -1,
                troop.Position,
                checkpoint,
                FallbackPolicy.Replan,
                true);
        }

        switch (intent.Target.Kind)
        {
            case IntentTargetKind.Position:
                if (!IsValidPosition(intent.Target.Position))
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.InvalidDestination,
                        -1,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.Replan,
                        true);
                }
                break;

            case IntentTargetKind.Troop:
            {
                Troop targetTroop = scenario.Troops.GetGameObject(intent.Target.TargetId) as Troop;
                if (targetTroop == null || targetTroop.Destroyed)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.TargetLost,
                        intent.Target.TargetId,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.RetargetSameMission,
                        true);
                }

                int ownerFactionId = targetTroop.BelongedFaction?.ID ?? -1;
                if (intent.TargetOwnerStamp >= 0 && ownerFactionId != intent.TargetOwnerStamp)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.TargetOwnerChanged,
                        targetTroop.ID,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.Replan,
                        true);
                }

                if (IsVisibilityLost(troop, targetTroop.Position) && currentTick > intent.CommitUntilTick)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.VisibilityLost,
                        targetTroop.ID,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.RetargetSameMission,
                        true);
                }
                break;
            }

            case IntentTargetKind.Architecture:
            {
                Architecture targetArchitecture = scenario.Architectures.GetGameObject(intent.Target.TargetId) as Architecture;
                if (targetArchitecture == null)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.TargetLost,
                        intent.Target.TargetId,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.Replan,
                        true);
                }

                int ownerFactionId = targetArchitecture.BelongedFaction?.ID ?? -1;
                if (intent.TargetOwnerStamp >= 0 && ownerFactionId != intent.TargetOwnerStamp)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.TargetOwnerChanged,
                        targetArchitecture.ID,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.Replan,
                        true);
                }

                if (intent.Kind == TroopIntentKind.AttackArchitecture && targetArchitecture.Endurance <= 0)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.TargetLost,
                        targetArchitecture.ID,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.RetargetSameMission,
                        true);
                }

                if (IsVisibilityLost(troop, targetArchitecture.Position) && currentTick > intent.CommitUntilTick)
                {
                    return IntentValidationResult.Invalid(
                        TroopIntentFailureReason.VisibilityLost,
                        targetArchitecture.ID,
                        troop.Position,
                        checkpoint,
                        FallbackPolicy.Replan,
                        true);
                }
                break;
            }

            default:
                return IntentValidationResult.Invalid(
                    TroopIntentFailureReason.ExecutorConflict,
                    -1,
                    troop.Position,
                    checkpoint,
                    FallbackPolicy.Hold,
                    false);
        }

        return IntentValidationResult.Valid(checkpoint, troop.Position);
    }

    private void HandleIntentFailure(
        GameScenario scenario,
        Troop troop,
        TroopIntent intent,
        TroopExecutionState executionState,
        IntentValidationResult validation)
    {
        executionState.LastFailureReason = validation.FailureReason;
        bool isBlockedFailure =
            validation.FailureReason is TroopIntentFailureReason.PathBlocked or
                TroopIntentFailureReason.ArbitrationLost;
        if (isBlockedFailure)
        {
            executionState.BlockedTicks++;
            executionState.FriendlyBlockEscalationTicks = Math.Max(
                executionState.FriendlyBlockEscalationTicks,
                executionState.BlockedTicks);
        }
        else
        {
            executionState.BlockedTicks = 0;
            executionState.FriendlyBlockEscalationTicks = 0;
        }

        if (validation.FailureReason == TroopIntentFailureReason.ThreatSpike)
        {
            executionState.ConsecutiveThreatSpikeTicks++;
        }
        else
        {
            executionState.ConsecutiveThreatSpikeTicks = 0;
        }

        int cooldownTicks = isBlockedFailure ? 1 : 0;
        executionState.LocalCooldownUntilTick = scenario.DaySince + cooldownTicks;
        executionState.PendingReplan = validation.ShouldReplanNow;

        int sourceFactionId = troop.BelongedFaction?.ID ?? intent.SourceFactionId;
        AdvisorFacade.ReportTroopIntentFailure(sourceFactionId, validation.FailureReason, scenario.DaySince);

        TroopIntentFailedEvent failedEvent = new(
            intent.Id,
            troop.ID,
            validation.FailureReason,
            scenario.DaySince,
            validation.FailedAt,
            validation.RelatedObjectId);
        ReportIntentFailure(in failedEvent);

        ApplyFallback(scenario.DaySince, troop, intent, validation, executionState);
    }

    private static FallbackPolicy ResolveSuggestedFallback(TroopIntentFailureReason reason)
    {
        return reason switch
        {
            TroopIntentFailureReason.TargetLost => FallbackPolicy.RetargetSameMission,
            TroopIntentFailureReason.TargetOwnerChanged => FallbackPolicy.Replan,
            TroopIntentFailureReason.VisibilityLost => FallbackPolicy.RetargetSameMission,
            TroopIntentFailureReason.ThreatSpike => FallbackPolicy.Withdraw,
            _ => FallbackPolicy.Hold
        };
    }

    private static void ApplyFallback(
        int currentTick,
        Troop troop,
        TroopIntent intent,
        IntentValidationResult validation,
        TroopExecutionState executionState)
    {
        FallbackPolicy fallback = validation.SuggestedFallback == FallbackPolicy.Abort
            ? intent.FallbackPolicy
            : validation.SuggestedFallback;

        troop.Action = TroopAction.Stop;
        troop.HasToDoCombatAction = false;

        switch (fallback)
        {
            case FallbackPolicy.Withdraw:
                SetPosture(executionState, TacticalPosture.Withdraw, currentTick);
                troop.CurrentAIState = TroopAIState.Retreating;
                troop.GoBack();
                troop.IsRetreatLocked = true;
                break;

            case FallbackPolicy.RetargetSameMission:
            case FallbackPolicy.Replan:
                SetPosture(executionState, TacticalPosture.Hold, currentTick);
                troop.CurrentAIState = TroopAIState.Waiting;
                troop.SetCommand(TroopCommand.None);
                executionState.PendingReplan = true;
                break;

            case FallbackPolicy.Abort:
                SetPosture(executionState, TacticalPosture.Hold, currentTick);
                troop.CurrentAIState = TroopAIState.Idle;
                troop.SetCommand(TroopCommand.None);
                troop.OperationDone = true;
                executionState.PendingReplan = false;
                break;

            default:
                SetPosture(executionState, TacticalPosture.Hold, currentTick);
                troop.CurrentAIState = TroopAIState.Waiting;
                troop.SetCommand(TroopCommand.None);
                break;
        }

        SyncLegacyRetreatLock(troop, executionState.CurrentPosture);
    }

    private FactionIntent EnsureFactionIntent(GameScenario scenario, Faction faction)
    {
        if (_factionIntents.TryGetValue(faction.ID, out FactionIntent intent))
        {
            return intent;
        }

        int version = GetNextFactionIntentVersion(faction.ID);
        FactionIntent rebuilt = _factionIntentPlanner.BuildFactionIntent(
            scenario,
            faction,
            AdvisorFacade,
            scenario.DaySince,
            version);
        _factionIntents[faction.ID] = rebuilt;
        return rebuilt;
    }

    private LegionIntent EnsureLegionIntent(GameScenario scenario, Legion legion, FactionIntent factionIntent, int issuedTick, bool forceRebuild)
    {
        if (!forceRebuild && _legionIntents.TryGetValue(legion.ID, out LegionIntent cached))
        {
            return cached;
        }

        int version = GetNextLegionIntentVersion(legion.ID);
        LegionIntent rebuilt = _legionIntentPlanner.BuildLegionIntent(
            scenario,
            legion,
            factionIntent,
            issuedTick,
            version);
        _legionIntents[legion.ID] = rebuilt;
        return rebuilt;
    }

    private static TroopIntent ApplyFailureBias(TroopIntent intent, TroopIntentFailureReason reason, int currentTick)
    {
        return reason switch
        {
            TroopIntentFailureReason.PathBlocked => intent with
            {
                Kind = TroopIntentKind.BlockRetry,
                PreferredPosture = TacticalPosture.Hold,
                CommitUntilTick = currentTick + 1,
                CooldownUntilTick = currentTick + 1,
                FallbackPolicy = FallbackPolicy.Hold
            },
            TroopIntentFailureReason.InvalidDestination => intent with
            {
                Kind = TroopIntentKind.Hold,
                PreferredPosture = TacticalPosture.Hold,
                Target = IntentTargetRef.None,
                CommitUntilTick = currentTick + 1,
                CooldownUntilTick = currentTick + 1,
                FallbackPolicy = FallbackPolicy.Replan
            },
            TroopIntentFailureReason.TargetOwnerChanged => intent with
            {
                Kind = TroopIntentKind.Hold,
                PreferredPosture = TacticalPosture.Hold,
                Target = IntentTargetRef.None,
                CommitUntilTick = currentTick + 1,
                CooldownUntilTick = currentTick + 1,
                FallbackPolicy = FallbackPolicy.Replan
            },
            TroopIntentFailureReason.VisibilityLost => intent with
            {
                PreferredPosture = TacticalPosture.Hold,
                CooldownUntilTick = currentTick + 1,
                FallbackPolicy = FallbackPolicy.RetargetSameMission
            },
            TroopIntentFailureReason.ThreatSpike => intent with
            {
                Kind = TroopIntentKind.Withdraw,
                PreferredPosture = TacticalPosture.Withdraw,
                CommitUntilTick = currentTick + 2,
                CooldownUntilTick = currentTick + 2,
                FallbackPolicy = FallbackPolicy.Withdraw
            },
            TroopIntentFailureReason.ArbitrationLost => intent with
            {
                Kind = TroopIntentKind.BlockRetry,
                PreferredPosture = TacticalPosture.Hold,
                CommitUntilTick = currentTick + 1,
                CooldownUntilTick = currentTick + 1,
                FallbackPolicy = FallbackPolicy.Hold
            },
            _ => intent
        };
    }

    private static void RefreshPostureState(
        GameScenario scenario,
        Troop troop,
        TroopIntent intent,
        TroopExecutionState state,
        IntentCheckpointKind checkpoint)
    {
        int currentTick = scenario.DaySince;
        TacticalPosture preferred = ResolveIntentPreferredPosture(intent);
        int threatScore = CalculateThreatScore(troop, state.CurrentPosture);

        if (threatScore >= TacticalPostureConfig.ThreatSpikeScore)
        {
            state.ConsecutiveThreatSpikeTicks++;
        }
        else if (state.ConsecutiveThreatSpikeTicks > 0)
        {
            state.ConsecutiveThreatSpikeTicks--;
        }

        TacticalPosture desired = ResolveDesiredPosture(intent, preferred, threatScore, checkpoint);
        TacticalPosture next = ResolvePostureWithHysteresis(state, desired, threatScore, currentTick);
        if (next != state.CurrentPosture)
        {
            SetPosture(state, next, currentTick);
        }
        else if (state.CurrentPosture == TacticalPosture.None)
        {
            SetPosture(state, preferred, currentTick);
        }

        SyncLegacyRetreatLock(troop, state.CurrentPosture);
    }

    private static TacticalPosture ResolveIntentPreferredPosture(TroopIntent intent)
    {
        if (intent.PreferredPosture != TacticalPosture.None)
        {
            return intent.PreferredPosture;
        }

        return intent.Kind switch
        {
            TroopIntentKind.Withdraw => TacticalPosture.Withdraw,
            TroopIntentKind.AttackArchitecture => TacticalPosture.Commit,
            TroopIntentKind.AttackTroop => TacticalPosture.Commit,
            TroopIntentKind.March => TacticalPosture.Advance,
            TroopIntentKind.EnterCity => TacticalPosture.Advance,
            TroopIntentKind.Recover => TacticalPosture.Recover,
            TroopIntentKind.BlockRetry => TacticalPosture.Hold,
            _ => TacticalPosture.Hold
        };
    }

    private static TacticalPosture ResolveDesiredPosture(
        TroopIntent intent,
        TacticalPosture preferred,
        int threatScore,
        IntentCheckpointKind checkpoint)
    {
        if (preferred == TacticalPosture.Recover || intent.Kind == TroopIntentKind.Recover)
        {
            return TacticalPosture.Recover;
        }

        if (threatScore >= TacticalPostureConfig.WithdrawEnterThreatScore)
        {
            return TacticalPosture.Withdraw;
        }

        if (intent.Kind == TroopIntentKind.BlockRetry)
        {
            return TacticalPosture.Hold;
        }

        if (preferred == TacticalPosture.Commit)
        {
            return threatScore <= TacticalPostureConfig.CommitEnterThreatCap
                ? TacticalPosture.Commit
                : TacticalPosture.Hold;
        }

        if (preferred == TacticalPosture.Advance &&
            (checkpoint == IntentCheckpointKind.BeforeCombatCheck || checkpoint == IntentCheckpointKind.BeforeAttackCommit) &&
            threatScore >= TacticalPostureConfig.CommitExitThreatScore)
        {
            return TacticalPosture.Hold;
        }

        return preferred;
    }

    private static TacticalPosture ResolvePostureWithHysteresis(
        TroopExecutionState state,
        TacticalPosture desired,
        int threatScore,
        int currentTick)
    {
        TacticalPosture current = state.CurrentPosture;
        if (current == TacticalPosture.None)
        {
            return desired;
        }

        if (desired == current)
        {
            return current;
        }

        if (current == TacticalPosture.Withdraw)
        {
            if (currentTick <= state.PostureCommitUntilTick)
            {
                return TacticalPosture.Withdraw;
            }

            if (threatScore > TacticalPostureConfig.WithdrawExitThreatScore)
            {
                return TacticalPosture.Withdraw;
            }
        }

        if (current == TacticalPosture.Commit &&
            desired != TacticalPosture.Withdraw &&
            currentTick <= state.PostureCommitUntilTick)
        {
            return TacticalPosture.Commit;
        }

        if (currentTick < state.PostureCooldownUntilTick && IsOppositePosture(current, desired))
        {
            return current;
        }

        return desired;
    }

    private static bool IsOppositePosture(TacticalPosture current, TacticalPosture desired)
    {
        return (current == TacticalPosture.Withdraw && (desired == TacticalPosture.Commit || desired == TacticalPosture.Advance)) ||
               ((current == TacticalPosture.Commit || current == TacticalPosture.Advance) && desired == TacticalPosture.Withdraw);
    }

    private static int CalculateThreatScore(Troop troop, TacticalPosture currentPosture)
    {
        int quantity = troop.Army?.Quantity ?? 0;
        int quantityMax = troop.Army?.Kind?.MaxScale ?? 0;
        int food = troop.Food;
        int foodMax = troop.FoodMax;

        int hpRisk = quantityMax > 0 ? 100 - (quantity * 100 / quantityMax) : 0;
        int foodRisk = foodMax > 0 ? 100 - (Math.Max(food, 0) * 100 / foodMax) : 0;
        int enemyUnits = Math.Max(troop.ViewingHostileTroopCount, troop.ContactHostileTroopCount);
        int enemyPressure = enemyUnits > 0 ? Math.Min(40, enemyUnits * 12) : 0;

        int retreatBias = currentPosture == TacticalPosture.Withdraw ? 10 : 0;
        int weighted = (hpRisk * 55 + foodRisk * 20 + enemyPressure * 25) / 100;
        return Math.Clamp(weighted + retreatBias, 0, 100);
    }

    private static void SetPosture(TroopExecutionState state, TacticalPosture posture, int currentTick)
    {
        TacticalPosture normalized = posture == TacticalPosture.None ? TacticalPosture.Hold : posture;
        state.CurrentPosture = normalized;
        state.PostureEnteredTick = currentTick;
        state.PostureCommitUntilTick = currentTick + TacticalPostureConfig.GetCommitTicks(normalized);
        state.PostureCooldownUntilTick = currentTick + TacticalPostureConfig.GetCooldownTicks(normalized);
    }

    private static TroopAIState ResolveProjectedAIState(
        TroopIntentKind intentKind,
        TacticalPosture posture,
        TroopAIState currentState)
    {
        if (posture == TacticalPosture.Withdraw)
        {
            return TroopAIState.Retreating;
        }

        if (posture == TacticalPosture.Recover)
        {
            return TroopAIState.Waiting;
        }

        if (intentKind == TroopIntentKind.EnterCity)
        {
            return TroopAIState.EnterCity;
        }

        if (posture == TacticalPosture.Hold)
        {
            return TroopAIState.Waiting;
        }

        return intentKind switch
        {
            TroopIntentKind.AttackTroop => TroopAIState.Combat,
            TroopIntentKind.AttackArchitecture => TroopAIState.Combat,
            TroopIntentKind.Withdraw => TroopAIState.Retreating,
            TroopIntentKind.BlockRetry => TroopAIState.Waiting,
            TroopIntentKind.Recover => TroopAIState.Waiting,
            TroopIntentKind.Hold => TroopAIState.Waiting,
            TroopIntentKind.March => TroopAIState.Marching,
            _ => currentState
        };
    }

    private TroopExecutionState GetOrCreateExecutionState(int troopId)
    {
        if (_executionStateByTroop.TryGetValue(troopId, out TroopExecutionState state))
        {
            return state;
        }

        state = new TroopExecutionState();
        _executionStateByTroop[troopId] = state;
        return state;
    }

    private static bool TryGetOrCreateTroopById(GameScenario scenario, int troopId, out Troop troop)
    {
        troop = scenario.Troops.GetGameObject(troopId) as Troop;
        return troop != null;
    }

    private static bool IsVisibilityLost(Troop troop, Point targetPosition)
    {
        Faction faction = troop.BelongedFaction;
        if (faction == null) return false;
        return !faction.IsPositionKnown(targetPosition);
    }

    private static bool ShouldAuthorityOwnFaction(GameScenario scenario, Faction faction)
    {
        if (scenario == null || faction == null || faction.Destroyed)
        {
            return false;
        }

        return !scenario.IsPlayer(faction);
    }

    private static bool ShouldAuthorityOwnTroop(GameScenario scenario, Troop troop)
    {
        if (scenario == null || troop == null || troop.Destroyed)
        {
            return false;
        }

        if (troop.ManualControl)
        {
            return false;
        }

        if (!ShouldAuthorityOwnFaction(scenario, troop.BelongedFaction))
        {
            return false;
        }

        Legion legion = troop.BelongedLegion;
        if (legion != null && legion.Kind == LegionKind.Player)
        {
            return false;
        }

        return true;
    }

    private int GetNextFactionIntentVersion(int factionId)
    {
        return GetNextVersion(_factionIntentVersionByFaction, factionId);
    }

    private int GetNextLegionIntentVersion(int legionId)
    {
        return GetNextVersion(_legionIntentVersionByLegion, legionId);
    }

    private int GetNextTroopIntentVersion(int troopId)
    {
        return GetNextVersion(_troopIntentVersionByTroop, troopId);
    }

    private static int GetNextVersion(Dictionary<int, int> versionMap, int id)
    {
        if (!versionMap.TryGetValue(id, out int currentVersion))
        {
            currentVersion = 0;
        }

        int nextVersion = currentVersion + 1;
        versionMap[id] = nextVersion;
        return nextVersion;
    }

    private static bool IsValidPosition(Point position)
    {
        return position.X >= 0 && position.Y >= 0;
    }

    private static bool ShouldPreserveExplicitCommandDestination(Troop troop)
    {
        if (troop == null)
        {
            return false;
        }

        if (!IsValidPosition(troop.RealDestination))
        {
            return false;
        }

        return troop.Command is TroopCommand.Move or
            TroopCommand.AttackTroop or
            TroopCommand.AttackArch or
            TroopCommand.Attack or
            TroopCommand.Stratagem or
            TroopCommand.Enter;
    }

    private static bool TryResolveCommandProjectedState(TroopCommand command, out TroopAIState projectedState)
    {
        switch (command)
        {
            case TroopCommand.Enter:
                projectedState = TroopAIState.EnterCity;
                return true;
            case TroopCommand.Move:
            case TroopCommand.Attack:
            case TroopCommand.AttackArch:
            case TroopCommand.AttackTroop:
            case TroopCommand.Stratagem:
                projectedState = TroopAIState.Marching;
                return true;
            default:
                projectedState = TroopAIState.Idle;
                return false;
        }
    }
}
