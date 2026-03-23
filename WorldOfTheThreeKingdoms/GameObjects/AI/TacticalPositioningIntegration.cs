using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

#if false

/// <summary>
/// ⚠️ 【示例代码】ZOC战术定位系统集成示例
/// 📚 用途：展示如何在实际游戏中使用战术定位功能
/// 🚫 注意：这是示例代码，不应在生产环境直接使用
/// ✅ 实际使用：请参考 UnifiedTacticalAI.cs 中的集成方式
/// </summary>
public static class TacticalPositioningIntegration
{
    /// <summary>
    /// 为部队计算最佳战术位置（完整流程，支持战略态势）
    /// </summary>
    /// <param name="troop">当前行动的部队（不能为 null）</param>
    /// <param name="posture">战略态势（默认为进攻）</param>
    /// <returns>最佳移动目标点</returns>
    public static Point GetBestTacticalMove(Troop troop, StrategicPosture posture = StrategicPosture.Attack)
    {
        // 🔥 数据契约：部队必须有所属势力
        if (troop.BelongedFaction == null)
        {
            System.Diagnostics.Debug.WriteLine($"[战术定位] 错误：部队 {troop.ID} 没有所属势力");
            return troop.Position;
        }

        // 1. 确保部队已分配角色
        if (troop.CurrentRole == TroopRole.None)
        {
            troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
        }

        // 2. 获取视野内的敌军和友军
        var enemies = GetVisibleEnemies(troop);
        var allies = GetVisibleAllies(troop);

        if (enemies.Count == 0)
        {
            // 无敌军时，使用默认移动逻辑
            return troop.Position;
        }

        // 3. 调用战术定位器（传入战略态势）
        Point2D bestPos = TacticalPositioning.GetBestTacticalPosition(troop, enemies, allies, posture);

        return bestPos;
    }

    /// <summary>
    /// 为军团内所有部队计算战术位置（批量处理，支持战略态势）
    /// </summary>
    /// <param name="legion">军团（不能为 null）</param>
    /// <param name="posture">战略态势（默认为进攻）</param>
    /// <returns>部队ID到目标位置的映射</returns>
    public static Dictionary<int, Point> GetLegionTacticalMoves(Legion legion, StrategicPosture posture = StrategicPosture.Attack)
    {
        var result = new Dictionary<int, Point>();

        // 🔥 数据契约：军团必须有部队列表
        if (legion.Troops == null)
        {
            System.Diagnostics.Debug.WriteLine($"[战术定位] 错误：军团 {legion.ID} 的 Troops 为 null");
            return result;
        }

        // 1. 先进行军团级角色分配
        LegionRoleAllocator.AssignAndApplyRoles(legion);

        // 2. 为每个部队计算战术位置
        foreach (Troop troop in legion.Troops.GetList())
        {
            if (troop == null)
            {
                System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：军团 {legion.ID} 的部队列表中存在 null");
                continue;
            }
            
            if (!troop.Destroyed)
            {
                Point bestMove = GetBestTacticalMove(troop, posture);
                result[troop.ID] = bestMove;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取视野内的敌军列表
    /// </summary>
    private static List<Troop> GetVisibleEnemies(Troop troop)
    {
        // 🔥 使用 C# 12 集合表达式
        List<Troop> enemies = [];

        // 🔥 数据契约：hostileTroopsInView 应该在部队初始化时创建
        if (troop.hostileTroopsInView == null)
        {
            System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：部队 {troop.ID} 的 hostileTroopsInView 为 null");
            return enemies;
        }

        foreach (Troop enemy in troop.hostileTroopsInView.GetList())
        {
            // 🔥 只过滤已销毁的部队，null 说明数据源有问题
            if (enemy == null)
            {
                System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：部队 {troop.ID} 的视野列表中存在 null 敌军");
                continue;
            }
            
            if (!enemy.Destroyed)
            {
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    /// <summary>
    /// 获取视野内的友军列表
    /// </summary>
    private static List<Troop> GetVisibleAllies(Troop troop)
    {
        // 🔥 使用 C# 12 集合表达式
        List<Troop> allies = [];

        // 🔥 数据契约：friendlyTroopsInView 应该在部队初始化时创建
        if (troop.friendlyTroopsInView == null)
        {
            System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：部队 {troop.ID} 的 friendlyTroopsInView 为 null");
            return allies;
        }

        foreach (Troop ally in troop.friendlyTroopsInView.GetList())
        {
            // 🔥 只过滤已销毁的部队和自己，null 说明数据源有问题
            if (ally == null)
            {
                System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：部队 {troop.ID} 的视野列表中存在 null 友军");
                continue;
            }
            
            if (!ally.Destroyed && ally != troop)
            {
                allies.Add(ally);
            }
        }

        return allies;
    }

    /// <summary>
    /// 应用战术移动（设置部队目标）
    /// </summary>
    /// <param name="troop">部队（不能为 null）</param>
    /// <param name="targetPosition">目标位置</param>
    public static void ApplyTacticalMove(Troop troop, Point targetPosition)
    {
        if (targetPosition == troop.Position)
            return;

        // 🔥 设置部队的移动目标
        // 注意：这里需要根据实际的 Troop 类接口调整
        troop.SetDestination(targetPosition);
    }

    /// <summary>
    /// 为势力的所有军团执行战术定位（支持战略态势）
    /// </summary>
    /// <param name="faction">势力（不能为 null）</param>
    /// <param name="posture">战略态势（默认为进攻）</param>
    public static void ExecuteFactionTacticalPositioning(Faction faction, StrategicPosture posture = StrategicPosture.Attack)
    {
        // 🔥 数据契约：势力必须有军团列表
        if (faction.Legions == null)
        {
            System.Diagnostics.Debug.WriteLine($"[战术定位] 错误：势力 {faction.ID} 的 Legions 为 null");
            return;
        }

        foreach (Legion legion in faction.Legions.GetList())
        {
            if (legion == null)
            {
                System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：势力 {faction.ID} 的军团列表中存在 null");
                continue;
            }

            var tacticalMoves = GetLegionTacticalMoves(legion, posture);

            // 应用移动指令
            foreach (var kvp in tacticalMoves)
            {
                var troop = faction.Troops.GetGameObject(kvp.Key) as Troop;
                
                if (troop == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[战术定位] 警告：找不到 ID={kvp.Key} 的部队");
                    continue;
                }
                
                ApplyTacticalMove(troop, kvp.Value);
            }
        }
    }

    /// <summary>
    /// 根据战场态势自动判断战略态势
    /// </summary>
    public static StrategicPosture DeterminePosture(Faction faction)
    {
        // 🔥 简化逻辑：根据势力的整体实力和战场情况判断
        // 这里可以根据实际需求扩展更复杂的判断逻辑
        
        if (faction == null)
            return StrategicPosture.Attack;

        // 示例逻辑：可以根据势力的部队数量、士气、资源等判断
        // 这里先返回默认值，实际使用时可以扩展
        return StrategicPosture.Attack;
    }
}
#endif
