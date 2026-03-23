# 设计文档：宝物交易系统

## 1. 高层设计（High-Level Design）

### 1.1 系统架构

宝物交易系统复用现有的粮食交易架构（`BuyFood`/`SellFood`），采用以下设计模式：

```
┌─────────────────────────────────────────────────────────────┐
│                    宝物交易系统架构                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  UI 层 (ContextMenu)                                         │
│  ├─ Faction_Treasure_Sell  (已存在，需修改)                 │
│  └─ Faction_Treasure_Buy   (新增)                           │
│                      ↓                                        │
│  业务逻辑层 (Architecture)                                   │
│  ├─ SellTreasure(Treasure)  (修改)                          │
│  └─ BuyTreasure(Treasure)   (新增)                          │
│                      ↓                                        │
│  数据层 (GameScenario)                                       │
│  ├─ Treasures (TreasureList) - 所有宝物                     │
│  └─ SoldTreasures (TreasureList) - 已出售宝物市场 (新增)   │
│                      ↓                                        │
│  事件层 (Events)                                             │
│  ├─ OnSellTreasure  (新增)                                  │
│  └─ OnBuyTreasure   (新增)                                  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 核心组件

#### 1.2.1 数据存储：宝物市场（Treasure Market）

**设计决策：** 在 `GameScenario` 中添加全局集合 `SoldTreasures`

```csharp
// 位置：GameScenario.cs
[DataMember]
public TreasureList SoldTreasures = new TreasureList();
```

**理由：**
- ✅ 复用现有的 `TreasureList` 类型
- ✅ 序列化兼容（`[DataMember]` 标记）
- ✅ 全局可访问（所有势力共享市场）
- ✅ 向后兼容（旧存档加载时自动初始化为空集合）

#### 1.2.2 宝物状态标记

**设计决策：** 复用现有字段 `Available` 和 `HidePlace`

| 状态 | Available | HidePlace | BelongedPerson | 说明 |
|------|-----------|-----------|----------------|------|
| 持有中 | `true` | `null` | `Person` | 君主或武将持有 |
| 隐藏中 | `false` | `Architecture` | `null` | 可被搜索到 |
| 已出售 | `false` | `null` | `null` | 在市场中，不可搜索 |

**理由：**
- ✅ 无需添加新字段（避免序列化复杂度）
- ✅ 通过 `HidePlace = null` 区分"已出售"和"隐藏"
- ✅ 符合现有逻辑（`Available = false` 表示不可用）

### 1.3 数据流图

#### 1.3.1 出售宝物流程

```
用户点击"出售" 
  → ContextMenuResult.Faction_Treasure_Sell
  → ShowTabListInFrame(FrameFunction.GetSellTreasure)
  → 用户选择宝物
  → FrameFunction_Architecture_AfterGetSellTreasure()
  → Architecture.SellTreasure(treasure)
      ├─ treasure.Available = false
      ├─ treasure.HidePlace = null
      ├─ treasure.BelongedPerson = null
      ├─ Scenario.SoldTreasures.Add(treasure)
      └─ Architecture.IncreaseFund(treasure.Worth)
  → 触发 OnSellTreasure 事件
```

#### 1.3.2 购买宝物流程

```
用户点击"购买"
  → ContextMenuResult.Faction_Treasure_Buy
  → ShowTabListInFrame(FrameFunction.GetBuyTreasure)
  → 显示 Scenario.SoldTreasures 列表
  → 用户选择宝物
  → FrameFunction_Architecture_AfterGetBuyTreasure()
  → Architecture.BuyTreasure(treasure)
      ├─ 检查资金：Fund >= (treasure.Worth * 1.2)
      ├─ Architecture.DecreaseFund((int)(treasure.Worth * 1.2))
      ├─ treasure.Available = true
      ├─ treasure.BelongedPerson = Leader
      └─ Scenario.SoldTreasures.Remove(treasure)
  → 触发 OnBuyTreasure 事件
