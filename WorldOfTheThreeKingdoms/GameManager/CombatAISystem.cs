using System;
using System.Collections.Generic;
using System.Linq;
using global::GameGlobal;
using global::GameManager;
using Microsoft.Xna.Framework;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 战斗决策类型枚举
    /// </summary>
    public enum CombatDecisionType
    {
        Attack,
        Defend,
        Retreat,
        Scout,
        Advance,
        Idle
    }

    /// <summary>
    /// 战斗决策结果
    /// </summary>
    public class CombatDecision
    {
        public CombatAISystem.CombatState State { get; set; }
        public CombatDecisionType Type { get; set; }
        public float Priority { get; set; }
        public Point TargetPosition { get; set; }
        public Troop TargetTroop { get; set; }
        public string Reason { get; set; }
        public float Confidence { get; set; }
        public DateTime DecisionTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 战斗AI系统 - 完整版本
    /// </summary>
    public class CombatAISystem
    {
        /// <summary>
        /// 战斗状态枚举
        /// </summary>
        public enum CombatState
        {
            Idle,           // 空闲
            Scouting,       // 侦察
            Advancing,      // 推进
            Attacking,      // 攻击
            Defending,      // 防御
            Retreating,     // 撤退
            Regrouping      // 重组
        }

        public static CombatAISystem Instance { get; private set; }
        
        private Dictionary<int, string> _combatStrategies;
        private Dictionary<int, CombatDecision> _combatDecisions;
        private Dictionary<int, DateTime> _lastDecisionTime;
        
        public CombatAISystem()
        {
            Instance = this;
            _combatStrategies = new Dictionary<int, string>();
            _combatDecisions = new Dictionary<int, CombatDecision>();
            _lastDecisionTime = new Dictionary<int, DateTime>();
        }

        /// <summary>
        /// 评估战斗情况
        /// </summary>
        public CombatDecision EvaluateCombatSituation(Troop troop)
        {
            try
            {
                if (troop?.Leader == null)
                {
                    return new CombatDecision
                    {
                        State = CombatState.Idle,
                        TargetPosition = troop?.Position ?? Point.Zero,
                        Reason = "无指挥官",
                        Confidence = 0f
                    };
                }

                // 检查是否需要重新评估
                if (_lastDecisionTime.ContainsKey(troop.ID))
                {
                    var timeSinceLastDecision = DateTime.Now - _lastDecisionTime[troop.ID];
                    if (timeSinceLastDecision.TotalSeconds < 30) // 30秒内不重复评估
                    {
                        return _combatDecisions.ContainsKey(troop.ID) 
                            ? _combatDecisions[troop.ID] 
                            : CreateIdleDecision(troop);
                    }
                }

                var decision = AnalyzeCombatSituation(troop);
                
                // 缓存决策
                _combatDecisions[troop.ID] = decision;
                _lastDecisionTime[troop.ID] = DateTime.Now;

                return decision;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EvaluateCombatSituation] 错误: {ex.Message}");
                return CreateIdleDecision(troop);
            }
        }

        /// <summary>
        /// 分析战斗情况
        /// </summary>
        private CombatDecision AnalyzeCombatSituation(Troop troop)
        {
            try
            {
                // 1. 检查周围敌军
                var nearbyEnemies = FindNearbyEnemies(troop, 5);
                
                if (nearbyEnemies.Count == 0)
                {
                    // 没有敌军，进行侦察或推进
                    return CreateScoutingDecision(troop);
                }

                // 2. 计算战力对比
                float ourStrength = troop.FightingForce;

                // [统一逻辑] 高价值精锐（虎豹骑/名将）在战力评估时获得加成，避免过早判定为劣势而撤退
                bool isHighValue = false;
                if (troop.Army != null && troop.Army.Kind != null)
                {
                    if (troop.Army.Kind.CreateCost >= 800 || troop.Army.Kind.RecruitLimit > 0 || troop.Army.Experience >= 300) 
                        isHighValue = true;
                }
                if (!isHighValue && troop.Leader != null)
                {
                    if (troop.Leader.Merit >= 2000 || troop.Leader.Strength >= 80 || troop.Leader.Command >= 80)
                        isHighValue = true;
                }

                if (isHighValue)
                {
                    ourStrength *= 2.0f; // 精锐部队战力评估翻倍，体现"以一当十"的战斗意志
                }

                float enemyStrength = nearbyEnemies.Sum(e => e.FightingForce);
                float strengthRatio = ourStrength / Math.Max(enemyStrength, 1f);

                // 3. 根据战力对比决定行动
                if (strengthRatio > 1.5f)
                {
                    // 我方优势明显，主动攻击
                    // 🔥 安全修复：避免InvalidOperationException
                    var orderedEnemies = nearbyEnemies.OrderBy(e => 
                        Session.Current.Scenario.GetSimpleDistance(troop.Position, e.Position));
                    var target = orderedEnemies.FirstOrDefault();
                    if (target != null)
                    {
                        return CreateAttackDecision(troop, target, strengthRatio);
                    }
                    else
                    {
                        // 如果没有目标，返回侦察决策
                        return CreateScoutingDecision(troop);
                    }
                }
                else if (strengthRatio > 0.8f)
                {
                    // 势均力敌，谨慎推进
                    return CreateAdvancingDecision(troop, nearbyEnemies);
                }
                else if (strengthRatio > 0.4f)
                {
                    // 略处劣势，防御为主
                    return CreateDefendingDecision(troop, nearbyEnemies);
                }
                else
                {
                    // 明显劣势，考虑撤退
                    return CreateRetreatDecision(troop, nearbyEnemies);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyzeCombatSituation] 错误: {ex.Message}");
                return CreateIdleDecision(troop);
            }
        }

        /// <summary>
        /// 寻找附近敌军
        /// </summary>
        private List<Troop> FindNearbyEnemies(Troop troop, int range)
        {
            try
            {
                var enemies = new List<Troop>();
                var allTroops = Session.Current.Scenario.Troops.GetList();

                foreach (var obj in allTroops)
                {
                    if (!(obj is Troop otherTroop) || otherTroop == troop || otherTroop.Destroyed) continue;
                    if (otherTroop.BelongedFaction == null || troop.BelongedFaction == null) continue;

                    // 检查是否为敌军
                    if (!troop.BelongedFaction.IsFriendly(otherTroop.BelongedFaction))
                    {
                        float distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, otherTroop.Position);
                        if (distance <= range)
                        {
                            enemies.Add(otherTroop);
                        }
                    }
                }

                return enemies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearbyEnemies] 错误: {ex.Message}");
                return new List<Troop>();
            }
        }

        /// <summary>
        /// 创建空闲决策
        /// </summary>
        private CombatDecision CreateIdleDecision(Troop troop)
        {
            return new CombatDecision
            {
                State = CombatState.Idle,
                TargetPosition = troop.Position,
                Reason = "无战斗目标",
                Confidence = 1.0f
            };
        }

        /// <summary>
        /// 创建侦察决策
        /// </summary>
        private CombatDecision CreateScoutingDecision(Troop troop)
        {
            // 选择一个侦察方向
            var scoutDirection = new Point(
                troop.Position.X + GameObject.Random(21) - 10,
                troop.Position.Y + GameObject.Random(21) - 10
            );

            return new CombatDecision
            {
                State = CombatState.Scouting,
                TargetPosition = scoutDirection,
                Reason = "侦察周围区域",
                Confidence = 0.7f
            };
        }

        /// <summary>
        /// 创建攻击决策
        /// </summary>
        private CombatDecision CreateAttackDecision(Troop troop, Troop target, float strengthRatio)
        {
            return new CombatDecision
            {
                State = CombatState.Attacking,
                TargetPosition = target.Position,
                TargetTroop = target,
                Reason = $"战力优势 {strengthRatio:F1}:1，主动攻击",
                Confidence = Math.Min(0.9f, strengthRatio * 0.5f)
            };
        }

        /// <summary>
        /// 创建推进决策
        /// </summary>
        private CombatDecision CreateAdvancingDecision(Troop troop, List<Troop> enemies)
        {
            // 选择最弱的敌军作为目标
            // 🔥 安全修复：避免InvalidOperationException
            var orderedEnemies = enemies.OrderBy(e => e.FightingForce);
            var weakestEnemy = orderedEnemies.FirstOrDefault();
            
            if (weakestEnemy == null)
            {
                // 如果没有敌军，返回防御决策
                return new CombatDecision
                {
                    Type = CombatDecisionType.Defend,
                    Priority = 0.3f,
                    Reason = "没有可攻击的敌军目标"
                };
            }
            
            return new CombatDecision
            {
                State = CombatState.Advancing,
                TargetPosition = weakestEnemy.Position,
                TargetTroop = weakestEnemy,
                Reason = "势均力敌，谨慎推进",
                Confidence = 0.6f
            };
        }

        /// <summary>
        /// 创建防御决策
        /// </summary>
        private CombatDecision CreateDefendingDecision(Troop troop, List<Troop> enemies)
        {
            // 寻找最近的友方建筑作为防御点
            var defensivePosition = FindDefensivePosition(troop);
            
            return new CombatDecision
            {
                State = CombatState.Defending,
                TargetPosition = defensivePosition,
                Reason = "战力劣势，防御为主",
                Confidence = 0.5f
            };
        }

        /// <summary>
        /// 创建撤退决策
        /// </summary>
        private CombatDecision CreateRetreatDecision(Troop troop, List<Troop> enemies)
        {
            // 寻找撤退路线
            var retreatPosition = FindRetreatPosition(troop, enemies);
            
            return new CombatDecision
            {
                State = CombatState.Retreating,
                TargetPosition = retreatPosition,
                Reason = "战力悬殊，战略撤退",
                Confidence = 0.8f
            };
        }

        /// <summary>
        /// 寻找防御位置
        /// </summary>
        private Point FindDefensivePosition(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return troop?.Position ?? Point.Zero;

                // 寻找最近的己方建筑
                var ownArchs = troop.BelongedFaction.Architectures.GetList();
                if (ownArchs.Count == 0) return troop.Position;

                Architecture nearest = null;
                float minDistance = float.MaxValue;
                
                foreach (var obj in ownArchs)
                {
                    if (obj is Architecture arch)
                    {
                        float distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, arch.Position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearest = arch;
                        }
                    }
                }

                return nearest?.Position ?? troop.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindDefensivePosition] 错误: {ex.Message}");
                return troop?.Position ?? Point.Zero;
            }
        }

        /// <summary>
        /// 寻找撤退位置
        /// </summary>
        private Point FindRetreatPosition(Troop troop, List<Troop> enemies)
        {
            try
            {
                if (troop == null) return Point.Zero;

                // 计算敌军的平均位置
                var enemyCenter = new Point(
                    (int)enemies.Average(e => e.Position.X),
                    (int)enemies.Average(e => e.Position.Y)
                );

                // 向相反方向撤退
                var retreatDirection = new Point(
                    troop.Position.X - enemyCenter.X,
                    troop.Position.Y - enemyCenter.Y
                );

                // 标准化方向并扩大距离
                var magnitude = Math.Max(Math.Abs(retreatDirection.X), Math.Abs(retreatDirection.Y));
                if (magnitude > 0)
                {
                    retreatDirection = new Point(
                        retreatDirection.X * 5 / magnitude,
                        retreatDirection.Y * 5 / magnitude
                    );
                }

                var retreatPos = new Point(
                    troop.Position.X + retreatDirection.X,
                    troop.Position.Y + retreatDirection.Y
                );

                // 确保在地图范围内
                retreatPos = new Point(
                    Math.Max(0, Math.Min(Session.Current.Scenario.ScenarioMap.MapDimensions.X - 1, retreatPos.X)),
                    Math.Max(0, Math.Min(Session.Current.Scenario.ScenarioMap.MapDimensions.Y - 1, retreatPos.Y))
                );

                return retreatPos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindRetreatPosition] 错误: {ex.Message}");
                return troop?.Position ?? Point.Zero;
            }
        }

        /// <summary>
        /// 获取战斗状态描述
        /// </summary>
        public string GetCombatStateDescription(CombatState state)
        {
            switch (state)
            {
                case CombatState.Idle: return "待命";
                case CombatState.Scouting: return "侦察";
                case CombatState.Advancing: return "推进";
                case CombatState.Attacking: return "攻击";
                case CombatState.Defending: return "防御";
                case CombatState.Retreating: return "撤退";
                case CombatState.Regrouping: return "重组";
                default: return "未知状态";
            }
        }

        /// <summary>
        /// 清理过期决策
        /// </summary>
        public void CleanupOldDecisions()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddMinutes(-5); // 清理5分钟前的决策
                var expiredKeys = _combatDecisions
                    .Where(kvp => kvp.Value.DecisionTime < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _combatDecisions.Remove(key);
                    _lastDecisionTime.Remove(key);
                }

                if (expiredKeys.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CombatAISystem] 清理了 {expiredKeys.Count} 个过期决策");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupOldDecisions] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理战斗AI
        /// </summary>
        public void ProcessCombatAI(Faction faction)
        {
            try
            {
                if (faction == null) return;
                
                // 为每个部队制定战斗策略
                foreach (Troop troop in faction.Troops)
                {
                    if (troop.Destroyed || !troop.Controllable) continue;
                    
                    string strategy = DetermineCombatStrategy(troop);
                    _combatStrategies[troop.ID] = strategy;
                }
                
                System.Diagnostics.Debug.WriteLine($"[CombatAISystem] 为势力 {faction.Name} 的 {faction.TroopCount} 支部队制定了战斗策略");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAISystem] 处理战斗AI时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 确定战斗策略
        /// </summary>
        private string DetermineCombatStrategy(Troop troop)
        {
            try
            {
                if (troop.FightingForce > 50000)
                {
                    return "主动攻击";
                }
                else if (troop.FightingForce > 20000)
                {
                    return "谨慎推进";
                }
                else
                {
                    return "防御为主";
                }
            }
            catch
            {
                return "默认策略";
            }
        }
        
        /// <summary>
        /// 获取战斗策略
        /// </summary>
        public string GetCombatStrategy(int troopId)
        {
            return _combatStrategies.ContainsKey(troopId) ? _combatStrategies[troopId] : "无策略";
        }
        
        /// <summary>
        /// 清除战斗策略
        /// </summary>
        public void ClearStrategies()
        {
            _combatStrategies.Clear();
            _combatDecisions.Clear();
            _lastDecisionTime.Clear();
        }

        /// <summary>
        /// 获取战斗AI统计信息
        /// </summary>
        public string GetCombatStats()
        {
            try
            {
                var activeDecisions = _combatDecisions.Count;
                var recentDecisions = _combatDecisions.Values
                    .Where(d => (DateTime.Now - d.DecisionTime).TotalMinutes < 5)
                    .ToList();

                var stateGroups = recentDecisions
                    .GroupBy(d => d.State)
                    .ToDictionary(g => g.Key, g => g.Count());

                string stats = $"战斗AI统计:\n";
                stats += $"活跃决策: {activeDecisions}\n";
                stats += $"近期决策: {recentDecisions.Count}\n";
                
                foreach (var stateGroup in stateGroups)
                {
                    stats += $"{GetCombatStateDescription(stateGroup.Key)}: {stateGroup.Value}\n";
                }

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCombatStats] 错误: {ex.Message}");
                return "获取统计信息失败";
            }
        }
    }
}