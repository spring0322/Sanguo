# Requirements Document

## Introduction

本文档定义了将游戏序列化架构从二进制格式统一迁移到 JSON 格式的需求。核心目标是建立一个基于三阶段架构（Save Data → Load Data → Link References）的序列化系统，使用 System.Text.Json + GZip 压缩，并优化集合序列化性能，同时保持向后兼容性。

## Glossary

- **Serialization_System**: 负责将游戏对象转换为可持久化格式的系统
- **Save_Data_Phase**: 序列化的第一阶段，将对象转换为只包含 ID 的数据结构
- **Load_Data_Phase**: 反序列化的第二阶段，从 JSON 恢复基础数据结构
- **Link_References_Phase**: 反序列化的第三阶段，根据 ID 重建对象引用关系
- **ID_Reference**: 使用整数 ID 代替对象引用的序列化策略
- **Collection_Serializer**: 负责序列化集合类型（如 List、Array）的组件
- **Legacy_Format**: 现有的二进制序列化格式（.bin 文件）
- **New_Format**: 新的 JSON + GZip 压缩格式（.sav.gz 文件）
- **Backward_Compatibility**: 系统能够读取旧格式存档的能力
- **Reference_Resolver**: 负责在 Link_References_Phase 中根据 ID 查找并重建对象引用的组件
- **GC_Pressure**: 垃圾回收器的内存分配和回收压力
- **AOT_Compatibility**: Ahead-of-Time 编译兼容性，确保代码可以预编译
- **JsonSerializerContext**: System.Text.Json 的源生成器上下文，用于 AOT 编译时的类型声明
- **JsonDerivedType**: System.Text.Json 的属性，用于声明多态类型的派生类
- **JsonInclude**: System.Text.Json 的属性，用于标记私有 setter 属性应被序列化
- **JsonConstructor**: System.Text.Json 的属性，用于指定反序列化时使用的构造函数
- **JsonExtensionData**: System.Text.Json 的属性，用于保留未知的 JSON 字段
- **Meta_File**: 存档元数据文件，包含存档的基本信息，用于快速显示存档列表

## Requirements

### Requirement 1: 三阶段序列化架构

**User Story:** 作为开发者，我希望实现一个清晰的三阶段序列化架构，以便将对象引用和数据存储分离，提高序列化的可维护性和可靠性。

#### Acceptance Criteria

1. THE Serialization_System SHALL implement a Save_Data_Phase that converts object references to ID references
2. THE Serialization_System SHALL implement a Load_Data_Phase that deserializes JSON into data structures with ID fields
3. THE Serialization_System SHALL implement a Link_References_Phase that resolves ID references back to object references
4. WHEN serializing an object with references, THE Save_Data_Phase SHALL store only the ID of the referenced object
5. WHEN deserializing, THE Load_Data_Phase SHALL complete before THE Link_References_Phase begins

### Requirement 2: JSON 序列化格式

**User Story:** 作为开发者，我希望使用 System.Text.Json 作为序列化引擎，并使用 GZip 压缩，以便获得良好的性能和文件大小。

#### Acceptance Criteria

1. THE Serialization_System SHALL use System.Text.Json for JSON serialization and deserialization
2. THE Serialization_System SHALL apply GZip compression to serialized JSON data
3. WHEN saving game data, THE Serialization_System SHALL produce files with .sav.gz extension
4. THE Serialization_System SHALL serialize object references as integer ID fields in JSON
5. WHEN serializing a Person with Faction reference, THE Serialization_System SHALL store FactionID instead of the Faction object
6. THE Serialization_System SHALL define a JsonSerializerContext with all serializable types explicitly declared
7. WHEN a type is missing from JsonSerializerContext, THE Serialization_System SHALL fail at compile time in AOT mode
8. THE Serialization_System SHALL declare collection types (such as List<Person>, List<int>) explicitly in JsonSerializerContext

### Requirement 3: 集合序列化性能优化

**User Story:** 作为开发者，我希望将集合序列化从 String 格式迁移到 List<int> 格式，以便减少 GC 压力和内存碎片，提高序列化性能。

