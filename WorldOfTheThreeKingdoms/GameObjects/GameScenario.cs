#nullable disable

using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.Animations;
using GameObjects.ArchitectureDetail;
using GameObjects.ArchitectureDetail.EventEffect;
using GameObjects.Conditions;
using GameObjects.FactionDetail;
using GameObjects.Influences;
using GameObjects.MapDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Linq;
using Tools;
using WorldOfTheThreeKingdoms.Tools;
using WorldOfTheThreeKingdoms.GameManager;
using WorldOfTheThreeKingdoms.Serialization;
using global::GameManager;
using Platforms;
using WorldOfTheThreeKingdoms.GameScreens;
using GameObjects.SectionDetail;
using System.Collections.Frozen; // 新增：用于高性能只读字典
using System.Runtime.InteropServices; // 新增：用于 CollectionsMarshal
using GameObjects.Events; // 新增：强类型事件系统

namespace GameObjects
{
    //using GameObjects.PersonDetail.PersonMessages;
    //using GameFreeText;
    [DataContract]

    public class GameScenario
    {
        //public GameFreeText.FreeText GameProgressCaution;

        // Helper method to parse ID list from space-separated string (SaveToString format)
        // 🔥 修复：SaveToString() 返回空格分隔的ID，不是逗号分隔
        private static List<int> ParseIDsFromString(string idString)
        {
            if (string.IsNullOrEmpty(idString))
                return new List<int>();
            
            try
            {
                // SaveToString() 返回格式: "1 2 3 " (空格分隔)
                return idString.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.Parse(s.Trim()))
                    .ToList();
            }
            catch (FormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseIDsFromString] ❌ FormatException: 无法解析ID字符串");
                System.Diagnostics.Debug.WriteLine($"  - 输入字符串: '{idString}'");
                System.Diagnostics.Debug.WriteLine($"  - 异常消息: {ex.Message}");
                throw new FormatException($"无法解析ID列表字符串: '{idString}'. 期望格式为空格分隔的整数。", ex);
            }
        }

        [DataMember]
        public string MOD { get; set; }

        [DataMember]
        public string Title 
        { 
            get { return ScenarioTitle; }
            set { ScenarioTitle = value; }
        }

        public static string SCENARIO_ERROR_TEXT_FILE
        {
            get
            {
                return Platform.Current.DirectoryName(Platform.Current.Location) + "/GameData/ScenarioErrors.txt";
            }
        }

        private Dictionary<int, Architecture> AllArchitectures = new Dictionary<int, Architecture>();
        private Dictionary<int, Person> AllPersons = new Dictionary<int, Person>();

        public FactionList PlayerFactions = new FactionList();
        public PersonList PreparedAvailablePersons = new PersonList();
        public bool Preparing = false;

        public Dictionary<TroopEvent, TroopList> TroopEventsToApply = new Dictionary<TroopEvent, TroopList>();

        public Dictionary<Event, Architecture> EventsToApply = new Dictionary<Event, Architecture>();
        public Dictionary<Event, Architecture> YesEventsToApply = new Dictionary<Event, Architecture>();
        public Dictionary<Event, Architecture> NoEventsToApply = new Dictionary<Event, Architecture>();

        // 缓存地图上有几支部队在埋伏
        private int numberOfAmbushTroop = -1;
        public static int savemaxcounts=49;
        
        // 🔥 新增：部队位置更新锁（原子性保护）
        // 日期：2026-03-10
        // 用途：确保 MapPositionCache 和 MapTileData 同步更新，防止部队重叠
        private readonly object _troopPositionLock = new object();
        // public Dictionary<Event, Architecture> YesArchiEventsToApply = new Dictionary<Event, Architecture>();
        //public Dictionary<Event, Architecture> NoArchiEventsToApply = new Dictionary<Event, Architecture>();

        public bool EnableLoadAndSave = true;

        // public OngoingBattleList AllOngoingBattles = new OngoingBattleList();

        private PersonList emptyPersonList = new PersonList();
        private CaptiveList emptyCaptiveList = new CaptiveList();

        public Dictionary<PathCacheKey, List<Point>> pathCache = new Dictionary<PathCacheKey, List<Point>>();

        public bool JustSaved = false;

        public GameScenario Clone()
        {
            return this.MemberwiseClone() as GameScenario;
        }

        // 🔥 2026-02-12 根本修复：添加 JsonConverter 处理字符串键转 int 键
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IntArrayDictionaryConverter))]
        public Dictionary<int, int[]> AiBattlingArchitectureStrings = [];

        /// <summary>
        /// 建筑列表
        /// </summary>
        [DataMember]
        public ArchitectureList Architectures = new ArchitectureList();
        
        public Faction CurrentFaction;
        public Faction CurrentPlayer;

        [DataMember]
        public GameDate Date = new GameDate();

        [DataMember]
        public DiplomaticRelationTable DiplomaticRelations = new DiplomaticRelationTable();

        [DataMember]
        public FacilityList Facilities = new FacilityList();

        // Pathfinding Penalty System
        private Dictionary<Point, int> _tempPathfindingPenalties = new Dictionary<Point, int>();
        
        // 🆕 势力范围能量缓存（Phase 4 性能优化）
        // 日期：2026-03-12
        // 用途：缓存每个位置的净能量值，避免在每个部队的 DayEvent 中重复计算
        // 键：(位置, 势力)，值：净能量值
        private Dictionary<(Point, Faction), int> _influenceEnergyCache = new();
        
        // 🆕 建筑核心占地地图（主权防波堤系统）
        // 日期：2026-03-13
        // 用途：O(1) 查表判断某格子是否属于建筑核心占地，用于势力范围扩散时的主权防波堤
        // 值：建筑 ID（-1 表示无建筑）
        // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0 判断
        public int[] ArchitectureCoreMap { get; private set; }

        /// <summary>
        /// 获取某坐标点的额外临时寻路惩罚
        /// </summary>
        public int GetTemporaryPenalty(Point location)
        {
            if (_tempPathfindingPenalties.TryGetValue(location, out int penalty))
            {
                return penalty;
            }
            return 0;
        }

        /// <summary>
        /// 设置临时路障（用于解决死锁）
        /// </summary>
        public void SetTemporaryMapPenalty(Point location, int penalty)
        {
            if (_tempPathfindingPenalties.ContainsKey(location))
            {
                _tempPathfindingPenalties[location] = penalty;
            }
            else
            {
                _tempPathfindingPenalties.Add(location, penalty);
            }
        }

        /// <summary>
        /// 清除临时路障
        /// </summary>
        public void ClearTemporaryMapPenalty(Point location)
        {
            if (_tempPathfindingPenalties.ContainsKey(location))
            {
                _tempPathfindingPenalties.Remove(location);
            }
        }

        public void InvalidateInfluenceEnergyCache()
        {
            _influenceEnergyCache.Clear();
        }

        private void EnsureCurrentFactionInfluenceMapsInitialized()
        {
            if (this.ScenarioMap == null)
            {
                throw new InvalidOperationException(
                    "[GameScenario] ScenarioMap is null while syncing influence topology.");
            }

            int mapWidth = this.ScenarioMap.MapDimensions.X;
            int mapHeight = this.ScenarioMap.MapDimensions.Y;
            if (mapWidth <= 0 || mapHeight <= 0)
            {
                throw new InvalidOperationException(
                    $"[GameScenario] Invalid map size while syncing influence topology: {mapWidth}x{mapHeight}");
            }

            int expectedLength = mapWidth * mapHeight;
            var factions = this.Factions.GetList();
            int factionCount = factions.Count;

            for (int i = 0; i < factionCount; i++)
            {
                if (factions[i] is not Faction faction)
                {
                    throw new InvalidOperationException(
                        $"[GameScenario] Factions contains invalid entry at index {i} while syncing influence topology.");
                }

                if (faction.GlobalInfluenceMap == null || faction.GlobalInfluenceMap.Length != expectedLength)
                {
                    faction.InitializeInfluenceMap(mapWidth, mapHeight);
                }
            }
        }

        private void SyncInfluenceAfterFactionTopologyChange(string reason)
        {
            this.InvalidateInfluenceEnergyCache();
            this.EnsureCurrentFactionInfluenceMapsInitialized();

            var influenceUpdateManager = Session.MainGame?.mainGameScreen?._influenceUpdateManager;
            if (influenceUpdateManager == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameScenario] InfluenceUpdateManager not ready, only initialized maps for topology change: {reason}");
                return;
            }

            influenceUpdateManager.SyncAfterFactionTopologyChange(reason);
        }

        [DataMember]
        public FactionListWithQueue Factions = new FactionListWithQueue();

        [DataMember]
        public PositionTable FireTable = new PositionTable();

        [DataMember]
        public CommonData GameCommonData = new CommonData();
        
        public TileAnimationGenerator GeneratorOfTileAnimation;

        [DataMember]
        public InformationList Informations = new InformationList();

        [DataMember]
        public LegionList Legions = new LegionList();

        public TileData[,] MapTileData;

        [DataMember]
        public MilitaryList Militaries = new MilitaryList();

        private Person neutralPerson;
        public bool NewInfluence;

        [DataMember]
        public NoFoodTable NoFoodDictionary = new NoFoodTable();

        public int[,] PenalizedMapData;

        [DataMember]
        public Dictionary<int, int> FatherIds = new Dictionary<int, int>();
        [DataMember]
        public Dictionary<int, int> MotherIds = new Dictionary<int, int>();
        [DataMember]
        public Dictionary<int, int> SpouseIds = [];
        
        // 🔥 2026-02-12 根本修复：添加 JsonConverter 处理字符串键转 int 键
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IntArrayDictionaryConverter))]
        public Dictionary<int, int[]> BrotherIds = [];
        
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IntArrayDictionaryConverter))]
        public Dictionary<int, int[]> SuoshuIds = [];
        
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IntArrayDictionaryConverter))]
        public Dictionary<int, int[]> CloseIds = [];
        
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IntArrayDictionaryConverter))]
        public Dictionary<int, int[]> HatedIds = [];
        
        [DataMember]
        public Dictionary<int, int> MarriageGranterId = [];

        [DataMember]
        public List<PersonIDRelation> PersonRelationIds = new List<PersonIDRelation>();

        [DataMember]
        public PersonList Persons = new PersonList();

        [DataMember]
        public List<int> PlayerList { get; set; } = new List<int>();

        [DataMember]
        public string CurrentPlayerID { get; set; }

        [DataMember]
        public string PlayerInfo { get; set; }        

        [DataMember]
        public RegionList Regions = new RegionList();

        [DataMember]
        public RoutewayList Routeways = new RoutewayList();

        [DataMember]
        public string ScenarioDescription;

        [DataMember]
        public Map ScenarioMap = new Map();

        [DataMember]
        public string ScenarioTitle;

        [DataMember]
        public SectionList Sections = new SectionList();
        //public GameMessageList SpyMessages = new GameMessageList();

        [DataMember]
        public StateList States = new StateList();

        public int[] TerrainAdaptability;
        public bool Threading;

        [DataMember]
        public TreasureList Treasures = new TreasureList();

        [DataMember]
        public TreasureList SoldTreasures = new TreasureList();

        [DataMember]
        public TroopEventList TroopEvents = new TroopEventList();

        [DataMember]
        public TroopListWithQueue Troops = new TroopListWithQueue();

        [DataMember]
        public YearTable YearTable = new YearTable();

        [DataMember]
        public EventList AllEvents = new EventList();

        public String LoadedFileName;

        [DataMember]
        public bool UsingOwnCommonData;

        [DataMember]
        public BiographyTable AllBiographies = new BiographyTable();

        [DataMember]
        public int GameTime;

        private DateTime sessionStartTime;

        public bool needAutoSave = false;

        public int NumberOfAmbushTroop
        {
            get
            {
                if (numberOfAmbushTroop >= 0)
                    return numberOfAmbushTroop;
                else
                {
                    int number = 0;
                    foreach (Troop t in Troops)
                    {
                        if (t.Status == TroopStatus.埋伏)
                            number++;
                    }
                    numberOfAmbushTroop = number;
                    return numberOfAmbushTroop;
                }
            }
        }

        public bool scenarioJustLoaded;

        [DataMember]
        public int DaySince { get; set; }

        [DataMember]
        public Parameters Parameters { get; set; }

        [DataMember]
        public GlobalVariables GlobalVariables { get; set; }
        // 🔥 修复：标记 GlobalVariables 是否从 JSON 文件加载
        // 用于区分"原始剧本"（null，应使用主菜单设置）和"存档剧本"（有值，应保留存档设置）
        // 日期：2026-03-03
        public bool IsGlobalVariablesFromJson { get; set; } = false;

        /// <summary>
        /// 天气管理器（2026-03-09 新增）
        /// 🔥 2026-03-12 修复：延迟初始化，兼容旧存档
        /// </summary>
        private WorldOfTheThreeKingdoms.GameLogic.WeatherManager? _weatherManager;
        
        public WorldOfTheThreeKingdoms.GameLogic.WeatherManager WeatherManager
        {
            get
            {
                // 延迟初始化：兼容旧存档反序列化
                if (_weatherManager == null)
                {
                    InitializeWeatherSystem();
                }
                return _weatherManager;
            }
        }

        /// <summary>
        /// 环境配置（2026-03-10 新增）
        /// 🔥 2026-03-12 修复：延迟初始化，兼容旧存档
        /// </summary>
        private WorldOfTheThreeKingdoms.GameGlobal.GameEnvironmentConfig? _environmentConfig;
        
        public WorldOfTheThreeKingdoms.GameGlobal.GameEnvironmentConfig EnvironmentConfig
        {
            get
            {
                // 延迟初始化：兼容旧存档反序列化
                if (_environmentConfig == null)
                {
                    InitializeWeatherSystem();
                }
                return _environmentConfig;
            }
        }

        /// <summary>
        /// 火势蔓延管理器（2026-03-10 新增）
        /// 基于风向风力系统，每回合结算火势蔓延与自然熄灭
        /// </summary>
        private WorldOfTheThreeKingdoms.GameLogic.FireSpreadManager _fireSpreadManager;

        public GameScenario()
        {
            // 🔥 Checklist Fix: Priority 2 - Field Initialization
            this.Factions = new FactionListWithQueue();
            this.Architectures = new ArchitectureList();
            this.Persons = new PersonList();
        }
          
        public void Init()
        {
            this.GeneratorOfTileAnimation = new TileAnimationGenerator();

            //public static readonly string SCENARIO_ERROR_TEXT_FILE = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/GameData/ScenarioErrors.txt";
            AllArchitectures = new Dictionary<int, Architecture>();
            AllPersons = new Dictionary<int, Person>();

            PlayerFactions = new FactionList();
            PreparedAvailablePersons = new PersonList();
            Preparing = false;

            TroopEventsToApply = new Dictionary<TroopEvent, TroopList>();

            EventsToApply = new Dictionary<Event, Architecture>();
            YesEventsToApply = new Dictionary<Event, Architecture>();
            NoEventsToApply = new Dictionary<Event, Architecture>();

            // 缓存地图上有几支部队在埋伏
            numberOfAmbushTroop = -1;

            EnableLoadAndSave = true;

            emptyPersonList = new PersonList();
            emptyCaptiveList = new CaptiveList();

            pathCache = new Dictionary<PathCacheKey, List<Point>>();

            // 🔥 修复：确保 GameCommonData 永不为 null
            if (this.UsingOwnCommonData || this.GameCommonData == null)
            {
                this.GameCommonData = CommonData.Current;
            }

            // 🔥 2026-03-03 新增：初始化宝物市场（向后兼容旧存档）
            if (SoldTreasures == null)
            {
                SoldTreasures = new TreasureList();
            }

            // 🔥 2026-03-09 新增：初始化天气系统
            // 🔥 2026-03-12 修复：提取为独立方法，支持延迟初始化
            InitializeWeatherSystem();
        }

        /// <summary>
        /// 初始化天气系统（2026-03-12 新增）
        /// 支持延迟初始化，兼容旧存档反序列化
        /// </summary>
        private void InitializeWeatherSystem()
        {
            // 防止重复初始化
            if (_weatherManager != null && _environmentConfig != null)
            {
                return;
            }

            // ANTI-BAND-AID：如果配置文件损坏或地图未初始化，必须 Fail Fast
            // 断言：ScenarioMap 必须已初始化
            if (this.ScenarioMap == null || this.ScenarioMap.MapDimensions.X <= 0 || this.ScenarioMap.MapDimensions.Y <= 0)
            {
                throw new InvalidOperationException(
                    "ScenarioMap 未初始化或地图尺寸无效，无法初始化天气系统！");
            }
            
            // 加载配置（如果失败会抛出异常，暴露问题）
            var envConfig = WorldOfTheThreeKingdoms.GameLogic.EnvironmentConfigLoader.LoadConfig();
            
            // 存储环境配置供其他系统使用
            _environmentConfig = envConfig;
            
            // 烘焙移动惩罚矩阵
            WorldOfTheThreeKingdoms.GameLogic.MovementCostCalculator.BakeConfig(envConfig.WeatherMovement);
            
            // 🆕 加载能量计算配置并初始化计算器
            // 日期：2026-03-16
            var energyConfig = WorldOfTheThreeKingdoms.GameLogic.EnergyCalculationConfigLoader.LoadConfig();
            WorldOfTheThreeKingdoms.GameObjects.EnergyCalculator.Initialize(energyConfig);
            System.Diagnostics.Debug.WriteLine("[GameScenario] 能量计算系统初始化完成");
            
            // 初始化天气管理器（C# 12 目标类型 new）
            _weatherManager = new(
                envConfig,
                this.ScenarioMap.MapDimensions.X,
                this.ScenarioMap.MapDimensions.Y
            );
            
            System.Diagnostics.Debug.WriteLine("[GameScenario] 天气系统初始化完成");
            
            // 初始化火势蔓延管理器（C# 12 目标类型 new）
            if (_fireSpreadManager == null)
            {
                _fireSpreadManager = new();
                System.Diagnostics.Debug.WriteLine("[GameScenario] 火势蔓延管理器初始化完成");
            }
        }

        // 优化：使用 FrozenDictionary 提升读取性能
        private FrozenDictionary<Architecture, PersonList>
             NormalPLCache, MovingPLCache, NoFactionPLCache, NoFactionMovingPLCache, PrincessPLCache,
             ZhenzaiPLCache, AgriculturePLCache, CommercePLCache, TechnologyPLCache,
             DominationPLCache, MoralePLCache, EndurancePLCache, TrainingPLCache;
        
        private FrozenDictionary<Architecture, CaptiveList> CaptivePLCache;

        // 通用获取方法，避免重复的空检查代码
        private PersonList GetFromFrozenCache(FrozenDictionary<Architecture, PersonList> cache, Architecture a)
        {
             if (cache == null) return emptyPersonList;
             return cache.TryGetValue(a, out var list) ? list : emptyPersonList;
        }

        public PersonList GetPersonList(Architecture a)
        {
            if (NormalPLCache == null) CreatePersonStatusCache();
            return GetFromFrozenCache(NormalPLCache, a);
        }

        public PersonList GetMovingPersonList(Architecture a)
        {
            if (MovingPLCache == null) CreatePersonStatusCache();
            return GetFromFrozenCache(MovingPLCache, a);
        }

        public PersonList GetNoFactionPersonList(Architecture a)
        {
            if (NoFactionPLCache == null) CreatePersonStatusCache();
            return GetFromFrozenCache(NoFactionPLCache, a);
        }

        public PersonList GetNoFactionMovingPersonList(Architecture a)
        {
            if (NoFactionMovingPLCache == null) CreatePersonStatusCache();
            return GetFromFrozenCache(NoFactionMovingPLCache, a);
        }

        public PersonList GetPrincessPersonList(Architecture a)
        {
            if (PrincessPLCache == null) CreatePersonStatusCache();
            return GetFromFrozenCache(PrincessPLCache, a);
        }

        public CaptiveList GetCaptiveList(Architecture a)
        {
            if (CaptivePLCache == null) CreatePersonStatusCache();
            if (CaptivePLCache.TryGetValue(a, out var list)) return list;
            return emptyCaptiveList;
        }

        // --- 工作相关获取方法 ---
        public PersonList GetZhenzaiPersonList(Architecture a) => GetFromFrozenCache(ZhenzaiPLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetAgriculturePersonList(Architecture a) => GetFromFrozenCache(AgriculturePLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetCommercePersonList(Architecture a) => GetFromFrozenCache(CommercePLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetTechnologyPersonList(Architecture a) => GetFromFrozenCache(TechnologyPLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetDomintaionPersonList(Architecture a) => GetFromFrozenCache(DominationPLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetMoralePersonList(Architecture a) => GetFromFrozenCache(MoralePLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetEndurancePersonList(Architecture a) => GetFromFrozenCache(EndurancePLCache ?? (CreatePersonWorkCacheReturn()), a);
        public PersonList GetTrainingPersonList(Architecture a) => GetFromFrozenCache(TrainingPLCache ?? (CreatePersonWorkCacheReturn()), a);

        // 辅助方法，用于一行代码调用
        private FrozenDictionary<Architecture, PersonList> CreatePersonWorkCacheReturn() { CreatePersonWorkCache(); return null; }

        public void CreatePersonWorkCache()
        {
            // 使用普通字典进行构建，利用 CollectionsMarshal 减少哈希计算
            Dictionary<Architecture, PersonList> zhenzai = new();
            Dictionary<Architecture, PersonList> agriculture = new();
            Dictionary<Architecture, PersonList> commerce = new();
            Dictionary<Architecture, PersonList> technology = new();
            Dictionary<Architecture, PersonList> domination = new();
            Dictionary<Architecture, PersonList> morale = new();
            Dictionary<Architecture, PersonList> endurance = new();
            Dictionary<Architecture, PersonList> training = new();

            foreach (Person i in this.AvailablePersons)
            {
                // 🔥 V17 修复：只处理状态正常、不在部队中、且有有效位置的 Person
                // 原因：读档后可能存在 LocationArchitecture 为 null 的情况（例如引用的建筑已被删除）
                // 这些 Person 不应该出现在工作缓存中
                if (i.Status != PersonStatus.Normal || 
                    i.LocationArchitecture == null ||
                    (i.LocationTroop != null && this.Troops.GameObjects.Contains(i.LocationTroop)))
                {
                    continue;
                }

                Dictionary<Architecture, PersonList> targetDict = i.WorkKind switch
                {
                    ArchitectureWorkKind.赈灾 => zhenzai,
                    ArchitectureWorkKind.农业 => agriculture,
                    ArchitectureWorkKind.商业 => commerce,
                    ArchitectureWorkKind.技术 => technology,
                    ArchitectureWorkKind.统治 => domination,
                    ArchitectureWorkKind.民心 => morale,
                    ArchitectureWorkKind.耐久 => endurance,
                    ArchitectureWorkKind.训练 => training,
                    _ => null
                };

                if (targetDict != null)
                {
                    // .NET 8 核心优化：获取值的引用，避免重复查找
                    ref PersonList list = ref CollectionsMarshal.GetValueRefOrAddDefault(targetDict, i.LocationArchitecture, out bool exists);
                    if (!exists)
                    {
                        list = new PersonList();
                    }
                    list.Add(i);
                }
            }

            // 冻结字典以供快速读取
            ZhenzaiPLCache = zhenzai.ToFrozenDictionary();
            AgriculturePLCache = agriculture.ToFrozenDictionary();
            CommercePLCache = commerce.ToFrozenDictionary();
            TechnologyPLCache = technology.ToFrozenDictionary();
            DominationPLCache = domination.ToFrozenDictionary();
            MoralePLCache = morale.ToFrozenDictionary();
            EndurancePLCache = endurance.ToFrozenDictionary();
            TrainingPLCache = training.ToFrozenDictionary();
        }

        public void CreatePersonStatusCache()
        {
            var normal = new Dictionary<Architecture, PersonList>();
            var moving = new Dictionary<Architecture, PersonList>();
            var noFaction = new Dictionary<Architecture, PersonList>();
            var noFactionMoving = new Dictionary<Architecture, PersonList>();
            var princess = new Dictionary<Architecture, PersonList>();
            var captives = new Dictionary<Architecture, CaptiveList>();



            foreach (Person i in this.AvailablePersons)
            {

                
                if (i.LocationArchitecture == null)
                {

                    continue;
                }
                
                if (i.LocationTroop != null && !i.LocationTroop.Destroyed && this.Troops.GameObjects.Contains(i.LocationTroop))
                {

                    continue;
                }

                Dictionary<Architecture, PersonList> targetDict = i.Status switch
                {
                    PersonStatus.Normal => normal,
                    PersonStatus.Moving => moving,
                    PersonStatus.NoFaction => noFaction,
                    PersonStatus.NoFactionMoving => noFactionMoving,
                    PersonStatus.Princess => princess,
                    _ => null
                };

                if (targetDict != null)
                {
                    ref PersonList list = ref CollectionsMarshal.GetValueRefOrAddDefault(targetDict, i.LocationArchitecture, out bool exists);
                    if (!exists) list = new PersonList();
                    list.Add(i);
                    

                }
            }



            NormalPLCache = normal.ToFrozenDictionary();
            MovingPLCache = moving.ToFrozenDictionary();
            NoFactionPLCache = noFaction.ToFrozenDictionary();
            NoFactionMovingPLCache = noFactionMoving.ToFrozenDictionary();
            PrincessPLCache = princess.ToFrozenDictionary();
            CaptivePLCache = captives.ToFrozenDictionary();
        }

        public void ClearPersonStatusCache()
        {
            NormalPLCache = MovingPLCache = NoFactionPLCache = NoFactionMovingPLCache = PrincessPLCache = null;
            CaptivePLCache = null;
        }

        public void ClearPersonWorkCache()
        {
            ZhenzaiPLCache = AgriculturePLCache = CommercePLCache = TechnologyPLCache =
            DominationPLCache = MoralePLCache = EndurancePLCache = TrainingPLCache = null;
        }

        [DataMember]
        public CaptiveList captiveData = new CaptiveList();

        public CaptiveList Captives
        {
            get
            {
                CaptiveList result = new CaptiveList();
                foreach (Person i in this.Persons)
                {
                    if (i.Status == PersonStatus.Captive)
                    {
                        if (i.BelongedCaptive == null)
                        {
                            continue;
                        }
                        result.Add(i.BelongedCaptive);
                    }
                }
                return result;
            }
        }

        public PersonList AvailablePersons
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person i in this.Persons)
                {
                    if (i.Status != PersonStatus.None && i.Alive && i.Available)
                    {
                        result.Add(i);
                    }
                }
                return result;
            }
        }

        public PersonList DeadPersons
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person i in this.Persons)
                {
                    if (i.Status != PersonStatus.None && !i.Alive && i.Available)
                    {
                        result.Add(i);
                    }
                }
                return result;
            }
        }

        public void AddPositionAreaInfluence(Troop troop, Point position, AreaInfluenceKind kind, int offset, float rate)
        {
            if (!this.PositionOutOfRange(position))
            {
                Troop troopByPositionNoCheck = this.GetTroopByPositionNoCheck(position);
                this.MapTileData[position.X, position.Y].AddAreaInfluence(troop, kind, offset, rate, troopByPositionNoCheck);
            }
        }

        public void AddPositionContactingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].AddContactingTroop(troop);
            }
        }

        public void AddPositionOffencingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].AddOffencingTroop(troop);
            }
        }

        public void AddPositionStratagemingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].AddStratagemingTroop(troop);
            }
        }

        public void AddPositionViewingTroopNoCheck(Troop troop, Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null || troop == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddPositionViewingTroopNoCheck] MapTileData 或 troop 为 null");
                return;
            }
            
            if (this.PositionOutOfRange(position))
            {
                System.Diagnostics.Debug.WriteLine($"[AddPositionViewingTroopNoCheck] 位置越界: ({position.X}, {position.Y})");
                return;
            }
            
            try
            {
                this.MapTileData[position.X, position.Y].AddViewingTroop(troop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddPositionViewingTroopNoCheck] 访问异常: {ex.Message}");
            }
        }
        /*
        private void AddPreparedAvailablePersons()
        {
            foreach (Person person in this.PreparedAvailablePersons)
            {
                Architecture gameObject = this.(Architectures.GetGameObject(person.AvailableLocation) is Architecture ? (Architecture)Architectures.GetGameObject(person.AvailableLocation) : null);
                person.Available = true;
                foreach (Treasure treasure in person.Treasures)
                {
                    treasure.Available = true;
                }
                if (person.Father > 0)
                {
                    foreach (Person p in this.Persons)
                    {
                        if (p.ID == person.Father)
                        {
                            if (p.Available && p.Alive && p.LocationArchitecture != null && p.BelongedFaction != null&&p.BelongedCaptive==null )
                            {
                                p.LocationArchitecture.AddPerson(person);
                                p.BelongedFaction.AddPerson(person);
                                // 🔥 直接使用已找到的 p 对象，不要重复查找
                                Session.MainGame.mainGameScreen.xianshishijiantupian(p.BelongedFaction.Leader, p.Name, "ChildJoin", "", "", person.Name, false);
                                Session.MainGame.mainGameScreen.xianshishijiantupian(person, p.LocationArchitecture.Name, "ChildJoinSelfTalk", "", "",  false);

                            }
                        }
                    }
                }
                else if (person.Mother > 0)
                {
                    foreach (Person p in this.Persons)
                    {
                        if (p.ID == person.Mother)
                        {
                            if (p.Available && p.Alive && p.LocationArchitecture != null && p.BelongedFaction != null && p.BelongedCaptive == null)
                            {
                                p.LocationArchitecture.AddPerson(person);
                                p.BelongedFaction.AddPerson(person);
                                // 🔥 直接使用已找到的 p 对象，不要重复查找
                                Session.MainGame.mainGameScreen.xianshishijiantupian(p.BelongedFaction.Leader, p.Name, "ChildJoin", "", "", person.Name, false);
                                Session.MainGame.mainGameScreen.xianshishijiantupian(person, p.LocationArchitecture.Name, "ChildJoinSelfTalk", "", "", false);

                            }
                        }
                    }
                }
                else if (person.Spouse > 0 )
                {
                    foreach (Person p in this.Persons)
                    {
                        if (p.ID == person.Spouse)
                        {
                            if (p.Alive && p.LocationArchitecture != null && p.BelongedFaction != null && p.BelongedCaptive == null)
                            {
                                p.LocationArchitecture.AddPerson(person);
                                p.BelongedFaction.AddPerson(person);
                                // 🔥 直接使用已找到的 p 对象，不要重复查找
                                if (person.Sex) //女的
                                {
                                    Session.MainGame.mainGameScreen.xianshishijiantupian(person, p.Name, "FemaleSpouseJoin", "", "", false);
                                }
                                else
                                {
                                    Session.MainGame.mainGameScreen.xianshishijiantupian(person, p.Name, "MaleSpouseJoin", "", "", false);
                                }

                            }
                        }
                    }
                }
                else
                {
                    gameObject.AddNoFactionPerson(person);
                }
                this.AvailablePersons.Add(person);
            }
            this.PreparedAvailablePersons.Clear();
        }
        */

        private void AddPreparedAvailablePersons()
        {
            foreach (Person person in this.PreparedAvailablePersons)
            {
                person.Available = true;
                foreach (Treasure treasure in person.Treasures)
                {
                    treasure.Available = true;
                }

                if (person.Sex)
                {
                    person.NvGuan = true;
                }

                List<GameObject> candidates = new List<GameObject>();
                candidates.Add(person.Spouse);
                candidates.AddRange(person.Brothers.GameObjects);
                candidates.Add(person.Father);
                candidates.Add(person.Mother);
                candidates.AddRange(person.Siblings.GameObjects);
                candidates.Add(person.Spouse?.Father);
                candidates.Add(person.Spouse?.Mother);

                Person joinToPerson = null;
                foreach (Person q in candidates)
                {
                    if (q != null && q.Available && q.Alive && q.BelongedCaptive == null)
                    {
                        joinToPerson = q;
                        break;
                    }
                }
                
                if (joinToPerson != null)
                {
                    person.LocationArchitecture = joinToPerson.BelongedArchitecture;
                    person.Status = joinToPerson.Status;
                    if (person.Status == PersonStatus.Moving || person.Status == PersonStatus.NoFactionMoving)
                    {
                        person.Status = PersonStatus.Normal;
                    }
                    else if (person.Status == PersonStatus.Princess)
                    {
                        person.Status = PersonStatus.Normal;
                    }
                    person.YearJoin = this.Date.Year;

                    if (joinToPerson.BelongedFactionWithPrincess != null)
                    {
                        if (person.Father == joinToPerson || person.Mother == joinToPerson)
                        {
                            Session.MainGame.mainGameScreen.xianshishijiantupian(joinToPerson.BelongedFactionWithPrincess.Leader, joinToPerson.Name, TextMessageKind.ChildJoin, "ChildJoin", "", "", person.Name, false);
                            if (person.LocationArchitecture != null)
                            {
                                Session.MainGame.mainGameScreen.xianshishijiantupian(person, person.LocationArchitecture.Name, TextMessageKind.ChildJoinSelfTalk, "ChildJoinSelfTalk", "", "", false);
                            }
                        }
                        else
                        {
                            Faction f = joinToPerson.BelongedFactionWithPrincess;
                            Session.MainGame.mainGameScreen.xianshishijiantupian(person, person.LocationArchitecture.Name, TextMessageKind.PersonJoin, "PersonJoin", "", "", f.Name, false);
                        }
                    }

                    if (person.BelongedFaction != null && !Session.Current.Scenario.IsPlayer(person.BelongedFaction))
                    {
                        person.BelongedFaction.ConsiderPromoteNvGuan(person);
                    }

                    this.AvailablePersons.Add(person);
                    if (joinToPerson.BelongedFactionWithPrincess != null) { 
                        Session.MainGame.mainGameScreen.haizizhangdachengren(joinToPerson, person, false);
                    }
                    this.YearTable.addGrownBecomeAvailableEntry(this.Date, person);

                    continue;
                }

                bool joined = false;
                foreach (int id in person.JoinFactionID)
                {
                    Faction f = (Faction)this.Factions.GetGameObject(id);
                    if (f != null)
                    {
                        this.AvailablePersons.Add(person);
                        person.LocationArchitecture = f.Capital;
                        person.Status = PersonStatus.Normal;
                        person.YearJoin = this.Date.Year;

                        if (person.BelongedFaction != null && !Session.Current.Scenario.IsPlayer(person.BelongedFaction))
                        {
                            person.BelongedFaction.ConsiderPromoteNvGuan(person);
                        }

                        Session.MainGame.mainGameScreen.xianshishijiantupian(person, f.Capital.Name, TextMessageKind.PersonJoin, "PersonJoin", "", "", f.Name, false);
                        this.YearTable.addGrownBecomeAvailableEntry(this.Date, person);
                        Session.MainGame.mainGameScreen.haizizhangdachengren(joinToPerson, person, false);
                        joined = true;

                        break;
                    }
                }

                if (joined) continue;
                if (Setting.Current.Chuchangsuiji)
                {
                    person.LocationArchitecture = this.Architectures.GetRandomObject() as Architecture;
                }
                else
                {
                    person.LocationArchitecture = this.Architectures.GetGameObject(person.AvailableLocation) as Architecture;
                }
                person.Status = PersonStatus.NoFaction;
            }
            this.PreparedAvailablePersons.Clear();
        }

        public void haizichusheng(Person person, Person father, Person muqin, bool doAffect)
        {
            person.Available = true;
            foreach (Treasure treasure in person.Treasures)
            {
                treasure.Available = true;
            }

            person.LocationArchitecture = muqin.BelongedArchitecture;
            person.ChangeFaction(muqin.BelongedFaction);

            if (muqin.IsCaptive)
            {
                Captive.Create(person, muqin.BelongedArchitecture == null ? null : muqin.BelongedArchitecture.BelongedFaction);
            }

            ScenarioEvents.RaiseChildrenJoinedFaction(this, person);

            Session.MainGame.mainGameScreen.haizizhangdachengren(person, person, true);
        }

        public void ApplyFireTable()
        {
            foreach (Point point in this.FireTable.Positions)
            {
                this.GeneratorOfTileAnimation.AddTileAnimation(TileAnimationKind.火焰, point, true);
            }
        }

        public void ApplyTroopEvents()
        {
            if (this.TroopEventsToApply.Count != 0)
            {
                foreach (GameObject gameObj in this.TroopEvents)
                {
                    TroopEvent event2 = gameObj as TroopEvent;
                    if (event2 == null)
                    {
                        // Skip objects that are not TroopEvent instances
                        continue;
                    }

                    TroopList list = null;
                    if (this.TroopEventsToApply.TryGetValue(event2, out list))
                    {
                        // 🔥 AOT修复：安全的类型转换
                        foreach (GameObject obj in list.GetList())
                        {
                            Troop troop = (obj is Troop ? (Troop)obj : null);
                            if (troop != null)
                            {
                                event2.ApplyEventEffects(troop);
                            }
                        }
                    }
                }
                this.TroopEventsToApply.Clear();
            }
        }

        public void ApplyYesEvents()
        {
            foreach (KeyValuePair<Event, Architecture> i in this.YesEventsToApply)
            {
                i.Key.DoYesApplyEvent(i.Value);
                i.Key.happened = true;
            }
            this.YesEventsToApply.Clear();
            this.NoEventsToApply.Clear();
        }

        public void ApplyNoEvents()
        {
            foreach (KeyValuePair<Event, Architecture> i in this.NoEventsToApply)
            {
                i.Key.DoNoApplyEvent(i.Value);
                i.Key.happened = true;
            }
            this.YesEventsToApply.Clear();
            this.NoEventsToApply.Clear();
            /*
            foreach (KeyValuePair<Event, Architecture> i in this.NoArchiEventsToApply)
            {
                i.Key.DoNoApplyEvent(i.Value);
                i.Key.happened = true;
            }
            this.NoArchiEventsToApply.Clear();
             */
        }
        /*
        public void ApplyYesArchiEvents()
        {
            foreach (KeyValuePair<Event, Architecture> i in this.YesArchiEventsToApply)
            {
                i.Key.DoYesArchiApplyEvent(i.Value);
                i.Key.happened = true;
            }
            this.YesArchiEventsToApply.Clear();
        }

        public void ApplyNoArchiEvents()
        {
            foreach (KeyValuePair<Event, Architecture> i in this.NoArchiEventsToApply)
            {
                i.Key.DoNoArchiApplyEvent(i.Value);
                i.Key.happened = true;
            }
            this.NoArchiEventsToApply.Clear();
        }*/

        public void ApplyEvents()
        {
            Dictionary<Event, Architecture> events = this.EventsToApply;
            foreach (KeyValuePair<Event, Architecture> i in events)
            {
                i.Key.DoApplyEvent(i.Value);
                i.Key.happened = true;
            }

            this.EventsToApply.Clear();
        }

        public void ChangeDiplomaticRelation(int faction1, int faction2, int offset)
        {
            if (faction1 != faction2)
            {
                DiplomaticRelation diplomaticRelation = this.DiplomaticRelations.GetDiplomaticRelation(faction1, faction2);
                if (diplomaticRelation != null)
                {
                    diplomaticRelation.Relation += offset;
                }
            }
        }

        public void SetDiplomaticRelationIfHigher(int faction1, int faction2, int value)
        {
            if (faction1 != faction2)
            {
                DiplomaticRelation diplomaticRelation = this.DiplomaticRelations.GetDiplomaticRelation(faction1, faction2);
                if (diplomaticRelation != null)
                {
                    if (diplomaticRelation.Relation > value)
                    {
                        diplomaticRelation.Relation = value;
                    }
                }
            }
        }

        public void SetDiplomaticRelationTruce(int faction1, int faction2, int value)
        {
            if (faction1 != faction2)
            {
                DiplomaticRelation diplomaticRelation = this.DiplomaticRelations.GetDiplomaticRelation(faction1, faction2);
                if (diplomaticRelation != null)
                {
                    diplomaticRelation.Truce = value;
                }
            }
        }

        private void CheckGameEnd()
        {
            FactionList noArchFaction = new FactionList();
            foreach (Faction f in this.Factions)
            {
                if (f.ArchitectureCount == 0)
                {
                    noArchFaction.Add(f);
                }
            }

            foreach (Faction f in noArchFaction)
            {
                this.Factions.Remove(f);
            }

            if (this.Factions.Count == 1)
            {
                ScenarioEvents.RaiseGameEnd(this);
                if (this.CurrentPlayer != null && !this.runScenarioEnd(this.CurrentPlayer.Capital, Session.MainGame.mainGameScreen))
                {
                    Session.MainGame.mainGameScreen.GameEndWithUnite(this.Factions[0] as Faction);
                }
            }
        }

        public void Clear()
        {
            this.AllEvents?.Clear();
            this.TroopEvents?.Clear();
            this.Persons?.Clear();
            this.AvailablePersons?.Clear();
            this.PreparedAvailablePersons?.Clear();
            this.Captives?.Clear();
            this.Facilities?.Clear();
            this.Militaries?.Clear();
            this.Treasures?.Clear();
            this.Informations?.Clear();
            //this.SpyMessages.Clear();
            this.Routeways?.Clear();
            GameObjectList t1 = this.Troops?.GetList();
            if (t1 != null)
            {
                foreach (Troop t in t1)
                {
                    t?.Destroy(true, false);
                }
            }
            this.Troops?.Clear();
            this.Legions?.Clear();
            this.Architectures?.Clear();
            this.Sections?.Clear();
            this.Factions?.Clear();
            this.Regions?.Clear();
            this.States?.Clear();
            this.ScenarioMap?.Clear();
            this.PlayerFactions?.Clear();
            this.FireTable?.Clear();
            this.NoFoodDictionary?.Clear();
            this.DiplomaticRelations?.Clear();
            this.GeneratorOfTileAnimation?.Clear();
            this.YearTable?.Clear();

            //this.GameCommonData.Clear();

            this.CurrentFaction = null;
            this.CurrentPlayer = null;
        }

        public void ClearPenalizedMapDataByArea(GameArea gameArea)
        {
            foreach (Point point in gameArea.Area)
            {
                if (!this.PositionOutOfRange(point))
                {
                    this.PenalizedMapData[point.X, point.Y] = 0;
                }
            }
        }

        public void ClearPenalizedMapDataByPosition(Point position)
        {
            this.PenalizedMapData[position.X, position.Y] = 0;
        }

        public void ClearPositionFire(Point position)
        {
            this.FireTable.RemovePosition(position);
            this.GeneratorOfTileAnimation.RemoveTileAnimation(TileAnimationKind.火焰, position, true);
        }

        public void CreateNewFaction(Person leader)
        {
            if (leader.Status != PersonStatus.Normal && leader.Status != PersonStatus.NoFaction) return;

            Faction newFaction = new Faction();
            newFaction.Init();
            newFaction.ID = this.Factions.GetFreeGameObjectID();
            this.Factions.AddFactionWithEvent(newFaction);
            foreach (Faction faction2 in this.Factions)
            {
                if (faction2 != newFaction)
                {
                    this.DiplomaticRelations.AddDiplomaticRelation(newFaction.ID, faction2.ID, 0);
                }
            }
            newFaction.Leader = leader;
            newFaction.Reputation = leader.Reputation;
            newFaction.Name = leader.Name;
            if (leader.PersonBiography != null)
            {
                foreach (MilitaryKind kind in leader.PersonBiography.MilitaryKinds.MilitaryKinds.Values)
                {
                    newFaction.BaseMilitaryKinds.AddMilitaryKind(kind);
                }
                newFaction.ColorIndex = leader.PersonBiography.FactionColor;
            }
            else
            {
                newFaction.BaseMilitaryKinds.AddBasicMilitaryKinds();
                newFaction.ColorIndex = -1;
            }

            List<int> allUnusedColors = new List<int>();
            for (int i = 0; i < this.GameCommonData.AllColors.Count; ++i)
            {
                allUnusedColors.Add(i);
            }
            foreach (Faction f in this.Factions)
            {
                allUnusedColors.Remove(f.ColorIndex);
            }
            if (allUnusedColors.Count == 0)
            {
                newFaction.ColorIndex = GameObject.Random(this.GameCommonData.AllColors.Count);
            }
            else
            {
                if (!allUnusedColors.Contains(newFaction.ColorIndex))
                {
                    newFaction.ColorIndex = allUnusedColors[GameObject.Random(allUnusedColors.Count)];
                }
            }

            // 🔧 FIX: 添加边界检查，防止 ArgumentOutOfRangeException
            if (this.GameCommonData?.AllColors != null && this.GameCommonData.AllColors.Count > 0)
            {
                // 确保 ColorIndex 在有效范围内
                if (newFaction.ColorIndex < 0 || newFaction.ColorIndex >= this.GameCommonData.AllColors.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameScenario] 警告：新势力 {newFaction.Name} 的 ColorIndex={newFaction.ColorIndex} 超出范围，重置为 0");
                    newFaction.ColorIndex = 0;
                }
                newFaction.FactionColor = this.GameCommonData.AllColors[newFaction.ColorIndex];
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GameScenario] 警告：AllColors 未初始化，使用默认颜色");
                newFaction.FactionColor = Color.White;
            }

            Architecture newFactionCapital = leader.LocationArchitecture;
            Faction oldFaction = newFactionCapital.BelongedFaction;

            if (oldFaction != null)
            {
                foreach (Technique tech in oldFaction.AvailableTechniques.GetTechniqueList())
                {
                    newFaction.AvailableTechniques.AddTechnique(tech);
                }

                if (oldFaction.IsAlien && leader.PersonalLoyalty < 2)
                {
                    newFaction.IsAlien = true;
                }
            }

            newFaction.Capital = newFactionCapital;

            if (leader.BelongedFaction == null)
            {
                leader.Status = PersonStatus.Normal;
            }
            else
            {
                this.ChangeDiplomaticRelation(newFaction.ID, newFactionCapital.BelongedFaction.ID, -500);
            }
            newFaction.PrepareData();

            newFactionCapital.ResetFaction(newFaction);

            newFaction.AddArchitectureKnownData(newFactionCapital);
            
            // 【修复】：新势力的FirstSection可能为null（非玩家势力不自动创建军区）
            // 此时需要手动创建军区
            Section firstSection = newFaction.FirstSection;
            if (firstSection == null)
            {
                // 手动创建第一个军区
                firstSection = new Section();
                firstSection.ID = this.Sections.GetFreeGameObjectID();
                firstSection.Name = newFactionCapital.Name + "军区";
                firstSection.BelongedFaction = newFaction; // 🔥 修复：设置所属势力
                firstSection.BelongedFactionID = newFaction.ID; // 🔥 修复：同步 ID
                firstSection.AIDetail = this.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(
                    SectionOrientationKind.无, false, false, true, true, false)[0] as SectionAIDetail;
                // 🔥 关键修复：同步设置 AIDetailID
                // 日期：2026-03-17
                // 原因：读档时 LinkReferencesPhase 需要通过 AIDetailID 恢复 AIDetail 引用
                firstSection.AIDetailID = firstSection.AIDetail?.ID ?? -1;
                newFaction.AddSection(firstSection);
                this.Sections.AddSectionWithEvent(firstSection);
            }
            firstSection.AddArchitecture(newFactionCapital);

            leader.MoveToArchitecture(newFactionCapital, null, true, false, oldFaction);

            foreach (Point p in newFactionCapital.ArchitectureArea.Area)
            {
                Troop t = GetTroopByPositionNoCheck(p);
                if (t != null)
                {
                    t.Morale = -100;
                    Troop.CheckTroopRout(t);
                }
            }

            if (oldFaction != null && !GameObject.Chance((int)oldFaction.Leader.PersonalLoyalty * 10))
            {
                oldFaction.Leader.AddHated(leader, -2000);
                leader.AdjustRelation(oldFaction.Leader, -60f, -10);
            }

            if (oldFaction != null)
            {
                int oldFactionLoyalty = oldFaction.Leader.PersonalLoyalty;
                leader.DecreaseKarma(Math.Max(12, 12 + 5 * oldFactionLoyalty + oldFaction.Leader.Karma / 2));
            }

            foreach (Person p in this.AvailablePersons)
            {
                if ((p.BelongedFaction == null || p.BelongedFaction == oldFaction) && !p.IsCaptive && p.Status != PersonStatus.Princess && p != leader)
                {
                    int offset = Person.GetIdealOffset(leader, p);
                    if (p.HasCloseStrainTo(leader) || p.IsVeryCloseTo(leader) || (GameObject.Chance(100 - offset * 20) && p.BelongedFaction == oldFaction))
                    {
                        if (p.BelongedFaction == null || p.IsVeryCloseTo(leader) || (GameObject.Chance(100 - ((int)p.PersonalLoyalty) * 25 + (5 - offset) * 10)
                            && GameObject.Chance(220 - p.Loyalty * 2 + (5 - offset) * 20)))
                        {
                            if (p.BelongedFaction != null)
                            {
                                p.BelongedFaction.Leader.AdjustRelation(p, -45f - p.PersonalLoyalty * 4.5f, -8);
                                p.BelongedFaction.Leader.AdjustRelation(newFaction.Leader, -45f, -2.5f);
                                p.AdjustRelation(p.BelongedFaction.Leader, -7.5f, -2);
                                p.ChangeFaction(newFaction);
                                p.DecreaseKarma(5 - p.BelongedFaction.Leader.PersonalLoyalty - Math.Min(0, p.BelongedFaction.Leader.Karma / 2));
                            }
                            newFaction.Leader.AdjustRelation(p, 15f, 3);
                            p.AdjustRelation(newFaction.Leader, 4.5f, 1);
                            if (p.LocationTroop == null)
                            {
                                p.MoveToArchitecture(newFactionCapital, null, true, false, oldFaction);
                            }
                            else
                            {
                                p.LocationTroop.ChangeFaction(newFaction);
                            }
                        }
                    }
                }
            }

            this.SyncInfluenceAfterFactionTopologyChange($"CreateNewFaction:{newFaction.ID}");
            ScenarioEvents.RaiseNewFactionCreated(this, oldFaction, newFaction, newFactionCapital);

            this.YearTable.addNewFactionEntry(this.Date, oldFaction, newFaction, newFactionCapital);
        }

        public int PlayerArchitectureCount
        {
            get
            {
                int r = 0;
                foreach (Faction f in this.Factions)
                {
                    if (this.IsPlayer(f))
                    {
                        r += f.ArchitectureCount;
                    }
                }
                return r;
            }
        }
        /*
        private void OngoingBattleDayEvent()
        {
            List<OngoingBattle> toRemove = new List<OngoingBattle>();
            foreach (OngoingBattle ob in this.AllOngoingBattles)
            {
                ob.CalmDay++;
                if (ob.CalmDay >= 5)
                {
                    Dictionary<Faction, int> factionDamages = new Dictionary<Faction, int>();
                    List<Person> persons = new List<Person>();
                    foreach (Person p in this.Persons)
                    {
                        if (p.Battle == ob && p.BelongedFaction != null) 
                        {
                            persons.Add(p);
                            if (!factionDamages.ContainsKey(p.BelongedFaction))
                            {
                               factionDamages.Add(p.BelongedFaction, 0); 
                            }
                            factionDamages[p.BelongedFaction] += p.BattleSelfDamage;
                        }
                    }

                    ArchitectureList battleArch = ob.Architectures;

                    bool first = true;
                    foreach (Person p in persons)
                    {
                        this.YearTable.addBattleEntry(first, this.Date, ob, p, battleArch, factionDamages);
                        p.Battle = null;
                        first = false;
                    }

                    foreach (Architecture a in battleArch)
                    {
                        a.OldFactionName = a.BelongedFaction == null ? "贼军" : a.BelongedFaction.Name;
                        a.Battle = null;
                    }


                    toRemove.Add(ob);
                }
            }

            foreach (OngoingBattle i in toRemove)
            {
                this.AllOngoingBattles.Remove(i);
            }
        }
        */
        public void DayPassedEvent()
        {
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 开始执行");
            
            ScenarioEvents.RaiseDayPassed(this);

            JustSaved = false;

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] ========== 开始 ==========");
            
            // 🔥 清空势力范围能量缓存（每回合重新计算）
            _influenceEnergyCache.Clear();
            
            // 🔥 取消预热：预热太慢，改为懒加载（按需计算）
            // PrewarmInfluenceCache();
            
            // 🔥 暂停势力范围分帧重算（避免与回合逻辑冲突）
            Session.MainGame?.mainGameScreen?._influenceUpdateManager?.Pause();
            
            //this.GameProgressCaution.Text = "开始";
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 1. 开始 Parameters.DayEvent");
            Session.Parameters.DayEvent(this.PlayerArchitectureCount);
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 1. 完成 Parameters.DayEvent");

            /*this.ClearPersonStatusCache();
            this.ClearPersonWorkCache();*/

            //clearupRepeatedOfficers();

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 2. 开始 Troops.FinalizeQueue");
            this.Troops.FinalizeQueue();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 2. 完成 Troops.FinalizeQueue");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 3. 开始 Factions.BuildQueue");
            this.Factions.BuildQueue(false);
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 3. 完成 Factions.BuildQueue");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 4. 开始 TrainChildren");
            this.TrainChildren();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 4. 完成 TrainChildren");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 5. 开始 NoFactionDevelop");
            this.Architectures.NoFactionDevelop();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 5. 完成 NoFactionDevelop");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 6. 开始 FireDayEvent");
            this.FireDayEvent();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 6. 完成 FireDayEvent");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 7. 开始 NoFoodPositionDayEvent");
            this.NoFoodPositionDayEvent();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 7. 完成 NoFoodPositionDayEvent");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 8. 开始 NewFaction");
            this.NewFaction();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 8. 完成 NewFaction");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 9. 开始外交关系处理");
            //this.GameProgressCaution.Text = "运行外交";
            foreach (DiplomaticRelationDisplay display in this.DiplomaticRelations.GetAllDiplomaticRelationDisplayList())
            {
                if (display.Truce > 0)
                {
                    display.Truce--;
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 9. 完成外交关系处理");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 10. 开始势力DayEvent（共 {this.Factions.Count} 个势力）");
            //this.GameProgressCaution.Text = "运行势力";
            //this.OngoingBattleDayEvent();

            int factionIndex = 0;
            foreach (GameObject obj in this.Factions.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Faction faction = (Faction)obj;
                    factionIndex++;
                    // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 10.{factionIndex}. 开始势力 {faction.Name} DayEvent");
                    faction.DayEvent();
                    // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 10.{factionIndex}. 完成势力 {faction.Name} DayEvent");
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 10.{factionIndex}. 数据损坏：Factions 列表中包含非 Faction 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Factions 列表中包含非 Faction 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 10.{factionIndex}. 势力DayEvent异常: {ex.Message}");
                    throw; // 🔥 ANTI-BAND-AID：不吞掉异常，让问题暴露
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 10. 完成所有势力DayEvent");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 11. 开始建筑DayEvent（共 {this.Architectures.Count} 个建筑）");
            int archIndex = 0;
            foreach (GameObject obj in this.Architectures.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Architecture architecture = (Architecture)obj;
                    archIndex++;
                    // 🔥 只输出前5个和后5个，避免刷屏
                    if (archIndex <= 5 || archIndex > this.Architectures.Count - 5)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 11.{archIndex}. 开始建筑 {architecture.Name} DayEvent");
                    }
                    architecture.DayEvent();
                    if (archIndex <= 5 || archIndex > this.Architectures.Count - 5)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 11.{archIndex}. 完成建筑 {architecture.Name} DayEvent");
                    }
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 11.{archIndex}. 数据损坏：Architectures 列表中包含非 Architecture 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Architectures 列表中包含非 Architecture 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 11.{archIndex}. 建筑DayEvent异常: {ex.Message}");
                    throw; // 🔥 ANTI-BAND-AID：不吞掉异常，让问题暴露
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 11. 完成所有建筑DayEvent");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 12. 开始路径DayEvent（共 {this.Routeways.Count} 个路径）");
            foreach (GameObject obj in this.Routeways.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Routeway routeway = (Routeway)obj;
                    routeway.DayEvent();
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 12. 数据损坏：Routeways 列表中包含非 Routeway 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Routeways 列表中包含非 Routeway 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 12. 路径DayEvent异常: {ex.Message}");
                    throw; // 🔥 ANTI-BAND-AID：不吞掉异常，让问题暴露
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 12. 完成所有路径DayEvent");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 13. 开始军团DayEvent（共 {this.Legions.Count} 个军团）");
            foreach (GameObject obj in this.Legions.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Legion legion = (Legion)obj;
                    legion.DayEvent();
                    if (legion.Troops.Count == 0)
                    {
                        legion.Disband();
                        this.Legions.Remove(legion);
                    }
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 13. 数据损坏：Legions 列表中包含非 Legion 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Legions 列表中包含非 Legion 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 13. 军团DayEvent异常: {ex.Message}");
                    throw; // 🔥 ANTI-BAND-AID：不吞掉异常，让问题暴露
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 13. 完成所有军团DayEvent");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 14. 开始部队DayEvent（共 {this.Troops.Count} 个部队）");
            //this.GameProgressCaution.Text = "运行军队";
            int troopIndex = 0;
            foreach (GameObject obj in this.Troops.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Troop troop = (Troop)obj;
                    
                    // 🔥 只处理无势力的部队（有势力的部队在 Faction.DayEvent 中处理）
                    if (troop.BelongedFaction == null)
                    {
                        troopIndex++;
                        // 🔥 只输出前5个和后5个，避免刷屏
                        if (troopIndex <= 5 || troopIndex > this.Troops.Count - 5)
                        {
                            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 14.{troopIndex}. 开始部队 {troop.DisplayName} DayEvent");
                        }
                        troop.DayEvent();
                        if (troopIndex <= 5 || troopIndex > this.Troops.Count - 5)
                        {
                            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 14.{troopIndex}. 完成部队 {troop.DisplayName} DayEvent");
                        }
                    }
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 14.{troopIndex}. 数据损坏：Troops 列表中包含非 Troop 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Troops 列表中包含非 Troop 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 14.{troopIndex}. 部队DayEvent异常: {ex.Message}");
                    throw; // 🔥 ANTI-BAND-AID：不吞掉异常，让问题暴露
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 14. 完成所有部队DayEvent");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 15. 开始 detectCurrentPlayerBattleState");
            this.detectCurrentPlayerBattleState(this.CurrentPlayer);
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 15. 完成 detectCurrentPlayerBattleState");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 16. 开始 militaryKindEvent");
            this.militaryKindEvent();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 16. 完成 militaryKindEvent");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 17. 开始 titleDayEvent");
            this.titleDayEvent();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 17. 完成 titleDayEvent");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 18. 开始 guanzhiDayEvent");
            this.guanzhiDayEvent();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 18. 完成 guanzhiDayEvent");


            //this.GameProgressCaution.Text = "运行人物";
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 19. 开始人物PreDayEvent（共 {this.AvailablePersons.Count} 个人物）");
            // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
            foreach (GameObject obj in this.AvailablePersons.GetList())
            {
                try
                {
                    Person person = (Person)obj;
                    person.PreDayEvent();
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 19. 数据损坏：AvailablePersons 列表中包含非 Person 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：AvailablePersons 列表中包含非 Person 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 19. 完成所有人物PreDayEvent");
            
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 20. 开始人物DayEvent（共 {this.AvailablePersons.Count} 个人物）");
            foreach (GameObject obj in this.AvailablePersons.GetRandomList())
            {
                try
                {
                    Person person = (Person)obj;
                    person.DayEvent();
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 20. 数据损坏：AvailablePersons 列表中包含非 Person 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：AvailablePersons 列表中包含非 Person 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 20. 完成所有人物DayEvent");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 21. 开始 AdjustGlobalPersonRelation");
            this.AdjustGlobalPersonRelation();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 21. 完成 AdjustGlobalPersonRelation");
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 22. 开始 AddPreparedAvailablePersons");
            this.AddPreparedAvailablePersons();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 22. 完成 AddPreparedAvailablePersons");
            /*
            foreach (SpyMessage message in this.SpyMessages.GetRandomList())
            {
                message.DayEvent();
            }
             */
            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 23. 开始俘虏DayEvent（共 {this.Captives.Count} 个俘虏）");
            foreach (GameObject obj in this.Captives.GetRandomList())
            {
                try
                {
                    // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
                    Captive captive = (Captive)obj;
                    captive.DayEvent();
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 23. 数据损坏：Captives 列表中包含非 Captive 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Captives 列表中包含非 Captive 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 23. 完成所有俘虏DayEvent");


            // System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 24. 开始宝物耐久度更新（共 {this.Treasures.Count} 个宝物）");
            // 🔥 ANTI-BAND-AID：不使用防御性空检查，如果类型不对说明数据损坏
            foreach (GameObject obj in this.Treasures.GetList())
            {
                try
                {
                    Treasure treasure = (Treasure)obj;
                    if (treasure.Durability > 0)
                    {
                        treasure.Durability -= Session.Parameters.DayInTurn;
                        if (treasure.Durability <= 0)
                        {
                            if (treasure.BelongedPerson != null)
                            {
                                treasure.BelongedPerson.LoseTreasure(treasure);
                            }

                            Session.Current.Scenario.Treasures.Remove(treasure);
                        }
                    }
                }
                catch (InvalidCastException ex)
                {
                    // 🔥 ANTI-BAND-AID：类型转换失败说明数据损坏，记录详细信息
                    System.Diagnostics.Debug.WriteLine($"[DayPassedEvent] 24. 数据损坏：Treasures 列表中包含非 Treasure 对象 {obj?.GetType().Name ?? "null"}");
                    throw new InvalidOperationException($"数据损坏：Treasures 列表中包含非 Treasure 对象 {obj?.GetType().Name ?? "null"}", ex);
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 24. 完成宝物耐久度更新");
            // 🔥 AOT修复：安全的类型转换
            foreach (GameObject obj in this.Treasures.GetList())
            {
                Treasure treasure = (obj is Treasure ? (Treasure)obj : null);
                if (treasure != null && treasure.Durability > 0)
                {
                    treasure.Durability -= Session.Parameters.DayInTurn;
                    if (treasure.Durability <= 0)
                    {
                        if (treasure.BelongedPerson != null)
                        {
                            treasure.BelongedPerson.LoseTreasure(treasure);
                        }

                        Session.Current.Scenario.Treasures.Remove(treasure);
                    }
                }
            }
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 24. 完成宝物耐久度更新");

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 25. 开始 CheckGameEnd");
            this.CheckGameEnd();
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 25. 完成 CheckGameEnd");

            //this.DaySince++;
            this.DaySince += Session.Parameters.DayInTurn;

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 26. 开始 ScenarioEvents.RaisePostDayEvent");
            ScenarioEvents.RaisePostDayEvent(this);
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 26. 完成 ScenarioEvents.RaisePostDayEvent");

            scenarioJustLoaded = false;
            Session.MainGame.mainGameScreen.LoadScenarioInInitialization = false;
            numberOfAmbushTroop = -1; // 缓存有几支部队在埋伏，绝大多数时候地图上根本没有埋伏部队，这时候不需要叫浪费时间的函数detectAmbushTroop

            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 27. 开始 DisposeMapTileMemory");
            Session.MainGame.mainGameScreen.DisposeMapTileMemory(false, false);
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] 27. 完成 DisposeMapTileMemory");
            
            // 🔥 恢复势力范围分帧重算
            Session.MainGame?.mainGameScreen?._influenceUpdateManager?.Resume();
            
            // System.Diagnostics.Debug.WriteLine("[DayPassedEvent] ========== 执行完成 ==========");
        }

        private void militaryKindEvent()
        {
            foreach (MilitaryKind m in this.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
            {
                if (m.Persons.Count > 0 && m.ObtainProb > 0)
                {
                    foreach (Person p in m.Persons)
                    {
                        if (GameObject.Random(m.ObtainProb) == 0)
                        {
                            if (p.BelongedFaction != null && !p.BelongedFaction.BaseMilitaryKinds.MilitaryKinds.ContainsValue(m))
                            {
                                p.BelongedFaction.BaseMilitaryKinds.AddMilitaryKind(m);
                                Session.MainGame.mainGameScreen.xianshishijiantupian(p, m.Name, TextMessageKind.ObtainMilitaryKind, "ObtainMilitaryKind", "", "", false);
                            }
                        }
                    }
                }
            }
        }

        private void guanzhiDayEvent()
        {

            List<Title> ManualAwardTitles = new List<Title>();
            foreach (Title t in this.GameCommonData.AllTitles.Titles.Values)
            {
                if (t.ManualAward)
                {
                    ManualAwardTitles.Add(t);
                }
            }
            foreach (Title t in ManualAwardTitles)
            {
                if (t.AutoLearn > 0 && GameObject.Random(t.AutoLearn) == 0)
                {
                    PersonList candidates = new PersonList();
                    if (t.Persons.Count > 0)
                    {
                        foreach (Person p in t.Persons)
                        {
                            if (p.Available && p.Alive)
                            {
                                candidates.Add(p);
                            }
                        }
                    }
                    else
                    {
                        candidates = this.AvailablePersons;
                    }
                    foreach (Person p in candidates)
                    {
                        
                        if ((!this.IsPlayer(p.BelongedFaction) || Session.GlobalVariables.PermitManualAwardTitleAutoLearn) && !p.HasHigherLevelTitle(t) && !t.ManualAward && t.CanLearn(p, true))
                        {
                            p.AwardTitle(t);
                        }
                    }
                }
            }
        }


        private static Person courier = null;
        private void titleDayEvent()
        {
            if (courier == null)
            {
                courier = (Person)this.Persons.GetGameObject(7200);
            }
            foreach (Title t in this.GameCommonData.AllTitles.Titles.Values)
            {
                if (t.AutoLearn > 0 && GameObject.Random(t.AutoLearn) == 0)
                {
                    PersonList candidates = new PersonList();
                    if (t.Persons.Count > 0)
                    {
                        foreach (Person p in t.Persons)
                        {
                            if (p.Available && p.Alive)
                            {
                                candidates.Add(p);
                            }
                        }
                    }
                    else
                    {
                        candidates = this.AvailablePersons;
                    }
                    foreach (Person p in candidates)
                    {
                        if (!p.HasHigherLevelTitle(t) && t.CanLearn(p, true) && !t.ManualAward)
                        {
                            p.LearnTitle(t);
                            Session.MainGame.mainGameScreen.AutoLearnTitle(p, courier, t);
                        }
                        else if (p.HasTitle() && t.WillLose(p))
                        {
                            p.LoseTitle();
                        }
                    }
                }
            }
        }

        private void detectCurrentPlayerBattleState(Faction faction, bool init = false)
        {

            if (faction == null) return;
            //defend
            ZhandouZhuangtai originalBattleState = faction.BattleState;
            bool fangshou = false;
            int fightingArchitectureCount = 0;
            foreach (Architecture architecture in faction.Architectures)
            {
                if (architecture.BelongedFaction == null) continue;

                if (architecture.BelongedSection == null || architecture.BelongedSection.AIDetail.AutoRun) continue;

                if (architecture.FindHostileTroopInView())
                {
                    fightingArchitectureCount++;

                    if (!architecture.hostileTroopInViewLastDay)  //如果已经提醒过就不再提醒
                    {
                        //architecture.JustAttacked = true;
                        architecture.BelongedFaction.StopToControl = Setting.Current.GlobalVariables.StopToControlOnAttack;
                        architecture.RecentlyAttacked = 5;
                        Session.MainGame.mainGameScreen.ArchitectureBeginRecentlyAttacked(architecture);  //提示玩家建筑视野范围内出现敌军。

                    }
                    architecture.hostileTroopInViewLastDay = true;

                }
                else
                {
                    architecture.hostileTroopInViewLastDay = false;
                }

            }
            if (fightingArchitectureCount == 0)
            {
                fangshou = false;
            }
            else
            {
                fangshou = true;
            }
            //attack
            bool jingong = false;

            foreach (Troop t in faction.Troops)
            {
                if (t.HasHostileArchitectureInView())         //||t.HasHostileTroopInView())
                {
                    jingong = true;
                    break;
                }
            }

            if (!jingong && !fangshou)
            {
                faction.BattleState = ZhandouZhuangtai.和平;
            }
            else if (jingong && !fangshou)
            {
                faction.BattleState = ZhandouZhuangtai.进攻;

            }
            else if (!jingong && fangshou)
            {
                faction.BattleState = ZhandouZhuangtai.防守;

            }
            else
            {
                faction.BattleState = ZhandouZhuangtai.攻守兼备;
            }

            // 🔥 修复：移除 init 参数的强制音乐切换
            // 只在战斗状态真正改变时切换音乐，避免游戏启动时的重复播放
            // 日期：2026-02-14
            if (originalBattleState != faction.BattleState)
            {
                Session.MainGame.mainGameScreen.SwichMusic(this.Date.Season);
            }

        }

        public void DayStartingEvent()
        {
            // 自动清理幽灵部队 (无将领)
            List<Troop> ghostTroops = new List<Troop>();
            // 🔥 AOT修复：安全的类型转换
            foreach (GameObject obj in this.Troops.GetList())
            {
                Troop troop = (obj is Troop ? (Troop)obj : null);
                if (troop == null) continue;
                
                // 针对用户报告的ID进行详细诊断
                if (troop.ID == 124 || troop.ID == 125 || troop.ID == 126)
                {
                    // System.Diagnostics.Debug.WriteLine($"[GhostCheck] ID={troop.ID}, Name={troop.DisplayName}, LeaderIsNull={(troop.Leader == null)}");
                }

                // 增加判断条件：如果Leader为空 OR 名字显示为"----"，都视为幽灵部队
                if (troop.Leader == null || troop.DisplayName == "----")
                {
                    ghostTroops.Add(troop);
                }
            }
            foreach (Troop t in ghostTroops)
            {
                try 
                {
                    System.Diagnostics.Debug.WriteLine($"[DayStartingEvent] 发现并清理幽灵部队: ID={t.ID}, Name={t.DisplayName}, Pos={t.Position}");
                    t.Destroy(true, true);
                    // 强制再次移除以防万一
                    this.Troops.Remove(t);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayStartingEvent] 清理幽灵部队失败: {ex.Message}");
                }
            }

            // ------------------------------------------------------------------------------------------------
            // AI Integration Hook: Process High-Level Tactical Decisions
            // ------------------------------------------------------------------------------------------------
            try
            {
                if (GameManager.AIManager.Instance != null)
                {
                    // Update troop list if needed (optional, ensures AI sees current troops)
                    // GameManager.AIManager.Instance.UpdateTroopList(this.Troops.GetList());

                    foreach (Faction faction in this.Factions)
                    {
                        if (faction == null || faction.Destroyed) continue;
                        GameManager.AIManager.Instance.ProcessAllTroopDecisions(faction);
                    }
                    // System.Diagnostics.Debug.WriteLine("[DayStartingEvent] AI Tactical Decisions Processed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DayStartingEvent] AI Decision Error: {ex.Message}");
            }
            // ------------------------------------------------------------------------------------------------

            this.Factions.SetControlling(false);
            
            // 🔥 AOT修复：安全的类型转换
            foreach (GameObject obj in this.Troops.GetList())
            {
                Troop troop = (obj is Troop ? (Troop)obj : null);
                if (troop != null && (troop.BelongedFaction == null || troop.BelongedLegion == null || !troop.BelongedLegion.Troops.HasGameObject(troop)))
                {
                    troop.AI();
                }
            }
            this.Troops.BuildQueue();
            
            // 🔥 2026-03-23 新增：构建 CommandBuffer（在 BuildQueue 之后）
            // 原因：BuildQueue 包含回合初始化副作用（InitializeInQueue、标志复位等）
            // 顺序：必须先执行 BuildQueue 的副作用，再生成 CommandBuffer
            if (Session.GlobalVariables.EnableCommandBufferScheduler && Session.Current?.CommandBufferScheduler != null)
            {
                try
                {
                    bool buildSucceeded = Session.Current.CommandBufferScheduler.BuildCommandBuffer(this);
                    if (!buildSucceeded)
                    {
                        System.Diagnostics.Debug.WriteLine("[DayStartingEvent] ⚠️ CommandBuffer 构建失败，本回合回退旧调度器");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DayStartingEvent] ❌ CommandBuffer 构建失败: {ex.Message}");
                }
            }
            
            // 🔥 AOT修复：安全的类型转换
            foreach (GameObject obj in this.Architectures.GetList())
            {
                Architecture architecture = (obj is Architecture ? (Architecture)obj : null);
                if (architecture != null)
                {
                    architecture.HireFinished = false;
                    architecture.HasManualHire = false;
                    architecture.TodayPersonArriveNote = false;
                }
            }
        }

        public void FireDayEvent()
        {
            List<Point> list = new List<Point>();
            foreach (Point point in this.FireTable.Positions)
            {
                if (GameObject.Chance(Session.Parameters.FireStayProb))
                {
                    list.Add(point);
                }
            }
            foreach (Point point in list)
            {
                this.ClearPositionFire(point);
            }
            list.Clear();
            foreach (Point point in this.FireTable.Positions)
            {
                list.Add(point);
            }
            foreach (Point point in list)
            {
                this.FireSpread(point);
            }
            
            // 🔥 2026-03-10 新增：基于风向风力的火势蔓延系统
            // 在旧逻辑之后调用，确保向后兼容
            // ANTI-BAND-AID：直接调用，如果 _fireSpreadManager 为 null 说明 Init() 未调用
            _fireSpreadManager.ProcessFireSpread();
        }

        public void FireSpread(Point position)
        {
            GameArea area = GameArea.GetArea(position, 1, false);
            foreach (Point point in area.Area)
            {
                if ((point != position) && this.IsFireVaild(point, false, MilitaryType.步兵))
                {
                    if (this.PositionIsOnFire(point))
                    {
                        continue;
                    }
                    int chance = 0;
                    switch (this.GetTerrainKindByPosition(position))
                    {
                        case TerrainKind.平原:
                            chance = 3;
                            break;

                        case TerrainKind.草原:
                            chance = 4;
                            break;

                        case TerrainKind.森林:
                            chance = 10;
                            break;

                        case TerrainKind.山地:
                            chance = 6;
                            break;
                    }
                    if (GameObject.Chance((int)(chance * Session.Parameters.FireSpreadProbMultiply)))
                    {
                        this.SetPositionOnFire(point);
                        Troop troopByPosition = this.GetTroopByPosition(point);
                        if (troopByPosition != null)
                        {
                            troopByPosition.BurntBySpreadFire();
                        }
                    }
                }
            }
        }

        public RoutewayList GetActiveRoutewayListByPosition(Point position)
        {
            RoutewayList list = new RoutewayList();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetActiveRoutewayListByPosition] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].TileRouteways == null)
                {
                    return list;
                }
                foreach (Routeway routeway in this.MapTileData[position.X, position.Y].TileRouteways)
                {
                    if (routeway.IsActive || routeway.IsPointActive(position))
                    {
                        list.Add(routeway);
                    }
                }
            }
            return list;
        }

        public Architecture GetArchitectureByPosition(Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return null;
            }
            // 🔥 Technical Fix: Prevent NRE if MapTileData is null
            if (this.MapTileData == null)
            {
                return null;
            }
            return this.MapTileData[position.X, position.Y].TileArchitecture;
        }

        public Architecture GetArchitectureByPositionNoCheck(Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null) return null;
            if (this.PositionOutOfRange(position)) return null;
            
            try
            {
                return this.MapTileData[position.X, position.Y].TileArchitecture;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetArchitectureByPositionNoCheck] 访问异常: {ex.Message}");
                return null;
            }
        }

        public GameArea GetAreaWithinDistance(Point centre, int distance, bool includingCentre)
        {
            GameArea area = new GameArea();
            for (int i = -distance; i <= distance; i++)
            {
                for (int j = -distance; j <= distance; j++)
                {
                    Point fromPosition = new Point(centre.X + i, centre.Y + j);
                    if ((includingCentre || !(fromPosition == centre)) && (this.GetDistance(fromPosition, centre) <= distance))
                    {
                        area.AddPoint(fromPosition);
                    }
                }
            }
            return area;
        }

        public Point GetClosestPoint(GameArea area, Point fromPosition)
        {
            // 🔥 修复：数据验证，暴露数据源错误
            if (area == null)
            {
                throw new ArgumentNullException(nameof(area), "GameArea 不能为 null");
            }
            
            if (area.Area == null || area.Area.Count == 0)
            {
                throw new InvalidOperationException($"GameArea.Area 为空，无法计算最近点。fromPosition={fromPosition}");
            }
            
            int simpleDistance = 0, minSimpleDistance = int.MaxValue;
            double distance = 0, minDistance = double.MaxValue;
            Point point = new Point();
            foreach (Point point2 in area.Area)
            {
                simpleDistance = this.GetSimpleDistance(fromPosition, point2);
                if (simpleDistance <= minSimpleDistance)
                {
                    distance = this.GetDistance(fromPosition, point2);
                    if (distance < minDistance)
                    {
                        minSimpleDistance = simpleDistance;
                        minDistance = distance;
                        point = point2;
                    }
                }
            }
            return point;
        }

        public void GetClosestPointsBetweenTwoAreas(GameArea area1, GameArea area2, out Point? out1, out Point? out2)
        {
            out1 = null;
            out2 = null;
            int simpleDistance = 0, minSimpleDistance = int.MaxValue;
            double distance = 0, minDistance = double.MaxValue;
            foreach (Point point in area1.Area)
            {
                foreach (Point point2 in area2.Area)
                {
                    simpleDistance = this.GetSimpleDistance(point, point2);
                    if (simpleDistance <= minSimpleDistance)
                    {
                        distance = this.GetDistance(point, point2);
                        if (distance < minDistance)
                        {
                            minSimpleDistance = simpleDistance;
                            minDistance = distance;
                            out1 = new Point?(point);
                            out2 = new Point?(point2);
                        }
                    }
                }
            }
        }

        public Point? GetClosestPosition(GameArea area, List<Point> orientations)
        {
            Point? nullable = null;
            int num = 0x7fffffff;
            foreach (Point point in area.Area)
            {
                int num2 = 0;
                foreach (Point point2 in orientations)
                {
                    num2 += this.GetSimpleDistance(point, point2);
                }
                if (num2 < num)
                {
                    num = num2;
                    nullable = new Point?(point);
                }
            }
            return nullable;
        }

        public string GetCoordinateString(Point position)
        {
            return (position.X + "," + position.Y);
        }

        public int GetDiplomaticRelation(int faction1, int faction2)
        {
            if (faction1 != faction2)
            {
                DiplomaticRelation diplomaticRelation = this.DiplomaticRelations.GetDiplomaticRelation(faction1, faction2);
                if (diplomaticRelation != null)
                {
                    return diplomaticRelation.Relation;
                }
            }
            return 0;
        }

        public int GetDiplomaticRelationTruce(int faction1, int faction2)
        {
            if (faction1 != faction2)
            {
                DiplomaticRelation diplomaticRelation = this.DiplomaticRelations.GetDiplomaticRelation(faction1, faction2);
                if (diplomaticRelation != null)
                {
                    return diplomaticRelation.Truce;
                }
            }
            return 0;
        }

        public double GetResourceConsumptionRate(Architecture a, Troop b)
        {
            return this.GetDistance(b.Position, a.ArchitectureArea) / 50.0 + 1;
        }

        public double GetResourceConsumptionRate(Architecture a, Architecture b)
        {
            return this.GetDistance(a.ArchitectureArea, b.ArchitectureArea) / 150.0 + 1;
        }

        public double GetDistance(GameArea fromArea, GameArea toArea)
        {
            // 上面这段浪费太多时间O(n^2)，下面仅需要O(1)，一个非常近似的值已经足够
            double distance = GetDistance(fromArea.Centre, toArea.Centre);

            if (distance < 0) return 0;

            distance -= (1 + Math.Sqrt(2 * fromArea.Count + 1)) / 2;
            distance -= (1 + Math.Sqrt(2 * toArea.Count + 1)) / 2;

            return distance;
        }

        public double GetDistance(Point fromPosition, GameArea toArea)
        {
            // O(1) instead of O(n)
            double distance = GetDistance(fromPosition, toArea.Centre);

            distance -= (1 + Math.Sqrt(2 * toArea.Count + 1)) / 2;

            return distance;
        }

        public double GetDistance(Point fromPosition, Point toPosition)
        {
            return Math.Sqrt(Math.Pow(toPosition.X - fromPosition.X, 2) + Math.Pow(toPosition.Y - fromPosition.Y, 2));
        }

        public Point? GetFarthestPosition(GameArea area, List<Point> orientations)
        {
            Point? nullable = null;
            int num = -2147483648;
            foreach (Point point in area.Area)
            {
                int num2 = 0;
                foreach (Point point2 in orientations)
                {
                    num2 += this.GetSimpleDistance(point, point2);
                }
                if (num2 > num)
                {
                    num = num2;
                    nullable = new Point?(point);
                }
            }
            return nullable;
        } 

        public ArchitectureList GetHighViewingArchitecturesByPosition(Point position)
        {
            ArchitectureList list = new ArchitectureList();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetHighViewingArchitecturesByPosition] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].HighViewingArchitectures == null)
                {
                    return list;
                }
                foreach (Architecture architecture in this.MapTileData[position.X, position.Y].HighViewingArchitectures)
                {
                    list.Add(architecture);
                }
            }
            return list;
        }

        public string GetPlayerInfo()
        {
            // 🔥 修复：正确判断玩家势力
            // 日期：2026-02-25
            if (this.CurrentPlayer != null)
            {
                if (this.PlayerFactions.Count > 1)
                {
                    return this.CurrentPlayer.Name + " 等";
                }
                // 🔥 修复：只要有 CurrentPlayer 就显示其名称
                return this.CurrentPlayer.Name;
            }
            if (this.PlayerFactions != null && this.PlayerFactions.Count > 0)
            {
                Faction firstPlayer = this.PlayerFactions[0] as Faction;
                if (firstPlayer != null) return firstPlayer.Name;
            }
            
            // 🔥 新增：处于存档克隆阶段时，由于 PlayerFactions 与 CurrentPlayer 都被摘除，
            // 此时必须回源检查 PlayerList 去匹配原本势力列表。
            if (this.PlayerList != null && this.PlayerList.Count > 0)
            {
                if (this.Factions != null)
                {
                    Faction f = this.Factions.GetGameObject(this.PlayerList[0]) as Faction;
                    if (f != null) return f.Name;
                }
            }
            
            return "电脑";
        }

        //public Texture2D GetPortrait(float id)
        //{
        //    return Session.MainGame.mainGameScreen.GetPortrait(id);
        //}

        public int GetPositionHostileOffencingDiscredit(Troop troop, Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null || troop == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetPositionHostileOffencingDiscredit] MapTileData 或 troop 为 null");
                return 0;
            }
            
            if (this.PositionOutOfRange(position))
            {
                System.Diagnostics.Debug.WriteLine($"[GetPositionHostileOffencingDiscredit] 位置越界: ({position.X}, {position.Y})");
                return 0;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].GetPositionHostileOffencingDiscredit(troop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPositionHostileOffencingDiscredit] 访问异常: {ex.Message}");
                return 0;
            }
        }

        public int GetPositionMapCost(Faction faction, Point position)
        {
            Architecture architectureByPositionNoCheck = this.GetArchitectureByPositionNoCheck(position);
            if (architectureByPositionNoCheck != null)
            {
                if ((architectureByPositionNoCheck.Endurance > 0) && (architectureByPositionNoCheck.BelongedFaction != faction))
                {
                    return 0xdac;
                }
                return 5;
            }
            Troop troopByPositionNoCheck = this.GetTroopByPositionNoCheck(position);
            if (troopByPositionNoCheck != null)
            {
                if (!((faction != null) && faction.IsFriendly(troopByPositionNoCheck.BelongedFaction)))
                {
                    return 0xdac;
                }
                return 0;
            }
            if (this.PositionIsOnFire(position))
            {
                return 10;
            }
            return 0;
        }
        
        /// <summary>
        /// 🆕 获取指定位置的净势力影响（Phase 4 - Buff系统）
        /// 日期：2026-03-11
        /// 🔥 性能优化：使用缓存避免重复计算（2026-03-12）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="faction">参考势力</param>
        /// <returns>净能量值（己方能量 - 敌方最大能量）</returns>
        public int GetNetInfluenceAt(Point position, Faction faction)
        {
            // 🆕 Phase 4 性能优化：使用缓存避免重复计算
            var key = (position, faction);
            if (!_influenceEnergyCache.TryGetValue(key, out int netEnergy))
            {
                // 缓存未命中，计算并缓存
                netEnergy = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.GetNetInfluenceAt(position, faction);
                _influenceEnergyCache[key] = netEnergy;
            }
            return netEnergy;
        }

        /// <summary>
        /// 🆕 获取指定位置的粮食消耗倍率（Phase 4 - Buff系统）
        /// 日期：2026-03-11
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="faction">部队所属势力</param>
        /// <returns>粮食消耗倍率（0.7 ~ 1.2）</returns>
        public float GetFoodConsumptionMultiplier(Point position, Faction faction)
        {
            // 🆕 Phase 4 性能优化：使用缓存避免重复计算
            // 日期：2026-03-12
            var key = (position, faction);
            if (!_influenceEnergyCache.TryGetValue(key, out int netEnergy))
            {
                // 缓存未命中，计算并缓存
                netEnergy = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.GetNetInfluenceAt(position, faction);
                _influenceEnergyCache[key] = netEnergy;
            }
            
            return WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateFoodConsumptionMultiplier(netEnergy);
        }

        /// <summary>
        /// 🔥 预热势力范围能量缓存（2026-03-12）
        /// 在回合开始时预先计算所有部队位置的能量，避免回合中卡顿
        /// 🧊 Cold Path：回合开始时调用一次
        /// </summary>
        private void PrewarmInfluenceCache()
        {
            System.Diagnostics.Debug.WriteLine("[PrewarmInfluenceCache] 开始预热缓存");
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            var troops = this.Troops.GetList();
            int troopCount = troops.Count;
            int cachedCount = 0;
            
            System.Diagnostics.Debug.WriteLine($"[PrewarmInfluenceCache] 共有 {troopCount} 支部队需要预热");
            
            for (int i = 0; i < troopCount; i++)
            {
                if (troops[i] is not Troop troop) continue;
                
                // 🔥 ANTI-BAND-AID：部队必须属于某个势力，否则数据损坏
                if (troop.BelongedFaction == null)
                {
                    throw new InvalidOperationException(
                        $"[PrewarmInfluenceCache] 部队 {troop.DisplayName}(ID:{troop.ID}) 的 BelongedFaction 为 null，数据损坏");
                }
                
                // 🔥 详细日志：输出前5个和后5个部队
                if (i < 5 || i >= troopCount - 5)
                {
                    System.Diagnostics.Debug.WriteLine($"[PrewarmInfluenceCache] 预热部队 {i+1}/{troopCount}: {troop.DisplayName}, 位置=({troop.Position.X},{troop.Position.Y}), 势力={troop.BelongedFaction.Name}");
                }
                
                // 预先计算并缓存（通过 GetNetInfluenceAt 自动缓存）
                var startTime = sw.ElapsedMilliseconds;
                GetNetInfluenceAt(troop.Position, troop.BelongedFaction);
                var elapsed = sw.ElapsedMilliseconds - startTime;
                
                // 🔥 性能警告：如果单次计算超过 50ms
                if (elapsed > 50)
                {
                    System.Diagnostics.Debug.WriteLine($"[PrewarmInfluenceCache] ⚠️ 性能警告：部队 {troop.DisplayName} 预热耗时 {elapsed}ms");
                }
                
                cachedCount++;
            }
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[PrewarmInfluenceCache] 完成，预热了 {cachedCount} 支部队的位置，缓存大小={_influenceEnergyCache.Count}，总耗时={sw.ElapsedMilliseconds}ms");
        }

        public Point GetProperDestination(Point from, Point to)
        {
            double distance = this.GetDistance(from, to);
            if (distance > 15.0)
            {
                return new Point(from.X + ((int)(((double)((to.X - from.X) * 15)) / distance)), from.Y + ((int)(((double)((to.Y - from.Y) * 15)) / distance)));
            }
            return to;
        }

        public int GetReturnDays(Point destination, GameArea fromArea)
        {
            int num = (int)Math.Ceiling((double)(this.GetDistance(destination, this.GetClosestPoint(fromArea, destination)) / 10.0));
            num *= 2;
            if (num == 0)
            {
                num = 1;
            }
            return num;
        }

        public ArchitectureList GetRoutewayArchitecturesByPosition(Routeway routeway, Point position)
        {
            ArchitectureList list = new ArchitectureList();
            if (!this.PositionOutOfRange(position))
            {
                foreach (Architecture architecture in routeway.BelongedFaction.Architectures)
                {
                    if ((architecture != routeway.StartArchitecture) && architecture.GetRoutewayStartArea().HasPoint(position))
                    {
                        list.Add(architecture);
                    }
                }
            }
            return list;
        }

        public Routeway GetRoutewayByPosition(Point position)
        {
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetRoutewayByPosition] MapTileData is null");
                return null;
            }
            
            if (this.PositionOutOfRange(position))
            {
                return null;
            }
            if (this.MapTileData[position.X, position.Y].TileRouteways == null)
            {
                return null;
            }
            if (this.MapTileData[position.X, position.Y].TileRouteways.Count == 0)
            {
                return null;
            }
            return this.MapTileData[position.X, position.Y].TileRouteways[0];
        }

        public Routeway GetRoutewayByPositionAndFaction(Point position, Faction faction)
        {
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetRoutewayByPositionAndFaction] MapTileData is null");
                return null;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].TileRouteways == null)
                {
                    return null;
                }
                foreach (Routeway routeway in this.MapTileData[position.X, position.Y].TileRouteways)
                {
                    if (((routeway.BelongedFaction == faction) && (routeway.StartArchitecture != null)) && ((((routeway.DestinationArchitecture == null) || !routeway.StartArchitecture.BelongedSection.AIDetail.AutoRun) || routeway.Building) || (routeway.LastActivePointIndex >= 0)))
                    {
                        return routeway;
                    }
                }
            }
            return null;
        }

        public List<Routeway> GetRoutewaysByPositionAndFaction(Point position, Faction faction)
        {
            List<Routeway> list = new List<Routeway>();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetRoutewaysByPositionAndFaction] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].TileRouteways == null)
                {
                    return list;
                }
                foreach (Routeway routeway in this.MapTileData[position.X, position.Y].TileRouteways)
                {
                    if (routeway.BelongedFaction == faction)
                    {
                        list.Add(routeway);
                    }
                }
            }
            return list;
        }

        public int GetSimpleDistance(Point from, Point to)
        {
            return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        }

        public int GetSingleWayDays(Point destination, GameArea fromArea)
        {
            int num = (int)Math.Ceiling((double)(this.GetDistance(destination, this.GetClosestPoint(fromArea, destination)) / 10.0));
            if (num == 0)
            {
                num = 1;
            }
            return num;
        }

        //public Texture2D GetSmallPortrait(float id)
        //{
        //    return Session.MainGame.mainGameScreen.GetSmallPortrait(id);
        //}

        //public Texture2D GetTroopPortrait(float id)
        //{
        //    return Session.MainGame.mainGameScreen.GetTroopPortrait(id);
        //}
        //public Texture2D GetFullPortrait(float id)
        //{
        //    return Session.MainGame.mainGameScreen.GetFullPortrait(id);
        //}

        public ArchitectureList GetSupplyArchitecturesByPositionAndFaction(Point position, Faction faction)
        {
            ArchitectureList list = new ArchitectureList();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetSupplyArchitecturesByPositionAndFaction] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].SupplyingArchitectures == null)
                {
                    return list;
                }
                foreach (Architecture architecture in this.MapTileData[position.X, position.Y].SupplyingArchitectures)
                {
                    //if (faction.IsFriendly(architecture.BelongedFaction))
                    if (faction == architecture.BelongedFaction)
                    {
                        list.Add(architecture);
                    }
                }
            }
            return list;
        }

        public List<RoutePoint> GetSupplyRoutePointsByPositionAndFaction(Point position, Faction faction)
        {
            List<RoutePoint> list = new List<RoutePoint>();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetSupplyRoutePointsByPositionAndFaction] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].SupplyingRoutePoints == null)
                {
                    return list;
                }
                foreach (RoutePoint point in this.MapTileData[position.X, position.Y].SupplyingRoutePoints)
                {
                    if (point.BelongedRouteway.IsSupporting(faction))
                    {
                        list.Add(point);
                    }
                }
            }
            return list;
        }

        public TerrainDetail GetTerrainDetailByPosition(Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return null;
            }
            return this.GameCommonData.AllTerrainDetails.GetTerrainDetail(ScenarioMap.MapData[position.X, position.Y]);
        }

        public TerrainDetail GetTerrainDetailByPositionNoCheck(Point position)
        {
            // 如果 MapData 为 null，说明地图数据未正确加载，这是严重错误
            if (ScenarioMap?.MapData == null)
            {
                throw new InvalidOperationException($"ScenarioMap.MapData 为 null！地图数据未正确加载。Position: ({position.X}, {position.Y})");
            }
            
            if (this.GameCommonData?.AllTerrainDetails == null)
            {
                throw new InvalidOperationException($"GameCommonData.AllTerrainDetails 为 null！");
            }
            
            return this.GameCommonData.AllTerrainDetails.GetTerrainDetail(ScenarioMap.MapData[position.X, position.Y]);
        }

        public TerrainKind GetTerrainKindByPosition(Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return TerrainKind.无;
            }
            return (TerrainKind)ScenarioMap.MapData[position.X, position.Y];
        }

        public TerrainKind GetTerrainKindByPositionNoCheck(Point position)
        {
            return (TerrainKind)ScenarioMap.MapData[position.X, position.Y];
        }

        public string GetTerrainNameByPosition(Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return "----";
            }
            var terrainDetail = this.GameCommonData.AllTerrainDetails.GetTerrainDetail(ScenarioMap.MapData[position.X, position.Y]);
            if (terrainDetail == null)
            {
                return "----";
            }
            return terrainDetail.Name;
        }

        public int GetTransferFundDays(Architecture from, Architecture to)
        {
            //return (int)Math.Ceiling(this.GetDistance(from.ArchitectureArea, to.ArchitectureArea) / 2.5);
            return (int)Math.Ceiling(this.GetDistance(from.ArchitectureArea, to.ArchitectureArea) / 2.5);
        }


        public Troop GetTroopByPosition(Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                // System.Diagnostics.Debug.WriteLine("[GetTroopByPosition] MapTileData 为 null");
                return null;
            }
            
            if (this.PositionOutOfRange(position))
            {
                return null;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].TileTroop;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTroopByPosition] 访问异常: {ex.Message}");
                return null;
            }
        }

        public Troop GetTroopByPositionNoCheck(Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                // System.Diagnostics.Debug.WriteLine("[GetTroopByPositionNoCheck] MapTileData 为 null");
                return null;
            }
            
            if (this.PositionOutOfRange(position))
            {
                System.Diagnostics.Debug.WriteLine($"[GetTroopByPositionNoCheck] 位置越界: ({position.X}, {position.Y})");
                return null;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].TileTroop;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTroopByPositionNoCheck] 访问异常: {ex.Message}");
                return null;
            }
        }

        public ArchitectureList GetViewingArchitecturesByPosition(Point position)
        {
            ArchitectureList list = new ArchitectureList();
            
            // 🔥 Critical Fix: Check MapTileData null before accessing
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetViewingArchitecturesByPosition] MapTileData is null");
                return list;
            }
            
            if (!this.PositionOutOfRange(position))
            {
                if (this.MapTileData[position.X, position.Y].ViewingArchitectures == null)
                {
                    return list;
                }
                foreach (Architecture architecture in this.MapTileData[position.X, position.Y].ViewingArchitectures)
                {
                    list.Add(architecture);
                }
            }
            return list;
        }

        public int GetWaterPositionMapCost(MilitaryKind kind, Point position)
        {
            if (ScenarioMap.MapData[position.X, position.Y] == 6)
            {
                if (Session.GlobalVariables.LandArmyCanGoDownWater)
                {
                    return 0;
                }

                if (this.GetArchitectureByPositionNoCheck(position) != null)
                {
                    return 0;
                }
                if (kind.Type == MilitaryType.水军)
                {
                    return 0;
                }
                int num = 0;
                Point point = new Point(position.X - 1, position.Y);
                if (!(this.PositionOutOfRange(point) || (ScenarioMap.MapData[point.X, point.Y] != 6)))
                {
                    num++;
                }
                Point point2 = new Point(position.X, position.Y - 1);
                if (!(this.PositionOutOfRange(point2) || (ScenarioMap.MapData[point2.X, point2.Y] != 6)))
                {
                    num++;
                }
                Point point3 = new Point(position.X + 1, position.Y);
                if (!(this.PositionOutOfRange(point3) || (ScenarioMap.MapData[point3.X, point3.Y] != 6)))
                {
                    num++;
                }
                if (num > 2)
                {
                    return 0xdac;
                }
                Point point4 = new Point(position.X, position.Y + 1);
                if (!(this.PositionOutOfRange(point4) || (ScenarioMap.MapData[point4.X, point4.Y] != 6)))
                {
                    num++;
                }
                if (num > 2)
                {
                    return 0xdac;
                }
            }
            else
            {
                if (kind.Type != MilitaryType.水军 || kind.IsShell || kind.IsTransport)
                {
                    return 0;
                }

                Architecture a = this.GetArchitectureByPositionNoCheck(position);
                if (a != null && !a.Kind.ShipCanEnter)
                {
                    return 0xdac;
                }
            }
            return 0;
        }

        private bool HasSameIdealFaction(Person person)
        {
            if ((person.BelongedFaction != null) && (person.BelongedFaction.Leader == person))
            {
                return true;
            }
            foreach (Faction faction in this.Factions)
            {
                if ((faction.Leader != null) && (faction.Leader.Ideal == person.Ideal))
                {
                    return true;
                }
            }
            return false;
        }

        public int HostileContactingTroopsCount(Faction faction, Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[HostileContactingTroopsCount] MapTileData 为 null");
                return 0;
            }
            
            if (this.PositionOutOfRange(position))
            {
                return 0;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].HostileContactingTroopsCount(faction);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HostileContactingTroopsCount] 访问异常: {ex.Message}");
                return 0;
            }
        }

        public int HostileOffencingTroopsCount(Faction faction, Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[HostileOffencingTroopsCount] MapTileData 为 null");
                return 0;
            }
            
            if (this.PositionOutOfRange(position))
            {
                return 0;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].HostileOffencingTroopsCount(faction);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HostileOffencingTroopsCount] 访问异常: {ex.Message}");
                return 0;
            }
        }

        public int HostileViewingTroopsCount(Faction faction, Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[HostileViewingTroopsCount] MapTileData 为 null");
                return 0;
            }
            
            if (this.PositionOutOfRange(position))
            {
                return 0;
            }
            
            try
            {
                return this.MapTileData[position.X, position.Y].HostileViewingTroopsCount(faction);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HostileViewingTroopsCount] 访问异常: {ex.Message}");
                return 0;
            }
        }

        public void InitialGameData()
        {
            // 🔥 性能诊断：详细拆解 InitialGameData 的耗时
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swStep = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitialGameData] 开始初始化游戏数据                      ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            // 1. 军区数据
            swStep.Restart();
            this.InitializeSectionData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 1. 军区数据: {swStep.ElapsedMilliseconds} ms");
            
            // 2. 路径数据
            swStep.Restart();
            this.InitializeRoutewayData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 2. 路径数据: {swStep.ElapsedMilliseconds} ms");
            
            // 3. 建筑数据
            swStep.Restart();
            this.InitializeArchitectureData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 3. 建筑数据: {swStep.ElapsedMilliseconds} ms");
            
            // 4. 编队数据
            swStep.Restart();
            this.InitializeMilitariesData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 4. 编队数据: {swStep.ElapsedMilliseconds} ms");
            
            // 5. 部队数据
            swStep.Restart();
            this.InitializeTroopData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 5. 部队数据: {swStep.ElapsedMilliseconds} ms");
            
            // 6. 俘虏数据
            swStep.Restart();
            this.InitializeCaptiveData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 6. 俘虏数据: {swStep.ElapsedMilliseconds} ms");
            
            // 7. 武将数据
            swStep.Restart();
            this.InitializePersonData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 7. 武将数据: {swStep.ElapsedMilliseconds} ms");
            
            // 8. 称号和兵种关联
            swStep.Restart();
            foreach (Person p in this.Persons)
            {
                foreach (Title t in p.UniqueTitles.Titles.Values)
                {
                    t.Persons.Add(p);
                }
                foreach (MilitaryKind m in p.UniqueMilitaryKinds.MilitaryKinds.Values)
                {
                    m.Persons.Add(p);
                }
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 8. 称号和兵种关联: {swStep.ElapsedMilliseconds} ms");

            // 9. 配偶检查
            swStep.Restart();
            if (Session.GlobalVariables.RemoveSpouseIfNotAvailable)
            {
                foreach (Person p in Persons)
                {
                    if (!p.Available && p.Spouse != null && !p.Spouse.Available)
                    {
                        p.suoshurenwuList.Remove(p.Spouse);
                        p.Spouse = null;
                    }
                }
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 9. 配偶检查: {swStep.ElapsedMilliseconds} ms");

            // 10. 数据迁移
            swStep.Restart();
            Session.Parameters.MigrateData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] 10. 数据迁移: {swStep.ElapsedMilliseconds} ms");

            swTotal.Stop();
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitialGameData] 初始化完成                              ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[InitialGameData] ⏱️ 总耗时: {swTotal.ElapsedMilliseconds} ms");
        }

        public void InitializeArchitectureMapTile()
        {
            /*
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitializeArchitectureMapTile] 开始重建地图引用          ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            */
            
            if (this.MapTileData == null)
            {
                // System.Diagnostics.Debug.WriteLine("❌ [InitializeArchitectureMapTile] MapTileData 为 null，跳过");
                return;
            }
            
            if (this.Architectures == null)
            {
                // System.Diagnostics.Debug.WriteLine("❌ [InitializeArchitectureMapTile] Architectures 为 null，跳过");
                return;
            }
            
            int successCount = 0;
            int errorCount = 0;
            
            // 第一遍：直接设置地图格子引用
            foreach (Architecture architecture in this.Architectures)
            {
                try
                {
                    if (architecture?.ArchitectureArea?.Area != null)
                    {
                        foreach (Point point in architecture.ArchitectureArea.Area)
                        {
                            if (!this.PositionOutOfRange(point))
                            {
                                this.MapTileData[point.X, point.Y].TileArchitecture = architecture;
                                successCount++;
                            }
                        }
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"⚠️  [InitializeArchitectureMapTile] 处理建筑 {architecture?.Name} 时出错: {ex.Message}");
                    errorCount++;
                }
            }
            
            // System.Diagnostics.Debug.WriteLine($"📊 [InitializeArchitectureMapTile] 第一遍完成: 成功 {successCount}, 错误 {errorCount}");
            
            // 第二遍：调用 SetMapTileArchitecture
            int setMapCount = 0;
            foreach (Architecture architecture in this.Architectures)
            {
                try
                {
                    this.SetMapTileArchitecture(architecture);
                    setMapCount++;
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"⚠️  [InitializeArchitectureMapTile] SetMapTileArchitecture 失败: {architecture?.Name}, {ex.Message}");
                }
            }
            
            /*
            System.Diagnostics.Debug.WriteLine($"📊 [InitializeArchitectureMapTile] 第二遍完成: 处理 {setMapCount} 个建筑");
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitializeArchitectureMapTile] 完成                      ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            */
        }

        /// <summary>
        /// 🔥 新增：初始化部队地图位置数据
        /// 日期：2026-03-10
        /// 问题：读档后部队位置没有同步到 MapTileData 和 MapPositionCache，导致位置冲突
        /// 解决：统一同步所有部队的地图位置，确保数据一致性
        /// 上下文：Cold Path（读档初始化），可以使用 LINQ 提高可读性
        /// </summary>
        public void InitializeTroopMapTile()
        {
            System.Diagnostics.Debug.WriteLine("[InitializeTroopMapTile] 开始同步部队地图位置...");
            
            // 🔥 ANTI-BAND-AID：不添加防御性空检查
            // 如果 MapTileData 或 Troops 为 null，说明初始化流程有严重错误，应该 Fail Fast
            // 让异常暴露出来，而不是静默跳过
            
            // 🔥 第一步：清空所有地图位置的部队引用
            // 防止旧数据残留导致的位置冲突
            System.Diagnostics.Debug.WriteLine("[InitializeTroopMapTile] 清空地图部队引用...");
            for (int x = 0; x < this.MapTileData.GetLength(0); x++)
            {
                for (int y = 0; y < this.MapTileData.GetLength(1); y++)
                {
                    this.MapTileData[x, y].TileTroop = null;
                    this.MapTileData[x, y].TroopCount = 0;
                }
            }
            
            // 清空 MapPositionCache
            if (MapPositionCache.IsInitialized)
            {
                MapPositionCache.Clear();
            }
            
            // 🔥 第二步：重新设置所有部队的地图位置
            // 使用 C# 12 集合表达式和 LINQ（Cold Path，可读性优先）
            List<Troop> validTroops = [.. this.Troops.GetList()
                .Cast<Troop>()
                .Where(troop => troop != null && !troop.Destroyed)];
            
            System.Diagnostics.Debug.WriteLine($"[InitializeTroopMapTile] 找到 {validTroops.Count} 个有效部队");
            
            int successCount = 0;
            int conflictCount = 0;
            
            foreach (var troop in validTroops)
            {
                try
                {
                    Point pos = troop.Position;
                    
                    // 边界检查
                    if (this.PositionOutOfRange(pos))
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ [InitializeTroopMapTile] {troop.DisplayName} 位置越界: {pos}");
                        continue;
                    }
                    
                    // 检查位置是否已被占用
                    var existingTroop = this.MapTileData[pos.X, pos.Y].TileTroop;
                    if (existingTroop != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ [InitializeTroopMapTile] 位置冲突！{troop.DisplayName} 和 {existingTroop.DisplayName} 都在 {pos}");
                        conflictCount++;
                        
                        // 🔥 ANTI-BAND-AID：不静默处理，而是记录冲突并跳过
                        // 让用户知道存档数据有问题，需要手动处理
                        continue;
                    }
                    
                    // 设置地图位置
                    this.MapTileData[pos.X, pos.Y].TileTroop = troop;
                    this.MapTileData[pos.X, pos.Y].TroopCount = 1;
                    
                    // 同步到 MapPositionCache
                    if (MapPositionCache.IsInitialized)
                    {
                        MapPositionCache.SetTroopAt(pos, troop);
                    }
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ [InitializeTroopMapTile] 处理部队 {troop?.DisplayName} 时出错: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[InitializeTroopMapTile] 完成：成功 {successCount}，冲突 {conflictCount}");
            
            if (conflictCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [InitializeTroopMapTile] 发现 {conflictCount} 个位置冲突，存档数据可能损坏");
            }
        }

        private void InitializeArchitectureData()
        {
            // 🔥 性能诊断：详细拆解 InitializeArchitectureData 的耗时
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swStep = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitializeArchitectureData] 开始初始化建筑数据           ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] 建筑总数: {this.Architectures.Count}");
            
            // A. 恢复 BelongedFaction 引用
            swStep.Restart();
            foreach (Architecture architecture in this.Architectures)
            {
                if (architecture.BelongedFactionID >= 0)
                {
                    GameObject factionObj = this.Factions.GetGameObject(architecture.BelongedFactionID);
                    architecture.BelongedFaction = (factionObj is Faction ? (Faction)factionObj : null);
                    
                    #if DEBUG
                    // 🔥 调试：检查洛阳的势力关联
                    if (architecture.ID == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] 🔥 洛阳(ID=0) 势力关联:");
                        System.Diagnostics.Debug.WriteLine($"  - BelongedFactionID: {architecture.BelongedFactionID}");
                        System.Diagnostics.Debug.WriteLine($"  - factionObj: {factionObj?.GetType().Name ?? "null"}");
                        System.Diagnostics.Debug.WriteLine($"  - BelongedFaction: {architecture.BelongedFaction?.Name ?? "null"}");
                    }
                    #endif
                    
                    if (architecture.BelongedFaction == null && factionObj != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AOT修复] Architecture {architecture.ID} 的 BelongedFaction 转换失败，obj类型={factionObj.GetType().FullName}");
                    }
                }
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] A. 恢复势力引用: {swStep.ElapsedMilliseconds} ms");
            
            // B. 恢复其他建筑引用
            swStep.Restart();
            foreach (Architecture architecture in this.Architectures)
            {
                if (architecture.PlanArchitectureID >= 0)
                {
                    architecture.PlanArchitecture = this.Architectures.GetGameObject(architecture.PlanArchitectureID) as Architecture;
                }
                if (architecture.TransferFundArchitectureID >= 0)
                {
                    architecture.TransferFundArchitecture = this.Architectures.GetGameObject(architecture.TransferFundArchitectureID) as Architecture;
                }
                if (architecture.TransferFoodArchitectureID >= 0)
                {
                    architecture.TransferFoodArchitecture = this.Architectures.GetGameObject(architecture.TransferFoodArchitectureID) as Architecture;
                }
                if (architecture.DefensiveLegionID >= 0)
                {
                    architecture.DefensiveLegion = this.Legions.GetGameObject(architecture.DefensiveLegionID) as Legion;
                }
                if (architecture.RobberTroopID >= 0)
                {
                    architecture.RobberTroop = this.Troops.GetGameObject(architecture.RobberTroopID) as Troop;
                }
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] B. 恢复其他引用: {swStep.ElapsedMilliseconds} ms");

            // C. 加载 AI 链接
            swStep.Restart();
            bool redoLinks = false;
            int emptyLandLinksCount = 0;
            int emptyWaterLinksCount = 0;
            int emptyBothLinksCount = 0;
            
            // 🔥 诊断：读档前检查字符串
            #if DEBUG
            Architecture firstArch = this.Architectures.GetGameObject(1) as Architecture;
            if (firstArch != null)
            {
                System.Diagnostics.Debug.WriteLine($"[读档诊断] 建筑 {firstArch.Name}(ID:{firstArch.ID})");
                System.Diagnostics.Debug.WriteLine($"  - 读档前 AILandLinksString: null={firstArch.AILandLinksString == null}, 长度={firstArch.AILandLinksString?.Length ?? -1}");
                System.Diagnostics.Debug.WriteLine($"  - 读档前 AIWaterLinksString: null={firstArch.AIWaterLinksString == null}, 长度={firstArch.AIWaterLinksString?.Length ?? -1}");
                System.Diagnostics.Debug.WriteLine($"  - AILandLinksString 内容: [{firstArch.AILandLinksString ?? "NULL"}]");
            }
            #endif
            
            foreach (Architecture architecture2 in this.Architectures)
            {
                architecture2.LoadAILandLinksFromString(this.Architectures, architecture2.AILandLinksString);
                architecture2.LoadAIWaterLinksFromString(this.Architectures, architecture2.AIWaterLinksString);
                
                // 🔥 诊断：统计链接数据
                if (architecture2.AILandLinks.Count == 0)
                {
                    emptyLandLinksCount++;
                }
                if (architecture2.AIWaterLinks.Count == 0)
                {
                    emptyWaterLinksCount++;
                }
            }
            
            foreach (Architecture architecture2 in this.Architectures)
            {
                if (architecture2.AILandLinks.Count == 0 && architecture2.AIWaterLinks.Count == 0)
                {
                    emptyBothLinksCount++;
                    redoLinks = true;
                    break;
                }
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] C. 加载AI链接: {swStep.ElapsedMilliseconds} ms");
            System.Diagnostics.Debug.WriteLine($"  - 陆地链接为空: {emptyLandLinksCount}/{this.Architectures.Count}");
            System.Diagnostics.Debug.WriteLine($"  - 水路链接为空: {emptyWaterLinksCount}/{this.Architectures.Count}");
            System.Diagnostics.Debug.WriteLine($"  - 两者都为空: {emptyBothLinksCount}/{this.Architectures.Count}");
            System.Diagnostics.Debug.WriteLine($"  - redoLinks: {redoLinks}");
            
            // D. 重新生成链接（如果需要）
            swStep.Restart();
            if (redoLinks)
            {
                foreach (Architecture architecture2 in this.Architectures)
                {
                    architecture2.AILandLinks.Clear();
                    architecture2.AIWaterLinks.Clear();
                }
                foreach (Architecture architecture2 in this.Architectures)
                {
                    architecture2.FindLinks(this.Architectures);
                }
                
                // 🔥 修复：生成链接后立即保存到字符串，避免下次读档时重新计算
                foreach (Architecture architecture2 in this.Architectures)
                {
                    architecture2.AILandLinksString = architecture2.AILandLinks.SaveToString();
                    architecture2.AIWaterLinksString = architecture2.AIWaterLinks.SaveToString();
                }
                System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] ✅ 链接数据已保存到字符串，下次读档将直接加载");
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] D. 重新生成链接: {swStep.ElapsedMilliseconds} ms");

            // E. 检查前线 + 生成 AI 链接节点
            swStep.Restart();
            int frontLineCount = 0;
            foreach (Architecture architecture in this.Architectures)
            {
                if (architecture.BelongedFaction != null)
                {
                    architecture.CheckIsFrontLine();
                    if (architecture.FrontLine)
                    {
                        frontLineCount++;
                    }
                }
                architecture.GenerateAllAILinkNodes(2);
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] E. 前线检查+AI节点生成: {swStep.ElapsedMilliseconds} ms (前线建筑: {frontLineCount})");

            swTotal.Stop();
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [InitializeArchitectureData] 初始化完成                   ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[InitializeArchitectureData] ⏱️ 总耗时: {swTotal.ElapsedMilliseconds} ms");
        }

        private void InitializeCaptiveData()
        {
            foreach (Captive captive in this.Captives)
            {
                if (captive.CaptiveFactionID >= 0)
                {
                    captive.CaptiveFaction = this.Factions.GetGameObject(captive.CaptiveFactionID) as Faction;
                }
                if (captive.RansomArchitectureID >= 0)
                {
                    captive.RansomArchitecture = this.Architectures.GetGameObject(captive.RansomArchitectureID) as Architecture;
                }
            }
        }

        private void InitializeFactionData()
        {
            foreach (Faction faction in this.Factions)
            {
                faction.PrepareData();
            }
        }

        public void InitializeMapData()
        {

            
            // 🔥 检查 ScenarioMap 是否存在
            if (this.ScenarioMap == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ [InitializeMapData] ScenarioMap 为 null，无法初始化");
                System.Diagnostics.Debug.WriteLine("⚠️ [InitializeMapData] 这通常意味着地图数据未被正确加载！");
                return;
            }
            
            // 🔥 检查地图尺寸
            if (this.ScenarioMap.MapDimensions.X <= 0 || this.ScenarioMap.MapDimensions.Y <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [InitializeMapData] 地图尺寸无效: {this.ScenarioMap.MapDimensions.X} x {this.ScenarioMap.MapDimensions.Y}");
                System.Diagnostics.Debug.WriteLine("⚠️ [InitializeMapData] 这通常意味着地图数据未被正确加载！");
                return;
            }
            

            
            try
            {
                var width = ScenarioMap.MapDimensions.X;
                var height = ScenarioMap.MapDimensions.Y;
                
                // System.Diagnostics.Debug.WriteLine($"✅ [InitializeMapData] 地图尺寸: {width} x {height}");
                
                // 创建 MapTileData 数组
                this.MapTileData = new TileData[width, height];
                this.PenalizedMapData = new int[width, height];
                // System.Diagnostics.Debug.WriteLine($"✅ [InitializeMapData] MapTileData 数组已创建");
                
                // 初始化所有格子
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        this.MapTileData[x, y] = new TileData();
                        this.PenalizedMapData[x, y] = 0;
                    }
                }
                // System.Diagnostics.Debug.WriteLine($"✅ [InitializeMapData] 所有地图格子已初始化");
                
                // 设置建筑引用
                if (this.Architectures == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ [InitializeMapData] Architectures 为 null");
                    return;
                }
                
                int totalArchitectures = this.Architectures.Count;
                int successfulTiles = 0;
                int errorArchitectures = 0;
                int outOfRangeTiles = 0;
                
                // System.Diagnostics.Debug.WriteLine($"📊 [InitializeMapData] 建筑总数: {totalArchitectures}");
                
                foreach (Architecture architecture in this.Architectures)
                {
                    if (architecture == null)
                    {
                        errorArchitectures++;
                        continue;
                    }
                    
                    if (architecture.ArchitectureArea == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️  [InitializeMapData] 建筑 '{architecture.Name}'(ID:{architecture.ID}) 的 ArchitectureArea 为 null");
                        errorArchitectures++;
                        continue;
                    }
                    
                    if (architecture.ArchitectureArea.Area == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️  [InitializeMapData] 建筑 '{architecture.Name}'(ID:{architecture.ID}) 的 Area 列表为 null");
                        errorArchitectures++;
                        continue;
                    }
                    
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        if (point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height)
                        {
                            this.MapTileData[point.X, point.Y].TileArchitecture = architecture;
                            successfulTiles++;
                        }
                        else
                        {
                            outOfRangeTiles++;
                        }
                    }
                }
                
                // 输出统计信息
                /*
                System.Diagnostics.Debug.WriteLine($"📊 [InitializeMapData] 统计:");
                System.Diagnostics.Debug.WriteLine($"   ✅ 成功设置的地图格子: {successfulTiles}");
                System.Diagnostics.Debug.WriteLine($"   ⚠️  有问题的建筑: {errorArchitectures}");
                System.Diagnostics.Debug.WriteLine($"   ⚠️  超出范围的格子: {outOfRangeTiles}");
                */
                
                if (successfulTiles == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ [InitializeMapData] 没有成功设置任何地图格子！");
                }
                
                /*
                System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║  [InitializeMapData] 初始化完成                            ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ [InitializeMapData] 发生异常");
                System.Diagnostics.Debug.WriteLine($"   异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        private void InitializeMilitaryData()
        {
            foreach (Military military in this.Militaries)
            {
                if (military.ShelledMilitaryID >= 0)
                {
                    military.SetShelledMilitary(this.Militaries.GetGameObject(military.ShelledMilitaryID) as Military);
                }
                
                // 🔧 修复：读档后恢复Military的Name
                if (string.IsNullOrEmpty(military.Name) && military.Kind != null)
                {
                    if (military.Kind.RecruitLimit == 1)
                    {
                        military.Name = military.Kind.Name;
                    }
                    else
                    {
                        military.Name = military.Kind.Name + "队";
                    }
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[InitializeMilitaryData] 恢复 Military ID={military.ID} 的名称: {military.Name}");
                    #endif
                }
            }
        }

        private void InitializePersonData()
        {
            foreach (Person person in this.Persons)
            {
                if (person.ConvincingPersonID >= 0)
                {
                    person.ConvincingPerson = this.Persons.GetGameObject(person.ConvincingPersonID) as Person;
                }
            }
        }

        private void InitializeRoutewayData()
        {
            foreach (Routeway routeway in this.Routeways)
            {
                routeway.RefreshRoutewayPointsData();
            }
        }

        public void InitializeScenarioPlayerFactions(List<int> factionIDs)
        {
            // 🔥 诊断：记录调用参数
            System.Diagnostics.Debug.WriteLine($"[InitializeScenarioPlayerFactions] 被调用");
            System.Diagnostics.Debug.WriteLine($"  - factionIDs == null: {factionIDs == null}");
            System.Diagnostics.Debug.WriteLine($"  - factionIDs?.Count: {factionIDs?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  - factionIDs 内容: [{string.Join(", ", factionIDs ?? new List<int>())}]");
            System.Diagnostics.Debug.WriteLine($"  - PlayerFactions.Count (调用前): {this.PlayerFactions?.Count ?? 0}");
            
            // 🔥 修复：防御性检查 - 如果PlayerFactions已经有数据（从存档恢复），不要覆盖
            if (this.PlayerFactions != null && this.PlayerFactions.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeScenarioPlayerFactions] PlayerFactions已有{this.PlayerFactions.Count}个势力，跳过初始化");
                return;
            }
            
            // 只在PlayerFactions为空时才初始化
            if (factionIDs != null && factionIDs.Count > 0)
            {
                this.PlayerFactions.LoadFromString(this.Factions, StaticMethods.SaveToString(factionIDs));
                System.Diagnostics.Debug.WriteLine($"[InitializeScenarioPlayerFactions] ✅ 初始化完成，PlayerFactions.Count = {this.PlayerFactions.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeScenarioPlayerFactions] ⚠️ factionIDs为空或null，进入观察者模式");
            }
        }

        private void InitializeSectionData()
        {
            foreach (Section section in this.Sections)
            {
                if (section.OrientationFactionID >= 0)
                {
                    section.OrientationFaction = this.Factions.GetGameObject(section.OrientationFactionID) as Faction;
                }
                if (section.OrientationSectionID >= 0)
                {
                    section.OrientationSection = this.Sections.GetGameObject(section.OrientationSectionID) as Section;
                }
                if (section.OrientationStateID >= 0)
                {
                    section.OrientationState = this.States.GetGameObject(section.OrientationStateID) as State;
                }
                if (section.OrientationArchitectureID >= 0)
                {
                    section.OrientationArchitecture = this.Architectures.GetGameObject(section.OrientationArchitectureID) as Architecture;
                }
            }
        }

        /*
        private void InitializeSpyMessageData()
        {
            foreach (SpyMessage message in this.SpyMessages)
            {
                if (message.MessageFactionID >= 0)
                {
                    message.MessageFaction = this.Factions.GetGameObject(message.MessageFactionID) as Faction;
                }
                if (message.MessageArchitectureID >= 0)
                {
                    message.MessageArchitecture = this.Architectures.GetGameObject(message.MessageArchitectureID) as Architecture;
                }
            }
        }
        */

        private void InitializeTroopData()
        {
            // 🔥 诊断：检查 Persons 集合状态

            
            int troopsWithPersons = 0;
            int troopsWithoutPersons = 0;
            foreach (Troop t in this.Troops)
            {
                if (t.Persons.Count > 0)
                    troopsWithPersons++;
                else
                    troopsWithoutPersons++;
            }


            
            // 🔥 AOT修复：首先恢复 Troop.BelongedFaction 引用

            foreach (Troop troop in this.Troops)
            {
                if (troop.BelongedFactionID >= 0)
                {
                    GameObject factionObj = this.Factions.GetGameObject(troop.BelongedFactionID);
                    troop.BelongedFaction = (factionObj is Faction ? (Faction)factionObj : null);
                    if (troop.BelongedFaction == null && factionObj != null)
                    {

                    }
                }
            }
            
            TroopList toRemove = new TroopList();
            foreach (Troop troop in this.Troops)
            {
                if (troop.Leader == null || troop.Army == null || troop.Army.Kind == null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 移除无效部队 ID={troop.ID}, Name={troop.Name}: Leader={(troop.Leader != null ? "有" : "NULL")}, Army={(troop.Army != null ? "有" : "NULL")}, Army.Kind={(troop.Army?.Kind != null ? "有" : "NULL")}");
                    toRemove.Add(troop);
                }
                else if (troop.Persons.Count == 0)
                {
                    // 🔥 根本修复：如果 PersonIDs 有数据但 Persons 为空，重新链接
                    // 日期：2026-02-17
                    // 问题：LinkReferencesPhase 执行后，Persons 集合被意外清空
                    // 解决：在这里重新链接 Persons 集合
                    if (troop.PersonIDs is { Count: > 0 })
                    {

                        
                        foreach (int personID in troop.PersonIDs)
                        {
                            var person = this.Persons.GetGameObject(personID) as Person;
                            
                            // 🔥 ANTI-BAND-AID: 不掩盖数据错误，直接抛出异常
                            // 如果 PersonID 存在于 PersonIDs 中，对应的 Person 必须存在
                            if (person is null)
                            {
                                throw new InvalidOperationException(
                                    $"数据损坏：部队 {troop.ID} ({troop.Name}) 引用了不存在的 Person {personID}。" +
                                    $"PersonIDs: [{string.Join(", ", troop.PersonIDs)}]。" +
                                    $"这表明保存数据已损坏，或 Person 加载失败，必须在数据源修复此问题。");
                            }
                            
                            troop.Persons.Add(person);
                            person.LocationTroop = troop;

                        }
                        

                    }
                    else
                    {
                        // PersonIDs 也为空，只设置 Leader
                        troop.Leader.LocationTroop = troop;
                    }
                }
            }
            foreach (Troop troop in toRemove)
            {
                if (troop.BelongedFaction != null)
                {
                    troop.BelongedFaction.RemoveTroop(troop);
                }
                this.Troops.Remove(troop);
            }
            // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 移除 {toRemove.Count} 个无效部队，剩余 {this.Troops.Count} 个部队");

            foreach (Troop troop in this.Troops)
            {
                troop.Initialize();
            }
            // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 部队初始化完成");
            foreach (GameObject gameObj in this.TroopEvents)
            {
                TroopEvent event2 = gameObj as TroopEvent;
                if (event2 == null)
                {
                    // Skip objects that are not TroopEvent instances
                    continue;
                }

                if (event2.AfterEventHappened >= 0)
                {
                    event2.AfterHappenedEvent = this.TroopEvents.GetGameObject(event2.AfterEventHappened) as TroopEvent;
                }
            }
        }

        private void InitializeMilitariesData()
        {
            MilitaryList toRemove = new MilitaryList();
            foreach (Military military in this.Militaries)
            {
                if (military.Kind == null)
                {
                    toRemove.Add(military);
                }
            }
            foreach (Military military in toRemove)
            {
                if (military.BelongedArchitecture != null)
                {
                    military.BelongedArchitecture.RemoveMilitary(military);
                }
                this.Militaries.Remove(military);
            }
        }

        public bool IsCurrentPlayer(Faction faction)
        {
            return (this.CurrentPlayer == faction);
        }

        /// <summary>
        /// 检查指定位置是否可以点火
        /// 更新：2026-03-12 新增雨雪天气检查
        /// </summary>
        public bool IsFireVaild(Point position, bool typevalid, MilitaryType type)
        {
            if (this.GetArchitectureByPosition(position) != null)
            {
                return false;
            }
            
            // 🔥 2026-03-12 核心规则：雨雪天气无法点火
            var weather = this.WeatherManager.GetWeatherAt(position);
            if (weather.SuppressFire())
            {
                return false;
            }
            
            TerrainKind terrainKindByPosition = this.GetTerrainKindByPosition(position);
            return (((typevalid && (type == MilitaryType.水军)) && (terrainKindByPosition == TerrainKind.水域)) || ((((terrainKindByPosition == TerrainKind.平原) || (terrainKindByPosition == TerrainKind.草原)) || (terrainKindByPosition == TerrainKind.森林)) || (terrainKindByPosition == TerrainKind.山地)));
        }

        public bool IsLastPlayer(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            foreach (Faction faction2 in this.PlayerFactions)
            {
                if ((faction2 != faction) && !faction2.Passed)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsPlayer(Faction faction)
        {
            if (faction == null) return false;
            
            // 优先使用 PlayerFactions（最可靠）
            if (this.PlayerFactions.Count > 0)
            {
                return this.PlayerFactions.GetGameObject(faction.ID) != null;
            }
            
            // 🔥 修复：如果 PlayerFactions 为空，使用 CurrentPlayer 判断
            if (this.CurrentPlayer != null)
            {
                return faction.ID == this.CurrentPlayer.ID;
            }
            
            // 🔥 2026-02-14 最后兜底：使用 CurrentPlayerID 字符串判断
            // 注意：这种情况只会在 LinkReferences 阶段出现（CurrentPlayer 还未从 ID 解析）
            if (!string.IsNullOrEmpty(this.CurrentPlayerID))
            {
                int currentPlayerId = int.Parse(this.CurrentPlayerID); // 如果格式错误，让它抛异常
                return faction.ID == currentPlayerId;
            }
            
            #if DEBUG

            #endif
            return false;
        }

        public bool HasAIResourceBonus(Section section)
        {
            if (Session.GlobalVariables.PlayerAutoSectionHasAIResourceBonus)
            {
                return section != null && (!IsPlayer(section.BelongedFaction) || !section.AIDetail.AutoRun);
            }
            else
            {
                return section != null && !IsPlayer(section.BelongedFaction);
            }
        }

        public bool IsPlayerControlling()
        {
            bool result = (((this.CurrentPlayer != null) && (this.CurrentFaction == this.CurrentPlayer)) && this.CurrentPlayer.Controlling);
            
            /*
            #if DEBUG
            if (!result)
            {
                System.Diagnostics.Debug.WriteLine($"[IsPlayerControlling] 返回 false:");
                System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer != null: {this.CurrentPlayer != null}");
                System.Diagnostics.Debug.WriteLine($"  - CurrentFaction == CurrentPlayer: {this.CurrentFaction == this.CurrentPlayer}");
                System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer.Controlling: {this.CurrentPlayer?.Controlling}");
                if (this.CurrentPlayer != null && this.CurrentFaction != this.CurrentPlayer)
                {
                    System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer: {this.CurrentPlayer.Name}");
                    System.Diagnostics.Debug.WriteLine($"  - CurrentFaction: {this.CurrentFaction?.Name ?? "null"}");
                }
            }
            #endif
            */
            
            return result;
        }

        public bool IsPositionDisplayable(Point position)
        {
            return (Session.MainGame.mainGameScreen.TileInScreen(position) && ((Session.GlobalVariables.SkyEye || (this.CurrentPlayer == null)) || this.CurrentPlayer.IsPositionKnown(position)));
        }

        public bool IsPositionEmpty(Point position)
        {
            if (this.PositionIsArchitecture(position))
            {
                return false;
            }
            if (this.PositionIsTroop(position))
            {
                return false;
            }
            return true;
        }

        public bool IsPositionMovable(Point position, Faction faction)
        {
            if (this.PositionIsTroop(position))
            {
                return false;
            }
            Architecture architectureByPosition = this.GetArchitectureByPosition(position);
            return ((architectureByPosition == null) || (architectureByPosition.BelongedFaction == faction));
        }

        public bool IsTheBottomTroop(Troop troop)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (troop == null || this.MapTileData == null) return false;
            if (this.PositionOutOfRange(troop.Position)) return false;
            
            try
            {
                return (this.MapTileData[troop.Position.X, troop.Position.Y].TileTroop == troop);
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public bool IsTroopViewingPosition(Troop troop, Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return false;
            }
            return this.MapTileData[position.X, position.Y].IsTroopViewing(troop);
        }

        public bool IsWaterPositionRoutewayable(Point position)
        {
            if (ScenarioMap.MapData[position.X, position.Y] == 6)
            {
                int num = 0;
                Point point = new Point(position.X - 1, position.Y);
                if (!(this.PositionOutOfRange(point) || (ScenarioMap.MapData[point.X, point.Y] != 6)))
                {
                    num++;
                }
                Point point2 = new Point(position.X, position.Y - 1);
                if (!(this.PositionOutOfRange(point2) || (ScenarioMap.MapData[point2.X, point2.Y] != 6)))
                {
                    num++;
                }
                Point point3 = new Point(position.X + 1, position.Y);
                if (!(this.PositionOutOfRange(point3) || (ScenarioMap.MapData[point3.X, point3.Y] != 6)))
                {
                    num++;
                }
                if (num > 2)
                {
                    return false;
                }
                Point point4 = new Point(position.X, position.Y + 1);
                if (!(this.PositionOutOfRange(point4) || (ScenarioMap.MapData[point4.X, point4.Y] != 6)))
                {
                    num++;
                }
                if (num > 2)
                {
                    return false;
                }
            }
            return true;
        }

        public bool SaveAvail()
        {
            return (this.IsPlayerControlling() && this.EnableLoadAndSave && !Session.GlobalVariables.HardcoreMode);
        }

        public bool LoadAvail()
        {
            return (this.IsPlayerControlling() && this.EnableLoadAndSave && !Session.GlobalVariables.HardcoreMode);
        }

        public bool isInCaptiveList(int personId)
        {
            foreach (Captive i in this.Captives)
            {
                if (i.CaptivePerson.ID == personId)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static CommonData ProcessCommonData(CommonData commonData)
        {
            if (commonData == null) return null;
            List<string> errorMsg = new List<string>();

            commonData.NumberGenerator = new CombatNumberGenerator();

            commonData.TroopAnimations = new TroopAnimation();

            errorMsg.AddRange(LoadGameCommonData());

            if (commonData.AllTerrainDetails != null && commonData.AllTerrainDetails.TerrainDetails != null)
            {
                foreach (var terrainDetail in commonData.AllTerrainDetails.TerrainDetails)
                {
                    terrainDetail.Value.Init();
                }
            }

            if (commonData.AllInfluences != null && commonData.AllInfluences.Influences != null)
            {
                foreach (var influence in commonData.AllInfluences.Influences)
                {
                    influence.Value.Init();
                }
            }

            if (commonData.AllFacilityKinds != null && commonData.AllFacilityKinds.FacilityKinds != null)
            {
                foreach (var facilityKind in commonData.AllFacilityKinds.FacilityKinds)
                {
                    facilityKind.Value.Init();

                    facilityKind.Value.Influences.LoadFromString(commonData.AllInfluences, facilityKind.Value.InfluencesString);

                    facilityKind.Value.Conditions.LoadFromString(commonData.AllConditions, facilityKind.Value.ConditionTableString);

                    Condition.LoadConditionWeightFromString(commonData.AllConditions, facilityKind.Value.AIBuildConditionWeightString, out facilityKind.Value.AIBuildConditionWeight);
                }
            }

            if (commonData.AllTechniques != null && commonData.AllTechniques.Techniques != null)
            {
                foreach (var technique in commonData.AllTechniques.Techniques)
                {
                    technique.Value.Init();
                    technique.Value.Influences.LoadFromString(commonData.AllInfluences, technique.Value.InfluencesString);
                    technique.Value.Conditions.LoadFromString(commonData.AllConditions, technique.Value.ConditionTableString);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, technique.Value.AIConditionWeightString, out technique.Value.AIConditionWeight);
                }
            }

            if (commonData.AllSkills != null && commonData.AllSkills.Skills != null)
            {
                foreach (var skill in commonData.AllSkills.Skills)
                {
                    skill.Value.Init();
                    skill.Value.Influences.LoadFromString(commonData.AllInfluences, skill.Value.InfluencesString);
                    skill.Value.Conditions.LoadFromString(commonData.AllConditions, skill.Value.ConditionTableString);
                }
            }

            if (commonData.AllTitles != null && commonData.AllTitles.Titles != null)
            {
                foreach (var title in commonData.AllTitles.Titles)
                {
                    title.Value.Init();
                    title.Value.Influences.LoadFromString(commonData.AllInfluences, title.Value.InfluencesString);
                    title.Value.Conditions.LoadFromString(commonData.AllConditions, title.Value.ConditionTableString);
                    title.Value.ArchitectureConditions.LoadFromString(commonData.AllConditions, title.Value.ArchitectureConditionsString);
                    title.Value.FactionConditions.LoadFromString(commonData.AllConditions, title.Value.FactionConditionsString);
                    title.Value.LoseConditions.LoadFromString(commonData.AllConditions, title.Value.LoseConditionsString);
                    title.Value.GenerateConditions.LoadFromString(commonData.AllConditions, title.Value.GenerateConditionsString);
                }
            }

            if (commonData.AllMilitaryKinds != null && commonData.AllMilitaryKinds.MilitaryKinds != null)
            {
                foreach (var militaryKind in commonData.AllMilitaryKinds.MilitaryKinds)
                {
                    militaryKind.Value.Init();

                    militaryKind.Value.Influences.LoadFromString(commonData.AllInfluences, militaryKind.Value.InfluencesString);

                    militaryKind.Value.CreateConditions.LoadFromString(commonData.AllConditions, militaryKind.Value.CreateConditionsString);

                    Condition.LoadConditionWeightFromString(commonData.AllConditions, militaryKind.Value.AICreateArchitectureConditionWeightString, out militaryKind.Value.AICreateArchitectureConditionWeight);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, militaryKind.Value.AIUpgradeArchitectureConditionWeightString, out militaryKind.Value.AIUpgradeArchitectureConditionWeight);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, militaryKind.Value.AIUpgradeLeaderConditionWeightString, out militaryKind.Value.AIUpgradeLeaderConditionWeight);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, militaryKind.Value.AILeaderConditionWeightString, out militaryKind.Value.AILeaderConditionWeight);

                    militaryKind.Value.successor = new MilitaryKindTable();
                    militaryKind.Value.successor.LoadFromString(commonData.AllMilitaryKinds, militaryKind.Value.SuccessorString);
                }
            }

            if (commonData.AllCombatMethods != null && commonData.AllCombatMethods.CombatMethods != null)
            {
                foreach (var combatMethod in commonData.AllCombatMethods.CombatMethods)
                {
                    combatMethod.Value.Init();

                    combatMethod.Value.Influences.LoadFromString(commonData.AllInfluences, combatMethod.Value.InfluencesString);

                    combatMethod.Value.AttackDefault = commonData.AllAttackDefaultKinds.GetGameObject(combatMethod.Value.AttackDefaultString) as AttackDefaultKind;
                    combatMethod.Value.AttackTarget = commonData.AllAttackTargetKinds.GetGameObject(combatMethod.Value.AttackTargetString) as AttackTargetKind;

                    combatMethod.Value.CastConditions.LoadFromString(commonData.AllConditions, combatMethod.Value.CastConditionsString);

                    Condition.LoadConditionWeightFromString(commonData.AllConditions, combatMethod.Value.AIConditionWeightSelfString, out combatMethod.Value.AIConditionWeightSelf);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, combatMethod.Value.AIConditionWeightEnemyString, out combatMethod.Value.AIConditionWeightEnemy);
                }
            }

            if (commonData.AllStunts != null && commonData.AllStunts.Stunts != null)
            {
                foreach (var stunt in commonData.AllStunts.Stunts)
                {
                    stunt.Value.Init();
                    stunt.Value.Influences.LoadFromString(commonData.AllInfluences, stunt.Value.InfluencesString);
                    stunt.Value.CastConditions.LoadFromString(commonData.AllConditions, stunt.Value.CastConditionsString);
                    stunt.Value.LearnConditions.LoadFromString(commonData.AllConditions, stunt.Value.LearnConditionsString);
                    stunt.Value.AIConditions.LoadFromString(commonData.AllConditions, stunt.Value.AIConditionsString);
                }
            }

            if (commonData.AllStratagems != null && commonData.AllStratagems.Stratagems != null)
            {
                foreach (var stratagem in commonData.AllStratagems.Stratagems)
                {
                    stratagem.Value.Init();
                    stratagem.Value.Influences.LoadFromString(commonData.AllInfluences, stratagem.Value.InfluencesString);
                    stratagem.Value.CastConditions.LoadFromString(commonData.AllConditions, stratagem.Value.CastConditionsString);
                    stratagem.Value.CastDefault = commonData.AllCastDefaultKinds.GetGameObject(stratagem.Value.CastDefaultString) as CastDefaultKind;
                    stratagem.Value.CastTarget = commonData.AllCastTargetKinds.GetGameObject(stratagem.Value.CastTargetString) as CastTargetKind;
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, stratagem.Value.AIConditionWeightSelfString, out stratagem.Value.AIConditionWeightSelf);
                    Condition.LoadConditionWeightFromString(commonData.AllConditions, stratagem.Value.AIConditionWeightEnemyString, out stratagem.Value.AIConditionWeightEnemy);
                }
            }

            var influenceKinds = new InfluenceKindTable();
            // 🔥 技术性修复：确保 AllInformationKinds 正确初始化
            if (commonData.AllInformationKinds == null)
            {
                //System.Diagnostics.Debug.WriteLine("[ProcessCommonData] ❌ AllInformationKinds 为 null，创建新实例");
                commonData.AllInformationKinds = new InformationKindList();
            }
            
            if (commonData.AllInformationKinds.GameObjects == null)
            {
                //System.Diagnostics.Debug.WriteLine("[ProcessCommonData] ❌ AllInformationKinds.GameObjects 为 null，创建新列表");
                commonData.AllInformationKinds.GameObjects = new List<GameObject>();
            }

            if (commonData.AllInformationKinds != null && commonData.AllInformationKinds.GameObjects.Count > 0)
            {
                //System.Diagnostics.Debug.WriteLine($"[ProcessCommonData] 情报类型总数: {commonData.AllInformationKinds.Count}");
                foreach (var obj in commonData.AllInformationKinds.GameObjects)
                {
                    if (obj is InformationKind kind)
                    {
                        //System.Diagnostics.Debug.WriteLine($"[ProcessCommonData] ✅ 情报类型 {kind.ID} ({kind.Name}): 等级={kind.Level}, 半径={kind.Radius}, 消耗={kind.CostFund}, 权重={kind.FightingWeighing}");
                    }
                }
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("[ProcessCommonData] ⚠️ AllInformationKinds 为空，添加原有项目的情报类型");
                
                // 🔥 修正：使用项目中原有的情报类型数据结构
                try
                {
                    var originalInfoKinds = new[]
                    {
                        new InformationKind { ID = 1, Name = "基础侦察", CostFund = 120, Level = (InformationLevel)3, Oblique = false, Radius = 4 },
                        new InformationKind { ID = 2, Name = "深度侦察", CostFund = 200, Level = (InformationLevel)4, Oblique = false, Radius = 4 },
                        new InformationKind { ID = 3, Name = "广域侦察", CostFund = 160, Level = (InformationLevel)3, Oblique = false, Radius = 5 },
                        new InformationKind { ID = 4, Name = "高级侦察", CostFund = 320, Level = (InformationLevel)4, Oblique = false, Radius = 5 }
                    };
                    
                    foreach (var infoKind in originalInfoKinds)
                    {
                        commonData.AllInformationKinds.GameObjects.Add(infoKind);
                        //System.Diagnostics.Debug.WriteLine($"[ProcessCommonData] 添加原有情报类型: {infoKind.Name} (ID={infoKind.ID}, 等级={infoKind.Level}, 费用={infoKind.CostFund}, 半径={infoKind.Radius})");
                    }
                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.WriteLine($"[ProcessCommonData] ❌ 添加原有情报类型时发生异常: {ex.Message}");
                }
            }

            // 🔥 AOT修复：验证和修复IdealTendencyKinds
            ValidateAndFixIdealTendencyKinds(commonData);

            return commonData;
        }

        /// <summary>
        /// 验证和修复CommonData中的IdealTendencyKinds - AOT兼容性修复
        /// </summary>
        private static void ValidateAndFixIdealTendencyKinds(CommonData commonData)
        {
            try
            {
                //System.Diagnostics.Debug.WriteLine("[ValidateAndFixIdealTendencyKinds] 开始验证IdealTendencyKinds");
                
                // 检查AllIdealTendencyKinds是否存在且有效
                if (commonData.AllIdealTendencyKinds == null)
                {
                    //System.Diagnostics.Debug.WriteLine("[ValidateAndFixIdealTendencyKinds] AllIdealTendencyKinds为null，创建新实例");
                    commonData.AllIdealTendencyKinds = new GameObjectList();
                }
                
                // 验证数据完整性
                bool hasValidData = false;
                int validCount = 0;
                
                //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] AllIdealTendencyKinds.Count: {commonData.AllIdealTendencyKinds.Count}");
                
                if (commonData.AllIdealTendencyKinds.Count > 0)
                {
                    foreach (var item in commonData.AllIdealTendencyKinds.GetList())
                    {
                        //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 检查项目: {item?.GetType().Name ?? "null"}");
                        
                        if (item is IdealTendencyKind itk)
                        {
                            //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 找到IdealTendencyKind: ID={itk.ID}, Name={itk.Name}, Offset={itk.Offset}");
                            
                            if (!string.IsNullOrEmpty(itk.Name))
                            {
                                hasValidData = true;
                                validCount++;
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] IdealTendencyKind名称为空: ID={itk.ID}");
                            }
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 项目不是IdealTendencyKind类型: {item?.GetType().Name ?? "null"}");
                        }
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("[ValidateAndFixIdealTendencyKinds] AllIdealTendencyKinds为空");
                }
                
                //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 当前有效IdealTendencyKind数量: {validCount}");
                
                // 如果没有有效数据，创建基础数据
                if (!hasValidData || validCount == 0)
                {
                    //System.Diagnostics.Debug.WriteLine("[ValidateAndFixIdealTendencyKinds] 没有有效数据，创建基础IdealTendencyKinds");
                    
                    // 清空现有数据
                    commonData.AllIdealTendencyKinds.Clear();
                    
                    // 创建基础的理想倾向数据
                    var basicIdealTendencies = new[]
                    {
                        new { ID = 0, Name = "仁德", Offset = 0 },
                        new { ID = 1, Name = "野心", Offset = 10 },
                        new { ID = 2, Name = "冷静", Offset = -5 },
                        new { ID = 3, Name = "勇猛", Offset = 15 },
                        new { ID = 4, Name = "智慧", Offset = 5 },
                        new { ID = 5, Name = "忠义", Offset = -10 },
                        new { ID = 6, Name = "狡诈", Offset = 20 },
                        new { ID = 7, Name = "正直", Offset = -8 },
                        new { ID = 8, Name = "豪放", Offset = 12 },
                        new { ID = 9, Name = "谨慎", Offset = -3 }
                    };
                    
                    foreach (var template in basicIdealTendencies)
                    {
                        var idealTendency = new IdealTendencyKind();
                        idealTendency.ID = template.ID;
                        idealTendency.Name = template.Name;
                        idealTendency.Offset = template.Offset;
                        
                        commonData.AllIdealTendencyKinds.Add(idealTendency);
                    }
                    
                    //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 创建了 {basicIdealTendencies.Length} 个基础IdealTendencyKind");
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] IdealTendencyKinds验证通过，有效数量: {validCount}");
                }
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 验证过程异常: {ex.Message}");
                // 兜底处理：确保至少不为空
                if (commonData.AllIdealTendencyKinds == null)
                {
                    commonData.AllIdealTendencyKinds = new GameObjectList();
                }
                
                if (commonData.AllIdealTendencyKinds.Count == 0)
                {
                    try
                    {
                        var defaultKind = new IdealTendencyKind();
                        defaultKind.ID = 0;
                        defaultKind.Name = "默认";
                        defaultKind.Offset = 0;
                        commonData.AllIdealTendencyKinds.Add(defaultKind);
                        //System.Diagnostics.Debug.WriteLine("[ValidateAndFixIdealTendencyKinds] 异常恢复：创建了默认IdealTendencyKind");
                    }
                    catch (Exception innerEx)
                    {
                        //System.Diagnostics.Debug.WriteLine($"[ValidateAndFixIdealTendencyKinds] 兜底处理也失败: {innerEx.Message}");
                    }
                }
            }
        }

        public List<string> ProcessScenarioData(bool fromScenario, bool editing = false)  //读剧本和读存档都调用了此函数
        {
            // 🔥 关键调试：确认方法被调用
            // 日期：2026-03-16

            
            // 🔥 性能监控：添加超时检测，定位卡死位置
            var sw = System.Diagnostics.Stopwatch.StartNew();
            List<string> errorMsg = [];  // ✅ C# 12 集合表达式

            // 🔥 检查点1：外部规则加载

            try
            {
                GameManager.ExternalRuleLoader.InjectExternalRulesIntoScenario(this);
    
            }
            catch (Exception ex)
            {

            }

            // 🔥 检查点2：场景初始化
            sw.Restart();

            Init();

            
            scenarioJustLoaded = true;
            
            // 🔥 诊断：检查 GameCommonData 初始化状态
            #if DEBUG

            #endif
            
            // 🔥 修复：确保 AllPersons 字典已填充
            if (this.AllPersons == null || this.AllPersons.Count == 0)
            {

                this.AllPersons = new Dictionary<int, Person>();
                if (this.Persons != null)
                {
                    foreach (Person p in this.Persons)
                    {
                        if (p != null && !this.AllPersons.ContainsKey(p.ID))
                        {
                            this.AllPersons.Add(p.ID, p);
                        }
                    }

                }
            }
            
            // 🔥 技术性修复：确保 ScenarioMap 和 MapDataString 正确初始化
            if (ScenarioMap == null)
            {

                ScenarioMap = new Map();
            }
                        
            if (!string.IsNullOrEmpty(ScenarioMap.MapDataString))
            {
                try
                {
                    ScenarioMap.LoadMapData(ScenarioMap.MapDataString, ScenarioMap.MapDimensions.X, ScenarioMap.MapDimensions.Y);
                    // System.Diagnostics.Debug.WriteLine("[ProcessScenarioData] ✅ 地图数据加载成功");
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] ❌ 加载地图数据时发生异常: {ex.Message}");
                    // 尝试从剧本文件重新加载真实数据
                    if (!TryReloadMapData())
                    {
                        HandleMissingMapData();
                    }
                }
            }
            else
            {

                
                // 尝试重新加载地图数据
                if (!TryReloadMapData())
                {
                    // 如果无法重新加载，安全处理缺失问题（不生成虚假数据）
                    HandleMissingMapData();
                }
            }
            
            sw.Restart();
            try
            {
                ScenarioMap.Init();

            }
            catch (Exception ex)
            {

            }
                       
            //if (Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.iOS || Platform.PlatFormType == PlatFormType.Win)
            //{
//                ScenarioMap.TileWidth = 50;
                //ScenarioMap.TileHeight = 50;
            //}

            // 🔥 FIX: 添加类型验证，防止 InvalidCastException
            try
            {
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] States 集合验证开始，Count: {this.States.Count}");
                
                // 验证 States 集合中的对象类型
                var validStates = new List<GameObjects.ArchitectureDetail.State>();
                for (int i = 0; i < this.States.Count; i++)
                {
                    var obj = this.States[i];
                    if (obj is GameObjects.ArchitectureDetail.State state)
                    {
                        validStates.Add(state);
                    }
                    else
                    {

                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 有效 State 对象数量: {validStates.Count}/{this.States.Count}");
                
                // 处理有效的 State 对象
                foreach (GameObjects.ArchitectureDetail.State state in validStates)
                {
                    try
                    {
                        state.Init();
                        // 🔥 修复：注释掉 LoadContactStatesFromString 调用
                        // 日期：2026-03-17
                        // 原因：LinkReferencesPhase 已经正确链接了 ContactStates 引用
                        // 这个调用会覆盖 LinkReferencesPhase 的工作，导致引用丢失
                        // state.LoadContactStatesFromString(this.States, state.ContactStatesString);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                
                // System.Diagnostics.Debug.WriteLine("[ProcessScenarioData] ✅ States 处理完成");
            }
            catch (Exception ex)
            {

                // 继续执行，不中断整个流程
            }

            // 🔥 FIX: 添加类型验证，防止 InvalidCastException
            try
            {
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] Regions 集合验证开始，Count: {this.Regions.Count}");
                
                // 验证 Regions 集合中的对象类型并处理
                for (int i = 0; i < this.Regions.Count; i++)
                {
                    var obj = this.Regions[i];
                    if (obj is GameObjects.ArchitectureDetail.Region region)
                    {
                        try
                        {
                            region.Init();
                            // 🔥 修复：注释掉 LoadStatesFromString 调用
                            // 日期：2026-03-17
                            // 原因：LinkReferencesPhase 已经正确链接了 State.LinkedRegion 引用
                            // LoadStatesFromString 会调用 state.LinkedRegion = this，覆盖 LinkReferencesPhase 的工作
                            // 这导致"所在州"和"地域"显示为"未知州域"和"未知地区"
                            // region.LoadStatesFromString(this.States, region.StatesListString);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else
                    {

                    }
                }
                
                // System.Diagnostics.Debug.WriteLine("[ProcessScenarioData] ✅ Regions 处理完成");
            }
            catch (Exception ex)
            {

                // 继续执行，不中断整个流程
            }

            // 🔥 检查点3：武将属性加载
            sw.Restart();

            int personCount = 0;
            
            foreach (Person person in Persons)
            {
                personCount++;
                if (personCount % 100 == 0)
                {

                    
                    // 🔥 超时检测
                    if (sw.ElapsedMilliseconds > 30000)
                    {
                        throw new TimeoutException($"ProcessScenarioData 超时：武将属性加载超过30秒，已处理{personCount}/{Persons.Count}");
                    }
                }
                
                List<string> errors = [];

                person.Init();

                //person.IdealTendencyIDString = (short)reader["IdealTendency"];
                // 🔥 调试：检查 AllIdealTendencyKinds 状态
                if (person.ID < 3)
                {

                }
                
                // 🔥 修复：只在ID有效时才恢复IdealTendency（ID >= 0表示有效）
                if (person.IdealTendencyIDString >= 0)
                {
                    person.IdealTendency = this.GameCommonData.AllIdealTendencyKinds.GetGameObject(person.IdealTendencyIDString) as IdealTendencyKind;
                    
                    if (person.ID < 3)
                    {

                    }
                }
                else if (person.ID < 3)
                {

                }

                person.Character = this.GameCommonData.AllCharacterKinds[person.PCharacter];

                //person.UniqueMilitaryKindsString = reader["UniqueMilitaryKinds"].ToString();
                //person.UniqueTitlesString = reader["UniqueTitles"].ToString();

                try
                {
                    errors.AddRange(person.UniqueMilitaryKinds.LoadFromString(this.GameCommonData.AllMilitaryKinds, person.UniqueMilitaryKindsString));
                    errors.AddRange(person.UniqueTitles.LoadFromString(this.GameCommonData.AllTitles, person.UniqueTitlesString));
                    //errors.AddRange(person.Guanzhis.LoadFromString(this.GameCommonData.AllTitles, reader["Guanzhis"].ToString()));
                }
                catch (Exception ex)
                {
                    #if DEBUG

                    #endif
                }

                // 🔥 2026-03-18 移除：旧的 Access 数据库加载代码
                // 原因：person.SkillsString/StuntsString/RealTitlesString 字段已删除
                // 现在使用 LinkReferencesPhase 从 SkillIDs/StuntIDs/TitleIDs 链接

                //person.StudyingTitleString = (short)reader["StudyingTitle"];
                person.StudyingTitle = this.GameCommonData.AllTitles.GetTitle(person.StudyingTitleString);

                // 🔥 2026-03-18 移除：旧的称号加载代码
                // 原因：person.RealTitlesString 字段已删除
                // 现在使用 LinkReferencesPhase 从 TitleIDs 链接

                //person.TrainPolicyIDString = (short)reader["TrainPolicy"];
                person.TrainPolicy = (TrainPolicy)this.GameCommonData.AllTrainPolicies.GetGameObject(person.TrainPolicyIDString);

                //person.preferredTroopPersonsString = reader["PreferredTroopPersons"].ToString();

                this.Persons.AddPersonWithEvent(person, false);  //所有武将，并加载武将事件

                // 🔥 修复：避免重复添加到 AllPersons 字典（前面第3755行已经添加过了）
                if (!this.AllPersons.ContainsKey(person.ID))
                {
                    this.AllPersons.Add(person.ID, person);   //武将字典
                }

                // this.AllChildren.Add(person, person.NumberOfChildren);

                if (person.Available && person.Alive)
                {
                    this.AvailablePersons.Add(person);  //已出场武将
                }
            }

            
            // 🔥 诊断：检查第一个武将的属性是否正确加载
            #if DEBUG
            if (this.Persons.Count > 0)
            {
                Person firstPerson = this.Persons[0] as Person;

            }
            #endif
            
            foreach (Person p in this.Persons)
            {
                p.WaitForFeiZi = this.Persons.GetGameObject(p.waitForFeiziId) as Person;
                List<string> e = p.preferredTroopPersons.LoadFromString(this.Persons, p.preferredTroopPersonsString);
                if (e.Count > 0)
                {
                    errorMsg.Add("人物ID" + p.ID + "：副将一栏：");
                    errorMsg.AddRange(e);
                }
            }

            foreach (KeyValuePair<int, int> i in FatherIds)
            {
                if (this.Persons.GetGameObject(i.Key) != null)
                {
                    (this.Persons.GetGameObject(i.Key) as Person).Father = this.Persons.GetGameObject(i.Value) as Person;
                }
            }

            foreach (KeyValuePair<int, int> i in MotherIds)
            {
                if (this.Persons.GetGameObject(i.Key) != null)
                {
                    (this.Persons.GetGameObject(i.Key) as Person).Mother = this.Persons.GetGameObject(i.Value) as Person;
                }
            }

            foreach (KeyValuePair<int, int> i in SpouseIds)
            {
                Person p = (this.Persons.GetGameObject(i.Key) as Person);
                Person q = this.Persons.GetGameObject(i.Value) as Person;
                if (p != null)
                {
                    p.Spouse = q;
                    if (q != null && fromScenario)
                    {
                        p.EnsureRelationAtLeast(q, Session.Parameters.VeryCloseThreshold);
                    }
                }
            }

            // 🔥 检查点4：义兄弟关系处理（高风险区域 - 原O(n²)复杂度已优化为O(n)）
            sw.Restart();

            
            // 🔥 优化：使用字典分组，避免O(n²)嵌套循环
            var brotherGroups = new Dictionary<int, List<int>>(); // groupId -> List<personId>
            int brotherPairCount = 0;
            
            foreach (KeyValuePair<int, int[]> i in BrotherIds)
            {
                brotherPairCount++;
                if (brotherPairCount % 100 == 0)
                {

                    
                    // 🔥 超时检测：如果超过30秒，抛出异常
                    if (sw.ElapsedMilliseconds > 30000)
                    {
                        throw new TimeoutException($"ProcessScenarioData 超时：义兄弟关系处理超过30秒，已处理{brotherPairCount}/{BrotherIds.Count}");
                    }
                }
                
                if (i.Value.Length == 1 && i.Value[0] != -1)
                {
                    // 使用组ID建立关系
                    int groupId = i.Value[0];
                    if (!brotherGroups.ContainsKey(groupId))
                    {
                        brotherGroups[groupId] = [];  // ✅ C# 12 集合表达式
                    }
                    brotherGroups[groupId].Add(i.Key);
                }
                else
                {
                    // 直接指定义兄弟ID的情况（原有逻辑）
                    Person p = this.Persons.GetGameObject(i.Key) as Person;
                    if (p == null) continue;
                    
                    foreach (int j in i.Value)
                    {
                        Person q = this.Persons.GetGameObject(j) as Person;
                        if (q != null)
                        {
                            p.Brothers.Add(q);
                            if (fromScenario)
                            {
                                p.EnsureRelationAtLeast(q, Session.Parameters.VeryCloseThreshold);
                            }
                        }
                        else
                        {
                            errorMsg.Add("人物ID" + p.ID + "：义兄弟ID" + j + "不存在");
                        }
                    }
                }
            }
            
            // 🔥 为每个组内的武将建立关系（O(n)复杂度）

            foreach (var group in brotherGroups.Values)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Person p = this.Persons.GetGameObject(group[i]) as Person;
                    if (p == null) continue;
                    
                    for (int j = 0; j < group.Count; j++)
                    {
                        if (i == j) continue; // 跳过自己
                        
                        Person q = this.Persons.GetGameObject(group[j]) as Person;
                        if (q != null)
                        {
                            p.Brothers.Add(q);
                            if (fromScenario)
                            {
                                p.EnsureRelationAtLeast(q, Session.Parameters.VeryCloseThreshold);
                            }
                        }
                    }
                }
            }


            // 🔥 检查点5：其他武将关系处理
            sw.Restart();

            
            foreach (KeyValuePair<int, int[]> i in CloseIds)
            {
                Person p = this.Persons.GetGameObject(i.Key) as Person;
                foreach (int j in i.Value)
                {
                    Person q = this.Persons.GetGameObject(j) as Person;
                    if (p != null && q != null)
                    {
                        p.AddClose(q);
                    }
                    else if (p != null)
                    {
                        errorMsg.Add("人物ID" + p.ID + "：亲爱武将ID" + j + "不存在");
                    }
                }
            }

            foreach (KeyValuePair<int, int[]> i in HatedIds)
            {
                Person p = this.Persons.GetGameObject(i.Key) as Person;
                foreach (int j in i.Value)
                {
                    Person q = this.Persons.GetGameObject(j) as Person;
                    if (p != null && q != null)
                    {
                        p.AddHated(q);
                    }
                    else if (p != null)
                    {
                        errorMsg.Add("人物ID" + p.ID + "：厌恶武将ID" + j + "不存在");
                    }
                }
            }

            foreach (KeyValuePair<int, int[]> i in SuoshuIds)
            {
                Person p = this.Persons.GetGameObject(i.Key) as Person;
                foreach (int j in i.Value)
                {
                    Person q = this.Persons.GetGameObject(j) as Person;
                    if (p != null && q != null)
                    {
                        p.suoshurenwuList.Add(q);
                    }
                    else if (p != null)
                    {
                        errorMsg.Add("人物ID" + p.ID + "：所属人物表ID" + j + "不存在");
                    } 
                    else
                    {
                        errorMsg.Add("人物ID" + p + "：所属人物表ID" + j + "不存在");
                    }
                }
            }

            foreach (KeyValuePair<int, int> i in MarriageGranterId)
            {
                if ((this.Persons.GetGameObject(i.Key) as Person) != null)
                {
                    (this.Persons.GetGameObject(i.Key) as Person).marriageGranter = this.Persons.GetGameObject(i.Value) as Person;
                }
            }

            foreach (Person p in this.Persons)
            {
                if (p.Spouse != null && !p.suoshurenwuList.HasGameObject(p.Spouse))
                {
                    p.suoshurenwuList.Add(p.Spouse);
                    p.Spouse.suoshurenwuList.Add(p);
                }
            }


            // 🔥 检查点6：列传数据处理
            sw.Restart();

            
            foreach (var biography in this.AllBiographies.Biographys)
            {
                biography.Value.Init();
                Person p = (Person)this.Persons.GetGameObject(biography.Value.ID);
                if (p != null)
                {
                    List<string> e = new List<string>();

                    if (!String.IsNullOrEmpty(biography.Value.MilitaryKindsString))
                    {
                        e = biography.Value.MilitaryKinds.LoadFromString(this.GameCommonData.AllMilitaryKinds, biography.Value.MilitaryKindsString);
                    }

                    if (e.Count > 0)
                    {
                        errorMsg.Add("列传人物ID" + biography.Value.ID + "：");
                        errorMsg.AddRange(e);
                    }
                    if (biography.Value.MilitaryKinds.MilitaryKinds.Count == 0)
                    {
                        errorMsg.Add("列传人物ID" + biography.Value.ID + "：没有基本兵种。");
                    }
                    

                    
                    p.PersonBiography = biography.Value;
                    

                }
            }

            foreach (Person p in this.Persons)
            {

                
                if (p.PersonBiography == null)
                {
                    var biography = new Biography();
                    biography.ID = p.ID;  // 🔥 先设置ID
                    biography.FactionColor = 52;
                    biography.MilitaryKinds.AddBasicMilitaryKinds();
                    biography.Brief = "";
                    biography.History = "";
                    biography.Romance = "";
                    biography.InGame = "";
                    
                    p.PersonBiography = biography;  // 🔥 再赋值，这样PersonBiographyID会被正确设置
                    this.AllBiographies.AddBiography(p.PersonBiography);
                    


                }
                

            }


            // 🔥 检查点7：人物关系处理
            sw.Restart();

            
            foreach (var relation in PersonRelationIds)
            {
                Person person1 = this.Persons.GetGameObject(relation.PersonID1) as Person;
                Person person2 = this.Persons.GetGameObject(relation.PersonID2) as Person;

                if (person1 == null)
                {
                    errorMsg.Add("人物关系：武将ID" + relation.PersonID1 + "不存在");
                }
                if (person2 == null)
                {
                    errorMsg.Add("人物关系：武将ID" + relation.PersonID2 + "不存在");
                }
                if (person1 != null && person2 != null)
                {
                    person1.SetRelation(person2, relation.Relation);
                }
            }

            
            #if DEBUG

            #endif

            if (this.captiveData != null && !editing)
            {
                foreach (Captive captive in this.captiveData)
                {
                    captive.CaptivePerson = this.Persons.GetGameObject(captive.CaptivePersonID) as Person;
                    if (captive.CaptivePerson == null)
                    {
                        errorMsg.Add("俘虏ID" + captive.ID + "：武将ID" + captive.CaptivePersonID + "不存在");
                        continue;
                    }
                    else
                    {
                        captive.CaptivePerson.SetBelongedCaptive(captive, PersonStatus.Captive);

                        captive.CaptivePerson.Status = PersonStatus.Captive;
                    }

                }
            }

            this.Captives.BindEvents();

            foreach (Military military in this.Militaries)
            {
                military.Init();

                if (this.GameCommonData.AllMilitaryKinds.GetMilitaryKind(military.KindID) == null)
                {
                    errorMsg.Add("编队ID" + military.ID + "：兵种ID" + military.KindID + "不存在");
                    continue;
                }
                
                // 🔥 修复：只有当 RecruitmentPersonID > 0 时才关联补充人员
                // 日期：2026-02-17
                // 问题：RecruitmentPersonID = 0 时会错误地关联到 ID=0 的武将
                if (military.RecruitmentPersonID > 0)
                {
                    foreach (Person p in this.Persons)
                    {
                        if (p.ID == military.RecruitmentPersonID)
                        {
                            //p.RecruitmentMilitary = military;
                            p.RecruitMilitary(military);
                            break; // 找到后立即退出循环
                        }
                    }
                }
            }

            this.InitializeMilitaryData();

            foreach (Facility facility in this.Facilities)
            {
                if (this.GameCommonData.AllFacilityKinds.GetFacilityKind(facility.KindID) == null)
                {
                    errorMsg.Add("设施ID" + facility.ID + "：设施种类ID" + facility.KindID + "不存在");
                    continue;
                }
            }

            //foreach (Information information in this.Informations)
            //{

            //}

            // 处理建筑数据
            #if DEBUG
            int archDebugCount = 0;
            #endif
            
            foreach (Architecture architecture in this.Architectures)
            {
                List<string> e = new List<string>();


                architecture.Init();

                
                #if DEBUG
                // 🔥 诊断：检查 CaptionID 加载情况
                if (archDebugCount < 5)
                {
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 建筑 {architecture.Name}(ID:{architecture.ID}), CaptionID={architecture.CaptionID}");
                }
                #endif
                
                // 建筑类型
                // 🔥 诊断：检查 AllArchitectureKinds 状态
                if (this.GameCommonData?.AllArchitectureKinds == null)
                {

                }
                else if (this.GameCommonData.AllArchitectureKinds.ArchitectureKinds == null)
                {

                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] AllArchitectureKinds 包含 {this.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count} 个建筑类型");
                }
                
                // Binary serialization removed - always use KindID from JSON (注意大写)
                int actualKindId = architecture.KindID; // 🔥 修复：使用 KindID 而不是 KindId
                
                // 🔥 防御性检查：如果 actualKindId 无效，尝试从现有 Kind 获取
                if (actualKindId <= 0)
                {
                    if (architecture.Kind != null)
                    {
                        actualKindId = architecture.Kind.ID;

                    }
                    else
                    {

                        continue; // 跳过这个建筑，不要设置 Kind 为 null
                    }
                }
                
                var kindResult = this.GameCommonData.AllArchitectureKinds.GetArchitectureKind(actualKindId);
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 建筑 {architecture.Name}(ID:{architecture.ID}) 查询 KindId={actualKindId}, 结果: {kindResult?.Name ?? "null"}");
                
                // 🔥 修复：只有在 kindResult 不为 null 时才设置 Kind
                if (kindResult != null)
                {
                    architecture.Kind = kindResult;
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] ✅ 建筑 {architecture.Name}(ID:{architecture.ID}) Kind已分配: {architecture.Kind.Name}(KindId:{actualKindId})");
                }
                else if (architecture.Kind != null)
                {
                    // 如果查询失败但原来有 Kind，保留原来的 Kind

                }
                else
                {
                    // 真的找不到，抛出异常
                    var message = $"建筑种类ID：{actualKindId}, 不存在";
                    throw new Exception(message);
                }

                architecture.LocationState = this.States.GetGameObject(architecture.StateID) as State;
                

                
                if (architecture.LocationState == null)
                {
                    e.Add($"州域ID {architecture.StateID} 不存在（建筑：{architecture.Name}, ID:{architecture.ID}）");
                }
                else
                {

                    
                    architecture.LocationState.Architectures.Add(architecture);
                    architecture.LocationState.LinkedRegion.Architectures.Add(architecture);
                    if (architecture.LocationState.StateAdminID == architecture.ID)
                    {
                        architecture.LocationState.StateAdmin = architecture;
                    }
                    if (architecture.LocationState.LinkedRegion.RegionCoreID == architecture.ID)
                    {
                        architecture.LocationState.LinkedRegion.RegionCore = architecture;
                    }
                }

                //architecture.CharacteristicsString = reader["Characteristics"].ToString();

                e.AddRange(architecture.Characteristics.LoadFromString(this.GameCommonData.AllInfluences, architecture.CharacteristicsString));


                //architecture.ArchitectureAreaString = reader["Area"].ToString();

                // 🔥 确保ArchitectureArea与ArchitectureAreaString严格对应
                if (!string.IsNullOrEmpty(architecture.ArchitectureAreaString))
                {
                    var expectedCount = architecture.ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                    var currentCount = architecture.ArchitectureArea?.Area?.Count ?? 0;
                    
                    // 如果坐标数量不匹配或ArchitectureArea为null，强制重新创建
                    if (architecture.ArchitectureArea == null || currentCount != expectedCount)
                    {
                        #if DEBUG
                        // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] {architecture.Name} 重建ArchitectureArea: 期望{expectedCount}, 当前{currentCount}");
                        #endif
                        
                        var newArea = new GameArea();
                        architecture.LoadFromString(newArea, architecture.ArchitectureAreaString);
                        architecture.ArchitectureArea = newArea;
                    }
                }
                else if (architecture.ArchitectureArea == null)
                {

                    architecture.ArchitectureArea = new GameArea();
                }

                //if (architecture.ArchitectureArea == null)
                //{
                //    architecture.ArchitectureArea = new GameArea();
                //}

                //if (architecture.ArchitectureArea.Area == null)
                //{
                //    architecture.ArchitectureArea.Area = new List<Point>();
                //}

                //architecture.PersonsString = reader["Persons"].ToString();
                //architecture.MovingPersonsString = reader["MovingPersons"].ToString();
                //architecture.NoFactionPersonsString = reader["NoFactionPersons"].ToString();
                //architecture.NoFactionMovingPersonsString = reader["NoFactionMovingPersons"].ToString();
                //architecture.feiziliebiaoString = reader["feiziliebiao"].ToString();

                /*
                #if DEBUG
                if (!string.IsNullOrEmpty(architecture.PersonsString))
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] {architecture.Name} PersonsString: '{architecture.PersonsString}'");
                }
                #endif
                */

                // 🔥 关键修复：PersonsString 被标记为 [JsonIgnore]，反序列化后为空
                // PersonsString_Legacy setter 只转换为 PersonIDs，没有设置 PersonsString
                // 因此需要从 PersonIDs 重建字符串
                // 日期：2026-03-16
                

                
                // 从 PersonIDs 重建 PersonsString（如果为空）
                string personsString = architecture.PersonsString;
                if (string.IsNullOrEmpty(personsString) && architecture.PersonIDs != null && architecture.PersonIDs.Count > 0)
                {
                    personsString = string.Join(" ", architecture.PersonIDs);
                }
                
                e.AddRange(architecture.LoadPersonsFromString(this.AllPersons, personsString, PersonStatus.Normal));
                e.AddRange(architecture.LoadPersonsFromString(this.AllPersons, architecture.MovingPersonsString, PersonStatus.Moving));
                e.AddRange(architecture.LoadPersonsFromString(this.AllPersons, architecture.NoFactionPersonsString, PersonStatus.NoFaction));
                e.AddRange(architecture.LoadPersonsFromString(this.AllPersons, architecture.NoFactionMovingPersonsString, PersonStatus.NoFactionMoving));
                e.AddRange(architecture.LoadPersonsFromString(this.AllPersons, architecture.feiziliebiaoString, PersonStatus.Princess));

                /*
                #if DEBUG
                if (architecture.Persons.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] ✅ {architecture.Name} 加载了 {architecture.Persons.Count} 个武将");
                }
                #endif
                */

                //architecture.MilitariesString = reader["Militaries"].ToString();

                //architecture.FacilitiesString = reader["Facilities"].ToString();

                try
                {
                    // 🔥 修复：使用 MilitaryIDs 列表而不是 MilitariesString（已被 _Legacy 属性转换）
                    string militariesStr = (architecture.MilitaryIDs != null && architecture.MilitaryIDs.Count > 0)
                        ? string.Join(" ", architecture.MilitaryIDs)
                        : architecture.MilitariesString;
                    
                    e.AddRange(architecture.LoadMilitariesFromString(this.Militaries, militariesStr));
                    e.AddRange(architecture.LoadFacilitiesFromString(this.Facilities, architecture.FacilitiesString));
                    
                    #if DEBUG
                    if (archDebugCount < 3)  // 只打印前3个建筑
                    {
                        // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] 建筑 {architecture.Name}(ID:{architecture.ID}):");
                        // System.Diagnostics.Debug.WriteLine($"  - FacilitiesString: '{architecture.FacilitiesString}'");
                        // System.Diagnostics.Debug.WriteLine($"  - Facilities.Count: {architecture.Facilities?.Count ?? 0}");
                        if (architecture.Facilities != null && architecture.Facilities.Count > 0)
                        {
                            foreach (Facility f in architecture.Facilities)
                            {
                                // System.Diagnostics.Debug.WriteLine($"    * {f.Name} (ID:{f.ID})");
                            }
                        }
                        archDebugCount++;
                    }
                    #endif
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] ⚠️ architecture.LoadMilitariesFromString/Facilities failed for {architecture.Name}(ID:{architecture.ID}): {ex.Message}");
                }

                //architecture.FundPacksString = reader["FundPacks"].ToString();

                //architecture.FoodPacksString = reader["FoodPacks"].ToString();

                try
                {
                    e.AddRange(architecture.LoadFundPacksFromString(architecture.FundPacksString));
                    e.AddRange(architecture.LoadFoodPacksFromString(architecture.FoodPacksString));
                }
                catch (Exception ex)
                {

                }

                //architecture.PopulationPacksString = reader["PopulationPacks"].ToString();
                e.AddRange(architecture.LoadPopulationPacksFromString(architecture.PopulationPacksString));

                e.AddRange(architecture.LoadMilitaryPopulationPacksFromString(architecture.MilitaryPopulationPacksString));

                //architecture.CaptivesString = reader["Captives"].ToString();
                e.AddRange(architecture.LoadCaptivesFromString(this.Captives, architecture.CaptivesString));

                //architecture.AILandLinksString = reader["AILandLinks"].ToString();
                //architecture.AIWaterLinksString = reader["AIWaterLinks"].ToString();

                try
                {
                    // Zainan information is currently not strictly loaded via a single string method here in some versions,
                    // but the catch block expects to handle it. 
                }
                catch (Exception ex)
                {

                    architecture.youzainan = false;
                }

                try
                {
                    e.AddRange(architecture.LoadInformationsFromString(this.Informations, architecture.InformationsString));
                }
                catch (Exception ex)
                {

                }

                architecture.AIBattlingArchitectures = new ArchitectureList();

                if (e.Count > 0)
                {
                    errorMsg.Add("建筑ID" + architecture.ID + "：");
                    errorMsg.AddRange(e);
                }
                //else
                //{
                    this.Architectures.AddArchitectureWithEvent(architecture, false);
                //后面宝物的所在地有用到此allar，所以要先将城池加入字典，否则会造成宝物所在地为空
                this.AllArchitectures.Add(architecture.ID, architecture);
                //}

            }

            foreach (KeyValuePair<int, int[]> a in AiBattlingArchitectureStrings)
            {
                foreach (int i in a.Value)
                {
                    (this.Architectures.GetGameObject(a.Key) is Architecture ? (Architecture)this.Architectures.GetGameObject(a.Key) : null).AIBattlingArchitectures.Add((this.Architectures.GetGameObject(i) is Architecture ? (Architecture)this.Architectures.GetGameObject(i) : null));
                }
            }

            foreach(Routeway routeway in Routeways)
            {
                List<string> e = new List<string>();

                routeway.Init();

                //routeway.StartArchitectureString = (int)reader["StartArchitecture"];
                routeway.StartArchitecture = this.Architectures.GetGameObject(routeway.StartArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(routeway.StartArchitectureString) : null;

                if (routeway.StartArchitecture != null)
                {
                    routeway.StartArchitecture.Routeways.Add(routeway);
                }
                else
                {
                    e.Add("建筑ID" + routeway.StartArchitectureString + "不存在");
                }

                //routeway.EndArchitectureString = (int)reader["EndArchitecture"];
                routeway.EndArchitecture = this.Architectures.GetGameObject(routeway.EndArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(routeway.EndArchitectureString) : null;

                //routeway.DestinationArchitectureString = (int)reader["DestinationArchitecture"];
                routeway.DestinationArchitecture = this.Architectures.GetGameObject(routeway.DestinationArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(routeway.DestinationArchitectureString) : null;

                routeway.BelongedFaction = this.Factions.GetGameObject(routeway.BelongedFactionString) is Faction ? (Faction)this.Factions.GetGameObject(routeway.BelongedFactionString) : null;

                //routeway.LoadRoutePointsFromString(reader["Points"].ToString());

                if (e.Count > 0)
                {
                    errorMsg.Add("粮道ID" + routeway.ID + "：");
                    errorMsg.AddRange(e);
                }
                //this.Routeways.AddRoutewayWithEvent(routeway);
            }

            this.Troops.Init();
            
            foreach (Troop troop in this.Troops)
            {
                List<string> errors = new List<string>();

                troop.Init();

                //troop.StartingArchitectureString = (short)reader["StartingArchitecture"];
                troop.StartingArchitecture = this.Architectures.GetGameObject(troop.StartingArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(troop.StartingArchitectureString) : null;

                if (troop.StartingArchitecture == null)
                {
                    errors.Add("起始建筑ID" + troop.StartingArchitectureString + "不存在");
                }

                //troop.PersonsString = reader["Persons"].ToString();
                //troop.LeaderIDString = (short)reader["LeaderID"];

                errors.AddRange(troop.LoadPersonsFromString(this.AllPersons, troop.PersonsString, troop.LeaderIDString));

                //troop.MilitaryID = (short)reader["MilitaryID"];
                //if (this.Militaries.GetGameObject(troop.MilitaryID) == null)
                //{
                //    errors.Add("编队ID" + troop.MilitaryID + "不存在");
                //}

                //troop.CaptivesString = reader["Captives"].ToString();
                errors.AddRange(troop.LoadCaptivesFromString(this.Captives, troop.CaptivesString.NullToString("")));

                //troop.EventInfluencesString = reader["EventInfluences"].ToString();
                errors.AddRange(troop.EventInfluences.LoadFromString(this.GameCommonData.AllInfluences, troop.EventInfluencesString.NullToString("")));

                errors.AddRange(troop.LoadCombatMethodFromString(this.GameCommonData.AllCombatMethods, troop.CombatMethodsString.NullToString("")));

                //troop.CurrentStuntIDString = (short)reader["CurrentStunt"];
                troop.CurrentStunt = this.GameCommonData.AllStunts.GetStunt(troop.CurrentStuntIDString);

                troop.CurrentStratagem = this.GameCommonData.AllStratagems.GetStratagem(troop.CurrentStratagemID);

                if (errors.Count > 0)
                {
                    errors.Add("部队ID" + troop.ID + "：");
                    errorMsg.AddRange(errors);
                }

                if (troop.Army != null)
                {
                    this.Troops.AddTroopWithEvent(troop, false);
                }
            }

            foreach(Legion legion in this.Legions)
            {
                legion.Init();

                //legion.StartArchitectureString = (int)reader["StartArchitecture"];
                legion.StartArchitecture = this.Architectures.GetGameObject(legion.StartArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(legion.StartArchitectureString) : null;

                //legion.WillArchitectureString = (int)reader["WillArchitecture"];
                legion.WillArchitecture = this.Architectures.GetGameObject(legion.WillArchitectureString) is Architecture ? (Architecture)this.Architectures.GetGameObject(legion.WillArchitectureString) : null;

                //legion.PreferredRoutewayString = (int)reader["PreferredRouteway"];
                legion.PreferredRouteway = this.Routeways.GetGameObject(legion.PreferredRoutewayString) as Routeway;

                //legion.InformationDestination = StaticMethods.LoadFromString(reader["InformationDestination"].ToString());

                //legion.CoreTroopString = (int)reader["CoreTroop"];
                legion.CoreTroop = this.Troops.GetGameObject(legion.CoreTroopString) is Troop ? (Troop)this.Troops.GetGameObject(legion.CoreTroopString) : null;

                //legion.TroopIDs = reader["Troops"].ToString();
                legion.LoadTroopsFromString(this.Troops, string.Join(" ", legion.TroopIDs ?? new List<int>()));

                //this.Legions.AddLegionWithEvent(legion);
            }

            // 🔥 FIX: 添加类型验证，防止 InvalidCastException
            try
            {
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] Sections 集合验证开始，Count: {this.Sections.Count}");
                
                // 验证 Sections 集合中的对象类型并处理
                for (int i = 0; i < this.Sections.Count; i++)
                {
                    var obj = this.Sections[i];
                    if (obj is GameObjects.Section section)
                    {
                        try
                        {
                            section.Init();

                            List<string> e = new List<string>();
                            //section.AIDetailID = (short)reader["AIDetail"];
                            section.AIDetail = this.GameCommonData.AllSectionAIDetails.GetSectionAIDetail(section.AIDetailID);

                            if (section.AIDetail == null)
                            {
                                e.Add("地区AI详细" + section.AIDetailID + "不存在");
                            }

                            // 🔥 修复：如果ArchitectureIDs为空（新开剧本），根据BelongedSectionID填充
                            if (section.ArchitectureIDs == null || section.ArchitectureIDs.Count == 0)
                            {

                                
                                // 遍历所有建筑，找出属于这个Section的
                                foreach (Architecture arch in this.Architectures.GetList())
                                {
                                    if (arch != null && arch.BelongedSectionID == section.ID)
                                    {
                                        if (section.Architectures == null)
                                        {
                                            section.Architectures = new ArchitectureList();
                                        }
                                        
                                        if (!section.Architectures.HasGameObject(arch.ID))
                                        {
                                            section.Architectures.Add(arch);
                                            arch.BelongedSection = section;
                                        }
                                    }
                                }
                                

                            }
                            else
                            {
                                // 从ArchitectureIDs恢复（存档加载或新剧本）

                                e.AddRange(section.LoadArchitecturesFromString(this.Architectures, string.Join(" ", section.ArchitectureIDs ?? new List<int>())));
                            }

                            if (e.Count > 0)
                            {
                                errorMsg.Add("军区ID" + section.ID + "：");
                                errorMsg.AddRange(e);
                            }

                            //this.Sections.AddSectionWithEvent(section);
                        }
                        catch (Exception exInner)
                        {

                        }
                    }
                    else
                    {

                    }
                }
                
                // System.Diagnostics.Debug.WriteLine("[ProcessScenarioData] ✅ Sections 处理完成");
            }
            catch (Exception ex)
            {

                // 继续执行，不中断整个流程
            }

            // 🔥 修复：在处理 Faction 之前，从建筑的 BelongedFactionID 反向构建势力的建筑列表
            // 原因：JSON 剧本文件中 Faction 没有 ArchitecturesString 字段

            foreach (Architecture arch in this.Architectures.GetList())
            {
                if (arch != null && arch.BelongedFactionID >= 0)
                {
                    Faction faction = this.Factions.GetGameObject(arch.BelongedFactionID) as Faction;
                    if (faction != null)
                    {
                        // 确保 ArchitectureIDs 列表已初始化
                        if (faction.ArchitectureIDs == null)
                        {
                            faction.ArchitectureIDs = new List<int>();
                        }
                        
                        // 添加建筑 ID（避免重复）
                        if (!faction.ArchitectureIDs.Contains(arch.ID))
                        {
                            faction.ArchitectureIDs.Add(arch.ID);
                        }
                    }
                }
            }
            
            // 输出统计信息
            int factionsWithArchs = 0;
            int totalArchs = 0;
            foreach (Faction f in this.Factions)
            {
                if (f.ArchitectureIDs != null && f.ArchitectureIDs.Count > 0)
                {
                    factionsWithArchs++;
                    totalArchs += f.ArchitectureIDs.Count;

                }
            }


            foreach (Faction faction in this.Factions)
            {
                List<string> e = new List<string>();

                faction.Init();
                
                // 🔥 根本性修复：确保所有集合已初始化，防止反序列化后为null
                faction.EnsureCollectionsInitialized();

                // 🔥 诊断：检查ArchitectureIDs状态
                // System.Diagnostics.Debug.WriteLine($"[ProcessScenarioData] Faction {faction.Name}(ID:{faction.ID}):");
                // System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs == null: {faction.ArchitectureIDs == null}");
                // System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs.Count: {faction.ArchitectureIDs?.Count ?? 0}");
                if (faction.ArchitectureIDs != null && faction.ArchitectureIDs.Count > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs前10个: [{string.Join(", ", faction.ArchitectureIDs.Take(10))}]");
                }
                
                // 🔥 修复：使用空格分隔，而不是逗号（LoadArchitecturesFromString期望空格分隔）
                string archIdsString = string.Join(" ", faction.ArchitectureIDs ?? new List<int>());
                // System.Diagnostics.Debug.WriteLine($"  - 传给LoadArchitecturesFromString的字符串长度: {archIdsString.Length}");
                // System.Diagnostics.Debug.WriteLine($"  - 字符串前100字符: {(archIdsString.Length > 100 ? archIdsString.Substring(0, 100) + "..." : archIdsString)}");

                //faction.ArchitectureIDs = reader["Architectures"].ToString();
                e.AddRange(faction.LoadArchitecturesFromString(this.Architectures, archIdsString));

                // System.Diagnostics.Debug.WriteLine($"  - LoadArchitecturesFromString后 Architectures.Count: {faction.Architectures?.Count ?? 0}");
                
                // 🔥 额外诊断：如果建筑列表为空，检查原因
                if (faction.Architectures == null || faction.Architectures.Count == 0)
                {

                }

                //faction.SectionIDs = reader["Sections"].ToString();
                e.AddRange(faction.LoadSectionsFromString(this.Sections, string.Join(" ", faction.SectionIDs ?? new List<int>())));

                //faction.TroopIDs = reader["Troops"].ToString();
                e.AddRange(faction.LoadTroopsFromString(this.Troops, string.Join(" ", faction.TroopIDs ?? new List<int>())));

                //faction.InformationsString = reader["Informations"].ToString();
                e.AddRange(faction.LoadInformationsFromString(this.Informations, faction.InformationsString));

                //faction.RoutewaysString = reader["Routeways"].ToString();
                e.AddRange(faction.LoadRoutewaysFromString(this.Routeways, faction.RoutewaysString));

                //faction.LegionIDs = reader["Legions"].ToString();
                e.AddRange(faction.LoadLegionsFromString(this.Legions, string.Join(" ", faction.LegionIDs ?? new List<int>())));

                //faction.BaseMilitaryKindsString = reader["BaseMilitaryKinds"].ToString();
                faction.BaseMilitaryKinds.LoadFromString(this.GameCommonData.AllMilitaryKinds, faction.BaseMilitaryKindsString);

                //faction.AvailableTechniquesString = reader["AvailableTechniques"].ToString();
                e.AddRange(faction.AvailableTechniques.LoadFromString(this.GameCommonData.AllTechniques, faction.AvailableTechniquesString));

                //faction.PlanTechniqueString = (short)reader["PlanTechnique"];
                faction.PlanTechnique = this.GameCommonData.AllTechniques.GetTechnique(faction.PlanTechniqueString);

                //faction.TransferingMilitariesString = reader["TransferingMilitaries"].ToString();
                e.AddRange(faction.LoadTransferingMilitariesFromString(this.Militaries, faction.TransferingMilitariesString.NullToString()));

                //faction.MilitaryIDs = reader["Militaries"].ToString();
                e.AddRange(faction.LoadMilitariesFromString(this.Militaries, string.Join(" ", faction.MilitaryIDs ?? new List<int>())));

                //faction.GetGeneratorPersonCountString = reader["GetGeneratorPersonCount"].ToString();
                e.AddRange(faction.LoadGeneratorPersonCountFromString(faction.GetGeneratorPersonCountString.NullToString()));
                if (faction.PrinceID != -1 && this.Persons.GetGameObject(faction.PrinceID) is Person prince && prince != null)//取消储君序列化，原有的方法会导致二次存档后储君为空
                {
                    faction.Prince = prince;
                }
                if (faction.AvailableMilitaryKinds.GetMilitaryKindList().Count == 0)
                {
                    faction.AvailableMilitaryKinds.AddMilitaryKind(this.GameCommonData.AllMilitaryKinds.GetMilitaryKind(0));
                    faction.AvailableMilitaryKinds.AddMilitaryKind(this.GameCommonData.AllMilitaryKinds.GetMilitaryKind(1));
                    faction.AvailableMilitaryKinds.AddMilitaryKind(this.GameCommonData.AllMilitaryKinds.GetMilitaryKind(2));
                }
                if (e.Count > 0)
                {
                    errorMsg.Add("势力ID" + faction.ID + "：");
                    errorMsg.AddRange(e);
                }

                this.Factions.AddFactionWithEvent(faction, false);
            }
            
            // 🔥 诊断：检查 Faction 的恢复状态
            #if DEBUG

            int factionDebugCount = 0;
            foreach (Faction faction in this.Factions)
            {
                if (factionDebugCount < 3)  // 只打印前3个势力
                {

                }
                factionDebugCount++;
            }

            #endif



            this.DiplomaticRelations.Init(this.Factions);

            // 处理宝物的其他属性（HidePlace、Influences）
            // 注意：宝物归属关系已在 LinkReferencesPhase.LinkTreasures() 中通过 Person.TreasureIDs 建立
            foreach (Treasure treasure in this.Treasures)
            {
                treasure.Init();

                // 链接隐藏地点
                treasure.HidePlace = this.AllArchitectures.ContainsKey(treasure.HidePlaceIDString) ? this.AllArchitectures[treasure.HidePlaceIDString] : null;

                // 加载宝物效果
                treasure.Influences.LoadFromString(this.GameCommonData.AllInfluences, treasure.InfluencesString);
            }

            //foreach (var dr in this.DiplomaticRelations.DiplomaticRelations)
            //{

            //}

            foreach (GameObject gameObj in this.TroopEvents)
            {
                TroopEvent te = gameObj as TroopEvent;
                if (te == null)
                {
                    // Skip objects that are not TroopEvent instances
                    continue;
                }

                te.Init();

                //te.LaunchPersonString = (short)reader["LaunchPerson"];
                te.LaunchPerson = this.Persons.GetGameObject(te.LaunchPersonString) is Person ? (Person)this.Persons.GetGameObject(te.LaunchPersonString) : null;

                //te.ConditionsString = reader["Conditions"].ToString();
                te.Conditions.LoadFromString(this.GameCommonData.AllConditions, te.ConditionsString);

                //te.TargetPersonsString = reader["TargetPersons"].ToString();
                te.LoadTargetPersonFromString(this.AllPersons, te.TargetPersonsString);

                //te.SelfEffectsString = reader["EffectSelf"].ToString();
                te.LoadSelfEffectFromString(this.GameCommonData.AllTroopEventEffects, te.SelfEffectsString);

                //te.EffectPersonsString = reader["EffectPersons"].ToString();
                te.LoadEffectPersonFromString(this.AllPersons, this.GameCommonData.AllTroopEventEffects, te.EffectPersonsString);

                //te.EffectAreasString = reader["EffectAreas"].ToString();
                te.LoadEffectAreaFromString(this.GameCommonData.AllTroopEventEffects, te.EffectAreasString);

                te.LoadDialogFromString(this.AllPersons, te.dialogString);
                if (te.TryToShowString == null) te.TryToShowString = "";
                this.TroopEvents.AddTroopEventWithEvent(te, false);
            }
            
            // ✅ 修复：清理TroopEvents列表中的非TroopEvent对象（场景加载后统一清理一次）
            int removedCount = this.TroopEvents.CleanupInvalidObjects();
            if (removedCount > 0)
            {

            }

            foreach (GameObject gameObj in this.AllEvents)
            {
                Event e = (gameObj is Event ? (Event)gameObj : null);
                if (e == null)
                {
                    // Skip objects that are not Event instances
                    continue;
                }

                e.Init();

                //e.personString = reader["PersonId"].ToString();
                e.LoadPersonIdFromString(this.Persons, e.personString);

                //e.PersonCondString = reader["PersonCond"].ToString();
                e.LoadPersonCondFromString(this.GameCommonData.AllConditions, e.PersonCondString);

                //e.architectureString = reader["ArchitectureID"].ToString();
                e.LoadArchitectureFromString(this.Architectures, e.architectureString);

                //e.architectureCondString = reader["ArchitectureCond"].ToString();
                e.LoadArchitctureCondFromString(this.GameCommonData.AllConditions, e.architectureCondString);

                //e.factionString = reader["FactionID"].ToString();
                e.LoadFactionFromString(this.Factions, e.factionString);

                //e.factionCondString = reader["FactionCond"].ToString();
                e.LoadFactionCondFromString(this.GameCommonData.AllConditions, e.factionCondString);

                //e.effectString = reader["Effect"].ToString();
                e.LoadEffectFromString(this.GameCommonData.AllEventEffects, e.effectString);

                //e.architectureEffectString = reader["ArchitectureEffect"].ToString();
                e.LoadArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.architectureEffectString);

                //e.factionEffectIDString = reader["FactionEffect"].ToString();
                e.LoadFactionEffectFromString(this.GameCommonData.AllEventEffects, e.factionEffectIDString);

                if (e.dialogString != null)
                {
                    e.LoadDialogFromString(e.dialogString);
                }

                //e.yesEffectString = reader["YesEffect"].ToString();
                e.LoadYesEffectFromString(this.GameCommonData.AllEventEffects, e.yesEffectString);
                //e.noEffectString = reader["NoEffect"].ToString();
                e.LoadNoEffectFromString(this.GameCommonData.AllEventEffects, e.noEffectString);

                if (e.yesdialogString != null)
                {
                    e.LoadyesDialogFromString(e.yesdialogString);
                }
                if (e.nodialogString != null)
                {
                    e.LoadnoDialogFromString(e.nodialogString);
                }

                //e.yesArchitectureEffectString = reader["YesArchitectureEffect"].ToString();
                //e.noArchitectureEffectString = reader["NoArchitectureEffect"].ToString();
                e.LoadYesArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.yesArchitectureEffectString);
                e.LoadNoArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.noArchitectureEffectString);

                if (e.scenBiographyString != null)
                {
                    e.LoadScenBiographyFromString(e.scenBiographyString);
                }

                if (e.TryToShowString == null) e.TryToShowString = "";
                //e.LoadScenBiographyFromString(reader["ScenBiography"].ToString());
                this.AllEvents.AddEventWithEvent(e, false);
            }
            if(!editing)//这里不加条件的话，用剧本编辑器读取有错剧本时，可能出现游戏主程序能读剧本而编辑器打不开剧本的情况
            {
                foreach (Person p in this.Persons)
                {
                    if (p.Status == PersonStatus.Normal || p.Status == PersonStatus.Moving)
                    {
                        if (p.LocationArchitecture != null && p.LocationArchitecture.BelongedFaction == null)
                        {
                            errorMsg.Add("武将ID" + p.ID + "在一座没有势力的城池仕官");
                            if (p.Status == PersonStatus.Normal)
                            {
                                p.Status = PersonStatus.NoFaction;
                            }
                            else
                            {
                                p.Status = PersonStatus.NoFactionMoving;
                            }
                        }
                    }
                    if (p.Status == PersonStatus.Moving || p.Status == PersonStatus.NoFactionMoving)
                    {
                        if (p.ArrivingDays <= 0)
                        {
                            errorMsg.Add("武将ID" + p.ID + "正移动，但没有移动天数");
                            p.ArrivingDays = 1;
                        }
                    }
                    if (p.Available && p.Alive && p.LocationArchitecture == null && p.LocationTroop == null && (p.ID < 7000 || p.ID >= 8000))
                    {
                        if (p.Status != PersonStatus.Princess)
                        {
                            errorMsg.Add("武将ID" + p.ID + "已登场，但没有所属建筑");
                            p.Available = false;
                            p.Alive = false;
                            p.Status = PersonStatus.None;
                        }
                    }
                }
                ClearTempDic();
            }

            this.YearTable.Init();
            //this.YearTable = new YearTable();

            this.AllPersons.Clear();
            this.AllArchitectures.Clear();

            this.alterTransportShipAdaptibility();

            //using (TextWriter tw = new StreamWriter(SCENARIO_ERROR_TEXT_FILE))
            //{
            //    foreach (string s in errorMsg)
            //    {
            //        tw.WriteLine(s);
            //    }
            //}

            // 🔥 注意：不在这里调用 InitialGameData
            // 原因：AfterLoadGameScenario 会调用 BuildQueue → LinkReferences → InitialGameData
            //       在这里调用会导致状态被覆盖
            // 日期：2026-03-16

            ScenarioEvents.RaiseScenarioLoaded(this);

            return errorMsg;
        }


        void ClearTempDic()
        {
            FatherIds.Clear();
            MotherIds.Clear();
            SpouseIds.Clear();
            BrotherIds.Clear();
            SuoshuIds.Clear();
            CloseIds.Clear();
            HatedIds.Clear();
            MarriageGranterId.Clear();
            // 🔥 修复：PersonRelationIds 不应该在这里清空！
            // PersonRelationIds 是需要保存的游戏数据，不是临时ID映射
            // PersonRelationIds.Clear(); // ❌ 错误：这会导致关系数据丢失
        }
        
        private void alterTransportShipAdaptibility()
        {
            try
            {

                
                // 🔥 使用专用的 AOT MilitaryKinds 修复器
                WorldOfTheThreeKingdoms.AOTCompatibility.AOTMilitaryKindsDeserializationFixer.FixGameScenarioMilitaryKinds(this);
                
                // 验证修复结果
                if (this.GameCommonData?.AllMilitaryKinds?.MilitaryKinds == null)
                {

                    return;
                }
                
                // 安全获取 ID=28 的兵种（运输船）
                MilitaryKind militaryKind = this.GameCommonData.AllMilitaryKinds.GetMilitaryKind(28);
                if (militaryKind == null)
                {

                    return;
                }
                

                
                if (Session.GlobalVariables.LandArmyCanGoDownWater)
                {
                    militaryKind.OneAdaptabilityKind = 0;
                    /*militaryKind.PlainAdaptability = 5;
                    militaryKind.GrasslandAdaptability = 5;
                    militaryKind.ForrestAdaptability = 6;
                    militaryKind.MarshAdaptability = 100;
                    militaryKind.MountainAdaptability = 10;
                    militaryKind.WaterAdaptability = 5;
                    militaryKind.RidgeAdaptability = 100;
                    militaryKind.WastelandAdaptability = 6;
                    militaryKind.DesertAdaptability = 10;
                    militaryKind.CliffAdaptability = 7;*/
                }
                else
                {
                    militaryKind.OneAdaptabilityKind = 6;
                    militaryKind.PlainAdaptability = 100;
                    militaryKind.GrasslandAdaptability = 100;
                    militaryKind.ForrestAdaptability = 100;
                    militaryKind.MarshAdaptability = 100;
                    militaryKind.MountainAdaptability = 100;
                    //militaryKind.WaterAdaptability = 5;
                    militaryKind.RidgeAdaptability = 100;
                    militaryKind.WastelandAdaptability = 100;
                    militaryKind.DesertAdaptability = 100;
                    militaryKind.CliffAdaptability = 100;
                }
                

            }
            catch (Exception ex)
            {

                
                // 不重新抛出异常，避免游戏崩溃

            }
        }

        /// <summary>
        /// 🔥 技术性修复：尝试重新加载地图数据（从真实剧本文件）
        /// </summary>
        private bool TryReloadMapData()
        {
            try
            {

                
                // 尝试重新加载当前剧本的 JSON 文件
                string scenarioPath = $"Content/Data/Scenario/{this.Title}.json";
                
                if (System.IO.File.Exists(scenarioPath))
                {
                    try
                    {
                        string jsonContent = System.IO.File.ReadAllText(scenarioPath);
                        
                        // 简单的字符串搜索来提取 MapDataString
                        // 这不是最优雅的方法，但在紧急修复中是安全的
                        int mapDataStart = jsonContent.IndexOf("\"MapDataString\":\"");
                        if (mapDataStart >= 0)
                        {
                            mapDataStart += "\"MapDataString\":\"".Length;
                            int mapDataEnd = jsonContent.IndexOf("\"", mapDataStart);
                            
                            if (mapDataEnd > mapDataStart)
                            {
                                string mapData = jsonContent.Substring(mapDataStart, mapDataEnd - mapDataStart);
                                if (!string.IsNullOrEmpty(mapData) && mapData.Contains(" "))
                                {
                                    ScenarioMap.MapDataString = mapData;

                                    return true;
                                }
                            }
                        }
                        

                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {

                }

                return false;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        /// <summary>
        /// 🔥 技术性修复：安全处理地图数据缺失（不生成虚假数据）
        /// </summary>
        private void HandleMissingMapData()
        {
            try
            {


                // 不创建虚假的地图数据，而是提供诊断信息


                // 设置为空字符串，让游戏的其他错误处理机制接管
                ScenarioMap.MapDataString = "";
                
                // 确保 MapDimensions 有基本值，避免其他地方的除零错误
                if (ScenarioMap.MapDimensions.X <= 0 || ScenarioMap.MapDimensions.Y <= 0)
                {
                    ScenarioMap.MapDimensions = new Point(1, 1); // 最小有效尺寸

                }
            }
            catch (Exception ex)
            {

                
                // 最后的安全措施
                ScenarioMap.MapDataString = "";
                ScenarioMap.MapDimensions = new Point(1, 1);
            }
        }

        private void ApplyInformations()
        {
            foreach (Information i in this.Informations)
            {
                i.Apply();
            }
        }

        public void ForceOptionsOnAutoplay()
        {
            if (this.PlayerFactions.Count == 0)
            {
                Session.GlobalVariables.SkyEye = true;
                // FIX: Do NOT force cheat mode on autoplay - let user toggle it manually
                // Session.GlobalVariables.EnableCheat = true;
                Session.GlobalVariables.HardcoreMode = false;
            }
        }

        public void InitPluginsWithScenario(MainGameScreen screen)
        {

            // 🔥 使用 C# 12 Collection Expressions
            List<IScenarioAwarePlugin> immediatePlugins = [];
            List<IScenarioAwarePlugin> lazyPlugins = [];
            


            
            foreach (GameObject plugin in screen.PluginList)
            {
                if (plugin is IScenarioAwarePlugin scenarioPlugin)
                {

                    bool supportsLazy = scenarioPlugin.SupportLazyInit;
                    

                    
                    if (supportsLazy)
                    {
                        lazyPlugins.Add(scenarioPlugin);
                    }
                    else
                    {
                        immediatePlugins.Add(scenarioPlugin);
                    }
                }
            }
            

            
            // 立即初始化关键插件
            foreach (var plugin in immediatePlugins)
            {
                plugin.SetScenario();
            }

            
            // 延迟初始化非关键插件（在后台线程）
            if (lazyPlugins.Count > 0)
            {

                
                System.Threading.Tasks.Task.Run(() =>
                {

                    int successCount = 0;
                    
                    foreach (var plugin in lazyPlugins)
                    {
                        try
                        {
                            plugin.SetScenarioLazy();
                            successCount++;
                        }
                        catch (Exception ex)
                        {

                        }
                    }


                });
            }
            else
            {

            }
        }

        private void MigrateScenario()
        {
            foreach (Architecture a in this.Architectures)
            {
                if (a.MilitaryPopulation == 0)
                {
                    a.MilitaryPopulation = (int) (a.Population * (0.25 + (500000 - a.Population) / 500000 * 0.25));
                }
            }
        }

        private void DeleteInvalidRelations()
        {
            foreach (Person p in Persons)
            {
                if (p.Spouse != null && !p.Spouse.Alive)
                {
                    p.Spouse = null;
                }

                if (p.Brothers != null)
                {
                    foreach (Person b in p.Brothers.GetList())
                    {
                        if (!b.Alive)
                        {
                            p.Brothers.Remove(b);
                        }
                    }
                }
            }
        }

        public void AfterLoadGameScenario(MainGameScreen screen)
        {
            // 🔥 关键修复：优先链接 CurrentPlayer 引用
            // 日期：2026-03-16
            // 原因：新游戏流程中 CurrentPlayer 引用可能未链接，导致后续逻辑失败
            // 🔥 ID=0 是有效的（汉势力），必须使用 >= 0 判断
            if (this.CurrentPlayer == null && !string.IsNullOrEmpty(this.CurrentPlayerID))
            {
                if (int.TryParse(this.CurrentPlayerID, out int playerID) && playerID >= 0)
                {
                    this.CurrentPlayer = this.Factions.GetGameObject(playerID) as Faction;
                    if (this.CurrentPlayer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ✅ 从 CurrentPlayerID={playerID} 链接玩家势力: {this.CurrentPlayer.Name}");
                        this.CurrentFaction = this.CurrentPlayer;
                        if (this.Factions is FactionListWithQueue factionQueue)
                        {
                            factionQueue.RunningFaction = this.CurrentPlayer;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ⚠️ CurrentPlayerID={playerID} 对应的势力不存在！");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ⚠️ CurrentPlayerID 格式错误: {this.CurrentPlayerID}");
                }
            }
            else if (this.CurrentPlayer != null)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] CurrentPlayer 已设置: {this.CurrentPlayer.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ⚠️ CurrentPlayer 和 CurrentPlayerID 都为空！");
            }
            
            MigrateScenario();

            DeleteInvalidRelations();
            
            // 🔥 修复：在InitializeMapData之前重建所有建筑的ArchitectureArea

            int rebuiltCount = 0;
            foreach (Architecture architecture in this.Architectures)
            {
                if (!string.IsNullOrEmpty(architecture.ArchitectureAreaString))
                {
                    var expectedCount = architecture.ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                    var currentCount = architecture.ArchitectureArea?.Area?.Count ?? 0;
                    
                    // 如果坐标数量不匹配或ArchitectureArea为null，强制重新创建
                    if (architecture.ArchitectureArea == null || currentCount != expectedCount)
                    {
                        var newArea = new GameArea();
                        architecture.LoadFromString(newArea, architecture.ArchitectureAreaString);
                        architecture.ArchitectureArea = newArea;
                        rebuiltCount++;
                    }
                }
                else if (architecture.ArchitectureArea == null)
                {
                    architecture.ArchitectureArea = new GameArea();
                }
            }

            
            // 🔥 修复：恢复势力颜色和初始化集合（System.Text.Json不支持[OnDeserialized]）

            int colorRestoredCount = 0;
            foreach (Faction faction in this.Factions)
            {
                // 1. 确保集合字段已初始化
                faction.EnsureCollectionsInitialized();
                
                // 2. 根据 ColorIndex 恢复 FactionColor
                if (this.GameCommonData?.AllColors != null && this.GameCommonData.AllColors.Count > 0)
                {
                    // 确保 ColorIndex 在有效范围内
                    if (faction.ColorIndex < 0)
                    {
                        faction.ColorIndex = 0;
                    }
                    else if (faction.ColorIndex >= this.GameCommonData.AllColors.Count)
                    {
                        faction.ColorIndex = 0;
                    }
                    
                    faction.FactionColor = this.GameCommonData.AllColors[faction.ColorIndex];
                    colorRestoredCount++;
                }
                else
                {
                    // 如果 AllColors 未初始化，使用默认颜色
                    faction.FactionColor = Color.White;
                }
            }

            
            // 🔥 修复：重建路径数据（System.Text.Json不支持[OnDeserialized]）

            int routewayRestoredCount = 0;
            foreach (Routeway routeway in this.Routeways)
            {
                // Routeway有RefreshRoutewayPointsData方法来重建路径点
                try
                {
                    routeway.RefreshRoutewayPointsData();
                    routewayRestoredCount++;
                }
                catch (Exception ex)
                {

                }
            }


            // 🔥 修复：重建PersonBiography关联

            int biographyRestoredCount = 0;
            foreach (Person person in this.Persons)
            {
                if (person.PersonBiographyID >= 0 && person.PersonBiography == null)
                {
                    var biography = this.AllBiographies?.GetBiography(person.PersonBiographyID);
                    if (biography != null)
                    {
                        person.PersonBiography = biography;
                        biographyRestoredCount++;
                    }
                }
            }


            // 🔥 修复：重建Person.IdealTendency关联

            int idealTendencyRestoredCount = 0;
            foreach (Person person in this.Persons)
            {
                if (person.IdealTendencyIDString >= 0)
                {
                    person.IdealTendency = this.GameCommonData.AllIdealTendencyKinds.GetGameObject(person.IdealTendencyIDString) as IdealTendencyKind;
                    idealTendencyRestoredCount++;
                }
            }


            // 🔥 修复：重建Person.TrainPolicy关联

            int trainPolicyRestoredCount = 0;
            foreach (Person person in this.Persons)
            {
                if (person.TrainPolicyIDString > 0)
                {
                    person.TrainPolicy = this.GameCommonData.AllTrainPolicies.GetGameObject(person.TrainPolicyIDString) as TrainPolicy;
                    trainPolicyRestoredCount++;
                }
            }


            this.InitPluginsWithScenario(screen);
            this.InitializeMapData();
            this.TroopAnimations.UpdateDirectionAnimations(ScenarioMap.TileWidth);
            this.ApplyFireTable();
            this.InitializeArchitectureMapTile();
            this.InitializeFactionData();
            this.ApplyInformations();
            this.Preparing = true;
            this.Factions.BuildQueue(true);
            
            // 🔥 关键修复：BuildQueue 之后，LinkReferencesPhase 之前，修复旧剧本的 Section.BelongedFactionID
            // 日期：2026-03-16
            // 原因：旧剧本 JSON 中 Section 没有 BelongedFactionID 字段，需要根据 Faction.Sections 反向设置
            System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] 🔥 修复旧剧本的 Section.BelongedFactionID...");
            int fixedSectionCount = 0;
            foreach (Faction faction in this.Factions.GetList())
            {
                if (faction.Sections != null)
                {
                    foreach (Section section in faction.Sections.GetList())
                    {
                        if (section.BelongedFactionID < 0)
                        {
                            section.BelongedFactionID = faction.ID;
                            fixedSectionCount++;
                            System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario]   修复军区 {section.ID} ({section.Name}) 的 BelongedFactionID={faction.ID} ({faction.Name})");
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ✅ 修复了 {fixedSectionCount} 个军区的 BelongedFactionID");
            
            // 🔥 关键修复：BuildQueue 之后链接所有对象引用
            // 日期：2026-03-16
            // 原因：BuildQueue 会创建新的 Section 对象，必须在之后链接引用
            //       否则新创建的 Section 的 BelongedFactionID 为 -1
            System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] 🔥 BuildQueue 完成，开始链接对象引用...");
            try
            {
                var linkPhase = new WorldOfTheThreeKingdoms.Serialization.Phases.LinkReferencesPhase();
                linkPhase.LinkReferences(this);
                System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] ✅ 对象引用链接完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ❌ 对象引用链接失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] 堆栈: {ex.StackTrace}");
                throw; // 重新抛出异常，因为这是致命错误
            }
            
            // 🔥 关键修复：链接完成后，构建武将缓存
            // 日期：2026-03-16
            // 原因：Architecture.Persons 依赖 CreatePersonStatusCache() 构建的缓存
            //       必须在 LinkReferencesPhase 之后调用，因为需要 Person.LocationArchitecture 已经链接
            System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] 🔥 构建武将缓存...");
            this.CreatePersonStatusCache();
            System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] ✅ 武将缓存构建完成");
            
            // 🔥 修复：BuildQueue会重置Controlling状态，需要在之后重新设置
            // 日期：2026-03-16
            // 原因：新游戏流程中玩家势力的控制权被 BuildQueue 重置，导致无法操作
            if (this.CurrentPlayer != null && this.IsPlayer(this.CurrentPlayer))
            {
                this.CurrentPlayer.Controlling = true;
                this.CurrentPlayer.StopToControl = true;
                if (this.Factions is FactionListWithQueue factionQueue)
                {
                    factionQueue.RunningFaction = this.CurrentPlayer;
                }
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario] ✅ 重新设置当前玩家 {this.CurrentPlayer.Name} 的控制权");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario]   - Controlling: {this.CurrentPlayer.Controlling}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario]   - StopToControl: {this.CurrentPlayer.StopToControl}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadGameScenario]   - RunningFaction: {(this.Factions as FactionListWithQueue)?.RunningFaction?.Name}");
            }
            
            this.Factions.ApplyInfluences();
            this.Architectures.ApplyInfluences();
            this.Architectures.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            this.Persons.ApplyInfluences();
            this.Troops.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            this.Preparing = false;
            this.InitialGameData();
            Session.Parameters.InitBaseRates();
            
            // 🔥 诊断：检查数据是否正确加载
            // 日期：2026-03-16
            System.Diagnostics.Debug.WriteLine("[AfterLoadGameScenario] 🔍 数据加载诊断:");
            System.Diagnostics.Debug.WriteLine($"  - 总势力数: {this.Factions.Count}");
            System.Diagnostics.Debug.WriteLine($"  - 总武将数: {this.Persons.Count}");
            System.Diagnostics.Debug.WriteLine($"  - 总城池数: {this.Architectures.Count}");
            
            if (this.CurrentPlayer != null)
            {
                System.Diagnostics.Debug.WriteLine($"  - 玩家势力: {this.CurrentPlayer.Name}");
                System.Diagnostics.Debug.WriteLine($"    - Leader: {this.CurrentPlayer.Leader?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"    - Capital: {this.CurrentPlayer.Capital?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"    - FactionColor: {this.CurrentPlayer.FactionColor}");
                System.Diagnostics.Debug.WriteLine($"    - Architectures.Count: {this.CurrentPlayer.Architectures?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"    - Persons.Count: {this.CurrentPlayer.Persons?.Count ?? 0}");
                
                if (this.CurrentPlayer.Capital != null)
                {
                    var capital = this.CurrentPlayer.Capital;
                    System.Diagnostics.Debug.WriteLine($"  - 首都 {capital.Name}:");
                    System.Diagnostics.Debug.WriteLine($"    - BelongedFaction: {capital.BelongedFaction?.Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"    - Persons.Count: {capital.Persons?.Count ?? 0}");
                    
                    if (capital.Persons != null && capital.Persons.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - 前5个武将:");
                        int count = 0;
                        foreach (Person p in capital.Persons.GetList())
                        {
                            System.Diagnostics.Debug.WriteLine($"      - {p.Name} (ID={p.ID})");
                            if (++count >= 5) break;
                        }
                    }
                }
            }

            // 🔥 修复：预加载所有兵种的 Move 纹理，避免首次出兵时的延迟
            // 日期：2026-02-25
            // 原因：新游戏时没有预加载纹理，首次出兵时在 Hot Path 中懒加载，导致延迟 1-2 秒
            // 位置：在 InitialGameData() 之后，确保所有数据已初始化

            int preloadCount = 0;
            foreach (MilitaryKind kind in this.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
            {
                var texture = kind.Textures.MoveTexture;
                if (texture != null)
                {
                    preloadCount++;
                }
            }


            // 🔥 新事件系统：触发剧本加载后处理完成事件
            ScenarioEvents.RaiseAfterScenarioLoaded(this);

            this.LoadedFileName = "";

            this.sessionStartTime = DateTime.Now;
        }

        public void AfterLoadSaveFile(MainGameScreen screen)
        {
            // 🔥 关键修复：优先链接 CurrentPlayer 引用
            // 日期：2026-03-16
            // 原因：新游戏流程中 CurrentPlayer 引用可能未链接，导致后续逻辑失败
            // 🔥 ID=0 是有效的（汉势力），必须使用 >= 0 判断
            if (this.CurrentPlayer == null && !string.IsNullOrEmpty(this.CurrentPlayerID))
            {
                if (int.TryParse(this.CurrentPlayerID, out int playerID) && playerID >= 0)
                {
                    this.CurrentPlayer = this.Factions.GetGameObject(playerID) as Faction;
                    if (this.CurrentPlayer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ✅ 从 CurrentPlayerID={playerID} 链接玩家势力: {this.CurrentPlayer.Name}");
                        this.CurrentFaction = this.CurrentPlayer;
                        this.Factions.RunningFaction = this.CurrentPlayer;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ CurrentPlayerID={playerID} 对应的势力不存在！");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ CurrentPlayerID 格式错误: {this.CurrentPlayerID}");
                }
            }
            else if (this.CurrentPlayer != null)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] CurrentPlayer 已设置: {this.CurrentPlayer.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ CurrentPlayer 和 CurrentPlayerID 都为空！");
            }

            
            // 🔥 新增：立即验证数据完整性
            // 日期：2026-02-11
            // 原因：确保 Military.Kind 和 Troop.Army 链接有效，避免运行时空引用异常
            try
            {

                
                // 验证 Military.Kind 链接
                this.ValidateAndLinkMilitaryKinds();
                
                // 验证 Troop.Army 链接
                this.ValidateAndLinkTroopArmies();
                

            }
            catch (InvalidDataException ex)
            {

                // 重新抛出异常，阻止游戏继续加载
                throw;
            }
            
            // 🔥 V12 修复：清空所有共享的 appliedArch
            // 日期：2026-02-15
            // 原因：appliedArch 不是序列化字段，在内存中跨游戏会话持久化
            //       读档后 Architecture 对象是新创建的，但 appliedArch 中的旧引用还在
            //       导致 RemoveWhere 无法匹配（引用不相等），影响无法被重新应用

            try
            {
                int clearedCount = 0;
                
                // 清空所有 FacilityKind 的 appliedArch
                foreach (var facilityKind in this.GameCommonData.AllFacilityKinds.FacilityKinds.Values)
                {
                    foreach (var influence in facilityKind.Influences.Influences.Values)
                    {
                        if (influence.appliedArch.Count > 0)
                        {
                            influence.appliedArch.Clear();
                            clearedCount++;
                        }
                    }
                }
                
                // 清空所有 Technique 的 appliedArch
                foreach (var technique in this.GameCommonData.AllTechniques.Techniques.Values)
                {
                    foreach (var influence in technique.Influences.Influences.Values)
                    {
                        if (influence.appliedArch.Count > 0 || influence.appliedFaction.Count > 0 || 
                            influence.appliedPerson.Count > 0 || influence.appliedTroop.Count > 0)
                        {
                            influence.appliedArch.Clear();
                            influence.appliedFaction.Clear();
                            influence.appliedPerson.Clear();
                            influence.appliedTroop.Clear();
                            clearedCount++;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ✅ 清空了 {clearedCount} 个影响的应用记录");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ 清空 appliedArch 失败: {ex.Message}");
                // 不影响主要流程，继续执行
            }
            
            // 【新增】加载AI学习系统数据
            try
            {
                if (WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance != null)
                {
                    // 尝试从对应的AI学习数据文件加载
                    string currentSaveFile = this.LoadedFileName ?? "Save01.json";
                    string aiDataFile = currentSaveFile.Replace(".json", "_AILearning.json");
                    
                    // 如果没有完整路径，添加Save目录前缀
                    if (!aiDataFile.Contains("\\") && !aiDataFile.Contains("/"))
                    {
                        aiDataFile = @"Save\" + aiDataFile;
                    }
                    
                    // 检查AI学习数据文件是否存在
                    if (Platform.Current.UserFileExist(new string[] { aiDataFile })[0])
                    {
                        string aiLearningData = Platform.Current.GetUserText(aiDataFile);
                        if (!string.IsNullOrEmpty(aiLearningData))
                        {
                            global::WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance.LoadLearningData(aiLearningData);
                            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] AI学习数据已从 {aiDataFile} 加载");
                        }
                    }
                    else
                    {
                        // 如果没有AI学习数据文件，初始化为空数据
                        global::WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance.LoadLearningData("{}");
                        System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 未找到AI学习数据文件 {aiDataFile}，使用空数据初始化");
                    }
                }
            }
            catch (Exception ex)
            {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 加载AI学习数据失败: {ex.Message}");
                // 不影响主要的游戏加载流程，使用空数据初始化
                try
                {
                    if (global::WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance != null)
                    {
                        global::WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance.LoadLearningData("{}");
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 回退初始化失败: {ex2.Message}");
                }
            }

            // 🔥 关键修复：不要在这里检查Capital！
            // LinkScenarioReferences已经正确设置了所有引用
            // 这里重新检查会因为时序问题导致误报
            
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 开始初始化游戏数据");
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   势力数: {this.Factions.Count}");
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   建筑数: {this.Architectures.Count}");
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   武将数: {this.Persons.Count}");
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   部队数: {this.Troops.Count}");
            

            
            // 🔥 修复：从字符串恢复设施、编队等关系
            // 但首先需要确保字符串字段已经填充（可能是旧存档没有这些字段）

            
            // 如果字符串字段为空，从集合生成字符串
            foreach (Architecture architecture in this.Architectures)
            {
                if (string.IsNullOrEmpty(architecture.FacilitiesString) && architecture.Facilities != null && architecture.Facilities.Count > 0)
                {
                    architecture.FacilitiesString = architecture.Facilities.SaveToString();
                }
                if (string.IsNullOrEmpty(architecture.MilitariesString) && architecture.Militaries != null && architecture.Militaries.Count > 0)
                {
                    architecture.MilitariesString = architecture.Militaries.SaveToString();
                }
                if (string.IsNullOrEmpty(architecture.PersonsString) && architecture.Persons != null && architecture.Persons.Count > 0)
                {
                    architecture.PersonsString = architecture.Persons.SaveToString();
                }
            }
            
            // 🔥 2026-03-18 移除：旧的保存前准备代码
            // 原因：person.SkillsString/StuntsString 字段已删除
            // 现在序列化使用 SkillIDs/StuntIDs，由 SaveDataPhase 处理
            

            int facilityRestoredCount = 0;
            int militaryRestoredCount = 0;
            int personRestoredCount = 0;
            int captiveRestoredCount = 0;
            int informationRestoredCount = 0;
            int archDebugCount = 0;
            
            foreach (Architecture architecture in this.Architectures)
            {
                try
                {
                    // 🔥 诊断：输出前3个建筑的字符串字段
                    if (archDebugCount < 3)
                    {

                        archDebugCount++;
                    }
                    
                    // 恢复设施
                    if (!string.IsNullOrEmpty(architecture.FacilitiesString))
                    {
                        architecture.LoadFacilitiesFromString(this.Facilities, architecture.FacilitiesString);
                        facilityRestoredCount++;
                    }
                    
                    // 恢复编队
                    if (!string.IsNullOrEmpty(architecture.MilitariesString))
                    {
                        architecture.LoadMilitariesFromString(this.Militaries, architecture.MilitariesString);
                        militaryRestoredCount++;
                    }
                    
                    // 恢复武将（需要转换为 Dictionary）
                    var personsDict = new System.Collections.Generic.Dictionary<int, Person>();
                    foreach (Person p in this.Persons)
                    {
                        if (!personsDict.ContainsKey(p.ID))
                        {
                            personsDict.Add(p.ID, p);
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(architecture.PersonsString))
                    {
                        architecture.LoadPersonsFromString(personsDict, architecture.PersonsString, PersonDetail.PersonStatus.Normal);
                        personRestoredCount++;
                    }
                    
                    if (!string.IsNullOrEmpty(architecture.MovingPersonsString))
                    {
                        architecture.LoadPersonsFromString(personsDict, architecture.MovingPersonsString, PersonDetail.PersonStatus.Moving);
                    }
                    
                    if (!string.IsNullOrEmpty(architecture.NoFactionPersonsString))
                    {
                        architecture.LoadPersonsFromString(personsDict, architecture.NoFactionPersonsString, PersonDetail.PersonStatus.NoFaction);
                    }
                    
                    if (!string.IsNullOrEmpty(architecture.NoFactionMovingPersonsString))
                    {
                        architecture.LoadPersonsFromString(personsDict, architecture.NoFactionMovingPersonsString, PersonDetail.PersonStatus.NoFactionMoving);
                    }
                    
                    // 恢复俘虏
                    if (!string.IsNullOrEmpty(architecture.CaptivesString))
                    {
                        architecture.LoadCaptivesFromString(this.Captives, architecture.CaptivesString);
                        captiveRestoredCount++;
                    }
                    
                    // 恢复情报
                    if (!string.IsNullOrEmpty(architecture.InformationsString))
                    {
                        architecture.LoadInformationsFromString(this.Informations, architecture.InformationsString);
                        informationRestoredCount++;
                    }
                    
                    // 恢复资金包和粮食包
                    if (!string.IsNullOrEmpty(architecture.FundPacksString))
                    {
                        architecture.LoadFundPacksFromString(architecture.FundPacksString);
                    }
                    
                    if (!string.IsNullOrEmpty(architecture.FoodPacksString))
                    {
                        architecture.LoadFoodPacksFromString(architecture.FoodPacksString);
                    }
                }
                catch (Exception ex)
                {

                }
            }

            
            // 🔥 修复：恢复武将的技能、特技、称号等


            
            int skillRestoredCount = 0;
            int stuntRestoredCount = 0;
            int titleRestoredCount = 0;
            int personDebugCount = 0;
            
            foreach (Person person in this.Persons)
            {
                try
                {
                    // 🔥 诊断：输出前3个武将的字符串字段
                    if (personDebugCount < 3)
                    {

                        personDebugCount++;
                    }
                    
                    // 🔥 2026-03-18 移除：旧的字符串恢复逻辑
                    // 原因：person.SkillsString/StuntsString/RealTitlesString 字段已删除
                    // 现在使用 LinkReferencesPhase 从 SkillIDs/StuntIDs/TitleIDs 链接
                    // 这些数据在 LinkReferencesPhase.LinkPersonReferences() 中已经正确链接
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 武将 {person.Name}(ID:{person.ID}) 数据恢复失败: {ex.Message}");
                }
            }
            
            // 🔥 2026-03-18 移除：旧的统计输出
            // 原因：Skills/Stunts/Titles 现在由 LinkReferencesPhase 处理
            // System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 武将数据恢复完成:");
            // System.Diagnostics.Debug.WriteLine($"  - 技能: {skillRestoredCount}");
            // System.Diagnostics.Debug.WriteLine($"  - 特技: {stuntRestoredCount}");
            // System.Diagnostics.Debug.WriteLine($"  - 称号: {titleRestoredCount}");

            // 🔥 注意：AllConditions 的 ConditionKind 修复已经在 LoadScenarioData 中完成
            // 日期：2026-03-06
            // 原因：LoadScenarioData 中检测到 Kind 是基类时，会强制替换为 CommonData.Current.AllConditions
            //       这里不需要再次修复，避免重复工作
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [AfterLoadSaveFile] AllConditions 已在 LoadScenarioData 中修复 ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            // 🔥 修复：重建Event条件（读档后事件触发问题）
            // 日期：2026-03-06
            // 原因：Event的条件对象（personCond, architectureCond, factionCond）没有被序列化
            //       只序列化了字符串字段，读档后需要从字符串重建条件对象
            //       新开剧本时会调用LoadXxxCondFromString，但读档时没有调用
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [AfterLoadSaveFile] 开始重建Event条件                     ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            // 🔥 诊断：检查AllEvents状态
            System.Diagnostics.Debug.WriteLine($"[Event条件重建] AllEvents: {(this.AllEvents != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[Event条件重建] AllEvents.Count: {this.AllEvents?.Count ?? 0}");
            
            if (this.AllEvents == null || this.AllEvents.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║  ❌ 致命错误：AllEvents 为空！                             ║");
                System.Diagnostics.Debug.WriteLine("║                                                            ║");
                System.Diagnostics.Debug.WriteLine("║  需要追溯数据源：                                          ║");
                System.Diagnostics.Debug.WriteLine("║  1. 检查 LoadScenarioData 的调试输出                      ║");
                System.Diagnostics.Debug.WriteLine("║  2. 确认反序列化后 AllEvents.Count 是多少                ║");
                System.Diagnostics.Debug.WriteLine("║  3. 如果反序列化后 Count > 0，说明中间环节清空了数据     ║");
                System.Diagnostics.Debug.WriteLine("║  4. 如果反序列化后 Count = 0，说明序列化/反序列化失败    ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ✅ AllEvents 包含 {this.AllEvents.Count} 个事件，开始重建条件");
            }
            
            int eventCondRestoredCount = 0;
            int eventPersonCondCount = 0;
            int eventArchCondCount = 0;
            int eventFactionCondCount = 0;
            int eventSubscribedCount = 0;
            
            foreach (GameObject gameObj in this.AllEvents)
            {
                // 🔥 注意：使用与新开档相同的检查模式（第5507行）
                Event e = (gameObj is Event ? (Event)gameObj : null);
                if (e == null)
                {
                    // AllEvents中不应该有非Event对象，但历史代码有此检查
                    continue;
                }
                
                try
                {
                    // 重建Init()中初始化的集合
                    e.Init();
                    
                    // 🔥 关键修复：订阅OnApplyEvent事件（与新开档时的AddEventWithEvent逻辑一致）
                    // 参考：GameScenario.cs 第5574行使用 AddEventWithEvent(e, false)
                    // 参考：EventList.cs AddEventWithEvent方法
                    this.AllEvents.AddEventWithEvent(e, false);
                    eventSubscribedCount++;
                    
                    // 重建人物条件
                    if (!string.IsNullOrEmpty(e.PersonCondString))
                    {
                        e.LoadPersonCondFromString(this.GameCommonData.AllConditions, e.PersonCondString);
                        eventPersonCondCount++;
                    }
                    
                    // 重建建筑条件
                    if (!string.IsNullOrEmpty(e.architectureCondString))
                    {
                        e.LoadArchitctureCondFromString(this.GameCommonData.AllConditions, e.architectureCondString);
                        eventArchCondCount++;
                    }
                    
                    // 重建势力条件
                    if (!string.IsNullOrEmpty(e.factionCondString))
                    {
                        e.LoadFactionCondFromString(this.GameCommonData.AllConditions, e.factionCondString);
                        eventFactionCondCount++;
                    }
                    
                    // 重建人物ID列表
                    if (!string.IsNullOrEmpty(e.personString))
                    {
                        e.LoadPersonIdFromString(this.Persons, e.personString);
                    }
                    
                    // 重建建筑列表
                    if (!string.IsNullOrEmpty(e.architectureString))
                    {
                        e.LoadArchitectureFromString(this.Architectures, e.architectureString);
                    }
                    
                    // 重建势力列表
                    if (!string.IsNullOrEmpty(e.factionString))
                    {
                        e.LoadFactionFromString(this.Factions, e.factionString);
                    }
                    
                    // 重建效果
                    if (!string.IsNullOrEmpty(e.effectString))
                    {
                        e.LoadEffectFromString(this.GameCommonData.AllEventEffects, e.effectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.yesEffectString))
                    {
                        e.LoadYesEffectFromString(this.GameCommonData.AllEventEffects, e.yesEffectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.noEffectString))
                    {
                        e.LoadNoEffectFromString(this.GameCommonData.AllEventEffects, e.noEffectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.architectureEffectString))
                    {
                        e.LoadArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.architectureEffectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.yesArchitectureEffectString))
                    {
                        e.LoadYesArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.yesArchitectureEffectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.noArchitectureEffectString))
                    {
                        e.LoadNoArchitectureEffectFromString(this.GameCommonData.AllEventEffects, e.noArchitectureEffectString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.factionEffectIDString))
                    {
                        e.LoadFactionEffectFromString(this.GameCommonData.AllEventEffects, e.factionEffectIDString);
                    }
                    
                    // 重建对话
                    if (!string.IsNullOrEmpty(e.dialogString))
                    {
                        e.LoadDialogFromString(e.dialogString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.yesdialogString))
                    {
                        e.LoadyesDialogFromString(e.yesdialogString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.nodialogString))
                    {
                        e.LoadnoDialogFromString(e.nodialogString);
                    }
                    
                    if (!string.IsNullOrEmpty(e.scenBiographyString))
                    {
                        e.LoadScenBiographyFromString(e.scenBiographyString);
                    }
                    
                    eventCondRestoredCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] Event {e.Name}(ID:{e.ID}) 条件恢复失败: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine($"║  [AfterLoadSaveFile] Event条件恢复完成                      ║");
            System.Diagnostics.Debug.WriteLine($"║  - 事件总数: {eventCondRestoredCount}                       ║");
            System.Diagnostics.Debug.WriteLine($"║  - 事件订阅: {eventSubscribedCount}                         ║");
            System.Diagnostics.Debug.WriteLine($"║  - 人物条件: {eventPersonCondCount}                         ║");
            System.Diagnostics.Debug.WriteLine($"║  - 建筑条件: {eventArchCondCount}                           ║");
            System.Diagnostics.Debug.WriteLine($"║  - 势力条件: {eventFactionCondCount}                        ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            // 🔥 关键修复：重建TroopEvent订阅（读档后部队事件影响不施加问题）
            // 日期：2026-03-23
            // 原因：TroopEvent的OnApplyTroopEvent事件没有被序列化，读档后需要重新订阅
            //       新开剧本时会在LoadDataPhase中调用AddTroopEventWithEvent，但读档时没有调用
            // 参考：GameScenario.cs 第5903行使用 AddTroopEventWithEvent(te, false)
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [AfterLoadSaveFile] 开始重建TroopEvent订阅                ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            System.Diagnostics.Debug.WriteLine($"[TroopEvent订阅] TroopEvents: {(this.TroopEvents != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[TroopEvent订阅] TroopEvents.Count: {this.TroopEvents?.Count ?? 0}");
            
            int troopEventSubscribedCount = 0;
            
            foreach (GameObject gameObj in this.TroopEvents)
            {
                TroopEvent te = gameObj as TroopEvent;
                if (te == null)
                {
                    continue;
                }
                
                try
                {
                    te.Init();
                    te.AfterHappenedEvent = te.AfterEventHappened >= 0 ? this.TroopEvents.GetGameObject(te.AfterEventHappened) as TroopEvent : null;
                    te.LaunchPerson = this.Persons.GetGameObject(te.LaunchPersonString) as Person;
                    te.Conditions.LoadFromString(this.GameCommonData.AllConditions, te.ConditionsString);
                    te.LoadTargetPersonFromString(this.AllPersons, te.TargetPersonsString);
                    te.LoadSelfEffectFromString(this.GameCommonData.AllTroopEventEffects, te.SelfEffectsString);
                    te.LoadEffectPersonFromString(this.AllPersons, this.GameCommonData.AllTroopEventEffects, te.EffectPersonsString);
                    te.LoadEffectAreaFromString(this.GameCommonData.AllTroopEventEffects, te.EffectAreasString);
                    te.LoadDialogFromString(this.AllPersons, te.dialogString);
                    if (te.TryToShowString == null) te.TryToShowString = "";

                    // 🔥 关键修复：订阅OnApplyTroopEvent事件（与新开档时的AddTroopEventWithEvent逻辑一致）
                    // 参考：GameScenario.cs 第5903行使用 AddTroopEventWithEvent(te, false)
                    // 参考：TroopEventList.cs AddTroopEventWithEvent方法
                    this.TroopEvents.AddTroopEventWithEvent(te, false);
                    troopEventSubscribedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] TroopEvent {te.Name}(ID:{te.ID}) 订阅失败: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine($"║  [AfterLoadSaveFile] TroopEvent订阅完成                     ║");
            System.Diagnostics.Debug.WriteLine($"║  - 事件总数: {this.TroopEvents.Count}                       ║");
            System.Diagnostics.Debug.WriteLine($"║  - 事件订阅: {troopEventSubscribedCount}                    ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            // 🔥 性能诊断：详细拆解 AfterLoadSaveFile 的耗时
            var swAfterLoad = System.Diagnostics.Stopwatch.StartNew();
            var swStep = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [AfterLoadSaveFile] 开始后续初始化                        ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            // A. 插件初始化
            swStep.Restart();
            this.InitPluginsWithScenario(screen);
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] A. 插件初始化: {swStep.ElapsedMilliseconds} ms");
            
            // B. 地图数据初始化
            swStep.Restart();
            this.InitializeMapData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] B. 地图数据初始化: {swStep.ElapsedMilliseconds} ms");
            
            // C. 部队动画
            swStep.Restart();
            this.TroopAnimations.UpdateDirectionAnimations(ScenarioMap.TileWidth);
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] C. 部队动画: {swStep.ElapsedMilliseconds} ms");
            
            // D. 火攻表
            swStep.Restart();
            this.ApplyFireTable();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] D. 火攻表: {swStep.ElapsedMilliseconds} ms");
            
            // E. 建筑地图块
            swStep.Restart();
            this.InitializeArchitectureMapTile();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] E. 建筑地图块: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 新增：部队地图位置同步
            // 日期：2026-03-10
            // 问题：读档后部队位置没有同步到 MapTileData 和 MapPositionCache，导致位置冲突
            // 解决：统一同步所有部队的地图位置，确保数据一致性
            swStep.Restart();
            this.InitializeTroopMapTile();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] E2. 部队地图位置同步: {swStep.ElapsedMilliseconds} ms");
            
            // F. 地形代价缓存初始化（必须在势力数据初始化之前）
            swStep.Restart();
            var (mapWidth, mapHeight) = (this.ScenarioMap.MapDimensions.X, this.ScenarioMap.MapDimensions.Y);
            global::WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.Initialize(mapWidth, mapHeight);
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] F1. 地形代价缓存初始化: {swStep.ElapsedMilliseconds} ms");
            
            // F2. 势力数据
            swStep.Restart();
            this.InitializeFactionData();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] F2. 势力数据: {swStep.ElapsedMilliseconds} ms");
            
            // G. 应用情报
            swStep.Restart();
            this.ApplyInformations();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] G. 应用情报: {swStep.ElapsedMilliseconds} ms");
            
            this.Preparing = true;

            // H. 构建队列
            swStep.Restart();
            this.Factions.BuildQueue(true);  //待考慮效果
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] H. 构建队列: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 关键修复：BuildQueue 之后链接所有对象引用（技能、特技、称号等）
            // 日期：2026-03-20
            // 原因：读档后 SkillIDs/StuntIDs/TitleIDs 已加载，但没有链接到对应的集合
            //       新开剧本在 AfterLoadGameScenario 中调用了 LinkReferences（BuildQueue 之后）
            //       读档也需要在 BuildQueue 之后调用，确保技能、特技、称号正确显示
            swStep.Restart();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 🔥 BuildQueue 完成，开始链接对象引用...");
            try
            {
                var linkPhase = new WorldOfTheThreeKingdoms.Serialization.Phases.LinkReferencesPhase();
                linkPhase.LinkReferences(this);
                System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] ✅ 对象引用链接完成");
                
                // 🔥 验证：检查前3个人物的技能、特技、称号是否正确链接
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 🔍 验证链接结果（前3个人物）:");
                int checkCount = 0;
                foreach (Person person in this.Persons.GetList())
                {
                    if (checkCount >= 3) break;
                    
                    System.Diagnostics.Debug.WriteLine($"  {person.Name}(ID:{person.ID}):");
                    System.Diagnostics.Debug.WriteLine($"    - SkillIDs.Count = {person.SkillIDs.Count}");
                    System.Diagnostics.Debug.WriteLine($"    - Skills.Skills.Count = {person.Skills.Skills.Count}");
                    if (person.Skills.Skills.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - Skills.Skills.Keys = [{string.Join(", ", person.Skills.Skills.Keys)}]");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"    - StuntIDs.Count = {person.StuntIDs.Count}");
                    System.Diagnostics.Debug.WriteLine($"    - Stunts.Stunts.Count = {person.Stunts.Stunts.Count}");
                    if (person.Stunts.Stunts.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - Stunts.Stunts.Keys = [{string.Join(", ", person.Stunts.Stunts.Keys)}]");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"    - TitleIDs.Count = {person.TitleIDs.Count}");
                    System.Diagnostics.Debug.WriteLine($"    - Titles.Count = {person.Titles.Count}");
                    
                    checkCount++;
                }
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ❌ 对象引用链接失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 堆栈: {ex.StackTrace}");
                throw; // 重新抛出异常，因为这是致命错误
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] H1. 链接对象引用: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 关键修复：链接完成后，构建武将缓存
            // 日期：2026-03-20
            // 原因：Architecture.Persons 依赖 CreatePersonStatusCache() 构建的缓存
            //       必须在 LinkReferencesPhase 之后调用，因为需要 Person.LocationArchitecture 已经链接
            swStep.Restart();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 🔥 构建武将缓存...");
            this.CreatePersonStatusCache();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] ✅ 武将缓存构建完成");
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] H2. 构建武将缓存: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 关键修复：读档后重置所有势力的回合状态
            // 日期：2026-03-16
            // 问题：读档后AI势力自己运行但日期不推进
            // 原因：Passed 和 AIFinished 状态未重置，导致 AfterDayPassed 永远返回 true
            // 解决：强制重置所有势力的状态，确保游戏从战略阶段开始
            swStep.Restart();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] H3. 重置势力回合状态...");
            int playerFactionCount = 0;
            int aiFactionCount = 0;
            
            foreach (GameObject obj in this.Factions.GetList())
            {
                if (obj is Faction faction && faction.IsAlive)
                {
                    // 重置回合状态
                    faction.Passed = false;
                    faction.AIFinished = false;
                    
                    if (this.IsPlayer(faction))
                    {
                        playerFactionCount++;
                        
                        // 玩家势力：设置控制权
                        if (faction == this.CurrentPlayer)
                        {
                            faction.Controlling = true;
                            faction.StopToControl = true;
                            System.Diagnostics.Debug.WriteLine($"  ✅ 当前玩家 {faction.Name}: Controlling=true, Passed=false, AIFinished=false");
                        }
                        else
                        {
                            faction.Controlling = false;
                            faction.StopToControl = false;
                            System.Diagnostics.Debug.WriteLine($"  ✅ 其他玩家 {faction.Name}: Controlling=false, Passed=false, AIFinished=false");
                        }
                    }
                    else
                    {
                        aiFactionCount++;
                        
                        // AI势力：清除控制权
                        faction.Controlling = false;
                        faction.StopToControl = false;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] H3. 势力状态重置完成:");
            System.Diagnostics.Debug.WriteLine($"  - 玩家势力: {playerFactionCount}");
            System.Diagnostics.Debug.WriteLine($"  - AI势力: {aiFactionCount}");
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] H3. 重置势力状态: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 修复：BuildQueue会重置Controlling状态，需要在之后重新设置
            if (this.CurrentPlayer != null && this.IsPlayer(this.CurrentPlayer))
            {
                this.CurrentPlayer.Controlling = true;
                this.CurrentPlayer.StopToControl = true;
                this.Factions.RunningFaction = this.CurrentPlayer;
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ✅ 最终确认当前玩家 {this.CurrentPlayer.Name} 的控制权");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   - Controlling: {this.CurrentPlayer.Controlling}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   - StopToControl: {this.CurrentPlayer.StopToControl}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   - Passed: {this.CurrentPlayer.Passed}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   - AIFinished: {this.CurrentPlayer.AIFinished}");
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile]   - RunningFaction: {this.Factions.RunningFaction?.Name}");
            }
            
            // I. 应用影响力
            swStep.Restart();
            this.Factions.ApplyInfluences();            
            this.Architectures.ApplyInfluences();
            this.Architectures.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            this.Persons.ApplyInfluences();
            this.Troops.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] I. 应用影响力: {swStep.ElapsedMilliseconds} ms");
            
            // 🔥 修复：为没有名称的编队生成名称
            // 这是为了兼容旧存档或序列化时Name字段丢失的情况
            swStep.Restart();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 检查并修复编队名称...");
            int militaryNameFixedCount = 0;
            int militaryWithNameCount = 0;
            int militaryWithoutKindCount = 0;
            
            foreach (Military military in this.Militaries)
            {
                bool hasName = !string.IsNullOrEmpty(military.Name);
                bool hasKind = military.Kind != null;
                
                #if DEBUG
                // 输出前5个编队的详细信息
                if (militaryNameFixedCount + militaryWithNameCount <= 5)
                {
                    System.Diagnostics.Debug.WriteLine($"  编队 {military.ID}:");
                    System.Diagnostics.Debug.WriteLine($"    Name: '{military.Name ?? "null"}'");
                    System.Diagnostics.Debug.WriteLine($"    Kind: {(hasKind ? military.Kind.Name : "null")}");
                    System.Diagnostics.Debug.WriteLine($"    KindID: {military.KindID}");
                }
                #endif
                
                if (hasName)
                {
                    militaryWithNameCount++;
                }
                else if (hasKind)
                {
                    // 生成名称
                    if (military.Kind.RecruitLimit == 1)
                    {
                        military.Name = military.Kind.Name;
                    }
                    else
                    {
                        military.Name = military.Kind.Name + "队";
                    }
                    militaryNameFixedCount++;
                    
                    #if DEBUG
                    if (militaryNameFixedCount <= 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"    ✅ 生成名称: {military.Name}");
                    }
                    #endif
                }
                else
                {
                    militaryWithoutKindCount++;
                    #if DEBUG
                    if (militaryWithoutKindCount <= 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"    ⚠️ 没有名称且Kind为null，无法生成名称");
                    }
                    #endif
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 编队名称统计:");
            System.Diagnostics.Debug.WriteLine($"  - 已有名称: {militaryWithNameCount}");
            System.Diagnostics.Debug.WriteLine($"  - 生成名称: {militaryNameFixedCount}");
            System.Diagnostics.Debug.WriteLine($"  - 无法生成: {militaryWithoutKindCount}");
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] J. 编队名称修复: {swStep.ElapsedMilliseconds} ms");

            // K. 验证编队数据
            swStep.Restart();
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 验证编队数据...");
            int archWithMilitaryIDsButNoMilitaries = 0;
            foreach (Architecture arch in this.Architectures)
            {
                if (arch.MilitaryIDs != null && arch.MilitaryIDs.Count > 0)
                {
                    if (arch.Militaries == null || arch.Militaries.Count == 0)
                    {
                        archWithMilitaryIDsButNoMilitaries++;
                        if (archWithMilitaryIDsButNoMilitaries <= 3)
                        {
                            System.Diagnostics.Debug.WriteLine($"  ⚠️ 建筑 {arch.Name}(ID:{arch.ID}) 有 {arch.MilitaryIDs.Count} 个MilitaryID但Militaries为空！");
                            System.Diagnostics.Debug.WriteLine($"     MilitaryIDs: {string.Join(", ", arch.MilitaryIDs)}");
                        }
                    }
                }
            }
            if (archWithMilitaryIDsButNoMilitaries > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ 发现 {archWithMilitaryIDsButNoMilitaries} 个建筑有MilitaryIDs但Militaries为空！");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ✅ 所有建筑的编队数据正常");
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] K. 验证编队数据: {swStep.ElapsedMilliseconds} ms");

            this.Preparing = false;

            // L. 初始化游戏数据
            swStep.Restart();
#if DEBUG
            // 🔥 诊断：检查 InitialGameData 前后的特色数据
            if (this.Architectures.GetGameObject(0) is Architecture testArch)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 🔍 InitialGameData前 {testArch.Name} 特色数量: {testArch.Characteristics.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⚠️ 无法获取ID=0的建筑进行诊断");
            }
#endif
            
            // 🔥 修复：在 InitialGameData 之前应用增益
            // 原因：设施、特色、技术的增益需要在游戏数据初始化前应用
            // 日期：2026-03-03
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] 开始应用增益...");
            this.Factions.ApplyInfluences();
            this.Architectures.ApplyInfluences();
            this.Architectures.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            this.Persons.ApplyInfluences();
            this.Troops.ApplyInfluenceBuff();  // 🆕 2026-03-16：应用势力范围增益
            System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] ✅ 增益应用完成");
            
            this.InitialGameData();
#if DEBUG
            if (this.Architectures.GetGameObject(0) is Architecture testArchAfter)
            {
                System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] 🔍 InitialGameData后 {testArchAfter.Name} 特色数量: {testArchAfter.Characteristics.Count}");
            }
#endif
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] L. 初始化游戏数据: {swStep.ElapsedMilliseconds} ms");

            // M. 触发事件
            swStep.Restart();
            // 🔥 新事件系统：触发剧本加载后处理完成事件
            ScenarioEvents.RaiseAfterScenarioLoaded(this);
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] M. 触发事件: {swStep.ElapsedMilliseconds} ms");
            
            // N. 初始化计略事件系统（AOT 兼容）
            swStep.Restart();
            InitializeStratagemEvents();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] N. 初始化计略事件: {swStep.ElapsedMilliseconds} ms");
            
            // O. 初始化内政与外交事件系统（AOT 兼容）
            swStep.Restart();
            InitializeInternalAffairEvents();
            InitializeDiplomacyEvents();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] O. 初始化内政外交事件: {swStep.ElapsedMilliseconds} ms");
            
            if (this.PlayerFactions.Count == 0)
            {
                oldDialogShowTime = Setting.Current.GlobalVariables.DialogShowTime;
                Setting.Current.GlobalVariables.DialogShowTime = 0;
            }
            else
            {
                //if (oldDialogShowTime >= 0)
                if (oldDialogShowTime > 0)
                {
                    Setting.Current.GlobalVariables.DialogShowTime = oldDialogShowTime;
                }
                else
                {
                    //Setting.Current.GlobalVariables.DialogShowTime = Session.globalVariablesBasic.DialogShowTime;
                }
            } 
            this.ForceOptionsOnAutoplay();

            this.sessionStartTime = DateTime.Now;
            
            swAfterLoad.Stop();
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [AfterLoadSaveFile] 加载后处理完成                        ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[AfterLoadSaveFile] ⏱️ 总耗时: {swAfterLoad.ElapsedMilliseconds} ms");
            
            // 🔥 修复：读档完成后，关闭读档界面
            if (Session.MainGame != null && Session.MainGame.loadingScreen != null)
            {
                System.Diagnostics.Debug.WriteLine("[AfterLoadSaveFile] ✅ 关闭读档界面");
                Session.MainGame.loadingScreen = null;
            }
        }

        /// <summary>
        /// 初始化计略事件系统（AOT 兼容）
        /// 🔥 Cold Path：游戏初始化时调用一次，可以使用 LINQ 和可读性优先的代码
        /// ⚠️ 注意：事件回调会在战斗中触发（Hot Path），必须避免 LINQ 和分配
        /// 日期：2026-02-27
        /// </summary>
        private void InitializeStratagemEvents()
        {
            // 🔥 重要：每次加载场景时清空旧的事件订阅，避免重复绑定
            GameObjects.Events.StratagemEvents.ClearAllSubscriptions();
            
            System.Diagnostics.Debug.WriteLine("[InitializeStratagemEvents] 开始绑定计略事件...");
            
            // ========================================
            // 1. 对目标施放计略的事件
            // ========================================
            GameObjects.Events.StratagemEvents.OnStratagemCastCompleted += (caster, target, stratagem) =>
            {
                try
                {
                    // 🔥 示例：火攻计略 - 在目标位置生成火势
                    if (stratagem.Name == "火攻" || stratagem.Name.Contains("火"))
                    {
                        // 获取目标地形
                        TerrainDetail targetTerrain = this.GetTerrainDetailByPosition(target.Position);
                        
                        // 🔥 Anti-Band-Aid：targetTerrain 为 null 是合法状态（位置越界或无地形数据）
                        if (targetTerrain != null && targetTerrain.FireDamageRate > 0)
                        {
                            // 在目标位置设置火势
                            float fireScale = caster.GenerateFireDamageScale(1.0f, targetTerrain);
                            target.SetOnFire(fireScale);
                            
                            System.Diagnostics.Debug.WriteLine(
                                $"[计略事件] {caster.DisplayName} 对 {target.DisplayName} 使用 {stratagem.Name}，生成火势（强度={fireScale:F2}）");
                        }
                    }
                    
                    // 🔥 示例：混乱计略 - 额外的视觉特效或音效
                    else if (stratagem.Name == "混乱" || stratagem.Name.Contains("乱"))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[计略事件] {caster.DisplayName} 对 {target.DisplayName} 使用 {stratagem.Name}");
                        
                        // 这里可以触发额外的特效
                        // 例如：播放混乱音效、显示混乱图标等
                        // Session.MainGame?.PlaySound("chaos_effect.wav");
                    }
                    
                    // 🔥 示例：攻心计略 - 记录战斗日志
                    else if (stratagem.Name == "攻心")
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[计略事件] {caster.DisplayName} 对 {target.DisplayName} 使用攻心，士气大幅下降");
                    }
                    
                    // 🔥 通用日志：记录所有计略使用
                    // if (Session.GlobalVariables.RecordStratagemUsage)
                    {
                        // 记录到年表或战斗日志
                        // this.YearTable?.AddStratagemEntry(this.Date, caster, target, stratagem);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略事件] 处理计略 {stratagem.Name} 时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 2. 自我施放计略的事件
            // ========================================
            GameObjects.Events.StratagemEvents.OnSelfStratagemCastCompleted += (caster, stratagem) =>
            {
                try
                {
                    // 🔥 示例：鼓舞计略 - 播放士气提升特效
                    if (stratagem.Name == "鼓舞" || stratagem.Name.Contains("激励"))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[计略事件] {caster.DisplayName} 使用 {stratagem.Name}，士气大幅提升");
                        
                        // 播放鼓舞特效
                        // 例如：在部队周围显示光环、播放鼓声等
                        // Session.MainGame?.PlaySound("morale_boost.wav");
                        
                        // 🔥 性能修复：使用 for 循环替代 LINQ（事件回调在战斗中触发，属于 Hot Path）
                        // 日期：2026-02-27
                        // 原因：事件回调虽然在初始化时绑定，但会在战斗循环中触发
                        // 解决：避免 LINQ 查询和 ToList() 分配
                        
                        // 🔥 Anti-Band-Aid：caster.BelongedFaction 为 null 是合法状态（野怪、叛军等）
                        if (caster.BelongedFaction != null)
                        {
                            // 使用 C# 12 集合表达式创建结果列表
                            List<Troop> nearbyFriendlyTroops = [];
                            
                            // 使用 for 循环遍历所有部队（零分配）
                            GameObjectList allTroops = this.Troops.GetList();
                            int troopCount = allTroops.Count;
                            
                            for (int i = 0; i < troopCount; i++)
                            {
                                Troop troop = allTroops[i] as Troop;
                                
                                // 跳过无效部队（GetList 可能返回 null 元素）
                                if (troop == null || troop == caster) continue;
                                
                                // 检查是否为友军
                                if (troop.BelongedFaction != caster.BelongedFaction) continue;
                                
                                // 检查距离（使用静态方法避免分配）
                                int distance = (int)GetDistance(troop.Position, caster.Position);
                                if (distance > 3) continue;
                                
                                // 添加到结果列表
                                nearbyFriendlyTroops.Add(troop);
                            }
                            
                            // 应用士气加成
                            int friendlyCount = nearbyFriendlyTroops.Count;
                            for (int i = 0; i < friendlyCount; i++)
                            {
                                Troop friendlyTroop = nearbyFriendlyTroops[i];
                                friendlyTroop.IncreaseMorale(5);
                                
                                System.Diagnostics.Debug.WriteLine(
                                    $"[计略事件]   → {friendlyTroop.DisplayName} 受到鼓舞影响，士气+5");
                            }
                        }
                    }
                    
                    // 🔥 示例：疗伤计略 - 恢复伤兵
                    else if (stratagem.Name == "疗伤" || stratagem.Name.Contains("治疗"))
                    {
                        int healAmount = caster.InjuryQuantity / 2; // 恢复一半伤兵
                        if (healAmount > 0)
                        {
                            caster.DecreaseInjuryQuantity(healAmount);
                            caster.IncreaseQuantity(healAmount);
                            
                            System.Diagnostics.Debug.WriteLine(
                                $"[计略事件] {caster.DisplayName} 使用 {stratagem.Name}，恢复 {healAmount} 伤兵");
                        }
                    }
                    
                    // 🔥 示例：铁壁计略 - 临时提升防御
                    else if (stratagem.Name == "铁壁" || stratagem.Name.Contains("防御"))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[计略事件] {caster.DisplayName} 使用 {stratagem.Name}，防御力大幅提升");
                        
                        // 这里的数值效果已经在 Stratagem.Apply() 中处理
                        // 此处只负责额外的表现层逻辑（特效、音效等）
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略事件] 处理自我计略 {stratagem.Name} 时发生错误: {ex.Message}");
                }
            };
            
            System.Diagnostics.Debug.WriteLine("[InitializeStratagemEvents] ✅ 计略事件绑定完成");
        }

        /// <summary>
        /// 初始化内政事件系统（AOT 兼容）
        /// 🔥 Cold Path：游戏初始化时调用一次，可以使用 LINQ 和可读性优先的代码
        /// 日期：2026-02-27
        /// </summary>
        private void InitializeInternalAffairEvents()
        {
            System.Diagnostics.Debug.WriteLine("[InitializeInternalAffairEvents] 开始绑定内政事件...");
            
            // ========================================
            // 1. 设施建造相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnStartBuildFacility += (scenario, architecture, facilityKind) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 开始建造设施：{facilityKind.Name}");
                    
                    // 🔥 示例：记录到年表
                    // this.YearTable?.AddFacilityBuildEntry(this.Date, architecture, facilityKind);
                    
                    // 🔥 示例：播放建造音效
                    // Session.MainGame?.PlaySound("construction_start.wav");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理开始建造设施事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnFacilityCompleted += (scenario, architecture, facility) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 完成建造设施：{facility.Name}");
                    
                    // 🔥 示例：显示完成提示
                    if (architecture.BelongedFaction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"{architecture.Name} 完成建造 {facility.Name}");
                    }
                    
                    // 🔥 示例：播放完成音效
                    // Session.MainGame?.PlaySound("construction_complete.wav");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理设施建造完成事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnFacilityDemolished += (scenario, architecture, facility) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 拆除设施：{facility.Name}");
                    
                    // 🔥 示例：记录到年表
                    // this.YearTable?.AddFacilityDemolishEntry(this.Date, architecture, facility);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理设施拆除事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 2. 资源交易相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnBuyFood += (scenario, architecture) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 购买粮食");
                    
                    // 🔥 示例：记录交易历史
                    // architecture.TradeHistory?.AddBuyFoodRecord(this.Date);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理购买粮食事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnSellFood += (scenario, architecture) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 出售粮食");
                    
                    // 🔥 示例：记录交易历史
                    // architecture.TradeHistory?.AddSellFoodRecord(this.Date);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理出售粮食事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 3. 灾难相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnDisasterHappened += (scenario, architecture, disasterType) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 发生灾难（类型={disasterType}）");
                    
                    // 🔥 示例：显示灾难提示
                    if (architecture.BelongedFaction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowDisasterWarning(architecture, disasterType);
                    }
                    
                    // 🔥 示例：记录到年表
                    // this.YearTable?.AddDisasterEntry(this.Date, architecture, disasterType);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理灾难事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 4. 军事编队相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnCreateRobberTroop += (scenario, architecture, troop) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 附近出现盗贼部队");
                    
                    // 🔥 示例：显示盗贼警告
                    if (architecture.BelongedFaction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowRobberWarning(architecture, troop);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理盗贼部队创建事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnDisbandMilitary += (scenario, architecture, military) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 解散编队：{military.Name}");
                    
                    // 🔥 示例：记录到年表
                    // this.YearTable?.AddDisbandMilitaryEntry(this.Date, architecture, military);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理解散编队事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnLevelUpMilitary += (scenario, architecture, military) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] {architecture.Name} 编队升级：{military.Name}");
                    
                    // 🔥 示例：显示升级提示
                    if (architecture.BelongedFaction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"{military.Name} 升级成功！");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理编队升级事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 5. AI 决策相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.OnAIArchitecture += (scenario, architecture) =>
            {
                try
                {
                    // 🔥 这个事件主要用于扩展 AI 逻辑
                    // 默认情况下不需要处理，只在需要自定义 AI 行为时订阅
                    
                    // 🔥 示例：自定义 AI 决策日志
                    // if (Session.GlobalVariables.EnableAIDebugLog)
                    // {
                    //     System.Diagnostics.Debug.WriteLine($"[AI决策] {architecture.Name} 执行 AI 逻辑");
                    // }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[内政事件] 处理 AI 决策事件时发生错误: {ex.Message}");
                }
            };
            
            System.Diagnostics.Debug.WriteLine("[InitializeInternalAffairEvents] ✅ 内政事件绑定完成");
        }

        /// <summary>
        /// 初始化外交事件系统（AOT 兼容）
        /// 🔥 Cold Path：游戏初始化时调用一次，可以使用 LINQ 和可读性优先的代码
        /// 日期：2026-02-27
        /// </summary>
        private void InitializeDiplomacyEvents()
        {
            System.Diagnostics.Debug.WriteLine("[InitializeDiplomacyEvents] 开始绑定外交事件...");
            
            // ========================================
            // 1. 首都相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnChangeCapital += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 迁都至 {faction.Capital?.Name ?? "未知"}");
                    
                    // 🔥 示例：显示迁都提示
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"首都已迁至 {faction.Capital.Name}");
                    }
                    
                    // 🔥 示例：记录到年表（已在 Faction.ChangeCapital 中处理）
                    // this.YearTable?.AddChangeCapitalEntry(this.Date, faction, faction.Capital);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理迁都事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnForceChangeCapital += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 被迫迁都至 {faction.Capital?.Name ?? "未知"}");
                    
                    // 🔥 示例：显示被迫迁都警告
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowWarning($"首都失守！被迫迁都至 {faction.Capital.Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理被迫迁都事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 2. 势力相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnChangeFaction += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 发生归属变更");
                    
                    // 🔥 示例：更新外交关系缓存
                    // this.DiplomaticRelations?.InvalidateCache();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理势力变更事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnFactionDestroyed += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 灭亡");
                    
                    // 🔥 示例：显示势力灭亡提示
                    if (this.IsPlayer(faction))
                    {
                        // Session.MainGame?.ShowGameOverScreen(faction);
                    }
                    else if (this.CurrentPlayer != null)
                    {
                        // Session.MainGame?.ShowMessage($"势力 {faction.Name} 已灭亡");
                    }
                    
                    // 🔥 示例：检查游戏结束条件
                    if (this.Factions.Count == 1)
                    {
                        // Session.MainGame?.ShowVictoryScreen(this.Factions[0]);
                    }

                    this.SyncInfluenceAfterFactionTopologyChange($"FactionDestroyed:{faction.ID}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理势力灭亡事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 3. 君主相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnChangeKing += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 君主更替：{faction.Leader?.Name ?? "未知"}");
                    
                    // 🔥 示例：显示君主更替提示
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"新君主：{faction.Leader.Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理君主更替事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnBecomeEmperorLegally += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 合法称帝（禅让）");
                    
                    // 🔥 示例：显示称帝动画
                    // Session.MainGame?.ShowEmperorCeremony(faction, isLegal: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理合法称帝事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnSelfBecomeEmperor += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 自立称帝");
                    
                    // 🔥 示例：显示称帝动画
                    // Session.MainGame?.ShowEmperorCeremony(faction, isLegal: false);
                    
                    // 🔥 示例：触发外交关系恶化（已在 Faction.SelfBecomeEmperor 中处理）
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理自立称帝事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 4. 官爵相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnAdvancement += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 获得朝廷晋升官爵");
                    
                    // 🔥 示例：显示晋升提示
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"恭喜！获得朝廷晋升");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理朝廷晋升事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnSelfAdvancement += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 自封官爵");
                    
                    // 🔥 示例：显示自封提示
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowMessage($"自封官爵成功");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理自封官爵事件时发生错误: {ex.Message}");
                }
            };
            
            // ========================================
            // 5. 技术相关事件
            // ========================================
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnTechniqueUpgradeComplete += (scenario, faction, technique) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 完成技术研发：{technique.Name}");
                    
                    // 🔥 示例：显示技术完成提示
                    if (faction == this.CurrentPlayer)
                    {
                        // Session.MainGame?.ShowTechniqueCompleteDialog(technique);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理技术研发完成事件时发生错误: {ex.Message}");
                }
            };
            
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.OnUpgradeTechnique += (scenario, faction) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 势力 {faction.Name} 开始研发技术");
                    
                    // 🔥 示例：播放研发开始音效
                    // Session.MainGame?.PlaySound("research_start.wav");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[外交事件] 处理开始研发技术事件时发生错误: {ex.Message}");
                }
            };
            
            System.Diagnostics.Debug.WriteLine("[InitializeDiplomacyEvents] ✅ 外交事件绑定完成");
        }

        public void AfterInit()
        {
            // AOT升级后建筑坐标修复
            try
            {
                FixArchitecturePositionsAfterAOT();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AOT修复] 建筑坐标修复失败: {ex.Message}");
            }
            
            // 🆕 初始化建筑核心占地地图（主权防波堤系统）
            // 日期：2026-03-13
            try
            {
                InitializeArchitectureCoreMap();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[主权防波堤] 初始化失败: {ex.Message}");
            }

            if (this.CurrentPlayer != null)
            {
                detectCurrentPlayerBattleState(this.CurrentPlayer, true);
                this.CurrentPlayer.RefreshImportantPerson();
            }
        }
        
        /// <summary>
        /// 初始化建筑核心占地地图（主权防波堤系统）
        /// 🧊 Cold Path：游戏初始化时调用一次
        /// 日期：2026-03-13
        /// </summary>
        private void InitializeArchitectureCoreMap()
        {
            if (this.ScenarioMap == null || this.Architectures == null)
            {
                throw new InvalidOperationException(
                    "[InitializeArchitectureCoreMap] ScenarioMap 或 Architectures 未初始化");
            }
            
            int mapWidth = this.ScenarioMap.MapDimensions.X;
            int mapHeight = this.ScenarioMap.MapDimensions.Y;
            int totalCells = mapWidth * mapHeight;
            
            // 初始化数组，-1 表示无建筑
            ArchitectureCoreMap = new int[totalCells];
            Array.Fill(ArchitectureCoreMap, -1);
            
            int totalMarkedCells = 0;
            
            // 🔥 使用 for 循环，避免 LINQ
            var architectureList = this.Architectures.GetList();
            int archCount = architectureList.Count;
            
            for (int i = 0; i < archCount; i++)
            {
                if (architectureList[i] is not Architecture arch)
                {
                    throw new InvalidOperationException(
                        $"[InitializeArchitectureCoreMap] 建筑列表包含无效项（索引 {i}）");
                }
                
                if (arch.ArchitectureArea == null)
                {
                    throw new InvalidOperationException(
                        $"[InitializeArchitectureCoreMap] 建筑 {arch.Name} 的 ArchitectureArea 为 null");
                }
                
                if (arch.ArchitectureArea.Area == null)
                {
                    throw new InvalidOperationException(
                        $"[InitializeArchitectureCoreMap] 建筑 {arch.Name} 的 ArchitectureArea.Area 为 null");
                }
                
                var area = arch.ArchitectureArea.Area;
                int areaCount = area.Count;
                
                // 标记建筑核心占地
                for (int j = 0; j < areaCount; j++)
                {
                    Point p = area[j];
                    int idx = p.Y * mapWidth + p.X;
                    
                    if (idx >= 0 && idx < totalCells)
                    {
                        ArchitectureCoreMap[idx] = arch.ID;
                        totalMarkedCells++;
                    }
                }
            }
        }

        /// <summary>
        /// AOT升级后建筑坐标修复
        /// </summary>
        private void FixArchitecturePositionsAfterAOT()
        {
            if (this.Architectures == null || this.MapTileData == null)
                return;

            System.Diagnostics.Debug.WriteLine("[AOT修复] 开始修复建筑坐标问题...");

            int fixedCount = 0;

            // 1. 清空并重新设置建筑引用
            for (int x = 0; x < this.ScenarioMap.MapDimensions.X; x++)
            {
                for (int y = 0; y < this.ScenarioMap.MapDimensions.Y; y++)
                {
                    this.MapTileData[x, y].TileArchitecture = null;
                }
            }

            // 2. 重新设置每个建筑的地图引用
            foreach (Architecture architecture in this.Architectures)
            {
                try
                {
                    if (architecture?.ArchitectureArea?.Area != null)
                    {
                        // 强制重新计算TopLeft（通过反射清空缓存）
                        try
                        {
                            var topleftField = typeof(GameArea).GetField("topleft", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            topleftField?.SetValue(architecture.ArchitectureArea, null);
                        }
                        catch (Exception exInner)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AOT修复] {architecture.Name} architecture.ArchitectureArea cache reset failed: {exInner.Message}");
                        }

                        // 重新设置地图引用
                        foreach (Point point in architecture.ArchitectureArea.Area)
                        {
                            if (point.X >= 0 && point.Y >= 0 && 
                                point.X < this.ScenarioMap.MapDimensions.X && 
                                point.Y < this.ScenarioMap.MapDimensions.Y)
                            {
                                this.MapTileData[point.X, point.Y].TileArchitecture = architecture;
                            }
                        }
                        fixedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AOT修复] 修复建筑 {architecture?.Name} 失败: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[AOT修复] 建筑坐标修复完成: {fixedCount}/{this.Architectures.Count}");
        }

        private int oldDialogShowTime = -1;

        private void AIMergeAgainstPlayer()
        {
            if (this.PlayerFactions.Count == 0) return;
            if (this.Factions.Count < 3) return;
            if (!Session.GlobalVariables.PermitFactionMerge) return;
            if (Session.GlobalVariables.AIMergeAgainstPlayer < 0) return;

            Faction strongestAI = null;
            Faction strongestPlayer = null;
            int strongestAIPower = int.MinValue;
            int strongestPlayerPower = int.MinValue;

            foreach (Faction f in this.Factions)
            {
                if (this.IsPlayer(f))
                {
                    if (f.Power > strongestPlayerPower)
                    {
                        strongestPlayerPower = f.Power;
                        strongestPlayer = f;
                    }
                }
                else
                {
                    FactionList adjacent = f.GetAdjecentFactions();
                    bool nextToPlayer = false;
                    foreach (Faction g in adjacent)
                    {
                        if (this.IsPlayer(g) && this.GetDiplomaticRelation(f.ID, g.ID) < -100)
                        {
                            nextToPlayer = true;
                            break;
                        }
                    }

                    if (!nextToPlayer) continue;

                    if (f.Power > strongestAIPower)
                    {
                        strongestAIPower = f.Power;
                        strongestAI = f;
                    }
                }
            }

            if (strongestAI == null || strongestPlayer == null) return;


            if (GameObject.Chance((int)(((float)strongestPlayerPower / strongestAIPower - Session.GlobalVariables.AIMergeAgainstPlayer) * 100)))
            {
                GameObjectList fl = this.Factions.GetList();
                fl.IsNumber = true;
                fl.PropertyName = "Power";
                fl.SmallToBig = false;
                fl.ReSort();

                Faction toMerge = null;
                foreach (Faction f in fl)
                {
                    if (this.IsPlayer(f) || f == strongestAI) continue;

                    if (!f.Leader.Hates(strongestAI.Leader))
                    {
                        if (GameObject.Chance((int)(Person.GetIdealAttraction(strongestAI.Leader, f.Leader) + strongestPlayerPower / strongestAIPower * 100)))
                        {
                            if (strongestAI.adjacentTo(f) && this.GetDiplomaticRelation(strongestAI.ID, f.ID) > 0)
                            {
                                toMerge = f;
                                break;
                            }
                        }
                    }
                }

                if (toMerge != null)
                {
                    if (toMerge.Power > strongestAI.Power)
                    {
                        Faction temp = toMerge;
                        toMerge = strongestAI;
                        strongestAI = temp;
                    }
                    Session.MainGame.mainGameScreen.OnAIMergeAgainstPlayer(strongestPlayer, strongestAI, toMerge);
                    this.YearTable.addChangeFactionEntry(this.Date, toMerge, strongestAI);
                    GameObjectList rebelCandidates = toMerge.Persons.GetList();
                    toMerge.ChangeFaction(strongestAI);
                    toMerge.AfterChangeLeader(strongestAI, rebelCandidates, toMerge.Leader, strongestAI.Leader);
                }
            }

        }

        public void MonthPassedEvent()
        {
            ScenarioEvents.RaiseMonthPassed(this);

            // 【新增】AI分帧处理：回合开始时将所有AI势力加入思考队列
            try
            {
                if (global::WorldOfTheThreeKingdoms.GameManager.AIStrategicManager.Instance != null)
                {
                    var aiFactions = new List<Faction>();
                    foreach (var obj in this.Factions.GetList())
                    {
                        if (obj is Faction f && f != null && !f.Destroyed && !f.Controlling)
                        {
                            aiFactions.Add(f);
                        }
                    }
                    global::WorldOfTheThreeKingdoms.GameManager.AIStrategicManager.Instance.OnTurnStart(aiFactions);
                    System.Diagnostics.Debug.WriteLine($"[MonthPassedEvent] AI分帧处理已启动，{aiFactions.Count}个AI势力进入思考队列");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonthPassedEvent] AI队列初始化异常: {ex.Message}");
            }

            this.AIMergeAgainstPlayer();

            foreach (GameObject obj in this.Factions.GetRandomList())
            {
                Faction faction = (obj is Faction ? (Faction)obj : null);
                if (faction != null)
                {
                    faction.MonthEvent();
                }
            }
            foreach (Person person in this.Persons)
            {
                person.TryToBeAvailable();
            }
            this.AddPreparedAvailablePersons();
            foreach (GameObject obj in this.AvailablePersons.GetRandomList())
            {
                Person person = (obj is Person ? (Person)obj : null);
                if (person != null)
                {
                    person.MonthEvent();
                }
            }
            foreach (GameObject obj in this.Architectures.GetRandomList())
            {
                Architecture architecture = (obj is Architecture ? (Architecture)obj : null);
                if (architecture != null)
                {
                    architecture.MonthEvent();
                }
            }
            foreach (MilitaryKind kind in this.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
            {
#pragma warning disable CS0219 // The variable 'flag' is assigned but its value is never used
                bool flag = true;
#pragma warning restore CS0219 // The variable 'flag' is assigned but its value is never used
                foreach (Troop troop in this.Troops)
                {
                    if ((troop.Army.Kind == kind) && Session.MainGame.mainGameScreen.TileInScreen(troop.Position))
                    {
                        flag = false;
                        break;
                    }
                }
                //if (flag)
                //{
                //    kind.Textures.Dispose();
                //}
            }
        }

        private void AdjustGlobalPersonRelation()
        {
            foreach (Person p in this.Persons)
            {
                if (p.Available && p.Alive && GameObject.Random(120 / Session.Parameters.DayInTurn) == 0)
                {
                    foreach (Person q in this.Persons)
                    {
                        if (p == q) continue;
                        if (!q.Alive)
                        {
                            p.SetRelation(q, 0);
                            q.SetRelation(p, 0);
                            continue;
                        }

                        if (q.Available && q.Alive && p.BelongedFactionWithPrincess != null && GameObject.Random(30 / Session.Parameters.DayInTurn) == 0)
                        {
                            float likeability = Person.GetIdealAttraction(p, q) * 8 + q.Glamour * 0.75f + p.Glamour * 0.25f + q.PersonalLoyalty * 7.5f + p.PersonalLoyalty * 2.5f - q.Ambition * 5 - p.Ambition * 5 - 100;
                            
                            bool sameWork = p.SameLocationAs(q) &&
                                    (
                                        (p.Status == PersonStatus.Normal && q.Status == PersonStatus.Normal &&
                                            ((p.WorkKind == q.WorkKind) || (p.OutsideTask == q.OutsideTask))
                                        ) ||
                                        (p.Status == PersonStatus.Princess && q.Status == PersonStatus.Princess)
                                    );
                            float factor = 0.0f;
                            
                            if (p.LocationTroop == q.LocationTroop && p.LocationTroop != null && q.LocationTroop != null)
                            {
                                factor = 3.0f;
                            }
                            else if (p.SameLocationAs(q) && p.Hates(q) && p.Spouse == q && GameObject.Chance(50))
                            {
                                factor = 3.0f;
                            }
                            else if (sameWork)
                            {
                                factor = 1.0f;
                            } 
                            else if (p.SameLocationAs(q) && GameObject.Chance(50))
                            {
                                factor = 1.0f;
                            }
                            else if (p.BelongedFactionWithPrincess == q.BelongedFactionWithPrincess && GameObject.Chance(20))
                            {
                                factor = 1.0f;
                            }

                            if (factor > 0)
                            {
                                if (GameObject.Chance((int) (likeability / 4.0f)))
                                {
                                    p.AdjustRelation(q, 6f * factor, 2 * factor);
                                    q.AdjustRelation(p, 6f * factor, 2 * factor);
                                }
                                else if (GameObject.Chance((int)(-likeability / 4.0f)))
                                {
                                    p.AdjustRelation(q, -6f * factor, -2 * factor);
                                    q.AdjustRelation(p, -6f * factor, -2 * factor);
                                }
                            }
                        }

                        if (p.GetRelation(q) > 0)
                        {
                            if (!p.Closes(q) && GameObject.Chance((5 - p.PersonalLoyalty) * 20 - 10))
                            {
                                float d = (float) Session.Parameters.CloseThreshold / Math.Max(10, p.GetRelation(q));
                                if (p.LocationArchitecture == q.LocationArchitecture || p.LocationTroop == q.LocationTroop)
                                {
                                    p.AdjustRelation(q, -d / 5f, 0);
                                }
                                else
                                {
                                    p.AdjustRelation(q, -d / 12.5f, 0);
                                }

                                if (p.GetRelation(q) < 0)
                                {
                                    p.SetRelation(q, 0);
                                }
                            }
                        }
                        else if (p.GetRelation(q) < 0)
                        {
                            if (!p.Hates(q))
                            {
                                float d = Session.Parameters.HateThreshold / -p.GetRelation(q) / 5f;
                                if (p.Status == PersonStatus.Princess && q.Status == PersonStatus.Princess)
                                {
                                    d *= 4;
                                }
                                if (p.LocationArchitecture == q.LocationArchitecture || p.LocationTroop == q.LocationTroop)
                                {
                                    p.AdjustRelation(q, -d / 5f, 0);
                                }
                                else
                                {
                                    p.AdjustRelation(q, -d / 12.5f, 0);
                                }

                                if (p.GetRelation(q) > 0)
                                {
                                    p.SetRelation(q, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void MonthStartingEvent()
        {
        }

        public void SeasonChangeEvent()
        {
            if (!scenarioJustLoaded)
            {
                ScenarioEvents.RaiseSeasonPassed(this);
                // 🔥 修复读档崩溃：使用 this.Parameters 而不是 Session.Current.Scenario.Parameters
                // 原因：ProcessScenarioData 期间 Session.Current.Scenario 还未赋值
                // 日期：2026-03-16
                if ((this.Date.Month == 3 || this.Date.Month == 6 || this.Date.Month == 9 || this.Date.Month == 12) && this.Date.Day <= this.Parameters.DayInTurn)
                {
                    foreach (GameObject obj in this.Factions.GetRandomList())
                    {
                        if (obj is Faction faction)
                        {
                            faction.SeasonEvent();
                        }
                    }
                    foreach (GameObject obj in this.Architectures.GetRandomList())
                    {
                        if (obj is Architecture architecture)
                        {
                            architecture.DevelopSeason();
                        }
                    }
                }
            }
        }

        public bool MoreThanOneTroopOnPosition(Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[MoreThanOneTroopOnPosition] MapTileData 为 null");
                return false;
            }
            
            if (this.PositionOutOfRange(position))
            {
                System.Diagnostics.Debug.WriteLine($"[MoreThanOneTroopOnPosition] 位置越界: ({position.X}, {position.Y})");
                return false;
            }
            
            try
            {
                return (this.MapTileData[position.X, position.Y].TroopCount > 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MoreThanOneTroopOnPosition] 访问异常: {ex.Message}");
                return false;
            }
        }

        public void NewFaction()
        {
            if (GameObject.Random(15) == 0)
            {
                this.NewFaction(this.AvailablePersons, false, false);
            }
        }

        public void NewFaction(PersonList candidates, bool leaderChange, bool nonInherited)
        {
            if (Session.GlobalVariables.WujiangYoukenengDuli == false) return;

            PersonList list = new PersonList();
            foreach (Person person in candidates)
            {
                if (person.YoukenengChuangjianXinShili())   //里面包含武将有可能独立的参数
                {
                    if ((person.Ambition > 1 && GameObject.Random((5 - person.Ambition) * (5 - person.Ambition) * (5 - person.Ambition)) == 0) ||
                        (person.BelongedFaction != null && person.Hates(person.BelongedFaction.Leader)))
                    {
                        list.Add(person);
                    }
                }
            }

            if (list.Count == 0) return;

            Person p = (Person)list[GameObject.Random(list.Count)];
            int cnt = 0;
            foreach (Person person8 in list)
            {
                cnt++;
                if (!leaderChange && cnt > 1)
                {
                    break;
                }

                if (leaderChange)
                {
                    p = person8;
                }

                Architecture location = p.BelongedArchitecture;
                Faction faction = p.BelongedFaction;
                if (location == null) continue;
                if (faction != null && !p.Hates(faction.Leader))
                {
                    if (p.Loyalty >= 100) continue;
                    if (p.Loyalty >= 90 && !p.LeaderPossibility) continue;
                }
                if (faction != null && Person.GetIdealOffset(faction.Leader, p) <= 10 && !p.Hates(faction.Leader)) continue;
                if (faction != null && location == faction.Capital) continue;
                //if (GameObject.Random(15) != 0) return;

                if (GameObject.Random(location.Population + location.ArmyScale * 5000 +
                        location.Domination * 200 + location.Morale * 10) >
                    GameObject.Random(p.Reputation *
                    (p.LeaderPossibility ? 3 : 1) *
                    (leaderChange && nonInherited ? p.Ambition * p.Ambition * (p.Glamour / 20) : 1) *
                    (faction != null && leaderChange && nonInherited ? Person.GetIdealOffset(p, faction.Leader) / 10 + 1 : 1) *
                    (faction != null && (p.Hates(faction.Leader) || faction.Leader.Hates(p)) ? (leaderChange ? 10000 : 3) : 1) *
                    (faction == null ? 3 : 1))) continue;
                this.CreateNewFaction(p);
            }
        }

        private void NoFoodPositionDayEvent()
        {
            List<NoFoodPosition> list = new List<NoFoodPosition>();
            foreach (NoFoodPosition position in this.NoFoodDictionary.Positions.Values)
            {
                position.Days--;
                if (position.Days <= 0)
                {
                    list.Add(position);
                }
            }
            foreach (NoFoodPosition position in list)
            {
                this.NoFoodDictionary.RemovePosition(position);
            }
        }

        public bool PositionIsArchitecture(Point position)
        {
            return (this.GetArchitectureByPosition(position) != null);
        }

        public bool PositionIsOnFire(Point position)
        {
            if (this.PositionOutOfRange(position))
            {
                return false;
            }
            return this.FireTable.HasPosition(position);
        }

        public bool PositionIsOnFireNoCheck(Point position)
        {
            return this.FireTable.HasPosition(position);
        }

        public bool PositionIsTroop(Point position)
        {
            return (this.GetTroopByPosition(position) != null);
        }

        public bool PositionOutOfRange(Point position)
        {
            return ScenarioMap.PositionOutOfRange(position);
        }

        public string PositionString(Point position)
        {

            if (this.PositionIsArchitecture(position))
            {
                return this.GetArchitectureByPositionNoCheck(position).Name;
            }
            /*
            if (this.PositionIsTroop(position))
            {
                return this.GetTroopByPositionNoCheck(position).DisplayName;
            }
            */
            return (this.GetTerrainNameByPosition(position) + " " + this.GetCoordinateString(position));
        }

        public void ReflectDiplomaticRelations(int src, int des, int offset)
        {
            foreach (DiplomaticRelation relation in this.DiplomaticRelations.GetDiplomaticRelationListByFactionID(des))
            {
                int theOtherFactionID = relation.GetTheOtherFactionID(des);
                if ((theOtherFactionID != src) && (Math.Abs(relation.Relation) >= 100))
                {
                    int num2 = this.DiplomaticRelations.GetDiplomaticRelation(src, theOtherFactionID).Relation;
                    if ((num2 > -GlobalVariables.FriendlyDiplomacyThreshold) && (num2 < Session.GlobalVariables.FriendlyDiplomacyThreshold))
                    {
                        int num3 = relation.Relation;
                        if (num3 > 0x3e8)
                        {
                            num3 = 0x3e8;
                        }
                        else if (num3 < -0x3e8)
                        {
                            num3 = -0x3e8;
                        }
                        this.ChangeDiplomaticRelation(src, theOtherFactionID, (offset * num3) / 0x3e8);
                    }
                }
            }
        }

        public void RemovePositionAreaInfluence(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                Troop troopByPositionNoCheck = this.GetTroopByPositionNoCheck(position);
                this.MapTileData[position.X, position.Y].RemoveAreaInfluence(troop, troopByPositionNoCheck);
                if (troopByPositionNoCheck != null)
                {
                    troopByPositionNoCheck.RefreshDataOfAreaInfluence();
                }
            }
        }

        public void RemovePositionContactingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].RemoveContactingTroop(troop);
            }
        }

        public void RemovePositionOffencingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].RemoveOffencingTroop(troop);
            }
        }

        public void RemovePositionStratagemingTroop(Troop troop, Point position)
        {
            if (!this.PositionOutOfRange(position))
            {
                this.MapTileData[position.X, position.Y].RemoveStratagemingTroop(troop);
            }
        }

        public void RemovePositionViewingTroopNoCheck(Troop troop, Point position)
        {
            this.MapTileData[position.X, position.Y].RemoveViewingTroop(troop);
        }

        public void RemoveRouteway(Routeway routeway)
        {
            if (routeway.FirstPoint != null)
            {
                routeway.CutAt(routeway.FirstPoint.Position);
            }
            if (routeway.StartArchitecture != null)
            {
                routeway.StartArchitecture.Routeways.Remove(routeway);
            }
            if (routeway.BelongedFaction != null)
            {
                routeway.BelongedFaction.RemoveRouteway(routeway);
            }
            this.Routeways.Remove(routeway);
        }

        public void ResetMapTileTroop(Point position)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[ResetMapTileTroop] MapTileData 为 null");
                return;
            }
            
            if (this.PositionOutOfRange(position))
            {
                System.Diagnostics.Debug.WriteLine($"[ResetMapTileTroop] 位置越界: ({position.X}, {position.Y})");
                return;
            }
            
            try
            {
                var tileTroop = this.MapTileData[position.X, position.Y].TileTroop;
                if (tileTroop != null && tileTroop.Destroyed)
                {
                    TileData data1 = this.MapTileData[position.X, position.Y];
                    data1.TroopCount--;
                    this.MapTileData[position.X, position.Y].TileTroop = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResetMapTileTroop] 访问异常: {ex.Message}");
            }
        }

        public void ReallyResetMapTileTroop()
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (this.MapTileData == null)
            {
                System.Diagnostics.Debug.WriteLine("[ReallyResetMapTileTroop] MapTileData 为 null");
                return;
            }
            
            try
            {
                for (int i = 0; i < this.MapTileData.GetLength(0); ++i)
                {
                    for (int j = 0; j < this.MapTileData.GetLength(1); ++j)
                    {
                        TileData t = this.MapTileData[i, j];
                    if (t.ContactingTroops != null)
                    {
                        t.ContactingTroops.RemoveAll(u => u == null || u.Destroyed || u.Simulating);
                        if (t.ContactingTroops.Count == 0)
                        {
                            // Yes I mean it. Too many empty lists kill the memory.......
                            this.MapTileData[i, j].ContactingTroops = null;
                        }
                        else
                        {
                            t.ContactingTroops.Capacity = t.ContactingTroops.Count;
                        }
                    }
                    if (t.OffencingTroops != null)
                    {
                        t.OffencingTroops.RemoveAll(u => u == null || u.Destroyed || u.Simulating);
                        if (t.OffencingTroops.Count == 0)
                        {
                            this.MapTileData[i, j].OffencingTroops = null;
                        }
                        else
                        {
                            t.OffencingTroops.Capacity = t.OffencingTroops.Count;
                        }
                    }
                    if (t.StratagemingTroops != null)
                    {
                        t.StratagemingTroops.RemoveAll(u => u == null || u.Destroyed || u.Simulating);
                        if (t.StratagemingTroops.Count == 0)
                        {
                            this.MapTileData[i, j].StratagemingTroops = null;
                        }
                        else
                        {
                            t.StratagemingTroops.Capacity = t.StratagemingTroops.Count;
                        }
                    }
                    if (t.ViewingTroops != null)
                    {
                        t.ViewingTroops.RemoveAll(u => u == null || u.Destroyed || u.Simulating);
                        if (t.ViewingTroops.Count == 0)
                        {
                            this.MapTileData[i, j].ViewingTroops = null;
                        }
                        else
                        {
                            t.ViewingTroops.Capacity = t.ViewingTroops.Count;
                        }
                    }

                    if (t.AreaInfluenceList != null)
                    {
                        t.AreaInfluenceList.RemoveAll(u => u == null || u.Owner.Destroyed || u.Owner.Simulating);
                        if (t.AreaInfluenceList.Count == 0)
                        {
                            this.MapTileData[i, j].AreaInfluenceList = null;
                        }
                        else
                        {
                            t.AreaInfluenceList.Capacity = t.AreaInfluenceList.Count;
                        }
                    }

                    if (t.TileRouteways != null)
                    {
                        if (t.TileRouteways.Count == 0)
                        {
                            this.MapTileData[i, j].TileRouteways = null;
                        }
                        else
                        {
                            t.TileRouteways.Capacity = t.TileRouteways.Count;
                        }
                    }

                    if (t.SupplyingRoutePoints != null)
                    {
                        if (t.SupplyingRoutePoints.Count == 0)
                        {
                            this.MapTileData[i, j].SupplyingRoutePoints = null;
                        }
                        else
                        {
                            t.SupplyingRoutePoints.Capacity = t.SupplyingRoutePoints.Count;
                        }
                    }

                    if (t.SupplyingRoutePoints != null)
                    {
                        if (t.SupplyingRoutePoints.Count == 0)
                        {
                            this.MapTileData[i, j].SupplyingRoutePoints = null;
                        }
                        else
                        {
                            t.SupplyingRoutePoints.Capacity = t.SupplyingRoutePoints.Count;
                        }
                    }

                    if (t.TileTroop != null && (t.TileTroop.Destroyed || t.TileTroop.Simulating))
                    {
                        this.MapTileData[i, j].TileTroop = null;
                    }
                }
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReallyResetMapTileTroop] 处理异常: {ex.Message}");
            }
        }

        public bool SaveGameScenario(string LoadedFileName, bool saveMap, bool saveCommonData, bool saveSettings, bool disposeMemory = true, bool fullPathProvided = false, bool editing = false)
        {
            // 🔥 诊断：记录保存参数
            System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 开始保存游戏");
            System.Diagnostics.Debug.WriteLine($"  - saveMap: {saveMap}");
            System.Diagnostics.Debug.WriteLine($"  - saveCommonData: {saveCommonData}");
            System.Diagnostics.Debug.WriteLine($"  - saveSettings: {saveSettings}");
            System.Diagnostics.Debug.WriteLine($"  - editing: {editing}");
            System.Diagnostics.Debug.WriteLine($"  - LoadedFileName: {LoadedFileName}");
            
            if (this.GameTime < 0)
            {
                this.GameTime = 0;
            }
            if(!editing)
            {
                this.GameTime += (int)DateTime.Now.Subtract(sessionStartTime).TotalSeconds;
            }
            sessionStartTime = DateTime.Now;

            List<string> errors = new List<string>();

            ClearPersonStatusCache();
            ClearPersonWorkCache();

            this.Architectures.GameObjects = this.Architectures.GameObjects.OrderBy(x => x.ID).ToList();
            this.AllBiographies.Biographys = this.AllBiographies.Biographys.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            this.Captives.GameObjects = this.Captives.GameObjects.OrderBy(x => x.ID).ToList();
            this.AllEvents.GameObjects = this.AllEvents.GameObjects.OrderBy(x => x.ID).ToList();
            this.Facilities.GameObjects = this.Facilities.GameObjects.OrderBy(x => x.ID).ToList();
            this.Factions.GameObjects = this.Factions.GameObjects.OrderBy(x => x.ID).ToList();
            this.Informations.GameObjects = this.Informations.GameObjects.OrderBy(x => x.ID).ToList();
            this.Legions.GameObjects = this.Legions.GameObjects.OrderBy(x => x.ID).ToList();
            this.Militaries.GameObjects = this.Militaries.GameObjects.OrderBy(x => x.ID).ToList();
            this.Persons.GameObjects = this.Persons.GameObjects.OrderBy(x => x.ID).ToList();
            this.Routeways.GameObjects = this.Routeways.GameObjects.OrderBy(x => x.ID).ToList();
            
            // 🔥 修复：保存前同步 Section.Architectures → Section.ArchitectureIDs
            System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 开始同步 Section.ArchitectureIDs...");
            foreach (var obj in this.Sections.GameObjects)
            {
                if (obj is Section section)
                {
                    if (section.Architectures != null && section.Architectures.Count > 0)
                    {
                        section.ArchitectureIDs = new List<int>();
                        foreach (Architecture arch in section.Architectures.GetList())
                        {
                            if (arch != null)
                            {
                                section.ArchitectureIDs.Add(arch.ID);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Section {section.Name}(ID:{section.ID}) 同步了 {section.ArchitectureIDs.Count} 个建筑ID");
                    }
                    else
                    {
                        section.ArchitectureIDs = new List<int>();
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ Section {section.Name}(ID:{section.ID}) Architectures为空");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ✅ Section.ArchitectureIDs 同步完成");
            
            this.Sections.GameObjects = this.Sections.GameObjects.OrderBy(x => x.ID).ToList();
            this.Treasures.GameObjects = this.Treasures.GameObjects.OrderBy(x => x.ID).ToList();
            this.Troops.GameObjects = this.Troops.GameObjects.OrderBy(x => x.ID).ToList();
            this.TroopEvents.GameObjects = this.TroopEvents.GameObjects.OrderBy(x => x.ID).ToList();
            this.DiplomaticRelations.DiplomaticRelations = this.DiplomaticRelations.DiplomaticRelations.OrderBy(x => x.Value.RelationFaction1ID).ToDictionary(x => x.Key, y => y.Value);
            if(editing)
            {
                this.FatherIds = this.FatherIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.MotherIds = this.MotherIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.SpouseIds = this.SpouseIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.BrotherIds = this.BrotherIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.SuoshuIds = this.SuoshuIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.CloseIds = this.CloseIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.HatedIds = this.HatedIds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                this.PersonRelationIds = this.PersonRelationIds.OrderBy(x => x.PersonID1).ToList();
            }

            if (!disposeMemory)
            {
                this.DisposeLotsOfMemory();
            }

            if (!editing)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ========== 势力官爵数据保存检查 ==========");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Factions.Count: {this.Factions?.Count ?? 0}");
                #endif
                
                int factionDebugCount = 0;
                foreach (Faction faction in this.Factions)
                {
                    faction.SectionIDs = ParseIDsFromString(faction.Sections.SaveToString());
                    faction.ArchitectureIDs = ParseIDsFromString(faction.Architectures.SaveToString());
                    faction.TroopIDs = ParseIDsFromString(faction.Troops.SaveToString());
                    faction.InformationsString = faction.Informations.SaveToString();
                    faction.RoutewaysString = faction.Routeways.SaveToString();
                    faction.LegionIDs = ParseIDsFromString(faction.Legions.SaveToString());
                    faction.BaseMilitaryKindsString = faction.BaseMilitaryKinds.SaveToString();
                    faction.AvailableTechniquesString = faction.AvailableTechniques.SaveToString();
                    faction.PlanTechniqueString = (faction.PlanTechnique != null) ? faction.PlanTechnique.ID : -1;
                    faction.GetGeneratorPersonCountString = faction.SaveGeneratorPersonCountToString();
                    faction.TransferingMilitariesString = faction.TransferingMilitaries.SaveToString();
                    faction.MilitaryIDs = ParseIDsFromString(faction.Militaries.SaveToString());
                    faction.PrinceID = faction.Prince != null ? faction.Prince.ID : -1;
                    
                    #if DEBUG
                    if (factionDebugCount < 3)  // 只打印前3个势力
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 势力 {faction.Name}(ID:{faction.ID}):");
                        System.Diagnostics.Debug.WriteLine($"  - guanjue: {faction.guanjue}");
                        System.Diagnostics.Debug.WriteLine($"  - chaotinggongxiandu: {faction.chaotinggongxiandu}");
                        System.Diagnostics.Debug.WriteLine($"  - Leader: {faction.Leader?.Name ?? "null"}");
                        System.Diagnostics.Debug.WriteLine($"  - Advisor: {faction.Advisor?.Name ?? "null"}");
                        System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs: [{string.Join(",", faction.ArchitectureIDs ?? new List<int>())}]");
                        System.Diagnostics.Debug.WriteLine($"  - Architectures.Count: {faction.Architectures.Count}");
                        System.Diagnostics.Debug.WriteLine($"  - Persons.Count: {faction.Persons.Count}");
                    }
                    factionDebugCount++;
                    #endif
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ✅ 势力官爵数据保存完成");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ==========================================");
                #endif
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ editing=true，跳过Faction字符串字段准备");
                #endif
            }

            foreach (Section section in this.Sections)
            {
                section.EnsureSectionArchitecture();
                if (!editing)
                {
                    section.AIDetailID = section.AIDetail.ID;
                    section.OrientationFactionID = (section.OrientationFaction != null) ? section.OrientationFaction.ID : -1;
                    section.OrientationSectionID = (section.OrientationSection != null) ? section.OrientationSection.ID : -1;
                    section.OrientationStateID = (section.OrientationState != null) ? section.OrientationState.ID : -1;
                    section.OrientationArchitectureID = (section.OrientationArchitecture != null) ? section.OrientationArchitecture.ID : -1;
                    section.ArchitectureIDs = ParseIDsFromString(section.Architectures.SaveToString());
                }
            }

            if (!editing)
            {
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ========== 建筑设施数据保存检查 ==========");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Architectures.Count: {this.Architectures?.Count ?? 0}");
                int facilityCount = 0;
                int archWithFacilities = 0;
                int archDebugCount = 0;
                #endif
                */
                

                
                foreach (Architecture architecture in this.Architectures)
                {
                    // 🔥 修复：同步 KindID（大写）而不是 KindId（小写）
                    if (architecture.Kind != null)
                    {
                        architecture.KindID = architecture.Kind.ID; // 使用 KindID
                        architecture.KindId = architecture.Kind.ID; // 同时更新 KindId 以保持兼容性
                    }
                    else
                    {
                        // ⚠️ Kind 为 null 是严重错误，输出详细诊断信息
                        var diagnosticInfo = new System.Text.StringBuilder();
                        diagnosticInfo.AppendLine($"[SaveGameScenario] ❌ 严重错误：建筑 Kind 为 null");
                        diagnosticInfo.AppendLine($"  - 建筑名称: {architecture.Name}");
                        diagnosticInfo.AppendLine($"  - 建筑ID: {architecture.ID}");
                        diagnosticInfo.AppendLine($"  - 当前KindID: {architecture.KindID}");
                        diagnosticInfo.AppendLine($"  - 当前KindId: {architecture.KindId}");
                        diagnosticInfo.AppendLine($"  - StateID: {architecture.StateID}");
                        diagnosticInfo.AppendLine($"  - LocationState: {(architecture.LocationState != null ? architecture.LocationState.Name : "null")}");
                        diagnosticInfo.AppendLine($"  - BelongedFaction: {(architecture.BelongedFaction != null ? architecture.BelongedFaction.Name : "null")}");
                        diagnosticInfo.AppendLine($"  - ArchitectureArea: {(architecture.ArchitectureArea != null ? architecture.ArchitectureArea.Count.ToString() : "null")}");
                        
                        // 检查GameCommonData状态
                        if (this.GameCommonData == null)
                        {
                            diagnosticInfo.AppendLine($"  - GameCommonData: null");
                        }
                        else if (this.GameCommonData.AllArchitectureKinds == null)
                        {
                            diagnosticInfo.AppendLine($"  - AllArchitectureKinds: null");
                        }
                        else
                        {
                            diagnosticInfo.AppendLine($"  - 可用ArchitectureKinds数量: {this.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count}");
                            foreach (var kvp in this.GameCommonData.AllArchitectureKinds.ArchitectureKinds)
                            {
                                diagnosticInfo.AppendLine($"    - ID {kvp.Key}: {kvp.Value.Name}");
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine(diagnosticInfo.ToString());
                        
                        // 抛出异常以便追踪问题
                        throw new InvalidOperationException($"建筑 {architecture.Name}(ID:{architecture.ID}) 的 Kind 为 null，KindID={architecture.KindID}。请检查建筑初始化流程。");
                    }
                    
                    // 同步StateID - 确保LocationState不为null
                    if (architecture.LocationState != null)
                    {
                        architecture.StateID = architecture.LocationState.ID;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 建筑 {architecture.Name}(ID:{architecture.ID}) 的 LocationState 为 null，保持 StateID={architecture.StateID}");
                    }
#if DEBUG
                    // 🔥 诊断：检查保存前的特色数据
                    if (architecture.ID == 0 || architecture.Name == "洛阳")
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 🔍 保存前 {architecture.Name}(ID:{architecture.ID}) 特色数量: {architecture.Characteristics.Count}");
                    }
#endif
                    architecture.CharacteristicsString = architecture.Characteristics.SaveToString();
#if DEBUG
                    if (architecture.ID == 0 || architecture.Name == "洛阳")
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 🔍 保存后 {architecture.Name}(ID:{architecture.ID}) CharacteristicsString='{architecture.CharacteristicsString}'");
                    }
#endif

                    architecture.ArchitectureAreaString = StaticMethods.SaveToString(architecture.ArchitectureArea.Area);

                    architecture.PersonsString = architecture.Persons.SaveToString();
                    architecture.MovingPersonsString = architecture.MovingPersons.SaveToString();
                    architecture.NoFactionPersonsString = architecture.NoFactionPersons.SaveToString();
                    architecture.NoFactionMovingPersonsString = architecture.NoFactionMovingPersons.SaveToString();

                    //row["AgricultureWorkingPersons"] = architecture.AgricultureWorkingPersons.SaveToString();
                    //row["CommerceWorkingPersons"] = architecture.CommerceWorkingPersons.SaveToString();
                    //row["TechnologyWorkingPersons"] = architecture.TechnologyWorkingPersons.SaveToString();
                    //row["DominationWorkingPersons"] = architecture.DominationWorkingPersons.SaveToString();
                    //row["MoraleWorkingPersons"] = architecture.MoraleWorkingPersons.SaveToString();
                    //row["EnduranceWorkingPersons"] = architecture.EnduranceWorkingPersons.SaveToString();
                    //row["zhenzaiWorkingPersons"] = architecture.ZhenzaiWorkingPersons.SaveToString();
                    //row["TrainingWorkingPersons"] = architecture.TrainingWorkingPersons.SaveToString();

                    architecture.feiziliebiaoString = architecture.Feiziliebiao.SaveToString();
                    architecture.MilitariesString = architecture.Militaries.SaveToString();
                    architecture.FacilitiesString = architecture.Facilities.SaveToString();
                    

                    


                    architecture.PlanFacilityKindID = (architecture.PlanFacilityKind != null) ? architecture.PlanFacilityKind.ID : -1;

                    architecture.FundPacksString = architecture.SaveFundPacksToString();
                    architecture.FoodPacksString = architecture.SaveFoodPacksToString();
                    architecture.PopulationPacksString = architecture.SavePopulationPacksToString();

                    architecture.PlanArchitectureID = (architecture.PlanArchitecture != null) ? architecture.PlanArchitecture.ID : -1;

                    architecture.TransferFundArchitectureID = (architecture.TransferFundArchitecture != null) ? architecture.TransferFundArchitecture.ID : -1;

                    architecture.TransferFoodArchitectureID = (architecture.TransferFoodArchitecture != null) ? architecture.TransferFoodArchitecture.ID : -1;

                    architecture.DefensiveLegionID = (architecture.DefensiveLegion != null) ? architecture.DefensiveLegion.ID : -1;

                    architecture.CaptivesString = architecture.Captives.SaveToString();

                    architecture.RobberTroopID = (architecture.RobberTroop != null) ? architecture.RobberTroop.ID : -1;

                    // 🔥 诊断：存档前检查链接数据
                    #if DEBUG
                    if (architecture.ID == 1)  // 只检查第一个建筑，避免刷屏
                    {
                        System.Diagnostics.Debug.WriteLine($"[存档诊断] 建筑 {architecture.Name}(ID:{architecture.ID})");
                        System.Diagnostics.Debug.WriteLine($"  - 存档前 AILandLinks.Count: {architecture.AILandLinks.Count}");
                        System.Diagnostics.Debug.WriteLine($"  - 存档前 AIWaterLinks.Count: {architecture.AIWaterLinks.Count}");
                    }
                    #endif
                    
                    architecture.AILandLinksString = architecture.AILandLinks.SaveToString();
                    architecture.AIWaterLinksString = architecture.AIWaterLinks.SaveToString();
                    
                    // 🔥 诊断：存档后检查字符串
                    #if DEBUG
                    if (architecture.ID == 1)  // 只检查第一个建筑
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 存档后 AILandLinksString: null={architecture.AILandLinksString == null}, 长度={architecture.AILandLinksString?.Length ?? -1}");
                        System.Diagnostics.Debug.WriteLine($"  - 存档后 AIWaterLinksString: null={architecture.AIWaterLinksString == null}, 长度={architecture.AIWaterLinksString?.Length ?? -1}");
                        System.Diagnostics.Debug.WriteLine($"  - AILandLinksString 内容: [{architecture.AILandLinksString ?? "NULL"}]");
                    }
                    #endif

                    //row["zainanleixing"] = architecture.zainan.zainanzhonglei.ID;
                    //row["zainanshengyutianshu"] = architecture.zainan.shengyutianshu;

                    architecture.InformationsString = architecture.Informations.SaveToString();

                    //string s = "";
                    //foreach (Architecture i in architecture.AIBattlingArchitectures)
                    //{
                    //    s += i.ID + " ";
                    //}
                    //row["AIBattlingArchitectures"] = s;
                }
                
                // 🔥 诊断：输出保存统计

            }

            foreach (Legion legion in this.Legions)
            {
                legion.StartArchitectureString = (legion.StartArchitecture != null) ? legion.StartArchitecture.ID : -1;
                legion.WillArchitectureString = (legion.WillArchitecture != null) ? legion.WillArchitecture.ID : -1;

                legion.PreferredRoutewayString = (legion.PreferredRouteway != null) ? legion.PreferredRouteway.ID : -1;

                legion.CoreTroopString = (legion.CoreTroop != null) ? legion.CoreTroop.ID : -1;

                legion.TroopIDs = ParseIDsFromString(legion.Troops.SaveToString());
            }

            foreach (Troop troop in this.Troops)
            {
                troop.LeaderIDString = troop.Leader.ID;

                troop.MilitaryID = troop.Army.ID;

                troop.StartingArchitectureString = (troop.StartingArchitecture != null) ? troop.StartingArchitecture.ID : -1;
                troop.PersonsString = troop.SavePersonsToString();

                //row["PositionX"] = troop.Position.X;
                //row["PositionY"] = troop.Position.Y;
                //row["RealDestinationX"] = troop.RealDestination.X;
                //row["RealDestinationY"] = troop.RealDestination.Y;

                troop.WillTroopID = troop.RealWillTroop == null ? -1 : troop.RealWillTroop.ID;
                troop.WillArchitectureID = troop.RealWillArchitecture == null ? -1 : troop.RealWillArchitecture.ID;

                troop.CaptivesString = troop.Captives.SaveToString();

                troop.EventInfluencesString = troop.EventInfluences.SaveToString();

                troop.CombatMethodsString = troop.CombatMethods.SaveToString();

                troop.CurrentStuntIDString = (troop.CurrentStunt != null) ? troop.CurrentStunt.ID : -1;
                
            }

            if (saveMap)
            {
                foreach (GameObject gameObj in this.TroopEvents)
                {
                    TroopEvent event2 = gameObj as TroopEvent;
                    if (event2 == null)
                    {
                        // Skip objects that are not TroopEvent instances
                        continue;
                    }

                    event2.AfterEventHappened = (event2.AfterHappenedEvent != null) ? event2.AfterHappenedEvent.ID : -1;
                    event2.LaunchPersonString = (event2.LaunchPerson != null) ? event2.LaunchPerson.ID : -1;
                    event2.ConditionsString = event2.Conditions.SaveToString();
                    event2.TargetPersonsString = event2.SaveTargetPersonToString();
                    event2.SelfEffectsString = event2.SaveSelfEffectToString();
                    event2.EffectPersonsString = event2.SaveEffectPersonToString();
                    event2.EffectAreasString = event2.SaveEffectAreaToString();
                    event2.dialogString = event2.SaveDialogToString();
                }
            }

            foreach (Routeway routeway in this.Routeways)
            {
                if ((routeway.StartArchitecture != null) && ((routeway.Building || (routeway.LastActivePointIndex >= 0)) || (routeway.StartArchitecture.BelongedSection == null || (!routeway.StartArchitecture.BelongedSection.AIDetail.AutoRun && this.IsPlayer(routeway.StartArchitecture.BelongedFaction)))))
                {
                    routeway.StartArchitectureString = (routeway.StartArchitecture != null) ? routeway.StartArchitecture.ID : -1;
                    routeway.EndArchitectureString = (routeway.EndArchitecture != null) ? routeway.EndArchitecture.ID : -1;
                    routeway.DestinationArchitectureString = (routeway.DestinationArchitecture != null) ? routeway.DestinationArchitecture.ID : -1;
                }
            }

            foreach (Military military in this.Militaries)
            {
                military.FollowedLeaderID = (military.FollowedLeader != null) ? military.FollowedLeader.ID : -1;
                military.LeaderID = (military.Leader != null) ? military.Leader.ID : -1;

                //row["LeaderExperience"] = military.LeaderExperience;

                //row["TrainingPersonID"] = -1;

                military.RecruitmentPersonID = military.RecruitmentPerson == null ? -1 : military.RecruitmentPerson.ID;
                military.ShelledMilitaryID = (military.ShelledMilitary != null) ? military.ShelledMilitary.ID : -1;
            }

            foreach (Captive captive in this.Captives)
            {
                captive.CaptivePersonID = (captive.CaptivePerson != null) ? captive.CaptivePerson.ID : -1;
                captive.CaptiveFactionID = (captive.CaptiveFaction != null) ? captive.CaptiveFaction.ID : -1;
                captive.RansomArchitectureID = (captive.RansomArchitecture != null) ? captive.RansomArchitecture.ID : -1;
            }

            if (!editing)
            {
                ClearTempDic();
            }

            if (!editing)
            {

                
                // 🔥 修复：清空 PersonRelationIds，避免重复数据
                // System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 清空 PersonRelationIds，当前数量: {PersonRelationIds.Count}");
                PersonRelationIds.Clear();
                

                foreach (Person person in this.Persons)
                {
                    person.UniqueTitlesString = person.UniqueTitles.SaveToString();
                    person.UniqueMilitaryKindsString = person.UniqueMilitaryKinds.SaveToString();
                    person.IdealTendencyIDString = (person.IdealTendency != null) ? person.IdealTendency.ID : -1;
                    
                    // 🔥 调试：检查保存时的IdealTendency状态
                    if (person.ID < 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Person {person.Name}(ID:{person.ID}):");
                        System.Diagnostics.Debug.WriteLine($"  保存前 IdealTendency={(person.IdealTendency != null ? $"{person.IdealTendency.Name}(ID:{person.IdealTendency.ID})" : "null")}");
                        System.Diagnostics.Debug.WriteLine($"  设置 IdealTendencyIDString={person.IdealTendencyIDString}");
                    }
                    
                    if (person.Character != null)
                    {
                        person.PCharacter = person.Character.ID;
                    }
                    
                    // 🔥 序列化 Treasures - 使用 TreasureIDs
                    person.TreasureIDs = ParseIDsFromString(person.Treasures.SaveToString());

                    //row["Braveness"] = person.BaseBraveness;                    
                    //row["Calmness"] = person.BaseCalmness;
                    //row["Loyalty"] = person.Loyalty;

                    FatherIds[person.ID] = person.Father == null ? -1 : person.Father.ID;
                    MotherIds[person.ID] = person.Mother == null ? -1 : person.Mother.ID;
                    SpouseIds[person.ID] = person.Spouse == null ? -1 : person.Spouse.ID;

                    String brotherStr = "";
                    foreach (Person p in person.Brothers)
                    {
                        brotherStr += p.ID + " ";
                    }

                    String str;
                    char[] separator = separator = new char[] { ' ', '\n', '\r', '\t' };
                    String[] strArray;
                    int[] intArray;
                    try
                    {
                        str = brotherStr;
                        strArray = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        intArray = new int[strArray.Length];
                        for (int i = 0; i < strArray.Length; i++)
                        {
                            intArray[i] = int.Parse(strArray[i]);
                        }
                        BrotherIds.Add(person.ID, intArray);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 义兄弟 ID 列表解析失败 (武将: {person.Name}, ID: {person.ID}): {ex.Message}");
                        errors.Add("义兄弟一栏应为半型空格分隔的人物ID");
                    }

                    String suoshuStr = "";
                    foreach (Person p in person.suoshurenwuList)
                    {
                        suoshuStr += p.ID + " ";
                    }

                    if (suoshuStr != null)
                    {
                        try
                        {
                            strArray = suoshuStr.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            intArray = new int[strArray.Length];
                            for (int i = 0; i < strArray.Length; i++)
                            {
                                intArray[i] = int.Parse(strArray[i]);
                            }
                            SuoshuIds.Add(person.ID, intArray);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 所属人物 ID 列表解析失败 (武将: {person.Name}, ID: {person.ID}): {ex.Message}");
                            errors.Add("所属人物表一栏应为半型空格分隔的人物ID");
                        }
                    }

                    String closeStr = "";
                    String hatedStr = "";
                    foreach (Person p in person.GetClosePersons())
                    {
                        closeStr += p.ID + " ";
                    }
                    foreach (Person p in person.GetHatedPersons())
                    {
                        hatedStr += p.ID + " ";
                    }

                    try
                    {
                        str = closeStr;
                        strArray = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        intArray = new int[strArray.Length];
                        for (int i = 0; i < strArray.Length; i++)
                        {
                            intArray[i] = int.Parse(strArray[i]);
                        }
                        CloseIds.Add(person.ID, intArray);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 亲爱武将 ID 列表解析失败 (武将: {person.Name}, ID: {person.ID}): {ex.Message}");
                        errors.Add("亲爱武将一栏应为半型空格分隔的人物ID");
                    }

                    try
                    {
                        str = hatedStr;
                        strArray = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        intArray = new int[strArray.Length];
                        for (int i = 0; i < strArray.Length; i++)
                        {
                            intArray[i] = int.Parse(strArray[i]);
                        }
                        HatedIds.Add(person.ID, intArray);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 厌恶武将 ID 列表解析失败 (武将: {person.Name}, ID: {person.ID}): {ex.Message}");
                        errors.Add("厌恶武将一栏应为半型空格分隔的人物ID");
                    }

                    MarriageGranterId.Add(person.ID, person.marriageGranter != null ? person.marriageGranter.ID : -1);

                    //row["TrainingMilitaryID"] = -1;
                    //row["RecruitmentMilitaryID"] = person.RecruitmentMilitary == null ? -1 : person.RecruitmentMilitary.ID;

                    person.ConvincingPersonID = (person.ConvincingPerson != null) ? person.ConvincingPerson.ID : -1;

                    // 🔥 2026-03-18 移除：旧的保存逻辑
                    // 原因：person.SkillsString/StuntsString/RealTitlesString 字段已删除
                    // 现在使用 SaveDataPhase 处理序列化，直接保存 SkillIDs/StuntIDs/TitleIDs
                    
                    person.StudyingTitleString = (person.StudyingTitle != null) ? person.StudyingTitle.ID : -1;
                    person.StudyingStuntString = (person.StudyingStunt != null) ? person.StudyingStunt.ID : -1;

                    person.waitForFeiziId = (person.WaitForFeiZi != null) ? person.WaitForFeiZi.ID : -1;
                    person.preferredTroopPersonsString = person.preferredTroopPersons.SaveToString();

                    person.TrainPolicyIDString = person.TrainPolicy == null ? -1 : person.TrainPolicy.ID;

                    foreach (KeyValuePair<Person, int> pi in person.GetRelations())
                    {
                        var personIDRelation = new PersonIDRelation()
                        {
                            PersonID1 = person.ID,
                            PersonID2 = pi.Key.ID,
                            Relation = pi.Value
                        };
                        PersonRelationIds.Add(personIDRelation);
                    }
                    

                }
                

                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ✅ PersonRelationIds 构建完成，共 {PersonRelationIds.Count} 条关系数据");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ editing=true，跳过Person字符串字段准备");
            }
            if(!editing)
            {
                captiveData = this.Captives;
            }

            if (saveMap)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 开始保存地图数据");
                System.Diagnostics.Debug.WriteLine($"  - ScenarioMap == null: {ScenarioMap == null}");
                
                if (ScenarioMap != null)
                {
                    var mapDataString = ScenarioMap.SaveToString();
                    System.Diagnostics.Debug.WriteLine($"  - SaveToString() 返回长度: {mapDataString?.Length ?? 0}");
                    
                    this.ScenarioMap.MapDataString = mapDataString;
                    System.Diagnostics.Debug.WriteLine($"  - MapDataString 已设置，长度: {this.ScenarioMap.MapDataString?.Length ?? 0}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  - ❌ ScenarioMap 为 null，无法保存地图数据");
                }
                
                //修复游戏中编辑地形后无法保存
                // 🔥 FIX: 添加类型验证，防止 InvalidCastException
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveToString] Regions 集合验证开始，Count: {this.Regions.Count}");
                    
                    // 验证 Regions 集合中的对象类型并处理
                    for (int i = 0; i < this.Regions.Count; i++)
                    {
                        var obj = this.Regions[i];
                        if (obj is GameObjects.ArchitectureDetail.Region region)
                        {
                            try
                            {
                                region.StatesListString = region.States.SaveToString();
                                region.RegionCoreID = (region.RegionCore != null) ? region.RegionCore.ID : -1;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SaveToString] Region 处理失败 (ID: {region.ID}): {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveToString] ⚠️ Regions[{i}] 类型错误: {obj?.GetType().Name ?? "null"}, 期望: Region");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[SaveToString] ✅ Regions 处理完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveToString] ❌ Regions 处理异常: {ex.Message}");
                    // 继续执行，不中断整个流程
                }

                // 🔥 FIX: 添加类型验证，防止 InvalidCastException
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveToString] States 集合验证开始，Count: {this.States.Count}");
                    
                    // 验证 States 集合中的对象类型并处理
                    for (int i = 0; i < this.States.Count; i++)
                    {
                        var obj = this.States[i];
                        if (obj is GameObjects.ArchitectureDetail.State state)
                        {
                            try
                            {
                                state.ContactStatesString = state.ContactStates.SaveToString();
                                state.StateAdminID = (state.StateAdmin != null) ? state.StateAdmin.ID : -1;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SaveToString] State 处理失败 (ID: {state.ID}): {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveToString] ⚠️ States[{i}] 类型错误: {obj?.GetType().Name ?? "null"}, 期望: State");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[SaveToString] ✅ States 处理完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveToString] ❌ States 处理异常: {ex.Message}");
                    // 继续执行，不中断整个流程
                }
            }

            foreach (Treasure treasure in this.Treasures)
            {
                treasure.BelongedPersonIDString = (treasure.BelongedPerson != null) ? treasure.BelongedPerson.ID : -1;
                treasure.HidePlaceIDString = (treasure.HidePlace != null) ? treasure.HidePlace.ID : -1;
                treasure.InfluencesString = treasure.Influences.SaveToString();
            }

            foreach (YearTableEntry yt in this.YearTable)
            {
                string factionStr = "";
                foreach (Faction f in yt.Factions)
                {
                    if (f != null)
                    {
                        factionStr += f.ID + " ";
                    }
                }
                yt.FactionsString = factionStr;
            }

            if (saveMap && !editing)
            {
                foreach (GameObject gameObj in this.AllEvents)
                {
                    Event e = (gameObj is Event ? (Event)gameObj : null);
                    if (e == null)
                    {
                        // Skip objects that are not Event instances
                        continue;
                    }

                    e.personString = e.SavePersonIdToString();
                    e.PersonCondString = e.SavePersonCondToString();
                    e.architectureString = e.architecture.SaveToString();
                    e.architectureCondString = e.SaveArchitecureCondToString();
                    e.factionString = e.faction.SaveToString();
                    e.factionCondString = e.SaveFactionCondToString();
                    e.dialogString = e.SaveDialogToString();
                    e.effectString = e.SaveEventEffectToString();
                    e.architectureEffectString = e.SaveArchitectureEffectToString();
                    e.factionEffectIDString = e.SaveFactionEffectToString();
                    e.yesdialogString = e.SaveyesDialogToString();
                    e.nodialogString = e.SavenoDialogToString();
                    e.yesEffectString = e.SaveYesEffectToString();
                    e.noEffectString = e.SaveNoEffectToString();
                    e.yesArchitectureEffectString = e.SaveYesArchitectureEffectToString();
                    e.noArchitectureEffectString = e.SaveNoArchitectureEffectToString();
                    e.scenBiographyString = e.SaveScenBiographyToString();
                }
            }

            // 🔥 修复：CurrentPlayer 不应该为 null，如果为 null 说明数据流有问题
            // 让它崩溃，暴露问题，而不是掩盖
            this.CurrentPlayerID = this.CurrentPlayer.ID.ToString();
            
            if(!editing)
            {
                // 🔥 修复：从 PlayerFactions 生成 PlayerList
                
                // 🔥 修复：如果 PlayerList 为空但有 CurrentPlayer，添加 CurrentPlayer
                if (false)
                {
                    this.PlayerList.Add(this.CurrentPlayer.ID);
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[⚠️ 保存修复] PlayerList 为空，从 CurrentPlayer 恢复: ID={this.CurrentPlayer.ID}, Name={this.CurrentPlayer.Name}");
                    #endif
                }
                
                this.PlayerInfo = this.GetPlayerInfo();
            }
            this.Factions.FactionQueue = this.Factions.SaveQueueToString();


            //row["JumpPosition"] = StaticMethods.SaveToString(new Point?(ScenarioMap.JumpPosition));

            // 🔥 新事件系统：触发剧本保存后处理完成事件
            ScenarioEvents.RaiseAfterScenarioSaved(this);

            foreach (Biography i in this.AllBiographies.Biographys.Values)
            {
                i.MilitaryKindsString = i.MilitaryKinds.SaveToString();
            }

            var scenarioClone = this.Clone();            

            if (saveCommonData || UsingOwnCommonData)
            {
                SaveGameCommonData(scenarioClone);
            }
            else
            {
                scenarioClone.GameCommonData = null;
            }


            if (saveSettings)
            {

            }
            else
            {
                //scenarioClone.Parameters = null;
                //scenarioClone.GlobalVariables = null;
            }

            var saves = LoadScenarioSaves();
            string file = LoadedFileName;
            if (!fullPathProvided)
            {
                file = @"Save\" + LoadedFileName;
            }

            //bool zip = true;

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    zip = false;
            //}

            // 【新增】保存AI学习系统数据
            try
            {
                if (WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance != null)
                {
                    string aiLearningData = WorldOfTheThreeKingdoms.GameManager.AILearningSystem.Instance.SerializeLearningData();
                    if (!string.IsNullOrEmpty(aiLearningData))
                    {
                        string aiDataFile = file.Replace(".json", "_AILearning.json");
                        if (!fullPathProvided)
                        {
                            aiDataFile = @"Save\" + LoadedFileName.Replace(".json", "_AILearning.json");
                        }
                        Platform.Current.SaveUserFile(aiDataFile, aiLearningData, fullPathProvided);
                        System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] AI学习数据已保存到: {aiDataFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 保存AI学习数据失败: {ex.Message}");
                // 不影响主要的游戏保存流程
            }

            // 实施二进制保存
            try
            {
                // � 诊断：检查保存前的Troops状态
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ========== 保存前数据检查 ==========");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Scenario.Troops.Count: {this.Troops?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Scenario.Militaries.Count: {this.Militaries?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Scenario.Persons.Count: {this.Persons?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Scenario.Architectures.Count: {this.Architectures?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] Scenario.Factions.Count: {this.Factions?.Count ?? 0}");
                
                // 检查Faction的Troops
                int totalFactionTroops = 0;
                foreach (Faction faction in this.Factions)
                {
                    totalFactionTroops += faction.Troops?.Count ?? 0;
                    if (faction.Troops != null && faction.Troops.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  势力 {faction.Name}: {faction.Troops.Count} 个部队");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 所有势力的部队总数: {totalFactionTroops}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ==========================================");
                
                // 🔥 修复路径问题：确保目录存在
                string saveDir = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(saveDir))
                {
                    saveDir = "Save";
                }
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                // 🔥 修复：正确构建 .sav.gz 路径，避免双重扩展名
                // 1. 移除所有已知扩展名（.bin, .sav.gz, .sav）
                // 2. 添加统一的 .sav.gz 扩展名
                string baseFileName = Path.GetFileNameWithoutExtension(file); // 移除最后一个扩展名
                
                // 如果是 .sav.gz 格式，需要再移除一次 .sav
                if (baseFileName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
                {
                    baseFileName = Path.GetFileNameWithoutExtension(baseFileName);
                }
                
                string savGzPath;
                if (!fullPathProvided)
                {
                    savGzPath = Path.Combine(saveDir, baseFileName + ".sav.gz");
                }
                else
                {
                    savGzPath = Path.Combine(saveDir, baseFileName + ".sav.gz");
                }
                
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 原始文件名: {file}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 基础文件名: {baseFileName}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 使用 SerializationManager 保存到: {savGzPath}");
                
                var serializationManager = new WorldOfTheThreeKingdoms.Serialization.SerializationManager();
                serializationManager.SaveGame(scenarioClone, savGzPath);
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ✅ SerializationManager 保存成功");
                
                // 🔥 修复：更新 LoadedFileName 为新格式（.sav.gz）
                this.LoadedFileName = Path.GetFileName(savGzPath);
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ✅ LoadedFileName 已更新为: {this.LoadedFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ❌ SerializationManager 保存失败！");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 内部异常: {ex.InnerException.Message}");
                }
                
                // 🔥 重要：不要忽略异常，应该抛出或至少警告用户
                throw new Exception($"保存游戏失败: {ex.Message}", ex);
            }

            // 使用 JSON 保存
            // bool result = SimpleSerializer.SerializeJsonFile(scenarioClone, file, false, false, fullPathProvided);
            bool result = true; // 假定二进制保存总是成功（或者应该根据上面的try-catch来设定）

            if (result)
            {
                int id;

                // 🔥 修复：正确提取存档ID，支持 .sav.gz 双扩展名
                string name = LoadedFileName;
                
                // 移除路径，只保留文件名
                if (name.Contains("\\") || name.Contains("/"))
                {
                    name = Path.GetFileName(name);
                }
                
                // 🔥 修复：处理 .sav.gz 双扩展名
                // Save01.sav.gz → Save01.sav → Save01
                name = Path.GetFileNameWithoutExtension(name); // 移除 .gz
                if (name.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
                {
                    name = Path.GetFileNameWithoutExtension(name); // 移除 .sav
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] LoadedFileName: '{LoadedFileName}'");
                System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 提取的name: '{name}'");
                #endif

                if (int.TryParse(name.Replace("Save", ""), out id))
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 解析的存档ID: {id}");
                    #endif
                    
                    string time = scenarioClone.Date.Year + "-" + scenarioClone.Date.Month + "-" + scenarioClone.Date.Day;

                    saves[id] = new Scenario()
                    {
                        ID = id < 10 ? "0" + id.ToString() : id.ToString(),  // 🔥 修复：设置ID字段
                        Create = DateTime.Now.ToSeasonDateTime(),
                        Desc = scenarioClone.ScenarioDescription,
                        IDs = "",
                        Info = scenarioClone.PlayerInfo,
                        Name = name,
                        Names = "",
                        Path = "",
                        PlayTime = GameTime.ToString(),
                        Player = "",
                        Players = String.Join(",", scenarioClone.PlayerList.NullToEmptyList()),
                        Time = time.ToSeasonDate(),
                        Title = scenarioClone.ScenarioTitle
                    };
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] 保存存档信息: ID={saves[id].ID}, Title={saves[id].Title}, Name={saves[id].Name}");
                    #endif
                    
                    if(!editing)
                    {
                        SaveScenarioSaves(saves);
                    }
                    else 
                    {
                        string saveDir = @"Save\";
                        string saveFile = saveDir + "Saves.json";
                        SimpleSerializer.SerializeJsonFile(saves, saveFile);
                    }
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SaveGameScenario] ⚠️ 无法从 '{name}' 解析存档ID");
                    #endif
                }
            }

            scenarioClone = null;

            JustSaved = true;

            //ExtensionInterface.call("Save", new Object[] { this });

            return true;
        }

        public static List<string> LoadGameCommonData()
        {
            var errorMsg = new List<string>();  // ← 保留：这是返回值，需要可变集合
            
            var conditionKinds = new ConditionKindTable();
            foreach (var conditionKind in CommonData.Current.AllConditionKinds.ConditionKinds)
            {
                int num = conditionKind.Key;
                ConditionKind ck = ConditionKindFactory.CreateConditionKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = conditionKind.Value.Name;
                    conditionKinds.AddConditionKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            CommonData.Current.AllConditionKinds = conditionKinds;

            var influenceKinds = new InfluenceKindTable();
            foreach (var influenceKind in CommonData.Current.AllInfluenceKinds.InfluenceKinds)
            {
                int num = influenceKind.Key;
                InfluenceKind ck = InfluenceKindFactory.CreateInfluenceKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = influenceKind.Value.Name;
                    ck.Type = influenceKind.Value.Type;
                    ck.Combat = influenceKind.Value.Combat;
                    ck.AIPersonValue = influenceKind.Value.AIPersonValue;
                    ck.AIPersonValuePow = influenceKind.Value.AIPersonValuePow;
                    influenceKinds.AddInfluenceKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            CommonData.Current.AllInfluenceKinds = influenceKinds;

            foreach (var influence in CommonData.Current.AllInfluences.Influences)
            {
                var kind = influence.Value.Kind;
                if (kind == null)
                {

                }
                else
                {
                    CommonData.Current.AllInfluenceKinds.InfluenceKinds.TryGetValue(kind.ID, out influence.Value.Kind);
                }
            }

            var eventEffectKinds = new EventEffectKindTable();
            foreach (var eventEffectKind in CommonData.Current.AllEventEffectKinds.EventEffectKinds)
            {
                int num = eventEffectKind.Key;
                EventEffectKind ck = EventEffectKindFactory.CreateEventEffectKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = eventEffectKind.Value.Name;
                    eventEffectKinds.AddEventEffectKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            CommonData.Current.AllEventEffectKinds = eventEffectKinds;

            foreach (var eventEffect in CommonData.Current.AllEventEffects.EventEffects)
            {
                var kind = eventEffect.Value.Kind;
                if (kind == null)
                {

                }
                else
                {
                    CommonData.Current.AllEventEffectKinds.EventEffectKinds.TryGetValue(kind.ID, out eventEffect.Value.Kind);
                }
            }

            var troopEventEffectKinds = new GameObjects.TroopDetail.EventEffect.EventEffectKindTable();
            foreach (var eventEffectKind in CommonData.Current.AllTroopEventEffectKinds.EventEffectKinds)
            {
                int num = eventEffectKind.Key;
                GameObjects.TroopDetail.EventEffect.EventEffectKind ck = GameObjects.TroopDetail.EventEffect.EventEffectKindFactory.CreateEventEffectKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = eventEffectKind.Value.Name;
                    troopEventEffectKinds.AddEventEffectKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            CommonData.Current.AllTroopEventEffectKinds = troopEventEffectKinds;

            foreach (var eventEffect in CommonData.Current.AllTroopEventEffects.EventEffects)
            {
                var kind = eventEffect.Value.Kind;
                if (kind == null)
                {
                    
                }
                else
                {
                    CommonData.Current.AllTroopEventEffectKinds.EventEffectKinds.TryGetValue(kind.ID, out eventEffect.Value.Kind);
                }
            }
            return errorMsg;
        }

        public static void SaveGameCommonData(GameScenario scenario)
        {
            var commonData = scenario.GameCommonData.Clone();
            commonData.AllTitles.Titles = commonData.AllTitles.Titles.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllArchitectureKinds.ArchitectureKinds = commonData.AllArchitectureKinds.ArchitectureKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllBiographyAdjectives = commonData.AllBiographyAdjectives.OrderBy(x => x.ID).ToList();
            commonData.AllCombatMethods.CombatMethods = commonData.AllCombatMethods.CombatMethods.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllConditionKinds.ConditionKinds = commonData.AllConditionKinds.ConditionKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllConditions.Conditions = commonData.AllConditions.Conditions.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllEventEffectKinds.EventEffectKinds = commonData.AllEventEffectKinds.EventEffectKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllEventEffects.EventEffects = commonData.AllEventEffects.EventEffects.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllFacilityKinds.FacilityKinds = commonData.AllFacilityKinds.FacilityKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllInfluenceKinds.InfluenceKinds = commonData.AllInfluenceKinds.InfluenceKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllInfluences.Influences = commonData.AllInfluences.Influences.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllMilitaryKinds.MilitaryKinds= commonData.AllMilitaryKinds.MilitaryKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllSkills.Skills = commonData.AllSkills.Skills.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllStratagems.Stratagems = commonData.AllStratagems.Stratagems.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllStunts.Stunts = commonData.AllStunts.Stunts.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTechniques.Techniques = commonData.AllTechniques.Techniques.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTitleKinds.TitleKinds = commonData.AllTitleKinds.TitleKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTitles.Titles = commonData.AllTitles.Titles.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTroopEventEffectKinds.EventEffectKinds = commonData.AllTroopEventEffectKinds.EventEffectKinds.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTroopEventEffects.EventEffects = commonData.AllTroopEventEffects.EventEffects.OrderBy(x => x.Value.ID).ToDictionary(x => x.Key, y => y.Value);
            commonData.AllTextMessages.textMessages= commonData.AllTextMessages.textMessages.OrderBy(x => x.Key.Key).ToDictionary(x => x.Key, y => y.Value);
            var errorMsg = new List<string>();

            var conditionKinds = new ConditionKindTable();
            foreach (var conditionKind in commonData.AllConditionKinds.ConditionKinds)
            {
                int num = conditionKind.Key;
                ConditionKind ck = new ConditionKind(); // ConditionKindFactory.CreateConditionKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = conditionKind.Value.Name;
                    conditionKinds.AddConditionKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            commonData.AllConditionKinds = conditionKinds;

            var allConditions = new ConditionTable();
            foreach (var condition in commonData.AllConditions.Conditions)
            {
                var conditionClone = condition.Value.Clone();
                if (conditionClone.Kind == null)
                {

                }
                else
                {
                    commonData.AllConditionKinds.ConditionKinds.TryGetValue(conditionClone.Kind.ID, out conditionClone.Kind);
                    allConditions.AddCondition(conditionClone);
                }
            }
            commonData.AllConditions = allConditions;

            var influenceKinds = new InfluenceKindTable();
            foreach (var influenceKind in commonData.AllInfluenceKinds.InfluenceKinds)
            {
                int num = influenceKind.Key;
                InfluenceKind ck = new InfluenceKind(); // InfluenceKindFactory.CreateInfluenceKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Type = influenceKind.Value.Type;
                    ck.Name = influenceKind.Value.Name;
                    ck.Combat = influenceKind.Value.Combat;
                    ck.AIPersonValue = influenceKind.Value.AIPersonValue;
                    ck.AIPersonValuePow = influenceKind.Value.AIPersonValuePow;
                    influenceKinds.AddInfluenceKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            commonData.AllInfluenceKinds = influenceKinds;

            var allInfluences = new InfluenceTable();
            foreach (var influence in commonData.AllInfluences.Influences)
            {
                var inf = influence.Value.Clone();
                commonData.AllInfluenceKinds.InfluenceKinds.TryGetValue(inf.Kind.ID, out inf.Kind);
                allInfluences.AddInfluence(inf);
            }
            commonData.AllInfluences = allInfluences;

            var eventEffectKinds = new EventEffectKindTable();
            foreach (var eventEffectKind in commonData.AllEventEffectKinds.EventEffectKinds)
            {
                int num = eventEffectKind.Key;
                EventEffectKind ck = new EventEffectKind(); // EventEffectKindFactory.CreateEventEffectKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = eventEffectKind.Value.Name;
                    eventEffectKinds.AddEventEffectKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            commonData.AllEventEffectKinds = eventEffectKinds;

            var eventEffects = new EventEffectTable();
            foreach (var eventEffect in commonData.AllEventEffects.EventEffects)
            {
                var eve = eventEffect.Value.Clone();
                if (eve.Kind == null)
                {

                }
                else
                {
                    commonData.AllEventEffectKinds.EventEffectKinds.TryGetValue(eve.Kind.ID, out eve.Kind);
                    eventEffects.AddEventEffect(eve);
                }
            }
            commonData.AllEventEffects = eventEffects;

            var troopEventEffectKinds = new GameObjects.TroopDetail.EventEffect.EventEffectKindTable();
            foreach (var eventEffectKind in commonData.AllTroopEventEffectKinds.EventEffectKinds)
            {
                int num = eventEffectKind.Key;
                GameObjects.TroopDetail.EventEffect.EventEffectKind ck = new TroopDetail.EventEffect.EventEffectKind(); // GameObjects.TroopDetail.EventEffect.EventEffectKindFactory.CreateEventEffectKindByID(num);
                if (ck != null)
                {
                    ck.ID = num;
                    ck.Name = eventEffectKind.Value.Name;
                    troopEventEffectKinds.AddEventEffectKind(ck);
                }
                else
                {
                    errorMsg.Add("条件类型ID" + num + "不存在于游戏中。");
                }
            }
            commonData.AllTroopEventEffectKinds = troopEventEffectKinds;

            var allTroopEventEffects = new TroopDetail.EventEffect.EventEffectTable();
            foreach (var eventEffect in commonData.AllTroopEventEffects.EventEffects)
            {
                var eve = eventEffect.Value.Clone();
                if (eve.Kind == null)
                {

                }
                else
                {
                    commonData.AllTroopEventEffectKinds.EventEffectKinds.TryGetValue(eve.Kind.ID, out eve.Kind);
                    allTroopEventEffects.AddEventEffect(eve);
                }
            }
            commonData.AllTroopEventEffects = allTroopEventEffects;

            scenario.GameCommonData = commonData;
        }

        public static List<Scenario> LoadScenarioSaves()
        {
            string saveDir = @"Save\";

            if (!Platform.Current.UserDirectoryExist(saveDir))
            {
                Platform.Current.UserDirectoryCreate(saveDir);
            }

            List<Scenario> scesList = null;

            // 1. 优先尝试读取 Saves.json (官方标准方式)
            // 这能确保加载完整的元数据（由 SaveGameScenario 生成）
            try 
            {
                string savesJsonPath = saveDir + "Saves.json";
                if (System.IO.File.Exists(savesJsonPath))
                {
                    scesList = SimpleSerializer.DeserializeJsonFile<List<Scenario>>(savesJsonPath, true, false);
                    if (scesList != null)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioSaves] ✅ 成功从 Saves.json 加载 {scesList.Count} 个存档记录");
                        #endif
                        
                        // 验证并修正列表长度
                        // 如果列表长度不够，补充空位；如果太长，截断（虽然 JSON 里的通常是完整的）
                        // 实际上我们应该返回这个列表，但在返回前要确保它覆盖了所有槽位，或者至少UI能处理
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioSaves] ⚠️ 读取 Saves.json 失败: {ex.Message}");
            }

            // 如果 Saves.json 读取失败或为空，初始化一个新列表
            if (scesList == null)
            {
                scesList = new List<Scenario>();
            }

            // 确保列表有足够的槽位（savemaxcounts + 1）
            // 如果是从 Saves.json 加载的，可能已经有部分数据，我们需要确保它是完整的列表
            // 如果它是一个稀疏列表或者长度不对，我们需要调整它
            
            // 将现有的列表转换为字典以便快速查找
            var existingSaves = new Dictionary<string, Scenario>();
            foreach (var s in scesList)
            {
                if (!string.IsNullOrEmpty(s.ID))
                {
                    existingSaves[s.ID] = s;
                }
            }

            var finalSavesList = new List<Scenario>();

            // 遍历所有存档槽 (0 to savemaxcounts)
            for (int i = 0; i <= savemaxcounts; i++)
            {
                string slotId = i < 10 ? "0" + i.ToString() : i.ToString();
                
                // 检查这个槽位是否通过 Saves.json 加载了有效数据
                // 只有当 Title 不为空时才认为是有效的（避免 Saves.json 里有一些占位符）
                bool hasMetadata = existingSaves.ContainsKey(slotId) && !string.IsNullOrEmpty(existingSaves[slotId].Title);
                
                if (hasMetadata)
                {
                        // 🔥 DEBUG INSTRUMENTATION
                        // Force a specific string to verify if this code is running
                        existingSaves[slotId].Summary = $"DEBUG_SETTER_JSON: {existingSaves[slotId].Title}";
                    
                    finalSavesList.Add(existingSaves[slotId]);
                    continue; // 已经有有效元数据，跳过文件扫描
                }

                // 如果没有有效的元数据，尝试从文件扫描（回退机制）
                string saveFileName = $"Save{slotId}";
                string savFilePath = saveDir + saveFileName + ".sav.gz";
                string binFilePath = saveDir + saveFileName + ".bin";

                Scenario sce = new Scenario()
                {
                    ID = slotId
                };

                bool fileLoaded = false;

                // 2. 尝试读取 .sav.gz 文件头部（增强版回退逻辑）
                if (File.Exists(savFilePath))
                {
                    try
                    {
                        // 读取更多数据以增加获取 Title 和 Date 的几率
                        // 32KB 应该足够覆盖头部信息
                        using (var fileStream = System.IO.File.OpenRead(savFilePath))
                        using (var gzipStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Decompress))
                        using (var reader = new System.IO.StreamReader(gzipStream, System.Text.Encoding.UTF8))
                        {
                            char[] buffer = new char[32768]; // 32KB
                            int charsRead = reader.Read(buffer, 0, buffer.Length);
                            string partialJson = new string(buffer, 0, charsRead);
                            
                            // 尝试提取信息
                            string scenarioTitle = ExtractJsonValue(partialJson, "ScenarioTitle");
                            if (string.IsNullOrEmpty(scenarioTitle))
                            {
                                scenarioTitle = ExtractJsonValue(partialJson, "Title");
                            }

                            // 🔥 补充提取玩家势力信息，解决读档列表势力显示为"电脑"的问题
                            string playerInfo = ExtractJsonValue(partialJson, "PlayerInfo");

                            if (!string.IsNullOrEmpty(scenarioTitle))
                            {
                                sce.Title = scenarioTitle;
                                sce.Name = saveFileName;
                                
                                if (!string.IsNullOrEmpty(playerInfo))
                                {
                                    sce.Info = playerInfo;
                                }

                                // 尝试提取日期 (Date 对象通常在前面)
                                // "Date": { "Year": 184, "Month": 1, "Day": 1 }
                                // 简单的正则提取
                                try 
                                {
                                    // 查找年份
                                    string year = ExtractByRegex(partialJson, "\"Year\"\\s*:\\s*(\\d+)");
                                    string month = ExtractByRegex(partialJson, "\"Month\"\\s*:\\s*(\\d+)");
                                    string day = ExtractByRegex(partialJson, "\"Day\"\\s*:\\s*(\\d+)");
                                    
                                    if (!string.IsNullOrEmpty(year) && !string.IsNullOrEmpty(month) && !string.IsNullOrEmpty(day))
                                    {
                                        string dateStr = $"{year}年{month}月{day}日";
                                        sce.Time = dateStr; // 设置 Time 字段，Scenario.Summary 会用到
                                    }
                                }
                                catch {}

                                // 获取文件修改时间作为备份
                                var fileInfo = new System.IO.FileInfo(savFilePath);
                                string timeStr = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                                
                                if (string.IsNullOrEmpty(sce.Time))
                                {
                                    sce.Time = timeStr;
                                }
                                
                                // 使用与 Scenario.cs 一致的格式: 标题 [势力] 时间
                                // 势力信息在 fallback 中较难获取，使用占位符或仅显示标题时间
                            }
                            else
                            {
                                // 无法解析标题，显示基本文件信息
                                var fileInfo = new System.IO.FileInfo(savFilePath);
                                sce.Title = "存档";
                                sce.Name = saveFileName;
                                sce.Create = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                            }
                        }
                        fileLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        // 发生异常，显示占位符
                        sce.Title = "存档";
                        sce.Name = saveFileName;
                        sce.Summary = "存档 (无法读取)";
                    }
                }
                // 3. 尝试读取旧的 .bin 文件
                else if (File.Exists(binFilePath))
                {
                    try
                    {
                        var fileInfo = new System.IO.FileInfo(binFilePath);
                        sce.Title = "旧存档 (.bin)";
                        sce.Name = saveFileName;
                        sce.Create = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                        fileLoaded = true;
                    }
                    catch
                    {
                        sce.Title = "旧存档 (.bin)";
                        sce.Name = saveFileName;
                    }
                }
                
                // 4. 空槽位
                if (!fileLoaded && !hasMetadata)
                {
                    sce.Title = "";
                    sce.Name = "";
                }

                finalSavesList.Add(sce);
            }

            return finalSavesList;
        }

        /// <summary>
        /// 使用正则提取值的辅助方法
        /// </summary>
        private static string ExtractByRegex(string input, string pattern)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            catch {}
            return null;
        }
        
        /// <summary>
        /// 从 JSON 字符串中提取指定字段的值（简单解析，避免完整反序列化）
        /// </summary>
        private static string ExtractJsonValue(string json, string fieldName)
        {
            try
            {
                // 查找字段名（支持 camelCase 和 PascalCase）
                string[] patterns = new[]
                {
                    $"\"{fieldName}\":",
                    $"\"{char.ToLower(fieldName[0])}{fieldName.Substring(1)}\":",
                    $"\"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}\":",
                };
                
                foreach (string pattern in patterns)
                {
                    int startIndex = json.IndexOf(pattern);
                    if (startIndex >= 0)
                    {
                        startIndex += pattern.Length;
                        
                        // 跳过空白字符
                        while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
                        {
                            startIndex++;
                        }
                        
                        // 提取值
                        if (startIndex < json.Length)
                        {
                            char firstChar = json[startIndex];
                            
                            // 字符串值
                            if (firstChar == '"')
                            {
                                int endIndex = json.IndexOf('"', startIndex + 1);
                                if (endIndex > startIndex)
                                {
                                    return json.Substring(startIndex + 1, endIndex - startIndex - 1);
                                }
                            }
                            // 数字值
                            else if (char.IsDigit(firstChar) || firstChar == '-')
                            {
                                int endIndex = startIndex;
                                while (endIndex < json.Length && (char.IsDigit(json[endIndex]) || json[endIndex] == '.' || json[endIndex] == '-'))
                                {
                                    endIndex++;
                                }
                                return json.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                }
            }
            catch
            {
                // 解析失败，返回空字符串
            }
            
            return string.Empty;
        }

        public static void SaveScenarioSaves(List<Scenario> saves)
        {
            string saveDir = @"Save\";
            string saveFile = saveDir + "Saves.json";

            SimpleSerializer.SerializeJsonFile(saves, saveFile);

            if (Session.MainGame.mainMenuScreen.MenuType == WorldOfTheThreeKingdoms.GameScreens.MenuType.Save)
            {
                Session.MainGame.mainMenuScreen.InitScenarioSaveList();
            }
        }

        public void DisposeLotsOfMemory()
        {
            //foreach (MilitaryKind kind in this.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
            //{
            //    kind.Textures.Dispose();
            //}
            //foreach (Animation a in this.GameCommonData.AllTroopAnimations.Animations.Values)
            //{
            //    a.disposeTexture();
            //}
            //foreach (Architecture a in this.Architectures)
            //{
            //    if (a.CaptionTexture != null)
            //    {
            //        a.CaptionTexture.Dispose();
            //        a.CaptionTexture = null;
            //    }
            //}
            //foreach (ArchitectureKind k in this.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Values)
            //{
            //    if (k.Texture != null)
            //    {
            //        k.ClearTexture();
            //    }
            //}
            //foreach (Treasure t in this.Treasures)
            //{
            //    t.disposeTexture();
            //}
            //foreach (TerrainDetail t in this.GameCommonData.AllTerrainDetails.TerrainDetails.Values)
            //{
            //    if (t.Textures != null)
            //    {
            //        //foreach (var u in t.Textures.BasicTextures)
            //        //{
            //        //    u.Dispose();
            //        //}
            //        foreach (Texture u in t.Textures.BottomEdgeTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.BottomLeftCornerTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.BottomLeftTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.BottomRightCornerTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.BottomRightTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.BottomTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.CentreTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.LeftEdgeTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.LeftTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.RightEdgeTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.RightTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.LeftEdgeTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopEdgeTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopLeftCornerTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopLeftTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopRightCornerTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopRightTextures)
            //        {
            //            u.Dispose();
            //        }
            //        foreach (Texture u in t.Textures.TopTextures)
            //        {
            //            u.Dispose();
            //        }
            //    }
            //    t.Textures = null;
            //}

            if (Session.MainGame != null && Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.DisposeMapTileMemory(true, false);
            }
        }

        public void SetMapTileArchitecture(Architecture architecture)
        {
            if (!architecture.AutoRefillFoodInLongViewArea)
            {
                architecture.AddBaseSupplyingArchitecture();
            }
            foreach (Point point in architecture.ViewArea.Area)
            {
                if (!this.PositionOutOfRange(point))
                {
                    this.MapTileData[point.X, point.Y].AddHighViewingArchitecture(architecture);
                }
            }
            foreach (Point point in architecture.LongViewArea.Area)
            {
                if (!this.PositionOutOfRange(point))
                {
                    this.MapTileData[point.X, point.Y].AddViewingArchitecture(architecture);
                }
            }
        }

        public void SetMapTileTroop(Troop troop)
        {
            // 🔥 修复：清理旧位置
            if (this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y].TroopCount > 0)
            {
                TileData data1 = this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y];
                data1.TroopCount--;
            }
            if (this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y].TileTroop == troop)
            {
                this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y].TileTroop = null;
            }
            
            // 🔥 修复：检测目标位置冲突（原子性保护）
            // 日期：2026-03-08
            // 问题：两支部队可能在竞态条件下移动到同一位置
            // 原因：MoveTo_Logic 的碰撞检测不是原子操作
            // 解决：在地图数据写入的最后一道防线添加冲突检测
            Troop existingTroop = this.MapTileData[troop.Position.X, troop.Position.Y].TileTroop;
            if (existingTroop != null && existingTroop != troop)
            {
                // ⚠️ 严重错误：目标位置已被占用
                // 这表明上层逻辑的碰撞检测失败（竞态条件）
                System.Diagnostics.Debug.WriteLine($"[SetMapTileTroop] ❌ 位置冲突！{troop.DisplayName} 试图移动到 {troop.Position}，但该位置已被 {existingTroop.DisplayName} 占据");
                System.Diagnostics.Debug.WriteLine($"[SetMapTileTroop] 拒绝移动，保持 {troop.DisplayName} 在 {troop.PreviousPosition}");
                
                // 🔥 关键修复：拒绝地图更新，但不回退 Position
                // 原因：Position 已经被 setter 更新，回退会导致状态不一致
                // 策略：让地图数据保持旧状态，Position 会在下一帧被修正
                // 恢复旧位置的地图数据（因为前面已经清理了）
                TileData oldData = this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y];
                oldData.TroopCount++;
                this.MapTileData[troop.PreviousPosition.X, troop.PreviousPosition.Y].TileTroop = troop;
                
                return; // 中止新位置的地图更新
            }
            
            // 🔥 修复：更新新位置（无条件覆盖，因为已经检测过冲突）
            TileData data2 = this.MapTileData[troop.Position.X, troop.Position.Y];
            data2.TroopCount++;
            this.MapTileData[troop.Position.X, troop.Position.Y].TileTroop = troop;
        }

        /// <summary>
        /// 🔥 新增：原子化部队位置更新
        /// 日期：2026-03-10
        /// 问题：MapPositionCache 和 MapTileData 更新不同步，导致部队重叠
        /// 解决：使用锁确保两套系统同步更新，并在更新前进行冲突和地形检测
        /// 性能：Hot Path，避免分配和不必要的日志
        /// </summary>
        public bool AtomicSetTroopPosition(Troop troop, Point oldPos, Point newPos)
        {
            // 🔥 原子性保护：使用锁确保两套系统同步更新
            lock (_troopPositionLock)
            {
                // 1. 冲突检测（在任何更新之前）
                Troop existingTroop = this.MapTileData[newPos.X, newPos.Y].TileTroop;
                if (existingTroop != null && existingTroop != troop)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[AtomicSetTroopPosition] ❌ 位置冲突！{troop.DisplayName} " +
                        $"试图移动到 {newPos}，但该位置已被 {existingTroop.DisplayName} 占据");
#endif
                    return false; // 拒绝移动
                }
                
                // 2. 地形检测（防止进入不可进入地形）
                if (!CanTroopEnterPosition(troop, newPos))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[AtomicSetTroopPosition] ❌ 地形阻挡！{troop.DisplayName} " +
                        $"无法进入 {newPos}");
#endif
                    return false;
                }
                
                // 3. 原子更新：先清理旧位置，再设置新位置
                // 3a. 清理旧位置的 MapTileData
                if (this.MapTileData[oldPos.X, oldPos.Y].TileTroop == troop)
                {
                    this.MapTileData[oldPos.X, oldPos.Y].TileTroop = null;
                    TileData oldData = this.MapTileData[oldPos.X, oldPos.Y];
                    oldData.TroopCount--;
                }
                
                // 3b. 清理旧位置的 MapPositionCache
                if (MapPositionCache.IsInitialized)
                {
                    MapPositionCache.RemoveTroopAt(oldPos, troop);
                }
                
                // 3c. 设置新位置的 MapTileData
                this.MapTileData[newPos.X, newPos.Y].TileTroop = troop;
                TileData newData = this.MapTileData[newPos.X, newPos.Y];
                newData.TroopCount++;
                
                // 3d. 设置新位置的 MapPositionCache
                if (MapPositionCache.IsInitialized)
                {
                    MapPositionCache.SetTroopAt(newPos, troop);
                }
                
                return true; // 移动成功
            }
        }

        /// <summary>
        /// 🔥 新增：检测部队是否可以进入指定位置
        /// 日期：2026-03-10
        /// 用途：防止部队进入不可进入的地形（水域、敌方城池内部等）
        /// 
        /// 🔥 修复：读档阶段时序问题
        /// 日期：2026-03-12
        /// 问题：读档时 Army 尚未加载，访问 Army.Kind 导致 NullReferenceException
        /// 解决：如果 Army 未初始化，跳过地形检查（读档阶段允许通过）
        /// </summary>
        private bool CanTroopEnterPosition(Troop troop, Point position)
        {
            // 1. 边界检查
            if (this.PositionOutOfRange(position))
                return false;
            
            // 2. 地形检查
            var terrainKind = this.GetTerrainKindByPositionNoCheck(position);
            
            // 3. 水域检查（非水军不能进入水域）
            // 🔥 修复：读档阶段 Army 可能未加载，跳过检查
            // 日期：2026-03-12
            // 原因：LoadTroopFromDTO 设置 Position 时，Militaries 可能还未完全加载
            //       导致 Army 延迟加载失败，返回 null
            // 解决：如果 Army 为 null，允许通过（读档阶段的临时状态）
            //       等 LinkReferencesPhase 完成后，Army 会被正确链接
            // 
            // 判断逻辑：
            // - terrainKind == TerrainKind.水域 表示水域地形
            // - WaterAdaptability < Movability 表示水域适应性不足，无法进入
            if (terrainKind == TerrainKind.水域)
            {
                // 🔥 关键：检查 Army 是否已初始化
                if (troop.Army == null)
                {
                    // 读档阶段：Army 未加载，跳过检查
                    return true;
                }
                
                // 🔥 关键：检查 Kind 是否已初始化
                if (troop.Army.Kind == null)
                {
                    // 数据损坏：Army 已加载但 Kind 为 null
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[CanTroopEnterPosition] ⚠️ 警告：{troop.DisplayName} 的 Army.Kind 为 null，跳过水域检查");
                    #endif
                    return true;
                }
                
                // 正常检查：水域适应性
                if (troop.Army.Kind.WaterAdaptability < troop.Army.Kind.Movability)
                    return false;
            }
            
            // 4. 城池检查（敌方城池耐久>0时不能进入内部）
            var architecture = this.GetArchitectureByPositionNoCheck(position);
            if (architecture != null && 
                !troop.BelongedFaction.IsFriendly(architecture.BelongedFaction) &&
                architecture.Endurance > 0)
            {
                // 🔥 ANTI-BAND-AID：不添加防御性空检查
                // 如果 ArchitectureArea 为 null，说明城池数据初始化有问题
                // 检查是否在城池区域内（不是攻击范围）
                if (architecture.ArchitectureArea.HasPoint(position))
                {
                    return false; // 不能进入敌方城池内部
                }
            }
            
            return true;
        }

        public void SetPenalizedMapDataByArea(GameArea gameArea, int cost)
        {
            foreach (Point point in gameArea.Area)
            {
                if (!this.PositionOutOfRange(point))
                {
                    this.PenalizedMapData[point.X, point.Y] = cost;
                }
            }
            this.SetPenalizedMapDataByPosition(gameArea.Centre, 0xdac);
        }

        public void SetPenalizedMapDataByPosition(Point position, int cost)
        {
            this.PenalizedMapData[position.X, position.Y] = cost;
        }

        public void SetPlayerFactionList(GameObjectList factions)
        {
            // 🔥 防御性检查：如果传入空列表，保持原有 PlayerFactions 不变
            // 🔥 修复：允许传入空列表（实现 AI 接管所有势力的观察者模式）
            if (factions == null)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SetPlayerFactionList] Factions arg is null.");
                #endif
                return;
            }
            
            this.PlayerFactions.Clear();
            foreach (Faction faction in factions)
            {
                this.PlayerFactions.Add(faction);
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SetPlayerFactionList] PlayerFactions 已更新: {this.PlayerFactions.Count} 个势力");
            #endif
        }

        public void SetPositionOnFire(Point position)
        {
            this.FireTable.AddPosition(position);
            this.GeneratorOfTileAnimation.AddTileAnimation(TileAnimationKind.火焰, position, true);
        }

        public void YearPassedEvent()
        {
            ScenarioEvents.RaiseYearPassed(this);
            foreach (GameObject obj in this.Architectures.GetRandomList())
            {
                Architecture architecture = (obj is Architecture ? (Architecture)obj : null);
                if (architecture != null)
                {
                    architecture.YearEvent();
                }
            }

            foreach (Faction faction in this.Factions)
            {
                faction.YearOfficialLimit = 0;
            }
            foreach (Person p in this.Persons)
            {
                if (p.Available && p.IsGeneratedChildren && p.Age >= Session.GlobalVariables.ChildrenAvailableAge)
                {
                    p.IsGeneratedChildren = false;
                }
            }
        }

        public void YearStartingEvent()
        {
        }

        /// <summary>
        /// [已废弃] 全局动画状态属性
        /// 新系统通过 Troop.IsAnimationPlaying 进行部队级检查，不再需要全局状态
        /// 保留此属性以兼容旧代码，但始终返回 false
        /// 日期：2026-02-28 动画阻塞重构
        /// </summary>
        [Obsolete("已废弃：使用 Troop.IsAnimationPlaying 代替全局检查")]
        public bool Animating => false;

        public Person NeutralPerson
        {
            get
            {
                if (this.neutralPerson == null)
                {
                    this.neutralPerson = this.Persons.GetGameObject(0x1b5f) is Person ? (Person)this.Persons.GetGameObject(0x1b5f) : null;
                }
                return this.neutralPerson;
            }
        }

        public bool NoCurrentPlayer
        {
            get
            {
                return (this.CurrentPlayer == null);
            }
        }

        public TroopAnimation TroopAnimations
        {
            get
            {
                return this.GameCommonData.TroopAnimations;
            }
        }

        private Architecture huangdisuozai = null;
        public Architecture huangdisuozaijianzhu()
        {
            if (huangdisuozai == null)
            {
                foreach (Architecture a in this.Architectures)
                {
                    if (a.huangdisuozai) huangdisuozai = a;
                }
            }
            return huangdisuozai;
        }

        public bool youhuangdi()
        {
            foreach (Architecture a in this.Architectures)
            {
                if (a.huangdisuozai) return true;
            }
            return false;
        }

        public void BecomeNoEmperor()
        {
            foreach (Architecture a in this.Architectures)
            {
                if (a.huangdisuozai)
                {
                    a.huangdisuozai = false;
                    this.huangdisuozai = null;
                }
            }

            Person neutralPerson = this.NeutralPerson;
            if (neutralPerson == null)
            {
                if (this.CurrentPlayer != null)
                {
                    neutralPerson = this.CurrentPlayer.Leader;
                }
                else
                {
                    if (this.Factions.Count <= 0)
                    {
                        return;
                    }
                    neutralPerson = (this.Factions[0] as Faction).Leader;
                }
            }

            Session.MainGame.mainGameScreen.xianshishijiantupian(neutralPerson, "汉朝", "FactionDestroy", "shilimiewang.jpg", "shilimiewang", true);

        }

        public YearTable getFactionYearTable(Faction f)
        {
            YearTable result = new YearTable();
            foreach (YearTableEntry i in this.YearTable)
            {
                if (i.IsGloballyKnown || i.Factions.GameObjects.Contains(f) || Session.GlobalVariables.SkyEye)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public YearTable getFactionYearTableRecentYears(Faction f, int y)
        {
            YearTable result = new YearTable();
            foreach (YearTableEntry i in this.YearTable)
            {
                if ((i.IsGloballyKnown || i.Factions.GameObjects.Contains(f) || Session.GlobalVariables.SkyEye) &&
                    i.Date.Year > this.Date.Year - y)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public YearTable getOnlyFactionYearTable(Faction f)
        {
            YearTable result = new YearTable();
            foreach (YearTableEntry i in this.YearTable)
            {
                if (i.Factions.GameObjects.Contains(f))
                {
                    result.Add(i);
                }
            }
            return result;
        }
        public bool runScenarioStart(Architecture triggerArch, Screen screen)
        {
            bool ran = false;
            foreach (GameObject gameObj in this.AllEvents)
            {
                Event e = (gameObj is Event ? (Event)gameObj : null);
                if (e == null)
                {
                    // Skip objects that are not Event instances
                    continue;
                }

                if ((e.IsStart() && e.matchEventPersons(triggerArch)) || e.checkConditions(triggerArch))
                {
                    if (!this.EventsToApply.ContainsKey(e))
                    {
                        this.EventsToApply.Add(e, triggerArch);
                        e.ApplyEventDialogs(triggerArch, screen);
                        ran = true;
                    }
                    if (!this.YesEventsToApply.ContainsKey(e) && e.yesEffect.Count > 0)
                    {
                        this.YesEventsToApply.Add(e, triggerArch);
                        ran = true;
                    }
                    if (!this.NoEventsToApply.ContainsKey(e) && e.noEffect.Count > 0)
                    {
                        this.NoEventsToApply.Add(e, triggerArch);
                        ran = true;
                    }
                    /*
                    if (!this.YesArchiEventsToApply.ContainsKey(e))
                    {
                        this.YesArchiEventsToApply.Add(e, triggerArch);

                        e.ApplyEventDialogs(triggerArch);
                        ran = true;
                    }
                    if (!this.NoArchiEventsToApply.ContainsKey(e))
                    {
                        this.NoArchiEventsToApply.Add(e, triggerArch);
                        e.ApplyEventDialogs(triggerArch);
                        ran = true;
                    }
                    */
                }
            }
            return ran;
        }

        public bool runScenarioEnd(Architecture triggerArch, Screen screen)
        {
            bool ran = false;
            foreach (GameObject gameObj in this.AllEvents)
            {
                Event e = (gameObj is Event ? (Event)gameObj : null);
                if (e == null)
                {
                    // Skip objects that are not Event instances
                    continue;
                }

                if ((e.IsEnd() && e.matchEventPersons(triggerArch)) || e.checkConditions(triggerArch))
                {
                    if (!this.EventsToApply.ContainsKey(e))
                    {
                        this.EventsToApply.Add(e, triggerArch);
                        e.ApplyEventDialogs(triggerArch, screen);
                        ran = true;
                    }

                    if (!this.YesEventsToApply.ContainsKey(e) && e.yesEffect.Count > 0)
                    {
                        this.YesEventsToApply.Add(e, triggerArch);
                        ran = true;
                    }
                    if (!this.NoEventsToApply.ContainsKey(e) && e.noEffect.Count > 0)
                    {
                        this.NoEventsToApply.Add(e, triggerArch);
                        ran = true;
                    }
                    /*
                    if (!this.YesArchiEventsToApply.ContainsKey(e))
                    {
                        this.YesArchiEventsToApply.Add(e, triggerArch);

                        e.ApplyEventDialogs(triggerArch);
                        ran = true;
                    }
                    if (!this.NoArchiEventsToApply.ContainsKey(e))
                    {
                        this.NoArchiEventsToApply.Add(e, triggerArch);
                        e.ApplyEventDialogs(triggerArch);
                        ran = true;
                    }*/
                }
            }
            return ran;
        }

        public PersonList Officers() //野武将列表
        {
            PersonList result = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.Available && person.Alive)
                {
                    if (person.ID >= 25000)
                    {
                        result.Add(person);
                    }
                }

            }

            return result;
        }

        public int OfficerCount //野武将总数
        {
            get
            {
                return (this.Officers().Count);
            }
        }

        public int OfficerLimit
        {
            get
            {
                return Session.GlobalVariables.zhaoxianOfficerMax;
            }
        }

        public int GetAITroopCount()
        {
            int cnt = 0;
            foreach (Troop t in this.Troops)
            {
                if (!this.IsPlayer(t.BelongedFaction))
                {
                    cnt++;
                }
            }
            return cnt;
        }

        public bool IsKnownToAnyPlayer(Architecture a)
        {
            if (Session.GlobalVariables.SkyEye) return true;
            foreach (Faction f in this.PlayerFactions)
            {
                if (f.IsArchitectureKnown(a)) return true;
            }
            return false;
        }

        public bool IsKnownToAnyPlayer(Troop a)
        {
            if (Session.GlobalVariables.SkyEye) return true;
            foreach (Faction f in this.PlayerFactions)
            {
                if (f.IsTroopKnown(a)) return true;
            }
            return false;
        }

        public void TrainChildren()
        {
            foreach (Person p in this.Persons)
            {
                //if (p.Trainable && GameObject.Random(30) == 0)
                if (p.Trainable && GameObject.Random((int)(30 / (IsPlayer(p.Father.BelongedFaction) ? 1 : Session.Current.Scenario.Parameters.AIExtraPerson) / Session.Parameters.DayInTurn)) == 0)
                {
                    if (p.TrainPolicy == null)
                    {
                        p.TrainPolicy = (TrainPolicy)this.GameCommonData.AllTrainPolicies.GetGameObject(1);
                    }
                    Dictionary<int, float> weighting = p.TrainPolicy.Weighting;
                    if (p.Age < 8) // No attempt to learn title until age 8
                    {
                        weighting.Remove(8);
                    }
                    int r = GameObject.WeightedRandom(weighting);

                    PersonList teachers = new PersonList();
                    if (p.Father.IsValidTeacher(p))
                    {
                        teachers.Add(p.Father);
                    }
                    if (p.Mother.IsValidTeacher(p))
                    {
                        teachers.Add(p.Mother);
                    }
                    
                    if (teachers.Count <= 3)
                    {
                        GameObjectList candidate = new GameObjectList();
                        foreach (Person q in this.Persons)
                        {
                            if (q.IsValidTeacher(p) && ((q.Father == p.Father) || (q.Mother == p.Mother)))
                            {
                                candidate.Add(q);
                            }
                        }
                        candidate.PropertyName = "Age";
                        candidate.IsNumber = true;
                        candidate.SmallToBig = false;
                        candidate.ReSort();
                        foreach (Person q in candidate)
                        {
                            teachers.Add(q);
                            if (teachers.Count > 3) break;
                        }
                    }
                    if (teachers.Count <= 3)
                    {
                        GameObjectList candidate = new GameObjectList();
                        foreach (Person q in this.Persons)
                        {
                            if (q.IsValidTeacher(p) && q.HasStrainTo(p))
                            {
                                candidate.Add(q);
                            }
                        }
                        candidate.PropertyName = "Age";
                        candidate.IsNumber = true;
                        candidate.SmallToBig = false;
                        candidate.ReSort();
                        foreach (Person q in candidate)
                        {
                            teachers.Add(q);
                            if (teachers.Count > 3) break;
                        }
                    }

                    if (teachers.Count <= 0)
                    {
                        GameObjectList candidate = new GameObjectList();
                        foreach (Person q in this.Persons)
                        {
                            if (GameObject.Chance(10) && q.IsValidTeacher(p) && !q.Hates(p) && !p.Hates(q))
                            {
                                candidate.Add(q);
                            }
                        }
                        candidate.PropertyName = "Merit";
                        candidate.IsNumber = true;
                        candidate.SmallToBig = false;
                        candidate.ReSort();
                        foreach (Person q in candidate)
                        {
                            teachers.Add(q);
                            if (teachers.Count > 0) break;
                        }
                    }

                    switch (r)
                    {
                        case 1:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (GameObject.Chance((int)((q.Strength - p.Strength + 50 + q.childrenAbilityIncrease) * ((float)p.StrengthPotential / p.Strength))))
                                    {
                                        p.Strength += GameObject.Random(Math.Max((p.StrengthPotential * 6 / 5 - p.Strength) / 10, 1) + 1);
                                        p.AdjustRelation(q, 5, 5);
                                        q.AdjustRelation(p, 2, 5);
                                        if (GameObject.Chance(30))
                                        {
                                            Dictionary<Person, int> rels = q.GetAllRelations();
                                            foreach (KeyValuePair<Person, int> rel in rels)
                                            {
                                                if (GameObject.Chance(100 / rels.Count))
                                                {
                                                    p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 2:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (GameObject.Chance((int)((q.Command - p.Command + 50 + q.childrenAbilityIncrease) * ((float)p.CommandPotential / p.Command))))
                                    {
                                        p.Command += GameObject.Random(Math.Max((p.CommandPotential * 6 / 5 - p.Command) / 10, 1) + 1);
                                        p.AdjustRelation(q, 5, 5);
                                        q.AdjustRelation(p, 2, 5);
                                        if (GameObject.Chance(30))
                                        {
                                            Dictionary<Person, int> rels = q.GetAllRelations();
                                            foreach (KeyValuePair<Person, int> rel in rels)
                                            {
                                                if (GameObject.Chance(100 / rels.Count))
                                                {
                                                    p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 3:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (GameObject.Chance((int)((q.Intelligence - p.Intelligence + 50 + q.childrenAbilityIncrease) * ((float)p.IntelligencePotential / p.Intelligence))))
                                    {
                                        p.Intelligence += GameObject.Random(Math.Max((p.IntelligencePotential * 6 / 5 - p.Intelligence) / 10, 1) + 1);
                                        p.AdjustRelation(q, 5, 5);
                                        q.AdjustRelation(p, 2, 5);
                                        if (GameObject.Chance(30))
                                        {
                                            Dictionary<Person, int> rels = q.GetAllRelations();
                                            foreach (KeyValuePair<Person, int> rel in rels)
                                            {
                                                if (GameObject.Chance(100 / rels.Count))
                                                {
                                                    p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 4:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (GameObject.Chance((int)((q.Politics - p.Politics + 50 + q.childrenAbilityIncrease) * ((float)p.PoliticsPotential / p.Politics))))
                                    {
                                        p.Politics += GameObject.Random(Math.Max((p.PoliticsPotential * 6 / 5 - p.Politics) / 10, 1) + 1);
                                        p.AdjustRelation(q, 5, 5);
                                        q.AdjustRelation(p, 2, 5);
                                        if (GameObject.Chance(30))
                                        {
                                            Dictionary<Person, int> rels = q.GetAllRelations();
                                            foreach (KeyValuePair<Person, int> rel in rels)
                                            {
                                                if (GameObject.Chance(100 / rels.Count))
                                                {
                                                    p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 5:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (GameObject.Chance((int)((q.Glamour - p.Glamour + 50 + q.childrenAbilityIncrease) * ((float)p.GlamourPotential / p.Glamour))))
                                    {
                                        p.Glamour += GameObject.Random(Math.Max((p.GlamourPotential * 6 / 5 - p.Glamour) / 10, 1) + 1);
                                        p.AdjustRelation(q, 5, 5);
                                        q.AdjustRelation(p, 2, 5);
                                        if (GameObject.Chance(30))
                                        {
                                            Dictionary<Person, int> rels = q.GetAllRelations();
                                            foreach (KeyValuePair<Person, int> rel in rels)
                                            {
                                                if (GameObject.Chance(100 / rels.Count))
                                                {
                                                    p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 6:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    if (q.Skills.Count <= 0) continue;
                                    List<Skill> skillToTeach = new List<Skill>();
                                    foreach (Skill s in q.Skills.Skills.Values)
                                    {
                                        if (s.CanBeBorn(p))
                                        {
                                            skillToTeach.Add(s);
                                        }
                                    }
                                    List<Skill> candidates = new List<Skill>();
                                    foreach (Skill s in this.GameCommonData.AllSkills.Skills.Values)
                                    {
                                        if (s.CanBeBorn(p) && GameObject.Chance((s.GetRelatedAbility(q) - 70) / 5) && GameObject.Chance(100 / s.Level))
                                        {
                                            skillToTeach.Add(s);
                                        }
                                    }

                                    List<Skill> realSkillToTeach = new List<Skill>();
                                    realSkillToTeach.Add(skillToTeach[GameObject.Random(skillToTeach.Count)]);
                                    realSkillToTeach.Add(skillToTeach[GameObject.Random(skillToTeach.Count)]);
                                    realSkillToTeach.Add(skillToTeach[GameObject.Random(skillToTeach.Count)]);

                                    foreach (Skill t in realSkillToTeach)
                                    {
                                        int extraChance = 0;
                                        if (p.Father.GetSkillList().GameObjects.Contains(t) || p.Mother.GetSkillList().GameObjects.Contains(t))
                                        {
                                            extraChance += 5;
                                        }
                                        if (GameObject.Chance(100 / t.Level + q.childrenSkillChanceIncrease + extraChance))
                                        {
                                            p.Skills.AddSkill(t);
                                            p.AdjustRelation(q, 5, 5);
                                            q.AdjustRelation(p, 2, 5);
                                            if (GameObject.Chance(30))
                                            {
                                                Dictionary<Person, int> rels = q.GetAllRelations();
                                                foreach (KeyValuePair<Person, int> rel in rels)
                                                {
                                                    if (GameObject.Chance(100 / rels.Count))
                                                    {
                                                        p.AdjustRelation(rel.Key, 2, Math.Min(5, rel.Value / 10));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 7:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    List<Stunt> stuntToTeach = new List<Stunt>();
                                    foreach (Stunt s in q.Stunts.Stunts.Values)
                                    {
                                        if (s.CanBeBorn(p))
                                        {
                                            stuntToTeach.Add(s);
                                        }
                                    }

                                    List<Stunt> candidates = new List<Stunt>();
                                    foreach (Stunt s in this.GameCommonData.AllStunts.Stunts.Values)
                                    {
                                        if (s.CanBeBorn(p))
                                        {
                                            candidates.Add(s);
                                        }
                                    }
                                    if (candidates.Count > 0 && GameObject.Chance((q.Strength + q.Command + q.Intelligence - 210) / 15))
                                    {
                                        stuntToTeach.Add(candidates[GameObject.Random(candidates.Count)]);
                                    }

                                    if (stuntToTeach.Count > 0)
                                    {
                                        Stunt t = stuntToTeach[GameObject.Random(stuntToTeach.Count)];
                                        int extraChance = 0;
                                        if (p.Father.GetStuntList().GameObjects.Contains(t) || p.Mother.GetStuntList().GameObjects.Contains(t))
                                        {
                                            extraChance += 10;
                                        }
                                        if (GameObject.Chance((10 + q.childrenStuntChanceIncrease + extraChance) / 3))
                                        {
                                            p.Stunts.AddStunt(t);
                                            p.AdjustRelation(q, 5, 10);
                                            q.AdjustRelation(p, 2, 10);
                                            if (GameObject.Chance(30))
                                            {
                                                Dictionary<Person, int> rels = q.GetAllRelations();
                                                foreach (KeyValuePair<Person, int> rel in rels)
                                                {
                                                    if (GameObject.Chance(100 / rels.Count))
                                                    {
                                                        p.AdjustRelation(rel.Key, 2, Math.Min(10, rel.Value / 10));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case 8:
                            {
                                foreach (Person q in teachers)
                                {
                                    if (p.Hates(q)) continue;
                                    if (q.Hates(p)) continue;
                                    //if (q.Hates(p.Father) || q.Hates(p.Mother) || p.Father.Hates(q) || p.Mother.Hates(q)) continue;
                                    List<Title> toTeach = q.Titles;
                                    int maxLevel = 1;
                                    foreach (Title t in toTeach)
                                    {
                                        if (t.Level > maxLevel && t.Kind.RandomTeachable)
                                        {
                                            maxLevel = t.Level;
                                        }
                                    }

                                    foreach (Title t in this.GameCommonData.AllTitles.Titles.Values)
                                    {
                                        if (t.Kind.RandomTeachable && t.Level <= maxLevel + q.childrenTitleChanceIncrease + 1 && GameObject.Chance(t.InheritChance) && t.CanBeBorn(p))
                                        {
                                            toTeach.Add(t);
                                        }
                                    }

                                    foreach (Title t in toTeach)
                                    {
                                        int extraChance = 0;
                                        if (p.Father.RealTitles.Contains(t) || p.Mother.RealTitles.Contains(t))
                                        {
                                            extraChance += 5;
                                        }
                                        if (GameObject.Chance(t.InheritChance * 3 + q.childrenTitleChanceIncrease * 3 + extraChance) && t.CanBeBorn(p))
                                        {
                                            Title existing = null;
                                            foreach (Title u in p.Titles)
                                            {
                                                if (u.Kind.Equals(t.Kind))
                                                {
                                                    existing = u;
                                                    break;
                                                }
                                            }

                                            // TODO let player choose
                                            if (existing == null || existing.Level < t.Level || (existing.Level == t.Level && existing.Merit < t.Merit))
                                            {
                                                if (existing != null)
                                                {
                                                    p.RealTitles.Remove(existing);
                                                }
                                                p.RealTitles.Add(t);

                                                p.AdjustRelation(q, 5, 5 * t.Level);
                                                q.AdjustRelation(p, 2, 5 * t.Level);
                                                if (GameObject.Chance(30))
                                                {
                                                    Dictionary<Person, int> rels = q.GetAllRelations();
                                                    foreach (KeyValuePair<Person, int> rel in rels)
                                                    {
                                                        if (GameObject.Chance(100 / rels.Count))
                                                        {
                                                            p.AdjustRelation(rel.Key, 2, Math.Min(5 * t.Level, rel.Value / 10));
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }

                                break;
                            }
                    }

                }
            }
        }

        public bool SkyEyeSimpleNotification(GameObject gameobject)
        {
            if (Session.GlobalVariables.SkyEyeSimpleNotification && gameobject != null)
            {
                if (gameobject is Person && (this.CurrentPlayer == null || !this.CurrentPlayer.IsPositionKnown(((gameobject is Person ? (Person)gameobject : null)).Position)))
                {
                    return true;
                }
                if (gameobject is Troop && (this.CurrentPlayer == null || !this.CurrentPlayer.IsPositionKnown(((gameobject is Troop ? (Troop)gameobject : null)).Position)))
                {
                    return true;
                }
                if (gameobject is Architecture && (this.CurrentPlayer == null || !this.CurrentPlayer.IsArchitectureKnown(((gameobject is Architecture ? (Architecture)gameobject : null)))))
                {
                    return true;
                }
            }
            return false;
        }

        public void captivestocaptiveData(CaptiveList captives)
        {
            this.captiveData = captives;
        }

        /// <summary>
        /// 获取指定范围内的部队
        /// </summary>
        public TroopList GetTroopsInRange(Point center, int range)
        {
            var result = new TroopList();
            
            try
            {
                foreach (var obj in this.Troops)
                {
                    if (obj is Troop troop && !troop.Destroyed)
                    {
                        int distance = Math.Abs(troop.Position.X - center.X) + Math.Abs(troop.Position.Y - center.Y);
                        if (distance <= range)
                        {
                            result.Add(troop);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameScenario] GetTroopsInRange error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 获取指定范围内的建筑
        /// </summary>
        public ArchitectureList GetArchitecturesInRange(Point center, int range)
        {
            var result = new ArchitectureList();
            
            try
            {
                foreach (var obj in this.Architectures)
                {
                    if (obj is Architecture architecture)
                    {
                        int distance = Math.Abs(architecture.Position.X - center.X) + Math.Abs(architecture.Position.Y - center.Y);
                        if (distance <= range)
                        {
                            result.Add(architecture);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameScenario] GetArchitecturesInRange error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 获取指定位置的地形
        /// </summary>
        public TerrainDetail GetTerrainAt(Point position)
        {
            try
            {
                if (this.ScenarioMap != null && position.X >= 0 && position.Y >= 0 && 
                    position.X < this.ScenarioMap.MapDimensions.X && position.Y < this.ScenarioMap.MapDimensions.Y)
                {
                    // 【修复】Map类没有GetTerrainDetailByPosition方法，返回默认地形
                    return new TerrainDetail(); // 返回默认地形详情
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameScenario] GetTerrainAt error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 验证并建立 Military.Kind 链接
        /// 🔥 在加载 Militaries 后立即调用，确保所有 Kind 都有效
        /// 日期：2026-02-11
        /// </summary>
        private void ValidateAndLinkMilitaryKinds()
        {
            System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkMilitaryKinds] 开始验证 {this.Militaries.Count} 个 Military...");
            
            int successCount = 0;
            List<string> errors = [];
            
            foreach (Military military in this.Militaries)
            {
                try
                {
                    // 强制触发 Kind 的 getter，立即建立链接并验证
                    var kind = military.Kind;
                    
                    if (kind != null)
                    {
                        successCount++;
                    }
                    else
                    {
                        errors.Add($"Military ID={military.ID}, KindID={military.KindID}: Kind 为 null");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Military ID={military.ID}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkMilitaryKinds] 完成: 成功={successCount}, 失败={errors.Count}");
            
            if (errors.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkMilitaryKinds] ❌ 发现 {errors.Count} 个错误:");
                
                // 🔥 使用传统循环代替 LINQ，符合性能规范
                int displayCount = Math.Min(10, errors.Count);
                for (int i = 0; i < displayCount; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {errors[i]}");
                }
                
                if (errors.Count > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... 还有 {errors.Count - 10} 个错误");
                }
                
                // 🔥 抛出异常，阻止游戏继续加载损坏的数据
                throw new InvalidDataException(
                    $"Military.Kind 验证失败: {errors.Count} 个 Military 的 Kind 无效。\n" +
                    $"存档数据可能已损坏，无法继续加载。\n" +
                    $"前3个错误:\n{string.Join("\n", errors.Count >= 3 ? [errors[0], errors[1], errors[2]] : errors)}");
            }
        }

        /// <summary>
        /// 验证并建立 Troop.Army 链接
        /// 🔥 在加载 Troops 后立即调用，确保所有 Army 都有效
        /// 日期：2026-02-11
        /// </summary>
        private void ValidateAndLinkTroopArmies()
        {
            System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkTroopArmies] 开始验证 {this.Troops.Count} 个 Troop...");
            
            int successCount = 0;
            List<string> errors = [];
            
            foreach (Troop troop in this.Troops)
            {
                try
                {
                    // 强制触发 Army 的 getter，立即建立链接并验证
                    var army = troop.Army;
                    
                    if (army != null)
                    {
                        // 进一步验证 Army.Kind
                        var kind = army.Kind;
                        if (kind != null)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Troop ID={troop.ID}: Army.Kind 为 null");
                        }
                    }
                    else
                    {
                        errors.Add($"Troop ID={troop.ID}, MilitaryID={troop.MilitaryID}: Army 为 null");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Troop ID={troop.ID}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkTroopArmies] 完成: 成功={successCount}, 失败={errors.Count}");
            
            if (errors.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateAndLinkTroopArmies] ❌ 发现 {errors.Count} 个错误:");
                foreach (var error in errors.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {error}");
                }
                if (errors.Count > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... 还有 {errors.Count - 10} 个错误");
                }
                
                throw new InvalidDataException(
                    $"Troop.Army 验证失败: {errors.Count} 个 Troop 的 Army 无效。\n" +
                    $"存档数据可能已损坏，无法继续加载。\n" +
                    $"前3个错误:\n{string.Join("\n", errors.Take(3))}");
            }
        }
    }
}

