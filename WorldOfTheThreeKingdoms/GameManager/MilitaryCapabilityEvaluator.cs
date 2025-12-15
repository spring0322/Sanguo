using System;
using System.Linq;
using GameObjects;
using GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    public static class MilitaryCapabilityEvaluator
    {
        private const float TITLE_SCORE = 100f;       // 称号匹配基础分
        private const float SKILL_SCORE = 30f;        // 技能匹配加分
        private const float STAT_WEIGHT = 0.5f;       // 属性转化分
        private const float COMMAND_PENALTY = 2.0f;   // 统率不足时的惩罚系数

        /// <summary>
        /// 计算武将统领特定兵种的适性评分（支持软门槛）
        /// </summary>
        public static float CalculateCapability(Person person, MilitaryKind unitInfo)
        {
            if (person == null || unitInfo == null) return 0f;
            MilitaryType kind = unitInfo.Type; // Using MilitaryType instead of UnitKind

            float score = 100f; // 基础分，保证只要能带就有分

            // 1. 称号权限 (Title Permission) - 唯一硬门槛
            // 如果称号明确禁止带该类兵种，那确实不能带
            // Adapted: Using RealTitles collection instead of single Title
            bool hasRestrictiveTitle = false;
            bool titleMatches = false;
            
            if (person.RealTitles.Count > 0)
            {
                foreach (var title in person.RealTitles)
                {
                    // MilitaryKindOnly is an int (ID), not a MilitaryKind object
                    // -1 or 0 usually means "no restriction" (generic title)
                    int restrictedKindId = title.MilitaryKindOnly;
                    
                    if (restrictedKindId > 0)
                    {
                        hasRestrictiveTitle = true;
                        // Check if this title's restriction matches the target unit type
                        // We need to look up the MilitaryKind by ID and compare types
                        var restrictedKind = Session.Current?.Scenario?.GameCommonData?.AllMilitaryKinds?.GetMilitaryKind(restrictedKindId);
                        if (restrictedKind != null && restrictedKind.Type == kind)
                        {
                            titleMatches = true;
                        }
                    }
                    else
                    {
                        // Generic title allows all (MilitaryKindOnly <= 0 means no restriction)
                        titleMatches = true;
                    }
                }
                
                // Only block if ALL titles are restrictive and NONE match
                if (hasRestrictiveTitle && !titleMatches)
                {
                    return 0f;
                }
                
                // 称号吻合加分
                if (titleMatches && hasRestrictiveTitle) score += TITLE_SCORE;
            }

            // 2. 统率软门槛 (Flexible Command Requirement)
            // 需求统率 80，实际 60 -> 差距 20 -> 扣 40 分
            // 但不会直接归零，保证在无人可用时，低统率将领也能顶上去
            if (person.Command < unitInfo.MinCommand)
            {
                float gap = unitInfo.MinCommand - person.Command;
                score -= gap * COMMAND_PENALTY;
            }

            // 3. 兵种强度修正 (Tier Bonus)
            // 鼓励优先选高级兵 (MaxScale 越大通常越强)
            score += (unitInfo.MaxScale / 1000f) * 10f;

            // 4. 属性适应性 (Stats)
            // Using MilitaryType enum values
            switch (kind)
            {
                case MilitaryType.骑兵: 
                    score += person.Command * 0.6f + person.Strength * 0.4f;
                    break;
                case MilitaryType.弩兵:  
                    score += person.Strength * 0.5f + person.Intelligence * 0.3f;
                    break;
                case MilitaryType.器械:   
                    score += person.Intelligence * 0.8f;
                    break;
                case MilitaryType.水军:
                    score += person.Command * 0.6f + person.Intelligence * 0.4f;
                    break;
                default: // 步兵
                    score += person.Command * 0.8f + person.Strength * 0.2f;
                    break;
            }

            // 5. 技能加成
            // Adapted: Using Skills.GetSkillList() instead of direct iteration
            if (person.Skills != null)
            {
                foreach (GameObject obj in person.Skills.GetSkillList())
                {
                    if (obj is Skill skill)
                    {
                        // Using MilitaryTypeOnly instead of ApplyToUnitKinds
                        if (skill.MilitaryTypeOnly == kind)
                        {
                            score += SKILL_SCORE;
                        }
                    }
                }
            }

            // 确保最低有 1 分 (只要称号不冲突)，防止完全选不中人
            return Math.Max(1.0f, score);
        }

        /// <summary>
        /// Helper to retrieve the Navy MilitaryKind from scenario configuration.
        /// </summary>
        public static MilitaryKind GetNavyMilitaryKind()
        {
            if (Session.Current == null || Session.Current.Scenario == null) return null;
            
            var commonData = Session.Current.Scenario.GameCommonData;
            if (commonData != null && commonData.AllMilitaryKinds != null)
            {
                foreach (MilitaryKind k in commonData.AllMilitaryKinds.MilitaryKinds.Values)
                {
                    if (k.Type == MilitaryType.水军)
                        return k;
                }
            }
            return null;
        }
    }
}
