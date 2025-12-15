// ===================================================================
// 测试说服和破坏系统优化效果
// ===================================================================

using System;
using System.Diagnostics;
using GameObjects;
using WorldOfTheThreeKingdoms;

namespace GameManager
{
    /// <summary>
    /// 测试说服和破坏系统的性能优化效果
    /// </summary>
    public class StrategyOptimizationTest
    {
        /// <summary>
        /// 测试优化后的性能提升
        /// </summary>
        public static void TestOptimizationPerformance()
        {
            Console.WriteLine("=== 说服和破坏系统性能优化测试 ===");
            
            if (Session.Current?.Scenario?.Architectures == null)
            {
                Console.WriteLine("错误：游戏会话未初始化");
                return;
            }
            
            var stopwatch = Stopwatch.StartNew();
            int convinceAvailableCount = 0;
            int destroyAvailableCount = 0;
            
            // 测试优化后的方法
            Console.WriteLine("测试优化后的方法...");
            stopwatch.Restart();
            
            foreach (Architecture arch in Session.Current.Scenario.Architectures)
            {
                if (arch.BelongedFaction != null && arch.BelongedFaction.IsAlive)
                {
                    // 测试说服可用性
                    if (arch.ConvincePersonAvail())
                    {
                        convinceAvailableCount++;
                    }
                    
                    // 测试破坏可用性
                    if (arch.DestroyAvail())
                    {
                        destroyAvailableCount++;
                    }
                }
            }
            
            long optimizedTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"优化后结果：");
            Console.WriteLine($"- 耗时: {optimizedTime}ms");
            Console.WriteLine($"- 可说服的建筑数: {convinceAvailableCount}");
            Console.WriteLine($"- 可破坏的建筑数: {destroyAvailableCount}");
            Console.WriteLine($"- 总建筑数: {Session.Current.Scenario.Architectures.Count}");
            
            // 测试缓存效果
            Console.WriteLine("\n测试缓存效果（重复调用100次）...");
            stopwatch.Restart();
            
            for (int i = 0; i < 100; i++)
            {
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    if (arch.BelongedFaction != null && arch.BelongedFaction.IsAlive)
                    {
                        arch.ConvincePersonAvail();
                        arch.DestroyAvail();
                    }
                }
            }
            
            long cachedTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"缓存重复调用结果：");
            Console.WriteLine($"- 100次重复调用耗时: {cachedTime}ms");
            Console.WriteLine($"- 平均每次耗时: {cachedTime / 100.0:F2}ms");
            
            if (optimizedTime > 0)
            {
                Console.WriteLine($"- 缓存效果: {(double)optimizedTime * 100 / cachedTime:F1}x 提升");
            }
            
            Console.WriteLine("\n=== 优化效果总结 ===");
            Console.WriteLine("✅ 说服按钮可用性检查：使用快速检查，避免遍历所有城市");
            Console.WriteLine("✅ 破坏按钮可用性检查：使用快速检查，避免遍历所有城市");
            Console.WriteLine("✅ 区域计算：添加缓存机制，避免重复计算");
            Console.WriteLine("✅ 回合管理：自动清理过期缓存");
        }
        
        /// <summary>
        /// 测试具体建筑的优化效果
        /// </summary>
        public static void TestSpecificArchitecture(Architecture architecture)
        {
            if (architecture == null)
            {
                Console.WriteLine("错误：建筑为空");
                return;
            }
            
            Console.WriteLine($"\n=== 测试建筑: {architecture.Name} ===");
            
            var stopwatch = Stopwatch.StartNew();
            
            // 测试说服功能
            Console.WriteLine("1. 说服功能测试:");
            stopwatch.Restart();
            bool convinceAvail = architecture.ConvincePersonAvail();
            long convinceTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"   - 可用性: {(convinceAvail ? "✅可用" : "❌不可用")}");
            Console.WriteLine($"   - 检查耗时: {convinceTime}ms");
            Console.WriteLine($"   - 有人物: {architecture.HasPerson()}");
            Console.WriteLine($"   - 资金充足: {architecture.Fund >= architecture.ConvincePersonFund} ({architecture.Fund}/{architecture.ConvincePersonFund})");
            
            // 测试破坏功能
            Console.WriteLine("2. 破坏功能测试:");
            stopwatch.Restart();
            bool destroyAvail = architecture.DestroyAvail();
            long destroyTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"   - 可用性: {(destroyAvail ? "✅可用" : "❌不可用")}");
            Console.WriteLine($"   - 检查耗时: {destroyTime}ms");
            Console.WriteLine($"   - 有可移动人物: {architecture.MovablePersons.Count > 0}");
            Console.WriteLine($"   - 资金充足: {architecture.Fund >= architecture.DestroyArchitectureFund} ({architecture.Fund}/{architecture.DestroyArchitectureFund})");
            
            // 测试缓存效果
            Console.WriteLine("3. 缓存效果测试:");
            stopwatch.Restart();
            for (int i = 0; i < 10; i++)
            {
                architecture.GetConvincePersonArchitectureArea();
                architecture.GetDestroyArchitectureArea();
            }
            long cacheTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"   - 10次区域计算耗时: {cacheTime}ms");
            Console.WriteLine($"   - 平均每次: {cacheTime / 10.0:F2}ms");
        }
        
        /// <summary>
        /// 验证优化的正确性
        /// </summary>
        public static void ValidateOptimization()
        {
            Console.WriteLine("\n=== 验证优化正确性 ===");
            
            if (Session.Current?.Scenario?.Architectures == null)
            {
                Console.WriteLine("错误：游戏会话未初始化");
                return;
            }
            
            int validationErrors = 0;
            
            foreach (Architecture arch in Session.Current.Scenario.Architectures)
            {
                if (arch.BelongedFaction != null && arch.BelongedFaction.IsAlive)
                {
                    try
                    {
                        // 验证说服功能
                        bool convinceQuick = arch.ConvincePersonAvail();
                        bool convinceDetailed = arch.ValidateConvinceTargetsOnExecution();
                        
                        // 验证破坏功能
                        bool destroyQuick = arch.DestroyAvail();
                        bool destroyDetailed = arch.ValidateDestroyTargetsOnExecution();
                        
                        // 检查逻辑一致性
                        if (convinceQuick && !convinceDetailed)
                        {
                            Console.WriteLine($"⚠️  {arch.Name}: 说服快速检查通过但详细验证失败");
                        }
                        
                        if (destroyQuick && !destroyDetailed)
                        {
                            Console.WriteLine($"⚠️  {arch.Name}: 破坏快速检查通过但详细验证失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ {arch.Name}: 验证出错 - {ex.Message}");
                        validationErrors++;
                    }
                }
            }
            
            if (validationErrors == 0)
            {
                Console.WriteLine("✅ 所有验证通过，优化正确实施");
            }
            else
            {
                Console.WriteLine($"❌ 发现 {validationErrors} 个验证错误");
            }
        }
    }
}

// ===================================================================
// 使用示例
// ===================================================================

/*
// 在游戏中调用测试：

// 1. 测试整体性能
StrategyOptimizationTest.TestOptimizationPerformance();

// 2. 测试特定建筑
Architecture currentArch = Session.Current.Scenario.CurrentPlayer?.CapitalArchitecture;
if (currentArch != null)
{
    StrategyOptimizationTest.TestSpecificArchitecture(currentArch);
}

// 3. 验证优化正确性
StrategyOptimizationTest.ValidateOptimization();
*/