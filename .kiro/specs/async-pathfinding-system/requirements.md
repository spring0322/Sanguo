# 异步寻路系统需求文档

**项目**: World of the Three Kingdoms (zhsan)  
**功能名称**: async-pathfinding-system  
**创建日期**: 2026-02-21  
**文档类型**: 业务需求

---

## 简介

异步寻路系统是一个高性能、线程安全的寻路调度系统，用于解决现有半成品异步实现中的生命周期管理缺失、跨线程安全问题和 GC 压力等 Critical 问题。系统需要在保证 60fps 稳定运行的前提下，支持 100 支部队同时寻路，并与现有系统共存以支持渐进式迁移。

---

## 术语表

- **System（系统）**: 异步寻路系统，包括调度器、工作线程池和对象池
- **Troop（部队）**: 游戏中的军事单位，需要寻路移动
- **PathfindingManager（寻路管理器）**: 全局单例调度器，负责管理所有寻路请求
- **PathRequest（寻路请求）**: 包含寻路所需参数的不可变数据结构
- **PathResult（寻路结果）**: 包含寻路结果的数据结构
- **PathPool（路径池）**: 对象池，用于复用路径列表以减少 GC 压力
- **MapSnapshot（地图快照）**: 地图数据的只读快照，用于后台线程安全访问
- **PathfindingVersion（寻路版本号）**: 用于防止时序错乱的单调递增版本号
- **WorkerThread（工作线程）**: 后台线程，执行实际的寻路算法
- **CancellationToken（取消令牌）**: 用于取消异步操作的机制

---

## 需求

### 需求 1：异步寻路请求提交

**用户故事**：作为游戏玩家，我希望下达移动命令后部队能立即响应，而不会造成游戏卡顿，以便获得流畅的游戏体验。

#### 验收标准

1. WHEN 玩家点击地图目标位置 THEN THE System SHALL 在 10 微秒内接受寻路请求并返回主线程
2. WHEN 部队接收到移动命令 THEN THE System SHALL 创建包含起点、终点、地形适应性和兵种类型的寻路请求
3. WHEN 寻路请求被提交 THEN THE System SHALL 将请求写入无边界通道而不阻塞主线程
4. WHEN 部队正在等待寻路结果 THEN THE Troop SHALL 标记为等待状态并递增寻路版本号

### 需求 2：后台寻路执行

**用户故事**：作为系统架构师，我希望寻路计算在后台线程执行，以避免阻塞游戏主循环，从而保证 60fps 稳定运行。

#### 验收标准

1. THE System SHALL 维护 2 到 4 个固定工作线程用于执行寻路算法
2. WHEN 工作线程从请求通道读取请求 THEN THE System SHALL 使用 A* 算法计算从起点到终点的路径
3. WHEN 执行寻路算法 THEN THE WorkerThread SHALL 只访问 PathRequest 中的不可变数据和 MapSnapshot 快照数据
4. WHEN 寻路算法执行期间 THEN THE WorkerThread SHALL 定期检查取消令牌并在 50 毫秒内响应取消请求
5. WHEN 寻路完成 THEN THE System SHALL 将结果写入并发队列供主线程处理

### 需求 3：寻路结果处理

**用户故事**：作为游戏玩家，我希望部队在寻路完成后能立即开始移动，以便快速响应我的指令。

#### 验收标准

1. WHEN 游戏主循环每帧更新 THEN THE System SHALL 从结果队列中取出最多 20 个已完成的寻路结果
2. WHEN 处理寻路结果 THEN THE System SHALL 根据部队 ID 找到对应的部队对象
3. WHEN 部队接收到寻路结果 THEN THE Troop SHALL 验证结果的寻路版本号与当前版本号是否匹配
4. WHEN 寻路版本号匹配且寻路成功 THEN THE Troop SHALL 接收新路径并开始移动
5. WHEN 寻路版本号不匹配 THEN THE Troop SHALL 丢弃过期结果并回收路径内存
6. WHEN 单帧处理所有寻路结果 THEN THE System SHALL 在 1 毫秒内完成以保证 60fps

### 需求 4：寻路任务取消

**用户故事**：作为游戏玩家，我希望能够随时改变部队的移动目标，系统应该取消旧的寻路任务，以避免浪费计算资源。

#### 验收标准

1. WHEN 部队接收到新的移动命令 THEN THE System SHALL 取消当前正在进行的寻路任务
2. WHEN 取消寻路任务 THEN THE System SHALL 触发取消令牌并释放相关资源
3. WHEN 后台线程检测到取消令牌 THEN THE WorkerThread SHALL 在 50 毫秒内停止寻路并返回取消结果
4. WHEN 寻路被取消 THEN THE System SHALL 回收已分配的路径内存到对象池
5. WHEN 部队销毁 THEN THE System SHALL 自动取消该部队的所有寻路任务

### 需求 5：生命周期管理

**用户故事**：作为系统架构师，我希望部队销毁时能正确清理所有相关资源，以避免内存泄漏和悬挂引用。

#### 验收标准

1. WHEN 部队被销毁 THEN THE System SHALL 取消该部队的所有正在进行的寻路任务
2. WHEN 部队被销毁 THEN THE System SHALL 回收该部队的缓存路径到对象池
3. WHEN 部队被销毁 THEN THE System SHALL 从部队注册表中移除该部队
4. WHEN 寻路结果返回时部队已销毁 THEN THE System SHALL 自动回收路径内存而不调用部队回调
5. WHEN 系统关闭 THEN THE System SHALL 取消所有后台线程并释放所有资源

### 需求 6：对象池内存管理

