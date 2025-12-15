# 圆形头像功能暂时停用说明

## 问题描述
在启用圆形头像功能后，游戏在点击人物详情时再次出现 `DXGI_ERROR_DEVICE_REMOVED` 错误导致程序崩溃。

## 决定
为了确保游戏的稳定性，暂时停用圆形头像功能。

## 修改内容

### 1. SimpleTextureManager.cs
```csharp
// 修改前
var isCircular = (size == PortraitSize.Small);

// 修改后
var isCircular = false; // 暂时停用圆形纹理功能
```

### 2. CacheManager.cs (两个方法)
```csharp
// 修改前
if (size == PortraitSize.Small && shape == TextureShape.None)
{
    shape = TextureShape.Circle;
}

// 修改后
// 暂时停用圆形纹理功能以确保稳定性
// if (size == PortraitSize.Small && shape == TextureShape.None)
// {
//     shape = TextureShape.Circle;
// }
```

## 当前状态
- ✅ 所有头像都显示为方形
- ✅ 避免了 DXGI_ERROR_DEVICE_REMOVED 错误
- ✅ 游戏运行稳定

## 未来优化方案

### 方案1：预加载圆形纹理
在游戏启动时预创建常用的圆形纹理，避免在渲染期间动态创建：
```csharp
public static void PreloadCircularTextures()
{
    // 在游戏初始化阶段预加载小尺寸头像的圆形版本
    for (int i = 1; i <= maxPortraitId; i++)
    {
        GetPortraitTexture(i, PortraitSize.Small, true);
    }
}
```

### 方案2：使用着色器实现圆形效果
通过 GPU 着色器在渲染时实现圆形裁剪，避免创建新纹理：
```hlsl
// 在像素着色器中实现圆形裁剪
float2 center = float2(0.5, 0.5);
float distance = length(texCoord - center);
if (distance > 0.5) discard;
```

### 方案3：异步纹理创建
使用后台线程创建圆形纹理，完成后再更新缓存：
```csharp
Task.Run(() => {
    var circularTexture = CreateCircularTexture(originalTexture);
    // 在主线程中更新缓存
});
```

### 方案4：纹理池管理
实现纹理对象池，重用纹理对象以减少创建/销毁开销。

## 重新启用步骤
当准备重新启用圆形头像功能时：

1. 取消注释相关代码
2. 实施上述优化方案之一
3. 进行充分测试，确保不会导致设备丢失错误
4. 监控内存和显存使用情况

## 测试建议
- 长时间运行游戏，确保稳定性
- 频繁点击人物详情，测试是否还会崩溃
- 监控内存使用情况，确保没有内存泄漏
- 测试不同分辨率和显卡配置下的表现

目前的优先级是确保游戏稳定运行，圆形头像功能可以在后续版本中通过更安全的方式重新实现。