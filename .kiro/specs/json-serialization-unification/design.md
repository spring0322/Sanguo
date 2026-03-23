# 设计文档：JSON 序列化架构统一

## 概述

本设计将游戏的序列化架构从双轨制（JSON + 二进制）统一到单一的 JSON 格式（带 GZip 压缩）。核心设计采用三阶段流程：

1. **Save Data**：将对象序列化为 JSON，对象引用转换为 ID，使用 GZip 压缩
2. **Load Data**：从 GZip 文件解压并反序列化 JSON，此时引用为 null
3. **Link References**：根据 ID 重建对象引用关系

这种设计避免了 JSON 序列化中的循环引用问题，同时保持了代码的简洁性和可维护性。

## 架构

### 当前架构问题

```
当前双轨制：
┌─────────────────┐     ┌─────────────────┐
│  剧本/配置      │     │    存档         │
│  (JSON)         │     │  (Binary)       │
└─────────────────┘     └─────────────────┘
        │                       │
        ▼                       ▼
  System.Text.Json      BinaryWriter/Reader
        │                       │
        └───────────┬───────────┘
                    │
            重复实现序列化逻辑
            容易遗漏字段
            维护困难
```

### 新架构设计

```
统一 JSON 架构：
┌─────────────────────────────────────┐
│         所有数据（JSON）            │
│  - 剧本/配置                        │
│  - 存档（带 GZip 压缩）             │
└─────────────────────────────────────┘
                │
                ▼
        System.Text.Json
                │
                ▼
        CompressedJsonSerializer
                │
        ┌───────┴───────┐
        │               │
        ▼               ▼
    保存 .sav.gz    加载 .sav.gz
```


### 三阶段序列化流程

```
阶段 1: Save Data
┌──────────────┐
│ GameScenario │
│  - SaveVersion: 1 │  <- 版本号
│  - Persons   │──┐
│  - Factions  │  │ SaveToString()
│  - Troops    │  │ 集合 → "1,2,3"
└──────────────┘  │
        │         │
        ▼         ▼
   JSON 序列化
   (只保存 ID)
        │
        ▼
   GZip 压缩
        │
        ▼
   .sav.gz 文件

阶段 2: Load Data
   .sav.gz 文件
        │
        ▼
   GZip 解压
        │
        ▼
   JSON 反序列化
        │
        ▼
┌──────────────┐
│ GameScenario │
│  - SaveVersion: 1 │  <- 读取版本号
│  - Persons   │ (引用 = null)
│  - Factions  │ (只有 ID)
│  - Troops    │
└──────────────┘

阶段 2.5: Migration（新增）
┌──────────────────────┐
│ 检查 SaveVersion     │
│ 如果版本 < 当前版本  │
│ 执行迁移逻辑         │
└──────────────────────┘
        │
        ▼
┌──────────────────────┐
│ V1 → V2: 添加新字段  │
│ V2 → V3: 重命名字段  │
│ ...                  │
└──────────────────────┘

阶段 3: Link References
┌──────────────┐
│ 遍历所有对象 │
└──────────────┘
        │
        ▼
┌──────────────────────┐
│ 根据 ID 查找对象     │
│ person.Faction =     │
│   factions[factionID]│
└──────────────────────┘
        │
        ▼
┌──────────────┐
│ 引用已恢复   │
└──────────────┘
```


## 组件和接口

### 1. CompressedJsonSerializer

负责 JSON 序列化和 GZip 压缩的核心组件。

