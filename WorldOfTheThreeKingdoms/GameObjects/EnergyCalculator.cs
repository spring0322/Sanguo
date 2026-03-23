using System;
using System.Runtime.InteropServices;
using GameObjects;
using Zhsan.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameObjects;

/// <summary>
/// 能量计算器（部队与城池统一体系）
/// 日期：2026-03-16
/// 功能：功过分离机制 + 动态门槛
/// 🧊 Cold Path：回合结束/建筑变化时调用
/// </summary>
public static class EnergyCalculator
{
    private static EnergyCalculationConfig _config = null!;
    
    /// <summary>
    /// 初始化配置（在游戏启动时调用）
    /// 🔥 ANTI-BAND-AID：必须在游戏启动时调用，否则后续调用会崩溃（Fail Fast）
    /// </summary>
    public static void Initialize(EnergyCalculationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
    
    /// <summary>
    /// 计算部队威压能量
    /// 🆕 2026-03-20：添加主将名声加权
    /// 🧊 Cold Path：回合结束时调用
    /// </summary>
    public static int CalculateTroopZocEnergy(
        GameObjectList persons,
        int quantity,
        int morale,
        int offence,
        int defence,
        int movability)
    {
        // 🔥 ANTI-BAND-AID：如果配置未初始化，直接崩溃（Fail Fast）
        // 不使用防御性空检查，因为配置未初始化说明游戏启动流程有严重错误
        var cfg = _config.TroopEnergy;
        
        // 1. 基础保底能量
        int baseEnergy = cfg.BaseEnergy;
        
        // 2. 兵力与士气加成
        int troopBonus = (int)(MathF.Sqrt(quantity) * cfg.QuantityWeight * (morale / 100.0f));
        
        // 3. 军事素养加成
        var combatCfg = cfg.CombatWeights;
        int combatBonus = (int)((offence * combatCfg.OffenceMultiplier 
                                + defence * combatCfg.DefenceMultiplier 
                                + movability * combatCfg.MovabilityMultiplier) 
                                / combatCfg.TotalDivisor / combatCfg.FinalDivisor);
        
        // 4. 政魅加权（功过分离机制）
        int adminBonus = CalculateOfficerMerit(persons, cfg.OfficerMeritSystem, isArchitecture: false);
        
        // 🆕 5. 主将名声加权
        // 日期：2026-03-20
        // 说明：主将名声影响部队威压，名声越高威压越强
        int reputationBonus = 0;
        
        // 🔥 ANTI-BAND-AID：Fail Fast
        // 如果 persons 为 null 或空，说明调用者传入了错误数据
        if (persons == null)
        {
            throw new ArgumentNullException(nameof(persons),
                "[CalculateTroopZocEnergy] persons 不能为 null");
        }
        
        if (persons.Count > 0)
        {
            var personList = persons.GetList();
            
            // 🔥 ANTI-BAND-AID：Fail Fast
            // 主将应该是第一个武将，如果为 null 说明数据损坏
            if (personList[0] is not Person leader)
            {
                throw new InvalidOperationException(
                    "[CalculateTroopZocEnergy] persons 列表第一个元素不是 Person 类型");
            }
            
            // 名声加权公式：开方衰减 + 权重系数
            // 名声范围：0-10000，开方后：0-100
            // 权重系数：0.3（避免名声影响过大）
            reputationBonus = (int)(MathF.Sqrt(leader.Reputation) * 0.3f);
        }
        
        // 6. 总计与截断
        int rawEnergy = baseEnergy + troopBonus + combatBonus + adminBonus + reputationBonus;
        
        // 🆕 7. 应用能量倍率
        // 日期：2026-03-20
        // 说明：降低部队能量产生，避免初期过快达到上限（380）
        rawEnergy = (int)(rawEnergy * cfg.EnergyMultiplier);
        
        return Math.Clamp(rawEnergy, cfg.EnergyRange.Minimum, cfg.EnergyRange.Maximum);
    }
    
    /// <summary>
    /// 计算城池影响力能量
    /// </summary>
    public static int CalculateArchitectureInfluenceEnergy(
        int scale,
        int domination,
        int morale,
        int agriculture,
        int commerce,
        int population,
        GameObjectList militaries,
        GameObjectList persons)
    {
        // 🔥 ANTI-BAND-AID：如果配置未初始化，直接崩溃（Fail Fast）
        var cfg = _config.ArchitectureEnergy;
        
        // 1. 内政基础能量
        var baseWeights = cfg.BaseStatsWeights;
        int baseStats = scale * baseWeights.ScaleMultiplier 
                       + (int)(domination * baseWeights.DominationWeight)
                       + (int)(morale * baseWeights.MoraleWeight)
                       + (int)(agriculture * baseWeights.AgricultureWeight)
                       + (int)(commerce * baseWeights.CommerceWeight);
        
        // 2. 人口加成
        int popBonus = (int)(MathF.Sqrt(population) * cfg.PopulationWeight);
        
        // 3. 驻军威势加成
        int garrisonBonus = CalculateGarrisonBonus(militaries, cfg.GarrisonWeights);
        
        // 4. 人才光环加成（功过分离机制）
        int officerBonus = CalculateArchitectureOfficerMerit(persons, cfg.OfficerMeritSystem);
        
        // 5. 开方与终极封顶
        int totalBonus = baseStats + garrisonBonus + officerBonus;
        int rawEnergy = cfg.BaseEnergyOffset + popBonus + (int)(MathF.Sqrt(totalBonus) * cfg.FinalFormulaMultiplier);
        
        return Math.Clamp(rawEnergy, cfg.EnergyRange.Minimum, cfg.EnergyRange.Maximum);
    }
    
    /// <summary>
    /// 计算武将功过分离（部队版）
    /// </summary>
    private static int CalculateOfficerMerit(
        GameObjectList persons,
        OfficerMeritSystem cfg,
        bool isArchitecture)
    {
        var personList = persons.GetList();
        int count = personList.Count;
        
        if (count == 0) return 0;
        
        // 第一遍：确立主导核心
        int maxPol = 0, maxCha = 0;
        
        for (int i = 0; i < count; i++)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast
            if (personList[i] is not Person person)
            {
                throw new InvalidOperationException(
                    $"Persons 列表包含无效项（索引 {i}）");
            }
            
            if (person.Politics > maxPol) maxPol = person.Politics;
            if (person.Glamour > maxCha) maxCha = person.Glamour;
        }
        
        // 动态门槛：主将能力的一半 + 人数冗余惩罚
        int deputyCount = Math.Max(0, count - 1);
        int bloatPenalty = deputyCount * cfg.BloatPenaltyPerDeputy;
        int polBaseline = (int)(maxPol * cfg.BaselineRatio) + bloatPenalty;
        int chaBaseline = (int)(maxCha * cfg.BaselineRatio) + bloatPenalty;
        
        // 第二遍：功过分离
        int positivePol = 0, negativePol = 0;
        int positiveCha = 0, negativeCha = 0;
        bool skippedMaxPol = false, skippedMaxCha = false;
        
        for (int i = 0; i < count; i++)
        {
            var person = (Person)personList[i]!;
            
            // 政治结算
            if (!skippedMaxPol && person.Politics == maxPol)
            {
                skippedMaxPol = true;
            }
            else
            {
                int diff = person.Politics - polBaseline;
                if (diff > 0) positivePol += diff;
                else negativePol += diff; // negativePol 会累加负数
            }
            
            // 魅力结算
            if (!skippedMaxCha && person.Glamour == maxCha)
            {
                skippedMaxCha = true;
            }
            else
            {
                int diff = person.Glamour - chaBaseline;
                if (diff > 0) positiveCha += diff;
                else negativeCha += diff;
            }
        }
        
        // 双向开方结算
        int effectivePol = maxPol 
                          + (int)(MathF.Sqrt(positivePol) * cfg.PositiveBonusMultiplier) 
                          - (int)(MathF.Sqrt(-negativePol) * cfg.NegativePenaltyMultiplier);
        
        int effectiveCha = maxCha 
                          + (int)(MathF.Sqrt(positiveCha) * cfg.PositiveBonusMultiplier) 
                          - (int)(MathF.Sqrt(-negativeCha) * cfg.NegativePenaltyMultiplier);
        
        // 防止极端庸才把属性扣成负数
        effectivePol = Math.Max(cfg.MinimumEffectiveValue, effectivePol);
        effectiveCha = Math.Max(cfg.MinimumEffectiveValue, effectiveCha);
        
        return (effectivePol + effectiveCha) / 2;
    }
    
