# Spec: 宝物交易系统

## 📋 概述

**功能名称**: 宝物交易系统（Treasure Trading System）  
**创建日期**: 2026-03-03  
**状态**: 📝 设计阶段  
**优先级**: 高

## 🎯 目标

实现宝物的出售和购买功能，允许**玩家势力**的君主将宝物出售到市场，并以加价 20% 的价格回购。确保宝物在游戏世界中循环流转，而不是永久消失或被搜索到。

**当前版本范围**:
- ✅ 玩家势力可以买卖宝物
- ⏸️ AI 势力暂不参与（未来扩展）

## 📚 文档结构

```
.kiro/specs/treasure-trading-system/
├── README.md          # 本文件：Spec 概述
├── requirements.md    # 需求文档（已完成）
├── design.md          # 设计文档（已完成）
└── tasks.md           # 任务列表（已完成）
```

## 🚀 快速开始

### 1. 阅读需求文档
```bash
# 查看完整需求
cat .kiro/specs/treasure-trading-system/requirements.md
```

**当前版本范围**:
- ✅ 玩家势力可以买卖宝物
- ⏸️ AI 势力暂不参与（未来扩展）

**功能描述**:
- 出售宝物不隐藏在建筑中（进入市场）
- 已出售宝物不能被搜索到
- 购买宝物需支付原价 × 1.2
- 添加"购买"菜单项

### 2. 阅读设计文档
```bash
# 查看技术设计
cat .kiro/specs/treasure-trading-system/design.md
```

**核心设计**:
- 复用粮食交易系统架构（`BuyFood`/`SellFood`）
- 在 `GameScenario` 添加 `SoldTreasures` 集合
- 通过 `HidePlace = null` 区分"已出售"和"隐藏"状态
- 使用强类型事件系统（AOT 兼容）

### 3. 查看任务列表
```bash
# 查看实现任务
cat .kiro/specs/treasure-trading-system/tasks.md
```

**任务阶段**:
1. 核心数据结构（2 个任务）
2. 出售宝物逻辑（2 个任务）
3. 购买宝物逻辑（3 个任务）
4. 枚举扩展（2 个任务）
5. UI 集成（3 个任务）
6. 事件系统（3 个任务）
7. 搜索逻辑验证（2 个任务）
8. 测试与验证（5 个任务）
9. 代码审查（4 个任务）
10. 文档与总结（2 个任务）

## 📊 实现状态

### 当前阶段
- [x] 需求分析
- [x] 技术设计
- [x] 任务规划
- [ ] 代码实现
- [ ] 测试验证
- [ ] 文档总结

### 任务进度
- **总任务数**: 33
- **已完成**: 0
- **进行中**: 0
- **待完成**: 33

## 🔑 关键决策

### 1. 数据存储方案
**决策**: 在 `GameScenario` 添加 `SoldTreasures` 集合  
**理由**:
- ✅ 全局可访问（所有势力共享市场）
- ✅ 复用现有 `TreasureList` 类型
- ✅ 序列化兼容（`[DataMember]`）
- ✅ 向后兼容（旧存档自动初始化为空）

### 2. 状态标记方案
**决策**: 复用 `Available` 和 `HidePlace` 字段  
**理由**:
- ✅ 无需添加新字段（避免序列化复杂度）
- ✅ 通过 `HidePlace = null` 区分"已出售"和"隐藏"
- ✅ 符合现有逻辑

**状态映射**:
| 状态 | Available | HidePlace | BelongedPerson |
|------|-----------|-----------|----------------|
| 持有中 | `true` | `null` | `Person` |
| 隐藏中 | `false` | `Architecture` | `null` |
| 已出售 | `false` | `null` | `null` |

### 3. 架构复用
**决策**: 复用粮食交易系统（`BuyFood`/`SellFood`）  
**理由**:
- ✅ 成熟的交易逻辑模式
- ✅ 减少代码重复
- ✅ 保持系统一致性

## 🛠️ 技术栈

