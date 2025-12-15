# AI技能系统集成指南

## 📋 概述

本指南说明如何将AI友好技能系统集成到现有游戏中，包括从JSON数据加载技能、自动生成AI档案、以及在战斗中使用AI技能选择。

## 🎯 核心组件

### **1. AI技能系统核心** (`AI友好技能系统.cs`)
- 技能数据结构 (Skill, Influence, InfluenceKind)
- AI评估引擎 (AISkillEvaluator)
- 技能管理器 (AISkillManager)
- 游戏集成接口 (SkillSystemGameIntegration)

### **2. 数据加载器** (`AI技能系统数据加载器.cs`)
- JSON数据解析
- 自动AI档案生成
- 智能效果分类
- 技能条件推断

### **3. 测试程序** (`AI技能系统测试程序.cs`)
- 7个典型战斗场景测试
- 性能和准确率验证
- 决策过程分析

## 🚀 快速开始

### **步骤1: 加载游戏数据**

```csharp
using System;
using System.IO;

// 创建数据加载器
var loader = new AISkillSystemDataLoader();

// 从文件加载JSON数据
string jsonContent = File.ReadAllText("GameData/Skills.json");

// 加载并自动生成AI档案
loader.LoadData(jsonContent);

// 获取AI技能管理器
var skillManager = loader.GetSkillManager();

Console.WriteLine($"加载完成: {loader.AllSkills.Count} 个技能");
```

### **步骤2: 在战斗中使用AI技能选择**

```csharp
// 创建游戏集成接口
var skillIntegration = new SkillSystemGameIntegration();

// 在AI回合中选择技能
public void AITurn(Troop aiTroop, List<Troop> enemies, List<Troop> allies)
{
    // AI自动选择最佳技能
    var bestSkill = skillIntegration.SelectBestSkillForTroop(
        aiTroop, 
        enemies, 
        allies
    );
    
    if (bestSkill != null)
    {
        // 执行技能
        ExecuteSkill(aiTroop, bestSkill, enemies);
        
        // 记录使用
        bestSkill.Usage.UseSkill(GameManager.CurrentTurn);
        
        Console.WriteLine($"{aiTroop.Leader.Name} 使用了 {bestSkill.Name}");
    }
    else
    {
        // 没有合适技能，执行普通攻击
        NormalAttack(aiTroop, enemies[0]);
    }
}
```

### **步骤3: 自定义技能评估（可选）**

```csharp
// 如果需要自定义AI评估逻辑
public class CustomAIEvaluator : AISkillEvaluator
{
    public override float EvaluateSkill(Skill skill, BattleContext context)
    {
        float baseScore = base.EvaluateSkill(skill, context);
        
        // 添加自定义逻辑
        if (context.TerrainType == TerrainType.Mountain)
        {
            // 山地上防御技能价值提升
            if (skill.AIProfile.Category == SkillCategory.Defense)
            {
                baseScore *= 1.3f;
            }
        }
        
        return baseScore;
    }
}
```

## 📊 JSON数据格式

### **标准格式**

```json
{
  "AllInfluences": {
    "Influences": [
      {
        "Value": {
          "ID": 4012,
          "Name": "攻击力提升",
          "Parameter": "50",
          "Kind": {
            "ID": 1,
            "Name": "攻击增益",
            "Type": 1
          }
        }
      }
    ]
  },
  "AllCombatMethods": {
    "CombatMethods": [
      {
        "Value": {
          "ID": 1,
          "Name": "火球术",
          "Description": "发射火球攻击敌人",
          "InfluencesString": "4012 4013"
        }
      }
    ]
  }
}
```

### **简化格式（也支持）**

```json
{
  "Influences": [
    {
      "ID": 4012,
      "Name": "攻击力提升",
      "Parameter": "50",
      "Kind": {
        "ID": 1,
        "Name": "攻击增益"
      }
    }
  ],
  "Skills": [
    {
      "ID": 1,
      "Name": "火球术",
      "Description": "发射火球攻击敌人",
      "InfluencesString": "4012"
    }
  ]
}
```

