namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战略行动枚举
/// 日期：2026-03-11
/// </summary>
public enum StrategicAction : byte
{
    /// <summary>无所事事/攒钱</summary>
    Idle = 0,
    
    /// <summary>开垦农田</summary>
    DevelopAgriculture = 1,
    
    /// <summary>发展商业</summary>
    DevelopCommerce = 2,
    
    /// <summary>招募士兵</summary>
    DraftTroops = 3,
    
    /// <summary>训练士气</summary>
    TrainTroops = 4,
    
    /// <summary>发起出征</summary>
    LaunchCampaign = 5
}
