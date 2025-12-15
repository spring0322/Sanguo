# 智能AI系统快速参考指南

## 🚀 5分钟快速开始

### 1. 创建智能部队
```csharp
var smartTroop = new SmartTroop
{
    Name = "诸葛亮",
    CurrentHP = 100,
    MaxHP = 100,
    Attack = 60,
    Defense = 40,
    Intelligence = 95,
    Position = new Point(10, 10),
    Mobility = 3,
    AvailableSkills = new List<Skill>
    {
        SkillFactory.CreateFireAttack(),
        SkillFactory.CreateConfusion()
    }
};
```

### 2. 执行智能回合
```csharp
smartTroop.ExecuteSmartTurn();
// AI会自动：
// 1. 更新记忆和势能图
// 2. 评估所有可能的移动+技能组合
// 3. 执行最优行动
```

### 3. 使用AI管理器（推荐）
```csharp
var aiManager = new SmartAIManager();
aiManager.RegisterSmartTroop(smartTroop, AIPersonality.CreateBalanced());
aiManager.ExecuteAITurns(scenario);
```

## 📋 常用API

### SmartTroop类
```csharp
// 执行智能回合
troop.ExecuteSmartTurn();

// 重置AI状态
troop.ResetAIState();

// 手动移动
troop.MoveTo(new Point(x, y));

// 使用技能
troop.CastSkill(skill, target);

// 普通攻击
troop.Attack(target);
```

### SmartAIManager类
```csharp
// 注册部队
aiManager.RegisterSmartTroop(troop, personality);

// 移除部队
aiManager.UnregisterSmartTroop(troopId);

// 执行所有AI回合
aiManager.ExecuteAITurns(scenario);

// 设置难度
aiManager.SetDifficulty(AIDifficulty.Hard);

// 获取统计
var stats = aiManager.GetStatistics();
```

### 技能工厂
```csharp
// 创建预定义技能
var fireAttack = SkillFactory.CreateFireAttack();
var heal = SkillFactory.CreateHeal();
var confusion = SkillFactory.CreateConfusion();
var thunder = SkillFactory.CreateThunderStrike();
var inspire = SkillFactory.CreateInspire();
var freeze = SkillFactory.CreateFreeze();
var massHeal = SkillFactory.CreateMassHeal();

// 获取所有技能
var allSkills = SkillFactory.GetAllSkills();

// 根据ID获取技能
var skill = SkillFactory.GetSkillById(1001);
```

### AI个性
```csharp
// 预定义个性
var aggressive = AIPersonality.CreateAggressive();
var defensive = AIPersonality.CreateDefensive();
var balanced = AIPersonality.CreateBalanced();

// 自定义个性
var custom = new AIPersonality
{
    Aggressiveness = 0.7f,
    Caution = 0.5f,
    Teamwork = 0.8f,
    Creativity = 0.6f,
    ResourceManagement = 0.7f
};
```

## 🎯 核心概念

### 战斗计划结构
```csharp
public struct CombatPlan
{
    public Point MoveDestination;      // 移动目标
    public Skill SkillToCast;         // 要使用的技能（null=普攻）
    public Troop Target;              // 攻击目标
    public float Score;               // 计划评分
    public CombatActionType ActionType; // 行动类型
}
```

### 行动类型
```csharp
public enum CombatActionType
{
    Wait,           // 等待
    Move,           // 纯移动
    BasicAttack,    // 普通攻击
    UseSkill,       // 使用技能
    Defend          // 防御
}
```

### AI难度
```csharp
public enum AIDifficulty
{
    Easy,      // 简单 - 降低AI智能度
    Normal,    // 普通 - 标准AI行为
    Hard,      // 困难 - 增强AI能力
    Expert     // 专家 - 最高AI智能度
}
```

## 🔧 配置选项

### AI配置
```csharp
aiManager.Config.EnableParallelProcessing = true;  // 启用并行处理
aiManager.Config.ParallelThreshold = 10;           // 并行处理阈值
aiManager.Config.MinParallelDistance = 5;          // 最小并行距离
aiManager.Config.EnableAdvancedTactics = true;     // 启用高级战术
aiManager.Config.EnableCoordination = true;        // 启用协调
```

## 🧪 测试方法

### 快速测试
```csharp
// 测试AI系统基本功能
AISystemTestRunner.QuickTest();

// 测试单个智能部队
SmartAIExample.TestSingleSmartTroop();

// 测试个性系统
SmartAIExample.TestPersonalitySystem();

// 压力测试
SmartAIExample.StressTest();

// 运行所有测试
SmartAIExample.RunAllTests();
```

### 完整演示
```csharp
// 运行完整的战斗演示
SmartAIExample.RunCompleteDemo();
```

