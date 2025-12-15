using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GameObjects;
using Platforms;
using GameGlobal;
using GameManager; // For Session

namespace WorldOfTheThreeKingdoms.GameManager
{
    public enum AppellationConditionType
    {
        FactionRank,
        Identity,
        Relation,
        Title,
        Spouse,   // 配偶关系
        Mother,   // 母亲关系
        None
    }

    public class AppellationRule
    {
        public int Priority;
        public AppellationConditionType Type;
        public string ConditionValue;
        public string Address;
        public string SelfAddress; // How the speaker refers to themselves
    }

    public static class AppellationSettings
    {
        private static List<AppellationRule> rules = new List<AppellationRule>();
        private static bool isLoaded = false;

        public static void Reload()
        {
            isLoaded = false;
            Load();
        }

        public static void Load()
        {
            if (isLoaded) return;

            rules.Clear();
            
            // 从 Plugins 文件夹加载配置
            string filePath = "Plugins/AppellationConfig.xml";
            
            // Try to find the file using Platform path
            if (!File.Exists(filePath))
            {
                if (Platform.Current != null)
                {
                    string baseDir = Platform.Current.DirectoryName(Platform.Current.Location);
                    filePath = Path.Combine(baseDir, "Plugins", "AppellationConfig.xml");
                }
            }

            if (File.Exists(filePath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    XmlNodeList ruleNodes = doc.SelectNodes("//Rule");
                    foreach (XmlNode node in ruleNodes)
                    {
                        AppellationRule rule = new AppellationRule();
                        
                        if (node.Attributes["Priority"] != null && int.TryParse(node.Attributes["Priority"].Value, out int p))
                            rule.Priority = p;
                        else
                            rule.Priority = 999;

                        string typeStr = node.SelectSingleNode("ConditionType")?.InnerText;
                        try {
                            rule.Type = (AppellationConditionType)Enum.Parse(typeof(AppellationConditionType), typeStr, true);
                        } catch { rule.Type = AppellationConditionType.None; }

                        rule.ConditionValue = node.SelectSingleNode("ConditionValue")?.InnerText;
                        rule.Address = node.SelectSingleNode("Address")?.InnerText;
                        rule.SelfAddress = node.SelectSingleNode("SelfAddress")?.InnerText; // Load SelfAddress

                        rules.Add(rule);
                    }

                    // Sort by priority (ascending)
                    rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    
                    isLoaded = true;
                    System.Diagnostics.Debug.WriteLine($"[AppellationSettings] Loaded {rules.Count} rules from {filePath}.");
                    return; // Successfully loaded
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppellationSettings] Error loading config: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AppellationSettings] Config file NOT found at {filePath}. Using HARDCODED defaults.");
            }

            // Fallback: Add default rules if file load failed
            AddDefaultRules();
            isLoaded = true;
        }

        private static void AddDefaultRules()
        {
            rules.Add(new AppellationRule { Priority = 10, Type = AppellationConditionType.FactionRank, ConditionValue = "Max", Address = "陛下", SelfAddress = "微臣" });
            rules.Add(new AppellationRule { Priority = 20, Type = AppellationConditionType.FactionRank, ConditionValue = "MaxMinusOne", Address = "大王", SelfAddress = "臣" });
            
            // 配偶和母亲关系 (高优先级)
            rules.Add(new AppellationRule { Priority = 22, Type = AppellationConditionType.Relation, ConditionValue = "Spouse", Address = "dynamic", SelfAddress = "dynamic" });
            rules.Add(new AppellationRule { Priority = 23, Type = AppellationConditionType.Relation, ConditionValue = "Mother", Address = "母亲", SelfAddress = "儿" });
            
            // Relations have higher priority than generic "Leader" identity
            rules.Add(new AppellationRule { Priority = 24, Type = AppellationConditionType.Relation, ConditionValue = "Father", Address = "父亲", SelfAddress = "儿" });
            rules.Add(new AppellationRule { Priority = 25, Type = AppellationConditionType.Relation, ConditionValue = "SwornBrother", Address = "义兄", SelfAddress = "愚弟" });
            
            rules.Add(new AppellationRule { Priority = 30, Type = AppellationConditionType.Identity, ConditionValue = "Leader", Address = "主公", SelfAddress = "臣" });
            
            rules.Add(new AppellationRule { Priority = 45, Type = AppellationConditionType.Relation, ConditionValue = "Son", Address = "我儿", SelfAddress = "为父" });
            rules.Add(new AppellationRule { Priority = 60, Type = AppellationConditionType.Relation, ConditionValue = "ClosePerson", Address = "Zi", SelfAddress = "我" });
            
            rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            System.Diagnostics.Debug.WriteLine("[AppellationSettings] Default rules applied.");
        }

        public static string GetAddress(Faction faction, Person speaker, Person target)
        {
            var result = GetCallings(faction, speaker, target);
            return result.Address;
        }

