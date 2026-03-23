# 实施计划：部队列表显示功能

## 概述

本实施计划将设计文档中的部队列表显示功能转换为可执行的编码任务。该功能在现有城池列表界面基础上增加部队列表视图，两者可以切换但不能同时显示。

实施遵循以下原则：
- 严格遵守Anti-Band-Aid协议（不使用防御性空检查）
- 使用C# 12语法（Collection Expressions、Primary Constructors）
- 区分热路径（禁用LINQ）和冷路径（可用LINQ提高可读性）
- 数据完整性在源头验证，UI层快速失败
- AOT兼容（禁用反射）

## 任务列表

### 阶段1: 核心组件实现

- [x] 1. 创建数据模型和接口定义
  - 在 `WorldOfTheThreeKingdoms/GamePlugins/MarshalSectionDialogPlugin/` 目录下创建新文件
  - 创建 `TroopDisplayData` record（包含TroopId、Name、MilitaryKindName、Quantity、HasCommand、StatusText）
  - 创建 `ViewMode` enum（Architecture、Troop）
  - 创建 `ITroopDataFilter` 接口
  - 创建 `ITroopDisplayCache` 接口
  - 使用C# 12语法（record、primary constructors）
  - _需求: 1.2, 1.3, 1.4, 1.5, 1.6, 2.2, 2.3_

- [x] 2. 实现部队数据过滤器
  - [x] 2.1 创建 `TroopDataFilter` 类实现 `ITroopDataFilter`
    - **【C# 12】使用主构造函数**：`public sealed class TroopDataFilter(Scenario scenario) : ITroopDataFilter`
    - 实现 `GetPlayerControlledFieldTroops(Faction faction)` 方法
    - 使用LINQ过滤（冷路径，可读性优先）：属于玩家势力、城外部队、非AI军团控制
    - 不使用 `?.` 运算符，直接访问 `Army` 属性（数据完整性由数据源保证）
    - 使用Collection Expressions: `List<Troop> result = [..filteredTroops];`
    - _需求: 1.1, 1.7, 1.8, 7.6, 7.7_
  
  - [ ]* 2.2 编写属性测试：部队过滤正确性
    - **属性1: 部队过滤正确性**
    - **验证需求: 1.1, 1.7, 1.8, 7.6, 7.7**
    - 使用FsCheck生成随机部队集合和势力
    - 验证所有过滤后的部队都满足三个条件
    - 最小迭代次数: 100
  
  - [ ]* 2.3 编写单元测试：边界情况
    - 测试空列表返回空集合（不是null）
    - 测试null faction抛出ArgumentNullException
    - 测试混合部队集合的过滤结果

- [x] 3. 实现部队显示缓存
  - [x] 3.1 创建 `TroopDisplayCache` 类实现 `ITroopDisplayCache`
    - 实现 `GetOrCreateDisplayData(Troop troop)` 方法
    - 实现 `InvalidateCache()` 和 `Clear()` 方法
    - 使用 `Dictionary<int, TroopDisplayData>` 缓存显示数据
    - 使用 `Dictionary<int, int>` 缓存上次人数用于增量更新检测
    - 实现 `ValidateTroopData()` 私有方法（使用Debug.Assert验证数据完整性）
    - 实现 `CreateDisplayData()` 私有方法（转换Troop到TroopDisplayData）
    - 实现 `HasActiveCommand()` 和 `GetStatusText()` 辅助方法
    - 如果数据验证失败，抛出InvalidOperationException并记录详细错误信息
    - _需求: 1.2, 1.3, 1.4, 1.5, 1.6, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5_
  
  - [ ]* 3.2 编写属性测试：数据映射准确性
    - **属性3: 数据映射准确性**
    - **验证需求: 7.1, 7.2, 7.3, 7.4, 7.5**
    - 使用FsCheck生成随机有效部队
    - 验证显示数据的每个字段与源数据一致
    - 最小迭代次数: 100
  
  - [ ]* 3.3 编写属性测试：缓存一致性
    - 验证同一部队多次查询返回相同数据
    - 验证人数变化后缓存正确更新
    - 最小迭代次数: 100
  
  - [ ]* 3.4 编写单元测试：数据验证
    - 测试null部队抛出ArgumentNullException
    - 测试null名称抛出InvalidOperationException并记录部队ID
    - 测试null兵种抛出InvalidOperationException
    - 测试负数人数抛出InvalidOperationException
    - 验证错误日志包含部队ID和数据源信息