```csharp
public static class CompressedJsonSerializer
{
    /// <summary>
    /// 保存对象为压缩的 JSON 文件
    /// </summary>
    public static void SaveCompressed<T>(T obj, string path)
    {
        // 1. 序列化为 JSON
        var json = JsonSerializer.Serialize(obj, GameJsonContext.Default);
        
        // 2. 转换为字节
        var bytes = Encoding.UTF8.GetBytes(json);
        
        // 3. GZip 压缩并写入文件
        using var fs = File.Create(path);
        using var gz = new GZipStream(fs, CompressionMode.Compress);
        gz.Write(bytes, 0, bytes.Length);
    }
    
    /// <summary>
    /// 从压缩的 JSON 文件加载对象
    /// </summary>
    public static T LoadCompressed<T>(string path)
    {
        // 1. 读取并解压文件
        using var fs = File.OpenRead(path);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        gz.CopyTo(ms);
        
        // 2. 转换为字符串
        var json = Encoding.UTF8.GetString(ms.ToArray());
        
        // 3. 反序列化为对象
        return JsonSerializer.Deserialize<T>(json, GameJsonContext.Default);
    }
}
```

**设计决策**：
- 使用 `System.IO.Compression.GZipStream` 进行压缩
- 使用 `System.Text.Json` 进行序列化
- 支持泛型以便复用


### 2. GameLoader（统一加载器）

负责根据文件扩展名选择正确的加载方法，支持向后兼容和版本迁移。

```csharp
public static class GameLoader
{
    // 当前存档格式版本
    public const int CURRENT_SAVE_VERSION = 1;
    
    /// <summary>
    /// 加载游戏场景（自动检测格式）
    /// </summary>
    public static GameScenario LoadScenario(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine($"[GameLoader] 文件不存在: {filePath}");
            return null;
        }
        
        GameScenario scenario = null;
        
        // 根据扩展名选择加载方法
        if (filePath.EndsWith(".bin"))
        {
            // 旧格式：二进制（视为版本 0）
            Debug.WriteLine($"[GameLoader] 检测到二进制格式，使用 BinaryScenarioHandler");
            scenario = BinaryScenarioHandler.LoadScenario(filePath);
            if (scenario != null)
            {
                scenario.SaveVersion = 0; // 标记为旧版本
            }
        }
        else if (filePath.EndsWith(".sav.gz"))
        {
            // 新格式：压缩 JSON
            Debug.WriteLine($"[GameLoader] 检测到压缩 JSON 格式");
            scenario = CompressedJsonSerializer.LoadCompressed<GameScenario>(filePath);
        }
        else if (filePath.EndsWith(".json"))
        {
            // 未压缩的 JSON（用于调试）
            Debug.WriteLine($"[GameLoader] 检测到未压缩 JSON 格式");
            var json = File.ReadAllText(filePath);
            scenario = JsonSerializer.Deserialize<GameScenario>(json, GameJsonContext.Default);
        }
        else
        {
            throw new NotSupportedException($"不支持的文件格式: {filePath}");
        }
        
        if (scenario != null)
        {
            // 执行版本迁移
            MigrateScenario(scenario);
            
            // 恢复引用
            RestoreReferences(scenario);
        }
        
        return scenario;
    }
    
    /// <summary>
    /// 执行版本迁移
    /// </summary>
    private static void MigrateScenario(GameScenario scenario)
    {
        int fromVersion = scenario.SaveVersion;
        int toVersion = CURRENT_SAVE_VERSION;
        
        if (fromVersion == toVersion)
        {
            Debug.WriteLine($"[GameLoader] 存档版本匹配 (v{fromVersion})，无需迁移");
            return;
        }
        
        Debug.WriteLine($"[GameLoader] 开始迁移存档：v{fromVersion} → v{toVersion}");
        
        // 执行逐步迁移
        for (int v = fromVersion; v < toVersion; v++)
        {
            MigrateFromVersion(scenario, v);
        }
        
        // 更新版本号
        scenario.SaveVersion = toVersion;
        Debug.WriteLine($"[GameLoader] 迁移完成");
    }
    
    /// <summary>
    /// 从特定版本迁移到下一个版本
    /// </summary>
    private static void MigrateFromVersion(GameScenario scenario, int fromVersion)
    {
        Debug.WriteLine($"[GameLoader] 执行迁移：v{fromVersion} → v{fromVersion + 1}");
        
        switch (fromVersion)
        {
            case 0:
                // V0 → V1: 二进制格式迁移到 JSON 格式
                MigrateV0ToV1(scenario);
                break;
                
            // 未来的迁移可以在这里添加
            // case 1:
            //     MigrateV1ToV2(scenario);
            //     break;
            
            default:
                Debug.WriteLine($"[GameLoader] 警告：未知的迁移版本 {fromVersion}");
                break;
        }
    }
    
    /// <summary>
    /// V0 → V1 迁移：二进制格式到 JSON 格式
    /// </summary>
    private static void MigrateV0ToV1(GameScenario scenario)
    {
        Debug.WriteLine($"[GameLoader] V0→V1: 二进制格式迁移");
        
        // 二进制格式已经通过 BinaryScenarioHandler 加载
        // 这里只需要确保所有必要的字段都已初始化
        
        // 示例：如果 V1 添加了新字段，在这里设置默认值
        // foreach (Person person in scenario.Persons)
        // {
        //     if (person.NewFieldInV1 == null)
        //     {
        //         person.NewFieldInV1 = "default_value";
        //     }
        // }
    }
    
    /// <summary>
    /// 恢复对象引用
    /// </summary>
    private static void RestoreReferences(GameScenario scenario)
    {
        // 详见 ReferenceLinker 组件
        ReferenceLinker.LinkAllReferences(scenario);
    }
}
```

