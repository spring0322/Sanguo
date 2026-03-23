using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using global::GameGlobal;
using global::GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI系统管理器 - 统一管理和协调所有AI子系统
    /// </summary>
    public class AISystemManager
    {
        public static AISystemManager Instance { get; private set; }

        // AI系统状态
        public bool IsInitialized { get; private set; }
        public bool IsEnabled { get; set; } = true;

        // 子系统实例
        private StrategicMap _strategicMap;
        private AIDecisionManager _decisionManager;
        private AITacticalCoordinator _tacticalCoordinator;
        private AILearningSystem _learningSystem;
        private DynamicDifficultySystem _difficultySystem;
        private AIDiplomacySystem _diplomacySystem;
        private CombatAISystem _combatAI;

        // 性能监控
        private DateTime _lastUpdate = DateTime.Now;
        private int _updateInterval = 1000; // 毫秒

        public AISystemManager()
        {
            Instance = this;
        }

        /// <summary>
        /// 初始化AI系统
        /// </summary>
        /// <param name="mapWidth">地图宽度</param>
        /// <param name="mapHeight">地图高度</param>
        public void Initialize(int mapWidth, int mapHeight)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AISystemManager] 开始初始化AI系统...");

                // 初始化战略地图
                _strategicMap = new StrategicMap(mapWidth, mapHeight);

                // 初始化决策管理器
                _decisionManager = new AIDecisionManager();

                // 初始化战术协调器
                _tacticalCoordinator = new AITacticalCoordinator();

                // 初始化学习系统
                _learningSystem = new AILearningSystem();

                // 初始化动态难度系统
                _difficultySystem = new DynamicDifficultySystem();

                // 初始化外交系统
                _diplomacySystem = new AIDiplomacySystem();

                // 初始化战斗AI系统
                _combatAI = new CombatAISystem();

                IsInitialized = true;
                System.Diagnostics.Debug.WriteLine("[AISystemManager] AI系统初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISystemManager] 初始化失败: {ex.Message}");
                IsInitialized = false;
            }
        }

        /// <summary>
        /// 更新AI系统（每回合调用）
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        public void UpdateAISystem(Faction currentFaction)
        {
            if (!IsInitialized || !IsEnabled || currentFaction == null)
                return;

            try
            {
                // 控制更新频率
                var now = DateTime.Now;
                if ((now - _lastUpdate).TotalMilliseconds < _updateInterval)
                    return;

                System.Diagnostics.Debug.WriteLine($"[AISystemManager] 更新AI系统 - 势力: {currentFaction.Name}");

                // 1. 更新战略地图
                UpdateStrategicMap(currentFaction);

                // 2. 组织战术编组
                UpdateTacticalGroups(currentFaction);

                // 3. 更新外交系统
                UpdateDiplomacy(currentFaction);

                // 4. 更新学习系统
                UpdateLearning(currentFaction);

                // 5. 执行AI决策
                ExecuteAIDecisions(currentFaction);

                // 6. 清理过期数据
                CleanupSystems();

                _lastUpdate = now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISystemManager] 更新AI系统时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新战略地图
        /// </summary>
        private void UpdateStrategicMap(Faction faction)
        {
            try
            {
                if (_strategicMap != null)
                {
                    _strategicMap.Refresh(faction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateStrategicMap] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新战术编组
        /// </summary>
        private void UpdateTacticalGroups(Faction faction)
        {
            try
            {
                if (_tacticalCoordinator != null)
                {
                    // 重新组织编组（如果需要）
                    _tacticalCoordinator.OrganizeTacticalGroups(faction);
                    
                    // 更新编组行动
                    _tacticalCoordinator.UpdateTacticalGroups(faction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateTacticalGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新外交系统
        /// </summary>
        private void UpdateDiplomacy(Faction faction)
        {
            try
            {
                if (_diplomacySystem != null)
                {
                    // 初始化外交关系（如果还没有初始化）
                    var allFactions = new List<Faction>();
                    foreach (var obj in Session.Current.Scenario.Factions.GetList())
                    {
                        if (obj is Faction f)
                        {
                            allFactions.Add(f);
                        }
                    }
                    
                    if (allFactions.Count > 0)
                    {
                        _diplomacySystem.InitializeDiplomacy(allFactions);
                    }

                    // 更新外交系统
                    _diplomacySystem.UpdateDiplomacy();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateDiplomacy] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新学习系统
        /// </summary>
        private void UpdateLearning(Faction faction)
        {
            try
            {
                if (_learningSystem != null)
                {
                    // 清理过期数据
                    _learningSystem.CleanupOldData();

                    // 分析玩家行为（如果是玩家势力）
                    if (faction.Controlling)
                    {
                        var playerPattern = _learningSystem.AnalyzePlayerBehavior(faction);
                        System.Diagnostics.Debug.WriteLine($"[AILearning] 玩家行为模式: {playerPattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateLearning] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理系统
        /// </summary>
        private void CleanupSystems()
        {
            try
            {
                // 清理战斗AI的过期决策
                _combatAI?.CleanupOldDecisions();

                // 清理学习系统的过期数据
                _learningSystem?.CleanupOldData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupSystems] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行AI决策
        /// </summary>
        private void ExecuteAIDecisions(Faction faction)
        {
            try
            {
                if (faction.Troops == null) return;

                var troops = new List<Troop>();
                foreach (var obj in faction.Troops.GetList())
                {
                    if (obj is Troop t && t.Status == TroopStatus.一般 && t.Leader != null)
                    {
                        troops.Add(t);
                    }
                }

                foreach (var troop in troops)
                {
                    ExecuteTroopAI(troop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteAIDecisions] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行单个部队的AI逻辑
        /// </summary>
        private void ExecuteTroopAI(Troop troop)
        {
            try
            {
                if (troop?.Leader == null) return;

                // 1. 战斗AI评估
                var combatDecision = _combatAI?.EvaluateCombatSituation(troop);
                if (combatDecision != null && combatDecision.State != CombatAISystem.CombatState.Idle)
                {
                    // 如果在战斗状态，优先执行战斗决策
                    troop.Destination = combatDecision.TargetPosition;
                    
                    System.Diagnostics.Debug.WriteLine(
                        $"[AI] 部队 {troop.DisplayName} 战斗状态: {_combatAI.GetCombatStateDescription(combatDecision.State)}"
                    );
                    return;
                }

                // 2. 确定战略目标
                Point strategicGoal = DetermineStrategicGoal(troop);

                // 3. 使用高级决策系统计算最佳移动目标
                if (_decisionManager != null)
                {
                    Point bestTarget = FindBestMoveTargetAdvanced(troop, strategicGoal);
                    
                    if (bestTarget != troop.Position)
                    {
                        // 设置移动目标
                        troop.Destination = bestTarget;
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"[AI] 部队 {troop.DisplayName} 移动到 ({bestTarget.X}, {bestTarget.Y})"
                        );
                    }
                }

                // 4. 记录AI行为用于学习
                RecordAIAction(troop, "Move", troop.Destination, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteTroopAI] 部队 {troop?.ID} 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 确定战略目标
        /// </summary>
        private Point DetermineStrategicGoal(Troop troop)
        {
            try
            {
                // 优先级：
                // 1. 已设定的目标建筑
                if (troop.WillArchitecture != null)
                {
                    return troop.WillArchitecture.Position;
                }

                // 2. 最近的敌方建筑
                var nearestEnemyArch = FindNearestEnemyArchitecture(troop);
                if (nearestEnemyArch != null)
                {
                    return nearestEnemyArch.Position;
                }

                // 3. 势力中心
                if (troop.BelongedFaction?.Architectures != null)
                {
                    var factionArchs = troop.BelongedFaction.Architectures.GetList();
                    if (factionArchs.Count > 0)
                    {
                        Architecture centerArch = null;
                        float minDistance = float.MaxValue;
                        
                        foreach (var obj in factionArchs)
                        {
                            if (obj is Architecture arch)
                            {
                                float distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, arch.Position);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    centerArch = arch;
                                }
                            }
                        }
                        
                        if (centerArch != null)
                        {
                            return centerArch.Position;
                        }
                    }
                }

                // 4. 默认：当前位置
                return troop.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetermineStrategicGoal] 错误: {ex.Message}");
                return troop.Position;
            }
        }

        /// <summary>
        /// 寻找最近的敌方建筑
        /// </summary>
        private Architecture FindNearestEnemyArchitecture(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return null;

                var allArchitectures = Session.Current.Scenario.Architectures.GetList();
                var enemyArchitectures = new List<Architecture>();
                
                foreach (var obj in allArchitectures)
                {
                    if (obj is Architecture a && a.BelongedFaction != null && 
                        !troop.BelongedFaction.IsFriendly(a.BelongedFaction))
                    {
                        enemyArchitectures.Add(a);
                    }
                }

                if (enemyArchitectures.Count == 0) return null;

                Architecture nearest = null;
                float minDistance = float.MaxValue;
                
                foreach (var arch in enemyArchitectures)
                {
                    float distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, arch.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = arch;
                    }
                }
                
                return nearest;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearestEnemyArchitecture] 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 使用高级决策系统寻找最佳移动目标
        /// </summary>
        private Point FindBestMoveTargetAdvanced(Troop troop, Point strategicGoal)
        {
            try
            {
                Point bestTarget = troop.Position;
                float bestScore = float.MinValue;
                int searchRadius = 5;

                // 在当前位置周围搜索
                for (int x = troop.Position.X - searchRadius; x <= troop.Position.X + searchRadius; x++)
                {
                    for (int y = troop.Position.Y - searchRadius; y <= troop.Position.Y + searchRadius; y++)
                    {
                        Point candidate = new Point(x, y);
                        
                        // 跳过当前位置
                        if (candidate == troop.Position) continue;

                        // 基本可达性检查
                        if (!IsValidPosition(troop, candidate)) continue;

                        // 使用高级评分系统
                        float score = _decisionManager.CalculateAdvancedTileScore(troop, candidate, strategicGoal);
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = candidate;
                        }
                    }
                }

                return bestTarget;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindBestMoveTargetAdvanced] 错误: {ex.Message}");
                return troop.Position;
            }
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        private bool IsValidPosition(Troop troop, Point pos)
        {
            try
            {
                // 边界检查
                if (pos.X < 0 || pos.Y < 0 || 
                    pos.X >= Session.Current.Scenario.ScenarioMap.MapDimensions.X || 
                    pos.Y >= Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                {
                    return false;
                }

                // 检查是否有其他部队占据
                var existingTroop = Session.Current.Scenario.GetTroopByPosition(pos);
                if (existingTroop != null && existingTroop != troop)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IsValidPosition] 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 记录AI行为用于学习
        /// </summary>
        private void RecordAIAction(Troop troop, string actionType, Point location, bool success)
        {
            try
            {
                if (_learningSystem != null && troop?.BelongedFaction != null)
                {
                    _learningSystem.RecordEvent(
                        actionType,
                        location,
                        $"部队 {troop.DisplayName} 执行 {actionType}",
                        success,
                        troop.BelongedFaction
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecordAIAction] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取AI系统状态信息
        /// </summary>
        public string GetSystemStatus()
        {
            if (!IsInitialized)
                return "AI系统未初始化";

            if (!IsEnabled)
                return "AI系统已禁用";

            var status = "AI系统运行中\n";
            status += $"战略地图: {(_strategicMap != null ? "已加载" : "未加载")}\n";
            status += $"决策管理器: {(_decisionManager != null ? "已加载" : "未加载")}\n";
            status += $"战术协调器: {(_tacticalCoordinator != null ? "已加载" : "未加载")}\n";
            status += $"学习系统: {(_learningSystem != null ? "已加载" : "未加载")}\n";
            status += $"难度系统: {(_difficultySystem != null ? "已加载" : "未加载")}\n";
            status += $"外交系统: {(_diplomacySystem != null ? "已加载" : "未加载")}\n";
            status += $"战斗AI系统: {(_combatAI != null ? "已加载" : "未加载")}\n";
            status += $"上次更新: {_lastUpdate:HH:mm:ss}";

            if (_difficultySystem != null)
            {
                status += $"\n当前难度: {_difficultySystem.CurrentDifficulty}";
            }

            return status;
        }

        /// <summary>
        /// 获取部队的AI行为描述
        /// </summary>
        public string GetTroopAIDescription(Troop troop)
        {
            if (!IsInitialized || _decisionManager == null || troop?.Leader == null)
                return "无AI信息";

            return _decisionManager.GetBehaviorModeDescription(troop);
        }

        /// <summary>
        /// 设置更新间隔
        /// </summary>
        /// <param name="intervalMs">间隔毫秒数</param>
        public void SetUpdateInterval(int intervalMs)
        {
            _updateInterval = Math.Max(100, intervalMs); // 最小100ms
        }

        /// <summary>
        /// 重置AI系统
        /// </summary>
        public void Reset()
        {
            try
            {
                _strategicMap?.Clear();
                _decisionManager?.ClearBehaviorCache();
                _tacticalCoordinator?.ClearAllGroups();
                WorldOfTheThreeKingdoms.GameManager.AICoordinatedAttackSystem.Instance.Update(); // Cleanup expired coordinations
                
                System.Diagnostics.Debug.WriteLine("[AISystemManager] AI系统已重置");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISystemManager] 重置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            Reset();
            IsInitialized = false;
            Instance = null;
        }
    }
}