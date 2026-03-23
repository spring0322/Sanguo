using System;
using System.Collections.Generic;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

#if false

/// <summary>
/// ⚠️ 【示例代码】战略态势判定器集成示例
/// 📚 用途：展示如何在现有AI系统中使用PostureEvaluator
/// 🚫 注意：这是示例代码，不应在生产环境直接使用
/// ✅ 实际使用：请参考 UnifiedTacticalAI.cs 中的集成方式
/// 🔥 Cold Path：回合开始时调用一次，性能开销极低
/// </summary>
public static class PostureEvaluatorIntegration
{
    /// <summary>
    /// 示例1：在军团AI中使用态势判定
    /// 调用时机：Legion.AI() 开始时
    /// </summary>
    public static void IntegrateWithLegionAI(Legion legion)
    {
        if (legion == null || legion.WillArchitecture == null || legion.BelongedFaction == null)
        {
            return;
        }

        // 收集己方部队
        List<Troop> friendlyTroops = [];
        foreach (Troop troop in legion.Troops)
        {
            if (troop != null && !troop.Destroyed)
            {
                friendlyTroops.Add(troop);
            }
        }

        // 收集敌方部队（从目标城市视野内获取）
        List<Troop> enemyTroops = [];
        Architecture targetCity = legion.WillArchitecture;
        
        if (targetCity.ViewArea != null)
        {
            foreach (var point in targetCity.ViewArea.Area)
            {
                Troop troop = WorldOfTheThreeKingdoms.GameManager.Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                if (troop != null && 
                    !troop.Destroyed && 
                    troop.BelongedFaction != null &&
                    !legion.BelongedFaction.IsFriendly(troop.BelongedFaction))
                {
                    if (!enemyTroops.Contains(troop))
                    {
                        enemyTroops.Add(troop);
                    }
                }
            }
        }

        // 评估战略态势
        StrategicPosture posture = PostureEvaluator.EvaluatePosture(
            targetCity,
            friendlyTroops,
            enemyTroops
        );

        // 根据态势调整军团行为
        ApplyPostureToLegion(legion, posture);

        System.Diagnostics.Debug.WriteLine(
            $"[态势判定] 军团 {legion.Name} 目标 {targetCity.Name}：" +
            $"态势={posture}, 己方战力={friendlyTroops.Count}部队, 敌方={enemyTroops.Count}部队"
        );
    }

    /// <summary>
    /// 示例2：在势力AI中使用态势判定
    /// 调用时机：Faction.AI() 中，决定整体战略方向
    /// </summary>
    public static void IntegrateWithFactionAI(Faction faction)
    {
        if (faction == null || faction.Architectures == null)
        {
            return;
        }

        // 使用势力级别的态势判定
        StrategicPosture factionPosture = PostureEvaluator.EvaluateFactionPosture(faction);

        System.Diagnostics.Debug.WriteLine(
            $"[态势判定] 势力 {faction.Name} 整体态势：{factionPosture}"
        );

        // 根据势力态势调整全局策略
        switch (factionPosture)
        {
            case StrategicPosture.Attack:
                // 进攻态势：积极扩张，主动寻找战机
                System.Diagnostics.Debug.WriteLine($"[态势判定] {faction.Name} 进入进攻态势");
                break;

            case StrategicPosture.Defense:
                // 防守态势：依托地形，节节抗击
                System.Diagnostics.Debug.WriteLine($"[态势判定] {faction.Name} 进入防守态势");
                break;

            case StrategicPosture.Garrison:
                // 驻守态势：死守要道，稳如泰山
                System.Diagnostics.Debug.WriteLine($"[态势判定] {faction.Name} 进入驻守态势");
                break;
        }
    }

    /// <summary>
    /// 示例3：在部队AI中使用态势判定
    /// 调用时机：Troop.AI() 中，决定战术行为
    /// </summary>
    public static void IntegrateWithTroopAI(Troop troop)
    {
        if (troop == null || 
            troop.BelongedFaction == null || 
            troop.StartingArchitecture == null)
        {
            return;
        }

        // 收集周边友军
        List<Troop> friendlyTroops = [];
        foreach (Troop ally in troop.BelongedFaction.Troops.GetList())
        {
            if (ally != null && 
                !ally.Destroyed && 
                GetDistance(troop.Position, ally.Position) <= 10)
            {
                friendlyTroops.Add(ally);
            }
        }

        // 收集视野内敌军
        List<Troop> enemyTroops = [];
        if (troop.ViewArea != null)
        {
            foreach (var point in troop.ViewArea.Area)
            {
                Troop enemy = WorldOfTheThreeKingdoms.GameManager.Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                if (enemy != null && 
                    !enemy.Destroyed && 
                    enemy.BelongedFaction != null &&
                    !troop.BelongedFaction.IsFriendly(enemy.BelongedFaction))
                {
                    if (!enemyTroops.Contains(enemy))
                    {
                        enemyTroops.Add(enemy);
                    }
                }
            }
        }

        // 评估局部态势
        StrategicPosture localPosture = PostureEvaluator.EvaluatePosture(
            troop.StartingArchitecture,
            friendlyTroops,
            enemyTroops
        );

        // 根据态势调整部队行为
        ApplyPostureToTroop(troop, localPosture);
    }

    /// <summary>
    /// 根据态势调整军团行为
    /// </summary>
    private static void ApplyPostureToLegion(Legion legion, StrategicPosture posture)
    {
        // 这里可以根据态势调整军团的具体行为
        // 例如：调整进攻强度、选择不同的战术等
        
        // 示例：根据态势设置军团的战术偏好
        foreach (Troop troop in legion.Troops)
        {
            if (troop != null && !troop.Destroyed)
            {
                ApplyPostureToTroop(troop, posture);
            }
        }
    }

    /// <summary>
    /// 根据态势调整部队行为
    /// </summary>
    private static void ApplyPostureToTroop(Troop troop, StrategicPosture posture)
    {
        // 这里可以根据态势调整部队的具体行为
        // 例如：调整移动策略、攻击优先级等
        
        // 示例：根据态势调整部队的战术参数
        switch (posture)
        {
            case StrategicPosture.Attack:
                // 进攻态势：提高攻击性，降低防守性
                // troop.AggressiveLevel = 0.8f; // 示例参数
                break;

            case StrategicPosture.Defense:
                // 防守态势：平衡攻守
                // troop.AggressiveLevel = 0.5f;
                break;

            case StrategicPosture.Garrison:
                // 驻守态势：优先防守
                // troop.AggressiveLevel = 0.2f;
                break;
        }
    }

    /// <summary>
    /// 计算两点之间的曼哈顿距离
    /// </summary>
    private static int GetDistance(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}

#endif
