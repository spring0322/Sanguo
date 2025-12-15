using System;
using System.Collections.Generic;
using System.Text;
using GameObjects;
using GameGlobal;

namespace GameGlobal
{
    /// <summary>
    /// AI目标选择器 (C# 7.3 修复版)
    /// 智能选择最佳攻击目标，考虑战损比、状态利用、兵种克制等因素
    /// </summary>
    public static class AITargetSelector
    {
        // -----------------------------------------------------------------------
        // 1. 辅助函数：解决低版本没有 Math.Clamp 的问题
        // -----------------------------------------------------------------------
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // -----------------------------------------------------------------------
        // 2. 核心入口
        // -----------------------------------------------------------------------
        public static Troop GetBestAttackTarget(Troop attacker, List<Troop> enemies)
        {
            try
            {
                if (attacker == null || enemies == null || enemies.Count == 0) return null;

                // 兼容老代码的 Role 判断
                TroopRole attackerRole = AIRoleSelector.DetermineRole(attacker);
                if (attackerRole == TroopRole.Mage)
                {
                    return GetBestDebuffTarget(attacker, enemies);
                }

                Troop bestTarget = null;
                float maxScore = -9999f;

                foreach (Troop target in enemies)
                {
                    if (target == null) continue;
                    if (!CanAttackTarget(attacker, target)) continue;

                    float score = CalculateAttackScore(attacker, target);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestTarget = target;
                    }
                }

                return bestTarget;
            }
            catch
            {
                return null;
            }
        }

        public static Troop GetBestDebuffTarget(Troop mage, List<Troop> visibleEnemies)
        {
            try
            {
                if (mage == null || visibleEnemies == null) return null;

                Troop bestDebuffTarget = null;
                float maxDebuffScore = -9999f;

                foreach (Troop target in visibleEnemies)
                {
                    if (target == null || target.Leader == null) continue;

                    // 假设 IsTargetConfused 是你要调用的方法
                    if (IsTargetConfused(target)) continue;
                    if (!CanCastStrategyOn(mage, target)) continue;

                    float score = (float)target.Leader.Strength + (float)target.Leader.Command;

                    if (mage.Leader != null && mage.Leader.Intelligence > target.Leader.Intelligence + 20)
                    {
                        score += 300f;
                    }

                    if (score > maxDebuffScore)
                    {
                        maxDebuffScore = score;
                        bestDebuffTarget = target;
                    }
                }

                return bestDebuffTarget;
            }
            catch 
            { 
                return null; 
            }
        }

        // -----------------------------------------------------------------------
        // 3. 评分逻辑 (修复 C# 7.3 语法不兼容)
        // -----------------------------------------------------------------------
        private static float CalculateAttackScore(Troop attacker, Troop target)
        {
            float score = 0f;
            float dmg = CalculateEstimatedDamage(attacker, target);

            // 修复：显式类型转换，防止 Quantity 类型不明确
            if (dmg >= (float)target.Quantity) return 10000f;

            score += dmg;
            score += CalculateRestraintBonus(attacker, target);

            // 修复：解决 Point 模糊引用，手动计算距离
            int distX = Math.Abs(attacker.Position.X - target.Position.X);
            int distY = Math.Abs(attacker.Position.Y - target.Position.Y);
            score -= (float)(distX + distY) * 10f;

            return score;
        }

        private static float CalculateRestraintBonus(Troop attacker, Troop target)
        {
            // 修复：C# 7.3 不支持 "is not null" 或 "{ Kind: ... }" 这种写法
            if (attacker.Army == null) return 0f;
            if (attacker.Army.Kind == null) return 0f;
            if (target.Army == null) return 0f;
            if (target.Army.Kind == null) return 0f;

            int atkID = attacker.Army.Kind.ID;
            int defID = target.Army.Kind.ID;

            // 修复：C# 7.3 不支持 "id is 2 or 3"，必须用 "||"
            // 戟兵(11) 克 骑兵(2,3,400)
            if (atkID == 11)
            {
                if (defID == 2 || defID == 3 || defID == 400) return 1500f;
            }

            // 骑兵(2,3) 克 弓兵(1,15)
            if (atkID == 2 || atkID == 3)
            {
                if (defID == 1 || defID == 15) return 1500f;
            }

            // 弓兵(1,15) 克 步兵(10)
            if (atkID == 1 || atkID == 15)
            {
                if (defID == 10) return 800f;
            }

            return 0f;
        }

