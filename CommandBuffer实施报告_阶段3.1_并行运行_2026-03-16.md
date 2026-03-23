# Command Buffer 实施报告 - 阶段 3.1（并行运行测试）

**日期：** 2026-03-16  
**状态：** ✅ 完成  
**阶段：** 3.1 - 并行运行测试（不修改游戏状态）

---

## 📋 实施内容

### 1. 全局开关

**文件：** `WorldOfTheThreeKingdoms/GameGlobal/GlobalVariables.cs`

```csharp
[DataMember] public bool EnableWegoEngine = false;  // 🔥 2026-03-16 新增：WEGO 引擎开关，默认关闭（测试阶段）
```

**位置：** 第 169 行

**说明：**
- 默认关闭，避免影响正常游戏
- 测试时手动打开（修改存档或代码）
- 后续可通过 UI 配置

---

### 2. 集成点修改

**文件：** `WorldOfTheThreeKingdoms/GameScreens/MGSDate.cs`

**修改位置：** `AfterDayStarting()` 方法（第 36-65 行）

**修改前：**
```csharp
private bool AfterDayStarting(GameTime gameTime)
{
    return this.MoveTheTroops(gameTime);
}
```

**修改后：**
```csharp
private bool AfterDayStarting(GameTime gameTime)
{
    bool result = this.MoveTheTroops(gameTime);
    
    // 🔥 2026-03-16 阶段 3.1：并行运行 WegoEngine（测试模式）
    // 仅在开关打开时运行，不修改游戏状态，只记录对比日志
    if (Session.GlobalVariables.EnableWegoEngine && Session.Current?.WegoEngine != null)
    {
        try
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[AfterDayStarting] 🔥 开始 WegoEngine.Update()（测试模式）");
            #endif
            
            Session.Current.WegoEngine.Update();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] ✅ WegoEngine 完成: 移动={Session.Current.WegoEngine.ProcessedMoveCommands}, 战斗={Session.Current.WegoEngine.ProcessedAttackCommands}, 计略={Session.Current.WegoEngine.ProcessedStratagemCommands}, 攻城={Session.Current.WegoEngine.ProcessedSiegeCommands}");
            System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] 📊 对比: 现有逻辑返回={result} (false=全部移完, true=还在移动)");
            #endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] ❌ WegoEngine 异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    return result;
}
```

**关键设计：**
1. **不修改游戏状态**：`WegoEngine.Update()` 只读取快照，不写入游戏对象
2. **异常隔离**：`try-catch` 确保 `WegoEngine` 崩溃不影响游戏
3. **对比日志**：记录现有逻辑和 `WegoEngine` 的结果，便于对比
4. **条件编译**：日志仅在 DEBUG 模式下输出

---

## 📊 调用链分析

### 完整调用链

```
MainGameScreen.GameGo(gameTime)
│
├─ AfterDayPassed(gameTime)
│  └─ RunTheFactions(gameTime)
│     └─ Faction.Run()
│        ├─ [玩家势力] 等待玩家操作
│        └─ [AI势力] AI决策
│
└─ [所有势力完成后]
   ├─ DateRunnerPlugin.DateGo()
   │
   ├─ AfterDayStarting(gameTime)  ← 🔥 集成点
   │  ├─ MoveTheTroops(gameTime)  ← 现有逻辑
   │  └─ WegoEngine.Update()      ← 新逻辑（测试模式）
   │
   └─ DateRunnerPlugin.DateStop()
```

### 时序说明

1. **战略阶段**（`AfterDayPassed`）：
   - 玩家下达命令
   - AI 势力决策
   - 所有部队设置 `Operated = true` + `Command = xxx`

2. **执行阶段**（`AfterDayStarting`）：
   - **现有逻辑**：`MoveTheTroops()` 逐帧移动部队
   - **新逻辑**：`WegoEngine.Update()` 并行处理所有部队（测试模式）

3. **结束阶段**：
   - `DateStop()` 结束本日
   - 重置部队状态

---

