# Design Document: JSON Serialization Unified Architecture

## Overview

本设计文档描述了将游戏序列化系统从二进制格式迁移到 JSON 格式的技术方案。核心架构采用三阶段设计：Save Data（保存数据）→ Load Data（加载数据）→ Link References（链接引用），实现对象引用与数据存储的分离。

### 核心设计理念

1. **ID-Based References**: 序列化时只存储对象的 ID，不存储对象本身
2. **Deferred Linking**: 反序列化后延迟重建对象引用关系
3. **Performance First**: 使用 List<int> 替代 String 存储集合，减少 GC 压力
4. **AOT Compatible**: 使用 JsonSerializerContext 确保 AOT 编译兼容性
5. **Backward Compatible**: 支持读取旧的二进制存档格式

### 技术栈

- **序列化引擎**: System.Text.Json (源生成器模式)
- **压缩**: GZip (System.IO.Compression)
- **文件格式**: .sav.gz (压缩的 JSON) + .meta (元数据)
- **目标平台**: .NET 6+ with AOT support

## Architecture

### 三阶段序列化流程

```
保存流程:
Game Objects → [Save Data Phase] → Data Transfer Objects (DTOs) → [JSON Serialization] → [GZip Compression] → .sav.gz file

加载流程:
.sav.gz file → [GZip Decompression] → [JSON Deserialization] → DTOs → [Load Data Phase] → Game Objects (with ID refs) → [Link References Phase] → Game Objects (with object refs) → [Validation Phase] → Ready for gameplay
```

### Phase 1: Save Data Phase

**职责**: 将游戏对象转换为可序列化的数据传输对象（DTOs）

**核心逻辑**:
```csharp
// 伪代码示例
class SaveDataPhase
{
    GameScenarioDTO ConvertToDTO(GameScenario scenario)
    {
        var dto = new GameScenarioDTO();
        
        // 转换 Person 对象，只保存 ID 引用
        foreach (var person in scenario.Persons)
        {
            var personDTO = new PersonDTO
            {
                ID = person.ID,
                Name = person.Name,
                // 只保存 Faction 的 ID，不保存整个对象
                BelongedFactionID = person.BelongedFaction?.ID ?? -1,
                // 集合使用 List<int> 而非 String
                TreasureIDs = person.Treasures.Select(t => t.ID).ToList()
            };
            dto.Persons.Add(personDTO);
        }
        
        return dto;
    }
}
```

**关键设计决策**:
- 使用独立的 DTO 类，避免污染游戏逻辑类
- 所有对象引用转换为 ID（int 类型）
- 集合引用转换为 List<int>，避免字符串拼接

### Phase 2: Load Data Phase

**职责**: 从 JSON 反序列化 DTOs，创建游戏对象（但不重建引用）

**核心逻辑**:
```csharp
class LoadDataPhase
{
    GameScenario LoadFromDTO(GameScenarioDTO dto)
    {
        var scenario = new GameScenario();
        
        // 第一遍：创建所有对象，但不链接引用
        foreach (var personDTO in dto.Persons)
        {
            var person = new Person
            {
                ID = personDTO.ID,
                Name = personDTO.Name,
                // 暂时只保存 ID，不查找对象
                BelongedFactionID = personDTO.BelongedFactionID,
                BelongedFaction = null  // 稍后在 Link Phase 中设置
            };
            scenario.Persons.Add(person.ID, person);
        }
        
        return scenario;
    }
}
```

**关键设计决策**:
- 先创建所有对象，建立 ID → Object 的映射
- 不在此阶段解析引用，避免循环依赖问题
- 保留 ID 字段用于后续链接

### Phase 3: Link References Phase

**职责**: 根据 ID 重建对象引用关系

**核心逻辑**:
```csharp
class LinkReferencesPhase
{
    void LinkReferences(GameScenario scenario)
    {
        // 建立查找表
        var factions = scenario.Factions;  // Dictionary<int, Faction>
        var treasures = scenario.Treasures;  // Dictionary<int, Treasure>
        
        // 链接 Person 的引用
        foreach (var person in scenario.Persons.Values)
        {
            // 链接 Faction 引用
            if (person.BelongedFactionID > 0)
            {
                if (factions.TryGetValue(person.BelongedFactionID, out var faction))
                {
                    person.BelongedFaction = faction;
                }
                else
                {
                    // 数据清洗：无效引用重置为安全状态
                    LogWarning($"Person {person.ID} references non-existent Faction {person.BelongedFactionID}");
                    person.BelongedFactionID = -1;
                    person.BelongedFaction = null;
                }
            }
            
            // 链接 Treasure 集合引用
            person.Treasures.Clear();
            foreach (var treasureID in person.TreasureIDs)
            {
                if (treasures.TryGetValue(treasureID, out var treasure))
                {
                    person.Treasures.Add(treasure);
                }
                else
                {
                    LogWarning($"Person {person.ID} references non-existent Treasure {treasureID}");
                }
            }
        }
    }
}
```