- [x] 4. Checkpoint - 核心组件验证
  - 确保所有单元测试和属性测试通过
  - 验证数据模型符合C# 12语法规范
  - 验证错误处理遵循快速失败原则
  - 如有问题请询问用户

### 阶段2: UI集成

- [ ] 5. 扩展MarshalSectionDialog
  - [x] 5.1 添加部队列表视图支持
    - 在 `MarshalSectionDialog.cs` 中添加 `ViewMode _currentViewMode` 字段
    - 添加 `TroopListManager _troopListManager` 字段
    - 在构造函数中初始化TroopListManager（注入依赖）
    - 添加 `ShowTroopListFrame()` 方法（显示部队列表框架）
    - 添加 `OnViewModeChanged(ViewMode mode)` 回调方法
    - 确保不修改现有城池列表功能
    - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_
  
  - [x] 5.2 添加视图切换按钮
    - 在UI中添加切换按钮（使用现有UI框架）
    - 按钮点击时调用 `TroopListManager.SwitchToTroopView()` 或 `SwitchToArchitectureView()`
    - 确保按钮在两种视图下都可见且可用
    - 实现200毫秒内完成视图切换
    - _需求: 2.1, 2.4, 2.5_
  
  - [ ]* 5.3 编写单元测试：视图切换
    - 测试切换到部队视图更新CurrentMode
    - 测试切换到城池视图更新CurrentMode
    - 测试视图互斥性（不能同时显示）
    - 测试切换按钮在两种模式下都可用

- [ ] 6. 实现TroopListManager
  - [x] 6.1 创建 `TroopListManager` 类
    - **【C# 12】使用主构造函数**：`public sealed class TroopListManager(MarshalSectionDialog dialog, ITroopDataFilter dataFilter, ITroopDisplayCache displayCache)`
    - **【内存管理】实现IDisposable接口**：提供Dispose()方法用于资源释放
    - 添加 `CurrentMode`、`IsVisible` 属性
    - 实现 `InitializeTroopList(Faction faction)` 方法
    - 实现 `RefreshTroopList(Faction faction)` 方法（仅在可见且为部队模式时更新）
    - 实现 `SwitchToTroopView(Faction faction)` 方法
    - 实现 `SwitchToArchitectureView()` 方法
    - 实现 `SetVisible(bool visible)` 方法（关闭时清空缓存）
    - 实现 `Dispose()` 方法：清空缓存、解除事件绑定、清理UI节点
    - 实现 `ConfigureTabListForTroops()` 私有方法（使用强类型委托，避免反射和装箱）
    - _需求: 1.1, 2.1, 2.2, 2.3, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.3, 5.5_
  
  - [x] 6.1.1 在MarshalSectionDialog中管理TroopListManager生命周期
    - 在MarshalSectionDialog的关闭/销毁方法中调用 `_troopListManager?.Dispose()`
    - 确保对话框销毁时释放所有资源
    - 验证无内存泄漏（使用内存分析工具）
  
  - [ ]* 6.2 编写属性测试：条件更新执行
    - **属性8: 条件更新执行**
    - **验证需求: 5.3, 5.5**
    - 验证仅在可见且为部队模式时执行更新
    - 验证不可见时不执行更新
    - 最小迭代次数: 100
  
  - [ ]* 6.3 编写单元测试：生命周期管理
    - 测试SetVisible(false)清空缓存
    - 测试SwitchToArchitectureView不修改IsVisible
    - 测试RefreshTroopList在不可见时不执行

