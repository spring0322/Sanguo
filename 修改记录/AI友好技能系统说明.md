# AI友好技能系统说明

## 🎯 系统概述

这个增强的技能系统专门为AI决策优化，让AI能够智能地理解、评估和使用技能。系统保持了原有的数据结构，同时添加了大量AI友好的功能。

## 🏗️ 核心架构

### **1. 数据结构层次**
```
Skill (技能)
├── 原有属性 (ID, Name, Description, InfluencesString, Effects)
├── AIProfile (AI档案) - 让AI快速理解技能特性
├── Conditions (使用条件) - AI判断何时使用
└── Usage (使用限制) - 冷却和次数管理

Influence (效果)
├── 原有属性 (ID, Name, Parameter, Kind)
└── AI增强方法 (GetNumericValue, IsPositiveEffect)

InfluenceKind (效果类型)
├── 原有属性 (ID, Name, Type)
└── AI增强属性 (Category, TargetType, AIWeight)
```

## 🧠 AI智能特性

### **1. AI技能档案 (AISkillProfile)**
为每个技能提供AI可读的"简历"：

```csharp
public class AISkillProfile
{
    public SkillCategory Category { get; set; }           // 技能分类
    public List<SkillScenario> BestScenarios { get; set; } // 最佳使用场景
    public int Priority { get; set; }                     // 使用优先级
    
    // 四维价值评估
    public float OffensiveValue { get; set; }    // 攻击价值
    public float DefensiveValue { get; set; }    // 防御价值  
    public float UtilityValue { get; set; }      // 辅助价值
    public float StrategicValue { get; set; }    // 战略价值
    
    public string AIDescription { get; set; }    // AI可读描述
}
```

**价值评估示例：**
- **火球术**：攻击价值50，防御价值0，辅助价值0，战略价值10
- **治疗术**：攻击价值0，防御价值60，辅助价值20，战略价值5
- **群体增益**：攻击价值10，防御价值10，辅助价值40，战略价值30

### **2. 智能使用条件 (SkillCondition)**
AI可以自动判断技能使用条件：

```csharp
// 示例：低血量时使用治疗
new SkillCondition 
{ 
    Type = ConditionType.OwnHP, 
    Parameter = "0.3", 
    Operator = ComparisonOperator.LessThan 
}

// 示例：敌人数量大于2时使用群攻
new SkillCondition 
{ 
    Type = ConditionType.EnemyCount, 
    Parameter = "2", 
    Operator = ComparisonOperator.GreaterThan 
}
```

### **3. 场景匹配系统**
AI根据当前战斗情况选择最合适的技能：

```csharp
public enum SkillScenario
{
    LowHP,          // 低血量时 - 优先治疗技能
    Outnumbered,    // 被围攻时 - 优先控制/逃跑技能
    GroupFight,     // 群战时 - 优先群攻技能
    OneVsOne,       // 单挑时 - 优先单体高伤害技能
    Advantage,      // 优势时 - 优先攻击技能
    Retreat,        // 撤退时 - 优先移动/防御技能
    Ambush          // 伏击时 - 优先爆发技能
}
```

## 🎮 AI决策流程

### **1. 技能评估流程**
```
1. 检查使用条件 → 不满足则评分为0
2. 计算基础价值 → 根据当前情况选择价值类型
3. 场景匹配度 → 匹配场景获得奖励分数
4. 效果价值评估 → 分析每个效果的实际价值
5. 紧急度修正 → 根据战斗紧急程度调整分数
```

### **2. 智能价值计算**
AI会根据当前情况动态调整技能价值：

```csharp
// 危险时：防御价值 × 2.0
if (context.IsInDanger())
    value += profile.DefensiveValue * 2.0f;

// 优势时：攻击价值 × 1.5  
else if (context.HasAdvantage())
    value += profile.OffensiveValue * 1.5f;

// 平衡时：辅助价值 × 1.2
else
    value += profile.UtilityValue * 1.2f;
```

### **3. 效果智能评估**
不同效果在不同情况下的价值不同：

```csharp
switch (effect.Kind.Category)
{
    case EffectCategory.Damage:
        // 战斗中伤害价值提升50%
        return value * weight * (context.IsInCombat() ? 1.5f : 1.0f);
        
    case EffectCategory.Healing:
        // 低血量时治疗价值翻倍
        return value * weight * (context.GetOwnHPRatio() < 0.5f ? 2.0f : 0.5f);
        
    case EffectCategory.Control:
        // 被围攻时控制价值提升50%
        return value * weight * (context.IsOutnumbered() ? 1.5f : 1.0f);
}
```

