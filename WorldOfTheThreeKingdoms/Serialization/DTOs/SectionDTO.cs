using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Section
    /// </summary>
    public class SectionDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        // Reference fields
        public int BelongedFactionID { get; set; } = -1;
        public int AIDetailID { get; set; } = -1;
        
        // 🔥 向后兼容：旧剧本使用 AIDetailIDString
        // 日期：2026-03-17
        // 原因：旧剧本 JSON 中使用 "AIDetailIDString" 而不是 "AIDetailID"
        // 解决：添加别名属性，映射到 AIDetailID
        [JsonPropertyName("AIDetailIDString")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AIDetailIDString_Legacy
        {
            get => null;  // 只用于读取，不写入
            set
            {
                if (value.HasValue)
                {
                    AIDetailID = value.Value;
                }
            }
        }
        
        // 🔥 修复：添加都督ID字段
        // 日期：2026-02-17
        // 问题：SectionLeaderID 没有被序列化，导致都督丢失
        public int SectionLeaderID { get; set; } = -1;
        
        // 🔥 修复：添加方向目标ID字段
        // 日期：2026-02-17
        // 问题：OrientationXXXID 没有被序列化，导致军区方向目标丢失
        public int OrientationFactionID { get; set; } = -1;
        public int OrientationSectionID { get; set; } = -1;
        public int OrientationStateID { get; set; } = -1;
        public int OrientationArchitectureID { get; set; } = -1;
        
        // Collection references - 使用 C# 12 集合表达式
        public List<int> ArchitectureIDs { get; set; } = [];
        
        // 🔥 修复：添加AI冷却相关字段（修复存档/读档后AI每回合都执行的问题）
        // 日期：2026-03-21
        // 问题：SectionDTO 缺少AI冷却字段，导致读档后冷却状态丢失，AI每回合都执行
        // 解决：添加所有AI冷却相关字段，确保存档/读档正确保存/恢复冷却状态
        public int AiCooldownCounter { get; set; }
        public bool IsDirty { get; set; }
        public bool IsInitialized { get; set; }
        public int UrgentCooldown { get; set; }
        public int CreationTurn { get; set; } = -1;
        
        // Backward compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ArchitecturesString { get; set; }
        
        // MOD extension data
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