## 🧪 测试计划

### 自动化测试

**测试工具：** `WegoEngineTests.cs`

**测试方法：**
```csharp
// 方法 1：在游戏中调用（推荐）
// 加载场景后，在 Visual Studio 的即时窗口中输入：
mainGameScreen.TestWegoEngine()

// 方法 2：在代码中调用
// 在 MainGameScreen.cs 的某个位置添加：
this.TestWegoEngine();
```

**测试套件：**

1. **测试 1：空场景测试**
   - 验证：无部队时不崩溃
   - 预期：所有指令计数为 0

2. **测试 2：单部队移动测试**
   - 验证：单个部队的移动指令能正确处理
   - 预期：如果部队有移动命令且已操作，处理至少 1 条移动指令

3. **测试 3：多部队场景测试**
   - 验证：多个部队能正确处理
   - 预期：处理的指令数 <= 已操作的部队数

4. **测试 4：性能测试**
   - 验证：大量部队时的性能
   - 预期：平均耗时 < 16.67ms（60fps）

**测试输出示例：**
```
========================================
[WegoEngineTests] 开始运行所有测试
========================================
[WegoEngineTests] 测试 1：空场景测试
[WegoEngineTests] ✅ 空场景测试通过
  - 处理指令数: 移动=0, 战斗=0
[WegoEngineTests] 测试 2：单部队移动测试
  - 部队: 曹操军
  - 初始位置: {X:100 Y:200}
  - 命令: Move
  - 已操作: True
[WegoEngineTests] ✅ 单部队移动测试完成
  - 处理指令数: 移动=1, 战斗=0
[WegoEngineTests] 测试 3：多部队场景测试
  - 部队数量: 15
  - 已操作部队: 10
  - 移动命令: 8
  - 攻击命令: 2
[WegoEngineTests] ✅ 多部队场景测试完成
  - 处理指令数: 移动=8, 战斗=2, 计略=0, 攻城=0
[WegoEngineTests] 测试 4：性能测试
  - 部队数量: 15
  - 迭代次数: 10
[WegoEngineTests] ✅ 性能测试完成
  - 总耗时: 45.23ms
  - 平均耗时: 4.52ms/回合
  - 每部队耗时: 0.3013ms
========================================
[WegoEngineTests] 测试完成: 4/4 通过
========================================
```

---

### 手动测试

**测试步骤：**

1. **启用开关**：
   ```csharp
   // 方法 1：修改代码（临时测试）
   [DataMember] public bool EnableWegoEngine = true;
   
   // 方法 2：修改存档（持久测试）
   // 在存档 XML 中添加：
   <EnableWegoEngine>true</EnableWegoEngine>
   ```

2. **加载测试场景**：
   - 推荐：`184DHZS.json`（董卓之乱）
   - 原因：部队数量适中，便于观察

3. **运行游戏**：
   - 点击"进行"按钮
   - 观察 DEBUG 输出

4. **检查日志**：
   ```
   [AfterDayStarting] 🔥 开始 WegoEngine.Update()（测试模式）
   [WegoEngine] 决策阶段开始，部队数量: 15
   [WegoEngine] 决策阶段完成，耗时: 2ms
   [WegoEngine] 结算阶段开始
   [WegoEngine] 结算阶段完成，耗时: 5ms
   [AfterDayStarting] ✅ WegoEngine 完成: 移动=10, 战斗=3, 计略=1, 攻城=1
   [AfterDayStarting] 📊 对比: 现有逻辑返回=false (false=全部移完, true=还在移动)
   ```

### 预期结果

| 指标 | 现有逻辑 | WegoEngine | 说明 |
|------|---------|-----------|------|
| 移动指令数 | N/A | 10 | WegoEngine 处理的移动指令 |
| 战斗指令数 | N/A | 3 | WegoEngine 处理的战斗指令 |
| 计略指令数 | N/A | 1 | WegoEngine 处理的计略指令 |
| 攻城指令数 | N/A | 1 | WegoEngine 处理的攻城指令 |
| 返回值 | false | N/A | 现有逻辑的返回值（false=全部移完） |

