using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameGlobal;
using GameManager;

namespace GameObjects
{
    /// <summary>
    /// 对话管理器 - 统一管理游戏中的对话系统
    /// </summary>
    public class DialogueManager
    {
        private static DialogueConfig appointmentConfig;
        private static DialogueConfig recallConfig;
        private static DialogueConfig refusalConfig;

        /// <summary>
        /// 初始化加载所有对话配置
        /// </summary>
        public static void Initialize()
        {
            LoadAppointmentConfig();
            LoadRecallConfig();
            LoadRefusalConfig();
        }

        /// <summary>
        /// 加载任命对话配置
        /// </summary>
        public static void LoadAppointmentConfig()
        {
            LoadConfig("Content/Data/AppointmentDialogues.xml", ref appointmentConfig);
        }

        /// <summary>
        /// 加载罢免对话配置
        /// </summary>
        public static void LoadRecallConfig()
        {
            LoadConfig("Content/Data/RecallDialogues.xml", ref recallConfig);
        }

        /// <summary>
        /// 加载拒绝对话配置
        /// </summary>
        public static void LoadRefusalConfig()
        {
            LoadConfig("Content/Data/RefusalDialogues.xml", ref refusalConfig);
        }

        /// <summary>
        /// 从XML文件加载对话配置
        /// </summary>
        private static void LoadConfig(string xmlPath, ref DialogueConfig config)
        {
            try
            {
                if (File.Exists(xmlPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DialogueConfig));
                    using (FileStream fs = new FileStream(xmlPath, FileMode.Open))
                    {
                        config = (DialogueConfig)serializer.Deserialize(fs);
                        System.Diagnostics.Debug.WriteLine($"[DialogueManager] 成功加载配置: {xmlPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueManager] 配置文件不存在: {xmlPath}，使用默认配置");
                    config = new DialogueConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueManager] 加载配置失败: {xmlPath}, 错误: {ex.Message}");
                config = new DialogueConfig(); // 使用空配置作为回退
            }
        }

        /// <summary>
        /// 获取任命军师对话
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <param name="isRefusal">是否为拒绝对话</param>
        /// <returns>对话条目</returns>
        public static DialogueEntry GetAppointDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            if (isRefusal)
            {
                if (refusalConfig == null)
                {
                    LoadRefusalConfig();
                }
                return GetBestMatchDialogue(refusalConfig, leader, advisor, true) ?? GetFallbackRefusalDialogue();
            }
            else
            {
                if (appointmentConfig == null)
                {
                    LoadAppointmentConfig();
                }
                return GetBestMatchDialogue(appointmentConfig, leader, advisor, false) ?? GetFallbackAppointDialogue();
            }
        }

        /// <summary>
        /// 通用对话获取方法（支持拒绝过滤）
        /// </summary>
        public static DialogueEntry GetDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            return GetAppointDialogue(leader, advisor, isRefusal);
        }

        /// <summary>
        /// 获取罢免军师对话
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">被罢免的军师</param>
        /// <returns>对话条目</returns>
        public static DialogueEntry GetRecallDialogue(Person leader, Person advisor)
        {
            if (recallConfig == null)
            {
                LoadRecallConfig();
            }

            return GetBestMatchDialogue(recallConfig, leader, advisor, false) ?? GetFallbackRecallDialogue();
        }

        /// <summary>
        /// 从配置中找到最佳匹配的对话
        /// </summary>
        private static DialogueEntry GetBestMatchDialogue(DialogueConfig config, Person leader, Person advisor, bool isRefusal = false)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return null;

            // 根据是否拒绝过滤对话类型
            var filteredEntries = config.Entries.Where(entry => 
                isRefusal ? entry.Type == DialogueType.Refusal 
                         : entry.Type != DialogueType.Refusal);

            // 核心逻辑：遍历过滤后的条目，找到匹配且权重最高的
            var bestMatch = filteredEntries
                .Select(entry => new { Entry = entry, Score = entry.GetMatchScore(leader, advisor) })
                .Where(x => x.Score > 0) // 过滤掉不匹配的
                .OrderByDescending(x => x.Score) // 分数高的排前面
                .FirstOrDefault();

            return bestMatch?.Entry;
        }

        /// <summary>
        /// 获取默认任命对话
        /// </summary>
        private static DialogueEntry GetFallbackAppointDialogue()
        {
            return new DialogueEntry
            {
                LeaderText = "今欲请足下担任军师一职，不知尊意如何？",
                AdvisorText = "承蒙主公错爱，属下定当竭尽所能。"
            };
        }

        /// <summary>
        /// 获取默认拒绝对话
        /// </summary>
        private static DialogueEntry GetFallbackRefusalDialogue()
        {
            return new DialogueEntry
            {
                Type = DialogueType.Refusal,
                LeaderText = "请你担任军师一职。",
                AdvisorText = "抱歉，某不能接受这个职位。"
            };
        }

        /// <summary>
        /// 获取默认罢免对话
        /// </summary>
        private static DialogueEntry GetFallbackRecallDialogue()
        {
            return new DialogueEntry
            {
                LeaderText = "军师之职暂且卸任，望你理解。",
                AdvisorText = "是，属下遵命。"
            };
        }

        /// <summary>
        /// 显示任命军师对话序列
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <param name="gameScreen">游戏屏幕</param>
        public static void ShowAppointDialogue(Person leader, Person advisor, WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            DialogueEntry dialogue = GetAppointDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 合并对话内容显示
            string combinedDialogue = $"{leader.Name}：「{dialogue.LeaderText}」\n\n{advisor.Name}：「{dialogue.AdvisorText}」";

            // 使用游戏的文本显示系统
            gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(leader, null, combinedDialogue, "AppointAdvisor.jpg", "", "");
            gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;

            // 设置关闭回调，执行游戏记录
            gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                gameScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
            }));
        }

        /// <summary>
        /// 显示罢免军师对话序列
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">被罢免的军师</param>
        /// <param name="gameScreen">游戏屏幕</param>
        public static void ShowRecallDialogue(Person leader, Person advisor, WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            DialogueEntry dialogue = GetRecallDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 合并对话内容显示
            string combinedDialogue = $"{leader.Name}：「{dialogue.LeaderText}」\n\n{advisor.Name}：「{dialogue.AdvisorText}」";

            // 使用游戏的文本显示系统
            gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(leader, null, combinedDialogue, "RecallAdvisor.jpg", "", "");
            gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 重新加载所有配置（用于热更新）
        /// </summary>
        public static void ReloadConfigs()
        {
            appointmentConfig = null;
            recallConfig = null;
            refusalConfig = null;
            Initialize();
            System.Diagnostics.Debug.WriteLine("[DialogueManager] 配置已重新加载");
        }

        /// <summary>
        /// 获取配置统计信息
        /// </summary>
        public static string GetConfigStats()
        {
            int appointmentCount = appointmentConfig?.Entries?.Count ?? 0;
            int recallCount = recallConfig?.Entries?.Count ?? 0;
            int refusalCount = refusalConfig?.Entries?.Count ?? 0;
            return $"任命对话: {appointmentCount} 条, 罢免对话: {recallCount} 条, 拒绝对话: {refusalCount} 条";
        }
    }
}