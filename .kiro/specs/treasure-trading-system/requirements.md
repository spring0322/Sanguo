# 需求文档：宝物交易系统

## 简介

宝物交易系统允许君主出售和购买宝物，实现宝物在游戏世界中的循环流转。系统基于现有粮食交易架构（`BuyFood`/`SellFood`），确保宝物不会永久消失，而是进入"已出售"状态并可被重新购买。

## 术语表

- **Treasure_Trading_System**：宝物交易系统，负责处理宝物的出售和购买逻辑
- **Sold_Treasure**：已出售宝物，指 `Available = false` 且 `HidePlace = null` 的宝物
- **Buyback_Price**：回购价格，计算公式为 `原价 × 1.2`
- **Treasure_Market**：宝物市场，存储所有已出售宝物的全局集合
- **Leader**：君主，势力的领导者，拥有宝物交易权限
- **Architecture**：建筑，执行交易操作的地点

## 需求

### 需求 1：宝物出售状态管理

**用户故事：** 作为君主，我想出售宝物时不让它隐藏在建筑中，以便宝物进入市场流通而不是被搜索到。

#### 验收标准

1. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 设置宝物的 `Available` 为 `false`
2. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 设置宝物的 `HidePlace` 为 `null`（不隐藏在建筑中）
3. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 将宝物添加到 Treasure_Market 集合
4. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 从君主的宝物列表中移除该宝物
5. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 增加建筑的资金 `原价 × 1000`

**正确性属性：**
- **不变量**：出售后宝物总数保持不变（`Scenario.AllTreasures.Count` 不变）
- **状态转换**：出售前 `Available = true` → 出售后 `Available = false && HidePlace = null`
- **资金守恒**：出售前后资金变化量 = `宝物价值 × 1000`

### 需求 2：建筑搜索宝物排除已出售宝物

**用户故事：** 作为玩家，我想搜索建筑时不会找到已出售的宝物，以便游戏逻辑符合现实。

#### 验收标准

1. WHEN 执行建筑搜索操作，THE Treasure_Trading_System SHALL 过滤掉所有 `HidePlace = null` 的宝物
2. WHEN 执行建筑搜索操作，THE Treasure_Trading_System SHALL 只返回 `HidePlace != null` 的宝物
3. FOR ALL 已出售宝物（`HidePlace = null`），搜索操作 SHALL NOT 返回该宝物

**正确性属性：**
- **集合分离**：`可搜索宝物集合 ∩ 已出售宝物集合 = ∅`（空集）
- **过滤正确性**：`len(搜索结果) ≤ len(建筑隐藏宝物)`

### 需求 3：宝物购买功能

**用户故事：** 作为君主，我想购买已出售的宝物，以便重新获得宝物但需要支付更高的价格。

#### 验收标准

1. WHEN 君主选择购买宝物，THE Treasure_Trading_System SHALL 显示所有 Sold_Treasure 的列表
2. WHEN 君主确认购买，THE Treasure_Trading_System SHALL 计算 Buyback_Price 为 `原价 × 1.2`
3. WHEN 君主资金充足（`Architecture.Fund >= Buyback_Price × 1000`），THE Treasure_Trading_System SHALL 扣除资金
4. WHEN 购买成功，THE Treasure_Trading_System SHALL 设置宝物的 `Available` 为 `true`
5. WHEN 购买成功，THE Treasure_Trading_System SHALL 设置宝物的 `BelongedPerson` 为君主
6. WHEN 购买成功，THE Treasure_Trading_System SHALL 从 Treasure_Market 移除该宝物
7. IF 君主资金不足（`Architecture.Fund < Buyback_Price × 1000`），THEN THE Treasure_Trading_System SHALL 显示错误提示并取消购买

**正确性属性：**
- **价格计算**：`Buyback_Price = Worth × 1.2`（精度误差 < 1）
- **资金检查**：购买操作执行 ⇒ `Fund_Before >= Buyback_Price × 1000`
- **状态恢复**：购买后 `Available = true && BelongedPerson = Leader`
- **往返属性**：出售后立即购买，宝物状态应恢复（除了资金损失）

### 需求 4：上下文菜单集成

**用户故事：** 作为玩家，我想在势力菜单中看到"购买宝物"选项，以便方便地访问购买功能。

#### 验收标准

1. WHEN 玩家打开势力-宝物菜单，THE Treasure_Trading_System SHALL 显示"购买"选项
2. WHEN 玩家点击"购买"选项，THE Treasure_Trading_System SHALL 显示已出售宝物列表
3. WHEN 已出售宝物列表为空，THE Treasure_Trading_System SHALL 显示"暂无可购买宝物"提示
4. THE Treasure_Trading_System SHALL 在宝物列表中显示购买价格（`原价 × 1.2`）

