using System;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// AI技能选择器 - 决定是否施放技能
    /// </summary>
    public static class AISkillSelector
    {
        // 默认参数
        private const float BaseDamageParam = 1.0f;
        private const int EliteTroopThreshold = 5000;
        private const float ControlBaseProb = 0.5f;
        private const float SkillDamageMultiplier = 1.5f;

        /// <summary>
        /// 决定是否施放技能，返回 true 表示已行动
        /// </summary>
        public static bool TryCastSkill(Troop source, Troop target)
        {
            try
            {
                if (source == null || target == null) return false;
                if (source.Leader == null) return false;

                // 1. 斩杀线计算
                float normalDamage = CalculateDamage(source, target);
                
                // 如果普攻能直接击杀，就不浪费气力(MP)
                if (normalDamage >= target.Quantity)
                {
                    return false; // 返回 false 让系统去执行普攻
                }

                // 2. 技能判断
                // 遍历武将技能
                if (source.Leader.Skills != null)
                {
                    foreach (Skill skill in source.Leader.Skills.Skills.Values)
                    {
                        if (skill == null) continue;

                        // 检查是否是战斗技能
                        if (!skill.Combat) continue;

                        // 检查气力/士气限制 (使用技能等级作为消耗参考)
                        int skillCost = skill.Level * 10;
                        if (source.Morale < skillCost) continue;

                        // 3. 高价值目标判断
                        float skillDamage = normalDamage * GetSkillPowerMultiplier(skill);
                        bool isHighValueTarget = target.Quantity > EliteTroopThreshold;

                        if (skillDamage > normalDamage * SkillDamageMultiplier && isHighValueTarget)
                        {
                            // 释放技能 (返回true表示建议使用技能)
                            System.Diagnostics.Debug.WriteLine(
                                $"[AISkillSelector] {source.Leader.Name} 对 {target.Leader?.Name ?? "敌军"} 使用技能 {skill.Name}");
                            return true;
                        }
                        
                        // 4. 控制类技能判断 (基于技能Kind)
                        if (IsControlSkill(skill))
                        {
                            float successRate = CalculateControlSuccess(source, target);
                            if (successRate > 0.6f) // 60%以上把握才放
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[AISkillSelector] {source.Leader.Name} 对 {target.Leader?.Name ?? "敌军"} 使用控制技能 {skill.Name}");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISkillSelector] TryCastSkill 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算基础伤害
        /// </summary>
        private static float CalculateDamage(Troop source, Troop target)
        {
            // 简化伤害计算：基于战斗力差异
            float attackPower = source.FightingForce * BaseDamageParam;
            float defensePower = target.FightingForce * 0.5f;
            
            float damage = Math.Max(0, attackPower - defensePower);
            return damage;
        }

        /// <summary>
        /// 获取技能伤害倍率
        /// </summary>
        private static float GetSkillPowerMultiplier(Skill skill)
        {
            // 基于技能等级计算倍率
            return 1.0f + (skill.Level * 0.3f);
        }

        /// <summary>
        /// 判断是否是控制类技能
        /// </summary>
        private static bool IsControlSkill(Skill skill)
        {
            // 基于技能Kind判断 (假设Kind 2-5是控制技能)
            return skill.Kind >= 2 && skill.Kind <= 5;
        }

        /// <summary>
        /// 计算控制技能成功率
        /// </summary>
        private static float CalculateControlSuccess(Troop source, Troop target)
        {
            // 基于智力差异计算成功率
            float baseRate = ControlBaseProb;
            
            if (source.Leader != null && target.Leader != null)
            {
                int intDiff = source.Leader.Intelligence - target.Leader.Intelligence;
                baseRate += intDiff * 0.01f; // 每点智力差距增加1%成功率
            }

            // 士气影响
            if (target.Morale < 50)
            {
                baseRate += 0.1f; // 低士气目标更容易被控制
            }

            return Math.Min(0.95f, Math.Max(0.05f, baseRate));
        }

        /// <summary>
        /// 获取技能使用建议
        /// </summary>
        public static string GetSkillRecommendation(Troop source, Troop target)
        {
            if (source == null || target == null || source.Leader == null)
                return "无可用技能";

            if (source.Leader.Skills == null || source.Leader.Skills.Count == 0)
                return "无技能";

            float normalDamage = CalculateDamage(source, target);
            
            if (normalDamage >= target.Quantity)
                return "普攻即可击杀";

            foreach (Skill skill in source.Leader.Skills.Skills.Values)
            {
                if (skill == null || !skill.Combat) continue;

                float skillDamage = normalDamage * GetSkillPowerMultiplier(skill);
                if (skillDamage > normalDamage * SkillDamageMultiplier)
                {
                    return $"建议使用 {skill.Name} (预计伤害 {skillDamage:F0})";
                }
            }

            return "普通攻击";
        }
    }
}
