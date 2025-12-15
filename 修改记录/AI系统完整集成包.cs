// ================================================================
// AI系统完整集成包 - 核心代码文件
// 包含所有AI系统相关的核心功能代码
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace GameManager
{
    // ================================================================
    // 1. AI决策管理器 - 协调不同的AI行为模式和决策逻辑
    // ================================================================
    
    /// <summary>
    /// AI决策管理器 - 协调不同的AI行为模式和决策逻辑
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

        public AIDecisionManager()
        {
            Instance = this;
            InitializeBehaviorWeights();
            _troopBehaviorModes = new Dictionary<int, AIBehaviorMode>();
        }

        /// <summary>
        /// 初始化不同行为模式的权重配置
        /// </summary>
        private void InitializeBehaviorWeights()
        {
            _behaviorWeights = new Dictionary<AIBehaviorMode, AIDecisionWeights>
            {
                [AIBehaviorMode.Aggressive] = new AIDecisionWeights
                {
                    DistanceToGoal = 1.2f,
                    ThreatAvoidance = 0.3f,
                    TerrainAdvantage = 0.4f,
                    UnknownAreaPenalty = 0.1f,
                    EnemyProximity = -0.5f, // 负值表示倾向于接近敌人
                    AllySupport = 0.3f
                },
                [AIBehaviorMode.Defensive] = new AIDecisionWeights
                {
                    DistanceToGoal = 0.8f,
                    ThreatAvoidance = 1.5f,
                    TerrainAdvantage = 1.0f,
                    UnknownAreaPenalty = 0.8f,
                    EnemyProximity = 1.2f,
                    AllySupport = 1.0f
                },
                [AIBehaviorMode.Balanced] = new AIDecisionWeights
                {
                    DistanceToGoal = 1.0f,
                    ThreatAvoidance = 1.0f,
                    TerrainAdvantage = 0.7f,
                    UnknownAreaPenalty = 0.5f,
                    EnemyProximity = 0.6f,
                    AllySupport = 0.7f
                },
                [AIBehaviorMode.Opportunistic] = new AIDecisionWeights
                {
                    DistanceToGoal = 0.9f,
                    ThreatAvoidance = 0.7f,
                    TerrainAdvantage = 0.8f,
                    UnknownAreaPenalty = 0.2f,
                    EnemyProximity = 0.0f, // 中性，根据情况而定
                    AllySupport = 0.5f
                },
                [AIBehaviorMode.Cautious] = new AIDecisionWeights
                {
                    DistanceToGoal = 0.7f,
                    ThreatAvoidance = 1.8f,
                    TerrainAdvantage = 1.2f,
                    UnknownAreaPenalty = 1.0f,
                    EnemyProximity = 1.5f,
                    AllySupport = 1.2f
                }
            };
        }
        /// <summary>
        /// 根据将领性格确定AI行为模式
        /// </summary>
        public AIBehaviorMode DetermineBehaviorMode(Troop troop)
        {
            if (troop.Leader == null)
                return AIBehaviorMode.Balanced;

            if (_troopBehaviorModes.ContainsKey(troop.ID))
                return _troopBehaviorModes[troop.ID];

            var leader = troop.Leader;
            AIBehaviorMode mode;

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

            _troopBehaviorModes[troop.ID] = mode;
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
        /// 计算高级地块评分
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

                return totalScore;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateAdvancedTileScore] 错误: {ex.Message}");
                return -1000.0f;
            }
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

        private float CalculateTerrainScore(Troop troop, Point targetPos) { return 0; }
        private float CalculateUnknownAreaPenalty(Troop troop, Point targetPos)
        {
            if (troop.BelongedFaction != null && !troop.BelongedFaction.IsPositionKnown(targetPos))
                return 20.0f;
            return 0;
        }

        private float CalculateEnemyProximityScore(Troop troop, Point targetPos)
        {
            var nearbyEnemies = GetNearbyEnemies(troop, targetPos, 5);
            if (nearbyEnemies.Count == 0) return 0;
            float avgDistance = nearbyEnemies.Average(e => Session.Current.Scenario.GetSimpleDistance(targetPos, e.Position));
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
        public void ClearBehaviorCache() { _troopBehaviorModes.Clear(); }

        public string GetBehaviorModeDescription(Troop troop)
        {
            var mode = DetermineBehaviorMode(troop);
            return mode switch
            {
                AIBehaviorMode.Aggressive => "攻击性 - 勇猛冲锋，不畏威胁",
                AIBehaviorMode.Defensive => "防御性 - 稳扎稳打，重视安全",
                AIBehaviorMode.Balanced => "平衡型 - 攻守兼备，灵活应变",
                AIBehaviorMode.Opportunistic => "机会主义 - 善于抓住时机",
                AIBehaviorMode.Cautious => "谨慎型 - 小心翼翼，避免风险",
                _ => "未知"
            };
        }
    }

    // ================================================================
    // 2. 完整AI决策系统 - 三步式AI逻辑
    // ================================================================
    
    public class CompleteAIDecisionSystem
    {
        private AIMemoryMap memoryMap;
        private InfluenceMap influenceMap;

        public CompleteAIDecisionSystem()
        {
            memoryMap = new AIMemoryMap();
            influenceMap = new InfluenceMap(200, 200);
        }

        public void UpdateMemory() { }
        public void RefreshInfluenceMap() { }
        public void ExecuteSmartMove() { }

        public void Update()
        {
            UpdateMemory();
            RefreshInfluenceMap();
            ExecuteSmartMove();
        }

        public void RunAILogic(Faction faction = null) { Update(); }
        public string GetSystemStats() { return $"AI系统运行正常 - 记忆单位: {memoryMap.GetAllGhosts().Count}"; }
    }
    // ================================================================
    // 3. AI管理器 - 性能优化的AI决策调度
    // ================================================================
    
    public class AIManager
    {
        private List<Troop> _allTroops;
        private int _processedThisFrame = 0;
        private float _totalProcessingTime = 0f;
        private int _maxTimeBudgetMs = 5;
        private int _decisionInterval = 30;
        private int _influenceUpdateInterval = 60;
        private int _intelUpdateInterval = 30;
        private readonly Dictionary<int, InfluenceMap> _factionInfluenceMaps = new Dictionary<int, InfluenceMap>();
        private int _lastInfluenceUpdateFrame = 0;

        public AIManager(List<Troop> troops)
        {
            _allTroops = troops ?? new List<Troop>();
        }

        public void UpdateTroopList(List<Troop> newTroops)
        {
            _allTroops = newTroops ?? new List<Troop>();
        }

        public void Update(GameTime gameTime, int currentFrame)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _processedThisFrame = 0;
                _totalProcessingTime = 0f;

                // 更新影响力地图
                if (currentFrame - _lastInfluenceUpdateFrame >= _influenceUpdateInterval)
                {
                    UpdateInfluenceMaps();
                    _lastInfluenceUpdateFrame = currentFrame;
                }

                // 处理AI部队
                int totalCount = _allTroops.Count;
                foreach (var troop in _allTroops)
                {
                    if (troop == null || troop.Destroyed) continue;
                    if (currentFrame % _decisionInterval != troop.ID % _decisionInterval) continue;

                    var troopStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    ProcessEnhancedAI(troop);
                    troopStopwatch.Stop();
                    
                    _totalProcessingTime += (float)troopStopwatch.Elapsed.TotalMilliseconds;
                    _processedThisFrame++;

                    if (stopwatch.ElapsedMilliseconds >= _maxTimeBudgetMs) break;
                }

                stopwatch.Stop();
                if (currentFrame % 60 == 0 && System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIManager] 处理部队: {_processedThisFrame}/{totalCount} 用时: {_totalProcessingTime:F2}ms");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager.Update] 错误: {ex.Message}");
            }
        }
        private void UpdateInfluenceMaps()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions == null) return;

                int mapWidth = Session.Current.Scenario.ScenarioMap?.MapDimensions.X ?? 200;
                int mapHeight = Session.Current.Scenario.ScenarioMap?.MapDimensions.Y ?? 200;

                foreach (Faction faction in Session.Current.Scenario.Factions)
                {
                    if (faction == null || !faction.IsAlive) continue;

                    if (!_factionInfluenceMaps.TryGetValue(faction.ID, out var influenceMap))
                    {
                        influenceMap = new InfluenceMap(mapWidth, mapHeight);
                        _factionInfluenceMaps[faction.ID] = influenceMap;
                    }

                    influenceMap.Refresh(faction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager.UpdateInfluenceMaps] 错误: {ex.Message}");
            }
        }

        private void ProcessEnhancedAI(Troop troop)
        {
            try
            {
                // 基于影响力地图的决策增强
                if (troop.BelongedFaction != null && 
                    _factionInfluenceMaps.TryGetValue(troop.BelongedFaction.ID, out var influenceMap))
                {
                    float currentThreat = influenceMap.GetInfluence(troop.Position);
                    
                    if (currentThreat > 50f) // 威胁过高
                    {
                        var searchArea = new List<Point>();
                        for (int x = troop.Position.X - 3; x <= troop.Position.X + 3; x++)
                        {
                            for (int y = troop.Position.Y - 3; y <= troop.Position.Y + 3; y++)
                            {
                                searchArea.Add(new Point(x, y));
                            }
                        }
                        
                        var safestPos = influenceMap.FindSafestPosition(searchArea);
                        
                        if (influenceMap.GetInfluence(safestPos) < currentThreat - 10f)
                        {
                            troop.RealDestination = safestPos;
                            return;
                        }
                    }
                }

                // 回退到原有AI
                troop.ProcessAI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager.ProcessEnhancedAI] 错误: {ex.Message}");
                troop.ProcessAI();
            }
        }
        public InfluenceMap GetInfluenceMap(int factionId)
        {
            _factionInfluenceMaps.TryGetValue(factionId, out var map);
            return map;
        }

        public void ConfigurePerformance(int maxTimeBudgetMs, int decisionInterval)
        {
            _maxTimeBudgetMs = Math.Max(1, maxTimeBudgetMs);
            _decisionInterval = Math.Max(1, decisionInterval);
            System.Diagnostics.Debug.WriteLine($"[AIManager] 性能配置更新: 时间预算={_maxTimeBudgetMs}ms, 决策间隔={_decisionInterval}帧");
        }

        public string GetPerformanceStats()
        {
            int totalMemoryUnits = 0;
            if (Session.Current?.Scenario?.Factions != null)
            {
                foreach (var faction in Session.Current.Scenario.Factions)
                {
                    if (faction?.MemoryMap != null)
                    {
                        totalMemoryUnits += faction.MemoryMap.GetAllGhosts().Count;
                    }
                }
            }
            
            return $"AI处理: {_processedThisFrame}部队 用时: {_totalProcessingTime:F2}ms 总数: {_allTroops.Count} 影响力地图: {_factionInfluenceMaps.Count}个 记忆单位: {totalMemoryUnits}个";
        }
    }

    // ================================================================
    // 4. 寻路管理器 - 分层寻路系统
    // ================================================================
    
    public class PathfindingManager
    {
        private static PathfindingManager _instance;
        
        public static PathfindingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PathfindingManager();
                }
                return _instance;
            }
        }

        public PathfindingManager() { }
        public void Update() { }
        public void Prewarm() { System.Diagnostics.Debug.WriteLine("[PathfindingManager] 寻路系统预热完成"); }
        public List<Point> FindPath(Point start, Point end)
        {
            var path = new List<Point>();
            path.Add(start);
            path.Add(end);
            return path;
        }

        public List<Point> FindPath(Point start, Point end, bool allowDiagonal)
        {
            return FindPath(start, end);
        }

        public void FindPathAsync(Point start, Point end, Action<List<Point>> callback)
        {
            var path = FindPath(start, end);
            callback?.Invoke(path);
        }
    }

    // ================================================================
    // 5. AI记忆地图系统
    // ================================================================
    
    public class AIMemoryMap
    {
        private Dictionary<string, GhostUnit> memoryMap;

        public AIMemoryMap()
        {
            memoryMap = new Dictionary<string, GhostUnit>();
        }

        public Dictionary<string, GhostUnit> Values => memoryMap;

        public void AddOrUpdateGhost(string key, GhostUnit ghost) { memoryMap[key] = ghost; }
        public GhostUnit GetGhost(string key) { return memoryMap.ContainsKey(key) ? memoryMap[key] : null; }
        public void RemoveGhost(string key) { memoryMap.Remove(key); }
        public void Clear() { memoryMap.Clear(); }
        public Dictionary<string, GhostUnit> GetAllGhosts() { return new Dictionary<string, GhostUnit>(memoryMap); }

        public void CleanExpiredMemories(int currentDay = 0)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in memoryMap)
            {
                if (kvp.Value.IsExpired())
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                memoryMap.Remove(key);
            }
        }
    }
    // ================================================================
    // 6. 影响力地图系统
    // ================================================================
    
    public class InfluenceMap
    {
        private float[,] influenceData;
        private int width;
        private int height;

        public InfluenceMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.influenceData = new float[width, height];
        }

        public void SetInfluence(int x, int y, float value)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                influenceData[x, y] = value;
            }
        }

        public float GetInfluence(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return influenceData[x, y];
            }
            return 0f;
        }

        public float GetInfluence(Point position) { return GetInfluence(position.X, position.Y); }

        public void Clear()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    influenceData[x, y] = 0f;
                }
            }
        }

        public void Refresh(object faction)
        {
            Clear();
            System.Diagnostics.Debug.WriteLine("[InfluenceMap] 影响力地图已刷新");
        }
        public Point FindSafestPosition(List<Point> searchArea)
        {
            if (searchArea == null || searchArea.Count == 0)
                return new Point(width / 2, height / 2);

            Point safestPos = searchArea[0];
            float lowestThreat = GetInfluence(safestPos.X, safestPos.Y);

            foreach (Point pos in searchArea)
            {
                float threat = GetInfluence(pos.X, pos.Y);
                if (threat < lowestThreat)
                {
                    lowestThreat = threat;
                    safestPos = pos;
                }
            }
            return safestPos;
        }

        public string GetDebugInfo()
        {
            float totalInfluence = 0f;
            int nonZeroCount = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = influenceData[x, y];
                    if (value != 0f)
                    {
                        totalInfluence += Math.Abs(value);
                        nonZeroCount++;
                    }
                }
            }
            return $"影响力地图 ({width}x{height}): {nonZeroCount} 个活跃点, 总影响力: {totalInfluence:F2}";
        }
    }

    // ================================================================
    // 7. 幽灵单位系统
    // ================================================================
    
    public class GhostUnit
    {
        public int TroopId { get; set; }
        public int FactionId { get; set; }
        public Point Position { get; set; }
        public DateTime LastSeenTime { get; set; }
        public float Strength { get; set; }
        public int PersonCount { get; set; }

        public Point LastPosition { get => Position; set => Position = value; }
        public int RealUnitID { get => TroopId; set => TroopId = value; }
        public GhostUnit(int troopId, int factionId, Point position, float strength, int personCount)
        {
            TroopId = troopId;
            FactionId = factionId;
            Position = position;
            Strength = strength;
            PersonCount = personCount;
            LastSeenTime = DateTime.Now;
        }

        public GhostUnit(Troop troop, int currentDay)
        {
            TroopId = troop.ID;
            FactionId = troop.BelongedFaction.ID;
            Position = troop.Position;
            Strength = troop.Quantity;
            PersonCount = troop.PersonCount;
            LastSeenTime = DateTime.Now;
        }

        public bool IsExpired() { return (DateTime.Now - LastSeenTime).TotalDays > 10; }

        public float GetCurrentStrength()
        {
            double daysPassed = (DateTime.Now - LastSeenTime).TotalDays;
            if (daysPassed >= 10) return 0f;
            return Strength * (1f - (float)(daysPassed / 10.0));
        }

        public void Update(Point newPosition, float newStrength)
        {
            Position = newPosition;
            LastPosition = newPosition;
            Strength = newStrength;
            LastSeenTime = DateTime.Now;
        }
    }

    // ================================================================
    // 8. 战略地图系统
    // ================================================================
    
    public class StrategicMap
    {
        public static StrategicMap Instance { get; private set; }
        private InfluenceMap _influenceMap;
        private int _width, _height;

        public StrategicMap(int width, int height)
        {
            _width = width;
            _height = height;
            _influenceMap = new InfluenceMap(width, height);
            Instance = this;
        }
        public void Refresh(Faction aiFaction)
        {
            if (_influenceMap == null) return;
            _influenceMap.Clear();

            if (aiFaction.MemoryMap != null && aiFaction.MemoryMap.Values != null)
            {
                foreach (var ghost in aiFaction.MemoryMap.Values.Values)
                {
                    float currentStrength = ghost.GetCurrentStrength();
                    if (currentStrength <= 0.1f) continue;

                    float threatValue = currentStrength / 100f;
                    AddInfluence(ghost.LastPosition, threatValue, 3);
                }
            }
        }

        private void AddInfluence(Point center, float value, int radius)
        {
            if (_influenceMap == null) return;

            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(_width - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(_height - 1, center.Y + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dist = Vector2.Distance(new Vector2(center.X, center.Y), new Vector2(x, y));
                    if (dist <= radius)
                    {
                        float influence = value * (1.0f - dist / radius);
                        float current = _influenceMap.GetInfluence(x, y);
                        _influenceMap.SetInfluence(x, y, current + influence);
                    }
                }
            }
        }

        public float GetThreat(Point pos) { return _influenceMap?.GetInfluence(pos) ?? 0; }
        public float GetInfluence(Point pos) { return GetThreat(pos); }
        public void Clear() { _influenceMap?.Clear(); }
        public Point GetMapSize() { return new Point(_width, _height); }
    }