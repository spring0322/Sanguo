# SimpleSerializer System.Text.Json 迁移设计文档

## 1. 设计概述

### 1.1 架构目标
本设计旨在将 SimpleSerializer 从 Newtonsoft.Json 完全迁移到 System.Text.Json，实现 Native AOT 兼容性，同时保持向后兼容性和性能优化。

### 1.2 设计原则
- **AOT 优先**: 所有设计决策优先考虑 AOT 兼容性
- **性能导向**: 优化序列化性能和内存使用
- **向后兼容**: 保持现有 API 和数据格式兼容
- **渐进迁移**: 支持新旧格式共存的过渡期

### 1.3 核心组件
```
SimpleSerializer (重构)
├── 核心序列化方法 (STJ 实现)
├── 兼容性检测器 (格式识别)
├── 性能优化器 (异步+缓存)
└── AOT 适配器 (源生成器集成)
```

## 2. 详细设计

### 2.1 核心序列化器重构

#### 2.1.1 新的 SerializeJson 实现
```csharp
/// <summary>
/// 使用 System.Text.Json 序列化对象
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
/// <param name="t">要序列化的对象</param>
/// <param name="zip">是否压缩</param>
/// <param name="indented">是否格式化</param>
/// <param name="net">遗留参数（兼容性）</param>
/// <returns>JSON 字符串</returns>
public static string SerializeJson<T>(T t, bool zip = false, bool indented = false, bool net = false)
{
    string result = null;
    lock (Platform.SerializerLock)
    {
        try
        {
            // 获取类型信息
            var typeInfo = GameJsonContext.Default.GetTypeInfo(typeof(T));
            if (typeInfo == null)
            {
                throw new NotSupportedException($"类型 {typeof(T).Name} 未在 GameJsonContext 中注册");
            }

            // 配置序列化选项
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.Preserve,
                TypeInfoResolver = GameJsonContext.Default
            };

            // 执行序列化
            result = JsonSerializer.Serialize(t, typeInfo, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleSerializer] STJ 序列化失败 {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }
    
    // 压缩处理
    if (zip)
    {
        result = result.GZipCompressString();
    }
    
    return result;
}
```

#### 2.1.2 新的 DeserializeJson 实现
```csharp
/// <summary>
/// 使用 System.Text.Json 反序列化对象，支持向后兼容
/// </summary>
/// <typeparam name="T">目标类型</typeparam>
/// <param name="s">JSON 字符串</param>
/// <param name="zip">是否压缩</param>
/// <param name="net">遗留参数</param>
/// <returns>反序列化的对象</returns>
public static T DeserializeJson<T>(string s, bool zip = false, bool net = false)
{
    if (zip)
    {
        s = s.GZipDecompressString();
    }

    T result = default(T);
    
    try
    {
        lock (Platform.SerializerLock)
        {
            // 1. 尝试 System.Text.Json 反序列化
            try
            {
                var typeInfo = GameJsonContext.Default.GetTypeInfo(typeof(T));
                if (typeInfo != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        ReferenceHandler = ReferenceHandler.Preserve,
                        TypeInfoResolver = GameJsonContext.Default
                    };
                    
                    result = (T)JsonSerializer.Deserialize(s, typeInfo, options);
                    
                    if (result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SimpleSerializer] STJ 反序列化成功: {typeof(T).Name}");
                        return result;
                    }
                }
            }
            catch (Exception stjEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializer] STJ 反序列化失败: {stjEx.Message}");
                
                // 2. 回退到 Newtonsoft.Json（临时兼容）
                if (IsLegacyFormat(s))
                {
                    System.Diagnostics.Debug.WriteLine("[SimpleSerializer] 检测到旧格式，使用 Newtonsoft.Json 回退");
                    result = DeserializeJsonLegacy<T>(s);
                    
                    if (result != null)
                    {
                        // 记录迁移建议
                        LogMigrationSuggestion(typeof(T).Name);
                        return result;
                    }
                }
                
                throw stjEx;
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[SimpleSerializer] 反序列化完全失败 {typeof(T).Name}: {ex.Message}");
        result = default(T);
    }
    
    return result;
}
```

