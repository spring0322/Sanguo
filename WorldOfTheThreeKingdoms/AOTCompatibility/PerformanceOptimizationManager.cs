using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// 性能优化管理器
    /// 提供启动时间优化、内存管理和缓存机制
    /// </summary>
    public class PerformanceOptimizationManager
    {
        /// <summary>
        /// 性能统计信息
        /// </summary>
        public class PerformanceStats
        {
            public TimeSpan StartupTime { get; set; }
            public long MemoryUsageMB { get; set; }
            public int CachedObjectsCount { get; set; }
            public int OptimizedOperationsCount { get; set; }
            public DateTime LastOptimizationTime { get; set; }
            public Dictionary<string, TimeSpan> OperationTimes { get; set; } = new Dictionary<string, TimeSpan>();

            public string GetSummary()
            {
                return $"性能统计: 启动{StartupTime.TotalMilliseconds:F0}ms, 内存{MemoryUsageMB}MB, 缓存{CachedObjectsCount}个对象";
            }
        }

        /// <summary>
        /// 优化配置
        /// </summary>
        public class OptimizationConfig
        {
            public bool EnableStartupOptimization { get; set; } = true;
            public bool EnableMemoryOptimization { get; set; } = true;
            public bool EnableCaching { get; set; } = true;
            public bool EnableLazyLoading { get; set; } = true;
            public int CacheMaxSize { get; set; } = 10000;
            public int MemoryCleanupIntervalMs { get; set; } = 300000; // 5分钟
            public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount;
        }

        private readonly OptimizationConfig _config;
        private readonly ConcurrentDictionary<string, object> _cache;
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;
        private readonly Timer _memoryCleanupTimer;
        private readonly PerformanceStats _stats;
        private readonly SemaphoreSlim _operationSemaphore;

        public PerformanceOptimizationManager(OptimizationConfig config = null)
        {
            _config = config ?? new OptimizationConfig();
            _cache = new ConcurrentDictionary<string, object>();
            _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
            _stats = new PerformanceStats();
            _operationSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);

            // 启动内存清理定时器
            if (_config.EnableMemoryOptimization)
            {
                _memoryCleanupTimer = new Timer(PerformMemoryCleanup, null, 
                    _config.MemoryCleanupIntervalMs, _config.MemoryCleanupIntervalMs);
            }

            Debug.WriteLine("[PerformanceOptimizationManager] 性能优化管理器已初始化");
        }

        /// <summary>
        /// 优化启动时间
        /// </summary>
        /// <returns>优化结果</returns>
        public async Task<bool> OptimizeStartupTime()
        {
            if (!_config.EnableStartupOptimization)
            {
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("[PerformanceOptimizationManager] 开始启动时间优化");

            try
            {
                // 1. 预热关键组件
                await PrewarmCriticalComponents();

                // 2. 优化数据加载
                await OptimizeDataLoading();

                // 3. 初始化缓存
                InitializeCache();

                stopwatch.Stop();
                _stats.StartupTime = stopwatch.Elapsed;
                _stats.LastOptimizationTime = DateTime.Now;

                Debug.WriteLine($"[PerformanceOptimizationManager] 启动优化完成: {stopwatch.ElapsedMilliseconds}ms");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerformanceOptimizationManager] 启动优化失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 预热关键组件
        /// </summary>
        private async Task PrewarmCriticalComponents()
        {
            Debug.WriteLine("[PerformanceOptimizationManager] 预热关键组件");

            var tasks = new List<Task>();

            // 预热序列化组件
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var testObject = new Person { ID = 0 };
                    // Name assignment removed due to read-only property
                    var json = System.Text.Json.JsonSerializer.Serialize(testObject);
                    Debug.WriteLine("  ✅ 序列化组件预热完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠️ 序列化组件预热失败: {ex.Message}");
                }
            }));

            // 预热数据完整性检查器
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var checker = new WorldOfTheThreeKingdoms.DataIntegrity.DataIntegrityChecker();
                    Debug.WriteLine("  ✅ 数据完整性检查器预热完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠️ 数据完整性检查器预热失败: {ex.Message}");
                }
            }));

            // 预热AOT兼容性适配器
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var adapter = new AOTCompatibilityAdapter();
                    adapter.RegisterTypeHandlers();
                    Debug.WriteLine("  ✅ AOT兼容性适配器预热完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠️ AOT兼容性适配器预热失败: {ex.Message}");
                }
            }));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 优化数据加载
        /// </summary>
        private async Task OptimizeDataLoading()
        {
            Debug.WriteLine("[PerformanceOptimizationManager] 优化数据加载");

            if (Session.Current?.Scenario == null)
            {
                Debug.WriteLine("  ⚠️ 无当前场景，跳过数据加载优化");
                return;
            }

            try
            {
                // 使用并行处理优化数据加载
                var tasks = new List<Task>();

                // 预加载关键数据
                if (_config.EnableLazyLoading)
                {
                    tasks.Add(PreloadCriticalData());
                }

                // 优化内存使用
                if (_config.EnableMemoryOptimization)
                {
                    tasks.Add(OptimizeMemoryUsage());
                }

                await Task.WhenAll(tasks);
                Debug.WriteLine("  ✅ 数据加载优化完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ⚠️ 数据加载优化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 预加载关键数据
        /// </summary>
        private async Task PreloadCriticalData()
        {
            Debug.WriteLine("[PerformanceOptimizationManager] 预加载关键数据");

            try
            {
                var scenario = Session.Current.Scenario;
                
                // 预加载势力领袖关系
                if (scenario.Factions != null)
                {
                    await Task.Run(() =>
                    {
                        int preloadedCount = 0;
                        foreach (GameObject obj in scenario.Factions.GameObjects)
                        {
                            var faction = (obj is Faction ? (Faction)obj : null);
                            if (faction == null) continue;

                            if (faction.Leader == null && faction.LeaderID > 0)
                            {
                                // 触发延迟加载
                                var leader = scenario.Persons?.GetGameObject(faction.LeaderID);
                                if (leader != null)
                                {
                                    faction.Leader = (leader is Person ? (Person)leader : null);
                                    preloadedCount++;
                                }
                            }
                        }
                        Debug.WriteLine($"    预加载了 {preloadedCount} 个势力领袖关系");
                    });
                }

                // 预加载建筑所属势力关系
                if (scenario.Architectures != null)
                {
                    await Task.Run(() =>
                    {
                        int preloadedCount = 0;
                        foreach (GameObject obj in scenario.Architectures.GameObjects)
                        {
                            var arch = (obj is Architecture ? (Architecture)obj : null);
                            if (arch == null) continue;

                            if (arch.BelongedFaction == null && arch.BelongedFaction?.ID > 0)
                            {
                                var faction = scenario.Factions?.GetGameObject(arch.BelongedFaction.ID) as Faction;
                                if (faction != null)
                                {
                                    arch.BelongedFaction = faction;
                                    preloadedCount++;
                                }
                            }
                        }
                        Debug.WriteLine($"    预加载了 {preloadedCount} 个建筑所属势力关系");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"    预加载关键数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 优化内存使用
        /// </summary>
        private async Task OptimizeMemoryUsage()
        {
            Debug.WriteLine("[PerformanceOptimizationManager] 优化内存使用");

            await Task.Run(() =>
            {
                try
                {
                    // 强制垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // 更新内存统计
                    _stats.MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024);
                    
                    Debug.WriteLine($"    内存优化完成，当前使用: {_stats.MemoryUsageMB}MB");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"    内存优化失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitializeCache()
        {
            Debug.WriteLine("[PerformanceOptimizationManager] 初始化缓存");

            if (!_config.EnableCaching)
            {
                return;
            }

            try
            {
                // 清理旧缓存
                _cache.Clear();
                _cacheTimestamps.Clear();

                // 预缓存常用数据
                if (Session.Current?.Scenario != null)
                {
                    var scenario = Session.Current.Scenario;
                    
                    // 缓存势力数量
                    CacheValue("faction_count", scenario.Factions?.GameObjects?.Count ?? 0);
                    
                    // 缓存人物数量
                    CacheValue("person_count", scenario.Persons?.GameObjects?.Count ?? 0);
                    
                    // 缓存建筑数量
                    CacheValue("architecture_count", scenario.Architectures?.GameObjects?.Count ?? 0);
                }

                _stats.CachedObjectsCount = _cache.Count;
                Debug.WriteLine($"  ✅ 缓存初始化完成，缓存 {_stats.CachedObjectsCount} 个对象");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ⚠️ 缓存初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 缓存值
        /// </summary>
        public void CacheValue<T>(string key, T value)
        {
            if (!_config.EnableCaching || _cache.Count >= _config.CacheMaxSize)
            {
                return;
            }

            _cache.TryAdd(key, value);
            _cacheTimestamps.TryAdd(key, DateTime.Now);
        }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        public T GetCachedValue<T>(string key, T defaultValue = default(T))
        {
            if (!_config.EnableCaching)
            {
                return defaultValue;
            }

            if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// 执行内存清理
        /// </summary>
        private void PerformMemoryCleanup(object state)
        {
            try
            {
                Debug.WriteLine("[PerformanceOptimizationManager] 执行内存清理");

                // 清理过期缓存
                CleanupExpiredCache();

                // 执行垃圾回收
                if (_stats.MemoryUsageMB > 500) // 如果内存使用超过500MB
                {
                    GC.Collect();
                    _stats.MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024);
                    Debug.WriteLine($"  内存清理完成，当前使用: {_stats.MemoryUsageMB}MB");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerformanceOptimizationManager] 内存清理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        private void CleanupExpiredCache()
        {
            var expiredKeys = new List<string>();
            var expireTime = DateTime.Now.AddMinutes(-10); // 10分钟过期

            foreach (var kvp in _cacheTimestamps)
            {
                if (kvp.Value < expireTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
                _cacheTimestamps.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _stats.CachedObjectsCount = _cache.Count;
                Debug.WriteLine($"  清理了 {expiredKeys.Count} 个过期缓存项");
            }
        }

        /// <summary>
        /// 执行性能优化操作
        /// </summary>
        public async Task<T> ExecuteOptimizedOperation<T>(string operationName, Func<Task<T>> operation)
        {
            await _operationSemaphore.WaitAsync();
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await operation();
                stopwatch.Stop();

                _stats.OperationTimes[operationName] = stopwatch.Elapsed;
                _stats.OptimizedOperationsCount++;

                Debug.WriteLine($"[PerformanceOptimizationManager] 操作 {operationName} 完成: {stopwatch.ElapsedMilliseconds}ms");
                return result;
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            _stats.MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024);
            _stats.CachedObjectsCount = _cache.Count;
            return _stats;
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public string GeneratePerformanceReport()
        {
            var stats = GetPerformanceStats();
            var report = new List<string>
            {
                "=== 性能优化报告 ===",
                $"启动时间: {stats.StartupTime.TotalMilliseconds:F0}ms",
                $"内存使用: {stats.MemoryUsageMB}MB",
                $"缓存对象: {stats.CachedObjectsCount}个",
                $"优化操作: {stats.OptimizedOperationsCount}次",
                $"最后优化: {stats.LastOptimizationTime:yyyy-MM-dd HH:mm:ss}"
            };

            if (stats.OperationTimes.Any())
            {
                report.Add("\n操作耗时统计:");
                foreach (var kvp in stats.OperationTimes.OrderByDescending(x => x.Value))
                {
                    report.Add($"  {kvp.Key}: {kvp.Value.TotalMilliseconds:F0}ms");
                }
            }

            return string.Join("\n", report);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _memoryCleanupTimer?.Dispose();
            _operationSemaphore?.Dispose();
            _cache.Clear();
            _cacheTimestamps.Clear();
            
            Debug.WriteLine("[PerformanceOptimizationManager] 性能优化管理器已释放");
        }
    }
}
