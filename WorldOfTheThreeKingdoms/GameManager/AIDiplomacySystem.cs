using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// AI外交系统 - 智能外交决策和关系管理
    /// </summary>
    public class AIDiplomacySystem
    {
        public static AIDiplomacySystem Instance { get; private set; }

        // 外交关系类型
        public enum DiplomaticRelation
        {
            Hostile = -3,       // 敌对
            Unfriendly = -2,    // 不友好
            Neutral = 0,        // 中立
            Friendly = 1,       // 友好
            Allied = 2,         // 同盟
            Vassal = 3          // 附庸
        }

        // 外交行动类型
        public enum DiplomaticAction
        {
            DeclareWar,         // 宣战
            OfferPeace,         // 求和
            ProposeAlliance,    // 提议同盟
            BreakAlliance,      // 破坏同盟
            RequestTribute,     // 要求进贡
            OfferTribute,       // 提供进贡
            TradeAgreement,     // 贸易协定
            NonAggressionPact,  // 互不侵犯条约
            MarriageAlliance,   // 联姻
            TerritoryExchange,  // 领土交换
            MilitarySupport,    // 军事支援
            Threaten,           // 威胁
            Compliment          // 恭维
        }

        // 外交关系数据
        public class DiplomaticRelationship
        {
            public int FactionA { get; set; }
            public int FactionB { get; set; }
            public DiplomaticRelation Relation { get; set; }
            public float TrustLevel { get; set; } = 0.5f;           // 信任度 (0-1)
            public float FearLevel { get; set; } = 0.0f;            // 恐惧度 (0-1)
            public float RespectLevel { get; set; } = 0.5f;         // 尊重度 (0-1)
            public List<DiplomaticEvent> History { get; set; } = new List<DiplomaticEvent>();
            public DateTime LastInteraction { get; set; } = DateTime.Now;
            public Dictionary<string, float> Agreements { get; set; } = new Dictionary<string, float>();
        }

        // 外交事件
        public class DiplomaticEvent
        {
            public DiplomaticAction Action { get; set; }
            public DateTime Timestamp { get; set; }
            public string Details { get; set; }
            public bool Success { get; set; }
            public float ImpactOnTrust { get; set; }
            public float ImpactOnFear { get; set; }
            public float ImpactOnRespect { get; set; }
        }

        // 外交策略
        public class DiplomaticStrategy
        {
            public string Name { get; set; }
            public Dictionary<DiplomaticAction, float> ActionPriorities { get; set; } = new Dictionary<DiplomaticAction, float>();
            public float AggressionLevel { get; set; } = 0.5f;      // 攻击性 (0-1)
            public float CooperationLevel { get; set; } = 0.5f;     // 合作性 (0-1)
            public float TrustworthinessLevel { get; set; } = 0.5f; // 可信度 (0-1)
        }

        private Dictionary<string, DiplomaticRelationship> _relationships;
        private Dictionary<int, DiplomaticStrategy> _factionStrategies;
        private List<DiplomaticEvent> _globalDiplomaticHistory;
        private DateTime _lastUpdate;
        private int _updateInterval = 30000; // 30秒更新一次

        public AIDiplomacySystem()
        {
            Instance = this;
            _relationships = new Dictionary<string, DiplomaticRelationship>();
            _factionStrategies = new Dictionary<int, DiplomaticStrategy>();
            _globalDiplomaticHistory = new List<DiplomaticEvent>();
            _lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 初始化势力间的外交关系
        /// </summary>
        /// <param name="factions">所有势力</param>
        public void InitializeDiplomacy(List<Faction> factions)
        {
            try
            {
                foreach (var factionA in factions)
                {
                    foreach (var factionB in factions)
                    {
                        if (factionA.ID >= factionB.ID) continue; // 避免重复

                        string relationKey = GetRelationKey(factionA.ID, factionB.ID);
                        if (!_relationships.ContainsKey(relationKey))
                        {
                            _relationships[relationKey] = new DiplomaticRelationship
                            {
                                FactionA = factionA.ID,
                                FactionB = factionB.ID,
                                Relation = DetermineInitialRelation(factionA, factionB),
                                TrustLevel = 0.5f,
                                FearLevel = CalculateInitialFear(factionA, factionB),
                                RespectLevel = CalculateInitialRespect(factionA, factionB)
                            };
                        }
                    }

                    // 初始化势力的外交策略
                    if (!_factionStrategies.ContainsKey(factionA.ID))
                    {
                        _factionStrategies[factionA.ID] = DetermineStrategy(factionA);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 初始化了 {_relationships.Count} 个外交关系");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 初始化外交失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 确定初始外交关系
        /// </summary>
        private DiplomaticRelation DetermineInitialRelation(Faction factionA, Faction factionB)
        {
            // 基于历史关系、地理位置、实力对比等因素
            // 这里简化为随机生成，实际可以根据游戏设定
            
            var random = new Random();
            var relations = Enum.GetValues(typeof(DiplomaticRelation)).Cast<DiplomaticRelation>().ToArray();
            
            // 倾向于中立关系
            if (random.NextDouble() < 0.6)
            {
                return DiplomaticRelation.Neutral;
            }
            
            return relations[random.Next(relations.Length)];
        }

        /// <summary>
        /// 计算初始恐惧度
        /// </summary>
        private float CalculateInitialFear(Faction factionA, Faction factionB)
        {
            try
            {
                // 基于实力对比
                float strengthA = CalculateFactionStrength(factionA);
                float strengthB = CalculateFactionStrength(factionB);
                
                if (strengthA == 0) return 0;
                
                float strengthRatio = strengthB / strengthA;
                return Math.Min(1.0f, Math.Max(0.0f, (strengthRatio - 0.5f) * 2.0f));
            }
            catch
            {
                return 0.3f; // 默认值
            }
        }

        /// <summary>
        /// 计算初始尊重度
        /// </summary>
        private float CalculateInitialRespect(Faction factionA, Faction factionB)
        {
            try
            {
                // 基于声望、历史成就等
                float reputationA = factionA.Reputation;
                float reputationB = factionB.Reputation;
                
                return Math.Min(1.0f, Math.Max(0.0f, reputationB / Math.Max(1, reputationA)));
            }
            catch
            {
                return 0.5f; // 默认值
            }
        }

        /// <summary>
        /// 计算势力实力
        /// </summary>
        private float CalculateFactionStrength(Faction faction)
        {
            try
            {
                float strength = 0;
                
                // 军事实力
                if (faction.Troops != null)
                {
                    strength += faction.Troops.GetList().Sum(t => t.FightingForce) * 0.4f;
                }
                
                // 经济实力
                if (faction.ArchitectureList != null)
                {
                    strength += faction.ArchitectureList.GetList().Sum(a => a.Fund + a.Food) * 0.0001f;
                }
                
                // 领土规模
                if (faction.ArchitectureList != null)
                {
                    strength += faction.ArchitectureList.Count * 1000;
                }
                
                return strength;
            }
            catch
            {
                return 1000; // 默认值
            }
        }

        /// <summary>
        /// 确定势力的外交策略
        /// </summary>
        private DiplomaticStrategy DetermineStrategy(Faction faction)
        {
            try
            {
                var strategy = new DiplomaticStrategy();
                
                if (faction.Leader != null)
                {
                    var leader = faction.Leader;
                    
                    // 基于领袖性格确定策略
                    strategy.AggressionLevel = Math.Min(1.0f, leader.Braveness / 10.0f);
                    strategy.CooperationLevel = Math.Min(1.0f, leader.Calmness / 10.0f);
                    strategy.TrustworthinessLevel = Math.Min(1.0f, (leader.Politics + leader.Intelligence) / 20.0f);
                    
                    // 设置行动优先级
                    if (leader.Braveness > 7)
                    {
                        strategy.Name = "攻击性外交";
                        strategy.ActionPriorities[DiplomaticAction.DeclareWar] = 0.8f;
                        strategy.ActionPriorities[DiplomaticAction.Threaten] = 0.7f;
                        strategy.ActionPriorities[DiplomaticAction.RequestTribute] = 0.6f;
                    }
                    else if (leader.Calmness > 7)
                    {
                        strategy.Name = "和平外交";
                        strategy.ActionPriorities[DiplomaticAction.ProposeAlliance] = 0.8f;
                        strategy.ActionPriorities[DiplomaticAction.TradeAgreement] = 0.7f;
                        strategy.ActionPriorities[DiplomaticAction.NonAggressionPact] = 0.6f;
                    }
                    else if (leader.Intelligence > 7)
                    {
                        strategy.Name = "智谋外交";
                        strategy.ActionPriorities[DiplomaticAction.MarriageAlliance] = 0.7f;
                        strategy.ActionPriorities[DiplomaticAction.TerritoryExchange] = 0.6f;
                        strategy.ActionPriorities[DiplomaticAction.MilitarySupport] = 0.5f;
                    }
                    else
                    {
                        strategy.Name = "平衡外交";
                        foreach (var action in Enum.GetValues(typeof(DiplomaticAction)).Cast<DiplomaticAction>())
                        {
                            strategy.ActionPriorities[action] = 0.5f;
                        }
                    }
                }
                else
                {
                    strategy.Name = "默认外交";
                    strategy.AggressionLevel = 0.5f;
                    strategy.CooperationLevel = 0.5f;
                    strategy.TrustworthinessLevel = 0.5f;
                }
                
                return strategy;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 确定策略失败: {ex.Message}");
                return new DiplomaticStrategy { Name = "默认外交" };
            }
        }

        /// <summary>
        /// 更新外交系统
        /// </summary>
        public void UpdateDiplomacy()
        {
            try
            {
                var now = DateTime.Now;
                if ((now - _lastUpdate).TotalMilliseconds < _updateInterval)
                {
                    return;
                }

                // 更新所有外交关系
                foreach (var relationship in _relationships.Values)
                {
                    UpdateRelationship(relationship);
                }

                // 执行AI外交决策
                ExecuteDiplomaticDecisions();

                _lastUpdate = now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 更新外交失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新外交关系
        /// </summary>
        private void UpdateRelationship(DiplomaticRelationship relationship)
        {
            try
            {
                // 时间衰减 - 关系会随时间自然变化
                var timeSinceLastInteraction = DateTime.Now - relationship.LastInteraction;
                if (timeSinceLastInteraction.TotalDays > 30)
                {
                    // 长时间没有互动，关系趋向中立
                    relationship.TrustLevel = relationship.TrustLevel * 0.99f + 0.5f * 0.01f;
                    relationship.FearLevel *= 0.99f;
                    relationship.RespectLevel = relationship.RespectLevel * 0.99f + 0.5f * 0.01f;
                }

                // 根据当前数值调整关系等级
                UpdateRelationLevel(relationship);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 更新关系失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新关系等级
        /// </summary>
        private void UpdateRelationLevel(DiplomaticRelationship relationship)
        {
            float overallScore = relationship.TrustLevel + relationship.RespectLevel - relationship.FearLevel;
            
            if (overallScore > 1.5f)
            {
                relationship.Relation = DiplomaticRelation.Allied;
            }
            else if (overallScore > 1.0f)
            {
                relationship.Relation = DiplomaticRelation.Friendly;
            }
            else if (overallScore > -0.5f)
            {
                relationship.Relation = DiplomaticRelation.Neutral;
            }
            else if (overallScore > -1.0f)
            {
                relationship.Relation = DiplomaticRelation.Unfriendly;
            }
            else
            {
                relationship.Relation = DiplomaticRelation.Hostile;
            }
        }

        /// <summary>
        /// 执行AI外交决策
        /// </summary>
        private void ExecuteDiplomaticDecisions()
        {
            try
            {
                foreach (var factionStrategy in _factionStrategies)
                {
                    var factionId = factionStrategy.Key;
                    var strategy = factionStrategy.Value;
                    
                    // 为每个势力执行外交决策
                    ExecuteFactionDiplomacy(factionId, strategy);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 执行外交决策失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行单个势力的外交决策
        /// </summary>
        private void ExecuteFactionDiplomacy(int factionId, DiplomaticStrategy strategy)
        {
            try
            {
                // 获取该势力与其他势力的关系
                var relationships = GetFactionRelationships(factionId);
                
                foreach (var relationship in relationships)
                {
                    var targetFactionId = relationship.FactionA == factionId ? relationship.FactionB : relationship.FactionA;
                    
                    // 决定是否执行外交行动
                    var action = DecideDiplomaticAction(factionId, targetFactionId, relationship, strategy);
                    
                    if (action.HasValue)
                    {
                        ExecuteDiplomaticAction(factionId, targetFactionId, action.Value, relationship);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 执行势力外交失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 决定外交行动
        /// </summary>
        private DiplomaticAction? DecideDiplomaticAction(int factionId, int targetFactionId, 
            DiplomaticRelationship relationship, DiplomaticStrategy strategy)
        {
            try
            {
                var random = new Random();
                
                // 基于策略和关系状态决定行动
                var possibleActions = new List<(DiplomaticAction action, float weight)>();
                
                foreach (var actionPriority in strategy.ActionPriorities)
                {
                    var action = actionPriority.Key;
                    var baseWeight = actionPriority.Value;
                    
                    // 根据当前关系调整权重
                    float adjustedWeight = AdjustActionWeight(action, relationship, baseWeight);
                    
                    if (adjustedWeight > 0.1f)
                    {
                        possibleActions.Add((action, adjustedWeight));
                    }
                }
                
                if (possibleActions.Count == 0) return null;
                
                // 随机选择行动（权重越高越容易被选中）
                float totalWeight = possibleActions.Sum(a => a.weight);
                float randomValue = (float)random.NextDouble() * totalWeight;
                
                float currentWeight = 0;
                foreach (var (action, weight) in possibleActions)
                {
                    currentWeight += weight;
                    if (randomValue <= currentWeight)
                    {
                        return action;
                    }
                }
                
                return possibleActions.First().action;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 决定外交行动失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 调整行动权重
        /// </summary>
        private float AdjustActionWeight(DiplomaticAction action, DiplomaticRelationship relationship, float baseWeight)
        {
            float weight = baseWeight;
            
            switch (action)
            {
                case DiplomaticAction.DeclareWar:
                    if (relationship.Relation >= DiplomaticRelation.Friendly) weight *= 0.1f;
                    if (relationship.FearLevel < 0.3f) weight *= 2.0f;
                    break;
                    
                case DiplomaticAction.ProposeAlliance:
                    if (relationship.Relation <= DiplomaticRelation.Unfriendly) weight *= 0.2f;
                    if (relationship.TrustLevel > 0.7f) weight *= 1.5f;
                    break;
                    
                case DiplomaticAction.OfferPeace:
                    if (relationship.Relation != DiplomaticRelation.Hostile) weight *= 0.1f;
                    break;
                    
                case DiplomaticAction.Threaten:
                    if (relationship.Relation >= DiplomaticRelation.Friendly) weight *= 0.3f;
                    if (relationship.FearLevel > 0.5f) weight *= 0.5f;
                    break;
            }
            
            return Math.Max(0, weight);
        }

        /// <summary>
        /// 执行外交行动
        /// </summary>
        private void ExecuteDiplomaticAction(int factionId, int targetFactionId, DiplomaticAction action, 
            DiplomaticRelationship relationship)
        {
            try
            {
                var diplomaticEvent = new DiplomaticEvent
                {
                    Action = action,
                    Timestamp = DateTime.Now,
                    Details = $"势力 {factionId} 对势力 {targetFactionId} 执行 {action}",
                    Success = DetermineActionSuccess(action, relationship)
                };

                // 计算行动对关系的影响
                CalculateActionImpact(action, diplomaticEvent, relationship);
                
                // 应用影响
                ApplyDiplomaticImpact(relationship, diplomaticEvent);
                
                // 记录事件
                relationship.History.Add(diplomaticEvent);
                _globalDiplomaticHistory.Add(diplomaticEvent);
                relationship.LastInteraction = DateTime.Now;
                
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] {diplomaticEvent.Details} - 成功: {diplomaticEvent.Success}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] 执行外交行动失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 确定行动成功率
        /// </summary>
        private bool DetermineActionSuccess(DiplomaticAction action, DiplomaticRelationship relationship)
        {
            var random = new Random();
            float successChance = 0.5f;
            
            switch (action)
            {
                case DiplomaticAction.ProposeAlliance:
                    successChance = relationship.TrustLevel * 0.8f + relationship.RespectLevel * 0.2f;
                    break;
                case DiplomaticAction.DeclareWar:
                    successChance = 0.9f; // 宣战通常会成功
                    break;
                case DiplomaticAction.OfferPeace:
                    successChance = (1.0f - relationship.FearLevel) * 0.6f + relationship.TrustLevel * 0.4f;
                    break;
                case DiplomaticAction.Threaten:
                    successChance = relationship.FearLevel * 0.7f + (1.0f - relationship.RespectLevel) * 0.3f;
                    break;
                default:
                    successChance = 0.6f;
                    break;
            }
            
            return random.NextDouble() < successChance;
        }

        /// <summary>
        /// 计算行动影响
        /// </summary>
        private void CalculateActionImpact(DiplomaticAction action, DiplomaticEvent diplomaticEvent, 
            DiplomaticRelationship relationship)
        {
            float trustImpact = 0;
            float fearImpact = 0;
            float respectImpact = 0;
            
            switch (action)
            {
                case DiplomaticAction.ProposeAlliance:
                    trustImpact = diplomaticEvent.Success ? 0.2f : -0.1f;
                    respectImpact = 0.1f;
                    break;
                    
                case DiplomaticAction.DeclareWar:
                    trustImpact = -0.5f;
                    fearImpact = 0.3f;
                    respectImpact = -0.2f;
                    break;
                    
                case DiplomaticAction.OfferPeace:
                    trustImpact = diplomaticEvent.Success ? 0.3f : 0.1f;
                    fearImpact = -0.2f;
                    break;
                    
                case DiplomaticAction.Threaten:
                    trustImpact = -0.2f;
                    fearImpact = diplomaticEvent.Success ? 0.4f : -0.1f;
                    respectImpact = diplomaticEvent.Success ? 0.1f : -0.2f;
                    break;
                    
                case DiplomaticAction.Compliment:
                    trustImpact = 0.1f;
                    respectImpact = 0.1f;
                    break;
            }
            
            diplomaticEvent.ImpactOnTrust = trustImpact;
            diplomaticEvent.ImpactOnFear = fearImpact;
            diplomaticEvent.ImpactOnRespect = respectImpact;
        }

        /// <summary>
        /// 应用外交影响
        /// </summary>
        private void ApplyDiplomaticImpact(DiplomaticRelationship relationship, DiplomaticEvent diplomaticEvent)
        {
            relationship.TrustLevel = Math.Max(0, Math.Min(1, relationship.TrustLevel + diplomaticEvent.ImpactOnTrust));
            relationship.FearLevel = Math.Max(0, Math.Min(1, relationship.FearLevel + diplomaticEvent.ImpactOnFear));
            relationship.RespectLevel = Math.Max(0, Math.Min(1, relationship.RespectLevel + diplomaticEvent.ImpactOnRespect));
        }

        /// <summary>
        /// 获取关系键
        /// </summary>
        private string GetRelationKey(int factionA, int factionB)
        {
            int min = Math.Min(factionA, factionB);
            int max = Math.Max(factionA, factionB);
            return $"{min}_{max}";
        }

        /// <summary>
        /// 获取势力的所有外交关系
        /// </summary>
        private List<DiplomaticRelationship> GetFactionRelationships(int factionId)
        {
            return _relationships.Values
                .Where(r => r.FactionA == factionId || r.FactionB == factionId)
                .ToList();
        }

        /// <summary>
        /// 获取两个势力间的关系
        /// </summary>
        public DiplomaticRelationship GetRelationship(int factionA, int factionB)
        {
            string key = GetRelationKey(factionA, factionB);
            return _relationships.ContainsKey(key) ? _relationships[key] : null;
        }

        /// <summary>
        /// 获取外交系统状态
        /// </summary>
        public string GetDiplomacyStatus()
        {
            var status = $"外交关系数: {_relationships.Count}\n";
            status += $"势力策略数: {_factionStrategies.Count}\n";
            status += $"外交事件数: {_globalDiplomaticHistory.Count}\n";
            status += $"上次更新: {_lastUpdate:HH:mm:ss}";
            
            return status;
        }

        /// <summary>
        /// 重置外交系统
        /// </summary>
        public void Reset()
        {
            _relationships.Clear();
            _factionStrategies.Clear();
            _globalDiplomaticHistory.Clear();
        }
    }
}