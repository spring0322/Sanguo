using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 智能AI系统使用示例
    /// </summary>
    public static class SmartAIExample
    {
        /// <summary>
        /// 完整的AI系统演示
        /// </summary>
        public static void RunCompleteDemo()
        {
            Console.WriteLine("=== 智能AI系统完整演示 ===\n");

            try
            {
                // 1. 创建AI管理器
                var aiManager = new SmartAIManager();
                
                // 2. 设置AI难度
                aiManager.SetDifficulty(AIDifficulty.Normal);
                
                // 3. 创建测试场景
                var scenario = CreateTestBattleScenario();
                
                // 4. 创建智能部队
                var smartTroops = CreateSmartTroops();
                
                // 5. 注册部队到AI管理器
                foreach (var troop in smartTroops)
                {
                    var personality = CreatePersonalityForTroop(troop);
                    aiManager.RegisterSmartTroop(troop, personality);
                }
                
                // 6. 模拟战斗回合
                Console.WriteLine("开始战斗模拟...\n");
                
                for (int turn = 1; turn <= 5; turn++)
                {
                    Console.WriteLine($"=== 第 {turn} 回合 ===");
                    
                    // 执行AI回合
                    aiManager.ExecuteAITurns(scenario);
                    
                    // 显示战斗状态
                    DisplayBattleStatus(smartTroops);
                    
                    Console.WriteLine();
                }
                
                // 7. 显示性能统计
                DisplayPerformanceStats(aiManager);
                
                Console.WriteLine("演示完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示过程中发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建测试战斗场景
        /// </summary>
        private static Scenario CreateTestBattleScenario()
        {
            // 这里需要根据实际的Scenario类来实现
            // 简化实现，返回null，实际使用时需要创建真实场景
            return null;
        }

        /// <summary>
        /// 创建智能部队
        /// </summary>
        private static List<SmartTroop> CreateSmartTroops()
        {
            var troops = new List<SmartTroop>();

            // 创建玩家势力部队
            var playerFaction = new Faction { ID = 1, Name = "玩家势力" };
            
            // 英雄单位 - 诸葛亮
            var zhuge = new SmartTroop
            {
                ID = 1001,
                Name = "诸葛亮",
                PersonId = 1,
                CurrentHP = 100,
                MaxHP = 100,
                Attack = 60,
                Defense = 40,
                Intelligence = 95,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(10, 10),
                AttackRange = 2,
                Mobility = 3,
                BelongedFaction = playerFaction
            };
            
            // 为诸葛亮分配技能
            zhuge.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateConfusion(),
                SkillFactory.CreateInspire()
            };
            
            troops.Add(zhuge);

            // 普通部队 - 蜀军步兵
            var infantry = new SmartTroop
            {
                ID = 1002,
                Name = "蜀军步兵",
                PersonId = 0,
                CurrentHP = 80,
                MaxHP = 80,
                Attack = 45,
                Defense = 35,
                Intelligence = 30,
                CurrentPrestige = 50,
                MaxPrestige = 60,
                Position = new Point(9, 10),
                AttackRange = 1,
                Mobility = 2,
                BelongedFaction = playerFaction
            };
            
            infantry.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateHeal()
            };
            
            troops.Add(infantry);

            // 创建敌对势力部队
            var enemyFaction = new Faction { ID = 2, Name = "敌对势力" };
            
            // 敌方英雄 - 司马懿
            var sima = new SmartTroop
            {
                ID = 2001,
                Name = "司马懿",
                PersonId = 2,
                CurrentHP = 95,
                MaxHP = 95,
                Attack = 65,
                Defense = 45,
                Intelligence = 90,
                CurrentPrestige = 85,
                MaxPrestige = 100,
                Position = new Point(15, 15),
                AttackRange = 2,
                Mobility = 3,
                BelongedFaction = enemyFaction
            };
            
            sima.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateThunderStrike(),
                SkillFactory.CreateFreeze(),
                SkillFactory.CreateConfusion()
            };
            
            troops.Add(sima);

            // 敌方部队 - 魏军骑兵
            var cavalry = new SmartTroop
            {
                ID = 2002,
                Name = "魏军骑兵",
                PersonId = 0,
                CurrentHP = 70,
                MaxHP = 70,
                Attack = 55,
                Defense = 25,
                Intelligence = 25,
                CurrentPrestige = 40,
                MaxPrestige = 50,
                Position = new Point(16, 14),
                AttackRange = 1,
                Mobility = 4,
                BelongedFaction = enemyFaction
            };
            
            cavalry.AvailableSkills = new List<Skill>();
            
            troops.Add(cavalry);

            return troops;
        }

        /// <summary>
        /// 为部队创建个性
        /// </summary>
        private static AIPersonality CreatePersonalityForTroop(SmartTroop troop)
        {
            if (troop.IsHero)
            {
                // 英雄有独特个性
                return troop.Name switch
                {
                    "诸葛亮" => new AIPersonality
                    {
                        Aggressiveness = 0.3f,
                        Caution = 0.8f,
                        Teamwork = 0.9f,
                        Creativity = 0.9f,
                        ResourceManagement = 0.8f
                    },
                    "司马懿" => new AIPersonality
                    {
                        Aggressiveness = 0.6f,
                        Caution = 0.7f,
                        Teamwork = 0.6f,
                        Creativity = 0.8f,
                        ResourceManagement = 0.7f
                    },
                    _ => AIPersonality.CreateBalanced()
                };
            }
            else
            {
                // 普通部队使用标准个性
                return troop.Name.Contains("步兵") ? 
                    AIPersonality.CreateDefensive() : 
                    AIPersonality.CreateAggressive();
            }
        }

        /// <summary>
        /// 显示战斗状态
        /// </summary>
        private static void DisplayBattleStatus(List<SmartTroop> troops)
        {
            Console.WriteLine("当前战斗状态:");
            
            foreach (var troop in troops)
            {
                var status = $"  {troop.Name}: HP {troop.CurrentHP}/{troop.MaxHP}, " +
                           $"气力 {troop.CurrentPrestige}/{troop.MaxPrestige}, " +
                           $"位置 ({troop.Position.X}, {troop.Position.Y})";
                
                if (troop.CurrentHP <= 0)
                    status += " [已败北]";
                else if (troop.HpRatio < 0.3f)
                    status += " [重伤]";
                
                Console.WriteLine(status);
            }
        }

        /// <summary>
        /// 显示性能统计
        /// </summary>
        private static void DisplayPerformanceStats(SmartAIManager aiManager)
        {
            Console.WriteLine("\n=== 性能统计 ===");
            
            var stats = aiManager.GetStatistics();
            var perfStats = aiManager.GetPerformanceStats();
            
            Console.WriteLine($"总部队数: {stats.TotalTroops}");
            Console.WriteLine($"存活部队: {stats.ActiveTroops}");
            Console.WriteLine($"总决策次数: {stats.TotalDecisions}");
            Console.WriteLine($"平均决策时间: {stats.AverageDecisionTime:F2} ms");
            Console.WriteLine($"最长回合时间: {perfStats.MaxTurnTime:F2} ms");
            Console.WriteLine($"最短回合时间: {perfStats.MinTurnTime:F2} ms");
        }

        /// <summary>
        /// 测试单个智能部队
        /// </summary>
        public static void TestSingleSmartTroop()
        {
            Console.WriteLine("=== 单个智能部队测试 ===\n");

            try
            {
                // 创建测试部队
                var troop = new SmartTroop
                {
                    ID = 1,
                    Name = "测试部队",
                    CurrentHP = 100,
                    MaxHP = 100,
                    Attack = 50,
                    Defense = 30,
                    Intelligence = 60,
                    CurrentPrestige = 80,
                    MaxPrestige = 100,
                    Position = new Point(5, 5),
                    AttackRange = 1,
                    Mobility = 2
                };

                // 添加技能
                troop.AvailableSkills = new List<Skill>
                {
                    SkillFactory.CreateFireAttack(),
                    SkillFactory.CreateHeal()
                };

                Console.WriteLine($"创建部队: {troop.Name}");
                Console.WriteLine($"初始状态: HP {troop.CurrentHP}/{troop.MaxHP}, 位置 ({troop.Position.X}, {troop.Position.Y})");

                // 执行智能回合
                Console.WriteLine("\n执行智能回合...");
                troop.ExecuteSmartTurn();

                Console.WriteLine($"回合后状态: HP {troop.CurrentHP}/{troop.MaxHP}, 位置 ({troop.Position.X}, {troop.Position.Y})");
                Console.WriteLine("测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试AI个性系统
        /// </summary>
        public static void TestPersonalitySystem()
        {
            Console.WriteLine("=== AI个性系统测试 ===\n");

            var personalities = new[]
            {
                ("攻击型", AIPersonality.CreateAggressive()),
                ("防御型", AIPersonality.CreateDefensive()),
                ("平衡型", AIPersonality.CreateBalanced())
            };

            foreach (var (name, personality) in personalities)
            {
                Console.WriteLine($"{name}个性:");
                Console.WriteLine($"  攻击性: {personality.Aggressiveness:F2}");
                Console.WriteLine($"  谨慎度: {personality.Caution:F2}");
                Console.WriteLine($"  团队合作: {personality.Teamwork:F2}");
                Console.WriteLine($"  创造性: {personality.Creativity:F2}");
                Console.WriteLine($"  资源管理: {personality.ResourceManagement:F2}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public static void StressTest()
        {
            Console.WriteLine("=== AI系统压力测试 ===\n");

            var aiManager = new SmartAIManager();
            var troops = new List<SmartTroop>();

            // 创建大量部队
            int troopCount = 20;
            Console.WriteLine($"创建 {troopCount} 个智能部队...");

            for (int i = 0; i < troopCount; i++)
            {
                var troop = new SmartTroop
                {
                    ID = i + 1,
                    Name = $"部队{i + 1}",
                    CurrentHP = 100,
                    MaxHP = 100,
                    Attack = 40 + i % 20,
                    Defense = 30 + i % 15,
                    Intelligence = 50 + i % 30,
                    CurrentPrestige = 60,
                    MaxPrestige = 80,
                    Position = new Point(i % 10, i / 10),
                    AttackRange = 1,
                    Mobility = 2,
                    BelongedFaction = new Faction { ID = i % 2 + 1, Name = $"势力{i % 2 + 1}" }
                };

                troop.AvailableSkills = new List<Skill>
                {
                    SkillFactory.CreateFireAttack()
                };

                troops.Add(troop);
                aiManager.RegisterSmartTroop(troop);
            }

            // 执行多轮测试
            var startTime = DateTime.Now;
            int rounds = 10;

            Console.WriteLine($"执行 {rounds} 轮AI决策...");

            for (int round = 1; round <= rounds; round++)
            {
                aiManager.ExecuteAITurns(null); // 简化测试，传入null
                
                if (round % 2 == 0)
                {
                    Console.WriteLine($"完成第 {round} 轮");
                }
            }

            var elapsed = DateTime.Now - startTime;
            var stats = aiManager.GetStatistics();

            Console.WriteLine($"\n压力测试结果:");
            Console.WriteLine($"总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"平均每轮: {elapsed.TotalMilliseconds / rounds:F2} ms");
            Console.WriteLine($"平均每部队每轮: {elapsed.TotalMilliseconds / (rounds * troopCount):F4} ms");
            Console.WriteLine($"总决策次数: {stats.TotalDecisions}");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始运行所有智能AI测试...\n");

            TestSingleSmartTroop();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            TestPersonalitySystem();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            StressTest();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            RunCompleteDemo();

            Console.WriteLine("\n所有测试完成！");
        }
    }

    /// <summary>
    /// 简化的势力类（用于测试）
    /// </summary>
    public class Faction
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// 简化的场景类（用于测试）
    /// </summary>
    public class Scenario
    {
        public int CurrentTurn { get; set; }
        public List<SmartTroop> Troops { get; set; } = new List<SmartTroop>();
    }

    /// <summary>
    /// 简化的Point结构（用于测试）
    /// </summary>
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }
}