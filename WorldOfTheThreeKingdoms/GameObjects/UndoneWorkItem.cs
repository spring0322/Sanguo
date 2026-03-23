using WorldOfTheThreeKingdoms.GameGlobal;
using System;
using System.Runtime.InteropServices;


namespace GameObjects
{

    [StructLayout(LayoutKind.Sequential)]
    public struct UndoneWorkItem
    {
        public UndoneWorkKind Kind;
        public int SubKind;
        public UndoneWorkItem(UndoneWorkKind kind, Enum subKind)
        {
            this.Kind = kind;
            this.SubKind = Convert.ToInt32(subKind);
        }

        public override string ToString()
        {
            return (this.Kind.ToString() + "_" + this.SubKind.ToString());
        }
    }
}

