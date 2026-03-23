using GameObjects;
using System;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.LazyLoading
{
    /// <summary>
    /// 延迟加载器接口
    /// 提供AOT兼容的延迟加载功能
    /// </summary>
    public interface ILazyLoader<T> where T : class
    {
        /// <summary>
        /// 是否已加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 获取值（如果未加载则触发加载）
        /// </summary>
        T Value { get; }

        /// <summary>
        /// 异步获取值
        /// </summary>
        Task<T> GetValueAsync();

        /// <summary>
        /// 强制重新加载
        /// </summary>
        void Reload();

        /// <summary>
        /// 异步强制重新加载
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// 清除已加载的值
        /// </summary>
        void Clear();

        /// <summary>
        /// 检查是否需要重新加载
        /// </summary>
        bool ShouldReload();
    }

    /// <summary>
    /// 延迟加载工厂接口
    /// </summary>
    public interface ILazyLoaderFactory
    {
        /// <summary>
        /// 创建延迟加载器
        /// </summary>
        /// <typeparam name="T">加载的类型</typeparam>
        /// <param name="loader">加载函数</param>
        /// <returns>延迟加载器</returns>
        ILazyLoader<T> Create<T>(Func<T> loader) where T : class;

        /// <summary>
        /// 创建异步延迟加载器
        /// </summary>
        /// <typeparam name="T">加载的类型</typeparam>
        /// <param name="loader">异步加载函数</param>
        /// <returns>延迟加载器</returns>
        ILazyLoader<T> Create<T>(Func<Task<T>> loader) where T : class;

        /// <summary>
        /// 创建游戏对象延迟加载器
        /// </summary>
        /// <typeparam name="T">游戏对象类型</typeparam>
        /// <param name="id">对象ID</param>
        /// <returns>延迟加载器</returns>
        ILazyLoader<T> CreateGameObject<T>(int id) where T : GameObject;

        /// <summary>
        /// 创建关系延迟加载器
        /// </summary>
        /// <typeparam name="T">关系对象类型</typeparam>
        /// <param name="sourceId">源对象ID</param>
        /// <param name="relationshipType">关系类型</param>
        /// <returns>延迟加载器</returns>
        ILazyLoader<T> CreateRelationship<T>(int sourceId, string relationshipType) where T : GameObject;
    }

    /// <summary>
    /// 延迟加载配置
    /// </summary>
    public class LazyLoadingConfig
    {
        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（毫秒）
        /// </summary>
        public int CacheExpirationMs { get; set; } = 300000; // 5分钟

        /// <summary>
        /// 是否启用异步加载
        /// </summary>
        public bool EnableAsyncLoading { get; set; } = true;

        /// <summary>
        /// 加载超时时间（毫秒）
        /// </summary>
        public int LoadTimeoutMs { get; set; } = 30000; // 30秒

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool EnablePreloading { get; set; } = false;

        /// <summary>
        /// 最大并发加载数
        /// </summary>
        public int MaxConcurrentLoads { get; set; } = 10;
    }
}