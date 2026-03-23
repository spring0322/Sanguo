using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for Map
    /// 🔥 2026-03-16 修复：支持旧格式（MapDimensions 对象）和新格式（MapDimensionsX/Y）
    /// </summary>
    public class MapDTO
    {
        public string MapName { get; set; }
        public string MapDataString { get; set; }
        
        // 🔥 新格式：分开的 X 和 Y 字段
        public int MapDimensionsX { get; set; }
        public int MapDimensionsY { get; set; }
        
        // 🔥 旧格式：MapDimensions 对象（向后兼容）
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Point? MapDimensions { get; set; }
        
        // 🔥 旧格式：JumpPosition 对象（向后兼容）
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Point? JumpPosition { get; set; }
        
        // 🔥 新格式：分开的 JumpPosition 字段
        public int JumpPositionX { get; set; }
        public int JumpPositionY { get; set; }
        
        public int TileWidthMin { get; set; }
        public int TileWidthMax { get; set; }
    }
}
