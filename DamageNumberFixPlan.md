# 伤害数字不显示问题 - 最终修复方案汇总

## 1. 问题根源分析 (Root Cause)

1.  **AOT 序列化失效**: 在 AOT 模式下，`System.Text.Json` 需要预先生成元数据。由于 `CombatNumberGenerator` 未在 `GameJsonContext` 注册，导致其加载后 `DigitWidth` 和 `DigitHeight` 为 0，渲染出来的矩形大小为 0，从而不可见。
2.  **UI 归属错误**: 之前的逻辑将伤害数字挂载在 **攻击者** 头上。但攻击者在动画结束时会立即调用 `ApplyDamageList()` 清空 UI 队列，导致数字只闪现 1 帧就消失了。
3.  **渲染状态绑定过死**: `TroopLayer.Draw` 中的数字渲染逻辑被硬编码在 `if (troop.Action == TroopAction.Stop)` 分支内。而在战斗发生时，部队处于 `Attack` 或 `BeAttacked` 状态，渲染循环直接跳过了数字绘制。

---

## 2. 修复步骤说明

### 第一步：注册 AOT 元数据
在 `Serialization\GameJsonContext.cs` 中添加以下注册（如果已添加请忽略）：
```csharp
[JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberGenerator))]
[JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberItemList))]
[JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberItem))]
[JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.Animations.CombatNumberItem>))]
```
*确保加载 `CommonData.json` 时能正确初始化数字的宽度和高度。*

### 第二步：修改伤害数字归属 (Defender-Target)
在 `GameObjects\Troop.cs` 中，修改 `ShowDamageNumberIfNeeded` 相关方法：
- **目标**: 将数字添加到 **被攻击者 (target)** 的 `DecrementNumberList`。
- **优点**: 被攻击者的状态更稳定，且符合“受击掉血”的视觉逻辑，不会被攻击者的动画结束指令干扰。

### 第三步：解耦渲染逻辑 (Render Decoupling)
在 `MapLayers\TroopLayer.cs` 中：
- **动作**: 将 `DecrementNumberList.Draw` 的调用从 `if (troop.Action == TroopAction.Stop)` 块中移出，放置在 Troop 渲染循环的末尾。
- **效果**: 无论部队是在移动、攻击、受击还是待机，只要 `DecrementNumberList` 中有数字，就会持续渲染，直到 1.2 秒生命周期结束。

---

## 3. 当前代码状态警告 (Code Warning)

> [!WARNING]
> 目前 `GameObjects\Animations\CombatNumberItem.cs` 存在**语法错误**（括号闭和逻辑混乱），这是由于最近的手动编辑导致的。在发布 AOT 时编译器会报 CS1022 等语法错误。

建议修复括号和重复代码：
```csharp
// 修复后示例：
                else
                {
                    num -= (int)(generator.DigitWidth * scale * renshuFangdaBeishu);
                    var rec0 = new Rectangle ? (generator.GetCurrentDigitRectangle((CombatNumberKind)renshuYanseXuhao, CombatNumberDirection.下, number % 10));
                    // 深度值 0.01f 确保数字显示在部队图层上方
                    CacheManager.Draw(generator.Texture, new Vector2((float)num , (float)start.Y), rec0, Color.White, 0f, Vector2.Zero, scale * renshuFangdaBeishu, SpriteEffects.None, 0.01f);
                }
                number /= 10;
            }
            while (number > 0);
        }
    }
}
```

---

## 4. 建议操作流程
如果您决定继续，我将执行以下操作：
1. **修复 `CombatNumberItem.cs` 的语法错误**并移除调试日志。
2. **应用 `TroopLayer.cs` 的解耦渲染代码**。
3. **完成 `Troop.cs` 的 target 属性传递修复**。

完成后，再次进行 AOT 编译即可解决问题。
