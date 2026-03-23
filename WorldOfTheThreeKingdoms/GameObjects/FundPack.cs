using System;


namespace GameObjects
{

    public class FundPack
    {
        public int Days;
        public int Fund;

        private FundPack() { }
        public FundPack(int fund, int days)
        {
            this.Fund = fund;
            this.Days = days;
        }
    }
}

