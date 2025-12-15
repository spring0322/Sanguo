# DialogueUI 状态流程图

## 状态枚举
```csharp
public enum State
{
    Hidden,     // 隐藏状态
    Showing,    // 正在显示
    Finished    // 播放完毕，等待回调处理
}
```

## 状态转换流程

```
[Hidden] 
    ↓ StartDialogue()
[Showing] 
    ↓ 文本播放完毕 + 玩家点击
[Finished] 
    ↓ MainGameScreen检测到 + 执行回调 + Hide()
[Hidden]
```

## 详细状态说明

### 1. Hidden 状态
- **条件**: 初始状态或调用Hide()后
- **行为**: 不显示UI，不处理输入
- **转换**: StartDialogue() → Showing

### 2. Showing 状态
- **条件**: 正在显示对话
- **行为**: 
  - 打字机效果逐字显示文本
  - 处理鼠标点击（跳过打字或继续）
  - 显示提示文字
- **转换**: 文本完成 + 点击 → Finished

### 3. Finished 状态
- **条件**: 对话播放完毕，等待回调处理
- **行为**: 
  - 继续显示对话内容
  - 等待MainGameScreen检测
  - 不处理新的输入
- **转换**: Hide() → Hidden

## 打字机效果

```csharp
// 文本显示逻辑
if (currentText.Length < targetText.Length)
{
    textTimer += deltaTime;
    if (textTimer >= TextSpeed)
    {
        textTimer = 0f;
        currentText = targetText.Substring(0, currentText.Length + 1);
    }
}
```

## 使用示例

```csharp
// 1. 开始对话
dialogueUI.StartDialogue(leader, advisor, dialogue);

// 2. 在Update循环中检测完成
if (dialogueUI.IsFinished)
{
    // 执行回调逻辑
    ExecuteCallback();
    
    // 隐藏对话
    dialogueUI.Hide();
}

// 3. 可选的控制方法
dialogueUI.SkipTypewriter();  // 跳过打字效果
dialogueUI.ForceFinish();     // 强制完成
dialogueUI.GetStateInfo();    // 获取状态信息
```

## 关键改进

1. **状态管理**: 清晰的状态转换，避免状态混乱
2. **打字机效果**: 增强用户体验，可跳过
3. **安全的Hide()**: 完全重置状态，防止残留数据
4. **调试支持**: 提供状态查询和强制控制方法
5. **更好的用户交互**: 不同阶段显示不同提示文字

## 注意事项

- `IsFinished` 属性只在 `State.Finished` 时返回true
- `Hide()` 方法会完全重置所有状态和文本
- 打字机效果可以通过点击跳过
- 状态转换是单向的，不能回退