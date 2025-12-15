using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GameGlobal;

namespace GameObjects
{
    /// <summary>
    /// 部队ZOC战术扩展方法
    /// 为Troop类添加ZOC相关的智能行为
    /// </summary>
    public static class TroopZOCExtensions
    {
        /// <summary>
        /// 执行智能ZOC卡位（仅限Tank角色）
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <returns>是否成功执行卡位</returns>
        public static bool ExecuteIntelligentZOCBlocking(this Troop troop)
        {
            try
            {
                // 只有 Tank 角色才执行这种复杂的卡位运算
                TroopRole role = AIRoleSelector.DetermineRole(troop);
                if (role != TroopRole.Tank)
                {
                    return false; // 非肉盾部队不执行卡位
                }

                System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 开始执行智能卡位");

                // 1. 获取所有能走到的格子
                List<Point> moveRange = troop.GetMoveRange();
                if (moveRange.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 无可移动位置");
                    return false;
                }

                // 2. 获取视野内的敌人
                List<Troop> visibleEnemies = troop.GetVisibleEnemies();
                if (visibleEnemies.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 未发现敌军");
                    return false;
                }

                // 3. 确定要保护的大哥 (比如己方的主将、或者兵力最少的队友)
                Troop vip = troop.GetLowestTroopAlly();
                if (vip != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 保护目标: {vip.Leader?.Name}");
                }

                // 4. 计算最佳卡位点
                Point blockPoint = ZOCEvaluator.GetBestBlockingPosition(troop, moveRange, visibleEnemies, vip);

                // 5. 检查是否需要移动
                if (blockPoint.X == troop.Position.X && blockPoint.Y == troop.Position.Y)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 当前位置已是最佳卡位点");
                    return true; // 当前位置就是最佳位置
                }

                // 6. 执行移动
                bool moveSuccess = troop.MoveTo(blockPoint);
                if (moveSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 成功移动到卡位点 ({blockPoint.X}, {blockPoint.Y})");
                    
                    // 记录卡位效果
                    var controlledEnemies = ZOCEvaluator.GetEnemiesInZOC(blockPoint, visibleEnemies, 1);
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] 控制了 {controlledEnemies.Count} 个敌军单位");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ZOC卡位] {troop.Leader?.Name} 移动到卡位点失败");
                }

