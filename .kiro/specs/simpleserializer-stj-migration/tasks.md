# SimpleSerializer System.Text.Json 迁移任务列表

## 1. 基础设施准备阶段

### 1.1 GameJsonContext 完善
- [ ] 1.1.1 审查并完善 GameJsonContext 中的类型注册
  - [ ] 1.1.1.1 验证所有核心 GameObject 类型已注册
  - [ ] 1.1.1.2 添加缺失的 Dictionary 和 List 类型注册
  - [ ] 1.1.1.3 注册所有自定义集合类型（FactionListWithQueue, TroopListWithQueue 等）
  - [ ] 1.1.1.4 添加枚举类型注册
- [ ] 1.1.2 添加 GetDefaultOptions 静态方法到 GameJsonContext
- [ ] 1.1.3 配置序列化选项（ReferenceHandler.Preserve, 编码器等）

### 1.2 AOT 兼容性验证
- [ ] 1.2.1 运行 AOTSerializationCompatibilityAnalyzer 分析当前状态
- [ ] 1.2.2 修复发现的 AOT 兼容性问题
- [ ] 1.2.3 创建 AOTSerializationValidator 编译时验证工具
- [ ] 1.2.4 集成到构建流程中

### 1.3 测试基础设施
- [ ] 1.3.1 创建性能基准测试项目
- [ ] 1.3.2 准备测试数据集（小、中、大规模 GameScenario）
- [ ] 1.3.3 建立基准性能指标（Newtonsoft.Json 作为基线）
- [ ] 1.3.4 创建单元测试框架

## 2. 核心序列化方法迁移阶段

### 2.1 SerializeJson 方法重写
- [x] 2.1.1 实现新的 SerializeJson<T> 方法
  - [x] 2.1.1.1 使用 JsonSerializer.Serialize 替代 JsonConvert.SerializeObject
  - [x] 2.1.1.2 集成 GameJsonContext.Default 作为类型解析器
  - [x] 2.1.1.3 保持原有参数兼容性（zip, indented, net）
  - [x] 2.1.1.4 实现错误处理和日志记录
- [x] 2.1.2 添加类型验证逻辑
- [x] 2.1.3 性能优化（减少临时对象分配）
- [ ] 2.1.4 单元测试覆盖

### 2.2 DeserializeJson 方法重写
- [x] 2.2.1 实现新的 DeserializeJson<T> 方法
  - [x] 2.2.1.1 使用 JsonSerializer.Deserialize 作为主要反序列化方法
  - [x] 2.2.1.2 实现格式检测逻辑（IsLegacyFormat）
  - [x] 2.2.1.3 实现旧格式回退机制（DeserializeJsonLegacy）
  - [x] 2.2.1.4 添加迁移建议日志记录
- [x] 2.2.2 保持 GameScenario 集合初始化逻辑
- [x] 2.2.3 错误处理和恢复策略
- [ ] 2.2.4 兼容性测试（新旧格式）

### 2.3 文件序列化方法更新
- [x] 2.3.1 更新 SerializeJsonFile 方法调用新的 SerializeJson
- [x] 2.3.2 更新 DeserializeJsonFile 方法调用新的 DeserializeJson
- [ ] 2.3.3 保持文件操作逻辑不变
- [ ] 2.3.4 添加文件格式迁移提示

## 3. 自定义转换器迁移阶段

### 3.1 GameObjectListConverter 实现
- [ ] 3.1.1 创建 System.Text.Json 版本的 GameObjectListConverter
  - [ ] 3.1.1.1 实现 CanConvert 方法识别游戏对象列表类型
  - [ ] 3.1.1.2 实现 Read 方法支持新旧格式
  - [ ] 3.1.1.3 实现 Write 方法使用新格式
  - [ ] 3.1.1.4 处理泛型列表类型
- [ ] 3.1.2 支持旧字典格式 {"1": {...}, "2": {...}} 的读取
- [ ] 3.1.3 新数组格式 [...] 的读写
- [ ] 3.1.4 单元测试和兼容性验证