    /// <summary>
    /// 计算城池武将功过分离
    /// </summary>
    private static int CalculateArchitectureOfficerMerit(
        GameObjectList persons,
        ArchitectureOfficerMeritSystem cfg)
    {
        var personList = persons.GetList();
        int count = personList.Count;
        
        if (count == 0) return 0;
        
        // 第一遍：确立主导核心
        int maxFiveStatsSum = 0;
        int maxPol = 0;
        
        for (int i = 0; i < count; i++)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast
            if (personList[i] is not Person person)
            {
                throw new InvalidOperationException(
                    $"Persons 列表包含无效项（索引 {i}）");
            }
            
            int currentSum = person.Command + person.Strength + person.Intelligence 
                           + person.Politics + person.Glamour;
            
            if (currentSum > maxFiveStatsSum) maxFiveStatsSum = currentSum;
            if (person.Politics > maxPol) maxPol = person.Politics;
        }
        
        // 动态门槛
        int bloatPenalty = count * cfg.BloatPenaltyPerOfficer;
        int sumBaseline = (int)(maxFiveStatsSum * cfg.FiveStatsBaselineRatio) + bloatPenalty;
        int polBaseline = (int)(maxPol * cfg.PoliticsBaselineRatio) + bloatPenalty;
        
        // 第二遍：功过分离
        int positiveSum = 0, negativeSum = 0;
        int positivePol = 0, negativePol = 0;
        bool skippedMaxSum = false, skippedMaxPol = false;
        
