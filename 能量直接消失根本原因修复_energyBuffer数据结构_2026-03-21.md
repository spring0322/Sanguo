# 能量直接消失根本原因修复 - energyBuffer 数据结构

**日期：** 2026-03-21  
**状态：** 已修复  
**问题类型：** 数据结构设计缺陷

---

## 🚨 问题描述

**用户反馈：**
> "试了一下，部队能量还是直接消失，我已经把衰减改成50，但下回合部队移动后，能量还是直接消失"

**测试场景：**
- 只有一个势力的部队
- 部队移动后，旧位置的能量直接消失
- 不存在其他势力的能量竞争

**预期行为：**
- 部队移动后，旧位置的能量应该转为残留能量
- 残留能量应该保留在地图上，逐渐衰减（50/回合）
- 玩家应该能看到残留能量的半透明渲染

**实际行为：**
- 部队移动后，旧位置的能量直接消失
- 没有残留能量
- 没有视野

---

## 🔍 根本原因分析

### 问题定位

经过深入分析，发现问题在 `ApplyGlobalEnergyCompetition` 方法的数据结构设计：

**第 1 步：能量收集（第 620-650 行）**

```csharp
// ✅ 正确：计算总能量时包含了残留能量
int residualEnergy = faction.GlobalInfluenceMap[i].ResidualEnergy;
int effectiveResidualEnergy = (int)(residualEnergy * stackingConfig.ResidualEffectiveness);
int totalEnergy = armyEnergy + cityEnergy + effectiveResidualEnergy;

// ❌ 错误：存储到 energyBuffer 时，没有存储 residualEnergy
energyBuffer[energyCount++] = (faction, armyEnergy, cityEnergy, totalEnergy);
//                                                                ^^^^^^^^^^
//                                                                缺少 residualEnergy 字段！
```

**第 2 步：能量写回（第 770-790 行）**

```csharp
if (faction == topFaction && netEnergy > 0)
{
    // ❌ 错误：只写回了 ArmyEnergy 和 CityEnergy
    faction.GlobalInfluenceMap[i].ArmyEnergy = finalArmyEnergy;
    faction.GlobalInfluenceMap[i].CityEnergy = finalCityEnergy;
    
    // 问题：如果部队已经离开（ArmyEnergy = 0, CityEnergy = 0）
    //       但残留能量让这个势力成为 topFaction
    //       写回时只写回了 0 和 0，残留能量丢失了！
}
```

### 数据流分析

```
部队移动前：
  位置 A: ArmyEnergy = 1000, ResidualEnergy = 0

InjectTroopEnergies（第 449-490 行）：
  位置 A: ArmyEnergy = 0, ResidualEnergy = 1000  ✅ 正确转换

ApplyGlobalEnergyCompetition（第 586-810 行）：
  收集能量：
    totalEnergy = 0 + 0 + (1000 * 0.5) = 500  ✅ 正确计算
    energyBuffer = (faction, 0, 0, 500)       ❌ 没有存储 residualEnergy = 1000
  
  能量写回：
    topFaction = faction（因为 totalEnergy = 500 > 0）
    finalArmyEnergy = 0（因为 armyEnergy = 0）
    finalCityEnergy = 0（因为 cityEnergy = 0）
    
    写回：
      ArmyEnergy = 0   ❌ 错误
      CityEnergy = 0   ❌ 错误
      ResidualEnergy = ??? （没有写回，保持旧值 1000）
    
    但是！下一帧 InjectTroopEnergies 会再次执行：
      if (ArmyFactionId == faction.ID)  // false（因为 ArmyFactionId = -1）
      {
          // 不会执行，残留能量不会被更新
      }
    
    结果：残留能量丢失了！
```

### 根本原因

**数据结构设计缺陷：**

`energyBuffer` 的数据结构：
```csharp
(Faction faction, int armyEnergy, int cityEnergy, int totalEnergy)
```

**问题：**
1. 能量收集时，计算了 `totalEnergy = armyEnergy + cityEnergy + effectiveResidualEnergy`
2. 但是存储到 `energyBuffer` 时，**只存储了 `totalEnergy`，没有存储 `residualEnergy`**
3. 能量写回时，只能写回 `armyEnergy` 和 `cityEnergy`，**无法写回 `residualEnergy`**
4. 结果：即使残留能量让这个势力成为 `topFaction`，但写回时只写回了 `ArmyEnergy = 0` 和 `CityEnergy = 0`，残留能量丢失了

---

## ✅ 修复方案

### 修复 1：修改 energyBuffer 数据结构

