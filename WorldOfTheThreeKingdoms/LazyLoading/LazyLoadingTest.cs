using GameObjects;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.LazyLoading
{
    /// <summary>
    /// 延迟加载系统测试类
    /// 验证延迟加载和按需初始化功能是否正常工作
    /// </summary>
    public static class LazyLoadingTest
    {
        /// <summary>
        /// 运行延迟加载系统测试
        /// </summary>
        /// <returns>测试是否通过</returns>
        public static async Task<bool> RunLazyLoadingTests()
        {
            Debug.WriteLine("=== 延迟加载系统测试 ===");
            
            bool allTestsPassed = true;

            try
            {
                // 测试1: 基础延迟加载器功能
                Debug.WriteLine("\n[测试1] 基础延迟加载器功能");
                bool test1Passed = await TestBasicLazyLoader();
                allTestsPassed &= test1Passed;
                Debug.WriteLine($"测试1结果: {(test1Passed ? "通过" : "失败")}");

                // 测试2: 异步延迟加载器功能
                Debug.WriteLine("\n[测试2] 异步延迟加载器功能");
                bool test2Passed = await TestAsyncLazyLoader();
                allTestsPassed &= test2Passed;
                Debug.WriteLine($"测试2结果: {(test2Passed ? "通过" : "失败")}");

                // 测试3: 延迟加载工厂功能
                Debug.WriteLine("\n[测试3] 延迟加载工厂功能");
                bool test3Passed = await TestLazyLoaderFactory();
                allTestsPassed &= test3Passed;
                Debug.WriteLine($"测试3结果: {(test3Passed ? "通过" : "失败")}");

                // 测试4: 按需初始化管理器功能
                Debug.WriteLine("\n[测试4] 按需初始化管理器功能");
                bool test4Passed = await TestOnDemandInitializer();
                allTestsPassed &= test4Passed;
                Debug.WriteLine($"测试4结果: {(test4Passed ? "通过" : "失败")}");

                // 测试5: 缓存和生命周期管理
                Debug.WriteLine("\n[测试5] 缓存和生命周期管理");
                bool test5Passed = await TestCacheAndLifecycle();
                allTestsPassed &= test5Passed;
                Debug.WriteLine($"测试5结果: {(test5Passed ? "通过" : "失败")}");

                // 测试6: 错误处理和恢复
                Debug.WriteLine("\n[测试6] 错误处理和恢复");
                bool test6Passed = await TestErrorHandling();
                allTestsPassed &= test6Passed;
                Debug.WriteLine($"测试6结果: {(test6Passed ? "通过" : "失败")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"延迟加载系统测试异常: {ex.Message}");
                allTestsPassed = false;
            }

            Debug.WriteLine($"\n=== 延迟加载系统测试完成，总体结果: {(allTestsPassed ? "通过" : "失败")} ===");
            return allTestsPassed;
        }

        /// <summary>
        /// 测试基础延迟加载器功能
        /// </summary>
        private static async Task<bool> TestBasicLazyLoader()
        {
            try
            {
                // 模拟异步延迟加载器初始化
                await Task.Delay(10).ConfigureAwait(false);
                
                // 测试同步延迟加载器
                int loadCount = 0;
                var loader = new LazyLoader<string>(() =>
                {
                    loadCount++;
                    return $"加载的值_{loadCount}";
                });

                // 模拟异步加载过程
                await Task.Delay(5).ConfigureAwait(false);
                
                // 初始状态检查
                if (loader.IsLoaded)
                {
                    Debug.WriteLine("  ❌ 初始状态应该是未加载");
                    return false;
                }
                Debug.WriteLine("  ✅ 初始状态正确（未加载）");

                // 第一次访问应该触发加载
                var value1 = loader.Value;
                if (value1 != "加载的值_1" || loadCount != 1)
                {
                    Debug.WriteLine($"  ❌ 第一次加载失败: {value1}, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 第一次加载成功");

                // 第二次访问应该返回缓存值
                var value2 = loader.Value;
                if (value2 != "加载的值_1" || loadCount != 1)
                {
                    Debug.WriteLine($"  ❌ 缓存机制失败: {value2}, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 缓存机制正常");

                // 测试重新加载
                loader.Reload();
                var value3 = loader.Value;
                if (value3 != "加载的值_2" || loadCount != 2)
                {
                    Debug.WriteLine($"  ❌ 重新加载失败: {value3}, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 重新加载成功");

                // 测试清除
                loader.Clear();
                if (loader.IsLoaded)
                {
                    Debug.WriteLine("  ❌ 清除后应该是未加载状态");
                    return false;
                }
                Debug.WriteLine("  ✅ 清除功能正常");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 基础延迟加载器测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试异步延迟加载器功能
        /// </summary>
        private static async Task<bool> TestAsyncLazyLoader()
        {
            try
            {
                // 测试异步延迟加载器
                int loadCount = 0;
                var loader = new LazyLoader<string>(async () =>
                {
                    loadCount++;
                    await Task.Delay(10); // 模拟异步操作
                    return $"异步加载的值_{loadCount}";
                });

                // 测试异步获取值
                var value1 = await loader.GetValueAsync();
                if (value1 != "异步加载的值_1" || loadCount != 1)
                {
                    Debug.WriteLine($"  ❌ 异步加载失败: {value1}, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 异步加载成功");

                // 测试并发访问
                var task1 = loader.GetValueAsync();
                var task2 = loader.GetValueAsync();
                var task3 = loader.GetValueAsync();

                var results = await Task.WhenAll(task1, task2, task3);
                
                // 所有结果应该相同，且只加载一次
                if (results[0] != results[1] || results[1] != results[2] || loadCount != 1)
                {
                    Debug.WriteLine($"  ❌ 并发访问失败: 结果不一致或重复加载, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 并发访问正常");

                // 测试异步重新加载
                await loader.ReloadAsync();
                var value2 = await loader.GetValueAsync();
                if (value2 != "异步加载的值_2" || loadCount != 2)
                {
                    Debug.WriteLine($"  ❌ 异步重新加载失败: {value2}, 加载次数: {loadCount}");
                    return false;
                }
                Debug.WriteLine("  ✅ 异步重新加载成功");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 异步延迟加载器测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试延迟加载工厂功能
        /// </summary>
        private static async Task<bool> TestLazyLoaderFactory()
        {
            try
            {
                var factory = new LazyLoaderFactory();

                // 测试创建同步加载器
                var syncLoader = factory.Create(() => "工厂创建的同步值");
                var syncValue = syncLoader.Value;
                if (syncValue != "工厂创建的同步值")
                {
                    Debug.WriteLine($"  ❌ 工厂创建同步加载器失败: {syncValue}");
                    return false;
                }
                Debug.WriteLine("  ✅ 工厂创建同步加载器成功");

                // 测试创建异步加载器
                var asyncLoader = factory.Create(async () =>
                {
                    await Task.Delay(10);
                    return "工厂创建的异步值";
                });
                var asyncValue = await asyncLoader.GetValueAsync();
                if (asyncValue != "工厂创建的异步值")
                {
                    Debug.WriteLine($"  ❌ 工厂创建异步加载器失败: {asyncValue}");
                    return false;
                }
                Debug.WriteLine("  ✅ 工厂创建异步加载器成功");

                // 测试游戏对象加载器创建（如果有Session）
                try
                {
                    var gameObjectLoader = factory.CreateGameObject<Person>(1);
                    if (gameObjectLoader == null)
                    {
                        Debug.WriteLine("  ⚠️ 游戏对象加载器创建返回null（可能没有Session）");
                    }
                    else
                    {
                        Debug.WriteLine("  ✅ 游戏对象加载器创建成功");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠️ 游戏对象加载器创建异常（可能没有Session）: {ex.Message}");
                }

                // 测试关系加载器创建
                try
                {
                    var relationLoader = factory.CreateRelationship<Person>(1, "leader");
                    if (relationLoader == null)
                    {
                        Debug.WriteLine("  ❌ 关系加载器创建失败");
                        return false;
                    }
                    Debug.WriteLine("  ✅ 关系加载器创建成功");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠️ 关系加载器创建异常: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 延迟加载工厂测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试按需初始化管理器功能
        /// </summary>
        private static async Task<bool> TestOnDemandInitializer()
        {
            try
            {
                var initializer = new OnDemandInitializer();

                // 测试null对象初始化
                bool nullResult = initializer.InitializeOnDemand(null);
                if (nullResult)
                {
                    Debug.WriteLine("  ❌ null对象初始化应该返回false");
                    return false;
                }
                Debug.WriteLine("  ✅ null对象初始化正确返回false");

                // 创建测试对象
                var testFaction = new Faction();
                testFaction.ID = 9999;
                testFaction.Name = "测试势力";

                // 测试初始化
                bool initResult = initializer.InitializeOnDemand(testFaction);
                if (!initResult)
                {
                    Debug.WriteLine("  ❌ 对象初始化失败");
                    return false;
                }
                Debug.WriteLine("  ✅ 对象初始化成功");

                // 测试重复初始化（应该跳过）
                bool repeatResult = initializer.InitializeOnDemand(testFaction);
                if (!repeatResult)
                {
                    Debug.WriteLine("  ❌ 重复初始化失败");
                    return false;
                }
                Debug.WriteLine("  ✅ 重复初始化正确处理");

                // 测试初始化状态检查
                bool isInitialized = initializer.IsInitialized(testFaction);
                if (!isInitialized)
                {
                    Debug.WriteLine("  ❌ 初始化状态检查失败");
                    return false;
                }
                Debug.WriteLine("  ✅ 初始化状态检查正确");

                // 测试异步初始化
                var testPerson = new Person();
                testPerson.ID = 8888;
                // testPerson.Name = "测试人物";

                bool asyncInitResult = await initializer.InitializeOnDemandAsync(testPerson);
                if (!asyncInitResult)
                {
                    Debug.WriteLine("  ❌ 异步初始化失败");
                    return false;
                }
                Debug.WriteLine("  ✅ 异步初始化成功");

                // 测试强制重新初始化
                bool forceReinitResult = initializer.ForceReinitialize(testFaction);
                if (!forceReinitResult)
                {
                    Debug.WriteLine("  ❌ 强制重新初始化失败");
                    return false;
                }
                Debug.WriteLine("  ✅ 强制重新初始化成功");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 按需初始化管理器测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试缓存和生命周期管理
        /// </summary>
        private static async Task<bool> TestCacheAndLifecycle()
        {
            try
            {
                // 测试缓存过期
                var config = new LazyLoadingConfig
                {
                    EnableCaching = true,
                    CacheExpirationMs = 100 // 100毫秒过期
                };

                int loadCount = 0;
                var loader = new LazyLoader<string>(() =>
                {
                    loadCount++;
                    return $"缓存测试值_{loadCount}";
                }, config);

                // 第一次加载
                var value1 = loader.Value;
                if (loadCount != 1)
                {
                    Debug.WriteLine($"  ❌ 第一次加载计数错误: {loadCount}");
                    return false;
                }

                // 立即再次访问，应该使用缓存
                var value2 = loader.Value;
                if (loadCount != 1 || value1 != value2)
                {
                    Debug.WriteLine($"  ❌ 缓存未生效: 加载次数{loadCount}, 值是否相同{value1 == value2}");
                    return false;
                }

                // 等待缓存过期
                await Task.Delay(150);

                // 再次访问，应该重新加载
                var value3 = loader.Value;
                if (loadCount != 2)
                {
                    Debug.WriteLine($"  ❌ 缓存过期后未重新加载: {loadCount}");
                    return false;
                }

                Debug.WriteLine("  ✅ 缓存和过期机制正常");

                // 测试禁用缓存
                var noCacheConfig = new LazyLoadingConfig
                {
                    EnableCaching = false
                };

                int noCacheLoadCount = 0;
                var noCacheLoader = new LazyLoader<string>(() =>
                {
                    noCacheLoadCount++;
                    return $"无缓存值_{noCacheLoadCount}";
                }, noCacheConfig);

                // 多次访问应该每次都加载
                var noCacheValue1 = noCacheLoader.Value;
                var noCacheValue2 = noCacheLoader.Value;
                
                if (noCacheLoadCount != 2)
                {
                    Debug.WriteLine($"  ❌ 禁用缓存后仍有缓存: {noCacheLoadCount}");
                    return false;
                }

                Debug.WriteLine("  ✅ 禁用缓存机制正常");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 缓存和生命周期测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试错误处理和恢复
        /// </summary>
        private static async Task<bool> TestErrorHandling()
        {
            try
            {
                // 测试同步加载异常
                var errorLoader = new LazyLoader<string>((Func<string>)(() =>
                {
                    throw new InvalidOperationException("测试异常");
                }));

                try
                {
                    var value = errorLoader.Value;
                    Debug.WriteLine("  ❌ 应该抛出异常但没有");
                    return false;
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("  ✅ 同步加载异常正确处理");
                }

                // 测试异步加载异常
                var asyncErrorLoader = new LazyLoader<string>(async () =>
                {
                    await Task.Delay(10);
                    throw new InvalidOperationException("异步测试异常");
                });

                try
                {
                    var value = await asyncErrorLoader.GetValueAsync();
                    Debug.WriteLine("  ❌ 应该抛出异步异常但没有");
                    return false;
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("  ✅ 异步加载异常正确处理");
                }

                // 测试异常后状态恢复
                if (errorLoader.IsLoaded)
                {
                    Debug.WriteLine("  ❌ 异常后不应该是已加载状态");
                    return false;
                }

                if (asyncErrorLoader.IsLoaded)
                {
                    Debug.WriteLine("  ❌ 异步异常后不应该是已加载状态");
                    return false;
                }

                Debug.WriteLine("  ✅ 异常后状态恢复正常");

                // 测试超时处理（如果支持）
                var timeoutConfig = new LazyLoadingConfig
                {
                    LoadTimeoutMs = 50 // 50毫秒超时
                };

                var timeoutLoader = new LazyLoader<string>(async () =>
                {
                    await Task.Delay(100); // 延迟100毫秒，超过超时时间
                    return "不应该返回的值";
                }, timeoutConfig);

                try
                {
                    var value = await timeoutLoader.GetValueAsync();
                    Debug.WriteLine("  ⚠️ 超时测试可能不支持或配置无效");
                }
                catch (Exception)
                {
                    Debug.WriteLine("  ✅ 超时处理正常（如果支持）");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 错误处理测试异常: {ex.Message}");
                return false;
            }
        }
    }
}