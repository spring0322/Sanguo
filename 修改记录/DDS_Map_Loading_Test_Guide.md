# DDS地图加载测试指南

## 🎯 测试目标
验证DDS地图文件能够正确加载，包括地图瓦片和小地图(AirView)。

## 🔧 实现的修改

### 1. 地图瓦片加载 (MainMapLayer.cs)
```csharp
// 使用DDS优先加载逻辑
string basePath = "Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number;

// 1. 优先尝试 DDS
if (File.Exists(basePath + ".dds"))
{
    maptile.TileTexture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, basePath + ".dds");
}

// 2. 如果 DDS 失败或不存在，尝试 PNG
if (maptile.TileTexture == null && File.Exists(basePath + ".png"))
{
    using (FileStream fs = new FileStream(basePath + ".png", FileMode.Open))
    {
        maptile.TileTexture = Texture2D.FromStream(Platform.GraphicsDevice, fs);
    }
}

// 3. 如果 PNG 也失败，回退到 JPG
if (maptile.TileTexture == null && File.Exists(basePath + ".jpg"))
{
    using (FileStream fs = new FileStream(basePath + ".jpg", FileMode.Open))
    {
        maptile.TileTexture = Texture2D.FromStream(Platform.GraphicsDevice, fs);
    }
}
```

### 2. 小地图加载 (AirViewPlugin.cs)
```csharp
// 使用DDS优先加载逻辑
string basePath = @"Content\Textures\Resources\ditu\_" + Path.GetFileNameWithoutExtension(dituwenjian);
Texture2D mapTexture = null;

// 1. 优先尝试 DDS
if (File.Exists(basePath + ".dds"))
{
    mapTexture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, basePath + ".dds");
}

// 2-3. PNG和JPG回退逻辑
// ... (类似地图瓦片的逻辑)

// 创建PlatformTexture包装并存储到缓存
if (mapTexture != null)
{
    this.airView.MapTexture = new PlatformTexture() 
    { 
        Name = basePath,
        Width = mapTexture.Width,
        Height = mapTexture.Height
    };
    CacheManager.TextureTempDics[basePath] = mapTexture;
}
```

## 📁 测试文件结构

### 地图瓦片文件
```
Content/Textures/Resources/ditu/
├── [MapName]/
│   ├── 1.dds     ← DDS格式瓦片 (优先)
│   ├── 1.png     ← PNG格式瓦片 (备用)
│   ├── 1.jpg     ← JPG格式瓦片 (兼容)
│   ├── 2.dds
│   ├── 2.png
│   └── ...
```

### 小地图文件
```
Content/Textures/Resources/ditu/
├── _[MapName].dds    ← DDS格式小地图 (优先)
├── _[MapName].png    ← PNG格式小地图 (备用)  
└── _[MapName].jpg    ← JPG格式小地图 (兼容)
```

## 🧪 测试步骤

### 测试1: 纯DDS地图
1. **准备**: 只放置DDS格式的地图文件
   - 地图瓦片: `Content/Textures/Resources/ditu/[MapName]/1.dds`, `2.dds`, ...
   - 小地图: `Content/Textures/Resources/ditu/_[MapName].dds`

2. **测试**: 启动游戏，加载该地图
3. **预期**: 地图正常显示，小地图正常显示

### 测试2: 混合格式地图
1. **准备**: 混合放置不同格式的地图文件
   - 部分瓦片用DDS: `1.dds`, `3.dds`
   - 部分瓦片用PNG: `2.png`, `4.png`
   - 部分瓦片用JPG: `5.jpg`
   - 小地图用DDS: `_[MapName].dds`

2. **测试**: 启动游戏，加载该地图
3. **预期**: 所有瓦片正常显示，优先使用DDS格式

### 测试3: 回退机制
1. **准备**: 故意损坏DDS文件或删除DDS文件
   - 删除部分DDS瓦片，保留PNG/JPG版本
   - 删除DDS小地图，保留PNG版本

2. **测试**: 启动游戏，加载该地图
3. **预期**: 自动回退到PNG/JPG，地图仍然正常显示

### 测试4: 向后兼容
1. **准备**: 只使用传统JPG格式
   - 地图瓦片: `1.jpg`, `2.jpg`, ...
   - 小地图: `_[MapName].jpg`

2. **测试**: 启动游戏，加载该地图
3. **预期**: 地图正常显示，完全兼容旧格式

## 🔍 调试信息

### 检查点1: 文件存在性
在代码中添加调试输出，确认文件路径正确：
```csharp
Console.WriteLine($"检查DDS文件: {basePath}.dds - 存在: {File.Exists(basePath + ".dds")}");
Console.WriteLine($"检查PNG文件: {basePath}.png - 存在: {File.Exists(basePath + ".png")}");
Console.WriteLine($"检查JPG文件: {basePath}.jpg - 存在: {File.Exists(basePath + ".jpg")}");
```

### 检查点2: DDS加载结果
```csharp
var ddsTexture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, fullPath);
Console.WriteLine($"DDS加载结果: {fullPath} - 成功: {ddsTexture != null}");
if (ddsTexture != null)
{
    Console.WriteLine($"DDS纹理尺寸: {ddsTexture.Width}x{ddsTexture.Height}");
}
```

### 检查点3: 最终纹理状态
```csharp
Console.WriteLine($"最终纹理状态: {maptile.TileTexture != null ? "成功" : "失败"}");
```

## ⚠️ 常见问题

### 问题1: DDS文件无法加载
- **原因**: DDS文件格式不支持或损坏
- **解决**: 检查DDS文件是否为支持的格式(DXT1/DXT3/DXT5)

### 问题2: 路径问题
- **原因**: 文件路径不正确
- **解决**: 确认文件放置在正确的目录结构中

### 问题3: 权限问题
- **原因**: 文件访问权限不足
- **解决**: 确认游戏有读取文件的权限

## 🎯 预期效果

### 性能提升
- **加载速度**: DDS格式应该比PNG/JPG加载更快
- **内存占用**: DDS压缩格式占用更少显存
- **渲染性能**: GPU原生支持，渲染更流畅

### 兼容性
- **完全向后兼容**: 现有JPG地图继续正常工作
- **渐进升级**: 可以逐步将地图转换为DDS格式
- **混合使用**: 支持DDS、PNG、JPG混合使用

## 📋 测试清单

- [ ] 纯DDS地图瓦片加载正常
- [ ] 纯DDS小地图加载正常
- [ ] 混合格式地图正常显示
- [ ] DDS优先级正确工作
- [ ] PNG回退机制正常
- [ ] JPG回退机制正常
- [ ] 向后兼容性完好
- [ ] 无编译错误
- [ ] 无运行时错误
- [ ] 性能有明显提升

完成这些测试后，DDS地图加载功能应该能够正常工作！