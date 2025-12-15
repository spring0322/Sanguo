# 🎖️ 任命军师功能完整实现总结

## 📋 功能概述
参照AppointMayor（任命太守）的实现模式，完整实现了任命军师（AppointAdvisor）功能，包括AI自动任命、玩家手动任命、UI集成和事件处理。

## 🔧 核心实现

### 1. Faction类扩展
**文件**: `WorldOfTheThreeKingdoms/GameObjects/Faction.cs`

#### 新增属性：
```csharp
// 私有字段
private Person advisor = null;
private int advisorID = -1;

// 公共属性
public Person Advisor { get; set; }  // 军师对象
public int AdvisorID { get; set; }   // 军师ID
public string AdvisorName { get; }   // 军师姓名
```

#### 新增方法：
```csharp
// 检查是否可以任命军师
public bool AppointAdvisorAvail()

// 军师候选人列表（玩家用）
public PersonList AdvisorCandicate { get; }

// AI军师候选人列表（按智力排序）
public PersonList AIAdvisorCandicate { get; }

// 任命军师
public void AppointAdvisor(Person person)

// AI自动任命军师
private void AIAppointAdvisor()
private void PlayerAIAppointAdvisor()
```

#### 候选人条件：
- 不是领袖本人
- 不是当前军师
- 可用且存活
- 未被俘虏
- 不在部队中
- 智力 >= 70

### 2. 年表系统集成
**文件**: `WorldOfTheThreeKingdoms/GameObjects/YearTable.cs`

```csharp
public void addAppointAdvisorEntry(GameDate date, Person p, Person leader)
```
- 记录任命军师的历史事件
- 添加到势力年表和个人传记

### 3. 事件系统
**文件**: `WorldOfTheThreeKingdoms/GameObjects/FactionList.cs`

```csharp
// 事件委托
public delegate void AppointAdvisorDelegate(Person leader, Person advisor);
public event AppointAdvisorDelegate OnAppointAdvisor;

// 事件处理
private void faction_OnAppointAdvisor(Person leader, Person advisor)
```

### 4. UI系统集成

#### 上下文菜单
**文件**: `WorldOfTheThreeKingdoms/GameGlobal/ContextMenuResult.cs`
```csharp
Person_Appointment_AppointAdvisor, //任命军师
Person_Appointment_RecallAdvisor,  //罢免军师
```

#### 框架功能
**文件**: `WorldOfTheThreeKingdoms/GameGlobal/FrameFunction.cs`
```csharp
AppointAdvisor, //任命军师
```

#### 菜单处理
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`
```csharp
case ContextMenuResult.Person_Appointment_AppointAdvisor:
    this.ShowTabListInFrame(..., this.CurrentFaction.AdvisorCandicate, ...);
    break;

case ContextMenuResult.Person_Appointment_RecallAdvisor:
    this.CurrentFaction.AdvisorID = -1;
    break;
```

#### 屏幕管理器
**文件**: `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`
```csharp
case FrameFunction.AppointAdvisor:
    this.FrameFunction_Faction_AppointAdvisor();
    break;

private void FrameFunction_Faction_AppointAdvisor()
{
    this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Person;
    if (this.CurrentPerson != null)
    {
        this.CurrentFaction.AdvisorID = this.CurrentPerson.ID;
        this.CurrentFaction.AppointAdvisor(this.CurrentPerson);
    }
}
```

### 5. 文本消息系统
**文件**: `WorldOfTheThreeKingdoms/GameObjects/PersonDetail/TextMessageKind.cs`
```csharp
AppointAdvisor, // 129
```

**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSPersonText.cs`
```csharp
public override void AppointAdvisor(Person p, Person q)  //军师
{
    // 显示任命军师的文本消息和动画
}
```

### 6. 基类扩展
**文件**: `WorldOfTheThreeKingdoms/GameObjects/Screen.cs`
```csharp
public virtual void AppointAdvisor(Person p, Person q) { }
```

