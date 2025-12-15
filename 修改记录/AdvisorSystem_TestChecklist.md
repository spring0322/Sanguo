# 军师系统完整测试清单

## 🎯 功能测试

### 1. 军师按钮显示测试
- [ ] 有军师时显示军师头像
- [ ] 没有军师时显示绿色方块
- [ ] 按钮位置正确（屏幕左上角 100,100）
- [ ] 按钮大小正确（100x100）

### 2. 军师按钮点击测试
- [ ] 有军师时点击显示管理菜单
- [ ] 没有军师时点击显示任命菜单
- [ ] 点击检测区域正确

### 3. 菜单系统测试
- [ ] 右键建筑 → 人事 → 任免 → 任命军师
- [ ] 右键建筑 → 人事 → 任免 → 罢免军师
- [ ] 菜单项正确显示/隐藏

### 4. 任命军师测试
- [ ] 显示候选人列表
- [ ] 选择候选人后正确任命
- [ ] 任命后 AdvisorID 正确设置
- [ ] 年表记录正确添加

### 5. 罢免军师测试
- [ ] 显示罢免对话
- [ ] 对话正确显示双人
- [ ] 罢免后 AdvisorID 正确清除
- [ ] 年表记录正确添加

### 6. 对话系统测试
- [ ] 君主说话时只显示君主
- [ ] 军师说话时只显示军师
- [ ] 文本不会混合显示
- [ ] 点击切换正常
- [ ] 震动效果正常

## 🔧 技术验证

### 1. 内存管理
- [ ] 纹理正确释放
- [ ] 缓存正确清理
- [ ] 无内存泄漏

### 2. 性能优化
- [ ] 防抖机制正常工作
- [ ] 不会每帧重复生成纹理
- [ ] UI更新频率合理

### 3. 错误处理
- [ ] 资源加载失败时有备用方案
- [ ] 异常不会导致游戏崩溃
- [ ] 调试信息输出正常

## 🎮 用户体验测试

### 1. 视觉效果
- [ ] 头像显示清晰
- [ ] 对话框布局合理
- [ ] 文字大小适中
- [ ] 颜色搭配协调

### 2. 交互体验
- [ ] 点击响应及时
- [ ] 状态切换流畅
- [ ] 提示信息清楚
- [ ] 操作逻辑直观

### 3. 游戏平衡
- [ ] 任命条件合理
- [ ] 忠诚度变化适当
- [ ] 关系判断准确

## 🐛 常见问题排查

### 1. 按钮不显示
**检查项目**:
- [ ] `UpdateAdvisorButton` 是否被调用
- [ ] `AdvisorButtonImage` 是否为 null
- [ ] 绘制代码是否正确执行
- [ ] SpriteBatch.Begin/End 是否配对

**解决方案**:
```csharp
// 在 Update 方法中确认调用
this.UpdateAdvisorButtonInput();

// 在 Drawing 方法中确认绘制
if (this.AdvisorButtonImage != null)
{
    Session.Current.SpriteBatch.Begin();
    Session.Current.SpriteBatch.Draw(this.AdvisorButtonImage, new Vector2(100, 100), Color.White);
    Session.Current.SpriteBatch.End();
}
```

### 2. 点击无响应
**检查项目**:
- [ ] `UpdateAdvisorButtonInput` 是否被调用
- [ ] 鼠标坐标是否正确
- [ ] 点击区域是否匹配按钮位置

**解决方案**:
```csharp
// 调试鼠标位置
var mouseState = Mouse.GetState();
System.Diagnostics.Debug.WriteLine($"鼠标位置: {mouseState.X}, {mouseState.Y}");
System.Diagnostics.Debug.WriteLine($"按钮区域: {advisorButtonRect}");
```

### 3. 菜单不显示
**检查项目**:
- [ ] `ContextMenuData.xml` 是否正确修改
- [ ] `AppointAdvisorAvail()` 是否返回 true
- [ ] `RecallAdvisorAvail()` 是否返回 true

**解决方案**:
```csharp
// 调试可用性检查
var faction = Session.Current.CurrentPlayerFaction;
System.Diagnostics.Debug.WriteLine($"可任命: {faction.AppointAdvisorAvail()}");
System.Diagnostics.Debug.WriteLine($"可罢免: {faction.RecallAdvisorAvail()}");
```

### 4. 对话显示异常
**检查项目**:
- [ ] `DialogueUI` 是否正确初始化
- [ ] `ShowDialogueUI` 是否被调用
- [ ] 对话状态切换是否正常

**解决方案**:
```csharp
// 调试对话状态
System.Diagnostics.Debug.WriteLine($"对话状态: {dialogueUI.currentState}");
System.Diagnostics.Debug.WriteLine($"当前文本: {dialogueUI.currentText}");
System.Diagnostics.Debug.WriteLine($"目标文本: {dialogueUI.targetText}");
```

## 📝 测试步骤

### 完整测试流程
1. **启动游戏** → 检查军师按钮是否显示
2. **点击按钮** → 检查是否有响应
3. **右键菜单** → 检查任免选项是否显示
4. **任命军师** → 检查流程是否正常
5. **罢免军师** → 检查对话是否正常
6. **重复测试** → 检查稳定性

### 边界情况测试
- 没有候选人时的处理
- 军师死亡时的处理
- 势力变更时的处理
- 存档加载时的处理

## 🚀 后续优化建议

### 1. 功能扩展
- 添加军师技能系统
- 添加军师建议系统
- 添加军师对话配置文件
- 添加更多震动效果

### 2. 性能优化
- 使用对象池管理纹理
- 优化绘制批次
- 减少字符串分配
- 缓存计算结果

### 3. 用户体验
- 添加音效支持
- 添加动画效果
- 优化UI布局
- 增加快捷键支持

---

**测试完成后，请在对应项目前打勾 ✅，并记录任何发现的问题。**