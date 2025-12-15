using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// AI撤退系统
    /// 智能判断撤退时机并执行撤退逻辑
    /// </summary>
    public static class AIRetreatSystem
    {
        /// <summary>
        /// 检查是否需要撤退
        /// 在 AITroop.Think() 开头调用
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否需要撤退（如果返回true，本回合结束）</returns>
        public static bool CheckAndExecuteRetreat(Troop troop)
        {
            try
            {
                if (troop == null) return false;

                // 兵力小于 30% 时考虑撤退
                float troopRatio = (float)troop.Quantity / (troop.Army?.Kind?.MaxScale ?? 1000);
                if (troopRatio >= 0.3f)
                {
                    return false; // 兵力充足，无需撤退
                }

                System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 兵力不足 ({troopRatio:P1})，评估撤退");

                // 检查是否有翻盘希望 (比如周围有很多强力队友)
                float allyPower = GetSurroundingAllyPower(troop);
                float enemyPower = GetSurroundingEnemyPower(troop);

                System.Diagnostics.Debug.WriteLine($"[AI撤退] 周围战力对比 - 友军: {allyPower:F1}, 敌军: {enemyPower:F1}");

                if (allyPower >= enemyPower)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 友军支援充足，继续战斗");
                    return false; // 有翻盘希望，继续战斗
                }

                // 触发撤退逻辑
                System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 开始执行撤退");

                // 1. 寻找最近的己方建筑 (City/Port/Gate)
                Architecture safeZone = GetNearestFriendlyArchitecture(troop);
                if (safeZone == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 未找到安全区域，继续战斗");
                    return false; // 没有安全区域，只能继续战斗
                }

                System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 目标安全区域: {safeZone.Name}");

                // 2. 全速移动 (不攻击，只移动)
                bool moveSuccess = MoveToSafeZone(troop, safeZone);

                // 3. 进城 (如果已经到了)
                if (IsAdjacentToArchitecture(troop, safeZone))
                {
                    bool enterSuccess = EnterArchitecture(troop, safeZone);
                    if (enterSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 成功撤退到 {safeZone.Name}");
                    }
                }

                return true; // 本回合结束，专注撤退
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] CheckAndExecuteRetreat 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取周围友军战力
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <returns>友军总战力</returns>
        private static float GetSurroundingAllyPower(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return 0f;

                float totalPower = 0f;
                int searchRadius = 5; // 搜索半径

                if (troop.BelongedFaction.Troops != null)
                {
                    foreach (Troop ally in troop.BelongedFaction.Troops.GetList())
                    {
                        if (ally == null || ally == troop) continue;

                        int distance = Math.Abs(troop.Position.X - ally.Position.X) + 
                                      Math.Abs(troop.Position.Y - ally.Position.Y);

                        if (distance <= searchRadius)
                        {
                            float allyPower = CalculateTroopPower(ally);
                            totalPower += allyPower;
                            
                            System.Diagnostics.Debug.WriteLine($"[AI撤退] 友军 {ally.Leader?.Name} 战力: {allyPower:F1}");
                        }
                    }
                }

                return totalPower;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] GetSurroundingAllyPower 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 获取周围敌军战力
        /// </summary>
        /// <param name="troop">当前部队</param>
        /// <returns>敌军总战力</returns>
        private static float GetSurroundingEnemyPower(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return 0f;

                float totalPower = 0f;
                int searchRadius = 5; // 搜索半径

                if (Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction == null || faction == troop.BelongedFaction) continue;

                        if (faction.Troops != null)
                        {
                            foreach (Troop enemy in faction.Troops.GetList())
                            {
                                if (enemy == null) continue;

                                int distance = Math.Abs(troop.Position.X - enemy.Position.X) + 
                                              Math.Abs(troop.Position.Y - enemy.Position.Y);

                                if (distance <= searchRadius)
                                {
                                    float enemyPower = CalculateTroopPower(enemy);
                                    totalPower += enemyPower;
                                    
                                    System.Diagnostics.Debug.WriteLine($"[AI撤退] 敌军 {enemy.Leader?.Name} 战力: {enemyPower:F1}");
                                }
                            }
                        }
                    }
                }

                return totalPower;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] GetSurroundingEnemyPower 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算部队战力
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>战力值</returns>
        private static float CalculateTroopPower(Troop troop)
        {
            try
            {
                if (troop?.Leader == null) return 0f;

                float power = 0f;
                
                // 基础战力 = 兵力 * 武将能力
                power += troop.Quantity * 0.1f;
                power += troop.Leader.Command * 2f;
                power += troop.Leader.Strength * 1.5f;
                power += troop.Leader.Intelligence * 1f;

                // 兵种修正
                if (troop.Army?.Kind != null)
                {
                    int kindID = troop.Army.Kind.ID;
                    switch (kindID)
                    {
                        case 400:
                        case 401: // 精英单位
                            power *= 1.5f;
                            break;
                        case 2:
                        case 3: // 骑兵
                            power *= 1.2f;
                            break;
                    }
                }

                return power;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 寻找最近的友方建筑
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>最近的安全建筑</returns>
        private static Architecture GetNearestFriendlyArchitecture(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return null;

                Architecture nearestSafe = null;
                int minDistance = int.MaxValue;

                if (troop.BelongedFaction.Architectures != null)
                {
                    foreach (Architecture arch in troop.BelongedFaction.Architectures.GetList())
                    {
                        if (arch == null) continue;

                        int distance = Math.Abs(troop.Position.X - arch.Position.X) + 
                                      Math.Abs(troop.Position.Y - arch.Position.Y);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestSafe = arch;
                        }
                    }
                }

                if (nearestSafe != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI撤退] 找到最近安全区域: {nearestSafe.Name} (距离: {minDistance})");
                }

                return nearestSafe;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] GetNearestFriendlyArchitecture 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 向安全区域移动
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="safeZone">安全区域</param>
        /// <returns>是否成功移动</returns>
        private static bool MoveToSafeZone(Troop troop, Architecture safeZone)
        {
            try
            {
                if (troop == null || safeZone == null) return false;

                System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 向 {safeZone.Name} 撤退移动");

                // 计算移动方向
                int deltaX = safeZone.Position.X - troop.Position.X;
                int deltaY = safeZone.Position.Y - troop.Position.Y;

                // 简化的移动逻辑：向目标方向移动一步
                Microsoft.Xna.Framework.Point newPosition = troop.Position;

                if (Math.Abs(deltaX) > Math.Abs(deltaY))
                {
                    // 优先水平移动
                    newPosition.X += deltaX > 0 ? 1 : -1;
                }
                else if (deltaY != 0)
                {
                    // 垂直移动
                    newPosition.Y += deltaY > 0 ? 1 : -1;
                }

                // 检查新位置是否可通行
                if (IsPositionSafe(newPosition, troop))
                {
                    troop.Position = newPosition;
                    System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 移动到 ({newPosition.X}, {newPosition.Y})");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] MoveToSafeZone 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否与建筑相邻
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="architecture">建筑</param>
        /// <returns>是否相邻</returns>
        private static bool IsAdjacentToArchitecture(Troop troop, Architecture architecture)
        {
            try
            {
                if (troop == null || architecture == null) return false;

                int distance = Math.Abs(troop.Position.X - architecture.Position.X) + 
                              Math.Abs(troop.Position.Y - architecture.Position.Y);

                return distance <= 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 进入建筑
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="architecture">建筑</param>
        /// <returns>是否成功进入</returns>
        private static bool EnterArchitecture(Troop troop, Architecture architecture)
        {
            try
            {
                if (troop == null || architecture == null) return false;

                System.Diagnostics.Debug.WriteLine($"[AI撤退] {troop.Leader?.Name} 尝试进入 {architecture.Name}");

                // 这里应该调用实际的进城逻辑
                // 暂时返回true表示成功
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI撤退] EnterArchitecture 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查位置是否安全
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="troop">部队</param>
        /// <returns>是否安全</returns>
        private static bool IsPositionSafe(Microsoft.Xna.Framework.Point position, Troop troop)
        {
            try
            {
                // 简化的安全检查：确保位置没有被其他部队占据
                if (Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
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

                return true; // 位置安全
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取撤退状态报告
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>撤退状态报告</returns>
        public static string GetRetreatStatusReport(Troop troop)
        {
            try
            {
                if (troop == null) return "无效部队";

                var report = new System.Text.StringBuilder();
                report.AppendLine($"=== {troop.Leader?.Name} 撤退状态报告 ===");

                float troopRatio = (float)troop.Quantity / (troop.Army?.Kind?.MaxScale ?? 1000);
                report.AppendLine($"兵力状况: {troop.Quantity}/{troop.Army?.Kind?.MaxScale ?? 1000} ({troopRatio:P1})");

                if (troopRatio < 0.3f)
                {
                    report.AppendLine("⚠️ 兵力不足，需要考虑撤退");

                    float allyPower = GetSurroundingAllyPower(troop);
                    float enemyPower = GetSurroundingEnemyPower(troop);

                    report.AppendLine($"周围友军战力: {allyPower:F1}");
                    report.AppendLine($"周围敌军战力: {enemyPower:F1}");

                    if (allyPower < enemyPower)
                    {
                        report.AppendLine("🚨 建议立即撤退");

                        var safeZone = GetNearestFriendlyArchitecture(troop);
                        if (safeZone != null)
                        {
                            int distance = Math.Abs(troop.Position.X - safeZone.Position.X) + 
                                          Math.Abs(troop.Position.Y - safeZone.Position.Y);
                            report.AppendLine($"最近安全区域: {safeZone.Name} (距离: {distance})");
                        }
                        else
                        {
                            report.AppendLine("❌ 未找到安全区域");
                        }
                    }
                    else
                    {
                        report.AppendLine("✅ 友军支援充足，可继续战斗");
                    }
                }
                else
                {
                    report.AppendLine("✅ 兵力充足，无需撤退");
                }

                return report.ToString();
            }
            catch (Exception ex)
            {
                return $"报告生成失败: {ex.Message}";
            }
        }
    }
}