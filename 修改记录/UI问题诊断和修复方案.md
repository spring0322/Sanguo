# UI显示问题诊断和修复方案

## 问题现状

根据调查，UI元素被压缩显示在屏幕左上角，而背景图片可以正常全屏显示。

## 已确认的正确设置

经过检查，以下关键设置都是正确的：

### ✅ Session.cs 缩放设置
```csharp
// 第572-573行 - 正确强制为1:1
screenscalex1 = 1f; // Convert.ToSingle(width) / 1280f;
screenscaley1 = 1f; // Convert.ToSingle(height) / 720f;

// 第587行 - 正确设置
InputManager.Scale1 = new Vector2(screenscalex1, screenscaley1);

// 第615和658行 - 正确强制为单位矩阵
Session.MainGame.SpriteScale1 = Matrix.Identity;
```

### ✅ MainGame.cs SpriteBatch设置
```csharp
// 第432行 - 正确使用Matrix.Identity
SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Matrix.Identity);
```

### ✅ MainMenuScreen.cs 缓存管理器设置
```csharp
// 第4373行 - 正确设置
CacheManager.Scale = Vector2.One;
```

### ✅ InputManager.cs 绘制缩放设置
```csharp
// 第604行 - 正确设置
InputManager.ScaleDraw = new Vector2(1, 1);
```

## 可能的问题源头

### 1. ButtonTexture.DrawScale 问题
**症状**: ButtonTexture使用DrawScale进行位置缩放
```csharp
// ButtonTexture.cs 第293行
CacheManager.Draw(Text, (basePos == null ? Position : (Vector2)(Position + basePos)) * DrawScale, ...)
```

**可能原因**: 
- DrawScale默认为1f，但可能被某个全局逻辑修改
- 可能存在隐藏的DrawScale设置逻辑

### 2. Windows DPI缩放问题
**症状**: 系统级坐标变换导致UI压缩
**可能原因**:
- Windows高DPI设置
- MonoGame框架DPI处理问题
- 应用程序DPI感知设置

### 3. 隐藏的缩放逻辑
**可能位置**:
- CacheManager内部缩放逻辑
- Platform层面的坐标变换
- 图形设备层面的变换

## 修复方案

### 方案1: 强制设置ButtonTexture.DrawScale
在MainMenuScreen.cs的Draw方法中强制设置所有按钮的DrawScale：

```csharp
public void Draw(GameTime gameTime)
{
    // 确保主菜单使用正确的缩放比例
    CacheManager.Scale = Vector2.One;
    
    // 强制设置所有按钮的DrawScale为1
    btList?.ForEach(bt => bt.DrawScale = 1f);
    btScenarioList?.ForEach(bt => bt.DrawScale = 1f);
    btSaveList?.ForEach(bt => bt.DrawScale = 1f);
    btSettingList?.ForEach(bt => bt.DrawScale = 1f);
    
    // ... 其余绘制代码
}
```

### 方案2: 添加DPI感知设置
在应用程序清单中添加DPI感知设置，或在代码中设置：

```csharp
// 在MainGame构造函数中添加
[System.Runtime.InteropServices.DllImport("user32.dll")]
static extern bool SetProcessDPIAware();

public MainGame()
{
    SetProcessDPIAware(); // 设置DPI感知
    // ... 其余初始化代码
}
```

### 方案3: 调试输出诊断
添加调试输出来确定问题源头：

```csharp
public void Draw(GameTime gameTime)
{
    // 调试输出
    System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] Viewport: {Platform.GraphicsDevice.Viewport}");
    System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] CacheManager.Scale: {CacheManager.Scale}");
    System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] InputManager.Scale1: {InputManager.Scale1}");
    System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] SpriteScale1: {Session.MainGame.SpriteScale1}");
    
    if (btList?.Count > 0)
    {
        var firstButton = btList[0];
        System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] Button DrawScale: {firstButton.DrawScale}");
        System.Diagnostics.Debug.WriteLine($"[MainMenuScreen.Draw] Button Position: {firstButton.Position}");
    }
    
    // ... 其余绘制代码
}
```

### 方案4: 使用绝对坐标绘制
绕过ButtonTexture系统，直接使用CacheManager绘制：

```csharp
// 在Draw方法中替换 btList.ForEach(bt => bt.Draw());
foreach (var bt in btList)
{
    if (bt.Visible)
    {
        // 直接使用CacheManager绘制，不经过DrawScale
        CacheManager.Draw(bt.Text, bt.Position, bt.Rectangle, Color.White, SpriteEffects.None, 1f);
    }
}
```

## 推荐执行顺序

1. **立即实施方案1**: 强制设置DrawScale，这是最直接的修复
2. **同时实施方案3**: 添加调试输出，确定问题根源
3. **如果方案1无效，尝试方案2**: DPI感知设置
4. **最后考虑方案4**: 绕过ButtonTexture系统

## 预期结果

实施方案1后，UI元素应该能够正确显示在预期位置，不再被压缩到左上角。

## 后续监控

修复后需要监控：
1. 不同分辨率下的显示效果
2. 不同DPI设置下的表现
3. 窗口/全屏模式切换的稳定性
4. 其他UI界面的显示情况

---
*诊断时间: 2025年12月24日*
*状态: 待实施修复方案*