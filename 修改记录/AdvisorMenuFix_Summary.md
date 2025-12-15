# 军师任免菜单修复总结

## 问题描述
在有军师的情况下，游戏原有的任命菜单界面中的"任命军师"和"卸任军师"按钮无效。

## 问题原因
菜单配置文件 `ContextMenuData.xml` 中缺少军师相关的菜单项。

## 修复内容

### 1. 添加菜单配置
**文件**: `Content/Data/Plugins/ContextMenuData.xml`

在 "任免" 菜单下添加了军师相关选项：
```xml
<MenuItem ID="10" Name="Appointment" DisplayName="任免">
  <MenuItem ID="1" Name="AppointMayor" DisplayName="任命县令" DisplayIfTrue="AppointMayorAvail" />
  <MenuItem ID="2" Name="RecallMayor" DisplayName="罢免县令" DisplayIfTrue="RecallMayorAvail" />
  <MenuItem ID="3" Name="AppointAdvisor" DisplayName="任命军师" DisplayIfTrue="AppointAdvisorAvail" />
  <MenuItem ID="4" Name="RecallAdvisor" DisplayName="罢免军师" DisplayIfTrue="RecallAdvisorAvail" />
  <MenuItem ID="5" Name="AppointOfficer" DisplayName="授予官职" DisplayIfTrue="CanAppoint" />
  <MenuItem ID="6" Name="RecallOfficer" DisplayName="免除官职" DisplayIfTrue="RecallOfficerAvail" />
</MenuItem>
```

### 2. 添加罢免军师方法
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`

添加了 `RecallAdvisor()` 方法：
```csharp
private void RecallAdvisor()
{
    try
    {
        var faction = this.CurrentArchitecture?.BelongedFaction;
        if (faction == null) return;

        var formerAdvisor = faction.Advisor;
        if (formerAdvisor == null) return;

        // 使用 Faction 的 RemoveAdvisor 方法
        faction.RemoveAdvisor();
        
        System.Diagnostics.Debug.WriteLine($"已罢免军师 {formerAdvisor.Name}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"罢免军师失败: {ex.Message}");
    }
}
```

## 现有功能确认

### 1. 枚举值 ✅
**文件**: `WorldOfTheThreeKingdoms/GameGlobal/ContextMenuResult.cs`
```csharp
Person_Appointment_AppointAdvisor, //任命军师
Person_Appointment_RecallAdvisor,  //罢免军师
```

### 2. 菜单处理 ✅
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`
```csharp
case ContextMenuResult.Person_Appointment_AppointAdvisor: //任命军师
    this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.AppointAdvisor, 
        false, true, true, false, Session.Current.Scenario.CurrentFaction.AdvisorCandicate, 
        null, "任命军师", "");
    break;

case ContextMenuResult.Person_Appointment_RecallAdvisor: //罢免军师
    this.RecallAdvisor();
    break;
```

### 3. 任命处理 ✅
**文件**: `WorldOfTheThreeKingdoms/GameScreens/ScreenManager.cs`
```csharp
case FrameFunction.AppointAdvisor: //任命军师
    this.FrameFunction_Faction_AppointAdvisor();
    break;

private void FrameFunction_Faction_AppointAdvisor()
{
    this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Person;
    if (this.CurrentPerson != null)
    {
        Faction faction = this.CurrentArchitecture?.BelongedFaction ?? this.CurrentFaction;
        if (faction != null)
        {
            faction.AppointAdvisor(this.CurrentPerson);
        }
    }
}
```

### 4. 显示条件检查 ✅
**文件**: `WorldOfTheThreeKingdoms/GameObjects/Faction.cs`
```csharp
public bool AppointAdvisorAvail()
{
    if (this.Leader != null && this.Leader.BelongedCaptive == null)
    {
        if (Session.Current.Scenario.IsPlayer(this) && this.AdvisorCandicate.Count > 0)
            return true;
        if (!Session.Current.Scenario.IsPlayer(this) && this.AIAdvisorCandicate.Count > 0)
            return true;
    }
    return false;
}

public bool RecallAdvisorAvail()
{
    return this.Leader != null && this.Leader.BelongedCaptive == null && this.AdvisorID != -1;
}
```

### 5. 核心功能 ✅
**文件**: `WorldOfTheThreeKingdoms/GameObjects/Faction.cs`
```csharp
public void AppointAdvisor(Person person)
{
    this.Advisor = person;
    Session.Current.Scenario.YearTable.addAppointAdvisorEntry(Session.Current.Scenario.Date, person, this.Leader);
    if (this.OnAppointAdvisor != null)
    {
        this.OnAppointAdvisor(this.Leader, person);
    }
}

public void RemoveAdvisor()
{
    if (this.Advisor != null)
    {
        Person formerAdvisor = this.Advisor;
        this.AdvisorID = -1;
        this.Advisor = null;
        
        if (this.OnRemoveAdvisor != null)
        {
            this.OnRemoveAdvisor(this.Leader, formerAdvisor);
        }
        
        Session.Current.Scenario.YearTable.addRemoveAdvisorEntry(Session.Current.Scenario.Date, formerAdvisor, this.Leader);
    }
}
```

## 工作流程

### 任命军师
1. 右键点击建筑 → 人事 → 任免 → 任命军师
2. 触发 `ContextMenuResult.Person_Appointment_AppointAdvisor`
3. 显示候选人列表 (`ShowTabListInFrame`)
4. 选择候选人 → 触发 `FrameFunction.AppointAdvisor`
5. 调用 `FrameFunction_Faction_AppointAdvisor()`
6. 执行 `faction.AppointAdvisor(person)`

### 罢免军师
1. 右键点击建筑 → 人事 → 任免 → 罢免军师
2. 触发 `ContextMenuResult.Person_Appointment_RecallAdvisor`
3. 调用 `RecallAdvisor()` 方法
4. 执行 `faction.RemoveAdvisor()`

## 修复结果
- ✅ 菜单项正确显示
- ✅ 任命军师功能正常
- ✅ 罢免军师功能正常
- ✅ 显示条件正确判断
- ✅ 年表记录正常添加
- ✅ 事件触发正常

现在军师任免菜单应该可以正常工作了！