### 3.2 其他关键转换器迁移
- [ ] 3.2.1 实现 GameObjectReferenceConverter
  - [ ] 3.2.1.1 处理对象引用和 ID 映射
  - [ ] 3.2.1.2 支持循环引用检测
- [ ] 3.2.2 实现 FactionLeaderConverter
  - [ ] 3.2.2.1 处理势力领袖关系序列化
  - [ ] 3.2.2.2 支持 LeaderID 到 Leader 对象的转换
- [ ] 3.2.3 实现 PersonIdealTendencyConverter
  - [ ] 3.2.3.1 处理人物理想倾向的复杂序列化
- [ ] 3.2.4 迁移其他必要的转换器（根据编译错误确定）

### 3.3 转换器注册和配置
- [ ] 3.3.1 在 GameJsonContext 中注册所有自定义转换器
- [ ] 3.3.2 配置转换器优先级和执行顺序
- [ ] 3.3.3 验证转换器在 AOT 环境下的兼容性

## 4. 依赖移除和编译修复阶段

### 4.1 项目文件更新
- [x] 4.1.1 从 WorldOfTheThreeKingdoms.csproj 移除 Newtonsoft.Json 包引用
- [x] 4.1.2 从 WorldOfTheThreeKingdomsEditor.csproj 移除 Newtonsoft.Json 包引用
- [x] 4.1.3 确保 System.Text.Json 包引用存在且版本正确

### 4.2 命名空间和引用清理
- [x] 4.2.1 移除所有 `using Newtonsoft.Json;` 引用 (已完成主要文件，Legacy转换器保留用于兼容性)
- [x] 4.2.2 移除所有 `using Newtonsoft.Json.Linq;` 引用
- [x] 4.2.3 添加必要的 `using System.Text.Json;` 引用
- [x] 4.2.4 更新相关的 using 语句

### 4.3 编译错误系统性修复
- [x] 4.3.1 识别所有 JsonConvert 使用位置并替换 (主要文件已完成)
- [ ] 4.3.2 修复 JObject, JArray 等 Newtonsoft 特有类型的使用 (Legacy转换器中保留)
- [ ] 4.3.3 更新自定义转换器基类继承 (Legacy转换器保留原有继承)
- [x] 4.3.4 修复属性特性（JsonProperty → JsonPropertyName 等）
- [x] 4.3.5 处理类型转换和兼容性问题

### 4.4 编译验证
- [x] 4.4.1 确保项目零编译错误 (当前状态: 编译成功，0个错误，仅有警告)
- [x] 4.4.2 确保项目零编译警告（JSON 相关）
- [ ] 4.4.3 验证所有项目配置正确

## 5. 性能优化阶段

### 5.1 异步序列化实现
- [x] 5.1.1 实现 SerializeJsonAsync<T> 方法
  - [x] 5.1.1.1 使用 JsonSerializer.SerializeAsync
  - [x] 5.1.1.2 支持 CancellationToken
  - [x] 5.1.1.3 流式处理大文件
- [x] 5.1.2 实现 DeserializeJsonAsync<T> 方法
  - [x] 5.1.2.1 使用 JsonSerializer.DeserializeAsync
  - [x] 5.1.2.2 支持异步格式检测和回退
- [ ] 5.1.3 异步文件操作方法
- [ ] 5.1.4 异步方法的单元测试

### 5.2 内存优化
- [ ] 5.2.1 实现 ArrayPool<T> 使用减少数组分配
- [ ] 5.2.2 优化字符串处理，使用 Span<T> 和 Memory<T>
- [ ] 5.2.3 实现对象池缓存常用对象
- [ ] 5.2.4 减少临时对象创建

### 5.3 性能基准验证
- [ ] 5.3.1 运行完整性能基准测试
- [ ] 5.3.2 验证序列化速度提升 ≥20%
- [ ] 5.3.3 验证反序列化速度提升 ≥15%
- [ ] 5.3.4 验证内存使用减少 ≥15%

