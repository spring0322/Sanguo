using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// AI战略管理器 - 负责协调所有AI势力的战略决策
    /// </summary>
    public class AIStrategicManager
    {
        private Dictionary<int, FactionProfile> _factionProfiles;
        private Dictionary<int, ResourceSnapshot> _resourceSnapshots;
        private Dictionary<int, StrategicStance> _currentStances;
        private Dictionary<int, int> _stanceChangeCooldown;
        
        private int _lastUpdateTurn = -1;
        private const int STANCE_CHANGE_COOLDOWN = 3; // 战略姿态改变冷却回合数

        public AIStrategicManager()
        {
            _factionProfiles = new Dictionary<int, FactionProfile>();
            _resourceSnapshots = new Dictionary<int, ResourceSnapshot>();
            _currentStances = new Dictionary<int, StrategicStance>();
            _stanceChangeCooldown = new Dictionary<int, int>();
        }

        /// <summary>
        /// 每回合更新AI战略决策
        /// </summary>
        public void UpdateStrategicDecisions()
        {
            try
            {
                if (Session.Current?.Scenario == null)
                    return;

                int currentTurn = Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
                
                // 避免同一回合重复更新
                if (_lastUpdateTurn == currentTurn)
                    return;
                
                _lastUpdateTurn = currentTurn;

                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 开始第{currentTurn}回合的战略决策更新");
                }

                // 先进行资源结算（包含AI难度调整和玩家腐败系统）
                TurnResourceManager.Instance.CalculateIncomePhase();

                // 然后更新所有AI势力的战略决策
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
                {
                    if (faction.IsAlive && !Session.Current.Scenario.IsPlayer(faction))
                    {
                        UpdateFactionStrategy(faction, currentTurn);
                    }
                }

                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 第{currentTurn}回合战略决策更新完成");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 更新战略决策时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新单个势力的战略
        /// </summary>
        private void UpdateFactionStrategy(Faction faction, int currentTurn)
        {
            try
            {
                if (faction?.Leader == null)
                    return;

                // 1. 更新势力画像
                var profile = GetOrUpdateFactionProfile(faction);
                
                // 2. 更新资源快照
                var snapshot = ResourceSnapshot.CalculateSnapshot(faction);
                _resourceSnapshots[faction.ID] = snapshot;

                // 3. 分析邻居情况
                var neighbors = FactionNeighbors.AnalyzeNeighbors(faction);

                // 4. 决定战略姿态 - 使用新的StrategicBrain
                var newStance = DetermineNewStance(faction, profile, snapshot, neighbors, currentTurn);
                
                // 5. 执行战略行动
                if (newStance.HasValue)
                {
                    _currentStances[faction.ID] = newStance.Value;
                    AIStrategicDecisionSystem.ExecuteStrategicActions(faction, newStance.Value, profile, snapshot);
                }

                // 6. 更新冷却时间
                UpdateCooldowns(faction.ID);

                if (AIStrategicConfig.EnableDetailedDecisionLog)
                {
                    LogFactionStrategy(faction, profile, snapshot, neighbors, _currentStances.GetValueOrDefault(faction.ID));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 更新势力 {faction.Name} 战略时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取或更新势力画像
        /// </summary>
        private FactionProfile GetOrUpdateFactionProfile(Faction faction)
        {
            // 检查是否有历史君主特性
            var historicalProfile = AIStrategicConfig.GetHistoricalProfile(faction.Leader.Name);
            if (historicalProfile.HasValue)
            {
                _factionProfiles[faction.ID] = historicalProfile.Value;
                return historicalProfile.Value;
            }

            // 动态计算势力画像
            var profile = FactionProfile.CalculateProfile(faction);
            _factionProfiles[faction.ID] = profile;
            return profile;
        }

        /// <summary>
        /// 决定新的战略姿态
        /// </summary>
        private StrategicStance? DetermineNewStance(Faction faction, FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors, int currentTurn)
        {
            // 检查是否在冷却期内
            if (_stanceChangeCooldown.ContainsKey(faction.ID) && _stanceChangeCooldown[faction.ID] > 0)
            {
                return null; // 冷却期内不改变战略
            }

            // 计算新的战略姿态 - 使用新的StrategicBrain
            var newStance = AIStrategicDecisionSystem.DetermineStrategicStance(profile, snapshot, neighbors, faction.ID);
            var currentStance = _currentStances.GetValueOrDefault(faction.ID, StrategicStance.Stabilization);

            // 如果战略姿态改变，设置冷却时间并记录推理过程
            if (newStance != currentStance)
            {
                _stanceChangeCooldown[faction.ID] = STANCE_CHANGE_COOLDOWN;
                
                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略] {faction.Name} 战略姿态变更: {currentStance} -> {newStance}");
                    
                    // 记录决策推理过程
                    var reasoning = AIStrategicDecisionSystem.GetDecisionReasoning(profile, snapshot, neighbors, faction.ID);
                    System.Diagnostics.Debug.WriteLine($"[AI战略] 决策推理: {reasoning}");
                }
            }

            return newStance;
        }

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        private void UpdateCooldowns(int factionId)
        {
            if (_stanceChangeCooldown.ContainsKey(factionId) && _stanceChangeCooldown[factionId] > 0)
            {
                _stanceChangeCooldown[factionId]--;
            }
        }

        /// <summary>
        /// 记录势力战略信息
        /// </summary>
        private void LogFactionStrategy(Faction faction, FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors, StrategicStance stance)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略详情] === {faction.Name} ===");
            System.Diagnostics.Debug.WriteLine($"  君主: {faction.Leader.Name}");
            System.Diagnostics.Debug.WriteLine($"  军师: {faction.Advisor?.Name ?? "无"}");
            System.Diagnostics.Debug.WriteLine($"  战略姿态: {AIStrategicDecisionSystem.GetStanceDescription(stance)}");
            
            // 显示难度调整信息
            var difficultyStatus = DifficultyManager.Instance.GetCurrentDifficultyStatus();
            System.Diagnostics.Debug.WriteLine($"  难度调整:");
            System.Diagnostics.Debug.WriteLine($"    游戏阶段: {difficultyStatus.GamePhase}");
            System.Diagnostics.Debug.WriteLine($"    玩家统治度: {difficultyStatus.DominanceRatio:P1}");
            System.Diagnostics.Debug.WriteLine($"    资源修正: {difficultyStatus.ResourceModifier:F2}x");
            System.Diagnostics.Debug.WriteLine($"    军事修正: {difficultyStatus.MilitaryModifier:F2}x");
            System.Diagnostics.Debug.WriteLine($"    招募修正: {difficultyStatus.RecruitmentModifier:F2}x");
            
            System.Diagnostics.Debug.WriteLine($"  势力画像:");
            System.Diagnostics.Debug.WriteLine($"    君主激进度: {profile.RulerAggression:F2}");
            System.Diagnostics.Debug.WriteLine($"    军师智力: {profile.AdvisorWisdom:F2}");
            System.Diagnostics.Debug.WriteLine($"    武将压力: {profile.GeneralWarPressure:F2}");
            System.Diagnostics.Debug.WriteLine($"    决策稳定性: {profile.Decisiveness:F2}");
            System.Diagnostics.Debug.WriteLine($"  资源状况:");
            System.Diagnostics.Debug.WriteLine($"    军事力量: {snapshot.TotalMilitaryPower:F0} (含难度修正)");
            System.Diagnostics.Debug.WriteLine($"    平均疲劳: {snapshot.AverageFatigue:F1}%");
            System.Diagnostics.Debug.WriteLine($"    经济健康: {snapshot.EconomicHealth:F2}");
            System.Diagnostics.Debug.WriteLine($"    威胁等级: {snapshot.ThreatLevel:F2}");
            System.Diagnostics.Debug.WriteLine($"    机会等级: {snapshot.OpportunityLevel:F2}");
            System.Diagnostics.Debug.WriteLine($"    城池数量: {snapshot.ArchitectureCount}");
            System.Diagnostics.Debug.WriteLine($"    是否在战: {(snapshot.IsAtWar ? "是" : "否")}");
            System.Diagnostics.Debug.WriteLine($"  邻居分析:");
            System.Diagnostics.Debug.WriteLine($"    邻居数量: {neighbors.NeighborCount}");
            System.Diagnostics.Debug.WriteLine($"    最强邻居: {neighbors.StrongestNeighborPower:F0}");
            System.Diagnostics.Debug.WriteLine($"    平均实力: {neighbors.AveragePower:F0}");
            System.Diagnostics.Debug.WriteLine($"    脆弱目标: {(neighbors.HasVulnerableTarget ? "有" : "无")}");
            
            // 如果有最佳攻击目标，显示详细分析
            if (neighbors.BestVictim != null)
            {
                System.Diagnostics.Debug.WriteLine($"  最佳攻击目标:");
                System.Diagnostics.Debug.WriteLine($"    目标势力: {neighbors.BestVictim.Name}");
                System.Diagnostics.Debug.WriteLine($"    攻击评分: {neighbors.BestVictimScore:F1}");
                
                // 显示详细的攻击目标分析
                var victimReport = FactionNeighbors.GetVictimAnalysisReport(faction, neighbors.BestVictim);
                foreach (var line in victimReport.Split('\n'))
                {
                    System.Diagnostics.Debug.WriteLine($"    {line}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"  决策推理: {AIStrategicDecisionSystem.GetDecisionReasoning(profile, snapshot, neighbors, faction.ID)}");
        }

        /// <summary>
        /// 获取势力当前的战略姿态
        /// </summary>
        public StrategicStance GetFactionStance(int factionId)
        {
            return _currentStances.GetValueOrDefault(factionId, StrategicStance.Stabilization);
        }

        /// <summary>
        /// 获取势力画像
        /// </summary>
        public FactionProfile? GetFactionProfile(int factionId)
        {
            return _factionProfiles.ContainsKey(factionId) ? _factionProfiles[factionId] : null;
        }

        /// <summary>
        /// 获取资源快照
        /// </summary>
        public ResourceSnapshot? GetResourceSnapshot(int factionId)
        {
            return _resourceSnapshots.ContainsKey(factionId) ? _resourceSnapshots[factionId] : null;
        }

        /// <summary>
        /// 强制更新指定势力的战略
        /// </summary>
        public void ForceUpdateFactionStrategy(Faction faction)
        {
            if (faction == null) return;

            try
            {
                // 清除冷却时间，允许立即更新
                _stanceChangeCooldown[faction.ID] = 0;
                
                int currentTurn = Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
                UpdateFactionStrategy(faction, currentTurn);
                
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 强制更新势力 {faction.Name} 的战略");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 强制更新势力 {faction.Name} 战略时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理已灭亡势力的数据
        /// </summary>
        public void CleanupDestroyedFactions()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions == null)
                    return;

                var aliveFactionIds = Session.Current.Scenario.Factions.GetList()
                    .Cast<Faction>()
                    .Where(f => f.IsAlive)
                    .Select(f => f.ID)
                    .ToHashSet();

                // 清理已灭亡势力的数据
                var keysToRemove = _factionProfiles.Keys.Where(id => !aliveFactionIds.Contains(id)).ToList();
                foreach (var key in keysToRemove)
                {
                    _factionProfiles.Remove(key);
                    _resourceSnapshots.Remove(key);
                    _currentStances.Remove(key);
                    _stanceChangeCooldown.Remove(key);
                }

                if (keysToRemove.Count > 0 && AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 清理了 {keysToRemove.Count} 个已灭亡势力的数据");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 清理已灭亡势力数据时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有AI势力的战略概览
        /// </summary>
        public Dictionary<string, object> GetStrategicOverview()
        {
            var overview = new Dictionary<string, object>();
            
            try
            {
                if (Session.Current?.Scenario?.Factions == null)
                    return overview;

                var factionSummaries = new List<Dictionary<string, object>>();

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
                {
                    if (faction.IsAlive && !Session.Current.Scenario.IsPlayer(faction))
                    {
                        var summary = new Dictionary<string, object>
                        {
                            ["Name"] = faction.Name,
                            ["Leader"] = faction.Leader?.Name ?? "无",
                            ["Advisor"] = faction.Advisor?.Name ?? "无",
                            ["Stance"] = GetFactionStance(faction.ID).ToString(),
                            ["StanceDescription"] = AIStrategicDecisionSystem.GetStanceDescription(GetFactionStance(faction.ID)),
                            ["Architectures"] = faction.Architectures?.Count ?? 0,
                            ["Fund"] = faction.Fund,
                            ["Food"] = faction.Food
                        };

                        var profile = GetFactionProfile(faction.ID);
                        if (profile.HasValue)
                        {
                            summary["RulerAggression"] = profile.Value.RulerAggression;
                            summary["AdvisorWisdom"] = profile.Value.AdvisorWisdom;
                            summary["Decisiveness"] = profile.Value.Decisiveness;
                        }

                        factionSummaries.Add(summary);
                    }
                }

                overview["Factions"] = factionSummaries;
                overview["LastUpdateTurn"] = _lastUpdateTurn;
                overview["TotalAIFactions"] = factionSummaries.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略管理器] 获取战略概览时发生异常: {ex.Message}");
            }

            return overview;
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : defaultValue;
        }
    }
}