## 🧠 AI自动推断规则

### **1. 效果类别推断**

数据加载器会根据效果名称自动推断类别：

| 关键词 | 推断类别 | AI权重 |
|--------|----------|--------|
| 伤害、攻击、破坏 | Damage | 1.2 |
| 治疗、恢复、回复 | Healing | 1.3 |
| 防御、护甲、提升 | Buff | 1.0 |
| 降低、减少、削弱 | Debuff | 1.1 |
| 眩晕、麻痹、束缚 | Control | 1.4 |
| 速度、移动、冲锋 | Movement | 0.8 |

### **2. 技能类别推断**

基于技能名称和效果组合：

```
技能名称包含"攻击" + 有伤害效果 → Attack
技能名称包含"治疗" + 有治疗效果 → Healing
技能名称包含"防御" + 有增益效果 → Defense
有控制效果 → Control
有多个增益效果 → Buff
有多个减益效果 → Debuff
```

### **3. 使用场景推断**

| 技能类别 | 自动添加场景 |
|----------|--------------|
| Attack | Advantage, OneVsOne |
| Attack (群攻) | GroupFight |
| Defense/Buff | Outnumbered, LowHP |
| Healing | LowHP |
| Control | Outnumbered, GroupFight |
| Debuff | Advantage, GroupFight |

### **4. 使用条件推断**

```
Healing技能 → 自动添加: OwnHP < 60%
Control技能 → 自动添加: EnemyCount >= 2
名称含"远程" → 自动添加: Distance > 2
名称含"冲锋" → 自动添加: Distance <= 3
```

### **5. 冷却时间推断**

```
技能总价值 > 80 → 冷却5回合 (强力技能)
技能总价值 > 50 → 冷却3回合 (中等技能)
技能总价值 <= 50 → 冷却1回合 (弱技能)
```

## 🎮 实战示例

### **示例1: 完整集成流程**

```csharp
public class GameAIManager
{
    private AISkillSystemDataLoader _dataLoader;
    private SkillSystemGameIntegration _skillIntegration;
    
    public void Initialize()
    {
        // 1. 加载数据
        _dataLoader = new AISkillSystemDataLoader();
        string jsonData = LoadGameData();
        _dataLoader.LoadData(jsonData);
        
        // 2. 创建集成接口
        _skillIntegration = new SkillSystemGameIntegration();
        
        // 3. 可选：导出AI档案查看
        string aiProfiles = _dataLoader.ExportAIProfilesToJson();
        File.WriteAllText("Debug/AIProfiles.json", aiProfiles);
        
        Console.WriteLine("AI技能系统初始化完成");
    }
    
    public void ExecuteAIBattle(Troop aiTroop, List<Troop> enemies)
    {
        // AI选择技能
        var skill = _skillIntegration.SelectBestSkillForTroop(
            aiTroop, 
            enemies, 
            new List<Troop>()
        );
        
        if (skill != null)
        {
            // 显示AI决策信息
            Console.WriteLine($"AI决策: {skill.Name}");
            Console.WriteLine($"理由: {skill.AIProfile.AIDescription}");
            
            // 执行技能
            UseSkill(aiTroop, skill, enemies);
        }
    }
}
```

### **示例2: 自定义效果映射**

```csharp
// 如果游戏有特殊的效果名称，可以自定义映射
var loader = new AISkillSystemDataLoader();

// 在加载前添加自定义映射
loader.AddEffectMapping("火焰", EffectCategory.Damage);
loader.AddEffectMapping("寒冰", EffectCategory.Control);
loader.AddEffectMapping("神圣", EffectCategory.Healing);

// 然后正常加载
loader.LoadData(jsonContent);
```

### **示例3: 调试AI决策**

