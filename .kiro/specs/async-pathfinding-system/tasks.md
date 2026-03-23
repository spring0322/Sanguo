# 异步寻路系统实施任务列表

## 概述

本任务列表采用渐进式迁移策略，分为四个阶段：
1. 基础设施搭建（数据结构、对象池、地图快照）
2. 核心调度器实现（全局管理器、工作线程池）
3. 部队集成（Troop 字段和方法、生命周期管理）
4. 主循环集成与测试（Session 集成、系统切换）

每个阶段都包含实现任务和可选的测试任务，确保增量验证和早期错误发现。

## 任务

### 阶段 1：基础设施搭建

- [x] 1. 创建核心数据结构
  - [x] 1.1 实现 PathRequest 值类型
    - 在 `GameObjects/AI/Pathfinding/PathRequest.cs` 中创建 readonly record struct
    - 包含字段：TroopId (Guid), PathfindingVersion (ulong), StartPosition, TargetPosition, TerrainAdaptability, MilitaryKind, CancellationToken
    - 使用 C# 12 record struct 语法
    - _需求: 1.2, 7.1, 9.2_
  
  - [x] 1.2 实现 PathResult 值类型
    - 在 `GameObjects/AI/Pathfinding/PathResult.cs` 中创建 readonly record struct
    - 包含字段：TroopId, PathfindingVersion, Path (List<Point>?), IsSuccess, IsCancelled
    - _需求: 2.5, 3.2, 12.1, 12.2_
  
  - [ ]* 1.3 为核心数据结构编写单元测试
    - 测试 PathRequest 和 PathResult 的创建和字段访问
    - 测试值类型的不可变性
    - _需求: 1.2, 7.1_

- [x] 2. 实现对象池（PathPool）
  - [x] 2.1 实现 PathPool 静态类
    - 在 `GameObjects/AI/Pathfinding/PathPool.cs` 中创建
    - 使用 ConcurrentBag<List<Point>> 作为池存储
    - 实现 Rent() 方法：从池中租借或创建新列表（预分配容量 64）
    - 实现 Return(List<Point>?) 方法：清空并归还列表（null 安全处理）
    - 实现 PoolSize 属性用于监控
    - _需求: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [ ]* 2.2 为 PathPool 编写属性测试
    - **属性 15: 对象池租借清空** - 验证租借的列表为空
    - **属性 16: 对象池归还清空** - 验证归还的列表被清空
    - **验证需求: 6.2, 6.5**
  
  - [ ]* 2.3 为 PathPool 编写单元测试
    - 测试 Rent 返回空列表
    - 测试 Return null 不崩溃
    - 测试多次 Rent/Return 循环
    - 测试并发场景（多线程同时 Rent/Return）
    - _需求: 6.2, 6.4, 6.5_

- [x] 3. 实现地图快照系统
  - [x] 3.1 实现 SpatialGrid 类
    - 在 `GameObjects/AI/Pathfinding/SpatialGrid.cs` 中创建
    - 使用 HashSet<Point> 存储障碍物位置
    - 使用 Dictionary<Point, int> 存储建筑控制区域
    - 实现 CreateFromScenario(Scenario) 静态方法（回合级快照）
    - 实现 IsObstacle(Point) 方法（O(1) 查询）
    - 实现 GetControllingFaction(Point) 方法
    - 使用 LINQ 提高可读性（Cold Path）
    - _需求: 8.2, 8.3_
  
  - [x] 3.2 实现 MapSnapshot 类
    - 在 `GameObjects/AI/Pathfinding/MapSnapshot.cs` 中创建
    - 静态数据：只读引用 TerrainData（零拷贝）
    - 动态数据：SpatialGrid 快照
    - 包含 Width, Height, TurnNumber 字段
    - 构造函数接收 Scenario 参数
    - _需求: 8.1, 8.2, 8.3_
  
  - [ ]* 3.3 为地图快照编写属性测试
    - **属性 19: 地图快照创建** - 验证快照包含所有动态障碍物
    - **属性 21: 快照创建性能** - 验证 200x200 地图在 5ms 内完成
    - **验证需求: 8.2, 8.4, 8.5**
  
  - [ ]* 3.4 为地图快照编写单元测试
    - 测试 SpatialGrid 创建和查询
    - 测试 MapSnapshot 零拷贝（引用相等性）
    - 测试建筑控制区域查询
    - 测试 null 安全（BelongedFaction 为 null）
    - _需求: 8.1, 8.2_

