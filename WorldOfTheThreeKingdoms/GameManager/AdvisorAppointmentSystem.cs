using System;
using GameObjects;
using GameGlobal;
using WorldOfTheThreeKingdoms;
using PluginInterface;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师任命系统 - 处理任命逻辑和拒绝判定
    /// </summary>
    public static class AdvisorAppointmentSystem
    {
        /// <summary>
        /// 尝试任命军师
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="candidate">候选军师</param>
        /// <param name="faction">势力</param>
        /// <returns>是否任命成功</returns>
        public static bool TryAppointAdvisor(Person leader, Person candidate, Faction faction)
        {
            if (leader == null || candidate == null || faction == null)
                return false;

            bool isSuccess = true;

            // 1. 判定逻辑：是否拒绝？
            // 如果是厌恶关系，必定拒绝
            if (candidate.CheckRelation(leader) == -1)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 厌恶 {leader.Name}，拒绝任命");
            }
            // 忠诚度极低也拒绝
            else if (candidate.Loyalty < 20)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 忠诚度过低({candidate.Loyalty})，拒绝任命");
            }
            // 高野心 + 低智力的组合可能拒绝（觉得自己被低估）
            else if (candidate.Ambition > 80 && candidate.Intelligence < 70)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 高野心但智力不足，拒绝任命");
            }
            // 性格严重不合也可能拒绝
            else if (IsPersonalityConflict(leader, candidate))
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 与 {leader.Name} 性格不合，拒绝任命");
            }

            // 2. 根据结果获取对话
            AdvisorDialogueEntry dialogue;
            if (isSuccess)
            {
                // 成功：获取成功任命对话
                dialogue = AdvisorAppointmentDialogueManager.GetAppointDialogue(leader, candidate, isRefusal: false);
                
                // 执行任命
                faction.AdvisorID = candidate.ID;
                System.Diagnostics.Debug.WriteLine($"[任命成功] {leader.Name} 任命 {candidate.Name} 为军师");
            }
            else
            {
                // 失败：获取拒绝对话
                dialogue = AdvisorAppointmentDialogueManager.GetAppointDialogue(leader, candidate, isRefusal: true);
                System.Diagnostics.Debug.WriteLine($"[任命失败] {candidate.Name} 拒绝了 {leader.Name} 的任命");
            }

            // 3. 显示对话UI
            ShowAppointmentDialogue(leader, candidate, dialogue, isSuccess);

            return isSuccess;
        }

        /// <summary>
        /// 检查性格是否严重冲突
        /// </summary>
        private static bool IsPersonalityConflict(Person leader, Person candidate)
        {
            // 霸道君主 vs 仁德军师
            if (leader.Character?.ID == 1 && candidate.Character?.ID == 0)
                return true;
            
            // 狡诈君主 vs 仁德军师
            if (leader.Character?.ID == 4 && candidate.Character?.ID == 0)
                return true;
            
            // 莽撞君主 vs 冷静军师（高智力时）
            if (leader.Character?.ID == 3 && candidate.Character?.ID == 2 && candidate.Intelligence > 85)
                return true;

            return false;
        }

        /// <summary>
        /// 显示任命对话 - 使用对话队列实现轮流对话
        /// </summary>
        private static void ShowAppointmentDialogue(Person leader, Person candidate, AdvisorDialogueEntry dialogue, bool isSuccess)
        {
            try
            {
                // 调试输出
                string resultText = isSuccess ? "任命成功" : "任命被拒绝";
                System.Diagnostics.Debug.WriteLine($"[{resultText}对话] {leader.Name}: {dialogue.LeaderText}");
                System.Diagnostics.Debug.WriteLine($"[{resultText}对话] {candidate.Name}: {dialogue.AdvisorText}");

                // 获取MainGameScreen实例
                var mainScreen = Session.MainGame?.mainGameScreen as WorldOfTheThreeKingdoms.GameScreens.MainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin != null)
                {
                    string imageName = ""; // 不使用图片，避免找不到文件
                    
                    // 设置对话位置
                    mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                    System.Diagnostics.Debug.WriteLine("[轮流对话] 已设置对话位置");
                    
                    // 第一个对话：君主说话（加入队列）
                    System.Diagnostics.Debug.WriteLine($"[轮流对话] 加入君主对话: {dialogue.LeaderText}");
                    mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        leader, leader, dialogue.LeaderText, imageName, "", "");
                    
                    // 第二个对话：军师回应（加入队列）
                    System.Diagnostics.Debug.WriteLine($"[轮流对话] 加入军师对话: {dialogue.AdvisorText}");
                    mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        candidate, candidate, dialogue.AdvisorText, imageName, "", "");
                    
                    // 检查DialogShowTime设置
                    System.Diagnostics.Debug.WriteLine($"[轮流对话] DialogShowTime = {Setting.Current.GlobalVariables.DialogShowTime}");
                    
                    // 在所有对话加入队列后，设置关闭回调和开始显示
                    if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                    {
                        // 设置关闭回调
                        if (isSuccess)
                        {
                            mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                                mainScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
                                System.Diagnostics.Debug.WriteLine("[轮流对话] 对话完成，记录任命事件");
                            }));
                        }
                        else
                        {
                            mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                                System.Diagnostics.Debug.WriteLine("[轮流对话] 拒绝对话完成");
                            }));
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[轮流对话] 已设置关闭回调");
                        
                        // 开始显示对话 - 使用正确的方法
                        try
                        {
                            var tupianwenziPluginInstance = mainScreen.Plugins.tupianwenziPlugin as tupianwenziPlugin.tupianwenziPlugin;
                            if (tupianwenziPluginInstance?.tupianwenzi != null)
                            {
                                tupianwenziPluginInstance.tupianwenzi.SetIsShowing(mainScreen, true);
                                System.Diagnostics.Debug.WriteLine("[轮流对话] 已启动对话显示 (使用SetIsShowing方法)");
                            }
                            else
                            {
                                // 备用方法
                                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                                System.Diagnostics.Debug.WriteLine("[轮流对话] 已启动对话显示 (使用IsShowing属性)");
                            }
                        }
                        catch (Exception showEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[轮流对话] 启动显示失败: {showEx.Message}");
                            // 备用方法
                            mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[轮流对话] DialogShowTime为0，直接执行回调");
                        // 如果DialogShowTime为0，直接执行回调
                        if (isSuccess)
                        {
                            mainScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[轮流对话] 对话处理完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAppointmentDialogue] 无法获取游戏屏幕或插件，跳过UI显示");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAppointmentDialogue] 显示对话时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowAppointmentDialogue] 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查候选人是否会拒绝任命
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="candidate">候选军师</param>
        /// <returns>true=会拒绝, false=会接受</returns>
        public static bool WillRefuseAppointment(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return true;

            // 厌恶关系必定拒绝
            if (candidate.CheckRelation(leader) == -1) return true;
            
            // 忠诚度极低拒绝
            if (candidate.Loyalty < 20) return true;
            
            // 高野心 + 低智力拒绝
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return true;
            
            // 性格严重冲突拒绝
            if (IsPersonalityConflict(leader, candidate)) return true;

            return false;
        }

        /// <summary>
        /// 获取拒绝原因描述
        /// </summary>
        public static string GetRefusalReason(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return "参数错误";

            if (candidate.CheckRelation(leader) == -1) return "厌恶关系";
            if (candidate.Loyalty < 20) return "忠诚度过低";
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return "高野心低智力";
            if (IsPersonalityConflict(leader, candidate)) return "性格冲突";

            return "无拒绝原因";
        }
    }
}