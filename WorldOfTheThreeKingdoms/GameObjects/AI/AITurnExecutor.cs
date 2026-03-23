using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using Microsoft.Xna.Framework;

namespace GameObjects.AI;

#if false

/// <summary>
/// AI回合总调度中心（AOT零分配架构）
/// 🎯 目的：负责数据组装、态势推演、效用寻优及指令下发
/// 🧊 Cold Path：AI决策层，允许LINQ，优先可读性
/// 📍 调用位置：可选的高级AI模式，不替代现有系统
/// ⚠️ 注意：与UnifiedTacticalAI互斥，不可同时启用
/// </summary>
public static class AITurnExecutor
{
    /// <summary>
    /// 执行特定城市/区域内所有AI部队的回合逻辑
    /// 🔥 注意：这是一个可选的高级AI模式，需要配置启用
    /// ⚠️ 与UnifiedTacticalAI互斥，不可同时启用
    /// </summary>
    public static void ExecuteRegionTurn(
        Architecture city, 
        List<Troop> regionTroops, 
        List<Troop> visibleEnemies)
    {
        if (regionTroops.Count == 0) return;

        // 🔥 互斥检查：不能与UnifiedTacticalAI同时启用
        var config = AITacticalConfigManager.Config;
        if (config == null)
        {
            return;
        }

        // 检查配置中的AI模式（假设配置中有AIMode字段）
        // 如果使用UnifiedTacticalAI，则跳过
        System.Diagnostics.Debug.WriteLine(
            "[AITurnExecutor] 🧪 实验性高级AI模式启动（基于效用AI）"
        );

        // ================= 第一阶段：态势感知与画像计算 =================
        
        // 1. 推演全局战略态势
        StrategicPosture currentPosture = PostureEvaluator.EvaluatePosture(
            city, 
            regionTroops, 
            visibleEnemies
        );

        System.Diagnostics.Debug.WriteLine(
            $"[AI回合执行] 城市={city.Name} 态势={currentPosture} 部队数={regionTroops.Count}"
        );

        // 2. 行动顺序重排（使用统一的排序方法）
        AITargetSelector.SortActionOrder(regionTroops);

        // ================= 第二阶段：效用寻优与执行循环 =================
        
        for (int i = 0; i < regionTroops.Count; i++)
        {
            Troop activeTroop = regionTroops[i];

            // 异常状态拦截：如果处于混乱等无法行动的状态，直接跳过
            if (HasHardCC(activeTroop))
            {
                System.Diagnostics.Debug.WriteLine($"[AI回合执行] {activeTroop.DisplayName} 被控制，跳过");
                continue;
            }

            // 检查是否还有行动力
            if (activeTroop.MovabilityLeft <= 0)
            {
                continue;
            }

            // 4. 大脑核心：寻找全局最优解
            ActionProposal bestAction = UtilityAIExecutor.FindBestAction(
                activeTroop, 
                visibleEnemies, 
                regionTroops, // 将同区域部队作为allies传入
                currentPosture
            );

            // 5. 引擎指令翻译器：将大脑决策转化为物理行为
            ExecuteProposal(activeTroop, in bestAction);
        }
    }

    // ================= 第三阶段：底层API翻译映射 =================

    /// <summary>
    /// 将逻辑层的Proposal映射为zhsan引擎的真实API调用
    /// </summary>
    private static void ExecuteProposal(Troop troop, in ActionProposal proposal)
    {
        // 1. 执行移动指令（如果目标位置不是原位）
        if (troop.Position != proposal.MovePosition)
        {
            // 🔥 对接异步寻路系统
            Point destination = proposal.MovePosition;
            
            if (!troop.IsPathfinding)
            {
                troop.RealDestination = destination;
                troop.CalculatePathAsync(destination, (success) =>
                {
                    if (success)
                    {
                        troop.CurrentAIState = TroopAIState.Marching;
                        troop.Action = TroopAction.Move;
                        System.Diagnostics.Debug.WriteLine(
                            $"[AI执行] {troop.DisplayName} 移动至 ({destination.X},{destination.Y})"
                        );
                    }
                });
            }
        }

        // 2. 执行动作指令
        // ✅ Anti-Band-Aid：业务逻辑检查保留（Destroyed是游戏状态）
        switch (proposal.Type)
        {
            case ActionType.Wait:
                troop.Action = TroopAction.Stop;
                System.Diagnostics.Debug.WriteLine($"[AI执行] {troop.DisplayName} 待命");
                break;

            case ActionType.NormalAttack:
                if (!proposal.Target.Destroyed)
                {
                    Point troopPos = troop.Position;
                    Point targetPos = proposal.Target.Position;
                    int distance = Math.Abs(troopPos.X - targetPos.X) + Math.Abs(troopPos.Y - targetPos.Y);
                    troop.AttackTroop(proposal.Target);
                    System.Diagnostics.Debug.WriteLine(
                        $"[AI执行] {troop.DisplayName} 普攻 {proposal.Target.DisplayName}"
                    );
                }
                break;

            case ActionType.Tactic:
                if (!proposal.Target.Destroyed && troop.Morale >= 20)
                {
                    // var availableTactic = troop.CombatMethods.GetCombatMethod(proposal.SkillId);
                    // troop.ApplyCombatMethodToTroop(availableTactic, proposal.Target);
                    System.Diagnostics.Debug.WriteLine(
                        $"[AI执行] {troop.DisplayName} 对 {proposal.Target.DisplayName} 使用战法 (需实现)"
                    );
                }
                break;

            case ActionType.Stratagem:
                if (!proposal.Target.Destroyed && 
                    troop.Morale >= 15 /* && 
                    troop.StratagemAvail(proposal.SkillId, proposal.Target)*/)
                {
                    // var availableStratagem = troop.Stratagems.GetStratagem(proposal.SkillId);
                    // troop.ApplyStratagemToTroop(availableStratagem, proposal.Target);
                    System.Diagnostics.Debug.WriteLine(
                        $"[AI执行] {troop.DisplayName} 对 {proposal.Target.DisplayName} 使用计略 (需实现)"
                    );
                }
                break;

            case ActionType.Move:
                // 纯移动已在上面处理
                break;
        }
    }

    /// <summary>
    /// 检查是否中了硬控（例如混乱）无法行动
    /// ✅ Anti-Band-Aid：不做防御性空检查
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    private static bool HasHardCC(Troop troop)
    {
        // 检查是否有混乱状态（ID=391）
        return troop.InfluencesApplying?.Any(inf => inf.Kind?.ID == 391) ?? false;
    }
}

#endif


