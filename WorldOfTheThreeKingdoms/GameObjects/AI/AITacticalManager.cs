using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI
{
    /// <summary>
    /// AI战术管理器 - 整合角色选择和位移决策的核心系统
    /// </summary>
    public static class AITacticalManager
    {
        /// <summary>
        /// 为部队执行完整的AI战术决策
        /// </summary>
        /// <param name="troop">当前行动的部队</param>
        /// <param name="enemies">敌军列表</param>
        /// <param name="allies">友军列表</param>
        /// <returns>推荐的移动目标点</returns>
        public static Point ExecuteTacticalDecision(Troop troop, List<Troop> enemies, List<Troop> allies)
        {
            if (troop == null) return Point.Zero;

            try
            {
                // 第一步：确保角色已分配
                EnsureRoleAssigned(troop);

                // 第二步：选择攻击目标
                Troop target = SelectBestTarget(troop, enemies);
                if (target == null)
                {
                    Console.WriteLine($"部队 {troop.ID} 没有找到合适的攻击目标");
                    return troop.Position; // 没有目标，保持原位
                }

                // 第三步：获取可移动位置
                List<Point> reachablePoints = AITacticalPositioner.GetReachablePositions(troop);
                if (reachablePoints.Count == 0)
                {
                    Console.WriteLine($"部队 {troop.ID} 没有可移动的位置");
                    return troop.Position;
                }

                // 第四步：计算最佳位置
                Point bestPosition = AITacticalPositioner.GetBestPosition(troop, target, allies, reachablePoints);

                // 输出决策信息
                LogTacticalDecision(troop, target, bestPosition);

                return bestPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI战术决策出错: {ex.Message}");
                return troop.Position;
            }
        }

        /// <summary>
        /// 确保部队已分配角色
        /// </summary>
        private static void EnsureRoleAssigned(Troop troop)
        {
            if (troop.CurrentRole == TroopRole.None)
            {
                troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
                Console.WriteLine($"为部队 {troop.ID} ({troop.Leader?.Name ?? "无名"}) 分配角色: {GetRoleDescription(troop.CurrentRole)}");
            }
        }

        /// <summary>
        /// 选择最佳攻击目标
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <param name="enemies">敌军列表</param>
        /// <returns>最佳目标</returns>
        private static Troop SelectBestTarget(Troop troop, List<Troop> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;

            Troop bestTarget = null;
            float bestScore = -1f;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.Destroyed) continue;

                float score = EvaluateTargetPriority(troop, enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 评估目标优先级
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="target">目标</param>
        /// <returns>目标评分</returns>
        private static float EvaluateTargetPriority(Troop attacker, Troop target)
        {
            float score = 100f; // 基础分

            // 距离因素：越近优先级越高
            int distance = AITacticalPositioner.GetManhattanDistance(attacker.Position, target.Position);
            score -= distance * 10;

            // 血量因素：残血敌人优先击杀
            if (target.Quantity < target.Army?.Quantity * 0.3f) // 血量低于30%
            {
                score += 200;
            }

            // 角色针对性
            switch (attacker.CurrentRole)
            {
                case TroopRole.Tank:
                    // 坦克优先攻击远程单位，限制其输出
                    if (target.CurrentRole == TroopRole.DPS || target.CurrentRole == TroopRole.Mage)
                        score += 150;
                    break;

                case TroopRole.DPS:
                    // DPS优先攻击脆皮目标
                    if (target.CurrentRole == TroopRole.Support || target.CurrentRole == TroopRole.Mage)
                        score += 100;
                    break;

                case TroopRole.Mage:
                    // 法师优先控制关键目标
                    if (target.CurrentRole == TroopRole.Tank) // 控制坦克，让队友输出
                        score += 120;
                    break;

                case TroopRole.Support:
                    // 辅助一般不主动攻击，但如果必须攻击，选择最近的
                    score -= 50; // 降低攻击倾向
                    break;
            }

            // 威胁评估：攻击力高的敌人优先级更高
            if (target.Leader != null)
            {
                score += target.Leader.Strength * 0.5f;
            }

            return score;
        }

        /// <summary>
        /// 批量执行AI决策（用于回合制或批量处理）
        /// </summary>
        /// <param name="friendlyTroops">友军部队列表</param>
        /// <param name="enemyTroops">敌军部队列表</param>
        /// <returns>每个部队的移动决策字典</returns>
        public static Dictionary<int, Point> ExecuteBatchTacticalDecisions(List<Troop> friendlyTroops, List<Troop> enemyTroops)
        {
            var decisions = new Dictionary<int, Point>();

            if (friendlyTroops == null || enemyTroops == null) return decisions;

            Console.WriteLine("=== 开始批量AI战术决策 ===");

            // 首先为所有部队分配角色
            AITacticalPositioner.AssignRolesToTroops(friendlyTroops);

            // 然后为每个部队计算最佳位置
            foreach (var troop in friendlyTroops)
            {
                if (troop != null && !troop.Destroyed)
                {
                    Point decision = ExecuteTacticalDecision(troop, enemyTroops, friendlyTroops);
                    decisions[troop.ID] = decision;
                }
            }

            Console.WriteLine($"=== 完成 {decisions.Count} 个部队的战术决策 ===");
            return decisions;
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        private static string GetRoleDescription(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.Tank: return "肉盾";
                case TroopRole.DPS: return "输出";
                case TroopRole.Mage: return "法师";
                case TroopRole.Support: return "辅助";
                case TroopRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 记录战术决策信息
        /// </summary>
        private static void LogTacticalDecision(Troop troop, Troop target, Point newPosition)
        {
            string troopName = troop.Leader?.Name ?? $"部队{troop.ID}";
            string targetName = target.Leader?.Name ?? $"敌军{target.ID}";
            string roleName = GetRoleDescription(troop.CurrentRole);

            if (newPosition != troop.Position)
            {
                Console.WriteLine($"[AI决策] {roleName} {troopName} 从 ({troop.Position.X},{troop.Position.Y}) 移动到 ({newPosition.X},{newPosition.Y}) 攻击 {targetName}");
            }
            else
            {
                Console.WriteLine($"[AI决策] {roleName} {troopName} 保持位置 ({troop.Position.X},{troop.Position.Y}) 攻击 {targetName}");
            }
        }

        /// <summary>
        /// 分析战场态势，提供战术建议
        /// </summary>
        /// <param name="friendlyTroops">友军</param>
        /// <param name="enemyTroops">敌军</param>
        /// <returns>战术分析报告</returns>
        public static string AnalyzeBattlefield(List<Troop> friendlyTroops, List<Troop> enemyTroops)
        {
            if (friendlyTroops == null || enemyTroops == null)
                return "无法分析：部队数据不完整";

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 战场态势分析 ===");

            // 统计双方角色分布
            var friendlyRoles = CountRoleDistribution(friendlyTroops);
            var enemyRoles = CountRoleDistribution(enemyTroops);

            report.AppendLine("友军配置:");
            foreach (var role in friendlyRoles)
            {
                report.AppendLine($"  {GetRoleDescription(role.Key)}: {role.Value}个");
            }

            report.AppendLine("敌军配置:");
            foreach (var role in enemyRoles)
            {
                report.AppendLine($"  {GetRoleDescription(role.Key)}: {role.Value}个");
            }

            // 战术建议
            report.AppendLine("战术建议:");
            
            int friendlyTanks = friendlyRoles.ContainsKey(TroopRole.Tank) ? friendlyRoles[TroopRole.Tank] : 0;
            int enemyDPS = enemyRoles.ContainsKey(TroopRole.DPS) ? enemyRoles[TroopRole.DPS] : 0;
            
            if (friendlyTanks < enemyDPS)
            {
                report.AppendLine("  - 肉盾不足，注意保护脆皮单位");
            }

            if (friendlyRoles.ContainsKey(TroopRole.Support) && friendlyRoles[TroopRole.Support] > 0)
            {
                report.AppendLine("  - 有辅助单位，注意保持阵型完整");
            }

            int enemyMages = enemyRoles.ContainsKey(TroopRole.Mage) ? enemyRoles[TroopRole.Mage] : 0;
            int friendlyMages = friendlyRoles.ContainsKey(TroopRole.Mage) ? friendlyRoles[TroopRole.Mage] : 0;
            
            if (enemyMages > friendlyMages)
            {
                report.AppendLine("  - 敌方法师较多，优先突击或分散阵型");
            }

            return report.ToString();
        }

        /// <summary>
        /// 统计角色分布
        /// </summary>
        private static Dictionary<TroopRole, int> CountRoleDistribution(List<Troop> troops)
        {
            var distribution = new Dictionary<TroopRole, int>();

            foreach (var troop in troops)
            {
                if (troop != null && !troop.Destroyed)
                {
                    TroopRole role = troop.CurrentRole;
                    if (role == TroopRole.None)
                    {
                        role = AIRoleSelector.DetermineRole(troop);
                    }

                    distribution[role] = distribution.ContainsKey(role) ? distribution[role] + 1 : 1;
                }
            }

            return distribution;
        }
    }
}