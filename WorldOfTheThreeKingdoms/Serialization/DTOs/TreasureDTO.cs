using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Treasure
    /// </summary>
    public class TreasureDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Worth { get; set; }
        public bool Available { get; set; }
        
        // 🔥 2026-02-17 修复：添加图片和基础属性字段
        public int Pic { get; set; }
        public int AppearYear { get; set; }
        public int TreasureGroup { get; set; }
        public int Durability { get; set; }
        
        // Reference fields
        // 🔥 2026-03-18 修复：JSON 中使用 "BelongedPersonIDString" 而不是 "BelongedPersonID"
        [JsonPropertyName("BelongedPersonIDString")]
        public int BelongedPersonID { get; set; } = -1;
        
        // 🔥 2026-03-18 修复：JSON 中使用 "HidePlaceIDString" 而不是 "HidePlaceID"
        [JsonPropertyName("HidePlaceIDString")]
        public int HidePlaceID { get; set; } = -1;
        
        // 🔥 2026-02-17 修复：添加 InfluencesString 用于序列化宝物效果
        public string InfluencesString { get; set; }
        
        // Treasure effects (legacy fields, kept for backward compatibility)
        public int CommandIncrement { get; set; }
        public int StrengthIncrement { get; set; }
        public int IntelligenceIncrement { get; set; }
        public int PoliticsIncrement { get; set; }
        public int GlamourIncrement { get; set; }
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
