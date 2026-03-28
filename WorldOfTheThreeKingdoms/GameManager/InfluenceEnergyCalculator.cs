using System;
using System.Runtime.CompilerServices;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 影响力能量统一计算入口
/// - 统一残留能量有效值换算
/// - 统一残留有效值回写 raw 值换算
/// - 统一地块总有效能量计算
/// </summary>
internal static class InfluenceEnergyCalculator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateEffectiveResidualEnergy(int residualEnergy)
    {
        float residualEffectiveness = WorldOfTheThreeKingdoms.GameData.InfluenceConfig.Current
            .EnergyStackingConfig
            .ResidualEffectiveness;
        return CalculateEffectiveResidualEnergy(residualEnergy, residualEffectiveness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateEffectiveResidualEnergy(int residualEnergy, float residualEffectiveness)
    {
        if (residualEnergy <= 0 || residualEffectiveness <= 0f)
        {
            return 0;
        }

        return (int)(residualEnergy * residualEffectiveness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RecoverRawResidualEnergy(int effectiveResidualEnergy, float residualEffectiveness)
    {
        if (effectiveResidualEnergy <= 0 || residualEffectiveness <= 0f)
        {
            return 0;
        }

        int rawResidual = (int)Math.Ceiling(effectiveResidualEnergy / residualEffectiveness);
        return rawResidual > 0 ? rawResidual : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateTileEffectiveTotalEnergy(
        int armyFactionId,
        int armyEnergy,
        int cityFactionId,
        int cityEnergy,
        int residualEnergy)
    {
        int activeEnergy;

        // 同势力叠加：主副衰减法（维持现有设计）
        if (armyFactionId >= 0 && armyFactionId == cityFactionId)
        {
            activeEnergy = cityEnergy > armyEnergy
                ? cityEnergy + (int)(armyEnergy * 0.3f)
                : armyEnergy + (int)(cityEnergy * 0.3f);
        }
        else
        {
            activeEnergy = Math.Abs(cityEnergy - armyEnergy);
        }

        int effectiveResidualEnergy = CalculateEffectiveResidualEnergy(residualEnergy);
        return activeEnergy + effectiveResidualEnergy;
    }
}

