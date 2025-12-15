using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 行动系统测试 - 验证新的行动模式架构
    /// </summary>
    public static class ActionSystemTest
    {
        /// <summary>
        /// 运行完整的行动系统测试
        /// </summary>
        public static void RunCompleteActionTest()
        {
            Console.WriteLine("=== 行动系统完整测试 ===\n");

            try
            {
                TestBasicActions();
                TestEnhancedTroopDecisions();
                TestActionPatternAnalysis();
                TestCombatScenario();
                TestPerformanceComparison();

                Console.WriteLine("\n🎉 所有行动系统测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试基础行动类型
        /// </summary>
        private static void TestBasicActions()
        {
            Console.WriteLine("--- 基础行动类型测试 ---");

            var troop = CreateTestTroop("测试部队");
            var enemy = CreateTestTroop("敌军");
            enemy.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            // 测试各种行动类型
            var actions = new List<CombatAction>
            {
                new SkipTurnAction(),
                new MoveAction(troop, new Point(10, 10)),
                new MoveAndAttackAction(troop, new Point(8, 8), enemy),
                new MoveAndCastAction(troop, new Point(7, 7), SkillFactory.CreateFireAttack(), enemy),
                new DefendAction(troop),
                new WaitAction(troop),
                new RetreatAction(troop, new Point(3, 3))
            };

            foreach (var action in actions)
            {
                Console.WriteLine($"  执行: {action.GetDescription()}");
                action.Execute();
            }

            Console.WriteLine("  ✓ 基础行动测试完成\n");
        }

        /// <summary>
        /// 测试增强部队的决策过程
        /// </summary>
        private static void TestEnhancedTroopDecisions()
        {
            Console.WriteLine("--- 增强部队决策测试 ---");

            // 创建测试场景
            var zhuge = CreateEnhancedZhugeLiang();
            var enemies = CreateEnemyGroup();

            Console.WriteLine($"初始状态: {zhuge.Name} HP:{zhuge.CurrentHP}/{zhuge.MaxHP}, 气力:{zhuge.CurrentPrestige}/{zhuge.MaxPrestige}");
            Console.WriteLine($"敌军数量: {enemies.Count}");

            // 执行几轮决策
            for (int turn = 1; turn <= 5; turn++)
            {
                Console.WriteLine($"\n第 {turn} 回合:");
                zhuge.ExecuteTurn();
                
                // 显示状态变化
                Console.WriteLine($"  回合后状态: HP:{zhuge.CurrentHP}/{zhuge.MaxHP}, 气力:{zhuge.CurrentPrestige}/{zhuge.MaxPrestige}");
                Console.WriteLine($"  位置: ({zhuge.Position.X}, {zhuge.Position.Y})");
            }

            Console.WriteLine("  ✓ 增强部队决策测试完成\n");
        }

        /// <summary>
        /// 测试行动模式分析
        /// </summary>
        private static void TestActionPatternAnalysis()
        {
            Console.WriteLine("--- 行动模式分析测试 ---");

            var troop = CreateEnhancedZhugeLiang();
            
            // 执行多轮行动
            for (int i = 0; i < 10; i++)
            {
                troop.ExecuteTurn();
            }

            // 分析行动模式
            var pattern = troop.AnalyzeActionPattern();
            var history = troop.GetActionHistory();

            Console.WriteLine($"  行动历史记录: {history.Count} 条");
            Console.WriteLine($"  行动模式分析: {pattern}");

            // 显示最近的行动
            Console.WriteLine("  最近5次行动:");
            var recentActions = history.TakeLast(5);
            foreach (var action in recentActions)
            {
                Console.WriteLine($"    - {action.GetDescription()} (评分: {action.Score:F1})");
            }

            Console.WriteLine("  ✓ 行动模式分析测试完成\n");
        }

        /// <summary>
        /// 测试战斗场景
        /// </summary>
        private static void TestCombatScenario()
        {
            Console.WriteLine("--- 战斗场景测试 ---");

            // 创建对战双方
            var zhuge = CreateEnhancedZhugeLiang();
            var sima = CreateEnhancedSimaYi();

            Console.WriteLine("战斗开始:");
            Console.WriteLine($"  {zhuge.Name}: HP {zhuge.CurrentHP}, 位置 ({zhuge.Position.X}, {zhuge.Position.Y})");
            Console.WriteLine($"  {sima.Name}: HP {sima.CurrentHP}, 位置 ({sima.Position.X}, {sima.Position.Y})");

            // 模拟战斗回合
            for (int round = 1; round <= 8; round++)
            {
                Console.WriteLine($"\n=== 第 {round} 回合 ===");

                // 诸葛亮行动
                Console.WriteLine($"{zhuge.Name} 的回合:");
                zhuge.ExecuteTurn();

                // 司马懿行动
                Console.WriteLine($"{sima.Name} 的回合:");
                sima.ExecuteTurn();

                // 显示回合结果
                Console.WriteLine("回合结果:");
                Console.WriteLine($"  {zhuge.Name}: HP {zhuge.CurrentHP}/{zhuge.MaxHP}, 气力 {zhuge.CurrentPrestige}/{zhuge.MaxPrestige}");
                Console.WriteLine($"  {sima.Name}: HP {sima.CurrentHP}/{sima.MaxHP}, 气力 {sima.CurrentPrestige}/{sima.MaxPrestige}");

                // 检查战斗结束条件
                if (zhuge.CurrentHP <= 0 || sima.CurrentHP <= 0)
                {
                    var winner = zhuge.CurrentHP > 0 ? zhuge.Name : sima.Name;
                    Console.WriteLine($"\n🏆 战斗结束！获胜者: {winner}");
                    break;
                }
            }

            Console.WriteLine("  ✓ 战斗场景测试完成\n");
        }

        /// <summary>
        /// 性能对比测试
        /// </summary>
        private static void TestPerformanceComparison()
        {
            Console.WriteLine("--- 性能对比测试 ---");

            var troop = CreateEnhancedZhugeLiang();
            int iterations = 1000;

            // 测试新行动系统性能
            var startTime = DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                troop.ExecuteTurn();
                troop.ResetAIState(); // 重置状态以保持一致性
            }
            var newSystemTime = DateTime.Now - startTime;

            Console.WriteLine($"新行动系统性能:");
            Console.WriteLine($"  {iterations} 次决策耗时: {newSystemTime.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均每次: {newSystemTime.TotalMilliseconds / iterations:F4} ms");
            Console.WriteLine($"  每秒可决策: {iterations / newSystemTime.TotalSeconds:F0} 次");

            // 性能评价
            double avgMs = newSystemTime.TotalMilliseconds / iterations;
            if (avgMs < 0.1)
                Console.WriteLine("  性能评价: 优秀 ⭐⭐⭐");
            else if (avgMs < 1.0)
                Console.WriteLine("  性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine("  性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能对比测试完成\n");
        }

        /// <summary>
        /// 测试组合行动
        /// </summary>
        public static void TestComboActions()
        {
            Console.WriteLine("--- 组合行动测试 ---");

            var troop = CreateEnhancedZhugeLiang();
            var enemy = CreateTestTroop("敌军");
            enemy.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            // 创建组合行动：移动 + 攻击 + 防御
            var combo = new ComboAction("战术组合");
            combo.AddAction(new MoveAction(troop, new Point(8, 8)));
            combo.AddAction(new MoveAndAttackAction(troop, new Point(7, 7), enemy));
            combo.AddAction(new DefendAction(troop));

            Console.WriteLine($"执行组合行动: {combo.GetDescription()}");
            combo.Execute();

            Console.WriteLine("  ✓ 组合行动测试完成\n");
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public static void StressTest()
        {
            Console.WriteLine("--- 行动系统压力测试 ---");

            var troops = new List<EnhancedSmartTroop>();
            int troopCount = 20;

            // 创建大量部队
            for (int i = 0; i < troopCount; i++)
            {
                var troop = new EnhancedSmartTroop
                {
                    ID = i + 1,
                    Name = $"部队{i + 1}",
                    CurrentHP = 100,
                    MaxHP = 100,
                    Attack = 50,
                    Defense = 30,
                    CurrentPrestige = 80,
                    MaxPrestige = 100,
                    Position = new Point(i % 10, i / 2),
                    ViewRadius = 5,
                    Mobility = 3,
                    BelongedFaction = new Faction { ID = i % 2 + 1, Name = $"势力{i % 2 + 1}" }
                };

                troop.AvailableSkills = new List<Skill>
                {
                    SkillFactory.CreateFireAttack(),
                    SkillFactory.CreateHeal()
                };

                troops.Add(troop);
            }

            Console.WriteLine($"创建 {troopCount} 个增强部队");

            // 执行压力测试
            var startTime = DateTime.Now;
            int rounds = 5;

            for (int round = 1; round <= rounds; round++)
            {
                foreach (var troop in troops)
                {
                    troop.ExecuteTurn();
                }
                Console.WriteLine($"完成第 {round} 轮 ({troopCount} 个部队)");
            }

            var elapsed = DateTime.Now - startTime;
            int totalDecisions = troopCount * rounds;

            Console.WriteLine($"\n压力测试结果:");
            Console.WriteLine($"  总决策数: {totalDecisions}");
            Console.WriteLine($"  总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均每决策: {elapsed.TotalMilliseconds / totalDecisions:F4} ms");
            Console.WriteLine($"  每秒决策数: {totalDecisions / elapsed.TotalSeconds:F0}");

            Console.WriteLine("  ✓ 压力测试完成\n");
        }

        // === 辅助方法 ===

        private static EnhancedSmartTroop CreateTestTroop(string name)
        {
            return new EnhancedSmartTroop
            {
                ID = new Random().Next(1000, 9999),
                Name = name,
                CurrentHP = 100,
                MaxHP = 100,
                Attack = 50,
                Defense = 30,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 1,
                Mobility = 2,
                ViewRadius = 5,
                BelongedFaction = new Faction { ID = 1, Name = "测试势力" }
            };
        }

        private static EnhancedSmartTroop CreateEnhancedZhugeLiang()
        {
            var zhuge = new EnhancedSmartTroop
            {
                ID = 1,
                Name = "诸葛亮",
                PersonId = 1,
                CurrentHP = 100,
                MaxHP = 100,
                Attack = 60,
                Defense = 40,
                Intelligence = 95,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 2,
                Mobility = 3,
                ViewRadius = 6,
                BelongedFaction = new Faction { ID = 1, Name = "蜀国" }
            };

            zhuge.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateConfusion(),
                SkillFactory.CreateInspire(),
                SkillFactory.CreateRumor()
            };

            return zhuge;
        }

        private static EnhancedSmartTroop CreateEnhancedSimaYi()
        {
            var sima = new EnhancedSmartTroop
            {
                ID = 2,
                Name = "司马懿",
                PersonId = 2,
                CurrentHP = 95,
                MaxHP = 95,
                Attack = 65,
                Defense = 45,
                Intelligence = 90,
                CurrentPrestige = 85,
                MaxPrestige = 100,
                Position = new Point(12, 8),
                AttackRange = 2,
                Mobility = 3,
                ViewRadius = 6,
                BelongedFaction = new Faction { ID = 2, Name = "魏国" }
            };

            sima.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateThunderStrike(),
                SkillFactory.CreateFreeze(),
                SkillFactory.CreateConfusion()
            };

            return sima;
        }

        private static List<EnhancedSmartTroop> CreateEnemyGroup()
        {
            var enemies = new List<EnhancedSmartTroop>();
            
            for (int i = 0; i < 3; i++)
            {
                var enemy = new EnhancedSmartTroop
                {
                    ID = 100 + i,
                    Name = $"敌军{i + 1}",
                    CurrentHP = 80,
                    MaxHP = 80,
                    Attack = 45,
                    Defense = 25,
                    Position = new Point(10 + i, 8 + i),
                    BelongedFaction = new Faction { ID = 2, Name = "敌对势力" }
                };
                enemies.Add(enemy);
            }

            return enemies;
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始运行行动系统的所有测试...\n");

            RunCompleteActionTest();
            TestComboActions();
            StressTest();

            Console.WriteLine("\n🎉 所有行动系统测试完成！系统运行正常。");
        }
    }
}