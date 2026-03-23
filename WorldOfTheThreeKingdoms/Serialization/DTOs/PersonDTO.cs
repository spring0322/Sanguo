using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Person
    /// Stores only IDs for references, not full objects
    /// </summary>
    public class PersonDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string SurName { get; set; }  // 🔥 修复：添加姓氏
        public string GivenName { get; set; }
        public string CalledName { get; set; }  // 这个已经有了，就是字
        public bool Sex { get; set; }
        public int PCharacter { get; set; }
        public int Ideal { get; set; }
        public int IdealTendencyIDString { get; set; } = -1;  // 🔥 修复：出仕志向考虑ID
        
        // Basic attributes
        [JsonPropertyName("BaseCommand")]
        public int Command { get; set; }
        
        [JsonPropertyName("BaseStrength")]
        public int Strength { get; set; }
        
        [JsonPropertyName("BaseIntelligence")]
        public int Intelligence { get; set; }
        
        [JsonPropertyName("BasePolitics")]
        public int Politics { get; set; }
        
        [JsonPropertyName("BaseGlamour")]
        public int Glamour { get; set; }
        
        [JsonPropertyName("BaseBraveness")]
        public int Braveness { get; set; }
        
        [JsonPropertyName("BaseCalmness")]
        public int Calmness { get; set; }
        
        public int Ambition { get; set; }
        
        // 🔥 忠诚度相关字段（不要序列化Loyalty，它是计算属性）
        [JsonInclude]
        public int PersonalLoyalty { get; set; }
        
        [JsonInclude]
        public int TempLoyaltyChange { get; set; }
        
        // 🔥 蜜月期字段（忠诚度保底机制）
        [JsonInclude]
        public int HoneymoonMonths { get; set; }
        
        // Experience values
        public float CommandExperience { get; set; }
        public float StrengthExperience { get; set; }
        public float IntelligenceExperience { get; set; }
        public float PoliticsExperience { get; set; }
        public float GlamourExperience { get; set; }
        
        // 🔥 2026-02-18 修复：军事经验值（注意：这些属性在Person中返回int，但内部是float）
        public int BubingExperience { get; set; }
        public int NubingExperience { get; set; }
        public int QibingExperience { get; set; }
        public int ShuijunExperience { get; set; }
        public int QixieExperience { get; set; }
        public int TacticsExperience { get; set; }
        public int StratagemExperience { get; set; }
        public int InternalExperience { get; set; }
        
        // Status
        public int Status { get; set; }
        public bool Alive { get; set; }
        public bool Available { get; set; }
        public int Generation { get; set; }
        
        // 🔥 2026-02-18 修复：出场年、出生年、死亡年字段
        public int YearAvailable { get; set; }
        public int YearBorn { get; set; }
        public int YearDead { get; set; }
        
        // 🔥 2026-02-18 修复：基础属性
        public int Reputation { get; set; }
        public int Fund { get; set; }
        public int Karma { get; set; }
        public int ArrivingDays { get; set; }
        public int AvailableLocation { get; set; }
        public int DeadReason { get; set; }
        
        // 🔥 2026-02-18 修复：能力潜力
        public int CommandPotential { get; set; }
        public int StrengthPotential { get; set; }
        public int IntelligencePotential { get; set; }
        public int PoliticsPotential { get; set; }
        public int GlamourPotential { get; set; }
        
        // 🔥 2026-03-03 修复：训练方针ID
        public int TrainPolicyIDString { get; set; }
        
        // 🔥 2026-02-18 修复：状态字段
        public int Tiredness { get; set; }
        public float InjureRate { get; set; }
        public int OfficerMerit { get; set; }
        public int WorkKind { get; set; }
        public int OutsideTask { get; set; }
        public int TaskDays { get; set; }
        public bool Immortal { get; set; }
        public bool NvGuan { get; set; }
        public bool IsGeneratedChildren { get; set; }
        public int DaySinceAvailable { get; set; }
        
        // 🔥 2026-02-18 修复：统计数据
        public int YearJoin { get; set; }
        public int TroopDamageDealt { get; set; }
        public int TroopBeDamageDealt { get; set; }
        public int ArchitectureDamageDealt { get; set; }
        public int RebelCount { get; set; }
        public int ExecuteCount { get; set; }
        public int OfficerKillCount { get; set; }
        public int FleeCount { get; set; }
        public int HeldCaptiveCount { get; set; }
        public int CaptiveCount { get; set; }
        public int StratagemSuccessCount { get; set; }
        public int StratagemFailCount { get; set; }
        public int StratagemBeSuccessCount { get; set; }
        public int StratagemBeFailCount { get; set; }
        public int RoutCount { get; set; }
        public int RoutedCount { get; set; }
        
        // 🔥 2026-02-18 修复：人物特性
        public int BornRegion { get; set; }
        public int Strain { get; set; }
        public int Qualification { get; set; }
        public bool LeaderPossibility { get; set; }
        public int StrategyTendency { get; set; }
        public int ValuationOnGovernment { get; set; }
        public int ReturnedDaySince { get; set; }
        public int LastOutsideTask { get; set; }
        public bool RewardFinished { get; set; }
        public int WaitForFeiZiPeriod { get; set; }
        public string Tags { get; set; }
        
        // 🔥 2026-02-18 修复：额外字段
        public List<int> JoinFactionID { get; set; } = [];
        public int BattleSelfDamage { get; set; }
        public int NumberOfChildren { get; set; }
        
        // Reference fields (only store IDs)
        public int BelongedFactionID { get; set; } = -1;
        public int LocationArchitectureID { get; set; } = -1;
        public int LocationTroopID { get; set; } = -1;
        public int ConvincingPersonID { get; set; } = -1;
        public int BelongedCaptiveID { get; set; } = -1;  // 🔥 新增：俘虏对象 ID（2026-03-07）
        
        // Collection references (use List<int> instead of String)
        public List<int> TreasureIDs { get; set; } = [];
        public List<int> SkillIDs { get; set; } = [];
        public List<int> StuntIDs { get; set; } = [];
        public List<int> TitleIDs { get; set; } = [];
        
        // Private member serialization support
        [JsonInclude]
        public int InformationKindID { get; set; } = -1;
        
        [JsonInclude]
        public bool Huaiyun { get; set; }
        
        [JsonInclude]
        public int HuaiyunTianshu { get; set; } = -1;
        
        [JsonInclude]
        public bool ManualStudy { get; set; }
        
        [JsonInclude]
        public List<int> ClosePersons { get; set; } = [];
        
        [JsonInclude]
        public List<int> HatedPersons { get; set; } = [];
        
        // 🔥 修复：头像和列传字段
        [JsonInclude]
        public int PictureIndex { get; set; }
        
        [JsonInclude]
        public int PersonBiographyID { get; set; } = -1;
        
        // 🔥 新增：能力增益字段（修复统治变成0的问题）
        [JsonInclude]
        public int IncrementOfAgricultureAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfChallengeWinningChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfCommerceAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfControversyWinningChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfDominationAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfEnduranceAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfMoraleAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfRecruitmentAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfSpyDays { get; set; }
        
        [JsonInclude]
        public int IncrementOfTechnologyAbility { get; set; }
        
        [JsonInclude]
        public int IncrementOfTrainingAbility { get; set; }
        
        // 🔥 新增：能力比例增益字段
        [JsonInclude]
        public int RadiusIncrementOfInformation { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfAgricultureAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfCommerceAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfConvince { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfDestroy { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfDominationAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfEnduranceAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfGossip { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfInstigate { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfJailBreakAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfMoraleAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfRecruitmentAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfSearch { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTechnologyAbility { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTrainingAbility { get; set; }
        
        // 🔥 新增：能力倍数字段
        [JsonInclude]
        public int MultipleOfAgricultureReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfAgricultureTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfCommerceReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfCommerceTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfDominationReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfDominationTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfEnduranceReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfEnduranceTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfMoraleReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfMoraleTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfRecruitmentReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfRecruitmentTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfTechnologyReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfTechnologyTechniquePoint { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfTrainingReputation { get; set; } = 1;
        
        [JsonInclude]
        public int MultipleOfTrainingTechniquePoint { get; set; } = 1;
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
