# 运输兵AI系统实际集成指南

## 概述

本指南说明如何将运输兵AI系统紧密集成到游戏的实际代码中，实现更直接和高效的AI控制。

## 核心集成方案

### 1. TroopAIExecutor - 直接部队AI执行器

创建了`TroopAIExecutor.cs`，提供单个部队的完整AI逻辑：

```csharp
// 在游戏的部队回合逻辑中调用
public void ExecuteTurn(Troop troop)
{
    // 1. 角色分配
    troop.CurrentRole = AIRoleSelector.GetBestRole(troop);
    
    // 2. 根据角色执行不同逻辑
    if (troop.CurrentRole == AIRole.Logistics)
    {
        // 后勤单位特殊处理
        ExecuteLogisticsTurn(troop);
    }
    else
    {
        // 战斗单位正常处理
        ExecuteCombatTurn(troop);
    }
}
```

### 2. 后勤单位分支处理

#### A. 运输兵逻辑 (ID 29)
```csharp
if (troop.IsTransport)
{
    // 获取所有己方城市
    var myCities = troop.BelongedFaction.Architectures;
    
    // 检查是否已进城
    if (AILogisticsHandler.CheckEnterCity(troop, myCities)) return;
    
    // 逃逸式移动
    var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
    var enemies = GetNearbyEnemies(troop, 10);
    Point bestMove = AILogisticsHandler.GetTransportMoveTarget(troop, myCities, enemies, moveArea);
    
    troop.MoveTo(bestMove);
    return;
}
```

#### B. 工程兵逻辑 (ID 601/621)
```csharp
if (AIConstructionPlanner.IsConstructionTroop(troop))
{
    // 如果还没变身，寻找造塔点
    var moveArea = MapNavigationHelper.GetUnitMoveableArea(troop);
    var enemies = GetNearbyEnemies(troop, 8);
    Point bestBuildPos = AIConstructionPlanner.GetBestBuildPosition(troop, moveArea, enemies);
    
    // 移动
    if (troop.Position != bestBuildPos)
    {
        troop.MoveTo(bestBuildPos);
    }
    
    // 到了位置，尝试变身
    if (troop.Position == bestBuildPos && AIConstructionPlanner.ShouldTransformNow(troop))
    {
        AIConstructionPlanner.ExecuteTransformation(troop);
    }
    return;
}
```

## 实际游戏代码集成点

### 1. 在Troop类的AI方法中集成

找到`Troop.cs`中的AI相关方法（如`AI()`或类似方法），添加：

```csharp
public void AI()
{
    // 现有的AI逻辑...
    
    // 新增：使用AI执行器
    if (Session.GlobalVariables.UseAdvancedAI)
    {
        TroopAIExecutor.ExecuteTurn(this);
        return;
    }
    
    // 原有的AI逻辑继续...
}
```

### 2. 在Faction类的回合方法中集成

找到`Faction.cs`中的`AI()`方法，在`AILegions()`后添加：

```csharp
public void AI()
{
    // 现有逻辑...
    this.AILegions();
    
    // 新增：智能部队AI
    this.AISmartTroops();
    
    // 其他逻辑...
}

private void AISmartTroops()
{
    // 使用AI势力回合集成系统
    AIFactionTurnIntegration.RunFactionTurn(this);
}
```

### 3. 在游戏主循环中集成

如果游戏有专门的AI回合处理，可以在那里调用：

```csharp
// 在AI势力的回合处理中
foreach (Faction faction in aiFactionsThisTurn)
{
    if (faction.UseAdvancedAI)
    {
        AIFactionTurnIntegration.RunFactionTurn(faction);
    }
    else
    {
        // 原有的AI逻辑
        faction.AI();
    }
}
```

## 配置和开关

### 1. 全局开关
在`Session.GlobalVariables`中添加：
```csharp
public static bool UseAdvancedAI = true;
public static bool UseTransportAI = true;
public static bool UseConstructionAI = true;
```

### 2. 势力级别开关
在`Faction`类中添加：
```csharp
public bool UseAdvancedAI { get; set; } = true;
```

### 3. 部队级别开关
在`Troop`类中添加：
```csharp
public bool UseAI { get; set; } = true;
```

## 性能优化建议

