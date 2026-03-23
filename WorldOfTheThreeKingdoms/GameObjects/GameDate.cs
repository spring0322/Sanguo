using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects.Events;  // 🔥 新增：ScenarioEvents
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;  // 🔥 新增：System.Text.Json 支持

namespace GameObjects
{
    [DataContract]
    public class GameDate
    {
        // 🔥 AOT 修复：添加 JsonInclude 以支持 System.Text.Json 序列化字段
        // 日期：2026-03-20
        // 原因：DataContract/DataMember 是 DataContractSerializer 的标记
        //       System.Text.Json 不识别这些标记，导致读档后字段值为 0
        //       添加 JsonInclude 后，System.Text.Json 可以正确序列化/反序列化字段
        
        [DataMember]
        [JsonInclude]
        public int Day = 1;

        [DataMember]
        [JsonInclude]
        public int DaysLeft;

        [DataMember]
        [JsonInclude]
        public bool IsRunning = false;

        [DataMember]
        [JsonInclude]
        public int Month = 1;

        [DataMember]
        [JsonInclude]
        public GameSeason Season;

        [DataMember]
        [JsonInclude]
        public int Year = 0xb8;

        public bool EndRunning()
        {
            if (!this.IsRunning)
            {
                return false;
            }
            
            var scenario = Session.Current.Scenario;
            
            // 🔥 新事件系统：直接检查阻塞条件，不依赖旧事件返回值
            // 阻塞条件 1：AI 正在思考
            if (scenario.Threading)
            {
                return false;
            }
            
            // 阻塞条件 2：玩家未通过回合
            if (scenario.CurrentPlayer != null && !scenario.CurrentPlayer.Passed)
            {
                // 🔥 死锁修复：如果玩家未 Passed 且未 Controlling，强制授予控制权
                if (!scenario.CurrentPlayer.Controlling)
                {
                    scenario.CurrentPlayer.Controlling = true;
                    return false; // 下一帧刷新 UI
                }
                return false;
            }
            
            // ✅ 所有阻塞条件解除，触发日事件
            scenario.DayPassedEvent();
            
            // 🔥 月事件和年事件检查
            if (this.Day >= 30 - Session.Parameters.DayInTurn + 1)
            {
                // 月末：检查是否需要阻塞
                if (scenario.Threading)
                {
                    return false;
                }
                
                scenario.MonthPassedEvent();
                
                // 年末：检查是否需要阻塞
                if (this.Month >= 12)
                {
                    if (scenario.Threading)
                    {
                        return false;
                    }
                    
                    scenario.YearPassedEvent();
                }
            }
            
            // ✅ 所有事件处理完成
            this.IsRunning = false;
            return true;
        }



        public float GetFoodRateBySeason(GameSeason season)
        {
            switch (season)
            {
                case GameSeason.春:
                    return 0.6f;

                case GameSeason.夏:
                    return 1f;

                case GameSeason.秋:
                    return 1f;

                case GameSeason.冬:
                    return 0.3f;
            }
            return 0f;
        }

        public GameSeason GetSeason(int dayslater)
        {
            if (((this.Day + dayslater) > 30) && ((this.Month % 3) == 0))
            {
                return (GameSeason.春 + ((int)this.Season % (int)GameSeason.冬));
            }
            return this.Season;
        }

        public void Go()
        {
            //this.Day++;
            this.Day += Session.Parameters.DayInTurn;
            if (this.Day > 30)
            {
                //this.Day = 1;
                this.Day -= 30;
                this.Month++;
                if (this.Month > 12)
                {
                    this.Month = 1;
                    this.Year++;
                }
                this.SetSeason();
            }
            if (this.DaysLeft > 0)
            {
                this.DaysLeft--;
                // System.Diagnostics.Debug.WriteLine($"[GameDate.Go] DaysLeft递减: {this.DaysLeft + 1} -> {this.DaysLeft}");
            }

        }

        public void Go(int i)
        {
            this.Day += i;
            while (this.Day > 30)
            {
                this.Day -= 30;
                this.Month++;
                if (this.Month > 12)
                {
                    this.Month = 1;
                    this.Year++;
                }
                this.SetSeason();
            }
        }

        public void LoadDateData(int year, int month, int day)
        {
            this.Year = year;
            this.Month = month;
            this.Day = day;
            this.SetSeason();
        }

