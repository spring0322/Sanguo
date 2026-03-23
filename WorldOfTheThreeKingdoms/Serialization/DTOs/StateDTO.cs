using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for State (州域)
    /// </summary>
    public class StateDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        // 州治所（州的行政中心）
        public int StateAdminID { get; set; }
        
        // 所属地域
        public int LinkedRegionID { get; set; }
        
        // 建筑列表（ID）
        public List<int> ArchitectureIDs { get; set; } = [];
        
        // 相邻州域列表（ID）
        public List<int> ContactStateIDs { get; set; } = [];
    }
}