**设计决策**：
- 支持三种格式：`.bin`（旧）、`.sav.gz`（新）、`.json`（调试）
- 自动检测格式，无需用户指定
- 在引用链接前执行版本迁移
- 使用逐步迁移策略（V0→V1→V2→...）
- 每个迁移步骤独立，易于维护


### 3. ReferenceLinker（引用链接器）

负责在反序列化后重建对象引用关系。

```csharp
public static class ReferenceLinker
{
    /// <summary>
    /// 链接所有对象引用
    /// </summary>
    public static void LinkAllReferences(GameScenario scenario)
    {
        // 1. 建立快速查找字典
        var personDict = scenario.Persons.ToDictionary(p => p.ID);
        var factionDict = scenario.Factions.ToDictionary(f => f.ID);
        var archDict = scenario.Architectures.ToDictionary(a => a.ID);
        var troopDict = scenario.Troops.ToDictionary(t => t.ID);
        var treasureDict = scenario.Treasures.ToDictionary(t => t.ID);
        
        // 2. 链接 Person 引用
        foreach (Person person in scenario.Persons)
        {
            LinkPersonReferences(person, factionDict, archDict, troopDict, treasureDict);
        }
        
        // 3. 链接 Faction 引用
        foreach (Faction faction in scenario.Factions)
        {
            LinkFactionReferences(faction, personDict, archDict);
        }
        
        // 4. 链接 Architecture 引用
        foreach (Architecture arch in scenario.Architectures)
        {
            LinkArchitectureReferences(arch, factionDict, personDict);
        }
        
        // 5. 链接 Troop 引用
        foreach (Troop troop in scenario.Troops)
        {
            LinkTroopReferences(troop, factionDict, personDict, archDict);
        }
        
        // ... 其他对象类型
    }
    
    private static void LinkPersonReferences(
        Person person, 
        Dictionary<int, Faction> factions,
        Dictionary<int, Architecture> architectures,
        Dictionary<int, Troop> troops,
        Dictionary<int, Treasure> treasures)
    {
        // 链接 Faction
        if (person.BelongedFactionID > 0 && factions.TryGetValue(person.BelongedFactionID, out var faction))
        {
            person.BelongedFaction = faction;
        }
        
        // 链接 Architecture
        if (person.LocationArchitectureID > 0 && architectures.TryGetValue(person.LocationArchitectureID, out var arch))
        {
            person.LocationArchitecture = arch;
        }
        
        // 链接 Troop
        if (person.LocationTroopID > 0 && troops.TryGetValue(person.LocationTroopID, out var troop))
        {
            person.LocationTroop = troop;
        }
        
        // 链接 Treasures 集合
        if (!string.IsNullOrEmpty(person.TreasuresString))
        {
            person.Treasures.LoadFromString(treasures, person.TreasuresString);
        }
    }
    
    // ... 其他链接方法
}
```

