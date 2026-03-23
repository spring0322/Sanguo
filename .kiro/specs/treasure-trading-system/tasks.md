# 任务列表：宝物交易系统

## 任务状态说明
- `[ ]` 待完成
- `[x]` 已完成
- `[~]` 进行中
- `[-]` 已跳过

---

## 阶段 1：核心数据结构（优先级：高）

### Task 1.1：在 GameScenario 添加 SoldTreasures 字段
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/GameScenario.cs`
- **描述**: 添加全局宝物市场集合
- **实现**:
  ```csharp
  [DataMember]
  public TreasureList SoldTreasures = new TreasureList();
  ```
- **位置**: 在 `Treasures` 字段附近（第 280 行）
- **验收标准**:
  - ✅ 字段标记 `[DataMember]`
  - ✅ 初始化为空集合
  - ✅ 编译通过

### Task 1.2：在 GameScenario.Init() 初始化 SoldTreasures
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/GameScenario.cs`
- **描述**: 确保旧存档加载时正确初始化
- **实现**:
  ```csharp
  if (SoldTreasures == null)
  {
      SoldTreasures = new TreasureList();
  }
  ```
- **位置**: `Init()` 方法末尾
- **验收标准**:
  - ✅ 旧存档加载不崩溃
  - ✅ `SoldTreasures` 不为 null

---

## 阶段 2：出售宝物逻辑（优先级：高）

### Task 2.1：在 Architecture 添加 SellTreasure() 方法
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`
- **描述**: 实现宝物出售核心逻辑
- **实现**: 参考 `design.md` 第 2.1.2 节
- **位置**: 在 `SellFood()` 方法附近（第 16189 行）
- **验收标准**:
  - ✅ 增加资金 `IncreaseFund(treasure.Worth)`
  - ✅ 设置 `treasure.Available = false`
  - ✅ 设置 `treasure.HidePlace = null`
  - ✅ 设置 `treasure.BelongedPerson = null`
  - ✅ 添加到市场 `Scenario.SoldTreasures.Add(treasure)`
  - ✅ 触发事件 `TreasureEvents.RaiseSellTreasure(...)`

### Task 2.2：修改 ScreenManager.FrameFunction_Architecture_AfterGetSellTreasure()
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`
- **描述**: 委托出售逻辑给 Architecture.SellTreasure()
- **实现**: 参考 `design.md` 第 2.1.1 节
- **位置**: 第 198 行
- **验收标准**:
  - ✅ 调用 `CurrentArchitecture.SellTreasure(treasure)`
  - ✅ 删除旧的 `treasure.HidePlace = this.CurrentArchitecture` 逻辑
  - ✅ 编译通过

---

## 阶段 3：购买宝物逻辑（优先级：高）

### Task 3.1：在 Architecture 添加 BuyTreasure() 方法
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`
- **描述**: 实现宝物购买核心逻辑
- **实现**: 参考 `design.md` 第 2.2.1 节
- **位置**: 在 `SellTreasure()` 方法之后
- **验收标准**:
  - ✅ 计算购买价格 `(int)(treasure.Worth * 1.2f)`
  - ✅ 扣除资金 `DecreaseFund(buyPrice)`
  - ✅ 设置 `treasure.Available = true`
  - ✅ 设置 `treasure.BelongedPerson = Leader`
  - ✅ 从市场移除 `Scenario.SoldTreasures.Remove(treasure)`
  - ✅ 触发事件 `TreasureEvents.RaiseBuyTreasure(...)`

### Task 3.2：在 Architecture 添加 BuyTreasureAvail() 方法
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`
- **描述**: 检查购买菜单可见性
- **实现**: 参考 `design.md` 第 2.2.2 节
- **位置**: 在 `BuyTreasure()` 方法之后
- **验收标准**:
  - ✅ 检查 `SoldTreasures.Count > 0`
  - ✅ 明确检查 `BelongedFaction == null`（不使用 `?.`）
  - ✅ 明确检查 `BelongedFaction.Leader == null`（不使用 `?.`）
  - ✅ 返回 bool
  - ✅ 符合 Anti-Band-Aid Protocol