### 2.2 兼容性检测器设计

#### 2.2.1 格式检测逻辑
```csharp
/// <summary>
/// 检测 JSON 字符串是否为旧格式
/// </summary>
/// <param name="json">JSON 字符串</param>
/// <returns>是否为旧格式</returns>
private static bool IsLegacyFormat(string json)
{
    if (string.IsNullOrEmpty(json)) return false;
    
    try
    {
        // 检测 Newtonsoft.Json 特有的格式标记
        var legacyIndicators = new[]
        {
            "\"$type\":",           // TypeNameHandling
            "\"$id\":",             // PreserveReferencesHandling
            "\"$ref\":",            // 引用标记
            "\"$values\":",         // 数组包装
        };
        
        foreach (var indicator in legacyIndicators)
        {
            if (json.Contains(indicator))
            {
                return true;
            }
        }
        
        // 检测旧版本特有的数据结构
        if (json.Contains("\"Newtonsoft.Json.") || 
            json.Contains("\"WorldOfTheThreeKingdoms.Serialization.Legacy"))
        {
            return true;
        }
        
        return false;
    }
    catch
    {
        return false;
    }
}
```

#### 2.2.2 迁移建议记录
```csharp
/// <summary>
/// 记录迁移建议
/// </summary>
/// <param name="typeName">类型名</param>
private static void LogMigrationSuggestion(string typeName)
{
    var message = $"[迁移建议] 类型 {typeName} 使用了旧格式，建议重新保存以使用新格式";
    System.Diagnostics.Debug.WriteLine(message);
    
    // 可以添加到迁移日志文件
    // MigrationLogger.LogSuggestion(typeName, message);
}
```

### 2.3 GameJsonContext 完善设计

#### 2.3.1 类型注册策略
```csharp
[JsonSerializable(typeof(GameObjects.GameScenario))]
[JsonSerializable(typeof(GameObjects.CommonData))]

// 核心游戏对象
[JsonSerializable(typeof(GameObjects.Person))]
[JsonSerializable(typeof(GameObjects.Troop))]
[JsonSerializable(typeof(GameObjects.Architecture))]
[JsonSerializable(typeof(GameObjects.Faction))]
[JsonSerializable(typeof(GameObjects.Legion))]
[JsonSerializable(typeof(GameObjects.Military))]

// 集合类型
[JsonSerializable(typeof(GameObjects.PersonList))]
[JsonSerializable(typeof(GameObjects.TroopListWithQueue))]
[JsonSerializable(typeof(GameObjects.ArchitectureList))]
[JsonSerializable(typeof(GameObjects.FactionListWithQueue))]

// 字典类型（关键）
[JsonSerializable(typeof(Dictionary<int, GameObjects.PersonDetail.Biography>))]
[JsonSerializable(typeof(Dictionary<int, GameObjects.FactionDetail.DiplomaticRelation>))]
[JsonSerializable(typeof(Dictionary<Point, GameObjects.NoFoodPosition>))]
[JsonSerializable(typeof(Dictionary<int, GameObjects.ArchitectureDetail.ArchitectureKind>))]

// 基础类型
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<int, int>))]
[JsonSerializable(typeof(Dictionary<string, string>))]

public partial class GameJsonContext : JsonSerializerContext
{
    /// <summary>
    /// 获取默认序列化选项
    /// </summary>
    public static JsonSerializerOptions GetDefaultOptions(bool indented = false)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = null, // 保持原始属性名
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = GameJsonContext.Default,
            Converters =
            {
                new GameObjectListConverter(),
                new GameObjectReferenceConverter(),
                new FactionLeaderConverter(),
                new PersonIdealTendencyConverter()
            }
        };
    }
}
```

### 2.4 自定义转换器迁移设计