**用户故事**：作为性能工程师，我希望通过对象池复用路径列表，以减少 GC 压力并提高性能。

#### 验收标准

1. THE System SHALL 维护一个线程安全的路径对象池用于复用 List<Point> 实例
2. WHEN 后台线程需要存储路径 THEN THE System SHALL 从对象池租借一个清空的列表
3. WHEN 部队接收到新路径 THEN THE System SHALL 将旧路径归还到对象池
4. WHEN 寻路失败或被取消 THEN THE System SHALL 将已分配的路径归还到对象池
5. WHEN 归还路径到对象池 THEN THE System SHALL 清空列表内容以供下次使用
6. WHEN 长时间运行 THEN THE System SHALL 保持对象池大小稳定以避免内存泄漏

### 需求 7：线程安全保证

**用户故事**：作为系统架构师，我希望后台线程不访问主线程的易变数据，以避免竞态条件和数据损坏。

#### 验收标准

1. THE PathRequest SHALL 只包含值类型或不可变数据而不包含 Troop 对象引用
2. THE WorkerThread SHALL 只访问 PathRequest 中的参数和 MapSnapshot 快照数据
3. THE WorkerThread SHALL 不访问 Session.Current.Scenario 或其他主线程易变数据
4. THE System SHALL 使用线程安全集合（Channel、ConcurrentQueue、ConcurrentBag）进行跨线程通信
5. WHEN 100 支部队同时寻路 THEN THE System SHALL 无数据竞争和竞态条件

### 需求 8：地图数据快照

**用户故事**：作为性能工程师，我希望地图数据快照采用零拷贝设计，以避免深度拷贝带来的性能开销。

#### 验收标准

1. WHEN 创建地图快照 THEN THE System SHALL 使用只读引用共享静态地形数据而不进行拷贝
2. WHEN 创建地图快照 THEN THE System SHALL 创建动态障碍物（部队位置、建筑控制区）的回合级快照
3. WHEN 多个寻路任务在同一回合执行 THEN THE System SHALL 共享同一个地图快照实例
4. WHEN 创建 200x200 大地图快照 THEN THE System SHALL 在 5 毫秒内完成
5. WHEN 创建地图快照 THEN THE System SHALL 分配少于 100KB 内存

### 需求 9：版本号防御机制

**用户故事**：作为系统架构师，我希望通过版本号机制防止时序错乱，确保部队只接收最新的寻路结果。

#### 验收标准

1. WHEN 部队发起新的寻路请求 THEN THE System SHALL 递增该部队的寻路版本号
2. WHEN 创建寻路请求 THEN THE System SHALL 将当前寻路版本号包含在请求中
3. WHEN 寻路完成 THEN THE System SHALL 将寻路版本号包含在结果中
4. WHEN 部队接收到寻路结果 THEN THE Troop SHALL 验证结果版本号与当前版本号是否匹配
5. WHEN 结果版本号不匹配 THEN THE Troop SHALL 丢弃过期结果并回收路径内存
6. WHEN 版本号校验 THEN THE System SHALL 在 1 纳秒内完成以保证零性能开销

### 需求 10：系统共存与迁移

**用户故事**：作为项目经理，我希望新旧系统能够共存，支持渐进式迁移和灰度发布，以降低风险。

#### 验收标准

1. THE System SHALL 提供全局开关 UseNewMovementSystem 用于启用或禁用新系统
2. WHEN UseNewMovementSystem 为 false THEN THE Troop SHALL 使用旧的 FirstTierPath 系统
3. WHEN UseNewMovementSystem 为 true THEN THE Troop SHALL 使用新的异步寻路系统
4. THE Troop.Destroy SHALL 同时清理新旧两套系统的资源以避免内存泄漏
5. WHEN 切换系统开关 THEN THE System SHALL 无需重启游戏即可生效
6. WHEN 新系统出现问题 THEN THE System SHALL 支持紧急回滚到旧系统

### 需求 11：性能保证

**用户故事**：作为游戏玩家，我希望游戏在大规模战斗时仍能保持流畅，不会出现卡顿或延迟。

#### 验收标准

1. WHEN 100 支部队同时寻路 THEN THE System SHALL 保持 60fps 稳定运行
2. WHEN 系统运行 THEN THE System SHALL 保持 GC 频率低于每秒 1 次
3. WHEN 发生 GC THEN THE System SHALL 保持 GC 停顿时间低于 5 毫秒
4. WHEN 执行寻路 THEN THE System SHALL 在 100 毫秒内完成 95% 的寻路请求（P95）
5. WHEN 系统长时间运行 THEN THE System SHALL 保持内存占用增长低于 10%
6. WHEN 系统运行 THEN THE System SHALL 保持 CPU 占用率低于 20%

### 需求 12：错误处理

**用户故事**：作为系统架构师，我希望系统能够优雅地处理各种错误情况，不会崩溃或泄漏资源。

#### 验收标准

1. WHEN 寻路无法找到有效路径 THEN THE System SHALL 返回失败结果并回收已分配内存
2. WHEN 寻路被取消 THEN THE System SHALL 返回取消结果并回收已分配内存
3. WHEN 寻路结果返回时部队已销毁 THEN THE System SHALL 自动回收路径内存
4. WHEN 接收到过期的寻路结果 THEN THE System SHALL 丢弃结果并回收路径内存
5. WHEN 系统关闭 THEN THE System SHALL 正常退出所有后台线程而不留悬挂线程
6. WHEN 发生任何错误 THEN THE System SHALL 输出调试日志以便问题诊断

---

**文档结束**

**创建日期**：2026-02-21  
**版本**：v1.0  
**状态**：待评审
