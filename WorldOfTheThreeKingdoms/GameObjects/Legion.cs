using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.FactionDetail;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using GameManager;

namespace GameObjects
{
    [DataContract]

    public partial class Legion : GameObject
    {

        public Faction BelongedFaction;

        [DataMember]
        [JsonInclude]
        public int BelongedFactionID { get; set; } = -1;

        [DataMember]
        public int CoreTroopString { get; set; } = -1;

        public Troop CoreTroop;
        
        public Person Leader;
        
        [DataMember]
        [JsonInclude]
        public int LeaderID { get; set; } = -1;
        
        [DataMember]
        public Point? InformationDestination = null;
        
        // 🔥 重构：军团类型字段
        // 日期：2026-03-09
        // 改为只读字段，配合init访问器，禁止运行时修改
        private LegionKind kind;
        
        // 🔥 新增：军团任务
        // 日期：2026-03-09
        // 将类型和任务分离，同一军团可以执行不同任务
        private LegionMission mission = LegionMission.None;
        
        // 🔥 新增：任务目标建筑ID
        // 日期：2026-03-09
        // 用于序列化，替代WillArchitecture的直接引用
        [DataMember]
        [JsonInclude]
        public int TargetArchitectureID { get; set; } = -1;
        
        // 🔥 新增：任务目标建筑
        // 日期：2026-03-09
        // 运行时引用，由TargetArchitectureID链接
        public Architecture Target { get; set; }

        [DataMember]
        public int PreferredRoutewayString { get; set; } = -1;

        public Routeway PreferredRouteway;

        [DataMember]
        public int StartArchitectureString { get; set; } = -1;

        public Architecture StartArchitecture;

        [DataMember]
        public List<SupplyingRoutewayPack> SupplyingRouteways = new List<SupplyingRoutewayPack>();

        [DataMember]
        public List<Point> TakenPositions = new List<Point>();

        // Removed obsolete TroopsString - use TroopIDs instead

        [DataMember]
        [JsonInclude]
        public List<int> TroopIDs { get; set; } = new List<int>();

        public TroopList Troops = new TroopList();

        [DataMember]
        public int WillArchitectureString { get; set; } = -1;

        public Architecture WillArchitecture;

        public void Init()
        {
            Troops = new TroopList();
        }

        public void AddRoutewayCredit(Routeway routeway, int credit)
        {
            foreach (SupplyingRoutewayPack pack in this.SupplyingRouteways)
            {
                if (pack.SupplyingRouteway == routeway)
                {
                    pack.Credit += credit;
                    return;
                }
            }
            SupplyingRoutewayPack item = new SupplyingRoutewayPack();
            item.SupplyingRouteway = routeway;
            item.Credit = credit;
            this.SupplyingRouteways.Add(item);
        }

        /// <summary>
        /// 添加部队到军团 - 确保双向关系
        /// </summary>
        public void AddTroop(Troop troop)
        {
            if (troop == null) return;
            
            // 🔥 关键修复：检查势力归属
            // 日期：2026-03-08
            // 原因：敌军部队被错误地加入玩家军团，导致军团系统完全混乱
            // 解决：拒绝添加不同势力的部队
            // 
            // ⚠️ 数据完整性检查：如果军团或部队没有势力，让它崩溃暴露问题
            // 这是严重的数据错误，不应该被掩盖
            if (this.BelongedFaction != troop.BelongedFaction)
            {
                System.Diagnostics.Debug.WriteLine($"[Legion.AddTroop] ❌ 拒绝：{troop.DisplayName}(势力:{troop.BelongedFaction?.Name ?? "无"}) 不能加入军团{this.Name}(势力:{this.BelongedFaction?.Name ?? "无"})");
                return;
            }
            
            // 检查是否已在本军团
            if (this.Troops.HasGameObject(troop.ID))
            {
                troop.BelongedLegion = this; // 确保双向一致
                return;
            }
            
            // 如果部队已属于其他军团，先移除
            if (troop.BelongedLegion != null && troop.BelongedLegion != this)
            {
                System.Diagnostics.Debug.WriteLine($"[Legion.AddTroop] {troop.DisplayName} 从旧军团{troop.BelongedLegion.Name}移除");
                troop.BelongedLegion.Troops.Remove(troop);
            }
            
            // 建立双向关系
            troop.BelongedLegion = this;
            this.Troops.Add(troop);
            
            // 🔥 验证：确保双向关系建立成功
            if (troop.BelongedLegion != this)
            {
                System.Diagnostics.Debug.WriteLine($"[Legion.AddTroop] ⚠️ 警告：{troop.DisplayName} BelongedLegion 设置失败！期望:{this.Name}, 实际:{troop.BelongedLegion?.Name}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[Legion.AddTroop] 军团{this.Name}({this.Kind}) 添加部队 {troop.DisplayName}(ID:{troop.ID})，当前部队数:{this.Troops.Count}, 验证BelongedLegion={troop.BelongedLegion?.Name}");
            
            // 🔥 自动分配军团角色
            // 当部队数量达到2支或以上时，重新分配整个军团的角色
            if (this.Troops.Count >= 2)
            {
                try
                {
                    WorldOfTheThreeKingdoms.GameGlobal.AIRoleSelector.UpdateLegionRoles(this);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Legion.AddTroop] 角色分配失败: {ex.Message}");
                }
            }
        }

        public void AI()
        {
            System.Diagnostics.Debug.WriteLine($"[Legion.AI] === 军团{this.Name}({this.Kind}) AI开始执行，部队数:{this.Troops.Count} ===");
            this.CallRouteway();
            this.ResetCoreTroop();
            this.TroopAI();
            System.Diagnostics.Debug.WriteLine($"[Legion.AI] === 军团{this.Name}({this.Kind}) AI执行完毕 ===");
        }

        public void AIWithAuto()
        {
            this.ResetCoreTroop();
            this.TakenPositions.Clear();
            
            System.Diagnostics.Debug.WriteLine($"[Legion.AIWithAuto] 军团{this.Name}({this.Kind}) 开始执行，部队数:{this.Troops.Count}");
            
            // 🔥 修复：玩家战略指令（攻击城池/部队/技能）也执行 AI
            // 日期：2026-03-07
            // 原因：玩家只选择目标（战略目标），具体如何接近和攻击/释放技能应该由 AI 决定
            // 解决：检查是否有部队下达了战略指令，如果有则执行 AI
            bool hasStrategicCommand = false;
            foreach (var obj in this.Troops.GetList())
            {
                if (obj is Troop t && (t.Command == TroopCommand.AttackArch || 
                                       t.Command == TroopCommand.AttackTroop ||
                                       t.Command == TroopCommand.Attack ||
                                       t.Command == TroopCommand.Stratagem))
                {
                    hasStrategicCommand = true;
                    System.Diagnostics.Debug.WriteLine($"[Legion.AIWithAuto] 检测到战略指令: {t.DisplayName} Command={t.Command}");
                    break;
                }
            }
            
            // 如果有战略攻击指令，执行 SmartSiege 分配攻击位置
            if (hasStrategicCommand && this.WillArchitecture != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Legion.AIWithAuto] 执行 SmartSiege 分配攻击位置，目标={this.WillArchitecture.Name}");
                this.AssignSmartSiegePositions();
            }
            
