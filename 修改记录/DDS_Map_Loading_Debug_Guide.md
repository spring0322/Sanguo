# DDS地图加载调试指南

## 🔍 已添加的调试信息

我已经在关键位置添加了调试输出，帮助诊断DDS地图加载问题：

### 1. DDSLoader.cs 调试输出
- **文件不存在**: `"DDS文件不存在: {filePath}"`
- **加载成功**: `"DDS加载成功: {filePath}, 尺寸: {texWidth}x{texHeight}"`
- **加载失败**: `"DDS加载失败: {filePath}, 错误: {ex.Message}"`

### 2. MainMapLayer.cs 调试输出
- **尝试加载**: `"尝试加载地图瓦片: {basePath}"`

## 🎯 如何使用调试信息

### 1. 启动游戏并查看调试输出
在Visual Studio中：
1. 启动游戏（Debug模式）
2. 打开 **输出** 窗口
3. 选择 **调试** 输出源
4. 加载一个地图，观察输出信息

### 2. 分析调试输出

#### 情况A：没有看到任何调试输出
**可能原因**：
- CheckMapTileTexture方法没有被调用
- 地图加载使用了其他路径

**解决方案**：
- 检查地图是否正确加载
- 确认MapName属性是否正确设置

#### 情况B：看到"尝试加载地图瓦片"但没有DDS相关输出
**可能原因**：
- DDS文件不存在
- 路径不正确

**解决方案**：
- 检查DDS文件是否存在于正确路径
- 确认文件命名是否正确

#### 情况C：看到"DDS文件不存在"
**可能原因**：
- DDS文件确实不存在
- 路径构建有问题
- 文件名不匹配

**解决方案**：
- 检查文件路径：`Content/Textures/Resources/ditu/{MapName}/{number}.dds`
- 确认MapName和number值是否正确
- 检查文件是否存在

#### 情况D：看到"DDS加载失败"
**可能原因**：
- DDS文件格式不支持
- 文件损坏
- DDSLoader有bug

**解决方案**：
- 检查错误消息详情
- 尝试不同的DDS文件
- 检查DDS文件格式是否为DXT1/DXT3/DXT5

#### 情况E：看到"DDS加载成功"但地图仍然不显示
**可能原因**：
- 纹理创建成功但绘制有问题
- 其他渲染问题

**解决方案**：
- 检查绘制代码
- 确认纹理是否正确设置到MapTile

## 📁 检查文件结构

确保你的DDS文件按以下结构存放：

```
Content/
└── Textures/
    └── Resources/
        └── ditu/
            ├── {MapName}/
            │   ├── 0.dds
            │   ├── 1.dds
            │   ├── 2.dds
            │   └── ...
            └── _{MapName}.dds  (小地图)
```

## 🔧 常见问题排查

### 1. 路径问题
- 确认MapName不包含扩展名
- 检查路径分隔符（Windows使用`\`或`/`都可以）
- 确认大小写匹配

### 2. 文件格式问题
- 确认DDS文件是DXT1、DXT3或DXT5格式
- 避免使用DX10格式的DDS文件
- 确认文件没有损坏

### 3. 权限问题
- 确认游戏有读取文件的权限
- 检查文件是否被其他程序锁定

## 📋 测试步骤

### 步骤1：基本测试
1. 启动游戏（Debug模式）
2. 加载一个地图
3. 查看调试输出
4. 记录所有相关信息

### 步骤2：文件存在性测试
1. 根据调试输出中的路径
2. 手动检查DDS文件是否存在
3. 确认文件大小不为0

### 步骤3：格式测试
1. 尝试用其他工具打开DDS文件
2. 确认文件格式正确
3. 如果可能，尝试重新生成DDS文件

### 步骤4：回退测试
1. 将DDS文件重命名为.dds.bak
2. 放置同名的PNG或JPG文件
3. 确认PNG/JPG文件能正常加载

## 🎯 预期的正常输出

如果一切正常，你应该看到类似这样的输出：

```
尝试加载地图瓦片: Content/Textures/Resources/ditu/MapName/0
DDS加载成功: Content/Textures/Resources/ditu/MapName/0.dds, 尺寸: 512x512
尝试加载地图瓦片: Content/Textures/Resources/ditu/MapName/1
DDS加载成功: Content/Textures/Resources/ditu/MapName/1.dds, 尺寸: 512x512
...
```

## 📞 反馈信息

请将以下信息反馈给我：

1. **调试输出的完整内容**
2. **DDS文件的实际路径和文件名**
3. **MapName的值**
4. **是否有任何错误消息**
5. **PNG/JPG文件是否能正常加载**

这些信息将帮助我进一步诊断和解决问题。

## 🔄 移除调试信息

测试完成后，如果需要移除调试输出，可以：
1. 将所有`System.Diagnostics.Debug.WriteLine`行注释掉
2. 或者将它们包装在`#if DEBUG`条件编译中

```csharp
#if DEBUG
System.Diagnostics.Debug.WriteLine($"调试信息: {info}");
#endif
```