# NullReferenceException 调试规范

**日期：** 2026-03-16  
**状态：** 强制执行  
**原因：** 避免误判空引用异常的根本原因

---

## 🚨 核心规则

### ✅ 正确的调试流程

```csharp
// 异常堆栈：
// at Condition.CheckConditionList(ICollection<Condition> list, Architecture a, Event e)
// 第 125 行: foreach (Condition condition in list)

// ✅ 正确分析：第 125 行是 foreach 语句，说明 list 本身为 null
if (list == null) return false;  // 修复：参数验证

// ❌ 错误分析：误以为是 condition 或 condition.Kind 为 null
if (condition?.Kind != null)  // 这是 Band-Aid！
```

### 关键原则

1. **精确定位异常行**：异常堆栈指向的行号是关键
2. **分析语句类型**：
   - `foreach (var item in collection)` → `collection` 为 null
   - `item.Property` → `item` 为 null
   - `item.Property.SubProperty` → `Property` 为 null
3. **参数验证 vs 数据容错**：
   - 参数验证：方法入口检查参数是否为 null
   - 数据容错：循环内检查元素是否为 null

---

## 📋 调试检查清单

遇到 `NullReferenceException` 时，必须按以下步骤操作：

### 步骤 1：读取异常堆栈

```
System.NullReferenceException
at GameObjects.Conditions.Condition.CheckConditionList(ICollection`1 list, Architecture a, Event e)
在 Condition.cs 中: 第 125 行
```

**关键信息：**
- 方法名：`CheckConditionList`
- 参数：`ICollection<Condition> list`, `Architecture a`, `Event e`
- 异常行：第 125 行

### 步骤 2：查看异常行的代码

```csharp
// 第 125 行
foreach (Condition condition in list)
```

**分析：**
- 这是 `foreach` 语句
- `foreach` 抛出 `NullReferenceException` → `list` 为 null
- **不是** `condition` 或 `condition.Kind` 为 null

### 步骤 3：确定修复方案

```csharp
public static bool CheckConditionList(ICollection<Condition> list, Architecture a, Event e = null)
{
    // ✅ 参数验证：在方法入口检查
    if (list == null) return false;
    if (a == null) return false;
    
    foreach (Condition condition in list)
    {
        // ✅ 数据容错：在循环内检查元素
        if (condition == null) continue;
        if (condition.Kind == null) continue;
        
        // 业务逻辑...
    }
}
```

---

## ⚠️ 常见错误模式

### 错误 1：误判异常源

```csharp
// ❌ 错误：看到 NullReferenceException 就加 ?.
foreach (Condition condition in list)
{
    if (condition?.Kind?.ID == 996)  // Band-Aid！
}

// ✅ 正确：分析异常行，发现是 list 为 null
if (list == null) return false;
foreach (Condition condition in list)
```

### 错误 2：过度使用 Fail Fast

```csharp
// ❌ 错误：在数据加载场景使用 Fail Fast
if (list == null)
{
    throw new ArgumentNullException(nameof(list));  // 游戏无法启动！
}

// ✅ 正确：数据加载场景使用容错处理
if (list == null) return false;  // 返回"条件不满足"
```

**判断标准：**
- **Fail Fast 场景**：API 调用、业务逻辑错误、数据损坏
- **容错处理场景**：数据加载、反序列化、配置文件解析

### 错误 3：反复修复同一问题

```csharp
// 第 1 次修复：添加 condition.Kind 的空检查
if (condition.Kind == null) continue;  // 仍然崩溃

// 第 2 次修复：改为 Fail Fast
if (condition.Kind == null) throw new Exception();  // 游戏无法启动

// 第 3 次修复：回滚到原始代码  // 仍然崩溃