        for (int i = 0; i < count; i++)
        {
            var person = (Person)personList[i]!;
            int currentSum = person.Command + person.Strength + person.Intelligence 
                           + person.Politics + person.Glamour;
            
            // 五维结算
            if (!skippedMaxSum && currentSum == maxFiveStatsSum)
            {
                skippedMaxSum = true;
            }
            else
            {
                int diffSum = currentSum - sumBaseline;
                if (diffSum > 0) positiveSum += diffSum;
                else negativeSum += diffSum;
            }
            
            // 政治结算
            if (!skippedMaxPol && person.Politics == maxPol)
            {
                skippedMaxPol = true;
            }
            else
            {
                int diffPol = person.Politics - polBaseline;
                if (diffPol > 0) positivePol += diffPol;
                else negativePol += diffPol;
            }
        }
        
        // 双向开方结算
        int effectiveOfficerPower = maxFiveStatsSum 
                                   + (int)(MathF.Sqrt(positiveSum) * cfg.PositiveBonusMultiplier) 
                                   - (int)(MathF.Sqrt(-negativeSum) * cfg.NegativePenaltyMultiplier);
        
        int effectivePol = maxPol 
                          + (int)(MathF.Sqrt(positivePol) * cfg.PositiveBonusMultiplier) 
                          - (int)(MathF.Sqrt(-negativePol) * cfg.NegativePenaltyMultiplier);
        
        // 防止极端庸才把属性扣成负数
        effectiveOfficerPower = Math.Max(cfg.MinimumEffectiveFiveStats, effectiveOfficerPower);
        effectivePol = Math.Max(cfg.MinimumEffectivePolitics, effectivePol);
        
        return (effectiveOfficerPower / cfg.FiveStatsDivisor) + (int)(effectivePol * cfg.PoliticsWeight);
    }
    
    /// <summary>
    /// 计算驻军加成
    /// </summary>
    private static int CalculateGarrisonBonus(GameObjectList militaries, GarrisonWeights cfg)
    {
        var militaryList = militaries.GetList();
        int count = militaryList.Count;
        
        if (count == 0) return 0;
        
        int totalTroops = 0;
        int totalMorale = 0;
        
        for (int i = 0; i < count; i++)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast
            if (militaryList[i] is not Military military)
            {
                throw new InvalidOperationException(
                    $"Militaries 列表包含无效项（索引 {i}）");
            }
            
            totalTroops += military.Quantity;
            totalMorale += military.Morale;
        }
        
        int avgMorale = totalMorale / count;
        return (int)(MathF.Sqrt(totalTroops) * cfg.QuantityWeight * (avgMorale / 100.0f * cfg.MoraleWeight));
    }
}
