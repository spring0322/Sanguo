using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms;

namespace GameObjects.AI;

/// <summary>
/// AI决策结果（零分配，AOT友好）
/// </summary>
public readonly record struct AIDecision(
    Point2D MoveTarget,
    Troop AttackTarget,
    ActionType ActionType,
    string DecisionReason
);

/// <summary>
/// 混合AI决策器 - UtilityAI为主，传统系统为辅
/// 🧊 Cold Path: AI决策在回合开始时执行，允许LINQ
/// </summary>
public static class HybridAIDecisionMaker
{
    private static int _utilityAIUsageThisTurn = 0;
    private static readonly System.Diagnostics.Stopwatch _perfTimer = new();

    /// <summary>
    /// 重置回合计数器（在新回合开始时调用）
    /// </summary>
    public static void ResetTurnCounter()
    {
        _utilityAIUsageThisTurn = 0;
    }

    /// <summary>
    /// 为部队选择最佳行动
    /// ✅ Anti-Band-Aid: 不做防御性空检查，让调用方保证数据有效性
    /// </summary>
    public static AIDecision DecideAction(
        Troop troop,
        List<Troop> enemies,
        List<Troop> allies,
        StrategicPosture posture)
    {
        // 🔥 Anti-Band-Aid: 直接访问，如果为null会崩溃，暴露数据问题
        
        // 第一步：确保角色已分配（作为能力倾向的快速索引）
        if (troop.CurrentRole == TroopRole.None)
        {
            troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
        }

        // 第二步：判断是否为关键部队
        bool isKeyTroop = IsKeyTroop(troop, out string reason);

        // 第三步：根据部队重要性选择决策引擎
        if (isKeyTroop && CanUseUtilityAI())
        {
            _perfTimer.Restart();
            AIDecision decision = UseUtilityAI(troop, enemies, allies, posture, reason);
            _perfTimer.Stop();

            LogPerformance($"UtilityAI决策耗时: {_perfTimer.ElapsedMilliseconds}ms");
            _utilityAIUsageThisTurn++;

            return decision;
        }
        
        // 普通部队：使用传统启发式规则
        return UseTraditionalAI(troop, enemies, allies, reason);
    }

