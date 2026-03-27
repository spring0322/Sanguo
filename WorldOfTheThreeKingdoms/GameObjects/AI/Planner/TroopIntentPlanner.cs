using System;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

public sealed class TroopIntentPlanner
{
    public TroopIntent BuildTroopIntent(
        GameScenario scenario,
        Troop troop,
        LegionIntent legionIntent,
        int issuedTick,
        int version)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (troop == null) throw new ArgumentNullException(nameof(troop));
        if (legionIntent == null) throw new ArgumentNullException(nameof(legionIntent));

        TroopIntentKind kind = ResolveIntentKind(troop, legionIntent);
        TacticalPosture preferredPosture = ResolvePreferredPosture(kind);
        IntentTargetRef target = ResolveIntentTarget(scenario, troop, legionIntent);
        int priority = Math.Max(ResolvePriority(kind), legionIntent.Priority);
        priority = Math.Max(1, priority + ResolveSpatialPriorityBias(scenario, troop, target, legionIntent));
        int commitUntilTick = issuedTick + ResolveCommitTicks(kind);
        FallbackPolicy fallbackPolicy = ResolveFallbackPolicy(kind);
        int visibilityStamp = ResolveVisibilityStamp(troop, target, legionIntent);
        int riskBudget = ResolveRiskBudget(scenario, troop, target, kind, legionIntent, visibilityStamp);
        int cooldownUntilTick = ResolveCooldownUntilTick(issuedTick, kind, legionIntent);

