# Viewport画面问题诊断报告

## 问题概述
通过搜索代码中的Viewport赋值语句，发现了几个可能导致画面显示问题的地方，主要涉及固定分辨率值和错误的缩放比例计算。

## 发现的问题

### 1. CloudLayer.cs中的固定缩放比例
**文件**: `WorldOfTheThreeKingdoms/MapLayers/CloudLayer.cs` (第56行)
**问题**: 使用了硬编码的800f和480f作为基准分辨率

```csharp
// 问题代码
scale = new Vector2(Convert.ToSingle(Session.ResolutionX) / 800f, Convert.ToSingle(Session.ResolutionY) / 480f);
```

**影响**: 
- 当实际分辨率与800x480不匹配时，云层效果会出现缩放错误
- 可能导致云层显示过大或过小，影响视觉效果

### 2. DantiaoLayer.cs中的注释掉的缩放代码
**文件**: `WorldOfTheThreeKingdoms/MapLayers/DantiaoLayer.cs` (第367行)
**问题**: 相同的800f和480f硬编码问题（虽然被注释了）

```csharp
// 注释掉的问题代码
//scale = new Vector2(Convert.ToSingle(Session.ResolutionX) / 800f, Convert.ToSingle(Session.ResolutionY) / 480f);
```

### 3. 平台特定的PreferResolution设置
**文件**: 
- `WorldOfTheThreeKingdoms/Platforms/PlatformWin.cs` (第47行): `"1368*768"`
- `WorldOfTheThreeKingdoms/Platforms/PlatformDesktop.cs` (第42行): `"1280*720"`
- `WorldOfTheThreeKingdoms/Platforms/PlatformiOS.cs` (第107行): `"925*520"`

**问题**: 不同平台使用不同的默认分辨率，可能导致跨平台显示不一致

### 4. MainGameScreen.cs中的viewportSize计算
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs` (第5419-5425行)
**问题**: 不同平台使用不同的viewport计算方式

```csharp
if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
{
    this.viewportSize.X = Platform.GraphicsDevice.Viewport.Width;
    this.viewportSize.Y = Platform.GraphicsDevice.Viewport.Height - this.Plugins.ToolBarPlugin.Height;
}
else
{
    this.viewportSize.X = Session.ResolutionX - 20;  // 硬编码的-20偏移
    this.viewportSize.Y = Convert.ToInt32(Session.ResolutionY - this.Plugins.ToolBarPlugin.Height - 10);  // 硬编码的-10偏移
}
```

### 5. PlatformUWP.cs中的Viewport直接赋值
**文件**: `WorldOfTheThreeKingdoms/Platforms/PlatformUWP.cs` (第140行)
**代码**: 
```csharp
GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
```

**状态**: 这个看起来是正确的，使用传入的width和height参数

## 潜在的根本原因

### 1. 硬编码的基准分辨率
- CloudLayer使用800x480作为基准分辨率进行缩放计算
- 当实际分辨率不是这个比例时，会导致显示异常

### 2. 平台差异化处理
- 不同平台使用不同的viewport计算逻辑
- 可能导致在某些平台上显示不正确

### 3. 固定偏移值
- 使用硬编码的-20和-10偏移值
- 可能在不同分辨率下不适用

## 建议的修复方案

### 1. 修复CloudLayer.cs中的缩放计算
```csharp
// 建议修复
// 使用动态基准分辨率或者移除硬编码值
scale = Vector2.One; // 或者使用更合适的缩放逻辑
```

### 2. 统一viewport计算逻辑
```csharp
// 建议统一使用GraphicsDevice.Viewport的值
this.viewportSize.X = Platform.GraphicsDevice.Viewport.Width;
this.viewportSize.Y = Platform.GraphicsDevice.Viewport.Height - this.Plugins.ToolBarPlugin.Height;
```

### 3. 移除硬编码偏移值
- 将-20和-10这样的硬编码偏移值改为可配置或动态计算

## 优先级建议

1. **高优先级**: 修复CloudLayer.cs中的硬编码缩放比例
2. **中优先级**: 统一MainGameScreen.cs中的viewport计算逻辑
3. **低优先级**: 检查和优化平台特定的PreferResolution设置

## 测试建议

修复后应该在以下情况下测试：
1. 不同分辨率下的显示效果
2. 不同平台的显示一致性
3. 窗口大小改变时的适应性
4. 全屏和窗口模式切换

这些修复应该能解决由于Viewport设置错误导致的画面显示问题。