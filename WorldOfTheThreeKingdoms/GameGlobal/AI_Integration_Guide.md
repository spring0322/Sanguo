# AI系统集成指南

## 概述

本指南将帮助你将新的AI战术评估系统集成到现有的中华三国志游戏中。

## 快速开始

### 1. 测试AI系统

首先，在游戏的任何地方调用测试方法来验证AI系统是否正常工作：

```csharp
// 在MainGame.cs或任何合适的地方添加
using GameGlobal;

// 快速测试
AISystemTestRunner.QuickTest();

// 或者运行完整测试
AISystemTestRunner.RunAllTests();
```

### 2. 基本集成

在现有的AI逻辑中添加新的决策系统：

```csharp
// 在现有的AI处理方法中
public void ProcessAITurn(Troop aiTroop)
{
    // 创建AI决策管理器（建议作为类成员变量）
    if (_aiDecisionManager == null)
        _aiDecisionManager = new AIDecisionManager();
    
    // 使用新的AI系统
    var decision = _aiDecisionManager.MakeCombatDecision(aiTroop, currentScenario);
    
    // 执行决策
    ExecuteAIDecision(aiTroop, decision);
}
```

## 详细集成步骤

### 步骤1: 适配现有数据结构

新的AI系统使用了一些简化的数据结构。你需要创建适配器来连接现有的游戏对象：

```csharp
// 创建适配器类
public static class GameObjectAdapter
{
    public static Troop ToAITroop(GameObjects.TroopDetail.Troop gameTroop)
    {
        return new Troop
        {
            ID = gameTroop.ID,
            Name = gameTroop.Name,
            CurrentHP = gameTroop.CurrentHP,
            MaxHP = gameTroop.MaxHP,
            Attack = gameTroop.Attack,
            Defense = gameTroop.Defense,
            CurrentPrestige = gameTroop.CurrentPrestige,
            MaxPrestige = gameTroop.MaxPrestige,
            Position = new Point(gameTroop.Position.X, gameTroop.Position.Y),
            AttackRange = gameTroop.AttackRange,
            BelongedFaction = ToAIFaction(gameTroop.BelongedFaction),
            AvailableSkills = GetTroopSkills(gameTroop),
            PersonId = gameTroop.PersonId
        };
    }
    
    public static GameScenario ToAIScenario(Scenario gameScenario)
    {
        var aiScenario = new GameScenario
        {
            CurrentTurn = gameScenario.CurrentTurn,
            Factions = gameScenario.Factions.GetList().Select(ToAIFaction).ToList(),
            Troops = new List<Troop>()
        };
        
        // 添加所有部队
        foreach (var troop in gameScenario.Troops.GetList())
        {
            aiScenario.AddTroop(ToAITroop(troop));
        }
        
        return aiScenario;
    }
}
```

### 步骤2: 技能系统集成

将现有的技能系统与新的AI系统连接：

```csharp
// 在适当的地方初始化技能系统
public void InitializeAISkillSystem()
{
    // 初始化默认技能配置
    SkillLearningSystem.InitializeDefaultSkills();
    
    // 为现有人物添加技能
    foreach (var person in scenario.Persons.GetList())
    {
        AssignSkillsToPerson(person);
    }
}

private void AssignSkillsToPerson(Person person)
{
    // 根据人物属性分配技能
    if (person.Intelligence >= 90)
    {
        SkillLearningSystem.LearnSkill(person.ID, 1001); // 火计
        SkillLearningSystem.LearnSkill(person.ID, 1003); // 混乱术
    }
    
    if (person.Politics >= 80)
    {
        SkillLearningSystem.LearnSkill(person.ID, 1002); // 治疗术
        SkillLearningSystem.LearnSkill(person.ID, 1005); // 鼓舞
    }
    
    // 根据特技添加特殊技能
    if (person.HasStunt("火神"))
    {
        SkillLearningSystem.LearnSkill(person.ID, 1001); // 火计
        SkillLearningSystem.LearnSkill(person.ID, 1004); // 雷击术
    }
}
```

### 步骤3: 战斗系统集成