## 6. 测试和验证阶段

### 6.1 功能测试
- [ ] 6.1.1 单元测试覆盖率达到 90%+
  - [ ] 6.1.1.1 核心序列化方法测试
  - [ ] 6.1.1.2 自定义转换器测试
  - [ ] 6.1.1.3 兼容性测试（新旧格式）
  - [ ] 6.1.1.4 错误处理测试
- [ ] 6.1.2 集成测试
  - [ ] 6.1.2.1 完整 GameScenario 序列化往返测试
  - [ ] 6.1.2.2 大文件处理测试
  - [ ] 6.1.2.3 并发序列化测试

### 6.2 兼容性验证
- [ ] 6.2.1 旧存档文件兼容性测试
  - [ ] 6.2.1.1 加载各版本历史存档文件
  - [ ] 6.2.1.2 验证数据完整性
  - [ ] 6.2.1.3 验证游戏功能正常
- [ ] 6.2.2 跨平台兼容性测试
- [ ] 6.2.3 AOT 编译测试
  - [ ] 6.2.3.1 Native AOT 编译成功
  - [ ] 6.2.3.2 AOT 运行时功能验证

### 6.3 性能验证
- [ ] 6.3.1 性能回归测试
- [ ] 6.3.2 内存泄漏检测
- [ ] 6.3.3 长时间运行稳定性测试
- [ ] 6.3.4 大数据量处理测试

## 7. 文档和清理阶段

### 7.1 代码文档更新
- [ ] 7.1.1 更新 SimpleSerializer 类的 XML 文档注释
- [ ] 7.1.2 添加迁移相关的代码注释
- [ ] 7.1.3 更新 GameJsonContext 文档
- [ ] 7.1.4 自定义转换器使用说明

### 7.2 用户文档
- [ ] 7.2.1 编写迁移指南文档
- [ ] 7.2.2 更新 API 文档
- [ ] 7.2.3 创建故障排除指南
- [ ] 7.2.4 性能优化建议文档

### 7.3 代码清理
- [ ] 7.3.1 移除临时兼容性代码（如果不再需要）
- [ ] 7.3.2 清理注释掉的旧代码
- [ ] 7.3.3 优化代码结构和命名
- [ ] 7.3.4 代码审查和重构

## 8. 部署和监控阶段

### 8.1 部署准备
- [ ] 8.1.1 准备部署脚本
- [ ] 8.1.2 创建回滚计划
- [ ] 8.1.3 准备监控和日志配置
- [ ] 8.1.4 用户通知和升级指南

### 8.2 监控设置
- [ ] 8.2.1 设置性能监控指标
- [ ] 8.2.2 配置异常监控和告警
- [ ] 8.2.3 迁移状态跟踪
- [ ] 8.2.4 用户反馈收集机制

## 9. 属性基测试 (Property-Based Testing)

### 9.1 序列化往返一致性测试
- [ ] 9.1.1 编写 GameScenario 序列化往返属性测试
  - **验证**: Requirements 2.1, 3.1.1, 3.1.2
  - **属性**: ∀ scenario: GameScenario. DeserializeJson(SerializeJson(scenario)) ≡ scenario
  - **生成器**: 创建有效的 GameScenario 实例，包含各种边界情况
  - **不变量**: 序列化后再反序列化的对象应与原对象在关键属性上保持一致

- [ ] 9.1.2 编写 Person 对象序列化往返属性测试
  - **验证**: Requirements 2.1, 3.1.1
  - **属性**: ∀ person: Person. DeserializeJson(SerializeJson(person)) ≡ person
  - **生成器**: 生成各种 Person 实例，包含不同的属性组合
  - **不变量**: ID、Name、属性值等关键字段保持一致

- [ ] 9.1.3 编写复杂集合类型序列化往返属性测试
  - **验证**: Requirements 3.2.1, 3.2.2
  - **属性**: ∀ list: GameObjectList. DeserializeJson(SerializeJson(list)) ≡ list
  - **生成器**: 生成包含不同数量和类型游戏对象的列表
  - **不变量**: 集合大小、元素顺序、元素内容保持一致

