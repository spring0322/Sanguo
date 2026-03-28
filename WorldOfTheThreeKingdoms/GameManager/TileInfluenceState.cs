using System.Runtime.CompilerServices;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 地块影响力状态（三层结构）
/// 🔥 日期：2026-03-16
/// 🔥 目的：区分城池能量和部队能量，实现"先抵消部队能量"的原则
/// 🆕 日期：2026-03-21
/// 🆕 新增：残留能量层，实现"余威自然衰退"机制
/// 
/// 设计理念：
/// - 基岩层（CityEnergy）：战略归属，来自城池扩散
/// - 表土层（ArmyEnergy）：战术控制，来自部队威压
/// - 🆕 残留层（ResidualEnergy）：余威能量，与源头断开连接的能量
/// - 抵消顺序：先抵消部队能量，再抵消城池能量
/// - 归属判断：只看城池能量，部队能量不算归属
/// 
/// 水流类比：
/// - 活水（ArmyEnergy/CityEnergy）：与水源有直接连接
/// - 死水（ResidualEnergy）：与水源断开连接，逐渐蒸发
/// </summary>
public struct TileInfluenceState
{
    // 🔥 基岩层：战略归属（城池）
    // 日期：2026-03-16
    // 说明：城池扩散产生的能量，决定地块的战略归属
    public int CityFactionId;
    public int CityEnergy;
    
    // 🔥 表土层：战术控制（部队）
    // 日期：2026-03-16
    // 说明：部队威压产生的能量，用于战术层面的判断（ZOC、战斗加成等）
    public int ArmyFactionId;
    public int ArmyEnergy;
    
    // 🆕 残留层：余威能量（2026-03-21）
    // 说明：与源头断开连接的能量，逐渐衰减消失
    // 用途：部队撤退后的短暂威慑力、战场态势的视觉反馈
    // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
    public int ResidualFactionId;  // -1 表示无残留
    public int ResidualEnergy;     // 残留能量值
    
    /// <summary>
    /// 地块归属：只看城池能量
    /// 🔥 关键：部队能量不算归属
    /// 日期：2026-03-16
    /// 原因：地块归属要看抵消后剩余的能量属于哪个势力城池
    /// </summary>
    public readonly int TerritoryOwner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
            // 参考：ID判断规范.md
            if (CityFactionId >= 0 && CityEnergy > 0)
            {
                return CityFactionId;
            }
            
            // 只有部队能量，不算归属
            return -1;  // -1 表示无归属
        }
    }
    
    /// <summary>
    /// 战术控制：看部队能量
    /// 🔥 用于 ZOC、战斗加成等战术层面的判断
    /// 日期：2026-03-16
    /// </summary>
    public readonly int TacticalController
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
            if (ArmyFactionId >= 0 && ArmyEnergy > 0)
            {
                return ArmyFactionId;
            }
            
            // 无部队，返回城池归属
            return CityFactionId;
        }
    }
    
    /// <summary>
    /// 有效总能量：用于 ZOC 增益计算
    /// 🔥 同势力叠加：主副衰减法（大值 + 小值×30%）
    /// 日期：2026-03-16
    /// </summary>
    public readonly int EffectiveTotalEnergy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return InfluenceEnergyCalculator.CalculateTileEffectiveTotalEnergy(
                ArmyFactionId,
                ArmyEnergy,
                CityFactionId,
                CityEnergy,
                ResidualEnergy);
        }
    }
    
    /// <summary>
    /// 重置地块状态
    /// 🧊 Cold Path：势力范围更新时调用
    /// 🆕 日期：2026-03-21
    /// 🆕 新增：重置残留能量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        CityFactionId = -1;  // -1 表示无归属
        CityEnergy = 0;
        ArmyFactionId = -1;
        ArmyEnergy = 0;
        ResidualFactionId = -1;  // 🆕 重置残留能量
        ResidualEnergy = 0;
    }
}
