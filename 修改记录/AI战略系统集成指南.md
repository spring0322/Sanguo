# AI战略系统集成指南 - StrategicBrain版

## 系统概述

这个AI战略系统为《中华三国志》提供了智能的AI决策框架，核心是**StrategicBrain**类，它能够像真正的君主一样进行战略思考。

## 核心特性

### 🧠 StrategicBrain - 战略大脑
```csharp
public class StrategicBrain 
{
    // 核心函数：输入当前数据，直接输出AI这回合要干嘛
    public StrategicStance DetermineStance(FactionProfile profile, ResourceSnapshot resources, FactionNeighbors neighbors)
}
```

**决策逻辑**（按优先级）：
1. **疲劳熔断** - 全军疲劳>70%强制休养
2. **危机判定** - 兵力<最强邻居20%进入危机
3. **机会主义** - 军师聪明+有脆弱目标=偷袭
4. **性格随机** - 袁绍式优柔寡断可能发呆
5. **扩张vs防御** - 综合评分决定

### 📊 三大数据源

**1. FactionProfile (势力画像)**
- RulerAggression: 君主激进度
- AdvisorWisdom: 军师智力修正  
- GeneralWarPressure: 武将好战压力
- Decisiveness: 决策稳定性

**2. ResourceSnapshot (资源快照)**
- TotalMilitaryPower: 总军事力量
- AverageFatigue: 平均疲劳度
- EconomicHealth: 经济健康度
- ThreatLevel/OpportunityLevel: 威胁/机会等级

**3. FactionNeighbors (邻居分析)** - 新增！
- StrongestNeighborPower: 最强邻居实力
- AveragePower: 邻居平均实力
- HasVulnerableTarget: 是否有脆弱目标
- NeighborCount: 邻居数量

## 集成步骤

### 第一步：初始化系统
```csharp
// 在Session或MainGameScreen中添加
public static AIStrategicManager AIStrategicManager { get; private set; }

// 游戏初始化时
AIStrategicManager = new AIStrategicManager();
```

### 第二步：回合循环集成
```csharp
public void OnTurnStart()
{
    // 每回合更新AI战略决策
    AIStrategicManager?.UpdateStrategicDecisions();
    
    // 清理已灭亡势力数据
    AIStrategicManager?.CleanupDestroyedFactions();
}
```

### 第三步：AI行动集成
```csharp
public void ExecuteAIActions(Faction faction)
{
    var stance = AIStrategicManager.GetFactionStance(faction.ID);
    var profile = AIStrategicManager.GetFactionProfile(faction.ID);
    
    switch (stance)
    {
        case StrategicStance.Expansion:
            // 执行扩张：招募、训练、攻击
            break;
        case StrategicStance.Opportunistic:
            // 执行机会主义：偷袭弱势邻居
            break;
        case StrategicStance.Crisis:
            // 执行危机处理：求和、迁都
            break;
        // ... 其他姿态
    }
}
```

## 智能特性展示

### 🎯 决策推理系统
系统会记录每个决策的推理过程：
```
[AI战略] 刘备 战略姿态变更: Defense -> Opportunistic
[AI战略] 决策推理: 军师智力0.95，发现脆弱目标，采取机会主义
```

### 🏛️ 历史君主特性
- **曹操**: 激进扩张 (高决策力0.9 + 高激进0.9)
- **刘备**: 稳健发展 (军师智力0.9 + 中等激进0.6)  
- **袁绍**: 优柔寡断 (低决策力0.3 + 高武将压力0.8)
- **孙权**: 机会主义 (平衡发展 + 伺机而动)

### 🔍 邻居分析系统
```csharp
var neighbors = FactionNeighbors.AnalyzeNeighbors(faction);
// 自动分析：
// - 谁是最强邻居？
// - 谁正在打仗可以偷袭？
// - 谁经济虚弱可以吞并？
```

## 调试功能

### 详细日志
```csharp
AIStrategicConfig.EnableDetailedDecisionLog = true;
```
输出示例：
```
[AI战略详情] === 曹操 ===
  君主: 曹操
  军师: 荀彧  
  战略姿态: 全力扩张 - 积极进攻，开疆拓土
  势力画像:
    君主激进度: 0.85
    军师智力: 0.88
    武将压力: 0.72
    决策稳定性: 0.91
  邻居分析:
    邻居数量: 3
    最强邻居: 15000
    脆弱目标: 有
  决策推理: 扩张评分: 0.73 (君主0.85 + 武将压力0.72 + 经济0.65); 评分超过0.6，选择扩张
```

### 战略概览
```csharp
var overview = AIStrategicManager.GetStrategicOverview();
// 返回所有AI势力的战略状态摘要
```

## 性能优化

- **缓存机制**: 势力画像和资源快照缓存
- **冷却系统**: 战略变更3回合冷却，避免频繁切换
- **增量计算**: 只在必要时重新计算邻居关系
- **自动清理**: 已灭亡势力数据自动清理

## 扩展指南

### 添加新的决策因素
```csharp
// 在StrategicBrain.DetermineStance中添加新的判定逻辑
if (profile.某个新属性 > 阈值 && resources.某个新条件)
{
    return StrategicStance.新姿态;
}
```

### 自定义历史君主
```csharp
// 在AIStrategicConfig.GetHistoricalProfile中添加
case "新君主":
    return new FactionProfile 
    {
        RulerAggression = 0.8f,
        AdvisorWisdom = 0.7f,
        GeneralWarPressure = 0.6f,
        Decisiveness = 0.9f
    };
```

## 使用示例

```csharp
// 手动测试某个势力的战略决策
var profile = FactionProfile.CalculateProfile(faction);
var snapshot = ResourceSnapshot.CalculateSnapshot(faction);  
var neighbors = FactionNeighbors.AnalyzeNeighbors(faction);

var brain = new StrategicBrain();
var stance = brain.DetermineStance(profile, snapshot, neighbors);
var reasoning = brain.GetStanceReasoning(profile, snapshot, neighbors, stance);

Console.WriteLine($"{faction.Name} 选择 {stance}: {reasoning}");
```

---
**更新日期**: 2024年12月22日  
**版本**: v2.0 - StrategicBrain集成版  
**新特性**: 邻居分析、决策推理、性能优化