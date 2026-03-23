using System;
using System.Diagnostics;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战术AI整合层 - 连接效用理论决策与现有执行系统
/// 日期：2026-03-11
/// 
/// 🧊 COLD PATH：每回合调用，可读性优先
/// </summary>
public static class CityTacticalIntegration
{
    /// <summary>
    /// 计算城市威胁等级 (0.0 ~ 1.0)
    /// 🧊 COLD PATH：每回合调用
    /// 
    /// ⚠️ ANTI-BAND-AID：不检查 city == null，调用者必须保证参数有效
    /// </summary>
    public static float CalculateCityThreatLevel(Architecture city)
    {
        float threatLevel = 0f;

        // 1. 视野内有敌军：基础威胁 0.3
        if (city.HasHostileTroopsInView())
        {
            threatLevel += 0.3f;
            
            // 获取敌军列表并评估威胁强度
            var hostileTroops = city.GetHostileTroopsInView();
            if (hostileTroops != null && hostileTroops.Count > 0)
            {
                // 每多一支敌军，威胁+0.1，最多+0.4
                threatLevel += Math.Min(0.4f, hostileTroops.Count * 0.1f);
            }
        }

        // 2. 最近被攻击：威胁+0.2
        if (city.RecentlyAttacked > 0)
        {
            threatLevel += 0.2f;
        }

        // 3. 耐久度低：威胁+0.1
        if (city.Endurance < city.EnduranceCeiling * 0.3f)
        {
            threatLevel += 0.1f;
        }

        return Math.Clamp(threatLevel, 0f, 1.0f);
    }

    /// <summary>
    /// 执行城市战术决策
    /// 🧊 COLD PATH：每回合调用
    /// 
    /// ⚠️ ANTI-BAND-AID：
    /// - 不检查 city == null，调用者必须保证参数有效
    /// - city.BelongedFaction == null 是合法状态（中立城市），直接跳过
    /// </summary>
    public static void ExecuteCityTacticalDecision(Architecture city)
    {
        // 中立城市（无势力）跳过AI决策
        if (city.BelongedFaction == null)
        {
            return;
        }

        // 玩家控制的城市跳过AI托管
        if (city.BelongedFaction.Controlling)
        {
            return;
        }

        try
        {
            // 1. 组装决策上下文
            var context = new StrategicContext(
                gold: city.Fund,
                food: city.Food,
                currentTroops: city.MilitaryCount,  // 使用 MilitaryCount 而不是 TotalTroops
                maxTroops: CalculateMaxTroops(city),
                averageMorale: city.Morale,
                threatLevel: CalculateCityThreatLevel(city)
            );

            // 2. 效用大脑抉择
            StrategicAction decision = CityTacticalAI.GetBestAction(in context);

            // 3. 执行决策
            ExecuteAction(city, decision);

            // 4. 调试日志
            Debug.WriteLine($"[城市战术AI] {city.Name} 决定执行: {CityTacticalAI.GetActionDescription(decision)} " +
                           $"(资金:{city.Fund}, 粮食:{city.Food}, 兵力:{city.MilitaryCount}/{CalculateMaxTroops(city)}, " +
                           $"士气:{city.Morale}, 威胁:{context.ThreatLevel:F2})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[城市战术AI] {city.Name} 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算城市最大兵力容量
    /// </summary>
    private static int CalculateMaxTroops(Architecture city)
    {
        // 基于人口计算理论最大兵力
        // 假设每5个人口可以支撑1个士兵
        int populationBasedMax = city.Population / 5;
        
        // 基于城市规模的硬上限
        int scaleBasedMax = city.AreaCount * 10000;
        
        return Math.Min(populationBasedMax, scaleBasedMax);
    }

    /// <summary>
    /// 执行具体行动
    /// 🧊 COLD PATH：调用现有的城市方法
    /// </summary>
    private static void ExecuteAction(Architecture city, StrategicAction action)
    {
        switch (action)
        {
            case StrategicAction.DevelopAgriculture:
                // 调用现有的农业发展逻辑
                // Architecture.DevelopAgriculture() 是 private 方法，通过 RunDomesticAI() 间接调用
                city.RunDomesticAI();
                break;

            case StrategicAction.DevelopCommerce:
                // 调用现有的商业发展逻辑
                city.RunDomesticAI();
                break;

            case StrategicAction.DraftTroops:
                // 调用现有的招募逻辑
                // AIRecruitMilitary() 是 private 方法，通过 RunMilitaryAI() 间接调用
                city.RunMilitaryAI();
                break;

            case StrategicAction.TrainTroops:
                // 调用现有的训练逻辑
                // TrainMilitary() 是 private 方法，通过 RunDomesticAI() 间接调用
                city.RunDomesticAI();
                break;

            case StrategicAction.LaunchCampaign:
                // 调用现有的出征逻辑
                city.RunMilitaryAI();
                break;

            case StrategicAction.Idle:
            default:
                // 休养生息，不执行任何操作
                break;
        }
    }
}
