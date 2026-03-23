using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.LazyLoading
{
    /// <summary>
    /// 延迟加载器实现
    /// 提供AOT兼容的延迟加载功能
    /// </summary>
    public class LazyLoader<T> : ILazyLoader<T> where T : class
    {
        private readonly Func<T> _syncLoader;
        private readonly Func<Task<T>> _asyncLoader;
        private readonly LazyLoadingConfig _config;
        private readonly object _lock = new object();
        
        private T _value;
        private bool _isLoaded;
        private DateTime _loadTime;
        private Task<T> _loadingTask;

        /// <summary>
        /// 构造函数（同步加载）
        /// </summary>
        public LazyLoader(Func<T> loader, LazyLoadingConfig config = null)
        {
            _syncLoader = loader ?? throw new ArgumentNullException(nameof(loader));
            _config = config ?? new LazyLoadingConfig();
        }

        /// <summary>
        /// 构造函数（异步加载）
        /// </summary>
        public LazyLoader(Func<Task<T>> loader, LazyLoadingConfig config = null)
        {
            _asyncLoader = loader ?? throw new ArgumentNullException(nameof(loader));
            _config = config ?? new LazyLoadingConfig();
        }

        /// <summary>
        /// 是否已加载
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                lock (_lock)
                {
                    return _isLoaded && !IsExpired();
                }
            }
        }

        /// <summary>
        /// 获取值（如果未加载则触发加载）
        /// </summary>
        public T Value
        {
            get
            {
                lock (_lock)
                {
                    if (!_isLoaded || IsExpired())
                    {
                        LoadSync();
                    }
                    return _value;
                }
            }
        }

        /// <summary>
        /// 异步获取值
        /// </summary>
        public async Task<T> GetValueAsync()
        {
            Task<T> existingTask = null;
            bool needsLoad;

            lock (_lock)
            {
                needsLoad = !_isLoaded || IsExpired();
                
                // 如果正在加载，获取正在进行的任务
                if (_loadingTask != null && !_loadingTask.IsCompleted)
                {
                    existingTask = _loadingTask;
                }
            }

            if (existingTask != null)
            {
                return await existingTask;
            }

            if (needsLoad)
            {
                return await LoadAsync();
            }

            lock (_lock)
            {
                return _value;
            }
        }

        /// <summary>
        /// 强制重新加载
        /// </summary>
        public void Reload()
        {
            lock (_lock)
            {
                _isLoaded = false;
                _value = null;
                _loadingTask = null;
                LoadSync();
            }
        }

        /// <summary>
        /// 异步强制重新加载
        /// </summary>
        public async Task ReloadAsync()
        {
            lock (_lock)
            {
                _isLoaded = false;
                _value = null;
                _loadingTask = null;
            }

            await LoadAsync();
        }

        /// <summary>
        /// 清除已加载的值
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _isLoaded = false;
                _value = null;
                _loadingTask = null;
                _loadTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// 检查是否需要重新加载
        /// </summary>
        public bool ShouldReload()
        {
            lock (_lock)
            {
                return !_isLoaded || IsExpired();
            }
        }

        /// <summary>
        /// 同步加载
        /// </summary>
        private void LoadSync()
        {
            try
            {
                if (_syncLoader != null)
                {
                    Debug.WriteLine($"[LazyLoader] 开始同步加载 {typeof(T).Name}");
                    _value = _syncLoader();
                }
                else if (_asyncLoader != null)
                {
                    Debug.WriteLine($"[LazyLoader] 开始异步加载 {typeof(T).Name}（同步调用）");
                    _value = _asyncLoader().GetAwaiter().GetResult();
                }
                else
                {
                    throw new InvalidOperationException("没有可用的加载器");
                }

                _isLoaded = true;
                _loadTime = DateTime.Now;
                
                Debug.WriteLine($"[LazyLoader] 同步加载完成 {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LazyLoader] 同步加载失败 {typeof(T).Name}: {ex.Message}");
                _isLoaded = false;
                _value = null;
                throw;
            }
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        private async Task<T> LoadAsync()
        {
            Task<T> loadingTask;
            
            lock (_lock)
            {
                // 如果已经在加载，返回正在进行的任务
                if (_loadingTask != null && !_loadingTask.IsCompleted)
                {
                    loadingTask = _loadingTask;
                }
                else
                {
                    // 创建新的加载任务
                    _loadingTask = LoadAsyncInternal();
                    loadingTask = _loadingTask;
                }
            }

            return await loadingTask;
        }

        /// <summary>
        /// 内部异步加载实现
        /// </summary>
        private async Task<T> LoadAsyncInternal()
        {
            try
            {
                Debug.WriteLine($"[LazyLoader] 开始异步加载 {typeof(T).Name}");

                T result;
                if (_asyncLoader != null)
                {
                    if (_config.LoadTimeoutMs > 0)
                    {
                        using var cts = new CancellationTokenSource(_config.LoadTimeoutMs);
                        result = await _asyncLoader().ConfigureAwait(false);
                    }
                    else
                    {
                        result = await _asyncLoader().ConfigureAwait(false);
                    }
                }
                else if (_syncLoader != null)
                {
                    result = await Task.Run(_syncLoader).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("没有可用的加载器");
                }

                lock (_lock)
                {
                    _value = result;
                    _isLoaded = true;
                    _loadTime = DateTime.Now;
                }

                Debug.WriteLine($"[LazyLoader] 异步加载完成 {typeof(T).Name}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LazyLoader] 异步加载失败 {typeof(T).Name}: {ex.Message}");
                
                lock (_lock)
                {
                    _isLoaded = false;
                    _value = null;
                    _loadingTask = null;
                }
                
                throw;
            }
        }

        /// <summary>
        /// 检查缓存是否过期
        /// </summary>
        private bool IsExpired()
        {
            if (!_config.EnableCaching || _config.CacheExpirationMs <= 0)
            {
                return false;
            }

            return (DateTime.Now - _loadTime).TotalMilliseconds > _config.CacheExpirationMs;
        }
    }
}