**设计决策**：
- 使用字典进行 O(1) 查找，避免 O(n²) 复杂度
- 分阶段链接，先建立字典再链接引用
- 使用 `TryGetValue` 避免异常，处理缺失引用


### 4. GameSaver（统一保存器）

负责保存游戏状态，自动处理 ID 转换和压缩，同时生成元数据文件。

```csharp
public static class GameSaver
{
    /// <summary>
    /// 保存游戏场景
    /// </summary>
    public static void SaveScenario(GameScenario scenario, string filePath)
    {
        // 1. 准备序列化：将引用转换为 ID
        PrepareForSerialization(scenario);
        
        // 2. 保存为压缩 JSON
        CompressedJsonSerializer.SaveCompressed(scenario, filePath);
        
        // 3. 生成元数据文件
        SaveMetadata(scenario, filePath);
        
        Debug.WriteLine($"[GameSaver] 游戏已保存到: {filePath}");
    }
    
    /// <summary>
    /// 保存元数据文件（用于快速浏览存档列表）
    /// </summary>
    private static void SaveMetadata(GameScenario scenario, string savePath)
    {
        var metaPath = Path.ChangeExtension(savePath, ".meta");
        
        var metadata = new SaveMetadata
        {
            Title = scenario.ScenarioTitle,
            SaveTime = DateTime.Now,
            GameDate = new GameDateInfo
            {
                Year = scenario.Date.Year,
                Month = scenario.Date.Month,
                Day = scenario.Date.Day
            },
            PlayerFaction = scenario.CurrentPlayer?.Name ?? "未知",
            PlayerLeader = scenario.CurrentPlayer?.Leader?.Name ?? "未知",
            TotalPersons = scenario.Persons.Count,
            TotalArchitectures = scenario.Architectures.Count,
            TotalTroops = scenario.Troops.Count
        };
        
        // 保存为未压缩的 JSON（文件很小，无需压缩）
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(metaPath, json);
    }
    
    /// <summary>
    /// 准备序列化：将对象引用转换为 ID
    /// </summary>
    private static void PrepareForSerialization(GameScenario scenario)
    {
        // 1. 转换 Person 引用
        foreach (Person person in scenario.Persons)
        {
            // 保存 Faction ID
            person.BelongedFactionID = person.BelongedFaction?.ID ?? 0;
            
            // 保存 Architecture ID
            person.LocationArchitectureID = person.LocationArchitecture?.ID ?? 0;
            
            // 保存 Troop ID
            person.LocationTroopID = person.LocationTroop?.ID ?? 0;
            
            // 保存 Treasures 集合
            person.TreasuresString = person.Treasures.SaveToString();
        }
        
        // 2. 转换 Faction 引用
        foreach (Faction faction in scenario.Factions)
        {
            // 保存 Architectures 集合
            faction.ArchitecturesString = faction.Architectures.SaveToString();
            
            // 保存 Persons 集合
            faction.PersonsString = faction.Persons.SaveToString();
            
            // ... 其他集合
        }
        
        // 3. 转换 Architecture 引用
        foreach (Architecture arch in scenario.Architectures)
        {
            // 保存 Faction ID
            arch.BelongedFactionID = arch.BelongedFaction?.ID ?? 0;
            
            // ... 其他引用
        }
        
        // ... 其他对象类型
    }
}
```

**设计决策**：
- 在序列化前统一转换引用为 ID
- 使用 `SaveToString()` 方法处理集合
- 保持现有的 String 字段命名约定


## 数据模型

### 版本控制

GameScenario 包含一个版本号字段，用于跟踪存档格式版本：

```csharp
public class GameScenario
{
    /// <summary>
    /// 存档格式版本号
    /// </summary>
    [DataMember]
    public int SaveVersion { get; set; } = 1;
    
    // ... 其他字段
}
```

