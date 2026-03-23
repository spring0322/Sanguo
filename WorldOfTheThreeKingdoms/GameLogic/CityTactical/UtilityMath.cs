using System;
using System.Runtime.CompilerServices;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 效用函数数学库 - 高性能内联计算
/// 日期：2026-03-11
/// </summary>
public static class UtilityMath
{
    /// <summary>
    /// 逻辑斯蒂反转曲线 (越缺越渴望)
    /// inputValue: 当前值饱和度 (0~1)
    /// sharpness: 曲线陡峭程度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InverseLogistic(float inputValue, float sharpness)
    {
        // 简单的高效多项式替代复杂 Exp，保持 0~1 的输出
        float val = Math.Clamp(inputValue, 0f, 1f);
        return 1.0f - (float)Math.Pow(val, sharpness);
    }

    /// <summary>
    /// 抛物线型渴望 (处于某个中间值时最渴望)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BellCurve(float inputValue, float optimalValue = 0.5f)
    {
        float val = Math.Clamp(inputValue, 0f, 1f);
        float distance = Math.Abs(val - optimalValue);
        return Math.Max(0f, 1.0f - (distance * 2f));
    }
}