**对比方法：**
- 手动统计现有逻辑处理的部队数量
- 对比 `WegoEngine` 的指令数量
- 检查是否一致

---

## 🔍 调试技巧

### 1. 查看部队状态

```csharp
// 在 WegoEngine.Update() 开始前添加：
foreach (var troop in _troops)
{
    System.Diagnostics.Debug.WriteLine($"[部队状态] {troop.DisplayName}: Operated={troop.Operated}, Command={troop.Command}, Position={troop.Position}");
}
```

### 2. 对比快照和实际状态

```csharp
// 在 WegoEngine.CreateSnapshot() 后添加：
var snapshot = CreateSnapshot();
foreach (var troopSnapshot in snapshot.Troops)
{
    var actualTroop = _troops.FirstOrDefault(t => t.ID == troopSnapshot.TroopID);
    if (actualTroop != null)
    {
        System.Diagnostics.Debug.WriteLine($"[快照对比] {actualTroop.DisplayName}: 快照位置={troopSnapshot.Position}, 实际位置={actualTroop.Position}");
    }
}
```

### 3. 检查指令缓冲区

```csharp
// 在 WegoEngine.AIDecisionPhase() 后添加：
_commandBuffer.LogCommands();  // 已在 Update() 中启用
```

---

## ⚠️ 已知限制

### 1. 简化版实现

当前 `WegoEngine` 使用简化版辅助方法：

| 方法 | 简化内容 | 影响 |
|------|---------|------|
| `CalculateNextStep()` | 直线移动 + 边界检查 | 无寻路，可能穿墙 |
| `FindNearestEnemy()` | 距离平方优化 | 无视野检测 |
| `CalculateDamage()` | 基础伤害 * 攻防修正 | 无地形、天气、士气影响 |
| `CanMoveTo()` | 边界检查 | 无地形检测 |

**说明：** 这些简化不影响测试，因为我们只是对比指令数量，不关心具体结果。

### 2. 不修改游戏状态

`WegoEngine.Update()` 只读取快照，不写入游戏对象。因此：
- 部队位置不会改变
- 部队血量不会改变
- 游戏状态完全由现有逻辑控制

**优点：** 安全，不会破坏游戏
**缺点：** 无法验证结果正确性

---

## 📈 下一步计划

### 阶段 3.2：部分替换（单部队测试）

**目标：** 让 `WegoEngine` 控制单个部队，验证结果正确性

**步骤：**
1. 添加 `EnableWegoEngineForTroop(int troopID)` 方法
2. 在 `MoveTheTroops()` 中跳过被 `WegoEngine` 控制的部队
3. 在 `WegoEngine.ExecutionPhase()` 中写入游戏状态
4. 对比单部队的移动结果

### 阶段 3.3：部分替换（多部队测试）

**目标：** 让 `WegoEngine` 控制多个部队，验证战斗逻辑

**步骤：**
1. 扩展到 5-10 个部队
2. 验证战斗结算正确性
3. 验证计略结算正确性

### 阶段 3.4：完全替换

**目标：** 完全替换 `MoveTheTroops()`，移除旧逻辑

**步骤：**
1. 将 `WegoEngine.Update()` 移到 `MoveTheTroops()` 内部
2. 移除旧的部队移动逻辑
3. 全面测试所有场景

---

## 📚 相关文档

- [Command Buffer 方案可行性分析](CommandBuffer方案可行性分析_2026-03-16.md)
- [阶段 1 实施报告](CommandBuffer实施报告_阶段1_2026-03-16.md)
- [阶段 2 实施报告](CommandBuffer实施报告_阶段2_2026-03-16.md)
- [阶段 3 集成计划](CommandBuffer实施报告_阶段3_集成计划_2026-03-16.md)
- [游戏核心机制](.kiro/steering/游戏核心机制.md)

---

**维护者：** Lead Architect  
**最后更新：** 2026-03-16
