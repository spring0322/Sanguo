using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework; 
using System.Linq;
using System.Buffers; // 内存池优化
using System.Runtime.Intrinsics; // SIMD 核心
using System.Runtime.CompilerServices; // 内联优化
using System.Runtime.InteropServices; // Span 互操作
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects
{
    public partial class Troop
    {
        // 缓存地图尺寸，减少对 Session.Current 的深层访问
        private int _cachedMapWidth = -1;
        private int _cachedMapHeight = -1;
        
        // 静态只读方向数组 (预创建，避免重复分配)
        private static readonly Point[] _directionOffsets = { 
            new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0),
            new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1)
        };

        /// <summary>
        /// 更新地图尺寸缓存
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMapCache()
        {
            if (_cachedMapWidth != -1) return;

            var scenario = Session.Current.Scenario;
            if (scenario?.ScenarioMap != null)
            {
                _cachedMapWidth = scenario.ScenarioMap.MapDimensions.X;
                _cachedMapHeight = scenario.ScenarioMap.MapDimensions.Y;
            }
        }

        #region 核心寻路 (ArrayPool + Span 极致优化)

        [SkipLocalsInit]
        private void GetLocalNeighborsEnhanced(Point center, List<Point> result)
        {
            result.Clear();
            EnsureMapCache();
            int width = _cachedMapWidth;
            int height = _cachedMapHeight;
            int mapSize = width * height;

            if (mapSize <= 0) return;

            int[] visitedMap = ArrayPool<int>.Shared.Rent(mapSize);
            visitedMap.AsSpan(0, mapSize).Fill(-1);

            if (this.Movability <= 0) 
            {
                ArrayPool<int>.Shared.Return(visitedMap);
                return;
            }

            Queue<Point> queue = new Queue<Point>(64);
            
            int startIdx = center.Y * width + center.X;
            if ((uint)startIdx < (uint)mapSize)
            {
                visitedMap[startIdx] = this.Movability;
                queue.Enqueue(center);
            }

            try
            {
                ReadOnlySpan<Point> dirs = _directionOffsets.AsSpan();

                while (queue.TryDequeue(out Point current))
                {
                    int currentIdx = current.Y * width + current.X;
                    int remainingMov = visitedMap[currentIdx];

                    if (remainingMov <= 0) continue;

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        ref readonly Point dir = ref dirs[i];
                        int nx = current.X + dir.X;
                        int ny = current.Y + dir.Y;

                        if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;

                        Point next = new Point(nx, ny);
                        if (!CanPassTileEnhanced(next)) continue;

                        int cost = GetMovementCostEnhanced(next);
                        int nextRemaining = remainingMov - cost;

                        if (nextRemaining >= 0 && cost < 255)
                        {
                            if (IsEnemyZOC(next)) nextRemaining = 0;

                            int nextIdx = ny * width + nx;
                            ref int visitedVal = ref visitedMap[nextIdx];

                            if (visitedVal < nextRemaining)
                            {
                                visitedVal = nextRemaining;
                                if (!result.Contains(next)) result.Add(next);
                                queue.Enqueue(next);
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(visitedMap);
            }
        }

        #endregion

        #region 战斗评分 (SIMD 硬件加速)

        /// <summary>
        /// 评估最佳战斗行动 (集成 SIMD 距离计算)
        /// </summary>
        public CombatPlan EvaluateBestCombatAction()
        {
            var visibleEnemies = this.GetVisibleEnemyTroops(); 
            int enemyCount = visibleEnemies.Count;
            if (enemyCount == 0) return new CombatPlan();

            int[] enemyXs = ArrayPool<int>.Shared.Rent(enemyCount);
            int[] enemyYs = ArrayPool<int>.Shared.Rent(enemyCount);

            try
            {
                for (int i = 0; i < enemyCount; i++)
                {
                    Troop t = visibleEnemies[i];
                    enemyXs[i] = t.Position.X;
                    enemyYs[i] = t.Position.Y;
                }

                CombatPlan bestPlan = new CombatPlan { Score = float.MinValue };
                
                List<Point> candidates = new List<Point>(128);
                GetLocalNeighborsEnhanced(this.Position, candidates);
                candidates.Add(this.Position);

                Span<Point> candidateSpan = CollectionsMarshal.AsSpan(candidates);
                int[] distanceBuffer = ArrayPool<int>.Shared.Rent(enemyCount);

                try 
                {
                    foreach (ref Point tile in candidateSpan)
                    {
                        if (tile != this.Position && !CanStopAtEnhanced(tile)) continue;

                        BatchCalculateChebyshevDistances(tile, enemyXs, enemyYs, distanceBuffer, enemyCount);

                        for (int i = 0; i < enemyCount; i++)
                        {
                            var enemy = visibleEnemies[i];
                            int dist = distanceBuffer[i];

                            float score = CalculateBasicAttackScore(enemy);
                            if (dist > this.OffenceRadius) score -= (dist - this.OffenceRadius) * 10;
                            
                            if (score > bestPlan.Score)
                            {
                                bestPlan = new CombatPlan
                                {
                                    MoveDestination = tile,
                                    Target = enemy,
                                    ActionType = CombatActionType.BasicAttack, 
                                    Score = score
                                };
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(distanceBuffer);
                }
                
                return bestPlan;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(enemyXs);
                ArrayPool<int>.Shared.Return(enemyYs);
            }
        }


        /// <summary>
        /// 使用 .NET 8 跨平台 SIMD (Vector256/128) 批量计算切比雪夫距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BatchCalculateChebyshevDistances(Point tile, int[] xs, int[] ys, int[] results, int count)
        {
            int i = 0;
            
            // 1. 尝试使用 256 位宽 (AVX2 等)
            if (Vector256.IsHardwareAccelerated && count >= Vector256<int>.Count)
            {
                Vector256<int> vTileX = Vector256.Create(tile.X);
                Vector256<int> vTileY = Vector256.Create(tile.Y);

                // 获取 Span 引用以进行不安全快速访问
                Span<int> xsSpan = xs.AsSpan();
                Span<int> ysSpan = ys.AsSpan();
                Span<int> resSpan = results.AsSpan();

                for (; i <= count - Vector256<int>.Count; i += Vector256<int>.Count)
                {
                    // 加载数据
                    var vXs = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(xsSpan.Slice(i)));
                    var vYs = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(ysSpan.Slice(i)));

                    // 计算绝对差值
                    var diffX = Vector256.Abs(vXs - vTileX);
                    var diffY = Vector256.Abs(vYs - vTileY);

                    // Max(dx, dy)
                    var maxDist = Vector256.Max(diffX, diffY);

                    // 存储结果
                    maxDist.StoreUnsafe(ref MemoryMarshal.GetReference(resSpan.Slice(i)));
                }
            }
            // 2. 降级使用 128 位宽 (SSE2 / ARM NEON)
            else if (Vector128.IsHardwareAccelerated && count >= Vector128<int>.Count)
            {
                Vector128<int> vTileX = Vector128.Create(tile.X);
                Vector128<int> vTileY = Vector128.Create(tile.Y);
                
                Span<int> xsSpan = xs.AsSpan();
                Span<int> ysSpan = ys.AsSpan();
                Span<int> resSpan = results.AsSpan();

                for (; i <= count - Vector128<int>.Count; i += Vector128<int>.Count)
                {
                    var vXs = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(xsSpan.Slice(i)));
                    var vYs = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(ysSpan.Slice(i)));

                    var diffX = Vector128.Abs(vXs - vTileX);
                    var diffY = Vector128.Abs(vYs - vTileY);
                    var maxDist = Vector128.Max(diffX, diffY);

                    maxDist.StoreUnsafe(ref MemoryMarshal.GetReference(resSpan.Slice(i)));
                }
            }

            // 3. 标量回退处理 (Scalar Fallback)
            for (; i < count; i++)
            {
                int dx = Math.Abs(tile.X - xs[i]);
                int dy = Math.Abs(tile.Y - ys[i]);
                results[i] = Math.Max(dx, dy);
            }
        }

        #endregion

        #region 环境依赖抽象

        private bool CanPassTileEnhanced(Point p)
        {
            Troop t = Session.Current.Scenario.GetTroopByPosition(p);
            if (t != null && !this.IsFriendly(t.BelongedFaction))
            {
                return false; 
            }

            if (this.Army?.Kind != null)
            {
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(p);
                if (terrainKind != null)
                {
                    int adaptability = this.Army.GetTerrainAdaptability(terrainKind);
                    if (adaptability >= 255) return false;
                }
            }
            return true;
        }

        private bool CanStopAtEnhanced(Point p)
        {
            if (Session.Current.Scenario.PositionOutOfRange(p)) return false;

            Troop t = Session.Current.Scenario.GetTroopByPosition(p);
            if (t != null && t != this) return false;

            Architecture arch = Session.Current.Scenario.GetArchitectureByPosition(p);
            if (arch != null)
            {
                if (this.BelongedFaction != null && arch.BelongedFaction == this.BelongedFaction)
                {
                    return true;
                }
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMovementCostEnhanced(Point p)
        {
            if (this.Army?.Kind != null)
            {
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(p);
                if (terrainKind != null)
                {
                    int adaptability = this.Army.GetTerrainAdaptability(terrainKind);
                    
                    if (adaptability >= 255) return 255; 
                    if (adaptability >= 200) return 10;  
                    if (adaptability >= 150) return 3;   
                    if (adaptability >= 100) return 2;   
                    return 1; 
                }
            }
            return 1; 
        }

        private float GetTerrainDefenseBonus(Point p)
        {
            var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(p);
            if (terrainKind == null) return 0f;

            // 使用 C# 8 Switch Expression 简化代码
            return terrainKind switch
            {
                TerrainKind.山地 => 20f,
                TerrainKind.森林 => 15f,
                TerrainKind.峻岭 => 10f,
                TerrainKind.湿地 => 5f,
                _ => 0f
            };
        }

        #endregion
        
        #region 目标评估方法

        private float CalculateDistancePenalty(int distanceToEnemy, int attackRange)
        {
            float distancePenalty = 0;
            
            if (distanceToEnemy > attackRange)
            {
                distancePenalty = (distanceToEnemy - attackRange) * 50.0f; 
            }
            else
            {
                if (attackRange > 1) 
                {
                    float optimalDistance = attackRange * 0.8f; 
                    float distanceDiff = Math.Abs(distanceToEnemy - optimalDistance);
                    distancePenalty = distanceDiff * 5.0f; 
                }
                else 
                {
                    distancePenalty = distanceToEnemy * 10.0f; 
                }
            }
            
            return distancePenalty;
        }

        #endregion

        #region 辅助计算方法

        private int CalculateDistanceToArchitecture(Point tile, Architecture arch)
        {
            int distanceToArch = int.MaxValue;
            if (arch.ArchitectureArea != null)
            {
                // 使用 Span 迭代
                var areaSpan = CollectionsMarshal.AsSpan(arch.ArchitectureArea.Area);
                foreach (ref Point p in areaSpan)
                {
                    int dist = GetChebyshevDistance(tile, p);
                    if (dist < distanceToArch) distanceToArch = dist;
                }
            }
            return distanceToArch;
        }

        private bool IsTileInArchitectureArea(Point tile, Architecture arch)
        {
            if (arch.ArchitectureArea != null)
            {
                var areaSpan = CollectionsMarshal.AsSpan(arch.ArchitectureArea.Area);
                foreach (ref Point p in areaSpan)
                {
                    if (tile.X == p.X && tile.Y == p.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region 特殊情况处理

        private CombatPlan HandleZeroEnduranceArchitectureWithDefenders()
        {
            CombatPlan plan = new CombatPlan { MoveDestination = this.Position, Score = -99999, ActionType = CombatActionType.Wait };
            
            TroopList hostileTroops = new TroopList();
            
            if (this.WillArchitecture?.ArchitectureArea?.Area != null)
            {
                // 优化遍历
                var areaSpan = CollectionsMarshal.AsSpan(this.WillArchitecture.ArchitectureArea.Area);
                foreach (ref Point point in areaSpan)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if ((troopByPosition != null) && !troopByPosition.IsFriendly(this.BelongedFaction))
                    {
                        hostileTroops.Add(troopByPosition);
                    }
                }
            }
            
            if (hostileTroops.Count > 0)
            {
                Troop targetTroop = hostileTroops[0] as Troop;
                float distanceToTarget = (float)Session.Current.Scenario.GetDistance(this.Position, targetTroop.Position);
                
                if (distanceToTarget <= this.OffenceRadius)
                {
                    plan.Target = targetTroop;
                    plan.ActionType = CombatActionType.BasicAttack;
                    plan.MoveDestination = this.Position;
                    plan.Score = 9000;
                    return plan;
                }
            }
            return plan;
        }

        private Point FindBestOccupyPositionEnhanced(Architecture arch)
        {
            if (arch == null || arch.ArchitectureArea == null) return new Point(-1, -1);

            var movableTiles = new List<Point>();
            GetLocalNeighborsEnhanced(this.Position, movableTiles);
            movableTiles.Add(this.Position);

            Point bestPos = new Point(-1, -1);
            int minMoveDist = int.MaxValue;

            var archPoints = CollectionsMarshal.AsSpan(arch.ArchitectureArea.Area);
            foreach (ref Point archPoint in archPoints)
            {
                if (movableTiles.Contains(archPoint) && CanStopAtEnhanced(archPoint))
                {
                    int dist = GetChebyshevDistance(this.Position, archPoint);
                    if (dist < minMoveDist)
                    {
                        minMoveDist = dist;
                        bestPos = archPoint;
                    }
                }
            }

            return bestPos;
        }

        #endregion

        #region 战术评分算法 (The Brains)

        private float CalculateKitingScore(Point myPos, int distToTarget, int maxRange, List<Point> threats)
        {
            float score = 0;

            score += distToTarget * 20.0f;
            if (distToTarget == maxRange) score += 50.0f;

            int minThreatDist = GetMinDistance(myPos, threats);

            if (minThreatDist <= 1)
            {
                score -= 600.0f; 
            }
            else
            {
                score += minThreatDist * 20.0f; 
            }

            return score;
        }

        private float CalculateFormationScore(Point myPos, Point enemyPos, List<Troop> friends)
        {
            float score = 0;
            var friendsSpan = CollectionsMarshal.AsSpan(friends);

            foreach (var friend in friendsSpan)
            {
                int distToFriend = GetChebyshevDistance(myPos, friend.Position);

                if (distToFriend == 1)
                {
                    int friendToEnemy = GetChebyshevDistance(friend.Position, enemyPos);
                    int meToEnemy = GetChebyshevDistance(myPos, enemyPos);

                    if (friendToEnemy < meToEnemy)
                    {
                        score += 150.0f; 

                        if (friend.Army?.Kind != null)
                        {
                            if (WorldOfTheThreeKingdoms.GameGlobal.AIRoleConfigManager.IsTroopKindForRole(friend.Army.Kind.ID, "Tank"))
                            {
                                score += 50.0f;
                            }
                        }
                    }
                }
            }

            return score;
        }

        private float CalculateProtectionScore(Point myPos, List<Troop> friends, List<Point> threats)
        {
            float score = 0;
            var friendsSpan = CollectionsMarshal.AsSpan(friends);

            foreach (var friend in friendsSpan)
            {
                if (friend.OffenceRadius > 1)
                {
                    int distToFriend = GetChebyshevDistance(myPos, friend.Position);

                    if (distToFriend == 1)
                    {
                        int minThreatToMe = GetMinDistance(myPos, threats);
                        int minThreatToFriend = GetMinDistance(friend.Position, threats);

                        if (minThreatToMe < minThreatToFriend)
                        {
                            score += 200.0f; 
                        }
                    }
                }
            }

            return score;
        }

        #endregion

        #region 基础辅助方法 (The Legs)

        private int GetMinDistance(Point p, List<Point> targets)
        {
            int min = int.MaxValue;
            var span = CollectionsMarshal.AsSpan(targets);
            foreach (ref Point t in span)
            {
                int d = GetChebyshevDistance(p, t);
                if (d < min) min = d;
            }
            return min;
        }

        [SkipLocalsInit]
        private Point CalculateBestStrategicMoveEnhanced()
        {
            if (this.RealDestination == new Point(-1, -1) || 
                this.RealDestination == Point.Zero || 
                this.RealDestination == this.Position) 
            {
                return this.Position;
            }

            Point target = this.RealDestination;
            Point bestStep = this.Position;
            int minDst = GetChebyshevDistance(this.Position, target);

            // StackAlloc 优化：在栈上分配小数组，避免堆分配
            Span<Point> directions = stackalloc Point[] { 
                new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0),
                new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1)
            };

            foreach (ref Point dir in directions)
            {
                Point next = new Point(this.Position.X + dir.X, this.Position.Y + dir.Y);
                if (CanStopAtEnhanced(next))
                {
                    int d = GetChebyshevDistance(next, target);
                    if (d < minDst) 
                    { 
                        minDst = d; 
                        bestStep = next; 
                    }
                }
            }

            return bestStep;
        }

        public Point FindBestAttackPositionEnhanced(Troop target)
        {
            if (target == null) return new Point(-1, -1);
            
            Point bestPos = new Point(-1, -1);
            float maxScore = float.MinValue;
            
            List<Point> candidates = new List<Point>(128);
            GetLocalNeighborsEnhanced(this.Position, candidates);
            candidates.Add(this.Position);
            
            var candSpan = CollectionsMarshal.AsSpan(candidates);
            foreach (ref Point tile in candSpan)
            {
                if (tile != this.Position && !CanStopAtEnhanced(tile)) continue;
                
                if (GetChebyshevDistance(this.Position, target.Position) <= this.OffenceRadius + this.Movability)
                {
                    if (GetChebyshevDistance(tile, target.Position) <= this.OffenceRadius)
                    {
                        float score = 0;
                        var tKind = Session.Current.Scenario.GetTerrainKindByPositionNoCheck(tile);
                        if (tKind != TerrainKind.平原) 
                        {
                            score += (int)tKind; 
                        }
                        
                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestPos = tile;
                        }
                    }
                }
            }
            return bestPos;
        }

        /// <summary>
        /// 这里的IsEnemyZOC是一个简单的实现，用于检查某个点是否被敌军ZOC控制
        /// </summary>
        private bool IsEnemyZOC(Point p)
        {
             Troop t = Session.Current.Scenario.GetTroopByPosition(p);
             if (t != null && !this.IsFriendly(t.BelongedFaction)) return true;
             
             ReadOnlySpan<Point> dirs = _directionOffsets.AsSpan();
             
             for (int i = 0; i < dirs.Length; i++)
             {
                 ref readonly Point offset = ref dirs[i];
                 Point neighbor = new Point(p.X + offset.X, p.Y + offset.Y);
                 
                 // 修正：使用 PositionOutOfRange 确保绝对安全，避免数组越界风险
                 // 虽然比简单的坐标检查慢微秒级，但保证了逻辑一致性和稳定性
                 if (!Session.Current.Scenario.PositionOutOfRange(neighbor))
                 {
                     Troop nt = Session.Current.Scenario.GetTroopByPosition(neighbor);
                     if (nt != null && !this.IsFriendly(nt.BelongedFaction)) return true;
                 }
             }
             return false;
        }

        private void LogArchitectureAttackScore(Architecture arch, Point tile, float score)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"Attack Arch {arch.Name} at {tile} Score: {score}");
            #endif
        }

        #endregion

    }
}