// 军师对话简单测试 - 完全模拟部队事件的实现方式

using System;
using System.Collections.Generic;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace GameManager
{
    /// <summary>
    /// 军师对话简单测试 - 模拟部队事件实现
    /// </summary>
    public static class AdvisorDialogueSimpleTest
    {
        /// <summary>
        /// 完全模拟部队事件的对话实现
        /// </summary>
        public static void TestTroopEventStyleDialogue()
        {
            try
            {
                var mainScreen = Session.MainGame?.CurrentScreen as MainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin == null)
                {
                    Console.WriteLine("❌ 无法获取tupianwenziPlugin");
                    return;
                }

                var currentFaction = Session.Current?.Scenario?.CurrentFaction;
                if (currentFaction?.Leader == null)
                {
                    Console.WriteLine("❌ 无法获取当前势力或君主");
                    return;
                }

                var candidates = currentFaction.AdvisorCandicate;
                if (candidates == null || candidates.Count == 0)
                {
                    Console.WriteLine("❌ 没有可用的军师候选人");
                    return;
                }

                var leader = currentFaction.Leader;
                var candidate = candidates[0];

                Console.WriteLine($"测试部队事件风格对话：{leader.Name} 与 {candidate.Name}");

                // 创建PersonDialog列表（完全模拟部队事件）
                var dialogList = new List<PersonDialog>
                {
                    new PersonDialog
                    {
                        SpeakingPersonID = leader.ID,
                        SpeakingPerson = leader,
                        Text = "今欲请足下担任军师一职，不知尊意如何？"
                    },
                    new PersonDialog
                    {
                        SpeakingPersonID = candidate.ID,
                        SpeakingPerson = candidate,
                        Text = "承蒙主公错爱，属下定当竭尽所能。"
                    }
                };

                // 完全按照部队事件的方式处理
                if (dialogList.Count > 0)
                {
                    // 1. 设置位置
                    mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                    
                    // 2. foreach循环加入所有对话
                    foreach (PersonDialog dialog in dialogList)
                    {
                        if (dialog.SpeakingPerson != null)
                        {
                            mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                                dialog.SpeakingPerson, null, dialog.Text, "AppointAdvisor.jpg", "", "");
                        }
                    }
                    
                    // 3. 检查DialogShowTime并设置显示
                    if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                    {
                        mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                            Console.WriteLine("✅ 部队事件风格对话完成");
                            // 这里可以添加任命逻辑
                            currentFaction.AdvisorID = candidate.ID;
                            mainScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
                        }));
                        
                        // 4. 启动显示 - 使用IsShowing属性
                        mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                        Console.WriteLine("✅ 已启动部队事件风格对话测试");
                    }
                    else
                    {
                        Console.WriteLine("❌ DialogShowTime设置为0，无法显示对话");
                        // 直接执行逻辑
                        currentFaction.AdvisorID = candidate.ID;
                        mainScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试军师任命的完整流程
        /// </summary>
        public static void TestFullAdvisorAppointment()
        {
            try
            {
                var currentFaction = Session.Current?.Scenario?.CurrentFaction;
                if (currentFaction?.Leader == null)
                {
                    Console.WriteLine("❌ 无法获取当前势力或君主");
                    return;
                }

                var candidates = currentFaction.AdvisorCandicate;
                if (candidates == null || candidates.Count == 0)
                {
                    Console.WriteLine("❌ 没有可用的军师候选人");
                    return;
                }

                var leader = currentFaction.Leader;
                var candidate = candidates[0];

                Console.WriteLine($"测试完整军师任命流程：{leader.Name} 任命 {candidate.Name}");

                // 使用AdvisorAppointmentSystem进行任命
                bool success = AdvisorAppointmentSystem.TryAppointAdvisor(leader, candidate, currentFaction);
                
                Console.WriteLine($"任命结果: {(success ? "成功" : "失败")}");
                Console.WriteLine($"当前军师: {currentFaction.AdvisorName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== 开始军师对话简单测试 ===\n");

            Console.WriteLine("1. 测试部队事件风格对话:");
            TestTroopEventStyleDialogue();

            Console.WriteLine("\n2. 测试完整军师任命流程:");
            TestFullAdvisorAppointment();

            Console.WriteLine("\n=== 测试完成 ===");
        }
    }
}

// 使用方法：
// 在游戏中调用 AdvisorDialogueSimpleTest.RunAllTests() 来测试对话系统