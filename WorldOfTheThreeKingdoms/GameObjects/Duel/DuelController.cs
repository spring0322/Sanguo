using System;
using GameManager;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelController(
    MainGameScreen gameScreen,
    DuelRequest duelRequest,
    Person left,
    Person right,
    IDuelView duelViewOverride = null)
{
    private readonly MainGameScreen mainGameScreen = gameScreen ?? throw new ArgumentNullException(nameof(gameScreen));
    private readonly DuelRequest request = duelRequest ?? throw new ArgumentNullException(nameof(duelRequest));
    private readonly Person leftPerson = left ?? throw new ArgumentNullException(nameof(left));
    private readonly Person rightPerson = right ?? throw new ArgumentNullException(nameof(right));
    private readonly DuelRuleSet duelRuleSet = DuelRuleSet.Load();
    private readonly DuelRandom duelRandom = new DuelRandom((duelRequest ?? throw new ArgumentNullException(nameof(duelRequest))).Seed);
    private readonly DuelAiPlanner duelAiPlanner = new(DuelRuleSet.Load());
    private readonly DuelPlaybackScriptBuilder playbackScriptBuilder = new();
    private readonly DuelPlaybackScript reusablePlaybackScript = new();
    private readonly IDuelView duelView = duelViewOverride ?? CreateDefaultView(left ?? throw new ArgumentNullException(nameof(left)), right ?? throw new ArgumentNullException(nameof(right)), duelRequest ?? throw new ArgumentNullException(nameof(duelRequest)));
    private readonly DuelSessionState sessionState = CreateSessionState(duelRequest ?? throw new ArgumentNullException(nameof(duelRequest)), left ?? throw new ArgumentNullException(nameof(left)), right ?? throw new ArgumentNullException(nameof(right)), DuelRuleSet.Load());
    private readonly float logicTickSeconds = 1f / Math.Max(1, DuelRuleSet.Load().LogicTicksPerSecond);
    private float logicAccumulator;
    private bool viewBound;
    private bool presentationCompleted;
    private bool hasPendingPlayerCommand;
    private DuelCommand pendingPlayerCommand = DuelCommand.Auto;
    private DuelCommand lastPlayerCommand = DuelCommand.Auto;

    public DuelSessionState SessionState => sessionState;

    public DantiaoLayer LegacyLayer => duelView as DantiaoLayer;

    public bool IsFinished => sessionState.Completed;

    public DuelCommand LastPlayerCommand => lastPlayerCommand;

    public void Start()
    {
        EnsureViewBound();

        if (mainGameScreen.cloudLayer != null)
        {
            mainGameScreen.cloudLayer.Reverse = true;
            mainGameScreen.cloudLayer.Start();
        }

        if (request.Damage != null)
        {
            request.Damage.ChallengeStarted = true;
        }

        mainGameScreen.EnableUpdate = false;
        mainGameScreen.dantiaoLayer = duelView as DantiaoLayer;
        duelView.SyncState(sessionState);
        duelView.Start();
    }

    public void Tick(float seconds)
    {
        if (sessionState.Completed)
        {
            return;
        }

        logicAccumulator += seconds;
        while (logicAccumulator >= logicTickSeconds)
        {
            logicAccumulator -= logicTickSeconds;
            TickLogic();

            if (sessionState.Completed)
            {
                return;
            }
        }
    }

    public void UpdateView(float seconds)
    {
        if (sessionState.Completed)
        {
            return;
        }

        duelView.UpdateView(seconds);
    }

    public void Draw()
    {
        if (sessionState.Completed)
        {
            return;
        }

        duelView.Draw();
    }

    private void EnsureViewBound()
    {
        if (viewBound)
        {
            return;
        }

        duelView.PresentationCompleted += HandlePresentationCompleted;
        duelView.CommandInput += HandleCommandInput;
        viewBound = true;
    }

    private void TickLogic()
    {
        sessionState.LogicTick++;

        switch (sessionState.Stage)
        {
            case DuelStage.Intro:
                TickIntro();
                break;
            case DuelStage.Command:
                TickCommandWindow();
                break;
            case DuelStage.Resolve:
                ResolveRound();
                break;
            case DuelStage.Playback:
                TickPlayback();
                break;
            case DuelStage.Result:
                TickResult();
                break;
            case DuelStage.Exit:
                CompleteDuel();
                break;
            default:
                throw new InvalidOperationException($"Unknown duel stage: {sessionState.Stage}");
        }
    }

    private void TickIntro()
    {
        if (!presentationCompleted)
        {
            return;
        }

        presentationCompleted = false;
        BeginCommandStage();
    }

    private void BeginCommandStage()
    {
        sessionState.Stage = DuelStage.Command;
        sessionState.Round++;
        sessionState.StageTick = 0;
        sessionState.StageTickBudget = duelRuleSet.CommandWindowTicks;
        sessionState.AwaitingPlayerInput = sessionState.LeftSide.IsPlayerControlled || sessionState.RightSide.IsPlayerControlled;
        sessionState.PlaybackRunning = false;

        ResetRoundCommands(sessionState.LeftSide);
        ResetRoundCommands(sessionState.RightSide);
        duelView.SyncState(sessionState);
        duelView.ShowCommandWindow(sessionState.StageTickBudget);
    }

    private void TickCommandWindow()
    {
        sessionState.StageTick++;
        ApplyPendingPlayerCommand();
        LockAiCommandIfNeeded(sessionState.LeftSide, true);
        LockAiCommandIfNeeded(sessionState.RightSide, false);

        int remainingTicks = Math.Max(0, sessionState.StageTickBudget - sessionState.StageTick);
        LockTimeoutCommandIfNeeded(sessionState.LeftSide, remainingTicks);
        LockTimeoutCommandIfNeeded(sessionState.RightSide, remainingTicks);
        duelView.UpdateCommandWindow(remainingTicks);
        duelView.SyncState(sessionState);

        if (!sessionState.LeftSide.CommandLocked || !sessionState.RightSide.CommandLocked)
        {
            return;
        }

        sessionState.AwaitingPlayerInput = false;
        sessionState.Stage = DuelStage.Resolve;
        duelView.HideCommandWindow();
    }

    private void ResolveRound()
    {
        DuelExchangeResult exchangeResult = DuelResolver.ResolveExchange(
            sessionState,
            sessionState.LeftSide.LockedCommand,
            sessionState.RightSide.LockedCommand,
            duelRandom,
            duelRuleSet);

        ApplyExchangeResult(sessionState.LeftSide, exchangeResult.LeftCommand, exchangeResult.RightDamageDealt);
        ApplyExchangeResult(sessionState.RightSide, exchangeResult.RightCommand, exchangeResult.LeftDamageDealt);
        sessionState.Outcome = exchangeResult.OutcomeAfterExchange;
        sessionState.Stage = DuelStage.Playback;
        sessionState.StageTick = 0;
        sessionState.StageTickBudget = 0;
        sessionState.PlaybackRunning = true;
        presentationCompleted = false;

        playbackScriptBuilder.Build(reusablePlaybackScript, sessionState, exchangeResult);
        duelView.SyncState(sessionState);
        duelView.PlayScript(reusablePlaybackScript);
    }

    private void TickPlayback()
    {
        if (!presentationCompleted)
        {
            return;
        }

        presentationCompleted = false;
        sessionState.PlaybackRunning = false;

        if (sessionState.Outcome == DuelOutcome.None)
        {
            BeginCommandStage();
            return;
        }

        BeginResultStage();
    }

    private void BeginResultStage()
    {
        sessionState.Stage = DuelStage.Result;
        sessionState.StageTick = 0;
        sessionState.StageTickBudget = duelRuleSet.ResultDisplayTicks;
        presentationCompleted = false;
        duelView.ShowResult(sessionState.Outcome, BuildResultTitle(sessionState.Outcome));
    }

    private void TickResult()
    {
        sessionState.StageTick++;
        if (sessionState.StageTick < sessionState.StageTickBudget || !presentationCompleted)
        {
            return;
        }

        presentationCompleted = false;
        sessionState.Stage = DuelStage.Exit;
        CompleteDuel();
    }

    private void CompleteDuel()
    {
        if (sessionState.Completed)
        {
            return;
        }

        sessionState.Completed = true;
        sessionState.Stage = DuelStage.Exit;

        DuelResult result = DuelResolver.BuildResult(request, leftPerson, rightPerson, sessionState, 0);
        DuelCommitBridge.Complete(mainGameScreen, request, result, leftPerson, rightPerson);
    }

    private void ApplyPendingPlayerCommand()
    {
        if (!hasPendingPlayerCommand)
        {
            return;
        }

        if (sessionState.LeftSide.IsPlayerControlled && !sessionState.LeftSide.CommandLocked)
        {
            LockCommand(sessionState.LeftSide, pendingPlayerCommand);
        }

        if (sessionState.RightSide.IsPlayerControlled && !sessionState.RightSide.CommandLocked)
        {
            LockCommand(sessionState.RightSide, pendingPlayerCommand);
        }

        hasPendingPlayerCommand = false;
    }

    private void LockAiCommandIfNeeded(DuelSideState sideState, bool isLeftSide)
    {
        if (sideState.IsPlayerControlled || sideState.CommandLocked)
        {
            return;
        }

        LockCommand(sideState, duelAiPlanner.ChooseCommand(sessionState, isLeftSide, duelRandom));
    }

    private void LockTimeoutCommandIfNeeded(DuelSideState sideState, int remainingTicks)
    {
        if (!sideState.IsPlayerControlled || sideState.CommandLocked || remainingTicks > 0)
        {
            return;
        }

        LockCommand(sideState, DuelCommand.Auto);
    }

    private static void LockCommand(DuelSideState sideState, in DuelCommand command)
    {
        sideState.LockedCommand = command;
        sideState.CommandLocked = true;
    }

    private static void ResetRoundCommands(DuelSideState sideState)
    {
        sideState.LockedCommand = DuelCommand.Auto;
        sideState.CommandLocked = false;
    }

    private static void ApplyExchangeResult(DuelSideState sideState, in DuelCommand command, float damageTaken)
    {
        sideState.LastCommand = command;
        sideState.CommandLocked = false;
        sideState.CurrentLife = Math.Max(0f, sideState.CurrentLife - damageTaken);
    }

    private void HandlePresentationCompleted()
    {
        presentationCompleted = true;
    }

    private void HandleCommandInput(DuelCommand command)
    {
        lastPlayerCommand = command;
        pendingPlayerCommand = command;
        hasPendingPlayerCommand = true;
    }

    private static IDuelView CreateDefaultView(Person left, Person right, DuelRequest request)
    {
        return new DantiaoLayer(left, right, request.Mode == DuelMode.Demo, request.Seed)
        {
            damage = request.Damage,
            UseExternalLifecycle = true
        };
    }

    private static DuelSessionState CreateSessionState(DuelRequest request, Person left, Person right, DuelRuleSet duelRuleSet)
    {
        DuelParticipantSnapshot leftSnapshot = DuelParticipantSnapshot.FromPerson(left);
        DuelParticipantSnapshot rightSnapshot = DuelParticipantSnapshot.FromPerson(right);
        bool isDemoMode = IsDemoMode(request);
        bool leftPlayerControlled = !isDemoMode && IsPlayerControlledSide(left);
        bool rightPlayerControlled = isDemoMode || (!leftPlayerControlled && IsPlayerControlledSide(right));

        return new DuelSessionState
        {
            Mode = request.Mode,
            Seed = request.Seed,
            Left = leftSnapshot,
            Right = rightSnapshot,
            LeftSide = new DuelSideState
            {
                Snapshot = leftSnapshot,
                CurrentLife = duelRuleSet.StartingLife,
                MaxLife = duelRuleSet.StartingLife,
                IsPlayerControlled = leftPlayerControlled
            },
            RightSide = new DuelSideState
            {
                Snapshot = rightSnapshot,
                CurrentLife = duelRuleSet.StartingLife,
                MaxLife = duelRuleSet.StartingLife,
                IsPlayerControlled = rightPlayerControlled
            },
            Stage = DuelStage.Intro,
            Outcome = DuelOutcome.None,
            Round = 0,
            LogicTick = 0,
            StageTick = 0,
            StageTickBudget = 0,
            AwaitingPlayerInput = false,
            PlaybackRunning = false,
            Completed = false
        };
    }

    private static bool IsDemoMode(DuelRequest request)
    {
        return request.Mode == DuelMode.Demo;
    }

    private static bool IsPlayerControlledSide(Person person)
    {
        if (person == null || person.BelongedFaction == null || Session.Current?.Scenario == null)
        {
            return false;
        }

        if (Session.Current.Scenario.CurrentPlayer != null)
        {
            return Session.Current.Scenario.IsCurrentPlayer(person.BelongedFaction);
        }

        return Session.Current.Scenario.IsPlayer(person.BelongedFaction);
    }

    private static string BuildResultTitle(DuelOutcome outcome)
    {
        return outcome switch
        {
            DuelOutcome.LeftWin => "左方获胜",
            DuelOutcome.RightWin => "右方获胜",
            DuelOutcome.LeftDead => "左方阵亡",
            DuelOutcome.RightDead => "右方阵亡",
            DuelOutcome.Draw => "双方平局",
            _ => "单挑结束"
        };
    }
}
