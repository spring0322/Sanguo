# Implementation Plan: JSON Serialization Unified Architecture

## Overview

本实施计划将游戏序列化系统从二进制格式迁移到 JSON 格式，采用三阶段架构（Save Data → Load Data → Link References），使用 System.Text.Json + GZip 压缩，优化集合序列化性能，并保持向后兼容性。

实施策略：
1. 先建立基础设施（DTO 类、JsonContext）
2. 实现核心序列化流程（Save/Load/Link）
3. 添加兼容性支持（旧格式读取、数据迁移）
4. 实现验证和错误处理
5. 性能优化和测试

## Tasks

- [ ] 1. 创建 DTO 类和 JsonSerializerContext
  - [ ] 1.1 创建基础 DTO 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/DTOs/` 目录下创建 DTO 类
    - 创建 `GameScenarioDTO.cs` - 包含所有顶层集合（Persons, Factions, Architectures 等）
    - 创建 `PersonDTO.cs` - 包含 ID 引用字段（BelongedFactionID）和集合字段（TreasureIDs）
    - 创建 `FactionDTO.cs` - 包含集合字段（ArchitectureIDs）
    - 创建 `TreasureDTO.cs` - 基础属性
    - 创建 `LegionDTO.cs` - 包含集合字段（TroopIDs）
    - 创建 `TroopDTO.cs` - 包含 ID 引用字段
    - 创建 `SectionDTO.cs` - 包含集合字段（ArchitectureIDs）
    - _Requirements: 2.1, 2.4_
  
  - [ ] 1.2 创建多态 DTO 类
    - 创建 `ArchitectureDTO.cs` 基类，添加 `[JsonDerivedType]` 属性声明派生类
    - 创建 `CityDTO.cs` 继承 ArchitectureDTO，添加 City 特有属性（Population, Agriculture, Commerce）
    - 创建 `PortDTO.cs` 继承 ArchitectureDTO，添加 Port 特有属性（ShipCapacity）
    - 创建 `GateDTO.cs` 继承 ArchitectureDTO，添加 Gate 特有属性（Endurance）
    - 为每个派生类指定 typeDiscriminator（"city", "port", "gate"）
    - _Requirements: 10.1, 10.3, 10.5_
  
  - [ ] 1.3 添加 MOD 扩展数据支持
    - 在所有核心 DTO 类中添加 `[JsonExtensionData]` 属性的 `ExtensionData` 字典
    - 在 GameScenarioDTO, PersonDTO, FactionDTO, ArchitectureDTO 中实现
    - _Requirements: 12.1, 12.4_
  
  - [ ] 1.4 添加私有成员序列化支持
    - 识别需要序列化的私有 setter 属性（如 Person.Loyalty）
    - 在 DTO 类中添加 `[JsonInclude]` 属性标记这些属性
    - _Requirements: 11.1, 11.2_
  
  - [ ] 1.5 添加向后兼容字段
    - 在 PersonDTO 中添加 `TreasuresString` 字段（标记为 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`）
    - 在 FactionDTO 中添加 `ArchitecturesString` 字段
    - 在 LegionDTO 中添加 `TroopsString` 字段
    - 在 SectionDTO 中添加 `ArchitecturesString` 字段
    - _Requirements: 4.3, 6.5_
  
  - [ ] 1.6 创建 GameJsonContext
    - 在 `WorldOfTheThreeKingdoms/Serialization/` 目录下创建 `GameJsonContext.cs`
    - 添加 `[JsonSerializable]` 属性声明所有 DTO 类型
    - 显式声明集合类型（List<PersonDTO>, List<int>, Dictionary<int, PersonDTO> 等）
    - 配置 JsonSourceGenerationOptions（WriteIndented=false, PropertyNamingPolicy=CamelCase）
    - _Requirements: 2.6, 2.7, 2.8_
  
  - [ ] 1.7 创建 SaveMetadata 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/` 目录下创建 `SaveMetadata.cs`
    - 包含字段：ScenarioName, SaveTime, CurrentTurn, GameVersion, PlayerFactionID
    - 在 GameJsonContext 中声明此类型
    - _Requirements: 13.1, 13.2_


- [x] 2. 实现 Save Data Phase
  - [x] 2.1 创建 SaveDataPhase 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/Phases/` 目录下创建 `SaveDataPhase.cs`
    - 实现 `ConvertToDTO(GameScenario scenario)` 方法，返回 GameScenarioDTO
    - 实现对象引用到 ID 的转换逻辑（如 person.BelongedFaction → personDTO.BelongedFactionID）
    - 实现集合引用到 List<int> 的转换（如 person.Treasures → personDTO.TreasureIDs）
    - 处理 null 引用（设置 ID 为 -1）
    - _Requirements: 1.1, 1.4, 3.1_
  
  - [x] 2.2 实现 Person 转换逻辑
    - 创建 `ConvertPersonToDTO(Person person)` 方法
    - 转换 BelongedFaction 为 BelongedFactionID
    - 转换 Treasures 集合为 TreasureIDs
    - 复制所有基础属性（Name, Loyalty 等）
    - _Requirements: 1.4, 2.4, 3.1_
  
  - [x] 2.3 实现 Faction 转换逻辑
    - 创建 `ConvertFactionToDTO(Faction faction)` 方法
    - 转换 Architectures 集合为 ArchitectureIDs
    - 复制所有基础属性
    - _Requirements: 1.4, 3.1, 6.2_
  
  - [x] 2.4 实现 Architecture 多态转换逻辑
    - 创建 `ConvertArchitectureToDTO(Architecture architecture)` 方法
    - 根据实际类型（City, Port, Gate）创建对应的 DTO
    - 转换 BelongedFaction 和 BelongedSection 为 ID
    - 复制派生类特有属性
    - _Requirements: 10.1, 10.2_
  
  - [x] 2.5 实现 Legion 和 Troop 转换逻辑
    - 创建 `ConvertLegionToDTO(Legion legion)` 方法，转换 Troops 为 TroopIDs
    - 创建 `ConvertTroopToDTO(Troop troop)` 方法，转换对象引用为 ID
    - _Requirements: 1.4, 6.3_
  
  - [x] 2.6 实现 Section 转换逻辑
    - 创建 `ConvertSectionToDTO(Section section)` 方法
    - 转换 Architectures 集合为 ArchitectureIDs
    - _Requirements: 1.4, 6.4_


