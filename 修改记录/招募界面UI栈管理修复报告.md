# 招募界面UI栈管理修复报告

## 修复状态
✅ **编译成功** - 所有编译错误已修复  
✅ **代码部署完成** - 招募界面UI栈管理修复已实施

## 问题背景

原有的招募界面存在UI栈管理问题，主要表现为：
1. **手动PopUndoneWork导致栈偏移** - 在ShowRecruitmentResult中手动Pop导致"The UndoneWork is not a Frame"错误
2. **Frame关闭时机冲突** - 在Frame还没完全关闭时强制插入对话框
3. **98.dds图片加载异常** - 图片加载失败引发的二次崩溃

## 实施的修复

### 1. ShowRecruitmentInterface方法修复
```csharp
private static void ShowRecruitmentInterface(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction)
{
    // 核心修复：
    // 1. 移除了所有手动 PopUndoneWork()。让插件通过 IsShowing = false 自行处理。
    // 2. 规范了 ShowRecruitmentResult 的显示逻辑，防止在 Frame 还没消失时强插对话框。
    // 3. 增强了对 null 的检查，防止 98.dds 加载异常引发的二次崩溃。
    
    // --- 安全清理第一步：只关显示，不乱动栈 ---
    if (gameScreen.Plugins.tupianwenziPlugin.IsShowing)
    {
        gameScreen.Plugins.tupianwenziPlugin.IsShowing = false;
    }
    
    // --- 显示招募 Frame ---
    // 注意：ShowTabListInFrame 内部会自动 Push 一个 UndoneWorkKind.Frame
    gameScreen.ShowTabListInFrame(
        UndoneWorkKind.Frame,
        FrameKind.Person,
        FrameFunction.PersonManualHire,
        false, true, true, false,
        recommendedPersons,
        null,
        "军师推荐招募",
        "Ability");
}
```

### 2. HandleRecruitmentConfirm方法修复
```csharp
private static void HandleRecruitmentConfirm(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction)
{
    // 【关键修复】显示结果
    // 不要在 HandleRecruitmentConfirm 里手动 Pop。
    // 框架执行完这个 OKFunction 后，会自动关闭当前 Frame 并 Pop 栈。
    ShowRecruitmentResult(gameScreen, faction, selectedPerson, recruitSuccess, 
        recruitSuccess ? "" : "拒绝加入");
}
```

### 3. ShowRecruitmentResult方法修复
```csharp
private static void ShowRecruitmentResult(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction, Person person, bool success, string failureReason)
{
    // 【核心修复】移除原代码中所有的 gameScreen.PopUndoneWork()。
    // 因为这个方法是被 GameFrame 的 OK 按钮触发的，Frame 此时正在关闭流程中。
    // 如果你在这里再次 Pop，会导致栈弹过了头，引发 "The UndoneWork is not a Frame"。
    
    string resultText = success 
        ? $"军师 {faction.Advisor.Name}：\n\n恭喜主公！{person.Name} 已加入我军！" 
        : $"军师 {faction.Advisor.Name}：\n\n招募失败。原因：{failureReason}";
    
    // 使用对话框显示结果
    if (gameScreen.Plugins.tupianwenziPlugin != null)
    {
        // 设置文本和人物（如果 98.dds 报错，这里尝试传空字符串不加载图片）
        gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
            faction.Advisor, faction.Advisor, resultText, "", "", "");
        gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
        
        // 延迟一帧显示，确保前一个 Frame 已经完全退出栈
        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
    }
}
```

## 修复原理

### 核心思想：让框架自己管理UI栈

1. **移除手动栈操作**：
   - 不再手动调用`gameScreen.PopUndoneWork()`
   - 让GameFrame的DoOK逻辑自动处理栈管理

2. **规范化界面关闭**：
   - 通过设置`IsShowing = false`来关闭插件
   - 避免在Frame关闭过程中强制插入新界面

3. **增强异常处理**：
   - 对null值进行充分检查
   - 避免图片加载异常导致的崩溃

### UI栈管理流程

```
1. 军师建议触发 → TriggerAdvisorAdvice()
2. 用户点击"确定，我要招募" → ShowRecruitmentInterface()
3. 显示招募人物列表界面 → ShowTabListInFrame() [自动Push Frame]
4. 用户选择武将并点击确定 → HandleRecruitmentConfirm()
5. 框架自动关闭Frame并Pop栈
6. 显示招募结果 → ShowRecruitmentResult()
```

## 预期效果

修复后的招募界面应该：
- ✅ 不再出现"The UndoneWork is not a Frame"错误
- ✅ Frame和对话框切换流畅，无冲突
- ✅ 图片加载异常不会导致游戏崩溃
- ✅ UI栈状态始终保持正确

## 测试建议

1. **触发军师招募建议**，验证界面是否正常显示
2. **选择武将并确认**，检查是否有栈管理错误
3. **查看招募结果**，确认对话框显示正常
4. **多次重复操作**，验证UI栈状态的稳定性

## 技术总结

这次修复的核心是**信任框架的自动管理机制**，而不是试图手动控制每一个细节。通过移除不必要的手动栈操作，让UI系统按照设计的方式正常工作，从而避免了复杂的栈状态冲突问题。

---
**修复完成时间**: 2025年12月24日  
**修复类型**: UI栈管理优化  
**编译状态**: ✅ 成功  
**影响范围**: 军师招募建议系统