# 人物详情页大头像显示修复

## 问题描述
用户反馈：小头像显示了，人物详情页的大头像还是无法显示。

## 问题分析
通过代码分析发现，PersonDetail.cs中的Draw方法调用`CacheManager.DrawZhsanAvatar`时没有明确指定PortraitSize参数，使用了默认值。

## 修复内容

### 修改文件
`WorldOfTheThreeKingdoms/GamePlugins/PersonDetailPlugin/PersonDetail.cs`

### 修改位置
第57行的DrawZhsanAvatar调用

### 修改前
```csharp
CacheManager.DrawZhsanAvatar(this.ShowingPerson, this.PortraitDisplayPosition, 0.199f);
```

### 修改后
```csharp
CacheManager.DrawZhsanAvatar(this.ShowingPerson, this.PortraitDisplayPosition, 0.199f, GameGlobal.PortraitSize.Medium);
```

## 修复原理

### PortraitSize枚举说明
- `PortraitSize.Medium` → 无后缀（大图，原尺寸）
- `PortraitSize.Small` → "s"后缀（小图）

### 头像文件查找逻辑
系统会按以下优先级查找头像文件：

1. **Medium尺寸（大图）**：
   - `{id}.dds`
   - `{id}.png` 
   - `{id}.jpg`

2. **Small尺寸（小图）**：
   - `{id}s.dds`
   - `{id}s.png`
   - `{id}s.jpg`

### 人物详情页应该显示的尺寸
人物详情页是详细信息界面，应该显示大头像（Medium尺寸），即无后缀的原尺寸图片。

## 可能的原因分析

### 1. 头像文件不存在
如果对应的大头像文件不存在，系统可能无法显示。检查以下路径：
- `Content/Textures/GameComponents/PersonPortrait/Images/Default/{id}.dds`
- `Content/Textures/GameComponents/PersonPortrait/Images/Default/{id}.png`
- `Content/Textures/GameComponents/PersonPortrait/Images/Default/{id}.jpg`

### 2. DDS格式支持问题
如果头像是DDS格式，需要确保DDS加载器正常工作。

### 3. 文件路径问题
检查MOD路径和默认路径的文件存在性。

## 测试步骤

### 1. 验证小头像显示
- 打开游戏中显示小头像的界面
- 确认小头像能正常显示

### 2. 验证大头像显示
- 打开人物详情页
- 检查大头像是否正常显示

### 3. 检查不同格式支持
- 测试DDS格式头像
- 测试PNG格式头像
- 测试JPG格式头像

### 4. 检查文件路径
- 验证默认头像目录中的文件
- 验证MOD头像目录中的文件（如果使用MOD）

## 预期结果
- ✅ 小头像正常显示
- ✅ 大头像正常显示
- ✅ 支持DDS、PNG、JPG格式
- ✅ 正确的格式优先级：DDS > PNG > JPG

## 如果问题仍然存在

### 检查头像文件
1. 确认对应ID的大头像文件存在
2. 检查文件格式是否正确
3. 验证文件路径是否正确

### 检查DDS支持
1. 如果使用DDS格式，确认DDS加载器工作正常
2. 检查DDS文件是否为支持的压缩格式（DXT1/DXT3/DXT5）

### 调试信息
查看游戏日志中是否有相关错误信息：
- 头像加载失败的警告
- DDS格式不支持的提示
- 文件不存在的错误

## 编译状态
✅ **修改已完成，无编译错误**

这个修复应该能解决人物详情页大头像不显示的问题。如果问题仍然存在，可能需要进一步检查头像文件的存在性和格式支持。