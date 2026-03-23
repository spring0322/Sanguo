using System;


namespace GameObjects.TroopDetail.EventEffect
{

    public class EventEffectKindFactory
    {
        public static EventEffectKind CreateEventEffectKindByID(int id)
        {
            switch (id)
            {
                case 0: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind0();
                case 1: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind1();
                case 10: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind10();
                case 15: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind15();
                case 20: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind20();
                case 25: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind25();
                case 30: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind30();
                case 35: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind35();
                case 40: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind40();
                case 45: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind45();
                case 50: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind50();
                case 60: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind60();
                case 80: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind80();
                case 100: return new GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind100();
                default: return null;
            }
        }
    }
}