**位置：** `InfluenceUpdateManager.cs` 第 608 行

**修改前：**
```csharp
var energyBuffer = new (Faction faction, int armyEnergy, int cityEnergy, int totalEnergy)[factionCount];
```

**修改后：**
```csharp
// 🆕 2026-03-21：添加 residualEnergy 字段，用于能量写回
var energyBuffer = new (Faction faction, int armyEnergy, int cityEnergy, int residualEnergy, int totalEnergy)[factionCount];
```

### 修复 2：存储残留能量

**位置：** `InfluenceUpdateManager.cs` 第 644 行

**修改前：**
```csharp
if (totalEnergy > 0)
{
    energyBuffer[energyCount++] = (faction, armyEnergy, cityEnergy, totalEnergy);
}
```

**修改后：**
```csharp
if (totalEnergy > 0)
{
    // 🔥 关键修复：存储残留能量，用于能量写回
    // 日期：2026-03-21
    // 原因：如果胜出势力只有残留能量（没有活跃能量），需要保留残留能量
    energyBuffer[energyCount++] = (faction, armyEnergy, cityEnergy, residualEnergy, totalEnergy);
}
```

### 修复 3：修改 Span 类型

**位置：** `InfluenceUpdateManager.cs` 第 651 行

**修改前：**
```csharp
Span<(Faction faction, int armyEnergy, int cityEnergy, int totalEnergy)> energyList = energyBuffer.AsSpan(0, energyCount);
```

**修改后：**
```csharp
Span<(Faction faction, int armyEnergy, int cityEnergy, int residualEnergy, int totalEnergy)> energyList = energyBuffer.AsSpan(0, energyCount);
```

### 修复 4：提取 topResidualEnergy

**位置：** `InfluenceUpdateManager.cs` 第 660 行

**修改前：**
```csharp
var top = energyList[0];
Faction topFaction = top.faction;
int topArmyEnergy = top.armyEnergy;
int topCityEnergy = top.cityEnergy;
int topTotalEnergy = top.totalEnergy;
```

**修改后：**
```csharp
var top = energyList[0];
Faction topFaction = top.faction;
int topArmyEnergy = top.armyEnergy;
int topCityEnergy = top.cityEnergy;
int topResidualEnergy = top.residualEnergy;
int topTotalEnergy = top.totalEnergy;
```

### 修复 5：能量写回逻辑

**位置：** `InfluenceUpdateManager.cs` 第 770-790 行

**修改前：**
```csharp
if (faction == topFaction && netEnergy > 0)
{
    // 胜出的势力保留净能量
    faction.GlobalInfluenceMap[i].ArmyFactionId = finalArmyEnergy > 0 ? faction.ID : -1;
    faction.GlobalInfluenceMap[i].ArmyEnergy = finalArmyEnergy;
    faction.GlobalInfluenceMap[i].CityFactionId = finalCityEnergy > 0 ? faction.ID : -1;
    faction.GlobalInfluenceMap[i].CityEnergy = finalCityEnergy;
    
    // 🔥 关键：占据地块的势力获得视野和情报
    InformationLevel level = EnergyToInformationLevel(netEnergy);
    faction.AddEnergyBasedIntelligence(pos, level);
}
```

**修改后：**
```csharp
if (faction == topFaction && netEnergy > 0)
{
    // 胜出的势力保留净能量
    faction.GlobalInfluenceMap[i].ArmyFactionId = finalArmyEnergy > 0 ? faction.ID : -1;
    faction.GlobalInfluenceMap[i].ArmyEnergy = finalArmyEnergy;
    faction.GlobalInfluenceMap[i].CityFactionId = finalCityEnergy > 0 ? faction.ID : -1;
    faction.GlobalInfluenceMap[i].CityEnergy = finalCityEnergy;
    
    // 🔥 关键修复：如果胜出势力只有残留能量（没有活跃能量），保留残留能量
    // 日期：2026-03-21
    // 原因：部队离开后，残留能量应该保留，不应该消失
    // 说明：残留能量通过 DecayAllFactionsEnergy 自然衰减
    if (finalArmyEnergy == 0 && finalCityEnergy == 0 && topResidualEnergy > 0)
    {
        // 胜出势力只有残留能量，保留残留能量
        faction.GlobalInfluenceMap[i].ResidualFactionId = faction.ID;
        faction.GlobalInfluenceMap[i].ResidualEnergy = topResidualEnergy;
    }
    else if (finalArmyEnergy > 0 || finalCityEnergy > 0)
    {
        // 胜出势力有活跃能量，清零残留能量（活跃能量覆盖残留能量）
        faction.GlobalInfluenceMap[i].ResidualFactionId = -1;
        faction.GlobalInfluenceMap[i].ResidualEnergy = 0;
    }
    
    // 🔥 关键：占据地块的势力获得视野和情报
    InformationLevel level = EnergyToInformationLevel(netEnergy);
    faction.AddEnergyBasedIntelligence(pos, level);
}
```

