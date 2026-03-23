# 插件异步加载优化 - 任务列表

**版本：** 1.0  
**创建日期：** 2026-02-13

---

## 📋 任务概览

- **总任务数：** 25
- **预计工时：** 40 小时
- **优先级：** 高

---

## 🏗️ 阶段 1：基础设施搭建（10 小时）

### 1.1 创建 AsyncTextureLoader 类
**描述：** 实现异步纹理加载器，支持单个和批量加载。

**验收标准：**
- [x] 实现 `LoadTextureAsync(string path)` 方法
- [x] 实现 `LoadTexturesAsync(IEnumerable<string> paths)` 方法
- [x] 支持进度报告（`IProgress<float>`）
- [x] 支持取消操作（`CancellationToken`）
- [x] 避免重复加载（同一路径多次调用）

**文件：** `WorldOfTheThreeKingdoms/GameManager/AsyncTextureLoader.cs`

**状态：** ✅ 已完成

---

### 1.2 创建 MainThreadDispatcher 类
**描述：** 实现主线程调度器，处理 MonoGame 线程限制。

**验收标准：**
- [ ] 实现 `InvokeAsync<T>(Func<T> action)` 方法
- [ ] 实现 `InvokeAsync(Action action)` 方法
- [ ] 实现 `ProcessQueue()` 方法（在 Update 中调用）
- [ ] 线程安全（使用 `ConcurrentQueue`）
- [ ] 限制每帧处理数量（避免卡顿）

**文件：** `WorldOfTheThreeKingdoms/GameManager/MainThreadDispatcher.cs`

---

### 1.3 集成 MainThreadDispatcher 到游戏循环
**描述：** 在 `MainGameScreen.Update()` 中调用 `ProcessQueue()`。

**验收标准：**
- [ ] 在 `MainGameScreen` 中创建 `MainThreadDispatcher` 实例
- [ ] 在 `Update()` 方法中调用 `ProcessQueue()`
- [ ] 确保每帧都处理队列
- [ ] 添加性能监控（队列长度）

**文件：** `WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs`

---

### 1.4 优化 CacheManager
**描述：** 增强纹理缓存，添加引用计数和统计功能。

**验收标准：**
- [ ] 创建 `EnhancedCacheManager` 类
- [ ] 实现 `GetTexture(string path)` 方法（带缓存检测）
- [ ] 实现 `ReleaseTexture(string path)` 方法（引用计数）
- [ ] 实现 `GetStats()` 方法（缓存统计）
- [ ] 添加缓存命中/未命中日志

**文件：** `WorldOfTheThreeKingdoms/Tools/EnhancedCacheManager.cs`

---

### 1.5 创建插件基类 AsyncLoadablePlugin
**描述：** 提供统一的异步加载接口。

**验收标准：**
- [ ] 实现 `LoadConfigOnly()` 抽象方法
- [ ] 实现 `LoadTexturesAsync()` 抽象方法
- [ ] 实现 `EnsureTexturesLoadedAsync()` 方法
- [ ] 实现 `ShowAsync()` 方法
- [ ] 添加加载状态管理

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/AsyncLoadablePlugin.cs`

---

## 🔧 阶段 2：插件改造（15 小时）

### 2.1 改造 ArchitectureDetailPlugin - 配置加载
**描述：** 将 `LoadDataFromXMLDocument` 拆分为配置加载和纹理加载。

**验收标准：**
- [x] 实现 `LoadConfigOnly()` 方法（只解析 XML）
- [x] 实现 `CollectTexturePaths()` 方法（收集纹理路径）
- [x] 实现 `InitializeDataStructures()` 方法（初始化数据）
- [x] 移除 `SetGraphicsDevice()` 中的纹理加载代码
- [x] 配置加载耗时 < 100ms

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/ArchitectureDetail/ArchitectureDetailPlugin.cs`

**状态：** ✅ 已完成

---

### 2.2 改造 ArchitectureDetailPlugin - 纹理加载
**描述：** 实现异步纹理加载逻辑。

**验收标准：**
- [x] 实现 `LoadTexturesAsync()` 方法
- [x] 使用 `AsyncTextureLoader` 批量加载纹理
- [x] 实现 `AssignTexturesToFields()` 方法（赋值纹理）
- [x] 添加加载进度日志
- [x] 支持取消加载
- [x] **使用 Source Generator 生成赋值代码（符合 AOT 规范）**

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/ArchitectureDetail/ArchitectureDetailPlugin.cs`

**状态：** ✅ 已完成

**备注：** 使用 Source Generator 自动生成 368 个纹理字段的赋值代码，完全符合 AOT 规范，无反射代码。

---

### 2.3 改造 ArchitectureDetailPlugin - 显示逻辑
**描述：** 修改显示逻辑，确保纹理已加载。

**验收标准：**
- [x] 修改 `Show()` 方法，调用 `EnsureTexturesLoadedAsync()`
- [x] 首次显示时异步加载纹理
- [x] 后续显示直接使用缓存
- [ ] 添加加载提示 UI（可选）

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/ArchitectureDetail/ArchitectureDetailPlugin.cs`

