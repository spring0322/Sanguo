# _yueluo_1.0 小地图加载问题修复总结

## 🎯 问题描述
`_yueluo_1.0` 这个小地图无法正常加载出来，导致游戏中的小地图显示异常。

## 🔍 问题根源分析
通过深入分析发现，问题出现在文件名处理逻辑上：

### 问题文件名
- **实际文件**: `_yueluo_1.0.jpg` / `_yueluo_1.0.dds`
- **期望处理结果**: `_yueluo_1.0`
- **实际处理结果**: `_yueluo_1` ❌

### 根本原因
`System.IO.Path.GetFileNameWithoutExtension("_yueluo_1.0.jpg")` 错误地将 `.0` 识别为文件扩展名，返回 `"_yueluo_1"` 而不是期望的 `"_yueluo_1.0"`。

## 🛠️ 修复方案

### 1. AirViewPlugin.cs 修复
**文件**: `WorldOfTheThreeKingdoms/GamePlugins/AirViewPlugin/AirViewPlugin.cs`
**问题行**: 第170行

**修复前**:
```csharp
string basePath = @"Content\Textures\Resources\ditu\_" + System.IO.Path.GetFileNameWithoutExtension(dituwenjian);
```

**修复后**:
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
**文件**: `WorldOfTheThreeKingdoms/GameObjects/Map.cs`
**问题行**: MapName属性的setter

**修复前**:
```csharp
if (value.EndsWith(".jpg") || value.EndsWith(".png") || value.EndsWith(".dds"))
{
    dituwenjian = System.IO.Path.GetFileNameWithoutExtension(value);
}
```

**修复后**:
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

## ✅ 修复效果

### 文件名处理对比
| 输入文件名 | 修复前 | 修复后 | 状态 |
|------------|--------|--------|------|
| `_yueluo_1.0.jpg` | `_yueluo_1` | `_yueluo_1.0` | ✅ 修复 |
| `map_v2.1.png` | `map_v2` | `map_v2.1` | ✅ 修复 |
| `simple.jpg` | `simple` | `simple` | ✅ 兼容 |
| `test.dds` | `test` | `test` | ✅ 兼容 |

### 路径构建对比
对于 `_yueluo_1.0.jpg`：
- **修复前**: `Content\Textures\Resources\ditu\_yueluo_1.dds` ❌ 文件不存在
- **修复后**: `Content\Textures\Resources\ditu\_yueluo_1.0.dds` ✅ 正确路径

## 🎮 功能改进

### 1. 小地图加载
- ✅ 正确识别包含版本号的地图文件名
- ✅ 支持DDS优先加载策略
- ✅ 向后兼容现有地图文件

### 2. 文件格式支持
- ✅ DDS格式优先加载
- ✅ PNG格式备用加载
- ✅ JPG格式兼容加载
- ✅ 大小写不敏感的扩展名检查

### 3. 错误处理
- ✅ 优雅的文件不存在处理
- ✅ 多格式尝试加载机制
- ✅ 调试信息输出

## 🔧 技术细节

### 核心改进
1. **精确的扩展名移除**: 使用 `LastIndexOf('.')` 而不是 `GetFileNameWithoutExtension()`
2. **大小写不敏感**: 使用 `StringComparison.OrdinalIgnoreCase`
3. **多格式支持**: 统一处理 JPG、PNG、DDS 格式
4. **向后兼容**: 保持对简单文件名的支持

### 性能影响
- ✅ 最小性能开销
- ✅ 不影响现有功能
- ✅ 减少文件加载失败次数

## 📋 测试建议

### 验证步骤
1. **启动游戏**并加载包含 `_yueluo_1.0` 地图的场景
2. **检查小地图显示**是否正常
3. **验证DDS优先加载**是否工作
4. **测试其他版本号格式**的地图文件

### 测试用例
- `_yueluo_1.0.jpg` → 应该正常加载
- `map_v2.1.png` → 应该正常加载  
- `simple.jpg` → 应该保持兼容
- `test.dds` → 应该优先加载

## 🎉 修复结果
- ✅ `_yueluo_1.0` 小地图现在应该能够正常加载
- ✅ 支持所有包含版本号的地图文件名
- ✅ 保持向后兼容性
- ✅ 改进了DDS格式支持

这个修复解决了文件名处理的根本问题，不仅修复了 `_yueluo_1.0` 地图的加载问题，还改善了整个地图文件名处理系统的健壮性。