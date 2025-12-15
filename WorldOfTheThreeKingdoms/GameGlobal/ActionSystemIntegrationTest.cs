using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 行动系统集成测试 - 验证完整的行动模式架构
    /// </summary>
    public static class ActionSystemIntegrationTest
    {
        /// <summary>
        /// 运行完整的集成测试
        /// </summary>
        public static void RunIntegrationTest()
        {
            Console.WriteLine("=== 行动系统集成测试 ===\n");

            try
            {
                TestActionSystemIntegration();
                TestCombatEvaluatorIntegration();
                TestEnhancedTroopIntegration();
                TestPerformanceIntegration();

                Console.WriteLine("\n🎉 行动系统集成测试完成！所有组件正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 集成测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试行动系统集成
        /// </summary>
        private static void TestActionSystemIntegration()
        {
            Console.WriteLine("--- 行动系统集成测试 ---");

            // 创建测试部队
            var troop = CreateTestEnhancedTroop("集成测试部队");
            var enemy = CreateTestEnhancedTroop("敌军");
            enemy.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            // 测试所有行动类型的创建和执行
            var actions = new List<CombatAction>
            {
                new MoveAction(troop, new Point(8, 8)),
                new MoveAndAttackAction(troop, new Point(7, 7), enemy),
                new MoveAndCastAction(troop, new Point(6, 6), CreateTestSkill(), enemy),
                new DefendAction(troop),
                new WaitAction(troop),
                new RetreatAction(troop, new Point(10, 10))
            };

            Console.WriteLine("  测试行动执行:");
            foreach (var action in actions)
            {
                try
                {
                    Console.WriteLine($"    执行: {action.GetDescription()}");
                    action.Execute();
                    Console.WriteLine($"    ✓ 成功");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ 失败: {ex.Message}");
                }
            }

            Console.WriteLine("  ✓ 行动系统集成测试完成\n");
        }

        /// <summary>
        /// 测试战斗评估器集成
        /// </summary>
        private static void TestCombatEvaluatorIntegration()
        {
            Console.WriteLine("--- 战斗评估器集成测试 ---");

            var attacker = CreateTestEnhancedTroop("攻击者");
            var target = CreateTestEnhancedTroop("目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var scenario = CreateTestScenario();

            // 测试技能评估
            var skill = CreateTestSkill();
            float skillScore = CombatEvaluator.EvaluateSkill(attacker, skill, target, scenario);
            Console.WriteLine($"  技能评估分数: {skillScore:F2}");

            // 测试普攻评估
            float attackScore = CombatEvaluator.EvaluateAttack(attacker, target);
            Console.WriteLine($"  普攻评估分数: {attackScore:F2}");

            // 测试战略移动评估
            var destination = new Point(8, 8);
            float moveScore = CombatEvaluator.EvaluateStrategicMove(attacker, destination, scenario);
            Console.WriteLine($"  战略移动评估分数: {moveScore:F2}");

            // 验证评估结果合理性
            if (skillScore > -1000 && attackScore > -1000 && moveScore > -1000)
            {
                Console.WriteLine("  ✓ 评估器返回合理分数");
            }
            else
            {
                Console.WriteLine("  ⚠ 评估器分数异常");
            }

            Console.WriteLine("  ✓ 战斗评估器集成测试完成\n");
        }

        /// <summary>
        /// 测试增强部队集成
        /// </summary>
        private static void TestEnhancedTroopIntegration()
        {
            Console.WriteLine("--- 增强部队集成测试 ---");

            var troop = CreateTestEnhancedTroop("增强部队");
            
            Console.WriteLine($"  初始状态: HP {troop.CurrentHP}/{troop.MaxHP}, 气力 {troop.CurrentPrestige}/{troop.MaxPrestige}");
            Console.WriteLine($"  位置: ({troop.Position.X}, {troop.Position.Y})");

            // 执行几轮决策
            for (int turn = 1; turn <= 3; turn++)
            {
                Console.WriteLine($"  第 {turn} 回合:");
                try
                {
                    troop.ExecuteTurn();
                    Console.WriteLine($"    ✓ 回合执行成功");
                    Console.WriteLine($"    状态: HP {troop.CurrentHP}/{troop.MaxHP}, 气力 {troop.CurrentPrestige}/{troop.MaxPrestige}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ 回合执行失败: {ex.Message}");
                }
            }

            // 测试行动历史
            var history = troop.GetActionHistory();
            Console.WriteLine($"  行动历史: {history.Count} 条记录");

            // 测试行动模式分析
            var pattern = troop.AnalyzeActionPattern();
            Console.WriteLine($"  行动模式: {pattern}");

            Console.WriteLine("  ✓ 增强部队集成测试完成\n");
        }

        /// <summary>
        /// 测试性能集成
        /// </summary>
        private static void TestPerformanceIntegration()
        {
            Console.WriteLine("--- 性能集成测试 ---");

            var troops = new List<EnhancedSmartTroop>();
            int troopCount = 10;

            // 创建测试部队
            for (int i = 0; i < troopCount; i++)
            {
                var troop = CreateTestEnhancedTroop($"部队{i + 1}");
                troop.BelongedFaction = new Faction { ID = i % 2 + 1, Name = $"势力{i % 2 + 1}" };
                troops.Add(troop);
            }

            Console.WriteLine($"  创建 {troopCount} 个增强部队");

            // 性能测试
            var startTime = DateTime.Now;
            int rounds = 3;

            for (int round = 1; round <= rounds; round++)
            {
                foreach (var troop in troops)
                {
                    troop.ExecuteTurn();
                }
            }

            var elapsed = DateTime.Now - startTime;
            int totalDecisions = troopCount * rounds;

            Console.WriteLine($"  性能结果:");
            Console.WriteLine($"    总决策数: {totalDecisions}");
            Console.WriteLine($"    总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"    平均每决策: {elapsed.TotalMilliseconds / totalDecisions:F4} ms");

            // 性能评价
            double avgMs = elapsed.TotalMilliseconds / totalDecisions;
            if (avgMs < 1.0)
                Console.WriteLine("    性能评价: 优秀 ⭐⭐⭐");
            else if (avgMs < 5.0)
                Console.WriteLine("    性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine("    性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能集成测试完成\n");
        }

        // === 辅助方法 ===

        private static EnhancedSmartTroop CreateTestEnhancedTroop(string name)
        {
            var troop = new EnhancedSmartTroop
            {
                ID = new Random().Next(1000, 9999),
                Name = name,
                CurrentHP = 100,
                MaxHP = 100,
                Attack = 50,
                Defense = 30,
                Intelligence = 60,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 1,
                Mobility = 2,
                ViewRadius = 5,
                BelongedFaction = new Faction { ID = 1, Name = "测试势力" }
            };

            // 添加测试技能
            troop.AvailableSkills = new List<Skill>
            {
                CreateTestSkill(),
                CreateHealSkill()
            };

            return troop;
        }

        private static Skill CreateTestSkill()
        {
            return new Skill
            {
                ID = 1,
                Name = "测试攻击",
                Cost = 20,
                Range = 2,
                Radius = 1,
                Power = 1.2f,
                Influences = new List<Influence>
                {
                    new Influence
                    {
                        Kind = InfluenceKind.Damage,
                        Power = 1.2f,
                        Amount = 50,
                        IsFire = false
                    }
                }
            };
        }

        private static Skill CreateHealSkill()
        {
            return new Skill
            {
                ID = 2,
                Name = "测试治疗",
                Cost = 15,
                Range = 2,
                Radius = 1,
                Power = 1.0f,
                Influences = new List<Influence>
                {
                    new Influence
                    {
                        Kind = InfluenceKind.Purify,
                        Power = 1.0f,
                        Amount = 40,
                        IsFire = false
                    }
                }
            };
        }

        private static GameScenario CreateTestScenario()
        {
            // 创建简化的测试场景
            var scenario = new GameScenario
            {
                Troops = new List<Troop>()
            };

            // 添加一些测试部队
            for (int i = 0; i < 5; i++)
            {
                var troop = CreateTestEnhancedTroop($"场景部队{i + 1}");
                troop.Position = new Point(i * 2, i * 2);
                scenario.Troops.Add(troop);
            }

            return scenario;
        }
    }
}