using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// AI学习系统 - 让AI从玩家行为中学习并适应
    /// </summary>
    public class AILearningSystem
    {
        public static AILearningSystem Instance { get; private set; }

        // 玩家行为模式
        public enum PlayerBehaviorPattern
        {
            Aggressive,     // 攻击性
            Defensive,      // 防御性
            Economic,       // 经济导向
            Diplomatic,     // 外交导向
            Opportunistic,  // 机会主义
            Unpredictable   // 不可预测
        }

        // 学习数据结构
        public class LearningData
        {
            public Dictionary<string, float> ActionFrequency { get; set; } = new Dictionary<string, float>();
            public Dictionary<string, float> SuccessRate { get; set; } = new Dictionary<string, float>();
            public List<GameEvent> RecentEvents { get; set; } = new List<GameEvent>();
            public PlayerBehaviorPattern DetectedPattern { get; set; } = PlayerBehaviorPattern.Unpredictable;
            public DateTime LastAnalysis { get; set; } = DateTime.Now;
        }

        // 游戏事件记录
        public class GameEvent
        {
            public string EventType { get; set; }
            public DateTime Timestamp { get; set; }
            public Point Location { get; set; }
            public string Details { get; set; }
            public bool Success { get; set; }
            public Faction Faction { get; set; }
        }

        // AI适应策略
        public class AdaptationStrategy
        {
            public string Name { get; set; }
            public Dictionary<string, float> CounterWeights { get; set; } = new Dictionary<string, float>();
            public string Description { get; set; }
        }

        private Dictionary<int, LearningData> _factionLearningData;
        private Dictionary<PlayerBehaviorPattern, AdaptationStrategy> _adaptationStrategies;
        private List<GameEvent> _globalEventHistory;
        private int _maxEventHistory = 1000;
        private int _analysisInterval = 60000; // 60秒分析一次

        public AILearningSystem()
        {
            Instance = this;
            _factionLearningData = new Dictionary<int, LearningData>();
            _globalEventHistory = new List<GameEvent>();
            InitializeAdaptationStrategies();
        }

        /// <summary>
        /// 初始化适应策略
        /// </summary>
        private void InitializeAdaptationStrategies()
        {
            _adaptationStrategies = new Dictionary<PlayerBehaviorPattern, AdaptationStrategy>
            {
                [PlayerBehaviorPattern.Aggressive] = new AdaptationStrategy
                {
                    Name = "反攻击策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["DefensiveBonus"] = 1.5f,
                        ["ThreatAvoidance"] = 1.3f,
                        ["AllySupport"] = 1.4f,
                        ["TerrainAdvantage"] = 1.2f
                    },
                    Description = "加强防御，利用地形优势，寻求友军支援"
                },
                [PlayerBehaviorPattern.Defensive] = new AdaptationStrategy
                {
                    Name = "破防策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["AggressiveBonus"] = 1.4f,
                        ["FlankingBonus"] = 1.6f,
                        ["EconomicPressure"] = 1.3f,
                        ["MultiDirectionalAttack"] = 1.5f
                    },
                    Description = "多方向进攻，经济施压，侧翼包抄"
                },
                [PlayerBehaviorPattern.Economic] = new AdaptationStrategy
                {
                    Name = "经济干扰策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["EconomicTargeting"] = 1.8f,
                        ["TradeRouteDisruption"] = 1.6f,
                        ["ResourceDenial"] = 1.4f,
                        ["FastExpansion"] = 1.3f
                    },
                    Description = "破坏贸易路线，抢夺资源点，快速扩张"
                },
                [PlayerBehaviorPattern.Diplomatic] = new AdaptationStrategy
                {
                    Name = "外交反制策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["AllianceBreaking"] = 1.7f,
                        ["CounterDiplomacy"] = 1.5f,
                        ["IsolationTactics"] = 1.4f,
                        ["DirectPressure"] = 1.2f
                    },
                    Description = "破坏联盟，反向外交，孤立对手"
                },
                [PlayerBehaviorPattern.Opportunistic] = new AdaptationStrategy
                {
                    Name = "预防策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["Predictability"] = 0.7f,
                        ["SecurityFocus"] = 1.4f,
                        ["ConservativePlay"] = 1.3f,
                        ["InformationDenial"] = 1.5f
                    },
                    Description = "减少可预测性，加强安全防护，保守行动"
                }
            };
        }

        /// <summary>
        /// 记录游戏事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="location">事件位置</param>
        /// <param name="details">事件详情</param>
        /// <param name="success">是否成功</param>
        /// <param name="faction">相关势力</param>
        public void RecordEvent(string eventType, Point location, string details, bool success, Faction faction)
        {
            try
            {
                var gameEvent = new GameEvent
                {
                    EventType = eventType,
                    Timestamp = DateTime.Now,
                    Location = location,
                    Details = details,
                    Success = success,
                    Faction = faction
                };

                // 添加到全局历史
                _globalEventHistory.Add(gameEvent);
                
                // 限制历史记录数量
                if (_globalEventHistory.Count > _maxEventHistory)
                {
                    _globalEventHistory.RemoveAt(0);
                }

                // 添加到势力学习数据
                if (faction != null)
                {
                    if (!_factionLearningData.ContainsKey(faction.ID))
                    {
                        _factionLearningData[faction.ID] = new LearningData();
                    }

                    var learningData = _factionLearningData[faction.ID];
                    learningData.RecentEvents.Add(gameEvent);

                    // 更新行为频率
                    if (!learningData.ActionFrequency.ContainsKey(eventType))
                    {
                        learningData.ActionFrequency[eventType] = 0;
                    }
                    learningData.ActionFrequency[eventType]++;

                    // 更新成功率
                    if (!learningData.SuccessRate.ContainsKey(eventType))
                    {
                        learningData.SuccessRate[eventType] = 0;
                    }
                    
                    var totalEvents = learningData.RecentEvents.Count(e => e.EventType == eventType);
                    var successfulEvents = learningData.RecentEvents.Count(e => e.EventType == eventType && e.Success);
                    learningData.SuccessRate[eventType] = totalEvents > 0 ? (float)successfulEvents / totalEvents : 0;

                    // 限制事件历史
                    if (learningData.RecentEvents.Count > 200)
                    {
                        learningData.RecentEvents.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 记录事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析玩家行为模式
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <returns>检测到的行为模式</returns>
        public PlayerBehaviorPattern AnalyzePlayerBehavior(Faction playerFaction)
        {
            try
            {
                if (playerFaction == null || !_factionLearningData.ContainsKey(playerFaction.ID))
                {
                    return PlayerBehaviorPattern.Unpredictable;
                }

                var learningData = _factionLearningData[playerFaction.ID];
                
                // 检查是否需要重新分析
                if ((DateTime.Now - learningData.LastAnalysis).TotalMilliseconds < _analysisInterval)
                {
                    return learningData.DetectedPattern;
                }

                var recentEvents = learningData.RecentEvents
                    .Where(e => (DateTime.Now - e.Timestamp).TotalMinutes < 30)
                    .ToList();

                if (recentEvents.Count < 10)
                {
                    return PlayerBehaviorPattern.Unpredictable;
                }

                // 分析行为模式
                var pattern = DeterminePattern(recentEvents, learningData);
                learningData.DetectedPattern = pattern;
                learningData.LastAnalysis = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"[AILearning] 检测到玩家行为模式: {pattern}");
                return pattern;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 分析行为模式失败: {ex.Message}");
                return PlayerBehaviorPattern.Unpredictable;
            }
        }

        /// <summary>
        /// 确定行为模式
        /// </summary>
        private PlayerBehaviorPattern DeterminePattern(List<GameEvent> events, LearningData learningData)
        {
            var scores = new Dictionary<PlayerBehaviorPattern, float>();

            // 攻击性评分
            var attackEvents = events.Count(e => e.EventType.Contains("Attack") || e.EventType.Contains("Assault"));
            var totalEvents = events.Count;
            scores[PlayerBehaviorPattern.Aggressive] = (float)attackEvents / totalEvents * 100;

            // 防御性评分
            var defenseEvents = events.Count(e => e.EventType.Contains("Defend") || e.EventType.Contains("Fortify"));
            scores[PlayerBehaviorPattern.Defensive] = (float)defenseEvents / totalEvents * 100;

            // 经济导向评分
            var economicEvents = events.Count(e => e.EventType.Contains("Build") || e.EventType.Contains("Trade") || e.EventType.Contains("Develop"));
            scores[PlayerBehaviorPattern.Economic] = (float)economicEvents / totalEvents * 100;

            // 外交导向评分
            var diplomaticEvents = events.Count(e => e.EventType.Contains("Diplomacy") || e.EventType.Contains("Alliance"));
            scores[PlayerBehaviorPattern.Diplomatic] = (float)diplomaticEvents / totalEvents * 100;

            // 机会主义评分
            var opportunisticIndicators = CalculateOpportunisticScore(events);
            scores[PlayerBehaviorPattern.Opportunistic] = opportunisticIndicators;

            // 返回得分最高的模式
            var maxScore = scores.Values.Max();
            if (maxScore < 20) // 如果所有得分都很低，认为是不可预测的
            {
                return PlayerBehaviorPattern.Unpredictable;
            }

            return scores.First(kvp => kvp.Value == maxScore).Key;
        }

        /// <summary>
        /// 计算机会主义评分
        /// </summary>
        private float CalculateOpportunisticScore(List<GameEvent> events)
        {
            float score = 0;

            // 检查是否经常在敌人虚弱时攻击
            var attackEvents = events.Where(e => e.EventType.Contains("Attack")).ToList();
            foreach (var attack in attackEvents)
            {
                // 这里可以添加更复杂的逻辑来判断是否是机会主义攻击
                if (attack.Success)
                {
                    score += 10;
                }
            }

            // 检查行为的多样性
            var eventTypes = events.Select(e => e.EventType).Distinct().Count();
            score += eventTypes * 5;

            return Math.Min(score, 100);
        }

        /// <summary>
        /// 获取针对玩家的适应策略
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <returns>适应策略</returns>
        public AdaptationStrategy GetAdaptationStrategy(Faction playerFaction)
        {
            var pattern = AnalyzePlayerBehavior(playerFaction);
            
            if (_adaptationStrategies.ContainsKey(pattern))
            {
                return _adaptationStrategies[pattern];
            }

            // 默认策略
            return new AdaptationStrategy
            {
                Name = "标准策略",
                CounterWeights = new Dictionary<string, float>(),
                Description = "使用标准AI行为"
            };
        }

        /// <summary>
        /// 应用学习结果到AI决策权重
        /// </summary>
        /// <param name="baseWeights">基础权重</param>
        /// <param name="playerFaction">玩家势力</param>
        /// <returns>调整后的权重</returns>
        public AIDecisionManager.AIDecisionWeights ApplyLearning(
            AIDecisionManager.AIDecisionWeights baseWeights, 
            Faction playerFaction)
        {
            try
            {
                var strategy = GetAdaptationStrategy(playerFaction);
                var adjustedWeights = new AIDecisionManager.AIDecisionWeights
                {
                    DistanceToGoal = baseWeights.DistanceToGoal,
                    ThreatAvoidance = baseWeights.ThreatAvoidance,
                    TerrainAdvantage = baseWeights.TerrainAdvantage,
                    UnknownAreaPenalty = baseWeights.UnknownAreaPenalty,
                    EnemyProximity = baseWeights.EnemyProximity,
                    AllySupport = baseWeights.AllySupport
                };

                // 应用适应策略的权重调整
                foreach (var adjustment in strategy.CounterWeights)
                {
                    switch (adjustment.Key)
                    {
                        case "DefensiveBonus":
                            adjustedWeights.ThreatAvoidance *= adjustment.Value;
                            break;
                        case "AggressiveBonus":
                            adjustedWeights.EnemyProximity *= -adjustment.Value; // 负值表示倾向接近
                            break;
                        case "TerrainAdvantage":
                            adjustedWeights.TerrainAdvantage *= adjustment.Value;
                            break;
                        case "AllySupport":
                            adjustedWeights.AllySupport *= adjustment.Value;
                            break;
                    }
                }

                return adjustedWeights;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 应用学习失败: {ex.Message}");
                return baseWeights;
            }
        }

        /// <summary>
        /// 获取学习统计信息
        /// </summary>
        /// <param name="factionId">势力ID</param>
        /// <returns>统计信息</returns>
        public string GetLearningStats(int factionId)
        {
            if (!_factionLearningData.ContainsKey(factionId))
            {
                return "无学习数据";
            }

            var data = _factionLearningData[factionId];
            var stats = $"行为模式: {data.DetectedPattern}\n";
            stats += $"事件总数: {data.RecentEvents.Count}\n";
            stats += $"最后分析: {data.LastAnalysis:HH:mm:ss}\n";
            
            if (data.ActionFrequency.Count > 0)
            {
                stats += "行为频率:\n";
                foreach (var action in data.ActionFrequency.Take(5))
                {
                    stats += $"  {action.Key}: {action.Value}\n";
                }
            }

            return stats;
        }

        /// <summary>
        /// 清理过期数据
        /// </summary>
        public void CleanupOldData()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-2);
                
                // 清理全局事件历史
                _globalEventHistory.RemoveAll(e => e.Timestamp < cutoffTime);

                // 清理各势力的学习数据
                foreach (var learningData in _factionLearningData.Values)
                {
                    learningData.RecentEvents.RemoveAll(e => e.Timestamp < cutoffTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 清理数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置学习数据
        /// </summary>
        public void Reset()
        {
            _factionLearningData.Clear();
            _globalEventHistory.Clear();
        }
    }
}