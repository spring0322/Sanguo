# 🪟 可拖拽军师UI系统 - 集成指南

## 概述

这是一个300x180像素的可拖拽悬浮窗军师UI系统，采用现代游戏UI设计理念，提供流畅的拖拽体验和实时建议更新功能。

## 🎯 核心特性

### 1. 可拖拽悬浮窗
- **尺寸**: 300x180像素，紧凑不遮挡
- **拖拽**: 点击标题栏拖拽移动
- **边界限制**: 自动防止拖出屏幕外
- **视觉反馈**: 拖拽时标题栏变亮

### 2. 立体视觉效果
- **阴影**: 半透明阴影增强立体感
- **透明度**: 主体微透明，不完全遮挡背景
- **边框**: 金色边框突出重要性
- **颜色**: 深色主题，护眼舒适

### 3. 智能文本处理
- **自动换行**: 根据窗口宽度自动换行
- **实时更新**: UpdateAdvice()方法动态更新内容
- **字体容错**: 支持FontS和Fonts/FontS路径

## 🔧 完整集成步骤

### 步骤1: 在MainGameScreen中添加字段

```csharp
public class MainGameScreen : GameScreen
{
    // 添加军师UI字段
    private StrategistUI strategistUI;
    
    // ... 其他字段 ...
}
```

### 步骤2: 在LoadContent中初始化

```csharp
protected override void LoadContent()
{
    base.LoadContent();
    
    // ... 其他初始化代码 ...
    
    // 初始化军师UI
    strategistUI = new StrategistUI();
    strategistUI.LoadContent(Content, GraphicsDevice);
    
    // 设置初始建议
    strategistUI.UpdateAdvice("军师已就位，随时为主公提供建议。");
}
```

### 步骤3: 在Update中更新

```csharp
protected override void Update(GameTime gameTime)
{
    // ... 其他更新逻辑 ...
    
    // 更新军师UI
    if (strategistUI != null)
    {
        strategistUI.Update(gameTime);
        
        // 根据游戏状态动态更新建议
        UpdateStrategistAdvice();
    }
    
    base.Update(gameTime);
}

// 动态更新军师建议的示例方法
private void UpdateStrategistAdvice()
{
    // 示例1: 根据时间更新
    if (gameTime.TotalGameTime.TotalSeconds % 10 < 0.1) // 每10秒更新一次
    {
        string timeAdvice = $"当前时间: {DateTime.Now:HH:mm}，适合处理内政。";
        strategistUI.UpdateAdvice(timeAdvice);
    }
    
    // 示例2: 根据游戏状态更新
    if (currentPlayerFaction != null)
    {
        if (currentPlayerFaction.Fund > 50000)
        {
            strategistUI.UpdateAdvice("主公，府库充盈，正是招贤纳士、扩充军备的好时机！");
        }
        else if (currentPlayerFaction.Fund < 1000)
        {
            strategistUI.UpdateAdvice("主公，国库空虚，建议加强商业发展，增加税收。");
        }
    }
    
    // 示例3: 根据事件更新
    if (isInBattle)
    {
        strategistUI.UpdateAdvice("战事紧急！建议集中兵力，速战速决！");
    }
}
```

### 步骤4: 在Draw中绘制 (关键：绘制顺序)

```csharp
protected override void Draw(GameTime gameTime)
{
    // ... 游戏原本的各种绘制代码 ...
    
    // 假设游戏原本的绘制结束了
    // spriteBatch.End();  <-- 确保这里已经结束了之前的绘制
    
    base.Draw(gameTime); // 这里的 base.Draw 可能会清屏或做其他事
    
    // ✅ 把军师UI放在【绝对的最后一行】
    // 并且不要在外面写 spriteBatch.Begin()，因为 StrategistUI 内部写了
    if (this.strategistUI != null)
    {
        this.strategistUI.Draw(this.spriteBatch);
    }
}
```

**⚠️ 重要注意事项**：
- **绝对最后绘制**: 军师UI必须在所有其他绘制完成后才绘制
- **不要外部Begin**: StrategistUI内部已经有SpriteBatch.Begin/End，不要在外面再包装
- **在base.Draw之后**: 确保在基类绘制完成后再绘制UI，避免被覆盖

