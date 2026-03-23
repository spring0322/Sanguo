# ID 判断规范

**日期：** 2026-03-09  
**状态：** 强制执行  
**原因：** ID=0 是有效的游戏对象 ID（洛阳、步兵、阿会喃等）

---

## 🚨 核心规则

### ✅ 正确的 ID 判断

```csharp
// ✅ 正确：ID >= 0 表示有效引用
if (architectureID >= 0)
{
    // 处理引用
}

// ✅ 正确：ID < 0 或 ID == -1 表示无效引用
if (architectureID < 0)
{
    // 无引用
}
```

### ❌ 错误的 ID 判断

```csharp
// ❌ 错误：会跳过 ID=0 的对象
if (architectureID > 0)
{
    // 这会导致洛阳（ID=0）被跳过！
}

// ❌ 错误：ID=0 不表示无效
if (architectureID == 0)
{
    // 错误假设：ID=0 是无效的
}
```

---

## 📊 ID=0 的有效对象列表

| 对象类型 | ID=0 的对象 | 说明 |
|---------|------------|------|
| Architecture | 洛阳 | 首都，游戏中最重要的城市 |
| MilitaryKind | 步兵 | 基础兵种 |
| Person | 阿会喃 | 武将 |
| Biography | 阿会喃列传 | 人物传记 |
| IdealTendencyKind | 默认理想倾向 | 配置数据 |

---

## ⚠️ 例外情况：ID 从 1 开始的对象类型

**警告：** 以下对象类型不遵循 ID=0 规范，ID 从 1 开始分配，ID=0 表示"无引用"。

| 对象类型 | 第一个有效 ID | ID=0 的含义 | 判断条件 | 数据位置 |
|---------|-------------|-----------|---------|---------|
| TrainPolicy | 1（均衡） | 未设置训练策略 | `> 0` | CommonData.json |

### 代码示例

```csharp
// ✅ 正确：TrainPolicy 使用 > 0 判断
if (person.TrainPolicyIDString > 0)
{
    person.TrainPolicy = scenario.GameCommonData.AllTrainPolicies
        .GetGameObject(person.TrainPolicyIDString) as TrainPolicy;
}

// ❌ 错误：TrainPolicy 不能使用 >= 0
if (person.TrainPolicyIDString >= 0)  // 会尝试查找不存在的 ID=0！
{
    // 这会产生警告：references non-existent TrainPolicy 0
}
```

### 历史原因

这是历史遗留的设计不一致问题。在项目早期，TrainPolicy 被设计为"可选配置"，ID=0 表示"使用默认策略"或"未设置"。但这与项目后来确立的 ID=0 规范（ID=0 是有效对象）相冲突。

### 长期计划

在下一个大版本中，应该：
1. 创建 ID=0 的 TrainPolicy（名称："默认"或"未设置"）
2. 将所有 `TrainPolicyIDString: 0` 的武将迁移到新的默认策略
3. 统一使用 `>= 0` 判断

---

## 🔧 修复检查清单

在编写涉及 ID 判断的代码时，必须检查：

1. ✅ 是否使用 `>= 0` 而不是 `> 0`？
2. ✅ 是否使用 `-1` 表示无效引用？
3. ✅ 是否在注释中说明 ID=0 是有效的？
4. ✅ 是否考虑了 ID=0 的边界情况？
5. ⚠️ **是否检查了例外情况列表**（如 TrainPolicy）？

---

## 📝 代码模板

### 引用链接模板

```csharp
// 🔥 关键：ID=0 是有效的，必须使用 >= 0
if (obj.ReferenceID >= 0)
{
    if (lookupTable.TryGetValue(obj.ReferenceID, out var reference))
    {
        obj.Reference = reference;
    }
    else
    {
        // ANTI-BAND-AID：Fail Fast
        throw new InvalidOperationException(
            $"数据损坏：{obj.GetType().Name} {obj.ID} 引用了不存在的对象 {obj.ReferenceID}");
    }
}
```

### 延迟加载模板

```csharp
public Architecture StartingArchitecture
{
    get
    {
        // 🔥 关键：ID=0 是有效的，必须使用 >= 0
        if (startingArchitecture == null && StartingArchitectureID >= 0)
        {
            startingArchitecture = Session.Current.Scenario.Architectures
                .GetGameObject(StartingArchitectureID) as Architecture;
        }
        return startingArchitecture;
    }
    set
    {
        startingArchitecture = value;
        StartingArchitectureID = value?.ID ?? -1;  // -1 表示无引用
    }
}
```

---

## 🎯 长期解决方案

### 方案：ID 从 1 开始分配

**目标：** 在下一个大版本中，将所有 ID 从 1 开始分配

**步骤：**
1. 创建存档迁移工具，将所有 ID +1
2. 修改 `GetFreeGameObjectID` 从 1 开始
3. 全局替换 `>= 0` 回 `> 0`
4. 更新所有配置文件

**优点：**
- `ID > 0` 判断有意义
- 符合业界最佳实践
- 彻底消除歧义

---

## ⚠️ 常见陷阱

### 陷阱1：复制旧代码

```csharp
// ❌ 从旧代码复制的错误判断
if (troop.StartingArchitectureID > 0)  // 会跳过洛阳！
```

**解决：** 使用代码审查工具检测 `> 0` 模式

### 陷阱2：假设 ID=0 无效

```csharp
// ❌ 错误假设
if (person.ID == 0)
{
    // 认为这是无效武将，但阿会喃的 ID 就是 0！
}
```

**解决：** 使用 `ID < 0` 判断无效

### 陷阱3：序列化时跳过 ID=0

```csharp
// ❌ 序列化时错误跳过
if (obj.ReferenceID > 0)  // 洛阳的引用会丢失！
{
    dto.ReferenceID = obj.ReferenceID;
}
```

**解决：** 使用 `>= 0` 或直接赋值

---

## 📚 相关文档

- [部队出发地ID=0引用错误修复报告](../部队出发地ID=0引用错误修复_2026-03-09.md)
- [ANTI-BAND-AID 协议](System%20Prompt%20/%20Custom%20Instructions.md)

---

**最后更新：** 2026-03-17  
**维护者：** Lead Architect  
**更新日志：**
- 2026-03-17：添加例外情况章节（TrainPolicy）
- 2026-03-09：初始版本