- [x] 3. 实现 Load Data Phase
  - [x] 3.1 创建 LoadDataPhase 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/Phases/` 目录下创建 `LoadDataPhase.cs`
    - 实现 `LoadFromDTO(GameScenarioDTO dto)` 方法，返回 GameScenario
    - 创建所有游戏对象但不链接引用
    - 建立 ID → Object 映射（Dictionary<int, Person> 等）
    - 保留 ID 字段用于后续链接
    - _Requirements: 1.2_
  
  - [x] 3.2 实现 Person 加载逻辑
    - 创建 `LoadPersonFromDTO(PersonDTO dto)` 方法
    - 创建 Person 对象，复制基础属性
    - 保存 BelongedFactionID，但不设置 BelongedFaction 引用
    - 保存 TreasureIDs，但不填充 Treasures 集合
    - _Requirements: 1.2_
  
  - [x] 3.3 实现 Faction 加载逻辑
    - 创建 `LoadFactionFromDTO(FactionDTO dto)` 方法
    - 创建 Faction 对象，复制基础属性
    - 保存 ArchitectureIDs，但不填充 Architectures 集合
    - _Requirements: 1.2_
  
  - [x] 3.4 实现 Architecture 多态加载逻辑
    - 创建 `LoadArchitectureFromDTO(ArchitectureDTO dto)` 方法
    - 根据 DTO 的实际类型（CityDTO, PortDTO, GateDTO）创建对应的游戏对象
    - 复制派生类特有属性
    - 保存 ID 引用字段
    - _Requirements: 1.2, 10.4_
  
  - [x] 3.5 实现 Legion, Troop, Section 加载逻辑
    - 创建相应的 Load 方法
    - 保存 ID 集合字段，不填充对象引用
    - _Requirements: 1.2_


- [x] 4. 实现 Link References Phase
  - [x] 4.1 创建 IReferenceLinker 接口和 LinkReferencesPhase 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/Phases/` 目录下创建 `LinkReferencesPhase.cs`
    - 定义 `IReferenceLinker<T>` 接口，包含 `LinkReferences(T obj, GameScenario scenario)` 方法
    - 实现 `LinkReferences(GameScenario scenario)` 主方法，协调所有链接器
    - _Requirements: 1.3, 5.1_
  
  - [x] 4.2 实现 PersonReferenceLinker
    - 创建 `PersonReferenceLinker : IReferenceLinker<Person>` 类
    - 实现 BelongedFaction 引用链接（从 scenario.Factions 查找）
    - 实现 Treasures 集合链接（从 scenario.Treasures 查找）
    - 处理无效引用：记录警告，重置 ID 为 -1，设置引用为 null
    - _Requirements: 5.2, 5.3, 5.4, 5.6, 5.7_
  
  - [x] 4.3 实现 FactionReferenceLinker
    - 创建 `FactionReferenceLinker : IReferenceLinker<Faction>` 类
    - 实现 Architectures 集合链接（从 scenario.Architectures 查找）
    - 处理无效引用：跳过不存在的 Architecture，记录警告
    - _Requirements: 5.2, 5.3, 5.4_
  
  - [x] 4.4 实现 ArchitectureReferenceLinker
    - 创建 `ArchitectureReferenceLinker : IReferenceLinker<Architecture>` 类
    - 实现 BelongedFaction 引用链接
    - 实现 BelongedSection 引用链接
    - 处理无效引用
    - _Requirements: 5.2, 5.3, 5.4_
  
  - [x] 4.5 实现 LegionReferenceLinker
    - 创建 `LegionReferenceLinker : IReferenceLinker<Legion>` 类
    - 实现 Troops 集合链接（从 scenario.Troops 查找）
    - 处理无效引用
    - _Requirements: 5.2, 5.3, 5.4_
  
  - [x] 4.6 实现 TroopReferenceLinker
    - 创建 `TroopReferenceLinker : IReferenceLinker<Troop>` 类
    - 实现所有对象引用链接（Leader, Army, BelongedFaction 等）
    - 处理无效引用
    - _Requirements: 5.2, 5.3, 5.4_
  
  - [x] 4.7 实现 SectionReferenceLinker
    - 创建 `SectionReferenceLinker : IReferenceLinker<Section>` 类
    - 实现 Architectures 集合链接
    - 处理无效引用
    - _Requirements: 5.2, 5.3, 5.4_