        private static float CalculateEstimatedDamage(Troop attacker, Troop target)
        {
            if (attacker.Leader == null || target.Leader == null) return 0f;

            float atk = (float)attacker.Leader.Strength + (float)attacker.Leader.Command * 0.5f;
            float def = (float)target.Leader.Command + (float)target.Leader.Strength * 0.3f;
            float damage = atk - def * 0.5f;

            // 修复：使用自定义 Clamp 或手动 if
            if (damage < atk * 0.1f) damage = atk * 0.1f;

            damage *= ((float)attacker.Quantity / 1000f);
            return damage;
        }

        // -----------------------------------------------------------------------
        // 4. 辅助判定 (修复 Point 和 语法)
        // -----------------------------------------------------------------------
        private static bool CanAttackTarget(Troop attacker, Troop target)
        {
            int distX = Math.Abs(attacker.Position.X - target.Position.X);
            int distY = Math.Abs(attacker.Position.Y - target.Position.Y);
            int dist = distX + distY;

            int range = 1;
            if (attacker.Army != null && attacker.Army.Kind != null)
            {
                int id = attacker.Army.Kind.ID;
                // 修复：C# 7.3 写法
                if (id == 1 || id == 15 || id == 32 || id == 301) range = 3;
                else if (id == 2 || id == 3 || id == 16 || id == 17) range = 2;
            }

            return dist <= range;
        }

        private static bool CanCastStrategyOn(Troop caster, Troop target)
        {
            int distX = Math.Abs(caster.Position.X - target.Position.X);
            int distY = Math.Abs(caster.Position.Y - target.Position.Y);
            return (distX + distY) <= GetStrategyRange(caster);
        }

        private static int GetStrategyRange(Troop caster)
        {
            if (caster == null || caster.Leader == null) return 2;

            int intel = caster.Leader.Intelligence;
            if (intel >= 90) return 4;
            if (intel >= 70) return 3;
            return 2;
        }

        private static bool IsTargetConfused(Troop target)
        {
            return false; // 等待你连接实际逻辑
        }

