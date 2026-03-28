#nullable disable

using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.MapDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using WTKGameManager = WorldOfTheThreeKingdoms.GameManager;
using GameObjects.Influences;
using System.Text;
using System.Text.Json.Serialization;
using GameObjects.Conditions;
using WorldOfTheThreeKingdoms.GameManager; // 添加对StrategicMap的引用
using global::GameManager;
using System.Runtime.Serialization;

namespace GameObjects
{

    [DataContract]
    [GenerateUIAccessor]  // 🔥 添加源生成器特性，支持势力列表和右键菜单访问

    public partial class Faction : GameObject, System.Text.Json.Serialization.IJsonOnDeserialized, WorldOfTheThreeKingdoms.GameGlobal.IPropertyAccessor
    {
        // public int PrinceID = -1;
        //public int AllRoundOfficerCount;
        private int militarycount;
        private int transferingmilitarycount;
        
        // 🔥 AOT 修复：添加 JsonInclude 以支持 System.Text.Json 序列化字段
        // 日期：2026-03-20
        [DataMember]
        [JsonInclude]
        public int ZhaoxianFailureCount = 0;
        
        [DataMember]
        [JsonInclude]
        public int YearOfficialLimit = 0;
        
        [DataMember]
        [JsonInclude]
        public List<int> TechniqueIDs { get; set; } = [];  // 🔥 C# 12：集合表达式

        private Person prince = null;
        private int princeID = -1;
        private bool isAlien = false;
        private int guanjuedezhi = 0;
        private int chaotinggongxiandudezhi = 0;
        public bool AIFinished;

        // 性能优化：缓存建筑数量，用于检测势力规模变化
        private int lastArchitectureCount = 0;

        // 新建势力优先调配
        // 🔥 AOT 修复：改为公共属性（AOT 源生成器无法访问私有字段）
        // 日期：2026-03-20
        [DataMember]
        [JsonInclude]
        public int FactionCreationTurn { get; set; } = -1;    // 势力建立回合数
        private const int NEW_FACTION_PRIORITY_TURNS = 3; // 新建势力优先期：3回合

        public bool IsAlive
        {
            get
            {
                return this.ArchitectureCount > 0;
            }
        }
#pragma warning disable CS0169 // The field 'Faction.AIThread' is never used
        //private Thread AIThread;
#pragma warning restore CS0169 // The field 'Faction.AIThread' is never used
        public ZhandouZhuangtai BattleState = ZhandouZhuangtai.和平;

        // 🔥 人事调配智能触发系统
        // 🔥 AOT 修复：改为公共属性（AOT 源生成器无法访问私有字段）
        // 日期：2026-03-20
        [DataMember]
        [JsonInclude]
        public bool HasPersonnelChanges { get; set; } = true; // 脏标记：默认为 true 确保加载后至少执行一次
        [DataMember]
        [JsonInclude]
        public GameDate LastAllocationDate { get; set; }     // 记录上次执行的时间，用于缓存控制

        // 🔥 NEW: AI执行时间控制系统
        // 🔥 AOT 修复：改为公共属性（AOT 源生成器无法访问私有字段）
        // 日期：2026-03-20
        [DataMember]
        [JsonInclude]
        public GameDate LastPersonnelAIDate { get; set; } = new GameDate(0, 1, 1);  // 上次人员调配执行日期
        
        [DataMember]
        [JsonInclude]
        public bool IsPersonnelUrgentDirty { get; set; } = false;               // 人员调配紧急脏标记
        
        [DataMember]
        [JsonInclude]
        public GameDate LastDomesticAIDate { get; set; } = new GameDate(0, 1, 1);   // 上次内政AI执行日期
        
        [DataMember]
        [JsonInclude]
        public bool IsDomesticUrgentDirty { get; set; } = false;                // 内政AI紧急脏标记

        // AI执行间隔常量
        private const int PERSONNEL_INTERVAL_NORMAL = 60;  // 人员调配正常间隔：60天
        private const int PERSONNEL_INTERVAL_URGENT = 5;   // 人员调配紧急间隔：5天
        private const int DOMESTIC_INTERVAL_NORMAL = 30;   // 内政AI正常间隔：30天
        private const int DOMESTIC_INTERVAL_URGENT = 3;    // 内政AI紧急间隔：3天

        // 🔥 NEW: 外交和军事AI时间控制
        // 🔥 AOT 修复：改为公共属性（AOT 源生成器无法访问私有字段）
        // 日期：2026-03-20
        [DataMember]
        [JsonInclude]
        public int DaysSinceLastDiplomacy { get; set; } = 0;           // 外交计时器
        private const int DIPLOMACY_INTERVAL = 90;         // 外交间隔：90天 (一季度)
        [DataMember]
        [JsonInclude]
        public int DaysSinceLastMilitary { get; set; } = 0;            // 军事计时器

        // 🛡️ 防止递归：人员调配执行标志
        [ThreadStatic]
        internal static bool _isExecutingPersonnelAllocation = false;



        /// <summary>
        /// 🧠 AI 记忆地图 - 存储该势力观察到的敌军情报
        /// 用于基于记忆的影响力计算和威胁评估
        /// 核心记忆库：Key = "{FactionID}_{TroopID}", Value = 幽灵数据
        /// 使用 Dictionary 保证 O(1) 查找速度
        /// </summary>
        public WorldOfTheThreeKingdoms.GameManager.AIMemoryMap MemoryMap { get; } = new WorldOfTheThreeKingdoms.GameManager.AIMemoryMap();

        /// <summary>
        /// 获取某个坐标上的幽灵单位 (用于 UI 显示或 AI 判断)
        /// </summary>
        /// <param name="pos">地图坐标</param>
        /// <returns>该位置的幽灵单位，如果没有则返回null</returns>
        public WorldOfTheThreeKingdoms.GameManager.GhostUnit GetGhostAt(Point pos)
        {
            if (MemoryMap?.Values == null) return null;

            foreach (var ghost in MemoryMap.Values.Values)
            {
                if (ghost.LastPosition == pos)
                {
                    return ghost;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据部队ID获取幽灵单位
        /// </summary>
        /// <param name="troopId">部队ID</param>
        /// <param name="factionId">势力ID</param>
        /// <returns>幽灵单位，如果没有则返回null</returns>
        public WorldOfTheThreeKingdoms.GameManager.GhostUnit GetGhostByTroopId(int troopId, int factionId)
        {
            if (MemoryMap?.Values == null) return null;

            string key = $"{factionId}_{troopId}";
            return MemoryMap.Values.ContainsKey(key) ? MemoryMap.Values[key] : null;
        }

        /// <summary>
        /// 获取所有记忆中的幽灵单位
        /// </summary>
        /// <returns>幽灵单位列表</returns>
        public List<WorldOfTheThreeKingdoms.GameManager.GhostUnit> GetAllGhosts()
        {
            if (MemoryMap?.Values == null) return new List<WorldOfTheThreeKingdoms.GameManager.GhostUnit>();

            return new List<WorldOfTheThreeKingdoms.GameManager.GhostUnit>(MemoryMap.Values.Values);
        }

        /// <summary>
        /// 🗺️ 战略影响力地图 - 基于记忆的势能图
        /// 用于AI战略决策和威胁评估
        /// </summary>
        private StrategicMap _strategicMap;

        /// <summary>
        /// 🆕 阶段 3：全局能量地图（双层结构）
        /// 该势力在全图的能量分布，长度为 MapWidth × MapHeight
        /// 使用一维数组优化缓存性能
        /// 🔥 日期：2026-03-16
        /// 🔥 重构：从 int[] 改为 TileInfluenceState[]，区分城池能量和部队能量
        /// </summary>
        public WorldOfTheThreeKingdoms.GameManager.TileInfluenceState[] GlobalInfluenceMap { get; private set; } = [];

        /// <summary>
        /// 🆕 阶段 3.5：领土总能量
        /// 该势力在全图所有格子的能量总和
        /// 用于势力实力评估、AI 决策、外交判断等
        /// 日期：2026-03-13
        /// </summary>
        public int TotalTerritoryEnergy { get; set; } = 0;

        /// <summary>
        /// 🆕 阶段 3：标记全局能量地图需要重算
        /// </summary>
        private bool _isInfluenceMapDirty = true;

        /// <summary>
        /// 获取或创建战略影响力地图
        /// </summary>
        public StrategicMap StrategicMap
        {
            get
            {
                if (_strategicMap == null && Session.Current?.Scenario?.ScenarioMap != null)
                {
                    int width = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                    int height = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                    _strategicMap = new StrategicMap(width, height);
                }
                return _strategicMap;
            }
        }

        /// <summary>
        /// 📜 军师建议日志 - 记录近期的军师建议和内政记录
        /// 用于玩家查看历史建议和决策参考
        /// </summary>
        public List<string> AdviceLog { get; } = new List<string>();
        
        /// <summary>
        /// 🎯 蜜月期结束事件队列 - 记录本月结束蜜月期的武将
        /// 用于在月度事件后统一显示提示
        /// </summary>
        private List<Person.HoneymoonEndEvent> honeymoonEndEvents = [];

        /// <summary>
        /// 添加军师建议到日志
        /// </summary>
        /// <param name="advice">建议内容</param>
        public void AddAdviceToLog(string advice)
        {
            if (string.IsNullOrEmpty(advice)) return;

            // 添加时间戳
            string timestamp = Session.Current?.Scenario?.Date != null
                ? $"{Session.Current.Scenario.Date.Year}年{Session.Current.Scenario.Date.Month}月{Session.Current.Scenario.Date.Day}日"
                : "未知日期";

            string logEntry = $"[{timestamp}] {advice}";
            AdviceLog.Add(logEntry);

            // 限制日志条数，避免内存过度使用
            if (AdviceLog.Count > 50)
            {
                AdviceLog.RemoveAt(0); // 移除最旧的记录
            }

            System.Diagnostics.Debug.WriteLine($"[AdviceLog] {logEntry}");
        }
        
        /// <summary>
        /// 添加蜜月期结束事件到队列
        /// </summary>
        /// <param name="honeymoonEvent">蜜月期结束事件</param>
        internal void AddHoneymoonEndEvent(Person.HoneymoonEndEvent honeymoonEvent)
        {
            honeymoonEndEvents.Add(honeymoonEvent);
        }
        
        /// <summary>
        /// 获取并清空蜜月期结束事件队列
        /// </summary>
        /// <returns>本月结束蜜月期的武将事件列表</returns>
        public List<Person.HoneymoonEndEvent> GetAndClearHoneymoonEndEvents()
        {
            List<Person.HoneymoonEndEvent> events = [..honeymoonEndEvents];
            honeymoonEndEvents.Clear();
            return events;
        }

        public bool AllowAttackAfterMoveOfBubing;
        public bool AllowAttackAfterMoveOfNubing;
        public bool AllowAttackAfterMoveOfQibing;
        public bool AllowAttackAfterMoveOfQixie;
        public bool AllowAttackAfterMoveOfShuijun;
        public int AntiArrowChanceIncrementOfBubing;
        public int AntiArrowChanceIncrementOfNubing;
        public int AntiArrowChanceIncrementOfQibing;
        public int AntiArrowChanceIncrementOfQixie;
        public int AntiArrowChanceIncrementOfShuijun;
        public int AntiCriticalStrikeChanceIncrementWhileCombatMethodOfBubing;
        public int AntiCriticalStrikeChanceIncrementWhileCombatMethodOfNubing;
        public int AntiCriticalStrikeChanceIncrementWhileCombatMethodOfQibing;
        public int AntiCriticalStrikeChanceIncrementWhileCombatMethodOfQixie;
        public int AntiCriticalStrikeChanceIncrementWhileCombatMethodOfShuijun;
        public int troopSequence = -1;


        private int[,] architectureAdjustCost;

        // 🔥 NEW: 高级人事管理器实例
        private AdvancedPersonnelManager _advancedPersonnelManager;

        public void Init()
        {
            BattleState = ZhandouZhuangtai.和平;

            // 初始化高级人事管理器
            _advancedPersonnelManager = new AdvancedPersonnelManager(this);

            Architectures = new ArchitectureList();

            AvailableTechniques = new TechniqueTable();

            BaseMilitaryKinds = new MilitaryKindTable();

            KnownTroops = new Dictionary<int, Troop>();

            Sections = new SectionList();

            Informations = new InformationList();
            KnownTroops = new Dictionary<int, Troop>();

            Legions = new LegionList();

            LevelOfView = InformationLevel.中;

            RoutewayPathBuilder = new RoutewayPathFinder();

            ClosedRouteways = new Dictionary<Point, object>();

            Routeways = new RoutewayList();

            SecondTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();

            TechniqueMilitaryKinds = new MilitaryKindTable();

            ThirdTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();

            Troops = new TroopList();

            TransferingMilitaries = new MilitaryList();

            count = new Dictionary<PersonGeneratorType, int>();

            RateOfCombativityRecoveryAfterAttacked = 0;
            RateOfCombativityRecoveryAfterStratagemFail = 0;
            RateOfCombativityRecoveryAfterStratagemSuccess = 0;
            RateOfFoodTransportBetweenArchitectures = 1;
            RateOfRoutewayConsumption = 1;

            techniqueFundCostRateDecrease = new List<float>();
            techniquePointCostRateDecrease = new List<float>();
            techniqueTimeRateDecrease = new List<float>();
            techniqueReputationRateDecrease = new List<float>();

            CriticalOfMillitaryType = new int[5];
            AntiCriticalOfMillitaryType = new int[5];
            ArchitectureDamageOfMillitaryType = new float[5];
            SpeedOfMillitaryType = new float[5];
            for (int i = 0; i < 5; ++i)
            {
                ArchitectureDamageOfMillitaryType[i] = 1;
                SpeedOfMillitaryType[i] = 1;
            }
            ViewAreaOfMillitaryType = new int[5];
            StratagemOfMillitaryType = new int[5];
            AntiStratagemOfMillitaryType = new int[5];

            // 🔧 FIX: 添加边界检查，防止 ArgumentOutOfRangeException
            if (Session.Current?.Scenario?.GameCommonData?.AllColors != null &&
                Session.Current.Scenario.GameCommonData.AllColors.Count > 0)
            {
                // 确保 ColorIndex 在有效范围内
                if (this.ColorIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.Init] 警告：势力 {this.Name} 的 ColorIndex={this.ColorIndex} 为负数，重置为 0");
                    this.ColorIndex = 0;
                }
                else if (this.ColorIndex >= Session.Current.Scenario.GameCommonData.AllColors.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.Init] 警告：势力 {this.Name} 的 ColorIndex={this.ColorIndex} 超出范围（最大={Session.Current.Scenario.GameCommonData.AllColors.Count - 1}），重置为 0");
                    this.ColorIndex = 0;
                }

                this.FactionColor = Session.Current.Scenario.GameCommonData.AllColors[this.ColorIndex];
            }
            else
            {
                // 如果 AllColors 未初始化，使用默认颜色
                System.Diagnostics.Debug.WriteLine($"[Faction.Init] 警告：AllColors 未初始化，使用默认颜色");
                this.FactionColor = Color.White;
            }

            this.RoutewayPathBuilder = new RoutewayPathFinder();
            this.RoutewayPathBuilder.OnGetCost += new RoutewayPathFinder.GetCost(this.RoutewayPathBuilder_OnGetCost);
            this.RoutewayPathBuilder.OnGetPenalizedCost += new RoutewayPathFinder.GetPenalizedCost(this.RoutewayPathBuilder_OnGetPenalizedCost);
        }

        /// <summary>
        /// 🔥 反序列化回调：恢复势力颜色和初始化集合
        /// 用途：在反序列化后自动调用，恢复未序列化的字段
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) => RestoreAfterDeserialization();

        /// <summary>
        /// IJsonOnDeserialized 接口实现 —— STJ AOT 模式下的反序列化回调。
        /// </summary>
        void System.Text.Json.Serialization.IJsonOnDeserialized.OnDeserialized() => RestoreAfterDeserialization();

        private void RestoreAfterDeserialization()
        {
            // 1. 根据 ColorIndex 恢复 FactionColor
            if (Session.Current?.Scenario?.GameCommonData?.AllColors != null &&
                Session.Current.Scenario.GameCommonData.AllColors.Count > 0)
            {
                if (this.ColorIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.OnDeserialized] 警告：势力 {this.Name} 的 ColorIndex={this.ColorIndex} 为负数，重置为 0");
                    this.ColorIndex = 0;
                }
                else if (this.ColorIndex >= Session.Current.Scenario.GameCommonData.AllColors.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.OnDeserialized] 警告：势力 {this.Name} 的 ColorIndex={this.ColorIndex} 超出范围（最大={Session.Current.Scenario.GameCommonData.AllColors.Count - 1}），重置为 0");
                    this.ColorIndex = 0;
                }

                this.FactionColor = Session.Current.Scenario.GameCommonData.AllColors[this.ColorIndex];
                System.Diagnostics.Debug.WriteLine($"[Faction.OnDeserialized] 势力 {this.Name} 恢复颜色: ColorIndex={this.ColorIndex}, Color=({this.FactionColor.R},{this.FactionColor.G},{this.FactionColor.B})");
            }
            else
            {
                this.FactionColor = Color.White;
                System.Diagnostics.Debug.WriteLine($"[Faction.OnDeserialized] 警告：AllColors 未初始化，势力 {this.Name} 使用默认颜色");
            }

            // 2. 确保集合字段已初始化
            EnsureCollectionsInitialized();
            
            // 🔥 3. 注意：BaseMilitaryKinds 和 AvailableTechniques 的加载
            // 日期：2026-03-18
            // 说明：OnDeserialized 在 LoadFromDTO 中触发，此时 CommonData 可能未加载
            //       因此 LoadFromString() 在 SerializationManager.LoadScenario() 的 Phase 4.5 中手动调用
            //       这里不需要调用 LoadFromString()，避免重复加载
        }

        /// <summary>
        /// 🔥 根本性修复：确保所有集合字段已初始化
        /// 用途：在反序列化后调用，防止集合字段为null导致的NullReferenceException
        /// 原因：反序列化时，未标记[DataMember]的字段可能被设置为null
        /// </summary>
        public void EnsureCollectionsInitialized()
        {
            if (this.Architectures == null)
                this.Architectures = new ArchitectureList();
            
            if (this.AvailableTechniques == null)
                this.AvailableTechniques = new TechniqueTable();
            
            if (this.BaseMilitaryKinds == null)
                this.BaseMilitaryKinds = new MilitaryKindTable();
            
            if (this.Informations == null)
                this.Informations = new InformationList();
            
            if (this.Legions == null)
                this.Legions = new LegionList();
            
            if (this.Routeways == null)
                this.Routeways = new RoutewayList();
            
            if (this.Sections == null)
                this.Sections = new SectionList();
            
            if (this.TechniqueMilitaryKinds == null)
                this.TechniqueMilitaryKinds = new MilitaryKindTable();
            
            if (this.Troops == null)
                this.Troops = new TroopList();
            
            if (this.TransferingMilitaries == null)
                this.TransferingMilitaries = new MilitaryList();
            
            if (this.KnownTroops == null)
                this.KnownTroops = new Dictionary<int, Troop>();
            
            if (this.ClosedRouteways == null)
                this.ClosedRouteways = new Dictionary<Point, object>();
            
            if (this.SecondTierKnownPaths == null)
                this.SecondTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
            
            if (this.ThirdTierKnownPaths == null)
                this.ThirdTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
            
            if (this.count == null)
                this.count = new Dictionary<PersonGeneratorType, int>();
            
            if (this.techniqueFundCostRateDecrease == null)
                this.techniqueFundCostRateDecrease = new List<float>();
            
            if (this.techniquePointCostRateDecrease == null)
                this.techniquePointCostRateDecrease = new List<float>();
            
            if (this.techniqueTimeRateDecrease == null)
                this.techniqueTimeRateDecrease = new List<float>();
            
            if (this.techniqueReputationRateDecrease == null)
                this.techniqueReputationRateDecrease = new List<float>();
        }

        /// <summary>
        /// 🆕 阶段 3：初始化全局能量地图
        /// 🧊 Cold Path：游戏启动时调用
        /// 🔥 日期：2026-03-16
        /// 🔥 重构：初始化为 TileInfluenceState[] 数组
        /// </summary>
        public void InitializeInfluenceMap(int mapWidth, int mapHeight)
        {
            GlobalInfluenceMap = new WorldOfTheThreeKingdoms.GameManager.TileInfluenceState[mapWidth * mapHeight];
            
            // 初始化所有地块状态
            for (int i = 0; i < GlobalInfluenceMap.Length; i++)
            {
                GlobalInfluenceMap[i].Reset();
            }
            
            _isInfluenceMapDirty = true;
            
            System.Diagnostics.Debug.WriteLine(
                $"[Faction] {Name} 初始化全局能量地图：{mapWidth}×{mapHeight}");
        }

        /// <summary>
        /// 🆕 阶段 3：标记能量地图为脏（需要重算）
        /// </summary>
        public void MarkInfluenceMapDirty()
        {
            _isInfluenceMapDirty = true;
        }

        // Removed obsolete ArchitecturesString - use ArchitectureIDs instead
        // 🔥 兼容性处理：从旧格式剧本文件加载
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("ArchitecturesString")]
        public string ArchitecturesString_Legacy
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ArchitectureIDs = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : -1)
                        .Where(id => id >= 0)
                        .ToList();
                    // System.Diagnostics.Debug.WriteLine($"[Faction.ArchitecturesString_Legacy] ID:{ID} Name:{Name ?? "null"} 转换了 {ArchitectureIDs.Count} 个建筑ID");
                }
            }
        }

        [DataMember]
        [JsonInclude]
        public List<int> ArchitectureIDs { get; set; } = [];  // 🔥 C# 12：集合表达式

        public ArchitectureList Architectures = new ArchitectureList();
        private int armyScale = 0;

        [DataMember]
        [JsonInclude]
        public bool AutoRefuse;

        [DataMember]
        [JsonInclude]
        public string AvailableTechniquesString { get; set; }

        public TechniqueTable AvailableTechniques = new TechniqueTable();

        [DataMember]
        [JsonInclude]
        public string BaseMilitaryKindsString { get; set; }

        public MilitaryKindTable BaseMilitaryKinds = new MilitaryKindTable();

        private Architecture capital;
        private int capitalID;

        public Dictionary<Point, object> ClosedRouteways = new Dictionary<Point, object>();
        private int colorIndex;
        private bool controlling;

        public int CriticalStrikeChanceIncrementWhileCombatMethodOfBubing;
        public int CriticalStrikeChanceIncrementWhileCombatMethodOfNubing;
        public int CriticalStrikeChanceIncrementWhileCombatMethodOfQibing;
        public int CriticalStrikeChanceIncrementWhileCombatMethodOfQixie;
        public int CriticalStrikeChanceIncrementWhileCombatMethodOfShuijun;
        public float DefenceRateOfBubing;
        public float DefenceRateOfNubing;
        public float DefenceRateOfQibing;
        public float DefenceRateOfQixie;
        public float DefenceRateOfShuijun;
        public float DefenceRateWhileCombatMethodOfBubing;
        public float DefenceRateWhileCombatMethodOfNubing;
        public float DefenceRateWhileCombatMethodOfQibing;
        public float DefenceRateWhileCombatMethodOfQixie;
        public float DefenceRateWhileCombatMethodOfShuijun;
        [DataMember]
        [JsonInclude]
        public bool Destroyed;

        public Color FactionColor;
        public int IncrementOfAntiCriticalStrikeChance;
        public int IncrementOfChaosDaysAfterPhisicalAttack;
        public int IncrementOfCombativityCeiling;
        public int IncrementOfCriticalStrikeChance;
        public int IncrementOfResistStratagemChance;
        public int IncrementOfRoutewayRadius;
        public int IncrementOfRoutewayWorkforce;
        public int IncrementOfStratagemSuccessChance;
        public int IncrementOfViewRadius;

        [DataMember]
        [JsonInclude]
        public string InformationsString { get; set; }

        public InformationList Informations = new InformationList();
        private Dictionary<Point, InformationTile> knownAreaData;
        
        // 🆕 能量情报独立存储（与建筑/部队视野分开）
        // 日期：2026-03-13
        // 说明：延迟初始化，不是掩盖数据错误
        private Dictionary<Point, InformationLevel>? energyBasedIntelligence;
        
        public Dictionary<int, Troop> KnownTroops = new Dictionary<int, Troop>();
        private Person leader = null;
        private int leaderID;
        private Person advisor = null;
        private int advisorID = -1;

        // Removed obsolete LegionsString - use LegionIDs instead

        [DataMember]
        [JsonInclude]
        public List<int> LegionIDs { get; set; } = new List<int>();

        public LegionList Legions = new LegionList();
        public InformationLevel LevelOfView = InformationLevel.中;
        private int[,] mapData;
        // public MilitaryList Militaries = new MilitaryList();

        public int NoCounterChanceIncrementOfBubing;
        public int NoCounterChanceIncrementOfNubing;
        public int NoCounterChanceIncrementOfQibing;
        public int NoCounterChanceIncrementOfQixie;
        public int NoCounterChanceIncrementOfShuijun;
        public int OffenceRadiusIncrementOfBubing;
        public int OffenceRadiusIncrementOfNubing;
        public int OffenceRadiusIncrementOfQibing;
        public int OffenceRadiusIncrementOfQixie;
        public int OffenceRadiusIncrementOfShuijun;
        public float OffenceRateOfBubing;
        public float OffenceRateOfNubing;
        public float OffenceRateOfQibing;
        public float OffenceRateOfQixie;
        public float OffenceRateOfShuijun;
        public float OffenceRateWhileCombatMethodOfBubing;
        public float OffenceRateWhileCombatMethodOfNubing;
        public float OffenceRateWhileCombatMethodOfQibing;
        public float OffenceRateWhileCombatMethodOfQixie;
        public float OffenceRateWhileCombatMethodOfShuijun;

        private bool passed;

        [DataMember]
        [JsonInclude]
        public int PlanTechniqueString { get; set; }

        public Technique PlanTechnique;
        public Architecture PlanTechniqueArchitecture;

        [DataMember]
        [JsonInclude]
        public List<int> PreferredTechniqueKinds = [];

        private bool preUserControlFinished = true;

        public float RateIncrementOfTerrainRate;
        public float RateOfCombativityRecoveryAfterAttacked;
        public float RateOfCombativityRecoveryAfterStratagemFail;
        public float RateOfCombativityRecoveryAfterStratagemSuccess;
        public float RateOfFoodTransportBetweenArchitectures = 1f;
        public float RateOfRoutewayConsumption = 1f;
        private int reputation;

        public RoutewayPathFinder RoutewayPathBuilder = new RoutewayPathFinder();

        [DataMember]
        [JsonInclude]
        public string RoutewaysString { get; set; }

        public RoutewayList Routeways = new RoutewayList();
        private Dictionary<ClosedPathEndpoints, List<Point>> SecondTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
        private int[,] secondTierMapCost;
        [DataMember]
        [JsonInclude]
        public int SecondTierXResidue = 0;
        [DataMember]
        [JsonInclude]
        public int SecondTierYResidue = 0;

        // Removed obsolete SectionsString - use SectionIDs instead
        // 🔥 兼容性处理：从旧格式剧本文件加载
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("SectionsString")]
        public string SectionsString_Legacy
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    SectionIDs = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : -1)
                        .Where(id => id >= 0)
                        .ToList();
                    // System.Diagnostics.Debug.WriteLine($"[Faction.SectionsString_Legacy] ID:{ID} Name:{Name ?? "null"} 转换了 {SectionIDs.Count} 个军区ID");
                }
            }
        }

        [DataMember]
        [JsonInclude]
        public List<int> SectionIDs { get; set; } = new List<int>();

        public SectionList Sections = new SectionList();

        public bool StopToControl;

        public MilitaryKindTable TechniqueMilitaryKinds = new MilitaryKindTable();
        private int techniquePoint;
        private int techniquePointForFacility;
        private int techniquePointForTechnique;
        private Dictionary<ClosedPathEndpoints, List<Point>> ThirdTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
        private int[,] thirdTierMapCost;
        [DataMember]
        [JsonInclude]
        public int ThirdTierXResidue = 0;
        [DataMember]
        [JsonInclude]
        public int ThirdTierYResidue = 0;

        // Removed obsolete TroopListString - use TroopIDs instead

        [DataMember]
        [JsonInclude]
        public List<int> TroopIDs { get; set; } = new List<int>();

        public TroopList Troops = new TroopList();
        // [DataMember]//后面有public的datamember
        private int upgradingDaysLeft;
        //  [DataMember]//后面有public的datamember
        private int upgradingTechnique = -1;
        // private Dictionary<MilitaryKind, int> militaryKindCounts = new Dictionary<MilitaryKind, int>();

        public List<float> techniqueReputationRateDecrease = new List<float>();
        public List<float> techniquePointCostRateDecrease = new List<float>();
        public List<float> techniqueTimeRateDecrease = new List<float>();
        public List<float> techniqueFundCostRateDecrease = new List<float>();

        [DataMember]
        [JsonInclude]
        public bool NotPlayerSelectable = false;

        public int[] CriticalOfMillitaryType = new int[5];
        public int[] AntiCriticalOfMillitaryType = new int[5];
        public float[] ArchitectureDamageOfMillitaryType = { 1f, 1f, 1f, 1f, 1f };
        public float[] SpeedOfMillitaryType = { 1f, 1f, 1f, 1f, 1f };
        public int[] ViewAreaOfMillitaryType = new int[5];
        public int[] StratagemOfMillitaryType = new int[5];
        public int[] AntiStratagemOfMillitaryType = new int[5];

        public String Counsellor;
        public String Governor;
        public String FiveTigers;

        public event AfterCatchLeader OnAfterCatchLeader;

        public event FactionDestroy OnFactionDestroy;

        public event ForcedChangeCapital OnForcedChangeCapital;

        public event GetControl OnGetControl;

        public event InitiativeChangeCapital OnInitiativeChangeCapital;

        public event TechniqueFinished OnTechniqueFinished;

        public event FactionUpgradeTechnique OnUpgradeTechnique;

        [DataMember]
        [JsonInclude]
        public List<int> PersonIDs { get; set; } = new List<int>();

        public PersonList Persons
        {
            get
            {
                PersonList result = new PersonList();
                //foreach (Person i in Session.Current.Scenario.Persons)
                //{
                //    if ((i.Status == GameObjects.PersonDetail.PersonStatus.Normal || i.Status == GameObjects.PersonDetail.PersonStatus.Moving)
                //        && i.BelongedFaction == this)
                //    {
                //        result.Add(i);
                //    }
                //}
                foreach (Architecture a in Architectures)
                {
                    foreach (Person p in a.Persons)
                        result.Add(p);
                    foreach (Person p in a.MovingPersons)
                        result.Add(p);
                }
                foreach (Troop t in Troops)
                {
                    foreach (Person p in t.Persons)
                        result.Add(p);
                }
                
                // 🔥 防御性检查：Session未初始化时跳过Captives
                if (Session.Current?.Scenario?.Captives != null)
                {
                    foreach (Captive c in Session.Current.Scenario.Captives)
                    {
                        if (c.CaptiveFaction == this)
                        {
                            result.Add(c.CaptivePerson);
                        }
                    }
                }

                return result;
            }
        }

        public PersonList MayorList
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Architecture a in this.Architectures)
                {
                    if (a.Mayor != null)
                    {
                        result.Add(a.Mayor);
                    }
                }
                return result;
            }
        }
        /*
        public PersonList ConvinceableMayorList   //可劝降的太守列表
        {
            get
            {
                PersonList list = new PersonList();
                foreach (Faction f in Session.Current.Scenario.Factions)
                {
                    foreach (Architecture a in f.Architectures)
                    {
                        if (f != this && a.Mayor != null)
                        {
                            list.Add(a.Mayor);
                        }
                    }
                }
                return list;
            }
        }
 
        public PersonList ConvinceableLeaderList //可劝降的君主列表
        {
            get
            {
                 PersonList list = new PersonList();
                 foreach (Faction f in Session.Current.Scenario.Factions )
                 {
                     if (f != this )
                     {
                         list.Add(f.Leader);
                     }
                 }
                return list ;
            }
        }
        */


        /// <summary>
        /// 获取势力所有宝物（君主除外）
        /// </summary>
        public TreasureList AllTreasuresExceptLeader
        {
            get
            {
                TreasureList list = new TreasureList();
                foreach (Person person in this.Persons)
                {
                    if (person != this.Leader)
                    {
                        person.AddTreasureToList(list);
                    }
                }
                return list;
            }
        }

        public PersonList PersonsInArchitecturesExceptLeader
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Architecture a in Architectures)
                {
                    foreach (Person p in a.Persons)
                    {
                        if (p != this.Leader) result.Add(p);
                    }
                }
                return result;
            }
        }

        public CaptiveList Captives
        {
            get
            {
                CaptiveList result = new CaptiveList();
                foreach (Captive i in Session.Current.Scenario.Captives)
                {
                    if (i.BelongedFaction == this)
                    {
                        result.Add(i);
                    }
                }
                return result;
            }
        }

        public CaptiveList SelfCaptives
        {
            get
            {
                CaptiveList result = new CaptiveList();
                foreach (Captive i in Session.Current.Scenario.Captives)
                {
                    if (i.CaptiveFaction == this)
                    {
                        result.Add(i);
                    }
                }
                return result;
            }
        }

        public ArchitectureList ArchitecturesExcluding(Architecture a)
        {
            ArchitectureList result = new ArchitectureList();
            foreach (Architecture i in this.Architectures)
            {
                if (i != a)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public int GetTechniqueUsefulness(Technique tech)
        {
            int result = 0;
            foreach (Influences.Influence i in tech.Influences.GetInfluenceList())
            {
                switch (i.Kind.ID)
                {
                    case 1030:
                    case 2400:
                    case 2420:
                    case 2430:
                        if (!Session.GlobalVariables.LiangdaoXitong) break;
                        result = Math.Max(100, result);
                        break;
                    case 2000:
                    case 2010:
                    case 2020:
                    case 2030:
                    case 2200:
                    case 2210:
                    case 2220:
                    case 2230:
                    case 2240:
                    case 2250:
                        if (int.Parse(i.Parameter) == 3)
                        {
                            bool hasWater = false;
                            foreach (Architecture a in this.Architectures)
                            {
                                if (a.IsBesideWater)
                                {
                                    hasWater = true;
                                    break;
                                }
                            }
                            if (!hasWater) break;
                        }
                        else if (int.Parse(i.Parameter) == 4)
                        {
                            bool hasSiege = false;
                            foreach (MilitaryKind mk in this.AvailableMilitaryKinds.MilitaryKinds.Values)
                            {
                                if (mk.Type == MilitaryType.器械)
                                {
                                    hasSiege = true;
                                }
                            }
                            if (!hasSiege) break;
                        }
                        else if (int.Parse(i.Parameter) == 0)
                        {
                            bool hasSiege = false;
                            foreach (MilitaryKind mk in this.AvailableMilitaryKinds.MilitaryKinds.Values)
                            {
                                if (mk.Type == MilitaryType.步兵)
                                {
                                    hasSiege = true;
                                }
                            }
                            if (!hasSiege) break;
                        }
                        else if (int.Parse(i.Parameter) == 1)
                        {
                            bool hasSiege = false;
                            foreach (MilitaryKind mk in this.AvailableMilitaryKinds.MilitaryKinds.Values)
                            {
                                if (mk.Type == MilitaryType.弩兵)
                                {
                                    hasSiege = true;
                                }
                            }
                            if (!hasSiege) break;
                        }
                        else if (int.Parse(i.Parameter) == 2)
                        {
                            bool hasSiege = false;
                            foreach (MilitaryKind mk in this.AvailableMilitaryKinds.MilitaryKinds.Values)
                            {
                                if (mk.Type == MilitaryType.骑兵)
                                {
                                    hasSiege = true;
                                }
                            }
                            if (!hasSiege) break;
                        }
                        result = Math.Max(100, result);
                        break;
                    default:
                        result = Math.Max(100, result);
                        break;
                }
            }
            return result;
        }

        public void AddArchitecture(Architecture architecture)
        {
            this.Architectures.Add(architecture);
            if (architecture.BelongedFaction != null)
            {
                if (architecture.BelongedFaction == this)
                {
                    return;
                }
                architecture.BelongedFaction.RemoveArchitecture(architecture);
            }
            architecture.BelongedFaction = this;
        }

        /// <summary>
        /// 判断某个位置（据点ID）是否属于本势力的“已知区域”
        /// </summary>
        /// <param name="locationId">目标据点的ID</param>
        /// <returns>true表示已知，false表示未知</returns>
        public bool IsCityKnown(int locationId)
        {
            // 军师智力100+则全知
            if (this.Advisor != null && this.Advisor.Intelligence >= 100) return true;

            GameScenario scenario = Session.Current.Scenario;
            Architecture targetArch = scenario.Architectures.GetGameObject(locationId) as Architecture;

            // 容错处理：如果地点不存在（比如特殊状态），视为未知
            if (targetArch == null) return false;

            // --- 条件1：是我方领土 ---
            if (targetArch.BelongedFaction == this)
            {
                return true;
            }

            // --- 条件2：是我方领土的相邻据点 ---
            // 遍历我方所有据点，检查目标是否与它们相连
            foreach (Architecture myArch in this.Architectures)
            {
                if (IsConnected(myArch, targetArch))
                {
                    return true;
                }
            }

            return false;
        }

        // 辅助方法：检查两个据点是否有直接连接
        private bool IsConnected(Architecture a1, Architecture a2)
        {
            if (a1.AILandLinks.HasGameObject(a2)) return true;
            if (a1.AIWaterLinks.HasGameObject(a2)) return true;
            return false;
        }

        public List<Point> GetAllKnownArea()
        {
            List<Point> result = new List<Point>();
            if (this.knownAreaData == null) return result;
            foreach (Point p in this.knownAreaData.Keys)
            {
                if (this.GetKnownAreaData(p) != InformationLevel.无 && this.GetKnownAreaData(p) != InformationLevel.未知)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        private bool? _cachedGlobalConvinceTargetAvailable = null;

        private TroopList visibleTroopsCache = null;
        public TroopList GetVisibleTroops()
        {
            if (visibleTroopsCache != null)
            {
                return visibleTroopsCache;
            }
            else
            {
                TroopList result = new TroopList();
                foreach (Point p in this.GetAllKnownArea())
                {
                    Troop troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(p);
                    if ((troopByPositionNoCheck != null) && !troopByPositionNoCheck.Destroyed)
                    {
                        result.Add(troopByPositionNoCheck);
                    }
                }
                visibleTroopsCache = result;
                return result;
            }
        }

        private void AddKnownAreaData(Point p, InformationLevel level)
        {
            if (this.knownAreaData == null)
                this.knownAreaData = new Dictionary<Point, InformationTile>();
            if (!this.knownAreaData.ContainsKey(p))
            {
                InformationTile it = new InformationTile();
                it.AddInformationLevel(level);
                this.knownAreaData.Add(p, it);
            }
            else
            {
                InformationTile it = this.knownAreaData[p];
                it.AddInformationLevel(level);
                this.knownAreaData[p] = it;
            }
        }

        /// <summary>
        /// 🆕 清除能量情报（在重新计算能量前调用）
        /// 日期：2026-03-13
        /// </summary>
        internal void ClearEnergyBasedIntelligence()
        {
            energyBasedIntelligence?.Clear();
        }

        /// <summary>
        /// 🆕 添加能量情报（独立于建筑/部队视野）
        /// 日期：2026-03-13
        /// </summary>
        internal void AddEnergyBasedIntelligence(Point p, InformationLevel level)
        {
            energyBasedIntelligence ??= new Dictionary<Point, InformationLevel>();
            energyBasedIntelligence[p] = level;
        }

        private void RemoveKnownAreaData(Point p, InformationLevel level)
        {
            if (this.knownAreaData == null) return;
            if (this.knownAreaData.ContainsKey(p))
            {
                InformationTile it = this.knownAreaData[p];
                it.RemoveInformationLevel(level);
                this.knownAreaData[p] = it;
                if (it.Level == InformationLevel.无)
                {
                    this.knownAreaData.Remove(p);
                }
            }
        }

        /// <summary>
        /// 🆕 修改查询方法：合并建筑视野和能量情报
        /// 日期：2026-03-13
        /// 性能：Hot Path - 只有字典查找，无分配
        /// </summary>
        private InformationLevel getInformationLevel(Point p)
        {
            // 1. 获取建筑/部队视野情报
            InformationLevel baseLevel = InformationLevel.无;
            if (knownAreaData?.TryGetValue(p, out var info) == true)
            {
                baseLevel = info.Level;
            }

            // 2. 获取能量情报
            InformationLevel energyLevel = InformationLevel.无;
            if (energyBasedIntelligence?.TryGetValue(p, out var eLevel) == true)
            {
                energyLevel = eLevel;
            }

            // 3. 返回两者中的最高等级
            return (InformationLevel)Math.Max((int)baseLevel, (int)energyLevel);
        }

        public InformationLevel GetInformationLevel(Point p)
        {
            return this.getInformationLevel(p);
        }

        public void AddArchitectureKnownData(Architecture a)
        {
            foreach (Point point in a.ArchitectureArea.Area)
            {
                this.AddKnownAreaData(point, InformationLevel.全);
            }
            foreach (Point point in a.ViewArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    this.AddKnownAreaData(point, InformationLevel.高);
                }
            }
            if (a.Kind != null && a.Kind.HasLongView)
            {
                foreach (Point point in a.LongViewArea.Area)
                {
                    if (!Session.Current.Scenario.PositionOutOfRange(point))
                    {
                        this.AddKnownAreaData(point, InformationLevel.中);
                    }
                }
            }
        }

        public void AddArchitectureMilitaries(Architecture architecture)
        {
            foreach (Military military in architecture.Militaries)
            {
                this.AddMilitary(military);
            }
        }

        public void AddInformation(Information information)
        {
            this.Informations.AddInformation(information);
            information.BelongedFaction = this;
        }

        public void AddLegion(Legion legion)
        {
            this.Legions.Add(legion);
            legion.BelongedFaction = this;
        }

        public void AddMilitary(Military military)
        {
            this.Militaries.AddMilitary(military);
            /* if (this.militaryKindCounts.ContainsKey(military.RealMilitaryKind))
             {
                 this.militaryKindCounts[military.Kind]++;
             }
             else
             {
                 this.militaryKindCounts[military.Kind] = 1;
             }*/
            military.BelongedFaction = this;
        }

        public void AddPositionInformation(Point position, InformationLevel level)
        {
            if (!Session.Current.Scenario.PositionOutOfRange(position))
            {
                this.AddKnownAreaData(position, level);
            }
        }

        public void AddRouteway(Routeway routeway)
        {
            this.Routeways.AddRoutewayWithEvent(routeway);
            routeway.BelongedFaction = this;
        }

        public void AddSecondTierKnownPath(List<Point> path)
        {
            if (path != null)
            {
                ClosedPathEndpoints key = new ClosedPathEndpoints(path[0], path[path.Count - 1]);
                if (!this.SecondTierKnownPaths.ContainsKey(key))
                {
                    if (this.SecondTierKnownPaths.Count > Session.GlobalVariables.MaxCountOfKnownPaths)
                    {
                        this.SecondTierKnownPaths.Clear();
                    }
                    this.SecondTierKnownPaths.Add(key, path);
                }
            }
        }

        public void AddSection(Section section)
        {
            this.Sections.Add(section);
            section.BelongedFaction = this;
            section.BelongedFactionID = this.ID; // 🔥 修复：同步 ID
        }

        public void AddTechniqueMilitaryKind(int kindID)
        {
            MilitaryKind militaryKind = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(kindID);
            if (militaryKind != null)
            {
                this.TechniqueMilitaryKinds.AddMilitaryKind(militaryKind);
            }
        }

        public void AddThirdTierKnownPath(List<Point> path)
        {
            if (path != null)
            {
                ClosedPathEndpoints key = new ClosedPathEndpoints(path[0], path[path.Count - 1]);
                if (!this.ThirdTierKnownPaths.ContainsKey(key))
                {
                    if (this.ThirdTierKnownPaths.Count > Session.GlobalVariables.MaxCountOfKnownPaths)
                    {
                        this.ThirdTierKnownPaths.Clear();
                    }
                    this.ThirdTierKnownPaths.Add(key, path);
                }
            }
        }

        public void AddTroop(Troop troop)
        {
            this.Troops.Add(troop);
            if (troop.BelongedFaction != null)
            {
                if (troop.BelongedFaction == this)
                {
                    return;
                }
                troop.BelongedFaction.RemoveTroop(troop);
            }
            troop.BelongedFaction = this;

            // 🔧 FIX: 只在部队有明确目标时才自动分配军团
            // 使用willArchitectureID而不是WillArchitecture属性，避免触发getter的自动设置逻辑
            if (troop.BelongedLegion == null && troop.WillArchitectureID >= 0)
            {
                Architecture targetArch = troop.WillArchitecture;

                if (targetArch != null)
                {
                    // 1. 尝试获取现有军团
                    troop.BelongedLegion = this.GetLegion(targetArch);

                    // 2. 如果没找到，创建一个新的默认军团
                    if (troop.BelongedLegion == null)
                    {
                        troop.BelongedLegion = this.CreateDefaultLegion(targetArch);
                    }

                    // 3. 确保双向引用：将部队加入到军团的列表中
                    if (troop.BelongedLegion != null && !troop.BelongedLegion.Troops.HasGameObject(troop))
                    {
                        troop.BelongedLegion.AddTroop(troop);
                    }
                }
            }
        }

        public void AddTroopKnownAreaData(Troop troop)
        {
            foreach (Point point in troop.ViewArea.Area)
            {
                if (Session.Current.Scenario.PositionOutOfRange(point))
                {
                    continue;
                }
                if (point == troop.ViewArea.Centre)
                {
                    this.AddKnownAreaData(point, InformationLevel.全);
                }
                else
                {
                    this.AddKnownAreaData(point, troop.ScoutLevel);
                }
            }
        }

        public void AddTroopMilitary(Troop troop)
        {
            //兼容以前的旧存档？
            if (troop.Army != null)
            {
                if (troop.Army.ShelledMilitary == null)
                {
                    this.AddMilitary(troop.Army);
                }
                else
                {
                    this.AddMilitary(troop.Army.ShelledMilitary);
                }
            }
        }

        public void AdjustByArchitecture(Architecture architecture, int cost)
        {
            foreach (Point point in architecture.ArchitectureArea.Area)
            {
                this.architectureAdjustCost[point.X, point.Y] = cost;
            }
        }

        protected void AdjustByArchitectures()
        {
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (!architecture.IsFriendly(this))
                {
                    this.AdjustByArchitecture(architecture, 0xdac);
                }
            }
        }

        public void AdjustMapCost()
        {
            this.AdjustByArchitectures();
        }

        public bool IsPersonForHouGong(Person p, bool forced, bool alreadyTaken = false)
        {
            if (!this.Leader.isLegalFeiZiExcludeAge(p) || !p.isLegalFeiZiExcludeAge(this.Leader)) return false;

            if (p.Sex && p.Age >= 45) return false;

            if (this.Leader.Sex && this.Leader.Age >= 45) return false;

            if (p.Spouse == this.Leader || this.Leader.Spouse == p) return true;

            if (p.BelongedFaction != null && p.marriageGranter == p.BelongedFaction.Leader)
            {
                return false;
            }

            bool hasSon = false;
            if (this.Leader.NumberOfChildren > 0)
            {
                foreach (Person q in this.Leader.ChildrenList)
                {
                    if (!q.Sex)
                    {
                        hasSon = true;
                    }
                }
            }

            int unAmbition = Enum.GetNames(typeof(PersonAmbition)).Length - (int)this.Leader.Ambition;
            bool take = (p.UntiredMerit > ((unAmbition - 1) * Session.Parameters.AINafeiAbilityThresholdRate) || !hasSon) &&
                                        (!((bool)Session.GlobalVariables.PersonNaturalDeath) || (p.Age >= 16 && (p.Age <= Session.Parameters.AINafeiMaxAgeThresholdAdd + (int)leader.Ambition * Session.Parameters.AINafeiMaxAgeThresholdMultiply || !hasSon))) &&
                                        p.marriageGranter != this.Leader && !p.Hates(this.Leader);

            Person hater = WillHateLeaderDueToAffair(this.Leader, p, false, forced);

            if (this.IsAlien && (hater == null || hater.PersonalLoyalty >= 2))
            {
                if (hater == null || hater.BelongedFaction != this)
                {
                    return true;
                }
            }

            if (hater != null)
            {
                if (this.leader.PersonalLoyalty >= 4) return false;
                if (this.leader.PersonalLoyalty >= 3 && this.leader.NumberOfChildren > 0) return false;
                if (this.leader.PersonalLoyalty >= 2 && (p.PersonalLoyalty >= 4 || (p.PersonalLoyalty >= 2 && p.Spouse != null && p.Spouse.Alive))) return false;
            }

            return (take || alreadyTaken) && (hater == null || (leader.PersonalLoyalty <= (int)PersonLoyalty.普通 && hater.UnalteredUntiredMerit * (leader.PersonalLoyalty * Session.Parameters.AINafeiStealSpouseThresholdRateMultiply + Session.Parameters.AINafeiStealSpouseThresholdRateAdd) < this.Leader.UnalteredUntiredMerit));
        }

        private Person WillHateLeaderDueToAffair(Person p, Person q, bool pForced, bool qForced)
        {
            Dictionary<Person, PersonList> haters = Person.willHateCausedByAffair(p, q, this.Leader, pForced, qForced);
            PersonList leaderHaters = new PersonList();
            foreach (KeyValuePair<Person, PersonList> i in haters)
            {
                if (i.Value.HasGameObject(this.Leader))
                {
                    leaderHaters.Add(i.Key);
                }
            }

            Person spousePerson = null;
            int maxMerit = 0;
            foreach (Person i in leaderHaters)
            {
                if (i.Alive && i != this.Leader && !i.Hates(this.Leader) && i.UntiredMerit > maxMerit)
                {
                    spousePerson = i;
                    maxMerit = i.UntiredMerit;
                }
            }

            return spousePerson;
        }

        public void AIActuallyMakeMarriage(Person p, Person q)
        {
            if (p.LocationArchitecture == q.LocationArchitecture && p.LocationArchitecture != null &&
                                p.LocationArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
            {
                if (p.WaitForFeiZi != null)
                {
                    p.WaitForFeiZi.WaitForFeiZi = null;
                }
                if (q.WaitForFeiZi != null)
                {
                    q.WaitForFeiZi.WaitForFeiZi = null;
                }
                p.Marry(q, this.Leader);
                p.WaitForFeiZi = null;
                q.WaitForFeiZi = null;
            }
            else
            {
                if (p.WaitForFeiZi != null)
                {
                    p.WaitForFeiZi.WaitForFeiZi = null;
                }
                if (q.WaitForFeiZi != null)
                {
                    q.WaitForFeiZi.WaitForFeiZi = null;
                }
                p.WaitForFeiZi = q;
                q.WaitForFeiZi = p;
                if (p.LocationArchitecture != q.LocationArchitecture)
                {
                    if (p.Status == PersonStatus.Captive && q.LocationTroop == null)
                    {
                        if (p.LocationArchitecture != null && p.LocationArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
                        {
                            q.MoveToArchitecture(p.LocationArchitecture);
                        }
                    }
                    else if (q.Status == PersonStatus.Captive && p.LocationTroop == null)
                    {
                        if (q.LocationArchitecture != null && q.LocationArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
                        {
                            p.MoveToArchitecture(q.LocationArchitecture);
                        }
                    }
                    else if (q.Status == PersonStatus.Normal && !q.NvGuan && q.LocationArchitecture != null && q.LocationTroop == null &&
                        p.BelongedArchitecture != null && p.BelongedArchitecture.BelongedFaction == p.BelongedFaction)
                    {
                        if (p.BelongedArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
                        {
                            q.MoveToArchitecture(p.BelongedArchitecture);
                        }
                    }
                    else if (p.Status == PersonStatus.Normal && !p.NvGuan && p.LocationArchitecture != null && p.LocationTroop == null &&
                        q.BelongedArchitecture != null && q.BelongedArchitecture.BelongedFaction == p.BelongedFaction)
                    {
                        if (q.BelongedArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
                        {
                            p.MoveToArchitecture(q.BelongedArchitecture);
                        }
                    }
                    else
                    {
                        p.WaitForFeiZi = null;
                        q.WaitForFeiZi = null;
                    }
                }
            }
        }

        private void AIMakeMarriage()
        {
            if (Session.Current.Scenario.IsPlayer(this)) return;

            if (this.Leader.Status == PersonStatus.Captive) return;

            foreach (Person p in this.Persons)
            {
                if (p.WaitForFeiZi != null)
                {
                    if ((p.BelongedFaction != this || p.Spouse != null
                        || (p.WaitForFeiZi.BelongedFaction != this && p.WaitForFeiZi.Spouse != null)))
                    {
                        if (p.WaitForFeiZi != null)
                        {
                            p.WaitForFeiZi.WaitForFeiZi = null;
                        }
                        p.WaitForFeiZi = null;
                    }
                    else if (!p.isLegalFeiZiExcludeAge(p.WaitForFeiZi) || p.WaitForFeiZi.isLegalFeiZiExcludeAge(p))
                    {
                        if (p.WaitForFeiZi != null)
                        {
                            p.WaitForFeiZi.WaitForFeiZi = null;
                        }
                        p.WaitForFeiZi = null;
                    }
                    else
                    {
                        if (p.Status == PersonStatus.Normal && p.LocationArchitecture != null
                            && p.LocationTroop == null)
                        {
                            if (p.LocationArchitecture.Fund >= Session.Parameters.MakeMarriageCost)
                            {
                                p.Marry(p.WaitForFeiZi, this.Leader);
                                if (p.WaitForFeiZi != null)
                                {
                                    p.WaitForFeiZi.WaitForFeiZi = null;
                                }
                                p.WaitForFeiZi = null;
                            }
                        }
                    }
                }
            }

            if (GameObject.Random(10) == 0 && leader.Status == PersonStatus.Normal && leader.LocationArchitecture != null)
            {
                if (leader.WaitForFeiZi == null && leader.Age <= 30 + leader.Ambition * 10)
                {
                    PersonList leaderMarryable = this.Leader.MakeAnyMarryableInFaction();
                    if (leaderMarryable.Count > 0)
                    {
                        Person q = this.Leader;
                        leaderMarryable.PropertyName = "UntiredMerit";
                        leaderMarryable.IsNumber = true;
                        leaderMarryable.SmallToBig = false;
                        leaderMarryable.ReSort();
                        foreach (Person p in leaderMarryable)
                        {
                            if (p.WaitForFeiZi == null && Math.Abs(p.Age - q.Age) <= 15 + leader.Ambition * leader.Ambition * 2 && IsPersonForHouGong(p, p.IsCaptive))
                            {
                                Person hater = WillHateLeaderDueToAffair(p, q, p.IsCaptive, false);
                                if (hater != null && hater != p && hater != q) continue;

                                AIActuallyMakeMarriage(q, p);
                                break;
                            }
                        }
                    }
                }

                GameObjectList pl = this.Persons.GetList();
                pl.PropertyName = "UntiredMerit";
                pl.IsNumber = true;
                pl.SmallToBig = false;
                pl.ReSort();
                foreach (Person p in pl)
                {
                    if (p.WaitForFeiZi != null) continue;
                    if (p.Spouse != null) continue;
                    if (p.Status != PersonStatus.Normal) continue;
                    PersonList allCandidates = p.MakeAnyMarryableInFaction();
                    PersonList candidates = new PersonList();
                    foreach (Person q in allCandidates)
                    {
                        if ((q.Spouse == null || q.Spouse == p) && Math.Abs(q.Age - p.Age) <= 15)
                        {
                            candidates.Add(q);
                        }
                    }
                    if (candidates.Count > 0)
                    {
                        Person q = candidates.GetMaxUntiredMeritPerson();
                        if (q.WaitForFeiZi == null)
                        {
                            if (this.hougongValid)
                            {
                                if ((IsPersonForHouGong(p, p.IsCaptive) || IsPersonForHouGong(q, q.IsCaptive)) && !(this.Leader == p || this.Leader == q)) continue;
                            }

                            Person hater = WillHateLeaderDueToAffair(p, q, p.IsCaptive, q.IsCaptive);
                            if (hater != null) continue;

                            AIActuallyMakeMarriage(q, p);
                            break;
                        }
                    }
                }
            }
        }

        public int TotalFeiziCount()
        {
            int count = 0;
            foreach (Architecture a in this.Architectures)
            {
                count += a.Feiziliebiao.Count;
            }
            return count;
        }

        public int TotalFeiziSpaceCount()
        {
            int count = 0;
            foreach (Architecture a in this.Architectures)
            {
                count += a.Meinvkongjian;
            }
            return count;
        }

        private void AIHouGong()
        {
            if (Session.Current.Scenario.IsPlayer(this)) return;

            int uncruelty = this.Leader.Uncruelty;
            int unAmbition = Enum.GetNames(typeof(PersonAmbition)).Length - (int)this.Leader.Ambition;

            // move
            foreach (Architecture a in this.Architectures)
            {
                if (a.Feiziliebiao.Count > 0)
                {
                    Architecture dest = null;
                    int maxPop = 0;
                    foreach (Architecture b in this.Architectures)
                    {
                        if (!b.withoutTruceFrontline && !b.JustAttacked && !b.HasHostileTroopsInView() &&
                            (b.Meinvkongjian > b.Feiziliebiao.Count || b.BelongedFaction.IsAlien))
                        {
                            if (b.Endurance > maxPop)
                            {
                                maxPop = b.Endurance;
                                dest = b;
                            }
                        }
                    }
                    if (dest == null)
                    {
                        foreach (Architecture b in this.Architectures)
                        {
                            if (b.RecentlyAttacked <= 0 && b.RecentlyBreaked <= 0 &&
                                (b.Meinvkongjian > b.Feiziliebiao.Count || b.BelongedFaction.IsAlien))
                            {
                                if (b.Endurance > maxPop)
                                {
                                    maxPop = b.Endurance;
                                    dest = b;
                                }
                            }
                        }
                    }
                    if (dest == null)
                    {
                        foreach (Architecture b in this.Architectures)
                        {
                            if (b.Endurance > 30 &&
                                (b.Meinvkongjian > b.Feiziliebiao.Count || b.BelongedFaction.IsAlien))
                            {
                                if (b.Endurance > maxPop)
                                {
                                    maxPop = b.Endurance;
                                    dest = b;
                                }
                            }
                        }
                    }
                    if (dest != null)
                    {
                        int cnt = dest.BelongedFaction.IsAlien ? 9999 : dest.Meinvkongjian - dest.Feiziliebiao.Count;
                        GameObjectList list = a.Feiziliebiao.GetList();
                        list.PropertyName = "Merit";
                        list.IsNumber = true;
                        list.SmallToBig = false;
                        list.ReSort();
                        int moved = 0;
                        foreach (Person p in list)
                        {
                            if (p.ArrivingDays <= 0 && p.LocationArchitecture != dest)
                            {
                                p.MoveToArchitecture(dest);
                                moved++;
                                if (moved >= cnt) break;
                            }
                        }
                    }
                }
            }

            if (GameObject.Random(10) == 0)
            {
                //release
                foreach (Architecture a in this.Architectures)
                {
                    foreach (Person p in a.ReleasableFeizis)
                    {
                        if (!IsPersonForHouGong(p, true))
                        {
                            if (!this.Leader.suoshurenwuList.HasGameObject(p) && (uncruelty <= 4 || !p.Hates(this.Leader)) && (uncruelty <= 8 || p.RecruitableBy(this, 0)))
                            {
                                p.feiziRelease();
                            }
                        }
                    }
                }
            }

            if (this.Leader.NumberOfChildren >= Session.GlobalVariables.OfficerChildrenLimit) return;

            if (this.Leader.Age <= 12) return;

            if (this.hougongValid)
            {

                // build hougong
                if (this.meinvkongjian() - this.feiziCount() <= 0 && !this.isAlien && TotalFeiziSpaceCount() < Session.Current.Scenario.Parameters.AIMaxFeizi &&
                    GameObject.Random((int)(GameObject.Square(unAmbition) * Session.Parameters.AIBuildHougongUnambitionProbWeight + GameObject.Square(this.meinvkongjian()) * unAmbition * Session.Parameters.AIBuildHougongSpaceBuiltProbWeight)) == 0)
                {
                    Architecture buildAt = null;
                    bool planned = false;
                    foreach (Architecture a in this.Architectures)
                    {
                        if (a.FrontLine) continue;
                        if (a.ExpectedFund - a.EnoughFund <= 50 * 30) continue;
                        if (a.Kind.FacilityPositionUnit <= 0) continue;
                        if (a.PlanFacilityKind != null && a.PlanFacilityKind.rongna > 0)
                        {
                            planned = true;
                            break;
                        }
                        if (a.BuildingFacility >= 0 && Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKind(a.BuildingFacility).rongna > 0)
                        {
                            planned = true;
                            break;
                        }

                        if (buildAt == null || a.Population > buildAt.Population)
                        {
                            buildAt = a;
                        }
                    }

                    if (!planned && buildAt != null)
                    {
                        int maxHgSize = (12 - uncruelty) + Math.Max(0, buildAt.FacilityPositionCount / buildAt.Kind.FacilityPositionUnit - 5) + Session.Parameters.AIBuildHougongMaxSizeAdd;
                        FacilityKind hougong = null;
                        foreach (FacilityKind fk in Session.Current.Scenario.GameCommonData.AllFacilityKinds.FacilityKinds.Values)
                        {
                            if (!fk.CanBuild(buildAt)) continue;
                            if (fk.FundCost > buildAt.Fund) continue;
                            if (fk.rongna > 0 && fk.rongna < maxHgSize && GameObject.Chance(Session.Parameters.AIBuildHougongSkipSizeChance))
                            {
                                if (hougong == null || hougong.rongna < fk.rongna)
                                {
                                    hougong = fk;
                                }
                            }
                        }
                        if (hougong != null)
                        {
                            int facilityPositionLeft = buildAt.FacilityPositionLeft;
                            if (facilityPositionLeft < hougong.PositionOccupied && buildAt.FacilityPositionCount >= hougong.PositionOccupied)
                            {
                                FacilityList fl = new FacilityList();
                                foreach (Facility f in buildAt.Facilities)
                                {
                                    if (f.location.CanRemoveFacility(f))
                                    {
                                        fl.Add(f);
                                    }
                                }

                                int totalRemovableSpace = 0;
                                foreach (Facility f in fl)
                                {
                                    totalRemovableSpace += f.PositionOccupied;
                                }

                                if (totalRemovableSpace >= hougong.PositionOccupied)
                                {
                                    fl.PropertyName = "AIValue";
                                    fl.IsNumber = true;
                                    fl.SmallToBig = true;
                                    fl.ReSort();

                                    while (buildAt.FacilityPositionLeft < hougong.PositionOccupied && fl.Count > 0)
                                    {
                                        Facility f = fl[0] as Facility;
                                        if (buildAt.FacilityEnabled || f.MaintenanceCost <= 0)
                                        {
                                            f.Influences.PurifyInfluence(this, Applier.Facility, f.ID);
                                        }
                                        buildAt.Facilities.Remove(f);
                                        Session.Current.Scenario.Facilities.Remove(f);
                                        fl.Remove(f);
                                    }
                                }

                                facilityPositionLeft = buildAt.FacilityPositionLeft;
                            }
                            if (facilityPositionLeft >= hougong.PositionOccupied)
                            {
                                if ((this.Fund >= hougong.FundCost) && ((buildAt.BelongedFaction.TechniquePoint + buildAt.BelongedFaction.TechniquePointForFacility) >= hougong.PointCost))
                                {
                                    buildAt.PlanFacilityKind = null;
                                    buildAt.BelongedFaction.DepositTechniquePointForFacility(hougong.PointCost);
                                    buildAt.BeginToBuildAFacility(hougong);
                                }
                                else
                                {
                                    buildAt.PlanFacilityKind = hougong;
                                    if (GameObject.Chance(0x21) && ((buildAt.BelongedFaction.TechniquePoint + buildAt.BelongedFaction.TechniquePointForFacility) < buildAt.PlanFacilityKind.PointCost))
                                    {
                                        buildAt.BelongedFaction.SaveTechniquePointForFacility(buildAt.PlanFacilityKind.PointCost / buildAt.PlanFacilityKind.Days);
                                    }
                                }
                            }
                        }
                    }
                }

                //nafei
                if (leader.WaitForFeiZi != null && leader.Status == PersonStatus.Normal && leader.LocationArchitecture != null &&
                    this.Leader.LocationTroop == null)
                {
                    if ((this.Leader.LocationArchitecture.Meinvkongjian - this.Leader.LocationArchitecture.Feiziliebiao.Count <= 0 && !this.IsAlien) ||
                        !this.Leader.isLegalFeiZiExcludeAge(leader.WaitForFeiZi) ||
                        this.Leader.WaitForFeiZi.BelongedFaction != this)
                    {
                        leader.WaitForFeiZi.WaitForFeiZi = null;
                        leader.WaitForFeiZi = null;
                    }
                    else if (this.Leader.LocationArchitecture.Fund >= Session.Parameters.NafeiCost)
                    {
                        if (this.Leader.WaitForFeiZi.LocationArchitecture == this.Leader.LocationArchitecture &&
                            this.Leader.WaitForFeiZi.Status == PersonStatus.Normal)
                        {
                            this.Leader.XuanZeMeiNv(this.Leader.WaitForFeiZi);
                            this.Leader.WaitForFeiZi.WaitForFeiZi = null;
                            this.Leader.WaitForFeiZi = null;
                        }
                    }
                }
                else if (this.Leader.Status == PersonStatus.Normal && this.Leader.LocationArchitecture != null &&
                    this.Leader.LocationTroop == null && this.Leader.WaitForFeiZi == null && (TotalFeiziCount() < Session.Current.Scenario.Parameters.AIMaxFeizi || this.IsAlien))
                {
                    Architecture dest = null;
                    if ((this.Leader.LocationArchitecture.Meinvkongjian - this.Leader.LocationArchitecture.Feiziliebiao.Count > 0 &&
                        this.Leader.LocationArchitecture.Fund >= Session.Parameters.NafeiCost + this.Leader.LocationArchitecture.EnoughFund) || this.IsAlien)
                    {
                        dest = this.Leader.LocationArchitecture;
                    }
                    else
                    {
                        foreach (Architecture a in this.Architectures)
                        {
                            if (((a.Meinvkongjian - a.Feiziliebiao.Count > 0 && a.Fund >= Session.Parameters.NafeiCost + a.EnoughFund) || this.IsAlien)
                                && (dest == null || a.Population > dest.Population))
                            {
                                dest = a;
                            }
                        }
                    }

                    if (dest != null)
                    {
                        PersonList candidate = new PersonList();
                        foreach (Architecture a in this.Architectures)
                        {
                            foreach (Person p in a.nvxingwujiang())
                            {
                                if (!this.Leader.isLegalFeiZiExcludeAge(p) || !p.isLegalFeiZiExcludeAge(this.Leader)) continue;
                                if (IsPersonForHouGong(p, p.IsCaptive) && p.WaitForFeiZi == null && p.BelongedArchitecture != null)
                                {
                                    candidate.Add(p);
                                }
                            }
                        }
                        foreach (Captive c in this.Captives)
                        {
                            Person person = c.CaptivePerson;
                            if (person.ArrivingDays > 0) continue;
                            if (this.Leader.isLegalFeiZiExcludeAge(person) && person.LocationTroop == null)
                            {
                                candidate.Add(person);
                            }
                        }

                        candidate.PropertyName = "UntiredMerit";
                        candidate.IsNumber = true;
                        candidate.SmallToBig = false;
                        candidate.ReSort();
                        Person toTake = null;
                        foreach (Person p in candidate)
                        {
                            if (p.Status != PersonStatus.Moving && p.Status != PersonStatus.Princess && p.LocationArchitecture != null && p.LocationTroop == null)
                            {
                                if ((!p.RecruitableBy(this, 0) && GameObject.Random((int)unAmbition) == 0) || GameObject.Chance((int)(Session.Parameters.AINafeiSkipChanceAdd + (int)leader.Ambition * Session.Parameters.AINafeiSkipChanceMultiply)))
                                {
                                    toTake = p;
                                    break;
                                }
                            }
                        }

                        if (toTake != null)
                        {
                            if (this.IsAlien)
                            {
                                this.Leader.XuanZeMeiNv(toTake);
                                toTake.WaitForFeiZi = null;
                                leader.WaitForFeiZi = null;
                            }
                            else if (this.Leader.LocationArchitecture == dest)
                            {
                                if (toTake.LocationArchitecture == dest)
                                {
                                    this.Leader.XuanZeMeiNv(toTake);
                                    toTake.WaitForFeiZi = null;
                                    leader.WaitForFeiZi = null;
                                }
                                else
                                {
                                    if (toTake.NvGuan)
                                    {
                                        toTake.NvGuanFollower(true, null).MoveToArchitecture(dest);
                                    }
                                    else
                                    {
                                        toTake.MoveToArchitecture(dest);
                                    }
                                    toTake.WaitForFeiZi = this.Leader;
                                    this.Leader.WaitForFeiZi = toTake;
                                }
                            }
                            else
                            {
                                if (toTake.LocationArchitecture == dest)
                                {
                                    this.Leader.MoveToArchitecture(dest);
                                    toTake.WaitForFeiZi = this.Leader;
                                    this.Leader.WaitForFeiZi = toTake;
                                }
                                else
                                {
                                    this.Leader.MoveToArchitecture(dest);
                                    if (toTake.NvGuan)
                                    {
                                        toTake.NvGuanFollower(true, null).MoveToArchitecture(dest);
                                    }
                                    else
                                    {
                                        toTake.MoveToArchitecture(dest);
                                    }
                                    toTake.WaitForFeiZi = this.Leader;
                                    this.Leader.WaitForFeiZi = toTake;
                                }
                            }
                        }
                    }
                }
            }

            //chongxing
            if (this.Leader.LocationArchitecture != null && this.Leader.LocationArchitecture.Endurance > 100 && !this.Leader.LocationArchitecture.HasHostileTroopsInView())
            {
                if (this.Leader.Status == PersonStatus.Normal && this.Leader.LocationArchitecture != null && this.Leader.LocationTroop == null &&
                    !this.Leader.huaiyun && this.Leader.WaitForFeiZi == null)
                {
                    if (hougongValid)
                    {
                        if (GameObject.Random((int)(60f / (this.Leader.Ambition + 1) * Math.Sqrt(this.Leader.NumberOfChildren))) == 0)
                        {
                            Person target = null;
                            Architecture location = null;
                            float max = 0;
                            foreach (Architecture a in this.Architectures)
                            {
                                foreach (Person p in a.meifaxianhuaiyundefeiziliebiao())
                                {
                                    if (p.huaiyun) continue;
                                    if (IsPersonForHouGong(p, false))
                                    {
                                        float v = p.UntiredMerit * p.PregnancyRate(this.Leader);
                                        if (p.Hates(this.Leader))
                                        {
                                            v /= 10;
                                        }
                                        if (p.GetRelation(this.Leader) < 0)
                                        {
                                            v *= Math.Abs(p.GetRelation(this.Leader)) / 50f;
                                        }
                                        v /= (p.NumberOfChildren / 10f + 1);
                                        if (v > max)
                                        {
                                            target = p;
                                            location = a;
                                            max = v;
                                        }
                                    }
                                }
                            }
                            if (target != null)
                            {
                                if (location == this.Leader.LocationArchitecture)
                                {
                                    this.Leader.GoForHouGong(target);
                                }
                                else
                                {
                                    this.Leader.MoveToArchitecture(location);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (GameObject.Random((20 + this.Leader.NumberOfMaleChildren * 20) / (this.Leader.Ambition * this.Leader.Ambition + 1)) == 0)
                        {
                            Person target = null;
                            float max = 0;
                            foreach (Person p in this.Leader.LocationArchitecture.meifaxianhuaiyundefeiziliebiao())
                            {
                                if (p.huaiyun) continue;
                                if (IsPersonForHouGong(p, false))
                                {
                                    float v = p.UntiredMerit * p.PregnancyRate(this.Leader);
                                    v *= Math.Abs(p.GetRelation(this.Leader)) / 200f;
                                    v /= (p.NumberOfChildren / 4f + 1);
                                    if (v > max)
                                    {
                                        target = p;
                                        max = v;
                                    }
                                }
                            }
                            if (target != null)
                            {
                                this.Leader.GoForHouGong(target);
                            }
                        }
                    }
                }
            }
        }

        private void AIDiplomacy()
        {
            if (this.Leader.Status == PersonStatus.Captive) return;

            // 1. 计时器累加
            DaysSinceLastDiplomacy++;

            // 2. 检查是否有"外交突发事件" (比如玩家主动派来了使者)
            // 这种是被动响应，必须每回合检查，但消耗极低
            if (this.HasIncomingEnvoys())
            {
                ProcessIncomingEnvoys(); // 处理来访使者
            }

            // 3. 主动外交决策 (最耗性能的部分)
            // 只有每过 90 天，或者处于生死存亡时，才主动思考外交
            if (DaysSinceLastDiplomacy >= DIPLOMACY_INTERVAL)
            {
                // 执行原本的复杂外交逻辑
                this.ExecuteDiplomacyStrategy();

                // 重置计时器 (加入随机扰动，防止所有势力同一天搞外交卡死CPU)
                DaysSinceLastDiplomacy = GameObject.Random(-10, 10);
            }
        }

        /// <summary>
        /// 检查是否有来访使者需要处理
        /// </summary>
        private bool HasIncomingEnvoys()
        {
            // 简化实现：检查是否有待处理的外交事件
            // 这里可以根据实际游戏逻辑进行扩展
            return false; // 暂时返回false，避免编译错误
        }

        /// <summary>
        /// 处理来访使者
        /// </summary>
        private void ProcessIncomingEnvoys()
        {
            // 处理被动外交响应
            // 这里可以根据实际游戏逻辑进行扩展
        }

        /// <summary>
        /// 执行外交策略 - 原有的复杂外交逻辑
        /// </summary>
        private void ExecuteDiplomacyStrategy()
        {
            // 🔥 NEW: 与AI管理系统协调 - 检查是否应该暂停外交
            bool shouldSuspendDiplomacy = false;
            try
            {
                // 检查军事威胁状况
                bool hasMilitaryThreats = false;
                foreach (var arch in this.Architectures.Cast<Architecture>())
                {
                    if (arch.HasHostileTroopsInView())
                    {
                        hasMilitaryThreats = true;
                        break;
                    }
                }

                // 与AIDecisionManager协调
                var aiDecisionManager = WorldOfTheThreeKingdoms.GameGlobal.AIDecisionManager.Instance;
                if (aiDecisionManager != null && hasMilitaryThreats)
                {
                    // 军事威胁时，暂停非紧急外交活动
                    shouldSuspendDiplomacy = true;

#if DEBUG
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[🤝 DiplomacyCoordination] {this.Name}: 检测到军事威胁，暂停外交活动");
                    }
#endif
                }

                // 与AIManager协调 - 获取全局态势
                var aiManager = GameManager.AIManager.Instance;
                if (aiManager != null)
                {
                    // 可以根据AI管理器的全局状态调整外交策略
                    // 例如：如果全局处于紧张状态，优先防御性外交
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AIDiplomacy] AI系统协调失败: {ex.Message}");
#endif
            }

            // 如果需要暂停外交，只执行紧急外交（如求和）
            if (shouldSuspendDiplomacy)
            {
                ExecuteEmergencyDiplomacy();
                return;
            }

            // 执行正常的外交逻辑
            ExecuteNormalDiplomacy();
        }

        /// <summary>
        /// 执行紧急外交
        /// </summary>
        private void ExecuteEmergencyDiplomacy()
        {
            // 这里可以实现紧急外交逻辑，如求和、结盟等
            // 暂时保持简单实现
#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[🚨 EmergencyDiplomacy] {this.Name}: 执行紧急外交协议");
            }
#endif
        }

        /// <summary>
        /// 执行正常外交
        /// </summary>
        private void ExecuteNormalDiplomacy()
        {
            // 这里实现原有的外交逻辑
            // 为了保持兼容性，暂时保持空实现
#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[🤝 NormalDiplomacy] {this.Name}: 执行正常外交活动");
            }
#endif

            foreach (Faction f in Session.Current.Scenario.PlayerFactions)
            {
                if (!this.adjacentTo(f)) continue;
                if (this.IsFriendly(f)) continue;
                if (this == f) continue;
                if (GameObject.Random(1000) < Session.Parameters.AIEncirclePlayerRate && GameObject.Chance(f.ArchitectureCount))
                {
                    if (GetEncircleFactionList(f, true) == null) continue;
                    foreach (Architecture a in this.Architectures)
                    {
                        if (a.Fund > 120000 + a.AbundantFund)
                        {
                            Encircle(a, f);
                            return;
                        }
                    }
                }
            }

            if (GameObject.Random(180 * Math.Max(1, 5 - this.Leader.Ambition)) == 0 && GameObject.Chance(100 - Session.Parameters.AIEncirclePlayerRate))
            {
                GameObjectList factions = this.GetAdjecentHostileFactions();
                if (factions.Count == 0) return;

                factions.PropertyName = "Power";
                factions.IsNumber = true;
                factions.SmallToBig = false;
                factions.ReSort();

                int rank = Session.Parameters.AIEncircleRank + GameObject.Random(Session.Parameters.AIEncircleVar * 2) - Session.Parameters.AIEncircleVar;
                rank = Math.Min(rank, 100);
                rank = Math.Max(rank, 0);
                Faction target = (Faction)factions[(factions.Count - 1) * rank / 100];
                int rel = Session.Current.Scenario.GetDiplomaticRelation(this.ID, target.ID);
                if (target != this && rel < 0 && GetEncircleFactionList(target, true) != null)
                {
                    if (GameObject.Chance(Math.Abs(rel) / 10))
                    {
                        foreach (Architecture a in this.Architectures)
                        {
                            if (a.Fund > 120000 + a.AbundantFund)
                            {
                                Encircle(a, target);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private int? powerCache = null;
        public int Power
        {
            get
            {
                if (powerCache == null)
                {
                    powerCache = this.ArchitectureCount * 10000 + this.TotalPersonMerit / 10 + this.Population / 10 + this.ArmyScale * 100;
                }
                return powerCache.Value;
            }
        }

        /// <summary>
        /// 创建军团（用于协同进攻）
        /// </summary>
        public void CreateLegion(Architecture source, Architecture target, Person leader, MilitaryType kind, int troopCount)
        {
            try
            {
                if (source == null || target == null) return;

                // 🔧 修复：检查是否满足创建进攻军团的条件
                // 查找对应兵种的编队信息
                Military military = null;
                foreach (Military m in source.Militaries)
                {
                    if (m.Kind.Type == kind)
                    {
                        // 检查士气和规模
                        if (m.Morale >= 60 && m.Scales >= 8)
                        {
                            military = m;
                            break;
                        }
                    }
                }
                
                // 如果没找到符合条件的，尝试找其他符合条件的编队
                if (military == null)
                {
                    foreach (Military m in source.Militaries)
                    {
                        if (m.Morale >= 60 && m.Scales >= 8)
                        {
                            military = m;
                            break;
                        }
                    }
                }
                
                if (military == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateLegion] {source.Name} 没有符合条件的编队（士气≥60，规模≥8），无法创建进攻军团");
                    return;
                }

                // 如果没有指定武将，自动选择一个
                if (leader == null && source.PersonsExcludeNvGuan.Count > 0)
                {
                    // 简单选择统率最高的
                    leader = source.PersonsExcludeNvGuan[0] as Person;
                    foreach (Person p in source.PersonsExcludeNvGuan)
                    {
                        if (p.Command > leader.Command) leader = p;
                    }
                }

                if (leader == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateLegion] {source.Name} 没有可用武将，无法创建进攻军团");
                    return;
                }

                // 准备部队人员列表
                GameObjectList persons = new GameObjectList();
                persons.Add(leader);

                // 计算携带粮草
                int foodToTake = troopCount * 10; // 假设每人带10粮
                if (source.Food < foodToTake) foodToTake = source.Food;

                // ✅ 修复：创建部队时，直接把 offensiveLegion 传进去
                // 这里的 target 是进攻目标
                // 🔥 重构：使用新的CreateLegion方法
                Legion offensiveLegion = this.GetOrCreateLegion(target, LegionKind.AI, LegionMission.Attack);

                // 使用 CreateTroop 创建部队，直接指定军团，避免后续重复操作
                Troop troop = source.CreateTroop(persons, leader, military, foodToTake, source.GetRandomStartingPosition(military) ?? source.Position, assignedLegion: offensiveLegion);

                if (troop != null)
                {
                    // 设置目标
                    troop.TargetArchitecture = target;
                    troop.Operation = TroopAction.Attack;

                    // 强制设置攻击状态
                    troop.TroopStatus = TroopStatus.攻击;
                    
                    System.Diagnostics.Debug.WriteLine($"[CreateLegion] {source.Name} 成功创建进攻部队 {troop.DisplayName} -> {target.Name}（士气:{troop.Morale}，规模:{troop.Army.Scales}）");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateLegion] Error: {ex.Message}");
            }
        }

        private void AI()
        {
            // 🔧 FIX: 检查势力是否已灭亡（没有首都），已灭亡势力不执行AI
            if (this.Capital == null)
            {
                this.AIFinished = true;
                return;
            }

            // 🔥 ANTI-BAND-AID：不掩盖数据错误，如果 Session.Current 或 Scenario 为 null，让它自然抛出异常
            bool isPlayer = Session.Current.Scenario.IsPlayer(this);
            
            #if DEBUG
            if (this.ID == 0) // 只诊断 ID=0 的势力（汉）
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] ========== 势力 {this.Name} AI 开始 ==========");
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] IsPlayer(this): {isPlayer}");
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] PlayerFactions.Count: {Session.Current.Scenario.PlayerFactions.Count}");
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] CurrentPlayer: {Session.Current.Scenario.CurrentPlayer?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] this.ID: {this.ID}");
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] this.Name: {this.Name}");
                
                // 🔥 诊断：输出 PlayerFactions 的内容
                if (Session.Current.Scenario.PlayerFactions.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.AI] PlayerFactions 内容:");
                    foreach (Faction f in Session.Current.Scenario.PlayerFactions)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - ID={f.ID}, Name={f.Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.AI] ⚠️ PlayerFactions 为空！");
                }
            }
            #endif

            // 【新增】：记录势力建立回合（如果还没有记录）
            if (FactionCreationTurn == -1 && Session.Current?.Scenario != null)
            {
                FactionCreationTurn = Session.Current.Scenario.DaySince / 30;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] {this.Name} 记录建立回合: 第{FactionCreationTurn}回合");
#endif
            }

            // 【新增】：检查是否为新建势力（3回合内）
            bool isNewFaction = IsNewlyCreatedFaction();

            // =========================================================================
            // 【Fast Path】电脑势力直接走快速通道
            // =========================================================================
            if (!isPlayer)
            {
                #if DEBUG
                if (this.ID == 0) // 只诊断 ID=0 的势力（汉）
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.AI] ⚠️⚠️⚠️ 势力 {this.Name} 被判定为非玩家势力，将执行 AI 逻辑！");
                }
                #endif
                
                // 【新增】：新建势力优先调配
                if (isNewFaction)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[Faction.AI] {this.Name} 新建势力，执行优先调配");
#endif
                    // 新建势力前3回合：更频繁的调配
                    this.RunPersonnel(null);  // 优先人员调配
                    this.ManageLogistics();   // 优先物资调配
                }

                // 1. 核心生产力
                this.RunDomestic(null);
                this.RunMilitary(null);
                this.AIDiplomacy();
                this.AutoAppointMayor(null);

                // 2. 辅助行为
                this.AICapital();
                this.AICaptives();
                this.AITechniques();
                this.AINvGuan();
                this.AIMakeMarriage();
                this.AISelectPrince();
                this.AIZhaoXian();
                this.AIZhaoXian(); // Intentional double call? Keeping as is.
                this.AIAppointAdvisor();
                this.AIHouGong();

                // 3. 全局管理
                if (!isNewFaction) // 非新建势力才执行正常频率的调配
                {
                    this.RunPersonnel(null);
                    this.ManageLogistics();
                }
                this.AICoordinatedAttacks();

                // 只有存在军团时才执行军团AI
                if (this.Legions != null && this.Legions.Count > 0)
                {
                    this.AILegions();
                }

                this.AITrainChildren();

                // 4. 清理由于之前的 "Managers" 引入的错误，只保留核心逻辑
                // (Removed undefined Manager updates)

                this.AIUpdateCounter++; // Keep the counter increment

                // 必须标记完成
                this.AIFinished = true;
                return;
            }

            // ----------------------------------------------------------------
            // 【Normal Path】玩家势力
            // ----------------------------------------------------------------
            try
            {
                Session.Current.Scenario.Threading = true;
                this.AIFinished = false;

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[Faction.AI] 势力 {this.Name} 准备执行军区AI");
                }
                this.AIPrepare();

                // 玩家势力：调用军区AI
                this.AISectionsOptimized();

                // 辅助逻辑
                this.AICapital();
                this.AICaptives();
                this.AITechniques();
                this.AINvGuan();
                this.AIMakeMarriage();
                this.AISelectPrince();
                this.AIZhaoXian();
                this.AIZhaoXian();
                this.AutoAppointMayor(); // 恢复调用，内部已有正确判断逻辑
                this.AIAppointAdvisor();

                // (Removed undefined Manager updates for Player as well)
                this.AIUpdateCounter++;

                this.AIHouGong();
                // 执行建筑AI
                this.AIArchitectures();
                this.AITransfer();
                this.ManageLogistics();
                this.AICoordinatedAttacks();

                // 只有存在军团时才执行军团AI
                if (this.Legions != null && this.Legions.Count > 0)
                {
                    this.AILegions();
                }

                this.AITrainChildren();

                System.Diagnostics.Debug.WriteLine("[AI] 玩家/军区 AI 处理完成: " + this.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[🔴 Faction.AI ERROR] 势力: {this.Name} (ID:{this.ID})\n异常: {ex.Message}\n堆栈: {ex.StackTrace}");
            }
            finally
            {
                this.AIFinished = true;

                if (Session.Current?.Scenario?.Factions != null)
                {
                    bool allFinished = true;
                    foreach (var f in Session.Current.Scenario.Factions.GetList())
                    {
                        if (f is Faction faction && faction.IsAlive && !faction.AIFinished)
                        {
                            allFinished = false;
                            break;
                        }
                    }
                    if (allFinished)
                    {
                        System.Diagnostics.Debug.WriteLine("[AI] 全势力回合 AI 完毕，关闭 Threading 锁定");
                        Session.Current.Scenario.Threading = false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[AI] Faction.AI 执行周期结束: " + this.Name);
            }
        }

        private void AIArchitectures()
        {
            // 🔥 修复：确保只有 AI 势力才执行建筑级 AI 逻辑
            // 玩家势力的建筑应该由 PlayerAIArchitectures() 处理
            if (Session.Current.Scenario.IsPlayer(this))
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.AIArchitectures] ⚠️ 警告：玩家势力 {this.Name} 意外调用了 AIArchitectures！");
                System.Diagnostics.Debug.WriteLine($"[Faction.AIArchitectures]   这个方法只应该由 AI 势力调用");
                return;
            }

            // 🔥 RESTORE: 恢复调用 Architecture.AI() 以确保 AI 势力执行完整逻辑 (处决、外交战术等)
            // 之前的修改只调用了 RunDomestic/RunMilitary，导致部分AI行为 (AIExecute) 丢失

            // 使用安全遍历
            List<Architecture> targets = new List<Architecture>(this.Architectures.Count);
            foreach (Architecture a in this.Architectures)
            {
                targets.Add(a);
            }

            foreach (Architecture architecture in targets)
            {
                // 如果是玩家势力，且该城池属于托管军区，Architecture.AI() 内部的 check 可能不足以阻止所有行为
                // 但 Architecture.AI() 主要是为 AI 势力设计的。

                try
                {
                    architecture.AI();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIArchitectures Error] {this.Name} - {architecture.Name}: {ex.Message}");
                }
            }
        }

        class DistanceComparer : IComparer<GameObject>
        {
            private Architecture target;
            public DistanceComparer(Architecture target)
            {
                this.target = target;
            }

            public int Compare(GameObject x, GameObject y)
            {
                double a = Session.Current.Scenario.GetDistance(target.Position, ((Architecture)x).Position);
                double b = Session.Current.Scenario.GetDistance(target.Position, ((Architecture)y).Position);
                if (a > b)
                {
                    return 1;
                }
                else if (a < b)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private void WithdrwalTransfer(ArchitectureList architectures)
        {
            foreach (Architecture a in architectures)
            {
                if (a.Abandoned)
                {
                    a.WithdrawPerson();
                    a.WithdrawMilitaries();
                }
                if (a.HasHostileTroopsInView())
                {
                    a.WithdrawResources();
                }
                else
                {
                    a.WithdrawResources();
                }
            }
        }

        public void FullTransfer(ArchitectureList srcArch, ArchitectureList destArch, bool resource, bool person, bool military)
        {
            foreach (Architecture a in srcArch)
            {
                if (a.Abandoned) continue;

                List<GameObject> candidates = new List<GameObject>(destArch.GameObjects);
                candidates.Sort(new DistanceComparer(a));

                if (a.Fund >= a.FundCeiling * 0.9 && resource && a.IsFundAbundant)
                {
                    foreach (Architecture b in candidates)
                    {
                        if (b.Fund + b.FundInPack < b.FundCeiling * 0.8 && !b.Abandoned && b != a)
                        {
                            int toTransfer = (int)(Math.Min(a.Fund - a.FundCeiling * (a.FrontLine ? 0.7 : 0.5), b.FundCeiling * 0.8 - b.Fund - b.FundInPack));
                            b.CallResource(a, toTransfer, 0);
                            if (a.Fund < a.FundCeiling * 0.9) break;
                        }
                    }
                }
                if (a.Food >= a.FoodCeiling * 0.9 && resource && a.IsFoodAbundant)
                {
                    foreach (Architecture b in candidates)
                    {
                        if (b.Food + b.FoodInPack < b.FoodCeiling * 0.8 && !b.Abandoned && b != a)
                        {
                            int toTransfer = (int)(Math.Min(a.Food - a.FoodCeiling * (a.FrontLine ? 0.7 : 0.5), b.FoodCeiling * 0.8 - b.Food - b.FoodInPack));
                            b.CallResource(a, 0, toTransfer);
                            if (a.Food < a.FoodCeiling * 0.9) break;
                        }
                    }
                }

                if (a.IsTroopExceedsLimit && military && !a.FrontLine)
                {
                    int toSend = a.ArmyScale / 2;
                    foreach (Architecture b in candidates)
                    {
                        if (b.FrontLine && b.IsFoodTwiceAbundant && !b.Abandoned & b != a)
                        {
                            int sent = b.CallMilitary(a, toSend);
                            toSend -= sent;
                            if (toSend <= 0) break;
                        }
                    }
                }
            }
        }

        public void PersonRegroupTransfer(ArchitectureList archs)
        {
            if (GameObject.Random(30 / Session.Current.Scenario.Parameters.DayInTurn) == 0)
            {
                foreach (Architecture a in archs)
                {
                    foreach (Person p in a.PersonsExcludeNvGuan)
                    {
                        if (p.Status == PersonStatus.Normal && p.LocationArchitecture != null && p.LocationTroop == null)
                        {
                            if (p.Spouse != null && p.Spouse.Status == PersonStatus.Normal && p.Spouse.BelongedFaction == p.BelongedFaction && p.Spouse.BelongedArchitecture.BelongedSection.AIDetail.AutoRun
                                && p.LocationArchitecture != p.Spouse.LocationArchitecture && p.Spouse.LocationArchitecture != null && p.Spouse.LocationTroop == null)
                            {
                                foreach (Military m in p.Spouse.LeadingArmies)
                                {
                                    p.Spouse.LocationArchitecture.TransferMilitary(m, p.LocationArchitecture);
                                }
                                p.Spouse.MoveToArchitecture(p.LocationArchitecture);
                            }

                            if (p.Brothers.Count > 0)
                            {
                                foreach (Person q in p.Brothers)
                                {
                                    if (q != null && q.Status == PersonStatus.Normal && q.BelongedFaction == p.BelongedFaction && q.BelongedArchitecture.BelongedSection.AIDetail.AutoRun
                                            && p.LocationArchitecture != q.LocationArchitecture && q.LocationArchitecture != null && q.LocationTroop == null)
                                    {
                                        foreach (Military m in q.LeadingArmies)
                                        {
                                            q.LocationArchitecture.TransferMilitary(m, p.LocationArchitecture);
                                        }
                                        q.MoveToArchitecture(p.LocationArchitecture);
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }

        public void AllocationTransfer(ArchitectureList srcArch, ArchitectureList destArch, bool resource, bool person, bool military)
        {
            ArchitectureList scope = new ArchitectureList();

            Dictionary<Architecture, int> minPerson = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> minFund = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> minFood = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> minTroop = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> goodPerson = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> goodFund = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> goodFood = new Dictionary<Architecture, int>();
            Dictionary<Architecture, int> goodTroop = new Dictionary<Architecture, int>();
            bool urgent = false;

            int totalPerson = 0;
            int totalFrontline = 0;
            foreach (Architecture a in srcArch)
            {
                foreach (Person p in a.PersonsExcludeNvGuan)
                {
                    if (p.Command > 50)
                    {
                        totalPerson++;
                    }
                }
                if (a.FrontLine)
                {
                    totalFrontline++;
                }
            }
            int avgFrontlinePerson;
            if (totalFrontline == 0)
            {
                avgFrontlinePerson = 0;
            }
            else
            {
                avgFrontlinePerson = totalPerson / totalFrontline;
            }

            foreach (Architecture a in srcArch.GameObjects.Union(destArch.GameObjects))
            {
                if (!a.Abandoned)
                {
                    scope.Add(a);

                    if (a.HasHostileTroopsInView() || a.RecentlyAttacked > 0)
                    {
                        minPerson.Add(a, a.EnoughPeople + a.JianzhuGuimo);
                        minTroop.Add(a, a.TroopReserveScale); // defensiveCampaign will deal with this
                        urgent = true;
                    }
                    else if (a.PlanArchitecture != null)
                    {
                        minPerson.Add(a, Math.Min(avgFrontlinePerson, a.EnoughPeople));
                        minTroop.Add(a, a.TroopReserveScale);
                    }
                    else if (a.FrontLine)
                    {
                        minPerson.Add(a, Math.Min(3, a.EnoughPeople));
                        minTroop.Add(a, a.TroopReserveScale);
                    }
                    else if (a.IsNetLosingPopulation)
                    {
                        minPerson.Add(a, a.EnoughPeople);
                        minTroop.Add(a, 0);
                    }
                    else
                    {
                        minPerson.Add(a, 0);
                        minTroop.Add(a, 0);
                    }
                    minFund.Add(a, Math.Min(a.FundCeiling * 9 / 10, a.EnoughFund));
                    minFood.Add(a, Math.Min(a.FoodCeiling * 9 / 10, a.EnoughFood));

                    if (a.HostileLine || a.orientationFrontLine)
                    {
                        int troop = Math.Max(a.HostileScale, a.OrientationScale);
                        if (a.IsVeryGood())
                        {
                            goodPerson.Add(a, Math.Max(avgFrontlinePerson, minPerson[a]));
                        }
                        else
                        {
                            goodPerson.Add(a, Math.Max(Math.Max(avgFrontlinePerson, a.EnoughPeople), minPerson[a]));
                        }
                        goodTroop.Add(a, Math.Max(troop, minTroop[a]));
                        goodFood.Add(a, Math.Min(a.FoodCeiling * 9 / 10, Math.Max(a.AbundantFood * 2, minFood[a])));
                        goodFund.Add(a, Math.Min(a.FundCeiling * 9 / 10, Math.Max(a.AbundantFund, minFund[a])));
                    }
                    else if (a.FrontLine)
                    {
                        goodPerson.Add(a, Math.Max(avgFrontlinePerson, minPerson[a]));
                        goodTroop.Add(a, Math.Max(a.TroopReserveScale, minTroop[a]));
                        goodFood.Add(a, Math.Min(a.FoodCeiling * 9 / 10, Math.Max(a.AbundantFood * 2, minFood[a])));
                        goodFund.Add(a, Math.Min(a.FundCeiling * 9 / 10, Math.Max(a.AbundantFund, minFund[a])));
                    }
                    else if (!a.IsVeryGood())
                    {
                        goodPerson.Add(a, Math.Max(a.EnoughPeople, minPerson[a]));
                        goodTroop.Add(a, Math.Max(a.TroopReserveScale, minTroop[a]));
                        goodFund.Add(a, Math.Min(a.FundCeiling * 9 / 10, Math.Max(a.AbundantFund, minFund[a])));
                        goodFood.Add(a, Math.Min(a.FoodCeiling * 9 / 10, Math.Max(a.AbundantFood * 2, minFood[a])));
                    }
                    else
                    {
                        goodPerson.Add(a, Math.Max(1, minPerson[a]));
                        goodTroop.Add(a, Math.Max(a.TroopReserveScale, minTroop[a]));
                        goodFund.Add(a, Math.Min(a.FundCeiling * 9 / 10, Math.Max(a.AbundantFund, minFund[a])));
                        goodFood.Add(a, Math.Min(a.FoodCeiling * 9 / 10, Math.Max(a.AbundantFood * 2, minFood[a])));
                    }
                }
            }

            scope.PropertyName = "Population";
            scope.SmallToBig = false;
            scope.IsNumber = true;
            scope.ReSort();

            for (int priority = 0; priority < 3; ++priority)
            {
                foreach (Architecture a in destArch)
                {
                    if (a.Abandoned) continue;

                    if (!a.HasHostileTroopsInView() && priority < 1) continue;
                    if (a.HasHostileTroopsInView() && priority >= 1) continue;

                    if (!a.FrontLine && priority < 2) continue;
                    if (a.FrontLine && priority >= 2) continue;

                    if (a.ArmyScale < minTroop[a] && military && (a.SuspendTroopTransfer <= 0 || urgent))
                    {
                        int deficit = minTroop[a] * 2 - a.ArmyScale;

                        List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                        candidates.Sort(new DistanceComparer(a));

                        foreach (Architecture b in candidates)
                        {
                            if (b.Abandoned || b == a) continue;
                            if (b.ArmyScale > goodTroop[b])
                            {
                                int send = Math.Min(deficit, b.ArmyScale - goodTroop[b]);
                                send = a.CallMilitary(b, send);
                                if (send > 0)
                                {
                                    deficit -= send;
                                    if (deficit <= 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        if (deficit > 0)
                        {
                            foreach (Architecture b in candidates)
                            {
                                if (b.Abandoned || b == a) continue;
                                int send = Math.Min(deficit, b.ArmyScale - minTroop[b]);
                                send = a.CallMilitary(b, send);
                                if (send > 0)
                                {
                                    deficit -= send;
                                    if (deficit <= 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (a.PersonCount + a.MovingPersonCount < minPerson[a] && person)
                    {
                        int deficit = minPerson[a] - a.PersonCount - a.MovingPersonCount;

                        List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                        candidates.Sort(new DistanceComparer(a));

                        foreach (Architecture b in candidates)
                        {
                            if (b.Abandoned || b == a) continue;
                            if (b.PersonCount + b.MovingPersonCount > goodPerson[b])
                            {
                                int send = Math.Min(deficit, b.PersonCount + b.MovingPersonCount - goodPerson[b]);
                                send = a.CallPeople(b, send);
                                if (send > 0)
                                {
                                    deficit -= send;
                                    if (deficit <= 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        if (deficit > 0)
                        {
                            foreach (Architecture b in candidates)
                            {
                                if (b.Abandoned || b == a) continue;
                                if (b.PersonCount + b.MovingPersonCount > minPerson[b])
                                {
                                    int send = Math.Min(deficit, b.PersonCount + b.MovingPersonCount - minPerson[b]);
                                    send = a.CallPeople(b, send);
                                    if (send > 0)
                                    {
                                        deficit -= send;
                                        if (deficit <= 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (deficit > 0 && urgent)
                        {
                            foreach (Architecture b in candidates)
                            {
                                if (b.Abandoned || b == a) continue;
                                if (b.PersonCount + b.MovingPersonCount > 1 && !b.HasHostileTroopsInView() && b.RecentlyAttacked == 0)
                                {
                                    int send = Math.Min(deficit, b.PersonCount + b.MovingPersonCount - minPerson[b]);
                                    send = a.CallPeople(b, send);
                                    if (send > 0)
                                    {
                                        deficit -= send;
                                        if (deficit <= 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                    }

                    if ((a.Fund + a.FundInPack < minFund[a] || a.Food + a.FoodInPack < minFood[a]) && resource)
                    {
                        int deficitFund = Math.Max(0, minFund[a] * 2 - a.Fund - a.FundInPack);
                        int deficitFood = Math.Max(0, minFood[a] * 2 - a.Food - a.FoodInPack);
                        deficitFood = Math.Min(deficitFood, a.FoodCeiling * 9 / 10 - a.FoodInPack - a.Food);
                        deficitFund = Math.Min(deficitFund, a.FundCeiling * 9 / 10 - a.FundInPack - a.Fund);

                        if (deficitFund > 0 || deficitFood > 0)
                        {
                            List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                            candidates.Sort(new DistanceComparer(a));

                            foreach (Architecture b in candidates)
                            {
                                if (b.Abandoned || b == a) continue;
                                if (!b.withoutTruceFrontline && !b.HasHostileTroopsInView())
                                {
                                    if (b.Fund >= goodFund[b] * 2 || b.Food >= goodFood[b] * 2)
                                    {
                                        int transferFund = Math.Max(0, Math.Min(deficitFund, b.Fund - goodFund[b] * 2));
                                        int transferFood = Math.Max(0, Math.Min(deficitFood, b.Food - goodFood[b] * 2));
                                        if (a.CallResource(b, transferFund, transferFood))
                                        {
                                            deficitFund -= transferFund;
                                            deficitFood -= transferFood;
                                            if (deficitFood <= 0 && deficitFund <= 0)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (deficitFood > 0 || deficitFund > 0)
                            {
                                foreach (Architecture b in candidates)
                                {
                                    if (b.Abandoned || b == a) continue;
                                    if (!b.HasHostileTroopsInView())
                                    {
                                        if (b.Fund >= goodFund[b] * 2 || b.Food >= goodFood[b] * 2)
                                        {
                                            int transferFund = Math.Max(0, Math.Min(deficitFund, b.Fund - goodFund[b] * 2));
                                            int transferFood = Math.Max(0, Math.Min(deficitFood, b.Food - goodFood[b] * 2));
                                            if (a.CallResource(b, transferFund, transferFood))
                                            {
                                                deficitFund -= transferFund;
                                                deficitFood -= transferFood;
                                                if (deficitFood <= 0 && deficitFund <= 0)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (deficitFood > 0 || deficitFund > 0)
                            {
                                foreach (Architecture b in candidates)
                                {
                                    if (b.Abandoned || b == a) continue;
                                    if (!b.HasHostileTroopsInView())
                                    {
                                        if (b.Fund >= goodFund[b] || b.Food >= goodFood[b])
                                        {
                                            int transferFund = Math.Max(0, Math.Min(deficitFund, b.Fund - goodFund[b]));
                                            int transferFood = Math.Max(0, Math.Min(deficitFood, b.Food - goodFood[b]));
                                            if (a.CallResource(b, transferFund, transferFood))
                                            {
                                                deficitFund -= transferFund;
                                                deficitFood -= transferFood;
                                                if (deficitFood <= 0 && deficitFund <= 0)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (Architecture a in destArch)
            {
                if (a.Abandoned) continue;

                if (a.ArmyScale < goodTroop[a] && military && a.SuspendTroopTransfer <= 0)
                {
                    int deficit = goodTroop[a] * 2 - a.ArmyScale;

                    List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                    candidates.Sort(new DistanceComparer(a));

                    foreach (Architecture b in candidates)
                    {
                        if (b.Abandoned || b == a) continue;
                        if (b.ArmyScale > goodTroop[b])
                        {
                            int send = Math.Min(deficit, b.ArmyScale - goodTroop[b]);
                            send = a.CallMilitary(b, send);
                            if (send > 0)
                            {
                                deficit -= send;
                                if (deficit <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }

                }

                if (a.PersonCount + a.MovingPersonCount < goodPerson[a] && person)
                {
                    int deficit = goodPerson[a] - a.PersonCount - a.MovingPersonCount;

                    List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                    candidates.Sort(new DistanceComparer(a));

                    foreach (Architecture b in candidates)
                    {
                        if (b.Abandoned || b == a) continue;
                        if (b.PersonCount + b.MovingPersonCount > goodPerson[b])
                        {
                            int send = Math.Min(deficit, b.PersonCount + b.MovingPersonCount - goodPerson[b]);
                            send = a.CallPeople(b, send);
                            if (send > 0)
                            {
                                deficit -= send;
                                if (deficit <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if ((a.Fund + a.FundInPack < goodFund[a] || a.Food + a.FoodInPack < goodFood[a]) && resource)
                {
                    int deficitFund = Math.Max(0, goodFund[a] * 2 - a.Fund - a.FundInPack);
                    int deficitFood = Math.Max(0, goodFood[a] * 2 - a.Food - a.FoodInPack);
                    deficitFood = Math.Min(deficitFood, a.FoodCeiling * 9 / 10 - a.FoodInPack - a.Food);
                    deficitFund = Math.Min(deficitFund, a.FundCeiling * 9 / 10 - a.FundInPack - a.Fund);

                    if (deficitFund > 0 || deficitFood > 0)
                    {
                        List<GameObject> candidates = new List<GameObject>(srcArch.GameObjects);
                        candidates.Sort(new DistanceComparer(a));

                        foreach (Architecture b in candidates)
                        {
                            if (b.Abandoned || b == a) continue;
                            if (!b.withoutTruceFrontline && !b.HasHostileTroopsInView())
                            {
                                if (b.Fund >= goodFund[b] * 2 || b.Food >= goodFood[b] * 2)
                                {
                                    int transferFund = Math.Max(0, Math.Min(deficitFund, b.Fund - goodFund[b] * 2));
                                    int transferFood = Math.Max(0, Math.Min(deficitFood, b.Food - goodFood[b] * 2));
                                    if (a.CallResource(b, transferFund, transferFood))
                                    {
                                        deficitFund -= transferFund;
                                        deficitFood -= transferFood;
                                        if (deficitFood <= 0 && deficitFund <= 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (deficitFood > 0 || deficitFund > 0)
                        {
                            foreach (Architecture b in candidates)
                            {
                                if (b.Abandoned || b == a) continue;
                                if (!b.HasHostileTroopsInView())
                                {
                                    if (b.Fund >= goodFund[b] * 2 || b.Food >= goodFood[b] * 2)
                                    {
                                        int transferFund = Math.Max(0, Math.Min(deficitFund, b.Fund - goodFund[b] * 2));
                                        int transferFood = Math.Max(0, Math.Min(deficitFood, b.Food - goodFood[b] * 2));
                                        if (a.CallResource(b, transferFund, transferFood))
                                        {
                                            deficitFund -= transferFund;
                                            deficitFood -= transferFood;
                                            if (deficitFood <= 0 && deficitFund <= 0)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

        public void AITransferPlanning(ArchitectureList architectures)
        {
            WithdrwalTransfer(architectures);
            AllocationTransfer(architectures, architectures, true, true, true);
            PersonRegroupTransfer(architectures);
            if (GameObject.Chance(10))
            {
                FullTransfer(architectures, architectures, true, true, true);
            }
        }

        private void PlayerAITransfer()
        {
            foreach (Section s in this.Sections)
            {
                if (s.AIDetail.AutoRun)
                {
                    s.AIIntraTransfer();
                    if (s.AIDetail.AllowFoodTransfer || s.AIDetail.AllowFundTransfer || s.AIDetail.AllowMilitaryTransfer)
                    {
                        s.AIInterTransfer();
                    }
                }
            }
        }

        private void AITransfer()
        {
            if (this.Architectures.Count > 1)
            {
                this.AITransferPlanning(this.Architectures);
            }
        }


        public void AICoordinatedAttacks()
        {
            // [新增] 只有在特定条件下才执行多城协同
            if (this.ArchitectureCount < 2) return;

            try
            {
                // 调用协同进攻系统
                WTKGameManager.AICoordinatedAttackSystem.Instance.TryLaunchCoordinatedAttack(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AICoordinatedAttacks] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 AI执行时间控制系统 - 人员调配版本
        /// 实现60天正常间隔 + 5天紧急间隔的智能调度
        /// </summary>
        private bool ShouldRunPersonnelAI(GameDate currentDate)
        {
            // 计算距离上次执行过了多少天
            int daysSinceLastRun = CalculateDaysDifference(LastPersonnelAIDate, currentDate);

            // 判断是否该执行
            bool timeUp = daysSinceLastRun >= PERSONNEL_INTERVAL_NORMAL;
            bool urgentAndReady = IsPersonnelUrgentDirty && (daysSinceLastRun >= PERSONNEL_INTERVAL_URGENT);

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput && (timeUp || urgentAndReady))
            {
                string reason = timeUp ? $"时间到({daysSinceLastRun}天)" : $"紧急事件({daysSinceLastRun}天)";
                System.Diagnostics.Debug.WriteLine($"[⏰ PersonnelAI] {this.Name} 触发人员调配: {reason}");
            }
#endif

            return timeUp || urgentAndReady;
        }

        /// <summary>
        /// 🔥 AI执行时间控制系统 - 内政AI版本
        /// 实现30天正常间隔 + 3天紧急间隔的智能调度
        /// </summary>
        private bool ShouldRunDomesticAI(GameDate currentDate)
        {
            // 计算距离上次执行过了多少天
            int daysSinceLastRun = CalculateDaysDifference(LastDomesticAIDate, currentDate);

            // 判断是否该执行
            bool timeUp = daysSinceLastRun >= DOMESTIC_INTERVAL_NORMAL;
            bool urgentAndReady = IsDomesticUrgentDirty && (daysSinceLastRun >= DOMESTIC_INTERVAL_URGENT);

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput && (timeUp || urgentAndReady))
            {
                string reason = timeUp ? $"时间到({daysSinceLastRun}天)" : $"紧急事件({daysSinceLastRun}天)";
                System.Diagnostics.Debug.WriteLine($"[⏰ DomesticAI] {this.Name} 触发内政AI: {reason}");
            }
#endif

            return timeUp || urgentAndReady;
        }

        /// <summary>
        /// 计算两个GameDate之间的天数差异
        /// </summary>
        private int CalculateDaysDifference(GameDate fromDate, GameDate toDate)
        {
            // 如果fromDate是初始值(0年1月1日)，返回一个大数值确保首次执行
            if (fromDate == null || (fromDate.Year == 0 && fromDate.Month == 1 && fromDate.Day == 1))
            {
                return int.MaxValue;
            }

            // 计算总天数差异
            int yearDiff = toDate.Year - fromDate.Year;
            int monthDiff = toDate.Month - fromDate.Month;
            int dayDiff = toDate.Day - fromDate.Day;

            // 简化计算：每年360天，每月30天
            int totalDays = yearDiff * 360 + monthDiff * 30 + dayDiff;

            return Math.Max(0, totalDays);
        }

        /// <summary>
        /// 🚨 通知紧急事件 - 人员调配
        /// 只有真正的紧急情况才能打断正常的60天间隔
        /// </summary>
        public void NotifyPersonnelUrgentEvent(string reason)
        {
            IsPersonnelUrgentDirty = true;

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[🚨 PersonnelUrgent] {this.Name} 收到紧急事件: {reason}，人员调配将在{PERSONNEL_INTERVAL_URGENT}天后介入");
            }
#endif
        }

        /// <summary>
        /// 🚨 通知紧急事件 - 内政AI
        /// 只有真正的紧急情况才能打断正常的30天间隔
        /// </summary>
        public void NotifyDomesticUrgentEvent(string reason)
        {
            IsDomesticUrgentDirty = true;

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[🚨 DomesticUrgent] {this.Name} 收到紧急事件: {reason}，内政AI将在{DOMESTIC_INTERVAL_URGENT}天后介入");
            }
#endif
        }

        [ThreadStatic]
        private static bool _isRunningPublicPersonnel = false;

        /// <summary>
        /// 执行人事调配（支持局部范围/军团）
        /// </summary>
        /// <param name="targetList">如果不为null，则仅在此列表内的城市间进行调配</param>
        /// <summary>
        /// 执行人事调配（支持局部范围/军团）
        /// </summary>
        /// <param name="targetList">如果不为null，则仅在此列表内的城市间进行调配</param>
        public void RunPersonnel(IEnumerable<Architecture> targetList = null)
        {
            // 🛡️ 递归保护：防止无限递归导致 StackOverflow
            if (_isRunningPublicPersonnel)
            {
                System.Diagnostics.Debug.WriteLine($"[🔴 RECURSION DETECTED] public RunPersonnel 检测到递归调用! 势力:{this.Name}");
                return;
            }
            _isRunningPublicPersonnel = true;
            try
            {
                GameDate currentDate = Session.Current.Scenario.Date;
                
                // 🔥 修复：统一使用势力级时间控制（60天冷却 + 紧急事件）
                // 无论是势力级调用还是军区级调用，都执行相同的时间检查
                // 这样可以避免人员频繁在路上奔波
                if (!ShouldRunPersonnelAI(currentDate))
                {
#if DEBUG
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        int daysSinceLastRun = CalculateDaysDifference(LastPersonnelAIDate, currentDate);
                        string caller = targetList == null ? "势力级" : "军区级";
                        System.Diagnostics.Debug.WriteLine($"[⏰ PersonnelAI] {this.Name} 跳过人员调配({caller}调用): 距上次执行{daysSinceLastRun}天，未达到触发条件");
                    }
#endif
                    return; // 时间未到且无紧急情况，直接返回
                }

                // 🎯 时间检查通过后才打印详细调试信息
                System.Diagnostics.Debug.WriteLine($"");
                System.Diagnostics.Debug.WriteLine($"╔═══════════════════════════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"║ [🎯 人员调动触发] 势力: {this.Name}");

                // 获取调用堆栈，显示触发来源
                var stackTrace = new System.Diagnostics.StackTrace(1, true);
                string callerInfo = stackTrace.GetFrame(0)?.ToString()?.Trim() ?? "未知";
                System.Diagnostics.Debug.WriteLine($"║ 调用来源: {callerInfo}");

                if (targetList != null)
                {
                    var targetCities = targetList.ToList();
                    System.Diagnostics.Debug.WriteLine($"║ 目标列表: {targetCities.Count}个城市");
                    System.Diagnostics.Debug.WriteLine($"║ 城市列表: {string.Join(", ", targetCities.Select(a => a.Name))}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"║ 目标列表: 全势力 ({this.Architectures.Count}个城市)");
                }

                System.Diagnostics.Debug.WriteLine($"║ 游戏日期: {Session.Current.Scenario.Date}");
                System.Diagnostics.Debug.WriteLine($"╚═══════════════════════════════════════════════════════════════");

                System.Diagnostics.Debug.WriteLine($"[🔍 RunPersonnel ENTRY] 势力:{this.Name} 被调用, targetList是否为空:{targetList == null}");

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 势力 {this.Name} 进入 RunPersonnel. 传参列表是否为空: {targetList == null}");
                }

                // 🔥 NEW: 与AIManager协调 - 获取军事威胁信息
                bool hasMilitaryThreats = false;
                bool shouldPrioritizeFrontline = false;

                try
                {
                    // 检查军事威胁
                    foreach (var arch in this.Architectures.Cast<Architecture>())
                    {
                        if (arch.HasHostileTroopsInView())
                        {
                            hasMilitaryThreats = true;
                            break;
                        }
                    }

                    // 与AIDecisionManager协调
                    var aiDecisionManager = WorldOfTheThreeKingdoms.GameGlobal.AIDecisionManager.Instance;
                    if (aiDecisionManager != null && hasMilitaryThreats)
                    {
                        shouldPrioritizeFrontline = true;

#if DEBUG
                        if (SectionAIHelper.EnableDebugOutput)
                        {
                            System.Diagnostics.Debug.WriteLine($"[🤝 PersonnelCoordination] {this.Name}: 检测到军事威胁，优先前线人员配置");
                        }
#endif
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] AI系统协调失败: {ex.Message}");
#endif
                }

                List<Architecture> scope = new List<Architecture>();

                if (targetList != null)
                {
                    // 军团模式：构建局部列表
                    foreach (var arch in targetList)
                    {
                        scope.Add(arch);
                    }
                }
                else
                {
                    // 势力模式：默认使用全势力城市
                    // 🔥 修复：AI势力不跳过托管军区，因为AI势力不应该依赖军区AI
                    bool isAIFaction = !Session.Current.Scenario.IsPlayer(this);

                    foreach (Architecture arch in this.Architectures)
                    {
                        if (isAIFaction)
                        {
                            // AI势力：包含所有城市，不跳过托管军区
                            scope.Add(arch);
                        }
                        else
                        {
                            // 玩家势力：跳过托管军区的城市（避免重复调配）
                            if (arch.BelongedSection != null && arch.BelongedSection.AIDetail != null && arch.BelongedSection.AIDetail.AutoRun)
                            {
                                continue;
                            }
                            scope.Add(arch);
                        }
                    }
                }

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 势力 {this.Name} 执行上下文构建完毕. 范围城市数: {scope.Count}");
                }

                // 🔥 使用 V8.7 战略视角版调配逻辑
                if (scope.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[🎯 调用V8.7] 势力:{this.Name} 准备调用 RunPersonnel_V85 (实际V8.7), 城市数:{scope.Count}");

                    // 调用 AI_Personnel_Management_V8.5_Strategic.cs 中的核心逻辑 (已更新到V8.7)
                    this.RunPersonnel_V85(scope.ToList());

                    System.Diagnostics.Debug.WriteLine($"[✅ V8.7完成] 势力:{this.Name} RunPersonnel_V85 执行完毕");
                }
                else
                {
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 势力 {this.Name} 城市数不足2 ({scope.Count})，跳过调配逻辑");
                    }
                }

                // 🔥 修复：统一更新时间戳
                // 无论是势力级还是军区级调用，都更新时间戳，确保60天冷却生效
                LastPersonnelAIDate = currentDate;     // 更新执行时间
                IsPersonnelUrgentDirty = false;        // 清除紧急标记

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    string caller = targetList == null ? "势力级" : "军区级";
                    System.Diagnostics.Debug.WriteLine($"[⏰ PersonnelAI] {this.Name} 人员调配执行完毕({caller}调用)，下次执行将在{PERSONNEL_INTERVAL_NORMAL}天后或紧急事件发生时");
                }
#endif
            }
            finally
            {
                _isRunningPublicPersonnel = false;
            }
        }

        /// <summary>
        /// 🔥 三巨头人员分配系统 V5.0 - 双模式支持
        /// 
        /// 势力AI模式：将人员集中分配给首都+评分最高的2个城市
        /// 军区AI模式：将人员集中分配给评分最高的3个城市（无首都概念）
        /// 
        /// 实现更激进的资源集中策略，确保重要城市有足够人员
        /// </summary>
        /// <summary>
        /// 🎯 三巨头人员分配系统 V6.0
        /// 基于人口一票否决制的精英城市选拔和动态人员分配
        /// </summary>
        private void DemandBasedPersonnelTransfer(List<Architecture> architectures)
        {
            if (architectures.Count <= 1) return;

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunPersonnel] 开始三巨头人员分配 - 城市数量:{architectures.Count}");
            }
#endif

            // 1. 算分
            Dictionary<Architecture, float> scores = new Dictionary<Architecture, float>();
            foreach (var a in architectures)
            {
                scores[a] = CalculatePersonnelDemand(a);

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunPersonnel] {a.Name}: {scores[a]:F1}分");
                }
#endif
            }

            // 2. 选拔"三巨头"
            List<Architecture> eliteCities = new List<Architecture>();

            // A. 首都入选 (老家)
            if (this.Capital != null && architectures.Contains(this.Capital))
            {
                eliteCities.Add(this.Capital);

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunPersonnel] 首都入选: {this.Capital.Name}");
                }
#endif
            }

            // B. 选拔剩下的
            // 过滤条件：(人口 >= 10000) 或者 (极其危险 > 80分)
            // 即使是关口，只要人口养起来了(比如后期发展过的关)，也有资格当大哥
            var topScorers = scores
                .Where(x => !eliteCities.Contains(x.Key))
                .Where(x => x.Key.Population >= 10000 || x.Value > 80.0f) // 🚨 核心修正：人口否决制
                .OrderByDescending(x => x.Value)
                .Take(3 - eliteCities.Count)
                .Select(x => x.Key)
                .ToList();

            eliteCities.AddRange(topScorers);

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunPersonnel] 精英城市选拔完成，共{eliteCities.Count}个:");
                foreach (var elite in eliteCities)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {elite.Name} (人口:{elite.Population}, 评分:{scores[elite]:F1})");
                }
            }
#endif

            // 3. 执行分配 (逻辑不变)
            int totalPersons = architectures.Sum(x => x.PersonCount);
            int eliteCount = eliteCities.Count; // 可能不足3个，比如只有首都能看，其他都是荒地

            // 动态计算配额
            int eliteQuota = (eliteCount > 0) ? (totalPersons / eliteCount) : 0;

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunPersonnel] 总人数:{totalPersons}, 精英数量:{eliteCount}, 精英配额:{eliteQuota}");
            }
#endif

            foreach (var arch in architectures)
            {
                int idealCount = 0;

                if (eliteCities.Contains(arch))
                {
                    // 精英吃肉
                    idealCount = eliteQuota;

#if DEBUG
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RunPersonnel] {arch.Name} (精英) -> {idealCount}人");
                    }
#endif
                }
                else
                {
                    // 闲城喝汤
                    // 只有当总人数非常富裕 (比如每座城平均都有5人以上) 时，才给小城分1个人
                    // 否则一律 0 人
                    if (totalPersons > architectures.Count * 3)
                    {
                        idealCount = 1;
                    }
                    else
                    {
                        idealCount = 0;
                    }

                    // 唯一的例外：如果你是"前线"且"稍微有点危险(分>50)"，虽然没进Top3，也不能空城
                    if (scores[arch] > 50.0f)
                    {
                        idealCount = 1;
                    }

#if DEBUG
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RunPersonnel] {arch.Name} (普通) -> {idealCount}人 (评分:{scores[arch]:F1})");
                    }
#endif
                }

                // 执行人员调整
                this.AdjustPersonsForArchitecture(arch, idealCount, architectures);
            }

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunPersonnel] 三巨头人员分配完成");
            }
#endif
        }

        /// <summary>
        /// 🔥 NEW: 整合战略地图的威胁评估系统
        /// 结合即时威胁和战略势能图进行综合评估
        /// 与AI管理系统深度协调
        /// </summary>
        private float CalculateIntegratedThreatLevel(Architecture arch)
        {
            float dangerLevel = 0f;
            bool isFrontLine = arch.FrontLine;

            // =========================================================
            // 1. 即时威胁评估 (原有逻辑)
            // =========================================================
            var enemies = arch.GetHostileTroopsInView();
            if (enemies.Count > 0)
            {
                // 计算战力比：敌方总兵力 / (我方驻军 + 10000保底)
                long enemyForce = 0;
                foreach (Troop enemy in enemies)
                {
                    enemyForce += enemy.FightingForce;
                }

                long myForce = arch.MilitaryCount + 10000;
                dangerLevel = (float)enemyForce / myForce;

                // 修正：如果耐久度很低，危险系数倍增
                if (arch.Endurance < arch.Kind.EnduranceBase * 0.3f)
                {
                    dangerLevel *= 3.0f; // 城墙快塌了，极其危险
                }
            }

            // 🔥 NEW: 与AIManager协调 - 获取全局威胁态势
            try
            {
                var aiManager = GameManager.AIManager.Instance;
                if (aiManager != null)
                {
                    // 从AI管理器获取影响力地图信息
                    var influenceMap = aiManager.GetInfluenceMap(this.ID);
                    if (influenceMap != null)
                    {
                        // 基于影响力地图调整威胁评估 - 使用安全的方法调用
                        try
                        {
                            // 直接调用GetInfluence方法
                            float influenceThreat = influenceMap.GetInfluence(arch.Position);
                            if (influenceThreat > 0.1f)
                            {
                                dangerLevel = Math.Max(dangerLevel, influenceThreat * 0.5f);

#if DEBUG
                                if (SectionAIHelper.EnableDebugOutput)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[🤖 AIManager] {arch.Name}: 影响力威胁={influenceThreat:F2}, 调整后威胁={dangerLevel:F2}");
                                }
#endif
                            }
                        }
                        catch
                        {
                            // GetThreat方法不存在时，使用替代逻辑
                            // 基于现有信息估算威胁
                            if (arch.GetHostileTroopsInView().Count > 0)
                            {
                                float estimatedThreat = Math.Min(arch.GetHostileTroopsInView().Count * 0.1f, 1.0f);
                                dangerLevel = Math.Max(dangerLevel, estimatedThreat * 0.5f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[⚠️ AIManagerCoordination] {arch.Name}: AIManager协调失败 - {ex.Message}");
#endif
            }

            // =========================================================
            // 2. 战略势能图威胁评估 (NEW) - 简化版本
            // =========================================================
            try
            {
                // 使用现有的威胁评估逻辑替代战略地图
                if (dangerLevel == 0f && arch.FrontLine)
                {
                    // 前线城市即使没有即时威胁也有基础威胁值
                    dangerLevel = 0.2f;
                }

                // 基于邻近敌对势力的威胁评估
                if (arch.HostileLine)
                {
                    dangerLevel = Math.Max(dangerLevel, 0.3f);
                }

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput && dangerLevel > 0.1f)
                {
                    System.Diagnostics.Debug.WriteLine($"[🗺️ SimplifiedThreat] {arch.Name}: 前线威胁={arch.FrontLine}, 敌对线={arch.HostileLine}, 综合威胁={dangerLevel:F2}");
                }
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[⚠️ ThreatIntegration] {arch.Name}: 威胁评估失败 - {ex.Message}");
#endif
                // 威胁评估失败时，回退到原有逻辑
            }

            return dangerLevel;
        }

        /// <summary>
        /// 🔥 计算某个据点的人员需求评分
        /// 综合考虑建筑类型、人口规模、资源状况、战争状态、耐久度等因素
        /// </summary>
        /// <summary>
        /// 🧠 智能人员需求评估系统 V6.0
        /// 基于三层优先级：战争高压 > 发展潜力 > 边角料据点
        /// 实现人口一票否决制和精英保留原则
        /// </summary>
        /// <param name="arch">目标城市</param>
        /// <returns>人员需求评分 (0.1-120+)</returns>
        private float CalculatePersonnelDemand_Old(Architecture arch)
        {
#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelDemand] 评估城市: {arch.Name}");
            }
#endif

            // ==========================================
            // 1. 战争高压 (War) -> 最高优先级
            // ==========================================
            // 不管有没有人，只要被打，就是爹。
            if (arch.HasHostileTroopsInView())
            {
                float warScore = 100.0f + (arch.GetHostileTroopsInView().Count * 20.0f);

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelDemand] 战争状态 - 敌军:{arch.GetHostileTroopsInView().Count}, 评分:{warScore:F1}");
                }
#endif

                return warScore;
            }

            // ==========================================
            // 2. 发展潜力 (Development) -> 人口一票否决
            // ==========================================
            // 设定人口红线 (可配置)
            int populationThreshold = 10000; // 只有人口达标，才计算征兵/内政的高分

            if (arch.Population >= populationThreshold)
            {
                // === 征兵重镇评分 ===
                // 降低一点征兵门槛，只要人多就是好地方
                bool canRecruit = arch.Population >= 5000; // 游戏设定的硬性征兵线
                int idealTroops = Math.Min(arch.Population / 5, 50000);
                bool needsTroops = arch.MilitaryCount < idealTroops;

                if (canRecruit && needsTroops)
                {
                    // 基础分 30
                    float score = 30.0f;

                    // 人口红利：人越多分越高 (5万人口 -> +25分)
                    score += (arch.Population / 2000.0f);

                    // 资金红利
                    score += (arch.Fund / 10000.0f);

#if DEBUG
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelDemand] 征兵重镇 - 人口:{arch.Population}, 兵力:{arch.MilitaryCount}/{idealTroops}, 评分:{score:F1}");
                    }
#endif

                    return score; // 直接返回高分
                }

                // === 即使不缺兵，人多也值得搞内政 ===
                // 只有人口大城，内政分才值得被计算
                float normalScore = 5.0f; // 基础分提高

                // 没满的内政加分
                float saturation = arch.InternalAffairSaturationThreshold;
                if (arch.Agriculture < arch.AgricultureCeiling * saturation) normalScore += 2.0f;
                if (arch.Commerce < arch.CommerceCeiling * saturation) normalScore += 2.0f;

#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelDemand] 内政维护 - 人口:{arch.Population}, 评分:{normalScore:F1}");
                }
#endif

                return normalScore;
            }

            // ==========================================
            // 3. 边角料据点 (Low Pop) -> 一票否决
            // ==========================================
            // 人口 < 10000 且 没仗打
            // 这种地方只配拿 0.1 分 (几乎分不到人，除非全势力人都满了)

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelDemand] 边角料据点 - 人口:{arch.Population}, 评分:0.1");
            }
#endif

            return 0.1f;
        }

        /// <summary>
        /// 🎯 兵役人口状态缓存系统
        /// 避免每回合重复检查，提升性能
        /// </summary>
        private static readonly Dictionary<int, MilitaryPopulationCache> _militaryPopulationCache =
            new Dictionary<int, MilitaryPopulationCache>();

        private struct MilitaryPopulationCache
        {
            public bool HasMilitaryPopulation;
            public int LastCheckDay;
            public int RecoveryDay; // 预计恢复日期
        }

        /// <summary>
        /// 获取缓存的兵役人口状态
        /// </summary>
        private bool GetCachedMilitaryPopulationStatus(Architecture arch)
        {
            int currentDay = Session.Current.Scenario.Date.Day;

            if (_militaryPopulationCache.TryGetValue(arch.ID, out var cache))
            {
                // 如果还在恢复期内，直接返回false
                if (!cache.HasMilitaryPopulation && currentDay < cache.RecoveryDay)
                {
                    return false;
                }

                // 如果超过恢复期或者状态为true，需要重新检查
                if (currentDay >= cache.RecoveryDay || cache.HasMilitaryPopulation)
                {
                    return UpdateMilitaryPopulationCache(arch, currentDay);
                }

                return cache.HasMilitaryPopulation;
            }
            else
            {
                // 首次检查，建立缓存
                return UpdateMilitaryPopulationCache(arch, currentDay);
            }
        }

        /// <summary>
        /// 更新兵役人口缓存
        /// </summary>
        private bool UpdateMilitaryPopulationCache(Architecture arch, int currentDay)
        {
            bool hasMilitaryPopulation = arch.MilitaryPopulation > 0;

            // 🔥 FIXED: 计算到下一个季度的天数（兵役人口按季度恢复：3、6、9、12月）
            int recoveryDay = currentDay;
            if (!hasMilitaryPopulation)
            {
                var currentDate = Session.Current.Scenario.Date;
                int currentMonth = currentDate.Month;
                int currentYear = currentDate.Year;

                // 找到下一个季度月份
                int nextSeasonMonth;
                if (currentMonth < 3) nextSeasonMonth = 3;
                else if (currentMonth < 6) nextSeasonMonth = 6;
                else if (currentMonth < 9) nextSeasonMonth = 9;
                else if (currentMonth < 12) nextSeasonMonth = 12;
                else
                {
                    nextSeasonMonth = 3;
                    currentYear++; // 跨年到下一年3月
                }

                // 计算到下一个季度的天数
                // 简化计算：假设每月30天
                int monthsToWait = (nextSeasonMonth - currentMonth + 12) % 12;
                if (monthsToWait == 0) monthsToWait = 3; // 如果当前就是季度月，等到下一个季度

                recoveryDay = currentDay + monthsToWait * 30;
            }

            var cache = new MilitaryPopulationCache
            {
                HasMilitaryPopulation = hasMilitaryPopulation,
                LastCheckDay = currentDay,
                RecoveryDay = recoveryDay
            };

            _militaryPopulationCache[arch.ID] = cache;

#if DEBUG
            if (!hasMilitaryPopulation && SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[🔍 MilitaryPopulationCache] {arch.Name} 兵役人口枯竭，预计下个季度({recoveryDay}日)恢复");
            }
#endif

            return hasMilitaryPopulation;
        }

        /// <summary>
        /// 清理过期的兵役人口缓存（定期调用以释放内存）
        /// </summary>
        public static void CleanupMilitaryPopulationCache()
        {
            if (Session.Current?.Scenario?.Date == null) return;

            int currentDay = Session.Current.Scenario.Date.Day;
            var keysToRemove = new List<int>();

            foreach (var kvp in _militaryPopulationCache)
            {
                // 清理超过100天未更新的缓存
                if (currentDay - kvp.Value.LastCheckDay > 100)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (int key in keysToRemove)
            {
                _militaryPopulationCache.Remove(key);
            }
        }

        /// <summary>
        /// 调整某个城市的人员数量到目标值
        /// </summary>
        private void AdjustPersonsForArchitecture(Architecture target, int idealCount, List<Architecture> allArchitectures)
        {
            int currentMovableCount = GetMovableOfficers(target).Count + 1; // +1 为基础人员
            int difference = idealCount - currentMovableCount;

            if (difference == 0) return;

            if (difference > 0)
            {
                // 需要增加人员：从其他城市调人过来
                TransferPersonsToArchitecture(target, difference, allArchitectures);
            }
            else
            {
                // 需要减少人员：把多余的人调到其他城市
                TransferPersonsFromArchitecture(target, -difference, allArchitectures);
            }
        }

        /// <summary>
        /// 从其他城市向目标城市调派人员
        /// </summary>
        private void TransferPersonsToArchitecture(Architecture target, int needed, List<Architecture> allArchitectures)
        {
            // 找出可以提供人员的城市（有可调动人员的城市）
            var donors = allArchitectures
                .Where(a => a != target && GetMovableOfficers(a).Count > 0) // 有可调动人员
                .OrderByDescending(a => GetMovableOfficers(a).Count) // 优先从可调动人员多的城市调
                .ToList();

            int transferred = 0;
            foreach (var donor in donors)
            {
                if (transferred >= needed) break;

                var candidates = GetMovableOfficers(donor);
                if (candidates.Count == 0) continue;

                int toTransfer = Math.Min(needed - transferred, candidates.Count);
                int actualTransferred = 0;

                foreach (var person in candidates.Take(toTransfer))
                {
                    person.MoveToArchitecture(target);
                    actualTransferred++;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[🔍 PersonnelTransfer] {person.Name} 从 {donor.Name} 调至 {target.Name}");
#endif
                }

                transferred += actualTransferred;
            }

#if DEBUG
            if (transferred > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[🔍 PersonnelTransfer] {target.Name} 成功调入 {transferred} 人 (需求 {needed} 人)");
            }
#endif
        }

        /// <summary>
        /// 从目标城市调出多余人员到其他城市
        /// </summary>
        private void TransferPersonsFromArchitecture(Architecture source, int excess, List<Architecture> allArchitectures)
        {
            // 找出需要人员的城市
            var receivers = allArchitectures
                .Where(a => a != source)
                .OrderBy(a => a.PersonCount) // 优先调到人少的城市
                .ToList();

            var candidates = GetMovableOfficers(source);
            int transferred = 0;

            foreach (var person in candidates.Take(excess))
            {
                // 选择最合适的接收城市
                var receiver = receivers.OrderBy(r => r.PersonCount).FirstOrDefault();
                if (receiver != null)
                {
                    person.MoveToArchitecture(receiver);
                    transferred++;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[🔍 PersonnelTransfer] {person.Name} 从 {source.Name} 调至 {receiver.Name}");
#endif
                }
            }

#if DEBUG
            if (transferred > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[🔍 PersonnelTransfer] {source.Name} 成功调出 {transferred} 人 (多余 {excess} 人)");
            }
#endif
        }

        /// <summary>
        /// 高级人事调配逻辑 (从 Section.cs 迁移并升级)
        /// 包含文武分流、前线支援、后方建设等策略
        /// </summary>
        public void AdvancedPersonnelTransfer(ArchitectureList scopeArchitectures)
        {
            // 1. 识别前线和后方
            var frontLineCities = new List<Architecture>();
            var rearCities = new List<Architecture>();

            foreach (Architecture a in scopeArchitectures)
            {
                if (a.FrontLine)
                {
                    frontLineCities.Add(a);
                }
                else
                {
                    rearCities.Add(a);
                }
            }

            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelAI] AdvancedPersonnelTransfer - 前线: {frontLineCities.Count}, 后方: {rearCities.Count}");
            }

            // 如果全是前线或全是后方，就只做简单的平均分配
            if (frontLineCities.Count == 0 || rearCities.Count == 0)
            {
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 全为前线或全为后方，执行简单平衡");
                }
                BalancePersonnelCount(scopeArchitectures);
                return;
            }

            // 2. 【文武分流】核心逻辑
            // A. 把后方的猛将运到前线
            MoveMilitaryOfficersToFront(rearCities, frontLineCities);

            // B. 把前线的纯文官运到后方 (除非前线极度缺人)
            MoveCivilOfficersToRear(frontLineCities, rearCities);

            // 3. 【数量平衡】逻辑
            BalancePersonnelCount(scopeArchitectures);
        }

        /// <summary>
        /// 逻辑A：猛将上前线
        /// </summary>
        private void MoveMilitaryOfficersToFront(List<Architecture> sources, List<Architecture> targets)
        {
            foreach (var source in sources)
            {
                // 找出该城中所有闲置武将
                var availableOfficers = GetMovableOfficers(source);

                foreach (var p in availableOfficers)
                {
                    // 判定标准：统率 > 70 或 武力 > 75，且不仅是纯文官
                    if (p.Command > 70 || p.Strength > 75)
                    {
                        // 找一个武将最少，或者统率总和最低的前线城市
                        var target = targets.OrderBy(t => t.PersonCount).FirstOrDefault();

                        if (target != null)
                        {
                            if (SectionAIHelper.EnableDebugOutput)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 猛将调动: {p.Name} 从 {source.Name} -> {target.Name} (统{p.Command}/武{p.Strength})");
                            }
                            p.MoveToArchitecture(target);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 逻辑B：文官回后方
        /// </summary>
        private void MoveCivilOfficersToRear(List<Architecture> sources, List<Architecture> targets)
        {
            foreach (var source in sources)
            {
                // 如果前线人本来就很少（比如少于5人），别调走了，留着搬砖也好
                if (source.PersonCount < 5) continue;

                var availableOfficers = GetMovableOfficers(source);

                foreach (var p in availableOfficers)
                {
                    // 判定标准：不能打仗（统率武力双低），但是会种田（政治高）
                    bool isUselessInWar = p.Command < 60 && p.Strength < 60;
                    bool isGoodAtDomestic = p.Politics > 70 || p.Glamour > 70;

                    if (isUselessInWar && isGoodAtDomestic)
                    {
                        // 找一个后方城市（优先去金钱/粮食产量高但缺太守的，或者随便一个不满员的）
                        var target = targets.OrderBy(t => t.PersonCount).FirstOrDefault();

                        if (target != null)
                        {
                            if (SectionAIHelper.EnableDebugOutput)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 文官后撤: {p.Name} 从 {source.Name} -> {target.Name} (统{p.Command}/武{p.Strength}/政{p.Politics})");
                            }
                            p.MoveToArchitecture(target);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 逻辑C：保底平衡
        /// 避免出现有的城20人，有的城0人
        /// </summary>
        private void BalancePersonnelCount(ArchitectureList cities)
        {
            // 使用 ArchitectureList
            var archs = new List<Architecture>();
            foreach (var obj in cities)
            {
                if (obj is Architecture a) archs.Add(a);
            }

            if (archs.Count == 0) return;

            // 计算平均值
            int total = archs.Sum(a => a.PersonCount);
            int averageCount = total / archs.Count;

            // 找出人太多的城
            var crowdedCities = archs.Where(a => a.PersonCount > averageCount + 2).ToList();
            // 找出人太少的城
            var emptyCities = archs.Where(a => a.PersonCount < averageCount - 2).ToList();

            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 平衡检查 - 势力: {this.Name}, 平均: {averageCount}, 拥挤城: {crowdedCities.Count}, 空置城: {emptyCities.Count}");
            }

            foreach (var source in crowdedCities)
            {
                if (emptyCities.Count == 0) break;

                var movers = GetMovableOfficers(source);
                // 拿出多余的人
                int moveCount = source.PersonCount - averageCount;

                if (movers.Count == 0 && moveCount > 0)
                {
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 拥挤警告: {source.Name} 人数 {source.PersonCount} > 平均 {averageCount}，但无可调动人员");
                    }
                }

                for (int i = 0; i < moveCount && i < movers.Count; i++)
                {
                    var orderedCities = emptyCities.OrderBy(c => c.PersonCount);
                    var target = orderedCities.FirstOrDefault();
                    if (target == null) break;

                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 平衡调动: {movers[i].Name} 从 {source.Name} -> {target.Name} (平均分配)");
                    }
                    movers[i].MoveToArchitecture(target);

                    if (target.PersonCount >= averageCount - 2)
                    {
                        emptyCities.Remove(target);
                        if (emptyCities.Count == 0) break;
                    }
                }
            }
        }

        /// <summary>
        /// 辅助：获取一个城市里能动的武将
        /// </summary>
        private List<Person> GetMovableOfficers_Old(Architecture arch)
        {
            var list = new List<Person>();
            foreach (Person p in arch.Persons)
            {
                if (p.Status == PersonStatus.Normal && // 状态正常
                    p != arch.Mayor &&                 // 不是太守
                    p != this.Leader &&                // 不是君主 (Faction级别是Leader)
                    (p.BelongedFaction == null || p.BelongedFaction.Leader != p) &&  // 双重保险
                    p.LocationArchitecture == arch &&  // 确保人确实在城里
                    !p.NvGuan                          // 排除女官，女官无法直接调动
                   )
                {
                    list.Add(p);
                }
            }

            if (list.Count == 0 && arch.Persons.Count > 1)
            {
                // 如果城里有人（多于1个，排除太守/君主），但没人能调动，打印原因
                if (SectionAIHelper.EnableDebugOutput)
                {
                    foreach (Person p in arch.Persons)
                    {
                        if (p == arch.Mayor || p == this.Leader) continue;
                        if (p.Status != PersonStatus.Normal || p.LocationArchitecture != arch)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 无法调遣 {p.Name} (@{arch.Name}): 状态={p.Status}, 位置匹配={p.LocationArchitecture == arch}");
                        }
                        else if (p.NvGuan)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PersonnelAI] 无法调遣 {p.Name} (@{arch.Name}): 女官无法直接调动");
                        }
                    }
                }
            }
            return list;
        }

        public void RunDomestic(IEnumerable<Architecture> targetList = null)
        {
            // 🔥 NEW: 时间控制检查 - 只有满足条件才执行
            GameDate currentDate = Session.Current.Scenario.Date;
            if (!ShouldRunDomesticAI(currentDate))
            {
#if DEBUG
                if (SectionAIHelper.EnableDebugOutput)
                {
                    int daysSinceLastRun = CalculateDaysDifference(LastDomesticAIDate, currentDate);
                    System.Diagnostics.Debug.WriteLine($"[⏰ DomesticAI] {this.Name} 跳过内政AI: 距上次执行{daysSinceLastRun}天，未达到触发条件");
                }
#endif
                return; // 时间未到且无紧急情况，直接返回
            }

            // 如果传入了 targetList（军区调用），就用传入的；
            // 否则（势力主AI调用），就默认使用 this.Architectures
            // 🔥 SAFE ITERATION: 创建列表副本，防止遍历过中集合被修改导致 invalid operation
            IEnumerable<Architecture> source = targetList ?? this.Architectures.Cast<Architecture>();
            List<Architecture> targets = new List<Architecture>();
            foreach (var a in source) { targets.Add(a); } // 手动复制以确保兼容性

            // 🔥 NEW: 调用高级人事管理的季度评估
            // 每季度的第一天 (1月1日, 4月1日, 7月1日, 10月1日) 执行一次
            if (currentDate.Day == 1 && (currentDate.Month == 1 || currentDate.Month == 4 || currentDate.Month == 7 || currentDate.Month == 10))
            {
                if (_advancedPersonnelManager == null) _advancedPersonnelManager = new AdvancedPersonnelManager(this);
                _advancedPersonnelManager.SeasonalAssessment();
            }

            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunDomestic] {this.Name} Start processing {targets.Count} architectures");
            }

            foreach (Architecture a in targets)
            {
                // [CRITICAL] 防止双重操作
                // 如果是“势力主AI”在运行 (targetList == null)
                // 且 该城池属于某个“已开启托管”的军区
                // -> 跳过，留给军区AI稍后处理
                // [MODIFIED] 允许托管军区执行AI逻辑 - 注释掉原有阻断逻辑
                /*
                if (targetList == null && a.BelongedSection != null && a.BelongedSection.AIDetail != null && a.BelongedSection.AIDetail.AutoRun)
                {
                    continue; 
                }
                */

                // System.Diagnostics.Debug.WriteLine($"[RunDomestic] {this.Name} -> {a.Name}");
                try
                {
                    a.RunPrepareAI();
                    a.RunDomesticAI();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunDomestic Error] {this.Name} in {a.Name}: {ex.Message}");
                }
            }

            // 🔥 NEW: 执行完毕后重置状态
            LastDomesticAIDate = currentDate;     // 更新执行时间
            IsDomesticUrgentDirty = false;        // 清除紧急标记

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[⏰ DomesticAI] {this.Name} 内政AI执行完毕，下次执行将在{DOMESTIC_INTERVAL_NORMAL}天后或紧急事件发生时");
            }
#endif
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[RunDomestic] {this.Name} Finished");
            }
        }

        public void RunMilitary(IEnumerable<Architecture> targetList = null)
        {
            // 1. 动态决定"思考间隔"
            // 默认和平时期：10天思考一次
            int interval = 10;

            // 2. 扫描威胁 (这是轻量级操作)
            bool isAtWar = false;
            IEnumerable<Architecture> source = targetList ?? this.Architectures.Cast<Architecture>();
            List<Architecture> targets = new List<Architecture>();
            foreach (var a in source) { targets.Add(a); }

            foreach (var arch in targets)
            {
                // 只要有一个城视野里有敌人，就进入【战时高频模式】
                if (arch.HasHostileTroopsInView())
                {
                    isAtWar = true;
                    interval = 1; // 战时：每天(或每2天)都要思考出兵
                    break;
                }
            }

            // 3. 执行判断
            DaysSinceLastMilitary++;
            if (DaysSinceLastMilitary >= interval)
            {
                // 真正执行原本的 RunMilitary 逻辑
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunMilitary] {this.Name} Start processing {targets.Count} architectures (战时状态: {isAtWar})");
                }

                foreach (Architecture a in targets)
                {
                    // [MODIFIED] 允许托管军区执行AI逻辑 - 注释掉原有阻断逻辑
                    /*
                    if (targetList == null && a.BelongedSection != null && a.BelongedSection.AIDetail != null && a.BelongedSection.AIDetail.AutoRun)
                    {
                        continue;
                    }
                    */

                    // System.Diagnostics.Debug.WriteLine($"[RunMilitary] {this.Name} -> {a.Name}");
                    try
                    {
                        a.RunPrepareAI();
                        a.RunMilitaryAI();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RunMilitary Error] {this.Name} in {a.Name}: {ex.Message}");
                    }
                }

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunMilitary] {this.Name} Finished");
                }

                // 重置计时器
                DaysSinceLastMilitary = 0;
            }
        }

        private void AICapital()
        {
            if ((this.ArchitectureCount != 0) && (this.Capital != null))
            {
                Architecture architecture;
                int num2;
                if ((this.Capital.Endurance < 30) && (this.Capital.RecentlyAttacked > 0))
                {
                    if (this.Capital.ChangeCapitalAvail() && this.Capital.HasHostileTroopsInView())
                    {
                        float rationRate = 0f;
                        if (this.Capital.GetRelationUnderZeroTroopFightingForceInView(out rationRate) > (this.Capital.GetFriendlyTroopFightingForceInView() * 3))
                        {
                            this.Capital.DecreaseFund(this.Capital.ChangeCapitalCost);
                            architecture = this.SelectNewCapital();
                            if (this.Capital.Fund > this.Capital.EnoughFund)
                            {
                                num2 = this.Capital.Fund - this.Capital.EnoughFund;
                                this.Capital.DecreaseFund(num2);
                                //architecture.AddFundPack(num2, (int)(Session.Current.Scenario.GetDistance(this.Capital.ArchitectureArea, architecture.ArchitectureArea) / 5.0));
                                architecture.AddFundPack(num2, (int)(Session.Current.Scenario.GetDistance(this.Capital.ArchitectureArea, architecture.ArchitectureArea) / 5.0));
                            }
                            this.ChangeCapital(architecture);
                        }
                    }
                }
                else if (this.Capital.ChangeCapitalAvail())
                {
                    architecture = this.SelectNewCapital();
                    if (((architecture != null) && (architecture.Population > (this.Capital.Population + 0x2710))) && ((architecture.Endurance * architecture.Domination) > (this.Capital.Endurance * this.Capital.Domination)))
                    {
                        this.Capital.DecreaseFund(this.Capital.ChangeCapitalCost);
                        if (this.Capital.Fund > this.Capital.EnoughFund)
                        {
                            num2 = this.Capital.Fund - this.Capital.EnoughFund;
                            this.Capital.DecreaseFund(num2);
                            //architecture.AddFundPack(num2, (int)(Session.Current.Scenario.GetDistance(this.Capital.ArchitectureArea, architecture.ArchitectureArea) / 5.0));
                            architecture.AddFundPack(num2, (int)(Session.Current.Scenario.GetDistance(this.Capital.ArchitectureArea, architecture.ArchitectureArea) / 5.0));
                        }
                        this.ChangeCapital(architecture);
                    }
                }
            }
        }

        private void AICaptives()
        {
            this.AISelfReleaseCaptives();
            this.AIRedeemCaptives();
        }

        private void AILegions()
        {
            // 1. 先清理已完成的军团
            this.CleanupCompletedLegions();
            
            // 没有军团时不输出日志，避免刷屏
            if (this.Legions.Count == 0) return;
            
            System.Diagnostics.Debug.WriteLine($"[Faction.AILegions] 势力{this.Name} 开始执行军团AI，军团数:{this.Legions.Count}");
            int executedCount = 0;
            foreach (GameObject obj in this.Legions.GetRandomList())
            {
                // 🔥 C# 12: 使用模式匹配替代三元运算符
                if (obj is not Legion legion) continue;
                
                System.Diagnostics.Debug.WriteLine($"[Faction.AILegions] 调用军团AI: {legion.Name}({legion.Kind})，部队数:{legion.Troops.Count}");
                legion.AI();
                executedCount++;
            }
            System.Diagnostics.Debug.WriteLine($"[Faction.AILegions] 势力{this.Name} 军团AI执行完毕，执行数:{executedCount}");
        }

        private void AITrainChildren()
        {
            if (GameObject.Random(90 / Session.Current.Scenario.Parameters.DayInTurn) == 0)
            {
                foreach (Person p in this.Children)
                {
                    if (!p.Trainable) continue;
                    if (Session.Current.Scenario.IsPlayer(this) && (p.Father == this.Leader || p.Mother == this.Leader)) continue;

                    if (p.Age >= 5 && p.Age < 8)
                    {
                        Dictionary<TrainPolicy, float> candidates = new Dictionary<TrainPolicy, float>();
                        foreach (TrainPolicy tp in Session.Current.Scenario.GameCommonData.AllTrainPolicies)
                        {
                            float c = (p.CommandPotential - p.Command) * tp.Command / tp.WeightSum + 1;
                            float s = (p.StrengthPotential - p.Strength) * tp.Strength / tp.WeightSum + 1;
                            float i = (p.IntelligencePotential - p.Intelligence) * tp.Intelligence / tp.WeightSum + 1;
                            float o = (p.PoliticsPotential - p.Politics) * tp.Politics / tp.WeightSum + 1;
                            float g = (p.GlamourPotential - p.Glamour) * tp.Glamour / tp.WeightSum + 1;
                            candidates.Add(tp, c + s + i + o + g);
                        }
                        p.TrainPolicy = candidates.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                    }
                    else if (p.Age >= 8)
                    {
                        float unfinishedSkillFactor;
                        int unlearnedSkillCount = 0;
                        int learnedSkillCount = 0;
                        foreach (Skill j in p.Father.GetSkillList())
                        {
                            learnedSkillCount += j.Merit;
                            if (!p.HasSkill(j.ID))
                            {
                                unlearnedSkillCount += j.Merit;
                            }
                        }
                        foreach (Skill j in p.Mother.GetSkillList())
                        {
                            learnedSkillCount += j.Merit;
                            if (!p.HasSkill(j.ID))
                            {
                                unlearnedSkillCount += j.Merit;
                            }
                        }
                        int total = learnedSkillCount + unlearnedSkillCount;
                        if (total > 0)
                        {
                            unfinishedSkillFactor = ((float)unlearnedSkillCount / total);
                        }
                        else
                        {
                            unfinishedSkillFactor = 0;
                        }

                        float unfinishedStuntFactor;
                        int unlearnedStuntCount = 0;
                        int learnedStuntCount = 0;
                        foreach (Stunt j in p.Father.GetStuntList())
                        {
                            learnedStuntCount++;
                            if (!p.HasStunt(j.ID))
                            {
                                unlearnedStuntCount++;
                            }
                        }
                        foreach (Stunt j in p.Mother.GetStuntList())
                        {
                            learnedStuntCount++;
                            if (!p.HasStunt(j.ID))
                            {
                                unlearnedStuntCount++;
                            }
                        }
                        total = learnedStuntCount + unlearnedStuntCount;
                        if (total > 0)
                        {
                            unfinishedStuntFactor = ((float)unlearnedStuntCount / total);
                        }
                        else
                        {
                            unfinishedStuntFactor = 0;
                        }

                        float unfinishedTitleFactor;
                        int unlearnedTitleLevels = 0;
                        int learnedTitleLevels = 0;
                        foreach (TitleKind tk in Session.Current.Scenario.GameCommonData.AllTitleKinds.TitleKinds.Values)
                        {
                            if (!tk.RandomTeachable) continue;
                            Title fatherTitle = p.Father.getTitleOfKind(tk);
                            Title motherTitle = p.Mother.getTitleOfKind(tk);
                            Title pTitle = p.getTitleOfKind(tk);

                            int fm = 0;
                            int mm = 0;
                            if (fatherTitle != null && fatherTitle.CanBeBorn(p))
                            {
                                fm = fatherTitle.Merit;
                            }
                            if (motherTitle != null && motherTitle.CanBeBorn(p))
                            {
                                mm = motherTitle.Merit;
                            }

                            unlearnedTitleLevels += Math.Max(fm, mm);

                            if (pTitle != null)
                            {
                                learnedTitleLevels += pTitle.Merit;
                            }
                        }
                        if (unlearnedTitleLevels > 0)
                        {
                            unfinishedTitleFactor = (float)unlearnedTitleLevels / (unlearnedTitleLevels + learnedTitleLevels);
                        }
                        else
                        {
                            unfinishedTitleFactor = 0;
                        }

                        Dictionary<TrainPolicy, float> candidates = new Dictionary<TrainPolicy, float>();
                        foreach (TrainPolicy tp in Session.Current.Scenario.GameCommonData.AllTrainPolicies)
                        {
                            float c = (p.CommandPotential - p.Command) * tp.Command / tp.WeightSum + 1;
                            float s = (p.StrengthPotential - p.Strength) * tp.Strength / tp.WeightSum + 1;
                            float i = (p.IntelligencePotential - p.Intelligence) * tp.Intelligence / tp.WeightSum + 1;
                            float o = (p.PoliticsPotential - p.Politics) * tp.Politics / tp.WeightSum + 1;
                            float g = (p.GlamourPotential - p.Glamour) * tp.Glamour / tp.WeightSum + 1;

                            float skill = 0;
                            float stunt = 0;
                            float title = 0;
                            int abyMax = Math.Max(Math.Max(Math.Max(Math.Max(p.Strength, p.Command), p.Intelligence), p.Politics), p.Glamour);
                            int csiMax = Math.Max(Math.Max(p.Command, p.Strength), p.Intelligence);
                            if (abyMax > 50)
                            {
                                skill = (abyMax - 50) * (100 / 50.0f) * tp.Skill / tp.WeightSum + 1;
                                skill *= unfinishedSkillFactor;
                            }
                            if (csiMax > 60)
                            {
                                stunt = (csiMax - 60) * (100 / 40.0f) * tp.Stunt / tp.WeightSum + 1;
                                stunt *= unfinishedStuntFactor;
                            }
                            if (abyMax > 70 && p.Age >= 8)
                            {
                                title = (abyMax - 70) * (100 / 30.0f) * tp.Title / tp.WeightSum + 1;
                                title *= unfinishedTitleFactor;
                            }

                            candidates.Add(tp, c + s + i + o + g + skill + stunt + title);
                        }
                        p.TrainPolicy = candidates.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                    }
                }
            }
        }

        private void AIPrepare()
        {
            if ((Session.Current.Scenario.Date.Day <= Session.Current.Scenario.Parameters.DayInTurn) && ((Session.Current.Scenario.Date.Month % 3) == 1))
            {
                foreach (Architecture architecture in this.Architectures)
                {
                    architecture.CheckIsFrontLine();
                }
            }
        }

        private void AIRedeemCaptives()
        {
            if (this.HasSelfCaptive())
            {
                foreach (GameObject obj in this.SelfCaptives.GetRandomList())
                {
                    Captive captive = obj as Captive;
                    if (captive == null) continue;
                    
                    if (captive.BelongedFaction == null)
                    {
                        captive.CaptivePerson.SetBelongedCaptive(null, PersonStatus.Normal);
                        if ((captive.CaptivePerson != null) && (captive.CaptiveFaction != null))
                        {
                            captive.CaptivePerson.MoveToArchitecture(captive.CaptiveFaction.Capital);
                        }

                        continue;
                    }
                    if ((captive.BelongedFaction.Capital != null) && (captive.RansomArriveDays <= 0))
                    {
                        if (captive.CaptivePerson == this.Leader && this.Capital.Fund >= captive.Ransom)
                        {
                            captive.SendRansom(captive.BelongedFaction.Capital, this.Capital);
                            continue;
                        }
                        int diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(captive.BelongedFaction.ID, base.ID);
                        if ((diplomaticRelation >= 0) || (GameObject.Random(Math.Abs(diplomaticRelation) + 50) < 50))
                        {
                            int ransom = captive.Ransom;
                            if (GameObject.Random(ransom) > GameObject.Random(0x7d0))
                            {
                                foreach (Architecture architecture in captive.BelongedFaction.Capital.GetClosestArchitectures(Session.Current.Scenario.Architectures.Count - 1))
                                {
                                    if ((architecture.BelongedFaction != this) || ((architecture.PlanArchitecture != null) || !(architecture.IsFundEnough || !architecture.HasHostileTroopsInView())))
                                    {
                                        continue;
                                    }
                                    if (architecture.Fund >= ransom)
                                    {
                                        if (GameObject.Random(architecture.Fund) >= (ransom - 1))
                                        {
                                            captive.SendRansom(captive.BelongedFaction.Capital, architecture);
                                            break;
                                        }
                                        if (GameObject.Chance(10))
                                        {
                                            break;
                                        }
                                    }
                                    else if (GameObject.Chance(1))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AISections()
        {
            if ((this.ArchitectureCount != 0) && (Session.Current.Scenario.GameCommonData.AllSectionAIDetails.Count > 0))
            {
                this.RebuildSections();
                foreach (Section section in this.Sections.GetList())
                {
                    section.AI(new GameTime());
                }
            }
        }

        private void AISelfReleaseCaptives()
        {
            return;
            /*if (this.HasCaptive())
            {
                foreach (Captive captive in this.Captives.GetList())
                {
                    if (((captive.CaptiveFaction == null) || (captive.BelongedFaction == null)) || ((captive.BelongedFaction.Capital == null) || ((captive.LocationTroop != null) && (captive.LocationTroop.RecentlyFighting > 0))))
                    {
                        continue;
                    }
                    int diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(captive.CaptiveFaction.ID, base.ID);
                    if ((diplomaticRelation >= 0) && (GameObject.Random(diplomaticRelation + 50) >= GameObject.Random(50)))
                    {
                        captive.SelfReleaseCaptive();
                        continue;
                    }
                    if (diplomaticRelation < 0)
                    {
                        int chance = Math.Abs((int) (diplomaticRelation / 5));
                        if (chance >= 100)
                        {
                            chance = 0x63;
                        }
                        if (!(GameObject.Chance(chance) || (GameObject.Random(Math.Abs(diplomaticRelation) + 50) >= GameObject.Random(50))))
                        {
                            captive.SelfReleaseCaptive();
                            continue;
                        }
                    }
                }
            }*/
        }

        private void AITechniques()
        {
            if ((this.ArchitectureCount != 0) && (this.UpgradingTechnique < 0))
            {
                if (this.PlanTechnique == null)
                {
                    if (this.PreferredTechniqueKinds.Count > 0)
                    {
                        Dictionary<Technique, float> list = new Dictionary<Technique, float>();
                        float preferredTechniqueComplition = this.GetPreferredTechniqueComplition();
                        foreach (Technique technique in Session.Current.Scenario.GameCommonData.AllTechniques.Techniques.Values)
                        {
                            if (!this.IsTechniqueUpgradable(technique))
                            {
                                continue;
                            }
                            if (this.GetTechniqueUsefulness(technique) <= 0) continue;

                            float weight = 1;
                            foreach (KeyValuePair<Condition, float> c in technique.AIConditionWeight)
                            {
                                if (c.Key.CheckCondition(this))
                                {
                                    weight *= c.Value;
                                }
                            }

                            if (preferredTechniqueComplition < 0.5f)
                            {
                                if (this.PreferredTechniqueKinds.IndexOf(technique.Kind) >= 0)
                                {
                                    list.Add(technique, weight);
                                }
                            }
                            else if (preferredTechniqueComplition < 0.75f)
                            {
                                if ((this.PreferredTechniqueKinds.IndexOf(technique.Kind) >= 0) || GameObject.Chance(0x19))
                                {
                                    list.Add(technique, weight);
                                }
                            }
                            else if (preferredTechniqueComplition < 1f)
                            {
                                if ((this.PreferredTechniqueKinds.IndexOf(technique.Kind) >= 0) || GameObject.Chance(50))
                                {
                                    list.Add(technique, weight);
                                }
                            }
                            else if ((this.PreferredTechniqueKinds.IndexOf(technique.Kind) >= 0) || GameObject.Chance(0x4b))
                            {
                                list.Add(technique, weight);
                            }
                        }
                        if (list.Count > 0)
                        {
                            this.PlanTechnique = GameObject.WeightedRandom(list);
                        }
                        else
                        {
                            this.PlanTechnique = this.GetRandomTechnique();
                        }
                    }
                    else
                    {
                        this.PlanTechnique = this.GetRandomTechnique();
                    }
                }
                if (this.PlanTechnique != null)
                {
                    if (((this.TechniquePoint + this.TechniquePointForTechnique) >= this.getTechniqueActualPointCost(this.PlanTechnique)) && (this.Reputation >= this.getTechniqueActualReputation(this.PlanTechnique)))
                    {
                        if (this.ArchitectureCount > 1)
                        {
                            this.Architectures.PropertyName = "Fund";
                            this.Architectures.IsNumber = true;
                            this.Architectures.ReSort();
                        }
                        // 🔥 FIX: 检查是否有建筑
                        if (this.Architectures.Count > 0)
                        {
                            Architecture a = this.Architectures[0] as Architecture;
                            if (a != null && a.IsFundEnough)
                            {
                                this.PlanTechniqueArchitecture = this.Architectures[0] as Architecture;
                                if (this.PlanTechniqueArchitecture.Fund >= this.getTechniqueActualFundCost(this.PlanTechnique))
                                {
                                    this.DepositTechniquePointForTechnique(this.TechniquePointForTechnique);
                                    this.UpgradeTechnique(this.PlanTechnique, this.PlanTechniqueArchitecture);
                                    this.PlanTechniqueArchitecture = null;
                                    this.PlanTechnique = null;
                                }
                            }
                            else
                            {
                                this.PlanTechniqueArchitecture = null;
                                this.PlanTechnique = null;
                            }
                        }
                        else
                        {
                            this.PlanTechniqueArchitecture = null;
                            this.PlanTechnique = null;
                        }
                    }
                    else if ((this.Reputation >= this.getTechniqueActualReputation(this.PlanTechnique)) && GameObject.Chance(0x21))
                    {
                        this.SaveTechniquePointForTechnique(this.getTechniqueActualPointCost(this.PlanTechnique) / this.PlanTechnique.Days);
                    }
                    else if (GameObject.Chance(10))
                    {
                        this.PlanTechniqueArchitecture = null;
                        this.PlanTechnique = null;
                    }
                }
            }
        }

        public void ApplyTechniques()
        {
            foreach (Technique technique in this.AvailableTechniques.Techniques.Values)
            {
                technique.Influences.ApplyInfluence(this, GameObjects.Influences.Applier.Technique, technique.ID);
            }
        }

        private void SetSectionAIDetailPinAtPlayer()
        {
            if (Session.GlobalVariables.PinPointAtPlayer)
            {
                foreach (Section i in this.Sections)
                {
                    //if ((((this.FirstSection.ArchitectureScale / 2) - (this.FirstSection.ArchitectureCount / 2)) + 1) * 20 <= this.ArmyScale)
                    //{
                    //FactionList playerFactions = Session.Current.Scenario.PlayerFactions.GetRandomList() as FactionList;
                    FactionList playerFactions = Session.Current.Scenario.PlayerFactions;
                    bool assigned = false;
                    foreach (Architecture j in i.Architectures)
                    {
                        foreach (Faction k in playerFactions)
                        {
                            if (j.HasFactionInClose(k, 1))
                            {
                                GameObjectList sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.势力, true, true, true, false, true);
                                if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                {
                                    this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                    this.FirstSection.OrientationFaction = k;
                                    assigned = true;
                                }
                                break;
                            }
                        }
                        if (assigned) break;
                    }
                    //}
                }
            }
        }

        private void BuildSectionByArchitectureList(GameObjectList architecturelist)
        {
            if (architecturelist.Count != 0)
            {
                Section section;
                GameObjectList list;
                if (architecturelist.Count == 1)
                {
                    section = new Section();
                    section.ID = Session.Current.Scenario.Sections.GetFreeGameObjectID();
                    section.BelongedFaction = this; // 🔥 修复：设置所属势力
                    section.BelongedFactionID = this.ID; // 🔥 修复：同步 ID

                    this.AddSection(section);
                    Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                    
                    // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
                    list = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                        .GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                    
                    if (list.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"配置数据损坏：无法找到 SectionOrientationKind.无 的 SectionAIDetail 配置。" +
                            "游戏无法继续，请检查配置文件。");
                    }
                    
                    section.AIDetail = list[GameObject.Random(list.Count)] as SectionAIDetail;
                    
                    if (section.AIDetail == null)
                    {
                        throw new InvalidOperationException(
                            "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
                    }
                    
                    section.AddArchitecture(architecturelist[0] as Architecture);
                }
                else
                {
                    architecturelist.PropertyName = "Population";
                    architecturelist.IsNumber = true;
                    architecturelist.ReSort();
                    int count = 0;
                    int num2 = 2 + (this.ArchitectureCount / 8);
                    if (architecturelist.Count < ((num2 * 3) / 2))
                    {
                        count = architecturelist.Count;
                    }
                    else
                    {
                        count = num2;
                    }
                    section = new Section();
                    section.ID = Session.Current.Scenario.Sections.GetFreeGameObjectID();
                    section.BelongedFaction = this; // 🔥 修复：设置所属势力
                    section.BelongedFactionID = this.ID; // 🔥 修复：同步 ID

                    this.AddSection(section);
                    Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                    
                    // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
                    list = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                        .GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                    
                    if (list.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"配置数据损坏：无法找到 SectionOrientationKind.无 的 SectionAIDetail 配置。" +
                            "游戏无法继续，请检查配置文件。");
                    }
                    
                    section.AIDetail = list[GameObject.Random(list.Count)] as SectionAIDetail;
                    
                    if (section.AIDetail == null)
                    {
                        throw new InvalidOperationException(
                            "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
                    }
                    
                    Architecture architecture = architecturelist[0] as Architecture;
                    section.AddArchitecture(architecture);
                    if (architecture.ClosestArchitectures == null)
                    {
                        architecture.GetClosestArchitectures();
                    }
                    if (architecture.AIAllLinkNodes.Count == 0)
                    {
                        architecture.GenerateAllAILinkNodes(2);
                    }
                    foreach (LinkNode node in architecture.AIAllLinkNodes.Values)
                    {
                        if (node.Level > 2)
                        {
                            break;
                        }
                        if ((node.A.BelongedFaction == this) && architecturelist.HasGameObject(node.A))
                        {
                            section.AddArchitecture(node.A);
                        }
                        if (section.ArchitectureCount >= count)
                        {
                            break;
                        }
                    }
                    if (count == architecturelist.Count)
                    {
                        if (section.ArchitectureCount < count)
                        {
                            foreach (Architecture architecture2 in section.Architectures)
                            {
                                architecturelist.Remove(architecture2);
                            }
                            if (this.SectionCount == 1)
                            {
                                foreach (Architecture architecture2 in architecturelist)
                                {
                                    this.FirstSection.AddArchitecture(architecture2);
                                }
                            }
                            else
                            {
                                foreach (Architecture architecture2 in architecturelist)
                                {
                                    int num3 = 0x7fffffff;
                                    Section section2 = null;
                                    foreach (Section section3 in this.Sections)
                                    {
                                        int distanceFromSection = architecture2.GetDistanceFromSection(section3);
                                        if (distanceFromSection < num3)
                                        {
                                            num3 = distanceFromSection;
                                            section2 = section3;
                                        }
                                    }
                                    section2.AddArchitecture(architecture2);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (Architecture architecture2 in section.Architectures)
                        {
                            architecturelist.Remove(architecture2);
                        }
                        this.BuildSectionByArchitectureList(architecturelist);
                    }
                }
            }
        }

        public void ChangeCapital(Architecture newCapital)
        {
            if (this.Capital != newCapital)
            {
                Architecture capital = this.Capital;
                this.Capital = newCapital;
                Session.Current.Scenario.YearTable.addChangeCapitalEntry(Session.Current.Scenario.Date, this, newCapital);
                if (this.OnInitiativeChangeCapital != null)
                {
                    this.OnInitiativeChangeCapital(this, capital, this.Capital);
                }
                // 🔥 AOT 重构：使用强类型事件替代反射调用
                WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseChangeCapital(Session.Current.Scenario, this);

                // 🔥 首都变化触发人员调配紧急事件
                // 首都是人员分配的核心城市，首都变化意味着战略重心转移，需要立即重新调配人员
                // 🛡️ 防止递归：如果当前正在执行人员调配，不再触发通知（避免循环）
                if (!_isExecutingPersonnelAllocation)
                {
                    this.NotifyPersonnelUrgentEvent($"首都变更: {capital?.Name ?? "无"} → {newCapital.Name}");

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ChangeCapital] 势力 {this.Name} 首都变更: {capital?.Name ?? "无"} → {newCapital.Name}，触发人员调配紧急事件");
#endif
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ChangeCapital] 势力 {this.Name} 首都变更: {capital?.Name ?? "无"} → {newCapital.Name}，但正在执行人员调配，跳过触发");
#endif
                }
            }
        }

        public void ChangeFaction(Faction faction)
        {
            GameObjectList list = this.Architectures.GetList();
            foreach (Architecture architecture in list)
            {
                architecture.ChangeFaction(faction);
            }
            foreach (Architecture architecture in list)
            {
                architecture.CheckIsFrontLine();
            }
            foreach (Troop troop in this.Troops.GetList())
            {
                troop.ChangeFaction(faction);
            }
            foreach (Section section in this.Sections.GetList())
            {
                this.RemoveSection(section);
                Session.Current.Scenario.Sections.Remove(section);
            }

            this.Destroy();
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                architecture.RefreshViewArea();
            }
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                troop.RefreshViewArchitectureRelatedArea();
            }
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseChangeFaction(Session.Current.Scenario, this);
        }

        public void AfterChangeLeader(Faction newFaction, GameObjectList candidates, Person oldLeader, Person newLeader)
        {
            foreach (Architecture a in this.Architectures)
            {
                foreach (Person p in a.Feiziliebiao)
                {
                    a.PrincessChangeLeader(false, a.BelongedFaction, p);
                }
            }
            PersonList pl = new PersonList();
            pl.AddRange(candidates);

            Session.Current.Scenario.NewFaction(pl, true, oldLeader.Strain != newLeader.Strain && !newLeader.IsVeryCloseTo(oldLeader));
        }

        public Faction ChangeLeaderAfterLeaderDeath()
        {
            Person leader = this.Leader;
            Architecture locationArchitecture = this.Leader.LocationArchitecture;
            this.Leader.Status = GameObjects.PersonDetail.PersonStatus.None;
            // this.Leader.Available = false;
            // Session.Current.Scenario.Persons.Remove(this.Leader);
            Session.Current.Scenario.AvailablePersons.Remove(this.Leader);
            Person person2 = null;
            PersonList list = new PersonList();
            if (person2 == null)
            {
                if (this.Prince != null && this.Prince != this.Leader && this.Prince.BelongedFaction == this)
                {
                    person2 = this.Prince;
                }
            }

            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if ((person3.Father != null) && (person3.Sex == this.Leader.Sex) && (this.Leader == person3.Father) && person3 != this.Leader
                         && (person3.BelongedFaction == this || !person3.Available) && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }

            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if ((person3.Father != null) && (person3.Sex == this.Leader.Sex) && (this.Leader.Father == person3.Father)
                        && person3 != this.Leader && (person3.BelongedFaction == this || !person3.Available) && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if ((person3.Strain >= 0) && (person3.Sex == this.Leader.Sex) && (this.Leader.Strain == person3.Strain) && person3 != this.Leader
                         && (person3.BelongedFaction == this || !person3.Available) && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in this.Leader.Brothers)
                {
                    if (person3 != this.Leader && person3.BelongedFaction == this)
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "Glamour";
                        list.IsNumber = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if ((person3.Mother != null) && (person3.Sex == this.Leader.Sex) && ((this.Leader.Mother == person3.Mother) || (person3.Mother == this.Leader))
                        && person3 != this.Leader && person3.BelongedFaction == this && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if (person3.Father != null && (person3.Sex == this.Leader.Sex) && ((person3.Father.Father != null && person3.Father.Father == this.Leader) || (person3.Father.Mother != null && person3.Father.Mother == this.Leader))
                        && person3 != this.Leader && person3.BelongedFaction == this && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                foreach (Person person3 in Session.Current.Scenario.Persons)
                {
                    if (person3.Mother != null && (person3.Sex == this.Leader.Sex) && ((person3.Mother.Father != null && person3.Mother.Father == this.Leader) || (person3.Mother.Mother != null && person3.Mother.Mother == this.Leader))
                        && person3 != this.Leader && person3.BelongedFaction == this && person3.Alive && person3.YearBorn <= Session.Current.Scenario.Date.Year && (person3.ID < 7000 || person3.ID >= 8000))
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "YearBorn";
                        list.IsNumber = true;
                        list.SmallToBig = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null && Session.GlobalVariables.PermitFactionMerge && !this.IsAlien)
            {
                float num = -75;
                Faction diplomaticFaction = null;
                foreach (DiplomaticRelation relation in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    if (relation.RelationFaction1 == null || relation.RelationFaction2 == null) continue;
                    float attr = Person.GetIdealAttraction(relation.RelationFaction1.Leader, relation.RelationFaction2.Leader);
                    if ((relation.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold) &&
                        (num < attr) &&
                        !relation.RelationFaction1.IsAlien && !relation.RelationFaction2.IsAlien)
                    {
                        num = attr;
                        diplomaticFaction = relation.GetDiplomaticFaction(base.ID);
                    }
                }

                if (diplomaticFaction != null)
                {
                    Session.Current.Scenario.YearTable.addChangeFactionEntry(Session.Current.Scenario.Date, this, diplomaticFaction);
                    GameObjectList rebelCandidates = this.Persons.GetList();
                    this.ChangeFaction(diplomaticFaction);
                    foreach (Treasure treasure in leader.Treasures.GetList())
                    {
                        treasure.HidePlace = locationArchitecture;
                        leader.LoseTreasure(treasure);
                        treasure.Available = false;
                    }
                    this.AfterChangeLeader(diplomaticFaction, rebelCandidates, leader, diplomaticFaction.Leader);
                    return diplomaticFaction;
                }
            }
            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in this.Persons)
                {
                    if ((this.Leader.Ideal == person3.Ideal) && (person3.Sex == this.Leader.Sex) && person3 != this.Leader)
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "Merit";
                        list.IsNumber = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in this.Persons)
                {
                    if (person3.Sex == this.Leader.Sex && person3 != this.Leader)
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "Merit";
                        list.IsNumber = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 == null)
            {
                list.Clear();
                foreach (Person person3 in this.Persons)
                {
                    if (person3 != this.Leader)
                    {
                        list.Add(person3);
                    }
                }
                if (list.Count > 0)
                {
                    if (list.Count > 1)
                    {
                        list.PropertyName = "Merit";
                        list.IsNumber = true;
                        list.ReSort();
                    }
                    person2 = list[0] as Person;
                }
            }
            if (person2 != null)
            {
                if (!person2.Available)
                {
                    person2.Available = true;
                    Session.Current.Scenario.AvailablePersons.Add(person2);
                    person2.LocationArchitecture = this.Capital;
                    person2.Status = PersonStatus.Normal;
                    person2.YearJoin = Session.Current.Scenario.Date.Year;
                    Session.MainGame.mainGameScreen.xianshishijiantupian(person2, this.Capital.Name, TextMessageKind.PersonJoin, "PersonJoin", "", "", this.Name, false);
                    Session.Current.Scenario.YearTable.addGrownBecomeAvailableEntry(Session.Current.Scenario.Date, person2);
                }
                this.Leader = person2;
                if (!((this.Leader.LocationTroop == null) || this.Leader.IsCaptive))
                {
                    this.Leader.LocationTroop.RefreshWithPersonList(this.Leader.LocationTroop.Persons.GetList());
                }
                foreach (Treasure treasure in leader.Treasures.GetList())
                {
                    leader.LoseTreasure(treasure);
                    this.Leader.ReceiveTreasure(treasure);
                }
                // 🔥 AOT 重构：使用强类型事件替代反射调用
                WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseChangeKing(Session.Current.Scenario, this);
                Session.Current.Scenario.YearTable.addChangeKingEntry(Session.Current.Scenario.Date, this.Leader, this, leader);
                this.AfterChangeLeader(this, this.Persons, leader, this.Leader);
                return this;
            }
            foreach (Treasure treasure in leader.Treasures.GetList())
            {
                treasure.HidePlace = locationArchitecture;
                leader.LoseTreasure(treasure);
                treasure.Available = false;
            }

            return null;
        }

        public void CheckLeaderDeath(Person leader)
        {
            if ((((((leader.LocationArchitecture != null) && (leader.LocationArchitecture.BelongedFaction == this.Leader.BelongedFaction))
                || ((leader.LocationTroop != null) && (leader.LocationTroop.BelongedFaction == this.Leader.BelongedFaction)))
                && (GameObject.Random(leader.CaptiveAbility) < GameObject.Random(this.Leader.CaptiveAbility)))
                && Session.Current.Scenario.IsPlayer(this)) && (this.OnAfterCatchLeader != null))
            {
                this.OnAfterCatchLeader(leader, this);
            }
        }

        public void ClearRouteways()
        {
            while (this.RoutewayCount > 0)
            {
                Session.Current.Scenario.RemoveRouteway(this.Routeways[0] as Routeway);
            }
        }

        private void ClearSections()
        {
            System.Diagnostics.Debug.WriteLine($"[TRACKING] ClearSections called! Stack: {new System.Diagnostics.StackTrace()}");

            // 🔥 优化：先将所有建筑的军区关系设为null，避免RemoveArchitecture中的重新分配逻辑
            foreach (Architecture architecture in this.Architectures)
            {
                architecture.BelongedSection = null;
            }

            // 然后清理军区
            foreach (Section section in this.Sections.GetList())
            {
                // 清空军区的建筑列表，避免触发RemoveArchitecture
                section.Architectures.Clear();
                this.RemoveSection(section);
                Session.Current.Scenario.Sections.Remove(section);
            }

            System.Diagnostics.Debug.WriteLine($"[ClearSections] 势力{this.Name}清理了所有军区，建筑将重新分配");
        }

        public Section CreateFirstSection()
        {
            // 🔥 关键修改：只有玩家势力才创建军区，AI势力不创建
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                System.Diagnostics.Debug.WriteLine($"[CreateFirstSection] AI势力{this.Name}不创建军区，使用势力级管理");
                return null;
            }

            if ((this.Capital != null) && (this.ArchitectureCount > 0))
            {
                Section section = new Section();
                section.ID = Session.Current.Scenario.Sections.GetFreeGameObjectID();
                section.Name = this.Capital.Name + "军区";
                section.BelongedFaction = this; // 🔥 修复：设置所属势力
                section.BelongedFactionID = this.ID; // 🔥 修复：同步 ID

                // 🔥 新增：设置军区建立回合
                if (Session.Current?.Scenario != null)
                {
                    section.SetCreationTurn(Session.Current.Scenario.DaySince / 30); // 转换为回合数
                }

                // 玩家势力的第一个军区默认不自动运行（Manual Control）
                // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
                var aiDetails = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                    .GetSectionAIDetailsByConditions(SectionOrientationKind.无, false, false, true, true, false);
                
                if (aiDetails == null || aiDetails.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"配置数据损坏：无法找到玩家势力的 SectionAIDetail 配置（SectionOrientationKind.无, Manual）。" +
                        "游戏无法继续，请检查配置文件。");
                }
                
                section.AIDetail = aiDetails[0] as SectionAIDetail;
                
                if (section.AIDetail == null)
                {
                    throw new InvalidOperationException(
                        "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
                }

                foreach (Architecture architecture in this.Architectures)
                {
                    section.AddArchitecture(architecture);
                }
                this.AddSection(section);
                Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                System.Diagnostics.Debug.WriteLine($"[CreateFirstSection] 为玩家势力{this.Name}创建默认军区");
                return section;
            }

            // 即使没有首都也要为玩家创建默认军区
            if (this.ArchitectureCount > 0)
            {
                Section section = new Section();
                section.ID = Session.Current.Scenario.Sections.GetFreeGameObjectID();
                section.Name = this.Name + "军区";  // 使用势力名称而不是首都名称
                section.BelongedFaction = this; // 🔥 修复：设置所属势力
                section.BelongedFactionID = this.ID; // 🔥 修复：同步 ID

                // 🔥 新增：设置军区建立回合
                if (Session.Current?.Scenario != null)
                {
                    section.SetCreationTurn(Session.Current.Scenario.DaySince / 30); // 转换为回合数
                }

                // 玩家势力的军区默认不自动运行
                // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
                var aiDetails = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                    .GetSectionAIDetailsByConditions(SectionOrientationKind.无, false, false, true, true, false);
                
                if (aiDetails == null || aiDetails.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"配置数据损坏：无法找到玩家势力的 SectionAIDetail 配置（SectionOrientationKind.无, Manual）。" +
                        "游戏无法继续，请检查配置文件。");
                }
                
                section.AIDetail = aiDetails[0] as SectionAIDetail;
                
                if (section.AIDetail == null)
                {
                    throw new InvalidOperationException(
                        "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
                }

                foreach (Architecture architecture in this.Architectures)
                {
                    section.AddArchitecture(architecture);
                }
                this.AddSection(section);
                Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                System.Diagnostics.Debug.WriteLine($"[CreateFirstSection] 为玩家势力{this.Name}创建默认军区（无首都）");
                return section;
            }

            return null;
        }

        /// <summary>
        /// 重置说服缓存（在回合开始或数据变动时调用）
        /// </summary>
        public void ResetConvinceCache()
        {
            _cachedGlobalConvinceTargetAvailable = null;
        }

        /// <summary>
        /// 【新建势力检查】：判断是否为新建立的势力（3回合内）
        /// </summary>
        private bool IsNewlyCreatedFaction()
        {
            if (FactionCreationTurn == -1 || Session.Current?.Scenario == null)
            {
                return false;
            }

            // 计算势力建立至今的回合数
            int currentTurn = Session.Current.Scenario.DaySince / 30; // 假设每回合30天
            int turnsSinceCreation = currentTurn - FactionCreationTurn;

#if DEBUG
            if (turnsSinceCreation <= NEW_FACTION_PRIORITY_TURNS)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.AI] {this.Name} 新建势力检查: 建立{turnsSinceCreation}回合，仍在优先期内");
            }
#endif

            return turnsSinceCreation <= NEW_FACTION_PRIORITY_TURNS;
        }

        /// <summary>
        /// 【外部接口】：手动设置势力建立回合（在创建势力时调用）
        /// </summary>
        public void SetFactionCreationTurn(int creationTurn)
        {
            FactionCreationTurn = creationTurn;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Faction.AI] {this.Name} 设置建立回合: 第{FactionCreationTurn}回合");
#endif
        }

        /// <summary>
        /// 获取是否存在可说服目标（带缓存机制）
        /// </summary>
        public bool HasGlobalConvinceTarget()
        {
            if (_cachedGlobalConvinceTargetAvailable.HasValue)
            {
                return _cachedGlobalConvinceTargetAvailable.Value;
            }

            bool hasTarget = false;
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture.BelongedFaction != this && !this.IsArchitectureKnown(architecture))
                {
                    continue;
                }

                if (architecture.BelongedFaction == this)
                {
                    if (architecture.HasCaptive() || architecture.HasNoFactionPerson())
                    {
                        hasTarget = true;
                        break;
                    }
                }
                else
                {
                    if (architecture.HasPerson() || architecture.HasNoFactionPerson())
                    {
                        hasTarget = true;
                        break;
                    }
                }
            }

            _cachedGlobalConvinceTargetAvailable = hasTarget;
            return hasTarget;
        }

        /// <summary>
        /// 供外部调用的方法：当发生武将登庸、死亡、移动、俘虏时调用此方法
        /// 触发人事调配的脏标记，确保下次执行时会重新分配人员
        /// </summary>
        public void NotifyPersonnelChange()
        {
            this.HasPersonnelChanges = true;

            if (GameObjects.AIConfiguration.EnablePersonnelDebug)
            {
                System.Diagnostics.Debug.WriteLine("[NotifyPersonnelChange] 势力 " + this.Name + " 标记人事变动");
            }
        }

        /// <summary>
        /// 辅助方法：判断日期是否符合配置的频率
        /// </summary>
        private bool IsPersonnelAllocationDay(GameDate date)
        {
            // 只有每月1号才有可能执行
            if (date.Day != 1) return false;

            switch (GameObjects.AIConfiguration.AllocationInterval)
            {
                case GameObjects.PersonnelInterval.Monthly:
                    return true;
                case GameObjects.PersonnelInterval.Quarterly:
                    return (date.Month - 1) % 3 == 0; // 1, 4, 7, 10
                case GameObjects.PersonnelInterval.HalfYearly:
                    return (date.Month - 1) % 6 == 0; // 1, 7
                case GameObjects.PersonnelInterval.Yearly:
                    return date.Month == 1;
                default:
                    return true;
            }
        }

        public void DayEvent()
        {
            // this.SpyMessageCloseList.Clear();
            this.TechniquesDayEvent();
            this.InformationDayEvent();
            this.MilitaryDayEvent();
            this.ResetConvinceCache();
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                // this.AISelectPrince();
                this.AIchaotingshijian();
                this.AIBecomeEmperor();
            }
            this.armyScale = this.ArmyScale; // 小写的是每天的缓存，因为被InternalSurplusRate叫很多次，不想每次都全部重新计算，大写的才是真正的值
            this.InternalSurplusRateCache = -1;
            this.visibleTroopsCache = null;
            this.RefreshImportantPerson();
            this.troopSequence = -1;
        }

        public void RefreshImportantPerson()
        {
            //阿柒:得到本势力除君主外的所有人物的名单
            List<Person> PersonInCurrentFaction = new List<Person>();
            foreach (Person person in this.Persons)
            {
                if (person != this.Leader)
                {
                    PersonInCurrentFaction.Add(person);
                }
            }

            //阿柒:排序得到智力最高的命名为军师
            if (PersonInCurrentFaction.Count >= 1)
            {
                List<Person> t = PersonInCurrentFaction.OrderByDescending(Person => Person.IntelligenceIncludingExperience).ToList();
                if (t[0].IntelligenceIncludingExperience >= 70)
                {
                    Counsellor = string.Concat(new object[] { t[0].Name, "(", t[0].IntelligenceIncludingExperience.ToString(), ")" });
                }
                else
                {
                    Counsellor = string.Concat(new object[] { "----" });
                }
            }
            else
            {
                Counsellor = string.Concat(new object[] { "----" });
            }

            //阿柒:排序得到统帅最高的命名为都督
            if (PersonInCurrentFaction.Count >= 1)
            {
                List<Person> t = PersonInCurrentFaction.OrderByDescending(Person => Person.CommandIncludingExperience).ToList();
                if (t[0].CommandIncludingExperience >= 70)
                {
                    Governor = string.Concat(new object[] { t[0].Name, "(", t[0].CommandIncludingExperience.ToString(), ")" });
                }
                else
                {
                    Governor = string.Concat(new object[] { "----" });
                }
            }
            else
            {
                Governor = string.Concat(new object[] { "----" });
            }

            //阿柒:排序得到武勇最高的命名为五虎将
            string[] FivetigerString = new string[5] { "----", "----", "----", "----", "----" };

            if (PersonInCurrentFaction.Count >= 1)
            {
                List<Person> Fivetiger = PersonInCurrentFaction.OrderByDescending(Person => Person.StrengthIncludingExperience).ToList();
                // 🔥 FIX: 防止索引越界，循环次数不能超过实际人数
                int maxCount = Math.Min(5, Fivetiger.Count);
                for (int i = 0; i < maxCount; i++)
                {
                    if (Fivetiger[i].StrengthIncludingExperience >= 70)
                    {
                        FivetigerString[i] = Fivetiger[i].Name + "(" + Fivetiger[i].StrengthIncludingExperience.ToString() + ")";
                    }
                }
            }
            FiveTigers = string.Concat(new object[] { FivetigerString[0], " • ", FivetigerString[1], " • ", FivetigerString[2], " • ", FivetigerString[3], " • ", FivetigerString[4] });
        }

        [DataMember]
        [JsonInclude]
        public string TransferingMilitariesString { get; set; }

        public MilitaryList TransferingMilitaries { get; set; } = new MilitaryList();

        public List<string> LoadTransferingMilitariesFromString(MilitaryList militaries, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后TransferingMilitaries为null
            if (this.TransferingMilitaries == null)
            {
                this.TransferingMilitaries = new MilitaryList();
            }
            this.TransferingMilitaries.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Military gameObject = militaries.GetGameObject(int.Parse(str)) as Military;
                    if (gameObject != null)
                    {
                        this.TransferingMilitaries.AddMilitary(gameObject);
                        this.AddMilitary(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("编队ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("编队列表一栏应为半型空格分隔的编队ID");
            }
            return errorMsg;
        }

        public List<string> LoadMilitariesFromString(MilitaryList militaries, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.Militaries.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Military gameObject = militaries.GetGameObject(int.Parse(str)) as Military;
                    if (gameObject != null)
                    {
                        this.Militaries.AddMilitary(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("编队ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("编队列表一栏应为半型空格分隔的编队ID");
            }
            return errorMsg;
        }

        private void HandleMilitary(Military military)
        {
            if (military.ArrivingDays != 0)
            {
                military.ArrivingDays = 0;
            }
            military.StartingArchitecture = null;
            military.TargetArchitecture = null;
            this.TransferingMilitaries.Remove(military);
            this.TransferingMilitaryCount--;
        }

        private void MilitaryDayEvent()
        {
            //if (this.TransferingMilitaryCount == 0) return;
            foreach (Military m in this.TransferingMilitaries.GetList())
            {
                m.ArrivingDays--;

                if (m.ArrivingDays <= 0)
                {
                    if (m.StartingArchitecture != null && m.TargetArchitecture != null && m.TargetArchitecture.BelongedFaction != null
                        && m.TargetArchitecture.BelongedFaction == this && m.BelongedArchitecture == null)
                    {
                        m.TargetArchitecture.AddMilitary(m);
                        Session.MainGame.mainGameScreen.TransferMilitaryArrivesAtArchitecture(m, m.TargetArchitecture);
                    }

                    this.HandleMilitary(m);
                }
                else
                {
                    if (m.StartingArchitecture != null && m.TargetArchitecture != null && m.TargetArchitecture.BelongedFaction != null && m.TargetArchitecture.BelongedFaction != this) //运兵过程中目标建筑被占领，停止运输，编队返回出发建筑
                    {

                        if (m.StartingArchitecture.BelongedFaction != null && m.BelongedArchitecture == null)
                        {

                            m.StartingArchitecture.AddMilitary(m);
                        }

                        this.HandleMilitary(m);

                    }
                    else if (m.StartingArchitecture != null && m.TargetArchitecture != null && m.StartingArchitecture.BelongedFaction != null
                        && m.TargetArchitecture.BelongedFaction != null && m.TargetArchitecture.IsSurrounded())   //运兵过程中目标建筑被围城，停止运兵,编队返回出发建筑
                    {
                        if (m.BelongedArchitecture == null)
                        {
                            m.StartingArchitecture.AddMilitary(m);
                        }

                        this.HandleMilitary(m);
                    }


                    else if (m.StartingArchitecture != null && m.StartingArchitecture.BelongedFaction == null) //势力灭亡，停止运兵，编队暂时消失
                    {
                        this.HandleMilitary(m);

                    }
                }

            }

        }

        private void AISelectPrince()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                if (GameObject.Random(10) == 0 && (this.Capital != null) && this.Capital.BelongedFaction == this && this.Capital.SelectPrinceAvail())
                {
                    // 🔥 FIX: 检查是否有可选的继承人
                    var children = this.Leader.ChildrenCanBeSelectedAsPrince();
                    if (children != null && children.Count > 0)
                    {
                        Person person = children[0] as Person;
                        if (person != null && person.ID != this.PrinceID)
                        {
                            this.PrinceID = person.ID;
                            this.Capital.DecreaseFund(Session.Parameters.SelectPrinceCost);
                            this.Capital.SelectPrince(person); //AI立储年表和报告
                            //Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, person.Name, "SelectPrince", "", "", true);
                        }
                    }
                }
            }
        }

        private void AINvGuan()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                if (Session.Current.Scenario.DaySince <= 1)
                {
                    foreach (Person p in Persons)
                    {
                        ConsiderPromoteNvGuan(p);
                    }
                }
            }
        }

        public void ConsiderPromoteNvGuan(Person p)
        {
            if (p.NvGuanPromotable && (p.Command * 2 + p.Strength) / (p.Politics + p.Glamour * 2) > 0.95f)
            {
                p.PromoteFromNvGuan();
            }
        }

        public void AutoAppointMayor(IEnumerable<Architecture> targetList = null)
        {
            // 🔍 调试输出：记录方法调用
            // System.Diagnostics.Debug.WriteLine($"");
            // System.Diagnostics.Debug.WriteLine($"🏛️ ╔═══════════════════════════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"🏛️ ║ [AutoAppointMayor 调用] 势力: {this.Name}");
            // System.Diagnostics.Debug.WriteLine($"🏛️ ║ 调用类型: {(targetList != null ? "军区托管" : "势力级")}");
            // if (targetList != null)
            // {
            //     var targetCities = targetList.ToList();
            //     System.Diagnostics.Debug.WriteLine($"🏛️ ║ 目标城市数: {targetCities.Count}");
            //     System.Diagnostics.Debug.WriteLine($"🏛️ ║ 城市列表: {string.Join(", ", targetCities.Select(a => a.Name))}");
            // }
            // else
            // {
            //     System.Diagnostics.Debug.WriteLine($"🏛️ ║ 目标城市数: 全势力 ({this.Architectures.Count}个)");
            // }
            // System.Diagnostics.Debug.WriteLine($"🏛️ ╚═══════════════════════════════════════════════════════════");

            IEnumerable<Architecture> source = targetList ?? this.Architectures.Cast<Architecture>();
            List<Architecture> targets = new List<Architecture>();
            foreach (var a in source) { targets.Add(a); }

            foreach (Architecture a in targets)
            {
                // 🔥 只对有人员的城市输出调试信息，减少刷屏
                if (a.PersonCount == 0)
                {
                    continue; // 跳过无人城市
                }

                // 🔍 添加详细的调试输出
                // System.Diagnostics.Debug.WriteLine($"");
                // System.Diagnostics.Debug.WriteLine($"🏛️ ═══════════════════════════════════════════════════════════");
                // System.Diagnostics.Debug.WriteLine($"🏛️ [县令任命检查] 城市: {a.Name}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ 当前县令: {(a.Mayor != null ? a.Mayor.Name : "无")}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ 人员数量: {a.PersonCount}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ 势力类型: {(Session.Current.Scenario.IsPlayer(this) ? "玩家" : "AI")}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ 调用类型: {(targetList != null ? "军区托管" : "势力级")}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ 托管状态: {(a.BelongedSection != null && a.BelongedSection.AIDetail != null ? a.BelongedSection.AIDetail.AutoRun.ToString() : "无军区")}");
                // System.Diagnostics.Debug.WriteLine($"🏛️ AppointMayorAvail: {a.AppointMayorAvail()}");
                // if (a.AIMayorCandicate != null)
                // {
                //    System.Diagnostics.Debug.WriteLine($"🏛️ AI候选人数量: {a.AIMayorCandicate.Count}");
                // }
                // System.Diagnostics.Debug.WriteLine($"🏛️ ═══════════════════════════════════════════════════════════");

                // [CRITICAL] 跳过逻辑：只有玩家势力的托管军区才跳过
                // AI势力没有军区AI的概念，应该直接处理所有城市
                if (targetList == null &&
                    Session.Current.Scenario.IsPlayer(this) &&  // 🔥 关键修复：只对玩家势力跳过
                    a.BelongedSection != null &&
                    a.BelongedSection.AIDetail != null &&
                    a.BelongedSection.AIDetail.AutoRun)
                {

                    continue;
                }

                // 🔥 判断是否应该自动任命县令
                bool shouldAutoAppoint = false;

                if (targetList != null)
                {
                    // 军区托管调用：应该自动任命
                    shouldAutoAppoint = true;
                }
                else if (!Session.Current.Scenario.IsPlayer(this))
                {
                    // AI势力：应该自动任命
                    shouldAutoAppoint = true;
                }
                else
                {
                    // 玩家势力且非托管调用：不自动任命，由玩家手动控制
                    shouldAutoAppoint = false;

#if DEBUG
                    // 🔥 FIX #2: 添加调试输出，帮助诊断县令任命问题
                    if (SectionAIHelper.EnableDebugOutput && a.Mayor == null && a.PersonCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ℹ️ 玩家势力不自动任命: {a.Name} " +
                            $"(人数={a.PersonCount}, 需手动任命)");
                    }
#endif
                }

                if (!shouldAutoAppoint)
                {
                    continue; // 跳过自动任命
                }

                if (a.AppointMayorAvail())
                {
                    // 如果是军区托管 (targetList != null)，需要限制候选人范围
                    if (targetList != null)
                    {
                        if (a.Mayor != null) continue; // 已有太守

                        // 🔥 修改：军区托管也使用 AI 的高级筛选逻辑 (AIMayorCandicate)
                        if (a.AIMayorCandicate.Count > 0)
                        {
                            Person person = a.AIMayorCandicate[0] as Person;
                            if (person != null)
                            {
                                a.MayorID = person.ID;
                                a.AppointMayor(person);
                                a.MayorOnDutyDays = 0;

                                if (SectionAIHelper.EnableDebugOutput)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ✅ 军区托管：{a.Name} 使用高级AI逻辑任命 {person.Name} 为县令");
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            if (SectionAIHelper.EnableDebugOutput)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ⚠️ 军区托管：{a.Name} 无合适候选人 (AIMayorCandicate为空)");
                            }
#endif
                        }
                    }
                    else // 原势力AI逻辑
                    {
                        // 优先使用高级AI的候选人列表 (AIMayorCandicate)
                        // Architecture.AppointMayorAvail 已经检查了 AIMayorCandicate.Count > 0

                        if (a.AIMayorCandicate.Count > 0)
                        {
                            Person person = a.AIMayorCandicate[0] as Person;
                            if (person != null)
                            {
                                a.MayorID = person.ID;
                                a.AppointMayor(person);
                                a.MayorOnDutyDays = 0;

                                if (SectionAIHelper.EnableDebugOutput)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ✅ AI势力：{a.Name} 任命 {person.Name} 为县令");
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            if (SectionAIHelper.EnableDebugOutput && a.Mayor == null && a.PersonCount > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ⚠️ AI势力：{a.Name} 无候选人 " +
                                    $"(人数={a.PersonCount}, 候选人列表为空)");
                            }
#endif
                        }
                    }
                }
                else
                {
#if DEBUG
                    // 🔥 FIX #2: 添加调试输出，显示为什么不能任命
                    if (SectionAIHelper.EnableDebugOutput && a.Mayor == null && a.PersonCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] ⚠️ 不满足任命条件: {a.Name} " +
                            $"(AppointMayorAvail=false)");
                    }
#endif
                }

                if (!shouldAutoAppoint)
                {
                    continue; // 跳过自动任命
                }

                if (a.AppointMayorAvail())
                {
                    // 如果是军区托管 (targetList != null)，需要限制候选人范围
                    if (targetList != null)
                    {
                        if (a.Mayor != null) continue; // 已有太守

                        Person bestCandidate = null;
                        float maxScore = -1;

                        foreach (Person p in a.Persons)
                        {
                            if (p.Status == PersonStatus.Normal)
                            {
                                // 简单的评分逻辑
                                float score = p.Politics * 1.5f + p.Command + p.Intelligence;
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                    bestCandidate = p;
                                }
                            }
                        }

                        if (bestCandidate != null)
                        {
                            a.MayorID = bestCandidate.ID;
                            a.AppointMayor(bestCandidate);
                            a.MayorOnDutyDays = 0;

                            if (SectionAIHelper.EnableDebugOutput)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] 军区托管：{a.Name} 任命 {bestCandidate.Name} 为县令");
                            }
                        }
                    }
                    else // 原势力AI逻辑
                    {
                        if (a.AIMayorCandicate.Count > 0)
                        {
                            Person person = a.AIMayorCandicate[0] as Person;
                            if (person != null)
                            {
                                a.MayorID = person.ID;
                                a.AppointMayor(person);
                                a.MayorOnDutyDays = 0;

                                if (SectionAIHelper.EnableDebugOutput)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[AutoAppointMayor] AI势力：{a.Name} 任命 {person.Name} 为县令");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// AI根据君主性格选择军师 - 改进版本
        /// </summary>
        private void AIAppointAdvisor()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                // ★★★ 修复：如果现任军师只是因为忙碌(Available=false)导致Advisor属性为null，
                // 此时不应该任命新人，而是保持现状 ★★★
                if (this.Advisor == null && this.AdvisorID != -1)
                {
                    // 🔥 AOT修复：使用更安全的类型转换方式
                    var gameObject = Session.Current.Scenario.Persons.GetGameObject(this.AdvisorID);
                    Person historicAdvisor = null;
                    if (gameObject is Person person)
                    {
                        historicAdvisor = person;
                    }
                    
                    if (historicAdvisor != null && historicAdvisor.Alive && historicAdvisor.BelongedFaction == this && !historicAdvisor.Available)
                    {
                        // 军师只是忙碌（出征/任务中），跳过任命检查
                        return;
                    }
                }

                if (this.AppointAdvisorAvail())
                {
                    PersonList candidates = this.AIAdvisorCandicate;
                    if (candidates.Count > 0)
                    {
                        // 【核心改进】根据君主性格选择军师，而不是总选智力最高的
                        Person selectedCandidate = SelectAdvisorByPersonality(candidates);

                        // 决定是否需要更换军师
                        if (ShouldAppointNewAdvisor(selectedCandidate))
                        {
                            System.Diagnostics.Debug.WriteLine($"[AI任命军师] {this.LeaderName} 任命 {selectedCandidate.Name} 为军师");

                            this.AdvisorID = selectedCandidate.ID;
                            this.AppointAdvisor(selectedCandidate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据君主性格选择军师
        /// </summary>
        private Person SelectAdvisorByPersonality(PersonList candidates)
        {
            if (candidates.Count == 0) return null;
            if (this.Leader == null) return candidates[0] as Person;

            int personalityId = this.Leader.Character?.ID ?? 0;

            // System.Diagnostics.Debug.WriteLine($"[AI选择军师] {this.Leader.Name} 的性格ID: {personalityId}");

            switch (personalityId)
            {
                case 0: // 仁德型 - 重视品德和忠诚
                    return SelectByVirtue(candidates);

                case 1: // 霸道型 - 重视能力，但也看重忠诚
                    return SelectByAbilityAndLoyalty(candidates);

                case 2: // 冷静型 - 理性选择，重视智力
                    return SelectByIntelligence(candidates);

                case 3: // 莽撞型/昏庸型 - 可能做出错误选择
                    return SelectByImpulse(candidates);

                case 4: // 狡诈型 - 重视智谋，但可能任人唯亲
                    return SelectByCunning(candidates);

                default:
                    // 默认选择智力最高的
                    return SelectByIntelligence(candidates);
            }
        }

        /// <summary>
        /// 仁德型选择：重视品德和忠诚
        /// </summary>
        private Person SelectByVirtue(PersonList candidates)
        {
            System.Diagnostics.Debug.WriteLine("[仁德型选择] 重视品德和忠诚");

            // 优先选择忠诚度高且智力不错的
            Person virtuous = null;
            int bestScore = -1;

            foreach (Person candidate in candidates)
            {
                if (candidate.Loyalty >= 80 && candidate.Intelligence >= 70)
                {
                    int score = candidate.Loyalty * 2 + candidate.Intelligence; // 忠诚权重更高
                    if (score > bestScore)
                    {
                        bestScore = score;
                        virtuous = candidate;
                    }
                }
            }

            if (virtuous != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[仁德型选择] 选择高忠诚候选人: {virtuous.Name} (忠诚{virtuous.Loyalty} 智力{virtuous.Intelligence})");
                return virtuous;
            }

            // 如果没有高忠诚的，选择智力最高的
            return SelectByIntelligence(candidates);
        }

        /// <summary>
        /// 霸道型选择：重视能力，但也看重忠诚
        /// </summary>
        private Person SelectByAbilityAndLoyalty(PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[霸道型选择] 重视能力和忠诚的平衡");

            Person best = null;
            double bestScore = -1;

            foreach (Person candidate in candidates)
            {
                // 综合评分：智力 * 0.7 + 忠诚度 * 0.3
                double score = candidate.Intelligence * 0.7 + candidate.Loyalty * 0.3;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[霸道型选择] 选择综合最佳: {best.Name} (智力{best.Intelligence} 忠诚{best.Loyalty} 综合分{bestScore:F1})");
            }

            return best;
        }

        /// <summary>
        /// 冷静型选择：理性选择，重视智力
        /// </summary>
        private Person SelectByIntelligence(PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[冷静型选择] 理性选择智力最高者");

            Person smartest = null;
            int highestIntelligence = -1;

            foreach (Person candidate in candidates)
            {
                if (candidate.Intelligence > highestIntelligence)
                {
                    highestIntelligence = candidate.Intelligence;
                    smartest = candidate;
                }
            }

            if (smartest != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[冷静型选择] 选择最高智力: {smartest.Name} (智力{smartest.Intelligence})");
            }

            return smartest;
        }

        /// <summary>
        /// 莽撞型/昏庸型选择：可能做出错误选择
        /// </summary>
        private Person SelectByImpulse(PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[莽撞型选择] 可能做出冲动或错误的选择");

            // 50% 概率做出错误选择
            if (GameObject.Random(100) < 50 && candidates.Count > 1)
            {
                // 可能选择魅力高但智力不是最高的
                Person charmingButNotSmartest = null;
                int highestCharm = -1;
                Person mostIntelligent = SelectByIntelligence(candidates);

                foreach (Person candidate in candidates)
                {
                    if (candidate != mostIntelligent && candidate.Intelligence > highestCharm)
                    {
                        highestCharm = candidate.Intelligence;
                        charmingButNotSmartest = candidate;
                    }
                }

                if (charmingButNotSmartest != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 冲动选择能力高者: {charmingButNotSmartest.Name} (能力{charmingButNotSmartest.Intelligence} 智力{charmingButNotSmartest.Intelligence})");
                    return charmingButNotSmartest;
                }

                // 或者随机选择前几名中的一个
                int randomIndex = GameObject.Random(Math.Min(3, candidates.Count));
                if (randomIndex < candidates.Count)
                {
                    Person randomChoice = candidates[randomIndex] as Person;
                    // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 随机选择: {randomChoice.Name} (智力{randomChoice.Intelligence})");
                    return randomChoice;
                }
            }

            // 50% 概率还是选择智力最高的
            Person smartest = SelectByIntelligence(candidates);
            if (smartest != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 偶然选对: {smartest.Name} (智力{smartest.Intelligence})");
            }
            return smartest;
        }

        /// <summary>
        /// 狡诈型选择：重视智谋，但可能任人唯亲
        /// </summary>
        private Person SelectByCunning(PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[狡诈型选择] 重视智谋，但可能任人唯亲");

            // 30% 概率任人唯亲（选择关系好的）
            if (GameObject.Random(100) < 30)
            {
                // 寻找有特殊关系的候选人
                foreach (Person candidate in candidates)
                {
                    if (HasSpecialRelationWithLeader(candidate) && candidate.Intelligence >= 60)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[狡诈型选择] 任人唯亲: {candidate.Name} (智力{candidate.Intelligence})");
                        return candidate;
                    }
                }
            }

            // 70% 概率选择智力高的
            Person smartest = null;
            int highestIntelligence = -1;

            foreach (Person candidate in candidates)
            {
                if (candidate.Intelligence >= 75 && candidate.Intelligence > highestIntelligence)
                {
                    highestIntelligence = candidate.Intelligence;
                    smartest = candidate;
                }
            }

            if (smartest != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[狡诈型选择] 选择高智力: {smartest.Name} (智力{smartest.Intelligence})");
                return smartest;
            }

            // 如果没有高智力的，选择最好的
            return SelectByIntelligence(candidates);
        }

        /// <summary>
        /// 检查是否与君主有特殊关系
        /// </summary>
        private bool HasSpecialRelationWithLeader(Person candidate)
        {
            if (this.Leader == null || candidate == null) return false;

            // 检查各种特殊关系
            if (this.Leader.Father == candidate || candidate.Father == this.Leader) return true; // 父子
            if (this.Leader.Spouse == candidate || candidate.Spouse == this.Leader) return true; // 配偶
            if (this.Leader.Brothers != null && this.Leader.Brothers.HasGameObject(candidate)) return true; // 兄弟

            // 检查亲密关系
            if (this.Leader.CheckRelation(candidate) == 1) return true; // 亲密关系

            return false;
        }

        /// <summary>
        /// 判断是否应该任命新军师
        /// </summary>
        private bool ShouldAppointNewAdvisor(Person candidate)
        {
            if (candidate == null) return false;

            // 如果没有军师，直接任命
            if (this.Advisor == null)
            {
                // System.Diagnostics.Debug.WriteLine("[任命判断] 无现任军师，直接任命");
                return true;
            }

            Person currentAdvisor = this.Advisor;

            // 🔥 FIX: 如果候选人就是当前军师，不需要重新任命
            if (candidate == currentAdvisor || candidate.ID == currentAdvisor.ID)
            {
                return false;
            }

            // 根据君主性格决定更换标准
            int personalityId = this.Leader?.Character?.ID ?? 0;

            switch (personalityId)
            {
                case 0: // 仁德型 - 不轻易更换，除非新人明显更好
                    bool shouldReplaceVirtuous = candidate.Intelligence > currentAdvisor.Intelligence + 15 ||
                                               (candidate.Loyalty > currentAdvisor.Loyalty + 20 && candidate.Intelligence >= currentAdvisor.Intelligence - 5);
                    // System.Diagnostics.Debug.WriteLine($"[仁德型判断] 是否更换: {shouldReplaceVirtuous}");
                    return shouldReplaceVirtuous;

                case 1: // 霸道型 - 追求更强的能力
                    bool shouldReplaceAmbitious = candidate.Intelligence > currentAdvisor.Intelligence + 10;
                    // System.Diagnostics.Debug.WriteLine($"[霸道型判断] 是否更换: {shouldReplaceAmbitious}");
                    return shouldReplaceAmbitious;

                case 2: // 冷静型 - 理性比较
                    bool shouldReplaceRational = candidate.Intelligence > currentAdvisor.Intelligence + 8;
                    // System.Diagnostics.Debug.WriteLine($"[冷静型判断] 是否更换: {shouldReplaceRational}");
                    return shouldReplaceRational;

                case 3: // 莽撞型 - 可能冲动更换
                    // 30% 概率冲动更换（即使新人不一定更好）
                    if (GameObject.Random(100) < 30)
                    {
                        // System.Diagnostics.Debug.WriteLine("[莽撞型判断] 冲动更换军师");
                        return true;
                    }
                    // 否则需要明显更好才换
                    bool shouldReplaceImpulsive = candidate.Intelligence > currentAdvisor.Intelligence + 20;
                    // System.Diagnostics.Debug.WriteLine($"[莽撞型判断] 理性判断是否更换: {shouldReplaceImpulsive}");
                    return shouldReplaceImpulsive;

                case 4: // 狡诈型 - 可能因为关系更换
                    // 如果新候选人有特殊关系，可能更换
                    if (HasSpecialRelationWithLeader(candidate) && candidate.Intelligence >= currentAdvisor.Intelligence - 10)
                    {
                        // System.Diagnostics.Debug.WriteLine("[狡诈型判断] 因关系更换军师");
                        return true;
                    }
                    // 否则需要智力明显更高
                    bool shouldReplaceCunning = candidate.Intelligence > currentAdvisor.Intelligence + 12;
                    // System.Diagnostics.Debug.WriteLine($"[狡诈型判断] 能力判断是否更换: {shouldReplaceCunning}");
                    return shouldReplaceCunning;

                default:
                    return candidate.Intelligence > currentAdvisor.Intelligence + 10;
            }
        }

        [DataMember]
        [JsonInclude]
        public string GetGeneratorPersonCountString { get; set; }

        private Dictionary<PersonGeneratorType, int> count = new Dictionary<PersonGeneratorType, int>();

        public void IncrementGeneratorCount(PersonGeneratorType type)
        {
            if (count.ContainsKey(type))
            {
                count[type]++;
            }
            else
            {
                count.Add(type, 1);
            }
        }

        public int GetGeneratorPersonCount(PersonGeneratorType type)
        {
            return count.ContainsKey(type) ? count[type] : 0;
        }



        // private List<PersonGeneratorType> allTypes = new List<PersonGeneratorType>();
        //  private Dictionary<PersonGeneratorType, int> types = new Dictionary<PersonGeneratorType, int>();

        public string SaveGeneratorPersonCountToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (GameObject obj in Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes.GameObjects)
            {
                if (obj is PersonGeneratorType type)
                {
                    sb.AppendFormat("{0}:{1},", type.ID, count.ContainsKey(type) ? count[type] : 0);
                }
            }
            return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "";
        }

        public List<string> LoadGeneratorPersonCountFromString(String s)
        {
            List<string> errorMsg = new List<string>();

            if (String.IsNullOrEmpty(s))
            {
                return errorMsg;
            }

            count.Clear();
            string[] sArray = s.Split(',');
            foreach (string ss in sArray)
            {
                string[] arr = ss.Split(':');
                int typeID = int.Parse(arr[0]);
                int typeCount = int.Parse(arr[1]);
                PersonGeneratorType type = FindPersonGeneratorType(typeID);
                if (type == null || typeCount < 0)
                {
                    errorMsg.Add("Invaild Person Generator Count, typeID=" + typeID + " typeCount=" + typeCount);
                }
                else if (!count.ContainsKey(type))
                {
                    count.Add(type, typeCount);
                }
            }
            return errorMsg;
        }

        public PersonGeneratorType FindPersonGeneratorType(int id)
        {
            // 🔥 修复：防御性类型检查，避免反序列化数据污染导致的 InvalidCastException
            // 原因：AllPersonGeneratorTypes 在反序列化后可能混入了错误类型的对象
            // 注意：Session.Current.Scenario.GameCommonData 在游戏运行时必定存在，不需要空检查
            
            foreach (GameObject obj in Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes.GameObjects)
            {
                // 🔥 关键修复：先检查类型再转换
                if (obj is PersonGeneratorType type && type.ID == id)
                {
                    return type;
                }
                
                // 🔥 诊断：记录数据污染（仅在非目标类型时）
                if (obj is not PersonGeneratorType)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[FindPersonGeneratorType] ⚠️ 数据污染：AllPersonGeneratorTypes[{obj.ID}] 类型为 {obj.GetType().Name}");
                }
            }
            return null;
        }


        private void AIZhaoXian()
        {
            if (Session.GlobalVariables.ZhaoXianSuccessRate <= 0) return;

            if (Session.Current.Scenario.IsPlayer(this)) return;

            int feiziCount = this.feiziCount();
            foreach (Architecture a in this.Architectures)
            {
                while (a.CanZhaoXian() && !a.HasEnoughPeople && (PersonCount <= 1 || a.IsFundEnough))
                {
                    PersonGeneratorTypeList list = a.AvailGeneratorTypeList();
                    Dictionary<PersonGeneratorType, float> weights = new Dictionary<PersonGeneratorType, float>();

                    int eFund;
                    if (PersonCount <= 1)
                    {
                        eFund = 0;
                    }
                    else
                    {
                        eFund = Math.Min(a.EnoughFund * PersonCount / 2, a.AbundantFund);
                    }
                    foreach (PersonGeneratorType t in list)
                    {
                        if (t.CostFund + eFund < a.Fund)
                        {
                            weights[t] = t.CostFund * t.generationChance;
                        }
                    }

                    if (weights.Count > 0)
                    {
                        PersonGeneratorType type = GameObject.WeightedRandom(weights);
                        a.DoZhaoXian(type);
                    }
                    else
                    {
                        break;
                    }

                }
            }

        }


        private void AIchaotingshijian()
        {
            if (Session.Current.Scenario.youhuangdi() && !Session.Current.Scenario.IsPlayer(this) && !this.IsAlien
                && (this.guanjue < Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 1))
            {
                if (Session.Current.Scenario.Date.Month == 3)
                {
                    this.AIjingong();
                }
            }
        }

        public int FundToAdvance
        {
            get
            {
                int cashToGive = 0;
                foreach (guanjuezhongleilei g in Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhongleiliebiao())
                {
                    if (g.xuyaochengchi <= this.ArchitectureCount)
                    {
                        cashToGive = g.xuyaogongxiandu - this.chaotinggongxiandu;
                    }
                }
                return cashToGive;
            }
        }

        private void AIjingong()
        {
            if (this.IsAlien) return;

            int cashToGive = this.FundToAdvance;

            if (cashToGive > 0)
            {
                int givenValue = 0;
                Dictionary<Architecture, int> archGiveFund = new Dictionary<Architecture, int>();
                foreach (GameObject obj in this.Architectures.GetRandomList())
                {
                    Architecture a = (obj is Architecture ? (Architecture)obj : null);
                    if (a == null) continue;
                    
                    int canGiveFund = a.Fund - a.EnoughFund;
                    if (canGiveFund >= 1000)
                    {
                        if (canGiveFund + givenValue >= cashToGive)
                        {
                            canGiveFund = cashToGive - givenValue;
                        }
                        givenValue += canGiveFund;
                        archGiveFund[a] = canGiveFund;
                    }
                    if (givenValue >= cashToGive)
                    {
                        foreach (KeyValuePair<Architecture, int> i in archGiveFund)
                        {
                            i.Key.DecreaseFund(i.Value);
                            if (!this.Architectures.HasGameObject(Session.Current.Scenario.huangdisuozaijianzhu()))
                            {
                                Session.Current.Scenario.huangdisuozaijianzhu().IncreaseFund(i.Value);
                            }
                            this.chaotinggongxiandu += i.Value;
                            Session.MainGame.mainGameScreen.shilijingong(this, i.Value, "资金");
                        }
                        break;
                    }
                }
            }

            /*int jingongshue;
            int gongxianduzangzhang=0;
            bool jingongliangcao;
            float jingongxishu=0f;
            switch (this.Leader.ValuationOnGovernment)
            {
                case PersonValuationOnGovernment.无视  :
                    jingongxishu=0.1f;
                    break;
                case PersonValuationOnGovernment.普通 :
                    jingongxishu=0.2f;
                    break;
                case PersonValuationOnGovernment.重视 :
                    jingongxishu=0.3f;
                    break;
                    
            }
            jingongliangcao = (GameObject.Random(8) == 0);
            if (jingongliangcao)
            {
                if (this.Architectures.HasGameObject(Session.Current.Scenario.huangdisuozaijianzhu())) jingongxishu = 0.3f;

                jingongshue = (int)(this.Capital.Food  * jingongxishu);
                this.Capital.DecreaseFood(jingongshue);
                if (!this.Architectures.HasGameObject(Session.Current.Scenario.huangdisuozaijianzhu()))
                {
                    Session.Current.Scenario.huangdisuozaijianzhu().IncreaseFood(jingongshue);
                }
                gongxianduzangzhang = jingongshue / 200;
                this.chaotinggongxiandu += gongxianduzangzhang;

            }
            else
            {
                if (this.Architectures.HasGameObject(Session.Current.Scenario.huangdisuozaijianzhu())) jingongxishu = 0.5f;

                jingongshue = (int)(this.Capital.Fund * jingongxishu);
                this.Capital.DecreaseFund(jingongshue);
                if (!this.Architectures.HasGameObject(Session.Current.Scenario.huangdisuozaijianzhu()))
                {
                    Session.Current.Scenario.huangdisuozaijianzhu().IncreaseFund(jingongshue);

                }
                gongxianduzangzhang = jingongshue;
                this.chaotinggongxiandu += gongxianduzangzhang;

            }*/
            //Session.MainGame.mainGameScreen.shilijingong(this,jingongshue,jingongliangcao?"粮草":"资金");

        }

        public void DecreaseReputation(int decrement)
        {
            this.reputation -= decrement;
            if (this.reputation < 0)
            {
                this.reputation = 0;
            }
        }

        public void DecreaseTechniquePoint(int decrement)
        {
            this.techniquePoint -= decrement;
            if (this.techniquePoint < 0)
            {
                this.techniquePoint = 0;
            }
        }

        public void DepositTechniquePointForFacility(int deposit)
        {
            if (deposit > this.techniquePointForFacility)
            {
                deposit = this.techniquePointForFacility;
            }
            this.techniquePointForFacility -= deposit;
            this.techniquePoint += deposit;
        }

        public void DepositTechniquePointForTechnique(int deposit)
        {
            if (deposit > this.techniquePointForTechnique)
            {
                deposit = this.techniquePointForTechnique;
            }
            this.techniquePointForTechnique -= deposit;
            this.techniquePoint += deposit;
        }

        public void Destroy()
        {
            // 【新增】清理AI缓存中与该势力相关的数据
            try
            {
                if (WorldOfTheThreeKingdoms.GameManager.AICacheManager.Instance != null)
                {
                    WorldOfTheThreeKingdoms.GameManager.AICacheManager.Instance.ClearCacheForFaction(this.ID);
                    WorldOfTheThreeKingdoms.GameManager.AICacheManager.Instance.SetMapDirty();
                    System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] 已清理势力 {this.Name} (ID:{this.ID}) 的AI缓存");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] AI缓存清理失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] 异常堆栈: {ex.StackTrace}");
            }

            try
            {
                Session.Current.Scenario.YearTable.addFactionDestroyedEntry(Session.Current.Scenario.Date, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] YearTable操作失败: {ex.Message}");
            }

            try
            {
                if (this.Leader != null)
                {
                    this.Leader.Reputation /= 2;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] Leader声望更新失败: {ex.Message}");
            }

            try
            {
                if (this.OnFactionDestroy != null)
                {
                    this.OnFactionDestroy(this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] OnFactionDestroy事件失败: {ex.Message}");
            }

            try
            {
                foreach (Captive captive in this.SelfCaptives.GetList())
                {
                    //captive.TransformToNoFaction();
                    captive.TransformToNoFactionCaptive();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] 俘虏处理失败: {ex.Message}");
            }

            /*
            foreach (Troop troop in this.Troops.GetList())
            {
                troop.Destroy();
            }
            */

            try
            {
                foreach (Section section in this.Sections.GetList())
                {
                    this.RemoveSection(section);
                    Session.Current.Scenario.Sections.Remove(section);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] Section清理失败: {ex.Message}");
            }

            try
            {
                Session.Current.Scenario.DiplomaticRelations.RemoveDiplomaticRelationByFactionID(base.ID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] 外交关系清理失败: {ex.Message}");
            }

            try
            {
                Session.Current.Scenario.Factions.Remove(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] Factions.Remove失败: {ex.Message}");
            }

            try
            {
                Session.Current.Scenario.PlayerFactions.Remove(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] PlayerFactions.Remove失败: {ex.Message}");
            }

            this.Destroyed = true;

            try
            {
                // 🔥 AOT 重构：使用强类型事件替代反射调用
                WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseFactionDestroyed(Session.Current.Scenario, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Faction.Destroy] 事件触发失败: {ex.Message}");
            }
        }

        private void Develop()
        {
            this.DevelopArchitectures();
        }

        private void DevelopArchitectures()
        {
            foreach (Architecture architecture in this.Architectures)
            {
                architecture.DevelopDay();
            }
        }

        private void FactionDiplomaticRelation()
        {
            this.ResetFriendlyDiplomaticRelations();
        }

        public void EndControl()
        {
            this.ClearRouteways();
            this.Controlling = false;
            this.StopToControl = false;
            this.Passed = false;
            this.AIFinished = false;
            foreach (Troop t in this.Troops)
            {
                t.ManualControl = false;
            }
        }

        public Section GetAnotherSection(Section section)
        {
            foreach (Section section2 in this.Sections)
            {
                if (section2 != section)
                {
                    return section2;
                }
            }
            return null;
        }

        public InformationLevel GetArchitectureKnownLevel(Architecture a)
        {
            InformationLevel level = InformationLevel.无;
            foreach (Point point in a.ArchitectureArea.Area)
            {
                if (this.getInformationLevel(point) > level)
                {
                    level = this.getInformationLevel(point);
                }
            }
            return level;
        }

        public GameArea GetAvailableTroopDestination(Troop troop)
        {
            GameArea area = new GameArea();
            for (int i = 0; i < Session.Current.Scenario.MapTileData.GetLength(0); i++)
            {
                for (int j = 0; j < Session.Current.Scenario.MapTileData.GetLength(1); j++)
                {
                    Point point = new Point(i, j);
                    area.AddPoint(point);
                }
            }
            if (area.Count > 0)
            {
                return area;
            }
            return null;
        }

        private int GetAverageValueOfSecondTier(int x, int y)
        {
            return 0;
        }

        private int GetAverageValueOfThirdTier(int x, int y)
        {
            return 0;
        }

        public List<Point> GetCurrentRoutewayPath()
        {
            List<Point> path = new List<Point>();
            this.RoutewayPathBuilder.SetPath(path);
            return path;
        }

        public FactionList GetHostileFactions()
        {
            FactionList list = new FactionList();
            foreach (Faction faction in Session.Current.Scenario.Factions)
            {
                if (!((faction == this) || this.IsFriendly(faction)))
                {
                    list.Add(faction);
                }
            }
            return list;
        }

        public InformationLevel GetKnownAreaData(Point position)
        {
            if (Session.Current.Scenario.PositionOutOfRange(position))
            {
                return InformationLevel.未知;
            }
            return this.getInformationLevel(position);
        }

        public InformationLevel GetKnownAreaDataNoCheck(Point position)
        {
            return this.getInformationLevel(position);
        }

        public Legion GetLegion(Architecture will)
        {
            // 1. 精确匹配：优先查找目标建筑完全匹配的军团
            // 🔥 修复：排除 Player 军团，避免 AI 部队被错误分配到玩家军团
            // 日期：2026-03-09（重构）
            foreach (Legion legion in this.Legions)
            {
                // 🔥 关键修复：跳过玩家手动控制军团
                if (legion.Kind == LegionKind.Player)
                {
                    continue;
                }
                
                if (legion.WillArchitecture == will)
                {
                    return legion;
                }
            }

            // 2. 🔧 FIX: 模糊匹配：如果是防守任务，只要出发地匹配也可以
            if (will != null && will.BelongedFaction == this)
            {
                foreach (Legion legion in this.Legions)
                {
                    // 🔥 关键修复：跳过玩家手动控制军团
                    if (legion.Kind == LegionKind.Player)
                    {
                        continue;
                    }
                    
                    if (legion.Mission == LegionMission.Defend && legion.StartArchitecture == will)
                    {
                        return legion;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取或创建军团 - 核心军团管理方法
        /// <summary>
        /// 🔥 重构：统一的军团创建入口（新版）
        /// 日期：2026-03-09
        /// 说明：类型和任务分离，支持新的Kind+Mission组合
        /// </summary>
        public Legion CreateLegion(LegionKind kind, LegionMission mission, Architecture target)
        {
            // 🔥 Anti-Band-Aid：不检查参数，让null崩溃暴露问题
            
            // 🔥 数据验证：撤退军团的目标必须是己方城市
            // 日期：2026-03-09
            // 原因：AI部队创建撤退军团时，目标可能被错误地设置为敌方城市
            // 解决：撤退任务时，强制验证目标归属，如果是敌方城市则抛出异常
            if (mission == LegionMission.Retreat && target.BelongedFaction != this)
            {
                throw new ArgumentException(
                    $"[CreateLegion] ❌ 数据错误：撤退军团的目标 {target.Name}(ID:{target.ID}) " +
                    $"不属于势力 {this.Name}，归属势力：{target.BelongedFaction.Name}",
                    nameof(target));
            }
            
            Legion legion = new Legion
            {
                ID = Session.Current.Scenario.Legions.GetFreeGameObjectID(),
                BelongedFaction = this,
                Kind = kind,
                Mission = mission,
                Target = target,
                TargetArchitectureID = target.ID,
                WillArchitecture = target,  // 保持兼容性
                Name = LegionKindConverter.GenerateLegionName(kind, mission, target)
            };
            
            // 根据任务类型设置起始建筑
            legion.StartArchitecture = mission switch
            {
                LegionMission.Attack => this.Capital ?? target,
                LegionMission.Defend => target,
                LegionMission.Retreat => target,
                _ => target
            };
            
            Session.Current.Scenario.Legions.Add(legion);
            this.Legions.Add(legion);
            
            System.Diagnostics.Debug.WriteLine(
                $"[Faction.CreateLegion] 势力{this.Name} 创建军团: {legion.Name} " +
                $"(Kind:{kind}, Mission:{mission}, Target:{target.Name})");
            
            return legion;
        }
        
        /// <summary>
        /// 🔥 重构：获取或创建军团（新版）
        /// 日期：2026-03-09
        /// </summary>
        public Legion GetOrCreateLegionNew(LegionKind kind, LegionMission mission, Architecture target)
        {
            // 🔥 冷路径：允许使用LINQ提升可读性
            Legion existing = this.Legions
                .Cast<Legion>()
                .FirstOrDefault(l => l.Kind == kind && l.Mission == mission && l.Target == target);
            
            return existing ?? CreateLegion(kind, mission, target);
        }
        
        /// <summary>
        /// 根据目标建筑和军团类型查找匹配军团，不存在则创建
        /// 🔥 兼容性方法：逐步迁移到新版GetOrCreateLegionNew
        /// </summary>
        /// <param name="target">目标建筑</param>
        /// <param name="kind">军团类型</param>
        public Legion GetOrCreateLegion(Architecture target, LegionKind kind)
        {
            if (target == null) return null;

            // 🔥 临时兼容：将旧的kind转换为新的mission
            LegionMission mission = kind switch
            {
                LegionKind.AI => target.BelongedFaction == this ? LegionMission.Defend : LegionMission.Attack,
                LegionKind.Player => LegionMission.None,
                _ => LegionMission.None
            };

            // 1. 查找匹配的现有军团（兼容旧逻辑）
            foreach (Legion legion in this.Legions)
            {
                if (legion.WillArchitecture == target && legion.Kind == kind)
                {
                    return legion;
                }
            }

            // 2. 使用新方法创建
            return CreateLegion(kind, mission, target);
        }

        /// <summary>
        /// 🔥 重构：获取或创建军团（三参数版本）
        /// 日期：2026-03-09
        /// </summary>
        public Legion GetOrCreateLegion(Architecture target, LegionKind kind, LegionMission mission)
        {
            if (target == null) return null;

            // 查找匹配的现有军团
            foreach (Legion legion in this.Legions)
            {
                if (legion.Kind == kind && legion.Mission == mission && legion.Target == target)
                {
                    return legion;
                }
            }

            // 创建新军团
            return CreateLegion(kind, mission, target);
        }

        /// <summary>
        /// 🔥 重构：获取或创建默认军团 - 确保部队总能分配到军团
        /// 日期：2026-03-09
        /// 更新：使用新的Kind+Mission系统
        /// </summary>
        public Legion GetOrCreateDefaultLegion(Architecture target)
        {
            if (target == null) return null;

            // 🔥 根据目标建筑归属判断任务类型
            bool isOwnArchitecture = target.BelongedFaction == this;
            LegionMission mission = isOwnArchitecture ? LegionMission.Defend : LegionMission.Attack;
            
            System.Diagnostics.Debug.WriteLine(
                $"[GetOrCreateDefaultLegion] 势力:{this.Name}，目标:{target.Name}" +
                $"(归属:{target.BelongedFaction?.Name ?? "无"})，判定任务:{mission}");

            // 查找现有的AI军团
            foreach (Legion legion in this.Legions)
            {
                if (legion.Kind == LegionKind.AI && 
                    legion.Mission == mission && 
                    legion.Target == target)
                {
                    return legion;
                }
            }

            // 创建新的AI军团
            return CreateLegion(LegionKind.AI, mission, target);
        }

        /// <summary>
        /// 🔥 重构：获取或创建玩家军团
        /// 日期：2026-03-09
        /// 更新：使用新的Kind+Mission系统
        /// </summary>
        public Legion GetOrCreatePlayerLegion(Architecture startArchitecture)
        {
            // 🔥 Anti-Band-Aid：不添加防御性空检查
            // 🔥 2026-03-16 修复：ID=0 是有效的（洛阳的 ID=0），必须使用 >= 0
            // 验证建筑ID有效性
            if (startArchitecture.ID < 0)
            {
                throw new ArgumentException(
                    $"[GetOrCreatePlayerLegion] ❌ 数据错误：建筑 {startArchitecture.Name} 的 ID={startArchitecture.ID} 无效（必须 >= 0）",
                    nameof(startArchitecture));
            }
            
            // 验证建筑归属
            if (startArchitecture.BelongedFaction != this)
            {
                throw new ArgumentException(
                    $"[GetOrCreatePlayerLegion] ❌ 数据错误：建筑 {startArchitecture.Name}(ID:{startArchitecture.ID}) 不属于势力 {this.Name}",
                    nameof(startArchitecture));
            }

            // 查找现有的玩家军团
            foreach (Legion legion in this.Legions)
            {
                if (legion.Kind == LegionKind.Player && 
                    legion.StartArchitecture == startArchitecture)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetOrCreatePlayerLegion] 找到现有军团: {legion.Name}");
                    return legion;
                }
            }

            // 创建新的玩家军团
            Legion newLegion = CreateLegion(LegionKind.Player, LegionMission.None, startArchitecture);
            
            System.Diagnostics.Debug.WriteLine(
                $"[GetOrCreatePlayerLegion] 创建军团：{newLegion.Name}（基地：{startArchitecture.Name}, ID:{startArchitecture.ID}）");
            
            return newLegion;
        }

        /// <summary>
        /// 🔧 FIX: 为指定建筑创建默认军团，确保部队总有归属（兼容旧接口）
        /// </summary>
        public Legion CreateDefaultLegion(Architecture architecture)
        {
            if (architecture == null) return null;

            // 🔥 重构：使用新的Kind+Mission系统
            // 日期：2026-03-09
            bool isOwnArchitecture = architecture.BelongedFaction == this;
            LegionKind kind = LegionKind.AI;
            LegionMission mission = isOwnArchitecture ? LegionMission.Defend : LegionMission.Attack;

            return GetOrCreateLegion(architecture, kind, mission);
        }

        /// <summary>
        /// 清理已完成的军团
        /// </summary>
        public void CleanupCompletedLegions()
        {
            var toRemove = new List<Legion>();
            foreach (Legion legion in this.Legions)
            {
                // Self-heal legacy saves: missing Kind field may default Player legions to AI.
                if (legion.Kind == LegionKind.AI &&
                    !string.IsNullOrEmpty(legion.Name) &&
                    legion.Name.StartsWith("Player_", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CleanupCompletedLegions][Recover] 军团类型修正: {legion.Name}(ID:{legion.ID}) AI -> Player");
                    legion.Kind = LegionKind.Player;
                }

                if (legion.IsComplete)
                {
                    toRemove.Add(legion);
                }
            }

            foreach (var legion in toRemove)
            {
                legion.Disband();
            }

            if (toRemove.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupCompletedLegions] 势力{this.Name} 清理了{toRemove.Count}个已完成军团");
            }
        }

        public int GetMapCost(Troop troop, Point position, MilitaryKind kind)
        {
            if (Session.Current.Scenario.PositionOutOfRange(position))
            {
                return 0xdac;
            }
            if (Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(position).RoutewayConsumptionRate >= 1)
            {
                return 0xdac;
            }
            
            // 🔥 关键修复：检查地形的 CanExtendInto 属性（峻岭等绝对不可进入地形）
            // 日期：2026-03-12
            // 问题：峻岭（CanExtendInto: false）的地形成本计算错误
            // 解决：优先检查 CanExtendInto，如果为 false 且没有城池则直接返回 0xdac
            // ANTI-BAND-AID：不添加 null 检查，地形数据缺失时应该 Fail Fast
            Architecture onArch = Session.Current.Scenario.GetArchitectureByPositionNoCheck(position);
            if (onArch == null)
            {
                TerrainDetail terrainDetail = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(position);
                if (!terrainDetail.CanExtendInto)
                {
                    return 0xdac; // 峻岭等绝对不可进入地形
                }
            }
            
            int terrainAdaptability = 0;

            if (onArch == null)
            {
                terrainAdaptability = troop.GetTerrainAdaptability((TerrainKind)this.mapData[position.X, position.Y]);
            }
            int waterPunishment = 0;
            if (this.mapData[position.X, position.Y] == 6 && kind.Type != MilitaryType.水军 && onArch == null)
            {
                waterPunishment = 3;
            }
            return ((terrainAdaptability + Session.Current.Scenario.GetWaterPositionMapCost(kind, position)) + Session.Current.Scenario.GetPositionMapCost(this, position) + waterPunishment);
        }

        public FactionList GetOtherFactions()
        {
            FactionList list = new FactionList();
            foreach (Faction faction in Session.Current.Scenario.Factions)
            {
                if (faction != this)
                {
                    list.Add(faction);
                }
            }
            return list;
        }

        public SectionList GetOtherSections(Section section)
        {
            SectionList list = new SectionList();
            foreach (Section section2 in this.Sections)
            {
                if (section2 != section)
                {
                    list.Add(section2);
                }
            }
            return list;
        }

        private float GetPreferredTechniqueComplition()
        {
            int num = 0;
            int num2 = 0;
            foreach (Technique technique in Session.Current.Scenario.GameCommonData.AllTechniques.Techniques.Values)
            {
                if (this.PreferredTechniqueKinds.IndexOf(technique.Kind) >= 0)
                {
                    num2++;
                    if (this.HasTechnique(technique.ID))
                    {
                        num++;
                    }
                }
            }
            if (num2 > 0)
            {
                return (((float)num) / ((float)num2));
            }
            return 0f;
        }

        private Technique GetRandomTechnique()
        {
            Dictionary<Technique, float> list = new Dictionary<Technique, float>();
            foreach (Technique technique in Session.Current.Scenario.GameCommonData.AllTechniques.Techniques.Values)
            {
                if (this.IsTechniqueUpgradable(technique) && this.GetTechniqueUsefulness(technique) > 0)
                {
                    float weight = 1;
                    foreach (KeyValuePair<Condition, float> c in technique.AIConditionWeight)
                    {
                        if (c.Key.CheckCondition(this))
                        {
                            weight *= c.Value;
                        }
                    }

                    list.Add(technique, weight);
                }
            }
            if (list.Count > 0)
            {
                return GameObject.WeightedRandom(list);
            }
            return null;
        }

        public List<Point> GetSecondTierKnownPath(Point start, Point end)
        {
            ClosedPathEndpoints key = new ClosedPathEndpoints(start, end);
            if (this.SecondTierKnownPaths.ContainsKey(key))
            {
                return this.SecondTierKnownPaths[key];
            }
            return null;
        }

        public List<Point> GetThirdTierKnownPath(Point start, Point end)
        {
            ClosedPathEndpoints key = new ClosedPathEndpoints(start, end);
            if (this.ThirdTierKnownPaths.ContainsKey(key))
            {
                return this.ThirdTierKnownPaths[key];
            }
            return null;
        }

        private int GetThreat(Faction faction)
        {
            if (faction == null)
            {
                return 0;
            }
            return ((faction.ArchitectureTotalSize * 10) - Session.Current.Scenario.GetDiplomaticRelation(base.ID, faction.ID));
        }

        public GameObjectList GetUnderZeroDiplomaticRelationFactions()
        {
            GameObjectList list = new GameObjectList();
            foreach (DiplomaticRelation relation in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
            {
                if (relation.Relation < 0)
                {
                    if (relation.RelationFaction1 == this)
                    {
                        list.Add(relation.RelationFaction2);
                    }
                    else
                    {
                        list.Add(relation.RelationFaction1);
                    }
                }
            }
            return list;
        }

        public Faction GetFactionByName(string FactionName)
        {
            foreach (Faction i in Session.Current.Scenario.Factions)
            {
                if (i.Name == FactionName) return i;
            }
            return null;
        }

        public void HandleForcedChangeCapital()
        {
            this.Reputation /= 2;
            if (this.Architectures.Count != 1)
            {
                Architecture capital = this.Capital;
                if (this.Architectures.Count == 2)
                {
                    foreach (Architecture architecture2 in this.Architectures)
                    {
                        if (architecture2 != this.Capital)
                        {
                            this.Capital = architecture2;
                            if (this.OnForcedChangeCapital != null)
                            {
                                this.OnForcedChangeCapital(this, capital, this.Capital);
                            }
                            Session.Current.Scenario.YearTable.addChangeCapitalEntry(Session.Current.Scenario.Date, this, this.Capital);

                            // 🔥 强制迁都触发人员调配紧急事件
                            // 🛡️ 防止递归：如果当前正在执行人员调配，不再触发通知（避免循环）
                            if (!_isExecutingPersonnelAllocation)
                            {
                                this.NotifyPersonnelUrgentEvent($"被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}");

#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[HandleForcedChangeCapital] 势力 {this.Name} 被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}，触发人员调配紧急事件");
#endif
                            }
                            else
                            {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[HandleForcedChangeCapital] 势力 {this.Name} 被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}，但正在执行人员调配，跳过触发");
#endif
                            }

                            break;
                        }
                    }
                }
                else
                {
                    this.Capital = this.SelectNewCapital();
                    if (this.OnForcedChangeCapital != null)
                    {
                        this.OnForcedChangeCapital(this, capital, this.Capital);
                    }
                    Session.Current.Scenario.YearTable.addChangeCapitalEntry(Session.Current.Scenario.Date, this, this.Capital);

                    // 🔥 强制迁都触发人员调配紧急事件
                    // 🛡️ 防止递归：如果当前正在执行人员调配，不再触发通知（避免循环）
                    if (!_isExecutingPersonnelAllocation)
                    {
                        this.NotifyPersonnelUrgentEvent($"被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}");

#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[HandleForcedChangeCapital] 势力 {this.Name} 被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}，触发人员调配紧急事件");
#endif
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[HandleForcedChangeCapital] 势力 {this.Name} 被迫迁都: {capital?.Name ?? "无"} → {this.Capital.Name}，但正在执行人员调配，跳过触发");
#endif
                    }
                }
            }
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseForceChangeCapital(Session.Current.Scenario, this);
        }

        public bool HasArchitecture(Architecture architecture)
        {
            return this.Architectures.HasGameObject(architecture);
        }

        public bool HasCaptive()
        {
            return (this.CaptiveCount > 0);
        }

        /*
        public int EachMilitaryKindCount(int id)
        {
            int count = 0;
            foreach (Military military in this.Militaries)
            {
                if (military.RealKindID == id)
                {
                    count++;
                }
            }
            MilitaryKind mk = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(id);
            return count;
        }
        */

        public bool IsMilitaryKindOverLimit(int id)
        {
            int count = 0;
            foreach (Military military in this.Militaries)
            {
                if (military.RealKindID == id)
                {
                    count++;
                }
            }


            MilitaryKind mk = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(id);
            return count >= mk.RecruitLimit;


        }

        public bool HasPerson(Person person)
        {
            return this.Persons.HasGameObject(person);
        }

        public bool HasSelfCaptive()
        {
            return (this.SelfCaptiveCount > 0);
        }

        public bool HasTechnique(int id)
        {
            return (this.AvailableTechniques.GetTechnique(id) != null);
        }

        public void IncreaseReputation(int increment)
        {
            this.reputation += increment;
            if (this.reputation > this.shengwangshangxian) this.reputation = this.shengwangshangxian;

        }

        public void IncreaseTechniquePoint(int increment)
        {
            this.techniquePoint = (int)(this.techniquePoint + increment * Session.GlobalVariables.TechniquePointMultiple);
        }

        public List<Architecture> GettingInformationArchitectures()
        {
            List<Architecture> result = new List<Architecture>();
            foreach (Person p in this.Persons)
            {
                if (p.OutsideTask == OutsideTaskKind.情报)
                {
                    if (!p.OutsideDestination.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"数据损坏：人物 {p.Name}(ID={p.ID}) 正在执行 {p.OutsideTask}，但 OutsideDestination 为空。");
                    }
                    Architecture a = Session.Current.Scenario.GetArchitectureByPositionNoCheck(p.OutsideDestination.Value);
                    result.Add(a);
                }
            }
            return result;
        }

        private void InformationDayEvent()
        {
            InformationList list = new InformationList();
            foreach (Information information in this.Informations)
            {
                information.DaysLeft -= Session.Parameters.DayInTurn;
                information.DaysStarted += Session.Parameters.DayInTurn;
                if (information.DaysLeft <= 0)
                {
                    list.Add(information);
                }
                else
                {
                    information.CheckAmbushTroop();
                }
            }
            foreach (Architecture a in this.Architectures)
            {
                foreach (Information info in a.Informations)
                {
                    info.DaysStarted += Session.Parameters.DayInTurn;
                }
            }
            foreach (Information information in list)
            {
                information.Purify();
                this.RemoveInformation(information);
                Session.Current.Scenario.Informations.Remove(information);
            }
        }

        public bool IsArchitectureKnown(Architecture a)
        {
            foreach (Point point in a.ArchitectureArea.Area)
            {
                if (this.getInformationLevel(point) != InformationLevel.无)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsTroopKnown(Troop t)
        {
            return this.getInformationLevel(t.Position) != InformationLevel.无;
        }

        public bool IsFriendly(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            return ((faction == this) || (Session.Current.Scenario.GetDiplomaticRelation(base.ID, faction.ID) >= Session.GlobalVariables.FriendlyDiplomacyThreshold) || (Session.Current.Scenario.GetDiplomaticRelationTruce(base.ID, faction.ID) > 0));
        }

        public bool IsFriendlyWithoutTruce(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            return (faction == this) || (Session.Current.Scenario.GetDiplomaticRelation(base.ID, faction.ID) >= Session.GlobalVariables.FriendlyDiplomacyThreshold);
        }

        public bool IsHostile(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            if (faction == this)
            {
                return false;
            }
            return (Session.Current.Scenario.GetDiplomaticRelation(base.ID, faction.ID) < 0);
        }

        public bool IsPositionKnown(Point position)
        {
            if (Session.Current.Scenario.PositionOutOfRange(position))
            {
                return false;
            }
            return (this.getInformationLevel(position) != InformationLevel.无);
        }

        private bool IsTechniqueUpgradable(Technique t)
        {
            return (!this.HasTechnique(t.ID) && ((t.PreID < 0) || this.HasTechnique(t.PreID))) && t.CanResearch(this);
        }

        public bool IsTechniqueUpgrading(int id)
        {
            return (id == this.UpgradingTechnique);
        }

        public List<string> LoadInformationsFromString(InformationList informations, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Informations为null
            if (this.Informations == null)
            {
                this.Informations = new InformationList();
            }
            this.Informations.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Information gameObject = informations.GetGameObject(int.Parse(str)) as Information;
                    if (gameObject != null)
                    {
                        this.AddInformation(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("情报ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("情报列表应为半型空格分隔的情报ID");
            }
            return errorMsg;
        }

        public List<string> LoadLegionsFromString(LegionList legions, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Legions为null
            if (this.Legions == null)
            {
                this.Legions = new LegionList();
            }
            this.Legions.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Legion gameObject = legions.GetGameObject(int.Parse(str)) as Legion;
                    if (gameObject != null)
                    {
                        this.AddLegion(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("LegionID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("Legion列表应为半型空格分隔的LegionID");
            }
            return errorMsg;
        }

        public List<string> LoadRoutewaysFromString(RoutewayList routeways, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Routeways为null
            if (this.Routeways == null)
            {
                this.Routeways = new RoutewayList();
            }
            this.Routeways.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Routeway gameObject = routeways.GetGameObject(int.Parse(str)) as Routeway;
                    if (gameObject != null)
                    {
                        this.AddRouteway(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("粮道ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("粮道列表应为半型空格分隔的粮道ID");
            }
            return errorMsg;
        }

        public List<string> LoadArchitecturesFromString(ArchitectureList architectures, string dataString)
        {
            List<string> errorMsg = [];
            
            // 🔥 修复：如果 dataString 为空，直接返回
            // 原因：二进制存档不使用这些字符串，关系已在 LinkScenarioReferences 中建立
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = [' ', '\n', '\r', '\t'];
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Architectures为null
            if (this.Architectures == null)
            {
                this.Architectures = new ArchitectureList();
            }
            this.Architectures.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    // 🔥 2026-03-16 修复：使用 TryParse 而不是 Parse，避免 FormatException
                    // 原因：字符串末尾可能有空格或其他无效字符
                    if (!int.TryParse(str, out int archId))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Faction.LoadArchitecturesFromString] {Name} 跳过无效ID: '{str}'");
                        continue;
                    }
                    
                    Architecture architecture = architectures.GetGameObject(archId) as Architecture;
                    if (architecture != null)
                    {
                        this.AddArchitecture(architecture);
                        this.AddArchitectureMilitaries(architecture);

                    }
                    else
                    {
                        errorMsg.Add("建筑ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("建筑列表应为半型空格分隔的建筑ID");
            }
            return errorMsg;
        }

        public List<string> LoadSectionsFromString(SectionList sections, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Sections为null
            if (this.Sections == null)
            {
                this.Sections = new SectionList();
            }
            this.Sections.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Section section = sections.GetGameObject(int.Parse(str)) as Section;
                    if (section != null)
                    {
                        this.AddSection(section);
                    }
                    else
                    {
                        errorMsg.Add("军区ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("军区列表应为半型空格分隔的军区ID");
            }
            if (this.SectionCount == 0)
            {
                // errorMsg.Add("没有军区");
            }
            return errorMsg;
        }

        public List<string> LoadTroopsFromString(TroopList troops, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 防止dataString为null
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            // 防止反序列化后Troops为null
            if (this.Troops == null)
            {
                this.Troops = new TroopList();
            }
            this.Troops.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Troop gameObject = troops.GetGameObject(int.Parse(str)) as Troop;
                    if (gameObject != null)
                    {
                        this.AddTroop(gameObject);
                        this.AddTroopMilitary(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("部队ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("部队列表应为半型空格分隔的部队ID");
            }
            return errorMsg;
        }

        public int getTechniqueActualPointCost(Technique technique)
        {
            if (this.techniquePointCostRateDecrease.Count == 0) return technique.PointCost;
            return (int)Math.Round(technique.PointCost * (1 - this.techniquePointCostRateDecrease.Max()));
        }

        public int getTechniqueActualReputation(Technique technique)
        {
            if (this.techniqueReputationRateDecrease.Count == 0) return technique.Reputation;
            return (int)Math.Round(technique.Reputation * (1 - this.techniqueReputationRateDecrease.Max()));
        }

        public int getTechniqueActualFundCost(Technique technique)
        {
            if (this.techniqueFundCostRateDecrease.Count == 0) return technique.FundCost;
            return (int)Math.Round(technique.FundCost * (1 - this.techniqueFundCostRateDecrease.Max()));
        }

        public int getTechniqueActualTime(Technique technique)
        {
            if (this.techniqueTimeRateDecrease.Count == 0) return technique.Days;
            return (int)Math.Round(technique.Days * (1 - techniqueTimeRateDecrease.Max()));
        }

        public bool MatchTechnique(Technique technique, Architecture architecture)
        {
            return (((((this.TotalTechniquePoint >= this.getTechniqueActualPointCost(technique)) &&
                (this.Reputation >= this.getTechniqueActualReputation(technique))) &&
                (architecture.Fund >= this.getTechniqueActualFundCost(technique))) &&
                (this.HasTechnique(technique.PreID) || (technique.PreID < 0))) && (this.UpgradingTechnique < 0)) && technique.CanResearch(this);
        }

        public void MonthEvent()
        {
            this.FactionDiplomaticRelation();
            powerCache = null;

            // AI自动褒赏（每月执行）
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                AI_RewardManager rewardManager = new();
                rewardManager.ExecuteMonthlyRewards(this);
            }
        }

        private void PlayerAI()
        {
            if (this.Capital == null)
            {
                this.AIFinished = true;
                return;
            }
            try
            {
                Session.Current.Scenario.Threading = true;
                this.AIFinished = false;

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[PlayerAI] 开始玩家AI处理: " + this.Name);
                }

                this.AIPrepare();
                // System.Diagnostics.Debug.WriteLine($"[Diagnostic] PlayerAI EXECUTION START: Faction={this.Name}");

                this.PlayerAITransfer();

                // 🔥 修复：玩家势力也需要执行军区AI系统
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] PlayerAI: 玩家势力 {this.Name} 调用军区AI(AISectionsOptimized)");
                }
                this.AISectionsOptimized(); // 添加军区AI调用

                this.PlayerTechniqueAI();
                this.PlayerAIArchitectures();
                this.PlayerAILegions();
                this.PlayerAIAppointMayor();
                this.PlayerAIAppointAdvisor();
                this.AITrainChildren();

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[PlayerAI] 玩家AI处理完成: " + this.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PlayerAI] 玩家AI处理异常 " + this.Name + ": " + ex.Message);
                System.Diagnostics.Debug.WriteLine("[PlayerAI] 堆栈跟踪: " + ex.StackTrace);
            }
            finally
            {
                // 确保无论如何都重置状态
                this.AIFinished = true;
                Session.Current.Scenario.Threading = false;
                // System.Diagnostics.Debug.WriteLine("[PlayerAI] 玩家AI处理周期结束: " + this.Name);
            }
        }


        private void PlayerAIArchitectures()
        {
            foreach (GameObject obj in this.Architectures.GetRandomList())
            {
                Architecture architecture = (obj is Architecture ? (Architecture)obj : null);
                if (architecture == null) continue;
                
                // 🔥 关键逻辑变更：只有开启了AutoRun的军区才由AI托管。 
                // 如果建筑没有军区(BelongedSection == null)，在玩家势力下通常意味着君主直辖，不应由AI自动执行。
                if (architecture.BelongedSection != null &&
                    architecture.BelongedSection.AIDetail != null &&
                    architecture.BelongedSection.AIDetail.AutoRun)
                {
                    // System.Diagnostics.Debug.WriteLine($"[Diagnostic] Calling Architecture.AI for {architecture.Name} (Section:{architecture.BelongedSection?.Name} AutoRun:{architecture.BelongedSection?.AIDetail?.AutoRun})");
                    architecture.AI();
                }
                else
                {
                    // 玩家直接控制的城市执行 PlayerAutoAI (处理一些基础自动项如人口增长，但不进行决策)
                    // System.Diagnostics.Debug.WriteLine($"[Diagnostic] Calling Architecture.PlayerAutoAI for {architecture.Name} (Section:{architecture.BelongedSection?.Name} AutoRun:{architecture.BelongedSection?.AIDetail?.AutoRun})");
                    architecture.PlayerAutoAI();
                }
            }
        }

        private void PlayerAILegions()
        {
            foreach (GameObject obj in this.Legions.GetRandomList())
            {
                // 🔥 C# 12: 使用模式匹配替代三元运算符
                if (obj is not Legion legion) continue;
                
                if ((legion.StartArchitecture != null) && (legion.StartArchitecture.BelongedFaction == this))
                {
                    if (legion.StartArchitecture.BelongedSection != null &&
                        legion.StartArchitecture.BelongedSection.AIDetail != null &&
                        legion.StartArchitecture.BelongedSection.AIDetail.AutoRun)
                    {
                        legion.AI();
                    }
                    else
                    {
                        legion.AIWithAuto();
                    }
                }
                else
                {
                    legion.AIWithAuto();
                }
            }
        }

        private void PlayerAIAppointMayor()
        {
            AutoAppointMayor(); // 恢复调用，内部已有正确判断逻辑
        }

        private void PlayerAIAppointAdvisor()
        {
            AIAppointAdvisor();
        }

        private void PlayerTechniqueAI()
        {
            this.SaveTechniquePointForTechnique(this.TechniquePoint / 4);
        }

        public void PrepareData()
        {
            this.mapData = Session.Current.Scenario.ScenarioMap.MapData;
            this.architectureAdjustCost = new int[Session.Current.Scenario.ScenarioMap.MapDimensions.X, Session.Current.Scenario.ScenarioMap.MapDimensions.Y];
            this.knownAreaData = new Dictionary<Point, InformationTile>();
            this.PrepareSecondTierMapCost();
            this.PrepareThirdTierMapCost();
            this.PrepareKnownAreaData();
            this.PrepareInformations();
        }

        public void PrepareInformations()
        {
            foreach (Information information in this.Informations)
            {
                information.Initialize();
            }
        }

        protected void PrepareKnownAreaData()
        {
            foreach (Architecture architecture in this.Architectures)
            {
                this.AddArchitectureKnownData(architecture);
            }
        }

        private void PrepareSecondTierMapCost()
        {
            this.SecondTierXResidue = Session.Current.Scenario.ScenarioMap.MapDimensions.X % GameObjectConsts.SecondTierSquareSize;
            this.SecondTierYResidue = Session.Current.Scenario.ScenarioMap.MapDimensions.Y % GameObjectConsts.SecondTierSquareSize;
            int num = Session.Current.Scenario.ScenarioMap.MapDimensions.X / GameObjectConsts.SecondTierSquareSize;
            int num2 = Session.Current.Scenario.ScenarioMap.MapDimensions.Y / GameObjectConsts.SecondTierSquareSize;
            if (this.SecondTierXResidue > 0)
            {
                num++;
            }
            if (this.SecondTierYResidue > 0)
            {
                num2++;
            }
            this.secondTierMapCost = new int[num, num2];
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num2; j++)
                {
                    this.secondTierMapCost[i, j] = this.GetAverageValueOfSecondTier(i, j);
                }
            }
        }

        private void PrepareThirdTierMapCost()
        {
            this.ThirdTierXResidue = Session.Current.Scenario.ScenarioMap.MapDimensions.X % GameObjectConsts.ThirdTierSquareSize;
            this.ThirdTierYResidue = Session.Current.Scenario.ScenarioMap.MapDimensions.Y % GameObjectConsts.ThirdTierSquareSize;
            int num = Session.Current.Scenario.ScenarioMap.MapDimensions.X / GameObjectConsts.ThirdTierSquareSize;
            int num2 = Session.Current.Scenario.ScenarioMap.MapDimensions.Y / GameObjectConsts.ThirdTierSquareSize;
            if (this.ThirdTierXResidue > 0)
            {
                num++;
            }
            if (this.ThirdTierYResidue > 0)
            {
                num2++;
            }
            this.thirdTierMapCost = new int[num, num2];
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num2; j++)
                {
                    this.thirdTierMapCost[i, j] = this.GetAverageValueOfThirdTier(i, j);
                }
            }
        }

        public void PurifyTechniques()
        {
            foreach (Technique technique in this.AvailableTechniques.Techniques.Values)
            {
                technique.Influences.PurifyInfluence(this, GameObjects.Influences.Applier.Technique, technique.ID);
            }
        }

        private void RebuildSections()
        {
            if ((this.Capital != null) && (Session.MainGame.mainGameScreen.LoadScenarioInInitialization || Session.Current.Scenario.Date.Day == 1 || this.SectionCount == 0))
            {
                if (Session.MainGame.mainGameScreen.LoadScenarioInInitialization || ((Session.Current.Scenario.Date.Month % 3) == 1))
                {
                    this.ClearSections();
                    this.BuildSectionByArchitectureList(this.Architectures.GetList());
                    foreach (Section section in this.Sections)
                    {
                        section.RefreshSectionName();
                    }
                }
                this.SetSectionAIDetail();
                this.SetSectionAIDetailPinAtPlayer();
            }
        }

        public void RemoveArchitecture(Architecture architecture)
        {
            // 🔥 NEW: 通知势力AI紧急事件 - 城市丢失
            this.NotifyPersonnelUrgentEvent($"城市{architecture.Name}丢失");
            this.NotifyDomesticUrgentEvent($"城市{architecture.Name}丢失");

            this.Architectures.Remove(architecture);
            architecture.BelongedFaction = null;
        }

        public void RemoveArchitectureKnownData(Architecture a)
        {
            foreach (Point point in a.ArchitectureArea.Area)
            {
                this.RemoveKnownAreaData(point, InformationLevel.全);
            }
            foreach (Point point in a.ViewArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    this.RemoveKnownAreaData(point, InformationLevel.高);
                }
            }
            if (a.Kind.HasLongView)
            {
                foreach (Point point in a.LongViewArea.Area)
                {
                    if (!Session.Current.Scenario.PositionOutOfRange(point))
                    {
                        this.RemoveKnownAreaData(point, InformationLevel.中);
                    }
                }
            }
        }

        public void RemoveArchitectureMilitaries(Architecture architecture)
        {
            foreach (Military military in architecture.Militaries)
            {
                this.RemoveMilitary(military);
            }
        }

        public void RemoveInformation(Information information)
        {
            this.Informations.Remove(information);
            information.BelongedFaction = null;
        }

        public void RemoveLegion(Legion legion)
        {
            // 🔥 根本修复：同时从两个列表移除
            // 日期：2026-03-22
            // 原因：Legion.Disband() 调用 RemoveLegion() 后，军团仍在 Scenario.Legions 中
            //       导致 GameScenario.DayPassedEvent() 检测到空军团，再次调用 Disband()
            // 解决：同步移除，确保列表一致性
            
            this.Legions.Remove(legion);
            legion.BelongedFaction = null;
            
            // 🔥 ANTI-BAND-AID：不使用防御性空检查，让 null 崩溃暴露问题
            // 如果 Session.Current 或 Scenario 或 Legions 为 null，说明数据损坏
            Session.Current.Scenario.Legions.Remove(legion);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[Faction.RemoveLegion] 势力{this.Name} 移除军团: {legion.Name} " +
                $"(Kind:{legion.Kind}, Mission:{legion.Mission})");
            #endif
        }

        public void RemoveMilitary(Military military)
        {
            this.Militaries.Remove(military);
            /*if (this.militaryKindCounts.ContainsKey(military.Kind))
            {
                this.militaryKindCounts[military.Kind]--;
            }*/
            military.BelongedFaction = null;
        }

        /*
        public void MorphMilitary(MilitaryKind before, MilitaryKind after)
        {
            if (this.militaryKindCounts.ContainsKey(before))
            {
                this.militaryKindCounts[before]--;
            }
            if (this.militaryKindCounts.ContainsKey(after))
            {
                this.militaryKindCounts[after]++;
            }
            else
            {
                this.militaryKindCounts[after] = 1;
            }
        }
        */
        public void RemovePositionInformation(Point position, InformationLevel level)
        {
            if (!Session.Current.Scenario.PositionOutOfRange(position))
            {
                this.RemoveKnownAreaData(position, level);
            }
        }

        public void RemoveRouteway(Routeway routeway)
        {
            this.Routeways.Remove(routeway);
            Session.Current.Scenario.Routeways.Remove(routeway);
            routeway.BelongedFaction = null;
        }

        public void RemoveSection(Section section)
        {
            this.Sections.Remove(section);
            section.BelongedFaction = null;
        }

        public void RemoveTroop(Troop troop)
        {
            this.Troops.Remove(troop);
            troop.BelongedFaction = null;
        }

        public void RemoveTroopKnownAreaData(Troop troop)
        {
            foreach (Point point in troop.ViewArea.Area)
            {
                if (Session.Current.Scenario.PositionOutOfRange(point))
                {
                    continue;
                }
                if (point == troop.ViewArea.Centre)
                {
                    this.RemoveKnownAreaData(point, InformationLevel.全);
                }
                else
                {
                    this.RemoveKnownAreaData(point, troop.ScoutLevel);
                }
            }
        }

        public void RemoveTroopMilitary(Troop troop)
        {
            this.RemoveMilitary(troop.Army);
        }

        public bool adjacentTo(Faction f)
        {
            if (this == f) return false;
            if (f != null)
            {
                foreach (Architecture i in this.Architectures)
                {
                    foreach (Architecture j in f.Architectures)
                    {
                        if (i.AILandLinks.HasGameObject(j) || i.AIWaterLinks.HasGameObject(j))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public FactionList AdjecentFactionList
        {
            get
            {
                /* FactionList list = new FactionList();
                 foreach (Faction f in this.GetAdjecentFactions())
                 {
                     list.Add(f);
                 }
                 return list;
                 */
                return this.GetAdjecentFactions();
            }
        }

        public FactionList GetAdjecentFactions()
        {
            FactionList result = new FactionList();
            foreach (Faction f in Session.Current.Scenario.Factions)
            {
                if (this.adjacentTo(f))
                {
                    result.Add(f);
                }
            }
            return result;
        }

        public FactionList GetAdjecentHostileFactions()
        {
            FactionList result = new FactionList();
            foreach (Faction f in Session.Current.Scenario.Factions)
            {
                if (this.adjacentTo(f) && this.IsHostile(f))
                {
                    result.Add(f);
                }
            }
            return result;
        }

        private bool hasNonFriendlyFrontline
        {
            get
            {
                foreach (Architecture i in this.Architectures)
                {
                    foreach (Architecture j in i.AILandLinks)
                    {
                        if (j.BelongedFaction == null) continue;
                        if (j.BelongedFaction.ID == this.ID) continue;
                        if (Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(j.BelongedFaction.ID, this.ID).Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                        {
                            return true;
                        }
                    }
                    foreach (Architecture j in i.AIWaterLinks)
                    {
                        if (j.BelongedFaction == null) continue;
                        if (j.BelongedFaction.ID == this.ID) continue;
                        if (Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(j.BelongedFaction.ID, this.ID).Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public FactionList GetEncircleFactionList(Faction target, bool simulate)
        {
            FactionList encircleList = new FactionList();
            foreach (Faction f in Session.Current.Scenario.Factions)
            {
                if (Session.Current.Scenario.IsPlayer(f)) continue; // TODO let player choose whether to enter
                if (f.Leader.Status == PersonStatus.Captive) continue;
                if ((f != target) && (f.Leader.StrategyTendency != PersonStrategyTendency.维持现状 || Session.GlobalVariables.IgnoreStrategyTendency) && !f.IsAlien)
                {
                    if (((Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(target.ID, f.ID).Relation +
                        Person.GetIdealOffset(target.Leader, f.Leader) * 1.5) < 0
                        && (GameObject.Chance(60 - Math.Min(60, target.Leader.Karma)) || simulate) && !f.IsFriendly(target) &&
                        (f.adjacentTo(target) || GameObject.Chance(30 - Math.Min(60, target.Leader.Karma) / 2) || simulate))
                        )
                    {
                        encircleList.Add(f);
                    }
                }
            }
            if (encircleList.Count >= 3 && (!simulate || encircleList.Count >= 6))
            {
                return encircleList;
            }
            else
            {
                return null;
            }
        }

        public void CheckEncircleDiplomatic(Faction target)
        {
            FactionList encircleList = GetEncircleFactionList(target, false);
            if (encircleList != null)
            {
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader?.Name ?? "", TextMessageKind.EncircleDiplomaticRelation, "EncircleDiplomaticRelation", "EncircleDiplomaticRelation.jpg", "EncircleDiplomaticRelation", target?.Name ?? "", true);
                foreach (Faction i in encircleList)
                {
                    foreach (Faction j in encircleList)
                    {
                        if (i != j)
                        {
                            if (Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(i.ID, j.ID).Truce < 180)
                            {
                                Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(i.ID, j.ID).Truce = 180;
                            }
                        }
                    }
                    target.Leader.AdjustRelation(i.Leader, -12f, -4);
                    i.Leader.AdjustRelation(target.Leader, -3f, -1);
                }
            }
        }

        public void Encircle(Architecture encircler, Faction toEncircle)
        {
            Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.NeutralPerson, encircler?.BelongedFaction?.Leader?.Name ?? "", "DenounceDiplomaticRelation", "DenounceDiplomaticRelation.jpg", "DenounceDiplomaticRelation", toEncircle?.Name ?? "", true);

            if (encircler.Fund < 120000) return;
            encircler.Fund -= 120000;

            DiplomaticRelation rel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(this.ID, toEncircle.ID);
            if (rel.Relation > -Session.GlobalVariables.FriendlyDiplomacyThreshold)
            {
                rel.Relation -= 100;
                toEncircle.Leader.AdjustRelation(this.Leader, -18f, -6);
                this.Leader.AdjustRelation(toEncircle.Leader, -6f, -2);
            }
            else
            {
                rel.Relation -= 50;
                toEncircle.Leader.AdjustRelation(this.Leader, -18f, -6);
                this.Leader.AdjustRelation(toEncircle.Leader, -6f, -2);
            }
            //处理所有势力和被声讨方的关系
            foreach (DiplomaticRelation f in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(toEncircle.ID))
            {
                if (f.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold / 2)
                {
                    f.Relation -= 100;
                }
            }
            //加入包围圈判定
            this.CheckEncircleDiplomatic(toEncircle);
        }

        private void ResetFriendlyDiplomaticRelations()
        {
            foreach (DiplomaticRelation i in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
            {
                if (this.IsAlien) continue;
                Faction opposite = i.GetDiplomaticFaction(this.ID);
                if (opposite != null && i.Relation >= -Session.GlobalVariables.FriendlyDiplomacyThreshold && opposite.IsAlien)
                {
                    i.Relation -= 15;
                }
            }

            if (Session.Current.Scenario.IsPlayer(this)) return;

            //bool relationBroken = false;

            if (Session.GlobalVariables.PinPointAtPlayer)
            {
                foreach (DiplomaticRelation i in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    Faction opposite = i.GetDiplomaticFaction(this.ID);
                    if (i.Relation >= -Session.GlobalVariables.FriendlyDiplomacyThreshold && !this.IsFriendly(opposite) &&
                        Session.Current.Scenario.IsPlayer(opposite))
                    {
                        i.Relation -= 15; //focus到玩家的时候，每月降低15点友好度
                    }
                }
            }

            if ((this.Leader.StrategyTendency != PersonStrategyTendency.维持现状))
            {
                // Break Relations
                FactionList nonFriendlyFactions = new FactionList();
                foreach (Faction f in Session.Current.Scenario.Factions)
                {
                    if (this.adjacentTo(f) && Session.Current.Scenario.GetDiplomaticRelation(this.ID, f.ID) < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                    {
                        nonFriendlyFactions.Add(f);
                    }
                }

                FactionList nearbyFactions = this.GetAdjecentFactions();
                Faction toBreak = null;
                int power = int.MaxValue;
                int totalPower = 0;
                foreach (Faction f in nearbyFactions)
                {
                    DiplomaticRelation rel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(this.ID, f.ID);
                    if (rel.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                    {
                        totalPower += f.Power;
                    }
                }
                foreach (Faction f in nearbyFactions)
                {
                    DiplomaticRelation rel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(this.ID, f.ID);
                    if (rel.Truce <= 0)
                    {
                        int unAmbition = 4 - this.Leader.Ambition;
                        if (nonFriendlyFactions.Count == 0 && f.Power < this.Power)
                        {
                            power = f.Power;
                            toBreak = f;
                        }
                        else if (this.Power > totalPower * ((unAmbition * unAmbition + (this.Leader.Calmness - this.Leader.Braveness) / 4) * 0.2 + 0.6) &&
                            rel.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold)
                        {
                            float ratio = (float)this.Power / f.Power;
                            if (GameObject.Chance((int)((ratio - 1) * this.Leader.Ambition * 10)))
                            {
                                power = f.Power;
                                toBreak = f;
                            }
                        }
                    }
                }

                if (toBreak != null)
                {
                    Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(this.ID, toBreak.ID).Relation = 0;

                    this.Leader.DecreaseKarma(5);

                    //AI宣布主动解盟
                    Session.MainGame.mainGameScreen.xianshishijiantupian(toBreak.Leader, this.Leader?.Name ?? "", TextMessageKind.ResetDiplomaticRelation, "ResetDiplomaticRelation", "ResetDiplomaticRelation.jpg", "ResetDiplomaticRelation", toBreak?.LeaderName ?? "", true);
                }

                // Randomly alter relations
                foreach (DiplomaticRelation rel in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    Faction opposite = rel.GetDiplomaticFaction(this.ID);
                    if (opposite != null)
                    {
                        if (rel.Relation > 300) continue;
                        if (rel.Truce > 0)
                        {
                            rel.Relation += 5;
                        }
                        rel.Relation += (int)(Person.GetIdealAttraction(opposite.Leader, this.Leader) * (GameObject.Random(100) / 1000.0f + 0.1f) / (Math.Abs(rel.Relation) / 10.0f + 1));
                    }
                }

                /*
                int minTroop = int.MaxValue;
                DiplomaticRelation minTroopFactionRelation = null;
                Faction minTroopFactionopposite = null;

                foreach (DiplomaticRelation i in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    Faction opposite = i.GetDiplomaticFaction(this.ID);
                    //if (i.Relation < 300) continue; 
                    if (!this.adjacentTo(opposite)) continue;    //不接壤的AI不主动改变关系值
                    if (GameObject.Chance((int)((double)this.armyScale / opposite.ArmyScale * ((int)this.Leader.Ambition + 1) * 20))
                        && i.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                    {
                        i.Relation -= (7 + (int)Random(15)); //根据总兵力情况每月随机减少
                        i.Relation -= (Person.GetIdealOffset(this.Leader, opposite.Leader)) / 10;
                        relationBroken = true;
                        break;
                    }
                    //增加关系300以上，随机一个降低数值后主动解盟的情况
                    if (GameObject.Chance((int)(Person.GetIdealOffset(this.Leader, opposite.Leader) / 3)) && i.Relation >= 300)
                    {
                        i.Relation -= (7 + (int)Random(15));
                        i.Relation -= (Person.GetIdealOffset(this.Leader, opposite.Leader)) / 10;
                        relationBroken = true;
                        if (i.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold)
                        {
                            //显示联盟破裂画面
                            Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader?.Name ?? "", TextMessageKind.BreakDiplomaticRelation, "BreakDiplomaticRelation", "BreakDiplomaticRelation.jpg", "BreakDiplomaticRelation", opposite?.Leader?.Name ?? "", true);
                        }
                        break;
                    }

                    if (!this.hasNonFriendlyFrontline)
                    {
                        if (opposite.ArmyScale < minTroop)
                        {
                            minTroop = opposite.ArmyScale;
                            minTroopFactionRelation = i;
                            minTroopFactionopposite = i.GetDiplomaticFaction(this.ID);
                        }
                    }
                }
                if (minTroopFactionRelation != null && !relationBroken)
                {
                    minTroopFactionRelation.Relation = 0;
                    //AI宣布主动解盟
                    Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader?.Name ?? "", TextMessageKind.ResetDiplomaticRelation, "ResetDiplomaticRelation", "ResetDiplomaticRelation.jpg", "ResetDiplomaticRelation", minTroopFactionopposite?.LeaderName ?? "", true);
                }
                */
            }
        }

        public bool RoutewayPathAvail(Point start, Point end, bool hasEnd)
        {
            bool result = this.RoutewayPathBuilder.GetPath(start, end, hasEnd);
            return result;
        }

        private int RoutewayPathBuilder_OnGetCost(Point position, out float consumptionRate)
        {
            consumptionRate = 0f;
            if (!Session.Current.Scenario.PositionOutOfRange(position))
            {
                if (Session.Current.Scenario.GetArchitectureByPositionNoCheck(position) != null)
                {
                    return 0x3e8;
                }
                if (this.RoutewayPathBuilder.MultipleWaterCost && !Session.Current.Scenario.IsWaterPositionRoutewayable(position))
                {
                    return 0x3e8;
                }
                TerrainDetail terrainDetailByPositionNoCheck = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(position);
                if (terrainDetailByPositionNoCheck != null)
                {
                    consumptionRate = terrainDetailByPositionNoCheck.RoutewayConsumptionRate;
                    int routewayBuildWorkCost = terrainDetailByPositionNoCheck.RoutewayBuildWorkCost;
                    int routewayWorkForce = this.RoutewayWorkForce;
                    if (this.RoutewayPathBuilder.MultipleWaterCost && (terrainDetailByPositionNoCheck.ID == 6))
                    {
                        routewayBuildWorkCost = this.RoutewayWorkForce;
                    }
                    return ((routewayBuildWorkCost <= routewayWorkForce) ? routewayBuildWorkCost : 0x3e8);
                }
            }
            return 0x3e8;
        }

        private int RoutewayPathBuilder_OnGetPenalizedCost(Point position)
        {
            foreach (Architecture architecture in Session.Current.Scenario.GetHighViewingArchitecturesByPosition(position))
            {
                if (!((architecture.BelongedFaction == null) || this.IsFriendly(architecture.BelongedFaction)))
                {
                    return (this.RoutewayWorkForce / 2);
                }
            }
            return 0;
        }

        // 🔥 静态计数器用于检测回合卡住
        private static int _threadingWaitCount = 0;
        private static int _lastFactionId = -1;
        private const int MAX_THREADING_WAIT = 100; // 最多等待100次循环

        public bool Run()
        {
            // 🔥 强制诊断输出


            if (!this.preUserControlFinished)
            {
                this.Develop();
                this.preUserControlFinished = true;
            }

            // =========================================================
            // 🎯 性能优化：定期清理兵役人口缓存
            // =========================================================
            if (GameObject.Random(100) == 0) // 1%概率执行清理，避免每回合都清理
            {
                CleanupMilitaryPopulationCache();
            }

            // =========================================================
            // 第一步：先跑下属的"分公司" (军区/军团 AI)
            // 🔥 关键修复：只有玩家势力才执行军区AI，AI势力跳过军区逻辑
            // =========================================================
            if (Session.Current.Scenario.IsPlayer(this))
            {
                // 只有玩家势力才执行委任军区AI
                foreach (Section section in this.Sections)
                {
                    // 只有开启了"自动运行"且"不手动"的军区才跑
                    // 注意：这里是军区AI的入口，不涉及外交等大战略
                    if (section.AIDetail != null && section.AIDetail.AutoRun)
                    {
                        // 这里调用 Section.AI()
                        // 在 Section.AI() 内部，去调用 RunPersonnel/RunMilitary
                        section.AI(new GameTime());
                    }
                }
            }
            else
            {
                // AI势力：跳过军区AI，直接使用势力级AI
                if (SectionAIHelper.EnableDebugOutput)
                {

                }
            }

            // =========================================================
            // 第二步：再判断"总公司" (势力 AI)
            // 逻辑：如果是玩家，到此为止，剩下的交给UI；如果是电脑，继续跑大战略
            // =========================================================
            if (Session.Current.Scenario.IsPlayer(this))
            {
                // 玩家势力本体不跑AI，交还控制权给UI
                // 注意：委任军区的AI已经在上面执行过了

                // [强制通行证]
                // 只要满足任一条件，直接告诉 AI 系统：我完事了，别卡我。
                // 1. Passed = true (玩家点了按钮)
                // 2. playing = true (插件正在跑天数)
                bool isPlaying = (Session.MainGame.mainGameScreen != null &&
                    Session.MainGame.mainGameScreen.Plugins.DateRunnerPlugin.IsPlaying);

                // 🔍 诊断日志
                // System.Diagnostics.Debug.WriteLine($"[Faction.Run] 玩家检查: {this.Name} Passed={this.Passed} IsPlaying={isPlaying} Controlling={this.Controlling} AIFinished={this.AIFinished}");

                if (this.Passed || isPlaying)
                {
                    this.AIFinished = true;
                    // System.Diagnostics.Debug.WriteLine($"[Faction.Run] {this.Name} 返回 TRUE (Passed={this.Passed} || IsPlaying={isPlaying})");
                    return true;
                }

                // 🔥 修复：玩家势力不执行后续的 AI 逻辑（外交、科技、内政等）
                // 直接进入玩家控制流程
                // 注意：不要在这里 return，继续执行下面的玩家控制逻辑
            }
            if (this.Controlling || this.Passed)
            {
                // 🔥 关键修复：当玩家控制时也需要标记AI完成
                if (!this.AIFinished)
                {
                    this.AIFinished = true;
                }

                // 🔥 关键：每次都检查Threading状态，确保不会卡住
                if (Session.Current?.Scenario?.Factions != null && Session.Current.Scenario.Threading)
                {
                    bool allAIFinished = true;
                    string waitingFor = "";
                    foreach (var obj in Session.Current.Scenario.Factions.GetList())
                    {
                        if (obj is Faction faction && faction.IsAlive && !faction.AIFinished)
                        {
                            allAIFinished = false;
                            waitingFor = faction.Name;
                            break;
                        }
                    }

                    if (allAIFinished)
                    {
                        // System.Diagnostics.Debug.WriteLine("[Faction.Run] 控制中：所有势力AI完成，重置Threading状态");
                        Session.Current.Scenario.Threading = false;
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"[Faction.Run] 控制中：等待势力 {waitingFor}");
                    }
                }

                bool returnValue = this.Passed;
                // System.Diagnostics.Debug.WriteLine($"[Faction.Run] {this.Name} 控制分支返回: {returnValue} (Controlling={this.Controlling}, Passed={this.Passed})");
                return returnValue;
            }
            if (Session.Current.Scenario.IsPlayer(this))
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Faction.Run] 玩家势力 {this.Name}: WantControl={this.WantControl}, Controlling={this.Controlling}, Passed={this.Passed}, AIFinished={this.AIFinished}");
                #endif
                
                if (this.WantControl)
                {
                    // System.Diagnostics.Debug.WriteLine($"[Faction.Run] 玩家势力 {this.Name} 进入WantControl分支");
                    if (this.Passed)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[Faction.Run] 玩家势力 {this.Name} 已Passed，返回true");
                        return true;
                    }

                    // 🔥 CRITICAL FIX: 玩家想要控制权时，强制重置Threading
                    if (Session.Current.Scenario.Threading)
                    {

                        Session.Current.Scenario.Threading = false;
                    }

                    if (!Session.Current.Scenario.Threading)
                    {

                        if (!this.AIFinished)
                        {
                            if (SectionAIHelper.EnableDebugOutput)
                            {
                                // System.Diagnostics.Debug.WriteLine("[Faction.Run] 玩家势力执行PlayerAI()");
                            }
                            this.PlayerAI();
                            return false;
                        }
                        if (!this.Controlling)
                        {
                            this.Controlling = true;
                            
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[Faction.Run] ✅ 玩家势力 {this.Name} 获得控制权 (WantControl分支)");
                            #endif
                            
                            if (this.OnGetControl != null)
                            {

                                this.OnGetControl(this);
                            }
                            else
                            {

                            }
                        }
                        return false;
                    }

                    return false;
                }

                // 🔥 关键修复：如果 WantControl = false，也要给玩家控制权
                // 日期：2026-03-19
                // 原因：点击"进行"后 WantControl 可能被重置，导致玩家无法获得控制权
                if (!Session.Current.Scenario.Threading)
                {
                    if (!this.AIFinished)
                    {
                        if (SectionAIHelper.EnableDebugOutput)
                        {
                            // System.Diagnostics.Debug.WriteLine("[Faction.Run] 玩家势力执行PlayerAI()");
                        }
                        this.PlayerAI();
                        return false;
                    }
                    
                    // 🔥 修复：AIFinished 后应该给玩家控制权，而不是直接 Passed
                    if (!this.Controlling)
                    {
                        this.Controlling = true;
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[Faction.Run] ✅ 玩家势力 {this.Name} 获得控制权 (非WantControl分支)");
                        #endif
                        
                        if (this.OnGetControl != null)
                        {
                            this.OnGetControl(this);
                        }
                        
                        return false; // 返回 false，等待玩家操作
                    }
                    
                    // 如果已经 Controlling，返回 Passed 状态
                    return this.Passed;
                }

                // 🔥 CRITICAL FIX: 死锁检测 - 如果Threading=true但玩家势力AIFinished=false，强制重置
                if (Session.Current.Scenario.Threading && !this.AIFinished)
                {

                    Session.Current.Scenario.Threading = false;
                    this.AIFinished = true;
                    this.Controlling = true;

                    if (this.OnGetControl != null)
                    {
                        this.OnGetControl(this);
                    }
                    else
                    {

                    }
                    return false;
                }


                return false;
            }

            // =========================================================
            // 第三步：电脑势力的独有逻辑 (外交、科技等)
            // =========================================================
            // 🔥 修复：确保只有 AI 势力才执行这部分逻辑
            if (Session.Current.Scenario.IsPlayer(this))
            {


                
                // 强制标记为完成并返回
                this.AIFinished = true;
                this.Passed = true;
                return true;
            }
            
            // 🔥 AI势力调试输出
            // System.Diagnostics.Debug.WriteLine($"[Faction.Run] AI势力 {this.Name}: AIFinished={this.AIFinished}, Passed={this.Passed}, IsAlive={this.IsAlive}");

            if (SectionAIHelper.EnableDebugOutput)
            {

            }

            #if DEBUG
            // 🔥 诊断：详细记录 IsPlayer 检查结果
            bool isPlayerFaction = Session.Current?.Scenario?.IsPlayer(this) ?? false;

            
            if (Session.Current?.Scenario?.PlayerFactions != null)
            {
                var playerFactionNames = new List<string>();
                foreach (GameObject obj in Session.Current.Scenario.PlayerFactions.GameObjects)
                {
                    if (obj is Faction f)
                    {
                        playerFactionNames.Add($"{f.Name}(ID:{f.ID})");
                    }
                }

            }
            #endif
            
            // 🔥 关键修复：只有AI势力才执行势力级AI逻辑
            if (!Session.Current.Scenario.IsPlayer(this) && !this.AIFinished)
            {
                if (SectionAIHelper.EnableDebugOutput)
                {

                }
                // System.Diagnostics.Debug.WriteLine($"[Faction.Run] AI势力 {this.Name} 开始执行势力级AI");

                // --- 外交 (极低频) ---
                // 只有特定日期才跑外交，减少CPU消耗
                if (Session.Current?.Scenario?.Date != null)
                {
                    var currentDate = Session.Current.Scenario.Date;
                    // 每季度第一天执行外交：1月1日, 4月1日, 7月1日, 10月1日
                    if ((currentDate.Month == 1 || currentDate.Month == 4 ||
                         currentDate.Month == 7 || currentDate.Month == 10) &&
                        currentDate.Day == 1)
                    {
                        this.AIDiplomacy();
                    }
                }
                else
                {
                    // 如果日期系统不可用，使用原有的计时器逻辑
                    this.AIDiplomacy();
                }

                // 只有电脑才会自动升级科技
                this.AITechniques();

                // 势力级独有操作：
                this.AICapital();           // 迁都
                this.AICaptives();          // 俘虏处理
                this.AINvGuan();            // 女官管理
                this.AIMakeMarriage();      // 联姻
                this.AISelectPrince();      // 选择继承人
                this.AIZhaoXian();          // 招贤
                this.AIZhaoXian();          // 招贤（双重调用保持原样）
                this.AIAppointAdvisor();    // 任命军师
                this.AIHouGong();           // 后宫管理

                // 全局管理操作：
                this.ManageLogistics();     // 全局物流
                this.AICoordinatedAttacks(); // 协调攻击
                this.AILegions();           // 军团管理
                this.AITrainChildren();     // 培养子女

                // 只有电脑才会在没分军区的地方自己瞎折腾
                // 注意：如果电脑也有军区，RunPersonnel/RunDomestic/RunMilitary会自动过滤已托管的城市
                // --- 人事/内政 (低频) ---
                // 之前的 60 天逻辑已在各自方法内实现
                this.RunPersonnel(null);    // 全势力人员调配（跳过托管军区）
                this.RunDomestic(null);     // 全势力内政（跳过托管军区）
                // --- 军事 (动态频率) ---
                // 结合"动静结合"逻辑，战时高频，和平时低频
                this.RunMilitary(null);     // 全势力军事（跳过托管军区）
                this.AutoAppointMayor(null); // 全势力太守任命（跳过托管军区）

                this.AIFinished = true;
                // System.Diagnostics.Debug.WriteLine($"[Faction.Run] AI势力 {this.Name} 势力级AI执行完成");
                return false;
            }
            this.Passed = true;
            if (SectionAIHelper.EnableDebugOutput)
            {

            }
            // System.Diagnostics.Debug.WriteLine($"[Faction.Run] AI势力 {this.Name} 已完成，返回true");
            return true;
        }


        public string SaveLegionsToString()
        {
            string str = "";
            foreach (Legion legion in this.Legions)
            {
                str = str + legion.ID.ToString() + " ";
            }
            return str;
        }

        public string SaveSectionsToString()
        {
            string str = "";
            foreach (Section section in this.Sections)
            {
                str = str + section.ID.ToString() + " ";
            }
            return str;
        }

        public void SaveTechniquePointForFacility(int credit)
        {
            if (credit > this.techniquePoint)
            {
                credit = this.techniquePoint;
            }
            this.techniquePoint -= credit;
            this.techniquePointForFacility += credit;
        }

        public void SaveTechniquePointForTechnique(int credit)
        {
            if (credit > this.techniquePoint)
            {
                credit = this.techniquePoint;
            }
            this.techniquePoint -= credit;
            this.techniquePointForTechnique += credit;
        }

        public string SaveTroopsToString()
        {
            string str = "";
            foreach (Troop troop in this.Troops)
            {
                str = str + troop.ID.ToString() + " ";
            }
            return str;
        }

        public void SeasonEvent()
        {
            this.RefrehCreatePersonTimes();
            this.shizheshengguan();
        }

        private void RefrehCreatePersonTimes()
        {
            if (Session.Current.Scenario.Date.Day <= Session.Current.Scenario.Parameters.DayInTurn)
            {
                this.ZhaoxianFailureCount = 0;
            }
        }

        public bool BecomeEmperorLegallyAvail()  //可以禅位
        {
            if (this.IsAlien || !Session.Current.Scenario.youhuangdi())
            {
                return false;
            }
            if (this.guanjue != Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 2)  //不是王
            {
                return false;
            }
            if (this.Capital.Fund < 100000)
            {
                return false;
            }
            guanjuezhongleilei shengjiguanjue = new guanjuezhongleilei();
            shengjiguanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1);
            if (this.HasEmperor() && this.chaotinggongxiandu >= shengjiguanjue.xuyaogongxiandu && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
            {
                return true;
            }
            return false;
        }


        public bool SelfBecomeEmperorAvail()  //可以称帝
        {
            if (this.guanjue != Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 2)  //不是王
            {
                return false;
            }
            if (this.Capital.Fund < 100000)
            {
                return false;
            }
            guanjuezhongleilei shengjiguanjue = new guanjuezhongleilei();
            shengjiguanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1);

            if (this.IsAlien)
            {
                if (this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (Session.Current.Scenario.youhuangdi())
                {
                    if (!this.HasEmperor() && this.chaotinggongxiandu >= shengjiguanjue.xuyaogongxiandu && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    if (this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
        }

        private void AIBecomeEmperor()
        {
            if (this.guanjue != Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 2)  //不是王
            {
                return;
            }
            if (this.Capital == null || this.Capital.Fund < 100000)
            {
                return;
            }
            guanjuezhongleilei shengjiguanjue = new guanjuezhongleilei();
            shengjiguanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1);
            if (Session.Current.Scenario.youhuangdi())
            {
                if (this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfBecomeEmperor();
                }
                else if (!this.IsAlien && this.chaotinggongxiandu >= shengjiguanjue.xuyaogongxiandu && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    if (this.HasEmperor())
                    {
                        foreach (Faction f in Session.Current.Scenario.Factions.GameObjects)
                        {
                            if (f == this) continue;
                            if (f.guanjue == Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 1) continue;
                            if (GameObject.Random((int)(((int)this.Leader.Ambition + 1) * 2 * (this.ArchitectureCount / (double)f.ArchitectureCount))) == 0)
                            {
                                return;
                            }
                        }
                        this.BecomeEmperorLegally();
                    }
                    else if (this.Leader.ValuationOnGovernment == PersonValuationOnGovernment.无视 ||
                        (this.Leader.ValuationOnGovernment == PersonValuationOnGovernment.普通 && GameObject.Chance(5)))
                    {
                        /*Faction owningEmperor = null;
                        foreach (Faction f in Session.Current.Scenario.Factions.GameObjects)
                        {
                            if (f.HasEmperor())
                            {
                                owningEmperor = f;
                                break;
                            }
                        }*/
                        if (GameObject.Random((int)((5 - (int)this.Leader.Ambition) * 100 / (double)this.ArchitectureCount)) == 0)
                        {
                            this.SelfBecomeEmperor();
                        }
                    }
                }
            }
            else
            {
                if (this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfBecomeEmperor();
                }
                else if (!this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfBecomeEmperor();
                }
            }

        }

        public void BecomeEmperorLegally()
        {
            this.guanjue++;
            Session.Current.Scenario.YearTable.addBecomeEmperorLegallyEntry(Session.Current.Scenario.Date, (Session.Current.Scenario.Persons.GetGameObject(7000) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(7000) : null), this);
            var guanjueInfo = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
            Session.MainGame.mainGameScreen.xianshishijiantupian((Session.Current.Scenario.Persons.GetGameObject(7000) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(7000) : null), this.LeaderName, TextMessageKind.BecomeEmperorLegally, "BecomeEmperorLegally", "shanwei.jpg", "",
                guanjueInfo?.Name ?? "", true);
            Session.MainGame.mainGameScreen.xiejinxingjilu(this.Leader, "BecomeEmperorLegally", this.LeaderName,
                guanjueInfo?.Name ?? "", this.Leader?.Position ?? Microsoft.Xna.Framework.Point.Zero);
            this.Capital.DecreaseFund(100000);

            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseBecomeEmperorLegally(Session.Current.Scenario, this);
            Session.Current.Scenario.BecomeNoEmperor();
        }

        public bool HasEmperor()
        {
            foreach (Architecture a in this.Architectures)
            {
                if (a.huangdisuozai) return true;
            }
            return false;
        }



        private void shizheshengguan()
        {
            if (this.guanjue >= Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 2)  //已经是王或者皇帝
            {
                return;
            }
            guanjuezhongleilei shengjiguanjue = new guanjuezhongleilei();
            shengjiguanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1);
            if (Session.Current.Scenario.youhuangdi())
            {
                if (this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfAdvancement();
                }
                else if (!this.IsAlien && this.chaotinggongxiandu >= shengjiguanjue.xuyaogongxiandu && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.Advancement();
                }
            }
            else
            {
                if (this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfAdvancement();
                }
                else if (!this.IsAlien && this.chengchigeshu() >= shengjiguanjue.xuyaochengchi)
                {
                    this.SelfAdvancement();
                }
            }



        }

        public void SelfBecomeEmperor()
        {
            this.guanjue++;
            Session.Current.Scenario.YearTable.addSelfBecomeEmperorEntry(Session.Current.Scenario.Date, this);
            var guanjueInfo2 = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
            
            if (this.Leader == null)
            {
                throw new InvalidOperationException($"数据损坏：势力 {this.Name} 没有君主");
            }
            
            if (this.Leader.ID == 7200)
            {
                throw new InvalidOperationException($"数据损坏：势力 {this.Name} 的君主是传令官（ID=7200），应该是 {this.LeaderName}");
            }
            
            Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.LeaderName, TextMessageKind.BecomeEmperorIllegally, "Zili", "BecomeEmperor.jpg", "",
                guanjueInfo2?.Name ?? "", true);
            Session.MainGame.mainGameScreen.xiejinxingjilu(this.Leader, "Zili", this.LeaderName,
                guanjueInfo2?.Name ?? "", this.Leader?.Position ?? Microsoft.Xna.Framework.Point.Zero);
            this.Capital.DecreaseFund(100000);
            if (!Session.Current.Scenario.youhuangdi() || this.IsAlien)
            {
                return;
            }
            else
            {
                this.DoSelfBecomeEmperorInfluence();
            }
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseSelfBecomeEmperor(Session.Current.Scenario, this);
        }

        private void DoSelfBecomeEmperorInfluence()
        {
            foreach (Architecture a in this.Architectures)
            {
                a.Morale = (int)(a.Morale * 0.9f);
            }
#pragma warning disable CS0168 // The variable 'loyaltyMultiplier' is declared but never used
            float loyaltyMultiplier;
#pragma warning restore CS0168 // The variable 'loyaltyMultiplier' is declared but never used
            foreach (Person person in this.Persons)
            {
                if (person == this.Leader)
                {
                    continue;
                }

                switch (person.ValuationOnGovernment)
                {
                    case PersonValuationOnGovernment.无视:
                        break;
                    case PersonValuationOnGovernment.普通:
                        person.TempLoyaltyChange = -10;
                        break;
                    case PersonValuationOnGovernment.重视:
                        person.TempLoyaltyChange = -60;
                        break;
                    default:
                        continue;

                }
            }
            Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, "", TextMessageKind.SelfBecomeInfluenceConsequence, "SelfBecomeEmperorInfluence", "", "", true);
        }


        private void Advancement()
        {
            this.guanjue++;

            guanjuezhongleilei gj = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);

            bool shouldShowCurrentPlayerDialog = Session.Current.Scenario.CurrentPlayer != null &&
                Session.Current.Scenario.CurrentPlayer == this;

            if (gj != null && shouldShowCurrentPlayerDialog)
            {
                // ANTI-BAND-AID：势力晋升事件必须有有效君主
                if (this.Leader == null)
                {
                    throw new InvalidOperationException($"数据损坏：势力 {this.Name} 没有君主");
                }

                // 统一由君主本人出面，避免显示“传令官”标题与头像
                Session.MainGame.mainGameScreen.xianshishijiantupian(
                    this.Leader,
                    gj.Name,
                    "LeaderAdvancement",
                    "shengguan.jpg",
                    "",
                    true);
            }

            // 保留原逻辑：所有势力都继续写入简报和年表
            if (gj != null)
            {
                if (this.Leader == null)
                {
                    throw new InvalidOperationException($"数据损坏：势力 {this.Name} 没有君主");
                }

                Session.MainGame.mainGameScreen.xiejinxingjilu(this.Leader, "shengguan", this.LeaderName,
                    gj.Name, this.Leader.Position);
                Session.Current.Scenario.YearTable.addAdvanceGuanjueEntry(Session.Current.Scenario.Date, this, gj);
            }

            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseAdvancement(Session.Current.Scenario, this);
        }

        private void SelfAdvancement()
        {
            this.guanjue++;

            guanjuezhongleilei gj2 = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
            
            // 🔥 关键修复：显示君主本人的头像和对话框，而不是传令官
            // 日期：2026-03-21
            // 原因：自封官爵是君主自己的决定，应该由君主本人宣布
            // 修复：移除 IsPlayer 检查，改为检查 CurrentPlayer 是否等于当前势力
            if (gj2 != null && Session.Current.Scenario.CurrentPlayer != null && Session.Current.Scenario.CurrentPlayer == this)
            {
                // ANTI-BAND-AID：势力必须有君主，否则数据损坏
                if (this.Leader == null)
                {
                    throw new InvalidOperationException($"数据损坏：势力 {this.Name} 没有君主");
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SelfAdvancement] 势力={this.Name}, 君主={this.Leader.Name}(ID:{this.Leader.ID}), 官爵={gj2.Name}");
                #endif
                
                Session.MainGame.mainGameScreen.xianshishijiantupian(
                    this.Leader,                        // 使用君主本人
                    gj2.Name,                           // 官爵名称（TextResultString）
                    "Zili",                             // 分支名
                    "shengguan.jpg",                    // 图片
                    "",                                 // 音效
                    true);                              // 总是显示
            }
            
            // 🔥 修复：所有势力都记录简报，让玩家通过简报查看 AI 势力的自封
            if (gj2 != null)
            {
                // ANTI-BAND-AID：势力必须有君主，否则数据损坏
                if (this.Leader == null)
                {
                    throw new InvalidOperationException($"数据损坏：势力 {this.Name} 没有君主");
                }
                
                Session.MainGame.mainGameScreen.xiejinxingjilu(this.Leader, "Zili", this.LeaderName,
                    gj2.Name, this.Leader.Position);
                Session.Current.Scenario.YearTable.addSelfAdvanceGuanjueEntry(Session.Current.Scenario.Date, this, gj2);
            }
            
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseSelfAdvancement(Session.Current.Scenario, this);
        }

        public int CityCount
        {
            get
            {
                return chengchigeshu();
            }
        }

        public int chengchigeshu()
        {
            int geshu = 0;
            foreach (Architecture a in this.Architectures)
            {
                if (a.Kind != null && a.Kind.CountToMerit)
                {
                    geshu++;
                }
            }
            return geshu;
        }

        public string guanjuezifuchuan
        {
            get
            {
                var guanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
                return guanjue?.Name ?? "";
            }
        }

        public int shengwangshangxian
        {
            get
            {
                var guanjue = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
                return guanjue?.shengwangshangxian ?? 0;
            }

        }
        public int shengguanxuyaogongxiandu
        {
            get
            {
                if (this.IsAlien || !Session.Current.Scenario.youhuangdi())
                {
                    return 0;
                }
                else
                {
                    if (this.guanjue >= Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 1)
                    {
                        return 0;
                    }
                    else
                    {
                        return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1)?.xuyaogongxiandu ?? 0;

                    }
                }
            }

        }
        public int shengguanxuyaochengchi
        {
            get
            {
                if (this.guanjue >= Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Count - 1)
                {
                    return 0;
                }
                else
                {
                    return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1)?.xuyaochengchi ?? 0;

                }
            }

        }

        public Architecture SelectNewCapital()
        {
            int population = 0;
            Architecture architecture = null;
            foreach (Architecture architecture2 in this.Architectures)
            {
                if ((architecture2 != this.Capital) && (architecture2.Population >= population))
                {
                    architecture = architecture2;
                    population = architecture2.Population;
                }
            }
            return architecture;
        }

        private void SetSectionAIDetail()
        {
            foreach (Section s in this.Sections)
            {
                GameObjectList candidates = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, false);
                if (candidates.Count > 0)
                {
                    s.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetail(candidates[GameObject.Random(candidates.Count)].ID);
                }
            }
        }

        private void SetSectionAIDetail_OLD()
        {
            Faction faction;
            int num2;
            int threat;
            GameObjectList sectionNoOrientationAutoAIDetailsByConditions;
            int num5;
            GameObjectList list5;
            if (this.SectionCount <= 1)
            {
                num5 = ((this.FirstSection.ArchitectureScale / 2) - (this.FirstSection.ArchitectureCount / 2)) + 1;
                if (this.ArmyScale < (num5 * 6))
                {
                    if (!this.Capital.IsOK())
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, false, false, false);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    else
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(false, true);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    return;
                }
                if (this.ArmyScale < (num5 * 12))
                {
                    if (!this.Capital.IsOK())
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, false, false, false);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    else
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(false, true);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    return;
                }
                if (this.ArmyScale < (num5 * 20))
                {
                    if (!this.Capital.IsOK())
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, false, false, false);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    else
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, true);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                    }
                    return;
                }
                if (this.ArmyScale >= (num5 * 30))
                {
                    if (!this.Capital.IsGood())
                    {
                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, false);
                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                        {
                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                        }
                        return;
                    }
                    faction = null;
                    num2 = -2147483648;
                    foreach (GameObject obj in Session.Current.Scenario.Factions.GetRandomList())
                    {
                        Faction faction2 = (obj is Faction ? (Faction)obj : null);
                        if (faction2 == null) continue;
                        
                        if (((faction2 != this) && !this.IsFriendly(faction2)) && (faction2.Capital != null))
                        {
                            threat = this.GetThreat(faction2);
                            if ((threat > num2) || GameObject.Chance(20))
                            {
                                num2 = threat;
                                faction = faction2;
                            }
                        }
                    }
                    if ((faction != null) && (this.FirstSection.OrientationFaction != faction))
                    {
                        foreach (Architecture architecture in this.Architectures)
                        {
                            if (architecture.HasFactionInClose(faction, 1))
                            {
                                sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.势力, true, true, true, false, true);
                                if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                {
                                    this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                    this.FirstSection.OrientationFaction = faction;
                                }
                                break;
                            }
                        }
                    }
                    list5 = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
                    if (list5.Count > 0)
                    {
                        this.FirstSection.AIDetail = list5[GameObject.Random(list5.Count)] as SectionAIDetail;
                        if (this.Capital.LocationState.GetFactionScale(this) < 100)
                        {
                            this.FirstSection.OrientationState = this.Capital.LocationState;
                        }
                        else
                        {
                            this.FirstSection.OrientationState = this.Capital.LocationState.ContactStates[GameObject.Random(this.Capital.LocationState.ContactStates.Count)] as State;
                        }
                    }
                    return;
                }
                if (!this.Capital.IsGood())
                {
                    sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, false);
                    if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                    {
                        this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                    }
                    return;
                }
                faction = null;
                num2 = -2147483648;
                foreach (GameObject obj in this.GetUnderZeroDiplomaticRelationFactions().GetRandomList())
                {
                    Faction faction2 = (obj is Faction ? (Faction)obj : null);
                    if (faction2 == null) continue;
                    
                    threat = this.GetThreat(faction2);
                    if ((threat > num2) || GameObject.Chance(20))
                    {
                        num2 = threat;
                        faction = faction2;
                    }
                }
                if ((faction != null) && (this.FirstSection.OrientationFaction != faction))
                {
                    foreach (Architecture architecture in this.Architectures)
                    {
                        if (architecture.HasFactionInClose(faction, 1))
                        {
                            sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.势力, true, true, true, false, true);
                            if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                            {
                                this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                this.FirstSection.OrientationFaction = faction;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                int num = (this.ArmyScale / 60) - (int)this.Leader.StrategyTendency;
                if (num <= 0)
                {
                    foreach (Section section in this.Sections)
                    {
                        if (section.AIDetail.OrientationKind == SectionOrientationKind.无)
                        {
                            Section section2;
                            num5 = this.FirstSection.ArchitectureScale - (this.FirstSection.ArchitectureCount / 2);
                            if (section.GetHostileScale() > 0)
                            {
                                if (section.ArmyScale > (num5 * 6))
                                {
                                    foreach (Architecture architecture in section.Architectures)
                                    {
                                        section2 = null;
                                        if (architecture.HasOffensiveSectionInClose(out section2, 1))
                                        {
                                            sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.军区, true, false, false, true, true);
                                            if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                            {
                                                section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                                section.OrientationSection = section2;
                                            }
                                            break;
                                        }
                                    }
                                    if (section.OrientationSection == null)
                                    {
                                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, true);
                                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                        {
                                            this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                        }
                                    }
                                }
                                else
                                {
                                    sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(false, true);
                                    if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                    {
                                        this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                    }
                                }
                            }
                            else if ((section.GetFrontScale() > 0) && (section.ArmyScale < (num5 * 6)))
                            {
                                sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(true, false);
                                if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                {
                                    this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                }
                            }
                            else
                            {
                                foreach (Architecture architecture in section.Architectures)
                                {
                                    section2 = null;
                                    if (architecture.HasOffensiveSectionInClose(out section2, 1))
                                    {
                                        sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.军区, true, false, false, false, false);
                                        if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                        {
                                            section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                            section.OrientationSection = section2;
                                        }
                                        break;
                                    }
                                }
                                if (section.OrientationSection == null)
                                {
                                    sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionNoOrientationAutoAIDetailsByConditions(false, false);
                                    if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                    {
                                        this.FirstSection.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
                faction = null;
                num2 = -2147483648;
                int num3 = 0;
                foreach (GameObject obj in this.GetUnderZeroDiplomaticRelationFactions().GetRandomList())
                {
                    Faction faction2 = (obj is Faction ? (Faction)obj : null);
                    if (faction2 == null) continue;
                    
                    threat = this.GetThreat(faction2);
                    if ((threat > num2) || GameObject.Chance(20))
                    {
                        num2 = threat;
                        faction = faction2;
                    }
                }
                GameObjectList list = this.Sections.GetList();
                list.PropertyName = "ArmyScale";
                list.IsNumber = true;
                list.ReSort();
                if (faction != null)
                {
                    foreach (Section section in list)
                    {
                        if (section.ArmyScale < 0x19)
                        {
                            break;
                        }
                        foreach (Architecture architecture in section.Architectures)
                        {
                            if (architecture.HasFactionInClose(faction, 1))
                            {
                                sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.势力, true, true, true, false, true);
                                if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                {
                                    section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                    section.OrientationFaction = faction;
                                    num3++;
                                }
                                break;
                            }
                        }
                        if (num3 >= num)
                        {
                            break;
                        }
                    }
                    return;
                }
                if (this.Capital.LocationState.GetFactionScale(this) < 100)
                {
                    foreach (Section section in list)
                    {
                        if (section.ArmyScale < 0x19)
                        {
                            break;
                        }
                        if (this.Capital.LocationState.GetSectionScale(section) >= 60)
                        {
                            sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
                            if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                            {
                                section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                section.OrientationState = this.Capital.LocationState;
                                num3++;
                            }
                            if (num3 >= num)
                            {
                                break;
                            }
                        }
                    }
                    return;
                }
                if (this.Capital.LocationState.LinkedRegion.GetFactionScale(this) < 100)
                {
                    foreach (Section section in list)
                    {
                        if (section.ArmyScale < 0x19)
                        {
                            break;
                        }
                        if (this.Capital.LocationState.LinkedRegion.GetSectionScale(section) >= 60)
                        {
                            sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
                            if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                            {
                                section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                foreach (Architecture architecture in section.Architectures)
                                {
                                    if (architecture.LocationState.LinkedRegion == this.Capital.LocationState.LinkedRegion)
                                    {
                                        section.OrientationState = architecture.LocationState;
                                        break;
                                    }
                                }
                                num3++;
                            }
                            if (num3 >= num)
                            {
                                break;
                            }
                        }
                    }
                    return;
                }
                StateList list3 = new StateList();
                foreach (State state in this.Capital.LocationState.LinkedRegion.States)
                {
                    foreach (State state2 in state.ContactStates)
                    {
                        if ((state2.LinkedRegion != this.Capital.LocationState.LinkedRegion) && (state2.GetFactionScale(this) < 100))
                        {
                            list3.Add(state2);
                        }
                    }
                }
                if (list3.Count > 0)
                {
                    foreach (Section section in list)
                    {
                        if (section.ArmyScale < 0x19)
                        {
                            break;
                        }
                        if (this.Capital.LocationState.LinkedRegion.GetSectionScale(section) >= 60)
                        {
                            sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
                            if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                            {
                                section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                section.OrientationState = list3[GameObject.Random(list3.Count)] as State;
                                num3++;
                            }
                            if (num3 >= num)
                            {
                                break;
                            }
                        }
                    }
                }
                if (num3 < num)
                {
                    foreach (Section section in list)
                    {
                        if (section.ArmyScale < 0x19)
                        {
                            return;
                        }
                        if (this.Capital.LocationState.LinkedRegion.GetSectionScale(section) <= 0)
                        {
                            Architecture maxPopulationArchitecture = section.MaxPopulationArchitecture;
                            if (maxPopulationArchitecture != null)
                            {
                                StateList list4 = new StateList();
                                foreach (State state3 in maxPopulationArchitecture.LocationState.ContactStates)
                                {
                                    if ((state3.LinkedRegion != this.Capital.LocationState.LinkedRegion) && (state3.GetFactionScale(this) < 100))
                                    {
                                        list4.Add(state3);
                                    }
                                }
                                if (list4.Count > 0)
                                {
                                    sectionNoOrientationAutoAIDetailsByConditions = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
                                    if (sectionNoOrientationAutoAIDetailsByConditions.Count > 0)
                                    {
                                        section.AIDetail = sectionNoOrientationAutoAIDetailsByConditions[GameObject.Random(sectionNoOrientationAutoAIDetailsByConditions.Count)] as SectionAIDetail;
                                        section.OrientationState = list4[GameObject.Random(list4.Count)] as State;
                                        num3++;
                                    }
                                    if (num3 >= num)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                return;
            }
            list5 = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.州域, true, true, true, false, true);
            if (list5.Count > 0)
            {
                this.FirstSection.AIDetail = list5[GameObject.Random(list5.Count)] as SectionAIDetail;
                if (this.Capital.LocationState.GetFactionScale(this) < 100)
                {
                    this.FirstSection.OrientationState = this.Capital.LocationState;
                }
                else
                {
                    this.FirstSection.OrientationState = this.Capital.LocationState.ContactStates[GameObject.Random(this.Capital.LocationState.ContactStates.Count)] as State;
                }
            }
        }

        private void TechniquesDayEvent()
        {
            if (this.UpgradingTechnique >= 0)
            {
                this.UpgradingDaysLeft--;
                if (this.UpgradingDaysLeft <= 0)
                {
                    Technique technique = Session.Current.Scenario.GameCommonData.AllTechniques.GetTechnique(this.UpgradingTechnique);
                    if (technique != null)
                    {
                        this.AvailableTechniques.AddTechnique(technique);
                        Session.Current.Scenario.NewInfluence = true;
                        technique.Influences.ApplyInfluence(this, GameObjects.Influences.Applier.Technique, technique.ID);
                        Session.Current.Scenario.NewInfluence = false;
                        if (this.OnTechniqueFinished != null)
                        {
                            this.OnTechniqueFinished(this, technique);
                        }
                        // 🔥 AOT 重构：使用强类型事件替代反射调用
                        WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseTechniqueUpgradeComplete(Session.Current.Scenario, this, technique);
                        Session.Current.Scenario.YearTable.addFactionTechniqueCompletedEntry(Session.Current.Scenario.Date, this, technique);
                        Session.MainGame.mainGameScreen.TechniqueComplete(this, technique);
                    }
                    this.UpgradingTechnique = -1;
                }
            }
        }

        public override string ToString()
        {
            return base.Name;
        }

        public void UpgradeTechnique(Technique technique, Architecture architecture)
        {
            this.UpgradingTechnique = technique.ID;
            this.UpgradingDaysLeft = getTechniqueActualTime(technique);
            if (this.TechniquePoint < this.getTechniqueActualPointCost(technique))
            {
                this.DepositTechniquePointForTechnique(this.getTechniqueActualPointCost(technique) - this.TechniquePoint);
                if (this.TechniquePoint < this.getTechniqueActualPointCost(technique))
                {
                    this.DepositTechniquePointForFacility(this.getTechniqueActualPointCost(technique) - this.TechniquePoint);
                }
            }
            this.DecreaseTechniquePoint(this.getTechniqueActualPointCost(technique));
            architecture.DecreaseFund(this.getTechniqueActualFundCost(technique));
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.DiplomacyEvents.RaiseUpgradeTechnique(Session.Current.Scenario, this);
            if (this.OnUpgradeTechnique != null)
            {
                this.OnUpgradeTechnique(this, technique, architecture);
            }
        }

        public void YearEvent()
        {
        }

        public int ArchitectureCount
        {
            get
            {
                return this.Architectures.Count;
            }
        }

        public int ArchitectureTotalSize
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.JianzhuGuimo;
                }
                return num;
            }
        }

        public int CityTotalSize
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    if (architecture.Kind.ID != 2 || architecture.Kind.ID != 4)
                    {
                        num += architecture.JianzhuGuimo;
                    }
                }
                return num;
            }
        }

        public long Army
        {
            get
            {
                long num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.ArmyQuantity;
                }
                foreach (Military m in this.TransferingMilitaries)
                {
                    num += m.Quantity;
                }
                foreach (Troop troop in this.Troops)
                {
                    num += troop.Quantity;
                }
                return num;
            }
        }

        public int ArmyScale
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.ArmyScale;
                }
                foreach (Troop troop in this.Troops)
                {
                    if (troop.Army != null)
                    {
                        num += troop.Army.Scales;
                    }
                }
                return num;
            }
        }

        public MilitaryKindTable AvailableMilitaryKinds
        {
            get
            {
                MilitaryKindTable table = new MilitaryKindTable();
                foreach (MilitaryKind kind in this.BaseMilitaryKinds.MilitaryKinds.Values)
                {
                    table.AddMilitaryKind(kind);
                }
                foreach (MilitaryKind kind in this.TechniqueMilitaryKinds.MilitaryKinds.Values)
                {
                    table.AddMilitaryKind(kind);
                }
                return table;
            }
        }

        public Architecture Capital
        {
            get
            {
                if (this.Architectures.Count == 0) return null;
                if (this.CapitalID == -1)
                {
                    this.CapitalID = this.Architectures[GameObject.Random(this.Architectures.Count)].ID;
                }
                if (this.capital == null)
                {
                    this.capital = (Session.Current.Scenario.Architectures.GetGameObject(this.capitalID) is Architecture ? (Architecture)Session.Current.Scenario.Architectures.GetGameObject(this.capitalID) : null);
                }
                return this.capital;
            }
            set
            {
                this.capital = value;
                if (this.capital != null)
                {
                    this.capitalID = this.capital.ID;
                }
                else
                {
                    this.capitalID = -1;
                }
            }
        }
        [DataMember]
        [JsonInclude]
        public int CapitalID
        {
            get
            {
                return this.capitalID;
            }
            set
            {
                this.capitalID = value;
                this.capital = null;
            }
        }

        public string CapitalName
        {
            get
            {
                if (this.Capital != null)
                {
                    return this.Capital.Name;
                }
                return "----";
            }
        }

        public int CaptiveCount
        {
            get
            {
                return this.Captives.Count;
            }
        }
        [DataMember]
        [JsonInclude]
        public int ColorIndex
        {
            get
            {
                return this.colorIndex;
            }
            set
            {
                this.colorIndex = value;
            }
        }

        public bool Controlling
        {
            get
            {
                return this.controlling;
            }
            set
            {
                this.controlling = value;
            }
        }

        public int GetFacilityKindCount(int id)
        {
            int cnt = 0;
            foreach (Architecture a in this.Architectures)
            {
                cnt += a.GetFacilityKindCount(id);
            }
            return cnt;
        }

        public Section FirstSection
        {
            get
            {
                if (this.SectionCount > 0)
                {
                    return (this.Sections[0] as Section);
                }

                // 🔥 修改：只有玩家势力才自动创建第一个军区，AI势力返回null
                if (Session.Current.Scenario.IsPlayer(this))
                {
                    return this.CreateFirstSection();
                }
                else
                {
                    // AI势力不自动创建军区，返回null
                    return null;
                }
            }
        }

        public long Food
        {
            get
            {
                long num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Food;
                }
                return num;
            }
        }

        public int FriendlyDiplomaticRelationCount
        {
            get
            {
                int num = 0;
                foreach (DiplomaticRelation relation in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    if (relation.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold)
                    {
                        num++;
                    }
                }
                return num;
            }
        }

        public int Fund
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Fund;
                }
                return num;
            }
        }

        public float GetCurrentRoutewayConsumptionRate
        {
            get
            {
                return this.RoutewayPathBuilder.PathConsumptionRate;
            }
        }

        public bool HasFriendlyDiplomaticRelation
        {
            get
            {
                foreach (DiplomaticRelation relation in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationListByFactionID(base.ID))
                {
                    if (relation.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int feiziCount()
        {
            int r = 0;
            foreach (Architecture a in this.Architectures)
            {
                r += a.Feiziliebiao.Count;
            }
            return r;
        }

        public PersonList GetFeiziList()
        {
            PersonList result = new PersonList();
            foreach (Architecture a in this.Architectures)
            {
                foreach (Person p in a.Feiziliebiao)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        public int meinvkongjian()
        {
            int r = 0;
            foreach (Architecture a in this.Architectures)
            {
                r += a.Meinvkongjian;
            }
            return r;
        }

        public int InformationCount
        {
            get
            {
                return this.Informations.Count;
            }
        }

        private float InternalSurplusRateCache = -1;
        public float InternalSurplusRate
        {
            get
            {
                if (InternalSurplusRateCache > 0)
                    return InternalSurplusRateCache;

                if ((!Session.Current.Scenario.IsPlayer(this) && !Session.GlobalVariables.internalSurplusRateForAI) || (Session.Current.Scenario.IsPlayer(this) && !Session.GlobalVariables.internalSurplusRateForPlayer))
                {
                    InternalSurplusRateCache = 1;
                    return 1;
                }

                float num = (Session.Parameters.InternalSurplusFactor - this.Power) / (float)Session.Parameters.InternalSurplusFactor;

                if (num < 0.2f)
                {
                    num = 0.2f;
                }

                InternalSurplusRateCache = num;
                return num;
            }
        }

        public string InternalSurplusRatePercentString
        {
            get
            {
                return StaticMethods.GetPercentString(this.InternalSurplusRate, 3);
            }
        }

        public Person Leader
        {
            get
            {
                if (this.leaderID == -1)
                {
                    this.leaderID = this.Persons.GetMaxMeritPerson().ID;
                }
                if (this.leader == null && Session.Current.Scenario != null && Session.Current.Scenario.Persons != null)
                {
                    // 🔥 AOT修复：使用更安全的类型转换方式
                    var gameObject = Session.Current.Scenario.Persons.GetGameObject(this.LeaderID);
                    if (gameObject is Person person)
                    {
                        this.leader = person;
                        System.Diagnostics.Debug.WriteLine($"[Faction.Leader] 成功解析Leader ID {this.LeaderID} -> {person.Name}");
                    }
                    else if (gameObject != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Faction.Leader] 警告：ID {this.LeaderID} 对应的对象不是Person类型: {gameObject.GetType().Name}");
                        this.leader = null;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Faction.Leader] 警告：未找到ID为 {this.LeaderID} 的Person对象");
                        this.leader = null;
                        
                        // 🔥 AOT修复：尝试从当前势力的人员中找到合适的领导者
                        if (this.Persons != null && this.Persons.Count > 0)
                        {
                            var maxMeritPerson = this.Persons.GetMaxMeritPerson();
                            if (maxMeritPerson != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Faction.Leader] 自动选择最高功绩人员作为Leader: {maxMeritPerson.Name}");
                                this.leader = maxMeritPerson;
                                this.leaderID = maxMeritPerson.ID;
                            }
                        }
                    }
                }
                return this.leader;
            }
            set
            {
                this.leader = value;
                if (this.leader != null)
                {
                    this.LeaderID = this.leader.ID;
                }
                else
                {
                    this.LeaderID = -1;
                }
            }
        }
        [DataMember]
        [JsonInclude]
        public int LeaderID
        {
            get
            {
                return this.leaderID;
            }
            set
            {
                this.leaderID = value;
            }
        }

        /// <summary>
        /// 军师 - 负责提供战略建议和决策支持
        /// </summary>
        public Person Advisor
        {
            get
            {
                if (this.advisor == null && this.advisorID != -1 && Session.Current.Scenario != null && Session.Current.Scenario.Persons != null)
                {
                    // 🔥 AOT修复：使用更安全的类型转换方式
                    var gameObject = Session.Current.Scenario.Persons.GetGameObject(this.AdvisorID);
                    if (gameObject is Person person)
                    {
                        this.advisor = person;
                        System.Diagnostics.Debug.WriteLine($"[Faction.Advisor] 成功解析Advisor ID {this.AdvisorID} -> {person.Name}");
                    }
                    else if (gameObject != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Faction.Advisor] 警告：ID {this.AdvisorID} 对应的对象不是Person类型: {gameObject.GetType().Name}");
                        this.advisor = null;
                        this.advisorID = -1; // 重置无效的ID
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Faction.Advisor] 警告：未找到ID为 {this.AdvisorID} 的Person对象");
                        this.advisor = null;
                        this.advisorID = -1; // 重置无效的ID
                    }
                }

                // 检查军师有效性
                if (this.advisor != null && (!this.advisor.Alive || !this.advisor.Available || this.advisor.BelongedFaction != this))
                {
                    this.Advisor = null;
                }

                return this.advisor;
            }
            set
            {
                this.advisor = value;
                if (this.advisor != null)
                {
                    this.AdvisorID = this.advisor.ID;
                }
                else
                {
                    this.AdvisorID = -1;
                }
            }
        }

        [DataMember]
        [JsonInclude]
        public int AdvisorID
        {
            get
            {
                return this.advisorID;
            }
            set
            {
                this.advisorID = value;
            }
        }

        public string AdvisorName
        {
            get
            {
                return ((this.Advisor != null) ? this.Advisor.Name : "----");
            }
        }

        public string LeaderName
        {
            get
            {
                return ((this.Leader != null) ? this.Leader.Name : "----");
            }
        }
        // [DataMember]//取消储君序列化，原有的方法会导致二次存档后储君为空
        public Person Prince
        {
            get
            {
                if (this.princeID == -1) return null;

                if (this.prince == null)
                {
                    this.prince = (Session.Current.Scenario.Persons.GetGameObject(this.PrinceID) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(this.PrinceID) : null);
                }

                //检查储君有效性
                if (this.prince != null && (this.prince == this.Leader || !this.prince.Alive || !this.prince.Available || this.prince.BelongedFaction != this
                    || this.prince.BelongedFaction == null))
                {
                    this.Prince = null;
                }
                return this.prince;
            }
            set
            {
                this.prince = value;
                if (this.prince != null)
                {
                    this.PrinceID = this.prince.ID;
                }
                else
                {
                    this.PrinceID = -1;
                }
            }
        }
        [DataMember]
        [JsonInclude]
        public int PrinceID
        {
            get
            {
                return this.princeID;
            }
            set
            {
                this.princeID = value;
            }
        }

        public string PrinceName
        {
            get
            {
                return ((this.Prince != null) ? this.Prince.Name : "----");
            }
        }

        public int LegionCount
        {
            get
            {
                return this.Legions.Count;
            }
        }
        
        // Removed obsolete MilitariesString - use MilitaryIDs instead

        [DataMember]
        [JsonInclude]
        public List<int> MilitaryIDs { get; set; } = new List<int>();

        public MilitaryList Militaries
        {
            get
            {
                MilitaryList list = new MilitaryList();
                /*
                foreach (Military military in Session.Current.Scenario.Militaries)
                {
                    if (military.BelongedArchitecture != null && military.BelongedArchitecture.BelongedFaction == this)
                    {
                        Militaries.Add(military);
                    }

                }*/
                if (this.Architectures != null)
                {
                    foreach (Architecture a in this.Architectures)
                    {
                        foreach (Military military in a.Militaries)
                        {
                            list.Add(military);
                        }
                    }
                }

                if (this.TransferingMilitaries != null)
                {
                    foreach (Military military in this.TransferingMilitaries)
                    {
                        list.Add(military);
                    }
                }

                if (this.Troops != null)
                {
                    foreach (Troop troop in this.Troops)
                    {
                        if (troop.Army != null)
                        {
                            if (troop.Army.ShelledMilitary == null)
                            {
                                list.Add(troop.Army);
                            }
                            else
                            {
                                list.Add(troop.Army.ShelledMilitary);
                            }
                        }
                    }
                }

                return list;

            }
        }

        // 🔥 注意：这是计算属性，不应该序列化
        // 值来自 MilitaryIDs 反序列化后的集合
        // 日期：2026-03-20
        // [DataMember]  // 已移除：计算属性不需要序列化
        public int MilitaryCount
        {
            get
            {
                return this.Militaries.Count;
            }
            set
            {
                this.militarycount = value;
            }
        }
        // 🔥 注意：这是计算属性，不应该序列化
        // 值来自 TransferingMilitariesString 反序列化后的集合
        // 日期：2026-03-20
        // [DataMember]  // 已移除：计算属性不需要序列化
        public int TransferingMilitaryCount
        {
            get
            {
                return this.TransferingMilitaries != null ? this.TransferingMilitaries.Count : 0;
            }
            set
            {
                this.transferingmilitarycount = value;
            }
        }


        public bool Passed
        {
            get
            {
                return this.passed;
            }
            set
            {
                this.passed = value;
            }
        }

        public int PersonCount
        {
            // 切记不要用this.Persons.Count，因为这样会把全势力人物一个一个加入PersonList，非常慢
            get
            {
                int result = 0;
                foreach (Architecture a in Architectures)
                {
                    result += a.Persons.Count;
                    result += a.MovingPersons.Count;
                }
                foreach (Troop t in Troops)
                {
                    result += t.PersonCount;
                }
                
                // 🔥 防御性检查：Session未初始化时跳过Captives
                if (Session.Current?.Scenario?.Captives != null)
                {
                    foreach (Captive c in Session.Current.Scenario.Captives)
                    {
                        if (c.CaptiveFaction == this)
                        {
                            result++;
                        }
                    }
                }


                return result;
            }
        }

        public PersonList SelfOfficers //自势力野武将
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person person in this.Persons)
                {
                    if (person.ID >= 25000)
                    {
                        result.Add(person);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// 检查是否可以任命军师（允许重新任命）
        /// </summary>
        public bool AppointAdvisorAvail()
        {
            if (this.Leader != null && this.Leader.BelongedCaptive == null)
            {
                if (Session.Current.Scenario.IsPlayer(this) && this.AdvisorCandicate.Count > 0)
                {
                    return true;
                }

                if (!Session.Current.Scenario.IsPlayer(this) && this.AIAdvisorCandicate.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否可以罢免军师
        /// </summary>
        public bool RecallAdvisorAvail()
        {
            return this.Leader != null && this.Leader.BelongedCaptive == null && this.AdvisorID != -1;
        }

        /// <summary>
        /// 军师候选人列表（玩家用）- 按智力从高到低排序
        /// </summary>
        public PersonList AdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    // 排除：君主、现任军师、忙碌状态、死亡、俘虏、未出仕(逻辑上Persons只包含已出仕)
                    // ★★★ 修复：排除女官 (!p.NvGuan) ★★★
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive &&
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70 && !p.NvGuan)
                    {
                        result.Add(p);
                    }
                }

                // 按智力从高到低排序
                result.PropertyName = "Intelligence";
                result.IsNumber = true;
                result.SmallToBig = false;  // false表示从大到小排序
                result.ReSort();

                return result;
            }
        }

        /// <summary>
        /// AI军师候选人列表（按智力排序）
        /// </summary>
        public PersonList AIAdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();

                foreach (Person p in this.AdvisorCandicate)
                {
                    result.Add(p);
                }

                result.PropertyName = "Intelligence";
                result.IsNumber = true;
                result.SmallToBig = false;
                result.ReSort();
                return result;
            }
        }

        /// <summary>
        /// 任命军师
        /// </summary>
        /// <param name="person">被任命的人物</param>
        public void AppointAdvisor(Person person)
        {
            // 🔥 根本修复：同时设置 Advisor 和 AdvisorID
            // 日期：2026-03-18
            // 问题：任命军师后保存读档，军师丢失
            // 原因：AppointAdvisor 只设置了 Advisor 属性，依赖 setter 自动设置 AdvisorID
            //       但如果 Advisor.Available = false，getter 会清除 advisor 字段，导致 AdvisorID 也被清除
            this.AdvisorID = person.ID;
            this.Advisor = person;

            // 添加年表记录
            Session.Current.Scenario.YearTable.addAppointAdvisorEntry(Session.Current.Scenario.Date, person, this.Leader);

            // 触发事件
            if (this.OnAppointAdvisor != null)
            {
                this.OnAppointAdvisor(this.Leader, person);
            }
        }

        /// <summary>
        /// 任命军师事件
        /// </summary>
        public event AppointAdvisorDelegate OnAppointAdvisor;
        public delegate void AppointAdvisorDelegate(Person leader, Person advisor);

        /// <summary>
        /// 罢免军师
        /// </summary>
        public void RemoveAdvisor()
        {
            if (this.Advisor != null)
            {
                Person formerAdvisor = this.Advisor;

                // 清除军师数据
                this.AdvisorID = -1;
                this.Advisor = null;

                // 触发罢免事件
                if (this.OnRemoveAdvisor != null)
                {
                    this.OnRemoveAdvisor(this.Leader, formerAdvisor);
                }

                // 添加年表记录
                Session.Current.Scenario.YearTable.addRemoveAdvisorEntry(Session.Current.Scenario.Date, formerAdvisor, this.Leader);
            }
        }

        /// <summary>
        /// 罢免军师事件
        /// </summary>
        public event RemoveAdvisorDelegate OnRemoveAdvisor;
        public delegate void RemoveAdvisorDelegate(Person leader, Person formerAdvisor);

        /// <summary>
        /// 【增强版】检查 AI 君主是否听从军师建议
        /// 使用 CharacterKind 的纳谏倾向系统
        /// </summary>
        /// <returns>true=听从, false=刚愎自用</returns>
        public bool AICheckListenToAdvisor()
        {
            // 1. 基础检查
            if (this.Leader == null || this.Advisor == null) return true;

            // 2. 如果是历史上的"黄金搭档"，大幅提升听从概率
            bool isGoldenPair = IsGoldenPair(this.Leader, this.Advisor);
            if (isGoldenPair)
            {
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] {this.Leader.Name}与{this.Advisor.Name}是历史黄金搭档！");
            }

            // 3. 使用性格的纳谏倾向作为基础概率
            int baseChance = this.Leader.Character?.ListenToAdvisorChance ?? 50; // 默认50%

            System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] {this.Leader.Name}的基础纳谏倾向: {baseChance}%");

            // 4. 智力差值修正 - 军师越聪明，君主越容易听从
            int intDiff = this.Advisor.Intelligence - this.Leader.Intelligence;
            int intModifier = intDiff / 2; // 减少智力差值的影响，避免过度
            baseChance += intModifier;

            if (intModifier != 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 智力差值修正: {intModifier}% (军师{this.Advisor.Intelligence} vs 君主{this.Leader.Intelligence})");
            }

            // 5. 黄金搭档加成
            if (isGoldenPair)
            {
                baseChance += 25; // 历史搭档大幅加成
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 黄金搭档加成: +25%");
            }

            // 6. 相性修正
            int compatibility = Math.Abs(this.Leader.Ideal - this.Advisor.Ideal);
            if (compatibility > 30)
            {
                int compatibilityPenalty = -(compatibility - 30) / 5; // 相性差每5点减1%
                baseChance += compatibilityPenalty;
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 相性差异修正: {compatibilityPenalty}% (差异{compatibility})");
            }

            // 7. 关系修正 - 个人关系影响
            int relationStatus = this.Advisor.CheckRelation(this.Leader);
            if (relationStatus == 1) // 亲密关系
            {
                baseChance += 15;
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 亲密关系加成: +15%");
            }
            else if (relationStatus == -1) // 厌恶关系
            {
                baseChance -= 25;
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 厌恶关系惩罚: -25%");
            }

            // 8. 军师忠诚度影响 - 忠诚度低的军师建议可信度下降
            if (this.Advisor.Loyalty < 80)
            {
                int loyaltyPenalty = -(80 - this.Advisor.Loyalty) / 5;
                baseChance += loyaltyPenalty;
                System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] 军师忠诚度修正: {loyaltyPenalty}% (忠诚{this.Advisor.Loyalty})");
            }

            // 9. 概率封顶修正 (保持合理范围)
            baseChance = Math.Max(5, Math.Min(95, baseChance));

            // 10. 最终判定
            bool isListen = GameObject.Random(100) < baseChance;

            // 11. 如果拒绝，显示拒绝消息
            if (!isListen)
            {
                ShowRefusalMessage(this);
            }

            // Debug 日志
            System.Diagnostics.Debug.WriteLine($"[AICheckListenToAdvisor] {this.Leader.Name} 最终听从概率: {baseChance}% -> {(isListen ? "听从" : "拒绝")}");

            return isListen;
        }

        /// <summary>
        /// 【修正版】AI 决策检查 - 区分明主决策与纳谏决策
        /// 当君主智力高于军师时，体现明主自主决策能力
        /// 当军师智力高于君主时，依赖传统纳谏系统
        /// </summary>
        /// <returns>true=执行决策, false=拒绝决策</returns>
        public bool AICheckDecision()
        {
            if (this.Leader == null || this.Advisor == null) return true;

            // 场景 1：君主比军师聪明 (例如：曹操 vs 杨修)
            if (this.Leader.Intelligence > this.Advisor.Intelligence)
            {
                System.Diagnostics.Debug.WriteLine($"[AICheckDecision] 明主决策模式: {this.Leader.Name}(智{this.Leader.Intelligence}) > {this.Advisor.Name}(智{this.Advisor.Intelligence})");

                // 逻辑：君主非常自信，几乎不完全依赖军师的倾向
                // 但因为君主自己智力高，所以最终决策正确的概率反而更高
                // 这里返回 true 表示"执行决策"，但决策源头其实是君主自己

                // 只有当军师忠诚度低，且可能在坑君主时，高智力君主会识破并拒绝
                if (this.Advisor.Loyalty < 80 && GameObject.Random(100) < this.Leader.Intelligence)
                {
                    System.Diagnostics.Debug.WriteLine($"[AICheckDecision] {this.Leader.Name} 识破了 {this.Advisor.Name} 的不良建议！(忠诚{this.Advisor.Loyalty})");

                    // 显示明主识破的消息 (暂时注释掉，因为相关属性不存在)
                    // if (Session.Current.Scenario.IsPlayerGivenFactionInfo(this))
                    // {
                    //     string message = GetWiseRulerDetectionMessage();
                    //     Session.Current.Scenario.GameScreen.AddTextMessage(message, this.Leader.Position);
                    // }

                    return false; // 识破了军师的坏主意
                }

                System.Diagnostics.Debug.WriteLine($"[AICheckDecision] {this.Leader.Name} 认可了决策方案 (明主把关)");
                return true; // 君主认可了这个方案（即使是军师提的）
            }
            // 场景 2：军师比君主聪明 (例如：刘备 vs 诸葛亮)
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AICheckDecision] 纳谏决策模式: {this.Advisor.Name}(智{this.Advisor.Intelligence}) >= {this.Leader.Name}(智{this.Leader.Intelligence})");

                // 逻辑：依赖之前的"纳谏概率"算法
                bool willListen = AICheckListenToAdvisor();

                System.Diagnostics.Debug.WriteLine($"[AICheckDecision] 纳谏结果: {(willListen ? "听从军师" : "刚愎自用")}");

                return willListen;
            }
        }

        /// <summary>
        /// 获取明主识破不良建议的消息
        /// </summary>
        /// <returns>识破消息</returns>
        private string GetWiseRulerDetectionMessage()
        {
            var messages = new string[]
            {
                $"{this.Leader.Name}：「{this.Advisor.Name}此言有诈，孤不从也！」",
                $"{this.Leader.Name}：「此计恐有后患，{this.Advisor.Name}用心何在？」",
                $"{this.Leader.Name}：「孤观此策不妥，{this.Advisor.Name}莫要糊弄于孤！」",
                $"{this.Leader.Name}：「{this.Advisor.Name}之言虽巧，然孤心明如镜！」"
            };

            return messages[GameObject.Random(messages.Length)];
        }

        /// <summary>
        /// 本回合军师的建议缓存
        /// (每回合开始时刷新，回合内保持不变，除非被标记为已解决)
        /// </summary>
        [DataMember]
        [JsonInclude]
        public AdvisorSuggestionKind CurrentRoundSuggestion { get; set; } = AdvisorSuggestionKind.None;

        /// <summary>
        /// 当前建议的详细信息
        /// </summary>
        public AdvisorSuggestion CurrentSuggestionDetails { get; set; }

        /// <summary>
        /// 上次检查建议的回合数
        /// </summary>
        [DataMember]
        [JsonInclude]
        public int LastSuggestionCheckTurn { get; set; } = -1;

        /// <summary>
        /// 上次军师推荐人才的年份（用于冷却控制）
        /// </summary>
        [DataMember]
        [JsonInclude]
        public int LastTalentRecommendYear { get; set; } = 0;

        /// <summary>
        /// AI系统更新计数器，用于控制各AI子系统的更新频率
        /// </summary>
        [DataMember]
        [JsonInclude]
        public int AIUpdateCounter { get; set; } = 0;

        /// <summary>
        /// 军师举荐功能是否启用
        /// </summary>
        [DataMember]
        [JsonInclude]
        public bool IsAdvisorRecommendationEnabled { get; set; } = true;

        /// <summary>
        /// 检查并生成建议 (核心算法)
        /// 每回合只执行一次，结果缓存到CurrentRoundSuggestion
        /// </summary>
        public AdvisorSuggestionKind CheckAdvisorHasSuggestion()
        {
            // 检查是否需要刷新建议
            int currentTurn = Session.Current?.Scenario?.Date?.Year ?? 0;

            if (LastSuggestionCheckTurn != currentTurn)
            {
                // 新回合，重新生成建议
                RefreshAdvisorSuggestion();
                LastSuggestionCheckTurn = currentTurn;
            }

            return CurrentRoundSuggestion;
        }

        /// <summary>
        /// 刷新军师建议 (每回合调用一次)
        /// </summary>
        public void RefreshAdvisorSuggestion()
        {
            if (this.Advisor == null || this.Leader == null)
            {
                CurrentRoundSuggestion = AdvisorSuggestionKind.None;
                CurrentSuggestionDetails = null;
                return;
            }

            // 使用建议系统生成新建议
            var suggestion = AdvisorSuggestionSystem.CheckAdvisorHasSuggestion(this);

            CurrentRoundSuggestion = suggestion.GeneralKind;
            CurrentSuggestionDetails = suggestion;

            // 记录建议到日志
            if (suggestion.GeneralKind != AdvisorSuggestionKind.None)
            {
                AddAdviceToLog($"军师建议: {suggestion.Title}");

                System.Diagnostics.Debug.WriteLine($"[AdvisorSuggestion] {this.Name}: {suggestion.Title} (优先级: {suggestion.Priority})");
            }
        }

        /// <summary>
        /// 当玩家执行某些指令后调用，检查建议是否已解决
        /// </summary>
        public void CheckAdviceResolved()
        {
            if (CurrentRoundSuggestion == AdvisorSuggestionKind.None)
                return;

            bool isResolved = false;

            // 根据具体建议类型检查是否已解决
            if (CurrentSuggestionDetails != null)
            {
                switch (CurrentSuggestionDetails.Kind)
                {
                    case SpecificSuggestionKind.PersonRecruit:
                        // 如果当前建议是"招募人才"，检查是否还有在野武将
                        if (!AdvisorSuggestionSystem.CheckUnfoundPerson(this))
                        {
                            isResolved = true;
                        }
                        break;

                    case SpecificSuggestionKind.EnemyAttack:
                        // 如果建议是"防御敌袭"，检查敌军是否被消灭了
                        if (!AdvisorSuggestionSystem.CheckEnemyApproaching(this, 100))
                        {
                            isResolved = true;
                        }
                        break;

                    case SpecificSuggestionKind.InternalAffairs:
                        // 检查内政是否已改善
                        if (!AdvisorSuggestionSystem.NeedsInternalDevelopment(this))
                        {
                            isResolved = true;
                        }
                        break;

                    case SpecificSuggestionKind.DefensePreparation:
                        // 检查防御是否已加强
                        if (!AdvisorSuggestionSystem.NeedsDefensePreparation(this))
                        {
                            isResolved = true;
                        }
                        break;

                    case SpecificSuggestionKind.ResourceManagement:
                        // 检查资源状况是否改善
                        if (!AdvisorSuggestionSystem.NeedsResourceManagement(this))
                        {
                            isResolved = true;
                        }
                        break;

                        // 其他建议类型的解决检查...
                }
            }

            if (isResolved)
            {
                // 建议已解决，清除缓存
                CurrentRoundSuggestion = AdvisorSuggestionKind.None;
                CurrentSuggestionDetails = null;

                AddAdviceToLog("军师建议已采纳并执行");
                System.Diagnostics.Debug.WriteLine($"[AdvisorSuggestion] {this.Name}: 建议已解决");
            }
        }

        /// <summary>
        /// 获取当前建议的显示文本
        /// </summary>
        /// <returns>格式化的建议文本</returns>
        public string GetCurrentSuggestionText()
        {
            if (CurrentSuggestionDetails == null || CurrentRoundSuggestion == AdvisorSuggestionKind.None)
            {
                return "军师暂无建议";
            }

            // 使用建议显示系统格式化文本
            return AdviceDisplaySystem.GetAdviceText(this, CurrentSuggestionDetails.DetailedAdvice, AdviceType.General);
        }

        /// <summary>
        /// 获取建议的紧急程度颜色
        /// </summary>
        /// <returns>颜色值</returns>
        public Color GetSuggestionUrgencyColor()
        {
            if (CurrentSuggestionDetails == null)
                return Color.Gray;

            if (CurrentSuggestionDetails.Urgency >= 8)
                return Color.Red;      // 非常紧急
            else if (CurrentSuggestionDetails.Urgency >= 6)
                return Color.Orange;   // 紧急
            else if (CurrentSuggestionDetails.Urgency >= 4)
                return Color.Yellow;   // 一般
            else if (CurrentSuggestionDetails.Urgency >= 2)
                return Color.LightBlue; // 不急
            else
                return Color.Gray;     // 无紧急性
        }

        /// <summary>
        /// 强制清除当前建议 (用于测试或特殊情况)
        /// </summary>
        public void ClearCurrentSuggestion()
        {
            CurrentRoundSuggestion = AdvisorSuggestionKind.None;
            CurrentSuggestionDetails = null;
            AddAdviceToLog("军师建议已被清除");
        }

        /// <summary>
        /// 检查是否为历史上的黄金搭档
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <returns>true=黄金搭档</returns>
        private bool IsGoldenPair(Person leader, Person advisor)
        {
            if (leader == null || advisor == null) return false;

            // 根据人物ID或姓名判断经典搭档
            // 这里可以根据实际游戏中的人物ID来配置
            string leaderName = leader.Name;
            string advisorName = advisor.Name;

            // 经典搭档列表
            var goldenPairs = new Dictionary<string, List<string>>
            {
                { "刘备", new List<string> { "诸葛亮", "庞统" } },
                { "曹操", new List<string> { "荀彧", "郭嘉", "程昱" } },
                { "孙权", new List<string> { "周瑜", "鲁肃", "陆逊" } },
                { "刘表", new List<string> { "蒯越", "蔡瑁" } },
                { "袁绍", new List<string> { "田丰", "沮授" } }, // 注意：袁绍历史上不太听这两位的
                { "吕布", new List<string> { "陈宫" } },
                { "董卓", new List<string> { "李儒" } }
            };

            if (goldenPairs.ContainsKey(leaderName))
            {
                return goldenPairs[leaderName].Contains(advisorName);
            }

            return false;
        }

        /// <summary>
        /// 显示君主拒绝军师建议的消息
        /// </summary>
        /// <param name="faction">拒绝建议的势力</param>
        private void ShowRefusalMessage(Faction faction)
        {
            if (faction?.Leader == null || faction.Advisor == null) return;

            // 根据君主性格选择不同的拒绝理由
            string[] refusalReasons = GetRefusalReasons(faction.Leader);
            string selectedReason = refusalReasons[GameObject.Random(refusalReasons.Length)];

            // 构建完整消息
            string fullMessage = $"{faction.Leader.Name}：「{selectedReason}」";

            System.Diagnostics.Debug.WriteLine($"[拒绝军师] {fullMessage}");

            // 显示给玩家（如果玩家有情报或视野）
            if (IsVisibleToPlayer(faction))
            {
                // 这里应该调用游戏的消息系统
                // Session.Current.Scenario.GameScreen.AddDialogue(faction.Leader, selectedReason);

                // 或者添加到历史记录
                // Session.Current.Scenario.GameScreen.AddTextMessage(fullMessage, faction.Leader.Position);

                // 暂时使用Debug输出
                System.Diagnostics.Debug.WriteLine($"[玩家可见] {fullMessage}");
            }
        }

        /// <summary>
        /// 根据君主性格获取拒绝理由
        /// </summary>
        /// <param name="leader">君主</param>
        /// <returns>可能的拒绝理由数组</returns>
        private string[] GetRefusalReasons(Person leader)
        {
            // 根据纳谏倾向和性格特点选择不同的拒绝理由
            int listenChance = leader.Character?.ListenToAdvisorChance ?? 50;

            if (listenChance <= 20) // 极度刚愎自用
            {
                return new string[]
                {
                    "孤意已决，先生勿复多言！",
                    "吾自有主张，何须他人指点？",
                    "此事吾心中早有定计！",
                    "先生多虑了，看吾行事便是！"
                };
            }
            else if (listenChance <= 40) // 较为固执
            {
                return new string[]
                {
                    "此计虽好，恐有诈也...",
                    "先生之言虽善，吾另有考量。",
                    "此事容吾再思量一二。",
                    "先生过于谨慎，机不可失！"
                };
            }
            else if (listenChance <= 60) // 有自己想法
            {
                return new string[]
                {
                    "吾视敌军如草芥，何须用计？",
                    "先生所言有理，然吾有更好之策。",
                    "时机未到，且看吾如何行事。",
                    "此计太过复杂，不如直取！"
                };
            }
            else // 一般情况下很少拒绝，但偶尔也有
            {
                return new string[]
                {
                    "先生之策虽妙，吾心中另有安排。",
                    "此事吾已有定计，先生勿忧。",
                    "时势变化，当机立断！",
                    "先生多虑，看吾神威！"
                };
            }
        }

        /// <summary>
        /// 检查该势力是否对玩家可见
        /// </summary>
        /// <param name="faction">目标势力</param>
        /// <returns>是否可见</returns>
        private bool IsVisibleToPlayer(Faction faction)
        {
            // 这里应该根据游戏的情报系统和视野系统来判断
            // 简化实现：
            // 1. 玩家势力总是可见
            // 2. 邻近势力可见
            // 3. 有外交关系的势力可见
            // 4. 有间谍的势力可见

            // 暂时返回true，实际游戏中需要根据具体情况判断
            return true;
        }

        /// <summary>
        /// 获取AI听从军师建议的详细分析信息（用于调试和UI显示）
        /// </summary>
        /// <returns>分析结果字符串</returns>
        public string GetAIAdvisorListenAnalysis()
        {
            if (this.Leader == null || this.Advisor == null)
                return "无君主或军师，无法分析";

            var analysis = new StringBuilder();
            analysis.AppendLine($"=== {this.Leader.Name} 听从 {this.Advisor.Name} 建议分析 ===");

            // 基础概率
            int baseChance = 70;
            analysis.AppendLine($"基础概率: {baseChance}%");

            // 黄金搭档检查
            if (IsGoldenPair(this.Leader, this.Advisor))
            {
                analysis.AppendLine("✨ 历史黄金搭档: 绝对听从!");
                return analysis.ToString();
            }

            // 智力差值
            int intDiff = this.Advisor.Intelligence - this.Leader.Intelligence;
            baseChance += intDiff;
            analysis.AppendLine($"智力差值修正: {intDiff:+#;-#;0}% (军师{this.Advisor.Intelligence} - 君主{this.Leader.Intelligence})");

            // 性格修正
            int personalityMod = 0;
            string personalityDesc = "";
            switch (this.Leader.Character?.ID ?? 0)
            {
                case 0: personalityMod = 10; personalityDesc = "胆小/稳健型"; break;
                case 1: personalityMod = -15; personalityDesc = "鲁莽/刚猛型"; break;
                case 2: personalityMod = 5; personalityDesc = "冷静/理智型"; break;
                case 3: personalityMod = -30; personalityDesc = "刚愎自用型"; break;
            }
            baseChance += personalityMod;
            analysis.AppendLine($"性格修正: {personalityMod:+#;-#;0}% ({personalityDesc})");

            // 相性修正
            int compatibility = Math.Abs(this.Leader.Ideal - this.Advisor.Ideal);
            int compatibilityMod = compatibility > 20 ? -10 : 0;
            baseChance += compatibilityMod;
            analysis.AppendLine($"相性修正: {compatibilityMod:+#;-#;0}% (理想差异{compatibility})");

            // 关系修正
            int relationStatus = this.Advisor.CheckRelation(this.Leader);
            int relationMod = 0;
            string relationDesc = "";
            if (relationStatus == 1) { relationMod = 15; relationDesc = "亲密"; }
            else if (relationStatus == -1) { relationMod = -20; relationDesc = "恶劣"; }
            else { relationDesc = "普通"; }
            baseChance += relationMod;
            analysis.AppendLine($"关系修正: {relationMod:+#;-#;0}% ({relationDesc})");

            // 最终概率
            if (baseChance > 95) baseChance = 95;
            if (baseChance < 5) baseChance = 5;
            analysis.AppendLine($"最终概率: {baseChance}%");

            return analysis.ToString();
        }

        public int SelfOfficerCount //本势力野武将总数
        {
            get
            {
                return (this.SelfOfficers.Count);

            }
        }

        public int Population
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Population;
                }
                return num;
            }
        }

        public bool PreUserControlFinished
        {
            get
            {
                return this.preUserControlFinished;
            }
            set
            {
                this.preUserControlFinished = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public int Reputation
        {
            get
            {
                return this.reputation;
            }
            set
            {
                this.reputation = value;
            }
        }

        public int RoutewayCount
        {
            get
            {
                return this.Routeways.Count;
            }
        }

        public int RoutewayWorkForce
        {
            get
            {
                return (100 + this.IncrementOfRoutewayWorkforce);
            }
        }

        public int[,] SecondTierMapCost
        {
            get
            {
                return this.secondTierMapCost;
            }
        }

        public int SectionCount
        {
            get
            {
                return this.Sections.Count;
            }
        }

        public int SelfCaptiveCount
        {
            get
            {
                return this.SelfCaptives.Count;
            }
        }
        [DataMember]
        [JsonInclude]
        public int TechniquePoint
        {
            get
            {
                return this.techniquePoint;
            }
            set
            {
                this.techniquePoint = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public int TechniquePointForFacility
        {
            get
            {
                return this.techniquePointForFacility;
            }
            set
            {
                this.techniquePointForFacility = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public int TechniquePointForTechnique
        {
            get
            {
                return this.techniquePointForTechnique;
            }
            set
            {
                this.techniquePointForTechnique = value;
            }
        }

        public int[,] ThirdTierMapCost
        {
            get
            {
                return this.thirdTierMapCost;
            }
        }

        public int TotalTechniquePoint
        {
            get
            {
                return ((this.techniquePoint + this.techniquePointForTechnique) + this.techniquePointForFacility);
            }
        }

        public int TroopCount
        {
            get
            {
                return this.Troops.Count;
            }
        }

        public int TroopCountExcludeTransport
        {
            get
            {
                int r = 0;
                foreach (Troop t in this.Troops)
                {
                    if (!t.IsTransport)
                    {
                        r++;
                    }
                }
                return r;
            }
        }
        [DataMember]
        [JsonInclude]
        public int UpgradingDaysLeft
        {
            get
            {
                return this.upgradingDaysLeft;
            }
            set
            {
                this.upgradingDaysLeft = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public int UpgradingTechnique
        {
            get
            {
                return this.upgradingTechnique;
            }
            set
            {
                this.upgradingTechnique = value;
            }
        }

        private bool WantControl
        {
            get
            {
                bool stopToControl = this.StopToControl;
                if (!stopToControl)
                {
                    stopToControl = Session.Current.Scenario.Date.DaysLeft == 0;
                }
                return stopToControl;
            }
        }

        public int TotalPersonMerit
        {
            get
            {
                int result = 0;
                foreach (Person p in this.Persons)
                {
                    result += p.Merit;
                }
                return result;
            }
        }

        public int TotalPersonFightingForce
        {
            get
            {
                int result = 0;
                foreach (Person p in this.Persons)
                {
                    result += p.FightingForce;
                }
                return result;
            }
        }

        public bool hougongValid
        {
            get
            {
                return Session.GlobalVariables.hougongGetChildrenRate > 0 && (!Session.GlobalVariables.hougongAlienOnly || this.IsAlien);
            }
        }

        [DataMember]
        [JsonInclude]
        public int chaotinggongxiandu
        {
            get
            {
                return this.chaotinggongxiandudezhi;
            }
            set
            {
                this.chaotinggongxiandudezhi = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public int guanjue
        {
            get
            {
                return this.guanjuedezhi;
            }
            set
            {
                this.guanjuedezhi = value;
            }
        }
        [DataMember]
        [JsonInclude]
        public bool IsAlien
        {
            get
            {
                return this.isAlien;
            }
            set
            {
                this.isAlien = value;
            }
        }

        public InformationList GetAllInformationList()
        {
            InformationList result = new InformationList();
            foreach (Information i in this.Informations)
            {
                result.Add(i);
            }
            foreach (Architecture a in this.Architectures)
            {
                foreach (Information i in a.Informations)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public Architecture GetGeDiArchitecture(Faction targetFaction)
        {
            Architecture result = null;
            int dist = int.MaxValue;

            foreach (Architecture a in this.Architectures)
            {
                if (a != this.Capital)
                {
                    int innerDist = int.MaxValue;
                    foreach (Architecture b in targetFaction.Architectures)
                    {
                        int d = Session.Current.Scenario.GetSimpleDistance(a.ArchitectureArea.Centre, b.ArchitectureArea.Centre);

                        if (d < innerDist)
                        {
                            d = innerDist;
                        }
                    }

                    if (innerDist < dist && !a.HasHostileTroopsInView())
                    {
                        dist = innerDist;
                        result = a;
                    }
                }
            }

            return result;
        }

        public bool HasInformation()
        {
            return this.GetAllInformationList().Count > 0;
        }

        public delegate void AfterCatchLeader(Person leader, Faction faction);

        [StructLayout(LayoutKind.Sequential)]
        public struct ClosedPathEndpoints
        {
            public Point Start;
            public Point End;
            public ClosedPathEndpoints(Point start, Point end)
            {
                this.Start = start;
                this.End = end;
            }

            public static bool operator !=(Faction.ClosedPathEndpoints a, Faction.ClosedPathEndpoints b)
            {
                return ((a.Start != b.Start) || (a.End != b.End));
            }

            public static bool operator ==(Faction.ClosedPathEndpoints a, Faction.ClosedPathEndpoints b)
            {
                return ((a.Start == b.Start) && (a.End == b.End));
            }

            public override int GetHashCode()
            {
                return (this.Start.GetHashCode() ^ this.End.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override string ToString()
            {
                return (this.Start.ToString() + " " + this.End.ToString());
            }
        }

        public delegate void FactionDestroy(Faction faction);

        public delegate void FactionUpgradeTechnique(Faction faction, Technique technique, Architecture architecture);

        public delegate void ForcedChangeCapital(Faction faction, Architecture oldCapital, Architecture newCapital);

        public delegate void GetControl(Faction faction);

        public delegate void InitiativeChangeCapital(Faction faction, Architecture oldCapital, Architecture newCapital);

        public delegate void TechniqueFinished(Faction faction, Technique technique);

        public int MaxPossibleReputation
        {
            get
            {
                int maxReputation = int.MinValue;
                foreach (guanjuezhongleilei i in Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhongleiliebiao())
                {
                    if (i.shengwangshangxian > maxReputation)
                    {
                        maxReputation = i.shengwangshangxian;
                    }
                }
                return maxReputation;
            }
        }

        public float InfluenceKindValue(int id)
        {
            float result = 0;
            foreach (Influence i in Session.Current.Scenario.GameCommonData.AllInfluences.Influences.Values)
            {
                if (i.Kind.ID == id)
                {
                    foreach (ApplyingFaction j in i.appliedFaction)
                    {
                        if (j.faction == this)
                        {
                            result += i.Value;
                        }
                    }
                }
            }
            return result;
        }

        public PersonList Children
        {
            get
            {
                HashSet<Person> result = new HashSet<Person>();
                foreach (Person p in Session.Current.Scenario.Persons)
                {
                    if (p.Alive && !p.Available && p.Age >= 0 && ((p.Father != null && p.Father.BelongedFactionWithPrincess == this) || (p.Mother != null && p.Mother.BelongedFactionWithPrincess == this))
                        && (p.ID < 7000 || p.ID > 8000))
                    {
                        if (!(p.Father != null && p.Father.Alive && (p.Father.BelongedFactionWithPrincess != this)))
                        {
                            result.Add(p);
                        }
                    }
                }
                PersonList result2 = new PersonList();
                foreach (Person q in result)
                {
                    result2.Add(q);
                }
                result2.SetImmutable();
                return result2;
            }
        }

        /// <summary>
        /// 🧠 AI 决策入口 - 三步式智能决策系统
        /// 基于记忆的战略AI逻辑：观察 → 分析 → 行动
        /// </summary>
        public void RunAILogic()
        {
            try
            {
                // 1. 全局刷新势能图 (只做一次！不要在下面的循环里做！)
                // 这解决了回合过慢的问题
                if (this.StrategicMap != null)
                {
                    this.StrategicMap.Refresh(this);
                }

                // 2. 只有此处循环部队
                foreach (Troop troop in this.Troops)
                {
                    if (troop != null && !troop.Destroyed && troop.Controllable)
                    {
                        // A. 感知环境
                        troop.UpdateMemory();
                        
                        // B. 执行新版移动 (彻底接管移动权)
                        troop.ExecuteSmartMove();
                    }
                }
            }
            catch (Exception ex)
            {
                // 防止AI逻辑异常影响游戏稳定性
                System.Diagnostics.Debug.WriteLine($"AI逻辑执行异常 - 势力 {this.Name}: {ex.Message}");
            }
        }
        private void AISectionsOptimized()
        {
            // 🔥 关键修改：禁用所有自动军区重建，只允许手动创建军区
            bool isPlayerFaction = Session.Current.Scenario.IsPlayer(this);

            if (!isPlayerFaction)
            {
                // AI势力：不自动创建军区，只运行势力级AI
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[AISectionsOptimized] AI势力 " + this.Name + " 跳过军区AI，使用势力级AI");
                }
                return; // AI势力直接返回，不执行军区相关逻辑
            }

            // 玩家势力：也不自动重建军区，只运行现有军区的AI
            // 玩家势力：也不自动重建军区，只运行现有军区的AI
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelAI] AISectionsOptimized: 玩家势力 {this.Name} 开始检测军区, 现有军区数: {this.Sections.Count}");
            }

            // 直接执行现有军区的AI，不进行重建
            foreach (Section section in this.Sections.GetList())
            {
                // 调用完整 AI 方法
                // 调用完整 AI 方法
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelAI] AISectionsOptimized: 准备调用军区 {section.Name} (ID:{section.ID}) 的 .AI()");
                }
                section.AI(new GameTime());
            }
        }

        private void RebuildSectionsOptimized()
        {
            System.Diagnostics.Debug.WriteLine($"[TRACKING] RebuildSectionsOptimized called! Stack: {new System.Diagnostics.StackTrace()}");

            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 势力 " + this.Name + " 开始检查军区重建条件");
                System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] Capital: " + (this.Capital?.Name ?? "null"));
                System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] LoadScenarioInInitialization: " + Session.MainGame.mainGameScreen.LoadScenarioInInitialization);
                System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] Date.Day: " + Session.Current.Scenario.Date.Day);
                System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] SectionCount: " + this.SectionCount);
            }

            if ((this.Capital != null) && (Session.MainGame.mainGameScreen.LoadScenarioInInitialization || this.SectionCount == 0))
            {
                // 初始化或没有军区时，立即重建
                // 初始化或没有军区时，立即重建
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 初始化或无军区，立即重建");
                }

                this.ClearSections();
                SectionAIHelper.AutoOrganizeSections(this);

                foreach (Section section in this.Sections)
                {
                    section.RefreshSectionName();
                }

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 初始化重建完成，军区数量: " + this.SectionCount);
                }
            }
            else if ((this.Capital != null) && Session.Current.Scenario.Date.Day == 1 && (Session.Current.Scenario.Date.Month % 6) == 1)
            {
                // 每6个月重建一次（减少频率）
                // 每6个月重建一次（减少频率）
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 定期重建（每6个月）");
                    System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 重建前军区数量: " + this.SectionCount);
                }

                this.ClearSections();
                SectionAIHelper.AutoOrganizeSections(this);

                foreach (Section section in this.Sections)
                {
                    section.RefreshSectionName();
                }

                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[RebuildSectionsOptimized] 定期重建完成，军区数量: " + this.SectionCount);
                }
            }
        }
        public void ManageLogistics()
        {
#if DEBUG
            // 🔥 添加势力级物流调试
            int totalFundBefore = this.Architectures.Cast<Architecture>().Sum(a => a.Fund);
            int totalFoodBefore = this.Architectures.Cast<Architecture>().Sum(a => a.Food);
            int transportCount = 0;
#endif

            foreach (Architecture supplier in this.Architectures)
            {
                if (supplier.IsFrontline() || supplier.Fund < 30000 || supplier.Food < 200000) continue;
                if (supplier.Persons.Count == 0) continue;

                Architecture target = FindNeedyFrontlineCity(supplier);
                if (target != null)
                {
                    int goldToSend = 10000;
                    int foodToSend = 50000;
                    if (target.Fund < 2000) goldToSend += 10000;
                    if (target.Food < 50000) foodToSend += 50000;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FactionLogistics] {this.Name}: {supplier.Name} 向 {target.Name} 运输 资金={goldToSend} 粮食={foodToSend}");
                    transportCount++;
#endif

                    ExecuteTransportCommand(supplier, target, goldToSend, foodToSend);
                }
            }

#if DEBUG
            if (transportCount > 0)
            {
                int totalFundAfter = this.Architectures.Cast<Architecture>().Sum(a => a.Fund);
                int totalFoodAfter = this.Architectures.Cast<Architecture>().Sum(a => a.Food);

                System.Diagnostics.Debug.WriteLine($"[FactionLogistics] {this.Name} 物流完成: 创建{transportCount}个运输队");
                System.Diagnostics.Debug.WriteLine($"[FactionLogistics] 资源变化: 资金 {totalFundBefore}→{totalFundAfter} (差异:{totalFundAfter - totalFundBefore})");
                System.Diagnostics.Debug.WriteLine($"[FactionLogistics] 资源变化: 粮食 {totalFoodBefore}→{totalFoodAfter} (差异:{totalFoodAfter - totalFoodBefore})");
            }
#endif
        }

        private void ExecuteTransportCommand(Architecture start, Architecture end, int gold, int food)
        {
            Person transportLeader = start.GetWorstCombatOfficer();
            if (transportLeader == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ExecuteTransportCommand] {start.Name} 没有可用的运输指挥官");
#endif
                return;
            }

#if DEBUG
            int fundBefore = start.Fund;
            System.Diagnostics.Debug.WriteLine($"[ExecuteTransportCommand] {start.Name} 开始创建运输队: 资金={gold} 粮食={food} 指挥官={transportLeader.Name}");
#endif

            start.Fund -= gold;
            // Food deduction is handled by CreateTroop

            MilitaryKind kind = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(29);
            if (kind == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ExecuteTransportCommand] 找不到运输兵种 (ID:29)");
#endif
                return;
            }

            Military m = Military.Create(start, kind);
            m.Quantity = 1000;

            GameObjectList persons = new GameObjectList();
            persons.Add(transportLeader);

            Point? spawnPoint = start.GetRandomStartingPosition(m);
            if (spawnPoint == null) spawnPoint = start.Position;

            Troop transportUnit = start.CreateTroop(persons, transportLeader, m, food, spawnPoint.Value);

            if (transportUnit != null)
            {
                transportUnit.Gold = gold;
                transportUnit.TargetArchitecture = end;
                transportUnit.Operation = TroopAction.Transport;

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ExecuteTransportCommand] 运输队创建成功: {start.Name}→{end.Name} 资金消耗={fundBefore - start.Fund} 部队携带资金={transportUnit.Gold}");
#endif

                // Trigger pathfinding if needed, or rely on game engine update
                // transportUnit.SetPath(Session.Current.Scenario.GetPath(start.Position, end.Position)); 
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ExecuteTransportCommand] 运输队创建失败: {start.Name}→{end.Name}");
#endif
            }
        }

        private Architecture FindNeedyFrontlineCity(Architecture supplier)
        {
            Architecture bestTarget = null;
            int minDistance = 9999;

            foreach (Architecture city in this.Architectures)
            {
                if (city == supplier) continue;

                if (city.IsFrontline() && (city.Fund < 5000 || city.Food < 50000))
                {
                    int dist = (int)Session.Current.Scenario.GetDistance(supplier.Position, city.Position);
                    if (dist < 30 && dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = city;
                    }
                }
            }
            return bestTarget;
        }

        /// <summary>
        /// 判断势力是否需要更多部队
        /// </summary>
        /// <returns>true表示需要征兵</returns>
        public bool NeedMoreTroops()
        {
            // 简化的征兵需求判断逻辑

            // 检查军队数量是否足够
            int totalTroops = 0;
            int totalCapacity = 0;

            foreach (Architecture arch in this.Architectures)
            {
                totalTroops += arch.Militaries.Count;
                // 使用人口的20%作为征兵容量估算
                totalCapacity += arch.Population / 5;
            }

            // 如果军队数量少于容量的70%，需要征兵
            return totalTroops < totalCapacity * 0.7f;
        }

        /// <summary>
        /// 获取按内政优先级排序的城市列表
        /// 功能：为势力级AI提供城市处理优先级
        /// </summary>
        /// <param name="architectures">要排序的城市列表</param>
        /// <returns>按优先级排序的城市列表（高分在前）</returns>
        public List<Architecture> GetArchitecturesByInternalPriority(IEnumerable<Architecture> architectures)
        {
            var scoreList = new List<(Architecture arch, float score)>();

            foreach (Architecture arch in architectures)
            {
                float score = arch.CalculateInternalAffairScore();
                scoreList.Add((arch, score));
            }

            // 按分数降序排列（高分优先）
            scoreList.Sort((a, b) => b.score.CompareTo(a.score));

            return scoreList.Select(item => item.arch).ToList();
        }
        public int TotalMilitaryPopulation
        {
            get
            {
                long total = 0;
                foreach (Architecture a in this.Architectures)
                {
                     foreach (Military m in a.Militaries)
                     {
                         total += m.Quantity;
                     }
                }
                foreach (Troop t in this.Troops)
                {
                     if (t.Army != null)
                         total += t.Army.Quantity;
                }
                return (int)total;
            }
        }
        /// <summary>
        /// 获取距离指定坐标最近的己方据点
        /// </summary>
        /// <param name="position">目标坐标</param>
        /// <returns>最近的据点，如果没有则返回null</returns>
        public Architecture GetNearestArchitecture(Point position)
        {
            Architecture nearest = null;
            double minDistance = double.MaxValue;

            foreach (Architecture arch in this.Architectures)
            {
                double distance = Math.Sqrt(Math.Pow(arch.Position.X - position.X, 2) + Math.Pow(arch.Position.Y - position.Y, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = arch;
                }
            }

            return nearest;
        }

    }
}