- [x] 5. 实现 Validation Phase
  - [x] 5.1 创建 ValidationReport 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/` 目录下创建 `ValidationReport.cs`
    - 实现 Errors, Warnings, Fixes 列表
    - 实现 AddError, AddWarning, AddFix 方法（同时记录日志）
    - 实现 GetSummary 方法，生成格式化的报告
    - _Requirements: 9.3, 9.6_
  
  - [x] 5.2 创建 ValidationPhase 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/Phases/` 目录下创建 `ValidationPhase.cs`
    - 实现 `Validate(GameScenario scenario)` 方法，返回 ValidationReport
    - _Requirements: 9.1, 9.2_
  
  - [x] 5.3 实现数据验证规则
    - 验证 Person 的 BelongedFaction 引用一致性（ID > 0 但引用为 null）
    - 验证 Faction 的 Architectures 集合完整性
    - 验证 Architecture 的 BelongedFaction 引用
    - 验证 Legion 的 Troops 集合
    - _Requirements: 9.4, 9.5_
  
  - [x] 5.4 实现数据清洗规则
    - 当 Person 引用无效 Faction 时，重置为无归属状态（BelongedFactionID = -1）
    - 当集合包含无效 ID 时，从集合中移除
    - 记录所有清洗操作到 ValidationReport
    - _Requirements: 9.2, 9.4_


