# 需求文档：JSON 序列化架构统一

## 简介

本规格旨在统一游戏的序列化架构，将当前的双轨制（JSON + 二进制）迁移到单一的 JSON 格式（带 GZip 压缩），以简化维护、减少错误并提高代码质量。

## 术语表

- **Scenario（剧本）**：游戏的完整状态，包括所有游戏对象（势力、建筑、武将、部队等）
- **Save_File（存档文件）**：保存的游戏状态，当前使用 `.bin` 格式（二进制）
- **Configuration_File（配置文件）**：游戏的静态配置数据（CommonData），当前使用 `.json` 格式
- **Serialization（序列化）**：将对象转换为可存储的格式
- **Deserialization（反序列化）**：从存储格式恢复对象
- **Object_Reference（对象引用）**：对象之间的关联关系（如 Person.BelongedFaction）
- **ID_Based_Reference（基于 ID 的引用）**：通过 ID 而非直接引用来表示对象关系
- **Link_References（链接引用）**：反序列化后根据 ID 重建对象引用的过程
- **System_Text_Json**：.NET 的 JSON 序列化库，支持 AOT 编译
- **GZip_Compression（GZip 压缩）**：用于减小文件体积的压缩算法
- **Binary_Serialization（二进制序列化）**：当前存档使用的自定义二进制格式
- **JSON_Serialization（JSON 序列化）**：当前配置文件使用的 JSON 格式
- **Backward_Compatibility（向后兼容）**：新系统能够读取旧格式的存档

## 需求

### 需求 1：统一序列化格式

**用户故事**：作为开发者，我希望只维护一套序列化逻辑，这样可以减少重复代码和维护成本。

#### 验收标准

1. THE System SHALL 使用 JSON 作为唯一的序列化格式
2. THE System SHALL 使用 System.Text.Json 库进行序列化和反序列化
3. THE System SHALL 支持 AOT 编译（通过 JsonSerializerContext）
4. WHEN 保存游戏状态时，THE System SHALL 将所有对象序列化为 JSON
5. WHEN 加载游戏状态时，THE System SHALL 从 JSON 反序列化所有对象

### 需求 2：文件压缩和元数据

**用户故事**：作为玩家，我希望存档文件不要太大，同时浏览存档列表时不会卡顿，这样可以节省磁盘空间并提供流畅的用户体验。

#### 验收标准

1. WHEN 保存存档时，THE System SHALL 使用 GZip 压缩 JSON 数据
2. WHEN 加载存档时，THE System SHALL 解压 GZip 数据后再反序列化
3. THE Compressed_JSON_File SHALL 使用 `.sav.gz` 扩展名
4. THE Compressed_File_Size SHALL 不超过原始 JSON 大小的 1.5 倍
5. THE Compression_Process SHALL 不显著影响保存/加载性能（不超过 20% 性能损失）
6. WHEN 保存存档时，THE System SHALL 同时生成一个 `.meta` 元数据文件
7. THE Metadata_File SHALL 包含存档标题、保存时间、游戏日期、玩家势力等信息
8. THE Metadata_File SHALL 使用未压缩的 JSON 格式
9. THE Metadata_File_Size SHALL 不超过 2KB
10. WHEN 浏览存档列表时，THE System SHALL 只读取 `.meta` 文件而不解压 `.sav.gz` 文件

### 需求 3：ID 基于引用系统

**用户故事**：作为开发者，我希望避免 JSON 序列化中的循环引用问题，这样可以确保序列化的可靠性。

#### 验收标准

1. WHEN 序列化对象引用时，THE System SHALL 只保存对象的 ID
2. WHEN 序列化 Person.BelongedFaction 时，THE System SHALL 保存 FactionID 而非完整的 Faction 对象
3. WHEN 序列化集合引用时，THE System SHALL 使用 String 字段保存 ID 列表（如 "1,2,3"）
4. THE System SHALL 保持现有的 String 字段命名约定（如 TreasuresString、ArchitecturesString）
5. THE JSON_Output SHALL 不包含嵌套的对象引用

### 需求 4：引用链接恢复

**用户故事**：作为开发者，我希望反序列化后能自动恢复对象引用，这样游戏逻辑可以正常工作。

#### 验收标准

1. WHEN 反序列化完成后，THE System SHALL 执行 Link_References 阶段
2. WHEN 链接引用时，THE System SHALL 根据 ID 从集合中查找对应对象
3. WHEN 链接 Person.BelongedFaction 时，THE System SHALL 从 scenario.Factions 中查找 FactionID 对应的 Faction 对象
4. WHEN 链接集合引用时，THE System SHALL 解析 String 字段（如 "1,2,3"）并填充集合
5. FOR ALL 对象引用，链接完成后 SHALL 指向正确的对象实例

### 需求 5：向后兼容和版本迁移

**用户故事**：作为玩家，我希望能够加载旧版本的存档，并且在游戏更新后不会丢失数据，这样不会丢失游戏进度。

#### 验收标准