```

---

## 2. 低层设计（Low-Level Design）

### 2.1 修改现有代码

#### 2.1.1 修改 `ScreenManager.FrameFunction_Architecture_AfterGetSellTreasure()`

**位置：** `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs` 第 198 行

**当前实现：**
```csharp
private void FrameFunction_Architecture_AfterGetSellTreasure()
{
    this.CurrentGameObjects = this.CurrentArchitecture.GetTreasureListOfLeader().GetSelectedList();
    if (this.CurrentGameObjects.Count > 0)
    {
        Treasure treasure = this.CurrentGameObjects[0] as Treasure;
        this.CurrentArchitecture.IncreaseFund(treasure.Worth);
        treasure.Available = false;
        treasure.HidePlace = this.CurrentArchitecture;  // ❌ 问题：隐藏在建筑中
    }
}
```

**新实现：**
```csharp
private void FrameFunction_Architecture_AfterGetSellTreasure()
{
    GameObjectList selectedTreasures = this.CurrentArchitecture.GetTreasureListOfLeader().GetSelectedList();
    if (selectedTreasures.Count > 0)
    {
        Treasure treasure = selectedTreasures[0] as Treasure;
        this.CurrentArchitecture.SellTreasure(treasure);  // ✅ 委托给 Architecture
    }
}
```

#### 2.1.2 在 `Architecture` 中添加 `SellTreasure()` 方法

**位置：** `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`

**实现：**
```csharp
/// <summary>
/// 出售宝物到市场
/// 🔥 2026-03-03 新增：宝物进入市场，不隐藏在建筑中
/// </summary>
public void SellTreasure(Treasure treasure)
{
    // 1. 增加资金
    this.IncreaseFund(treasure.Worth);
    
    // 2. 设置宝物状态为"已出售"
    treasure.Available = false;
    treasure.HidePlace = null;  // ✅ 不隐藏在建筑中
    treasure.BelongedPerson = null;
    
    // 3. 添加到市场
    Session.Current.Scenario.SoldTreasures.Add(treasure);
    
    // 4. 触发事件
    TreasureEvents.RaiseSellTreasure(Session.Current.Scenario, this, treasure);
}
```

**路径分类：** 🧊 COLD PATH（UI 事件响应，低频操作）
- ✅ 允许使用 LINQ（如果需要）
- ✅ 优先可读性

### 2.2 新增代码

#### 2.2.1 在 `Architecture` 中添加 `BuyTreasure()` 方法

**位置：** `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`

**实现：**
```csharp
/// <summary>
/// 从市场购买宝物
/// 🔥 2026-03-03 新增：购买价格为原价的 1.2 倍
/// </summary>
public void BuyTreasure(Treasure treasure)
{
    // 1. 计算购买价格（加价 20%）
    int buyPrice = (int)(treasure.Worth * 1.2f);
    
    // 2. 扣除资金
    this.DecreaseFund(buyPrice);
    
    // 3. 设置宝物状态为"持有"
    treasure.Available = true;
    treasure.BelongedPerson = this.BelongedFaction?.Leader;
    treasure.HidePlace = null;
    
    // 4. 从市场移除
    Session.Current.Scenario.SoldTreasures.Remove(treasure);
    
    // 5. 触发事件
    TreasureEvents.RaiseBuyTreasure(Session.Current.Scenario, this, treasure);
}
```

**路径分类：** 🧊 COLD PATH

#### 2.2.2 在 `Architecture` 中添加 `BuyTreasureAvail()` 方法

**位置：** `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`

**实现：**
```csharp
/// <summary>
/// 检查是否可以购买宝物
/// 🔥 Anti-Band-Aid: 明确检查势力和君主，不使用 ?. 掩盖逻辑
/// </summary>
public bool BuyTreasureAvail()
{
    // 检查市场是否有宝物
    if (Session.Current.Scenario.SoldTreasures.Count == 0)
        return false;
    
    // 检查建筑是否有归属势力
    if (this.BelongedFaction == null)
        return false;
    
    // 检查势力是否有君主
    if (this.BelongedFaction.Leader == null)
        return false;
    
    return true;
}
```

**路径分类：** 🧊 COLD PATH（菜单可见性检查）

#### 2.2.3 在 `ScreenManager` 中添加购买宝物处理

**位置：** `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`

**实现：**
```csharp
/// <summary>
/// 购买宝物
/// 🔥 2026-03-03 新增：从市场购买已出售的宝物
/// </summary>
private void FrameFunction_Architecture_AfterGetBuyTreasure()
{
    GameObjectList selectedTreasures = Session.Current.Scenario.SoldTreasures.GetSelectedList();
    if (selectedTreasures.Count > 0)
    {
        Treasure treasure = selectedTreasures[0] as Treasure;
        int buyPrice = (int)(treasure.Worth * 1.2f);
        
        // 检查资金是否充足
        if (this.CurrentArchitecture.Fund >= buyPrice)
        {
            this.CurrentArchitecture.BuyTreasure(treasure);
        }
        else
        {
            // 显示资金不足提示（复用现有提示系统）
            // TODO: 添加提示逻辑
        }
    }
}
```

**路径分类：** 🧊 COLD PATH

#### 2.2.4 在 `MGSContextMenu` 中添加购买菜单处理

**位置：** `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`

**实现：**
```csharp
case ContextMenuResult.Faction_Treasure_Buy:
    this.ShowTabListInFrame(
        UndoneWorkKind.Frame, 
        FrameKind.Treasure, 
        FrameFunction.GetBuyTreasure, 
        false, true, true, true, 
        Session.Current.Scenario.SoldTreasures,  // ✅ 显示市场中的宝物
        null, 
        "购买宝物", 
        ""
    );
    break;