**版本演进策略**：

| 版本 | 描述 | 迁移逻辑 |
|------|------|----------|
| 0 | 旧的二进制格式 | 通过 BinaryScenarioHandler 加载，标记为 V0 |
| 1 | 初始 JSON 格式 | 当前版本 |
| 2+ | 未来版本 | 根据需要添加迁移逻辑 |

**迁移示例**：

假设在 V2 中，我们将 `Person.TreasuresString` 重命名为 `Person.TreasureIDs`：

```csharp
private static void MigrateV1ToV2(GameScenario scenario)
{
    Debug.WriteLine($"[GameLoader] V1→V2: 重命名 TreasuresString → TreasureIDs");
    
    foreach (Person person in scenario.Persons)
    {
        // 如果旧字段存在，迁移到新字段
        if (!string.IsNullOrEmpty(person.TreasuresString))
        {
            person.TreasureIDs = person.TreasuresString;
            person.TreasuresString = null; // 清除旧字段
        }
    }
}
```

**迁移原则**：
1. **向后兼容**：新版本能读取旧版本的存档
2. **逐步迁移**：V0→V1→V2→...，每步独立
3. **非破坏性**：迁移失败不应导致数据丢失
4. **可测试**：每个迁移步骤都有单元测试

### 元数据结构

为了支持快速浏览存档列表，每个存档都有一个对应的元数据文件：

```csharp
/// <summary>
/// 存档元数据（用于快速浏览存档列表）
/// </summary>
public class SaveMetadata
{
    /// <summary>
    /// 剧本标题
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// 保存时间
    /// </summary>
    public DateTime SaveTime { get; set; }
    
    /// <summary>
    /// 游戏日期
    /// </summary>
    public GameDateInfo GameDate { get; set; }
    
    /// <summary>
    /// 玩家势力名称
    /// </summary>
    public string PlayerFaction { get; set; }
    
    /// <summary>
    /// 玩家君主名称
    /// </summary>
    public string PlayerLeader { get; set; }
    
    /// <summary>
    /// 武将总数
    /// </summary>
    public int TotalPersons { get; set; }
    
    /// <summary>
    /// 建筑总数
    /// </summary>
    public int TotalArchitectures { get; set; }
    
    /// <summary>
    /// 部队总数
    /// </summary>
    public int TotalTroops { get; set; }
}

public class GameDateInfo
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    
    public override string ToString() => $"{Year}年{Month}月{Day}日";
}
```

**文件结构**：
```
Save01.sav.gz    <- 完整的游戏数据（压缩）
Save01.meta      <- 元数据（未压缩，约 1KB）
```

**优势**：
- 浏览存档列表时只需读取 .meta 文件（1KB）
- 无需解压和反序列化完整的游戏数据（可能几 MB）
- UI 响应速度快，不会卡顿

### ID 字段约定

每个需要序列化引用的类都应该有对应的 ID 字段：

```csharp
public class Person
{
    // 对象引用（不序列化）
    [JsonIgnore]
    public Faction BelongedFaction { get; set; }
    
    // ID 字段（序列化）
    [DataMember]
    public int BelongedFactionID { get; set; }
    
    // 集合引用（不序列化）
    [JsonIgnore]
    public TreasureList Treasures = new TreasureList();
    
    // 集合 ID 字符串（序列化）
    [DataMember]
    public string TreasuresString { get; set; }
}
```

### String 字段命名约定

集合引用的 String 字段遵循以下命名规则：

- 集合名 + "String"
- 例如：`Treasures` → `TreasuresString`
- 例如：`Architectures` → `ArchitecturesString`

### ID 字符串格式

集合 ID 使用逗号分隔的字符串：

```
"1,2,3,4,5"
```

空集合表示为空字符串或 null。

### CommonData 处理

CommonData 是静态配置数据，不应该序列化到存档中：

