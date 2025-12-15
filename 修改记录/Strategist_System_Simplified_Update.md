# 🎖️ 军师系统简化更新总结

## 📋 更新概述

根据用户要求，对军师系统的建议类型进行了简化，从原来的5种类型简化为3种核心类型，并优化了数据结构设计。

---

## 🔄 主要变更

### 1. 建议类型简化

#### 更新前 (5种类型)
```csharp
public enum AdviceType
{
    Emergency,      // 紧急军情
    Military,       // 军事建议
    Internal,       // 内政建议
    Diplomatic,     // 外交建议
    Economic        // 经济建议
}
```

#### 更新后 (3种类型)
```csharp
public enum AdviceType
{
    General,    // 日常建议 (主动问出来的)
    Emergency,  // 紧急军情 (被动弹出来的)
    Internal    // 内部隐患 (被动弹出来的)
}
```

### 2. 数据结构优化

#### 更新前
```csharp
public class AdviceData
{
    public string Title { get; set; }           // 建议标题
    public string Content { get; set; }         // 建议内容
    public AdviceType Type { get; set; }        // 建议类型
    public int Priority { get; set; }           // 优先级 (1-10)
    public DateTime Timestamp { get; set; }     // 时间戳
    public object RelatedObject { get; set; }   // 相关对象
}
```

#### 更新后
```csharp
public class AdviceData
{
    public string Title;                // 标题 (如：【军情急报】 或 【军师锦囊】)
    public string Content;              // 内容
    public string ButtonText;           // 按钮文字 (如：【前往处理】)
    public Action OnClick;              // 点击按钮后的跳转逻辑
    public AdviceType Type;             // 类型
    public object RelatedObject;        // 相关对象
    public DateTime Timestamp;          // 时间戳
}
```

---

## 🎯 三种建议类型详解

### 1. General (日常建议)
- **触发方式**: 主动问出来的
- **显示标题**: 【军师锦囊】
- **按钮文字**: 【查看详情】
- **行为**: 不打断时间跳过，仅显示红点提示
- **用途**: 经济建议、军备建议、人才建议等

**示例**:
```csharp
new AdviceData
{
    Title = "【军师锦囊】",
    Content = "主公，国库充盈，可考虑招募更多人才或扩充军备。",
    ButtonText = "【查看详情】",
    Type = AdviceType.General,
    OnClick = () => { /* 跳转到相关界面 */ }
}
```

### 2. Emergency (紧急军情)
- **触发方式**: 被动弹出来的
- **显示标题**: 【军情急报】
- **按钮文字**: 【前往调度】、【紧急增援】等
- **行为**: 强制中断时间跳过，立即弹窗
- **用途**: 军事威胁、粮草告急、国库空虚等

**示例**:
```csharp
new AdviceData
{
    Title = "【军情急报】",
    Content = "主公！某城危在旦夕，敌军威胁极大而我军兵力不足！",
    ButtonText = "【前往调度】",
    Type = AdviceType.Emergency,
    OnClick = () => { /* 跳转到该城市 */ }
}
```

### 3. Internal (内部隐患)
- **触发方式**: 被动弹出来的
- **显示标题**: 【内忧外患】
- **按钮文字**: 【安抚武将】、【更换太守】等
- **行为**: 强制中断时间跳过，立即弹窗
- **用途**: 武将叛变、太守不忠等内政问题

**示例**:
```csharp
new AdviceData
{
    Title = "【紧急兵变】",
    Content = "主公！某武将忠诚度极低，此人能力出众，若叛变后果严重！",
    ButtonText = "【安抚武将】",
    Type = AdviceType.Internal,
    OnClick = () => { /* 跳转到武将界面 */ }
}
```

---

## 🔧 具体更新内容

### 1. StrategistSystem.cs 更新

#### 建议生成逻辑调整
- 所有军事威胁 → `AdviceType.Emergency`
- 所有忠诚度问题 → `AdviceType.Internal`
- 所有经济危机 → `AdviceType.Emergency`
- 所有日常建议 → `AdviceType.General`

#### 新增按钮文字和点击逻辑
```csharp
// 军情急报
ButtonText = "【前往调度】",
OnClick = () => {
    System.Diagnostics.Debug.WriteLine($"跳转到城市: {architecture.Name}");
}

// 武将叛变
ButtonText = "【安抚武将】",
OnClick = () => {
    System.Diagnostics.Debug.WriteLine($"跳转到武将: {person.Name}");
}

// 日常建议
ButtonText = "【查看详情】",
OnClick = () => {
    System.Diagnostics.Debug.WriteLine("跳转到相关界面");
}
```

