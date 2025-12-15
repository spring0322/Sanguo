# 🧠 你的 GhostUnit 实现分析与使用指南

## 📋 你的实现特点

### ✅ 优秀的设计

```csharp
[Serializable] // 支持存档 - 非常重要！
public class GhostUnit
{
    public int RealUnitID;       // 对应真实部队 ID
    public int FactionID;        // 所属势力
    public Point LastPosition;   // 最后一次看见的位置
    public int KnownStrength;    // 上次看见时的兵力
    public int LastSeenDay;      // 上次看见是哪一天
}
```

### 🎯 核心特性

1. **支持序列化存档** - `[Serializable]` 属性确保游戏可以保存/加载AI记忆
2. **使用 `Troop.Quantity`** - 正确使用了你的游戏中的兵力属性
3. **线性衰减算法** - 简单高效，每天衰减10%
4. **10天完全失效** - 清晰的过期策略

## 🔄 置信度算法对比

### 你的实现（线性衰减）
```csharp
public float GetConfidence(int currentDay)
{
    int daysPassed = currentDay - LastSeenDay;
    if (daysPassed <= 0) return 1.0f;
    float confidence = 1.0f - (daysPassed * 0.1f);
    return Math.Max(0f, confidence);
}
```

**特点：**
- 0天 = 1.0 (100% 可信)
- 1天 = 0.9 (90% 可信)
- 5天 = 0.5 (50% 可信)
- 10天 = 0.0 (完全失效)
- 计算速度快，逻辑直观

### 指数衰减（备选方案）
```csharp
// 如果你想要更平滑的衰减曲线
public float GetConfidenceExponential(int currentDay)
{
    int daysPassed = currentDay - LastSeenDay;
    if (daysPassed <= 0) return 1.0f;
    return (float)Math.Exp(-daysPassed * 0.2f);
}
```

**特点：**
- 0天 = 1.0 (100% 可信)
- 1天 = 0.82 (82% 可信)
- 5天 = 0.37 (37% 可信)
- 10天 = 0.14 (14% 可信)
- 衰减更平滑，但计算稍慢

## 📊 衰减曲线对比

```
天数 | 线性衰减 | 指数衰减
-----|---------|----------
  0  |  1.00   |  1.00
  1  |  0.90   |  0.82
  2  |  0.80   |  0.67
  3  |  0.70   |  0.55
  4  |  0.60   |  0.45
  5  |  0.50   |  0.37
  6  |  0.40   |  0.30
  7  |  0.30   |  0.25
  8  |  0.20   |  0.20
  9  |  0.10   |  0.17
 10  |  0.00   |  0.14
```

**建议：** 你的线性衰减算法非常适合策略游戏，因为：
- 玩家容易理解（每天减10%）
- 计算速度快
- 有明确的过期时间（10天）

## 🎮 完整使用流程

### 1. 初始化记忆系统

```csharp
// 在 Faction 类的构造函数中
public Faction()
{
    // ... 其他初始化代码
    
    // 初始化AI记忆系统
    MemoryMap = new AIMemoryMap();
}
```

### 2. 收集记忆（观察敌军）

```csharp
public void UpdateMemory(Faction aiFaction)
{
    int currentDay = Session.Current.Scenario.Date.Day;
    
    // 遍历AI势力的所有部队
    foreach (Troop observerTroop in aiFaction.Troops.GetList())
    {
        if (observerTroop?.Destroyed != false) continue;
        
        // 观察视野范围内的敌军（例如3格）
        var visibleEnemies = GetVisibleEnemies(observerTroop, aiFaction, 3);
        
        foreach (var enemy in visibleEnemies)
        {
            string memoryKey = $"{enemy.BelongedFaction.ID}_{enemy.ID}";
            
            if (aiFaction.MemoryMap.Values.ContainsKey(memoryKey))
            {
                // 更新现有记忆
                aiFaction.MemoryMap.Values[memoryKey].Update(enemy, currentDay);
            }
            else
            {
                // 创建新记忆
                var ghost = new GhostUnit(enemy, currentDay);
                aiFaction.MemoryMap.Values[memoryKey] = ghost;
            }
        }
    }
}
```

### 3. 基于记忆刷新影响力地图

```csharp
public void RefreshInfluenceMap(InfluenceMap map, Faction aiFaction)
{
    // 1. 清空重绘
    map.Clear();
    
    int currentDay = Session.Current.Scenario.Date.Day;
    
    // 2. 遍历记忆而非实时单位
    foreach (var ghost in aiFaction.MemoryMap.Values.Values)
    {
        // A. 计算置信度
        float confidence = ghost.GetConfidence(currentDay);
        
        // 跳过置信度为0的记忆
        if (confidence <= 0f) continue;
        
        // B. 计算影响力：兵力 × 置信度
        float strength = (ghost.KnownStrength / 100.0f) * confidence;
        
        // C. 敌我识别
        bool isEnemy = ghost.FactionID != aiFaction.ID;
        if (!isEnemy) strength *= -1; // 友军负值
        
        // D. 盖章到地图
        map.AddInfluence(ghost.LastPosition, strength, 3);
    }
}
```

### 4. 清理过期记忆

```csharp
public void CleanExpiredMemories(Faction aiFaction)
{
    int currentDay = Session.Current.Scenario.Date.Day;
    var keysToRemove = new List<string>();
    
    foreach (var kvp in aiFaction.MemoryMap.Values)
    {
        var ghost = kvp.Value;
        float confidence = ghost.GetConfidence(currentDay);
        
        // 清理置信度为0的记忆
        if (confidence <= 0f)
        {
            keysToRemove.Add(kvp.Key);
        }
    }
    
    foreach (var key in keysToRemove)
    {
        aiFaction.MemoryMap.Values.Remove(key);
    }
}
```

