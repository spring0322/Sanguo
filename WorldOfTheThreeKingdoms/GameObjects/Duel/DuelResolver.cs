using GameObjects;
using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public static class DuelResolver
{
    public static DuelOutcome ResolveOutcomeFromLegacyResult(int legacyResult)
    {
        return legacyResult switch
        {
            -1 => DuelOutcome.Draw,
            1 => DuelOutcome.LeftWin,
            2 => DuelOutcome.RightWin,
            3 => DuelOutcome.LeftDead,
            4 => DuelOutcome.RightDead,
            5 => DuelOutcome.RightWin,
            6 => DuelOutcome.LeftWin,
            _ => DuelOutcome.None
        };
    }

    public static int ResolveLegacyResult(DuelOutcome outcome, int fallbackLegacyResult = 0)
    {
        if (fallbackLegacyResult != 0)
        {
            return fallbackLegacyResult;
        }

        return outcome switch
        {
            DuelOutcome.LeftWin => 1,
            DuelOutcome.RightWin => 2,
            DuelOutcome.Draw => -1,
            DuelOutcome.LeftDead => 3,
            DuelOutcome.RightDead => 4,
            _ => 0
        };
    }

    public static DuelStage ResolveStageFromLegacy(string legacyStage)
    {
        if (string.IsNullOrEmpty(legacyStage))
        {
            return DuelStage.Intro;
        }

        return legacyStage switch
        {
            "Cloud" => DuelStage.Intro,
            "Start" => DuelStage.Intro,
            "Gen1Move" => DuelStage.Intro,
            "Gen1Moving" => DuelStage.Intro,
            "Gen1Speak" => DuelStage.Intro,
            "Gen1Speaking" => DuelStage.Intro,
            "Gen1Run" => DuelStage.Intro,
            "Gen1Running" => DuelStage.Intro,
            "Gen2Speak" => DuelStage.Intro,
            "Gen2Speaking" => DuelStage.Intro,
            "Gen2Run" => DuelStage.Intro,
            "Gen2Running" => DuelStage.Intro,
            "WaitRush" => DuelStage.Resolve,
            "FightRush" => DuelStage.Resolve,
            "FightRun" => DuelStage.Resolve,
            "FightRunStop" => DuelStage.Resolve,
            "FightRunBack" => DuelStage.Resolve,
            "Fighting" => DuelStage.Resolve,
            "Over" => DuelStage.Result,
            "OverOut" => DuelStage.Exit,
            _ => DuelStage.Playback
        };
    }

    public static DuelResult BuildResult(DuelRequest request, Person left, Person right, DuelSessionState sessionState, int legacyResult)
    {
        DuelOutcome outcome = sessionState?.Outcome ?? ResolveOutcomeFromLegacyResult(legacyResult);
        int finalLegacyResult = ResolveLegacyResult(outcome, legacyResult);

        return new DuelResult
        {
            Outcome = outcome,
            LegacyResult = finalLegacyResult,
            RoundCount = sessionState?.Round ?? 0,
            LeftPersonId = left?.ID ?? request?.LeftPersonId ?? -1,
            RightPersonId = right?.ID ?? request?.RightPersonId ?? -1,
            Seed = request?.Seed ?? 0
        };
    }

    public static DuelExchangeResult ResolveExchange(DuelSessionState sessionState, in DuelCommand leftCommand, in DuelCommand rightCommand, DuelRandom duelRandom, DuelRuleSet ruleSet)
    {
        if (sessionState == null)
        {
            throw new ArgumentNullException(nameof(sessionState));
        }

        if (duelRandom == null)
        {
            throw new ArgumentNullException(nameof(duelRandom));
        }

        if (ruleSet == null)
        {
            throw new ArgumentNullException(nameof(ruleSet));
        }

        DuelCommand normalizedLeft = NormalizeCommand(leftCommand);
        DuelCommand normalizedRight = NormalizeCommand(rightCommand);
        DuelCommandType leftType = normalizedLeft.CommandType;
        DuelCommandType rightType = normalizedRight.CommandType;
        bool leftActsFirst = ResolveInitiative(leftType, rightType, duelRandom);
        bool leftCritical = duelRandom.Next(0, 100) < ruleSet.CriticalChancePercent;
        bool rightCritical = duelRandom.Next(0, 100) < ruleSet.CriticalChancePercent;

        float rawLeftDamage = ResolveBaseDamage(sessionState.LeftSide.Snapshot.Force, duelRandom, ruleSet);
        float rawRightDamage = ResolveBaseDamage(sessionState.RightSide.Snapshot.Force, duelRandom, ruleSet);
        float leftDamage = rawLeftDamage * ResolveAttackModifier(leftType, ruleSet) * ResolveDefenseModifier(rightType, ruleSet);
        float rightDamage = rawRightDamage * ResolveAttackModifier(rightType, ruleSet) * ResolveDefenseModifier(leftType, ruleSet);

        if (leftCritical)
        {
            leftDamage *= ruleSet.CriticalDamageMultiplier;
        }

        if (rightCritical)
        {
            rightDamage *= ruleSet.CriticalDamageMultiplier;
        }

        bool leftBlocked = rightType == DuelCommandType.Defensive;
        bool rightBlocked = leftType == DuelCommandType.Defensive;
        float leftRemainingLife = Math.Max(0f, sessionState.LeftSide.CurrentLife - rightDamage);
        float rightRemainingLife = Math.Max(0f, sessionState.RightSide.CurrentLife - leftDamage);
        DuelOutcome outcome = ResolveOutcome(leftRemainingLife, rightRemainingLife);

        return new DuelExchangeResult(
            normalizedLeft,
            normalizedRight,
            leftActsFirst,
            leftDamage,
            rightDamage,
            leftBlocked,
            rightBlocked,
            leftCritical,
            rightCritical,
            false,
            false,
            outcome);
    }

    private static DuelCommand NormalizeCommand(in DuelCommand command)
    {
        return command.CommandType == DuelCommandType.Auto
            ? new DuelCommand(DuelCommandType.Normal, command.TargetId, command.Payload, command.FrameLock)
            : command;
    }

    private static bool ResolveInitiative(DuelCommandType leftType, DuelCommandType rightType, DuelRandom duelRandom)
    {
        int leftPriority = leftType switch
        {
            DuelCommandType.Aggressive => 2,
            DuelCommandType.Normal => 1,
            DuelCommandType.Defensive => 0,
            _ => 1
        };

        int rightPriority = rightType switch
        {
            DuelCommandType.Aggressive => 2,
            DuelCommandType.Normal => 1,
            DuelCommandType.Defensive => 0,
            _ => 1
        };

        if (leftPriority == rightPriority)
        {
            return duelRandom.Next(0, 2) == 0;
        }

        return leftPriority > rightPriority;
    }

    private static float ResolveBaseDamage(int force, DuelRandom duelRandom, DuelRuleSet ruleSet)
    {
        int upperExclusive = Math.Max(2, force / Math.Max(1, ruleSet.BaseDamageDivisor) + 1);
        float rolled = duelRandom.Next(0, upperExclusive) * ruleSet.BaseDamageScale;
        return Math.Max(ruleSet.MinimumDamage, rolled);
    }

    private static float ResolveAttackModifier(DuelCommandType commandType, DuelRuleSet ruleSet)
    {
        return commandType switch
        {
            DuelCommandType.Aggressive => ruleSet.AggressiveAttackModifier,
            DuelCommandType.Defensive => ruleSet.DefensiveAttackModifier,
            _ => 1f
        };
    }

    private static float ResolveDefenseModifier(DuelCommandType commandType, DuelRuleSet ruleSet)
    {
        return commandType switch
        {
            DuelCommandType.Aggressive => ruleSet.AggressiveDefenseModifier,
            DuelCommandType.Defensive => ruleSet.DefensiveDefenseModifier,
            _ => 1f
        };
    }

    private static DuelOutcome ResolveOutcome(float leftRemainingLife, float rightRemainingLife)
    {
        if (leftRemainingLife <= 0f && rightRemainingLife <= 0f)
        {
            return DuelOutcome.Draw;
        }

        if (rightRemainingLife <= 0f)
        {
            return DuelOutcome.LeftWin;
        }

        if (leftRemainingLife <= 0f)
        {
            return DuelOutcome.RightWin;
        }

        return DuelOutcome.None;
    }
}
