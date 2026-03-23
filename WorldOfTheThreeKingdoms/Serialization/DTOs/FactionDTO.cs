using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Faction
    /// </summary>
    public class FactionDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ColorIndex { get; set; }
        
        // Leader reference (ID only)
        public int LeaderID { get; set; } = -1;
        
        // Advisor reference (ID only)
        // 🔥 根本修复：添加 AdvisorID 字段
        // 日期：2026-03-18
        // 问题：任命军师后保存读档，军师丢失
        // 原因：FactionDTO 缺少 AdvisorID 字段，导致序列化时未保存军师ID
        public int AdvisorID { get; set; } = -1;
        
        // Capital reference (ID only)
        public int CapitalID { get; set; } = -1;
        
        // Collection references (use List<int>)
        public List<int> ArchitectureIDs { get; set; } = [];
        public List<int> PersonIDs { get; set; } = [];
        public List<int> MilitaryIDs { get; set; } = [];
        public List<int> LegionIDs { get; set; } = [];
        public List<int> TroopIDs { get; set; } = [];
        public List<int> SectionIDs { get; set; } = [];
        public List<int> TechniqueIDs { get; set; } = [];
        
        // Backward compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ArchitecturesString { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PersonsString { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MilitariesString { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LegionsString { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TroopsString { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SectionsString { get; set; }
        
        // 🔥 根本修复：添加 AvailableTechniquesString 字段
        // 日期：2026-02-17
        // 问题：读档后势力技巧不显示为已拥有状态
        // 原因：FactionDTO 缺少 AvailableTechniquesString 字段
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AvailableTechniquesString { get; set; }
        
        // 🔥 根本修复：添加 BaseMilitaryKindsString 字段
        // 日期：2026-03-18
        // 问题：读档后势力兵种丢失，导致"新编"按钮灰色
        // 原因：FactionDTO 缺少 BaseMilitaryKindsString 字段，导致序列化时未保存兵种数据
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string BaseMilitaryKindsString { get; set; }
        
        // 🔥 根本修复：添加 InformationsString 字段
        // 日期：2026-03-20
        // 问题：读档后势力情报数据丢失（已探索区域、已知敌军位置）
        // 原因：FactionDTO 缺少 InformationsString 字段，导致序列化时未保存情报数据
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string InformationsString { get; set; }
        
        // 🔥 根本修复：添加 TransferingMilitariesString 字段
        // 日期：2026-03-20
        // 问题：读档后运输中的部队丢失
        // 原因：FactionDTO 缺少 TransferingMilitariesString 字段，导致序列化时未保存运输中的部队
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TransferingMilitariesString { get; set; }
        
        // 🔥 根本修复：添加 RoutewaysString 字段
        // 日期：2026-03-20
        // 问题：读档后运输路线丢失
        // 原因：FactionDTO 缺少 RoutewaysString 字段，导致序列化时未保存运输路线
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RoutewaysString { get; set; }
        
        // 🔥 根本修复：添加 GetGeneratorPersonCountString 字段
        // 日期：2026-03-20
        // 问题：读档后武将生成器计数重置
        // 原因：FactionDTO 缺少 GetGeneratorPersonCountString 字段
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string GetGeneratorPersonCountString { get; set; }
        
        // 🔥 根本修复：添加技巧研究相关字段
        // 日期：2026-03-20
        [JsonInclude]
        public int PlanTechniqueString { get; set; }
        
        [JsonInclude]
        public List<int> PreferredTechniqueKinds { get; set; } = [];
        
        // 🔥 根本修复：添加地图残差字段（路径查找优化）
        // 日期：2026-03-20
        [JsonInclude]
        public int SecondTierXResidue { get; set; }
        
        [JsonInclude]
        public int SecondTierYResidue { get; set; }
        
        [JsonInclude]
        public int ThirdTierXResidue { get; set; }
        
        [JsonInclude]
        public int ThirdTierYResidue { get; set; }
        
        // 🔥 根本修复：添加军师建议系统字段
        // 日期：2026-03-20
        [JsonInclude]
        public int CurrentRoundSuggestion { get; set; }
        
        [JsonInclude]
        public int LastSuggestionCheckTurn { get; set; } = -1;
        
        [JsonInclude]
        public int LastTalentRecommendYear { get; set; }
        
        // Faction state
        public int Reputation { get; set; }
        public int TechniquePoint { get; set; }
        public bool IsAlien { get; set; }
        
        // 🔥 根本修复：添加势力官爵和朝廷贡献度字段
        // 日期：2026-03-13
        // 问题：读档后势力官爵（公侯伯子男）和朝廷贡献度丢失
        // 原因：FactionDTO 缺少 guanjue 和 chaotinggongxiandu 字段
        public int Guanjue { get; set; }
        public int Chaotinggongxiandu { get; set; }
        
        // Private members with JsonInclude
        [JsonInclude]
        public int PrinceID { get; set; } = -1;
        
        [JsonInclude]
        public int ZhaoxianFailureCount { get; set; }
        
        [JsonInclude]
        public int YearOfficialLimit { get; set; }
        
        // 🔥 新增：势力增益字段（修复读档后势力能力变化的问题）
        
        // 战斗增益
        [JsonInclude]
        public int IncrementOfAntiCriticalStrikeChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfChaosDaysAfterPhisicalAttack { get; set; }
        
        [JsonInclude]
        public int IncrementOfCombativityCeiling { get; set; }
        
        [JsonInclude]
        public int IncrementOfCriticalStrikeChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfResistStratagemChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfRoutewayRadius { get; set; }
        
        [JsonInclude]
        public int IncrementOfRoutewayWorkforce { get; set; }
        
        [JsonInclude]
        public int IncrementOfStratagemSuccessChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfViewRadius { get; set; }
        
        // 比例增益
        [JsonInclude]
        public float RateIncrementOfTerrainRate { get; set; }
        
        [JsonInclude]
        public float RateOfCombativityRecoveryAfterAttacked { get; set; }
        
        [JsonInclude]
        public float RateOfCombativityRecoveryAfterStratagemFail { get; set; }
        
        [JsonInclude]
        public float RateOfCombativityRecoveryAfterStratagemSuccess { get; set; }
        
        [JsonInclude]
        public float RateOfFoodTransportBetweenArchitectures { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfRoutewayConsumption { get; set; } = 1f;
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
