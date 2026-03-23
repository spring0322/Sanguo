using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 风向数学计算工具类
/// 日期：2026-03-10
/// </summary>
public static class WindMath
{
    /// <summary>
    /// 获取当前地块在特定风向下的"下风口"相邻坐标
    /// 🔥 HOT PATH：使用 AggressiveInlining 优化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point GetDownwindTile(Point origin, WindDirection direction)
    {
        // 🔥 C# 12 switch 表达式，返回目标坐标
        // 假设地图原点 (0,0) 在左上角，X 向右，Y 向下
        return direction switch
        {
            WindDirection.North => new(origin.X, origin.Y + 1),      // 北风往南吹 (Y+)
            WindDirection.South => new(origin.X, origin.Y - 1),      // 南风往北吹 (Y-)
            WindDirection.East  => new(origin.X - 1, origin.Y),      // 东风往西吹 (X-)
            WindDirection.West  => new(origin.X + 1, origin.Y),      // 西风往东吹 (X+)
            WindDirection.NorthWest => new(origin.X + 1, origin.Y + 1), // 西北风往东南吹
            WindDirection.NorthEast => new(origin.X - 1, origin.Y + 1), // 东北风往西南吹
            WindDirection.SouthWest => new(origin.X + 1, origin.Y - 1), // 西南风往东北吹
            WindDirection.SouthEast => new(origin.X - 1, origin.Y - 1), // 东南风往西北吹
            _ => origin // 无风或未知，原地不动
        };
    }
}
