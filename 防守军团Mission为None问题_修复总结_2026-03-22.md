# 防守军团 Mission=None 问题修复总结（2026-03-22）

## 1. 问题现象
- 调试日志出现（读档/链接期与运行期都有）：
  - `[Legion.TroopAI] AI军团AI_守_阳翟(Mission:None)，目标:洛阳(归属:汉)`
  - `[LegionLoad][Warn] Legion 1 (AI_守_阳翟) Mission=None cannot be recovered in fast link.`
  - `[LegionLoad][Warn] DTO AI legion has Mission=None: AI_守_阳翟(ID:1), TargetArchitectureID=-1, WillArchitectureID=-1`
  - `[LegionLoad][Warn] DTO AI legion has Mission=None: Player__洛阳(ID:0), TargetArchitectureID=-1, WillArchitectureID=-1`
- 异常点：
  - AI 军团任务为 `None`，导致行为分支错误。
  - 目标建筑被错误指向（或恢复不完整），出现“初始就是别人的城池”的错觉。
  - 洛阳归属未变化，但军团目标链路异常。
  - 回合中会出现“军团无目标建筑 -> 切换到 Idle 状态”，导致部队无法正常执行委任/军团 AI。

## 2. 根本原因（Root Cause）

### 2.1 军团任务/目标在读档链路恢复不完整
- 旧存档与新字段并存（`WillArchitectureString` / `TargetArchitectureID`），如果只恢复一部分引用，AI 军团可能保持 `Mission=None`。
- 已补齐读档字段与链接逻辑，增加恢复路径（按名称、按归属关系兜底恢复 Mission）。

相关位置：
- [LoadDataPhase.cs:925](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LoadDataPhase.cs:925)
- [SaveDataPhase.cs:774](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/SaveDataPhase.cs:774)
- [LinkReferencesPhase.cs:2663](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2663)
- [LinkReferencesPhase.cs:2698](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2698)
- [LinkReferencesPhase.cs:2735](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2735)
- [LinkReferencesPhase.cs:3296](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:3296)

### 2.2 `ID=0`（洛阳）被历史写法误伤
- 军团/部队引用恢复中存在“`> 0`”历史写法会跳过 `ID=0` 的合法对象（洛阳），引发引用缺失与错误回退。
- 已在关键链路改为 `>= 0` 并补充异常/告警。

相关位置：
- [LinkReferencesPhase.cs:2666](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2666)
- [LinkReferencesPhase.cs:2701](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2701)
- [LinkReferencesPhase.cs:2895](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LinkReferencesPhase.cs:2895)
- [ValidationPhase.cs:403](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/ValidationPhase.cs:403)

### 2.3 AI 军团创建入口可能落到 `Mission=None`
- 旧接口 `GetOrCreateLegion(Architecture, LegionKind)` 对 AI 任务需要明确映射为 `Defend/Attack`，否则会传播到运行期。
- 已在入口按目标归属判定任务（同势力=防守，异势力=进攻）。

相关位置：
- [Faction.cs:7450](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Faction.cs:7450)
- [Faction.cs:7501](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Faction.cs:7501)
- [Faction.cs:7576](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Faction.cs:7576)

### 2.4 `IsComplete` 对空目标的处理会提前结束军团
- 在 `WillArchitecture == null` 场景下，AI 军团不应被误判完成并解散。
- 已调整为此场景返回 `false` 并输出诊断日志。

相关位置：
- [Legion.cs:579](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Legion.cs:579)
- [Legion.cs:591](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Legion.cs:591)
- [Legion.cs:605](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Legion.cs:605)

### 2.5 玩家军团被旧存档默认还原为 AI（Kind 丢失/默认值）
- 现象日志（旧）：`[LegionLoad][Warn] DTO AI legion has Mission=None: Player__洛阳...`。
- 根因：部分旧存档没有 `Kind` 字段（或字段丢失），`LoadLegionFromDTO` 读到默认值导致 `Player_` 军团被还原成 `LegionKind.AI`。
- 后果：
  - 玩家军团会走 AI 的 `Mission` 恢复/告警分支，形成“玩家军团=AI军团”的错误状态；
  - 玩家选择目标城市后，如果只改 `WillArchitecture` 不同步 `Target/Mission`，下回合仍会被错误逻辑改坏；
  - 一旦玩家中途“委任给AI”，AI 行为会按错误的枚举分支执行。

