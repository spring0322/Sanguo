using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT兼容性测试
    /// 验证AOT兼容性适配器的功能是否正常工作
    /// </summary>
    public static class AOTCompatibilityTest
    {
        /// <summary>
        /// 运行完整的AOT兼容性测试
        /// </summary>
        /// <returns>测试是否通过</returns>
        public static async Task<bool> RunCompatibilityTests()
        {
            Debug.WriteLine("=== 开始AOT兼容性测试 ===");
            
            bool allTestsPassed = true;
            
            try
            {
                // 测试1: 适配器基础功能
                allTestsPassed &= await TestAdapterBasicFunctionality();
                
                // 测试2: 对象工厂功能
                allTestsPassed &= await TestObjectFactory();
                
                // 测试3: 对象初始化器功能
                allTestsPassed &= await TestObjectInitializer();
                
                // 测试4: 类型处理器功能
                allTestsPassed &= await TestTypeHandlers();
                
                // 测试5: 属性访问功能
                allTestsPassed &= await TestPropertyAccess();
                
                // 测试6: 性能测试
                allTestsPassed &= await TestPerformance();
                
                Debug.WriteLine($"=== AOT兼容性测试完成: {(allTestsPassed ? "全部通过" : "部分失败")} ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AOT兼容性测试异常: {ex.Message}");
                allTestsPassed = false;
            }
            
            return allTestsPassed;
        }

        /// <summary>
        /// 测试适配器基础功能
        /// </summary>
        private static async Task<bool> TestAdapterBasicFunctionality()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试适配器基础功能");
            
            try
            {
                // 模拟异步初始化过程
                await Task.Delay(10).ConfigureAwait(false);
                
                var adapter = new AOTCompatibilityAdapter();
                
                // 测试类型兼容性检查
                var factionCompatible = adapter.IsAOTCompatible(typeof(Faction));
                var personCompatible = adapter.IsAOTCompatible(typeof(Person));
                var stringCompatible = adapter.IsAOTCompatible(typeof(string));
                
                if (!factionCompatible || !personCompatible || !stringCompatible)
                {
                    Debug.WriteLine("  ❌ 类型兼容性检查失败");
                    return false;
                }
                
                // 测试处理器注册
                var handlers = adapter.GetRegisteredHandlers();
                if (handlers.Count == 0)
                {
                    Debug.WriteLine("  ❌ 没有注册任何类型处理器");
                    return false;
                }
                
                Debug.WriteLine($"  ✅ 适配器基础功能测试通过，已注册 {handlers.Count} 个处理器");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 适配器基础功能测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试对象工厂功能
        /// </summary>
        private static async Task<bool> TestObjectFactory()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试对象工厂功能");
            
            try
            {
                // 模拟异步工厂初始化
                await Task.Delay(10).ConfigureAwait(false);
                
                // 测试按类型名创建
                var faction = AOTObjectFactory.CreateByTypeName("Faction");
                var person = AOTObjectFactory.CreateByTypeName("Person");
                var architecture = AOTObjectFactory.CreateByTypeName("Architecture");
                
                if (faction == null || person == null || architecture == null)
                {
                    Debug.WriteLine("  ❌ 按类型名创建对象失败");
                    return false;
                }
                
                // 模拟异步对象创建过程
                await Task.Delay(5).ConfigureAwait(false);
                
                // 测试按类型创建
                var troop = AOTObjectFactory.CreateByType(typeof(Troop));
                var legion = AOTObjectFactory.CreateByType(typeof(Legion));
                
                if (troop == null || legion == null)
                {
                    Debug.WriteLine("  ❌ 按类型创建对象失败");
                    return false;
                }
                
                // 测试泛型创建
                var genericFaction = AOTObjectFactory.Create<Faction>();
                if (genericFaction == null)
                {
                    Debug.WriteLine("  ❌ 泛型创建对象失败");
                    return false;
                }
                
                // 测试批量创建
                var batchObjects = AOTObjectFactory.CreateBatch("Person", 3);
                if (batchObjects.Count != 3)
                {
                    Debug.WriteLine("  ❌ 批量创建对象失败");
                    return false;
                }
                
                // 测试异步创建
                var asyncObject = await AOTObjectFactory.CreateAsync("Troop");
                if (asyncObject == null)
                {
                    Debug.WriteLine("  ❌ 异步创建对象失败");
                    return false;
                }
                
                Debug.WriteLine("  ✅ 对象工厂功能测试通过");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 对象工厂功能测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试对象初始化器功能
        /// </summary>
        private static async Task<bool> TestObjectInitializer()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试对象初始化器功能");
            
            try
            {
                // 模拟异步初始化器准备
                await Task.Delay(10).ConfigureAwait(false);
                
                // 创建未初始化的对象
                var faction = new Faction();
                var person = new Person();
                var architecture = new Architecture();
                
                // 记录初始化前的状态
                var factionNameBefore = faction.Name;
                var personNameBefore = person.Name;
                var architectureNameBefore = architecture.Name;
                
                // 模拟异步初始化过程
                await Task.Delay(5).ConfigureAwait(false);
                
                // 执行初始化
                AOTObjectInitializer.Initialize(faction);
                AOTObjectInitializer.Initialize(person);
                AOTObjectInitializer.Initialize(architecture);
                
                // 检查初始化结果
                if (string.IsNullOrEmpty(faction.Name) || faction.Name == factionNameBefore)
                {
                    Debug.WriteLine("  ❌ Faction初始化失败");
                    return false;
                }
                
                if (string.IsNullOrEmpty(person.Name) || person.Name == personNameBefore)
                {
                    Debug.WriteLine("  ❌ Person初始化失败");
                    return false;
                }
                
                if (string.IsNullOrEmpty(architecture.Name) || architecture.Name == architectureNameBefore)
                {
                    Debug.WriteLine("  ❌ Architecture初始化失败");
                    return false;
                }
                
                // 测试批量初始化
                var objects = new List<GameObject> { new Troop(), new Legion() };
                AOTObjectInitializer.InitializeBatch(objects);
                
                if (objects.Any(obj => string.IsNullOrEmpty(obj.Name)))
                {
                    Debug.WriteLine("  ❌ 批量初始化失败");
                    return false;
                }
                
                // 测试异步初始化
                var asyncObject = new Faction();
                await AOTObjectInitializer.InitializeAsync(asyncObject);
                
                if (string.IsNullOrEmpty(asyncObject.Name))
                {
                    Debug.WriteLine("  ❌ 异步初始化失败");
                    return false;
                }
                
                Debug.WriteLine("  ✅ 对象初始化器功能测试通过");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 对象初始化器功能测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试类型处理器功能
        /// </summary>
        private static async Task<bool> TestTypeHandlers()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试类型处理器功能");
            
            try
            {
                // 模拟异步处理器初始化
                await Task.Delay(10).ConfigureAwait(false);
                
                var adapter = new AOTCompatibilityAdapter();
                var handlers = adapter.GetRegisteredHandlers();
                
                // 测试每个处理器
                foreach (var kvp in handlers)
                {
                    var type = kvp.Key;
                    var handler = kvp.Value;
                    
                    // 模拟异步实例创建过程
                    await Task.Delay(1).ConfigureAwait(false);
                    
                    // 测试创建实例
                    var instance = handler.CreateInstance();
                    if (instance == null)
                    {
                        Debug.WriteLine($"    ❌ {type.Name} 处理器创建实例失败");
                        return false;
                    }
                    
                    // 测试初始化
                    handler.InitializeObject(instance);
                    
                    // 测试属性获取
                    var properties = handler.GetProperties();
                    if (properties.Count == 0)
                    {
                        Debug.WriteLine($"    ❌ {type.Name} 处理器获取属性失败");
                        return false;
                    }
                    
                    // 测试属性访问
                    var idProperty = properties.FirstOrDefault(p => p.Name == "ID");
                    if (idProperty != null)
                    {
                        var originalId = handler.GetProperty(instance, "ID");
                        var newId = 12345;
                        
                        if (handler.SetProperty(instance, "ID", newId))
                        {
                            var retrievedId = handler.GetProperty(instance, "ID");
                            if (!newId.Equals(retrievedId))
                            {
                                Debug.WriteLine($"    ❌ {type.Name} 处理器属性设置/获取不一致");
                                return false;
                            }
                        }
                    }
                    
                    Debug.WriteLine($"    ✅ {type.Name} 处理器测试通过");
                }
                
                Debug.WriteLine("  ✅ 类型处理器功能测试通过");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 类型处理器功能测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试属性访问功能
        /// </summary>
        private static async Task<bool> TestPropertyAccess()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试属性访问功能");
            
            try
            {
                // 模拟异步属性访问器初始化
                await Task.Delay(10).ConfigureAwait(false);
                
                var adapter = new AOTCompatibilityAdapter();
                var faction = new Faction { ID = 1001, Name = "测试势力" };
                
                // 模拟异步属性读取过程
                await Task.Delay(5).ConfigureAwait(false);
                
                // 测试属性获取
                var id = adapter.GetPropertyValue(faction, "ID");
                var name = adapter.GetPropertyValue(faction, "Name");
                
                if (!1001.Equals(id) || !"测试势力".Equals(name))
                {
                    Debug.WriteLine("  ❌ 属性获取失败");
                    return false;
                }
                
                // 测试属性设置
                var setIdResult = adapter.SetPropertyValue(faction, "ID", 2002);
                var setNameResult = adapter.SetPropertyValue(faction, "Name", "新势力");
                
                if (!setIdResult || !setNameResult)
                {
                    Debug.WriteLine("  ❌ 属性设置失败");
                    return false;
                }
                
                // 验证设置结果
                if (faction.ID != 2002 || faction.Name != "新势力")
                {
                    Debug.WriteLine("  ❌ 属性设置验证失败");
                    return false;
                }
                
                // 测试类型属性信息
                var properties = adapter.GetTypeProperties(typeof(Faction));
                if (properties.Count == 0)
                {
                    Debug.WriteLine("  ❌ 获取类型属性信息失败");
                    return false;
                }
                
                Debug.WriteLine("  ✅ 属性访问功能测试通过");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 属性访问功能测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试性能
        /// </summary>
        private static async Task<bool> TestPerformance()
        {
            Debug.WriteLine("[AOTCompatibilityTest] 测试性能");
            
            try
            {
                // 模拟异步性能测试准备
                await Task.Delay(10).ConfigureAwait(false);
                
                const int iterations = 1000;
                
                // 测试对象创建性能
                var createStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    // 模拟异步创建过程
                    if (i % 100 == 0)
                    {
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    var obj = AOTObjectFactory.CreateByTypeName("Faction");
                    if (obj == null) return false;
                }
                createStopwatch.Stop();
                
                // 测试属性访问性能
                var faction = AOTObjectFactory.Create<Faction>();
                var adapter = new AOTCompatibilityAdapter();
                
                var propertyStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    adapter.SetPropertyValue(faction, "ID", i);
                    var id = adapter.GetPropertyValue(faction, "ID");
                }
                propertyStopwatch.Stop();
                
                Debug.WriteLine($"    对象创建: {createStopwatch.ElapsedMilliseconds}ms ({iterations} 次)");
                Debug.WriteLine($"    属性访问: {propertyStopwatch.ElapsedMilliseconds}ms ({iterations * 2} 次)");
                
                // 性能阈值检查（可根据需要调整）
                if (createStopwatch.ElapsedMilliseconds > 5000 || propertyStopwatch.ElapsedMilliseconds > 1000)
                {
                    Debug.WriteLine("  ⚠️ 性能可能需要优化");
                }
                
                Debug.WriteLine("  ✅ 性能测试完成");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 性能测试异常: {ex.Message}");
                return false;
            }
        }
    }
}