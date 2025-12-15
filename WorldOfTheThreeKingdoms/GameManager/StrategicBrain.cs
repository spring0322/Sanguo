using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 战略大脑系统
    /// 为AI势力提供高级战略决策能力，集成联盟系统和军师预测
    /// </summary>
    public class StrategicBrain
    {
        private readonly Faction _faction;
        private readonly Dictionary<string, float> _strategicMemory;
        private float _lastEvaluationTime;
        private StrategicStance _currentStance;
        private float _stanceStability; // 立场稳定性，防止频繁切换

        public StrategicBrain(Faction faction)
        {
            _faction = faction ?? throw new ArgumentNullException(nameof(faction));
            _strategicMemory = new Dictionary<string, float>();
            _lastEvaluationTime = 0f;
            _currentStance = StrategicStance.Neutral;
            _stanceStability = 0f;
        }

        /// <summary>
        /// 确定战略立场
        /// </summary>
        /// <param name="gameContext">游戏上下文信息</param>
        /// <returns>当前应采取的战略立场</returns>
        public StrategicStance DetermineStance(GameContext gameContext)
        {
            try
            {
                // 防止过于频繁的立场切换
                if (_stanceStability > 0f)
                {
                    _stanceStability -= Time.deltaTime;
                    return _currentStance;
                }

                StrategicStance newStance = EvaluateStrategicSituation(gameContext);
                
                // 如果立场发生重大变化，增加稳定性延迟
                if (newStance != _currentStance && IsSignificantStanceChange(_currentStance, newStance))
                {
                    _stanceStability = UnityEngine.Random.Range(3f, 8f); // 3-8回合的稳定期
                    Debug.Log($"[战略大脑] {_faction.Name} 战略立场变更: {_currentStance} → {newStance}");
                }

                _currentStance = newStance;
                return _currentStance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] DetermineStance 失败: {ex.Message}");
                return StrategicStance.Neutral;
            }
        }

        /// <summary>
        /// 评估战略形势
        /// </summary>
        private StrategicStance EvaluateStrategicSituation(GameContext gameContext)
        {
            try
            {
                // === 优先级1: 联盟状态检查 ===
                if (IsCoalitionMember())
                {
                    return EvaluateCoalitionStance();
                }

                // === 优先级2: 生存威胁检查 ===
                if (IsUnderExistentialThreat(gameContext))
                {
                    return StrategicStance.Panic;
                }

                // === 优先级3: 疲劳度和内政压力 ===
                float warWeariness = CalculateWarWeariness();
                if (warWeariness > 80f)
                {
                    return StrategicStance.Consolidation;
                }

                // === 优先级4: 机会评估 ===
                if (HasExpansionOpportunity(gameContext))
                {
                    return StrategicStance.Aggressive;
                }

                // === 优先级5: 威胁感知 ===
                float threatLevel = EvaluateThreatLevel(gameContext);
                if (threatLevel > 70f)
                {
                    return StrategicStance.Defensive;
                }

                // === 默认: 中性发展 ===
                return StrategicStance.Neutral;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluateStrategicSituation 失败: {ex.Message}");
                return StrategicStance.Neutral;
            }
        }

        /// <summary>
        /// 评估联盟立场
        /// </summary>
        private StrategicStance EvaluateCoalitionStance()
        {
            try
            {
                var coalitionManager = CoalitionManager.Instance;
                if (coalitionManager == null || !coalitionManager.IsActive)
                {
                    return StrategicStance.Neutral;
                }

                // 1. 检查盟主是否还活着
                if (coalitionManager.LeaderFaction == null || coalitionManager.LeaderFaction.Destroyed)
                {
                    Debug.Log($"[战略大脑] {_faction.Name}: 盟主已灭亡，联盟解散");
                    return StrategicStance.Panic; // 盟主死了，鸟兽散
                }

                // 2. 使用军师预测系统评估联盟作战成功率
                if (_faction.Advisor != null)
                {
                    Architecture coalitionTarget = coalitionManager.GetCoalitionTarget();
                    if (coalitionTarget?.Mayor != null)
                    {
                        int siegeChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                            _faction.Advisor, coalitionTarget.Mayor, "Siege");
                        
                        Debug.Log($"[战略大脑] {_faction.Name} 军师预测攻打 {coalitionTarget.Name} 成功率: {siegeChance}%");
                        
                        // 如果军师预测成功率太低，可能会犹豫
                        if (siegeChance < 30)
                        {
                            return StrategicStance.Cautious; // 谨慎观望
                        }
                    }
                }

                // 3. 评估联盟整体实力对比
                float coalitionStrength = EvaluateCoalitionStrength();
                float playerStrength = EvaluatePlayerStrength();
                
                if (coalitionStrength > playerStrength * 1.5f)
                {
                    return StrategicStance.CoalitionCrusade; // 强制执行：协同进攻
                }
                else if (coalitionStrength > playerStrength)
                {
                    return StrategicStance.CoalitionSupport; // 支援作战
                }
                else
                {
                    return StrategicStance.Cautious; // 实力不足，谨慎行事
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluateCoalitionStance 失败: {ex.Message}");
                return StrategicStance.Neutral;
            }
        }

        /// <summary>
        /// 检查是否为联盟成员
        /// </summary>
        private bool IsCoalitionMember()
        {
            try
            {
                return CoalitionStateManager.IsInCoalition(_faction);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否面临生存威胁
        /// </summary>
        private bool IsUnderExistentialThreat(GameContext gameContext)
        {
            try
            {
                // 城市数量过少
                if (_faction.Architectures.Count <= 2)
                {
                    return true;
                }

                // 被强敌包围
                int hostileNeighbors = CountHostileNeighbors();
                int totalNeighbors = CountAllNeighbors();
                
                if (totalNeighbors > 0 && (float)hostileNeighbors / totalNeighbors > 0.7f)
                {
                    return true;
                }

                // 经济崩溃
                long totalWealth = CalculateTotalWealth();
                if (totalWealth < 5000) // 经济危机阈值
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] IsUnderExistentialThreat 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算战争疲劳度
        /// </summary>
        private float CalculateWarWeariness()
        {
            try
            {
                float weariness = 0f;

                // 基于连续战争时间
                float continuousWarTime = GetContinuousWarTime();
                weariness += continuousWarTime * 2f;

                // 基于损失
                float casualtyRate = GetRecentCasualtyRate();
                weariness += casualtyRate * 30f;

                // 基于经济压力
                float economicStrain = GetEconomicStrain();
                weariness += economicStrain * 20f;

                // 基于民心
                float publicMorale = GetPublicMorale();
                weariness += (100f - publicMorale) * 0.5f;

                return Mathf.Clamp(weariness, 0f, 100f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] CalculateWarWeariness 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 检查是否有扩张机会
        /// </summary>
        private bool HasExpansionOpportunity(GameContext gameContext)
        {
            try
            {
                // 使用军师预测系统评估扩张机会
                if (_faction.Advisor == null) return false;

                var nearbyTargets = FindNearbyExpansionTargets();
                
                foreach (Architecture target in nearbyTargets)
                {
                    if (target.Mayor != null)
                    {
                        int attackChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                            _faction.Advisor, target.Mayor, "Siege");
                        
                        if (attackChance >= 70) // 高成功率
                        {
                            Debug.Log($"[战略大脑] {_faction.Name} 发现扩张机会: {target.Name} (成功率 {attackChance}%)");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] HasExpansionOpportunity 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 评估威胁等级
        /// </summary>
        private float EvaluateThreatLevel(GameContext gameContext)
        {
            try
            {
                float threatLevel = 0f;

                // 玩家威胁
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction != null)
                {
                    float playerThreat = EvaluatePlayerThreat(playerFaction);
                    threatLevel += playerThreat * 0.6f;
                }

                // 邻近敌对势力威胁
                float neighborThreat = EvaluateNeighborThreat();
                threatLevel += neighborThreat * 0.4f;

                return Mathf.Clamp(threatLevel, 0f, 100f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluateThreatLevel 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 评估联盟整体实力
        /// </summary>
        private float EvaluateCoalitionStrength()
        {
            try
            {
                float totalStrength = 0f;
                var coalitionManager = CoalitionManager.Instance;
                
                if (coalitionManager?.Members != null)
                {
                    foreach (Faction member in coalitionManager.Members)
                    {
                        if (member != null && !member.Destroyed)
                        {
                            totalStrength += EvaluateFactionStrength(member);
                        }
                    }
                }

                // 加上盟主实力
                if (coalitionManager?.LeaderFaction != null)
                {
                    totalStrength += EvaluateFactionStrength(coalitionManager.LeaderFaction);
                }

                return totalStrength;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluateCoalitionStrength 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 评估玩家实力
        /// </summary>
        private float EvaluatePlayerStrength()
        {
            try
            {
                Faction playerFaction = GetPlayerFaction();
                return playerFaction != null ? EvaluateFactionStrength(playerFaction) : 0f;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluatePlayerStrength 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 评估势力实力
        /// </summary>
        private float EvaluateFactionStrength(Faction faction)
        {
            try
            {
                if (faction == null) return 0f;

                float strength = 0f;

                // 领土实力
                strength += faction.Architectures.Count * 10f;

                // 军事实力
                if (faction.Troops != null)
                {
                    foreach (Troop troop in faction.Troops.GetList())
                    {
                        if (troop != null)
                        {
                            strength += troop.Quantity * 0.01f;
                        }
                    }
                }

                // 经济实力
                long totalWealth = 0;
                foreach (Architecture arch in faction.Architectures.GetList())
                {
                    if (arch != null)
                    {
                        totalWealth += arch.Fund + arch.Food;
                    }
                }
                strength += totalWealth * 0.0001f;

                // 人才实力
                if (faction.Persons != null)
                {
                    foreach (Person person in faction.Persons.GetList())
                    {
                        if (person != null)
                        {
                            int totalAbility = person.Intelligence + person.Command + person.Politics + person.Strength;
                            strength += totalAbility * 0.1f;
                        }
                    }
                }

                return strength;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluateFactionStrength 失败: {ex.Message}");
                return 0f;
            }
        }

        #region 辅助方法

        /// <summary>
        /// 检查立场变化是否重大
        /// </summary>
        private bool IsSignificantStanceChange(StrategicStance oldStance, StrategicStance newStance)
        {
            // 定义重大变化的立场对
            var significantChanges = new[]
            {
                (StrategicStance.Neutral, StrategicStance.Aggressive),
                (StrategicStance.Neutral, StrategicStance.CoalitionCrusade),
                (StrategicStance.Defensive, StrategicStance.Aggressive),
                (StrategicStance.Consolidation, StrategicStance.CoalitionCrusade),
                (StrategicStance.Panic, StrategicStance.Aggressive)
            };

            return significantChanges.Any(change => 
                (change.Item1 == oldStance && change.Item2 == newStance) ||
                (change.Item2 == oldStance && change.Item1 == newStance));
        }

        /// <summary>
        /// 获取玩家势力
        /// </summary>
        private Faction GetPlayerFaction()
        {
            try
            {
                return Session.Current?.Scenario?.Factions?.GetList()
                    ?.FirstOrDefault(f => f != null && f.IsPlayer);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 计算敌对邻居数量
        /// </summary>
        private int CountHostileNeighbors()
        {
            // 简化实现，实际需要根据地图邻接关系计算
            return 2;
        }

        /// <summary>
        /// 计算所有邻居数量
        /// </summary>
        private int CountAllNeighbors()
        {
            // 简化实现，实际需要根据地图邻接关系计算
            return 4;
        }

        /// <summary>
        /// 计算总财富
        /// </summary>
        private long CalculateTotalWealth()
        {
            try
            {
                long totalWealth = 0;
                foreach (Architecture arch in _faction.Architectures.GetList())
                {
                    if (arch != null)
                    {
                        totalWealth += arch.Fund + arch.Food;
                    }
                }
                return totalWealth;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取连续战争时间
        /// </summary>
        private float GetContinuousWarTime()
        {
            // 简化实现，实际需要跟踪战争状态
            return _strategicMemory.GetValueOrDefault("ContinuousWarTime", 0f);
        }

        /// <summary>
        /// 获取近期伤亡率
        /// </summary>
        private float GetRecentCasualtyRate()
        {
            // 简化实现，实际需要跟踪伤亡统计
            return _strategicMemory.GetValueOrDefault("CasualtyRate", 0f);
        }

        /// <summary>
        /// 获取经济压力
        /// </summary>
        private float GetEconomicStrain()
        {
            try
            {
                long totalWealth = CalculateTotalWealth();
                long militaryUpkeep = CalculateMilitaryUpkeep();
                
                if (totalWealth <= 0) return 100f;
                
                float strainRatio = (float)militaryUpkeep / totalWealth;
                return Mathf.Clamp(strainRatio * 100f, 0f, 100f);
            }
            catch
            {
                return 50f;
            }
        }

        /// <summary>
        /// 获取民心
        /// </summary>
        private float GetPublicMorale()
        {
            // 简化实现，实际需要基于各种因素计算民心
            return _strategicMemory.GetValueOrDefault("PublicMorale", 70f);
        }

        /// <summary>
        /// 寻找附近的扩张目标
        /// </summary>
        private List<Architecture> FindNearbyExpansionTargets()
        {
            var targets = new List<Architecture>();
            
            try
            {
                var allFactions = Session.Current?.Scenario?.Factions?.GetList();
                if (allFactions == null) return targets;

                foreach (Faction faction in allFactions)
                {
                    if (faction == null || faction == _faction || faction.IsPlayer) continue;
                    
                    foreach (Architecture arch in faction.Architectures.GetList())
                    {
                        if (arch != null && IsNearby(arch))
                        {
                            targets.Add(arch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] FindNearbyExpansionTargets 失败: {ex.Message}");
            }

            return targets;
        }

        /// <summary>
        /// 检查建筑是否在附近
        /// </summary>
        private bool IsNearby(Architecture target)
        {
            // 简化实现，实际需要基于地图距离计算
            return true;
        }

        /// <summary>
        /// 评估玩家威胁
        /// </summary>
        private float EvaluatePlayerThreat(Faction playerFaction)
        {
            try
            {
                float threat = 0f;

                // 基于实力对比
                float playerStrength = EvaluateFactionStrength(playerFaction);
                float myStrength = EvaluateFactionStrength(_faction);
                
                if (myStrength > 0)
                {
                    float strengthRatio = playerStrength / myStrength;
                    threat += strengthRatio * 30f;
                }

                // 基于距离
                float distance = CalculateDistanceToPlayer(playerFaction);
                if (distance < 50f) // 如果很近
                {
                    threat += (50f - distance) * 0.5f;
                }

                // 基于玩家的扩张趋势
                float expansionTrend = GetPlayerExpansionTrend();
                threat += expansionTrend * 0.3f;

                return Mathf.Clamp(threat, 0f, 100f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] EvaluatePlayerThreat 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 评估邻居威胁
        /// </summary>
        private float EvaluateNeighborThreat()
        {
            // 简化实现，实际需要评估所有邻近势力的威胁
            return 30f;
        }

        /// <summary>
        /// 计算军事维护费用
        /// </summary>
        private long CalculateMilitaryUpkeep()
        {
            try
            {
                long upkeep = 0;
                foreach (Troop troop in _faction.Troops.GetList())
                {
                    if (troop != null)
                    {
                        upkeep += troop.Quantity * 2; // 简化的维护费计算
                    }
                }
                return upkeep;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 计算到玩家的距离
        /// </summary>
        private float CalculateDistanceToPlayer(Faction playerFaction)
        {
            // 简化实现，实际需要计算最近城市间的距离
            return 30f;
        }

        /// <summary>
        /// 获取玩家扩张趋势
        /// </summary>
        private float GetPlayerExpansionTrend()
        {
            // 简化实现，实际需要跟踪玩家近期的扩张活动
            return _strategicMemory.GetValueOrDefault("PlayerExpansionTrend", 0f);
        }

        #endregion

        /// <summary>
        /// 更新战略记忆
        /// </summary>
        public void UpdateStrategicMemory(string key, float value)
        {
            _strategicMemory[key] = value;
        }

        /// <summary>
        /// 获取当前战略立场
        /// </summary>
        public StrategicStance GetCurrentStance()
        {
            return _currentStance;
        }

        /// <summary>
        /// 获取战略分析报告
        /// </summary>
        public string GetStrategicAnalysis()
        {
            try
            {
                var analysis = new System.Text.StringBuilder();
                analysis.AppendLine($"=== {_faction.Name} 战略分析 ===");
                analysis.AppendLine($"当前立场: {_currentStance}");
                analysis.AppendLine($"立场稳定性: {_stanceStability:F1} 回合");
                
                if (_faction.Advisor != null)
                {
                    analysis.AppendLine($"军师: {_faction.Advisor.Name} (智力 {_faction.Advisor.Intelligence})");
                    string accuracy = AdvisorDataHelper.GetAccuracyAssessment(_faction.Advisor);
                    analysis.AppendLine($"预测可信度: {accuracy}");
                }

                analysis.AppendLine($"联盟状态: {(IsCoalitionMember() ? "联盟成员" : "独立势力")}");
                analysis.AppendLine($"战争疲劳度: {CalculateWarWeariness():F1}%");
                analysis.AppendLine($"威胁等级: {EvaluateThreatLevel(null):F1}%");

                return analysis.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[战略大脑] GetStrategicAnalysis 失败: {ex.Message}");
                return "分析失败";
            }
        }
    }

    /// <summary>
    /// 战略立场枚举
    /// </summary>
    public enum StrategicStance
    {
        Neutral,           // 中性发展
        Aggressive,        // 主动扩张
        Defensive,         // 防御保守
        Consolidation,     // 休养生息
        Panic,            // 恐慌求生
        CoalitionCrusade, // 联盟十字军（强制协同进攻）
        CoalitionSupport, // 联盟支援
        Cautious          // 谨慎观望
    }

    /// <summary>
    /// 游戏上下文信息
    /// </summary>
    public class GameContext
    {
        public int CurrentTurn { get; set; }
        public List<Faction> AllFactions { get; set; }
        public Dictionary<string, object> GlobalEvents { get; set; }
        public float GlobalThreatLevel { get; set; }

        public GameContext()
        {
            AllFactions = new List<Faction>();
            GlobalEvents = new Dictionary<string, object>();
        }
    }
}