- [x] 4. 检查点 - 基础设施验证
  - 确保所有测试通过，询问用户是否有问题

### 阶段 2：核心调度器实现

- [x] 5. 实现 AsyncPathfindingManager 核心结构
  - [x] 5.1 创建 AsyncPathfindingManager 单例类
    - 在 `GameObjects/AI/Pathfinding/AsyncPathfindingManager.cs` 中创建
    - 实现单例模式（public static Instance 属性）
    - 创建 Channel<PathRequest> 无边界通道
    - 创建 ConcurrentQueue<PathResult> 结果队列
    - 创建 CancellationTokenSource 用于系统生命周期
    - 定义 WorkerCount 常量（默认 2）
    - 实现 IDisposable 接口
    - _需求: 2.1, 5.5_
  
  - [x] 5.2 实现请求提交方法
    - 实现 EnqueueRequest(PathRequest) 方法
    - 使用 Channel.Writer.TryWrite 非阻塞写入
    - 添加调试日志
    - _需求: 1.1, 1.3_
  
  - [x] 5.3 实现结果处理方法
    - 实现 ProcessCompletedPaths(Action<PathResult>) 方法
    - 使用 for 循环限制单帧最多处理 20 个结果（避免 LINQ）
    - 使用 ConcurrentQueue.TryDequeue 读取结果
    - 确保执行时间 < 1ms（Hot Path 优化）
    - _需求: 3.1, 3.6, 7.4_
  
  - [ ]* 5.4 为调度器核心结构编写单元测试
    - 测试单例模式
    - 测试 EnqueueRequest 非阻塞性
    - 测试 ProcessCompletedPaths 限流（最多 20 个）
    - _需求: 1.1, 1.3, 3.1_

- [x] 6. 实现后台工作线程
  - [x] 6.1 实现 WorkerLoopAsync 方法
    - 使用 await foreach 从 Channel 读取请求
    - 检查 CancellationToken 是否已取消
    - 调用 ExecuteAStar 执行寻路
    - 处理取消情况：回收路径并返回取消结果
    - 处理成功情况：将结果写入队列
    - 捕获 OperationCanceledException 正常退出
    - _需求: 2.3, 2.4, 2.5, 4.3_
  
  - [x] 6.2 在构造函数中启动工作线程
    - 使用 Task.Factory.StartNew 启动 WorkerCount 个线程
    - 使用 TaskCreationOptions.LongRunning 标志
    - 添加调试日志
    - _需求: 2.1_
  
  - [x] 6.3 实现 Dispose 方法
    - 触发 _systemCts.Cancel()
    - 等待所有工作线程退出
    - 释放资源
    - _需求: 5.5_
  
  - [ ]* 6.4 为工作线程编写集成测试
    - 测试工作线程启动和退出
    - 测试请求处理流程
    - 测试取消响应性（< 50ms）
    - _需求: 2.1, 4.3, 5.5_

