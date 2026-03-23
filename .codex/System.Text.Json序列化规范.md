# System.Text.Json 序列化规范

**日期：** 2026-03-20  
**状态：** 强制执行  
**原因：** 避免 ReferenceHandler.Preserve 对简单集合的错误处理

---

## 🚨 核心问题

### ReferenceHandler.Preserve 的陷阱

**问题描述**：
- `ReferenceHandler.Preserve` 设计用于处理循环引用（如 `Person → Faction → Person`）
- 但它对**所有集合**都使用引用保留格式，包括简单值类型集合（如 `List<int>`）
- 简单值类型集合不可能有循环引用，不需要引用保留
- 引用保留格式与简单集合的正常格式不兼容，导致反序列化失败

**错误的序列化格式**：
```json
{
  "SkillIDs": {"$id":"5","$values":[21]},      // ❌ 引用保留格式
  "TreasureIDs": {"$id":"4","$values":[]},
  "TitleIDs": {"$id":"7","$values":[4015]}
}
```

**正确的序列化格式**：
```json
{
  "SkillIDs": [21],              // ✅ 正常格式
  "TreasureIDs": [],
  "TitleIDs": [4015]
}
```

**后果**：
- 反序列化时，`{"$id":"5","$values":[21]}` 无法正确解析为 `List<int>`
- 结果：`SkillIDs` 变成空列表 `[]`
- 导致：技能、特技、称号等数据丢失

---

## ✅ 解决方案

### 使用自定义 ReferenceHandler

**核心思路**：
1. 继承 `ReferenceHandler.Preserve`
2. 对简单值类型集合（`List<int>`, `List<float>`, `List<string>` 等）禁用引用保留
3. 对复杂对象集合（`List<Person>`, `List<Architecture>` 等）保持引用保留

**实现代码**：

```csharp
/// <summary>
/// 自定义 ReferenceHandler：对简单值类型集合（如 List<int>）禁用引用保留
/// 
/// 问题：ReferenceHandler.Preserve 会将 List<int> 序列化为 {"$id":"5","$values":[21]}
///       反序列化时无法正确解析为 List<int>，导致集合变成空列表
/// 
/// 解决：继承 ReferenceHandler.Preserve，但对简单值类型集合返回 null（不使用引用保留）
/// 
/// 日期：2026-03-20
/// </summary>
public class SimpleCollectionReferenceHandler : ReferenceHandler
{
    private readonly ReferenceHandler _preserveHandler = ReferenceHandler.Preserve;
    private ReferenceResolver? _resolver;

    public override ReferenceResolver CreateResolver()
    {
        // 🔥 C# 12：使用 ??= 空合并赋值
        return _resolver ??= new SimpleCollectionReferenceResolver(_preserveHandler.CreateResolver());
    }

    private class SimpleCollectionReferenceResolver(ReferenceResolver innerResolver) : ReferenceResolver
    {
        private readonly ReferenceResolver _innerResolver = innerResolver;

        public override void AddReference(string referenceId, object value)
        {
            // 🔥 关键：对简单值类型集合不添加引用
            if (IsSimpleCollection(value?.GetType()))
            {
                return;
            }
            _innerResolver.AddReference(referenceId, value);
        }

        public override string GetReference(object value, out bool alreadyExists)
        {
            // 🔥 关键：对简单值类型集合不使用引用
            if (IsSimpleCollection(value?.GetType()))
            {
                alreadyExists = false;
                return null;
            }
            return _innerResolver.GetReference(value, out alreadyExists);
        }

        public override object ResolveReference(string referenceId)
        {
            return _innerResolver.ResolveReference(referenceId);
        }

        /// <summary>
        /// 判断类型是否是简单值类型集合（不需要引用保留）
        /// </summary>
        private static bool IsSimpleCollection(Type? type)
        {
            if (type == null) return false;

            // List<int>, List<float>, List<string> 等
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) || genericTypeDef == typeof(IList<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    // 值类型（int, float, bool 等）或 string
                    return elementType.IsValueType || elementType == typeof(string);
                }
            }

            // int[], float[], string[] 等
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return elementType?.IsValueType == true || elementType == typeof(string);
            }

            return false;
        }
    }
}
```

**使用方式**：

```csharp
private static void InitializeOptions()
{
    // 🔥 关键修复：使用自定义 ReferenceHandler
    var referenceHandler = new SimpleCollectionReferenceHandler();
    
    _serializerOptions = new JsonSerializerOptions
    {
        ReferenceHandler = referenceHandler, // 🔥 使用自定义 ReferenceHandler
        // ...
    };
    
    _deserializerOptions = new JsonSerializerOptions
    {
        ReferenceHandler = referenceHandler, // 🔥 使用自定义 ReferenceHandler
        // ...
    };
}
```

---

## 🔍 诊断方法

### 如何发现这类问题

