// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/InfluenceBuffCalculator.cs
// 创建日期: 2026-03-11
// 功能: 势力范围Buff计算器（Phase 4）
// ============================================================

using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.FactionDetail;
using GameManager;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 🆕 势力范围Buff计算器
/// 🧊 Cold Path：不在Update循环中频繁调用，只在部队状态变化时计算
/// 日期：2026-03-11
/// </summary>
public static class InfluenceBuffCalculator
{
    /// <summary>
    /// 🆕 计算指定位置的净势力影响
    /// 返回值：正数表示己方优势，负数表示敌方优势，0表示中立
    /// 🔥 优化：直接查询全局能量地图，从 O(势力数×建筑数) 降低到 O(势力数)
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="faction">参考势力</param>
    /// <returns>净能量值（己方能量 - 敌方最大能量）</returns>
    public static int GetNetInfluenceAt(Point position, Faction faction)
    {
        // 🔥 数据验证：确保参数有效
        if (faction == null)
        {
            throw new ArgumentNullException(nameof(faction),
                "[GetNetInfluenceAt] faction 不能为 null");
        }
        
        var scenario = Session.Current.Scenario;
        
        // 🔥 ANTI-BAND-AID：明确检查 null
        if (scenario == null)
        {
            throw new InvalidOperationException(
                "[GetNetInfluenceAt] Session.Current.Scenario 为 null，游戏未正确初始化");
        }
        
        // 🔥 ANTI-BAND-AID：检查全局能量地图
        if (faction.GlobalInfluenceMap is not { Length: > 0 })
        {
            throw new InvalidOperationException(
                $"[GetNetInfluenceAt] 势力 {faction.Name} 的 GlobalInfluenceMap 未初始化");
        }
        
        // 🔥 性能优化：直接查询全局能量地图（O(1)）
        // 🔥 日期：2026-03-16
        // 🔥 重构：使用 EffectiveTotalEnergy
        int index = TerrainCostCache.GetIndex(position.X, position.Y);
        int friendlyEnergy = faction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
        
        // 🔥 查询所有敌对势力的能量（O(势力数)）
        int maxEnemyEnergy = 0;
        
        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;
        
        for (int f = 0; f < factionCount; f++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (factions[f] is not Faction otherFaction)
            {
                throw new InvalidOperationException(
                    $"[GetNetInfluenceAt] Factions 列表包含无效项（索引 {f}）");
            }
            
            // 跳过己方和友军
            if (otherFaction == faction || faction.IsFriendly(otherFaction))
            {
                continue;
            }
            
            // 🔥 ANTI-BAND-AID：检查敌方全局能量地图
            if (otherFaction.GlobalInfluenceMap is not { Length: > 0 })
            {
                throw new InvalidOperationException(
                    $"[GetNetInfluenceAt] 势力 {otherFaction.Name} 的 GlobalInfluenceMap 未初始化");
            }
            
            // 🔥 O(1) 查询敌方能量
            // 🔥 日期：2026-03-16
            // 🔥 重构：使用 EffectiveTotalEnergy
            int enemyEnergy = otherFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
            
            if (enemyEnergy > maxEnemyEnergy)
            {
                maxEnemyEnergy = enemyEnergy;
            }
        }
        
        // 返回净能量：己方总能量 - 敌方最大能量
        return friendlyEnergy - maxEnemyEnergy;
    }
    
    /// <summary>
    /// 🆕 计算攻击加成（基于净能量）
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <param name="isArchitecture">是否为城池（城池最高10%，部队最高5%）</param>
    /// <returns>攻击倍率（1.0 = 无加成，1.05 = +5%）</returns>
    public static float CalculateAttackBonus(int netEnergy, bool isArchitecture = false)
    {
        var config = GameData.InfluenceConfig.Current;
        
        if (!config.EnableInfluenceBuff) return 1.0f;
        
        if (netEnergy > 0)
        {
            // 己方优势：提供攻击加成
            // 部队最高5%（阈值300），城池最高10%（阈值500）
            int threshold = isArchitecture ? config.ArchitectureEnergyThresholdForCore : config.EnergyThresholdForCore;
            float ratio = Math.Min(1.0f, netEnergy / (float)threshold);
            float maxBonus = isArchitecture ? config.MaxArchitectureAttackBonus : config.MaxAttackBonus;
            return 1.0f + (maxBonus * ratio);
        }
        
        // 敌方优势或中立：无加成
        return 1.0f;
    }
    