- [x] 7. 实现 A* 寻路算法
  - [x] 7.1 实现 ExecuteAStar 方法框架
    - 从 PathPool 租借路径列表
    - 创建 openSet (PriorityQueue<Point, int>)
    - 创建 closedSet (HashSet<Point>)
    - 创建 cameFrom (Dictionary<Point, Point>)
    - 创建 gScore (Dictionary<Point, int>)
    - 使用 for 循环而非 LINQ（Hot Path 优化）
    - 定期检查 CancellationToken（每 N 次迭代）
    - _需求: 2.2, 2.4_
  
  - [x] 7.2 实现 A* 核心算法逻辑
    - 实现主循环：从 openSet 取出最小 fScore 节点
    - 检查是否到达目标
    - 遍历邻居节点（上下左右）
    - 计算 tentativeGScore
    - 更新 gScore 和 cameFrom
    - 使用 MapSnapshot 访问地形数据（线程安全）
    - 使用 SpatialGrid 查询动态障碍物
    - _需求: 2.2, 7.2, 7.3, 8.1_
  
  - [x] 7.3 实现辅助方法
    - 实现 GetMoveCost(from, to, request) 方法
    - 实现 Heuristic(from, to) 方法（曼哈顿距离）
    - 实现 ReconstructPath(cameFrom, current, path) 方法
    - 使用 for 循环和 Reverse（避免 LINQ）
    - _需求: 2.2_
  
  - [x] 7.4 处理寻路失败和取消
    - 寻路失败：回收路径并返回 null
    - 寻路取消：回收路径并返回 null
    - 确保所有路径都正确回收
    - _需求: 4.4, 12.1, 12.2_
  
  - [ ]* 7.5 为 A* 算法编写属性测试
    - **属性 4: 寻路算法正确性** - 验证找到有效路径
    - **属性 5: 取消响应性** - 验证 50ms 内响应取消
    - **属性 29: 寻路失败处理** - 验证失败时回收内存
    - **验证需求: 2.2, 2.4, 12.1**
  
  - [ ]* 7.6 为 A* 算法编写单元测试
    - 测试简单路径（直线、L 形）
    - 测试障碍物绕行
    - 测试无法到达的目标
    - 测试取消机制
    - _需求: 2.2, 2.4, 12.1_

- [x] 8. 检查点 - 调度器验证
  - 确保所有测试通过，询问用户是否有问题

### 阶段 3：部队集成

- [x] 9. 为 Troop 添加新系统字段
  - [x] 9.1 在 Troop 类中添加新字段
    - 在 `WorldOfTheThreeKingdoms/GameObjects/Troop.cs` 中添加
    - 添加 Id (Guid) 属性，默认 Guid.NewGuid()
    - 添加 _currentPathfindingVersion (ulong) 字段，初始值 0
    - 添加 _isWaitingForPath (bool) 字段，初始值 false
    - 添加 _moveCts (CancellationTokenSource?) 字段
    - 添加 _cachedPath (List<Point>?) 字段
    - 添加 UseNewMovementSystem (static bool) 属性，默认 false
    - _需求: 1.4, 9.1, 10.1_
  
  - [ ]* 9.2 为 Troop 字段编写单元测试
    - 测试 Id 唯一性
    - 测试版本号初始值
    - 测试字段初始状态
    - _需求: 9.1_

- [x] 10. 实现 Troop 寻路方法
  - [x] 10.1 实现 RequestMoveAsync 方法
    - 检查 UseNewMovementSystem 开关
    - 调用 CancelCurrentPathfinding() 清理旧任务
    - 递增 _currentPathfindingVersion
    - 创建新的 CancellationTokenSource
    - 设置 _isWaitingForPath = true
    - 构建 PathRequest（使用快照数据）
    - 调用 AsyncPathfindingManager.Instance.EnqueueRequest
    - 添加调试日志
    - _需求: 1.2, 1.3, 1.4, 9.1_
  
  - [x] 10.2 实现 OnPathfindingCompleted 方法
    - 检查 UseNewMovementSystem 开关
    - 验证版本号匹配（三层防御的第三层）
    - 处理过期结果：回收路径并返回
    - 设置 _isWaitingForPath = false
    - 处理取消结果：输出日志并返回
    - 处理失败结果：输出日志并返回
    - 处理成功结果：回收旧路径，接收新路径
    - 添加调试日志
    - _需求: 3.3, 3.4, 3.5, 9.4, 9.5_
  
  - [x] 10.3 实现 CancelCurrentPathfinding 方法
    - 检查 UseNewMovementSystem 开关
    - 检查 _isWaitingForPath 状态
    - 调用 _moveCts.Cancel()
    - 释放 _moveCts 并设为 null
    - 设置 _isWaitingForPath = false
    - 不使用 ?. 防御性检查（反创可贴协议）
    - _需求: 4.1, 4.2_
  
  - [ ]* 10.4 为 Troop 寻路方法编写属性测试
    - **属性 1: 异步请求非阻塞性** - 验证 10μs 内完成
    - **属性 2: 请求数据完整性** - 验证 PathRequest 包含所有字段
    - **属性 3: 版本号单调递增** - 验证版本号递增
    - **属性 8: 版本号防御机制** - 验证过期结果被丢弃
    - **属性 9: 成功路径接收** - 验证路径接收和旧路径回收
    - **验证需求: 1.1, 1.2, 1.4, 3.3, 3.4, 3.5, 9.1, 9.4, 9.5**
  
  - [ ]* 10.5 为 Troop 寻路方法编写单元测试
    - 测试 RequestMoveAsync 非阻塞性
    - 测试版本号递增
    - 测试取消旧任务
    - 测试 OnPathfindingCompleted 版本号校验
    - 测试过期结果处理
    - _需求: 1.1, 1.4, 3.3, 3.5, 9.4_

