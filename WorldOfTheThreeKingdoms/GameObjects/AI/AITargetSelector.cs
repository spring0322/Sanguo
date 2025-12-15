using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameObjects.AI.Helper;
using GameManager;

namespace GameObjects.AI
{
    /// <summary>
    /// AI目标选择器 (Phase 3)
    /// 负责计算最佳攻击目标，实现"集火"与"收割"策略
    /// 集成现有游戏系统，提供智能的目标优先级计算
    /// </summary>
    public static class AITargetSelector
    {
        // ================= 仇恨值参数 =================
        private const float Score_Base = 100f;

        // 状态加成 (协同核心)
        private const float Bonus_Chaos = 2000f;     // 趁敌人混乱要他命
        private const float Bonus_Fire = 500f;       // 痛打着火敌军
        private const float Bonus_Pinned = 300f;     // 敌人被我方Tank贴身

        // 战术加成
        private const float Bonus_KillSecure = 5000f; // 能击杀，优先级最高
        private const float Bonus_SoftTarget = 200f;  // 优先打脆皮 (高智低防)
        private const float Bonus_HighValue = 400f;   // 高价值目标 (将领、精英)

        // 距离惩罚
        private const float Penalty_Distance = 10f;   // 每多走一格扣的分
        private const float Penalty_OutOfRange = 1000f; // 超出攻击范围的惩罚

        // 血量阈值
        private const float LowHealthThreshold = 0.3f; // 30%血量以下视为残血

