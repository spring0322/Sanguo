// ⏰ 时间管理器 - 支持军师智能打断的时间跳过系统
// 实现时间跳过中的风险检测和智能中断功能

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// ⏰ 时间管理器 - 智能时间跳过系统 (MonoGame版本)
    /// 模拟Unity协程的时间跳过机制，支持军师智能中断
    /// </summary>
    public class TimeManager
    {
        public bool IsSkippingTime { get; private set; } = false; // 是否正在快速跳过
        public Faction PlayerFaction { get; set; }
        
        // 跳过状态
        private int _currentSkipDay = 0;
        private int _totalSkipDays = 0;
        private float _skipTimer = 0f;
        private const float _dayInterval = 0.1f; // 每0.1秒跳过一天
        
        // 事件委托
        // public event Action<AdviceData> OnStrategistAdvice;
        public event Action<int> OnDayAdvanced;
        public event Action OnTimeSkipCompleted;
        public event Action OnTimeSkipInterrupted;
        
        private static TimeManager _instance;
        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TimeManager();
                return _instance;
            }
        }
        
        /// <summary>
        /// 开始跳过指定天数 - MonoGame版本
        /// </summary>
        /// <param name="daysToSkip">要跳过的天数</param>
        /// <param name="playerFaction">玩家势力</param>
        public void StartSkipping(int daysToSkip, Faction playerFaction)
        {
            if (IsSkippingTime)
            {
                System.Diagnostics.Debug.WriteLine("时间跳过已在进行中");
                return;
            }
            
            if (daysToSkip <= 0)
            {
                System.Diagnostics.Debug.WriteLine("跳过天数必须大于0");
                return;
            }
            
            // 设置跳过参数
            PlayerFaction = playerFaction;
            _totalSkipDays = daysToSkip;
            _currentSkipDay = 0;
            _skipTimer = 0f;
            IsSkippingTime = true;
            
            System.Diagnostics.Debug.WriteLine($">> 开始跳过 {daysToSkip} 天");
        }
        
        /// <summary>
        /// 更新时间跳过逻辑 - 在游戏主循环中调用
        /// </summary>
        /// <param name="gameTime">游戏时间</param>
        public void Update(GameTime gameTime)
        {
            if (!IsSkippingTime) return;
            
            _skipTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // 每0.1秒处理一天
            if (_skipTimer >= _dayInterval)
            {
                _skipTimer = 0f;
                ProcessOneDay();
                
                _currentSkipDay++;
                
                // 检查是否完成跳过
                if (_currentSkipDay >= _totalSkipDays)
                {
                    CompleteTimeSkip();
                }
            }
        }
        
        /// <summary>
        /// 停止时间跳过
        /// </summary>
        public void StopSkipping()
        {
            if (IsSkippingTime)
            {
                IsSkippingTime = false;
                System.Diagnostics.Debug.WriteLine(">> 时间跳过已手动停止");
                OnTimeSkipInterrupted?.Invoke();
            }
        }
        
        /// <summary>
        /// 处理一天的跳过逻辑 - 核心智能中断逻辑
        /// </summary>
        private void ProcessOneDay()
        {
            try
            {
                // 1. 推进一天的时间/资源逻辑
                AdvanceOneDay();
                
                // 获取当前日期字符串用于日志
                string dateStr = GetCurrentDateString();
                
                // ===========================================
                // [补丁生效处] 军师检查
                // ===========================================
                if (PlayerFaction?.Leader != null) // 检查玩家势力是否有君主
                {
                    var strategist = GetStrategist(PlayerFaction);
                    if (strategist != null)
                    {
                        // 让军师检查一下，给他当前日期方便写日志
                        var advice = StrategistManager.CheckRisks(PlayerFaction, dateStr);
                        
                        // 情况 A：遇到致命威胁 -> 强制急停
                        if (advice.Level == RiskLevel.Critical)
                        {
                            System.Diagnostics.Debug.WriteLine($">> 军师 {strategist.Name} 触发紧急熔断，停止跳过！日期: {dateStr}");
                            
                            // 1. 关掉跳过状态
                            IsSkippingTime = false;
                            
                            // 2. 弹窗
                            EventManager.TriggerStrategistEvent(advice);
                            OnTimeSkipInterrupted?.Invoke();
                            
                            // 3. 彻底退出循环 (剩下的天数不跳了)
                            return;
                        }
                        
                        // 情况 B：遇到一般建议 -> 仅仅记录，不打断
                        if (advice.Level == RiskLevel.Info)
                        {
                            System.Diagnostics.Debug.WriteLine($"军师建议 ({dateStr}): {advice.Title}");
                            // 这里不弹窗，只是把 UI 上的"军师"按钮设为高亮/红点
                            // NotificationSystem.ShowRedDot("StrategistBtn");
                        }
                    }
                }
                
                // 触发日期推进事件
                OnDayAdvanced?.Invoke(_currentSkipDay + 1);
                
                System.Diagnostics.Debug.WriteLine($"跳过进度: {_currentSkipDay + 1}/{_totalSkipDays} - {dateStr}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理一天跳过逻辑出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 完成时间跳过
        /// </summary>
        private void CompleteTimeSkip()
        {
            IsSkippingTime = false;
            System.Diagnostics.Debug.WriteLine(">> 时间跳过完成");
            OnTimeSkipCompleted?.Invoke();
        }
        
        /// <summary>
        /// 推进一天的游戏逻辑
        /// </summary>
        private void AdvanceOneDay()
        {
            try
            {
                // 推进游戏日期
                if (Session.Current?.Scenario?.Date != null)
                {
                    // 使用游戏原有的日期推进方法
                    Session.Current.Scenario.Date.Go();
                }
                
                // 执行每日更新逻辑
                ExecuteDailyUpdates();
                
                System.Diagnostics.Debug.WriteLine($"日期推进到: {Session.Current.Scenario.Date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"推进日期时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行每日更新逻辑
        /// </summary>
        private void ExecuteDailyUpdates()
        {
            if (Session.Current?.Scenario == null) return;
            
            try
            {
                // 1. 更新所有势力的资源
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (faction == null || faction.Destroyed) continue;
                    
                    UpdateFactionDaily(faction);
                }
                
                // 2. 更新部队状态
                foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                {
                    if (troop == null || troop.Destroyed) continue;
                    
                    UpdateTroopDaily(troop);
                }
                
                // 3. 处理建筑事件
                foreach (Architecture architecture in Session.Current.Scenario.Architectures.GetList())
                {
                    if (architecture == null) continue;
                    
                    UpdateArchitectureDaily(architecture);
                }
                
                // 4. 清理过期的AI记忆
                CleanupAIMemories();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"每日更新出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新势力每日状态
        /// </summary>
        private void UpdateFactionDaily(Faction faction)
        {
            // 这里可以添加势力每日更新逻辑
            // 例如：外交关系变化、技术研发进度等
        }
        
        /// <summary>
        /// 更新部队每日状态
        /// </summary>
        private void UpdateTroopDaily(Troop troop)
        {
            // 这里可以添加部队每日更新逻辑
            // 例如：移动、补给消耗、士气变化等
            
            // 执行部队的每日事件
            try
            {
                troop.DayEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"部队 {troop.ID} 每日更新出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新建筑每日状态
        /// </summary>
        private void UpdateArchitectureDaily(Architecture architecture)
        {
            // 这里可以添加建筑每日更新逻辑
            // 例如：资源生产、人口增长、建设进度等
            
            try
            {
                // 执行建筑的每日事件
                architecture.DayEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"建筑 {architecture.Name} 每日更新出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理AI记忆
        /// </summary>
        private void CleanupAIMemories()
        {
            try
            {
                int currentDay = Session.Current.Scenario.Date.Day;
                
                foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                {
                    if (faction?.MemoryMap != null)
                    {
                        faction.MemoryMap.CleanExpiredMemories(currentDay);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理AI记忆出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取势力的军师
        /// </summary>
        private Person GetStrategist(Faction faction)
        {
            try
            {
                // 查找智力最高的武将作为军师
                Person strategist = null;
                int maxIntelligence = 0;
                
                foreach (Person person in faction.Persons.GetList())
                {
                    if (person != null && person.Intelligence > maxIntelligence)
                    {
                        maxIntelligence = person.Intelligence;
                        strategist = person;
                    }
                }
                
                // 只有智力超过80的才能当军师
                return (strategist?.Intelligence >= 80) ? strategist : null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 获取当前跳过进度
        /// </summary>
        public float GetSkipProgress()
        {
            if (!IsSkippingTime || _totalSkipDays == 0)
                return 0f;
            
            return (float)_currentSkipDay / _totalSkipDays;
        }
        
        /// <summary>
        /// 获取跳过进度信息
        /// </summary>
        public (int current, int total) GetSkipProgressInfo()
        {
            return (_currentSkipDay, _totalSkipDays);
        }
        
        /// <summary>
        /// 获取跳过状态信息
        /// </summary>
        public string GetSkipStatusInfo()
        {
            if (!IsSkippingTime)
                return "未在跳过时间";
            
            return $"正在跳过时间... ({_currentSkipDay}/{_totalSkipDays}) 当前日期: {GetCurrentDateString()}";
        }
        
        /// <summary>
        /// 获取当前日期字符串
        /// </summary>
        private string GetCurrentDateString()
        {
            try
            {
                if (Session.Current?.Scenario?.Date != null)
                {
                    var date = Session.Current.Scenario.Date;
                    return $"{date.Year}年{date.Month}月{date.Day}日";
                }
                return "未知日期";
            }
            catch
            {
                return "日期获取失败";
            }
        }
        
        /// <summary>
        /// 处理每日游戏逻辑
        /// </summary>
        private void ProcessDailyGameLogic()
        {
            try
            {
                // 这里可以添加每日特殊逻辑
                // 例如：随机事件、外交变化、市场波动等
                
                // 示例：每10天清理一次过期数据
                if (Session.Current?.Scenario?.Date?.Day % 10 == 0)
                {
                    CleanupExpiredData();
                }
                
                // 示例：每月初进行特殊检查
                if (Session.Current?.Scenario?.Date?.Day == 1)
                {
                    ProcessMonthlyEvents();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理每日游戏逻辑出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理过期数据
        /// </summary>
        private void CleanupExpiredData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("执行每10天的数据清理");
                
                // 清理AI记忆
                CleanupAIMemories();
                
                // 可以添加其他清理逻辑
                // 例如：清理过期的外交消息、过期的市场数据等
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理过期数据出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理月初事件
        /// </summary>
        private void ProcessMonthlyEvents()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("处理月初特殊事件");
                
                // 这里可以添加月初特殊逻辑
                // 例如：月度报告、季节变化、税收结算等
                
                // 示例：触发月度军师全面分析
                if (PlayerFaction != null)
                {
                    var strategist = GetStrategist(PlayerFaction);
                    if (strategist != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"月初 - 军师 {strategist.Name} 进行全面分析");
                        // 这里可以触发更详细的分析报告
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理月初事件出错: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 时间跳过事件参数
    /// </summary>
    public class TimeSkipEventArgs : EventArgs
    {
        public int DaysSkipped { get; set; }
        public int TotalDays { get; set; }
        public bool WasInterrupted { get; set; }
        public AdviceData InterruptReason { get; set; }
    }
}