    /// <summary>
    /// 判断是否为关键部队（配置驱动）
    /// ✅ Anti-Band-Aid: 不做防御性空检查
    /// 🔥 如果配置为null，说明配置加载失败，应该崩溃
    /// </summary>
    private static bool IsKeyTroop(Troop troop, out string reason)
    {
        AIDecisionConfig config = AIDecisionConfigManager.Config;
        List<string> reasons = [];

        // 🔥 Anti-Band-Aid: 直接访问，如果为null会崩溃，暴露配置加载问题
        KeyTroopCriteriaConfig criteria = config.KeyTroopCriteria;

        // 1. 玩家势力判定
        // 🔥 Anti-Band-Aid: 直接访问BelongedFaction，如果为null会崩溃
        if (criteria.PlayerFaction.UseUtilityAIForImportantOnly)
        {
            if (Session.Current.Scenario.IsPlayer(troop.BelongedFaction))
            {
                reasons.Add("玩家势力");
            }
        }

        // 2. 属性阈值判定：统率/智力/武力任一超过阈值
        // 🔥 Anti-Band-Aid: 直接访问Leader，如果为null会崩溃
        StatThresholdsConfig statThresholds = criteria.StatThresholds;
        if (troop.Leader.Command >= statThresholds.Command)
        {
            reasons.Add($"统率{troop.Leader.Command}≥{statThresholds.Command}");
        }
        if (troop.Leader.Intelligence >= statThresholds.Intelligence)
        {
            reasons.Add($"智力{troop.Leader.Intelligence}≥{statThresholds.Intelligence}");
        }
        if (troop.Leader.Strength >= statThresholds.Strength)
        {
            reasons.Add($"武力{troop.Leader.Strength}≥{statThresholds.Strength}");
        }

        // 3. 称号等级判定
        // ✅ 业务逻辑：称号可以为null（武将没有称号是合法状态）
        TitleLevelThresholdConfig titleThreshold = criteria.TitleLevelThreshold;
        Title personalTitle = troop.Leader.getTitleOfKind(Session.Current.Scenario.GameCommonData.AllTitleKinds.GetTitleKind(troop.Leader.PersonalTitleString));
        if (personalTitle != null && personalTitle.Level >= titleThreshold.MinLevel)
        {
            reasons.Add($"私有称号Lv{personalTitle.Level}≥{titleThreshold.MinLevel}");
        }
        Title combatTitle = troop.Leader.getTitleOfKind(Session.Current.Scenario.GameCommonData.AllTitleKinds.GetTitleKind(troop.Leader.CombatTitleString));
        if (combatTitle != null && combatTitle.Level >= titleThreshold.MinLevel)
        {
            reasons.Add($"战斗称号Lv{combatTitle.Level}≥{titleThreshold.MinLevel}");
        }

        // 4. 兵力阈值判定
        QuantityThresholdConfig quantityThreshold = criteria.QuantityThreshold;
        if (troop.Quantity > quantityThreshold.MinQuantity)
        {
            reasons.Add($"兵力{troop.Quantity}>{quantityThreshold.MinQuantity}");
        }

        // 5. 精锐兵种判定
        // 🔥 Anti-Band-Aid: 直接访问Army.Kind，如果为null会崩溃
        EliteTroopKindsConfig eliteConfig = criteria.EliteTroopKinds;
        int kindID = troop.Army.Kind.ID;
        
        // 5.1 配置列表判定
        if (eliteConfig.TroopKindIDs.Contains(kindID))
        {
            string kindName = AIDecisionConfigManager.GetEliteTroopKindName(kindID);
            reasons.Add($"精锐兵种:{kindName}");
        }
        
        // 5.2 自动检测判定
        if (eliteConfig.AutoDetect.Enabled)
        {
            int maxScale = troop.Army.Kind.MaxScale;
            string typeName = troop.Army.Kind.Type.ToString();
            
            bool isElite = maxScale <= eliteConfig.AutoDetect.MaxScaleThreshold &&
                          !eliteConfig.AutoDetect.ExcludeTypes.Contains(typeName);
            
            if (isElite)
            {
                reasons.Add($"自动检测精锐(上限{maxScale}≤{eliteConfig.AutoDetect.MaxScaleThreshold})");
            }
        }

        // 6. 难度判定：困难模式下的所有敌军
        DifficultyBasedConfig difficultyConfig = criteria.DifficultyBased;
        int currentDifficulty = (int)Session.Parameters.AIDifficulty;
        bool isEnemy = troop.BelongedFaction != Session.Current.Scenario.CurrentPlayer;
        
        if (currentDifficulty >= difficultyConfig.MinDifficultyLevel && isEnemy)
        {
            if ((currentDifficulty >= 4 && difficultyConfig.EnableForEnemyInVeryHard) ||
                (currentDifficulty >= 3 && difficultyConfig.EnableForEnemyInHard))
            {
                reasons.Add($"困难模式敌军(难度{currentDifficulty})");
            }
        }

        // 汇总结果
        if (reasons.Count > 0)
        {
            reason = string.Join(", ", reasons);
            return true;
        }

        reason = "普通部队";
        return false;
    }

    /// <summary>
    /// 检查是否可以使用UtilityAI（性能限制）
    /// </summary>
    private static bool CanUseUtilityAI()
    {
        PerformanceLimitsConfig limits = AIDecisionConfigManager.Config.PerformanceLimits;
        return _utilityAIUsageThisTurn < limits.MaxUtilityAITroopsPerTurn;
    }

