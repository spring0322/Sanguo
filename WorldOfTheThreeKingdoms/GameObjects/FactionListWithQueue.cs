using GameManager;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects
{
    [DataContract]
    public class FactionListWithQueue : FactionList
    {
        private Queue<Faction> factionQueue = new Queue<Faction>();
        public Faction RunningFaction;
        private int consecutiveFailures = 0; // Deadlock protection

        [DataMember]
        public string FactionQueue { get; set; }

        public void BuildQueue(bool preUserControlFinished)
        {
            factionQueue = new Queue<Faction>();

            GameObjectList list = base.GetList();
            list.PropertyName = "Power";
            list.IsNumber = true;
            list.SmallToBig = true;
            list.ReSort();
            foreach (Faction faction in list)
            {
                this.SetFactionInQueue(faction, preUserControlFinished);
            }
        }

        public bool HasFactionInQueue(FactionList list)
        {
            foreach (Faction faction in list)
            {
                if (this.IsFactionInQueue(faction))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsFactionInQueue(Faction faction)
        {
            foreach (Faction faction2 in this.factionQueue)
            {
                if (faction2 == faction)
                {
                    return true;
                }
            }
            return false;
        }

        public void LoadQueueFromString(string dataString)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.factionQueue.Clear();
            foreach (string str in strArray)
            {
                Faction gameObject = base.GetGameObject(int.Parse(str)) as Faction;
                if (gameObject != null)
                {
                    this.factionQueue.Enqueue(gameObject);
                }
            }
        }

        private static int _runQueueStuckCounter = 0;
        private static Faction _lastRunningFaction = null;
        
        public void RunQueue()
        {

            
            if (this.RunningFaction != null)
            {
                // 检测是否卡在同一个势力
                if (this.RunningFaction == _lastRunningFaction)
                {
                    // 🔥 修复：如果是玩家控制的势力，不要计数也不要跳过
                    if (!this.RunningFaction.Controlling)
                    {
                        _runQueueStuckCounter++;
                        if (_runQueueStuckCounter > 100)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RunQueue] 警告：势力 {this.RunningFaction?.Name} Run()返回false超过100次，强制跳过");
                            this.RunningFaction = null;
                            _runQueueStuckCounter = 0;
                            return;
                        }
                    }
                }
                else
                {
                    _lastRunningFaction = this.RunningFaction;
                    _runQueueStuckCounter = 0;
                }
                
                if (this.RunningFaction.Run())
                {
                    // System.Diagnostics.Debug.WriteLine($"[RunQueue] 势力 {this.RunningFaction?.Name} Run() 完成，清除RunningFaction");
                    this.RunningFaction = null;
                    this.consecutiveFailures = 0;
                }
                else
                {
                    // 每10次输出一次警告
                    if (_runQueueStuckCounter % 10 == 0 && _runQueueStuckCounter > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RunQueue] 势力 {this.RunningFaction?.Name} Run() 返回false，等待中... (第{_runQueueStuckCounter}次)");
                    }
                    this.consecutiveFailures++;
                }
            }
            else if (!this.QueueEmpty)
            {
                _runQueueStuckCounter = 0;
                _lastRunningFaction = null;
                
                this.RunningFaction = this.factionQueue.Dequeue();
                // System.Diagnostics.Debug.WriteLine($"[Diagnostic] RunQueue Dequeue. New RunningFaction: {this.RunningFaction?.Name}");
                // System.Diagnostics.Debug.WriteLine($"[RunQueue] 开始处理势力: {this.RunningFaction?.Name}");
                
                if (this.RunningFaction != null)
                {
                    if (this.RunningFaction.Leader.BelongedFaction == null)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[RunQueue] 势力 {this.RunningFaction?.Name} Leader无归属势力，跳过");
                        this.RunningFaction = null;
                    }
                    else
                    {
                        Session.Current.Scenario.CurrentFaction = this.RunningFaction;
                        if (this.RunningFaction.Run())
                        {
                            // System.Diagnostics.Debug.WriteLine($"[RunQueue] 势力 {this.RunningFaction?.Name} 立即完成");
                            this.RunningFaction = null;
                            this.consecutiveFailures = 0;
                        }
                        else
                        {
                            this.consecutiveFailures++;
                        }
                    }
                }
            }
        }

        public string SaveQueueToString()
        {
            string str = "";
            if (this.factionQueue != null)
            { 
                foreach (Faction faction in this.factionQueue)
                {
                    str = str + " " + faction.ID.ToString();
                }
            }
            return str;
        }

        public void SetControlling(bool controlling)
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Faction in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Faction faction = base.GameObjects[i] as Faction;
                if (faction == null)
                {
                    throw new InvalidOperationException($"FactionListWithQueue 中存在非 Faction 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                faction.Controlling = controlling;
            }
        }

        private void SetFactionInQueue(Faction faction, bool preUserControlFinished)
        {
            this.factionQueue.Enqueue(faction);
            faction.Passed = false;
            faction.PreUserControlFinished = preUserControlFinished;
            faction.AIFinished = false;
            
            // 🔥 CRITICAL FIX: 玩家势力在新回合开始时应该获得控制权
            // 日期：2026-03-19
            // 原因：点击"进行"后 Controlling 被设置为 false，导致下一回合无法获得控制权
            // 解决：在 BuildQueue 时重置玩家势力的状态，确保新回合能正确获得控制权
            if (Session.Current?.Scenario?.IsPlayer(faction) == true)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SetFactionInQueue] 玩家势力 {faction.Name} 加入队列，重置状态");
                #endif
                
                faction.Controlling = false; // 初始为false，等待Run()时设置为true
                faction.StopToControl = true;  // 🔥 关键修复：标记玩家需要控制权（WantControl 由此计算）
            }
            else
            {
                faction.Controlling = false;
                faction.StopToControl = false;
            }
        }

        public bool QueueEmpty
        {
            get
            {
                // 🔥 修复：必须同时检查队列为空 AND 当前没有正在运行的任务
                return factionQueue.Count == 0 && RunningFaction == null;
            }
        }
    }
}

