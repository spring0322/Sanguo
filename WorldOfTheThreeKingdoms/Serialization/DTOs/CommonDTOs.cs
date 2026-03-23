using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Region
    /// </summary>
    public class RegionDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Routeway
    /// </summary>
    public class RoutewayDTO
    {
        public int ID { get; set; }
        public int StartArchitectureID { get; set; }
        public int EndArchitectureID { get; set; }
        public int Level { get; set; }
        public bool Building { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Military
    /// </summary>
    public class MilitaryDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int KindID { get; set; }
        public int BelongedArchitectureID { get; set; } = -1;
        public int BelongedFactionID { get; set; } = -1;
        public int Quantity { get; set; }
        public int Morale { get; set; }
        public int Combativity { get; set; }
        public int Experience { get; set; }
        
        // 🔥 根本修复：添加 ShelledMilitaryID 字段
        // 日期：2026-02-11
        // 问题：ShelledMilitaryID 从未被序列化，导致加载后默认为 0，形成意外的自引用
        // 解决：在 DTO 中添加此字段，确保正确保存和加载
        public int ShelledMilitaryID { get; set; } = 0;
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Facility
    /// </summary>
    public class FacilityDTO
    {
        public int ID { get; set; }
        public int KindID { get; set; }
        public int BelongedArchitectureID { get; set; } = -1;
        public int Endurance { get; set; }
        public bool MainFacility { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Information
    /// </summary>
    public class InformationDTO
    {
        public int ID { get; set; }
        public int KindID { get; set; }
        public int BelongedFactionID { get; set; } = -1;
        public int BelongedArchitectureID { get; set; } = -1;  // 🔥 新增：归属建筑
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Level { get; set; }
        public bool Oblique { get; set; }                      // 🔥 新增：斜向范围
        public int Radius { get; set; }                        // 🔥 新增：情报半径
        public int DayCost { get; set; }                       // 🔥 新增：每日消耗
        public int DaysLeft { get; set; }                      // 🔥 新增：剩余天数
        public int DaysStarted { get; set; }                   // 🔥 新增：开始天数
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for TroopEvent
    /// </summary>
    public class TroopEventDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        // 🔥 2026-03-16 修复：Happened 在 TroopEvent.cs 中是 bool 类型
        // 问题：TroopEventDTO 错误定义为 int，与实际类型不匹配
        public bool Happened { get; set; }
        
        public bool Repeatable { get; set; }
        
        // 🔥 2026-03-06 修复：添加所有需要序列化的字段
        public int AfterEventHappened { get; set; }
        public int CheckArea { get; set; }
        public string ConditionsString { get; set; }
        public string DialogString { get; set; }
        public string EffectAreasString { get; set; }
        public string EffectPersonsString { get; set; }
        public int LaunchPersonString { get; set; }
        public string SelfEffectsString { get; set; }
        public string TargetPersonsString { get; set; }
        public string Image { get; set; }
        public string Sound { get; set; }
        public string TryToShowString { get; set; }
        public int HappenChance { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Captive
    /// </summary>
    public class CaptiveDTO
    {
        public int ID { get; set; }
        public int CaptivePersonID { get; set; }
        public int BelongedFactionID { get; set; } = -1;
        public int LocationArchitectureID { get; set; } = -1;
        public int RansomArchitectureID { get; set; } = -1;
        
        // 🔥 2026-03-06 修复：添加缺失字段
        public int RansomArriveDays { get; set; }
        public int RansomFund { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for Event
    /// </summary>
    public class EventDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        // 🔥 2026-03-16 修复：happened 在 Event.cs 中是 bool 类型，JSON 中也是 bool
        // 问题：EventDTO 错误定义为 int，导致反序列化失败
        // 错误信息：The JSON value could not be converted to System.Int32. Path: $[0].happened
        public bool Happened { get; set; }
        
        public bool Repeatable { get; set; }
        public int LaunchYear { get; set; }
        
        // 🔥 2026-03-06 修复：添加所有需要序列化的字符串字段
        // 这些字段在AfterLoadSaveFile中会被LoadXXXFromString方法解析
        public int AfterEventHappened { get; set; }
        public int HappenChance { get; set; }
        public string NextScenario { get; set; }
        public string PersonString { get; set; }
        public string PersonCondString { get; set; }
        public string ArchitectureString { get; set; }
        public string ArchitectureCondString { get; set; }
        public string FactionString { get; set; }
        public string FactionCondString { get; set; }
        public string DialogString { get; set; }
        public string EffectString { get; set; }
        public string YesDialogString { get; set; }
        public string NoDialogString { get; set; }
        public string YesEffectString { get; set; }
        public string NoEffectString { get; set; }
        public string ArchitectureEffectString { get; set; }
        public string FactionEffectIDString { get; set; }
        public string YesArchitectureEffectString { get; set; }
        public string NoArchitectureEffectString { get; set; }
        public string ScenBiographyString { get; set; }
        public string Image { get; set; }
        public string Sound { get; set; }
        public bool GloballyDisplayed { get; set; }
        public int StartMonth { get; set; }
        public int EndYear { get; set; }
        public int EndMonth { get; set; }
        public bool Minor { get; set; }
        public string TryToShowString { get; set; }
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for PersonIDRelation
    /// </summary>
    public class PersonIDRelationDTO
    {
        public int PersonID1 { get; set; }
        public int PersonID2 { get; set; }
        public int Relation { get; set; }
    }
}
