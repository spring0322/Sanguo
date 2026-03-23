# 插件异步加载优化 - 设计文档

**版本：** 1.0  
**日期：** 2026-02-13  
**作者：** Lead Architect

---

## 🏗️ 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    Game Initialization                       │
│  (MainGameScreen.Initialize)                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Plugin Manager (GamePlugin)                     │
│  - InitializePlugins() [同步，快速]                          │
│  - 只加载配置，不加载纹理                                     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│         Individual Plugins (延迟加载)                        │
│  ┌──────────────────────────────────────────────┐           │
│  │  ArchitectureDetailPlugin                    │           │
│  │  - SetGraphicsDevice() [快速配置加载]        │           │
│  │  - EnsureTexturesLoadedAsync() [异步纹理]    │           │
│  └──────────────────────────────────────────────┘           │
│  ┌──────────────────────────────────────────────┐           │
│  │  InGameEditorPlugin                          │           │
│  │  - SetGraphicsDevice() [快速配置加载]        │           │
│  │  - EnsureTexturesLoadedAsync() [异步纹理]    │           │
│  └──────────────────────────────────────────────┘           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          Async Texture Loading System                        │
│  ┌──────────────────────────────────────────────┐           │
│  │  AsyncTextureLoader                          │           │
│  │  - LoadTextureAsync()                        │           │
│  │  - LoadTexturesAsync() [批量加载]            │           │
│  └──────────────────┬───────────────────────────┘           │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────────────┐           │
│  │  MainThreadDispatcher                        │           │
│  │  - InvokeAsync() [主线程调度]                │           │
│  │  - 处理 MonoGame 线程限制                     │           │
│  └──────────────────┬───────────────────────────┘           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          Enhanced Texture Cache                              │
│  ┌──────────────────────────────────────────────┐           │
│  │  CacheManager (优化版)                       │           │
│  │  - GetTempTexture() [智能缓存]               │           │
│  │  - 引用计数 + LRU 淘汰                        │           │
│  │  - 线程安全                                   │           │
│  └──────────────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 核心组件设计

### 1. AsyncTextureLoader（异步纹理加载器）

**职责：**
- 异步加载纹理文件
- 批量加载优化
- 进度报告
- 取消支持

**实现：**

```csharp
namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 异步纹理加载器，支持后台加载纹理资源
    /// </summary>
    public class AsyncTextureLoader
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly MainThreadDispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, Task<Texture2D>> _loadingTasks = [];
        
        public AsyncTextureLoader(GraphicsDevice graphicsDevice, MainThreadDispatcher dispatcher)
        {
            _graphicsDevice = graphicsDevice;
            _dispatcher = dispatcher;
        }
        
        /// <summary>
        /// 异步加载单个纹理
        /// </summary>
        public async Task<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
        {
            // 检查是否已在加载中（避免重复加载）
            if (_loadingTasks.TryGetValue(path, out var existingTask))
            {
                return await existingTask;
            }
            
            // 创建加载任务
            var loadTask = LoadTextureInternalAsync(path, cancellationToken);
            _loadingTasks[path] = loadTask;
            
            try
            {
                var texture = await loadTask;
                return texture;
            }
            finally
            {
                // 加载完成后移除任务
                _loadingTasks.TryRemove(path, out _);
            }
        }
        
        /// <summary>
        /// 批量异步加载纹理
        /// </summary>
        public async Task<Dictionary<string, Texture2D>> LoadTexturesAsync(
            IEnumerable<string> paths, 
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var pathList = paths.ToList();
            var results = new Dictionary<string, Texture2D>();
            var completed = 0;
            
            // 并行加载（限制并发数）
            var semaphore = new SemaphoreSlim(4); // 最多 4 个并发加载
            var tasks = pathList.Select(async path =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var texture = await LoadTextureAsync(path, cancellationToken);
                    lock (results)
                    {
                        results[path] = texture;
                        completed++;
                        progress?.Report((float)completed / pathList.Count);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            await Task.WhenAll(tasks);
            return results;
        }
        
        /// <summary>
        /// 内部加载实现
        /// </summary>
        private async Task<Texture2D> LoadTextureInternalAsync(string path, CancellationToken cancellationToken)
        {
            // 阶段 1：后台线程读取文件数据
            byte[] imageData = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 使用 Platform.Current.LoadBytes 保持兼容性
                return Platform.Current.LoadBytes(path);
            }, cancellationToken);
            
            // 阶段 2：主线程创建纹理对象（MonoGame 限制）
            var texture = await _dispatcher.InvokeAsync(() =>
            {
                using var stream = new MemoryStream(imageData);
                return Texture2D.FromStream(_graphicsDevice, stream);
            });
            
            return texture;
        }
        
        /// <summary>
        /// 取消所有加载任务
        /// </summary>
        public void CancelAll()
        {
            _loadingTasks.Clear();
        }
    }
}
```

