# 防守军团 Mission 为 None 问题 - 根本原因

**日期：** 2026-03-22  
**状态：** 已确认根本原因  
**严重性：** 高

---

## 🚨 问题描述

防守军团 `AI_守_阳翟` 的 `Mission` 是 `None`，导致部队判断目标是敌方城市，触发撤退逻辑，军团变成空军团被解散。

---

## 📊 完整日志分析

### 关键日志序列

```
[Legion.TroopAI] AI军团AI_守_阳翟(Mission:None)，目标:洛阳(归属:汉)，军团归属:张角，是否敌方:True
↓
[UpdateLegionMandate] 张曼成队 ID:5 Legion:AI_守_阳翟 RealDest:{X:-1 Y:-1} AIState:Idle
↓
[WillArch变化] 张曼成队(ID:5) WillArch: null -> 洛阳(ID:0)
↓
[Legion.RemoveTroop] 军团 AI_守_阳翟(AI, None) 移除部队 张曼成队(ID:5)，剩余部队数: 0
↓
[Legion.Mission] 军团任务变更：None→Retreat
↓
[Faction.CreateLegion] 势力张角 创建军团: AI_撤_阳翟 (Kind:AI, Mission:Retreat, Target:阳翟)
↓
[Troop.Enter] 张曼成队 从撤退军团进城
```

---

## 🔍 根本原因

### 问题 1：防守军团的 `Mission` 是 `None`

**证据**：
1. 军团名称：`AI_守_阳翟`（"守" 表示防守任务）
2. 实际 `Mission`：`None`
3. 军团目标：`洛阳`（敌方城市）

**分析**：
- 军团名称是根据 `Mission` 生成的：
  ```csharp
  string missionName = mission switch
  {
      LegionMission.Defend => "守",
      _ => ""
  };
  ```
- 名称中有 "守"，说明军团**创建时** `Mission` 是 `Defend`
- 但日志显示 `Mission` 是 `None`，说明**创建后被修改了**

**可能原因**：
1. 读档时 `Mission` 字段反序列化失败，使用默认值 `None`
2. 某个逻辑错误地将 `Mission` 设置为 `None`
3. 旧存档数据不兼容（旧版本没有 `Mission` 字段）

### 问题 2：`Mission=None` 的军团目标是敌方城市

**证据**：
```
[Legion.TroopAI] 目标:洛阳(归属:汉)，军团归属:张角，是否敌方:True
```

**分析**：
- 防守军团的目标应该是己方城市（阳翟）
- 但实际目标是敌方城市（洛阳）
- 这导致部队判断"目标是敌方"，触发撤退逻辑

**后果**：
1. 部队从防守军团移除
2. 防守军团变成空军团（部队数：0）
3. 空军团被 `CleanupCompletedLegions()` 解散

### 问题 3：部队撤退后立即入城

**证据**：
```
[TransferToRetreatLegion] 张曼成队 转入撤退军团，目标=阳翟, RealDest={X:167 Y:139}
[Troop.Enter] 张曼成队 从撤退军团进城
```

**分析**：
- 部队已经在阳翟城市范围内
- 转入撤退军团后，立即执行移动
- 发现已经在目标城市，直接入城
- 撤退军团变成空军团，被解散

---

## 🎯 修复方案

### 修复 1：防止 `Mission` 被错误设置为 `None`

**目标**：确保防守军团的 `Mission` 始终是 `Defend`

**方法 1：在 `Legion.Mission` setter 中添加验证**

```csharp
public LegionMission Mission
{
    get => this.mission;
    set
    {
        // 🔥 ANTI-BAND-AID：防守军团的 Mission 不能被设置为 None
        // 日期：2026-03-22
        // 原因：Mission=None 的防守军团会导致部队判断目标是敌方，触发撤退
        // 解决：Fail Fast，抛出异常暴露问题
        if (this.Name != null && this.Name.Contains("守") && value == LegionMission.None)
        {
            throw new InvalidOperationException(
                $"[Legion.Mission] ❌ 数据错误：防守军团 {this.Name} 的 Mission 不能设置为 None");
        }
        
        if (this.mission != value)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Legion.Mission] 军团{this.Name}任务变更：{this.mission}→{value}");
        }
        this.mission = value;
    }
}
```