**关键设计决策**:
- 使用 Dictionary<int, T> 实现 O(1) 查找
- 优雅处理无效引用，不崩溃
- 记录所有数据清洗操作用于调试

### Phase 4: Validation Phase

**职责**: 验证数据完整性，清洗脏数据

**核心逻辑**:
```csharp
class ValidationPhase
{
    ValidationReport Validate(GameScenario scenario)
    {
        var report = new ValidationReport();
        
        // 验证关键引用
        foreach (var person in scenario.Persons.Values)
        {
            // 检查必需的引用
            if (person.BelongedFactionID > 0 && person.BelongedFaction == null)
            {
                report.AddError($"Person {person.ID} has invalid faction reference");
                // 自动修复
                person.BelongedFactionID = -1;
                report.AddFix($"Reset Person {person.ID} to unaffiliated");
            }
        }
        
        return report;
    }
}
```

## Components and Interfaces

### 核心组件

#### 1. SerializationManager

**职责**: 协调整个序列化/反序列化流程

```csharp
public class SerializationManager
{
    private readonly SaveDataPhase _savePhase;
    private readonly LoadDataPhase _loadPhase;
    private readonly LinkReferencesPhase _linkPhase;
    private readonly ValidationPhase _validationPhase;
    private readonly LegacyFormatReader _legacyReader;
    
    public void SaveGame(GameScenario scenario, string filePath)
    {
        // 1. Convert to DTO
        var dto = _savePhase.ConvertToDTO(scenario);
        
        // 2. Serialize to JSON
        var json = JsonSerializer.Serialize(dto, GameJsonContext.Default.GameScenarioDTO);
        
        // 3. Compress and write
        using var fileStream = File.Create(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using var writer = new StreamWriter(gzipStream);
        writer.Write(json);
        
        // 4. Write metadata file
        WriteMetadata(scenario, filePath + ".meta");
    }
    
    public GameScenario LoadGame(string filePath)
    {
        GameScenarioDTO dto;
        
        // 1. Detect format
        if (filePath.EndsWith(".bin"))
        {
            dto = _legacyReader.ReadBinaryFormat(filePath);
        }
        else
        {
            // 2. Decompress and deserialize
            using var fileStream = File.OpenRead(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            dto = JsonSerializer.Deserialize<GameScenarioDTO>(gzipStream, GameJsonContext.Default.GameScenarioDTO);
        }
        
        // 3. Load data phase
        var scenario = _loadPhase.LoadFromDTO(dto);
        
        // 4. Link references phase
        _linkPhase.LinkReferences(scenario);
        
        // 5. Validation phase
        var report = _validationPhase.Validate(scenario);
        if (report.HasErrors)
        {
            LogValidationReport(report);
        }
        
        return scenario;
    }
}
```

#### 2. GameJsonContext (AOT Source Generator)

**职责**: 为 System.Text.Json 提供 AOT 编译支持

```csharp
[JsonSerializable(typeof(GameScenarioDTO))]
[JsonSerializable(typeof(PersonDTO))]
[JsonSerializable(typeof(FactionDTO))]
[JsonSerializable(typeof(ArchitectureDTO))]
[JsonSerializable(typeof(TreasureDTO))]
[JsonSerializable(typeof(LegionDTO))]
[JsonSerializable(typeof(TroopDTO))]
[JsonSerializable(typeof(SectionDTO))]
// 显式声明集合类型
[JsonSerializable(typeof(List<PersonDTO>))]
[JsonSerializable(typeof(List<FactionDTO>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(Dictionary<int, PersonDTO>))]
// 元数据类型
[JsonSerializable(typeof(SaveMetadata))]
[JsonSourceGenerationOptions(
    WriteIndented = false,  // 减小文件大小
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public partial class GameJsonContext : JsonSerializerContext
{
}
```

**关键设计决策**:
- 必须显式声明所有序列化类型，否则 AOT 编译失败
- 包含泛型集合类型（List<T>, Dictionary<K,V>）
- 使用 camelCase 命名约定，与 JSON 标准一致
- 忽略 null 值以减小文件大小

#### 3. ReferenceLinker

**职责**: 统一的引用链接接口

```csharp
public interface IReferenceLinker<T>
{
    void LinkReferences(T obj, GameScenario scenario);
}

public class PersonReferenceLinker : IReferenceLinker<Person>
{
    public void LinkReferences(Person person, GameScenario scenario)
    {
        // Link Faction
        if (person.BelongedFactionID > 0)
        {
            if (scenario.Factions.TryGetValue(person.BelongedFactionID, out var faction))
            {
                person.BelongedFaction = faction;
            }
            else
            {
                person.BelongedFactionID = -1;
                person.BelongedFaction = null;
            }
        }
        
        // Link Treasures
        person.Treasures.Clear();
        foreach (var id in person.TreasureIDs)
        {
            if (scenario.Treasures.TryGetValue(id, out var treasure))
            {
                person.Treasures.Add(treasure);
            }
        }
    }
}
```

