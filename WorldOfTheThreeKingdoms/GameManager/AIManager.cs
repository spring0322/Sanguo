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
    /// 版本: 2.1 - 适配 C# 7.3 / MonoGame 环境
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
        // 使用完整限定名以避免命名空间冲突
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

        public AIManager(List<Troop> troops)
        {
            _allTroops = troops ?? new List<Troop>();
            InitializePerformanceMetrics();
        }

        private void InitializePerformanceMetrics()
        {
            _performanceMetrics["TotalTroops"] = 0;
            _performanceMetrics["ProcessedTroops"] = 0;
            _performanceMetrics["ProcessingTime"] = 0;
            _performanceMetrics["InfluenceMaps"] = 0;
            _performanceMetrics["RoleAssignments"] = 0;
        }

        public void UpdateTroopList(List<Troop> newTroops)
        {
            _allTroops = newTroops ?? new List<Troop>();
            _performanceMetrics["TotalTroops"] = _allTroops.Count;
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

                // 智能角色分配
                AssignRolesIntelligently(currentFrame);

                // 处理AI部队（按优先级排序）
                var prioritizedTroops = GetPrioritizedTroops();

                foreach (var troop in prioritizedTroops)
                {
                    if (troop == null || troop.Destroyed) continue;

                    // 分帧处理机制
                    if (currentFrame % _decisionInterval != troop.ID % _decisionInterval) continue;

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

                if (currentFrame % 60 == 0 && System.Diagnostics.Debugger.IsAttached)
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
        /// 智能角色分配系统
        /// </summary>
        private void AssignRolesIntelligently(int currentFrame)
        {
            int assignedRoles = 0;

            foreach (var troop in _allTroops)
            {
                if (troop == null || troop.Destroyed) continue;

                // 检查是否需要重新分配角色
                bool needsRoleAssignment = troop.CurrentRole == AIRole.None;

                if (!needsRoleAssignment && _roleAssignmentTimestamps.ContainsKey(troop.ID))
                {
                    var timeSinceAssignment = DateTime.Now - _roleAssignmentTimestamps[troop.ID];
                    needsRoleAssignment = timeSinceAssignment > _roleReassignmentInterval;
                }

                if (needsRoleAssignment)
                {
                    var newRole = AIRoleSelector.GetBestRole(troop);
                    if (newRole != troop.CurrentRole)
                    {
                        troop.CurrentRole = newRole;
                        if (_roleAssignmentTimestamps.ContainsKey(troop.ID))
                            _roleAssignmentTimestamps[troop.ID] = DateTime.Now;
                        else
                            _roleAssignmentTimestamps.Add(troop.ID, DateTime.Now);

                        assignedRoles++;

                        System.Diagnostics.Debug.WriteLine($"[AIManager] 为部队 {troop.ID} 分配角色: {GetRoleDescription(newRole)}");
                    }
                }
            }

            _performanceMetrics["RoleAssignments"] = assignedRoles;
        }

        /// <summary>
        /// 获取按优先级排序的部队列表
        /// </summary>
        private List<Troop> GetPrioritizedTroops()
        {
            return _allTroops.OrderByDescending(troop => GetTroopPriority(troop)).ToList();
        }

        /// <summary>
        /// 计算部队优先级
        /// </summary>
        private int GetTroopPriority(Troop troop)
        {
            if (troop == null || troop.Destroyed) return 0;

            int priority = 0;

            // 基础优先级：基于部队重要性
            priority += troop.FightingForce / 100;

            // 角色优先级加成
            switch (troop.CurrentRole)
            {
                case AIRole.Support:
                    priority += 50; // 辅助优先
                    break;
                case AIRole.Tank:
                    priority += 40; // 坦克次之
                    break;
                case AIRole.Mage:
                    priority += 30; // 法师
                    break;
                case AIRole.DPS:
                    priority += 20; // DPS
                    break;
                case AIRole.Logistics:
                    priority += 10; // 后勤最后
                    break;
            }

            // 战斗状态加成
            if (troop.Action == TroopAction.Attack || troop.Action == TroopAction.BeAttacked)
            {
                priority += 100; // 正在战斗的部队优先处理
            }

            return priority;
        }

        private void UpdateInfluenceMaps()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions == null) return;

                int mapWidth = Session.Current.Scenario.ScenarioMap?.MapDimensions.X ?? 200;
                int mapHeight = Session.Current.Scenario.ScenarioMap?.MapDimensions.Y ?? 200;

                int updatedMaps = 0;

                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (faction == null || faction.Destroyed) continue;

                    if (!_factionInfluenceMaps.TryGetValue(faction.ID, out var influenceMap))
                    {
                        influenceMap = new WorldOfTheThreeKingdoms.GameManager.InfluenceMap(mapWidth, mapHeight);
                        _factionInfluenceMaps[faction.ID] = influenceMap;
                    }

                    influenceMap.Refresh(faction);
                    updatedMaps++;
                }

                _performanceMetrics["InfluenceMaps"] = updatedMaps;
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
                // Fallback to original AI processing since AITacticalExecution and AISkillSelector
                // are currently commented out due to dependency issues
                troop.ProcessAI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIManager.ProcessEnhancedAI] 错误: {ex.Message}");
                troop.ProcessAI(); // 回退到原有AI
            }
        }

        private void ProcessTankAI(Troop troop)
        {
            // 坦克AI：寻找需要保护的友军或前线位置
            var nearbyAllies = GetNearbyAllies(troop, 4);
            var nearbyEnemies = GetNearbyEnemies(troop, 3);

            if (nearbyEnemies.Count > 0 && nearbyAllies.Count > 0)
            {
                // 移动到友军和敌军之间的位置
                var protectionPosition = CalculateProtectionPosition(troop, nearbyAllies, nearbyEnemies);
                if (protectionPosition != Point.Zero)
                {
                    troop.RealDestination = protectionPosition;
                    return;
                }
            }

            ProcessDefaultAI(troop);
        }

        private void ProcessDPSAI(Troop troop)
        {
            // DPS AI：寻找最佳输出位置
            var nearbyEnemies = GetNearbyEnemies(troop, 5);
            if (nearbyEnemies.Count > 0)
            {
                // 选择最弱的敌人作为目标
                var weakestEnemy = nearbyEnemies.OrderBy(e => e.FightingForce).First();
                troop.RealDestination = weakestEnemy.Position;
                return;
            }

            ProcessDefaultAI(troop);
        }

        private void ProcessMageAI(Troop troop)
        {
            // 法师AI：保持安全距离，寻找AOE机会
            var nearbyEnemies = GetNearbyEnemies(troop, 6);
            var closeEnemies = GetNearbyEnemies(troop, 2);

            if (closeEnemies.Count > 0)
            {
                // 如果敌人太近，先撤退
                var safePosition = FindSafePosition(troop, closeEnemies);
                if (safePosition != Point.Zero)
                {
                    troop.RealDestination = safePosition;
                    return;
                }
            }
            else if (nearbyEnemies.Count >= 2)
            {
                // 寻找能攻击多个敌人的位置
                var optimalPosition = FindOptimalCastingPosition(troop, nearbyEnemies);
                if (optimalPosition != Point.Zero)
                {
                    troop.RealDestination = optimalPosition;
                    return;
                }
            }

            ProcessDefaultAI(troop);
        }

        private void ProcessSupportAI(Troop troop)
        {
            // 辅助AI：寻找需要支援的友军
            var nearbyAllies = GetNearbyAllies(troop, 6);
            var injuredAllies = nearbyAllies.Where(ally => ally.Morale < 80).ToList();

            if (injuredAllies.Count > 0)
            {
                // 移动到最需要帮助的友军附近
                var mostInjured = injuredAllies.OrderBy(ally => ally.Morale).First();
                var supportPosition = FindSupportPosition(troop, mostInjured);
                if (supportPosition != Point.Zero)
                {
                    troop.RealDestination = supportPosition;
                    return;
                }
            }

            ProcessDefaultAI(troop);
        }

        private void ProcessLogisticsAI(Troop troop)
        {
            // 后勤AI：远离战斗，执行支援任务
            var nearbyEnemies = GetNearbyEnemies(troop, 8);

            if (nearbyEnemies.Count > 0)
            {
                // 远离敌人
                var safePosition = FindSafePosition(troop, nearbyEnemies);
                if (safePosition != Point.Zero)
                {
                    troop.RealDestination = safePosition;
                    return;
                }
            }

            ProcessDefaultAI(troop);
        }

        private void ProcessDefaultAI(Troop troop)
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

        // 辅助方法
        private List<Troop> GetNearbyAllies(Troop troop, int radius)
        {
            var allies = new List<Troop>();
            if (troop.BelongedFaction == null) return allies;

            foreach (var other in _allTroops)
            {
                if (other == null || other == troop || other.Destroyed) continue;
                if (!troop.BelongedFaction.IsFriendly(other.BelongedFaction)) continue;

                int distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, other.Position);
                if (distance <= radius)
                {
                    allies.Add(other);
                }
            }

            return allies;
        }

        private List<Troop> GetNearbyEnemies(Troop troop, int radius)
        {
            var enemies = new List<Troop>();
            if (troop.BelongedFaction == null) return enemies;

            foreach (var other in _allTroops)
            {
                if (other == null || other == troop || other.Destroyed) continue;
                if (troop.BelongedFaction.IsFriendly(other.BelongedFaction)) continue;

                int distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, other.Position);
                if (distance <= radius)
                {
                    enemies.Add(other);
                }
            }

            return enemies;
        }

        private Point CalculateProtectionPosition(Troop troop, List<Troop> allies, List<Troop> enemies)
        {
            // 简化实现：返回友军和敌军之间的中点
            if (allies.Count == 0 || enemies.Count == 0) return Point.Zero;

            var allyCenter = new Point(
                (int)allies.Average(a => a.Position.X),
                (int)allies.Average(a => a.Position.Y)
            );

            var enemyCenter = new Point(
                (int)enemies.Average(e => e.Position.X),
                (int)enemies.Average(e => e.Position.Y)
            );

            return new Point(
                (allyCenter.X + enemyCenter.X) / 2,
                (allyCenter.Y + enemyCenter.Y) / 2
            );
        }

        private Point FindSafePosition(Troop troop, List<Troop> threats)
        {
            // 简化实现：向远离威胁的方向移动
            if (threats.Count == 0) return Point.Zero;

            var threatCenter = new Point(
                (int)threats.Average(t => t.Position.X),
                (int)threats.Average(t => t.Position.Y)
            );

            int deltaX = troop.Position.X - threatCenter.X;
            int deltaY = troop.Position.Y - threatCenter.Y;

            // 向远离威胁的方向移动
            return new Point(
                troop.Position.X + Math.Sign(deltaX) * 2,
                troop.Position.Y + Math.Sign(deltaY) * 2
            );
        }

        private Point FindOptimalCastingPosition(Troop troop, List<Troop> enemies)
        {
            // 简化实现：返回能覆盖最多敌人的位置
            return enemies.Count > 0 ? enemies[0].Position : Point.Zero;
        }

        private Point FindSupportPosition(Troop troop, Troop ally)
        {
            // 简化实现：移动到友军附近
            return new Point(ally.Position.X + 1, ally.Position.Y);
        }

        private void UpdatePerformanceMetrics()
        {
            _performanceMetrics["ProcessedTroops"] = _processedThisFrame;
            _performanceMetrics["ProcessingTime"] = _totalProcessingTime;

            // 每5秒更新一次统计信息
            if (DateTime.Now - _lastStatsUpdate > TimeSpan.FromSeconds(5))
            {
                _lastStatsUpdate = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"[AIManager] 性能统计: {GetPerformanceStats()}");
            }
        }

        private string GetRoleDescription(GameObjects.AI.AIRole role)
        {
            // 修复: C# 7.3 不支持 switch 表达式，改回标准 switch case
            switch (role)
            {
                case GameObjects.AI.AIRole.Tank: return "坦克";
                case GameObjects.AI.AIRole.DPS: return "输出";
                case GameObjects.AI.AIRole.Mage: return "法师";
                case GameObjects.AI.AIRole.Support: return "辅助";
                case GameObjects.AI.AIRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        public WorldOfTheThreeKingdoms.GameManager.InfluenceMap GetInfluenceMap(int factionId)
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
            
            // 安全地获取记忆单位数量
            try
            {
                if (Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction != null && faction.MemoryMap != null)
                        {
                            var allGhosts = faction.MemoryMap.GetAllGhosts();
                            if (allGhosts != null)
                            {
                                totalMemoryUnits += allGhosts.Count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPerformanceStats] 获取记忆单位数量时出错: {ex.Message}");
            }

            return $"AI处理: {_performanceMetrics["ProcessedTroops"]}部队 用时: {_performanceMetrics["ProcessingTime"]:F2}ms " +
                   $"总数: {_performanceMetrics["TotalTroops"]} 影响力地图: {_performanceMetrics["InfluenceMaps"]}个 " +
                   $"角色分配: {_performanceMetrics["RoleAssignments"]}个 记忆单位: {totalMemoryUnits}个";
        }

        /// <summary>
        /// 获取详细的AI统计信息
        /// </summary>
        public Dictionary<string, object> GetDetailedStats()
        {
            var stats = new Dictionary<string, object>();

            // 基础统计
            stats["TotalTroops"] = _allTroops.Count;
            stats["ProcessedThisFrame"] = _processedThisFrame;
            stats["ProcessingTime"] = _totalProcessingTime;
            stats["InfluenceMaps"] = _factionInfluenceMaps.Count;

            // 角色分布统计
            var roleDistribution = new Dictionary<string, int>();
            foreach (var troop in _allTroops)
            {
                if (troop == null || troop.Destroyed) continue;

                string roleName = GetRoleDescription(troop.CurrentRole);
                if (roleDistribution.ContainsKey(roleName))
                    roleDistribution[roleName]++;
                else
                    roleDistribution.Add(roleName, 1);
            }
            stats["RoleDistribution"] = roleDistribution;

            // 性能指标
            stats["AverageProcessingTime"] = _allTroops.Count > 0 ? _totalProcessingTime / _allTroops.Count : 0;
            stats["TimeBudgetUtilization"] = (_totalProcessingTime / _maxTimeBudgetMs) * 100;

            return stats;
        }
    }
}