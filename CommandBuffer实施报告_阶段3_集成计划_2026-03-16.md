# Command Buffer 实施报告 - 阶段 3（MainGameScreen 集成计划）

**日期：** 2026-03-16  
**状态：** 计划中  
**架构师：** Lead Architect

---

## 📋 集成点分析

### 1. WegoEngine 初始化 ✅ 已完成

**位置：** `Session.StartScenario()` → `PendingMainThreadInitialization`

```csharp
// 🔥 初始化 WegoEngine（2026-03-16）
// 位置：部队注册后，确保所有部队已在注册表中
Session.Current.WegoEngine = new GameManager.WegoEngine();

// 将所有已注册的部队添加到 WegoEngine
foreach (var kvp in Session.Current._troopRegistry)
{
    Session.Current.WegoEngine.RegisterTroop(kvp.Value);
}
```

**关键点：**
- 在部队注册后初始化（确保所有部队已在 `_troopRegistry` 中）
- 将所有部队注册到 `WegoEngine`
- 记录初始化日志

---

### 2. WegoEngine 调用点分析

根据 WEGO 机制，游戏流程如下：

```
GameGo(gameTime)
│
├─ AfterDayPassed()                    ← 战略阶段（操作面）
│  └─ Factions.RunQueue()
│     └─ Faction.Run()
│        ├─ [玩家势力] 等待玩家操作
│        └─ [AI势力] 战略决策
│
└─ [所有势力完成后]
   ├─ DateGo()                         ← 开始执行阶段
   ├─ AfterDayStarting(gameTime)       ← 🔥 集成点：部队移动
   │  └─ CurrentQueueTroopMove()       ← 现有的部队移动逻辑
   └─ DateStop()                       ← 结束执行阶段
```

**集成策略：**

#### 方案 A：渐进式集成（推荐）

**阶段 3.1：并行运行（测试阶段）**
- 保留现有的 `CurrentQueueTroopMove()` 逻辑
- 在 `AfterDayStarting()` 末尾调用 `WegoEngine.Update()`
- 仅记录日志，不修改游戏状态
- 对比两种实现的结果

**阶段 3.2：部分替换（过渡阶段）**
- 使用 `WegoEngine` 处理 AI 部队
- 使用现有逻辑处理玩家部队
- 验证 AI 行为正确性

**阶段 3.3：完全替换（最终阶段）**
- 使用 `WegoEngine` 处理所有部队
- 移除 `CurrentQueueTroopMove()` 逻辑
- 性能测试和优化

#### 方案 B：直接替换（激进）

**风险：**
- 可能破坏现有游戏逻辑
- 难以回滚
- 调试困难

**不推荐原因：**
- 现有代码库复杂，直接替换风险高
- 缺乏对比测试，难以验证正确性

---

### 3. 集成代码（阶段 3.1：并行运行）

#### 3.1.1 在 `AfterDayStarting()` 中添加 WegoEngine 调用

```csharp
private bool AfterDayStarting(GameTime gameTime)
{
    // 🔥 现有逻辑：部队移动
    bool troopsMovementDone = this.CurrentQueueTroopMove(gameTime);
    
    // 🔥 新增：WegoEngine 并行运行（仅测试，不修改游戏状态）
    // 日期：2026-03-16
    // 目的：对比两种实现的结果，验证 WegoEngine 正确性
    if (Session.Current?.WegoEngine != null && Session.GlobalVariables.EnableWegoEngine)
    {
        try
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[AfterDayStarting] 🧪 WegoEngine 测试运行开始");
            #endif
            
            // 🔥 关键：使用读锁保护快照创建
            Session.WorkLock.EnterReadLock();
            try
            {
                Session.Current.WegoEngine.Update();
            }
            finally
            {
                Session.WorkLock.ExitReadLock();
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] ✅ WegoEngine 测试完成: " +
                $"移动={Session.Current.WegoEngine.ProcessedMoveCommands}, " +
                $"战斗={Session.Current.WegoEngine.ProcessedAttackCommands}");
            #endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] ❌ WegoEngine 测试失败: {ex.Message}");
        }
    }
    
    return troopsMovementDone;
}
```

#### 3.1.2 添加全局开关

在 `GlobalVariables.xml` 中添加：

```xml
<EnableWegoEngine>false</EnableWegoEngine>
```

在 `GlobalVariables.cs` 中添加：

```csharp
public bool EnableWegoEngine { get; set; } = false;
```

---

### 4. 测试计划

#### 4.1 单元测试

**测试场景 1：空场景**
- 0 个部队
- 验证：不崩溃，无异常

**测试场景 2：单部队**
- 1 个部队，无目标
- 验证：部队不移动

**测试场景 3：两部队战斗**
- 2 个敌对部队，相邻位置
- 验证：战斗指令生成，伤害计算正确

**测试场景 4：大量部队**
- 100 个部队，随机分布
- 验证：性能可接受（<16ms/回合）

#### 4.2 集成测试

**测试场景 1：184 年剧本**
- 加载 184DHZS.json
- 运行 10 回合
- 验证：无崩溃，日志正常