        public void SetSeason()
        {
            GameSeason oldSeason = this.Season;
            
            if (this.Month >= 3 && this.Month <= 5)
            {
                this.Season = GameSeason.春;
            }
            else if (this.Month >= 6 && this.Month <= 8)
            {
                this.Season = GameSeason.夏;
            }
            else if (this.Month >= 9 && this.Month <= 11)
            {
                this.Season = GameSeason.秋;
            }
            else
            {
                this.Season = GameSeason.冬;
            }
            
            // 🔥 修复读档崩溃：读档期间 Session.Current.Scenario 可能为 null
            // 原因：ProcessScenarioData 中设置 Date 时会触发季节变化，但此时 Session.Current.Scenario 还未赋值
            // 日期：2026-03-16
            if (oldSeason != this.Season && Session.Current.Scenario != null)
            {
                ScenarioEvents.RaiseSeasonChanged(Session.Current.Scenario, this.Season);
                
                // 🔥 2026-03-09 新增：季节变化时更新天气
                // ANTI-BAND-AID：WeatherManager 必须在 Scenario.Init() 中初始化，如果为 null 说明数据流错误
                if (Session.Current.Scenario.WeatherManager != null)
                {
                    var seasonType = WorldOfTheThreeKingdoms.GameLogic.SeasonConverter.ToSeasonType(this.Season);
                    Session.Current.Scenario.WeatherManager.UpdateWeather(seasonType);
                    System.Diagnostics.Debug.WriteLine($"[GameDate] 季节变化：{oldSeason} → {this.Season}，天气已更新");
                }
            }
        }

        private static DateTime _lastStartRunningLogTime = DateTime.MinValue;
        
        public bool StartRunning()
        {
            // 🔥 AOT修复：如果IsRunning卡在true，检查是否是死锁状态并强制重置
            if (this.IsRunning)
            {
                // 检查是否是死锁状态（Threading=false但IsRunning=true）
                if (Session.Current.Scenario != null && !Session.Current.Scenario.Threading)
                {
                    this.IsRunning = false;
                }
                else
                {
                    // 正常的等待状态
                    if ((DateTime.Now - _lastStartRunningLogTime).TotalSeconds > 5)
                    {
                        _lastStartRunningLogTime = DateTime.Now;
                    }
                    return false;
                }
            }
            
            // 🔥 数据完整性：StartRunning 在游戏运行时调用，Scenario 必须存在
            // 如果为 null 说明游戏状态异常，应该崩溃而不是静默返回
            var scenario = Session.Current.Scenario;
            
            // 🔥 新事件系统：调用 ScenarioEvents.OnDayStarting
            if (!ScenarioEvents.RaiseDayStarting(scenario))
            {
                return false;
            }
            
            if (this.Day <= scenario.Parameters.DayInTurn)
            {
                // 🔥 新事件系统：调用 ScenarioEvents.OnMonthStarting
                if (!ScenarioEvents.RaiseMonthStarting(scenario))
                {
                    return false;
                }
                
                // 🔥 新事件系统：调用 ScenarioEvents.OnYearStarting
                if (this.Month == 1 && !ScenarioEvents.RaiseYearStarting(scenario))
                {
                    return false;
                }
            }
            
            this.IsRunning = true;
            return true;
        }

        public string ToDateString()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            return string.Concat(new object[] { this.Year, "年", this.Month, "月", this.Day, "日" });
        }

        public int LeftDays
        {
            get
            {
                return (360 - this.PassedDays);
            }
        }

        public int PassedDays
        {
            get
            {
                return ((this.Month * 30) + this.Day);
            }
        }

        public GameDate()
        {
            // 🔥 关键修复：显式初始化字段，避免 AOT 或序列化器问题
            // 日期：2026-03-17
            // 原因：字段默认值（Year = 0xb8）在某些情况下不会被应用
            //       导致 Year/Month/Day 都是 0，显示"0年0月0日"
            Year = 0xb8;  // 184
            Month = 1;
            Day = 1;
        }

        public GameDate(int y, int m, int d)
        {
            Year = y;
            Month = m;
            Day = d;
        }

        public GameDate(GameDate d)
        {
            Year = d.Year;
            Month = d.Month;
            Day = d.Day;
        }
    }
}

