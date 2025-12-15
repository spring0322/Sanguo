using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// AI决策管理器：整合战术评估器，为AI提供智能决策
    /// </summary>
    public class AIDecisionManager
    {
        private readonly Dictionary<int, AIDecisionCache> _decisionCache;
        private readonly Random _random;
        
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
            if (_decisionCache.ContainsKey(cacheKey))
            {
                var cached = _decisionCache[cacheKey];
                if (cached.IsValid())
                    return cached.Decision;
            }

            // 计算新决策
            var decision = CalculateBestDecision(troop, scenario);
            
            // 缓存决策
            _decisionCache[cacheKey] = new AIDecisionCache(decision, DateTime.Now);
            
            return decision;
        }

        private AIDecision CalculateBestDecision(Troop troop, GameScenario scenario)
        {
            var bestDecision = new AIDecision
            {
                Action = AIActionType.Wait,
                Score = -1000,
                Skill = null,
                Target = null
            };

            // 1. 评估所有可用技能
            foreach (var skill in troop.AvailableSkills)
            {
                // 跳过消耗过大的技能
                if (troop.CurrentPrestige < skill.Cost)
                    continue;

                // 评估所有可能的目标
                var potentialTargets = GetPotentialTargets(troop, skill, scenario);
                
                foreach (var target in potentialTargets)
                {
                    float score = CombatEvaluator.EvaluateSkill(troop, skill, target, scenario);
                    
                    // 添加随机性，避免AI过于机械
                    score += _random.Next(-20, 21);
                    
                    if (score > bestDecision.Score)
                    {
                        bestDecision.Action = AIActionType.UseSkill;
                        bestDecision.Score = score;
                        bestDecision.Skill = skill;
                        bestDecision.Target = target;
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

            return bestDecision;
        }

        private List<Troop> GetPotentialTargets(Troop source, Skill skill, GameScenario scenario)
        {
            var targets = new List<Troop>();

            if (skill.IsDamage || skill.IsControl)
            {
                // 攻击性技能：寻找敌军目标
                targets.AddRange(scenario.GetEnemyTroops(source.BelongedFaction)
                    .Where(t => IsInRange(source, t, skill.Range)));
            }
            
            if (skill.IsHealing || skill.IsBuff)
            {
                // 支援性技能：寻找友军目标
                targets.AddRange(scenario.GetFriendlyTroops(source.BelongedFaction)
                    .Where(t => IsInRange(source, t, skill.Range)));
            }

            return targets;
        }

        private AIDecision EvaluateMovement(Troop troop, GameScenario scenario)
        {
            var decision = new AIDecision
            {
                Action = AIActionType.Move,
                Score = 0
            };

            // 简化的移动评估：向最近的敌人移动
            var nearestEnemy = scenario.GetNearestEnemy(troop);
            if (nearestEnemy != null)
            {
                var distance = CalculateDistance(troop.Position, nearestEnemy.Position);
                
                // 如果距离适中，移动有价值
                if (distance > 2 && distance < 10)
                {
                    decision.Score = 50 - distance * 5;
                    decision.TargetPosition = GetOptimalMovePosition(troop, nearestEnemy, scenario);
                }
            }

            return decision;
        }

        private AIDecision EvaluateDefense(Troop troop, GameScenario scenario)
        {
            var decision = new AIDecision
            {
                Action = AIActionType.Defend,
                Score = 0
            };

            // 如果血量较低且有敌人接近，考虑防御
            if (troop.HpRatio < 0.4f)
            {
                var nearbyEnemies = scenario.GetTroopsInRadius(troop.Position, 3)
                    .Where(t => troop.IsEnemy(t))
                    .Count();

                if (nearbyEnemies > 0)
                {
                    decision.Score = 100 + nearbyEnemies * 20;
                }
            }

            return decision;
        }

        private bool IsInRange(Troop source, Troop target, int range)
        {
            return CalculateDistance(source.Position, target.Position) <= range;
        }

        private int CalculateDistance(Point pos1, Point pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        private Point GetOptimalMovePosition(Troop troop, Troop target, GameScenario scenario)
        {
            // 简化实现：向目标方向移动一格
            var dx = Math.Sign(target.Position.X - troop.Position.X);
            var dy = Math.Sign(target.Position.Y - troop.Position.Y);
            
            return new Point(troop.Position.X + dx, troop.Position.Y + dy);
        }

        private int GenerateCacheKey(Troop troop, GameScenario scenario)
        {
            // 简化的缓存键生成
            return HashCode.Combine(
                troop.ID,
                troop.Position.GetHashCode(),
                troop.CurrentHP,
                scenario.CurrentTurn
            );
        }

        /// <summary>
        /// 清理过期的缓存
        /// </summary>
        public void CleanupCache()
        {
            var expiredKeys = _decisionCache
                .Where(kvp => !kvp.Value.IsValid())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _decisionCache.Remove(key);
            }
        }
    }

    /// <summary>
    /// AI决策结果
    /// </summary>
    public class AIDecision
    {
        public AIActionType Action { get; set; }
        public float Score { get; set; }
        public Skill Skill { get; set; }
        public Troop Target { get; set; }
        public Point? TargetPosition { get; set; }
    }

    /// <summary>
    /// AI行动类型
    /// </summary>
    public enum AIActionType
    {
        Wait,       // 等待
        Move,       // 移动
        UseSkill,   // 使用技能
        Defend,     // 防御
        Retreat     // 撤退
    }

    /// <summary>
    /// AI决策缓存
    /// </summary>
    internal class AIDecisionCache
    {
        public AIDecision Decision { get; }
        public DateTime CreatedTime { get; }
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(5);

        public AIDecisionCache(AIDecision decision, DateTime createdTime)
        {
            Decision = decision;
            CreatedTime = createdTime;
        }

        public bool IsValid()
        {
            return DateTime.Now - CreatedTime < CacheExpiry;
        }
    }
}