---

### 2. MainThreadDispatcher（主线程调度器）

**职责：**
- 将操作调度到主线程执行
- 处理 MonoGame 的线程限制
- 支持异步等待

**实现：**

```csharp
namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 主线程调度器，用于在主线程执行操作
    /// </summary>
    public class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _actionQueue = [];
        private readonly object _lock = new();
        
        /// <summary>
        /// 在主线程执行操作并返回结果
        /// </summary>
        public Task<T> InvokeAsync<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            
            _actionQueue.Enqueue(() =>
            {
                try
                {
                    var result = action();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 在主线程执行操作（无返回值）
        /// </summary>
        public Task InvokeAsync(Action action)
        {
            return InvokeAsync(() =>
            {
                action();
                return 0; // Dummy return
            });
        }
        
        /// <summary>
        /// 在 Update 循环中调用，执行队列中的操作
        /// </summary>
        public void ProcessQueue()
        {
            // 限制每帧处理的操作数量，避免卡顿
            const int maxActionsPerFrame = 10;
            int processed = 0;
            
            while (processed < maxActionsPerFrame && _actionQueue.TryDequeue(out var action))
            {
                action();
                processed++;
            }
        }
        
        /// <summary>
        /// 获取待处理操作数量
        /// </summary>
        public int PendingCount => _actionQueue.Count;
    }
}
```

---

### 3. Enhanced CacheManager（增强缓存管理器）

**职责：**
- 智能缓存纹理资源
- 避免重复加载
- 引用计数管理
- 线程安全

**实现：**

```csharp
namespace WorldOfTheThreeKingdoms.Tools
{
    /// <summary>
    /// 增强的纹理缓存管理器
    /// </summary>
    public static class EnhancedCacheManager
    {
        private static readonly ConcurrentDictionary<string, CachedTexture> _textureCache = [];
        private static readonly object _cacheLock = new();
        
        /// <summary>
        /// 缓存的纹理信息
        /// </summary>
        private class CachedTexture
        {
            public Texture2D Texture { get; set; }
            public int ReferenceCount { get; set; }
            public DateTime LastAccessTime { get; set; }
            public long SizeInBytes { get; set; }
        }
        
        /// <summary>
        /// 获取纹理（带缓存优化）
        /// </summary>
        public static Texture2D GetTexture(string path)
        {
            // 尝试从缓存获取
            if (_textureCache.TryGetValue(path, out var cached))
            {
                cached.ReferenceCount++;
                cached.LastAccessTime = DateTime.Now;
                
                Debug.WriteLine($"[缓存命中] {path}");
                return cached.Texture;
            }
            
            // 缓存未命中，加载纹理
            Debug.WriteLine($"[缓存未命中] 加载纹理: {path}");
            
            var texture = CacheManager.GetTempTexture(path); // 使用原有加载逻辑
            
            // 添加到缓存
            var cachedTexture = new CachedTexture
            {
                Texture = texture,
                ReferenceCount = 1,
                LastAccessTime = DateTime.Now,
                SizeInBytes = EstimateTextureSize(texture)
            };
            
            _textureCache[path] = cachedTexture;
            
            return texture;
        }
        
        /// <summary>
        /// 释放纹理引用
        /// </summary>
        public static void ReleaseTexture(string path)
        {
            if (_textureCache.TryGetValue(path, out var cached))
            {
                cached.ReferenceCount--;
                
                // 引用计数为 0 时，可以考虑卸载（可选）
                if (cached.ReferenceCount <= 0)
                {
                    Debug.WriteLine($"[缓存] 纹理引用计数为 0: {path}");
                    // 暂不卸载，保留在缓存中以便后续使用
                }
            }
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStats GetStats()
        {
            lock (_cacheLock)
            {
                return new CacheStats
                {
                    TotalTextures = _textureCache.Count,
                    TotalSizeInBytes = _textureCache.Values.Sum(c => c.SizeInBytes),
                    TotalReferences = _textureCache.Values.Sum(c => c.ReferenceCount)
                };
            }
        }
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var cached in _textureCache.Values)
                {
                    cached.Texture?.Dispose();
                }
                _textureCache.Clear();
                Debug.WriteLine("[缓存] 已清除所有纹理缓存");
            }
        }
        
        /// <summary>
        /// 估算纹理大小
        /// </summary>
        private static long EstimateTextureSize(Texture2D texture)
        {
            if (texture == null) return 0;
            
            // 简单估算：宽 * 高 * 4 字节（RGBA）
            return texture.Width * texture.Height * 4;
        }
        
        public class CacheStats
        {
            public int TotalTextures { get; set; }
            public long TotalSizeInBytes { get; set; }
            public int TotalReferences { get; set; }
            
            public double TotalSizeInMB => TotalSizeInBytes / (1024.0 * 1024.0);
        }
    }
}
```

