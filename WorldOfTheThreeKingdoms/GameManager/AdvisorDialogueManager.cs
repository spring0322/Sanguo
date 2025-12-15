using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GameObjects;
using Platforms;
using Tools;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public static class AdvisorDialogueManager
    {
        private static Dictionary<string, string> templates = new Dictionary<string, string>();
        private static bool isLoaded = false;

        public static void Load()
        {
            if (isLoaded) return;

            templates.Clear();
            string filePath = "GameData/AdvisorDialogueConfig.xml";
            
            // Try to find the file
            if (!File.Exists(filePath))
            {
                try 
                {
                    string gameDataPath = PathHelper.GetGameDataPath();
                    filePath = Path.Combine(gameDataPath, "AdvisorDialogueConfig.xml");
                }
                catch 
                { 
                    if (Platform.Current != null)
                    {
                        filePath = Platform.Current.DirectoryName(Platform.Current.Location) + "/GameData/AdvisorDialogueConfig.xml";
                    }
                }
            }

            if (File.Exists(filePath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    XmlNodeList nodes = doc.SelectNodes("//Dialogue");
                    foreach (XmlNode node in nodes)
                    {
                        string resultType = node.Attributes["Result"]?.Value;
                        string text = node.SelectSingleNode("Text")?.InnerText;
                        
                        if (!string.IsNullOrEmpty(resultType) && !string.IsNullOrEmpty(text))
                        {
                            templates[resultType] = text;
                        }
                    }
                    
                    isLoaded = true;
                    // System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Loaded {templates.Count} templates.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Error loading config: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Config file NOT found at {filePath}");
                AddDefaults();
            }
        }

        private static void AddDefaults()
        {
             // minimal defaults if file missing
             templates["Success_DirectJoin"] = "{LeaderAddress}！{SelfAddress}发现贤才【{Talent}】，已加入我军。";
             templates["Success_FoundOnly"] = "{LeaderAddress}，{SelfAddress}发现贤才【{Talent}】，请主公定夺。";
             templates["Fail_NoTalent"] = "{LeaderAddress}，{SelfAddress}未发现人才。";
             isLoaded = true;
        }

        public static string GetDialogue(Faction faction, AdvisorRecommendationSystem.RecommendationResult result, Person advisor, Person talent, int initialLoyalty)
        {
            if (!isLoaded) Load();

            string key = result.ToString();
            string template = "";
            
            if (templates.ContainsKey(key))
            {
                template = templates[key];
            }
            else if (templates.ContainsKey("Unknown"))
            {
                template = templates["Unknown"];
            }
            else
            {
                return $"{advisor.Name}: ...";
            }

            // Get Appellations
            // Assuming Leader is the person being addressed?
            // "Standard rule: Call My Lord (or Relation)"
            Person leader = faction.Leader;
            var (address, selfAddress) = AppellationSettings.GetCallings(faction, advisor, leader);

            // Replace Placeholders
            string message = template
                .Replace("{LeaderAddress}", address)
                .Replace("{SelfAddress}", selfAddress)
                .Replace("{Advisor}", advisor.Name)
                .Replace("{Talent}", talent != null ? talent.Name : "未知")
                .Replace("{Location}", talent != null && talent.LocationArchitecture != null ? talent.LocationArchitecture.Name : "某地")
                .Replace("{InitialLoyalty}", initialLoyalty.ToString());

            return message;
        }

        /// <summary>
        /// 获取通用对话模板
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="advisor">军师</param>
        /// <param name="dialogueKey">对话模板Key (如 Advice_FundHigh)</param>
        /// <param name="replacements">额外占位符替换 (如 {Fund}, {Food}, {Person}, {Location})</param>
        /// <returns>生成的对话文本</returns>
        public static string GetGenericDialogue(Faction faction, Person advisor, string dialogueKey, Dictionary<string, string> replacements = null)
        {
            if (!isLoaded) Load();

            string template = "";
            
            if (templates.ContainsKey(dialogueKey))
            {
                template = templates[dialogueKey];
            }
            else if (templates.ContainsKey("Unknown"))
            {
                template = templates["Unknown"];
            }
            else
            {
                return "..."; // fallback
            }

            // Get Appellations
            Person leader = faction?.Leader;
            string address = "主公";
            string selfAddress = "臣";

            if (faction != null && advisor != null && leader != null)
            {
                var callings = AppellationSettings.GetCallings(faction, advisor, leader);
                address = callings.Address;
                selfAddress = callings.SelfAddress;
            }

            // Replace standard placeholders
            string message = template
                .Replace("{LeaderAddress}", address)
                .Replace("{SelfAddress}", selfAddress)
                .Replace("{Advisor}", advisor != null ? advisor.Name : "军师");

            // Replace custom placeholders
            if (replacements != null)
            {
                foreach (var kvp in replacements)
                {
                    message = message.Replace(kvp.Key, kvp.Value);
                }
            }

            return message;
        }
    }
}
