# GetStratagemSuccessChanceCredit 空引用异常修复报告

**日期：** 2026-03-23  
**状态：** 已完成  
**修复类型：** 设计重构（ANTI-BAND-AID）

---

## 🚨 问题描述

### 异常信息

```
System.NullReferenceException
at GameObjects.Troop.GetStratagemSuccessChanceCredit(Troop troop, Boolean inevitableSuccess, Boolean invincible, Boolean lowerIntelligenceInvincible)
在 Troop.cs 中: 第 10014 行
```

### 异常行代码

```csharp
// 第 10014 行
if (!this.CurrentStratagem.Friendly)
```

### 根本原因

**设计问题：** `GetStratagemSuccessChanceCredit` 方法依赖 `this.CurrentStratagem` 实例字段，但在 AI 评估阶段调用此方法时，`CurrentStratagem` 尚未设置（为 null）。

**调用链：**
```
Stratagem.GetCredit(source, destination)
  → Influence.GetCredit(source, destination)
    → InfluenceKind.GetCredit(source, destination)
      → source.GetStratagemSuccessChanceCredit(troop, ...)
        → this.CurrentStratagem.Friendly  // ❌ CurrentStratagem 为 null
```

**时序问题：**
- AI 评估阶段：遍历所有可用计略，调用 `Stratagem.GetCredit` 评估收益
- 此时 `Troop.CurrentStratagem` 尚未设置（AI 还在选择计略）
- `GetStratagemSuccessChanceCredit` 访问 `this.CurrentStratagem` 导致空引用

---

## ❌ 错误的修复方案（Band-Aid）

### 方案 1：添加防御性空检查

```csharp
public int GetStratagemSuccessChanceCredit(Troop troop, bool inevitableSuccess, bool invincible, bool lowerIntelligenceInvincible)
{
    // ❌ Band-Aid：掩盖设计问题
    if (this.CurrentStratagem == null) return 0;
    
    if (!this.CurrentStratagem.Friendly)
    {
        // ...
    }
}
```

**为什么错误：**
- 掩盖了设计问题：方法依赖不稳定的实例状态
- 返回 0 是错误的：AI 无法正确评估计略收益
- 违反 ANTI-BAND-AID 协议：没有追溯数据源

---

## ✅ 正确的修复方案（设计重构）

### 核心思路

**职责分离：** 将 `stratagem` 作为显式参数传入，而不是依赖实例字段 `this.CurrentStratagem`。

### 修复步骤

#### 步骤 1：修改 `GetStratagemSuccessChanceCredit` 方法签名

```csharp
// ✅ 修改前
public int GetStratagemSuccessChanceCredit(Troop troop, bool inevitableSuccess, bool invincible, bool lowerIntelligenceInvincible)
{
    if (!this.CurrentStratagem.Friendly)  // ❌ 依赖实例字段
    {
        // ...
    }
}

// ✅ 修改后
public int GetStratagemSuccessChanceCredit(Stratagem stratagem, Troop troop, bool inevitableSuccess, bool invincible, bool lowerIntelligenceInvincible)
{
    if (!stratagem.Friendly)  // ✅ 使用参数
    {
        // ...
    }
}
```

#### 步骤 2：修改 `InfluenceKind.GetCredit` 基类方法签名

```csharp
// ✅ 修改前
public virtual int GetCredit(Troop source, Troop destination)
{
    return 0;
}

// ✅ 修改后
public virtual int GetCredit(Troop source, Troop destination, Stratagem stratagem = null)
{
    return 0;
}
```

**注意：** `stratagem` 参数设为可选（nullable），因为 `CombatMethod` 也会调用 `GetCredit`，但它不需要 stratagem。

#### 步骤 3：更新所有 InfluenceKind 子类

修改了 8 个 InfluenceKind 子类的 `GetCredit` 方法：

1. `InfluenceKind390.cs` - 攻心计略
2. `InfluenceKind391.cs` - 扰乱计略
3. `InfluenceKind394.cs` - 火攻计略
4. `InfluenceKind395.cs` - 恢复计略
5. `InfluenceKind397.cs` - 鼓舞计略
6. `InfluenceKind399.cs` - 恢复计略
7. `InfluenceKind720.cs` - 谣言计略
8. `InfluenceKind721.cs` - 吸引计略

**修改示例：**

```csharp
// ✅ InfluenceKind391.cs（扰乱计略）
public override int GetCredit(Troop source, Troop destination, Stratagem stratagem)
{
    int num = 0;
    int pureFightingForce = source.PureFightingForce;
    foreach (Troop troop in source.GetAreaStratagemTroops(destination, false))
    {
        // ✅ 传递 stratagem 参数
        int num3 = source.GetStratagemSuccessChanceCredit(stratagem, troop, 
            source.InevitableRaoluanOnLowerIntelligence || source.InevitableStratagemOnLowerIntelligence, 
            (troop.NeverBeIntoChaos || troop.OutburstNeverBeIntoChaos) || troop.InvincibleRaoluan, 
            troop.InvincibleStratagemFromLowerIntelligence);
        // ...
    }
    return num;
}
```

#### 步骤 4：更新 `Influence.GetCredit` 包装方法

```csharp
// ✅ 修改后
public int GetCredit(Troop source, Troop destination, Stratagem stratagem = null)
{
    return this.Kind.GetCredit(source, destination, stratagem);
}
```

#### 步骤 5：更新 `Stratagem.GetCredit` 调用链

