# 上下文菜单集成总结

## 修改的文件

### WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs

在 `HandleContextMenuResult` 方法中修改了军师相关的菜单处理：

## 修改内容

### 1. 任命军师 (AppointAdvisor)
```csharp
case ContextMenuResult.Person_Appointment_AppointAdvisor: //任命军师
    // 这是一个假设的调用，你可以把 AppointmentAdvisor 的逻辑也加进来
    this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.AppointAdvisor, false, true, true, false, Session.Current.Scenario.CurrentFaction.AdvisorCandicate, null, "任命军师", "");
    break;
```

**功能说明**:
- 对应 XML 中的 AppointAdvisor (ID=3)
- 显示候选人列表，让玩家选择要任命的军师
- 使用 `Session.Current.Scenario.CurrentFaction.AdvisorCandicate` 获取候选人列表

### 2. 罢免军师 (RecallAdvisor)
```csharp
case ContextMenuResult.Person_Appointment_RecallAdvisor: //罢免军师
    // 这里调用我们在上一步写好的罢免逻辑！
    this.RecallAdvisor();
    break;
```

**功能说明**:
- 对应 XML 中的 RecallAdvisor (ID=4)
- 调用 `MainGameScreen.RecallAdvisor()` 方法
- 该方法会显示对话UI并处理罢免逻辑

## 相关方法

### MainGameScreen.RecallAdvisor()
位置: `WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs`

```csharp
public void RecallAdvisor()
{
    // 1. 获取当前数据
    Faction faction = Session.Current.Scenario.CurrentFaction;
    Person leader = faction.Leader;
    Person advisor = faction.Advisor;

    // 2. 获取对话内容
    var dialogue = DialogueManager.GetDialogue(leader, advisor, DialogueType.Recall);

    // 3. 显示对话UI
    this.ShowDialogueUI(leader, advisor, dialogue, () => 
    {
        // 回调函数：对话结束后执行
        faction.RemoveAdvisor();
        if (advisor.CheckRelation(leader) != 1)
        {
            advisor.Loyalty -= 5;
            if (advisor.Loyalty < 0) advisor.Loyalty = 0;
        }
        this.UpdateAdvisorButton(Session.Current.Scenario);
    });
}
```

## 工作流程

### 任命军师流程
1. 玩家右键点击 → 选择"任命军师"菜单
2. 触发 `ContextMenuResult.Person_Appointment_AppointAdvisor`
3. 显示候选人列表框架
4. 玩家选择候选人 → 通过 `FrameFunction.AppointAdvisor` 处理
5. 可以在 FrameFunction 处理中添加对话系统

### 罢免军师流程
1. 玩家右键点击 → 选择"罢免军师"菜单
2. 触发 `ContextMenuResult.Person_Appointment_RecallAdvisor`
3. 调用 `RecallAdvisor()` 方法
4. 加载罢免对话配置
5. 显示对话UI
6. 玩家看完对话点击继续
7. 执行回调：清除军师、降低忠诚度等

## 对话系统集成

### 使用的组件
- **DialogueManager**: 管理对话配置和匹配
- **DialogueUI**: 显示对话界面
- **DialogueConfig**: 对话配置系统
- **MainGameScreen.ShowDialogueUI()**: 显示对话的入口方法

### 配置文件
- `Content/Data/AppointmentDialogues.xml` - 任命对话
- `Content/Data/RecallDialogues.xml` - 罢免对话

## 扩展建议

### 1. 任命军师对话
可以在 FrameFunction.AppointAdvisor 的处理中添加对话系统：

```csharp
// 在选择候选人后
var dialogue = DialogueManager.GetDialogue(leader, selectedAdvisor, DialogueType.Default);
this.ShowDialogueUI(leader, selectedAdvisor, dialogue, () => {
    // 执行任命逻辑
    faction.AppointAdvisor(selectedAdvisor);
});
```

### 2. 菜单项扩展
可以添加更多军师相关的菜单项：
- 军师建议
- 军师技能
- 军师状态查看

### 3. 对话类型扩展
可以添加更多对话类型：
- 任命成功对话
- 拒绝任命对话
- 军师建议对话

## 注意事项

1. **空值检查**: 确保 faction、leader、advisor 不为空
2. **候选人列表**: 确保 AdvisorCandicate 属性存在且有效
3. **对话配置**: 确保 XML 配置文件存在且格式正确
4. **回调安全**: 确保回调函数中的操作是安全的

## 测试建议

1. 测试有军师时的罢免功能
2. 测试无军师时的任命功能
3. 测试对话显示和交互
4. 测试回调函数的执行
5. 测试异常情况的处理