```csharp
public class GameScenario
{
    // CommonData 不序列化
    [JsonIgnore]
    public CommonData GameCommonData { get; set; }
    
    // 加载时从文件读取
    public void LoadCommonData()
    {
        GameCommonData = CommonData.LoadFromFile("Content/Data/CommonData.xml");
    }
}
```

**原因**：
1. 避免循环引用
2. 减小存档文件大小（CommonData 可能有几 MB）
3. 提高性能
4. CommonData 在游戏中不会改变


## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：序列化往返一致性

*对于任何* 有效的 GameScenario 对象，序列化为 JSON 然后反序列化（包括引用链接）应该产生等价的对象，其中所有字段值相同，所有对象引用指向正确的实例。

**验证需求**：8.1, 8.2, 8.3, 8.4, 4.5

**测试策略**：
- 创建包含各种对象的 GameScenario
- 保存为 JSON
- 加载并链接引用
- 验证所有字段值相同
- 验证所有引用指向正确对象

### 属性 2：压缩往返一致性

*对于任何* 有效的 GameScenario 对象，使用 GZip 压缩保存然后解压加载应该产生等价的对象。

**验证需求**：2.1, 2.2

**测试策略**：
- 创建 GameScenario
- 使用 CompressedJsonSerializer 保存
- 使用 CompressedJsonSerializer 加载
- 验证对象等价

### 属性 3：ID 序列化正确性

*对于任何* 包含对象引用的对象，序列化后的 JSON 应该只包含 ID 字段，不包含嵌套的对象引用。

**验证需求**：3.1, 3.3, 3.5

**测试策略**：
- 创建包含引用的对象（如 Person with BelongedFaction）
- 序列化为 JSON
- 解析 JSON 并验证只有 ID 字段
- 验证没有嵌套的对象结构

### 属性 4：压缩效率

*对于任何* GameScenario 对象，GZip 压缩后的文件大小应该不超过原始 JSON 大小的 1.5 倍。

**验证需求**：2.4

**测试策略**：
- 创建 GameScenario
- 序列化为 JSON 并记录大小
- 使用 GZip 压缩并记录大小
- 验证压缩大小 ≤ 原始大小 × 1.5


### 属性 5：文件格式自动检测

*对于任何* 有效的文件扩展名（.bin, .sav.gz, .json），GameLoader 应该自动选择正确的加载方法并成功加载。

**验证需求**：5.3

**测试策略**：
- 创建相同 GameScenario 的三种格式文件
- 使用 GameLoader 加载每种格式
- 验证所有格式都能正确加载
- 验证加载结果等价

### 属性 6：向后兼容性

*对于任何* 旧格式（.bin）的存档文件，使用 GameLoader 加载应该成功恢复所有游戏状态。

**验证需求**：5.4

**测试策略**：
- 使用旧的 BinaryScenarioHandler 保存存档
- 使用新的 GameLoader 加载
- 验证所有对象和引用正确恢复

### 属性 7：错误处理健壮性

*对于任何* 无效的文件格式或损坏的数据，系统应该抛出描述性异常而不是崩溃。

**验证需求**：10.2

**测试策略**：
- 创建各种无效输入（空文件、损坏的 JSON、无效的 GZip）
- 尝试加载
- 验证抛出适当的异常
- 验证异常消息有意义

### 属性 8：CommonData 排除

*对于任何* GameScenario 对象，序列化后的 JSON 不应该包含 CommonData 字段。

**验证需求**：11.1

**测试策略**：
- 创建包含 CommonData 的 GameScenario
- 序列化为 JSON
- 解析 JSON 并验证不包含 CommonData
- 验证文件大小显著小于包含 CommonData 的版本

### 属性 9：元数据一致性

*对于任何* GameScenario 对象，保存时生成的 `.meta` 文件应该包含与主存档一致的关键信息（标题、日期、玩家势力等）。

**验证需求**：2.6, 2.7, 2.8

