using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Base Data Transfer Object for Architecture
    /// Supports polymorphic serialization with derived types
    /// </summary>
    [JsonDerivedType(typeof(CityDTO), typeDiscriminator: "city")]
    [JsonDerivedType(typeof(PortDTO), typeDiscriminator: "port")]
    [JsonDerivedType(typeof(GateDTO), typeDiscriminator: "gate")]
    public class ArchitectureDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int CaptionID { get; set; }
        public int KindID { get; set; }
        
        // Location
        public int AreaX { get; set; }
        public int AreaY { get; set; }
        public int AreaWidth { get; set; }
        public int AreaHeight { get; set; }
        
        // 🔥 关键修复：添加 ArchitectureAreaString 用于序列化建筑区域
        public string ArchitectureAreaString { get; set; }
        
        // Reference fields (ID only)
        public int BelongedFactionID { get; set; } = -1;
        public int BelongedSectionID { get; set; } = -1;
        public int MayorID { get; set; } = -1;
        public int StateID { get; set; }  // 🔥 修复：添加 StateID 字段用于恢复 LocationState
        
        // Resources
        public int Agriculture { get; set; }
        public int Commerce { get; set; }
        public int Technology { get; set; }
        public int Morale { get; set; }
        public int Endurance { get; set; }
        public int Domination { get; set; }  // 🔥 修复：添加统治度字段
        public int Population { get; set; }
        public int MilitaryPopulation { get; set; }  // 🔥 修复：添加兵役人口字段
        public int Fund { get; set; }
        public int Food { get; set; }
        
        // Collection references
        public List<int> PersonIDs { get; set; } = [];
        public List<int> MilitaryIDs { get; set; } = [];
        public List<int> FacilityIDs { get; set; } = [];
        
        // Backward compatibility
        // 🔥 AOT 修复：使用 JsonPropertyName 显式指定属性名，确保多态序列化时正确识别
        // 日期：2026-03-16
        // 问题：AOT 开启后，多态序列化（JsonDerivedType）可能导致基类的 JsonInclude 字段被跳过
        // 解决：显式使用 JsonPropertyName 强制 AOT Source Generator 生成反序列化代码
        [JsonInclude]
        [JsonPropertyName("PersonsString")]
        public string PersonsString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("MilitariesString")]
        public string MilitariesString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("FacilitiesString")]
        public string FacilitiesString { get; set; }
        
        // 🔥 修复：添加 CharacteristicsString 用于序列化建筑特色
        [JsonInclude]
        [JsonPropertyName("CharacteristicsString")]
        public string CharacteristicsString { get; set; }
        
        // 🔥 根本修复：添加 AI 链接数据字段，避免每次读档重新生成（耗时54秒）
        [JsonInclude]
        [JsonPropertyName("AILandLinksString")]
        public string AILandLinksString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("AIWaterLinksString")]
        public string AIWaterLinksString { get; set; }
        
        // 🔥 完整性修复：添加所有缺失的字符串字段（2026-02-16）
        [JsonInclude]
        [JsonPropertyName("FundPacksString")]
        public string FundPacksString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("FoodPacksString")]
        public string FoodPacksString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("InformationsString")]
        public string InformationsString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("PopulationPacksString")]
        public string PopulationPacksString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("MilitaryPopulationPacksString")]
        public string MilitaryPopulationPacksString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("CaptivesString")]
        public string CaptivesString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("MovingPersonsString")]
        public string MovingPersonsString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("NoFactionPersonsString")]
        public string NoFactionPersonsString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("NoFactionMovingPersonsString")]
        public string NoFactionMovingPersonsString { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("feiziliebiaoString")]
        public string feiziliebiaoString { get; set; }
        
        // Private members
        [JsonInclude]
        [JsonPropertyName("AutoHiring")]
        public bool AutoHiring { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("AutoRewarding")]
        public bool AutoRewarding { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("AutoSearching")]
        public bool AutoSearching { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("AutoWorking")]
        public bool AutoWorking { get; set; }
        
        // 🔥 V15 修复：添加 FacilityEnabled 字段，避免读档后设施增益丢失
        [JsonInclude]
        [JsonPropertyName("FacilityEnabled")]
        public bool FacilityEnabled { get; set; }
        
        // 🔥 新增：建筑增益字段（修复统治变成0的问题）
        
        // 上限增益
        [JsonInclude]
        [JsonPropertyName("IncrementOfAgricultureCeiling")]
        public int IncrementOfAgricultureCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfCommerceCeiling")]
        public int IncrementOfCommerceCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfDominationCeiling")]
        public int IncrementOfDominationCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfEnduranceCeiling")]
        public int IncrementOfEnduranceCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfMoraleCeiling")]
        public int IncrementOfMoraleCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfTechnologyCeiling")]
        public int IncrementOfTechnologyCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfFundCeiling")]
        public int IncrementOfFundCeiling { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfFoodCeiling")]
        public int IncrementOfFoodCeiling { get; set; }
        
        // 每日增益
        [JsonInclude]
        [JsonPropertyName("IncrementOfAgriculturePerDay")]
        public int IncrementOfAgriculturePerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfCommercePerDay")]
        public int IncrementOfCommercePerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfDominationPerDay")]
        public int IncrementOfDominationPerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfEndurancePerDay")]
        public int IncrementOfEndurancePerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfMoralePerDay")]
        public int IncrementOfMoralePerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfTechnologyPerDay")]
        public int IncrementOfTechnologyPerDay { get; set; }
        
        // 每月增益
        [JsonInclude]
        [JsonPropertyName("IncrementOfMonthFood")]
        public int IncrementOfMonthFood { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfMonthFund")]
        public int IncrementOfMonthFund { get; set; }
        
        // 其他增益
        [JsonInclude]
        [JsonPropertyName("IncrementOfCombativityInViewArea")]
        public int IncrementOfCombativityInViewArea { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfFacilityPositionCount")]
        public int IncrementOfFacilityPositionCount { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfFactionReputationPerDay")]
        public int IncrementOfFactionReputationPerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfFactionTechniquePointPerDay")]
        public int IncrementOfFactionTechniquePointPerDay { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("IncrementOfViewRadius")]
        public int IncrementOfViewRadius { get; set; }
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// City-specific DTO (derived from ArchitectureDTO)
    /// </summary>
    public class CityDTO : ArchitectureDTO
    {
        // City-specific properties can be added here
        public int DevelopmentLevel { get; set; }
    }
    
    /// <summary>
    /// Port-specific DTO (derived from ArchitectureDTO)
    /// </summary>
    public class PortDTO : ArchitectureDTO
    {
        public int ShipCapacity { get; set; }
    }
    
    /// <summary>
    /// Gate-specific DTO (derived from ArchitectureDTO)
    /// </summary>
    public class GateDTO : ArchitectureDTO
    {
        public int DefenseBonus { get; set; }
    }
}
