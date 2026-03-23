using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.AI;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI
{
#if false
    /// <summary>
    /// AI行动序列器 (Phase 4)
    /// 负责优化部队的行动顺序，实现战术协同
    /// 顺序：Support/Mage (铺垫) -> Tank (卡位) -> DPS (收割)
    /// </summary>
    public static class AIActionSequencer
    {
        /// <summary>
        /// 获取优化后的行动队列
        /// 按照战术角色优先级排序，确保最佳的协同效果
        /// </summary>
        /// <param name="unsortedTroops">未排序的部队列表</param>
        /// <returns>按战术优先级排序的部队列表</returns>
        public static List<Troop> GetSortedTurnOrder(List<Troop> unsortedTroops)
        {
            try
            {
                if (unsortedTroops == null || unsortedTroops.Count == 0)
                {
                    Console.WriteLine("[AIActionSequencer] 输入的部队列表为空");
                    return new List<Troop>();
                }

                Console.WriteLine($"[AIActionSequencer] 开始排序 {unsortedTroops.Count} 个部队的行动顺序");

                // 创建副本避免修改原列表
                List<Troop> sortedList = new List<Troop>(unsortedTroops);

                // 确保所有部队都有分配的角色
                foreach (var troop in sortedList)
                {
                    if (troop != null && troop.CurrentRole == WorldOfTheThreeKingdoms.GameGlobal.TroopRole.None)
                    {
                        troop.CurrentRole = WorldOfTheThreeKingdoms.GameGlobal.AIRoleSelector.DetermineRole(troop);
                        Console.WriteLine($"[AIActionSequencer] 为部队 {troop.ID} 自动分配角色: {GetRoleDescription(troop.CurrentRole)}");
                    }
                }

                // 按照战术优先级排序
                sortedList.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;

                    int priorityA = GetRolePriority(a.CurrentRole);
                    int priorityB = GetRolePriority(b.CurrentRole);

                    // 优先级高的排在前面 (降序)
                    int result = priorityB.CompareTo(priorityA);

                    // 如果优先级相同，按照部队能力排序
                    if (result == 0)
                    {
                        result = GetTroopCapability(b).CompareTo(GetTroopCapability(a));
                    }

                    return result;
                });

                // 输出排序结果
                Console.WriteLine("[AIActionSequencer] 行动顺序排序完成:");
                for (int i = 0; i < sortedList.Count; i++)
                {
                    var troop = sortedList[i];
                    if (troop != null)
                    {
                        Console.WriteLine($"  {i + 1}. 部队 {troop.ID} ({GetRoleDescription(troop.CurrentRole)}) - 优先级: {GetRolePriority(troop.CurrentRole)}");
                    }
                }

                return sortedList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIActionSequencer] 排序行动顺序时发生错误: {ex.Message}");
                return unsortedTroops ?? new List<Troop>();
            }
        }

        /// <summary>
        /// 获取角色的行动优先级
        /// 数值越高，行动顺序越靠前
        /// </summary>
        /// <param name="role">战术角色</param>
        /// <returns>优先级数值</returns>
        private static int GetRolePriority(WorldOfTheThreeKingdoms.GameGlobal.TroopRole role)
        {
            switch (role)
            {
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support:
                    return 50; // 最先动：开鼓舞/加Buff，为队友提供支援

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Mage:
                    return 40; // 次先动：控制敌人/AOE削血，创造战术优势

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank:
                    return 30; // 再次：上去卡住位置，形成包围和控制

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS:
                    return 20; // 最后：进场收割残血/被控目标，完成击杀

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Logistics:
                    return 10; // 后勤：远离战斗，执行辅助任务

                default:
                    return 0; // 未定义角色最低优先级
            }
        }

        /// <summary>
        /// 获取部队的综合能力值（用于同优先级内的排序）
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>综合能力值</returns>
        private static int GetTroopCapability(Troop troop)
        {
            try
            {
                if (troop?.Leader == null)
                    return 0;

                // 根据角色计算不同的能力权重
                switch (troop.CurrentRole)
                {
                    case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support:
                        // 辅助看智力和统率
                        return troop.Leader.Intelligence + troop.Leader.Command;

                    case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Mage:
                        // 法师主要看智力
                        return troop.Leader.Intelligence * 2 + troop.Leader.Command;

                    case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank:
                        // 坦克看统率和武力
                        return troop.Leader.Command * 2 + troop.Leader.Strength;

                    case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS:
                        // 输出看武力
                        return troop.Leader.Strength * 2 + troop.Leader.Command;

                    default:
                        // 默认综合能力
                        return troop.Leader.Command + troop.Leader.Strength + troop.Leader.Intelligence;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIActionSequencer] 计算部队能力时出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        /// <param name="role">战术角色</param>
        /// <returns>中文描述</returns>
        private static string GetRoleDescription(WorldOfTheThreeKingdoms.GameGlobal.TroopRole role)
        {
            switch (role)
            {
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank: return "肉盾";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS: return "输出";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Mage: return "法师";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support: return "辅助";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 按照优化顺序执行批量AI决策
        /// 确保部队按照最佳战术顺序行动
        /// </summary>
        /// <param name="aiTroops">AI控制的部队列表</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteSequencedAIDecisions(List<Troop> aiTroops, List<Troop> allTroops)
        {
            try
            {
                Console.WriteLine($"=== 开始执行序列化AI决策，共 {aiTroops.Count} 个部队 ===");

                // 1. 获取优化后的行动顺序
                var sortedTroops = GetSortedTurnOrder(aiTroops);

                // 2. 按顺序执行AI决策
                foreach (var troop in sortedTroops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        Console.WriteLine($"[SequencedAI] 执行部队 {troop.ID} ({GetRoleDescription(troop.CurrentRole)}) 的回合");
                        AIMapNavigationIntegrationExample.ExecuteAITacticalDecision(troop, allTroops);
                    }
                }

                Console.WriteLine("=== 序列化AI决策执行完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIActionSequencer] 执行序列化AI决策时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 按角色分组执行AI决策
        /// 同一角色的部队可以并行执行，不同角色按优先级顺序执行
        /// </summary>
        /// <param name="aiTroops">AI控制的部队列表</param>
        /// <param name="allTroops">场景中的所有部队</param>
        public static void ExecuteGroupedAIDecisions(List<Troop> aiTroops, List<Troop> allTroops)
        {
            try
            {
                Console.WriteLine($"=== 开始执行分组AI决策，共 {aiTroops.Count} 个部队 ===");

                // 1. 按角色分组
                var roleGroups = aiTroops
                    .Where(t => t != null && !t.Destroyed)
                    .GroupBy(t => t.CurrentRole)
                    .OrderByDescending(g => GetRolePriority(g.Key))
                    .ToList();

                // 2. 按优先级顺序执行每个角色组
                foreach (var roleGroup in roleGroups)
                {
                    var role = roleGroup.Key;
                    var troops = roleGroup.ToList();

                    Console.WriteLine($"[GroupedAI] 执行 {GetRoleDescription(role)} 角色组，共 {troops.Count} 个部队");

                    // 同一角色组内的部队可以并行执行（这里仍然串行，但可以扩展为并行）
                    foreach (var troop in troops)
                    {
                        if (troop != null && !troop.Destroyed)
                        {
                            Console.WriteLine($"[GroupedAI] 执行部队 {troop.ID} 的回合");
                            AIMapNavigationIntegrationExample.ExecuteAITacticalDecision(troop, allTroops);
                        }
                    }

                    Console.WriteLine($"[GroupedAI] {GetRoleDescription(role)} 角色组执行完成");
                }

                Console.WriteLine("=== 分组AI决策执行完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIActionSequencer] 执行分组AI决策时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取角色的战术描述
        /// </summary>
        /// <param name="role">战术角色</param>
        /// <returns>战术描述</returns>
        public static string GetRoleTacticalDescription(WorldOfTheThreeKingdoms.GameGlobal.TroopRole role)
        {
            switch (role)
            {
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support:
                    return "优先行动，提供Buff和治疗支援";

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Mage:
                    return "次优先行动，控制敌军和AOE削血";

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank:
                    return "中等优先级，卡位控制和保护友军";

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS:
                    return "后期行动，收割残血和被控敌军";

                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Logistics:
                    return "最低优先级，执行后勤和辅助任务";

                default:
                    return "未定义角色，默认优先级";
            }
        }

        /// <summary>
        /// 分析当前部队组合的战术优势
        /// </summary>
        /// <param name="troops">部队列表</param>
        /// <returns>战术分析结果</returns>
        public static string AnalyzeTacticalComposition(List<Troop> troops)
        {
            try
            {
                if (troops == null || troops.Count == 0)
                    return "没有可分析的部队";

                var roleCount = new Dictionary<WorldOfTheThreeKingdoms.GameGlobal.TroopRole, int>();
                
                // 统计各角色数量
                foreach (var troop in troops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        var role = troop.CurrentRole;
                        if (roleCount.ContainsKey(role))
                            roleCount[role]++;
                        else
                            roleCount[role] = 1;
                    }
                }

                var analysis = "战术组合分析:\n";
                foreach (var kvp in roleCount.OrderByDescending(x => GetRolePriority(x.Key)))
                {
                    analysis += $"  {GetRoleDescription(kvp.Key)}: {kvp.Value}个 - {GetRoleTacticalDescription(kvp.Key)}\n";
                }

                // 提供战术建议
                analysis += "\n战术建议:\n";
                if (!roleCount.ContainsKey(WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support) || roleCount[WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support] == 0)
                    analysis += "  - 缺少辅助单位，建议增加治疗和Buff支援\n";
                
                if (!roleCount.ContainsKey(WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank) || roleCount[WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank] == 0)
                    analysis += "  - 缺少肉盾单位，建议增加前排保护\n";
                
                if (roleCount.ContainsKey(WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS) && roleCount[WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS] > troops.Count / 2)
                    analysis += "  - 输出单位过多，建议平衡队伍组合\n";

                return analysis;
            }
            catch (Exception ex)
            {
                return $"分析战术组合时发生错误: {ex.Message}";
            }
        }
    }
#endif
}