## 🔧 使用方法

### **1. 基础使用**
```csharp
// 创建技能管理器
var skillManager = new AISkillManager();

// 加载技能数据
skillManager.LoadSkills(allSkills);

// 创建战斗上下文
var context = new BattleContext
{
    CurrentTurn = 5,
    OwnHP = 600,
    MaxHP = 1000,
    EnemyCount = 2,
    AllyCount = 1,
    InCombat = true
};

// AI选择最佳技能
var bestSkill = skillManager.SelectBestSkill(availableSkillIds, context);
```

### **2. 自定义技能档案**
```csharp
var skill = new Skill
{
    ID = 1,
    Name = "雷电术",
    AIProfile = new AISkillProfile
    {
        Category = SkillCategory.Attack,
        OffensiveValue = 70f,
        DefensiveValue = 0f,
        UtilityValue = 10f,
        StrategicValue = 20f,
        Priority = 80,
        BestScenarios = new List<SkillScenario> 
        { 
            SkillScenario.GroupFight, 
            SkillScenario.Advantage 
        },
        AIDescription = "强力群攻技能，适合对付多个敌人"
    },
    Conditions = new List<SkillCondition>
    {
        new SkillCondition 
        { 
            Type = ConditionType.EnemyCount, 
            Parameter = "2", 
            Operator = ComparisonOperator.GreaterOrEqual 
        }
    },
    Usage = new SkillUsage 
    { 
        Cooldown = 5, 
        MaxUses = 3 
    }
};
```

## 📊 AI决策示例

### **场景1：低血量危机**
```
战斗上下文：
- 自己血量：200/1000 (20%)
- 敌人数量：2
- 是否危险：是

AI评估结果：
- 治疗术：防御价值60 × 2.0(危险修正) = 120分
- 火球术：攻击价值50 × 1.0 = 50分
- 选择：治疗术 ✓
```

### **场景2：群战优势**
```
战斗上下文：
- 自己血量：900/1000 (90%)
- 敌人数量：3
- 友军数量：2
- 是否优势：是

AI评估结果：
- 群攻技能：攻击价值80 × 1.5(优势修正) + 20(场景匹配) = 140分
- 单体攻击：攻击价值60 × 1.5 = 90分
- 选择：群攻技能 ✓
```

### **场景3：一对一决斗**
```
战斗上下文：
- 敌人数量：1
- 友军数量：1
- 场景：OneVsOne

AI评估结果：
- 高伤害单体技能：攻击价值90 + 20(场景匹配) = 110分
- 群攻技能：攻击价值80 - 10(场景不匹配) = 70分
- 选择：高伤害单体技能 ✓
```

## 🎯 系统优势

### **1. AI友好性**
- **快速决策**：预计算的价值评估，无需复杂分析
- **情境感知**：根据战斗情况动态调整策略
- **学习能力**：可以通过调整权重来优化AI行为

### **2. 扩展性**
- **新技能**：自动生成AI档案，无需手动配置
- **新场景**：轻松添加新的使用场景
- **新条件**：灵活的条件系统支持各种判断

### **3. 平衡性**
- **多维评估**：攻击、防御、辅助、战略四维平衡
- **动态权重**：根据情况调整技能价值
- **使用限制**：冷却和次数限制防止滥用

## 🎮 游戏集成

### **1. 与现有系统集成**
```csharp
// 创建集成器
var skillIntegration = new SkillSystemGameIntegration();

// 为AI部队选择技能
var bestSkill = skillIntegration.SelectBestSkillForTroop(troop, enemies, allies);

// 执行技能
if (bestSkill != null)
{
    troop.UseSkill(bestSkill);
}
```

### **2. 自动战斗上下文创建**
系统会自动从游戏状态创建战斗上下文：
- **部队状态**：当前兵力、最大兵力
- **敌我态势**：敌军数量、友军数量
- **地理信息**：距离、地形类型
- **回合信息**：当前回合数

### **3. 兵种专属技能**
不同兵种有不同的技能库：
- **骑兵**：冲锋、践踏
- **步兵**：方阵、坚守  
- **弓兵**：齐射、精准射击
- **攻城器械**：攻城、破城

### **4. 实战场景演示**