### 步骤5: 在UnloadContent中清理

```csharp
protected override void UnloadContent()
{
    // 清理军师UI资源
    strategistUI?.UnloadContent();
    
    // ... 其他清理代码 ...
    
    base.UnloadContent();
}
```

## 🎮 高级用法示例

### 1. 与军师系统集成

```csharp
// 在回合开始时获取军师建议
private void OnTurnStart()
{
    if (currentPlayerFaction != null && strategistUI != null)
    {
        // 调用军师系统获取建议
        var advice = StrategistManager.AskForAdvice(currentPlayerFaction);
        if (advice != null)
        {
            strategistUI.UpdateAdvice(advice.Content);
        }
    }
}
```

### 2. 与时间跳过系统集成

```csharp
// 在时间跳过过程中更新建议
private void OnTimeSkipping(int currentDay, int totalDays)
{
    string skipAdvice = $"时间跳过中... ({currentDay}/{totalDays})";
    strategistUI.UpdateAdvice(skipAdvice);
}

// 时间跳过被中断时
private void OnTimeSkipInterrupted(AdviceData criticalAdvice)
{
    strategistUI.UpdateAdvice($"紧急情况！{criticalAdvice.Content}");
}
```

### 3. 与AI决策系统集成

```csharp
// 在AI决策后更新建议
private void OnAIDecisionMade(string decision)
{
    string aiAdvice = $"AI建议: {decision}";
    strategistUI.UpdateAdvice(aiAdvice);
}
```

## 🎨 自定义选项

### 1. 修改窗口尺寸

```csharp
// 在StrategistUI.cs中修改常量
private const int Width = 400;  // 默认300
private const int Height = 200; // 默认180
```

### 2. 修改初始位置

```csharp
// 在LoadContent中修改初始位置
panelPosition = new Vector2(50, 50); // 左上角
// 或
panelPosition = new Vector2(device.Viewport.Width / 2 - Width / 2, 100); // 顶部居中
```

### 3. 修改颜色主题

```csharp
// 修改背景色
spriteBatch.Draw(pixelTexture, bodyRect, new Color(50, 50, 60, 240)); // 更蓝的主题

// 修改标题栏颜色
Color headerColor = isDragging ? new Color(120, 60, 0) : new Color(80, 40, 0); // 更橙的主题
```

## 🔍 故障排除

### 常见问题

1. **UI不显示**
   - 检查字体文件是否存在 (FontS 或 Fonts/FontS)
   - 确保在Draw方法最后调用strategistUI.Draw()

2. **拖拽不响应**
   - 确保Update方法正确调用strategistUI.Update()
   - 检查鼠标输入是否被其他UI组件拦截

3. **文字显示异常**
   - 检查字体文件是否支持中文字符
   - 尝试使用不同的字体文件 (FontL, FontT等)

4. **性能问题**
   - 避免在Update中频繁调用UpdateAdvice()
   - 建议使用定时器或事件驱动的方式更新建议

### 调试技巧

```csharp
// 添加调试信息
public void Update(GameTime gameTime)
{
    // ... 更新逻辑 ...
    
    // 调试输出
    System.Diagnostics.Debug.WriteLine($"军师UI位置: {panelPosition}, 拖拽状态: {isDragging}");
}
```

## 📊 性能特性

- **内存使用**: 极低，仅使用1x1像素纹理
- **CPU开销**: 最小化，仅在拖拽时进行位置计算
- **GPU开销**: 优化的绘制调用，使用批处理渲染
- **兼容性**: 完全兼容MonoGame框架

## 🎉 总结

这个可拖拽军师UI系统提供了：

- ✅ **现代化UI体验**: 流畅的拖拽交互
- ✅ **灵活的集成方式**: 简单的API调用
- ✅ **强大的自定义能力**: 可调整尺寸、颜色、位置
- ✅ **稳定的性能表现**: 基于小地图模式的成熟架构
- ✅ **完善的错误处理**: 优雅的降级机制

这是一个可以直接用于生产环境的企业级UI组件！