#### 4. LegacyFormatReader

**职责**: 读取旧的二进制存档格式

```csharp
public class LegacyFormatReader
{
    public GameScenarioDTO ReadBinaryFormat(string filePath)
    {
        // 读取二进制格式
        var binaryData = ReadBinaryFile(filePath);
        
        // 转换为 DTO
        var dto = ConvertBinaryToDTO(binaryData);
        
        // 迁移 String 集合到 List<int>
        MigrateStringCollections(dto);
        
        return dto;
    }
    
    private void MigrateStringCollections(GameScenarioDTO dto)
    {
        foreach (var person in dto.Persons)
        {
            // 如果有旧的 TreasuresString，转换为 TreasureIDs
            if (!string.IsNullOrEmpty(person.TreasuresString))
            {
                person.TreasureIDs = person.TreasuresString
                    .Split(',')
                    .Select(int.Parse)
                    .ToList();
                person.TreasuresString = null;  // 清除旧字段
            }
        }
    }
}
```

## Data Models

### DTO 类设计

#### GameScenarioDTO

```csharp
public class GameScenarioDTO
{
    public int Version { get; set; } = 1;
    public string ScenarioName { get; set; }
    public int CurrentTurn { get; set; }
    
    public List<PersonDTO> Persons { get; set; } = new();
    public List<FactionDTO> Factions { get; set; } = new();
    public List<ArchitectureDTO> Architectures { get; set; } = new();
    public List<TreasureDTO> Treasures { get; set; } = new();
    public List<LegionDTO> Legions { get; set; } = new();
    public List<TroopDTO> Troops { get; set; } = new();
    public List<SectionDTO> Sections { get; set; } = new();
    
    // MOD 扩展数据
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
```

#### PersonDTO

```csharp
public class PersonDTO
{
    public int ID { get; set; }
    public string Name { get; set; }
    
    // 引用字段（只存 ID）
    public int BelongedFactionID { get; set; } = -1;
    
    // 集合引用（使用 List<int>）
    public List<int> TreasureIDs { get; set; } = new();
    
    // 向后兼容：保留旧的 String 字段（仅用于读取旧存档）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string TreasuresString { get; set; }
    
    // 私有成员序列化
    [JsonInclude]
    public int Loyalty { get; private set; }
    
    // MOD 扩展数据
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
```

#### ArchitectureDTO (多态基类)

```csharp
// 多态序列化：声明所有派生类
[JsonDerivedType(typeof(CityDTO), typeDiscriminator: "city")]
[JsonDerivedType(typeof(PortDTO), typeDiscriminator: "port")]
[JsonDerivedType(typeof(GateDTO), typeDiscriminator: "gate")]
public class ArchitectureDTO
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int BelongedFactionID { get; set; } = -1;
    public int BelongedSectionID { get; set; } = -1;
    
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}

public class CityDTO : ArchitectureDTO
{
    public int Population { get; set; }
    public int Agriculture { get; set; }
    public int Commerce { get; set; }
}

public class PortDTO : ArchitectureDTO
{
    public int ShipCapacity { get; set; }
}

public class GateDTO : ArchitectureDTO
{
    public int Endurance { get; set; }
}
```

#### FactionDTO

```csharp
public class FactionDTO
{
    public int ID { get; set; }
    public string Name { get; set; }
    
    // 集合引用
    public List<int> ArchitectureIDs { get; set; } = new();
    
    // 向后兼容
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ArchitecturesString { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
```

#### SaveMetadata

```csharp
public class SaveMetadata
{
    public string ScenarioName { get; set; }
    public DateTime SaveTime { get; set; }
    public int CurrentTurn { get; set; }
    public string GameVersion { get; set; }
    public int PlayerFactionID { get; set; }
}
```

### 游戏对象类修改

游戏对象类需要添加 ID 字段用于序列化：

```csharp
public class Person
{
    // 原有字段
    public int ID { get; set; }
    public string Name { get; set; }
    
    // 对象引用（运行时使用）
    public Faction BelongedFaction { get; set; }
    
    // ID 引用（序列化使用）
    [JsonIgnore]  // 游戏对象不直接序列化，通过 DTO 转换
    public int BelongedFactionID { get; set; }
    
    // 集合引用（运行时使用）
    public List<Treasure> Treasures { get; set; } = new();
    
    // ID 集合（序列化使用）
    [JsonIgnore]
    public List<int> TreasureIDs { get; set; } = new();
    
    // 废弃字段（仅用于向后兼容）
    [Obsolete("Use TreasureIDs instead")]
    public string TreasuresString { get; set; }
}
```

### 集合性能优化

#### String vs List<int> 对比