        public static (string Address, string SelfAddress) GetCallings(Faction faction, Person speaker, Person target)
        {
            if (!isLoaded) Load();
            string defaultAddress = target != null ? target.Name : "";
            string defaultSelf = "我";

            if (faction == null || speaker == null || target == null) return (defaultAddress, defaultSelf);
            
            // Self-addressing with ID check
            if (speaker.ID == target.ID) return ("我", "我");

            foreach (var rule in rules)
            {
                if (CheckCondition(rule, faction, speaker, target))
                {
                    string address = ResolveDynamicAddress(rule, speaker, target, true);
                    string selfAddress = ResolveDynamicAddress(rule, speaker, target, false);
                    
                    if (!string.IsNullOrEmpty(address)) 
                        return (address, string.IsNullOrEmpty(selfAddress) ? defaultSelf : selfAddress);
                }
            }

            return (target.Name, defaultSelf);
        }

        private static bool CheckCondition(AppellationRule rule, Faction faction, Person speaker, Person target)
        {
            switch (rule.Type)
            {
                case AppellationConditionType.FactionRank:
                    if (rule.ConditionValue == "Max")
                    {
                        // Using global Session from GameManager namespace
                        if (global::GameManager.Session.Current == null || global::GameManager.Session.Current.Scenario == null) return false;
                        int count = global::GameManager.Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count;
                        return faction.guanjue >= count - 1; 
                    }
                    else if (rule.ConditionValue == "MaxMinusOne")
                    {
                         if (global::GameManager.Session.Current == null || global::GameManager.Session.Current.Scenario == null) return false;
                         int count = global::GameManager.Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count;
                         return faction.guanjue == count - 2;
                    }
                    else
                    {
                         if (int.TryParse(rule.ConditionValue, out int reqRank))
                         {
                             return faction.guanjue == reqRank;
                         }
                    }
                    break;

                case AppellationConditionType.Identity:
                    if (rule.ConditionValue == "Leader") 
                    {
                        return faction.Leader != null && target.ID == faction.Leader.ID;
                    }
                    break;

                case AppellationConditionType.Relation:
                     // 配偶关系
                     if (rule.ConditionValue == "Spouse") 
                     {
                         return speaker.Spouse != null && speaker.Spouse.ID == target.ID;
                     }
                     // 母亲关系
                     if (rule.ConditionValue == "Mother") 
                     {
                         return speaker.Mother != null && speaker.Mother.ID == target.ID;
                     }
                     if (rule.ConditionValue == "Father") 
                     {
                         return speaker.Father != null && speaker.Father.ID == target.ID;
                     }
                     if (rule.ConditionValue == "Son") 
                     {
                         return target.Father != null && target.Father.ID == speaker.ID;
                     }
                     
                     if (rule.ConditionValue == "SwornBrother") 
                     {
                         // Check by ID in Brothers list
                         foreach (GameObject obj in speaker.Brothers.GameObjects)
                         {
                             if (obj is Person p && p.ID == target.ID) return true;
                         }
                         return false;
                     }
                     if (rule.ConditionValue == "ClosePerson") return speaker.ClosePersons.Contains(target.ID);
                     break;
            }
            return false;
        }

        private static bool IsFactionRank(Faction faction, string val)
        {
             // This logic was moved inside CheckCondition above to ensure correct Session access
             return false; 
        }

        private static string ResolveDynamicAddress(AppellationRule rule, Person speaker, Person target, bool isTargetAddress)
        {
            // If resolving for Target Address
            if (isTargetAddress)
            {
                // 配偶称呼：根据说话者性别动态选择
                if (rule.ConditionValue == "Spouse")
                {
                    // Sex: true = 女性, false = 男性
                    // 男性称呼妻子为"娘子"，女性称呼丈夫为"夫君"
                    return speaker.Sex ? "夫君" : "娘子";
                }
                
                if (rule.Address == "Zi")
                {
                    return !string.IsNullOrEmpty(target.CalledName) ? target.CalledName : target.Name; 
                }
                
                if (rule.ConditionValue == "SwornBrother") 
                {
                    if (target.YearBorn < speaker.YearBorn) return "大哥"; 
                    else return "贤弟"; 
                }
                
                if (rule.ConditionValue == "Son") return "我儿";
                
                return rule.Address;
            }
            // If resolving for Self Address
            else
            {
                // 配偶自称：根据说话者性别动态选择
                if (rule.ConditionValue == "Spouse")
                {
                    // 男性自称"为夫"，女性自称"妾身"
                    return speaker.Sex ? "妾身" : "为夫";
                }
                
                if (rule.ConditionValue == "SwornBrother")
                {
                    if (target.YearBorn < speaker.YearBorn) return "愚弟"; 
                    else return "愚兄";
                }
                return rule.SelfAddress;
            }
        }
    }
}
