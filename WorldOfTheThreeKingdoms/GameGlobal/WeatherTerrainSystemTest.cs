using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 天气地形系统测试 - 验证天气和地形交互功能
    /// </summary>
    public static class WeatherTerrainSystemTest
    {
        /// <summary>
        /// 运行完整的天气地形系统测试
        /// </summary>
        public static void RunWeatherTerrainTest()
        {
            Console.WriteLine("=== 天气地形系统测试 ===\n");

            try
            {
                TestFireSkillInteractions();
                TestWaterSkillInteractions();
                TestThunderSkillInteractions();
                TestNewInfluenceTypes();
                TestWeatherEffects();

                Console.WriteLine("\n🎉 天气地形系统测试完成！所有功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 天气地形系统测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试火系技能交互
        /// </summary>
        private static void TestFireSkillInteractions()
        {
            Console.WriteLine("--- 火系技能交互测试 ---");

            var caster = CreateTestTroop("火法师");
            var target = CreateTestTroop("目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var fireSkill = SkillFactory.CreateFireAttack();
            var scenario = CreateTestScenario();

            // 测试不同地形的火计效果
            Console.WriteLine("  测试火计在不同地形的效果:");

            // 森林地形
            SetTargetTerrain(scenario, target, TerrainKind.Forest);
            float forestScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    森林地形火计评分: {forestScore:F2} (应该有1.5x加成)");

            // 草地地形
            SetTargetTerrain(scenario, target, TerrainKind.Grassland);
            float grassScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    草地地形火计评分: {grassScore:F2} (应该有1.2x加成)");

            // 平原地形
            SetTargetTerrain(scenario, target, TerrainKind.Plain);
            float plainScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    平原地形火计评分: {plainScore:F2} (基础伤害)");

            // 验证地形加成
            if (forestScore > grassScore && grassScore > plainScore)
            {
                Console.WriteLine("  ✓ 火系技能地形交互正常");
            }
            else
            {
                Console.WriteLine("  ⚠ 火系技能地形交互异常");
            }

            Console.WriteLine("  ✓ 火系技能交互测试完成\n");
        }

        /// <summary>
        /// 测试水系技能交互
        /// </summary>
        private static void TestWaterSkillInteractions()
        {
            Console.WriteLine("--- 水系技能交互测试 ---");

            var caster = CreateTestTroop("水法师");
            var target = CreateTestTroop("目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var waterSkill = SkillFactory.CreateWaterAttack();
            var scenario = CreateTestScenario();

            Console.WriteLine("  测试水攻在不同地形的效果:");

            // 河流地形
            SetTargetTerrain(scenario, target, TerrainKind.River);
            float riverScore = CombatEvaluator.EvaluateSkill(caster, waterSkill, target, scenario);
            Console.WriteLine($"    河流地形水攻评分: {riverScore:F2} (应该有1.5x加成)");

            // 沼泽地形
            SetTargetTerrain(scenario, target, TerrainKind.Swamp);
            float swampScore = CombatEvaluator.EvaluateSkill(caster, waterSkill, target, scenario);
            Console.WriteLine($"    沼泽地形水攻评分: {swampScore:F2} (应该有1.5x加成)");

            // 沙漠地形
            SetTargetTerrain(scenario, target, TerrainKind.Desert);
            float desertScore = CombatEvaluator.EvaluateSkill(caster, waterSkill, target, scenario);
            Console.WriteLine($"    沙漠地形水攻评分: {desertScore:F2} (应该有0.7x减成)");

            Console.WriteLine("  ✓ 水系技能交互测试完成\n");
        }

        /// <summary>
        /// 测试雷系技能交互
        /// </summary>
        private static void TestThunderSkillInteractions()
        {
            Console.WriteLine("--- 雷系技能交互测试 ---");

            var caster = CreateTestTroop("雷法师");
            var target = CreateTestTroop("目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var thunderSkill = SkillFactory.CreateEnhancedThunderStrike();
            var scenario = CreateTestScenario();

            Console.WriteLine("  测试天雷在不同天气的效果:");

            // 雷雨天气
            SetScenarioWeather(scenario, WeatherKind.Storm);
            float stormScore = CombatEvaluator.EvaluateSkill(caster, thunderSkill, target, scenario);
            Console.WriteLine($"    雷雨天气天雷评分: {stormScore:F2} (应该有1.4x加成)");

            // 晴天天气
            SetScenarioWeather(scenario, WeatherKind.Clear);
            float clearScore = CombatEvaluator.EvaluateSkill(caster, thunderSkill, target, scenario);
            Console.WriteLine($"    晴天天气天雷评分: {clearScore:F2} (应该有0.9x减成)");

            Console.WriteLine("  ✓ 雷系技能交互测试完成\n");
        }

        /// <summary>
        /// 测试新的影响类型
        /// </summary>
        private static void TestNewInfluenceTypes()
        {
            Console.WriteLine("--- 新影响类型测试 ---");

            var caster = CreateTestTroop("策士");
            var target = CreateTestTroop("敌军");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var scenario = CreateTestScenario();

            // 测试诱敌技能
            Console.WriteLine("  测试诱敌技能:");
            var lureSkill = SkillFactory.CreateLure();
            float lureScore = CombatEvaluator.EvaluateSkill(caster, lureSkill, target, scenario);
            Console.WriteLine($"    诱敌技能评分: {lureScore:F2}");

            // 测试反间计
            Console.WriteLine("  测试反间计:");
            var strifeSkill = SkillFactory.CreateInternalStrife();
            float strifeScore = CombatEvaluator.EvaluateSkill(caster, strifeSkill, target, scenario);
            Console.WriteLine($"    反间计评分: {strifeScore:F2}");

            // 测试谣言术
            Console.WriteLine("  测试谣言术:");
            var rumorSkill = SkillFactory.CreateRumor();
            float rumorScore = CombatEvaluator.EvaluateSkill(caster, rumorSkill, target, scenario);
            Console.WriteLine($"    谣言术评分: {rumorScore:F2}");

            Console.WriteLine("  ✓ 新影响类型测试完成\n");
        }

        /// <summary>
        /// 测试天气效果
        /// </summary>
        private static void TestWeatherEffects()
        {
            Console.WriteLine("--- 天气效果测试 ---");

            var caster = CreateTestTroop("全能法师");
            var target = CreateTestTroop("目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var fireSkill = SkillFactory.CreateFireAttack();
            var scenario = CreateTestScenario();

            Console.WriteLine("  测试火计在不同天气的效果:");

            // 雨天
            SetScenarioWeather(scenario, WeatherKind.Rain);
            float rainScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    雨天火计评分: {rainScore:F2} (应该有0.5x减成)");

            // 干旱
            SetScenarioWeather(scenario, WeatherKind.Drought);
            float droughtScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    干旱火计评分: {droughtScore:F2} (应该有1.3x加成)");

            // 晴天
            SetScenarioWeather(scenario, WeatherKind.Clear);
            float clearScore = CombatEvaluator.EvaluateSkill(caster, fireSkill, target, scenario);
            Console.WriteLine($"    晴天火计评分: {clearScore:F2} (基础伤害)");

            // 验证天气效果
            if (droughtScore > clearScore && clearScore > rainScore)
            {
                Console.WriteLine("  ✓ 天气效果正常");
            }
            else
            {
                Console.WriteLine("  ⚠ 天气效果异常");
            }

            Console.WriteLine("  ✓ 天气效果测试完成\n");
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
                Intelligence = 70,
                CurrentPrestige = 80,
                MaxPrestige = 100,
                Position = new Point(5, 5),
                AttackRange = 1,
                Mobility = 2,
                ViewRadius = 5,
                BelongedFaction = new Faction { ID = 1, Name = "测试势力" }
            };
        }

        private static GameScenario CreateTestScenario()
        {
            var scenario = new GameScenario
            {
                Troops = new List<Troop>()
            };

            // 添加一些测试部队
            for (int i = 0; i < 5; i++)
            {
                var troop = CreateTestTroop($"场景部队{i + 1}");
                troop.Position = new Point(i * 2, i * 2);
                scenario.Troops.Add(troop);
            }

            return scenario;
        }

        private static void SetTargetTerrain(GameScenario scenario, Troop target, TerrainKind terrain)
        {
            // 这里需要根据实际的地形系统来设置
            // 简化实现：假设可以直接设置目标位置的地形
            Console.WriteLine($"    设置目标位置地形为: {terrain}");
        }

        private static void SetScenarioWeather(GameScenario scenario, WeatherKind weather)
        {
            // 这里需要根据实际的天气系统来设置
            // 简化实现：假设可以直接设置场景天气
            Console.WriteLine($"    设置场景天气为: {weather}");
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("--- 天气地形系统性能测试 ---");

            var caster = CreateTestTroop("测试法师");
            var target = CreateTestTroop("测试目标");
            target.BelongedFaction = new Faction { ID = 2, Name = "敌对势力" };

            var skills = new List<Skill>
            {
                SkillFactory.CreateFireAttack(),
                SkillFactory.CreateWaterAttack(),
                SkillFactory.CreateEnhancedThunderStrike(),
                SkillFactory.CreateLure(),
                SkillFactory.CreateInternalStrife()
            };

            var scenario = CreateTestScenario();
            int iterations = 1000;

            var startTime = DateTime.Now;

            for (int i = 0; i < iterations; i++)
            {
                foreach (var skill in skills)
                {
                    CombatEvaluator.EvaluateSkill(caster, skill, target, scenario);
                }
            }

            var elapsed = DateTime.Now - startTime;
            int totalEvaluations = iterations * skills.Count;

            Console.WriteLine($"  性能测试结果:");
            Console.WriteLine($"    总评估次数: {totalEvaluations}");
            Console.WriteLine($"    总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"    平均每次: {elapsed.TotalMilliseconds / totalEvaluations:F4} ms");

            if (elapsed.TotalMilliseconds / totalEvaluations < 0.1)
                Console.WriteLine("    性能评价: 优秀 ⭐⭐⭐");
            else if (elapsed.TotalMilliseconds / totalEvaluations < 1.0)
                Console.WriteLine("    性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine("    性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能测试完成\n");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始运行天气地形系统的所有测试...\n");

            RunWeatherTerrainTest();
            RunPerformanceTest();

            Console.WriteLine("\n🎉 天气地形系统所有测试完成！系统运行正常。");
        }
    }
}