#nullable disable

using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.AI;
using GameObjects.Animations;
using GameObjects.ArchitectureDetail;
using GameObjects.Conditions;
using GameObjects.FactionDetail;
using GameObjects.Influences;
using GameObjects.MapDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;
using ContextMenuPlugin;
using WTKGameManager = WorldOfTheThreeKingdoms.GameManager;
using global::GameManager;
using WorldOfTheThreeKingdoms.GameData;

namespace GameObjects
{
// ... (rest of imports)

    [DataContract]
    [GenerateUIAccessor]  // 🔥 添加源生成器特性，支持右键菜单访问

    public partial class Architecture : GameObject, System.Text.Json.Serialization.IJsonOnDeserialized, WorldOfTheThreeKingdoms.GameGlobal.IPropertyAccessor
    {
        // 🔥 修复：删除重复的 KindID 属性，只保留 KindId
        [DataMember]
        public int KindID { get; set; }

        [DataMember]
        public int AreaX { get; set; }
        [DataMember]
        public int AreaY { get; set; }
        [DataMember]
        public int AreaWidth { get; set; }
        [DataMember]
        public int AreaHeight { get; set; }
// ...


        class SimulatingFightingForceComparer : IComparer<Troop>
        {
            public int Compare(Troop x, Troop y)
            {
                return y.SimulatingFightingForce - x.SimulatingFightingForce;
            }
        }

        class FightingForceComparer : IComparer<Troop>
        {
            public int Compare(Troop x, Troop y)
            {
                return y.SimulatingFightingForce - x.SimulatingFightingForce;
            }
        }

        public void Init()
        {
            AIAllLinkNodes = new Dictionary<int, LinkNode>();

            AILandLinks = new ArchitectureList();

            AILinkProcedureDetails = new Queue<AILinkProcedureDetail>();

            AIWaterLinks = new ArchitectureList();

            BeMergedMilitaryList = new MilitaryList();

            BuildableFacilityKindList = new GameObjectList();

            actuallyUnreachableArch = new HashSet<Architecture>();

            CampaignMilitaryList = new MilitaryList();
            ChangeCapitalArchitectureList = new ArchitectureList();

            IncrementNumberList = new CombatNumberItemList(CombatNumberDirection.上);

            DecrementNumberList = new CombatNumberItemList(CombatNumberDirection.下);

            Characteristics = new InfluenceTable();

            Facilities = new FacilityList();

            FundPacks = new List<FundPack>();

            FoodPacks = new List<FoodPack>();

            MergeMilitaryList = new MilitaryList();

            Militaries = new MilitaryList();

            UpgradableMilitaryKindList = new MilitaryKindList();

            Informations = new InformationList();
            LevelUpMilitaryList = new MilitaryList();

            OtherArchitectureList = new ArchitectureList();

            NewMilitaryKindList = new MilitaryKindList();

            PopulationPacks = new List<PopulationPack>();
            MilitaryPopulationPacks = new List<PopulationPack>();
            PrivateMilitaryKinds = new MilitaryKindTable();

            RecruitmentMilitaryList = new MilitaryList();
            RedeemCaptiveList = new CaptiveList();
            ResetDiplomaticRelationList = new GameObjectList();
            EnhanceDiplomaticRelationList = new GameObjectList();
            AllyDiplomaticRelationList = new GameObjectList();
            TruceDiplomaticRelationList = new GameObjectList();
            DenounceDiplomaticRelationList = new GameObjectList();
            QuanXiangDiplomaticRelationList = new GameObjectList(); //劝降
            GeDiDiplomaticRelationList = new GameObjectList(); //割地
            RewardPersonList = new PersonList();
            RoutewayDestinationArchitectures = new Dictionary<int, Architecture>();
            RoutewayProcedures = new Queue<RoutewayProcedureDetail>();

            Routeways = new RoutewayList();

            ShelledMilitaryList = new MilitaryList();
            TrainingMilitaryList = new MilitaryList();
            TransferArchitectureList = new ArchitectureList();

            linkNodeRouteway = new Dictionary<LinkNode, Routeway>();

            //MayorID = -1;
            //buildingFacility = -1;
            PathRoutewayID = -1;

            pathFinder = new RoutewayPathFinder();

            CombativityOfRecruitment = 50;
            MoraleOfRecruitment = 50;

            DayLearnTitleDay = Session.Parameters.LearnTitleDays;

            MultipleOfRecovery = 1;
            MultipleOfTraining = 1;

            RateOfClearField = 1;
            RateOfConvincePerson = 1;
            RateOfDestroyArchitecture = 1;
            RateOfFacilityEnduranceDown = 1;
            RateOfArchitectureCounterDamage = 1;
            RateOfFoodReduceRate = 1;
            RateOfGossipArchitecture = 1;
            RateOfHirePerson = 1;
            RateOfInstigateArchitecture = 1;
            RateOfJailBreakArchitecture = 1;
            RateOfNewBubingMilitaryFundCost = 1;
            RateOfNewNubingMilitaryFundCost = 1;
            RateOfNewQibingMilitaryFundCost = 1;
            RateOfNewQixieMilitaryFundCost = 1;
            RateOfNewShuijunMilitaryFundCost = 1;
            RateOfPublic = 1;
            RateOfRewardPerson = 1;
            RateOfRoutewayBuildFundCost = 1;

            disasterChanceDecrease = new Dictionary<int, int>();
            disasterChanceIncrease = new Dictionary<int, int>();
            disasterDamageRateDecrease = new Dictionary<int, float>();

        }

        // public int[] preferredOfficialTypes = {100, 100, 100, 100, 60, 100, 1, 250, 250, 39 };

        private Person mayor = null;



        //[DataMember]
        //public int mayorID = -1;

        private int militaryPopulation = 0;

        [DataMember]
        public bool TodayPersonArriveNote = false;
        
        // UI Tuner property
        public Point NameDisplayOffset { get; set; } = new Point(0, 0);

        private int agriculture;

        [DataMember]
        public int CaptionID = 0;
        //private bool shoudongluyongshibai=false;

        [DataMember]
        public bool HasManualHire = false;

        public Dictionary<int, LinkNode> AIAllLinkNodes = new Dictionary<int, LinkNode>();

        [DataMember]
        public string AILandLinksString;

        [DataMember]
        public string AIWaterLinksString;

        [DataMember]
        public string ArchitectureAreaString { get; set; }

        private bool autoHiring;

        public bool AutoRefillFoodInLongViewArea;
        private bool autoRewarding;
        private bool autoSearching;
        private bool autoZhaoXian;
        private bool autoWorking;
        private bool autoRecruiting;
        private GameArea baseFoodSurplyArea;

        public int ChanceDecrementOfCriticalStrike = 0;

        public Faction BelongedFaction = null;
        [DataMember]
        public int BelongedFactionID { get; set; } = -1;
        
        public bool AutoRecommend
        {
            get
            {
                return this.BelongedFaction != null && this.BelongedFaction.IsAdvisorRecommendationEnabled;
            }
        }



        public Section BelongedSection = null;

        [DataMember]
        [JsonInclude]
        public int BelongedSectionID { get; set; } = -1;

        public ArchitectureList AILandLinks = new ArchitectureList();

        private Queue<AILinkProcedureDetail> AILinkProcedureDetails = new Queue<AILinkProcedureDetail>();

        public ArchitectureList AIWaterLinks = new ArchitectureList();

        // 🔥 将ArchitectureArea字段改为属性，以便追踪赋值和检测共享引用问题
        private GameArea _architectureArea = new GameArea();
        
        public GameArea ArchitectureArea
        {
            get 
            { 
                // 🔥 严格修复：确保规模与ArchitectureAreaString完全对应
                if (_architectureArea == null || 
                    _architectureArea.Area == null || 
                    _architectureArea.Area.Count == 0)
                {
                    _architectureArea = new GameArea();
                    if (!string.IsNullOrEmpty(ArchitectureAreaString))
                    {
                        LoadFromString(_architectureArea, ArchitectureAreaString);
                    }
                }
                else if (!string.IsNullOrEmpty(ArchitectureAreaString))
                {
                    // 🔥 验证一致性：实际规模必须与字符串匹配
                    var expectedCount = ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                    if (_architectureArea.Area.Count != expectedCount)
                    {
                        #if DEBUG
                        // 详细调试信息，帮助找到根本原因
                        System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} 规模不一致，重建: 期望{expectedCount}, 当前{_architectureArea.Area.Count}");
                        System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} ArchitectureAreaString: '{ArchitectureAreaString}'");
                        System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} _architectureArea.Area内容:");
                        for (int i = 0; i < _architectureArea.Area.Count; i++)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{i}]: {_architectureArea.Area[i]}");
                        }
                        
                        // 🔥 添加调用堆栈以找到问题源头
                        var stackTrace = new System.Diagnostics.StackTrace(true);
                        System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} 调用堆栈:");
                        for (int i = 0; i < Math.Min(10, stackTrace.FrameCount); i++)
                        {
                            var frame = stackTrace.GetFrame(i);
                            System.Diagnostics.Debug.WriteLine(frame?.ToString()?.Trim() ?? "at <unknown frame>");
                        }
                        #endif
                        
                        // 强制重建以确保一致性
                        _architectureArea = new GameArea();
                        LoadFromString(_architectureArea, ArchitectureAreaString);
                        
                        #if DEBUG
                        // 验证重建后是否修复了问题
                        if (_architectureArea.Area.Count != expectedCount)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} 重建后仍然不一致: 期望{expectedCount}, 实际{_architectureArea.Area.Count}");
                            System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} 这表明 LoadFromString 或 ArchitectureAreaString 有问题");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ArchitectureArea] {Name} 重建成功，规模已修复为 {_architectureArea.Area.Count}");
                        }
                        #endif
                    }
                }
                
                return _architectureArea; 
            }
            set
            {
                _architectureArea = value;
            }
        }

        public MilitaryList BeMergedMilitaryList = new MilitaryList();

        public GameObjectList BuildableFacilityKindList = new GameObjectList();

        public HashSet<Architecture> actuallyUnreachableArch = new HashSet<Architecture>();

        private int buildingDaysLeft;
        //private int buildingFacility = -1;

        public MilitaryList CampaignMilitaryList = new MilitaryList();

        public ArchitectureList ChangeCapitalArchitectureList = new ArchitectureList();

        public CombatNumberItemList IncrementNumberList = new CombatNumberItemList(CombatNumberDirection.上);

        public CombatNumberItemList DecrementNumberList = new CombatNumberItemList(CombatNumberDirection.下);

        public InfluenceTable Characteristics = new InfluenceTable();

        public ArchitectureList ClosestArchitectures;

        public FacilityList Facilities = new FacilityList();

        public List<FundPack> FundPacks = new List<FundPack>();

        public List<FoodPack> FoodPacks = new List<FoodPack>();

        public MilitaryList MergeMilitaryList = new MilitaryList();

        public MilitaryList Militaries = new MilitaryList();

        public MilitaryKindList UpgradableMilitaryKindList = new MilitaryKindList();

        public InformationList Informations = new InformationList();
        public MilitaryList LevelUpMilitaryList = new MilitaryList();

        [DataMember]
        public string CharacteristicsString { get; set; }

        public int CombativityOfRecruitment = 50;
        private int commerce;
        private GameArea contactArea;

        public bool CriticalHostile;
        public bool DayAvoidInfluenceByBattle;
        public bool DayAvoidInternalDecrementOnBattle;
        public bool DayAvoidPopulationEscape;
        public int DayLearnTitleDay = Session.Parameters.LearnTitleDays;
        public bool DayLocationLoyaltyNoChange;
        public float DayRateIncrementOfInternal;

        //[DataMember]
        public Legion DefensiveLegion;
        [DataMember]
        public int DefensiveLegionID;
        private int domination;
        private int endurance;

        [DataMember]
        public string FacilitiesString { get; set; }

        private bool facilityEnabled;
        private int food;
        public bool FrontLine;
        private int fund;

        [DataMember]
        public string FundPacksString { get; set; }

        [DataMember]
        public string FoodPacksString { get; set; }

        private bool hireFinished;
        public bool HostileLine;

        public int IncrementOfAgricultureCeiling;
        public int IncrementOfAgriculturePerDay;
        public int IncrementOfCombativityInViewArea;
        public int IncrementOfCommerceCeiling;
        public int IncrementOfCommercePerDay;
        public int IncrementOfDominationPerDay;
        public int IncrementOfEnduranceCeiling;
        public int IncrementOfDominationCeiling;
        public int IncrementOfMoraleCeiling;
        public int IncrementOfEndurancePerDay;
        public int IncrementOfFacilityPositionCount;
        public int IncrementOfFactionReputationPerDay;
        public int IncrementOfFactionTechniquePointPerDay;
        public int IncrementOfMonthFood;
        public int IncrementOfMonthFund;
        public int IncrementOfMoralePerDay;
        public int IncrementOfTechnologyCeiling;
        public int IncrementOfTechnologyPerDay;
        public int IncrementOfViewRadius;
        public int IncrementOfFundCeiling = 0;
        public int IncrementOfFoodCeiling = 0;

        [DataMember]
        public string InformationsString { get; set; }


        private bool isStrategicCenter;

        public bool JustAttacked = false;

        private ArchitectureKind _architectureKind;

        /// <summary>
        /// 建筑类型
        /// </summary>
        public ArchitectureKind Kind
        {
            get => _architectureKind;
            set
            {
                // 诊断：追踪Kind被设置为null的情况
                if (value == null && _architectureKind != null)
                {
                    var stackTrace = new System.Diagnostics.StackTrace(true);
                    System.Diagnostics.Debug.WriteLine($"[Architecture.Kind] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 被设置为 null！原值: {_architectureKind.Name}");
                    System.Diagnostics.Debug.WriteLine($"[Architecture.Kind] 调用堆栈:\n{stackTrace}");
                }
                _architectureKind = value;
            }
        }

        public bool HasPort
        {
            get { return this.Kind != null && this.Kind.HasHarbor; }
        }


        [DataMember]
        public int StateID { get; set; }

        public State LocationState;

        private GameArea longViewArea = null;

        [DataMember]
        [JsonIgnore]  // 🔥 使用 MilitariesString_Legacy 代替
        public string MilitariesString { get; set; }

        private int morale;

        public int MoraleOfRecruitment = 50;
        public float MultipleOfRecovery = 1;
        public float MultipleOfTraining = 1;

        public ArchitectureList OtherArchitectureList = new ArchitectureList();

        public MilitaryKindList NewMilitaryKindList = new MilitaryKindList();

        public bool NoCounterStrikeInArchitecture;
        public bool orientationFrontLine;


        private int PathRoutewayID = -1;

        public Architecture PlanArchitecture;

        [DataMember]
        public int PlanArchitectureID;

        public FacilityKind PlanFacilityKind;

        [DataMember]
        public int PlanFacilityKindID;
        private int population;

        [DataMember]
        public string PopulationPacksString { get; set; }

        [DataMember]
        public string MilitaryPopulationPacksString { get; set; }

        public List<PopulationPack> PopulationPacks = new List<PopulationPack>();
        public List<PopulationPack> MilitaryPopulationPacks = new List<PopulationPack>();
        public MilitaryKindTable PrivateMilitaryKinds = new MilitaryKindTable();

        public float RateIncrementOfPopulationCeiling;
        public float RateIncrementOfMonthFood;
        public float RateIncrementOfMonthFund;
        public float RateIncrementOfNewBubingTroopDefence;
        public float RateIncrementOfNewBubingTroopOffence;
        public float RateIncrementOfNewNubingTroopDefence;
        public float RateIncrementOfNewNubingTroopOffence;
        public float RateIncrementOfNewQibingTroopDefence;
        public float RateIncrementOfNewQibingTroopOffence;
        public float RateIncrementOfNewQixieTroopDefence;
        public float RateIncrementOfNewQixieTroopOffence;
        public float RateIncrementOfNewShuijunTroopDefence;
        public float RateIncrementOfNewShuijunTroopOffence;
        public double RateIncrementOfPopulationDevelop;
        public float RateOfClearField = 1f;
        public float RateOfConvincePerson = 1f;
        public float RateOfDestroyArchitecture = 1f;
        public float RateOfFacilityEnduranceDown = 1f;
        public float RateOfArchitectureCounterDamage = 1f;
        public float RateOfFoodReduceRate = 1f;
        public float RateOfGossipArchitecture = 1f;
        public float RateOfJailBreakArchitecture = 1f;
        public float RateOfHirePerson = 1f;
        public float RateOfInstigateArchitecture = 1f;
        public float RateOfPublic = 1f;
        public float RateOfNewBubingMilitaryFundCost = 1f;
        public float RateOfNewNubingMilitaryFundCost = 1f;
        public float RateOfNewQibingMilitaryFundCost = 1f;
        public float RateOfNewQixieMilitaryFundCost = 1f;
        public float RateOfNewShuijunMilitaryFundCost = 1f;
        public float RateOfRewardPerson = 1f;
        public float RateOfRoutewayBuildFundCost = 1f;
        // public float RateOfSpyArchitecture = 1f;
        [DataMember]
        public int RecentlyAttacked;
        [DataMember]
        public int RecentlyHit;
        [DataMember]
        public int RecentlyBreaked;

        public MilitaryList RecruitmentMilitaryList = new MilitaryList();
        public CaptiveList RedeemCaptiveList = new CaptiveList();
        public GameObjectList ResetDiplomaticRelationList = new GameObjectList();
        public GameObjectList EnhanceDiplomaticRelationList = new GameObjectList();
        public GameObjectList AllyDiplomaticRelationList = new GameObjectList();
        public GameObjectList TruceDiplomaticRelationList = new GameObjectList();
        public GameObjectList DenounceDiplomaticRelationList = new GameObjectList();
        public GameObjectList QuanXiangDiplomaticRelationList = new GameObjectList(); //劝降
        public GameObjectList GeDiDiplomaticRelationList = new GameObjectList(); //割地
        public PersonList RewardPersonList = new PersonList();

        public Troop RobberTroop;

        [DataMember]
        public int RobberTroopID;

        private Dictionary<int, Architecture> RoutewayDestinationArchitectures = new Dictionary<int, Architecture>();
        private Queue<RoutewayProcedureDetail> RoutewayProcedures = new Queue<RoutewayProcedureDetail>();

        public RoutewayList Routeways = new RoutewayList();

        public MilitaryList ShelledMilitaryList = new MilitaryList();
        private bool showNumber;
       // public List<SpyPack> SpyPacks = new List<SpyPack>();
        private float surplusRate;
        private int technology;
        //public SpyMessage TodayNewMilitarySpyMessage;
        // public SpyMessage TodayNewTroopSpyMessage;

        public int TotalStoredForce;
        public int TotalFriendlyForce;
        public int TotalHostileForce;

        public MilitaryList TrainingMilitaryList = new MilitaryList();
        public ArchitectureList TransferArchitectureList = new ArchitectureList();
        public Architecture TransferFoodArchitecture;

        [DataMember]
        public int TransferFoodArchitectureID;

        public Architecture TransferFundArchitecture;
        [DataMember]
        public int TransferFundArchitectureID;
        [DataMember]
        public bool TroopershipAvailable;
        private GameArea viewArea = null;
        [DataMember]
        public zainanlei zainan = new zainanlei();
        public PlatformTexture CaptionTexture;

        public bool noFactionFrontline;
        public int captureChance;
        public int noEscapeChance;
        [DataMember]
        public List<KeyValuePair<int, int>> captiveLoyaltyFall = new List<KeyValuePair<int, int>>();
        [DataMember]
        public bool noFundToSustainFacility;
        public int facilityEnduranceIncrease;
        public Dictionary<int, int> disasterChanceDecrease = new Dictionary<int, int>();
        public Dictionary<int, int> disasterChanceIncrease = new Dictionary<int, int>();
        public Dictionary<int, float> disasterDamageRateDecrease = new Dictionary<int, float>();
        public float militaryPopulationRateIncrease;
        public float enduranceDecreaseRateDrop;

        public bool hostileTroopInViewLastDay = false;
        
        // 🔥 新增：城市占领日期（用于识别新占领城市）
        [DataMember]
        private GameDate _occupiedDate = new(0, 1, 1);
        
        [DataMember]
        public int SuspendTroopTransfer;
        public bool withoutTruceFrontline;
        public float ExperienceRate;
        public int InfluenceIncrementOfLoyalty;
        public int CommandExperienceIncrease { get; set; }
        public int StrengthExperienceIncrease { get; set; }
        public int IntelligenceExperienceIncrease { get; set; }
        public int PoliticsExperienceIncrease { get; set; }
        public int GlamourExperienceIncrease { get; set; }
        public int ReputationIncrease { get; set; }
        public float TroopTransportDayRate { get; set; }
        public float TroopTransportFundRate { get; set; }
        public float TroopTransportFoodRate { get; set; }

        public ArchitectureList AIBattlingArchitectures { get; set; }
        
        // 🔥 兼容性处理：从旧格式剧本文件加载PersonsString
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("PersonsString")]
        public string PersonsString_Legacy
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    PersonIDs = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : -1)
                        .Where(id => id >= 0)
                        .ToList();
                    
                    #if DEBUG
                    if (this.ID == 0)  // 洛阳
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonsString_Legacy] 洛阳(ID=0) setter 被调用，转换了 {PersonIDs.Count} 个武将ID");
                    }
                    #endif
                }
            }
        }
        
        [DataMember]
        [JsonInclude]
        public List<int> PersonIDs { get; set; } = new List<int>();

        // 🔥 兼容性处理：从旧格式剧本文件加载MilitariesString
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("MilitariesString")]
        public string MilitariesString_Legacy
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    MilitaryIDs = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : -1)
                        .Where(id => id >= 0)
                        .ToList();
                    // 延迟日志输出，避免Name为null
                    // System.Diagnostics.Debug.WriteLine($"[Architecture] {Name}(ID:{ID}) 从 MilitariesString 转换了 {MilitaryIDs.Count} 个部队ID");
                }
            }
        }

        [DataMember]
        [JsonInclude]
        public List<int> MilitaryIDs { get; set; } = new List<int>();

        [DataMember]
        [JsonInclude]
        public List<int> FacilityIDs { get; set; } = new List<int>();

        [DataMember]
        public int MayorOnDutyDays {get;set;}

        public float CommandTrainingFacilityRate { get; set; }
        public float StrengthTrainingFacilityRate { get; set; }
        public float IntelligenceTrainingFacilityRate { get; set; }
        public float PoliticsTrainingFacilityRate { get; set; }
        public float GlamourTrainingFacilityRate { get; set; }
        public float InfantryTrainingFacilityRate { get; set; }
        public float CavalryTrainingFacilityRate { get; set; }
        public float BowmanTrainingFacilityRate { get; set; }
        public float NavalTrainingFacilityRate { get; set; }
        public float SiegeTrainingFacilityRate { get; set; }

        // public OngoingBattle Battle { get; set; }

        private String oldFactionName = "";
        [DataMember]
        public String OldFactionName
        {
            get
            {
                if (oldFactionName == null || oldFactionName.Equals(""))
                {
                    if (this.BelongedFaction == null)
                    {
                        return "";
                    }
                    return this.BelongedFaction.Name;
                }
                return oldFactionName;
            }
            set
            {
                oldFactionName = value;
            }
        }
        public float facilityConstructionTimeRateDecrease = 0;

#pragma warning disable CS0067 // The event 'Architecture.OnBeginRecentlyAttacked' is never used
        public event BeginRecentlyAttacked OnBeginRecentlyAttacked;
#pragma warning restore CS0067 // The event 'Architecture.OnBeginRecentlyAttacked' is never used

        public event FacilityCompleted OnFacilityCompleted;

        public event fashengzainan Onfashengzainan;

#pragma warning disable CS0067 // The event 'Architecture.OnHirePerson' is never used
        public event HirePerson OnHirePerson;
#pragma warning restore CS0067 // The event 'Architecture.OnHirePerson' is never used

        public event MilitaryCreate OnMilitaryCreate;

        public event PopulationEnter OnPopulationEnter;

        public event PopulationEscape OnPopulationEscape;

        public event ReleaseCaptiveAfterOccupied OnReleaseCaptiveAfterOccupied;

#pragma warning disable CS0067 // The event 'Architecture.OnRewardPersons' is never used
        public event RewardPersons OnRewardPersons;
#pragma warning restore CS0067 // The event 'Architecture.OnRewardPersons' is never used


        public CaptiveList Captives
        {
            get
            {
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    var emptyList = new CaptiveList();
                    emptyList.SetImmutable();
                    return emptyList;
                }
                
                CaptiveList p = Session.Current.Scenario.GetCaptiveList(this);
                p.SetImmutable();
                foreach (Captive c in p) //禁止俘虏自势力武将
                {
                    if (c.CaptiveFaction == c.BelongedFaction)
                    {
                        c.CaptivePerson.SetBelongedCaptive(null, GameObjects.PersonDetail.PersonStatus.Normal);
                    }
                }
                return p;
            }
        }

        public PersonList AllPersonAndChildren
        {
            get
            {
                PersonList result = new PersonList();
                
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    return result;
                }
                
                GameObjectList p = Session.Current.Scenario.GetPersonList(this).GetList();

                HashSet<Person> resultSet = new HashSet<Person>();
                foreach (Person q in p)
                {
                    resultSet.Add(q);
                    foreach (Person r in q.ChildrenList)
                    {
                        resultSet.Add(r);
                    }
                }

                GameObjectList p2 = Session.Current.Scenario.GetMovingPersonList(this).GetList();
                foreach (Person q in p2)
                {
                    resultSet.Add(q);
                    foreach (Person r in q.ChildrenList)
                    {
                        resultSet.Add(r);
                    }
                }

                GameObjectList p3 = Session.Current.Scenario.GetNoFactionPersonList(this).GetList();
                foreach (Person q in p3)
                {
                    resultSet.Add(q);
                    foreach (Person r in q.ChildrenList)
                    {
                        resultSet.Add(r);
                    }
                }

                GameObjectList p4 = Session.Current.Scenario.GetNoFactionMovingPersonList(this).GetList();
                foreach (Person q in p4)
                {
                    resultSet.Add(q);
                    foreach (Person r in q.ChildrenList)
                    {
                        resultSet.Add(r);
                    }
                }

                foreach (Captive q in this.Captives)
                {
                    resultSet.Add(q.CaptivePerson);
                }
                
                foreach (Person person in resultSet)
                {
                    result.Add(person);
                }

                return result;
            }
        }

        public PersonList PersonAndChildren
        {
            get
            {
                PersonList result2 = new PersonList();
                
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    result2.SetImmutable();
                    return result2;
                }
                
                GameObjectList personList = Session.Current.Scenario.GetPersonList(this).GetList();
                HashSet<Person> result = new HashSet<Person>();
                foreach (Person q in personList)
                {
                    result.Add(q);
                    foreach (Person r in q.ChildrenList)
                    {
                        result.Add(r);
                    }
                }
                foreach (Person q in this.Feiziliebiao)
                {
                    result.Add(q);
                }
                foreach (Person q in result)
                {
                    result2.Add(q);
                }
                result2.SetImmutable();
                return result2;
            }
        }

        public PersonList Children
        {
            get
            {
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    var emptyList = new PersonList();
                    emptyList.SetImmutable();
                    return emptyList;
                }
                
                GameObjectList personList = Session.Current.Scenario.GetPersonList(this).GetList();
                HashSet<Person> result = new HashSet<Person>();
                foreach (Person q in personList)
                {
                    foreach (Person r in q.ChildrenList)
                    {
                        result.Add(r);
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

        public PersonList Persons
        {
            get
            {
                // 🔥 防御性检查：在序列化/反序列化过程中，Session可能还没有初始化
                if (Session.Current?.Scenario == null)
                {
                    // 返回空列表而不是崩溃
                    var emptyList = new PersonList();
                    emptyList.SetImmutable();
                    return emptyList;
                }
                
                PersonList p = Session.Current.Scenario.GetPersonList(this);
                p.SetImmutable();
                return p;
            }
        }

        public PersonList PersonsExcludeNvGuan
        {
            get
            {
                // 🔥 防御性检查：在序列化/反序列化过程中，Session可能还没有初始化
                if (Session.Current?.Scenario == null)
                {
                    var emptyList = new PersonList();
                    emptyList.SetImmutable();
                    return emptyList;
                }
                
                PersonList all = Session.Current.Scenario.GetPersonList(this);
                PersonList result = new PersonList();

                foreach (Person p in all)
                {
                    if (!p.NvGuan)
                    {
                        result.Add(p);
                    }
                }

                result.SetImmutable();
                return result;
            }
        }

        public PersonList NvGuans
        {
            get
            {
                PersonList result = new PersonList();
                
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    result.SetImmutable();
                    return result;
                }
                
                PersonList all = Session.Current.Scenario.GetPersonList(this);

                foreach (Person p in all)
                {
                    if (p.NvGuan)
                    {
                        result.Add(p);
                    }
                }

                result.SetImmutable();
                return result;
            }
        }

        public PersonList PromotableNvGuans
        {
            get
            {
                PersonList result = new PersonList();
                
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    result.SetImmutable();
                    return result;
                }
                
                PersonList all = Session.Current.Scenario.GetPersonList(this);

                foreach (Person p in all)
                {
                    if (p.NvGuanPromotable)
                    {
                        result.Add(p);
                    }
                }

                result.SetImmutable();
                return result;
            }
        }

        public bool PromoteNvGuanAvail()
        {
            return this.PromotableNvGuans.Count > 0;
        }

        public PersonList MovablePersons
        {
            get
            {
                return PersonsExcludeNvGuan;
            }
        }

        // 🔥 辅助方法：安全获取PersonList，处理Session未初始化的情况
        private PersonList GetPersonListSafe(Func<GameScenario, Architecture, PersonList> getter)
        {
            if (Session.Current?.Scenario == null)
            {
                var emptyList = new PersonList();
                emptyList.SetImmutable();
                return emptyList;
            }
            
            PersonList p = getter(Session.Current.Scenario, this);
            p.SetImmutable();
            return p;
        }

        public PersonList MovingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetMovingPersonList(arch));
            }
        }

        public PersonList NoFactionPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetNoFactionPersonList(arch));
            }
        }

        public PersonList NoFactionMovingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetNoFactionMovingPersonList(arch));
            }
        }

        public PersonList Feiziliebiao
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetPrincessPersonList(arch));
            }
        }

        public PersonList ZhenzaiWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetZhenzaiPersonList(arch));
            }
        }

        public PersonList AgricultureWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetAgriculturePersonList(arch));
            }
        }

        public PersonList CommerceWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetCommercePersonList(arch));
            }
        }

        public PersonList TechnologyWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetTechnologyPersonList(arch));
            }
        }

        public PersonList DominationWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetDomintaionPersonList(arch));
            }
        }

        public PersonList MoraleWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetMoralePersonList(arch));
            }
        }

        public PersonList EnduranceWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetEndurancePersonList(arch));
            }
        }

        public PersonList TrainingWorkingPersons
        {
            get
            {
                return GetPersonListSafe((scenario, arch) => scenario.GetTrainingPersonList(arch));
            }
        }

        public PersonList DiplomaticWorkingPersons
        {
            get
            {
                PersonList result = new PersonList();
                
                // 🔥 防御性检查：Session未初始化时返回空列表
                if (Session.Current?.Scenario == null)
                {
                    return result;
                }
                
                foreach (Person i in Session.Current.Scenario.Persons)
                {
                    if (i.Status == PersonStatus.Normal && i.LocationArchitecture == this && i.LocationTroop == null && !this.BelongedFaction.MayorList.HasGameObject(i))
                    {
                        result.Add(i);
                    }
                }
                return result;
            }
        }

        public MilitaryList ZhengzaiBuchongDeBiandui()
        {
            MilitaryList zhengzaiBuchongDeBiandui = new MilitaryList();
            foreach (Military military in this.Militaries)
            {
                if (military.RecruitmentPerson != null)
                {
                    zhengzaiBuchongDeBiandui.AddMilitary(military);
                }
            }
            return zhengzaiBuchongDeBiandui;
        }
        
        /// <summary>
        /// 判断是否为新占领城市（30天内）
        /// </summary>
        public bool IsRecentlyOccupied()
        {
            // 未记录占领时间，视为旧城市
            if (_occupiedDate == null) return false;
            
            // 🔥 ANTI-BAND-AID：假设游戏状态有效，如果无效让它崩溃
            GameDate currentDate = Session.Current.Scenario.Date;
            
            // 计算天数差异
            int daysSinceOccupied = CalculateDaysDifference(_occupiedDate, currentDate);
            return daysSinceOccupied <= 30;
        }
        
        /// <summary>
        /// 计算两个GameDate之间的天数差异
        /// </summary>
        private int CalculateDaysDifference(GameDate fromDate, GameDate toDate)
        {
            // 如果fromDate是初始值(0年1月1日)，返回一个大数值确保视为旧城市
            if (fromDate.Year == 0 && fromDate.Month == 1 && fromDate.Day == 1)
            {
                return 9999;
            }
            
            // 简化计算：年*360 + 月*30 + 日
            int fromDays = fromDate.Year * 360 + fromDate.Month * 30 + fromDate.Day;
            int toDays = toDate.Year * 360 + toDate.Month * 30 + toDate.Day;
            
            return toDays - fromDays;
        }
        
        [DataMember]
        public int MilitaryPopulation
        {
            get
            {
                return this.militaryPopulation;
            }
            set
            {
                this.militaryPopulation = value;
            }
        }

        private void AddAllAILink(int level, int levelMax, Architecture root, List<Architecture> path)
        {
            path.Add(root);
            if (root != this)
            {
                double num = 0.0;
                for (int i = 1; i < path.Count; i++)
                {
                    num += Session.Current.Scenario.GetDistance(path[i - 1].ArchitectureArea, path[i].ArchitectureArea);
                }
                if (!this.AIAllLinkNodes.ContainsKey(root.ID))
                {
                    LinkNode node = new LinkNode();
                    node.A = root;
                    node.Level = level;
                    foreach (Architecture architecture in path)
                    {
                        node.Path.Add(architecture);
                    }
                    node.Distance = num;
                    this.AIAllLinkNodes.Add(root.ID, node);
                }
                else if ((this.AIAllLinkNodes[root.ID].Level == level) && (this.AIAllLinkNodes[root.ID].Distance > num))
                {
                    this.AIAllLinkNodes[root.ID].Distance = num;
                    this.AIAllLinkNodes[root.ID].Path.Clear();
                    foreach (Architecture architecture in path)
                    {
                        this.AIAllLinkNodes[root.ID].Path.Add(architecture);
                    }
                }
                else if (this.AIAllLinkNodes[root.ID].Level > level)
                {
                    this.AIAllLinkNodes[root.ID].Level = level;
                    this.AIAllLinkNodes[root.ID].Path.Clear();
                    foreach (Architecture architecture in path)
                    {
                        this.AIAllLinkNodes[root.ID].Path.Add(architecture);
                    }
                }
            }
            if (level < levelMax)
            {
                foreach (Architecture architecture in root.GetAILinks())
                {
                    this.AILinkProcedureDetails.Enqueue(new AILinkProcedureDetail(level + 1, architecture, path));
                }
            }
        }

        public String BuildingFacilityName
        {
            get
            {
                int type = BuildingFacility;
                GameObjectList fkl = Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKindList();

                foreach (FacilityKind i in fkl)
                {
                    if (type == i.ID)
                    {
                        return i.Name;
                    }
                }
                return "-";
            }
        }

        public String SheshiMiaoshu
        {
            get
            {
                int type = BuildingFacility;
                GameObjectList fkl = Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKindList();

                foreach (FacilityKind i in fkl)
                {
                    if (type == i.ID)
                    {
                        return i.Name;
                    }
                }

                if (this.FacilityPositionCount > 0 && this.FacilityPositionLeft <= 0)
                {
                    return "已建满";
                }

                return this.FacilityPositionString;
            }
        }



        public void AddBaseSupplyingArchitecture()
        {
            foreach (Point point in this.BaseFoodSurplyArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    Session.Current.Scenario.MapTileData[point.X, point.Y].AddSupplyingArchitecture(this);
                }
            }
        }

        private void AddCloseRoutewayDestinationArchitectures(Architecture a, float previousrate)
        {
            foreach (Routeway routeway in a.Routeways)
            {
                float minRate = 1f;
                if ((routeway.EndArchitecture != null) && routeway.IsActiveInArea(routeway.EndArchitecture.GetRoutewayStartArea(), out minRate))
                {
                    float rate = previousrate * (1f - (minRate * this.BelongedFaction.RateOfFoodTransportBetweenArchitectures));
                    if (rate > routeway.EndArchitecture.surplusRate)
                    {
                        routeway.EndArchitecture.surplusRate = rate;
                        routeway.EndArchitecture.PathRoutewayID = routeway.ID;
                        if (!this.RoutewayDestinationArchitectures.ContainsKey(routeway.EndArchitecture.ID))
                        {
                            this.RoutewayDestinationArchitectures.Add(routeway.EndArchitecture.ID, routeway.EndArchitecture);
                        }
                        this.RoutewayProcedures.Enqueue(new RoutewayProcedureDetail(routeway.EndArchitecture, rate));
                    }
                }
            }
        }

        public void AddFundPack(int number, int days)
        {
            if (number > 0)
            {
                FundPack item = new FundPack(number, days);
                this.FundPacks.Add(item);
            }
        }

        public void AddFoodPack(int number, int days)
        {
            if (number > 0)
            {
                FoodPack item = new FoodPack(number, days);
                this.FoodPacks.Add(item);
            }
        }

        /*
        private void AddMessageToTodayMilitaryScaleSpyMessage(Military m)
        {
            this.CreateMilitaryScaleSpyMessage(m);
        }

        private void AddMessageToTodayNewMilitarySpyMessage(Military m)
        {
            if (this.TodayNewMilitarySpyMessage == null)
            {
                this.TodayNewMilitarySpyMessage = this.CreateNewMilitarySpyMessage(m);
            }
            else
            {
                this.TodayNewMilitarySpyMessage.Message3 = this.TodayNewMilitarySpyMessage.Message3 + "," + m.Name;
            }
        }

        private void AddMessageToTodayNewTroopSpyMessage(Troop t, bool hand)
        {
            if (this.TodayNewTroopSpyMessage == null)
            {
                this.TodayNewTroopSpyMessage = this.CreateNewTroopSpyMessage(t, hand);
            }
            else
            {
                this.TodayNewTroopSpyMessage.Message3 = this.TodayNewTroopSpyMessage.Message3 + "," + t.DisplayName;
            }
        }
        */

        public void AddMilitary(Military military)
        {
            this.Militaries.AddMilitary(military);
            military.BelongedArchitecture = this;
        }

        private PersonList SelectSubOfficersToTroop(Troop t)
        {
            PersonList result = new PersonList();
            result.Add(t.Leader);
            if (t.TroopIntelligence < (75 - t.Leader.Calmness))
            {
                foreach (Person person in this.MovablePersons)
                {
                    if (person.WaitForFeiZi != null) continue;
                    if (!person.Selected && person.Intelligence >= 75 - t.Leader.Calmness && !t.Persons.HasGameObject(person) && t.Leader.Character.IntelligenceRate >= 0.75f &&
                        person.Strength < t.TroopStrength && person.Intelligence - t.TroopIntelligence >= 10 && person.FightingForce < t.Leader.FightingForce && !person.HasLeaderValidTitle)
                    {
                        person.Selected = true;
                        result.Add(person);
                        break;
                    }
                }
            }
            if (t.TroopStrength < 75)
            {
                foreach (Person person in this.MovablePersons)
                {
                    if (person.WaitForFeiZi != null) continue;
                    if (!person.Selected && person.Strength >= 75 && !t.Persons.HasGameObject(person) && person.Closes(t.Leader) &&
                        person.Strength - t.TroopStrength >= 10 && person.FightingForce < t.Leader.FightingForce && !person.HasLeaderValidTitle)
                    {
                        person.Selected = true;
                        result.Add(person);
                        break;
                    }
                }
            }
            if (t.TroopCommand < 75)
            {
                foreach (Person person in this.MovablePersons)
                {
                    if (person.WaitForFeiZi != null) continue;
                    if (!person.Selected && person.Command >= 75 && !t.Persons.HasGameObject(person) && person.Closes(t.Leader) &&
                        person.Command - t.TroopCommand >= 10 && person.FightingForce < t.Leader.FightingForce && !person.HasLeaderValidTitle)
                    {
                        person.Selected = true;
                        result.Add(person);
                        break;
                    }
                }
            }

            if (this.MovablePersons.HasGameObject(t.Leader.Spouse))
            {
                if ((!t.Leader.Spouse.Selected && !t.Persons.HasGameObject(t.Leader.Spouse)) && ((t.Leader.Spouse.FightingForce < t.Leader.FightingForce)) && !t.Leader.Spouse.HasLeaderValidTitle)
                {
                    t.Leader.Spouse.Selected = true;
                    result.Add(t.Leader.Spouse);
                }
            }

            GameObjectList pl = this.MovablePersons;
            pl.PropertyName = "SubFightingForce";
            pl.IsNumber = true;
            pl.SmallToBig = false;
            pl.ReSort();
            int personCount = this.PersonCount;
            foreach (Person person in pl)
            {
                if (person.WaitForFeiZi != null) continue;
                if (person.SubFightingForce < t.Leader.FightingForce * result.Count / Math.Min(3, personCount / this.MilitaryCount + 1)) break;
                if (!person.Selected && !t.Persons.HasGameObject(person) && person.FightingForce < t.Leader.FightingForce &&
                    !person.HasLeaderValidTitle && person.HasSubofficerValidTitle)
                {
                    person.Selected = true;
                    result.Add(person);
                }
            }
            /*
            foreach (Person person in this.MovablePersons)
            {
                if (person.WaitForFeiZi != null) continue;
                if ((!person.Selected && !t.Persons.HasGameObject(person)) && ((person.FightingForce < t.Leader.FightingForce) && !person.HasLeaderValidTitle))
                {
                    int incrementPerDayOfCombativity = t.IncrementPerDayOfCombativity;
                    bool immunityOfCaptive = t.ImmunityOfCaptive;
                    int routIncrementOfCombativity = t.RoutIncrementOfCombativity;
                    int attackDecrementOfCombativity = t.AttackDecrementOfCombativity;
                    int count = t.CombatMethods.Count;
                    int chanceIncrementOfCriticalStrike = t.ChanceIncrementOfCriticalStrike;
                    int chanceDecrementOfCriticalStrike = t.ChanceDecrementOfCriticalStrike;
                    int chanceIncrementOfChaosAfterCriticalStrike = t.ChanceIncrementOfChaosAfterCriticalStrike;
                    int avoidSurroundedChance = t.AvoidSurroundedChance;
                    int chaosAfterSurroundAttackChance = t.ChaosAfterSurroundAttackChance;
                    int chanceIncrementOfStratagem = t.ChanceIncrementOfStratagem;
                    int chanceDecrementOfStratagem = t.ChanceDecrementOfStratagem;
                    int chanceIncrementOfChaosAfterStratagem = t.ChanceIncrementOfChaosAfterStratagem;
                    foreach (Skill s in person.Skills.GetSkillList())
                    {
                        s.Influences.PurifyInfluence(this, Applier.Skill, s.ID);
                    }
                    foreach (Title i in person.Titles)
                    {
                        i.Influences.PurifyInfluence(this, Applier.Title, i.ID);
                    }
                    person.ApplySkills(false);
                    person.ApplyTitles(false);
                    if (((((((t.IncrementPerDayOfCombativity > incrementPerDayOfCombativity) || (t.ImmunityOfCaptive != immunityOfCaptive)) || ((t.RoutIncrementOfCombativity > routIncrementOfCombativity) || (t.AttackDecrementOfCombativity > attackDecrementOfCombativity))) || ((t.CombatMethods.Count > count) || (((t.TroopStrength >= 70) && (t.ChanceIncrementOfCriticalStrike > chanceIncrementOfCriticalStrike)) && (t.ChanceIncrementOfCriticalStrike <= 50)))) || (((((t.TroopCommand >= 70) && (t.ChanceDecrementOfCriticalStrike > chanceDecrementOfCriticalStrike)) && (t.ChanceDecrementOfCriticalStrike <= 50)) || (((t.ChanceIncrementOfCriticalStrike >= 10) && (t.ChanceIncrementOfChaosAfterCriticalStrike > chanceIncrementOfChaosAfterCriticalStrike)) && (t.ChanceIncrementOfChaosAfterCriticalStrike <= 100))) || (((t.AvoidSurroundedChance <= 80) && (t.AvoidSurroundedChance > avoidSurroundedChance)) || ((t.ChaosAfterSurroundAttackChance <= 20) && (t.ChaosAfterSurroundAttackChance > chaosAfterSurroundAttackChance))))) || ((((t.TroopIntelligence >= 70) && (t.ChanceIncrementOfStratagem > chanceIncrementOfStratagem)) && (t.ChanceIncrementOfStratagem <= 30)) || (((t.TroopIntelligence >= 70) && (t.ChanceDecrementOfStratagem > chanceDecrementOfStratagem)) && (t.ChanceDecrementOfStratagem <= 30)))) || (((t.TroopIntelligence >= 0x55) && (t.ChanceIncrementOfChaosAfterStratagem > chanceIncrementOfChaosAfterStratagem)) && (t.ChanceIncrementOfChaosAfterStratagem <= 100)))
                    {
                        person.Selected = true;
                        result.Add(person);
                        personCnt++;
                    }
                }
                if (personCnt >= 5) break;
            }
            */
            return result;
        }

        public void AddPopulationPack(int days, int population)
        {
            PopulationPack item = new PopulationPack(days, population);
            this.PopulationPacks.Add(item);
        }

        public void AddMilitaryPopulationPack(int days, int population)
        {
            PopulationPack item = new PopulationPack(days, population);
            this.MilitaryPopulationPacks.Add(item);
        }

        /*
        public void AddSpyPack(Person person, int days)
        {
            SpyPack item = new SpyPack(person, days);
            this.SpyPacks.Add(item);
        }
        */

        public bool AgricultureAvail()
        {
            return (_architectureKind.HasAgriculture && this.HasPerson());
        }

        private void RoutewayAI()
        {
            // 粮道管理是基础策略，委任军区可以执行
            if (!ShouldExecuteBasicAI()) return;
            
            if (GameObject.Random(10) == 0)
            {
                RoutewayList toRemove = new RoutewayList();
                foreach (Routeway r in this.Routeways)
                {
                    if (!r.IsInUsing)
                    {
                        toRemove.Add(r);
                    }
                }
                foreach (Routeway r in toRemove)
                {
                    this.RemoveRoutewayToArchitecture(r.DestinationArchitecture);
                }
            }
        }

        public void RunPrepareAI()
        {
            this.PrepareAI();
        }

        public void RunDomesticAI()
        {
            // 🔥 修复：确保只有 AI 势力的建筑才执行内政 AI 逻辑
            
            // 1. 如果建筑没有势力（中立建筑），不执行内政 AI
            if (BelongedFaction == null)
            {
                return;
            }
            
            // 2. 如果建筑属于玩家势力，检查是否是委任军区
            if (Session.Current?.Scenario?.IsPlayer(BelongedFaction) == true)
            {
                // 如果不是委任军区，不执行内政 AI
                if (BelongedSection == null || 
                    BelongedSection.AIDetail == null || 
                    !BelongedSection.AIDetail.AutoRun)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunDomesticAI] ⚠️ 警告：玩家建筑 {this.Name} 意外调用了 RunDomesticAI！");
                    System.Diagnostics.Debug.WriteLine($"[RunDomesticAI]   所属势力: {BelongedFaction.Name}");
                    System.Diagnostics.Debug.WriteLine($"[RunDomesticAI]   所属军区: {(BelongedSection != null ? BelongedSection.Name : "无")}");
                    System.Diagnostics.Debug.WriteLine($"[RunDomesticAI]   军区托管: {(BelongedSection?.AIDetail?.AutoRun == true ? "是" : "否")}");
                    
                    // 记录调用堆栈
                    var stackTrace = new System.Diagnostics.StackTrace(true);
                    System.Diagnostics.Debug.WriteLine($"[RunDomesticAI] 调用堆栈:");
                    for (int i = 0; i < Math.Min(5, stackTrace.FrameCount); i++)
                    {
                        var frame = stackTrace.GetFrame(i);
                        System.Diagnostics.Debug.WriteLine($"  {frame?.ToString()?.Trim() ?? "<unknown frame>"}");
                    }
                    
                    return; // 玩家非委任建筑不执行内政 AI
                }
            }
            
            // 3. 只有 AI 势力的建筑或玩家委任军区的建筑才执行以下内政 AI 逻辑
            
            this.RoutewayAI();
            this.AITreasure();
            this.AITrade();
            this.AIFacility();
            this.AIWork(false);
            this.AIExpand();
        }

        public void RunMilitaryAI()
        {
            // 🔥 CRITICAL FIX: 添加招募新军队的逻辑
            // 这是AI创建编队的前提条件，必须先有军队才能创建编队
            this.AIRecruitMilitary();
            
            // 🔥 V8.2 FIX: 为新创建的编队分配招募任务
            // 确保编队创建后立即开始补充兵力
            this.AutoRecruit();
            
            this.AIMilitary();
            this.AICampaign();
            this.CheckActiveDefense();
            this.CheckEmergencyEvacuation();
            try { WTKGameManager.AITroopRecyclingSystem.Instance.RecycleWeakTroopsForManpower(this); } catch { }
            this.OutsideTacticsAI();
            this.InsideTacticsAI();
            this.RefreshIdleWorkForAI();
        }

        public void AI()
        {
            // 🔥 修复：确保只有 AI 势力的建筑才执行 AI 逻辑
            
            // 1. 如果建筑没有势力（中立建筑、空城），不执行 AI
            // 用户反馈：游戏中存在没有势力的建筑是正常的业务逻辑
            if (BelongedFaction == null)
            {
                return;
            }
            
            // 2. 如果建筑属于玩家势力，检查是否是委任军区
            // 注意：Session.Current 永远不为 null（静态初始化），但 Scenario 可能为 null
            if (Session.Current.Scenario?.IsPlayer(BelongedFaction) == true)
            {
                // 🔥 关键修复：允许委任军区执行AI逻辑
                // 如果不是委任军区，不执行AI（使用 C# 12 模式匹配）
                if (BelongedSection?.AIDetail?.AutoRun != true)
                {
                    return; // 玩家非委任建筑不执行AI逻辑
                }
                
                // 委任军区继续执行AI逻辑
            }
            
            // 3. AI 势力的建筑或玩家委任军区的建筑执行以下 AI 逻辑

            //this.PlayAIZhaoXian();
            this.RunPrepareAI();
            
            // Faction AI performs its own specialized tasks (e.g. executions) here, 
            // but for the sake of structure, we keep the original flow's dedicated calls if any were interleaved.
            // In the original code, AIExecute was called right after PrepareAI.
            this.AIExecute();
            
            this.RunDomesticAI();
            this.RunMilitaryAI();

            this.AIDiplomaticTactics();

            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseAIArchitecture(Session.Current.Scenario, this);
        }

        private void AIExpand()
        {
            #if DEBUG
            // 🔥 诊断：记录AIExpand调用，追踪玩家势力是否误触发
            // System.Diagnostics.Debug.WriteLine($"[AIExpand] {Name}(ID:{ID}) 被调用");
            if (BelongedFaction != null)
            {
                bool isPlayer = Session.Current?.Scenario?.IsPlayer(BelongedFaction) ?? false;
                // System.Diagnostics.Debug.WriteLine($"[AIExpand] 所属势力: {BelongedFaction.Name}, 是否玩家: {isPlayer}");
                
                if (isPlayer)
                {
                    // 🔥 如果是玩家势力，记录调用堆栈并直接返回
                    var stackTrace = new System.Diagnostics.StackTrace(true);
                    // System.Diagnostics.Debug.WriteLine($"[AIExpand] ⚠️ 警告：玩家势力 {BelongedFaction.Name} 触发了AIExpand！");
                    // System.Diagnostics.Debug.WriteLine($"[AIExpand] 调用堆栈:\n{stackTrace}");
                    return; // 玩家势力不应该执行AI扩建
                }
            }
            #endif
            
            // 🔥 诊断：检查 _architectureKind 是否为 null
            if (_architectureKind == null)
            {
                // System.Diagnostics.Debug.WriteLine($"[AIExpand] ❌ {Name}(ID:{ID}) 的 _architectureKind 为 null！");
                // System.Diagnostics.Debug.WriteLine($"[AIExpand] Kind={(Kind != null ? Kind.Name : "null")}");
                // System.Diagnostics.Debug.WriteLine($"[AIExpand] BelongedFaction={BelongedFaction?.Name}, IsPlayer={Session.Current?.Scenario?.IsPlayer(BelongedFaction)}");
                return; // 直接返回，不执行扩建逻辑
            }
            
            // #if DEBUG
            // if (this.Name == "高陵")
            // {
            //     System.Diagnostics.Debug.WriteLine($"[AIExpand] ========== {Name} AIExpand 被调用 ==========");
            //     System.Diagnostics.Debug.WriteLine($"  当前 _architectureArea 状态:");
            //     System.Diagnostics.Debug.WriteLine($"    _architectureArea == null: {_architectureArea == null}");
            //     if (_architectureArea != null)
            //     {
            //         System.Diagnostics.Debug.WriteLine($"    _architectureArea.Area == null: {_architectureArea.Area == null}");
            //         if (_architectureArea.Area != null)
            //         {
            //             System.Diagnostics.Debug.WriteLine($"    _architectureArea.Area.Count: {_architectureArea.Area.Count}");
            //             for (int i = 0; i < _architectureArea.Area.Count; i++)
            //             {
            //                 System.Diagnostics.Debug.WriteLine($"      [{i}]: {_architectureArea.Area[i]}");
            //             }
            //         }
            //     }
            //     System.Diagnostics.Debug.WriteLine($"  ArchitectureAreaString: '{ArchitectureAreaString}'");
            //     System.Diagnostics.Debug.WriteLine($"  IsStrategicFrontline={IsStrategicFrontline()}");
            //     System.Diagnostics.Debug.WriteLine($"  Population={Population}, PopulationCeiling={PopulationCeiling}");
            //     System.Diagnostics.Debug.WriteLine($"  人口过载={(Population > PopulationCeiling * 0.9f)}");
            // }
            // #endif
            
            // 🔥 NEW: 整合战略地图的智能扩建逻辑
            bool isFrontLine = IsStrategicFrontline();
            // 使用PopulationCeiling替代MaxPopulation
            bool isOvercrowded = (this.Population > this.PopulationCeiling * 0.9f);
            
            if (isFrontLine || isOvercrowded)
            {
                // #if DEBUG
                // if (this.Name == "高陵")
                // {
                //     System.Diagnostics.Debug.WriteLine($"[AIExpand] {Name} 满足扩建触发条件: 前线={isFrontLine}, 过载={isOvercrowded}");
                // }
                // #endif
                
                // 执行扩建
                if (this.ExpandAvail())
                {
                    // #if DEBUG
                    // System.Diagnostics.Debug.WriteLine($"[AIExpand] {this.Name} 满足扩建条件并执行扩建: 前线={isFrontLine}, 过载={isOvercrowded}");
                    // #endif
                    this.Expand();
                }

            }
        }

        /// <summary>
        /// 🔥 NEW: 整合战略地图的前线判断
        /// 结合传统前线标识和战略势能图进行综合判断
        /// <summary>
        /// 🔥 NEW: 智能前线判断
        /// 结合传统前线标识和战略势能图进行综合判断
        /// </summary>
        public bool IsStrategicFrontline()
        {
            // 1. 基础物理前线判定 (保持不变)
            bool traditionalFrontline = this.FrontLine || this.HostileLine || this.noFactionFrontline;
            
            // 2. 深度战略价值评估
            try
            {
                // A. 政治中心：首都或州治所
                bool isCapital = this.BelongedFaction?.Capital == this;
                // 假设有个 IsStateCapital 属性，如果没有可以去掉
                // bool isStateCapital = this.IsStateCapital; 

                // B. 战争热点：最近被打过，或者视野内有大量敌军
                bool isHotZone = this.RecentlyAttacked > 0;
                if (this.HasHostileTroopsInView() && this.GetHostileTroopsInView().Count > 1)
                {
                    isHotZone = true;
                }

                // C. 人口重镇 (修正逻辑)
                // 不再看比例，而是看绝对值。假设 50000 人口就算具备动员能力的大城
                bool isPopCenter = this.Population >= 50000; 

                // D. 军事重镇 (新增)
                // 兵多粮足，哪怕不是前线也是兵站
                bool isMilitaryHub = this.MilitaryCount >= 30000 && this.Food >= 1000000;

                // E. 经济重镇 (新增)
                // 钱多，是势力的金库
                bool isEconomicHub = this.Fund >= 50000;

                // 3. 综合评分判定
                // 满足以下任一条件即为【战略重要】：
                // 1. 是首都
                // 2. 正在挨打 (HotZone)
                // 3. 是传统前线，并且 (人多 OR 兵多 OR 钱多) -> 避免了贫瘠的前线也被当宝
                // 4. 即使不是前线，但是超级兵站 (MilitaryHub) -> 后方集结地
                
                bool strategicImportance = isCapital 
                                        || isHotZone
                                        || (traditionalFrontline && (isPopCenter || isMilitaryHub || isEconomicHub))
                                        || (isMilitaryHub && isPopCenter); // 后方的大型兵源地也算战略要地

#if DEBUG
                // 优化日志输出，不再刷屏，只在确实重要时显示
                if (global::GameObjects.SectionAIHelper.EnableDebugOutput && strategicImportance)
                {
                    string reason = "";
                    if (isCapital) reason += "首都 ";
                    if (isHotZone) reason += "交战中 ";
                    if (traditionalFrontline) reason += "前线 ";
                    if (isPopCenter) reason += "人口 ";
                    if (isMilitaryHub) reason += "军事 ";
                    
                    System.Diagnostics.Debug.WriteLine($"[🏰 StrategicFrontline] {this.Name}: 判定为战略要地 (原因: {reason})");
                }
#endif

                // 返回逻辑：只要是传统前线，或者具备战略价值，都返回 true
                // 这样可以让 AI 既照顾边界，又重点照顾核心
                return traditionalFrontline || strategicImportance;
            }
            catch (Exception ex)
            {
                return traditionalFrontline;
            }
        }

        /// <summary>
        /// 🔥 NEW: 动态内政饱和度阈值
        /// 根据战略情况和势力状态动态调整内政发展优先级
        /// </summary>
        public float InternalAffairSaturationThreshold
        {
            get
            {
                return CalculateDynamicSaturationThreshold();
            }
        }

        /// <summary>
        /// 🔥 NEW: 计算动态饱和度阈值
        /// 基于威胁等级、资源状况、发展阶段等因素动态调整
        /// </summary>
        private float CalculateDynamicSaturationThreshold()
        {
            // 🛡️ 递归保护：如果正在执行人员调配，返回默认阈值
            if (Faction._isExecutingPersonnelAllocation)
            {
                return 0.85f; // 返回默认阈值，避免复杂计算
            }
            
            float baseThreshold = 0.85f; // 基础阈值
            
            try
            {
                // =========================================================
                // 1. 威胁等级调整
                // =========================================================
                bool hasImmediateThreat = this.HasHostileTroopsInView();
                bool isUnderAttack = this.RecentlyAttacked > 0;
                
                if (isUnderAttack)
                {
                    // 正在被攻击：大幅降低阈值，专注军事
                    baseThreshold = 0.60f;
                }
                else if (hasImmediateThreat)
                {
                    // 有即时威胁：适度降低阈值
                    baseThreshold = 0.70f;
                }
                else if (this.FrontLine)
                {
                    // 前线城市：略微降低阈值，保持警戒
                    baseThreshold = 0.80f;
                }

                // =========================================================
                // 2. 资源状况调整
                // =========================================================
                if (!this.IsFundEnough || !this.IsFoodEnough)
                {
                    // 资源紧缺：提高阈值，优先发展经济
                    baseThreshold = Math.Min(baseThreshold + 0.10f, 0.95f);
                }
                
                if (this.Fund > this.FundCeiling * 0.8f && this.Food > this.FoodCeiling * 0.8f)
                {
                    // 资源充足：可以追求完美，提高阈值
                    baseThreshold = Math.Min(baseThreshold + 0.05f, 0.95f);
                }

                // =========================================================
                // 3. 发展阶段调整
                // =========================================================
                float developmentLevel = (this.Agriculture + this.Commerce + this.Technology) / 
                                       (float)(this.AgricultureCeiling + this.CommerceCeiling + this.TechnologyCeiling);
                
                if (developmentLevel < 0.3f)
                {
                    // 发展初期：提高阈值，快速发展基础设施
                    baseThreshold = Math.Min(baseThreshold + 0.10f, 0.95f);
                }
                else if (developmentLevel > 0.8f)
                {
                    // 发展后期：降低阈值，避免过度投入
                    baseThreshold = Math.Max(baseThreshold - 0.05f, 0.70f);
                }

                // =========================================================
                // 4. 势力整体状况调整
                // =========================================================
                if (this.BelongedFaction != null)
                {
                    int totalCities = this.BelongedFaction.ArchitectureCount;
                    int threatenedCities = 0;
                    
                    foreach (var arch in this.BelongedFaction.Architectures.Cast<Architecture>())
                    {
                        if (arch.HasHostileTroopsInView() || arch.RecentlyAttacked > 0)
                        {
                            threatenedCities++;
                        }
                    }
                    
                    float threatRatio = (float)threatenedCities / totalCities;
                    
                    if (threatRatio > 0.5f)
                    {
                        // 势力面临全面威胁：大幅降低阈值
                        baseThreshold = Math.Max(baseThreshold - 0.15f, 0.60f);
                    }
                    else if (threatRatio > 0.2f)
                    {
                        // 势力面临部分威胁：适度降低阈值
                        baseThreshold = Math.Max(baseThreshold - 0.08f, 0.70f);
                    }
                }

                // =========================================================
                // 5. 特殊情况调整
                // =========================================================
                
                // 人口压力：人口接近上限时提高阈值，优先发展
                if (this.Population > this.PopulationCeiling * 0.9f)
                {
                    baseThreshold = Math.Min(baseThreshold + 0.05f, 0.95f);
                }
                
                // 耐久度危机：城墙快塌时降低其他内政阈值
                if (this.Endurance < this.EnduranceCeiling * 0.3f)
                {
                    baseThreshold = Math.Max(baseThreshold - 0.10f, 0.65f);
                }

#if DEBUG
                if (global::GameObjects.SectionAIHelper.EnableDebugOutput && Math.Abs(baseThreshold - 0.85f) > 0.05f)
                {
                    System.Diagnostics.Debug.WriteLine($"[🎯 DynamicThreshold] {this.Name}: 动态阈值={baseThreshold:F2} " +
                        $"(威胁={hasImmediateThreat}, 前线={this.FrontLine}, 发展度={developmentLevel:F2})");
                }
#endif

                return Math.Max(0.60f, Math.Min(0.95f, baseThreshold)); // 限制在合理范围内
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[⚠️ DynamicThreshold] {this.Name}: 计算失败 - {ex.Message}");
#endif
                return 0.85f; // 失败时返回默认值
            }
        }

        /// <summary>
        /// 判断建筑是否应该执行基础AI逻辑（仅说服、招募等基础策略）
        /// 规则：AI势力总是执行；玩家势力只有在委任军区时才执行基础策略
        /// </summary>
        /// <returns>true=应该执行基础AI, false=不执行AI</returns>
        private bool ShouldExecuteBasicAI()
        {
            // 🔥 调试：基础AI执行判断
            if (Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
#if DEBUG
                if (global::GameObjects.SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShouldExecuteBasicAI] 建筑{this.Name}: 是否玩家={Session.Current.Scenario.IsPlayer(this.BelongedFaction)}, 军区={this.BelongedSection?.Name}, AutoRun={this.BelongedSection?.AIDetail?.AutoRun}");
                }
#endif
            }
            
            // AI势力总是执行AI（不需要军区）
            if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                return true;
            }
            
            // 玩家势力：只有委任的军区才执行基础AI
            if (this.BelongedSection != null && 
                this.BelongedSection.AIDetail != null && 
                this.BelongedSection.AIDetail.AutoRun)
            {
#if DEBUG
                if (global::GameObjects.SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[ShouldExecuteBasicAI] 玩家势力建筑 " + this.Name + " 属于委任军区 " + this.BelongedSection.Name + "，将执行基础AI逻辑");
                }
#endif
                return true;
            }
            
#if DEBUG
            if (global::GameObjects.SectionAIHelper.EnableDebugOutput && Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                System.Diagnostics.Debug.WriteLine("[ShouldExecuteBasicAI] 玩家势力建筑 " + this.Name + " 不属于委任军区，跳过基础AI逻辑");
            }
#endif
            
            return false;
        }

        private void CheckActiveDefense()
        {
            try
            {
                global::WorldOfTheThreeKingdoms.GameManager.AICoordinatedDefenseSystem.Instance.CheckActiveDefense(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckActiveDefense] Error: {ex.Message}");
            }
        }

        private void CheckEmergencyEvacuation()
        {
            try
            {
                global::WorldOfTheThreeKingdoms.GameManager.AICoordinatedDefenseSystem.Instance.CheckEmergencyEvacuation(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckEmergencyEvacuation] Error: {ex.Message}");
            }
        }  /// <summary>
        /// 判断建筑是否应该执行高级AI逻辑（外交、处决、劝降等势力级别决策）
        /// 规则：只有AI势力才执行高级策略
        /// </summary>
        /// <returns>true=应该执行高级AI, false=不执行高级AI</returns>
        private bool ShouldExecuteAdvancedAI()
        {
            // 只有AI势力才执行高级策略
            return !Session.Current.Scenario.IsPlayer(this.BelongedFaction);
        }

        private void AIExecute()
        {
            // 🔥 处决是势力级别的决策，只有AI势力才执行
            if (!ShouldExecuteAdvancedAI()) return;
            if (Session.GlobalVariables.AIExecutionRate <= 0) return;

            //AI for executing officers. High ambition and low personal loyalty leads to higher chance of execution (rate lower => higher chance)
            int uncruelty = this.BelongedFaction.Leader.Uncruelty;
            if (uncruelty > Session.Parameters.AIExecuteMaxUncreulty) return;

            //int leaderExecutionRate = uncruelty <= 2 ? 5 : uncruelty * uncruelty * uncruelty * 2;
            //int leaderExecutionRate = uncruelty * uncruelty * uncruelty * 4;
            foreach (Captive i in this.Captives)
            {
                if ((!i.CaptivePerson.RecruitableBy(this.BelongedFaction, (int)((uncruelty - 2) * Session.Parameters.AIExecutePersonIdealToleranceMultiply)) || this.BelongedFaction.Leader.Hates(i.CaptivePerson) || i.CaptivePerson.Hates(this.BelongedFaction.Leader)) &&
                    GameObject.Random((int)(uncruelty * uncruelty * (Session.GlobalVariables.AIExecuteBetterOfficer ? 100000.0 / i.CaptivePerson.Merit : i.CaptivePerson.Merit / 100000.0)
                        * (100.0 / Session.GlobalVariables.AIExecutionRate))) == 0)  //处斩几率修改系数就可以，可设为小数
                {
                    if (!this.BelongedFaction.Leader.HasStrainTo(i.CaptivePerson))
                    {
                        if (!this.BelongedFaction.Leader.Sex && this.BelongedFaction.IsPersonForHouGong(i.CaptivePerson, true))
                        {
                            this.BelongedFaction.AIActuallyMakeMarriage(this.BelongedFaction.Leader, i.CaptivePerson);   
                        }
                        else
                        {
                            Session.MainGame.mainGameScreen.OnExecute(this.BelongedFaction.Leader, i.CaptivePerson);
                            i.CaptivePerson.execute(this.BelongedFaction);
                        }

                        break;
                    }
                }
                if (this.BelongedFaction.IsAlien &&
                    (i.CaptivePerson.PersonalLoyalty >= 2 || this.BelongedFaction.Leader.Hates(i.CaptivePerson) || i.CaptivePerson.Hates(this.BelongedFaction.Leader)) &&
                    GameObject.Chance(10))
                {
                    if (!this.BelongedFaction.Leader.HasStrainTo(i.CaptivePerson))
                    {
                        if (!this.BelongedFaction.Leader.Sex && this.BelongedFaction.IsPersonForHouGong(i.CaptivePerson, true))
                        {
                            this.BelongedFaction.AIActuallyMakeMarriage(this.BelongedFaction.Leader, i.CaptivePerson);   
                        }
                        else
                        {
                            Session.MainGame.mainGameScreen.OnExecute(this.BelongedFaction.Leader, i.CaptivePerson);
                            i.CaptivePerson.execute(this.BelongedFaction);
                        }
                    }

                    break;
                }
            }
            /*foreach (Person i in this.Persons)
            {
                if (!i.RecruitableBy(this.BelongedFaction) &&
                    GameObject.Random((int)(leaderExecutionRate / 2 * (100000.0 / i.Merit))) == 0)
                {
                    i.execute(this.BelongedFaction.Leader);
                    break;
                }
            }*/
        }

        private bool AIExtension()
        {
            if (this.BuildingFacility < 0)
            {
                foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKindList().GetRandomList())
                {
                    if (kind.IsExtension)
                    {
                        //八哥修改：增加this.ExpectedFund != 0 和 this.PlanFacilityKind.Days != 0 的限制判斷
                        if (!kind.CanBuild(this)) continue;
                        if (kind.FundCost <= this.Fund)
                        {
                            FacilityKind facilityKind = kind;
                            this.BelongedFaction.DepositTechniquePointForFacility(facilityKind.PointCost);
                            this.BeginToBuildAFacility(facilityKind);
                            return true;
                        }
                        else if (this.ExpectedFund != 0 && (kind.FundCost - (this.Fund - this.EnoughFund)) / this.ExpectedFund + 1 <= kind.Days / 15)
                        {
                            this.PlanFacilityKind = kind;
                            if (GameObject.Chance(0x21) && this.PlanFacilityKind.Days != 0 && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) < this.PlanFacilityKind.PointCost))
                            {
                                this.BelongedFaction.SaveTechniquePointForFacility(this.PlanFacilityKind.PointCost / this.PlanFacilityKind.Days);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 🔥 NEW: 整合AI管理系统的威胁检查
        /// 结合即时威胁、战略势能图和AI决策管理器进行综合判断
        /// </summary>
        private bool HasIntegratedThreat()
        {
            // 🔥 2026-02-17 修复：简化威胁判断，移除过度防御性检查
            // 根本原因：原逻辑充满 try-catch 和防御性空检查，违反 ANTI-BAND-AID PROTOCOL
            // 新逻辑：只检查真正的即时威胁，允许前线城市在安全时建设施
            
            // 1. 即时威胁：视野内有敌军
            if (this.HasHostileTroopsInView())
            {
                return true;
            }

            // 2. 战略威胁：只有同时满足多个条件才认为有威胁
            // 单纯的 FrontLine 不应该阻止建设施，只有在敌对线且最近被攻击才算真正威胁
            if (this.HostileLine && this.RecentlyAttacked > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AIFacility] {this.Name} 检测到战略威胁 - 敌对线={this.HostileLine}, 最近攻击={this.RecentlyAttacked}");
                return true;
            }

            return false;
        }

        private void AIFacility()
        {
            // 🔥 终极修复：在 AIFacility 方法内部再次检查玩家势力
            // 这是最后一道防线，确保玩家城市绝对不会自动建造设施
            
            // 1. 如果建筑没有势力（中立建筑），不执行
            if (BelongedFaction == null)
            {
                return;
            }
            
            // 2. 如果建筑属于玩家势力，检查是否是委任军区
            if (Session.Current?.Scenario?.IsPlayer(BelongedFaction) == true)
            {
                // 如果不是委任军区，不执行
                if (BelongedSection == null || 
                    BelongedSection.AIDetail == null || 
                    !BelongedSection.AIDetail.AutoRun)
                {

                    
                    return; // 玩家非委任建筑绝对不执行设施建造
                }
                else
                {

                }
            }
            
            // 🔥 NEW: 1. 整合战略地图的安全检查
            if (HasIntegratedThreat())
            {

                // 有威胁时，只允许建设防御性设施或停止建设
                // 这里可以根据需要添加防御性设施的建设逻辑
                return;
            }

            // 🔥 NEW: 2. 资金保留：不要把钱花光
            // 必须保留足够的钱用于征兵 (例如保留 2000 金)
            if (this.Fund < 2000)
            {

                return;
            }

            // 🔥 2026-02-17 修复：调整 PlanArchitecture 对设施建造的影响
            // PlanArchitecture 的含义：
            // - 如果是敌对势力：进攻目标
            // - 如果是同势力：资源调配/支援目标（例如：支援首都）
            // 原逻辑：有计划时只有 10% 概率建设施（优先支援目标城市）
            // 新逻辑：有计划时 50% 概率建设施（平衡本地发展和支援任务）
            
            var canBuildFacility = (this.PlanArchitecture == null) || GameObject.Chance(50);
            


            if (canBuildFacility && this.BuildingFacility < 0 && this.FacilityPositionCount > 0)
            {
                // 🔥 修复：先处理已有计划，如果能建造就建造，否则清除计划
                if (this.PlanFacilityKind != null && this.BelongedFaction != null)
                {
                    bool shouldClearPlan = false;
                    
                    // 检查技术和空间是否满足
                    if (this.Technology < this.PlanFacilityKind.TechnologyNeeded || this.PlanFacilityKind.PositionOccupied > this.FacilityPositionLeft)
                    {
                        shouldClearPlan = true;
                    }
                    // 检查造价是否超过资金上限一半
                    else if (this.PlanFacilityKind.FundCost > this.FundCeiling / 2)
                    {
                        shouldClearPlan = true;
                    }
                    // 检查资金和技巧点
                    else if ((this.Fund >= this.PlanFacilityKind.FundCost) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) >= this.PlanFacilityKind.PointCost))
                    {
                        // 资金和技巧点都足够，开始建造
                        this.BelongedFaction.DepositTechniquePointForFacility(this.PlanFacilityKind.PointCost);
                        this.BeginToBuildAFacility(this.PlanFacilityKind);
                        this.PlanFacilityKind = null;
                        return; // 建造开始，本回合结束
                    }
                    else
                    {
                        // 资金或技巧点不足
                        // 技巧点不足时，尝试保存技巧点
                        if (GameObject.Chance(0x21) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) < this.PlanFacilityKind.PointCost))
                        {
                            this.BelongedFaction.SaveTechniquePointForFacility(this.PlanFacilityKind.PointCost / this.PlanFacilityKind.Days);
                        }
                        
                        // 🔥 关键修复：资金不足时，有概率放弃计划，避免永远等待
                        if (this.Fund < this.PlanFacilityKind.FundCost && GameObject.Chance(30))
                        {
                            System.Diagnostics.Debug.WriteLine($"[AIFacility] {this.Name} 资金不足({this.Fund}/{this.PlanFacilityKind.FundCost})，放弃计划设施 {this.PlanFacilityKind.Name}");
                            shouldClearPlan = true;
                        }
                    }
                    
                    if (shouldClearPlan)
                    {
                        this.PlanFacilityKind = null;
                    }
                    else
                    {
                        // 有计划且未清除，等待资金/技巧点，本回合不选择新设施
                        return;
                    }
                }
                
                // 🔥 修复：没有计划时，执行设施选择逻辑
                if (this.PlanFacilityKind == null)
                {
                    if (AIExtension()) return;

                    //remove useless facilities
                    if (this.BelongedSection != null && this.BelongedSection.AIDetail != null && this.BelongedSection.AIDetail.AllowFacilityRemoval)
                    {
                        foreach (Facility i in this.Facilities)
                        {
                            float value = (float) i.Kind.AIValue(this);
                            foreach (KeyValuePair<Condition, float> weight in i.Kind.AIBuildConditionWeight)
                            {
                                if (weight.Key.CheckCondition(this))
                                {
                                    value *= weight.Value;
                                }
                            }
                            if (value < 0 && this.CanRemoveFacility(i) && i.Kind.rongna == 0)
                            {
                            if (this.FacilityEnabled || i.MaintenanceCost <= 0)
                                {
                                    i.Influences.PurifyInfluence(this, Applier.Facility, i.ID);
                                }
                                this.Facilities.Remove(i);
                                Session.Current.Scenario.Facilities.Remove(i);
                                break;
                            }
                        }
                    }
                    //remove facilities if not enough fund to support
                    if (this.FacilityMaintenanceCost * 30 + 100 > this.ExpectedFund && 
                        this.BelongedSection != null && this.BelongedSection.AIDetail != null && this.BelongedSection.AIDetail.AllowFacilityRemoval)
                    {
                        GameObjectList f = this.Facilities.GetList();
                        f.PropertyName = "AIValue";
                        f.SmallToBig = true;
                        f.IsNumber = true;
                        f.ReSort();
                        foreach (Facility i in f)
                        {
                            if (this.CanRemoveFacility(i) && i.Kind.NetFundIncrease <= 0 && i.Kind.rongna == 0)
                            {
                                if (this.FacilityEnabled || i.MaintenanceCost <= 0)
                                {
                                    i.Influences.PurifyInfluence(this, Applier.Facility, i.ID);
                                }
                                this.Facilities.Remove(i);
                                Session.Current.Scenario.Facilities.Remove(i);
                            }
                            if (this.FacilityMaintenanceCost * 30 + 100 <= this.ExpectedFund) break;
                        }
                    }

                    //choose facilities
                    double maxValue = double.MinValue;
                    FacilityKind toBuild = null;
                    List<Facility> toDestroy = new List<Facility>();
                    List<Facility> realToDestroy = new List<Facility>();
                    foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKindList())
                    {
                        if (kind.IsExtension) continue;
                        if (!kind.CanBuild(this)) continue;
                        if (kind.rongna > 0) continue;
                        if ((kind.MaintenanceCost + this.FacilityMaintenanceCost) * 30 + 2000 > this.ExpectedFund && kind.NetFundIncrease <= 0)
                        {
                            continue;
                        }
                        if (kind.FundCost > this.FundCeiling / 2)
                        {
                            continue;
                        }
                        float value = (float)kind.AIValue(this);
                        foreach (KeyValuePair<Condition, float> weight in kind.AIBuildConditionWeight)
                        {
                            if (weight.Key.CheckCondition(this))
                            {
                                value *= weight.Value;
                            }
                        }
                        if (value > 0 && this.ExpectedFund != 0)
                        {
                            int fundMonthToWait = (kind.FundCost - (this.Fund - this.EnoughFund)) / this.ExpectedFund + 1;
                            
                            // 🔥 优化：和平时期只要没敌人，资金允许就必然修
                            bool isPeaceful = this.RecentlyAttacked <= 0 && !this.HasHostileTroopsInView();
                            bool chanceCheck = GameObject.Chance((int)(100 - fundMonthToWait * Session.Parameters.AIFacilityFundMonthWaitParam));
                            int effectiveEnoughFund = this.EnoughFund;

                            if (isPeaceful)
                            {
                                chanceCheck = true; // 和平时期强制通过概率检测
                                effectiveEnoughFund = (int)(this.EnoughFund * 0.7); // 和平时期放宽资金保留要求，鼓励建设
                            }

                            if (value > maxValue && chanceCheck && this.Fund - kind.FundCost > effectiveEnoughFund)
                            {
                                if (this.FacilityPositionLeft < kind.PositionOccupied)
                                {
                                    if (this.BelongedSection != null && this.BelongedSection.AIDetail != null && 
                                        this.BelongedSection.AIDetail.AllowFacilityRemoval && 
                                        this.FacilityPositionLeft < Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetMaxFacilitySpace())
                                    {
                                        int fpl = this.FacilityPositionLeft;
                                        toDestroy.Clear();
                                        foreach (Facility f in this.Facilities.GetRandomList())
                                        {
                                            if (value > f.Kind.AIValue(this) * Session.Parameters.AIFacilityDestroyValueRate && this.CanRemoveFacility(f) && f.Kind.rongna == 0)
                                            {
                                                toDestroy.Add(f);
                                                fpl += f.Kind.PositionOccupied;
                                                if (fpl >= kind.PositionOccupied)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        if (fpl >= kind.PositionOccupied)
                                        {
                                            maxValue = value;
                                            toBuild = kind;
                                            realToDestroy = new List<Facility>(toDestroy);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    } else
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    maxValue = value;
                                    toBuild = kind;
                                    realToDestroy.Clear();
                                }
                            }
                        }
                    }
                    if (toBuild != null)
                    {
                        //if no space and the facility is good enough than others, remove others
                        foreach (Facility f in realToDestroy)
                        {
                            if (this.FacilityEnabled || f.MaintenanceCost <= 0)
                            {
                                f.Influences.PurifyInfluence(this, Applier.Facility, f.ID);
                            }
                            this.Facilities.Remove(f);
                            Session.Current.Scenario.Facilities.Remove(f);
                        }
                        if (toBuild.PositionOccupied <= this.FacilityPositionLeft)
                        {
                            //actually build it, or put to plan if fund is not enough
                            if (this.BelongedFaction != null && (this.Fund >= toBuild.FundCost) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) >= toBuild.PointCost))
                            {
                                FacilityKind facilityKind = toBuild;
                                this.BelongedFaction.DepositTechniquePointForFacility(facilityKind.PointCost);
                                this.BeginToBuildAFacility(facilityKind);
                            }
                            else
                            {
                                this.PlanFacilityKind = toBuild;
                                if (this.BelongedFaction != null && GameObject.Chance(0x21) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) < this.PlanFacilityKind.PointCost))
                                {
                                    this.BelongedFaction.SaveTechniquePointForFacility(this.PlanFacilityKind.PointCost / this.PlanFacilityKind.Days);
                                }

                            }
                        }
                    }
                    /*List<FacilityKind> list3 = new List<FacilityKind>();
                    int facilityPositionLeft = this.FacilityPositionLeft;
                    int iD = 10;
                    int num3 = 0;
                    foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.FacilityKinds.Values)
                    {
                        if (((kind.ID > iD) && ((kind.ID / 10) == 1)) && (kind.TechnologyNeeded < this.Technology))
                        {
                            iD = kind.ID;
                        }
                        if (((kind.ID > num3) && ((kind.ID / 10) == 0)) && (kind.TechnologyNeeded < this.Technology))
                        {
                            num3 = kind.ID;
                        }
                    }
                    foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.FacilityKinds.Values)
                    {
                        if (((kind.rongna > 0) || (((kind.ID / 10) == 0) && (kind.ID != num3))) || (((kind.ID / 10) == 1) && (kind.ID != iD)))
                        {
                            continue;
                        }
                        if ((((!kind.PopulationRelated || _architectureKind.HasPopulation) && ((this.Technology >= kind.TechnologyNeeded) && (facilityPositionLeft >= kind.PositionOccupied)))
                            && ((!kind.UniqueInArchitecture || !this.ArchitectureHasFacilityKind(kind.ID)) && (!kind.UniqueInFaction || !this.FactionHasFacilityKind(kind.ID))))
                            && ((kind.FrontLine && ((this.HostileLine || (this.FrontLine && GameObject.Chance(50))) || (!this.FrontLine && GameObject.Chance(10)))) || (!kind.FrontLine && ((!this.FrontLine || (!this.HostileLine && GameObject.Chance(50))) || (this.HostileLine && GameObject.Chance(5))))))
                        {
                            list.Add(kind);
                            if ((this.Fund >= kind.FundCost) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) >= kind.PointCost))
                            {
                                list2.Add(kind);
                            }
                        }
                    }
                    if (facilityPositionLeft <= 0)
                    {
                        foreach (Facility facility in this.Facilities.GetList())
                        {
                            if ((((this.Technology > facility.TechnologyNeeded) && this.FacilityIsPossibleOverTechnology(facility.TechnologyNeeded))
                                && ((this.Fund > (facility.FundCost * 10)) && (this.BelongedFaction.TechniquePoint > (facility.PointCost * 10))))
                                && (GameObject.Random(facility.Days * facility.PositionOccupied) < 20)
                                && !facility.Kind.bukechaichu)
                            {
                                if (list.IndexOf(facility.Kind) >= 0)
                                {
                                    continue;
                                }
                                list3.Add(facility.Kind);
                                if (this.FacilityEnabled)
                                {
                                    facility.Influences.PurifyInfluence(this);
                                }
                                this.Facilities.Remove(facility);
                                Session.Current.Scenario.Facilities.Remove(facility);
                            }
                        }
                        if (list3.Count == 0)
                        {
                            return;
                        }
                        facilityPositionLeft = this.FacilityPositionLeft;
                    }
                    if (list2.Count > 0)
                    {
                        FacilityKind facilityKind = list2[GameObject.Random(list2.Count)];
                        this.BelongedFaction.DepositTechniquePointForFacility(facilityKind.PointCost);
                        this.BeginToBuildAFacility(facilityKind);
                    }
                    else if (list.Count > 0)
                    {
                        this.PlanFacilityKind = list[GameObject.Random(list.Count)];
                        if (GameObject.Chance(0x21) && ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) < this.PlanFacilityKind.PointCost))
                        {
                            this.BelongedFaction.SaveTechniquePointForFacility(this.PlanFacilityKind.PointCost / this.PlanFacilityKind.Days);
                        }
                    }*/
                }
            }
        }

        public int EnoughPeople
        {
            get
            {
                int fundSupport = (this.Fund - this.ExpectedSalary) / (Session.Parameters.InternalFundCost * 30);
                int develop = Math.Max((this.AgricultureCeiling - this.Agriculture) / 90,
                    Math.Max((this.CommerceCeiling - this.Commerce) / 90,
                    Math.Max((this.TechnologyCeiling - this.Technology) / 90,
                    Math.Max((this.EnduranceCeiling - this.Endurance) / 90,
                    Math.Max((this.MoraleCeiling - this.Morale) / 90,
                    (this.DominationCeiling - this.Domination) / 30)))));
                int frontLine = (this.withoutTruceFrontline || this.noFactionFrontline) ? this.EffectiveMilitaryCount * 2 : 0;
                int result = Math.Min(this.MaxSupportableTroop, Math.Min(Math.Max(develop, frontLine), fundSupport));
                return Math.Max(0, result);
            }
        }

        public bool Abandoned
        {
            get
            {
                if (!this.HasHostileTroopsInView()) return false;
                if (this.Endurance >= 30) return false;
                if (this.TotalHostileForce > (this.TotalFriendlyForce + this.TotalStoredForce))
                {
                    return true;
                }
                return false;
            }
        }

        public int TroopReserveScale
        {
            get
            {
                Person leader = this.BelongedFaction.Leader;
                int reserve = (int)(((leader.Calmness - leader.Braveness) * Session.Parameters.AIBackendArmyReserveCalmBraveDifferenceMultiply +
                    (5 - (int)leader.Ambition) * Session.Parameters.AIBackendArmyReserveAmbitionMultiply)
                    * Session.Parameters.AIBackendArmyReserveMultiply + Session.Parameters.AIBackendArmyReserveAdd);

                return reserve;
            }
        }

        public bool HasEnoughTroopReserve
        {
            get
            {
                return this.ArmyScale >= this.TroopReserveScale;
            }
        }

        public void WithdrawPerson()
        {
            if (this.BelongedFaction.ArchitectureCount <= 1) return;
            GameObjectList list = this.MovablePersons.GetList();
            if (list.Count > 1)
            {
                list.IsNumber = true;
                list.SmallToBig = true;
                list.PropertyName = "FightingForce";
                list.ReSort();
            }
            Architecture capital = this.BelongedFaction.Capital;
            ArchitectureList otherArchitectureList = this.GetOtherArchitectureList();
            if (capital == this)
            {
                if (otherArchitectureList.Count > 1)
                {
                    otherArchitectureList.IsNumber = true;
                    otherArchitectureList.PropertyName = "ArmyScaleWeighing";
                    otherArchitectureList.ReSort();
                    capital = otherArchitectureList[0] as Architecture;
                }
            }
            ArchitectureList otherArch = this.GetOtherArchitectureList();
            Architecture dest = (Architecture)otherArch[GameObject.Random(otherArch.Count)];
            double minDist = double.MaxValue;
            foreach (Architecture i in otherArchitectureList)
            {
                double distance = Session.Current.Scenario.GetDistance(this.Position, i.Position);
                if (distance < minDist && !dest.Abandoned)
                {
                    minDist = distance;
                    dest = i;
                }
            }
            for (int i = 0; i < list.Count; ++i) 
            { 
                Person p = list[i] as Person;
                if (!p.HasFollowingArmy && !p.HasLeadingArmy)
                {
                    p.WaitForFeiZi = null;
                    p.MoveToArchitecture(dest);
                    foreach (Person q in p.AvailableVeryClosePersons)
                    {
                        q.MoveToArchitecture(dest);
                    }
                }
            }
        }

        public bool HasEnoughPeople
        {
            get
            {
                return this.PersonCount + this.MovingPersonCount >= this.EnoughPeople;
            }
        }

        public bool CallResource(Architecture src, int fund, int food)
        {

            if (fund == 0 && food == 0) return false;

            int actualTransferFood = food;
            int actualTransferFund = fund;

            src.DecreaseFood(food);
            src.DecreaseFund(fund);

            if (food > 100)
            {
                this.AddFoodPack((int) (food / Session.Current.Scenario.GetResourceConsumptionRate(src, this)), Session.Current.Scenario.GetTransferFundDays(src, this));
            }
            if (fund > 100)
            {
                this.AddFundPack((int) (fund / Session.Current.Scenario.GetResourceConsumptionRate(src, this)), Session.Current.Scenario.GetTransferFundDays(src, this));
            }

            return true;
        }

        public int CallMilitary(Architecture src, int scale)
        {
            MilitaryList leaderlessArmies = new MilitaryList();
            int transferredScale = 0;

            foreach (Military i in src.Militaries)
            {
                if ((i.FollowedLeader == null || i.Leader == null) && !i.IsTransport)
                {
                    leaderlessArmies.Add(i);
                }
            }

            foreach (Military i in leaderlessArmies.GetRandomList())
            {
                if (i.Scales < i.MaxScale && this.IsTroopExceedsLimit)
                {
                    continue;
                }
                if (i.Scales + transferredScale <= scale)
                {
                    src.TransferMilitary(i, this);
                    transferredScale += i.Scales;
                    if (transferredScale >= scale) return transferredScale;
                }
            }

            this.SuspendTroopTransfer = 30;

            return transferredScale;
        }

        /*
        public int CallTroop(Architecture src, int scale)
        {
            MilitaryList leaderlessArmies = new MilitaryList();
            int transferredScale = 0;

            foreach (Military i in src.Militaries)
            {
                if ((i.FollowedLeader == null || i.Leader == null) && !i.IsTransport)
                {
                    leaderlessArmies.Add(i);
                }
            }

            foreach (Military i in leaderlessArmies.GetRandomList())
            {
                if (i.Scales + transferredScale <= scale)
                {
                   // Military transfer = src.TransferMilitary(i, this);
                    Troop built = src.BuildTroopForTransfer(i, this);
                    if (built == null) return transferredScale;

                    transferredScale += i.Scales;
                    if (transferredScale >= scale) return transferredScale;
                }
            }

            /*if (transferredScale < scale)
            {
                foreach (Military i in src.Militaries.GetRandomList())
                {
                    if (i.IsTransport) continue;
                    if (i.Scales <= transferredScale)
                    {
                        if (src.Persons.HasGameObject(i.Leader) || src.Persons.HasGameObject(i.FollowedLeader))
                        {
                            transferredScale += i.Scales;
                            src.BuildTroopForTransfer(i, this);
                            if (transferredScale >= scale) return transferredScale;
                        }
                        else
                        {
                            Person armyLeader = i.FollowedLeader != null ? i.FollowedLeader : i.Leader;
                            if (armyLeader != null && !armyLeader.IsCaptive && armyLeader.LocationArchitecture != null
                                && armyLeader.Status == PersonStatus.Normal
                                &&
                                (!Session.Current.Scenario.IsPlayer(this.BelongedFaction) || armyLeader.LocationArchitecture.BelongedSection == this.BelongedSection))
                            {
                                armyLeader.MoveToArchitecture(this);
                            }
                        }
                    }
                }
            }

            this.SuspendTroopTransfer = 30;

            return transferredScale;
        }*/

        public int CallPeople(Architecture src, int cnt)
        {
            GameObjectList list = src.MovablePersons.GetList();
            if (list.Count > 1)
            {
                list.IsNumber = true;
                list.SmallToBig = false;
                list.PropertyName = this.FrontLine ? "FightingForce" : "Merit";
                list.ReSort();
            }
            if (src != null)
            {
                int num2 = 0;
                int called = 0;
                while (called < cnt && num2 < list.Count)
                {
                    Person p = list[num2] as Person;
                    if (!p.DontMoveMeUnlessIMust )
                    {
                        p.MoveToArchitecture(this);
                        called++;
                        foreach (Person q in p.AvailableVeryClosePersons)
                        {
                            if (!this.BelongedFaction.MayorList.HasGameObject(q))
                            {
                                q.MoveToArchitecture(this);
                                called++;
                            }


                        }
                    }
                    num2++;
                }
                return called;
            }
            return 0;
        }

        /// <summary>
        /// 【主动索取】：城池发现自己缺粮时，主动向周围请求 (V8.7 迟滞阈值版)
        /// 修复：引入 70% 警戒线，高于此线不请求，彻底解决"每回合微调"的问题
        /// </summary>
        public void WithdrawResources()
        {
            // 1. 基础检查
            if (this.BelongedFaction == null || this.BelongedSection == null) return;

            // =============================================================
            // 📊 1. 计算上限 (Cap)
            // =============================================================
            bool isCity = (this.Kind != null && (this.Kind.HasAgriculture || this.Kind.HasCommerce));

            int capFund, capFood;
            if (isCity)
            {
                // 城市：资金1万+人口加成，粮草300万
                capFund = 10000 + (this.Population / 10);
                capFood = Math.Min(3000000, 100000 + (this.MilitaryCount * 360));
            }
            else
            {
                // 关隘：严格锁死
                capFund = 5000;
                capFood = Math.Min(200000, Math.Max(30000, this.MilitaryCount * 60));
            }

            // =============================================================
            // 🛑 2. 迟滞判定 (Hysteresis) - 这一步能过滤 90% 的无效请求
            // =============================================================
            
            // 设定警戒线：只有低于 70% 才算"缺"，才允许发起请求
            // (这给资源留出了 30% 的波动空间，不会一有消耗就请求)
            int safetyFund = (int)(capFund * 0.7f);
            int safetyFood = (int)(capFood * 0.7f);

            bool needFund = this.Fund < safetyFund;
            bool needFood = this.Food < safetyFood;

            // 如果两个都还没跌破警戒线，直接躺平，什么都不做
            if (!needFund && !needFood) return;

            // =============================================================
            // 3. 寻找金主 (遍历邻接据点)
            // =============================================================
            var neighbors = new List<Architecture>();
            if (this.AILandLinks != null) 
                foreach(var link in this.AILandLinks) neighbors.Add((link is Architecture ? (Architecture)link : null));
            if (this.AIWaterLinks != null)
                foreach(var link in this.AIWaterLinks) neighbors.Add((link is Architecture ? (Architecture)link : null));

            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor.BelongedFaction != this.BelongedFaction) continue;

                // 邻居必须非常富裕 (城市120% / 关口200%)，防止把邻居拖下水
                bool neighborIsCity = (neighbor.Kind != null && (neighbor.Kind.HasAgriculture || neighbor.Kind.HasCommerce));
                float threshold = neighborIsCity ? 1.2f : 2.0f; 

                // 简单的邻居上限估算
                int nCapFund = neighborIsCity ? 20000 : 5000;
                int nCapFood = neighborIsCity ? 2000000 : 200000;

                bool neighborRichFund = neighbor.Fund > (nCapFund * threshold);
                bool neighborRichFood = neighbor.Food > (nCapFood * threshold);

                if (!neighborRichFund && !neighborRichFood) continue;

                // =============================================================
                // 📉 4. 计算请求量 (按需补满)
                // =============================================================

                int requestFund = 0;
                int requestFood = 0;

                // 如果缺钱且邻居有钱
                if (needFund && neighborRichFund)
                {
                    // 目标是补满到 100%，但只在跌破 70% 时触发
                    requestFund = capFund - this.Fund;
                    
                    // 邻居保留底线
                    int canGive = (int)(neighbor.Fund - (nCapFund * 1.0f));
                    requestFund = Math.Min(requestFund, canGive);
                }

                // 如果缺粮且邻居有粮
                if (needFood && neighborRichFood)
                {
                    requestFood = capFood - this.Food;
                    int canGive = (int)(neighbor.Food - (nCapFood * 1.0f));
                    requestFood = Math.Min(requestFood, canGive);
                }

                // =============================================================
                // 🛑 5. 最小运量过滤 (防止蚂蚁搬家)
                // =============================================================
                
                // 除非真的快饿死了(<=0)，否则小额请求直接忽略
                bool isEmergency = this.Fund <= 0 || this.Food <= 0;

                if (!isEmergency)
                {
                    // 如果请求量小于 5000 资金 或 5万 粮草，没必要折腾一趟
                    // (您日志里的 10229, 14944 这种刚刚过线的，会因为上面的 70% 判定直接被拦在外面)
                    if (requestFund < 5000 && requestFood < 50000) continue;
                }

                // 关隘安全阀
                if (!isCity)
                {
                    if (requestFood > 100000) requestFood = 100000;
                    if (requestFund > 10000) requestFund = 10000;
                }

                // 执行请求
                if (requestFund > 0 || requestFood > 0)
                {
                    this.CallResource(neighbor, requestFund, requestFood);

                    // 模拟加上了资源 (为了跳出循环)
                    this.Fund += requestFund;
                    this.Food += requestFood;

                    // 如果已经补满了，就别找下一个邻居了
                    if (this.Fund >= capFund && this.Food >= capFood) break;
                }
            }
        }

        public bool IsNetLosingPopulation
        {
            get
            {
                return this.RecentlyAttacked <= 0 && this.PopulationDevelopingRate < 0;
            }
        }

        private void assignWork(Person p, ArchitectureWorkKind k, bool[] need, bool needOnlyOneDomination, bool needOnlyOneMorale, bool needOnlyOneTrain)
        {
            switch (k)
            {
                case ArchitectureWorkKind.农业:
                    if (need[0]) p.WorkKind = ArchitectureWorkKind.农业;
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.商业:
                    if (need[1]) p.WorkKind = ArchitectureWorkKind.商业;
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.技术:
                    if (need[2]) p.WorkKind = ArchitectureWorkKind.技术;
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.统治:
                    if (need[3])
                    {
                        p.WorkKind = ArchitectureWorkKind.统治;
                        if (needOnlyOneDomination) // 因为补充导致的统治下降1或2点时，只需要选择1个武将进行统治就足够了
                            need[3] = false;
                    }
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.民心:
                    if (need[4])
                    {
                        p.WorkKind = ArchitectureWorkKind.民心;
                        if (needOnlyOneMorale) // 因为补充导致的民心下降1或2点时，只需要选择1个武将进行民心就足够了
                            need[4] = false;
                    }
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.耐久:
                    if (need[5]) p.WorkKind = ArchitectureWorkKind.耐久;
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                case ArchitectureWorkKind.训练:
                    if (need[6])
                    {
                        p.WorkKind = ArchitectureWorkKind.训练;
                        if (needOnlyOneTrain) // 因为补充导致的士气或战意下降1或2点时，只需要选择1个武将进行训练就足够了
                            need[6] = false;
                    }
                    else p.WorkKind = ArchitectureWorkKind.无;
                    break;
                default:
                    p.WorkKind = ArchitectureWorkKind.无;
                    break;
            }
        }

        // 从农业商业技术统治民心耐久训练随机挑一项工作，need储存相应工作是否需要做
        // 根据能力比例随机选择，例如：只有农业商业要做，农业200，商业100。则2/3做农业，1/3做商业
        // 有了这个函数可以避免用resort，太浪费时间而且不科学
        /// <summary>
        /// 🧠 智能工作分配系统 - 基于动态权重的武将工作分配
        /// 替代原有的固定阈值逻辑，实现更精细的内政管理
        /// </summary>
        /// <param name="p">要分配工作的武将</param>
        private void AIWorkSmart_Old(Person p)
        {
            // 获取动态阈值
            float limit = this.InternalAffairSaturationThreshold;
            
            // 🔥 NEW: 特殊处理统治度45-55区间，避免49停滞
            if (_architectureKind.HasDomination && this.Domination < 55)
            {
                // 在这个关键区间，强制提高统治优先级
                limit = Math.Max(limit, 0.75f); // 确保至少70%的目标
                
#if DEBUG
                if (this.Domination == 49)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI工作] {this.Name}: 检测到统治度49，提高优先级 (阈值={limit:F2})");
                }
#endif
            }
            
            // 定义工作权重数组
            // [0]=农业, [1]=商业, [2]=技术, [3]=统治, [4]=民心, [5]=耐久, [6]=训练
            float[] workWeights = new float[7];
            
            // --- 农业检查 ---
            if (_architectureKind.HasAgriculture && this.Agriculture < this.AgricultureCeiling)
            {
                if (this.Agriculture < this.AgricultureCeiling * limit)
                {
                    // 没达标：正常权重 (优先干)
                    workWeights[0] = 100.0f;
                }
                else
                {
                    // 达标了但没满：填缝权重 (没事干才干这个)
                    workWeights[0] = 1.0f;
                }
            }
            
            // --- 商业检查 ---
            if (_architectureKind.HasCommerce && this.Commerce < this.CommerceCeiling)
            {
                if (this.Commerce < this.CommerceCeiling * limit)
                {
                    workWeights[1] = 100.0f;
                }
                else
                {
                    workWeights[1] = 1.0f;
                }
            }
            
            // --- 技术检查 ---
            if (_architectureKind.HasTechnology && this.Technology < this.TechnologyCeiling)
            {
                if (this.Technology < this.TechnologyCeiling * limit)
                {
                    workWeights[2] = 100.0f;
                }
                else
                {
                    workWeights[2] = 1.0f;
                }
            }
            
            // --- 统治检查 ---
            if (_architectureKind.HasDomination && this.Domination < this.DominationCeiling)
            {
                if (this.Domination < this.DominationCeiling * limit)
                {
                    workWeights[3] = 100.0f;
                }
                else
                {
                    workWeights[3] = 1.0f;
                }
            }
            
            // --- 民心检查 ---
            if (_architectureKind.HasMorale && this.Morale < this.MoraleCeiling)
            {
                if (this.Morale < this.MoraleCeiling * limit)
                {
                    workWeights[4] = 100.0f;
                }
                else
                {
                    workWeights[4] = 1.0f;
                }
            }
            
            // --- 耐久检查 ---
            if (_architectureKind.HasEndurance && this.Endurance < this.EnduranceCeiling)
            {
                if (this.Endurance < this.EnduranceCeiling * limit)
                {
                    workWeights[5] = 100.0f;
                }
                else
                {
                    workWeights[5] = 1.0f;
                }
            }
            
            // --- 训练检查 ---
            MilitaryList trainingMilitaryList = this.GetTrainingMilitaryList();
            if (trainingMilitaryList.Count > 0)
            {
                // 训练总是高优先级（军事需求）
                workWeights[6] = 150.0f;
                
                // 检查是否只需要一人训练
                if (trainingMilitaryList.Count == 1)
                {
                    Military m = trainingMilitaryList[0] as Military;
                    if (m.Morale >= m.MoraleCeiling - 3 && m.Combativity >= m.CombativityCeiling - 3)
                    {
                        workWeights[6] = 50.0f; // 降低权重，只需要一人训练
                    }
                }
            }
            
            // --- 特殊情况调整 ---
            
            // 最近被攻击：优先耐久修复
            if (this.RecentlyAttacked > 0)
            {
                if (this.Endurance < this.EnduranceCeiling)
                {
                    workWeights[5] = 200.0f; // 耐久最高优先级
                }
                // 降低其他内政权重
                workWeights[0] = Math.Min(workWeights[0], 10.0f);
                workWeights[1] = Math.Min(workWeights[1], 10.0f);
                workWeights[2] = Math.Min(workWeights[2], 10.0f);
            }
            
            // 资金不足：优先商业
            if (!this.IsFundEnough)
            {
                workWeights[1] *= 2.0f;
            }
            
            // 粮食不足：优先农业
            if (!this.IsFoodEnough)
            {
                workWeights[0] *= 2.0f;
            }
            
            // --- 最终决策 ---
            this.AssignWorkByWeights(p, workWeights);
            
#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[AIWorkSmart] {p.Name} 在 {this.Name} 被分配工作: {p.WorkKind} " +
                    $"(阈值={limit:F2}, 权重=[{string.Join(",", workWeights.Select(w => w.ToString("F1")))}])");
            }
#endif
        }

        /// <summary>
        /// 基于权重数组分配工作
        /// </summary>
        /// <param name="p">武将</param>
        /// <param name="workWeights">工作权重数组</param>
        private void AssignWorkByWeights_Old(Person p, float[] workWeights)
        {
            // 计算总权重（基于武将能力）
            float totalWeight = 0f;
            float[] adjustedWeights = new float[workWeights.Length];
            
            for (int i = 0; i < workWeights.Length; i++)
            {
                if (workWeights[i] > 0)
                {
                    float abilityWeight = GetPersonAbilityForWork_Old(p, i);
                    adjustedWeights[i] = workWeights[i] * abilityWeight;
                    totalWeight += adjustedWeights[i];
                }
            }
            
            if (totalWeight == 0)
            {
                p.WorkKind = ArchitectureWorkKind.无;
                return;
            }
            
            // 随机选择工作（基于权重）
            float randomValue = (float)(GameObject.Random(10000) / 10000.0) * totalWeight;
            float currentWeight = 0f;
            
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                if (adjustedWeights[i] > 0)
                {
                    currentWeight += adjustedWeights[i];
                    if (randomValue <= currentWeight)
                    {
                        AssignSpecificWork_Old(p, i);
                        return;
                    }
                }
            }
            
            // 兜底：如果没有分配到工作，分配第一个可用的
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                if (adjustedWeights[i] > 0)
                {
                    AssignSpecificWork_Old(p, i);
                    return;
                }
            }
            
            p.WorkKind = ArchitectureWorkKind.无;
        }

        /// <summary>
        /// 获取武将在特定工作上的能力值
        /// </summary>
        /// <param name="p">武将</param>
        /// <param name="workIndex">工作索引</param>
        /// <returns>能力值</returns>
        private float GetPersonAbilityForWork_Old(Person p, int workIndex)
        {
            switch (workIndex)
            {
                case 0: return p.AgricultureAbility;
                case 1: return p.CommerceAbility;
                case 2: return p.TechnologyAbility;
                case 3: return p.DominationAbility;
                case 4: return p.MoraleAbility;
                case 5: return p.EnduranceAbility;
                case 6: return p.TrainingAbility;
                case 7:
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.补充) / 200.0f);

                case 8:
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.赈灾) / 200.0f);

                default: return 1.0f;
            }
        }

        /// <summary>
        /// 分配特定工作给武将
        /// </summary>
        /// <param name="p">武将</param>
        /// <param name="workIndex">工作索引</param>
        private void AssignSpecificWork_Old(Person p, int workIndex)
        {
            switch (workIndex)
            {
                case 0: p.WorkKind = ArchitectureWorkKind.农业; break;
                case 1: p.WorkKind = ArchitectureWorkKind.商业; break;
                case 2: p.WorkKind = ArchitectureWorkKind.技术; break;
                case 3: p.WorkKind = ArchitectureWorkKind.统治; break;
                case 4: p.WorkKind = ArchitectureWorkKind.民心; break;
                case 5: p.WorkKind = ArchitectureWorkKind.耐久; break;
                case 6: p.WorkKind = ArchitectureWorkKind.训练; break;
                default: p.WorkKind = ArchitectureWorkKind.无; break;
            }
        }

        #region AI内政评分系统 - 整合版

        /// <summary>
        /// 🎯 计算城市内政发展评分
        /// 功能：基于动态阈值和城市状况计算综合评分，用于AI决策
        /// 策略：征兵优先城市得高分，内政需求城市根据缺口计算分数
        /// </summary>
        /// <returns>城市内政评分，分数越高优先级越高</returns>
        public float CalculateInternalAffairScore()
        {
            // 🛡️ 递归保护：如果正在执行人员调配，返回简化评分
            if (Faction._isExecutingPersonnelAllocation)
            {
                // 返回基于人口的简化评分，避免复杂计算
                return this.Population / 1000.0f;
            }
            
            float score = 0.0f;
            
            // 获取动态饱和度阈值
            float saturation = this.InternalAffairSaturationThreshold;
            
            // 征兵逻辑优先：如果城市适合征兵，给予高分
            if (this.IsGoodForRecruitment())
            {
                score = 10.0f; // 征兵城市基础高分
                
                // 征兵城市额外评分因素
                if (this.Population > this.PopulationCeiling * 0.8f)
                {
                    score += 2.0f; // 人口充足加分
                }
                
                if (this.Fund > this.EnoughFund)
                {
                    score += 1.0f; // 资金充足加分
                }
                
                if (this.Food > this.EnoughFood)
                {
                    score += 1.0f; // 粮食充足加分
                }
                
                return score;
            }
            
            // 内政发展评分：只有非征兵城市才计算内政小分
            if (score < 10.0f)
            {
                // 农业发展评分
                if (this.Agriculture < this.AgricultureCeiling * saturation)
                {
                    score += (this.AgricultureCeiling - this.Agriculture) / 600.0f;
                }
                
                // 商业发展评分
                if (this.Commerce < this.CommerceCeiling * saturation)
                {
                    score += (this.CommerceCeiling - this.Commerce) / 600.0f;
                }
                
                // 技术发展评分
                if (this.Technology < this.TechnologyCeiling * saturation)
                {
                    score += (this.TechnologyCeiling - this.Technology) / 400.0f;
                }
                
                // 统治发展评分
                if (this.Domination < this.DominationCeiling * saturation)
                {
                    score += (this.DominationCeiling - this.Domination) / 500.0f;
                }
                
                // 民心发展评分
                if (this.Morale < this.MoraleCeiling * saturation)
                {
                    score += (this.MoraleCeiling - this.Morale) / 500.0f;
                }
                
                // 耐久修复评分（特殊处理）
                if (this.Endurance < this.EnduranceCeiling * 0.9f)
                {
                    float enduranceScore = (this.EnduranceCeiling - this.Endurance) / 300.0f;
                    
                    // 最近被攻击的城市，耐久修复优先级更高
                    if (this.RecentlyAttacked > 0)
                    {
                        enduranceScore *= 2.0f;
                    }
                    
                    score += enduranceScore;
                }
                
                // 城市类型基础分
                score += (this.Kind.Name == "城市" || this.Kind.Name == "City") ? 2.0f : 0.1f;
                
                // 战略位置加分
                if (this.IsStrategicPosition())
                {
                    score += 1.0f;
                }
                
                // 资源状况调整
                if (!this.IsFundEnough)
                {
                    score *= 0.8f; // 资金不足时降低优先级
                }
                
                if (!this.IsFoodEnough)
                {
                    score *= 0.8f; // 粮食不足时降低优先级
                }
            }
            
            return score;
        }

        /// <summary>
        /// 判断城市是否适合征兵
        /// </summary>
        /// <returns>true表示适合征兵，false表示适合内政发展</returns>
        private bool IsGoodForRecruitment()
        {
            // 🛡️ 递归保护：如果正在执行人员调配，返回false避免复杂判断
            if (Faction._isExecutingPersonnelAllocation)
            {
                return false;
            }
            
            // 征兵判断逻辑：人口充足、资源充足、有征兵需求
            return this.Population > this.PopulationCeiling * 0.7f &&
                   this.Fund > this.EnoughFund &&
                   this.Food > this.EnoughFood &&
                   this.BelongedFaction != null &&
                   this.BelongedFaction.NeedMoreTroops();
        }

        /// <summary>
        /// 判断是否为战略要地
        /// </summary>
        /// <returns>true表示战略位置重要</returns>
        private bool IsStrategicPosition()
        {
            // 战略位置判断：边境城市、交通要道、资源丰富等
            if (this.Kind.Name == "城市" || this.Kind.Name == "City") return true;
            
            // 检查是否为边境城市（邻近敌对势力）
            foreach (Architecture neighbor in this.AILandLinks)
            {
                if (neighbor.BelongedFaction != this.BelongedFaction)
                {
                    return true; // 边境城市具有战略价值
                }
            }
            
            // 检查是否为交通枢纽（连接多个城市）
            if (this.AILandLinks.Count >= 3)
            {
                return true;
            }
            
            return false;
        }



        /// <summary>
        /// 获取武将在特定工作上的能力值 (Enhanced)
        /// </summary>
        private float GetPersonAbilityForWork(Person p, int workIndex)
        {
            const int STAT_THRESHOLD = 60;
            
            switch (workIndex)
            {
                case 0: // 农业 (Politics)
                    if (p.Politics < STAT_THRESHOLD) return (p.GetWorkAbility(ArchitectureWorkKind.农业) / 100.0f) * 0.6f;
                    return Math.Min(1.25f, p.GetWorkAbility(ArchitectureWorkKind.农业) / 100.0f);
                    
                case 1: // 商业 (Politics)
                    if (p.Politics < STAT_THRESHOLD) return (p.GetWorkAbility(ArchitectureWorkKind.商业) / 100.0f) * 0.6f;
                    return Math.Min(1.25f, p.GetWorkAbility(ArchitectureWorkKind.商业) / 100.0f);
                    
                case 2: // 技术 (Intelligence)
                    if (p.Intelligence < STAT_THRESHOLD) return (p.GetWorkAbility(ArchitectureWorkKind.技术) / 100.0f) * 0.6f;
                    return Math.Min(1.25f, p.GetWorkAbility(ArchitectureWorkKind.技术) / 100.0f);
                    
                case 3: // 统治 (Average of Strength and Glamour)
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.统治) / 200.0f);
                    
                case 4: // 民心 (Average of Strength and Glamour)
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.民心) / 200.0f);
                    
                case 5: // 耐久 (Strength + Command)
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.耐久) / 200.0f);
                    
                case 6: // 训练 (Strength + Command, with bonus for fierce generals)
                    float trainScore = Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.训练) / 200.0f);
                    if (p.Strength > 80) trainScore *= 1.2f;
                    return trainScore;

                case 7:
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.补充) / 200.0f);

                case 8:
                    return Math.Min(1.5f, p.GetWorkAbility(ArchitectureWorkKind.赈灾) / 200.0f);

                default: return 1.0f;
            }
        }

        /// <summary>
        /// 分配特定工作给武将
        /// </summary>
        private bool AssignSpecificWork(Person p, int workIndex)
        {
            switch (workIndex)
            {
                case 0: p.WorkKind = ArchitectureWorkKind.农业; return true;
                case 1: p.WorkKind = ArchitectureWorkKind.商业; return true;
                case 2: p.WorkKind = ArchitectureWorkKind.技术; return true;
                case 3: p.WorkKind = ArchitectureWorkKind.统治; return true;
                case 4: p.WorkKind = ArchitectureWorkKind.民心; return true;
                case 5: p.WorkKind = ArchitectureWorkKind.耐久; return true;
                case 6: p.WorkKind = ArchitectureWorkKind.训练; return true;
                case 7: return this.TryAssignRecruitmentWork(p);
                case 8: p.WorkKind = ArchitectureWorkKind.赈灾; return true;
                default: return false;
            }
        }

        #endregion

        private bool AdjacentToHostileByWater
        {
            get
            {
                if (this.BelongedFaction == null) return true;
                foreach (Architecture i in AIWaterLinks)
                {
                    if (i.BelongedFaction == null || !this.BelongedFaction.IsFriendly(i.BelongedFaction))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void AutoRecruit()
        {
            // System.Diagnostics.Debug.WriteLine($"");
            // System.Diagnostics.Debug.WriteLine($"⚔️ ═══════════════════════════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"⚔️ [编队招募检查] 城市: {this.Name}");
            // System.Diagnostics.Debug.WriteLine($"⚔️ 资金: {this.Fund}, 兵役人口: {this.MilitaryPopulation}");
            // System.Diagnostics.Debug.WriteLine($"⚔️ 编队数量: {this.Militaries.Count}");
            // System.Diagnostics.Debug.WriteLine($"⚔️ 前线状态: FrontLine={this.FrontLine}, HostileLine={this.HostileLine}");
            // System.Diagnostics.Debug.WriteLine($"⚔️ 威胁状态: HasHostileTroops={this.HasHostileTroopsInView()}");
            // System.Diagnostics.Debug.WriteLine($"⚔️ ═══════════════════════════════════════════════════════════");
            
            // 显示每个编队的状态
            /*
            foreach (Military m in this.Militaries)
            {
                float fillRate = m.Kind.MaxScale > 0 ? (float)m.Scales / m.Kind.MaxScale * 100 : 0;
                string recruitStatus = m.RecruitmentPerson != null ? m.RecruitmentPerson.Name : "无";
                System.Diagnostics.Debug.WriteLine($"  📋 {m.Name}: {m.Scales}/{m.Kind.MaxScale} ({fillRate:F1}%) " +
                    $"招募中: {recruitStatus}");
            }
            */
            
            // 🔥 修复：放宽招募条件，确保编队能及时补充
            if (this.BelongedFaction != null &&
                !Session.Current.Scenario.IsPlayer(this.BelongedFaction) &&
                !this.CanExecuteAIRecruitment(false))
            {
                foreach (Military m in this.Militaries)
                {
                    m.StopRecruitment();
                }
                return;
            }

            bool shouldRecruit = false;
            string recruitReason = "";
            
            // 1. 标准条件：资金充裕 (>500)
            if (RecruitmentAvail() && this.Fund > 500 && (this.IsFundEnough || this.HasHostileTroopsInView()))
            {
                shouldRecruit = true;
                recruitReason = "标准条件：资金充足且(富裕或有威胁)";
            }
            // 2. 强制招募：严重缺员 (>200)
            else if (RecruitmentAvail() && this.Fund > 200 && HasUnderstaffedMilitaries())
            {
                shouldRecruit = true;
                recruitReason = "强制招募：编队严重缺员(<30%)";
            }
            // 🔥 FIX V1.1: 新增"日常维护"逻辑
            // 填补 200-500 资金段的逻辑空白
            // 如果资金尚可(>300)，且有编队兵力不满 80%，则允许进行非紧急招募
            else if (RecruitmentAvail() && this.Fund > 300 && HasMilitariesBelowRatio(0.8f))
            {
                shouldRecruit = true;
                recruitReason = "日常维护：资金尚可且编队不满80%";
            }
            // 3. 前线优先：资金紧张但处于前线 (>100)
            else if (RecruitmentAvail() && this.Fund > 100 && (this.FrontLine || this.HostileLine))
            {
                shouldRecruit = true;
                recruitReason = "前线优先：保证前线兵力";
            }
            // 4. 紧急招募：完全无兵 (>50)
            else if (RecruitmentAvail() && this.Fund > 50 && this.Militaries.Count > 0 && this.MilitaryCount == 0)
            {
                shouldRecruit = true;
                recruitReason = "紧急招募：有编队但无兵力";
            }
            
            // System.Diagnostics.Debug.WriteLine($"⚔️ [招募决策] 是否招募: {shouldRecruit}");
            /*
            if (shouldRecruit)
            {
                System.Diagnostics.Debug.WriteLine($"⚔️ [招募原因] {recruitReason}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚔️ [跳过原因] 不满足招募条件");
                System.Diagnostics.Debug.WriteLine($"  - RecruitmentAvail: {RecruitmentAvail()}");
                System.Diagnostics.Debug.WriteLine($"  - Fund: {this.Fund} (需要>50)");
                System.Diagnostics.Debug.WriteLine($"  - IsFundEnough: {this.IsFundEnough}");
                System.Diagnostics.Debug.WriteLine($"  - HasHostileTroops: {this.HasHostileTroopsInView()}");
                System.Diagnostics.Debug.WriteLine($"  - HasUnderstaffed: {HasUnderstaffedMilitaries()}");
            }
            */
            
            if (shouldRecruit)
            {
                MilitaryList recruitmentMilitaryList = this.GetRecruitmentMilitaryList();

                // System.Diagnostics.Debug.WriteLine($"⚔️ [可招募编队] 数量: {recruitmentMilitaryList.Count}");
                /*
                foreach (Military m in recruitmentMilitaryList)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {m.Name}: {m.Quantity}/{m.Kind.MaxScale}");
                }
                */

                // 🔥 修复：改为按综合优先级排序，而不是按功勋排序
                // 优先补充：兵力缺口大、上限小的编队
                foreach (Military m in recruitmentMilitaryList)
                {
                    // 1. 兵力缺口比例（0-1，越大越需要补充）
                    float gapRatio = m.Kind.MaxScale > 0 ? 
                        (float)(m.Kind.MaxScale - m.Quantity) / m.Kind.MaxScale : 0f;
                    
                    // 2. 上限权重（上限越大，权重越低）
                    // 标准正规军上限为10000，超过此值的大编制兵种权重降低
                    float scaleWeight = 10000f / Math.Max(m.Kind.MaxScale, 10000);
                    
                    // 3. 功勋权重（大幅降低影响，避免功勋主导决策）
                    // 原来：1.0 + (Merit / 1000) * 0.1，对于Merit=6000，权重=1.6
                    // 修改：1.0 + (Merit / 1000) * 0.01，对于Merit=6000，权重=1.06
                    float meritWeight = 1.0f + (m.Merit / 1000f) * 0.01f;
                    
                    // 4. 综合评分
                    m.RecruitmentPriority = gapRatio * scaleWeight * meritWeight;
                    
                    // System.Diagnostics.Debug.WriteLine($"  📊 {m.Name}: 缺口={gapRatio:F3}, 上限权重={scaleWeight:F3}, 功勋权重={meritWeight:F3}, 综合分数={m.RecruitmentPriority:F3}");
                }

                recruitmentMilitaryList.PropertyName = "RecruitmentPriority";
                recruitmentMilitaryList.IsNumber = true;
                recruitmentMilitaryList.SmallToBig = false; // 分数高的优先
                recruitmentMilitaryList.ReSort();

                GameObjectList recruitmentPersonList = this.PersonsExcludeNvGuan.GetList();
                recruitmentPersonList.PropertyName = "RecruitmentAbility";
                recruitmentPersonList.IsNumber = true;
                recruitmentPersonList.SmallToBig = false;
                recruitmentPersonList.ReSort();

                int recruitCount = Math.Min(recruitmentMilitaryList.Count, recruitmentPersonList.Count);
                
                // 🔥 修复：放宽兵役人口限制
                if (this.MilitaryPopulation < 10 && recruitCount > 0)
                {
                    recruitCount = Math.Min(recruitCount, 2); // 从1提升到2
                    // System.Diagnostics.Debug.WriteLine($"⚔️ [兵役人口不足] 限制招募数量为: {recruitCount}");
                }
                
                // System.Diagnostics.Debug.WriteLine($"⚔️ [开始招募] 可招募编队: {recruitmentMilitaryList.Count}, 可用武将: {recruitmentPersonList.Count}, 实际招募: {recruitCount}");
                 
                for (int i = 0; i < recruitCount; ++i)
                {
                    var person = recruitmentPersonList[i] as Person;
                    var military = recruitmentMilitaryList[i] as Military;
                    
                    // System.Diagnostics.Debug.WriteLine($"  ➜ {person.Name} 开始招募 {military.Name} (招募能力:{person.RecruitmentAbility})");
                    person.RecruitMilitary(military);
                }
                
                // System.Diagnostics.Debug.WriteLine($"⚔️ [招募完成] 已分配 {recruitCount} 个招募任务");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"⚔️ [停止招募] 停止所有编队的招募工作");
                foreach (Military m in this.Militaries)
                {
                    if (m.RecruitmentPerson != null)
                    {
                        // System.Diagnostics.Debug.WriteLine($"  ⏹️ 停止 {m.Name} 的招募 (原招募者: {m.RecruitmentPerson.Name})");
                    }
                    m.StopRecruitment();
                }
            }
            
            // System.Diagnostics.Debug.WriteLine($"⚔️ ═══════════════════════════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"");
        }
        
        /// <summary>
        /// 🔥 新增：检查是否有严重缺员的编队
        /// </summary>
        private bool HasUnderstaffedMilitaries()
        {
            foreach (Military m in this.Militaries)
            {
                if (m.Kind.MinScale > 0 && m.Scales < m.Kind.MinScale * 0.3f) // 低于最小规模的30%
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 🔥 FIX V1.1: 检查是否有编队兵力比例低于指定阈值
        /// </summary>
        /// <param name="ratio">比例阈值 (0.0 - 1.0)</param>
        /// <returns>如果有编队低于该比例，返回 true</returns>
        private bool HasMilitariesBelowRatio(float ratio)
        {
            foreach (Military m in this.Militaries)
            {
                if (m.Kind.MaxScale > 0)
                {
                    // 计算当前满员率
                    float currentRatio = (float)m.Quantity / m.Kind.MaxScale;
                    // 如果低于阈值，且不是运输队
                    if (currentRatio < ratio && !m.IsTransport)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AIWork(bool forPlayer)
        {
            this.StopAllWork();
            if (!this.HasPerson()) return;
            this.EnsureMilitaryWorkTargetsForAI();

            // 🔥 资金不足时的应急策略：全员训练
            if ((!this.IsFundEnough && this.RecentlyAttacked <= 0) || 
                this.Fund < Session.Current.Scenario.Parameters.InternalFundCost)
            {
                MilitaryList trainingMilitaryList = this.GetTrainingMilitaryList();
                if (trainingMilitaryList.Count > 0)
                {
                    foreach (Person p in this.Persons)
                    {
                        p.WorkKind = ArchitectureWorkKind.训练;
                    }
                    return;
                }
            }

            // 🧠 使用新的智能工作分配系统
            foreach (Person p in this.Persons)
            {
                // 为每个武将单独计算最优工作分配
                this.AIWorkSmart(p);
                
                // 如果智能分配失败，使用兜底逻辑
                if (p.WorkKind == ArchitectureWorkKind.无)
                {
                    this.AssignDefaultWork(p);
                }
            }

#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                var workCounts = new Dictionary<ArchitectureWorkKind, int>();
                foreach (Person p in this.Persons)
                {
                    if (!workCounts.ContainsKey(p.WorkKind))
                        workCounts[p.WorkKind] = 0;
                    workCounts[p.WorkKind]++;
                }
                
                string workSummary = string.Join(", ", workCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                System.Diagnostics.Debug.WriteLine($"[AIWork] {this.Name} 工作分配完成: {workSummary} " +
                    $"(阈值={this.InternalAffairSaturationThreshold:F2})");
            }
#endif
        }

        public void WithdrawMilitaries()
        {
            GameObjectList list = this.Militaries.GetList();
            foreach (Military m in list)
            {
                this.DisbandMilitary(m);
            }
        }

        private void AIRecruitMilitary()
        {
            // 0. 基础门槛：如果连补兵的资格都没有（没钱/没人口），直接不谈
            if (!this.NewMilitaryAvail()) return;
            if (this.BelongedFaction != null &&
                !Session.Current.Scenario.IsPlayer(this.BelongedFaction) &&
                !this.CanExecuteAIRecruitment(true))
            {
                return;
            }

            // =========================================================
            // 🎲 软上限概率控制系统 (Soft Cap System)
            // =========================================================
            
            // 1. 定义软上限 (Soft Cap) - 这是一个"理想"的编队数量
            // 基础: 5队
            int softLimit = 5; 
            
            // 规模加成: 大城+3, 巨城+6
            if (this.PopulationCeiling >= 150000) softLimit += 6;
            else if (this.PopulationCeiling >= 50000) softLimit += 3;

            // 武将修正: 每2个武将允许多带1个队 (防止有兵无将)
            softLimit += this.PersonCount / 2;

            // 2. 定义硬底线 (Hard Cap) - 绝对不能超过的红线
            // 允许在软上限的基础上最多再溢出 3 队
            int hardLimit = softLimit + 3;

            // 战时状态：如果有敌军，直接放宽限制
            bool isEmergency = this.HasHostileTroopsInView();

            // 3. 逻辑判断
            if (isEmergency)
            {
                // 战时只看硬上限，无视软上限惩罚
                if (this.MilitaryCount >= hardLimit + 5) return;
            }
            else
            {
                // --- 和平时期逻辑 ---

                // A. 绝对硬上限拦截
                if (this.MilitaryCount >= hardLimit) 
                {
                    return; 
                }

                // B. 软上限概率拦截 (核心逻辑)
                if (this.MilitaryCount >= softLimit)
                {
                    // 计算目前超标了多少 (比如超了1个，overflow=1)
                    int overflow = this.MilitaryCount - softLimit;
                    
                    // 基础成功率：随着超标数量指数级下降
                    // 超0个(刚满): 50% 概率新建
                    // 超1个: 25% 概率新建
                    // 超2个: 12% 概率新建
                    int successChance = 50 / (int)Math.Pow(2, overflow);

                    // [土豪修正] 如果钱太多(>10万)，哪怕超标了也允许它任性，概率翻倍
                    if (this.Fund > 100000) successChance *= 2;

                    // 掷骰子 (0-99)
                    // 如果 随机数 > 成功率，则"本次没有意愿新建"，跳过
                    // AI 会转而去执行 AutoRecruit 给老队伍补兵
                    if (GameObject.Random(100) >= successChance)
                    {
                        return;
                    }
                }
            }
            // =========================================================

            
            bool flag2 = this.RecentlyAttacked > 0;
            if ((_architectureKind.HasPopulation && (flag2 || (this.BelongedFaction.PlanTechniqueArchitecture != this))) &&
                (flag2 || (this.Population > ((this.RecruitmentPopulationBoundary * (1 + (int)this.BelongedFaction.Leader.StrategyTendency * 0.5f)) + GameObject.Random(this.RecruitmentPopulationBoundary)))))
            {
                int unfullArmyCount = 0;
                int unfullNavalArmyCount = 0;
                foreach (Military military in this.Militaries)
                {
                    if (military.Scales < ((((float)military.Kind.MaxScale) / ((float)military.Kind.MinScale)) * 0.75f) && !military.IsTransport)
                    {
                        unfullArmyCount++;
                        if (military.Kind.Type == MilitaryType.水军)
                        {
                            unfullNavalArmyCount++;
                        }
                    }
                }
                int unfullArmyCountThreshold;
                if (this.IsFoodAbundant && this.IsFundAbundant)
                {
                    unfullArmyCountThreshold = Math.Min((this.MilitaryPopulation) * (this.BelongedFaction.Leader.Ambition + 1) / Session.Parameters.AINewMilitaryPopulationThresholdDivide + 1, 
                        (this.PersonCount + this.MovingPersonCount) * (this.BelongedFaction.Leader.Ambition + 1) / Session.Parameters.AINewMilitaryPersonThresholdDivide + 1);
                }
                else
                {
                    unfullArmyCountThreshold = 1;
                }
                if (unfullArmyCount < unfullArmyCountThreshold)
                {
                    if (this.AIWaterLinks.Count > 0 && this.IsBesideWater && this.HasShuijunMilitaryKind() && 
                        (this.EffectiveMilitaryCount == 0 || GameObject.Chance((int)(100 - this.ShuijunMilitaryCount / (double)this.EffectiveMilitaryCount * 100))))
                    {
                        this.AIRecruitment(true, false);
                    }
                    else if (this.AILandLinks.Count <= 0)
                    {
                        this.AIRecruitment(true, false);
                    }
                    else
                    {
                        int siegeCount = 0;
                        foreach (Military m in this.Militaries)
                        {
                            if (m.Kind.Type == MilitaryType.器械)
                            {
                                siegeCount++;
                            }
                        }
                        if (siegeCount < this.Militaries.Count / (this.IsBesideWater ? 6 : 3))
                        {
                            this.AIRecruitment(false, true);
                        }
                        else
                        {
                            this.AIRecruitment(false, false);
                        }
                    }
                }
            }

            //disband unused transports except one
            MilitaryList ml = new MilitaryList();
            foreach (Military m in Militaries)
            {
                if (m.IsTransport)
                {
                    ml.Add(m);
                }
            }
            if (ml.Count > 1)
            {
                Military minTroop = null;
                int min = int.MaxValue;
                foreach (Military m in ml)
                {
                    if (m.Quantity < min)
                    {
                        min = m.Quantity;
                        minTroop = m;
                    }
                }
                this.DisbandMilitary(minTroop);
            }
        }

        private void ConvinceNoFactionAI()
        {
            if (this.HasPerson() && this.IsFundEnough && this.HasNoFactionPerson() && !this.HasHostileTroopsInView())
            {
                GameObjectList convincer = this.PersonsExcludeNvGuan.GetList();
                convincer.SmallToBig = false;
                convincer.PropertyName = "ConvinceAbility";
                convincer.IsNumber = true;
                convincer.ReSort();

                GameObjectList convinced = this.NoFactionPersons.GetList();
                convinced.SmallToBig = false;
                convinced.PropertyName = "Merit";
                convinced.IsNumber = true;
                convinced.ReSort();

                foreach (Person p in convinced)
                {
                    foreach (Person q in convincer)
                    {
                        if (q.CanConvinceChance(p) > 10 && q.Status == PersonStatus.Normal)
                        {
                            q.OutsideDestination = this.ArchitectureArea.Centre;
                            q.GoForConvince(p);
                            break;
                        }
                    }
                }
            }
        }

        private void ConvinceCaptivesAI(Architecture architecture2)
        {
            if (this.BelongedFaction == null) return;
            if (this.HasHostileTroopsInView()) return;

            GameObjectList convincer = this.PersonsExcludeNvGuan.GetList();
            convincer.SmallToBig = false;
            convincer.PropertyName = "ConvinceAbility";
            convincer.IsNumber = true;
            convincer.ReSort();

            GameObjectList convinced = architecture2.Captives;
            convinced.SmallToBig = false;
            convinced.PropertyName = "Merit";
            convinced.IsNumber = true;
            convinced.ReSort();

            foreach (Captive p in convinced)
            {
                foreach (Person q in convincer)
                {
                    if (q.CanConvinceChance(p.CaptivePerson) > 20 && q.Status == PersonStatus.Normal)
                    {
                        q.OutsideDestination = this.ArchitectureArea.Centre;
                        q.GoForConvince(p.CaptivePerson);
                        break;
                    }
                }
            }
        }

        private List<Architecture> GettingInformationArchitectures()
        {
            if (this.BelongedFaction == null) return new List<Architecture>();
            return this.BelongedFaction.GettingInformationArchitectures();
        }

        private void OutsideTacticsAI()
        {
            ConvinceNoFactionAI();

            if (this.PlanArchitecture == null && this.RecentlyAttacked <= 0 && this.HasPerson() && this.IsFundEnough)
            {
                Architecture architecture2;
                int diplomaticRelation;
                Person firstHalfPerson;
                ArchitectureList unknownArch = new ArchitectureList();
                ArchitectureList knownArch = new ArchitectureList();
                foreach (Architecture architecture in this.GetClosestArchitectures(20, 40))
                {
                    if (!this.BelongedFaction.IsArchitectureKnown(architecture))
                    {
                        unknownArch.Add(architecture);
                    }
                    else
                    {
                        knownArch.Add(architecture);
                    }
                }
                /*
                if (this.BelongedSection != null && (unknownArch.Count > 0) && this.BelongedSection.AIDetail.AllowInvestigateTactics)
                {
                    if (unknownArch.Count > 1)
                    {
                        unknownArch.PropertyName = "Population";
                        unknownArch.IsNumber = true;
                        unknownArch.ReSort();
                    }
                    if ((((this.RecentlyAttacked <= 0) && (GameObject.Random(40) < GameObject.Random(unknownArch.Count))) && GameObject.Chance(20)) && this.InformationAvail())
                    {
                        architecture2 = unknownArch[GameObject.Random(unknownArch.Count / 2)] as Architecture;
                        List<Architecture> gettingInformation = GettingInformationArchitectures();
                        if (!this.BelongedFaction.IsArchitectureKnown(architecture2) && architecture2.BelongedFaction != null && !this.IsFriendly(architecture2.BelongedFaction) && !gettingInformation.Contains(architecture2))
                        {
                            diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, architecture2.BelongedFaction.ID);
                            if (((diplomaticRelation >= 0) && (GameObject.Random(diplomaticRelation + 200) <= GameObject.Random(50))) || ((diplomaticRelation < 0) && (GameObject.Random(Math.Abs(diplomaticRelation) + 100) >= GameObject.Random(100))))
                            {
                                firstHalfPerson = this.GetFirstHalfPerson("InformationAbility");
                                if ((((firstHalfPerson != null) && (!this.HasFollowedLeaderMilitary(firstHalfPerson) || GameObject.Chance(10))) && (GameObject.Random(firstHalfPerson.NonFightingNumber) > GameObject.Random(firstHalfPerson.FightingNumber))) && (GameObject.Random(firstHalfPerson.FightingNumber) < 100))
                                {
                                    firstHalfPerson.CurrentInformationKind = this.GetFirstHalfInformationKind();
                                    if (firstHalfPerson.CurrentInformationKind != null)
                                    {
                                        firstHalfPerson.GoForInformation(Session.Current.Scenario.GetClosestPoint(architecture2.ArchitectureArea, this.Position));
                                    }
                                }
                            }
                        }
                    }
                }
                */

                if (ShouldExecuteBasicAI())
                {

                    if ((this.BelongedSection != null) && ((knownArch.Count > 0) && (this.PlanArchitecture == null)) && this.BelongedSection.AIDetail.AllowPersonTactics)
                    {
                        if (knownArch.Count > 1)
                        {
                            knownArch.PropertyName = "PersonCount";
                            knownArch.IsNumber = true;
                            knownArch.ReSort();
                        }
                        if ((this.HasPerson() && (GameObject.Random(this.Fund) >= this.GossipArchitectureFund)) && GameObject.Chance(50))
                        {
                            ArchitectureList list3 = new ArchitectureList();
                            foreach (Architecture architecture in knownArch)
                            {
                                if ((architecture.BelongedFaction != this.BelongedFaction) && (architecture.BelongedFaction != null))
                                {
                                    list3.Add(architecture);
                                }
                            }
                            if (list3.Count > 0)
                            {
                                architecture2 = list3[GameObject.Random(list3.Count / 2)] as Architecture;
                                if (GameObject.Chance(100 - architecture2.noEscapeChance * 2))
                                {
                                    if (!this.IsFriendly(architecture2.BelongedFaction))
                                    {
                                        diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, architecture2.BelongedFaction.ID);
                                        if (((diplomaticRelation >= 0) && (GameObject.Random(diplomaticRelation + 200) <= GameObject.Random(50))) || ((diplomaticRelation < 0) && (GameObject.Random(Math.Abs(diplomaticRelation) + 100) >= GameObject.Random(100))))
                                        {
                                            firstHalfPerson = this.GetFirstHalfPerson("GossipAbility");
                                            if (firstHalfPerson != null && !firstHalfPerson.HasLeadingArmy &&
                                                firstHalfPerson.NonFightingNumber > firstHalfPerson.FightingNumber &&
                                                firstHalfPerson.FightingNumber < 350 && firstHalfPerson != this.BelongedFaction.Leader &&
                                                (firstHalfPerson != firstHalfPerson.BelongedFaction.Leader || firstHalfPerson.ImmunityOfCaptive) &&
                                                GameObject.Random(architecture2.GetGossipablePersonCount() + 4) >= 4
                                                && GameObject.Random(firstHalfPerson.GossipAbility) >= 200
                                                && GameObject.Chance(100 - architecture2.captureChance * (firstHalfPerson.Sex != architecture2.BelongedFaction.Leader.Sex ? 2 : 1)))
                                            {
                                                firstHalfPerson.GoForGossip(Session.Current.Scenario.GetClosestPoint(architecture2.ArchitectureArea, this.Position));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if ((this.HasPerson() && (GameObject.Random(this.Fund) >= this.ConvincePersonFund)) && GameObject.Chance(50) && this.BelongedSection.AIDetail.AllowPersonTactics)
                        {
                            ArchitectureList list4 = new ArchitectureList();
                            foreach (Architecture architecture in knownArch)
                            {
                                if (((architecture.BelongedFaction != this.BelongedFaction) && (architecture.BelongedFaction != null)) && architecture.HasPerson())
                                {
                                    list4.Add(architecture);
                                }
                            }
                            foreach (Architecture architecture in this.BelongedFaction.Architectures)
                            {
                                if (architecture.HasCaptive())
                                {
                                    list4.Add(architecture);
                                }
                            }
                            if (list4.Count > 0)
                            {
                                architecture2 = list4[GameObject.Random(list4.Count)] as Architecture;
                                if (architecture2.BelongedFaction == this.BelongedFaction)
                                {
                                    ConvinceCaptivesAI(architecture2);
                                }
                                else if (!this.IsFriendly(architecture2.BelongedFaction) || GameObject.Chance(50))
                                {
                                    diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, architecture2.BelongedFaction.ID);
                                    if (((diplomaticRelation >= 0) && (GameObject.Random(diplomaticRelation + 50) <= GameObject.Random(50))) || (diplomaticRelation < 0))
                                    {
                                        Person extremeLoyaltyPerson = architecture2.GetLowestLoyaltyPersonRecruitable();
                                        if ((extremeLoyaltyPerson != null) && ((extremeLoyaltyPerson.Loyalty < 100) && (extremeLoyaltyPerson.BelongedFaction != null)) && (extremeLoyaltyPerson != extremeLoyaltyPerson.BelongedFaction.Leader))
                                        {
                                            firstHalfPerson = this.GetFirstHalfPerson("ConvinceAbility");
                                            if (firstHalfPerson != null)
                                            {
                                                foreach (Person p in architecture2.PersonsExcludeNvGuan)
                                                {
                                                    if (firstHalfPerson.CanConvinceChance(p) > 20)
                                                    {
                                                        if (firstHalfPerson != null && !firstHalfPerson.HasLeadingArmy &&
                                                            firstHalfPerson.NonFightingNumber > firstHalfPerson.FightingNumber &&
                                                             firstHalfPerson.FightingNumber < 350 && firstHalfPerson != this.BelongedFaction.Leader &&
                                                            (firstHalfPerson != firstHalfPerson.BelongedFaction.Leader || firstHalfPerson.ImmunityOfCaptive)
                                                             && GameObject.Chance(100 - architecture2.captureChance * (firstHalfPerson.Sex != architecture2.BelongedFaction.Leader.Sex ? 2 : 1)))
                                                        {
                                                            firstHalfPerson.OutsideDestination = this.ArchitectureArea.Centre;
                                                            firstHalfPerson.GoForConvince(p);
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
                        if ((this.HasPerson() && (GameObject.Random(this.Fund) >= this.JailBreakArchitectureFund)) && GameObject.Chance(50) && this.JailBreakAvail() && this.BelongedSection.AIDetail.AllowPersonTactics)
                        {
                            List<Architecture> a = new List<Architecture>();
                            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
                            {
                                if (architecture.HasFactionCaptive(this.BelongedFaction) && knownArch.HasGameObject(architecture))
                                {
                                    a.Add(architecture);
                                }
                            }
                            if (a.Count > 0)
                            {
                                Architecture target = a[GameObject.Random(a.Count)] as Architecture;
                                if (GameObject.Chance(100 - target.noEscapeChance * 2))
                                {
                                    int totalCaptiveValue = 0;
                                    foreach (Captive c in target.Captives)
                                    {
                                        if (c.CaptiveFaction == this.BelongedFaction)
                                        {
                                            totalCaptiveValue += c.AIWantsTheCaptive;
                                        }
                                    }
                                    if (GameObject.Random(totalCaptiveValue) > GameObject.Random(100000))
                                    {
                                        firstHalfPerson = this.GetFirstHalfPerson("JailBreakAbility");
                                        if (firstHalfPerson != null && !firstHalfPerson.HasLeadingArmy &&
                                                firstHalfPerson.NonFightingNumber > firstHalfPerson.FightingNumber &&
                                                 firstHalfPerson.FightingNumber < 350 && firstHalfPerson != this.BelongedFaction.Leader &&
                                                (firstHalfPerson != firstHalfPerson.BelongedFaction.Leader || firstHalfPerson.ImmunityOfCaptive)
                                                 && GameObject.Chance(100 - target.captureChance * (firstHalfPerson.Sex != target.BelongedFaction.Leader.Sex ? 2 : 1)))
                                        {
                                            firstHalfPerson.GoForJailBreak(Session.Current.Scenario.GetClosestPoint(target.ArchitectureArea, this.Position));
                                        }
                                    }
                                }
                            }
                        }
                        if (this.HasPerson() && GameObject.Chance(50) && this.AssassinateAvail() && this.BelongedSection.AIDetail.AllowPersonTactics)
                        {
                            if (knownArch.Count > 0)
                            {
                                Architecture target = (Architecture)knownArch[GameObject.Random(knownArch.Count)];
                                if (target.BelongedFaction != null || this.BelongedFaction.IsAlien)
                                {
                                    if (target.BelongedFaction != null && target.BelongedFaction != this.BelongedFaction)
                                    {
                                        diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, target.BelongedFaction.ID);
                                    }
                                    else
                                    {
                                        diplomaticRelation = -1000;
                                    }
                                    if (((diplomaticRelation >= 0) && (GameObject.Random(diplomaticRelation + 400) <= GameObject.Random(50))) || (diplomaticRelation < 0))
                                    {
                                        firstHalfPerson = this.GetFirstHalfPerson("AssassinateAbility");
                                        if (firstHalfPerson != null && firstHalfPerson.PersonalLoyalty < 4)
                                        {
                                            foreach (Person p in target.GetAssassinatePersonTarget(this.BelongedFaction))
                                            {
                                                int targetDef;
                                                if (p.Status == PersonStatus.Normal)
                                                {
                                                    targetDef = target.DefendAssassinateAbility;
                                                }
                                                else
                                                {
                                                    targetDef = p.AssassinateAbility;
                                                }
                                                if (firstHalfPerson.CanConvinceChance(p) <= 20 && firstHalfPerson.AssassinateAbility > targetDef)
                                                {
                                                    if (target.BelongedFaction == this.BelongedFaction)
                                                    {
                                                        firstHalfPerson.OutsideDestination = this.ArchitectureArea.Centre;
                                                        firstHalfPerson.GoForAssassinate(p);
                                                        break;
                                                    }
                                                    else if (firstHalfPerson != null &&
                                                             !firstHalfPerson.HasLeadingArmy &&
                                                             p.BelongedFaction == null &&
                                                             firstHalfPerson.CanConvinceChance(p) <= 10 &&
                                                             p.Sex != firstHalfPerson.BelongedFaction.Leader.Sex)
                                                    {
                                                        firstHalfPerson.OutsideDestination = target.ArchitectureArea.Centre;
                                                        firstHalfPerson.GoForAssassinate(p);
                                                        break;
                                                    }
                                                    else if (firstHalfPerson != null && !firstHalfPerson.HasLeadingArmy &&
                                                           firstHalfPerson.NonFightingNumber > firstHalfPerson.FightingNumber &&
                                                            firstHalfPerson.FightingNumber < 350 && firstHalfPerson != this.BelongedFaction.Leader &&
                                                            firstHalfPerson.AssassinateAbility > targetDef * 2 &&
                                                           (firstHalfPerson != firstHalfPerson.BelongedFaction.Leader || firstHalfPerson.ImmunityOfCaptive)
                                                            && GameObject.Chance(100 - target.captureChance * (target.BelongedFaction != null && firstHalfPerson.Sex != target.BelongedFaction.Leader.Sex ? 2 : 1)))
                                                    {
                                                        firstHalfPerson.OutsideDestination = target.ArchitectureArea.Centre;
                                                        firstHalfPerson.GoForAssassinate(p);
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
            }

            InformationList toRemove = new InformationList();
            int dayCost = this.InformationDayCost;
            foreach (Information i in this.Informations)
            {
                bool stop = true;
                if (i.DaysStarted <= 3 * Session.Parameters.DayInTurn)
                {
                    stop = false;
                }
                else if (i.DaysStarted < GameObject.Random(10) + 30 && this.IsFundIncomeEnough && this.IsFundEnough
                    && dayCost < 500)
                {
                    foreach (Point p in i.Area.Area)
                    {
                        Architecture a = Session.Current.Scenario.GetArchitectureByPosition(p);
                        if (a != null && !this.IsFriendly(a.BelongedFaction))
                        {
                            stop = false;
                            break;
                        }
                    }
                }

                if (stop)
                {
                    bool hasEnemy = false;
                    bool hasOwn = false;
                    foreach (Point p in i.Area.Area)
                    {
                        Troop t = Session.Current.Scenario.GetTroopByPosition(p);
                        if (t != null && !this.IsFriendly(t.BelongedFaction))
                        {
                            hasEnemy = true;
                        }
                        if (t != null && t.BelongedFaction == this.BelongedFaction)
                        {
                            hasOwn = true;
                        }
                        if (hasEnemy && hasOwn)
                        {
                            stop = false;
                            break;
                        }
                    }
                }

                if (stop && this.PlanArchitecture != null)
                {
                    foreach (Point p in i.Area.Area)
                    {
                        Architecture a = Session.Current.Scenario.GetArchitectureByPosition(p);
                        if (a == this.PlanArchitecture)
                        {
                            stop = false;
                            break;
                        }
                    }
                }

                if (!stop)
                {
                    foreach (Information j in this.Informations)
                    {
                        if (i == j) continue;
                        if (toRemove.HasGameObject(i)) continue;
                        if (j.Position == i.Position && j.Radius >= i.Radius && j.DayCost < i.DayCost)
                        {
                            stop = true;
                            break;
                        }
                    }
                }

                if (stop)
                {
                    toRemove.Add(i);
                    dayCost -= i.DayCost;
                }
            }
            foreach (Information i in toRemove)
            {
                i.Purify();
                this.RemoveInformation(i);
                Session.Current.Scenario.Informations.Remove(i);
            }
        }

        public bool IsArchitectureHostileWithoutTruce(Architecture a)
        {
            if (this.IsArchitectureHostile(a))
            {
                DiplomaticRelation rel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(this.BelongedFaction.ID, a.BelongedFaction.ID);
                if (rel.Truce > 60)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool IsArchitectureHostile(Architecture a)
        {
            if (a.BelongedFaction == null || this.BelongedFaction == null) return false;
            int n = (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, a.BelongedFaction.ID) + a.BelongedFaction.ArchitectureTotalSize) - this.BelongedFaction.ArchitectureTotalSize;
            return n < 0;
        }

        public bool IsArchitectureCriticalHostile(Architecture a)
        {
            if (a.BelongedFaction == null || this.BelongedFaction == null) return false;
            int n = (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, a.BelongedFaction.ID) + a.BelongedFaction.ArchitectureTotalSize) - this.BelongedFaction.ArchitectureTotalSize;
            return n < -200;
        }

        public int HostileScale
        {
            get
            {
                int result = 0;
                foreach (LinkNode i in this.AIAllLinkNodes.Values)
                {
                    if (this.IsArchitectureHostile(i.A) && i.Level <= 1)
                    {
                        result += i.A.ArmyScale;
                    }
                }
                return result * 2;
            }
        }

        public int OrientationScale
        {
            get
            {
                int result = 0;
                
                // 添加空值检查防止NullReferenceException
                if (this.BelongedSection?.OrientationFaction != null && this.AIAllLinkNodes != null)
                {
                    foreach (LinkNode i in this.AIAllLinkNodes.Values)
                    {
                        if (i?.A?.BelongedFaction != null && 
                            this.BelongedSection.OrientationFaction == i.A.BelongedFaction && 
                            i.Level <= 1)
                        {
                            result += i.A.ArmyScale;
                        }
                    }
                }
                
                return result * 2;
            }
        }

        private PersonList GetFirstHalfPersonList(string propertyName)
        {
            PersonList list = new PersonList();
            if (this.Persons.Count > 0)
            {
                foreach (Person p in this.Persons)
                {
                    list.Add(p);
                }
                if (list.Count > 1)
                {
                    list.PropertyName = propertyName;
                    list.IsNumber = true;
                    list.ReSort();
                } else
                {
                    return list;
                }
                list.GameObjects.RemoveRange(list.Count / 2, list.Count - list.Count / 2);
                return (list);
            }
            return new PersonList();
        }

        private void AIAutoSearch()
        {
            if (this.HasHostileTroopsInView()) return;
            foreach (Person person in this.PersonsExcludeNvGuan.GetList())
            {
                if (person.WorkKind == ArchitectureWorkKind.无 && person.Tiredness <= 0)
                {
                    person.GoForSearch();
                }
            }
        }
        /*
        private void AIAutoZhaoXian()
        {
            if (Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {


                    if (GameObject.Chance(10)  && !this.HasEnoughPeople && this.AutoCreatePersonAvail())
                    {
                        this.AutoCreatePerson();
                    }

            }
        }*/

        public void AICampaign()
        {
            this.DefensiveCampaign(null);
            this.OffensiveCampaign();
        }

        private Point? GetRandomStartingPosition(Troop troop)
        {
            GameArea allAvailableArea = this.GetAllAvailableArea(false);
            GameArea sourceArea = new GameArea();
            foreach (Point point in allAvailableArea.Area)
            {
                if (((Session.Current.Scenario.GetArchitectureByPosition(point) == this) && (Session.Current.Scenario.GetTroopByPosition(point) == null)) || troop.IsMovableOnPosition(point))
                {
                    sourceArea.Area.Add(point);
                }
            }
            if (sourceArea.Count == 0)
            {
                return null;
            }
            return sourceArea[GameObject.Random(sourceArea.Count)];
        }

        public Point? GetRandomStartingPosition(Military m)
        {
            GameArea allAvailableArea = this.GetAllAvailableArea(false);
            m.ModifyAreaByTerrainAdaptablity(allAvailableArea);
            if (allAvailableArea.Count == 0)
            {
                return null;
            }
            return allAvailableArea[GameObject.Random(allAvailableArea.Count)];
        }

        /*
        public Troop BuildTransportTroop(Architecture destination, Military military, int food, int fund)
        {
            Troop troop;
            int min = int.MaxValue;
            PersonList leader = new PersonList();
            foreach (Person p in this.Persons)
            {
                if (p.Merit < min && p.Tiredness < 30 && p.InjureRate >= 1 && p.Loyalty > 80 &&
                    (this.HasHostileTroopsInView() || isPersonAllowedIntoTroop(p, military, false)))
                {
                    leader.Clear();
                    leader.Add(p);
                    min = p.Merit;
                }
            }
            if (leader.Count <= 0) return null;
            troop = Troop.CreateSimulateTroop(leader, military, this.Position);
            Point? nullable = this.GetRandomStartingPosition(troop);
            if (!nullable.HasValue)
            {
                return null;
            }
            troop.Destroy(true, false);
            troop = this.CreateTroop(leader, leader[0] as Person, military, food, nullable.Value);
            troop.WillArchitecture = destination;
            troop.zijin = fund;
            this.DecreaseFund(fund);  // 使用安全的减少方法，防止负数
            Legion legion = this.BelongedFaction.GetLegion(destination);
            if (legion == null)
            {
                legion = this.CreateOffensiveLegion(destination);
            }
            legion.AddTroop(troop);
            this.PostCreateTroop(troop, false);
            return troop;
        }
        */

        private void AIMilitary()
        {
            foreach (Military military in this.GetLevelUpMilitaryList())
            {
                List<MilitaryKind> candidates = military.Kind.GetLevelUpKinds(this);
                Dictionary<MilitaryKind, float> upgradable = new Dictionary<MilitaryKind, float>();
                foreach (MilitaryKind mk in candidates)
                {
                    if (military.Kind.LevelUpAvail(this))
                    {
                        float weight = 1;
                        foreach (KeyValuePair<Condition, float> c in military.Kind.AIUpgradeArchitectureConditionWeight)
                        {
                            if (c.Key.CheckCondition(this))
                            {
                                weight *= c.Value;
                            }
                        }
                        foreach (KeyValuePair<Condition, float> c in military.Kind.AIUpgradeLeaderConditionWeight)
                        {
                            if (c.Key.CheckCondition(military.Leader))
                            {
                                weight *= c.Value;
                            }
                        }
                        weight *= military.Kind.Merit;
                        upgradable.Add(mk, weight);
                    }
                }

                if (upgradable.Count > 0)
                {
                    this.LevelUpMilitary(military, GameObject.WeightedRandom(upgradable));
                }
            }

            bool merged = false;

            MilitaryList merger = this.GetMergeMilitaryList();
            merger.SmallToBig = false;
            merger.PropertyName = "LeaderFightingForce";
            merger.IsNumber = true;
            merger.ReSort();
            foreach (Military i in merger)
            {
                MilitaryList list = this.GetBeMergedMilitaryList(i);
                list.SmallToBig = true;
                list.PropertyName = "LeaderFightingForce";
                list.IsNumber = true;
                list.ReSort();
                foreach (Military j in list)
                {
                    if (i.Quantity + j.Quantity <= i.Kind.MaxScale)
                    {
                        int increment = j.Quantity + i.Quantity - i.Kind.MaxScale;
                        if (increment > 0)
                        {
                            this.IncreasePopulation(increment);
                            this.IncreaseMilitaryPopulation(increment);
                        }
                        if (j.LeaderID == i.LeaderID)
                        {
                            i.IncreaseQuantity(j.Quantity, j.Morale, j.Combativity, j.Experience, j.LeaderExperience);
                        }
                        else
                        {
                            i.IncreaseQuantity(j.Quantity, j.Morale, j.Combativity, j.Experience, 0);
                        }
                        this.RemoveMilitary(j);
                        this.BelongedFaction.RemoveMilitary(j);
                        Session.Current.Scenario.Militaries.Remove(j);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }


        }



        public void  TransferMilitary(Military military, Architecture destination) // 运兵无需武将来运
        {
            if (this.MilitaryCount == 0) return ;
            MilitaryList list = new MilitaryList();
            if ((military.Scales > 5) && (military.Morale >= 80) && (military.Combativity >= 80) && (military.InjuryQuantity < military.Kind.MinScale)
                && !military.IsFewScaleNeedRetreat && military.Kind.Movable && military.Kind.Type != MilitaryType.水军)
            {
                list.Add(military);
            }

            PersonList pl = new PersonList();
            Military military2 = military;
            if ((military2.FollowedLeader != null) && this.PersonsExcludeNvGuan.HasGameObject(military2.FollowedLeader) &&
                    military2.FollowedLeader.WaitForFeiZi == null && military2.FollowedLeader.LocationTroop == null
                    && !this.BelongedFaction.MayorList.HasGameObject(military2.FollowedLeader))

            {
                    pl.Add(military2.FollowedLeader);
            }

             if ((((military2.Leader != null) && (military2.LeaderExperience >= 10)) && (((military2.Leader.Strength >= 80) || (military2.Leader.Command >= 80)) || military2.Leader.HasLeaderValidTitle))
                    && this.PersonsExcludeNvGuan.HasGameObject(military2.Leader) && military2.Leader.WaitForFeiZi == null && military2.Leader.LocationTroop == null
                    && !this.BelongedFaction.MayorList.GameObjects.Contains(military2.Leader))

             {

                    pl.Add(military2.Leader);
             }

             if (!this.IsSurrounded() && !destination.IsSurrounded ())
             {
                 if (list.Count > 0)
                 {
                     double distance = (double)Session.Current.Scenario.GetDistance(this.ArchitectureArea, destination.ArchitectureArea);
                     foreach (Military m in list)
                     {
                         int fundCost = (int) (military.TransferFundCost(distance) * (1 - this.TroopTransportFundRate));
                         int foodCost = (int) (military.TransferFoodCost(distance) * (1 - this.TroopTransportFoodRate));
                         if (this.Fund >= fundCost &&
                            this.Food >= foodCost)
                         {
                             this.DecreaseFund(military.TransferFundCost(distance));
                             this.DecreaseFood(military.TransferFoodCost(distance));
                             m.StartingArchitecture = this;
                             m.TargetArchitecture = destination;
                             m.ArrivingDays = Math.Max(1, (int) (m.TransferDays(distance) * (1 - this.TroopTransportDayRate)));
                             this.RemoveMilitary(m);
                             this.BelongedFaction.TransferingMilitaries.Add(m);
                             this.BelongedFaction.TransferingMilitaryCount++;
                         }
                     }

                 }

                 if (pl.Count > 0)
                 {
                     foreach (Person p in pl)
                     {
                         p.MoveToArchitecture(destination);
                     }
                 }
             }
          }

                   
        private void AIRecruitment(bool water, bool siege)
        {
            // 🔥 招募是基础策略，委任军区可以执行
            if (!ShouldExecuteBasicAI()) return;
            
            // 🔥 额外检查：如果是委任军区但不允许新建军队，则跳过
            if (Session.Current.Scenario.IsPlayer(this.BelongedFaction) && 
                this.BelongedSection != null && 
                this.BelongedSection.AIDetail != null && 
                this.BelongedSection.AIDetail.AutoRun && 
                !this.BelongedSection.AIDetail.AllowNewMilitary) 
            {
                return;
            }

            if (this.Population > 0 && (this.IsFundEnough || this.HasHostileTroopsInView()))
            {
                MilitaryKind current;
                Dictionary<int, MilitaryKind>.ValueCollection.Enumerator enumerator;

                Dictionary<MilitaryKind, float> list = new Dictionary<MilitaryKind, float>();
                MilitaryKindList list2 = new MilitaryKindList();
                Dictionary<MilitaryKind, float> allMilitaries = new Dictionary<MilitaryKind, float>();
                MilitaryKindList allMilitaries2 = new MilitaryKindList();

                using (enumerator = this.BelongedFaction.AvailableMilitaryKinds.MilitaryKinds.Values.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        current = enumerator.Current;
                        if (current.IsTransport) continue;
                        if (current.Type == MilitaryType.水军 && this.AIWaterLinks.Count == 0) continue;
                        if (current.Type != MilitaryType.水军 && this.AILandLinks.Count == 0) continue;

                        float weight = CalculateFinalWeight(current, false);

                        if (((water && current.Type == MilitaryType.水军) || (!water && current.Type != MilitaryType.水军))
                            && ((siege && current.Type == MilitaryType.器械) || (!siege && current.Type != MilitaryType.器械))
                            && current.CreateAvail(this))
                        {
                            list2.Add(current);
                            list.Add(current, weight);
                        }
                        if (current.CreateAvail(this))
                        {
                            allMilitaries2.Add(current);
                            allMilitaries.Add(current, weight);
                        }
                    }
                }
                using (enumerator = this.PrivateMilitaryKinds.MilitaryKinds.Values.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        current = enumerator.Current;
                        if (current.IsTransport) continue;
                        if (current.Type == MilitaryType.水军 && this.AIWaterLinks.Count == 0) continue;
                        if (current.Type != MilitaryType.水军 && this.AILandLinks.Count == 0) continue;

                        float weight = CalculateFinalWeight(current, true);

                        if (((water && current.Type == MilitaryType.水军) || (!water && current.Type != MilitaryType.水军))
                            && ((siege && current.Type == MilitaryType.器械) || (!siege && current.Type != MilitaryType.器械))
                            && current.CreateAvail(this))
                        {
                            list2.Add(current);
                            if (list.ContainsKey(current))
                            {
                                list[current] *= weight;
                            }
                            else
                            {
                                list.Add(current, weight);
                            }
                        }
                        if (current.CreateAvail(this))
                        {
                            allMilitaries2.Add(current);
                            if (allMilitaries.ContainsKey(current))
                            {
                                allMilitaries[current] *= weight;
                            }
                            else
                            {
                                allMilitaries.Add(current, weight);
                            }
                        }
                    }
                }
                if (list.Count > 0)
                {
                    current = GameObject.WeightedRandom(list);
                    this.CreateMilitary(current.findSuccessorCreatable(list2, this));
                }
                else if (allMilitaries.Count > 0)
                {
                    current = GameObject.WeightedRandom(allMilitaries);
                    this.CreateMilitary(current.findSuccessorCreatable(allMilitaries2, this));
                }
            }
        }

        /// <summary>
        /// 计算最终权重（整合了原有逻辑、兵种上限动态权重、武将加成、地形修正）
        /// </summary>
        private float CalculateFinalWeight(MilitaryKind kind, bool isPrivate)
        {
            float w = 1.0f;

            // 1. 基础条件权重 (原有逻辑)
            foreach (KeyValuePair<Condition, float> c in kind.AICreateArchitectureConditionWeight)
            {
                if (c.Key.CheckCondition(this))
                {
                    w *= c.Value;
                }
            }

            // 2. 私有兵种品质加成 (原有逻辑)
            if (isPrivate)
            {
                w *= kind.Merit;
            }

            // 🔥 3. 兵种上限动态权重系统（通用版本，替代硬编码的贼兵限制）
            // 标准正规军上限为10000，超过此值的大编制兵种需要特殊处理
            const int STANDARD_TROOP_CAP = 10000;
            
            if (kind.MaxScale > STANDARD_TROOP_CAP)
            {
                // 计算超额倍率
                float excessRatio = (float)kind.MaxScale / STANDARD_TROOP_CAP;
                float baseDecay = 1.0f / excessRatio;
                
                // 统计城内"有效战备部队"数量
                // 条件1：是正规军（上限 <= 10000，成型快）
                // 条件2：有实际战力（兵力 >= 上限的80%）
                int validCombatReadyUnits = 0;
                foreach (Military m in this.Militaries)
                {
                    bool isQuickType = m.Kind.MaxScale <= STANDARD_TROOP_CAP;
                    bool isCombatReady = m.Quantity >= (m.Kind.MaxScale * 0.8f);
                    
                    if (isQuickType && isCombatReady)
                    {
                        validCombatReadyUnits++;
                    }
                }
                
                // 根据有效战备部队数量调整权重
                if (validCombatReadyUnits < 3)
                {
                    // 战力不足时，极度抑制大编制兵种
                    // 使用平方衰减 + 额外惩罚，确保几乎不会招募
                    w *= baseDecay * baseDecay * 0.1f;
                    
                    System.Diagnostics.Debug.WriteLine($"  🚫 {kind.Name}: 上限={kind.MaxScale}, 有效战备部队={validCombatReadyUnits}/3, 权重衰减={baseDecay * baseDecay * 0.1f:F4}");
                }
                else
                {
                    // 战力充足时，允许以较低概率招募大编制兵种
                    w *= baseDecay;
                    
                    System.Diagnostics.Debug.WriteLine($"  ✓ {kind.Name}: 上限={kind.MaxScale}, 有效战备部队={validCombatReadyUnits}/3, 权重衰减={baseDecay:F4}");
                }
            }

            // 4. 骑兵地形与全局修正
            if (kind.Type == MilitaryType.骑兵)
            {
                // 骑兵基础优势：机动高、攻击高，给予 1.3 倍基础偏好
                w *= 1.3f;
                // 地形修正：如果周围全是山地森林，大幅降低权重
                w *= this.CalculateCavalryTerrainFactor();
            }

            // 5. 武将适性加成 (技能/称号)
            // 如果城内有很多擅长该兵种的武将，权重提升
            w *= this.CalculateOfficerRelevance(kind);

            return w;
        }

        /// <summary>
        /// 计算城内武将对特定兵种的适配程度
        /// </summary>
        private float CalculateOfficerRelevance(MilitaryKind kind)
        {
            if (this.Persons.Count == 0) return 1.0f;

            float relevanceScore = 0;
            int relevantOfficers = 0;

            foreach (Person p in this.Persons)
            {
                bool isRelevant = false;

                // 1. 检查技能
                foreach (Skill s in p.Skills.GetSkillList())
                {
                    // 检查技能的影响是否与兵种类型相关
                    if (IsSkillRelevantToMilitaryType(s, kind.Type))
                    {
                        relevanceScore += 1.0f;
                        isRelevant = true;
                    }
                }

                // 2. 检查称号（遍历武将的所有称号）
                if (p.RealTitles != null && p.RealTitles.Count > 0)
                {
                    foreach (Title title in p.RealTitles)
                    {
                        if (IsTitleRelevantToMilitaryType(title, kind.Type))
                        {
                            relevanceScore += 1.5f;
                            isRelevant = true;
                            break; // 找到一个相关称号即可
                        }
                    }
                }

                if (isRelevant)
                {
                    relevantOfficers++;
                }
            }

            // 计算加成倍率
            // 如果有一半的武将都适合带这个兵，权重最高可达 1.5 倍
            // 公式可自行调整
            float ratio = (float)relevantOfficers / this.Persons.Count;
            return 1.0f + (ratio * 0.5f) + (relevanceScore * 0.05f);
        }

        /// <summary>
        /// 简单的技能匹配判断 (通过技能影响系统判断)
        /// </summary>
        private bool IsSkillRelevantToMilitaryType(Skill skill, MilitaryType type)
        {
            // 检查技能的MilitaryTypeOnly属性
            if (skill.MilitaryTypeOnly == type)
            {
                return true;
            }

            // 检查技能影响中是否包含对应兵种的加成
            foreach (Influence influence in skill.Influences.Influences.Values)
            {
                // 检查影响类型是否与兵种相关
                // 根据CommonData中的影响ID判断
                // 例如：ID 290 是兵种限定影响
                if (influence.Kind.ID == 290 && influence.Parameter == type.ToString())
                {
                    return true;
                }

                // 检查其他与兵种相关的影响
                // 步兵相关影响 (示例ID，需根据实际配置调整)
                if (type == MilitaryType.步兵 && (
                    influence.Kind.ID == 800 || // 步兵攻击力
                    influence.Kind.ID == 801))  // 步兵防御力
                {
                    return true;
                }

                // 骑兵相关影响
                if (type == MilitaryType.骑兵 && (
                    influence.Kind.ID == 802 || // 骑兵攻击力
                    influence.Kind.ID == 803))  // 骑兵防御力
                {
                    return true;
                }

                // 弩兵相关影响
                if (type == MilitaryType.弩兵 && (
                    influence.Kind.ID == 804 || // 弩兵攻击力
                    influence.Kind.ID == 805))  // 弩兵防御力
                {
                    return true;
                }

                // 水军相关影响
                if (type == MilitaryType.水军 && (
                    influence.Kind.ID == 824 || // 水军攻击力
                    influence.Kind.ID == 825))  // 水军防御力
                {
                    return true;
                }

                // 器械相关影响
                if (type == MilitaryType.器械 && (
                    influence.Kind.ID == 832 || // 器械攻击力
                    influence.Kind.ID == 833))  // 器械防御力
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查称号是否与兵种类型相关
        /// </summary>
        private bool IsTitleRelevantToMilitaryType(Title title, MilitaryType type)
        {
            if (title == null) return false;

            // 检查称号的影响
            foreach (Influence influence in title.Influences.Influences.Values)
            {
                // 使用与技能相同的逻辑
                if (influence.Kind.ID == 290 && influence.Parameter == type.ToString())
                {
                    return true;
                }

                // 检查其他与兵种相关的影响
                if (type == MilitaryType.步兵 && (influence.Kind.ID == 800 || influence.Kind.ID == 801))
                {
                    return true;
                }
                if (type == MilitaryType.骑兵 && (influence.Kind.ID == 802 || influence.Kind.ID == 803))
                {
                    return true;
                }
                if (type == MilitaryType.弩兵 && (influence.Kind.ID == 804 || influence.Kind.ID == 805))
                {
                    return true;
                }
                if (type == MilitaryType.水军 && (influence.Kind.ID == 824 || influence.Kind.ID == 825))
                {
                    return true;
                }
                if (type == MilitaryType.器械 && (influence.Kind.ID == 832 || influence.Kind.ID == 833))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算骑兵的地形适应因子
        /// </summary>
        private float CalculateCavalryTerrainFactor()
        {
            try
            {
                var scenario = Session.Current.Scenario;
                int badTerrainCount = 0;
                int totalCount = 0;

                // 扫描城市周围 3 格范围 (7x7 区域)
                int range = 3;
                Point archPos = this.Position;

                for (int x = -range; x <= range; x++)
                {
                    for (int y = -range; y <= range; y++)
                    {
                        Point targetPoint = new Point(archPos.X + x, archPos.Y + y);

                        // 检查是否越界
                        if (scenario.PositionOutOfRange(targetPoint))
                        {
                            continue;
                        }

                        // 获取地形类型
                        TerrainKind terrain = scenario.GetTerrainKindByPosition(targetPoint);

                        // 判断是否为骑兵不利地形
                        if (terrain == TerrainKind.森林 ||   // 森林
                            terrain == TerrainKind.山地 ||   // 山地
                            terrain == TerrainKind.水域 ||   // 水域
                            terrain == TerrainKind.峻岭 ||   // 峻岭
                            terrain == TerrainKind.栈道 ||   // 栈道
                            terrain == TerrainKind.湿地)     // 湿地
                        {
                            badTerrainCount++;
                        }

                        totalCount++;
                    }
                }

                // 如果不利地形超过 50%，骑兵权重打 2 折
                if (totalCount > 0)
                {
                    float badTerrainRatio = (float)badTerrainCount / totalCount;
                    if (badTerrainRatio > 0.5f)
                    {
                        return 0.2f;
                    }
                    else if (badTerrainRatio > 0.3f)
                    {
                        return 0.5f;
                    }
                }

                return 1.0f; // 默认不惩罚
            }
            catch
            {
                // 如果出现任何异常，返回默认值
                return 1.0f;
            }
        }



        public bool CompletelyDeveloped
        {
            get
            {
                return (this.Agriculture >= this.AgricultureCeiling && this.Commerce >= this.CommerceCeiling && this.Technology >= this.TechnologyCeiling &&
                    this.Domination >= this.DominationCeiling && this.Endurance >= this.EnduranceCeiling && this.Morale >= this.MoraleCeiling);
            }
        }



        private void AITrade()
        {
            if ((Session.Current.Scenario.Date.Day % Session.Parameters.AITradePeriod) <= Session.Current.Scenario.Parameters.DayInTurn)
            {
                int num;
                if (this.SellFoodAvail())
                {
                    if (!this.IsFundEnough && this.Food > this.EnoughFood * 2)
                    {
                        num = this.Food - this.EnoughFood * 2;
                        if (num > 0)
                        {
                            this.SellFood(num);
                        }
                    }
                    else if (!this.IsFundAbundant && this.Food > this.AbundantFood * 2)
                    {
                        num = this.Food - this.AbundantFood * 2;
                        if (num > 0)
                        {
                            this.SellFood(num);
                        }
                    }
                    else if (this.Fund < this.FundCeiling / 2 && this.Food >= FoodCeiling / 2)
                    {
                        num = this.Food - this.FoodCeiling / 2;
                        if (num > 0)
                        {
                            this.SellFood(num);
                        }
                    }
                }
                else if (this.BuyFoodAvail())
                {
                    if (this.Fund > this.EnoughFund * 2 && !this.IsFoodEnough)
                    {
                        num = this.Fund - this.EnoughFund * 2;
                        if (num > 0)
                        {
                            this.BuyFood(num);
                        }
                    }
                    else if (this.Fund > this.AbundantFund * 2 && !this.IsFoodAbundant)
                    {
                        num = this.Fund - this.AbundantFund * 2;
                        if (num > 0)
                        {
                            this.BuyFood(num);
                        }
                    }
                    else if (this.Fund >= this.FundCeiling / 2 && this.Food < FoodCeiling / 2)
                    {
                        num = this.Fund - this.FundCeiling / 2;
                        if (num > 0)
                        {
                            this.BuyFood(num);
                        }
                    }
                    /*if ((((this.PlanArchitecture == null) && (this.PlanFacilityKind == null)) && (this.BelongedFaction.PlanTechniqueArchitecture != this)) && (((this.Fund >= (this.FundCeiling / 2)) || !this.IsFoodEnough) && this.IsFundAbundant))
                    {
                        num = this.Fund - this.AbundantFund;
                        if (num > 0)
                        {
                            this.BuyFood(num / 2);
                        }
                    }*/
                }
                
                /*else if ((this.SellFoodAvail() && ((this.PlanArchitecture == null) && (this.TransferFoodArchitecture == null))) && (((!this.HostileLine && (this.Fund < (this.FundCeiling / 2))) && !this.IsFoodEnough) && this.IsFoodAbundant))
                {
                    num = this.Food - this.AbundantFood;
                    if (num > 0)
                    {
                        this.SellFood(num / 10);
                    }
                }*/
            }
        }

        private void AITreasure()
        {
            // 宝物管理是高级策略，只有AI势力执行
            if (!ShouldExecuteAdvancedAI()) return;
            
            if (GameObject.Chance(Session.Parameters.AITreasureChance))
            {
                /*if (this.HasTreasureToConfiscate())
                {
                    foreach (Person person in this.Persons.GetList())
                    {
                        if (((person != this.BelongedFaction.Leader) && (person.TreasureCount > 0)) && ((person.TreasureCount > Session.Parameters.AITreasureCountMax) ||
                            ((((person.PersonalTitle == null) && GameObject.Chance(50)) || (((person.PersonalTitle != null) && (person.PersonalTitle.Level * Session.Parameters.AITreasureCountCappedTitleLevelMultiply + Session.Parameters.AITreasureCountCappedTitleLevelAdd <= person.TreasureCount)) && GameObject.Chance(25)))
                            && ((person.CombatTitle == null) || (((person.CombatTitle != null) && (person.CombatTitle.Level * Session.Parameters.AITreasureCountCappedTitleLevelMultiply + Session.Parameters.AITreasureCountCappedTitleLevelAdd <= person.TreasureCount)) && GameObject.Chance(50))))))
                        {
                            foreach (Treasure treasure in person.Treasures.GetRandomList())
                            {
                                person.ConfiscatedTreasure(treasure);
                                this.BelongedFaction.Leader.ReceiveTreasure(treasure);
                                break;
                            }
                        }
                    }
                }*/
                if (((this.BelongedFaction.Leader != null) && (this.BelongedFaction.Leader.TreasureCount > Session.Parameters.AITreasureCountMax)) && this.HasTreasureToAward())
                {
                    GameObjectList list = this.Persons.GetList();
                    list.PropertyName = "FightingForce";
                    list.IsNumber = true;
                    list.ReSort();
                    foreach (Person person in list)
                    {
                        if ((person == this.BelongedFaction.Leader) || (person.TreasureCount != 0))
                        {
                            continue;
                        }
                        if (person.TreasureCount < person.TotalTitleLevel * Session.Parameters.AITreasureCountCappedTitleLevelMultiply + Session.Parameters.AITreasureCountCappedTitleLevelAdd)
                        {
                            foreach (Treasure treasure in this.BelongedFaction.Leader.Treasures.GetRandomList())
                            {
                                if (treasure.Worth < Session.Parameters.AIGiveTreasureMaxWorth)
                                {
                                    this.BelongedFaction.Leader.LoseTreasure(treasure);
                                    person.AwardedTreasure(treasure);
                                    break;
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }
        /*
        private void AIWork()
        {
            this.AIAutoHire();
            this.StopAllWork();
            if (((this.PlanArchitecture == null) || GameObject.Chance(10)) && this.HasPerson())
            {
                int num;
                this.ReSortAllWeighingList();
                bool isFundAbundant = this.IsFundAbundant;
                if (this.Fund < ((100 * this.AreaCount) + ((30 - Session.Current.Scenario.Date.Day) * this.FacilityMaintenanceCost)))
                {
                    MilitaryList trainingMilitaryList = this.GetTrainingMilitaryList();
                    if (trainingMilitaryList.Count > 0)
                    {
                        trainingMilitaryList.IsNumber = true;
                        trainingMilitaryList.PropertyName = "Weighing";
                        trainingMilitaryList.ReSort();
                        GameObjectList maxObjects = this.trainingPersons.GetMaxObjects(trainingMilitaryList.Count);
                        for (num = 0; num < maxObjects.Count; num++)
                        {
                            this.AddPersonToTrainingWork(maxObjects[num] as Person, trainingMilitaryList[num] as Military);
                        }
                    }
                    int num2 = 0;
                    if ((GameObject.Chance(50) && _architectureKind.HasDomination) && (this.Domination < (this.DominationCeiling * 0.8)))
                    {
                        num2++;
                    }
                    if ((GameObject.Chance(50) && _architectureKind.HasEndurance) && (this.Endurance < (this.EnduranceCeiling * 0.2f)))
                    {
                        num2++;
                    }
                    if ((GameObject.Chance(50) && _architectureKind.HasMorale) && (this.Morale < Session.Parameters.RecruitmentMorale))
                    {
                        num2++;
                    }
                    if (num2 > 0)
                    {
                        for (num = 0; num < (this.Persons.Count - trainingMilitaryList.Count); num += num2)
                        {
                            foreach (Person person in this.dominationPersons)
                            {
                                if (person.WorkKind == ArchitectureWorkKind.无)
                                {
                                    this.AddPersonToDominationWorkingList(person);
                                    break;
                                }
                            }
                            foreach (Person person in this.endurancePersons)
                            {
                                if (person.WorkKind == ArchitectureWorkKind.无)
                                {
                                    this.AddPersonToEnduranceWorkingList(person);
                                    break;
                                }
                            }
                            foreach (Person person in this.moralePersons)
                            {
                                if (person.WorkKind == ArchitectureWorkKind.无)
                                {
                                    this.AddPersonToMoraleWorkingList(person);
                                    break;
                                }
                            }
                        }
                    }
                }
                else if ((GameObject.Chance(20) || !this.HasBuildingRouteway) || this.IsFundEnough)
                {
                    float num3;
                    bool flag2 = this.RecentlyAttacked > 0;
                    WorkRateList list3 = new WorkRateList();
                    if ((flag2 || (this.BelongedFaction.PlanTechniqueArchitecture != this)) || GameObject.Chance(20))
                    {
                        if (!flag2 || !GameObject.Chance(80))
                        {
                            if (_architectureKind.HasAgriculture && (this.Agriculture < this.AgricultureCeiling))
                            {
                                if (this.BelongedSection.AIDetail.ValueAgriculture)
                                {
                                    list3.AddWorkRate(new WorkRate((((float) this.Agriculture) / 4f) / ((float) this.AgricultureCeiling), ArchitectureWorkKind.农业));
                                }
                                else
                                {
                                    list3.AddWorkRate(new WorkRate(((float) this.Agriculture) / ((float) this.AgricultureCeiling), ArchitectureWorkKind.农业));
                                }
                            }
                            if (_architectureKind.HasCommerce && (this.Commerce < this.CommerceCeiling))
                            {
                                if (this.BelongedSection.AIDetail.ValueCommerce)
                                {
                                    list3.AddWorkRate(new WorkRate((((float) this.Commerce) / 4f) / ((float) this.CommerceCeiling), ArchitectureWorkKind.商业));
                                }
                                else
                                {
                                    list3.AddWorkRate(new WorkRate(((float) this.Commerce) / ((float) this.CommerceCeiling), ArchitectureWorkKind.商业));
                                }
                            }
                            if (_architectureKind.HasTechnology && (this.Technology < this.TechnologyCeiling))
                            {
                                if (this.BelongedSection.AIDetail.ValueTechnology || (GameObject.Chance(50) && (this.IsStateAdmin || this.IsRegionCore)))
                                {
                                    list3.AddWorkRate(new WorkRate((((float) this.Technology) / 4f) / ((float) this.TechnologyCeiling), ArchitectureWorkKind.技术));
                                }
                                else
                                {
                                    list3.AddWorkRate(new WorkRate(((float) this.Technology) / ((float) this.TechnologyCeiling), ArchitectureWorkKind.技术));
                                }
                            }
                        }
                        if (_architectureKind.HasDomination && (this.Domination < this.DominationCeiling))
                        {
                            if (this.BelongedSection.AIDetail.ValueDomination)
                            {
                                list3.AddWorkRate(new WorkRate(((((float) this.Domination) / 5f) / 4f) / ((float) this.DominationCeiling), ArchitectureWorkKind.统治));
                            }
                            else
                            {
                                list3.AddWorkRate(new WorkRate((((float) this.Domination) / 5f) / ((float) this.DominationCeiling), ArchitectureWorkKind.统治));
                            }
                        }
                        if (_architectureKind.HasMorale && (this.Morale < this.MoraleCeiling))
                        {
                            if (this.BelongedSection.AIDetail.ValueMorale)
                            {
                                list3.AddWorkRate(new WorkRate((((float) this.Morale) / 4f) / ((float) this.MoraleCeiling), ArchitectureWorkKind.民心));
                            }
                            else
                            {
                                list3.AddWorkRate(new WorkRate(((float) this.Morale) / ((float) this.MoraleCeiling), ArchitectureWorkKind.民心));
                            }
                        }
                        if (_architectureKind.HasEndurance && (this.Endurance < this.EnduranceCeiling))
                        {
                            if (this.BelongedSection.AIDetail.ValueEndurance)
                            {
                                list3.AddWorkRate(new WorkRate((((float) this.Endurance) / 4f) / ((float) this.EnduranceCeiling), ArchitectureWorkKind.耐久));
                            }
                            else
                            {
                                list3.AddWorkRate(new WorkRate(((float) this.Endurance) / ((float) this.EnduranceCeiling), ArchitectureWorkKind.耐久));
                            }
                        }
                    }
                    MilitaryList list4 = this.GetTrainingMilitaryList();
                    if (list4.Count > 0)
                    {
                        if (flag2)
                        {
                            list3.AddWorkRate(new WorkRate(0f, ArchitectureWorkKind.训练));
                        }
                        else
                        {
                            num3 = 0f;
                            foreach (Military military in list4)
                            {
                                num3 += ((float) military.TrainingWeighing) / ((float) military.MaxTrainingWeighing);
                            }
                            num3 /= (float) list4.Count;
                            if (this.BelongedSection.AIDetail.ValueTraining)
                            {
                                list3.AddWorkRate(new WorkRate(num3 / 4f, ArchitectureWorkKind.训练));
                            }
                            else
                            {
                                list3.AddWorkRate(new WorkRate(num3, ArchitectureWorkKind.训练));
                            }
                        }
                    }
                    MilitaryList recruitmentMilitaryList = null;
                    if (((flag2 || (this.BelongedFaction.PlanTechniqueArchitecture != this)) && _architectureKind.HasPopulation) && ((flag2 || (GameObject.Random(GameObject.Square(((int) this.BelongedFaction.Leader.StrategyTendency) + 1)) == 0)) && this.RecruitmentAvail()))
                    {
                        recruitmentMilitaryList = this.GetRecruitmentMilitaryList();
                        if ((this.ArmyScale < this.FewArmyScale) && flag2)
                        {
                            list3.AddWorkRate(new WorkRate(0f, ArchitectureWorkKind.补充));
                        }
                        else if (((this.ArmyScale < this.FewArmyScale) && ((this.BelongedSection.AIDetail.ValueRecruitment && GameObject.Chance(20)) || GameObject.Chance(5))) && this.IsFoodAbundant)
                        {
                            list3.AddWorkRate(new WorkRate(0f, ArchitectureWorkKind.补充));
                        }
                        else if ((((GameObject.Chance(1) || (this.BelongedSection.AIDetail.ValueRecruitment && GameObject.Chance(5))) && ((this.ArmyScale >= this.LargeArmyScale) && this.IsFoodAbundant)) || ((((this.ArmyScale < this.LargeArmyScale) && this.IsFoodEnough) && (((this.IsImportant || (this.AreaCount > 2)) && (this.Population > _architectureKind.PopulationBoundary)) || (((this.AreaCount <= 2) && !this.IsImportant) && (this.Population > (this.RecruitmentPopulationBoundary / 2))))) && ((this.BelongedSection.AIDetail.ValueRecruitment && GameObject.Chance(60)) || GameObject.Chance(15)))) && (GameObject.Random(Enum.GetNames(typeof(PersonStrategyTendency)).Length) >=(int) this.BelongedFaction.Leader.StrategyTendency))
                        {
                            num3 = 0f;
                            foreach (Military military in recruitmentMilitaryList)
                            {
                                num3 += ((float) military.RecruitmentWeighing) / ((float) military.MaxRecruitmentWeighing);
                            }
                            num3 /= (float) recruitmentMilitaryList.Count;
                            if (this.BelongedSection.AIDetail.ValueRecruitment)
                            {
                                list3.AddWorkRate(new WorkRate(num3 / 4f, ArchitectureWorkKind.补充));
                            }
                            else
                            {
                                list3.AddWorkRate(new WorkRate(num3, ArchitectureWorkKind.补充));
                            }
                        }
                    }
                    if (list3.Count > 0)
                    {
                        for (num = 0; num < this.Persons.Count; num += list3.Count)
                        {
                            foreach (WorkRate rate in list3.RateList)
                            {
                                switch (rate.workKind)
                                {
                                    case ArchitectureWorkKind.农业:
                                        foreach (Person person in this.agriculturePersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.AgricultureAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToAgricultureWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.商业:
                                        foreach (Person person in this.commercePersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.CommerceAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToCommerceWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.技术:
                                        foreach (Person person in this.technologyPersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.TechnologyAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToTechnologyWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.统治:
                                        foreach (Person person in this.dominationPersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.DominationAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToDominationWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.民心:
                                        foreach (Person person in this.moralePersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.MoraleAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToMoraleWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.耐久:
                                        foreach (Person person in this.endurancePersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.EnduranceAbility >= (120 + (this.AreaCount * 5)))))
                                            {
                                                this.AddPersonToEnduranceWorkingList(person);
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.训练:
                                        foreach (Person person in this.trainingPersons)
                                        {
                                            if (person.WorkKind == ArchitectureWorkKind.无)
                                            {
                                                foreach (Military military in list4.GetRandomList())
                                                {
                                                    if (military.RecruitmentPersonID < 0)
                                                    {
                                                        this.AddPersonToTrainingWork(person, military);
                                                        break;
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        break;

                                    case ArchitectureWorkKind.补充:
                                        foreach (Person person in this.recruitmentPersons)
                                        {
                                            if ((person.WorkKind == ArchitectureWorkKind.无) && (isFundAbundant || (person.RecruitmentAbility >= 120)))
                                            {
                                                if (recruitmentMilitaryList != null)
                                                {
                                                    foreach (Military military in recruitmentMilitaryList.GetRandomList())
                                                    {
                                                        if (military.TrainingPersonID < 0)
                                                        {
                                                            this.AddPersonToRecuitmentWork(person, military);
                                                            break;
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    if (_architectureKind.HasPopulation && (flag2 || (this.BelongedFaction.PlanTechniqueArchitecture != this)))
                    {
                        if (((!flag2 && !this.BelongedSection.AIDetail.ValueNewMilitary) && !GameObject.Chance(10)) || (this.ArmyScale >= this.LargeArmyScale))
                        {
                            MilitaryList list6 = new MilitaryList();
                            foreach (Military military in this.Militaries)
                            {
                                if ((((military.Scales < 15) && (military.Quantity > 0)) && (military.Morale < (military.MoraleCeiling / 2))) && ((military.Kind.PointsPerSoldier <= 1) && (this.BelongedFaction.TechniquePoint > (military.Quantity * (military.Kind.PointsPerSoldier + 1)))))
                                {
                                    list6.Add(military);
                                }
                            }
                            foreach (Military military in list6)
                            {
                                this.DisbandMilitary(military);
                            }
                        }
                        else if (((this.Population > this.RecruitmentPopulationBoundary) || flag2) || ((this.Population >= 10000) && GameObject.Chance(5)))
                        {
                            bool flag3 = true;
                            foreach (Military military in this.Militaries)
                            {
                                if (military.Scales < 15)
                                {
                                    flag3 = false;
                                    break;
                                }
                            }
                            if (flag3)
                            {
                                this.AIRecruitment(false);
                            }
                            else if ((this.ValueWater && !this.HasShuijun()) && this.HasShuijunMilitaryKind())
                            {
                                this.AIRecruitment(true);
                            }
                        }
                    }
                }
            }
        }

        */

        public void AllEnter()
        {
            foreach (Point point in this.GetAllContactArea().Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if (((troopByPosition != null) && (troopByPosition.BelongedFaction == this.BelongedFaction)) && troopByPosition.CanMoveAndEnterAnyway())
                {
                    troopByPosition.Enter(this);
                }
            }
        }

        public bool AllEnterAvail()
        {
            foreach (Point point in this.GetAllContactArea().Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if (((troopByPosition != null) && (troopByPosition.BelongedFaction == this.BelongedFaction)) && troopByPosition.CanMoveAndEnterAnyway())
                {
                    return true;
                }
            }
            return false;
        }

        public void PurifyFactionInfluences()
        {
            if (this.BelongedFaction != null)
            {
                foreach (Technique t in this.BelongedFaction.AvailableTechniques.Techniques.Values)
                {
                    foreach (Influences.Influence i in t.Influences.Influences.Values)
                    {
                        i.PurifyInfluence(this, Applier.Technique, t.ID);
                    }
                }
            }
        }

        public void ApplyFactionInfluences()
        {
            if (this.BelongedFaction != null)
            {
                foreach (Technique t in this.BelongedFaction.AvailableTechniques.Techniques.Values)
                {
                    foreach (Influences.Influence i in t.Influences.Influences.Values)
                    {
                        i.ApplyInfluence(this, Applier.Technique, t.ID);
                    }
                }
            }
        }

        public void ApplyInfluences()
        {
            // 🔥 关键：只清空 Characteristics 的 appliedArch
            // Characteristics 是建筑特有的，不是共享对象，可以安全清空
            foreach (var influence in this.Characteristics.Influences.Values)
            {
                influence.appliedArch.Clear();
            }
            
            // 应用 Characteristics 影响
            this.Characteristics.ApplyInfluence(this, Applier.Characteristics, 0);
            
            // 🔥 修复：应该调用 ApplyFacilityInfluencesForced()，而不是 ApplyFacilityInfluences()
            // ApplyFacilityInfluencesForced() 不会重置增益字段，只会移除旧记录并重新应用
            // ApplyFacilityInfluences() 会重置增益字段，导致 Characteristics 增益丢失
            this.ApplyFacilityInfluencesForced();
        }
        
        /// <summary>
        /// 🆕 应用势力范围增益（基于能量分级）
        /// 🧊 Cold Path：势力范围更新或读档后调用
        /// 日期：2026-03-16
        /// </summary>
        public void ApplyInfluenceBuff()
        {
            // 🔥 ANTI-BAND-AID：检查数据源
            if (this.BelongedFaction == null)
            {
                // 无归属势力的城池（如中立城池）不获得增益
                return;
            }
            
            if (this.ArchitectureArea == null)
            {
                throw new InvalidOperationException(
                    $"[Architecture.ApplyInfluenceBuff] 城池 {this.Name} 的 ArchitectureArea 为 null");
            }
            
            // 🔥 初始化顺序容错：GlobalInfluenceMap 在 InfluenceUpdateManager.Initialize() 中初始化
            // 🔥 日期：2026-03-17
            // 🔥 场景：AfterLoadGameScenario() 在 GlobalInfluenceMap 初始化前调用
            if (this.BelongedFaction.GlobalInfluenceMap is not { Length: > 0 })
            {
                // 跳过增益应用，等待 InfluenceUpdateManager.Initialize() 完成后再次调用
                return;
            }
            
            // 1. 获取城池中心位置的净能量
            int netEnergy = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.GetNetInfluenceAt(
                this.ArchitectureArea.Centre, 
                this.BelongedFaction);
            
            // 2. 计算本次应应用的增量
            float attackMultiplier = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateAttackBonus(
                netEnergy,
                isArchitecture: true);
            float newArchitectureCounterDamageDelta = attackMultiplier > 1.0f ? attackMultiplier - 1.0f : 0f;

            float defenseMultiplier = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateDefenseBonus(
                netEnergy,
                isArchitecture: true);
            float newEnduranceDropDelta = defenseMultiplier > 1.0f ? defenseMultiplier - 1.0f : 0f;

            float foodConsumptionMultiplier = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateFoodConsumptionMultiplier(
                netEnergy,
                isArchitecture: true);
            float newFoodReduceRateDelta = foodConsumptionMultiplier - 1.0f;
            int newVisionDelta = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateVisionRange(netEnergy, isArchitecture: true);
            int oldVisionDelta = _appliedInfluenceVisionDelta;

            // 3. 撤销旧增益，再应用新增益，保证幂等
            this.RateOfArchitectureCounterDamage -= _appliedInfluenceArchitectureCounterDamageDelta;
            this.enduranceDecreaseRateDrop -= _appliedInfluenceEnduranceDropDelta;
            this.RateOfFoodReduceRate -= _appliedInfluenceFoodReduceRateDelta;

            _appliedInfluenceArchitectureCounterDamageDelta = newArchitectureCounterDamageDelta;
            _appliedInfluenceEnduranceDropDelta = newEnduranceDropDelta;
            _appliedInfluenceFoodReduceRateDelta = newFoodReduceRateDelta;

            this.RateOfArchitectureCounterDamage += _appliedInfluenceArchitectureCounterDamageDelta;
            this.enduranceDecreaseRateDrop += _appliedInfluenceEnduranceDropDelta;
            this.RateOfFoodReduceRate += _appliedInfluenceFoodReduceRateDelta;

            // 4. 更新视野缓存，并在半径变化时重建视野登记
            int oldEffectiveViewDistance = this.ViewDistance + oldVisionDelta;
            _appliedInfluenceVisionDelta = newVisionDelta;
            _cachedEffectiveViewDistance = this.ViewDistance + _appliedInfluenceVisionDelta;

            if ((this.viewArea != null || this.longViewArea != null) && oldEffectiveViewDistance != _cachedEffectiveViewDistance)
            {
                this.RefreshViewArea();
            }

            // 5. 记录调试信息
            #if DEBUG
            if (_appliedInfluenceArchitectureCounterDamageDelta != 0f ||
                _appliedInfluenceEnduranceDropDelta != 0f ||
                _appliedInfluenceFoodReduceRateDelta != 0f)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Architecture.ApplyInfluenceBuff] 城池 {this.Name} 获得增益：" +
                    $"反击+{_appliedInfluenceArchitectureCounterDamageDelta * 100:F1}%，" +
                    $"减伤+{_appliedInfluenceEnduranceDropDelta * 100:F1}%，" +
                    $"粮耗{_appliedInfluenceFoodReduceRateDelta * 100:F1}%，" +
                    $"视野={_cachedEffectiveViewDistance}");
            }
            #endif
        }
        
        /// <summary>
        /// 🆕 获取动态视野范围（基于能量）
        /// 🧊 Cold Path：查询视野时调用
        /// 日期：2026-03-16
        /// </summary>
        /// <returns>视野范围（格子数）</returns>
        public int GetDynamicVisionRange()
        {
            // 🔥 ANTI-BAND-AID：检查数据源
            if (this.BelongedFaction == null)
            {
                return 0; // 无归属势力，无视野
            }
            
            if (this.ArchitectureArea == null)
            {
                throw new InvalidOperationException(
                    $"[Architecture.GetDynamicVisionRange] 城池 {this.Name} 的 ArchitectureArea 为 null");
            }
            
            // 🔥 初始化顺序容错：GlobalInfluenceMap 在 InfluenceUpdateManager.Initialize() 中初始化
            // 🔥 日期：2026-03-17
            // 🔥 场景：Faction.PrepareKnownAreaData() 在 GlobalInfluenceMap 初始化前调用
            if (this.BelongedFaction.GlobalInfluenceMap is not { Length: > 0 })
            {
                // 返回基础视野（无动态增益）
                return 0;
            }
            
            // 获取城池中心位置的净能量
            int netEnergy = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.GetNetInfluenceAt(
                this.ArchitectureArea.Centre, 
                this.BelongedFaction);
            
            // 计算视野范围（建筑可以为 0）
            return WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateVisionRange(netEnergy, isArchitecture: true);
        }
        
        /// <summary>
        /// 🆕 获取情报等级（基于能量）
        /// 🧊 Cold Path：情报查询时调用
        /// 日期：2026-03-16
        /// </summary>
        /// <returns>情报等级</returns>
        public InformationLevel GetDynamicInformationLevel()
        {
            // 🔥 ANTI-BAND-AID：检查数据源
            if (this.BelongedFaction == null)
            {
                return InformationLevel.无;
            }
            
            if (this.ArchitectureArea == null)
            {
                throw new InvalidOperationException(
                    $"[Architecture.GetDynamicInformationLevel] 城池 {this.Name} 的 ArchitectureArea 为 null");
            }
            
            // 获取城池中心位置的净能量
            int netEnergy = WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.GetNetInfluenceAt(
                this.ArchitectureArea.Centre, 
                this.BelongedFaction);
            
            // 计算情报等级
            return WorldOfTheThreeKingdoms.GameManager.InfluenceBuffCalculator.CalculateInformationLevel(netEnergy);
        }
        
        public void ApplyFacilityInfluencesForced()
        {
            // 🔥 V18 修复：必须先调用 PurifyInfluence 回退增益值
            // 原因：读档后可能已经有旧的增益值，需要先清除再重新应用
            
            // 先清除所有设施的影响
            foreach (Facility facility in this.Facilities)
            {
                facility.Influences.PurifyInfluence(this, Applier.Facility, facility.ID);
            }
            
            // 然后重新应用所有设施影响
            foreach (Facility facility in this.Facilities)
            {
                facility.Influences.ApplyInfluence(this, Applier.Facility, facility.ID);
            }
        }

        public void ApplyFacilityInfluences(bool skipNoCostFacility)
        {
            // 🔥 V19 修复：只清除将要重新应用的设施，避免误清除未启用的设施
            // 原因：如果无条件清除所有设施，会导致未启用的设施增益被错误回退
            

            
            // 先清除将要重新应用的设施影响
            foreach (Facility facility in this.Facilities)
            {
                if (!skipNoCostFacility || facility.MaintenanceCost > 0)
                {
                    bool shouldApply = (facility.MaintenanceCost <= 0) || this.FacilityEnabled;
                    
                    if (shouldApply)
                    {
                        // 只清除将要重新应用的设施
                        facility.Influences.PurifyInfluence(this, Applier.Facility, facility.ID);
                    }
                }
            }
            
            // 然后重新应用符合条件的设施影响
            foreach (Facility facility in this.Facilities)
            {
                if (!skipNoCostFacility || facility.MaintenanceCost > 0)
                {
                    // 🔥 V15 修复：无维护费用的设施总是启用，有维护费用的设施需要检查 FacilityEnabled
                    bool shouldApply = (facility.MaintenanceCost <= 0) || this.FacilityEnabled;
                    

                    
                    if (shouldApply)
                    {
                        facility.Influences.ApplyInfluence(this, Applier.Facility, facility.ID);
                    }
                }
            }
            

        }

        public int GetFacilityKindCount(int id)
        {
            int cnt = 0;
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id || this.BuildingFacility == id)
                {
                    cnt++;
                }
            }
            return cnt;
        }
        //以下添加20170426
        public int GetFacilityCountForKind(int id)
        {
            int cnt = 0;
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    cnt++;
                }
            }
            return cnt;
        }
        public string GetFacilityNameForKind(int id)
        {
            string N = "";
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    N = facility.Name;
                }
            }
            return N;
        }
        public string GetFacilityDescriptionForKind(int id)
        {
            string D = "";
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    D = facility.Description;
                }
            }
            return D;
        }
        public int GetFacilityPositionOccupiedForKind(int id)
        {
            int D = -1;
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    D = facility.PositionOccupied;
                }
            }
            return D;
        }
        public int GetFacilityMaintenanceCostForKind(int id)
        {
            int D = -1;
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    D = facility.MaintenanceCost;
                }
            }
            return D;
        }
        public bool HasTheFacilityForKind(int id)
        {
            bool H = false;
            foreach (Facility facility in this.Facilities)
            {
                if (facility.KindID == id)
                {
                    H = true;
                }
            }
            return H;
        }
        //
        public string ArmyQuantityInInformationLevel(InformationLevel level)
        {
            switch (level)
            {
                case InformationLevel.未知:
                    return "----";

                case InformationLevel.无:
                    return "----";

                case InformationLevel.低:
                    return StaticMethods.GetNumberStringByGranularity(this.ArmyQuantity, 0x2710);

                case InformationLevel.中:
                    return StaticMethods.GetNumberStringByGranularity(this.ArmyQuantity, 0x1388);

                case InformationLevel.高:
                    return StaticMethods.GetNumberStringByGranularity(this.ArmyQuantity, 0x3e8);

                case InformationLevel.全:
                    return this.ArmyQuantity.ToString();
            }
            return "----";
        }

        private void AutoDecrement()
        {
            /*if (!(((this.BelongedFaction == null) || (this.RecentlyAttacked <= 0)) || this.DayAvoidInternalDecrementOnBattle))
            {
                int maxValue = (this.RecentlyAttacked / 2) + 1;
                this.DecreaseAgriculture(GameObject.Random(maxValue));
                this.DecreaseCommerce(GameObject.Random(maxValue));
                this.DecreaseTechnology(GameObject.Random(maxValue));
                this.DecreaseMorale(GameObject.Random(maxValue));
            }*/
        }

        public bool AutoHiringAvail()
        {
            return (this.BelongedSection == null || (this.BelongedSection.AIDetail == null) || !this.BelongedSection.AIDetail.AutoRun);
        }

        private void AutoIncrement()
        {
            if (this.IncrementOfAgriculturePerDay > 0)
            {
                this.IncreaseAgriculture(this.IncrementOfAgriculturePerDay);
            }
            if (this.IncrementOfCommercePerDay > 0)
            {
                this.IncreaseCommerce(this.IncrementOfCommercePerDay);
            }
            if (this.IncrementOfTechnologyPerDay > 0)
            {
                this.IncreaseTechnology(this.IncrementOfTechnologyPerDay);
            }
            if (this.IncrementOfDominationPerDay > 0)
            {
                this.IncreaseDomination(this.IncrementOfDominationPerDay);
            }
            if (this.IncrementOfMoralePerDay > 0)
            {
                this.IncreaseMorale(this.IncrementOfMoralePerDay);
            }
            if ((this.IncrementOfEndurancePerDay > 0) && !((this.Endurance <= 0) && this.HasContactHostileTroop(this.BelongedFaction)))
            {
                this.IncreaseEndurance(this.IncrementOfEndurancePerDay);
            }
            if ((this.IncrementOfFactionReputationPerDay > 0) && (this.BelongedFaction != null))
            {
                this.BelongedFaction.IncreaseReputation(this.IncrementOfFactionReputationPerDay);
            }
            if ((this.IncrementOfFactionTechniquePointPerDay > 0) && (this.BelongedFaction != null))
            {
                this.BelongedFaction.IncreaseTechniquePoint(this.IncrementOfFactionTechniquePointPerDay);
            }
            if ((this.Technology > 0) && (this.BelongedFaction != null))
            {
                this.BelongedFaction.IncreaseTechniquePoint(this.Technology / 5);
            }
            if ((this.BelongedFaction == null) && ((this.RecentlyAttacked <= 0) && !this.HasHostileTroopsInArchitecture()))
            {
                if (this.Endurance < (50 * this.AreaCount))
                {
                    this.Endurance++;
                }
                if (this.Domination < 30)
                {
                    this.Domination++;
                }
            }
        }

        public bool AutoRewardingAvail()
        {
            return (this.BelongedSection == null || (this.BelongedSection.AIDetail == null) || !this.BelongedSection.AIDetail.AutoRun);
        }
        
        public bool RewardPersonAvail()
        {
            return this.HasPerson() && this.Fund >= Session.Parameters.RewardPersonCost && this.GetRewardPersons().Count > 0;
        }

        public bool AutoSearchingAvail()
        {
            return (this.BelongedSection == null || (this.BelongedSection.AIDetail == null) || !this.BelongedSection.AIDetail.AutoRun);
        }

        /*
        public bool AutoZhaoXianAvail()
        {
            if (Session.Current.Scenario.IsPlayer(this.BelongedFaction) && !this.BelongedSection.AIDetail.AutoRun)
            {
                return true;
            }
            return false;
        }
        */
        public bool AutoWorkingAvail()
        {
            return (this.BelongedSection == null || (this.BelongedSection.AIDetail == null) || !this.BelongedSection.AIDetail.AutoRun);
        }

        public bool AutoRecruitingAvail()
        {
            return (this.BelongedSection == null || (this.BelongedSection.AIDetail == null) || !this.BelongedSection.AIDetail.AutoRun);
        }

        public void BeginToBuildAFacility(FacilityKind facilityKind)
        {
            // 🔥 根本性修复：标记为 AI 上下文，触发属性 setter 中的检查
            _isInAIContext = true;
            try
            {
                this.BuildingFacility = facilityKind.ID;
                this.BuildingDaysLeft = Math.Max(1, (int)(facilityKind.Days * (1 - this.facilityConstructionTimeRateDecrease)));
                this.DecreaseFund(facilityKind.FundCost);
                if (this.BelongedFaction.TechniquePoint < facilityKind.PointCost)
                {
                    this.BelongedFaction.DepositTechniquePointForFacility(facilityKind.PointCost - this.BelongedFaction.TechniquePoint);
                }
                if (this.BelongedFaction.TechniquePoint < facilityKind.PointCost)
                {
                    this.BelongedFaction.DepositTechniquePointForTechnique(facilityKind.PointCost - this.BelongedFaction.TechniquePoint);
                }
                this.BelongedFaction.DecreaseTechniquePoint(facilityKind.PointCost);
                /*
                if (this.HasSpy)
                {
                    this.CreateNewFacilitySpyMessage(facilityKind);
                }
                 */
                this.PlanFacilityKind = null;
                this.PlanFacilityKindID = -1;
                // 🔥 AOT 重构：使用强类型事件替代反射调用
                WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseStartBuildFacility(Session.Current.Scenario, this, facilityKind);
            }
            finally
            {
                // 🔥 关键：无论成功或失败，都要重置 AI 上下文标志
                _isInAIContext = false;
            }
        }

        public void StopBuildingFacility()
        {
            FacilityKind fac = Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKind(this.BuildingFacility);
            this.IncreaseFund((int)(fac.FundCost * 0.5 * this.BuildingDaysLeft / fac.Days));
            this.BuildingFacility = -1;
            this.BuildingDaysLeft = 0;
        }

        public void BuildFacility(FacilityKind facilityKind)
        {
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] ===== 开始建造设施 =====");
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] 建筑: {this.Name}, 设施: {facilityKind.Name}");
            
            Facility facility = new Facility();
            facility.ID = Session.Current.Scenario.Facilities.GetFreeGameObjectID();
            facility.KindID = facilityKind.ID;
            facility.Endurance = facilityKind.Endurance;
            this.Facilities.AddFacility(facility);
            Session.Current.Scenario.Facilities.AddFacility(facility);
            
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] 设施已添加到建筑，ID={facility.ID}");
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] 开始应用设施影响，影响数量: {facility.Influences.Influences.Count}");
            
            // 🔥 关键修复：直接应用新设施的影响
            // 不需要清除 appliedArch 记录，因为每个设施都有唯一的 applierID（设施 ID）
            // InfluenceKind.ApplyInfluenceKind 会自动检查是否已经应用过（通过 HashSet.Add）
            facility.Influences.ApplyInfluence(this, Applier.Facility, facility.ID);
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] ApplyInfluence 完成");
            
            if (this.OnFacilityCompleted != null)
            {
                this.OnFacilityCompleted(this, facility);
            }
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseFacilityCompleted(Session.Current.Scenario, this, facility);
            
            System.Diagnostics.Debug.WriteLine($"[BuildFacility] ===== 设施建造完成 =====");
        }

        public bool BuildFacilityAvail()
        {
            return ((this.BuildingFacility < 0) && (this.GetBuildableFacilityKindList().Count > 0));
        }

        public bool StopBuildingFacilityAvail()
        {
            return ((this.BuildingFacility >= 0));
        }

        private struct CreateTroopInfo
        {
            public GameObjectList candidates;
            public Person leader;
            public Military military;
            public Point position;
        }

        // 计算将领能力系数 (修正版：120 上限)
        private float GetLeaderCapabilityFactor(Troop troop)
        {
            if (troop.Leader == null) return 0.5f;

            // 设定参考上限 120 (兼容宝物/官职)
            const float REF_MAX = 120.0f;
            
            // 权重算法：(统+武+智)*2 + (魅+政)*0.5
            float score = (troop.Leader.Command * 2.0f) + 
                          (troop.Leader.Strength * 2.0f) + 
                          (troop.Leader.Intelligence * 2.0f) + 
                          (troop.Leader.Glamour * 0.5f) + 
                          (troop.Leader.Politics * 0.5f);

            // 分母 = 总权重(7.0) * 参考上限(120) = 840
            float denominator = 7.0f * REF_MAX;

            // 归一化到 0~1
            return Math.Min(Math.Max(score / denominator, 0f), 1.0f);
        }

        private bool CheckOffensiveCapability(Troop troop)
        {
            System.Diagnostics.Debug.WriteLine($"[CheckOffensiveCapability] 开始检查 {troop.DisplayName}: Food={troop.Food}, CityFood={this.Food}, Qty={troop.Army?.Quantity}");
            
            float capability = GetLeaderCapabilityFactor(troop); 
            // 基础比例：能力越低，要求的兵力比例越高
            // 1.0能力 -> 0.4 MaxScale
            // 0.0能力 -> 0.7 MaxScale
            float leaderBaseRate = 0.4f + ((1.0f - capability) * 0.3f);

            // 获取编制最大上限
            int maxScale = 10000;
            if (troop.Army != null && troop.Army.Kind != null)
            {
                maxScale = troop.Army.Kind.MaxScale > 0 ? troop.Army.Kind.MaxScale : 10000;
            }

            // 规模修正：编制越大，要求的统率比例可以越低（大军团本身就是威慑）
            float scaleRatio = (float)maxScale / 100000.0f; 
            float scaleCorrection = (float)Math.Sqrt(scaleRatio); 
            if (scaleCorrection > 1.0f) scaleCorrection = 1.0f;

            // 最终兵力阈值
            int finalThreshold = (int)(maxScale * leaderBaseRate * scaleCorrection);

            // 硬性下限：至少要有 500 人
            int hardFloor = Math.Min(500, maxScale);
            if (finalThreshold < hardFloor) finalThreshold = hardFloor;

            // Check 1: Quantity
            if (troop.Army.Quantity < finalThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckOffensiveCapability] ❌ {troop.DisplayName} 兵力不足: {troop.Army.Quantity} < {finalThreshold}");
                return false;
            }

            // Check 2: Morale (Dynamic)
            // 高能力将领更能驾驭低士气部队
            // 🔥 修复：与 Troop.CheckOffensiveCapabilityStatic 保持一致
            int dynamicMorale = 60 + (int)((1.0f - capability) * 25);
            if (troop.Morale < dynamicMorale)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckOffensiveCapability] ❌ {troop.DisplayName} 士气不足: {troop.Morale} < {dynamicMorale}");
                return false;
            }

            // Check 3: Food
            // 🔥 修复：粮草检查应基于健康兵数量，而非总兵力（含伤兵）
            // 原因：FoodCostPerDay = FoodPerSoldier * TotalQuantity（包含伤兵）
            // 但伤兵会逐渐恢复，且部队可在途中补给，不应按总兵力要求 30 天粮草
            
            // 计算基于健康兵的粮草消耗（伤兵按 50% 计算）
            int effectiveFoodCost = troop.Army.Kind.FoodPerSoldier * 
                (troop.Army.Quantity + troop.Army.InjuryQuantity / 2);
            
            // 要求 15 天粮草（降低要求，因为可以途中补给）
            int requiredFood = effectiveFoodCost * 15;
            
            if (this.Food < requiredFood)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckOffensiveCapability] ❌ {troop.DisplayName} 城市粮草不足: {this.Food} < {requiredFood} (健康兵={troop.Army.Quantity}, 伤兵={troop.Army.InjuryQuantity}, 有效消耗={effectiveFoodCost}/天)");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[CheckOffensiveCapability] ✅ {troop.DisplayName} 通过检查");
            return true;
        }

        private bool CheckDefensiveCapability(Troop troop)
        {
            // 防守要求较低
            // 至少满编的 20%
             int maxScale = 10000;
            if (troop.Army != null && troop.Army.Kind != null)
            {
                maxScale = troop.Army.Kind.MaxScale > 0 ? troop.Army.Kind.MaxScale : 10000;
            }

            if (troop.Army.Quantity < maxScale * 0.2f)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckDefensiveCapability] ❌ {troop.DisplayName} 兵力不足: {troop.Army.Quantity} < {maxScale * 0.2f}");
                return false;
            }
            
            // 至少 10 天存粮
            if (this.Food < Troop.GetConservativePlanningFoodCostPerDay(troop.Army) * 10)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckDefensiveCapability] ❌ {troop.DisplayName} 城市粮草不足: {this.Food} < {Troop.GetConservativePlanningFoodCostPerDay(troop.Army) * 10}");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[CheckDefensiveCapability] ✅ {troop.DisplayName} 通过检查");
            return true;
        }

        private bool BuildOffensiveTroop(Architecture destination, LinkKind linkkind, bool offensive, int reserve)
        {
            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] ===== {this.Name} 开始创建部队 =====");
            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] destination={destination?.Name}, linkkind={linkkind}, offensive={offensive}, reserve={reserve}");
            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] AIUseNewFormationLogic={Session.Parameters.AIUseNewFormationLogic}");
            
            // 🔥 新增：防守出兵逻辑优化
            if (!offensive)
            {
                // 防守本城时不需要保留兵力，可以全部出兵
                if (destination == this)
                {
                    System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 防守本城：无需保留兵力，全部出兵");
                    reserve = 0;
                }
                else
                {
                    // 救援其他城市时使用较低的保留兵力
                    int defensiveReserve = this.getArmyReserveForDefensive();
                    if (defensiveReserve < reserve)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 救援出兵：降低保留兵力从 {reserve} 到 {defensiveReserve}");
                        reserve = defensiveReserve;
                    }
                }
            }
            
            // [新增] AI策略限制：防止在弱势情况下无脑添油
            // 如果城内（领地范围内）已集结了3支或更多士气低落（<60）的部队，暂停出兵以积蓄力量
            if (this.BelongedFaction != null && this.BelongedFaction.Troops != null)
            {
                int weakTroopCount = 0;
                int totalTroopsInCity = 0;

                foreach (Troop t in this.BelongedFaction.Troops)
                {
                    if (t != null && !t.Destroyed && t.StartingArchitecture == this)
                    {
                        // 判定“城内”：位于该据点所属的区域内
                        // 优先使用 ArchitectureArea 判断，因为部队可能不在中心点
                        bool isInCity = false;
                        if (this.ArchitectureArea != null)
                        {
                            isInCity = this.ArchitectureArea.HasPoint(t.Position);
                        }
                        else
                        {
                            isInCity = (t.Position == this.Position);
                        }

                        if (isInCity)
                        {
                            totalTroopsInCity++;
                            if (t.Morale < 60)
                            {
                                weakTroopCount++;
                            }
                        }
                    }
                }

                // 规则： 如果城内编队数量大于等于3，并且这些部队士气都未达到60
                if (totalTroopsInCity >= 3 && weakTroopCount == totalTroopsInCity)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] {this.Name} 暂停出兵：城内积压{totalTroopsInCity}支低士气部队，停止添油");
#endif
                    return false;
                }
            }

            // 🧠 AI New Formation Logic Integration
            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 检查新AI逻辑: AIUseNewFormationLogic={Session.Parameters.AIUseNewFormationLogic}, offensive={offensive}");
            // 🔥 修改：防守出兵也使用新AI逻辑
            if (Session.Parameters.AIUseNewFormationLogic)
            {
#if !DISABLE_AI_FORMATION
                string logicType = offensive ? "进攻" : "防守";
                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 使用新AI编队逻辑 ({logicType})");
                // ✅ 整合：根据 offensive 参数先准备好目标军团，并在创建部队时直接传入
                // 🔥 重构：使用新的CreateLegion方法
                Legion targetLegion = null;
                if (offensive)
                {
                    targetLegion = this.BelongedFaction.GetOrCreateLegion(destination, LegionKind.AI, LegionMission.Attack);
                }
                else
                {
                    // 防守情况：如果目标是自己，使用防守军团；否则使用进攻军团
                    LegionKind kind = LegionKind.AI;
                    LegionMission mission = (destination == this) ? LegionMission.Defend : LegionMission.Attack;
                    targetLegion = this.BelongedFaction.GetOrCreateLegion(destination, kind, mission);
                }

                // Create manager
                GameManager.AIFormationManager formationManager = new GameManager.AIFormationManager();
                // Call optimal troops and pass the legion for immediate binding.
                List<Troop> troops = formationManager.CreateOptimalTroops(this, Session.Parameters.MaxAITroopCountCandidates, reserve, assignedLegion: targetLegion);
                
                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] CreateOptimalTroops返回{troops.Count}支部队 ({logicType})");
                
                if (troops.Count > 0)
                {
                    // 🔧 校验：如果准备好的军团依然为 null，清理僵尸部队
                    if (targetLegion == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] {this.Name} 无法获取/创建目标军团，清理部队");
                        foreach (Troop t in troops) t.Enter(this);
                        return false;
                    }

                    // 🔥 防守本城时的特殊检查：过滤战斗力太低或士气太低的部队
                    if (!offensive && destination == this)
                    {
                        var validTroops = new List<Troop>();
                        foreach (Troop t in troops)
                        {
                            // 检查战斗力和士气
                            bool hasValidFightingForce = t.FightingForce > 100; // 战斗力至少100
                            bool hasValidMorale = t.Morale >= 20; // 士气至少20
                            
                            if (hasValidFightingForce && hasValidMorale)
                            {
                                validTroops.Add(t);
                                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 防守部队 {t.DisplayName} 通过检查: 战斗力={t.FightingForce}, 士气={t.Morale}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 防守部队 {t.DisplayName} 不符合要求: 战斗力={t.FightingForce}(需>100), 士气={t.Morale}(需>=20)");
                                // 解散不符合要求的部队
                                t.Enter(this);
                            }
                        }
                        
                        // 更新部队列表
                        troops = validTroops;
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 防守本城筛选后剩余{troops.Count}支合格部队");
                        
                        // 如果没有合格的部队，直接返回失败
                        if (troops.Count == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] {this.Name} 防守本城：无合格部队，取消出兵");
                            return false;
                        }
                    }
                    
                    foreach (Troop t in troops)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 部队 {t.DisplayName} 创建后武将数: {t.PersonCount} ({logicType})");
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 设置部队 {t.DisplayName} 目标: {destination.Name} ({logicType})");
                        t.WillArchitecture = destination;
                        t.TargetArchitecture = destination;
                        t.StartingArchitecture = this;  // 🔥 根本修复：属性会自动同步 StartingArchitectureID
                        
                        // 🔥 核心修复：由于是静默创建，手动触发通知
                        t.NotifyCreation();

                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 加入军团验证: Will={t.WillArchitecture?.Name}, Target={t.TargetArchitecture?.Name}, Legion={t.BelongedLegion?.Name}, PersonCount={t.PersonCount} ({logicType})");
                        
                        // 🔥 诊断：输出部队的关键移动属性
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 移动属性: Position={t.Position}, Destination={t.Destination}, RealDestination={t.RealDestination} ({logicType})");
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] 控制状态: Auto={t.Auto}, Controllable={t.Controllable} ({logicType})");
                        
                        // 🔥 修复：完全初始化后启用移动
                        t.Controllable = true;
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop-NewAI] {t.DisplayName} 初始化完成，启用移动 ({logicType})");
                    }
                    return true;
                }
#endif
            }
            Troop troop;
            if (linkkind == LinkKind.None)
            {
                return false;
            }

            SortedBoundedSet<Troop> list = new SortedBoundedSet<Troop>(Session.Parameters.MaxAITroopCountCandidates, new SimulatingFightingForceComparer());

            this.PersonsExcludeNvGuan.ClearSelected();

            GameObjectList mList = this.Militaries.GetList();
            mList.PropertyName = "Merit";
            mList.IsNumber = true;
            mList.SmallToBig = false;
            mList.ReSort();

            //Label_0309:
            foreach (Military military in mList)
            {
                // [统一逻辑] 如果是高价值精锐（造价>=800或特殊兵种），即使比例不高也允许出征
                bool isHighValue = (military.Kind.CreateCost >= 800) || (military.Kind.RecruitLimit > 0);
                
                // 🔧 FIX: 高价值精锐降低出征标准，但仍需满足基本兵力要求
                // 精锐标准：Scales >= RetreatScale（与撤退标准一致，避免矛盾）
                // 普通标准：Scales >= RetreatScale * 1.5（更高的要求）
                double minScalesRequired = isHighValue ? military.RetreatScale : military.RetreatScale * 1.5;
                
                if (military.Scales < minScalesRequired)
                {
                    if (isHighValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 跳过精锐 {military.Name}：兵力不足 (Scales:{military.Scales:F2} < {minScalesRequired:F2})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 跳过普通 {military.Name}：兵力不足 (Scales:{military.Scales:F2} < {minScalesRequired:F2})");
                    }
                    continue;
                }
                
                if (!military.Kind.Movable) continue;
                if (military.IsTransport) continue; //never deal with transports in this function

                // [AI Optimization 2026-01-08] 反添油战术检测
                // 估算当前这支预备部队的战斗力 (Combativity * 100 + quantity * 1) 简单预估
                int estimatedForce = military.Combativity * 50 + military.Quantity; 
                if (this.Persons.Count > 0)
                {
                    // 加上最强武将的加成估算
                    Person bestLeader = this.Persons.GetMaxStrengthPerson();
                    if (bestLeader != null)
                    {
                        estimatedForce += (bestLeader.Strength + bestLeader.Command) * 50;
                    }
                }

                bool canAttack = this.CheckCoordinatedAttack(destination, estimatedForce);
                if (!canAttack) 
                {
                    // System.Diagnostics.Debug.WriteLine($"[AI] {this.Name} 正在积蓄兵力，暂不攻击 {destination.Name} (Force:{estimatedForce})");
                    continue; 
                }

                bool movable = false;
                foreach (Point p in this.ArchitectureArea.GetContactArea(false).Area)
                {
                    if (this.ArchitectureArea.Area.Contains(p)) continue;
                    if (military.Kind.IsMovableOnPosition(p))
                    {
                        movable = true;
                    }
                }
                if (!movable) continue;

                switch (linkkind)
                {
                    case LinkKind.Land:
                        {
                            if (military.Kind.Type != MilitaryType.水军)
                            {
                                break;
                            }
                            continue;
                        }
                    case LinkKind.Water:
                        {
                            //if ((military.Kind.Type == MilitaryType.水军) || (this.ValueWater && (!offensive || ((military.Quantity >= 0x1f40) && (GameObject.Random(military.Kind.Merit) <= 0)))))
                            if (Session.GlobalVariables.LandArmyCanGoDownWater)
                            {
                                if (!offensive || (military.KindID != 28 && !military.IsTransport))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (military.Kind.Type == MilitaryType.水军)
                                {
                                    break;
                                }
                            }
                            continue;
                        }
                }
                if ((((military.Scales > 5) && (military.Morale >= 80)) && (military.Combativity >= 80)) && (military.InjuryQuantity < military.Kind.MinScale)
                    && (!offensive ||
                    (military.Merit > 0)
                    )) //do not use transport teams to attack
                {
                    TroopList candidates = this.AISelectPersonIntoTroop(this, military, true, destination);
                    foreach (Troop t in candidates)
                    {
                        // 🔥 AI Dispatch Capability Check
                        if (offensive) 
                        {
                            if (!CheckOffensiveCapability(t)) 
                            {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[AI Dispatch] Refused Offensive: {t.DisplayName} (Cap:{GetLeaderCapabilityFactor(t):F2}, Qty:{t.Army.Quantity})");
#endif
                                // 🔥 修复：移除豁免逻辑，所有部队都必须通过能力检查
                                // 原因：新创建的部队如果不满足条件，更应该被拒绝，而不是豁免
                                t.Destroy(true, false);
                                continue;
                            }
                        }
                        else 
                        {
                            if (!CheckDefensiveCapability(t))
                            {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[AI Dispatch] Refused Defensive: {t.DisplayName} (Qty:{t.Army.Quantity})");
#endif
                                // 🔥 修复：移除豁免逻辑，所有部队都必须通过能力检查
                                // 原因：新创建的部队如果不满足条件，更应该被拒绝，而不是豁免
                                t.Destroy(true, false);
                                continue;
                            }
                        }

                        // 🔥 战力检查已移除：让所有部队（包括新创建的）都有机会参与进攻
                        
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 尝试加入部队: {t.DisplayName}, SimulatingFightingForce={t.SimulatingFightingForce}, IsNewlyCreated={t.IsNewlyCreated}");
#endif
                        
                        Troop removed;
                        list.Add(t, out removed);
                        if (removed != null)
                        {
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] ⚠️ 部队 {removed.DisplayName} 被淘汰: SimulatingFightingForce={removed.SimulatingFightingForce}, 当前列表已满({list.Count}/{Session.Parameters.MaxAITroopCountCandidates})");
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop]   新加入部队: {t.DisplayName}, SimulatingFightingForce={t.SimulatingFightingForce}");
                            
                            // 🔥 诊断：显示当前列表中的所有部队
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop]   当前列表部队:");
                            foreach (Troop listTroop in list)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop]     - {listTroop.DisplayName}: SimulatingFightingForce={listTroop.SimulatingFightingForce}");
                            }
#endif
                            removed.Destroy(true, false);
                        }
                    }
                }
            }

            List<CreateTroopInfo> willCreate = [];
            HashSet<Person> selectedPersons = [];
            HashSet<Military> selectedMilitaries = [];
            HashSet<Point> takenPosition = [];
            bool ranOutOfArea = false;

            foreach (Troop troop2 in list)
            {
                if (this.ArmyScale < reserve) break;
                bool personAlreadyOut = false;
                foreach (Person p in troop2.Candidates)
                {
                    if (selectedPersons.Contains(p))
                    {
                        personAlreadyOut = true;
                        break;
                    }
                }
                if (personAlreadyOut) continue;
                if (selectedMilitaries.Contains(troop2.Army)) continue;

                GameArea allAvailableArea = this.GetAllAvailableArea(false);
                GameArea sourceArea = new GameArea();
                foreach (Point point in allAvailableArea.Area)
                {
                    if (!takenPosition.Contains(point) && troop2.IsMovableOnPosition(point))
                    {
                        sourceArea.Area.Add(point);        
                    }
                }
                if (sourceArea.Count == 0)
                {
                    ranOutOfArea = true;
                    break;
                }
                Point position = sourceArea[GameObject.Random(sourceArea.Count)];

                Person leader = troop2.Candidates[0] as Person;
                leader.Selected = true;
                PersonList candidates = this.SelectSubOfficersToTroop(troop2);

                CreateTroopInfo info = new CreateTroopInfo();
                info.candidates = candidates;
                info.leader = leader;
                info.military = troop2.Army;
                info.position = position;
                willCreate.Add(info);

                foreach (Person p in candidates)
                {
                    selectedPersons.Add(p);
                }
                selectedMilitaries.Add(troop2.Army);
                takenPosition.Add(position);
            }

            bool hasCreatedTroop = false;
            if (willCreate.Count > 0)
            {
                int willCreateScale = 0;
                int destScale = 0;
                if (!ranOutOfArea && willCreate.Count < 4)
                {
                    foreach (CreateTroopInfo info in willCreate)
                    {
                        willCreateScale += info.military.FightingForce;
                    }
                    GameObjectList destMilitary = destination.Militaries.GetList();
                    destMilitary.PropertyName = "FightingForce";
                    destMilitary.IsNumber = true;
                    destMilitary.SmallToBig = false;
                    destMilitary.ReSort();
                    int cnt = 0;
                    foreach (Military m in destMilitary)
                    {
                        if (cnt >= destination.PersonsExcludeNvGuan.Count) break;
                        destScale += m.FightingForce;
                    }
                }

                if (willCreateScale >= destScale || ranOutOfArea || willCreate.Count >= 4)
                {
                    foreach (CreateTroopInfo info in willCreate)
                    {
                        troop = this.CreateTroop(info.candidates, info.leader, info.military, -1, info.position);
                        
                        // ✅ CreateTroop 返回 null 是合法的业务逻辑（兵力不足、不满足条件）
                        if (troop == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] CreateTroop 返回 null（业务逻辑），跳过此部队");
                            continue;
                        }
                        
                        // 🔥 防守本城时的特殊检查：过滤战斗力太低或士气太低的部队
                        if (!offensive && destination == this)
                        {
                            bool hasValidFightingForce = troop.FightingForce > 100;
                            bool hasValidMorale = troop.Morale >= 20;
                            
                            if (!hasValidFightingForce || !hasValidMorale)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 防守部队 {troop.DisplayName} 不符合要求: 战斗力={troop.FightingForce}(需>100), 士气={troop.Morale}(需>=20)，解散");
                                troop.Destroy(true, false);
                                continue;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 防守部队 {troop.DisplayName} 通过检查: 战斗力={troop.FightingForce}, 士气={troop.Morale}");
                            }
                        }
                        
                        // 🔥 诊断：确认 destination 是敌方目标
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 创建部队:{troop.DisplayName} 目标:{destination.Name}(势力:{destination.BelongedFaction.Name}) 出发:{this.Name}(势力:{this.BelongedFaction.Name})");
                        
                        // 🔧 设置所有目标相关属性
                        troop.WillArchitecture = destination;
                        troop.TargetArchitecture = destination;
                        troop.StartingArchitecture = this;
                        
                        // 🔧 确保加入军团
                        Legion legion = this.BelongedFaction.GetLegion(destination);
                        if (legion == null)
                        {
                            legion = this.CreateOffensiveLegion(destination);
                        }

                        if (legion != null)
                        {
                            legion.AddTroop(troop);
                        }
                        else
                        {
                            // 无法创建军团（可能因为兵力不足 CreateOffensiveLegion 的严格限制）
                            // 销毁已创建的部队，防止僵尸部队
                            troop.Destroy(true, false);
                            continue;
                        }
                        
                        // 🔧 FIX: 立即验证设置
                        System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 设置后验证：Will={troop.WillArchitecture?.Name}, Target={troop.TargetArchitecture?.Name}, Start={troop.StartingArchitecture?.Name}, Legion={troop.BelongedLegion?.Name}");
                        
                        if (troop.WillArchitecture != destination || troop.BelongedLegion == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] ⚠️ 警告：设置被覆盖！Will={troop.WillArchitecture?.Name}, Legion={troop.BelongedLegion?.Name}");
                            // 强制重新设置
                            troop.WillArchitecture = destination;
                            troop.TargetArchitecture = destination;
                            if (troop.BelongedLegion == null)
                            {
                                legion.AddTroop(troop);
                            }
                        }
                        
                        //this.PostCreateTroop(troop, false);
                        hasCreatedTroop = true;
                    }
                }

            }
            
            // 🔥 根本修复：只有在使用旧逻辑时才销毁list中的部队
            // 新AI逻辑(CreateOptimalTroops)已经创建了真实部队，不应该销毁
            if (!Session.Parameters.AIUseNewFormationLogic)
            {
                foreach (Troop t in list)
                {
                    t.Destroy(true, false);
                }
            }
#if DEBUG
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BuildOffensiveTroop] 使用新AI逻辑，跳过销毁list中的{list.Count}支部队");
            }
#endif
            
            return hasCreatedTroop;
        }

        public bool IsSelfFoodEnoughForOffensive(LinkNode node, Routeway routeway)
        {
            MilitaryList cropConsumptionOrderedList = Militaries;
            cropConsumptionOrderedList.PropertyName = "FoodCostPerDay";
            cropConsumptionOrderedList.IsNumber = true;
            cropConsumptionOrderedList.ReSort();
            PersonList leaderablePersonList = new PersonList();
            foreach (Person p in this.PersonsExcludeNvGuan)
            {
                if (p.Command >= 40)
                {
                    leaderablePersonList.Add(p);
                }
            }
            double consumptionRate = Session.Current.Scenario.GetDistance(this.ArchitectureArea, node.A.ArchitectureArea) / 50.0 + 1;
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    {
                        int crop = 0;
                        int troopCnt = 0;
                        foreach (Military m in cropConsumptionOrderedList)
                        {
                            if ((((m.Scales >= 3) && (m.Morale >= 80)) && (m.Combativity >= 80)) && (m.InjuryQuantity < m.Kind.MinScale) && m.Kind.Type != MilitaryType.水军)
                            {
                                crop += m.FoodCostPerDay;
                                troopCnt++;
                                if (troopCnt >= leaderablePersonList.Count) break;
                            }
                        }
                        return (this.Food >= crop * consumptionRate * 1.1);
                    }

                case LinkKind.Water:
                    {
                        int crop = 0;
                        int troopCnt = 0;
                        foreach (Military m in cropConsumptionOrderedList)
                        {
                            if ((((m.Scales >= 3) && (m.Morale >= 80)) && (m.Combativity >= 80)) && (m.InjuryQuantity < m.Kind.MinScale) && m.Kind.Type == MilitaryType.水军)
                            {
                                crop += m.FoodCostPerDay;
                                troopCnt++;
                                if (troopCnt >= leaderablePersonList.Count) break;
                            }
                        }
                        return (this.Food >= crop * consumptionRate * 1.1);
                    }

                case LinkKind.Both:
                    {
                        int crop = 0;
                        int troopCnt = 0;
                        foreach (Military m in cropConsumptionOrderedList)
                        {
                            if ((((m.Scales >= 3) && (m.Morale >= 80)) && (m.Combativity >= 80)) && (m.InjuryQuantity < m.Kind.MinScale))
                            {
                                crop += m.FoodCostPerDay;
                                troopCnt++;
                                if (troopCnt >= leaderablePersonList.Count) break;
                            }
                        }

                        return (this.Food >= crop * consumptionRate * 1.1);
                    }
            }
            return false;
        }

        public Routeway BuildRouteway(LinkNode node, bool hasEnd)
        {
            Point key = new Point(base.ID, node.A.ID);
            if (!this.BelongedFaction.ClosedRouteways.ContainsKey(key))
            {
                Point? nullable;
                Point? nullable2;
                Session.Current.Scenario.GetClosestPointsBetweenTwoAreas(this.GetRoutewayStartPoints(), node.A.GetAIRoutewayEndPoints(this, false), out nullable, out nullable2);
                if (nullable.HasValue && nullable2.HasValue)
                {
                    this.BelongedFaction.RoutewayPathBuilder.MultipleWaterCost = false;
                    this.BelongedFaction.RoutewayPathBuilder.MustUseWater = node.Kind == LinkKind.Water;
                    if (this.BelongedFaction.RoutewayPathAvail(nullable.Value, nullable2.Value, hasEnd))
                    {
                        Routeway routeway = this.CreateRouteway(this.BelongedFaction.GetCurrentRoutewayPath());
                        routeway.DestinationArchitecture = node.A;
                        if (hasEnd)
                        {
                            routeway.EndArchitecture = node.A;
                        }
                        return routeway;
                    }
                    this.BelongedFaction.ClosedRouteways.Add(new Point(base.ID, node.A.ID), null);
                }
            }
            return null;
        }

        public Routeway BuildShortestRouteway(Architecture des, bool noWater)
        {
            Point? nullable;
            Point? nullable2;
            if (!noWater)
            {
                Point key = new Point(base.ID, des.ID);
                if (this.BelongedFaction.ClosedRouteways.ContainsKey(key))
                {
                    return null;
                }
            }
            Session.Current.Scenario.GetClosestPointsBetweenTwoAreas(this.GetRoutewayStartPoints(), des.GetRoutewayStartPoints(), out nullable, out nullable2);
            if (nullable.HasValue && nullable2.HasValue)
            {
                this.BelongedFaction.RoutewayPathBuilder.MultipleWaterCost = noWater;
                if (this.BelongedFaction.RoutewayPathAvail(nullable.Value, nullable2.Value, true))
                {
                    Routeway routeway = this.CreateRouteway(this.BelongedFaction.GetCurrentRoutewayPath());
                    routeway.DestinationArchitecture = des;
                    routeway.EndArchitecture = des;
                    return routeway;
                }
                if (!noWater)
                {
                    this.BelongedFaction.ClosedRouteways.Add(new Point(base.ID, des.ID), null);
                }
            }
            return null;
        }

        public Routeway BuildShortestRouteway(Point point, bool noWater)
        {
            Point closestPoint = Session.Current.Scenario.GetClosestPoint(this.GetRoutewayStartPoints(), point);
            this.BelongedFaction.RoutewayPathBuilder.MultipleWaterCost = noWater;
            if (this.BelongedFaction.RoutewayPathAvail(closestPoint, point, true))
            {
                return this.CreateRouteway(this.BelongedFaction.GetCurrentRoutewayPath());
            }
            return null;
        }

        public void BuyFood(int spendFund)
        {
            this.DecreaseFund(spendFund);
            this.IncreaseFood(spendFund * Session.Parameters.FundToFoodMultiple);
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseBuyFood(Session.Current.Scenario, this);
        }

        public bool BuyFoodAvail()
        {
            // 🔥 技术性修复：避免ArgumentNullException
            string archName = this.Name ?? "";
            
            return this.Agriculture >= Session.Parameters.BuyFoodAgriculture && (Session.Current.Scenario.Date.Season == GameSeason.夏 || Session.Current.Scenario.Date.Season == GameSeason.秋) && this.Fund > 0 && this.Food < this.FoodCeiling
                && (Session.Current.Scenario.Date.Month * 483
                + (archName.Length > 0 ? archName[0] : 735) * 203
                + (archName.Length > 1 ? archName[1] : 492) * 680
                    + this.ID * 912) % 2 == 0;
        }

        public bool CampaignAvail()
        {
            if ((this.PersonsExcludeNvGuan.Count > 0) && (this.Militaries.Count > 0))
            {
                foreach (Military military in this.Militaries)
                {
                    if (((military.Quantity > 0) && (military.Morale > 0)) && (this.GetMilitaryCampaignArea(military).Count > 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ChangeCapitalAvail()
        {
            return this.BelongedFaction != null && this.BelongedFaction.ArchitectureCount > 1 && this.IsCapital && this.Fund >= this.ChangeCapitalCost && this.BelongedFaction.Leader.Status != PersonStatus.Captive;
        }

        public bool AppointMayorAvail() //任命县令
        {
            if (this.BelongedFaction != null && this.BelongedFaction.Leader.BelongedCaptive == null && this.MayorID == -1  && _architectureKind.ID != 4)
            {
                if (Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.MayorCandicate.Count > 0)
                {
                    return true;
                }

                if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.AIMayorCandicate.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool RecallMayorAvail() //罢免县令
        {
            if (this.BelongedFaction != null && this.Mayor != null)
            {
                return true;
            }
            return false;
        }

        public bool CanAppointAdvisor() //可以任命军师
        {
            if (this.BelongedFaction != null && this.BelongedFaction.Leader.BelongedCaptive == null)
            {
                if (Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.BelongedFaction.AdvisorCandicate.Count > 0)
                {
                    return true;
                }

                if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.BelongedFaction.AIAdvisorCandicate.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasAdvisor() //有军师
        {
            return this.BelongedFaction != null && this.BelongedFaction.Advisor != null;
        }

        public bool CanAppointAdvisorOrHasAdvisor() //可以任命军师或有军师
        {
            return this.CanAppointAdvisor() || this.HasAdvisor();
        }

       public void RecallMayor()
       {

           foreach (Person p in this.BelongedFaction .Persons)
           {
               if (p.ID == this.MayorID)
               {
                   this.Mayor = null;
               }
           }
       }

        /// <summary>
        /// 检查是否可以任命军师（委托给所属势力）
        /// </summary>
        public bool AppointAdvisorAvail()
        {
            return this.BelongedFaction != null && this.BelongedFaction.AppointAdvisorAvail();
        }

        /// <summary>
        /// 检查是否可以罢免军师（委托给所属势力）
        /// </summary>
        public bool RecallAdvisorAvail()
        {
            bool isPlayerOwned = Session.GlobalVariables.SkyEye || this.CurrentPlayerOwned();
            bool hasAdvisor = this.HasAdvisor();
            
            System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 城池: {this.Name}");
            System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 玩家拥有: {isPlayerOwned}");
            System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 有军师: {hasAdvisor}");
            
            if (this.BelongedFaction != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 势力: {this.BelongedFaction.Name}");
                System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 军师ID: {this.BelongedFaction.AdvisorID}");
                System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 军师对象: {this.BelongedFaction.Advisor?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 势力RecallAdvisorAvail: {this.BelongedFaction.RecallAdvisorAvail()}");
            }
            
            bool result = isPlayerOwned && hasAdvisor;
            System.Diagnostics.Debug.WriteLine($"[RecallAdvisorAvail] 最终结果: {result}");
            
            return result;
        }



        GameObjectList assassinatablePersons = new GameObjectList();
        public GameObjectList AssassinatablePersons(Faction f)
        {
            if (this.BelongedFaction == null || this.BelongedFaction.IsFriendly(f))
            {
                assassinatablePersons = this.NoFactionPersons;
            }
            else
            {
                assassinatablePersons = this.MovablePersons.GetList();
                assassinatablePersons.AddRange(this.NoFactionPersons);
            }
            return assassinatablePersons;
        }

        public PersonList MayorCandicate
        {
            get
            {
                PersonList result = new PersonList() ;
                foreach (Person p in this.PersonsExcludeNvGuan)
                {
                    if (p != this.BelongedFaction.Leader && this.Mayor != p)
                    {
                        result.Add(p);
                    }
                }

                return result;
            }
        }

        public PersonList AIMayorCandicate
        {
            get
            {
                PersonList result = new PersonList();

                foreach (Person p in this.MayorCandicate)
                {
                    result.Add(p);
                }

                result.PropertyName = "AbilitySum";
                result.IsNumber = true;
                result.SmallToBig = false;
                result.ReSort();
                return result;
            }
        }


        public event Appointmayor OnAppointmayor; //县令年表
        public delegate void Appointmayor(Person p, Person q);
        public void AppointMayor(Person Person)
        {
            Session.Current.Scenario.YearTable.addAppointMayorEntry(Session.Current.Scenario.Date, Person, this.BelongedFaction.Leader);
            if (this.OnAppointmayor != null)
            {
                this.OnAppointmayor(this.BelongedFaction.Leader, Person);
            }
            if (this.Mayor != null )
            {
                //this.Mayor.LocationArchitecture = this;
               // this.IncrementOfEndurancePerDay = 100;

                //this.Mayor.DayRateIncrementOfpublic += 0.7f;  //太守内政加成系数
            }
        }




        public bool SelectPrinceAvail()
        {


            if (this.BelongedFaction != null && this.BelongedFaction.Leader.BelongedCaptive == null &&
                (this.BelongedFaction.PrinceID == -1) &&
                this.Fund >= Session.Parameters.SelectPrinceCost && this.BelongedFaction.Leader.ChildrenCanBeSelectedAsPrince().Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public event Selectprince OnSelectprince; //立储
        public delegate void Selectprince(Person p, Person q);
        public void SelectPrince(Person Person)
        {
            Session.Current.Scenario.YearTable.addSelectPrinceEntry(Session.Current.Scenario.Date, Person, this.BelongedFaction.Leader);
            if (this.OnSelectprince != null)
            {
                this.OnSelectprince(this.BelongedFaction.Leader, Person);
            }
        }

        public bool BecomeEmperorLegallyAvail()
        {

            return this.BelongedFaction.BecomeEmperorLegallyAvail();
        }



        public bool CanZhaoXian()
        {
            if (this.BelongedFaction != null && Session.GlobalVariables.ZhaoXianSuccessRate > 0 && Session.Current.Scenario.Date.Month == 3
              && this.BelongedFaction.ZhaoxianFailureCount < 1 && this.BelongedFaction != null && this.BelongedFaction.Leader.Status != PersonStatus.Captive)

            {
                if (this.AvailGeneratorTypeList().Count > 0 )
                {
                    return true;
                }
            }

            return false;
        }


        public PersonGeneratorTypeList AvailGeneratorTypeList()
        {
            PersonGeneratorTypeList list = new PersonGeneratorTypeList();

            foreach (GameObject obj in Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes.GameObjects)
            {
                if (obj is PersonGeneratorType type && 
                    this.Fund >= type.CostFund && 
                    this.BelongedFaction.GetGeneratorPersonCount(type) < type.FactionLimit)
                {
                    list.Add(type);
                }
            }
            return list;

        }

        private bool IsChanceOfGeneratingOfficer(int factionPersonCount, bool isAI, PersonGeneratorType preferredType)
        {
            float coef = isAI ? Session.Parameters.AIExtraPerson : 1;
            if (coef <= 0)
            {
                return false ;
            }

            float result = GameObject.Random((int)(10000 * Math.Pow(factionPersonCount, Session.Parameters.SearchPersonArchitectureCountPower) / Person.CreatePersonFactor));
            float target = Session.GlobalVariables.ZhaoXianSuccessRate * coef * preferredType.generationChance;
            return result < target;
        }

        public void DoZhaoXian(PersonGeneratorType preferredType)
        {
            // 🔥 修复：添加基本的空引用检查
            if (this.BelongedFaction == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DoZhaoXian] BelongedFaction为null，无法执行招贤");
                return;
            }
            
            // 🔥 Checklist Fix: Priority 3 - Defensive Check
            if (this.BelongedFaction.Leader == null)
            {
                 System.Diagnostics.Debug.WriteLine($"[DoZhaoXian] BelongedFaction.Leader为null，无法执行招贤");
                 return;
            }
            
            if (preferredType == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DoZhaoXian] preferredType为null，无法执行招贤");
                return;
            }

            bool isAI = !Session.Current.Scenario.IsPlayer(this.BelongedFaction);

            if (!IsChanceOfGeneratingOfficer(this.BelongedFaction.PersonCount,isAI,preferredType))
            {
                if (!isAI)
                {
                    Session.MainGame.mainGameScreen.xianshishijiantupian(this.BelongedFaction.Leader, this.Name, "ZhaoXianFailed", "ZhaoXian.jpg", "ZhaoXianFailed", true);
                }
                this.BelongedFaction.ZhaoxianFailureCount++;
                return ;
            }

           // PersonGeneratorType type = new PersonGeneratorType();
            //type.ID = preferredType;
            PersonGenerateParam param = new PersonGenerateParam(this,this.BelongedFaction.Leader, true,preferredType ,isAI);
            Person r = Person.createPerson(param, true);
            if (r == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DoZhaoXian] 创建人物失败，返回null");
                return;
            }
            
            if (this.BelongedFaction.IsAlien && r.PersonalLoyalty >= 2)
            {
                r.PersonalLoyalty = GameObject.Random(0, 1);
            }
            
            // 🔥 修复：添加空引用检查
            if (this.BelongedFaction?.Leader != null && r.IdealTendency != null)
            {
                r.Ideal = (this.BelongedFaction.Leader.Ideal + GameObject.Random(r.IdealTendency.Offset * 2 + 1) - r.IdealTendency.Offset) % 150;
            }
            else
            {
                // 如果无法获取Leader的理想或人物的理想倾向，使用默认值
                r.Ideal = GameObject.Random(150);
                System.Diagnostics.Debug.WriteLine($"[DoZhaoXian] 无法获取Leader理想或人物理想倾向，使用随机理想值: {r.Ideal}");
            }
            this.ZhaoXian(r);
            
            // 🔥 修复：添加空引用检查
            if (this.BelongedFaction != null)
            {
                this.BelongedFaction.YearOfficialLimit++;
                this.BelongedFaction.GetGeneratorPersonCount(preferredType);
                this.BelongedFaction.IncrementGeneratorCount(preferredType);
            }
            
            if (preferredType != null)
            {
                preferredType.TypeCount++;
                this.DecreaseFund(preferredType.CostFund);
            }

        }

        public void GenerateOfficer(PersonGeneratorType preferredType, bool success) //事件专用
        {
            PersonGenerateParam param = new PersonGenerateParam(this, this.BelongedFaction.Leader, true, preferredType, false);
            Person r = Person.createPerson(param, true);
            //this.ZhaoXian(r);
            this.DecreaseFund(preferredType.CostFund);
        }

        public event Zhaoxian OnZhaoxian; //招贤
        public delegate void Zhaoxian(Person p, Person q);
        public void ZhaoXian(Person person)
        {
            Session.Current.Scenario.YearTable.addZhaoXianEntry(Session.Current.Scenario.Date, person, this.BelongedFaction.Leader);
            if (this.OnZhaoxian != null)
            {
                this.OnZhaoxian(this.BelongedFaction.Leader, person);
            }
        }

        /*
        public int CreatePersonCost
        {
            get
            {
                if (this.BelongedFaction.PersonCount <= 5)
                {
                    return 5000;
                }
                else if (this.BelongedFaction.PersonCount > 5 && this.BelongedFaction.PersonCount <= 10)
                {
                    return 10000;
                }
                else
                {
                    return (this.BelongedFaction.SelfOfficerCount * 1000 + 10000);
                }
            }
        }

        /*
        public bool DengYongAvail()
        {
            if (this.BelongedFaction != null && this.NoFactionOfficerCount > 0)
            {
                foreach (Person person in this.NoFactionOfficers)
                {
                    if (this.Fund > person.UntiredMerit)
                    {
                        return true;
                    }
                }

            }
            return false;
        }


        */

        public bool DismissOfficerAvail()
        {
            if (this.BelongedFaction != null && this.NoFactionOfficerCount > 0)
            {
                return true;
            }
            return false;
        }

        public void DismissOfficer() //遣散野武将
        {

            foreach (Person person in this.NoFactionOfficers)
            {
                person.Alive = false;
                person.Status = PersonStatus.None;
                Session.Current.Scenario.Persons.Remove(person);
                //this.NoFactionOfficers.Remove(person);
            }

        }

        public PersonList NoFactionOfficers //在野野武将列表
        {
            get
            {
                PersonList result = new PersonList();

                foreach (Person p in this.NoFactionPersons)
                {
                    if (p.ID >= 25000)
                    {
                        result.Add(p);
                    }
                }

                return result;
            }
        }

        public int NoFactionOfficerCount
        {
            get
            {
                return (this.NoFactionOfficers.Count);
            }
        }


        public PersonList Kerenmingdeguanyuan
        {
            get
            {
                PersonList list = new PersonList();

                foreach (Person p in this.PersonsExcludeNvGuan)
                {
                    if (p != this.BelongedFaction.Leader && p.GetAppointableTitleList().Count > 0)
                    {
                        list.Add(p);
                    }
                }
                return list;
            }
        }



        public bool CanAppoint()
        {
            if (this.BelongedFaction != null && this.Kerenmingdeguanyuan.Count > 0)
            {
                return true;
            }
            return false;
        }

        public PersonList RecallableOfficer
        {
            get
            {
                PersonList list = new PersonList();

                foreach (Person p in this.PersonsExcludeNvGuan)
                {
                    if (p != this.BelongedFaction.Leader && p.RecallableTitleList().Count > 0)
                    {
                        list.Add(p);
                    }
                }
                return list;
            }
        }

        public bool RecallOfficerAvail()
        {
            if (this.BelongedFaction != null && this.RecallableOfficer.Count > 0)
            {
                return true;
            }
            return false;
        }

        public bool SelfBecomeEmperorAvail()
        {
            return this.BelongedFaction.SelfBecomeEmperorAvail();
        }

        public void ChangeFaction(Faction faction)
        {
            // 🔥 NEW: 通知原势力AI紧急事件 - 城市丢失
            if (this.BelongedFaction != null && faction != this.BelongedFaction)
            {
                this.BelongedFaction.NotifyPersonnelUrgentEvent($"城市{this.Name}丢失");
                this.BelongedFaction.NotifyDomesticUrgentEvent($"城市{this.Name}丢失");
            }
            
            this.ResetAuto();
            if ((faction != null) && Session.Current.Scenario.IsPlayer(faction))
            {
                this.AutoHiring = true;
                // 🔧 修复：自动褒赏默认关闭，由玩家手动开启
                // this.AutoRewarding = true;

            }
            if ((faction != null) && (this.BelongedFaction != null))
            {
                // 🔥 修复：移除旧势力的补给范围注册
                // 日期：2026-03-07
                // 问题：城池变更势力后，MapTileData 中的 SupplyingArchitectures 仍指向旧势力
                //       导致部队无法从新势力城池获得补给（势力过滤失败）
                this.RemoveBaseSupplyingArchitecture();
                
                this.BelongedFaction.Architectures.Remove(this);
                this.BelongedFaction.RemoveArchitectureKnownData(this);
                if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                {
                    this.ClearRouteways();
                }
                else
                {
                    foreach (Routeway routeway in this.Routeways)
                    {
                        this.BelongedFaction.RemoveRouteway(routeway);
                    }
                }
                foreach (Military military in this.Militaries)
                {
                    this.BelongedFaction.RemoveMilitary(military);
                }
                this.PurifyFactionInfluences();
                this.BelongedFaction = null;
                faction.AddArchitecture(this);
                faction.AddArchitectureKnownData(this);
                foreach (Captive captive in this.Captives.GetList())
                {
                    if (captive.CaptiveFaction == faction)
                    {
                        captive.CaptivePerson.SetBelongedCaptive(null, PersonStatus.Normal);
                    }
                }
                foreach (Military military in this.Militaries)
                {
                    faction.AddMilitary(military);
                }
                foreach (Routeway routeway in this.Routeways)
                {
                    faction.AddRouteway(routeway);
                }
                faction.FirstSection.AddArchitecture(this);
                this.ApplyFactionInfluences();
                
                // 🔥 修复：添加新势力的补给范围注册
                // 日期：2026-03-07
                // 原因：城池变更势力后，需要重新注册补给范围到 MapTileData
                //       否则新势力的部队无法从该城池获得补给
                this.AddBaseSupplyingArchitecture();
                
                // 🔥 记录占领时间（用于识别新占领城市）
                // 日期：2026-03-12
                _occupiedDate = Session.Current.Scenario.Date;
                
                // 🔥 NEW: 通知新势力AI紧急事件 - 新占领建筑
                faction.NotifyPersonnelUrgentEvent($"新占领建筑{this.Name}");
                faction.NotifyDomesticUrgentEvent($"新占领建筑{this.Name}");
            }
            if (faction != null)
            {
                //this.jianzhuqizi.qizidezi.Text = faction.ToString().Substring(0, 1);
            }
        }

        private void CheckAmbushTroop(Point p)
        {
            Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(p);
            if (((troopByPosition != null) && (troopByPosition.Status == TroopStatus.埋伏)) && !this.IsFriendly(troopByPosition.BelongedFaction))
            {
                this.DetectAmbush(troopByPosition, this.BelongedFaction.GetKnownAreaData(p));
            }
        }

        private void CheckBuildingFacility()
        {
            if (this.BuildingDaysLeft > 0)
            {
                this.BuildingDaysLeft -= 1;
                if (this.BuildingDaysLeft <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CheckBuildingFacility] 设施建造完成: {this.Name}, BuildingFacility={this.BuildingFacility}");
                    
                    FacilityKind facilityKind = Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKind(this.BuildingFacility);
                    if (facilityKind != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CheckBuildingFacility] 找到设施类型: {facilityKind.Name}");
                        this.BuildFacility(facilityKind);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CheckBuildingFacility] ⚠️ 未找到设施类型 ID={this.BuildingFacility}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CheckBuildingFacility] 清除建造状态");
                    this.BuildingFacility = -1;
                    
                    // 🔥 修复：设施建造完成后，立即选择下一个设施（不等待AI回合）
                    // 注意：这里只选择设施（设置PlanFacilityKind），不立即建造
                    // 真正的建造会在下次AIFacility()调用时进行（有资金检查）
                    System.Diagnostics.Debug.WriteLine($"[CheckBuildingFacility] 尝试选择下一个设施建造");
                    SelectNextFacilityToBuild();
                }
            }
        }
        
        /// <summary>
        /// 选择下一个要建造的设施（仅选择，不立即建造）
        /// 这个方法跳过AIFacility()的资金检查，只负责选择最优设施
        /// </summary>
        private void SelectNextFacilityToBuild()
        {
            // 基础检查
            if (this.BelongedFaction == null) return;
            if (this.FacilityPositionCount <= 0) return;
            if (this.PlanFacilityKind != null) return; // 已有计划，不重复选择
            
            // 选择最优设施（复用AIFacility中的选择逻辑）
            double maxValue = double.MinValue;
            FacilityKind toBuild = null;
            
            foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKindList())
            {
                if (kind.IsExtension) continue;
                if (!kind.CanBuild(this)) continue;
                if (kind.rongna > 0) continue;
                
                // 跳过维护成本过高的设施
                if ((kind.MaintenanceCost + this.FacilityMaintenanceCost) * 30 + 2000 > this.ExpectedFund && kind.NetFundIncrease <= 0)
                {
                    continue;
                }
                
                // 跳过造价过高的设施（超过资金上限一半）
                if (kind.FundCost > this.FundCeiling / 2)
                {
                    continue;
                }
                
                // 计算设施价值
                float value = (float)kind.AIValue(this);
                foreach (KeyValuePair<Condition, float> weight in kind.AIBuildConditionWeight)
                {
                    if (weight.Key.CheckCondition(this))
                    {
                        value *= weight.Value;
                    }
                }
                
                if (value > 0 && value > maxValue)
                {
                    // 检查空间是否足够
                    if (this.FacilityPositionLeft >= kind.PositionOccupied)
                    {
                        maxValue = value;
                        toBuild = kind;
                    }
                }
            }
            
            // 设置计划设施
            if (toBuild != null)
            {
                this.PlanFacilityKind = toBuild;
                System.Diagnostics.Debug.WriteLine($"[SelectNextFacilityToBuild] {this.Name} 选择下一个设施: {toBuild.Name}");
                
                // 如果有技巧点需求，尝试保留技巧点
                if (this.BelongedFaction != null && GameObject.Chance(0x21) && 
                    ((this.BelongedFaction.TechniquePoint + this.BelongedFaction.TechniquePointForFacility) < this.PlanFacilityKind.PointCost))
                {
                    this.BelongedFaction.SaveTechniquePointForFacility(this.PlanFacilityKind.PointCost / this.PlanFacilityKind.Days);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SelectNextFacilityToBuild] {this.Name} 没有可建造的设施");
            }
        }

        public LinkKind CheckCampaignable(LinkNode node)
        {
            bool flag = true;
            bool flag2 = true;
            for (int i = 1; i < node.Path.Count; i++)
            {
                flag = flag && node.Path[i - 1].IsLandLink(node.Path[i]);
                flag2 = flag2 && node.Path[i - 1].IsWaterLink(node.Path[i]);
            }
            if (flag && flag2)
            {
                return LinkKind.Both;
            }
            if (flag)
            {
                return LinkKind.Land;
            }
            if (flag2)
            {
                return LinkKind.Water;
            }
            return LinkKind.None;
        }

        public void CheckIsFrontLine()
        {
            if (this.BelongedFaction == null) return;
            this.FrontLine = false;
            this.HostileLine = false;
            this.CriticalHostile = false;
            this.noFactionFrontline = false;
            this.orientationFrontLine = false;
            this.withoutTruceFrontline = false;
            this.BelongedFaction.RoutewayPathBuilder.ConsumptionMax = 0.35f;
            foreach (Architecture architecture in this.GetAILinks())
            {
                if (this.IsFriendlyWithoutTruce(architecture.BelongedFaction))
                {
                    continue;
                }
                if (architecture.BelongedFaction == null)
                {
                    noFactionFrontline = true;
                    continue;
                }
                this.FrontLine = true;
                if (this.IsArchitectureHostileWithoutTruce(architecture))
                {
                    this.withoutTruceFrontline = true;
                }
                if (this.IsArchitectureHostile(architecture))
                {
                    this.HostileLine = true;
                }
                if (this.IsArchitectureCriticalHostile(architecture))
                {
                    this.CriticalHostile = true;
                }
                if (this.BelongedSection != null && this.BelongedSection.OrientationFaction == architecture.BelongedFaction)
                {
                    this.orientationFrontLine = true;
                }
            }
            this.BelongedFaction.RoutewayPathBuilder.ConsumptionMax = 0.7f;
        }

        private void RefreshNeutralBuilding()
        {
            if (_architectureKind.ID == 250)
            {
                this.Endurance += 50;
                this.Domination += 5;
                this.Morale += 10;
                this.BelongedFaction = null;
                foreach (Troop troop in Session.Current.Scenario.Troops)
                {
                    if (troop.BelongedFaction != null && this.ViewArea.HasPoint(troop.Position))
                    {
                        if (troop.Army.Tiredness > 0)
                        {
                            troop.Army.Tiredness -= Session.GlobalVariables.TroopTirednessDecrease;
                        }

                        if (troop.Morale < 100)
                        {
                            troop.Morale += 10;
                        }

                        if (troop.Combativity < 100)
                        {
                            troop.Combativity += 10;
                        }
                    }
                    if (troop.Army.Tiredness < 0)
                    {
                        troop.Army.Tiredness = 0;
                    }
                }
            }
        }

        private void CheckRobberTroop()
        {
            if (this.RobberTroop != null && this.RobberTroop.BelongedFaction != null)
            {
                this.RobberTroop = null;
                this.RobberTroopID = -1;
            }
            if (this.BelongedFaction != null)
            {
                if ((this.RobberTroop != null) && (this.RobberTroop.RecentlyFighting <= 0))
                {
                    this.RobberTroop.Destroy(true, true);
                    Session.Current.Scenario.Militaries.Remove(this.RobberTroop.Army);
                    Session.Current.Scenario.Troops.RemoveTroop(this.RobberTroop);
                    this.RobberTroop = null;
                }
            }
            else if (this.RobberTroop == null)
            {
                if ((this.JustAttacked && (this.Endurance > 0)) && !this.HasHostileTroopsInArchitecture())
                {
                    List<Point> orientations = new List<Point>();
                    foreach (Troop troop in this.GetHostileTroopsInView())
                    {
                        orientations.Add(troop.Position);
                    }
                    this.CreateRobberTroop(Session.Current.Scenario.GetClosestPosition(this.ArchitectureArea, orientations).Value);
                }
            }
            else if (!(((this.RecentlyAttacked > 0) || (this.RobberTroop.RecentlyFighting > 0)) || this.HasHostileTroopsInView()))
            {
                this.RobberTroop.Destroy(true, true);
                Session.Current.Scenario.Militaries.Remove(this.RobberTroop.Army);
                Session.Current.Scenario.Troops.RemoveTroop(this.RobberTroop);
                this.RobberTroop = null;
            }
        }

        private void AlienTroopGain()
        {
            if (this.BelongedFaction != null && this.BelongedFaction.IsAlien && this.BelongedFaction.Capital == this && !IsSurrounded())
            {
                // 🔥 根本修复：使用对象快照避免集合修改导致的索引越界
                // 日期：2026-03-19
                // 原因：CreateMilitary() 会调用 AddMilitary()，修改 Militaries 集合
                // 问题：在遍历 Militaries.GetList() 时调用 CreateMilitary() 会导致索引越界
                // 解决方案：先创建 List<Military> 快照，避免直接遍历可能被修改的集合
                // 性能：Cold Path（每回合一次），使用 List 分配可接受
                
                List<Military> militaries = [];
                foreach (var mo in Militaries.GetList())
                {
                    if (mo is Military m && m.Quantity < m.Kind.MaxScale)
                    {
                        militaries.Add(m);
                    }
                }

                Military targetMilitary;
                if (militaries.Count == 0)
                {
                    // 🔥 关键修复：异族势力可能没有 BaseMilitaryKinds，使用城市的私有兵种
                    // 日期：2026-03-19
                    // 原因：异族势力的 BaseMilitaryKinds 可能为空（正常设定）
                    // 解决方案：优先使用 BaseMilitaryKinds，如果为空则使用城市的 PrivateMilitaryKinds
                    // 如果两者都为空，则跳过（不创建新编队）
                    
                    GameObjectList kindList = BelongedFaction.BaseMilitaryKinds.GetMilitaryKindList();
                    
                    // 如果势力没有 BaseMilitaryKinds，使用城市的私有兵种
                    if (kindList == null || kindList.Count == 0)
                    {
                        kindList = this.PrivateMilitaryKinds.GetMilitaryKindList();
                    }
                    
                    // 如果城市也没有私有兵种，跳过（不创建新编队）
                    if (kindList == null || kindList.Count == 0)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"[AlienTroopGain] {this.Name}(ID:{this.ID}) 无可用兵种，跳过创建新编队。" +
                            $"势力：{BelongedFaction.Name}(ID:{BelongedFaction.ID})");
                        #endif
                        return;
                    }
                    
                    targetMilitary = CreateMilitary(kindList.GetRandomObject() as MilitaryKind);
                }
                else
                {
                    targetMilitary = militaries[GameObject.Random(militaries.Count)];
                }
                targetMilitary.Quantity = (int)Math.Min(targetMilitary.Quantity + Session.Parameters.AlienTroopGain * Session.Parameters.AIRecruitmentSpeedRate, targetMilitary.Kind.MaxScale);
            }
        }

        public void ClearFundPacks()
        {
            this.FundPacks.Clear();
        }

        public void ClearFoodPacks()
        {
            this.FoodPacks.Clear();
        }

        public void ClearPopulationPacks()
        {
            this.PopulationPacks.Clear();
        }

        public void ClearRouteways()
        {
            if (this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture != this)
                    {
                        foreach (Routeway routeway in architecture.HasRoutewayList(this))
                        {
                            routeway.RemoveAfterClose = true;
                        }
                    }
                }
            }
            foreach (Routeway routeway in this.Routeways.GetList())
            {
                Session.Current.Scenario.RemoveRouteway(routeway);
            }
            this.Routeways.Clear();
        }

        /*
        public void ClearSpyPacks()
        {
            this.SpyPacks.Clear();
        }
        */

        private void ClearWork()
        {
            if (this.Agriculture >= this.AgricultureCeiling)
            {
                foreach (Person person in this.AgricultureWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }
            if (this.Commerce >= this.CommerceCeiling)
            {
                foreach (Person person in this.CommerceWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }
            if (this.Technology >= this.TechnologyCeiling)
            {
                foreach (Person person in this.TechnologyWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }

            if (this.Domination >= this.DominationCeiling)
            {
                foreach (Person person in this.DominationWorkingPersons)
                {
                    //this.RemovePersonFromWorkingList(person);
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }


            if (this.Morale >= this.MoraleCeiling)
            {
                foreach (Person person in this.MoraleWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }
            if (this.Endurance >= this.EnduranceCeiling)
            {
                foreach (Person person in this.EnduranceWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }

            foreach (Military military in this.Militaries)
            {
                if (military.Quantity >= military.Kind.MaxScale || this.Domination < 50 || this.Morale < 100)
                {
                    military.StopRecruitment();

                }
            }

            if (suoyouJunduiDouYijingXunlianHao())
            {
                foreach (Person person in this.TrainingWorkingPersons)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }

        }
        private bool suoyouJunduiDouYijingXunlianHao()
        {
            bool JunduiDouYijingXunlianHao = true;
            foreach (Military military in this.Militaries)
            {
                if (military.Morale < military.MoraleCeiling || military.Combativity < military.CombativityCeiling)
                {
                    JunduiDouYijingXunlianHao = false;
                    break;
                }
            }
            return JunduiDouYijingXunlianHao;
        }

        public void CloseAllRouteways()
        {
            foreach (Routeway routeway in this.Routeways.GetList())
            {
                routeway.Close();
            }
        }

        public bool CommerceAvail()
        {
            return (_architectureKind.HasCommerce && this.HasPerson());
        }

        // ===================================================================
        // 缓存相关字段
        // ===================================================================
        private GameArea _cachedConvinceArea = null;
        private GameArea _cachedDestroyArea = null;
        private bool _convinceAreaCacheValid = false;
        private bool _destroyAreaCacheValid = false;
        private int _lastCacheUpdateTurn = -1;

        /// <summary>
        /// 优化后的说服可用性检查 - 避免每次遍历所有城市
        /// </summary>
        public bool ConvincePersonAvail()
        {
            if (!this.HasPerson() || this.Fund < this.ConvincePersonFund)
            {
                return false;
            }
            
            return this.HasAnyConvinceTarget();
        }

        /// <summary>
        /// 检查是否存在可说服目标
        /// </summary>
        public bool HasAnyConvinceTarget()
        {
            return this.BelongedFaction.HasGlobalConvinceTarget();
        }
        

        public Legion CreateDefensiveLegion()
        {
            // 🔥 重构：使用统一的军团创建入口
            // 日期：2026-03-09
            this.DefensiveLegion = this.GetOrCreateDefensiveLegion();
            
            // 🔥 自动分配军团角色
            if (this.DefensiveLegion.Troops != null && this.DefensiveLegion.Troops.Count > 0)
            {
                WorldOfTheThreeKingdoms.GameGlobal.AIRoleSelector.UpdateLegionRoles(this.DefensiveLegion);
            }
            
            return this.DefensiveLegion;
        }

        public Legion ResolveDefensiveLegion()
        {
            if (this.DefensiveLegion != null &&
                this.BelongedFaction != null &&
                this.DefensiveLegion.BelongedFaction == this.BelongedFaction &&
                this.DefensiveLegion.IsDefensive() &&
                (this.DefensiveLegion.Target == this || this.DefensiveLegion.StartArchitecture == this))
            {
                this.DefensiveLegionID = this.DefensiveLegion.ID;
                return this.DefensiveLegion;
            }

            Legion defensiveLegion = null;
            if (this.BelongedFaction != null)
            {
                foreach (Legion legion in this.BelongedFaction.Legions)
                {
                    if (!legion.IsDefensive())
                    {
                        continue;
                    }

                    if (legion.Target == this)
                    {
                        defensiveLegion = legion;
                        break;
                    }
                }

                if (defensiveLegion == null)
                {
                    foreach (Legion legion in this.BelongedFaction.Legions)
                    {
                        if (!legion.IsDefensive())
                        {
                            continue;
                        }

                        if (legion.StartArchitecture == this)
                        {
                            defensiveLegion = legion;
                            break;
                        }
                    }
                }
            }

            this.DefensiveLegion = defensiveLegion;
            this.DefensiveLegionID = defensiveLegion != null ? defensiveLegion.ID : -1;
            return defensiveLegion;
        }

        public Legion GetOrCreateDefensiveLegion()
        {
            Legion defensiveLegion = this.ResolveDefensiveLegion();
            if (defensiveLegion != null)
            {
                return defensiveLegion;
            }

            defensiveLegion = this.BelongedFaction.GetOrCreateLegion(this, LegionKind.AI, LegionMission.Defend);
            this.DefensiveLegion = defensiveLegion;
            this.DefensiveLegionID = defensiveLegion.ID;
            return defensiveLegion;
        }

        /*
        private SpyMessage CreateHireNewPersonSpyMessage(Person person)
        {
            SpyMessage message = new SpyMessage();
            message.Scenario = Session.Current.Scenario;
            message.ID = message.Scenario.SpyMessages.GetFreeGameObjectID();
            message.Kind = SpyMessageKind.HireNewPerson;
            message.MessageFaction = this.BelongedFaction;
            message.MessageArchitecture = this;
            message.Message1 = this.BelongedFaction.Name;
            message.Message2 = base.Name;
            message.Message3 = person.Name;
            message.Message4 = Session.Current.Scenario.Date.ToDateString();
            message.Scenario.SpyMessages.AddMessageWithEvent(message);
            foreach (SpyPack pack in this.SpyPacks)
            {
                int singleWayDays = Session.Current.Scenario.GetSingleWayDays(pack.SpyPerson.Position, this.ArchitectureArea);
                message.AddPersonPack(pack.SpyPerson, singleWayDays);
            }
            return message;
        }*/

        public Military CreateMilitary(MilitaryKind mk)
        {
            Military military = Military.Create(this, mk);
            if (this.OnMilitaryCreate != null)
            {
                this.OnMilitaryCreate(this, military);
            }
            /*
            if (this.HasSpy)
            {
                this.AddMessageToTodayNewMilitarySpyMessage(military);
            }
            */
            return military;
        }
        /*
        private SpyMessage CreateMilitaryScaleSpyMessage(Military m)
        {
            SpyMessage message = new SpyMessage();
            message.Scenario = Session.Current.Scenario;
            message.ID = message.Scenario.SpyMessages.GetFreeGameObjectID();
            message.Kind = SpyMessageKind.MilitaryScale;
            message.MessageFaction = this.BelongedFaction;
            message.MessageArchitecture = this;
            message.Message1 = this.BelongedFaction.Name;
            message.Message2 = base.Name;
            message.Message3 = m.Name;
            message.Message4 = Session.Current.Scenario.Date.ToDateString();
            message.Message5 = (m.Scales * m.Kind.MinScale).ToString();
            message.Scenario.SpyMessages.AddMessageWithEvent(message);
            foreach (SpyPack pack in this.SpyPacks)
            {
                int singleWayDays = Session.Current.Scenario.GetSingleWayDays(pack.SpyPerson.Position, this.ArchitectureArea);
                message.AddPersonPack(pack.SpyPerson, singleWayDays);
            }
            return message;
        }

        private SpyMessage CreateNewFacilitySpyMessage(FacilityKind fk)
        {
            SpyMessage message = new SpyMessage();
            message.Scenario = Session.Current.Scenario;
            message.ID = message.Scenario.SpyMessages.GetFreeGameObjectID();
            message.Kind = SpyMessageKind.NewFacility;
            message.MessageFaction = this.BelongedFaction;
            message.MessageArchitecture = this;
            message.Message1 = this.BelongedFaction.Name;
            message.Message2 = base.Name;
            message.Message3 = fk.Name;
            message.Message4 = Session.Current.Scenario.Date.ToDateString();
            message.Scenario.SpyMessages.AddMessageWithEvent(message);
            foreach (SpyPack pack in this.SpyPacks)
            {
                int singleWayDays = Session.Current.Scenario.GetSingleWayDays(pack.SpyPerson.Position, this.ArchitectureArea);
                message.AddPersonPack(pack.SpyPerson, singleWayDays);
            }
            return message;
        }

        private SpyMessage CreateNewMilitarySpyMessage(Military m)
        {
            SpyMessage message = new SpyMessage();
            message.Scenario = Session.Current.Scenario;
            message.ID = message.Scenario.SpyMessages.GetFreeGameObjectID();
            message.Kind = SpyMessageKind.NewMilitary;
            message.MessageFaction = this.BelongedFaction;
            message.MessageArchitecture = this;
            message.Message1 = this.BelongedFaction.Name;
            message.Message2 = base.Name;
            message.Message3 = m.Name;
            message.Message4 = Session.Current.Scenario.Date.ToDateString();
            message.Scenario.SpyMessages.AddMessageWithEvent(message);
            foreach (SpyPack pack in this.SpyPacks)
            {
                int singleWayDays = Session.Current.Scenario.GetSingleWayDays(pack.SpyPerson.Position, this.ArchitectureArea);
                message.AddPersonPack(pack.SpyPerson, singleWayDays);
            }
            return message;
        }

        private SpyMessage CreateNewTroopSpyMessage(Troop t, bool hand)
        {
            SpyMessage message = new SpyMessage();
            message.Scenario = Session.Current.Scenario;
            message.ID = message.Scenario.SpyMessages.GetFreeGameObjectID();
            message.Kind = SpyMessageKind.NewTroop;
            message.MessageFaction = this.BelongedFaction;
            message.MessageArchitecture = this;
            message.Message1 = this.BelongedFaction.Name;
            message.Message2 = base.Name;
            message.Message3 = t.DisplayName;
            message.Message4 = Session.Current.Scenario.Date.ToDateString();
            if (hand)
            {
                message.Message5 = "不明";
            }
            else
            {
                message.Message5 = (t.WillArchitecture != null) ? t.WillArchitecture.Name : "不明";
            }
            message.Scenario.SpyMessages.AddMessageWithEvent(message);
            foreach (SpyPack pack in this.SpyPacks)
            {
                int singleWayDays = Session.Current.Scenario.GetSingleWayDays(pack.SpyPerson.Position, this.ArchitectureArea);
                message.AddPersonPack(pack.SpyPerson, singleWayDays);
            }
            return message;
        }
        */

        public Legion CreateOffensiveLegion(Architecture willArchitecture)
        {
            // 🔧 修复：检查是否满足创建进攻军团的条件
            // 不应该创建会立即撤退的进攻军团
            
            // 检查是否有足够的部队
            if (this.Militaries.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateOffensiveLegion] {this.Name} 没有可用编队，无法创建进攻军团");
                return null;
            }
            
            // 检查士气是否足够（撤退阈值是45，我们要求至少60）
            bool hasGoodMorale = false;
            foreach (Military m in this.Militaries)
            {
                if (m.Morale >= 60)
                {
                    hasGoodMorale = true;
                    break;
                }
            }
            
            if (!hasGoodMorale)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateOffensiveLegion] {this.Name} 所有编队士气不足60，无法创建进攻军团");
                return null;
            }
            
            // 检查规模是否足够（撤退阈值是5，我们要求至少8）
            bool hasGoodScale = false;
            foreach (Military m in this.Militaries)
            {
                if (m.Scales >= 8)
                {
                    hasGoodScale = true;
                    break;
                }
            }
            
            if (!hasGoodScale)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateOffensiveLegion] {this.Name} 所有编队规模不足8，无法创建进攻军团");
                return null;
            }
            
            // 🔥 重构：使用新的CreateLegion方法
            Legion legion = this.BelongedFaction.GetOrCreateLegion(willArchitecture, LegionKind.AI, LegionMission.Attack);
            if (legion != null)
            {
                legion.StartArchitecture = this;
            }
            LinkNode node = null;
            if (this.AIAllLinkNodes.TryGetValue(willArchitecture.ID, out node))
            {
                legion.PreferredRouteway = this.GetRouteway(node, false);
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateOffensiveLegion] {this.Name} 成功创建进攻军团 -> {willArchitecture?.Name}");
            
            // 🔥 自动分配军团角色（创建时可能还没有部队，所以这里不调用）
            // 角色分配会在部队加入军团后自动触发
            
            return legion;
        }

        public void CreateRobberTroop(Point position)
        {
            Military military = new Military();
            military.ID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
            Session.Current.Scenario.Militaries.AddMilitary(military);
            military.Kind = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(0x15);
            military.Name = military.Kind.Name;
            military.Morale = military.MoraleCeiling;
            military.Combativity = military.CombativityCeiling;
            
            // 🔥 修复：添加边界检查，防止计算溢出
            int populationFactor = Math.Min(this.Population / 100, 10000); // 限制人口因子最大10000
            int areaFactor = Math.Min((this.AreaCount / 2) + 2, 100);      // 限制区域因子最大100
            int baseQuantity = military.Kind.MinScale + populationFactor;
            
            // 防止中间计算溢出
            long calculatedQuantity = (long)baseQuantity * areaFactor;
            
            // 限制在合理范围内
            if (calculatedQuantity > military.Kind.MaxScale)
            {
                military.Quantity = military.Kind.MaxScale;
                System.Diagnostics.Debug.WriteLine($"[CreateRobberTroop] {this.Name} 兵力计算溢出：{calculatedQuantity} -> {military.Kind.MaxScale}（人口:{this.Population}，区域:{this.AreaCount}）");
            }
            else if (calculatedQuantity < military.Kind.MinScale)
            {
                military.Quantity = military.Kind.MinScale;
            }
            else
            {
                military.Quantity = (int)calculatedQuantity;
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateRobberTroop] {this.Name} 创建盗贼部队，兵力:{military.Quantity}（人口:{this.Population}，区域:{this.AreaCount}）");
            
            GameObjectList persons = new GameObjectList();
            Person gameObject = Session.Current.Scenario.Persons.GetGameObject(0x1bc4) as Person;
            persons.Add(gameObject);
            Troop troop = this.CreateTroop(persons, gameObject, military, 0, position);
            troop.WillArchitecture = this;
            this.RobberTroop = troop;
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseCreateRobberTroop(Session.Current.Scenario, this, troop);
        }

        public Routeway CreateRouteway(Point p)
        {
            if (Session.Current.Scenario.GetTerrainDetailByPosition(p) != null)
            {
                Routeway routeway = new Routeway();
                routeway.ID = Session.Current.Scenario.Routeways.GetFreeGameObjectID();
                Session.Current.Scenario.Routeways.AddRoutewayWithEvent(routeway);
                this.BelongedFaction.AddRouteway(routeway);
                routeway.StartArchitecture = this;
                this.Routeways.Add(routeway);
                routeway.Extend(p);
                ArchitectureList routewayArchitecturesByPosition = Session.Current.Scenario.GetRoutewayArchitecturesByPosition(routeway, p);
                if (routewayArchitecturesByPosition.Count > 0)
                {
                    if (routewayArchitecturesByPosition.Count > 1)
                    {
                        routewayArchitecturesByPosition.PropertyName = "Food";
                        routewayArchitecturesByPosition.IsNumber = true;
                        routewayArchitecturesByPosition.SmallToBig = true;
                        routewayArchitecturesByPosition.ReSort();
                    }
                    routeway.EndArchitecture = routewayArchitecturesByPosition[0] as Architecture;
                    routeway.DestinationArchitecture = routeway.EndArchitecture;
                }
                ExtensionInterface.call("CreateRouteway", new Object[] { Session.Current.Scenario, this, routeway });
                return routeway;
            }
            return null;
        }

        public Routeway CreateRouteway(List<Point> pointlist)
        {
            int num2;
            Routeway routeway = new Routeway();
            routeway.ID = Session.Current.Scenario.Routeways.GetFreeGameObjectID();
            if (Session.GlobalVariables.LiangdaoXitong)
            {
                Session.Current.Scenario.Routeways.AddRoutewayWithEvent(routeway);
                this.BelongedFaction.AddRouteway(routeway);
            }
            else
            {
                routeway.BelongedFaction = this.BelongedFaction;
            }
            routeway.StartArchitecture = this;
            this.Routeways.Add(routeway);
            GameArea routewayStartPoints = this.GetRoutewayStartPoints();
            int num = 0;
            for (num2 = 0; num2 < pointlist.Count; num2++)
            {
                if (routewayStartPoints.HasPoint(pointlist[num2]))
                {
                    num = num2;
                }
            }
            for (num2 = num; num2 < pointlist.Count; num2++)
            {
                routeway.Extend(pointlist[num2]);
            }
            ExtensionInterface.call("CreateRouteway", new Object[] { Session.Current.Scenario, this, routeway });
            return routeway;
        }

        public Troop CreateTroop(GameObjectList persons, Person leader, Military military, int food, Point position, Legion assignedLegion = null, bool silent = false, bool playerManual = false)
        {
            return Troop.Create(this, persons, leader, military, food, position, assignedLegion, silent, false, playerManual);
        }

        public bool CurrentPlayerOwned()
        {
            return ((Session.GlobalVariables.SkyEye && this.HasFaction()) || (this.HasFaction() && (Session.Current.Scenario.NoCurrentPlayer || (this.BelongedFaction == Session.Current.Scenario.CurrentPlayer))));
        }

        public void DamageByGossip(int damage)
        {
            foreach (Person person in this.Persons)
            {
                if (person != this.BelongedFaction.Leader)
                {
                    if (person.TempLoyaltyChange > -20)
                    {
                        person.TempLoyaltyChange -= (StaticMethods.GetRandomValue((int)(damage * (int)(Enum.GetNames(typeof(PersonLoyalty)).Length - person.PersonalLoyalty) * (Math.Min(person.Loyalty, 100) / 100.0)), 100));
                    }
                }
            }
            ExtensionInterface.call("GossipDamage", new Object[] { Session.Current.Scenario, this, damage });
        }

        public void checkEvent()
        {
            // 🔥 AOT修复：直接使用 EventList，避免通过 GetList() 导致的类型信息丢失
            EventList eventList = Session.Current.Scenario.AllEvents;
            eventList.PropertyName = "ID";
            eventList.SmallToBig = true;
            eventList.IsNumber = true;
            eventList.ReSort();

            // 🔥 AOT修复：直接访问 GameObjects 属性并进行安全的类型转换
            foreach (GameObject obj in eventList.GameObjects)
            {
                Event e = (obj is Event ? (Event)obj : null);
                if (e == null)
                {
                    // 如果转换失败，跳过该对象
                    continue;
                }
                
                if (e.checkConditions(this))
                {
                    if (!Session.Current.Scenario.EventsToApply.ContainsKey(e))
                    {
                        Session.Current.Scenario.EventsToApply.Add(e, this);
                        e.ApplyEventDialogs(this, Session.MainGame.mainGameScreen);
                    }
                    if (!Session.Current.Scenario.YesEventsToApply.ContainsKey(e) && e.yesEffect.Count > 0)
                    {
                        Session.Current.Scenario.YesEventsToApply.Add(e, this);
                    }
                    if (!Session.Current.Scenario.NoEventsToApply.ContainsKey(e) && e.noEffect.Count > 0)
                    {
                        Session.Current.Scenario.NoEventsToApply.Add(e, this);
                    }
                    /*
                    if (!Session.Current.Scenario.YesArchiEventsToApply.ContainsKey(e))
                    {
                        Session.Current.Scenario.YesArchiEventsToApply.Add(e, this);
                        e.ApplyEventDialogs(this);
                    }
                    if (!Session.Current.Scenario.NoArchiEventsToApply.ContainsKey(e))
                    {
                        Session.Current.Scenario.NoArchiEventsToApply.Add(e, this);
                        e.ApplyEventDialogs(this);
                    }*/
                }
            }
        }

        private void PersonExperienceIncrease()
        {
            foreach (Person p in this.Persons)
            {
                p.CommandExperience += this.CommandExperienceIncrease;
                p.StrengthExperience += this.StrengthExperienceIncrease;
                p.IntelligenceExperience += this.IntelligenceExperienceIncrease;
                p.PoliticsExperience += this.PoliticsExperienceIncrease;
                p.GlamourExperience += this.GlamourExperienceIncrease;
                p.Reputation += this.ReputationIncrease;
            }
        }

        // 🔥 递归检测：防止 DayEvent 被事件触发导致无限递归
        // 日期：2026-03-19
        private bool _isInDayEvent = false;
        
        public void DayEvent()
        {
            // 🔥 关键：检测递归调用
            // 如果 DayEvent 已经在执行中，说明事件触发导致了递归
            if (_isInDayEvent)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"⚠️ [DayEvent] 检测到递归调用！建筑：{this.Name}(ID:{this.ID})");
                System.Diagnostics.Debug.WriteLine($"   堆栈跟踪：{Environment.StackTrace}");
                #endif
                
                // ANTI-BAND-AID：不吞掉异常，让问题暴露
                throw new InvalidOperationException(
                    $"检测到 DayEvent 递归调用！建筑：{this.Name}(ID:{this.ID})。" +
                    $"这通常是由事件触发导致的，请检查 IncreaseFood/IncreaseFund/ReceivePopulation 等方法的事件处理器。");
            }
            
            try
            {
                _isInDayEvent = true;
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name}(ID:{this.ID}) 开始");
                #endif
                */
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - RefreshNeutralBuilding");
                #endif
                */
                this.RefreshNeutralBuilding(); //  加buff中立建筑
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - FundPacksDayEvent");
                #endif
                */
                this.FundPacksDayEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - FoodPacksDayEvent");
                #endif
                */
                this.FoodPacksDayEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - PopulationPacksDayEvent");
                #endif
                */
                this.PopulationPacksDayEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - characteristicsDoWork");
                #endif
                */
                this.characteristicsDoWork();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - InformationDayEvent");
                #endif
                */
                this.InformationDayEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - HandleFacilities");
                #endif
                */
                this.HandleFacilities();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - ViewAreaEvent");
                #endif
                */
                this.ViewAreaEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - StrategicCenterEffect");
                #endif
                */
                this.StrategicCenterEffect();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - AutoDecrement");
                #endif
                */
                this.AutoDecrement();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - AutoIncrement");
                #endif
                */
                this.AutoIncrement();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - Sourrounded");
                #endif
                */
                this.Sourrounded();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - ResetDayInfluence");
                #endif
                */
                this.ResetDayInfluence();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - CheckRobberTroop");
                #endif
                */
                this.CheckRobberTroop();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - AlienTroopGain");
                #endif
                */
                this.AlienTroopGain();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - PopulationEscapeEvent");
                #endif
                */
                this.PopulationEscapeEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - FoodReduce");
                #endif
                */
                this.FoodReduce();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - RestEvent");
                #endif
                */
                this.RestEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - zainanshijian");
                #endif
                */
                this.zainanshijian();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - captiveEscape");
                #endif
                */
                this.captiveEscape();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - PersonExperienceIncrease");
                #endif
                */
                this.PersonExperienceIncrease();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - checkEvent");
                #endif
                */
                this.checkEvent();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - 清理标志位");
                #endif
                */
                this.JustAttacked = false;
                ExpectedFoodCache = -1;
                ExpectedFundCache = -1;
                this.SuspendTroopTransfer--;

                if (this.Mayor != null)
                {
                    this.MayorOnDutyDays++;
                    this.MayorOnDutyDays += Session.Parameters.DayInTurn;
                }

                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name} - resolveAIQuickBattle");
                #endif
                */
                this.resolveAIQuickBattle();
                
                /*
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DayEvent] {this.Name}(ID:{this.ID}) 完成");
                #endif
                */
            }
            finally
            {
                _isInDayEvent = false;
            }
        }

        private void resolveAIQuickBattle()
        {

            foreach (Architecture a in this.AIBattlingArchitectures.GetList())
            {
                bool aborted = false;

                // === 可视性判定（循环外计算一次）===
                bool isOnScreen = Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(this.Position) || 
                                  Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(a.Position);
                // 观察者模式 = 无当前玩家 或 天眼开启
                bool shouldShowRealBattle = isOnScreen && (Session.Current.Scenario.NoCurrentPlayer || Session.GlobalVariables.SkyEye);

                // === 检查是否已有部队针对此目标（防止重复创建）===
                Legion existingLegion = this.BelongedFaction.GetLegion(a);
                bool troopsAlreadyExist = existingLegion != null && existingLegion.Troops.Count > 0;

                // --- 情况1：观察者正在看 且 部队已存在 -> 唤醒为实体战斗 ---
                if (shouldShowRealBattle && troopsAlreadyExist)
                {
                    // --- A. 唤醒进攻方（使用螺旋散布防止堆叠）---
                    int attackerIndex = 0;
                    foreach (Troop t in existingLegion.Troops)
                    {
                        // 🔥 只在第一次唤醒时执行瞬移，避免每帧重复定位导致闪烁
                        if (t.QuickBattling)
                        {
                            t.QuickBattling = false; 
                            
                            // 🔧 FIX: 确保进攻方不会把己方城市设为目标
                            if (a.BelongedFaction == t.BelongedFaction)
                            {
                                // 错误！进攻方不应该攻击己方城市，跳过此部队
                                System.Diagnostics.Debug.WriteLine($"[resolveAIQuickBattle] ⚠️ 跳过：进攻方{t.DisplayName}的目标{a.Name}属于己方{a.BelongedFaction?.Name}");
                                continue;
                            }
                            
                            t.TargetArchitecture = a;
                            t.WillArchitecture = a;

                            // 【核心修复：虚空瞬移 + 螺旋散布】距离太远则拉到城下
                            double dist = Math.Sqrt(Math.Pow(t.Position.X - a.Position.X, 2) + Math.Pow(t.Position.Y - a.Position.Y, 2));
                            if (dist > 10) 
                            {
                                // 使用索引生成唯一位置（8方向螺旋扩散）
                                int angle = (attackerIndex * 45) % 360; // 0°, 45°, 90°...
                                int radius = 4 + (attackerIndex / 8);   // 第一圈半径4, 第二圈半径5...
                                int offsetX = (int)(radius * Math.Cos(angle * Math.PI / 180));
                                int offsetY = (int)(radius * Math.Sin(angle * Math.PI / 180));
                                t.Position = new Point(a.Position.X + offsetX, a.Position.Y + offsetY);
                                t.RealDestination = a.Position; 
                            }
                            
                            a.TotalHostileForce -= t.FightingForce;
                        }
                        // 如果已唤醒(QuickBattling==false)，不再重新定位，让AI自然控制移动
                        attackerIndex++;
                    }

                    // --- B. 唤醒防守方（使用螺旋散布防止堆叠）---
                    Legion defensiveLegion = a.ResolveDefensiveLegion();
                    if (defensiveLegion != null)
                    {
                        int defenderIndex = 0;
                        foreach (Troop t in defensiveLegion.Troops)
                        {
                            // 🔥 只在第一次唤醒时执行瞬移
                            if (t.QuickBattling)
                            {
                                t.QuickBattling = false;
                                // 🔧 FIX: 不再设置 WillArchitecture = a，避免防守部队自杀
                                // t.WillArchitecture = a;      // ❌ 已删除
                                // t.RealDestination = t.Position; // ❌ 已删除 
                                
                                // 防守方位置修正（使用螺旋散布）
                                double distDef = Math.Sqrt(Math.Pow(t.Position.X - a.Position.X, 2) + Math.Pow(t.Position.Y - a.Position.Y, 2));
                                if (distDef > 5)
                                {
                                    // 防守方使用较小半径（靠近城池）
                                    int angle = (defenderIndex * 45 + 22) % 360; // 偏移22度，与进攻方错开
                                    int radius = 2 + (defenderIndex / 8);
                                    int defOffsetX = (int)(radius * Math.Cos(angle * Math.PI / 180));
                                    int defOffsetY = (int)(radius * Math.Sin(angle * Math.PI / 180));
                                    t.Position = new Point(a.Position.X + defOffsetX, a.Position.Y + defOffsetY);
                                }

                                this.TotalFriendlyForce -= t.FightingForce;
                            }
                            defenderIndex++;
                        }
                    }

                    aborted = true; // 交给真实战斗系统
                }
                // --- 情况2 已删除：原来的 continue 导致部队根本不被创建 ---
                // 现在改为：在快速战斗分支中，根据 shouldShowRealBattle 决定可见性

                if (!aborted)
                {
                    // === 如果部队已存在，跳过创建步骤，直接进入战斗循环 ===
                    bool skipBuild = troopsAlreadyExist;
                    
                    if (!skipBuild)
                    {
                        // offensive troop
                        int reserve = this.getArmyReserveForOffensive();
                        if (this.ArmyScale < reserve)
                        {
                            this.AIBattlingArchitectures.Remove(a);
                            break;
                        }

                        bool built = this.BuildOffensiveTroop(a, LinkKind.Land, true, reserve);
                        if (!built)
                        {
                            this.AIBattlingArchitectures.Remove(a);
                            break;
                        }
                    }

                    // 获取最新的军团引用（可能刚创建）
                    Legion currentLegion = this.BelongedFaction.GetLegion(a);
                    if (currentLegion == null || currentLegion.Troops.Count == 0)
                    {
                        this.AIBattlingArchitectures.Remove(a);
                        break;
                    }

                    // 🔥 核心修复：根据观察者模式决定部队可见性
                    int newTroopIndex = 0;
                    foreach (Troop t in currentLegion.Troops)
                    {
                        t.TargetArchitecture = null;
                        // 🔧 FIX: 不再清空 WillArchitecture，保持原有目标
                        // t.WillArchitecture = null;  // ❌ 会触发回退到 StartingArchitecture
                        
                        // 🔥 如果观察者正在看，新创建的部队直接设为可见
                        if (shouldShowRealBattle)
                        {
                            t.QuickBattling = false; // 可见
                            t.TargetArchitecture = a; // 战斗目标
                            // 🔧 FIX: 不再设置 WillArchitecture = a，保持原有目标
                            // 这会导致防守方部队错误地以己方城池为目标，触发入城销毁
                            // t.WillArchitecture = a; // ❌ 已删除
                            
                            // 使用螺旋散布放置新部队
                            int angle = (newTroopIndex * 45) % 360;
                            int radius = 4 + (newTroopIndex / 8);
                            int offsetX = (int)(radius * Math.Cos(angle * Math.PI / 180));
                            int offsetY = (int)(radius * Math.Sin(angle * Math.PI / 180));
                            t.Position = new Point(a.Position.X + offsetX, a.Position.Y + offsetY);
                            t.RealDestination = a.Position;
                        }
                        else
                        {
                            // 🔧 FIX: 禁用快速战斗隐身模式，部队始终可见
                            // t.QuickBattling = true; // 隐身模拟
                        }
                        
                        a.TotalHostileForce += t.FightingForce;
                        
                        // 初始化快速战斗移动优化
                        t.InitializeMovementOptimization();
                        newTroopIndex++;
                    }

                    if (a.BelongedFaction != null && !skipBuild)
                    {
                        // defensive troop (只在新创建进攻方时才创建防守方)
                        a.DefensiveCampaign(currentLegion.Troops);
                    }
                    
                    Legion defensiveLegion = a.ResolveDefensiveLegion();
                    if (defensiveLegion != null)
                    {
                        foreach (Troop t in defensiveLegion.Troops)
                        {
                            // 🔧 FIX: 禁用快速战斗隐身模式，部队始终可见
                            // t.QuickBattling = true;
                            this.TotalFriendlyForce += t.FightingForce;
                            
                            // 初始化快速战斗移动优化
                            t.InitializeMovementOptimization();
                        }
                    }

                    // fight
                    if (defensiveLegion != null)
                    {
                        GameObjectList defList = defensiveLegion.Troops.GetList();
                        foreach (Troop t in defList)
                        {
                            if (currentLegion.Troops.Count > 0)
                            {
                                TroopList list = currentLegion.Troops;
                                Troop target = (Troop)list[GameObject.Random(list.Count)];
                                t.AttackTroop(target);
                            }
                            else
                            {
                                this.AIBattlingArchitectures.Remove(a);
                            }
                        }
                    }
                    GameObjectList attackingTroops = currentLegion.Troops.GetList();
                    foreach (Troop t in attackingTroops)
                    {
                        if (a.Endurance > 0)
                        {
                            t.AttackArchitecture(a);
                            if (a.Endurance <= 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (defensiveLegion != null && defensiveLegion.Troops.Count > 0)
                            {
                                Troop target = (Troop)defensiveLegion.Troops[GameObject.Random(defensiveLegion.Troops.Count)];
                                t.AttackTroop(target);
                            }
                            else
                            {
                                t.Position = a.ArchitectureArea.Centre;
                                t.BelongedFaction = this.BelongedFaction;
                                t.Occupy();

                                foreach (Troop u in attackingTroops)
                                {
                                    u.Position = a.ArchitectureArea.Centre;
                                    u.Enter(a);
                                }
                                this.AIBattlingArchitectures.Remove(a);
                                break;
                            }

                        }
                    }
                    int i = 0;
                    foreach (Military m in this.Militaries)
                    {
                        if (this.PersonsExcludeNvGuan.Count > i)
                        {
                            if (GameObject.Chance((m.Kind.OffenceRadius + 1) * (m.Kind.OffenceRadius + 1) * 100 / this.Militaries.Count))
                            {
                                a.Endurance = a.Endurance - Math.Max(1, (int)(m.Offence / 4 * m.Kind.ArchitectureDamageRate * Session.Parameters.ArchitectureDamageRate));
                                if (a.Endurance <= 0) break;
                            }
                        }
                        else
                        {
                            break;
                        }
                        i++;
                    }


                }
                else
                {
                    foreach (Architecture x in this.AIBattlingArchitectures)
                    {
                        x.AIBattlingArchitectures.Remove(this);
                    }
                    this.AIBattlingArchitectures.Clear();
                }
            }
        }

        private void RestEvent()
        {
            foreach (Military m in this.Militaries)
            {
                if (m.Tiredness > 0)
                {
                    m.Tiredness -= Session.GlobalVariables.TirednessDecrease;
                    if (m.Tiredness < 0) m.Tiredness = 0;
                }
            }
            foreach (Person p in this.Persons)
            {
                if (p.Tiredness > 0)
                {
                    p.Tiredness -= Session.GlobalVariables.TirednessDecrease;
                    if (p.Tiredness < 0) p.Tiredness = 0;
                }
            }
            foreach (Person p in this.MovingPersons)
            {
                if (p.Tiredness > 0 && (p.OutsideTask == OutsideTaskKind.宠幸))
                {
                    p.Tiredness -= Session.GlobalVariables.TirednessDecrease;
                    if (p.Tiredness < 0) p.Tiredness = 0;
                }
            }
            foreach (Captive c in this.Captives)
            {
                if (c.CaptivePerson.Tiredness > 0)
                {
                    c.CaptivePerson.Tiredness -= Session.GlobalVariables.TirednessDecrease;
                    if (c.CaptivePerson.Tiredness < 0) c.CaptivePerson.Tiredness = 0;
                }
            }
            foreach (Person p in this.Feiziliebiao)
            {
                if (p.Tiredness > 0)
                {
                    p.Tiredness -= Session.GlobalVariables.TirednessDecrease;
                    if (p.Tiredness < 0) p.Tiredness = 0;
                }
            }
        }

        private void captiveEscape()
        {
            foreach (Captive p in this.Captives.GetRandomList())
            {
                if (GameObject.Random(5) == 0 && !GameObject.Chance(p.CaptivePerson.PersonalLoyalty * 25))
                {
                    if (p.CaptiveFaction != null)
                    {
                        bool pass = true;
                        if (p.CaptivePerson.HasStrainTo(p.CaptiveFaction.Leader) && GameObject.Chance(33))
                        {
                            pass = false;
                        }
                        if (p.CaptivePerson.HasCloseStrainTo(p.CaptiveFaction.Leader) && GameObject.Chance(67))
                        {
                            pass = false;
                        }
                        if (p.CaptivePerson.IsCloseTo(p.CaptiveFaction.Leader) && GameObject.Chance(33))
                        {
                            pass = false;
                        }
                        if (p.CaptivePerson.IsVeryCloseTo(p.CaptiveFaction.Leader) && GameObject.Chance(67))
                        {
                            pass = false;
                        }
                        if (p.CaptivePerson.Hates(p.CaptiveFaction.Leader))
                        {
                            pass = true;
                        }
                        if (pass)
                        {
                            p.CaptivePerson.TempLoyaltyChange--;
                        }
                    }
                    else
                    {
                        p.CaptivePerson.TempLoyaltyChange--;
                    }
                }
                if (GameObject.Random((this.Domination * 10 + this.Morale) * 20) + 200 <= GameObject.Random(p.CaptivePerson.CaptiveAbility))
                {
                    if (!GameObject.Chance(noEscapeChance) || GameObject.Chance(p.CaptivePerson.captiveEscapeChance))
                    {
                        p.CaptiveEscape();
                    }
                }
                if (p.CaptivePerson.ArrivingDays > 0 && (GameObject.Chance(p.CaptivePerson.JailBreakAbility / 100) || GameObject.Chance(p.CaptivePerson.captiveEscapeChance)))
                {
                    p.CaptiveEscape();
                }
            }
        }

        public void tingzhizhenzai()
        {
            foreach (Person person in this.ZhenzaiWorkingPersons)
            {
                person.WorkKind = ArchitectureWorkKind.无;
            }
        }

        private void zainanshijian()
        {
            if (Session.Current.Scenario.DaySince < 720 * Session.Parameters.DayInTurn) return;
            if (this.youzainan)
            {
                //this.DecreaseFood(this.ZhenzaiWorkingPersons.Count * 3000);
                this.DecreaseFund(this.ZhenzaiWorkingPersons.Count * this.InternalFundCost);
                this.zhixingzainanshanghai();

                //this.zainan.shengyutianshu--;
                //this.zainan.shengyutianshu -= this.zhenzaijianshaotianshu();

                this.zainan.shengyutianshu -= Session.Parameters.DayInTurn;
                this.zainan.shengyutianshu -= this.zhenzaijianshaotianshu() * Session.Parameters.DayInTurn;

                if (this.zainan.shengyutianshu <= 0)
                {
                    this.youzainan = false;
                    this.tingzhizhenzai();
                }

                if (this.Food <= 0 || this.Fund <= 0)
                {
                    this.tingzhizhenzai();
                }
            }
            else
            {
                if (GameObject.Random(Session.GlobalVariables.zainanfashengjilv) == 0 && _architectureKind.CountToMerit)
                {
                    int kindID;
                    kindID = GameObject.Random(Session.Current.Scenario.GameCommonData.suoyouzainanzhonglei.Count);

                    bool doDisaster = true;
                    if (disasterChanceDecrease.ContainsKey(kindID))
                    {
                        if (GameObject.Chance(disasterChanceDecrease[kindID]))
                        {
                            doDisaster = false;
                        }
                    }
                    if (disasterChanceIncrease.ContainsKey(kindID))
                    {
                        if (GameObject.Chance(disasterChanceIncrease[kindID]))
                        {
                            doDisaster = true;
                        }
                    }

                    if (doDisaster)
                    {
                        this.zainan.zainanzhonglei = Session.Current.Scenario.GameCommonData.suoyouzainanzhonglei.Getzainanzhonglei(kindID);
                        this.zainan.shengyutianshu = this.zainan.zainanzhonglei.shijianxiaxian + GameObject.Random(this.zainan.zainanzhonglei.shijianshangxian - this.zainan.zainanzhonglei.shijianxiaxian);
                        this.youzainan = true;
                        // 🔥 AOT 重构：使用强类型事件替代反射调用
                        WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseDisasterHappened(Session.Current.Scenario, this, this.zainan.zainanzhonglei.ID);
                        foreach (Military military in this.Militaries)//发生灾难时不能补充
                        {
                            military.StopRecruitment();
                        }
                        this.Onfashengzainan(this, this.zainan.zainanzhonglei.ID);
                    }
                }
            }
        }

        private int zhenzaijianshaotianshu()
        {
            int tianshu;
            int zhenzainenglizonghe = 0;
            foreach (Person person in this.ZhenzaiWorkingPersons)
            {
                zhenzainenglizonghe += person.zhenzaiAbility;
            }
            float extraProb = (zhenzainenglizonghe % 3000) / 30.0f;
            tianshu = zhenzainenglizonghe / 3000;
            return tianshu + (GameObject.Chance((int)extraProb) ? 1 : 0);
        }

        private float jianzaixishu()
        {
            float xishu;
            int zhenzainenglizonghe = 0;
            foreach (Person person in this.ZhenzaiWorkingPersons)
            {
                zhenzainenglizonghe += person.zhenzaiAbility;
            }
            xishu = 500.0f / zhenzainenglizonghe;
            if (xishu < 0.01f)
            {
                xishu = 0.01f;
            }
            if (xishu > 1f)
            {
                xishu = 1f;
            }
            return xishu;
        }

        private void zhixingzainanshanghai()
        {
            float rate = 1;
            if (disasterDamageRateDecrease.ContainsKey(this.zainan.ID))
            {
                rate = (float)(rate - disasterDamageRateDecrease[this.zainan.ID] / 100.0);
                if (rate < 0) rate = 0;
            }
            this.DecreasePopulation((int)(this.zainan.zainanzhonglei.renkoushanghai * jianzaixishu() * rate));
            this.DecreaseDomination((int)(this.zainan.zainanzhonglei.tongzhishanghai * rate));
            this.xiajiangnaijiu((int)(this.zainan.zainanzhonglei.naijiushanghai * rate));
            this.DecreaseAgriculture((int)(this.zainan.zainanzhonglei.nongyeshanghai * rate));
            this.DecreaseCommerce((int)(this.zainan.zainanzhonglei.shangyeshanghai * rate));
            this.DecreaseTechnology((int)(this.zainan.zainanzhonglei.jishushanghai * rate));
            this.DecreaseMorale((int)(this.zainan.zainanzhonglei.minxinshanghai * jianzaixishu() * rate));
            this.DecreaseFood((int)(this.zainan.zainanzhonglei.FoodDamage * jianzaixishu() * rate));
            this.DecreaseFund((int)(this.zainan.zainanzhonglei.FundDamage * jianzaixishu() * rate));
            if (this.zainan.zainanzhonglei.TroopDamage > 0)
            {
                foreach (Military m in this.Militaries)
                {
                    int loseTroop = Math.Min(m.Quantity, (int)(this.zainan.zainanzhonglei.TroopDamage * jianzaixishu() * rate));
                    m.DecreaseQuantity(loseTroop);
                    m.IncreaseInjuryQuantity(loseTroop * m.Kind.InjuryChance / 100);
                    m.DecreaseMorale(loseTroop / 100);
                    m.DecreaseCombativity(loseTroop / 100);
                }
            }
            if (this.zainan.zainanzhonglei.OfficerDamage > 0)
            {
                foreach (Person p in this.Persons)
                {
                    while (GameObject.Chance(this.zainan.zainanzhonglei.OfficerDamage))
                    {
                        p.InjureRate *= 0.85f;
                        Session.MainGame.mainGameScreen.OnOfficerSick(p);
                        if (p.InjureRate < 0.05 && Session.GlobalVariables.OfficerDieInBattleRate > 0)
                        {
                            p.ToDeath(null, this.BelongedFaction);
                        }
                    }
                }
            }
        }

        public void DecreaseAgriculture(int decrement)
        {
            this.Agriculture -= decrement;
            if (this.Agriculture < 0)
            {
                this.Agriculture = 0;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            this.InvalidateInfluenceCache();
        }

        public void DecreaseCommerce(int decrement)
        {
            this.Commerce -= decrement;
            if (this.Commerce < 0)
            {
                this.Commerce = 0;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            this.InvalidateInfluenceCache();
        }

        public int DecreaseDomination(int decrement)
        {
            int domination = decrement;
            if ((this.Domination - decrement) < 0)
            {
                domination = this.Domination;
            }
            this.Domination -= domination;
            return domination;
        }

        public void xiajiangnaijiu(int decrement)  //灾难下降耐久
        {
            this.Endurance -= decrement;
            if (this.Endurance < 0)
            {
                this.Endurance = 0;
            }
        }

        public int DecreaseEndurance(int decrement)
                {
                    if (this.Endurance <= 0)
                    {
                        return 0;
                    }

                    int endurance = decrement;
                    if ((this.Endurance - decrement) < 0)
                    {
                        endurance = this.Endurance;
                    }
                    this.Endurance -= endurance;
                    this.SetRecentlyAttacked();
                    this.DecreaseFacilityEndurance(endurance);
                    if (this.Endurance == 0)
                    {
                        this.RecentlyBreaked = 30;
                        this.WallStateChange();
                    }

                    // 🆕 内政变化时清除势力范围缓存（2026-03-11）
                    if (endurance > 0)
                    {
                        this.InvalidateInfluenceCache();
                    }

                    return endurance;
                }


        public void DecreaseFacilityEndurance(int decrement)
        {
            if (decrement > 0)
            {
                this.Facilities.DecreaseEndurance((int)(decrement * this.RateOfFacilityEnduranceDown));
                foreach (Facility facility in this.Facilities.GetList())
                {
                    if (facility.Endurance <= 0)
                    {
                        this.DemolishFacility(facility);
                    }
                }
            }
        }

        public void DecreaseFood(int decrement)
        {
            this.food -= decrement;
            if (this.food < 0)
            {
                this.food = 0;
            }
        }

        public void DecreaseFund(int decrement)
        {
            this.fund -= decrement;
            if (this.fund < 0)
            {
                this.fund = 0;
            }
        }

        public void DecreaseMorale(int decrement)
        {
            this.Morale -= decrement;
            if (this.Morale < 0)
            {
                this.Morale = 0;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            this.InvalidateInfluenceCache();
        }

        public int DecreasePopulation(int decrement)
        {
            if (this.population < decrement)
            {
                decrement = this.population;
            }
            this.population -= decrement;
            return decrement;
        }

        public int DecreaseMilitaryPopulation(int decrement)
        {
            if (this.MilitaryPopulation < decrement)
            {
                decrement = this.MilitaryPopulation;
            }
            this.MilitaryPopulation -= decrement;
            return decrement;
        }


        public void DecreaseTechnology(int decrement)
        {
            this.Technology -= decrement;
            if (this.Technology < 0)
            {
                this.Technology = 0;
            }
        }

        private PersonList AISelectPersonIntoTroop_inner(Person leader, PersonList otherPersons, bool markSelected)
        {
            PersonList persons = new PersonList();
            persons.Add(leader);
            if (markSelected)
            {
                leader.Selected = true;
            }
            return persons;
        }

        private bool isPersonAllowedIntoTroop(Person person, Military military, bool offensive)
        {
            bool r = person.LocationArchitecture == this && !person.TooTiredToBattle && GameObject.Random(person.Tiredness / 5) == 0 && (person.Command >= military.Kind.MinCommand);
            foreach (KeyValuePair<Condition, float> c in military.Kind.AILeaderConditionWeight)
            {
                if (c.Key.CheckCondition(person) && c.Value <= 0)
                {
                    return false;
                }
            }
            return r;
        }

        private TroopList AISelectPersonIntoTroop(Architecture from, Military military, bool offensive, Architecture targetArchitecture = null)
        {
            TroopList result = new TroopList();
            if (military.FollowedLeader != null && from.PersonsExcludeNvGuan.HasGameObject(military.FollowedLeader) && military.FollowedLeader.LocationTroop == null
                && isPersonAllowedIntoTroop(military.FollowedLeader, military, offensive))
            {
                result.Add(Troop.CreateSimulateTroop(this.AISelectPersonIntoTroop_inner(military.FollowedLeader, from.PersonsExcludeNvGuan, true), military, from.Position));
            }
            else if (military.Leader != null && military.LeaderExperience >= 10 && (military.Leader.Strength >= 80 || military.Leader.Command >= 80 || military.Leader.HasLeaderValidTitle)
                && from.PersonsExcludeNvGuan.HasGameObject(military.Leader) && military.Leader.LocationTroop == null && isPersonAllowedIntoTroop(military.Leader, military, offensive)
               )
            {
                result.Add(Troop.CreateSimulateTroop(this.AISelectPersonIntoTroop_inner(military.Leader, from.PersonsExcludeNvGuan, true), military, from.Position));
            }
            else
            {
                GameObjectList pl = from.PersonsExcludeNvGuan.GetList();
                pl.PropertyName = "FightingForce";
                pl.IsNumber = true;
                pl.SmallToBig = false;
                pl.ReSort();
                foreach (Person person in pl)
                {
                    if (!person.Selected && (!offensive || isPersonAllowedIntoTroop(person, military, offensive)))
                    {
                        if (person.HasMilitaryKindTitle(military.Kind))
                        {
                            result.Add(Troop.CreateSimulateTroop(this.AISelectPersonIntoTroop_inner(person, from.PersonsExcludeNvGuan, false), military, from.Position));
                        }
                        else if (person.HasMilitaryTypeTitle(military.Kind.Type))
                        {
                            result.Add(Troop.CreateSimulateTroop(this.AISelectPersonIntoTroop_inner(person, from.PersonsExcludeNvGuan, false), military, from.Position));
                        }
                        else if ((this.BelongedFaction.AvailableMilitaryKinds.GetMilitaryKindList().GameObjects.Contains(military.Kind) && military.Kind.RecruitLimit > 10) ||
                            person.FightingForce >= Session.Parameters.AIUniqueTroopFightingForceThreshold || (this.Endurance < 30 && !offensive))
                        {
                            result.Add(Troop.CreateSimulateTroop(this.AISelectPersonIntoTroop_inner(person, from.PersonsExcludeNvGuan, false), military, from.Position));
                        }
                    }
                }
            }

            RankTroopCandidatesByCommanderEnergy(result, offensive, from, targetArchitecture);
            return result;
        }

        private static void RankTroopCandidatesByCommanderEnergy(
            TroopList candidates,
            bool offensive,
            Architecture sourceArchitecture,
            Architecture targetArchitecture)
        {
            if (candidates == null || candidates.Count < 2) return;
            if (candidates.GameObjects == null || candidates.GameObjects.Count < 2) return;

            List<GameObject> candidateObjects = candidates.GameObjects;
            for (int i = 1; i < candidateObjects.Count; i++)
            {
                GameObject key = candidateObjects[i];
                Troop keyTroop = key as Troop;
                if (keyTroop == null)
                {
                    throw new InvalidOperationException(
                        $"[RankTroopCandidatesByCommanderEnergy] 候选列表存在非 Troop 对象：{key?.GetType().Name ?? "null"}");
                }

                int keyScore = CommanderEnergyScoring.CalculateCandidateScore(
                    keyTroop,
                    offensive,
                    sourceArchitecture,
                    targetArchitecture);

                int j = i - 1;
                while (j >= 0)
                {
                    Troop currentTroop = candidateObjects[j] as Troop;
                    if (currentTroop == null)
                    {
                        throw new InvalidOperationException(
                            $"[RankTroopCandidatesByCommanderEnergy] 候选列表存在非 Troop 对象：{candidateObjects[j]?.GetType().Name ?? "null"}");
                    }

                    int currentScore = CommanderEnergyScoring.CalculateCandidateScore(
                        currentTroop,
                        offensive,
                        sourceArchitecture,
                        targetArchitecture);
                    if (currentScore >= keyScore)
                    {
                        break;
                    }

                    candidateObjects[j + 1] = candidateObjects[j];
                    j--;
                }

                candidateObjects[j + 1] = key;
            }
        }

        private void DefensiveCampaign(TroopList quickBattleList)
        {
            DateTime beforeStart = DateTime.UtcNow;

            List<Point> orientations = new List<Point>();
            TroopList hostileTroopsInView;

            if (quickBattleList == null)
            {
                hostileTroopsInView = this.GetHostileTroopsInView();
                if (hostileTroopsInView.Count <= 0) return;
            }
            else
            {
                hostileTroopsInView = quickBattleList;
            }

            foreach (Troop troop in hostileTroopsInView)
            {
                orientations.Add(troop.Position);
            }

            if ((this.HasPerson() && this.HasCampaignableMilitary()) && (this.GetAllAvailableArea(false).Count != 0))
            {
                if (hostileTroopsInView.Count > 0)
                {
                    TroopList friendlyTroopsInView = this.GetFriendlyTroopsInView();
                    int troopSent = 0;
                    int militaryCount = this.MilitaryCount;

                    int opFactor = 2;
                    if (this.Food > this.AbundantFood)
                    {
                        opFactor = 5;
                    }
                    else if (this.Food < this.EnoughFood)
                    {
                        opFactor = 1;
                    }

                    if (!(this.Endurance > this.EnduranceCeiling * 0.2f && this.TotalFriendlyForce > this.TotalHostileForce * opFactor && friendlyTroopsInView.Count >= 4))
                    {
                        Troop troop2;
                        SortedBoundedSet<Troop> list4 = new SortedBoundedSet<Troop>(Session.Parameters.MaxAITroopCountCandidates, new FightingForceComparer());
                        bool isBesideWater = this.IsBesideWater;

                        foreach (Military military in this.Militaries.GetRandomList())
                        {
                            // ★★★ 修复：跳过刚撤退回来的军队（需要休整） ★★★
                            if (military.RecentlyFought > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] 跳过军队{military.Name}（冷却中，剩余{military.RecentlyFought}天）");
                                continue;
                            }
                            
                            if (military.IsFewScaleNeedRetreat && this.Endurance >= 30) continue;
                            if ((isBesideWater || (military.Kind.Type != MilitaryType.水军)) && (((((this.Endurance < 30) || military.Kind.AirOffence) || (military.Scales >= 2)) && (military.Morale > 0x2d)) && ((this.Endurance < 30) || (military.InjuryQuantity < military.Kind.MinScale))))
                            {
                                TroopList candidates = this.AISelectPersonIntoTroop(this, military, false);
                                foreach (Troop t in candidates)
                                {
                                    if (t.FightingForce < 10000 && t.FightingForce < (this.TotalHostileForce * opFactor - this.TotalFriendlyForce) / 25)
                                    {
                                        // 🔥 技术性修复：新创建的部队有保护期，不立即销毁
                                        if (!t.IsNewlyCreated)
                                        {
                                            t.Destroy(true, false);
                                        }
                                        continue;
                                    }
                                    if (t.Army.Scales < 5 && this.Endurance > 30)
                                    {
                                        // 🔥 技术性修复：新创建的部队有保护期，不立即销毁
                                        if (!t.IsNewlyCreated)
                                        {
                                            t.Destroy(true, false);
                                        }
                                        continue;
                                    }
                                    if (t.FoodMax / 2 > this.Food)
                                    {
                                        // 🔥 技术性修复：新创建的部队有保护期，不立即销毁
                                        if (!t.IsNewlyCreated)
                                        {
                                            t.Destroy(true, false);
                                        }
                                        continue;
                                    }

                                    Troop removed;
                                    list4.Add(t, out removed);
                                    if (removed != null)
                                    {
                                        removed.Destroy(true, false);
                                    }
                                }
                            }
                        }

                        if (list4.Count > 0)
                        {
                            foreach (Troop troop in list4)
                            {
                                bool personAlreadyOut = false;
                                foreach (Person p in troop.Candidates)
                                {
                                    if (p.LocationTroop != null)
                                    {
                                        personAlreadyOut = true;
                                        break;
                                    }
                                }
                                if (personAlreadyOut) continue;
                                bool militaryOut = true;
                                foreach (Military m in this.Militaries)
                                {
                                    if (troop.Army == m)
                                    {
                                        militaryOut = false;
                                        break;
                                    }
                                }
                                if (militaryOut) continue;

                                Point? nullable = this.GetCampaignPosition(troop, orientations, troop.Army.Scales > 0);
                                if (!nullable.HasValue)
                                {
                                    break;
                                }

                                Person leader = troop.Candidates[0] as Person;
                                PersonList candidates = this.SelectSubOfficersToTroop(troop);
                                troop2 = this.CreateTroop(candidates, leader, troop.Army, -1, nullable.Value);
                                
                                // 🔥 修复：CreateTroop 可能返回 null（武将不足、位置冲突等）
                                if (troop2 == null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] CreateTroop 失败，跳过该部队");
                                    continue;
                                }
                                
                                // 🔧 FIX: 不再设置 WillArchitecture = this，避免防守部队自杀
                                // troop2.WillArchitecture = this; // ❌ 已删除
                                Legion defensiveLegion = this.GetOrCreateDefensiveLegion();
                                defensiveLegion.AddTroop(troop2);
                                //this.PostCreateTroop(troop2, false);
                                this.TotalFriendlyForce += troop2.FightingForce;
                                troopSent++;

                                if (this.TotalFriendlyForce > this.TotalHostileForce * opFactor && friendlyTroopsInView.Count + troopSent >= 4)
                                {
                                    break;
                                }
                            }

                            foreach (Troop t in list4)
                            {
                                t.Destroy(true, false);
                            }
                        }
                    }
                }
            }
            else if (!this.HasPerson() && this.HasCampaignableMilitary())
            {
                int totalHostilePersonCount = 0;
                foreach (Troop troop in this.GetHostileTroopsInView())
                {
                    totalHostilePersonCount += troop.PersonCount;
                }
                int send = totalHostilePersonCount / 2;
                // 🔧 修复：检查并修复BelongedSection为null的问题
                if (this.BelongedSection == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] ⚠️ 建筑{this.Name}(势力:{this.BelongedFaction?.Name})的BelongedSection为null，尝试修复...");
                    
                    // 尝试重新分配到势力的第一个军区
                    if (this.BelongedFaction != null)
                    {
                        Section firstSection = this.BelongedFaction.FirstSection;
                        if (firstSection != null)
                        {
                            firstSection.AddArchitecture(this);
                            System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] 建筑{this.Name}已重新分配到军区{firstSection.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] 势力{this.BelongedFaction.Name}没有军区，跳过防御AI");
                            return; // 无法修复，跳过防御AI
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DefensiveCampaign] 建筑{this.Name}没有势力，跳过防御AI");
                        return; // 无法修复，跳过防御AI
                    }
                }
                foreach (Architecture a in this.BelongedSection.Architectures)
                {
                    if (a == this) continue;
                    if (a.HasHostileTroopsInView()) continue;

                    if (a.PersonCount <= send) continue;

                    this.CallPeople(a, send);

                }
            }

            //not enough defensive troop, call for reinforcements!!
            float rate = (float)Math.Max(1, (200 - this.Endurance) * 0.005 + 1);
            if (quickBattleList == null && this.TotalFriendlyForce < this.TotalHostileForce * rate)
            {
                foreach (LinkNode i in this.AIAllLinkNodes.Values)
                {
                    if (i.Level > 1) break;
                    if (i.A.actuallyUnreachableArch.Contains(this))
                    {
                        continue;
                    }
                    if (this.BelongedFaction == i.A.BelongedFaction && i.A.HasPerson()
                        && i.A.BelongedSection != null && i.A.BelongedSection.AIDetail.AutoRun)
                    {

                        SortedBoundedSet<Troop> supportList = new SortedBoundedSet<Troop>(Session.Parameters.MaxAITroopCountCandidates, new FightingForceComparer());
                        Troop troop2;

                        foreach (Military military in i.A.Militaries.GetRandomList())
                        {
                            if (military.IsFewScaleNeedRetreat) continue;
                            if (military.IsTransport) continue;
                            if (this.isArmyNavigableTo(i, military) && (military.Morale > 90) && (military.InjuryQuantity < military.Kind.MinScale))
                            {
                                TroopList candidates = this.AISelectPersonIntoTroop(this, military, true);
                                foreach (Troop t in candidates)
                                {
                                    if ((t.FightingForce < 10000) && (t.Army.Scales < 10))
                                    {
                                        // 🔥 技术性修复：新创建的部队有保护期，不立即销毁
                                        if (!t.IsNewlyCreated)
                                        {
                                            t.Destroy(true, false);
                                        }
                                        continue;
                                    }

                                    Troop removed;
                                    supportList.Add(t, out removed);
                                    if (removed != null)
                                    {
                                        removed.Destroy(true, false);
                                    }
                                }
                            }
                        }

                        if (supportList.Count > 0)
                        {
                            foreach (Troop troop in supportList)
                            {
                                bool personAlreadyOut = false;
                                foreach (Person p in troop.Candidates)
                                {
                                    if (p.LocationTroop != null)
                                    {
                                        personAlreadyOut = true;
                                        break;
                                    }
                                }
                                if (personAlreadyOut) continue;
                                bool militaryOut = true;
                                foreach (Military m in i.A.Militaries)
                                {
                                    if (troop.Army == m)
                                    {
                                        militaryOut = false;
                                        break;
                                    }
                                }
                                if (militaryOut) continue;

                                Point? nullable = i.A.GetCampaignPosition(troop, orientations, troop.Army.Scales > 0);
                                if (!nullable.HasValue)
                                {
                                    continue;
                                }
                                Person leader = troop.Candidates[0] as Person;
                                PersonList candidates = i.A.SelectSubOfficersToTroop(troop);
                                troop2 = i.A.CreateTroop(candidates, leader, troop.Army, -1, nullable.Value);
                                // 🔧 FIX: 不再设置 WillArchitecture = this，避免援军自杀
                                // troop2.WillArchitecture = this; // ❌ 已删除
                                Legion defensiveLegion = this.GetOrCreateDefensiveLegion();
                                defensiveLegion.AddTroop(troop2);
                               // i.A.PostCreateTroop(troop2, false);
                                this.TotalFriendlyForce += troop2.FightingForce;
                            }
                            foreach (Troop t in supportList)
                            {
                                t.Destroy(true, false);
                            }

                        }
                    }

                }
            }
        }

        private bool isArmyNavigableTo(LinkKind kind, Military military)
        {
            return Session.GlobalVariables.LandArmyCanGoDownWater ||
                ((kind == LinkKind.Land && military.Kind.Type != MilitaryType.水军) || (kind == LinkKind.Water && military.Kind.Type == MilitaryType.水军) || kind == LinkKind.Both);
        }

        private bool isArmyNavigableTo(LinkNode targetNode, Military military)
        {
            return Session.GlobalVariables.LandArmyCanGoDownWater ||
                ((targetNode.Kind == LinkKind.Land && military.Kind.Type != MilitaryType.水军) || (targetNode.Kind == LinkKind.Water && military.Kind.Type == MilitaryType.水军) || targetNode.Kind == LinkKind.Both);
        }

        public void DemolishAllRouteways()
        {
            foreach (Routeway routeway in this.Routeways.GetList())
            {
                Session.Current.Scenario.RemoveRouteway(routeway);
            }
        }

        public void DemolishFacility(Facility facility)
        {
            if (this.FacilityEnabled || facility.MaintenanceCost <= 0)
            {
                facility.Influences.PurifyInfluence(this, Applier.Facility, facility.ID);
            }
            this.Facilities.Remove(facility);
            Session.Current.Scenario.Facilities.Remove(facility);
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseFacilityDemolished(Session.Current.Scenario, this, facility);
        }

        /// <summary>
        /// 优化后的破坏可用性检查 - 避免每次遍历所有城市
        /// </summary>
        public bool DestroyAvail()
        {
            // 第一步：快速检查基本条件
            if (this.MovablePersons.Count <= 0 || this.Fund < this.DestroyArchitectureFund)
            {
                return false;
            }
            
            // 第二步：使用简化检查，避免遍历所有城市
            return this.HasPotentialDestroyTargetsQuick();
        }
        
        /// <summary>
        /// 快速检查是否可能有破坏目标（不遍历所有城市）
        /// </summary>
        private bool HasPotentialDestroyTargetsQuick()
        {
            // Helper function to check if a single architecture is a valid target
            bool IsValidTarget(Architecture arch)
            {
                if (arch != null && 
                    arch.BelongedFaction != null && 
                    !this.IsFriendly(arch.BelongedFaction) && 
                    this.BelongedFaction.IsArchitectureKnown(arch))
                {
                     // Check if "worthwhile" (Fund > 0 or Endurance > 0) per user logic
                     if (arch.Endurance > 0 || arch.Fund > 0)
                     {
                         return true;
                     }
                }
                return false;
            }

            // 1. Check direct neighbors (Land + Water)
            foreach (Architecture neighbor in this.AILandLinks)
            {
                if (IsValidTarget(neighbor)) return true;
                
                // 2. Check neighbors of neighbors (Land + Water)
                foreach (Architecture nextNeighbor in neighbor.AILandLinks)
                {
                    if (IsValidTarget(nextNeighbor)) return true;
                }
                foreach (Architecture nextNeighbor in neighbor.AIWaterLinks)
                {
                    if (IsValidTarget(nextNeighbor)) return true;
                }
            }

            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                if (IsValidTarget(neighbor)) return true;

                // 2. Check neighbors of neighbors (Land + Water)
                foreach (Architecture nextNeighbor in neighbor.AILandLinks)
                {
                    if (IsValidTarget(nextNeighbor)) return true;
                }
                foreach (Architecture nextNeighbor in neighbor.AIWaterLinks)
                {
                    if (IsValidTarget(nextNeighbor)) return true;
                }
            }

            return false;
        }

        public bool DetailAvail()
        {
            return (Session.GlobalVariables.SkyEye || this.CurrentPlayerOwned());
        }



        private void DetectAmbush(Troop troop, InformationLevel level)
        {
            int chance = 40 - troop.Leader.Calmness;
            if (level <= InformationLevel.中)
            {
                if (troop.OnlyBeDetectedByHighLevelInformation)
                {
                    return;
                }
            }
            else
            {
                chance *= 3;
            }
            if (GameObject.Chance(chance))
            {
                troop.AmbushDetected(troop);
            }
        }

        private void DetectAmbushTroop()
        {
            if (this.BelongedFaction != null)
            {
                GameArea longViewArea = this.LongViewArea;
                foreach (Point point in longViewArea.Area)
                {
                    this.CheckAmbushTroop(point);
                }
            }
        }

        private void DevelopAgriculture()
        {
            if (this.Agriculture != this.AgricultureCeiling)
            {
                foreach (Person person in this.AgricultureWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.AgricultureAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddPoliticsExperience(randomValue * 2);
                        person.AddGlamourExperience(randomValue * 2);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        if (GameObject.Random(360 / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.IncreaseKarma(1);
                        }
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfAgricultureReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfAgricultureTechniquePoint) * 100);
                        this.IncreaseAgriculture(randomValue);
                    }
                }
            }
        }

        private void DevelopArmy()
        {
            foreach (Military military in this.Militaries)
            {
                military.Recovery(this.MultipleOfRecovery);

                this.RecruitmentMilitary(military);
            }
            this.TrainMilitary();
        }

        private void DevelopCommerce()
        {
            if (this.Commerce != this.CommerceCeiling)
            {
                foreach (Person person in this.CommerceWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.CommerceAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddIntelligenceExperience(randomValue);
                        person.AddPoliticsExperience(randomValue * 2);
                        person.AddGlamourExperience(randomValue);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        if (GameObject.Random(540 / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.IncreaseKarma(1);
                        }
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfCommerceReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfCommerceTechniquePoint) * 100);
                        this.IncreaseCommerce(randomValue);
                    }
                }
            }
        }

        public void DevelopDay()
        {
            if (_architectureKind.HasAgriculture)
            {
                this.DevelopAgriculture();
            }
            if (_architectureKind.HasCommerce)
            {
                this.DevelopCommerce();
            }
            if (_architectureKind.HasTechnology)
            {
                this.DevelopTechnology();
            }
            if (_architectureKind.HasDomination)
            {
                this.DevelopDomination();
            }
            if (_architectureKind.HasMorale)
            {
                this.DevelopMorale();
            }
            if (_architectureKind.HasEndurance)
            {
                this.DevelopEndurance();
            }
            if (_architectureKind.HasPopulation)
            {
                this.DevelopPopulation();
            }
            this.DevelopArmy();
            this.ClearWork();
            this.RefreshIdleWorkAfterDailyDevelop();
        }

        public void DevelopDayNoFaction()
        {
            this.DevelopPopulation();
        }

        private void DevelopDomination()
        {
            if (this.Domination != this.DominationCeiling)
            {
                foreach (Person person in this.DominationWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.DominationAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddStrengthExperience(randomValue * 2);
                        person.AddCommandExperience(randomValue);
                        person.AddGlamourExperience(randomValue);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        if (GameObject.Random((190 - person.PersonalLoyalty * 10) / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.IncreaseKarma(1);
                        }
                        else if (GameObject.Random((160 + person.PersonalLoyalty * person.PersonalLoyalty * 20) / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.DecreaseKarma(1);
                        }
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfDominationReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfDominationTechniquePoint) * 100);
                        this.IncreaseDomination(randomValue);
                    }
                }
            }
        }

        private void DevelopEndurance()
        {
            if ((this.Endurance != this.EnduranceCeiling) && ((this.Endurance != 0) || !this.HasContactHostileTroop(this.BelongedFaction)))
            {
                foreach (Person person in this.EnduranceWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.EnduranceAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (this.IsSurrounded()) { randomValue = randomValue / 10; }
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddStrengthExperience(randomValue);
                        person.AddCommandExperience(randomValue);
                        person.AddIntelligenceExperience(randomValue);
                        person.AddPoliticsExperience(randomValue);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfEnduranceReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfEnduranceTechniquePoint) * 100);
                        this.IncreaseEndurance(randomValue);
                    }
                }
            }
        }

        public void DevelopFood()
        {
            this.IncreaseFood(this.ExpectedFood);
        }

        public void DevelopFund()
        {
            this.IncreaseFund(this.ExpectedFund);
        }

        public int ExpectedSalary
        {
            get
            {
                var sum = 0;
                foreach (Person p in Persons)
                {
                    sum += p.Salary;
                }
                return sum;
            }
        }

        public void PaySalary()
        {
            foreach (Person p in Persons.GetRandomList())
            {
                if (Fund >= p.Salary)  // 修复：使用>=确保不会负数
                {
                    this.DecreaseFund(p.Salary);  // 使用安全的减少方法
                }
                else
                {
                    p.TempLoyaltyChange -= (int) (p.Salary * Session.Parameters.SalaryLoyaltyLoss);
                }
            }
        }

        public void DevelopMilitaryPopulation()
        {

        }

        private void DevelopMonth()
        {
            if (this.BelongedFaction != null)
            {
                if (_architectureKind.HasAgriculture)
                {
                    this.DevelopFood();
                }
                if (_architectureKind.HasCommerce)
                {
                    this.DevelopFund();
                }
                this.PaySalary();
            }

        }

        private void DevelopMorale()
        {
            if (this.Morale != this.MoraleCeiling)
            {
                foreach (Person person in this.MoraleWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.MoraleAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddCommandExperience(randomValue);
                        person.AddPoliticsExperience(randomValue);
                        person.AddGlamourExperience(randomValue * 2);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        if (GameObject.Random((190 - person.PersonalLoyalty * 10) / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.IncreaseKarma(1);
                        }
                        else if (GameObject.Random((160 + person.PersonalLoyalty * person.PersonalLoyalty * 20) / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.DecreaseKarma(1);
                        }
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfMoraleReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfMoraleTechniquePoint) * 100);
                        this.IncreaseMorale(randomValue);
                    }
                }
            }
        }

        private void DevelopPopulation()
        {
            double populationDevelopingRate = this.PopulationDevelopingRate;
            if (populationDevelopingRate != 0.0)
            {
                //this.IncreasePopulation(StaticMethods.GetRandomValue(this.population + (0x3e8 * this.AreaCount), (int) (1.0 / populationDevelopingRate)));
                int pop = StaticMethods.GetBigRandomValue(this.PopulationCeiling + (1000 * this.AreaCount), (int)(1.0 / populationDevelopingRate));
                this.IncreasePopulation(pop);

                float mPop;
                if (this.Population > 500000)
                {
                    mPop = pop / 4;
                }
                else
                {
                    mPop = (int) (pop * (0.25 + (500000 - this.Population) / 500000 * 0.25) * Session.Parameters.MilitaryPopulationReloadQuantity);
                }
                this.IncreaseMilitaryPopulation((int) mPop);
                if (GameObject.Chance((int) ((mPop - (int) mPop) * 100))) {
                    this.IncreaseMilitaryPopulation(1);
                }
            }
        }

        public void DevelopSeason()
        {
            if (this.BelongedFaction != null)
            {

                if (_architectureKind.HasPopulation || _architectureKind.HasMorale)
                {
                    this.DevelopMilitaryPopulation();
                }
            }
        }

        private void DevelopTechnology()
        {
            if (this.Technology != this.TechnologyCeiling)
            {
                foreach (Person person in this.TechnologyWorkingPersons)
                {
                    if (!person.InternalNoFundNeeded)
                    {
                        if (this.Fund < this.InternalFundCost)
                        {
                            continue;
                        }
                        this.DecreaseFund(this.InternalFundCost);
                    }
                    int randomValue = StaticMethods.GetRandomValue((int)((person.TechnologyAbility * this.CurrentRateOfInternal) * Session.Parameters.InternalRate), 500 + (150 * (this.AreaCount - 1)));
                    if (randomValue > 0)
                    {
                        person.AddInternalExperience(randomValue * 2);
                        person.AddIntelligenceExperience(randomValue * 2);
                        person.AddPoliticsExperience(randomValue * 2);
                        person.IncreaseReputation(randomValue * 4);
                        person.IncreaseOfficerMerit(randomValue * 4);
                        if (GameObject.Random(450 / Session.Current.Scenario.Parameters.DayInTurn) == 0)
                        {
                            person.IncreaseKarma(1);
                        }
                        this.BelongedFaction.IncreaseReputation(randomValue * person.MultipleOfTechnologyReputation);
                        this.BelongedFaction.IncreaseTechniquePoint((randomValue * person.MultipleOfTechnologyTechniquePoint) * 100);
                        this.IncreaseTechnology(randomValue);
                    }
                }
            }
        }

        public void DevelopYear()
        {
        }

        private void AIDiplomaticTactics()
        {
           // this.AIQuanXiang();
            this.AIGeDi();
        }

        private void AIGeDi()
        {
            return;
#pragma warning disable CS0162 // Unreachable code detected
            if (!this.HasHostileTroopsInView()) return;
#pragma warning restore CS0162 // Unreachable code detected

            if (this.MovablePersons.Count > 0)
            {
                TroopList hostileTroopInView = this.GetHostileTroopsInView();
                TroopList friendlyTroopsInView = this.GetFriendlyTroopsInView();
                int hostileFightingForce = 0;
                int friendlyFightingForce = 0;

                foreach (Troop t in hostileTroopInView)
                {
                    hostileFightingForce += t.FightingForce;
                }
                foreach (Troop t in friendlyTroopsInView)
                {
                    friendlyFightingForce += t.FightingForce;
                }

                if (hostileFightingForce > friendlyFightingForce * (this.BelongedFaction.Leader.PersonalLoyalty + 1) &&
                    GameObject.Chance(40 - this.BelongedFaction.Leader.PersonalLoyalty * 10))
                {
                    // 🔥 安全修复：避免IndexOutOfRangeException和ArgumentNullException
                    if (this.MovablePersons != null && this.MovablePersons.Count > 0)
                    {
                        Person shizhe = this.MovablePersons[Random(this.MovablePersons.Count)] as Person;
                        var diplomaticList = this.GetGeDiDiplomaticRelationList();
                        if (diplomaticList != null && diplomaticList.Count > 0 && shizhe != null)
                        {
                            DiplomaticRelationDisplay display = diplomaticList[GameObject.Random(diplomaticList.Count)] as DiplomaticRelationDisplay;
                            if (display != null)
                            {
                                shizhe.GoToGeDiDiplomatic(display);
                            }
                        }
                    }
                }
            }
        }


        /*
        private void AIQuanXiang()
        {
            // 🔥 劝降是势力级别的外交决策，只有AI势力才执行
            if (!ShouldExecuteAdvancedAI()) return;

            if (!Session.GlobalVariables.PermitQuanXiang) return;

            if (!GameObject.Chance(50)) return ;

            if (this.QuanXiangDiplomaticRelationAvail())
            {
                PersonList pl = new PersonList();
                DiplomaticRelationDisplay display = this.GetAIQuanXiangDiplomaticRelationList()[GameObject.Random(this.GetAIQuanXiangDiplomaticRelationList().Count)] as DiplomaticRelationDisplay;
                foreach (Person person in this.MovablePersons)
                {
                    if (person.Loyalty >= 100 && person.WorkKind == ArchitectureWorkKind.无
                        && person.Intelligence >= 60 && person.Politics >= 60)
                    {
                        pl.Add(person);
                    }
                }

                if (pl.Count > 1)
                {
                    foreach (Person p in pl)
                    {
                        if (this.Fund >= 50000)
                        {
                            this.DecreaseFund(50000);  // 使用安全的减少方法

                            p.GoToQuanXiangDiplomatic(display);
                        }
                        //throw new Exception("劝降目标势力" + display.FactionName + "劝降武将" + p.Name + p.ID );
                    }
                }

            }
        }*/

        /*
        private void DiplomaticRelationAI()
        {
            if (((this.PlanArchitecture == null) || GameObject.Chance(10)) && (this.BelongedFaction != null))
            {
            }
        }*/

        public bool DisbandAvail()
        {
            return ((this.Militaries.Count > 0) && _architectureKind.HasPopulation);
        }

        public void DisbandMilitary(Military m)
        {
            if (!m.IsTransport)
            {
                this.IncreasePopulation(m.Quantity);
                this.IncreaseMilitaryPopulation(m.Quantity);
            }
            this.RemoveMilitary(m);
            this.BelongedFaction.RemoveMilitary(m);
            Session.Current.Scenario.Militaries.Remove(m);
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseDisbandMilitary(Session.Current.Scenario, this, m);
        }

        public bool DisbandSectionAvail()
        {
            return (this.BelongedFaction.SectionCount > 1);
        }

        public bool DominationAvail()
        {
            return (_architectureKind.HasDomination && this.HasPerson());
        }

        public string DominationInInformationLevel(InformationLevel level)
        {
            switch (level)
            {
                case InformationLevel.未知:
                    return "----";

                case InformationLevel.无:
                    return "----";

                case InformationLevel.低:
                    return StaticMethods.GetNumberStringByGranularity(this.Domination, 20);

                case InformationLevel.中:
                    return StaticMethods.GetNumberStringByGranularity(this.Domination, 10);

                case InformationLevel.高:
                    return StaticMethods.GetNumberStringByGranularity(this.Domination, 5);

                case InformationLevel.全:
                    return this.Domination.ToString();
            }
            return "----";
        }

        public bool EnduranceAvail()
        {
            return (_architectureKind.HasEndurance && this.HasPerson());
        }

        public string EnduranceInInformationLevel(InformationLevel level)
        {
            switch (level)
            {
                case InformationLevel.未知:
                    return "----";

                case InformationLevel.无:
                    return "----";

                case InformationLevel.低:
                    return StaticMethods.GetNumberStringByGranularity(this.Endurance, 500);

                case InformationLevel.中:
                    return StaticMethods.GetNumberStringByGranularity(this.Endurance, 200);

                case InformationLevel.高:
                    return StaticMethods.GetNumberStringByGranularity(this.Endurance, 100);

                case InformationLevel.全:
                    return this.Endurance.ToString();
            }
            return "----";
        }

        public bool FacilityBuildable(FacilityKind facilityKind)
        {
            return facilityKind.CanBuild(this) && this.Fund >= facilityKind.FundCost && this.FacilityPositionLeft >= facilityKind.PositionOccupied;
        }

        private void FacilityDoWork()
        {
            foreach (Facility facility in this.Facilities)
            {
                if (this.FacilityEnabled || facility.MaintenanceCost <= 0)
                {
                    facility.DoWork(this);
                }
            }
        }

        private void FacilityMaintenance()
        {
            int facilityMaintenanceCost = noFundToSustainFacility ? 0 : this.FacilityMaintenanceCost;
            if (this.Fund >= facilityMaintenanceCost)
            {
                // 🔥 关键修复：必须先设置FacilityEnabled=true，再调用ApplyFacilityInfluences
                // 因为ApplyFacilityInfluences内部会检查FacilityEnabled状态
                this.FacilityEnabled = true;
                this.ApplyFacilityInfluences(true);
                this.DecreaseFund(facilityMaintenanceCost);
            }
            else
            {
                this.PurifyFacilityInfluences();
                this.FacilityEnabled = false;
            }
        }

        private void FacilityRecovery()
        {
            if (this.FacilityEnabled)
            {
                this.Facilities.RecoverEndurance(this.facilityEnduranceIncrease);
            }
        }

        public bool FactionHasCaptive()
        {
            return ((this.BelongedFaction != null) ? this.BelongedFaction.HasCaptive() : false);
        }

        public bool FactionHasSelfCaptive()
        {
            return ((this.BelongedFaction != null) ? this.BelongedFaction.HasSelfCaptive() : false);
        }

        public bool FindRouteway(LinkNode node, bool hasEnd, out float rate)
        {
            rate = 1f;
            Point key = new Point(base.ID, node.A.ID);
            if (!this.BelongedFaction.ClosedRouteways.ContainsKey(key))
            {
                Point? nullable;
                Point? nullable2;
                Session.Current.Scenario.GetClosestPointsBetweenTwoAreas(this.GetRoutewayStartPoints(), node.A.GetAIRoutewayEndPoints(this, false), out nullable, out nullable2);
                if (nullable.HasValue && nullable2.HasValue)
                {
                    this.BelongedFaction.RoutewayPathBuilder.MultipleWaterCost = node.Kind == LinkKind.Land;
                    if (this.BelongedFaction.RoutewayPathAvail(nullable.Value, nullable2.Value, hasEnd))
                    {
                        rate = this.BelongedFaction.RoutewayPathBuilder.PathConsumptionRate;
                        return true;
                    }
                }
            }
            return false;
        }

        public void FoodReduce()
        {
            this.DecreaseFood((int)(this.Food * this.FoodReduceDayRate));
        }

        public void FundPacksDayEvent()
        {
            // 🔥 根本修复：使用对象快照避免集合修改导致的索引越界
            // 日期：2026-03-19
            // 原因：IncreaseFund 可能触发事件，事件处理器可能修改 FundPacks 集合
            // 解决方案：先收集需要处理的对象，再统一删除和处理
            // 性能：Cold Path（每回合一次），使用 List 分配可接受
            
            List<FundPack> toRemove = [];
            List<FundPack> toProcess = [];
            
            foreach (FundPack pack in this.FundPacks)
            {
                pack.Days--;
                
                // 🔥 关键：到期的资源包总是删除
                if (pack.Days <= 0)
                {
                    toRemove.Add(pack);
                    
                    // 🔥 关键：只有未被围困时才增加资源
                    if (!this.IsSurrounded())
                    {
                        toProcess.Add(pack);
                    }
                }
            }
            
            // 🔥 关键：先删除所有到期的包（无论是否被围困）
            foreach (FundPack pack in toRemove)
            {
                this.FundPacks.Remove(pack);
            }
            
            // 🔥 关键：只处理未被围困时到期的包
            foreach (FundPack pack in toProcess)
            {
                this.IncreaseFund(pack.Fund);
            }
        }

        public void FoodPacksDayEvent()
        {
            // 🔥 根本修复：使用对象快照避免集合修改导致的索引越界
            // 日期：2026-03-19
            // 原因：IncreaseFood 可能触发事件，事件处理器可能修改 FoodPacks 集合
            // 解决方案：先收集需要处理的对象，再统一删除和处理
            // 性能：Cold Path（每回合一次），使用 List 分配可接受
            
            List<FoodPack> toRemove = [];
            List<FoodPack> toProcess = [];
            
            foreach (FoodPack pack in this.FoodPacks)
            {
                pack.Days--;
                
                // 🔥 关键：到期的资源包总是删除
                if (pack.Days <= 0)
                {
                    toRemove.Add(pack);
                    
                    // 🔥 关键：只有未被围困时才增加资源
                    if (!this.IsSurrounded())
                    {
                        toProcess.Add(pack);
                    }
                }
            }
            
            // 🔥 关键：先删除所有到期的包（无论是否被围困）
            foreach (FoodPack pack in toRemove)
            {
                this.FoodPacks.Remove(pack);
            }
            
            // 🔥 关键：只处理未被围困时到期的包
            foreach (FoodPack pack in toProcess)
            {
                this.IncreaseFood(pack.Food);
            }
        }

        public void GenerateAllAILinkNodes(int levelMax)
        {
            this.AILinkProcedureDetails.Clear();
            this.AIAllLinkNodes.Clear();
            List<Architecture> path = new List<Architecture>();
            this.AILinkProcedureDetails.Enqueue(new AILinkProcedureDetail(0, this, path));
            while (this.AILinkProcedureDetails.Count > 0)
            {
                AILinkProcedureDetail detail = this.AILinkProcedureDetails.Dequeue();
                this.AddAllAILink(detail.Level, levelMax, detail.A, detail.Path);
            }
            foreach (LinkNode node in this.AIAllLinkNodes.Values)
            {
                node.Kind = this.CheckCampaignable(node);
            }
        }

        public GameObjectList GetAILinks()
        {
            GameObjectList list = this.AILandLinks.GetList();
            foreach (Architecture architecture in this.AIWaterLinks)
            {
                if (list.GetGameObject(architecture.ID) == null)
                {
                    list.Add(architecture);
                }
            }
            return list;
        }

        public GameObjectList GetAILinks(int level)
        {
            GameObjectList list = new GameObjectList();
            foreach (LinkNode node in this.AIAllLinkNodes.Values)
            {
                if (node.Level <= level)
                {
                    list.Add(node.A);
                }
            }
            return list;
        }

        public GameArea GetAIRoutewayEndPoints(Architecture a, bool nowater)
        {
            GameArea area = new GameArea();
            if (!this.IsFriendly(a.BelongedFaction))
            {
                foreach (Point point in this.ContactArea.Area)
                {
                    if (a.IsRoutewayPossible(point) && (!nowater || (Session.Current.Scenario.GetTerrainKindByPosition(point) != TerrainKind.水域)))
                    {
                        area.AddPoint(point);
                    }
                }
            }
            if (area.Count == 0)
            {
                foreach (Point point in this.GetRoutewayStartArea().Area)
                {
                    if (a.IsRoutewayPossible(point) && (!nowater || (Session.Current.Scenario.GetTerrainKindByPosition(point) != TerrainKind.水域)))
                    {
                        area.AddPoint(point);
                    }
                }
            }
            if (area.Count == 0)
            {
                foreach (Point point in this.LongViewArea.Area)
                {
                    if (a.IsRoutewayPossible(point))
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }

        public GameArea GetAllAvailableArea(bool Square)
        {
            GameArea area = new GameArea();
            foreach (Point point in this.ContactArea.Area)
            {
                if (Session.Current.Scenario.IsPositionEmpty(point) && Session.Current.Scenario.GetTerrainDetailByPosition(point) != null && Session.Current.Scenario.GetTerrainDetailByPosition(point).RoutewayConsumptionRate < 1)
                {
                    area.AddPoint(point);
                }
            }
            foreach (Point point in this.ArchitectureArea.Area)
            {
                if (!Session.Current.Scenario.PositionIsTroop(point) && Session.Current.Scenario.GetTerrainDetailByPosition(point) != null && Session.Current.Scenario.GetTerrainDetailByPosition(point).RoutewayConsumptionRate < 1)
                {
                    area.AddPoint(point);
                }
            }
            return area;
        }

        public GameArea GetAllContactArea()
        {
            GameArea area = new GameArea();
            foreach (Point point in this.ContactArea.Area)
            {
                area.AddPoint(point);
            }
            foreach (Point point in this.ArchitectureArea.Area)
            {
                area.AddPoint(point);
            }
            return area;
        }

        /// <summary>
        /// 获取建筑内所有人物（含移动人物）
        /// </summary>
        /// <returns></returns>
        public PersonList GetAllPersons()
        {
            PersonList list = new PersonList();

            list.AddRange(Persons);

            list.AddRange(MovingPersons);

            return list;
        }

        public GameArea GetAvailableContactArea(bool Square)
        {
            GameArea area = new GameArea();
            foreach (Point point in this.ContactArea.Area)
            {
                if (Session.Current.Scenario.IsPositionEmpty(point))
                {
                    area.AddPoint(point);
                }
            }
            if (area.Count > 0)
            {
                return area;
            }
            return null;
        }

        public MilitaryList GetBeMergedMilitaryList(Military military)
        {
            this.BeMergedMilitaryList.Clear();
            foreach (Military military2 in this.MergeMilitaryList)
            {
                if ((military2 != military) && (military2.Kind.Equals(military.Kind)))
                {
                    this.BeMergedMilitaryList.Add(military2);
                }
            }
            return this.BeMergedMilitaryList;
        }

        public GameObjectList GetBuildableFacilityKindList()
        {
            this.BuildableFacilityKindList.Clear();
            foreach (FacilityKind kind in Session.Current.Scenario.GameCommonData.AllFacilityKinds.FacilityKinds.Values)
            {
                if (this.BelongedFaction != null && !this.BelongedFaction.hougongValid && kind.rongna > 0 && kind.InfluenceCount == 0) continue;
                if (this.FacilityBuildable(kind))
                {
                    this.BuildableFacilityKindList.Add(kind);
                }
            }
            return this.BuildableFacilityKindList;
        }

        public MilitaryList GetCampaignMilitaryList()
        {
            this.CampaignMilitaryList.Clear();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[GetCampaignMilitaryList] 建筑: {this.Name}, 总Military数: {this.Militaries.Count}");
            #endif
            
            foreach (Military military in this.Militaries)
            {
                if ((military.Quantity > 0) && (military.Morale > 0))
                {
                    this.CampaignMilitaryList.AddMilitary(military);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  Military ID={military.ID}:");
                    System.Diagnostics.Debug.WriteLine($"    Name: '{military.Name ?? "null"}'");
                    System.Diagnostics.Debug.WriteLine($"    KindID: {military.RealKindID}");
                    System.Diagnostics.Debug.WriteLine($"    Kind: {military.Kind?.Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"    Quantity: {military.Quantity}, Morale: {military.Morale}");
                    #endif
                }
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[GetCampaignMilitaryList] 可出征Military数: {this.CampaignMilitaryList.Count}");
            #endif
            
            return this.CampaignMilitaryList;
        }

        public Point? GetCampaignPosition(Troop troop, List<Point> orientations, bool close)
        {
            GameArea allAvailableArea = this.GetAllAvailableArea(false);
            GameArea sourceArea = new GameArea();
            foreach (Point point in allAvailableArea.Area)
            {
                if ((Session.Current.Scenario.GetArchitectureByPosition(point) == this || troop.IsMovableOnPosition(point)) && Session.Current.Scenario.GetTroopByPosition(point) == null)
                {
                    sourceArea.Area.Add(point);
                }
            }
            GameArea highestFightingForceArea = troop.GetHighestFightingForceArea(sourceArea);
            if (highestFightingForceArea != null)
            {
                if (close)
                {
                    return Session.Current.Scenario.GetClosestPosition(highestFightingForceArea, orientations);
                }
                return Session.Current.Scenario.GetFarthestPosition(highestFightingForceArea, orientations);
            }
            return null;
        }

        public ArchitectureList GetChangeCapitalArchitectureList()
        {
            this.ChangeCapitalArchitectureList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture != this)
                    {
                        this.ChangeCapitalArchitectureList.Add(architecture);
                    }
                }
            }
            return this.ChangeCapitalArchitectureList;
        }

        public void GetClosestArchitectures()
        {
            this.ClosestArchitectures = new ArchitectureList();
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture != this)
                {
                    this.ClosestArchitectures.Add(architecture);
                }
            }
            this.QuickSortArchitecturesDistance(this.ClosestArchitectures, 0, this.ClosestArchitectures.Count - 1);
        }

        public ArchitectureList GetClosestArchitectures(int count, double maxDistance)
        {
            if (this.ClosestArchitectures == null)
            {
                this.GetClosestArchitectures();
            }
            ArchitectureList list = new ArchitectureList();
            if (count > this.ClosestArchitectures.Count)
            {
                count = this.ClosestArchitectures.Count;
            }
            for (int i = 0; i < count; i++)
            {
                if (Session.Current.Scenario.GetDistance(this.ArchitectureArea, (this.ClosestArchitectures[i] as Architecture).ArchitectureArea) <= maxDistance)
                {
                    list.Add(this.ClosestArchitectures[i]);
                }
                else
                {
                    break;
                }
            }
            if (list.Count == 0)
            {
                list.Add(this.ClosestArchitectures[0]);
            }
            return list;
        }

        public ArchitectureList GetClosestArchitectures(int count)
        {
            return GetClosestArchitectures(count, double.MaxValue);
        }

        public Routeway GetConnectedRouteway(Architecture end)
        {
            foreach (Routeway routeway in this.Routeways)
            {
                if ((routeway.EndArchitecture == end) && routeway.IsActive)
                {
                    return routeway;
                }
            }
            return null;
        }
        //[DataMember]
        public PersonList ConvinceDestinationPersonList = new PersonList();
        public PersonList GetConvinceDestinationPersonList(Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 开始检查 - 建筑: {this.Name}, 查询势力: {faction.Name}");
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 建筑归属势力: {this.BelongedFaction?.Name ?? "无"}");
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 建筑位置: {this.Position}");
                
                PersonList result = new PersonList();
                
                if (this.BelongedFaction == faction)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 己方建筑，检查俘虏");
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 俘虏数量: {this.Captives.Count}");
                    // 己方建筑：可以说服俘虏
                    foreach (Captive captive in this.Captives)
                    {
                        result.Add(captive.CaptivePerson);
                        System.Diagnostics.Debug.WriteLine($"  - 添加俘虏: {captive.CaptivePerson.Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 非己方建筑，检查情报等级");
                    // 非己方建筑：需要检查情报等级
                    var informationLevel = faction.GetKnownAreaData(this.Position);
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 情报等级: {informationLevel} (数值: {(int)informationLevel})");
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 需要的最低等级: {InformationLevel.低} (数值: {(int)InformationLevel.低})");
                    
                    bool hasEnoughInformation = informationLevel >= InformationLevel.低;
                    System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 情报是否足够: {hasEnoughInformation}");
                    
                    if (hasEnoughInformation)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 情报足够，检查敌方人员");
                        System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] PersonsExcludeNvGuan数量: {this.PersonsExcludeNvGuan.Count}");
                        // 有足够情报才能说服敌方人员
                        foreach (Person person in this.PersonsExcludeNvGuan)
                        {
                            result.Add(person);
                            System.Diagnostics.Debug.WriteLine($"  - 添加敌方人员: {person.Name} (状态: {person.Status})");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 情报不足，不添加敌方人员");
                    }
                }
                
                // 在野人员：不受情报等级限制，任何建筑都可以说服
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 检查在野人员");
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] NoFactionPersons数量: {this.NoFactionPersons.Count}");
                foreach (Person person in this.NoFactionPersons)
                {
                    result.Add(person);
                    System.Diagnostics.Debug.WriteLine($"  - 添加在野人员: {person.Name} (状态: {person.Status})");
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 总目标数量: {result.Count}");
                
                ConvinceDestinationPersonList = result;
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetConvinceDestinationPersonList] 堆栈: {ex.StackTrace}");
                return new PersonList();
            }
        }

        public bool CanMoveFeizi()
        {
            if (this.movableFeizis.Count <= 0) return false;
            //if (this.HasHostileTroopsInView()) return false;
            if (this.BelongedFaction == null) return false;

            foreach (Architecture a in this.BelongedFaction.Architectures)
            {
                if ((a.Meinvkongjian > a.Feiziliebiao.Count || a.BelongedFaction.IsAlien) && a != this && !a.HasHostileTroopsInView())
                {
                    return true;
                }
            }

            return false;
        }

        public GameArea GetFeiziTransferArchitectureArea()
        {
            GameArea area = new GameArea();

            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {

                if (architecture == this)
                {
                    continue;
                }

                if (architecture.Meinvkongjian <= architecture.Feiziliebiao.Count && !architecture.BelongedFaction.IsAlien)
                {
                    continue;
                }

                foreach (Point point in architecture.ArchitectureArea.Area)
                {
                    area.AddPoint(point);
                }
            }

            return area;
        }

        public PersonList ReleasableFeizis
        {
            get
            {
                PersonList list = new PersonList();
                foreach (Person p in this.Feiziliebiao)
                {
                    if (!this.BelongedFaction.Leader.suoshurenwuList.HasGameObject(p))
                    {
                        list.Add(p);
                    }
                }
                return list;
            }
        }

        public bool HaveReleasableFeizis()
        {
            return ReleasableFeizis.Count > 0;
        }

        public bool MoveCaptiveAvail() //俘虏可移动
        {
            if (this.movableCaptives.Count <= 0) return false;
            foreach (Architecture a in this.BelongedFaction.Architectures)
            {
                if (a != this)
                {
                    return true;
                }
            }

            return false;

        }

        public PersonList movableCaptives //俘虏列表
        {
            get
            {
                PersonList movableCaptives = new PersonList();

                if (this.HasHostileTroopsInView())
                {
                    return movableCaptives;
                }

                foreach (Captive captive in this.Captives)
                {
                    if (captive.CaptivePerson.ArrivingDays <= 0)
                    {
                        movableCaptives.Add(captive.CaptivePerson);
                    }

                }
                return movableCaptives;
            }
        }

        public GameArea GetCaptiveTransferArchitectureArea() //俘虏可移动
        {
            GameArea area = new GameArea();

            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {

                if (architecture == this)
                {
                    continue;
                }

                if (architecture.HasHostileTroopsInView())
                {
                    continue;
                }

                foreach (Point point in architecture.ArchitectureArea.Area)
                {
                    area.AddPoint(point);
                }
            }

            return area;
        }

        public bool TransferMilitaryAvail()  //运输编队
        {
            if (this.BelongedFaction == null) return false;
            if (this.movableMilitaries.Count <= 0) return false;
            if (this.HasHostileTroopsInView()) return false;

            foreach (Architecture a in this.BelongedFaction.Architectures)
            {
                if (a != this)
                {
                    return true;
                }
            }
            return false ;
        }

        public MilitaryList movableMilitaries
        {
            get
            {
                MilitaryList movableMilitaries = new MilitaryList();
                foreach (Military i in this.Militaries)
                {
                    if (!i.Kind.IsTransport && i.Quantity > 0 && i.Kind.Movable)
                    {
                        movableMilitaries.Add(i);
                    }
                }
                return movableMilitaries;
            }
        }
        /*
        public GameArea GetMilitaryTransferArchitectureArea()
        {
            GameArea area = new GameArea();

            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {

                if (architecture == this)
                {
                    continue;
                }


                foreach (Point point in architecture.ArchitectureArea.Area)
                {
                    area.AddPoint(point);
                }


            }
            return area;
        }
        */
        public GameArea GetPersonTransferArchitectureArea()
        {

            GameArea area = new GameArea();

            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {

                if (architecture == this)
                {
                    continue;
                }


                foreach (Point point in architecture.ArchitectureArea.Area)
                {
                    area.AddPoint(point);
                }


            }
            return area;
        }


        /// <summary>
        /// 获取可说服区域（带缓存优化）
        /// </summary>
        public GameArea GetConvincePersonArchitectureArea()
        {
            // 检查缓存是否需要更新
            if (ShouldUpdateCache())
            {
                InvalidateAllCaches();
            }
            
            // 如果缓存有效，直接返回
            if (_convinceAreaCacheValid && _cachedConvinceArea != null)
            {
                return _cachedConvinceArea;
            }
            
            // 重新计算并缓存
            _cachedConvinceArea = CalculateConvincePersonArchitectureArea();
            _convinceAreaCacheValid = true;
            _lastCacheUpdateTurn = Session.Current.Scenario.DaySince;
            
            return _cachedConvinceArea;
        }
        
        /// <summary>
        /// 实际计算可说服区域的方法（原始逻辑）
        /// </summary>
        private GameArea CalculateConvincePersonArchitectureArea()
        {
            GameArea area = new GameArea();
            
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture.BelongedFaction == this.BelongedFaction)
                {
                    if (!architecture.HasCaptive() && !architecture.HasNoFactionPerson())
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
                else
                {
                    // 修改：检查情报等级而不只是建筑是否已知
                    bool hasEnoughInformation = this.BelongedFaction.GetKnownAreaData(architecture.Position) >= InformationLevel.低;
                    
                    if ((!architecture.HasPerson() && !architecture.HasNoFactionPerson()) || !hasEnoughInformation)
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }

        /// <summary>
        /// 获取可破坏区域（带缓存优化）
        /// </summary>
        public GameArea GetDestroyArchitectureArea()
        {
            // 检查缓存是否需要更新
            if (ShouldUpdateCache())
            {
                InvalidateAllCaches();
            }
            
            // 如果缓存有效，直接返回
            if (_destroyAreaCacheValid && _cachedDestroyArea != null)
            {
                return _cachedDestroyArea;
            }
            
            // 重新计算并缓存
            _cachedDestroyArea = CalculateDestroyArchitectureArea();
            _destroyAreaCacheValid = true;
            _lastCacheUpdateTurn = Session.Current.Scenario.DaySince;
            
            return _cachedDestroyArea;
        }

        private GameArea CalculateDestroyArchitectureArea()
        {
            GameArea area = new GameArea();

            void AddAreaIfTarget(Architecture arch)
            {
                if (IsDestroyTarget(arch))
                {
                    foreach (Point point in arch.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }

            foreach (Architecture neighbor in this.AILandLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }

            return area;
        }

        private bool IsDestroyTarget(Architecture arch)
        {
            if (arch != null && arch.BelongedFaction != null && !this.IsFriendly(arch.BelongedFaction))
            {
                if (this.BelongedFaction.IsArchitectureKnown(arch))
                {
                    if (arch.Endurance > 0 || arch.Fund > 0) return true;
                }
            }
            return false;

        }
        
        /// <summary>
        /// 实际计算可破坏区域的方法（原始逻辑）
        /// </summary>
        public GameArea GetGossipArchitectureArea()
        {
            GameArea area = new GameArea();

            void AddAreaIfTarget(Architecture arch)
            {
                if (IsGossipTarget(arch))
                {
                    foreach (Point point in arch.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }

            foreach (Architecture neighbor in this.AILandLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }

            return area;
        }
        
        /// <summary>
        /// 检查是否需要更新缓存
        /// </summary>
        private bool ShouldUpdateCache()
        {
            return _lastCacheUpdateTurn != Session.Current.Scenario.DaySince;
        }
        
        /// <summary>
        /// 使所有缓存失效
        /// </summary>
        public void InvalidateAllCaches()
        {
            _convinceAreaCacheValid = false;
            _destroyAreaCacheValid = false;
            _cachedConvinceArea = null;
            _cachedDestroyArea = null;
        }
        
        /// <summary>
        /// 在用户实际选择说服时才检查是否有真实目标
        /// </summary>
        public bool ValidateConvinceTargetsOnExecution()
        {
            return this.CalculateConvincePersonArchitectureArea().Count > 0;
        }
        
        /// <summary>
        /// 在用户实际选择破坏时才检查是否有真实目标
        /// </summary>
        public bool ValidateDestroyTargetsOnExecution()
        {
            return this.CalculateDestroyArchitectureArea().Count > 0;
        }

        public int GetDistanceFromFaction(Faction faction)
        {
            if ((faction == null) || (faction.ArchitectureCount == 0))
            {
                return 0;
            }
            if (this.BelongedFaction == faction)
            {
                return 0;
            }
            if (this.ClosestArchitectures == null)
            {
                this.GetClosestArchitectures();
            }
            int num = 0;
            for (int i = 0; i < this.ClosestArchitectures.Count; i++)
            {
                if ((this.ClosestArchitectures[i] as Architecture).BelongedFaction == faction)
                {
                    num += (i * (this.ClosestArchitectures[i] as Architecture).Population) / 0x2710;
                }
            }
            if (this.BelongedFaction != null)
            {
                int diplomaticRelation = Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, faction.ID);
                if (diplomaticRelation <= -200)
                {
                    num /= 4;
                }
                else if (diplomaticRelation < 0)
                {
                    num /= 1;
                }
            }
            return (num / faction.ArchitectureCount);
        }

        public int GetDistanceFromSection(Section section)
        {
            if ((section == null) || (section.ArchitectureCount == 0))
            {
                return 0;
            }
            if (this.BelongedSection == section)
            {
                return 0;
            }
            int num = 0;
            foreach (Architecture architecture in section.Architectures)
            {
                LinkNode node = null;
                this.AIAllLinkNodes.TryGetValue(architecture.ID, out node);
                if (node != null)
                {
                    num += (int)(node.Level * node.Distance);
                }
                else
                {
                    num += 0x3e8;
                }
                num -= architecture.Population / 0x2710;
            }
            return (num / section.ArchitectureCount);
        }

        public Routeway GetExistingRouteway(Architecture destination)
        {
            foreach (Routeway routeway in this.Routeways)
            {
                if (routeway.DestinationArchitecture == destination)
                {
                    return routeway;
                }
            }
            return null;
        }

        public Captive GetLowestLoyaltyCaptiveRecruitable()
        {
            GameObjectList list = this.Captives.GetRandomList();
            int lowestLoyalty = int.MaxValue;
            Captive target = null;
            if (list.Count > 0)
            {
                foreach (Captive c in list)
                {
                    int idealOffset = Person.GetIdealOffset(c.CaptivePerson, this.BelongedFaction.Leader);
                    if ((!Session.GlobalVariables.IdealTendencyValid || (idealOffset <= c.CaptivePerson.IdealTendency.Offset + (double)this.BelongedFaction.Reputation / Session.Current.Scenario.Parameters.MaxReputationForRecruit * 75))
                        && (!c.CaptivePerson.Hates(this.BelongedFaction.Leader)) && (!this.BelongedFaction.IsAlien || c.CaptivePerson.PersonalLoyalty < 2))
                    {
                        if (c.CaptivePerson.Loyalty < lowestLoyalty)
                        {
                            target = c;
                            lowestLoyalty = c.CaptivePerson.Loyalty;
                        }
                    }
                }
            }
            return target;
        }

        public Captive GetExtremeLoyaltyCaptive(bool low)
        {
            GameObjectList list = this.Captives.GetList();
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    list.PropertyName = "Loyalty";
                    list.IsNumber = true;
                    list.SmallToBig = low;
                    list.ReSort();
                }
                return (list[0] as Captive);
            }
            return null;
        }

        public Person GetLowestLoyaltyPersonRecruitable()
        {
            GameObjectList list = this.PersonsExcludeNvGuan.GetRandomList();
            int lowestLoyalty = int.MaxValue;
            Person target = null;
            if (list.Count > 0)
            {
                foreach (Person c in list)
                {
                    // 🛡️ 防止列表中的 null 数据导致崩溃
                    if (c == null) continue;
                    
                    // 🛡️ 防止 IdealTendency 为 null 导致崩溃
                    if (c.IdealTendency == null) continue;

                    int idealOffset = Person.GetIdealOffset(c, this.BelongedFaction.Leader);
                    if ((!Session.GlobalVariables.IdealTendencyValid || (idealOffset <= c.IdealTendency.Offset + (double)this.BelongedFaction.Reputation / Session.Current.Scenario.Parameters.MaxReputationForRecruit * 75))
                        && (!c.Hates(this.BelongedFaction.Leader)) && (!this.BelongedFaction.IsAlien || c.PersonalLoyalty < 2) &&
                        (!c.ProhibitedFactionID.ContainsValue(this.BelongedFaction.ID)) && c.GetRelation(c.VeryClosePersonInArchitecture) < 500)
                    {
                        if (c.Loyalty < lowestLoyalty)
                        {
                            target = c;
                            lowestLoyalty = c.Loyalty;
                        }
                    }
                }
            }
            return target;
        }

        public Person GetExtremeLoyaltyPerson(bool low)
        {
            GameObjectList list = this.Persons.GetList();
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    list.PropertyName = "Loyalty";
                    list.IsNumber = true;
                    list.SmallToBig = low;
                    list.ReSort();
                }
                return (list[0] as Person);
            }
            return null;
        }

        public Person GetExtremePersonFromWorkingList(ArchitectureWorkKind workKind, bool highest)  //大概是选择在冒泡小窗口说话的人
        {
            PersonList agricultureWorkingPersons = null;
            int num2;
            int num3;
            int workAbility;
            switch (workKind)
            {
                case ArchitectureWorkKind.赈灾:
                    agricultureWorkingPersons = this.ZhenzaiWorkingPersons;
                    break;
                case ArchitectureWorkKind.训练:
                    agricultureWorkingPersons = this.TrainingWorkingPersons;
                    break;
                case ArchitectureWorkKind.农业:
                    agricultureWorkingPersons = this.AgricultureWorkingPersons;
                    break;

                case ArchitectureWorkKind.商业:
                    agricultureWorkingPersons = this.CommerceWorkingPersons;
                    break;

                case ArchitectureWorkKind.技术:
                    agricultureWorkingPersons = this.TechnologyWorkingPersons;
                    break;

                case ArchitectureWorkKind.统治:
                    agricultureWorkingPersons = this.DominationWorkingPersons;
                    break;

                case ArchitectureWorkKind.民心:
                    agricultureWorkingPersons = this.MoraleWorkingPersons;
                    break;

                case ArchitectureWorkKind.耐久:
                    agricultureWorkingPersons = this.EnduranceWorkingPersons;
                    break;

                default:
                    return null;
            }
            if (agricultureWorkingPersons.Count == 0)
            {
                return null;
            }
            if (agricultureWorkingPersons.Count == 1)
            {
                return (agricultureWorkingPersons[0] as Person);
            }
            if (highest)
            {
                int num = 0;
                num2 = 0;
                for (num3 = 0; num3 < agricultureWorkingPersons.Count; num3++)
                {
                    workAbility = (agricultureWorkingPersons[num3] as Person).GetWorkAbility(workKind);
                    if (workAbility > num)
                    {
                        num = workAbility;
                        num2 = num3;
                    }
                }
                return (agricultureWorkingPersons[num2] as Person);
            }
            int num5 = 0x7fffffff;
            num2 = 0;
            for (num3 = 0; num3 < agricultureWorkingPersons.Count; num3++)
            {
                workAbility = (agricultureWorkingPersons[num3] as Person).GetWorkAbility(workKind);
                if (workAbility < num5)
                {
                    num5 = workAbility;
                    num2 = num3;
                }
            }
            return (agricultureWorkingPersons[num2] as Person);
        }

        public InformationKind GetFirstHalfInformationKind()
        {
            var allKinds = Session.Current.Scenario.GameCommonData.AllInformationKinds;
            // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - 情报类型总数: {allKinds.GameObjects.Count}");
            
            InformationKindList availList = allKinds.GetAvailList(this);
            // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - 可用情报类型数: {availList.Count}, 当前资金: {this.Fund}");
            
            if (availList.Count == 0)
            {
                // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - ❌ 没有可用的情报类型");
                return null;
            }
            
            InformationKindList list = new InformationKindList();
            foreach (InformationKind kind in availList)
            {
                if ((kind.Level <= InformationLevel.中) || GameObject.Chance(20))
                {
                    list.Add(kind);
                    // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - 添加情报类型 {kind.ID} ({kind.Name}): 等级={kind.Level}, 消耗={kind.CostFund}");
                }
            }
            
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    list.PropertyName = "FightingWeighing";
                    list.IsNumber = true;
                    list.ReSort();
                }
                var selectedObject = list[GameObject.Random(list.Count / 2)];
                var selected = (selectedObject is InformationKind ? (InformationKind)selectedObject : null);
                
                if (selected == null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - ❌ 类型转换失败: 对象类型为 {selectedObject?.GetType()?.Name ?? "null"}");
                    return null;
                }
                
                // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - ✅ 选择情报类型 {selected.ID} ({selected.Name})");
                return selected;
            }
            
            // System.Diagnostics.Debug.WriteLine($"[GetFirstHalfInformationKind] {this.Name} - ❌ 过滤后没有合适的情报类型");
            return null;
        }

        private Person GetFirstHalfPerson(string propertyName)
        {
            GameObjectList list = this.MovablePersons.GetList();
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    list.PropertyName = propertyName;
                    list.IsNumber = true;
                    list.ReSort();
                }
                return (list[GameObject.Random(list.Count / 2)] as Person);
            }
            return null;
        }

        public int GetFriendlyTroopFightingForceInView()
        {
            int num = 0;
            foreach (Point point in this.LongViewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if (((troopByPosition != null) && (troopByPosition.BelongedFaction != null)) && this.BelongedFaction.IsFriendly(troopByPosition.BelongedFaction))
                {
                    num += troopByPosition.FightingForce;
                }
            }
            return num;
        }

        public TroopList GetFriendlyTroopsInView()
        {
            GameArea longViewArea = this.LongViewArea;
            TroopList list = new TroopList();
            foreach (Point point in longViewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && troopByPosition.IsFriendly(this.BelongedFaction))
                {
                    list.Add(troopByPosition);
                }
            }
            return list;
        }

        public int GetGossipablePersonCount()
        {
            int num = 0;
            foreach (Person person in this.PersonsExcludeNvGuan)
            {
                if ((person.Loyalty <= 100) && (person != this.BelongedFaction.Leader))
                {
                    num++;
                }
            }
            return num;
        }



        public int DefendAssassinateAbility
        {
            get
            {
                int targetAbility = 0;
                foreach (Person p in this.PersonsExcludeNvGuan)
                {
                    if (p.AssassinateAbility > targetAbility)
                    {
                        targetAbility = p.AssassinateAbility;
                    }
                }

                return targetAbility;
            }
        }

        public PersonList GetAssassinatePersonTarget(Faction from)
        {
            PersonList pl = new PersonList();
            foreach (Person p in this.AssassinatablePersons(from))
            {
                pl.Add(p);
            }
            return pl;
        }

        public GameArea GetAssassinateArchitectureArea(Faction from)
        {
            GameArea area = new GameArea();

            void AddAreaIfTarget(Architecture arch)
            {
                if (IsAssassinateTarget(arch))
                {
                    foreach (Point point in arch.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }

            foreach (Architecture neighbor in this.AILandLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }

            return area;
        }

        public GameArea GetJailBreakArchitectureArea()
        {
            GameArea area = new GameArea();
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture.BelongedFaction != null && !this.IsFriendly(architecture.BelongedFaction) && architecture.HasFactionCaptive(this.BelongedFaction))
                {
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }

        public TroopList GetHostileTroopsInView()
        {
            GameArea viewArea = this.ViewArea;
            if (this.Endurance < this.EnduranceCeiling / 2)
            {
                viewArea = this.LongViewArea;
            }
            TroopList list = new TroopList();
            foreach (Point point in viewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && (!troopByPosition.IsFriendly(this.BelongedFaction) && (troopByPosition.Status != TroopStatus.埋伏)))
                {
                    int days = 1;
                    if ((((this.BelongedFaction != null) && (troopByPosition.BelongedFaction != null)) && (this.RecentlyAttacked <= 0)) && (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, troopByPosition.BelongedFaction.ID) >= 0))
                    {
                        days = 0;
                    }
                    if (troopByPosition.DaysToReachPosition(Session.Current.Scenario.GetClosestPoint(this.ArchitectureArea, troopByPosition.Position), days))
                    {
                        list.Add(troopByPosition);
                    }
                }
            }
            return list;
        }


        public bool FindHostileTroopInView()
        {


            GameArea viewArea = this.LongViewArea;


            foreach (Point point in viewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && !troopByPosition.IsTransport && (!troopByPosition.IsFriendly(this.BelongedFaction) && (troopByPosition.Status != TroopStatus.埋伏)))
                {
                    return true;
                }
            }
            return false;
        }

        public GameArea GetInstigateArchitectureArea()
        {
            GameArea area = new GameArea();
            
            void AddAreaIfTarget(Architecture arch)
            {
                if (IsInstigateTarget(arch))
                {
                    foreach (Point point in arch.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }

            foreach (Architecture neighbor in this.AILandLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                AddAreaIfTarget(neighbor);
                foreach (Architecture next in neighbor.AILandLinks) AddAreaIfTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) AddAreaIfTarget(next);
            }

            return area;
        }

        public MilitaryList GetLevelUpMilitaryList()
        {
            this.LevelUpMilitaryList.Clear();
            Dictionary<MilitaryKind, bool> canLevelUp = new Dictionary<MilitaryKind, bool>();
            foreach (Military military in this.Militaries)
            {
                bool hasLevelupable = false;
                foreach (int id in military.Kind.LevelUpKindID)
                {
                    bool flag = false;
                    if (canLevelUp.ContainsKey(military.Kind))
                    {
                        flag = canLevelUp[military.Kind];
                    }
                    else
                    {
                        flag = military.Kind.LevelUpAvail(this);
                        canLevelUp[military.Kind] = flag;
                    }
                    if (flag)
                    {
                        hasLevelupable = true;
                        break;
                    }
                }

                if (((military.InjuryQuantity == 0) && military.Kind != null && military.Kind.CanLevelUp) && (military.Experience >= military.Kind.LevelUpExperience)
                    && hasLevelupable)
                {
                    military.BelongedFaction = this.BelongedFaction;
                    this.LevelUpMilitaryList.AddMilitary(military);
                }
            }
            return this.LevelUpMilitaryList;
        }

        public MilitaryList GetMergeMilitaryList()
        {
            this.MergeMilitaryList.Clear();
            for (int i = 0; i < this.Militaries.Count; i++)
            {
                Military t = this.Militaries[i] as Military;
                if ((t.Quantity != t.Kind.MaxScale) && (t.InjuryQuantity <= 0))
                {
                    foreach (Military military2 in this.Militaries)
                    {
                        if (((t != military2) && (t.Kind.Equals(military2.Kind))) && ((military2.Quantity < military2.Kind.MaxScale) && (military2.InjuryQuantity == 0)))
                        {
                            this.MergeMilitaryList.Add(t);
                            break;
                        }
                    }
                }
            }
            return this.MergeMilitaryList;
        }

        public GameArea GetMilitaryCampaignArea(Military military)
        {
            GameArea allAvailableArea = this.GetAllAvailableArea(false);
            military.ModifyAreaByTerrainAdaptablity(allAvailableArea);
            return allAvailableArea;
        }

        public MilitaryKindList GetUpgradableMilitaryKindList(Military m)
        {
            this.UpgradableMilitaryKindList.Clear();
            foreach (MilitaryKind mk in m.Kind.GetLevelUpKinds(this))
            {
                this.UpgradableMilitaryKindList.Add(mk);
            }
            return this.UpgradableMilitaryKindList;
        }

        public MilitaryKindList GetNewMilitaryKindList()
        {
            this.NewMilitaryKindList.Clear();
            foreach (MilitaryKind kind in this.BelongedFaction.AvailableMilitaryKinds.MilitaryKinds.Values)
            {
                if (kind.CreateAvail(this))
                {
                    this.NewMilitaryKindList.Add(kind);
                }
            }
            foreach (MilitaryKind kind in this.PrivateMilitaryKinds.MilitaryKinds.Values)
            {
                if (kind.CreateAvail(this) && !this.NewMilitaryKindList.GameObjects.Contains(kind))
                {
                    this.NewMilitaryKindList.Add(kind);
                }
            }
            return this.NewMilitaryKindList;
        }

        public ArchitectureList GetOtherArchitectureList()
        {
            this.OtherArchitectureList.Clear();

            if (Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                if (this.BelongedSection != null)
                {
                    foreach (Architecture architecture in this.BelongedSection.Architectures)
                    {
                        if (architecture != this)
                        {
                            this.OtherArchitectureList.Add(architecture);
                        }
                    }
                }
            }

            if (this.OtherArchitectureList.Count == 0)
            {
                if (this.BelongedFaction != null)
                {
                    foreach (Architecture architecture in this.BelongedFaction.Architectures)
                    {
                        if (architecture != this)
                        {
                            this.OtherArchitectureList.Add(architecture);
                        }
                    }
                }
            }

            return this.OtherArchitectureList;
        }


        public ArchitectureList jingongjianzhuliebiao()
        {
            ArchitectureList jianzhuliebiao = new ArchitectureList();
            if (Session.Current.Scenario.youhuangdi())
            {
                jianzhuliebiao.Add(Session.Current.Scenario.huangdisuozaijianzhu());
            }
            return jianzhuliebiao;
        }

        public PersonList PersonConveneList = new PersonList();
        public PersonList GetPersonConveneList()
        {
            PersonList result = new PersonList();
            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {
                if (architecture != this)
                {
                    foreach (Person person in architecture.MovablePersons)
                    {
                        result.Add(person);
                    }
                }
            }
            PersonConveneList = result;
            return result;
        }

        public PersonList GetPersonListExceptLeader()
        {
            PersonList list = new PersonList();
            if (this.BelongedFaction != null)
            {
                foreach (Person person in this.Persons)
                {
                    if (person != this.BelongedFaction.Leader)
                    {
                        list.Add(person);
                    }
                }
            }
            return list;
        }

        public PersonList PersonStudySkillList = new PersonList();
        public PersonList GetPersonStudySkillList()
        {
            PersonList result = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableSkill)
                {
                    result.Add(person);
                }
            }
            PersonStudySkillList = result;
            return result;
        }

        public PersonList PersonStudyStuntList = new PersonList();
        public PersonList GetPersonStudyStuntList()
        {
            PersonList result = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableStunt)
                {
                    result.Add(person);
                }
            }
            PersonStudyStuntList = result;
            return result;
        }

        public PersonList PersonStudyTitleList = new PersonList();
        public PersonList GetPersonStudyTitleList()
        {
            PersonList result = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableTitle)
                {
                    result.Add(person);
                }
            }
            PersonStudyTitleList = result;
            return result;
        }

        public GameArea GetRealTroopEnterableArea(Troop troop)
        {
            GameArea area = new GameArea();
            foreach (Point point in this.GetTroopEnterableArea(troop).Area)
            {
                //if (!Session.Current.Scenario.PositionIsTroop(point))
                //{
                area.AddPoint(point);
                //}
            }
            return area;
        }

        public MilitaryList GetRecruitmentMilitaryList()
        {
            this.RecruitmentMilitaryList.Clear();
            foreach (Military military in this.Militaries)
            {
                // 🔥 FIX V1.1: 移除伤兵限制，但添加溢出保护
                // 原逻辑: if ((military.Quantity < military.Kind.MaxScale) && (military.InjuryQuantity == 0))
                // 问题: 一旦有伤兵就停止招募，导致编队兵力越打越少且无法补充，形成死循环
                // 修改: 只要总兵力(健康+伤兵)未达到最大编制，就允许招募
                // 安全检查: 防止招募溢出，确保 TotalQuantity 不超过 MaxScale
                if (military.Quantity + military.InjuryQuantity < military.Kind.MaxScale)
                {
                    this.RecruitmentMilitaryList.AddMilitary(military);
                }
            }
            return this.RecruitmentMilitaryList;
        }



        public CaptiveList GetRedeemCaptiveList()
        {
            this.RedeemCaptiveList.Clear();
            foreach (Captive captive in this.BelongedFaction.SelfCaptives)
            {
                if ((captive.RansomArriveDays == 0) && (captive.Ransom <= this.Fund) && captive.BelongedFaction != null)
                {
                    this.RedeemCaptiveList.Add(captive);
                }
            }
            return this.RedeemCaptiveList;
        }

        public int GetRelationUnderZeroTroopFightingForceInView(out float rationRate)
        {
            int num = 0;
            rationRate = 0f;
            int num2 = 0;
            foreach (Point point in this.LongViewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if (((troopByPosition != null) && (troopByPosition.BelongedFaction != null)) && (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, troopByPosition.BelongedFaction.ID) < 0))
                {
                    rationRate += ((float)troopByPosition.RationDaysLeft) / ((float)troopByPosition.RationDays);
                    num2++;
                    num += troopByPosition.FightingForce;
                }
            }
            if (num2 > 1)
            {
                rationRate /= (float)num2;
            }
            return num;
        }

        public GameObjectList GetResetDiplomaticRelationList()
        {
            this.ResetDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (display.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold && (display.LinkedFaction1 != null) && (display.LinkedFaction2 != null))
                    {
                        this.ResetDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.ResetDiplomaticRelationList;
        }

        public GameObjectList GetEnhanceDiplomaticRelationList()
        {
            this.EnhanceDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if ((display.LinkedFaction1 != null) && (display.LinkedFaction2 != null))
                    {
                        this.EnhanceDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.EnhanceDiplomaticRelationList;
        }

        public GameObjectList GetAllyDiplomaticRelationList()
        {
            this.AllyDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if ((display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold && display.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold * 0.9) && ((display.LinkedFaction1 != null) && (display.LinkedFaction2 != null)))
                    {
                        this.AllyDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.AllyDiplomaticRelationList;
        }

        public GameObjectList GetTruceDiplomaticRelationList()
        {
            this.TruceDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (((display.LinkedFaction1 != null) && (display.LinkedFaction2 != null)) && display.Truce < 1)
                    {
                        this.TruceDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.TruceDiplomaticRelationList;
        }

        public GameObjectList GetQuanXiangDiplomaticRelationList() //劝降
        {
            this.QuanXiangDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null && Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold && (display.LinkedFaction1 != null) && (display.LinkedFaction2 != null)
                          && (this.BelongedFaction.AdjecentFactionList.GameObjects.Contains(display.LinkedFaction2) || this.BelongedFaction.AdjecentFactionList.GameObjects.Contains(display.LinkedFaction1)))
                    {
                        this.QuanXiangDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.QuanXiangDiplomaticRelationList;
        }
        /*
        private GameObjectList AIQuanXiangDiplomaticRelationList = new GameObjectList ();
        public GameObjectList GetAIQuanXiangDiplomaticRelationList()
        {
            this.AIQuanXiangDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null && !Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold && (display.LinkedFaction1 != null) && (display.LinkedFaction2 != null)
                          && (this.BelongedFaction.AdjecentFactionList.GameObjects.Contains(display.LinkedFaction2) || this.BelongedFaction.AdjecentFactionList.GameObjects.Contains(display.LinkedFaction1)))
                    {

                        //foreach (Faction f in this.BelongedFaction.AdjecentFactionList)
                        {
                            //if (this.BelongedFaction.Reputation > f.Reputation  && this.BelongedFaction.Army != 0 && this.BelongedFaction.Army >= f.Army * 5)
                            {
                                this.AIQuanXiangDiplomaticRelationList.Add(display);
                            }
                        }
                    }

                }
            }
            return this.AIQuanXiangDiplomaticRelationList;
        }
        */

        public GameObjectList GetDenounceDiplomaticRelationList()
        {
            this.DenounceDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold && (display.LinkedFaction1 != null) && (display.LinkedFaction2 != null))
                    {
                        this.DenounceDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.DenounceDiplomaticRelationList;
        }

        public PersonList GetRewardPersons()
        {
            this.RewardPersonList.Clear();
            
            // 使用AI打分逻辑筛选和排序武将
            AI_RewardManager rewardManager = new();
            PersonList sortedCandidates = rewardManager.GetRewardCandidatesForUI(this);
            
            // 自动勾选前N个武将（根据资金情况）
            int rewardCost = Session.Parameters.RewardPersonCost;
            int availableFund = this.BelongedFaction.Fund;
            int maxRewardCount = availableFund / rewardCost;
            
            for (int i = 0; i < sortedCandidates.Count; i++)
            {
                // PersonList 保证类型安全，直接转换
                Person person = (Person)sortedCandidates[i];
                
                this.RewardPersonList.Add(person);
                
                // 自动勾选前N个（资金允许的情况下）
                if (i < maxRewardCount)
                {
                    person.Selected = true;
                }
            }
            
            return this.RewardPersonList;
        }

        public Routeway GetRouteway(LinkNode node, bool hasEnd)
        {
            foreach (Routeway routeway in this.Routeways)
            {
                if ((routeway.DestinationArchitecture == node.A) && (!hasEnd || (routeway.EndArchitecture == node.A)))
                {
                    return routeway;
                }
            }
            return this.BuildRouteway(node, hasEnd);
        }

        public ArchitectureList GetRoutewayDestinationArchitectureList()
        {
            this.RoutewayDestinationArchitectures.Clear();
            this.RoutewayProcedures.Clear();
            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {
                architecture.surplusRate = 0f;
                architecture.PathRoutewayID = -1;
            }
            this.surplusRate = 1f;
            this.RoutewayProcedures.Enqueue(new RoutewayProcedureDetail(this, 1f));
            while (this.RoutewayProcedures.Count > 0)
            {
                RoutewayProcedureDetail detail = this.RoutewayProcedures.Dequeue();
                this.AddCloseRoutewayDestinationArchitectures(detail.Start, detail.PreviousRate);
            }
            return this.RoutewayDestinationArchitectureList;
        }

        public GameArea GetRoutewayStartArea()
        {
            return this.GetAllContactArea().GetContactArea(false);
        }

        public GameArea GetRoutewayStartPoints()
        {
            GameArea area = new GameArea();
            foreach (Point point in this.GetRoutewayStartArea().Area)
            {
                if (this.IsRoutewayPossible(point))
                {
                    area.AddPoint(point);
                }
            }
            if (area.Count == 0)
            {
                foreach (Point point in this.ContactArea.Area)
                {
                    if (this.IsRoutewayPossible(point))
                    {
                        area.AddPoint(point);
                    }
                }
            }
            if (area.Count == 0)
            {
                foreach (Point point in this.LongViewArea.Area)
                {
                    if (this.IsRoutewayPossible(point))
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }

        public MilitaryList GetShelledMilitaryList(MilitaryType militaryType)
        {
            this.ShelledMilitaryList.Clear();
            foreach (Military military in this.Militaries)
            {
                if (((military.Quantity > 0) && (military.Morale > 0)) && (military.Kind.Type != militaryType))
                {
                    this.ShelledMilitaryList.AddMilitary(military);
                }
            }
            return this.ShelledMilitaryList;
        }

        /*
        public GameArea GetSpyArchitectureArea()
        {
            GameArea area = new GameArea();
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if ((architecture.BelongedFaction != null) && (architecture.BelongedFaction != this.BelongedFaction))
                {
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }
         */

        public MilitaryList GetTrainingMilitaryList()
        {
            this.TrainingMilitaryList.Clear();
            foreach (Military military in this.Militaries)
            {
                if ((military.Quantity > 0) && ((military.Morale < military.MoraleCeiling) || (military.Combativity < military.CombativityCeiling)))
                {
                    this.TrainingMilitaryList.AddMilitary(military);
                }
            }
            return this.TrainingMilitaryList;
        }

        public ArchitectureList GetTransferArchitectureList()
        {
            this.TransferArchitectureList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture != this)
                    {
                        this.TransferArchitectureList.Add(architecture);
                    }
                }
            }
            return this.TransferArchitectureList;
        }

        public GameArea GetTroopEnterableArea(Troop troop)
        {
            GameArea area = new GameArea();
            foreach (Point point in this.ArchitectureArea.Area)
            {
                if (Session.Current.Scenario.GetWaterPositionMapCost(troop.Army.Kind, point) < 3500)
                {
                    area.AddPoint(point);
                }
            }
            foreach (Point point in this.ContactArea.Area)
            {
                if (troop.IsMovableOnPosition(point) && (Session.Current.Scenario.GetWaterPositionMapCost(troop.Army.Kind, point) < 3500))
                {
                    area.AddPoint(point);
                }
            }
            return area;
        }

        public bool GossipAvail()
        {
            return ((this.MovablePersons.Count > 0 && (this.Fund >= this.GossipArchitectureFund)) && this.HasAnyGossipTarget());
        }

        private bool HasAnyGossipTarget()
        {
            foreach (Architecture neighbor in this.AILandLinks)
            {
                if (IsGossipTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsGossipTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsGossipTarget(next)) return true;
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                if (IsGossipTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsGossipTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsGossipTarget(next)) return true;
            }
            return false;
        }

        private bool IsGossipTarget(Architecture arch)
        {
            if (arch != null && arch.BelongedFaction != null && !this.IsFriendly(arch.BelongedFaction))
            {
                if (this.BelongedFaction.IsArchitectureKnown(arch))
                {
                    if (arch.Population > 0 || arch.Domination > 0) return true;
                }
            }
            return false;
        }

        public bool JailBreakAvail()
        {
            return ((this.MovablePersons.Count > 0 && (this.Fund >= this.JailBreakArchitectureFund)) && (this.GetJailBreakArchitectureArea().Count > 0));
        }

        public bool AssassinateAvail()
        {
            return this.MovablePersons.Count > 0 && this.HasAnyAssassinateTarget();
        }

        private bool HasAnyAssassinateTarget()
        {
            foreach (Architecture neighbor in this.AILandLinks)
            {
                if (IsAssassinateTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsAssassinateTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsAssassinateTarget(next)) return true;
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                if (IsAssassinateTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsAssassinateTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsAssassinateTarget(next)) return true;
            }
            return false;
        }

        private bool IsAssassinateTarget(Architecture arch)
        {
            if (arch != null && arch.BelongedFaction != null && !this.IsFriendly(arch.BelongedFaction))
            {
                if (this.BelongedFaction.IsArchitectureKnown(arch))
                {
                    if (arch.AssassinatablePersons(this.BelongedFaction).Count > 0) return true;
                }
            }
            return false;
        }

        private void characteristicsDoWork()
        {
            foreach (Influence i in this.Characteristics.Influences.Values)
            {
                i.DoWork(this);
            }
        }

        private void HandleFacilities()
        {
            this.CheckBuildingFacility();
            this.FacilityMaintenance();
            this.FacilityRecovery();
            this.FacilityDoWork();
        }

        public bool HasAnyPerson()
        {
            return ((this.Persons.Count > 0) || (this.MovingPersons.Count > 0));
        }

        public bool HasCampaignableMilitary()
        {
            foreach (Military military in this.Militaries)
            {
                if (((military.Quantity > 0) && (military.Morale > 0)) && (military.InjuryQuantity < military.Kind.MinScale))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasCaptive()
        {
            return (this.CaptiveCount > 0);
        }

        public bool HasFactionCaptive(Faction f)
        {
            foreach (Captive c in this.Captives)
            {
                if (c.CaptiveFaction == f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasCloserOffensiveArchitecture(LinkNode node, out Architecture closer)
        {
            closer = null;
            foreach (Architecture architecture in this.BelongedFaction.Architectures)
            {
                if (architecture != this)
                {
                    LinkNode node2 = null;
                    architecture.AIAllLinkNodes.TryGetValue(node.A.ID, out node2);
                    if (((node2 != null) && (node2.Level < node.Level)) && (node2.Kind != LinkKind.None))
                    {
                        closer = architecture;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasContactHostileTroop(Faction faction)
        {
            foreach (Point point in this.GetAllContactArea().Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && !troopByPosition.IsFriendly(faction))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasEnoughForceOffensiveMilitary()
        {
            foreach (Military military in this.Militaries)
            {
                if (this.IsOffensiveMilitary(military) && (military.Scales >= 30))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasExperiencedLeaderMilitary(Person person)
        {
            foreach (Military military in this.Militaries)
            {
                if ((military.Leader == person) && (military.LeaderExperience >= 200))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasPrincess()
        {
            return (this.Feiziliebiao.Count > 0);
        }

        public bool HasLandLink()
        {
            return (this.AILandLinks.Count > 0);
        }

        public bool HasWaterLink()
        {
            return (this.AIWaterLinks.Count > 0);
        }

        public bool HasFacility()
        {
            return (this.FacilityCount > 0);
        }

        // [新增] 检查是否有酒馆设施
        public bool HasPub()
        {
            // 简单实现：检查是否有相关设施
            return this.HasFacility();
        }

        // [新增] 获取酒馆费用
        public int GetPubCost()
        {
            // 简单实现：返回基础费用
            return 100;
        }

        public bool HaskechaichuFacility()
        {
            return (this.kechaichudesheshi().Count > 0);
        }
        public bool HasFaction()
        {
            return (this.BelongedFaction != null);
        }

        public bool ArchitectureEditMode()
        {
            // 只有在作弊模式开启时才显示编辑菜单
            return Session.GlobalVariables.EnableCheat;
        }

        public bool HasFactionInClose(Faction faction, int level)
        {
            foreach (LinkNode node in this.AIAllLinkNodes.Values)
            {
                if (node.Level > level)
                {
                    return false;
                }
                if (node.A.BelongedFaction == faction)
                {
                    return true;
                }
            }
            return false;
        }

        /*
        public bool HasFactionSpy(Faction faction)
        {
            foreach (SpyPack pack in this.SpyPacks)
            {
                if (pack.SpyPerson.BelongedFaction == faction)
                {
                    return true;
                }
            }
            return false;
        }
        */

        public bool HasFollowedLeaderMilitary(Person person)
        {
            foreach (Military military in this.Militaries)
            {
                if (military.FollowedLeader == person)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasHostileArchitectureOnPath(LinkNode node)
        {
            foreach (Architecture architecture in node.Path)
            {
                if (((architecture != this) && (architecture != node.A)) && !((architecture.BelongedFaction == null) || this.IsFriendlyWithoutTruce(architecture.BelongedFaction)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasHostileTroopsInArchitecture()
        {
            foreach (Point point in this.ArchitectureArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && !troopByPosition.IsFriendly(this.BelongedFaction))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasOwnFactionTroopsInArchitecture()
        {
            foreach (Point point in this.ArchitectureArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && troopByPosition.BelongedFaction == this.BelongedFaction)
                {
                    return true;
                }
            }
            return false;
        }

        public void ResetLandLink(GameObjectList gameObjectList)
        {
            foreach (Architecture architecture in this.AILandLinks)
            {
                architecture.AILandLinks.Remove(this);
            }
            this.AILandLinks.Clear();
            foreach (GameObject gameObject in gameObjectList)
            {
                Architecture architecture = (gameObject is Architecture ? (Architecture)gameObject : null);
                if (architecture != null)
                {
                    this.AILandLinks.Add(architecture);
                }
            }
            foreach (Architecture architecture in this.AILandLinks)
            {
                architecture.AILandLinks.Add(this);
            }
        }

        public void ResetWaterLink(GameObjectList gameObjectList)
        {
            foreach (Architecture architecture in this.AIWaterLinks)
            {
                architecture.AIWaterLinks.Remove(this);
            }
            this.AIWaterLinks.Clear();
            foreach (GameObject gameObject in gameObjectList)
            {
                Architecture architecture = (gameObject is Architecture ? (Architecture)gameObject : null);
                if (architecture != null)
                {
                    this.AIWaterLinks.Add(architecture);
                }
            }
            foreach (Architecture architecture in this.AIWaterLinks)
            {
                architecture.AIWaterLinks.Add(this);
            }
        }

        public bool HasHostileTroopsInView()
        {
            if (Session.GlobalVariables.AIQuickBattle)
            {
                foreach (Architecture a in Session.Current.Scenario.Architectures)
                {
                    if (a.AIBattlingArchitectures.GameObjects.Contains(this))
                    {
                        return true;
                    }
                }
            }

            GameArea viewArea = this.ViewArea;
            if ((this.RecentlyAttacked > 0) || (this.ArmyScale > this.NormalArmyScale))
            {
                viewArea = this.LongViewArea;
            }
            
            foreach (Point point in viewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if ((troopByPosition != null) && (!troopByPosition.IsFriendly(this.BelongedFaction) && (troopByPosition.Status != TroopStatus.埋伏)))
                {
                    return true;
                }
            }
            
            return false;
        }

        public bool HasOwnFactionTroopsInView()
        {
            if (Session.GlobalVariables.AIQuickBattle)
            {
                foreach (Architecture a in Session.Current.Scenario.Architectures)
                {
                    if (a.AIBattlingArchitectures.GameObjects.Contains(this))
                    {
                        return true;
                    }
                }
            }

            GameArea viewArea = this.LongViewArea;
            foreach (Point point in viewArea.Area)
            {
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                if (troopByPosition != null && troopByPosition.BelongedFaction == this.BelongedFaction)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasMilitary()
        {
            return (this.Militaries.Count > 0);
        }

        public bool HasMovingPerson()
        {
            return (this.MovingPersons.Count > 0);
        }

        public bool HasNoFactionPerson()
        {
            return (this.NoFactionPersonCount > 0);
        }

        public bool HasOffensiveMilitary()
        {
            foreach (Military military in this.Militaries)
            {
                if (this.IsOffensiveMilitary(military))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasOffensiveSectionInClose(out Section section, int level)
        {
            section = null;
            foreach (LinkNode node in this.AIAllLinkNodes.Values)
            {
                if (node.Level > level)
                {
                    return false;
                }
                if (((node.A.BelongedSection != null) && (node.A.BelongedSection.BelongedFaction == this.BelongedFaction)) && node.A.BelongedSection.AIDetail.ValueOffensiveCampaign)
                {
                    section = node.A.BelongedSection;
                    return true;
                }
            }
            return false;
        }

        public bool HasPerson()
        {
            return (this.Persons.Count > 0);
        }

        public bool HasRelationUnderZeroHostileTroopsInView()
        {
            if (this.BelongedFaction != null)
            {
                GameArea viewArea = this.ViewArea;
                if (_architectureKind.HasLongView && (this.ArmyScale < this.NormalArmyScale))
                {
                    viewArea = this.LongViewArea;
                }
                foreach (Point point in viewArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if ((((troopByPosition != null) && (troopByPosition.BelongedFaction != null)) && (troopByPosition.Status != TroopStatus.埋伏)) && (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, troopByPosition.BelongedFaction.ID) < 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasRouteway(Architecture destination)
        {
            foreach (Routeway routeway in this.Routeways)
            {
                if (routeway.DestinationArchitecture == destination)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasRouteway(LinkNode node, bool hasEnd)
        {
            float rate = 1f;
            foreach (Routeway routeway in this.Routeways)
            {
                if (((routeway.DestinationArchitecture == node.A) && (!hasEnd || (routeway.EndArchitecture == node.A))) && (routeway.LastPoint.ConsumptionRate <= this.BelongedFaction.RoutewayPathBuilder.ConsumptionMax))
                {
                    return true;
                }
            }
            return this.FindRouteway(node, hasEnd, out rate);
        }

        public bool HasRouteway(LinkNode node, bool hasEnd, out float rate)
        {
            foreach (Routeway routeway in this.Routeways)
            {
                if (((routeway.DestinationArchitecture == node.A) && (!hasEnd || (routeway.EndArchitecture == node.A))) && (routeway.LastPoint.ConsumptionRate <= this.BelongedFaction.RoutewayPathBuilder.ConsumptionMax))
                {
                    rate = routeway.LastPoint.ConsumptionRate;
                    return true;
                }
            }
            return this.FindRouteway(node, hasEnd, out rate);
        }

        public RoutewayList HasRoutewayList(Architecture destination)
        {
            RoutewayList list = new RoutewayList();
            foreach (Routeway routeway in this.Routeways)
            {
                if (routeway.DestinationArchitecture == destination)
                {
                    list.Add(routeway);
                }
            }
            return list;
        }

        public bool HasInformation()
        {
            return this.Informations.Count > 0;
        }

        public bool FactionHasInformation()
        {
            return this.BelongedFaction != null && this.BelongedFaction.HasInformation();
        }

        public int ShuijunMilitaryCount
        {
            get
            {
                int result = 0;
                foreach (Military m in this.Militaries)
                {
                    if (m.Kind.Type == MilitaryType.水军)
                    {
                        result++;
                    }
                }
                return result;
            }
        }

        public bool HasShuijun()
        {
            return ShuijunMilitaryCount > 0;
        }

        public bool HasShuijunMilitaryKind()
        {
            foreach (MilitaryKind kind in this.BelongedFaction.AvailableMilitaryKinds.MilitaryKinds.Values)
            {
                if (kind.Type == MilitaryType.水军 && !kind.IsTransport && kind.ID != 28)
                {
                    return true;
                }
            }
            foreach (MilitaryKind kind in this.PrivateMilitaryKinds.MilitaryKinds.Values)
            {
                if (kind.Type == MilitaryType.水军 && !kind.IsTransport && kind.ID != 28)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasMarriageToMake()
        {
            return this.makeMarryablePersons().Count > 0;
        }

        public bool HasMarriageToMake2()
        {
            return this.MakeMarryablePersons2().Count > 0;
        }

        public bool HasChildrenToTrain()
        {
            return this.BelongedFaction.Leader.TrainableChildren.Count > 0;
        }

        public bool HasUnavailablePerson(PersonList personlist)
        {
            foreach (Person person in personlist)
            {
                if (person.LocationArchitecture == null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasWorkingPerson()
        {
            foreach (Person person in this.Persons)
            {
                if (person.WorkKind != ArchitectureWorkKind.无)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HigtViewTroop(Troop troop)
        {
            return (this.ViewArea.HasPoint(troop.Position) && (((this.BelongedFaction != null) && this.IsFriendly(troop.BelongedFaction)) || (troop.Status != TroopStatus.埋伏)));
        }

        public void IncreaseAgriculture(int increment)
        {
            if (this.AgricultureCeiling == 0) return;
            float actualIncrement = increment > 0 ? increment * (1-(float)this.Agriculture / this.AgricultureCeiling) : increment;
            this.Agriculture += (int) Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Agriculture++;
            }

            if (this.Agriculture > this.AgricultureCeiling)
            {
                this.Agriculture = this.AgricultureCeiling;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            this.InvalidateInfluenceCache();
        }

        public void IncreaseCommerce(int increment)
        {
            if (this.CommerceCeiling == 0) return;
            float actualIncrement = increment > 0 ? increment * (1-(float)this.Commerce / this.CommerceCeiling) : increment;
            this.Commerce += (int)Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Commerce++;
            }

            if (this.Commerce > this.CommerceCeiling)
            {
                this.Commerce = this.CommerceCeiling;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            this.InvalidateInfluenceCache();
        }

        public int IncreaseDomination(int increment)
        {
            if (this.DominationCeiling == 0) return 0;
            int old = this.Domination;

            // 🔥 IMPROVED: 改进的增长公式，避免在低值时增长过慢
            float progress = (float)this.Domination / this.DominationCeiling;
            float actualIncrement;
            
            if (increment > 0)
            {
                // 在前70%时正常增长，后30%时才开始递减
                if (progress < 0.7f)
                {
                    actualIncrement = increment; // 前70%全速增长
                }
                else
                {
                    // 后30%时才开始递减，但保持最低30%的增长速度
                    float reductionFactor = Math.Max(0.3f, 1.0f - (progress - 0.7f) / 0.3f);
                    actualIncrement = increment * reductionFactor;
                }
                
#if DEBUG
                if (this.Domination >= 45 && this.Domination <= 55)
                {
                    System.Diagnostics.Debug.WriteLine($"[统治增长] {this.Name}: {this.Domination}→{this.Domination + (int)actualIncrement} " +
                        $"(进度={progress:P1}, 系数={actualIncrement/increment:F2})");
                }
#endif
            }
            else
            {
                actualIncrement = increment; // 负增长不受影响
            }
            
            this.Domination += (int)Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Domination++;
            }

            if (this.Domination > this.DominationCeiling)
            {
                this.Domination = this.DominationCeiling;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            if (this.Domination != old)
            {
                this.InvalidateInfluenceCache();
            }

            return this.Domination - old;
        }

        public int IncreaseEndurance(int increment)
        {
            if (increment <= 0)
            {
                return 0;
            }
            if (this.EnduranceCeiling == 0) return 0;

            int old = this.Endurance;

            float actualIncrement = increment > 0 ? increment * (1-(float)this.Endurance / this.EnduranceCeiling):increment;
            this.Endurance += (int)Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Endurance++;
            }

            if (this.Endurance > this.EnduranceCeiling)
            {
                this.Endurance = this.EnduranceCeiling;
            }

            if (this.Endurance == 0)
            {
                this.WallStateChange();
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            if (this.Endurance != old)
            {
                this.InvalidateInfluenceCache();
            }

            return this.Endurance - old;
        }

        public void IncreaseFood(int increment)
        {
            if ((increment + this.food) > this.FoodCeiling)
            {
                increment = this.FoodCeiling - this.food;
            }
            this.food += increment;
            this.IncrementNumberList.AddNumber(increment, CombatNumberKind.粮草, this.Position);
            this.ShowNumber = true;
        }

        public void IncreaseFund(int increment)
        {
            if ((increment + this.fund) > this.FundCeiling)
            {
                increment = this.FundCeiling - this.fund;
            }
            this.fund += increment;
            this.IncrementNumberList.AddNumber(increment, CombatNumberKind.资金, this.Position);
            this.ShowNumber = true;
        }

        public void IncreaseMilitaryPopulation(int increment)
        {
            this.militaryPopulation += increment;

            if (this.militaryPopulation > this.PopulationCeiling)
            {
                this.militaryPopulation = this.PopulationCeiling;
            }

            if (this.militaryPopulation < 0)
            {
                this.militaryPopulation = 0;
            }


        }



        public void IncreaseMorale(int increment)
        {
            if (this.MoraleCeiling == 0) return;
            int old = this.Morale;
            
            float actualIncrement = increment > 0 ? increment * (1-(float)this.Morale / this.MoraleCeiling) : increment;
            this.Morale += (int)Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Morale++;
            }

            if (this.Morale > this.MoraleCeiling)
            {
                this.Morale = this.MoraleCeiling;
            }
            
            // 🆕 内政变化时清除势力范围缓存（2026-03-11）
            if (this.Morale != old)
            {
                this.InvalidateInfluenceCache();
            }
        }

        public int IncreasePopulation(int increment)
        {
            if (this.PopulationCeiling == 0) return 0;
            int old = this.Population;

            this.Population += increment;

            if (this.Population > this.PopulationCeiling)
            {
                this.Population = this.PopulationCeiling;
            }

            if (this.population < 0)
            {
                this.population = 0;
            }

            return this.Population - old;
        }

        public void IncreaseTechnology(int increment)
        {
            if (this.TechnologyCeiling == 0) return;
            float actualIncrement = increment > 0 ? increment * (1-(float)this.Technology / this.TechnologyCeiling) : increment;
            this.Technology += (int)Math.Floor(actualIncrement);
            if (GameObject.Random(1000000) < (actualIncrement - Math.Floor(actualIncrement)) * 1000000)
            {
                this.Technology++;
            }

            if (this.Technology > this.TechnologyCeiling)
            {
                this.Technology = this.TechnologyCeiling;
            }
        }

        private void IncreaseViewAreaCombativity()
        {
            if (this.IncrementOfCombativityInViewArea > 0)
            {
                foreach (Point point in this.ViewArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if ((troopByPosition != null) && this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction))
                    {
                        troopByPosition.IncreaseCombativity(this.IncrementOfCombativityInViewArea);
                    }
                }
            }
        }

        public bool InformationAvail()
        {
            // 基础条件：有可移动人员 + 有可用的情报类型（资金足够）
            bool hasMovablePersons = this.MovablePersons.Count > 0;
            bool hasAvailItems = Session.Current.Scenario.GameCommonData.AllInformationKinds.HasAvailItem(this);
            
            if (!hasMovablePersons || !hasAvailItems)
            {
                return false;
            }
            
            // 🔥 AI专用限制：如果是AI势力，需要额外检查
            if (!this.CurrentPlayerOwned())
            {
                return InformationAvailForAI();
            }
            
            // 玩家：只检查基础条件
            return true;
        }
        
        /// <summary>
        /// AI专用的情报可用性判断
        /// 限制条件：在有敌军威胁时，至少保留2个人或2个建筑
        /// </summary>
        private bool InformationAvailForAI()
        {
            // 如果只有1个建筑且只有1个可移动人员
            if (this.BelongedFaction.Architectures.Count == 1 && this.MovablePersons.Count == 1)
            {
                // 检查是否有敌军威胁
                bool hasHostileThreat = HasHostileTroopsInView() || HasContactHostileTroop(this.BelongedFaction);
                
                // 有敌军威胁时不允许派出唯一的人
                if (hasHostileThreat)
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool StopInformationAvail()
        {
            return this.Informations.Count > 0;
        }

        public int InformationDayCost
        {
            get
            {
                int sum = 0;
                foreach (Information i in this.Informations)
                {
                    sum += i.DayCost;
                }
                return sum;
            }
        }

        public string InformationCostString
        {
            get
            {
                return this.InformationDayCost * 30 + "/月";
            }
        }

        public void RemoveAllInformations()
        {
            foreach (Information information in this.Informations.GetList())
            {
                information.Purify();
                this.RemoveInformation(information);
                Session.Current.Scenario.Informations.Remove(information);
            }
        }

        private void InformationDayEvent()
        {
            int cost = this.InformationDayCost;
            if (this.Fund >= cost)
            {
                this.DecreaseFund(cost);
            }
            else
            {
                this.RemoveAllInformations();
            }
            //foreach (Information information in this.Informations)
            //{
            //    information.CheckAmbushTroop();
            //    information.DaysStarted++;
            //}
        }

        private void InsideTacticsAI()
        {
            // 内部策略（学习技能、称号等）是基础策略，委任军区可以执行
            if (!ShouldExecuteBasicAI()) return;
            
            //if (((this.PlanArchitecture == null) || GameObject.Chance(10)) && this.HasPerson())
            if (this.HasPerson())
            {
                if (((this.RecentlyAttacked <= 0) && (this.PlanArchitecture == null)) && !this.HasHostileTroopsInView())
                {
                    //Label_0221:
                    foreach (Person person in this.Persons.GetList())
                    {
                        if (((!this.withoutTruceFrontline || !GameObject.Chance(5)) && !GameObject.Chance(20)) || (GameObject.Random(Session.Current.Scenario.Date.Day) < GameObject.Random(30)))
                        {
                            continue;
                        }
                        if (GameObject.Chance(100 - Session.Parameters.AutoLearnSkillSuccessRate * Session.Parameters.LearnSkillDays) && person.HasLearnableSkill)
                        {
                            person.GoForStudySkill();
                        }
                        else if (GameObject.Chance(50))
                        {
                            List<Title> higherLevelLearnableTitle = person.HigherLevelLearnableTitle;
                            if (higherLevelLearnableTitle.Count > 0)
                            {
                                person.GoForStudyTitle(higherLevelLearnableTitle[GameObject.Random(higherLevelLearnableTitle.Count)]);
                            }
                        }
                        else if (Session.Current.Scenario.GameCommonData.AllStunts.Count > person.StuntCount)
                        {
                            foreach (Stunt stunt in Session.Current.Scenario.GameCommonData.AllStunts.GetStuntList().GetRandomList())
                            {
                                if ((person.Stunts.GetStunt(stunt.ID) == null) && stunt.IsLearnable(person) &&
                                    GameObject.Chance(100 - Session.Parameters.AutoLearnStuntSuccessRate * Session.Parameters.LearnStuntDays))
                                {
                                    person.GoForStudyStunt(stunt);
                                    break;
                                }
                            }
                            //Label_0220:;
                        }
                    }
                    AICityOperationalState cityState = this.GetAICityOperationalState();
                    int searchQuota = cityState switch
                    {
                        AICityOperationalState.Balanced => Math.Max(1, this.PersonCount / 8),
                        AICityOperationalState.MilitaryBuildUp => Math.Max(1, this.PersonCount / 10),
                        _ => 0
                    };
                    int searchingCount = 0;
                    foreach (Person person in this.PersonsExcludeNvGuan.GetList())
                    {
                        if (searchingCount >= searchQuota)
                        {
                            break;
                        }
                        if (person.ReturnedDaySince >= 3)
                        {
                            if (person.WaitForFeiZi == null &&
                                person.WorkKind == ArchitectureWorkKind.无 &&
                                !person.HasFollowingArmy &&
                                !person.HasEffectiveLeadingArmy &&
                                (this.Fund < Session.Parameters.InternalFundCost ||
                                 !this.withoutTruceFrontline ||
                                 GameObject.Random(person.FightingNumber) < 100))
                            {
                                if (person.Tiredness <= 0)
                                {
                                    person.GoForSearch();
                                    searchingCount++;
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool InstigateAvail()
        {
            return ((this.MovablePersons.Count > 0 && (this.Fund >= this.InstigateArchitectureFund)) && this.HasAnyInstigateTarget());
        }

        private bool HasAnyInstigateTarget()
        {
            foreach (Architecture neighbor in this.AILandLinks)
            {
                if (IsInstigateTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsInstigateTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsInstigateTarget(next)) return true;
            }
            foreach (Architecture neighbor in this.AIWaterLinks)
            {
                if (IsInstigateTarget(neighbor)) return true;
                foreach (Architecture next in neighbor.AILandLinks) if (IsInstigateTarget(next)) return true;
                foreach (Architecture next in neighbor.AIWaterLinks) if (IsInstigateTarget(next)) return true;
            }
            return false;
        }

        private bool IsInstigateTarget(Architecture arch)
        {
            if (arch != null && arch.BelongedFaction != null && !this.IsFriendly(arch.BelongedFaction))
            {
                if (this.BelongedFaction.IsArchitectureKnown(arch))
                {
                    if (arch.Population > 0 || arch.Domination > 0) return true;
                }
            }
            return false;
        }

        public bool IsCaptiveInArchitecture(Captive captive)
        {
            return this.Captives.HasGameObject(captive);
        }

        public bool IsFriendly(Faction faction)
        {
            return ((this.BelongedFaction == faction) || ((this.BelongedFaction != null) && this.BelongedFaction.IsFriendly(faction)));
        }

        public bool IsFriendlyWithoutTruce(Faction faction)
        {
            return ((this.BelongedFaction == faction) || ((this.BelongedFaction != null) && this.BelongedFaction.IsFriendlyWithoutTruce(faction)));
        }

        public bool IsFull()
        {
            return (((((this.Agriculture == this.AgricultureCeiling) && (this.Commerce == this.CommerceCeiling)) && ((this.Technology == this.TechnologyCeiling) && (this.Domination == this.DominationCeiling))) && (this.Morale == this.MoraleCeiling)) && (this.Endurance == this.EnduranceCeiling));
        }

        public bool IsGood()
        {
            return (((((this.Agriculture >= (this.AgricultureCeiling * 0.5)) && (this.Commerce >= (this.CommerceCeiling * 0.5))) && ((this.Technology >= (this.TechnologyCeiling * 0.5)) && (this.Domination >= (this.DominationCeiling * 0.7)))) && (this.Morale >= (this.MoraleCeiling * 0.5))) && (this.Endurance >= (this.EnduranceCeiling * 0.5)));
        }

        public bool IsVeryGood()
        {
            return (((((this.Agriculture >= (this.AgricultureCeiling * 0.8)) && (this.Commerce >= (this.CommerceCeiling * 0.8))) && ((this.Technology >= (this.TechnologyCeiling * 0.8)) && (this.Domination >= (this.DominationCeiling * 0.8)))) && (this.Morale >= (this.MoraleCeiling * 0.8))) && (this.Endurance >= (this.EnduranceCeiling * 0.8)));
        }

        public bool IsHostile(Faction faction)
        {
            return ((this.BelongedFaction != null) && this.BelongedFaction.IsHostile(faction));
        }

        public bool IsLandLink(Architecture a)
        {
            return (this.AILandLinks.GetGameObject(a.ID) != null);
        }

        public bool IsMilitaryUnavailable(Military military)
        {
            return (military.BelongedArchitecture == null);
        }

        public bool IsNodeFoodEnough(LinkNode node, Routeway routeway)
        {
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    return (((node.A.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (node.A.FoodCostPerDayOfLandMilitaries * ((routeway.Length + 6) - (node.A.LandArmyScale / 8))));

                case LinkKind.Water:
                    return (((node.A.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (node.A.FoodCostPerDayOfWaterMilitaries * ((routeway.Length + 6) - (node.A.WaterArmyScale / 8))));

                case LinkKind.Both:
                    return (((node.A.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (node.A.FoodCostPerDayOfAllMilitaries * ((routeway.Length + 6) - (node.A.ArmyScale / 8))));
            }
            return false;
        }

        public bool IsNodeHelpArmyEnough(LinkNode node)
        {
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    return ((!node.A.IsImportant && (node.A.LandArmyScale >= node.A.FewArmyScale)) || (node.A.LandArmyScale >= node.A.NormalArmyScale));

                case LinkKind.Water:
                    return ((!node.A.IsImportant && (node.A.WaterArmyScale >= node.A.FewArmyScale)) || (node.A.WaterArmyScale >= node.A.NormalArmyScale));

                case LinkKind.Both:
                    return ((!node.A.IsImportant && (node.A.ArmyScale >= node.A.FewArmyScale)) || (node.A.ArmyScale >= node.A.NormalArmyScale));
            }
            return false;
        }

        public bool IsOffensiveMilitary(Military m)
        {
            // [统一逻辑] 高价值精锐（造价>=800或特殊兵种）即使兵力比例不高，也视为具有进攻能力
            bool isHighValue = (m.Kind.CreateCost >= 800) || (m.Kind.RecruitLimit > 0);
            return ((((m.Scales >= 3) && (m.Morale >= 80)) && (m.Combativity >= 80)) && (m.InjuryQuantity <= m.Kind.MinScale) && ((m.Scales >= m.RetreatScale * 1.5) || isHighValue));
        }

        public int OffensiveMilitaryCount()
        {
            int r = 0;
            foreach (Military m in this.Militaries)
            {
                if (this.IsOffensiveMilitary(m))
                {
                    r++;
                }
            }
            return r;
        }

        public bool IsOK()
        {
            return (((((this.Agriculture >= (this.AgricultureCeiling * 0.45)) && (this.Commerce >= (this.CommerceCeiling * 0.45))) && ((this.Technology >= (this.TechnologyCeiling * 0.45)) && (this.Domination >= (this.DominationCeiling * 0.7)))) && (this.Morale >= (this.MoraleCeiling * 0.45))) && (this.Endurance >= (this.EnduranceCeiling * 0.4)));
        }

        public bool IsRoutewayPossible(Point p)
        {
            if (Session.Current.Scenario.GetArchitectureByPosition(p) != null)
            {
                return false;
            }
            TerrainDetail terrainDetailByPosition = Session.Current.Scenario.GetTerrainDetailByPosition(p);
            return ((terrainDetailByPosition != null) && ((this.BelongedFaction == null) || ((this.BelongedFaction.RoutewayWorkForce >= terrainDetailByPosition.RoutewayBuildWorkCost) && (terrainDetailByPosition.RoutewayConsumptionRate < 1f))));
        }

        public bool IsSelfFoodEnough(LinkNode node, Routeway routeway)
        {
            //if (routeway.LastPoint != null)   //临时加上，避免跳出
            {
                switch (node.Kind)
                {
                    case LinkKind.None:
                        return false;

                    case LinkKind.Land:
                        return (((this.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (this.FoodCostPerDayOfLandMilitaries * ((routeway.Length + 6) - (this.LandArmyScale / 8))));

                    case LinkKind.Water:
                        return (((this.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (this.FoodCostPerDayOfWaterMilitaries * ((routeway.Length + 6) - (this.WaterArmyScale / 8))));

                    case LinkKind.Both:
                        return (((this.Food * (1f - routeway.LastPoint.ConsumptionRate)) * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length))) >= (this.FoodCostPerDayOfAllMilitaries * ((routeway.Length + 6) - (this.ArmyScale / 8))));
                }
            }
            return false;
        }

        public bool IsSelfHelpArmyEnough(LinkNode node)
        {
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    return ((!this.IsImportant && (this.LandArmyScale >= this.FewArmyScale)) || (this.LandArmyScale >= this.NormalArmyScale));

                case LinkKind.Water:
                    return ((!this.IsImportant && (this.WaterArmyScale >= this.FewArmyScale)) || (this.WaterArmyScale >= this.NormalArmyScale));

                case LinkKind.Both:
                    return ((!this.IsImportant && (this.ArmyScale >= this.FewArmyScale)) || (this.ArmyScale >= this.NormalArmyScale));
            }
            return false;
        }

        public bool IsSelfMoveArmyEnough(LinkNode node)
        {
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    return (((((this.IsImportant && (this.HostileLine || this.withoutTruceFrontline)) && (this.LandArmyScale > this.LargeArmyScale)) || (((this.IsImportant && !this.withoutTruceFrontline) && (this.LandArmyScale > this.NormalArmyScale)) || ((!this.IsImportant && this.HostileLine) && (this.LandArmyScale > this.LargeArmyScale)))) || ((!this.IsImportant && this.withoutTruceFrontline) && (this.LandArmyScale > this.NormalArmyScale))) || ((!this.IsImportant && !this.withoutTruceFrontline) && (this.LandArmyScale > this.FewArmyScale)));

                case LinkKind.Water:
                    return (((((this.IsImportant && (this.HostileLine || this.withoutTruceFrontline)) && (this.WaterArmyScale > this.LargeArmyScale)) || (((this.IsImportant && !this.withoutTruceFrontline) && (this.WaterArmyScale > this.NormalArmyScale)) || ((!this.IsImportant && this.HostileLine) && (this.WaterArmyScale > this.LargeArmyScale)))) || ((!this.IsImportant && this.withoutTruceFrontline) && (this.WaterArmyScale > this.NormalArmyScale))) || ((!this.IsImportant && !this.withoutTruceFrontline) && (this.WaterArmyScale > this.FewArmyScale)));

                case LinkKind.Both:
                    return (((((this.IsImportant && (this.HostileLine || this.withoutTruceFrontline)) && (this.ArmyScale > this.LargeArmyScale)) || (((this.IsImportant && !this.withoutTruceFrontline) && (this.ArmyScale > this.NormalArmyScale)) || ((!this.IsImportant && this.HostileLine) && (this.ArmyScale > this.LargeArmyScale)))) || ((!this.IsImportant && this.withoutTruceFrontline) && (this.ArmyScale > this.NormalArmyScale))) || ((!this.IsImportant && !this.withoutTruceFrontline) && (this.ArmyScale > this.FewArmyScale)));
            }
            return false;
        }

        public bool IsSelfOffensiveArmyEnough(LinkNode node)
        {
            switch (node.Kind)
            {
                case LinkKind.None:
                    return false;

                case LinkKind.Land:
                    return (((!Session.Current.Scenario.IsPlayer(node.A.BelongedFaction) && this.LandArmyScale > this.LargeArmyScale) || (node.A.IsImportant && (this.LandArmyScale > node.A.ArmyScale))) || (!node.A.IsImportant && ((this.LandArmyScale * 2) > (node.A.ArmyScale * 3))));

                case LinkKind.Water:
                    return (((!Session.Current.Scenario.IsPlayer(node.A.BelongedFaction) && this.WaterArmyScale > this.LargeArmyScale) || (node.A.IsImportant && (this.WaterArmyScale > node.A.ArmyScale))) || (!node.A.IsImportant && ((this.WaterArmyScale * 2) > (node.A.ArmyScale * 3))));

                case LinkKind.Both:
                    return (((!Session.Current.Scenario.IsPlayer(node.A.BelongedFaction) && this.ArmyScale > this.LargeArmyScale) || (node.A.IsImportant && (this.ArmyScale > node.A.ArmyScale))) || ((!node.A.IsImportant && (this.ArmyScale > this.NormalArmyScale)) && ((this.ArmyScale * 2) > (node.A.ArmyScale * 3))));
            }
            return false;
        }

        public bool IsViewing(Point position)
        {
            return this.LongViewArea.HasPoint(position);
        }

        public bool IsWaterLink(Architecture a)
        {
            return (this.AIWaterLinks.GetGameObject(a.ID) != null);
        }

        public bool LevelUpAvail()
        {
            return GetLevelUpMilitaryList().Count > 0;
        }

        public void LevelUpMilitary(Military m, MilitaryKind militaryKind)
        {
            if ((militaryKind != null) && (m.Kind.LevelUpAvail(this)))
            {
                //this.BelongedFaction.MorphMilitary(m.Kind, militaryKind);
                int num = (m.Quantity * militaryKind.MinScale) / m.Kind.MinScale;
                int num2 = ((m.Experience - m.Kind.LevelUpExperience) * militaryKind.MinScale) / m.Kind.MinScale;
                this.IncreasePopulation(m.Quantity - num);
                this.IncreaseMilitaryPopulation(m.Quantity - num);
                m.Kind = militaryKind;
                m.Quantity = num;
                m.Experience = num2;
                m.Name = m.Kind.Name + "队";
                // 🔥 AOT 重构：使用强类型事件替代反射调用
                WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseLevelUpMilitary(Session.Current.Scenario, this, m);
            }
        }

        private int RoutewayPathBuilder_OnGetCost(Point position, out float consumptionRate)
        {
            GameArea singleton = new GameArea();
            singleton.AddPoint(position);
            singleton.Centre = position;

            consumptionRate = 0f;
            if (!Session.Current.Scenario.PositionOutOfRange(position))
            {
                int dist = (int)Math.Ceiling(Math.Min(Math.Min(Session.Current.Scenario.GetDistance(singleton, this.pathFinder.startingArchitecture.ArchitectureArea),
                    Session.Current.Scenario.GetDistance(singleton, this.pathFinder.targetArchitecture.ArchitectureArea)), 20));
                if (dist > 4)
                {
                    for (int i = -dist; i <= dist; ++i)
                    {
                        for (int j = Math.Abs(i) - dist; j <= dist - Math.Abs(i); ++j)
                        {
                            Point loc = new Point(position.X + i, position.Y + j);
                            Architecture landedArch = Session.Current.Scenario.GetArchitectureByPosition(loc);

                            if (landedArch != null && landedArch != this.pathFinder.startingArchitecture && landedArch != this.pathFinder.targetArchitecture)
                            {
                                return 1000;
                            }
                        }
                    }
                }

                TerrainDetail terrainDetailByPositionNoCheck = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(position);
                if (terrainDetailByPositionNoCheck != null)
                {
                    Architecture landedArch = Session.Current.Scenario.GetArchitectureByPosition(position);
                    if (landedArch != null && landedArch != this.pathFinder.startingArchitecture && landedArch != this.pathFinder.targetArchitecture)
                    {
                        return 1000;
                    }
                    else if (landedArch == null)
                    {
                        if (this.pathFinder.MultipleWaterCost && !Session.Current.Scenario.IsWaterPositionRoutewayable(position))
                        {
                            return 1000;
                        }
                        if (this.pathFinder.MustUseWater && (terrainDetailByPositionNoCheck.ID != 6))
                        {
                            return 1000;
                        }
                        else if (!terrainDetailByPositionNoCheck.TroopPassable)
                        {
                            return 1000;
                        }
                    }

                    return 1;
                }
            }
            return 1000;
        }

        private int RoutewayPathBuilder_OnGetPenalizedCost(Point position)
        {
            return 0;
        }

        public void FindLinks(ArchitectureList allArch)
        {
            pathFinder.OnGetCost += new RoutewayPathFinder.GetCost(RoutewayPathBuilder_OnGetCost);
            pathFinder.OnGetPenalizedCost += new RoutewayPathFinder.GetPenalizedCost(RoutewayPathBuilder_OnGetPenalizedCost);
            FindLandLinks(allArch, 50);
            FindWaterLinks(allArch, 50);
        }

        private GameArea GetLandTroopMovableArea()
        {
            GameArea a = new GameArea();
            foreach (Point i in this.ArchitectureArea.Area)
            {
                if (Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(i).ID != 6)
                {
                    a.AddPoint(i);
                }
                else
                {
                    TerrainKind t1 = Session.Current.Scenario.GetTerrainKindByPosition(new Point(i.X - 1, i.Y));
                    TerrainKind t2 = Session.Current.Scenario.GetTerrainKindByPosition(new Point(i.X + 1, i.Y));
                    TerrainKind t3 = Session.Current.Scenario.GetTerrainKindByPosition(new Point(i.X, i.Y - 1));
                    TerrainKind t4 = Session.Current.Scenario.GetTerrainKindByPosition(new Point(i.X, i.Y + 1));
                    if (t1 != TerrainKind.水域 && t1 != TerrainKind.无)
                    {
                        a.AddPoint(i);
                    }
                    else if (t2 != TerrainKind.水域 && t2 != TerrainKind.无)
                    {
                        a.AddPoint(i);
                    }
                    else if (t3 != TerrainKind.水域 && t3 != TerrainKind.无)
                    {
                        a.AddPoint(i);
                    }
                    else if (t4 != TerrainKind.水域 && t4 != TerrainKind.无)
                    {
                        a.AddPoint(i);
                    }
                }
            }
            return a;
        }

        private GameArea GetWaterTroopMovableArea()
        {
            return this.ArchitectureArea;
        }

        private RoutewayPathFinder pathFinder = new RoutewayPathFinder();
        private void FindLandLinks(ArchitectureList allArch, int maxDistance)
        {
            foreach (Architecture i in allArch)
            {
                if (i == this) continue;
                if (i.AILandLinks.HasGameObject(this)) continue;
                if (Session.Current.Scenario.GetSimpleDistance(i.Position, this.Position) < maxDistance)
                {
                    pathFinder.ConsumptionMax = 0.7f;
                    pathFinder.startingArchitecture = this;
                    pathFinder.targetArchitecture = i;
                    pathFinder.MultipleWaterCost = !Session.GlobalVariables.LandArmyCanGoDownWater;
                    pathFinder.MustUseWater = false;
                    Point? p1;
                    Point? p2;
                    Session.Current.Scenario.GetClosestPointsBetweenTwoAreas(this.GetLandTroopMovableArea(), i.GetLandTroopMovableArea(), out p1, out p2);
                    if (p1.HasValue && p2.HasValue)
                    {
                        if (pathFinder.GetPath(p1.Value, p2.Value, true))
                        {
                            this.AILandLinks.Add(i);
                            i.AILandLinks.Add(this);
                        }
                    }
                }
            }
        }

        private void FindWaterLinks(ArchitectureList allArch, int maxDistance)
        {
            if (!this.IsBesideWater) return;
            foreach (Architecture i in allArch)
            {
                if (i == this) continue;
                if (!i.IsBesideWater) continue;
                if (i.AIWaterLinks.HasGameObject(this)) continue;
                pathFinder.startingArchitecture = this;
                pathFinder.targetArchitecture = i;
                pathFinder.MultipleWaterCost = false;
                pathFinder.MustUseWater = true;
                if (Session.Current.Scenario.GetSimpleDistance(i.Position, this.Position) < maxDistance)
                {
                    pathFinder.ConsumptionMax = 0.7f;
                    Point? p1;
                    Point? p2;
                    Session.Current.Scenario.GetClosestPointsBetweenTwoAreas(this.GetWaterTroopMovableArea(), i.GetWaterTroopMovableArea(), out p1, out p2);
                    if (p1.HasValue && p2.HasValue)
                    {
                        if (pathFinder.GetPath(p1.Value, p2.Value, true))
                        {
                            this.AIWaterLinks.Add(i);
                            i.AIWaterLinks.Add(this);
                        }
                    }
                }
            }
        }

        public void LoadFromString(GameArea gameArea, string dataString)
        {
            // #if DEBUG block removed

            
            // 🔥 默认不输出正常日志，只在检测到异常时才输出
            #if DEBUG
            bool hasIssue = false;
            if (!string.IsNullOrEmpty(dataString))
            {
                var expectedCount = dataString.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length / 2;
                var currentCount = gameArea?.Area?.Count ?? 0;
                hasIssue = (currentCount > 0 && currentCount != expectedCount);
            }
            
            // 只在启用详细日志或检测到问题时才输出
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableLoadFromStringLog || 
                (hasIssue && WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection))
            {
                System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} GameArea ID:{gameArea?.InstanceId}, 字符串:'{dataString}'");
                System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} 解析前Area.Count:{gameArea?.Area?.Count ?? 0}");
            }
            #endif
            
            // 🔥 防护：确保gameArea不为null
            if (gameArea == null)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} 错误：gameArea为null");
                return;
            }
            
            // 🔥 防护：确保从干净状态开始，避免累积错误
            gameArea.Area.Clear();
            gameArea.topleft = null;
            gameArea.topright = null;
            gameArea.bottomleft = null;
            gameArea.bottomright = null;
            
            if (string.IsNullOrEmpty(dataString))
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} 数据字符串为空");
                #endif
                return;
            }
            
            // 🔥 使用局部变量避免全局状态污染
            char[] localSeparator = new char[] { ' ', '\n', '\r', '\t' };
            string[] localStrArray = dataString.Split(localSeparator, StringSplitOptions.RemoveEmptyEntries);
            

            
            int minX = 10000, minY = 10000, maxX = -10000, maxY = -10000, x, y;
            
            for (int i = 0; i < localStrArray.Length; i += 2)
            {
                try
                {
                    if (i + 1 >= localStrArray.Length)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} 警告：坐标数据不完整，跳过最后一个值");
                        break;
                    }
                    
                    x = int.Parse(localStrArray[i]);
                    y = int.Parse(localStrArray[i + 1]);
                    
                    // 🔥 直接添加到Area，避免AddPoint的副作用
                    gameArea.Area.Add(new Microsoft.Xna.Framework.Point(x, y));
                    
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadFromString] {Name} 解析坐标失败 i={i}: {ex.Message}");
                }
            }
            
            // 🔥 防护：只有在有有效坐标时才设置Centre
            if (gameArea.Area.Count > 0)
            {
                gameArea.Centre = new Point((minX + maxX) / 2, (minY + maxY) / 2);
            }
            else
            {
                gameArea.Centre = new Point(0, 0);
            }
            
            #if DEBUG
            // 🔥 只在启用详细日志或检测到问题时才输出
            var finalExpectedCount = localStrArray.Length / 2;
            bool hasResultIssue = (gameArea.Area.Count != finalExpectedCount);
            
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableLoadFromStringLog || 
                (hasResultIssue && WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection))
            {
                System.Diagnostics.Debug.WriteLine($"[LoadFromString完成] {Name} Area.Count:{gameArea.Area.Count}, Centre:({gameArea.Centre.X},{gameArea.Centre.Y})");
                
                if (hasResultIssue)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadFromString警告] {Name} 期望{finalExpectedCount}个坐标，实际解析{gameArea.Area.Count}个");
                }
            }
            #endif
        }

        // 🔥 新增：确保ArchitectureArea独立性的方法
        public void EnsureUniqueAreaInstance()
        {
            if (ArchitectureArea != null && !string.IsNullOrEmpty(ArchitectureAreaString))
            {
                // 检查是否需要创建新实例
                var expectedCount = ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                var actualCount = ArchitectureArea.Area.Count;
                
                if (actualCount != expectedCount)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[独立性修复] {Name} 创建新的ArchitectureArea实例 (期望{expectedCount}，实际{actualCount})");
                    #endif
                    
                    // 强制创建新实例，避免共享引用
                    var newArea = new GameArea();
                    LoadFromString(newArea, ArchitectureAreaString);
                    ArchitectureArea = newArea;
                }
            }
        }

        public void LoadAILandLinksFromString(ArchitectureList architectures, string dataString)
        {
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.AILandLinks.Clear();
            foreach (string str in strArray)
            {
                Architecture gameObject = architectures.GetGameObject(int.Parse(str)) as Architecture;
                if (gameObject != null)
                {
                    this.AILandLinks.Add(gameObject);
                }
            }
        }

        public void LoadAIWaterLinksFromString(ArchitectureList architectures, string dataString)
        {
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.AIWaterLinks.Clear();
            foreach (string str in strArray)
            {
                Architecture gameObject = architectures.GetGameObject(int.Parse(str)) as Architecture;
                if (gameObject != null)
                {
                    this.AIWaterLinks.Add(gameObject);
                }
            }
        }

        [DataMember]
        public string CaptivesString { get; set; }

        public List<string> LoadCaptivesFromString(CaptiveList captives, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (string str in strArray)
                {
                    Captive gameObject = captives.GetGameObject(int.Parse(str)) as Captive;
                    if (gameObject != null)
                    {
                        gameObject.CaptivePerson.LocationArchitecture = this;
                        gameObject.CaptivePerson.LocationTroop = null;
                        gameObject.CaptivePerson.Status = PersonStatus.Captive;
                    }
                    else
                    {
                        errorMsg.Add("俘虜ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("俘虜列表一栏应为半型空格分隔的俘虜ID");
            }
            return errorMsg;
        }

        public List<string> LoadFacilitiesFromString(FacilityList facilities, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            // 原因：二进制存档不使用这些字符串，设施关系已在 LinkScenarioReferences 中建立
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.Facilities.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Facility gameObject = facilities.GetGameObject(int.Parse(str)) as Facility;
                    if (gameObject != null)
                    {
                        this.Facilities.AddFacility(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("設施ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("設施列表一栏应为半型空格分隔的設施ID");
            }
            return errorMsg;
        }

        public List<string> LoadFundPacksFromString(string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.FundPacks.Clear();
            try
            {
                for (int i = 0; i < strArray.Length; i += 2)
                {
                    this.FundPacks.Add(new FundPack(int.Parse(strArray[i]), int.Parse(strArray[i + 1])));
                }
            }
            catch
            {
                errorMsg.Add("資金包應為半型空格分隔的數字，資金、日數相間");
            }
            return errorMsg;
        }

        public List<string> LoadFoodPacksFromString(string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
                        char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.FoodPacks.Clear();
            try
            {
                for (int i = 0; i < strArray.Length; i += 2)
                {
                    this.FoodPacks.Add(new FoodPack(int.Parse(strArray[i]), int.Parse(strArray[i + 1])));
                }
            }
            catch
            {
                errorMsg.Add("糧草包應為半型空格分隔的數字，糧草、日數相間");
            }
            return errorMsg;
        }

        public List<string> LoadMilitariesFromString(MilitaryList militaries, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 关键修复：如果 dataString 为空，直接返回，不要清空 Militaries 列表
            // 原因：新版本存档使用 MilitaryIDs 而不是 MilitariesString
            // 编队关系已在 LinkMilitaries 中通过 MilitaryIDs 建立
            // 如果这里清空了，编队就会消失！
            if (string.IsNullOrEmpty(dataString))
            {
                #if DEBUG
                // System.Diagnostics.Debug.WriteLine($"[LoadMilitariesFromString] {this.Name} 的 MilitariesString 为空，跳过（编队已通过 MilitaryIDs 加载）");
                #endif
                return errorMsg;
            }
            
            // 🔥 只有当 dataString 不为空时，才清空并重新加载
            // 这是为了兼容旧版本存档（使用 MilitariesString）
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            #if DEBUG
            // System.Diagnostics.Debug.WriteLine($"[LoadMilitariesFromString] {this.Name} 从 MilitariesString 加载 {strArray.Length} 个编队");
            #endif
            
            this.Militaries.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Military gameObject = militaries.GetGameObject(int.Parse(str)) as Military;
                    if (gameObject != null)
                    {
                        this.AddMilitary(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("編隊ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("編隊列表一栏应为半型空格分隔的編隊ID");
            }
            return errorMsg;
        }

        [DataMember]
        [JsonIgnore]  // 🔥 使用 PersonsString_Legacy 代替
        public string PersonsString { get; set; }

        [DataMember]
        public string MovingPersonsString { get; set; }

        [DataMember]
        public string NoFactionPersonsString { get; set; }

        [DataMember]
        public string NoFactionMovingPersonsString { get; set; }

        [DataMember]
        public string feiziliebiaoString { get; set; }

        public List<string> LoadPersonsFromString(Dictionary<int, Person> persons, string dataString, PersonStatus status)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (string str in strArray)
                {
                    Person t = persons[int.Parse(str)];
                    // 🔥 修复：移除对 Session.Current 的依赖
                    // 日期：2026-03-16
                    // 原因：LoadPersonsFromString 在 ProcessScenarioData 中被调用，此时 Session.Current.Scenario 为 null
                    // 解决：俘虏检查延迟到数据加载完成后，此处只检查 Person 是否存在
                    if (t != null)
                    {

                        
                        t.LocationArchitecture = this;
                        // 🔥 关键修复：同步更新 LocationArchitectureID
                        // 日期：2026-03-16
                        // 原因：剧本 JSON 中 Person 没有 LocationArchitectureID 字段，默认 -1
                        //       LinkReferencesPhase 从 LocationArchitectureID 重建引用，会把 LocationArchitecture 重置为 null
                        //       导致 CreatePersonStatusCache 缓存为空，Architecture.Persons.Count = 0
                        t.LocationArchitectureID = this.ID;
                        
                        #if DEBUG
                        if (this.ID == 0)  // 洛阳
                        {

                        }
                        #endif
                        
                        t.LocationTroop = null;
                        t.Status = status;
                        if (status == PersonStatus.Moving || status == PersonStatus.NoFactionMoving)
                        {
                            t.TargetArchitecture = this;
                        }
                    }
                    else
                    {
                        errorMsg.Add("人物ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("人物列表一栏应为半型空格分隔的人物ID");
            }
            return errorMsg;
        }

        public List<string> LoadPopulationPacksFromString(string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.PopulationPacks.Clear();
            try
            {
                for (int i = 0; i < strArray.Length; i += 2)
                {
                    this.PopulationPacks.Add(new PopulationPack(int.Parse(strArray[i]), int.Parse(strArray[i + 1])));
                }
            }
            catch
            {
                errorMsg.Add("人口包應為半型空格分隔的數字，糧草、日數相間");
            }
            return errorMsg;
        }

        public List<string> LoadMilitaryPopulationPacksFromString(string dataString)
        {
            if (dataString == null) return new List<string>();

            List<string> errorMsg = new List<string>();
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.MilitaryPopulationPacks.Clear();
            try
            {
                for (int i = 0; i < strArray.Length; i += 2)
                {
                    this.MilitaryPopulationPacks.Add(new PopulationPack(int.Parse(strArray[i]), int.Parse(strArray[i + 1])));
                }
            }
            catch
            {
                errorMsg.Add("人口包應為半型空格分隔的數字，糧草、日數相間");
            }
            return errorMsg;
        }

        /*
        public void LoadSpyPacksFromString(string dataString)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.SpyPacks.Clear();
            for (int i = 0; i < strArray.Length; i += 2)
            {
                Person gameObject = Session.Current.Scenario.Persons.GetGameObject(int.Parse(strArray[i])) as Person;
                if (gameObject != null)
                {
                    this.SpyPacks.Add(new SpyPack(gameObject, int.Parse(strArray[i + 1])));
                }
            }
        }
        */

        public List<string> LoadInformationsFromString(InformationList informations, string dataString)
        {
            List<string> errorMsg = new List<string>();
            
            // 🔥 修复：如果 dataString 为空，直接返回
            if (string.IsNullOrEmpty(dataString))
            {
                return errorMsg;
            }
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
                        errorMsg.Add("情報" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("情報一欄應為半型空格分隔的情報ID");
            }
            return errorMsg;
        }

        public bool MergeAvail()
        {
            for (int i = 0; i < this.Militaries.Count; i++)
            {
                Military military = this.Militaries[i] as Military;
                if ((military.Quantity != military.Kind.MaxScale) && (military.InjuryQuantity <= 0))
                {
                    foreach (Military military2 in this.Militaries)
                    {
                        if (((military != military2) && (military.Kind.Equals(military2.Kind))) && ((military2.Quantity < military2.Kind.MaxScale) && (military2.InjuryQuantity == 0)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void MonthEvent()
        {
            this.DevelopMonth();
            this.CheckIsFrontLine();
        }

        public bool MoraleAvail()
        {
            return (_architectureKind.HasMorale && this.HasPerson());
        }

        public bool NewMilitaryAvail()
        {
            if (this.BelongedFaction != null)
            {
                if (!_architectureKind.HasPopulation)
                {
                    return false;
                }
                
                // 🔥 修复：新编按钮的显示逻辑
                // 日期：2026-03-18
                // 原因：新编是创建新编队，应该在以下情况显示：
                // 1. 城内没有编队（可以创建第一个编队）
                // 2. 城内有编队但还有空闲武将（可以创建更多编队）
                bool hasAvailableKind = false;
                foreach (MilitaryKind kind in this.BelongedFaction.AvailableMilitaryKinds.MilitaryKinds.Values)
                {
                    if (kind.CreateAvail(this))
                    {
                        hasAvailableKind = true;
                        break;
                    }
                }
                if (!hasAvailableKind)
                {
                    foreach (MilitaryKind kind in this.PrivateMilitaryKinds.MilitaryKinds.Values)
                    {
                        if (kind.CreateAvail(this))
                        {
                            hasAvailableKind = true;
                            break;
                        }
                    }
                }
                
                if (!hasAvailableKind)
                {
                    return false;
                }
                
                if (this.BelongedFaction.MilitaryCount > this.BelongedFaction.CityTotalSize * Session.GlobalVariables.FactionMilitaryLimt + 1)
                {
                    return false;
                }
                
                // 🔥 关键修复：只有在城内没有编队，或者有空闲武将时才显示新编按钮
                // 城内没有编队：可以创建第一个编队
                // 有空闲武将：PersonsExcludeNvGuan数量 > Militaries数量
                if (!this.HasMilitary())
                {
                    // 没有编队，可以新编
                    return true;
                }
                else
                {
                    // 有编队，检查是否有空闲武将
                    // 如果武将数量 > 编队数量，说明有空闲武将可以创建新编队
                    return this.PersonsExcludeNvGuan.Count > this.Militaries.Count;
                }
            }
            return false;
        }

        public bool NewSectionAvail()
        {
            return (this.BelongedFaction.ArchitectureCount > 1);
        }

        public int connectedToFactionArchitectureCount(Faction f)
        {
            int result = 0;
            foreach (Architecture a in this.GetAILinks())
            {
                if (a.BelongedFaction == f)
                {
                    result++;
                }
            }
            return result;
        }

        public int connectedNotToFactionArchitectureCount(Faction f)
        {
            int result = 0;
            foreach (Architecture a in this.GetAILinks())
            {
                if (a.BelongedFaction != f)
                {
                    result++;
                }
            }
            return result;
        }

        private int AttackableLandArmyScale
        {
            get
            {
                int result = 0;
                int troopCnt = 0;
                foreach (Military i in this.Militaries)
                {
                    if (i.Kind.Type != MilitaryType.水军)
                    {
                        result += i.Scales;
                        troopCnt++;
                        if (troopCnt >= this.PersonsExcludeNvGuan.Count) break;
                    }
                }
                return result;
            }
        }

        private int AttackableWaterArmyScale
        {
            get
            {
                int result = 0;
                int troopCnt = 0;
                foreach (Military i in this.Militaries)
                {
                    result += i.Scales / (i.Kind.Type == MilitaryType.水军 ? 1 : 2);
                    troopCnt++;
                    if (troopCnt >= this.PersonsExcludeNvGuan.Count) break;
                }
                return result;
            }
        }

        private long EstimatedIdleFightingForce()
        {
            GameObjectList targetPersons = this.PersonsExcludeNvGuan.GetList();
            targetPersons.IsNumber = true;
            targetPersons.SmallToBig = false;
            targetPersons.PropertyName = "FightingForce";
            targetPersons.ReSort();

            GameObjectList targetMilitaries = this.Militaries.GetList();
            targetMilitaries.IsNumber = true;
            targetMilitaries.SmallToBig = false;
            targetMilitaries.PropertyName = "KindMerit";
            targetMilitaries.ReSort();

            long result = 0;
            for (int i = 0; i < targetPersons.Count && i < targetMilitaries.Count; ++i)
            {
                result += (targetPersons[i] as Person).FightingForce * (targetMilitaries[i] as Military).KindMerit;
            }
            return result;
        }

        private int getArmyScaleRequiredForAttack(LinkNode wayToTarget)
        {
            // 🔥 修复：添加null检查，防止ArgumentOutOfRangeException
            if (wayToTarget?.A?.BelongedFaction == null)
            {
                // System.Diagnostics.Debug.WriteLine($\"[getArmyScaleRequiredForAttack] wayToTarget或其目标建筑为null，返回默认值\");
                return 1000; // 返回一个较高的默认值，避免AI轻易出兵
            }
            
            Person leader = this.BelongedFaction.Leader;

            float totalScale = 0;
            
            if (wayToTarget.A.BelongedFaction != null)
            {
                int targetPersonCount = wayToTarget.A.PersonCount;
                targetPersonCount = Math.Min(wayToTarget.A.BelongedFaction.PersonCount, (int) (targetPersonCount + wayToTarget.A.Endurance / Session.Current.Scenario.Parameters.ArchitectureDamageRate * 25));
                int count = 0;
                foreach (Military m in wayToTarget.A.Militaries)
                {
                    if (count > targetPersonCount) break;
                    totalScale += m.Scales;
                    count++;
                }

                totalScale = (int) (totalScale * ((double)wayToTarget.A.EstimatedIdleFightingForce() / (this.EstimatedIdleFightingForce() + 1)));
            }
            else
            {
                totalScale = wayToTarget.A.ArmyScale;
            }

            if (Session.Current.Scenario.IsPlayer(wayToTarget.A.BelongedFaction))
            {
                totalScale = (int) (totalScale * Session.Parameters.AIOffensiveCampaignRequiredScaleFactor);
            }

            return (int)(totalScale + wayToTarget.A.Endurance / 15 +
                            (Session.Parameters.AIOffendDefendTroopAdd + (leader.Calmness - leader.Braveness + (3 - (int)leader.Ambition) * 2) * Session.Parameters.AIOffendDefendTroopMultiply));
        }

        private int getArmyReserveForOffensive()
        {
            int totalThreat = 0;
            int maxThreat = 0;
            int playerMaxThreat = 0;
            int playerTotalThreat = 0;
            foreach (LinkNode i in this.AIAllLinkNodes.Values)
            {
                if (i.Level > 1) break;
                if (i.A.BelongedFaction != null && !this.IsFriendlyWithoutTruce(i.A.BelongedFaction))
                {
                    int threat = (i.Kind == LinkKind.Land ? i.A.AttackableLandArmyScale : i.A.AttackableWaterArmyScale);
                    if (threat > maxThreat)
                    {
                        maxThreat = threat;
                        if (Session.Current.Scenario.IsPlayer(i.A.BelongedFaction))
                        {
                            playerMaxThreat = threat;
                        }
                    }
                    totalThreat += threat;
                    if (Session.Current.Scenario.IsPlayer(i.A.BelongedFaction))
                    {
                        playerTotalThreat += threat;
                    }
                }
            }
            int reserve;
            if (Session.GlobalVariables.PinPointAtPlayer && this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.势力 &&
                    Session.Current.Scenario.IsPlayer(this.BelongedSection.OrientationFaction))
            {
                maxThreat = playerMaxThreat;
                totalThreat = playerTotalThreat;
            }
            Person leader = this.BelongedFaction.Leader;
            if (leader.Braveness >= 5 || leader.Calmness <= 5)
            {
                reserve = (int)(maxThreat * (0.8 + (leader.Calmness - leader.Braveness) * 0.1));
            }
            else
            {
                reserve = (int)(totalThreat / 2 * (0.8 + (leader.Calmness - leader.Braveness) * 0.1));
            }

            return reserve;
        }

        /// <summary>
        /// 计算防守出兵时的保留兵力（比进攻出兵更宽松）
        /// </summary>
        private int getArmyReserveForDefensive()
        {
            // 获取进攻时的保留兵力作为基准
            int offensiveReserve = this.getArmyReserveForOffensive();
            
            // 防守时只保留60%的进攻保留兵力，更积极出兵
            int defensiveReserve = (int)(offensiveReserve * 0.6f);
            
            // 最低保留一定兵力防止空城（至少保留10%兵力或10人）
            int minReserve = Math.Max(10, this.ArmyScale / 10);
            
            // 返回较大值，确保不会过于激进
            int finalReserve = Math.Max(defensiveReserve, minReserve);
            
// #if DEBUG
//             System.Diagnostics.Debug.WriteLine($"[防守保留兵力] {this.Name}: 进攻保留={offensiveReserve}, 防守保留={finalReserve} (60%系数+最低{minReserve})");
// #endif
            
            return finalReserve;
        }

        public void RemoveRoutewayToArchitecture(Architecture a)
        {
            Routeway toRemove = null;
            foreach (Routeway r in this.Routeways)
            {
                if (r.DestinationArchitecture == a)
                {
                    toRemove = r;
                    break;
                }
            }
            if (toRemove != null)
            {
                toRemove.RemoveAfterClose = true;
                toRemove.Close();
            }
        }

        private bool ignoreReserve = false;
        private Dictionary<LinkNode, Routeway> linkNodeRouteway = new Dictionary<LinkNode, Routeway>();
        private void OffensiveCampaign()
        {
            DateTime beforeStart = DateTime.UtcNow;
            
            // 🔥 修复：强制刷新人员缓存，确保 PersonsExcludeNvGuan 返回正确数据
            Session.Current.Scenario.ClearPersonStatusCache();

            Person leader = this.BelongedFaction.Leader;
            int reserveBase = this.getArmyReserveForOffensive();
            
#if DEBUG
            // 调试日志前缀
            string logPrefix = $"[AI进攻决策] {this.Name}({this.BelongedFaction.Name})";
            
            // 🔥 添加详细调试信息 - 已根据需求关掉
            // System.Diagnostics.Debug.WriteLine($"{logPrefix} 开始评估进攻");
            // System.Diagnostics.Debug.WriteLine($"{logPrefix} AIAllLinkNodes数量: {this.AIAllLinkNodes?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"{logPrefix} 武将数: {this.PersonsExcludeNvGuan?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"{logPrefix} 编队数: {this.Militaries?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"{logPrefix} 预留兵力: {reserveBase}");
#endif

            if (this.actuallyUnreachableArch.Contains(this.PlanArchitecture))
            {
#if DEBUG
//                 System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃计划目标 {this.PlanArchitecture.Name}: 实际上无法到达");
#endif
                this.RemoveRoutewayToArchitecture(this.PlanArchitecture);
                this.PlanArchitecture = null;
            }

            if (this.BelongedSection != null && !this.BelongedSection.AIDetail.AllowOffensiveCampaign)
            {
#if DEBUG
                // System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃进攻: 军区设置不允许进攻");
#endif
                this.RemoveRoutewayToArchitecture(this.PlanArchitecture);
                this.PlanArchitecture = null;
            }
            else if (!this.HasPerson())
            {
#if DEBUG
                // System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃进攻: 无武将");
#endif
                this.RemoveRoutewayToArchitecture(this.PlanArchitecture);
                this.PlanArchitecture = null;
            }
            else if (this.PlanArchitecture != null && this.IsFriendly(this.PlanArchitecture.BelongedFaction))
            {
#if DEBUG
//                 System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃计划目标 {this.PlanArchitecture.Name}: 目标已变成友好势力");
#endif
                this.RemoveRoutewayToArchitecture(this.PlanArchitecture);
                this.PlanArchitecture = null;
            }
            else if ((this.PlanArchitecture != null) || ((this.IsGood() || GameObject.Chance((int)(GameObject.Square((int)leader.Ambition) * Session.Parameters.AIAttackChanceIfUnfull))) &&
              (this.Domination >= this.DominationCeiling * 0.7 || this.Population <= _architectureKind.PopulationBoundary / 2)))
            {
                // ★★★ 早期检查：如果没有可用兵力，直接返回，避免无意义的节点遍历 ★★★
                if (this.LandArmyScale <= 0 && this.WaterArmyScale <= 0)
                {
#if DEBUG
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 无可用兵力: LandArmyScale={this.LandArmyScale}, WaterArmyScale={this.WaterArmyScale}，跳过进攻决策");
#endif
                    this.RemoveRoutewayToArchitecture(this.PlanArchitecture);
                    this.PlanArchitecture = null;
                    return;
                }
                
                Architecture target = this.PlanArchitecture;
                LinkNode wayToTarget = null;
                if (target == null)
                {
                    ignoreReserve = false;
                    //choose target
                    int maxWeight = int.MinValue;
                    LinkNode maxNode = null;
                    int maxLevel = 1;
                    foreach (LinkNode i in this.AIAllLinkNodes.Values)
                    {
                        if (i.Level > maxLevel && maxNode != null)
                        {
                            break;
                        }
                        else if (i.Level > maxLevel)
                        {
                            maxLevel++;
                        }

                        if (this.actuallyUnreachableArch.Contains(i.A))
                        {
                            continue;
                        }
                        if (this.IsFriendly(i.A.BelongedFaction) || i.Kind == LinkKind.None)
                        {
                            continue;
                        }
                        if (GameObject.Chance(Session.Parameters.AIObeyStrategyTendencyChance) && this.BelongedFaction.Leader.StrategyTendency == PersonStrategyTendency.统一地区 && this.LocationState?.LinkedRegion != null && i.A.LocationState?.LinkedRegion != null && i.A.LocationState.LinkedRegion != this.LocationState.LinkedRegion)
                        {
                            continue;
                        }
                        if (GameObject.Chance(Session.Parameters.AIObeyStrategyTendencyChance) && this.BelongedFaction.Leader.StrategyTendency == PersonStrategyTendency.统一州 && this.LocationState != null && i.A.LocationState != null && i.A.LocationState != this.LocationState)
                        {
                            continue;
                        }
                        if (GameObject.Chance(Session.Parameters.AIObeyStrategyTendencyChance) && this.BelongedFaction.Leader.StrategyTendency == PersonStrategyTendency.维持现状)
                        {
                            continue;
                        }
                        if (this.actuallyUnreachableArch.Contains(i.A))
                        {
                            continue;
                        }
                        if (i.A.BelongedFaction != null && i.A.BelongedFaction.ArchitectureCount > 1 && i.A.connectedNotToFactionArchitectureCount(this.BelongedFaction) > 0)
                        {
                            // 添加空值检查防止NullReferenceException
                            if (this.BelongedSection != null && this.BelongedSection.AIDetail != null)
                            {
                                if (this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.军区)
                                {
                                    continue;
                                }
                                if (this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.势力)
                                {
                                    if (this.BelongedSection.OrientationFaction != i.A.BelongedFaction)
                                    {
                                        continue;
                                    }
                                }
                                if (this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.州域)
                                {
                                    if (i.A.LocationState == null || this.BelongedSection.OrientationState != i.A.LocationState)
                                    {
                                        continue;
                                    }
                                }
                                if (this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.建筑)
                                {
                                    if (this.BelongedSection.OrientationArchitecture != i.A)
                                    {
                                        continue;
                                    }
                                }
                            }
                            if (this.BelongedSection != null && this.BelongedSection.AIDetail != null && this.BelongedSection.AIDetail.OrientationKind == SectionOrientationKind.无)
                            {
                                if (i.A.BelongedFaction != null &&
                                    (Session.Current.Scenario.GetDiplomaticRelation(this.BelongedFaction.ID, i.A.BelongedFaction.ID) >= leader.Uncruelty * Session.Parameters.AIOffendMaxDiplomaticRelationMultiply)
                                    && GameObject.Random(leader.Uncruelty * leader.Uncruelty * 10) > 0)
                                {
                                    continue;
                                }
                            }
                        }

                        // 原始计算
                        int rawReserve = Math.Max(0, reserveBase - i.A.ArmyScale);
                        int reserve = rawReserve;

                        // 中原激进补丁：如果连接点多(>2)，且君主有野心，强行降低防守预留，迫使其出兵
                        if (this.AILandLinks.Count > 2 && leader.Ambition >= 3)
                        {
                            // 激进策略：只保留 50% 的计算预留值
                            reserve = (int)(rawReserve * 0.5f);
                        }

                        int armyScaleRequiredForAttack = (int) (this.getArmyScaleRequiredForAttack(i));
                        int armyScaleHere = (i.Kind == LinkKind.Land ? this.LandArmyScale : (this.WaterArmyScale + this.LandArmyScale / 2));
                        
                        // 预备兵力检查逻辑
                        bool ignoreReserveCheck = false;

                        // 条件1: 兵力溢出 (原有)
                        if ((this.ArmyScale > this.MaxSupportableTroopScale) && GameObject.Random(20 * (5 - this.BelongedFaction.Leader.Ambition)) == 0)
                        {
                            ignoreReserveCheck = true;
                            ignoreReserve = true;
                        }
                        // 条件2: [新增] 优势兵力强攻 (即使后方有威胁)
                        // 只有高野心君主，且当前兵力远大于目标(1.2倍)时触发
                        else if (leader.Ambition >= 4 && armyScaleHere > (armyScaleRequiredForAttack * 1.2))
                        {
                            // 提高触发几率，避免死守
                            if (GameObject.Random(100) < 40) // 40% 概率忽略后方
                            {
                                ignoreReserveCheck = true;
                                ignoreReserve = true;
                            }
                        }

                        if ((armyScaleHere < armyScaleRequiredForAttack + reserve) && !ignoreReserveCheck)
                        {
                            if ((GameObject.Random((5 - (int)leader.Ambition) * Session.Parameters.AIOffendIgnoreReserveProbAmbitionMultiply - Session.Parameters.AIOffendIgnoreReserveProbAmbitionAdd) == 0 &&
                                (GameObject.Random((leader.Calmness - leader.Braveness) * Session.Parameters.AIOffendIgnoreReserveProbBCDiffMultiply + Session.Parameters.AIOffendIgnoreReserveProbBCDiffAdd)) == 0) &&
                                (GameObject.Chance((int)(((double)armyScaleHere / i.A.ArmyScale - Session.Parameters.AIOffendIgnoreReserveChanceTroopRatioAdd) * Session.Parameters.AIOffendIgnoreReserveChanceTroopRatioMultiply))))
                            {
                                if (armyScaleHere >= armyScaleRequiredForAttack)
                                {
                                    // ignoreReserve = true; // 原代码这里似乎有问题，只是局部变量？不，这是类成员
                                    ignoreReserve = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if ((Session.GlobalVariables.PopulationRecruitmentLimit && (this.ArmyQuantity > this.Population)) || this.Population <= 0 ||
                                !_architectureKind.HasPopulation || !_architectureKind.HasMorale)
                            {
                                if (armyScaleHere >= armyScaleRequiredForAttack)
                                {
                                    ignoreReserve = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // 兵力不足以进攻
                                continue;
                            }
                        }

                        Routeway rw;
                        if (!linkNodeRouteway.TryGetValue(i, out rw))
                        {
                            rw = this.GetRouteway(i, true);
                            linkNodeRouteway.Add(i, rw);
                        }

                        if (rw == null)
                        {
                            continue;
                        }

                        Architecture bypass = rw.ByPassHostileArchitecture;
                        LinkNode candidate = i;
                        if (bypass != null)
                        {
                            foreach (LinkNode j in this.AIAllLinkNodes.Values)
                            {
                                if (j.Level > maxLevel)
                                {
                                    break;
                                }
                                if (j.A == bypass)
                                {
                                    candidate = j;
                                }
                            }
                        }
                        if (!IsSelfFoodEnoughForOffensive(i, rw) && !this.IsFoodTwiceAbundant)
                        {
#if DEBUG
                            // System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃目标 {i.A.Name}: 粮草不足且非多粮状态");
#endif
                            continue;
                        }

                        if (candidate == null) continue;

                        int weight = 1000 + (candidate.Kind == LinkKind.Land ? this.LandArmyScale : this.WaterArmyScale) - candidate.A.ArmyScale;
                        weight += weight / 10 * (candidate.A.connectedToFactionArchitectureCount(this.BelongedFaction) - candidate.A.connectedNotToFactionArchitectureCount(this.BelongedFaction));
                        if (i.A.IsImportant)
                        {
                            weight = weight * 3 / 2;
                        }
                        if (i.A.PopulationCeiling > 0 && this.PopulationCeiling > 0)
                        {
                            weight = (int)(weight * ((double)(candidate.A.Population - this.Population) / this.PopulationCeiling / 2 + 0.5));
                        }
                        else
                        {
                            if (i.A.PopulationCeiling <= 0)
                            {
                                weight /= 3;
                            }
                        }
                        
                        if (weight > maxWeight)
                        {
                            maxWeight = weight;
                            maxNode = candidate;
                        }
                    }
                    wayToTarget = maxNode;
                    
#if DEBUG
                    if (wayToTarget == null && this.AIAllLinkNodes.Count > 0)
                    {
//                        System.Diagnostics.Debug.WriteLine($"{logPrefix} ⚠️ 扫描了 {this.AIAllLinkNodes.Count} 个节点但未找到合适目标，启动详细诊断...");
//                        System.Diagnostics.Debug.WriteLine($"{logPrefix} 本城兵力状态: LandArmyScale={this.LandArmyScale}, WaterArmyScale={this.WaterArmyScale}, ArmyScale={this.ArmyScale}, ArmyQuantity={this.ArmyQuantity}");
                        
                        // 🔥 详细诊断：重新遍历并输出每个节点被过滤的原因
                        int diagCount = 0;
                        foreach (LinkNode i in this.AIAllLinkNodes.Values)
                        {
                            if (diagCount >= 5) break; // 只诊断前5个节点
                            diagCount++;
                            
                            string skipReason = "未知";
                            
                            // 检查1: Level检查
                            if (i.Level > maxLevel)
                            {
                                skipReason = $"Level({i.Level}) > maxLevel({maxLevel})";
                            }
                            // 检查2: 策略倾向（移除随机性，只检查条件）
                            else if (this.BelongedFaction.Leader.StrategyTendency == PersonStrategyTendency.统一州 && this.LocationState != null && i.A.LocationState != null && i.A.LocationState != this.LocationState)
                            {
                                skipReason = $"策略倾向=统一州，目标州={i.A.LocationState?.Name ?? "null"}，本州={this.LocationState?.Name ?? "null"}";
                            }
                            else if (this.BelongedFaction.Leader.StrategyTendency == PersonStrategyTendency.维持现状)
                            {
                                skipReason = "策略倾向=维持现状";
                            }
                            // 检查3: 不可达
                            else if (this.actuallyUnreachableArch.Contains(i.A))
                            {
                                skipReason = "标记为不可达";
                            }
                            // 检查4: 兵力检查
                            else
                            {
                                int rawReserve = Math.Max(0, reserveBase - i.A.ArmyScale);
                                int reserve = (this.AILandLinks.Count > 2 && leader.Ambition >= 3) ? (int)(rawReserve * 0.5f) : rawReserve;
                                int armyScaleRequiredForAttack = (int)(this.getArmyScaleRequiredForAttack(i));
                                int armyScaleHere = (i.Kind == LinkKind.Land ? this.LandArmyScale : (this.WaterArmyScale + this.LandArmyScale / 2));
                                
                                if (armyScaleHere < armyScaleRequiredForAttack + reserve)
                                {
                                    skipReason = $"兵力不足: 当前={armyScaleHere}, 所需={armyScaleRequiredForAttack}, 保留={reserve}";
                                }
                                // 检查5: Routeway
                                else
                                {
                                    Routeway rw;
                                    if (!linkNodeRouteway.TryGetValue(i, out rw))
                                    {
                                        rw = this.GetRouteway(i, true);
                                    }
                                    
                                    if (rw == null)
                                    {
                                        skipReason = "无法生成Routeway";
                                    }
                                    // 检查6: 粮草
                                    else if (!IsSelfFoodEnoughForOffensive(i, rw) && !this.IsFoodTwiceAbundant)
                                    {
                                        skipReason = $"粮草不足: Food={this.Food}, IsFoodTwiceAbundant={this.IsFoodTwiceAbundant}";
                                    }
                                    else
                                    {
                                        skipReason = "通过所有检查但权重不足";
                                    }
                                }
                            }
                            
//                            System.Diagnostics.Debug.WriteLine($"{logPrefix}   节点 {i.A.Name}: {skipReason}");
                        }
                    }
#endif
                }
                else
                {
                    //get way to target
                    this.AIAllLinkNodes.TryGetValue(this.PlanArchitecture.ID, out wayToTarget);
                    
                    // 🔥 修复：PlanArchitecture不在AIAllLinkNodes中时，尝试寻找中间节点
                    if (wayToTarget == null)
                    {
                        // 在所有链接节点中查找路径包含PlanArchitecture的节点
                        foreach (LinkNode node in this.AIAllLinkNodes.Values)
                        {
                            if (node.Path != null && node.Path.Contains(this.PlanArchitecture))
                            {
                                wayToTarget = node;
                                break;
                            }
                        }
                        
                        // 如果仍然找不到，清除PlanArchitecture，让AI下次重新选择目标
                        if (wayToTarget == null)
                        {
// #if DEBUG
//                             System.Diagnostics.Debug.WriteLine($"{logPrefix} PlanArchitecture {this.PlanArchitecture.Name} 不在链接图中，清除计划目标");
// #endif
                            this.PlanArchitecture = null;
                        }
                    }
// #if DEBUG
//                     if (wayToTarget != null)
//                     {
//                         System.Diagnostics.Debug.WriteLine($"{logPrefix} 从PlanArchitecture获取wayToTarget: {wayToTarget.A.Name}");
//                     }
// #endif
                }
                
                if (wayToTarget != null)
                {
// #if DEBUG
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 找到目标: {wayToTarget.A.Name}");
// #endif
//                     
                    int reserve = Math.Max(0, reserveBase - wayToTarget.A.ArmyScale);
                    int armyScaleRequiredForAttack = (int)(this.getArmyScaleRequiredForAttack(wayToTarget));
                    int armyScaleHere = (wayToTarget.Kind == LinkKind.Land ? this.LandArmyScale : (this.WaterArmyScale + this.LandArmyScale / 2));
// 
// #if DEBUG
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 兵力检查: 当前={armyScaleHere}, 所需={armyScaleRequiredForAttack}, 保留={reserve}");
// #endif
                    
                    if (armyScaleHere < armyScaleRequiredForAttack)
                    {
// #if DEBUG
//                         System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃目标 {wayToTarget.A.Name}: 当前兵力({armyScaleHere}) < 所需兵力({armyScaleRequiredForAttack})");
// #endif
                        this.PlanArchitecture = null;
                        return;
                    }

// #if DEBUG
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 检查是否知道目标: {this.BelongedFaction.IsArchitectureKnown(wayToTarget.A)}");
// #endif
                    
                    if (this.BelongedFaction.IsArchitectureKnown(wayToTarget.A))
                    {
                            Routeway routeway = this.GetRouteway(wayToTarget, true);

// #if DEBUG
//                             System.Diagnostics.Debug.WriteLine($"{logPrefix} Routeway: {(routeway != null ? "存在" : "null")}");
// #endif
                            
                            if (routeway == null)
                            {
// #if DEBUG
//                                 System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃目标 {wayToTarget.A.Name}: 无法建立路线 (Routeway is null)");
// #endif
                                this.PlanArchitecture = null;
                            }
                            else
                            {
                                Architecture bypass = routeway.ByPassHostileArchitecture;
                                if (bypass != null)
                                {
// #if DEBUG
//                                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 需要绕道: {bypass.Name}");
// #endif
                                    this.PlanArchitecture = bypass;
                                }
                                else if (Session.GlobalVariables.LiangdaoXitong && (routeway.LastPoint.BuildFundCost * (4 + ((wayToTarget.A.AreaCount >= 4) ? 2 : 0))) > this.Fund)
                                {
                                    // 粮道系统资金检查
// #if DEBUG
//                                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 粮道建设资金不足，转为仅计划进攻 {wayToTarget.A.Name}");
// #endif
                                    routeway.Building = false;
                                    this.PlanArchitecture = wayToTarget.A;
                                }
                                else
                                {
                                    double foodRateBySeason = Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length));
                                    bool foodEnough = ((this.Food * foodRateBySeason) >= (this.FoodCeiling / 3)) || this.IsSelfFoodEnoughForOffensive(wayToTarget, routeway);
                                    
// #if DEBUG
//                                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 粮草检查: Food={this.Food}, FoodCeiling={this.FoodCeiling}, foodEnough={foodEnough}");
// #endif
                                    
                                    if (!foodEnough)
                                    {
// #if DEBUG
//                                         System.Diagnostics.Debug.WriteLine($"{logPrefix} 放弃目标 {wayToTarget.A.Name}: 粮草不足维持远征");
// #endif
                                        routeway.Building = false;
                                        this.PlanArchitecture = wayToTarget.A;
                                    }
                                    else if (Session.GlobalVariables.LiangdaoXitong && (routeway.LastPoint.ConsumptionRate >= 0.1f) && (((int)(routeway.Length * (routeway.LastPoint.ConsumptionRate + 0.2f))) > routeway.LastActivePointIndex))
                                    {
                                        routeway.Building = true;
                                        this.PlanArchitecture = wayToTarget.A;
                                    }
                                    else
                                    {
                                        if (!routeway.IsActive)
                                        {
                                            routeway.Building = true;
                                        }

                                        bool playerKnown = false;
                                        foreach (Faction f in Session.Current.Scenario.PlayerFactions)
                                        {
                                            if (f.IsArchitectureKnown(this) || f.IsArchitectureKnown(wayToTarget.A))
                                            {
                                                playerKnown = true;
                                            }
                                        }

// #if DEBUG
//                                         System.Diagnostics.Debug.WriteLine($"{logPrefix} AIQuickBattle={Session.GlobalVariables.AIQuickBattle}, playerKnown={playerKnown}");
// #endif
                                        
                                        if (Session.GlobalVariables.AIQuickBattle && !playerKnown)
                                        {
// #if DEBUG
//                                             System.Diagnostics.Debug.WriteLine($"{logPrefix} 使用快速战斗模式，跳过实际出兵");
// #endif
                                            this.AIBattlingArchitectures.Add(wayToTarget.A);
                                            this.PlanArchitecture = null;
                                        }
                                        else
                                        {
                                            // 🔥 核心出兵逻辑
// #if DEBUG
//                                             System.Diagnostics.Debug.WriteLine($"{logPrefix} 准备调用BuildOffensiveTroop: 目标={wayToTarget.A.Name}, 兵力={armyScaleHere}, 保留={reserve}");
// #endif
                                            bool hasCreatedTroop = this.BuildOffensiveTroop(wayToTarget.A, wayToTarget.Kind, true, ignoreReserve ? 0 : reserve);
// #if DEBUG
//                                             System.Diagnostics.Debug.WriteLine($"{logPrefix} BuildOffensiveTroop返回: {hasCreatedTroop}");
// #endif
                                            if (armyScaleHere <= reserve || !hasCreatedTroop)
                                            {
// #if DEBUG
//                                                 System.Diagnostics.Debug.WriteLine($"{logPrefix} 最终放弃目标 {wayToTarget.A.Name}: 创建部队失败 或 兵力({armyScaleHere}) <= 保留兵力({reserve})");
// #endif
                                                this.PlanArchitecture = null;
                                            }
                                            else
                                            {
// #if DEBUG
//                                                 System.Diagnostics.Debug.WriteLine($"{logPrefix} 成功出兵进攻 {wayToTarget.A.Name}!");
// #endif
                                            }
                                        }
                                    }
                                }
                            }
                    }
                    else if (this.InformationAvail())
                    {
                        // System.Diagnostics.Debug.WriteLine($\"[AI情报] {this.Name} - InformationAvail()=true, 准备放置情报\");
                        Routeway routeway = this.GetRouteway(wayToTarget, true);
                        // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - Routeway={(routeway != null ? "存在" : "null")}");
                        
                        if ((routeway != null) && ((routeway.LastPoint.BuildFundCost * (4 + ((wayToTarget.A.AreaCount >= 4) ? 2 : 0))) <= this.Fund))
                        {
                            // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - 资金检查通过");
                            double foodRateBySeason = Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.GetSeason(routeway.Length));
                            // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - 粮食检查: Food={this.Food}, FoodRateBySeason={foodRateBySeason}, FoodCeiling={this.FoodCeiling}");
                            
                            if (((this.Food * foodRateBySeason) >= (this.FoodCeiling / 3)) || this.IsSelfFoodEnoughForOffensive(wayToTarget, routeway))
                            {
                                // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - 粮食检查通过，设置PlanArchitecture={wayToTarget.A.Name}");
                                this.PlanArchitecture = wayToTarget.A;
                                Person firstHalfPerson = this.GetFirstHalfPerson("InformationAbility");
                                // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - GetFirstHalfPerson结果: {(firstHalfPerson != null ? firstHalfPerson.Name : "null")}");
                                
                                if (firstHalfPerson != null && firstHalfPerson.LocationArchitecture != null)
                                {
                                    firstHalfPerson.CurrentInformationKind = this.GetFirstHalfInformationKind();
                                    if (firstHalfPerson.CurrentInformationKind != null)
                                    {
                                        List<Architecture> gettingInfo = GettingInformationArchitectures();
                                        if (!gettingInfo.Contains(wayToTarget.A))
                                        {
                                            // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ✅ 派遣 {firstHalfPerson.Name} 前往 {wayToTarget.A.Name} 放置情报");
                                            firstHalfPerson.GoForInformation(Session.Current.Scenario.GetClosestPoint(wayToTarget.A.ArchitectureArea, this.Position));
                                        }
                                        else
                                        {
                                            // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ❌ 目标 {wayToTarget.A.Name} 已有人在放置情报");
                                        }
                                    }
                                    else
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ❌ GetFirstHalfInformationKind返回null");
                                    }
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ❌ firstHalfPerson为null或LocationArchitecture为null");
                                    this.PlanArchitecture = null;
                                }
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ❌ 粮食不足");
                            }
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[AI情报] {this.Name} - ❌ Routeway为null或资金不足");
                        }
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($\"[AI情报] {this.Name} - InformationAvail()=false\");
                    }

                }
                else
                {
// #if DEBUG
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} ❌ wayToTarget为null，无法出兵");
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 诊断信息: AIAllLinkNodes数量={this.AIAllLinkNodes.Count}, AILandLinks数量={this.AILandLinks.Count}, AIWaterLinks数量={this.AIWaterLinks.Count}");
                    
                    // 🔥 诊断：检查是否真的没有敌对建筑（仅用于调试，不影响游戏逻辑）
                    int hostileCount = 0;
                    int displayCount = 0;
                    ArchitectureList allArchs = Session.Current.Scenario.Architectures;
                    
// #if DEBUG
//                     for (int i = 0; i < allArchs.Count; i++)
//                     {
//                         Architecture arch = allArchs[i] as Architecture;
//                         if (this.IsArchitectureHostile(arch))
//                         {
//                             hostileCount++;
//                             if (displayCount < 3)
//                             {
//                                 double distance = Session.Current.Scenario.GetSimpleDistance(this.Position, arch.Position);
//                                 System.Diagnostics.Debug.WriteLine($"{logPrefix}   发现敌对建筑: {arch.Name}({arch.BelongedFaction.Name}), 直线距离={distance:F1}");
//                                 displayCount++;
//                             }
//                         }
//                     }
//                     System.Diagnostics.Debug.WriteLine($"{logPrefix} 共发现 {hostileCount} 个敌对建筑");
//                     
//                     // 🔥 尝试重新生成链接图
//                     if (this.AIAllLinkNodes.Count == 0 && hostileCount > 0)
//                     {
//                         System.Diagnostics.Debug.WriteLine($"{logPrefix} 检测到孤立状态，尝试重新生成链接图...");
//                         this.PrepareAI();
//                         System.Diagnostics.Debug.WriteLine($"{logPrefix} 重新生成后: AIAllLinkNodes={this.AIAllLinkNodes.Count}");
//                     }
// #endif
                }
            }
        }

        public bool PersonConveneAvail()
        {
            int num = 0;
            if (this.BelongedFaction != null)
            {
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture != this)
                    {
                        num += architecture.PersonsExcludeNvGuan.Count;
                    }
                }
            }
            return (num > 0);
        }

        public bool PersonStudySkillAvail()
        {
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableSkill)
                {
                    return true;
                }
            }
            return false;
        }

        public bool PersonStudyStuntAvail()
        {
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableStunt)
                {
                    return true;
                }
            }
            return false;
        }

        public bool PersonStudyTitleAvail()
        {
            foreach (Person person in this.Persons)
            {
                if (person.HasLearnableTitle)
                {
                    return true;
                }
            }
            return false;
        }

        public bool PersonTransferAvail()
        {
            return ((this.BelongedFaction != null) && ((this.MovablePersons.Count > 0) && (this.BelongedFaction.ArchitectureCount > 1)));
        }

        public bool TransportAvailable()
        {
            return this.BelongedFaction != null && (this.Fund > 0 || this.Food > 0) && this.BelongedFaction.ArchitectureCount > 1;
        }

        public void PlayerAISearch()
        {
            this.AIAutoSearch();
        }

        public void PlayerAIWork()
        {
            this.AIWork(true);
        }

        public void PlayerAIRecruit()
        {
            this.AutoRecruit();
        }

        public void PlayerAIHire()
        {
            if (this.ConvincePersonAvail())
            {
                if (this.NoFactionPersons.Count > 0)
                {
                    ConvinceNoFactionAI();
                }

                if (this.Captives.Count > 0)
                {
                    ConvinceCaptivesAI(this);
                }
            }
        }



        public void PlayerAutoAI()
        {
            if (this.AutoWorking)
            {
                this.PlayerAIWork();
            }
            if (this.AutoRecruiting)
            {
                this.PlayerAIRecruit();
            }
            if (this.AutoHiring)
            {
                this.PlayerAIHire();
            }
            if (this.AutoSearching)
            {
                this.PlayerAISearch();
            }
            if (this.AutoRewarding)
            {
                AI_RewardManager rewardManager = new();
                rewardManager.ExecuteArchitectureAutoReward(this);
            }
           /* if (this.AutoZhaoXian)
            {
                this.PlayAIZhaoXian();
            }*/
        }

        public void callReturnedOfficerToWork()
        {
            // 🔥 NRE Fix: Check for null BelongedSection
            bool isSectionAutoRun = (this.BelongedSection != null && this.BelongedSection.AIDetail != null && this.BelongedSection.AIDetail.AutoRun);

            if (this.AutoWorking || isSectionAutoRun)
            {
                if (isSectionAutoRun)
                {
                    this.AIWork(false);
                }
                else
                {
                    this.PlayerAIWork();
                    if (this.AutoRecruiting)
                    {
                        this.PlayerAIRecruit();
                    }
                }
            }
            if (this.AutoSearching)
            {
                this.PlayerAISearch();
            }

        }

        private void PopulationEscapeEvent()
        {
            if ((((!this.DayAvoidPopulationEscape && _architectureKind.HasPopulation) && ((this.Domination < this.DominationCeiling) && (this.RecentlyAttacked > 0))) && ((this.Population > (0x3e8 * this.AreaCount)) && (this.Morale < this.MoraleCeiling))) && (GameObject.Random(((int)Math.Pow((double)(this.Domination + this.Morale), 2.0)) + 0x3e8) < GameObject.Random(0x3e8)))
            {
                int num = 0;
                int maxValue = this.Population / 100;
                foreach (Architecture architecture in this.GetAILinks().GetRandomList())
                {
                    if (architecture.Kind.HasPopulation)
                    {
                        //architecture.AddPopulationPack((int)(Session.Current.Scenario.GetDistance(this.ArchitectureArea, architecture.ArchitectureArea) / 2.0), 1 + GameObject.Random(maxValue));
                        architecture.AddPopulationPack((int)(Session.Current.Scenario.GetDistance(this.ArchitectureArea, architecture.ArchitectureArea) / 2.0) * Session.Parameters.DayInTurn, 1 + GameObject.Random(maxValue));
                        num++;
                    }
                    if (num >= 100)
                    {
                        break;
                    }
                }
                if (num > 0)
                {
                    int decrement = maxValue * num;
                    int militaryPopDecrement = (int) (decrement * ((float)MilitaryPopulation / Population));
                    this.DecreasePopulation(decrement);
                    this.DecreaseMilitaryPopulation(militaryPopDecrement);
                    ExtensionInterface.call("PopulationEscape", new Object[] { Session.Current.Scenario, this, decrement });
                    if (this.OnPopulationEscape != null)
                    {
                        this.OnPopulationEscape(this, decrement);
                    }
                }
            }
        }

        public string PopulationInInformationLevel(InformationLevel level)
        {
            switch (level)
            {
                case InformationLevel.未知:
                    return "----";

                case InformationLevel.无:
                    return "----";

                case InformationLevel.低:
                    return StaticMethods.GetNumberStringByGranularity(this.Population, 0x186a0);

                case InformationLevel.中:
                    return StaticMethods.GetNumberStringByGranularity(this.Population, 0xc350);

                case InformationLevel.高:
                    return StaticMethods.GetNumberStringByGranularity(this.Population, 0x2710);

                case InformationLevel.全:
                    return this.Population.ToString();
            }
            return "----";
        }

        public string MilitaryPopulationInInformationLevel(InformationLevel level)
        {
            switch (level)
            {
                case InformationLevel.未知:
                    return "----";

                case InformationLevel.无:
                    return "----";

                case InformationLevel.低:
                    return StaticMethods.GetNumberStringByGranularity(this.MilitaryPopulation, 0x186a0);

                case InformationLevel.中:
                    return StaticMethods.GetNumberStringByGranularity(this.MilitaryPopulation, 0xc350);

                case InformationLevel.高:
                    return StaticMethods.GetNumberStringByGranularity(this.MilitaryPopulation, 0x2710);

                case InformationLevel.全:
                    return this.MilitaryPopulation.ToString();
            }
            return "----";
        }


        public void PopulationPacksDayEvent()
        {
            // 🔥 根本修复：使用对象快照而非索引快照
            // 日期：2026-03-19
            // 原因：
            //   1. 索引快照在倒序删除时仍可能失效（如果多个连续索引都需要删除）
            //   2. ReceivePopulation 可能触发事件，事件处理器可能间接修改集合
            // 解决方案：先收集需要处理的对象本身（而非索引），再统一删除和处理
            // 性能：Cold Path（每回合一次），使用 List 分配可接受
            
            // 处理 PopulationPacks
            List<PopulationPack> toRemove = [];
            foreach (PopulationPack pack in this.PopulationPacks)
            {
                pack.Days -= Session.Parameters.DayInTurn;
                
                if (pack.Days <= 0)
                {
                    toRemove.Add(pack);
                }
            }
            
            // 🔥 关键：先删除，再处理（避免 ReceivePopulation 事件触发时集合状态不一致）
            foreach (PopulationPack pack in toRemove)
            {
                this.PopulationPacks.Remove(pack);
            }
            
            foreach (PopulationPack pack in toRemove)
            {
                this.ReceivePopulation(pack.Population);
            }

            // 处理 MilitaryPopulationPacks
            toRemove.Clear();
            foreach (PopulationPack pack in this.MilitaryPopulationPacks)
            {
                pack.Days -= Session.Parameters.DayInTurn;
                
                if (pack.Days <= 0)
                {
                    toRemove.Add(pack);
                }
            }
            
            // 🔥 关键：先删除，再处理
            foreach (PopulationPack pack in toRemove)
            {
                this.MilitaryPopulationPacks.Remove(pack);
            }
            
            // 🔥 关键修复：ReceiveMilitaryPopulation 内部会调用 ReceivePopulation
            // 所以这里只调用 ReceiveMilitaryPopulation，不要重复调用
            foreach (PopulationPack pack in toRemove)
            {
                this.ReceiveMilitaryPopulation(pack.Population);
            }
        }

        /*
        public void PostCreateTroop(Troop troop, bool hand)
        {
            if ((this.BelongedFaction != null) && this.HasSpy)
            {
                this.AddMessageToTodayNewTroopSpyMessage(troop, hand);
            }
        }
        */

        private void PrepareAI()
        {
            // 🔥 FIX: 确保 AIAllLinkNodes 已初始化
            if (this.AIAllLinkNodes == null || this.AIAllLinkNodes.Count == 0)
            {
                // 🔥 修复：如果基础链接也为空，先重新生成基础链接
                if (this.AILandLinks.Count == 0 && this.AIWaterLinks.Count == 0)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PrepareAI] {this.Name} AILandLinks和AIWaterLinks都为空，正在重新生成基础链接...");
#endif
                    this.FindLinks(Session.Current.Scenario.Architectures);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PrepareAI] {this.Name} FindLinks完成: AILandLinks={this.AILandLinks.Count}, AIWaterLinks={this.AIWaterLinks.Count}");
#endif
                }
                
                this.GenerateAllAILinkNodes(2);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PrepareAI] {this.Name} GenerateAllAILinkNodes完成: AIAllLinkNodes={this.AIAllLinkNodes.Count}");
#endif
            }
            
            this.TotalHostileForce = 0;
            this.TotalFriendlyForce = 0;
            TroopList hostileTroopsInView = this.GetHostileTroopsInView();
            foreach (Troop troop in hostileTroopsInView)
            {
                this.TotalHostileForce += troop.FightingForce;
            }
            Legion defensiveLegion = this.ResolveDefensiveLegion();
            if (defensiveLegion == null)
            {
                TroopList friendlyTroopsInView = this.GetFriendlyTroopsInView();
                foreach (Troop troop in friendlyTroopsInView)
                {
                    this.TotalFriendlyForce += troop.FightingForce;
                }
            }
            else
            {
                foreach (Troop i in defensiveLegion.Troops)
                {
                    this.TotalFriendlyForce += i.FightingForce;
                }
            }

            this.TotalStoredForce = 0;
            if (this.PersonsExcludeNvGuan.Count > 0) {
                int fightingMerit = 0;

                GameObjectList maxList = this.PersonsExcludeNvGuan.GetList();
                maxList.PropertyName = "FightingForce";
                maxList.IsNumber = true;
                maxList.SmallToBig = false;
                maxList.ReSort();
                int cnt = 0;
                foreach (Person p in maxList)
                {
                    cnt++;
                    fightingMerit += p.FightingForce;
                    if (cnt > 5 || cnt > this.MilitaryCount) break;
                }
                int avgFightingMerit = fightingMerit / cnt;
                foreach (Military m in this.Militaries)
                {
                    this.TotalStoredForce += (int) (((m.Offence + m.Defence) * m.Morale / 100.0 + m.Combativity / 4) * fightingMerit / 100);
                }
            }
        }

        public void PurifyFacilityInfluences()
        {
            foreach (Facility facility in this.Facilities)
            {
                if (facility.MaintenanceCost > 0)
                {
                    facility.Influences.PurifyInfluence(this, Applier.Facility, facility.ID);
                }
            }
        }

        private void QuickSortArchitecturesDistance(ArchitectureList List, int begin, int end)
        {
            if (begin < end)
            {
                int num = this.QuickSortPartitionArchitecturesDistance(List, begin, end);
                if (begin < (num - 1))
                {
                    this.QuickSortArchitecturesDistance(List, begin, num - 1);
                }
                if ((num + 1) < end)
                {
                    this.QuickSortArchitecturesDistance(List, num + 1, end);
                }
            }
        }

        private int QuickSortPartitionArchitecturesDistance(ArchitectureList List, int begin, int end)
        {
            Architecture architecture = List[begin] as Architecture;
            int simpleDistance = Session.Current.Scenario.GetSimpleDistance(architecture.Position, this.Position);
            int num2 = begin;
            while (begin < end)
            {
                int num3 = Session.Current.Scenario.GetSimpleDistance((List[end] as Architecture).Position, this.Position);
                while ((begin < end) && (num3 >= simpleDistance))
                {
                    end--;
                    num3 = Session.Current.Scenario.GetSimpleDistance((List[end] as Architecture).Position, this.Position);
                }
                if (begin >= end)
                {
                    return begin;
                }
                this.QuickSortSwapArchitectureDistance(List, begin, end);
                begin++;
                for (num3 = Session.Current.Scenario.GetSimpleDistance((List[begin] as Architecture).Position, this.Position); (begin < end) && (num3 <= simpleDistance); num3 = Session.Current.Scenario.GetSimpleDistance((List[begin] as Architecture).Position, this.Position))
                {
                    begin++;
                }
                if (begin >= end)
                {
                    return begin;
                }
                this.QuickSortSwapArchitectureDistance(List, begin, end);
                end--;
            }
            return begin;
        }

        private void QuickSortSwapArchitectureDistance(ArchitectureList List, int i, int j)
        {
            GameObject obj2 = List[i];
            List[i] = List[j];
            List[j] = obj2;
        }

        public ArchitectureDamage ReceiveAttackDamage(ArchitectureDamage receivedDamage)
        {
            if (receivedDamage.Damage > 0)
            {
                int maxValue = 2 + (receivedDamage.Damage / 5);
                this.DecreaseAgriculture(GameObject.Random(maxValue));
                this.DecreaseCommerce(GameObject.Random(maxValue));
                this.DecreaseTechnology(GameObject.Random(maxValue));
                this.DecreaseMorale(GameObject.Random(maxValue));
                ExtensionInterface.call("ArchitectureReceiveDamage", new Object[] { Session.Current.Scenario, this, receivedDamage });
            }
            return receivedDamage;
        }

        private void ReceivePopulation(int quantity)
        {
            int population = this.Population;
            quantity = this.IncreasePopulation(quantity);
            if (quantity > 0)
            {
                if (this.BelongedFaction != null)
                {
                    float decrease = this.Domination - ((float) this.Domination * population) / this.Population;
                    if (decrease > 1)
                    {
                        this.Domination -= (int) decrease;
                        decrease -= (int)decrease;
                    }
                    if (GameObject.Chance((int) (decrease * 100)))
                    {
                        this.Domination--;
                    }

                    decrease = this.Morale - ((float)this.Morale * population) / this.Population;
                    if (decrease > 1)
                    {
                        this.Morale -= (int)decrease;
                        decrease -= (int)decrease;
                    }
                    if (GameObject.Chance((int)(decrease * 100)))
                    {
                        this.Morale--;
                    }
                }
                if (this.OnPopulationEnter != null)
                {
                    this.OnPopulationEnter(this, quantity);
                }
                ExtensionInterface.call("ReceivePopulation", new Object[] { Session.Current.Scenario, this, quantity });
            }
        }

        private void ReceiveMilitaryPopulation(int quantity)
        {
            this.ReceivePopulation(quantity);
            this.IncreaseMilitaryPopulation(quantity);
        }

        public bool RecruitmentAvail()
        {
            if (this.HasPerson())
            {
                if (this.youzainan)
                {
                    return false;
                }
                if (!_architectureKind.HasPopulation || !_architectureKind.HasMorale)
                {
                    return false;
                }
                if (Session.GlobalVariables.PopulationRecruitmentLimit && (this.ArmyQuantity > this.Population))
                {
                    return false;
                }
                if (this.BelongedFaction != null && this.BelongedFaction.Army > (long)(this.BelongedFaction.Population * Session.Current.Scenario.Parameters.MilitaryPopulationCap)) //势力兵力超过上限时，不能补充,原有参数替换成在主菜单设置可调的参数
                {
                    return false;
                }
                if (this.Population <= 0 || this.MilitaryPopulation <= 0)
                {
                    return false;
                }
                if (this.Domination < Session.Parameters.RecruitmentDomination)
                {
                    return false;
                }
                if (this.Morale < Session.Parameters.RecruitmentMorale)
                {
                    // return false; 
                    // 🔧 FIX: 允许低士气补兵/新建，士气低不应该完全禁止
                }
                
                // 🔥 修复：补充按钮只有在城内有未满员的编队时才点亮
                // 日期：2026-03-18
                // 原因：补充是给现有编队补充兵力，需要检查是否有编队需要补充
                if (!this.HasMilitary())
                {
                    return false;
                }
                
                // 检查是否有未满员的编队
                foreach (Military military in this.Militaries)
                {
                    if (military.TotalQuantity < military.Kind.MaxScale)
                    {
                        return true;  // 有未满员的编队，可以补充
                    }
                }
                
                return false;  // 所有编队都满员了，不需要补充
            }
            return false;
        }

#pragma warning disable CS0414 // The field 'Architecture.crlm_recurse_level' is assigned but its value is never used
        private int crlm_recurse_level = 0;
#pragma warning restore CS0414 // The field 'Architecture.crlm_recurse_level' is assigned but its value is never used
        private bool CanRecruitLowerMilitary_r(MilitaryKind mk)
        {
            MilitaryKind current;
            Dictionary<int, MilitaryKind>.ValueCollection.Enumerator enumerator;
            using (enumerator = this.BelongedFaction.AvailableMilitaryKinds.MilitaryKinds.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    current = enumerator.Current;
                    if (current == mk)
                    {
                        return true;
                    }
                }
            }
            using (enumerator = this.PrivateMilitaryKinds.MilitaryKinds.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    current = enumerator.Current;
                    if (current == mk)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool CanRecruitMilitary(MilitaryKind mk)
        {
            crlm_recurse_level = 0;
            return CanRecruitLowerMilitary_r(mk);
        }

        private void RecruitmentMilitary(Military military)
        {
            if (this.BelongedFaction != null &&
                !Session.Current.Scenario.IsPlayer(this.BelongedFaction) &&
                !this.CanExecuteAIRecruitment(false))
            {
                if (military != null)
                {
                    military.StopRecruitment();
                }
                return;
            }

            if ((((this.MilitaryPopulation != 0) && (this.Population != 0) && (!Session.GlobalVariables.PopulationRecruitmentLimit
                || (this.ArmyQuantity <= this.Population))) && ((this.Fund >= (Session.Parameters.RecruitmentFundCost * this.AreaCount * (this.CanRecruitMilitary(military.Kind) ? 1 : 10))))
                && (this.Domination >= Session.Parameters.RecruitmentDomination)
                && ((military.RecruitmentPerson != null) && (military.RecruitmentPerson.BelongedFaction != null)
                && (military.Quantity < military.Kind.MaxScale)) && (military.BelongedFaction != null)))
            {

                int randomValue = StaticMethods.GetRandomValue((int)((military.RecruitmentPerson.RecruitmentAbility * military.Kind.MinScale) * Session.Parameters.RecruitmentRate), 0x7d0);
                randomValue = (int)((float)randomValue * Math.Min(1.0f, ((float)this.Population * Session.Current.Scenario.Parameters.RecruitPopualationDecreaseRate / Math.Max(1, this.ArmyQuantity))));
                int populationDecrement;

                if ((randomValue + military.Quantity) > military.Kind.MaxScale)
                {
                    randomValue = military.Kind.MaxScale - military.Quantity;
                }
                if ((randomValue * military.Kind.PointsPerSoldier) > military.BelongedFaction.TechniquePoint && military.Kind.PointsPerSoldier != 0)
                {
                    if (!(((this.BelongedSection == null) || (this.BelongedSection.AIDetail == null)) || this.BelongedSection.AIDetail.AutoRun))
                    {
                        military.BelongedFaction.DepositTechniquePointForTechnique(randomValue * military.Kind.PointsPerSoldier);
                    }
                    randomValue = military.BelongedFaction.TechniquePoint / military.Kind.PointsPerSoldier;
                }
                populationDecrement = randomValue;
                if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                {
                    randomValue = (int)(randomValue * Session.Parameters.AIRecruitmentSpeedRate);
                }
                if (randomValue > 0)
                {
                    this.DecreaseFund(Session.Parameters.RecruitmentFundCost * this.AreaCount * (this.CanRecruitMilitary(military.Kind) ? 1 : 10));
                    if (populationDecrement > this.MilitaryPopulation)
                    {
                        populationDecrement = this.MilitaryPopulation;
                        randomValue = populationDecrement;
                    }
                    if (populationDecrement > this.Population)
                    {
                        populationDecrement = this.Population;
                        randomValue = populationDecrement;
                    }
                    this.DecreaseMilitaryPopulation(populationDecrement);
                    this.DecreasePopulation(populationDecrement);

                    int scales = military.Scales;
                    military.IncreaseQuantity(randomValue, this.MoraleOfRecruitment, this.CombativityOfRecruitment, 0, 0);
                    /*
                    if (this.HasSpy && ((military.Scales / 10) > (scales / 10)))
                    {
                        this.AddMessageToTodayMilitaryScaleSpyMessage(military);
                    }
                    */
                    if (this.Population < this.RecruitmentPopulationBoundary)
                    {
                        this.DecreaseDomination(GameObject.Random(6));
                        this.DecreaseMorale(GameObject.Random(6) * 2);
                    }
                    else
                    {
                        this.DecreaseDomination(GameObject.Random(2));
                        this.DecreaseMorale(GameObject.Random(2) * 2);
                    }
                    this.BelongedFaction.DecreaseTechniquePoint(randomValue * military.Kind.PointsPerSoldier);
                    int increment = StaticMethods.GetRandomValue(randomValue * 10, military.Kind.MinScale);
                    if (increment > 0)
                    {
                        military.RecruitmentPerson.AddRecruitmentExperience(increment);
                        military.RecruitmentPerson.AddCommandExperience(increment / 2);
                        military.RecruitmentPerson.AddGlamourExperience(increment / 2);
                        military.RecruitmentPerson.IncreaseReputation(increment * 4);
                        military.RecruitmentPerson.IncreaseOfficerMerit(increment * 4);
                        military.RecruitmentPerson.BelongedFaction.IncreaseReputation(increment * 2);
                        military.RecruitmentPerson.BelongedFaction.IncreaseTechniquePoint(increment * 100);
                    }
                }

            }
            else
            {
                if (military.RecruitmentPerson != null)
                {
                    military.StopRecruitment();
                }
            }
        }

        public void RecruitmentMilitary(Military military, float scale)
        {
            if (this.BelongedFaction != null &&
                !Session.Current.Scenario.IsPlayer(this.BelongedFaction) &&
                !this.CanExecuteAIRecruitment(false))
            {
                if (military != null)
                {
                    military.StopRecruitment();
                }
                return;
            }

            if ((((this.MilitaryPopulation != 0) && (this.Population != 0) && (!Session.GlobalVariables.PopulationRecruitmentLimit || (this.ArmyQuantity <= this.Population))) && (this.Domination >= Session.Parameters.RecruitmentDomination)) && (military.Quantity < military.Kind.MaxScale))
            {
                int decrement = (int)(military.Kind.MinScale * scale);
                int populationDecrement;
                if ((decrement + military.Quantity) > military.Kind.MaxScale)
                {
                    decrement = military.Kind.MaxScale - military.Quantity;
                }
                if ((decrement * military.Kind.PointsPerSoldier) > military.BelongedFaction.TechniquePoint)
                {
                    if (!(((this.BelongedSection == null) || (this.BelongedSection.AIDetail == null)) || this.BelongedSection.AIDetail.AutoRun))
                    {
                        military.BelongedFaction.DepositTechniquePointForTechnique(decrement * military.Kind.PointsPerSoldier);
                    }
                    decrement = military.BelongedFaction.TechniquePoint / military.Kind.PointsPerSoldier;
                }
                populationDecrement = decrement;
                if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                {
                    decrement = (int)(decrement * Session.Parameters.AIRecruitmentSpeedRate);
                }
                if (decrement > 0)
                {
                    if (populationDecrement > this.MilitaryPopulation)
                    {
                        populationDecrement = this.MilitaryPopulation;
                        decrement = populationDecrement;
                    }
                    if (populationDecrement > this.Population)
                    {
                        populationDecrement = this.Population;
                        decrement = populationDecrement;
                    }
                    this.DecreaseMilitaryPopulation(populationDecrement);
                    this.DecreasePopulation(populationDecrement);
                    int scales = military.Scales;
                    military.IncreaseQuantity(decrement, this.MoraleOfRecruitment, this.CombativityOfRecruitment, 0, 0);
                    /*
                    if (this.HasSpy && ((military.Scales / 10) > (scales / 10)))
                    {
                        this.AddMessageToTodayMilitaryScaleSpyMessage(military);
                    }
                    */
                    if (this.Population < this.RecruitmentPopulationBoundary)
                    {
                        this.DecreaseDomination(GameObject.Random(6));
                        this.DecreaseMorale(GameObject.Random(6) * 2);
                    }
                    else
                    {
                        this.DecreaseDomination(GameObject.Random(2));
                        this.DecreaseMorale(GameObject.Random(2) * 2);
                    }
                    this.BelongedFaction.DecreaseTechniquePoint(decrement * military.Kind.PointsPerSoldier);
                    int randomValue = StaticMethods.GetRandomValue(decrement * 10, military.Kind.MinScale);
                    if (randomValue > 0)
                    {
                        military.BelongedFaction.IncreaseReputation(randomValue * 2);
                        military.BelongedFaction.IncreaseTechniquePoint(randomValue * 100);
                    }
                }

            }
        }

        public bool RedeemAvail()
        {
            if (this.FactionHasSelfCaptive())
            {
                return this.GetRedeemCaptiveList().Count > 0;
            }
            return false;
        }

        public void RefreshViewArea()
        {
            if (!Session.Current.Scenario.Preparing)
            {
                foreach (Point point in this.ViewArea.Area)
                {
                    if (!Session.Current.Scenario.PositionOutOfRange(point))
                    {
                        Session.Current.Scenario.MapTileData[point.X, point.Y].RemoveHighViewingArchitecture(this);
                    }
                }
                foreach (Point point in this.LongViewArea.Area)
                {
                    if (!Session.Current.Scenario.PositionOutOfRange(point))
                    {
                        Session.Current.Scenario.MapTileData[point.X, point.Y].RemoveViewingArchitecture(this);
                    }
                }
            }
            this.ViewArea = null;
            this.LongViewArea = null;
            foreach (Point point in this.ViewArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    Session.Current.Scenario.MapTileData[point.X, point.Y].AddHighViewingArchitecture(this);
                }
            }
            foreach (Point point in this.LongViewArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    Session.Current.Scenario.MapTileData[point.X, point.Y].AddViewingArchitecture(this);
                }
            }
        }

        public bool RegionCoreEffectAvail()
        {
            return (_architectureKind.HasTechnology && (this.Technology >= ((int)(this.TechnologyCeiling * 0.8))));
        }

        public bool RegroupSectionAvail()
        {
            return (this.BelongedFaction.SectionCount > 0);
        }

        private void ReleaseAllCaptive()
        {
            if (this.HasCaptive())
            {
                PersonList persons = new PersonList();
                foreach (Captive captive in this.Captives.GetList())
                {
                    if (((captive.CaptivePerson != null) && (captive.CaptiveFaction != null)) && (captive.CaptiveFaction.Capital != null))
                    {
                        Architecture moveTo = captive.CaptiveFaction.Capital;
                        persons.Add(captive.CaptivePerson);
                        Person p = captive.CaptivePerson;
                        captive.CaptivePerson.SetBelongedCaptive(null, PersonStatus.Normal);
                        p.MoveToArchitecture(moveTo);
                    }
                }
                if ((persons.Count > 0) && (this.OnReleaseCaptiveAfterOccupied != null))
                {
                    this.OnReleaseCaptiveAfterOccupied(this, persons);
                }
            }
        }

        public bool ReleaseCaptiveAvail()
        {
            return (this.BelongedFaction.CaptiveCount > 0);
        }

        public void RemoveBaseSupplyingArchitecture()
        {
            foreach (Point point in this.BaseFoodSurplyArea.Area)
            {
                if (!Session.Current.Scenario.PositionOutOfRange(point))
                {
                    Session.Current.Scenario.MapTileData[point.X, point.Y].RemoveSupplyingArchitecture(this);
                }
            }
        }

        public void RemoveInactiveRouteways()
        {
            foreach (Routeway routeway in this.Routeways.GetList())
            {
                if (!(routeway.Building || (routeway.LastActivePointIndex >= 0)))
                {
                    Session.Current.Scenario.RemoveRouteway(routeway);
                }
            }
        }

        public void RemoveMilitary(Military military)
        {
            this.Militaries.Remove(military);
            military.StopRecruitment();
            military.BelongedArchitecture = null;
        }

        public void RemovePopulationPack(PopulationPack pp)
        {
            this.PopulationPacks.Remove(pp);
        }

        /*
        public void RemoveSpyPack(SpyPack sp)
        {
            this.SpyPacks.Remove(sp);
        }
        */

        private void ResetAuto()
        {
            this.AutoHiring = false;
            this.AutoRewarding = false;
            this.AutoWorking = false;
            this.AutoSearching = false;
           // this.AutoZhaoXian = false;
        }

        private void ResetDayInfluence()
        {
            if (this.RecentlyAttacked > 0)
            {
                this.RecentlyAttacked--;
            }
            if (this.RecentlyBreaked > 0)
            {
                this.RecentlyBreaked--;
            }
            if (this.RecentlyHit > 0)
            {
                this.RecentlyHit--;
            }
        }

        public bool ResetDiplomaticRelationAvail()
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }
            return (this.HasFriendlyDiplomaticRelation && (this.BelongedFaction.TroopCountExcludeTransport == 0));
        }

        public bool AllyDiplomaticRelationAvail()
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }

            foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
            {
                if ((display.Relation <= Session.GlobalVariables.FriendlyDiplomacyThreshold) && (display.Relation >= Session.GlobalVariables.FriendlyDiplomacyThreshold * 0.9) && (this.Fund > 20000) && (this.PersonsExcludeNvGuan.Count > 0))
                {
                    return true;
                }
            }

            return false;
        }

        public bool EnhanceDiplomaticRelationAvail()
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }
            return ((this.Fund > 10000) && (this.PersonsExcludeNvGuan.Count > 0));
        }

        public GameObjectList GetGeDiDiplomaticRelationList() //割地
        {
            this.GeDiDiplomaticRelationList.Clear();
            if (this.BelongedFaction != null)
            {
                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if (display.LinkedFaction1 != null && display.LinkedFaction2 != null)
                    {
                        this.GeDiDiplomaticRelationList.Add(display);
                    }
                }
            }
            return this.GeDiDiplomaticRelationList;
        }

        public bool GeDiDiplomaticRelationAvail() //割地
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }

            return (this.BelongedFaction.ArchitectureCount > 1 && this.MovablePersons.Count > 0);
        }


        public bool QuanXiangDiplomaticRelationAvail() //劝降
        {

                if (!Session.GlobalVariables.PermitQuanXiang) return false;

                if (this.BelongedFaction == null)
                {
                    return false;
                }

                foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
                {
                    if ((display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold) && this.Fund > 50000 && (this.MovablePersons.Count > 0))
                    {
                        if (Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.GetQuanXiangDiplomaticRelationList().Count > 0)
                        {
                            return true;
                        }

                       /* if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction) && this.GetAIQuanXiangDiplomaticRelationList().Count > 0)
                        {
                            return true;
                        }*/
                    }
                }


                return false;

        }

        public bool TruceDiplomaticRelationAvail()
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }
            return ((this.Fund > 50000) && (this.PersonsExcludeNvGuan.Count > 0));
        }

        public bool DenounceDiplomaticRelationAvail()
        {
            if (this.BelongedFaction == null)
            {
                return false;
            }

            foreach (DiplomaticRelationDisplay display in Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelationDisplayListByFactionID(this.BelongedFaction.ID))
            {
                if ((display.Relation < Session.GlobalVariables.FriendlyDiplomacyThreshold) && (this.Fund > 120000))
                {
                    return true;
                }
            }

            return false;
        }


        public void ResetFaction(Faction faction)
        {
            Faction oldFaction = this.BelongedFaction;
            
            // 【新增】通知AI缓存管理器地图状态发生变化
            try
            {
#if !DISABLE_AI_CACHE
                if (WTKGameManager.AICacheManager.Instance != null)
                {
                    WTKGameManager.AICacheManager.Instance.SetMapDirty();
                    
                    // 清理相关势力的缓存
                    if (oldFaction != null)
                    {
                        WTKGameManager.AICacheManager.Instance.ClearCacheForFaction(oldFaction.ID);
#endif
                    }
#if !DISABLE_AI_CACHE
                    if (faction != null)
                    {
                        WTKGameManager.AICacheManager.Instance.ClearCacheForFaction(faction.ID);
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResetFaction] AI缓存更新失败: {ex.Message}");
            }
            
            this.ResetAuto();
            this.PlanFacilityKind = null;
            this.PlanFacilityKindID = -1;
            this.SuspendTroopTransfer = 0;
            if ((faction != null) && Session.Current.Scenario.IsPlayer(faction))
            {
                this.AutoHiring = true;
                // 🔧 修复：自动褒赏默认关闭，由玩家手动开启
                // this.AutoRewarding = true;

            }
            if (this.BelongedFaction != null && this.BelongedFaction != faction)
            {
                // 🔥 根本修复：城市易手时，清理所有引用该城市作为出发地的部队
                // 日期：2026-03-09
                // 问题：城市被占领后，原势力部队的 StartingArchitecture 仍然指向该城市，导致存档后读档时出现错误引用
                // 解决：遍历原势力所有部队，如果 StartingArchitecture 是当前城市，清空该引用
                // ANTI-BAND-AID：不检查 Troops 是否为 null，如果为 null 则让它崩溃暴露初始化问题
                foreach (Troop troop in this.BelongedFaction.Troops.GetList())
                {
                    if (troop.StartingArchitecture == this)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[ResetFaction] 城市 {this.Name} 易手，清空部队 {troop.DisplayName}(ID:{troop.ID}) 的出发地引用");
                        troop.StartingArchitecture = null;
                    }
                    
                    // 同时清理 WillArchitecture 引用（如果部队正在前往该城市）
                    if (troop.WillArchitecture == this && troop.Command != TroopCommand.AttackArch)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[ResetFaction] 城市 {this.Name} 易手，清空部队 {troop.DisplayName}(ID:{troop.ID}) 的目标地引用");
                        troop.WillArchitecture = null;
                        troop.RealDestination = new(-1, -1);  // C# 12: 目标类型 new
                    }
                }
                
                this.ClearFundPacks();
                this.ClearFoodPacks();
                this.ClearRouteways();
                this.ReleaseAllCaptive();
                this.PurifyFactionInfluences();
                if (this.BelongedSection != null)
                {
                    this.BelongedSection.RemoveArchitecture(this);
                }
                this.DefensiveLegion = null;
                this.DefensiveLegionID = -1;
                if (this == this.BelongedFaction.Capital || this.BelongedFaction.ArchitectureCount <= 1)
                {
                    Person leader = this.BelongedFaction.Leader;
                    while (this.Persons.Count > 0)
                    {
                        Person person2 = this.Persons[0] as Person;

                        Session.Current.Scenario.YearTable.addBecomeNoFactionDueToDestructionEntry(Session.Current.Scenario.Date, person2, this.BelongedFaction);
                        person2.Status = PersonStatus.NoFaction;
                        person2.LocationArchitecture = this;
                        if (leader == person2)
                        {
                            person2.Reputation = (int)(person2.Reputation * 0.6);
                        }
                        else
                        {
                            person2.Reputation = (int)(person2.Reputation * 0.95);
                        }

                        int hateDays = (int)((Math.Pow(5, person2.PersonalLoyalty - 2) * 10 + GameObject.Random(20) - 30) * Session.GlobalVariables.ProhibitFactionAgainstDestroyer);
                        if (hateDays > 0)
                        {
                            if (person2.ProhibitedFactionID.ContainsKey(faction.ID))
                            {
                                person2.ProhibitedFactionID[faction.ID] += hateDays;
                            }
                            else
                            {
                                person2.ProhibitedFactionID.Add(faction.ID, hateDays);
                            }
                        }
                    }
                    
                    while (this.MovingPersons.Count > 0)
                    {
                        Person person2 = this.MovingPersons[0] as Person;
                        Session.Current.Scenario.YearTable.addBecomeNoFactionDueToDestructionEntry(Session.Current.Scenario.Date, person2, this.BelongedFaction);
                        person2.OutsideTask = OutsideTaskKind.无;
                        person2.TaskDays = 0;
                        person2.Status = PersonStatus.NoFactionMoving;

                        person2.LocationArchitecture = this;
                        person2.TargetArchitecture = null;

                        if (leader == person2)
                        {
                            person2.Reputation = (int)(person2.Reputation * 0.6);
                        }
                        else
                        {
                            person2.Reputation = (int)(person2.Reputation * 0.95);
                        }

                        int hateDays = (int)(Math.Pow(5, person2.PersonalLoyalty - 2) * 10 + GameObject.Random(20) - 10);
                        if (hateDays > 0)
                        {
                            if (person2.ProhibitedFactionID.ContainsKey(faction.ID))
                            {
                                person2.ProhibitedFactionID[faction.ID] += hateDays;
                            }
                            else
                            {
                                person2.ProhibitedFactionID.Add(faction.ID, hateDays);
                            }
                        }
                    }

                    //if ((leader.LocationTroop == null) || leader.IsCaptive)
                    {
                        TroopList list = new TroopList();
                        foreach (Troop troop in this.BelongedFaction.Troops)
                        {
                            list.Add(troop);
                        }
                        foreach (Troop troop in list)
                        {
                            troop.FactionDestroy();
                        }
                        if (faction != null)
                        {
                            faction.CheckLeaderDeath(leader);
                        }
                        this.BelongedFaction.Destroy();

                    }
                    this.BelongedFaction.Capital = null;
                }
                else
                {
                    while (this.Persons.Count > 0)
                    {
                        if ((this.Persons[0] as Person).LocationArchitecture != null)
                        {
                            (this.Persons[0] as Person).MoveToArchitecture(this.BelongedFaction.Capital, this.Position, false, true, null);
                        }
                    }
                    while (this.MovingPersons.Count > 0)
                    {
                        if ((this.MovingPersons[0] as Person).LocationArchitecture != null)
                        {
                            (this.MovingPersons[0] as Person).MoveToArchitecture(this.BelongedFaction.Capital, this.Position, false, true, null);
                        }
                    }
                    foreach (Military m in this.BelongedFaction.TransferingMilitaries.GetList())
                    {
                        if (m.TargetArchitecture == this)
                        {
                            this.TransferMilitary(m, this.BelongedFaction.Capital);
                        }
                    }
                }
                if (this.BelongedFaction != null)
                {
                    this.BelongedFaction.RemoveArchitectureMilitaries(this);
                    this.BelongedFaction.RemoveArchitectureKnownData(this);
                    this.BelongedFaction.RemoveArchitecture(this);
                }
                if (faction != null)
                {
                    faction.AddArchitecture(this);
                    this.ApplyFactionInfluences();
                    faction.AddArchitectureMilitaries(this);
                }
                else
                {
                    this.BelongedFaction = null;
                }
            }
            else if (faction != null)
            {
                faction.AddArchitecture(this);
                this.ApplyFactionInfluences();
                faction.AddArchitectureMilitaries(this);
            }

            if (faction != null)
            {
                //this.jianzhuqizi.qizidezi.Text = faction.ToString().Substring(0, 1);
            }

            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                architecture.RefreshViewArea();
            }
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                troop.RefreshViewArchitectureRelatedArea();
            }
            this.AIBattlingArchitectures.Clear();
            foreach (LinkNode i in this.AIAllLinkNodes.Values)
            {
                i.A.CheckIsFrontLine();
            }
            this.CheckIsFrontLine();
            ExtensionInterface.call("ArchitectureResetFaction", new Object[] { Session.Current.Scenario, this, oldFaction });
        }

        private void ReSortAllWeighingList(PersonList zhenzaiPersons, PersonList agriculturePersons, PersonList commercePersons,
            PersonList technologyPersons, PersonList dominationPersons, PersonList moralePersons, PersonList endurancePersons,
            PersonList recruitmentPersons, PersonList trainingPersons, MilitaryList weighingMilitaries)
        {
            PersonList pl = this.Persons;
            zhenzaiPersons.Clear();
            if (this.kezhenzai())
            {
                foreach (Person person in pl)
                {
                    zhenzaiPersons.Add(person);
                }
                zhenzaiPersons.IsNumber = true;
                zhenzaiPersons.PropertyName = "zhenzaiWeighing";
                zhenzaiPersons.ReSort();
            }
            agriculturePersons.Clear();
            if (_architectureKind.HasAgriculture)
            {
                foreach (Person person in pl)
                {
                    agriculturePersons.Add(person);
                }
                agriculturePersons.IsNumber = true;
                agriculturePersons.PropertyName = "AgricultureWeighing";
                agriculturePersons.ReSort();
            }
            commercePersons.Clear();
            if (_architectureKind.HasCommerce)
            {
                foreach (Person person in pl)
                {
                    commercePersons.Add(person);
                }
                commercePersons.IsNumber = true;
                commercePersons.PropertyName = "CommerceWeighing";
                commercePersons.ReSort();
            }
            technologyPersons.Clear();
            if (_architectureKind.HasTechnology)
            {
                foreach (Person person in pl)
                {
                    technologyPersons.Add(person);
                }
                technologyPersons.IsNumber = true;
                technologyPersons.PropertyName = "TechnologyWeighing";
                technologyPersons.ReSort();
            }
            dominationPersons.Clear();
            if (_architectureKind.HasDomination)
            {
                foreach (Person person in pl)
                {
                    dominationPersons.Add(person);
                }
                dominationPersons.IsNumber = true;
                dominationPersons.PropertyName = "DominationWeighing";
                dominationPersons.ReSort();
            }
            moralePersons.Clear();
            if (_architectureKind.HasMorale)
            {
                foreach (Person person in pl)
                {
                    moralePersons.Add(person);
                }
                moralePersons.IsNumber = true;
                moralePersons.PropertyName = "MoraleWeighing";
                moralePersons.ReSort();
            }
            endurancePersons.Clear();
            if (_architectureKind.HasEndurance)
            {
                foreach (Person person in pl)
                {
                    endurancePersons.Add(person);
                }
                endurancePersons.IsNumber = true;
                endurancePersons.PropertyName = "EnduranceWeighing";
                endurancePersons.ReSort();
            }
            trainingPersons.Clear();
            foreach (Person person in pl)
            {
                trainingPersons.Add(person);
            }
            trainingPersons.IsNumber = true;
            trainingPersons.PropertyName = "TrainingWeighing";
            trainingPersons.ReSort();
            recruitmentPersons.Clear();
            foreach (Person person in this.Persons)
            {
                recruitmentPersons.Add(person);
            }
            recruitmentPersons.IsNumber = true;
            recruitmentPersons.PropertyName = "RecruitmentWeighing";
            recruitmentPersons.ReSort();
            weighingMilitaries.Clear();
            foreach (Military military in this.Militaries)
            {
                weighingMilitaries.Add(military);
            }
            weighingMilitaries.IsNumber = true;
            weighingMilitaries.PropertyName = "Weighing";
            weighingMilitaries.ReSort();
        }

        public bool RoutewayAvail()
        {
            if (!CaiyongLiangdaoXitong()) return false;
            foreach (Point point in this.GetRoutewayStartArea().Area)
            {
                if (this.IsRoutewayPossible(point))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CaiyongLiangdaoXitong()
        {
            if (Session.GlobalVariables.LiangdaoXitong == false)
            {
                return false;
            }
            else
            {
                return true;
            }

        }


        public string SaveFundPacksToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (FundPack pack in this.FundPacks)
            {
                builder.Append(string.Concat(new object[] { pack.Fund, " ", pack.Days, " " }));
            }
            return builder.ToString();
        }

        public string SaveFoodPacksToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (FoodPack pack in this.FoodPacks)
            {
                builder.Append(string.Concat(new object[] { pack.Food, " ", pack.Days, " " }));
            }
            return builder.ToString();
        }

        public string SavePopulationPacksToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (PopulationPack pack in this.PopulationPacks)
            {
                builder.Append(string.Concat(new object[] { pack.Days, " ", pack.Population, " " }));
            }
            return builder.ToString();
        }

        /*
        public string SaveSpyPacksToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (SpyPack pack in this.SpyPacks)
            {
                builder.Append(string.Concat(new object[] { pack.SpyPerson.ID, " ", pack.Days, " " }));
            }
            return builder.ToString();
        }
        */

        public bool SearchAvail()
        {
            return this.HasPerson();
        }

        public void SellFood(int spendFood)
        {
            this.DecreaseFood(spendFood);
            this.IncreaseFund(spendFood / Session.Parameters.FoodToFundDivisor);
            // 🔥 AOT 重构：使用强类型事件替代反射调用
            WorldOfTheThreeKingdoms.GameObjects.Events.InternalAffairEvents.RaiseSellFood(Session.Current.Scenario, this);
        }

        public bool SellFoodAvail()
        {
            // 🔥 技术性修复：避免ArgumentNullException
            string archName = this.Name ?? "";
            
            return this.Commerce >= Session.Parameters.SellFoodCommerce && (Session.Current.Scenario.Date.Season == GameSeason.冬 || Session.Current.Scenario.Date.Season == GameSeason.春) && this.Food > 0 && this.Fund < this.FundCeiling
                && (Session.Current.Scenario.Date.Month * 671
                + (archName.Length > 0 ? archName[0] : 321) * 864
                + (archName.Length > 1 ? archName[1] : 384) * 259
                    + this.ID * 513) % 2 == 0;
        }

        /// <summary>
        /// 出售宝物到市场
        /// 🔥 2026-03-03 新增：宝物进入市场，不隐藏在建筑中
        /// 🧊 COLD PATH：UI事件响应，优先可读性
        /// </summary>
        public void SellTreasure(Treasure treasure)
        {
            // 1. 增加资金（宝物价值 × 1000）
            this.IncreaseFund(treasure.Worth * 1000);
            
            // 2. 设置宝物状态为"已出售"
            treasure.Available = false;
            treasure.HidePlace = null;  // ✅ 不隐藏在建筑中
            treasure.BelongedPerson = null;
            
            // 3. 添加到市场
            Session.Current.Scenario.SoldTreasures.Add(treasure);
            
            // 4. 触发事件
            WorldOfTheThreeKingdoms.GameObjects.Events.TreasureEvents.RaiseSellTreasure(Session.Current.Scenario, this, treasure);
        }

        /// <summary>
        /// 从市场购买宝物
        /// 🔥 2026-03-03 新增：购买价格为原价的 1.2 倍
        /// 🧊 COLD PATH：UI事件响应，优先可读性
        /// </summary>
        public void BuyTreasure(Treasure treasure)
        {
            // 1. 计算购买价格（加价 20%，宝物价值 × 1000）
            int buyPrice = (int)(treasure.Worth * 1.2f * 1000);
            
            // 2. 扣除资金
            this.DecreaseFund(buyPrice);
            
            // 3. 设置宝物状态为"持有"
            treasure.Available = true;
            treasure.BelongedPerson = this.BelongedFaction.Leader;
            treasure.HidePlace = null;
            
            // 4. 从市场移除
            Session.Current.Scenario.SoldTreasures.Remove(treasure);
            
            // 5. 触发事件
            WorldOfTheThreeKingdoms.GameObjects.Events.TreasureEvents.RaiseBuyTreasure(Session.Current.Scenario, this, treasure);
        }

        /// <summary>
        /// 检查是否可以购买宝物
        /// 🔥 2026-03-03 新增：明确检查势力和君主，符合 Anti-Band-Aid Protocol
        /// 🧊 COLD PATH：菜单可见性检查
        /// </summary>
        public bool BuyTreasureAvail()
        {
            // 检查市场是否有宝物
            if (Session.Current.Scenario.SoldTreasures.Count == 0)
                return false;
            
            // 检查建筑是否有归属势力
            if (this.BelongedFaction == null)
                return false;
            
            // 检查势力是否有君主
            if (this.BelongedFaction.Leader == null)
                return false;
            
            return true;
        }

        public void SetLongViewArea(GameArea area)
        {
            this.longViewArea = area;
        }

        public void SetRecentlyAttacked()
        {
            if (this.RecentlyAttacked <= 0)
            {
                ExtensionInterface.call("ArchitectureBeingAttacked", new Object[] { Session.Current.Scenario, this });
                this.JustAttacked = true;
                
                // 🔥 NEW: 通知势力AI紧急事件 - 城市被攻击
                if (this.BelongedFaction != null)
                {
                    this.BelongedFaction.NotifyPersonnelUrgentEvent($"城市{this.Name}被攻击");
                    this.BelongedFaction.NotifyDomesticUrgentEvent($"城市{this.Name}被攻击");
                }
                
                /*
                if (this.BelongedFaction != null)
                {
                    this.BelongedFaction.StopToControl = true;
                }
                if (this.OnBeginRecentlyAttacked != null)
                {
                    this.OnBeginRecentlyAttacked(this);
                }
                */

            }
            this.RecentlyAttacked = 10;
            this.RecentlyHit = 10;
            //this.AttackedReminder();
        }

        public void SetViewArea(GameArea area)
        {
            this.viewArea = area;
        }

        private void Sourrounded()
        {
            if (((this.BelongedFaction != null) && (this.Endurance > 0)) && _architectureKind.HasDomination)
            {
                int num = 0;
                foreach (Point point in this.ContactArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (!((troopByPosition == null) || this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction)))
                    {
                        num++;
                    }
                }
                if (num > this.AreaCount)
                {
                    int decrement = (num - this.AreaCount) * Session.Parameters.SurroundArchitectureDominationUnit;
                    decrement = this.DecreaseDomination(decrement);
                    ExtensionInterface.call("ArchitectureSurrounded", new Object[] { Session.Current.Scenario, this });
                    if (decrement > 0)
                    {
                        this.DecrementNumberList.AddNumber(decrement, CombatNumberKind.士气, this.Position);
                    }
                }
            }
        }

        public bool IsSurrounded()
        {
            if (((this.BelongedFaction != null) && (this.Endurance > 0)))
            {
                int num = 0;
                foreach (Point point in this.ContactArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (!((troopByPosition == null) || this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction)))
                    {
                        num++;
                    }
                }
                if (num > this.AreaCount)
                {
                    return true;
                }
            }
            if (this.Endurance <= 0)
            {
                foreach (Point point in this.ContactArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (!((troopByPosition == null) || this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction)))
                    {
                        return true;
                    }
                }
                foreach (Point point in this.ArchitectureArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (!((troopByPosition == null) || this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /*
        public bool SpyAvail()
        {
            return ((this.HasPerson() && (this.Fund >= this.SpyArchitectureFund)) && (this.GetSpyArchitectureArea().Count > 0));
        }


        public void SpyPacksDayEvent()
        {
            this.TodayNewMilitarySpyMessage = null;
            this.TodayNewTroopSpyMessage = null;
            for (int i = this.SpyPacks.Count - 1; i >= 0; i--)
            {
                SpyPack local1 = this.SpyPacks[i];
                local1.Days--;
                if ((this.SpyPacks[i].Days <= 0) || ((this.SpyPacks[i].SpyPerson != null) && (this.SpyPacks[i].SpyPerson.BelongedFaction == this.BelongedFaction)))
                {
                    this.SpyPacks.RemoveAt(i);
                }
            }
        }
        */

        public bool StateAdminEffectAvail()
        {
            return (_architectureKind != null && _architectureKind.HasTechnology && (this.Technology >= ((int)(this.TechnologyCeiling * 0.5))));
        }

        private void StopAllWork()
        {
            foreach (Person person in this.Persons)
            {
                person.WorkKind = ArchitectureWorkKind.无;
            }
        }

        private void StopCostFundWork()
        {
            foreach (Person person in this.Persons)
            {
                if ((person.WorkKind != ArchitectureWorkKind.无) && (person.WorkKind != ArchitectureWorkKind.训练))
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                }
            }
        }

        private void StrategicCenterEffect()
        {
            if ((this.BelongedFaction != null) && this.IsStrategicCenter)
            {
                foreach (Point point in this.LongViewArea.Area)
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(point);
                    if (troopByPosition != null)
                    {
                        if (this.IsFriendlyWithoutTruce(troopByPosition.BelongedFaction))
                        {
                            troopByPosition.IncreaseCombativity(5);
                        }
                        else
                        {
                            troopByPosition.DecreaseCombativity(10);
                        }
                    }
                }
                //if (Session.Current.Scenario.Date.Day == 30)
                if (GameObject.Random(30 / Session.Parameters.DayInTurn) == 0)
                {
                    GameObjectList aILinks = this.GetAILinks();
                    aILinks.Add(this);
                    foreach (Architecture architecture in aILinks)
                    {
                        if (this.IsFriendlyWithoutTruce(architecture.BelongedFaction))
                        {
                            if (architecture.Kind.HasDomination)
                            {
                                int number = architecture.IncreaseDomination(20);
                                if (number > 0)
                                {
                                    architecture.IncrementNumberList.AddNumber(number, CombatNumberKind.士气, architecture.Position);
                                }
                            }
                        }
                        else if (this.IsHostile(architecture.BelongedFaction) && architecture.Kind.HasDomination)
                        {
                            int num2 = architecture.DecreaseDomination(10);
                            if (num2 > 0)
                            {
                                architecture.DecrementNumberList.AddNumber(num2, CombatNumberKind.士气, architecture.Position);
                            }
                        }
                    }
                }
            }
        }

        private int TargetingTroopCount(Architecture a)
        {
            int num = 0;
            foreach (Troop troop in this.BelongedFaction.Troops)
            {
                if (troop.WillArchitecture == a)
                {
                    num++;
                }
            }
            return num;
        }

        public bool TechnologyAvail()
        {
            return (_architectureKind.HasTechnology && this.HasPerson());
        }

        public override string ToString()
        {
            return string.Concat(new object[] { base.Name, "  ", this.KindString, "  ", this.FactionString, "  ", this.Persons.Count, "人" });
        }

        public bool TrainingAvail()
        {
            if (this.HasPerson())
            {
                foreach (Military military in this.Militaries)
                {
                    if ((military.Quantity > 0) && ((military.Morale < military.MoraleCeiling) || (military.Combativity < military.CombativityCeiling)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }



        private void TrainMilitary()  //训练编队
        {
            int meiXunlianHaoDeBianduiShu;
            meiXunlianHaoDeBianduiShu = this.MeiXunlianHaoDeBianduiShu();
            if (meiXunlianHaoDeBianduiShu == 0)
            {
                return;
            }
            int zongXunlianNengli = 0;
            int pingjunXunlianNengli;
            foreach (Person person in this.TrainingWorkingPersons)
            {
                zongXunlianNengli += person.TrainingAbility;
            }
            pingjunXunlianNengli = zongXunlianNengli / meiXunlianHaoDeBianduiShu;

            if (pingjunXunlianNengli > 0)
            {
                int pingjunJinyan = 0;
                foreach (Military military in this.Militaries)
                {
                    if (military.Morale < military.MoraleCeiling)
                    {
                        int randomValue = StaticMethods.GetRandomValue((int)((pingjunXunlianNengli * this.MultipleOfTraining) * Session.Parameters.TrainingRate), 200 + (10 * (military.Scales + military.InjuryQuantity / military.Kind.MinScale)));
                        if (randomValue > 0)
                        {
                            if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                            {
                                randomValue = (int)(randomValue * Session.Parameters.AITrainingSpeedRate);
                            }
                            pingjunJinyan = randomValue / this.TrainingWorkingPersons.Count;
                            List<Person> needRemoval = new List<Person>();
                            foreach (Person person in this.TrainingWorkingPersons)
                            {
                                if (person.BelongedFaction == null)
                                {
                                    needRemoval.Add(person);
                                }
                                else
                                {
                                    //person.AddTrainingExperience(pingjunJinyan * 2);
                                    person.AddCommandExperience(pingjunJinyan * 2);
                                    person.IncreaseReputation(pingjunJinyan * 3);
                                    person.IncreaseOfficerMerit(pingjunJinyan * 3);
                                    person.BelongedFaction.IncreaseReputation(pingjunJinyan * 2);
                                    person.BelongedFaction.IncreaseTechniquePoint(pingjunJinyan * 50);
                                }
                            }
                            foreach (Person p in needRemoval)
                            {
                                this.TrainingWorkingPersons.Remove(p);
                            }
                            military.IncreaseMorale(randomValue);
                        }
                    }
                    if (military != null && military.Combativity < military.CombativityCeiling && this.TrainingWorkingPersons.Count > 0)
                    {
                        int increment = StaticMethods.GetRandomValue((int)((pingjunXunlianNengli * this.MultipleOfTraining) * Session.Parameters.TrainingRate), 50 + (5 * (military.Scales + military.InjuryQuantity / military.Kind.MinScale)));
                        if (increment > 0)
                        {
                            if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                            {
                                increment = (int)(increment * Session.Parameters.AITrainingSpeedRate);
                            }
                            List<Person> needRemoval = new List<Person>();
                            pingjunJinyan = increment / this.TrainingWorkingPersons.Count;
                            foreach (Person person in this.TrainingWorkingPersons)
                            {
                                if (person.BelongedFaction == null)
                                {
                                    needRemoval.Add(person);
                                }
                                else
                                {
                                    //person.AddTrainingExperience(pingjunJinyan);
                                    person.AddStrengthExperience(pingjunJinyan);
                                    person.IncreaseReputation(pingjunJinyan);
                                    person.IncreaseOfficerMerit(pingjunJinyan);
                                    person.BelongedFaction.IncreaseReputation(0);
                                    person.BelongedFaction.IncreaseTechniquePoint(pingjunJinyan * 20);
                                }
                            }
                            foreach (Person p in needRemoval)
                            {
                                this.TrainingWorkingPersons.Remove(p);
                            }
                            military.IncreaseCombativity(increment);
                        }
                    }

                }

            }
        }


        public bool TransferFoodAvail()
        {
            if (this.IsSurrounded()) return false;
            return ((this.Fund > 0) && (this.GetOtherArchitectureList().Count > 0));
        }

        public bool TransferFundAvail()
        {
            if (this.IsSurrounded()) return false;
            return ((this.Fund > 0) && (this.GetOtherArchitectureList().Count > 0));
        }

        public bool TroopershipAvail()
        {
            if ((((Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(0x1c) != null)
                && (this.PersonsExcludeNvGuan.Count > 0)) && (this.Militaries.Count > 0)) && this.IsBesideWater && !Session.GlobalVariables.LandArmyCanGoDownWater)
            {
                foreach (Military military in this.Militaries)
                {
                    if ((((military.Quantity > 0) && (military.Morale > 0)) && (military.Kind.Type != MilitaryType.水军)) && (this.GetMilitaryCampaignArea(military).Count > 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void ViewAreaEvent()
        {
            this.DetectAmbushTroop();
            this.IncreaseViewAreaCombativity();
        }

        public bool ViewTroop(Troop troop)
        {
            return (this.LongViewArea.HasPoint(troop.Position) && (((this.BelongedFaction != null) && this.IsFriendly(troop.BelongedFaction)) || (troop.Status != TroopStatus.埋伏)));
        }

        public void WallStateChange()
        {
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                if (!this.IsFriendly(troop.BelongedFaction))
                {
                    troop.RefreshViewArchitectureRelatedArea();
                }
            }
        }

        private void YearEnd()
        {
        }

        public void YearEvent()
        {
            this.YearEnd();
        }

        public PersonList makeMarryablePersons()
        {
            PersonList result = new PersonList();

            if (this.Fund < Session.Parameters.MakeMarriageCost) return result;

            foreach (Person p in this.Persons)
            {
                if (p.MakeMarryable(true).Count > 0)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        public PersonList MakeMarryablePersons2()//可以纳妾的男武将列表
        {
            PersonList result = new PersonList();

            if (this.Fund < Session.Parameters.MakeMarriageCost) return result;

            foreach (Person p in this.Persons)
            {
                if (!p.Sex && p.MakeMarryable2(true).Count > 0)
                {
                    result.Add(p);
                }
            }
            return result;
        }
        public int AbundantFood
        {
            get
            {
                int num = 0;
                Legion defensiveLegion = this.ResolveDefensiveLegion();
                foreach (Legion legion in this.BelongedFaction.Legions)
                {
                    if (legion.IsDefensive())
                    {
                        if (legion == defensiveLegion)
                        {
                            num += legion.FoodCostPerDay * 80;
                        }
                    }
                    else if (legion.IsOffensive() && (legion.PreferredRouteway != null) && (legion.PreferredRouteway.StartArchitecture == this))
                    {
                        num += legion.FoodCostPerDay * 80;
                    }
                }
                int factor = 80;
                if (HasHostileTroopsInView())
                {
                    factor = 180;
                }
                int num2 = (((int)(Math.Sqrt((double)this.Population) * 400.0)) + (this.FoodCostPerDayOfAllMilitaries * factor)) + num;
                if (!this.HostileLine)
                {
                    num2 /= 2;
                }
                if (!this.withoutTruceFrontline)
                {
                    num2 /= 2;
                }
                return num2;
            }
        }

        public int AbundantFund
        {
            get
            {
                int num = this.FacilityMaintenanceCost * 60;
                num += this.RoutewayActiveCost * 60;
                num += this.PersonCount * Session.Parameters.InternalFundCost * 30;
                num += (this.BelongedFaction.BecomeEmperorLegallyAvail() || this.BelongedFaction.SelfBecomeEmperorAvail()) && this.BelongedFaction.Capital == this ? 100000 : 0;
                num += this.BelongedFaction.Leader.WaitForFeiZi != null ? Session.Parameters.NafeiCost : 0;
                num += this.ExpectedSalary;
                num += (int)(Math.Sqrt(this.Population) * 8.0);
                if (this.withoutTruceFrontline)
                {
                    num += this.Population / 50;
                }
                num += this.BelongedFaction.Capital == this ? this.BelongedFaction.FundToAdvance : 0;
                num += this.InformationDayCost * 15;
                num += this.PlanFacilityKind == null ? 0 : this.PlanFacilityKind.FundCost;
                num += this.BelongedFaction != null && this.BelongedFaction.PlanTechniqueArchitecture == this ? this.BelongedFaction.getTechniqueActualFundCost(this.BelongedFaction.PlanTechnique) : 0;
                return num;
            }
        }
        [DataMember]
        public int Agriculture
        {
            get
            {
                return this.agriculture;
            }
            set
            {
                this.agriculture = value;
            }
        }

        /// <summary>
        /// 农业最大值
        /// </summary>
        public int AgricultureCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.AgricultureCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasAgriculture ? ((_architectureKind.AgricultureBase + (_architectureKind.AgricultureUnit * (this.JianzhuGuimo - 1))) + this.IncrementOfAgricultureCeiling) : 0;
            }
        }

        public string AgricultureString
        {
            get
            {
                return (this.Agriculture + "/" + this.AgricultureCeiling);
            }
        }

        public string AILandLinksDisplayString
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (Architecture architecture in this.AILandLinks)
                {
                    builder.Append(architecture.Name + " ");
                }
                return builder.ToString();
            }
        }

        public string AIWaterLinksDisplayString
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (Architecture architecture in this.AIWaterLinks)
                {
                    builder.Append(architecture.Name + " ");
                }
                return builder.ToString();
            }
        }

        public int AreaCount
        {
            get
            {
                //return this.ArchitectureArea.Count;
                return 1;

            }
        }

        public int JianzhuGuimo
        {
            get
            {
                return this.ArchitectureArea.Count;


            }
        }


        public int ArmyQuantity
        {
            get
            {
                int num = 0;
                foreach (Military military in this.Militaries)
                {
                    num += military.Quantity;
                }
                return num;
            }
        }

        public int ArmyScale
        {
            get
            {
                if (this.BelongedFaction != null)
                {
                    int num = 0;
                    foreach (Military military in this.Militaries)
                    {
                        num += military.Scales;
                    }
                    return num;
                }
                return ((this.AreaCount * 5) + ((this.Population / 0x2710) / 2));
            }
        }

        public int InverseArmyScaleWeighing
        {
            get
            {
                return (int)((10000.0 / this.ArmyScale) * (((((((this.IsCapital ? 2 : 1) + (this.IsStateAdmin ? 1 : 0)) + (this.IsRegionCore ? 1 : 0)) + (this.IsStrategicCenter ? 1 : 0)) + (this.withoutTruceFrontline ? 2 : 0)) + (this.HostileLine ? 2 : 0)) + (this.CriticalHostile ? 3 : 0)));
            }
        }

        public int ArmyScaleWeighing
        {
            get
            {
                return ((this.ArmyScale + 10) * (((((((this.IsCapital ? 2 : 1) + (this.IsStateAdmin ? 1 : 0)) + (this.IsRegionCore ? 1 : 0)) + (this.IsStrategicCenter ? 1 : 0)) + (this.withoutTruceFrontline ? 2 : 0)) + (this.HostileLine ? 2 : 0)) + (this.CriticalHostile ? 3 : 0)));
            }
        }
        [DataMember]
        public bool AutoHiring
        {
            get
            {
                return this.autoHiring;
            }
            set
            {
                this.autoHiring = value;
            }
        }
        [DataMember]
        public bool AutoRewarding
        {
            get
            {
                return this.autoRewarding;
            }
            set
            {
                this.autoRewarding = value;
            }
        }
        [DataMember]
        public bool AutoSearching
        {
            get
            {
                return this.autoSearching;
            }
            set
            {
                this.autoSearching = value;
            }
        }
        [DataMember]
        public bool AutoZhaoXian
        {
            get
            {
                return this.autoZhaoXian;
            }
            set
            {
                this.autoZhaoXian = value;
            }
        }
        [DataMember]
        public bool AutoWorking
        {
            get
            {
                return this.autoWorking;
            }
            set
            {
                this.autoWorking = value;
            }
        }
        [DataMember]
        public bool AutoRecruiting
        {
            get
            {
                return this.autoRecruiting;
            }
            set
            {
                this.autoRecruiting = value;
            }
        }

        public GameArea BaseFoodSurplyArea
        {
            get
            {
                if (this.baseFoodSurplyArea == null)
                {
                    this.baseFoodSurplyArea = this.LongViewArea;
                    /*this.baseFoodSurplyArea = new GameArea();
                    foreach (Point point in this.ArchitectureArea.GetContactArea(true).Area)
                    {
                        this.baseFoodSurplyArea.AddPoint(point);
                    }
                    foreach (Point point in this.ArchitectureArea.Area)
                    {
                        this.baseFoodSurplyArea.AddPoint(point);
                    }
                    return this.baseFoodSurplyArea;*/
                }
                return this.baseFoodSurplyArea;
            }
            set
            {
                this.baseFoodSurplyArea = value;
            }

        }
        [DataMember]
        public int BuildingDaysLeft
        {
            get
            {
                return this.buildingDaysLeft;
            }
            set
            {
                this.buildingDaysLeft = value;
            }
        }

        public int BuildingDaysLeftText
        {
            get
            {
                return this.buildingDaysLeft * Session.Parameters.DayInTurn;
            }
        }

        // 🔥 线程本地变量：标记当前是否在 AI 上下文中
        [ThreadStatic]
        private static bool _isInAIContext = false;

        private int _buildingFacility = -1;

        [DataMember]
        public int BuildingFacility
        {
            get => _buildingFacility;
            set
            {
                // 🔥 修复：使用线程本地变量标记，避免使用反射和 StackTrace
                
                // 如果是清除建造状态（设置为 -1），总是允许
                if (value == -1)
                {
                    _buildingFacility = value;
                    return;
                }
                
                // 只在 AI 上下文中检查（通过 BeginToBuildAFacility 调用时）
                if (_isInAIContext)
                {
                    // 1. 如果建筑没有势力（中立建筑），不允许 AI 建造
                    if (BelongedFaction == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuildingFacility.set] ⚠️ AI尝试为中立建筑 {this.Name} 建造设施 ID={value}，已拦截");
                        return;
                    }
                    
                    // 2. 如果建筑属于玩家势力，检查是否是委任军区
                    if (Session.Current?.Scenario?.IsPlayer(BelongedFaction) == true)
                    {
                        // 如果不是委任军区，不允许 AI 建造
                        if (BelongedSection == null || 
                            BelongedSection.AIDetail == null || 
                            !BelongedSection.AIDetail.AutoRun)
                        {
                            var facilityName = Session.Current?.Scenario?.GameCommonData?.AllFacilityKinds?.GetFacilityKind(value)?.Name ?? $"ID={value}";
                            System.Diagnostics.Debug.WriteLine($"[BuildingFacility.set] ⚠️⚠️⚠️ AI尝试为玩家建筑 {this.Name} 建造设施 {facilityName}，已拦截！");
                            return;
                        }
                    }
                }
                
                // 允许设置（包括：玩家手动建造、存档加载、委任军区AI建造、AI势力建造）
                _buildingFacility = value;
            }
        }

        public int CaptiveCount
        {
            get
            {
                return this.Captives.Count;
            }
        }

        public int ChangeCapitalCost
        {
            get
            {
                return (Session.Parameters.ChangeCapitalCost * this.AreaCount);
            }
        }

        [DataMember]
        public int Commerce
        {
            get
            {
                return this.commerce;
            }
            set
            {
                this.commerce = value;
            }
        }

        /// <summary>
        /// 商业最大值
        /// </summary>
        public int CommerceCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.CommerceCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasCommerce ? ((_architectureKind.CommerceBase + (_architectureKind.CommerceUnit * (this.JianzhuGuimo - 1))) + this.IncrementOfCommerceCeiling) : 0;
            }
        }

        public string CommerceString
        {
            get
            {
                return (this.Commerce + "/" + this.CommerceCeiling);
            }
        }

        public GameArea ContactArea
        {
            get
            {
                if (this.contactArea == null)
                {
                    this.contactArea = this.ArchitectureArea.GetContactArea(false);
                }
                return this.contactArea;
            }
            set
            {
                this.contactArea = value;
            }
        }

        public int ConvincePersonFund
        {
            get
            {
                return (int)(Session.Parameters.ConvincePersonCost * this.RateOfConvincePerson);
            }
        }

        public int ConvincePersonMaxCount
        {
            get
            {
                if (this.ConvincePersonFund == 0) return int.MaxValue;
                return (this.Fund / this.ConvincePersonFund);
            }
        }

        public float CurrentRateOfInternal
        {
            get
            {
                //县令内政加成系数
                float mayorRate = 0;
                float leaderRate = 0;
                if (this.Mayor != null && this.Mayor.Status != PersonStatus.Captive)
                {
                    mayorRate = ((float)this.Mayor.Politics / 100 + (float)this.Mayor.Intelligence / 100) / 2 * Math.Min(1, this.MayorOnDutyDays / 90.0f);
                }
                if (this.BelongedFaction != null && this.BelongedFaction.Leader != null && this.BelongedFaction.Leader.Status != PersonStatus.Captive)
                {
                    leaderRate = ((float)this.BelongedFaction.Leader.Politics / 100 + (float)this.BelongedFaction.Leader.Intelligence / 100) / 2;
                    leaderRate *= 0.2f;
                }

                return (this.RateOfPublic + this.DayRateIncrementOfInternal + mayorRate);
            }
        }

        public float CurrentSurplusRate
        {
            get
            {
                return this.surplusRate;
            }
        }

        public int DestroyArchitectureFund
        {
            get
            {
                return (int)(Session.Parameters.DestroyArchitectureCost * this.RateOfDestroyArchitecture);
            }
        }

        public int DestroyPersonMaxCount
        {
            get
            {
                if (this.DestroyArchitectureFund == 0) return int.MaxValue;
                return (this.Fund / this.DestroyArchitectureFund);
            }
        }
        [DataMember]
        public int Domination
        {
            get
            {
               return this.domination;

            }
            set
            {
                this.domination = value;
            }
        }

        /// <summary>
        /// 统治最大值
        /// </summary>
        public int DominationCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.DominationCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasDomination ? (_architectureKind.DominationBase + (_architectureKind.DominationUnit * (this.JianzhuGuimo - 1)) + this.IncrementOfDominationCeiling) : 0;
            }
        }

        public string DominationString
        {
            get
            {
                return (this.Domination + "/" + this.DominationCeiling);
            }
        }
        [DataMember]
        public int Endurance
        {
            get
            {
                return this.endurance;
            }
            set
            {
                this.endurance = value;
            }
        }

        /// <summary>
        /// 耐力最大值
        /// </summary>
        public int EnduranceCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.EnduranceCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasEndurance ? ((_architectureKind.EnduranceBase + (_architectureKind.EnduranceUnit * (this.JianzhuGuimo - 1))) + this.IncrementOfEnduranceCeiling) : 0;
            }
        }

        public string EnduranceString
        {
            get
            {
                return (this.Endurance + "/" + this.EnduranceCeiling);
            }
        }

        public int EnoughFood
        {
            get
            {
                int num = 0;
                Legion defensiveLegion = this.ResolveDefensiveLegion();
                foreach (Legion legion in this.BelongedFaction.Legions)
                {
                    if (legion.IsDefensive())
                    {
                        if (legion == defensiveLegion)
                        {
                            num += legion.FoodCostPerDay * 30;
                        }
                    }
                    else if (legion.IsOffensive() && (legion.PreferredRouteway != null) && (legion.PreferredRouteway.StartArchitecture == this))
                    {
                        num += legion.FoodCostPerDay * 30;
                    }
                }
                int factor = 30;
                if (HasHostileTroopsInView())
                {
                    factor = 180;
                }

                int num2 = this.FoodCostPerDayOfAllMilitaries * factor + num;

                // 🔥 BUG修复：限制EnoughFood不超过粮仓上限的90%，防止巨额物资转运
                int maxEnoughFood = (int)(this.FoodCeiling * 0.9);
                if (num2 > maxEnoughFood)
                {
                    num2 = maxEnoughFood;
                }

                return num2;
            }
        }

        public int EnoughFund
        {
            get
            {
                int num = this.FacilityMaintenanceCost * 30;
                num += this.RoutewayActiveCost * 30;
                num += this.InformationDayCost * 15;
                num += this.PlanFacilityKind == null ? 0 : this.PlanFacilityKind.FundCost;
                num += this.BelongedFaction != null && this.BelongedFaction.PlanTechniqueArchitecture == this ? this.BelongedFaction.getTechniqueActualFundCost(this.BelongedFaction.PlanTechnique) : 0;
                num += this.PersonCount * this.InternalFundCost * 30;
                num += this.ExpectedSalary;
                return num;
            }
        }

        public bool IsFoodIncomeEnough
        {
            get
            {
                return this.ExpectedFood * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.Season) - this.EnoughFood / 8 >= 0;
            }
        }

        public bool IsFundIncomeEnough
        {
            get
            {
                return this.ExpectedFund - this.EnoughFund >= 0;
            }
        }

        private int ExpectedFoodCache = -1;
        public int ExpectedFood
        {
            get
            {
                if (ExpectedFoodCache > 0)
                {
                    return ExpectedFoodCache;
                }
                // 🔥 安全检查：防止异常大的人口和农业值导致计算溢出
                double safePopulation = Math.Min(this.Population, 1000000);  // 限制最大人口
                double safeAgriculture = Math.Min(this.Agriculture, 100000);  // 限制最大农业
                
                int num = this.Agriculture + ((int)((Math.Pow(safePopulation, 0.3) * Math.Pow(safeAgriculture, 0.8)) * 47.0));
                num += this.IncrementOfMonthFood;
                num += (int)(this.RateIncrementOfMonthFood * num);
                num = (int)(num * Session.Current.Scenario.Date.GetFoodRateBySeason(Session.Current.Scenario.Date.Season));
                if (this.LocationState?.StateAdmin != null && this.LocationState.StateAdmin.StateAdminEffectAvail())
                {
                    if (this.IsFriendlyWithoutTruce(this.LocationState.StateAdmin.BelongedFaction))
                    {
                        num += (int)(num * 0.2);
                    }
                    else if (this.IsHostile(this.LocationState.StateAdmin.BelongedFaction))
                    {
                        num -= (int)(num * 0.2);
                    }
                }

                if (this.BelongedFaction != null)
                {
                    num = (int)(num * this.BelongedFaction.InternalSurplusRate);
                }
                num = (int)(num * Session.Parameters.FoodRate);
                if (Session.Current.Scenario.HasAIResourceBonus(this.BelongedSection))
                {
                    num = (int)(num * Session.Parameters.AIFoodRate);
                }
                if (Session.GlobalVariables.MultipleResource)
                {
                    num *= 2;
                }
                // 🔥 安全检查：防止计算结果异常大
                if (num > 10000000)  // 如果超过1000万，限制为合理值
                {
                    num = Math.Max(this.Agriculture * 10, 50000);  // 基于农业的合理值
                    System.Diagnostics.Debug.WriteLine($"[ExpectedFood] 检测到异常大的粮食收入，已限制为: {num}");
                }
                
                num += 10000;
                ExpectedFoodCache = num;
                return num;
            }
        }

        public string ExpectedFoodString
        {
            get
            {
                return (this.ExpectedFood + "/月");
            }
        }

        private int ExpectedFundCache = -1;
        public int ExpectedFund
        {
            get
            {
                if (ExpectedFundCache > 0)
                {
                    return ExpectedFundCache;
                }
                // 🔥 安全检查：防止异常大的人口和商业值导致计算溢出
                double safePopulation = Math.Min(this.Population, 1000000);  // 限制最大人口
                double safeCommerce = Math.Min(this.Commerce, 100000);  // 限制最大商业
                
                int num = this.Commerce + ((int)((Math.Pow(safePopulation, 0.6) * Math.Pow(safeCommerce, 0.8)) / 49.0));
                num += this.IncrementOfMonthFund;
                num += (int)(this.RateIncrementOfMonthFund * num);
                if (this.LocationState?.StateAdmin != null && this.LocationState.StateAdmin.StateAdminEffectAvail())
                {
                    if (this.IsFriendlyWithoutTruce(this.LocationState.StateAdmin.BelongedFaction))
                    {
                        num += (int)(num * 0.2);
                    }
                    else if (this.IsHostile(this.LocationState.StateAdmin.BelongedFaction))
                    {
                        num -= (int)(num * 0.2);
                    }
                }
                if (this.BelongedFaction != null)
                {
                    num = (int)(num * this.BelongedFaction.InternalSurplusRate);
                }
                num = (int)(num * Session.Parameters.FundRate);
                if (Session.Current.Scenario.HasAIResourceBonus(this.BelongedSection))
                {
                    num = (int)(num * Session.Parameters.AIFundRate);
                }
                if (Session.GlobalVariables.MultipleResource)
                {
                    num *= 2;
                }
                // 🔥 安全检查：防止计算结果异常大
                if (num > 10000000)  // 如果超过1000万，限制为合理值
                {
                    num = Math.Max(this.Commerce * 10, 50000);  // 基于商业的合理值
                    System.Diagnostics.Debug.WriteLine($"[ExpectedFund] 检测到异常大的资金收入，已限制为: {num}");
                }
                
                num += 100;
                ExpectedFundCache = num;
                return num;
            }
        }

        public string ExpectedFundString
        {
            get
            {
                return (this.ExpectedFund + "/月");
            }
        }

        public int FacilityCount
        {
            get
            {
                return this.Facilities.Count;
            }
        }
        [DataMember]
        public bool FacilityEnabled
        {
            get
            {
                return this.facilityEnabled;
            }
            set
            {
                this.facilityEnabled = value;
            }
        }

        public string FacilityEnabledString
        {
            get
            {
                return (this.FacilityEnabled ? "○" : "×");
            }
        }

        public int FacilityMaintenanceCost
        {
            get
            {
                int num = 0;
                
                foreach (Facility facility in this.Facilities)
                {
                    num += facility.MaintenanceCost;
                }

                if (this.Feiziliebiao.Count > 0 && (this.BelongedFaction == null || !this.BelongedFaction.IsAlien))
                {
                    int princessCost = this.Feiziliebiao.Count * Session.Parameters.PrincessMaintainenceCost;
                    num += princessCost;
                }
                
                return num;
            }
        }

        public string FacilityMaintenanceCostString
        {
            get
            {
                return (this.FacilityMaintenanceCost * 30 + "/月");
            }
        }

        public int FacilityPositionCount
        {
            get
            {
                int result = (_architectureKind.FacilityPositionUnit * (this.JianzhuGuimo + this.IncrementOfFacilityPositionCount));
                

                
                return result;
            }
        }

        public int FacilityPositionLeft
        {
            get
            {
                int facilityPositionCount = this.FacilityPositionCount;
                foreach (Facility facility in this.Facilities)
                {
                    facilityPositionCount -= facility.PositionOccupied;
                }
                if (this.BuildingFacility >= 0)
                {
                    FacilityKind facilityKind = Session.Current.Scenario.GameCommonData.AllFacilityKinds.GetFacilityKind(this.BuildingFacility);
                    if (facilityKind != null)
                    {
                        facilityPositionCount -= facilityKind.PositionOccupied;
                    }
                }
                return facilityPositionCount;
            }
        }

        public string FacilityPositionString
        {
            get
            {
                return ((this.FacilityPositionCount - this.FacilityPositionLeft) + "/" + this.FacilityPositionCount);
            }
        }

        public bool FactionAutoRefuse
        {
            get
            {
                return ((this.BelongedFaction != null) && this.BelongedFaction.AutoRefuse);
            }
        }

        public string FactionInternalSurplusRatePercentString
        {
            get
            {
                if (this.BelongedFaction != null)
                {
                    return StaticMethods.GetPercentString(this.BelongedFaction.InternalSurplusRate, 3);
                }
                return "----";
            }
        }

        public string FactionString
        {
            get
            {
                if (this.BelongedFaction != null)
                {
                    return this.BelongedFaction.Name;
                }
                return "----";
            }
        }

        public int FewArmyScale
        {
            get
            {
                return (10 + (2 * this.AreaCount));
            }
        }
        [DataMember]
        public int Food
        {
            get
            {
                return this.food;
            }
            set
            {
                if (this.food - value > 50000)
                {
                    System.Diagnostics.Debug.WriteLine($"[LARGE FOOD DEDUCTION] Architecture {this.Name} (ID:{this.ID}) Food decreased by {this.food - value} (From {this.food} To {value}). Stack: {new System.Diagnostics.StackTrace()}");
                }
                this.food = value;
            }
        }

        /// <summary>
        /// 粮草最大值
        /// </summary>
        public int FoodCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.FoodCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return (_architectureKind.FoodMaxUnit * this.JianzhuGuimo) + this.IncrementOfFoodCeiling;
            }
        }

        public int FoodCostPerDayOfAllMilitaries
        {
            get
            {
                int num = 0;
                foreach (Military military in this.Militaries)
                {
                    num += military.FoodCostPerDay;
                }
                return num;
            }
        }

        public int MaxSupportableTroopScale
        {
            get
            {
                if (Session.GlobalVariables.PopulationRecruitmentLimit)
                {
                    MilitaryKind b = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0];
                    return this.Population / (b.MaxScale / b.MinScale);
                }
                int cost = this.FoodCostPerDayOfAllMilitaries * 60;
                if (cost < this.FoodCeiling * 0.9)
                {
                    return int.MaxValue;
                }
                return (int)((this.FoodCeiling / (double)cost) * this.ArmyScale);
            }
        }

        public MilitaryList TransferingMilitaries
        {
            get
            {
                MilitaryList result = new MilitaryList();
                if (this.BelongedFaction == null) return result;
                foreach (Military m in this.BelongedFaction.TransferingMilitaries)
                {
                    if (m.TargetArchitecture == this)
                    {
                        result.Add(m);
                    }
                }
                return result;
            }
        }

        public int TransferingMilitariesScale
        {
            get
            {
                int s = 0;
                foreach (Military m in this.TransferingMilitaries)
                {
                    s += m.Scales;
                }
                return s;
            }
        }

        public bool IsTroopExceedsLimit
        {
            get
            {
                if (Session.GlobalVariables.PopulationRecruitmentLimit)
                {
                    return this.Population < this.ArmyQuantity + this.TransferingMilitariesScale;
                }
                return false;
            }
        }

        public int MaxSupportableTroop
        {
            get
            {
                if (Session.GlobalVariables.PopulationRecruitmentLimit)
                {
                    return this.Population / Session.Current.Scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0].MaxScale;
                }
                int cost = this.FoodCostPerDayOfAllMilitaries * 60;
                if (cost < this.FoodCeiling * 0.9)
                {
                    return int.MaxValue;
                }
                return (int)((this.FoodCeiling / (double)cost) * this.Militaries.Count);
            }
        }

        public int FoodCostPerDayOfLandMilitaries
        {
            get
            {
                int num = 0;
                foreach (Military military in this.Militaries)
                {
                    if (military.Kind.Type != MilitaryType.水军)
                    {
                        num += military.FoodCostPerDay;
                    }
                }
                return num;
            }
        }

        public int FoodCostPerDayOfWaterMilitaries
        {
            get
            {
                int num = 0;
                foreach (Military military in this.Militaries)
                {
                    if (military.Kind.Type == MilitaryType.水军)
                    {
                        num += military.FoodCostPerDay;
                    }
                }
                return num;
            }
        }

        public float FoodReduceDayRate
        {
            get
            {
                return (0.001f * this.RateOfFoodReduceRate);
            }
        }

        public string FoodReduceDayRateString
        {
            get
            {
                return (Math.Round((double)this.FoodReduceDayRate, 4).ToString() + "/日");
            }
        }
        [DataMember]
        public int Fund
        {
            get
            {
                return this.fund;
            }
            set
            {

                
                if (this.fund - value > 50000)
                {
                    System.Diagnostics.Debug.WriteLine($"[LARGE FUND DEDUCTION] Architecture {this.Name} (ID:{this.ID}) Fund decreased by {this.fund - value} (From {this.fund} To {value}). Stack: {new System.Diagnostics.StackTrace()}");
                }
                this.fund = value;
            }
        }

        public int FundCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.FundCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return (_architectureKind.FundMaxUnit * this.JianzhuGuimo) + this.IncrementOfFundCeiling;
            }
        }

        public int FundInPack
        {
            get
            {
                int num = 0;
                foreach (FundPack pack in this.FundPacks)
                {
                    num += pack.Fund;
                }
                return num;
            }
        }

        public int FoodInPack
        {
            get
            {
                int num = 0;
                foreach (FoodPack pack in this.FoodPacks)
                {
                    num += pack.Food;
                }
                return num;
            }
        }

        public string FundPackString
        {
            get
            {
                String s = "";
                foreach (FundPack p in this.FundPacks)
                {
                    s += (p.Days * Session.Current.Scenario.Parameters.DayInTurn) + "天" + p.Fund + "。";
                }
                return s;
            }
        }

        public string FoodPackString
        {
            get
            {
                String s = "";
                foreach (FoodPack p in this.FoodPacks)
                {
                    s += (p.Days * Session.Current.Scenario.Parameters.DayInTurn) + "天" + p.Food + "。";
                }
                return s;
            }
        }

        public int GossipArchitectureFund
        {
            get
            {
                return (int)(Session.Parameters.GossipArchitectureCost * this.RateOfGossipArchitecture);
            }
        }

        public int JailBreakArchitectureFund
        {
            get
            {
                return (int)(Session.Parameters.JailBreakArchitectureCost * this.RateOfJailBreakArchitecture);
            }
        }

        public int GossipPersonMaxCount
        {
            get
            {
                if (this.GossipArchitectureFund == 0) return int.MaxValue;
                return (this.Fund / this.GossipArchitectureFund);
            }
        }

        public int JailBreakPersonMaxCount
        {
            get
            {
                if (this.JailBreakArchitectureFund == 0) return int.MaxValue;
                return (this.Fund / this.JailBreakArchitectureFund);
            }
        }

        public bool HasBuildingRouteway
        {
            get
            {
                foreach (Routeway routeway in this.Routeways)
                {
                    if (routeway.Building)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool HasDefensiveLegion
        {
            get
            {
                return this.ResolveDefensiveLegion() != null;
            }
        }

        public bool HasFriendlyDiplomaticRelation
        {
            get
            {
                if (this.BelongedFaction == null)
                {
                    return false;
                }
                return this.BelongedFaction.HasFriendlyDiplomaticRelation;
            }
        }

        private bool HasHirablePerson
        {
            get
            {
                foreach (Person person in this.NoFactionPersons)
                {
                    if (person.IsHirable(this.BelongedFaction))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /*
        public bool HasSpy
        {
            get
            {
                return (this.SpyPacks.Count > 0);
            }
        }
        */

        private int HirablePersonCount
        {
            get
            {
                int num = 0;
                foreach (Person person in this.NoFactionPersons)
                {
                    if (person.IsHirable(this.BelongedFaction))
                    {
                        num++;
                    }
                }
                return num;
            }
        }
        [DataMember]
        public bool HireFinished
        {
            get
            {
                return this.hireFinished;
            }
            set
            {
                this.hireFinished = value;
            }
        }

        public int InstigateArchitectureFund
        {
            get
            {
                return (int)(Session.Parameters.InstigateArchitectureCost * this.RateOfInstigateArchitecture);
            }
        }

        public int InstigatePersonMaxCount
        {
            get
            {
                if (this.InstigateArchitectureFund == 0) return int.MaxValue;
                return (this.Fund / this.InstigateArchitectureFund);
            }
        }

        public int InternalFundCost
        {
            get
            {
                return (Session.Parameters.InternalFundCost * this.AreaCount);
            }
        }

        public bool IsBesideWater
        {
            get
            {
                foreach (Point point in this.ArchitectureArea.GetContactArea(false).Area)
                {
                    if (!Session.Current.Scenario.PositionOutOfRange(point) && (Session.Current.Scenario.ScenarioMap.MapData[point.X, point.Y] == 6))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsCapital
        {
            get
            {
                return ((this.BelongedFaction != null) && (this.BelongedFaction.Capital == this));
            }
        }

        public bool IsFoodAbundant
        {
            get
            {
                return ((this.Food >= this.FoodCeiling) || (this.Food + this.FoodInPack >= this.AbundantFood));
            }
        }

        public bool IsFoodEnough
        {
            get
            {
                return ((this.Food >= this.FoodCeiling) || (this.Food + this.FoodInPack >= this.EnoughFood));
            }
        }

        public bool IsFoodTwiceAbundant
        {
            get
            {
                return ((this.Food >= this.FoodCeiling) || (this.Food + this.FoodInPack > (this.AbundantFood * 2)));
            }
        }

        public bool IsFundAbundant
        {
            get
            {
                return (this.Fund >= this.FundCeiling) || (this.Fund + this.FundInPack >= this.AbundantFund);
            }
        }

        public bool IsFundEnough
        {
            get
            {
                return (this.Fund >= this.FundCeiling) || (this.Fund + this.FundInPack >= this.EnoughFund);
            }
        }

        public bool IsImportant
        {
            get
            {
                return (((this.IsCapital || this.IsStrategicCenter) || this.IsStateAdmin) || this.IsRegionCore || this.huangdisuozai);
            }
        }

        public bool IsRegionCore
        {
            get
            {
                return this.LocationState?.LinkedRegion?.RegionCore == this;
            }
        }

        public bool IsStateAdmin
        {
            get
            {
                return this.LocationState?.StateAdmin == this;
            }
        }
        [DataMember]
        public bool IsStrategicCenter
        {
            get
            {
                return this.isStrategicCenter;
            }
            set
            {
                this.isStrategicCenter = value;
            }
        }

        public string IsStrategicCenterString
        {
            get
            {
                return (this.IsStrategicCenter ? "○" : "×");
            }
        }

        // 🔥 修复：KindId 与 KindID 冲突，使用 JsonIgnore 忽略此属性
        [DataMember]
        [System.Text.Json.Serialization.JsonIgnore]
        public int KindId { get; set; }

        public string KindString
        {
            get
            {
                return _architectureKind.Name;
            }
        }

        public int LandArmyScale
        {
            get
            {
                if (this.BelongedFaction != null)
                {
                    int num = 0;
                    foreach (Military military in this.Militaries)
                    {
                        if (military.Kind.Type != MilitaryType.水军)
                        {
                            num += military.Scales;
                        }
                    }
                    return num;
                }
                return ((this.AreaCount * 5) + ((this.Population / 0x2710) / 2));
            }
        }

        public int LargeArmyScale
        {
            get
            {
                return (40 + (10 * this.AreaCount));
            }
        }

        public GameArea LongViewArea
        {
            get
            {
                // 🔥 防御性检查：Kind未初始化时返回ViewArea
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.LongViewArea] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回 ViewArea");
                    return this.ViewArea;
                }
                
                if (!_architectureKind.HasLongView)
                {
                    return this.ViewArea;
                }
                if (this.longViewArea != null)
                {
                    return this.longViewArea;
                }
                return (this.longViewArea = GameArea.GetAreaFromArea(this.ArchitectureArea, this.LongViewDistance, _architectureKind.HasObliqueView, this.BelongedFaction));
            }
            set
            {
                this.longViewArea = value;
            }
        }

        public int LongViewDistance
        {
            get
            {
                // 🔥 使用 EffectiveViewDistance 而不是 ViewDistance
                // 日期：2026-03-16
                // 原因：整合动态视野系统，远视野也应该基于能量增加
                return (this.EffectiveViewDistance * 2);
            }
        }
        
        // ============================================================
        // 🆕 势力范围系统扩展（2026-03-11）
        // ============================================================
        
        /// <summary>
        /// 🆕 势力范围缓存
        /// </summary>
        private GameArea? _influenceArea = null;
        
        /// <summary>
        /// 🆕 有效视野距离缓存（基础 + 动态）
        /// 🔥 Hot Path：在 ViewArea 计算中使用
        /// 日期：2026-03-16
        /// </summary>
        private int _cachedEffectiveViewDistance = -1;
        private float _appliedInfluenceArchitectureCounterDamageDelta = 0f;
        private float _appliedInfluenceEnduranceDropDelta = 0f;
        private float _appliedInfluenceFoodReduceRateDelta = 0f;
        private int _appliedInfluenceVisionDelta = 0;
        
        /// <summary>
        /// 🆕 势力范围（基于内政数值和地形阻力计算）
        /// 缓存结果，只在内政变化或手动清除时重新计算
        /// 🧊 Cold Path：不在 Update 循环中频繁访问
        /// </summary>
        public GameArea InfluenceArea
        {
            get
            {
                if (_influenceArea != null) return _influenceArea;
                
                // 🔥 数据验证：确保前置条件满足
                if (this.ArchitectureArea?.Area == null || this.ArchitectureArea.Area.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"建筑 {this.Name}(ID:{this.ID}) 的 ArchitectureArea 未初始化，" +
                        "请检查 Architecture.LoadFromString() 或剧本数据");
                }
                
                int energy = CalculateInfluenceEnergy();
                
                _influenceArea = GameArea.GetWeightedFloodFill(
                    this.ArchitectureArea,
                    energy,
                    GetTerrainCostForInfluence
                );
                
                return _influenceArea;
            }
            set => _influenceArea = value;
        }
        
        /// <summary>
        /// 🆕 计算势力范围的能量值
        /// 公式：(规模 + 统治/民心 + 资源) × 天气倍率
        /// 🧊 Cold Path：初始化和缓存失效时调用
        /// </summary>
        /// <summary>
        /// 🆕 计算城池的势力范围能量（含驻军加权）
        /// 日期：2026-03-14
        /// 
        /// 核心逻辑：
        /// - 内政基础：规模、统治、民心、农业、商业
        /// - 驻军威势：总兵力、平均士气（开方压制）
        /// - 人才光环：武将五维（最高值权重 × 2）
        /// - 政魅特化：最高政治 + 最高魅力（行政辐射力）
        /// 
        /// 能量范围：300-650
        /// 
        /// 🧊 Cold Path：回合结束/建筑变化时调用
        /// </summary>
        /// <summary>
        /// 计算城池影响力能量
        /// 日期：2026-03-16
        /// 功能：使用统一的能量计算体系（功过分离机制 + 动态门槛 + 人口权重）
        /// 🧊 Cold Path：回合结束/建筑变化时调用
        /// </summary>
        public int CalculateInfluenceEnergy()
        {
            // 使用统一的能量计算器
            return WorldOfTheThreeKingdoms.GameObjects.EnergyCalculator.CalculateArchitectureInfluenceEnergy(
                scale: this.JianzhuGuimo,
                domination: this.Domination,
                morale: this.Morale,
                agriculture: this.Agriculture,
                commerce: this.Commerce,
                population: this.Population,
                militaries: this.Militaries,
                persons: this.Persons
            );
        }
        
        /// <summary>
        /// 🆕 MOD 扩展点：自定义能量加成
        /// </summary>
        protected virtual double CalculateModInfluenceBonus()
        {
            // MOD 可以重写此方法添加自定义逻辑
            return 0.0;
        }
        
        /// <summary>
        /// 🆕 获取当前天气的能量倍率
        /// </summary>
        private double GetCurrentWeatherEnergyMultiplier()
        {
            var config = WorldOfTheThreeKingdoms.GameData.InfluenceConfig.Current;
            
            // 🚫 ANTI-BAND-AID：不使用 ?? 掩盖 null，而是明确检查
            // 🔥 使用 WeatherManager 获取建筑位置的天气
            var weatherManager = Session.Current.Scenario.WeatherManager;
            if (weatherManager == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Architecture] ⚠️ WeatherManager 未初始化，使用默认天气（晴）");
                return 1.0;
            }
            
            var weather = weatherManager.GetWeatherAt(this.ArchitectureArea.Centre);
            string weatherName = weather.ToString();
            
            return config.GetWeatherEnergyMultiplier(weatherName);
        }
        
        /// <summary>
        /// 🆕 获取地形阻力（用于势力范围计算，考虑天气影响）
        /// 🔥 阶段 1 优化：使用全局缓存，避免重复查询
        /// </summary>
        private static int GetTerrainCostForInfluence(Point position)
        {
            // 🔥 直接从缓存查表（O(1) 性能）
            return WTKGameManager.TerrainCostCache.GetCost(position);
        }
        
        /// <summary>
        /// 🆕 清除势力范围缓存（在内政变化时调用）
        /// </summary>
        public void InvalidateInfluenceCache()
        {
            this._influenceArea = null;
        }

        /// <summary>
        /// 🆕 阶段 3：查询指定点的净能量（使用全局能量地图）
        /// 🔥 Hot Path：O(1) 查询性能，可在 Update/Draw 循环中频繁调用
        /// </summary>
        /// <param name="position">地图坐标</param>
        /// <returns>净能量值（我方能量 - 敌方最大能量）</returns>
        public int GetNetInfluenceAt(Point position)
        {
            // 🔥 ANTI-BAND-AID：明确检查数据源
            if (BelongedFaction == null)
            {
                throw new InvalidOperationException(
                    $"[Architecture] 建筑 {Name} 没有所属势力，无法查询净能量");
            }
            
            if (BelongedFaction.GlobalInfluenceMap == null || BelongedFaction.GlobalInfluenceMap.Length == 0)
            {
                throw new InvalidOperationException(
                    $"[Architecture] 势力 {BelongedFaction.Name} 的 GlobalInfluenceMap 未初始化");
            }
            
            // 🔥 日期：2026-03-16
            // 🔥 重构：使用 EffectiveTotalEnergy
            int index = WTKGameManager.TerrainCostCache.GetIndex(position.X, position.Y);
            int myEnergy = BelongedFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
            int enemyEnergy = 0;
            
            // 🔥 ANTI-BAND-AID：明确检查数据源（不使用 ?.）
            if (Session.Current == null)
            {
                throw new InvalidOperationException(
                    "[Architecture] Session.Current 未初始化");
            }
            
            if (Session.Current.Scenario == null)
            {
                throw new InvalidOperationException(
                    "[Architecture] Scenario 未初始化，无法查询敌对势力");
            }
            
            var scenario = Session.Current.Scenario;
            
            if (scenario.Factions == null)
            {
                throw new InvalidOperationException(
                    "[Architecture] Scenario.Factions 为 null");
            }
            
            // 🔥 Hot Path：直接访问 Factions，避免 GetList() 分配
            var factions = scenario.Factions;
            int factionCount = factions.Count;
            
            // 🔥 使用 for 循环，避免 LINQ
            for (int i = 0; i < factionCount; i++)
            {
                // 🔥 ANTI-BAND-AID：明确检查类型
                var factionObj = factions.GetGameObject(i);
                if (factionObj is not Faction enemyFaction)
                {
                    throw new InvalidOperationException(
                        $"[Architecture] Factions 列表包含无效项（索引 {i}）");
                }
                
                if (enemyFaction == BelongedFaction) continue;
                
                // 检查是否敌对
                if (BelongedFaction.IsHostile(enemyFaction))
                {
                    if (enemyFaction.GlobalInfluenceMap == null || enemyFaction.GlobalInfluenceMap.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"[Architecture] 势力 {enemyFaction.Name} 的 GlobalInfluenceMap 未初始化");
                    }
                    
                    // 🔥 日期：2026-03-16
                    // 🔥 重构：使用 EffectiveTotalEnergy
                    int energy = enemyFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
                    if (energy > enemyEnergy)
                    {
                        enemyEnergy = energy;
                    }
                }
            }
            
            return myEnergy - enemyEnergy;
        }
        
        /// <summary>
        /// 🆕 阶段 3：查询指定点是否在我方势力范围内
        /// 🔥 Hot Path：O(1) 查询性能
        /// </summary>
        /// <param name="position">地图坐标</param>
        /// <returns>true 表示在我方势力范围内（净能量 > 0）</returns>
        public bool IsInMyInfluence(Point position)
        {
            return GetNetInfluenceAt(position) > 0;
        }
        
        /// <summary>
        /// 🆕 阶段 3：获取指定点的我方能量值
        /// 🔥 Hot Path：O(1) 查询性能
        /// </summary>
        /// <param name="position">地图坐标</param>
        /// <returns>我方能量值</returns>
        public int GetMyInfluenceAt(Point position)
        {
            // 🔥 ANTI-BAND-AID：明确检查数据源
            if (BelongedFaction == null)
            {
                throw new InvalidOperationException(
                    $"[Architecture] 建筑 {Name} 没有所属势力");
            }
            
            if (BelongedFaction.GlobalInfluenceMap == null || BelongedFaction.GlobalInfluenceMap.Length == 0)
            {
                throw new InvalidOperationException(
                    $"[Architecture] 势力 {BelongedFaction.Name} 的 GlobalInfluenceMap 未初始化");
            }
            
            // 🔥 日期：2026-03-16
            // 🔥 重构：使用 EffectiveTotalEnergy
            int index = WTKGameManager.TerrainCostCache.GetIndex(position.X, position.Y);
            return BelongedFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
        }

        public int MilitaryCount
        {
            get
            {
                return this.Militaries.Count;
            }
        }

        public int EffectiveMilitaryCount
        {
            get
            {
                int result = 0;
                foreach (Military m in this.Militaries)
                {
                    if (!m.IsTransport)
                    {
                        result++;
                    }
                }
                return result;
            }
        }
        [DataMember]
        public int Morale
        {
            get
            {
                return this.morale;
            }
            set
            {
                if (this.morale == -2147483648)
                {
                    int z = 0;
                    z++;
                }
                this.morale = value;
            }
        }

        /// <summary>
        /// 士气最大值 ?? 民心最大值
        /// </summary>
        public int MoraleCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.MoraleCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasMorale ? (_architectureKind.MoraleBase + (_architectureKind.MoraleUnit * (this.JianzhuGuimo - 1)) + this.IncrementOfMoraleCeiling) : 0;
            }
        }

        public string MoraleString
        {
            get
            {
                return (this.Morale + "/" + this.MoraleCeiling);
            }
        }

        public string FrontLineString
        {
            get
            {
                return this.FrontLine ? "○" : "×";
            }
        }

        public int MovingPersonCount
        {
            get
            {
                return this.MovingPersons.Count;
            }
        }

        public int NoFactionPersonCount
        {
            get
            {
                return this.NoFactionPersons.Count;
            }
        }

        public int NormalArmyScale
        {
            get
            {
                return (20 + (5 * this.AreaCount));
            }
        }

        public ArchitectureList OtherArchitectures
        {
            get
            {
                ArchitectureList list = new ArchitectureList();
                foreach (Architecture architecture in this.BelongedFaction.Architectures)
                {
                    if (architecture != this)
                    {
                        list.Add(architecture);
                    }
                }
                return list;
            }
        }

        public double PDRAgricultureFix
        {
            get
            {
                if (this.Agriculture >= ((int)(this.AgricultureCeiling * 0.6)))
                {
                    return 2E-05;
                }
                if (this.Agriculture < ((int)(this.AgricultureCeiling * 0.3)))
                {
                    return -2E-05;
                }
                return 0.0;
            }
        }

        public double PDRCommerceFix
        {
            get
            {
                if (this.Commerce >= ((int)(this.CommerceCeiling * 0.6)))
                {
                    return 2E-05;
                }
                if (this.Commerce < ((int)(this.CommerceCeiling * 0.3)))
                {
                    return -2E-05;
                }
                return 0.0;
            }
        }

        public double PDRDominationFix
        {
            get
            {
                if (this.Domination >= ((int)(this.DominationCeiling * 0.8)))
                {
                    return 2E-05;
                }
                if (this.Domination < ((int)(this.DominationCeiling * 0.2)))
                {
                    return -0.0001;
                }
                if (this.Domination < ((int)(this.DominationCeiling * 0.5)))
                {
                    return -2E-05;
                }
                return 0.0;
            }
        }

        public double PDRMoraleFix
        {
            get
            {
                if (this.Morale >= ((int)(this.MoraleCeiling * 0.6)))
                {
                    return 2E-05;
                }
                if (this.Morale < ((int)(this.MoraleCeiling * 0.1)))
                {
                    return -0.0001;
                }
                if (this.Morale < ((int)(this.MoraleCeiling * 0.3)))
                {
                    return -2E-05;
                }
                return 0.0;
            }
        }

        public int PersonCount
        {
            get
            {
                return this.Persons.Count;
            }
        }
        [DataMember]
        public int Population
        {
            get
            {
                return this.population;
            }
            set
            {
                this.population = value;
            }
        }

        public int PopulationCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.PopulationCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return (int)((_architectureKind.PopulationBase + (_architectureKind.PopulationUnit * (this.JianzhuGuimo - 1))) * (1 + this.RateIncrementOfPopulationCeiling));
            }
        }

        // 在 Architecture.cs 中
        public bool IsHabitableCity
        {
            get
            {
                if (this.Kind == null) return false;

                // 🟢 强烈建议启用方案 A：最准确，彻底区分关隘和城市
                return this.Kind.HasAgriculture || this.Kind.HasCommerce;

                // 方案 B (备选)：如果 Kind 里没有 HasAgriculture 属性，再用人口判断
                // return this.Kind.PopulationCeiling >= 20000;
            }
        }
        public double PopulationDevelopingRate
        {
            get
            {
                double num = Math.Round((double)(((((Session.Parameters.DefaultPopulationDevelopingRate + this.PDRAgricultureFix) + this.PDRCommerceFix) + this.PDRDominationFix) + this.PDRMoraleFix) + this.RateIncrementOfPopulationDevelop), 6);
                if (!((this.RecentlyHit <= 0) || this.DayAvoidInfluenceByBattle))
                {
                    num += -0.00030000000000000003;
                }

                return num * Session.Parameters.PopulationDevelopingRate;
            }
        }

        public double PopulationDevelopingRateString
        {
            get
            {
                return Math.Round((double)(this.PopulationDevelopingRate / 0.0001), 3);
            }
        }

        public Point Position
        {
            get
            {
                return this.ArchitectureArea.TopLeft;
            }
        }

        public int RecruitmentPopulationBoundary
        {
            get
            {
                // return (_architectureKind.PopulationBoundary * this.AreaCount);
                return (_architectureKind.PopulationBoundary);
            }
        }

        public string RegionEffectString
        {
            get
            {
                if (this.LocationState?.LinkedRegion?.RegionCore != null && this.LocationState.LinkedRegion.RegionCore.RegionCoreEffectAvail())
                {
                    if (this.LocationState.LinkedRegion.RegionCore.IsFriendlyWithoutTruce(this.BelongedFaction))
                    {
                        return "正面";
                    }
                    if (this.LocationState.LinkedRegion.RegionCore.IsHostile(this.BelongedFaction))
                    {
                        return "负面";
                    }
                }
                return "----";
            }
        }

        public string RegionString
        {
            get
            {
                return this.LocationState?.LinkedRegionString ?? "未知地区";
            }
        }

        public int RewardPersonFund
        {
            get
            {
                return (int)(Session.Parameters.RewardPersonCost * this.RateOfRewardPerson);
            }
        }

        public int RewardPersonMaxCount
        {
            get
            {
                if (this.RewardPersonFund == 0) return int.MaxValue;
                return (this.Fund / this.RewardPersonFund);
            }
        }

        public int RoutewayActiveCost
        {
            get
            {
                int num = 0;
                foreach (Routeway routeway in this.Routeways)
                {
                    if (routeway.LastActivePoint != null)
                    {
                        num += routeway.LastActivePoint.ActiveFundCost;
                    }
                }
                return num;
            }
        }

        public string RoutewayActiveCostString
        {
            get
            {
                return (this.RoutewayActiveCost * 30 + "/月");
            }
        }

        public ArchitectureList RoutewayDestinationArchitectureList
        {
            get
            {
                ArchitectureList list = new ArchitectureList();
                foreach (Architecture architecture in this.RoutewayDestinationArchitectures.Values)
                {
                    list.Add(architecture);
                }
                return list;
            }
        }

        public string SectionString
        {
            get
            {
                if (this.BelongedSection != null)
                {
                    return this.BelongedSection.Name;
                }
                return "----";
            }
        }
        public bool ShowNumber
        {
            get
            {
                return this.showNumber;
            }
            set
            {
                this.showNumber = value;
                if (!value && this.IncrementNumberList != null && this.DecrementNumberList != null)
                {
                    this.IncrementNumberList.Clear();
                    this.DecrementNumberList.Clear();
                }
            }
        }

        /*
        public int SpyArchitectureFund
        {
            get
            {
                return (int)(Session.Parameters.SendSpyCost * this.RateOfSpyArchitecture);
            }
        }

        public int SpyPersonMaxCount
        {
            get
            {
                if (this.SpyArchitectureFund == 0) return int.MaxValue;
                return (this.Fund / this.SpyArchitectureFund);
            }
        }
        */

        public string StateEffectString
        {
            get
            {
                if (this.LocationState?.StateAdmin != null && this.LocationState.StateAdmin.StateAdminEffectAvail())
                {
                    if (this.LocationState.StateAdmin.IsFriendlyWithoutTruce(this.BelongedFaction))
                    {
                        return "正面";
                    }
                    if (this.LocationState.StateAdmin.IsHostile(this.BelongedFaction))
                    {
                        return "负面";
                    }
                }
                return "----";
            }
        }

        public string StateString
        {
            get
            {
                return this.LocationState?.Name ?? "未知州域";
            }
        }

        [DataMember]
        public int Technology
        {
            get
            {
                return this.technology;
            }
            set
            {
                this.technology = value;
            }
        }

        /// <summary>
        /// 技术最大值
        /// </summary>
        public int TechnologyCeiling
        {
            get
            {
                if (_architectureKind == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.TechnologyCeiling] ⚠️ 建筑 {this.Name}(ID:{this.ID}) 的 Kind 为 null，返回默认值 0");
                    return 0;
                }
                return _architectureKind.HasTechnology ? ((_architectureKind.TechnologyBase + (_architectureKind.TechnologyUnit * (this.JianzhuGuimo - 1))) + this.IncrementOfTechnologyCeiling) : 0;
            }
        }

        public string TechnologyString
        {
            get
            {
                return (this.Technology + "/" + this.TechnologyCeiling);
            }
        }

        public PlatformTexture Texture
        {
            get
            {
                return _architectureKind.Texture;
            }
        }

        public int TransferFoodArchitectureCount
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.OtherArchitectures)
                {
                    if (architecture.TransferFoodArchitecture == this)
                    {
                        num++;
                    }
                }
                return num;
            }
        }

        public int UnitPopulation
        {
            get
            {
                return (this.Population / this.AreaCount);
            }
        }

        public bool ValueWater
        {
            get
            {
                return (_architectureKind.HasHarbor || this.TroopershipAvailable);
            }
        }

        public int VeryFewArmyScale
        {
            get
            {
                return (5 + this.AreaCount);
            }
        }

        public GameArea ViewArea
        {
            get
            {
                if (viewArea == null)
                {
                    var hasOblique = _architectureKind?.HasObliqueView ?? false;

                    // 🔥 使用 EffectiveViewDistance 而不是 ViewDistance
                    // 日期：2026-03-16
                    // 原因：整合动态视野系统，基于能量增加视野范围
                    viewArea = GameArea.GetAreaFromArea(this.ArchitectureArea, this.EffectiveViewDistance, hasOblique, this.BelongedFaction);
                }

                return viewArea;
            }
            set
            {
                this.viewArea = value;
            }
        }

        public int ViewDistance
        {
            get
            {
                if (_architectureKind == null)
                {
                    return 5 + this.IncrementOfViewRadius; // 默认视野距离
                }
                return _architectureKind.ViewDistance + (this.AreaCount / _architectureKind.ViewDistanceIncrementDivisor) + this.IncrementOfViewRadius;
            }
        }
        
        /// <summary>
        /// 🆕 有效视野距离（基础 + 动态增益）
        /// 🔥 Hot Path：在 ViewArea 计算中使用
        /// 日期：2026-03-16
        /// </summary>
        public int EffectiveViewDistance
        {
            get
            {
                // 🔥 使用缓存，避免重复计算
                if (_cachedEffectiveViewDistance < 0)
                {
                    _cachedEffectiveViewDistance = this.ViewDistance + GetDynamicVisionRange();
                }
                return _cachedEffectiveViewDistance;
            }
        }

        public int WaterArmyScale
        {
            get
            {
                if (this.BelongedFaction != null)
                {
                    int num = 0;
                    foreach (Military military in this.Militaries)
                    {
                        if (military.Kind.Type == MilitaryType.水军 && !military.IsTransport)
                        {
                            num += military.Scales;
                        }
                        else if (this.ValueWater)
                        {
                            num += military.Quantity / 0x7d0;
                        }
                    }
                    return num;
                }
                return ((this.AreaCount * 5) + ((this.Population / 0x2710) / 2));
            }
        }

        private class AILinkProcedureDetail
        {
            public Architecture A;
            public int Level;
            public List<Architecture> Path = new List<Architecture>();

            public AILinkProcedureDetail(int level, Architecture a, List<Architecture> path)
            {
                this.Level = level;
                this.A = a;
                foreach (Architecture architecture in path)
                {
                    this.Path.Add(architecture);
                }
            }
        }
        /*
        public FreeText jianzhubiaoti
        {
            get;
            set;
        }
        */
        //[DataMember]
        public qizi jianzhuqizi
        {
            get;
            set;
        }
        [DataMember]
        public bool youzainan
        {
            get;
            set;
        }

        public string zainanming
        {
            get
            {
                if (this.youzainan)
                {
                    return this.zainan.zainanzhonglei.Name;
                }
                else
                {
                    return "——";
                }
            }
        }

        public string zainanshengyutianshu
        {
            get
            {
                if (this.youzainan)
                {
                    return (this.zainan.shengyutianshu * Session.Parameters.DayInTurn).ToString();
                }
                else
                {
                    return "——";
                }
            }
        }

        public bool kezhenzai()
        {

            if (this.youzainan && this.Fund > 0 && this.Food > 0 && this.HasPerson())
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool kenafei()
        {
            if (this.BelongedFaction != null && !this.BelongedFaction.hougongValid) return false;

            if (this.nvxingwujiang().Count > 0 && (this.Fund >= Session.Parameters.NafeiCost || this.BelongedFaction.IsAlien) &&
                (this.Meinvkongjian > this.Feiziliebiao.Count || this.BelongedFaction.IsAlien)
                && this.Persons.GameObjects.Contains(this.BelongedFaction.Leader))
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        public bool kejinhougong()
        {
            if (this.meifaxianhuaiyundefeiziliebiao().Count != 0 && this.Persons.GameObjects.Contains(this.BelongedFaction.Leader) && !this.BelongedFaction.Leader.huaiyun)
            {

                return true;
            }
            return false;

        }


        public int ExpandFund()
        {
            if (this.JianzhuGuimo == 1)
            {
                return 100000;
            }
            else if (this.JianzhuGuimo == 5)
            {
                return 200000;
            }
            else
            {
                return 0;
            }
        }
        public List<Point> ExpandPoint()
        {
            List<Point> xinjiadedian = new List<Point>();
            if (this.JianzhuGuimo == 1)
            {

                xinjiadedian.Add(new Point(zhongxindian.X - 1, zhongxindian.Y));
                xinjiadedian.Add(new Point(zhongxindian.X + 1, zhongxindian.Y));
                xinjiadedian.Add(new Point(zhongxindian.X, zhongxindian.Y - 1));
                xinjiadedian.Add(new Point(zhongxindian.X, zhongxindian.Y + 1));


            }
            else if (this.JianzhuGuimo == 5)
            {

                xinjiadedian.Add(new Point(zhongxindian.X - 2, zhongxindian.Y));
                xinjiadedian.Add(new Point(zhongxindian.X + 2, zhongxindian.Y));
                xinjiadedian.Add(new Point(zhongxindian.X, zhongxindian.Y - 2));
                xinjiadedian.Add(new Point(zhongxindian.X, zhongxindian.Y + 2));
                xinjiadedian.Add(new Point(zhongxindian.X - 1, zhongxindian.Y - 1));
                xinjiadedian.Add(new Point(zhongxindian.X - 1, zhongxindian.Y + 1));
                xinjiadedian.Add(new Point(zhongxindian.X + 1, zhongxindian.Y - 1));
                xinjiadedian.Add(new Point(zhongxindian.X + 1, zhongxindian.Y + 1));
            }
            else
            {
                return null;
            }
            return xinjiadedian;
        }

        public bool ExpandAvail()
        {
            #if DEBUG
            if (this.Name == "高陵")
            {
                // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] {Name} 检查扩建条件:");
                // System.Diagnostics.Debug.WriteLine($"  资金检查: Fund={Fund}, ExpandFund={ExpandFund()}, 满足={Fund >= ExpandFund()}");
                // System.Diagnostics.Debug.WriteLine($"  规模检查: JianzhuGuimo={JianzhuGuimo}, 满足={JianzhuGuimo == 1 || JianzhuGuimo == 5}");
                // System.Diagnostics.Debug.WriteLine($"  简单图像: UseSimpleArchImages={Session.Current.Scenario.ScenarioMap.UseSimpleArchImages}");
                // System.Diagnostics.Debug.WriteLine($"  可扩建性: Expandable={_architectureKind?.Expandable}, 满足={_architectureKind?.Expandable >= JianzhuGuimo}");
            }
            #endif
            
            if (this.Fund < this.ExpandFund()) return false;
            if (this.JianzhuGuimo != 1 && this.JianzhuGuimo != 5) return false;
            if (Session.Current.Scenario.ScenarioMap.UseSimpleArchImages) return false;

            if (_architectureKind.Expandable < this.JianzhuGuimo) return false;

            TerrainDetail terrainKindByPosition;
            foreach (Point point in this.ExpandPoint())
            {
                if (Session.Current.Scenario.PositionOutOfRange(point))
                {
                    return false;
                }
                terrainKindByPosition = Session.Current.Scenario.GetTerrainDetailByPosition(point);
                if (!terrainKindByPosition.CanExtendInto)
                {
                    return false;
                }
            }
            if (this.HasHostileTroopsInView()) return false;

            #if DEBUG
            // 🔥 诊断：检查 ExpandConditions 是否正确加载
            // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] {Name} 检查 ExpandConditions:");
            // System.Diagnostics.Debug.WriteLine($"  - Session.Parameters == null: {Session.Parameters == null}");
            if (Session.Parameters != null)
            {
                // System.Diagnostics.Debug.WriteLine($"  - ExpandConditions == null: {Session.Parameters.ExpandConditions == null}");
                if (Session.Parameters.ExpandConditions != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"  - ExpandConditions.Count: {Session.Parameters.ExpandConditions.Count}");
                    // System.Diagnostics.Debug.WriteLine($"  - ExpandConditions 内容: {string.Join(", ", Session.Parameters.ExpandConditions)}");
                }
            }
            #endif

            List<Condition> conditions = new List<Condition>();
            for (int i = 0; i < Session.Parameters.ExpandConditions.Count; i++)
            {
                Condition c = Session.Current.Scenario.GameCommonData.AllConditions.GetCondition(Session.Parameters.ExpandConditions[i]);
                conditions.Add(c);
                
                #if DEBUG
                // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] {Name} 条件 {i}: ID={Session.Parameters.ExpandConditions[i]}, Name={c?.Name ?? "null"}");
                #endif
            }
            
            #if DEBUG
            // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] {Name} 开始检查 {conditions.Count} 个条件");
            #endif
            
            if (!Condition.CheckConditionList(conditions, this))
            {
                #if DEBUG
                // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] ❌ {Name} 不满足扩建条件");
                #endif
                return false;
            }

            #if DEBUG
            // 🔍 调试：确认哪些城市在什么条件下满足了扩建条件
            // System.Diagnostics.Debug.WriteLine($"[ExpandAvail] ✅ {Name} 满足扩建条件: 资金={Fund}, 规模={JianzhuGuimo}, 人口={Population}");
            #endif
            
            return true;
        }

        public void Expand()
        {
            // 🔥 诊断：记录调用堆栈
            #if DEBUG
            var stackTrace = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.Debug.WriteLine($"[Expand] {Name}(ID:{ID}) 被调用");
            System.Diagnostics.Debug.WriteLine($"[Expand] 调用堆栈:\n{stackTrace}");
            #endif
            
            this.DecreaseFund(this.ExpandFund());  // 使用安全的减少方法
            
            // 🔥 修复：确保 _architectureArea 存在且正确初始化
            if (this._architectureArea == null)
            {
                this._architectureArea = new GameArea();
                if (!string.IsNullOrEmpty(ArchitectureAreaString))
                {
                    LoadFromString(this._architectureArea, ArchitectureAreaString);
                }
            }
            
            // 🔥 修复：直接操作 _architectureArea，避免通过 getter 触发一致性检查
            foreach (Point point in this.ExpandPoint())
            {
                this._architectureArea.AddPoint(point);
            }
            
            // 🔥 修复：扩建后立即同步更新 ArchitectureAreaString，防止 getter 中的一致性检查回滚修改
            if (this._architectureArea != null && this._architectureArea.Area != null && this._architectureArea.Area.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (Point p in this._architectureArea.Area)
                {
                    sb.Append(p.X);
                    sb.Append(" ");
                    sb.Append(p.Y);
                    sb.Append(" ");
                }
                this.ArchitectureAreaString = sb.ToString();
                
                #if DEBUG
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection)
                {
                    System.Diagnostics.Debug.WriteLine($"[Expand] {Name} 扩建完成，同步更新 ArchitectureAreaString，新规模: {this._architectureArea.Area.Count}");
                }
                #endif
            }


            this.ContactArea = this.ArchitectureArea.GetContactArea(false);
            
            // 🔥 全面诊断：检查所有可能为 null 的对象
            if (this.ArchitectureArea == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] ❌ 严重错误：{Name}(ID:{ID}) 的 ArchitectureArea 为 null！");
                throw new InvalidOperationException($"建筑 {Name}(ID:{ID}) 的 ArchitectureArea 为 null");
            }
            
            if (_architectureKind == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] ❌ 严重错误：{Name}(ID:{ID}) 的 _architectureKind 为 null！");
                System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] Kind={(Kind != null ? Kind.Name : "null")}");
                throw new InvalidOperationException($"建筑 {Name}(ID:{ID}) 的 _architectureKind 为 null");
            }
            
            if (this.BelongedFaction == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] ❌ 严重错误：{Name}(ID:{ID}) 的 BelongedFaction 为 null！");
                throw new InvalidOperationException($"建筑 {Name}(ID:{ID}) 的 BelongedFaction 为 null");
            }
            
            System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] {Name}(ID:{ID}) 准备调用 GetAreaFromArea");
            System.Diagnostics.Debug.WriteLine($"  - ArchitectureArea: {(this.ArchitectureArea != null ? "OK" : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"  - ViewDistance: {this.ViewDistance}");
            System.Diagnostics.Debug.WriteLine($"  - EffectiveViewDistance: {this.EffectiveViewDistance}");
            System.Diagnostics.Debug.WriteLine($"  - _architectureKind: {(_architectureKind != null ? "OK" : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"  - HasObliqueView: {_architectureKind.HasObliqueView}");
            System.Diagnostics.Debug.WriteLine($"  - BelongedFaction: {(this.BelongedFaction != null ? this.BelongedFaction.Name : "NULL")}");
            
            // 🔥 使用 EffectiveViewDistance 而不是 ViewDistance
            // 日期：2026-03-16
            // 原因：扩建后重新创建 ViewArea，应该使用有效视野
            this.ViewArea = GameArea.GetAreaFromArea(this.ArchitectureArea, this.EffectiveViewDistance, _architectureKind.HasObliqueView, this.BelongedFaction);
            System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] {Name}(ID:{ID}) ViewArea 创建成功");
            
            this.LongViewArea = GameArea.GetAreaFromArea(this.ArchitectureArea, this.LongViewDistance, _architectureKind.HasObliqueView, this.BelongedFaction);
            System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] {Name}(ID:{ID}) LongViewArea 创建成功");
            this.BaseFoodSurplyArea = this.LongViewArea;

            foreach (Point point in this.ArchitectureArea.Area)
            {
                Session.Current.Scenario.MapTileData[point.X, point.Y].TileArchitecture = this;
            }

            Session.Current.Scenario.SetMapTileArchitecture(this);

            // 🔥 修复：在设置为null之前，先保存LongViewArea的引用
            GameArea savedLongViewArea = this.LongViewArea;
            
            this.ViewArea = null;
            this.LongViewArea = null;
            if (!Session.Current.Scenario.Preparing)
            {
                foreach (Architecture architecture2 in Session.Current.Scenario.Architectures)
                {
                    architecture2.RefreshViewArea();
                }
                foreach (Troop troop in Session.Current.Scenario.Troops)
                {
                    troop.RefreshViewArchitectureRelatedArea();
                }
            }
            if (this.AutoRefillFoodInLongViewArea)
            {
                // 🔥 修复：使用保存的引用而不是已经为null的this.LongViewArea
                if (savedLongViewArea != null && savedLongViewArea.Area != null)
                {
                    foreach (Point point in savedLongViewArea.Area)
                    {
                        if (!Session.Current.Scenario.PositionOutOfRange(point))
                        {
                            Session.Current.Scenario.MapTileData[point.X, point.Y].AddSupplyingArchitecture(this);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Architecture.Expand] ⚠️ {Name}(ID:{ID}) savedLongViewArea为null，跳过AddSupplyingArchitecture");
                }
            }
            this.BelongedFaction.AddArchitectureKnownData(this);
        }

        public PersonList meifaxianhuaiyundefeiziliebiao()
        {
            PersonList meihuailiebiao = new PersonList();
            foreach (Person person in this.Feiziliebiao)
            {
                if (!person.faxianhuaiyun && this.BelongedFaction.Leader.isLegalFeiZiExcludeAge(person) && person.ArrivingDays <= 0)
                    meihuailiebiao.Add(person);
            }
            foreach (Person person in this.NvGuans)
            {
                if (!person.faxianhuaiyun && this.BelongedFaction.Leader.isLegalFeiZi(person) && person.Spouse == this.BelongedFaction.Leader && person.ArrivingDays <= 0)
                    meihuailiebiao.Add(person);
            }
            return meihuailiebiao;
        }

        public PersonList movableFeizis
        {
            get
            {
                PersonList meihuailiebiao = new PersonList();
                foreach (Person person in this.Feiziliebiao)
                {
                    if (person.ArrivingDays <= 0)
                        meihuailiebiao.Add(person);
                }
                return meihuailiebiao;
            }
        }

        /*
        private bool younvxingwujiang()
        {
            foreach (Person person in this.Persons)
            {
                if (person.Sex != person.BelongedFaction.Leader.Sex)
                {
                    return true;
                }
            }
            return false;
        }
        */

        public PersonList nvxingwujiang()
        {
            PersonList nvxingwujiangliebiao = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.ArrivingDays > 0) continue;
                if (this.BelongedFaction.Leader.isLegalFeiZi(person, true) || (this.BelongedFaction.IsAlien && this.BelongedFaction.Leader.isLegalFeiZiExcludeAge(person)))
                {
                    nvxingwujiangliebiao.Add(person);
                }
            }
            foreach (Captive c in this.Captives)
            {
                Person person = c.CaptivePerson;
                if (person.ArrivingDays > 0) continue;
                if (this.BelongedFaction.Leader.isLegalFeiZiExcludeAge(person))
                {
                    nvxingwujiangliebiao.Add(person);
                }
            }

            return nvxingwujiangliebiao;
        }

        public PersonList CanKilledPersons()
        {
            PersonList personList = new PersonList();
            foreach (Person person in this.Persons)
            {
                if (person.ID != this.BelongedFaction.LeaderID)
                {
                    personList.Add(person);
                }
            }

            return personList;
        }

        public PersonList CanKilledCaptives()
        {
            PersonList personList = new PersonList();
            foreach (Captive captive in this.Captives)
            {

                personList.Add(captive.CaptivePerson);

            }

            return personList;
        }

        public bool PrincessChangeLeader(bool byOccupy, Faction capturer, Person p)
        {
            bool result = false;
            if (p.Spouse != null)
            {
                if (p.Spouse.Spouse == p)
                {
                    p.Spouse.Spouse = null;
                }
                p.Spouse = null;
            }
            if (capturer.Leader.isLegalFeiZiExcludeAge(p) && capturer.hougongValid)
             {
                if (byOccupy)
                {
                    Session.Current.Scenario.YearTable.addChangeFactionPrincessEntry(Session.Current.Scenario.Date, p, capturer);
                }
                if (p.Spouse != null && this.BelongedFaction != null)
                {
                    p.Spouse.AddHated(this.BelongedFaction.Leader, -200 * p.Spouse.PersonalLoyalty * p.Spouse.PersonalLoyalty);
                }
                result = true;
             }
             else
             {
                p.Status = PersonStatus.Normal;
                if (byOccupy)
                {
                    Session.Current.Scenario.YearTable.addOutOfPrincessEntry(Session.Current.Scenario.Date, p, capturer);
                }
                else
                {
                    Session.Current.Scenario.YearTable.addOutOfPrincessByLeaderDeathEntry(Session.Current.Scenario.Date, p, capturer);
                }
            }
            return result;
        }

        public int Meinvkongjian
        {
            get
            {
                int kongjian = 0;
                foreach (Facility facility in this.Facilities)
                {
                    if (facility.Kind != null)
                    {
                        kongjian += facility.Kind.rongna;
                    }
                }
                return kongjian;
            }
        }

        public string meinvkongjianzifu
        {
            get
            {
                return this.Feiziliebiao.Count.ToString() + "/" + this.Meinvkongjian.ToString();
            }
        }

        public bool CanRemoveFacility(Facility f)
        {
            if (f.Kind.bukechaichu) return false;
            // if (this.Meinvkongjian - this.Feiziliebiao.Count < f.Kind.rongna && this.BelongedFaction != null && !this.BelongedFaction.IsAlien) return false;
            return true;
        }

        public FacilityList kechaichudesheshi()
        {
            FacilityList kechaichu = new FacilityList();
            foreach (Facility facility in this.Facilities)
            {
                if (this.CanRemoveFacility(facility))
                {
                    kechaichu.Add(facility);
                }
            }
            return kechaichu;
        }

        public PersonList yihuaiyundefeiziliebiao()
        {
            PersonList feiziliebiao = new PersonList();
            foreach (Person feizi in this.Feiziliebiao)
            {
                if (feizi.huaiyun)
                {
                    feiziliebiao.Add(feizi);
                }
            }
            return feiziliebiao;
        }

        [DataMember]
        public bool huangdisuozai
        {
            get;
            set;
        }

        public bool kejingongzijin()
        {
            if (Session.Current.Scenario.Date.Month == 3 && Session.Current.Scenario.youhuangdi() && this.Fund > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool KillPersonAvail()
        {
            if (this.PersonCount - (this.Persons.GameObjects.Contains(this.BelongedFaction.Leader) ? 1 : 0) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }



        public bool kejingongliangcao()
        {
            if (Session.Current.Scenario.Date.Month == 3 && Session.Current.Scenario.youhuangdi() && this.Food > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 中心点
        /// </summary>
        public Point zhongxindian
        {
            get
            {
                if (this.ArchitectureArea == null || this.ArchitectureArea.Area == null || this.ArchitectureArea.Area.Count == 0)
                {
                    return new Point(0, 0);
                }

                int xzonghe = 0;
                int yzonghe = 0;
                int xpingjunzhi;
                int ypingjunzhi;
                foreach (Point p in this.ArchitectureArea.Area)
                {
                    xzonghe += p.X;
                    yzonghe += p.Y;
                }
                xpingjunzhi = xzonghe / this.JianzhuGuimo;
                ypingjunzhi = yzonghe / this.JianzhuGuimo;
                foreach (Point p in this.ArchitectureArea.Area)
                {
                    if (p.X == xpingjunzhi && p.Y == ypingjunzhi)
                    {
                        return p;
                    }
                }
                return this.ArchitectureArea.Area[0];
            }
        }

        /// <summary>
        /// 顶点
        /// </summary>
        public Point dingdian
        {
            get
            {
                if (this.ArchitectureArea == null || this.ArchitectureArea.Area == null || this.ArchitectureArea.Area.Count == 0)
                {
                    return new Point(0, 0);
                }

                Point zuishangmiandedian = this.ArchitectureArea.Area[0];
                foreach (Point p in this.ArchitectureArea.Area)
                {
                    if (p.Y < zuishangmiandedian.Y)
                    {
                        zuishangmiandedian = p;
                    }
                }

                if (_architectureKind != null && _architectureKind.ID == 2)  //如果是关隘
                {
                    if (this.JianzhuGuimo == 1)
                    {
                        return zuishangmiandedian;
                    }
                    if (this.ArchitectureArea.Area.Count > 1 && this.ArchitectureArea.Area[0].X == this.ArchitectureArea.Area[1].X)
                    {
                        return zuishangmiandedian;
                    }
                    else
                    {
                        return this.zhongxindian;
                    }

                }

                return zuishangmiandedian;
            }
        }

        public int IdlingPersonCount
        {
            get
            {
                int result = 0;
                foreach (Person person in this.Persons)
                {
                    if (person.WorkKind == ArchitectureWorkKind.无)
                    {
                        result++;
                    }
                }
                return result;
            }
        }

        public String yocelanPersonString
        {
            get
            {
                PersonList pList = Session.Current.Scenario.GetPersonList(this);
                int idleCount = 0;
                foreach (Person p in pList)
                {
                    if (p.WorkKind == ArchitectureWorkKind.无)
                    {
                        idleCount++;
                    }
                }
                return idleCount.ToString() + "/" + pList.Count.ToString();
            }
        }

        public Person Mayor
        {
            get
            {
                if (this.MayorID == -1 ) return null ;

                if ( this.mayor == null )
                {
                    this.mayor = Session.Current.Scenario.Persons.GetGameObject(this.MayorID)as Person ;
                }

                if (this.mayor != null && this.BelongedFaction != null &&
                    (this.mayor == this.BelongedFaction .Leader || !this.mayor.Alive || !this.mayor.Available
                    || this.mayor.BelongedFaction != this.BelongedFaction
                    || this.mayor.BelongedFaction == null || (this.mayor.LocationArchitecture != this && this.mayor.BelongedTroop == null )))
                {
                    this.Mayor = null;
                }
                return this.mayor;
            }
            set
            {
                this.mayor = value;
                if (this.mayor != null)
                {
                    this.MayorID = this.mayor.ID;
                }
                else
                {
                    this.MayorID = -1;
                }
                this.MayorOnDutyDays = 0;
            }
        }
        [DataMember]
        public int MayorID { get; set; }
        //{
        //    get
        //    {
        //        return this.mayorID;
        //    }
        //    set
        //    {
        //        this.mayorID = value;
        //        //this.MayorOnDutyDays = 0;
        //    }
        //}

        public string MayorName
        {
            get
            {
                return ((this.Mayor != null) ? this.Mayor.Name : "----");
            }
        }

        /// <summary>
        /// 城威（能量值）- 用于右侧栏UI显示
        /// 日期：2026-03-21
        /// </summary>
        public int EnergyDisplay => this.CalculateInfluenceEnergy();


        public Person Advisor
        {
            get
            {
                GameObjectList sorted = this.PersonsExcludeNvGuan.GetList();

                if (sorted.Count == 0) return null;

                sorted.IsNumber = true;
                sorted.PropertyName = "Intelligence";
                sorted.SmallToBig = false;
                sorted.ReSort();

                PersonList cropped = new PersonList();
                foreach (Person p in sorted)
                {
                    if (p.Intelligence >= 70)
                    {
                        cropped.Add(p);
                    }
                    else
                    {
                        break;
                    }
                }

                if (cropped.Count == 0) return null;

                return (Person) cropped[GameObject.Random(cropped.Count / 5)];
            }
        }

        private int MeiXunlianHaoDeBianduiShu()
        {
            int bianduiShu = 0;
            foreach (Military military in this.Militaries)
            {
                if (military.Morale < military.MoraleCeiling || military.Combativity < military.CombativityCeiling)
                {
                    bianduiShu++;
                }
            }
            return bianduiShu;
        }

        public ArchitectureList ArchitectureListWithoutSelf()
        {
            ArchitectureList architectureList = new ArchitectureList();
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                architectureList.Add(architecture);
            }
            architectureList.Remove(this);
            return architectureList;
        }

        public void AddInformation(Information information)
        {
            this.Informations.AddInformation(information);
            information.BelongedArchitecture = this;
        }

        public void RemoveInformation(Information information)
        {
            this.Informations.Remove(information);
            information.BelongedArchitecture = null;
        }

        public delegate void BeginRecentlyAttacked(Architecture architecture);

        public delegate void FacilityCompleted(Architecture architecture, Facility facility);

        public delegate void fashengzainan(Architecture architecture, int zainanID);

        public delegate void HirePerson(PersonList personList);

        public delegate void MilitaryCreate(Architecture architecture, Military military);

        public delegate void PopulationEnter(Architecture a, int quantity);

        public delegate void PopulationEscape(Architecture a, int quantity);

        public delegate void ReleaseCaptiveAfterOccupied(Architecture architecture, PersonList persons);

        public delegate void RewardPersons(Architecture architecture, GameObjectList personlist);

        [StructLayout(LayoutKind.Sequential)]
        private struct RoutewayProcedureDetail
        {
            public Architecture Start;
            public float PreviousRate;
            public RoutewayProcedureDetail(Architecture a, float rate)
            {
                this.Start = a;
                this.PreviousRate = rate;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WorkRate
        {
            public float rate;
            public ArchitectureWorkKind workKind;
            public WorkRate(float r, ArchitectureWorkKind k)
            {
                this.rate = r;
                this.workKind = k;
            }
        }

        public class WorkRateList
        {
            public List<Architecture.WorkRate> RateList = new List<Architecture.WorkRate>();

            public void AddWorkRate(Architecture.WorkRate wr)
            {
                for (int i = 0; i < this.RateList.Count; i++)
                {
                    if (wr.rate <= this.RateList[i].rate)
                    {
                        this.RateList.Insert(i, wr);
                        return;
                    }
                }
                this.RateList.Add(wr);
            }

            public int Count
            {
                get
                {
                    return this.RateList.Count;
                }
            }
        }

        public float InfluenceKindValue(int id)
        {
            float result = 0;
            foreach (Influence i in Session.Current.Scenario.GameCommonData.AllInfluences.Influences.Values)
            {
                if (i.Kind.ID == id)
                {
                    foreach (ApplyingArchitecture j in i.appliedArch)
                    {
                        if (j.arch == this)
                        {
                            result += i.Value;
                        }
                    }
                }
            }
            return result;
        }

        public Person GetMaxFightingForcePerson()
        {
            int temp = 0;
            Person person = new Person();
            foreach (Person p in this.PersonsExcludeNvGuan)
            {
                if(p.FightingForce>temp)
                {
                    person = p;
                }
            }
            return person;
        }

        /**
         *  public float CommandTrainingFacilityRate { get; set; }
        public float StrengthTrainingFacilityRate { get; set; }
        public float IntelligenceTrainingFacilityRate { get; set; }
        public float PoliticsTrainingFacilityRate { get; set; }
        public float GlamourTrainingFacilityRate { get; set; }
        public float InfantryTrainingFacilityRate { get; set; }
        public float CavalryTrainingFacilityRate { get; set; }
        public float BowmanTrainingFacilityRate { get; set; }
        public float NavalTrainingFacilityRate { get; set; }
        public float SiegeTrainingFacilityRate { get; set; }
*/
        public void FacilityTrainCommand(Person p)
        {
            if (CommandTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.CommandExperience += (int) (Session.Parameters.TrainAbilityAmount * CommandTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainStrength(Person p)
        {
            if (StrengthTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.StrengthExperience += (int)(Session.Parameters.TrainAbilityAmount * StrengthTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainIntelligence(Person p)
        {
            if (IntelligenceTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.IntelligenceExperience += (int)(Session.Parameters.TrainAbilityAmount * IntelligenceTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainPolitics(Person p)
        {
            if (PoliticsTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.PoliticsExperience += (int)(Session.Parameters.TrainAbilityAmount * PoliticsTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainGlamour(Person p)
        {
            if (GlamourTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.GlamourExperience += (int)(Session.Parameters.TrainAbilityAmount * GlamourTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainInfantry(Person p)
        {
            if (InfantryTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.BubingExperience += (int)(Session.Parameters.TrainAbilityAmount * InfantryTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainCavalry(Person p)
        {
            if (CavalryTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.QibingExperience += (int)(Session.Parameters.TrainAbilityAmount * CavalryTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainBowman(Person p)
        {
            if (BowmanTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.NubingExperience += (int)(Session.Parameters.TrainAbilityAmount * BowmanTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainSiege(Person p)
        {
            if (SiegeTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.QixieExperience += (int)(Session.Parameters.TrainAbilityAmount * SiegeTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void FacilityTrainNaval(Person p)
        {
            if (NavalTrainingFacilityRate > 0 && p.Fund >= Session.Parameters.TrainAbilityCost)
            {
                p.Fund -= Session.Parameters.TrainAbilityCost;
                p.Tiredness += Session.Parameters.TrainAbilityTiredness;
                p.ShuijunExperience += (int)(Session.Parameters.TrainAbilityAmount * NavalTrainingFacilityRate * GameObject.Random(90, 110) / 100f);
            }
        }

        public void GoToPub(Person p)
        {
            if (HasPub())
            {
                int pubCost = GetPubCost();
                if (p.Fund < pubCost) return;
                p.Fund -= pubCost;

                p.GlamourExperience += GameObject.Random(20, 30);

                var candidates = Persons.GetList();
                candidates.AddRange(NoFactionPersons);

                var dict = new Dictionary<Person, float>();
                foreach (Person q in candidates)
                {
                    if (q == p) break;
                    dict[q] = Person.GetIdealAttraction(q, p);
                }
                dict.SortByDictValueDesc();
                
                int invited = 0;
                foreach (Person p2 in candidates)
                {
                    if (GameObject.Chance(p2.GetRelation(p) / 60 + 10))
                    {
                        p.AdjustRelation(p2, 3, 0);
                        p2.AdjustRelation(p, 3, 0);
                        invited++;
                        if (invited > 3) break;
                    }
                }
            }
        }

        public List<Facility> GetAvailableCreateTreasureFacilities()
        {
            var results = new List<Facility>();
            
            foreach (Facility facility in this.Facilities)
            {
                if (facility.Kind.Influences.HasInfluenceKind(3530))
                {
                    results.Add(facility);
                }
            }

            return results;
        }

        public void CreateTreasure(Person p)
        {
            var facilities = GetAvailableCreateTreasureFacilities();

            var groupsToCreate = new HashSet<TreasureCreationSetting>();
            foreach (Facility facility in facilities) 
            {
                foreach (Influence influence in facility.Kind.Influences.Influences.Values) 
                {
                    if (influence.Kind.ID == 3530) 
                    {
                        var treasureSetting = Session.Current.Scenario.GameCommonData.AllTreasureCreationSettings.GetGameObject(int.Parse(influence.Parameter)) as TreasureCreationSetting;

                        if (p.Fund < treasureSetting.Cost) continue;
                        groupsToCreate.Add(treasureSetting);
                    }
                }
            }

            foreach (Treasure t in p.Treasures) {
                if (groupsToCreate.Select(g => g.TreasureGroup).Contains(t.TreasureGroup)) {
                    groupsToCreate.RemoveWhere(g => g.TreasureGroup == t.TreasureGroup);
                }
            }

            if (groupsToCreate.Count == 0) return;

            // 🔥 技术性修复：避免IndexOutOfRangeException
            var groupList = groupsToCreate.ToList();
            if (groupList.Count == 0) return;
            
            var selectedSetting = groupList[GameObject.Random(groupList.Count)];

            p.Fund -= selectedSetting.Cost;

            var treasure = new Treasure();
            treasure.TreasureGroup = selectedSetting.TreasureGroup;
            treasure.AppearYear = Session.Current.Scenario.Date.Year;
            treasure.Available = true;

            do {
                var chosenInfluence = Session.Current.Scenario.GameCommonData.AllInfluences.GetInfluence(selectedSetting.EligibleInfluenceIDs[GameObject.Random(selectedSetting.EligibleInfluenceIDs.Length)]) as Influence;
                treasure.Influences.AddInfluence(chosenInfluence);
            } while (GameObject.Chance(50));
            
            treasure.Pic = selectedSetting.PicIDs[GameObject.Random(selectedSetting.PicIDs.Length)];
            treasure.Worth = selectedSetting.Cost / 250;

            treasure.Durability = GameObject.Random(360) + 1620; 
            
            treasure.Name = p.Name + selectedSetting.Name;

            var minId = 9999;
            foreach (Treasure t in Session.Current.Scenario.Treasures)
            {
                if (t.ID > minId)
                {
                    minId = t.ID;
                }
            }
            treasure.ID = minId + 1;

            Session.Current.Scenario.Treasures.Add(treasure);

            p.ReceiveTreasure(treasure);
        }

        #region 宝物

        /// <summary>
        /// 势力内是否有宝物
        /// </summary>
        /// <returns></returns>
        public bool FactionHasTreasure()
        {
            if (this.BelongedFaction != null)
            {
                // 检查势力君主是否有宝物
                if (this.BelongedFaction.Leader != null && this.BelongedFaction.Leader.TreasureCount > 0)
                {
                    return true;
                }
                
                // 检查势力内其他人员是否有宝物
                foreach (Person person in this.BelongedFaction.Persons.GetList())
                {
                    if (person.TreasureCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否有宝物可以奖励（君主拥有的宝物）
        /// 🔥 2026-03-03 修复：从扩展方法移到类内部，支持源生成器
        /// </summary>
        public bool HasTreasureToAward()
        {
            if (this.BelongedFaction == null)
                return false;

            if (this.BelongedFaction.Leader == null)
                return false;

            return this.BelongedFaction.Leader.Treasures.Count > 0;
        }

        /// <summary>
        /// 检查是否有宝物可以没收（下属拥有的宝物）
        /// 🔥 2026-03-03 修复：从扩展方法移到类内部，支持源生成器
        /// </summary>
        public bool HasTreasureToConfiscate()
        {
            if (this.BelongedFaction == null)
                return false;

            return this.BelongedFaction.AllTreasuresExceptLeader.Count > 0;
        }

        /// <summary>
        /// 检查是否有宝物可以出售（君主拥有的宝物）
        /// 🔥 2026-03-03 修复：从扩展方法移到类内部，支持源生成器
        /// </summary>
        public bool HasTreasureToSell()
        {
            return this.HasTreasureToAward();
        }

        /// <summary>
        /// 检查建筑内是否有人拥有宝物
        /// 🔥 2026-03-16 修复：添加缺失的 HasTreasure 方法，供条件系统调用
        /// </summary>
        public bool HasTreasure()
        {
            foreach (Person person in this.Persons)
            {
                if (person.TreasureCount > 0)
                {
                    return true;
                }
            }
            return false;
        }


        #endregion

        // [AI Optimization 2026-01-08] 反添油战术检测：是否满足协同出兵条件？
        // target: 进攻目标
        // potentialTroopForce: 预备创建的这支部队的战斗力
        // return: true = 允许出兵; false = 囤积兵力，暂不出兵
        public bool CheckCoordinatedAttack(Architecture target, int potentialTroopForce)
        {
            if (target == null || target.BelongedFaction == null) return true;

            // 1. 防守/救援紧急情况：无需协同，立即出兵
            // 如果目标是自己或友军（救援），或者目标就在我脸贴脸（防守），直接允许
            if (target.BelongedFaction == this.BelongedFaction || this.BelongedFaction.IsFriendly(target.BelongedFaction)) return true;
            
            // 2. 战场侦察：前线是否已经打起来了？
            // 如果目标周围已经有我方其他部队（>=1支），说明是去"增援"，不需要再死等，立即出发形成连续攻势
            bool hasFriendlyTroopsNearby = false;
            foreach (Troop t in this.BelongedFaction.Troops)
            {
                if (t.TargetArchitecture == target && !t.IsTransport && !t.Destroyed) 
                {
                    hasFriendlyTroopsNearby = true;
                    break;
                }
            }
            if (hasFriendlyTroopsNearby) return true;

            // 3. "单点爆破"判定：不仅要看人多，还要看是否够强
            // 如果这支部队是"高达"（战斗力极高，比如 > 15000 或 统率 > 95），
            // 这种主力就算单走也是核威慑，允许单独出击。
            if (potentialTroopForce > 12000) return true; 

            // --- 核心：反添油逻辑 (Anti-Drip-Feeding) ---
            
            // 4. 后勤储备检查
            // 如果前线没队友，自己又不是超级主力，那么必须保证：
            // "我出城后，城里剩下的兵和粮，至少还够再造 1-2 支同等规模的部队"
            // 这样才能形成"波次进攻"，而不是排队送死。
            
            int safeFoodThreshold = 50000; // 安全粮草线
            
            // 检查剩余人口：假设每支部队平均消耗 5000-8000 人，城里至少要留个 10000+ 人做后备
            bool hasManpowerReserve = this.Population >= 12000; 
            
            // 检查剩余粮草：打仗最怕断粮，存粮不够不许开第一枪
            bool hasFoodReserve = this.Food > safeFoodThreshold;

            if (hasManpowerReserve && hasFoodReserve)
            {
                // 只有弹药充足，才允许打响第一枪
                return true; 
            }
            
            // 5. 否则，忍耐 (Hold)
            // 资源不足以形成波次攻势，取消本次出兵，继续攒钱攒人。
            return false;
        }
        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (this.ArchitectureArea != null)
            {
                this.ArchitectureAreaString = this.ArchitectureArea.ToString();
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context) => RebuildAfterDeserialization();

        /// <summary>
        /// IJsonOnDeserialized 接口实现 —— STJ AOT 模式下的反序列化回调。
        /// [OnDeserialized] 特性仅被 DataContractSerializer 识别，STJ AOT 不调用它。
        /// </summary>
        void System.Text.Json.Serialization.IJsonOnDeserialized.OnDeserialized() => RebuildAfterDeserialization();

        private void RebuildAfterDeserialization()
        {
            // 🔥 关键修复：确保 ArchitectureArea 从 ArchitectureAreaString 正确重建
            if (!string.IsNullOrEmpty(this.ArchitectureAreaString))
            {
                this.ArchitectureArea = new GameArea();
                this.LoadFromString(this.ArchitectureArea, this.ArchitectureAreaString);
                System.Diagnostics.Debug.WriteLine($"[Architecture.OnDeserialized] {this.Name}(ID:{this.ID}) 区域点数: {this.ArchitectureArea.Area.Count}");
            }
            else
            {
                this.ArchitectureArea ??= new GameArea();
                System.Diagnostics.Debug.WriteLine($"[Architecture.OnDeserialized] {this.Name}(ID:{this.ID}) ArchitectureAreaString 为空");
            }

            // 🔥 新增：诊断 KindID 状态
            if (this.KindID <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Architecture.OnDeserialized] ⚠️ {this.Name}(ID:{this.ID}) KindID 无效: {this.KindID}");
            }
        }
    }
}

