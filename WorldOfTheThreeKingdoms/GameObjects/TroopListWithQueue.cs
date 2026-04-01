using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using Microsoft.Xna.Framework; // 必需：用于 GameTime
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using GameObjects.AI; // 🔥 2026-03-22 添加：用于 AITacticalPositioner

namespace GameObjects
{
    [DataContract]
    public class TroopListWithQueue : TroopList
    {
        public TroopList AmbushList = new TroopList();
        public Queue<Troop> CurrentQueue = new Queue<Troop>();
        public Troop CurrentTroop;
        private bool queueEnded = true;
        private Queue<Troop> troopQueue = new Queue<Troop>();

        public void Init()
        {
            queueEnded = true;
            AmbushList = new TroopList();
            CurrentQueue = new Queue<Troop>();
            troopQueue = new Queue<Troop>();
        }

        public void BuildQueue()
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[BuildQueue] 🔍 被调用！开始构建新回合队列");
            #endif
            
            this.queueEnded = false;
            if (this.troopQueue.Count != 0)
            {
                // 🔥 增强诊断：记录残余部队详情
                System.Diagnostics.Debug.WriteLine($"[TroopListWithQueue] ⚠️ troopQueue不为空 (Count={this.troopQueue.Count})，正在清理...");

                int validTroops = 0, destroyedTroops = 0, stuckTroops = 0;
                foreach (Troop troop in this.troopQueue)
                {
                    if (troop.Destroyed)
                    {
                        destroyedTroops++;
                    }
                    else if (troop.MovabilityLeft > 0 && !troop.OperationDone)
                    {
                        stuckTroops++;
                        System.Diagnostics.Debug.WriteLine($"  卡死部队: {troop.DisplayName}(ID:{troop.ID}) MovabilityLeft:{troop.MovabilityLeft} Status:{troop.Status}");
                    }
                    else
                    {
                        validTroops++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"  分析: 有效={validTroops}, 已销毁={destroyedTroops}, 卡死={stuckTroops}");

                this.troopQueue.Clear();
            }
            this.AmbushList.Clear();
            GameObjectList randomList = base.GetRandomList();

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[BuildQueue] 共有 {randomList.Count} 个部队待处理");
            #endif

            // 🔥 新增：智能排序（速度优先，同速度时按距离目标排序）
            // 日期：2026-03-20
            // 原因：多支部队同向移动时，后方部队先动导致被前方部队阻挡
            // 解决：离目标近的部队优先移动，避免阻挡
            if (randomList.Count > 1)
            {
                // 🔥 C# 12：使用集合表达式（Cold Path 允许）
                List<Troop> troops = [];
                foreach (GameObject obj in randomList)
                {
                    if (obj is Troop troop)
                    {
                        troop.BeginExecutionTargetFromCurrentState();
                        troops.Add(troop);
                    }
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[BuildQueue] 排序前部队列表:");
                for (int i = 0; i < troops.Count; i++)
                {
                    var t = troops[i];
                    Point target = t.GetExecutionTargetPosition();
                    bool hasTarget = target.X >= 0 && target.Y >= 0;
                    int dist = hasTarget ? Math.Abs(t.Position.X - target.X) + Math.Abs(t.Position.Y - target.Y) : -1;
                    System.Diagnostics.Debug.WriteLine($"  [{i}] {t.DisplayName} 位置={t.Position} 目标={t.RealDestination} 距离={dist} 速度={t.Speed}");
                }
                #endif
                
                troops.Sort((a, b) =>
                {
                    // 第一优先级：速度（保持原有逻辑）
                    if (Session.GlobalVariables.MilitaryKindSpeedValid)
                    {
                        int speedCompare = b.Speed.CompareTo(a.Speed);
                        if (speedCompare != 0)
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[BuildQueue] 速度排序: {b.DisplayName}(速度={b.Speed}) 优先于 {a.DisplayName}(速度={a.Speed})");
                            #endif
                            return speedCompare;
                        }
                    }
                    
                    // 第二优先级：距离目标（同速度时）
                    // 🔥 关键修复：只对有明确目标的部队进行距离排序
                    // 日期：2026-03-20
                    // 原因：没有目标的部队（驻守、待命）不应该参与移动排序
                    // 解决：没有目标的部队排在最后（距离视为无穷大）
                    // 注意：RealDestination 初始值是 (-1, -1)，表示无目标
                    Point targetA = a.GetExecutionTargetPosition();
                    Point targetB = b.GetExecutionTargetPosition();
                    bool hasTargetA = targetA.X >= 0 && targetA.Y >= 0;
                    bool hasTargetB = targetB.X >= 0 && targetB.Y >= 0;
                    
                    // 有目标的部队优先于没有目标的部队
                    if (hasTargetA && !hasTargetB)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[BuildQueue] 目标排序: {a.DisplayName}(有目标) 优先于 {b.DisplayName}(无目标)");
                        #endif
                        return -1;
                    }
                    if (!hasTargetA && hasTargetB)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[BuildQueue] 目标排序: {b.DisplayName}(有目标) 优先于 {a.DisplayName}(无目标)");
                        #endif
                        return 1;
                    }
                    if (!hasTargetA && !hasTargetB) return 0;  // 都没有目标，顺序无所谓
                    
                    // 都有目标：按距离目标排序（曼哈顿距离）
                    int distA = Math.Abs(a.Position.X - targetA.X) + 
                                Math.Abs(a.Position.Y - targetA.Y);
                    int distB = Math.Abs(b.Position.X - targetB.X) + 
                                Math.Abs(b.Position.Y - targetB.Y);
                    
                    int distCompare = distA.CompareTo(distB);
                    
                    #if DEBUG
                    if (distCompare != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildQueue] 距离排序: {a.DisplayName}(距离={distA}) 优先于 {b.DisplayName}(距离={distB})");
                    }
                    #endif
                    
                    return distCompare;
                });
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[BuildQueue] 排序后部队列表:");
                for (int i = 0; i < troops.Count; i++)
                {
                    var t = troops[i];
                    bool hasTarget = t.RealDestination.X >= 0 && t.RealDestination.Y >= 0;
                    int dist = hasTarget ? Math.Abs(t.Position.X - t.RealDestination.X) + Math.Abs(t.Position.Y - t.RealDestination.Y) : -1;
                    System.Diagnostics.Debug.WriteLine($"  [{i}] {t.DisplayName} 位置={t.Position} 目标={t.RealDestination} 距离={dist} 速度={t.Speed}");
                }
                #endif
                
