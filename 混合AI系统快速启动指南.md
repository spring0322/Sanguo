# 混合AI系统 - 快速启动指南

**5分钟快速上手**

---

## ⚠️ 当前状态

系统使用**临时存根实现**，可以直接编译运行，但功能受限。

**当前文件**：
- ✅ `AISharedTypes.cs` - 共享类型（永久）
- ⚠️ `UtilityAIExecutorStub.cs` - 临时存根（功能受限）

**完整功能需要**：移除 `UtilityAICore.cs` 的 `#if false`

---

## 🚀 方案A：快速测试（使用存根）

### 第一步：编译（30秒）

```bash
dotnet build
```

✅ 应该编译成功！

---

### 第二步：集成决策器（2分钟）

打开文件：`WorldOfTheThreeKingdoms/GameObjects/AI/AITurnExecutor.cs`

**查找**：
```csharp
ActionProposal bestAction = UtilityAIExecutor.FindBestAction(
```

**替换为**：
```csharp
AIDecision decision = HybridAIDecisionMaker.DecideAction(
    activeTroop,
    visibleEnemies,
    allies,
    posture
);

Point moveTarget = decision.MoveTarget;
Troop attackTarget = decision.AttackTarget;
ActionType actionType = decision.ActionType;
```

---

### 第三步：添加回合重置（1分钟）

在回合管理器中添加：

```csharp
public void StartNewTurn()
{
    HybridAIDecisionMaker.ResetTurnCounter();
    // 原有代码...
}
```

---

### 第四步：运行测试（1分钟）

```csharp
HybridAISystemTests.Test1_ConfigDiagnostics();
```

**预期日志**：
```
[警告] 使用UtilityAI存根实现！请移除 UtilityAICore.cs 的 #if false 来启用完整功能
```

---

## 🎯 方案B：完整功能（推荐）

### 第一步：启用 UtilityAI（30秒）

打开文件：`WorldOfTheThreeKingdoms/GameObjects/AI/UtilityAICore.cs`

**删除这两行**：
```csharp
#if false    // ← 删除第8行
...
#endif       // ← 删除文件末尾
```

---

### 第二步：删除存根文件（10秒）

```bash
rm WorldOfTheThreeKingdoms/GameObjects/AI/UtilityAIExecutorStub.cs
```

或在 Visual Studio 中删除该文件。

---

### 第三步：编译（30秒）

```bash
dotnet build
```

---

### 第四步：集成和测试（同方案A）

按照方案A的步骤2-4执行。

**预期日志**（无警告）：
```
[UtilityAI] 关羽(部队101) 选择 NormalAttack (效用分: 850.5)
```

---

## 📊 预期结果

### 日志输出示例

```
=== AI决策配置诊断报告 ===

决策模式: Hybrid

【关键部队判定标准】
  属性阈值:
    统率 ≥ 80
    智力 ≥ 80
    武力 ≥ 80
  称号等级 ≥ 6
  兵力 > 5000
  精锐兵种数量: 16
  自动检测: 启用 (上限≤3000)

【性能限制】
  每回合最大UtilityAI使用次数: 20
  超时时间: 100ms
  超时降级: 启用

✅ 配置加载成功
```

### AI决策日志示例

```
[UtilityAI] 关羽(部队101) 选择 NormalAttack (效用分: 850.5) 原因: 武力95≥80, 战斗称号Lv7≥6
[UtilityAI] 张飞(部队102) 选择 Tactic (效用分: 920.3) 原因: 武力90≥80, 精锐兵种:白耳兵
[传统AI] 普通士兵(部队103) 移动到(45,67) 原因: 不满足任何关键部队条件

UtilityAI使用次数: 2/20
```

---

## 🎯 成功标志

- ✅ 编译无错误
- ✅ 配置加载成功
- ✅ 关键部队使用UtilityAI
- ✅ 普通部队使用传统AI
- ✅ 性能在可接受范围内

---

## 🐛 遇到问题？

### 问题1：编译错误

**症状**：`UtilityAIExecutor` 未定义

**解决**：检查是否删除了 `#if false`

---

### 问题2：配置加载失败

**症状**：日志显示"配置文件不存在"

**解决**：检查 `Content/Data/AIDecisionConfig.json` 是否存在

---

### 问题3：所有部队都用传统AI

**症状**：日志中没有"UtilityAI"字样

**解决**：
1. 检查部队属性是否达标（统/智/武 ≥ 80）
2. 临时降低阈值测试：
   ```json
   "StatThresholds": {
     "Command": 60,
     "Intelligence": 60,
     "Strength": 60
   }
   ```

---

## 📚 更多信息

- 完整文档：`修改记录/混合AI系统实施指南_2026-03-08.md`
- 测试示例：`修改记录/混合AI系统测试示例_2026-03-08.cs`
- 交付清单：`修改记录/混合AI系统完整交付清单_2026-03-08.md`

---

**祝你使用愉快！** 🎉