**旧方案（String）**:
```csharp
// 保存
person.TreasuresString = string.Join(",", person.Treasures.Select(t => t.ID));
// 问题：string.Join 创建临时字符串，GC 压力大

// 加载
var ids = person.TreasuresString.Split(',');
foreach (var id in ids)
{
    var treasure = scenario.Treasures[int.Parse(id)];
    person.Treasures.Add(treasure);
}
// 问题：Split 创建字符串数组，Parse 有开销
```

**新方案（List<int>）**:
```csharp
// 保存
person.TreasureIDs = person.Treasures.Select(t => t.ID).ToList();
// 优势：直接创建 List<int>，无字符串分配

// 加载
foreach (var id in person.TreasureIDs)
{
    var treasure = scenario.Treasures[id];
    person.Treasures.Add(treasure);
}
// 优势：无 Split，无 Parse，直接整数查找
```

**性能收益**:
- 减少 50%+ 的内存分配
- 减少 30%+ 的序列化时间
- 减少 GC 压力，避免内存碎片
- AOT 编译器原生优化 List<int>

## Correctness Properties


A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.

### Property 1: Serialization Round-Trip Preserves Game State

*For any* valid GameScenario, serializing it to JSON and then deserializing it back should produce a GameScenario with equivalent game state (all objects, references, and collections intact).

**Validates: Requirements 1.4, 2.1, 2.2**

**Rationale**: This is the fundamental correctness property for any serialization system. It ensures that save/load cycles don't lose or corrupt data.

### Property 2: Object References Serialize as IDs Only

*For any* game object with references (Person, Faction, Architecture, etc.), the serialized JSON should contain only integer ID fields, not nested object structures.

**Validates: Requirements 1.4, 2.4**

**Rationale**: This ensures the three-phase architecture is correctly implemented and prevents circular reference issues in JSON.

### Property 3: GZip Compression is Applied

*For any* saved game file with .sav.gz extension, decompressing it with GZip should yield valid JSON that can be deserialized.

**Validates: Requirements 2.2**

**Rationale**: Verifies that compression is actually applied and files are not just renamed.

### Property 4: Collections Serialize as JSON Arrays

*For any* collection of IDs (TreasureIDs, ArchitectureIDs, etc.), the serialized JSON should contain a JSON array [1,2,3] not a string "1,2,3".

**Validates: Requirements 3.1**

**Rationale**: Ensures the performance optimization is correctly implemented.

### Property 5: List<int> Serialization Reduces Memory Allocations

*For any* large collection of IDs, serializing as List<int> should produce fewer GC allocations than serializing as comma-separated string.

**Validates: Requirements 3.4**

**Rationale**: Validates the performance benefit of the migration from String to List<int>.

### Property 6: Invalid References Don't Crash

*For any* GameScenario with invalid ID references (referencing non-existent objects), the Link References Phase should complete without throwing exceptions.

**Validates: Requirements 5.3, 9.7**

**Rationale**: Ensures robustness against corrupted saves or MOD-modified data.

### Property 7: List<int> Takes Priority Over String

*For any* DTO with both a String field (e.g., TreasuresString) and a List<int> field (e.g., TreasureIDs) populated, only the List<int> field should appear in the serialized JSON.

**Validates: Requirements 6.5**

**Rationale**: Ensures clean migration path and prevents redundant data in save files.

### Property 8: Polymorphic Types Preserve All Properties

*For any* derived type instance (City, Port, Gate), serializing and deserializing it should preserve all properties including those specific to the derived type.

**Validates: Requirements 10.2**

**Rationale**: Critical for inheritance hierarchies. Without this, derived type data would be lost.

### Property 9: Correct Derived Type is Instantiated

*For any* polymorphic type in JSON with a type discriminator, deserializing should instantiate the correct derived type, not the base type.

**Validates: Requirements 10.4**

**Rationale**: Ensures polymorphic deserialization works correctly and doesn't downcast to base types.

### Property 10: Type Discriminators Present in JSON

*For any* polymorphic type instance, the serialized JSON should include a type discriminator field (e.g., "$type": "city").

**Validates: Requirements 10.5**

**Rationale**: Type discriminators are essential for polymorphic deserialization to work.

### Property 11: Private Setters with JsonInclude Serialize

*For any* property with a private setter marked with [JsonInclude], the property value should be present in serialized JSON and correctly restored on deserialization.

**Validates: Requirements 11.1**

**Rationale**: Ensures encapsulation doesn't break serialization.

### Property 12: Unknown Fields Are Preserved

*For any* JSON with unknown fields (not matching current class definition), deserializing should store those fields in ExtensionData dictionary.

**Validates: Requirements 12.1**

**Rationale**: Critical for MOD support and forward/backward compatibility.

### Property 13: ExtensionData Round-Trips Correctly

*For any* DTO with ExtensionData populated, serializing and deserializing should preserve all ExtensionData fields with their original values.

**Validates: Requirements 12.3**

