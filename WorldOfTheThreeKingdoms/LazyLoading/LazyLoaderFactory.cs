using GameObjects;
using GameManager;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace WorldOfTheThreeKingdoms.LazyLoading
{
    /// <summary>
    /// 延迟加载工厂实现
    /// 提供各种类型的延迟加载器创建
    /// </summary>
    public class LazyLoaderFactory : ILazyLoaderFactory
    {
        private readonly LazyLoadingConfig _defaultConfig;
        private readonly ConcurrentDictionary<string, ILazyLoader<GameObject>> _gameObjectCache;

        public LazyLoaderFactory(LazyLoadingConfig config = null)
        {
            _defaultConfig = config ?? new LazyLoadingConfig();
            _gameObjectCache = new ConcurrentDictionary<string, ILazyLoader<GameObject>>();
        }

        /// <summary>
        /// 创建延迟加载器
        /// </summary>
        public ILazyLoader<T> Create<T>(Func<T> loader) where T : class
        {
            return new LazyLoader<T>(loader, _defaultConfig);
        }

        /// <summary>
        /// 创建异步延迟加载器
        /// </summary>
        public ILazyLoader<T> Create<T>(Func<Task<T>> loader) where T : class
        {
            return new LazyLoader<T>(loader, _defaultConfig);
        }

        /// <summary>
        /// 创建游戏对象延迟加载器
        /// </summary>
        public ILazyLoader<T> CreateGameObject<T>(int id) where T : GameObject
        {
            var cacheKey = $"{typeof(T).Name}_{id}";
            
            if (_gameObjectCache.TryGetValue(cacheKey, out var cachedLoader) && cachedLoader is ILazyLoader<T> typedLoader)
            {
                return typedLoader;
            }

            var loader = new LazyLoader<T>(() => LoadGameObject<T>(id), _defaultConfig);
            
            // 缓存加载器（如果启用缓存）
            if (_defaultConfig.EnableCaching)
            {
                _gameObjectCache.TryAdd(cacheKey, loader as ILazyLoader<GameObject>);
            }

            return loader;
        }

        /// <summary>
        /// 创建关系延迟加载器
        /// </summary>
        public ILazyLoader<T> CreateRelationship<T>(int sourceId, string relationshipType) where T : GameObject
        {
            return new LazyLoader<T>(() => LoadRelationship<T>(sourceId, relationshipType), _defaultConfig);
        }

        /// <summary>
        /// 加载游戏对象
        /// </summary>
        private T LoadGameObject<T>(int id) where T : GameObject
        {
            try
            {
                Debug.WriteLine($"[LazyLoaderFactory] 加载游戏对象: {typeof(T).Name}[{id}]");

                // 根据类型从相应的集合中加载对象
                if (typeof(T) == typeof(Person))
                {
                    return Session.Current?.Scenario?.Persons?.GetGameObject(id) as T;
                }
                else if (typeof(T) == typeof(Architecture))
                {
                    return Session.Current?.Scenario?.Architectures?.GetGameObject(id) as T;
                }
                else if (typeof(T) == typeof(Faction))
                {
                    return Session.Current?.Scenario?.Factions?.GetGameObject(id) as T;
                }
                else if (typeof(T) == typeof(Troop))
                {
                    return Session.Current?.Scenario?.Troops?.GetGameObject(id) as T;
                }
                else if (typeof(T) == typeof(Legion))
                {
                    return Session.Current?.Scenario?.Legions?.GetGameObject(id) as T;
                }

                Debug.WriteLine($"[LazyLoaderFactory] 不支持的游戏对象类型: {typeof(T).Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LazyLoaderFactory] 加载游戏对象失败 {typeof(T).Name}[{id}]: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载关系对象
        /// </summary>
        private T LoadRelationship<T>(int sourceId, string relationshipType) where T : GameObject
        {
            try
            {
                Debug.WriteLine($"[LazyLoaderFactory] 加载关系对象: {relationshipType} -> {typeof(T).Name} (源ID: {sourceId})");

                // 根据关系类型加载相关对象
                switch (relationshipType.ToLower())
                {
                    case "leader":
                        if (typeof(T) == typeof(Person))
                        {
                            // AOT修复: 原 as Faction 转换
                            if (Session.Current?.Scenario?.Factions?.GetGameObject(sourceId) is Faction faction)
                            {
                                return faction?.Leader as T;
                            }
                        }
                        break;

                    case "belongedfaction":
                        if (typeof(T) == typeof(Faction))
                        {
                            // 可能是Person、Architecture或Troop的BelongedFaction
                            // AOT修复: 原 as Person 转换
                            if (Session.Current?.Scenario?.Persons?.GetGameObject(sourceId) is Person person)
                            {
                                return person.BelongedFaction as T;
                            }

                            // AOT修复: 原 as Architecture 转换
                            if (Session.Current?.Scenario?.Architectures?.GetGameObject(sourceId) is Architecture architecture)
                            {
                                return architecture.BelongedFaction as T;
                            }

                            // AOT修复: 原 as Troop 转换
                            if (Session.Current?.Scenario?.Troops?.GetGameObject(sourceId) is Troop troop)
                            {
                                return troop.BelongedFaction as T;
                            }
                        }
                        break;

                    case "belongedlegion":
                        if (typeof(T) == typeof(Legion))
                        {
                            // AOT修复: 原 as Troop 转换
                            if (Session.Current?.Scenario?.Troops?.GetGameObject(sourceId) is Troop troop)
                            {
                                return troop?.BelongedLegion as T;
                            }
                        }
                        break;

                    case "mayor":
                        if (typeof(T) == typeof(Person))
                        {
                            // AOT修复: 原 as Architecture 转换
                            if (Session.Current?.Scenario?.Architectures?.GetGameObject(sourceId) is Architecture architecture)
                            {
                                return architecture?.Mayor as T;
                            }
                        }
                        break;
                }

                Debug.WriteLine($"[LazyLoaderFactory] 不支持的关系类型: {relationshipType}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LazyLoaderFactory] 加载关系对象失败 {relationshipType}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _gameObjectCache.Clear();
            Debug.WriteLine("[LazyLoaderFactory] 缓存已清除");
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            return $"延迟加载工厂缓存: {_gameObjectCache.Count} 个加载器";
        }

        /// <summary>
        /// 预加载游戏对象
        /// </summary>
        public async Task PreloadGameObjectsAsync<T>(int[] ids) where T : GameObject
        {
            if (!_defaultConfig.EnablePreloading || ids == null || ids.Length == 0)
            {
                return;
            }

            Debug.WriteLine($"[LazyLoaderFactory] 开始预加载 {typeof(T).Name} x {ids.Length}");

            var tasks = new Task[Math.Min(ids.Length, _defaultConfig.MaxConcurrentLoads)];
            int taskIndex = 0;

            foreach (var id in ids)
            {
                if (taskIndex >= tasks.Length)
                {
                    // 等待一个任务完成
                    await Task.WhenAny(tasks);
                    taskIndex = Array.FindIndex(tasks, t => t.IsCompleted);
                }

                tasks[taskIndex] = Task.Run(() =>
                {
                    var loader = CreateGameObject<T>(id);
                    _ = loader.Value; // 触发加载
                });

                taskIndex++;
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks.Where(t => t != null));

            Debug.WriteLine($"[LazyLoaderFactory] 预加载完成 {typeof(T).Name}");
        }
    }
}