    /// <summary>
    /// 🆕 计算防御加成（基于净能量）
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <param name="isArchitecture">是否为城池（城池最高10%，部队最高5%）</param>
    /// <returns>防御倍率（1.0 = 无加成，1.05 = +5%）</returns>
    public static float CalculateDefenseBonus(int netEnergy, bool isArchitecture = false)
    {
        var config = GameData.InfluenceConfig.Current;
        
        if (!config.EnableInfluenceBuff) return 1.0f;
        
        if (netEnergy > 0)
        {
            // 己方优势：提供防御加成
            // 部队最高5%（阈值300），城池最高10%（阈值500）
            int threshold = isArchitecture ? config.ArchitectureEnergyThresholdForCore : config.EnergyThresholdForCore;
            float ratio = Math.Min(1.0f, netEnergy / (float)threshold);
            float maxBonus = isArchitecture ? config.MaxArchitectureDefenseBonus : config.MaxDefenseBonus;
            return 1.0f + (maxBonus * ratio);
        }
        
        // 敌方优势或中立：无加成
        return 1.0f;
    }
    
    /// <summary>
    /// 🆕 计算粮食消耗倍率（基于净能量）
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <param name="isArchitecture">是否为城池（影响阈值选择）</param>
    /// <returns>粮食消耗倍率（0.85 = -15%，1.15 = +15%）</returns>
    public static float CalculateFoodConsumptionMultiplier(int netEnergy, bool isArchitecture = false)
    {
        var config = GameData.InfluenceConfig.Current;
        
        if (!config.EnableInfluenceBuff) return 1.0f;
        
        // 🔥 关键：部队阈值300，城池阈值500
        int threshold = isArchitecture ? config.ArchitectureEnergyThresholdForCore : config.EnergyThresholdForCore;
        
        if (netEnergy > 0)
        {
            // 己方优势：减少粮食消耗（最大-15%）
            // 🔥 动态计算：能量越高，减免越多
            float ratio = Math.Min(1.0f, netEnergy / (float)threshold);
            return 1.0f - (config.MaxFoodReduction * ratio);
        }
        else if (netEnergy < 0)
        {
            // 敌方优势：增加粮食消耗（最大+15%）
            // 🔥 修复：动态计算，敌方能量越强，惩罚越重
            // 🔥 日期：2026-03-17
            float ratio = Math.Min(1.0f, -netEnergy / (float)threshold);
            return 1.0f + (config.EnemyFoodPenalty * ratio);
        }
        
        // 中立：无影响
        return 1.0f;
    }
    
    /// <summary>
    /// 🆕 获取Buff描述文本（用于UI显示）
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <param name="isArchitecture">是否为城池（城池攻防最高10%，部队最高5%；阈值不同）</param>
    /// <returns>Buff描述字符串</returns>
    public static string GetBuffDescription(int netEnergy, bool isArchitecture = false)
    {
        var config = GameData.InfluenceConfig.Current;
        int threshold = isArchitecture ? config.ArchitectureEnergyThresholdForCore : config.EnergyThresholdForCore;
        
        if (netEnergy > threshold)
        {
            // 使用配置中的最大值
            float maxAttack = (isArchitecture ? config.MaxArchitectureAttackBonus : config.MaxAttackBonus) * 100;
            float maxDefense = (isArchitecture ? config.MaxArchitectureDefenseBonus : config.MaxDefenseBonus) * 100;
            float maxFoodReduction = config.MaxFoodReduction * 100;
            return $"己方核心区域：攻击+{maxAttack:F0}%，防御+{maxDefense:F0}%，粮耗-{maxFoodReduction:F0}%";
        }
        else if (netEnergy > 0)
        {
            float attackBonus = (CalculateAttackBonus(netEnergy, isArchitecture) - 1.0f) * 100;
            float defenseBonus = (CalculateDefenseBonus(netEnergy, isArchitecture) - 1.0f) * 100;
            float foodReduction = (1.0f - CalculateFoodConsumptionMultiplier(netEnergy, isArchitecture)) * 100;
            return $"己方势力范围：攻击+{attackBonus:F1}%，防御+{defenseBonus:F1}%，粮耗-{foodReduction:F1}%";
        }
        else if (netEnergy < -threshold)
        {
            float enemyFoodPenalty = config.EnemyFoodPenalty * 100;
            return $"敌方核心区域：粮耗+{enemyFoodPenalty:F0}%";
        }
        else if (netEnergy < 0)
        {
            float enemyFoodPenalty = config.EnemyFoodPenalty * 100;
            return $"敌方势力范围：粮耗+{enemyFoodPenalty:F0}%";
        }
        else
        {
            return "中立区域：无影响";
        }
    }
    