**Rationale**: This is a round-trip property specifically for MOD data preservation.

## Error Handling

### Error Categories

#### 1. Serialization Errors

**Causes**:
- Null reference in required field
- Circular references (should not occur with ID-based design)
- Type not registered in JsonSerializerContext (AOT mode)

**Handling**:
```csharp
try
{
    var json = JsonSerializer.Serialize(dto, GameJsonContext.Default.GameScenarioDTO);
}
catch (JsonException ex)
{
    Logger.Error($"Serialization failed: {ex.Message}");
    Logger.Error($"Object type: {dto.GetType().Name}");
    throw new SerializationException("Failed to serialize game data", ex);
}
```

#### 2. Deserialization Errors

**Causes**:
- Invalid JSON syntax
- Missing required fields
- Type mismatch
- Corrupted file

**Handling**:
```csharp
try
{
    var dto = JsonSerializer.Deserialize<GameScenarioDTO>(json, GameJsonContext.Default.GameScenarioDTO);
}
catch (JsonException ex)
{
    Logger.Error($"Deserialization failed: {ex.Message}");
    Logger.Error($"File: {filePath}");
    
    // Attempt recovery
    if (TryRecoverFromBackup(filePath, out var recoveredDto))
    {
        Logger.Info("Recovered from backup");
        return recoveredDto;
    }
    
    throw new DeserializationException("Failed to load game data", ex);
}
```

#### 3. Reference Linking Errors