    /// <summary>
    /// 使用 UtilityAI 决策（高级）
    /// </summary>
    private static AIDecision UseUtilityAI(
        Troop troop,
        List<Troop> enemies,
        List<Troop> allies,
        StrategicPosture posture,
        string keyTroopReason)
    {
        ActionProposal best = UtilityAIExecutor.FindBestAction(
            troop, 
            enemies, 
            allies, 
            posture
        );

        // 🔥 Anti-Band-Aid: 直接访问Leader.Name，如果为null会崩溃
        string decisionReason = $"UtilityAI({keyTroopReason})";
        
        LogDecision(
            $"[UtilityAI] {troop.Leader.Name}(部队{troop.ID}) " +
            $"选择 {best.Type} (效用分: {best.UtilityScore:F1}) " +
            $"原因: {keyTroopReason}"
        );

        return new AIDecision(best.MovePosition, best.Target, best.Type, decisionReason);
    }

    /// <summary>
    /// 使用传统AI决策（快速）
    /// </summary>
    private static AIDecision UseTraditionalAI(
        Troop troop,
        List<Troop> enemies,
        List<Troop> allies,
        string reason)
    {
        Point moveTarget = AITacticalManager.ExecuteTacticalDecision(
            troop, 
            enemies, 
            allies
        );

        // 简化的目标选择（传统系统）
        Troop target = SelectNearestEnemy(troop, enemies);

        string decisionReason = $"传统AI({reason})";
        
        // 🔥 隐式转换：Point -> Point2D
        Point2D moveTarget2D = moveTarget;
        
        LogDecision(
            $"[传统AI] {troop.Leader.Name}(部队{troop.ID}) " +
            $"移动到({moveTarget2D.X},{moveTarget2D.Y}) " +
            $"原因: {reason}"
        );

        return new AIDecision(moveTarget2D, target, ActionType.NormalAttack, decisionReason);
    }

    /// <summary>
    /// 选择最近的敌人（传统启发式）
    /// ✅ Anti-Band-Aid: 不做防御性空检查
    /// 🔥 如果列表里有null或Destroyed的敌人，说明数据源有问题，应该在收集时过滤
    /// </summary>
    private static Troop SelectNearestEnemy(Troop troop, List<Troop> enemies)
    {
        // 🔥 Anti-Band-Aid: 假设enemies列表已经过滤了null和Destroyed
        // 如果这里崩溃，说明调用方传入了脏数据
        
        if (enemies.Count == 0)
        {
            throw new InvalidOperationException(
                $"[数据错误] 部队 {troop.Leader.Name} (ID:{troop.ID}) 的敌人列表为空\n" +
                $"这说明战场态势评估有问题，不应该在没有敌人时调用AI决策"
            );
        }

        Troop nearest = enemies[0];  // 🔥 直接访问，如果为null会崩溃
        // 🔥 隐式转换：Point -> Point2D
        Point2D troopPos = troop.Position;
        Point2D nearestPos = nearest.Position;
        int minDistance = troopPos.ManhattanDistance(nearestPos);

        // ✅ 使用for循环（虽然是Cold Path，但保持一致性）
        for (int i = 1; i < enemies.Count; i++)
        {
            Troop enemy = enemies[i];
            // 🔥 Anti-Band-Aid: 直接访问，如果enemy为null会崩溃
            // 这会暴露enemies列表收集时的问题
            Point2D enemyPos = enemy.Position;
            int distance = troopPos.ManhattanDistance(enemyPos);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 记录决策日志
    /// </summary>
    private static void LogDecision(string message)
    {
        DebugSettingsConfig debug = AIDecisionConfigManager.Config.DebugSettings;
        if (debug.EnableLogging && debug.LogKeyTroopDecisions)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    /// <summary>
    /// 记录性能日志
    /// </summary>
    private static void LogPerformance(string message)
    {
        DebugSettingsConfig debug = AIDecisionConfigManager.Config.DebugSettings;
        if (debug.EnableLogging && debug.LogPerformanceMetrics)
        {
            System.Diagnostics.Debug.WriteLine($"[性能] {message}");
        }
    }

    /// <summary>
    /// 获取本回合UtilityAI使用统计
    /// </summary>
    public static string GetUsageStats()
    {
        PerformanceLimitsConfig limits = AIDecisionConfigManager.Config.PerformanceLimits;
        int maxUsage = limits.MaxUtilityAITroopsPerTurn;
        
        return $"UtilityAI使用次数: {_utilityAIUsageThisTurn}/{maxUsage}";
    }
}