**正确性属性：**
- **菜单可见性**：`Treasure_Market.Count > 0` ⇒ 购买选项可用
- **列表一致性**：显示的宝物列表 = `Treasure_Market` 的内容

### 需求 5：序列化支持

**用户故事：** 作为开发者，我想确保宝物交易状态能正确保存和加载，以便游戏存档功能正常工作。

#### 验收标准

1. WHEN 保存游戏，THE Treasure_Trading_System SHALL 序列化 Treasure_Market 集合
2. WHEN 加载游戏，THE Treasure_Trading_System SHALL 反序列化 Treasure_Market 集合
3. WHEN 加载游戏，THE Treasure_Trading_System SHALL 恢复所有已出售宝物的状态（`Available = false`, `HidePlace = null`）
4. THE Treasure_Trading_System SHALL 使用 `[DataMember]` 标记所有需要序列化的字段

**正确性属性：**
- **往返属性（序列化）**：`保存(状态) → 加载(数据) = 原始状态`
- **集合完整性**：`加载后的 Treasure_Market.Count = 保存前的 Treasure_Market.Count`

### 需求 6：事件系统集成

**用户故事：** 作为开发者，我想在宝物交易时触发事件，以便其他系统（如日志、统计）能响应交易行为。

#### 验收标准

1. WHEN 君主出售宝物，THE Treasure_Trading_System SHALL 触发 `OnSellTreasure` 事件
2. WHEN 君主购买宝物，THE Treasure_Trading_System SHALL 触发 `OnBuyTreasure` 事件
3. THE Treasure_Trading_System SHALL 传递 `(GameScenario, Architecture, Treasure)` 作为事件参数
4. THE Treasure_Trading_System SHALL 使用 AOT 兼容的事件模式（禁止反射）

**正确性属性：**
- **事件顺序**：出售操作 → `OnSellTreasure` 触发（在状态修改后）
- **事件顺序**：购买操作 → `OnBuyTreasure` 触发（在状态修改后）

### 需求 7：AI 自动交易支持（未来扩展，暂不实现）

**状态**: 🔮 未来功能，当前版本不实现

**用户故事：** 作为 AI 势力，我想能够自动购买宝物，以便增强 AI 的智能表现。

**实现计划**:
- ⏸️ 当前版本：宝物买卖功能仅对玩家开放
- 🔮 未来版本：编写专门的 AI 逻辑后再开放 AI 买卖
- 🎯 原因：避免 AI 行为不合理导致游戏平衡问题

#### 验收标准（未来实现时参考）

1. WHERE AI 自动交易启用，WHEN AI 势力资金充足且需要宝物，THE Treasure_Trading_System SHALL 自动购买合适的宝物
2. WHERE AI 自动交易启用，THE Treasure_Trading_System SHALL 根据宝物属性和君主特性评估购买优先级
3. WHERE AI 自动交易启用，THE Treasure_Trading_System SHALL 限制 AI 每回合最多购买 1 个宝物

**正确性属性：**
- **资源约束**：AI 购买操作 ⇒ `AI_Fund >= Buyback_Price × 1000`
- **频率限制**：每回合 AI 购买次数 ≤ 1

**技术约束（未来实现时）**:
- 需要设计 AI 决策权重系统
- 需要防止 AI 过度消耗资金
- 需要考虑游戏平衡性

## 技术约束

1. **AOT 编译兼容**：禁止使用 `System.Reflection`，使用源生成器或强类型事件
2. **序列化兼容**：所有新增字段必须标记 `[DataMember]`
3. **C# 12 语法**：使用集合表达式 `List<int> x = [1, 2];`
4. **性能要求**：
   - 🔥 HOT PATH：宝物搜索过滤（每次搜索调用）→ 使用 `for` 循环，禁止 LINQ
   - 🧊 COLD PATH：购买/出售操作（低频）→ 允许 LINQ，优先可读性

## 依赖关系

- **复用架构**：参考 `BuyFood`/`SellFood` 的实现模式
- **现有类**：`Treasure`, `Architecture`, `Faction`, `Person`
- **现有事件系统**：`InternalAffairEvents` 模式
- **UI 框架**：`FrameFunction`, `ContextMenuResult`

## 非功能需求

1. **可维护性**：代码结构清晰，遵循现有命名约定
2. **可测试性**：核心逻辑可单元测试
3. **本地化**：所有 UI 文本使用简体中文
4. **向后兼容**：旧存档加载时，`Treasure_Market` 默认为空集合
5. **玩家专属**：当前版本仅对玩家势力开放，AI 势力不参与宝物交易（未来扩展）