1. **症状**：
   - 保存时数据正常
   - 读档后集合变成空列表
   - 日志显示：`SkillIDs.Count = 0`（但存档文件中有数据）

2. **检查存档文件**：
   ```bash
   grep -A 5 '"SkillIDs"' Save/Save08.sav/Save08.sav
   ```
   
   **发现问题**：
   ```json
   "SkillIDs":{"$id":"5","$values":[21]}  // ❌ 引用保留格式
   ```

3. **确认根本原因**：
   - 序列化时：`ReferenceHandler.Preserve` 生成引用保留格式
   - 反序列化时：System.Text.Json 无法将引用保留格式解析为 `List<int>`

---

## ⚠️ 需要检查的集合类型

### 简单值类型集合（需要禁用引用保留）

| 类型 | 示例 | 说明 |
|------|------|------|
| `List<int>` | `SkillIDs`, `TreasureIDs`, `TitleIDs` | 整数 ID 列表 |
| `List<float>` | 数值配置列表 | 浮点数列表 |
| `List<string>` | `Tags`, 字符串列表 | 字符串列表 |
| `int[]` | 数组 | 整数数组 |
| `Dictionary<int, int>` | ID 映射 | 简单键值对 |

### 复杂对象集合（需要保持引用保留）

| 类型 | 示例 | 说明 |
|------|------|------|
| `List<Person>` | `Persons` | 可能有循环引用 |
| `List<Architecture>` | `Architectures` | 可能有循环引用 |
| `GameObjectList<T>` | 所有游戏对象集合 | 可能有循环引用 |

---

## 📋 检查清单

在使用 System.Text.Json 序列化时，必须检查：

1. ✅ 是否使用了 `ReferenceHandler.Preserve`？
2. ✅ 是否有简单值类型集合（`List<int>`, `List<string>` 等）？
3. ✅ 是否使用了自定义 `ReferenceHandler` 来禁用简单集合的引用保留？
4. ✅ 是否在存档文件中检查了序列化格式？
5. ✅ 是否测试了读档后集合数据是否正确？

---

## 🎯 最佳实践

### 1. 优先使用简单格式

**推荐**：
```csharp
public class PersonDTO
{
    public List<int> SkillIDs { get; set; } = [];  // ✅ 简单格式
}
```

**避免**：
```csharp
public class PersonDTO
{
    public List<Skill> Skills { get; set; } = [];  // ❌ 复杂对象，可能触发引用保留
}
```

### 2. 分离 ID 和对象引用

**正确的设计**：
```csharp
public class Person
{
    // 序列化：只保存 ID 列表
    public List<int> SkillIDs { get; set; } = [];
    
    // 运行时：对象引用（不序列化）
    [JsonIgnore]
    public Skills Skills { get; private set; }
}
```

### 3. 测试序列化格式

**验证步骤**：
1. 保存游戏
2. 打开存档文件（JSON）
3. 检查集合字段格式：
   - ✅ 正常：`"SkillIDs": [21]`
   - ❌ 错误：`"SkillIDs": {"$id":"5","$values":[21]}`

---

## 📚 相关文档

- [数据加载问题诊断规范](数据加载问题诊断规范.md)
- [ANTI-BAND-AID 协议](../global-rules.md)
- [读档技能丢失问题修复_2026-03-20.md](../../读档技能丢失问题修复_2026-03-20.md)

---

## 📖 案例研究：读档后技能丢失问题

### 问题描述

用户报告：读取存档后，人物的技能、特技、称号不显示，但新开剧本正常显示。

### 诊断过程

1. **初步诊断**：
   - 日志显示：`SkillIDs` 正确加载（`[21]`），但 `Skills.Count = 0`
   - 发现：`LinkReferences()` 方法没有被调用

2. **第一次修复**：
   - 在 `AfterLoadSaveFile()` 中添加 `LinkReferences()` 调用
   - 结果：仍然失败，`SkillIDs.Count = 0`

3. **深入诊断**：
   - 检查保存阶段：数据正常（`Skills.Skills.Count = 1`, `SkillIDs = [21]`）
   - 检查存档文件：发现引用保留格式 `{"$id":"5","$values":[21]}`
   - **根本原因**：反序列化时无法解析引用保留格式

4. **最终修复**：
   - 创建 `SimpleCollectionReferenceHandler`
   - 对简单值类型集合禁用引用保留
   - 结果：序列化为正常格式 `[21]`，反序列化成功

### 教训总结

1. **不要假设序列化格式**：打开存档文件查看实际格式
2. **追溯完整数据流**：保存 → 序列化 → 反序列化 → 加载
3. **测试边界情况**：空列表、单元素列表、多元素列表
4. **验证修复效果**：新开剧本 → 保存 → 读档 → 检查数据

---

**最后更新**：2026-03-20  
**维护者**：Lead Architect  
**教训来源**：读档后技能丢失问题修复（反复诊断 10 次才找到根本原因）
