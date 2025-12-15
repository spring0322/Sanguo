# AI系统快速使用指南

## 🚀 快速开始

AI系统已经成功集成到项目中！以下是快速使用指南。

## ✅ 验证集成状态

### 1. 运行验证脚本

在游戏启动后，可以运行以下代码来验证AI系统是否正常工作：

```csharp
// 在任何合适的地方调用（比如MainGame.cs或调试界面）
GameGlobal.AISystemIntegrationValidator.RunFullValidation();

// 或者运行快速验证
bool isWorking = GameGlobal.AISystemIntegrationValidator.QuickValidation();
```

### 2. 检查调试输出

启动游戏后，查看调试输出窗口，应该能看到类似以下信息：

```
[MainGameScreen] 🧠 AI决策系统初始化完成
[MainGameScreen] AI管理器和对象池初始化完成
[AIManager] 为部队 123 分配角色: 坦克
[AIManager] 处理部队: 15/50 用时: 2.3ms
```

## 🎮 基本使用

### 1. AI系统自动运行

AI系统已经集成到游戏的主循环中，会自动执行以下功能：

- **自动角色分配**: 为每个部队分配最适合的战术角色
- **智能移动决策**: 基于威胁评估和战略目标的移动决策
- **记忆系统**: 自动记录和更新敌军情报
- **影响力地图**: 实时计算战略威胁分布

### 2. 查看AI状态

可以通过以下方式查看AI系统的运行状态：

```csharp
// 在MainGameScreen中添加调试方法
public void ShowAIStatus()
{
    if (_aiManager != null)
    {
        // 显示性能统计
        string stats = _aiManager.GetPerformanceStats();
        System.Diagnostics.Debug.WriteLine($"AI状态: {stats}");
        
        // 显示详细统计
        var detailedStats = _aiManager.GetDetailedStats();
        foreach (var kvp in detailedStats)
        {
            System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
}
```

### 3. 查看部队角色

检查部队的AI角色分配：

```csharp
// 遍历所有部队，查看角色分配
foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
{
    if (troop != null && !troop.Destroyed)
    {
        string roleName = GetRoleName(troop.CurrentRole);
        System.Diagnostics.Debug.WriteLine($"部队 {troop.ID}: {roleName}");
    }
}

private string GetRoleName(GameObjects.AI.AIRole role)
{
    return role switch
    {
        GameObjects.AI.AIRole.Tank => "坦克",
        GameObjects.AI.AIRole.DPS => "输出",
        GameObjects.AI.AIRole.Mage => "法师",
        GameObjects.AI.AIRole.Support => "辅助",
        GameObjects.AI.AIRole.Logistics => "后勤",
        _ => "未分配"
    };
}
```

## ⚙️ 配置调优

### 1. 性能配置

根据设备性能调整AI参数：

```csharp
// 在MainGameScreen的初始化方法中
private void ConfigureAIPerformance()
{
    if (_aiManager != null)
    {
        // 低性能设备配置
        if (IsLowEndDevice())
        {
            _aiManager.ConfigurePerformance(
                maxTimeBudgetMs: 3,    // 降低时间预算
                decisionInterval: 60   // 降低决策频率
            );
        }
        // 高性能设备配置
        else
        {
            _aiManager.ConfigurePerformance(
                maxTimeBudgetMs: 8,    // 提高时间预算
                decisionInterval: 15   // 提高决策频率
            );
        }
    }
}

private bool IsLowEndDevice()
{
    // 根据实际情况判断设备性能
    // 可以基于CPU、内存、GPU等信息
    return Environment.ProcessorCount < 4;
}
```

### 2. AI行为调整

可以清理AI缓存来重新评估部队行为：

```csharp
// 在游戏设置改变时调用
public void RefreshAIBehavior()
{
    if (GameManager.AIDecisionManager.Instance != null)
    {
        GameManager.AIDecisionManager.Instance.ClearBehaviorCache();
        System.Diagnostics.Debug.WriteLine("AI行为缓存已清理，将重新评估部队行为模式");
    }
}
```

## 🔧 调试和监控

### 1. 启用详细日志

在调试模式下启用详细的AI日志：

```csharp
// 在MainGameScreen的Update方法中添加
private void LogAIStatus(int frameCounter)
{
    // 每5秒输出一次AI状态
    if (frameCounter % 300 == 0 && _aiManager != null)
    {
        var stats = _aiManager.GetDetailedStats();
        
        System.Diagnostics.Debug.WriteLine("=== AI系统状态 ===");
        System.Diagnostics.Debug.WriteLine($"总部队数: {stats["TotalTroops"]}");
        System.Diagnostics.Debug.WriteLine($"本帧处理: {stats["ProcessedThisFrame"]}");
        System.Diagnostics.Debug.WriteLine($"处理时间: {stats["ProcessingTime"]:F2}ms");
        
        if (stats.ContainsKey("RoleDistribution"))
        {
            var roleDistribution = (Dictionary<string, int>)stats["RoleDistribution"];
            System.Diagnostics.Debug.WriteLine("角色分布:");
            foreach (var kvp in roleDistribution)
            {
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value}个");
            }
        }
    }
}
```

