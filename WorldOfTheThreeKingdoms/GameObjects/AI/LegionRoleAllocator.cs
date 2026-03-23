using System;
using System.Collections.Generic;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

/// <summary>
/// 军团级角色分配器
/// 解决"全员DPS"问题，强制保证军团的阵容平衡
/// </summary>
public static class LegionRoleAllocator
{
    /// <summary>
    /// 为军团内的所有部队分配角色
    /// </summary>
    /// <param name="legionTroops">军团内的所有部队（不能为 null）</param>
    /// <returns>分配好的角色字典</returns>
    public static Dictionary<Troop, TroopRole> AllocateLegionRoles(List<Troop> legionTroops)
    {
        var result = new Dictionary<Troop, TroopRole>();
        
        // 🔥 数据契约：调用方必须保证参数非 null
        if (legionTroops.Count == 0)
            return result;

        try
        {
            // 1. 准备记分卡
            var scoreCards = new Dictionary<Troop, Dictionary<TroopRole, float>>();
            var pool = new HashSet<Troop>();

            foreach (var troop in legionTroops)
            {
                if (troop == null) continue;

                // 优先处理后勤，直接锁定，不参与分配
                int kindID = GetTroopKindID(troop);
                if (AIRoleConfigManager.IsTroopKindForRole(kindID, "Logistics"))
                {
                    result[troop] = TroopRole.Logistics;
                    continue;
                }

                // 计算该部队所有维度的分数
                var scores = new Dictionary<TroopRole, float>
                {
                    { TroopRole.Tank, AIRoleSelector.CalculateTankScore(troop) },
                    { TroopRole.DPS, AIRoleSelector.CalculateDpsScore(troop) },
                    { TroopRole.Mage, AIRoleSelector.CalculateMageScore(troop) },
                    { TroopRole.Support, AIRoleSelector.CalculateSupportScore(troop) }
                };
                
                scoreCards[troop] = scores;
                pool.Add(troop);
            }

            // 2. 定义编制需求（根据军团人数动态调整）
            var formationConfig = AITacticalConfigManager.GetFormationConfig(pool.Count);
            int tankSlots = formationConfig.TankSlots;
            int supportSlots = formationConfig.SupportSlots;

            // 3. 竞聘上岗（贪心算法）
            
            // --- 第一轮：选拔辅助 (Support) ---
            AllocateRole(pool, scoreCards, result, TroopRole.Support, supportSlots, "Support");

            // --- 第二轮：选拔肉盾 (Tank) ---
            AllocateRole(pool, scoreCards, result, TroopRole.Tank, tankSlots, "Tank");

            // --- 第三轮：其余人自由选择 (DPS/Mage) ---
            AllocateRemainingRoles(pool, scoreCards, result);

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[军团分配] AllocateLegionRoles 失败: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// 分配指定角色
    /// </summary>
    private static void AllocateRole(
        HashSet<Troop> pool,
        Dictionary<Troop, Dictionary<TroopRole, float>> scoreCards,
        Dictionary<Troop, TroopRole> result,
        TroopRole role,
        int slots,
        string roleName)
    {
        var config = AITacticalConfigManager.Config.LegionFormation;
        float minScore = 0f;

        // 获取最低分数要求
        if (config?.RoleThresholds != null && 
            config.RoleThresholds.TryGetValue(roleName, out var threshold))
        {
            minScore = threshold.MinScore;
        }

        for (int i = 0; i < slots; i++)
        {
            if (pool.Count == 0) break;

            // 找该角色分数最高的
            Troop bestCandidate = null;
            float bestScore = float.MinValue;

            foreach (var troop in pool)
            {
                if (scoreCards[troop].TryGetValue(role, out float score) && score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = troop;
                }
            }

            // 检查是否达标
            if (bestCandidate != null && bestScore >= minScore)
            {
                result[bestCandidate] = role;
                pool.Remove(bestCandidate);
            }
            else
            {
                break; // 无合格人选
            }
        }
    }

    /// <summary>
    /// 为剩余部队分配角色
    /// </summary>
    private static void AllocateRemainingRoles(
        HashSet<Troop> pool,
        Dictionary<Troop, Dictionary<TroopRole, float>> scoreCards,
        Dictionary<Troop, TroopRole> result)
    {
        var config = AITacticalConfigManager.Config.LegionFormation;
        float mageMinScore = 60f;

        if (config?.RoleThresholds != null && 
            config.RoleThresholds.TryGetValue("Mage", out var mageThreshold))
        {
            mageMinScore = mageThreshold.MinScore;
        }

        foreach (var troop in pool)
        {
            float dpsScore = scoreCards[troop][TroopRole.DPS];
            float mageScore = scoreCards[troop][TroopRole.Mage];

            if (mageScore > dpsScore && mageScore >= mageMinScore)
            {
                result[troop] = TroopRole.Mage;
            }
            else
            {
                result[troop] = TroopRole.DPS;
            }
        }
    }

    /// <summary>
    /// 获取部队兵种ID
    /// </summary>
    private static int GetTroopKindID(Troop troop)
    {
        // 🔥 ANTI-BAND-AID：数据源验证，不使用防御性检查
        if (troop.Army == null)
        {
            throw new InvalidOperationException($"数据损坏：部队 {troop.ID} 的 Army 为 null，应在数据加载时修复");
        }
        
        if (troop.Army.Kind == null)
        {
            throw new InvalidOperationException($"数据损坏：部队 {troop.ID} 的 Army.Kind 为 null (MilitaryID={troop.Army.ID})，应在数据加载时修复");
        }
        
        return troop.Army.Kind.ID;
    }

    /// <summary>
    /// 批量为军团分配角色并应用到部队
    /// </summary>
    public static void AssignAndApplyRoles(Legion legion)
    {
        // 🔥 ANTI-BAND-AID：数据源验证，不使用防御性检查
        if (legion.Troops == null)
        {
            throw new InvalidOperationException($"数据损坏：军团 {legion.ID} 的 Troops 为 null，应在数据加载时修复");
        }

        // 🔥 使用 C# 12 集合表达式
        List<Troop> troops = [];
        
        foreach (Troop troop in legion.Troops.GetList())
        {
            if (troop == null)
            {
                throw new InvalidOperationException($"数据损坏：军团 {legion.ID} 的部队列表中存在 null，应在数据加载时修复");
            }
            
            troops.Add(troop);
        }

        var roleAssignments = AllocateLegionRoles(troops);

        // 应用角色分配
        foreach (var kvp in roleAssignments)
        {
            kvp.Key.CurrentRole = kvp.Value;
        }
    }
}