- [x] 11. 修复 Troop 生命周期管理
  - [x] 11.1 修改 Troop.Destroy 方法
    - 在现有 Destroy 方法开头添加新系统清理逻辑
    - 检查 UseNewMovementSystem 开关
    - 调用 CancelCurrentPathfinding()
    - 调用 PathPool.Return(_cachedPath)
    - 设置 _cachedPath = null
    - 保留旧系统清理逻辑（FirstTierPath 等）
    - 确保新旧系统都清理（防止内存泄漏）
    - _需求: 4.5, 5.1, 5.2, 10.4_
  
  - [ ]* 11.2 为 Troop 生命周期编写属性测试
    - **属性 12: 销毁时任务取消** - 验证销毁时取消寻路
    - **属性 13: 生命周期资源清理** - 验证所有资源被清理
    - **验证需求: 4.5, 5.1, 5.2, 5.3**
  
  - [ ]* 11.3 为 Troop 生命周期编写单元测试
    - 测试 Destroy 取消寻路任务
    - 测试 Destroy 回收路径内存
    - 测试新旧系统都清理
    - _需求: 5.1, 5.2, 10.4_

- [x] 12. 检查点 - 部队集成验证
  - 确保所有测试通过，询问用户是否有问题

### 阶段 4：主循环集成与测试

- [x] 13. 集成到 Session 主循环
  - [x] 13.1 在 Session 中添加部队注册表
    - 在 `GameScreens/Session.cs` 中添加
    - 添加 _troopRegistry (Dictionary<Guid, Troop>) 字段
    - 实现 RegisterTroop(Troop) 方法
    - 实现 UnregisterTroop(Troop) 方法
    - 检查 UseNewMovementSystem 开关
    - _需求: 5.3_
  
  - [x] 13.2 在 Session.Initialize 中注册现有部队
    - 检查 UseNewMovementSystem 开关
    - 遍历 Scenario.Troops 并调用 RegisterTroop
    - _需求: 5.3_
  
  - [x] 13.3 在 Session.Update 中处理寻路结果
    - 检查 UseNewMovementSystem 开关
    - 调用 AsyncPathfindingManager.Instance.ProcessCompletedPaths
    - 使用 _troopRegistry.TryGetValue 查找部队（避免异常）
    - 如果部队存在，调用 OnPathfindingCompleted
    - 如果部队不存在，回收路径内存（孤儿结果处理）
    - 确保执行时间 < 1ms（Hot Path 优化）
    - _需求: 3.2, 5.4, 12.3_
  
  - [x] 13.4 修改 Troop.Destroy 调用 UnregisterTroop
    - 在 Destroy 方法中添加 Session.Current.UnregisterTroop(this)
    - 检查 UseNewMovementSystem 开关
    - _需求: 5.3_
  
  - [ ]* 13.5 为 Session 集成编写属性测试
    - **属性 6: 结果传递完整性** - 验证结果被正确分发
    - **属性 7: 单帧处理限流** - 验证最多处理 20 个结果
    - **属性 14: 孤儿结果处理** - 验证部队销毁后路径回收
    - **属性 30: 单帧处理性能** - 验证 < 1ms 完成
    - **验证需求: 2.5, 3.1, 3.2, 3.6, 5.4, 12.3**
  
  - [ ]* 13.6 为 Session 集成编写集成测试
    - 测试完整寻路流程（请求 → 执行 → 结果）
    - 测试部队注册和注销
    - 测试孤儿结果处理
    - 测试单帧限流
    - _需求: 3.1, 3.2, 5.3, 5.4_

