using System;
using GameObjects.Animations;
using GameObjects.Influences;
using GameObjects.Conditions;
using GameObjects.MapDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.FactionDetail;
using GameObjects.AI;
using GameObjects.AI.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameObjects
{
    public enum QueueAction : byte 
    { 
        SkipTurn, 
        EndTroop, 
        ForceCombatCheck, 
        ExecuteStratagemDirectly, 
        EnterMovementPipeline 
    }

    // 🔥 优化：使用 readonly 字段 + 构造函数，消除 init 访问器的元数据检查
    public readonly ref struct EnvContext 
    { 
        public readonly bool IsAtDestination;
        public readonly bool HasEnemyBlocker;
        public readonly bool IsDestinationFriendlyCity;
        public readonly bool HasEnemiesNearby;
        
        public EnvContext(bool isAtDest, bool hasBlocker, bool isFriendly, bool hasNearby)
        {
            IsAtDestination = isAtDest;
            HasEnemyBlocker = hasBlocker;
            IsDestinationFriendlyCity = isFriendly;
            HasEnemiesNearby = hasNearby;
        }
    }

    public static class TroopStateMachineRouter
    {
        public static QueueAction DetermineQueueAction(Troop candidate)
        {
            if (candidate.Status == TroopStatus.混乱 || candidate.Status == TroopStatus.埋伏)
                return QueueAction.SkipTurn;

            if (candidate.MovabilityLeft <= 0)
            {
                if (candidate.OperationDone)
                    return QueueAction.EndTroop;
                
                return QueueAction.ForceCombatCheck;
            }

            if (!candidate.OperationDone && candidate.Command == TroopCommand.Stratagem)
            {
                return QueueAction.ExecuteStratagemDirectly;
            }

            if (!candidate.OperationDone && candidate.Command != TroopCommand.Stratagem)
            {
                return QueueAction.EnterMovementPipeline;
            }

            return QueueAction.EndTroop;
        }

        public static bool CanExecuteCombatAction(Troop troop)
        {
            
            
            if (troop.OperationDone || troop.Will != TroopWill.行军)
            {
                
                return false;
            }

            // 🔥 2026-03-22 修复：检查是否需要调整位置到最佳射程
            // 问题：原逻辑只检查"是否有攻击目标"，不检查"当前位置是否适合攻击"
            // 改进：如果部队有 RealDestination 且与当前位置不同，说明需要移动到更好的位置
            // 参考：远程部队位置调整失败_复用战术评分系统修复_2026-03-22.md
            if (troop.RealDestination != troop.Position && 
                troop.RealDestination.X >= 0 && troop.RealDestination.Y >= 0)
            {
                // 部队需要移动到目标位置，不应该立即攻击
                
                return false;
            }

            if (troop.CurrentStratagem == null)
            {
                var target = troop.GetAttackOrientationObject(troop.QueueEnded);
                
                
                
                return target != null;
            }

            if (troop.CurrentStratagem.Self)
            {
                
                return true;
            }

            // 🔥 优化：使用三元运算符减少分支跳转
            bool result = troop.OrientationTroop != null || troop.GetCastOrientationTroop(troop.QueueEnded) != null;
            
            
            
            return result;
        }

        public static TroopAIState DetermineNextAIState(Troop troop, in EnvContext env)
        {
            // 最高优先级拦截：若 Status 是 伪报，返回 ForcedRetreat；若 Status 是 挑衅，返回 ForcedChase。
            if (troop.Status == TroopStatus.伪报)
                return TroopAIState.ForcedRetreat;

            if (troop.Status == TroopStatus.挑衅)
                return TroopAIState.ForcedChase;

            // 🔥 优化：按实际概率排序，提升分支预测命中率
            // Marching/Idle 遇到敌人 -> Combat
            // 到达友方城市 -> EnterCity
            // Idle 且未到达 -> Marching（避免长期Idle锁死）
            return (troop.CurrentAIState, env.IsAtDestination, env.HasEnemyBlocker, env.IsDestinationFriendlyCity) switch
            {
                // 遇敌优先进入战斗（包括从 Idle 直接转战斗）
                (TroopAIState.Marching, _, true, _) => TroopAIState.Combat,
                (TroopAIState.Idle, _, true, _) => TroopAIState.Combat,
                // 次高频：Marching 到达友方城市
                (TroopAIState.Marching, true, _, true) => TroopAIState.EnterCity,
                (TroopAIState.Idle, true, _, true) => TroopAIState.EnterCity,
                // 中频：Retreating 到达目的地
                (TroopAIState.Retreating, true, _, _) => TroopAIState.EnterCity,
                // 关键修复：Idle 在有目标但未到达时应恢复行军，避免永久待命
                (TroopAIState.Idle, false, _, _) => TroopAIState.Marching,
                // 默认：保持当前状态
                _ => troop.CurrentAIState
            };
        }
    }
}