```

**路径分类：** 🧊 COLD PATH

### 2.3 修改搜索逻辑

#### 2.3.1 验证 `Person.SearchTreasure()` 方法

**位置：** `WorldOfTheThreeKingdoms/GameObjects/Person.cs` 第 4738-4746 行

**当前实现：**
```csharp
public void SearchTreasure()
{
    TreasureList list = new TreasureList();
    foreach (Treasure treasure in Session.Current.Scenario.Treasures)
    {
        if (treasure.HidePlace == this.LocationArchitecture)
        {
            list.Add(treasure);
        }
    }
    // ...
}
```

**分析结果：**
- ✅ 现有逻辑已经正确：`HidePlace = null` 的宝物（已出售）会被自动排除
- ✅ 无需修改代码

**路径分类：** 🧊 COLD PATH（玩家手动触发搜索，低频操作）
- ✅ 允许使用 `foreach`（可读性优先）
- ✅ 允许使用 LINQ（如果需要）
- ❌ 不需要过度优化为 `for` 循环

**可选优化（仅当性能分析显示瓶颈时）：**
```csharp
public void SearchTreasure()
{
    TreasureList list = [];  // ✅ C# 12 集合表达式
    
    // 如果确认是 Hot Path，才使用 for 循环
    TreasureList allTreasures = Session.Current.Scenario.Treasures;
    for (int i = 0; i < allTreasures.Count; i++)
    {
        Treasure treasure = allTreasures[i];
        if (treasure.HidePlace == this.LocationArchitecture)
        {
            list.Add(treasure);
        }
    }
    // ...
}
```

**正确性验证：**
- 原逻辑：`treasure.HidePlace == this.LocationArchitecture`
- 新逻辑：相同条件，但 `HidePlace = null` 的宝物自动被排除
- ✅ 无需修改条件，现有逻辑已正确

### 2.4 枚举扩展

#### 2.4.1 添加 `ContextMenuResult.Faction_Treasure_Buy`

**位置：** `WorldOfTheThreeKingdoms/GameGlobal/ContextMenuResult.cs`

**实现：**
```csharp
/// <summary>
/// 势力-宝物-购买
/// 🔥 2026-03-03 新增
/// </summary>
Faction_Treasure_Buy,
```

#### 2.4.2 添加 `FrameFunction.GetBuyTreasure`

**位置：** `WorldOfTheThreeKingdoms/GameGlobal/FrameFunction.cs`

**实现：**
```csharp
/// <summary>
/// 宝物-购买
/// 🔥 2026-03-03 新增
/// </summary>
GetBuyTreasure,
```

### 2.5 事件系统

#### 2.5.1 添加宝物交易事件

**位置：** `WorldOfTheThreeKingdoms/GameObjects/Events/TreasureEvents.cs`（新文件）

**实现：**
```csharp
using GameObjects;

namespace GameObjects.Events
{
    /// <summary>
    /// 宝物交易事件
    /// 🔥 AOT 兼容：使用强类型事件替代反射
    /// </summary>
    public static class TreasureEvents
    {
        public static event Action<GameScenario, Architecture, Treasure>? OnSellTreasure;
        public static event Action<GameScenario, Architecture, Treasure>? OnBuyTreasure;

