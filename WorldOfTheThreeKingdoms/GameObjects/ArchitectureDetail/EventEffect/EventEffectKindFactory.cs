using System;
using System.Runtime.Serialization;

namespace GameObjects.ArchitectureDetail.EventEffect
{
    [DataContract]
    public class EventEffectKindFactory
    {
        public static EventEffectKind CreateEventEffectKindByID(int id)
        {
            switch (id)
            {
                case 0: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect0();
                case 10: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect10();
                case 100: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect100();
                case 1000: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1000();
                case 1010: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1010();
                case 1020: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1020();
                case 1030: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1030();
                case 1040: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1040();
                case 1050: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1050();
                case 1060: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1060();
                case 1070: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1070();
                case 1080: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1080();
                case 1090: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1090();
                case 110: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect110();
                case 1100: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1100();
                case 1110: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1110();
                case 1120: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1120();
                case 1130: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1130();
                case 1140: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1140();
                case 1150: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1150();
                case 120: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect120();
                case 1200: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1200();
                case 1210: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1210();
                case 1220: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1220();
                case 1230: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1230();
                case 1240: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1240();
                case 1250: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1250();
                case 130: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect130();
                case 1300: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1300();
                case 1310: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1310();
                case 140: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect140();
                case 1400: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect1400();
                case 15: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect15();
                case 150: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect150();
                case 160: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect160();
                case 170: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect170();
                case 20: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect20();
                case 200: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect200();
                case 2000: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2000();
                case 2010: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2010();
                case 2020: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2020();
                case 2030: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2030();
                case 2040: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2040();
                case 2050: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2050();
                case 210: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect210();
                case 2100: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2100();
                case 211: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect211();
                case 2110: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2110();
                case 212: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect212();
                case 2120: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2120();
                case 213: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect213();
                case 2130: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2130();
                case 2131: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2131();
                case 214: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect214();
                case 215: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect215();
                case 216: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect216();
                case 217: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect217();
                case 220: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect220();
                case 2200: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2200();
                case 221: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect221();
                case 222: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect222();
                case 223: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect223();
                case 224: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect224();
                case 225: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect225();
                case 226: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect226();
                case 227: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect227();
                case 230: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect230();
                case 2300: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect2300();
                case 235: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect235();
                case 240: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect240();
                case 25: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect25();
                case 250: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect250();
                case 270: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect270();
                case 275: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect275();
                case 280: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect280();
                case 290: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect290();
                case 30: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect30();
                case 300: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect300();
                case 305: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect305();
                case 310: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect310();
                case 315: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect315();
                case 320: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect320();
                case 325: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect325();
                case 330: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect330();
                case 35: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect35();
                case 350: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect350();
                case 355: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect355();
                case 40: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect40();
                case 400: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect400();
                case 4000: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect4000();
                case 410: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect410();
                case 420: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect420();
                case 430: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect430();
                case 440: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect440();
                case 45: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect45();
                case 450: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect450();
                case 460: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect460();
                case 465: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect465();
                case 466: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect466();
                case 470: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect470();
                case 480: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect480();
                case 5: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect5();
                case 50: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect50();
                case 500: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect500();
                case 510: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect510();
                case 60: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect60();
                case 600: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect600();
                case 650: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect650();
                case 70: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect70();
                case 700: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect700();
                case 800: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect800();
                case 810: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect810();
                case 820: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect820();
                case 821: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect821();
                case 822: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect822();
                case 823: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect823();
                case 824: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect824();
                case 825: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect825();
                case 826: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect826();
                case 827: return new GameObjects.ArchitectureDetail.EventEffect.EventEffect827();
                default: return null;
            }
        }
    }
}
