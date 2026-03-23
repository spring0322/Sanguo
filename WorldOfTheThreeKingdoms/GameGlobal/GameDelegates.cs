using System;
using System.Runtime.CompilerServices;



namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public class GameDelegates
    {
        public delegate void ObjectFunction(object obj);

        public delegate void StringFunction(string s);

        public delegate void VoidFunction();
    }
}

