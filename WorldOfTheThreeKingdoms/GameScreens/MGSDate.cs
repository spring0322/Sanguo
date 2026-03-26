using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using GameObjects.Events;  // 🔥 新增：ScenarioEvents
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PluginInterface;
using WorldOfTheThreeKingdoms.GameLogic;
using WorldOfTheThreeKingdoms.GameScreens;
using GameManager;  // 🔥 TurnManager
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;
using WorldOfTheThreeKingdoms.Resources;
using WorldOfTheThreeKingdoms.GameManager;  // 🔥 YearlyRecommendationManager
//using GameObjects.PersonDetail.PersonMessages;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen : Screen
    {
        private bool AfterDayPassed(GameTime gameTime)
        {
            return this.RunTheFactions(gameTime);
        }

        private bool AfterDayStarting(GameTime gameTime)
        {
            bool enableAIAuthorityPhase1 =
                Session.GlobalVariables != null &&
                Session.GlobalVariables.EnableAIAuthorityPhase1;

            if (enableAIAuthorityPhase1 && Session.Current?.Scenario != null)
            {
                var authorityContext = Session.Current.Scenario.EnsureAIAuthorityContext();
                authorityContext.BeginLogicFrame(Session.Current.Scenario);
            }

            // 🔥 2026-03-23 灰度开关：CommandBufferScheduler vs 旧系统
            // 策略：同一帧只允许一个调度器落盘，避免双写
            bool useCommandBufferScheduler =
                (enableAIAuthorityPhase1 || Session.GlobalVariables.EnableCommandBufferScheduler) &&
                Session.Current?.CommandBufferScheduler != null &&
                Session.Current.CommandBufferScheduler.HasValidBuffer;
            
            if (useCommandBufferScheduler)
            {
                // 使用新调度器
                return this.MoveTheTroopsWithCommandBuffer(gameTime);
            }
            else
            {
                // 使用旧系统
                bool result = this.MoveTheTroops(gameTime);
                
                // 🔥 2026-03-16 阶段 3.1：并行运行 WegoEngine（测试模式）
                // 仅在开关打开时运行，不修改游戏状态，只记录对比日志
                if (Session.GlobalVariables.EnableWegoEngine && Session.Current?.WegoEngine != null)
                {
                    try
                    {
                        Session.Current.WegoEngine.Update();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AfterDayStarting] ❌ WegoEngine 异常: {ex.Message}");
                    }
                }
                
                return result;
            }
        }
        
        // 🔥 2026-03-23 新增：使用 CommandBufferScheduler 的移动逻辑
        private bool MoveTheTroopsWithCommandBuffer(GameTime gameTime)
        {
            if (!Session.Current.Scenario.Threading)
            {
                bool isPlayerControlling = Session.Current.Scenario.CurrentPlayer != null && 
                                           Session.Current.Scenario.CurrentPlayer.Controlling;
                
                if (!isPlayerControlling)
                {
                    // 🔥 关键：调用新调度器的逐帧更新
                    bool stillRunning = Session.Current.CommandBufferScheduler.UpdateFrame(gameTime, Session.Current.Scenario);
                    
                    if (!stillRunning)
                    {
                        // 执行完毕
                        return false;
                    }
                }
            }
            
            return true;
        }


        // ==================== 新事件系统处理器（ScenarioEvents） ====================
        
        /// <summary>
        /// 日事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnDayPassed(GameScenario scenario)
        {
            try
            {
                // 🔥 注意：阻塞逻辑已移至 GameDate.EndRunning()
                // 这里只处理 UI 更新和自动存档
                
                this.gengxinyoucelan();
                
                // 自动存档检查
                cundangShijianJiange = scenario.DaySince - shangciCundangShijian;
                if (cundangShijianJiange >= Setting.Current.GlobalVariables.AutoSaveFrequency)
                {
                    if (Setting.Current.GlobalVariables.doAutoSave)
                    {
                        scenario.needAutoSave = true;
                    }
                    shangciCundangShijian = scenario.DaySince;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scenario_OnDayPassed] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 月事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnMonthPassed(GameScenario scenario)
        {
            try
            {
                // 🎯 优化：每年2月自动清理年度推荐缓存
                if (scenario.Date.Month == 2)
                {
                    YearlyRecommendationManager.Instance.ClearYearlyRecords();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scenario_OnMonthPassed] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 季节事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnSeasonPassed(GameScenario scenario)
        {
            try
            {
                // 季节变化时切换音乐
                if (scenario.CurrentPlayer == null || scenario.CurrentPlayer.BattleState == ZhandouZhuangtai.和平)
                {
                    this.SwichMusic(scenario.Date.Season);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scenario_OnSeasonPassed] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 年事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnYearPassed(GameScenario scenario)
        {
            try
            {
                // 🔥 关键修复：恢复玩家势力推荐
                if (scenario.CurrentPlayer != null)
                {
                    YearlyRecommendationManager.Instance.OnTurnStart(scenario.CurrentPlayer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scenario_OnYearPassed] 异常: {ex.Message}");
            }
        }
        
        // ==================== 旧事件系统处理器（保留用于兼容） ====================

        private bool Date_OnDayPassed()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] 被调用，Threading={Session.Current.Scenario.Threading}");
                
                // ----------------------------------------------------------------
                // 正常的推进逻辑（状态为 false 时允许通过）
                if (!Session.Current.Scenario.Threading)
                {
                    // 🔥 Critical Fix: Check if CurrentPlayer has passed.
                    // Even if Threading is false (AI done), we must NOT advance the day if the player is currently controlling and hasn't passed the turn.
                    if (Session.Current.Scenario.CurrentPlayer != null && !Session.Current.Scenario.CurrentPlayer.Passed)
                    {
                        // 🔥 Deadlock Fix: If player hasn't passed but also doesn't have control, they can never pass.
                        // This creates an infinite loop where the date system waits for the player, but the player is disabled.
                        // We must force-grant control here to break the deadlock.
                        if (!Session.Current.Scenario.CurrentPlayer.Controlling)
                        {
                             // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] 玩家未Passed且未Controlling，强制设置Controlling=true: {Session.Current.Scenario.CurrentPlayer.Name}");
                             Session.Current.Scenario.CurrentPlayer.Controlling = true;
                             // We return false to allow the UI to refresh in the next frame with control enabled.
                             return false;
                        }

                         // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] 玩家未Passed，阻止推进: {Session.Current.Scenario.CurrentPlayer.Name}, Controlling={Session.Current.Scenario.CurrentPlayer.Controlling}");
                         return false;
                    }

                    // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] 执行DayPassedEvent");
                    Session.Current.Scenario.DayPassedEvent();
                    //Session.Current.Scenario.CheckRepeatedPerson(); // 原有注释保持不动
                    //this.Plugins.AirViewPlugin.ReloadTroopView(); // 原有注释保持不动

                    this.gengxinyoucelan();
                    //this.DrawAutoSavePicture(); // 原有注释保持不动

                    cundangShijianJiange = Session.Current.Scenario.DaySince - shangciCundangShijian;
                    if (cundangShijianJiange >= Setting.Current.GlobalVariables.AutoSaveFrequency)
                    {
                        if (Setting.Current.GlobalVariables.doAutoSave)
                        {
                            Session.Current.Scenario.needAutoSave = true;
                        }
                        shangciCundangShijian = Session.Current.Scenario.DaySince;
                    }
                    
                    // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] 成功，返回true");
                    return true;
                }
                
                // 还在忙，返回 false 阻止日期推进
                // System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] Threading=true，返回false");
                return false;
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine($"[Date_OnDayPassed] DayStartingEvent异常: {ex2.Message}");
            }
            return true;
        }

        /// <summary>
        /// 每日开始前事件处理器（新事件系统）
        /// </summary>
        private bool Scenario_OnDayStarting(GameScenario scenario)
        {
            try
            {
                if (!scenario.Threading)
                {
                    Session.Current.OnTurnStart(); // 创建异步寻路的地图快照
                    scenario.DayStartingEvent();
                    
                    // 🔥 新增：触发回合开始事件（包括军师风险扫描）
                    if (scenario.CurrentPlayer != null)
                    {
                        var turnManager = new TurnManager();
                        turnManager.OnTurnStart(scenario.CurrentPlayer);
                    }
                    
                    return true;
                }
                else
                {
                    // 检查是否所有AI都已完成
                    bool allAiDone = true;
                    foreach (Faction faction in scenario.Factions)
                    {
                        if (faction.Controlling && !faction.Passed)
                        {
                            allAiDone = false;
                            break;
                        }
                    }

                    if (allAiDone)
                    {
                        scenario.Threading = false;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scenario_OnDayStarting] 异常: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 每月开始前事件处理器（新事件系统）
        /// </summary>
        private bool Scenario_OnMonthStarting(GameScenario scenario)
        {
            if (!scenario.Threading)
            {
                // 月初逻辑（如果需要）
                return true;
            }
            return false;
        }

        private bool Date_OnMonthPassed()
        {
            if (!Session.Current.Scenario.Threading)
            {
                Session.Current.Scenario.MonthPassedEvent();
                
                // 🎯 优化：每年2月自动清理年度推荐缓存
                if (Session.Current.Scenario.Date.Month == 2)
                {
                    WorldOfTheThreeKingdoms.GameManager.YearlyRecommendationManager.Instance.ClearYearlyRecords();
                }
                
                return true;
            }
            return false;
        }

        public override void SwichMusic(GameSeason season)
        {
            if (Session.GlobalVariables.PlayMusic)
            {
                try
                {
                    if (Session.Current.Scenario.CurrentPlayer != null && Session.Current.Scenario.CurrentPlayer.BattleState != ZhandouZhuangtai.和平)
                    {
                        // 使用新的AudioManager播放战斗音乐
                        AudioManager.Instance?.PlayBattleMusic(Session.Current.Scenario.CurrentPlayer.BattleState);
                    }
                    else
                    {
                        // 使用新的AudioManager播放季节音乐
                        AudioManager.Instance?.PlaySeasonMusic(season);
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 忽略COM异常
                }
            }
            else
            {
                AudioManager.Instance?.StopMusic();
            }
        }

        /// <summary>
        /// 季节变化事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnSeasonChanged(GameScenario scenario, GameSeason newSeason)
        {
            // 🔥 修复读档崩溃：读档期间 scenario 的属性可能未初始化
            // 原因：ProcessScenarioData 期间事件触发时 scenario 还未完全初始化
            // 日期：2026-03-16
            if (scenario == null || scenario.Date == null || scenario.Parameters == null) return;
            
            if (scenario.CurrentPlayer == null || scenario.CurrentPlayer.BattleState == ZhandouZhuangtai.和平)
            {
                this.SwichMusic(newSeason);
            }
            if (!scenario.Threading && scenario.Date.Day <= scenario.Parameters.DayInTurn)
            {
                scenario.SeasonChangeEvent();
            }
        }

        private bool Date_OnYearPassed()
        {
            if (!Session.Current.Scenario.Threading)
            {
                Session.Current.Scenario.YearPassedEvent();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 每年开始前事件处理器（新事件系统）
        /// </summary>
        private bool Scenario_OnYearStarting(GameScenario scenario)
        {
            if (!scenario.Threading)
            {
                scenario.YearStartingEvent();

                // 🔥 关键修复：恢复玩家势力推荐，但避免与MainGameScreen重复UI显示
                // 触发年度人才举荐
                if (scenario.CurrentPlayer != null)
                {
                    WorldOfTheThreeKingdoms.GameManager.YearlyRecommendationManager.Instance.OnTurnStart(scenario.CurrentPlayer);
                }
                
                return true;
            }
            return false;
        }

        public  void DateGo(int Days)
        {
            if (Session.Current.Scenario.CurrentPlayer != null)
            {
                Session.Current.Scenario.CurrentPlayer.Passed = true;
                if (Session.Current.Scenario.IsLastPlayer(Session.Current.Scenario.CurrentPlayer))
                {
                    this.Plugins.DateRunnerPlugin.RunDays(Days);
                }
            } else 
            if (Session.Current.Scenario.PlayerFactions.Count == 0)
            {
                this.Plugins.DateRunnerPlugin.RunDays(Days);
            }
        }

    }
}