            // 🔥 FIX: 安全遍历，使用 C# 12 模式匹配
            foreach (var obj in this.Troops.GetList())
            {
                if (obj is not Troop troop) continue;
                
                // 🔥 修复：玩家控制的部队也需要执行移动逻辑
                // 条件：Auto=true（AI控制）或 SelectedMove=true（玩家下达了移动命令）
                bool shouldExecuteAI = troop.Auto || 
                                      (troop.StartingArchitecture.BelongedSection != null && 
                                       troop.StartingArchitecture.BelongedSection.AIDetail.AutoRun);
                
                bool shouldExecutePlayerMove = troop.SelectedMove && 
                                               troop.RealDestination.X >= 0 && 
                                               troop.RealDestination.Y >= 0;
                
                // 🔥 新增：玩家战略指令（攻击城池/部队/技能）也需要执行 AI
                // 日期：2026-03-07
                bool hasPlayerStrategicCommand = (troop.Command == TroopCommand.AttackArch || 
                                                   troop.Command == TroopCommand.AttackTroop ||
                                                   troop.Command == TroopCommand.Attack ||
                                                   troop.Command == TroopCommand.Stratagem);
                
                if (shouldExecuteAI || shouldExecutePlayerMove || hasPlayerStrategicCommand)
                {
                    System.Diagnostics.Debug.WriteLine($"[Legion.AIWithAuto] 执行部队AI: {troop.DisplayName} Command={troop.Command}");
                    troop.AI();
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Legion.AIWithAuto] 军团{this.Name} 执行完毕");
        }

        public void CallInformation()
        {
            if (!this.InformationDestination.HasValue)
            {
                PersonList list = new PersonList();
                foreach (LinkNode node in this.WillArchitecture.AIAllLinkNodes.Values)
                {
                    if ((((node.A.BelongedFaction == this.BelongedFaction) && node.A.BelongedSection != null && 
                        node.A.BelongedSection.AIDetail.AllowInvestigateTactics) && node.A.InformationAvail()) &&
                        (node.A.RecentlyAttacked <= 0))
                    {
                        foreach (Person person in node.A.MovablePersons)
                        {
                            if (person.LocationArchitecture != null)
                            {
                                list.Add(person);
                            }
                        }
                        if (list.Count >= 10)
                        {
                            break;
                        }
                    }
                }
                if (list.Count > 0)
                {
                    Person person = list[GameObject.Random(list.Count)] as Person;
                    InformationKindList availList = Session.Current.Scenario.GameCommonData.AllInformationKinds.GetAvailList(person.LocationArchitecture);
                    if (availList.Count > 0)
                    {
                        if (availList.Count > 1)
                        {
                            if (this.WillArchitecture.BelongedFaction == null)
                            {
                                availList.PropertyName = "CostFund";
                                availList.SmallToBig = true;
                            }
                            else
                            {
                                availList.PropertyName = "FightingWeighing";
                            }
                            availList.IsNumber = true;
                            availList.ReSort();
                        }
                        this.SetInformationPosition();
                        if (this.InformationDestination.HasValue)
                        {
                            var selectedObject = availList[GameObject.Random(availList.Count / 2)];
                            var selectedKind = (selectedObject is InformationKind ? (InformationKind)selectedObject : null);
                            
                            if (selectedKind != null)
                            {
                                person.CurrentInformationKind = selectedKind;
                                person.GoForInformation(this.InformationDestination.Value);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Legion.CallInformation] 类型转换失败: 对象类型为 {selectedObject?.GetType()?.Name ?? "null"}");
                            }
                        }
                    }
                }
            }
        }