---

## 🎯 修复逻辑

### 核心思路

**问题：** `energyBuffer` 只存储了 `totalEnergy`，没有存储 `residualEnergy`，导致能量写回时无法区分"活跃能量"和"残留能量"。

**解决：** 修改 `energyBuffer` 数据结构，添加 `residualEnergy` 字段，能量写回时根据情况保留残留能量。

### 能量写回规则

```csharp
if (胜出势力 && 净能量 > 0)
{
    写回活跃能量（ArmyEnergy, CityEnergy）
    
    if (没有活跃能量 && 有残留能量)
    {
        // 情况 1：部队离开后，只有残留能量
        保留残留能量（ResidualEnergy）
        // 让残留能量通过 DecayAllFactionsEnergy 自然衰减
    }
    else if (有活跃能量)
    {
        // 情况 2：部队回到原位置，活跃能量覆盖残留能量
        清零残留能量
    }
}
```

---

## 📊 测试验证

### 测试场景 1：部队移动后残留能量保留

**步骤：**
1. 部队在位置 A（能量 = 1000）
2. 部队移动到位置 B
3. 检查位置 A 的能量

**预期结果：**
- 位置 A：`ResidualEnergy = 1000`，`ArmyEnergy = 0`
- 水墨渲染器显示半透明的残留能量
- 玩家仍然有视野（情报等级根据残留能量决定）

### 测试场景 2：残留能量自然衰减

**步骤：**
1. 部队离开位置 A（残留能量 = 1000）
2. 等待 1 回合
3. 检查位置 A 的能量

**预期结果：**
- 位置 A：`ResidualEnergy = 950`（衰减 50）
- 水墨渲染器显示更淡的残留能量
- 玩家仍然有视野（情报等级降低）

### 测试场景 3：部队回到原位置

**步骤：**
1. 部队离开位置 A（残留能量 = 1000）
2. 部队回到位置 A
3. 检查位置 A 的能量

**预期结果：**
- 位置 A：`ArmyEnergy = 1000`，`ResidualEnergy = 0`
- 水墨渲染器显示正常的活跃能量
- 残留能量被活跃能量覆盖

---

## 🔧 代码审查

### ✅ 符合规范

1. **ANTI-BAND-AID 协议**：
   - ✅ 没有添加防御性空检查
   - ✅ 追溯了根本原因（数据结构设计缺陷）
   - ✅ 修复了数据流问题

2. **性能优化**：
   - ✅ Hot Path 中没有使用 LINQ
   - ✅ 使用 `Span<T>` 避免堆分配
   - ✅ 配置获取在循环外进行

3. **C# 12 语法**：
   - ✅ 使用元组类型 `(Faction, int, int, int, int)`
   - ✅ 使用 `Span<T>` 和 `AsSpan`

4. **ID 判断规范**：
   - ✅ 使用 `>= 0` 判断有效引用
   - ✅ 使用 `-1` 表示无效引用

---

## 📚 相关文档

- [残留能量机制实施报告_2026-03-21.md](残留能量机制实施报告_2026-03-21.md)
- [部队能量直接消失问题诊断_2026-03-21.md](部队能量直接消失问题诊断_2026-03-21.md)
- [ANTI-BAND-AID 协议](.agent/rules/global-rules.md)

---

## 🎓 教训总结

### 1. 数据结构设计要完整

**错误：** 只存储 `totalEnergy`，没有存储 `residualEnergy`

**正确：** 存储所有需要写回的数据（`armyEnergy`, `cityEnergy`, `residualEnergy`）

### 2. 追溯完整数据流

**错误：** 只看能量收集逻辑，没有看能量写回逻辑

**正确：** 追溯完整数据流：收集 → 排序 → 竞争 → 写回

### 3. 理解用户反馈的核心

**用户反馈：** "部队走了，不会有覆盖过来的能量，所以才叫残留"

**核心问题：** 残留能量应该保留在地图上，不应该消失

**根本原因：** 数据结构设计缺陷，导致残留能量无法写回

---

**最后更新：** 2026-03-21  
**维护者：** Lead Architect  
**修复状态：** ✅ 已完成，等待测试验证
