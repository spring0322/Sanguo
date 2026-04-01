using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GameObjects.TroopDetail
{
    public class TierPathFinder
    {
        // 使用 .NET 8 优先队列，彻底消除 Key 冲突并大幅提升性能
        private PriorityQueue<GameSquare, int> openQueue = new();
        private Dictionary<Point, GameSquare> openDictionary = [];
        private Dictionary<Point, GameSquare> closeDictionary = [];
        private List<GameSquare> closeList = [];
        private List<Point> lastPath = [];
        
        // 🔥 标志：显示移动范围时忽略友军代价（只影响显示，不影响实际寻路）
        private bool ignoreFriendlyPenalty = false;
        private Troop currentTroop = null;
        
        // 🔥 2026-03-12 新增：调试用部队名称（仅 DEBUG 模式使用）
        internal string DebugTroopName { get; set; }
        
        public event GetCost OnGetCost;
        public event GetPenalizedCost OnGetPenalizedCost;

        // 🔧 修复：使用标准的八边形距离匹配直行5/斜行7的设定，解决 AI 绕路/无视地形的问题
        private int distance(Point a, Point b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return 5 * Math.Max(dx, dy) + 2 * Math.Min(dx, dy); 
        }

        private GameSquare AddToCloseList()
        {
            GameSquare square = this.RemoveFromOpenList();
            if (square != null)
            {
                closeList.Add(square);
                closeDictionary.TryAdd(square.Position, square);
            }
            return square;
        }

        private void AddToCloseList(GameSquare square)
        {
            closeList.Add(square);
            closeDictionary.TryAdd(square.Position, square);
        }

        private void AddToOpenList(GameSquare square, bool useAStar)
        {
            // A* 必须把 PenalizedCost 纳入优先级，否则高惩罚目标会退化成全图扩张。
            int priority = useAStar ? square.F : square.RealG;
            openQueue.Enqueue(square, priority);
            openDictionary[square.Position] = square;
        }

        private void CheckAdjacentSquares(GameSquare currentSquare, Point end, bool useAStar, MilitaryKind kind)
        {
            int leftSquareCost = this.MakeSquare(currentSquare, false, new Point(currentSquare.Position.X - 1, currentSquare.Position.Y), end, -1, useAStar, kind);
            int topSquareCost = this.MakeSquare(currentSquare, false, new Point(currentSquare.Position.X, currentSquare.Position.Y - 1), end, -1, useAStar, kind);
            int rightSquareCost = this.MakeSquare(currentSquare, false, new Point(currentSquare.Position.X + 1, currentSquare.Position.Y), end, -1, useAStar, kind);
            int bottomSquareCost = this.MakeSquare(currentSquare, false, new Point(currentSquare.Position.X, currentSquare.Position.Y + 1), end, -1, useAStar, kind);
            
            this.MakeSquare(currentSquare, true, new Point(currentSquare.Position.X - 1, currentSquare.Position.Y - 1), end, Math.Min(topSquareCost, leftSquareCost), useAStar, kind);
            this.MakeSquare(currentSquare, true, new Point(currentSquare.Position.X - 1, currentSquare.Position.Y + 1), end, Math.Min(bottomSquareCost, leftSquareCost), useAStar, kind);
            this.MakeSquare(currentSquare, true, new Point(currentSquare.Position.X + 1, currentSquare.Position.Y - 1), end, Math.Min(topSquareCost, rightSquareCost), useAStar, kind);
            this.MakeSquare(currentSquare, true, new Point(currentSquare.Position.X + 1, currentSquare.Position.Y + 1), end, Math.Min(bottomSquareCost, rightSquareCost), useAStar, kind);
        }

        private int GetCostByPosition(Point position, bool oblique, int DirectionCost, MilitaryKind kind)
        {
            int cost = OnGetCost?.Invoke(position, oblique, DirectionCost, kind) ?? 0xdac;
            
            // ★★★ 修复：显示移动范围时，必须与实际移动使用相同的逻辑 ★★★
            // 日期：2026-03-07
            // 数据完整性断言：currentTroop 在 GetDayArea 中设置，如果为 null 说明数据流错误
            if (ignoreFriendlyPenalty)
            {
                Troop occupyingTroop = Session.Current.Scenario.GetTroopByPositionNoCheck(position);
                if (occupyingTroop != null && occupyingTroop != currentTroop)
                {
                    // 友军：减去 +30 代价惩罚，允许寻路算法探索友军后面的空地
                    // 但友军格子本身仍然会在 GetDayArea 中被排除
                    if (occupyingTroop.BelongedFaction == currentTroop.BelongedFaction)
                    {
                        cost = Math.Max(0, cost - 30);
                    }
                    // 敌军：标记为不可通行，阻止寻路算法"穿过"敌军
                    // 这样敌军后面的格子就不会被错误地标记为可达
                    // 注意：这里复制了 GetPossibleMoveByPosition 中的敌军检查逻辑
                    else if (!occupyingTroop.IsFriendly(currentTroop.BelongedFaction))
                    {
                        cost = 0xdac; // 不可通行
                    }
                }
            }
            
            // 🔥 2026-03-10 优化：使用统一的天气移动消耗计算
            // 🔥 HOT PATH：寻路系统每帧调用数百次，必须高性能
            // ANTI-BAND-AID：WeatherManager 必须在 Scenario.Init() 中初始化
            if (cost < 0xdac)
            {
                // ANTI-BAND-AID：运行时断言（仅 DEBUG 模式）
                System.Diagnostics.Debug.Assert(
                    Session.Current?.Scenario?.WeatherManager != null,
                    "数据损坏：WeatherManager 未初始化！请检查 Scenario.Init() 是否正确调用。");
                
                var terrain = GetTerrainTypeFromPosition(position);
                var unitType = GetUnitTypeFromMilitaryKind(kind);
                
                // 使用统一的移动消耗计算方法（内联优化）
                cost = WorldOfTheThreeKingdoms.GameLogic.MovementCostCalculator.GetFinalMovementCost(
                    position,
                    unitType,
                    terrain,
                    cost
                );
            }
            
            return cost;
        }

        // 🔥 2026-03-09 新增：从坐标获取 TerrainType
        // 🔥 HOT PATH：内联优化，避免额外分配
        private GameManager.TerrainType GetTerrainTypeFromPosition(Point position)
        {
            var terrainKind = Session.Current.Scenario.GetTerrainKindByPositionNoCheck(position);
            
            return terrainKind switch
            {
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.平原 => GameManager.TerrainType.Plain,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.草原 => GameManager.TerrainType.Plain,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.森林 => GameManager.TerrainType.Forest,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.山地 => GameManager.TerrainType.Mountain,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.峻岭 => GameManager.TerrainType.Mountain,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.水域 => GameManager.TerrainType.River,
                WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.湿地 => GameManager.TerrainType.River,
                _ => GameManager.TerrainType.Other
            };
        }

        // 🔥 2026-03-09 新增：从 MilitaryKind 获取 UnitType
        // 🔥 HOT PATH：内联优化，避免额外分配
        private GameManager.UnitType GetUnitTypeFromMilitaryKind(MilitaryKind kind)
        {
            return kind.Type switch
            {
                MilitaryType.步兵 => GameManager.UnitType.步兵,
                MilitaryType.弩兵 => GameManager.UnitType.弓兵,
                MilitaryType.骑兵 => GameManager.UnitType.骑兵,
                MilitaryType.器械 => GameManager.UnitType.攻城器械,
                MilitaryType.水军 => GameManager.UnitType.水军,
                _ => GameManager.UnitType.步兵 // 默认步兵
            };
        }
        
        private void saveLastPath() 
        {
            lastPath.Clear();
            if (closeList.Count == 0) return;

            List<Point> list = [];
            GameSquare parent = closeList[^1]; // C# 12 索引语法
            do
            {
                list.Add(parent.Position);
                parent = parent.Parent;
            }
            while (parent != null);
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                lastPath.Add(list[i]);
            }
        }

        public GameArea GetDayArea(Troop troop, int Days)
        {
            GameArea area = new();
            ClearLists();
            
            GameSquare square = new() { Position = troop.Position };
            this.AddToCloseList(square);
            
            int num = troop.Movability * Days;
            int movabilityLeft = troop.MovabilityLeft;
            troop.MovabilityLeft = num;
            MilitaryKind kind = troop.Army.Kind;
            
            // 🔥 设置标志：显示移动范围时忽略友军代价
            ignoreFriendlyPenalty = true;
            currentTroop = troop;
            
            while (true)
            {
                CheckAdjacentSquares(square, troop.Position, false, kind);
                if (openQueue.Count == 0) break;
                
                square = this.AddToCloseList();
                if (square == null) break;
                
                if (num >= square.RealG)
                {
                    // ★★★ 修复：排除所有部队位置（友军和敌军），只显示真实可到达的空地 ★★★
                    // 日期：2026-03-07
                    // 原因：之前只排除了占据格子本身，但没有阻止寻路算法"穿过"敌军
                    //       导致敌军后面的格子被标记为可达，但实际移动时无法穿过敌军
                    // 解决：在添加到可达区域前，验证该格子是否真的可以到达（不被任何部队占据）
                    Troop occupyingTroop = Session.Current.Scenario.GetTroopByPositionNoCheck(square.Position);
                    bool shouldExclude = occupyingTroop != null && occupyingTroop != troop;
                    
                    if (!shouldExclude)
                    {
                        area.AddPoint(square.Position);
                    }
                }
                // 🔥 移除 else break，让算法继续探索所有可达路径
                // 这样即使某个格子移动力不足，也能探索其他分支路径
                
                if (closeList.Count > 2500 || closeDictionary.Count > 2500) break;
            }
            
            // 🔥 恢复标志
            ignoreFriendlyPenalty = false;
            currentTroop = null;
            
            troop.MovabilityLeft = movabilityLeft;
            saveLastPath();
            ClearLists();
            return area;
        }

        public bool GetPath(Point start, Point end, MilitaryKind kind)
        {
            ClearLists();
            
            if (start == end)
            {
                lastPath = [];
                return true;
            }
            
            // 🔥 2026-03-12 修复：起点节点必须初始化H值
            // 问题：起点只设置了Position，H值默认为0，导致A*退化成Dijkstra
            // 结果：无方向的全图搜索，9500次迭代仍失败
            // 解决：初始化起点的H值为到目标的估计距离
            GameSquare square = new() 
            { 
                Position = start,
                H = distance(start, end)  // 🔥 关键修复：初始化H值
            };
            this.AddToCloseList(square);
            bool flag = false;
            
            // 🔥 2026-03-12 修复：使用合理的迭代上限
            // 问题：短距离寻路不应该需要 9500 次迭代
            // 原因：如果迭代次数过多，说明目标点实际上无法到达
            // 解决：使用更保守的迭代上限，快速失败
            int manhattanDist = Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y);
            
            int directDistanceCost = distance(start, end);
            int nearPathCostLimit = manhattanDist <= 4
                ? Math.Max(360, directDistanceCost * 24)
                : int.MaxValue;
            int pathCostLimit = Math.Max(0xdac, directDistanceCost * 64);

            // 近距离目标不应该跑成大范围盲搜，给更严格的迭代上限。
            int maxIterations = manhattanDist <= 4
                ? 256 + manhattanDist * 64
                : 500 + manhattanDist * manhattanDist * 50;
            
            while (!flag && square.RealG < pathCostLimit)
            {
                CheckAdjacentSquares(square, end, true, kind);
                
                if (openQueue.Count == 0) break;
                
                square = this.AddToCloseList();
                if (square == null) break;

                if (square.G >= pathCostLimit)
                {
                    #if DEBUG
                    string troopInfo = DebugTroopName ?? kind?.Name ?? "null";
                    System.Diagnostics.Debug.WriteLine($"[寻路早退] {troopInfo}: {start}→{end}, bestG={square.G} >= pathLimit={pathCostLimit}");
                    #endif
                    break;
                }

                if (square.F > nearPathCostLimit)
                {
                    #if DEBUG
                    string troopInfo = DebugTroopName ?? kind?.Name ?? "null";
                    System.Diagnostics.Debug.WriteLine($"[寻路早退] {troopInfo}: {start}→{end}, bestF={square.F} > nearLimit={nearPathCostLimit}");
                    #endif
                    break;
                }
                
                flag = square.Position == end;
                
                // 🔥 修复：使用更保守的迭代上限，快速失败
                if (closeList.Count > maxIterations || closeDictionary.Count > maxIterations) 
                {
                    #if DEBUG
                    string troopInfo = DebugTroopName ?? kind?.Name ?? "null";
                    System.Diagnostics.Debug.WriteLine($"[寻路超限] {troopInfo}: {start}→{end}, >{maxIterations}次");
                    #endif
                    break;
                }
            }
            
            saveLastPath();
            ClearLists();
            return flag;
        }

        private int GetPenalizedCostByPosition(Point position, MilitaryKind kind)
        {
            return OnGetPenalizedCost?.Invoke(position, kind) ?? 0;
        }

        private int MakeSquare(GameSquare currentSquare, bool oblique, Point position, Point end, int DirectionCost, bool useAStar, MilitaryKind kind)
        {
            // 🔧 边界检查：在调用 GetCostByPosition 之前就过滤越界坐标
            // 这是数据源头的验证，避免后续所有逻辑处理无效数据
            if (Session.Current.Scenario.PositionOutOfRange(position))
            {
                return 0xdac;  // 返回不可通行的代价，寻路算法会自动跳过
            }
            
            int num = this.GetCostByPosition(position, oblique, DirectionCost, kind);
            if (closeDictionary.ContainsKey(position) || num >= 0xdac) return num;

            int costIncrease = oblique ? (7 * num) : (5 * num);
            int newRealG = currentSquare.RealG + costIncrease;

            if (openDictionary.TryGetValue(position, out GameSquare existingSquare))
            {
                if (newRealG < existingSquare.RealG)
                {
                    // 🔧 修复：惰性更新，只推入更优节点
                    // 日期：2026-03-11
                    // 问题：旧代码只推入新节点，但不更新字典，导致 RemoveFromOpenList 无法识别最优节点
                    // 解决：创建更优节点，让 AddToOpenList 统一处理字典更新
                    
                    // 🔥 2026-03-12 修复：重新计算H值，不复用旧值
                    // 问题：existingSquare.H 可能是 0（从非A*搜索残留），导致A*退化成Dijkstra
                    // 解决：强制重新计算H值
                    int newH = useAStar ? distance(position, end) : 0;
                    
                    GameSquare betterSquare = new()
                    {
                        Parent = currentSquare,
                        Position = position,
                        PenalizedCost = existingSquare.PenalizedCost,
                        H = newH,
                        RealG = newRealG
                    };
                    
                    // 🔥 关键修复：让 AddToOpenList 统一处理字典更新，避免双重添加
                    AddToOpenList(betterSquare, useAStar);
                }
            }
            else
            {
                // 🔥 2026-03-12 修复：统一H值计算逻辑
                int newH = useAStar ? distance(position, end) : 0;
                
                GameSquare newSquare = new()
                {
                    Parent = currentSquare,
                    Position = position,
                    PenalizedCost = this.GetPenalizedCostByPosition(position, kind),
                    H = newH,
                    RealG = newRealG
                };
                
                AddToOpenList(newSquare, useAStar);
            }
            return num;
        }

        private GameSquare RemoveFromOpenList()
        {
            while (openQueue.TryDequeue(out GameSquare square, out _))
            {
                // 🔥 ANTI-BAND-AID：Fail Fast
                // 日期：2026-03-29
                // 原因：PriorityQueue 不应存储 null，如果出现说明数据损坏
                if (square == null)
                {
                    throw new InvalidOperationException(
                        $"[TierPathFinder] 数据损坏：openQueue 中检测到 null 值！" +
                        $"队列计数: {openQueue.Count}, 字典计数: {openDictionary.Count}");
                }
                
                // 🔥 2026-03-11 修复：跳过已在 closeList 中的节点
                // 问题：惰性删除导致同一位置在队列中有多个副本
                // 当第一个副本被加入 closeList 后，其他副本变成僵尸节点
                // 解决：在返回节点前，检查是否已在 closeList 中
                if (closeDictionary.ContainsKey(square.Position))
                {
                    continue;  // 跳过，继续取下一个节点
                }
                
                // 惰性删除拦截：如果该节点在字典里是最新的有效版本，则返回；否则说明它是被遗弃的旧节点，直接扔掉
                if (openDictionary.TryGetValue(square.Position, out GameSquare bestSquare) && bestSquare == square)
                {
                    openDictionary.Remove(square.Position);
                    return square;
                }
            }
            
            return null;
        }

        public void SetPath(List<Point> path)
        {
            path.Clear();
            for (int i = 1; i < lastPath.Count; i++)
            {
                path.Add(lastPath[i]);
            }
        }

        private void ClearLists()
        {
            openQueue.Clear();
            openDictionary.Clear();
            closeDictionary.Clear();
            closeList.Clear();
        }

        public delegate int GetCost(Point position, bool Oblique, int DirectionCost, MilitaryKind kind);
        public delegate int GetPenalizedCost(Point position, MilitaryKind kind);
    }
}
