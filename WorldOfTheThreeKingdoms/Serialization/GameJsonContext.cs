using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.Conditions;
using GameObjects.Influences;
using GameObjects.MapDetail;
using GameObjects.Animations;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail.EventEffect;
using GameObjects.ArchitectureDetail.EventEffect;
using GameManager;
using GameGlobal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;  // 🔥 2026-03-09 添加：单挑配置类型

namespace WorldOfTheThreeKingdoms.Serialization
{
    // 🔥 2026-02-12 根本修复：添加所有 DTO 类型注册
    // 问题：启用 AOT 源生成器后，DTO 类型未注册导致反序列化失败
    // 解决：注册所有序列化/反序列化使用的 DTO 类型
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.GameScenarioDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.PersonDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.TroopDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.ArchitectureDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.CityDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.PortDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.GateDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.FactionDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.LegionDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.TreasureDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.SectionDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.BiographyDTO))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Serialization.DTOs.MapDTO))]
    
    // 🔥 DTO 列表类型注册
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.PersonDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.TroopDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.ArchitectureDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.FactionDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.LegionDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.TreasureDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.SectionDTO>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.Serialization.DTOs.BiographyDTO>))]
    
    // 🔥 AOT 关键修复：EventEffect 和 EventEffectKind 基类注册（必须先注册基础类型）
    // 🔥 2026-02-12 根本修复：使用 TypeInfoPropertyName 解决同名类型冲突
    // 问题：TroopDetail 和 ArchitectureDetail 都有 EventEffect/EventEffectKind 类型
    // 解决：为每个类型指定唯一的 TypeInfoPropertyName
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffect), TypeInfoPropertyName = "TroopEventEffect")]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKind), TypeInfoPropertyName = "TroopEventEffectKind")]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect), TypeInfoPropertyName = "ArchEventEffect")]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind), TypeInfoPropertyName = "ArchEventEffectKind")]
    
    // 🔥 AOT 关键修复：字典类型注册（基于基础类型）
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "TroopEventEffectKindDict")]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "TroopEventEffectDict")]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "ArchEventEffectKindDict")]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "ArchEventEffectDict")]
    
    // 🔥 2026-02-12 补充修复：添加完整命名空间的字典类型注册
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "TroopEventEffectKindDictFull")]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.TroopDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "TroopEventEffectDictFull")]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "ArchEventEffectKindDictFull")]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "ArchEventEffectDictFull")]
    
    // 🔥 AOT 关键修复：EventEffectKindTable 和 EventEffectTable 类型注册（最后注册容器类型）
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable), TypeInfoPropertyName = "TroopEventEffectKindTable")]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffectTable), TypeInfoPropertyName = "ArchEventEffectTable")]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectTable), TypeInfoPropertyName = "TroopEventEffectTable")]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable), TypeInfoPropertyName = "ArchEventEffectKindTable")]
    
    // 🔥 2026-02-12 根本修复：添加 EventEffect 列表类型注册
    // 问题：Event 和 TroopEvent 类使用 List<EventEffect>，但未在 AOT 中注册
    // 解决：注册所有 EventEffect 相关的列表和字典类型
    [JsonSerializable(typeof(List<global::GameObjects.TroopDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "TroopEventEffectList")]
    [JsonSerializable(typeof(List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "ArchEventEffectList")]
    [JsonSerializable(typeof(Dictionary<int, List<global::GameObjects.TroopDetail.EventEffect.EventEffect>>), TypeInfoPropertyName = "TroopEventEffectListDict")]
    [JsonSerializable(typeof(Dictionary<int, List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>>), TypeInfoPropertyName = "ArchEventEffectListDict")]
    [JsonSerializable(typeof(Dictionary<global::GameObjects.Person, List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>>))]
    
    // 🔥 2026-02-12 关键修复：添加完整的泛型类型注册（System.Collections.Generic 形式）
    // 问题：AOT 需要完整的泛型类型路径才能正确生成元数据
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, System.Collections.Generic.List<global::GameObjects.TroopDetail.EventEffect.EventEffect>>), TypeInfoPropertyName = "TroopEventEffectListDictFull")]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, System.Collections.Generic.List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>>), TypeInfoPropertyName = "ArchEventEffectListDictFull")]
    
    // 🔥 2026-02-12 补充修复：注册 List<EventEffect> 的完整形式
    [JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.TroopDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "TroopEventEffectListFull")]
    [JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>), TypeInfoPropertyName = "ArchEventEffectListFull")]
    
    // 🔥 2026-02-12 补充修复：注册 EventEffectKind 的完整形式
    [JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.TroopDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "TroopEventEffectKindList")]
    [JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>), TypeInfoPropertyName = "ArchEventEffectKindList")]
    
    // 🔥 2026-02-12 补充修复：添加其他可能使用的 List 类型
    // 问题：InfluenceTable.GetInfluenceByKind 返回 List<Influence>
    [JsonSerializable(typeof(List<global::GameObjects.Influences.Influence>))]
    [JsonSerializable(typeof(List<global::GameObjects.Influences.InfluenceKind>))]
    [JsonSerializable(typeof(List<global::GameObjects.Conditions.Condition>))]
    [JsonSerializable(typeof(List<global::GameObjects.Conditions.ConditionKind>))]
    
    // 🔥 AOT 关键修复：添加完整的 System.Collections.Generic 形式
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>))]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.TroopDetail.EventEffect.EventEffect>))]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>))]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<System.Int32, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>))]
    
    // 🔥 AOT 根本修复：添加 JsonSourceGenerationOptions 配置
    // GenerationMode.Default 同时生成元数据和序列化代码
    // 🔥 2026-03-06 启用引用追踪：解决对象引用和多态类型序列化问题
    // 注意：ReferenceHandler 需要在运行时通过 JsonSerializerOptions 配置，不能在源生成器中设置
    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Default,
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    )]
    
    [JsonSerializable(typeof(global::GameObjects.GameScenario))]
    [JsonSerializable(typeof(global::GameObjects.CommonData))]
    
    // Base GameObject class
    [JsonSerializable(typeof(global::GameObjects.GameObject))]
    [JsonSerializable(typeof(global::GameObjects.GameObjectList))]
    
    // Core GameObjects
    [JsonSerializable(typeof(global::GameObjects.Person))]
    [JsonSerializable(typeof(global::GameObjects.Troop))]
    [JsonSerializable(typeof(global::GameObjects.Architecture))]
    [JsonSerializable(typeof(global::GameObjects.Faction))]
    [JsonSerializable(typeof(global::GameObjects.Legion))]
    [JsonSerializable(typeof(global::GameObjects.Military))]
    [JsonSerializable(typeof(global::GameObjects.Treasure))]
    [JsonSerializable(typeof(global::GameObjects.Event))]
    [JsonSerializable(typeof(global::GameObjects.Routeway))]
    [JsonSerializable(typeof(global::GameObjects.RoutePoint))]
    [JsonSerializable(typeof(global::GameObjects.Section))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.Region))]
    [JsonSerializable(typeof(global::GameObjects.Facility))]
    [JsonSerializable(typeof(global::GameObjects.Information))]
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.Technique))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.Stratagem))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.Stunt))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.Skill))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.Title))]
    [JsonSerializable(typeof(global::GameObjects.Animations.TileAnimation))]
    [JsonSerializable(typeof(global::GameObjects.TroopEvent))]
    [JsonSerializable(typeof(global::GameObjects.Captive))]
    [JsonSerializable(typeof(global::GameObjects.Map))]
    [JsonSerializable(typeof(global::GameObjects.GameDate))]
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.DiplomaticRelationTable))]
    [JsonSerializable(typeof(global::GameObjects.NoFoodTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonIDRelation))]
    [JsonSerializable(typeof(global::GameObjects.YearTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.BiographyTable))]
    [JsonSerializable(typeof(global::GameObjects.zainanzhongleibiao))]
    [JsonSerializable(typeof(global::GameObjects.guanjuezhongleibiao))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TextMessageTable))]
    [JsonSerializable(typeof(global::GameObjects.PositionTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.PersonGeneratorSetting))]
    
    [JsonSerializable(typeof(global::GameObjects.Animations.AnimationTable))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.Parameters))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.GlobalVariables))]
    [JsonSerializable(typeof(global::GameManager.Setting))]
    [JsonSerializable(typeof(global::GameManager.Scenario))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AIRoleConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.RoleConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.SkillRange))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.GlobalSettings))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.Helpers.CacheConfig))]
    
    // 🔥 2026-03-09 AI配置系统 AOT 注册 - 计略、战法、特技、策略配置
    // AITacticalConfig 及其所有子配置类型
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AITacticalConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TacticalPositioningConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.PostureEvaluationConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TargetSelectionConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.StrategicPostureConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.RoleScoreConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.SpecialTroopKindsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.StratagemScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.MoraleModifiersConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.ExecutionModifierConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AOEScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.FriendlyStratagemScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TargetNeedModifiersConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TargetValueModifiersConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.FriendlyAOEScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.StratagemSuccessRateConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.CombatMethodScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.CombativityModifiersConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.CombatMethodSuccessRateConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.StuntScoringConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.LegionFormationConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.FormationConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.RoleThresholdConfig))]
    
    // AIDecisionConfig 及其所有子配置类型
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AIDecisionConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.KeyTroopCriteriaConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.StatThresholdsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TitleLevelThresholdConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.QuantityThresholdConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.EliteTroopKindsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AutoDetectConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.DifficultyBasedConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.PlayerFactionConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.PerformanceLimitsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.UtilityAIWeightsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.TraditionalAISettingsConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.DebugSettingsConfig))]
    
    // AIRoleConfig 子配置类型（主类型已注册）
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.UtilityAIConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AbilityProfilesConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AbilityProfileEntry))]
    
    // 单挑配置
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameScreens.ScreenLayers.DantiaoConfigData))]
    
    // 配置使用的字典类型
    [JsonSerializable(typeof(Dictionary<string, Dictionary<string, float>>))]
    [JsonSerializable(typeof(Dictionary<string, global::WorldOfTheThreeKingdoms.GameGlobal.StrategicPostureConfig>))]
    [JsonSerializable(typeof(Dictionary<string, global::WorldOfTheThreeKingdoms.GameGlobal.FormationConfig>))]
    [JsonSerializable(typeof(Dictionary<string, global::WorldOfTheThreeKingdoms.GameGlobal.RoleThresholdConfig>))]
    [JsonSerializable(typeof(Dictionary<string, global::WorldOfTheThreeKingdoms.GameGlobal.AbilityProfileEntry>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    
    // Core Tables and Lists
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.ArchitectureKindTable))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.FacilityKindTable))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.AttackDefaultKindList))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.AttackTargetKindList))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CastDefaultKindList))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CastTargetKindList))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CombatMethodTable))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.MilitaryKindTable))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.StratagemTable))]
    [JsonSerializable(typeof(global::GameObjects.Conditions.ConditionKindTable))]
    [JsonSerializable(typeof(global::GameObjects.Conditions.ConditionTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.CharacterKind))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.SkillTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.StuntTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TitleTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TitleKindTable))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.BiographyAdjectives))]
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.TechniqueTable))]
    [JsonSerializable(typeof(global::GameObjects.MapDetail.TerrainDetailTable))]
    [JsonSerializable(typeof(global::GameObjects.SectionDetail.SectionAIDetailTable))]
    [JsonSerializable(typeof(global::GameObjects.Animations.AnimationTable))]
    [JsonSerializable(typeof(global::GameObjects.zainanzhongleibiao))]
    [JsonSerializable(typeof(global::GameObjects.guanjuezhongleibiao))]
    
    // 🔥 2026-03-22 AOT 修复：伤害数字不显示问题（缺失元数据导致加载失败）
    [JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberGenerator))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.TroopAnimation), TypeInfoPropertyName = "TroopDetailAnimation")]
    [JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberItemList))]
    [JsonSerializable(typeof(global::GameObjects.Animations.CombatNumberItem))]
    [JsonSerializable(typeof(System.Collections.Generic.List<global::GameObjects.Animations.CombatNumberItem>))]
    
    // Additional GameObjects
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.State))]
    [JsonSerializable(typeof(global::GameObjects.NoFoodPosition))]
    [JsonSerializable(typeof(global::GameObjects.GameArea))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.Biography))]
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.DiplomaticRelation))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CombatMethod))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.MilitaryKind))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.ArchitectureKind))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.FacilityKind))]
    [JsonSerializable(typeof(global::GameObjects.Conditions.ConditionKind))]
    [JsonSerializable(typeof(global::GameObjects.Conditions.Condition))]
    [JsonSerializable(typeof(global::GameObjects.Influences.InfluenceKind))]
    [JsonSerializable(typeof(global::GameObjects.Influences.Influence))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect))]
    [JsonSerializable(typeof(global::GameObjects.SectionDetail.SectionAIDetail))]
    [JsonSerializable(typeof(global::GameObjects.MapDetail.TerrainDetail))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TitleKind))]
    [JsonSerializable(typeof(global::GameObjects.guanjuezhongleilei))]
    [JsonSerializable(typeof(global::GameObjects.zainanzhongleilei))]
    [JsonSerializable(typeof(global::GameObjects.Animations.Animation))]
    
    // Additional detail classes
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.InformationKind))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.PersonGeneratorType))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TrainPolicy))]
    [JsonSerializable(typeof(global::GameObjects.TreasureCreationSetting))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.AttackDefaultKind))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.AttackTargetKind))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CastDefaultKind))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.CastTargetKind))]
    [JsonSerializable(typeof(global::GameObjects.YearTableEntry))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TextMessageKind))]
    [JsonSerializable(typeof(KeyValuePair<int, global::GameObjects.PersonDetail.TextMessageKind>))]
    [JsonSerializable(typeof(Dictionary<KeyValuePair<int, global::GameObjects.PersonDetail.TextMessageKind>, List<string>>))]

    // Custom Lists (if they are separate classes)
    [JsonSerializable(typeof(global::GameObjects.PersonList))]
    [JsonSerializable(typeof(global::GameObjects.TroopList))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureList))]
    [JsonSerializable(typeof(global::GameObjects.FactionList))]
    [JsonSerializable(typeof(global::GameObjects.LegionList))]
    [JsonSerializable(typeof(global::GameObjects.MilitaryList))]
    [JsonSerializable(typeof(global::GameObjects.TreasureList))]
    [JsonSerializable(typeof(global::GameObjects.EventList))]
    [JsonSerializable(typeof(global::GameObjects.RoutewayList))]
    [JsonSerializable(typeof(global::GameObjects.SectionList))]
    [JsonSerializable(typeof(global::GameObjects.FacilityList))]
    [JsonSerializable(typeof(global::GameObjects.InformationList))]
    [JsonSerializable(typeof(global::GameObjects.TroopEventList))]
    [JsonSerializable(typeof(global::GameObjects.CaptiveList))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.RegionList))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.StateList))]
    [JsonSerializable(typeof(global::GameObjects.FactionDetail.InformationKindList))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.PersonGeneratorTypeList))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.TrainPolicyList))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.IdealTendencyKindList))]
    [JsonSerializable(typeof(global::GameObjects.TreasureCreationSettingList))]
    
    [JsonSerializable(typeof(global::GameObjects.FactionListWithQueue))]
    [JsonSerializable(typeof(global::GameObjects.TroopListWithQueue))]

    // Primitive and Common Generic Lists
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<global::GameObjects.GameObject>))]
    [JsonSerializable(typeof(List<global::GameManager.Scenario>))]
    [JsonSerializable(typeof(global::GameManager.Scenario[]))]
    [JsonSerializable(typeof(List<global::GameManager.Setting>))]
    [JsonSerializable(typeof(List<global::WorldOfTheThreeKingdoms.GameGlobal.Parameters>))]
    [JsonSerializable(typeof(List<Point>))]
    [JsonSerializable(typeof(Point))]
    [JsonSerializable(typeof(Point?))]
    [JsonSerializable(typeof(List<Color>))]
    [JsonSerializable(typeof(Color))]
    [JsonSerializable(typeof(List<global::GameObjects.PersonDetail.CharacterKind>))]
    [JsonSerializable(typeof(List<global::GameObjects.PersonDetail.BiographyAdjectives>))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.InformationLevel))]
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.IdealTendencyKind))]
    // 🔥 2026-03-20 AOT 修复：添加 List<IdealTendencyKind> 注册
    // 问题：CommonDataLoader 直接反序列化 List<IdealTendencyKind>，但 AOT 源生成器缺少元数据
    // 解决：注册 List<IdealTendencyKind> 类型
    [JsonSerializable(typeof(List<global::GameObjects.PersonDetail.IdealTendencyKind>))]
    [JsonSerializable(typeof(Dictionary<int, int>))]
    [JsonSerializable(typeof(Dictionary<int, int[]>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, float>))]
    [JsonSerializable(typeof(Dictionary<int, string>))] // 🔥 添加测试需要的类型
    [JsonSerializable(typeof(Dictionary<string, global::WorldOfTheThreeKingdoms.GameGlobal.RoleConfig>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.Biography>))]
    
    // 🆕 势力范围配置类型（AOT 支持）
    // 日期：2026-03-16
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.InfluenceConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.EnergyTiers))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.BuffConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.Tier1BuffConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.Tier2BuffConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.Tier3BuffConfig))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameData.VisionConfig))]
    [JsonSerializable(typeof(Dictionary<string, int>))]

    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.FactionDetail.DiplomaticRelation>))]
    [JsonSerializable(typeof(Dictionary<Point, global::GameObjects.NoFoodPosition>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.ArchitectureDetail.ArchitectureKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.TroopDetail.CombatMethod>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.TroopDetail.MilitaryKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.TroopDetail.Stratagem>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.Skill>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.Stunt>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.IdealTendencyKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.FactionDetail.Technique>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.Conditions.ConditionKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.Conditions.Condition>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.Influences.InfluenceKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.Influences.Influence>))]
    
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.ArchitectureDetail.FacilityKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.SectionDetail.SectionAIDetail>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.MapDetail.TerrainDetail>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.TitleKind>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.PersonDetail.Title>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.guanjuezhongleilei>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.zainanzhongleilei>))]
    [JsonSerializable(typeof(Dictionary<int, global::GameObjects.Animations.Animation>))]
    
    // 🔥 补充缺失的字典类型
    [JsonSerializable(typeof(Dictionary<global::GameObjects.Conditions.Condition, float>))]
    [JsonSerializable(typeof(Dictionary<Point, global::GameObjects.NoFoodPosition>))]
    [JsonSerializable(typeof(Dictionary<Point, List<Point>>))]
    [JsonSerializable(typeof(Dictionary<Point, global::GameObjects.RoutePoint>))]
    
    // Core Enums
    [JsonSerializable(typeof(global::GameObjects.PersonDetail.PersonStatus))]
    [JsonSerializable(typeof(global::GameObjects.TroopStatus))]
    [JsonSerializable(typeof(global::WorldOfTheThreeKingdoms.GameGlobal.AIDifficulty))]
    
    // 🔥 AOT 根本修复：注册 EventEffectKind 多态派生类型
    // TroopDetail.EventEffect 派生类
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind0))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind1))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind10))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind15))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind20))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind25))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind30))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind35))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind40))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind45))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind50))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind60))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind80))]
    [JsonSerializable(typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind100))]
    
    // 🔥 AOT 根本修复：注册 ArchitectureDetail EventEffectKind 派生类型
    // ArchitectureDetail.EventEffect 派生类 - 关键修复！
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect0))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect5))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect10))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect15))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect20))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect25))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect30))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect35))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect40))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect45))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect50))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect60))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect70))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect100))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect110))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect120))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect130))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect140))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect150))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect160))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect170))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect200))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect210))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect211))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect212))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect213))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect214))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect215))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect216))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect217))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect220))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect221))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect222))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect223))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect224))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect225))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect226))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect227))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect230))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect235))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect240))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect250))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect270))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect275))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect280))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect290))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect300))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect305))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect310))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect315))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect320))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect325))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect330))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect350))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect355))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect400))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect410))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect420))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect430))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect440))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect450))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect460))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect465))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect466))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect470))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect480))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect500))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect510))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect600))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect650))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect700))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect800))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect810))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect820))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect821))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect822))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect823))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect824))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect825))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect826))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect827))]
    // 高级系列
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1000))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1010))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1020))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1030))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1040))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1050))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1060))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1070))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1080))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1090))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1100))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1110))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1120))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1130))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1140))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1150))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1200))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1210))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1220))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1230))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1240))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1250))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1300))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1310))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect1400))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2000))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2010))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2020))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2030))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2040))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2050))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2100))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2110))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2120))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2130))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2131))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2200))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect2300))]
    [JsonSerializable(typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffect4000))]
    
    public partial class GameJsonContext : JsonSerializerContext
    {
        /// <summary>
        /// 🔥 AOT 强制类型引用 - 确保 AOT 编译器能够识别这些类型
        /// 这个方法永远不会被调用，只是为了强制 AOT 编译器生成类型元数据
        /// </summary>
        private static void ForceAOTTypeReferences()
        {
            // 强制引用 EventEffectTable 类型
            var troopEventEffectTable = new global::GameObjects.TroopDetail.EventEffect.EventEffectTable();
            var archEventEffectTable = new global::GameObjects.ArchitectureDetail.EventEffect.EventEffectTable();
            var troopEventEffectKindTable = new global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable();
            var archEventEffectKindTable = new global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable();
            
            // 强制引用字典类型
            var troopEffectDict = new Dictionary<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>();
            var troopEffectKindDict = new Dictionary<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>();
            var archEffectDict = new Dictionary<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>();
            var archEffectKindDict = new Dictionary<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>();
            
            // 防止编译器优化掉这些变量
            _ = troopEventEffectTable;
            _ = archEventEffectTable;
            _ = troopEventEffectKindTable;
            _ = archEventEffectKindTable;
            _ = troopEffectDict;
            _ = troopEffectKindDict;
            _ = archEffectDict;
            _ = archEffectKindDict;
        }
        /// <summary>
        /// 获取默认的序列化选项，针对 AOT 优化
        /// </summary>
        /// <param name="indented">是否格式化输出</param>
        /// <returns>JsonSerializerOptions</returns>
        public static JsonSerializerOptions GetDefaultOptions(bool indented = false)
        {
            return new JsonSerializerOptions
            {
                WriteIndented = indented,
                PropertyNamingPolicy = null, // 保持原始属性名
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // 🔥 保持ReferenceHandler.Preserve以支持循环引用，通过其他方式解决共享引用问题
                ReferenceHandler = ReferenceHandler.Preserve,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // 🔥 2026-02-12 根本修复：启用 AOT 源生成器
                // 问题：TypeInfoResolver = null 导致使用反射模式，某些复杂类型反序列化失败
                // 解决：使用 GameJsonContext.Default 启用 AOT 源生成器
                TypeInfoResolver = GameJsonContext.Default,
                IncludeFields = true, // 重要：包含字段
                PropertyNameCaseInsensitive = true, // 忽略大小写
                AllowTrailingCommas = true, // 允许尾随逗号
                ReadCommentHandling = JsonCommentHandling.Skip, // 跳过注释
                NumberHandling = JsonNumberHandling.AllowReadingFromString, // 允许从字符串读取数字（适配字典Key）
                Converters =
                {
                    new SystemTextJson.GameObjectListConverter(),
                    // 🔥 禁用 GameObjectReferenceConverter 以避免 StackOverflow - ReferenceHandler.Preserve 已处理循环引用
                    // new SystemTextJson.GameObjectReferenceConverter(),
                    new SystemTextJson.FactionLeaderConverter(),
                    new SystemTextJson.PersonIdealTendencyConverter(),
                    new SystemTextJson.IdealTendencyKindConverter(),
                    new SystemTextJson.EventEffectKindConverter(),
                    // 🔥 2026-02-12 关键修复：移除 EventEffectListDictionaryConverter
                    // 问题：自定义转换器阻止了 AOT 生成 Dictionary<int, List<EventEffect>> 的元数据
                    // 解决：让 AOT 自动处理，已在 GameJsonContext 中注册完整类型
                    // new SystemTextJson.EventEffectListDictionaryConverter<global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    // new SystemTextJson.EventEffectListDictionaryConverter<global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    // 🔥 2026-02-12 根本修复：移除 EventEffect 相关的全局转换器
                    // 问题：全局转换器会拦截所有 Dictionary<int, EventEffect> 的序列化，阻止 AOT 生成元数据
                    // 解决：让 AOT 源生成器完全接管 EventEffectTable 和 EventEffectKindTable 的序列化
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    // 🔥 添加 NoFoodPosition 字典转换器
                    new SystemTextJson.LegacyDictionaryConverter<Microsoft.Xna.Framework.Point, global::GameObjects.NoFoodPosition>(),
                    // 🔥 修复 ProhibitedFactionID 转换问题
                    new SystemTextJson.IntDictionaryConverter(),
                    new SystemTextJson.TextMessageTableConverter(),
                    
                    // 🔥 关键修复：CommonData.json 字典键转换（字符串 → int）
                    // 日期：2026-03-20
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.ArchitectureKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.MilitaryKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.FactionDetail.Technique>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Skill>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.Condition>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.ConditionKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Influences.Influence>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.FacilityKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.CombatMethod>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.Stratagem>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Stunt>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.MapDetail.TerrainDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Title>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.TitleKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.SectionDetail.SectionAIDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Animations.Animation>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>(),
                    
                    // 🔥 修复 InfluenceKind 多态序列化 - 419个派生类动态创建
                    new SystemTextJson.InfluenceKindConverter(),
                    // 🔥 2026-02-12 场景文件修复：处理 Dictionary<int, int[]> 字符串键转 int 键
                    // 问题：JSON 中键是字符串 "0", "1"，但 C# 定义是 int 键，导致 InvalidCastException
                    // 解决：全局注册 IntArrayDictionaryConverter，因为属性级 [JsonConverter] 在 AOT 源生成器中可能失效
                    // 用途：AiBattlingArchitectureStrings, BrotherIds, SuoshuIds, CloseIds, HatedIds
                    new SystemTextJson.IntArrayDictionaryConverter(),
                }
            };
        }

        /// <summary>
        /// 获取剧本文件专用的反序列化选项。
        /// 剧本 JSON（Content/Data/Scenario/*.json）由编辑器生成，不含 $id/$ref 引用标记，
        /// 必须使用 ReferenceHandler = null，否则 AOT 模式下 STJ 会因找不到 $id 字段而抛出
        /// JsonException / FormatException，进而导致集合为空、IndexOutOfRangeException。
        /// Debug 模式下反射路径对此有容忍度，Release/AOT 严格执行，所以仅 Release 崩溃。
        /// </summary>
        public static JsonSerializerOptions GetScenarioFileOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // 🔥 关键：剧本文件无 $id/$ref，不能用 Preserve，否则 AOT 下崩溃
                ReferenceHandler = null,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = GameJsonContext.Default,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters =
                {
                    new SystemTextJson.GameObjectListConverter(),
                    new SystemTextJson.FactionLeaderConverter(),
                    new SystemTextJson.PersonIdealTendencyConverter(),
                    new SystemTextJson.IdealTendencyKindConverter(),
                    new SystemTextJson.EventEffectKindConverter(),
                    new SystemTextJson.LegacyDictionaryConverter<Microsoft.Xna.Framework.Point, global::GameObjects.NoFoodPosition>(),
                    new SystemTextJson.IntDictionaryConverter(),
                    new SystemTextJson.TextMessageTableConverter(),
                    
                    // 🔥 关键修复：CommonData.json 字典键转换（字符串 → int）
                    // 日期：2026-03-20
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.ArchitectureKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.MilitaryKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.FactionDetail.Technique>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Skill>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.Condition>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.ConditionKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Influences.Influence>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.FacilityKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.CombatMethod>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.Stratagem>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Stunt>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.MapDetail.TerrainDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Title>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.TitleKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.SectionDetail.SectionAIDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Animations.Animation>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>(),
                    
                    new SystemTextJson.InfluenceKindConverter(),
                    new SystemTextJson.IntArrayDictionaryConverter(),
                }
            };
        }

        /// <summary>
        /// 获取宽松的反序列化选项，用于兼容性回退
        /// </summary>
        /// <returns>JsonSerializerOptions</returns>
        public static JsonSerializerOptions GetLooseOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = ReferenceHandler.Preserve,
                // 🔥 2026-02-12 根本修复：启用 AOT 源生成器
                TypeInfoResolver = GameJsonContext.Default,
                NumberHandling = JsonNumberHandling.AllowReadingFromString, // 允许从字符串读取数字（适配字典Key）
                Converters =
                {
                    new SystemTextJson.GameObjectListConverter(),
                    // 🔥 禁用 GameObjectReferenceConverter 以避免 StackOverflow - ReferenceHandler.Preserve 已处理循环引用
                    // new SystemTextJson.GameObjectReferenceConverter(),
                    new SystemTextJson.FactionLeaderConverter(),
                    new SystemTextJson.PersonIdealTendencyConverter(),
                    new SystemTextJson.IdealTendencyKindConverter(),
                    new SystemTextJson.EventEffectKindConverter(),
                    // 🔥 2026-02-12 根本修复：移除 EventEffect 相关的全局转换器
                    // 问题：全局转换器会拦截所有 Dictionary<int, EventEffect> 的序列化，阻止 AOT 生成元数据
                    // 解决：让 AOT 源生成器完全接管 EventEffectTable 和 EventEffectKindTable 的序列化
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>(),
                    // new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    // 🔥 添加 NoFoodPosition 字典转换器
                    new SystemTextJson.LegacyDictionaryConverter<Microsoft.Xna.Framework.Point, global::GameObjects.NoFoodPosition>(),
                    // 🔥 修复 ProhibitedFactionID 转换问题
                    new SystemTextJson.IntDictionaryConverter(),
                    new SystemTextJson.TextMessageTableConverter(),
                    
                    // 🔥 关键修复：CommonData.json 字典键转换（字符串 → int）
                    // 日期：2026-03-20
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.ArchitectureKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.MilitaryKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.FactionDetail.Technique>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Skill>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.Condition>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Conditions.ConditionKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Influences.Influence>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.FacilityKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.CombatMethod>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.Stratagem>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Stunt>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.MapDetail.TerrainDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.Title>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.PersonDetail.TitleKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.SectionDetail.SectionAIDetail>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.Animations.Animation>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.TroopDetail.EventEffect.EventEffectKind>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffect>(),
                    new SystemTextJson.LegacyDictionaryConverter<int, global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKind>(),
                    
                    // 🔥 修复 InfluenceKind 多态序列化 - 419个派生类动态创建
                    new SystemTextJson.InfluenceKindConverter(),
                }
            };
        }
    }
}