// ✅ 正确：分析异常堆栈，发现是 list 为 null
if (list == null) return false;
```

**教训：** 如果修复后仍然崩溃，说明没有找到根本原因，必须重新分析异常堆栈。

---

## 📝 代码模板

### 模板 1：参数验证

```csharp
public static bool CheckConditionList(ICollection<Condition> list, Architecture a, Event e = null)
{
    // 🔥 参数验证：在方法入口检查
    // 原因：调用者可能传入 null
    if (list == null) return false;
    if (a == null) return false;
    
    // 业务逻辑...
}
```

### 模板 2：数据容错

```csharp
foreach (Condition condition in list)
{
    // 🔥 数据容错：在循环内检查元素
    // 原因：反序列化时数据可能不完整
    if (condition == null)
    {
        #if DEBUG
        System.Diagnostics.Debug.WriteLine($"⚠️ Condition 集合中存在 null 元素");
        #endif
        continue;
    }
    
    if (condition.Kind == null)
    {
        #if DEBUG
        System.Diagnostics.Debug.WriteLine($"⚠️ Condition {condition.ID} 的 Kind 为 null");
        #endif
        continue;
    }
    
    // 业务逻辑...
}
```

### 模板 3：Fail Fast（仅用于 API 调用）

```csharp
public void ProcessData(DataObject data)
{
    // 🔥 Fail Fast：API 调用场景
    // 原因：调用者违反了契约
    if (data == null)
    {
        throw new ArgumentNullException(nameof(data), "数据对象不能为 null");
    }
    
    // 业务逻辑...
}
```

---

## 🎯 异常堆栈分析速查表

| 异常行代码 | 可能的 null 对象 | 修复方案 |
|-----------|----------------|---------|
| `foreach (var item in collection)` | `collection` | 参数验证：`if (collection == null)` |
| `item.Property` | `item` | 数据容错：`if (item == null)` |
| `item.Property.SubProperty` | `Property` | 数据容错：`if (item.Property == null)` |
| `list[index]` | `list` | 参数验证：`if (list == null)` |
| `dict[key]` | `dict` | 参数验证：`if (dict == null)` |
| `obj.Method()` | `obj` | 参数验证或数据容错 |

---

## 🔧 调试工具

### 工具 1：添加 DEBUG 日志

```csharp
#if DEBUG
System.Diagnostics.Debug.WriteLine($"[CheckConditionList] list={list}, a={a?.Name}(ID:{a?.ID})");
#endif
```

### 工具 2：使用条件断点

```csharp
// Visual Studio 条件断点：list == null
foreach (Condition condition in list)
```

### 工具 3：异常堆栈分析

```
异常堆栈：
at Condition.CheckConditionList(ICollection`1 list, Architecture a, Event e)
在 Condition.cs 中: 第 125 行

步骤：
1. 打开 Condition.cs
2. 跳转到第 125 行
3. 分析该行的语句类型
4. 确定哪个对象为 null
```

---

## 📚 相关文档

- [ANTI-BAND-AID 协议](System%20Prompt%20/%20Custom%20Instructions.md)
- [游戏核心机制](游戏核心机制.md)

---

## 📖 案例研究：Condition.CheckConditionList 修复

### 问题描述

```
System.NullReferenceException
at GameObjects.Conditions.Condition.CheckConditionList(ICollection`1 list, Architecture a, Event e)
在 Condition.cs 中: 第 125 行
```

### 错误的修复尝试

1. **第 1 次**：添加 `if (condition.Kind == null) continue;`
   - 结果：仍然崩溃
   - 原因：误判了异常源

2. **第 2 次**：改为 Fail Fast `throw new InvalidOperationException()`
   - 结果：游戏无法启动
   - 原因：数据加载场景不应使用 Fail Fast

3. **第 3 次**：回滚到原始代码
   - 结果：仍然崩溃
   - 原因：没有解决根本问题

### 正确的修复

```csharp
public static bool CheckConditionList(ICollection<Condition> list, Architecture a, Event e = null)
{
    // 🔥 关键修复：list 参数本身可能为 null
    // 日期：2026-03-16
    // 原因：数据加载时某些对象的 Conditions 集合未初始化
    if (list == null) return false;
    if (a == null) return false;
    
    foreach (Condition condition in list)  // 第 125 行
    {
        // 数据容错...
    }
}
```

### 教训总结

1. **精确分析异常行**：第 125 行是 `foreach` 语句，说明 `list` 为 null
2. **区分参数验证和数据容错**：`list` 是参数，需要在方法入口验证
3. **数据加载场景使用容错处理**：返回 `false` 而不是抛出异常
4. **如果修复后仍然崩溃**：重新分析异常堆栈，不要反复尝试同一方案

---

**最后更新：** 2026-03-16  
**维护者：** Lead Architect  
**教训来源：** Condition.CheckConditionList NullReferenceException 修复（反复 3 次才成功）