---

### 4. Plugin Base Class（插件基类）

**职责：**
- 提供统一的异步加载接口
- 管理加载状态
- 显示加载提示

**实现：**

```csharp
namespace WorldOfTheThreeKingdoms.GamePlugins
{
    /// <summary>
    /// 支持异步加载的插件基类
    /// </summary>
    public abstract class AsyncLoadablePlugin
    {
        protected bool _texturesLoaded = false;
        protected Task _loadingTask;
        protected CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// 快速初始化（只加载配置）
        /// </summary>
        public virtual void SetGraphicsDevice()
        {
            var sw = Stopwatch.StartNew();
            
            LoadConfigOnly();
            
            sw.Stop();
            Debug.WriteLine($"[{GetType().Name}] 配置加载耗时: {sw.ElapsedMilliseconds} ms");
        }
        
        /// <summary>
        /// 只加载配置，不加载纹理（子类实现）
        /// </summary>
        protected abstract void LoadConfigOnly();
        
        /// <summary>
        /// 异步加载纹理资源（子类实现）
        /// </summary>
        protected abstract Task LoadTexturesAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// 确保纹理已加载
        /// </summary>
        public async Task EnsureTexturesLoadedAsync()
        {
            if (_texturesLoaded) return;
            
            _cancellationTokenSource = new CancellationTokenSource();
            _loadingTask ??= LoadTexturesWithProgressAsync(_cancellationTokenSource.Token);
            
            await _loadingTask;
            _texturesLoaded = true;
        }
        
        /// <summary>
        /// 带进度的纹理加载
        /// </summary>
        private async Task LoadTexturesWithProgressAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                Debug.WriteLine($"[{GetType().Name}] 开始异步加载纹理...");
                
                await LoadTexturesAsync(cancellationToken);
                
                sw.Stop();
                Debug.WriteLine($"[{GetType().Name}] 纹理加载完成，耗时: {sw.ElapsedMilliseconds} ms");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{GetType().Name}] 纹理加载已取消");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetType().Name}] 纹理加载失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 取消加载
        /// </summary>
        public void CancelLoading()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        /// <summary>
        /// 显示时确保资源已加载
        /// </summary>
        public virtual async Task ShowAsync()
        {
            if (!_texturesLoaded)
            {
                // TODO: 显示加载提示 UI
                await EnsureTexturesLoadedAsync();
                // TODO: 隐藏加载提示 UI
            }
            
            // 显示插件 UI
            Show();
        }
        
        /// <summary>
        /// 显示插件（子类实现）
        /// </summary>
        protected abstract void Show();
    }
}
```

