using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;
using System.Diagnostics.CodeAnalysis;

namespace GameManager
{
    /// <summary>
    /// 对象池健康状态
    /// </summary>
    public enum PoolHealthStatus
    {
        Unknown,    // 未知 (还没有足够数据)
        Excellent,  // 优秀 (命中率>80%, 丢弃率<10%)
        Good,       // 良好 (命中率>60%, 丢弃率<20%)
        Fair,       // 一般 (命中率>40%, 丢弃率<30%)
        Poor        // 较差 (需要优化)
    }

    /// <summary>
    /// 高性能对象池 - 使用Stack提高CPU缓存命中率(LIFO)
    /// 🎯 增强特性：
    /// 1. 支持工厂方法创建对象
    /// 2. 智能容量管理和统计
    /// 3. 性能监控和调试支持
    /// 4. 线程安全的基础操作
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    public class ObjectPool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> where T : class
    {
        // 使用 Stack 提高 CPU 缓存命中率 (LIFO)
        private readonly Stack<T> _pool;
        private readonly int _maxCapacity;
        private readonly Func<T> _factory; // 工厂方法
        private readonly object _lockObject = new object(); // 线程安全锁

        // 统计数据（用于 Debug 调整池大小）
        public int Count => _pool.Count;
        public int TotalCreated { get; private set; }
        public int TotalReturned { get; private set; }
        public int TotalDiscarded { get; private set; }
        public int PeakUsage { get; private set; } // 峰值使用量
        
        // 性能监控
        public float HitRate => TotalReturned > 0 ? (float)(TotalReturned - TotalCreated) / TotalReturned : 0f;
        public bool IsHealthy => HitRate > 0.7f; // 命中率超过70%认为健康

        /// <summary>
        /// 构造函数 - 使用默认new()创建对象 (需要T有无参构造函数)
        /// ⚠️ AOT 升级：此构造函数已废弃，必须使用工厂方法版本
        /// </summary>
        [Obsolete("AOT 环境不支持通过反射创建泛型对象。请使用带有 Func<T> factory 的构造函数。", error: true)]
        public ObjectPool(int initialCapacity = 100, int maxCapacity = 1000) 
            : this(() => (T)System.Activator.CreateInstance(typeof(T)), initialCapacity, maxCapacity)
        {
            throw new NotSupportedException(
                "中华三国志 AOT 升级：严禁使用反射初始化对象池，必须显式传入工厂方法。");
        }
        
        /// <summary>
        /// 构造函数 - 使用工厂方法创建对象 (推荐)
        /// </summary>
        /// <param name="factory">对象创建工厂方法</param>
        /// <param name="initialCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        public ObjectPool(Func<T> factory, int initialCapacity = 100, int maxCapacity = 1000)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _pool = new Stack<T>(initialCapacity);
            _maxCapacity = maxCapacity;
            TotalCreated = 0;
            TotalReturned = 0;
            TotalDiscarded = 0;
            PeakUsage = 0;
        }