## 📊 性能监控

### 获取统计信息
```csharp
var stats = aiManager.GetStatistics();
Console.WriteLine($"总部队数: {stats.TotalTroops}");
Console.WriteLine($"存活部队: {stats.ActiveTroops}");
Console.WriteLine($"平均决策时间: {stats.AverageDecisionTime:F2} ms");
Console.WriteLine($"总决策次数: {stats.TotalDecisions}");
```

### 性能统计
```csharp
var perfStats = aiManager.GetPerformanceStats();
Console.WriteLine($"平均回合时间: {perfStats.AverageTurnTime:F2} ms");
Console.WriteLine($"最长回合时间: {perfStats.MaxTurnTime:F2} ms");
Console.WriteLine($"最短回合时间: {perfStats.MinTurnTime:F2} ms");
```

## 🎮 实战示例

### 示例1: 创建英雄部队
```csharp
var zhuge = new SmartTroop
{
    ID = 1,
    Name = "诸葛亮",
    PersonId = 1,
    CurrentHP = 100,
    MaxHP = 100,
    Attack = 60,
    Defense = 40,
    Intelligence = 95,
    CurrentPrestige = 80,
    MaxPrestige = 100,
    Position = new Point(10, 10),
    AttackRange = 2,
    Mobility = 3,
    AvailableSkills = new List<Skill>
    {
        SkillFactory.CreateFireAttack(),
        SkillFactory.CreateConfusion(),
        SkillFactory.CreateInspire()
    }
};

// 设置谨慎型个性
var personality = new AIPersonality
{
    Aggressiveness = 0.3f,
    Caution = 0.8f,
    Teamwork = 0.9f,
    Creativity = 0.9f,
    ResourceManagement = 0.8f
};

aiManager.RegisterSmartTroop(zhuge, personality);
```

### 示例2: 批量创建部队
```csharp
for (int i = 0; i < 10; i++)
{
    var troop = new SmartTroop
    {
        ID = i + 1,
        Name = $"部队{i + 1}",
        CurrentHP = 80,
        MaxHP = 80,
        Attack = 40 + i * 2,
        Defense = 30,
        Position = new Point(i, 5),
        Mobility = 2,
        AvailableSkills = new List<Skill>
        {
            SkillFactory.CreateFireAttack()
        }
    };
    
    aiManager.RegisterSmartTroop(troop);
}
```

### 示例3: 战斗循环
```csharp
for (int turn = 1; turn <= 10; turn++)
{
    Console.WriteLine($"=== 第 {turn} 回合 ===");
    
    // 执行AI回合
    aiManager.ExecuteAITurns(scenario);
    
    // 检查战斗结束条件
    if (IsBattleOver())
        break;
}
```

## 🐛 常见问题

### Q: 如何让AI更激进？
```csharp
var aggressive = AIPersonality.CreateAggressive();
aiManager.RegisterSmartTroop(troop, aggressive);
```

### Q: 如何禁用并行处理？
```csharp
aiManager.Config.EnableParallelProcessing = false;
```

### Q: 如何添加自定义技能？
```csharp
var customSkill = new Skill
{
    ID = 2001,
    Name = "自定义技能",
    Cost = 30,
    Power = 1.5f,
    Range = 3,
    Radius = 1,
    Influences = new List<Influence>
    {
        new DamageInfluence(1.5f, isFire: true)
    }
};

troop.AvailableSkills.Add(customSkill);
```

### Q: 如何调试AI决策？
```csharp
// AI会自动输出调试日志到Debug窗口
// 查看 Visual Studio 的输出窗口
System.Diagnostics.Debug.WriteLine("[SmartTroop] 决策信息");
```

## 📚 相关文档

- **完整开发报告**: `智能AI系统完整开发报告.md`
- **系统说明**: `WorldOfTheThreeKingdoms/GameGlobal/AI_System_README.md`
- **集成指南**: `WorldOfTheThreeKingdoms/GameGlobal/AI_Integration_Guide.md`

## 🎯 最佳实践

1. **使用AI管理器**: 推荐使用`SmartAIManager`而不是直接调用`ExecuteSmartTurn()`
2. **设置个性**: 为不同类型的部队设置不同的AI个性
3. **监控性能**: 定期检查性能统计，优化配置
4. **渐进集成**: 先在小范围测试，再全面应用
5. **保留备份**: 保留原有AI系统作为后备方案

## 💡 提示

- 智能部队会自动避免友军误伤
- AI会优先攻击残血敌人
- 技能使用会考虑气力消耗
- 移动会考虑地形和战略价值
- 支持大规模战斗场景（20+部队）

---

**快速参考版本**: 1.0  
**更新日期**: 2024年12月18日