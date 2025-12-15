# 🎖️ 军师UI系统集成指南

## 📋 概述

本指南说明如何将MonoGame版本的军师UI系统集成到《三国志》游戏的MainGameScreen中，实现右上角军师按钮和建议弹窗功能。

## 🔧 集成步骤

### 1. 在MainGameScreen中添加军师UI实例

在 `MainGameScreen.cs` 的字段声明区域添加：

```csharp
// 军师UI系统
private StrategistUI _strategistUI;
```

### 2. 在构造函数中初始化

在 `MainGameScreen()` 构造函数中添加：

```csharp
// 初始化军师UI系统
_strategistUI = new StrategistUI();
```

### 3. 在LoadContent方法中加载资源

如果有LoadContent方法，添加以下代码：

```csharp
// 加载军师UI资源
// 注意：这些纹理需要从游戏现有资源中获取或创建简单的1x1白色纹理
Texture2D buttonTexture = CreateWhitePixelTexture(); // 创建简单纹理
Texture2D panelTexture = CreateWhitePixelTexture();
Texture2D closeButtonTexture = CreateWhitePixelTexture();
SpriteFont titleFont = this.Textures.DefaultFont; // 使用游戏现有字体
SpriteFont contentFont = this.Textures.DefaultFont;

_strategistUI.LoadContent(buttonTexture, panelTexture, closeButtonTexture, titleFont, contentFont);
_strategistUI.Initialize(base.viewportSize);
```

### 4. 在Update方法中更新UI

在主Update方法中添加：

```csharp
// 更新军师UI
if (_strategistUI != null)
{
    MouseState mouseState = Mouse.GetState();
    Faction currentFaction = Session.Current.Scenario.CurrentPlayer;
    _strategistUI.Update(gameTime, mouseState, currentFaction);
}
```

### 5. 在Draw方法中绘制UI

在 `Drawing(GameTime gameTime)` 方法的最后，在绘制鼠标箭头之前添加：

```csharp
// 绘制军师UI（在最上层）
if (_strategistUI != null)
{
    _strategistUI.Draw(Session.MainGame.SpriteBatch);
}
```

### 6. 创建简单纹理的辅助方法

添加以下辅助方法：

```csharp
/// <summary>
/// 创建1x1白色纹理用于UI绘制
/// </summary>
private Texture2D CreateWhitePixelTexture()
{
    Texture2D texture = new Texture2D(Platform.MainGame.GraphicsDevice, 1, 1);
    texture.SetData(new[] { Color.White });
    return texture;
}
```

### 7. 在Dispose方法中清理资源

如果有Dispose方法，添加：

```csharp
// 清理军师UI
_strategistUI?.Dispose();
```

## 🎮 UI布局设计

### 军师按钮位置
- **位置**: 屏幕右上角
- **尺寸**: 80x30 像素
- **边距**: 距离屏幕边缘10像素
- **文字**: "军师"
- **颜色**: 正常状态白色，悬停状态浅灰色

### 建议面板位置
- **位置**: 屏幕中央
- **尺寸**: 400x250 像素
- **背景**: 半透明黑色背景 + 金色边框
- **关闭按钮**: 面板右上角20x20像素红色按钮

## 🔄 交互逻辑

### 主动咨询流程
1. 玩家点击右上角"军师"按钮
2. 调用 `StrategistManager.AskForAdvice(currentFaction)`
3. 显示日常建议面板
4. 玩家点击"退下"或面板外区域关闭

### 被动建议流程
1. 回合开始时 `TurnManager.OnTurnStart()` 调用
2. `StrategistManager.CheckCriticalRisks()` 检测风险
3. 如发现紧急情况，触发 `EventManager.TriggerStrategistEvent()`
4. StrategistUI自动接收事件并显示紧急建议面板
5. 玩家处理建议后关闭面板

## 🎨 视觉效果

### 按钮状态
- **正常**: 白色背景，黑色文字
- **悬停**: 浅灰色背景，黑色文字
- **点击**: 短暂变暗效果

### 面板样式
- **紧急建议**: 红色标题，金色边框
- **日常建议**: 金色标题，金色边框
- **背景**: 半透明遮罩，突出面板内容

## 🔧 自定义选项

### 位置调整
可以通过修改 `Initialize()` 方法中的坐标来调整按钮位置：

```csharp
// 调整到右上角其他位置
_strategistButtonRect = new Rectangle(
    viewportSize.X - buttonWidth - customMarginX,
    customMarginY,
    buttonWidth,
    buttonHeight
);
```

### 样式自定义
可以通过修改颜色常量来调整UI样式：

```csharp
private Color _normalColor = Color.LightBlue;      // 自定义按钮颜色
private Color _emergencyColor = Color.Orange;      // 自定义紧急颜色
private Color _panelBackgroundColor = new Color(50, 50, 100, 220); // 自定义面板背景
```

## 📝 注意事项

1. **资源管理**: 确保正确加载和释放纹理资源
2. **性能优化**: UI绘制在最上层，避免过度绘制
3. **输入处理**: 确保UI交互不与游戏其他输入冲突
4. **字体兼容**: 使用游戏现有字体资源确保一致性
5. **分辨率适配**: UI位置应根据不同分辨率自动调整

## 🚀 扩展功能

### 可选增强
1. **动画效果**: 面板淡入淡出动画
2. **音效支持**: 按钮点击和建议弹出音效
3. **多语言支持**: 根据游戏语言设置显示对应文本
4. **快捷键支持**: 键盘快捷键打开军师面板
5. **历史记录**: 保存最近的军师建议历史

### 高级集成
1. **与现有UI系统集成**: 使用游戏原有的UI框架
2. **主题系统**: 支持不同的UI主题和皮肤
3. **自适应布局**: 根据屏幕尺寸自动调整UI元素大小
4. **无障碍支持**: 添加屏幕阅读器支持和高对比度模式

## 🎯 完成效果

集成完成后，玩家将看到：
- 右上角有一个"军师"按钮，不遮挡游戏界面
- 点击按钮可主动咨询军师获得建议
- 回合开始时如有紧急情况会自动弹出建议
- 建议面板居中显示，背景半透明，不影响游戏体验
- 支持鼠标交互，操作简单直观

这样就完成了一个完整的军师UI系统集成！🎉