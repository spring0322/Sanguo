using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;
using tupianwenziPlugin;

namespace GameObjects
{
    public static class DialogueManager
    {
        public static DialogueConfig UnifiedConfig { get; private set; }

        /// <summary>
        /// 初始化加载所有对话配置
        /// </summary>
        public static void Initialize()
        {
            // 统一读取 AdvisorDialogue.xml
            UnifiedConfig = LoadConfig("Content/Data/Plugins/AdvisorDialogue.xml");
            
            if (UnifiedConfig == null)
            {
                UnifiedConfig = new DialogueConfig();
            }
        }

        /// <summary>
        /// 从XML文件加载对话配置
        /// </summary>
        private static DialogueConfig LoadConfig(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DialogueConfig));
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        DialogueConfig config = (DialogueConfig)serializer.Deserialize(stream);
                        System.Diagnostics.Debug.WriteLine($"[DialogueManager] 成功加载配置: {path}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueManager] 错误: 加载 {path} 失败 - {ex.Message}");
                    return null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueManager] 错误: 找不到配置文件 {path}");
                return null;
            }
        }

        public static DialogueEntry GetAppointDialogue(Person leader, Person advisor)
        {
            return GetBestMatchDialogue("Appointment", leader, advisor) ?? GetFallbackAppointDialogue();
        }

        public static DialogueEntry GetRecallDialogue(Person leader, Person advisor)
        {
            return GetBestMatchDialogue("Recall", leader, advisor) ?? GetFallbackRecallDialogue();
        }

        public static DialogueEntry GetRefusalDialogue(Person leader, Person advisor)
        {
            return GetBestMatchDialogue("Refusal", leader, advisor) ?? GetFallbackRefusalDialogue();
        }

        /// <summary>
        /// 获取军师举荐对话
        /// </summary>
        public static DialogueEntry GetRecommendationDialogue(Person leader, Person advisor)
        {
            return GetBestMatchDialogue("Recommendation", leader, advisor) ?? GetFallbackRecommendationDialogue();
        }

        /// <summary>
        /// 从指定分组中获取最匹配的对话
        /// </summary>
        private static DialogueEntry GetBestMatchDialogue(string groupType, Person leader, Person advisor)
        {
            if (UnifiedConfig == null) return null;

            var entries = UnifiedConfig.GetEntriesByType(groupType);
            if (entries == null || entries.Count == 0) return null;

            DialogueEntry bestMatch = null;
            int maxScore = -1;

            foreach (var entry in entries)
            {
                int score = entry.GetMatchScore(leader, advisor);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestMatch = entry;
                }
            }

            return bestMatch;
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
        /// 获取默认军师举荐对话
        /// </summary>
        private static DialogueEntry GetFallbackRecommendationDialogue()
        {
            return new DialogueEntry
            {
                LeaderText = "好！立即招募此人！",
                AdvisorText = "主公，臣发现有贤才在野，是否招募？"
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
        /// <param name="screen">游戏屏幕</param>
        public static void ShowAppointDialogue(Person leader, Person advisor, MainGameScreen screen)
        {
            DialogueEntry dialogue = GetAppointDialogue(leader, advisor);

            if (dialogue != null)
            {
                ShowDialogueQueue(leader, advisor, dialogue, "AppointAdvisor", screen);
            }
        }

        /// <summary>
        /// 显示罢免军师对话序列
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">被罢免的军师</param>
        /// <param name="screen">游戏屏幕</param>
        public static void ShowRecallDialogue(Person leader, Person advisor, MainGameScreen screen)
        {
            DialogueEntry dialogue = GetRecallDialogue(leader, advisor);

            if (dialogue != null)
            {
                ShowDialogueQueue(leader, advisor, dialogue, "RecallAdvisor", screen);
            }
        }

        /// <summary>
        /// 显示军师举荐对话序列（单对话框，支持确认）- 完全参考说服功能实现
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="screen">游戏屏幕</param>
        /// <param name="customMessage">自定义消息（可选）</param>
        public static void ShowRecommendationDialogue(Person leader, Person advisor, Action onConfirm, MainGameScreen screen, string customMessage = null)
        {
            if (screen?.Plugins?.tupianwenziPlugin == null) return;

            string message;
            if (!string.IsNullOrEmpty(customMessage))
            {
                // 使用自定义消息（通常是结果消息）
                message = customMessage;
            }
            else
            {
                // 使用默认确认消息
                DialogueEntry dialogue = GetRecommendationDialogue(leader, advisor);
                message = dialogue?.AdvisorText ?? "主公，臣发现有贤才在野，是否招募？";
                message = DialogueEntry.Replace(message, leader, advisor);
            }

            if (onConfirm != null)
            {
                // 1. 设置确认对话框的回调函数 - 完全参考说服功能
                screen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    screen.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() => onConfirm?.Invoke()),
                    new GameDelegates.VoidFunction(() => {
                        // 取消回调：什么都不做
                    })
                );

                // 2. 设置确认对话框位置
                screen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
            }

            // 3. 显示军师头像和对话 - 使用成功的5参数版本
            screen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                advisor,                           // 说话人
                advisor,                           // 对象
                "",                                // 空字符串 - 不使用分支名称
                "",                                // 图片 - 使用空字符串
                "",                                // 声音 - 使用空字符串
                message                            // 直接显示的文本
            );

            // 4. 显示对话框
            screen.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 使用对话队列显示轮流对话
        /// </summary>
        private static void ShowDialogueQueue(Person leader, Person advisor, DialogueEntry dialogue, string eventType, MainGameScreen screen)
        {
            if (dialogue == null || screen == null || screen.Plugins.tupianwenziPlugin == null) return;

            // 不使用背景图片，避免找不到文件导致的null texture错误
            string imageName = "";

            // 创建对话队列
            var dialogueQueue = new List<(Person speaker, string text)>();
            
            // 添加对话内容到队列
            if (!string.IsNullOrEmpty(dialogue.LeaderText))
            {
                dialogueQueue.Add((leader, dialogue.LeaderText));
            }
            if (!string.IsNullOrEmpty(dialogue.AdvisorText))
            {
                dialogueQueue.Add((advisor, dialogue.AdvisorText));
            }

            // 显示对话队列
            ShowDialogueSequence(dialogueQueue, imageName, screen);
        }

        /// <summary>
        /// 使用确认对话队列显示轮流对话（带确认/取消按钮）
        /// </summary>
        private static void ShowConfirmationDialogueQueue(Person leader, Person advisor, DialogueEntry dialogue, Action onConfirm, Action onCancel, MainGameScreen screen)
        {
            if (dialogue == null || screen == null || screen.Plugins.tupianwenziPlugin == null) return;

            // 不使用背景图片
            string imageName = "";

            // 设置确认对话框
            screen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                screen.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => onConfirm?.Invoke()),
                new GameDelegates.VoidFunction(() => onCancel?.Invoke())
            );
            screen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

            // 显示军师的对话内容
            screen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                advisor,
                advisor,
                "",
                "",
                imageName,
                dialogue.AdvisorText
            );
            screen.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示对话序列 - 修复轮流对话功能
        /// </summary>
        private static void ShowDialogueSequence(List<(Person speaker, string text)> dialogueQueue, string imageName, MainGameScreen screen)
        {
            if (dialogueQueue == null || dialogueQueue.Count == 0 || screen?.Plugins?.tupianwenziPlugin == null) return;

            // 🎯 修复：实现真正的轮流对话
            ShowDialogueRecursive(dialogueQueue, 0, imageName, screen);
        }

        /// <summary>
        /// 递归显示对话序列 - 修复双对话框问题
        /// </summary>
        private static void ShowDialogueRecursive(List<(Person speaker, string text)> dialogueQueue, int currentIndex, string imageName, MainGameScreen screen)
        {
            if (currentIndex >= dialogueQueue.Count || screen?.Plugins?.tupianwenziPlugin == null) return;

            var currentDialogue = dialogueQueue[currentIndex];
            
            // 🎯 修复：确保在设置新对话前，清空可能存在的队列
            if (currentIndex == 0)
            {
                // 只在第一个对话时清空队列
                var plugin = screen.Plugins.tupianwenziPlugin as tupianwenziPlugin.tupianwenziPlugin;
                if (plugin != null)
                {
                    while (plugin.tupianwenzi.DisplayQueue.Count > 0)
                    {
                        plugin.tupianwenzi.DisplayQueue.Dequeue();
                    }
                }
            }
            
            // 显示当前对话 - 修复头像显示问题
            System.Diagnostics.Debug.WriteLine($"[DialogueManager] 准备显示对话 - 说话人: {currentDialogue.speaker?.Name ?? "null"}, PictureIndex: {currentDialogue.speaker?.PictureIndex ?? -1}");
            screen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                currentDialogue.speaker,    // 说话人（显示头像的人物）
                currentDialogue.speaker,    // 对象
                currentDialogue.text,       // 对话文本
                imageName,                  // 背景图片
                "",                         // 音效
                ""                          // TryToShowString（右侧对话框文本，留空避免双对话框）
            );
            
            screen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, screen);

            // 设置关闭回调：显示下一个对话
            if (currentIndex < dialogueQueue.Count - 1)
            {
                // 还有下一个对话，设置回调继续显示
                screen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                    ShowDialogueRecursive(dialogueQueue, currentIndex + 1, imageName, screen);
                }));
            }
            else
            {
                // 最后一个对话，设置最终回调（如果需要的话）
                screen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                    // 对话结束，记录事件（如果是任命成功的话）
                    if (dialogueQueue.Count >= 2) // 有君主和军师的对话，说明是任命成功
                    {
                        screen.Plugins.GameRecordPlugin.AddBranch(dialogueQueue[0].speaker, "AppointAdvisor", dialogueQueue[0].speaker.Position);
                        System.Diagnostics.Debug.WriteLine("[轮流对话] 对话完成，记录任命事件");
                    }
                }));
            }
            
            // 🎯 修复：只在设置完所有回调后才显示
            screen.Plugins.tupianwenziPlugin.IsShowing = true;

            System.Diagnostics.Debug.WriteLine($"[轮流对话] 第{currentIndex + 1}轮 - 显示{currentDialogue.speaker.Name}: {currentDialogue.text}");
        }

        /// <summary>
        /// 显示任命被拒绝对话序列
        /// </summary>
        public static void ShowRefusalDialogue(Person leader, Person advisor, MainGameScreen screen)
        {
            DialogueEntry dialogue = GetRefusalDialogue(leader, advisor);

            if (dialogue != null)
            {
                ShowDialogueQueue(leader, advisor, dialogue, "RefusalAdvisor", screen);
            }
        }

        /// <summary>
        /// 重新加载所有配置（用于热更新）
        /// </summary>
        public static void ReloadConfigs()
        {
            UnifiedConfig = null;
            Initialize();
            System.Diagnostics.Debug.WriteLine("[DialogueManager] 配置已重新加载");
        }

        /// <summary>
        /// 获取配置统计信息
        /// </summary>
        public static string GetConfigStats()
        {
            int appointmentCount = UnifiedConfig?.GetEntriesByType("Appointment")?.Count ?? 0;
            int recallCount = UnifiedConfig?.GetEntriesByType("Recall")?.Count ?? 0;
            int refusalCount = UnifiedConfig?.GetEntriesByType("Refusal")?.Count ?? 0;
            return $"任命对话: {appointmentCount} 条, 罢免对话: {recallCount} 条, 拒绝对话: {refusalCount} 条";
        }
    }
}