将AI决策集成到现有的战斗系统中：

```csharp
public class EnhancedAIBattleManager
{
    private AIDecisionManager _aiDecisionManager;
    private AIIntegrationExample _aiIntegration;
    
    public EnhancedAIBattleManager()
    {
        _aiDecisionManager = new AIDecisionManager();
        _aiIntegration = new AIIntegrationExample();
    }
    
    public void ProcessAIBattleTurn(Troop aiTroop, Scenario scenario)
    {
        // 转换为AI系统格式
        var aiTroopData = GameObjectAdapter.ToAITroop(aiTroop);
        var aiScenario = GameObjectAdapter.ToAIScenario(scenario);
        
        // 使用AI系统处理回合
        _aiIntegration.ProcessAITurn(aiTroopData, aiScenario);
        
        // 将结果应用回游戏
        ApplyAIResults(aiTroop, aiTroopData);
    }
    
    private void ApplyAIResults(Troop gameTroop, Troop aiTroop)
    {
        // 同步AI决策的结果
        gameTroop.CurrentHP = aiTroop.CurrentHP;
        gameTroop.CurrentPrestige = aiTroop.CurrentPrestige;
        gameTroop.Position = new GameObjects.Point(aiTroop.Position.X, aiTroop.Position.Y);
    }
}
```

### 步骤4: 地形系统集成

连接地形系统以支持地形相关的战术评估：

```csharp
// 扩展GameScenario以支持地形查询
public partial class GameScenario
{
    private Scenario _gameScenario; // 引用原始游戏场景
    
    public TerrainKind GetTerrainKind(Point position)
    {
        // 根据实际的地形系统实现
        var terrain = _gameScenario.GetTerrain(position.X, position.Y);
        
        return terrain.Kind switch
        {
            GameObjects.TerrainDetail.TerrainKind.Forest => TerrainKind.Forest,
            GameObjects.TerrainDetail.TerrainKind.Mountain => TerrainKind.Mountain,
            GameObjects.TerrainDetail.TerrainKind.River => TerrainKind.River,
            GameObjects.TerrainDetail.TerrainKind.Plain => TerrainKind.Plain,
            _ => TerrainKind.Plain
        };
    }
}
```

## 高级集成

### 自定义评分权重

根据游戏平衡需要调整AI评分：

```csharp
// 创建配置类
public static class AIConfig
{
    public static float KillBonusMultiplier = 1.0f;      // 击杀奖励倍数
    public static float FriendlyFirePenalty = 1000f;    // 误伤惩罚
    public static float ResourceCostWeight = 1.0f;      // 资源消耗权重
    public static int AIRandomness = 20;                // AI随机性 (±值)
    
    // 根据难度调整参数
    public static void SetDifficulty(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                AIRandomness = 50;
                ResourceCostWeight = 0.5f;
                break;
            case DifficultyLevel.Normal:
                AIRandomness = 20;
                ResourceCostWeight = 1.0f;
                break;
            case DifficultyLevel.Hard:
                AIRandomness = 5;
                ResourceCostWeight = 1.5f;
                KillBonusMultiplier = 1.2f;
                break;
        }
    }
}
```

### 性能优化

对于大规模战斗，实施性能优化：

```csharp
public class OptimizedAIManager
{
    private readonly Dictionary<int, DateTime> _lastDecisionTime;
    private readonly TimeSpan _decisionCooldown = TimeSpan.FromSeconds(1);
    
    public bool ShouldMakeDecision(Troop troop)
    {
        if (!_lastDecisionTime.ContainsKey(troop.ID))
            return true;
            
        return DateTime.Now - _lastDecisionTime[troop.ID] > _decisionCooldown;
    }
    
    public void ProcessAIBatch(List<Troop> aiTroops, GameScenario scenario)
    {
        // 并行处理多个AI单位
        Parallel.ForEach(aiTroops.Where(ShouldMakeDecision), troop =>
        {
            var decision = _aiDecisionManager.MakeCombatDecision(troop, scenario);
            ExecuteDecision(troop, decision, scenario);
            _lastDecisionTime[troop.ID] = DateTime.Now;
        });
    }
}
```

