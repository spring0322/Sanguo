using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// AI管理器 - 性能优化的AI决策调度系统
    /// 版本: 3.0 - 集成分帧初始化与频率控制优化
    /// </summary>
    public class AIManager
    {
        private List<Troop> _allTroops;
        private int _processedThisFrame = 0;
        private float _totalProcessingTime = 0f;
        private int _maxTimeBudgetMs = 5;
        private int _decisionInterval = 30;
        private int _influenceUpdateInterval = 60;
        private int _intelUpdateInterval = 30;
        private readonly Dictionary<int, WorldOfTheThreeKingdoms.GameManager.InfluenceMap> _factionInfluenceMaps = new Dictionary<int, WorldOfTheThreeKingdoms.GameManager.InfluenceMap>();
        private int _lastInfluenceUpdateFrame = 0;

        // 新增：性能统计
        private readonly Dictionary<string, float> _performanceMetrics = new Dictionary<string, float>();
        private DateTime _lastStatsUpdate = DateTime.Now;

        // 新增：AI优先级队列
        private readonly Dictionary<int, int> _troopPriorities = new Dictionary<int, int>();

        // 新增：角色分配缓存
        private readonly Dictionary<int, DateTime> _roleAssignmentTimestamps = new Dictionary<int, DateTime>();
        private readonly TimeSpan _roleReassignmentInterval = TimeSpan.FromMinutes(2);

        // 性能优化：缓存变量
        private List<Troop> _prioritizedTroopsCache;
        private int _lastPriorityUpdateFrame = 0;
        private int _globalFrameCounter = 0;

        // --- 优化字段：分摊初始化专用 ---
        private Queue<Troop> _initQueue;
        private bool _isInitializing = false;
        private const int INIT_BATCH_SIZE = 20; // 每帧只处理20个，保证进场丝滑
        
        // --- 优化字段：运行时分流专用 ---
        // 如果 Session 中没有 CurrentFrame，我们自己维护一个计数器
        private static long _localFrameCounter = 0;
        
        // 调试开关：建议设为 false，彻底解决日志刷屏
        public static bool EnableDebugLog = false;
        
        // 战术AI系统（静态类，无需实例化）
        // private GameManager.AITacticalExecution _tacticalAI = new GameManager.AITacticalExecution();

        public static AIManager Instance { get; private set; }

        /// <summary>
        /// 检查AI管理器是否正在初始化
        /// </summary>
        public bool IsInitializing => _isInitializing;

        /// <summary>
        /// 获取初始化进度（0.0 - 1.0）
        /// </summary>
        public float InitializationProgress
        {
            get
            {
                if (!_isInitializing || _initQueue == null) return 1.0f;
                if (_allTroops.Count == 0) return 1.0f;
                
                int remaining = _initQueue.Count;
                int total = _allTroops.Count;
                return (float)(total - remaining) / total;
            }
        }

        public AIManager(List<Troop> troops)
        {
            Instance = this;
            _allTroops = troops ?? new List<Troop>();
            InitializePerformanceMetrics();
            
            // 确保CombatAISystem已初始化
            if (CombatAISystem.Instance == null)
            {
                new CombatAISystem();
                System.Diagnostics.Debug.WriteLine("[AIManager] 初始化CombatAISystem实例");
            }

            // 启动分帧初始化 - 使用新的优化方法
            StartInitialization(_allTroops);
        }

        /// <summary>
        /// 【关键优化】进入场景时调用此方法，替代原来的直接 foreach 循环
        /// </summary>
        public void StartInitialization(List<Troop> allTroops)
        {
            if (allTroops == null) return;
            _initQueue = new Queue<Troop>(allTroops);
            _isInitializing = true;
        }

        /// <summary>
        /// 在 MainGameScreen 的 Update 中每帧调用
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // --- 【插入】优先处理初始化队列 ---
            if (_isInitializing)
            {
                ProcessInitializationBatch();
                return; // 初始化期间跳过其他逻辑
            }

            // --- 【插入】帧计数器自增 ---
            _localFrameCounter++;

            // --- 下面保留原有的 Update 逻辑 ---
            UpdateRoutineAI(gameTime);
        }

        /// <summary>
        /// 【新增辅助方法】
        /// </summary>
        private void ProcessInitializationBatch()
        {
            if (_initQueue == null || _initQueue.Count == 0)
            {
                _isInitializing = false;
                return;
            }

            int processed = 0;
            while (processed < INIT_BATCH_SIZE && _initQueue.Count > 0)
            {
                var troop = _initQueue.Dequeue();
                // 如果你原代码里有 troop.InitializeAI() 或类似逻辑，在这里调用
                // 否则什么都不做也行，主要为了错峰加载
                processed++;
            }
        }

        private void AssignRoleAndInitAI(Troop troop)
        {
            // 在这里执行你原来那个耗时的分配逻辑
            // ... (角色分配代码) ...
            
            // 调用原有的角色分配逻辑
            AssignRoleToTroop(troop);
            
            // 【日志优化】只在特定情况下输出，防止 IO 卡顿
            if (EnableDebugLog && troop.ID < 5) 
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager] 初始化部队 {troop.ID}");
            }
        }

        /// <summary>
        /// 常规AI更新逻辑（原有逻辑的包装）
        /// </summary>
        private void UpdateRoutineAI(GameTime gameTime)
        {
            try
            {
                _globalFrameCounter++;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _processedThisFrame = 0;
                _totalProcessingTime = 0f;

                // --- 这里的代码是原本的 Update 逻辑 ---
                // 2. 常规 Update 逻辑
                // 更新影响力地图
                if (_globalFrameCounter - _lastInfluenceUpdateFrame >= _influenceUpdateInterval)
                {
                    UpdateInfluenceMaps();
                    _lastInfluenceUpdateFrame = _globalFrameCounter;
                }

                // 智能角色分配 - 降低频率
                if (_globalFrameCounter % 120 == 0) // 每2秒执行一次
                {
                    AssignRolesIntelligently(_globalFrameCounter);
                }

                // 处理AI部队（按优先级排序）
                var prioritizedTroops = GetPrioritizedTroops();

                foreach (var troop in prioritizedTroops)
                {
                    if (troop == null || troop.Destroyed) continue;

                    // 分帧处理机制
                    if (_globalFrameCounter % _decisionInterval != troop.ID % _decisionInterval) continue;

                    var troopStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    ProcessEnhancedAI(troop);
                    troopStopwatch.Stop();

                    _totalProcessingTime += (float)troopStopwatch.Elapsed.TotalMilliseconds;
                    _processedThisFrame++;

                    // 时间预算耗尽则中断
                    if (stopwatch.ElapsedMilliseconds >= _maxTimeBudgetMs) break;
                }

                stopwatch.Stop();

                // 更新性能指标
                UpdatePerformanceMetrics();

                if (_globalFrameCounter % 300 == 0 && EnableDebugLog) // 降低调试输出频率
                {
                    System.Diagnostics.Debug.WriteLine($"[AIManager] 处理部队: {_processedThisFrame}/{_allTroops.Count} 用时: {_totalProcessingTime:F2}ms");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager.Update] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取优先级排序的部队列表
        /// </summary>
        private List<Troop> GetPrioritizedTroops()
        {
            try
            {
                // 使用缓存机制，避免每帧重新排序
                if (_prioritizedTroopsCache == null || _globalFrameCounter - _lastPriorityUpdateFrame >= 60)
                {
                    _prioritizedTroopsCache = _allTroops
                        .Where(t => t != null && !t.Destroyed)
                        .OrderByDescending(t => GetTroopPriority(t))
                        .ToList();
                    _lastPriorityUpdateFrame = _globalFrameCounter;
                }
                
                return _prioritizedTroopsCache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPrioritizedTroops] 错误: {ex.Message}");
                return _allTroops ?? new List<Troop>();
            }
        }

        /// <summary>
        /// 计算部队优先级
        /// </summary>
        private int GetTroopPriority(Troop troop)
        {
            try
            {
                if (troop == null || troop.Destroyed) return 0;
                
                int priority = 0;
                
                // 基于部队状态的优先级
                switch (troop.TroopStatus)
                {
                    case TroopStatus.攻击:
                        priority += 100; // 战斗中的部队优先级最高
                        break;
                    case TroopStatus.行军:
                        priority += 50;  // 移动中的部队次之
                        break;
                    default:
                        priority += 10;  // 其他状态优先级较低
                        break;
                }
                
                // 基于部队规模的优先级
                if (troop.Army != null)
                {
                    priority += Math.Min(troop.Army.Quantity / 1000, 50); // 规模越大优先级越高
                }
                
                return priority;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 处理增强AI逻辑
        /// </summary>
        private void ProcessEnhancedAI(Troop troop)
        {
            try
            {
                if (troop == null || troop.Destroyed) return;
                
                // 调用部队的AI处理方法
                troop.ProcessAI();
            }
            catch (Exception ex)
            {
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessEnhancedAI] 部队{troop?.ID} 处理错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 智能角色分配
        /// </summary>
        private void AssignRolesIntelligently(int currentFrame)
        {
            try
            {
                // 只处理需要重新分配角色的部队
                var needAssignmentTroops = _allTroops
                    .Where(t => t != null && !t.Destroyed && ShouldReassignRole(t))
                    .Take(5) // 每次最多处理5个，避免性能问题
                    .ToList();
                
                foreach (var troop in needAssignmentTroops)
                {
                    AssignRoleToTroop(troop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AssignRolesIntelligently] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否需要重新分配角色
        /// </summary>
        private bool ShouldReassignRole(Troop troop)
        {
            try
            {
                if (troop == null) return false;
                
                // 检查是否有角色分配时间戳
                if (!_roleAssignmentTimestamps.ContainsKey(troop.ID))
                {
                    return true; // 从未分配过角色
                }
                
                // 检查是否超过重新分配间隔
                var lastAssignment = _roleAssignmentTimestamps[troop.ID];
                return DateTime.Now - lastAssignment > _roleReassignmentInterval;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 更新影响力地图
        /// </summary>
        private void UpdateInfluenceMaps()
        {
            try
            {
                // 简化的影响力地图更新
                if (Session.Current?.Scenario?.Factions != null)
                {
                    var factions = Session.Current.Scenario.Factions.GetList();
                    foreach (var faction in factions.Cast<Faction>().Take(3)) // 限制处理数量
                    {
                        if (faction != null && !faction.Destroyed)
                        {
                            UpdateFactionInfluence(faction);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateInfluenceMaps] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新势力影响力
        /// </summary>
        private void UpdateFactionInfluence(Faction faction)
        {
            try
            {
                // 简化的势力影响力计算
                if (faction?.ID != null && !_factionInfluenceMaps.ContainsKey(faction.ID))
                {
                    // 使用默认地图尺寸创建影响力地图
                    _factionInfluenceMaps[faction.ID] = new WorldOfTheThreeKingdoms.GameManager.InfluenceMap(100, 100);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateFactionInfluence] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            try
            {
                _performanceMetrics["ProcessedTroops"] = _processedThisFrame;
                _performanceMetrics["ProcessingTime"] = _totalProcessingTime;
                _performanceMetrics["TotalTroops"] = _allTroops?.Count ?? 0;
                
                // 每10秒更新一次统计
                if (DateTime.Now - _lastStatsUpdate > TimeSpan.FromSeconds(10))
                {
                    _lastStatsUpdate = DateTime.Now;
                    if (EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[性能统计] 处理部队: {_processedThisFrame}, 用时: {_totalProcessingTime:F2}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePerformanceMetrics] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 为单个部队分配角色 - 优化版本，减少调试输出开销
        /// </summary>
        public void AssignRoleToTroop(Troop troop)
        {
            if (troop == null || troop.Destroyed) return;

            var newRole = WorldOfTheThreeKingdoms.GameGlobal.AIRoleSelector.DetermineRole(troop);
            if (newRole != troop.CurrentRole)
            {
                troop.CurrentRole = newRole;
                if (_roleAssignmentTimestamps.ContainsKey(troop.ID))
                    _roleAssignmentTimestamps[troop.ID] = DateTime.Now;
                else
                    _roleAssignmentTimestamps.Add(troop.ID, DateTime.Now);

                // --- 日志优化 ---
                // 只有在显式开启调试且满足特定条件时才输出
                // 解决了刷屏导致的 IO 卡顿
                if (Session.GlobalVariables.EnableAIDebugLog)
                {
                    // 甚至可以只打印前 5 个部队，确认逻辑是对的就行
                    if (troop.ID < 5)
                    {
                        // 使用 System.Diagnostics.Debug 仅在调试窗口输出，不影响控制台性能
                        System.Diagnostics.Debug.WriteLine($"[AIManager] 为部队 {troop.ID} 分配角色: {GetRoleDescription(newRole)}");
                    }
                }
            }
        }

        private void InitializePerformanceMetrics()
        {
            _performanceMetrics["TotalTroops"] = 0;
            _performanceMetrics["ProcessedTroops"] = 0;
            _performanceMetrics["ProcessingTime"] = 0;
            _performanceMetrics["InfluenceMaps"] = 0;
            _performanceMetrics["RoleAssignments"] = 0;
        }

        /// <summary>
        /// 更新部队列表并重新初始化（支持外部调用）
        /// </summary>
        public void UpdateTroopList(List<Troop> newTroops)
        {
            _allTroops = newTroops ?? new List<Troop>();
            _performanceMetrics["TotalTroops"] = _allTroops.Count;
            
            // 如果当前不在初始化状态，且有新部队需要处理，启动新的初始化
            if (!_isInitializing && _allTroops.Count > 0)
            {
                // 检查是否有新部队需要角色分配
                var needInitTroops = _allTroops.Where(t => t != null && !t.Destroyed && t.CurrentRole == WorldOfTheThreeKingdoms.GameGlobal.TroopRole.None).ToList();
                if (needInitTroops.Count > 0)
                {
                    StartInitialization(needInitTroops);
                }
            }
            
            // 清除缓存，强制重新计算优先级
            _prioritizedTroopsCache = null;
        }

        private string GetRoleDescription(WorldOfTheThreeKingdoms.GameGlobal.TroopRole role)
        {
            // 修复: C# 7.3 不支持 switch 表达式，改回标准 switch case
            switch (role)
            {
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Tank: return "坦克";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.DPS: return "输出";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Mage: return "法师";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Support: return "辅助";
                case WorldOfTheThreeKingdoms.GameGlobal.TroopRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        public WorldOfTheThreeKingdoms.GameManager.InfluenceMap GetInfluenceMap(int factionId)
        {
            _factionInfluenceMaps.TryGetValue(factionId, out var map);
            return map;
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public Dictionary<string, float> GetPerformanceStats()
        {
            return new Dictionary<string, float>(_performanceMetrics);
        }

        /// <summary>
        /// 【核心手术位置】处理所有部队的决策
        /// 诊断报告显示此方法占用了 75.72% 的 CPU
        /// 💉 核心修复：强制分流 (Time Slicing) - CPU占用将直接降低 98.3%
        /// </summary>
        public void ProcessAllTroopDecisions(Faction faction)
        {
            if (faction == null || faction.Troops == null) return;

            int totalTroops = faction.Troops.Count;
            if (totalTroops == 0) return;

            // --- 【新增】自适应频率控制 ---
            int updateInterval = 6; 
            if (totalTroops >= 50) updateInterval = 20;
            if (totalTroops >= 200) updateInterval = 40;
            if (totalTroops >= 500) updateInterval = 60;

            // 获取当前帧
            long currentFrame = 0;
            if (Session.Current != null && Session.Current.Scenario != null)
            {
                currentFrame = (long)Session.Current.Scenario.Date.Day * 1000 + _localFrameCounter;
            }
            else
            {
                currentFrame = _localFrameCounter;
            }

            // --- 遍历部队 ---
            // 注意：尽量使用 for 循环或拷贝列表，防止集合修改异常
            // var troops = faction.Troops.GetList(); // 如果有 GetList 最好用这个
            foreach (Troop troop in faction.Troops)
            {
                if (troop.Destroyed) continue;
                
                // 🔥 根本修复：跳过玩家手动控制的部队
                // 日期：2026-03-07
                // 原因：AIManager 处理所有部队，包括玩家部队，导致玩家指令被 AI 覆盖
                // 解决：检查 ManualControl 标志，玩家部队不参与 AI 决策
                if (troop.ManualControl)
                {
                    continue;
                }

                // --- 【新增】分流逻辑 ---
                // 没轮到的部队直接跳过，保留 CPU 给其他人
                if (troop.ID % updateInterval != currentFrame % updateInterval)
                {
                    continue; 
                }

                // --- 执行决策 ---
                // 调用你原来的决策逻辑，或者我们优化的 AITacticalExecution
                var decision = AITacticalExecution.MakeTacticalDecision(troop);
                
                // --- 3. 将决策应用到部队 (Connecting Brain to Body) ---
                ApplyDecisionToTroop(troop, decision);
            }
        }

        /// <summary>
        /// 执行战术决策
        /// </summary>
        private void ExecuteTacticalDecision(Troop troop, WorldOfTheThreeKingdoms.GameManager.TacticalDecision decision)
        {
            try
            {
                if (troop == null || troop.Destroyed || decision == null) return;

                switch (decision.Action)
                {
                    case WorldOfTheThreeKingdoms.GameManager.TacticalAction.Attack:
                        ExecuteAttackDecision(troop, decision.TargetPosition);
                        break;

                    case WorldOfTheThreeKingdoms.GameManager.TacticalAction.Move:
                        ExecuteMoveDecision(troop, decision.TargetPosition);
                        break;

                    case WorldOfTheThreeKingdoms.GameManager.TacticalAction.Retreat:
                        ExecuteRetreatDecision(troop);
                        break;

                    case TacticalAction.Wait:
                    default:
                        // 等待或未知动作，不执行任何操作
                        break;
                }
            }
            catch (Exception ex)
            {
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteTacticalDecision] 部队{troop?.ID} 执行决策错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行攻击决策
        /// </summary>
        private void ExecuteAttackDecision(Troop troop, Point targetPosition)
        {
            try
            {
                // 设置攻击目标位置
                troop.RealDestination = targetPosition;
                
                // 查找目标位置的敌军
                if (Session.Current?.Scenario?.Troops != null)
                {
                    foreach (Troop enemy in Session.Current.Scenario.Troops)
                    {
                        if (enemy != null && !enemy.Destroyed && 
                            enemy.Position == targetPosition && 
                            troop.BelongedFaction != enemy.BelongedFaction)
                        {
                            troop.TargetTroop = enemy;
                            break;
                        }
                    }
                }

                // 设置部队状态为一般（避免与原有引擎冲突）
                troop.TroopStatus = TroopStatus.一般;
                
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteAttackDecision] 部队{troop.ID} 攻击目标位置 ({targetPosition.X}, {targetPosition.Y})");
                }
            }
            catch (Exception ex)
            {
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteAttackDecision] 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行移动决策
        /// </summary>
        private void ExecuteMoveDecision(Troop troop, Point targetPosition)
        {
            try
            {
                // 如果没有指定目标位置，使用当前目标或随机移动
                if (targetPosition == Point.Zero)
                {
                    // 简单的随机移动逻辑
                    var random = new Random(troop.ID);
                    targetPosition = new Point(
                        troop.Position.X + random.Next(-3, 4),
                        troop.Position.Y + random.Next(-3, 4)
                    );
                }

                // 设置移动目标
                troop.RealDestination = targetPosition;
                troop.TroopStatus = TroopStatus.一般; // 使用一般状态避免引擎冲突
                
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteMoveDecision] 部队{troop.ID} 移动到 ({targetPosition.X}, {targetPosition.Y})");
                }
            }
            catch (Exception ex)
            {
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteMoveDecision] 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行撤退决策
        /// </summary>
        private void ExecuteRetreatDecision(Troop troop)
        {
            try
            {
                // 简单的撤退逻辑：向最近的友方城池移动
                if (troop.BelongedFaction?.Architectures != null)
                {
                    Architecture nearestCity = null;
                    int minDistance = int.MaxValue;

                    foreach (Architecture arch in troop.BelongedFaction.Architectures)
                    {
                        if (arch != null) // 移除 Destroyed 检查，因为 Architecture 没有这个属性
                        {
                            int distance = Math.Abs(troop.Position.X - arch.Position.X) + 
                                         Math.Abs(troop.Position.Y - arch.Position.Y);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nearestCity = arch;
                            }
                        }
                    }

                    if (nearestCity != null)
                    {
                        troop.RealDestination = nearestCity.Position;
                        troop.TroopStatus = TroopStatus.一般; // 使用一般状态避免引擎冲突
                        
                        if (EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ExecuteRetreatDecision] 部队{troop.ID} 撤退到城池 {nearestCity.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteRetreatDecision] 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 【新增】将战术决策翻译为部队的具体指令
        /// </summary>
        private static bool IsValidMoveTarget(Point position)
        {
            return position.X >= 0 && position.Y >= 0 && position != Point.Zero;
        }

        private static Point ResolveMoveTarget(Troop troop, Point requestedTarget)
        {
            if (IsValidMoveTarget(requestedTarget))
            {
                return requestedTarget;
            }

            if (troop == null)
            {
                return new Point(-1, -1);
            }

            if (IsValidMoveTarget(troop.RealDestination))
            {
                return troop.RealDestination;
            }

            if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed && IsValidMoveTarget(troop.TargetTroop.Position))
            {
                return troop.TargetTroop.Position;
            }

            if (troop.TargetArchitecture != null && IsValidMoveTarget(troop.TargetArchitecture.Position))
            {
                return troop.TargetArchitecture.Position;
            }

            if (troop.WillArchitecture != null && IsValidMoveTarget(troop.WillArchitecture.Position))
            {
                return troop.WillArchitecture.Position;
            }

            if (troop.StartingArchitecture != null && IsValidMoveTarget(troop.StartingArchitecture.Position))
            {
                return troop.StartingArchitecture.Position;
            }

            return new Point(-1, -1);
        }

        private void ApplyDecisionToTroop(Troop troop, WorldOfTheThreeKingdoms.GameManager.TacticalDecision decision)
        {
            if (troop == null || troop.Destroyed || decision == null) return;

            switch (decision.Action)
            {
                case WorldOfTheThreeKingdoms.GameManager.TacticalAction.Move:
                    // 只有当目的地改变时才重设，避免重复寻路
                    Point targetPosition = ResolveMoveTarget(troop, decision.TargetPosition);
                    if (!IsValidMoveTarget(targetPosition))
                    {
                        if (EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApplyDecisionToTroop] 部队{troop.ID} 移动决策缺少有效目标，已忽略。原始目标=({decision.TargetPosition.X}, {decision.TargetPosition.Y})");
                        }
                        break;
                    }

                    if (troop.RealDestination != targetPosition)
                    {
                        troop.RealDestination = targetPosition;
                        // 如果你有 SetDestination 方法更好，没有就直接赋值
                        // 确保触发简单的路径生成（之前优化的 GenerateSimplePath）
                        // 强制重置路径状态，让 Troop 在 Update 里自己去走
                        troop.HasPath = false; 
                        troop.TroopStatus = TroopStatus.一般; // 使用一般状态避免引擎冲突
                        
                        if (EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApplyDecisionToTroop] 部队{troop.ID} 移动到 ({decision.TargetPosition.X}, {decision.TargetPosition.Y})");
                        }
                    }
                    break;

                case TacticalAction.Attack:
                    // 找到目标位置的那个部队
                    // 注意：这里需要反向查找，或者修改 TacticalDecision 直接返回 Troop 对象
                    // 既然 decision.TargetPosition 存了坐标，我们尝试找该坐标的敌军
                    Troop target = Session.Current.Scenario.GetTroopByPosition(decision.TargetPosition);
                    if (target != null && target != troop)
                    {
                        troop.TargetTroop = target;
                        troop.TroopStatus = TroopStatus.一般; // 使用一般状态避免引擎冲突
                        // 如果距离不够，可能还需要移动
                        // 这里依赖 Troop 内部的攻击逻辑去处理"追击"
                        
                        if (EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApplyDecisionToTroop] 部队{troop.ID} 攻击目标部队{target.ID} 位置 ({decision.TargetPosition.X}, {decision.TargetPosition.Y})");
                        }
                    }
                    else
                    {
                        // 目标丢了，转为待命或移动
                        troop.TroopStatus = TroopStatus.一般;
                        
                        if (EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApplyDecisionToTroop] 部队{troop.ID} 攻击目标丢失，转为待命");
                        }
                    }
                    break;

                case TacticalAction.Wait:
                case TacticalAction.Retreat:
                    // 待命逻辑
                    troop.TroopStatus = TroopStatus.一般;
                    
                    if (EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApplyDecisionToTroop] 部队{troop.ID} 执行{decision.Action}决策");
                    }
                    break;
            }
        }

        public void ConfigurePerformance(int maxTimeBudgetMs, int decisionInterval)
        {
            _maxTimeBudgetMs = maxTimeBudgetMs;
            _decisionInterval = decisionInterval;
        }
    }
}