**状态：** ✅ 已完成（加载提示 UI 为可选项）

---

### 2.4 改造 InGameEditorPlugin - 配置加载
**描述：** 优化 `LoadDataFromXMLDocument`，拆分配置和纹理加载。

**验收标准：**
- [ ] 实现 `LoadConfigOnly()` 方法
- [ ] 分析 `editorFrame.LoadFromXML()` 的耗时
- [ ] 优化 XML 解析逻辑（如果需要）
- [ ] 配置加载耗时 < 100ms

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/InGameEditorPlugin/InGameEditorPlugin.cs`

---

### 2.5 改造 InGameEditorPlugin - 纹理加载
**描述：** 实现异步纹理加载（如果有纹理）。

**验收标准：**
- [ ] 检查 `InGameEditorPlugin` 是否加载纹理
- [ ] 如果有，实现 `LoadTexturesAsync()` 方法
- [ ] 如果没有，跳过此任务
- [ ] 添加性能监控

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/InGameEditorPlugin/InGameEditorPlugin.cs`

---

### 2.6 诊断 InGameEditorPlugin 的 3.2 秒耗时
**描述：** 深入分析 `InGameEditorPlugin` 的耗时原因。

**验收标准：**
- [x] 在 `editorFrame.LoadFromXML()` 内部添加性能监控
- [x] 找出具体耗时操作（XML 解析？反射？）
- [x] 生成诊断报告
- [x] 提出优化方案

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/InGameEditorPlugin/EditorFrame.cs`

---

## 🧪 阶段 3：测试与验证（8 小时）

### 3.1 编写 AsyncTextureLoader 单元测试
**描述：** 测试异步加载器的核心功能。

**验收标准：**
- [ ] 测试单个纹理加载
- [ ] 测试批量纹理加载
- [ ] 测试重复加载（应返回缓存）
- [ ] 测试取消操作
- [ ] 测试错误处理

**文件：** `WorldOfTheThreeKingdoms.Tests/AsyncTextureLoaderTests.cs`

---

### 3.2 编写 MainThreadDispatcher 单元测试
**描述：** 测试主线程调度器的正确性。

**验收标准：**
- [ ] 测试同步调用
- [ ] 测试异步等待
- [ ] 测试队列处理
- [ ] 测试并发安全
- [ ] 测试异常处理

**文件：** `WorldOfTheThreeKingdoms.Tests/MainThreadDispatcherTests.cs`

---

### 3.3 编写 EnhancedCacheManager 单元测试
**描述：** 测试缓存管理器的功能。

**验收标准：**
- [ ] 测试缓存命中
- [ ] 测试缓存未命中
- [ ] 测试引用计数
- [ ] 测试统计信息
- [ ] 测试清除缓存

**文件：** `WorldOfTheThreeKingdoms.Tests/EnhancedCacheManagerTests.cs`

---

### 3.4 性能基准测试
**描述：** 测试优化后的性能指标。

**验收标准：**
- [ ] 测试插件初始化耗时（应 < 1 秒）
- [ ] 测试首次打开面板耗时（应 < 500ms）
- [ ] 测试后续打开面板耗时（应 < 10ms）
- [ ] 测试内存占用（不应增加）
- [ ] 生成性能对比报告

**文件：** `WorldOfTheThreeKingdoms.Tests/PerformanceBenchmarks.cs`

---

### 3.5 集成测试
**描述：** 端到端测试完整流程。

**验收标准：**
- [ ] 测试读档 → 初始化 → 打开面板流程
- [ ] 测试多次打开/关闭面板
- [ ] 测试取消加载
- [ ] 测试异常情况（文件不存在等）
- [ ] 测试多线程并发

**文件：** `WorldOfTheThreeKingdoms.Tests/IntegrationTests.cs`

---

## 🎨 阶段 4：UI 优化（5 小时）

### 4.1 创建加载提示 UI
**描述：** 显示"正在加载资源..."提示。

**验收标准：**
- [ ] 创建 `LoadingIndicator` 类
- [ ] 实现 `Show()` 和 `Hide()` 方法
- [ ] 支持进度条显示（可选）
- [ ] 支持取消按钮（可选）
- [ ] 样式美观

**文件：** `WorldOfTheThreeKingdoms/GameScreens/LoadingIndicator.cs`

---

### 4.2 集成加载提示到插件
**描述：** 在插件异步加载时显示提示。

**验收标准：**
- [ ] 在 `AsyncLoadablePlugin.ShowAsync()` 中显示提示
- [ ] 加载完成后自动隐藏
- [ ] 加载失败时显示错误信息
- [ ] 不阻塞主线程

**文件：** `WorldOfTheThreeKingdoms/GamePlugins/AsyncLoadablePlugin.cs`

---

### 4.3 添加缓存统计 UI（可选）
**描述：** 在调试菜单中显示缓存统计信息。

**验收标准：**
- [ ] 显示缓存纹理数量
- [ ] 显示缓存总大小
- [ ] 显示缓存命中率
- [ ] 提供清除缓存按钮

**文件：** `WorldOfTheThreeKingdoms/GameScreens/DebugMenu.cs`

---

## 📊 阶段 5：监控与优化（2 小时）

### 5.1 添加性能监控日志
**描述：** 在关键路径添加性能监控。

**验收标准：**
- [ ] 记录每个插件的配置加载耗时
- [ ] 记录每个插件的纹理加载耗时
- [ ] 记录缓存命中/未命中次数
- [ ] 记录主线程队列长度
- [ ] 生成性能报告

**文件：** 各插件文件

---

### 5.2 优化并发加载策略
**描述：** 调整并发加载数量，平衡速度和资源占用。

**验收标准：**
- [ ] 测试不同并发数（2, 4, 8）的性能
- [ ] 选择最优并发数
- [ ] 添加配置选项（可选）
- [ ] 文档化最佳实践

**文件：** `WorldOfTheThreeKingdoms/GameManager/AsyncTextureLoader.cs`

---

### 5.3 内存泄漏检测
**描述：** 检测并修复潜在的内存泄漏。

**验收标准：**
- [ ] 使用内存分析工具（如 dotMemory）
- [ ] 检测纹理是否正确释放
- [ ] 检测事件订阅是否正确取消
- [ ] 修复发现的内存泄漏
- [ ] 生成内存分析报告

**工具：** JetBrains dotMemory / Visual Studio Profiler

---

## 📝 阶段 6：文档与部署（2 小时）

### 6.1 编写技术文档
**描述：** 文档化异步加载系统的使用方法。

**验收标准：**
- [ ] 编写 API 文档
- [ ] 编写使用示例
- [ ] 编写最佳实践指南
- [ ] 编写故障排查指南

**文件：** `docs/AsyncLoadingGuide.md`

---

### 6.2 更新插件开发指南
**描述：** 更新插件开发文档，说明如何使用异步加载。

**验收标准：**
- [ ] 更新插件基类说明
- [ ] 添加异步加载示例
- [ ] 说明线程安全注意事项
- [ ] 添加常见问题解答

**文件：** `docs/PluginDevelopmentGuide.md`

---

### 6.3 生成性能对比报告
**描述：** 对比优化前后的性能数据。

**验收标准：**
- [ ] 记录优化前的性能数据
- [ ] 记录优化后的性能数据
- [ ] 生成对比图表
- [ ] 计算性能提升百分比
- [ ] 生成最终报告

**文件：** `2026-02-13_插件异步加载性能对比报告.md`

---

## ✅ 验收标准（总体）

### 功能验收
- [ ] 所有插件支持异步加载
- [ ] 读档后界面初始化 < 1 秒
- [ ] 首次打开面板 < 500ms
- [ ] 后续打开面板无延迟
- [ ] 无功能回归

### 性能验收
- [ ] 插件初始化耗时减少 > 80%
- [ ] 缓存命中率 > 90%
- [ ] 内存占用不增加
- [ ] 无性能回归

### 代码质量验收
- [ ] 符合 C# 12 语法规范
- [ ] 无防御性空检查掩盖问题
- [ ] 通过所有单元测试
- [ ] 代码覆盖率 > 80%
- [ ] 无线程安全问题

---

## 📅 时间计划

| 阶段 | 任务数 | 预计工时 | 开始日期 | 结束日期 |
|------|--------|----------|----------|----------|
| 阶段 1：基础设施 | 5 | 10h | Week 1 Day 1 | Week 1 Day 2 |
| 阶段 2：插件改造 | 6 | 15h | Week 1 Day 3 | Week 2 Day 2 |
| 阶段 3：测试验证 | 5 | 8h | Week 2 Day 3 | Week 2 Day 4 |
| 阶段 4：UI 优化 | 3 | 5h | Week 2 Day 5 | Week 3 Day 1 |
| 阶段 5：监控优化 | 3 | 2h | Week 3 Day 2 | Week 3 Day 2 |
| 阶段 6：文档部署 | 3 | 2h | Week 3 Day 3 | Week 3 Day 3 |
| **总计** | **25** | **42h** | - | - |

---

## 🚀 下一步行动

**立即开始：** 阶段 1.1 - 创建 AsyncTextureLoader 类

**命令：**
```bash
# 创建文件
touch WorldOfTheThreeKingdoms/GameManager/AsyncTextureLoader.cs

# 开始实现
code WorldOfTheThreeKingdoms/GameManager/AsyncTextureLoader.cs
```
