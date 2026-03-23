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
    /// 战术编组
    /// </summary>
    public class TacticalGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<Troop> Troops { get; set; } = new List<Troop>();
        public Point TargetPosition { get; set; }
        public string Mission { get; set; } = "巡逻";
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public int TotalStrength => Troops.Sum(t => t?.FightingForce ?? 0);
        public Person Commander => Troops.FirstOrDefault(t => t?.Leader?.Command == Troops.Max(tr => tr?.Leader?.Command ?? 0))?.Leader;
    }

    /// <summary>
    /// AI战术协调器 - 完整版本
    /// </summary>
    public class AITacticalCoordinator
    {
        public static AITacticalCoordinator Instance { get; private set; }
        
        private Dictionary<int, List<string>> _tacticalPlans;
        private Dictionary<int, List<TacticalGroup>> _tacticalGroups;
        private int _nextGroupId = 1;
        
        public AITacticalCoordinator()
        {
            Instance = this;
            _tacticalPlans = new Dictionary<int, List<string>>();
            _tacticalGroups = new Dictionary<int, List<TacticalGroup>>();
        }

        /// <summary>
        /// 组织战术编组
        /// </summary>
        public void OrganizeTacticalGroups(Faction faction)
        {
            try
            {
                if (faction?.Troops == null) return;

                // 清除旧的编组
                if (!_tacticalGroups.ContainsKey(faction.ID))
                {
                    _tacticalGroups[faction.ID] = new List<TacticalGroup>();
                }
                else
                {
                    _tacticalGroups[faction.ID].Clear();
                }

                var availableTroops = new List<Troop>();
                foreach (var obj in faction.Troops.GetList())
                {
                    if (obj is Troop t && !t.Destroyed && t.Leader != null)
                    {
                        availableTroops.Add(t);
                    }
                }

                if (availableTroops.Count == 0) return;

                // 按照指挥官能力排序
                availableTroops = availableTroops
                    .OrderByDescending(t => t.Leader.Command)
                    .ToList();

                // 创建编组
                while (availableTroops.Count > 0)
                {
                    var group = CreateTacticalGroup(faction, availableTroops);
                    if (group != null && group.Troops.Count > 0)
                    {
                        _tacticalGroups[faction.ID].Add(group);
                        
                        // 从可用部队中移除已编组的部队
                        foreach (var troop in group.Troops)
                        {
                            availableTroops.Remove(troop);
                        }
                    }
                    else
                    {
                        break; // 避免无限循环
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AITacticalCoordinator] 为势力 {faction.Name} 创建了 {_tacticalGroups[faction.ID].Count} 个战术编组");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrganizeTacticalGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建战术编组
        /// </summary>
        private TacticalGroup CreateTacticalGroup(Faction faction, List<Troop> availableTroops)
        {
            try
            {
                if (availableTroops.Count == 0) return null;

                var group = new TacticalGroup
                {
                    GroupId = _nextGroupId++,
                    GroupName = $"编组{_nextGroupId - 1}"
                };

                // 选择指挥官（指挥能力最高的）
                var commander = availableTroops.First();
                group.Troops.Add(commander);

                // 添加附近的部队到同一编组（最多5个部队）
                var commanderPos = commander.Position;
                var nearbyTroops = availableTroops
                    .Where(t => t != commander)
                    .Where(t => Session.Current.Scenario.GetSimpleDistance(t.Position, commanderPos) <= 3)
                    .Take(4) // 最多再加4个，总共5个
                    .ToList();

                group.Troops.AddRange(nearbyTroops);

                // 确定编组任务
                group.Mission = DetermineGroupMission(faction, group);

                // 确定目标位置
                group.TargetPosition = DetermineGroupTarget(faction, group);

                return group;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateTacticalGroup] 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 确定编组任务
        /// </summary>
        private string DetermineGroupMission(Faction faction, TacticalGroup group)
        {
            try
            {
                if (group?.Commander == null) return "巡逻";

                // 根据指挥官特点和势力状况确定任务
                if (group.Commander.Command > 80 && group.TotalStrength > 1000)
                {
                    return "主力攻击";
                }
                else if (group.Commander.Intelligence > 80)
                {
                    return "侦察探索";
                }
                else if (faction.ArchitectureCount > faction.TroopCount)
                {
                    return "防御巡逻";
                }
                else
                {
                    return "支援作战";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetermineGroupMission] 错误: {ex.Message}");
                return "巡逻";
            }
        }

        /// <summary>
        /// 确定编组目标
        /// </summary>
        private Point DetermineGroupTarget(Faction faction, TacticalGroup group)
        {
            try
            {
                if (group?.Troops == null || group.Troops.Count == 0) 
                    return Point.Zero;

                var groupCenter = CalculateGroupCenter(group);

                switch (group.Mission)
                {
                    case "主力攻击":
                        return FindNearestEnemyArchitecture(faction, groupCenter) ?? groupCenter;
                    
                    case "侦察探索":
                        return FindUnexploredArea(faction, groupCenter);
                    
                    case "防御巡逻":
                        return FindDefensivePosition(faction, groupCenter);
                    
                    case "支援作战":
                        return FindAlliedPosition(faction, groupCenter);
                    
                    default:
                        return groupCenter;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetermineGroupTarget] 错误: {ex.Message}");
                return Point.Zero;
            }
        }

        /// <summary>
        /// 计算编组中心位置
        /// </summary>
        private Point CalculateGroupCenter(TacticalGroup group)
        {
            if (group?.Troops == null || group.Troops.Count == 0) return Point.Zero;

            int totalX = 0, totalY = 0;
            foreach (var troop in group.Troops)
            {
                totalX += troop.Position.X;
                totalY += troop.Position.Y;
            }

            return new Point(totalX / group.Troops.Count, totalY / group.Troops.Count);
        }

        /// <summary>
        /// 寻找最近的敌方建筑
        /// </summary>
        private Point? FindNearestEnemyArchitecture(Faction faction, Point fromPosition)
        {
            try
            {
                var enemyArchs = new List<Architecture>();
                foreach (var obj in Session.Current.Scenario.Architectures.GetList())
                {
                    if (obj is Architecture a && a.BelongedFaction != null && !faction.IsFriendly(a.BelongedFaction))
                    {
                        enemyArchs.Add(a);
                    }
                }

                if (enemyArchs.Count == 0) return null;

                Architecture nearest = null;
                float minDistance = float.MaxValue;
                foreach (var arch in enemyArchs)
                {
                    float distance = Session.Current.Scenario.GetSimpleDistance(fromPosition, arch.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = arch;
                    }
                }

                return nearest.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearestEnemyArchitecture] 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 寻找未探索区域
        /// </summary>
        private Point FindUnexploredArea(Faction faction, Point fromPosition)
        {
            // 简化实现：随机选择一个方向
            int offsetX = GameObject.Random(21) - 10; // -10 到 +10
            int offsetY = GameObject.Random(21) - 10;
            
            return new Point(
                Math.Max(0, Math.Min(Session.Current.Scenario.ScenarioMap.MapDimensions.X - 1, fromPosition.X + offsetX)),
                Math.Max(0, Math.Min(Session.Current.Scenario.ScenarioMap.MapDimensions.Y - 1, fromPosition.Y + offsetY))
            );
        }

        /// <summary>
        /// 寻找防御位置
        /// </summary>
        private Point FindDefensivePosition(Faction faction, Point fromPosition)
        {
            try
            {
                // 寻找最近的己方建筑
                var ownArchs = faction.Architectures.GetList();
                if (ownArchs.Count == 0) return fromPosition;

                // 【修复】手动实现排序逻辑，避免使用LINQ的OrderBy
                Architecture nearest = null;
                int minDistance = int.MaxValue;
                
                foreach (var obj in ownArchs)
                {
                    if (obj is Architecture arch)
                    {
                        int distance = Session.Current.Scenario.GetSimpleDistance(fromPosition, arch.Position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearest = arch;
                        }
                    }
                }

                return nearest?.Position ?? fromPosition;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindDefensivePosition] 错误: {ex.Message}");
                return fromPosition;
            }
        }

        /// <summary>
        /// 寻找友军位置
        /// </summary>
        private Point FindAlliedPosition(Faction faction, Point fromPosition)
        {
            try
            {
                // 寻找其他友军部队
                var alliedTroops = new List<Troop>();
                foreach (var obj in faction.Troops.GetList())
                {
                    if (obj is Troop t && !t.Destroyed)
                    {
                        float distance = Session.Current.Scenario.GetSimpleDistance(fromPosition, t.Position);
                        if (distance > 2)
                        {
                            alliedTroops.Add(t);
                        }
                    }
                }

                if (alliedTroops.Count == 0) return fromPosition;

                var target = alliedTroops[GameObject.Random(alliedTroops.Count)];
                return target.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindAlliedPosition] 错误: {ex.Message}");
                return fromPosition;
            }
        }

        /// <summary>
        /// 更新战术编组
        /// </summary>
        public void UpdateTacticalGroups(Faction faction)
        {
            try
            {
                if (!_tacticalGroups.ContainsKey(faction.ID)) return;

                var groups = _tacticalGroups[faction.ID];
                foreach (var group in groups.ToList())
                {
                    UpdateSingleGroup(faction, group);
                }

                // 清理无效编组
                groups.RemoveAll(g => !g.IsActive || g.Troops.Count == 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateTacticalGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新单个编组
        /// </summary>
        private void UpdateSingleGroup(Faction faction, TacticalGroup group)
        {
            try
            {
                if (group?.Troops == null) return;

                // 移除已销毁的部队
                group.Troops.RemoveAll(t => t == null || t.Destroyed);

                if (group.Troops.Count == 0)
                {
                    group.IsActive = false;
                    return;
                }

                // 检查是否需要重新分配任务
                if ((DateTime.Now - group.CreatedTime).TotalMinutes > 10)
                {
                    group.Mission = DetermineGroupMission(faction, group);
                    group.TargetPosition = DetermineGroupTarget(faction, group);
                    group.CreatedTime = DateTime.Now;
                }

                // 协调编组内部队移动
                CoordinateGroupMovement(group);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateSingleGroup] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 协调编组移动
        /// </summary>
        private void CoordinateGroupMovement(TacticalGroup group)
        {
            try
            {
                if (group?.Troops == null || group.Troops.Count == 0) return;

                var targetPos = group.TargetPosition;
                
                foreach (var troop in group.Troops)
                {
                    if (troop?.Leader == null) continue;

                    // 如果部队距离目标较远，设置移动目标
                    float distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, targetPos);
                    if (distance > 2)
                    {
                        troop.Destination = targetPos;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CoordinateGroupMovement] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除所有编组
        /// </summary>
        public void ClearAllGroups()
        {
            try
            {
                _tacticalGroups.Clear();
                System.Diagnostics.Debug.WriteLine("[AITacticalCoordinator] 所有战术编组已清除");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClearAllGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 协调势力的战术
        /// </summary>
        public void CoordinateTactics(Faction faction)
        {
            try
            {
                if (faction == null) return;
                
                var plans = new List<string>();
                
                // 基于势力状态制定战术计划
                if (faction.TroopCount > 0)
                {
                    plans.Add("部队协调");
                }
                
                if (faction.ArchitectureCount > 1)
                {
                    plans.Add("城市防御");
                }
                
                _tacticalPlans[faction.ID] = plans;
                
                System.Diagnostics.Debug.WriteLine($"[AITacticalCoordinator] 为势力 {faction.Name} 制定了 {plans.Count} 个战术计划");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AITacticalCoordinator] 协调战术时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取战术计划
        /// </summary>
        public List<string> GetTacticalPlans(int factionId)
        {
            return _tacticalPlans.ContainsKey(factionId) ? _tacticalPlans[factionId] : new List<string>();
        }
        
        /// <summary>
        /// 清除战术计划
        /// </summary>
        public void ClearPlans(int factionId)
        {
            if (_tacticalPlans.ContainsKey(factionId))
            {
                _tacticalPlans[factionId].Clear();
            }
        }

        /// <summary>
        /// 获取编组统计信息
        /// </summary>
        public string GetGroupStats(int factionId)
        {
            try
            {
                if (!_tacticalGroups.ContainsKey(factionId))
                    return "无编组信息";

                var groups = _tacticalGroups[factionId];
                var activeGroups = groups.Where(g => g.IsActive).ToList();

                string stats = $"战术编组统计:\n";
                stats += $"总编组数: {groups.Count}\n";
                stats += $"活跃编组: {activeGroups.Count}\n";
                stats += $"总部队数: {activeGroups.Sum(g => g.Troops.Count)}\n";
                stats += $"总战力: {activeGroups.Sum(g => g.TotalStrength)}\n";

                foreach (var group in activeGroups.Take(3)) // 显示前3个编组
                {
                    stats += $"\n编组 {group.GroupName}:\n";
                    stats += $"  任务: {group.Mission}\n";
                    stats += $"  部队数: {group.Troops.Count}\n";
                    stats += $"  战力: {group.TotalStrength}\n";
                    stats += $"  指挥官: {group.Commander?.Name ?? "无"}\n";
                }

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetGroupStats] 错误: {ex.Message}");
                return "获取统计信息失败";
            }
        }
    }
}