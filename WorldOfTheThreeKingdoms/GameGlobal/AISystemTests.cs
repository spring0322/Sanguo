using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// AI系统测试类 - 用于验证战术评估器的正确性
    /// </summary>
    public static class AISystemTests
    {
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== 开始AI系统测试 ===\n");

            TestDamageEvaluation();
            TestHealingEvaluation();
            TestControlEvaluation();
            TestKillPotential();
            TestFriendlyFire();
            TestResourceManagement();
            TestDecisionMaking();

            Console.WriteLine("\n=== 所有测试完成 ===");
        }

        /// <summary>
        /// 测试伤害评估
        /// </summary>
        private static void TestDamageEvaluation()
        {
            Console.WriteLine("测试1: 伤害评估");

            // 创建测试数据
            var attacker = CreateTestTroop("攻击者", 100, 100, 50);
            var target = CreateTestTroop("目标", 50, 100, 30);
            var scenario = CreateTestScenario();

            // 创建火计技能
            var fireAttack = SkillFactory.CreateFireAttack();

            // 评估技能
            float score = CombatEvaluator.EvaluateSkill(attacker, fireAttack, target, scenario);

            Console.WriteLine($"  火计对敌人的评分: {score:F1}");
            Console.WriteLine($"  预期: 正分 (有效攻击)");
            Console.WriteLine($"  结果: {(score > 0 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试治疗评估
        /// </summary>
        private static void TestHealingEvaluation()
        {
            Console.WriteLine("测试2: 治疗评估");

            var healer = CreateTestTroop("治疗者", 100, 100, 40);
            var wounded = CreateTestTroop("受伤友军", 30, 100, 40); // 血量30%
            var scenario = CreateTestScenario();

            // 设置为友军
            healer.BelongedFaction = scenario.Factions[0];
            wounded.BelongedFaction = scenario.Factions[0];

            var healSkill = SkillFactory.CreateHeal();
            float score = CombatEvaluator.EvaluateSkill(healer, healSkill, wounded, scenario);

            Console.WriteLine($"  治疗受伤友军的评分: {score:F1}");
            Console.WriteLine($"  预期: 高分 (血量低，需要治疗)");
            Console.WriteLine($"  结果: {(score > 200 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试控制技能评估
        /// </summary>
        private static void TestControlEvaluation()
        {
            Console.WriteLine("测试3: 控制技能评估");

            var caster = CreateTestTroop("施法者", 100, 100, 45);
            var enemy = CreateTestTroop("敌人", 100, 100, 50);
            var scenario = CreateTestScenario();

            // 设置为敌对
            caster.BelongedFaction = scenario.Factions[0];
            enemy.BelongedFaction = scenario.Factions[1];

            var confusionSkill = SkillFactory.CreateConfusion();
            float score = CombatEvaluator.EvaluateSkill(caster, confusionSkill, enemy, scenario);

            Console.WriteLine($"  混乱术对敌人的评分: {score:F1}");
            Console.WriteLine($"  预期: 正分 (控制敌人有价值)");
            Console.WriteLine($"  结果: {(score > 100 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试斩杀线判断
        /// </summary>
        private static void TestKillPotential()
        {
            Console.WriteLine("测试4: 斩杀线判断");

            var attacker = CreateTestTroop("攻击者", 100, 100, 80);
            var lowHpEnemy = CreateTestTroop("残血敌人", 10, 100, 30); // 仅10点血
            var scenario = CreateTestScenario();

            // 设置为敌对
            attacker.BelongedFaction = scenario.Factions[0];
            lowHpEnemy.BelongedFaction = scenario.Factions[1];

            var fireAttack = SkillFactory.CreateFireAttack();
            float score = CombatEvaluator.EvaluateSkill(attacker, fireAttack, lowHpEnemy, scenario);

            Console.WriteLine($"  对残血敌人使用火计的评分: {score:F1}");
            Console.WriteLine($"  预期: 高分 (可以击杀)");
            Console.WriteLine($"  结果: {(score > 150 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试友军误伤检测
        /// </summary>
        private static void TestFriendlyFire()
        {
            Console.WriteLine("测试5: 友军误伤检测");

            var attacker = CreateTestTroop("攻击者", 100, 100, 50);
            var ally = CreateTestTroop("友军", 100, 100, 40);
            var scenario = CreateTestScenario();

            // 设置为友军
            attacker.BelongedFaction = scenario.Factions[0];
            ally.BelongedFaction = scenario.Factions[0];

            var fireAttack = SkillFactory.CreateFireAttack();
            float score = CombatEvaluator.EvaluateSkill(attacker, fireAttack, ally, scenario);

            Console.WriteLine($"  对友军使用火计的评分: {score:F1}");
            Console.WriteLine($"  预期: 严重负分 (误伤友军)");
            Console.WriteLine($"  结果: {(score < -500 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试资源管理
        /// </summary>
        private static void TestResourceManagement()
        {
            Console.WriteLine("测试6: 资源管理");

            var lowPrestige = CreateTestTroop("低气力单位", 100, 100, 50);
            lowPrestige.CurrentPrestige = 10; // 气力很低
            lowPrestige.MaxPrestige = 100;

            var target = CreateTestTroop("目标", 100, 100, 40);
            var scenario = CreateTestScenario();

            // 设置为敌对
            lowPrestige.BelongedFaction = scenario.Factions[0];
            target.BelongedFaction = scenario.Factions[1];

            // 尝试使用高消耗技能
            var expensiveSkill = SkillFactory.CreateMassHeal(); // 消耗50气力
            float score = CombatEvaluator.EvaluateSkill(lowPrestige, expensiveSkill, target, scenario);

            Console.WriteLine($"  气力不足时使用高消耗技能的评分: {score:F1}");
            Console.WriteLine($"  预期: 极低分 (气力不足)");
            Console.WriteLine($"  结果: {(score < -9000 ? "通过" : "失败")}\n");
        }

        /// <summary>
        /// 测试完整决策流程
        /// </summary>
        private static void TestDecisionMaking()
        {
            Console.WriteLine("测试7: 完整决策流程");

            var aiTroop = CreateTestTroop("AI部队", 100, 100, 60);
            aiTroop.CurrentPrestige = 80;
            aiTroop.MaxPrestige = 100;

            var scenario = CreateTestScenario();
            aiTroop.BelongedFaction = scenario.Factions[0];

            // 添加一些敌人
            var enemy1 = CreateTestTroop("敌人1", 50, 100, 40);
            enemy1.BelongedFaction = scenario.Factions[1];
            enemy1.Position = new Point(5, 5);

            var enemy2 = CreateTestTroop("敌人2", 20, 100, 30);
            enemy2.BelongedFaction = scenario.Factions[1];
            enemy2.Position = new Point(6, 5);

            scenario.AddTroop(enemy1);
            scenario.AddTroop(enemy2);

            // 给AI添加技能
            aiTroop.AvailableSkills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateThunderStrike(),
                SkillFactory.CreateConfusion()
            };

            // 创建决策管理器
            var decisionManager = new AIDecisionManager();
            var decision = decisionManager.MakeCombatDecision(aiTroop, scenario);

            Console.WriteLine($"  AI选择的行动: {decision.Action}");
            if (decision.Skill != null)
            {
                Console.WriteLine($"  选择的技能: {decision.Skill.Name}");
                Console.WriteLine($"  目标: {decision.Target?.Name ?? "无"}");
            }
            Console.WriteLine($"  决策评分: {decision.Score:F1}");
            Console.WriteLine($"  预期: 选择攻击残血敌人");
            Console.WriteLine($"  结果: {(decision.Action == AIActionType.UseSkill ? "通过" : "需要检查")}\n");
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
                CurrentPrestige = 50,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 1,
                AvailableSkills = new List<Skill>()
            };
        }

        private static GameScenario CreateTestScenario()
        {
            var scenario = new GameScenario
            {
                CurrentTurn = 1,
                Factions = new List<Faction>
                {
                    new Faction { ID = 1, Name = "势力1" },
                    new Faction { ID = 2, Name = "势力2" }
                },
                Troops = new List<Troop>()
            };

            return scenario;
        }

        /// <summary>
        /// 性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("\n=== 性能测试 ===");

            var attacker = CreateTestTroop("攻击者", 100, 100, 50);
            var target = CreateTestTroop("目标", 100, 100, 40);
            var scenario = CreateTestScenario();
            var skill = SkillFactory.CreateFireAttack();

            attacker.BelongedFaction = scenario.Factions[0];
            target.BelongedFaction = scenario.Factions[1];

            int iterations = 10000;
            var startTime = DateTime.Now;

            for (int i = 0; i < iterations; i++)
            {
                CombatEvaluator.EvaluateSkill(attacker, skill, target, scenario);
            }

            var elapsed = DateTime.Now - startTime;
            double avgTime = elapsed.TotalMilliseconds / iterations;

            Console.WriteLine($"  执行 {iterations} 次评估");
            Console.WriteLine($"  总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均耗时: {avgTime:F4} ms");
            Console.WriteLine($"  预期: < 0.1 ms/次");
            Console.WriteLine($"  结果: {(avgTime < 0.1 ? "通过" : "需要优化")}\n");
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public static void RunStressTest()
        {
            Console.WriteLine("\n=== 压力测试 ===");

            var scenario = CreateTestScenario();
            var decisionManager = new AIDecisionManager();

            // 创建大量AI单位
            int troopCount = 50;
            var troops = new List<Troop>();

            for (int i = 0; i < troopCount; i++)
            {
                var troop = CreateTestTroop($"部队{i}", 100, 100, 50);
                troop.BelongedFaction = scenario.Factions[i % 2];
                troop.Position = new Point(i % 10, i / 10);
                troop.AvailableSkills = new List<Skill>
                {
                    SkillFactory.CreateFireAttack(),
                    SkillFactory.CreateHeal()
                };
                troops.Add(troop);
                scenario.AddTroop(troop);
            }

            var startTime = DateTime.Now;

            // 为所有单位做决策
            foreach (var troop in troops)
            {
                decisionManager.MakeCombatDecision(troop, scenario);
            }

            var elapsed = DateTime.Now - startTime;
            double avgTime = elapsed.TotalMilliseconds / troopCount;

            Console.WriteLine($"  为 {troopCount} 个单位做决策");
            Console.WriteLine($"  总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  平均耗时: {avgTime:F2} ms/单位");
            Console.WriteLine($"  预期: < 10 ms/单位");
            Console.WriteLine($"  结果: {(avgTime < 10 ? "通过" : "需要优化")}\n");

            // 测试缓存效果
            startTime = DateTime.Now;
            foreach (var troop in troops)
            {
                decisionManager.MakeCombatDecision(troop, scenario);
            }
            var cachedElapsed = DateTime.Now - startTime;

            Console.WriteLine($"  缓存后再次决策耗时: {cachedElapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  加速比: {elapsed.TotalMilliseconds / cachedElapsed.TotalMilliseconds:F2}x");
            Console.WriteLine($"  预期: > 2x 加速");
            Console.WriteLine($"  结果: {(elapsed > cachedElapsed * 2 ? "通过" : "缓存效果不明显")}\n");
        }
    }

    // === 测试用的简化类定义 ===

    public class Troop
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int CurrentPrestige { get; set; }
        public int MaxPrestige { get; set; }
        public Point Position { get; set; }
        public int AttackRange { get; set; }
        public Faction BelongedFaction { get; set; }
        public List<Skill> AvailableSkills { get; set; }
        public int PersonId { get; set; }

        public float HpRatio => (float)CurrentHP / MaxHP;
        public bool IsHero => PersonId > 0;
        public bool IsCasting => false;
        
        public bool IsFriend(Troop other) => BelongedFaction == other.BelongedFaction;
        public bool IsEnemy(Troop other) => !IsFriend(other);
        public bool HasStatus(StatusKind status) => false;
        public bool HasTech(string techName) => false;
    }

    public class Faction
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class GameScenario
    {
        public int CurrentTurn { get; set; }
        public List<Faction> Factions { get; set; }
        public List<Troop> Troops { get; set; }

        public GameScenario()
        {
            Troops = new List<Troop>();
            Factions = new List<Faction>();
        }

        public void AddTroop(Troop troop)
        {
            if (!Troops.Contains(troop))
                Troops.Add(troop);
        }

        public bool IsTargetValid(Skill skill, Troop source, Troop target)
        {
            return true; // 简化实现
        }

        public List<Troop> GetTroopsInRadius(Point position, int radius)
        {
            return Troops.FindAll(t => 
                Math.Abs(t.Position.X - position.X) <= radius &&
                Math.Abs(t.Position.Y - position.Y) <= radius);
        }

        public TerrainKind GetTerrainKind(Point position)
        {
            return TerrainKind.Plain; // 简化实现
        }

        public Troop GetNearestEnemy(Troop troop)
        {
            Troop nearest = null;
            int minDistance = int.MaxValue;

            foreach (var other in Troops)
            {
                if (troop.IsEnemy(other))
                {
                    int distance = Math.Abs(troop.Position.X - other.Position.X) +
                                 Math.Abs(troop.Position.Y - other.Position.Y);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = other;
                    }
                }
            }

            return nearest;
        }

        public List<Troop> GetEnemyTroops(Faction faction)
        {
            return Troops.FindAll(t => t.BelongedFaction != faction);
        }

        public List<Troop> GetFriendlyTroops(Faction faction)
        {
            return Troops.FindAll(t => t.BelongedFaction == faction);
        }
    }

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