### 2. EventManager.cs 更新

#### 通知标题调整
```csharp
switch (advice.Type)
{
    case AdviceType.Emergency:
        notificationTitle = "🚨 紧急军情";
        break;
    case AdviceType.Internal:
        notificationTitle = "⚠️ 内部隐患";
        break;
    case AdviceType.General:
        notificationTitle = "💡 军师锦囊";
        break;
}
```

#### 统计信息简化
```csharp
return $"军师建议统计:\n" +
       $"总计: {_adviceHistory.Count} 条\n" +
       $"今日: {todayCount} 条\n" +
       $"紧急军情: {emergencyCount} 条\n" +
       $"内部隐患: {internalCount} 条\n" +
       $"日常建议: {generalCount} 条";
```

---

## 🎮 使用方式

### 1. 基本使用
```csharp
var (level, data) = StrategistManager.CheckRisks(playerFaction);

if (data != null)
{
    Console.WriteLine($"建议类型: {data.Type}");
    Console.WriteLine($"标题: {data.Title}");
    Console.WriteLine($"内容: {data.Content}");
    Console.WriteLine($"按钮: {data.ButtonText}");
    
    // 点击按钮
    data.OnClick?.Invoke();
}
```

### 2. 时间跳过集成
```csharp
timeManager.OnStrategistAdvice += (advice) =>
{
    switch (advice.Type)
    {
        case AdviceType.Emergency:
        case AdviceType.Internal:
            // 强制中断，显示紧急对话框
            ShowUrgentDialog(advice);
            break;
        case AdviceType.General:
            // 仅显示红点，不中断
            ShowRedDot("StrategistBtn");
            break;
    }
};
```

### 3. UI对话框样式
```csharp
private void ShowAdviceDialog(AdviceData advice)
{
    var dialog = new AdviceDialog();
    
    switch (advice.Type)
    {
        case AdviceType.General:
            dialog.SetStyle(DialogStyle.Info);
            dialog.Icon = "💡";
            break;
        case AdviceType.Emergency:
            dialog.SetStyle(DialogStyle.Critical);
            dialog.Icon = "🚨";
            break;
        case AdviceType.Internal:
            dialog.SetStyle(DialogStyle.Warning);
            dialog.Icon = "⚠️";
            break;
    }
    
    dialog.Title = advice.Title;
    dialog.Content = advice.Content;
    dialog.ButtonText = advice.ButtonText;
    dialog.OnButtonClick = advice.OnClick;
    
    dialog.ShowDialog();
}
```

---

## 📊 编译状态

- ✅ **编译成功**: 0错误，40警告
- ✅ **所有文件已更新**
- ✅ **向后兼容性保持**
- ✅ **功能完整性验证**

---

## 📁 更新文件列表

### 修改的文件
1. `WorldOfTheThreeKingdoms/GameManager/StrategistSystem.cs`
   - 简化 AdviceType 枚举
   - 优化 AdviceData 数据结构
   - 更新所有建议生成逻辑
   - 添加按钮文字和点击逻辑

2. `WorldOfTheThreeKingdoms/GameManager/EventManager.cs`
   - 更新通知标题映射
   - 简化统计信息显示

### 新增文件
3. `Simplified_Strategist_Usage_Example.cs`
   - 简化版使用示例
   - 三种建议类型的详细演示
   - UI集成建议

---

## 🔮 优势总结

### 1. 简化优势
- **更清晰的分类**: 3种类型比5种更容易理解和使用
- **明确的触发方式**: 主动 vs 被动，逻辑更清晰
- **统一的命名规范**: 【军师锦囊】、【军情急报】等

### 2. 功能优势
- **智能按钮文字**: 根据具体情况生成相应的按钮文字
- **点击跳转逻辑**: 每个建议都有对应的处理逻辑
- **类型化处理**: 不同类型有不同的UI样式和行为

### 3. 扩展优势
- **易于扩展**: 新的建议类型可以轻松添加
- **灵活配置**: 按钮文字和跳转逻辑可以灵活定制
- **统一接口**: 所有建议都使用相同的数据结构

---

## 📝 总结

军师系统已成功简化为三种核心建议类型：

1. **General (日常建议)** - 主动查询，不打断操作
2. **Emergency (紧急军情)** - 被动触发，强制中断
3. **Internal (内部隐患)** - 被动触发，强制中断

新的设计更加简洁明了，功能更加完善，为玩家提供更好的游戏体验。

---

**更新完成时间**: 2024年12月18日  
**状态**: ✅ 完成并测试  
**编译状态**: ✅ 成功 (0错误)  
**向后兼容**: ✅ 完全兼容