### Task 3.3：在 ScreenManager 添加 FrameFunction_Architecture_AfterGetBuyTreasure()
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`
- **描述**: 处理购买宝物的 UI 回调
- **实现**: 参考 `design.md` 第 2.2.3 节
- **位置**: 在 `FrameFunction_Architecture_AfterGetSellTreasure()` 之后
- **验收标准**:
  - ✅ 获取选中的宝物
  - ✅ 检查资金是否充足
  - ✅ 调用 `CurrentArchitecture.BuyTreasure(treasure)`
  - ✅ 资金不足时显示提示（可选）

---

## 阶段 4：枚举扩展（优先级：高）

### Task 4.1：添加 ContextMenuResult.Faction_Treasure_Buy
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameGlobal/ContextMenuResult.cs`
- **描述**: 添加购买宝物的菜单结果枚举
- **实现**:
  ```csharp
  /// <summary>
  /// 势力-宝物-购买
  /// 🔥 2026-03-03 新增
  /// </summary>
  Faction_Treasure_Buy,
  ```
- **位置**: 在 `Faction_Treasure_Sell` 之后（第 158 行）
- **验收标准**:
  - ✅ 枚举值唯一
  - ✅ 编译通过

### Task 4.2：添加 FrameFunction.GetBuyTreasure
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameGlobal/FrameFunction.cs`
- **描述**: 添加购买宝物的帧函数枚举
- **实现**:
  ```csharp
  /// <summary>
  /// 宝物-购买
  /// 🔥 2026-03-03 新增
  /// </summary>
  GetBuyTreasure,
  ```
- **位置**: 在 `GetSellTreasure` 之后（第 108 行）
- **验收标准**:
  - ✅ 枚举值唯一
  - ✅ 编译通过

---

## 阶段 5：UI 集成（优先级：高）

### Task 5.1：在 MGSContextMenu 添加购买菜单处理
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`
- **描述**: 处理购买菜单点击事件
- **实现**: 参考 `design.md` 第 2.2.4 节
- **位置**: 在 `case ContextMenuResult.Faction_Treasure_Sell:` 之后（第 890 行）
- **验收标准**:
  - ✅ 调用 `ShowTabListInFrame` 显示市场宝物列表
  - ✅ 传递 `Session.Current.Scenario.SoldTreasures`
  - ✅ 设置标题为"购买宝物"

### Task 5.2：在 ScreenManager 添加 switch case 处理
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`
- **描述**: 在 FrameFunction switch 中添加购买宝物分支
- **实现**:
  ```csharp
  case FrameFunction.GetBuyTreasure:
      this.FrameFunction_Architecture_AfterGetBuyTreasure();
      break;
  ```
- **位置**: 在 `case FrameFunction.GetSellTreasure:` 之后（第 2677 行）
- **验收标准**:
  - ✅ 编译通过
  - ✅ 点击购买菜单能触发回调

### Task 5.3：修改 XML 配置添加购买菜单项
- **状态**: `[ ]`
- **文件**: `Content/Data/Plugins/ContextMenuData.xml`
- **描述**: 在宝物子菜单中添加"购买"选项
- **实现**: 参考 `design.md` 第 3.1 节
- **位置**: 在 `<MenuItem Text="宝物" Kind="Faction_Treasure">` 内部
- **验收标准**:
  - ✅ 添加 `<MenuItem Text="购买" Kind="Faction_Treasure_Buy" Result="Faction_Treasure_Buy" Conditions="BuyTreasureAvail" />`
  - ✅ XML 格式正确
  - ✅ 游戏中菜单显示正确

---

## 阶段 6：事件系统（优先级：中）

### Task 6.1：创建 TreasureEvents.cs 文件
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Events/TreasureEvents.cs`（新文件）
- **描述**: 实现宝物交易事件系统
- **实现**: 参考 `design.md` 第 2.5.1 节
- **验收标准**:
  - ✅ 定义 `OnSellTreasure` 事件
  - ✅ 定义 `OnBuyTreasure` 事件
  - ✅ 实现 `RaiseSellTreasure()` 方法
  - ✅ 实现 `RaiseBuyTreasure()` 方法
  - ✅ 使用 AOT 兼容的强类型事件