        /// <summary>
        /// 从池中获取对象 - 线程安全版本
        /// </summary>
        public T Get()
        {
            lock (_lockObject)
            {
                T item;
                
                if (_pool.Count > 0)
                {
                    item = _pool.Pop();
                }
                else
                {
                    // 使用工厂方法创建新对象
                    item = _factory();
                    TotalCreated++;
                    
                    // 更新峰值使用量
                    int currentUsage = TotalCreated - _pool.Count;
                    if (currentUsage > PeakUsage)
                    {
                        PeakUsage = currentUsage;
                    }
                }
                
                // 拿出来时Reset，确保拿到的肯定是干净的
                if (item is IResettable resettable)
                {
                    try
                    {
                        resettable.Reset();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>] Reset错误: {ex.Message}");
                        // Reset失败时重新创建对象
                        item = _factory();
                        TotalCreated++;
                    }
                }
                
                return item;
            }
        }
        
        /// <summary>
        /// 非线程安全的快速获取 - 用于单线程高性能场景
        /// </summary>
        public T GetFast()
        {
            T item;
            
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
            }
            else
            {
                item = _factory();
                TotalCreated++;
            }
            
            // 快速Reset
            if (item is IResettable resettable)
            {
                resettable.Reset();
            }
            
            return item;
        }

        /// <summary>
        /// 将对象归还到池中 - 线程安全版本
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            lock (_lockObject)
            {
                // 防止池子无限膨胀 (比如加载关卡时瞬间峰值)
                if (_pool.Count < _maxCapacity)
                {
                    // 可选：在归还时进行额外的Reset (双重保险)
                    // if (item is IResettable resettable)
                    // {
                    //     resettable.Reset();
                    // }
                    
                    _pool.Push(item);
                    TotalReturned++;
                }
                else
                {
                    // 池子满了，丢弃该对象，让 GC 稍后回收
                    // 这比无限持有内存要安全
                    TotalDiscarded++;
                    
                    // 记录警告 (但不要每次都记录，避免刷屏)
                    if (TotalDiscarded % 100 == 1) // 每100次记录一次
                    {
                        System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>] 警告: 池子已满，已丢弃{TotalDiscarded}个对象");
                    }
                }
            }
        }
        
        /// <summary>
        /// 非线程安全的快速归还 - 用于单线程高性能场景
        /// </summary>
        public void ReturnFast(T item)
        {
            if (item == null) return;

            if (_pool.Count < _maxCapacity)
            {
                _pool.Push(item);
                TotalReturned++;
            }
            else
            {
                TotalDiscarded++;
            }
        }
        
        /// <summary>
        /// 批量归还对象 - 高性能批处理
        /// </summary>
        public void ReturnBatch(IEnumerable<T> items)
        {
            if (items == null) return;
            
            lock (_lockObject)
            {
                foreach (var item in items)
                {
                    if (item != null && _pool.Count < _maxCapacity)
                    {
                        _pool.Push(item);
                        TotalReturned++;
                    }
                    else if (item != null)
                    {
                        TotalDiscarded++;
                    }
                }
            }
        }

        /// <summary>
        /// 清空池子 - 用于场景切换时释放内存
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }

        /// <summary>
        /// 获取池子统计信息 - 增强版
        /// </summary>
        public string GetStats()
        {
            return $"Pool<{typeof(T).Name}>: Count={Count}/{_maxCapacity}, Created={TotalCreated}, Returned={TotalReturned}, Discarded={TotalDiscarded}, Peak={PeakUsage}, HitRate={HitRate:P1}, Health={IsHealthy}";
        }
        
        /// <summary>
        /// 获取详细性能报告
        /// </summary>
        public string GetDetailedStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine($"=== ObjectPool<{typeof(T).Name}> 详细统计 ===");
            stats.AppendLine($"当前数量: {Count} / {_maxCapacity}");
            stats.AppendLine($"总创建数: {TotalCreated}");
            stats.AppendLine($"总归还数: {TotalReturned}");
            stats.AppendLine($"总丢弃数: {TotalDiscarded}");
            stats.AppendLine($"峰值使用: {PeakUsage}");
            stats.AppendLine($"命中率: {HitRate:P2}");
            stats.AppendLine($"健康状态: {(IsHealthy ? "✅ 良好" : "⚠️ 需优化")}");
            
            // 使用效率分析
            if (TotalReturned > 0)
            {
                float reuseRate = (float)(TotalReturned - TotalCreated) / TotalReturned;
                stats.AppendLine($"重用率: {reuseRate:P2}");
                
                if (reuseRate < 0.5f)
                {
                    stats.AppendLine("💡 建议: 考虑增加预热数量或池子容量");
                }
            }
            
            if (TotalDiscarded > TotalCreated * 0.1f)
            {
                stats.AppendLine("⚠️ 警告: 丢弃率过高，建议增加池子容量");
            }
            
            return stats.ToString();
        }
        
        /// <summary>
        /// 检查池子健康状态
        /// </summary>
        public PoolHealthStatus GetHealthStatus()
        {
            if (TotalReturned == 0) return PoolHealthStatus.Unknown;
            
            float hitRate = HitRate;
            float discardRate = (float)TotalDiscarded / TotalReturned;
            
            if (hitRate > 0.8f && discardRate < 0.1f)
                return PoolHealthStatus.Excellent;
            else if (hitRate > 0.6f && discardRate < 0.2f)
                return PoolHealthStatus.Good;
            else if (hitRate > 0.4f && discardRate < 0.3f)
                return PoolHealthStatus.Fair;
            else
                return PoolHealthStatus.Poor;
        }

        /// <summary>
        /// 预热池子 - 在Loading阶段调用，智能预创建对象
        /// </summary>
        /// <param name="count">目标预热数量</param>
        public void Prewarm(int count)
        {
            try
            {
                // 智能容量管理 - 确保不超过最大容量
                count = Math.Min(count, _maxCapacity);
                
                // 获取当前池中对象数量
                int currentCount = Count;
                
                // 如果已经达到或超过目标数量，直接返回
                if (currentCount >= count) 
                {
                    System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>] 预热跳过: 当前{currentCount}个对象已满足需求{count}个");
                    return;
                }
                
                // 计算需要创建的对象数量
                int toCreate = count - currentCount;
                
                // 智能批量创建 - 分批创建避免内存峰值
                int batchSize = Math.Min(50, toCreate); // 每批最多50个对象
                int batches = (toCreate + batchSize - 1) / batchSize; // 向上取整
                
                System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>] 开始智能预热: 需要创建{toCreate}个对象，分{batches}批处理");
                
                for (int batch = 0; batch < batches; batch++)
                {
                    int currentBatchSize = Math.Min(batchSize, toCreate - batch * batchSize);
                    
                    // 批量创建对象并放入池中
                    for (int i = 0; i < currentBatchSize; i++)
                    {
                        var item = _factory();
                        _pool.Push(item);
                        TotalCreated++;
                    }
                    
                    // 每批之间稍作停顿，避免内存分配过于集中
                    if (batch < batches - 1)
                    {
                        System.Threading.Thread.Sleep(1); // 1ms停顿
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>] 智能预热完成: 创建了{toCreate}个对象，池中共有{Count}个对象，总创建数{TotalCreated}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObjectPool<{typeof(T).Name}>.Prewarm] 预热错误: {ex.Message}");
                // 预热失败不应该影响游戏运行，只记录错误
            }
        }
    }

    /// <summary>
    /// 对象池管理器 - 统一管理所有对象池
    /// </summary>
    public static class ObjectPoolManager
    {
        // 常用对象池（AOT 安全：使用工厂方法）
        public static ObjectPool<List<object>> ListPool = new ObjectPool<List<object>>(() => new List<object>(), 50, 200);
        public static ObjectPool<Dictionary<int, object>> DictPool = new ObjectPool<Dictionary<int, object>>(() => new Dictionary<int, object>(), 20, 100);
        
        // 游戏特定对象池 - 根据需要添加
        public static ObjectPool<TroopDamage> DamagePool = new ObjectPool<TroopDamage>(() => new TroopDamage(), 500, 2000);
        public static ObjectPool<List<Point>> PathPool = new ObjectPool<List<Point>>(() => new List<Point>(50), 100, 500);
        // public static ObjectPool<Vector2> Vector2Pool = new ObjectPool<Vector2>(100, 500);

        /// <summary>
        /// 获取所有池子的统计信息
        /// </summary>
        public static string GetAllStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 对象池统计 ===");
            stats.AppendLine(ListPool.GetStats());
            stats.AppendLine(DictPool.GetStats());
            stats.AppendLine(DamagePool.GetStats());
            stats.AppendLine(PathPool.GetStats());
            // 添加其他池子的统计
            return stats.ToString();
        }

        /// <summary>
        /// 清空所有池子 - 用于场景切换
        /// </summary>
        public static void ClearAll()
        {
            ListPool.Clear();
            DictPool.Clear();
            DamagePool.Clear();
            PathPool.Clear();
            // 清空其他池子
            
            // System.Diagnostics.Debug.WriteLine("[ObjectPoolManager] 所有对象池已清空");
        }

        /// <summary>
        /// 预热所有池子 - 在Loading阶段调用，智能调整预热数量
        /// </summary>
        public static void PrewarmAll()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("[ObjectPoolManager] 开始智能预热所有对象池...");
                
                // 检测系统内存情况，智能调整预热策略
                long availableMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
                string memoryLevel = availableMemoryMB < 512 ? "low" : availableMemoryMB > 2048 ? "high" : "medium";
                
                // System.Diagnostics.Debug.WriteLine($"[ObjectPoolManager] 检测到内存级别: {memoryLevel} (当前使用: {availableMemoryMB}MB)");
                
                // 根据内存情况调整预热数量
                int listPoolSize, dictPoolSize;
                switch (memoryLevel)
                {
                    case "low":
                        listPoolSize = 10;
                        dictPoolSize = 5;
                        break;
                    case "high":
                        listPoolSize = 50;
                        dictPoolSize = 25;
                        break;
                    default: // medium
                        listPoolSize = 20;
                        dictPoolSize = 10;
                        break;
                }
                
                // 执行智能预热
                ListPool.Prewarm(listPoolSize);
                DictPool.Prewarm(dictPoolSize);
                DamagePool.Prewarm(Math.Min(500, listPoolSize * 10)); // 伤害池需要更多对象
                PathPool.Prewarm(Math.Min(100, listPoolSize * 2)); // 路径池适中
                // 预热其他池子
                
                // System.Diagnostics.Debug.WriteLine($"[ObjectPoolManager] 智能预热完成 (内存级别: {memoryLevel})");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[ObjectPoolManager] 预热错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 预热所有池子 - 保持向后兼容
        /// </summary>
        public static void WarmupAll()
        {
            PrewarmAll();
        }
    }
}