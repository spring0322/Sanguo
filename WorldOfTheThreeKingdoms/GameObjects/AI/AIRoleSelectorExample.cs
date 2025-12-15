using System;
using GameObjects;
using GameObjects.AI;

namespace GameObjects.AI
{
    /// <summary>
    /// AI角色选择器使用示例
    /// </summary>
    public static class AIRoleSelectorExample
    {
        /// <summary>
        /// 演示如何使用AI角色选择器
        /// </summary>
        /// <param name="troop">要分析的部队</param>
        public static void DemonstrateRoleSelection(Troop troop)
        {
            if (troop == null)
            {
                Console.WriteLine("部队为空，无法分析角色");
                return;
            }

            // 获取AI推荐的角色
            AIRole recommendedRole = AIRoleSelector.GetBestRole(troop);

            // 输出分析结果
            Console.WriteLine($"=== 部队角色分析 ===");
            Console.WriteLine($"部队ID: {troop.ID}");
            Console.WriteLine($"主将: {troop.Leader?.Name ?? "无"}");
            
            if (troop.Army != null)
            {
                Console.WriteLine($"兵种ID: {troop.Army.KindID}");
                Console.WriteLine($"兵种名称: {troop.Army.Kind?.Name ?? "未知"}");
            }

            Console.WriteLine($"推荐角色: {GetRoleDescription(recommendedRole)}");
            
            // 显示主将属性
            if (troop.Leader != null)
            {
                Console.WriteLine($"主将属性 - 武力: {troop.Leader.Strength}, 统率: {troop.Leader.Command}, 智力: {troop.Leader.Intelligence}");
            }

            Console.WriteLine("==================");
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        /// <param name="role">AI角色</param>
        /// <returns>角色描述</returns>
        private static string GetRoleDescription(AIRole role)
        {
            switch (role)
            {
                case AIRole.Tank:
                    return "肉盾 - 适合前排承受伤害，卡位控制";
                case AIRole.DPS:
                    return "输出 - 适合物理攻击，造成大量伤害";
                case AIRole.Mage:
                    return "法师 - 适合使用策略，控制战场";
                case AIRole.Support:
                    return "辅助 - 适合治疗和增益，支援队友";
                case AIRole.Logistics:
                    return "后勤 - 适合运输和建造，非战斗单位";
                case AIRole.None:
                default:
                    return "未定义 - 无法确定合适角色";
            }
        }

        /// <summary>
        /// 批量分析多个部队的角色
        /// </summary>
        /// <param name="troops">部队列表</param>
        public static void AnalyzeTroopRoles(TroopList troops)
        {
            if (troops == null || troops.Count == 0)
            {
                Console.WriteLine("没有部队需要分析");
                return;
            }

            Console.WriteLine($"=== 批量角色分析 ({troops.Count}个部队) ===");

            int tankCount = 0, dpsCount = 0, mageCount = 0, supportCount = 0, logisticsCount = 0, noneCount = 0;

            foreach (Troop troop in troops)
            {
                AIRole role = AIRoleSelector.GetBestRole(troop);
                
                switch (role)
                {
                    case AIRole.Tank: tankCount++; break;
                    case AIRole.DPS: dpsCount++; break;
                    case AIRole.Mage: mageCount++; break;
                    case AIRole.Support: supportCount++; break;
                    case AIRole.Logistics: logisticsCount++; break;
                    default: noneCount++; break;
                }
            }

            Console.WriteLine($"角色分布统计:");
            Console.WriteLine($"  肉盾: {tankCount}个");
            Console.WriteLine($"  输出: {dpsCount}个");
            Console.WriteLine($"  法师: {mageCount}个");
            Console.WriteLine($"  辅助: {supportCount}个");
            Console.WriteLine($"  后勤: {logisticsCount}个");
            Console.WriteLine($"  未定义: {noneCount}个");
            Console.WriteLine("========================");
        }
    }
}