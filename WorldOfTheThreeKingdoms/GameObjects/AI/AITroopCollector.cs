using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameObjects.AI;

/// <summary>
/// AI部队收集工具类（统一实现）
/// 🎯 目的：消除重复的敌军/友军收集逻辑
/// 🧊 Cold Path：AI决策层，使用LINQ提高可读性
/// 📍 使用位置：所有需要收集部队的AI模块
/// </summary>
public static class AITroopCollector
{
    /// <summary>
    /// 收集视野内的敌军（统一实现）
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    public static List<Troop> CollectVisibleEnemies(Troop troop)
    {
        var list = troop.GetHostileTroopsInView();
        if (list == null)
        {
            return new List<Troop>();
        }

        return list.GetList().OfType<Troop>()
            .Where(enemy => enemy != null && !enemy.Destroyed)
            .ToList();
    }

    /// <summary>
    /// 收集视野内的友军（统一实现）
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    public static List<Troop> CollectVisibleAllies(Troop troop)
    {
        if (troop.BelongedFaction == null || troop.ViewArea == null)
        {
            return new List<Troop>();
        }

        return troop.BelongedFaction.Troops.GetList().OfType<Troop>()
            .Where(ally => ally != null && !ally.Destroyed && ally != troop && troop.ViewArea.HasPoint(ally.Position))
            .ToList();
    }

    /// <summary>
    /// 收集建筑附近的敌军（统一实现）
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    public static List<Troop> CollectEnemyTroopsNearArchitecture(
        Architecture architecture, 
        Faction faction)
    {
        if (architecture.ViewArea == null)
        {
            return new List<Troop>();
        }

        return architecture.ViewArea.Area
            .Select(point => GameManager.Session.Current.Scenario.GetTroopByPositionNoCheck(point))
            .Where(t => t != null && 
                           !t.Destroyed && 
                           t.BelongedFaction != null &&
                           !faction.IsFriendly(t.BelongedFaction))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 收集军团内可行动的部队（统一实现）
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    public static List<Troop> CollectActiveTroops(Legion legion)
    {
        if (legion.Troops == null)
        {
            return new List<Troop>();
        }

        return legion.Troops.GetList().OfType<Troop>()
            .Where(t => t != null && 
                           !t.Destroyed && 
                           t.Controllable && 
                           t.MovabilityLeft > 0)
            .ToList();
    }

    /// <summary>
    /// 收集军团内所有部队（包括不可行动的）
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    public static List<Troop> CollectAllTroops(Legion legion)
    {
        if (legion.Troops == null)
        {
            return new List<Troop>();
        }

        return legion.Troops.GetList().OfType<Troop>()
            .Where(t => t != null && !t.Destroyed)
            .ToList();
    }
}