```csharp
// 创建测试场景
var testScenario = new BattleContext
{
    CurrentTurn = 5,
    OwnHP = 300,
    MaxHP = 1000,
    EnemyCount = 3,
    AllyCount = 1,
    InCombat = true
};

// 获取所有可用技能
var availableSkills = new List<int> { 1, 2, 3, 4, 5 };

// 评估每个技能
var skillManager = loader.GetSkillManager();
foreach (var skillId in availableSkills)
{
    var skill = loader.GetSkillById(skillId);
    if (skill != null)
    {
        var evaluator = new AISkillEvaluator();
        float score = evaluator.EvaluateSkill(skill, testScenario);
        
        Console.WriteLine($"{skill.Name}: 评分 {score:F2}");
        Console.WriteLine($"  - 类别: {skill.AIProfile.Category}");
        Console.WriteLine($"  - 价值: 攻{skill.AIProfile.OffensiveValue:F1} " +
                         $"防{skill.AIProfile.DefensiveValue:F1} " +
                         $"辅{skill.AIProfile.UtilityValue:F1}");
    }
}
```

## 🔧 高级配置

### **1. 自定义AI权重**

```csharp
// 加载后调整特定技能的AI权重
var skill = loader.GetSkillById(1);
if (skill != null)
{
    // 手动调整价值
    skill.AIProfile.OffensiveValue = 100f;
    skill.AIProfile.Priority = 95;
    
    // 添加特殊场景
    skill.AIProfile.BestScenarios.Add(SkillScenario.Ambush);
    
    // 添加使用条件
    skill.Conditions.Add(new SkillCondition
    {
        Type = ConditionType.Weather,
        Parameter = "Rain",
        Operator = ComparisonOperator.Equal
    });
}
```

### **2. 兵种专属技能**

```csharp
// 为不同兵种配置专属技能
public class TroopSkillConfig
{
    public static List<int> GetSkillsForTroopType(TroopType type)
    {
        return type switch
        {
            TroopType.Cavalry => new List<int> { 1001, 1002, 1003 },
            TroopType.Infantry => new List<int> { 2001, 2002, 2003 },
            TroopType.Archer => new List<int> { 3001, 3002, 3003 },
            TroopType.Mage => new List<int> { 4001, 4002, 4003 },
            _ => new List<int>()
        };
    }
}
```

### **3. 动态难度调整**

```csharp
// 根据游戏难度调整AI技能选择
public class DifficultyAdjustedAI
{
    private float _difficultyMultiplier;
    
    public DifficultyAdjustedAI(GameDifficulty difficulty)
    {
        _difficultyMultiplier = difficulty switch
        {
            GameDifficulty.Easy => 0.7f,
            GameDifficulty.Normal => 1.0f,
            GameDifficulty.Hard => 1.3f,
            GameDifficulty.Expert => 1.5f,
            _ => 1.0f
        };
    }
    
    public Skill SelectSkill(List<int> skills, BattleContext context)
    {
        var skillManager = GetSkillManager();
        var bestSkill = skillManager.SelectBestSkill(skills, context);
        
        // 简单难度下，AI有概率选择次优技能
        if (_difficultyMultiplier < 1.0f && Random.value > _difficultyMultiplier)
        {
            // 随机选择一个技能
            var randomSkill = skills[Random.Range(0, skills.Count)];
            return GetSkillById(randomSkill);
        }
        
        return bestSkill;
    }
}
```

## 📈 性能优化建议

### **1. 技能预加载**

```csharp
// 游戏启动时预加载所有技能
public class SkillPreloader
{
    private static AISkillSystemDataLoader _loader;
    
    public static void PreloadAllSkills()
    {
        _loader = new AISkillSystemDataLoader();
        string jsonData = Resources.Load<TextAsset>("GameData/Skills").text;
        _loader.LoadData(jsonData);
        
        Console.WriteLine("技能预加载完成");
    }
    
    public static AISkillManager GetSkillManager()
    {
        return _loader?.GetSkillManager();
    }
}
```

### **2. 技能缓存**

