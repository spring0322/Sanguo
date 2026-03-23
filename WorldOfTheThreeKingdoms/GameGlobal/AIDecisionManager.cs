using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    /// <summary>
    /// AI决策管理器：整合战术评估器，为AI提供智能决策
    /// </summary>
    public class AIDecisionManager
    {
        private readonly Dictionary<int, AIDecisionCache> _decisionCache;
        private readonly Random _random;
        
        public static AIDecisionManager Instance { get; } = new AIDecisionManager();

        public AIDecisionManager()
        {
            _decisionCache = new Dictionary<int, AIDecisionCache>();
            _random = new Random();
        }

        /// <summary>
        /// 为指定部队选择最佳技能和目标
        /// </summary>
        public AIDecision MakeCombatDecision(Troop troop, GameScenario scenario)
        {
            // 检查缓存
            var cacheKey = GenerateCacheKey(troop, scenario);
            if (_decisionCache.TryGetValue(troop.ID, out var cachedDecision))
            {
                if (cachedDecision.IsValid(GetDayCount(scenario.Date)))
                {
                    return cachedDecision.Decision;
                }
            }

            var bestDecision = new AIDecision
            {
                Action = AIActionType.Wait,
                Score = -1000,
                Skill = null,
                Target = null
            };

            // 1. 评估所有可用战法 (CombatMethods)
            if (troop.CombatMethods != null)
            {
                foreach (var combatMethod in troop.CombatMethods.GetCombatMethodList())
                {
                    var cm = combatMethod as CombatMethod;
                    if (cm == null) continue;

                    // 检查士气/战意消耗 (Combativity)
                    if (troop.Combativity < cm.Combativity)
                        continue;

                    // 检查施展条件
                    if (!cm.IsCastable(troop))
                        continue;

                    // 评估所有可能的目标
                    var potentialTargets = GetPotentialTargets(troop, cm, scenario);
                    
                    foreach (var target in potentialTargets)
                    {
                        float score = EvaluateCombatMethod(troop, cm, target, scenario);
                        
                        // 添加随机性，避免AI过于机械
                        score += _random.Next(-20, 21);
                        
                        if (score > bestDecision.Score)
                        {
                            bestDecision.Action = AIActionType.UseSkill; 
                            bestDecision.Score = score;
                            bestDecision.Skill = null; 
                            bestDecision.Target = target;
                        }
                    }
                }
            }

            // 2. 评估移动选项
            var moveDecision = EvaluateMovement(troop, scenario);
            if (moveDecision.Score > bestDecision.Score)
            {
                bestDecision = moveDecision;
            }

            // 3. 评估防御选项
            var defendDecision = EvaluateDefense(troop, scenario);
            if (defendDecision.Score > bestDecision.Score)
            {
                bestDecision = defendDecision;
            }

            // 更新缓存
            _decisionCache[troop.ID] = new AIDecisionCache
            {
                Decision = bestDecision,
                Timestamp = GetDayCount(scenario.Date)
            };

            return bestDecision;
        }

        private int GetDayCount(GameDate date)
        {
            return date.Year * 360 + date.Month * 30 + date.Day;
        }

        private List<Troop> GetPotentialTargets(Troop source, CombatMethod cm, GameScenario scenario)
        {
            var targets = new List<Troop>();
            int range = 1; // Default range
            
            if (cm.ViewingHostile)
            {
                // 攻击性技能：寻找敌军目标
                targets.AddRange(GetEnemyTroops(scenario, source.BelongedFaction)
                    .Where(t => IsInRange(source, t, range)));
            }
            else
            {
                // 支援性技能：寻找友军目标
                targets.AddRange(GetFriendlyTroops(scenario, source.BelongedFaction)
                    .Where(t => IsInRange(source, t, range)));
            }

            return targets;
        }

        private float EvaluateCombatMethod(Troop source, CombatMethod cm, Troop target, GameScenario scenario)
        {
            float score = 0;
            if (cm.ViewingHostile)
            {
                score += cm.Combativity * 2;
                if (target.Quantity < 5000) score += 50; 
            }
            else
            {
                score += cm.Combativity;
                if (target.Quantity < 5000) score += 50;
            }
            return score;
        }

        private AIDecision EvaluateMovement(Troop troop, GameScenario scenario)
        {
            var decision = new AIDecision
            {
                Action = AIActionType.Move,
                Score = 0
            };

            var nearestEnemy = GetNearestEnemy(scenario, troop);
            if (nearestEnemy != null)
            {
                float distance = GetDistance(troop.Position, nearestEnemy.Position);
                // Simple score: closer to enemy is better if healthy
                if (troop.Quantity > 5000)
                {
                    decision.Score = 100 - distance;
                }
                else
                {
                    // Retreat if weak
                    decision.Score = distance * 2;
                }
            }

            return decision;
        }

        private AIDecision EvaluateDefense(Troop troop, GameScenario scenario)
        {
            return new AIDecision { Action = AIActionType.Wait, Score = 10 };
        }

        private bool IsInRange(Troop a, Troop b, int range)
        {
            return GetDistance(a.Position, b.Position) <= range;
        }

        private int GenerateCacheKey(Troop troop, GameScenario scenario)
        {
            return troop.ID ^ GetDayCount(scenario.Date);
        }

        private IEnumerable<Troop> GetEnemyTroops(GameScenario scenario, Faction faction)
        {
            foreach (Troop t in scenario.Troops)
            {
                if (t.BelongedFaction != faction) yield return t;
            }
        }

        private IEnumerable<Troop> GetFriendlyTroops(GameScenario scenario, Faction faction)
        {
            foreach (Troop t in scenario.Troops)
            {
                if (t.BelongedFaction == faction) yield return t;
            }
        }

        private Troop GetNearestEnemy(GameScenario scenario, Troop source)
        {
            Troop nearest = null;
            double minDist = double.MaxValue;
            foreach (Troop t in GetEnemyTroops(scenario, source.BelongedFaction))
            {
                double dist = GetDistance(source.Position, t.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = t;
                }
            }
            return nearest;
        }

        private float GetDistance(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public float CalculateThreat(Troop me, Point pos, int difficulty)
        {
             return 0.0f;
        }
    }

    public struct AIDecision
    {
        public AIActionType Action;
        public float Score;
        public Skill Skill;
        public object Target;
    }

    public enum AIActionType
    {
        Wait,
        Move,
        UseSkill,
        Defend,
        Retreat
    }

    public class AIDecisionCache
    {
        public AIDecision Decision;
        public int Timestamp;
        public bool IsValid(int currentTimestamp) => Timestamp == currentTimestamp;
    }
}