- [x] 6. 实现 SerializationManager
  - [x] 6.1 创建 SerializationManager 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/` 目录下创建 `SerializationManager.cs`
    - 注入依赖：SaveDataPhase, LoadDataPhase, LinkReferencesPhase, ValidationPhase
    - _Requirements: 2.2, 2.3_
  
  - [x] 6.2 实现 SaveGame 方法
    - 实现方法签名：`void SaveGame(GameScenario scenario, string filePath)`
    - 调用 SaveDataPhase.ConvertToDTO 转换为 DTO
    - 使用 JsonSerializer.Serialize 序列化（使用 GameJsonContext）
    - 创建 GZipStream 压缩并写入文件（.sav.gz）
    - 调用 WriteMetadata 创建 .meta 文件
    - 添加 try-catch 错误处理
    - _Requirements: 2.2, 2.3, 13.1, 13.3_
  
  - [x] 6.3 实现 LoadGame 方法
    - 实现方法签名：`GameScenario LoadGame(string filePath)`
    - 检测文件格式（.bin vs .sav.gz）
    - 对于 .sav.gz：使用 GZipStream 解压，JsonSerializer.Deserialize 反序列化
    - 对于 .bin：调用 LegacyFormatReader（稍后实现）
    - 调用 LoadDataPhase.LoadFromDTO 创建游戏对象
    - 调用 LinkReferencesPhase.LinkReferences 链接引用
    - 调用 ValidationPhase.Validate 验证数据
    - 记录 ValidationReport
    - 添加 try-catch 错误处理
    - _Requirements: 2.2, 2.3, 9.1, 13.2_
  
  - [x] 6.4 实现 WriteMetadata 方法
    - 实现方法签名：`void WriteMetadata(GameScenario scenario, string metaFilePath)`
    - 创建 SaveMetadata 对象（ScenarioName, SaveTime, CurrentTurn, GameVersion, PlayerFactionID）
    - 序列化为 JSON（不压缩）
    - 写入 .meta 文件
    - 确保文件大小 < 1KB
    - _Requirements: 13.1, 13.2, 13.5_
  
  - [x] 6.5 实现格式检测逻辑
    - 创建 `DetectFileFormat(string filePath)` 方法
    - 根据文件扩展名判断格式
    - 返回枚举值（Binary, JsonGzip）
    - _Requirements: 4.1_


- [-] 7. Checkpoint - 核心功能验证
  - [x] 7.1 手动测试核心序列化流程
    - 创建简单的 GameScenario（包含几个 Person, Faction, Architecture）
    - 调用 SaveGame 保存
    - 验证 .sav.gz 和 .meta 文件已创建
    - 调用 LoadGame 加载
    - 验证所有对象和引用正确恢复
    - 如有问题，修复后继续
    - _Requirements: 1.1, 1.2, 1.3_


- [x] 8. 实现 LegacyFormatReader
  - [x] 8.1 创建 LegacyFormatReader 类
    - 在 `WorldOfTheThreeKingdoms/Serialization/Legacy/` 目录下创建 `LegacyFormatReader.cs`
    - 实现 `ReadBinaryFormat(string filePath)` 方法，返回 GameScenarioDTO
    - _Requirements: 4.1, 4.2_
  
  - [x] 8.2 实现二进制格式读取逻辑
    - 使用现有的二进制反序列化代码读取 .bin 文件
    - 将二进制数据转换为游戏对象
    - _Requirements: 4.1, 4.2_
  
  - [x] 8.3 实现二进制到 DTO 的转换
    - 创建 `ConvertBinaryToDTO(GameScenario scenario)` 方法
    - 将从二进制加载的游戏对象转换为 DTO
    - 调用 SaveDataPhase 的转换逻辑（复用代码）
    - _Requirements: 4.2_
  
  - [x] 8.4 实现 String 集合迁移逻辑
    - 创建 `MigrateStringCollections(GameScenarioDTO dto)` 方法
    - 对于 PersonDTO：如果 TreasuresString 不为空，解析为 TreasureIDs，清空 TreasuresString
    - 对于 FactionDTO：迁移 ArchitecturesString → ArchitectureIDs
    - 对于 LegionDTO：迁移 TroopsString → TroopIDs
    - 对于 SectionDTO：迁移 ArchitecturesString → ArchitectureIDs
    - _Requirements: 4.3, 6.1, 6.2, 6.3, 6.4_
  
  - [x] 8.5 集成到 SerializationManager
    - 在 SerializationManager.LoadGame 中调用 LegacyFormatReader
    - 当检测到 .bin 文件时，使用 LegacyFormatReader 读取
    - _Requirements: 4.1, 4.4_


- [x] 9. 实现向后兼容性支持
  - [x] 9.1 修改游戏对象类添加序列化字段
    - 在 `Person` 类中添加 `public int BelongedFactionID { get; set; }` 和 `public List<int> TreasureIDs { get; set; }`
    - 在 `Faction` 类中添加 `public List<int> ArchitectureIDs { get; set; }`
    - 在 `Legion` 类中添加 `public List<int> TroopIDs { get; set; }`
    - 在 `Section` 类中添加 `public List<int> ArchitectureIDs { get; set; }`
    - 标记旧的 String 字段为 `[Obsolete("Use List<int> instead")]`
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  
  - [x] 9.2 实现字段优先级逻辑
    - 在 DTO 类中：当 List<int> 字段存在时，标记 String 字段为 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
    - 在 LoadDataPhase 中：优先使用 List<int> 字段，如果为空则回退到 String 字段
    - 确保序列化时只输出 List<int> 字段
    - _Requirements: 4.5, 6.5_


- [x] 10. 实现错误处理
  - [x] 10.1 实现序列化错误处理
    - 在 SerializationManager.SaveGame 中添加 try-catch 块
    - 捕获 JsonException，记录详细错误信息（对象类型、文件路径）
    - 抛出自定义 SerializationException
    - _Requirements: 8.1_
  
  - [x] 10.2 实现反序列化错误处理
    - 在 SerializationManager.LoadGame 中添加 try-catch 块
    - 捕获 JsonException，记录详细错误信息
    - 实现 `TryRecoverFromBackup(string filePath, out GameScenarioDTO dto)` 方法
    - 抛出自定义 DeserializationException
    - _Requirements: 8.2, 8.5_
  
  - [x] 10.3 实现引用链接错误处理
    - 在所有 ReferenceLinker 中使用 TryGetValue 查找对象
    - 当查找失败时，记录警告日志（包含对象 ID、引用 ID）
    - 重置无效引用为安全值（-1 或 null）
    - _Requirements: 8.3_
  
  - [x] 10.4 实现压缩错误处理
    - 在 LoadGame 中捕获 InvalidDataException（GZip 解压失败）
    - 实现 `TryReadUncompressed(string filePath, out string json)` 回退方法
    - 抛出自定义 CorruptedFileException
    - _Requirements: 8.5_


- [x] 11. 更新游戏加载界面
  - [x] 11.1 修改存档列表加载逻辑
    - 在 `MGSStartLoad.cs` 中修改存档扫描逻辑
    - 优先读取 .meta 文件获取存档信息
    - 解析 SaveMetadata（ScenarioName, SaveTime, CurrentTurn）
    - 在 UI 中显示存档元数据
    - _Requirements: 13.4, 13.6_
  
  - [x] 11.2 实现 .meta 缺失时的回退逻辑
    - 当 .meta 文件不存在时，读取 .sav.gz 文件头部
    - 部分解压获取基本信息（避免完全解压）
    - 显示基本信息或占位符
    - _Requirements: 13.6_
  
  - [x] 11.3 更新存档加载调用
    - 修改 MGSStartLoad 中的加载逻辑，调用 SerializationManager.LoadGame
    - 移除旧的二进制反序列化代码（保留为备份）
    - 处理 ValidationReport，向用户显示警告（如有）
    - _Requirements: 9.6_


- [x] 12. 实现性能优化
  - [x] 12.1 优化集合序列化
    - 确认所有集合字段使用 List<int> 而非 String
    - 在 SaveDataPhase 中使用 LINQ Select 直接创建 List<int>（避免中间分配）
    - 移除不必要的 ToList() 调用
    - _Requirements: 3.3, 3.4_
  
  - [x] 12.2 优化 GZip 压缩参数
    - 测试不同的 CompressionLevel（Fastest, Optimal, SmallestSize）
    - 测量压缩时间和文件大小
    - 选择平衡点（推荐 Optimal）
    - _Requirements: 2.2_
  
  - [x] 12.3 优化引用查找性能
    - 确认所有查找使用 Dictionary<int, T> 而非 List<T>.Find
    - 在 LinkReferencesPhase 中预先构建查找表
    - _Requirements: 5.5_


- [x] 13. Final Checkpoint - 完整系统验证
  - [x] 13.1 端到端测试
    - 使用真实的游戏存档测试完整流程
    - 保存当前游戏状态
    - 加载保存的存档
    - 验证游戏可以正常继续
    - _Requirements: 所有需求_
  
  - [x] 13.2 旧格式迁移测试
    - 准备几个旧的 .bin 存档文件
    - 使用 SerializationManager.LoadGame 加载
    - 验证数据正确迁移
    - 保存为新格式
    - 重新加载验证
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  
  - [x] 13.3 性能验证
    - 测量大型存档（1000+ 对象）的保存/加载时间
    - 对比新旧格式的性能
    - 验证内存使用合理
    - _Requirements: 3.3, 3.4, 7.1, 7.2, 7.3_
  
  - [x] 13.4 错误处理验证
    - 测试损坏的 JSON 文件
    - 测试无效引用的处理
    - 测试压缩错误的处理
    - 验证所有错误都有清晰的日志
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [x] 13.5 MOD 兼容性验证
    - 创建包含未知字段的 JSON 存档
    - 加载并验证 ExtensionData 保留
    - 保存并验证未知字段仍然存在
    - _Requirements: 12.1, 12.3_


## Optional Tasks (Testing and Benchmarking)

以下任务为可选任务，主要用于测试和性能验证。可以在核心功能完成后根据需要实施。

- [ ]* 14. 编写单元测试
  - [ ]* 14.1 DTO 序列化测试
    - 测试 PersonDTO 的基本序列化和反序列化
    - 测试多态类型（CityDTO, PortDTO, GateDTO）的序列化
    - 测试 ExtensionData 的保留
    - _Requirements: 10.2, 12.1_
  
  - [ ]* 14.2 Save Data Phase 测试
    - 测试对象引用转换为 ID
    - 测试集合转换为 List<int>
    - 测试 null 引用处理
    - _Requirements: 1.4, 2.4, 3.1_
  
  - [ ]* 14.3 Load Data Phase 测试
    - 测试从 DTO 创建游戏对象
    - 测试 ID 映射的正确性
    - 测试多态类型实例化
    - _Requirements: 1.2, 10.4_
  
  - [ ]* 14.4 Link References Phase 测试
    - 测试有效引用的链接
    - 测试无效引用的处理（不崩溃）
    - 测试无效 FactionID 被重置为 -1
    - 测试无效 Treasure 引用被跳过
    - _Requirements: 5.3, 5.6, 5.7, 9.7_
  
  - [ ]* 14.5 Validation Phase 测试
    - 测试无效引用的检测
    - 测试数据清洗规则的应用
    - 测试 ValidationReport 的生成
    - _Requirements: 9.4, 9.6_
  
  - [ ]* 14.6 SerializationManager 测试
    - 测试 .meta 文件的创建和内容
    - 测试 .meta 文件大小 < 1KB
    - 测试格式检测（.bin vs .sav.gz）
    - _Requirements: 13.1, 13.2, 13.5_
  
  - [ ]* 14.7 LegacyFormatReader 测试
    - 测试 .bin 文件的检测和加载
    - 测试 TreasuresString → TreasureIDs 的迁移
    - 测试加载后保存为新格式
    - _Requirements: 4.1, 4.3, 4.4_
  
  - [ ]* 14.8 字段优先级测试
    - 测试当两个字段都存在时，只序列化 List<int>
    - 测试从旧格式迁移时的正确性
    - _Requirements: 4.5, 6.5_
  
  - [ ]* 14.9 错误处理测试
    - 测试无效 JSON 的错误消息
    - 测试损坏文件的恢复
    - 测试引用链接失败的日志
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ]* 14.10 存档列表加载测试
    - 测试 .meta 文件的快速加载
    - 测试 .meta 缺失时的回退
    - _Requirements: 13.4, 13.6_

- [ ]* 15. 编写属性测试（Property-Based Testing）
  - [ ]* 15.1 创建测试数据生成器
    - 创建 GameScenarioGenerator（生成随机 GameScenario）
    - 创建 PersonGenerator（生成随机 Person）
    - 创建 FactionGenerator（生成随机 Faction）
    - 创建 ArchitectureGenerator（生成随机 City/Port/Gate）
    - 创建 PersonDTOWithExtensionDataGenerator（生成包含未知字段的 DTO）
    - 创建 GameScenarioWithInvalidRefsGenerator（生成包含无效引用的场景）
    - _Requirements: 所有属性测试需要_
  
  - [ ]* 15.2 Property 1: Serialization Round-Trip Preserves Game State
    - 使用 FsCheck 生成随机 GameScenario
    - 序列化后反序列化
    - 验证游戏状态等价
    - 最少运行 100 次迭代
    - _Validates: Requirements 1.4, 2.1, 2.2_
  
  - [ ]* 15.3 Property 2: Object References Serialize as IDs Only
    - 生成随机 Person 对象
    - 序列化为 JSON
    - 验证 JSON 只包含 "belongedFactionID"，不包含嵌套的 "belongedFaction" 对象
    - _Validates: Requirements 1.4, 2.4_
  
  - [ ]* 15.4 Property 3: GZip Compression is Applied
    - 保存游戏到 .sav.gz 文件
    - 使用 GZipStream 解压
    - 验证解压后是有效的 JSON
    - _Validates: Requirements 2.2_
  
  - [ ]* 15.5 Property 4: Collections Serialize as JSON Arrays
    - 生成随机 ID 数组
    - 序列化 PersonDTO
    - 验证 JSON 包含 "treasureIDs":[1,2,3]，不包含 "treasureIDs":"1,2,3"
    - _Validates: Requirements 3.1_
  
  - [ ]* 15.6 Property 5: List<int> Serialization Reduces Memory Allocations
    - 对比 String 和 List<int> 的序列化
    - 使用 BenchmarkDotNet 的 MemoryDiagnoser
    - 验证 List<int> 的内存分配更少
    - _Validates: Requirements 3.4_
  
  - [ ]* 15.7 Property 6: Invalid References Don't Crash
    - 生成包含无效引用的 GameScenario
    - 调用 LinkReferences
    - 验证不抛出异常
    - _Validates: Requirements 5.3, 9.7_
  
  - [ ]* 15.8 Property 7: List<int> Takes Priority Over String
    - 生成同时包含 TreasureIDs 和 TreasuresString 的 PersonDTO
    - 序列化
    - 验证 JSON 只包含 "treasureIDs"，不包含 "treasuresString"
    - _Validates: Requirements 6.5_
  
  - [ ]* 15.9 Property 8: Polymorphic Types Preserve All Properties
    - 生成随机 Architecture（City/Port/Gate）
    - 序列化后反序列化
    - 验证所有属性（包括派生类特有属性）都保留
    - _Validates: Requirements 10.2_
  
  - [ ]* 15.10 Property 9: Correct Derived Type is Instantiated
    - 生成随机 Architecture
    - 序列化后反序列化
    - 验证反序列化的对象类型与原始类型一致
    - _Validates: Requirements 10.4_
  
  - [ ]* 15.11 Property 10: Type Discriminators Present in JSON
    - 生成随机 Architecture
    - 序列化
    - 验证 JSON 包含 "$type" 字段
    - _Validates: Requirements 10.5_
  
  - [ ]* 15.12 Property 11: Private Setters with JsonInclude Serialize
    - 生成包含私有 setter 属性的对象
    - 序列化后反序列化
    - 验证私有 setter 属性的值正确恢复
    - _Validates: Requirements 11.1_
  
  - [ ]* 15.13 Property 12: Unknown Fields Are Preserved
    - 生成包含未知字段的 JSON
    - 反序列化
    - 验证 ExtensionData 包含未知字段
    - _Validates: Requirements 12.1_
  
  - [ ]* 15.14 Property 13: ExtensionData Round-Trips Correctly
    - 生成包含 ExtensionData 的 PersonDTO
    - 序列化后反序列化
    - 验证 ExtensionData 完全保留
    - _Validates: Requirements 12.3_

- [ ]* 16. 创建性能基准测试
  - [ ]* 16.1 设置 BenchmarkDotNet
    - 创建 `SerializationBenchmarks.cs` 类
    - 添加 `[MemoryDiagnoser]` 属性
    - 实现 GlobalSetup 方法创建测试数据
    - _Requirements: 7.1_
  
  - [ ]* 16.2 String vs List<int> 对比基准
    - 创建 SerializeWithStringCollections 基准方法
    - 创建 SerializeWithListIntCollections 基准方法
    - 测量时间和内存分配
    - _Requirements: 7.2, 7.4_
  
  - [ ]* 16.3 完整 Save/Load 周期基准
    - 创建 FullSaveLoadCycle 基准方法
    - 使用大型场景（1000+ 对象）
    - 测量总时间和 GC 次数
    - _Requirements: 7.3, 7.5_
  
  - [ ]* 16.4 运行基准测试并记录结果
    - 运行所有基准测试
    - 记录性能指标（时间、内存、GC）
    - 对比 String 和 List<int> 的性能差异
    - 生成性能报告
    - _Requirements: 7.2, 7.3, 7.4_

- [ ]* 17. 创建集成测试
  - [ ]* 17.1 完整工作流集成测试
    - 测试 Save → Load → Validate 完整流程
    - 测试复杂场景的序列化（多层引用、循环引用）
    - 测试引用链接的正确性
    - _Requirements: 1.1, 1.2, 1.3, 9.1_
  
  - [ ]* 17.2 旧格式迁移集成测试
    - 测试 .bin → .sav.gz 迁移
    - 测试迁移后的数据完整性
    - 测试迁移后游戏可以正常运行
    - _Requirements: 4.1, 4.4_

## Notes


## Notes

- **任务优先级**: 核心任务（1-13）必须完成，可选任务（14-17）可根据时间和需求选择性实施
- **每个任务都引用了具体的需求编号**，便于追溯和验证
- **Checkpoint 任务**（7, 13）确保增量验证，及时发现问题
- **测试策略**: 
  - 单元测试验证具体示例和边界情况
  - 属性测试验证通用正确性属性
  - 集成测试验证完整工作流
  - 性能测试验证优化效果
- **实施建议**: 按顺序执行核心任务，每完成一个阶段进行测试验证
- **错误处理**: 所有错误都应有清晰的日志和用户友好的错误消息
- **向后兼容**: 确保旧存档可以正常加载并自动迁移到新格式

## Implementation Phases

### Phase 1: 基础设施 (Tasks 1)
创建所有 DTO 类、JsonContext、元数据类。这是整个系统的基础。

### Phase 2: 核心序列化 (Tasks 2-6)
实现三阶段序列化架构和 SerializationManager。这是系统的核心功能。

### Phase 3: 兼容性支持 (Tasks 8-9)
实现旧格式读取和向后兼容性。确保平滑迁移。

### Phase 4: 错误处理和优化 (Tasks 10-12)
添加健壮的错误处理和性能优化。提升系统可靠性和性能。

### Phase 5: 集成和验证 (Tasks 11, 13)
更新游戏界面，进行完整的端到端验证。

### Phase 6: 测试和基准 (Tasks 14-17, Optional)
编写全面的测试套件和性能基准。确保质量和性能目标。
