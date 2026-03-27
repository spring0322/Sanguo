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
            
            // 尝试多个可能的路径
            string[] possiblePaths = {
                "Content/Data/AdvisorDialogueConfig.xml",
                "Content/Data/Plugins/AdvisorDialogueConfig.xml"
            };
            
            string filePath = null;
            
            // 查找存在的配置文件
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }
            
            // 如果还没找到，尝试使用Platform路径
            if (filePath == null && Platform.Current != null)
            {
                string baseDir = Platform.Current.DirectoryName(Platform.Current.Location);
                foreach (string path in possiblePaths)
                {
                    string testPath = Path.Combine(baseDir, path);
                    if (File.Exists(testPath))
                    {
                        filePath = testPath;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Loading config from: {filePath}");
                    
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
                            System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Loaded template: {resultType} = {text}");
                        }
                    }
                    
                    isLoaded = true;
                    System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Successfully loaded {templates.Count} dialogue templates.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Error loading config: {ex.Message}");
                    AddDefaults();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Config file NOT found in any location, using defaults");
                AddDefaults();
            }
        }

        private static void AddDefaults()
        {
             // 添加完整的默认对话模板
             templates["Success_DirectJoin"] = "{LeaderAddress}！{SelfAddress}在巡查时偶遇贤才【{Talent}】，经过一番劝说，与其意气相投，他已答应出仕，现正在殿外候命！（初始忠诚度：{InitialLoyalty}）";
             templates["Success_FoundOnly"] = "{LeaderAddress}，{SelfAddress}在{Location}发现了一位名叫【{Talent}】的在野贤才。{SelfAddress}虽极力邀请，但其似乎意在待价而沽，还请{LeaderAddress}亲自出马（或派人）前往登庸。（预计忠诚度：{InitialLoyalty}）";
             templates["Fail_NoTalent"] = "{LeaderAddress}，{SelfAddress}已搜遍已知区域，暂未发现合适的贤才。{SelfAddress}会继续留意，请{LeaderAddress}耐心等待。";
             templates["Fail_LowAbility"] = "{LeaderAddress}，{SelfAddress}才疏学浅，未能发现合适的人才。或许需要提升{SelfAddress}的见识，方能为{LeaderAddress}觅得良才。";
             templates["None"] = "{LeaderAddress}，{SelfAddress}今年已经进行过举荐，请待来年再行此事。";
             templates["Unknown"] = "{LeaderAddress}，恕{SelfAddress}的搜寻遇到了意外情况，无法完成招募。";
             
             // 简化招募对话
             templates["Discovery_Initial"] = "{LeaderAddress}，{SelfAddress}在巡查时发现有贤才在野，是否前去查看？";
             templates["Discovery_Confirm"] = "{LeaderAddress}，{SelfAddress}发现了贤才【{Talent}】，此人颇有才能，是否招募？";
             templates["Recruitment_Success"] = "{LeaderAddress}，{SelfAddress}已成功招募到【{Talent}】，现已安排其前往首都效力！";
             templates["Recruitment_Failed"] = "{LeaderAddress}，恕{SelfAddress}无能，【{Talent}】拒绝了我们的邀请，招募失败。";
             templates["No_Talent_Available"] = "{LeaderAddress}，抱歉，未能找到合适的人才。";
             templates["Talent_Already_Recruited"] = "{LeaderAddress}，抱歉，该人才已被其他势力招募。";
             
             // 日常建议对话
             templates["Advice_FundHigh"] = "{LeaderAddress}，当前府库充盈({Fund} 金)，正是厉兵秣马、招贤纳士之时。";
             templates["Advice_FoodHigh"] = "{LeaderAddress}，粮草充足({Food} 石)，可考虑扩充军备或开拓疆土。";
             templates["Advice_LowTroops"] = "{LeaderAddress}，当前军队相对城池较少，建议适当扩充军备以备不时之需。";
             templates["Advice_LowPersons"] = "{LeaderAddress}，当前人才稀缺，建议多方招揽贤士以助大业。";
             templates["Advice_Stable"] = "{LeaderAddress}，当前形势尚好，可继续按既定方针发展。";
             templates["Advice_Loading"] = "{LeaderAddress}，{SelfAddress}正在观察天下大势，稍后再为您详细分析。";
             
             // 紧急警报对话
             templates["Emergency_Rebellion"] = "{LeaderAddress}！{SelfAddress}夜观天象，发现 {Person} 面露反骨，恐近日即将叛变！";
             templates["Emergency_CityUnderAttack"] = "{LeaderAddress}！{Location} 危在旦夕，敌军兵临城下，请{LeaderAddress}立即前往指挥！";
             
             // 无军师提示
             templates["NoAdvisor"] = "{LeaderAddress}，当前无军师在侧，无法提供策略建议。";
             
             isLoaded = true;
             System.Diagnostics.Debug.WriteLine($"[AdvisorDialogueManager] Loaded {templates.Count} default dialogue templates.");
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
            string message = (template ?? "{Advisor}: ...")
                .Replace("{LeaderAddress}", address ?? "主公")
                .Replace("{SelfAddress}", selfAddress ?? "臣")
                .Replace("{Advisor}", advisor != null ? advisor.Name : "军师")
                .Replace("{Talent}", talent != null ? talent.Name : "未知")
                .Replace("{Location}", (talent != null && talent.LocationArchitecture != null) ? talent.LocationArchitecture.Name : "某地")
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
            string message = (template ?? "...")
                .Replace("{LeaderAddress}", address ?? "主公")
                .Replace("{SelfAddress}", selfAddress ?? "臣")
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
