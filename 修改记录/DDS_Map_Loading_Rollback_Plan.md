# DDS地图加载修改回退方案

## 🔄 回退机制说明

为了确保每次修改都有安全的回退路径，以下是所有已修改文件的原始状态备份和回退方法。

## 📁 已修改的文件列表

### 1. WorldOfTheThreeKingdoms/GameManager/CacheManager.cs
**修改内容**: LoadTexture方法中添加了地图文件路径检测逻辑

**回退方法**: 将以下代码：
```csharp
if (!name.Contains("."))
{
    // 检查是否是地图文件路径，如果是则不添加扩展名，让LoadTextureWithFormatPriority处理
    if (name.Contains("ditu/") || name.Contains("ditu\\"))
    {
        // 地图文件，保持原路径不变，让Platform.LoadTexture使用DDS优先级
        res = name;
    }
    else
    {
        // 非地图文件，使用TextureRecs中的扩展名
        TextureRecs rec = Session.TextureRecs.FirstOrDefault(te => te.Key.Split('#')[0] == name).Value;
        if (rec.Ext != null)
        {
            res = res + "." + rec.Ext;
        }
    }
}
```

**回退为**:
```csharp
if (!name.Contains("."))
{
    TextureRecs rec = Session.TextureRecs.FirstOrDefault(te => te.Key.Split('#')[0] == name).Value;
    res = res + "." + rec.Ext;
}
```

### 2. WorldOfTheThreeKingdoms/GameManager/DDSLoader.cs
**修改内容**: 添加了调试输出和数据读取逻辑修改

**回退方法**: 
1. 移除所有`System.Diagnostics.Debug.WriteLine`调用
2. 将数据读取逻辑回退为：
```csharp
// 原始版本
long remainingBytes = stream.Length - stream.Position;
if (dataSize > remainingBytes)
{
    return null;
}
byte[] data = reader.ReadBytes(dataSize);
```

### 3. WorldOfTheThreeKingdoms/MapLayers/MainMapLayer.cs
**修改内容**: CheckMapTileTexture方法实现了DDS优先加载，添加了调试输出

**回退方法**: 将CheckMapTileTexture方法回退为原始的CacheManager.GetTempTexture调用：
```csharp
private void CheckMapTileTexture(MapTile maptile)
{
    if (maptile.TileTexture == null)
    {
        try
        {
            maptile.TileTexture = CacheManager.GetTempTexture("Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number + ".jpg");
        }
        catch (FileNotFoundException)
        {
            maptile.TileTexture = CacheManager.GetTempTexture("Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.MapName + "/" + maptile.number + ".png");
        }
        catch (Exception)
        {
            maptile.TileTexture = new Texture2D(Platform.GraphicsDevice, 1, 1);
        }
    }
}
```

### 4. WorldOfTheThreeKingdoms/GameScreens/MGSStartLoad.cs
**修改内容**: ReloadAirView调用移除了.jpg扩展名

**回退方法**:
```csharp
// 第727行回退为：
this.Plugins.AirViewPlugin.ReloadAirView(Session.Current.Scenario.ScenarioMap.MapName + ".jpg");
```

### 5. WorldOfTheThreeKingdoms/GameScreens/LoadingScreen.cs
**修改内容**: 地图文件搜索支持多种格式

**回退方法**:
```csharp
// 第72行回退为：
maps = Platform.Current.GetMODFiles(dir + "/", true).NullToEmptyArray().Where(fi => fi.EndsWith(".jpg")).NullToEmptyArray();

// 第146行回退为：
var pictures = Platform.Current.GetMODFiles(baseDir, true).NullToEmptyArray().Where(pi => pi.EndsWith(".jpg")).NullToEmptyArray();
```

### 6. WorldOfTheThreeKingdoms/GamePlugins/AirViewPlugin/AirViewPlugin.cs
**修改内容**: ReloadAirView方法实现了DDS优先加载

**回退方法**: 将ReloadAirView方法回退为：
```csharp
public void ReloadAirView(string dituwenjian)
{
    try
    {
        this.airView.MapTexture = CacheManager.GetTempTexture(@"Content\Textures\Resources\ditu\_" + dituwenjian);
    }
    catch
    {
        this.airView.MapTexture = null;
    }
}
```

## 🚨 紧急回退脚本

创建一个批处理文件来快速回退所有修改：

```batch
@echo off
echo 正在回退DDS地图加载修改...

REM 备份当前修改的文件
copy "WorldOfTheThreeKingdoms\GameManager\CacheManager.cs" "WorldOfTheThreeKingdoms\GameManager\CacheManager.cs.dds_backup"
copy "WorldOfTheThreeKingdoms\GameManager\DDSLoader.cs" "WorldOfTheThreeKingdoms\GameManager\DDSLoader.cs.dds_backup"
copy "WorldOfTheThreeKingdoms\MapLayers\MainMapLayer.cs" "WorldOfTheThreeKingdoms\MapLayers\MainMapLayer.cs.dds_backup"

REM 这里需要手动恢复原始文件内容
echo 请手动恢复以上文件到原始状态
echo 或者从版本控制系统恢复
pause
```

## 📋 回退检查清单

回退完成后，请检查以下项目：

- [ ] 游戏能正常启动
- [ ] 地图能正常加载（JPG格式）
- [ ] 小地图能正常显示
- [ ] 没有编译错误
- [ ] 没有运行时异常

## 🔍 版本控制建议

建议使用Git来管理这些修改：

```bash
# 创建功能分支
git checkout -b feature/dds-map-support

# 提交每个文件的修改
git add WorldOfTheThreeKingdoms/GameManager/CacheManager.cs
git commit -m "修改CacheManager支持地图DDS加载"

git add WorldOfTheThreeKingdoms/MapLayers/MainMapLayer.cs
git commit -m "修改MainMapLayer支持DDS优先加载"

# 如果需要回退
git checkout main
git branch -D feature/dds-map-support
```

## ⚠️ 重要提醒

1. **每次修改前先备份**
2. **测试每个修改的影响**
3. **保持原有功能的完整性**
4. **如果出现问题立即回退**
5. **记录所有修改的详细信息**

这个回退方案确保我们可以随时安全地撤销所有DDS相关的修改，恢复到原始的稳定状态。