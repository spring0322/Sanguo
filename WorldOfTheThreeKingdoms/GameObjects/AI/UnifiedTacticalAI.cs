using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

#if false

/// <summary>
/// 统一战术AI系统 - 整合所有AI模块的协调器
/// 🔥 设计原则：
/// 1. 避免重复功能
/// 2. 配置驱动，零硬编码
/// 3. 与现有异步寻路系统协同
/// 4. Cold Path设计，性能优化
/// </summary>
public static class UnifiedTacticalAI
{
    /// <summary>
    /// 为军团执行完整的战术AI回合
    /// 调用时机：Legion.AI() 开始时
    /// ✅ Anti-Band-Aid：不做防御性空检查
    /// 🧊 Cold Path：AI决策，允许LINQ，优先可读性
    /// </summary>
    public static void ExecuteLegionTacticalTurn(Legion legion)
    {
        // ========== 阶段1：战略态势判定（Cold Path，回合开始时调用一次）==========
        StrategicPosture posture = EvaluateLegionPosture(legion);

        // ========== 阶段2：军团角色分配（Cold Path）==========
        LegionRoleAllocator.AssignAndApplyRoles(legion);

(legion);
        AITargetSelector.SortActionOrder(activeTroops);

        // ========== 阶段4：依次执行每个部队的战术决策（Hot Path）==========
        foreach (var troop in activeTroops)
        {
            ExecuteTroopTacticalTurn(troop, posture);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[统一战术AI] 军团 {legion.Name} 完成回合，态势={posture}，部队数={activeTroops.Count}"
        );
    }

    /// <summary>
    /// 为单个部队执行战术决策
    /// ✅ Anti-Band-Aid：Destroyed检查是业务逻辑，保留；null检查移除
    /// </summary>
    private static void ExecuteTroopTacticalTurn(Troop troop, StrategicPosture posture)
    {
        if (troop.Destroyed)
        {
            return;
        }

        // 1. 收集战场信息
        var enemies = AITroopCollector.CollectVisibleEnemies(troop);
        var allies = AITroopCollector.CollectVisibleAllies(troop);

        if (enemies.Count == 0)
        {
            // 无敌军时，执行巡逻或驻守逻辑
            ExecutePatrolLogic(troop, posture);
            return;
        }

        // 2. 选择最佳攻击目标
        Troop target = AITargetSelector.SelectBestTarget(troop, enemies, allies);

        if (target == null)
        {
            // 无合适目标，执行战术移动
            ExecuteTacticalMovement(troop, enemies, allies, posture);
            return;
        }

        // 3. 执行攻击或移动到攻击位置
        ExecuteAttackOrApproach(troop, target, enemies, allies, posture);
    }

    /// <summary>
    /// 评估军团的战略态势
    /// ✅ Anti-Band-Aid：不做防御性空检查
    /// </summary>
    private static StrategicPosture EvaluateLegionPosture(Legion legion)
    {
        if (legion.WillArchitecture == null)
        {
            return StrategicPosture.Attack;
        }

        // 🧊 Cold Path：使用LINQ提高可读性
        List<Troop> friendlyTroops = AITroopCollector.CollectAllTroops(legion);

(
            legion.WillArchitecture, 
            legion.BelongedFaction
        );

        // 使用PostureEvaluator评估态势
        return PostureEvaluator.EvaluatePosture(
            legion.WillArchitecture,
            friendlyTroops,
            enemyTroops
        );
    }

    /// <summary>
    /// 收集可行动的部队
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    private static List<Troop> CollectActiveTroops(Legion legion)
    {
        return legion.Troops.GetList()
            .Where(troop => troop != null && 
                           !troop.Destroyed && 
                           troop.Controllable && 
                           troop.MovabilityLeft > 0)
            .ToList();
    }

    /// <summary>
    /// 收集视野内的敌军
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    private static List<Troop> CollectVisibleEnemies(Troop troop)
    {
        if (troop.hostileTroopsInView == null)
        {
            return [];
        }

        return troop.hostileTroopsInView.GetList()
            .Where(enemy => enemy != null && !enemy.Destroyed)
            .ToList();
    }

    /// <summary>
    /// 收集视野内的友军
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    private static List<Troop> CollectVisibleAllies(Troop troop)
    {
        if (troop.friendlyTroopsInView == null)
        {
            return [];
        }

        return troop.friendlyTroopsInView.GetList()
            .Where(ally => ally != null && !ally.Destroyed && ally != troop)
            .ToList();
    }

    /// <summary>
    /// 收集建筑附近的敌军
    /// ⚠️ 已废弃：请使用 AITroopCollector.CollectEnemyTroopsNearArchitecture()
    /// </summary>
    [Obsolete("请使用 AITroopCollector.CollectEnemyTroopsNearArchitecture()")]
    private static List<Troop> CollectEnemyTroopsNearArchitecture(
        Architecture architecture, 
        Faction faction)
    {
        List<Troop> enemies = [];

        if (architecture.ViewArea == null)
        {
            return enemies;
        }

        foreach (var point in architecture.ViewArea.Area)
        {
            Troop troop = Session.Current.Scenario.GetTroopByPositionNoCheck(point);
            if (troop != null && 
                !troop.Destroyed && 
                troop.BelongedFaction != null &&
                !faction.IsFriendly(troop.BelongedFaction))
            {
                if (!enemies.Contains(troop))
                {
                    enemies.Add(troop);
                }
            }
        }

        return enemies;
    }