### Task 6.2：在 SellTreasure() 中触发事件
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`
- **描述**: 在出售宝物后触发事件
- **依赖**: Task 6.1
- **验收标准**:
  - ✅ 调用 `TreasureEvents.RaiseSellTreasure(...)`
  - ✅ 传递正确的参数

### Task 6.3：在 BuyTreasure() 中触发事件
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`
- **描述**: 在购买宝物后触发事件
- **依赖**: Task 6.1
- **验收标准**:
  - ✅ 调用 `TreasureEvents.RaiseBuyTreasure(...)`
  - ✅ 传递正确的参数

---

## 阶段 7：搜索逻辑验证（优先级：低）

### Task 7.1：验证 Person.SearchTreasure() 逻辑
- **状态**: `[ ]`
- **文件**: `WorldOfTheThreeKingdoms/GameObjects/Person.cs`
- **描述**: 确认现有搜索逻辑正确排除已出售宝物
- **验证点**:
  - ✅ 条件 `treasure.HidePlace == this.LocationArchitecture` 自动排除 `HidePlace = null` 的宝物
  - ✅ 无需修改代码
  - ✅ 路径分类：🧊 COLD PATH（玩家手动触发，低频）
- **验收标准**:
  - ✅ 搜索不会返回已出售宝物
  - ✅ 搜索能返回隐藏宝物

### Task 7.2：（已取消）优化 SearchTreasure() 为 Hot Path
- **状态**: `[-]` 已跳过
- **原因**: `SearchTreasure()` 是 COLD PATH（玩家手动触发），不需要过度优化
- **说明**: 如果未来性能分析显示瓶颈，再考虑优化为 `for` 循环

---

## 阶段 8：测试与验证（优先级：高）

### Task 8.1：手动测试 - 出售宝物
- **状态**: `[ ]`
- **测试步骤**:
  1. 启动游戏，选择有宝物的君主
  2. 打开"君主-宝物-出售"菜单
  3. 选择一个宝物出售
  4. 验证资金增加
  5. 验证宝物从持有列表消失
  6. 验证宝物出现在市场列表
- **验收标准**:
  - ✅ 资金增加 = 宝物价值
  - ✅ 宝物状态：`Available = false`, `HidePlace = null`

### Task 8.2：手动测试 - 购买宝物
- **状态**: `[ ]`
- **测试步骤**:
  1. 打开"君主-宝物-购买"菜单
  2. 验证显示已出售的宝物列表
  3. 选择一个宝物购买
  4. 验证资金扣除（原价 × 1.2）
  5. 验证宝物回到君主持有列表
  6. 验证宝物从市场列表消失
- **验收标准**:
  - ✅ 资金扣除 = 宝物价值 × 1.2
  - ✅ 宝物状态：`Available = true`, `BelongedPerson = Leader`

### Task 8.3：手动测试 - 搜索排除已出售宝物
- **状态**: `[ ]`
- **测试步骤**:
  1. 出售一个宝物
  2. 派遣武将搜索建筑
  3. 验证不会找到已出售的宝物
- **验收标准**:
  - ✅ 搜索结果不包含已出售宝物

### Task 8.4：手动测试 - 资金不足
- **状态**: `[ ]`
- **测试步骤**:
  1. 确保建筑资金 < 宝物价值 × 1.2
  2. 尝试购买宝物
  3. 验证交易失败或显示提示
- **验收标准**:
  - ✅ 交易不执行
  - ✅ 显示错误提示（可选）

### Task 8.5：手动测试 - 存档兼容性
- **状态**: `[ ]`
- **测试步骤**:
  1. 加载旧存档（没有 `SoldTreasures` 字段）
  2. 验证游戏正常运行
  3. 验证 `SoldTreasures` 初始化为空集合
  4. 保存游戏
  5. 重新加载，验证 `SoldTreasures` 正确序列化
- **验收标准**:
  - ✅ 旧存档加载不崩溃
  - ✅ 新存档正确保存/加载

---

## 阶段 9：代码审查（优先级：高）

### Task 9.1：Anti-Band-Aid Protocol 检查
- **状态**: `[ ]`
- **检查点**:
  - ✅ 无防御性空检查（`if (obj != null)` 或 `?.`）
  - ✅ 无逻辑篡改（不修改游戏规则）
  - ✅ 只修改 `.cs` 文件（XML 由用户明确要求）