**测试场景 2：对比测试**
- 同时运行现有逻辑和 WegoEngine
- 对比部队位置、兵力变化
- 验证：结果一致（或差异可解释）

---

## 🚧 当前状态

### 已完成 ✅

1. **阶段 1：基础架构**
   - ✅ 指令结构（`MoveCommand`, `AttackCommand`, `StratagemCommand`, `SiegeCommand`）
   - ✅ `CommandBuffer` 类
   - ✅ `WegoEngine` 核心引擎
   - ✅ `Session.WorkLock` 替换为 `ReaderWriterLockSlim`

2. **阶段 2：快照创建和辅助方法**
   - ✅ `CreateSnapshot()` 方法（简化版）
   - ✅ `CalculateNextStep()` 方法（简化版寻路）
   - ✅ `FindNearestEnemy()` 方法
   - ✅ `CalculateDamage()` 方法（简化版）
   - ✅ `CanMoveTo()` 方法（简化版地形检测）
   - ✅ 代码审查通过（无 Band-Aid，性能优化，C# 12 语法）

3. **阶段 3.0：初始化**
   - ✅ `WegoEngine` 在 `Session.StartScenario()` 中初始化
   - ✅ 部队注册到 `WegoEngine`

### 待完成 ⏳

4. **阶段 3.1：并行运行（测试阶段）**
   - ⏳ 在 `AfterDayStarting()` 中添加 `WegoEngine.Update()` 调用
   - ⏳ 添加全局开关 `EnableWegoEngine`
   - ⏳ 添加对比日志
   - ⏳ 单元测试
   - ⏳ 集成测试

5. **阶段 3.2：部分替换（过渡阶段）**
   - ⏳ 使用 `WegoEngine` 处理 AI 部队
   - ⏳ 保留现有逻辑处理玩家部队
   - ⏳ 验证 AI 行为正确性

6. **阶段 3.3：完全替换（最终阶段）**
   - ⏳ 使用 `WegoEngine` 处理所有部队
   - ⏳ 移除 `CurrentQueueTroopMove()` 逻辑
   - ⏳ 性能测试和优化

---

## 📊 性能目标

| 指标 | 目标 | 当前 | 状态 |
|------|------|------|------|
| 回合处理时间 | <16ms | 未测试 | ⏳ |
| 内存分配 | <20KB/回合 | ~12KB/回合（估算） | ✅ |
| 并发性能 | 5-10x 提升 | 未测试 | ⏳ |
| 代码可读性 | 高 | 高 | ✅ |

---

## 🎯 下一步行动

### 立即执行（优先级：高）

1. **添加全局开关**
   - 在 `GlobalVariables.xml` 中添加 `EnableWegoEngine`
   - 在 `GlobalVariables.cs` 中添加属性

2. **查找 `AfterDayStarting()` 方法**
   - 定位方法位置
   - 分析现有逻辑
   - 添加 `WegoEngine.Update()` 调用

3. **添加对比日志**
   - 记录现有逻辑的结果
   - 记录 `WegoEngine` 的结果
   - 对比差异

### 后续执行（优先级：中）

4. **单元测试**
   - 创建测试场景
   - 验证基本功能

5. **集成测试**
   - 加载真实剧本
   - 运行多回合
   - 验证稳定性

### 长期优化（优先级：低）

6. **完整实现**
   - 完整的快照创建（区分敌我）
   - 完整的寻路逻辑（A* 算法）
   - 完整的伤害计算（地形、天气、技能）
   - 完整的地形检测（兵种适应性）

---

## 🚨 风险评估

### 高风险

1. **现有逻辑复杂**
   - 风险：`CurrentQueueTroopMove()` 逻辑复杂，难以完全替换
   - 缓解：渐进式集成，先并行运行

2. **性能回归**
   - 风险：`WegoEngine` 性能不如现有逻辑
   - 缓解：性能测试，优化热点

### 中风险

3. **行为差异**
   - 风险：`WegoEngine` 行为与现有逻辑不一致
   - 缓解：对比测试，调整算法

4. **并发 Bug**
   - 风险：读写锁使用不当，导致死锁或数据竞争
   - 缓解：严格遵守锁规范，单元测试

### 低风险

5. **内存泄漏**
   - 风险：部队注册/注销不当，导致内存泄漏
   - 缓解：使用 `ConcurrentDictionary`，自动管理

---

## 📚 相关文档

- [Command Buffer 方案可行性分析](CommandBuffer方案可行性分析_2026-03-16.md)
- [阶段 1 实施报告](CommandBuffer实施报告_阶段1_2026-03-16.md)
- [阶段 2 实施报告](CommandBuffer实施报告_阶段2_2026-03-16.md)
- [游戏核心机制规范](.kiro/steering/游戏核心机制.md)
- [ANTI-BAND-AID 协议](.kiro/steering/System%20Prompt%20/%20Custom%20Instructions.md)

---

**维护者：** Lead Architect  
**最后更新：** 2026-03-16  
**状态：** 阶段 3.0 完成，等待阶段 3.1 实施