        /// <summary>
        /// 获取最佳攻击目标
        /// </summary>
        /// <param name="me">我方部队</param>
        /// <param name="enemies">视野内所有敌军</param>
        /// <param name="myAllies">我方所有部队 (用于判断配合)</param>
        /// <returns>评分最高的敌军，若无则返回 null</returns>
        public static Troop GetBestTarget(Troop me, List<Troop> enemies, List<Troop> myAllies)
        {
            try
            {
                if (enemies == null || enemies.Count == 0)
                {
                    Console.WriteLine($"[AITargetSelector] 部队 {me.ID} 没有可攻击的敌军");
                    return null;
                }

                Troop bestTarget = null;
                float maxScore = -9999f;

                Console.WriteLine($"[AITargetSelector] 为部队 {me.ID} 评估 {enemies.Count} 个敌军目标");

                foreach (var enemy in enemies)
                {
                    // 1. 基础过滤：必须活着且可攻击
                    if (enemy == null || enemy.Destroyed)
                        continue;

                    // 2. 计算该目标的得分
                    float score = CalculateTargetScore(me, enemy, myAllies);

                    Console.WriteLine($"[AITargetSelector] 敌军 {enemy.ID} 评分: {score:F1}");

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestTarget = enemy;
                    }
                }

                if (bestTarget != null)
                {
                    Console.WriteLine($"[AITargetSelector] 部队 {me.ID} 选择目标: 敌军 {bestTarget.ID} (评分: {maxScore:F1})");
                }

                return bestTarget;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 选择目标时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算目标的攻击优先级评分
        /// </summary>
        /// <param name="me">攻击者</param>
        /// <param name="target">目标</param>
        /// <param name="allies">友军列表</param>
        /// <returns>目标评分</returns>
        private static float CalculateTargetScore(Troop me, Troop target, List<Troop> allies)
        {
            try
            {
                float score = Score_Base;

                // --- A. 距离因素 ---
                int dist = MapNavigationHelper.GetManhattanDistance(me.Position, target.Position);
                int attackRange = GetTroopAttackRange(me);

                // 如果超出攻击范围，大幅扣分
                if (dist > attackRange)
                {
                    score -= Penalty_OutOfRange;
                }

                // 距离越远，优先级越低
                score -= dist * Penalty_Distance;

                // --- B. 击杀计算 (Kill Secure) ---
                // 获取预估伤害
                int estimatedDamage = CalculateEstimatedDamage(me, target);
                int targetHealth = GetTroopCurrentHealth(target);

                if (estimatedDamage >= targetHealth)
                {
                    score += Bonus_KillSecure;
                    Console.WriteLine($"[AITargetSelector] 敌军 {target.ID} 可被击杀，获得击杀加成");
                }

                // --- C. 状态协同 (核心逻辑) ---
                if (IsInChaos(target))
                {
                    score += Bonus_Chaos;
                    Console.WriteLine($"[AITargetSelector] 敌军 {target.ID} 处于混乱状态，获得协同加成");
                }

                // 检查着火状态
                if (IsOnFire(target))
                {
                    score += Bonus_Fire;
                    Console.WriteLine($"[AITargetSelector] 敌军 {target.ID} 着火，获得火焰加成");
                }

                // --- D. 战术配合 ---
                // 检查目标是否被我方Tank贴身
                if (IsPinnedByTank(target, allies))
                {
                    score += Bonus_Pinned;
                    Console.WriteLine($"[AITargetSelector] 敌军 {target.ID} 被Tank贴身，获得配合加成");
                }

                // --- E. 目标价值评估 ---
                // 优先攻击脆皮目标
                if (IsSoftTarget(target))
                {
                    score += Bonus_SoftTarget;
                }

                // 优先攻击高价值目标
                if (IsHighValueTarget(target))
                {
                    score += Bonus_HighValue;
                }

                // 优先攻击残血目标
                float healthRatio = (float)targetHealth / GetTroopMaxHealth(target);
                if (healthRatio <= LowHealthThreshold)
                {
                    score += Bonus_SoftTarget * (1f - healthRatio); // 血量越少加成越高
                }

                return score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 计算目标评分时发生错误: {ex.Message}");
                return Score_Base;
            }
        }

        // ---------- 辅助判断逻辑 ----------

        /// <summary>
        /// 检查目标是否处于混乱状态
        /// </summary>
        /// <param name="target">目标部队</param>
        /// <returns>是否混乱</returns>
        private static bool IsInChaos(Troop target)
        {
            try
            {
                // 检查部队状态
                return target.Status == TroopStatus.混乱;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 检查混乱状态时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查目标是否着火
        /// </summary>
        /// <param name="target">目标部队</param>
        /// <returns>是否着火</returns>
        private static bool IsOnFire(Troop target)
        {
            try
            {
                // 简化实现：检查部队是否有火焰相关的状态
                // 实际游戏中可能需要检查具体的火焰状态或影响
                return false; // 暂时返回false，需要根据实际游戏系统调整
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 检查着火状态时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查目标是否被我方Tank贴身 (距离=1)
        /// </summary>
        /// <param name="target">目标部队</param>
        /// <param name="allies">友军列表</param>
        /// <returns>是否被Tank贴身</returns>
        private static bool IsPinnedByTank(Troop target, List<Troop> allies)
        {
            try
            {
                if (allies == null) return false;

                foreach (var ally in allies)
                {
                    if (ally != null && ally.CurrentRole == AIRole.Tank)
                    {
                        int dist = MapNavigationHelper.GetManhattanDistance(ally.Position, target.Position);
                        if (dist <= 1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 检查Tank贴身时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断是否为脆皮目标
        /// </summary>
        /// <param name="target">目标部队</param>
        /// <returns>是否为脆皮</returns>
        private static bool IsSoftTarget(Troop target)
        {
            try
            {
                if (target.Leader == null) return false;

                // 高智力低防御的目标视为脆皮
                return target.Leader.Intelligence > 70 && target.Defence < 50;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 判断脆皮目标时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断是否为高价值目标
        /// </summary>
        /// <param name="target">目标部队</param>
        /// <returns>是否为高价值目标</returns>
        private static bool IsHighValueTarget(Troop target)
        {
            try
            {
                if (target.Leader == null) return false;

                // 高统率或高智力的将领视为高价值目标
                return target.Leader.Command > 80 || target.Leader.Intelligence > 80;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 判断高价值目标时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算预估伤害
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防御者</param>
        /// <returns>预估伤害值</returns>
        private static int CalculateEstimatedDamage(Troop attacker, Troop defender)
        {
            try
            {
                // 简化的伤害计算公式
                // 实际游戏中应该调用游戏的伤害计算系统
                int attackPower = attacker.Offence;
                int defense = defender.Defence;

                // 基础伤害 = 攻击力 - 防御力的一半
                int baseDamage = Math.Max(1, attackPower - defense / 2);

                // 考虑兵种克制等因素
                float multiplier = GetDamageMultiplier(attacker, defender);

                return (int)(baseDamage * multiplier);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 计算预估伤害时出错: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 获取伤害倍数（兵种克制等）
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防御者</param>
        /// <returns>伤害倍数</returns>
        private static float GetDamageMultiplier(Troop attacker, Troop defender)
        {
            try
            {
                // 简化的兵种克制计算
                // 实际游戏中应该查询兵种克制表
                if (attacker.Army?.Kind != null && defender.Army?.Kind != null)
                {
                    int attackerKind = attacker.Army.KindID;
                    int defenderKind = defender.Army.KindID;

                    // 简单的克制关系示例
                    // 骑兵(2) 克制 弓兵(15)
                    if (attackerKind == 2 && defenderKind == 15)
                        return 1.5f;
                    
                    // 戟兵(11) 克制 骑兵(2)
                    if (attackerKind == 11 && defenderKind == 2)
                        return 1.3f;
                }

                return 1.0f;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 计算伤害倍数时出错: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// 获取部队当前血量
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>当前血量</returns>
        private static int GetTroopCurrentHealth(Troop troop)
        {
            try
            {
                // 使用部队数量作为血量
                return troop.Quantity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 获取部队血量时出错: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 获取部队最大血量
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>最大血量</returns>
        private static int GetTroopMaxHealth(Troop troop)
        {
            try
            {
                // 使用部队数量 + 伤兵数量作为最大血量
                return troop.Quantity + troop.InjuryQuantity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 获取部队最大血量时出错: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 获取部队攻击范围
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>攻击范围</returns>
        private static int GetTroopAttackRange(Troop troop)
        {
            try
            {
                return troop.OffenceRadius;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 获取攻击范围时出错: {ex.Message}");
                // 根据角色返回默认值
                switch (troop.CurrentRole)
                {
                    case AIRole.Mage:
                        return 3;
                    case AIRole.DPS:
                        return 2;
                    case AIRole.Tank:
                        return 1;
                    default:
                        return 2;
                }
            }
        }

        /// <summary>
        /// 批量为多个部队选择最佳目标
        /// </summary>
        /// <param name="myTroops">我方部队列表</param>
        /// <param name="enemies">敌军列表</param>
        /// <returns>目标分配结果字典</returns>
        public static Dictionary<Troop, Troop> AssignTargetsToTroops(List<Troop> myTroops, List<Troop> enemies)
        {
            var assignments = new Dictionary<Troop, Troop>();

            try
            {
                if (myTroops == null || enemies == null)
                    return assignments;

                Console.WriteLine($"[AITargetSelector] 开始为 {myTroops.Count} 个部队分配目标");

                foreach (var troop in myTroops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        var target = GetBestTarget(troop, enemies, myTroops);
                        if (target != null)
                        {
                            assignments[troop] = target;
                        }
                    }
                }

                Console.WriteLine($"[AITargetSelector] 目标分配完成，共分配 {assignments.Count} 个目标");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 批量分配目标时发生错误: {ex.Message}");
            }

            return assignments;
        }

        /// <summary>
        /// 获取集火目标（多个部队攻击同一目标）
        /// </summary>
        /// <param name="myTroops">我方部队列表</param>
        /// <param name="enemies">敌军列表</param>
        /// <returns>最适合集火的目标</returns>
        public static Troop GetFocusFireTarget(List<Troop> myTroops, List<Troop> enemies)
        {
            try
            {
                if (myTroops == null || enemies == null || enemies.Count == 0)
                    return null;

                // 计算每个敌军被多少部队攻击的总伤害
                var targetDamageMap = new Dictionary<Troop, float>();

                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.Destroyed) continue;

                    float totalDamage = 0f;
                    int attackerCount = 0;

                    foreach (var myTroop in myTroops)
                    {
                        if (myTroop == null || myTroop.Destroyed) continue;

                        // 检查是否在攻击范围内
                        int distance = MapNavigationHelper.GetManhattanDistance(myTroop.Position, enemy.Position);
                        int range = GetTroopAttackRange(myTroop);

                        if (distance <= range)
                        {
                            totalDamage += CalculateEstimatedDamage(myTroop, enemy);
                            attackerCount++;
                        }
                    }

                    // 如果有多个部队可以攻击这个目标，计算集火价值
                    if (attackerCount >= 2)
                    {
                        // 集火价值 = 总伤害 * 攻击者数量加成
                        float focusFireValue = totalDamage * (1f + attackerCount * 0.2f);
                        targetDamageMap[enemy] = focusFireValue;
                    }
                }

                // 选择集火价值最高的目标
                if (targetDamageMap.Count > 0)
                {
                    var bestTarget = targetDamageMap.OrderByDescending(kvp => kvp.Value).First().Key;
                    Console.WriteLine($"[AITargetSelector] 选择集火目标: 敌军 {bestTarget.ID}");
                    return bestTarget;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITargetSelector] 选择集火目标时发生错误: {ex.Message}");
                return null;
            }
        }
    }
}