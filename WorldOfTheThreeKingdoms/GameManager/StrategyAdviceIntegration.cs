using System;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms;

// 使用别名避免命名冲突
using Session = GameManager.Session;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师策略建议系统集成类 - 提供简单易用的接口
    /// </summary>
    public static class StrategyAdviceIntegration
    {
        /// <summary>
        /// 显示流言策略建议
        /// </summary>
        public static void ShowGossipAdvice(Person targetEnemy)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetEnemy, StrategyKind.Gossip);
            ShowStrategyAdvice("流言策略", advice);
        }

        /// <summary>
        /// 显示放火策略建议
        /// </summary>
        public static void ShowArsonAdvice(Architecture targetCity)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetCity, StrategyKind.Arson);
            ShowStrategyAdvice("放火策略", advice);
        }

        /// <summary>
        /// 显示破坏策略建议
        /// </summary>
        public static void ShowDestructionAdvice(Architecture targetCity)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetCity, StrategyKind.Destruction);
            ShowStrategyAdvice("破坏策略", advice);
        }

        /// <summary>
        /// 显示离间策略建议
        /// </summary>
        public static void ShowInstigateAdvice(Person targetEnemy)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetEnemy, StrategyKind.Instigate);
            ShowStrategyAdvice("离间策略", advice);
        }

        /// <summary>
        /// 显示结盟策略建议
        /// </summary>
        public static void ShowAllianceAdvice(Faction targetFaction)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            // 对于结盟，目标是势力的君主
            var targetLeader = targetFaction?.Leader;
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetLeader, StrategyKind.Alliance);
            ShowStrategyAdvice("结盟策略", advice);
        }

        /// <summary>
        /// 显示搜索策略建议
        /// </summary>
        public static void ShowSearchAdvice(Architecture targetArea = null)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetArea, StrategyKind.Search);
            ShowStrategyAdvice("搜索策略", advice);
        }

        /// <summary>
        /// 通用策略建议显示方法
        /// </summary>
        public static void ShowStrategyAdvice(StrategyKind strategy, object target = null)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, target, strategy);
            string strategyName = AdvisorStrategySystem.GetStrategyName(strategy);
            ShowStrategyAdvice(strategyName + "策略", advice);
        }

        /// <summary>
        /// 显示策略建议的核心方法
        /// </summary>
        private static void ShowStrategyAdvice(string title, StrategyAdviceResult advice)
        {
            string content = advice.AdvisorComment;

            // 如果有推荐人选，添加额外信息
            if (advice.BestCandidate != null)
            {
                content += $"\n\n推荐执行人：{advice.BestCandidate.Name}";
                content += $"\n预测成功率：{advice.PredictedChance}%";
                
                // 显示推荐人选的关键属性
                var candidate = advice.BestCandidate;
                content += $"\n人选属性：智力{candidate.Intelligence} 魅力{candidate.Glamour} 统率{candidate.Command} 政治{candidate.Politics}";
            }

            ShowMessage(title, content);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[策略建议] {title}");
            System.Diagnostics.Debug.WriteLine($"[策略建议] 推荐人选: {advice.BestCandidate?.Name ?? "无"}");
            System.Diagnostics.Debug.WriteLine($"[策略建议] 预测成功率: {advice.PredictedChance}%");
        }

        /// <summary>
        /// 显示消息框 - 这里需要根据实际的UI系统进行调整
        /// </summary>
        private static void ShowMessage(string title, string content)
        {
            // 方案1：使用现有的对话系统
            try
            {
                var mainScreen = Session.MainGame?.mainGameScreen as WorldOfTheThreeKingdoms.GameScreens.MainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin != null)
                {
                    // 使用游戏内置的对话显示系统
                    var currentFaction = Session.Current.Scenario.CurrentPlayer;
                    var advisor = currentFaction?.Advisor;
                    
                    if (advisor != null)
                    {
                        mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Center, mainScreen);
                        mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            advisor, advisor, content, "StrategyAdvice.jpg", "", "");
                        mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                }
                else
                {
                    // 方案2：控制台输出（调试用）
                    System.Diagnostics.Debug.WriteLine($"=== {title} ===");
                    System.Diagnostics.Debug.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                // 方案3：异常时的备用输出
                System.Diagnostics.Debug.WriteLine($"[策略建议显示异常] {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"=== {title} ===");
                System.Diagnostics.Debug.WriteLine(content);
            }
        }

        /// <summary>
        /// 获取当前势力的军师建议能力评估
        /// </summary>
        public static string GetAdvisorCapabilityAssessment()
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                return "当前无军师，无法提供策略建议";
            }

            string assessment = $"当前军师：{advisor.Name}\n";
            assessment += $"智力：{advisor.Intelligence} ";
            
            if (advisor.Intelligence >= 90)
                assessment += "(神机妙算)";
            else if (advisor.Intelligence >= 80)
                assessment += "(智谋过人)";
            else if (advisor.Intelligence >= 70)
                assessment += "(颇有智慧)";
            else if (advisor.Intelligence >= 60)
                assessment += "(尚可谋划)";
            else
                assessment += "(智力平庸)";

            assessment += $"\n建议准确度：约 {100 - (110 - advisor.Intelligence)}%";

            return assessment;
        }

        /// <summary>
        /// 批量获取所有策略的建议（用于策略规划界面）
        /// </summary>
        public static void ShowAllStrategyAdvice()
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            var advisor = faction?.Advisor;

            if (advisor == null)
            {
                ShowMessage("军师谏言", AdvisorDialogueManager.GetGenericDialogue(faction, null, "NoAdvisor", null));
                return;
            }

            string allAdvice = $"=== {advisor.Name}的策略建议总览 ===\n\n";

            // 遍历所有策略类型
            var strategies = Enum.GetValues<StrategyKind>();
            foreach (StrategyKind strategy in strategies)
            {
                var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, null, strategy);
                string strategyName = AdvisorStrategySystem.GetStrategyName(strategy);
                
                allAdvice += $"【{strategyName}】\n";
                if (advice.BestCandidate != null)
                {
                    allAdvice += $"推荐：{advice.BestCandidate.Name} (成功率约{advice.PredictedChance}%)\n";
                }
                else
                {
                    allAdvice += "暂无合适人选\n";
                }
                allAdvice += "\n";
            }

            ShowMessage("策略建议总览", allAdvice);
        }
    }

    /// <summary>
    /// 在MainGameScreen中的集成示例
    /// </summary>
    public static class MainGameScreenExtensions
    {
        /// <summary>
        /// 在右键菜单中添加策略建议选项的示例
        /// </summary>
        public static void AddStrategyAdviceToContextMenu()
        {
            // 这是一个示例，展示如何在游戏的右键菜单中集成策略建议
            // 实际实现需要根据游戏的菜单系统进行调整
            
            System.Diagnostics.Debug.WriteLine("[集成示例] 在右键菜单中添加策略建议选项");
            
            // 伪代码示例：
            // contextMenu.AddItem("军师建议", () => {
            //     StrategyAdviceIntegration.ShowAllStrategyAdvice();
            // });
        }

        /// <summary>
        /// 在策略执行前显示建议的示例
        /// </summary>
        public static bool ShowAdviceBeforeStrategy(StrategyKind strategy, object target)
        {
            // 在执行策略前显示军师建议
            StrategyAdviceIntegration.ShowStrategyAdvice(strategy, target);
            
            // 这里可以添加确认对话框，让玩家决定是否继续
            // 返回true表示继续执行，false表示取消
            return true;
        }
    }
}
