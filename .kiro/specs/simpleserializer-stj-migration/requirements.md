# SimpleSerializer System.Text.Json Migration Requirements

## 1. 项目概述 (Project Overview)

### 1.1 背景 (Background)
中华三国志项目目前处于**混合序列化阶段**，同时使用 Newtonsoft.Json 和 System.Text.Json。为了实现 Native AOT 兼容性和提升性能，需要完全移除 Newtonsoft.Json 依赖，将 SimpleSerializer 完全迁移到 System.Text.Json。

### 1.2 目标 (Objectives)
- 完全移除 Newtonsoft.Json 依赖
- 实现 Native AOT 兼容的序列化
- 保持现有存档文件的兼容性
- 提升序列化性能
- 减少运行时反射依赖

### 1.3 范围 (Scope)
- 核心序列化器 `SimpleSerializer.cs` 的完全重写
- 项目文件中 Newtonsoft.Json 包引用的移除
- 所有编译错误的系统性修复
- 序列化兼容性验证

### 1.4 约束条件 (Constraints)
- 必须保持现有 API 签名不变
- 必须支持现有存档文件格式
- 必须通过 AOT 兼容性检查
- 不能影响游戏运行时性能

## 2. 用户故事 (User Stories)

### 2.1 作为开发者
**故事**: 作为游戏开发者，我希望序列化系统完全使用 System.Text.Json，以便支持 Native AOT 编译。

**验收标准**:
- [ ] SimpleSerializer 完全使用 System.Text.Json API
- [ ] 移除所有 Newtonsoft.Json 引用
- [ ] 项目能够成功编译
- [ ] 现有存档文件能够正常加载

### 2.2 作为性能优化者
**故事**: 作为性能优化者，我希望序列化性能得到提升，减少内存分配和 GC 压力。

**验收标准**:
- [ ] 序列化速度提升至少 20%
- [ ] 内存分配减少至少 15%
- [ ] 支持异步序列化操作
- [ ] 提供性能基准测试

### 2.3 作为AOT编译用户
**故事**: 作为希望使用 AOT 编译的用户，我希望游戏能够在 AOT 模式下正常运行。

**验收标准**:
- [ ] 通过 AOT 兼容性分析
- [ ] 所有序列化操作在 AOT 下正常工作
- [ ] 无运行时反射依赖
- [ ] 支持源生成器

### 2.4 作为质量保证工程师
**故事**: 作为质量保证工程师，我希望迁移后的系统具有完整的测试覆盖和可靠的错误处理机制。

**验收标准**:
- [ ] 单元测试覆盖率达到 90% 以上
- [ ] 集成测试覆盖所有关键场景
- [ ] 错误处理机制完善且一致
- [ ] 提供详细的日志和诊断信息

## 3. 功能需求 (Functional Requirements)

### 3.1 核心序列化方法迁移

#### 3.1.1 SerializeJson 方法
**需求**: 将 `SerializeJson<T>` 方法从 Newtonsoft.Json 迁移到 System.Text.Json

**实现要求**:
```csharp
public static string SerializeJson<T>(T t, bool zip = false, bool Indented = false, bool Net = false)
{
    // 使用 JsonSerializer.Serialize(t, GameJsonContext.Default.GetTypeInfo(typeof(T)))
    // 保持原有参数兼容性
    // 支持 GZip 压缩
}
```

**验收标准**:
- [ ] 使用 `JsonSerializer.Serialize` 替代 `JsonConvert.SerializeObject`
- [ ] 使用 `GameJsonContext.Default` 作为序列化上下文
- [ ] 保持 zip、Indented、Net 参数的兼容性
- [ ] 错误处理与原版本一致

#### 3.1.2 DeserializeJson 方法
**需求**: 将 `DeserializeJson<T>` 方法从 Newtonsoft.Json 迁移到 System.Text.Json

**实现要求**:
```csharp
public static T DeserializeJson<T>(string s, bool zip = false, bool Net = false)
{
    // 使用 JsonSerializer.Deserialize<T>(s, GameJsonContext.Default)
    // 保持向后兼容性，支持旧格式回退
}
```

**验收标准**:
- [ ] 使用 `JsonSerializer.Deserialize` 替代 `JsonConvert.DeserializeObject`
- [ ] 实现旧格式兼容性检测和回退机制
- [ ] 保持错误处理逻辑
- [ ] 支持 GZip 解压缩

### 3.2 文件序列化方法

#### 3.2.1 SerializeJsonFile 方法
**需求**: 更新文件序列化方法使用新的 JSON 序列化器

