using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using PluginInterface;
using System;
using System.Collections.Generic;
using GameFreeText;

namespace tupianwenziPlugin
{

    internal class GameObjectAndBranchName
    {
        internal string branchName;
        internal IConfirmationDialog iConfirmationDialog;
        internal GameDelegates.VoidFunction NoFunction;
        internal Person person;
        internal List<SimpleText> texts = new List<SimpleText>();
        internal GameDelegates.VoidFunction YesFunction;

        internal GameObjectAndBranchName(GameObject   p,  List<SimpleText> list, string name, IConfirmationDialog confirmationDialog, GameDelegates.VoidFunction yesFunction, GameDelegates.VoidFunction noFunction, string TryToShowString = "")
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[GameObjectAndBranchName] 传入对象类型={p?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[GameObjectAndBranchName] 是否Person={p is Person}");
            #endif

            this.person = (p is Person ? (Person)p : null) ;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[GameObjectAndBranchName] person.Name={this.person?.Name ?? "null"}(ID:{this.person?.ID ?? -1})");
            System.Diagnostics.Debug.WriteLine($"[GameObjectAndBranchName] branchName={name}");
            #endif

            this.texts.AddRange(list);
            this.branchName = name;
            this.iConfirmationDialog = confirmationDialog;
            this.YesFunction = yesFunction;
            this.NoFunction = noFunction;
            this.TryToShowString = TryToShowString;
        }

        public string TryToShowString = "";
    }
}


