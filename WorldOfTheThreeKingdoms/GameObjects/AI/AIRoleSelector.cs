using System;
using System.Collections.Generic;
using GameObjects; // 假设 Troop 和 Person 类在此命名空间
using GameObjects.TroopDetail; // 假设特技/兵种定义在此

namespace GameObjects.AI
{
    /// <summary>
    /// AI 部队战术角色枚举
    /// </summary>
    public enum AIRole
    {
        None = 0,
        Tank,       // 肉盾：卡位、吸收伤害
        DPS,        // 输出：物理核心输出
        Mage,       // 法师：控制、策略输出
        Support,    // 辅助：治疗、Buff
        Logistics   // 后勤：运输、建造 (非战斗)
    }

    /// <summary>
    /// 角色选择器 - 负责根据兵种、属性和特技分配战术角色
    /// </summary>
    public static class AIRoleSelector
    {
        // ================= 配置区域 (ID 表) =================
        // 提示：如果 CommonData.json 变动，请修改此处的 ID

        // 兵种 ID 配置
        private static readonly HashSet<int> TankTroopIDs = new HashSet<int> { 11, 51, 150 }; // 戟兵, 盾兵, 象兵
        private static readonly HashSet<int> DpsTroopIDs = new HashSet<int> { 2, 15, 400 };   // 骑兵, 弩兵, 虎豹骑
        private static readonly HashSet<int> LogisticsTroopIDs = new HashSet<int> { 29, 601, 621 }; // 运输队, 建造队

        // 核心特技 ID 配置
        private const int Skill_JianZhen = 350;   // 坚阵 (受暴击伤害减少)
        private const int Skill_TieBi = 690;      // 铁壁 (友军防御光环)
        private const int Skill_GuanChuan = 383;  // 贯穿
        private const int Skill_GongXin = 390;    // 攻心
        private const int Skill_RaoLuan = 391;    // 扰乱
        private const int Skill_ShenSuan = 570;   // 神算
        private const int Skill_YiZhi = 399;      // 医治
        private const int Skill_GuWu = 397;       // 鼓舞

        // 暴击类特技范围 (ID 400 - 450)
        private const int CritSkill_Min = 400;
        private const int CritSkill_Max = 450;

        // ================= 评分权重参数 =================
        private const float Weight_Stat = 1.0f;         // 属性分系数
        private const float Bonus_TroopMatch = 50.0f;   // 兵种契合加分
        private const float Bonus_CoreSkill = 30.0f;    // 核心特技加分
        private const float Bonus_SupportSkill = 100.0f;// 辅助特技极大加分

        /// <summary>
        /// 计算并返回部队的最佳战术角色
        /// </summary>
        /// <param name="troop">目标部队</param>
        /// <returns>计算出的角色</returns>
        public static AIRole GetBestRole(Troop troop)
        {
            if (troop == null || troop.Leader == null) return AIRole.None;

            // 1. 优先剔除后勤单位
            // 运输队和建造队无战斗能力，必须强制锁定
            if (troop.Army != null && LogisticsTroopIDs.Contains(troop.Army.KindID))
            {
                return AIRole.Logistics;
            }

            // 2. 初始化各角色评分
            float scoreTank = CalculateTankScore(troop);
            float scoreDps = CalculateDpsScore(troop);
            float scoreMage = CalculateMageScore(troop);
            float scoreSupport = CalculateSupportScore(troop);

            // 3. 比较得出最高分 (C# 7.3 基础写法)
            AIRole bestRole = AIRole.Tank;
            float maxScore = scoreTank;

            if (scoreDps > maxScore)
            {
                maxScore = scoreDps;
                bestRole = AIRole.DPS;
            }

            // 法师的优先级通常高于纯物理，如果分数接近优先选法师
            if (scoreMage > maxScore)
            {
                maxScore = scoreMage;
                bestRole = AIRole.Mage;
            }

            // 辅助特技非常稀缺，一旦判定为辅助，通常直接锁定
            if (scoreSupport > maxScore)
            {
                maxScore = scoreSupport;
                bestRole = AIRole.Support;
            }

            return bestRole;
        }

        // ---------- 评分算法细节 ----------

        private static float CalculateTankScore(Troop troop)
        {
            // 基础分：统率 (使用Command属性)
            float score = troop.Leader.Command * Weight_Stat;

            // 兵种加成
            if (troop.Army != null && TankTroopIDs.Contains(troop.Army.KindID))
            {
                score += Bonus_TroopMatch;
            }

            // 特技加成
            if (HasSkill(troop, Skill_JianZhen)) score += Bonus_CoreSkill;
            if (HasSkill(troop, Skill_TieBi)) score += Bonus_CoreSkill;

            return score;
        }

        private static float CalculateDpsScore(Troop troop)
        {
            // 基础分：武力
            float score = troop.Leader.Strength * Weight_Stat;

            // 兵种加成
            if (troop.Army != null && DpsTroopIDs.Contains(troop.Army.KindID))
            {
                score += Bonus_TroopMatch;
            }

            // 特技加成：贯穿
            if (HasSkill(troop, Skill_GuanChuan)) score += Bonus_CoreSkill;

            // 特技加成：暴击类
            if (HasCriticalSkill(troop)) score += Bonus_CoreSkill;

            return score;
        }

        private static float CalculateMageScore(Troop troop)
        {
            // 门槛：智力过低直接排除，防止"弱智"法师送策略点
            if (troop.Leader.Intelligence < 70) return 0f;

            // 基础分：智力
            float score = troop.Leader.Intelligence * Weight_Stat;

            // 法师主要依赖特技，兵种影响较小 (除非有井阑等特殊兵种，此处暂略)

            // 特技加成
            if (HasSkill(troop, Skill_GongXin)) score += Bonus_CoreSkill;
            if (HasSkill(troop, Skill_RaoLuan)) score += Bonus_CoreSkill;

            // 神算价值极高，额外加权
            if (HasSkill(troop, Skill_ShenSuan)) score += Bonus_CoreSkill * 1.5f;

            return score;
        }

        private static float CalculateSupportScore(Troop troop)
        {
            float score = 0f;

            // 辅助主要看是否有技能，属性次之 (智力/统率略微加分)
            score += (troop.Leader.Command + troop.Leader.Intelligence) * 0.2f;

            // 拥有治疗或鼓舞，分数激增
            if (HasSkill(troop, Skill_YiZhi)) score += Bonus_SupportSkill;
            if (HasSkill(troop, Skill_GuWu)) score += Bonus_SupportSkill;

            return score;
        }

        // ---------- 辅助方法 ----------

        /// <summary>
        /// 检查部队(主将或副将)是否拥有特定ID的特技
        /// </summary>
        private static bool HasSkill(Troop troop, int skillID)
        {
            // 检查主将
            if (troop.Leader != null && troop.Leader.Skills != null && troop.Leader.HasSkillforGroup(skillID)) 
                return true;

            // 检查副将 (兼容 Persons 列表)
            if (troop.Persons != null)
            {
                foreach (Person p in troop.Persons)
                {
                    if (p != troop.Leader && p.Skills != null && p.HasSkillforGroup(skillID)) 
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否包含暴击类特技 (ID 400-450)
        /// </summary>
        private static bool HasCriticalSkill(Troop troop)
        {
            if (troop.Persons == null) return false;

            foreach (Person p in troop.Persons)
            {
                if (p.Skills != null)
                {
                    for (int sId = CritSkill_Min; sId <= CritSkill_Max; sId++)
                    {
                        if (p.HasSkillforGroup(sId)) return true;
                    }
                }
            }

            return false;
        }
    }
}