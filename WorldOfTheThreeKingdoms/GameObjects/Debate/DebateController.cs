using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateController
{
    private readonly DebateRequest request;
    private readonly DebateRuleSet ruleSet;
    private readonly DebateRandom debateRandom;
    private readonly float logicTickSeconds;
    private float logicAccumulator;
    private bool started;

    public DebateController(DebateRequest debateRequest, DebateRuleSet debateRuleSet = null)
    {
        request = debateRequest ?? throw new ArgumentNullException(nameof(debateRequest));
        ruleSet = debateRuleSet ?? DebateRuleSet.Load();
        debateRandom = new DebateRandom(request.Seed);
        logicTickSeconds = 1f / Math.Max(1, ruleSet.LogicTicksPerSecond);
        SessionState = CreateSessionState(request, ruleSet);
    }

    public DebateRequest Request => request;

    public DebateRuleSet RuleSet => ruleSet;

    public DebateRandom Random => debateRandom;

    public DebateSessionState SessionState { get; }

    public bool IsFinished => SessionState.Completed;

    public void Start()
    {
        if (started || SessionState.Completed)
        {
            return;
        }

        started = true;
        SessionState.Stage = DebateStage.Intro;
        SessionState.Outcome = DebateOutcome.None;
        SessionState.Round = 0;
        SessionState.LogicTick = 0;
        SessionState.StageTick = 0;
        SessionState.StageTickBudget = 0;
        SessionState.AwaitingPlayerInput = false;
        SessionState.PlaybackRunning = false;
        SessionState.Completed = false;
    }

    public void Tick(float seconds)
    {
        if (!started || SessionState.Completed || seconds <= 0f)
        {
            return;
        }

        logicAccumulator += seconds;
        while (logicAccumulator >= logicTickSeconds)
        {
            logicAccumulator -= logicTickSeconds;
            TickLogic();

            if (SessionState.Completed)
            {
                return;
            }
        }
    }

    public bool SubmitCommand(bool isLeftSide, in DebateCommand command)
    {
        if (!started || SessionState.Completed || SessionState.Stage != DebateStage.Command)
        {
            return false;
        }

        DebateSideState sideState = isLeftSide ? SessionState.LeftSide : SessionState.RightSide;
        if (sideState.CommandLocked)
        {
            return false;
        }

        sideState.LockedCommand = NormalizeCommand(command);
        sideState.CommandLocked = true;
        return true;
    }

    public void ForceFinish(DebateOutcome outcome = DebateOutcome.None)
    {
        if (SessionState.Completed)
        {
            return;
        }

        SessionState.Outcome = outcome;
        BeginResultStage();
    }

    public DebateResult BuildResult()
    {
        return new DebateResult
        {
            Outcome = SessionState.Outcome,
            RoundCount = SessionState.Round,
            LeftPersonId = SessionState.LeftSide.Snapshot.PersonId,
            RightPersonId = SessionState.RightSide.Snapshot.PersonId,
            Seed = SessionState.Seed
        };
    }

    private void TickLogic()
    {
        SessionState.LogicTick++;

        switch (SessionState.Stage)
        {
            case DebateStage.Intro:
                BeginCommandStage();
                break;
            case DebateStage.Command:
                TickCommandStage();
                break;
            case DebateStage.Resolve:
                ResolveRoundSkeleton();
                break;
            case DebateStage.Playback:
                TickPlaybackStage();
                break;
            case DebateStage.Result:
                TickResultStage();
                break;
            case DebateStage.Exit:
                SessionState.Completed = true;
                break;
            default:
                throw new InvalidOperationException($"Unknown debate stage: {SessionState.Stage}");
        }
    }

    private void BeginCommandStage()
    {
        if (SessionState.Outcome != DebateOutcome.None || SessionState.Completed)
        {
            BeginResultStage();
            return;
        }

        SessionState.Stage = DebateStage.Command;
        SessionState.Round++;
        SessionState.StageTick = 0;
        SessionState.StageTickBudget = ruleSet.CommandWindowTicks;
        SessionState.AwaitingPlayerInput = SessionState.LeftSide.IsPlayerControlled || SessionState.RightSide.IsPlayerControlled;
        SessionState.PlaybackRunning = false;

        ResetRoundCommand(SessionState.LeftSide);
        ResetRoundCommand(SessionState.RightSide);
    }

    private void TickCommandStage()
    {
        SessionState.StageTick++;
        int remainingTicks = Math.Max(0, SessionState.StageTickBudget - SessionState.StageTick);

        if (remainingTicks == 0)
        {
            LockTimeoutCommandIfNeeded(SessionState.LeftSide);
            LockTimeoutCommandIfNeeded(SessionState.RightSide);
        }

        if (!SessionState.LeftSide.CommandLocked || !SessionState.RightSide.CommandLocked)
        {
            return;
        }

        SessionState.Stage = DebateStage.Resolve;
        SessionState.StageTick = 0;
        SessionState.StageTickBudget = 0;
        SessionState.AwaitingPlayerInput = false;
    }

    private void ResolveRoundSkeleton()
    {
        ApplyRoundCommand(SessionState.LeftSide);
        ApplyRoundCommand(SessionState.RightSide);

        SessionState.Stage = DebateStage.Playback;
        SessionState.StageTick = 0;
        SessionState.StageTickBudget = ruleSet.PlaybackTicks;
        SessionState.PlaybackRunning = true;

        if (SessionState.Round >= ruleSet.MaxRounds && SessionState.Outcome == DebateOutcome.None)
        {
            SessionState.Outcome = DebateOutcome.Draw;
        }
    }

    private void TickPlaybackStage()
    {
        SessionState.StageTick++;
        if (SessionState.StageTick < SessionState.StageTickBudget)
        {
            return;
        }

        SessionState.PlaybackRunning = false;
        if (SessionState.Outcome == DebateOutcome.None)
        {
            BeginCommandStage();
            return;
        }

        BeginResultStage();
    }

    private void BeginResultStage()
    {
        SessionState.Stage = DebateStage.Result;
        SessionState.StageTick = 0;
        SessionState.StageTickBudget = ruleSet.ResultDisplayTicks;
        SessionState.PlaybackRunning = false;
        SessionState.AwaitingPlayerInput = false;
    }

    private void TickResultStage()
    {
        SessionState.StageTick++;
        if (SessionState.StageTick < SessionState.StageTickBudget)
        {
            return;
        }

        SessionState.Stage = DebateStage.Exit;
        SessionState.Completed = true;
    }

    private static DebateCommand NormalizeCommand(in DebateCommand command)
    {
        return command.CommandType == DebateCommandType.Auto
            ? DebateCommand.Auto
            : command;
    }

    private static void LockTimeoutCommandIfNeeded(DebateSideState sideState)
    {
        if (sideState.CommandLocked)
        {
            return;
        }

        sideState.LockedCommand = DebateCommand.Auto;
        sideState.CommandLocked = true;
    }

    private static void ResetRoundCommand(DebateSideState sideState)
    {
        sideState.LockedCommand = DebateCommand.Auto;
        sideState.CommandLocked = false;
    }

    private static void ApplyRoundCommand(DebateSideState sideState)
    {
        sideState.LastCommand = sideState.LockedCommand;
        sideState.CommandLocked = false;
    }

    private static DebateSessionState CreateSessionState(DebateRequest request, DebateRuleSet ruleSet)
    {
        return new DebateSessionState
        {
            Mode = request.Mode,
            Seed = request.Seed,
            LeftSide = new DebateSideState
            {
                Snapshot = request.Left,
                CurrentMomentum = ruleSet.StartingMomentum,
                MaxMomentum = ruleSet.StartingMomentum,
                CurrentFocus = Math.Clamp(ruleSet.StartingFocus, 0, ruleSet.MaxFocus),
                MaxFocus = ruleSet.MaxFocus,
                IsPlayerControlled = request.Left.IsPlayerControlled
            },
            RightSide = new DebateSideState
            {
                Snapshot = request.Right,
                CurrentMomentum = ruleSet.StartingMomentum,
                MaxMomentum = ruleSet.StartingMomentum,
                CurrentFocus = Math.Clamp(ruleSet.StartingFocus, 0, ruleSet.MaxFocus),
                MaxFocus = ruleSet.MaxFocus,
                IsPlayerControlled = request.Right.IsPlayerControlled
            },
            Stage = DebateStage.Intro,
            Outcome = DebateOutcome.None,
            Round = 0,
            LogicTick = 0,
            StageTick = 0,
            StageTickBudget = 0,
            AwaitingPlayerInput = false,
            PlaybackRunning = false,
            Completed = false
        };
    }
}

