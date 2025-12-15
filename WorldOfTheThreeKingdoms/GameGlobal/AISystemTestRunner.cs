using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// AI系统测试运行器 - 可以在游戏中调用来测试AI系统
    /// </summary>
    public static class AISystemTestRunner
    {
        /// <summary>
        /// 快速测试AI系统基本功能
        /// </summary>
        public static void QuickTest()
        {
            Console.WriteLine("=== AI系统快速测试 ===");
            
            try
            {
                // 1. 测试技能创建
                var fireSkill = SkillFactory.CreateFireAttack();
                Console.WriteLine($"✓ 技能创建成功: {fireSkill.Name}");

                // 2. 测试战术评估器
                var attacker = CreateTestTroop("测试攻击者", 100, 100, 50);
                var target = CreateTestTroop("测试目标", 80, 100, 30);
                var scenario = CreateTestScenario();

                attacker.BelongedFaction = scenario.Factions[0];
                target.BelongedFaction = scenario.Factions[1];

                float score = CombatEvaluator.EvaluateSkill(attacker, fireSkill, target, scenario);
                Console.WriteLine($"✓ 战术评估成功: 评分 {score:F1}");

                // 3. 测试AI决策管理器
                var aiManager = new AIDecisionManager();
                attacker.AvailableSkills = new List<Skill> { fireSkill };
                scenario.AddTroop(target);

                var decision = aiManager.MakeCombatDecision(attacker, scenario);
                Console.WriteLine($"✓ AI决策成功: {decision.Action}");

                // 4. 测试技能学习系统
                SkillLearningSystem.LearnSkill(1, fireSkill.ID);
                var learnedSkills = SkillLearningSystem.GetPersonSkills(1);
                Console.WriteLine($"✓ 技能学习成功: 学会 {learnedSkills.Count} 个技能");

                Console.WriteLine("✓ 所有测试通过！AI系统运行正常。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
                Console.WriteLine($"详细信息: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 详细测试AI系统各个组件
        /// </summary>
        public static void DetailedTest()
        {
            Console.WriteLine("=== AI系统详细测试 ===");
            
            TestSkillFactory();
            TestCombatEvaluator();
            TestAIDecisionManager();
            TestSkillLearningSystem();
            
            Console.WriteLine("=== 详细测试完成 ===");
        }

        private static void TestSkillFactory()
        {
            Console.WriteLine("\n--- 测试技能工厂 ---");
            
            var allSkills = SkillFactory.GetAllSkills();
            Console.WriteLine($"预定义技能数量: {allSkills.Count}");
            
            foreach (var skill in allSkills)
            {
                Console.WriteLine($"  {skill.Name}: 消耗{skill.Cost}, 威力{skill.Power}, 影响数{skill.Influences.Count}");
            }
        }

        private static void TestCombatEvaluator()
        {
            Console.WriteLine("\n--- 测试战术评估器 ---");
            
            var attacker = CreateTestTroop("强力攻击者", 100, 100, 80);
            var weakTarget = CreateTestTroop("弱小目标", 20, 100, 20);
            var strongTarget = CreateTestTroop("强大目标", 100, 100, 70);
            var ally = CreateTestTroop("友军", 50, 100, 40);
            
            var scenario = CreateTestScenario();
            attacker.BelongedFaction = scenario.Factions[0];
            weakTarget.BelongedFaction = scenario.Factions[1];
            strongTarget.BelongedFaction = scenario.Factions[1];
            ally.BelongedFaction = scenario.Factions[0];

            var fireSkill = SkillFactory.CreateFireAttack();
            var healSkill = SkillFactory.CreateHeal();

            // 测试不同目标的评分
            float scoreWeak = CombatEvaluator.EvaluateSkill(attacker, fireSkill, weakTarget, scenario);
            float scoreStrong = CombatEvaluator.EvaluateSkill(attacker, fireSkill, strongTarget, scenario);
            float scoreFriendly = CombatEvaluator.EvaluateSkill(attacker, fireSkill, ally, scenario);
            float scoreHeal = CombatEvaluator.EvaluateSkill(attacker, healSkill, ally, scenario);

            Console.WriteLine($"  火计 vs 弱敌: {scoreWeak:F1}");
            Console.WriteLine($"  火计 vs 强敌: {scoreStrong:F1}");
            Console.WriteLine($"  火计 vs 友军: {scoreFriendly:F1} (应该是负分)");
            Console.WriteLine($"  治疗 vs 友军: {scoreHeal:F1}");
        }

        private static void TestAIDecisionManager()
        {
            Console.WriteLine("\n--- 测试AI决策管理器 ---");
            
            var aiTroop = CreateTestTroop("AI部队", 100, 100, 60);
            var enemy = CreateTestTroop("敌人", 50, 100, 40);
            var scenario = CreateTestScenario();

            aiTroop.BelongedFaction = scenario.Factions[0];
            enemy.BelongedFaction = scenario.Factions[1];
            enemy.Position = new Point(6, 5);

            aiTroop.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateHeal(),
                SkillFactory.CreateConfusion()
            };

            scenario.AddTroop(enemy);

            var aiManager = new AIDecisionManager();
            
            // 测试多次决策
            for (int i = 0; i < 3; i++)
            {
                var decision = aiManager.MakeCombatDecision(aiTroop, scenario);
                Console.WriteLine($"  决策 {i+1}: {decision.Action}, 评分: {decision.Score:F1}");
                if (decision.Skill != null)
                {
                    Console.WriteLine($"    技能: {decision.Skill.Name}, 目标: {decision.Target?.Name}");
                }
            }
        }

        private static void TestSkillLearningSystem()
        {
            Console.WriteLine("\n--- 测试技能学习系统 ---");
            
            // 初始化默认技能
            SkillLearningSystem.InitializeDefaultSkills();
            
            // 测试几个人物的技能
            var persons = new[] { 1, 2, 3 };
            foreach (var personId in persons)
            {
                var skills = SkillLearningSystem.GetPersonSkills(personId);
                Console.WriteLine($"  人物 {personId}: 掌握 {skills.Count} 个技能");
                foreach (var skill in skills)
                {
                    Console.WriteLine($"    - {skill.Name}");
                }
            }
        }

        // === 辅助方法 ===

        private static Troop CreateTestTroop(string name, int currentHp, int maxHp, int attack)
        {
            return new Troop
            {
                ID = new Random().Next(1000, 9999),
                Name = name,
                CurrentHP = currentHp,
                MaxHP = maxHp,
                Attack = attack,
                Defense = 20,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 1,
                AvailableSkills = new List<Skill>(),
                PersonId = 0
            };
        }

        private static GameScenario CreateTestScenario()
        {
            return new GameScenario
            {
                CurrentTurn = 1,
                Factions = new List<Faction>
                {
                    new Faction { ID = 1, Name = "玩家势力" },
                    new Faction { ID = 2, Name = "敌对势力" }
                },
                Troops = new List<Troop>()
            };
        }

        /// <summary>
        /// 性能基准测试
        /// </summary>
        public static void BenchmarkTest()
        {
            Console.WriteLine("\n=== AI系统性能基准测试 ===");
            
            var attacker = CreateTestTroop("攻击者", 100, 100, 50);
            var target = CreateTestTroop("目标", 100, 100, 40);
            var scenario = CreateTestScenario();
            var skill = SkillFactory.CreateFireAttack();

            attacker.BelongedFaction = scenario.Factions[0];
            target.BelongedFaction = scenario.Factions[1];

            // 测试评估器性能
            int iterations = 1000;
            var startTime = DateTime.Now;

            for (int i = 0; i < iterations; i++)
            {
                CombatEvaluator.EvaluateSkill(attacker, skill, target, scenario);
            }

            var elapsed = DateTime.Now - startTime;
            double avgTime = elapsed.TotalMilliseconds / iterations;

            Console.WriteLine($"评估器性能测试:");
            Console.WriteLine($"  {iterations} 次评估耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均每次: {avgTime:F4} ms");
            Console.WriteLine($"  每秒可评估: {1000.0 / avgTime:F0} 次");

            // 测试决策管理器性能
            var aiManager = new AIDecisionManager();
            attacker.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateHeal(),
                SkillFactory.CreateConfusion()
            };
            scenario.AddTroop(target);

            startTime = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                aiManager.MakeCombatDecision(attacker, scenario);
            }
            elapsed = DateTime.Now - startTime;

            Console.WriteLine($"\n决策管理器性能测试:");
            Console.WriteLine($"  100 次决策耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均每次: {elapsed.TotalMilliseconds / 100:F2} ms");
        }

        /// <summary>
        /// 在游戏中调用此方法来运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            QuickTest();
            DetailedTest();
            BenchmarkTest();
        }
    }
}