相关位置：
- [LoadDataPhase.cs:925](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/Serialization/Phases/LoadDataPhase.cs:925)
- [Legion.cs:774](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameObjects/Legion.cs:774)
- [MainGameScreen.cs:2686](G:/sanguo/sanguo260320-2/WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs:2686)

## 3. 本次关键修复项
- 持久化层：
  - 保存 `Legion.Mission / WillArchitectureID / TargetArchitectureID`；
  - 加载时恢复对应字段并对 `AI + Mission=None` 打告警。
- 引用链接层：
  - 新增/强化 `LinkTarget`、`LinkWillArchitecture`；
  - `Target` 缺失时使用 `WillArchitecture` 恢复；
  - 增加 `RecoverMissingMission`（普通/快速链接双路径）。
- 读档纠错（Kind 维度）：
  - `LoadLegionFromDTO` 增加按军团名推断 `Kind`（`Player_`/`AI_`）的恢复逻辑，避免玩家军团被默认还原为 AI。
- 部队关联层：
  - `BelongedLegionID`、`StartingArchitectureID`、`WillArchitectureID` 按 `>=0` 恢复；
  - 非法引用 fail-fast 或告警并回收坏 ID。
- 军团创建层：
  - AI 默认任务不再落到 `None`，按目标归属自动判定攻/守。
- 运行期一致性（玩家军团目标/枚举）：
  - 新增 `Legion.SetOperationalTarget(target)`：同步 `WillArchitecture/WillArchitectureString/Target/TargetArchitectureID`，并为玩家军团按目标归属自动校正 `Mission`（敌方=Attack，己方=Defend，空=None）。
  - UI/指令入口改为调用 `SetOperationalTarget`，避免只改 `WillArchitecture` 导致字段错配。
  - 保险：在 `SetOperationalTarget` 与 `Faction.CleanupCompletedLegions()` 中对 `Player_` 军团做 `Kind` 自愈（AI -> Player）。

## 4. 诊断日志增强
- 增加统一前缀，便于后续排查：
  - `[LegionLoad][Link]`
  - `[LegionLoad][Recover]`
  - `[LegionLoad][Warn]`
- 备注：如果调试信息在控制台/输出窗口出现乱码（例如 `[LinkPersons]` 部分），优先排查输出编码（Console/IDE 输出窗口编码）问题，本次修复不依赖该点。

## 5. 验证结果
- 实机回归验证：问题链路已恢复正常（不再出现“AI/玩家军团被还原成 AI 且 Mission=None”导致的目标丢失与回合内 Idle 卡死）。
- 针对该问题链路，核心目标是：**AI 军团不再无故保持 Mission=None，且洛阳（ID=0）引用不会被跳过**。
- 玩家军团链路补充验证（实机回归）：
  - `Player_` 军团不会再在读档阶段被当成 AI；
  - 玩家选择敌方城市为目标时，军团 `Mission/Target` 与目标归属保持一致；
  - 玩家中途“委任给AI”时，AI 逻辑能按 `Attack/Defend` 正确分支执行，不再出现“目标敌城但仍是防守军团”的错配。

## 6. 后续排查建议（军团方向）
- 优先关注以下日志是否仍出现：
  - `Mission=None cannot be recovered`
  - `missing TargetArchitecture`
  - `missing WillArchitecture`
- 若仍有个案，优先检查：
  - 存档中该军团的 `Mission/WillArchitectureID/TargetArchitectureID`
  - `target.BelongedFaction` 与 `legion.BelongedFaction` 的一致性
  - 该军团是否由旧接口创建并被旧数据覆盖。
