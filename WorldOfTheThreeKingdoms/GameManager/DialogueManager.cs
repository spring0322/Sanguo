using System;
using System.Collections.Generic;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师任命对话管理器 (遗留代码适配器)
    /// </summary>
    public static class AdvisorAppointmentDialogueManager
    {
        public static void Initialize()
        {
            // 重定向到 GameObjects.DialogueManager
            global::GameObjects.DialogueManager.Initialize();
        }

        public static WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry GetAppointDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            if (isRefusal)
            {
                return global::GameObjects.DialogueManager.GetRefusalDialogue(leader, advisor);
            }
            return global::GameObjects.DialogueManager.GetAppointDialogue(leader, advisor);
        }

        public static WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry GetRecallDialogue(Person leader, Person advisor)
        {
            return global::GameObjects.DialogueManager.GetRecallDialogue(leader, advisor);
        }

        public static WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry GetDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            return GetAppointDialogue(leader, advisor, isRefusal);
        }

        public static void ShowAppointDialogue(Person leader, Person advisor, MainGameScreen gameScreen)
        {
            global::GameObjects.DialogueManager.ShowAppointDialogue(leader, advisor, gameScreen);
        }

        public static void ShowRecallDialogue(Person leader, Person advisor, MainGameScreen gameScreen)
        {
            global::GameObjects.DialogueManager.ShowRecallDialogue(leader, advisor, gameScreen);
        }

        public static void ReloadConfigs()
        {
            global::GameObjects.DialogueManager.ReloadConfigs();
        }
    }
}