**验收标准**:
- [ ] 调用新的 `SerializeJson` 方法
- [ ] 保持文件保存逻辑不变
- [ ] 维持错误处理机制

#### 3.2.2 DeserializeJsonFile 方法
**需求**: 更新文件反序列化方法，支持新旧格式兼容

**实现要求**:
- 优先尝试 System.Text.Json 反序列化
- 失败时回退到 Newtonsoft.Json（临时兼容）
- 记录迁移状态和警告

**验收标准**:
- [ ] 实现双重反序列化策略
- [ ] 提供迁移状态日志
- [ ] 保持 GameScenario 集合初始化逻辑

### 3.3 AOT 兼容性支持

#### 3.3.1 源生成器集成
**需求**: 完善 GameJsonContext 源生成器配置

**实现要求**:
- 注册所有核心游戏对象类型
- 配置必要的序列化选项
- 支持循环引用处理

**验收标准**:
- [ ] 所有 GameObject 派生类已注册
- [ ] 配置 ReferenceHandler.Preserve
- [ ] 注册所有必要的 Dictionary 和 List 类型
- [ ] 通过 AOT 兼容性分析

#### 3.3.2 自定义转换器迁移
**需求**: 将现有 Newtonsoft.Json 转换器迁移到 System.Text.Json

**关键转换器**:
- `LegacyGameObjectListConverter` → `GameObjectListConverter`
- `LegacyDictionaryConverter` → STJ 版本
- `TextMessageTableConverter` → STJ 版本
- `ArrayToDictionaryConverter` → STJ 版本

**验收标准**:
- [ ] 所有关键转换器已迁移
- [ ] 转换器在 GameJsonContext 中注册
- [ ] 保持数据格式兼容性
- [ ] 通过单元测试验证

### 3.4 性能优化

#### 3.4.1 异步序列化支持
**需求**: 添加异步序列化方法以提升性能

**实现要求**:
```csharp
public static async Task<string> SerializeJsonAsync<T>(T t, bool zip = false, bool indented = false)
public static async Task<T> DeserializeJsonAsync<T>(string s, bool zip = false)
```

**验收标准**:
- [ ] 实现异步序列化方法
- [ ] 支持流式处理大文件
- [ ] 提供取消令牌支持
- [ ] 性能基准测试通过

#### 3.4.2 内存优化
**需求**: 优化内存使用和 GC 压力

**实现要求**:
- 使用对象池减少分配
- 优化字符串处理
- 减少临时对象创建

**验收标准**:
- [ ] 内存分配减少 15% 以上
- [ ] GC 压力降低
- [ ] 大文件处理性能提升

## 4. 技术需求 (Technical Requirements)

### 4.1 依赖移除

#### 4.1.1 项目文件更新
**需求**: 从项目文件中移除 Newtonsoft.Json 包引用

**影响文件**:
- `WorldOfTheThreeKingdoms/WorldOfTheThreeKingdoms.csproj`
- `WorldOfTheThreeKingdomsEditor/WorldOfTheThreeKingdomsEditor.csproj`

**验收标准**:
- [ ] 移除 `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />`
- [ ] 项目编译时暴露所有 Newtonsoft.Json 依赖错误
- [ ] 保留 `<PackageReference Include="System.Text.Json" Version="8.0.0" />`

#### 4.1.2 命名空间清理
**需求**: 移除所有 Newtonsoft.Json 命名空间引用

**验收标准**:
- [ ] 移除 `using Newtonsoft.Json;`
- [ ] 移除 `using Newtonsoft.Json.Linq;`
- [ ] 添加必要的 `using System.Text.Json;` 引用

### 4.2 编译错误修复

#### 4.2.1 系统性错误修复
**需求**: 修复移除 Newtonsoft.Json 后的所有编译错误

**修复策略**:
1. 识别所有使用 JsonConvert 的位置
2. 替换为对应的 JsonSerializer 调用
3. 更新自定义转换器实现
4. 修复类型转换问题

**验收标准**:
- [ ] 零编译错误
- [ ] 零编译警告（与 JSON 序列化相关）
- [ ] 所有单元测试通过

#### 4.2.2 运行时兼容性
**需求**: 确保运行时行为与原版本一致

**验收标准**:
- [ ] 游戏正常启动
- [ ] 存档加载功能正常
- [ ] 存档保存功能正常
- [ ] 网络序列化功能正常（如适用）

### 4.3 向后兼容性