        private void CallRouteway()
        {
            if (!Session.GlobalVariables.LiangdaoXitong) return;
            if (this.WillArchitecture != null)
            {
                int foodCostPerDay;
                LinkNode node2;
                Routeway routeway;
                
                // 🔥 重构：使用Mission判断
                if (this.Mission == LegionMission.Attack)
                {
                    if ((this.WillArchitecture.BelongedFaction != this.BelongedFaction) || (this.WillArchitecture.RecentlyAttacked > 0))
                    {
                        foodCostPerDay = this.FoodCostPerDay;
                        if (((this.PreferredRouteway == null) || (!this.PreferredRouteway.Building && (this.PreferredRouteway.LastActivePointIndex < 0))) || ((this.PreferredRouteway.LastPoint != null) && !this.PreferredRouteway.IsEnough(this.PreferredRouteway.LastPoint.ConsumptionRate, foodCostPerDay * 12)))
                        {
                            foreach (LinkNode node in this.WillArchitecture.AIAllLinkNodes.Values)
                            {
                                if (node.Level > 2)
                                {
                                    break;
                                }
                                if (((node.A.BelongedFaction == this.BelongedFaction) && (node.A.RecentlyAttacked <= 0)) && (node.A.Food >= (foodCostPerDay * 15)))
                                {
                                    node2 = null;
                                    if (node.A.AIAllLinkNodes.TryGetValue(this.WillArchitecture.ID, out node2))
                                    {
                                        routeway = node.A.GetRouteway(node2, true);
                                        if (((routeway != null) && (routeway.LastPoint != null) && (node.A.Fund >= (routeway.LastPoint.BuildFundCost * (2 + ((this.WillArchitecture.AreaCount >= 4) ? 1 : 0))))) && routeway.ByPassHostileArchitecture == null)
                                        {
                                            routeway.Building = true;
                                            this.PreferredRouteway = routeway;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (this.Mission == LegionMission.Defend && (this.WillArchitecture.BelongedFaction == this.BelongedFaction))
                {
                    foodCostPerDay = this.FoodCostPerDay;
                    if ((this.WillArchitecture.Food < (foodCostPerDay * 12)) && (((this.PreferredRouteway == null) || (!this.PreferredRouteway.Building && (this.PreferredRouteway.LastActivePointIndex < 0))) || ((this.PreferredRouteway.LastPoint != null) && !this.PreferredRouteway.IsEnough(this.PreferredRouteway.LastPoint.ConsumptionRate, foodCostPerDay * 12))))
                    {
                        foreach (LinkNode node in this.WillArchitecture.AIAllLinkNodes.Values)
                        {
                            if (node.Level > 2)
                            {
                                break;
                            }
                            if (((node.A.BelongedFaction == this.BelongedFaction) && (node.A.RecentlyAttacked <= 0)) && (node.A.Food >= (foodCostPerDay * 15)))
                            {
                                node2 = null;
                                if (node.A.AIAllLinkNodes.TryGetValue(this.WillArchitecture.ID, out node2))
                                {
                                    routeway = node.A.GetRouteway(node2, true);
                                    if ((routeway != null) && (routeway.LastPoint != null) && (node.A.Fund >= (routeway.LastPoint.BuildFundCost * 2)))
                                    {
                                        routeway.Building = true;
                                        this.PreferredRouteway = routeway;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DayEvent()
        {
            this.SupplyingRouteways.Clear();

            // 2. 修复：安全遍历 Troops 列表
            // 原理：不使用 foreach (Troop t in ...) 的隐式强转，改用显式安全转换
            var rawTroopList = this.Troops.GetList();
            for (int i = 0; i < rawTroopList.Count; i++)
            {
                // 使用 'as' 尝试转换，如果对象不是 Troop，则返回 null，不会崩溃
                Troop troop = rawTroopList[i] as Troop; 
                
                if (troop != null)
                {
                    troop.DayEvent();
                }
            }

            Routeway maxCreditRouteway = this.GetMaxCreditRouteway();
            if (maxCreditRouteway != null)
            {
                this.PreferredRouteway = maxCreditRouteway;
            }
        }

        public void Disband()
        {
            System.Diagnostics.Debug.WriteLine($"[Legion.Disband] 军团{this.Name}({this.Kind})解散，部队数:{this.Troops.Count}");
            
            // 🔥 修复：先清理部队的军团引用，避免部队访问已解散的军团
            // 必须在清空 WillArchitecture 之前执行，否则部队会读取到 null
            // 🔥 数据验证：只清理真正属于该军团的部队，避免误伤其他军团的部队
            // 🔥 2026-03-07 修复：军团解散时，部队必须重新分配到新军团，不能设置为 null
            // 日期：2026-03-07
            foreach (Troop t in this.Troops.GetList())
            {
                // 🔥 Anti-Band-Aid：验证部队确实属于该军团
                // 如果不属于，说明数据损坏，让它崩溃暴露问题
                if (t.BelongedLegion != this)
                {
                    throw new InvalidOperationException(
                        $"[Legion.Disband] ❌ 数据错误：部队 {t.DisplayName}(ID:{t.ID}) 的 BelongedLegion={t.BelongedLegion?.Name ?? "null"} " +
                        $"不等于当前军团 {this.Name}(ID:{this.ID})");
                }
                
                // 🔥 Anti-Band-Aid：验证部队必须有势力
                if (t.BelongedFaction == null)
                {
                    throw new InvalidOperationException(
                        $"[Legion.Disband] ❌ 数据错误：部队 {t.DisplayName}(ID:{t.ID}) 的 BelongedFaction 为 null");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Legion.Disband] 清理部队 {t.DisplayName} 的军团引用");
                
                // 🔥 根本修复：根据解散的军团任务决定部队的下一步状态
                // - 进攻任务解散 → 撤退军团（回到己方建筑）
                // - 防守任务解散 → 撤退军团（撤退到安全地点）
                // - 撤退任务解散 → 不需要军团（已到达目的地）
                
                if (this.Mission == LegionMission.Retreat)
                {
                    // 🔥 根本修复：撤退军团解散时，部队必须入城
                    // 日期：2026-03-22
                    // 问题：只清空军团引用，部队仍在城外，没有入城
                    // 解决：调用 Enter() 让部队入城，或者如果不在城市范围内则保持目标继续移动
                    
                    // 清空军团引用
                    t.BelongedLegion = null;
                    
                    // 确定目标城市：撤退目标 > 出发地 > 所属建筑
                    Architecture targetArch = this.WillArchitecture ?? t.StartingArchitecture ?? t.BelongedArchitecture;
                    
                    // 🔥 ANTI-BAND-AID：Fail Fast
                    // 日期：2026-03-22
                    // 原因：撤退军团的部队必须有目标城市，如果没有说明数据严重损坏
                    // 解决：抛出异常，暴露问题
                    if (targetArch == null)
                    {
                        throw new InvalidOperationException(
                            $"[Legion.Disband] ❌ 数据错误：撤退军团 {this.Name} 的部队 {t.DisplayName}(ID:{t.ID}) " +
                            $"没有任何目标城市（WillArchitecture={this.WillArchitecture?.Name ?? "null"}, " +
                            $"StartingArchitecture={t.StartingArchitecture?.Name ?? "null"}, " +
                            $"BelongedArchitecture={t.BelongedArchitecture?.Name ?? "null"}）");
                    }
                    
                    // 检查部队是否在目标城市范围内
                    bool isInArchitectureArea = targetArch.ArchitectureArea.HasPoint(t.Position);
                    
                    if (isInArchitectureArea)
                    {
                        // 部队在城市范围内，直接入城
                        System.Diagnostics.Debug.WriteLine(
                            $"[Legion.Disband] 部队 {t.DisplayName} 撤退完成，入城: {targetArch.Name}");
                        t.Enter(targetArch);
                        // 注意：Enter() 会销毁部队对象，后续的清理逻辑不会执行
                        continue;  // 🔥 跳过后续的清理逻辑
                    }
                    else
                    {
                        // 部队不在城市范围内，保持目标让它继续移动
                        // 🔥 关键：不清空 WillArchitecture 和 RealDestination
                        // 使用 continue 跳过后续的清理逻辑
                        System.Diagnostics.Debug.WriteLine(
                            $"[Legion.Disband] 部队 {t.DisplayName} 撤退未完成，保持目标 {targetArch.Name}，继续移动");
                        
                        // 确保目标设置正确
                        if (t.WillArchitecture != targetArch)
                        {
                            t.WillArchitecture = targetArch;
                        }
                        if (t.RealDestination.X < 0 || t.RealDestination.Y < 0)
                        {
                            t.RealDestination = Session.Current.Scenario.GetClosestPoint(
                                targetArch.GetTroopEnterableArea(t),
                                t.Position);
                        }
                        t.CurrentAIState = TroopAIState.EnterCity;
                        
                        // 🔥 关键：跳过后续的清理逻辑（第 487-500 行）
                        continue;
                    }
                }
                else
                {
                    // 进攻/防守军团解散：部队需要撤退
                    // 优先级：所属建筑 → 出发建筑 → 首都
                    Architecture targetArch = t.BelongedArchitecture ?? t.StartingArchitecture;
                    
                    if (targetArch != null)
                    {
                        // 创建或加入撤退军团
                        Legion retreatLegion = t.BelongedFaction.GetOrCreateLegion(targetArch, LegionKind.AI, LegionMission.Retreat);
                        retreatLegion.AddTroop(t);
                        t.IsRetreating = true;
                        System.Diagnostics.Debug.WriteLine($"[Legion.Disband] 部队 {t.DisplayName} 加入撤退军团: {retreatLegion.Name} (目标: {targetArch.Name}, 原军团任务: {this.Mission})");
                    }
                    else
                    {
                        // 部队没有任何建筑引用，使用势力首都作为兜底
                        Architecture capital = t.BelongedFaction.Capital;
                        if (capital != null)
                        {
                            Legion retreatLegion = t.BelongedFaction.GetOrCreateLegion(capital, LegionKind.AI, LegionMission.Retreat);
                            retreatLegion.AddTroop(t);
                            t.IsRetreating = true;
                            System.Diagnostics.Debug.WriteLine($"[Legion.Disband] ⚠️ 部队 {t.DisplayName} 没有任何建筑引用，撤退到首都 {capital.Name}");
                        }
                        else
                        {
                            // 势力没有首都，说明势力被灭国，部队应该被销毁
                            throw new InvalidOperationException(
                                $"[Legion.Disband] ❌ 数据错误：部队 {t.DisplayName}(ID:{t.ID}) 的势力 {t.BelongedFaction.Name} 没有首都，可能已被灭国");
                        }
                    }
                }
                
                // 🔥 修复：清理军团任务相关状态，避免部队保留旧军团的目标
                // 日期：2026-02-26
                // 原因：军团解散后，部队保留旧的 RealDestination，导致玩家无法控制
                t.RealDestination = new(-1, -1);
                t.WillArchitecture = null;
                
                // 🔥 根本修复：清空 Command，避免状态不一致
                // 日期：2026-03-09
                // 问题：军团解散后，RealDestination 被重置为 (-1,-1)，但 Command 没有清空
                //       导致状态机判断错误，进入无限循环
                // 解决：同时清空 Command，确保状态一致性
                t.SetCommand(TroopCommand.None);
                
                // 🔥 修复：如果部队还在野外（不在建筑内），切换到 Idle 状态，让它自己决定下一步
                if (!t.Destroyed && !t.IsInArchitecture)
                {
                    t.CurrentAIState = TroopAIState.Idle;
                    System.Diagnostics.Debug.WriteLine($"[Legion.Disband] 部队 {t.DisplayName} 切换到 Idle 状态，清理目标");
                }
            }
            
            this.Troops.Clear();
            
            // 清空军团属性
            this.PreferredRouteway = null;
            this.StartArchitecture = null;
            this.WillArchitecture = null;
            this.CoreTroop = null;
            
            // 清理建筑的防守军团引用
            if (this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture.DefensiveLegion == this)
                    {
                        architecture.DefensiveLegion = null;
                    }
                }
                this.BelongedFaction.RemoveLegion(this);
            }
        }

        /// <summary>
        /// 判断军团任务是否完成
        /// </summary>
        public bool IsComplete
        {
            get
            {
                // 无部队 = 完成
                if (this.Troops.Count == 0) return true;
                this.EnsureOperationalTarget();

                // 玩家军团不参与自动完成/解散逻辑，仅同步任务枚举用于委任给AI时的行为判断。
                // Player legions do not auto-complete/disband. Mission is still synchronized for AI delegation.
                if (this.Kind == LegionKind.Player)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[Legion.IsComplete] 玩家军团 {this.Name} 不进行自动完成判定，当前Mission:{this.Mission}, Target:{this.WillArchitecture?.Name ?? "null"}");
                    #endif
                    return false;
                }

                // 🔥 重构：使用Mission判断
                switch (this.Mission)
                {
                    case LegionMission.Attack:
                        // 目标已被我方占领 = 攻击完成
                        if (this.WillArchitecture == null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[Legion.IsComplete] WARNING: 攻击军团 {this.Name} WillArchitecture 为 null，跳过自动解散。");
                            return false;
                        }
                        return this.WillArchitecture.BelongedFaction == this.BelongedFaction;

                    case LegionMission.Defend:
                        // 🔥 修复：防守军团完成判定逻辑
                        // 日期：2026-03-22
                        // 问题：只检查视野内的敌军，导致敌军刚离开视野就解散军团
                        // 解决：添加冷却时间和前线城市判定
                        
                        if (this.WillArchitecture == null)
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[Legion.IsComplete] 军团{this.Name} WillArchitecture为null，返回false");
                            #endif
                            return false;
                        }
                        
                        // 🔥 关键：只调用一次 HasHostileTroopsInView()，避免重复调用导致结果不一致
                        bool hasHostile = this.WillArchitecture.HasHostileTroopsInView();
                        
                        #if DEBUG
                        // 🔥 诊断日志：只记录阳翟(ID:203)的防守军团
                        // 日期：2026-03-22
                        if (false /* this.WillArchitecture.ID == 203 */)
                        {
                            int recentlyAttacked = this.WillArchitecture.RecentlyAttacked;
                            
                            System.Diagnostics.Debug.WriteLine($"[Legion.IsComplete] 防守军团 {this.Name} 目标:{this.WillArchitecture.Name}");
                            System.Diagnostics.Debug.WriteLine($"  - HasHostileTroopsInView: {hasHostile}");
                            System.Diagnostics.Debug.WriteLine($"  - RecentlyAttacked: {recentlyAttacked}");
                            System.Diagnostics.Debug.WriteLine($"  - FrontLine: {this.WillArchitecture.FrontLine}, HostileLine: {this.WillArchitecture.HostileLine}");
                        }
                        #endif
                        
                        // 1. 视野内有敌军 = 未完成
                        if (hasHostile)
                        {
                            #if DEBUG
                            if (false /* this.WillArchitecture.ID == 203 */)
                            {
                                System.Diagnostics.Debug.WriteLine($"  → 判定结果: 未完成（视野内有敌军）");
                            }
                            #endif
                            return false;
                        }
                        
                        // 2. 最近被攻击 = 未完成（冷却时间）
                        if (this.WillArchitecture.RecentlyAttacked > 0)
                        {
                            #if DEBUG
                            if (false /* this.WillArchitecture.ID == 203 */)
                            {
                                System.Diagnostics.Debug.WriteLine($"  → 判定结果: 未完成（最近被攻击）");
                            }
                            #endif
                            return false;
                        }
                        
                        // 3. 前线城市需要更长的冷却时间
                        // 原因：敌军可能在视野外集结，随时可能再次进攻
                        if ((this.WillArchitecture.FrontLine || this.WillArchitecture.HostileLine) && 
                            this.WillArchitecture.RecentlyAttacked > -5)
                        {
                            #if DEBUG
                            if (false /* this.WillArchitecture.ID == 203 */)
                            {
                                System.Diagnostics.Debug.WriteLine($"  → 判定结果: 未完成（前线城市冷却时间，RecentlyAttacked={this.WillArchitecture.RecentlyAttacked}）");
                            }
                            #endif
                            return false;
                        }
                        
                        #if DEBUG
                        if (false /* this.WillArchitecture.ID == 203 */)
                        {
                            System.Diagnostics.Debug.WriteLine($"  → 判定结果: 已完成（所有条件都不满足）");
                        }
                        #endif
                        return true;

                    case LegionMission.Retreat:
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[Legion.IsComplete] 撤退军团 {this.Name} 检查完成状态，部队数:{this.Troops.Count}");
                        #endif
                        
                        // 所有部队都进城了或都被摧毁 = 撤退完成
                        foreach (Troop t in this.Troops)
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"  - 部队: {t.DisplayName}(ID:{t.ID}) " +
                                $"Destroyed:{t.Destroyed} IsRetreating:{t.IsRetreating} " +
                                $"Position:{t.Position} WillArch:{t.WillArchitecture?.Name ?? "null"}");
                            #endif
                            
                            if (!t.Destroyed && t.IsRetreating)
                            {
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"  → 判定结果: 未完成（部队 {t.DisplayName} 还在撤退）");
                                #endif
                                return false;
                            }
                        }
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"  → 判定结果: 已完成（所有部队都进城或被摧毁）");
                        #endif
                        return true;

                    case LegionMission.None:
                        // 玩家军团或无任务AI军团永远不完成
                        return false;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// 检查并解散已完成的军团
        /// </summary>
        public bool CheckAndDisband()
        {
            if (this.IsComplete)
            {
                System.Diagnostics.Debug.WriteLine($"[Legion.CheckAndDisband] 军团{this.Name}({this.Kind})任务完成，自动解散");
                this.Disband();
                return true;
            }
            return false;
        }

        public int GetLegionHostileTroopFightingForceInView()
        {
            TroopList list = new TroopList();
            int num = 0;
            foreach (Troop troop in this.Troops)
            {
                foreach (Troop troop2 in troop.GetHostileTroopsInView())
                {
                    if (!list.HasGameObject(troop2))
                    {
                        list.Add(troop2);
                        num += troop2.FightingForce;
                    }
                }
            }
            return num;
        }

        public Architecture GetLegionTroopFactionStartArchitecture()
        {
            foreach (Troop troop in this.Troops)
            {
                if ((troop.StartingArchitecture != null) && (troop.StartingArchitecture.BelongedFaction == this.BelongedFaction))
                {
                    return troop.StartingArchitecture;
                }
            }
            return null;
        }

        /// <summary>
        /// 同步军团目标字段，并在玩家军团中自动校正任务枚举。
        /// </summary>
        public void SetOperationalTarget(Architecture target)
        {
            if (this.Kind == LegionKind.AI &&
                !string.IsNullOrEmpty(this.Name) &&
                this.Name.StartsWith("Player_", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Legion.SetOperationalTarget] 恢复军团类型: {this.Name}(ID:{this.ID}) AI -> Player");
                this.Kind = LegionKind.Player;
            }

            this.WillArchitecture = target;
            this.WillArchitectureString = target?.ID ?? -1;
            this.Target = target;
            this.TargetArchitectureID = target?.ID ?? -1;

            if (this.Kind == LegionKind.Player)
            {
                LegionMission expectedMission = target switch
                {
                    null => LegionMission.None,
                    _ when target.BelongedFaction == this.BelongedFaction => LegionMission.Defend,
                    _ => LegionMission.Attack
                };

                if (this.Mission != expectedMission)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Legion.SetOperationalTarget] 玩家军团{this.Name}任务校正：{this.Mission} -> {expectedMission} (Target:{target?.Name ?? "null"})");
                    this.Mission = expectedMission;
                }
            }
        }

        public bool EnsureOperationalTarget()
        {
            if (this.WillArchitecture != null)
            {
                this.SetOperationalTarget(this.WillArchitecture);
                return true;
            }

            GameScenario scenario = Session.Current?.Scenario;
            Architecture recovered = this.Target;

            if (recovered == null && scenario != null && this.TargetArchitectureID >= 0)
            {
                recovered = scenario.Architectures.GetGameObject(this.TargetArchitectureID) as Architecture;
            }

            if (recovered == null && scenario != null && this.WillArchitectureString >= 0)
            {
                recovered = scenario.Architectures.GetGameObject(this.WillArchitectureString) as Architecture;
            }

            if (recovered == null && this.StartArchitecture != null)
            {
                recovered = this.StartArchitecture;
            }

            if (recovered == null && scenario != null && this.StartArchitectureString >= 0)
            {
                recovered = scenario.Architectures.GetGameObject(this.StartArchitectureString) as Architecture;
                if (recovered != null)
                {
                    this.StartArchitecture = recovered;
                }
            }

            if (recovered == null && this.IsDefensive() && this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture.DefensiveLegion == this)
                    {
                        recovered = architecture;
                        break;
                    }
                }
            }

            if (recovered == null && this.IsDefensive())
            {
                recovered = this.GetLegionTroopFactionStartArchitecture();
            }

            if (recovered == null && scenario != null)
            {
                recovered = TryResolveArchitectureFromLegionName(scenario);
            }

            if (recovered == null && this.IsRetreating() && this.BelongedFaction?.Capital != null)
            {
                recovered = this.BelongedFaction.Capital;
            }

            if (recovered == null)
            {
                if (this.Kind == LegionKind.Player)
                {
                    this.SetOperationalTarget(null);
                }
                return false;
            }

            this.SetOperationalTarget(recovered);

            if (this.StartArchitecture == null && this.IsDefensive())
            {
                this.StartArchitecture = recovered;
                this.StartArchitectureString = recovered.ID;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Legion.TargetRecover] {this.Name}(ID:{this.ID}) recovered WillArchitecture -> {recovered.Name}(ID:{recovered.ID}), Mission:{this.Mission}");

            return true;
        }

        private Architecture TryResolveArchitectureFromLegionName(GameScenario scenario)
        {
            if (scenario?.Architectures == null || string.IsNullOrEmpty(this.Name))
            {
                return null;
            }

            int lastUnderscore = this.Name.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore >= this.Name.Length - 1)
            {
                return null;
            }

            string targetName = this.Name[(lastUnderscore + 1)..].Trim();
            if (string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            foreach (Architecture architecture in scenario.Architectures)
            {
                if (architecture != null && string.Equals(architecture.Name, targetName, StringComparison.Ordinal))
                {
                    return architecture;
                }
            }

            return null;
        }

        public int GetLegionTroopFightingForce()
        {
            int num = 0;
            foreach (Troop troop in this.Troops)
            {
                num += troop.FightingForce;
            }
            return num;
        }

        public Routeway GetMaxCreditRouteway()
        {
            int credit = 0;
            Routeway supplyingRouteway = null;
            foreach (SupplyingRoutewayPack pack in this.SupplyingRouteways)
            {
                if (pack.Credit > credit)
                {
                    credit = pack.Credit;
                    supplyingRouteway = pack.SupplyingRouteway;
                }
            }
            return supplyingRouteway;
        }

        public int GetMinTroopFoodCost()
        {
            if (this.Troops.Count <= 0)
            {
                return 0;
            }
            int foodCostPerDay = 0x7fffffff;
            foreach (Troop troop in this.Troops)
            {
                if (troop.FoodCostPerDay < foodCostPerDay)
                {
                    foodCostPerDay = troop.FoodCostPerDay;
                }
            }
            return foodCostPerDay;
        }

        public Troop GetWillClosestTroop()
        {
            if (this.Troops.Count == 1)
            {
                return (this.Troops[0] as Troop);
            }
            double maxValue = double.MaxValue;
            Troop troop = null;
            foreach (Troop troop2 in this.Troops)
            {
                double distance = Session.Current.Scenario.GetDistance(troop2.Position, this.WillArchitecture.ArchitectureArea);
                if (distance < maxValue)
                {
                    maxValue = distance;
                    troop = troop2;
                }
            }
            return troop;
        }

        public bool HasMovingTroopStartFromArchitecture(Architecture start)
        {
            foreach (Troop troop in this.Troops)
            {
                if ((troop.StartingArchitecture == start) && !troop.IsBaseViewingArchitecture(troop.WillArchitecture))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasTroop(Troop troop)
        {
            return this.Troops.HasGameObject(troop.ID);
        }

        public void LoadTroopsFromString(TroopList troops, string dataString)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.Troops.Clear();
            foreach (string str in strArray)
            {
                Troop gameObject = troops.GetGameObject(int.Parse(str)) as Troop;
                if (gameObject != null)
                {
                    this.AddTroop(gameObject);
                }
            }
        }

        /// <summary>
        /// 从军团移除部队 - 确保双向关系
        /// </summary>
        public void RemoveTroop(Troop troop)
        {
            if (troop == null) return;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[Legion.RemoveTroop] 军团 {this.Name}({this.Kind}, {this.Mission}) " +
                $"移除部队 {troop.DisplayName}(ID:{troop.ID})，剩余部队数: {this.Troops.Count - 1}");
            #endif
            
            troop.BelongedLegion = null;
            this.Troops.Remove(troop);
        }

        public void ResetCoreTroop()
        {
            if (this.Troops.Count > 0)
            {
                if (this.Troops.Count > 1)
                {
                    this.Troops.PropertyName = "Weighing";
                    this.Troops.IsNumber = true;
                    this.Troops.ReSort();
                }
                this.CoreTroop = this.Troops[0] as Troop;
            }
        }

        public void SetInformationPosition()
        {
            List<Point> orientations = new List<Point>();
            foreach (Troop troop in this.Troops)
            {
                orientations.Add(troop.Position);
            }
            this.InformationDestination = Session.Current.Scenario.GetClosestPosition(this.WillArchitecture.ArchitectureArea, orientations);
        }

        public void TroopAI()
        {
            System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] 军团{this.Name}({this.Kind}) 开始执行部队AI，部队数:{this.Troops.Count}");

            // 🔥 修复：清空推动链，防止上一回合的推动链污染本回合
            Troop.ClearPushingChain();

            // =========================================================
            // 步骤 1: 战术站位分配 (大脑先思考)
            // 必须先执行这个，给每个部队分配好 RealDestination，后面的排序才准确
            // =========================================================

            // 🔥 重构：玩家军团与AI军团分离
            // 日期：2026-03-09
            // 使用Kind判断，Mission用于AI军团的具体任务
            
            if (this.Kind == LegionKind.Player)
            {
                // 玩家军团：只在进攻城池时使用站位分配
                if (this.WillArchitecture != null && this.Troops.Count > 0)
                {
                    // 🔥 性能优化：使用 for 循环避免 GetList() 分配
                    // 检查是否是进攻指令（攻击城池或攻击部队）
                    bool isAttackCommand = false;
                    int troopCount = this.Troops.Count;
                    for (int i = 0; i < troopCount; i++)
                    {
                        if (this.Troops[i] is Troop t && 
                            (t.Command == TroopCommand.AttackArch || t.Command == TroopCommand.AttackTroop))
                        {
                            isAttackCommand = true;
                            break;
                        }
                    }
                    
                    if (isAttackCommand)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] 玩家军团{this.Name}，目标:{this.WillArchitecture.Name}，执行进攻站位分配");
                        this.AssignSmartSiegePositions();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] 玩家军团{this.Name}，非进攻指令，跳过站位分配");
                    }
                }
            }
            else
            {
                // AI 军团：根据任务分配站位
                if (this.WillArchitecture != null)
                {
                    // 🔥 调试：输出军团类型和目标城市归属
                    string targetFactionName = this.WillArchitecture.BelongedFaction?.Name ?? "无势力";
                    string legionFactionName = this.BelongedFaction?.Name ?? "无势力";
                    bool isTargetEnemy = this.WillArchitecture.BelongedFaction != this.BelongedFaction;
                    
                    System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] AI军团{this.Name}(Mission:{this.Mission})，目标:{this.WillArchitecture.Name}(归属:{targetFactionName})，军团归属:{legionFactionName}，是否敌方:{isTargetEnemy}");
                    
                    if (this.Mission == LegionMission.Attack)
                    {
                        this.AssignSmartSiegePositions();
                    }
                    else if (this.Mission == LegionMission.Defend)
                    {
                        this.AssignSmartDefensivePositions();
                    }
                }
            }

            // =========================================================
            // 步骤 2: 筛选有效部队
            // =========================================================
            List<Troop> activeTroops = [];
            foreach (var obj in this.Troops.GetList())
            {
                // 🔥 C# 12: 使用模式匹配
                if (obj is not Troop t) continue;
                if (t.Destroyed || t.OperationDone) continue;
                
                activeTroops.Add(t);
            }

            // =========================================================
            // 步骤 3: 智能调度排序 (按分配好的坑位距离排序)
            // =========================================================
            activeTroops.Sort((a, b) =>
            {
                // 撤退优先
                if (a.CurrentAIState == TroopAIState.Retreating && b.CurrentAIState != TroopAIState.Retreating) return -1;
                if (b.CurrentAIState == TroopAIState.Retreating && a.CurrentAIState != TroopAIState.Retreating) return 1;

                // 获取目标 (此时 RealDestination 已经被步骤1设置好了)
                Point destA = a.RealDestination;
                Point destB = b.RealDestination;

                // 兜底逻辑
                if (destA == Point.Zero && this.InformationDestination.HasValue) destA = this.InformationDestination.Value;
                if (destB == Point.Zero && this.InformationDestination.HasValue) destB = this.InformationDestination.Value;

                double distA = Session.Current.Scenario.GetDistance(a.Position, destA);
                double distB = Session.Current.Scenario.GetDistance(b.Position, destB);

                return distA.CompareTo(distB);
            });

            // =========================================================
            // 步骤 4: 执行部队AI (手脚开始行动)
            // =========================================================
            int executedCount = 0;
            foreach (Troop troop in activeTroops)
            {
                if (troop.OperationDone || troop.Destroyed) continue;

                System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] 调用部队AI: {troop.DisplayName}(ID:{troop.ID}) 排序顺位:{executedCount + 1}");
                troop.AI();
                executedCount++;
            }

            System.Diagnostics.Debug.WriteLine($"[Legion.TroopAI] 军团{this.Name} 部队AI执行完毕，执行数:{executedCount}");
        }
        /// <summary>
        /// 【智能防守】军团级别统一分配防守坑位
        /// </summary>
        private void AssignSmartDefensivePositions()
        {
            if (this.WillArchitecture == null) return;

            HashSet<Point> takenPositions = new HashSet<Point>();
            int assignedCount = 0;

            System.Diagnostics.Debug.WriteLine($"[SmartDefense] === 军团{this.Name} 开始分配防守坑位，目标:{this.WillArchitecture.Name}，部队数:{this.Troops.Count} ===");

            // 按战斗力排序，强的优先分配好位置
            List<Troop> sortedTroops = new List<Troop>();
            int destroyedCount = 0;
            int alreadyDefendingCount = 0;
            
            foreach (Troop t in this.Troops)
            {
                if (t.Destroyed)
                {
                    destroyedCount++;
                    continue;
                }
                
                // 🔥 新增：检查部队是否已在防守范围内
                if (t.IsBaseViewingArchitecture(this.WillArchitecture))
                {
                    alreadyDefendingCount++;
                    System.Diagnostics.Debug.WriteLine($"[SmartDefense] {t.DisplayName} 已在防守范围内，跳过分配");
                    continue;
                }
                
                sortedTroops.Add(t);
            }
            
            // 🔥 新增：详细日志
            System.Diagnostics.Debug.WriteLine($"[SmartDefense] 部队筛选：总数{this.Troops.Count}，已摧毁{destroyedCount}，已防守{alreadyDefendingCount}，待分配{sortedTroops.Count}");
            
            if (sortedTroops.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartDefense] === 军团{this.Name} 无需分配（所有部队已就位） ===");
                return;
            }
            