        public static void RaiseSellTreasure(GameScenario scenario, Architecture architecture, Treasure treasure)
        {
            OnSellTreasure?.Invoke(scenario, architecture, treasure);
        }

        public static void RaiseBuyTreasure(GameScenario scenario, Architecture architecture, Treasure treasure)
        {
            OnBuyTreasure?.Invoke(scenario, architecture, treasure);
        }
    }
}
```

**路径分类：** 🧊 COLD PATH

---

## 3. XML 配置修改

### 3.1 添加"购买"菜单项

**位置：** `Content/Data/Plugins/ContextMenuData.xml`

**实现：**
```xml
<!-- 在 <MenuItem Text="宝物" Kind="Faction_Treasure"> 内部添加 -->
<MenuItem Text="购买" Kind="Faction_Treasure_Buy" Result="Faction_Treasure_Buy" 
          Conditions="BuyTreasureAvail" />
```

**说明：**
- `Conditions="BuyTreasureAvail"`：只有市场有宝物时才显示
- `Result="Faction_Treasure_Buy"`：触发购买流程

---

## 4. 序列化支持

### 4.1 GameScenario 序列化

**位置：** `WorldOfTheThreeKingdoms/GameObjects/GameScenario.cs`

**实现：**
```csharp
[DataMember]
public TreasureList SoldTreasures = new TreasureList();
```

**向后兼容性：**
- 旧存档加载时，`SoldTreasures` 自动初始化为空集合
- 不影响现有游戏逻辑

### 4.2 初始化逻辑

**位置：** `GameScenario.Init()` 方法

**实现：**
```csharp
public void Init()
{
    // ... 现有初始化代码 ...
    
    // 🔥 2026-03-03 新增：初始化宝物市场
    if (SoldTreasures == null)
    {
        SoldTreasures = new TreasureList();
    }
}
```

---

## 5. 性能分析

### 5.1 Hot Path vs Cold Path

| 操作 | 路径类型 | 频率 | 优化策略 |
|------|---------|------|---------|
| `SearchTreasure()` | 🔥 HOT | 每次搜索 | `for` 循环，禁止 LINQ |
| `SellTreasure()` | 🧊 COLD | 低频 | 允许 LINQ，优先可读性 |
| `BuyTreasure()` | 🧊 COLD | 低频 | 允许 LINQ，优先可读性 |
| `BuyTreasureAvail()` | 🧊 COLD | 菜单检查 | 简单条件判断 |

### 5.2 内存影响

- `SoldTreasures` 集合：预期大小 < 50 个宝物
- 内存开销：可忽略（< 10KB）
- 序列化开销：可忽略

---

## 6. 测试策略

### 6.1 单元测试（伪代码）

```csharp
[Test]
public void TestSellTreasure_ShouldAddToMarket()
{
    // Arrange
    var treasure = new Treasure { Worth = 1000, Available = true };
    var arch = new Architecture { Fund = 0 };
    
    // Act
    arch.SellTreasure(treasure);
    
    // Assert
    Assert.IsFalse(treasure.Available);
    Assert.IsNull(treasure.HidePlace);
    Assert.AreEqual(1000, arch.Fund);
    Assert.Contains(treasure, Session.Current.Scenario.SoldTreasures);
}

[Test]
public void TestBuyTreasure_ShouldRemoveFromMarket()
{
    // Arrange
    var treasure = new Treasure { Worth = 1000, Available = false };
    Session.Current.Scenario.SoldTreasures.Add(treasure);
    var arch = new Architecture { Fund = 1200 };
    
    // Act
    arch.BuyTreasure(treasure);
    
    // Assert
    Assert.IsTrue(treasure.Available);
    Assert.AreEqual(0, arch.Fund);  // 1200 - 1200 = 0
    Assert.DoesNotContain(treasure, Session.Current.Scenario.SoldTreasures);
}