**测试策略**：
- 创建 GameScenario
- 保存为 .sav.gz（同时生成 .meta）
- 读取 .meta 文件
- 验证元数据与原始 GameScenario 的关键字段一致
- 验证 .meta 文件大小 < 2KB

### 属性 10：元数据快速加载

*对于任何* 存档列表，浏览时只读取 `.meta` 文件应该比解压完整存档快至少 10 倍。

**验证需求**：2.10

**测试策略**：
- 创建多个存档（每个都有 .sav.gz 和 .meta）
- 测量读取所有 .meta 文件的时间
- 测量解压并反序列化所有 .sav.gz 文件的时间
- 验证 .meta 方式至少快 10 倍

### 属性 11：版本迁移正确性

*对于任何* 旧版本的存档，执行迁移后应该能够正确恢复所有游戏状态，并且版本号更新为当前版本。

**验证需求**：5.6, 5.8

**测试策略**：
- 创建不同版本的存档（V0, V1, ...）
- 使用 GameLoader 加载
- 验证迁移逻辑正确执行
- 验证 SaveVersion 更新为当前版本
- 验证所有数据正确恢复


## 错误处理

### 文件不存在

```csharp
public static GameScenario LoadScenario(string filePath)
{
    if (!File.Exists(filePath))
    {
        Debug.WriteLine($"[GameLoader] 错误：文件不存在 - {filePath}");
        return null;
    }
    // ...
}
```

### 文件格式无效

```csharp
try
{
    scenario = CompressedJsonSerializer.LoadCompressed<GameScenario>(filePath);
}
catch (InvalidDataException ex)
{
    throw new InvalidOperationException(
        $"无效的 GZip 格式: {filePath}", ex);
}
catch (JsonException ex)
{
    throw new InvalidOperationException(
        $"无效的 JSON 格式: {filePath}\n" +
        $"位置: {ex.Path}, 行: {ex.LineNumber}", ex);
}
```

### 引用链接失败

```csharp
private static void LinkPersonReferences(Person person, Dictionary<int, Faction> factions)
{
    if (person.BelongedFactionID > 0)
    {
        if (!factions.TryGetValue(person.BelongedFactionID, out var faction))
        {
            Debug.WriteLine(
                $"[ReferenceLinker] 警告：Person {person.Name}(ID:{person.ID}) " +
                $"的 Faction 引用无法解析 (FactionID:{person.BelongedFactionID})");
            // 不抛出异常，允许部分恢复
        }
        else
        {
            person.BelongedFaction = faction;
        }
    }
}
```

### CommonData 加载失败

```csharp
public static void LoadCommonData()
{
    try
    {
        CommonData.Current = CommonData.LoadFromFile("Content/Data/CommonData.xml");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "无法加载 CommonData，游戏无法继续", ex);
    }
}
```

**错误处理策略**：
- 文件不存在：返回 null，记录日志
- 格式错误：抛出描述性异常
- 引用缺失：记录警告，允许部分恢复
- 关键数据缺失：抛出异常，阻止继续


## 测试策略

### 单元测试

单元测试专注于具体示例和边缘情况：

1. **CompressedJsonSerializer 测试**
   - 测试简单对象的压缩/解压
   - 测试空对象
   - 测试大对象
   - 测试无效的 GZip 数据

2. **ReferenceLinker 测试**
   - 测试单个引用链接（Person.BelongedFaction）
   - 测试集合引用链接（Person.Treasures）
   - 测试缺失引用的处理
   - 测试循环引用的处理

3. **GameLoader 测试**
   - 测试 .bin 文件加载
   - 测试 .sav.gz 文件加载
   - 测试 .json 文件加载
   - 测试不支持的格式
   - 测试文件不存在

4. **GameSaver 测试**
   - 测试基本保存功能
   - 测试 ID 转换正确性
   - 测试 String 字段生成

### 属性测试

属性测试验证通用规则，每个测试运行 100+ 次随机输入：

