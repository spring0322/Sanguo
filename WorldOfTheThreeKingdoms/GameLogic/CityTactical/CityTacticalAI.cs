#nullable enable

using System;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// 城市战术AI - 基于效用理论的精细决策系统
/// 日期：2026-03-11
/// 
/// 🧊 COLD PATH：每回合调用一次，可读性优先
/// </summary>
public static class CityTacticalAI
{
    private static CityTacticalConfig? _config;

    /// <summary>
    /// 🧊 COLD PATH：初始化配置（游戏启动时调用）
    /// </summary>
    public static void Initialize()
    {
        _config = CityTacticalConfigLoader.LoadConfig();
    }

    /// <summary>
    /// 评估并返回当前城市的最优战略决策
    /// 🧊 COLD PATH：每回合调用，允许使用可读性优先的代码
    /// </summary>
    public static StrategicAction GetBestAction(in StrategicContext context)
    {
        if (_config == null)
        {
            throw new InvalidOperationException("CityTacticalAI 未初始化，请先调用 Initialize()");
        }

        StrategicAction bestAction = StrategicAction.Idle;
        float highestScore = 0f;

        // 1. 评估【开垦农田】
        float agricultureScore = EvaluateAgriculture(in context, _config);
        if (agricultureScore > highestScore)
        {
            highestScore = agricultureScore;
            bestAction = StrategicAction.DevelopAgriculture;
        }

        // 2. 评估【招募士兵】
        float draftScore = EvaluateDrafting(in context, _config);
        if (draftScore > highestScore)
        {
            highestScore = draftScore;
            bestAction = StrategicAction.DraftTroops;
        }

        // 3. 评估【训练部队】
        float trainScore = EvaluateTraining(in context, _config);
        if (trainScore > highestScore)
        {
            highestScore = trainScore;
            bestAction = StrategicAction.TrainTroops;
        }

        // 4. 评估【发起出征】
        float campaignScore = EvaluateCampaign(in context, _config);
        if (campaignScore > highestScore)
        {
            highestScore = campaignScore;
            bestAction = StrategicAction.LaunchCampaign;
        }

        return bestAction;
    }

    private static float EvaluateAgriculture(in StrategicContext ctx, CityTacticalConfig config)
    {
        // 如果处于极度危险中，没人有心情种田
        if (ctx.ThreatLevel > 0.8f) return 0f;

        // 粮草饱和度
        float foodSaturation = Math.Min(1.0f, ctx.Food / (float)config.ResourceThresholds.FoodSafetyLine);

        // 越缺粮，种田得分越高
        float baseScore = UtilityMath.InverseLogistic(foodSaturation, config.UtilityWeights.AgricultureSharpness) 
                          * config.BaseScores.Agriculture;

        return baseScore;
    }

    private static float EvaluateDrafting(in StrategicContext ctx, CityTacticalConfig config)
    {
        // 没钱没粮不能征兵
        if (ctx.Gold < config.ActionCosts.DraftMinGold || ctx.Food < config.ActionCosts.DraftMinFood)
        {
            return 0f;
        }

        // 兵力越少，征兵渴望越高
        float baseScore = UtilityMath.InverseLogistic(ctx.TroopSaturation, config.UtilityWeights.DraftSharpness) 
                          * config.BaseScores.Draft;

        // 环境修正：如果外部威胁极大，征兵渴望直接突破天际
        baseScore += ctx.ThreatLevel * config.UtilityWeights.ThreatMultiplier;

        return baseScore;
    }

    private static float EvaluateTraining(in StrategicContext ctx, CityTacticalConfig config)
    {
        // 兵太少没必要练
        if (ctx.TroopSaturation < config.ActionCosts.TrainMinTroopSaturation)
        {
            return 0f;
        }

        // 士气中间值评估：士气越低越渴望训练，但满士气时得分为0
        float moraleSaturation = ctx.AverageMorale / 100f;
        float baseScore = UtilityMath.InverseLogistic(moraleSaturation, config.UtilityWeights.TrainSharpness) 
                          * config.BaseScores.Train;

        return baseScore;
    }

    private static float EvaluateCampaign(in StrategicContext ctx, CityTacticalConfig config)
    {
        var req = config.CampaignRequirements;
        
        // 基础条件：兵多、粮足、士气高、老家安全
        if (ctx.TroopSaturation < req.MinTroopSaturation 
            || ctx.AverageMorale < req.MinMorale 
            || ctx.ThreatLevel > req.MaxThreatLevel)
        {
            return 0f;
        }

        return config.BaseScores.Campaign;
    }

    /// <summary>
    /// 获取行动的中文描述
    /// </summary>
    public static string GetActionDescription(StrategicAction action)
    {
        return action switch
        {
            StrategicAction.Idle => "休养生息",
            StrategicAction.DevelopAgriculture => "开垦农田",
            StrategicAction.DevelopCommerce => "发展商业",
            StrategicAction.DraftTroops => "招募士兵",
            StrategicAction.TrainTroops => "训练士气",
            StrategicAction.LaunchCampaign => "发起出征",
            _ => "未知行动"
        };
    }
}
