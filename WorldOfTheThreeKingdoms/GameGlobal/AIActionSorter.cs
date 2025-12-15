using System.Collections.Generic;
using System.Linq; // 必须引用 Linq 方便排序
using GameObjects;
using GameGlobal;

namespace GameGlobal
{
    /// <summary>
    /// AI行动排序器
    /// 根据战术角色智能排序AI部队的行动顺序，实现最优战术配合
    /// </summary>
    public static class AIActionSorter
    {
        /// <summary>
        /// 对 AI 的行动队列进行智能排序
        /// </summary>
        /// <param name="originalTroops">原始部队列表</param>
        /// <returns>按战术优先级排序后的部队列表</returns>
        public static List<Troop> SortTroopActions(List<Troop> originalTroops)
        {
            try
            {
                if (originalTroops == null || originalTroops.Count == 0)
                {
                    return new List<Troop>();
                }

                // 使用我们在第一阶段写的 TacticalRole 进行排序
                var sortedTroops = originalTroops.OrderBy(t => GetActionPriority(t)).ToList();

                System.Diagnostics.Debug.WriteLine($"[AI行动排序] 完成排序，共 {sortedTroops.Count} 支部队");
                LogSortingResult(sortedTroops);

                return sortedTroops;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] SortTroopActions 失败: {ex.Message}");
                return originalTroops ?? new List<Troop>();
            }
        }

        /// <summary>
        /// 获取部队的行动优先级
        /// </summary>
        /// <param name="t">部队</param>
        /// <returns>优先级数字（越小越先行动）</returns>
        private static int GetActionPriority(Troop t)
        {
            try
            {
                if (t == null) return 999; // 空部队最后处理

                // 获取部队的战术角色
                TroopRole role = AIRoleSelector.DetermineRole(t);

                // 数字越小，越先行动
                switch (role)
                {
                    // 1. 纯辅助先动：开鼓舞(ID 397)、回气光环，保证大家满状态
                    case TroopRole.Support: 
                        return 1;

                    // 2. 法师/控制先动：先尝试把敌人晕住(ID 391 惊营)，降低敌人反击率
                    case TroopRole.Mage: 
                        return 2;

                    // 3. 肉盾动：先上去利用 ZOC 卡住位置，防止敌人逃跑，形成包围网
                    case TroopRole.Tank: 
                        return 3;

                    // 4. DPS 最后动：此时敌人可能已经晕了/被包围了(有加成)，上去收割
                    case TroopRole.DPS: 
                        return 4;

                    // 5. 其他角色
                    default: 
                        return 5;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] GetActionPriority 失败: {ex.Message}");
                return 999; // 出错的部队最后处理
            }
        }

        /// <summary>
        /// 记录排序结果到调试日志
        /// </summary>
        /// <param name="sortedTroops">排序后的部队列表</param>
        private static void LogSortingResult(List<Troop> sortedTroops)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AI行动排序] 排序结果:");
                
                for (int i = 0; i < sortedTroops.Count; i++)
                {
                    var troop = sortedTroops[i];
                    if (troop?.Leader != null)
                    {
                        var role = AIRoleSelector.DetermineRole(troop);
                        var priority = GetActionPriority(troop);
                        
                        System.Diagnostics.Debug.WriteLine($"  {i + 1}. {troop.Leader.Name} ({GetRoleDescription(role)}) - 优先级: {priority}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] LogSortingResult 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取角色描述
        /// </summary>
        /// <param name="role">战术角色</param>
        /// <returns>角色描述</returns>
        private static string GetRoleDescription(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.Support: return "辅助";
                case TroopRole.Mage: return "法师";
                case TroopRole.Tank: return "肉盾";
                case TroopRole.DPS: return "输出";
                case TroopRole.Balanced: return "均衡";
                default: return "未知";
            }
        }

        /// <summary>
        /// 获取势力所有部队的智能行动顺序
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>按战术优先级排序的部队列表</returns>
        public static List<Troop> GetFactionActionOrder(Faction faction)
        {
            try
            {
                if (faction?.Troops == null)
                {
                    return new List<Troop>();
                }

                // 获取所有可行动的部队
                var availableTroops = new List<Troop>();
                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop != null && troop.Quantity > 0)
                    {
                        availableTroops.Add(troop);
                    }
                }

                // 进行智能排序
                return SortTroopActions(availableTroops);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] GetFactionActionOrder 失败: {ex.Message}");
                return new List<Troop>();
            }
        }

        /// <summary>
        /// 检查部队是否应该优先执行特定行动
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="actionType">行动类型</param>
        /// <returns>是否应该优先执行</returns>
        public static bool ShouldPrioritizeAction(Troop troop, string actionType)
        {
            try
            {
                if (troop == null || string.IsNullOrEmpty(actionType))
                {
                    return false;
                }

                TroopRole role = AIRoleSelector.DetermineRole(troop);

                switch (actionType.ToLower())
                {
                    case "buff":
                    case "support":
                        return role == TroopRole.Support;

                    case "control":
                    case "debuff":
                        return role == TroopRole.Mage;

                    case "zoc":
                    case "block":
                        return role == TroopRole.Tank;

                    case "attack":
                    case "damage":
                        return role == TroopRole.DPS;

                    default:
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] ShouldPrioritizeAction 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取推荐的行动类型
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>推荐的行动类型</returns>
        public static string GetRecommendedActionType(Troop troop)
        {
            try
            {
                if (troop == null) return "wait";

                TroopRole role = AIRoleSelector.DetermineRole(troop);

                switch (role)
                {
                    case TroopRole.Support:
                        return "support"; // 优先使用辅助技能

                    case TroopRole.Mage:
                        return "control"; // 优先使用控制技能

                    case TroopRole.Tank:
                        return "zoc"; // 优先执行ZOC卡位

                    case TroopRole.DPS:
                        return "attack"; // 优先攻击

                    default:
                        return "balanced"; // 均衡行动
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI行动排序] GetRecommendedActionType 失败: {ex.Message}");
                return "wait";
            }
        }
    }
}