**Causes**:
- Invalid ID reference (object doesn't exist)
- MOD removed objects
- Save file edited manually

**Handling**:
```csharp
if (!scenario.Factions.TryGetValue(person.BelongedFactionID, out var faction))
{
    Logger.Warning($"Person {person.ID} ({person.Name}) references non-existent Faction {person.BelongedFactionID}");
    
    // Sanitize data
    person.BelongedFactionID = -1;
    person.BelongedFaction = null;
    
    validationReport.AddWarning($"Reset Person {person.ID} to unaffiliated");
}
```

#### 4. Compression Errors

**Causes**:
- Disk full during write
- Corrupted compressed data
- Incomplete write (crash during save)

**Handling**:
```csharp
try
{
    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
    // ... read data
}
catch (InvalidDataException ex)
{
    Logger.Error($"File appears to be corrupted: {filePath}");
    
    // Try reading as uncompressed JSON (fallback)
    if (TryReadUncompressed(filePath, out var data))
    {
        Logger.Info("Successfully read as uncompressed JSON");
        return data;
    }
    
    throw new CorruptedFileException("Cannot read save file", ex);
}
```

### Validation Report

```csharp
public class ValidationReport
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Fixes { get; } = new();
    
    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
    
    public void AddError(string message)
    {
        Errors.Add(message);
        Logger.Error($"[Validation] {message}");
    }
    
    public void AddWarning(string message)
    {
        Warnings.Add(message);
        Logger.Warning($"[Validation] {message}");
    }
    
    public void AddFix(string message)
    {
        Fixes.Add(message);
        Logger.Info($"[Validation] {message}");
    }
    
    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Validation Report ===");
        
        if (Errors.Count > 0)
        {
            sb.AppendLine($"Errors: {Errors.Count}");
            foreach (var error in Errors)
                sb.AppendLine($"  - {error}");
        }
        
        if (Warnings.Count > 0)
        {
            sb.AppendLine($"Warnings: {Warnings.Count}");
            foreach (var warning in Warnings)
                sb.AppendLine($"  - {warning}");
        }
        
        if (Fixes.Count > 0)
        {
            sb.AppendLine($"Fixes Applied: {Fixes.Count}");
            foreach (var fix in Fixes)
                sb.AppendLine($"  - {fix}");
        }
        
        return sb.ToString();
    }
}
```

## Testing Strategy

### Dual Testing Approach

本项目采用单元测试和基于属性的测试（Property-Based Testing, PBT）相结合的策略：

- **单元测试**: 验证具体示例、边界情况和错误条件
- **属性测试**: 验证跨所有输入的通用属性

两者互补，共同确保全面覆盖：
- 单元测试捕获具体的 bug
- 属性测试验证通用正确性

### Property-Based Testing Configuration

**测试库选择**: 使用 FsCheck（.NET 平台的成熟 PBT 库）

**配置要求**:
- 每个属性测试最少运行 100 次迭代（由于随机化）
- 每个测试必须引用其设计文档中的属性
- 标签格式: `// Feature: json-serialization-unified-architecture, Property {number}: {property_text}`

**示例属性测试**:

```csharp
using FsCheck;
using FsCheck.Xunit;

public class SerializationPropertyTests
{
    // Feature: json-serialization-unified-architecture, Property 1: Serialization Round-Trip Preserves Game State
    [Property(MaxTest = 100)]
    public Property RoundTripPreservesGameState()
    {
        return Prop.ForAll(
            GameScenarioGenerator.Arbitrary(),
            scenario =>
            {
                // Serialize
                var manager = new SerializationManager();
                var tempFile = Path.GetTempFileName();
                manager.SaveGame(scenario, tempFile);
                
                // Deserialize
                var loaded = manager.LoadGame(tempFile);
                
                // Verify equivalence
                return AreEquivalent(scenario, loaded);
            });
    }
    
    // Feature: json-serialization-unified-architecture, Property 2: Object References Serialize as IDs Only
    [Property(MaxTest = 100)]
    public Property ObjectReferencesSerializeAsIDsOnly()
    {
        return Prop.ForAll(
            PersonGenerator.Arbitrary(),
            person =>
            {
                var dto = SaveDataPhase.ConvertPersonToDTO(person);
                var json = JsonSerializer.Serialize(dto, GameJsonContext.Default.PersonDTO);
                
                // Verify JSON doesn't contain nested "belongedFaction" object
                // Only "belongedFactionID" should be present
                return !json.Contains("\"belongedFaction\":{") &&
                       json.Contains("\"belongedFactionID\":");
            });
    }
    
    // Feature: json-serialization-unified-architecture, Property 4: Collections Serialize as JSON Arrays
    [Property(MaxTest = 100)]
    public Property CollectionsSerializeAsArrays()
    {
        return Prop.ForAll(
            Arb.Default.NonEmptyArray<int>(),
            ids =>
            {
                var person = new PersonDTO { TreasureIDs = ids.Get.ToList() };
                var json = JsonSerializer.Serialize(person, GameJsonContext.Default.PersonDTO);
                
                // Verify it's an array [1,2,3] not a string "1,2,3"
                return json.Contains("\"treasureIDs\":[") &&
                       !json.Contains("\"treasureIDs\":\"");
            });
    }
    
    // Feature: json-serialization-unified-architecture, Property 6: Invalid References Don't Crash
    [Property(MaxTest = 100)]
    public Property InvalidReferencesDontCrash()
    {
        return Prop.ForAll(
            GameScenarioWithInvalidRefsGenerator.Arbitrary(),
            scenario =>
            {
                var linker = new LinkReferencesPhase();
                
                // Should not throw
                try
                {
                    linker.LinkReferences(scenario);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
    }
    
    // Feature: json-serialization-unified-architecture, Property 8: Polymorphic Types Preserve All Properties
    [Property(MaxTest = 100)]
    public Property PolymorphicTypesPreserveAllProperties()
    {
        return Prop.ForAll(
            ArchitectureGenerator.Arbitrary(),  // Generates City, Port, Gate randomly
            architecture =>
            {
                var json = JsonSerializer.Serialize(architecture, GameJsonContext.Default.ArchitectureDTO);
                var deserialized = JsonSerializer.Deserialize<ArchitectureDTO>(json, GameJsonContext.Default.ArchitectureDTO);
                
                // Verify all properties are preserved
                return AreEquivalent(architecture, deserialized);
            });
    }
    
    // Feature: json-serialization-unified-architecture, Property 13: ExtensionData Round-Trips Correctly
    [Property(MaxTest = 100)]
    public Property ExtensionDataRoundTrips()
    {
        return Prop.ForAll(
            PersonDTOWithExtensionDataGenerator.Arbitrary(),
            personDTO =>
            {
                var json = JsonSerializer.Serialize(personDTO, GameJsonContext.Default.PersonDTO);
                var deserialized = JsonSerializer.Deserialize<PersonDTO>(json, GameJsonContext.Default.PersonDTO);
                
                // Verify ExtensionData is preserved
                return DictionariesEqual(personDTO.ExtensionData, deserialized.ExtensionData);
            });
    }
}
```

### Unit Testing Strategy

**单元测试重点**:
- 具体示例（如特定的 Person 对象序列化）
- 边界情况（空集合、null 引用、最大 ID 值）
- 错误条件（无效 JSON、缺失文件、损坏数据）
- 集成点（组件之间的交互）

**示例单元测试**:

```csharp
public class SerializationUnitTests
{
    [Fact]
    public void SaveGame_CreatesMetadataFile()
    {
        // Arrange
        var scenario = CreateTestScenario();
        var manager = new SerializationManager();
        var savePath = Path.Combine(Path.GetTempPath(), "test.sav.gz");
        var metaPath = savePath + ".meta";
        
        // Act
        manager.SaveGame(scenario, savePath);
        
        // Assert
        Assert.True(File.Exists(savePath));
        Assert.True(File.Exists(metaPath));
        
        // Verify metadata content
        var metaJson = File.ReadAllText(metaPath);
        var metadata = JsonSerializer.Deserialize<SaveMetadata>(metaJson);
        Assert.Equal(scenario.ScenarioName, metadata.ScenarioName);
        Assert.Equal(scenario.CurrentTurn, metadata.CurrentTurn);
    }
    
    [Fact]
    public void LoadGame_HandlesLegacyBinaryFormat()
    {
        // Arrange
        var legacyFile = "test_data/legacy_save.bin";
        var manager = new SerializationManager();
        
        // Act
        var scenario = manager.LoadGame(legacyFile);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.NotEmpty(scenario.Persons);
    }
    
    [Fact]
    public void LoadGame_MigratesStringCollectionsToListInt()
    {
        // Arrange
        var dto = new PersonDTO
        {
            ID = 1,
            TreasuresString = "10,20,30"  // Old format
        };
        var manager = new SerializationManager();
        
        // Act
        var person = manager.LoadDataPhase.ConvertFromDTO(dto);
        
        // Assert
        Assert.Equal(3, person.TreasureIDs.Count);
        Assert.Contains(10, person.TreasureIDs);
        Assert.Contains(20, person.TreasureIDs);
        Assert.Contains(30, person.TreasureIDs);
    }
    
    [Fact]
    public void LinkReferences_ResetsInvalidFactionReference()
    {
        // Arrange
        var scenario = new GameScenario();
        var person = new Person { ID = 1, BelongedFactionID = 9999 };  // Invalid ID
        scenario.Persons.Add(person.ID, person);
        var linker = new LinkReferencesPhase();
        
        // Act
        linker.LinkReferences(scenario);
        
        // Assert
        Assert.Equal(-1, person.BelongedFactionID);
        Assert.Null(person.BelongedFaction);
    }
    
    [Fact]
    public void Serialize_PrioritizesListIntOverString()
    {
        // Arrange
        var person = new PersonDTO
        {
            ID = 1,
            TreasureIDs = new List<int> { 1, 2, 3 },
            TreasuresString = "4,5,6"  // Should be ignored
        };
        
        // Act
        var json = JsonSerializer.Serialize(person, GameJsonContext.Default.PersonDTO);
        
        // Assert
        Assert.Contains("\"treasureIDs\":[1,2,3]", json);
        Assert.DoesNotContain("\"treasuresString\"", json);
    }
    
    [Fact]
    public void Deserialize_PreservesUnknownFields()
    {
        // Arrange
        var json = @"{
            ""id"": 1,
            ""name"": ""Test"",
            ""modField"": ""modValue"",
            ""anotherModField"": 123
        }";
        
        // Act
        var person = JsonSerializer.Deserialize<PersonDTO>(json, GameJsonContext.Default.PersonDTO);
        
        // Assert
        Assert.NotNull(person.ExtensionData);
        Assert.Equal(2, person.ExtensionData.Count);
        Assert.True(person.ExtensionData.ContainsKey("modField"));
        Assert.True(person.ExtensionData.ContainsKey("anotherModField"));
    }
}
```

### Performance Benchmarking

使用 BenchmarkDotNet 进行性能测试：

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private GameScenario _scenario;
    private PersonDTO _personWithString;
    private PersonDTO _personWithList;
    
    [GlobalSetup]
    public void Setup()
    {
        _scenario = CreateLargeScenario(1000);  // 1000 persons
        
        _personWithString = new PersonDTO
        {
            TreasuresString = string.Join(",", Enumerable.Range(1, 100))
        };
        
        _personWithList = new PersonDTO
        {
            TreasureIDs = Enumerable.Range(1, 100).ToList()
        };
    }
    
    [Benchmark]
    public string SerializeWithStringCollections()
    {
        return JsonSerializer.Serialize(_personWithString, GameJsonContext.Default.PersonDTO);
    }
    
    [Benchmark]
    public string SerializeWithListIntCollections()
    {
        return JsonSerializer.Serialize(_personWithList, GameJsonContext.Default.PersonDTO);
    }
    
    [Benchmark]
    public void FullSaveLoadCycle()
    {
        var manager = new SerializationManager();
        var tempFile = Path.GetTempFileName();
        
        manager.SaveGame(_scenario, tempFile);
        var loaded = manager.LoadGame(tempFile);
        
        File.Delete(tempFile);
    }
}
```

### Test Data Generators

为属性测试创建数据生成器：

```csharp
public static class GameScenarioGenerator
{
    public static Arbitrary<GameScenario> Arbitrary()
    {
        return Arb.From(Gen.Sized(size =>
        {
            var scenario = new GameScenario
            {
                ScenarioName = Gen.Sample<string>(1, 1, Arb.Default.String()).First(),
                CurrentTurn = Gen.Choose(1, 1000).Sample(1, 1).First()
            };
            
            // Generate persons
            var personCount = Math.Min(size, 100);
            for (int i = 1; i <= personCount; i++)
            {
                var person = GeneratePerson(i, scenario);
                scenario.Persons.Add(person.ID, person);
            }
            
            // Generate factions
            var factionCount = Math.Min(size / 10, 10);
            for (int i = 1; i <= factionCount; i++)
            {
                var faction = GenerateFaction(i);
                scenario.Factions.Add(faction.ID, faction);
            }
            
            return Gen.Constant(scenario);
        }));
    }
    
