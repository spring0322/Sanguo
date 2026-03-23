using GameObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.AOTCompatibility;

namespace WorldOfTheThreeKingdoms.LazyLoading
{
    /// <summary>
    /// 按需初始化管理器
    /// 提供游戏对象的按需初始化功能，减少启动时间
    /// </summary>
    public class OnDemandInitializer
    {
        private readonly ConcurrentDictionary<int, bool> _initializedObjects;
        private readonly ConcurrentDictionary<Type, List<Action<GameObject>>> _initializationActions;
        private readonly AOTCompatibilityAdapter _aotAdapter;
        private readonly LazyLoaderFactory _lazyLoaderFactory;

        public OnDemandInitializer()
        {
            _initializedObjects = new ConcurrentDictionary<int, bool>();
            _initializationActions = new ConcurrentDictionary<Type, List<Action<GameObject>>>();
            _aotAdapter = new AOTCompatibilityAdapter();
            _lazyLoaderFactory = new LazyLoaderFactory();
            
            RegisterDefaultInitializationActions();
        }

        /// <summary>
        /// 注册默认的初始化动作
        /// </summary>
        private void RegisterDefaultInitializationActions()
        {
            Debug.WriteLine("[OnDemandInitializer] 注册默认初始化动作");

            // Faction初始化动作
            RegisterInitializationAction<Faction>(faction =>
            {
                if (faction.Leader == null && faction.LeaderID > 0)
                {
                    var leaderLoader = _lazyLoaderFactory.CreateGameObject<Person>(faction.LeaderID);
                    faction.Leader = leaderLoader.Value;
                }
            });

            // Person初始化动作
            RegisterInitializationAction<Person>(person =>
            {
                if (person.BelongedFaction == null && person.BelongedFactionID > 0)
                {
                    var factionLoader = _lazyLoaderFactory.CreateGameObject<Faction>(person.BelongedFactionID);
                    // person.BelongedFaction = factionLoader.Value;
                }
            });

            // Architecture初始化动作
            RegisterInitializationAction<Architecture>(architecture =>
            {
                if (architecture.BelongedFaction == null && architecture.BelongedFaction?.ID > 0)
                {
                    var factionLoader = _lazyLoaderFactory.CreateGameObject<Faction>(architecture.BelongedFaction.ID);
                    // architecture.BelongedFaction = factionLoader.Value;
                }

                if (architecture.Mayor == null && architecture.Mayor?.ID > 0)
                {
                    var mayorLoader = _lazyLoaderFactory.CreateGameObject<Person>(architecture.Mayor.ID);
                    architecture.Mayor = mayorLoader.Value;
                }
            });

            // Troop初始化动作
            RegisterInitializationAction<Troop>(troop =>
            {
                if (troop.BelongedFaction == null && troop.BelongedFaction?.ID > 0)
                {
                    var factionLoader = _lazyLoaderFactory.CreateGameObject<Faction>(troop.BelongedFaction.ID);
                    // troop.BelongedFaction = factionLoader.Value;
                }

                if (troop.BelongedLegion == null && troop.BelongedLegion?.ID > 0)
                {
                    var legionLoader = _lazyLoaderFactory.CreateGameObject<Legion>(troop.BelongedLegion.ID);
                    // troop.BelongedLegion = legionLoader.Value;
                }

                if (troop.Leader == null && troop.Leader?.ID > 0)
                {
                    var leaderLoader = _lazyLoaderFactory.CreateGameObject<Person>(troop.Leader.ID);
                    troop.Leader = leaderLoader.Value;
                }
            });

            // Legion初始化动作
            RegisterInitializationAction<Legion>(legion =>
            {
                if (legion.BelongedFaction == null && legion.BelongedFaction?.ID > 0)
                {
                    var factionLoader = _lazyLoaderFactory.CreateGameObject<Faction>(legion.BelongedFaction.ID);
                    legion.BelongedFaction = factionLoader.Value;
                }

                /*
                if (legion.Leader == null && legion.Leader?.ID > 0)
                {
                    var leaderLoader = _lazyLoaderFactory.CreateGameObject<Person>(legion.Leader.ID);
                    legion.Leader = leaderLoader.Value;
                }
                */
            });

            Debug.WriteLine($"[OnDemandInitializer] 已注册 {_initializationActions.Count} 种类型的初始化动作");
        }