## 调试和监控

### 添加AI决策日志

```csharp
public static class AILogger
{
    private static bool _loggingEnabled = true;
    
    public static void LogDecision(Troop troop, AIDecision decision)
    {
        if (!_loggingEnabled) return;
        
        var message = $"[AI] {troop.Name}: {decision.Action}";
        if (decision.Skill != null)
            message += $" - {decision.Skill.Name} -> {decision.Target?.Name}";
        message += $" (评分: {decision.Score:F1})";
        
        System.Diagnostics.Debug.WriteLine(message);
        
        // 可选：写入文件
        // File.AppendAllText("ai_decisions.log", message + Environment.NewLine);
    }
}
```

### 性能监控

```csharp
public class AIPerformanceMonitor
{
    private readonly List<double> _decisionTimes = new List<double>();
    
    public void RecordDecisionTime(TimeSpan elapsed)
    {
        _decisionTimes.Add(elapsed.TotalMilliseconds);
        
        // 保持最近1000次记录
        if (_decisionTimes.Count > 1000)
            _decisionTimes.RemoveAt(0);
    }
    
    public void PrintStatistics()
    {
        if (_decisionTimes.Count == 0) return;
        
        var avg = _decisionTimes.Average();
        var max = _decisionTimes.Max();
        var min = _decisionTimes.Min();
        
        Console.WriteLine($"AI性能统计 (最近{_decisionTimes.Count}次):");
        Console.WriteLine($"  平均决策时间: {avg:F2} ms");
        Console.WriteLine($"  最长决策时间: {max:F2} ms");
        Console.WriteLine($"  最短决策时间: {min:F2} ms");
    }
}
```

## 常见问题解决

### Q: 编译错误 - 找不到某些类型

A: 确保添加了正确的using语句：
```csharp
using GameGlobal;
using GameObjects;
using GameObjects.Influences;
```

### Q: AI决策太慢

A: 启用决策缓存和批处理：
```csharp
// 使用缓存
var decision = _aiDecisionManager.MakeCombatDecision(troop, scenario);

// 定期清理缓存
_aiDecisionManager.CleanupCache();
```

### Q: AI行为不够智能

A: 调整评分权重和添加更多战术考量：
```csharp
// 在CombatEvaluator中添加自定义评估逻辑
private static float EvaluateCustomTactics(Troop source, Troop target, GameScenario scenario)
{
    float bonus = 0;
    
    // 添加你的自定义战术逻辑
    if (IsFlankingPosition(source, target))
        bonus += 100;
        
    if (HasTerrainAdvantage(source, target, scenario))
        bonus += 50;
        
    return bonus;
}
```

## 测试验证

在集成完成后，运行以下测试来验证系统：

```csharp
public void ValidateIntegration()
{
    // 1. 基本功能测试
    AISystemTestRunner.QuickTest();
    
    // 2. 性能测试
    AISystemTestRunner.BenchmarkTest();
    
    // 3. 游戏集成测试
    TestGameIntegration();
}

private void TestGameIntegration()
{
    // 创建真实的游戏场景
    var scenario = Session.Current.Scenario;
    var aiTroop = scenario.Troops.GetRandomList(1)[0];
    
    // 测试AI决策
    var aiManager = new AIDecisionManager();
    var aiScenario = GameObjectAdapter.ToAIScenario(scenario);
    var aiTroopData = GameObjectAdapter.ToAITroop(aiTroop);
    
    var decision = aiManager.MakeCombatDecision(aiTroopData, aiScenario);
    
    Console.WriteLine($"集成测试成功: AI选择了 {decision.Action}");
}
```

## 总结

通过以上步骤，你可以将新的AI系统成功集成到现有游戏中。记住：

1. **渐进式集成** - 先在小范围测试，再全面应用
2. **保留后备方案** - 保持原有AI逻辑作为备用
3. **持续监控** - 使用日志和性能监控来优化系统
4. **用户反馈** - 根据玩家反馈调整AI行为

新的AI系统将为游戏带来更智能、更有挑战性的对手，提升整体游戏体验。