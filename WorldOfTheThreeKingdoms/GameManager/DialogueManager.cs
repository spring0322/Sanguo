using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameGlobal;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;
using PluginInterface;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 对话类型枚举
    /// </summary>
    public enum DialogueType
    {
        Default,    // 默认对话
        Refusal,    // 拒绝对话
        Recall,     // 罢免对话
        Bond        // 特殊关系对话
    }

    /// <summary>
    /// 军师对话条目类
    /// </summary>
    [Serializable]
    public class AdvisorDialogueEntry
    {
        [XmlAttribute("Type")]
        public string TypeString { get; set; } = "Default";
        
        [XmlIgnore]
        public DialogueType Type 
        { 
            get { return ParseDialogueType(TypeString); }
            set { TypeString = value.ToString(); }
        }
        
        [XmlAttribute("LeaderKind")]
        public int LeaderKind { get; set; } = -1;        // 君主性格类型 (-1表示任意)
        
        [XmlAttribute("AdvisorKind")]
        public int AdvisorKind { get; set; } = -1;       // 军师性格类型 (-1表示任意)
        
        [XmlAttribute("LeaderID")]
        public int LeaderID { get; set; } = -1;          // 特定君主ID (-1表示任意)
        
        [XmlAttribute("AdvisorID")]
        public int AdvisorID { get; set; } = -1;         // 特定军师ID (-1表示任意)
        
        [XmlAttribute("MinIntelligence")]
        public int MinIntelligence { get; set; } = 0;    // 最低智力
        
        [XmlAttribute("MaxIntelligence")]
        public int MaxIntelligence { get; set; } = 999;  // 最高智力
        
        [XmlAttribute("MinAge")]
        public int MinAge { get; set; } = 0;             // 最低年龄
        
        [XmlAttribute("MaxAge")]
        public int MaxAge { get; set; } = 999;           // 最高年龄
        
        [XmlAttribute("MinLoyalty")]
        public int MinLoyalty { get; set; } = 0;         // 最低忠诚度
        
        [XmlAttribute("MaxLoyalty")]
        public int MaxLoyalty { get; set; } = 999;       // 最高忠诚度
        
        [XmlAttribute("MinCommand")]
        public int MinCommand { get; set; } = 0;         // 最低统率
        
        [XmlAttribute("MinPolitics")]
        public int MinPolitics { get; set; } = 0;        // 最低政治
        
        [XmlAttribute("HighAmbition")]
        public bool HighAmbition { get; set; } = false;  // 高野心
        
        [XmlAttribute("Relation")]
        public string Relation { get; set; } = "";       // 关系类型
        
        [XmlElement("LeaderText")]
        public string LeaderText { get; set; } = "";
        
        [XmlElement("AdvisorText")]
        public string AdvisorText { get; set; } = "";

        /// <summary>
        /// 解析对话类型
        /// </summary>
        private static DialogueType ParseDialogueType(string typeString)
        {
            switch (typeString?.ToLower())
            {
                case "refusal": return DialogueType.Refusal;
                case "recall": return DialogueType.Recall;
                case "bond": return DialogueType.Bond;
                default: return DialogueType.Default;
            }
        }
        
        /// <summary>
        /// 计算与指定君主和军师的匹配分数
        /// </summary>
        public int GetMatchScore(Person leader, Person advisor)
        {
            int score = 0;
            
            System.Diagnostics.Debug.WriteLine($"[匹配计算] 开始计算匹配分数");
            System.Diagnostics.Debug.WriteLine($"[匹配计算] 条目要求 - LeaderID: {LeaderID}, AdvisorID: {AdvisorID}, LeaderKind: {LeaderKind}, AdvisorKind: {AdvisorKind}");
            System.Diagnostics.Debug.WriteLine($"[匹配计算] 实际数据 - 君主ID: {leader.ID}, 军师ID: {advisor.ID}, 君主性格: {leader.Character?.ID ?? -999}, 军师性格: {advisor.Character?.ID ?? -999}");
            
            // 特定人物ID匹配检查（必须严格匹配，否则直接淘汰）
            if (LeaderID != -1)
            {
                if (leader.ID == LeaderID)
                {
                    score += 100; // 君主ID完全匹配，最高优先级
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 君主ID匹配，得分+100，当前分数: {score}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 君主ID不匹配 (要求: {LeaderID}, 实际: {leader.ID})，直接淘汰");
                    return 0; // 指定了君主ID但不匹配，直接淘汰
                }
            }
            
            if (AdvisorID != -1)
            {
                if (advisor.ID == AdvisorID)
                {
                    score += 100; // 军师ID完全匹配，最高优先级
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 军师ID匹配，得分+100，当前分数: {score}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 军师ID不匹配 (要求: {AdvisorID}, 实际: {advisor.ID})，直接淘汰");
                    return 0; // 指定了军师ID但不匹配，直接淘汰
                }
            }
            
            // 性格类型匹配检查（必须严格匹配，否则直接淘汰）
            if (LeaderKind != -1)
            {
                if (leader.Character?.ID == LeaderKind)
                {
                    score += 50; // 君主性格匹配
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 君主性格匹配，得分+50，当前分数: {score}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 君主性格不匹配 (要求: {LeaderKind}, 实际: {leader.Character?.ID ?? -999})，直接淘汰");
                    return 0; // 指定了君主性格但不匹配，直接淘汰
                }
            }
            
            if (AdvisorKind != -1)
            {
                if (advisor.Character?.ID == AdvisorKind)
                {
                    score += 50; // 军师性格匹配
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 军师性格匹配，得分+50，当前分数: {score}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 军师性格不匹配 (要求: {AdvisorKind}, 实际: {advisor.Character?.ID ?? -999})，直接淘汰");
                    return 0; // 指定了军师性格但不匹配，直接淘汰
                }
            }
            
            // 智力匹配
            if (advisor.Intelligence >= MinIntelligence && advisor.Intelligence <= MaxIntelligence)
            {
                score += 20;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 智力匹配 ({advisor.Intelligence} 在 {MinIntelligence}-{MaxIntelligence} 范围内)，得分+20，当前分数: {score}");
            }
            else if (MinIntelligence > 0 || MaxIntelligence < 999)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 智力不匹配 ({advisor.Intelligence} 不在 {MinIntelligence}-{MaxIntelligence} 范围内)，直接淘汰");
                return 0; // 不满足智力要求直接淘汰
            }
            
            // 年龄匹配
            if (advisor.Age >= MinAge && advisor.Age <= MaxAge)
            {
                score += 10;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 年龄匹配 ({advisor.Age} 在 {MinAge}-{MaxAge} 范围内)，得分+10，当前分数: {score}");
            }
            else if (MinAge > 0 || MaxAge < 999)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 年龄不匹配 ({advisor.Age} 不在 {MinAge}-{MaxAge} 范围内)，直接淘汰");
                return 0; // 不满足年龄要求直接淘汰
            }
            
            // 忠诚度匹配
            if (advisor.Loyalty >= MinLoyalty && advisor.Loyalty <= MaxLoyalty)
            {
                score += 15;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 忠诚度匹配 ({advisor.Loyalty} 在 {MinLoyalty}-{MaxLoyalty} 范围内)，得分+15，当前分数: {score}");
            }
            else if (MinLoyalty > 0 || MaxLoyalty < 999)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 忠诚度不匹配 ({advisor.Loyalty} 不在 {MinLoyalty}-{MaxLoyalty} 范围内)，直接淘汰");
                return 0; // 不满足忠诚度要求直接淘汰
            }
            
            // 统率匹配
            if (advisor.Command >= MinCommand)
            {
                score += 10;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 统率匹配 ({advisor.Command} >= {MinCommand})，得分+10，当前分数: {score}");
            }
            else if (MinCommand > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 统率不匹配 ({advisor.Command} < {MinCommand})，直接淘汰");
                return 0; // 不满足统率要求直接淘汰
            }
            
            // 政治匹配
            if (advisor.Politics >= MinPolitics)
            {
                score += 10;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 政治匹配 ({advisor.Politics} >= {MinPolitics})，得分+10，当前分数: {score}");
            }
            else if (MinPolitics > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 政治不匹配 ({advisor.Politics} < {MinPolitics})，直接淘汰");
                return 0; // 不满足政治要求直接淘汰
            }
            
            // 野心匹配
            if (HighAmbition && advisor.Ambition > 80)
            {
                score += 15;
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 高野心匹配 ({advisor.Ambition} > 80)，得分+15，当前分数: {score}");
            }
            else if (HighAmbition && advisor.Ambition <= 80)
            {
                System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 高野心不匹配 ({advisor.Ambition} <= 80)，直接淘汰");
                return 0; // 要求高野心但不满足
            }
            
            // 关系匹配
            if (!string.IsNullOrEmpty(Relation))
            {
                if (Relation == "Hate" && advisor.CheckRelation(leader) == -1)
                {
                    score += 30;
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ✅ 厌恶关系匹配，得分+30，当前分数: {score}");
                }
                else if (Relation == "Hate" && advisor.CheckRelation(leader) != -1)
                {
                    System.Diagnostics.Debug.WriteLine($"[匹配计算] ❌ 厌恶关系不匹配，直接淘汰");
                    return 0; // 要求厌恶关系但不满足
                }
            }
            
            int finalScore = Math.Max(score, 1); // 至少返回1分，表示可以匹配
            System.Diagnostics.Debug.WriteLine($"[匹配计算] 最终分数: {finalScore}");
            return finalScore;
        }
    }

    /// <summary>
    /// 对话条目集合
    /// </summary>
    [Serializable]
    public class DialogueEntries
    {
        [XmlAttribute("Type")]
        public string Type { get; set; } = "";
        
        [XmlElement("Entry")]
        public List<AdvisorDialogueEntry> Entries { get; set; } = new List<AdvisorDialogueEntry>();
    }

    /// <summary>
    /// 对话配置类
    /// </summary>
    [Serializable]
    [XmlRoot("DialogueConfig")]
    public class DialogueConfig
    {
        [XmlElement("Entries")]
        public List<DialogueEntries> EntriesGroups { get; set; } = new List<DialogueEntries>();
        
        /// <summary>
        /// 获取指定类型的对话条目
        /// </summary>
        public List<AdvisorDialogueEntry> GetEntriesByType(string type)
        {
            var group = EntriesGroups.FirstOrDefault(g => g.Type == type);
            return group?.Entries ?? new List<AdvisorDialogueEntry>();
        }
    }

    /// <summary>
    /// 军师任命对话管理器 - 统一管理军师任命/罢免相关的对话系统
    /// 注意：与AdvisorDialogueManager.cs中的类分开，该类仅处理任命/罢免对话
    /// </summary>
    public static class AdvisorAppointmentDialogueManager
    {
        private static DialogueConfig dialogueConfig;

        /// <summary>
        /// 初始化加载对话配置
        /// </summary>
        public static void Initialize()
        {
            LoadDialogueConfig();
        }

        /// <summary>
        /// 获取任命军师对话
        /// </summary>
        public static AdvisorDialogueEntry GetAppointDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            if (dialogueConfig == null)
            {
                LoadDialogueConfig();
            }

            List<AdvisorDialogueEntry> entries;
            
            if (isRefusal)
            {
                // 对于拒绝对话，从所有分组中查找Type为Refusal的条目
                entries = new List<AdvisorDialogueEntry>();
                foreach (var group in dialogueConfig.EntriesGroups)
                {
                    entries.AddRange(group.Entries.Where(e => e.Type == DialogueType.Refusal));
                }
            }
            else
            {
                // 对于任命对话，从Appointment分组中查找Type为Default或Bond的条目
                var appointmentEntries = dialogueConfig.GetEntriesByType("Appointment");
                entries = appointmentEntries.Where(e => e.Type == DialogueType.Default || e.Type == DialogueType.Bond).ToList();
            }

            return GetBestMatchDialogue(entries, leader, advisor) ?? GetFallbackAppointDialogue(isRefusal);
        }

        /// <summary>
        /// 获取罢免军师对话
        /// </summary>
        public static AdvisorDialogueEntry GetRecallDialogue(Person leader, Person advisor)
        {
            if (dialogueConfig == null)
            {
                LoadDialogueConfig();
            }

            var entries = dialogueConfig.GetEntriesByType("Recall");
            entries = entries.Where(e => e.Type == DialogueType.Default).ToList();

            return GetBestMatchDialogue(entries, leader, advisor) ?? GetFallbackRecallDialogue();
        }

        /// <summary>
        /// 通用对话获取方法
        /// </summary>
        public static AdvisorDialogueEntry GetDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            return GetAppointDialogue(leader, advisor, isRefusal);
        }

        /// <summary>
        /// 从条目列表中找到最佳匹配的对话
        /// </summary>
        private static AdvisorDialogueEntry GetBestMatchDialogue(List<AdvisorDialogueEntry> entries, Person leader, Person advisor)
        {
            if (entries == null || entries.Count == 0)
                return null;

            System.Diagnostics.Debug.WriteLine($"[对话匹配] 开始匹配对话，候选条目数: {entries.Count}");
            System.Diagnostics.Debug.WriteLine($"[对话匹配] 君主: {leader.Name} (ID: {leader.ID}, 性格ID: {leader.Character?.ID ?? -999}, 性格名: {leader.Character?.Name ?? "无"}), 军师: {advisor.Name} (ID: {advisor.ID}, 性格ID: {advisor.Character?.ID ?? -999}, 性格名: {advisor.Character?.Name ?? "无"})");

            // 计算所有匹配的条目
            var matches = entries
                .Select(entry => {
                    int score = entry.GetMatchScore(leader, advisor);
                    System.Diagnostics.Debug.WriteLine($"[对话匹配] 条目匹配分数: {score}");
                    System.Diagnostics.Debug.WriteLine($"[对话匹配]   - LeaderID限制: {entry.LeaderID} (实际: {leader.ID})");
                    System.Diagnostics.Debug.WriteLine($"[对话匹配]   - AdvisorID限制: {entry.AdvisorID} (实际: {advisor.ID})");
                    System.Diagnostics.Debug.WriteLine($"[对话匹配]   - LeaderKind限制: {entry.LeaderKind} (实际: {leader.Character?.ID ?? -999})");
                    System.Diagnostics.Debug.WriteLine($"[对话匹配]   - AdvisorKind限制: {entry.AdvisorKind} (实际: {advisor.Character?.ID ?? -999})");
                    System.Diagnostics.Debug.WriteLine($"[对话匹配]   - 对话内容: {entry.LeaderText?.Substring(0, Math.Min(20, entry.LeaderText?.Length ?? 0))}...");
                    return new { Entry = entry, Score = score };
                })
                .Where(x => x.Score > 0) // 过滤掉不匹配的
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[对话匹配] 有效匹配条目数: {matches.Count}");

            if (!matches.Any()) return null;

            // 找到最高分数
            int maxScore = matches.Max(x => x.Score);
            
            // 获取所有最高分数的条目
            var topMatches = matches.Where(x => x.Score == maxScore).ToList();
            
            // 如果有多个最高分条目，随机选择一个
            if (topMatches.Count > 1)
            {
                int randomIndex = GameObject.Random(topMatches.Count);
                System.Diagnostics.Debug.WriteLine($"[对话匹配] 发现 {topMatches.Count} 个同权重对话，随机选择第 {randomIndex + 1} 个 (分数: {maxScore})");
                return topMatches[randomIndex].Entry;
            }
            
            System.Diagnostics.Debug.WriteLine($"[对话匹配] 选择最佳匹配对话 (分数: {maxScore})，对话内容: {topMatches.First().Entry.LeaderText?.Substring(0, Math.Min(30, topMatches.First().Entry.LeaderText?.Length ?? 0))}...");
            return topMatches.First().Entry;
        }

        /// <summary>
        /// 获取默认任命对话
        /// </summary>
        private static AdvisorDialogueEntry GetFallbackAppointDialogue(bool isRefusal = false)
        {
            if (isRefusal)
            {
                return new AdvisorDialogueEntry
                {
                    Type = DialogueType.Refusal,
                    LeaderText = "还望先生能为我出谋划策，担任军师一职。",
                    AdvisorText = "承蒙错爱，但在下才疏学浅，恐难胜任此职，万望主公海涵。"
                };
            }
            else
            {
                return new AdvisorDialogueEntry
                {
                    Type = DialogueType.Default,
                    LeaderText = "今欲请足下担任军师一职，不知尊意如何？",
                    AdvisorText = "承蒙主公错爱，属下定当竭尽所能。"
                };
            }
        }

        /// <summary>
        /// 获取默认罢免对话
        /// </summary>
        private static AdvisorDialogueEntry GetFallbackRecallDialogue()
        {
            return new AdvisorDialogueEntry
            {
                Type = DialogueType.Recall,
                LeaderText = "军师之职暂且卸任，望你理解。",
                AdvisorText = "是，属下遵命。"
            };
        }

        /// <summary>
        /// 显示任命军师对话序列 - 使用对话队列实现轮流显示
        /// </summary>
        public static void ShowAppointDialogue(Person leader, Person advisor, MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            AdvisorDialogueEntry dialogue = GetAppointDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 使用对话队列显示轮流对话
            ShowDialogueQueue(gameScreen, leader, advisor, dialogue, "AppointAdvisor");
        }

        /// <summary>
        /// 显示罢免军师对话序列 - 使用对话队列实现轮流显示
        /// </summary>
        public static void ShowRecallDialogue(Person leader, Person advisor, MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            AdvisorDialogueEntry dialogue = GetRecallDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 使用对话队列显示轮流对话
            ShowDialogueQueue(gameScreen, leader, advisor, dialogue, "RecallAdvisor");
        }

        /// <summary>
        /// 使用对话队列显示轮流对话
        /// </summary>
        private static void ShowDialogueQueue(MainGameScreen gameScreen, Person leader, Person advisor, AdvisorDialogueEntry dialogue, string eventType)
        {
            try
            {
                string imageName = $"{eventType}.jpg";
                
                // 设置对话位置
                gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                
                // 第一个对话：君主说话（加入队列）
                gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    leader, leader, dialogue.LeaderText, imageName, "", "");
                
                // 第二个对话：军师回应（自动排队）
                gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, dialogue.AdvisorText, imageName, "", "");
                
                // 在所有对话加入队列后，设置关闭回调和开始显示
                if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                {
                    // 设置关闭回调
                    gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                        gameScreen.Plugins.GameRecordPlugin.AddBranch(leader, eventType, leader.Position);
                        System.Diagnostics.Debug.WriteLine("[轮流对话] 对话完成，记录事件");
                    }));
                    
                    // 开始显示对话 - 使用正确的方法
                    try
                    {
                        var tupianwenziPluginInstance = gameScreen.Plugins.tupianwenziPlugin as tupianwenziPlugin.tupianwenziPlugin;
                        if (tupianwenziPluginInstance?.tupianwenzi != null)
                        {
                            tupianwenziPluginInstance.tupianwenzi.SetIsShowing(gameScreen, true);
                            System.Diagnostics.Debug.WriteLine("[轮流对话] 已启动对话显示 (使用SetIsShowing方法)");
                        }
                        else
                        {
                            // 备用方法
                            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                            System.Diagnostics.Debug.WriteLine("[轮流对话] 已启动对话显示 (使用IsShowing属性)");
                        }
                    }
                    catch (Exception showEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[轮流对话] 启动显示失败: {showEx.Message}");
                        // 备用方法
                        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                }
                else
                {
                    // 如果DialogShowTime为0，直接执行回调
                    gameScreen.Plugins.GameRecordPlugin.AddBranch(leader, eventType, leader.Position);
                }

                System.Diagnostics.Debug.WriteLine($"[轮流对话] 已将两个对话加入队列 - {leader.Name}: {dialogue.LeaderText}");
                System.Diagnostics.Debug.WriteLine($"[轮流对话] 已将两个对话加入队列 - {advisor.Name}: {dialogue.AdvisorText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDialogueQueue] 显示轮流对话时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载对话配置
        /// </summary>
        private static void LoadDialogueConfig()
        {
            try
            {
                string configPath = Path.Combine("Content", "Data", "Plugins", "AdvisorDialogue.xml");
                if (File.Exists(configPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DialogueConfig));
                    using (FileStream fs = new FileStream(configPath, FileMode.Open))
                    {
                        dialogueConfig = (DialogueConfig)serializer.Deserialize(fs);
                    }
                    
                    int totalEntries = dialogueConfig.EntriesGroups.Sum(g => g.Entries.Count);
                    System.Diagnostics.Debug.WriteLine($"[DialogueManager] 加载对话配置成功: {dialogueConfig.EntriesGroups.Count} 个分组, 共 {totalEntries} 条对话");
                }
                else
                {
                    dialogueConfig = new DialogueConfig();
                    System.Diagnostics.Debug.WriteLine("[DialogueManager] 对话配置文件不存在，使用默认配置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueManager] 加载对话配置失败: {ex.Message}");
                dialogueConfig = new DialogueConfig();
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void ReloadConfigs()
        {
            dialogueConfig = null;
            Initialize();
        }
    }
}