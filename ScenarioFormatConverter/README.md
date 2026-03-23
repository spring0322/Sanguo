# 剧本格式转换器

**版本:** 1.0  
**日期:** 2026-03-17  
**目的:** 将旧格式剧本（Architecture-Centric）转换为新格式（Person-Centric）

---

## 📋 功能说明

### 转换内容

1. **Person.LocationArchitectureID**
   - 从 `Architecture.PersonIDs` 反向设置
   - 修复建筑武将列表为空的问题

2. **Architecture.BelongedFactionID**
   - 从 `Faction.ArchitectureIDs` 反向设置
   - 修复势力-建筑归属关系丢失

3. **Person.BelongedFactionID**
   - 通过 `Architecture.BelongedFactionID` 推断
   - 修复势力-武将归属关系丢失（解决"在野"问题）

4. **Architecture.BelongedSectionID**
   - 从 `Section.ArchitectureIDs` 反向设置
   - 修复军区系统无法工作

5. **Section.BelongedFactionID**
   - 通过 `Architecture.BelongedFactionID` 推断
   - 修复军区-势力归属关系丢失

### 安全保障

- ✅ 自动创建备份文件（带时间戳）
- ✅ 完整的数据验证（引用完整性、双向一致性）
- ✅ 验证失败时不保存转换结果
- ✅ 详细的转换统计和日志

---

## 🚀 使用方法

### 1. 编译项目

```bash
cd ScenarioFormatConverter
dotnet build -c Release
```

### 2. 转换单个剧本

```bash
dotnet run --project ScenarioFormatConverter.csproj -- "Content/Data/Scenario/184DHZS.json"
```

**输出示例:**
```
===========================================
  剧本格式转换器 v1.0
  Architecture-Centric → Person-Centric
===========================================

转换文件: Content/Data/Scenario/184DHZS.json

  📦 备份已创建: 184DHZS.json.backup_20260317_143052
  🔄 开始转换...
    Person.LocationArchitectureID: 1234
    Architecture.BelongedFactionID: 156
    Person.BelongedFactionID: 1234
    Architecture.BelongedSectionID: 156
    Section.BelongedFactionID: 12
  ✅ 转换完成
  🔍 验证转换结果...
  ✅ 验证通过
  💾 已保存转换后的文件
  📦 备份文件: 184DHZS.json.backup_20260317_143052
✅ 转换成功！

转换统计:
  Person.LocationArchitectureID: 1234
  Person.BelongedFactionID: 1234
  Architecture.BelongedFactionID: 156
  Architecture.BelongedSectionID: 156
  Section.BelongedFactionID: 12
```

### 3. 批量转换剧本

```bash
dotnet run --project ScenarioFormatConverter.csproj -- "Content/Data/Scenario" --batch
```

**输出示例:**
```
找到 15 个剧本文件

处理: 184DHZS.json
  📦 备份已创建: 184DHZS.json.backup_20260317_143052
  🔄 开始转换...
  ...
  ✅ 转换成功

处理: 190FDLM.json
  ⏭️  跳过（已是新格式）

...

===========================================
批量转换完成:
  成功: 12
  跳过: 2
  失败: 1
===========================================
```

---

## 📊 转换前后对比

### 旧格式（Architecture-Centric）

```json
{
  "Architectures": [
    {
      "ID": 0,
      "Name": "洛阳",
      "PersonsString": "9 16 18 25 59 71 ...",
      "PersonIDs": null
    }
  ],
  "Persons": [
    {
      "ID": 9,
      "Name": "韦昭",
      "LocationArchitectureID": null,
      "BelongedFactionID": null
    }
  ],
  "Factions": [
    {
      "ID": 0,
      "Name": "汉",
      "ArchitecturesString": "0 5 28 105 ..."
    }
  ]
}
```

### 新格式（Person-Centric）

```json
{
  "Architectures": [
    {
      "ID": 0,
      "Name": "洛阳",
      "PersonsString": "9 16 18 25 59 71 ...",
      "PersonIDs": null,
      "BelongedFactionID": 0
    }
  ],
  "Persons": [
    {
      "ID": 9,
      "Name": "韦昭",
      "LocationArchitectureID": 0,
      "BelongedFactionID": 0
    }
  ],
  "Factions": [
    {
      "ID": 0,
      "Name": "汉",
      "ArchitecturesString": "0 5 28 105 ..."
    }
  ]
}
```