                return moveSuccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZOC卡位] ExecuteIntelligentZOCBlocking 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取部队的移动范围
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>可移动的位置列表</returns>
        public static List<Point> GetMoveRange(this Troop troop)
        {
            var moveRange = new List<Point>();
            
            try
            {
                if (troop == null) return moveRange;

                // 获取部队的移动力
                int movePoints = GetTroopMovePoints(troop);
                Point currentPos = troop.Position;

                // 生成曼哈顿距离内的所有可达点
                for (int x = currentPos.X - movePoints; x <= currentPos.X + movePoints; x++)
                {
                    for (int y = currentPos.Y - movePoints; y <= currentPos.Y + movePoints; y++)
                    {
                        int distance = Math.Abs(x - currentPos.X) + Math.Abs(y - currentPos.Y);
                        if (distance <= movePoints)
                        {
                            Point candidate = new Point(x, y);
                            
                            // 检查位置是否可通行
                            if (IsPositionWalkable(candidate, troop))
                            {
                                moveRange.Add(candidate);
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[移动范围] {troop.Leader?.Name} 可移动到 {moveRange.Count} 个位置");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[移动范围] GetMoveRange 失败: {ex.Message}");
            }

            return moveRange;
        }

        /// <summary>
        /// 获取视野内的敌军
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>可见的敌军列表</returns>
        public static List<Troop> GetVisibleEnemies(this Troop troop)
        {
            var visibleEnemies = new List<Troop>();
            
            try
            {
                if (troop?.BelongedFaction == null) return visibleEnemies;

                // 获取视野范围
                int visionRange = GetTroopVisionRange(troop);
                Point currentPos = troop.Position;

                // 遍历所有势力的部队
                if (GameManager.Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in GameManager.Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction == null || faction == troop.BelongedFaction) continue;

                        // 检查是否为敌对势力
                        if (IsEnemyFaction(troop.BelongedFaction, faction))
                        {
                            if (faction.Troops != null)
                            {
                                foreach (Troop enemyTroop in faction.Troops.GetList())
                                {
                                    if (enemyTroop == null) continue;

                                    // 计算距离
                                    int distance = Math.Abs(currentPos.X - enemyTroop.Position.X) + 
                                                  Math.Abs(currentPos.Y - enemyTroop.Position.Y);

                                    if (distance <= visionRange)
                                    {
                                        visibleEnemies.Add(enemyTroop);
                                    }
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[视野侦察] {troop.Leader?.Name} 发现 {visibleEnemies.Count} 个敌军");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[视野侦察] GetVisibleEnemies 失败: {ex.Message}");
            }

            return visibleEnemies;
        }

        /// <summary>
        /// 获取兵力最少的友军（需要保护的目标）
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <returns>需要保护的友军</returns>
        public static Troop GetLowestTroopAlly(this Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction?.Troops == null) return null;

                Troop weakestAlly = null;
                int lowestQuantity = int.MaxValue;
                Point currentPos = troop.Position;

                foreach (Troop ally in troop.BelongedFaction.Troops.GetList())
                {
                    if (ally == null || ally == troop) continue;

                    // 只考虑附近的友军（距离 <= 8）
                    int distance = Math.Abs(currentPos.X - ally.Position.X) + 
                                  Math.Abs(currentPos.Y - ally.Position.Y);
                    
                    if (distance <= 8 && ally.Quantity < lowestQuantity)
                    {
                        lowestQuantity = ally.Quantity;
                        weakestAlly = ally;
                    }
                }

                // 优先保护主将
                if (troop.BelongedFaction.Leader != null)
                {
                    foreach (Troop ally in troop.BelongedFaction.Troops.GetList())
                    {
                        if (ally?.Leader == troop.BelongedFaction.Leader)
                        {
                            int distance = Math.Abs(currentPos.X - ally.Position.X) + 
                                          Math.Abs(currentPos.Y - ally.Position.Y);
                            if (distance <= 8)
                            {
                                System.Diagnostics.Debug.WriteLine($"[保护目标] {troop.Leader?.Name} 优先保护主将 {ally.Leader?.Name}");
                                return ally;
                            }
                        }
                    }
                }

                if (weakestAlly != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[保护目标] {troop.Leader?.Name} 保护最弱友军 {weakestAlly.Leader?.Name} (兵力: {weakestAlly.Quantity})");
                }

                return weakestAlly;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[保护目标] GetLowestTroopAlly 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否移动成功</returns>
        public static bool MoveTo(this Troop troop, Point targetPosition)
        {
            try
            {
                if (troop == null) return false;

                // 检查目标位置是否可达
                if (!IsPositionWalkable(targetPosition, troop))
                {
                    System.Diagnostics.Debug.WriteLine($"[移动] {troop.Leader?.Name} 目标位置不可通行: ({targetPosition.X}, {targetPosition.Y})");
                    return false;
                }

                // 检查移动距离是否在范围内
                int distance = Math.Abs(troop.Position.X - targetPosition.X) + 
                              Math.Abs(troop.Position.Y - targetPosition.Y);
                int movePoints = GetTroopMovePoints(troop);

                if (distance > movePoints)
                {
                    System.Diagnostics.Debug.WriteLine($"[移动] {troop.Leader?.Name} 目标位置超出移动范围: 距离{distance} > 移动力{movePoints}");
                    return false;
                }

                // 执行移动
                Point oldPosition = troop.Position;
                troop.Position = targetPosition;

                System.Diagnostics.Debug.WriteLine($"[移动] {troop.Leader?.Name} 从 ({oldPosition.X}, {oldPosition.Y}) 移动到 ({targetPosition.X}, {targetPosition.Y})");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[移动] MoveTo 失败: {ex.Message}");
                return false;
            }
        }

        #region 辅助方法

        /// <summary>
        /// 获取部队的移动力
        /// </summary>
        private static int GetTroopMovePoints(Troop troop)
        {
            try
            {
                if (troop?.Army?.Kind == null) return 3; // 默认移动力

                // 基于兵种类型的移动力
                int kindID = troop.Army.Kind.ID;
                switch (kindID)
                {
                    case 2:
                    case 3:
                    case 16:
                    case 17:
                    case 91:
                    case 400:
                    case 401:
                        return 5; // 骑兵类高移动力
                    case 1:
                    case 15:
                    case 32:
                    case 301:
                        return 3; // 远程类中等移动力
                    case 11:
                    case 51:
                    case 52:
                    case 101:
                    case 102:
                        return 2; // 重步兵低移动力
                    default:
                        return 3; // 默认移动力
                }
            }
            catch
            {
                return 3;
            }
        }

        /// <summary>
        /// 获取部队的视野范围
        /// </summary>
        private static int GetTroopVisionRange(Troop troop)
        {
            try
            {
                if (troop?.Army?.Kind == null) return 5; // 默认视野

                // 基于兵种类型的视野范围
                int kindID = troop.Army.Kind.ID;
                switch (kindID)
                {
                    case 2:
                    case 3:
                    case 16:
                    case 17:
                        return 7; // 骑兵视野远
                    case 1:
                    case 15:
                    case 32:
                    case 301:
                        return 6; // 远程单位视野较远
                    default:
                        return 5; // 默认视野
                }
            }
            catch
            {
                return 5;
            }
        }

        /// <summary>
        /// 检查位置是否可通行
        /// </summary>
        private static bool IsPositionWalkable(Point position, Troop troop)
        {
            try
            {
                // 这里应该调用实际的地图系统检查
                // 暂时简化实现，认为所有位置都可通行
                
                // 检查是否有其他部队占据
                if (GameManager.Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in GameManager.Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction?.Troops == null) continue;

                        foreach (Troop otherTroop in faction.Troops.GetList())
                        {
                            if (otherTroop == null || otherTroop == troop) continue;

                            if (otherTroop.Position.X == position.X && otherTroop.Position.Y == position.Y)
                            {
                                return false; // 位置被占据
                            }
                        }
                    }
                }

                return true; // 位置可通行
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否为敌对势力
        /// </summary>
        private static bool IsEnemyFaction(Faction faction1, Faction faction2)
        {
            try
            {
                if (faction1 == null || faction2 == null) return false;

                // 简化的敌对判断逻辑
                // 实际实现应该基于外交关系系统
                return faction1 != faction2;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 高级ZOC战术

        /// <summary>
        /// 执行协同ZOC战术
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <returns>是否成功执行协同战术</returns>
        public static bool ExecuteCoordinatedZOC(this Troop troop)
        {
            try
            {
                TroopRole role = AIRoleSelector.DetermineRole(troop);
                
                // 不同角色执行不同的协同战术
                switch (role)
                {
                    case TroopRole.Tank:
                        return troop.ExecuteIntelligentZOCBlocking();
                        
                    case TroopRole.Support:
                        return troop.ExecuteSupportZOC();
                        
                    case TroopRole.DPS:
                        return troop.ExecuteFlankingZOC();
                        
                    case TroopRole.Mage:
                        return troop.ExecuteControlZOC();
                        
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[协同ZOC] ExecuteCoordinatedZOC 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行辅助ZOC战术
        /// </summary>
        private static bool ExecuteSupportZOC(this Troop troop)
        {
            // 辅助部队专注于保护关键目标
            var vip = troop.GetLowestTroopAlly();
            if (vip == null) return false;

            var enemies = troop.GetVisibleEnemies();
            var moveRange = troop.GetMoveRange();
            
            var bestPosition = ZOCEvaluator.GetBestBlockingPosition(troop, moveRange, enemies, vip);
            return troop.MoveTo(bestPosition);
        }

        /// <summary>
        /// 执行侧翼ZOC战术
        /// </summary>
        private static bool ExecuteFlankingZOC(this Troop troop)
        {
            // DPS部队执行侧翼控制
            var enemies = troop.GetVisibleEnemies();
            if (enemies.Count == 0) return false;

            var moveRange = troop.GetMoveRange();
            var bestPosition = ZOCEvaluator.GetBestBlockingPosition(troop, moveRange, enemies, null);
            
            return troop.MoveTo(bestPosition);
        }

        /// <summary>
        /// 执行控制ZOC战术
        /// </summary>
        private static bool ExecuteControlZOC(this Troop troop)
        {
            // 法师部队执行区域控制
            var enemies = troop.GetVisibleEnemies();
            if (enemies.Count == 0) return false;

            var moveRange = troop.GetMoveRange();
            
            // 法师优先控制多个敌军
            Point bestPosition = troop.Position;
            int maxControlled = 0;

            foreach (var position in moveRange)
            {
                var controlledEnemies = ZOCEvaluator.GetEnemiesInZOC(position, enemies, 2);
                if (controlledEnemies.Count > maxControlled)
                {
                    maxControlled = controlledEnemies.Count;
                    bestPosition = position;
                }
            }

            return troop.MoveTo(bestPosition);
        }

        #endregion
    }
}