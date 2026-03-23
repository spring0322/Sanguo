using System.Collections.Generic;
using System.Text.Json.Serialization;
using GameObjects;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Legion
    /// </summary>
    public class LegionDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        // Reference fields
        public int BelongedFactionID { get; set; } = -1;
        public int LeaderID { get; set; } = -1;
        public LegionKind Kind { get; set; } = LegionKind.AI;
        public LegionMission Mission { get; set; } = LegionMission.None;
        public int StartArchitectureID { get; set; } = -1;
        public int WillArchitectureID { get; set; } = -1;
        public int TargetArchitectureID { get; set; } = -1;
        
        // Collection references
        public List<int> TroopIDs { get; set; } = new List<int>();
        
        // Backward compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TroopsString { get; set; }
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
