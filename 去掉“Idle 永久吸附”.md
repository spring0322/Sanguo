这次更像是“运行期状态机锁死”，不是“新读档残留问题”，而且目前证据并不支持把它先定性成“军团没链接上部队”。
• 防守军团分支里，UpdateLegionMandate() 明明会根据敌情构造 defEnv，再交给状态机决定下一步状态，位置在 Troop.cs#L23153。
• 但状态机 DetermineNextAIState() 里有一个硬编码：只要部队当前已经是 Idle，就直接继续返回 Idle，完全不看 env.HasEnemyBlocker / env.IsAtDestination / env.IsDestinationFriendlyCity，位置在 TroopStateMachineRouter.cs#L126。
• 这就会形成死锁：防守部队一旦在运行几回合后被打成 Idle，后面即使军团目标还在、RealDest 还有效、视野里也有敌人，它也回不去 Marching / Combat / EnterCity。
• 而把部队打成 Idle 的运行期入口其实不少。最典型的是移动逻辑在“到达目标”后主动把状态重置为 Idle，位置在 Troop.cs#L21869。另外 Waiting 恢复分支还会把防守军团直接恢复成 Idle，位置在 Troop.cs#L23479。
• 之后 ExecuteTactics() 的 Idle 分支只做“贴身 1 格内有敌人就战斗”的自保逻辑，不负责继续行军，所以你日志里这种“RealDest 有值，但 AIState:Idle，然后部队不动”正好能对上，位置在 Troop.cs#L23508。
• 关于你怀疑的“军团没正常链接上部队”，我暂时没看到强证据。相反，日开始时还有一层自愈：如果 troop.BelongedLegion == null 或 troop 不在 legion.Troops 里，会单独重新跑 troop.AI()，位置在 GameScenario.cs#L2159。所以当前更像“军团对象还在，但部队状态被锁死”，不是“纯双向引用丢失”。
核心是去掉“Idle 永久吸附”。
1. 状态机修复：Idle 不再无条件维持
在 TroopStateMachineRouter.cs 调整了 DetermineNextAIState：
• Idle + 有敌阻挡 -> Combat
• Idle + 到达友方目标 -> EnterCity
• Idle + 未到达目标 -> Marching
这样防守军团部队不会因为中途被打成 Idle 后永久不动。
2. Waiting 恢复修复（防守军团）
在 Troop.cs 把防守军团从 Waiting 恢复时的状态由 Idle 改为 Marching，避免恢复后再次被锁死。
3. 增加防守路由诊断日志
在 Troop.cs 新增 [UpdateLegionMandate][DefRoute] 日志，输出 prev -> next 和 defEnv 关键输入，方便你回合内直接验证路由是否正确。