        /// <summary>
        /// 按需初始化游戏对象
        /// </summary>
        /// <param name="gameObject">要初始化的游戏对象</param>
        /// <returns>是否成功初始化</returns>
        public bool InitializeOnDemand(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.WriteLine("[OnDemandInitializer] 对象为null，跳过初始化");
                return false;
            }

            // 检查是否已经初始化
            if (_initializedObjects.ContainsKey(gameObject.ID))
            {
                return true;
            }

            try
            {
                Debug.WriteLine($"[OnDemandInitializer] 开始按需初始化: {gameObject.GetType().Name}[{gameObject.ID}]");

                // 使用AOT适配器进行基础初始化
                _aotAdapter.InitializeObject(gameObject);

                // 执行类型特定的初始化动作
                var objectType = gameObject.GetType();
                if (_initializationActions.TryGetValue(objectType, out var actions))
                {
                    foreach (var action in actions)
                    {
                        try
                        {
                            action(gameObject);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[OnDemandInitializer] 初始化动作执行失败: {ex.Message}");
                        }
                    }
                }

                // 标记为已初始化
                _initializedObjects.TryAdd(gameObject.ID, true);

                Debug.WriteLine($"[OnDemandInitializer] 按需初始化完成: {gameObject.GetType().Name}[{gameObject.ID}]");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnDemandInitializer] 按需初始化失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步按需初始化游戏对象
        /// </summary>
        /// <param name="gameObject">要初始化的游戏对象</param>
        /// <returns>是否成功初始化</returns>
        public async Task<bool> InitializeOnDemandAsync(GameObject gameObject)
        {
            return await Task.Run(() => InitializeOnDemand(gameObject));
        }

        /// <summary>
        /// 批量按需初始化游戏对象
        /// </summary>
        /// <param name="gameObjects">要初始化的游戏对象列表</param>
        /// <returns>成功初始化的对象数量</returns>
        public int InitializeBatchOnDemand(List<GameObject> gameObjects)
        {
            if (gameObjects == null || gameObjects.Count == 0)
            {
                return 0;
            }

            int successCount = 0;
            foreach (var gameObject in gameObjects)
            {
                if (InitializeOnDemand(gameObject))
                {
                    successCount++;
                }
            }

            Debug.WriteLine($"[OnDemandInitializer] 批量按需初始化完成: {successCount}/{gameObjects.Count}");
            return successCount;
        }

        /// <summary>
        /// 异步批量按需初始化游戏对象
        /// </summary>
        /// <param name="gameObjects">要初始化的游戏对象列表</param>
        /// <returns>成功初始化的对象数量</returns>
        public async Task<int> InitializeBatchOnDemandAsync(List<GameObject> gameObjects)
        {
            return await Task.Run(() => InitializeBatchOnDemand(gameObjects));
        }

        /// <summary>
        /// 注册初始化动作
        /// </summary>
        /// <typeparam name="T">游戏对象类型</typeparam>
        /// <param name="action">初始化动作</param>
        public void RegisterInitializationAction<T>(Action<T> action) where T : GameObject
        {
            if (action == null) return;

            var type = typeof(T);
            var wrappedAction = new Action<GameObject>(obj =>
            {
                if (obj is T typedObj)
                {
                    action(typedObj);
                }
            });

            _initializationActions.AddOrUpdate(type,
                new List<Action<GameObject>> { wrappedAction },
                (key, existing) =>
                {
                    existing.Add(wrappedAction);
                    return existing;
                });

            Debug.WriteLine($"[OnDemandInitializer] 注册初始化动作: {type.Name}");
        }

        /// <summary>
        /// 检查对象是否已初始化
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>是否已初始化</returns>
        public bool IsInitialized(GameObject gameObject)
        {
            return gameObject != null && _initializedObjects.ContainsKey(gameObject.ID);
        }

        /// <summary>
        /// 强制重新初始化对象
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>是否成功重新初始化</returns>
        public bool ForceReinitialize(GameObject gameObject)
        {
            if (gameObject == null) return false;

            // 移除初始化标记
            _initializedObjects.TryRemove(gameObject.ID, out _);

            // 重新初始化
            return InitializeOnDemand(gameObject);
        }

        /// <summary>
        /// 清除初始化状态
        /// </summary>
        public void ClearInitializationState()
        {
            _initializedObjects.Clear();
            Debug.WriteLine("[OnDemandInitializer] 初始化状态已清除");
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetStats()
        {
            return $"按需初始化管理器: 已初始化 {_initializedObjects.Count} 个对象，支持 {_initializationActions.Count} 种类型";
        }
    }
}