            sortedTroops.Sort((a, b) => b.FightingForce.CompareTo(a.FightingForce));

            foreach (Troop troop in sortedTroops)
            {
                // 跳过已在城市周边的部队
                if (troop.IsBaseViewingArchitecture(this.WillArchitecture))
                {
                    System.Diagnostics.Debug.WriteLine($"[SmartDefense] {troop.DisplayName} 已在防守范围内，跳过分配");
                    continue;
                }

                Point defensePos = troop.GetSmartSiegePosition(this.WillArchitecture, takenPositions);
                if (defensePos != new Point(-1, -1))
                {
                    takenPositions.Add(defensePos);
                    this.TakenPositions.Add(defensePos);
                    troop.ApplySmartSiegePosition(defensePos);
                    assignedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SmartDefense] === 军团{this.Name} 分配完成，成功{assignedCount}/{sortedTroops.Count}个部队 ===");
        }

        /// <summary>
        /// 【智能攻城】军团级别统一分配攻击坑位
        /// </summary>
        internal void AssignSmartSiegePositions()
        {
            if (this.WillArchitecture == null) return;

            HashSet<Point> takenPositions = [];
            int assignedCount = 0;

            System.Diagnostics.Debug.WriteLine($"[SmartSiege] === 军团{this.Name} 开始分配攻击坑位，目标:{this.WillArchitecture.Name}，部队数:{this.Troops.Count} ===");

            // 先按距离排序，近的优先分配
            List<Troop> sortedTroops = [];
            foreach (Troop t in this.Troops)
            {
                if (!t.Destroyed && t.WillArchitecture == this.WillArchitecture)
                {
                    sortedTroops.Add(t);
                }
            }
            sortedTroops.Sort((a, b) =>
            {
                // 使用曼哈顿距离，避免溢出
                int distA = Math.Abs(a.Position.X - this.WillArchitecture.Position.X) +
                            Math.Abs(a.Position.Y - this.WillArchitecture.Position.Y);
                int distB = Math.Abs(b.Position.X - this.WillArchitecture.Position.X) +
                            Math.Abs(b.Position.Y - this.WillArchitecture.Position.Y);
                return distA.CompareTo(distB);
            });

            foreach (Troop troop in sortedTroops)
            {
                // 🔥 修复：跳过已在城池接触区的部队（与攻击触发条件一致）
                // 使用 GetContactArea 而非 IsBaseViewingArchitecture，确保部队不会被重新分配坑位
                bool inContactArea = this.WillArchitecture.ArchitectureArea.GetContactArea(false).HasPoint(troop.Position);
                bool inAttackRange = troop.CanAttack(this.WillArchitecture);
                if (inContactArea || inAttackRange)
                {
                    // 🔥 新增：协同前进检查 - 如果堵住了后方友军，尝试侧移到其他攻击位
                    bool isBlockingAllies = IsBlockingAlliesPath(troop, sortedTroops, takenPositions);
                    if (isBlockingAllies)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SmartSiege] {troop.DisplayName} 已在接触区但堵住后方友军，尝试侧移");
                        
                        // 🔥 根本修复：移除部队当前位置，避免误判为"已占用"
                        // 日期：2026-03-05
                        // 原因：侧移时，旧位置仍在 takenPositions 中，导致后续部队无法使用该位置
                        // 解决：在计算新坑位之前，先移除当前位置
                        takenPositions.Remove(troop.Position);
                        this.TakenPositions.Remove(troop.Position);
                        
                        // 尝试找到其他可用的攻击位
                        Point newSiegePos = troop.GetSmartSiegePosition(this.WillArchitecture, takenPositions);
                        if (newSiegePos != new Point(-1, -1))
                        {
                            takenPositions.Add(newSiegePos);
                            this.TakenPositions.Add(newSiegePos);
                            troop.ApplySmartSiegePosition(newSiegePos);
                            assignedCount++;
                            System.Diagnostics.Debug.WriteLine($"[SmartSiege] {troop.DisplayName} 侧移到新攻击位 {newSiegePos}");
                        }
                        else
                        {
                            // 🔥 修复：如果没有可用侧移位置，需要把当前位置加回去
                            takenPositions.Add(troop.Position);
                            this.TakenPositions.Add(troop.Position);
                            System.Diagnostics.Debug.WriteLine($"[SmartSiege] {troop.DisplayName} 无可用侧移位置，保持原位");
                        }
                    }
                    else
                    {
                        // 🔥 修复：已在攻击位置且不堵路的部队，需要把位置加入 takenPositions
                        // 日期：2026-03-08
                        // 原因：如果不加入，后续部队会认为该位置可用，导致候选坑位被错误过滤
                        // 场景：曹操队已在接触区 → 不加入 takenPositions → 朱儁队生成候选时认为该位置可用
                        //       → 但实际上曹操队占据了 → 导致朱儁队只有1个候选坑位
                        takenPositions.Add(troop.Position);
                        this.TakenPositions.Add(troop.Position);
                        System.Diagnostics.Debug.WriteLine($"[SmartSiege] {troop.DisplayName} 已在攻击范围内且不堵路，保持原位并标记位置为已占用");
                    }
                    continue;
                }