---

## ⚠️ 注意事项

### 1. 备份文件管理

- 每次转换都会创建带时间戳的备份文件
- 格式：`<原文件名>.backup_yyyyMMdd_HHmmss`
- 建议定期清理旧备份文件

### 2. 验证失败处理

如果验证失败，转换器会：
- ❌ 不保存转换结果
- 📦 保留备份文件
- 📝 输出详细的错误信息

**常见验证错误:**
- 引用了不存在的对象 ID
- 双向引用不一致（如 `Architecture.PersonIDs` 与 `Person.LocationArchitectureID` 不匹配）

### 3. ID=0 的特殊处理

**重要:** ID=0 是有效的游戏对象 ID！

- Architecture ID=0 → 洛阳
- Person ID=0 → 阿会喃
- Faction ID=0 → 汉

转换器正确处理了 ID=0 的情况，使用 `>= 0` 判断有效 ID。

### 4. 已转换剧本的处理

- 转换器会自动检测剧本是否已是新格式
- 如果已转换，会跳过并显示 "⏭️  跳过（已是新格式）"
- 不会重复转换或覆盖数据

---

## 🔧 故障排除

### 问题 1：转换后武将仍然显示"在野"

**可能原因:**
- `Person.BelongedFactionID` 未正确设置
- `Architecture.BelongedFactionID` 为 -1 或 null

**解决方案:**
1. 检查转换统计中的 `Person.BelongedFactionID` 数量
2. 如果为 0，说明 `Architecture.BelongedFactionID` 也未设置
3. 检查原始剧本的 `Faction.ArchitecturesString` 是否有数据

### 问题 2：验证失败 - 引用不存在的对象

**可能原因:**
- 原始剧本数据损坏
- ID 引用错误

**解决方案:**
1. 查看详细的错误信息
2. 手动检查原始剧本的引用关系
3. 修复原始数据后重新转换

### 问题 3：批量转换时部分文件失败

**可能原因:**
- 文件格式不正确
- 文件被占用

**解决方案:**
1. 查看失败文件的错误信息
2. 单独转换失败的文件以获取详细日志
3. 检查文件权限和占用情况

---

## 📚 技术细节

### 转换算法

1. **读取 JSON 文件**
   - 使用 `System.Text.Json` 解析
   - 保持原始结构不变

2. **检测格式**
   - 检查 `Person[0].LocationArchitectureID` 是否存在
   - 存在 → 新格式，跳过
   - 不存在 → 旧格式，执行转换

3. **执行转换**
   - 按顺序执行 5 个转换步骤
   - 每步都创建 ID 映射表以提高性能
   - 只设置未设置的字段（避免覆盖已有数据）

4. **验证结果**
   - 引用完整性验证（所有引用的 ID 都存在）
   - 双向一致性验证（如 `Architecture.PersonIDs` ↔ `Person.LocationArchitectureID`）
   - 发现错误 → 不保存
   - 发现警告 → 保存但提示

5. **保存文件**
   - 使用不缩进的 JSON 格式（保持文件小）
   - 使用 UTF-8 编码
   - 保留原始文件名

### 性能优化

- 使用 `Dictionary<int, JsonNode>` 缓存对象映射
- 避免重复遍历大数组
- 单次文件读写

### 代码质量

- ✅ 符合 C# 12 语法规范
- ✅ 符合 ID 判断规范（使用 `>= 0`）
- ✅ 符合 ANTI-BAND-AID 协议（数据验证而非掩盖错误）
- ✅ 详细的注释和日志

---

## 📝 更新日志

### v1.0 (2026-03-17)

- ✅ 初始版本
- ✅ 支持 5 种引用关系转换
- ✅ 完整的数据验证
- ✅ 自动备份功能
- ✅ 批量转换支持

---

## 🎯 后续计划

### v1.1

- [ ] 支持命令行参数配置（如 `--no-backup`）
- [ ] 支持转换报告导出（JSON/CSV）
- [ ] 支持回滚功能（从备份恢复）

### v1.2

- [ ] 支持增量转换（只转换变更的字段）
- [ ] 支持自定义验证规则
- [ ] 支持并行批量转换

---

## 📞 联系方式

**维护者:** Lead Architect  
**日期:** 2026-03-17  
**项目:** World of the Three Kingdoms (zhsan)

---

## ⚖️ 许可证

本工具是 zhsan 项目的一部分，遵循项目的许可证。