    // ===== 🆕 Phase 3：分级 Buff 系统 =====
    // 日期：2026-03-16
    
    /// <summary>
    /// 🆕 计算分级 Buff（基于能量阈值）
    /// 🧊 Cold Path：部队/建筑状态变化时调用
    /// 日期：2026-03-16
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <returns>分级 Buff 效果</returns>
    public static TieredBuffEffect CalculateTieredBuffs(int netEnergy)
    {
        var config = GameData.InfluenceConfig.Current;
        var tiers = config.EnergyTiers;
        var buffConfig = config.BuffConfig;
        int stepSize = config.TieredBuffStepSize;
        
        var effect = new TieredBuffEffect();
        
        if (netEnergy <= 0) return effect;
        
        // Tier 1: 基础 Buff（能量 >= 100）
        if (netEnergy >= tiers.Tier1)
        {
            int steps = (netEnergy - tiers.Tier1) / stepSize;
            effect = effect with
            {
                DefenseBonus = steps * buffConfig.Tier1Buffs.DefenseBonus,
                ResistBonus = steps * buffConfig.Tier1Buffs.ResistBonus
            };
        }
        
        // Tier 2: 进阶 Buff（能量 >= 250）
        if (netEnergy >= tiers.Tier2)
        {
            int tier2Steps = (netEnergy - tiers.Tier2) / stepSize;
            effect = effect with
            {
                AttackBonus = buffConfig.Tier2Buffs.AttackBonusBase + 
                             (tier2Steps * buffConfig.Tier2Buffs.AttackBonusStep),
                CritBonus = buffConfig.Tier2Buffs.CritBonusBase + 
                           (tier2Steps * buffConfig.Tier2Buffs.CritBonusStep)
            };
        }
        
        // Tier 3: 终极 Buff（能量 >= 350）
        if (netEnergy >= tiers.Tier3)
        {
            int tier3Steps = (netEnergy - tiers.Tier3) / stepSize;
            effect = effect with
            {
                ApRecoveryBonus = buffConfig.Tier3Buffs.ApRecoveryBase + 
                                 (tier3Steps * buffConfig.Tier3Buffs.ApRecoveryStep),
                FatigueRecovery = buffConfig.Tier3Buffs.FatigueRecoveryBase + 
                                 (tier3Steps * buffConfig.Tier3Buffs.FatigueRecoveryStep),
                FoodDiscount = buffConfig.Tier3Buffs.FoodDiscountBase + 
                              (tier3Steps * buffConfig.Tier3Buffs.FoodDiscountStep)
            };
        }
        
        return effect;
    }
    