                Point siegePos = troop.GetSmartSiegePosition(this.WillArchitecture, takenPositions);
                if (siegePos != new Point(-1, -1))
                {
                    if (siegePos == troop.Position)
                    {
                        takenPositions.Add(troop.Position);
                        this.TakenPositions.Add(troop.Position);
                        System.Diagnostics.Debug.WriteLine($"[SmartSiege] {troop.DisplayName} 当前攻击位评分最高，保持原位");
                        continue;
                    }

                    takenPositions.Add(siegePos);
                    this.TakenPositions.Add(siegePos); // 使用Legion自带的字段
                    troop.ApplySmartSiegePosition(siegePos);
                    assignedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SmartSiege] === 军团{this.Name} 分配完成，成功{assignedCount}/{sortedTroops.Count}个部队 ===");
        }
        
        /// <summary>
        /// 🔥 协同前进：检查部队是否堵住了后方友军的路径
        /// </summary>
        private bool IsBlockingAlliesPath(Troop frontTroop, List<Troop> allTroops, HashSet<Point> takenPositions)
        {
            HashSet<Point> reservedPositions = new HashSet<Point>(takenPositions)
            {
                frontTroop.Position
            };

            // 检查是否有后方友军想要前进但被堵住
            foreach (Troop backTroop in allTroops)
            {
                // 跳过自己和已销毁的部队
                if (backTroop == frontTroop || backTroop.Destroyed) continue;
                
                // 只检查还没到达接触区的部队
                bool backInContactArea = this.WillArchitecture.ArchitectureArea.GetContactArea(false).HasPoint(backTroop.Position);
                if (backInContactArea) continue;
                
                // 检查后方部队是否在前往目标的路径上
                // 简化判断：如果后方部队的目标是 WillArchitecture，且前方部队在其路径上
                if (backTroop.WillArchitecture == this.WillArchitecture)
                {
                    Point alternativeSiegePosition = backTroop.GetSmartSiegePosition(this.WillArchitecture, reservedPositions);
                    if (alternativeSiegePosition != new Point(-1, -1))
                    {
                        continue;
                    }

                    // 检查前方部队是否在后方部队和目标之间
                    // 使用曼哈顿距离判断
                    int backToTarget = Math.Abs(backTroop.Position.X - this.WillArchitecture.Position.X) +
                                      Math.Abs(backTroop.Position.Y - this.WillArchitecture.Position.Y);
                    int frontToTarget = Math.Abs(frontTroop.Position.X - this.WillArchitecture.Position.X) +
                                       Math.Abs(frontTroop.Position.Y - this.WillArchitecture.Position.Y);
                    int backToFront = Math.Abs(backTroop.Position.X - frontTroop.Position.X) +
                                     Math.Abs(backTroop.Position.Y - frontTroop.Position.Y);
                    
                    // 如果 backToFront + frontToTarget ≈ backToTarget，说明前方部队在路径上
                    // 允许 ±2 的误差（考虑到斜线移动）
                    if (Math.Abs((backToFront + frontToTarget) - backToTarget) <= 2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[协同检查] {frontTroop.DisplayName} 堵住了 {backTroop.DisplayName} 的路径");
                        return true;
                    }
                }
            }
            
            return false;
        }

        public int FoodCostPerDay
        {
            get
            {
                int num = 0;
                foreach (Troop troop in this.Troops)
                {
                    num += troop.FoodCostPerDay;
                }
                return num;
            }
        }

        public bool HasCuttingRoutewayTroop
        {
            get
            {
                foreach (Troop troop in this.Troops)
                {
                    if (troop.CutRoutewayDays > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool HasTroopViewingWillArchitecture
        {
            get
            {
                foreach (Troop troop in this.Troops)
                {
                    if (troop.IsBaseViewingArchitecture(this.WillArchitecture))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool HasTroopWillArchitectureIsWillArchitecture
        {
            get
            {
                foreach (Troop troop in this.Troops)
                {
                    if (troop.WillArchitecture == this.WillArchitecture)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int ArmyScale
        {
            get
            {
                int r = 0;
                foreach (Troop t in this.Troops)
                {
                    r += t.Army.Scales;
                }
                return r;
            }
        }
        [DataMember]
        public LegionKind Kind
        {
            get => this.kind;
            set
            {
                // 🔥 调试：追踪军团类型修改
                // 日期：2026-03-09
                // 警告：Kind应该在创建时设置，不应该运行时修改
                // TODO: 后续版本改为init-only
                if (this.kind != value)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Legion.Kind] ⚠️ 军团{this.Name}类型被修改：{this.kind}→{value}");
                    
                    #if DEBUG
                    if (!string.IsNullOrEmpty(this.Name))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Legion.Kind] 调用堆栈：\n{Environment.StackTrace}");
                    }
                    #endif
                }
                this.kind = value;
            }
        }
        
        /// <summary>
        /// 军团任务
        /// 日期：2026-03-09
        /// 说明：类型和任务分离，同一军团可以执行不同任务
        /// </summary>
        [DataMember]
        [JsonInclude]
        public LegionMission Mission
        {
            get => this.mission;
            set
            {
                if (this.mission != value)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Legion.Mission] 军团{this.Name}任务变更：{this.mission}→{value}");
                }
                this.mission = value;
            }
        }
        
        /// <summary>
        /// 🔥 重构：判断是否为进攻军团
        /// 日期：2026-03-09
        /// 说明：使用Mission判断，而不是旧的Kind
        /// </summary>
        public bool IsOffensive()
        {
            return this.Kind == LegionKind.AI && this.Mission == LegionMission.Attack;
        }
        
        /// <summary>
        /// 🔥 重构：判断是否为防守军团
        /// 日期：2026-03-09
        /// </summary>
        public bool IsDefensive()
        {
            return this.Kind == LegionKind.AI && this.Mission == LegionMission.Defend;
        }
        
        /// <summary>
        /// 🔥 重构：判断是否为撤退军团
        /// 日期：2026-03-09
        /// </summary>
        public bool IsRetreating()
        {
            return this.Kind == LegionKind.AI && this.Mission == LegionMission.Retreat;
        }
        
        /// <summary>
        /// 🔥 重构：判断是否为玩家军团
        /// 日期：2026-03-09
        /// </summary>
        public bool IsPlayerControlled()
        {
            return this.Kind == LegionKind.Player;
        }
    }
}


