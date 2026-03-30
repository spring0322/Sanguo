using System.Diagnostics;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen
    {
        internal void TryShowDeferredTupianwenziDialogs()
        {
            if (IsTupianwenziDeferralActive())
            {
                return;
            }

            if (this.Plugins?.tupianwenziPlugin is not tupianwenziPlugin.tupianwenziPlugin plugin)
            {
                return;
            }

            if (plugin.IsShowing || plugin.tupianwenzi.DisplayQueue.Count <= 0)
            {
                return;
            }

#if DEBUG
            Debug.WriteLine($"[TupianwenziDeferral] Flushing deferred dialogs: count={plugin.tupianwenzi.DisplayQueue.Count}");
#endif
            plugin.IsShowing = true;
        }

        internal static bool IsTupianwenziDeferralActive()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null)
            {
                return false;
            }

            return scenario.Threading || scenario.Date?.IsRunning == true;
        }
    }
}