        /// <summary>
        /// 释放策略技能
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标</param>
        /// <param name="strategyId">策略技能ID</param>
        /// <returns>是否成功释放</returns>
        public static bool CastStrategy(Troop caster, Troop target, int strategyId)
        {
            try
            {
                if (caster == null || target == null) return false;
                if (caster.Leader == null) return false;

                // 检查距离
                if (!CanCastStrategyOn(caster, target)) return false;

                // 检查是否拥有该技能
                if (!HasStrategy(caster, strategyId)) return false;

                // 简化的成功率计算
                int successRate = CalculateStrategySuccessRate(caster, target, strategyId);
                Random random = new Random();
                bool success = random.Next(0, 100) < successRate;

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI策略] {caster.Leader.Name} 成功对 {target.Leader?.Name} 释放策略 {strategyId}");
                    // 这里应该调用实际的策略效果逻辑
                    ApplyStrategyEffect(target, strategyId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AI策略] {caster.Leader.Name} 对 {target.Leader?.Name} 释放策略 {strategyId} 失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] CastStrategy 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否拥有指定策略
        /// </summary>
        private static bool HasStrategy(Troop troop, int strategyId)
        {
            try
            {
                if (troop?.Leader?.Skills == null) return false;

                foreach (var skill in troop.Leader.Skills.GetSkillList())
                {
                    if (skill?.Kind != null && skill.Kind.ID == strategyId)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 计算策略成功率
        /// </summary>
        private static int CalculateStrategySuccessRate(Troop caster, Troop target, int strategyId)
        {
            try
            {
                if (caster?.Leader == null || target?.Leader == null) return 0;

                int baseRate = 50; // 基础成功率
                int intelligenceDiff = caster.Leader.Intelligence - target.Leader.Intelligence;
                
                // 智力差影响成功率
                baseRate += intelligenceDiff * 2;

                // 特定策略的修正
                switch (strategyId)
                {
                    case 391: // 惊营
                        baseRate += 10;
                        break;
                    case 390: // 攻心
                        baseRate += 5;
                        break;
                }

                // 限制在0-95之间
                if (baseRate < 0) baseRate = 0;
                if (baseRate > 95) baseRate = 95;

                return baseRate;
            }
            catch
            {
                return 50; // 默认成功率
            }
        }

        /// <summary>
        /// 应用策略效果
        /// </summary>
        private static void ApplyStrategyEffect(Troop target, int strategyId)
        {
            try
            {
                // 这里应该调用实际的游戏逻辑来应用策略效果
                // 暂时只记录日志
                System.Diagnostics.Debug.WriteLine($"[AI策略] 对 {target.Leader?.Name} 应用策略效果 {strategyId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] ApplyStrategyEffect 失败: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // 5. 那些被"吃掉"的辅助方法 (完全保留，且修复了 Point)
        // -----------------------------------------------------------------------
        public static List<Troop> GetTargetsInRange(Troop attacker, List<Troop> allEnemies, int maxRange = 5)
        {
            List<Troop> targetsInRange = new List<Troop>();
            if (allEnemies == null) return targetsInRange;

            foreach (Troop enemy in allEnemies)
            {
                if (enemy == null) continue;

                int dist = Math.Abs(attacker.Position.X - enemy.Position.X) + 
                          Math.Abs(attacker.Position.Y - enemy.Position.Y);

                if (dist <= maxRange)
                {
                    targetsInRange.Add(enemy);
                }
            }

            return targetsInRange;
        }

        public static Troop GetWeakestTarget(List<Troop> targets)
        {
            Troop weakest = null;
            int lowestQuantity = int.MaxValue;
            if (targets == null) return null;

            foreach (Troop target in targets)
            {
                if (target == null) continue;

                if (target.Quantity < lowestQuantity)
                {
                    lowestQuantity = target.Quantity;
                    weakest = target;
                }
            }

            return weakest;
        }

        public static Troop GetMostValuableTarget(List<Troop> targets)
        {
            Troop mostValuable = null;
            float highestValue = 0f;
            if (targets == null) return null;

            foreach (Troop target in targets)
            {
                if (target == null) continue;

                float value = (float)target.Quantity * 0.1f; 
                if (target.Leader != null)
                {
                    value += (float)(target.Leader.Strength + target.Leader.Command);
                }

                if (value > highestValue)
                {
                    highestValue = value;
                    mostValuable = target;
                }
            }

            return mostValuable;
        }

        // 修复：不使用 AppendLine 里的 C# 6.0+ 插值，改用 String.Format 兼容性最强
        public static string GetTargetSelectionReport(Troop attacker, List<Troop> targets)
        {
            try
            {
                StringBuilder report = new StringBuilder();
                string attackerName = (attacker != null && attacker.Leader != null) ? attacker.Leader.Name : "未知";
                
                report.AppendLine(string.Format("=== {0} 目标选择报告 ===", attackerName));
                report.AppendLine(string.Format("候选目标数: {0}", targets.Count));

                Troop bestTarget = GetBestAttackTarget(attacker, targets);
                if (bestTarget != null)
                {
                    float score = CalculateAttackScore(attacker, bestTarget);
                    string targetName = (bestTarget.Leader != null) ? bestTarget.Leader.Name : "未知";
                    report.AppendLine(string.Format("最佳目标: {0} (评分: {1:F1})", targetName, score));
                }
                else
                {
                    report.AppendLine("未找到合适的攻击目标");
                }

                return report.ToString();
            }
            catch (Exception ex)
            {
                return "报告生成失败: " + ex.Message;
            }
        }
    }
}