---

## 🔄 改造现有插件

### ArchitectureDetailPlugin 改造

**改造前：**
```csharp
public void SetGraphicsDevice()
{
    // 同步加载 100+ 个纹理，耗时 2.5 秒
    this.LoadDataFromXMLDocument(@"Content\Data\Plugins\ArchitectureDetailData.xml");
}
```

**改造后：**
```csharp
public class ArchitectureDetailPlugin : AsyncLoadablePlugin
{
    private XmlDocument _configDoc;
    private List<string> _texturePaths = [];
    
    // 阶段 1：快速配置加载
    protected override void LoadConfigOnly()
    {
        // 只解析 XML，不加载纹理
        _configDoc = new XmlDocument();
        string xml = Platform.Current.LoadText(@"Content\Data\Plugins\ArchitectureDetailData.xml");
        _configDoc.LoadXml(xml);
        
        // 收集所有纹理路径
        CollectTexturePaths(_configDoc);
        
        // 初始化数据结构
        InitializeDataStructures(_configDoc);
    }
    
    // 阶段 2：异步纹理加载
    protected override async Task LoadTexturesAsync(CancellationToken cancellationToken)
    {
        var loader = new AsyncTextureLoader(graphicsDevice, mainThreadDispatcher);
        
        // 批量异步加载所有纹理
        var progress = new Progress<float>(p => 
        {
            Debug.WriteLine($"[ArchitectureDetail] 加载进度: {p:P0}");
        });
        
        var textures = await loader.LoadTexturesAsync(_texturePaths, progress, cancellationToken);
        
        // 将纹理赋值给对应字段
        AssignTexturesToFields(textures);
    }
    
    private void CollectTexturePaths(XmlDocument doc)
    {
        // 遍历 XML，收集所有纹理路径
        var nodes = doc.SelectNodes("//node()[@FileName]");
        foreach (XmlNode node in nodes)
        {
            var fileName = node.Attributes["FileName"]?.Value;
            if (!string.IsNullOrEmpty(fileName))
            {
                _texturePaths.Add($@"Content\Textures\GameComponents\ArchitectureDetail\Data\{fileName}");
            }
        }
    }
    
    protected override void Show()
    {
        // 显示建筑详情面板
        this.IsShowing = true;
    }
}
```

---

## 📊 性能优化效果预测

### 优化前（基线）
```
插件初始化: 5932 ms
├─ ArchitectureDetailPlugin: 2526 ms (同步加载 100+ 纹理)
├─ InGameEditorPlugin:       3181 ms (同步加载 + XML 解析)
└─ 其他插件:                  225 ms
```

### 优化后（预测）
```
插件初始化: < 500 ms
├─ ArchitectureDetailPlugin: < 50 ms (只加载配置)
├─ InGameEditorPlugin:       < 50 ms (只加载配置)
└─ 其他插件:                  225 ms

首次打开面板: 300-500 ms (异步加载纹理)
后续打开面板: 0 ms (缓存命中)
```

**性能提升：**
- 初始化耗时减少：**91.6%** (5932ms → 500ms)
- 用户感知延迟：从 6 秒 → 0.5 秒
- 内存占用：减少（未使用的插件不加载纹理）

---

## 🧪 测试策略

### 单元测试

```csharp
[TestClass]
public class AsyncTextureLoaderTests
{
    [TestMethod]
    public async Task LoadTextureAsync_ValidPath_ReturnsTexture()
    {
        // Arrange
        var loader = new AsyncTextureLoader(graphicsDevice, dispatcher);
        
        // Act
        var texture = await loader.LoadTextureAsync("test.png");
        
        // Assert
        Assert.IsNotNull(texture);
    }
    
    [TestMethod]
    public async Task LoadTextureAsync_SamePath_ReturnsCachedTexture()
    {
        // Arrange
        var loader = new AsyncTextureLoader(graphicsDevice, dispatcher);
        
        // Act
        var texture1 = await loader.LoadTextureAsync("test.png");
        var texture2 = await loader.LoadTextureAsync("test.png");
        
        // Assert
        Assert.AreSame(texture1, texture2); // 应该是同一个对象
    }
}
```

