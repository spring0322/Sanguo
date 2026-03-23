# 插件异步加载优化 - 需求文档

**功能名称：** 插件纹理资源异步加载与缓存优化  
**创建日期：** 2026-02-13  
**优先级：** 高（性能关键路径）

---

## 📋 问题陈述

当前游戏读档后，界面初始化耗时 5.9 秒，其中：
- `ArchitectureDetailPlugin` 耗时 2.5 秒（加载 100+ 个纹理）
- `InGameEditorPlugin` 耗时 3.2 秒（XML 解析 + 纹理加载）

**用户影响：**
- 读档后需要等待 6 秒才能进入游戏
- 用户体验差，感觉游戏"卡死"

---

## 🎯 目标

1. **主目标：** 将插件初始化耗时从 5.9 秒降低到 < 1 秒
2. **次要目标：** 优化内存占用，避免加载未使用的资源
3. **约束条件：** 不影响游戏功能，保持向后兼容

---

## 👥 用户故事

### US-1: 快速进入游戏
**作为** 玩家  
**我希望** 读档后能快速进入游戏（< 2 秒）  
**以便** 不用长时间等待加载

**验收标准：**
- [ ] 读档后界面初始化耗时 < 1 秒
- [ ] 首次打开建筑详情面板时，延迟 < 500ms（可接受）
- [ ] 后续打开面板无延迟（已缓存）

---

### US-2: 透明的加载状态
**作为** 玩家  
**我希望** 看到加载进度提示  
**以便** 知道游戏没有卡死

**验收标准：**
- [ ] 异步加载时显示"正在加载资源..."提示
- [ ] 加载完成后自动隐藏提示
- [ ] 加载失败时显示友好错误信息

---

### US-3: 内存优化
**作为** 开发者  
**我希望** 只加载必要的资源  
**以便** 减少内存占用和启动时间

**验收标准：**
- [ ] 未使用的插件不加载纹理资源
- [ ] 纹理资源支持按需卸载（可选）
- [ ] 缓存命中率 > 90%

---

## 🔧 技术需求

### TR-1: 异步纹理加载器
**描述：** 创建一个通用的异步纹理加载器，支持后台加载纹理资源。

**技术细节：**
- 使用 `Task<Texture2D>` 异步加载纹理
- 支持批量加载（一次加载多个纹理）
- 支持加载优先级（高优先级资源先加载）
- 线程安全（MonoGame 纹理必须在主线程创建）

**接口设计：**
```csharp
public interface IAsyncTextureLoader
{
    Task<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Texture2D>> LoadTexturesAsync(IEnumerable<string> paths, IProgress<float> progress = null);
    void CancelAll();
}
```

---

### TR-2: 智能纹理缓存
**描述：** 优化 `CacheManager.GetTempTexture`，避免重复加载。

**技术细节：**
- 检测重复加载（同一路径多次调用）
- 引用计数（跟踪纹理使用情况）
- LRU 缓存淘汰策略（可选）
- 线程安全的缓存访问

**优化点：**
```csharp
// ❌ 当前代码（可能重复加载）
texture1 = CacheManager.GetTempTexture("path/to/texture.png");
texture2 = CacheManager.GetTempTexture("path/to/texture.png"); // 重复加载？

// ✅ 优化后（保证缓存命中）
texture1 = CacheManager.GetTempTexture("path/to/texture.png"); // 加载
texture2 = CacheManager.GetTempTexture("path/to/texture.png"); // 缓存命中
```

---

### TR-3: 插件延迟初始化
**描述：** 将插件初始化拆分为"配置加载"和"资源加载"两个阶段。

**实现策略：**

#### 阶段 1：配置加载（同步，快速）
- 解析 XML 配置文件
- 初始化数据结构
- 注册事件处理器
- **耗时：** < 100ms

#### 阶段 2：资源加载（异步，延迟）
- 加载纹理资源
- 创建 UI 元素
- 预渲染缓存
- **触发时机：** 首次显示插件时

**代码结构：**
```csharp
public class ArchitectureDetailPlugin
{
    private bool _texturesLoaded = false;
    private Task _loadingTask;
    
    // 阶段 1：快速初始化（同步）
    public void SetGraphicsDevice()
    {
        LoadConfigOnly(); // 只加载配置，不加载纹理
    }
    
    // 阶段 2：资源加载（异步）
    public async Task EnsureTexturesLoadedAsync()
    {
        if (_texturesLoaded) return;
        
        _loadingTask ??= LoadTexturesAsync();
        await _loadingTask;
        _texturesLoaded = true;
    }
    
    // 显示时触发加载
    public void Show()
    {
        if (!_texturesLoaded)
        {
            // 显示加载提示
            ShowLoadingIndicator();
            
            // 异步加载纹理
            _ = EnsureTexturesLoadedAsync().ContinueWith(t => 
            {
                HideLoadingIndicator();
                RenderUI();
            });
        }
        else
        {
            RenderUI();
        }
    }
}
```

