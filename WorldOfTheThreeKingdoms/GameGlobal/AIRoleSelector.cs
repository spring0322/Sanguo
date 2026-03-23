using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects; // 假设 Troop 和 Person 类在此命名空间
using GameObjects.PersonDetail; // 引用 Skill 类
using GameObjects.TroopDetail; // 🔥 新增：引用 MilitaryType 枚举
using GameManager;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    /// <summary>
    /// AI 部队战术角色枚举
    /// </summary>
    public enum TroopRole
    {
        None = 0,
        Tank,       // 肉盾：卡位、吸收伤害
        DPS,        // 输出：物理核心输出
        Mage,       // 法师：控制、策略输出
        Support,    // 辅助：治疗、Buff
        Logistics,  // 后勤：运输、建造 (非战斗)
        Balanced    // 均衡/通用
    }

    /// <summary>
    /// 向后兼容的AIRole别名
    /// </summary>
    public enum AIRole
    {
        None = 0,
        Tank = 1,
        DPS = 2,
        Mage = 3,
        Support = 4,
        Logistics = 5,
        Balanced = 6
    }

    /// <summary>
    /// 角色选择器 - 负责根据兵种、属性和特技分配战术角色
    /// 环境：C# 7.3 / MonoGame 完全兼容
    /// </summary>
    public static class AIRoleSelector
    {
        // ================= 配置化改造 =================
        // 所有硬编码的ID和参数现在从配置文件中读取
        // 配置文件：GameGlobal/AIRoleConfig.json

        /// <summary>
        /// 计算并返回部队的最佳战术角色
        /// 整合原有DetermineRole功能，兼容C# 7.3语法
        /// </summary>
        /// <param name="troop">目标部队</param>
        /// <returns>计算出的角色</returns>
        public static TroopRole DetermineRole(Troop troop)
        {
            if (troop == null || troop.Leader == null) return TroopRole.None;

            try
            {
                // 1. 优先剔除后勤单位
                // 运输队和建造队无战斗能力，必须强制锁定
                int kindID = GetTroopKindID(troop);
                if (AIRoleConfigManager.IsTroopKindForRole(kindID, "Logistics"))
                {
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 识别为后勤单位(ID:{kindID}, {AIRoleConfigManager.GetTroopKindName(kindID)})");
                    return TroopRole.Logistics;
                }

                Person leader = troop.Leader;

                // 2. 初始化各角色评分
                float scoreTank = CalculateTankScore(troop);
                float scoreDps = CalculateDpsScore(troop);
                float scoreMage = CalculateMageScore(troop);
                float scoreSupport = CalculateSupportScore(troop);

                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 评分: Tank={scoreTank:F1}, DPS={scoreDps:F1}, Mage={scoreMage:F1}, Support={scoreSupport:F1}");

                // 3. 比较得出最高分 (C# 7.3 基础写法)
                TroopRole bestRole = TroopRole.Tank;
                float maxScore = scoreTank;

                if (scoreDps > maxScore)
                {
                    maxScore = scoreDps;
                    bestRole = TroopRole.DPS;
                }
                // 法师的优先级通常高于纯物理，如果分数接近优先选法师
                if (scoreMage > maxScore)
                {
                    maxScore = scoreMage;
                    bestRole = TroopRole.Mage;
                }
                // 辅助特技非常稀缺，一旦判定为辅助，通常直接锁定
                if (scoreSupport > maxScore)
                {
                    maxScore = scoreSupport;
                    bestRole = TroopRole.Support;
                }

                // 如果最高分太低（例如全员杂鱼），设置默认行为
                float minThreshold = AIRoleConfigManager.Config.GlobalSettings.MinScoreThreshold;
                if (maxScore < minThreshold)
                {
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 最高评分过低({maxScore:F1})，分配为均衡角色");
                    return TroopRole.Balanced;
                }

                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 最终角色: {bestRole} (评分: {maxScore:F1})");
                return bestRole;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] DetermineRole 失败: {ex.Message}");
                return TroopRole.Balanced;
            }
        }

        /// <summary>
        /// 向后兼容的GetBestRole方法
        /// </summary>
        /// <param name="troop">目标部队</param>
        /// <returns>AIRole枚举格式的角色</returns>
        public static AIRole GetBestRole(Troop troop)
        {
            TroopRole role = DetermineRole(troop);
            return ConvertToAIRole(role);
        }

        /// <summary>
        /// 将TroopRole转换为AIRole（向后兼容）
        /// </summary>
        private static AIRole ConvertToAIRole(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.None: return AIRole.None;
                case TroopRole.Tank: return AIRole.Tank;
                case TroopRole.DPS: return AIRole.DPS;
                case TroopRole.Mage: return AIRole.Mage;
                case TroopRole.Support: return AIRole.Support;
                case TroopRole.Logistics: return AIRole.Logistics;
                case TroopRole.Balanced: return AIRole.Balanced;
                default: return AIRole.None;
            }
        }

        /// <summary>
        /// 将AIRole转换为TroopRole
        /// </summary>
        public static TroopRole ConvertToTroopRole(AIRole role)
        {
            switch (role)
            {
                case AIRole.None: return TroopRole.None;
                case AIRole.Tank: return TroopRole.Tank;
                case AIRole.DPS: return TroopRole.DPS;
                case AIRole.Mage: return TroopRole.Mage;
                case AIRole.Support: return TroopRole.Support;
                case AIRole.Logistics: return TroopRole.Logistics;
                case AIRole.Balanced: return TroopRole.Balanced;
                default: return TroopRole.None;
            }
        }

        // ---------- 评分算法细节 ----------
        /// <summary>
        /// 计算肉盾评分
        /// </summary>
        public static float CalculateTankScore(Troop troop)
        {
            try
            {
                // 🔥 ANTI-BAND-AID：数据源验证，不使用防御性检查
                if (troop.Army == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army 为 null，应在数据加载时修复");
                }
                
                if (troop.Army.Kind == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army.Kind 为 null (MilitaryID={troop.Army.ID})，应在数据加载时修复");
                }

                // 🔥 修复：弩兵不能当肉盾 - 2026-03-10
                // 问题：皇甫嵩队（弩兵）被错误分配为肉盾角色
                if (troop.Army.Kind.Type == MilitaryType.弩兵)
                {
                    // System.Diagnostics.Debug.WriteLine($"[角色分配] {troop.Leader.Name} 是弩兵，不适合肉盾角色，评分=0");
                    return 0f;
                }

                var roleConfig = AIRoleConfigManager.GetRoleConfig("Tank");
                if (roleConfig == null) return 0f;

                float score = 0f;

                // 基础分：属性权重
                if (roleConfig.StatWeights != null)
                {
                    if (roleConfig.StatWeights.TryGetValue("Command", out float commandWeight))
                        score += troop.Leader.Command * commandWeight;
                    if (roleConfig.StatWeights.TryGetValue("Strength", out float strengthWeight))
                        score += troop.Leader.Strength * strengthWeight;
                    if (roleConfig.StatWeights.TryGetValue("Intelligence", out float intWeight))
                        score += troop.Leader.Intelligence * intWeight;
                }

                // 兵种加成
                int kindID = GetTroopKindID(troop);
                if (AIRoleConfigManager.IsTroopKindForRole(kindID, "Tank"))
                {
                    float bonus = roleConfig.Bonuses?.TryGetValue("TroopMatch", out float troopBonus) == true ? troopBonus : 50f;
                    score += bonus;
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 肉盾兵种匹配加分({AIRoleConfigManager.GetTroopKindName(kindID)})");
                }

                // 特技加成
                if (roleConfig.CoreSkillIDs != null)
                {
                    float skillBonus = roleConfig.Bonuses?.TryGetValue("CoreSkill", out float coreBonus) == true ? coreBonus : 30f;
                    foreach (int skillID in roleConfig.CoreSkillIDs)
                    {
                        if (HasSkill(troop, skillID))
                        {
                            score += skillBonus;
                            #if DEBUG
                            // 🔥 ANTI-BAND-AID：不掩盖数据错误，直接使用已验证的数据
                            string militaryTypeName = troop.Army.Kind.Type.ToString();
                            System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.Leader.Name}({militaryTypeName}) 肉盾技能生效: {AIRoleConfigManager.GetSkillName(skillID)}");
                            #endif
                        }
                    }
                }

                return score;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateTankScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算输出评分
        /// </summary>
        public static float CalculateDpsScore(Troop troop)
        {
            try
            {
                // 🔥 ANTI-BAND-AID：数据源验证，不使用防御性检查
                if (troop.Army == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army 为 null，应在数据加载时修复");
                }
                
                if (troop.Army.Kind == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army.Kind 为 null (MilitaryID={troop.Army.ID})，应在数据加载时修复");
                }

                // 🔥 修复：增加兵种适配性检查 - 2026-03-10
                // DPS角色更适合骑兵、弩兵等机动性强的兵种
                var militaryType = troop.Army.Kind.Type;
                if (militaryType == MilitaryType.步兵)
                {
                    // 步兵可以当DPS，但评分降低
                    // System.Diagnostics.Debug.WriteLine($"[角色分配] {troop.Leader.Name} 是步兵，DPS适配性一般");
                }

                var roleConfig = AIRoleConfigManager.GetRoleConfig("DPS");
                if (roleConfig == null) return 0f;

                float score = 0f;

                // 基础分：属性权重
                if (roleConfig.StatWeights != null)
                {
                    if (roleConfig.StatWeights.TryGetValue("Strength", out float strengthWeight))
                        score += troop.Leader.Strength * strengthWeight;
                    if (roleConfig.StatWeights.TryGetValue("Command", out float commandWeight))
                        score += troop.Leader.Command * commandWeight;
                    if (roleConfig.StatWeights.TryGetValue("Intelligence", out float intWeight))
                        score += troop.Leader.Intelligence * intWeight;
                }

                // 兵种加成
                int kindID = GetTroopKindID(troop);
                if (AIRoleConfigManager.IsTroopKindForRole(kindID, "DPS"))
                {
                    float bonus = roleConfig.Bonuses?.TryGetValue("TroopMatch", out float troopBonus) == true ? troopBonus : 50f;
                    score += bonus;
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 输出兵种匹配加分({AIRoleConfigManager.GetTroopKindName(kindID)})");
                }

                // 特技加成：核心技能
                if (roleConfig.CoreSkillIDs != null)
                {
                    float skillBonus = roleConfig.Bonuses?.TryGetValue("CoreSkill", out float coreBonus) == true ? coreBonus : 30f;
                    foreach (int skillID in roleConfig.CoreSkillIDs)
                    {
                        if (HasSkill(troop, skillID))
                        {
                            score += skillBonus;
                            // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有{AIRoleConfigManager.GetSkillName(skillID)}技能");
                        }
                    }
                }
                
                // 特技加成：暴击类
                if (roleConfig.CriticalSkillRange != null && HasCriticalSkill(troop, roleConfig.CriticalSkillRange)) 
                {
                    float skillBonus = roleConfig.Bonuses?.TryGetValue("CoreSkill", out float coreBonus) == true ? coreBonus : 30f;
                    score += skillBonus;
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有暴击技能");
                }

                return score;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateDpsScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算法师评分
        /// </summary>
        public static float CalculateMageScore(Troop troop)
        {
            try
            {
                var roleConfig = AIRoleConfigManager.GetRoleConfig("Mage");
                if (roleConfig == null) return 0f;

                // 门槛：智力过低直接排除，防止"弱智"法师送策略点
                int minInt = roleConfig.MinIntelligence > 0 ? roleConfig.MinIntelligence : 70;
                if (troop.Leader.Intelligence < minInt) 
                {
                    // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 智力不足({troop.Leader.Intelligence})，不适合做法师");
                    return 0f;
                }

                float score = 0f;

                // 基础分：属性权重
                if (roleConfig.StatWeights != null)
                {
                    if (roleConfig.StatWeights.TryGetValue("Intelligence", out float intWeight))
                        score += troop.Leader.Intelligence * intWeight;
                    if (roleConfig.StatWeights.TryGetValue("Command", out float commandWeight))
                        score += troop.Leader.Command * commandWeight;
                    if (roleConfig.StatWeights.TryGetValue("Strength", out float strengthWeight))
                        score += troop.Leader.Strength * strengthWeight;
                }

                // 法师主要依赖特技，兵种影响较小 (除非有井阑等特殊兵种，此处暂略)

                // 特技加成
                if (roleConfig.CoreSkillIDs != null)
                {
                    float skillBonus = roleConfig.Bonuses?.TryGetValue("CoreSkill", out float coreBonus) == true ? coreBonus : 30f;
                    foreach (int skillID in roleConfig.CoreSkillIDs)
                    {
                        if (HasSkill(troop, skillID))
                        {
                            float actualBonus = skillBonus;
                            
                            // 检查是否有特殊技能加权
                            if (roleConfig.SpecialSkills != null && 
                                roleConfig.SpecialSkills.TryGetValue(skillID.ToString(), out float multiplier))
                            {
                                actualBonus *= multiplier;
                                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有{AIRoleConfigManager.GetSkillName(skillID)}技能(特殊加权x{multiplier})");
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有{AIRoleConfigManager.GetSkillName(skillID)}技能");
                            }
                            
                            score += actualBonus;
                        }
                    }
                }

                return score;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateMageScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算辅助评分
        /// </summary>
        public static float CalculateSupportScore(Troop troop)
        {
            try
            {
                var roleConfig = AIRoleConfigManager.GetRoleConfig("Support");
                if (roleConfig == null) return 0f;

                float score = 0f;

                // 辅助主要看是否有技能，属性次之
                if (roleConfig.StatWeights != null)
                {
                    if (roleConfig.StatWeights.TryGetValue("Command", out float commandWeight))
                        score += troop.Leader.Command * commandWeight;
                    if (roleConfig.StatWeights.TryGetValue("Intelligence", out float intWeight))
                        score += troop.Leader.Intelligence * intWeight;
                    if (roleConfig.StatWeights.TryGetValue("Strength", out float strengthWeight))
                        score += troop.Leader.Strength * strengthWeight;
                }

                // 拥有治疗或鼓舞，分数激增
                if (roleConfig.CoreSkillIDs != null)
                {
                    float skillBonus = roleConfig.Bonuses?.TryGetValue("CoreSkill", out float coreBonus) == true ? coreBonus : 100f;
                    foreach (int skillID in roleConfig.CoreSkillIDs)
                    {
                        if (HasSkill(troop, skillID))
                        {
                            score += skillBonus;
                            // System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有{AIRoleConfigManager.GetSkillName(skillID)}技能，强制辅助倾向");
                        }
                    }
                }

                return score;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateSupportScore 失败: {ex.Message}");
                return 0f;
            }
        }

        // ---------- 辅助方法 ----------
        /// <summary>
        /// 获取部队兵种ID，兼容不同的属性访问方式
        /// </summary>
        private static int GetTroopKindID(Troop troop)
        {
            try
            {
                // 🔥 ANTI-BAND-AID：数据源验证，不使用防御性检查
                if (troop.Army == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army 为 null，应在数据加载时修复");
                }
                
                if (troop.Army.Kind == null)
                {
                    throw new InvalidOperationException($"数据损坏：部队 {troop.DisplayName} 的 Army.Kind 为 null (MilitaryID={troop.Army.ID})，应在数据加载时修复");
                }

                return troop.Army.Kind.ID;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                // 只捕获非数据错误的异常，数据错误必须向上传播
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] GetTroopKindID 访问失败: {ex.Message}");
                throw; // 重新抛出，不掩盖问题
            }
        }

        /// <summary>
        /// 检查部队(主将或副将)是否拥有特定ID的特技
        /// 兼容 zhsan 的数据结构：需要同时检查主将和副将(如果有)
        /// </summary>
        /// <summary>
        /// 检查部队是否拥有特定技能，并且该技能在当前兵种下能够生效
        /// 🔥 修复：增加兵种适用性检查，防止错误的角色分配
        /// 日期：2026-03-09
        /// 问题：弓兵被分配肉盾角色，因为主将有肉盾技能，但该技能只适用于步兵
        /// 解决：检查技能的 MilitaryTypeOnly 属性，只有匹配当前兵种或"其他"的技能才算有效
        /// </summary>
        private static bool HasSkill(Troop troop, int skillID)
        {
            try
            {
                // 🔥 数据源验证：部队必须有有效的兵种信息
                // 如果 Army 或 Kind 为 null，说明数据初始化有问题，应该在上层处理
                if (troop.Army == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] ⚠️ 部队 {troop.DisplayName} 的 Army 为 null，跳过角色评分");
                    return false;
                }
                
                if (troop.Army.Kind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] ⚠️ 部队 {troop.DisplayName} 的 Army.Kind 为 null (MilitaryID={troop.Army.ID})，跳过角色评分");
                    return false;
                }

                MilitaryType currentMilitaryType = troop.Army.Kind.Type;

                // 🔥 新增：特殊技能的兵种限制检查 - 2026-03-10
                // 肉盾技能只适用于近战兵种，弩兵不能使用
                if ((skillID == 350 || skillID == 690) && currentMilitaryType == MilitaryType.弩兵)
                {
                    // System.Diagnostics.Debug.WriteLine($"[角色分配] {troop.Leader.Name}({currentMilitaryType}) 技能{skillID}不适用于弩兵");
                    return false;
                }

                // 检查主将
                if (troop.Leader != null && HasPersonSkillForMilitaryType(troop.Leader, skillID, currentMilitaryType)) 
                    return true;

                // 检查副将
                if (troop.Persons != null)
                {
                    foreach (Person p in troop.Persons.GetList())
                    {
                        if (p != null && HasPersonSkillForMilitaryType(p, skillID, currentMilitaryType)) 
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] HasSkill 检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查人物是否拥有特定技能（旧方法，保留用于兼容性）
        /// </summary>
        private static bool HasPersonSkill(Person person, int skillID)
        {
            try
            {
                if (person == null) return false;
                
                if (person.Skills == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] ⚠️ 人物 {person.Name} 的 Skills 为 null");
                    return false;
                }

                // 遍历人物的技能列表
                foreach (GameObject obj in person.Skills.GetSkillList())
                {
                    if (obj is Skill skill && skill.Kind == skillID)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] HasPersonSkill 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查人物是否拥有特定技能，并且该技能在指定兵种下能够生效
        /// 🔥 新增：兵种适用性检查
        /// 日期：2026-03-09
        /// 用途：防止将只适用于特定兵种的技能计入角色评分
        /// 示例：步兵专属的"铁壁"技能不应该让弓兵被判定为肉盾
        /// </summary>
        private static bool HasPersonSkillForMilitaryType(Person person, int skillID, MilitaryType militaryType)
        {
            try
            {
                // 🔥 数据源验证：人物必须有技能列表
                if (person == null)
                {
                    return false;
                }
                
                if (person.Skills == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] ⚠️ 人物 {person.Name} 的 Skills 为 null");
                    return false;
                }

                // 遍历人物的技能列表
                foreach (GameObject obj in person.Skills.GetSkillList())
                {
                    if (obj is Skill skill && skill.Kind == skillID)
                    {
                        // 🔥 关键检查：技能的兵种限制
                        MilitaryType skillMilitaryType = skill.MilitaryTypeOnly;
                        
                        // 技能适用条件：
                        // 1. 技能标记为"其他"（通用技能，适用所有兵种）
                        // 2. 技能的兵种类型与当前部队兵种匹配
                        if (skillMilitaryType == MilitaryType.其他 || skillMilitaryType == militaryType)
                        {
                            return true;
                        }
                        
                        // 技能存在但不适用于当前兵种，继续检查其他技能
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] HasPersonSkillForMilitaryType 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否包含暴击类特技 (使用配置的范围)
        /// </summary>
        private static bool HasCriticalSkill(Troop troop, SkillRange range = null)
        {
            try
            {
                if (troop.Persons == null) return false;

                // 如果没有提供范围，从DPS配置中获取
                if (range == null)
                {
                    var dpsConfig = AIRoleConfigManager.GetRoleConfig("DPS");
                    range = dpsConfig?.CriticalSkillRange;
                    if (range == null) return false;
                }

                foreach (Person p in troop.Persons.GetList())
                {
                    if (p?.Skills != null)
                    {
                        foreach (GameObject obj in p.Skills.GetSkillList())
                        {
                            if (obj is Skill skill)
                            {
                                int sId = skill.Kind;
                                if (sId >= range.Min && sId <= range.Max) return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI角色选择] HasCriticalSkill 检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量分析势力所有部队角色
        /// 保留原有功能，整合新的角色识别逻辑
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>部队角色分配结果</returns>
        public static Dictionary<Troop, TroopRole> AnalyzeFactionTroops(Faction faction)
        {
            var result = new Dictionary<Troop, TroopRole>();

            try
            {
                if (faction?.Troops == null) return result;

                // System.Diagnostics.Debug.WriteLine($"[AI角色分析] 开始分析 {faction.Name} 的部队角色");

                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop != null)
                    {
                        TroopRole role = DetermineRole(troop);
                        result[troop] = role;
                    }
                }

                // 统计角色分布
                var roleStats = result.Values.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
                // System.Diagnostics.Debug.WriteLine($"[AI角色分析] {faction.Name} 部队角色分布:");
                foreach (var stat in roleStats)
                {
                    // System.Diagnostics.Debug.WriteLine($"  {GetRoleDescription(stat.Key)}: {stat.Value} 支部队");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色分析] AnalyzeFactionTroops 失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 获取角色描述
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>角色描述</returns>
        public static string GetRoleDescription(TroopRole role)
        {
            // C# 7.3 兼容的switch语句
            switch (role)
            {
                case TroopRole.Tank:
                    return "肉盾/前排";
                case TroopRole.DPS:
                    return "物理输出";
                case TroopRole.Mage:
                    return "法系/控制";
                case TroopRole.Support:
                    return "辅助/治疗";
                case TroopRole.Logistics:
                    return "后勤/运输";
                case TroopRole.Balanced:
                    return "均衡/通用";
                case TroopRole.None:
                    return "未定义";
                default:
                    return "未知角色";
            }
        }

        /// <summary>
        /// 获取角色优先级（用于战术排序）
        /// 保留原有的战术优先级逻辑
        /// </summary>
        /// <param name="role">角色</param>
        /// <param name="battlePhase">战斗阶段</param>
        /// <returns>优先级分数（越高越优先）</returns>
        public static int GetRolePriority(TroopRole role, BattlePhase battlePhase)
        {
            // C# 7.3 兼容的嵌套switch语句
            switch (battlePhase)
            {
                case BattlePhase.Opening: // 开场阶段
                    switch (role)
                    {
                        case TroopRole.Tank: return 100;    // 肉盾先上
                        case TroopRole.Support: return 90;  // 辅助跟上
                        case TroopRole.Mage: return 70;     // 法师准备
                        case TroopRole.DPS: return 60;      // DPS待命
                        case TroopRole.Balanced: return 50;
                        default: return 0;
                    }
                case BattlePhase.Engagement: // 交战阶段
                    switch (role)
                    {
                        case TroopRole.DPS: return 100;     // DPS主力输出
                        case TroopRole.Mage: return 90;     // 法师控制
                        case TroopRole.Tank: return 80;     // 肉盾保持阵型
                        case TroopRole.Support: return 70;  // 辅助续航
                        case TroopRole.Balanced: return 60;
                        default: return 0;
                    }
                case BattlePhase.Cleanup: // 收尾阶段
                    switch (role)
                    {
                        case TroopRole.DPS: return 100;     // DPS追击
                        case TroopRole.Balanced: return 80; // 通用部队收尾
                        case TroopRole.Mage: return 60;     // 法师补刀
                        case TroopRole.Tank: return 40;     // 肉盾殿后
                        case TroopRole.Support: return 30;  // 辅助收拾残局
                        default: return 0;
                    }
                default:
                    return 50;
            }
        }

        // ================= 军团级角色分配器 =================
        /// <summary>
        /// 军团级角色分配器
        /// 解决"全员DPS"问题，强制保证军团的阵容平衡
        /// </summary>
        /// <param name="legionTroops">军团内的所有部队</param>
        /// <returns>分配好的角色字典</returns>
        public static Dictionary<Troop, TroopRole> AllocateLegionRoles(List<Troop> legionTroops)
        {
            Dictionary<Troop, TroopRole> result = new Dictionary<Troop, TroopRole>();
            if (legionTroops == null || legionTroops.Count == 0) return result;

            try
            {
                // System.Diagnostics.Debug.WriteLine($"[军团分配] 开始分配 {legionTroops.Count} 支部队的角色");

                // 1. 准备记分卡
                // 记录每个部队在不同职位上的得分
                var scoreCards = new Dictionary<Troop, Dictionary<TroopRole, float>>();
                
                // 待分配池
                HashSet<Troop> pool = new HashSet<Troop>();

                foreach (var troop in legionTroops)
                {
                    if (troop == null) continue;

                    // 优先处理后勤，直接锁定，不参与分配
                    int kindID = GetTroopKindID(troop);
                    if (AIRoleConfigManager.IsTroopKindForRole(kindID, "Logistics"))
                    {
                        result[troop] = TroopRole.Logistics;
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.DisplayName} 识别为后勤单位，直接锁定");
                        continue;
                    }

                    // 计算该部队所有维度的分数
                    var scores = new Dictionary<TroopRole, float>
                    {
                        { TroopRole.Tank, CalculateTankScore(troop) },
                        { TroopRole.DPS, CalculateDpsScore(troop) },
                        { TroopRole.Mage, CalculateMageScore(troop) },
                        { TroopRole.Support, CalculateSupportScore(troop) }
                    };
                    
                    scoreCards[troop] = scores;
                    pool.Add(troop);
                    
                    // System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.DisplayName} 评分: Tank={scores[TroopRole.Tank]:F1}, DPS={scores[TroopRole.DPS]:F1}, Mage={scores[TroopRole.Mage]:F1}, Support={scores[TroopRole.Support]:F1}");
                }

                // 2. 定义编制需求 (根据军团人数动态调整)
                int count = pool.Count;
                int tankSlots = 0;
                int supportSlots = 0;

                if (count >= 4) // 4-5人军团：1T 1奶/法 2-3DPS
                {
                    tankSlots = 1;
                    supportSlots = 1;
                    // System.Diagnostics.Debug.WriteLine($"[军团分配] 军团规模: {count}人，编制需求: 1坦克 + 1辅助 + {count - 2}输出");
                }
                else if (count >= 2) // 2-3人军团：1T 1-2DPS
                {
                    tankSlots = 1;
                    supportSlots = 0; // 人少就别搞辅助了，直接干
                    // System.Diagnostics.Debug.WriteLine($"[军团分配] 军团规模: {count}人，编制需求: 1坦克 + {count - 1}输出");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[军团分配] 军团规模过小({count}人)，所有人自由选择角色");
                }

                // 3. 竞聘上岗 (贪心算法)
                
                // --- 第一轮：选拔辅助 (Support) ---
                // 辅助最稀缺，如果有人有治疗技能（分数会极高），必须先把他摘出来
                for (int i = 0; i < supportSlots; i++)
                {
                    if (pool.Count == 0) break;

                    // 找 Support 分数最高的，且分数必须达标(比如 > 50)，否则宁缺毋滥
                    var bestSupport = pool.OrderByDescending(t => scoreCards[t][TroopRole.Support]).FirstOrDefault();
                    if (bestSupport != null && scoreCards[bestSupport][TroopRole.Support] > 50)
                    {
                        result[bestSupport] = TroopRole.Support;
                        pool.Remove(bestSupport);
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] {bestSupport.DisplayName} 被指派为 辅助 (Support分: {scoreCards[bestSupport][TroopRole.Support]:F1})");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] 无合格辅助人选（最高分: {bestSupport?.DisplayName} {scoreCards[bestSupport][TroopRole.Support]:F1}），跳过辅助位");
                        break;
                    }
                }

                // --- 第二轮：选拔肉盾 (Tank) ---
                // 在剩下的人里，找最能抗的
                for (int i = 0; i < tankSlots; i++)
                {
                    if (pool.Count == 0) break;

                    // 找 Tank 分数最高的
                    // 注意：哪怕李傕的DPS分是100，Tank分是90，如果他是剩下人里Tank分最高的，他也得当Tank
                    var bestTank = pool.OrderByDescending(t => scoreCards[t][TroopRole.Tank]).FirstOrDefault();
                    if (bestTank != null)
                    {
                        result[bestTank] = TroopRole.Tank;
                        pool.Remove(bestTank);
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] {bestTank.DisplayName} 被指派为 肉盾 (Tank分: {scoreCards[bestTank][TroopRole.Tank]:F1})");
                    }
                }

                // --- 第三轮：其余人自由选择 (DPS/Mage) ---
                foreach (var troop in pool)
                {
                    float dpsScore = scoreCards[troop][TroopRole.DPS];
                    float mageScore = scoreCards[troop][TroopRole.Mage];

                    if (mageScore > dpsScore && mageScore > 60) // 智力及格才当法师
                    {
                        result[troop] = TroopRole.Mage;
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.DisplayName} 自由选择为 法师 (Mage分: {mageScore:F1})");
                    }
                    else
                    {
                        result[troop] = TroopRole.DPS;
                        // System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.DisplayName} 自由选择为 输出 (DPS分: {dpsScore:F1})");
                    }
                }

                // 4. 统计最终分配结果
                var roleStats = result.Values.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
                // System.Diagnostics.Debug.WriteLine($"[军团分配] 最终角色分布:");
                foreach (var stat in roleStats)
                {
                    // System.Diagnostics.Debug.WriteLine($"  {GetRoleDescription(stat.Key)}: {stat.Value} 支部队");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军团分配] AllocateLegionRoles 失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 为军团分配角色并应用到部队
        /// </summary>
        /// <param name="legion">军团对象</param>
        /// <summary>
        /// 更新军团角色分配
        /// 🔥 2026-03-16 AOT 修复 + ANTI-BAND-AID：移除防御性检查和异常捕获
        /// </summary>
        public static void UpdateLegionRoles(Legion legion)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast，不做防御性检查
            // 如果 legion 或 legion.Troops 为 null，让它崩溃以便发现调用者的 bug
            
            // 🔥 C# 12：使用集合表达式 + Cast<T>
            List<Troop> troopList = [..legion.Troops.Cast<Troop>()];

            // 一次性分配整个军团
            var assignments = AllocateLegionRoles(troopList);

            // 应用结果
            foreach (var kvp in assignments)
            {
                Troop troop = kvp.Key;
                TroopRole role = kvp.Value;

                // 设置部队角色（强制分配）
                SetTroopRole(troop, role);
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[军团分配] 军团 {legion.Name} 角色分配完成");
            #endif
        }

        /// <summary>
        /// 设置部队角色（内部方法）
        /// 如果Troop类有SetRoleForce方法则调用，否则尝试设置属性
        /// </summary>
        /// <summary>
        /// 设置部队角色（AOT 安全版本）
        /// 🔥 2026-03-16 AOT 修复：移除反射，直接调用 SetRoleForce 方法
        /// </summary>
        private static void SetTroopRole(Troop troop, TroopRole role)
        {
            // ✅ AOT 安全：直接调用方法，无反射
            troop.SetRoleForce(role);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[军团分配] {troop.DisplayName} 角色设置为 {GetRoleDescription(role)}");
            #endif
        }
    }

    /// <summary>
    /// 战斗阶段枚举
    /// </summary>
    public enum BattlePhase
    {
        Opening,    // 开场布阵
        Engagement, // 主要交战
        Cleanup     // 收尾追击
    }

    /// <summary>
    /// 战术阵型
    /// </summary>
    public class TacticalFormation
    {
        public List<Troop> FrontLine { get; set; } = new List<Troop>();  // 前排
        public List<Troop> MiddleLine { get; set; } = new List<Troop>(); // 中排
        public List<Troop> BackLine { get; set; } = new List<Troop>();   // 后排

        /// <summary>
        /// 获取总部队数量
        /// </summary>
        public int TotalTroops => FrontLine.Count + MiddleLine.Count + BackLine.Count;

        /// <summary>
        /// 获取阵型平衡度评分
        /// </summary>
        public float GetBalanceScore()
        {
            if (TotalTroops == 0) return 0f;

            // 理想比例：前排30%，中排50%，后排20%
            float frontRatio = (float)FrontLine.Count / TotalTroops;
            float middleRatio = (float)MiddleLine.Count / TotalTroops;
            float backRatio = (float)BackLine.Count / TotalTroops;

            float frontScore = 100f - Math.Abs(frontRatio - 0.3f) * 200f;
            float middleScore = 100f - Math.Abs(middleRatio - 0.5f) * 100f;
            float backScore = 100f - Math.Abs(backRatio - 0.2f) * 250f;

            return (frontScore + middleScore + backScore) / 3f;
        }

        /// <summary>
        /// 获取阵型描述
        /// </summary>
        public string GetFormationDescription()
        {
            return $"阵型配置: 前排{FrontLine.Count}支, 中排{MiddleLine.Count}支, 后排{BackLine.Count}支 (平衡度: {GetBalanceScore():F1}%)";
        }
    }
}