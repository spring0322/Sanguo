using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameGlobal;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// AI决策管理器 - 协调不同的AI行为模式和决策逻辑
    /// 版本: 2.1 - 适配 C# 7.3 / MonoGame 环境
    /// </summary>
    public class AIDecisionManager
    {
        public static AIDecisionManager Instance { get; private set; }

        // AI行为模式枚举
        public enum AIBehaviorMode
        {
            Aggressive,     // 攻击性
            Defensive,      // 防御性
            Balanced,       // 平衡型
            Opportunistic,  // 机会主义
            Cautious        // 谨慎型
        }

        // AI决策权重配置
        public class AIDecisionWeights
        {
            public float DistanceToGoal { get; set; } = 1.0f;
            public float ThreatAvoidance { get; set; } = 1.0f;
            public float TerrainAdvantage { get; set; } = 0.5f;
            public float UnknownAreaPenalty { get; set; } = 0.3f;
            public float EnemyProximity { get; set; } = 0.8f;
            public float AllySupport { get; set; } = 0.6f;
        }

        private Dictionary<AIBehaviorMode, AIDecisionWeights> _behaviorWeights;
        private Dictionary<int, AIBehaviorMode> _troopBehaviorModes;
        private Dictionary<int, DateTime> _behaviorCacheTimestamps;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        // === 新增：冲动行为和惯性决策支持 ===
        private Random _rng = new Random();
        private InfluenceMap _influenceMap;
        private Dictionary<int, AIBehaviorMode> _currentModes = new Dictionary<int, AIBehaviorMode>();
        private Dictionary<int, DateTime> _lastDecisionTimes = new Dictionary<int, DateTime>();
        private const float HysteresisFactor = 1.2f;
        private readonly TimeSpan _minDecisionInterval = TimeSpan.FromSeconds(2);

        public CampaignManager CampaignManager { get; private set; }

        public AIDecisionManager()
        {
            Instance = this;
            InitializeBehaviorWeights();
            _troopBehaviorModes = new Dictionary<int, AIBehaviorMode>();
            _behaviorCacheTimestamps = new Dictionary<int, DateTime>();
            CampaignManager = new CampaignManager();
            
            // 初始化影响力地图
            int mapWidth = 200;
            int mapHeight = 200;
            try
            {
                if (Session.Current?.Scenario?.ScenarioMap != null)
                {
                    mapWidth = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                    mapHeight = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                }
            }
            catch { }
            _influenceMap = new InfluenceMap(mapWidth, mapHeight);
        }

        /// <summary>
        /// 初始化不同行为模式的权重配置
        /// </summary>
        private void InitializeBehaviorWeights()
        {
            _behaviorWeights = new Dictionary<AIBehaviorMode, AIDecisionWeights>
            {
                { AIBehaviorMode.Aggressive, new AIDecisionWeights
                    {
                        DistanceToGoal = 1.2f,
                        ThreatAvoidance = 0.3f,
                        TerrainAdvantage = 0.4f,
                        UnknownAreaPenalty = 0.1f,
                        EnemyProximity = -0.5f, // 负值表示倾向于接近敌人
                        AllySupport = 0.3f
                    }
                },
                { AIBehaviorMode.Defensive, new AIDecisionWeights
                    {
                        DistanceToGoal = 0.8f,
                        ThreatAvoidance = 1.5f,
                        TerrainAdvantage = 1.0f,
                        UnknownAreaPenalty = 0.8f,
                        EnemyProximity = 1.2f,
                        AllySupport = 1.0f
                    }
                },
                { AIBehaviorMode.Balanced, new AIDecisionWeights
                    {
                        DistanceToGoal = 1.0f,
                        ThreatAvoidance = 1.0f,
                        TerrainAdvantage = 0.7f,
                        UnknownAreaPenalty = 0.5f,
                        EnemyProximity = 0.6f,
                        AllySupport = 0.7f
                    }
                },
                { AIBehaviorMode.Opportunistic, new AIDecisionWeights
                    {
                        DistanceToGoal = 0.9f,
                        ThreatAvoidance = 0.7f,
                        TerrainAdvantage = 0.8f,
                        UnknownAreaPenalty = 0.2f,
                        EnemyProximity = 0.0f, // 中性
                        AllySupport = 0.5f
                    }
                },
                { AIBehaviorMode.Cautious, new AIDecisionWeights
                    {
                        DistanceToGoal = 0.7f,
                        ThreatAvoidance = 1.8f,
                        TerrainAdvantage = 1.2f,
                        UnknownAreaPenalty = 1.0f,
                        EnemyProximity = 1.5f,
                        AllySupport = 1.2f
                    }
                }
            };
        }

        /// <summary>
        /// 根据将领性格确定AI行为模式（带缓存优化）
        /// </summary>
        public AIBehaviorMode DetermineBehaviorMode(Troop troop)
        {
            if (troop.Leader == null)
                return AIBehaviorMode.Balanced;

            // 检查缓存
            if (_troopBehaviorModes.ContainsKey(troop.ID) &&
                _behaviorCacheTimestamps.ContainsKey(troop.ID))
            {
                if (DateTime.Now - _behaviorCacheTimestamps[troop.ID] < _cacheExpiration)
                {
                    return _troopBehaviorModes[troop.ID];
                }
            }

            var leader = troop.Leader;
            AIBehaviorMode mode;

            // 基于将领属性的行为模式判断
            if (leader.Braveness >= 8 && leader.Calmness <= 5)
            {
                mode = AIBehaviorMode.Aggressive;
            }
            else if (leader.Calmness >= 8 && leader.Braveness <= 5)
            {
                mode = AIBehaviorMode.Cautious;
            }
            else if (leader.Intelligence >= 8)
            {
                mode = AIBehaviorMode.Opportunistic;
            }
            else if (Math.Abs(leader.Braveness - leader.Calmness) <= 2)
            {
                mode = AIBehaviorMode.Balanced;
            }
            else
            {
                mode = AIBehaviorMode.Defensive;
            }

            // 更新缓存
            if (_troopBehaviorModes.ContainsKey(troop.ID))
                _troopBehaviorModes[troop.ID] = mode;
            else
                _troopBehaviorModes.Add(troop.ID, mode);

            if (_behaviorCacheTimestamps.ContainsKey(troop.ID))
                _behaviorCacheTimestamps[troop.ID] = DateTime.Now;
            else
                _behaviorCacheTimestamps.Add(troop.ID, DateTime.Now);

            return mode;
        }

        /// <summary>
        /// 获取指定行为模式的决策权重
        /// </summary>
        public AIDecisionWeights GetDecisionWeights(AIBehaviorMode mode)
        {
            return _behaviorWeights.ContainsKey(mode) ? _behaviorWeights[mode] : _behaviorWeights[AIBehaviorMode.Balanced];
        }

        /// <summary>
        /// 计算高级地块评分（增强版）
        /// </summary>
        public float CalculateAdvancedTileScore(Troop troop, Point targetPos, Point strategicGoal)
        {
            try
            {
                var behaviorMode = DetermineBehaviorMode(troop);
                var weights = GetDecisionWeights(behaviorMode);
                float totalScore = 0;

                // 1. 距离到战略目标的评分
                int distToGoal = Session.Current.Scenario.GetSimpleDistance(targetPos, strategicGoal);
                float distanceScore = -distToGoal * 10.0f * weights.DistanceToGoal;
                totalScore += distanceScore;

                // 2. 威胁评估评分
                float threatScore = CalculateThreatScore(troop, targetPos) * weights.ThreatAvoidance;
                totalScore += threatScore;

                // 3. 地形优势评分
                float terrainScore = CalculateTerrainScore(troop, targetPos) * weights.TerrainAdvantage;
                totalScore += terrainScore;

                // 4. 未知区域惩罚
                float unknownPenalty = CalculateUnknownAreaPenalty(troop, targetPos) * weights.UnknownAreaPenalty;
                totalScore -= unknownPenalty;

                // 5. 敌军接近度评分
                float enemyProximityScore = CalculateEnemyProximityScore(troop, targetPos) * weights.EnemyProximity;
                totalScore += enemyProximityScore;

                // 6. 友军支援评分
                float allySupportScore = CalculateAllySupportScore(troop, targetPos) * weights.AllySupport;
                totalScore += allySupportScore;

                // 7. 角色特定加分
                float roleBonus = CalculateRoleSpecificBonus(troop, targetPos);
                totalScore += roleBonus;

                return totalScore;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateAdvancedTileScore] 错误: {ex.Message}");
                return -1000.0f;
            }
        }

        /// <summary>
        /// Calculate threat with difficulty adjustments
        /// </summary>
        public float CalculateThreat(Troop troop, Point pos, AIDifficulty difficulty)
        {
            // 1. 正常视野威胁
            float threat = CalculateThreatScore(troop, pos); // User called it GetVisibleEnemyThreat, mapping to existing logic but returning positive threat (CalculateThreatScore returns negative)

            // Convert existing negative score to positive threat for calculation
            threat = -threat;

            // 2. 记忆/天眼修正
            if (difficulty >= AIDifficulty.Hard)
            {
                // 加上记忆中的敌军 (Ghost Units)
                threat += GetGhostUnitThreat(troop, pos);
            }

            if (difficulty == AIDifficulty.Nightmare)
            {
                // 作弊：获取战争迷雾下的真实敌军，但给予 50% 的数值折损
                threat += GetHiddenEnemyThreat(troop, pos) * 0.5f; 
            }

            return threat;
        }

        private float GetGhostUnitThreat(Troop troop, Point pos)
        {
            float threat = 0;
            if (troop.BelongedFaction != null && troop.BelongedFaction.MemoryMap != null)
            {
                 // Iterate ghosts
                 var ghosts = troop.BelongedFaction.MemoryMap.GetAllGhosts();
                 if (ghosts != null)
                 {
                     foreach (var kvp in ghosts)
                     {
                         var ghost = kvp.Value;
                         if (ghost == null) continue;
                         // Check distance
                         // GhostPosition is likely a Point
                         int dist = Session.Current.Scenario.GetSimpleDistance(pos, ghost.Position); // Assuming GhostUnit has Position
                         if (dist <= 3) // Threat range
                         {
                             // Threat calculation: assume ghost has Force/Quantity
                             // If GhostUnit has only minimal info, use heuristic
                             threat += (ghost.Strength / Math.Max(1, dist)); 
                         }
                     }
                 }
            }
            return threat;
        }

        private float GetHiddenEnemyThreat(Troop troop, Point pos)
        {
            float threat = 0;
            // Iterate all troops in scenario
            foreach (Troop t in Session.Current.Scenario.Troops)
            {
                if (t.Destroyed || t == troop) continue;
                if (!troop.BelongedFaction.IsFriendly(t.BelongedFaction))
                {
                    // Check if invisible? The point of this is utilizing info even if invisible.
                    // Just calculating real threat regardless of visibility.
                    // But we should subtract visible ones if we want ONLY hidden ones?
                    // The formula: threat += GetHiddenEnemyThreat(pos) * 0.5f
                    // If GetVisibleEnemyThreat sums visible ones, this might double count if we don't filter.
                    // However, Nightmare difficulty usually allows being "Overly cautious" or "Omniscient".
                    // Let's iterate all and if NOT visible, add 0.5, if visible, it's already in base threat?
                    // User said: "GetHiddenEnemyThreat" (implying threats that are hidden).
                    
                    if (!troop.BelongedFaction.IsPositionKnown(t.Position)) // Assuming IsPositionKnown exists or we check visibility
                    {
                        int dist = Session.Current.Scenario.GetSimpleDistance(pos, t.Position);
                        if (dist <= 3)
                        {
                            threat += (t.FightingForce / Math.Max(1, dist));
                        }
                    }
                }
            }
            return threat;
        }

        /// <summary>
        /// 计算角色特定加分
        /// </summary>
        private float CalculateRoleSpecificBonus(Troop troop, Point targetPos)
        {
            if (troop.CurrentRole == AIRole.None)
                return 0;

            float bonus = 0;

            switch (troop.CurrentRole)
            {
                case AIRole.Tank:
                    // 坦克倾向于前线位置
                    var nearbyEnemiesTank = GetNearbyEnemies(troop, targetPos, 2);
                    bonus += nearbyEnemiesTank.Count * 10.0f;
                    break;

                case AIRole.Support:
                    // 辅助倾向于友军附近但相对安全的位置
                    var nearbyAlliesSup = GetNearbyAllies(troop, targetPos, 3);
                    var nearbyThreatsSup = GetNearbyEnemies(troop, targetPos, 2);
                    bonus += nearbyAlliesSup.Count * 15.0f - nearbyThreatsSup.Count * 20.0f;
                    break;

                case AIRole.DPS:
                    // DPS寻找最佳输出位置
                    var enemiesDPS = GetNearbyEnemies(troop, targetPos, 3);
                    bonus += enemiesDPS.Count * 8.0f;
                    break;

                case AIRole.Mage:
                    // 法师需要安全距离但要能覆盖敌人
                    var distantEnemies = GetNearbyEnemies(troop, targetPos, 4);
                    var closeEnemies = GetNearbyEnemies(troop, targetPos, 2);
                    bonus += distantEnemies.Count * 12.0f - closeEnemies.Count * 15.0f;
                    break;

                case AIRole.Logistics:
                    // 后勤部队远离战斗
                    var allEnemies = GetNearbyEnemies(troop, targetPos, 5);
                    bonus -= allEnemies.Count * 25.0f;
                    break;
            }

            return bonus;
        }

        /// <summary>
        /// 计算威胁评分
        /// </summary>
        private float CalculateThreatScore(Troop troop, Point targetPos)
        {
            float threat = 0;
            var nearbyEnemies = GetNearbyEnemies(troop, targetPos, 3);
            foreach (var enemy in nearbyEnemies)
            {
                float distance = Session.Current.Scenario.GetSimpleDistance(targetPos, enemy.Position);
                float enemyThreat = enemy.FightingForce / Math.Max(1, distance);
                threat += enemyThreat;
            }
            return -threat; // 威胁越高，评分越低
        }

        private float CalculateTerrainScore(Troop troop, Point targetPos)
        {
            // 待根据实际地图数据实现
            return 0;
        }

        private float CalculateUnknownAreaPenalty(Troop troop, Point targetPos)
        {
            // 简化实现：如果部队所属势力不存在，返回默认惩罚
            if (troop.BelongedFaction == null)
                return 20.0f;
                
            // 暂时返回0，避免调用不存在的IsPositionKnown方法
            // TODO: 实现位置已知检查逻辑
            return 0;
        }

        private float CalculateEnemyProximityScore(Troop troop, Point targetPos)
        {
            var nearbyEnemies = GetNearbyEnemies(troop, targetPos, 5);
            if (nearbyEnemies.Count == 0) return 0;

            // C# 7.3 使用 Linq Average 是安全的
            float avgDistance = (float)nearbyEnemies.Average(e => Session.Current.Scenario.GetSimpleDistance(targetPos, e.Position));
            return avgDistance * 2.0f;
        }

        private float CalculateAllySupportScore(Troop troop, Point targetPos)
        {
            var nearbyAllies = GetNearbyAllies(troop, targetPos, 4);
            float supportScore = 0;
            foreach (var ally in nearbyAllies)
            {
                float distance = Session.Current.Scenario.GetSimpleDistance(targetPos, ally.Position);
                float support = ally.FightingForce / Math.Max(1, distance);
                supportScore += support;
            }
            return supportScore * 0.1f;
        }

        private List<Troop> GetNearbyEnemies(Troop troop, Point position, int radius)
        {
            var enemies = new List<Troop>();
            try
            {
                if (troop.BelongedFaction == null) return enemies;
                for (int x = position.X - radius; x <= position.X + radius; x++)
                {
                    for (int y = position.Y - radius; y <= position.Y + radius; y++)
                    {
                        var checkPos = new Point(x, y);
                        // 确保坐标在地图范围内
                        if (!GameObjects.AI.Helper.MapNavigationHelper.IsPositionInMapBounds(checkPos)) continue;

                        var otherTroop = Session.Current.Scenario.GetTroopByPosition(checkPos);
                        if (otherTroop != null && otherTroop != troop &&
                            !troop.BelongedFaction.IsFriendly(otherTroop.BelongedFaction))
                        {
                            enemies.Add(otherTroop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetNearbyEnemies] 错误: {ex.Message}");
            }
            return enemies;
        }

        private List<Troop> GetNearbyAllies(Troop troop, Point position, int radius)
        {
            var allies = new List<Troop>();
            try
            {
                if (troop.BelongedFaction == null) return allies;
                for (int x = position.X - radius; x <= position.X + radius; x++)
                {
                    for (int y = position.Y - radius; y <= position.Y + radius; y++)
                    {
                        var checkPos = new Point(x, y);
                        // 确保坐标在地图范围内
                        if (!GameObjects.AI.Helper.MapNavigationHelper.IsPositionInMapBounds(checkPos)) continue;

                        var otherTroop = Session.Current.Scenario.GetTroopByPosition(checkPos);
                        if (otherTroop != null && otherTroop != troop &&
                            troop.BelongedFaction.IsFriendly(otherTroop.BelongedFaction))
                        {
                            allies.Add(otherTroop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetNearbyAllies] 错误: {ex.Message}");
            }
            return allies;
        }

        public void ClearBehaviorCache()
        {
            _troopBehaviorModes.Clear();
            _behaviorCacheTimestamps.Clear();
        }

        public string GetBehaviorModeDescription(Troop troop)
        {
            // 修复: C# 7.3 不支持 switch 表达式，改回标准 switch case
            var mode = DetermineBehaviorMode(troop);
            switch (mode)
            {
                case AIBehaviorMode.Aggressive:
                    return "攻击性 - 勇猛冲锋，不畏威胁";
                case AIBehaviorMode.Defensive:
                    return "防御性 - 稳扎稳打，重视安全";
                case AIBehaviorMode.Balanced:
                    return "平衡型 - 攻守兼备，灵活应变";
                case AIBehaviorMode.Opportunistic:
                    return "机会主义 - 善于抓住时机";
                case AIBehaviorMode.Cautious:
                    return "谨慎型 - 小心翼翼，避免风险";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 获取AI决策统计信息
        /// </summary>
        public string GetDecisionStats()
        {
            return $"AI决策管理器 - 缓存行为模式: {_troopBehaviorModes.Count}个, 权重配置: {_behaviorWeights.Count}种";
        }

        public void Update()
        {
            CampaignManager?.Update();
        }

        #region === 增强决策系统：冲动行为 + 惯性决策 + 势图战术 ===

        /// <summary>
        /// [核心入口] 为部队计算下一帧的目标位置
        /// </summary>
        public Point GetTroopNextDestination(Troop troop)
        {
            if (troop == null || troop.Leader == null) return troop?.Position ?? Point.Zero;

            try
            {
                // --- 第一层：非理性冲动检查 ---
                var impulseDest = CheckImpulsiveBehavior(troop);
                if (impulseDest.HasValue)
                {
                    return impulseDest.Value;
                }

                // --- 第二层：战略模式确立 (带惯性) ---
                AIBehaviorMode mode = DetermineBehaviorModeWithHysteresis(troop);

                // --- 第三层：基于势图的战术移动 ---
                return CalculateTacticalPosition(troop, mode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTroopNextDestination] 错误: {ex.Message}");
                return troop.Position;
            }
        }

        /// <summary>
        /// 更新影响力地图
        /// </summary>
        public void UpdateInfluenceMap()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions != null && Session.Current.Scenario.Factions.Count > 0)
                {
                    _influenceMap.Refresh(Session.Current.Scenario.Factions[0]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateInfluenceMap] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 逻辑1：非理性冲动 (Impulse)
        /// </summary>
        private Point? CheckImpulsiveBehavior(Troop troop)
        {
            var leader = troop.Leader;
            if (leader == null) return null;

            // 1. 仇敌冲锋 (高勇猛 + 低冷静 + 视野内有仇人)
            if (leader.Braveness > 80 && leader.Calmness < 50)
            {
                var nemesis = FindNearestEnemyEnhanced(troop, 6);
                if (nemesis != null && _rng.Next(100) < 5) // 5% 概率触发
                {
                    System.Diagnostics.Debug.WriteLine($"[AI] {leader.Name} 怒发冲冠，发起冲锋！");
                    return nemesis.Position;
                }
            }

            // 2. 恐慌溃逃 (极低士气 + 低勇猛)
            if (troop.Morale < 40 && leader.Braveness < 40)
            {
                if (_rng.Next(100) < 10) // 10% 概率触发
                {
                    System.Diagnostics.Debug.WriteLine($"[AI] {leader.Name} 吓破了胆，试图逃跑！");
                    var searchArea = GetSurroundingPointsEnhanced(troop.Position, 5);
                    return _influenceMap.FindSafestPosition(searchArea);
                }
            }

            return null; // 理智尚存，返回空
        }

        /// <summary>
        /// 逻辑2：决策惯性 (Hysteresis)
        /// </summary>
        private AIBehaviorMode DetermineBehaviorModeWithHysteresis(Troop troop)
        {
            // 1. 时间锁：防止频繁计算
            if (_currentModes.ContainsKey(troop.ID) && 
                _lastDecisionTimes.ContainsKey(troop.ID) &&
                (DateTime.Now - _lastDecisionTimes[troop.ID]) < _minDecisionInterval)
            {
                return _currentModes[troop.ID];
            }

            // 2. 计算各模式得分
            float aggroScore = CalculateModeScoreEnhanced(troop, AIBehaviorMode.Aggressive);
            float defScore = CalculateModeScoreEnhanced(troop, AIBehaviorMode.Defensive);

            // 3. 应用惯性：给当前模式加分
            AIBehaviorMode current = _currentModes.ContainsKey(troop.ID) ? _currentModes[troop.ID] : AIBehaviorMode.Balanced;
            if (current == AIBehaviorMode.Aggressive) aggroScore *= HysteresisFactor;
            if (current == AIBehaviorMode.Defensive) defScore *= HysteresisFactor;

            // 4. 决出胜者
            AIBehaviorMode newMode = aggroScore > defScore ? AIBehaviorMode.Aggressive : AIBehaviorMode.Defensive;

            // 5. 更新缓存
            _currentModes[troop.ID] = newMode;
            _lastDecisionTimes[troop.ID] = DateTime.Now;

            return newMode;
        }

        private float CalculateModeScoreEnhanced(Troop troop, AIBehaviorMode mode)
        {
            float score = 50f; // 基础分
            var leader = troop.Leader;
            if (leader == null) return score;

            if (mode == AIBehaviorMode.Aggressive)
            {
                score += leader.Braveness * 0.5f;
                if (troop.Morale > 80) score += 30;
                // 简化判断：士气高则进攻模式得分更高
                score += troop.Morale * 0.2f;
            }
            else if (mode == AIBehaviorMode.Defensive)
            {
                score += leader.Calmness * 0.5f;
                if (troop.Morale < 60) score += 40;
                // 简化判断：士气低则防御模式得分更高
                score += (100 - troop.Morale) * 0.3f;
            }
            return score;
        }

        /// <summary>
        /// 逻辑3：结合势图的战术移动 (Influence Map Integration)
        /// </summary>
        private Point CalculateTacticalPosition(Troop troop, AIBehaviorMode mode)
        {
            var candidates = GetSurroundingPointsEnhanced(troop.Position, 4);
            Point bestPos = troop.Position;
            float bestScore = float.MinValue;

            foreach (var pos in candidates)
            {
                float influence = _influenceMap.GetInfluence(pos);
                float score = 0;

                if (mode == AIBehaviorMode.Aggressive)
                {
                    // 进攻模式：喜欢去影响力为负(敌区)的地方
                    if (influence < 0 && influence > -50) score += 100;
                    score -= Math.Abs(influence + 20); // 趋向于 -20 的区域
                }
                else // Defensive
                {
                    // 防御模式：直接找正值最大的地方
                    score += influence * 2.0f;
                }

                // 叠加距离因素
                int dist = Math.Abs(pos.X - troop.Position.X) + Math.Abs(pos.Y - troop.Position.Y);
                score -= dist * 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = pos;
                }
            }

            return bestPos;
        }

        private List<Point> GetSurroundingPointsEnhanced(Point center, int radius)
        {
            var list = new List<Point>();
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    var pos = new Point(x, y);
                    if (GameObjects.AI.Helper.MapNavigationHelper.IsPositionInMapBounds(pos))
                        list.Add(pos);
                }
            }
            return list;
        }

        private Troop FindNearestEnemyEnhanced(Troop me, int radius)
        {
            try
            {
                if (me.BelongedFaction == null || Session.Current?.Scenario?.Troops == null)
                    return null;

                Troop nearestEnemy = null;
                int nearestDistance = int.MaxValue;

                foreach (Troop t in Session.Current.Scenario.Troops)
                {
                    if (t == null || t.Destroyed || t == me) continue;
                    if (t.BelongedFaction == null) continue;
                    if (me.BelongedFaction.IsFriendly(t.BelongedFaction)) continue;

                    int distance = Session.Current.Scenario.GetSimpleDistance(t.Position, me.Position);
                    if (distance <= radius && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = t;
                    }
                }

                return nearestEnemy;
            }
            catch
            {
                return null;
            }
        }

        public void OrderAttack(Faction attacker, Architecture target)
        {
            CampaignManager?.StartSiegeCampaign(attacker, target);
        }

        public void PrepareAttackForce(Architecture city, int legionCount = 3)
        {
            // AIResourceAllocator is currently commented out due to dependency issues
            // This method is stubbed for now
            System.Diagnostics.Debug.WriteLine($"[AIDecisionManager] PrepareAttackForce called for {city?.Name ?? "null"} but AIResourceAllocator is unavailable.");
        }

        #endregion
    }
}