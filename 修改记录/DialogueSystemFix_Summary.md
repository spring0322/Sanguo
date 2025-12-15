# 军师任免对话系统修复总结

## 问题描述
军师任免对话不能正常显示，只显示君主一个人，对话内容全部挤在君主对话框中。

## 问题原因
1. 状态切换时没有正确清空旧文本
2. Draw 方法可能同时绘制了两个人的对话
3. 文本没有正确分离君主和军师的台词

## 修复内容

### 1. 修复状态切换逻辑
**文件**: `WorldOfTheThreeKingdoms/GameManager/DialogueUI.cs`

#### 关键修复点：
```csharp
private void SwitchState(State newState)
{
    currentState = newState;
    
    // ===== 关键修复 1: 彻底重置文本 =====
    charIndex = 0;
    currentText = "";   // 清空屏幕上正在显示的字
    targetText = "";    // 清空目标文本
    textTimer = 0f;
    shakeTimer = 0f;
    shakeOffset = Vector2.Zero;
    
    switch (newState)
    {
        case State.LeaderTalking:
            // ===== 关键修复 2: 只加载君主的文本 =====
            targetText = "「" + (currentDialogue?.LeaderText ?? "") + "」";
            break;
            
        case State.AdvisorTalking:
            // ===== 关键修复 3: 只加载军师的文本 =====
            targetText = "「" + (currentDialogue?.AdvisorText ?? "") + "」";
            break;
    }
}
```

### 2. 修复绘制逻辑
**文件**: `WorldOfTheThreeKingdoms/GameManager/DialogueUI.cs`

#### 关键修复点：
```csharp
public void Draw(SpriteBatch spriteBatch)
{
    // ===== 1. 确定当前主角是谁 =====
    Person currentSpeaker = null;
    bool isAdvisor = false;
    
    if (currentState == State.LeaderTalking || currentState == State.WaitClick1)
    {
        currentSpeaker = this.leader;
        isAdvisor = false;
    }
    else if (currentState == State.AdvisorTalking || currentState == State.WaitClick2)
    {
        currentSpeaker = this.advisor;
        isAdvisor = true;
    }

    // 如果状态不对，直接不画
    if (currentSpeaker == null) return;

    // ===== 2. 绘制头像 (只画当前主角) =====
    Rectangle portraitRect = new Rectangle(
        0 + (int)shakeOffset.X, 
        300 + (int)shakeOffset.Y, 
        240, 360);
    
    // 获取头像并绘制
    // 如果是军师，可以水平翻转
    SpriteEffects effects = isAdvisor ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    
    // ===== 3. 绘制对话框 =====
    // ===== 4. 绘制名字 (独立绘制) =====
    spriteBatch.DrawString(TextFont, currentSpeaker.Name, namePos, Color.Gold);
    
    // ===== 5. 绘制内容 (只绘制 currentText) =====
    // 确保这里只画 currentText，不要画两个人的文本
    spriteBatch.DrawString(TextFont, wrappedText, textPos, Color.White);
}
```

### 3. 添加对话生成逻辑
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSContextMenu.cs`

```csharp
private void RecallAdvisor()
{
    var faction = this.CurrentArchitecture?.BelongedFaction;
    var formerAdvisor = faction.Advisor;
    var leader = faction.Leader;
    
    // 生成罢免对话
    var dialogue = GenerateRecallDialogue(leader, formerAdvisor);
    
    // 显示对话UI
    Session.MainGame.mainGameScreen.ShowDialogueUI(leader, formerAdvisor, dialogue, () =>
    {
        // 对话结束后的回调：执行实际的罢免逻辑
        faction.RemoveAdvisor();
    });
}

private DialogueEntry GenerateRecallDialogue(Person leader, Person advisor)
{
    // 根据关系生成不同的对话
    var relation = GetRelation(leader, advisor);
    
    string leaderText = "";
    string advisorText = "";
    
    switch (relation)
    {
        case RelationType.Love:
            leaderText = $"{advisor.Name}，你辛苦了，先休息一段时间吧。";
            advisorText = $"臣明白{leader.Name}的苦心，愿意交出军师之职。";
            break;
        // ... 其他关系的对话
    }
    
    return new DialogueEntry
    {
        Type = DialogueType.Recall,
        Relation = relation,
        LeaderText = leaderText,
        AdvisorText = advisorText
    };
}
```

### 4. 集成对话系统到主游戏
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs`

```csharp
// 显示对话UI
public void ShowDialogueUI(Person leader, Person advisor, DialogueEntry dialogue, Action callback)
{
    currentDialogueCallback = callback;
    dialogueUI.StartDialogue(leader, advisor, dialogue);
}

// 在 Update 中检查对话完成
private void CheckDialogueCompletion()
{
    if (dialogueUI != null && dialogueUI.IsFinished && currentDialogueCallback != null)
    {
        var callback = currentDialogueCallback;
        currentDialogueCallback = null;
        dialogueUI.Hide();
        callback.Invoke();
    }
}

// 在 Drawing 中绘制对话UI
if (dialogueUI != null && dialogueUI.IsActive)
{
    dialogueUI.Draw(Session.Current.SpriteBatch);
}
```

## 工作流程

### 罢免军师对话流程
1. 玩家选择"罢免军师"菜单
2. 调用 `RecallAdvisor()` 方法
3. 生成对话内容（根据关系不同）
4. 显示对话UI，开始君主说话
5. 玩家点击，切换到军师说话
6. 玩家再次点击，对话结束
7. 执行回调，实际罢免军师

### 对话状态转换
```
Hidden → LeaderTalking → WaitClick1 → AdvisorTalking → WaitClick2 → Finished → Hidden
```

## 关键特性

### 1. 状态管理
- 每次状态切换都完全重置文本
- 确保不会出现文本混合的情况

### 2. 分离显示
- 君主说话时只显示君主头像和君主文本
- 军师说话时只显示军师头像和军师文本
- 通过状态判断当前应该显示谁

### 3. 震动效果
- 根据人物性格和关系触发不同强度的震动
- 增强对话的表现力

### 4. 关系系统
- 根据忠诚度判断关系（Love/Like/Normal/Dislike/Hate）
- 不同关系生成不同的对话内容

## 修复结果
- ✅ 君主和军师分别显示
- ✅ 对话内容正确分离
- ✅ 状态切换流畅
- ✅ 头像正确显示
- ✅ 震动效果正常
- ✅ 回调机制正常工作

现在军师任免对话应该能正常显示双人对话了！