using System;
using System.Collections.Generic;
using GameObjects; // 假设这是游戏基础命名空间
using Microsoft.Xna.Framework;

namespace GameGlobal
{
    /// <summary>
    /// ZOC (Zone of Control) 评估器
    /// 用于智能选择最佳卡位点，实现战术控制
    /// </summary>
    public static class ZOCEvaluator
    {
        /// <summary>
        /// 核心入口：获取最佳卡位点
        /// </summary>
        /// <param name="me">自己</param>
        /// <param name="candidates">我能走到的所有格子（MoveRange）</param>
        /// <param name="enemies">附近的敌军列表</param>
        /// <param name="targetToProtect">需要保护的目标（如主公、攻城车、濒死队友）</param>
        /// <returns>最佳卡位点</returns>
        public static Point GetBestBlockingPosition(Troop me, List<Point> candidates, List<Troop> enemies, Troop targetToProtect)
        {
            try
            {
                Point bestTile = me.Position; // 默认不动
                float maxScore = -9999f;

                foreach (Point tile in candidates)
                {
                    // 排除已经被占用的格子（除非是自己当前位置）
                    if (!IsTileWalkable(tile, me)) continue;

                    float score = EvaluateTile(tile, me, enemies, targetToProtect);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestTile = tile;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[ZOC评估] {me.Leader?.Name} 选择卡位点: ({bestTile.X}, {bestTile.Y}), 评分: {maxScore:F1}");
                return bestTile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC评估] GetBestBlockingPosition 失败: {ex.Message}");
                return me.Position;
            }
        }

        /// <summary>
        /// 评分函数：给某个格子打分
        /// </summary>
        private static float EvaluateTile(Point tile, Troop me, List<Troop> enemies, Troop targetToProtect)
        {
            try
            {
                float score = 0;

                // --- 1. 贴脸 ZOC 评分 (最重要) ---
                // 四边形格子逻辑：检查上下左右是否有敌军
                foreach (Troop enemy in enemies)
                {
                    if (enemy == null) continue;

                    int dist = GetManhattanDistance(tile, enemy.Position);
                    
                    // 距离为1 (上下左右)，说明骑脸了，ZOC 生效
                    if (dist == 1)
                    {
                        // 基础分：卡住一个人 +50 分
                        float lockScore = 50f;
                        
                        // 进阶：卡住的是骑兵还是弓兵？
                        // 如果卡住弓兵(ID 1, 15)，让他无法射箭(弓兵通常不能射邻接)，加分！
                        // 如果卡住骑兵(ID 2, 3)，废掉他的冲锋，加分！
                        if (IsRangedUnit(enemy))
                        {
                            lockScore += 30; // 卡住远程单位特别有价值
                        }
                        else if (IsCavalryUnit(enemy))
                        {
                            lockScore += 25; // 卡住骑兵也很有价值
                        }
                        
                        score += lockScore;
                    }
                    // 距离为2 (斜角或隔一格)，只有微弱威胁
                    else if (dist == 2)
                    {
                        score += 5f;
                    }
                }

                // --- 2. 护卫评分 (挡拆逻辑) ---
                if (targetToProtect != null)
                {
                    // 计算 "敌人 -> 保护目标" 的原始曼哈顿距离
                    // 只有最近的那个敌人构成最大威胁，我们只算最近的
                    Troop nearestThreat = GetNearestEnemy(targetToProtect, enemies);
                    if (nearestThreat != null)
                    {
                        // 几何判定：我是否在他们的 "矩形包围盒" 内？
                        // 如果我在，说明我挡路了
                        if (IsPointInRectangle(tile, nearestThreat.Position, targetToProtect.Position))
                        {
                            score += 40f; // 完美阻挡：我正好站在他们中间的必经之路上
                            
                            // 判定：距离之和等于总距离 (dist(E, Me) + dist(Me, T) == dist(E, T))
                            int distTotal = GetManhattanDistance(nearestThreat.Position, targetToProtect.Position);
                            int distViaMe = GetManhattanDistance(nearestThreat.Position, tile) + GetManhattanDistance(tile, targetToProtect.Position);
                            
                            if (distViaMe == distTotal)
                            {
                                score += 20f; // 完美卡位
                            }
                        }
                    }
                }

                // --- 3. 人墙评分 (Formation) ---
                // 检查上下左右有没有友军，有则加分，形成连环阵
                List<Troop> allies = GetNearbyAllies(me); // 获取周围友军
                foreach (Troop ally in allies)
                {
                    if (ally == me || ally == null) continue;
                    
                    if (GetManhattanDistance(tile, ally.Position) == 1)
                    {
                        score += 15f; // 抱团分，防止被单抓
                    }
                }

                // --- 4. 地形评分 (防守位) ---
                // 基于地形类型给予加分
                float terrainBonus = GetTerrainDefenseBonus(tile);
                score += terrainBonus;

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC评估] EvaluateTile 失败: {ex.Message}");
                return 0f;
            }
        }

        #region 辅助数学工具