### 9.2 格式兼容性属性测试
- [ ] 9.2.1 编写旧格式兼容性属性测试
  - **验证**: Requirements 3.4.1, 6.2.1
  - **属性**: ∀ legacyJson: LegacyFormatJson. DeserializeJson(legacyJson) succeeds
  - **生成器**: 生成符合旧 Newtonsoft.Json 格式的 JSON 字符串
  - **不变量**: 旧格式数据能够成功反序列化且数据完整

- [ ] 9.2.2 编写格式检测准确性属性测试
  - **验证**: Requirements 2.2.1
  - **属性**: ∀ json: JsonString. IsLegacyFormat(json) ⟺ json contains legacy markers
  - **生成器**: 生成新旧两种格式的 JSON 字符串
  - **不变量**: 格式检测结果与实际格式一致

### 9.3 性能属性测试
- [ ] 9.3.1 编写序列化性能属性测试
  - **验证**: Requirements 5.1, 6.3.1
  - **属性**: ∀ obj: GameObject. SerializationTime(STJ, obj) < SerializationTime(Newtonsoft, obj) * 0.8
  - **生成器**: 生成不同大小和复杂度的游戏对象
  - **不变量**: System.Text.Json 序列化时间应比 Newtonsoft.Json 快至少 20%

- [ ] 9.3.2 编写内存使用属性测试
  - **验证**: Requirements 5.2.2, 6.3.2
  - **属性**: ∀ obj: GameObject. MemoryUsage(STJ, obj) < MemoryUsage(Newtonsoft, obj) * 0.85
  - **生成器**: 生成各种规模的对象进行内存使用测试
  - **不变量**: System.Text.Json 内存使用应比 Newtonsoft.Json 少至少 15%

### 9.4 AOT 兼容性属性测试
- [ ] 9.4.1 编写 AOT 序列化兼容性属性测试
  - **验证**: Requirements 4.3.1, 6.2.3
  - **属性**: ∀ type: RegisteredType. AOTSerialize(type) succeeds without reflection
  - **生成器**: 遍历 GameJsonContext 中注册的所有类型
  - **不变量**: 所有注册类型都能在 AOT 环境下成功序列化

- [ ] 9.4.2 编写类型注册完整性属性测试
  - **验证**: Requirements 1.1.1, 4.3.1
  - **属性**: ∀ gameObjectType: GameObjectTypes. gameObjectType ∈ RegisteredTypes
  - **生成器**: 扫描所有 GameObject 派生类
  - **不变量**: 所有游戏对象类型都在 GameJsonContext 中注册

### 9.5 错误处理属性测试
- [ ] 9.5.1 编写异常处理属性测试
  - **验证**: Requirements 4.1, 4.2
  - **属性**: ∀ invalidJson: InvalidJsonString. DeserializeJson(invalidJson) handles gracefully
  - **生成器**: 生成各种无效的 JSON 字符串
  - **不变量**: 无效输入应产生适当的异常而不是崩溃

- [ ] 9.5.2 编写数据完整性属性测试
  - **验证**: Requirements 5.2, 6.2.1
  - **属性**: ∀ scenario: GameScenario. ValidateDataIntegrity(DeserializeJson(SerializeJson(scenario))) = true
  - **生成器**: 生成包含复杂关系的 GameScenario
  - **不变量**: 序列化往返后对象关系和数据完整性保持不变

---

**任务总数**: 约 80+ 个具体任务  
**预估工期**: 10-12 个工作日  
**关键路径**: 基础设施准备 → 核心方法迁移 → 转换器迁移 → 依赖移除 → 测试验证  
**风险任务**: 自定义转换器迁移、旧格式兼容性、性能优化  

**优先级说明**:
- P0 (关键): 核心序列化方法、依赖移除、基本兼容性
- P1 (重要): 自定义转换器、性能优化、全面测试
- P2 (一般): 文档更新、监控设置、代码清理