### 2. 性能监控

监控AI系统的性能表现：

```csharp
private void MonitorAIPerformance()
{
    if (_aiManager != null)
    {
        var stats = _aiManager.GetDetailedStats();
        
        if (stats.ContainsKey("TimeBudgetUtilization"))
        {
            float utilization = (float)stats["TimeBudgetUtilization"];
            
            if (utilization > 90f)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ AI系统负载过高: {utilization:F1}%");
                // 自动降低AI处理频率
                _aiManager.ConfigurePerformance(maxTimeBudgetMs: 3, decisionInterval: 45);
            }
            else if (utilization < 30f && utilization > 0f)
            {
                System.Diagnostics.Debug.WriteLine($"✅ AI系统负载正常: {utilization:F1}%");
            }
        }
    }
}
```

## 🎯 常见问题解决

### 1. AI不响应

**症状**: 部队不执行AI行为，停在原地不动

**解决方案**:
```csharp
// 检查AI管理器是否正确初始化
if (_aiManager == null)
{
    System.Diagnostics.Debug.WriteLine("❌ AI管理器未初始化");
    // 重新初始化
    var troopList = Session.Current.Scenario.Troops.GetList().Cast<Troop>().ToList();
    _aiManager = new GameManager.AIManager(troopList);
}

// 检查部队列表是否为空
if (_aiManager != null)
{
    var stats = _aiManager.GetDetailedStats();
    System.Diagnostics.Debug.WriteLine($"AI管理器管理的部队数: {stats["TotalTroops"]}");
}
```

### 2. 性能问题

**症状**: 游戏卡顿，帧率下降

**解决方案**:
```csharp
// 降低AI处理频率
_aiManager.ConfigurePerformance(maxTimeBudgetMs: 2, decisionInterval: 90);

// 检查处理时间
var stats = _aiManager.GetPerformanceStats();
System.Diagnostics.Debug.WriteLine($"AI处理时间: {stats}");
```

### 3. 角色分配异常

**症状**: 部队角色显示为"未分配"或角色不合理

**解决方案**:
```csharp
// 手动重新分配角色
foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
{
    if (troop != null && !troop.Destroyed && troop.CurrentRole == GameObjects.AI.AIRole.None)
    {
        troop.CurrentRole = GameObjects.AI.AIRoleSelector.GetBestRole(troop);
        System.Diagnostics.Debug.WriteLine($"为部队 {troop.ID} 重新分配角色: {troop.CurrentRole}");
    }
}
```

## 📊 性能基准

### 正常性能指标

- **处理时间**: 每帧 < 5ms
- **时间预算利用率**: 30-70%
- **角色分配成功率**: > 95%
- **记忆单位数量**: 根据地图大小，通常 < 100个

### 性能优化建议

1. **低端设备**: 时间预算 ≤ 3ms，决策间隔 ≥ 60帧
2. **中端设备**: 时间预算 5ms，决策间隔 30帧
3. **高端设备**: 时间预算 ≥ 8ms，决策间隔 ≤ 15帧

## 🔄 更新和维护

### 定期维护任务

1. **清理AI缓存** (每局游戏开始时):
```csharp
GameManager.AIDecisionManager.Instance?.ClearBehaviorCache();
```

2. **更新部队列表** (当部队创建/销毁时):
```csharp
if (_aiManager != null)
{
    var currentTroops = Session.Current.Scenario.Troops.GetList().Cast<Troop>().ToList();
    _aiManager.UpdateTroopList(currentTroops);
}
```

3. **监控内存使用** (定期检查):
```csharp
// 检查记忆系统内存使用
int totalGhosts = 0;
foreach (var faction in Session.Current.Scenario.Factions)
{
    if (faction?.MemoryMap != null)
    {
        totalGhosts += faction.MemoryMap.GetAllGhosts().Count;
    }
}
System.Diagnostics.Debug.WriteLine($"总记忆单位数: {totalGhosts}");
```

## 🎉 总结

AI系统已经成功集成并可以正常使用！主要特性包括：

- ✅ **自动运行**: 无需手动干预，AI系统会自动处理所有决策
- ✅ **智能角色**: 自动为部队分配最适合的战术角色
- ✅ **性能优化**: 智能的时间切片和优先级调度
- ✅ **记忆系统**: 基于情报的战略决策
- ✅ **可配置**: 丰富的配置选项适应不同需求

如果遇到问题，请：
1. 首先运行验证脚本检查集成状态
2. 查看调试输出了解AI系统运行情况
3. 根据性能表现调整配置参数
4. 定期执行维护任务保持系统健康

祝您游戏愉快！🎮