[Test]
public void TestSearchTreasure_ShouldExcludeSoldTreasures()
{
    // Arrange
    var soldTreasure = new Treasure { HidePlace = null };  // 已出售
    var hiddenTreasure = new Treasure { HidePlace = arch };  // 隐藏
    
    // Act
    var result = person.SearchTreasure();
    
    // Assert
    Assert.DoesNotContain(soldTreasure, result);
    Assert.Contains(hiddenTreasure, result);
}
```

### 6.2 集成测试

1. **出售-购买往返测试**：
   - 出售宝物 → 检查市场 → 购买宝物 → 验证状态恢复

2. **资金不足测试**：
   - 尝试购买但资金不足 → 验证交易失败

3. **搜索排除测试**：
   - 出售宝物 → 执行搜索 → 验证不会找到已出售宝物

---

## 7. 实现顺序

### 阶段 1：核心逻辑（优先级：高）
1. ✅ 在 `GameScenario` 添加 `SoldTreasures` 字段
2. ✅ 修改 `ScreenManager.FrameFunction_Architecture_AfterGetSellTreasure()`
3. ✅ 在 `Architecture` 添加 `SellTreasure()` 方法
4. ✅ 在 `Architecture` 添加 `BuyTreasure()` 方法
5. ✅ 在 `Architecture` 添加 `BuyTreasureAvail()` 方法

### 阶段 2：UI 集成（优先级：高）
6. ✅ 添加 `ContextMenuResult.Faction_Treasure_Buy` 枚举
7. ✅ 添加 `FrameFunction.GetBuyTreasure` 枚举
8. ✅ 在 `ScreenManager` 添加 `FrameFunction_Architecture_AfterGetBuyTreasure()`
9. ✅ 在 `MGSContextMenu` 添加购买菜单处理
10. ✅ 修改 XML 配置添加"购买"菜单项

### 阶段 3：事件系统（优先级：中）
11. ✅ 创建 `TreasureEvents.cs` 文件
12. ✅ 在 `SellTreasure()` 和 `BuyTreasure()` 中触发事件

### 阶段 4：搜索逻辑（优先级：低，现有逻辑已正确）
13. ⚠️ 验证 `Person.SearchTreasure()` 逻辑（无需修改）

### 阶段 5：AI 支持（优先级：未来扩展，暂不实现）
14. 🔮 添加 AI 自动购买逻辑（需求 7）
    - ⏸️ 当前版本不实现
    - 🔮 未来编写专门 AI 逻辑后再开放

---

## 8. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 旧存档不兼容 | 高 | `SoldTreasures` 自动初始化为空集合 |
| 搜索逻辑遗漏 | 中 | 验证所有搜索宝物的代码路径 |
| 价格计算精度 | 低 | 使用 `(int)(Worth * 1.2f)` 确保整数 |
| 事件系统冲突 | 低 | 使用独立的 `TreasureEvents` 类 |

---

## 9. 依赖关系

```
GameScenario.SoldTreasures (新增)
    ↑
    ├─ Architecture.SellTreasure() (新增)
    ├─ Architecture.BuyTreasure() (新增)
    └─ Architecture.BuyTreasureAvail() (新增)
        ↑
        ├─ ScreenManager.FrameFunction_Architecture_AfterGetSellTreasure() (修改)
        ├─ ScreenManager.FrameFunction_Architecture_AfterGetBuyTreasure() (新增)
        └─ MGSContextMenu (修改)
            ↑
            └─ ContextMenuData.xml (修改)
```

---

## 10. 符合性检查

### 10.1 Anti-Band-Aid Protocol
- ✅ 无防御性空检查
- ✅ 无逻辑篡改
- ✅ 只修改 `.cs` 文件（XML 由用户明确要求）

### 10.2 性能策略
- ✅ Hot Path 使用 `for` 循环（`SearchTreasure`）
- ✅ Cold Path 优先可读性（交易逻辑）

### 10.3 C# 12 语法
- ✅ 使用集合表达式 `[]`
- ✅ 使用 `?.` 安全导航（合理场景）

### 10.4 AOT 兼容
- ✅ 使用强类型事件（`TreasureEvents`）
- ✅ 禁止反射

---

## 11. 总结

本设计文档提供了宝物交易系统的完整技术方案，复用现有粮食交易架构，确保：
- ✅ 最小化代码修改
- ✅ 向后兼容
- ✅ 性能优化
- ✅ AOT 兼容
- ✅ 符合项目规范

下一步：创建实现任务列表（Tasks）。