#### 2.4.1 GameObjectListConverter 设计
```csharp
/// <summary>
/// System.Text.Json 版本的游戏对象列表转换器
/// 替换 LegacyGameObjectListConverter
/// </summary>
public class GameObjectListConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        // 检查是否为游戏对象列表类型
        return typeToConvert.Name.EndsWith("List") && 
               typeToConvert.Namespace?.StartsWith("GameObjects") == true;
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 实现旧格式兼容的读取逻辑
        // 支持 {"1": {...}, "2": {...}} 格式
        
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // 旧格式：字典形式
            return ReadLegacyDictionaryFormat(ref reader, typeToConvert, options);
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            // 新格式：数组形式
            return ReadArrayFormat(ref reader, typeToConvert, options);
        }
        
        throw new JsonException($"无法解析 {typeToConvert.Name} 的 JSON 格式");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        // 始终使用新格式（数组）写入
        WriteArrayFormat(writer, value, options);
    }
    
    private object ReadLegacyDictionaryFormat(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 实现旧字典格式的读取逻辑
        // ...
    }
    
    private object ReadArrayFormat(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 实现新数组格式的读取逻辑
        // ...
    }
    
    private void WriteArrayFormat(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        // 实现数组格式的写入逻辑
        // ...
    }
}
```

#### 2.4.2 其他关键转换器
- **GameObjectReferenceConverter**: 处理对象引用和循环引用
- **FactionLeaderConverter**: 处理势力领袖关系
- **PersonIdealTendencyConverter**: 处理人物理想倾向

### 2.5 性能优化设计

#### 2.5.1 异步序列化支持
```csharp
/// <summary>
/// 异步序列化方法
/// </summary>
public static async Task<string> SerializeJsonAsync<T>(T t, bool zip = false, bool indented = false, CancellationToken cancellationToken = default)
{
    var typeInfo = GameJsonContext.Default.GetTypeInfo(typeof(T));
    var options = GameJsonContext.GetDefaultOptions(indented);
    
    using var stream = new MemoryStream();
    await JsonSerializer.SerializeAsync(stream, t, typeInfo, options, cancellationToken);
    
    var result = Encoding.UTF8.GetString(stream.ToArray());
    
    if (zip)
    {
        result = await Task.Run(() => result.GZipCompressString(), cancellationToken);
    }
    
    return result;
}

/// <summary>
/// 异步反序列化方法
/// </summary>
public static async Task<T> DeserializeJsonAsync<T>(string s, bool zip = false, CancellationToken cancellationToken = default)
{
    if (zip)
    {
        s = await Task.Run(() => s.GZipDecompressString(), cancellationToken);
    }
    
    var typeInfo = GameJsonContext.Default.GetTypeInfo(typeof(T));
    var options = GameJsonContext.GetDefaultOptions();
    
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
    return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
}
```

#### 2.5.2 内存优化策略
- 使用 `ArrayPool<T>` 减少数组分配
- 实现对象池缓存常用对象
- 优化字符串处理，减少临时字符串创建
- 使用 `Span<T>` 和 `Memory<T>` 进行零拷贝操作

### 2.6 AOT 兼容性集成

#### 2.6.1 与现有 AOTCompatibilityAdapter 集成
```csharp
/// <summary>
/// AOT 兼容的序列化方法
/// </summary>
public static string SerializeJsonAOT<T>(T t, IAOTCompatibilityAdapter aotAdapter = null) where T : GameObject
{
    aotAdapter ??= new AOTCompatibilityAdapter();
    
    // 验证 AOT 兼容性
    if (!aotAdapter.IsAOTCompatible(typeof(T)))
    {
        throw new NotSupportedException($"类型 {typeof(T).Name} 不支持 AOT 序列化");
    }
    
    // 使用 AOT 友好的序列化
    return SerializeJson(t);
}
```

