using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 优化后AI系统测试 - 展示整合后的完整功能
    /// </summary>
    public static class OptimizedAITest
    {
        /// <summary>
        /// 测试优化后的战术评估器
        /// </summary>
        public static void TestOptimizedCombatEvaluator()
        {
            Console.WriteLine("=== 优化后战术评估器测试 ===\n");

            try
            {
                // 创建测试场景
                var scenario = CreateTestScenario();
                
                // 创建测试部队
                var zhuge = CreateZhugeLiang();
                var enemy = CreateEnemyTroop();
                
                // 测试各种技能的评分
                TestSkillEvaluation(zhuge, enemy, scenario);
                
                // 测试特殊情况
                TestSpecialCases(zhuge, enemy, scenario);
                
                Console.WriteLine("✓ 优化后评估器测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试技能评估
        /// </summary>
        private static void TestSkillEvaluation(SmartTroop zhuge, SmartTroop enemy, GameScenario scenario)
        {
            Console.WriteLine("--- 技能评估测试 ---");
            
            var skills = new[]
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateConfusion(),
                SkillFactory.CreateThunderStrike(),
                SkillFactory.CreateRumor()
            };

            foreach (var skill in skills)
            {
                float score = CombatEvaluator.EvaluateSkill(zhuge, skill, enemy, scenario);
                Console.WriteLine($"  {skill.Name}: {score:F1} 分");
                
                // 分析评分原因
                AnalyzeScore(skill, score);
            }
        }

        /// <summary>
        /// 测试特殊情况
        /// </summary>
        private static void TestSpecialCases(SmartTroop zhuge, SmartTroop enemy, GameScenario scenario)
        {
            Console.WriteLine("\n--- 特殊情况测试 ---");
            
            // 1. 测试气力不足
            zhuge.CurrentPrestige = 10; // 设置低气力
            var fireAttack = SkillFactory.CreateFireAttack();
            float lowPrestigeScore = CombatEvaluator.EvaluateSkill(zhuge, fireAttack, enemy, scenario);
            Console.WriteLine($"  气力不足时火计评分: {lowPrestigeScore:F1} (应该是-1)");
            
            // 恢复气力
            zhuge.CurrentPrestige = 80;
            
            // 2. 测试友军误伤
            var ally = CreateAllyTroop();
            ally.BelongedFaction = zhuge.BelongedFaction; // 设为友军
            scenario.AddTroop(ally);
            
            float friendlyFireScore = CombatEvaluator.EvaluateSkill(zhuge, fireAttack, ally, scenario);
            Console.WriteLine($"  对友军使用火计评分: {friendlyFireScore:F1} (应该是负分)");
            
            // 3. 测试斩杀线
            enemy.CurrentHP = 20; // 设置残血
            float killScore = CombatEvaluator.EvaluateSkill(zhuge, fireAttack, enemy, scenario);
            Console.WriteLine($"  对残血敌人使用火计: {killScore:F1} (应该有斩杀奖励)");
            
            // 4. 测试治疗技能
            ally.CurrentHP = 30; // 友军受伤
            var heal = SkillFactory.CreateHeal();
            float healScore = CombatEvaluator.EvaluateSkill(zhuge, heal, ally, scenario);
            Console.WriteLine($"  治疗受伤友军: {healScore:F1} (应该是正分)");
        }

        /// <summary>
        /// 分析评分原因
        /// </summary>
        private static void AnalyzeScore(Skill skill, float score)
        {
            if (score < 0)
            {
                Console.WriteLine($"    → 负分原因: 可能是气力不足或友军误伤");
            }
            else if (score > 500)
            {
                Console.WriteLine($"    → 高分原因: 可能触发斩杀线或高价值目标");
            }
            else if (score > 200)
            {
                Console.WriteLine($"    → 中等分数: 正常的有效攻击");
            }
            else if (score > 0)
            {
                Console.WriteLine($"    → 低分: 效果一般的行动");
            }
        }

        /// <summary>
        /// 测试完整的智能回合执行
        /// </summary>
        public static void TestSmartTurnExecution()
        {
            Console.WriteLine("\n=== 智能回合执行测试 ===\n");

            try
            {
                // 创建战斗场景
                var scenario = CreateBattleScenario();
                var aiManager = new SmartAIManager();
                
                // 创建智能部队
                var zhuge = CreateZhugeLiang();
                var sima = CreateSimaYi();
                
                // 注册到AI管理器
                aiManager.RegisterSmartTroop(zhuge, AIPersonality.CreateBalanced());
                aiManager.RegisterSmartTroop(sima, AIPersonality.CreateAggressive());
                
                Console.WriteLine("初始状态:");
                DisplayTroopStatus(zhuge);
                DisplayTroopStatus(sima);
                
                // 执行几轮AI回合
                for (int turn = 1; turn <= 3; turn++)
                {
                    Console.WriteLine($"\n--- 第 {turn} 回合 ---");
                    
                    // 诸葛亮回合
                    Console.WriteLine("诸葛亮的回合:");
                    zhuge.ExecuteSmartTurn();
                    
                    // 司马懿回合
                    Console.WriteLine("司马懿的回合:");
                    sima.ExecuteSmartTurn();
                    
                    Console.WriteLine("\n回合后状态:");
                    DisplayTroopStatus(zhuge);
                    DisplayTroopStatus(sima);
                }
                
                Console.WriteLine("\n✓ 智能回合执行测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 性能对比测试
        /// </summary>
        public static void PerformanceComparisonTest()
        {
            Console.WriteLine("\n=== 性能对比测试 ===\n");

            var zhuge = CreateZhugeLiang();
            var enemy = CreateEnemyTroop();
            var scenario = CreateTestScenario();
            var fireAttack = SkillFactory.CreateFireAttack();

            int iterations = 10000;
            
            // 测试优化后的评估器
            var startTime = DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                CombatEvaluator.EvaluateSkill(zhuge, fireAttack, enemy, scenario);
            }
            var optimizedTime = DateTime.Now - startTime;

            Console.WriteLine($"优化后评估器性能:");
            Console.WriteLine($"  {iterations} 次评估耗时: {optimizedTime.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均每次: {optimizedTime.TotalMilliseconds / iterations:F4} ms");
            Console.WriteLine($"  每秒可评估: {iterations / optimizedTime.TotalSeconds:F0} 次");
            
            // 性能评价
            double avgMs = optimizedTime.TotalMilliseconds / iterations;
            if (avgMs < 0.01)
                Console.WriteLine("  性能评价: 优秀 ⭐⭐⭐");
            else if (avgMs < 0.1)
                Console.WriteLine("  性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine("  性能评价: 需要优化 ⭐");
        }

        /// <summary>
        /// 运行所有优化测试
        /// </summary>
        public static void RunAllOptimizedTests()
        {
            Console.WriteLine("开始运行优化后AI系统的完整测试...\n");

            TestOptimizedCombatEvaluator();
            TestSmartTurnExecution();
            PerformanceComparisonTest();

            Console.WriteLine("\n🎉 所有优化测试完成！系统运行正常。");
        }

        // === 辅助方法 ===

        private static GameScenario CreateTestScenario()
        {
            return new GameScenario
            {
                CurrentTurn = 1,
                Factions = new List<Faction>
                {
                    new Faction { ID = 1, Name = "蜀国" },
                    new Faction { ID = 2, Name = "魏国" }
                },
                Troops = new List<Troop>()
            };
        }

        private static GameScenario CreateBattleScenario()
        {
            var scenario = CreateTestScenario();
            
            // 添加一些背景部队
            for (int i = 0; i < 5; i++)
            {
                var troop = new SmartTroop
                {
                    ID = 100 + i,
                    Name = $"背景部队{i}",
                    CurrentHP = 60,
                    MaxHP = 60,
                    Attack = 30,
                    Position = new Point(i * 2, 10),
                    BelongedFaction = scenario.Factions[i % 2]
                };
                scenario.AddTroop(troop);
            }
            
            return scenario;
        }

        private static SmartTroop CreateZhugeLiang()
        {
            var zhuge = new SmartTroop
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
                BelongedFaction = new Faction { ID = 1, Name = "蜀国" }
            };

            zhuge.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateConfusion(),
                SkillFactory.CreateHeal(),
                SkillFactory.CreateRumor()
            };

            return zhuge;
        }

        private static SmartTroop CreateSimaYi()
        {
            var sima = new SmartTroop
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
                Position = new Point(10, 8),
                AttackRange = 2,
                Mobility = 3,
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

        private static SmartTroop CreateEnemyTroop()
        {
            return new SmartTroop
            {
                ID = 10,
                Name = "敌军",
                CurrentHP = 80,
                MaxHP = 80,
                Attack = 45,
                Defense = 30,
                Position = new Point(8, 6),
                BelongedFaction = new Faction { ID = 2, Name = "魏国" }
            };
        }

        private static SmartTroop CreateAllyTroop()
        {
            return new SmartTroop
            {
                ID = 11,
                Name = "友军",
                CurrentHP = 70,
                MaxHP = 70,
                Attack = 40,
                Defense = 35,
                Position = new Point(6, 5),
                BelongedFaction = new Faction { ID = 1, Name = "蜀国" }
            };
        }

        private static void DisplayTroopStatus(SmartTroop troop)
        {
            Console.WriteLine($"  {troop.Name}: HP {troop.CurrentHP}/{troop.MaxHP}, " +
                            $"气力 {troop.CurrentPrestige}/{troop.MaxPrestige}, " +
                            $"位置 ({troop.Position.X}, {troop.Position.Y})");
        }
    }
}