#### **场景A：骑兵冲锋**
```
战斗情况：骑兵(1000/1000) vs 步兵(800/1000)
AI分析：优势场景，距离合适
选择技能：骑兵冲锋
理由：高伤害冲锋技能，适合开战时使用
```

#### **场景B：弓兵被围**
```
战斗情况：弓兵(600/800) vs 3个敌军
AI分析：被围攻场景，敌众我寡
选择技能：弓兵齐射
理由：群体攻击技能，适合对付多个敌人
```

#### **场景C：重伤治疗**
```
战斗情况：步兵(300/1000)，血量30%
AI分析：低血量危机，生存优先
选择技能：战地医疗
理由：治疗技能，低血量时优先使用
```

## 🔧 技能配置示例

### **攻击型技能**
```csharp
var attackSkill = new Skill
{
    Name = "雷霆一击",
    AIProfile = new AISkillProfile
    {
        OffensiveValue = 90f,    // 高攻击价值
        DefensiveValue = 0f,
        Priority = 85,
        BestScenarios = { SkillScenario.OneVsOne, SkillScenario.Advantage }
    },
    Conditions = new List<SkillCondition>
    {
        new SkillCondition { Type = ConditionType.OwnHP, Parameter = "0.7", Operator = ComparisonOperator.GreaterThan }
    }
};
```

### **防御型技能**
```csharp
var defenseSkill = new Skill
{
    Name = "铁壁防御",
    AIProfile = new AISkillProfile
    {
        OffensiveValue = 0f,
        DefensiveValue = 80f,    // 高防御价值
        Priority = 75,
        BestScenarios = { SkillScenario.Outnumbered, SkillScenario.LowHP }
    },
    Conditions = new List<SkillCondition>
    {
        new SkillCondition { Type = ConditionType.EnemyCount, Parameter = "2", Operator = ComparisonOperator.GreaterOrEqual }
    }
};
```

### **辅助型技能**
```csharp
var utilitySkill = new Skill
{
    Name = "战术指挥",
    AIProfile = new AISkillProfile
    {
        OffensiveValue = 20f,
        DefensiveValue = 20f,
        UtilityValue = 60f,      // 高辅助价值
        StrategicValue = 40f,    // 高战略价值
        Priority = 70,
        BestScenarios = { SkillScenario.GroupFight }
    }
};
```

## 📊 AI决策权重调整

### **情况优先级**
1. **生存危机** (血量<30%)：防御和治疗技能权重 ×2.0
2. **优势局面** (血量>70%且敌少)：攻击技能权重 ×1.5
3. **被围攻** (敌军数量>友军×2)：控制和逃跑技能权重 ×1.5
4. **群战** (敌军>2)：群攻技能权重 ×1.3

### **兵种特性**
- **骑兵**：冲锋类技能权重 +30%
- **弓兵**：远程技能权重 +25%，近战技能权重 -50%
- **步兵**：防御技能权重 +20%
- **法师**：魔法技能权重 +40%

## 🔮 未来扩展

### **已实现功能：**
✅ **智能评估**：四维价值评估系统  
✅ **场景匹配**：7种战斗场景自动识别  
✅ **条件判断**：5种使用条件类型  
✅ **游戏集成**：与现有战斗系统无缝对接  
✅ **兵种适配**：不同兵种专属技能库  

### **可以添加的功能：**
1. **技能组合**：AI学会使用技能连招
2. **敌人预测**：根据敌人行为模式选择技能
3. **团队协作**：多个AI角色的技能配合
4. **学习系统**：根据使用效果调整技能评估
5. **情感系统**：AI的"偏好"影响技能选择
6. **地形战术**：根据地形选择最优技能
7. **天气影响**：天气条件影响技能效果
8. **装备加成**：武器装备影响技能威力

## 🎯 系统优势总结

### **1. 智能化**
- AI能够理解技能特性和使用时机
- 根据战斗情况动态调整策略
- 自动生成技能评估档案

### **2. 灵活性**
- 支持任意数量的技能和效果
- 可扩展的条件和场景系统
- 模块化设计便于集成

### **3. 真实性**
- 模拟人类玩家的决策过程
- 考虑多种战斗因素
- 避免AI行为过于机械化

### **4. 性能优化**
- 预计算的价值评估
- 高效的技能筛选算法
- 最小化实时计算开销

这个AI友好技能系统让游戏中的AI角色能够像人类玩家一样智能地使用技能，大大提升了游戏的策略性和趣味性！