1. **属性 1：序列化往返一致性**
   ```csharp
   [Property(Iterations = 100)]
   public void SerializationRoundTrip_PreservesAllData(GameScenario scenario)
   {
       // 保存
       var json = JsonSerializer.Serialize(scenario);
       
       // 加载
       var loaded = JsonSerializer.Deserialize<GameScenario>(json);
       ReferenceLinker.LinkAllReferences(loaded);
       
       // 验证
       Assert.Equal(scenario.Persons.Count, loaded.Persons.Count);
       Assert.Equal(scenario.Factions.Count, loaded.Factions.Count);
       // ... 验证所有字段
   }
   ```

2. **属性 2：压缩往返一致性**
   ```csharp
   [Property(Iterations = 100)]
   public void CompressionRoundTrip_PreservesData(GameScenario scenario)
   {
       var tempFile = Path.GetTempFileName() + ".sav.gz";
       
       CompressedJsonSerializer.SaveCompressed(scenario, tempFile);
       var loaded = CompressedJsonSerializer.LoadCompressed<GameScenario>(tempFile);
       
       Assert.NotNull(loaded);
       Assert.Equal(scenario.Persons.Count, loaded.Persons.Count);
   }
   ```

3. **属性 3：ID 序列化正确性**
   ```csharp
   [Property(Iterations = 100)]
   public void Serialization_OnlyContainsIDs(Person person)
   {
       var json = JsonSerializer.Serialize(person);
       var jsonObj = JsonDocument.Parse(json);
       
       // 验证只有 ID 字段，没有嵌套对象
       Assert.True(jsonObj.RootElement.TryGetProperty("BelongedFactionID", out _));
       Assert.False(jsonObj.RootElement.TryGetProperty("BelongedFaction", out _));
   }
   ```

4. **属性 4：压缩效率**
   ```csharp
   [Property(Iterations = 100)]
   public void Compression_MeetsEfficiencyTarget(GameScenario scenario)
   {
       var json = JsonSerializer.Serialize(scenario);
       var originalSize = Encoding.UTF8.GetByteCount(json);
       
       var tempFile = Path.GetTempFileName() + ".sav.gz";
       CompressedJsonSerializer.SaveCompressed(scenario, tempFile);
       var compressedSize = new FileInfo(tempFile).Length;
       
       Assert.True(compressedSize <= originalSize * 1.5);
   }
   ```

### 集成测试

集成测试验证完整的保存/加载流程：

1. **完整游戏状态往返测试**
   - 创建包含所有对象类型的完整 GameScenario
   - 保存为 .sav.gz
   - 加载并验证所有数据
   - 验证游戏逻辑可以正常运行

2. **向后兼容性测试**
   - 使用真实的旧版本存档
   - 使用新系统加载
   - 验证所有数据正确恢复

3. **性能测试**
   - 测试大型游戏状态的保存时间
   - 测试大型游戏状态的加载时间
   - 验证满足性能要求（保存 < 5s，加载 < 10s）

### 测试数据生成

使用属性测试库（如 FsCheck 或 CsCheck）生成随机测试数据：

```csharp
public static class Generators
{
    public static Gen<Person> PersonGen()
    {
        return from id in Gen.Choose(1, 10000)
               from name in Gen.Elements("张飞", "关羽", "刘备")
               from factionId in Gen.Choose(0, 100)
               select new Person 
               { 
                   ID = id, 
                   Name = name, 
                   BelongedFactionID = factionId 
               };
    }
    
    public static Gen<GameScenario> ScenarioGen()
    {
        return from persons in Gen.ListOf(PersonGen())
               from factions in Gen.ListOf(FactionGen())
               select new GameScenario
               {
                   Persons = new PersonList(persons),
                   Factions = new FactionList(factions)
               };
    }
}
```

**测试标签格式**：
```csharp
// Feature: json-serialization-unification, Property 1: 序列化往返一致性
[Property(Iterations = 100)]
public void SerializationRoundTrip_PreservesAllData(GameScenario scenario)
{
    // ...
}
```
