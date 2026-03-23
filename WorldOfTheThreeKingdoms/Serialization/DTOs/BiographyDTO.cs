using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Biography
    /// </summary>
    public class BiographyDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Brief { get; set; }
        public int FactionColor { get; set; }
        public string History { get; set; }
        public string Romance { get; set; }
        public string InGame { get; set; }
        
        // MilitaryKinds as ID list
        public List<int> MilitaryKindIDs { get; set; } = new List<int>();
        
        // Backward compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MilitaryKindsString { get; set; }
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
