using GameManager;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GameObjects
{
    /// <summary>
    /// 部队位置缓存 - 用于O(1)极速查找指定位置的部队
    /// 比 Scenario.GetTroopByPosition 更快，因为使用直接数组访问
    /// 🔥 新增：整合城池视野系统，缓存每个势力可见的部队
    /// </summary>
    public static class MapPositionCache
    {
        // ============================================================================
        // 原有：物理位置缓存
        // ============================================================================
        private static Troop[,] _troopMap;
        private static int _width, _height;
        private static bool _initialized = false;

        // ============================================================================
        // 🔥 新增：势力视野缓存
        // ============================================================================
        
        // 每个势力能看到的位置（包括城池视野）
        private static Dictionary<int, HashSet<Point>> _factionVisiblePositions;
        
        // 每个势力能看到的部队
        private static Dictionary<int, HashSet<Troop>> _factionVisibleTroops;
        
        // 是否启用视野缓存
        private static bool _visionCacheEnabled = true;

        /// <summary>
        /// 每回合开始前，或者初始化时调用一次
        /// 从当前场景重新加载所有部队位置
        /// 🔥 新增：同时加载视野缓存
        /// </summary>
        public static void ReloadCache()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario?.ScenarioMap == null) return;

            _width = scenario.ScenarioMap.MapDimensions.X;
            _height = scenario.ScenarioMap.MapDimensions.Y;
            _troopMap = new Troop[_width, _height];

            // 遍历所有部队，将其位置记录到缓存中
            foreach (Troop t in scenario.Troops)
            {
                if (t != null && !t.Destroyed && IsValid(t.Position))
                {
                    _troopMap[t.Position.X, t.Position.Y] = t;
                }
            }

            // 🔥 新增：重新加载视野缓存
            if (_visionCacheEnabled)
            {
                ReloadVisionCache();
            }

            _initialized = true;
            System.Diagnostics.Debug.WriteLine($"[MapPositionCache] 缓存已刷新: {_width}x{_height}, 部队数={scenario.Troops.Count}");
        }

        /// <summary>
        /// 🔥 新增：重新加载视野缓存
        /// </summary>
        private static void ReloadVisionCache()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            // 初始化视野缓存
            _factionVisiblePositions = new Dictionary<int, HashSet<Point>>();
            _factionVisibleTroops = new Dictionary<int, HashSet<Troop>>();

            // 遍历所有势力
            foreach (Faction faction in scenario.Factions)
            {
                if (faction == null) continue;

                var visiblePositions = new HashSet<Point>();
                var visibleTroops = new HashSet<Troop>();

                // 1. 添加己方部队的视野
                foreach (Troop troop in faction.Troops)
                {
                    if (troop == null || troop.Destroyed) continue;
                    if (troop.ViewArea == null) continue;

                    foreach (Point p in troop.ViewArea.Area)
                    {
                        if (IsValid(p))
                        {
                            visiblePositions.Add(p);
                            
                            // 检查该位置是否有部队
                            Troop troopAtPos = _troopMap[p.X, p.Y];
                            if (troopAtPos != null && !troopAtPos.Destroyed)
                            {
                                visibleTroops.Add(troopAtPos);
                            }
                        }
                    }
                }

                // 2. 🔥 添加己方城池的视野
                foreach (Architecture arch in faction.Architectures)
                {
                    if (arch == null) continue;
                    if (arch.ViewArea == null) continue;

                    foreach (Point p in arch.ViewArea.Area)
                    {
                        if (IsValid(p))
                        {
                            visiblePositions.Add(p);
                            
                            // 检查该位置是否有部队
                            Troop troopAtPos = _troopMap[p.X, p.Y];
                            if (troopAtPos != null && !troopAtPos.Destroyed)
                            {
                                visibleTroops.Add(troopAtPos);
                            }
                        }
                    }
                }

                // 保存到缓存
                _factionVisiblePositions[faction.ID] = visiblePositions;
                _factionVisibleTroops[faction.ID] = visibleTroops;

#if DEBUG
                int enemyCount = visibleTroops.Count(t => !faction.IsFriendly(t.BelongedFaction));
                System.Diagnostics.Debug.WriteLine(
                    $"[MapPositionCache] {faction.Name} 视野缓存: " +
                    $"可见位置={visiblePositions.Count}, 可见部队={visibleTroops.Count}, 敌军={enemyCount}"
                );
#endif
            }
        }

        /// <summary>
        /// 当部队移动结束时，必须更新这个缓存！
        /// 🔥 新增：同时更新视野缓存
        /// </summary>
        /// <param name="t">移动的部队</param>
        /// <param name="oldPos">旧位置</param>
        /// <param name="newPos">新位置</param>
        public static void UpdateMove(Troop t, Point oldPos, Point newPos)
        {
            if (!_initialized) return;

            // 清除旧位置（仅当旧位置确实是该部队时）
            if (IsValid(oldPos) && _troopMap[oldPos.X, oldPos.Y] == t)
            {
                _troopMap[oldPos.X, oldPos.Y] = null;
            }

            // 设置新位置
            if (IsValid(newPos))
            {
                _troopMap[newPos.X, newPos.Y] = t;
            }

            // 🔥 新增：更新视野缓存
            if (_visionCacheEnabled && t.BelongedFaction != null)
            {
                UpdateFactionVision(t.BelongedFaction);
            }
        }

        /// <summary>
        /// 当部队被销毁时，从缓存中移除
        /// 🔥 新增：同时更新视野缓存
        /// </summary>
        /// <param name="t">被销毁的部队</param>
        public static void RemoveTroop(Troop t)
        {
            if (!_initialized || t == null) return;

            if (IsValid(t.Position) && _troopMap[t.Position.X, t.Position.Y] == t)
            {
                _troopMap[t.Position.X, t.Position.Y] = null;
            }

            // 🔥 新增：更新视野缓存
            if (_visionCacheEnabled && t.BelongedFaction != null)
            {
                UpdateFactionVision(t.BelongedFaction);
            }
        }

        /// <summary>
        /// 当新部队创建时，添加到缓存
        /// 🔥 新增：同时更新视野缓存
        /// </summary>
        /// <param name="t">新创建的部队</param>
        public static void AddTroop(Troop t)
        {
            if (!_initialized || t == null || t.Destroyed) return;

            if (IsValid(t.Position))
            {
                _troopMap[t.Position.X, t.Position.Y] = t;
            }

            // 🔥 新增：更新视野缓存
            if (_visionCacheEnabled && t.BelongedFaction != null)
            {
                UpdateFactionVision(t.BelongedFaction);
            }
        }

        /// <summary>
        /// O(1) 极速查找指定位置的部队
        /// </summary>
        /// <param name="p">要查找的位置</param>
        /// <returns>该位置的部队，如果没有则返回 null</returns>
        public static Troop GetTroopAt(Point p)
        {
            if (!_initialized || !IsValid(p)) return null;
            return _troopMap[p.X, p.Y];
        }

        /// <summary>
        /// 检查指定位置是否有部队
        /// </summary>
        /// <param name="p">要检查的位置</param>
        /// <returns>是否有部队</returns>
        public static bool HasTroopAt(Point p)
        {
            return GetTroopAt(p) != null;
        }

        /// <summary>
        /// 🔥 新增：移除指定位置的指定部队
        /// 日期：2026-03-10
        /// 用途：原子化位置更新时，确保只移除指定的部队
        /// </summary>
        public static void RemoveTroopAt(Point position, Troop troop)
        {
            if (!_initialized || !IsValid(position)) return;
            
            // 只有当该位置确实是该部队时才清除
            if (_troopMap[position.X, position.Y] == troop)
            {
                _troopMap[position.X, position.Y] = null;
            }
        }

        /// <summary>
        /// 🔥 新增：设置指定位置的部队
        /// 日期：2026-03-10
        /// 用途：原子化位置更新时，直接设置部队位置
        /// </summary>
        public static void SetTroopAt(Point position, Troop troop)
        {
            if (!_initialized || !IsValid(position)) return;
            
            _troopMap[position.X, position.Y] = troop;
        }

        /// <summary>
        /// 检查坐标是否在地图范围内
        /// </summary>
        private static bool IsValid(Point p)
        {
            return p.X >= 0 && p.X < _width && p.Y >= 0 && p.Y < _height;
        }

        /// <summary>
        /// 检查缓存是否已初始化
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 清除缓存（用于场景切换时）
        /// </summary>
        public static void Clear()
        {
            _troopMap = null;
            _factionVisiblePositions = null;
            _factionVisibleTroops = null;
            _width = 0;
            _height = 0;
            _initialized = false;
        }

        // ============================================================================
        // 🔥 新增：视野缓存相关方法
        // ============================================================================

        /// <summary>
        /// 🔥 新增：检查某势力是否能看到某位置
        /// </summary>
        public static bool CanSee(Faction faction, Point position)
        {
            if (!_initialized || !_visionCacheEnabled) return false;
            if (faction == null || !IsValid(position)) return false;

            if (_factionVisiblePositions != null && _factionVisiblePositions.TryGetValue(faction.ID, out var positions))
            {
                return positions.Contains(position);
            }

            return false;
        }

        /// <summary>
        /// 🔥 新增：获取某势力可见的所有部队
        /// </summary>
        public static List<Troop> GetVisibleTroops(Faction faction)
        {
            if (!_initialized || !_visionCacheEnabled) return new List<Troop>();
            if (faction == null) return new List<Troop>();

            if (_factionVisibleTroops != null && _factionVisibleTroops.TryGetValue(faction.ID, out var troops))
            {
                return new List<Troop>(troops);
            }

            return new List<Troop>();
        }

        /// <summary>
        /// 🔥 新增：获取某势力可见的敌军
        /// </summary>
        public static List<Troop> GetVisibleEnemies(Faction faction)
        {
            var visibleTroops = GetVisibleTroops(faction);
            return visibleTroops.Where(t => t != null && !t.Destroyed && !faction.IsFriendly(t.BelongedFaction)).ToList();
        }

        /// <summary>
        /// 🔥 新增：获取某势力在指定位置可见的部队
        /// </summary>
        public static Troop GetVisibleTroopAt(Faction faction, Point position)
        {
            if (!CanSee(faction, position)) return null;
            return GetTroopAt(position);
        }

        /// <summary>
        /// 🔥 新增：更新某势力的视野缓存
        /// </summary>
        private static void UpdateFactionVision(Faction faction)
        {
            if (!_initialized || faction == null) return;
            if (_factionVisiblePositions == null || _factionVisibleTroops == null) return;

            var visiblePositions = new HashSet<Point>();
            var visibleTroops = new HashSet<Troop>();

            // 重新计算该势力的视野
            foreach (Troop troop in faction.Troops)
            {
                if (troop == null || troop.Destroyed) continue;
                if (troop.ViewArea == null) continue;

                foreach (Point p in troop.ViewArea.Area)
                {
                    if (IsValid(p))
                    {
                        visiblePositions.Add(p);
                        Troop troopAtPos = _troopMap[p.X, p.Y];
                        if (troopAtPos != null && !troopAtPos.Destroyed)
                        {
                            visibleTroops.Add(troopAtPos);
                        }
                    }
                }
            }

            foreach (Architecture arch in faction.Architectures)
            {
                if (arch == null) continue;
                if (arch.ViewArea == null) continue;

                foreach (Point p in arch.ViewArea.Area)
                {
                    if (IsValid(p))
                    {
                        visiblePositions.Add(p);
                        Troop troopAtPos = _troopMap[p.X, p.Y];
                        if (troopAtPos != null && !troopAtPos.Destroyed)
                        {
                            visibleTroops.Add(troopAtPos);
                        }
                    }
                }
            }

            _factionVisiblePositions[faction.ID] = visiblePositions;
            _factionVisibleTroops[faction.ID] = visibleTroops;
        }

        /// <summary>
        /// 🔥 新增：启用/禁用视野缓存
        /// </summary>
        public static void SetVisionCacheEnabled(bool enabled)
        {
            _visionCacheEnabled = enabled;
            if (enabled && _initialized)
            {
                ReloadVisionCache();
            }
        }
    }
}
