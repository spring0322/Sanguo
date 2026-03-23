# MonoGame SpriteBatch.Draw 调用规范

**日期：** 2026-03-14  
**状态：** 强制执行  
**原因：** 避免重复犯 API 调用错误

---

## 🚨 核心规则

### ✅ 正确的 Draw 调用

```csharp
// ✅ 正确：使用项目实际可用的 3 参数重载
spriteBatch.Draw(
    texture,
    new Rectangle(x, y, width, height),  // 必须显式类型
    new Color(r, g, b, a));              // 必须显式类型

// ✅ 正确：使用 8 参数重载（带旋转、原点等）
spriteBatch.Draw(
    texture,
    new Rectangle(x, y, width, height),  // destinationRectangle
    null,                                 // sourceRectangle
    new Color(r, g, b, a),               // color
    0f,                                   // rotation
    Vector2.Zero,                         // origin
    SpriteEffects.None,                   // effects
    0f);                                  // layerDepth
```

### ❌ 错误的 Draw 调用

```csharp
// ❌ 错误：使用目标类型推断 new(...)
spriteBatch.Draw(
    texture,
    new(x, y, width, height),  // 编译器无法推断类型！
    new(r, g, b, a));          // 编译器无法推断类型！

// ❌ 错误：参数顺序错误
spriteBatch.Draw(
    texture,
    destinationRect,
    color,           // 缺少 sourceRectangle 参数
    rotation,        // 编译器会报错
    origin);

// ❌ 错误：假设存在不存在的重载
spriteBatch.Draw(
    texture,
    destinationRect,
    color);  // 可能不存在这个重载！
```

---

## 📋 修复流程（强制执行）

当遇到 `SpriteBatch.Draw` 编译错误时，必须按以下步骤操作：

### 步骤 1：查找项目中的实际用法

```bash
# 搜索项目中如何使用 Draw 方法
grep -r "spriteBatch.Draw" --include="*.cs"
```

**关键：** 不要猜测 API，直接查看项目中其他地方的实际用法！

### 步骤 2：确认可用的重载

项目中常用的重载：

1. **3 参数版本**（最常用）：
   ```csharp
   Draw(Texture2D, Rectangle, Color)
   ```

2. **8 参数版本**（完整控制）：
   ```csharp
   Draw(Texture2D, Rectangle, Rectangle?, Color, float, Vector2, SpriteEffects, float)
   ```

3. **Vector2 位置版本**：
   ```csharp
   Draw(Texture2D, Vector2, Color)
   ```

### 步骤 3：使用显式类型

**强制规则：** 在 `SpriteBatch.Draw` 的参数位置，必须使用显式类型，不能使用 C# 12 目标类型推断。

```csharp
// ❌ 错误：目标类型推断
spriteBatch.Draw(texture, new(x, y, w, h), new(r, g, b, a));

// ✅ 正确：显式类型
spriteBatch.Draw(texture, new Rectangle(x, y, w, h), new Color(r, g, b, a));
```

**原因：** MonoGame 的 `SpriteBatch.Draw` 有多个重载，编译器无法在方法参数位置推断 `new(...)` 的类型。

---

## ⚠️ 常见陷阱

### 陷阱 1：盲目使用 C# 12 语法

```csharp
// ❌ 错误：在方法参数位置使用目标类型推断
spriteBatch.Draw(_lowResTarget, new(...), new(...));
```

**解决：** 在 `SpriteBatch.Draw` 调用中，始终使用显式类型。

### 陷阱 2：假设 API 存在

```csharp
// ❌ 错误：假设存在 Draw(Texture2D, Rectangle, Color) 重载
spriteBatch.Draw(texture, rect, color);  // 可能不存在！
```

**解决：** 先搜索项目中的实际用法，确认可用的重载。

### 陷阱 3：参数顺序错误

```csharp
// ❌ 错误：8 参数版本的参数顺序错误
spriteBatch.Draw(texture, rect, color, null, 0f, Vector2.Zero, SpriteEffects.None, 0f);
```

**解决：** 严格按照正确的参数顺序：
1. `Texture2D texture`
2. `Rectangle destinationRectangle`
3. `Rectangle? sourceRectangle`
4. `Color color`
5. `float rotation`
6. `Vector2 origin`
7. `SpriteEffects effects`
8. `float layerDepth`

---

## 🔧 修复检查清单

在编写 `SpriteBatch.Draw` 调用时，必须检查：

1. ✅ 是否查看了项目中的实际用法？
2. ✅ 是否使用了显式类型（`new Rectangle(...)`、`new Color(...)`）？
3. ✅ 是否确认了参数顺序正确？
4. ✅ 是否避免了假设不存在的重载？

---

## 📝 代码模板

### 模板 1：简单绘制（3 参数）

```csharp
spriteBatch.Draw(
    texture,
    new Rectangle(x, y, width, height),
    new Color(r, g, b, a));
```

### 模板 2：完整控制（8 参数）

```csharp
spriteBatch.Draw(
    texture,
    new Rectangle(x, y, width, height),  // destinationRectangle
    null,                                 // sourceRectangle: null = 使用整个纹理
    new Color(r, g, b, a),               // color
    0f,                                   // rotation: 无旋转
    Vector2.Zero,                         // origin: 左上角
    SpriteEffects.None,                   // effects: 无翻转
    0f);                                  // layerDepth: 最底层
```

### 模板 3：位置绘制（Vector2）

```csharp
spriteBatch.Draw(
    texture,
    new Vector2(x, y),
    new Color(r, g, b, a));
```

---

## 🎯 性能注意事项

### Hot Path 优化

在 `Update()` 或 `Draw()` 循环中：

```csharp
// ✅ 正确：值类型在栈上分配，Zero-Allocation
spriteBatch.Draw(
    texture,
    new Rectangle(x, y, w, h),  // 栈上分配
    new Color(r, g, b, a));     // 栈上分配

// ✅ 正确：预计算常量
private static readonly Color TransparentWhite = new(255, 255, 255, 102);

spriteBatch.Draw(texture, rect, TransparentWhite);  // 无分配
```

### 避免的模式

```csharp
// ❌ 错误：每帧计算颜色
spriteBatch.Draw(texture, rect, Color.White * 0.4f);  // 可能产生临时对象

// ✅ 正确：使用预计算的颜色
spriteBatch.Draw(texture, rect, new Color(255, 255, 255, 102));
```

---

## 📚 相关文档

- [ANTI-BAND-AID 协议](System%20Prompt%20/%20Custom%20Instructions.md)
- [性能优化规范](System%20Prompt%20/%20Custom%20Instructions.md#性能优化)

---

**最后更新：** 2026-03-14  
**维护者：** Lead Architect  
**教训来源：** 水墨渲染器 SpriteBatch.Draw 编译错误修复（反复 5 次才成功）
