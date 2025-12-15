using GameGlobal;
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
using GameObjects.Conditions;
using System.Runtime.Serialization;
using GameManager;

namespace GameObjects
{

    [DataContract]
    public class Faction : GameObject
    {
       // public int PrinceID = -1;
        //public int AllRoundOfficerCount;
        private int militarycount;
        private int transferingmilitarycount;
        [DataMember]
        public int ZhaoxianFailureCount = 0;
        [DataMember]
        public int YearOfficialLimit = 0;
        private Person prince = null;
        private int princeID = -1;
        private bool isAlien = false;
        private int guanjuedezhi = 0;
        private int chaotinggongxiandudezhi = 0;
        public bool AIFinished;
#pragma warning disable CS0169 // The field 'Faction.AIThread' is never used
        //private Thread AIThread;
#pragma warning restore CS0169 // The field 'Faction.AIThread' is never used
        public ZhandouZhuangtai BattleState = ZhandouZhuangtai.和平;
        

        
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
        private WorldOfTheThreeKingdoms.GameManager.StrategicMap _strategicMap;
        
        /// <summary>
        /// 获取或创建战略影响力地图
        /// </summary>
        public WorldOfTheThreeKingdoms.GameManager.StrategicMap StrategicMap
        {
            get
            {
                if (_strategicMap == null && Session.Current?.Scenario?.ScenarioMap != null)
                {
                    int width = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                    int height = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                    _strategicMap = new WorldOfTheThreeKingdoms.GameManager.StrategicMap(width, height);
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

        public void Init()
        {
            BattleState = ZhandouZhuangtai.和平;

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

            this.FactionColor = Session.Current.Scenario.GameCommonData.AllColors[this.ColorIndex];

            this.RoutewayPathBuilder = new RoutewayPathFinder();
            this.RoutewayPathBuilder.OnGetCost += new RoutewayPathFinder.GetCost(this.RoutewayPathBuilder_OnGetCost);
            this.RoutewayPathBuilder.OnGetPenalizedCost += new RoutewayPathFinder.GetPenalizedCost(this.RoutewayPathBuilder_OnGetPenalizedCost);
        }

        [DataMember]
        public string ArchitecturesString { get; set; }

        public ArchitectureList Architectures = new ArchitectureList();
        private int armyScale = 0;

        [DataMember]
        public bool AutoRefuse;

        [DataMember]
        public string AvailableTechniquesString { get; set; }

        public TechniqueTable AvailableTechniques = new TechniqueTable();

        [DataMember]
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
        public string InformationsString { get; set; }

        public InformationList Informations = new InformationList();
        private Dictionary<Point, InformationTile> knownAreaData;
        public Dictionary<int, Troop> KnownTroops = new Dictionary<int, Troop>();
        private Person leader = null;
        private int leaderID;
        private Person advisor = null;
        private int advisorID = -1;

        [DataMember]
        public string LegionsString { get; set; }

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
        public int PlanTechniqueString { get; set; }

        public Technique PlanTechnique;
        public Architecture PlanTechniqueArchitecture;

        [DataMember]
        public List<int> PreferredTechniqueKinds = new List<int>();

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
        public string RoutewaysString { get; set; }

        public RoutewayList Routeways = new RoutewayList();
        private Dictionary<ClosedPathEndpoints, List<Point>> SecondTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
        private int[,] secondTierMapCost;
        [DataMember]
        public int SecondTierXResidue = 0;
        [DataMember]
        public int SecondTierYResidue = 0;
        
        [DataMember]
        public string SectionsString { get; set; }

        public SectionList Sections = new SectionList();

        public bool StopToControl;

        public MilitaryKindTable TechniqueMilitaryKinds = new MilitaryKindTable();
        private int techniquePoint;
        private int techniquePointForFacility;
        private int techniquePointForTechnique;
        private Dictionary<ClosedPathEndpoints, List<Point>> ThirdTierKnownPaths = new Dictionary<ClosedPathEndpoints, List<Point>>();
        private int[,] thirdTierMapCost;
        [DataMember]
        public int ThirdTierXResidue = 0;
        [DataMember]
        public int ThirdTierYResidue = 0;

        [DataMember]
        public string TroopListString { get; set; }

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
                foreach (Captive c in Session.Current.Scenario.Captives)
                {
                    if (c.CaptiveFaction == this)
                    {
                        result.Add(c.CaptivePerson);
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
                        if (p != this.Leader)  result.Add(p);
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

        private void RemoveKnownAreaData(Point p, InformationLevel level)
        {
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

        private InformationLevel getInformationLevel(Point p)
        {
            if (!this.knownAreaData.ContainsKey(p))
            {
                return InformationLevel.无;
            }
            else
            {
                return this.knownAreaData[p].Level;
            }
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
                        if (GameObject.Random((int) (60f / (this.Leader.Ambition + 1) * Math.Sqrt(this.Leader.NumberOfChildren))) == 0)
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

        private void AI()
        {
            Session.Current.Scenario.Threading = true;
            this.AIFinished = false;
            this.AIPrepare();
            this.AIDiplomacy();
            this.AISections();
            this.AICapital();
            this.AICaptives();
            this.AITechniques();
            this.AINvGuan();
            this.AIMakeMarriage();
            this.AISelectPrince();
            this.AIZhaoXian();
            this.AIAppointMayor();
            this.AIAppointAdvisor();
            this.AIHouGong();
            this.AIArchitectures();
            this.AITransfer();
            this.AILegions();
            this.AITrainChildren();
            this.AIFinished = true;
            Session.Current.Scenario.Threading = false;
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
                            int toTransfer = (int) (Math.Min(a.Fund - a.FundCeiling * (a.FrontLine ? 0.7 : 0.5), b.FundCeiling * 0.8 - b.Fund - b.FundInPack));
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
                        deficitFund = Math.Min(deficitFund, a.FundCeiling * 9 / 10- a.FundInPack - a.Fund);

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

        private void AIArchitectures()
        {
            foreach (Architecture architecture in this.Architectures.GetRandomList())
            {
                architecture.AI();
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
            foreach (Legion legion in this.Legions.GetRandomList())
            {
                legion.AI();
            }
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
                foreach (Captive captive in this.SelfCaptives.GetRandomList())
                {
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
                    section.AI();
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
                        Architecture a = this.Architectures[0] as Architecture;
                        if (a.IsFundEnough)
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

                    this.AddSection(section);
                    Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                    list = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                    if (list.Count > 0)
                    {
                        section.AIDetail = list[GameObject.Random(list.Count)] as SectionAIDetail;
                    }
                    else
                    {
                        section.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailList()[0] as SectionAIDetail;
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
                    
                    this.AddSection(section);
                    Session.Current.Scenario.Sections.AddSectionWithEvent(section);
                    list = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                    if (list.Count > 0)
                    {
                        section.AIDetail = list[GameObject.Random(list.Count)] as SectionAIDetail;
                    }
                    else
                    {
                        section.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailList()[0] as SectionAIDetail;
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
                ExtensionInterface.call("ChangeCapital", new Object[] { Session.Current.Scenario, this });
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
            ExtensionInterface.call("ChangeFaction", new Object[] { Session.Current.Scenario, this });
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
                ExtensionInterface.call("ChangeKing", new Object[] { Session.Current.Scenario, this });
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
            foreach (Section section in this.Sections.GetList())
            {
                this.RemoveSection(section);
                Session.Current.Scenario.Sections.Remove(section);
            }
            foreach (Architecture architecture in this.Architectures)
            {
                architecture.BelongedSection = null;
            }
        }

        public Section CreateFirstSection()
        {
            if ((this.Capital != null) && (this.ArchitectureCount > 0))
            {
                Section section = new Section();
                section.ID = Session.Current.Scenario.Sections.GetFreeGameObjectID();
                section.Name = this.Capital.Name + "军区";
                section.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false)[0] as SectionAIDetail;
                foreach (Architecture architecture in this.Architectures)
                {
                    section.AddArchitecture(architecture);
                }
                this.AddSection(section);
                Session.Current.Scenario.Sections.AddSectionWithEvent(section);
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
                for (int i = 0; i < PersonInCurrentFaction.Count; i++)
                {
                    if (Fivetiger[i].StrengthIncludingExperience >= 70)
                    {
                        FivetigerString[i] = Fivetiger[i].Name + "(" + Fivetiger[i].StrengthIncludingExperience.ToString() + ")";
                    }
                    if (i == 4) break;
                }
            }
            FiveTigers = string.Concat(new object[] { FivetigerString[0], " • ", FivetigerString[1], " • ", FivetigerString[2], " • ", FivetigerString[3], " • ", FivetigerString[4] });
        }

        [DataMember]
        public string TransferingMilitariesString { get; set; }

        public MilitaryList TransferingMilitaries { get; set; }

        public List<string> LoadTransferingMilitariesFromString(MilitaryList militaries, string dataString)
        {
            List<string> errorMsg = new List<string>();
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
                    if (m.StartingArchitecture != null && m.TargetArchitecture != null  && m.TargetArchitecture.BelongedFaction != null 
                        && m.TargetArchitecture.BelongedFaction == this && m.BelongedArchitecture == null )                                            
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
                    Person person = this.Leader.ChildrenCanBeSelectedAsPrince()[0] as Person;
                    if (person.ID != this.PrinceID)
                    {
                        this.PrinceID = person.ID;
                        this.Capital.DecreaseFund(Session.Parameters.SelectPrinceCost);
                        this.Capital.SelectPrince(person); //AI立储年表和报告
                        //Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, person.Name, "SelectPrince", "", "", true);
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

        private void AIAppointMayor()
        {
            foreach (Architecture a in this.Architectures)
            {
                if (!Session.Current.Scenario.IsPlayer(this) || a.BelongedSection.AIDetail.AutoRun)
                {
                    if (a.AppointMayorAvail())
                    {
                        Person person = a.AIMayorCandicate[0] as Person;
                        a.MayorID = person.ID;
                        a.AppointMayor(person);
                        a.MayorOnDutyDays = 0;
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
            foreach (PersonGeneratorType type in Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes)
            {
                sb.AppendFormat("{0}:{1},", type.ID, count.ContainsKey(type) ? count[type] : 0);
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
            foreach (PersonGeneratorType type in Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes)
            {
                if (type.ID == id)
                {
                    return type;
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
                foreach (Architecture a in this.Architectures.GetRandomList())
                {
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
            Session.Current.Scenario.YearTable.addFactionDestroyedEntry(Session.Current.Scenario.Date, this);
            this.Leader.Reputation /= 2;
            if (this.OnFactionDestroy != null)
            {
                this.OnFactionDestroy(this);
            }
            foreach (Captive captive in this.SelfCaptives.GetList())
            {
                //captive.TransformToNoFaction();
                captive.TransformToNoFactionCaptive();
            }
            /*
            foreach (Troop troop in this.Troops.GetList())
            {
                troop.Destroy();
            }
            */

            foreach (Section section in this.Sections.GetList())
            {
                this.RemoveSection(section);
                Session.Current.Scenario.Sections.Remove(section);
            }
            Session.Current.Scenario.DiplomaticRelations.RemoveDiplomaticRelationByFactionID(base.ID);
            Session.Current.Scenario.Factions.Remove(this);
            Session.Current.Scenario.PlayerFactions.Remove(this);
            this.Destroyed = true;
            ExtensionInterface.call("FactionDestroyed", new Object[] { Session.Current.Scenario, this });
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
            foreach (Legion legion in this.Legions)
            {
                if (legion.WillArchitecture == will)
                {
                    return legion;
                }
            }
            return null;
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
            int terrainAdaptability = 0;

            Architecture onArch = Session.Current.Scenario.GetArchitectureByPositionNoCheck(position);
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
                }
            }
            ExtensionInterface.call("ForceChangeCapital", new Object[] { Session.Current.Scenario, this });
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
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
            List<string> errorMsg = new List<string>();
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.Architectures.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    Architecture architecture = architectures.GetGameObject(int.Parse(str)) as Architecture;
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
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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
            
        }

        private void PlayerAI()
        {
            Session.Current.Scenario.Threading = true;
            this.AIFinished = false;
            this.AIPrepare();
            this.PlayerAITransfer();
            this.PlayerTechniqueAI();
            this.PlayerAIArchitectures();
            this.PlayerAILegions();
            this.PlayerAIAppointMayor();
            this.PlayerAIAppointAdvisor();
            this.AITrainChildren();
            this.AIFinished = true;
            Session.Current.Scenario.Threading = false;
        }

        private void PlayerAIArchitectures()
        {
            foreach (Architecture architecture in this.Architectures.GetRandomList())
            {
                if (architecture.BelongedSection == null || architecture.BelongedSection.AIDetail.AutoRun)
                {
                    if (architecture.BelongedSection == null)
                    {
                        architecture.BelongedSection = architecture.BelongedFaction.FirstSection;
                    }
                    architecture.AI();
                }
                else
                {
                    architecture.PlayerAutoAI();
                }
            }
        }

        private void PlayerAILegions()
        {
            foreach (Legion legion in this.Legions.GetRandomList())
            {
                if ((legion.StartArchitecture != null) && (legion.StartArchitecture.BelongedFaction == this))
                {
                    if (legion.StartArchitecture.BelongedSection.AIDetail.AutoRun)
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
            AIAppointMayor();
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
            this.Legions.Remove(legion);
            legion.BelongedFaction = null;
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
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader.Name, TextMessageKind.EncircleDiplomaticRelation, "EncircleDiplomaticRelation", "EncircleDiplomaticRelation.jpg", "EncircleDiplomaticRelation", target.Name, true);
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
            Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.NeutralPerson, encircler.BelongedFaction.Leader.Name, "DenounceDiplomaticRelation", "DenounceDiplomaticRelation.jpg", "DenounceDiplomaticRelation", toEncircle.Name, true);

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
                    Session.MainGame.mainGameScreen.xianshishijiantupian(toBreak.Leader, this.Leader.Name, TextMessageKind.ResetDiplomaticRelation, "ResetDiplomaticRelation", "ResetDiplomaticRelation.jpg", "ResetDiplomaticRelation", toBreak.LeaderName, true);
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
                                Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader.Name, TextMessageKind.BreakDiplomaticRelation, "BreakDiplomaticRelation", "BreakDiplomaticRelation.jpg", "BreakDiplomaticRelation", opposite.Leader.Name, true);
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
                        Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.Leader.Name, TextMessageKind.ResetDiplomaticRelation, "ResetDiplomaticRelation", "ResetDiplomaticRelation.jpg", "ResetDiplomaticRelation", minTroopFactionopposite.LeaderName, true);
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

        public bool Run()
        {
            if (!this.preUserControlFinished)
            {
                this.Develop();
                this.preUserControlFinished = true;
            }
            if (this.Controlling || this.Passed)
            {
                return this.Passed;
            }
            if (Session.Current.Scenario.IsPlayer(this))
            {
                if (this.WantControl)
                {
                    if (!Session.Current.Scenario.Threading)
                    {
                        if (!this.AIFinished)
                        {
                            /*thread = new Thread(new ThreadStart(this.PlayerAI));
                            thread.Start();
                            thread.Join();
                            thread = null;*/
                            this.PlayerAI();
                            return false;
                        }
                        this.Controlling = true;
                        if (this.OnGetControl != null)
                        {
                            this.OnGetControl(this);
                        }
                        return false;
                    }
                    return false;
                }
                if (!Session.Current.Scenario.Threading)
                {
                    if (!this.AIFinished)
                    {
                        /*thread = new Thread(new ThreadStart(this.PlayerAI));
                            thread.Start();
                            thread.Join();
                            thread = null;*/
                        this.PlayerAI();
                        return false;
                    }
                    this.Passed = true;
                    return true;
                }
                return false;
            }

            if (!this.AIFinished)
            {
                /*thread = new Thread(new ThreadStart(this.AI));
                        thread.Start();
                        thread.Join();
                        thread = null;*/
                this.AI();
                return false;
            }
            this.Passed = true;
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
            Session.Current.Scenario.YearTable.addBecomeEmperorLegallyEntry(Session.Current.Scenario.Date, Session.Current.Scenario.Persons.GetGameObject(7000) as Person, this);
            Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.Persons.GetGameObject(7000) as Person, this.LeaderName, TextMessageKind.BecomeEmperorLegally, "BecomeEmperorLegally", "shanwei.jpg", "",
                Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, true);
            Session.MainGame.mainGameScreen.xiejinxingjilu("BecomeEmperorLegally", this.LeaderName,
                Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, this.Leader.Position);
            this.Capital.DecreaseFund(100000);

            ExtensionInterface.call("BecomeEmperorLegally", new Object[] { Session.Current.Scenario, this });
            Session.Current.Scenario.BecomeNoEmperor();
        }

        private bool HasEmperor()
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
            Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.LeaderName, TextMessageKind.BecomeEmperorIllegally, "Zili", "BecomeEmperor.jpg", "",
                Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, true);
            Session.MainGame.mainGameScreen.xiejinxingjilu("Zili", this.LeaderName,
                Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, this.Leader.Position);
            this.Capital.DecreaseFund(100000);
            if (!Session.Current.Scenario.youhuangdi() || this.IsAlien)
            {
                return;
            }
            else
            {
                this.DoSelfBecomeEmperorInfluence();
            }
            ExtensionInterface.call("SelfBecomeEmperor", new Object[] { Session.Current.Scenario, this });
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
            if (Session.Current.Scenario.IsPlayer(this) || gj.ShowDialog)
            {
                Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.Persons.GetGameObject(7000) as Person, this.LeaderName, TextMessageKind.RiseEmperorClass, "shengguan", "shengguan.jpg", "",
                   gj.Name, true);
                Session.MainGame.mainGameScreen.xiejinxingjilu("shengguan", this.LeaderName,
                    gj.Name, this.Leader.Position);
            }

            Session.Current.Scenario.YearTable.addAdvanceGuanjueEntry(Session.Current.Scenario.Date, this, Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue));
            ExtensionInterface.call("Advancement", new Object[] { Session.Current.Scenario, this });
        }

        private void SelfAdvancement()
        {
            this.guanjue++;

            guanjuezhongleilei gj = Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue);
            if (Session.Current.Scenario.IsPlayer(this) || gj.ShowDialog)
            {
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.Leader, this.LeaderName, TextMessageKind.SelfRiseEmperorClass, "Zili", "", "",
                    Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, true);
                Session.MainGame.mainGameScreen.xiejinxingjilu("Zili", this.LeaderName,
                    Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name, this.Leader.Position);
            }
            Session.Current.Scenario.YearTable.addSelfAdvanceGuanjueEntry(Session.Current.Scenario.Date, this, Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue));
            ExtensionInterface.call("SelfAdvancement", new Object[] { Session.Current.Scenario, this });
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
                if (a.Kind.CountToMerit)
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
                return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).Name;
            }
        }

        public int shengwangshangxian
        {
            get
            {
                return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue).shengwangshangxian;
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
                        return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1).xuyaogongxiandu;

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
                    return Session.Current.Scenario.GameCommonData.suoyouguanjuezhonglei.Getguanjuedezhonglei(this.guanjue + 1).xuyaochengchi;

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
                    foreach (Faction faction2 in Session.Current.Scenario.Factions.GetRandomList())
                    {
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
                foreach (Faction faction2 in this.GetUnderZeroDiplomaticRelationFactions().GetRandomList())
                {
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
                foreach (Faction faction2 in this.GetUnderZeroDiplomaticRelationFactions().GetRandomList())
                {
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
                        ExtensionInterface.call("TechniqueUpgradeComplete", new Object[] { Session.Current.Scenario, this, technique });
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
            ExtensionInterface.call("UpgradeTechnique", new Object[] { Session.Current.Scenario, this });
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
                    this.capital = Session.Current.Scenario.Architectures.GetGameObject(this.capitalID) as Architecture;
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
                return this.CreateFirstSection();
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

                float num = (Session.Parameters.InternalSurplusFactor - this.Power) / (float) Session.Parameters.InternalSurplusFactor;

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
                    this.leader = Session.Current.Scenario.Persons.GetGameObject(this.LeaderID) as Person;
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
                    this.advisor = Session.Current.Scenario.Persons.GetGameObject(this.AdvisorID) as Person;
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
                    this.prince = Session.Current.Scenario.Persons.GetGameObject(this.PrinceID) as Person;
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
        [DataMember]
        public string MilitariesString { get; set; }

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

        [DataMember]
        public int MilitaryCount
        {
            get
            {
                return this.Militaries.Count ;
            }
            set
            {
                this.militarycount = value;
            }
        }
        [DataMember]
        public int TransferingMilitaryCount
        {
            get
            {
                return this.TransferingMilitaries.Count;
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
                foreach (Captive c in Session.Current.Scenario.Captives)
                {
                    if (c.CaptiveFaction == this)
                    {
                        result++;
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
                        result .Add (person);
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
        /// 军师候选人列表（玩家用）
        /// </summary>
        public PersonList AdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive && 
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70)
                    {
                        result.Add(p);
                    }
                }
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
            // 设置军师
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
        public AdvisorSuggestionKind CurrentRoundSuggestion { get; set; } = AdvisorSuggestionKind.None;

        /// <summary>
        /// 当前建议的详细信息
        /// </summary>
        public AdvisorSuggestion CurrentSuggestionDetails { get; set; }

        /// <summary>
        /// 上次检查建议的回合数
        /// </summary>
        [DataMember]
        public int LastSuggestionCheckTurn { get; set; } = -1;

        /// <summary>
        /// 上次军师推荐人才的年份（用于冷却控制）
        /// </summary>
        [DataMember]
        public int LastTalentRecommendYear { get; set; } = 0;

        /// <summary>
        /// 军师举荐功能是否启用
        /// </summary>
        [DataMember]
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
                // 1. 先让所有部队更新记忆 (睁眼看世界)
                // 每个部队观察周围环境，更新AI记忆地图
                foreach (Troop troop in this.Troops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        troop.UpdateMemory();
                    }
                }
                
                // 2. 刷新势能图 (基于记忆在大脑里绘制地图)
                // 基于收集到的情报，重新计算战略威胁分布
                if (this.StrategicMap != null)
                {
                    this.StrategicMap.Refresh(this);
                }
                
                // 3. 执行部队移动 (基于势能图做决策)
                // 每个部队根据战略地图做出最优移动决策
                foreach (Troop troop in this.Troops)
                {
                    if (troop != null && !troop.Destroyed && troop.Controllable)
                    {
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
    }
}

