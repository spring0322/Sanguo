using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    // 对话类型枚举
    public enum DialogueType
    {
        Default,
        Personality,
        Bond,
        Refusal,  // 新增：拒绝对话类型
        Recall    // 新增：罢免对话类型
    }

    // 定义关系类型枚举
    public enum RelationType
    {
        None, // 无特殊要求
        Love, // 亲爱
        Hate  // 厌恶
    }

    // 对应 XML 中的 <Entry>
    public class DialogueEntry
    {
        [XmlAttribute]
        public DialogueType Type { get; set; } = DialogueType.Default;

        // 羁绊匹配条件
        [XmlAttribute]
        public int LeaderID { get; set; } = -1;

        [XmlAttribute]
        public int AdvisorID { get; set; } = -1;

        // 性格匹配条件
        [XmlAttribute]
        public int LeaderKind { get; set; } = -1; // -1 代表不限制

        // 智力匹配条件
        [XmlAttribute]
        public int MinIntelligence { get; set; } = 0;

        [XmlAttribute]
        public int MaxIntelligence { get; set; } = 999;

        // 新增匹配条件
        [XmlAttribute]
        public int MaxLoyalty { get; set; } = 100; // 默认100，即不限制低忠诚

        [XmlAttribute]
        public int MinLoyalty { get; set; } = 0; // 最小忠诚度

        [XmlAttribute]
        public bool HighAmbition { get; set; } = false; // 是否要求是野心家

        [XmlAttribute]
        public int MinAge { get; set; } = 0; // 最小年龄

        [XmlAttribute]
        public int MaxAge { get; set; } = 999; // 最大年龄

        [XmlAttribute]
        public int MinCommand { get; set; } = 0; // 最小统率

        [XmlAttribute]
        public int MinPolitics { get; set; } = 0; // 最小政治

        [XmlAttribute]
        public int AdvisorKind { get; set; } = -1; // 军师性格要求，-1表示不限制

        // 【新增】关系匹配条件
        [XmlAttribute]
        public RelationType Relation { get; set; } = RelationType.None;

        // 对话内容
        public string LeaderText { get; set; }
        public string AdvisorText { get; set; }

        /// <summary>
        /// 计算匹配权重 (用于排序，越具体的匹配权重越高)
        /// </summary>
        public int GetMatchScore(Person leader, Person advisor)
        {
            int score = 0;

            // --- 1. 关系检查 (权重极高) ---
            if (Relation != RelationType.None)
            {
                // 假设 Person 类有 CheckRelation 方法
                // CheckRelation 返回: 0=无, 1=亲爱, -1=厌恶
                int relStatus = advisor.CheckRelation(leader);
                
                if (Relation == RelationType.Hate)
                {
                    // 只有当军师真的厌恶君主时才匹配
                    if (relStatus != -1) return -1;
                    score += 500; // 厌恶关系的权重非常高
                }
                else if (Relation == RelationType.Love)
                {
                    // 只有当军师亲爱君主时才匹配
                    if (relStatus != 1) return -1;
                    score += 200; // 亲爱关系的权重较高
                }
            }

            // --- 2. 羁绊检查 (Tier 0) ---
            if (Type == DialogueType.Bond)
            {
                if (leader.ID == LeaderID && advisor.ID == AdvisorID) return 1000 + score;
                return -1; // 不匹配
            }

            // --- 3. 罢免对话检查 (Recall) ---
            if (Type == DialogueType.Recall)
            {
                // 如果 XML 里配置了 Type="Recall"，就只匹配 Recall 的请求
                // 逻辑与 Personality 类似，检查智力或关系
                
                // 优先匹配关系好的 (Relation="Love")
                if (Relation == RelationType.Love)
                {
                    if (advisor.CheckRelation(leader) == 1) return 500;
                    return -1;
                }
                
                // 其次匹配智力低的 (MaxIntelligence)
                if (MaxIntelligence < 999)
                {
                    if (advisor.Intelligence <= MaxIntelligence) return 200;
                    return -1;
                }
                
                // 通用保底
                return 10;
            }

            // --- 4. 性格/属性检查 ---
            if (Type == DialogueType.Personality)
            {
                bool conditionMet = false;

                // 检查君主性格
                if (LeaderKind != -1)
                {
                    if (leader.Character.ID != LeaderKind) return -1;
                    score += 100;
                    conditionMet = true;
                }

                // 检查军师性格
                if (AdvisorKind != -1)
                {
                    if (advisor.Character.ID != AdvisorKind) return -1;
                    score += 80;
                    conditionMet = true;
                }

                // 检查军师智力
                if (advisor.Intelligence < MinIntelligence || advisor.Intelligence > MaxIntelligence) return -1;
                if (MinIntelligence > 0 || MaxIntelligence < 999)
                {
                    score += 50;
                    conditionMet = true;
                }

                // 检查忠诚度
                if (advisor.Loyalty < MinLoyalty || advisor.Loyalty > MaxLoyalty) return -1;
                if (MinLoyalty > 0 || MaxLoyalty < 100)
                {
                    score += 40;
                    conditionMet = true;
                }

                // 检查野心 (假设 Ambition > 60 算高野心)
                if (HighAmbition && advisor.Ambition <= 60) return -1;
                if (HighAmbition)
                {
                    score += 30;
                    conditionMet = true;
                }

                // 检查年龄
                if (advisor.Age < MinAge || advisor.Age > MaxAge) return -1;
                if (MinAge > 0 || MaxAge < 999)
                {
                    score += 20;
                    conditionMet = true;
                }

                // 检查统率
                if (advisor.Command < MinCommand) return -1;
                if (MinCommand > 0)
                {
                    score += 25;
                    conditionMet = true;
                }

                // 检查政治
                if (advisor.Politics < MinPolitics) return -1;
                if (MinPolitics > 0)
                {
                    score += 25;
                    conditionMet = true;
                }

                // 如果没有任何具体条件限制 (纯泛型Personality)，权重较低
                return conditionMet ? score : -1;
            }

            // Default
            return 1 + score;
        }
    }

    // 对应 XML 中的根节点 <DialogueConfig>
    [XmlRoot("DialogueConfig")]
    public class DialogueConfig
    {
        [XmlElement("Entry")]
        public List<DialogueEntry> Entries { get; set; } = new List<DialogueEntry>();
    }

    // 【新增】任命军师对话配置类
    [XmlRoot("AppointmentDialogues")]
    public class AppointmentDialogueConfig
    {
        [XmlElement("Entry")]
        public List<DialogueEntry> Entries { get; set; } = new List<DialogueEntry>();
    }

    // 【新增】罢免军师对话配置类
    [XmlRoot("RecallDialogues")]
    public class RecallDialogueConfig
    {
        [XmlElement("Entry")]
        public List<DialogueEntry> Entries { get; set; } = new List<DialogueEntry>();
    }

    /// <summary>
    /// 对话配置管理器
    /// </summary>
    public static class DialogueConfigManager
    {
        private static AppointmentDialogueConfig _appointmentConfig;
        private static RecallDialogueConfig _recallConfig;
        private static List<DialogueEntry> _cachedEntries;

        /// <summary>
        /// 加载任命军师对话配置
        /// </summary>
        public static AppointmentDialogueConfig LoadAppointmentDialogues()
        {
            if (_appointmentConfig == null)
            {
                _appointmentConfig = LoadAppointmentDialogueConfig("Content/Data/AppointmentDialogues.xml");
            }
            return _appointmentConfig;
        }

        /// <summary>
        /// 加载罢免军师对话配置
        /// </summary>
        public static RecallDialogueConfig LoadRecallDialogues()
        {
            if (_recallConfig == null)
            {
                _recallConfig = LoadRecallDialogueConfig("Content/Data/RecallDialogues.xml");
            }
            return _recallConfig;
        }

        /// <summary>
        /// 获取缓存的对话条目
        /// </summary>
        public static List<DialogueEntry> CachedEntries
        {
            get { return _cachedEntries ?? new List<DialogueEntry>(); }
        }

        /// <summary>
        /// 通用配置加载方法 - 任命对话
        /// </summary>
        public static void LoadConfig(string xmlPath)
        {
            // 使用新的类名 AppointmentDialogueConfig
            XmlSerializer serializer = new XmlSerializer(typeof(AppointmentDialogueConfig));
            using (FileStream fs = new FileStream(xmlPath, FileMode.Open))
            {
                // 加载并强转为新的配置类
                var config = (AppointmentDialogueConfig)serializer.Deserialize(fs);
                // 如果你需要缓存它，请确保你的缓存变量类型也是 AppointmentDialogueConfig
                // 或者把 Entries 提取出来存到一个通用的 List<DialogueEntry> 里
                _cachedEntries = config.Entries;
            }
        }

        /// <summary>
        /// 通用配置加载方法 - 罢免对话
        /// </summary>
        public static void LoadRecallConfig(string xmlPath)
        {
            // 使用罢免对话配置类
            XmlSerializer serializer = new XmlSerializer(typeof(RecallDialogueConfig));
            using (FileStream fs = new FileStream(xmlPath, FileMode.Open))
            {
                // 加载并强转为罢免配置类
                var config = (RecallDialogueConfig)serializer.Deserialize(fs);
                // 把 Entries 提取出来存到通用的缓存里
                _cachedEntries = config.Entries;
            }
        }

        /// <summary>
        /// 从XML文件加载任命对话配置
        /// </summary>
        private static AppointmentDialogueConfig LoadAppointmentDialogueConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppointmentDialogueConfig));
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        return (AppointmentDialogueConfig)serializer.Deserialize(fs);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 配置文件不存在: {filePath}");
                    return new AppointmentDialogueConfig(); // 返回空配置
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 加载配置失败: {ex.Message}");
                return new AppointmentDialogueConfig(); // 返回空配置
            }
        }

        /// <summary>
        /// 从XML文件加载罢免对话配置
        /// </summary>
        private static RecallDialogueConfig LoadRecallDialogueConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(RecallDialogueConfig));
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        return (RecallDialogueConfig)serializer.Deserialize(fs);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 配置文件不存在: {filePath}");
                    return new RecallDialogueConfig(); // 返回空配置
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 加载配置失败: {ex.Message}");
                return new RecallDialogueConfig(); // 返回空配置
            }
        }

        /// <summary>
        /// 从XML文件加载通用对话配置（保留用于兼容性）
        /// </summary>
        private static DialogueConfig LoadDialogueConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DialogueConfig));
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        return (DialogueConfig)serializer.Deserialize(fs);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 配置文件不存在: {filePath}");
                    return new DialogueConfig(); // 返回空配置
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueConfig] 加载配置失败: {ex.Message}");
                return new DialogueConfig(); // 返回空配置
            }
        }

        /// <summary>
        /// 查找最佳匹配的对话条目（通用版本）
        /// </summary>
        public static DialogueEntry FindBestMatch(DialogueConfig config, Person leader, Person advisor)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return null;

            // 计算所有条目的匹配分数
            var matches = config.Entries
                .Select(entry => new { Entry = entry, Score = entry.GetMatchScore(leader, advisor) })
                .Where(match => match.Score > 0) // 过滤掉不匹配的
                .OrderByDescending(match => match.Score) // 按分数降序排列
                .ToList();

            return matches.FirstOrDefault()?.Entry;
        }

        /// <summary>
        /// 查找最佳匹配的任命对话条目
        /// </summary>
        public static DialogueEntry FindBestMatch(AppointmentDialogueConfig config, Person leader, Person advisor)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return null;

            // 计算所有条目的匹配分数
            var matches = config.Entries
                .Select(entry => new { Entry = entry, Score = entry.GetMatchScore(leader, advisor) })
                .Where(match => match.Score > 0) // 过滤掉不匹配的
                .OrderByDescending(match => match.Score) // 按分数降序排列
                .ToList();

            return matches.FirstOrDefault()?.Entry;
        }

        /// <summary>
        /// 查找最佳匹配的罢免对话条目
        /// </summary>
        public static DialogueEntry FindBestMatch(RecallDialogueConfig config, Person leader, Person advisor)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return null;

            // 计算所有条目的匹配分数
            var matches = config.Entries
                .Select(entry => new { Entry = entry, Score = entry.GetMatchScore(leader, advisor) })
                .Where(match => match.Score > 0) // 过滤掉不匹配的
                .OrderByDescending(match => match.Score) // 按分数降序排列
                .ToList();

            return matches.FirstOrDefault()?.Entry;
        }
    }
}