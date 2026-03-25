using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public enum ActiveAbilityCategory
    {
        Passive = 0,
        EnemyTargetedOffense = 1,
        SelfBuffOffense = 2,
        SelfBuffDefense = 3,
        FriendlySupport = 4,
        Utility = 5,
        Disabled = 6
    }

    public enum ActiveAbilityEvaluationMode
    {
        Disabled = 0,
        Direct = 1,
        IndirectCombatMethod = 2
    }

    /// <summary>
    /// AI角色配置管理器
    /// 负责加载和管理AIRoleSelector的配置数据
    /// 日期：2026-03-10 重构：使用 ConfigManagerBase 统一热重载机制
    /// </summary>
    public static class AIRoleConfigManager
    {
        // 内部管理器实例（继承 ConfigManagerBase）
        private static readonly RoleConfigManager _manager = new();

        /// <summary>
        /// 获取配置实例（线程安全）
        /// </summary>
        public static AIRoleConfig Config => _manager.Config;
        
        /// <summary>
        /// 初始化配置管理器（游戏启动时调用）
        /// </summary>
        public static void Initialize() => _manager.Initialize();
        
        /// <summary>
        /// 热重载更新（主线程 Update 中调用）
        /// </summary>
        public static void Update() => _manager.Update();
        
        /// <summary>
        /// 手动重新加载配置
        /// </summary>
        public static void ReloadConfig() => _manager.ReloadConfig();

        // ==================== 内部管理器类 ====================
        
        private class RoleConfigManager : ConfigManagerBase<AIRoleConfig>
        {
            protected override string ConfigFileName => "AIRoleConfig.json";
            
            protected override AIRoleConfig CreateDefaultConfig() => CreateDefaultRoleConfig();
        }

        // ==================== 默认配置创建 ====================

        private static AIRoleConfig CreateDefaultRoleConfig()
        {
            // 🔥 C# 12: 使用集合表达式
            return new AIRoleConfig
            {
                TroopRoles = new Dictionary<string, RoleConfig>
                {
                    ["Tank"] = new RoleConfig
                    {
                        Description = "肉盾/前排",
                        TroopKindIDs = [11, 51, 150],
                        CoreSkillIDs = [350, 690],
                        StatWeights = new Dictionary<string, float>
                        {
                            ["Command"] = 1.0f,
                            ["Strength"] = 0.3f,
                            ["Intelligence"] = 0.2f
                        },
                        Bonuses = new Dictionary<string, float>
                        {
                            ["TroopMatch"] = 50.0f,
                            ["CoreSkill"] = 30.0f
                        }
                    },
                    ["DPS"] = new RoleConfig
                    {
                        Description = "物理输出",
                        TroopKindIDs = [2, 15, 400],
                        CoreSkillIDs = [383],
                        CriticalSkillRange = new SkillRange { Min = 400, Max = 450 },
                        StatWeights = new Dictionary<string, float>
                        {
                            ["Strength"] = 1.0f,
                            ["Command"] = 0.4f,
                            ["Intelligence"] = 0.2f
                        },
                        Bonuses = new Dictionary<string, float>
                        {
                            ["TroopMatch"] = 50.0f,
                            ["CoreSkill"] = 30.0f
                        }
                    },
                    ["Mage"] = new RoleConfig
                    {
                        Description = "法系/控制",
                        TroopKindIDs = [],
                        CoreSkillIDs = [390, 391, 570],
                        MinIntelligence = 70,
                        StatWeights = new Dictionary<string, float>
                        {
                            ["Intelligence"] = 1.0f,
                            ["Command"] = 0.3f,
                            ["Strength"] = 0.1f
                        },
                        Bonuses = new Dictionary<string, float>
                        {
                            ["TroopMatch"] = 50.0f,
                            ["CoreSkill"] = 30.0f,
                            ["SpecialSkill"] = 45.0f
                        },
                        SpecialSkills = new Dictionary<string, float>
                        {
                            ["570"] = 1.5f // 神算特殊加权
                        }
                    },
                    ["Support"] = new RoleConfig
                    {
                        Description = "辅助/治疗",
                        TroopKindIDs = [],
                        CoreSkillIDs = [399, 397],
                        StatWeights = new Dictionary<string, float>
                        {
                            ["Command"] = 0.2f,
                            ["Intelligence"] = 0.2f,
                            ["Strength"] = 0.1f
                        },
                        Bonuses = new Dictionary<string, float>
                        {
                            ["TroopMatch"] = 50.0f,
                            ["CoreSkill"] = 100.0f
                        }
                    },
                    ["Logistics"] = new RoleConfig
                    {
                        Description = "后勤/运输",
                        TroopKindIDs = [29, 601, 621],
                        CoreSkillIDs = [],
                        StatWeights = new Dictionary<string, float>
                        {
                            ["Command"] = 0.5f,
                            ["Intelligence"] = 0.3f,
                            ["Strength"] = 0.2f
                        },
                        Bonuses = new Dictionary<string, float>
                        {
                            ["TroopMatch"] = 1000.0f // 后勤单位强制锁定
                        }
                    }
                },
                GlobalSettings = new GlobalSettings
                {
                    MinScoreThreshold = 80.0f,
                    DefaultRole = "Balanced"
                },
                SkillMappings = new Dictionary<string, string>
                {
                    ["350"] = "坚阵",
                    ["690"] = "铁壁",
                    ["383"] = "贯穿",
                    ["390"] = "攻心",
                    ["391"] = "扰乱",
                    ["570"] = "神算",
                    ["399"] = "医治",
                    ["397"] = "鼓舞"
                },
                TroopKindMappings = new Dictionary<string, string>
                {
                    ["11"] = "戟兵",
                    ["51"] = "盾兵",
                    ["150"] = "象兵",
                    ["2"] = "骑兵",
                    ["15"] = "弩兵",
                    ["400"] = "虎豹骑",
                    ["29"] = "运输队",
                    ["601"] = "建造队",
                    ["621"] = "工程队"
                },
                AbilityProfiles = new AbilityProfilesConfig
                {
                    CombatMethods = new Dictionary<string, AbilityProfileEntry>
                    {
                        ["0"] = new AbilityProfileEntry { Category = nameof(ActiveAbilityCategory.SelfBuffDefense) },
                        ["1"] = new AbilityProfileEntry { Category = nameof(ActiveAbilityCategory.SelfBuffOffense) },
                        ["2"] = new AbilityProfileEntry { Category = nameof(ActiveAbilityCategory.EnemyTargetedOffense) },
                        ["58"] = new AbilityProfileEntry { Category = nameof(ActiveAbilityCategory.SelfBuffOffense) }
                    },
                    Skills = new Dictionary<string, AbilityProfileEntry>
                    {
                        ["21"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.Passive),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.Disabled)
                        },
                        ["22"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.Passive),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.Disabled)
                        },
                        ["26"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.Utility),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.Disabled)
                        },
                        ["30"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.SelfBuffDefense),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.IndirectCombatMethod),
                            LinkedCombatMethodID = 0
                        },
                        ["31"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.SelfBuffOffense),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.IndirectCombatMethod),
                            LinkedCombatMethodID = 1
                        },
                        ["32"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.EnemyTargetedOffense),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.IndirectCombatMethod),
                            LinkedCombatMethodID = 2
                        },
                        ["92"] = new AbilityProfileEntry
                        {
                            Category = nameof(ActiveAbilityCategory.Utility),
                            EvaluationMode = nameof(ActiveAbilityEvaluationMode.Disabled)
                        }
                    }
                }
            };
        }



        /// <summary>
        /// 获取角色配置
        /// </summary>
        public static RoleConfig GetRoleConfig(string roleName)
        {
            // ANTI-BAND-AID：配置应该在初始化时加载
            System.Diagnostics.Debug.Assert(Config != null,
                "[GetRoleConfig] Config 为 null，检查配置加载逻辑");
            System.Diagnostics.Debug.Assert(Config.TroopRoles != null,
                "[GetRoleConfig] TroopRoles 配置缺失，检查 AIRoleConfig.json");

            if (Config.TroopRoles.TryGetValue(roleName, out RoleConfig roleConfig))
            {
                return roleConfig;
            }

            // ANTI-BAND-AID：找不到角色配置时抛出异常
            throw new InvalidOperationException(
                $"数据损坏：找不到角色 '{roleName}' 的配置！检查 AIRoleConfig.json 中的 TroopRoles 配置。");
        }

        /// <summary>
        /// 检查兵种是否属于指定角色
        /// </summary>
        public static bool IsTroopKindForRole(int troopKindID, string roleName)
        {
            // ANTI-BAND-AID：配置应该在初始化时加载
            System.Diagnostics.Debug.Assert(Config != null,
                "[IsTroopKindForRole] Config 为 null，检查配置加载逻辑");

            var roleConfig = GetRoleConfig(roleName);
            return roleConfig.TroopKindIDs?.Contains(troopKindID) ?? false;
        }

        /// <summary>
        /// 检查技能是否属于指定角色
        /// </summary>
        public static bool IsSkillForRole(int skillID, string roleName)
        {
            // ANTI-BAND-AID：配置应该在初始化时加载
            System.Diagnostics.Debug.Assert(Config != null,
                "[IsSkillForRole] Config 为 null，检查配置加载逻辑");

            var roleConfig = GetRoleConfig(roleName);
            return roleConfig.CoreSkillIDs?.Contains(skillID) ?? false;
        }

        /// <summary>
        /// 获取技能名称
        /// </summary>
        public static string GetSkillName(int skillID)
        {
            // ANTI-BAND-AID：配置应该在初始化时加载
            System.Diagnostics.Debug.Assert(Config != null,
                "[GetSkillName] Config 为 null，检查配置加载逻辑");

            string key = skillID.ToString();
            if (Config.SkillMappings != null && Config.SkillMappings.TryGetValue(key, out string name))
            {
                return name;
            }
            return $"技能{skillID}";
        }

        /// <summary>
        /// 获取兵种名称
        /// </summary>
        public static string GetTroopKindName(int troopKindID)
        {
            // ANTI-BAND-AID：配置应该在初始化时加载
            System.Diagnostics.Debug.Assert(Config != null,
                "[GetTroopKindName] Config 为 null，检查配置加载逻辑");

            string key = troopKindID.ToString();
            if (Config.TroopKindMappings != null && Config.TroopKindMappings.TryGetValue(key, out string name))
            {
                return name;
            }
            return $"兵种{troopKindID}";
        }

        public static ActiveAbilityCategory GetCombatMethodCategory(int combatMethodID, bool viewingHostileFallback)
        {
            return TryGetCombatMethodCategory(combatMethodID, out var category)
                ? category
                : (viewingHostileFallback ? ActiveAbilityCategory.EnemyTargetedOffense : ActiveAbilityCategory.Utility);
        }

        public static ActiveAbilityCategory GetStuntCategory(int stuntID)
        {
            return TryGetStuntCategory(stuntID, out var category)
                ? category
                : ActiveAbilityCategory.Utility;
        }

        public static ActiveAbilityCategory GetSkillCategory(int skillID)
        {
            return TryGetSkillCategory(skillID, out var category)
                ? category
                : ActiveAbilityCategory.Passive;
        }

        public static bool TryGetCombatMethodCategory(int combatMethodID, out ActiveAbilityCategory category)
        {
            return TryGetCategory(Config?.AbilityProfiles?.CombatMethods, combatMethodID, out category);
        }

        public static bool TryGetStuntCategory(int stuntID, out ActiveAbilityCategory category)
        {
            return TryGetCategory(Config?.AbilityProfiles?.Stunts, stuntID, out category);
        }

        public static bool TryGetSkillCategory(int skillID, out ActiveAbilityCategory category)
        {
            return TryGetCategory(Config?.AbilityProfiles?.Skills, skillID, out category);
        }

        public static ActiveAbilityEvaluationMode GetSkillEvaluationMode(int skillID)
        {
            return TryGetSkillEvaluationMode(skillID, out var evaluationMode)
                ? evaluationMode
                : ActiveAbilityEvaluationMode.Disabled;
        }

        public static bool TryGetSkillEvaluationMode(int skillID, out ActiveAbilityEvaluationMode evaluationMode)
        {
            return TryGetEvaluationMode(Config?.AbilityProfiles?.Skills, skillID, out evaluationMode);
        }

        public static bool TryGetSkillLinkedCombatMethodID(int skillID, out int linkedCombatMethodID)
        {
            linkedCombatMethodID = -1;
            var table = Config?.AbilityProfiles?.Skills;
            if (table == null)
            {
                return false;
            }

            var key = skillID.ToString();
            if (!table.TryGetValue(key, out var profile) || profile == null || !profile.LinkedCombatMethodID.HasValue)
            {
                return false;
            }

            linkedCombatMethodID = profile.LinkedCombatMethodID.Value;
            return linkedCombatMethodID >= 0;
        }

        private static bool TryGetCategory(
            Dictionary<string, AbilityProfileEntry> table,
            int id,
            out ActiveAbilityCategory category)
        {
            category = ActiveAbilityCategory.Utility;
            if (table == null)
            {
                return false;
            }

            var key = id.ToString();
            if (!table.TryGetValue(key, out var profile) || profile == null || string.IsNullOrEmpty(profile.Category))
            {
                return false;
            }

            if (Enum.TryParse(profile.Category, true, out category))
            {
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[AIRoleConfig] AbilityProfiles[{key}] category invalid: {profile.Category}");
            return false;
        }

        private static bool TryGetEvaluationMode(
            Dictionary<string, AbilityProfileEntry> table,
            int id,
            out ActiveAbilityEvaluationMode evaluationMode)
        {
            evaluationMode = ActiveAbilityEvaluationMode.Disabled;
            if (table == null)
            {
                return false;
            }

            var key = id.ToString();
            if (!table.TryGetValue(key, out var profile) || profile == null || string.IsNullOrEmpty(profile.EvaluationMode))
            {
                return false;
            }

            if (Enum.TryParse(profile.EvaluationMode, true, out evaluationMode))
            {
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[AIRoleConfig] AbilityProfiles[{key}] evaluation mode invalid: {profile.EvaluationMode}");
            return false;
        }
    }

    /// <summary>
    /// AI角色配置数据结构
    /// </summary>
    public class AIRoleConfig
    {
        public Dictionary<string, RoleConfig> TroopRoles { get; set; }
        public GlobalSettings GlobalSettings { get; set; }
        public Dictionary<string, string> SkillMappings { get; set; }
        public Dictionary<string, string> TroopKindMappings { get; set; }
        public AbilityProfilesConfig AbilityProfiles { get; set; }
        public UtilityAIConfig UtilityAI { get; set; }
    }

    public class AbilityProfilesConfig
    {
        public Dictionary<string, AbilityProfileEntry> CombatMethods { get; set; }
        public Dictionary<string, AbilityProfileEntry> Stunts { get; set; }
        public Dictionary<string, AbilityProfileEntry> Skills { get; set; }
    }

    public class AbilityProfileEntry
    {
        public string Category { get; set; }
        public string EvaluationMode { get; set; }
        public int? LinkedCombatMethodID { get; set; }
    }

    /// <summary>
    /// 角色配置
    /// </summary>
    public class RoleConfig
    {
        public string Description { get; set; }
        public List<int> TroopKindIDs { get; set; }
        public List<int> CoreSkillIDs { get; set; }
        public SkillRange CriticalSkillRange { get; set; }
        public int MinIntelligence { get; set; }
        public Dictionary<string, float> StatWeights { get; set; }
        public Dictionary<string, float> Bonuses { get; set; }
        public Dictionary<string, float> SpecialSkills { get; set; }
    }

    /// <summary>
    /// 技能范围
    /// </summary>
    public class SkillRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    /// <summary>
    /// 全局设置
    /// </summary>
    public class GlobalSettings
    {
        public float MinScoreThreshold { get; set; }
        public string DefaultRole { get; set; }
    }

    /// <summary>
    /// 效用AI配置
    /// </summary>
    public class UtilityAIConfig
    {
        public string Description { get; set; }
        public int ControlStratagemID { get; set; }
        public int DamageStratagemID { get; set; }
        public int HealStratagemID { get; set; }
        public int BuffStratagemID { get; set; }
        public int TacticID { get; set; }
        public Dictionary<string, int> MoraleThresholds { get; set; }
        public Dictionary<string, float> AptitudeThresholds { get; set; }
    }
}