                randomList.Clear();
                foreach (Troop t in troops)
                {
                    randomList.Add(t);
                }
            }
            
            foreach (Troop troop in randomList)
            {
                // 🔥 根本修复：无条件初始化，确保冷却被重置
                // 日期：2026-02-27
                // 原因：第一回合结束时 _moveStepCooldown 有残留值（如 0.183 秒）
                //       如果 CanMoveAnyway() 返回 false，InitializeInQueue() 不被调用，冷却不被重置
                //       导致第二回合 ExecutePathMove() 检查冷却 > 0，直接返回，部队无法移动
                // 解决：无条件调用 InitializeInQueue()，确保冷却、MovabilityLeft 等状态被重置
                //       只有"是否入队"需要 canMove 判断
                troop.InitializeInQueue();
                troop.BeginExecutionTargetFromCurrentState();
                
                bool canMove = troop.CanMoveAnyway();
                
                #if DEBUG
                if (troop.ManualControl)
                {
                    System.Diagnostics.Debug.WriteLine($"[BuildQueue] {troop.DisplayName} CanMoveAnyway={canMove}, Status={troop.Status}, Controllable={troop.Controllable}");
                }
                #endif

                if (canMove)
                {
                    if (troop.Status == TroopStatus.埋伏)
                    {
                        this.AmbushList.Add(troop);
                    }
                    else
                    {
                        this.troopQueue.Enqueue(troop);
                        
                        // 🔥 诊断日志：记录玩家部队入队
                        if (troop.ManualControl)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BuildQueue] ✅ 玩家部队 {troop.DisplayName} 已入队 - SelectedMove={troop.SelectedMove}, Command={troop.Command}, Operated={troop.Operated}, MovLeft={troop.MovabilityLeft}, Status={troop.Status}");
                        }
                    }
                }
                else
                {
                    // 🔥 诊断日志：记录玩家部队未入队的原因
                    if (troop.ManualControl)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildQueue] ❌ 玩家部队 {troop.DisplayName} 未入队 - CanMove=false, Status={troop.Status}, Controllable={troop.Controllable}, MovLeft={troop.MovabilityLeft}");
                    }
                }

                troop.Operated = false;
                
                // 🔥 根本修复：不要清空玩家下达的指令标志
                // 日期：2026-02-27
                // 原因：玩家下达计略指令后设置了 SelectedAttack=true 和 mingling="Stratagem"
                //       BuildQueue 在回合开始时无条件清空 SelectedAttack，导致计略指令丢失
                // 解决：只清空移动指令，保留攻击/计略指令
                troop.SelectedMove = false;
                
                // 只有在没有计略指令时才清空 SelectedAttack
                if (troop.Command != TroopCommand.Stratagem)
                {
                    troop.SelectedAttack = false;
                }
                
                troop.Controllable = true;

                // 🔥 根本修复：不要在 BuildQueue 中设置 Action=Move
                // 日期：2026-02-27
                // 原因：设置 Action=Move 会触发 Animating=true，导致 MoveTheTroops 跳过 CurrentQueueTroopMove()
                // 解决：始终设置为 Stop，让 UpdateMovementLogic 根据实际情况决定 Action
                troop.Action = TroopAction.Stop;
                
                #if DEBUG
                if (troop.ManualControl)
                {
                    System.Diagnostics.Debug.WriteLine($"[BuildQueue] 重置标志后: {troop.DisplayName} Operated={troop.Operated}, Controllable={troop.Controllable}, Status={troop.Status}, OperationDone={troop.OperationDone}, HasToDoCombatAction={troop.HasToDoCombatAction}");
                }
                #endif
            }

            // System.Diagnostics.Debug.WriteLine($"[BuildQueue] 队列构建完成: 主队列={this.troopQueue.Count}, 伏兵={this.AmbushList.Count}");
        }

        private bool CheckAmbushList()
        {
            Troop gameObject = null;
            // int count = this.AmbushList.Count; // Unused
            foreach (Troop troop2 in this.AmbushList)
            {
                if (troop2.ToDoCombatAction())
                {
                    troop2.DoCombatAction();
                    if (troop2.OperationDone)
                    {
                        gameObject = troop2;
                        break;
                    }
                }
            }
            this.AmbushList.Remove(gameObject);
            return (gameObject != null);
        }

#pragma warning disable CS0108 
        public void Clear()