```csharp
// ✅ 修改后
public int GetCredit(Troop source, Troop destination)
{
    if (!source.HasStratagem(this.ID)) { return 0; }
    int num = 0;
    foreach (Influence influence in this.Influences.Influences.Values)
    {
        // ✅ 传递 this（当前计略）
        num += influence.GetCredit(source, destination, this);
    }
    return num;
}
```

#### 步骤 6：添加 using 指令

所有 InfluenceKind 子类需要引用 `GameObjects.TroopDetail` 命名空间：

```csharp
using GameObjects;
using GameObjects.Influences;
using GameObjects.TroopDetail;  // ✅ 添加此行
using System;
using System.Runtime.Serialization;
```

---

## 🎯 修复效果

### 修复前

```
AI 评估计略 → Stratagem.GetCredit
  → Influence.GetCredit
    → InfluenceKind.GetCredit
      → source.GetStratagemSuccessChanceCredit(troop, ...)
        → this.CurrentStratagem.Friendly  // ❌ NullReferenceException
```

### 修复后

```
AI 评估计略 → Stratagem.GetCredit(source, destination)
  → Influence.GetCredit(source, destination, this)  // ✅ 传递 stratagem
    → InfluenceKind.GetCredit(source, destination, stratagem)
      → source.GetStratagemSuccessChanceCredit(stratagem, troop, ...)
        → stratagem.Friendly  // ✅ 使用参数，不依赖实例字段
```

### 兼容性

**CombatMethod 调用链：** 仍然正常工作，因为 `stratagem` 参数是可选的：

```
CombatMethod.GetCredit(source, destination)
  → Influence.GetCredit(source, destination)  // ✅ stratagem = null（默认值）
    → InfluenceKind.GetCredit(source, destination, null)
      → 不调用 GetStratagemSuccessChanceCredit（战法不需要）
```

---

## 📋 修改文件清单

### 核心文件

1. `WorldOfTheThreeKingdoms/GameObjects/Troop.cs`
   - 修改 `GetStratagemSuccessChanceCredit` 方法签名（第 10010 行）

2. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKind.cs`
   - 修改 `GetCredit` 基类方法签名
   - 添加 `using GameObjects.TroopDetail;`

3. `WorldOfTheThreeKingdoms/GameObjects/Influences/Influence.cs`
   - 修改 `GetCredit` 包装方法
   - 添加 `using GameObjects.TroopDetail;`

4. `WorldOfTheThreeKingdoms/GameObjects/TroopDetail/Stratagem.cs`
   - 修改 `GetCredit` 方法，传递 `this` 给 influence

### InfluenceKind 子类（8 个文件）

5. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind390.cs`
6. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind391.cs`
7. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind394.cs`
8. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind395.cs`
9. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind397.cs`
10. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind399.cs`
11. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind720.cs`
12. `WorldOfTheThreeKingdoms/GameObjects/Influences/InfluenceKindPack/InfluenceKind721.cs`

**每个文件的修改：**
- 修改 `GetCredit` 方法签名，添加 `Stratagem stratagem` 参数
- 传递 `stratagem` 给 `GetStratagemSuccessChanceCredit`
- 添加 `using GameObjects.TroopDetail;`

---

## 🔍 代码审查

### ✅ 符合 ANTI-BAND-AID 协议

1. **没有添加防御性空检查：** 不使用 `if (obj != null)` 或 `?.` 掩盖问题
2. **追溯数据源：** 分析了为什么 `CurrentStratagem` 为 null（AI 评估阶段）
3. **修复设计问题：** 重构方法签名，显式传递参数，不依赖实例状态

### ✅ 性能检查

- **Cold Path 代码：** AI 评估阶段，不在 Update/Draw 循环内
- **无 LINQ：** 使用 `foreach` 遍历集合
- **无额外分配：** 只是传递引用参数

### ✅ 语法检查

- **C# 12 语法：** 使用可选参数 `Stratagem stratagem = null`
- **命名规范：** 参数名 `stratagem` 符合驼峰命名
- **注释：** 添加了修复日期和原因注释

---

## 🎓 教训总结

### 1. 不要依赖不稳定的实例状态

**错误模式：**
```csharp
public int Calculate()
{
    return this.CurrentObject.Value;  // ❌ CurrentObject 可能为 null
}
```

**正确模式：**
```csharp
public int Calculate(MyObject obj)
{
    return obj.Value;  // ✅ 显式传递参数
}
```

### 2. 分析调用时序

**问题：** 方法在不同阶段被调用，实例状态可能不一致

**解决：** 
- 分析完整的调用链
- 确认每个调用点的状态
- 使用参数传递而不是实例字段

### 3. 职责分离

**原则：** 方法应该只依赖参数，不依赖外部状态

**好处：**
- 更容易测试
- 更容易理解
- 更少的副作用
- 更少的空引用异常

### 4. 可选参数处理兼容性

**场景：** 同一个方法被不同的调用者使用，有些需要参数，有些不需要

**解决：** 使用可选参数 `Stratagem stratagem = null`

**注意：** 子类需要检查参数是否为 null（如果需要使用）

---

## 📚 相关文档

- [NullReferenceException调试规范.md](.codex/NullReferenceException调试规范.md)
- [ANTI-BAND-AID 协议](.agent/rules/global-rules.md)
- [GetAreaCastTroops空引用修复_2026-03-23.md](GetAreaCastTroops空引用修复_2026-03-23.md)

---

**最后更新：** 2026-03-23  
**维护者：** Lead Architect  
**修复次数：** 1 次（正确分析，一次成功）