- **语言**: C# 12
- **框架**: .NET 8, MonoGame
- **编译**: AOT（禁止反射）
- **序列化**: DataContract
- **事件系统**: 强类型事件

## 📝 实现规范

### Anti-Band-Aid Protocol
- ❌ 禁止防御性空检查（`if (obj != null)` 或 `?.`）
- ❌ 禁止逻辑篡改（不修改游戏规则）
- ✅ 只修改 `.cs` 文件（XML 由用户明确要求）

### 性能策略
- 🔥 **Hot Path**（`SearchTreasure`）: 使用 `for` 循环，禁止 LINQ
- 🧊 **Cold Path**（交易逻辑）: 允许 LINQ，优先可读性

### C# 12 语法
- ✅ 使用集合表达式 `List<int> x = [1, 2];`
- ✅ 使用主构造函数（适当场景）
- ❌ 避免旧式语法 `new List<int> { 1, 2 }`

### AOT 兼容
- ✅ 使用强类型事件（`TreasureEvents`）
- ❌ 禁止反射（`System.Reflection`）
- ✅ 所有序列化字段标记 `[DataMember]`

## 🧪 测试计划

### 手动测试
1. **出售宝物**: 验证资金增加、宝物进入市场
2. **购买宝物**: 验证资金扣除（× 1.2）、宝物回到持有列表
3. **搜索排除**: 验证搜索不会找到已出售宝物
4. **资金不足**: 验证交易失败
5. **存档兼容**: 验证旧存档加载正常

### 单元测试（可选）
- `TestSellTreasure_ShouldAddToMarket()`
- `TestBuyTreasure_ShouldRemoveFromMarket()`
- `TestSearchTreasure_ShouldExcludeSoldTreasures()`

## 📦 交付物

### 代码文件
- `WorldOfTheThreeKingdoms/GameObjects/GameScenario.cs`（修改）
- `WorldOfTheThreeKingdoms/GameObjects/Architecture.cs`（修改）
- `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`（修改）
- `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`（修改）
- `WorldOfTheThreeKingdoms/GameGlobal/ContextMenuResult.cs`（修改）
- `WorldOfTheThreeKingdoms/GameGlobal/FrameFunction.cs`（修改）
- `WorldOfTheThreeKingdoms/GameObjects/Events/TreasureEvents.cs`（新增）

### 配置文件
- `Content/Data/Plugins/ContextMenuData.xml`（修改）

### 文档
- `宝物交易系统实现总结.md`（根目录）

## 🔮 未来改进

### 未来扩展功能（当前版本不实现）

#### 1. AI 自动购买（优先级：中）
- **状态**: ⏸️ 暂不实现
- **原因**: 需要先编写专门的 AI 逻辑，避免 AI 行为不合理
- **计划**: 未来版本设计 AI 决策权重系统后再开放
- **功能**: AI 势力根据资金和需求自动购买宝物

#### 2. 价格波动（优先级：低）
- **功能**: 宝物价格随时间或供需变化

#### 3. 交易历史（优先级：低）
- **功能**: 记录宝物的交易历史

### 性能优化
- 如果 `SearchTreasure()` 在游戏循环中频繁调用，优化为 Hot Path

## 📞 联系与支持

### 问题反馈
- 如果发现 bug，请在根目录创建 `宝物交易系统问题报告.md`
- 如果需要新功能，请更新 `requirements.md`

### 代码审查
- 所有代码修改必须通过 Anti-Band-Aid Protocol 检查
- 所有代码修改必须通过性能策略检查
- 所有代码修改必须通过 C# 12 语法检查
- 所有代码修改必须通过 AOT 兼容性检查

## 📜 变更日志

### 2026-03-03
- ✅ 创建需求文档（7 个核心需求）
- ✅ 创建设计文档（高层设计 + 低层设计）
- ✅ 创建任务列表（33 个任务）
- ✅ 创建 README 文档

---

**下一步**: 开始实现阶段 1（核心数据结构）的任务。
