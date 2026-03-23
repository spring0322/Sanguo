using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GameObjects
{
    /// <summary>
    /// Troop 类的动作状态锁扩展（防止 AI "多动症"）
    /// </summary>
    public partial class Troop
    {
        // ========================================
        // 🎯 动作状态锁辅助方法
        // ========================================

        /// <summary>
        /// 检查移动是否完成
        /// </summary>
        private bool CheckMovementCompleted()
        {
            // 🔥 防御性检查：如果 _currentMoveTask 为 null（反序列化错误或状态不一致）
            // 这不是掩盖数据错误，而是防止反序列化后的状态不一致
            if (_currentMoveTask is null)
            {
                Debug.WriteLine($"[CheckMovementCompleted] {this.DisplayName} 警告：_currentMoveTask 为 null，强制解除移动锁");
                _actionLock = ActionLockState.None; // 强制解锁
                return true; // 视为移动完成
            }

            // 🔥 修复：如果任务已完成但还有路径要走，重新启动任务
            if (_currentMoveTask.IsCompleted)
            {
                // 检查是否还有剩余路径或未到达目标
                bool hasRemainingPath = this.FirstTierPath.Count > 0;
                bool notAtDestination = this.RealDestination != new Point(-1, -1) 
                    && this.RealDestination != Point.Zero 
                    && !IsAtPosition(this.RealDestination);
                
                #if DEBUG
                Debug.WriteLine($"[CheckMovementCompleted] {this.DisplayName} 任务已完成: hasPath={hasRemainingPath}, notAtDest={notAtDestination}, MovLeft={this.MovabilityLeft}");
                #endif
                
                // ★★★ 修复：只有在有移动力时才重新启动任务 ★★★
                // 日期：2026-03-04
                // 原因：CheckMovementCompleted 在移动力为0时重启任务，导致 ExecuteMoveTurnAsync 立即失败
                //       形成死循环：分配目标 → 移动力不足 → 任务完成 → 重启任务 → 移动力不足
                // 解决：如果移动力已耗尽，视为任务完成，等待下回合 InitializeInQueue 恢复移动力
                if ((hasRemainingPath || notAtDestination) && this.MovabilityLeft > 0)
                {
                    // 还有路径要走且有移动力，重新启动移动任务
                    _oldSystemMoveCts?.Dispose();
                    _oldSystemMoveCts = new CancellationTokenSource();
                    
                    // 根据当前状态决定是否激进模式
                    bool isAggressive = this.CurrentAIState == TroopAIState.Marching;
                    
                    #if DEBUG
                    Debug.WriteLine($"[CheckMovementCompleted] {this.DisplayName} 重新启动移动任务");
                    #endif
                    
                    _currentMoveTask = ExecuteMoveTurnAsync(
                        isAggressive: isAggressive, 
                        animationDelayPerStep: 250, 
                        cancellationToken: _oldSystemMoveCts.Token
                    );
                    
                    return false; // 还在移动中
                }
                
                // 任务完成（已到达目标 或 移动力耗尽）
                return true;
            }

            // 任务还在执行中
            return false;
        }

        /// <summary>
        /// 移动完成后处理后续计划（攻击/战法/计略）
        /// </summary>
        private void ProcessPlanAfterMovement()
        {
            Debug.WriteLine($"[ProcessPlanAfterMovement] {this.DisplayName} 移动完成，处理后续计划");

            // 解除移动锁
            _actionLock = ActionLockState.None;

            // 检查是否有待执行的战斗计划
            if (!_pendingCombatPlan.HasValue)
            {
                return; // 单纯移动，无后续计划
            }

            // 根据计划类型执行不同动作
            CombatPlan plan = _pendingCombatPlan.Value;
            
            // 🔥 WEGO 修复：增加计略执行分支
            // 日期：2026-02-27
            if (plan.StratagemToCast is not null && plan.Target is not null)
            {
                // 执行计略（数值结算在 StartCastTroop 内部完成）
                this.CurrentStratagem = plan.StratagemToCast;
                this.StartCastTroop(plan.Target);
                _actionLock = ActionLockState.PlayingAnimation;
            }
            else if (plan.MethodToCast is not null)
            {
                // 触发战法表现，并上动画锁
                StartCastMethodAnimation(plan.MethodToCast);
                _actionLock = ActionLockState.PlayingAnimation;
            }
            else if (plan.Target is not null)
            {
                // 触发普攻表现，并上动画锁
                StartAttackAnimation(plan.Target);
                _actionLock = ActionLockState.PlayingAnimation;
            }
            else
            {
                // 计划无效，清空
                _pendingCombatPlan = null;
            }
        }

        /// <summary>
        /// 检查当前动画是否播放完毕
        /// </summary>
        private bool CheckCurrentAnimationFinished()
        {
            // 🔥 WEGO 修复：强制兜底机制
            // 日期：2026-02-27
            // 原因：即使动画未完成，也必须在合理时间内解除锁定，避免永久卡死
            // 解决：使用帧计数器，超时后强制解锁
            
            // 增加动画帧计数器（需要在 Troop 类中添加字段）
            _animationFrameCounter++;
            
            // 超时阈值：60帧（1秒）后强制解锁
            const int ANIMATION_TIMEOUT_FRAMES = 60;
            
            if (_animationFrameCounter >= ANIMATION_TIMEOUT_FRAMES)
            {
                Debug.WriteLine($"[CheckCurrentAnimationFinished] {this.DisplayName} 动画超时，强制解锁");
                _animationFrameCounter = 0;
                
                // 清理动画状态
                this.CurrentCombatMethod = null;
                this.CurrentStratagem = null;
                
                return true; // 强制视为完成
            }
            
            // TODO: 这里需要与动画系统对接
            // 暂时简化处理：假设动画立即完成
            // 未来可以检查 TileAnimation 的状态
            
            // 如果有正在播放的战法动画
            if (this.CurrentCombatMethod is not null)
            {
                // 检查动画是否播放完毕（需要动画系统支持）
                // 暂时返回 true，表示立即完成
                _animationFrameCounter = 0;
                return true;
            }

            // 如果有正在播放的计略动画
            if (this.CurrentStratagem is not null)
            {
                _animationFrameCounter = 0;
                return true;
            }

            // 没有动画在播放
            _animationFrameCounter = 0;
            return true;
        }

        /// <summary>
        /// 启动战法动画（为未来扩展预留）
        /// </summary>
        private void StartCastMethodAnimation(CombatMethod method)
        {
            Debug.WriteLine($"[StartCastMethodAnimation] {this.DisplayName} 开始释放战法: {method.Name}");
            
            // TODO: 这里应该触发战法动画
            // 1. 播放转身动画（朝向目标）
            // 2. 播放战法施法动画
            // 3. 播放战法特效
            // 4. 动画结束后结算伤害
            
            // 暂时简化处理：直接执行战法效果
            // 注意：_pendingCombatPlan 在此处保证有值（由 ProcessPlanAfterMovement 调用）
            if (_pendingCombatPlan.HasValue && _pendingCombatPlan.Value.Target is not null)
            {
                // 执行战法逻辑（这里需要调用现有的战法系统）
                // 例如：this.CastCombatMethodOn(_pendingCombatPlan.Value.Target, method);
            }
        }

        /// <summary>
        /// 启动攻击动画（为未来扩展预留）
        /// </summary>
        private void StartAttackAnimation(Troop target)
        {
            Debug.WriteLine($"[StartAttackAnimation] {this.DisplayName} 开始攻击: {target.DisplayName}");
            
            // TODO: 这里应该触发攻击动画
            // 1. 播放转身动画（朝向目标）
            // 2. 播放攻击动画
            // 3. 播放命中特效
            // 4. 动画结束后结算伤害
            
            // 暂时简化处理：直接执行攻击
            // this.AttackTroop(target);
        }

        /// <summary>
        /// 设置待执行的战斗计划（在 ExecuteTactics 中调用）
        /// </summary>
        public void SetPendingCombatPlan(CombatPlan? plan)
        {
            _pendingCombatPlan = plan;
        }

        /// <summary>
        /// 启动异步移动并上锁（在 ExecuteTactics 中调用）
        /// </summary>
        public void StartAsyncMovementWithLock()
        {
            _actionLock = ActionLockState.MovingOnMap;
        }
    }
}
