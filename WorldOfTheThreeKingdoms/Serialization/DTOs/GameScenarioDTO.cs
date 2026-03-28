#nullable disable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WorldOfTheThreeKingdoms.Serialization.Converters;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for GameScenario
    /// Used in the three-phase serialization architecture (Save Data → Load Data → Link References)
    /// </summary>
    public class GameScenarioDTO
    {
        public int Version { get; set; } = 1;
        public string ScenarioTitle { get; set; }
        public string ScenarioDescription { get; set; }
        public string MOD { get; set; }
        public int CurrentTurn { get; set; }
        
        // 🔥 关键修复：支持嵌套的 Date 对象（双向序列化）
        // 日期：2026-03-20
        // 问题：DateObject getter 返回 null，导致保存时不序列化 Date 对象，读档时无法反序列化
        // 解决：getter 返回实际的 DateDTO 对象，setter 展开到顶层属性（双向兼容）
        [JsonPropertyName("Date")]
        public DateDTO? DateObject
        {
            get => new DateDTO { Year = Year, Month = Month, Day = Day };  // 🔥 序列化时生成 Date 对象
            set
            {
                if (value != null)
                {
                    Year = value.Year;
                    Month = value.Month;
                    Day = value.Day;
                }
            }
        }
        
        // Date information (顶层属性，用于代码访问)
        [JsonIgnore]  // 反序列化时忽略，由 DateObject 设置
        public int Year { get; set; }
        
        [JsonIgnore]
        public int Month { get; set; }
        
        [JsonIgnore]
        public int Day { get; set; }
        
        public int DaySince { get; set; }
        
        // Player information
        public List<int> PlayerList { get; set; } = new List<int>();
        public string CurrentPlayerID { get; set; }
        public int ControlMode { get; set; }
        public string PlayerInfo { get; set; }
        
        // Game time tracking
        public int GameTime { get; set; }
        
        // Collections of game objects (stored as DTOs)
        // 🔥 向后兼容修复：支持旧格式（GameObjectList 包装）
        // 日期：2026-03-16
        // 问题：剧本文件使用 { "GameObjects": [...] } 格式，DTO 期望直接数组
        // 解决：使用 GameObjectListConverter 自动处理两种格式
        [JsonConverter(typeof(GameObjectListConverter<PersonDTO>))]
        public List<PersonDTO> Persons { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<FactionDTO>))]
        public List<FactionDTO> Factions { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<ArchitectureDTO>))]
        public List<ArchitectureDTO> Architectures { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<TreasureDTO>))]
        public List<TreasureDTO> Treasures { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<LegionDTO>))]
        public List<LegionDTO> Legions { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<TroopDTO>))]
        public List<TroopDTO> Troops { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<SectionDTO>))]
        public List<SectionDTO> Sections { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<RegionDTO>))]
        public List<RegionDTO> Regions { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<StateDTO>))]
        public List<StateDTO> States { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<RoutewayDTO>))]
        public List<RoutewayDTO> Routeways { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<MilitaryDTO>))]
        public List<MilitaryDTO> Militaries { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<FacilityDTO>))]
        public List<FacilityDTO> Facilities { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<InformationDTO>))]
        public List<InformationDTO> Informations { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<TroopEventDTO>))]
        public List<TroopEventDTO> TroopEvents { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<CaptiveDTO>))]
        public List<CaptiveDTO> Captives { get; set; } = [];
        
        [JsonConverter(typeof(GameObjectListConverter<EventDTO>))]
        public List<EventDTO> AllEvents { get; set; } = [];
        
        // 🔥 修复：添加Biography序列化
        public List<BiographyDTO> Biographies { get; set; } = new List<BiographyDTO>();
        
        // Relationship data (stored as ID mappings)
        public Dictionary<int, int> FatherIds { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> MotherIds { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> SpouseIds { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int[]> BrotherIds { get; set; } = new Dictionary<int, int[]>();
        public Dictionary<int, int[]> SuoshuIds { get; set; } = new Dictionary<int, int[]>();
        public Dictionary<int, int[]> CloseIds { get; set; } = new Dictionary<int, int[]>();
        public Dictionary<int, int[]> HatedIds { get; set; } = new Dictionary<int, int[]>();
        public Dictionary<int, int> MarriageGranterId { get; set; } = new Dictionary<int, int>();
        public List<PersonIDRelationDTO> PersonRelationIds { get; set; } = new List<PersonIDRelationDTO>();
        
        // 🔥 关键修复：添加地图数据
        public MapDTO ScenarioMap { get; set; }
        // Configuration flags
        public bool UsingOwnCommonData { get; set; }
        
        // MOD extension data support
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
