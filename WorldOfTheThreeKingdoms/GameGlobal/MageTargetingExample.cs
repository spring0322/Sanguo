using System;
using System.Collections.Generic;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 法师目标选择示例
    /// 展示法师专用的控制目标选择逻辑
    /// </summary>
    public static class MageTargetingExample
    {
        /// <summary>
        /// 法师控制目标选择示例
        /// </summary>
        public static void MageControlTargetingExample(Troop mage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 法师控制目标选择示例 ===");

                if (mage == null) return;

                // 确认是法师角色
                TroopRole role = AIRoleSelector.DetermineRole(mage);
                if (role != TroopRole.Mage)
                {
                    System.Diagnostics.Debug.WriteLine($"{mage.Leader?.Name} 不是法师角色，跳过");
                    return;
                }

                // 获取视野内的敌军
                var visibleEnemies = GetVisibleEnemies(mage);
                if (visibleEnemies.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("未发现敌军目标");
                    return;
                }

                // 使用法师专用目标选择
                var bestDebuffTarget = AITargetSelector.GetBestDebuffTarget(mage, visibleEnemies);
                
                if (bestDebuffTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"法师 {mage.Leader?.Name} 选择控制目标: {bestDebuffTarget.Leader?.Name}");
                    
                    // 执行控制技能
                    bool success = AITargetSelector.CastStrategy(mage, bestDebuffTarget, 391);
                    System.Diagnostics.Debug.WriteLine($"惊营技能释放结果: {(success ? "成功" : "失败")}");
                }

                System.Diagnostics.Debug.WriteLine("法师控制目标选择示例完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"法师控制目标选择示例失败: {ex.Message}");
            }
        }

        private static List<Troop> GetVisibleEnemies(Troop troop)
        {
            // 简化实现，实际应该调用游戏的视野系统
            return new List<Troop>();
        }
    }
}