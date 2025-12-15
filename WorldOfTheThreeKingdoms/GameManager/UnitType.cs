using System;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // 单位类型枚举
    public enum UnitType
    {
        Infantry,    // 步兵
        Cavalry,     // 骑兵
        Archer,      // 弓兵
        Siege,       // 攻城器械
        Naval,       // 水军
        
        // 中文别名
        步兵 = Infantry,
        骑兵 = Cavalry,
        弓兵 = Archer,
        攻城器械 = Siege,
        水军 = Naval
    }
}