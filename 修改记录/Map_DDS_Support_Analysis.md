# 地图文件DDS格式支持分析

## 🎯 需要修改的源码文件

通过代码分析，发现以下文件需要修改以支持地图的DDS格式：

### 1. 🗺️ 主地图瓦片加载
**文件**: `WorldOfTheThreeKingdoms/MapLayers/MainMapLayer.cs`
**位置**: 第54行
**当前代码**:
```csharp
maptile.TileTexture = Platform.Current.LoadTexture("Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number + ".jpg", false);
```
**需要修改**: 移除硬编码的`.jpg`扩展名，让系统自动按优先级查找DDS/PNG/JPG

### 2. 🛰️ 小地图(AirView)加载  
**文件**: `WorldOfTheThreeKingdoms/GameScreens/MGSStartLoad.cs`
**位置**: 第727行
**当前代码**:
```csharp
this.Plugins.AirViewPlugin.ReloadAirView(Session.Current.Scenario.ScenarioMap.MapName + ".jpg");
```
**需要修改**: 移除硬编码的`.jpg`扩展名

### 3. 🔍 地图文件搜索逻辑
**文件**: `WorldOfTheThreeKingdoms/GameScreens/LoadingScreen.cs`  
**位置**: 第71行
**当前代码**:
```csharp
maps = Platform.Current.GetMODFiles(dir + "/", true).NullToEmptyArray().Where(fi => fi.EndsWith(".jpg")).NullToEmptyArray();
```
**需要修改**: 扩展搜索逻辑以包含DDS和PNG格式

### 4. 📁 全局常量定义
**文件**: `WorldOfTheThreeKingdoms/GameGlobal/GlobalStrings.cs`
**位置**: 第189行  
**当前代码**:
```csharp
public const string BackGroundMap_FileName = "BackGroundMap.jpg";
```
**需要修改**: 移除硬编码扩展名或改为优先级数组

## 🔧 具体修改方案

### 修改1: MainMapLayer.cs - 地图瓦片加载
```csharp
// 原代码
maptile.TileTexture = Platform.Current.LoadTexture("Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number + ".jpg", false);

// 修改后
string basePath = "Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number;
maptile.TileTexture = Platform.Current.LoadTexture(basePath, false);
```

### 修改2: MGSStartLoad.cs - 小地图加载
```csharp
// 原代码  
this.Plugins.AirViewPlugin.ReloadAirView(Session.Current.Scenario.ScenarioMap.MapName + ".jpg");

// 修改后
this.Plugins.AirViewPlugin.ReloadAirView(Session.Current.Scenario.ScenarioMap.MapName);
```

### 修改3: LoadingScreen.cs - 地图文件搜索
```csharp
// 原代码
maps = Platform.Current.GetMODFiles(dir + "/", true).NullToEmptyArray().Where(fi => fi.EndsWith(".jpg")).NullToEmptyArray();

// 修改后
maps = Platform.Current.GetMODFiles(dir + "/", true).NullToEmptyArray()
    .Where(fi => fi.EndsWith(".dds") || fi.EndsWith(".png") || fi.EndsWith(".jpg"))
    .NullToEmptyArray();
```

### 修改4: AirViewPlugin.cs - 小地图纹理加载
**文件**: `WorldOfTheThreeKingdoms/GamePlugins/AirViewPlugin/AirViewPlugin.cs`
**位置**: 第170行
```csharp
// 原代码
this.airView.MapTexture = CacheManager.GetTempTexture(@"Content\Textures\Resources\ditu\_" + dituwenjian);

// 修改后 - 需要检查CacheManager.GetTempTexture是否支持格式优先级
// 如果不支持，需要修改为：
string basePath = @"Content\Textures\Resources\ditu\_" + Path.GetFileNameWithoutExtension(dituwenjian);
this.airView.MapTexture = CacheManager.GetTempTexture(basePath);
```

## ⚠️ 注意事项

### 1. CacheManager.GetTempTexture方法
需要检查这个方法是否使用了我们的新LoadTexture逻辑，如果没有，需要修改：

```csharp
public static PlatformTexture GetTempTexture(string name)
{
    // 确保使用支持DDS的加载逻辑
    return new PlatformTexture() { Name = name };
}
```

### 2. 地图文件命名约定
- **地图瓦片**: `{MapName}/{number}.dds` (优先) → `{MapName}/{number}.png` → `{MapName}/{number}.jpg`
- **小地图**: `_{MapName}.dds` (优先) → `_{MapName}.png` → `_{MapName}.jpg`

### 3. 向后兼容性
所有修改都保持向后兼容，现有的JPG地图文件仍然可以正常使用。

## 🚀 修改的好处

### 1. 性能提升
- **DDS格式**: 硬件压缩，加载速度更快
- **内存效率**: 压缩格式减少显存占用
- **渲染性能**: GPU原生支持，渲染更流畅

### 2. 文件大小优化
- **DDS压缩**: 比PNG/JPG更小的文件大小
- **加载时间**: 减少磁盘IO时间
- **网络传输**: 如果有在线内容，传输更快

### 3. 视觉质量
- **无损压缩**: DDS可以提供更好的压缩质量
- **Mipmap支持**: 自动生成多级细节，远距离观看更清晰

## 📋 实施步骤

1. **修改MainMapLayer.cs** - 地图瓦片加载逻辑
2. **修改MGSStartLoad.cs** - 小地图加载调用
3. **修改LoadingScreen.cs** - 地图文件搜索逻辑  
4. **修改AirViewPlugin.cs** - 小地图纹理加载
5. **测试验证** - 确保DDS/PNG/JPG都能正常加载
6. **性能测试** - 验证DDS格式的性能提升

## 🎯 预期效果

修改完成后，游戏将能够：
- ✅ 优先加载DDS格式的地图文件
- ✅ 自动回退到PNG/JPG格式
- ✅ 保持完全的向后兼容性
- ✅ 获得更好的加载性能和视觉效果

这些修改将使地图系统与头像系统一样，都支持统一的DDS优先级加载机制！