- [ ] 7. 配置TabListPlugin适配部队数据
  - [x] 7.1 实现部队数据到TabListPlugin的适配（AOT兼容）
    - 分析TabListPlugin的API和数据格式要求
    - **【AOT关键】严禁使用反射**：绝对禁止使用 `System.Reflection.PropertyInfo` 进行数据绑定
    - **【AOT关键】使用强类型委托映射**：显式定义列映射 `Dictionary<string, Func<TroopDisplayData, string>>`
    - **【性能优化】避免装箱**：将值类型（int、bool）提前格式化为string，UI框架仅渲染纯文本
    - 创建适配器方法将 `TroopDisplayData` 手动提取为string[]数组
    - 配置列显示：部队名称、兵种、人数、指令状态、当前状态
    - 实现滚动功能（当列表项超过可视区域）
    - 实现空列表提示："当前没有城外部队"
    - 使用与游戏整体风格一致的字体和颜色
    - **【AOT验证】在Native AOT模式下编译和测试，确保运行时不崩溃**
    - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  
  - [ ]* 7.2 编写单元测试：UI布局
    - 测试空列表显示提示信息
    - 测试列表项显示顺序正确
    - 测试滚动功能在超长列表时可用

- [x] 8. Checkpoint - UI集成验证
  - 确保所有UI组件正确连接
  - 手动测试视图切换流畅性
  - 验证城池列表功能不受影响
  - 如有问题请询问用户

### 阶段3: 属性测试和单元测试补充

- [ ] 9. 编写剩余属性测试
  - [ ]* 9.1 属性2: 显示数据完整性
    - **验证需求: 1.2, 1.3, 1.4, 1.5, 1.6**
    - 验证所有显示数据包含必需字段
    - 最小迭代次数: 100
  
  - [ ]* 9.2 属性4: 视图互斥性
    - **验证需求: 2.2, 2.3**
    - 验证城池列表和部队列表不能同时可见
    - 最小迭代次数: 100
  
  - [ ]* 9.3 属性5: 切换按钮可用性
    - **验证需求: 2.5**
    - 验证切换按钮在任意视图模式下都可用
    - 最小迭代次数: 100
  
  - [ ]* 9.4 属性6: 数据变化反映
    - **验证需求: 4.1, 4.2**
    - 验证部队状态或人数变化后刷新显示最新数据
    - 最小迭代次数: 100
  
  - [ ]* 9.5 属性7: 过滤条件动态性
    - **验证需求: 4.3, 4.4, 4.5, 4.6**
    - 验证部队进入/离开城池后列表正确更新
    - 验证部队被AI接管/释放后列表正确更新
    - 最小迭代次数: 100
  
  - [ ]* 9.6 属性9: 列表行数一致性
    - **验证需求: 6.2**
    - 验证渲染的行数等于过滤后的部队数量
    - 最小迭代次数: 100
  
  - [ ]* 9.7 属性10: 输入数据非空验证
    - **验证需求: 8.1**
    - 验证null输入抛出ArgumentNullException
    - 最小迭代次数: 100
  
  - [ ]* 9.8 属性11: 字段数据有效性验证
    - **验证需求: 8.2, 8.3, 8.4, 8.5**
    - 验证无效字段抛出InvalidOperationException并记录详细信息
    - 最小迭代次数: 100

- [ ] 10. 编写FsCheck测试数据生成器
  - [ ]* 10.1 创建 `TroopGenerators` 类
    - 实现 `GenerateValidTroop()` 生成器（有效部队）
    - 实现 `GenerateInvalidTroop()` 生成器（无效数据部队）
    - 实现 `GenerateRandomTroopCollection()` 生成器（随机部队集合）
    - 实现 `GenerateRandomFaction()` 生成器（随机势力）
    - 确保生成器覆盖边界情况

- [ ] 11. Checkpoint - 测试覆盖率验证
  - 运行所有单元测试和属性测试
  - 验证代码覆盖率达到80%以上
  - 验证所有11个属性测试通过
  - 如有失败测试，分析并修复
  - 如有问题请询问用户

### 阶段4: 集成和优化

- [x] 12. 性能测试和优化
  - [ ]* 12.1 编写性能测试
    - 测试50个部队时保持60fps
    - 测试100个部队时保持30fps
    - 测试过滤大量部队的耗时（应小于100ms）
    - _需求: 5.1, 5.2_
  
  - [ ] 12.2 性能优化（如需要）
    - 分析性能瓶颈
    - 实施虚拟滚动（仅渲染可见行）
    - 优化缓存策略（增量更新）
    - 实施延迟更新（debounce）避免频繁刷新
    - _需求: 5.1, 5.2, 5.4_

