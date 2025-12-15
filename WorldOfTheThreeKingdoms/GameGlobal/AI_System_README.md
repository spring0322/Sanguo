# 中华三国志 AI 战术评估系统

## 概述

这是一个为中华三国志游戏设计的智能AI战术评估系统，基于数据驱动的设计理念，能够为AI提供智能的战斗决策。

## 系统架构

### 核心组件

1. **CombatEvaluator** - 战术评估器
   - 负责评估技能使用的价值
   - 支持复杂的战术计算
   - 考虑地形、装备、状态等因素

2. **AIDecisionManager** - AI决策管理器
   - 整合战术评估器
   - 提供完整的AI决策流程
   - 支持决策缓存优化

3. **InfluenceDefinitions** - 技能影响定义
   - 定义各种技能效果类型
   - 支持数据驱动的技能设计

4. **SkillDefinitions** - 技能定义系统
   - 预定义的技能模板
   - 技能学习和管理系统

## 使用方法

### 1. 基本使用

```csharp
// 创建AI决策管理器
var aiManager = new AIDecisionManager();

// 为AI部队做决策
var decision = aiManager.MakeCombatDecision(aiTroop, scenario);

// 执行决策
switch (decision.Action)
{
    case AIActionType.UseSkill:
        // 使用技能
        ExecuteSkill(decision.Skill, decision.Target);
        break;
    case AIActionType.Move:
        // 移动
        MoveTroop(aiTroop, decision.TargetPosition);
        break;
    // ... 其他行动
}
```

### 2. 技能评估

```csharp
// 直接评估技能价值
float score = CombatEvaluator.EvaluateSkill(
    source: myTroop,
    skill: fireAttack,
    primaryTarget: enemyTroop,
    scenario: currentScenario
);

// 分数越高，技能使用价值越大
if (score > 100)
{
    // 这是一个很好的技能使用时机
}
```

### 3. 自定义技能

```csharp
// 创建自定义技能
var customSkill = new Skill
{
    ID = 2001,
    Name = "自定义火球术",
    Cost = 25,
    Power = 1.8f,
    Range = 4,
    Radius = 1,
    Influences = new List<Influence>
    {
        new DamageInfluence(1.8f, isFire: true),
        new StatusInfluence(StatusKind.Burn, 3)
    }
};
```

## 评分系统

### 伤害评估
- **基础伤害**: 根据攻击力和防御力计算
- **有效伤害**: 只计算实际能造成的伤害（不超过目标血量）
- **特殊加成**: 火计+森林、藤甲等特殊组合

### 治疗评估
- **治疗量**: 实际能恢复的血量
- **紧急度**: 血量越少，治疗价值越高
- **目标价值**: 英雄优先治疗

### 控制技能评估
- **基础价值**: 控制敌人的基础分数
- **打断加成**: 打断敌人施法的额外分数
- **重复惩罚**: 避免对已控制的敌人重复使用

### 战术修正
- **斩杀线**: 能够击杀敌人时的额外分数
- **过度伤害**: 伤害溢出时的轻微扣分
- **资源消耗**: 根据气力消耗比例扣分
- **位置优势**: 地形和包围优势加分

## 配置参数

### 评分权重
```csharp
// 在CombatEvaluator中可以调整这些参数
private const float KILL_BONUS = 200f;        // 击杀奖励
private const float HERO_BONUS = 300f;        // 英雄击杀奖励
private const float FRIENDLY_FIRE_PENALTY = 1000f; // 误伤惩罚
private const float OVERKILL_PENALTY = 50f;   // 过度伤害惩罚
```

### AI难度调整
```csharp
// 通过调整随机性来控制AI难度
// 在AIDecisionManager.CalculateBestDecision中
score += _random.Next(-20, 21); // 简单AI: ±50
score += _random.Next(-10, 11); // 普通AI: ±20  
score += _random.Next(-5, 6);   // 困难AI: ±10
```

## 扩展指南

### 添加新的影响类型

1. 在`InfluenceKind`枚举中添加新类型
2. 在`CombatEvaluator.EvaluateInfluence`中添加处理逻辑
3. 在`AIIntegrationExample.ApplyInfluence`中添加执行逻辑

### 添加新的战术考量

1. 在`CombatEvaluator.EvaluateSkill`中添加新的评估逻辑
2. 创建专门的评估方法
3. 调整评分权重

### 性能优化

1. **缓存系统**: 决策结果会自动缓存5秒
2. **批量计算**: 可以批量评估多个技能
3. **早期退出**: 明显不可行的选项会提前跳过

## 调试功能

### 日志输出
系统会输出详细的调试日志，包括：
- AI决策过程
- 技能评分详情
- 执行结果

### 评分分析
```csharp
// 启用详细评分日志
CombatEvaluator.EnableDetailedLogging = true;

// 查看评分细节
var scoreBreakdown = CombatEvaluator.GetScoreBreakdown(skill, target);
```

## 集成到现有系统

### 1. 替换现有AI逻辑
```csharp
// 在现有的AI回合处理中
public void ProcessAITurn(Troop aiTroop)
{
    var aiIntegration = new AIIntegrationExample();
    aiIntegration.ProcessAITurn(aiTroop, currentScenario);
}
```

### 2. 渐进式集成
- 可以先在特定情况下使用新AI
- 保留原有AI作为后备方案
- 逐步扩展到所有AI行为

### 3. 兼容性考虑
- 系统设计为与现有代码兼容
- 不会破坏现有的游戏逻辑
- 可以选择性启用新功能

## 常见问题

### Q: AI会不会太聪明？
A: 可以通过调整随机性和评分权重来控制AI难度。

### Q: 性能影响如何？
A: 系统使用了缓存和优化算法，性能影响很小。

### Q: 如何添加新技能？
A: 使用SkillFactory创建新技能，或直接实例化Skill类。

### Q: 能否自定义AI行为？
A: 可以通过修改评分逻辑或添加新的决策类型来自定义。

## 未来扩展

1. **机器学习**: 可以收集游戏数据训练更智能的AI
2. **多层决策**: 支持战略层面的长期规划
3. **协作AI**: 多个AI单位之间的协调作战
4. **动态难度**: 根据玩家水平自动调整AI强度

## 技术支持

如有问题或建议，请查看代码注释或联系开发团队。

---

**版本**: 1.0  
**更新日期**: 2024年12月18日  
**兼容性**: 中华三国志 v2.0+