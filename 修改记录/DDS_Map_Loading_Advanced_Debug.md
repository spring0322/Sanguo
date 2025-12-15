# DDS地图加载高级调试指南

## 🔍 深入问题分析

经过进一步分析，我发现了几个可能的问题点：

### 1. 地图文件结构假设
我们假设DDS地图文件的结构是：
```
Content/Textures/Resources/ditu/{MapName}/{number}.dds
```

但实际的文件结构可能不同。

### 2. 地图加载时机
地图瓦片的加载有两个路径：
- 主线程中的`CheckMapTileTexture`调用（绘制时）
- 后台线程中的`ProcessMapTileTextureSync`调用（异步加载）

### 3. 可能的问题点
1. **MapName值不正确**
2. **文件路径构建错误**
3. **DDS文件不存在于预期位置**
4. **DDSLoader本身有问题**
5. **线程安全问题**

## 🧪 高级调试步骤

### 步骤1：验证基本信息
运行游戏并查看调试输出，确认：
- MapName的实际值
- TileNumber的实际值
- 构建的完整路径

### 步骤2：手动验证文件存在性
根据调试输出的路径，手动检查以下文件是否存在：
```
Content/Textures/Resources/ditu/{实际MapName}/0.dds
Content/Textures/Resources/ditu/{实际MapName}/1.dds
Content/Textures/Resources/ditu/{实际MapName}/2.dds
```

### 步骤3：验证DDS文件格式
使用DDS查看工具检查文件：
- 文件是否为有效的DDS格式
- 是否为支持的压缩格式（DXT1/DXT3/DXT5）
- 文件大小是否合理

### 步骤4：测试简化版本
创建一个最简单的测试：

```csharp
// 在CheckMapTileTexture方法开始处添加
if (maptile.number == "0") // 只测试第一个瓦片
{
    string testPath = "Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/0.dds";
    System.Diagnostics.Debug.WriteLine($"测试路径: {testPath}");
    System.Diagnostics.Debug.WriteLine($"文件存在: {File.Exists(testPath)}");
    
    if (File.Exists(testPath))
    {
        try
        {
            var testTexture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, testPath);
            System.Diagnostics.Debug.WriteLine($"DDS加载结果: {testTexture != null}");
            if (testTexture != null)
            {
                System.Diagnostics.Debug.WriteLine($"纹理尺寸: {testTexture.Width}x{testTexture.Height}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DDS加载异常: {ex.Message}");
        }
    }
}
```

## 🔧 可能的解决方案

### 方案1：路径问题
如果路径不正确，可能需要：
- 检查实际的地图文件存放位置
- 修改路径构建逻辑
- 确认MapName的格式

### 方案2：文件格式问题
如果DDS文件格式不支持：
- 重新生成DDS文件
- 使用不同的压缩格式
- 检查文件头信息

### 方案3：加载时机问题
如果是线程安全问题：
- 在主线程中测试加载
- 添加线程同步机制
- 检查GraphicsDevice的线程安全性

### 方案4：回退到已知工作的方案
如果DDS加载确实有问题：
- 暂时使用PNG格式测试
- 确认PNG版本能正常工作
- 逐步调试DDS支持

## 📋 调试检查清单

请按顺序检查以下项目：

- [ ] 游戏启动时是否有调试输出
- [ ] MapName的值是否正确
- [ ] 构建的文件路径是否正确
- [ ] DDS文件是否存在于该路径
- [ ] DDS文件是否为有效格式
- [ ] DDSLoader是否被调用
- [ ] DDSLoader是否返回有效纹理
- [ ] 纹理是否正确设置到MapTile
- [ ] 绘制时是否使用了正确的纹理

## 🚨 紧急回退方案

如果问题无法快速解决，可以使用以下回退方案：

### 临时回退到PNG
```csharp
// 在CheckMapTileTexture中暂时禁用DDS
// 注释掉DDS加载部分，直接尝试PNG
if (File.Exists(basePath + ".png"))
{
    using (FileStream fs = new FileStream(basePath + ".png", FileMode.Open))
    {
        maptile.TileTexture = Texture2D.FromStream(Platform.GraphicsDevice, fs);
    }
}
```

### 完全回退到原始方案
使用之前提供的回退方案，恢复到CacheManager.GetTempTexture的原始实现。

## 📞 需要的反馈信息

请提供以下调试信息：

1. **完整的调试输出**（从游戏启动到地图加载）
2. **实际的地图文件结构**（截图或文件列表）
3. **MapName的实际值**
4. **是否有任何异常或错误消息**
5. **PNG格式的地图是否能正常工作**

这些信息将帮助我准确定位问题并提供针对性的解决方案。