        return new TroopIntent(
            new TroopIntentId(troop.ID, version, issuedTick),
            kind,
            preferredPosture,
            target,
            priority,
            commitUntilTick,
            cooldownUntilTick,
            legionIntent.SourceLegionId,
            legionIntent.SourceFactionId,
            riskBudget,
            visibilityStamp,
            target.OwnerFactionId,
            fallbackPolicy);
    }

    private static TroopIntentKind ResolveIntentKind(Troop troop, LegionIntent legionIntent)
    {
        if (TryResolveCommandIntentKind(troop, out TroopIntentKind commandKind))
        {
            return commandKind;
        }

        if (!legionIntent.AttackAuthorized &&
            troop.CurrentAIState == TroopAIState.Waiting &&
            !troop.IsRetreatLocked)
        {
            return legionIntent.FrontRole == LegionFrontRole.Reserve
                ? TroopIntentKind.Recover
                : TroopIntentKind.Hold;
        }

        return troop.CurrentAIState switch
        {
            TroopAIState.Retreating => TroopIntentKind.Withdraw,
            TroopAIState.ForcedRetreat => TroopIntentKind.Withdraw,
            TroopAIState.EnterCity => TroopIntentKind.EnterCity,
            TroopAIState.Marching => TroopIntentKind.March,
            TroopAIState.Sieging => TroopIntentKind.AttackArchitecture,
            TroopAIState.Combat => troop.TargetArchitecture != null
                ? TroopIntentKind.AttackArchitecture
                : TroopIntentKind.AttackTroop,
            TroopAIState.Waiting => troop.IsRetreatLocked
                ? TroopIntentKind.Withdraw
                : MapFromLegionIntent(legionIntent),
            _ => MapFromLegionIntent(legionIntent)
        };
    }

    private static bool TryResolveCommandIntentKind(Troop troop, out TroopIntentKind intentKind)
    {
        switch (troop.Command)
        {
            case TroopCommand.Move:
                intentKind = TroopIntentKind.March;
                return true;
            case TroopCommand.Enter:
                intentKind = TroopIntentKind.EnterCity;
                return true;
            case TroopCommand.AttackArch:
                intentKind = TroopIntentKind.AttackArchitecture;
                return true;
            case TroopCommand.AttackTroop:
                intentKind = TroopIntentKind.AttackTroop;
                return true;
            case TroopCommand.Attack:
            case TroopCommand.Stratagem:
                if (troop.TargetArchitecture != null)
                {
                    intentKind = TroopIntentKind.AttackArchitecture;
                    return true;
                }

                if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed)
                {
                    intentKind = TroopIntentKind.AttackTroop;
                    return true;
                }

                intentKind = IsValidPosition(troop.RealDestination)
                    ? TroopIntentKind.March
                    : TroopIntentKind.Hold;
                return true;
            default:
                intentKind = TroopIntentKind.Hold;
                return false;
        }
    }

    private static TacticalPosture ResolvePreferredPosture(TroopIntentKind intentKind)
    {
        return intentKind switch
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

    private static TroopIntentKind MapFromLegionIntent(LegionIntent legionIntent)
    {
        if (!legionIntent.AttackAuthorized &&
            legionIntent.Kind == LegionIntentKind.Assault)
        {
            return legionIntent.FrontRole == LegionFrontRole.Reserve
                ? TroopIntentKind.Recover
                : TroopIntentKind.Hold;
        }

        LegionIntentKind legionIntentKind = legionIntent.Kind;
        return legionIntentKind switch
        {
            LegionIntentKind.Assault => TroopIntentKind.March,
            LegionIntentKind.Defend => TroopIntentKind.Hold,
            LegionIntentKind.Withdraw => TroopIntentKind.Withdraw,
            LegionIntentKind.Patrol => TroopIntentKind.March,
            LegionIntentKind.Recover => TroopIntentKind.Recover,
            _ => TroopIntentKind.Hold
        };
    }

    private static IntentTargetRef ResolveIntentTarget(GameScenario scenario, Troop troop, LegionIntent legionIntent)
    {
        if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed)
        {
            return IntentTargetRef.ForTroop(
                troop.TargetTroop.ID,
                troop.TargetTroop.Position,
                troop.TargetTroop.BelongedFaction?.ID ?? -1);
        }

        if (troop.TargetArchitecture != null)
        {
            Point architectureTargetPosition = troop.TargetArchitecture.Position;
            if (troop.Command == TroopCommand.AttackArch && IsValidPosition(troop.RealDestination))
            {
                architectureTargetPosition = troop.RealDestination;
            }

            return IntentTargetRef.ForArchitecture(
                troop.TargetArchitecture.ID,
                architectureTargetPosition,
                troop.TargetArchitecture.BelongedFaction?.ID ?? -1);
        }

        if (legionIntent.Target.Kind == IntentTargetKind.Architecture && legionIntent.Target.TargetId >= 0)
        {
            Architecture targetArchitecture = scenario.Architectures.GetGameObject(legionIntent.Target.TargetId) as Architecture;
            if (targetArchitecture != null)
            {
                return IntentTargetRef.ForArchitecture(
                    targetArchitecture.ID,
                    targetArchitecture.Position,
                    targetArchitecture.BelongedFaction?.ID ?? -1);
            }
        }

        if (legionIntent.Target.Kind == IntentTargetKind.Troop && legionIntent.Target.TargetId >= 0)
        {
            Troop targetTroop = scenario.Troops.GetGameObject(legionIntent.Target.TargetId) as Troop;
            if (targetTroop != null && !targetTroop.Destroyed)
            {
                return IntentTargetRef.ForTroop(
                    targetTroop.ID,
                    targetTroop.Position,
                    targetTroop.BelongedFaction?.ID ?? -1);
            }
        }

        if (IsValidPosition(troop.RealDestination))
        {
            return IntentTargetRef.ForPosition(troop.RealDestination);
        }

        if (troop.WillArchitecture != null)
        {
            return IntentTargetRef.ForArchitecture(
                troop.WillArchitecture.ID,
                troop.WillArchitecture.Position,
                troop.WillArchitecture.BelongedFaction?.ID ?? -1);
        }

        if (legionIntent.Target.Kind == IntentTargetKind.Position && IsValidPosition(legionIntent.Target.Position))
        {
            return IntentTargetRef.ForPosition(legionIntent.Target.Position);
        }

        return IntentTargetRef.None;
    }

    private static int ResolvePriority(TroopIntentKind intentKind)
    {
        return intentKind switch
        {
            TroopIntentKind.Withdraw => 100,
            TroopIntentKind.EnterCity => 80,
            TroopIntentKind.AttackArchitecture => 75,
            TroopIntentKind.AttackTroop => 70,
            TroopIntentKind.March => 40,
            TroopIntentKind.BlockRetry => 30,
            TroopIntentKind.Recover => 20,
            TroopIntentKind.Hold => 10,
            _ => 0
        };
    }

    private static int ResolveCommitTicks(TroopIntentKind intentKind)
    {
        return intentKind switch
        {
            TroopIntentKind.Withdraw => 1,
            TroopIntentKind.AttackArchitecture => 2,
            TroopIntentKind.AttackTroop => 2,
            TroopIntentKind.March => 1,
            TroopIntentKind.Recover => 2,
            _ => 1
        };
    }

    private static FallbackPolicy ResolveFallbackPolicy(TroopIntentKind intentKind)
    {
        return intentKind switch
        {
            TroopIntentKind.Withdraw => FallbackPolicy.Withdraw,
            TroopIntentKind.AttackArchitecture => FallbackPolicy.RetargetSameMission,
            TroopIntentKind.AttackTroop => FallbackPolicy.RetargetSameMission,
            TroopIntentKind.March => FallbackPolicy.Replan,
            TroopIntentKind.BlockRetry => FallbackPolicy.Hold,
            _ => FallbackPolicy.Replan
        };
    }

    private static int ResolveSpatialPriorityBias(
        GameScenario scenario,
        Troop troop,
        in IntentTargetRef target,
        LegionIntent legionIntent)
    {
        int bias = legionIntent.TheaterScore / 12;

        if (target.Kind == IntentTargetKind.Architecture || target.Kind == IntentTargetKind.Position || target.Kind == IntentTargetKind.Troop)
        {
            Point targetPosition = ResolveTargetPosition(scenario, target);
            if (IsValidPosition(targetPosition))
            {
                double distance = scenario.GetDistance(troop.Position, targetPosition);
                if (distance > 20.0)
                {
                    bias -= Math.Min(8, (int)Math.Round((distance - 20.0) / 4.0));
                }
            }
        }

        if (legionIntent.TheaterEnergyBalance > 0)
        {
            bias += Math.Min(6, legionIntent.TheaterEnergyBalance / 40);
        }
        else if (legionIntent.TheaterEnergyBalance < 0)
        {
            bias -= Math.Min(6, (-legionIntent.TheaterEnergyBalance) / 35);
        }

        bias += legionIntent.FrontRole switch
        {
            LegionFrontRole.MainAssault => 4,
            LegionFrontRole.Diversion => 2,
            LegionFrontRole.Defense => -1,
            LegionFrontRole.Reserve => -4,
            _ => 0
        };

        if (!legionIntent.AttackAuthorized)
        {
            bias -= 4;
        }

        return bias;
    }

    private static int ResolveRiskBudget(
        GameScenario scenario,
        Troop troop,
        in IntentTargetRef target,
        TroopIntentKind kind,
        LegionIntent legionIntent,
        int visibilityStamp)
    {
        int baseBudget = kind switch
        {
            TroopIntentKind.Withdraw => 85,
            TroopIntentKind.AttackArchitecture => 62,
            TroopIntentKind.AttackTroop => 60,
            TroopIntentKind.March => 55,
            TroopIntentKind.EnterCity => 52,
            TroopIntentKind.Recover => 45,
            TroopIntentKind.BlockRetry => 40,
            _ => 42
        };

        int budget = baseBudget;
        budget += Math.Clamp((visibilityStamp - 50) / 5, -8, 8);
        budget += Math.Clamp(legionIntent.TheaterEnergyBalance / 35, -10, 10);
        budget -= Math.Clamp(legionIntent.TheaterTravelCost / 2, 0, 10);

        Point targetPosition = ResolveTargetPosition(scenario, target);
        if (IsValidPosition(targetPosition))
        {
            double distance = scenario.GetDistance(troop.Position, targetPosition);
            if (distance > 24.0)
            {
                budget -= Math.Min(8, (int)Math.Round((distance - 24.0) / 4.0));
            }

            if (troop.BelongedFaction != null)
            {
                int targetNetEnergy = TheaterSpatialCosting.CalculateNetInfluenceAt(troop.BelongedFaction, targetPosition);
                budget += Math.Clamp(targetNetEnergy / 40, -8, 8);
            }
        }

        budget += legionIntent.FrontRole switch
        {
            LegionFrontRole.MainAssault => 4,
            LegionFrontRole.Diversion => 1,
            LegionFrontRole.Defense => -3,
            LegionFrontRole.Reserve => -6,
            _ => 0
        };

        if (!legionIntent.AttackAuthorized)
        {
            budget = Math.Min(budget, 56);
        }

        int strategyCap = Math.Clamp(legionIntent.StrategyRiskTolerance, 30, 90);
        budget = Math.Min(budget, strategyCap);
        return Math.Clamp(budget, 25, 92);
    }

    private static int ResolveVisibilityStamp(Troop troop, in IntentTargetRef target, LegionIntent legionIntent)
    {
        Faction faction = troop.BelongedFaction;
        if (faction == null) return Math.Clamp(legionIntent.TheaterVisibilityConfidence, 0, 100);

        Point targetPosition = target.Position;
        if (!IsValidPosition(targetPosition))
        {
            return Math.Clamp(legionIntent.TheaterVisibilityConfidence, 0, 100);
        }

        int visibility = TheaterSpatialCosting.CalculateVisibilityConfidence(faction, targetPosition);
        if (visibility <= 0)
        {
            visibility = legionIntent.TheaterVisibilityConfidence;
        }

        return Math.Clamp(visibility, 0, 100);
    }

    private static int ResolveCooldownUntilTick(int issuedTick, TroopIntentKind kind, LegionIntent legionIntent)
    {
        int baseCooldown = kind switch
        {
            TroopIntentKind.Withdraw => 1,
            TroopIntentKind.AttackArchitecture => 1,
            TroopIntentKind.AttackTroop => 1,
            TroopIntentKind.March => 1,
            TroopIntentKind.BlockRetry => 1,
            _ => 0
        };

        int travelBias = legionIntent.TheaterTravelCost > 8 ? 1 : 0;
        if (!legionIntent.AttackAuthorized)
        {
            travelBias++;
        }

        return issuedTick + baseCooldown + travelBias;
    }

    private static Point ResolveTargetPosition(GameScenario scenario, in IntentTargetRef target)
    {
        if (target.Kind == IntentTargetKind.Position && IsValidPosition(target.Position))
        {
            return target.Position;
        }

        if (target.Kind == IntentTargetKind.Architecture && target.TargetId >= 0)
        {
            Architecture architecture = scenario.Architectures.GetGameObject(target.TargetId) as Architecture;
            if (architecture != null) return architecture.Position;
        }

        if (target.Kind == IntentTargetKind.Troop && target.TargetId >= 0)
        {
            Troop targetTroop = scenario.Troops.GetGameObject(target.TargetId) as Troop;
            if (targetTroop != null && !targetTroop.Destroyed) return targetTroop.Position;
        }

        return target.Position;
    }

    private static bool IsValidPosition(Point position)
    {
        return position.X >= 0 && position.Y >= 0;
    }
}