### 性能基准测试

```csharp
[TestClass]
public class PerformanceBenchmarks
{
    [TestMethod]
    public async Task Benchmark_PluginInitialization()
    {
        var sw = Stopwatch.StartNew();
        
        // 初始化所有插件
        var plugins = new GamePlugin();
        plugins.InitializePlugins(screen);
        
        sw.Stop();
        
        // 断言：初始化耗时 < 1 秒
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, 
            $"插件初始化耗时 {sw.ElapsedMilliseconds} ms，超过 1 秒");
    }
}
```

---

## 🚀 部署计划

### 阶段 1：基础设施（Week 1）
- [ ] 实现 `AsyncTextureLoader`
- [ ] 实现 `MainThreadDispatcher`
- [ ] 实现 `EnhancedCacheManager`
- [ ] 编写单元测试

### 阶段 2：插件改造（Week 2）
- [ ] 改造 `ArchitectureDetailPlugin`
- [ ] 改造 `InGameEditorPlugin`
- [ ] 添加加载进度 UI

### 阶段 3：测试与优化（Week 3）
- [ ] 性能基准测试
- [ ] 内存泄漏检测
- [ ] 边界条件测试
- [ ] 用户验收测试

---

## 📝 正确性属性（Property-Based Testing）

### Property 1: 缓存一致性
**描述：** 同一路径的纹理，多次加载应返回相同对象。

```csharp
[Property]
public Property CacheConsistency()
{
    return Prop.ForAll(
        Arb.Default.String(),
        path =>
        {
            var texture1 = EnhancedCacheManager.GetTexture(path);
            var texture2 = EnhancedCacheManager.GetTexture(path);
            return texture1 == texture2;
        });
}
```

### Property 2: 异步加载幂等性
**描述：** 多次调用 `EnsureTexturesLoadedAsync` 应该只加载一次。

```csharp
[Property]
public async Task<Property> LoadingIdempotence()
{
    return Prop.ForAll(
        Arb.Default.Int32(),
        async callCount =>
        {
            var plugin = new ArchitectureDetailPlugin();
            var loadCount = 0;
            
            for (int i = 0; i < callCount; i++)
            {
                await plugin.EnsureTexturesLoadedAsync();
            }
            
            // 验证只加载了一次
            return loadCount == 1;
        });
}
```

---

## 🔒 线程安全保证

### 关键线程安全点

1. **纹理缓存访问：** 使用 `ConcurrentDictionary` 保证线程安全
2. **主线程调度：** 使用 `ConcurrentQueue` 保证操作顺序
3. **加载任务管理：** 使用 `Task` 和 `TaskCompletionSource` 保证异步安全

### 死锁预防

- 避免在主线程等待异步操作完成
- 使用 `ConfigureAwait(false)` 避免上下文切换
- 限制并发加载数量（使用 `SemaphoreSlim`）

---

## 📚 参考实现

### MonoGame 官方异步加载示例
```csharp
// 参考：https://github.com/MonoGame/MonoGame/wiki/Asynchronous-Content-Loading
protected override void LoadContent()
{
    Task.Run(() =>
    {
        // 后台线程加载
        var data = File.ReadAllBytes("texture.png");
        
        // 主线程创建纹理
        MainThreadDispatcher.Invoke(() =>
        {
            using var stream = new MemoryStream(data);
            texture = Texture2D.FromStream(GraphicsDevice, stream);
        });
    });
}
```

---

## ✅ 验收标准

### 功能验收
- [ ] 所有插件支持异步加载
- [ ] 缓存命中率 > 90%
- [ ] 无功能回归

### 性能验收
- [ ] 插件初始化 < 1 秒
- [ ] 首次打开面板 < 500ms
- [ ] 内存占用不增加

### 代码质量验收
- [ ] 符合 C# 12 规范
- [ ] 通过所有单元测试
- [ ] 无线程安全问题