#### Acceptance Criteria

1. THE Collection_Serializer SHALL serialize ID collections as List<int> instead of comma-separated strings
2. WHEN serializing Person.Treasures, THE Collection_Serializer SHALL produce TreasureIDs as [1,2,3] instead of "1,2,3"
3. WHEN deserializing collections, THE Collection_Serializer SHALL avoid string.Split() operations
4. THE Collection_Serializer SHALL reduce memory allocations compared to string-based serialization
5. THE Collection_Serializer SHALL be compatible with AOT compilation

### Requirement 4: 向后兼容性

**User Story:** 作为玩家，我希望能够加载旧的二进制存档文件，以便继续我之前的游戏进度。

#### Acceptance Criteria

1. THE Serialization_System SHALL detect and load Legacy_Format (.bin) files
2. WHEN loading a .bin file, THE Serialization_System SHALL convert it to the internal data structure
3. THE Serialization_System SHALL support reading both TreasuresString and TreasureIDs fields during migration
4. WHEN saving after loading a .bin file, THE Serialization_System SHALL use New_Format (.sav.gz)
5. IF a field exists in both String and List<int> formats, THEN THE Serialization_System SHALL prioritize the List<int> format

### Requirement 5: 引用重建机制

**User Story:** 作为开发者，我希望有一个统一的引用重建机制，以便在 Link_References_Phase 中可靠地恢复对象引用关系。

#### Acceptance Criteria

1. THE Reference_Resolver SHALL provide a unified interface for resolving ID references to objects
2. WHEN resolving a FactionID, THE Reference_Resolver SHALL lookup the Faction in Scenario.Factions collection
3. THE Reference_Resolver SHALL handle missing references gracefully without crashing
4. WHEN an ID reference is invalid, THE Reference_Resolver SHALL log a warning and set the reference to null
5. THE Reference_Resolver SHALL complete all reference linking before game logic execution begins
6. WHEN an invalid ID is encountered, THE Reference_Resolver SHALL reset the ID field to a safe default value
7. IF a Person references a non-existent FactionID, THEN THE Reference_Resolver SHALL set BelongedFactionID to -1 and BelongedFaction to null

### Requirement 6: 关键类集合迁移

**User Story:** 作为开发者，我希望将关键游戏类的集合字段从 String 迁移到 List<int>，以便获得性能提升。

#### Acceptance Criteria

1. THE Serialization_System SHALL support migrating Person.Treasures from TreasuresString to TreasureIDs
2. THE Serialization_System SHALL support migrating Faction.Architectures from ArchitecturesString to ArchitectureIDs
3. THE Serialization_System SHALL support migrating Legion.Troops from TroopsString to TroopIDs
4. THE Serialization_System SHALL support migrating Section.Architectures from ArchitecturesString to ArchitectureIDs
5. WHEN both String and List<int> fields exist, THE Serialization_System SHALL only serialize the List<int> field

### Requirement 7: 性能基准测试

**User Story:** 作为开发者，我希望能够测量序列化性能改进，以便验证优化效果。

#### Acceptance Criteria

1. THE Serialization_System SHALL provide benchmark tests for serialization performance
2. THE Serialization_System SHALL measure GC allocations before and after migration
3. THE Serialization_System SHALL measure serialization time for large game scenarios
4. THE Serialization_System SHALL compare String-based vs List<int>-based collection serialization performance
5. THE Serialization_System SHALL report memory usage during serialization operations

### Requirement 8: 错误处理和日志

**User Story:** 作为开发者，我希望序列化系统能够提供清晰的错误信息和日志，以便快速诊断问题。

#### Acceptance Criteria

1. WHEN serialization fails, THE Serialization_System SHALL log the exception with context information
2. WHEN deserialization encounters invalid JSON, THE Serialization_System SHALL provide a descriptive error message
3. WHEN reference linking fails, THE Reference_Resolver SHALL log which ID could not be resolved
4. THE Serialization_System SHALL log the file format version being loaded
5. WHEN loading a corrupted file, THE Serialization_System SHALL attempt recovery and log the recovery process


