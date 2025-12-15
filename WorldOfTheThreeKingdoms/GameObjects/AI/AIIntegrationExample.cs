using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;

namespace GameObjects.AI
{
    /// <summary>
    /// AI系统集成示例 - 展示如何在游戏中使用完整的AI战术系统
    /// </summary>
    public static class AIIntegrationExample
    {
        /// <summary>
        /// 完整的AI回合处理示例
        /// </summary>
        /// <param name="currentFaction">当前行动的势力</param>
        /// <param name="scenario">游戏场景</param>
        public static void ProcessAITurn(Faction currentFaction, GameScenario scenario)
        {
            if (currentFaction == null || scenario == null) return;

            Console.WriteLine($"=== 开始处理 {currentFaction.Name} 的AI回合 ===");

            try
            {
                // 获取当前势力的所有部队
                var friendlyTroops = GetFactionTroops(currentFaction);
                if (friendlyTroops.Count == 0)
                {
                    Console.WriteLine("该势力没有可用部队");
                    return;
                }

                // 获取敌对部队
                var enemyTroops = GetEnemyTroops(currentFaction, scenario);
                if (enemyTroops.Count == 0)
                {
                    Console.WriteLine("没有发现敌军，AI回合结束");
                    return;
                }

                // 分析战场态势
                string battlefieldAnalysis = AITacticalManager.AnalyzeBattlefield(friendlyTroops, enemyTroops);
                Console.WriteLine(battlefieldAnalysis);

                // 执行批量战术决策
                var decisions = AITacticalManager.ExecuteBatchTacticalDecisions(friendlyTroops, enemyTroops);

                // 应用决策结果
                ApplyTacticalDecisions(decisions, friendlyTroops);

                Console.WriteLine($"=== {currentFaction.Name} 的AI回合处理完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI回合处理出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 单个部队的AI行动示例
        /// </summary>
        /// <param name="troop">行动的部队</param>
        /// <param name="scenario">游戏场景</param>
        public static void ProcessSingleTroopAI(Troop troop, GameScenario scenario)
        {
            if (troop == null || scenario == null) return;

            try
            {
                // 获取友军和敌军
                var allies = GetFactionTroops(troop.BelongedFaction);
                var enemies = GetEnemyTroops(troop.BelongedFaction, scenario);

                // 执行战术决策
                Point newPosition = AITacticalManager.ExecuteTacticalDecision(troop, enemies, allies);

                // 如果需要移动
                if (newPosition != troop.Position)
                {
                    // 这里应该调用游戏的移动系统
                    Console.WriteLine($"部队 {troop.ID} 准备移动到 ({newPosition.X}, {newPosition.Y})");
                    
                    // 示例：设置部队的目标位置（实际实现需要根据游戏的移动系统）
                    // troop.SetDestination(newPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"单个部队AI处理出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取势力的所有部队
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>部队列表</returns>
        private static List<Troop> GetFactionTroops(Faction faction)
        {
            var troops = new List<Troop>();
            
            if (faction?.Troops != null)
            {
                foreach (Troop troop in faction.Troops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        troops.Add(troop);
                    }
                }
            }

            return troops;
        }

        /// <summary>
        /// 获取敌对部队
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        /// <param name="scenario">游戏场景</param>
        /// <returns>敌军列表</returns>
        private static List<Troop> GetEnemyTroops(Faction currentFaction, GameScenario scenario)
        {
            var enemies = new List<Troop>();

            if (scenario?.Factions != null)
            {
                foreach (Faction faction in scenario.Factions)
                {
                    if (faction != currentFaction && faction.Troops != null)
                    {
                        // 检查是否为敌对关系
                        if (AreFactionsHostile(currentFaction, faction))
                        {
                            foreach (Troop troop in faction.Troops)
                            {
                                if (troop != null && !troop.Destroyed)
                                {
                                    enemies.Add(troop);
                                }
                            }
                        }
                    }
                }
            }

            return enemies;
        }

        /// <summary>
        /// 检查两个势力是否敌对
        /// </summary>
        /// <param name="faction1">势力1</param>
        /// <param name="faction2">势力2</param>
        /// <returns>是否敌对</returns>
        private static bool AreFactionsHostile(Faction faction1, Faction faction2)
        {
            // 简化的敌对关系判断，实际游戏中应该检查外交关系
            return faction1 != faction2;
        }

        /// <summary>
        /// 应用战术决策结果
        /// </summary>
        /// <param name="decisions">决策字典</param>
        /// <param name="troops">部队列表</param>
        private static void ApplyTacticalDecisions(Dictionary<int, Point> decisions, List<Troop> troops)
        {
            foreach (var troop in troops)
            {
                if (decisions.TryGetValue(troop.ID, out Point targetPosition))
                {
                    if (targetPosition != troop.Position)
                    {
                        // 这里应该调用实际的移动命令
                        Console.WriteLine($"执行移动: 部队 {troop.ID} -> ({targetPosition.X}, {targetPosition.Y})");
                        
                        // 示例代码（需要根据实际游戏系统调整）:
                        // troop.MoveTo(targetPosition);
                        // 或者
                        // troop.SetWillPosition(targetPosition);
                    }
                }
            }
        }

        /// <summary>
        /// 战斗前的AI准备示例
        /// </summary>
        /// <param name="attackingFaction">攻击方势力</param>
        /// <param name="defendingFaction">防守方势力</param>
        public static void PrepareBattleFormation(Faction attackingFaction, Faction defendingFaction)
        {
            Console.WriteLine("=== 战斗前AI阵型准备 ===");

            var attackers = GetFactionTroops(attackingFaction);
            var defenders = GetFactionTroops(defendingFaction);

            // 为攻击方分配角色
            AITacticalPositioner.AssignRolesToTroops(attackers);
            
            // 为防守方分配角色
            AITacticalPositioner.AssignRolesToTroops(defenders);

            // 分析双方实力对比
            Console.WriteLine("攻击方配置:");
            PrintFactionRoleDistribution(attackers);
            
            Console.WriteLine("防守方配置:");
            PrintFactionRoleDistribution(defenders);

            // 提供战术建议
            string analysis = AITacticalManager.AnalyzeBattlefield(attackers, defenders);
            Console.WriteLine(analysis);
        }

        /// <summary>
        /// 打印势力的角色分布
        /// </summary>
        /// <param name="troops">部队列表</param>
        private static void PrintFactionRoleDistribution(List<Troop> troops)
        {
            var roleCount = new Dictionary<AIRole, int>();

            foreach (var troop in troops)
            {
                if (troop?.CurrentRole != null)
                {
                    roleCount[troop.CurrentRole] = roleCount.ContainsKey(troop.CurrentRole) ? roleCount[troop.CurrentRole] + 1 : 1;
                }
            }

            foreach (var role in roleCount)
            {
                string roleName = GetRoleDescription(role.Key);
                Console.WriteLine($"  {roleName}: {role.Value}个");
            }
        }

        /// <summary>
        /// 获取角色的中文描述
        /// </summary>
        private static string GetRoleDescription(AIRole role)
        {
            switch (role)
            {
                case AIRole.Tank: return "肉盾";
                case AIRole.DPS: return "输出";
                case AIRole.Mage: return "法师";
                case AIRole.Support: return "辅助";
                case AIRole.Logistics: return "后勤";
                default: return "未定义";
            }
        }

        /// <summary>
        /// 测试AI系统的完整流程
        /// </summary>
        /// <param name="scenario">游戏场景</param>
        public static void TestAISystem(GameScenario scenario)
        {
            Console.WriteLine("=== AI系统测试开始 ===");

            if (scenario?.Factions == null || scenario.Factions.Count < 2)
            {
                Console.WriteLine("测试需要至少2个势力");
                return;
            }

            // 选择前两个势力进行测试
            Faction faction1 = null;
            Faction faction2 = null;
            
            int count = 0;
            foreach (Faction faction in scenario.Factions)
            {
                if (count == 0) faction1 = faction;
                else if (count == 1) faction2 = faction;
                count++;
                if (count >= 2) break;
            }

            Console.WriteLine($"测试势力: {faction1.Name} vs {faction2.Name}");

            // 测试角色分配
            var troops1 = GetFactionTroops(faction1);
            var troops2 = GetFactionTroops(faction2);

            if (troops1.Count == 0 || troops2.Count == 0)
            {
                Console.WriteLine("测试势力没有足够的部队");
                return;
            }

            // 分配角色
            AITacticalPositioner.AssignRolesToTroops(troops1);
            AITacticalPositioner.AssignRolesToTroops(troops2);

            // 执行战术决策
            var decisions1 = AITacticalManager.ExecuteBatchTacticalDecisions(troops1, troops2);
            var decisions2 = AITacticalManager.ExecuteBatchTacticalDecisions(troops2, troops1);

            Console.WriteLine($"{faction1.Name} 产生了 {decisions1.Count} 个战术决策");
            Console.WriteLine($"{faction2.Name} 产生了 {decisions2.Count} 个战术决策");

            Console.WriteLine("=== AI系统测试完成 ===");
        }
    }
}