```csharp
// 缓存常用技能的评估结果
public class SkillEvaluationCache
{
    private Dictionary<string, float> _cache = new Dictionary<string, float>();
    
    public float GetCachedScore(int skillId, BattleContext context)
    {
        string key = $"{skillId}_{context.GetHashCode()}";
        
        if (_cache.TryGetValue(key, out float score))
        {
            return score;
        }
        
        // 计算并缓存
        var evaluator = new AISkillEvaluator();
        var skill = GetSkillById(skillId);
        score = evaluator.EvaluateSkill(skill, context);
        
        _cache[key] = score;
        return score;
    }
}
```

### **3. 批量评估**

```csharp
// 一次性评估多个技能
public List<(Skill skill, float score)> EvaluateAllSkills(
    List<int> skillIds, 
    BattleContext context)
{
    var results = new List<(Skill, float)>();
    var evaluator = new AISkillEvaluator();
    
    foreach (var id in skillIds)
    {
        var skill = GetSkillById(id);
        if (skill != null)
        {
            float score = evaluator.EvaluateSkill(skill, context);
            results.Add((skill, score));
        }
    }
    
    return results.OrderByDescending(r => r.score).ToList();
}
```

## 🐛 调试技巧

### **1. 导出AI档案**

```csharp
// 导出所有技能的AI档案到JSON文件
string aiProfiles = loader.ExportAIProfilesToJson();
File.WriteAllText("Debug/AIProfiles.json", aiProfiles);

// 在编辑器中查看，验证AI推断是否正确
```

### **2. 日志记录**

```csharp
// 记录AI决策过程
public void LogAIDecision(Skill skill, BattleContext context)
{
    Console.WriteLine($"=== AI决策日志 ===");
    Console.WriteLine($"技能: {skill.Name}");
    Console.WriteLine($"类别: {skill.AIProfile.Category}");
    Console.WriteLine($"战斗情况: HP {context.GetOwnHPRatio():P0}, " +
                     $"敌军{context.EnemyCount}, 友军{context.AllyCount}");
    Console.WriteLine($"技能价值: 攻{skill.AIProfile.OffensiveValue:F1} " +
                     $"防{skill.AIProfile.DefensiveValue:F1}");
    Console.WriteLine($"选择理由: {skill.AIProfile.AIDescription}");
}
```

### **3. 单元测试**

```csharp
// 测试特定场景下的AI选择
[Test]
public void TestLowHPScenario()
{
    var loader = new AISkillSystemDataLoader();
    loader.LoadData(testJsonData);
    
    var context = new BattleContext
    {
        OwnHP = 200,
        MaxHP = 1000, // 20% HP
        EnemyCount = 1
    };
    
    var skillManager = loader.GetSkillManager();
    var bestSkill = skillManager.SelectBestSkill(
        new List<int> { 1, 2, 3 }, 
        context
    );
    
    // 应该选择治疗技能
    Assert.AreEqual(SkillCategory.Healing, bestSkill.AIProfile.Category);
}
```

## ✅ 集成检查清单

- [ ] 已加载 `AI友好技能系统.cs`
- [ ] 已加载 `AI技能系统数据加载器.cs`
- [ ] JSON数据格式正确
- [ ] 数据加载成功，无异常
- [ ] AI档案自动生成正确
- [ ] 技能评估逻辑正常工作
- [ ] 战斗中AI能正确选择技能
- [ ] 性能满足要求（< 10ms响应）
- [ ] 已进行充分测试
- [ ] 已添加必要的日志和调试信息

## 🎉 总结

AI友好技能系统提供了完整的从数据加载到AI决策的解决方案：

1. **自动化**: 自动从JSON加载数据并生成AI档案
2. **智能化**: AI能理解技能特性并做出合理选择
3. **灵活性**: 支持自定义和扩展
4. **高性能**: 优化的评估算法，响应迅速
5. **易集成**: 简单的API，最小化代码修改

现在你可以让游戏中的AI像人类玩家一样智能地使用技能了！🚀