---

### TR-4: MonoGame 线程安全处理
**描述：** 处理 MonoGame 纹理加载的线程限制。

**技术挑战：**
- MonoGame 的 `Texture2D.FromStream()` 必须在主线程调用
- 后台线程只能做文件 I/O，不能创建纹理对象

**解决方案：**
```csharp
// 后台线程：读取文件数据
var imageData = await Task.Run(() => File.ReadAllBytes(path));

// 主线程：创建纹理对象
var texture = await MainThreadDispatcher.InvokeAsync(() => 
{
    using var stream = new MemoryStream(imageData);
    return Texture2D.FromStream(graphicsDevice, stream);
});
```

---

## 📊 性能指标

### 当前性能（基线）
```
插件初始化总耗时: 5932 ms
├─ ArchitectureDetailPlugin: 2526 ms
├─ InGameEditorPlugin:       3181 ms
└─ 其他插件:                  225 ms
```

### 目标性能
```
插件初始化总耗时: < 1000 ms
├─ ArchitectureDetailPlugin: < 100 ms (配置加载)
├─ InGameEditorPlugin:       < 100 ms (配置加载)
└─ 其他插件:                  < 800 ms

首次打开面板延迟: < 500 ms (异步加载纹理)
后续打开面板延迟: 0 ms (已缓存)
```

---

## 🚧 实现阶段

### 阶段 1：缓存优化（快速见效）
**预计耗时：** 1 小时  
**预期收益：** 减少 20-30% 耗时（如果有重复加载）

**任务：**
1. 在 `CacheManager.GetTempTexture` 中添加性能监控
2. 检测重复加载的纹理路径
3. 优化缓存逻辑（如果需要）

---

### 阶段 2：异步加载框架（核心功能）
**预计耗时：** 4 小时  
**预期收益：** 减少 80% 初始化耗时

**任务：**
1. 创建 `AsyncTextureLoader` 类
2. 实现 `MainThreadDispatcher`（主线程调度器）
3. 编写单元测试

---

### 阶段 3：插件改造（应用优化）
**预计耗时：** 3 小时  
**预期收益：** 完成端到端优化

**任务：**
1. 改造 `ArchitectureDetailPlugin`
2. 改造 `InGameEditorPlugin`
3. 添加加载进度提示 UI

---

### 阶段 4：测试与验证
**预计耗时：** 2 小时

**任务：**
1. 性能基准测试
2. 内存泄漏检测
3. 边界条件测试（网络延迟、磁盘慢速等）

---

## ⚠️ 风险与缓解

### 风险 1：MonoGame 线程限制
**描述：** MonoGame 纹理创建必须在主线程，可能导致异步加载复杂化。

**缓解措施：**
- 使用主线程调度器模式
- 后台线程只做文件 I/O
- 参考 MonoGame 官方异步加载示例

---

### 风险 2：首次打开面板延迟
**描述：** 异步加载导致首次打开面板时有短暂延迟。

**缓解措施：**
- 显示"正在加载..."提示
- 预加载高频使用的纹理
- 使用渐进式加载（先显示框架，再加载图片）

---

### 风险 3：缓存失效
**描述：** 缓存逻辑错误可能导致纹理显示异常。

**缓解措施：**
- 完善的单元测试
- 添加缓存版本号（支持热更新）
- 提供手动清除缓存的接口

---

## 📝 验收标准

### 功能验收
- [ ] 读档后界面初始化耗时 < 1 秒
- [ ] 首次打开建筑详情面板延迟 < 500ms
- [ ] 后续打开面板无延迟
- [ ] 所有现有功能正常工作（回归测试）

### 性能验收
- [ ] 插件初始化耗时减少 > 80%
- [ ] 内存占用不增加（或减少）
- [ ] 缓存命中率 > 90%

### 代码质量验收
- [ ] 符合 C# 12 语法规范
- [ ] 无 LINQ 在 Hot Path
- [ ] 无防御性空检查掩盖问题
- [ ] 通过所有单元测试

---

## 📚 参考资料

1. **MonoGame 异步加载：** https://docs.monogame.net/articles/content/custom_effects.html
2. **C# Task 最佳实践：** https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/
3. **纹理缓存策略：** LRU Cache Implementation in C#

---

## 🔄 后续优化（可选）

1. **纹理压缩：** 使用 DXT 压缩减少内存占用
2. **纹理图集：** 将小纹理合并为图集，减少 Draw Call
3. **按需卸载：** 长时间未使用的纹理自动卸载
4. **预加载策略：** 根据用户行为预测，提前加载可能使用的资源