#### 2.6.2 编译时类型验证
```csharp
/// <summary>
/// 编译时验证所有注册类型的 AOT 兼容性
/// </summary>
public static class AOTSerializationValidator
{
    public static void ValidateAllTypes()
    {
        var analyzer = new AOTSerializationCompatibilityAnalyzer();
        var report = analyzer.AnalyzeCompatibility();
        
        if (!report.IsAOTCompatible)
        {
            var issues = string.Join("\n", report.Issues.Select(i => $"- {i.TypeName}: {i.Description}"));
            throw new InvalidOperationException($"发现 AOT 兼容性问题:\n{issues}");
        }
    }
}
```

## 3. 数据流设计

### 3.1 序列化流程
```
输入对象 (T)
    ↓
类型验证 (GameJsonContext)
    ↓
AOT 兼容性检查
    ↓
JsonSerializer.Serialize
    ↓
压缩处理 (可选)
    ↓
输出 JSON 字符串
```

### 3.2 反序列化流程
```
输入 JSON 字符串
    ↓
解压缩处理 (可选)
    ↓
格式检测 (新/旧)
    ↓
┌─ 新格式 → JsonSerializer.Deserialize (STJ)
└─ 旧格式 → 回退处理 (Newtonsoft.Json)
    ↓
对象后处理 (GameScenario 集合初始化)
    ↓
输出对象 (T)
```

## 4. 错误处理设计

### 4.1 异常层次结构
```csharp
public class SerializationException : Exception
{
    public string TypeName { get; }
    public string Operation { get; }
    
    public SerializationException(string typeName, string operation, string message, Exception innerException = null)
        : base($"序列化操作失败 [{operation}] {typeName}: {message}", innerException)
    {
        TypeName = typeName;
        Operation = operation;
    }
}

public class AOTCompatibilityException : SerializationException
{
    public AOTCompatibilityException(string typeName, string message)
        : base(typeName, "AOT兼容性检查", message)
    {
    }
}
```

### 4.2 错误恢复策略
- **序列化失败**: 记录错误，抛出异常
- **反序列化失败**: 尝试旧格式回退
- **AOT 兼容性问题**: 提供详细的修复建议
- **数据损坏**: 提供数据修复工具

## 5. 测试策略

### 5.1 单元测试设计
```csharp
[TestClass]
public class SimpleSerializerTests
{
    [TestMethod]
    public void SerializeJson_Person_Success()
    {
        // 测试 Person 对象序列化
    }
    
    [TestMethod]
    public void DeserializeJson_LegacyFormat_Success()
    {
        // 测试旧格式兼容性
    }
    
    [TestMethod]
    public void SerializeDeserialize_RoundTrip_Success()
    {
        // 测试往返一致性
    }
    
    [TestMethod]
    public void AOTCompatibility_AllTypes_Success()
    {
        // 测试 AOT 兼容性
    }
}
```

### 5.2 性能基准测试
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SerializationBenchmarks
{
    [Benchmark]
    public string SerializeGameScenario_NewtonSoft() { }
    
    [Benchmark]
    public string SerializeGameScenario_SystemTextJson() { }
    
    [Benchmark]
    public GameScenario DeserializeGameScenario_NewtonSoft() { }
    
    [Benchmark]
    public GameScenario DeserializeGameScenario_SystemTextJson() { }
}
```

## 6. 部署策略

### 6.1 渐进式迁移
1. **阶段 1**: 实现新方法，保持旧方法并存
2. **阶段 2**: 默认使用新方法，旧方法作为回退
3. **阶段 3**: 移除 Newtonsoft.Json 依赖
4. **阶段 4**: 清理旧代码和注释

### 6.2 回滚计划
- 保留旧版本 SimpleSerializer 作为备份
- 提供快速回滚脚本
- 监控关键指标，异常时自动回滚

## 7. 监控与日志

### 7.1 性能监控
- 序列化/反序列化耗时
- 内存使用情况
- GC 压力指标
- 异常发生率

### 7.2 迁移状态跟踪
- 新旧格式使用比例
- 迁移建议触发次数
- AOT 兼容性问题统计

---

**设计版本**: 1.0  
**创建日期**: 2026-01-30  
**设计负责人**: 开发团队  
**技术审核**: 架构师