### 1. 分帧处理
```csharp
// 避免一帧内处理所有部队
private static int currentTroopIndex = 0;
private static List<Troop> troopsToProcess = new List<Troop>();

public static void ProcessTroopsGradually(Faction faction, int troopsPerFrame = 5)
{
    if (troopsToProcess.Count == 0)
    {
        troopsToProcess = faction.Troops.GetList();
        currentTroopIndex = 0;
    }
    
    int processed = 0;
    while (currentTroopIndex < troopsToProcess.Count && processed < troopsPerFrame)
    {
        TroopAIExecutor.ExecuteTurn(troopsToProcess[currentTroopIndex]);
        currentTroopIndex++;
        processed++;
    }
    
    if (currentTroopIndex >= troopsToProcess.Count)
    {
        troopsToProcess.Clear();
    }
}
```

### 2. 缓存优化
```csharp
// 缓存计算结果
private static Dictionary<int, List<Troop>> enemyCache = new Dictionary<int, List<Troop>>();
private static int lastCacheFrame = -1;

private static List<Troop> GetCachedEnemies(Faction faction)
{
    int currentFrame = Session.Current.Scenario.Date.Day;
    if (currentFrame != lastCacheFrame)
    {
        enemyCache.Clear();
        lastCacheFrame = currentFrame;
    }
    
    if (!enemyCache.ContainsKey(faction.ID))
    {
        enemyCache[faction.ID] = CalculateEnemies(faction);
    }
    
    return enemyCache[faction.ID];
}
```

## 调试和监控

### 1. 日志系统
```csharp
public static class AILogger
{
    public static bool EnableLogging = true;
    
    public static void Log(string message)
    {
        if (EnableLogging)
        {
            Console.WriteLine($"[AI] {DateTime.Now:HH:mm:ss} {message}");
        }
    }
}
```

### 2. 性能监控
```csharp
public static class AIPerformanceMonitor
{
    private static Dictionary<string, TimeSpan> timings = new Dictionary<string, TimeSpan>();
    
    public static void StartTiming(string operation)
    {
        // 记录开始时间
    }
    
    public static void EndTiming(string operation)
    {
        // 记录结束时间并统计
    }
    
    public static void PrintStats()
    {
        // 输出性能统计
    }
}
```

## 测试验证

### 1. 单元测试
```csharp
[Test]
public void TestTransportAI()
{
    var troop = CreateTestTransportTroop();
    var enemies = CreateTestEnemies();
    
    AILogisticsHandler.ExecuteTransportAI(troop, enemies, new List<Troop>());
    
    Assert.IsTrue(troop.Position != troop.PreviousPosition);
}
```

### 2. 集成测试
```csharp
[Test]
public void TestFullFactionTurn()
{
    var faction = CreateTestFaction();
    
    AIFactionTurnIntegration.RunFactionTurn(faction);
    
    // 验证所有部队都执行了AI
    foreach (var troop in faction.Troops.GetList())
    {
        Assert.IsTrue(troop.CurrentRole != AIRole.None);
    }
}
```

## 兼容性保证

### 1. 向后兼容
- 保留原有AI逻辑作为备选
- 通过开关控制新旧AI系统
- 渐进式迁移，避免破坏性更改

### 2. 存档兼容
- 新增的AI属性使用默认值
- 不修改现有数据结构
- 确保旧存档能正常加载

### 3. 模组兼容
- 提供扩展接口
- 允许模组覆盖AI行为
- 保持API稳定性

## 部署步骤

1. **准备阶段**
   - 备份现有代码
   - 创建测试环境
   - 准备回滚方案

2. **集成阶段**
   - 添加AI文件到项目
   - 在关键位置添加调用
   - 配置开关和参数

3. **测试阶段**
   - 单元测试验证
   - 集成测试验证
   - 性能测试验证

4. **部署阶段**
   - 默认关闭新AI系统
   - 逐步开放给测试用户
   - 收集反馈并优化

5. **维护阶段**
   - 监控性能指标
   - 收集用户反馈
   - 持续优化改进

## 总结

通过`TroopAIExecutor`提供的直接集成方案，可以将运输兵AI系统无缝集成到游戏的实际代码中。这种方案具有以下优势：

- **直接性**: 直接在部队回合中调用，无需复杂的中间层
- **高效性**: 避免重复计算，提高执行效率
- **灵活性**: 支持开关控制，可以渐进式部署
- **兼容性**: 保持与现有系统的兼容，降低风险
- **可维护性**: 模块化设计，便于后续扩展和维护

这个集成方案能够让AI系统真正融入游戏的核心逻辑，提供更智能和高效的AI行为。