**方法 2：在读档后验证并修复 `Mission`**

```csharp
// 在 LoadDataPhase.cs 的 AfterLoadSaveFile() 中添加
foreach (Legion legion in scenario.Legions)
{
    // 🔥 修复：根据军团名称修复 Mission
    // 日期：2026-03-22
    // 原因：旧存档可能没有 Mission 字段，反序列化后为 None
    if (legion.Mission == LegionMission.None && legion.Name != null)
    {
        if (legion.Name.Contains("攻"))
        {
            legion.Mission = LegionMission.Attack;
            System.Diagnostics.Debug.WriteLine(
                $"[AfterLoadSaveFile] 修复军团 {legion.Name} 的 Mission: None -> Attack");
        }
        else if (legion.Name.Contains("守"))
        {
            legion.Mission = LegionMission.Defend;
            System.Diagnostics.Debug.WriteLine(
                $"[AfterLoadSaveFile] 修复军团 {legion.Name} 的 Mission: None -> Defend");
        }
        else if (legion.Name.Contains("撤"))
        {
            legion.Mission = LegionMission.Retreat;
            System.Diagnostics.Debug.WriteLine(
                $"[AfterLoadSaveFile] 修复军团 {legion.Name} 的 Mission: None -> Retreat");
        }
    }
}
```

### 修复 2：防止防守军团的目标是敌方城市

**目标**：确保防守军团的目标是己方城市

**方法：在 `Legion.TroopAI()` 中添加验证**

```csharp
// 在 Legion.TroopAI() 开始时添加
if (this.Mission == LegionMission.Defend && this.WillArchitecture?.BelongedFaction != this.BelongedFaction)
{
    // 🔥 ANTI-BAND-AID：防守军团的目标必须是己方城市
    // 日期：2026-03-22
    // 原因：目标是敌方城市会导致部队触发撤退逻辑
    // 解决：Fail Fast，抛出异常暴露问题
    throw new InvalidOperationException(
        $"[Legion.TroopAI] ❌ 数据错误：防守军团 {this.Name} 的目标 {this.WillArchitecture.Name} " +
        $"不属于势力 {this.BelongedFaction.Name}，归属势力：{this.WillArchitecture.BelongedFaction?.Name ?? "null"}");
}
```

### 修复 3：防止撤退军团立即解散

**目标**：撤退军团的部队入城后，军团不应该立即解散

**方法：修改 `Troop.Enter()` 逻辑**

```csharp
// 在 Troop.Enter() 中
if (this.BelongedLegion != null && this.BelongedLegion.Mission == LegionMission.Retreat)
{
    // 🔥 修复：部队入城后，不立即从军团移除
    // 日期：2026-03-22
    // 原因：立即移除会导致军团变成空军团，被 CleanupCompletedLegions() 解散
    // 解决：延迟到下一回合再移除
    
    // 不调用 this.BelongedLegion.RemoveTroop(this)
    // 让军团的 IsComplete 判定来处理
}
```

---

## 📝 推荐修复顺序

1. **修复 1（方法 2）**：在读档后根据军团名称修复 `Mission`
   - 优先级：最高
   - 原因：立即解决旧存档的兼容性问题

2. **修复 2**：在 `Legion.TroopAI()` 中验证防守军团的目标
   - 优先级：高
   - 原因：防止数据错误导致的逻辑混乱

3. **修复 1（方法 1）**：在 `Legion.Mission` setter 中添加验证
   - 优先级：中
   - 原因：防止未来再次出现类似问题

4. **修复 3**：修改 `Troop.Enter()` 逻辑
   - 优先级：低
   - 原因：前面的修复已经解决了根本问题

---

## 🧪 测试计划

1. 读取旧存档，确认 `Mission` 被正确修复
2. 观察防守军团是否正常工作
3. 确认部队不会错误地触发撤退
4. 确认军团不会被错误解散

---

**维护者：** Lead Architect  
**最后更新：** 2026-03-22
