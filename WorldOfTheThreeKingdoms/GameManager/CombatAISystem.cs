using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 战斗AI系统 - 管理战斗中的AI决策和战术
    /// </summary>
    public class CombatAISystem
    {
        public static CombatAISystem Instance { get; private set; }

        // 战斗状态
        public enum CombatState
        {
            Idle,           // 空闲
            Engaging,       // 交战中
            Pursuing,       // 追击
            Retreating,     // 撤退
            Regrouping      // 重整
        }

        // 战斗决策
        public class CombatDecision
        {
            public Troop Troop { get; set; }
            public CombatState State { get; set; }
            public Point TargetPosition { get; set; }
            public Troop TargetEnemy { get; set; }
            public float Confidence { get; set; } // 决策信心 (0-1)
            public DateTime DecisionTime { get; set; }
        }

        private Dictionary<int, CombatDecision> _troopDecisions;
        private Dictionary<int, CombatState> _troopStates;

        public CombatAISystem()
        {
            Instance = this;
            _troopDecisions = new Dictionary<int, CombatDecision>();
            _troopStates = new Dictionary<int, CombatState>();
        }

        /// <summary>
        /// 评估战斗情况并做出决策
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>战斗决策</returns>
        public CombatDecision EvaluateCombatSituation(Troop troop)
        {
            try
            {
                if (troop?.Leader == null)
                    return null;

                var decision = new CombatDecision
                {
                    Troop = troop,
                    DecisionTime = DateTime.Now,
                    State = CombatState.Idle
                };

                // 查找附近的敌军
                var nearbyEnemies = FindNearbyEnemies(troop, 5);
                
                if (nearbyEnemies.Count == 0)
                {
                    decision.State = CombatState.Idle;
                    decision.Confidence = 1.0f;
                    return decision;
                }

                // 计算战力对比
                float ourStrength = CalculateTroopStrength(troop);
                float enemyStrength = nearbyEnemies.Sum(e => CalculateTroopStrength(e));
                float strengthRatio = ourStrength / Math.Max(1, enemyStrength);

                // 根据战力对比和将领性格决定战斗状态
                if (strengthRatio > 1.5f || troop.Leader.Braveness > 8)
                {
                    // 优势或勇猛 - 主动进攻
                    decision.State = CombatState.Engaging;
                    decision.TargetEnemy = SelectBestTarget(troop, nearbyEnemies);
                    decision.TargetPosition = decision.TargetEnemy?.Position ?? troop.Position;
                    decision.Confidence = Math.Min(1.0f, strengthRatio * 0.5f);
                }
                else if (strengthRatio < 0.6f && troop.Leader.Calmness > 6)
                {
                    // 劣势且冷静 - 撤退
                    decision.State = CombatState.Retreating;
                    decision.TargetPosition = FindSafeRetreatPosition(troop, nearbyEnemies);
                    decision.Confidence = 1.0f - strengthRatio;
                }
                else
                {
                    // 势均力敌 - 谨慎交战
                    decision.State = CombatState.Engaging;
                    decision.TargetEnemy = SelectBestTarget(troop, nearbyEnemies);
                    decision.TargetPosition = decision.TargetEnemy?.Position ?? troop.Position;
                    decision.Confidence = 0.5f;
                }

                // 缓存决策
                _troopDecisions[troop.ID] = decision;
                _troopStates[troop.ID] = decision.State;

                return decision;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAI] 评估战斗情况失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查找附近的敌军
        /// </summary>
        private List<Troop> FindNearbyEnemies(Troop troop, int radius)
        {
            var enemies = new List<Troop>();
            
            try
            {
                if (troop?.BelongedFaction == null)
                    return enemies;

                var center = troop.Position;
                
                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    for (int y = center.Y - radius; y <= center.Y + radius; y++)
                    {
                        var checkPos = new Point(x, y);
                        var otherTroop = Session.Current.Scenario.GetTroopByPosition(checkPos);
                        
                        if (otherTroop != null && 
                            otherTroop != troop && 
                            !troop.BelongedFaction.IsFriendly(otherTroop.BelongedFaction))
                        {
                            enemies.Add(otherTroop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAI] 查找敌军失败: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 计算部队战力
        /// </summary>
        private float CalculateTroopStrength(Troop troop)
        {
            if (troop == null) return 0;

            float strength = troop.FightingForce;
            
            // 考虑士气
            strength *= (troop.Morale / 100.0f);
            
            // 考虑将领能力
            if (troop.Leader != null)
            {
                float leaderBonus = (troop.Leader.Command + troop.Leader.Braveness) / 200.0f;
                strength *= (1.0f + leaderBonus);
            }

            return strength;
        }

        /// <summary>
        /// 选择最佳攻击目标
        /// </summary>
        private Troop SelectBestTarget(Troop troop, List<Troop> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            Troop bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
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
        private float EvaluateTargetPriority(Troop troop, Troop enemy)
        {
            float score = 0;

            // 1. 距离因素 - 越近优先级越高
            int distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, enemy.Position);
            score += (10 - distance) * 10;

            // 2. 敌人强度 - 根据性格决定
            float enemyStrength = CalculateTroopStrength(enemy);
            if (troop.Leader.Braveness > 7)
            {
                // 勇猛的将领倾向于攻击强敌
                score += enemyStrength * 0.1f;
            }
            else
            {
                // 谨慎的将领倾向于攻击弱敌
                score -= enemyStrength * 0.1f;
            }

            // 3. 兵种克制关系
            score += CalculateUnitTypeAdvantage(troop, enemy) * 20;

            // 4. 敌人士气 - 低士气的敌人优先
            score += (100 - enemy.Morale) * 0.5f;

            return score;
        }

        /// <summary>
        /// 计算兵种优势
        /// </summary>
        private float CalculateUnitTypeAdvantage(Troop troop, Troop enemy)
        {
            if (troop?.Army?.Kind == null || enemy?.Army?.Kind == null)
                return 0;

            var ourType = troop.Army.Kind.Type;
            var enemyType = enemy.Army.Kind.Type;

            // 简化的兵种克制关系
            // 骑兵 > 弓兵 > 步兵 > 器械 > 骑兵
            if (ourType == MilitaryType.骑兵 && enemyType == MilitaryType.弩兵)
                return 1.0f;
            if (ourType == MilitaryType.弩兵 && enemyType == MilitaryType.步兵)
                return 0.5f;
            if (ourType == MilitaryType.步兵 && enemyType == MilitaryType.器械)
                return 0.5f;
            if (ourType == MilitaryType.器械 && enemyType == MilitaryType.骑兵)
                return 0.5f;

            // 被克制
            if (enemyType == MilitaryType.骑兵 && ourType == MilitaryType.弩兵)
                return -1.0f;
            if (enemyType == MilitaryType.弩兵 && ourType == MilitaryType.步兵)
                return -0.5f;
            if (enemyType == MilitaryType.步兵 && ourType == MilitaryType.器械)
                return -0.5f;
            if (enemyType == MilitaryType.器械 && ourType == MilitaryType.骑兵)
                return -0.5f;

            return 0;
        }

        /// <summary>
        /// 寻找安全的撤退位置
        /// </summary>
        private Point FindSafeRetreatPosition(Troop troop, List<Troop> enemies)
        {
            try
            {
                // 计算敌人的平均位置
                int avgEnemyX = (int)enemies.Average(e => e.Position.X);
                int avgEnemyY = (int)enemies.Average(e => e.Position.Y);
                Point enemyCenter = new Point(avgEnemyX, avgEnemyY);

                // 向远离敌人的方向撤退
                int deltaX = troop.Position.X - enemyCenter.X;
                int deltaY = troop.Position.Y - enemyCenter.Y;

                // 归一化方向
                float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length > 0)
                {
                    deltaX = (int)((deltaX / length) * 5); // 撤退5格
                    deltaY = (int)((deltaY / length) * 5);
                }

                Point retreatPos = new Point(
                    troop.Position.X + deltaX,
                    troop.Position.Y + deltaY
                );

                // 确保在地图范围内
                var mapSize = Session.Current.Scenario.ScenarioMap.MapDimensions;
                retreatPos.X = Math.Max(0, Math.Min(mapSize.X - 1, retreatPos.X));
                retreatPos.Y = Math.Max(0, Math.Min(mapSize.Y - 1, retreatPos.Y));

                return retreatPos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAI] 寻找撤退位置失败: {ex.Message}");
                return troop.Position;
            }
        }

        /// <summary>
        /// 获取部队的战斗状态
        /// </summary>
        public CombatState GetTroopCombatState(int troopId)
        {
            return _troopStates.ContainsKey(troopId) ? _troopStates[troopId] : CombatState.Idle;
        }

        /// <summary>
        /// 获取部队的战斗决策
        /// </summary>
        public CombatDecision GetTroopDecision(int troopId)
        {
            return _troopDecisions.ContainsKey(troopId) ? _troopDecisions[troopId] : null;
        }

        /// <summary>
        /// 清理过期的决策
        /// </summary>
        public void CleanupOldDecisions()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddSeconds(-30);
                var expiredIds = _troopDecisions
                    .Where(kvp => kvp.Value.DecisionTime < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var id in expiredIds)
                {
                    _troopDecisions.Remove(id);
                    _troopStates.Remove(id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAI] 清理决策失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置战斗AI系统
        /// </summary>
        public void Reset()
        {
            _troopDecisions.Clear();
            _troopStates.Clear();
        }

        /// <summary>
        /// 获取战斗状态描述
        /// </summary>
        public string GetCombatStateDescription(CombatState state)
        {
            return state switch
            {
                CombatState.Idle => "空闲",
                CombatState.Engaging => "交战中",
                CombatState.Pursuing => "追击",
                CombatState.Retreating => "撤退",
                CombatState.Regrouping => "重整",
                _ => "未知"
            };
        }
    }
}