- [x] 14. 实现系统切换机制
  - [x] 14.1 验证 UseNewMovementSystem 开关
    - 确保所有新系统代码都检查开关
    - 确保旧系统不受影响
    - 测试开关切换（false → true → false）
    - _需求: 10.1, 10.2, 10.3_
  
  - [x] 14.2 验证新旧系统共存
    - 测试 UseNewMovementSystem = false 时使用旧系统
    - 测试 UseNewMovementSystem = true 时使用新系统
    - 测试 Destroy 同时清理新旧系统资源
    - _需求: 10.2, 10.3, 10.4_
  
  - [ ]* 14.3 为系统切换编写属性测试
    - **属性 23: 系统共存隔离** - 验证新旧系统隔离
    - **验证需求: 10.2, 10.3**
  
  - [ ]* 14.4 为系统切换编写集成测试
    - 测试灰度发布场景
    - 测试紧急回滚场景
    - 测试运行时切换
    - _需求: 10.5, 10.6_

- [x] 15. 性能测试与优化
  - [ ]* 15.1 编写性能基准测试
    - 测试 100 支部队同时寻路
    - 测试 GC 频率和停顿时间
    - 测试寻路延迟（P50, P95, P99）
    - 测试 MapSnapshot 创建时间
    - 测试单帧处理时间
    - 测试 CPU 占用率
    - 测试内存占用
    - _需求: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_
  
  - [ ]* 15.2 编写压力测试
    - 测试长时间运行（24 小时）
    - 测试频繁创建/销毁部队
    - 测试高频取消操作
    - 验证无内存泄漏
    - 验证性能不退化
    - _需求: 6.6, 11.5_
  
  - [ ]* 15.3 编写并发测试
    - 测试 100 支部队同时寻路无竞态条件
    - 测试对象池并发访问
    - 测试 Channel 和 ConcurrentQueue 并发性能
    - _需求: 7.5, 18_
  
  - [ ]* 15.4 性能分析与优化
    - 使用 .NET Profiler 分析热点
    - 优化 Hot Path 代码
    - 验证所有性能指标达标
    - _需求: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

- [x] 16. 最终检查点 - 系统验证
  - 确保所有测试通过
  - 验证所有性能指标达标
  - 询问用户是否准备好部署

## 注意事项

- 标记 `*` 的任务为可选测试任务，可跳过以加快 MVP 开发
- 每个任务都引用了具体的需求编号，确保可追溯性
- 检查点任务确保增量验证，及早发现问题
- Hot Path 代码（Update、ExecuteAStar、ProcessCompletedPaths）严格优化：禁止 LINQ，禁止分配
- Cold Path 代码（MapSnapshot 创建、Destroy）可读性优先：可使用 LINQ
- 使用 C# 12 现代语法：集合表达式 `[]`、record struct、primary constructors
- 所有字符串内容使用简体中文
- 新旧系统通过 UseNewMovementSystem 开关隔离，支持渐进式迁移

## 性能目标

| 指标 | 目标 |
|------|------|
| 帧率 | 60fps |
| GC 频率 | < 1次/秒 |
| GC 停顿 | < 5ms |
| 寻路延迟（P95） | < 100ms |
| MapSnapshot 创建 | < 5ms |
| 单帧处理时间 | < 1ms |
| 内存占用增长 | < 10% |
| CPU 占用 | < 20% |
