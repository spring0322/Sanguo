using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameObjects.AI.Helper;
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;
#if false
{
    /// <summary>
    /// AI势力回合集成系统
    /// 将AI系统与现有的Faction回合逻辑完整整合
    /// 实现智能化的势力回合管理
    /// </summary>
    public static class AIFactionTurnIntegration
    {
        /// <summary>
        /// 执行完整的AI势力回合
        /// 集成角色分配、目标选择、战术定位和行动序列优化
        /// </summary>
        /// <param name="faction">执行回合的势力</param>
        public static void RunFactionTurn(Faction faction)
        {
            try
            {
                if (faction == null || faction.Destroyed)
                {
                    Console.WriteLine("[AIFactionTurnIntegration] 势力无效或已被摧毁");
                    return;
                }

                Console.WriteLine($"=== 开始执行势力 {faction.Name} 的AI回合 ===");

                // Phase 0: 后勤管理 - 自动创建运输兵进行资源调配
                ExecuteLogisticsManagement(faction);

                // Phase 1: 角色分配 - 为所有部队分配战术角色
                AssignTacticalRoles(faction);

                // Phase 2: 获取战场信息
                var battlefieldInfo = AnalyzeBattlefield(faction);

                // Phase 3: 行动序列优化 - 调整行动顺序，让辅助先动
                var actionQueue = AIActionSequencer.GetSortedTurnOrder(faction.Troops.Cast<Troop>().ToList());

                // Phase 4: 执行优化的AI决策
                ExecuteOptimizedTroopActions(actionQueue, battlefieldInfo);

                Console.WriteLine($"=== 势力 {faction.Name} 的AI回合执行完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIFactionTurnIntegration] 执行势力回合时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行后勤管理
        /// </summary>
        /// <param name="faction">势力</param>
        private static void ExecuteLogisticsManagement(Faction faction)
        {
            try
            {
                Console.WriteLine($"[Phase 0] 执行势力 {faction.Name} 的后勤管理");

                // 自动创建运输兵进行资源调配
                AILogisticsHandler.AutoCreateTransportTroops(faction);

                Console.WriteLine($"[Phase 0] 后勤管理完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phase 0] 后勤管理时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 为势力的所有部队分配战术角色
        /// </summary>
        /// <param name="faction">势力</param>
        private static void AssignTacticalRoles(Faction faction)
        {
            try
            {
                Console.WriteLine($"[Phase 1] 为势力 {faction.Name} 分配战术角色");

                int roleCount = 0;
                foreach (Troop troop in faction.Troops.GetList())
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        // 如果部队还没有分配角色，或者需要重新评估角色
                        if (troop.CurrentRole == TroopRole.None || ShouldReassignRole(troop))
                        {
                            var oldRole = troop.CurrentRole;
                            troop.CurrentRole = AIRoleSelector.DetermineRole(troop);
                            
                            if (oldRole != troop.CurrentRole)
                            {
                                Console.WriteLine($"  部队 {troop.ID}: {GetRoleDescription(oldRole)} -> {GetRoleDescription(troop.CurrentRole)}");
                                roleCount++;
                            }
                        }
                    }
                }

                Console.WriteLine($"[Phase 1] 角色分配完成，共调整 {roleCount} 个部队的角色");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phase 1] 角色分配时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否需要重新分配角色
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>是否需要重新分配</returns>
        private static bool ShouldReassignRole(Troop troop)
        {
            try
            {
                // 如果部队受伤严重，可能需要改变角色
                if (troop.InjuryQuantity > troop.Quantity * 0.5)
                {
                    return true;
                }

                // 如果部队装备发生重大变化，需要重新评估
                // 这里可以添加更多的重新分配条件
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 分析战场态势
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>战场信息</returns>
        private static BattlefieldInfo AnalyzeBattlefield(Faction faction)
        {
            var info = new BattlefieldInfo();

            try
            {
                Console.WriteLine($"[Phase 2] 分析势力 {faction.Name} 的战场态势");

                // 获取所有敌军
                info.AllEnemies = GetAllEnemies(faction);
                
                // 获取所有友军
                info.AllAllies = faction.Troops.Cast<Troop>().ToList();

                // 分析威胁等级
                info.ThreatLevel = CalculateThreatLevel(faction, info.AllEnemies);

                // 识别关键目标
                info.HighValueTargets = IdentifyHighValueTargets(info.AllEnemies);

                Console.WriteLine($"[Phase 2] 战场分析完成 - 敌军: {info.AllEnemies.Count}, 友军: {info.AllAllies.Count}, 威胁等级: {info.ThreatLevel}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phase 2] 战场分析时发生错误: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// 执行优化的部队行动
        /// </summary>
        /// <param name="actionQueue">优化后的行动队列</param>
        /// <param name="battlefieldInfo">战场信息</param>
        private static void ExecuteOptimizedTroopActions(List<Troop> actionQueue, BattlefieldInfo battlefieldInfo)
        {
            try
            {
                Console.WriteLine($"[Phase 4] 开始执行优化的部队行动，共 {actionQueue.Count} 个部队");

                int actionCount = 0;
                foreach (var troop in actionQueue)
                {
                    if (troop == null || troop.Destroyed) continue;

                    Console.WriteLine($"  执行部队 {troop.ID} ({GetRoleDescription(troop.CurrentRole)}) 的回合");

                    // 使用新的TroopAIExecutor执行单个部队的AI逻辑
                    TroopAIExecutor.ExecuteTurn(troop);
                    actionCount++;
                }

                Console.WriteLine($"[Phase 4] 部队行动执行完成，共执行 {actionCount} 个部队的行动");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phase 4] 执行部队行动时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行单个部队的完整回合
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="battlefieldInfo">战场信息</param>
        private static void ExecuteSingleTroopTurn(Troop troop, BattlefieldInfo battlefieldInfo)
        {
            try
            {
                // 🔧 特殊处理：运输兵AI
                if (AILogisticsHandler.IsTransportTroop(troop))
                {
                    Console.WriteLine($"    检测到运输兵 {troop.ID}，执行运输AI");
                    AILogisticsHandler.ExecuteTransportAI(troop);
                    return;
                }

                // 🔧 特殊处理：工程兵AI
                if (AIConstructionPlanner.IsConstructionTroop(troop))
                {
                    Console.WriteLine($"    检测到工程兵 {troop.ID}，执行建造AI");
                    AIConstructionPlanner.ExecuteConstructionAI(troop);
                    return;
                }

                // Phase 3: 智能目标选择
                Troop bestTarget = AITargetSelector.GetBestTarget(
                    troop, 
                    battlefieldInfo.AllEnemies, 
                    battlefieldInfo.AllAllies
                );

                if (bestTarget != null)
                {
                    Console.WriteLine($"    选择目标: 敌军 {bestTarget.ID}");

                    // Phase 2: 战术定位 - 为了攻击这个目标，我该站哪里？
                    var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
                    var bestPos = AITacticalPositioner.GetBestPosition(
                        troop, 
                        bestTarget, 
                        battlefieldInfo.AllAllies, 
                        moveArea
                    );

                    // 执行移动
                    if (bestPos != troop.Position && moveArea.Contains(bestPos))
                    {
                        Console.WriteLine($"    移动到位置: ({bestPos.X}, {bestPos.Y})");
                        ExecuteMovement(troop, bestPos);
                    }

                    // 如果到了攻击范围，执行攻击
                    if (CanAttack(troop, bestTarget))
                    {
                        Console.WriteLine($"    攻击目标: 敌军 {bestTarget.ID}");
                        ExecuteAttack(troop, bestTarget);
                    }
                }
                else
                {
                    // 没有目标时的行为
                    ExecuteIdleBehavior(troop, battlefieldInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    部队 {troop.ID} 执行回合时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行部队移动
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="targetPosition">目标位置</param>
        private static void ExecuteMovement(Troop troop, Point targetPosition)
        {
            try
            {
                // 使用游戏原有的移动系统
                if (troop.MovabilityLeft > 0)
                {
                    // 计算移动路径
                    var path = MapNavigationHelper.FindPath(troop, troop.Position, targetPosition);
                    
                    if (path.Count > 0)
                    {
                        // 设置部队目标位置
                        troop.Destination = targetPosition;
                        
                        // 这里可以调用游戏原有的移动逻辑
                        // troop.MoveTo(targetPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      移动执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="target">目标</param>
        private static void ExecuteAttack(Troop attacker, Troop target)
        {
            try
            {
                // 使用游戏原有的攻击系统
                if (AIHelper.CanAttackTarget(attacker, target))
                {
                    // 这里可以调用游戏原有的攻击逻辑
                    // attacker.AttackTroop(target);
                    
                    // 或者使用策略攻击
                    if (AIHelper.CanCastStratagem(attacker, target) && ShouldUseStratagem(attacker, target))
                    {
                        // attacker.CastStratagem(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      攻击执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行空闲行为（没有目标时）
        /// </summary>
        /// <param name="troop">部队</param>
        /// <param name="battlefieldInfo">战场信息</param>
        private static void ExecuteIdleBehavior(Troop troop, BattlefieldInfo battlefieldInfo)
        {
            try
            {
                Console.WriteLine($"    部队 {troop.ID} 没有找到目标，执行空闲行为");

                switch (troop.CurrentRole)
                {
                    case TroopRole.Tank:
                        // 坦克寻找前线位置
                        ExecuteTankIdleBehavior(troop, battlefieldInfo);
                        break;
                        
                    case TroopRole.Support:
                        // 辅助寻找需要治疗的友军
                        ExecuteSupportIdleBehavior(troop, battlefieldInfo);
                        break;
                        
                    case TroopRole.Logistics:
                        // 后勤执行资源管理
                        ExecuteLogisticsIdleBehavior(troop, battlefieldInfo);
                        break;
                        
                    default:
                        // 默认巡逻行为
                        ExecutePatrolBehavior(troop);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      空闲行为执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 坦克空闲行为
        /// </summary>
        private static void ExecuteTankIdleBehavior(Troop troop, BattlefieldInfo battlefieldInfo)
        {
            // 寻找最需要保护的友军位置
            var vulnerableAllies = battlefieldInfo.AllAllies
                .Where(a => a.CurrentRole == TroopRole.Mage || a.CurrentRole == TroopRole.Support)
                .OrderBy(a => a.Quantity)
                .ToList();

            if (vulnerableAllies.Count > 0)
            {
                var protectTarget = vulnerableAllies.First();
                var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
                var protectPosition = moveArea
                    .OrderBy(p => AIHelper.GetManhattanDistance(p, protectTarget.Position))
                    .FirstOrDefault();

                if (protectPosition != Point.Zero)
                {
                    ExecuteMovement(troop, protectPosition);
                }
            }
        }

        /// <summary>
        /// 辅助空闲行为
        /// </summary>
        private static void ExecuteSupportIdleBehavior(Troop troop, BattlefieldInfo battlefieldInfo)
        {
            // 寻找受伤的友军
            var injuredAllies = battlefieldInfo.AllAllies
                .Where(a => a.InjuryQuantity > 0)
                .OrderByDescending(a => a.InjuryQuantity)
                .ToList();

            if (injuredAllies.Count > 0)
            {
                var healTarget = injuredAllies.First();
                var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
                var healPosition = moveArea
                    .Where(p => AIHelper.GetManhattanDistance(p, healTarget.Position) <= 2)
                    .OrderBy(p => AIHelper.GetManhattanDistance(p, healTarget.Position))
                    .FirstOrDefault();

                if (healPosition != Point.Zero)
                {
                    ExecuteMovement(troop, healPosition);
                }
            }
        }

        /// <summary>
        /// 后勤空闲行为
        /// </summary>
        private static void ExecuteLogisticsIdleBehavior(Troop troop, BattlefieldInfo battlefieldInfo)
        {
            // 后勤部队寻找安全位置
            if (battlefieldInfo.AllEnemies.Count > 0)
            {
                var safePosition = MapNavigationHelper.GetBestRetreatPosition(
                    troop, 
                    battlefieldInfo.AllEnemies, 
                    battlefieldInfo.AllAllies
                );
                
                if (safePosition != troop.Position)
                {
                    ExecuteMovement(troop, safePosition);
                }
            }
        }

        /// <summary>
        /// 默认巡逻行为
        /// </summary>
        private static void ExecutePatrolBehavior(Troop troop)
        {
            // 简单的巡逻逻辑
            var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
            if (moveArea.Count > 1)
            {
                var randomPosition = moveArea[GameObject.Random(moveArea.Count)];
                ExecuteMovement(troop, randomPosition);
            }
        }

        // ================= 辅助方法 =================

        /// <summary>
        /// 获取所有敌军
        /// </summary>
        /// <param name="faction">己方势力</param>
        /// <returns>敌军列表</returns>
        private static List<Troop> GetAllEnemies(Faction faction)
        {
            var enemies = new List<Troop>();
            
            try
            {
                var scenario = AIHelper.GetScenario();
                if (scenario?.Troops != null)
                {
                    foreach (Troop troop in scenario.Troops.GetList())
                    {
                        if (troop != null && !troop.Destroyed && 
                            troop.BelongedFaction != null && troop.BelongedFaction != faction)
                        {
                            // 检查是否为敌对势力
                            if (!faction.IsFriendly(troop.BelongedFaction))
                            {
                                enemies.Add(troop);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AIHelper.Log($"获取敌军时发生错误: {ex.Message}");
            }

            return enemies;
        }

        /// <summary>
        /// 计算威胁等级
        /// </summary>
        /// <param name="faction">己方势力</param>
        /// <param name="enemies">敌军列表</param>
        /// <returns>威胁等级</returns>
        private static ThreatLevel CalculateThreatLevel(Faction faction, List<Troop> enemies)
        {
            try
            {
                if (enemies.Count == 0) return ThreatLevel.None;

                int totalEnemyPower = enemies.Cast<Troop>().Sum(e => e.Quantity * e.Offence);
                int totalAllyPower = faction.Troops.Cast<Troop>().Sum(t => t.Quantity * t.Offence);

                float powerRatio = totalAllyPower > 0 ? (float)totalEnemyPower / totalAllyPower : float.MaxValue;

                if (powerRatio < 0.5f) return ThreatLevel.Low;
                if (powerRatio < 1.0f) return ThreatLevel.Medium;
                if (powerRatio < 2.0f) return ThreatLevel.High;
                return ThreatLevel.Critical;
            }
            catch
            {
                return ThreatLevel.Medium;
            }
        }

        /// <summary>
        /// 识别高价值目标
        /// </summary>
        /// <param name="enemies">敌军列表</param>
        /// <returns>高价值目标列表</returns>
        private static List<Troop> IdentifyHighValueTargets(List<Troop> enemies)
        {
            return enemies
                .Where(e => e.Leader != null && 
                           (e.Leader.Command > 80 || e.Leader.Intelligence > 80))
                .ToList();
        }

        /// <summary>
        /// 检查是否可以攻击
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="target">目标</param>
        /// <returns>是否可以攻击</returns>
        private static bool CanAttack(Troop attacker, Troop target)
        {
            try
            {
                int distance = AIHelper.GetManhattanDistance(attacker.Position, target.Position);
                return distance <= attacker.OffenceRadius && AIHelper.CanAttackTarget(attacker, target);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否应该使用策略
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="target">目标</param>
        /// <returns>是否使用策略</returns>
        private static bool ShouldUseStratagem(Troop attacker, Troop target)
        {
            try
            {
                // 如果目标血量较高，优先使用策略
                if (target.Quantity > target.Army.Quantity * 0.7)
                {
                    return true;
                }

                // 如果攻击者智力较高，倾向于使用策略
                if (attacker.Leader != null && attacker.Leader.Intelligence > 70)
                {
                    return GameObject.Chance(60);
                }

                return GameObject.Chance(30);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>中文描述</returns>
        private static string GetRoleDescription(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.Tank: return "肉盾";
                case TroopRole.DPS: return "输出";
                case TroopRole.Mage: return "法师";
                case TroopRole.Support: return "辅助";
                case TroopRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 战场信息类
        /// </summary>
        public class BattlefieldInfo
        {
            public List<Troop> AllEnemies { get; set; } = new List<Troop>();
            public List<Troop> AllAllies { get; set; } = new List<Troop>();
            public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.None;
            public List<Troop> HighValueTargets { get; set; } = new List<Troop>();
        }

        /// <summary>
        /// 威胁等级枚举
        /// </summary>
        public enum ThreatLevel
        {
            None,       // 无威胁
            Low,        // 低威胁
            Medium,     // 中等威胁
            High,       // 高威胁
            Critical    // 极高威胁
        }
    }
}

#endif
