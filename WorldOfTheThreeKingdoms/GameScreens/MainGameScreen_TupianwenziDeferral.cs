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

            if (Setting.Current.GlobalVariables.DialogShowTime <= 0)
            {
                if (plugin.tupianwenzi.DisplayQueue.Count > 0 &&
                    plugin.tupianwenzi.CanDiscardDeferredDialogsWhenHidden())
                {
#if DEBUG
                    Debug.WriteLine($"[TupianwenziDeferral] Discarding hidden deferred dialogs: count={plugin.tupianwenzi.DisplayQueue.Count}");
#endif
                    plugin.tupianwenzi.ClearDeferredDialogs();
                }
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
