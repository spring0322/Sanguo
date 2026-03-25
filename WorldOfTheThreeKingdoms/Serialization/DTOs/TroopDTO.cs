using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Troop
    /// </summary>
    public class TroopDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        
        // Position
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int RealDestinationX { get; set; } = -1;
        public int RealDestinationY { get; set; } = -1;
        
        // Reference fields
        public int BelongedFactionID { get; set; } = -1;
        public int BelongedLegionID { get; set; } = -1;
        public int BelongedArchitectureID { get; set; } = -1;
        public int StartingArchitectureID { get; set; } = -1;  // 🔥 修复：添加缺失的出发城市ID
        public int WillArchitectureID { get; set; } = -1;
        public int WillTroopID { get; set; } = -1;
        public int TargetArchitectureID { get; set; } = -1;
        public int TargetTroopID { get; set; } = -1;
        public int LeaderID { get; set; } = -1;
        public int MilitaryID { get; set; } = -1;
        
        // 🔥 根本修复：添加控制状态字段，防止读档后AI错误控制玩家部队
        public bool Auto { get; set; }
        public bool Controllable { get; set; } = true;
        public bool ManualControl { get; set; }
        
        // 🔥 根本修复：添加AI状态字段，防止读档后部队状态错误
        public int CurrentAIState { get; set; }  // TroopAIState enum
        public bool IsRetreatLocked { get; set; }
        public int CreationTurn { get; set; } = -1;
        
        // 🔥 根本修复：添加角色字段，防止读档后战术角色丢失
        public int? AssignedRole { get; set; }  // TroopRole? enum
        public int CurrentRole { get; set; }  // TroopRole enum
        
        // Troop stats
        public int Quantity { get; set; }
        public int Morale { get; set; }
        public int Combativity { get; set; }
        public int Experience { get; set; }
        
        // 🔥 根本修复：添加粮食字段，防止读档后粮食变成0
        // 日期：2026-02-12
        // 问题：TroopDTO 缺少 Food 字段，导致保存时粮食数据丢失，读档后变成0
        public int Food { get; set; }
        
        // 🔥 根本修复：添加特技相关字段，防止读档后特技状态丢失
        // 日期：2026-02-12
        // 问题：TroopDTO 缺少特技字段，导致读档后特技剩余时间变成0，AI误判并自动触发新特技
        public int CurrentStuntIDString { get; set; }
        public int StuntDayLeft { get; set; }
        public int CurrentCombatMethodID { get; set; } = -1;
        public int CurrentStratagemID { get; set; } = -1;
        public int AutoCombatMethodID { get; set; } = -1;
        
        // Collection references
        public List<int> PersonIDs { get; set; } = [];
        
        // Backward compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PersonsString { get; set; }
        
        // 🔥 新增：部队增益字段（修复读档后战斗力变化的问题）
        
        // 基础增益
        [JsonInclude]
        public int IncrementOfAvoidSurroundedChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfRumourDay { get; set; }
        
        [JsonInclude]
        public int IncrementOfAttractDay { get; set; }
        
        [JsonInclude]
        public int IncrementOfChaosAfterSurroundAttackChance { get; set; }
        
        [JsonInclude]
        public int IncrementOfChaosDay { get; set; }
        
        [JsonInclude]
        public int IncrementOfInjuryRate { get; set; }
        
        [JsonInclude]
        public int IncrementOfInvestigateRadius { get; set; }
        
        [JsonInclude]
        public int IncrementOfMovability { get; set; }
        
        [JsonInclude]
        public int IncrementOfRationDays { get; set; }
        
        [JsonInclude]
        public int IncrementOfStratagemRadius { get; set; }
        
        [JsonInclude]
        public int IncrementPerDayOfCombativity { get; set; }
        
        [JsonInclude]
        public int IncrementPerDayOfMorale { get; set; }
        
        [JsonInclude]
        public int IncrementOfStuntDay { get; set; }
        
        [JsonInclude]
        public int IncrementOfSpeed { get; set; }
        
        // 地形适应性增益
        [JsonInclude]
        public float RateIncrementOfRateOnWater { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnCliff { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnDesert { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnForrest { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnGrassland { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnMarsh { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnMountain { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnPlain { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnRidge { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnWasteland { get; set; }
        
        [JsonInclude]
        public float RateIncrementOfTerrainRateOnWater { get; set; }
        
        // 战斗比例增益
        [JsonInclude]
        public float IncrementOffenceRate { get; set; }
        
        [JsonInclude]
        public float RateOfBoost { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfCriticalArchitectureDamage { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfCriticalDamageReceived { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfDefence { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfFireDamage { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfFireProtection { get; set; }
        
        [JsonInclude]
        public float RateOfGongxin { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfInjuryOnCriticalStrike { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfMovability { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfOffence { get; set; } = 1f;
        
        [JsonInclude]
        public float RateOfQibingDamage { get; set; } = 1f;
        
        // 其他增益
        [JsonInclude]
        public int AttackRangeIncreaseByInfluence { get; set; }
        
        [JsonInclude]
        public float InCityOffenseRate { get; set; }
        
        [JsonInclude]
        public int MovabilityByViewArea { get; set; }
        
        // 🔥 修复：部队命令字段（修复存档/读档后攻击命令丢失）
        // 日期：2026-03-21
        // 问题：TroopDTO 缺少 Mingling 字段，导致 Troop.mingling 和 Troop.Command 未被保存/恢复
        //       玩家下达攻击命令后保存，读档后命令消失
        // 解决：添加 Mingling 字段，在序列化/反序列化时保存/恢复部队命令
        public string Mingling { get; set; } = "";
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