#### 4.3.1 存档兼容性
**需求**: 确保现有存档文件能够正常加载

**实现策略**:
- 实现格式检测机制
- 提供旧格式转换工具
- 保持关键数据结构不变

**验收标准**:
- [ ] 旧版本存档能够加载
- [ ] 数据完整性验证通过
- [ ] 提供格式升级提示

#### 4.3.2 API 兼容性
**需求**: 保持 SimpleSerializer 公共 API 不变

**验收标准**:
- [ ] 所有公共方法签名保持不变
- [ ] 方法行为与原版本一致
- [ ] 异常处理逻辑保持一致

## 5. 非功能性需求 (Non-Functional Requirements)

### 5.1 性能需求
- 序列化速度提升 20% 以上
- 反序列化速度提升 15% 以上
- 内存使用减少 15% 以上
- 支持大文件（>100MB）处理

### 5.2 可靠性需求
- 数据完整性 100% 保证
- 错误恢复机制完善
- 异常处理覆盖率 95% 以上

### 5.3 可维护性需求
- 代码注释覆盖率 80% 以上
- 单元测试覆盖率 90% 以上
- 符合 C# 编码规范

### 5.4 AOT 兼容性需求
- 通过 AOT 兼容性分析器检查
- 零反射依赖（序列化相关）
- 支持源生成器

## 6. 验收标准 (Acceptance Criteria)

### 6.1 功能验收
- [ ] 所有核心序列化方法已迁移
- [ ] 项目成功编译，零错误零警告
- [ ] 所有单元测试通过
- [ ] 集成测试通过

### 6.2 性能验收
- [ ] 性能基准测试达标
- [ ] 内存使用优化达标
- [ ] 大文件处理测试通过

### 6.3 兼容性验收
- [ ] 旧存档文件兼容性测试通过
- [ ] AOT 编译测试通过
- [ ] 跨平台兼容性测试通过

### 6.4 质量验收
- [ ] 代码审查通过
- [ ] 文档更新完成
- [ ] 迁移指南编写完成

## 7. 风险与缓解 (Risks and Mitigation)

### 7.1 技术风险
**风险**: 复杂自定义转换器迁移困难
**缓解**: 分阶段迁移，保持双重支持过渡期

**风险**: 性能回归
**缓解**: 持续性能监控，基准测试验证

**风险**: 数据兼容性问题
**缓解**: 全面的兼容性测试，回退机制

### 7.2 项目风险 (Project Risks)
**风险**: 迁移时间超预期
**缓解**: 分阶段实施，优先级管理

**风险**: 现有功能破坏
**缓解**: 全面回归测试，渐进式部署

### 7.3 业务风险 (Business Risks)
**风险**: 用户体验受影响
**缓解**: 充分的用户测试，渐进式发布

**风险**: 数据丢失或损坏
**缓解**: 完整的备份策略，数据验证机制

## 8. 实施计划 (Implementation Plan)

### 8.1 阶段一：基础设施准备（1-2天）
- [ ] 完善 GameJsonContext 配置
- [ ] 准备性能基准测试
- [ ] 设置 AOT 兼容性检查

### 8.2 阶段二：核心方法迁移（2-3天）
- [ ] 迁移 SerializeJson 方法
- [ ] 迁移 DeserializeJson 方法
- [ ] 实现兼容性检测

### 8.3 阶段三：依赖移除（1天）
- [ ] 移除项目文件中的 Newtonsoft.Json 引用
- [ ] 修复编译错误

### 8.4 阶段四：转换器迁移（3-4天）
- [ ] 迁移关键自定义转换器
- [ ] 验证数据格式兼容性
- [ ] 性能优化

### 8.5 阶段五：测试与验证（2-3天）
- [ ] 全面功能测试
- [ ] 性能基准验证
- [ ] AOT 兼容性验证
- [ ] 文档更新

## 9. 成功指标 (Success Metrics)

### 9.1 技术指标
- 编译成功率：100%
- 单元测试通过率：100%
- 性能提升：序列化 +20%，反序列化 +15%
- 内存优化：-15%

### 9.2 质量指标
- 代码覆盖率：90%+
- AOT 兼容性：100%
- 存档兼容性：100%

### 9.3 用户体验指标
- 游戏启动时间：无回归
- 存档加载时间：提升 10%+
- 内存使用：减少 15%+

---

**文档版本**: 1.0  
**创建日期**: 2026-01-30  
**最后更新**: 2026-01-30  
**负责人**: 开发团队  
**审核人**: 技术负责人