- **验收标准**:
  - ✅ 所有修改符合 Anti-Band-Aid Protocol

### Task 9.2：性能策略检查
- **状态**: `[ ]`
- **检查点**:
  - ✅ Hot Path 使用 `for` 循环（`SearchTreasure`）
  - ✅ Cold Path 优先可读性（交易逻辑）
  - ✅ 无不必要的 LINQ 在 Hot Path
- **验收标准**:
  - ✅ 所有修改符合性能策略

### Task 9.3：C# 12 语法检查
- **状态**: `[ ]`
- **检查点**:
  - ✅ 使用集合表达式 `[]`
  - ✅ 使用 `?.` 安全导航（合理场景）
  - ✅ 避免旧式语法 `new List<int> { 1, 2 }`
- **验收标准**:
  - ✅ 所有新代码使用 C# 12 语法

### Task 9.4：AOT 兼容性检查
- **状态**: `[ ]`
- **检查点**:
  - ✅ 使用强类型事件（`TreasureEvents`）
  - ✅ 禁止反射（`System.Reflection`）
  - ✅ 所有序列化字段标记 `[DataMember]`
- **验收标准**:
  - ✅ 所有修改符合 AOT 要求

---

## 阶段 10：文档与总结（优先级：中）

### Task 10.1：创建修复总结文档
- **状态**: `[ ]`
- **文件**: `宝物交易系统实现总结.md`（根目录）
- **内容**:
  - 功能描述
  - 修改文件列表
  - 关键代码片段
  - 测试结果
  - 已知问题
- **验收标准**:
  - ✅ 文档完整
  - ✅ 放置在根目录

### Task 10.2：更新 Spec 状态
- **状态**: `[ ]`
- **文件**: `.kiro/specs/treasure-trading-system/README.md`（新文件）
- **内容**:
  - Spec 概述
  - 实现状态
  - 未来改进方向
- **验收标准**:
  - ✅ 文档完整

---

## 未来扩展任务（暂不实现）

### Task 11.1：AI 自动购买宝物
- **状态**: `[🔮]` 未来功能
- **文件**: AI 相关代码
- **描述**: 实现 AI 势力自动购买宝物的逻辑
- **实现计划**:
  - ⏸️ 当前版本：宝物买卖功能仅对玩家开放
  - 🔮 未来版本：编写专门的 AI 逻辑后再开放
  - 🎯 原因：避免 AI 行为不合理导致游戏平衡问题
- **依赖**: 所有核心功能完成 + AI 决策系统设计
- **验收标准**（未来实现时参考）:
  - ✅ AI 根据资金和需求自动购买
  - ✅ 每回合最多购买 1 个宝物
  - ✅ AI 购买决策合理（不过度消耗资金）

### Task 11.2：宝物价格波动
- **状态**: `[ ]`
- **描述**: 实现宝物价格随时间或供需变化
- **验收标准**:
  - ✅ 价格在 0.8-1.5 倍之间波动

### Task 11.3：宝物购买历史记录
- **状态**: `[ ]`
- **描述**: 记录宝物的交易历史
- **验收标准**:
  - ✅ 显示宝物被谁出售、何时购买

---

## 任务统计

- **总任务数**: 33
- **核心任务**: 23（当前版本实现）
- **未来扩展**: 3（AI 支持、价格波动、交易历史）
- **测试任务**: 5
- **文档任务**: 2

**当前版本范围**: 仅实现玩家宝物买卖功能，AI 功能留待未来扩展

## 预估工作量

- **阶段 1-5（核心功能）**: 4-6 小时
- **阶段 6（事件系统）**: 1-2 小时
- **阶段 7（搜索验证）**: 0.5-1 小时
- **阶段 8（测试）**: 2-3 小时
- **阶段 9（代码审查）**: 1 小时
- **阶段 10（文档）**: 1 小时

**总计**: 9.5-14 小时

---

## 下一步

1. 按顺序完成阶段 1-5 的核心任务
2. 运行阶段 8 的测试验证功能
3. 执行阶段 9 的代码审查
4. 完成阶段 10 的文档

开始实现前，请确认：
- ✅ 需求文档已审阅
- ✅ 设计文档已审阅
- ✅ 任务列表已确认
