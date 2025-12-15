// 军师对话测试方法
// 用于验证轮流对话是否正常工作

using System;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace GameManager
{
    /// <summary>
    /// 军师对话测试类 - 简化版本
    /// </summary>
    public static class AdvisorDialogueTest
    {
        /// <summary>
        /// 测试简单的轮流对话
        /// </summary>
        public static void TestSimpleAlternatingDialogue()
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

                Console.WriteLine($"测试轮流对话：{leader.Name} 与 {candidate.Name}");

                // 设置对话位置
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);

                // 第一个对话：君主说话
                string leaderText = $"{leader.Name}：今欲请足下担任军师一职，不知尊意如何？";
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    leader, leader, leaderText, "AppointAdvisor.jpg", "", "");

                // 第二个对话：军师回应
                string advisorText = $"{candidate.Name}：承蒙主公错爱，属下定当竭尽所能。";
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    candidate, candidate, advisorText, "AppointAdvisor.jpg", "", "");

                // 开始显示对话
                if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                {
                    var plugin = mainScreen.Plugins.tupianwenziPlugin as WorldOfTheThreeKingdoms.GamePlugins.tupianwenziPlugin.tupianwenziPlugin;
                    plugin.tupianwenzi.SetIsShowing(mainScreen, true);
                    Console.WriteLine("✅ 已启动轮流对话测试");
                }
                else
                {
                    Console.WriteLine("❌ DialogShowTime设置为0，无法显示对话");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试事件系统的对话方式
        /// </summary>
        public static void TestEventStyleDialogue()
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

                Console.WriteLine($"测试事件风格对话：{leader.Name} 与 {candidate.Name}");

                // 创建PersonDialog列表（模拟事件系统）
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

                // 设置对话位置
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);

                // 模拟事件系统的foreach循环
                foreach (PersonDialog dialog in dialogList)
                {
                    if (dialog.SpeakingPerson != null)
                    {
                        mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            dialog.SpeakingPerson, null, dialog.Text, "AppointAdvisor.jpg", "", "");
                    }
                }

                // 开始显示对话（模拟事件系统）
                if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                {
                    mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                        Console.WriteLine("✅ 事件风格对话完成");
                    }));

                    var plugin = mainScreen.Plugins.tupianwenziPlugin as WorldOfTheThreeKingdoms.GamePlugins.tupianwenziPlugin.tupianwenziPlugin;
                    plugin.tupianwenzi.SetIsShowing(mainScreen, true);
                    Console.WriteLine("✅ 已启动事件风格对话测试");
                }
                else
                {
                    Console.WriteLine("❌ DialogShowTime设置为0，无法显示对话");
                }
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
            Console.WriteLine("=== 开始军师对话测试 ===\n");

            Console.WriteLine("1. 测试简单轮流对话:");
            TestSimpleAlternatingDialogue();

            Console.WriteLine("\n2. 测试事件风格对话:");
            TestEventStyleDialogue();

            Console.WriteLine("\n=== 测试完成 ===");
        }
    }
}

// 使用方法：
// 在游戏中调用 AdvisorDialogueTest.RunAllTests() 来测试对话系统