#pragma warning restore CS0108 
        {
            base.GameObjects.Clear();
            this.troopQueue.Clear();
            this.CurrentQueue.Clear();
            this.AmbushList.Clear();
        }

        /// <summary>
        /// 【核心重构】新的队列驱动方法
        /// 🔥 动画阻塞重构：废除全局 Animating 检查，改用部队级 IsAnimationPlaying
        /// 日期：2026-02-28
        /// 性能：零分配，纯布尔轮询，O(1) 复杂度
        /// </summary>
        /// <param name="gameTime">游戏时间，用于冷却计算</param>
        public void CurrentQueueTroopMove(GameTime gameTime = null)
        {
            // 1. 如果当前有部队正在行动
            if (this.CurrentTroop != null)
            {
                #if DEBUG
                // 🔥 诊断：记录当前部队的完整状态
                if (this.CurrentTroop.DisplayName.Contains("张宝") || this.CurrentTroop.DisplayName.Contains("荀攸"))
                {
                    System.Diagnostics.Debug.WriteLine($"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 状态: Destroyed={this.CurrentTroop.Destroyed}, IsAnimationPlaying={this.CurrentTroop.IsAnimationPlaying}, Position={this.CurrentTroop.Position}, RealDestination={this.CurrentTroop.RealDestination}, CurrentAIState={this.CurrentTroop.CurrentAIState}");
                    System.Diagnostics.Debug.WriteLine($"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 动画状态: Action={this.CurrentTroop.Action}, ShowNumber={this.CurrentTroop.ShowNumber}, PreAction={this.CurrentTroop.PreAction}, WaitForDeepChaosFrameCount={this.CurrentTroop.WaitForDeepChaosFrameCount}, _isPathfinding={this.CurrentTroop.IsPathfinding}");
                }
                #endif
                
                // 🔥 动画阻塞重构：检查部队自身的动画状态
                // 日期：2026-02-28
                if (this.CurrentTroop.Destroyed)
                {
                    this.CurrentTroop = null;
                }
                else if (this.CurrentTroop.IsAnimationPlaying)
                {
                    // 🔥 2026-03-20 修复：区分"真正的动画"和"被友军阻挡"
                    // 问题：IsAnimationPlaying 包括异步寻路和被友军阻挡，导致整个队列被阻塞
                    // 解决：如果是被友军阻挡（Action=Stop 且有路径），跳过当前部队，让其他部队先走
                    
                    bool isBlockedByFriendly = this.CurrentTroop.Action == TroopAction.Stop && 
                                               this.CurrentTroop.HasCachedPath &&
                                               !this.CurrentTroop.IsPathfinding;
                    
                    if (isBlockedByFriendly)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 被友军阻挡，跳过，让其他部队先走");
                        #endif
                        
                        // 🔥 关键：将当前部队重新放回队列末尾
                        // 这样其他部队可以先移动，等友军让开后再尝试
                        this.CurrentQueue.Enqueue(this.CurrentTroop);
                        this.CurrentTroop = null;
                        
                        // 继续处理下一个部队（不 return）
                    }
                    else
                    {
                        // 真正的动画播放（异步寻路、战法动画等），等待下一帧
                        // 不阻塞主线程，不分配对象，只是简单返回
                        // 重要：继续驱动部队的移动逻辑（如动画帧推进）
                        this.CurrentTroop.UpdateMovementLogic(gameTime);
                        return; // 本帧只处理这一个部队
                    }
                }
                else
                {
                    // 🔥 2026-03-22 修复：检查是否需要执行攻击，如果不需要则继续移动
                    // 问题：原逻辑在 ToDoCombatAction() 返回 false 后直接设置 OperationDone=true，结束回合
                    // 改进：如果不能攻击（需要移动到更好的位置），继续驱动移动逻辑
                    // 参考：远程部队位置调整失败_CanExecuteCombatAction检查缺失修复_2026-03-22.md
                    
                    bool isNoTargetCleanup = this.CurrentTroop.OperationDone &&
                                             this.CurrentTroop.Command == TroopCommand.None &&
                                             this.CurrentTroop.CurrentAIState == TroopAIState.Idle &&
                                             this.CurrentTroop.MovabilityLeft < 0;
                    
                    #if DEBUG
                    if (!isNoTargetCleanup)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 检查是否需要执行攻击 " +
                            $"(Pos={this.CurrentTroop.Position}, RealDest={this.CurrentTroop.RealDestination}, " +
                            $"Command={this.CurrentTroop.Command}, TargetTroop={this.CurrentTroop.TargetTroop?.DisplayName ?? "null"}, " +
                            $"TargetArch={this.CurrentTroop.TargetArchitecture?.Name ?? "null"})");
                    }
                    #endif
                    
                    bool canExecuteCombat = this.CurrentTroop.ToDoCombatAction();
                    
                    #if DEBUG
                    if (!isNoTargetCleanup)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} ToDoCombatAction 返回: {canExecuteCombat}");
                    }
                    #endif
                    
                    if (canExecuteCombat)
                    {
                        // 可以执行攻击
                        if (!this.CurrentTroop.HasToDoCombatAction)
                        {
                            this.CurrentTroop.HasToDoCombatAction = true;
                            this.CurrentTroop.DoCombatAction();
                        }
                        
                        // 🔥 根本修复：标记部队为"已完成"，防止无限循环
                        this.CurrentTroop.OperationDone = true;
                        this.CurrentTroop = null;
                    }
                    else
                    {
                        // 不能执行攻击，继续移动
                        
                        #if DEBUG
                        if (!isNoTargetCleanup)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} canExecuteCombat=false，调用 UpdateMovementLogic " +
                                $"(RealDest={this.CurrentTroop.RealDestination}, Pos={this.CurrentTroop.Position}, " +
                                $"State={this.CurrentTroop.CurrentAIState}, Command={this.CurrentTroop.Command})");
                        }
                        #endif
                        
                        // 🔥 关键：调用 UpdateMovementLogic 驱动移动
                        this.CurrentTroop.UpdateMovementLogic(gameTime);
                        
                        // 🔥 2026-03-22 修复：检查部队是否真的在移动
                        // 问题：如果 UpdateMovementLogic 无法移动（寻路失败、目标无效等），会陷入死循环
                        // 解决：检查部队是否正在移动，如果不是则标记为完成
                        // 
                        // 🔥 2026-03-22 修复：机动力耗尽时不认为"正在移动"
                        // 问题：部队有路径但机动力耗尽（MovabilityLeft=0），ExecutePathMove 直接返回
                        //       导致 HasCachedPath=true 但实际不移动，陷入死循环
                        // 解决：只有在有路径且有机动力时，才认为部队"正在移动"
                        
                        bool isActuallyMoving = this.CurrentTroop.IsAnimationPlaying || 
                                               this.CurrentTroop.IsPathfinding ||
                                               (this.CurrentTroop.HasCachedPath && this.CurrentTroop.MovabilityLeft > 0);
                        
                        #if DEBUG
                        if (!isNoTargetCleanup)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} UpdateMovementLogic 后: " +
                                $"IsAnimationPlaying={this.CurrentTroop.IsAnimationPlaying}, " +
                                $"IsPathfinding={this.CurrentTroop.IsPathfinding}, " +
                                $"HasCachedPath={this.CurrentTroop.HasCachedPath}, " +
                                $"MovabilityLeft={this.CurrentTroop.MovabilityLeft}, " +
                                $"isActuallyMoving={isActuallyMoving}");
                        }
                        #endif
                        
                        if (!isActuallyMoving)
                        {
                            // 部队无法移动（无有效目标、寻路失败、状态异常等）
                            // 标记为完成，避免死循环
                            
                            #if DEBUG
                            if (isNoTargetCleanup)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 已在目标判定阶段正常结束，跳过重复的移动失败日志");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[CurrentQueueTroopMove] {this.CurrentTroop.DisplayName} 无法移动，标记为完成 " +
                                    $"(RealDest={this.CurrentTroop.RealDestination}, Pos={this.CurrentTroop.Position}, " +
                                    $"State={this.CurrentTroop.CurrentAIState}, stuckedFor={this.CurrentTroop.stuckedFor})");
                            }
                            #endif
                            
                            this.CurrentTroop.OperationDone = true;
                            this.CurrentTroop = null;
                        }
                        
                        // 如果正在移动，继续等待下一帧
                        return;
                    }
                }
            }

            // 2. 如果当前没有部队，寻找下一个 (Loop直到找到能动的或者队列空)
            // 这里使用循环是为了跳过那些虽然在队列里但实际不需要动的部队（如已阵亡、无机动力）
            // 避免空跑几帧
            int safetyCounter = 0; // 🔥 防止死循环
            while (this.CurrentTroop == null)
            {
                // 🔥 安全检查：防止死循环
                if (++safetyCounter > 1000)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ TroopListWithQueue: 检测到潜在死循环，强制退出");
                    return;
                }

                // 2a. 检查伏兵
                if (this.CheckAmbushList())
                {
                    return; // 伏兵行动了，本帧结束
                }

                // 2b. 补充 CurrentQueue
                if (this.CurrentQueue.Count == 0)
                {
                    if (this.troopQueue.Count > 0)
                    {
                        // 从总队列搬运一个到当前队列
                        this.CurrentQueue.Enqueue(this.troopQueue.Dequeue());
                    }
                    else
                    {
                        // 彻底没兵了，检查是否所有人都跑完了
                        if (!this.queueEnded && this.TotallyEmpty)
                        {
                            this.HandleQueueEnd();
                            if (this.troopQueue.Count == 0)
                            {
                                return; // 真的没了
                            }
                        }
                        else
                        {
                            return; // 队列空了
                        }
                    }
                }

                // 2c. 从 CurrentQueue 取出一个
                if (this.CurrentQueue.Count > 0)
                {
                    Troop candidate = this.CurrentQueue.Dequeue();
                    
                    #if DEBUG
                    if (candidate.ManualControl)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CurrentQueueTroopMove] 从队列取出: {candidate.DisplayName}, Destroyed={candidate.Destroyed}");
                    }
                    #endif

                    // 校验有效性
                    if (candidate.Destroyed)
                    {
                        #if DEBUG
                        if (candidate.ManualControl)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CurrentQueueTroopMove] {candidate.DisplayName} 已销毁，跳过");
                        }
                        #endif
                        continue;
                    }

                    // === 找到有效部队 ===
                    this.CurrentTroop = candidate;

                    if (Session.GlobalVariables != null && Session.GlobalVariables.EnableAIAuthorityPhase1)
                    {
                        GameScenario scenario = Session.Current?.Scenario;
                        if (scenario != null)
                        {
                            var authorityContext = scenario.EnsureAIAuthorityContext();
                            if (!authorityContext.ValidateAndHandleIntentCheckpoint(scenario, this.CurrentTroop, IntentCheckpointKind.QueuePickup))
                            {
                                this.CurrentTroop.OperationDone = true;
                                this.CurrentTroop = null;
                                continue;
                            }
                        }
                    }

                    var action = TroopStateMachineRouter.DetermineQueueAction(this.CurrentTroop);
                    switch (action)
                    {
                        case QueueAction.SkipTurn:
                            this.CurrentTroop.MovabilityLeft = -1;
                            this.CurrentTroop.OperationDone = true;
                            this.CurrentTroop = null;
                            continue;
                            
                        case QueueAction.EndTroop:
                            this.CurrentTroop = null;
                            continue;
                            
                        case QueueAction.ForceCombatCheck:
                            if (!this.CurrentTroop.HasToDoCombatAction && this.CurrentTroop.ToDoCombatAction())
                            {
                                this.CurrentTroop.HasToDoCombatAction = true;
                                this.CurrentTroop.DoCombatAction();
                            }
                            this.CurrentTroop = null;
                            continue;

                        case QueueAction.ExecuteStratagemDirectly:
                            if (!this.CurrentTroop.HasToDoCombatAction && this.CurrentTroop.ToDoCombatAction())
                            {
                                this.CurrentTroop.HasToDoCombatAction = true;
                                this.CurrentTroop.DoCombatAction();
                            }
                            this.CurrentTroop = null;
                            continue;

                        case QueueAction.EnterMovementPipeline:
                            // 🔍 诊断：记录进入移动管线前的状态
                            #if DEBUG
                            if (this.CurrentTroop.ManualControl)
                            {
                                System.Diagnostics.Debug.WriteLine($"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 进入前: RealDest={this.CurrentTroop.RealDestination}, Command={this.CurrentTroop.Command}, mingling={this.CurrentTroop.mingling}");
                            }
                            #endif
                            
                            // 🔥 2026-03-12 修复：移除部队级别的 SmartSiege 触发，避免死循环
                            // 问题：每个部队进入 EnterMovementPipeline 时都触发 AssignSmartSiegePositions
                            //       导致已经在寻路的部队的 RealDestination 被重新分配，触发路径清空和重新寻路
                            // 解决：SmartSiege 只在军团级别执行（Legion.AIWithAuto），不在部队级别重复触发
                            // 注意：如果 RealDestination 为 Zero，TroopChangeRealDestination 会处理
                            
                            // AI 决策：去哪？(保留原有逻辑)
                            this.CurrentTroop.BeginExecutionTargetFromCurrentState();
                            this.TroopChangeRealDestination(this.CurrentTroop);

                            // 🔍 诊断：记录 TroopChangeRealDestination 后的状态
                            #if DEBUG
                            if (this.CurrentTroop.ManualControl)
                            {
                                System.Diagnostics.Debug.WriteLine($"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} TroopChangeRealDestination 后: RealDest={this.CurrentTroop.RealDestination}");
                            }
                            #endif

                            // 🔥 根本修复：设置 CurrentAIState，确保 UpdateMovementLogic 能正常执行
                            // 日期：2026-03-07
                            // 原因：玩家下达攻击指令后，CurrentAIState 保持 Idle，导致 UpdateMovementLogic 拒绝移动
                            // 解决：根据 Command 设置正确的 CurrentAIState
                            bool projectedByAuthority = false;
                            if (Session.GlobalVariables != null && Session.GlobalVariables.EnableAIAuthorityPhase1)
                            {
                                GameScenario scenario = Session.Current?.Scenario;
                                if (scenario != null)
                                {
                                    var authorityContext = scenario.EnsureAIAuthorityContext();
                                    projectedByAuthority = authorityContext.ApplyIntentProjection(scenario, this.CurrentTroop);
                                }
                            }

                            if (!projectedByAuthority && this.CurrentTroop.Command == TroopCommand.Enter)
                            {
                                this.CurrentTroop.CurrentAIState = TroopAIState.EnterCity;
                                #if DEBUG
                                if (this.CurrentTroop.ManualControl)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 设置 CurrentAIState=EnterCity");
                                }
                                #endif
                            }
                            else if (!projectedByAuthority && this.CurrentTroop.Command != TroopCommand.None)
                            {
                                // 所有非空指令（Move/Attack/AttackArch/AttackTroop/Stratagem）都设置为 Marching
                                this.CurrentTroop.CurrentAIState = TroopAIState.Marching;
                                #if DEBUG
                                if (this.CurrentTroop.ManualControl)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 设置 CurrentAIState=Marching (Command={this.CurrentTroop.Command})");
                                }
                                #endif
                            }

                            // 🔥 修复：传递 gameTime 参数，确保移动冷却能正常递减
                            this.CurrentTroop.UpdateMovementLogic(gameTime);
                            
                            // 🔥 根本修复：检查是否已在攻击范围内，应该执行攻击而不是移动
                            // 日期：2026-03-08
                            // 原因：部队已在攻击范围内，但 SmartSiege/GetOptimalAttackPosition 重新分配位置
                            //       导致部队开始移动，然后又被 UpdateMovement_Quick 停止
                            // 解决：如果已在攻击范围内，立即执行攻击，不进入移动流程
                            bool shouldAttackImmediately = false;
                            
                            // 检查1：攻击城池 - 检查是否已在攻击范围内
                            if (this.CurrentTroop.HasArchitectureAttackCommandContext())
                            {
                                var targetArchitecture = this.CurrentTroop.TargetArchitecture;
                                if (targetArchitecture == null || targetArchitecture.Endurance <= 0)
                                {
                                    this.CurrentTroop.SetCommand(TroopCommand.None);
                                    this.CurrentTroop.SelectedAttack = false;
                                    this.CurrentTroop.OperationDone = true;
                                    this.CurrentTroop = null;
                                    break;
                                }

                                int distToCity = int.MaxValue;
                                var cityArea = targetArchitecture.ArchitectureArea.Area;
                                
                                for (int i = 0; i < cityArea.Count; i++)
                                {
                                    Point cityTile = cityArea[i];
                                    int dx = Math.Abs(this.CurrentTroop.Position.X - cityTile.X);
                                    int dy = Math.Abs(this.CurrentTroop.Position.Y - cityTile.Y);
                                    int d = Math.Max(dx, dy);
                                    if (d < distToCity) distToCity = d;
                                }
                                
                                if (distToCity <= this.CurrentTroop.OffenceRadius)
                                {
                                    shouldAttackImmediately = true;
                                    #if DEBUG
                                    if (this.CurrentTroop.ManualControl)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 已在城池攻击范围内（距离={distToCity}），立即攻击");
                                    }
                                    #endif
                                }
                            }
                            // 检查2：攻击部队 - 使用战术评分系统决定是否立即攻击
                            else if (this.CurrentTroop.Command == TroopCommand.AttackTroop)
                            {
                                Troop targetTroop = this.CurrentTroop.TargetTroop;
                                if (targetTroop == null || targetTroop.Destroyed)
                                {
                                    this.CurrentTroop.SetCommand(TroopCommand.None);
                                    this.CurrentTroop.SelectedAttack = false;
                                    this.CurrentTroop.OperationDone = true;
                                    this.CurrentTroop = null;
                                    break;
                                }

                                // 🔥 2026-03-22 修复：复用现有的战术评分系统
                                // 问题：原逻辑只检查"是否在射程内"，导致远程部队在非最佳距离直接攻击
                                // 改进：使用 AITacticalPositioner.EvaluateRangedPosition 评估位置价值
                                // 参考：复用现有功能规范.md
                                
                                // 🔥 Anti-Band-Aid：不检查 TargetTroop 是否为 null
                                // 如果 Command == AttackTroop 但 TargetTroop == null，说明数据源有问题
                                // 让它崩溃，暴露问题：指令设置时必须验证目标有效性，或在目标被摧毁时清空指令
                                
                                int dx = Math.Abs(this.CurrentTroop.Position.X - targetTroop.Position.X);
                                int dy = Math.Abs(this.CurrentTroop.Position.Y - targetTroop.Position.Y);
                                int distToTroop = Math.Max(dx, dy);
                                
                                bool inRange = (distToTroop <= this.CurrentTroop.OffenceRadius);
                                
                                if (inRange)
                                {
                                    // 已经在射程内，使用战术评分系统决定是否需要调整位置
                                    Point currentTarget = this.CurrentTroop.GetExecutionTargetPosition();
                                    bool needsPositionAdjustment = (currentTarget != this.CurrentTroop.Position);
                                    
                                    if (needsPositionAdjustment)
                                    {
                                        // 评估当前位置和目标位置的战术价值
                                        float currentPosScore = AITacticalPositioner.EvaluateRangedPosition(
                                            this.CurrentTroop, this.CurrentTroop.Position, targetTroop);
                                        float targetPosScore = AITacticalPositioner.EvaluateRangedPosition(
                                            this.CurrentTroop, currentTarget, targetTroop);
                                        
                                        // 如果目标位置的评分显著高于当前位置，继续移动
                                        // 阈值：50分（避免为了微小的改进而频繁移动）
                                        if (targetPosScore > currentPosScore + 50.0f)
                                        {
                                            // 继续移动到目标位置
                                            #if DEBUG
                                            if (this.CurrentTroop.ManualControl)
                                            {
                                                System.Diagnostics.Debug.WriteLine(
                                                    $"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 决策：调整位置 " +
                                                    $"(当前评分={currentPosScore:F1}, 目标评分={targetPosScore:F1})");
                                            }
                                            #endif
                                        }
                                        else
                                        {
                                            // 当前位置已经足够好，立即攻击
                                            shouldAttackImmediately = true;
                                            #if DEBUG
                                            if (this.CurrentTroop.ManualControl)
                                            {
                                                System.Diagnostics.Debug.WriteLine(
                                                    $"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 决策：立即攻击 " +
                                                    $"(当前评分={currentPosScore:F1}, 目标评分={targetPosScore:F1})");
                                            }
                                            #endif
                                        }
                                    }
                                    else
                                    {
                                        // 没有目标位置，立即攻击
                                        shouldAttackImmediately = true;
                                        #if DEBUG
                                        if (this.CurrentTroop.ManualControl)
                                        {
                                            System.Diagnostics.Debug.WriteLine(
                                                $"[EnterMovementPipeline] {this.CurrentTroop.DisplayName} 已在部队攻击范围内（距离={distToTroop}），立即攻击");
                                        }
                                        #endif
                                    }
                                }
                            }
                            
                            // 如果已在攻击范围内，立即执行攻击
                            if (shouldAttackImmediately)
                            {
                                // 检查是否需要执行战斗动作
                                if (!this.CurrentTroop.HasToDoCombatAction && this.CurrentTroop.ToDoCombatAction())
                                {
                                    this.CurrentTroop.HasToDoCombatAction = true;
                                    this.CurrentTroop.DoCombatAction();
                                }
                                
                                // 标记部队为"已完成"
                                this.CurrentTroop.OperationDone = true;
                                this.CurrentTroop = null;
                            }
                            break;
                    }
                    
                    break;
                }
            }
        }

        /// <summary>
        /// 处理队列结束后的逻辑 (如重新填充未完成的部队)
        /// </summary>
        private void HandleQueueEnd()
        {
            this.queueEnded = true;
            TroopList list = new TroopList();
            foreach (Troop troop2 in base.GameObjects)
            {
                // 原逻辑：如果部队标记为 QueueEnded (可能意味着还没动完?) 则重新入队
                if (troop2.QueueEnded)
                {
                    list.Add(troop2);
                }
            }
            // 随机打乱重新入队
            foreach (Troop troop2 in list.GetRandomList())
            {
                this.troopQueue.Enqueue(troop2);
            }
        }

        // 保留原有的 AI 目标修正逻辑 (非常重要，否则 AI 会乱走)
        public void TroopChangeRealDestination(Troop troop, bool skipAuthorityProjection = false)
        {
            if (!skipAuthorityProjection &&
                Session.GlobalVariables != null &&
                Session.GlobalVariables.EnableAIAuthorityPhase1)
            {
                GameScenario scenario = Session.Current?.Scenario;
                if (scenario != null)
                {
                    var authorityContext = scenario.EnsureAIAuthorityContext();
                    if (!authorityContext.ValidateAndHandleIntentCheckpoint(scenario, troop, IntentCheckpointKind.BeforeProjection))
                    {
                        troop.OperationDone = true;
                        troop.Action = TroopAction.Stop;
                        troop.SetCommand(TroopCommand.None);
                        return;
                    }

                    if (!troop.ManualControl &&
                        !troop.HasArchitectureAttackCommandContext() &&
                        authorityContext.TryGetIntent(troop.ID, out _))
                    {
                        authorityContext.ApplyIntentProjection(scenario, troop);
                        return;
                    }
                }
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 进入目标判定 Command={troop.Command}, RealDestination={troop.RealDestination}, ManualControl={troop.ManualControl}, Position={troop.Position}, Destroyed={troop.Destroyed}");
            #endif
            
            // 🔥 根本修复：玩家战略指令分类处理
            // 日期：2026-03-07
            // 原因：不同的战略指令需要不同的处理方式
            // - AttackArch：需要 SmartSiege 系统分配攻击位置（在 EnterMovementPipeline 中处理）
            // - AttackTroop/Stratagem：需要 AI 自动设置 RealDestination
            // - Move：玩家已设置 RealDestination，保持不变
            
            // 攻击城池指令：不设置 RealDestination，等待 SmartSiege 系统处理
            if (troop.IsExecutionTargetLockedForCurrentTick())
            {
                return;
            }

            if (troop.HasArchitectureAttackCommandContext())
            {
                var targetArchitecture = troop.TargetArchitecture;
                if (targetArchitecture == null || targetArchitecture.Endurance <= 0)
                {
                    Troop targetTroop = troop.TargetTroop;
                    if (targetTroop != null && !targetTroop.Destroyed)
                    {
                        troop.SetCommand(TroopCommand.AttackTroop);
                    }
                    else
                    {
                        troop.SetCommand(TroopCommand.None);
                        troop.SelectedAttack = false;
                        return;
                    }
                }

                if (troop.HasArchitectureAttackCommandContext())
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 攻击城池指令，交给 SmartSiege 处理");
                    #endif
                    return;
                }
            }
            
            // 🔥 2026-03-24 新增：入城指令处理
            // 原因：CommandBufferScheduler 生成 EnterCommand，需要设置 RealDestination 为城池位置
            // 策略：使用 TargetArchitecture.Position（城池中心点）
            if (troop.Command == TroopCommand.Enter)
            {
                if (troop.TargetArchitecture != null)
                {
                    troop.RealDestination = troop.TargetArchitecture.Position;
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 入城指令，目标城池: {troop.TargetArchitecture.Name}, 位置: {troop.RealDestination}");
                    #endif
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 入城指令但 TargetArchitecture 为 null，清空指令");
                    #endif
                    troop.SetCommand(TroopCommand.None);
                    troop.OperationDone = true;
                }
                return;
            }
            
            // 移动指令：玩家已设置 RealDestination，保持不变
            if (troop.Command == TroopCommand.Move)
            {
                // 🔥 2026-03-24 修复：验证 RealDestination 是否有效
                // 问题：玩家下达移动指令后，RealDestination 可能未设置（仍为 -1, -1）
                // 原因：某些代码路径只设置了 Command，没有设置 RealDestination
                // 解决：检查 RealDestination 是否有效，无效则标记为完成
                bool hasInvalidDestination = (troop.RealDestination.X == -1 && troop.RealDestination.Y == -1) ||
                                             troop.RealDestination == Point.Zero ||
                                             troop.RealDestination == troop.Position;
                
                if (hasInvalidDestination)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] ⚠️ {troop.DisplayName} 移动指令但目标无效 (RealDest={troop.RealDestination})，标记为完成");
                    #endif
                    
                    // 标记为已完成，避免无限循环
                    troop.OperationDone = true;
                    troop.MovabilityLeft = -1;
                    troop.SetCommand(TroopCommand.None);
                    troop.CurrentAIState = TroopAIState.Idle;
                    troop.Action = TroopAction.Stop;
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 移动指令，保持玩家目标不变 (RealDest={troop.RealDestination})");
                    #endif
                }
                return;
            }
            
            // 攻击部队指令：AI 自动设置 RealDestination
            // 🔥 数据验证：检查 TargetTroop 是否有效，避免时序问题（目标被消灭但指令未清空）
            if (troop.Command == TroopCommand.AttackTroop)
            {
                Troop targetTroop = troop.TargetTroop;
                // 修复日期：2026-03-08
                // 问题：目标部队被消灭后，TargetTroop 被设为 null，但 Command 可能还未清空，导致 NullReferenceException
                // 解决：在调用 GetOptimalAttackPosition 前检查目标有效性，无效则清空指令
                if (targetTroop == null || targetTroop.Destroyed)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 攻击目标无效，清空指令");
                    #endif
                    troop.SetCommand(TroopCommand.None);
                    troop.SelectedAttack = false;
                    return;
                }
                
                // 🎯 远程战术：远程部队保持在最大射程，近战部队直接接近
                troop.RealDestination = troop.GetOptimalAttackPosition(targetTroop);
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 攻击部队指令，目标位置: {troop.RealDestination}");
                #endif
                return;
            }
            
            // 计略指令：检查是否在计略范围内
            // 🔥 修复：计略不应该使用 GetOptimalAttackPosition（攻击用的）
            // 日期：2026-03-07
            // 原因：GetOptimalAttackPosition 会让部队移动到攻击位置，导致计略释放时瞬移
            // 解决：检查是否在计略范围内，如果在范围内则原地释放，不在范围内才移动
            if (troop.Command == TroopCommand.Stratagem)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] === 计略指令开始 ===");
                System.Diagnostics.Debug.WriteLine($"  部队: {troop.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"  当前位置: {troop.Position}");
                System.Diagnostics.Debug.WriteLine($"  TargetTroop: {troop.TargetTroop?.DisplayName ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  CurrentStratagem: {troop.CurrentStratagem?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  SelfCastPosition: {troop.SelfCastPosition}");
                #endif
                
                if (troop.TargetTroop != null && troop.CanStratagem(troop.TargetTroop))
                {
                    // 已在计略范围内，原地释放
                    troop.RealDestination = troop.Position;
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  决策: 已在范围内，原地释放");
                    System.Diagnostics.Debug.WriteLine($"  RealDestination: {troop.RealDestination}");
                    #endif
                }
                else if (troop.TargetTroop != null)
                {
                    // 🔥 2026-03-11 修复：不在范围内，使用 GetOptimalAttackPosition 计算射程边缘的可通行位置
                    // 之前直接使用 troop.TargetTroop.Position（敌军位置，不可通行），会导致 A* 寻路失败
                    troop.RealDestination = troop.GetOptimalAttackPosition(troop.TargetTroop);
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  决策: 不在范围内，使用 GetOptimalAttackPosition 移动到射程边缘");
                    System.Diagnostics.Debug.WriteLine($"  目标部队位置: {troop.TargetTroop.Position}");
                    System.Diagnostics.Debug.WriteLine($"  计算后 RealDestination: {troop.RealDestination}");
                    #endif
                }
                else
                {
                    // 无目标（可能是自释放计略），原地释放
                    troop.RealDestination = troop.Position;
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  决策: 无目标（自释放），原地释放");
                    System.Diagnostics.Debug.WriteLine($"  RealDestination: {troop.RealDestination}");
                    #endif
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] === 计略指令结束 ===");
                #endif
                return;
            }
            
            #if DEBUG
            if (troop.Command == TroopCommand.None)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 无显式指令，执行 AI 目标判定");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} Command={troop.Command}，执行原有 AI 逻辑");
            }
            #endif
            
            // 🔥 根本修复：Command=None 且 RealDestination 无效时，标记部队为"已完成"
            // 日期：2026-03-09
            // 问题：张宝队 RealDestination=(-1,-1)，Command=None，导致无限循环
            // 根因：部队没有有效目标，但 TroopStateMachineRouter 返回 EnterMovementPipeline
            //       导致部队被反复取出，但 UpdateMovementLogic 检测到无目标后立即返回
            //       CurrentQueueTroopMove 检测到 !IsAnimationPlaying，设置 OperationDone=true，CurrentTroop=null
            //       下一轮循环又从队列取出，陷入死循环
            // 解决：在 TroopChangeRealDestination 中检测"无效目标"状态，直接标记为完成
            //       避免进入 UpdateMovementLogic → 立即返回 → 标记完成 的无意义循环
            if (troop.Command == TroopCommand.None)
            {
                bool hasInvalidDestination = (troop.RealDestination.X == -1 && troop.RealDestination.Y == -1) ||
                                             troop.RealDestination == Point.Zero ||
                                             troop.RealDestination == troop.Position;
                
                if (hasInvalidDestination)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[TroopChangeRealDestination] {troop.DisplayName} 无有效目标，本回合正常结束");
                    #endif
                    
                    // 标记为已完成，避免无限循环
                    troop.OperationDone = true;
                    troop.MovabilityLeft = -1;
                    
                    // 重置状态为 Idle
                    troop.CurrentAIState = TroopAIState.Idle;
                    troop.Action = TroopAction.Stop;
                    
                    return;
                }
            }
            
            if (troop.Command == TroopCommand.Attack)
            {
                if (troop.TargetTroop != null)
                {
                    if (!(troop.Command == TroopCommand.Attack && troop.CanAttack(troop.TargetTroop)))
                    {
                        // 🔥 2026-03-11 修复：使用 GetOptimalAttackPosition 而非直接使用敌军位置
                        troop.RealDestination = troop.GetOptimalAttackPosition(troop.TargetTroop);
                    }
                    else
                    {
                        troop.RealDestination = troop.Position;
                    }
                }
                else if (troop.TargetArchitecture != null && troop.Command == TroopCommand.Attack && !troop.CanAttack(troop.TargetArchitecture))
                {
                    troop.RealDestination = troop.TargetArchitecture.Position;
                }

                else if (troop.TargetArchitecture != null && troop.TargetArchitecture.Endurance <= 0)
                {
                    troop.RealDestination = troop.TargetArchitecture.Position;
                }
                else troop.RealDestination = troop.Position;
            }
            else if (troop.TargetTroop != null && troop.Will.ToString() == "行军" && troop.Command != TroopCommand.Move)
            {
                if (troop.BaseAttackEveryAround || troop.AttackEveryAround)
                {//修复雷霆战法攻击方式
                    foreach (Troop troop2 in troop.GetAllOtherTroopsInView())
                    {
                        if ((!troop.AttackedTroopList.HasGameObject(troop2)) && troop.CanAttack(troop2) && (!troop.TroopNoAccidentalInjury || !troop.IsFriendly(troop2.BelongedFaction)))
                        {
                            troop.AttackTroop(troop2);
                            troop.ApplyDamageList(); // ★★★ 修复：立即应用伤害 ★★★
                        }
                    }
                }
            }
        }



        public void FinalizeQueue()
        {
            // 🔥 确保所有队列都被清空，防止残余
            if (this.troopQueue.Count > 0)
            {
                // System.Diagnostics.Debug.WriteLine($"[FinalizeQueue] 警告：还有{this.troopQueue.Count}个部队未处理完成，强制清理");
                this.troopQueue.Clear();
            }

            if (this.CurrentQueue.Count > 0)
            {
                this.CurrentQueue.Clear();
            }

            if (this.CurrentTroop != null)
            {
                this.CurrentTroop = null;
            }

            foreach (Troop troop in base.GameObjects)
            {
                troop.FinalizeInQueue();
            }
        }



        /// <summary>
        /// [已废弃] 全局动画帧推进方法
        /// 新系统中，每个部队通过 UpdateMovementLogic 自行推进动画帧
        /// 保留此方法以兼容旧代码，但不执行任何操作
        /// 日期：2026-02-28 动画阻塞重构
        /// </summary>
        [Obsolete("已废弃：部队通过 UpdateMovementLogic 自行推进动画")]
        public void StepAnimationIndex(int steps)
        {
            // 🔥 新系统：部队自行推进动画，不需要全局驱动
            // 保留空方法以避免编译错误
        }

        public bool CurrentQueueEmpty
        {
            get
            {
                return ((this.CurrentQueue.Count == 0) && (this.CurrentTroop == null));
            }
        }

        public bool QueueEmpty
        {
            get
            {
                return (this.troopQueue.Count == 0);
            }
        }

        public bool TotallyEmpty
        {
            get
            {
                return (this.QueueEmpty && this.CurrentQueueEmpty);
            }
        }

        /// <summary>
        /// 根据ID获取部队对象
        /// </summary>
        /// <param name="troopId">部队ID</param>
        /// <returns>找到的部队对象，如果不存在则返回null</returns>
        public Troop GetTroop(int troopId)
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Troop in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Troop troop = base.GameObjects[i] as Troop;
                if (troop == null)
                {
                    throw new InvalidOperationException($"TroopListWithQueue 中存在非 Troop 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                
                if (troop.ID == troopId)
                {
                    return troop;
                }
            }
            return null;
        }
    }
}
