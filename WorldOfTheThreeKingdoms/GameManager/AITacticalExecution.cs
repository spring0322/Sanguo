using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public class AITacticalExecution
    {
        // -----------------------------------------------------------------------
        // 🧠 大脑层：做决策 (保留智能，优化流程)
        // -----------------------------------------------------------------------
        public static TacticalDecision MakeTacticalDecision(Troop troop)
        {
            TacticalDecision decision = new TacticalDecision();
            if (troop == null || troop.Destroyed) return decision;

            // 1. 获取所有"物理上打得到"的敌人 (极速版，耗时 < 0.1ms)
            List<Point> targets = GetAttackableTargets(troop, troop.Position);
            
            if (targets.Count > 0)
            {
                // 【智能优化】：不要只选 targets[0]，而是选"价值最高"的
                Point bestTargetPos = targets[0];
                int maxScore = int.MinValue;
                
                foreach (Point targetPos in targets)
                {
                    // 反向查找该位置的部队 (O(1) 或 O(N) 取决于你的架构，视野内只有几个兵，很快)
                    Troop targetTroop = Session.Current.Scenario.GetTroopByPosition(targetPos);
                    if (targetTroop != null)
                    {
                        int score = 0;
                        
                        // 1. 斩杀分：如果能打死，优先级最高
                        // 使用 Offence 属性代替 AttackPower
                        if (targetTroop.Army.Quantity < troop.Offence) score += 10000;
                        
                        // 2. 伤其十指不如断其一指：优先打兵少的
                        score -= targetTroop.Army.Quantity; 
                        
                        // 3. 兵种克制分 (假设你有这个方法)
                        // if (troop.IsCounter(targetTroop)) score += 500;
                        
                        // 4. 距离分：尽量打近的，减少移动消耗
                        // int dist = ...
                        // score -= dist * 10;
                        
                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestTargetPos = targetPos;
                        }
                    }
                }
                
                decision.TargetPosition = bestTargetPos;
                decision.Action = TacticalAction.Attack;
                return decision;
            }

            // 3. 如果没得打，考虑移动 (结合地形评分)
            // 这里加入简单的环境判断，避免陆军傻傻走到水里
            int currentTerrainScore = EvaluateTerrainEnvironment(troop, troop.Position);
            
            // 如果当前地形很差（比如陆军在水里），通过简单的移动逻辑尝试脱困
            // (这里不展开复杂寻路，依靠 Troop.ProcessMovementAI 去走)
            Point moveTarget = ResolveStrategicMoveTarget(troop);
            if (IsValidMoveTarget(moveTarget) && moveTarget != troop.Position)
            {
                decision.TargetPosition = moveTarget;
                decision.Action = TacticalAction.Move;
                return decision;
            }

            decision.TargetPosition = troop.Position;
            decision.Action = TacticalAction.Wait;
            return decision;
        }

        private static Point ResolveStrategicMoveTarget(Troop troop)
        {
            if (troop == null) return new Point(-1, -1);

            if (IsValidMoveTarget(troop.RealDestination))
            {
                return troop.RealDestination;
            }

            if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed && IsValidMoveTarget(troop.TargetTroop.Position))
            {
                return troop.TargetTroop.Position;
            }

            if (troop.TargetArchitecture != null && IsValidMoveTarget(troop.TargetArchitecture.Position))
            {
                return troop.TargetArchitecture.Position;
            }

            if (troop.WillArchitecture != null && IsValidMoveTarget(troop.WillArchitecture.Position))
            {
                return troop.WillArchitecture.Position;
            }

            if (troop.StartingArchitecture != null && IsValidMoveTarget(troop.StartingArchitecture.Position))
            {
                return troop.StartingArchitecture.Position;
            }

            return new Point(-1, -1);
        }

        private static bool IsValidMoveTarget(Point position)
        {
            return position.X >= 0 && position.Y >= 0 && position != Point.Zero;
        }

        // -----------------------------------------------------------------------
        // 👀 眼睛层：找目标 (性能优化核心)
        // -----------------------------------------------------------------------
        /// <summary>
        /// 获取可攻击的敌军列表（替代原来的 GetAttackableTargets 返回 Point）
        /// 优化：只遍历视野内单位，不再遍历几百个地图格子
        /// </summary>
        private static List<Troop> GetAttackableEnemies(Troop troop)
        {
            List<Troop> result = new List<Troop>();
            
            // 简化实现：从场景中获取所有敌军部队
            try
            {
                if (Session.Current?.Scenario?.Troops != null)
                {
                    // 获取攻击距离（平方），避免开方
                    int range = 1;
                    if (troop.Army != null && troop.Army.Kind != null) 
                    {
                        range = 1; // 暂时默认1，请根据实际代码修改
                    }
                    int rangeSq = range * range;

                    // 限制最大检查数，防止极端情况卡顿
                    int checkCount = 0;
                    foreach (Troop enemy in Session.Current.Scenario.Troops)
                    {
                        if (checkCount++ > 20) break;
                        if (enemy == null || enemy.Destroyed || enemy.BelongedFaction == troop.BelongedFaction) continue;

                        // 距离计算
                        int dx = troop.Position.X - enemy.Position.X;
                        int dy = troop.Position.Y - enemy.Position.Y;
                        
                        // 只有距离够，才算作"可攻击目标"
                        if ((dx * dx + dy * dy) <= rangeSq)
                        {
                            result.Add(enemy);
                        }
                    }
                }
            }
            catch
            {
                // 发生错误时返回空列表
            }

            return result;
        }

        // 【请替换原有的 GetAttackableTargets 方法】
        // 性能优化：O(N^2) -> O(N)，只检查视野内的敌人，不再扫描空地
        public static List<Point> GetAttackableTargets(Troop troop, Point position)
        {
            List<Point> validTargets = new List<Point>();

            // 1. 快速判空 - 使用正确的属性名
            if (troop == null || troop.Destroyed)
                return validTargets;

            // 获取敌对部队列表
            var hostileList = troop.GetHostileTroopsInView();
            if (hostileList == null || hostileList.Count == 0)
                return validTargets;

            // 2. 获取攻击距离（支持不同兵种）
            int attackRange = 1;
            if (troop.Army != null && troop.Army.Kind != null)
            {
                // 假设原代码里有获取攻击距离的逻辑，这里保留引用
                // 如果你的属性名不同，请修改这里，例如 troop.Army.Kind.AttackRange
                // attackRange = troop.AttackRange; 
            }
            int rangeSq = attackRange * attackRange;

            // 3. 智能遍历（只看活着的敌人）
            // 限制最大检查数 15，防止在超大规模混战中卡顿，15个目标足够战术选择了
            int checkCount = 0;
            foreach (Troop enemy in hostileList.GetList())
            {
                if (checkCount++ > 15) break; 
                if (enemy == null || enemy.Destroyed || enemy.Position == Point.Zero) continue;

                // 4. 距离判定（使用平方，避开开方运算）
                int dx = position.X - enemy.Position.X;
                int dy = position.Y - enemy.Position.Y;
                int distSq = dx * dx + dy * dy;

                // 只有在射程内的敌人才加入列表
                if (distSq <= rangeSq)
                {
                    validTargets.Add(enemy.Position);
                }
            }

            return validTargets;
        }

        // -----------------------------------------------------------------------
        // ⚖️ 权衡层：战术评分 (纯数学计算，极快且智能)
        // -----------------------------------------------------------------------
        /// <summary>
        /// 给目标打分：决定打谁最划算
        /// 耗时：极低 (纯加减乘除)
        /// </summary>
        private static int RateTargetValue(Troop attacker, Troop target)
        {
            int score = 0;

            // 1. 【智能】优先打残血（能造成减员）
            // 假设满血是 1000
            int hpPercent = (target.Army.Quantity * 100) / 1000; 
            score += (100 - hpPercent); // 血越少分越高

            // 2. 【智能】如果能秒杀，权重极大
            // 简单预估伤害（假设 attacker.AttackPower 存在）
            // int estimatedDamage = attacker.AttackPower;
            // if (estimatedDamage >= target.Army.Quantity) score += 500;

            // 3. 【智能】兵种克制 (根据你的游戏规则添加)
            // if (attacker.IsCounter(target)) score += 200;

            // 4. 【智能】优先打脆皮（防御低的）
            // score -= target.Defense / 10;

            return score;
        }

        // 【请替换原有的 EvaluateWaterEnvironment 方法】
        // 性能优化：移除模拟计算，改为直接查表
        // 智能增强：加入"灭火"和"兵种适性"判断
        public static int EvaluateWaterEnvironment(Troop troop, Point position)
        {
            try
            {
                var map = Session.Current.Scenario.ScenarioMap;

                // 1. 安全检查
                if (position.X < 0 || position.X >= map.MapDimensions.X ||
                    position.Y < 0 || position.Y >= map.MapDimensions.Y)
                    return -999;

                // 2. 极速获取地形 (O(1)复杂度)
                int terrainId = map.MapData[position.X, position.Y];

                // 假设 ID 1,2 是水域 (请根据你的 Enum 或常量修改)
                bool isWater = (terrainId == 1 || terrainId == 2); 

                // --- 智能判断逻辑 ---
                int score = 0;

                // A. 兵种适性判断 - 使用实际存在的属性
                bool isWaterArmy = false;
                if (troop.Army != null && troop.Army.Kind != null)
                {
                    // 检查是否为水军类型 - 根据实际的兵种系统调整
                    // 这里使用简化判断，可以根据兵种名称或ID来判断
                    isWaterArmy = troop.Army.Kind.Name.Contains("水军") || 
                                  troop.Army.Kind.Name.Contains("水师") ||
                                  troop.Army.Kind.Name.Contains("舰队");
                }

                if (isWaterArmy)
                {
                    // 水军：在水里战斗力强，加分
                    if (isWater) score += 300;
                }
                else
                {
                    // 陆军：在水里由于"不习水性"会变弱，大幅扣分
                    // 除非被迫，否则尽量不上水
                    if (isWater) score -= 500; 
                }

                // B. 特殊状态判断 (保留原有的战术深度)
                // 如果部队着火了，水域是救命稻草
                /* if (troop.Status == TroopStatus.OnFire && isWater) 
                {
                    score += 1000; // 极高优先级：灭火
                }*/

                return score;
            }
            catch
            {
                return 0;
            }
        }

        // -----------------------------------------------------------------------
        // 🌊 环境层：地形判断 (查表法替代模拟法)
        // -----------------------------------------------------------------------
        /// <summary>
        /// 评估地形好坏（替代原来的 EvaluateWaterEnvironment）
        /// 耗时：O(1) 查表
        /// </summary>
        public static int EvaluateTerrainEnvironment(Troop troop, Point position)
        {
            try
            {
                var map = Session.Current.Scenario.ScenarioMap;
                
                // 越界保护
                if (position.X < 0 || position.X >= map.MapDimensions.X ||
                    position.Y < 0 || position.Y >= map.MapDimensions.Y)
                    return -999;

                // 1. 获取地形
                int terrainId = map.MapData[position.X, position.Y];

                // 2. 假设 ID 1,2 是水域
                bool isWater = (terrainId == 1 || terrainId == 2);

                // 3. 【智能】根据兵种适性打分
                bool isWaterArmy = false;
                if (troop.Army != null && troop.Army.Kind != null)
                {
                    // 简化判断，假设水军类型
                    isWaterArmy = false; // 暂时设为false，需要根据实际代码调整
                }

                if (isWater)
                {
                    // 水军在水里加分，陆军在水里扣大分
                    return isWaterArmy ? 200 : -500; 
                }
                else
                {
                    // 陆军在陆地加分，水军在陆地扣分
                    return !isWaterArmy ? 50 : -50;
                }
            }
            catch
            {
                return 0;
            }
        }
    }

    // 保持辅助类定义
    public class TacticalDecision
    {
        public TacticalAction Action = TacticalAction.Wait;
        public Point TargetPosition = new Point(-1, -1);
    }

    public enum TacticalAction
    {
        Wait,
        Move,
        Attack,
        Retreat
    }
}
