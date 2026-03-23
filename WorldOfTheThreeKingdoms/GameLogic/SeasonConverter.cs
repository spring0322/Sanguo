using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 季节转换辅助类（GameSeason → SeasonType）
/// 日期：2026-03-09
/// </summary>
public static class SeasonConverter
{
    /// <summary>
    /// 将游戏季节转换为天气系统的季节类型
    /// </summary>
    public static SeasonType ToSeasonType(GameSeason gameSeason)
    {
        return gameSeason switch
        {
            GameSeason.春 => SeasonType.Spring,
            GameSeason.夏 => SeasonType.Summer,
            GameSeason.秋 => SeasonType.Autumn,
            GameSeason.冬 => SeasonType.Winter,
            _ => SeasonType.Spring // 默认春季
        };
    }

    /// <summary>
    /// 将天气系统的季节类型转换为游戏季节
    /// </summary>
    public static GameSeason ToGameSeason(SeasonType seasonType)
    {
        return seasonType switch
        {
            SeasonType.Spring => GameSeason.春,
            SeasonType.Summer => GameSeason.夏,
            SeasonType.Autumn => GameSeason.秋,
            SeasonType.Winter => GameSeason.冬,
            _ => GameSeason.春
        };
    }
}
