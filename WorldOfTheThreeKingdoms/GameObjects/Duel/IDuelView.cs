using System;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public interface IDuelView
{
    event Action PresentationCompleted;

    event Action<DuelCommand> CommandInput;

    void Start();

    void SyncState(DuelSessionState sessionState);

    void ShowCommandWindow(int remainingTicks);

    void UpdateCommandWindow(int remainingTicks);

    void HideCommandWindow();

    void PlayScript(DuelPlaybackScript script);

    void ShowResult(DuelOutcome outcome, string title);

    void UpdateView(float seconds);

    void Draw();
}
