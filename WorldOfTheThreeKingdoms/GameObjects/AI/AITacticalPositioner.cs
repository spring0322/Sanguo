using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameObjects.AI.Helper;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI
{
    /// <summary>
    /// 战术位移选择器 (Phase 2)
    /// 负责计算移动范围内每个格子的战术评分
    /// </summary>
    public static class AITacticalPositioner
    {
        // ================= 评分参数 =================
        // 基础分
        private const int Score_Base = 1000;

        // Tank 权重
        private const int Tank_Bonus_ZocLock = 2000;    // 贴脸锁定敌军 (ZOC)
        private const int Tank_Bonus_Protect = 500;     // 位于保护线上 (包围盒)
        private const int Tank_Bonus_Cluster = 100;     // 靠近队友 (抱团)

        // DPS 权重
        private const int Dps_Penalty_MeleeRisk = -1500; // 脆皮被贴脸的惩罚
        private const int Dps_Bonus_MaxRange = 300;      // 极限射程打击 (风筝)

        /// <summary>
        /// 获取当前部队的最佳移动目标点
        /// </summary>
        /// <param name="me">当前行动的 AI 部队</param>
        /// <param name="target">首要攻击目标</param>
        /// <param name="allies">所有友军列表 (用于计算保护位)</param>
        /// <param name="reachablePoints">移动力可达的所有坐标列表</param>
        /// <returns>最佳坐标点</returns>
        public static Point GetBestPosition(Troop me, Troop target, List<Troop> allies, List<Point> reachablePoints)
        {
            if (me == null || target == null || reachablePoints.Count == 0) 
                return me.Position; // 无法移动或无目标，保持原地

            Point bestPoint = me.Position;
            float maxScore = -99999f;

            // 确保部队有角色分配
            EnsureRoleAssigned(me);

            // 寻找需要保护的脆皮队友 (用于 Tank 逻辑)
            Troop vulnerableAlly = GetNearestVulnerableAlly(me, allies);

            foreach (var point in reachablePoints)
            {
                float currentScore = Score_Base;

                // 距离惩罚：尽量少走几步就能达到目的 (保留移动力或 AP)
                int distMoved = GetManhattanDistance(me.Position, point);
                currentScore -= distMoved * 10;

                // 根据角色应用不同的评分策略
                switch (me.CurrentRole)
                {
                    case TroopRole.Tank:
                        currentScore += EvaluateTankPosition(point, target, vulnerableAlly);
                        break;
                    case TroopRole.DPS:
                    case TroopRole.Mage:
                        currentScore += EvaluateRangedPosition(me, point, target);
                        break;
                    case TroopRole.Support:
                        currentScore += EvaluateSupportPosition(point, allies);
                        break;
                    default:
                        // 默认逻辑：只管靠近敌人
                        currentScore -= GetManhattanDistance(point, target.Position) * 50;
                        break;
                }

                // 更新最高分
                if (currentScore > maxScore)
                {
                    maxScore = currentScore;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// 确保部队已分配角色，如果没有则自动分配
        /// </summary>
        /// <param name="troop">需要检查角色的部队</param>
        private static void EnsureRoleAssigned(Troop troop)
        {
            if (troop.CurrentRole == TroopRole.None)
            {
                troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
                Console.WriteLine($"自动为部队 {troop.ID} 分配角色: {GetRoleDescription(troop.CurrentRole)}");
            }
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

        // ---------- 战术评分逻辑 ----------

        /// <summary>
        /// Tank 评分：优先贴脸卡 ZOC，优先站在敌人和脆皮之间
        /// </summary>
        private static float EvaluateTankPosition(Point pos, Troop target, Troop vulnerableAlly)
        {
            float score = 0;
            int distToEnemy = GetManhattanDistance(pos, target.Position);

            // 1. ZOC 锁定逻辑：针对弓/骑，贴脸 (距离=1) 价值极大
            if (distToEnemy == 1)
            {
                score += Tank_Bonus_ZocLock;
            }
            else
            {
                // 如果不能贴脸，越近越好
                score -= distToEnemy * 50;
            }

            // 2. 矩形包围盒保护逻辑
            if (vulnerableAlly != null)
            {
                // 如果当前点位于 [敌人] 和 [脆皮] 构成的矩形内，说明阻挡了路径
                if (IsPointInBoundingBox(pos, target.Position, vulnerableAlly.Position))
                {
                    score += Tank_Bonus_Protect;
                }
            }

            return score;
        }

        /// <summary>
        /// 远程/脆皮 评分：保持距离，避免接敌
        /// </summary>
        public static float EvaluateRangedPosition(Troop me, Point pos, Troop target)
        {
            float score = 0;
            int distToEnemy = GetManhattanDistance(pos, target.Position);

            // 1. 安全检查：如果位置贴脸，极大扣分 (除非能直接秒杀，暂不考虑)
            if (distToEnemy <= 1)
            {
                score += Dps_Penalty_MeleeRisk;
            }

            // 2. 射程检查：必须在攻击范围内
            // 使用部队的实际攻击范围
            int myRange = GetTroopAttackRange(me);

            // 处于射程内且不贴脸
            if (distToEnemy > 1 && distToEnemy <= myRange) 
            {
                score += Dps_Bonus_MaxRange;
                // 偏好最远距离输出 (风筝)
                if (distToEnemy == myRange) 
                    score += 100;
            }
            else if (distToEnemy > myRange)
            {
                // 超出射程，需要靠近
                score -= (distToEnemy - myRange) * 100;
            }

            return score;
        }

        /// <summary>
        /// 辅助 评分：躲在最人堆里
        /// </summary>
        private static float EvaluateSupportPosition(Point pos, List<Troop> allies)
        {
            float score = 0;

            // 计算周围 2 格内有多少队友
            int neighborCount = 0;
            foreach (var ally in allies)
            {
                if (GetManhattanDistance(pos, ally.Position) <= 2)
                {
                    neighborCount++;
                }
            }

            // 队友越多越安全，光环覆盖越好
            score += neighborCount * 200;

            return score;
        }

        // ---------- 几何算法辅助 ----------

        /// <summary>
        /// 获取曼哈顿距离 (|x1-x2| + |y1-y2|)
        /// </summary>
        public static int GetManhattanDistance(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }

        /// <summary>
        /// 判定点是否在两点构成的矩形包围盒内 (包含边界)
        /// 用于判断是否处于"卡位"路径上
        /// </summary>
        private static bool IsPointInBoundingBox(Point p, Point corner1, Point corner2)
        {
            int minX = Math.Min(corner1.X, corner2.X);
            int maxX = Math.Max(corner1.X, corner2.X);
            int minY = Math.Min(corner1.Y, corner2.Y);
            int maxY = Math.Max(corner1.Y, corner2.Y);

            return p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY;
        }

        /// <summary>
        /// 寻找最近的需要保护的队友 (Mage/Support/DPS)
        /// </summary>
        private static Troop GetNearestVulnerableAlly(Troop me, List<Troop> allies)
        {
            Troop bestTarget = null;
            int minDistance = 999;

            foreach (var ally in allies)
            {
                if (ally == me) continue;

                // 只有脆皮需要保护
                if (ally.CurrentRole == TroopRole.Mage || 
                    ally.CurrentRole == TroopRole.Support || 
                    ally.CurrentRole == TroopRole.DPS)
                {
                    int dist = GetManhattanDistance(me.Position, ally.Position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = ally;
                    }
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 获取部队的攻击范围
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>攻击范围（格子数）</returns>
        private static int GetTroopAttackRange(Troop troop)
        {
            try
            {
                // 使用部队的实际攻击范围属性
                return troop.OffenceRadius;
            }
            catch
            {
                // 如果获取失败，根据角色返回默认值
                switch (troop.CurrentRole)
                {
                    case TroopRole.Mage:
                        return 3; // 法师通常射程较远
                    case TroopRole.DPS:
                        return 2; // 弓兵等远程单位
                    case TroopRole.Tank:
                        return 1; // 近战单位
                    default:
                        return 2; // 默认值
                }
            }
        }

        /// <summary>
        /// 批量为部队分配角色（用于初始化或重新评估）
        /// </summary>
        /// <param name="troops">部队列表</param>
        public static void AssignRolesToTroops(List<Troop> troops)
        {
            if (troops == null || troops.Count == 0) return;

            Console.WriteLine("=== 开始批量分配AI战术角色 ===");
            
            foreach (var troop in troops)
            {
                if (troop != null)
                {
                    TroopRole oldRole = troop.CurrentRole;
                    troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
                    
                    if (oldRole != troop.CurrentRole)
                    {
                        Console.WriteLine($"部队 {troop.ID} ({troop.Leader?.Name ?? "无名"}) 角色变更: {GetRoleDescription(oldRole)} -> {GetRoleDescription(troop.CurrentRole)}");
                    }
                }
            }
            
            Console.WriteLine("=== 角色分配完成 ===");
        }

        /// <summary>
        /// 获取可移动的位置列表（使用真实的游戏寻路系统）
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="maxDistance">最大移动距离（可选）</param>
        /// <returns>可达位置列表</returns>
        public static List<Point> GetReachablePositions(Troop troop, int? maxDistance = null)
        {
            try
            {
                // 使用MapNavigationHelper获取真实的可移动区域
                return Helper.MapNavigationHelper.GetUnitMoveableArea(troop, maxDistance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AITacticalPositioner] 获取可达位置时发生错误: {ex.Message}");
                
                // 如果出错，回退到简化版本
                var reachablePoints = new List<Point>();
                Point currentPos = troop.Position;
                int searchDistance = maxDistance ?? 3;

                for (int x = currentPos.X - searchDistance; x <= currentPos.X + searchDistance; x++)
                {
                    for (int y = currentPos.Y - searchDistance; y <= currentPos.Y + searchDistance; y++)
                    {
                        Point testPoint = new Point(x, y);
                        int distance = GetManhattanDistance(currentPos, testPoint);
                        
                        if (distance <= searchDistance && distance > 0)
                        {
                            reachablePoints.Add(testPoint);
                        }
                    }
                }

                return reachablePoints;
            }
        }
    }
}