using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects; // 假设 Troop 和 Person 类在此命名空间
using GameManager;

namespace GameGlobal
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
    /// 角色选择器 - 负责根据兵种、属性和特技分配战术角色
    /// 环境：C# 7.3 / MonoGame 完全兼容
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
                if (LogisticsTroopIDs.Contains(kindID))
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 识别为后勤单位(ID:{kindID})");
                    return TroopRole.Logistics;
                }

                Person leader = troop.Leader;

                // 2. 初始化各角色评分
                float scoreTank = CalculateTankScore(troop);
                float scoreDps = CalculateDpsScore(troop);
                float scoreMage = CalculateMageScore(troop);
                float scoreSupport = CalculateSupportScore(troop);

                System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 评分: Tank={scoreTank:F1}, DPS={scoreDps:F1}, Mage={scoreMage:F1}, Support={scoreSupport:F1}");

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
                if (maxScore < 80)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 最高评分过低({maxScore:F1})，分配为均衡角色");
                    return TroopRole.Balanced;
                }

                System.Diagnostics.Debug.WriteLine($"[AI角色选择] {leader.Name} 最终角色: {bestRole} (评分: {maxScore:F1})");
                return bestRole;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] DetermineRole 失败: {ex.Message}");
                return TroopRole.Balanced;
            }
        }

        // ---------- 评分算法细节 ----------
        /// <summary>
        /// 计算肉盾评分
        /// </summary>
        private static float CalculateTankScore(Troop troop)
        {
            try
            {
                // 基础分：统率
                float score = troop.Leader.Command * Weight_Stat;

                // 兵种加成
                int kindID = GetTroopKindID(troop);
                if (TankTroopIDs.Contains(kindID))
                {
                    score += Bonus_TroopMatch;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 肉盾兵种匹配加分");
                }

                // 特技加成
                if (HasSkill(troop, Skill_JianZhen)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有坚阵技能");
                }
                if (HasSkill(troop, Skill_TieBi)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有铁壁技能");
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateTankScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算输出评分
        /// </summary>
        private static float CalculateDpsScore(Troop troop)
        {
            try
            {
                // 基础分：武力
                float score = troop.Leader.Strength * Weight_Stat;

                // 兵种加成
                int kindID = GetTroopKindID(troop);
                if (DpsTroopIDs.Contains(kindID))
                {
                    score += Bonus_TroopMatch;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 输出兵种匹配加分");
                }

                // 特技加成：贯穿
                if (HasSkill(troop, Skill_GuanChuan)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有贯穿技能");
                }
                
                // 特技加成：暴击类
                if (HasCriticalSkill(troop)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有暴击技能");
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateDpsScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算法师评分
        /// </summary>
        private static float CalculateMageScore(Troop troop)
        {
            try
            {
                // 门槛：智力过低直接排除，防止"弱智"法师送策略点
                if (troop.Leader.Intelligence < 70) 
                {
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 智力不足({troop.Leader.Intelligence})，不适合做法师");
                    return 0f;
                }

                // 基础分：智力
                float score = troop.Leader.Intelligence * Weight_Stat;

                // 法师主要依赖特技，兵种影响较小 (除非有井阑等特殊兵种，此处暂略)

                // 特技加成
                if (HasSkill(troop, Skill_GongXin)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有攻心技能");
                }
                if (HasSkill(troop, Skill_RaoLuan)) 
                {
                    score += Bonus_CoreSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有扰乱技能");
                }
                // 神算价值极高，额外加权
                if (HasSkill(troop, Skill_ShenSuan)) 
                {
                    score += Bonus_CoreSkill * 1.5f;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有神算技能");
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateMageScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算辅助评分
        /// </summary>
        private static float CalculateSupportScore(Troop troop)
        {
            try
            {
                float score = 0f;

                // 辅助主要看是否有技能，属性次之 (智力/统率略微加分)
                score += (troop.Leader.Command + troop.Leader.Intelligence) * 0.2f;

                // 拥有治疗或鼓舞，分数激增
                if (HasSkill(troop, Skill_YiZhi)) 
                {
                    score += Bonus_SupportSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有医治技能，强制辅助倾向");
                }
                if (HasSkill(troop, Skill_GuWu)) 
                {
                    score += Bonus_SupportSkill;
                    System.Diagnostics.Debug.WriteLine($"[AI角色选择] {troop.Leader.Name} 拥有鼓舞技能");
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] CalculateSupportScore 失败: {ex.Message}");
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
                // 尝试不同的属性访问方式
                if (troop.Army?.Kind != null)
                {
                    return troop.Army.Kind.ID;
                }
                // 如果有其他访问方式，可以在这里添加
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 检查部队(主将或副将)是否拥有特定ID的特技
        /// 兼容 zhsan 的数据结构：需要同时检查主将和副将(如果有)
        /// </summary>
        private static bool HasSkill(Troop troop, int skillID)
        {
            try
            {
                // 检查主将
                if (troop.Leader != null && HasPersonSkill(troop.Leader, skillID)) 
                    return true;

                // 检查副将 (假设 Persons 列表包含主副将)
                if (troop.Persons != null)
                {
                    foreach (Person p in troop.Persons.GetList())
                    {
                        if (p != null && HasPersonSkill(p, skillID)) 
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
        /// 检查人物是否拥有特定技能
        /// </summary>
        private static bool HasPersonSkill(Person person, int skillID)
        {
            try
            {
                if (person?.Skills == null) return false;

                // 遍历人物的技能列表
                foreach (var skill in person.Skills.GetSkillList())
                {
                    if (skill?.Kind != null && skill.Kind.ID == skillID)
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
        /// 检查是否包含暴击类特技 (ID 400-450)
        /// </summary>
        private static bool HasCriticalSkill(Troop troop)
        {
            try
            {
                if (troop.Persons == null) return false;

                foreach (Person p in troop.Persons.GetList())
                {
                    if (p?.Skills != null)
                    {
                        foreach (var skill in p.Skills.GetSkillList())
                        {
                            if (skill?.Kind != null)
                            {
                                int sId = skill.Kind.ID;
                                if (sId >= CritSkill_Min && sId <= CritSkill_Max) return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI角色选择] HasCriticalSkill 检查失败: {ex.Message}");
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

                System.Diagnostics.Debug.WriteLine($"[AI角色分析] 开始分析 {faction.Name} 的部队角色");

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
                System.Diagnostics.Debug.WriteLine($"[AI角色分析] {faction.Name} 部队角色分布:");
                foreach (var stat in roleStats)
                {
                    System.Diagnostics.Debug.WriteLine($"  {GetRoleDescription(stat.Key)}: {stat.Value} 支部队");
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