- [ ] 13. 集成测试
  - [ ]* 13.1 编写集成测试
    - 测试完整的视图切换流程
    - 测试数据实时更新流程
    - 测试与城池列表的兼容性
    - 测试界面打开/关闭时的缓存管理
    - _需求: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

- [ ] 14. 回归测试
  - [ ]* 14.1 验证城池列表功能不受影响
    - 测试城池列表的所有现有功能
    - 测试城池列表的显示内容
    - 测试城池列表的交互行为
    - _需求: 3.1, 3.2, 3.3, 3.4_

- [ ] 15. Checkpoint - 集成验证
  - 确保所有测试通过
  - 验证性能要求达标
  - 验证现有功能不受影响
  - 如有问题请询问用户

### 阶段5: 代码审查和发布准备

- [ ] 16. 代码质量检查
  - [ ] 16.1 Anti-Band-Aid协议审查
    - 检查是否存在防御性空检查（`?.`、`if (obj != null)`）
    - 验证所有数据验证在源头进行
    - 验证UI层使用断言/异常快速失败
  
  - [ ] 16.2 C# 12语法审查
    - 验证使用Collection Expressions而非 `new List<T> { }`
    - 验证使用Primary Constructors（如适用）
    - 验证使用record类型（如适用）
  
  - [ ] 16.3 AOT兼容性审查
    - **【关键】检查TabListPlugin适配层是否使用反射**（`System.Reflection.PropertyInfo`）
    - 验证所有列映射使用强类型委托而非反射
    - 验证所有代码AOT兼容
    - **【验证】在Native AOT模式下编译项目**：`dotnet publish -c Release -r win-x64 --self-contained`
    - **【验证】运行AOT编译后的程序，测试部队列表功能**
  
  - [ ] 16.4 热路径/冷路径审查
    - 验证UI更新代码（冷路径）可以使用LINQ
    - 验证没有在热路径（Update/Draw）中使用LINQ

- [ ] 17. 文档更新
  - [ ] 17.1 更新代码注释
    - 为所有公共API添加XML文档注释
    - 为复杂逻辑添加内联注释
    - 使用简体中文编写注释
  
  - [ ] 17.2 更新用户文档
    - 记录如何使用部队列表功能
    - 记录视图切换操作
    - 添加截图（如需要）

- [ ] 18. 最终验证
  - 运行完整测试套件
  - 手动测试所有功能
  - 验证所有需求都已实现
  - 准备发布说明

## 注意事项

1. **标记说明**:
   - 标记 `*` 的任务为可选测试任务，可以跳过以加快MVP开发
   - 未标记的任务为必需实现任务

2. **需求追溯**:
   - 每个任务都标注了对应的需求编号（_需求: X.Y_）
   - 确保所有需求都被至少一个任务覆盖

3. **Checkpoint任务**:
   - Checkpoint任务用于阶段性验证
   - 遇到问题时应停下来询问用户

4. **测试优先级**:
   - 属性测试验证通用正确性（11个属性）
   - 单元测试验证具体示例和边界情况
   - 两者互补，都很重要

5. **性能要求**:
   - UI更新属于冷路径，可使用LINQ提高可读性
   - 但仍需满足性能要求（50部队60fps，100部队30fps）
   - 如性能不达标，需实施优化措施

6. **Anti-Band-Aid协议**:
   - 不使用 `?.` 运算符掩盖null问题
   - 不使用 `if (obj != null)` 防御性检查
   - 数据完整性在源头保证，UI层快速失败
   - 如遇到null，应该立即抛出异常并记录详细信息

## 实施完成标准

当以下条件全部满足时，该功能实施完成：

1. 所有必需任务（未标记 `*`）已完成
2. 所有单元测试通过
3. 所有属性测试通过（如实施）
4. 代码覆盖率达到80%以上
5. 性能测试达标
6. 回归测试通过（城池列表功能不受影响）
7. 代码审查通过（Anti-Band-Aid、C# 12、AOT兼容性）
8. 文档更新完成

---

**文档版本**: 1.0  
**创建日期**: 2026-03-04  
**功能名称**: troop-list-display  
**工作流类型**: requirements-first  
**规范类型**: feature