    private static Person GeneratePerson(int id, GameScenario scenario)
    {
        var person = new Person
        {
            ID = id,
            Name = $"Person_{id}",
            BelongedFactionID = Gen.Choose(-1, scenario.Factions.Count).Sample(1, 1).First()
        };
        
        // Generate treasure IDs
        var treasureCount = Gen.Choose(0, 10).Sample(1, 1).First();
        person.TreasureIDs = Gen.ListOf(treasureCount, Gen.Choose(1, 100)).Sample(1, 1).First();
        
        return person;
    }
}
```

### Integration Testing

测试完整的工作流程：

```csharp
public class IntegrationTests
{
    [Fact]
    public void CompleteWorkflow_SaveLoadValidate()
    {
        // Create a complex scenario
        var scenario = CreateComplexScenario();
        
        // Save
        var manager = new SerializationManager();
        var savePath = Path.Combine(Path.GetTempPath(), "integration_test.sav.gz");
        manager.SaveGame(scenario, savePath);
        
        // Verify files created
        Assert.True(File.Exists(savePath));
        Assert.True(File.Exists(savePath + ".meta"));
        
        // Load
        var loaded = manager.LoadGame(savePath);
        
        // Validate
        Assert.Equal(scenario.Persons.Count, loaded.Persons.Count);
        Assert.Equal(scenario.Factions.Count, loaded.Factions.Count);
        
        // Verify references are linked
        var firstPerson = loaded.Persons.Values.First();
        if (firstPerson.BelongedFactionID > 0)
        {
            Assert.NotNull(firstPerson.BelongedFaction);
            Assert.Equal(firstPerson.BelongedFactionID, firstPerson.BelongedFaction.ID);
        }
        
        // Cleanup
        File.Delete(savePath);
        File.Delete(savePath + ".meta");
    }
    