    /// <summary>
    /// 🆕 计算视野范围（基于能量）
    /// 🧊 Cold Path：部队/建筑移动或状态变化时调用
    /// 日期：2026-03-16
    /// 🔥 核心保护：部队至少有 1 格视野，建筑至少有 0 格视野
    /// 🔥 修复：2026-03-22 使用 TryGetValue 避免 KeyNotFoundException
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <param name="isArchitecture">是否为建筑（建筑可以为 0，部队至少为 1）</param>
    /// <returns>视野范围（格子数）</returns>
    public static int CalculateVisionRange(int netEnergy, bool isArchitecture = false)
    {
        var config = GameData.InfluenceConfig.Current;
        
        if (!config.VisionConfig.EnableDynamicVision)
        {
            return isArchitecture ? 0 : 1; // 默认视野：建筑 0，部队 1
        }
        
        var tiers = config.EnergyTiers;
        var visionRanges = config.VisionConfig.VisionRangeByEnergy;
        
        int dynamicVision;
        
        // 🔥 修复：使用 TryGetValue 避免 KeyNotFoundException
        // 原因：JSON 反序列化时，如果配置文件格式错误，字典可能缺少某些键
        // 日期：2026-03-22
        if (netEnergy >= tiers.Tier3 && visionRanges.TryGetValue("Tier3", out int tier3Vision))
            dynamicVision = tier3Vision;
        else if (netEnergy >= tiers.Tier2 && visionRanges.TryGetValue("Tier2", out int tier2Vision))
            dynamicVision = tier2Vision;
        else if (netEnergy >= tiers.Tier1 && visionRanges.TryGetValue("Tier1", out int tier1Vision))
            dynamicVision = tier1Vision;
        else
            dynamicVision = 0; // 无动态视野增益
        
        // 🔥 核心保护：部队至少有 1 格视野（即使在敌方核心区域）
        // 原因：部队需要基本的战场感知能力，否则无法战斗
        // 建筑可以为 0（被敌方完全压制时失去视野）
        if (!isArchitecture && dynamicVision < 1)
        {
            return 1;
        }
        
        return dynamicVision;
    }
    
    /// <summary>
    /// 🆕 计算情报等级（基于能量）
    /// 🧊 Cold Path：情报查询时调用
    /// 日期：2026-03-16
    /// 🔥 修复：2026-03-22 使用 TryGetValue 避免 KeyNotFoundException
    /// </summary>
    /// <param name="netEnergy">净能量值</param>
    /// <returns>情报等级</returns>
    public static InformationLevel CalculateInformationLevel(int netEnergy)
    {
        var config = GameData.InfluenceConfig.Current;
        var levels = config.VisionConfig.InformationLevelByEnergy;
        
        // 🔥 修复：使用 TryGetValue 避免 KeyNotFoundException
        // 原因：JSON 反序列化时，如果配置文件格式错误，字典可能缺少某些键
        // 日期：2026-03-22
        if (levels.TryGetValue("High", out int highThreshold) && netEnergy >= highThreshold)
            return InformationLevel.高;
        else if (levels.TryGetValue("Medium", out int mediumThreshold) && netEnergy >= mediumThreshold)
            return InformationLevel.中;
        else if (levels.TryGetValue("Low", out int lowThreshold) && netEnergy >= lowThreshold)
            return InformationLevel.低;
        else
            return InformationLevel.无;
    }
}

/// <summary>
/// 🆕 分级 Buff 效果（AOT 友好结构体）
/// 日期：2026-03-16
/// 
/// 使用 readonly struct 确保：
/// - 值类型，栈分配
/// - 不可变性
/// - Zero-GC
/// </summary>
public readonly struct TieredBuffEffect
{
    // Tier 1: 基础 Buff
    public float DefenseBonus { get; init; }
    public float ResistBonus { get; init; }
    
    // Tier 2: 进阶 Buff
    public float AttackBonus { get; init; }
    public float CritBonus { get; init; }
    
    // Tier 3: 终极 Buff
    public float ApRecoveryBonus { get; init; }
    public float FatigueRecovery { get; init; }
    public float FoodDiscount { get; init; }
    
    /// <summary>
    /// 🆕 获取 Buff 描述文本（用于 UI 显示）
    /// </summary>
    public string GetDescription()
    {
        List<string> parts = [];
        
        if (DefenseBonus > 0) parts.Add($"防御+{DefenseBonus * 100:F1}%");
        if (ResistBonus > 0) parts.Add($"抗性+{ResistBonus * 100:F1}%");
        if (AttackBonus > 0) parts.Add($"攻击+{AttackBonus * 100:F1}%");
        if (CritBonus > 0) parts.Add($"暴击+{CritBonus * 100:F1}%");
        if (ApRecoveryBonus > 0) parts.Add($"AP恢复+{ApRecoveryBonus * 100:F1}%");
        if (FatigueRecovery > 0) parts.Add($"疲劳恢复+{FatigueRecovery * 100:F1}%");
        if (FoodDiscount > 0) parts.Add($"粮耗-{FoodDiscount * 100:F1}%");
        
        return parts.Count > 0 ? string.Join("，", parts) : "无增益";
    }
}