### 7. 军师UI系统更新
**文件**: `WorldOfTheThreeKingdoms/GameManager/StrategistUI.cs`
```csharp
// 更新为使用真正的军师ID而不是领袖ID
int advisorId = currentFaction.AdvisorID;
var advisor = (advisorId != -1) ? scenario.Persons.GetGameObject(advisorId) as Person : null;
```

## 🤖 AI集成

### AI调用流程
1. **主AI流程**: `Faction.AI()` → `AIAppointAdvisor()`
2. **玩家AI流程**: `Faction.PlayerAI()` → `PlayerAIAppointAdvisor()`

### AI任命逻辑
- 检查是否可以任命军师 (`AppointAdvisorAvail()`)
- 从候选人中选择智力最高的人物 (`AIAdvisorCandicate[0]`)
- 自动任命并触发相关事件

## 🎮 玩家操作流程

### 任命军师
1. 右键点击势力或建筑
2. 选择"任命军师"选项
3. 从候选人列表中选择合适的人物
4. 确认任命

### 罢免军师
1. 右键点击势力或建筑
2. 选择"罢免军师"选项
3. 军师职位立即清空

## 📊 功能特点

### 智能候选人筛选
- **智力要求**: 最低70点智力
- **可用性检查**: 确保人物可用、存活、未被俘虏
- **职位冲突**: 排除领袖和当前军师
- **状态检查**: 排除在部队中的人物

### AI智能任命
- **自动评估**: AI会自动评估是否需要任命军师
- **智力优先**: 优先选择智力最高的候选人
- **玩家势力**: 支持玩家势力的AI辅助任命

### 完整事件系统
- **历史记录**: 任命事件记录到年表和传记
- **文本消息**: 显示任命过程的文本和动画
- **事件广播**: 通知其他系统军师变更

## 🔗 与现有系统的集成

### 军师系统联动
- **StrategistSystem**: 军师建议系统会使用真正的军师
- **StrategistUI**: 军师按钮显示真正的军师头像
- **AI决策**: AI系统可以基于军师智力做出更好的决策

### 数据持久化
- **存档兼容**: AdvisorID标记为DataMember，支持存档
- **向下兼容**: 旧存档中AdvisorID默认为-1（无军师）

## 🎯 使用示例

### 代码调用示例
```csharp
// 检查是否可以任命军师
if (faction.AppointAdvisorAvail())
{
    // 获取候选人
    var candidates = faction.AdvisorCandicate;
    
    // 任命第一个候选人
    if (candidates.Count > 0)
    {
        Person advisor = candidates[0] as Person;
        faction.AdvisorID = advisor.ID;
        faction.AppointAdvisor(advisor);
    }
}

// 获取当前军师
Person currentAdvisor = faction.Advisor;
if (currentAdvisor != null)
{
    Console.WriteLine($"当前军师: {currentAdvisor.Name}");
}
```

## ✅ 测试要点

### 功能测试
- [ ] 玩家可以通过UI任命军师
- [ ] AI会自动任命合适的军师
- [ ] 军师任命事件正确记录到年表
- [ ] 军师UI显示正确的军师信息
- [ ] 罢免军师功能正常工作

### 边界测试
- [ ] 无候选人时不显示任命选项
- [ ] 已有军师时不能重复任命
- [ ] 军师死亡或被俘时自动清空职位
- [ ] 存档加载后军师信息正确

## 🎉 总结

任命军师功能已完整实现，包括：
- ✅ **核心逻辑**: 完整的任命、罢免、候选人筛选
- ✅ **AI集成**: 自动任命和智能选择
- ✅ **UI集成**: 完整的用户界面和操作流程
- ✅ **事件系统**: 完整的事件处理和历史记录
- ✅ **系统联动**: 与军师系统、UI系统的完整集成

该功能完全参照AppointMayor的实现模式，确保了代码的一致性和可维护性。