    /// <summary>
    /// 执行战术移动（无攻击目标时）
    /// </summary>
    private static void ExecuteTacticalMovement(
        Troop troop, 
        List<Troop> enemies, 
        List<Troop> allies,
        StrategicPosture posture)
    {
        // 使用战术定位系统计算最佳位置
        Point2D bestPos = TacticalPositioning.GetBestTacticalPosition(
            troop, 
            enemies, 
            allies, 
            posture
        );

        if (bestPos == troop.Position)
        {
            return;
        }

        // 🔥 与异步寻路系统协同：使用异步寻路
        RequestAsyncPathfinding(troop, bestPos);
    }

    /// <summary>
    /// 执行攻击或接近目标
    /// </summary>
    private static void ExecuteAttackOrApproach(
        Troop troop, 
        Troop target,
        List<Troop> enemies, 
        List<Troop> allies,
        StrategicPosture posture)
    {
        Point2D troopPos = troop.Position;
        Point2D targetPos = target.Position;
        int distance = troopPos.ManhattanDistance(targetPos);
        int attackRange = troop.OffenceRadius;

        if (distance <= attackRange)
        {
            // 在攻击范围内，直接攻击
            troop.AttackTroop(target);
        }
        else
        {
            // 不在攻击范围内，计算接近位置
            Point2D approachPos = CalculateApproachPosition(
                troop, 
                target, 
                enemies, 
                allies, 
                posture
            );

            // 🔥 与异步寻路系统协同
            RequestAsyncPathfinding(troop, approachPos);
        }
    }

    /// <summary>
    /// 计算接近目标的最佳位置
    /// </summary>
    private static Point2D CalculateApproachPosition(
        Troop troop, 
        Troop target,
        List<Troop> enemies, 
        List<Troop> allies,
        StrategicPosture posture)
    {
        // 使用战术定位系统，但限制在目标附近
        Point2D targetPos = target.Position;
        int attackRange = troop.OffenceRadius;

        // 生成目标周围的候选位置
        List<Point2D> candidates = [];
        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Point2D candidate = new Point2D(targetPos.X + dx, targetPos.Y + dy);
                
                // 检查是否在攻击范围内
                if (candidate.ManhattanDistance(targetPos) <= attackRange)
                {
                    candidates.Add(candidate);
                }
            }
        }

        // 从候选位置中选择战术价值最高的
        Point2D bestPos = troop.Position;
        float bestScore = float.MinValue;

        foreach (var candidate in candidates)
        {
            // 检查位置是否可通行
            if (Session.Current.Scenario.PositionOutOfRange(candidate))
            {
                continue;
            }

            // 评估战术价值
            float score = TacticalPositioning.EvaluatePositionScore(
                candidate, 
                troop, 
                enemies, 
                allies, 
                posture
            );

            if (score > bestScore)
            {
                bestScore = score;
                bestPos = candidate;
            }
        }

        return bestPos;
    }

    /// <summary>
    /// 执行巡逻逻辑（无敌军时）
    /// </summary>
    private static void ExecutePatrolLogic(Troop troop, StrategicPosture posture)
    {
        // 根据态势决定巡逻行为
        switch (posture)
        {
            case StrategicPosture.Attack:
                // 进攻态势：向目标建筑移动
                if (troop.WillArchitecture != null)
                {
                    RequestAsyncPathfinding(troop, troop.WillArchitecture.Position);
                }
                break;

            case StrategicPosture.Defense:
            case StrategicPosture.Garrison:
                // 防守/驻守态势：返回起始建筑
                if (troop.StartingArchitecture != null)
                {
                    RequestAsyncPathfinding(troop, troop.StartingArchitecture.Position);
                }
                break;
        }
    }

    /// <summary>
    /// 请求异步寻路（与现有异步寻路系统协同）
    /// 🔥 关键修复：正确使用异步寻路系统
    /// </summary>
    private static void RequestAsyncPathfinding(Troop troop, Point2D destination)
    {
        if (destination == troop.Position)
        {
            return;
        }

        // 🔥 使用现有的异步寻路系统
        // 1. 检查是否已经在寻路中（避免重复请求）
        if (troop._isPathfinding)
        {
            System.Diagnostics.Debug.WriteLine($"[统一战术AI] {troop.DisplayName} 正在寻路中，跳过重复请求");
            return;
        }

        // 2. 设置目标位置
        troop.RealDestination = destination;

        // 3. 请求异步寻路（使用现有系统的方法）
        // 注意：CalculatePathAsync 会自动设置 _isPathfinding 标志
        troop.CalculatePathAsync(new Point(destination.X, destination.Y), (success) =>
        {
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[统一战术AI] {troop.DisplayName} 寻路成功到 {destination}");
                // 设置AI状态为进军
                troop.CurrentAIState = TroopAIState.Marching;
                troop.Action = TroopAction.Move;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[统一战术AI] {troop.DisplayName} 寻路失败到 {destination}");
            }
        });
    }
}
#endif