        /// <summary>
        /// 曼哈顿距离：|x1-x2| + |y1-y2|
        /// 在四边形战棋中，这是最真实的移动步数
        /// </summary>
        private static int GetManhattanDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// 判断点 P 是否在 A 和 B 构成的矩形范围内
        /// 用于判断是否在 "挡路"
        /// </summary>
        private static bool IsPointInRectangle(Point p, Point a, Point b)
        {
            int minX = Math.Min(a.X, b.X);
            int maxX = Math.Max(a.X, b.X);
            int minY = Math.Min(a.Y, b.Y);
            int maxY = Math.Max(a.Y, b.Y);
            
            return (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY);
        }

        /// <summary>
        /// 简单的远程单位类型判断
        /// </summary>
        private static bool IsRangedUnit(Troop t)
        {
            try
            {
                if (t?.Army?.Kind == null) return false;
                
                int kind = t.Army.Kind.ID;
                // 1=弓, 14=射手, 15=弩, 32=投石 (数据来源 CommonData.json)
                return (kind == 1 || kind == 14 || kind == 15 || kind == 32 || kind == 301);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为骑兵单位
        /// </summary>
        private static bool IsCavalryUnit(Troop t)
        {
            try
            {
                if (t?.Kind == null) return false;
                
                int kind = t.Kind.ID;
                // 2=骑兵, 3=骑兵, 16=轻骑, 17=重骑, 91=甲骑具装, 400=虎豹骑, 401=西凉铁骑
                return (kind == 2 || kind == 3 || kind == 16 || kind == 17 || kind == 91 || kind == 400 || kind == 401);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取最近的敌人
        /// </summary>
        private static Troop GetNearestEnemy(Troop center, List<Troop> enemies)
        {
            try
            {
                Troop nearest = null;
                int minDist = 9999;
                
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    
                    int d = GetManhattanDistance(center.Position, e.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = e;
                    }
                }
                
                return nearest;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取附近的友军
        /// </summary>
        private static List<Troop> GetNearbyAllies(Troop me)
        {
            var allies = new List<Troop>();
            
            try
            {
                if (me?.BelongedFaction?.Troops == null) return allies;
                
                // 获取同一势力的所有部队
                foreach (Troop troop in me.BelongedFaction.Troops.GetList())
                {
                    if (troop == null || troop == me) continue;
                    
                    // 只考虑附近的友军（距离 <= 5）
                    if (GetManhattanDistance(me.Position, troop.Position) <= 5)
                    {
                        allies.Add(troop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC评估] GetNearbyAllies 失败: {ex.Message}");
            }
            
            return allies;
        }

        /// <summary>
        /// 获取地形防御加成
        /// </summary>
        private static float GetTerrainDefenseBonus(Point tile)
        {
            try
            {
                // 这里应该调用实际的地形系统
                // 暂时返回基础值，实际实现时需要根据游戏的地形系统调整
                
                // 示例逻辑：
                // int terrainID = Session.Current?.Scenario?.GetTerrainAt(tile) ?? 0;
                // switch (terrainID)
                // {
                //     case 2: return 10f; // 森林
                //     case 4: return 15f; // 山地
                //     case 6: return 20f; // 城寨
                //     default: return 0f; // 平原
                // }
                
                return 0f; // 暂时返回0，等待实际地形系统集成
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 占位符：判断格子是否可通行
        /// </summary>
        private static bool IsTileWalkable(Point p, Troop me)
        {
            try
            {
                // 这里调用游戏原本的逻辑，检查地形阻挡或是否有其他部队
                // return GameScenario.MainMap.IsWalkable(p) && GameScenario.GetTroopAt(p) == null;
                return true; // 暂时返回true，允许所有位置
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取ZOC影响范围内的敌军
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="allEnemies">所有敌军列表</param>
        /// <param name="range">影响范围（默认为2）</param>
        /// <returns>范围内的敌军列表</returns>
        public static List<Troop> GetEnemiesInZOC(Point position, List<Troop> allEnemies, int range = 2)
        {
            var enemiesInRange = new List<Troop>();
            
            try
            {
                foreach (Troop enemy in allEnemies)
                {
                    if (enemy == null) continue;
                    
                    if (GetManhattanDistance(position, enemy.Position) <= range)
                    {
                        enemiesInRange.Add(enemy);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC评估] GetEnemiesInZOC 失败: {ex.Message}");
            }
            
            return enemiesInRange;
        }

        /// <summary>
        /// 计算位置的ZOC控制强度
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="friendlyTroops">友军列表</param>
        /// <returns>控制强度评分</returns>
        public static float CalculateZOCStrength(Point position, List<Troop> friendlyTroops)
        {
            float strength = 0f;
            
            try
            {
                foreach (Troop troop in friendlyTroops)
                {
                    if (troop == null) continue;
                    
                    int distance = GetManhattanDistance(position, troop.Position);
                    if (distance <= 2)
                    {
                        // 距离越近，控制力越强
                        float contribution = (3 - distance) * 10f;
                        
                        // 根据部队角色调整贡献
                        TroopRole role = AIRoleSelector.DetermineRole(troop);
                        switch (role)
                        {
                            case TroopRole.Tank:
                                contribution *= 1.2f; // 肉盾控制力更强
                                break;
                            case TroopRole.DPS:
                                contribution *= 0.8f; // DPS控制力较弱
                                break;
                        }
                        
                        strength += contribution;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC评估] CalculateZOCStrength 失败: {ex.Message}");
            }
            
            return strength;
        }

        #endregion
    }
}