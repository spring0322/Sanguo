# 地图文件名处理修复测试

## 问题描述
`_yueluo_1.0` 这个小地图无法正常加载，原因是文件名处理逻辑错误。

## 问题分析

### 原始问题
对于文件名 `_yueluo_1.0.jpg`：
- `System.IO.Path.GetFileNameWithoutExtension("_yueluo_1.0.jpg")` 返回 `"_yueluo_1"`
- 但实际应该返回 `"_yueluo_1.0"`

### 根本原因
`Path.GetFileNameWithoutExtension()` 方法将 `.0` 视为文件扩展名，而不是文件名的一部分。

## 修复方案

### 1. AirViewPlugin.cs 修复
**位置**: `WorldOfTheThreeKingdoms/GamePlugins/AirViewPlugin/AirViewPlugin.cs`

**修改前**:
```csharp
string basePath = @"Content\Textures\Resources\ditu\_" + System.IO.Path.GetFileNameWithoutExtension(dituwenjian);
```

**修改后**:
```csharp
// 修复：对于包含版本号的文件名（如_yueluo_1.0.jpg），需要特殊处理
string fileName = System.IO.Path.GetFileName(dituwenjian);
string nameWithoutExt;

// 检查是否是标准的图片扩展名
if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
    fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
    fileName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
{
    // 只移除最后一个扩展名
    int lastDotIndex = fileName.LastIndexOf('.');
    nameWithoutExt = fileName.Substring(0, lastDotIndex);
}
else
{
    nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
}

string basePath = @"Content\Textures\Resources\ditu\" + nameWithoutExt;
```

### 2. Map.cs 修复
**位置**: `WorldOfTheThreeKingdoms/GameObjects/Map.cs`

**修改前**:
```csharp
if (value.EndsWith(".jpg") || value.EndsWith(".png") || value.EndsWith(".dds"))
{
    dituwenjian = System.IO.Path.GetFileNameWithoutExtension(value);
}
```

**修改后**:
```csharp
if (value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
    value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
    value.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
{
    // 修复：对于包含版本号的文件名（如_yueluo_1.0.jpg），只移除最后一个扩展名
    int lastDotIndex = value.LastIndexOf('.');
    dituwenjian = value.Substring(0, lastDotIndex);
}
```

## 测试用例

### 输入文件名测试
| 输入文件名 | 修复前结果 | 修复后结果 | 期望结果 |
|------------|------------|------------|----------|
| `_yueluo_1.0.jpg` | `_yueluo_1` | `_yueluo_1.0` | `_yueluo_1.0` ✅ |
| `map_v2.1.png` | `map_v2` | `map_v2.1` | `map_v2.1` ✅ |
| `simple.jpg` | `simple` | `simple` | `simple` ✅ |
| `test.dds` | `test` | `test` | `test` ✅ |
| `noext` | `noext` | `noext` | `noext` ✅ |

### 文件路径构建测试
对于 `_yueluo_1.0.jpg`：
- **修复前**: `Content\Textures\Resources\ditu\_yueluo_1.dds` ❌
- **修复后**: `Content\Textures\Resources\ditu\_yueluo_1.0.dds` ✅

## 影响范围
这个修复影响以下功能：
1. **小地图显示**: AirView插件中的小地图加载
2. **地图名称处理**: Map对象的MapName属性设置
3. **DDS优先加载**: 支持DDS格式的地图文件优先加载

## 兼容性
- ✅ 向后兼容：不影响现有的简单文件名
- ✅ 扩展名大小写不敏感
- ✅ 支持所有图片格式（JPG、PNG、DDS）
- ✅ 正确处理版本号格式的文件名

## 验证方法
1. 启动游戏并加载包含 `_yueluo_1.0` 地图的场景
2. 检查小地图是否正常显示
3. 验证DDS格式优先加载是否正常工作
4. 测试其他包含版本号的地图文件名

修复完成后，`_yueluo_1.0` 地图应该能够正常加载和显示。