### Requirement 9: 数据验证和清洗

**User Story:** 作为开发者，我希望在加载存档后能够验证和清洗脏数据，以便防止 MOD 修改或数据损坏导致的游戏崩溃。

#### Acceptance Criteria

1. THE Serialization_System SHALL implement a validation phase after Link_References_Phase
2. WHEN invalid references are detected, THE Serialization_System SHALL apply data sanitization rules
3. THE Serialization_System SHALL log all data sanitization operations for debugging
4. WHEN a Person references a non-existent Faction, THE Serialization_System SHALL reset the Person to unaffiliated state
5. THE Serialization_System SHALL validate all critical references before allowing game logic to execute
6. WHEN data sanitization occurs, THE Serialization_System SHALL provide a summary report to the user
7. THE Serialization_System SHALL allow game to continue after sanitization without crashing


### Requirement 10: 多态序列化支持

**User Story:** 作为开发者，我希望正确序列化继承层次结构，以便子类的特有属性不会在序列化过程中丢失。

#### Acceptance Criteria

1. THE Serialization_System SHALL support polymorphic serialization for inheritance hierarchies
2. WHEN serializing a derived type (such as City : Architecture), THE Serialization_System SHALL preserve all derived type properties
3. THE Serialization_System SHALL use JsonDerivedType attributes to declare all derived types on base classes
4. WHEN deserializing polymorphic types, THE Serialization_System SHALL instantiate the correct derived type
5. THE Serialization_System SHALL include type discriminators in JSON for polymorphic types
6. WHEN a base class has derived types, THE Serialization_System SHALL fail at compile time if JsonDerivedType attributes are missing in AOT mode

### Requirement 11: 私有成员序列化

**User Story:** 作为开发者，我希望能够序列化具有私有 setter 的属性，以便保持封装性的同时支持序列化。

#### Acceptance Criteria

1. THE Serialization_System SHALL serialize properties with private setters when marked with JsonInclude
2. WHEN a property has a private setter, THE Serialization_System SHALL deserialize it if JsonInclude is present
3. THE Serialization_System SHALL support JsonConstructor for types that require constructor-based initialization
4. WHEN deserializing, THE Serialization_System SHALL use the JsonConstructor if available
5. THE Serialization_System SHALL log a warning if a property with private setter is not marked with JsonInclude

### Requirement 12: MOD 扩展数据保留

**User Story:** 作为玩家，我希望在卸载 MOD 后重新安装时，MOD 添加的数据不会丢失，以便保持游戏进度的完整性。

#### Acceptance Criteria

1. THE Serialization_System SHALL preserve unknown JSON fields using JsonExtensionData
2. WHEN deserializing encounters unknown fields, THE Serialization_System SHALL store them in ExtensionData dictionary
3. WHEN serializing, THE Serialization_System SHALL write back all ExtensionData fields to JSON
4. THE Serialization_System SHALL implement JsonExtensionData on core game classes (Person, Faction, Architecture, etc.)
5. WHEN a MOD is uninstalled and reinstalled, THE Serialization_System SHALL restore MOD-specific fields from ExtensionData
6. THE Serialization_System SHALL handle version differences gracefully by preserving deprecated fields in ExtensionData

### Requirement 13: 存档元数据文件

**User Story:** 作为玩家，我希望在加载界面能够快速看到存档信息（如保存时间、游戏进度），而不需要解压和解析整个存档文件。

#### Acceptance Criteria

1. THE Serialization_System SHALL create a separate .meta file alongside each .sav.gz file
2. THE .meta file SHALL contain save metadata (save time, game version, scenario name, turn number)
3. WHEN saving, THE Serialization_System SHALL write the .meta file as uncompressed JSON
4. WHEN loading the save list, THE Serialization_System SHALL read only .meta files without decompressing .sav.gz files
5. THE .meta file SHALL be small enough to load instantly (< 1KB)
6. IF a .meta file is missing, THEN THE Serialization_System SHALL fall back to reading the .sav.gz file header