    [Fact]
    public void LegacyMigration_BinaryToJson()
    {
        // Load legacy binary file
        var manager = new SerializationManager();
        var legacyPath = "test_data/legacy_save.bin";
        var scenario = manager.LoadGame(legacyPath);
        
        // Save in new format
        var newPath = Path.Combine(Path.GetTempPath(), "migrated.sav.gz");
        manager.SaveGame(scenario, newPath);
        
        // Verify new format
        Assert.True(File.Exists(newPath));
        
        // Load again and verify
        var reloaded = manager.LoadGame(newPath);
        Assert.Equal(scenario.Persons.Count, reloaded.Persons.Count);
        
        // Cleanup
        File.Delete(newPath);
        File.Delete(newPath + ".meta");
    }
}
```

## Implementation Notes

### Migration Strategy

**阶段 1: 基础设施**
- 创建 DTO 类
- 实现 GameJsonContext
- 实现 SerializationManager 骨架

**阶段 2: 核心功能**
- 实现 Save Data Phase
- 实现 Load Data Phase
- 实现 Link References Phase

**阶段 3: 兼容性**
- 实现 LegacyFormatReader
- 实现 String → List<int> 迁移
- 实现 Validation Phase

**阶段 4: 优化**
- 性能基准测试
- 优化集合序列化
- 优化压缩参数

**阶段 5: 测试**
- 单元测试
- 属性测试
- 集成测试
- 性能测试

### Key Design Decisions

1. **为什么使用独立的 DTO 类？**
   - 分离序列化逻辑和游戏逻辑
   - 避免污染游戏对象类
   - 更容易进行版本控制和迁移

2. **为什么使用三阶段架构？**
   - 避免循环引用问题
   - 清晰的职责分离
   - 更容易调试和测试

3. **为什么使用 List<int> 而非 String？**
   - 减少内存分配和 GC 压力
   - 更快的序列化/反序列化
   - AOT 编译器原生优化
   - 避免字符串拼接和 Split 开销

4. **为什么需要 JsonExtensionData？**
   - 支持 MOD 扩展
   - 向前/向后兼容性
   - 保留未知字段，避免数据丢失

5. **为什么需要独立的 .meta 文件？**
   - 快速加载存档列表
   - 避免解压整个存档文件
   - 提升用户体验

### Potential Risks

1. **AOT 编译失败**
   - 风险：忘记在 JsonSerializerContext 中声明类型
   - 缓解：编译时检查，完整的类型清单

2. **多态序列化失败**
   - 风险：忘记添加 JsonDerivedType 属性
   - 缓解：单元测试覆盖所有派生类

3. **性能回退**
   - 风险：JSON 序列化比二进制慢
   - 缓解：性能基准测试，GZip 压缩

4. **数据丢失**
   - 风险：迁移过程中丢失数据
   - 缓解：完整的测试覆盖，验证阶段

5. **MOD 兼容性**
   - 风险：MOD 数据丢失
   - 缓解：JsonExtensionData，完整的测试