1. WHEN 加载 `.bin` 文件时，THE System SHALL 使用 BinaryScenarioHandler 读取
2. WHEN 加载 `.sav.gz` 文件时，THE System SHALL 使用 CompressedJsonSerializer 读取
3. THE System SHALL 根据文件扩展名自动选择正确的加载方法
4. WHEN 加载旧格式存档后，THE System SHALL 正确恢复所有游戏状态
5. THE GameScenario SHALL 包含 SaveVersion 字段记录存档格式版本
6. WHEN 加载存档时，THE System SHALL 检查 SaveVersion 并执行必要的迁移
7. THE Migration_Process SHALL 在 Load Data 阶段之后、Link References 阶段之前执行
8. FOR ALL 版本差异，THE System SHALL 提供对应的迁移逻辑（V0→V1, V1→V2, ...）
9. WHEN 迁移失败时，THE System SHALL 记录详细错误并尝试部分恢复
10. THE System SHALL 在加载旧格式后提示用户转换为新格式（可选）

### 需求 6：废弃二进制序列化

**用户故事**：作为开发者，我希望移除不再使用的二进制序列化代码，这样可以简化代码库。

#### 验收标准

1. WHEN 新格式稳定后，THE System SHALL 移除所有 SaveToBinary 方法
2. WHEN 新格式稳定后，THE System SHALL 移除所有 LoadFromBinary 方法
3. WHEN 新格式稳定后，THE System SHALL 移除 BinaryScenarioHandler 类
4. THE System SHALL 保留向后兼容代码直到确认所有用户已迁移
5. THE System SHALL 提供迁移工具将旧存档转换为新格式

### 需求 7：序列化完整性

**用户故事**：作为开发者，我希望确保所有需要保存的字段都被正确序列化，这样不会丢失数据。

#### 验收标准

1. FOR ALL 需要序列化的字段，THE System SHALL 添加 [DataMember] 或 [JsonInclude] 特性
2. FOR ALL 集合字段，THE System SHALL 提供对应的 String 字段（如 TreasuresString）
3. WHEN 保存游戏时，THE System SHALL 调用所有集合的 SaveToString 方法
4. WHEN 加载游戏时，THE System SHALL 调用所有集合的 LoadFromString 方法
5. THE System SHALL 确保 Person.Treasures、Faction.Architectures 等关键字段正确序列化

### 需求 8：解析和打印的往返一致性

**用户故事**：作为开发者，我希望序列化和反序列化是可逆的，这样可以确保数据完整性。

#### 验收标准

1. FOR ANY GameScenario 对象，序列化后再反序列化 SHALL 产生等价的对象
2. FOR ANY Person 对象，其 Treasures 集合在往返后 SHALL 包含相同的宝物
3. FOR ANY Faction 对象，其 Architectures 集合在往返后 SHALL 包含相同的建筑
4. FOR ANY 对象引用，在往返后 SHALL 指向正确的对象实例
5. THE System SHALL 提供单元测试验证往返一致性

### 需求 9：性能要求

**用户故事**：作为玩家，我希望保存和加载游戏不会太慢，这样不会影响游戏体验。

#### 验收标准

1. WHEN 保存游戏时，THE System SHALL 在 5 秒内完成（对于典型的游戏状态）
2. WHEN 加载游戏时，THE System SHALL 在 10 秒内完成（对于典型的游戏状态）
3. THE Compression_Overhead SHALL 不超过 20% 的额外时间
4. THE Link_References_Phase SHALL 不超过总加载时间的 30%
5. THE System SHALL 使用高效的字典查找进行引用链接

### 需求 10：错误处理

**用户故事**：作为玩家，我希望在加载失败时能看到清晰的错误信息，这样可以知道问题所在。

#### 验收标准

1. WHEN 文件不存在时，THE System SHALL 返回 null 并记录错误
2. WHEN 文件格式无效时，THE System SHALL 抛出描述性异常
3. WHEN 反序列化失败时，THE System SHALL 记录详细的错误信息（包括字段名和值）
4. WHEN 引用链接失败时，THE System SHALL 指出哪个对象的哪个引用无法解析
5. THE System SHALL 在控制台输出诊断信息以帮助调试

### 需求 11：配置文件处理

**用户故事**：作为开发者，我希望 CommonData 始终从文件加载，这样可以避免序列化大量静态数据。

#### 验收标准

1. THE System SHALL 不在存档中保存 CommonData
2. WHEN 加载存档时，THE System SHALL 从 `Content/Data/CommonData.xml` 加载 CommonData
3. WHEN 加载存档时，THE System SHALL 从 `Content/Data/GameParameters.xml` 加载 Parameters
4. WHEN 加载存档时，THE System SHALL 从 `Content/Data/GlobalVariables.xml` 加载 GlobalVariables
5. THE System SHALL 确保 CommonData 在引用链接前已正确加载

### 需求 12：测试覆盖

**用户故事**：作为开发者，我希望有完善的测试确保序列化系统的正确性，这样可以避免回归错误。

#### 验收标准

1. THE System SHALL 提供单元测试验证基本序列化功能
2. THE System SHALL 提供集成测试验证完整的保存/加载流程
3. THE System SHALL 提供测试验证向后兼容性
4. THE System SHALL 提供测试验证引用链接的正确性
5. THE System SHALL 提供测试验证压缩功能的正确性