## 🎯 实战示例

### 示例1：威胁评估

```csharp
public float EvaluateThreat(Faction aiFaction, Point position)
{
    int currentDay = Session.Current.Scenario.Date.Day;
    float totalThreat = 0f;
    
    foreach (var ghost in aiFaction.MemoryMap.Values.Values)
    {
        float confidence = ghost.GetConfidence(currentDay);
        if (confidence <= 0.2f) continue; // 忽略低置信度情报
        
        // 计算距离
        int distance = Math.Abs(ghost.LastPosition.X - position.X) + 
                      Math.Abs(ghost.LastPosition.Y - position.Y);
        
        if (distance <= 5) // 5格内算威胁
        {
            // 威胁值 = 兵力 × 置信度 × 距离衰减
            float distanceDecay = Math.Max(0.2f, 1.0f - (distance * 0.15f));
            float threat = (ghost.KnownStrength / 100.0f) * confidence * distanceDecay;
            totalThreat += threat;
        }
    }
    
    return totalThreat;
}
```

### 示例2：寻找最安全的位置

```csharp
public Point FindSafestPosition(Faction aiFaction, Rectangle searchArea)
{
    Point safestPos = new Point(searchArea.X, searchArea.Y);
    float lowestThreat = float.MaxValue;
    
    for (int x = searchArea.Left; x < searchArea.Right; x++)
    {
        for (int y = searchArea.Top; y < searchArea.Bottom; y++)
        {
            Point candidate = new Point(x, y);
            float threat = EvaluateThreat(aiFaction, candidate);
            
            if (threat < lowestThreat)
            {
                lowestThreat = threat;
                safestPos = candidate;
            }
        }
    }
    
    return safestPos;
}
```

### 示例3：记忆统计

```csharp
public void PrintMemoryStats(Faction aiFaction)
{
    int currentDay = Session.Current.Scenario.Date.Day;
    
    int totalMemories = aiFaction.MemoryMap.Values.Count;
    int freshMemories = 0;  // 0-3天
    int oldMemories = 0;    // 4-7天
    int staleMemories = 0;  // 8-10天
    
    foreach (var ghost in aiFaction.MemoryMap.Values.Values)
    {
        int age = currentDay - ghost.LastSeenDay;
        
        if (age <= 3) freshMemories++;
        else if (age <= 7) oldMemories++;
        else staleMemories++;
    }
    
    Console.WriteLine($"势力 {aiFaction.Name} 记忆统计:");
    Console.WriteLine($"  总记忆数: {totalMemories}");
    Console.WriteLine($"  新鲜情报 (0-3天): {freshMemories}");
    Console.WriteLine($"  陈旧情报 (4-7天): {oldMemories}");
    Console.WriteLine($"  过期情报 (8-10天): {staleMemories}");
}
```

## ⚡ 性能优化建议

### 1. 定期清理
```csharp
// 每30帧清理一次过期记忆
if (frameCount % 30 == 0)
{
    CleanExpiredMemories(aiFaction);
}
```

### 2. 批量更新
```csharp
// 不要每帧都更新记忆，每N帧更新一次
if (frameCount % 20 == 0)
{
    UpdateMemory(aiFaction);
}
```

### 3. 空间索引（可选）
```csharp
// 如果记忆数量很大，可以考虑按区域索引
public class SpatialMemoryIndex
{
    private Dictionary<Point, List<GhostUnit>> _grid;
    
    public void AddMemory(GhostUnit ghost)
    {
        Point gridCell = new Point(ghost.LastPosition.X / 10, ghost.LastPosition.Y / 10);
        if (!_grid.ContainsKey(gridCell))
            _grid[gridCell] = new List<GhostUnit>();
        _grid[gridCell].Add(ghost);
    }
    
    public List<GhostUnit> GetNearbyMemories(Point position, int range)
    {
        // 只查询附近的格子，而不是遍历所有记忆
        // ...
    }
}
```

## 🎉 总结

你的 `GhostUnit` 实现非常优秀：

✅ **支持存档** - `[Serializable]` 属性
✅ **简单高效** - 线性衰减算法
✅ **清晰的过期策略** - 10天完全失效
✅ **易于理解** - 代码简洁明了
✅ **性能良好** - 适合大规模使用

### 与AI决策系统的完美配合

```csharp
// 你的三步式AI架构
public void RunAILogic(Faction faction)
{
    // 1. 更新记忆 (使用你的 GhostUnit)
    UpdateMemory(faction);
    
    // 2. 刷新势能图 (基于 GhostUnit.GetConfidence)
    RefreshInfluenceMap(influenceMap, faction);
    
    // 3. 执行智能移动 (基于势能图决策)
    ExecuteSmartMove(faction);
}
```

现在你有了一个完整的、基于记忆驱动的AI决策系统！🎉

## 📚 相关文件

- `GhostUnit.cs` - 你的原始实现
- `Complete_AI_Decision_System.cs` - 完整AI决策系统（已适配你的GhostUnit）
- `GhostUnit_Usage_Example.cs` - 详细使用示例
- `AI_Decision_System_Integration_Guide.md` - 集成指南

## 🔧 下一步建议

1. ✅ 在 `Faction` 类中添加 `MemoryMap` 属性
2. ✅ 在游戏主循环中集成AI决策系统
3. ✅ 测试记忆收集和衰减是否正常工作
4. ✅ 根据游戏平衡性调整参数（视野范围、衰减速度等）
5. ✅ 添加可视化调试工具（在地图上显示记忆位置）
