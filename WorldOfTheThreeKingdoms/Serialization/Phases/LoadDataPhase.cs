using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.SectionDetail;
using GameObjects.MapDetail;
using GameObjects.Influences;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace WorldOfTheThreeKingdoms.Serialization.Phases
{
    /// <summary>
    /// Phase 2: Load Data Phase
    /// Deserializes DTOs into game objects but does NOT link references
    /// Creates ID → Object mappings for later reference linking
    /// </summary>
    public class LoadDataPhase
    {
        #if DEBUG
        // 🔥 2026-03-18 诊断：记录宝物 DTO 加载次数（用于限制日志输出）
        private int _treasureDTODebugCount = 0;
        #endif
        
        /// <summary>
        /// Load GameScenario from GameScenarioDTO
        /// This is the main entry point for the Load Data Phase
        /// Creates all game objects but does NOT link references (that happens in Phase 3)
        /// </summary>
        /// <param name="dto">The DTO to load from</param>
        /// <param name="isNewScenario">True if loading a new scenario, false if loading a save file</param>
        public GameScenario LoadFromDTO(GameScenarioDTO dto, bool isNewScenario = false)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            
            var scenario = new GameScenario();
            
            // Basic properties
            scenario.ScenarioTitle = dto.ScenarioTitle;
            scenario.ScenarioDescription = dto.ScenarioDescription;
            scenario.MOD = dto.MOD;
            
            // Date information
            // 🔥 关键修复：确保 Date 对象存在且字段已初始化
            // 日期：2026-03-17
            // 原因：System.Text.Json 反序列化时不会调用构造函数，也不会应用字段默认值
            //       导致 Year/Month/Day 都是 0
            if (scenario.Date == null)
            {
                scenario.Date = new GameDate();
            }
            
            // 🔥 关键：从 DTO 加载日期数据（覆盖默认值）
            scenario.Date.Year = dto.Year;
            scenario.Date.Month = dto.Month;
            scenario.Date.Day = dto.Day;
            scenario.DaySince = dto.DaySince;
            
            // 🔥 调试日志：确认 Date 已正确加载
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] Date 加载完成: Year={scenario.Date.Year}, Month={scenario.Date.Month}, Day={scenario.Date.Day} (来自 DTO: Year={dto.Year}, Month={dto.Month}, Day={dto.Day})");
            #endif
            
            // Player information
            scenario.PlayerList = dto.PlayerList ?? new List<int>();
            scenario.CurrentPlayerID = dto.CurrentPlayerID;
            scenario.ControlMode = dto.ControlMode == (int)ScenarioControlMode.Observer ? ScenarioControlMode.Observer : ScenarioControlMode.Player;
            scenario.PlayerInfo = dto.PlayerInfo;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] PlayerList 加载: Count={scenario.PlayerList.Count}");
            if (scenario.PlayerList.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] PlayerList IDs: {string.Join(", ", scenario.PlayerList)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] ⚠️ PlayerList 为空!");
            }
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] CurrentPlayerID: {scenario.CurrentPlayerID ?? "null"}");
            #endif
            
            // Game time
            scenario.GameTime = dto.GameTime;
            
            // Configuration
            scenario.UsingOwnCommonData = dto.UsingOwnCommonData;
            
            // Relationship data (these are already ID-based, so just copy)
            scenario.FatherIds = dto.FatherIds ?? new Dictionary<int, int>();
            scenario.MotherIds = dto.MotherIds ?? new Dictionary<int, int>();
            scenario.SpouseIds = dto.SpouseIds ?? new Dictionary<int, int>();
            scenario.BrotherIds = dto.BrotherIds ?? new Dictionary<int, int[]>();
            scenario.SuoshuIds = dto.SuoshuIds ?? new Dictionary<int, int[]>();
            scenario.CloseIds = dto.CloseIds ?? new Dictionary<int, int[]>();
            scenario.HatedIds = dto.HatedIds ?? new Dictionary<int, int[]>();
            scenario.MarriageGranterId = dto.MarriageGranterId ?? new Dictionary<int, int>();
            
            // 🔥 关键修复：加载地图数据
            if (dto.ScenarioMap != null)
            {
                scenario.ScenarioMap = ConvertDTOToMap(dto.ScenarioMap);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[LoadDataPhase] ⚠️ ScenarioMap DTO 为 null，创建新地图");
                scenario.ScenarioMap = new global::GameObjects.Map();
            }
            
            // Convert PersonIDRelation
            if (dto.PersonRelationIds != null)
            {
                scenario.PersonRelationIds = dto.PersonRelationIds
                    .Select(r => new PersonIDRelation
                    {
                        PersonID1 = r.PersonID1,
                        PersonID2 = r.PersonID2,
                        Relation = r.Relation
                    })
                    .ToList();
            }
            
            // Load all game objects (but don't link references yet)
            LoadPersons(dto.Persons, scenario);
            LoadFactions(dto.Factions, scenario);
            LoadArchitectures(dto.Architectures, scenario);
            LoadTreasures(dto.Treasures, scenario);
            LoadLegions(dto.Legions, scenario);
            LoadTroops(dto.Troops, scenario);
            LoadSections(dto.Sections, scenario);
            LoadRegions(dto.Regions, scenario);
            LoadStates(dto.States, scenario);  // 🔥 修复：加载 States
            LoadRouteways(dto.Routeways, scenario);
            LoadMilitaries(dto.Militaries, scenario);
            LoadFacilities(dto.Facilities, scenario);
            LoadInformations(dto.Informations, scenario);
            LoadTroopEvents(dto.TroopEvents, scenario);
            LoadCaptives(dto.Captives, scenario);
            LoadEvents(dto.AllEvents, scenario);
            
            // 🔥 修复：加载Biography数据
            LoadBiographies(dto.Biographies, scenario);
            
            // 🔥 关键修复：初始化 MapTileData
            // MapTileData 不被序列化，需要在加载后重新初始化
            System.Diagnostics.Debug.WriteLine("[LoadDataPhase] 初始化 MapTileData...");
            scenario.InitializeMapData();
            System.Diagnostics.Debug.WriteLine("[LoadDataPhase] MapTileData 初始化完成");
            
            // 🔥 2026-03-17 移除：反向设置逻辑已废弃
            // 原因：所有剧本已通过 ScenarioFormatConverter 转换为新格式
            //       JSON 中已包含所有引用字段（LocationArchitectureID, BelongedFactionID 等）
            //       LoadPersonFromDTO 和 LoadArchitectureFromDTO 会直接加载这些字段
            //       LinkReferencesPhase 会根据这些 ID 链接对象引用
            // 结论：不再需要从容器对象（Architecture.PersonIDs）反向推断引用关系
            
            // 🔥 2026-03-17 移除：蜜月期初始化不能在这里执行
            // 原因：LoadDataPhase 只负责创建对象，此时 BelongedFaction 引用还未建立
            //       引用链接在 LinkReferencesPhase 中完成
            //       蜜月期初始化必须在 ProcessScenarioData 中执行（引用链接之后）
            // 结论：只在 ProcessScenarioData 中初始化蜜月期
            
            return scenario;
        }
        
        /// <summary>
        /// Load Person collection from PersonDTO collection
        /// Creates Person objects and stores ID references for later linking
        /// </summary>
        private void LoadPersons(List<PersonDTO> personDTOs, GameScenario scenario)
        {
            if (personDTOs == null || personDTOs.Count == 0)
                return;
            
            #if DEBUG
            int debugCounter = 0;
            const int maxDebugOutput = 5;
            #endif
            
            foreach (var dto in personDTOs)
            {
                #if DEBUG
                var person = LoadPersonFromDTO(dto, debugCounter < maxDebugOutput);
                debugCounter++;
                #else
                var person = LoadPersonFromDTO(dto);
                #endif
                
                if (person != null)
                {
                    scenario.Persons.Add(person);
                }
            }
            
            #if DEBUG
            if (debugCounter > maxDebugOutput)
            {
}
            #endif
        }
        
        /// <summary>
        /// Load a single Person from PersonDTO
        /// Creates the Person object and copies basic properties
        /// Stores ID references but does NOT set object references (that happens in Link Phase)
        /// </summary>
        public Person LoadPersonFromDTO(PersonDTO dto, bool enableDebugOutput = false)
        {
            if (dto == null)
                return null;
            

            
            var person = new Person();
            
            // 🔥 关键修复：先调用 Init() 初始化集合，再加载数据
            // 日期：2026-03-19
            // 问题：如果在加载 SkillIDs/StuntIDs/TitleIDs 之后调用 Init()，会清空这些集合
            // 解决：将 Init() 移到最前面，确保集合已初始化
            person.Init();
            
            // Basic properties
            person.ID = dto.ID;
            // person.Name = dto.Name;  // ❌ Name 是只读属性，通过 SurName + GivenName 计算
            person.SurName = dto.SurName;  // 🔥 修复：恢复姓氏
            person.GivenName = dto.GivenName;
            person.CalledName = dto.CalledName;
            person.Sex = dto.Sex;
            person.PCharacter = dto.PCharacter;
            person.Ideal = dto.Ideal;
            person.IdealTendencyIDString = dto.IdealTendencyIDString;  // 🔥 修复：恢复出仕志向考虑ID
            

            
            // Basic attributes (加载基础值)
            person.BaseCommand = dto.Command;
            person.BaseStrength = dto.Strength;
            person.BaseIntelligence = dto.Intelligence;
            person.BasePolitics = dto.Politics;
            person.BaseGlamour = dto.Glamour;
            person.Braveness = dto.Braveness;
            person.Calmness = dto.Calmness;
            person.Ambition = dto.Ambition;
            
            // 🔥 忠诚度相关字段
            // 向后兼容：如果旧存档有Loyalty字段，用它来设置PersonalLoyalty
            person.PersonalLoyalty = dto.PersonalLoyalty;
            person.TempLoyaltyChange = dto.TempLoyaltyChange;
            person.HoneymoonMonths = dto.HoneymoonMonths;
            
            // Experience values
            person.CommandExperience = (int)dto.CommandExperience;
            person.StrengthExperience = (int)dto.StrengthExperience;
            person.IntelligenceExperience = (int)dto.IntelligenceExperience;
            person.PoliticsExperience = (int)dto.PoliticsExperience;
            person.GlamourExperience = (int)dto.GlamourExperience;
            
            // 🔥 2026-02-18 修复：军事经验值
            person.BubingExperience = dto.BubingExperience;
            person.NubingExperience = dto.NubingExperience;
            person.QibingExperience = dto.QibingExperience;
            person.ShuijunExperience = dto.ShuijunExperience;
            person.QixieExperience = dto.QixieExperience;
            person.TacticsExperience = dto.TacticsExperience;
            person.StratagemExperience = dto.StratagemExperience;
            person.InternalExperience = dto.InternalExperience;
            
            // Status
            person.Status = (PersonStatus)dto.Status;
            person.Alive = dto.Alive;
            person.Available = dto.Available;
            person.Generation = dto.Generation;
            
            // 🔥 2026-02-18 修复：出场年、出生年、死亡年
            person.YearAvailable = dto.YearAvailable;
            person.YearBorn = dto.YearBorn;
            person.YearDead = dto.YearDead;
            
            // 🔥 2026-02-18 修复：基础属性
            person.Reputation = dto.Reputation;
            person.Fund = dto.Fund;
            person.Karma = dto.Karma;
            person.ArrivingDays = dto.ArrivingDays;
            person.AvailableLocation = dto.AvailableLocation;
            person.DeadReason = (PersonDeadReason)dto.DeadReason;
            
            // 🔥 2026-02-18 修复：能力潜力
            person.CommandPotential = dto.CommandPotential;
            person.StrengthPotential = dto.StrengthPotential;
            person.IntelligencePotential = dto.IntelligencePotential;
            person.PoliticsPotential = dto.PoliticsPotential;
            person.GlamourPotential = dto.GlamourPotential;
            
            // 🔥 2026-03-03 修复：训练方针ID
            person.TrainPolicyIDString = dto.TrainPolicyIDString;
            
            // 🔥 2026-02-18 修复：状态字段
            person.Tiredness = dto.Tiredness;
            person.InjureRate = dto.InjureRate;
            person.OfficerMerit = dto.OfficerMerit;
            person.WorkKind = (ArchitectureWorkKind)dto.WorkKind;
            person.OutsideTask = (OutsideTaskKind)dto.OutsideTask;
            person.OutsideDestination = dto.OutsideDestination;
            person.TaskDays = dto.TaskDays;
            person.Immortal = dto.Immortal;
            person.NvGuan = dto.NvGuan;
            person.IsGeneratedChildren = dto.IsGeneratedChildren;
            person.DaySinceAvailable = dto.DaySinceAvailable;
            
            // 🔥 2026-02-18 修复：统计数据
            person.YearJoin = dto.YearJoin;
            person.TroopDamageDealt = dto.TroopDamageDealt;
            person.TroopBeDamageDealt = dto.TroopBeDamageDealt;
            person.ArchitectureDamageDealt = dto.ArchitectureDamageDealt;
            person.RebelCount = dto.RebelCount;
            person.ExecuteCount = dto.ExecuteCount;
            person.OfficerKillCount = dto.OfficerKillCount;
            person.FleeCount = dto.FleeCount;
            person.HeldCaptiveCount = dto.HeldCaptiveCount;
            person.CaptiveCount = dto.CaptiveCount;
            person.StratagemSuccessCount = dto.StratagemSuccessCount;
            person.StratagemFailCount = dto.StratagemFailCount;
            person.StratagemBeSuccessCount = dto.StratagemBeSuccessCount;
            person.StratagemBeFailCount = dto.StratagemBeFailCount;
            person.RoutCount = dto.RoutCount;
            person.RoutedCount = dto.RoutedCount;
            
            // 🔥 2026-02-18 修复：人物特性
            person.BornRegion = (PersonBornRegion)dto.BornRegion;
            person.Strain = dto.Strain;
            person.Qualification = (PersonQualification)dto.Qualification;
            person.LeaderPossibility = dto.LeaderPossibility;
            person.StrategyTendency = (PersonStrategyTendency)dto.StrategyTendency;
            person.ValuationOnGovernment = (PersonValuationOnGovernment)dto.ValuationOnGovernment;
            person.ReturnedDaySince = dto.ReturnedDaySince;
            person.LastOutsideTask = (OutsideTaskKind)dto.LastOutsideTask;
            person.RewardFinished = dto.RewardFinished;
            person.WaitForFeiZiPeriod = dto.WaitForFeiZiPeriod;
            person.Tags = dto.Tags;
            
            // 🔥 2026-02-18 修复：额外字段
            person.JoinFactionID = dto.JoinFactionID ?? [];
            person.BattleSelfDamage = dto.BattleSelfDamage;
            person.NumberOfChildren = dto.NumberOfChildren;
            
            // Store ID references (do NOT set object references yet)
            person.BelongedFactionID = dto.BelongedFactionID;
            // BelongedFaction will be set in Link Phase
            
            person.BelongedCaptiveID = dto.BelongedCaptiveID;  // 🔥 新增：加载俘虏对象 ID（2026-03-07）
            // BelongedCaptive will be lazy-loaded in getter
            
            // 🔥 关键修复：设置位置引用 ID
            // 日期：2026-03-17
            // 问题：LoadPersonFromDTO 未设置 LocationArchitectureID 和 LocationTroopID
            //       导致 LinkReferencesPhase 无法链接 LocationArchitecture
            //       进而导致 CreatePersonStatusCache 缓存为空，Architecture.Persons.Count = 0
            // 解决：从 DTO 复制 LocationArchitectureID 和 LocationTroopID
            person.LocationArchitectureID = dto.LocationArchitectureID;
            person.LocationTroopID = dto.LocationTroopID;
            person.ConvincingPersonID = dto.ConvincingPersonID;
            RepairLegacyOutsideTaskState(person, enableDebugOutput);
            // LocationArchitecture、LocationTroop 和 ConvincingPerson 将在 Link Phase 中设置
            
            // Store collection IDs for later linking
            // 2026-03-18：所有剧本已转换为新格式，直接使用 IDs 字段
            person.TreasureIDs = dto.TreasureIDs ?? [];
            person.SkillIDs = dto.SkillIDs ?? [];
            person.StuntIDs = dto.StuntIDs ?? [];
            person.TitleIDs = dto.TitleIDs ?? [];
            
            #if DEBUG
            // 🔥 调试日志：追踪技能、特技、称号加载（仅前5个）
            if (enableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadPersonFromDTO] {person.Name}(ID:{person.ID}):");
                System.Diagnostics.Debug.WriteLine($"  - SkillIDs: [{string.Join(", ", person.SkillIDs)}]");
                System.Diagnostics.Debug.WriteLine($"  - StuntIDs: [{string.Join(", ", person.StuntIDs)}]");
                System.Diagnostics.Debug.WriteLine($"  - TitleIDs: [{string.Join(", ", person.TitleIDs)}]");
                System.Diagnostics.Debug.WriteLine($"  - Skills 集合状态: {(person.Skills == null ? "null" : $"已初始化，Count={person.Skills.Skills.Count}")}");
            }
            #endif
            
            // Private members
            person.InformationKindID = dto.InformationKindID;
            person.huaiyun = dto.Huaiyun;
            person.huaiyuntianshu = dto.HuaiyunTianshu;
            person.ManualStudy = dto.ManualStudy;
            person.ClosePersons = dto.ClosePersons ?? [];
            person.HatedPersons = dto.HatedPersons ?? [];
            
            // 🔥 修复：头像和列传
            person.PictureIndex = dto.PictureIndex;
            // person.PersonBiographyID = dto.PersonBiographyID;  // ❌ PersonBiographyID 是只读属性，从 PersonBiography.ID 计算
            // 🔥 关键修复：保存 PersonBiographyID 到临时字段，供 LinkReferencesPhase 使用
            // 日期：2026-03-17
            // 原因：PersonBiographyID 是只读属性，但我们需要在 LinkReferencesPhase 中链接 PersonBiography 对象
            //       使用 Person 类中已有的私有字段来存储（如果存在），或者通过 PersonBiography 延迟加载
            // 注意：Person.PersonBiography 字段是公开的，可以直接设置，PersonBiographyID 会自动计算
            // 因此我们需要在 LinkReferencesPhase 中通过 dto.PersonBiographyID 查找并设置 PersonBiography 对象
            
            // 🔥 新增：恢复能力增益字段（修复统治变成0的问题）
            person.IncrementOfAgricultureAbility = dto.IncrementOfAgricultureAbility;
            person.IncrementOfChallengeWinningChance = dto.IncrementOfChallengeWinningChance;
            person.IncrementOfCommerceAbility = dto.IncrementOfCommerceAbility;
            person.IncrementOfControversyWinningChance = dto.IncrementOfControversyWinningChance;
            person.IncrementOfDominationAbility = dto.IncrementOfDominationAbility;
            person.IncrementOfEnduranceAbility = dto.IncrementOfEnduranceAbility;
            person.IncrementOfMoraleAbility = dto.IncrementOfMoraleAbility;
            person.IncrementOfRecruitmentAbility = dto.IncrementOfRecruitmentAbility;
            person.IncrementOfSpyDays = dto.IncrementOfSpyDays;
            person.IncrementOfTechnologyAbility = dto.IncrementOfTechnologyAbility;
            person.IncrementOfTrainingAbility = dto.IncrementOfTrainingAbility;
            
            // 🔥 新增：恢复能力比例增益字段
            person.RadiusIncrementOfInformation = dto.RadiusIncrementOfInformation;
            person.RateIncrementOfAgricultureAbility = dto.RateIncrementOfAgricultureAbility;
            person.RateIncrementOfCommerceAbility = dto.RateIncrementOfCommerceAbility;
            person.RateIncrementOfConvince = dto.RateIncrementOfConvince;
            person.RateIncrementOfDestroy = dto.RateIncrementOfDestroy;
            person.RateIncrementOfDominationAbility = dto.RateIncrementOfDominationAbility;
            person.RateIncrementOfEnduranceAbility = dto.RateIncrementOfEnduranceAbility;
            person.RateIncrementOfGossip = dto.RateIncrementOfGossip;
            person.RateIncrementOfInstigate = dto.RateIncrementOfInstigate;
            person.RateIncrementOfJailBreakAbility = dto.RateIncrementOfJailBreakAbility;
            person.RateIncrementOfMoraleAbility = dto.RateIncrementOfMoraleAbility;
            person.RateIncrementOfRecruitmentAbility = dto.RateIncrementOfRecruitmentAbility;
            person.RateIncrementOfSearch = dto.RateIncrementOfSearch;
            person.RateIncrementOfTechnologyAbility = dto.RateIncrementOfTechnologyAbility;
            person.RateIncrementOfTrainingAbility = dto.RateIncrementOfTrainingAbility;
            
            // 🔥 新增：恢复能力倍数字段
            person.MultipleOfAgricultureReputation = dto.MultipleOfAgricultureReputation;
            person.MultipleOfAgricultureTechniquePoint = dto.MultipleOfAgricultureTechniquePoint;
            person.MultipleOfCommerceReputation = dto.MultipleOfCommerceReputation;
            person.MultipleOfCommerceTechniquePoint = dto.MultipleOfCommerceTechniquePoint;
            person.MultipleOfDominationReputation = dto.MultipleOfDominationReputation;
            person.MultipleOfDominationTechniquePoint = dto.MultipleOfDominationTechniquePoint;
            person.MultipleOfEnduranceReputation = dto.MultipleOfEnduranceReputation;
            person.MultipleOfEnduranceTechniquePoint = dto.MultipleOfEnduranceTechniquePoint;
            person.MultipleOfMoraleReputation = dto.MultipleOfMoraleReputation;
            person.MultipleOfMoraleTechniquePoint = dto.MultipleOfMoraleTechniquePoint;
            person.MultipleOfRecruitmentReputation = dto.MultipleOfRecruitmentReputation;
            person.MultipleOfRecruitmentTechniquePoint = dto.MultipleOfRecruitmentTechniquePoint;
            person.MultipleOfTechnologyReputation = dto.MultipleOfTechnologyReputation;
            person.MultipleOfTechnologyTechniquePoint = dto.MultipleOfTechnologyTechniquePoint;
            person.MultipleOfTrainingReputation = dto.MultipleOfTrainingReputation;
            person.MultipleOfTrainingTechniquePoint = dto.MultipleOfTrainingTechniquePoint;
            
            // 🔥 注意：person.Init() 已在方法开头调用，不要在这里重复调用！
            // 日期：2026-03-19
            // 原因：Init() 会清空 Skills、Stunts、RealTitles 集合
            
            return person;
        }

        private static void RepairLegacyOutsideTaskState(Person person, bool enableDebugOutput)
        {
            if (!RequiresOutsideDestination(person.OutsideTask) || person.OutsideDestination.HasValue)
            {
                return;
            }

            #if DEBUG
            if (enableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LoadPersonFromDTO] Detected legacy outside task without destination for {person.Name}(ID:{person.ID}), " +
                    $"task={person.OutsideTask}. Resetting the invalid task state.");
            }
            #endif

            person.OutsideTask = OutsideTaskKind.无;
            person.LastOutsideTask = OutsideTaskKind.无;
            person.OutsideDestination = null;
            person.TaskDays = 0;
            person.ArrivingDays = 0;
            person.WorkKind = ArchitectureWorkKind.无;
            person.Status = PersonStatus.Normal;
            person.ConvincingPersonID = -1;
        }

        private static bool RequiresOutsideDestination(OutsideTaskKind task)
        {
            return task is OutsideTaskKind.说服
                or OutsideTaskKind.情报
                or OutsideTaskKind.破坏
                or OutsideTaskKind.煽动
                or OutsideTaskKind.流言
                or OutsideTaskKind.劫狱
                or OutsideTaskKind.暗杀
                or OutsideTaskKind.劝降
                or OutsideTaskKind.割地
                or OutsideTaskKind.亲善
                or OutsideTaskKind.结盟
                or OutsideTaskKind.停战;
        }
        
        /// <summary>
        /// Load Faction collection from FactionDTO collection
        /// </summary>
        private void LoadFactions(List<FactionDTO> factionDTOs, GameScenario scenario)
        {
            if (factionDTOs == null)
            {
                throw new InvalidOperationException(
                    "存档数据损坏：Factions 列表为 null。" +
                    "这说明 JSON 反序列化失败，或者存档文件中缺少 'Factions' 字段。");
            }
            
            if (factionDTOs.Count == 0)
            {
                throw new InvalidOperationException(
                    "存档数据损坏：Factions 列表为空。" +
                    "存档文件中没有任何势力数据，无法继续游戏。");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] 开始加载 {factionDTOs.Count} 个势力");
            
            foreach (var dto in factionDTOs)
            {
                var faction = LoadFactionFromDTO(dto);
                if (faction != null)
                {
                    scenario.Factions.Add(faction);
                    System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] 加载势力: ID={faction.ID}, Name={faction.Name}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] ✅ 势力加载完成，共 {scenario.Factions.Count} 个");
        }
        
        /// <summary>
        /// Load a single Faction from FactionDTO
        /// Creates the Faction object and stores ID references for later linking
        /// </summary>
        public Faction LoadFactionFromDTO(FactionDTO dto)
        {
            if (dto == null)
                return null;
            
            var faction = new Faction();
            
            // Basic properties
            faction.ID = dto.ID;
            faction.Name = dto.Name;
            faction.ColorIndex = dto.ColorIndex;
            
            // Store ID references (do NOT set object references yet)
            faction.LeaderID = dto.LeaderID;
            faction.AdvisorID = dto.AdvisorID;  // 🔥 恢复军师ID（2026-03-18）
            faction.CapitalID = dto.CapitalID;
            
            // Store collection IDs for later linking
            // Support backward compatibility: prefer List<int> over String
            // Note: Faction class doesn't have string properties for these collections anymore
            // They were removed in favor of List<int> properties only
            // 🔥 关键修复：旧剧本用空格分隔，不是逗号
            // 日期：2026-03-16
            static List<int> ParseIDString(string s)
            {
                if (string.IsNullOrEmpty(s)) return [];
                var result = new List<int>();
                foreach (var part in s.Split([' ', ',', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(part, out int id))
                        result.Add(id);
                return result;
            }
            
            faction.ArchitectureIDs = (dto.ArchitectureIDs?.Count > 0)
                ? dto.ArchitectureIDs
                : ParseIDString(dto.ArchitecturesString);
            
            faction.PersonIDs = (dto.PersonIDs?.Count > 0)
                ? dto.PersonIDs
                : ParseIDString(dto.PersonsString);
            
            faction.MilitaryIDs = (dto.MilitaryIDs?.Count > 0)
                ? dto.MilitaryIDs
                : ParseIDString(dto.MilitariesString);
            
            faction.LegionIDs = (dto.LegionIDs?.Count > 0)
                ? dto.LegionIDs
                : ParseIDString(dto.LegionsString);
            
            faction.TroopIDs = (dto.TroopIDs?.Count > 0)
                ? dto.TroopIDs
                : ParseIDString(dto.TroopsString);
            
            faction.SectionIDs = (dto.SectionIDs?.Count > 0)
                ? dto.SectionIDs
                : ParseIDString(dto.SectionsString);
            
            // 🔥 根本修复：加载 AvailableTechniquesString
            // 日期：2026-02-17
            // 问题：读档后势力技巧不显示为已拥有状态
            // 原因：LoadDataPhase 使用了不存在的 TechniqueIDs 字段，应该使用 AvailableTechniquesString
            faction.AvailableTechniquesString = dto.AvailableTechniquesString ?? string.Empty;
            
            // 🔥 根本修复：加载 BaseMilitaryKindsString
            // 日期：2026-03-18
            // 问题：新开剧本后势力兵种丢失，导致"新编"按钮灰色
            // 原因：LoadDataPhase 忘记从 DTO 复制 BaseMilitaryKindsString 到 Faction 对象
            faction.BaseMilitaryKindsString = dto.BaseMilitaryKindsString ?? string.Empty;
            
            // 🔥 根本修复：加载 InformationsString
            // 日期：2026-03-20
            // 问题：读档后势力情报数据丢失（已探索区域、已知敌军位置）
            // 原因：LoadDataPhase 忘记从 DTO 复制 InformationsString 到 Faction 对象
            faction.InformationsString = dto.InformationsString ?? string.Empty;
            
            // 🔥 根本修复：加载 TransferingMilitariesString
            // 日期：2026-03-20
            // 问题：读档后运输中的部队丢失
            // 原因：LoadDataPhase 忘记从 DTO 复制 TransferingMilitariesString 到 Faction 对象
            faction.TransferingMilitariesString = dto.TransferingMilitariesString ?? string.Empty;
            
            // 🔥 根本修复：加载 RoutewaysString
            // 日期：2026-03-20
            // 问题：读档后运输路线丢失
            // 原因：LoadDataPhase 忘记从 DTO 复制 RoutewaysString 到 Faction 对象
            faction.RoutewaysString = dto.RoutewaysString ?? string.Empty;
            
            // 🔥 根本修复：加载 GetGeneratorPersonCountString
            // 日期：2026-03-20
            faction.GetGeneratorPersonCountString = dto.GetGeneratorPersonCountString ?? string.Empty;
            
            // 🔥 根本修复：加载技巧研究相关字段
            // 日期：2026-03-20
            faction.PlanTechniqueString = dto.PlanTechniqueString;
            faction.PreferredTechniqueKinds = dto.PreferredTechniqueKinds ?? [];
            
            // 🔥 根本修复：加载地图残差字段
            // 日期：2026-03-20
            faction.SecondTierXResidue = dto.SecondTierXResidue;
            faction.SecondTierYResidue = dto.SecondTierYResidue;
            faction.ThirdTierXResidue = dto.ThirdTierXResidue;
            faction.ThirdTierYResidue = dto.ThirdTierYResidue;
            
            // 🔥 根本修复：加载军师建议系统字段
            // 日期：2026-03-20
            faction.CurrentRoundSuggestion = (AdvisorSuggestionKind)dto.CurrentRoundSuggestion;
            faction.LastSuggestionCheckTurn = dto.LastSuggestionCheckTurn;
            faction.LastTalentRecommendYear = dto.LastTalentRecommendYear;
            
            // Faction state
            faction.Reputation = dto.Reputation;
            faction.TechniquePoint = dto.TechniquePoint;
            faction.IsAlien = dto.IsAlien;
            
            // 🔥 根本修复：恢复势力官爵和朝廷贡献度
            // 日期：2026-03-13
            faction.guanjue = dto.Guanjue;
            faction.chaotinggongxiandu = dto.Chaotinggongxiandu;
            
            // Private members
            faction.PrinceID = dto.PrinceID;
            faction.ZhaoxianFailureCount = dto.ZhaoxianFailureCount;
            faction.YearOfficialLimit = dto.YearOfficialLimit;
            
            // 🔥 新增：恢复势力增益字段
            faction.IncrementOfAntiCriticalStrikeChance = dto.IncrementOfAntiCriticalStrikeChance;
            faction.IncrementOfChaosDaysAfterPhisicalAttack = dto.IncrementOfChaosDaysAfterPhisicalAttack;
            faction.IncrementOfCombativityCeiling = dto.IncrementOfCombativityCeiling;
            faction.IncrementOfCriticalStrikeChance = dto.IncrementOfCriticalStrikeChance;
            faction.IncrementOfResistStratagemChance = dto.IncrementOfResistStratagemChance;
            faction.IncrementOfRoutewayRadius = dto.IncrementOfRoutewayRadius;
            faction.IncrementOfRoutewayWorkforce = dto.IncrementOfRoutewayWorkforce;
            faction.IncrementOfStratagemSuccessChance = dto.IncrementOfStratagemSuccessChance;
            faction.IncrementOfViewRadius = dto.IncrementOfViewRadius;
            
            faction.RateIncrementOfTerrainRate = dto.RateIncrementOfTerrainRate;
            faction.RateOfCombativityRecoveryAfterAttacked = dto.RateOfCombativityRecoveryAfterAttacked;
            faction.RateOfCombativityRecoveryAfterStratagemFail = dto.RateOfCombativityRecoveryAfterStratagemFail;
            faction.RateOfCombativityRecoveryAfterStratagemSuccess = dto.RateOfCombativityRecoveryAfterStratagemSuccess;
            faction.RateOfFoodTransportBetweenArchitectures = dto.RateOfFoodTransportBetweenArchitectures;
            faction.RateOfRoutewayConsumption = dto.RateOfRoutewayConsumption;
            
            return faction;
        }
        
        /// <summary>
        /// Load Architecture collection from ArchitectureDTO collection
        /// Handles polymorphic types (City, Port, Gate)
        /// </summary>
        private void LoadArchitectures(List<ArchitectureDTO> architectureDTOs, GameScenario scenario)
        {
            if (architectureDTOs == null || architectureDTOs.Count == 0)
                return;
            
            foreach (var dto in architectureDTOs)
            {
                var architecture = LoadArchitectureFromDTO(dto);
                if (architecture != null)
                {
                    scenario.Architectures.Add(architecture);
                }
            }
        }
        
        /// <summary>
        /// Load a single Architecture from ArchitectureDTO
        /// Handles polymorphic deserialization (City, Port, Gate)
        /// Creates the correct derived type based on DTO type
        /// </summary>
        public Architecture LoadArchitectureFromDTO(ArchitectureDTO dto)
        {
            if (dto == null)
                return null;

            ArchitectureKind kind = ResolveArchitectureKindOrThrow(dto);
            dto = NormalizeArchitectureDtoCompatibility(dto, kind);
            ValidateArchitectureDtoTypeConsistency(dto, kind);
            
            Architecture architecture;
            
            // Polymorphic instantiation based on DTO type
            if (dto is CityDTO cityDTO)
            {
                var city = new Architecture(); // In the actual game, City might be a derived type
                // Copy City-specific properties
                // city.DevelopmentLevel = cityDTO.DevelopmentLevel;
                architecture = city;
            }
            else if (dto is PortDTO portDTO)
            {
                var port = new Architecture(); // In the actual game, Port might be a derived type
                // Copy Port-specific properties
                // port.ShipCapacity = portDTO.ShipCapacity;
                architecture = port;
            }
            else if (dto is GateDTO gateDTO)
            {
                var gate = new Architecture(); // In the actual game, Gate might be a derived type
                // Copy Gate-specific properties
                // gate.DefenseBonus = gateDTO.DefenseBonus;
                architecture = gate;
            }
            else
            {
                // Base Architecture type
                architecture = new Architecture();
            }
            
            // Copy base properties
            architecture.ID = dto.ID;
            architecture.Name = dto.Name;
            architecture.CaptionID = dto.CaptionID;
            architecture.Kind = kind;
            architecture.KindID = dto.KindID;
            
            // Location
            architecture.AreaX = dto.AreaX;
            architecture.AreaY = dto.AreaY;
            architecture.AreaWidth = dto.AreaWidth;
            architecture.AreaHeight = dto.AreaHeight;
            
            // 🔥 关键修复：恢复 ArchitectureAreaString
            architecture.ArchitectureAreaString = dto.ArchitectureAreaString;
            
            // Store ID references (do NOT set object references yet)
            architecture.BelongedFactionID = dto.BelongedFactionID;
            architecture.BelongedSectionID = dto.BelongedSectionID;
            architecture.MayorID = dto.MayorID;
            architecture.StateID = dto.StateID;  // 🔥 修复：恢复 StateID
            architecture.huangdisuozai = dto.huangdisuozai;
            
            // Resources
            architecture.Agriculture = dto.Agriculture;
            architecture.Commerce = dto.Commerce;
            architecture.Technology = dto.Technology;
            architecture.Morale = dto.Morale;
            architecture.Endurance = dto.Endurance;
            
            architecture.Domination = dto.Domination;  // 🔥 修复：恢复统治度
            
            architecture.Population = dto.Population;
            architecture.MilitaryPopulation = dto.MilitaryPopulation;  // 🔥 修复：恢复兵役人口
            architecture.Fund = dto.Fund;
            architecture.Food = dto.Food;
            
            // Store collection IDs for later linking
            // 🔥 修复：使用空格分隔符，而不是逗号
            char[] separator = new char[] { ' ', '\n', '\r', '\t', ',' };
            
            if (dto.PersonIDs != null && dto.PersonIDs.Count > 0)
            {
                architecture.PersonIDs = dto.PersonIDs;
                // 🔥 关键修复：同时复制字符串字段
                architecture.PersonsString = dto.PersonsString;
            }
            else if (!string.IsNullOrEmpty(dto.PersonsString))
            {
                architecture.PersonIDs = dto.PersonsString
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
                // 🔥 同时保存字符串字段
                architecture.PersonsString = dto.PersonsString;
            }
            else
            {
                architecture.PersonIDs = [];
                architecture.PersonsString = null;
            }
            
            if (dto.MilitaryIDs != null && dto.MilitaryIDs.Count > 0)
            {
                // 🔥 关键修复：如果MilitaryIDs全是负数（无效ID），忽略它，改用MilitariesString
                // 注意：Military ID=0 是有效的（步兵队），所以不能把0当成无效
                bool allZero = dto.MilitaryIDs.All(id => id < 0);
                
                /*#if DEBUG
                if (allZero)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ [LoadArchitectureFromDTO] 建筑 {architecture.Name}(ID:{architecture.ID}) 的 MilitaryIDs 全是0（无效），尝试使用 MilitariesString");
                    System.Diagnostics.Debug.WriteLine($"   MilitaryIDs: [{string.Join(", ", dto.MilitaryIDs)}]");
                    System.Diagnostics.Debug.WriteLine($"   MilitariesString: '{dto.MilitariesString ?? "null"}'");
                }
                #endif*/
                
                if (!allZero)
                {
                    architecture.MilitaryIDs = dto.MilitaryIDs;
                    // 🔥 关键修复：同时复制字符串字段
                    architecture.MilitariesString = dto.MilitariesString;
                }
                else if (!string.IsNullOrEmpty(dto.MilitariesString))
                {
                    // MilitaryIDs无效，使用MilitariesString
                    architecture.MilitaryIDs = dto.MilitariesString
                        .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();
                    architecture.MilitariesString = dto.MilitariesString;
                    
                    /*#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"   ✅ 从 MilitariesString 恢复了 {architecture.MilitaryIDs.Count} 个编队ID: [{string.Join(", ", architecture.MilitaryIDs)}]");
                    #endif*/
                }
                else
                {
                    // MilitaryIDs无效且MilitariesString也为空，设置为空列表
                    architecture.MilitaryIDs = [];
                    architecture.MilitariesString = null;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"   ⚠️ MilitaryIDs无效且MilitariesString为空，无法恢复编队");
                    #endif
                }
            }
            else if (!string.IsNullOrEmpty(dto.MilitariesString))
            {
                architecture.MilitaryIDs = dto.MilitariesString
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
                // 🔥 同时保存字符串字段
                architecture.MilitariesString = dto.MilitariesString;
                
                /*#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadArchitectureFromDTO] 建筑 {architecture.Name}(ID:{architecture.ID}) 从 MilitariesString 恢复了 {architecture.MilitaryIDs.Count} 个编队ID");
                #endif*/
            }
            else
            {
                architecture.MilitaryIDs = [];
                architecture.MilitariesString = null;
            }
            
            if (dto.FacilityIDs != null && dto.FacilityIDs.Count > 0)
            {
                architecture.FacilityIDs = dto.FacilityIDs;
                // 🔥 关键修复：同时复制字符串字段
                architecture.FacilitiesString = dto.FacilitiesString;
                
                /*#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadArchitectureFromDTO] 建筑 {architecture.Name}(ID:{architecture.ID}) 从 FacilityIDs 恢复了 {architecture.FacilityIDs.Count} 个设施ID");
                #endif*/
            }
            else if (!string.IsNullOrEmpty(dto.FacilitiesString))
            {
                architecture.FacilityIDs = dto.FacilitiesString
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
                // 🔥 同时保存字符串字段
                architecture.FacilitiesString = dto.FacilitiesString;
                
                /*#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadArchitectureFromDTO] 建筑 {architecture.Name}(ID:{architecture.ID}) 从 FacilitiesString '{dto.FacilitiesString}' 恢复了 {architecture.FacilityIDs.Count} 个设施ID: [{string.Join(", ", architecture.FacilityIDs)}]");
                #endif*/
            }
            else
            {
                architecture.FacilityIDs = [];
                architecture.FacilitiesString = null;
            }
            
            // Private members
            architecture.AutoHiring = dto.AutoHiring;
            architecture.AutoRewarding = dto.AutoRewarding;
            architecture.AutoSearching = dto.AutoSearching;
            architecture.AutoWorking = dto.AutoWorking;
            
            // 🔥 V15 修复：从 DTO 恢复 FacilityEnabled 状态
            // 优先使用 DTO 的值；如果 DTO 为 false 但有设施，则启用（向后兼容旧存档）
            architecture.FacilityEnabled = dto.FacilityEnabled 
                || (dto.FacilityIDs?.Count > 0) 
                || !string.IsNullOrEmpty(dto.FacilitiesString);
            
            // 🔥 修复：恢复 CharacteristicsString，避免读档后特色丢失
            // 日期：2026-02-16
            // 问题：LoadDataPhase 缺少 CharacteristicsString 的恢复，导致读档后特色数据丢失
            architecture.CharacteristicsString = dto.CharacteristicsString ?? string.Empty;
            
            // 🔥 根本修复：恢复 AI 链接数据，避免每次读档重新生成（耗时54秒）
            // 日期：2026-02-15
            // 问题：LoadDataPhase 缺少这两个字段的恢复，导致读档后链接数据丢失
            architecture.AILandLinksString = dto.AILandLinksString;
            architecture.AIWaterLinksString = dto.AIWaterLinksString;
            
            // 🔥 完整性修复：恢复所有缺失的字符串字段（2026-02-16）
            architecture.FundPacksString = dto.FundPacksString;
            architecture.FoodPacksString = dto.FoodPacksString;
            architecture.InformationsString = dto.InformationsString;
            architecture.PopulationPacksString = dto.PopulationPacksString;
            architecture.MilitaryPopulationPacksString = dto.MilitaryPopulationPacksString;
            architecture.CaptivesString = dto.CaptivesString;
            architecture.MovingPersonsString = dto.MovingPersonsString;
            architecture.NoFactionPersonsString = dto.NoFactionPersonsString;
            architecture.NoFactionMovingPersonsString = dto.NoFactionMovingPersonsString;
            architecture.feiziliebiaoString = dto.feiziliebiaoString;
            
            // 🔥 关键修复：不要恢复增益字段！
            // 原因：AfterLoadSaveFile() 会调用 ApplyInfluences() 重新计算所有增益
            // 如果这里恢复了旧值，ApplyInfluences() 会在旧值基础上累加，导致增益错误
            // 
            // 错误做法（已移除）：
            // architecture.IncrementOfDominationCeiling = dto.IncrementOfDominationCeiling;
            // architecture.IncrementOfFacilityPositionCount = dto.IncrementOfFacilityPositionCount;
            // 
            // 正确做法：让这些字段保持初始值 0，由 ApplyInfluences() 重新计算
            
            return architecture;
        }

        private static ArchitectureKind ResolveArchitectureKindOrThrow(ArchitectureDTO dto)
        {
            ArchitectureKind kind =
                global::GameManager.Session.Current?.Scenario?.GameCommonData?.AllArchitectureKinds?.GetArchitectureKind(dto.KindID) ??
                CommonData.Current?.AllArchitectureKinds?.GetArchitectureKind(dto.KindID);

            if (kind == null)
            {
                throw new InvalidOperationException(
                    $"[LoadDataPhase] Architecture DTO {dto.ID} ({dto.Name}) references unknown KindID={dto.KindID}.");
            }

            return kind;
        }

        private static ArchitectureDTO NormalizeArchitectureDtoCompatibility(ArchitectureDTO dto, ArchitectureKind kind)
        {
            if (dto is CityDTO or PortDTO or GateDTO)
            {
                return dto;
            }

            if (dto.GetType() != typeof(ArchitectureDTO))
            {
                return dto;
            }

            return ResolveArchitectureDtoCategory(kind, dto) switch
            {
                ArchitectureDtoCategory.City => CreateCompatibleCityDto(dto),
                ArchitectureDtoCategory.Port => CreateCompatiblePortDto(dto),
                ArchitectureDtoCategory.Gate => CreateCompatibleGateDto(dto),
                _ => dto
            };
        }

        private static CityDTO CreateCompatibleCityDto(ArchitectureDTO source)
        {
            return CopyArchitectureDto(source, new CityDTO
            {
                DevelopmentLevel = source.Agriculture + source.Commerce + source.Technology
            });
        }

        private static PortDTO CreateCompatiblePortDto(ArchitectureDTO source)
        {
            return CopyArchitectureDto(source, new PortDTO
            {
                ShipCapacity = 100
            });
        }

        private static GateDTO CreateCompatibleGateDto(ArchitectureDTO source)
        {
            return CopyArchitectureDto(source, new GateDTO
            {
                DefenseBonus = source.Endurance / 10
            });
        }

        private static TDto CopyArchitectureDto<TDto>(ArchitectureDTO source, TDto target)
            where TDto : ArchitectureDTO
        {
            target.ID = source.ID;
            target.Name = source.Name;
            target.CaptionID = source.CaptionID;
            target.KindID = source.KindID;
            target.AreaX = source.AreaX;
            target.AreaY = source.AreaY;
            target.AreaWidth = source.AreaWidth;
            target.AreaHeight = source.AreaHeight;
            target.ArchitectureAreaString = source.ArchitectureAreaString;
            target.BelongedFactionID = source.BelongedFactionID;
            target.BelongedSectionID = source.BelongedSectionID;
            target.MayorID = source.MayorID;
            target.StateID = source.StateID;
            target.huangdisuozai = source.huangdisuozai;
            target.Agriculture = source.Agriculture;
            target.Commerce = source.Commerce;
            target.Technology = source.Technology;
            target.Morale = source.Morale;
            target.Endurance = source.Endurance;
            target.Domination = source.Domination;
            target.Population = source.Population;
            target.MilitaryPopulation = source.MilitaryPopulation;
            target.Fund = source.Fund;
            target.Food = source.Food;
            target.PersonIDs = source.PersonIDs;
            target.MilitaryIDs = source.MilitaryIDs;
            target.FacilityIDs = source.FacilityIDs;
            target.PersonsString = source.PersonsString;
            target.MilitariesString = source.MilitariesString;
            target.FacilitiesString = source.FacilitiesString;
            target.CharacteristicsString = source.CharacteristicsString;
            target.AILandLinksString = source.AILandLinksString;
            target.AIWaterLinksString = source.AIWaterLinksString;
            target.FundPacksString = source.FundPacksString;
            target.FoodPacksString = source.FoodPacksString;
            target.InformationsString = source.InformationsString;
            target.PopulationPacksString = source.PopulationPacksString;
            target.MilitaryPopulationPacksString = source.MilitaryPopulationPacksString;
            target.CaptivesString = source.CaptivesString;
            target.MovingPersonsString = source.MovingPersonsString;
            target.NoFactionPersonsString = source.NoFactionPersonsString;
            target.NoFactionMovingPersonsString = source.NoFactionMovingPersonsString;
            target.feiziliebiaoString = source.feiziliebiaoString;
            target.AutoHiring = source.AutoHiring;
            target.AutoRewarding = source.AutoRewarding;
            target.AutoSearching = source.AutoSearching;
            target.AutoWorking = source.AutoWorking;
            target.FacilityEnabled = source.FacilityEnabled;
            target.ExtensionData = source.ExtensionData;
            return target;
        }

        private static void ValidateArchitectureDtoTypeConsistency(ArchitectureDTO dto, ArchitectureKind kind)
        {
            ArchitectureDtoCategory expected = ResolveArchitectureDtoCategory(kind, dto);
            bool matches = expected switch
            {
                ArchitectureDtoCategory.City => dto is CityDTO,
                ArchitectureDtoCategory.Port => dto is PortDTO,
                ArchitectureDtoCategory.Gate => dto is GateDTO,
                _ => false
            };

            if (!matches)
            {
                throw new InvalidOperationException(
                    $"[LoadDataPhase] Architecture DTO type mismatch for {dto.ID} ({dto.Name}): KindID={dto.KindID}, KindName={kind.Name}, expected {expected} DTO, actual {dto.GetType().Name}.");
            }
        }

        private static ArchitectureDtoCategory ResolveArchitectureDtoCategory(ArchitectureKind kind, ArchitectureDTO dto)
        {
            string kindName = kind.Name ?? string.Empty;
            if (kindName.Contains("城", StringComparison.Ordinal) ||
                kindName.Contains("City", StringComparison.OrdinalIgnoreCase))
            {
                return ArchitectureDtoCategory.City;
            }

            if (kindName.Contains("港", StringComparison.Ordinal) ||
                kindName.Contains("Port", StringComparison.OrdinalIgnoreCase))
            {
                return ArchitectureDtoCategory.Port;
            }

            if (kindName.Contains("关", StringComparison.Ordinal) ||
                kindName.Contains("Gate", StringComparison.OrdinalIgnoreCase))
            {
                return ArchitectureDtoCategory.Gate;
            }

            if (kind.HasHarbor)
            {
                return ArchitectureDtoCategory.Port;
            }

            if (kind.HasPopulation)
            {
                return ArchitectureDtoCategory.City;
            }

            throw new InvalidOperationException(
                $"[LoadDataPhase] Architecture Kind {kind.ID} ({kind.Name}) for DTO {dto.ID} ({dto.Name}) cannot be classified to City/Port/Gate.");
        }

        private enum ArchitectureDtoCategory
        {
            City,
            Port,
            Gate
        }
        
        /// <summary>
        /// Load Legion collection from LegionDTO collection
        /// </summary>
        private void LoadLegions(List<LegionDTO> legionDTOs, GameScenario scenario)
        {
            if (legionDTOs == null || legionDTOs.Count == 0)
                return;
            
            foreach (var dto in legionDTOs)
            {
                var legion = LoadLegionFromDTO(dto);
                if (legion != null)
                {
                    scenario.Legions.Add(legion);
                }
            }
        }
        
        /// <summary>
        /// Load a single Legion from LegionDTO
        /// </summary>
        public Legion LoadLegionFromDTO(LegionDTO dto)
        {
            if (dto == null)
                return null;
            
            var legion = new Legion();
            
            // Basic properties
            legion.ID = dto.ID;
            legion.Name = dto.Name;
            
            // Store ID references
            legion.BelongedFactionID = dto.BelongedFactionID;
            legion.LeaderID = dto.LeaderID;
            legion.Kind = dto.Kind;
            legion.Mission = dto.Mission;
            legion.StartArchitectureString = dto.StartArchitectureID;
            legion.WillArchitectureString = dto.WillArchitectureID;
            legion.TargetArchitectureID = dto.TargetArchitectureID;

            // Recover Kind for legacy saves where Kind field is absent and defaults to AI.
            if (TryInferKindFromLegionName(legion.Name, out LegionKind inferredKind) &&
                legion.Kind != inferredKind)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Recover] DTO kind inferred by name: {legion.Name}(ID:{legion.ID}) {legion.Kind} -> {inferredKind}");
                legion.Kind = inferredKind;
            }

            if (legion.Kind == LegionKind.AI && legion.Mission == LegionMission.None)
            {
                if (TryInferMissionFromLegionName(legion.Name, out LegionMission inferred))
                {
                    legion.Mission = inferred;
                    System.Diagnostics.Debug.WriteLine(
                        $"[LegionLoad][Recover] DTO mission inferred by name: {legion.Name}(ID:{legion.ID}) -> {legion.Mission}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[LegionLoad][Warn] DTO AI legion has Mission=None: {legion.Name}(ID:{legion.ID}), " +
                        $"TargetArchitectureID={legion.TargetArchitectureID}, WillArchitectureID={legion.WillArchitectureString}");
                }
            }
            
            // Store collection IDs for later linking
            if (dto.TroopIDs != null && dto.TroopIDs.Count > 0)
            {
                legion.TroopIDs = dto.TroopIDs;
            }
            else if (!string.IsNullOrEmpty(dto.TroopsString))
            {
                legion.TroopIDs = dto.TroopsString
                    .Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(int.Parse)
                    .ToList();
            }
            else
            {
                legion.TroopIDs = new List<int>();
            }
            
            return legion;
        }

        private static bool TryInferKindFromLegionName(string legionName, out LegionKind kind)
        {
            kind = LegionKind.AI;
            if (string.IsNullOrEmpty(legionName))
            {
                return false;
            }

            if (legionName.StartsWith("Player_", StringComparison.OrdinalIgnoreCase))
            {
                kind = LegionKind.Player;
                return true;
            }

            if (legionName.StartsWith("AI_", StringComparison.OrdinalIgnoreCase))
            {
                kind = LegionKind.AI;
                return true;
            }

            return false;
        }

        private static bool TryInferMissionFromLegionName(string legionName, out LegionMission mission)
        {
            mission = LegionMission.None;
            if (string.IsNullOrEmpty(legionName))
            {
                return false;
            }

            if (legionName.Contains("_守_") || legionName.Contains("Defend", StringComparison.OrdinalIgnoreCase))
            {
                mission = LegionMission.Defend;
                return true;
            }

            if (legionName.Contains("_攻_") || legionName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
            {
                mission = LegionMission.Attack;
                return true;
            }

            if (legionName.Contains("_退_") || legionName.Contains("Retreat", StringComparison.OrdinalIgnoreCase))
            {
                mission = LegionMission.Retreat;
                return true;
            }

            if (legionName.Contains("_巡_") || legionName.Contains("Patrol", StringComparison.OrdinalIgnoreCase))
            {
                mission = LegionMission.Patrol;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Load Troop collection from TroopDTO collection
        /// </summary>
        private void LoadTroops(List<TroopDTO> troopDTOs, GameScenario scenario)
        {
            if (troopDTOs == null || troopDTOs.Count == 0)
                return;
            
            foreach (var dto in troopDTOs)
            {
                var troop = LoadTroopFromDTO(dto);
                if (troop != null)
                {
                    scenario.Troops.Add(troop);
                }
            }
        }
        
        /// <summary>
        /// Load a single Troop from TroopDTO
        /// </summary>
        public Troop LoadTroopFromDTO(TroopDTO dto)
        {
            if (dto == null)
                return null;
            
            var troop = new Troop();
            
            // Basic properties
            troop.ID = dto.ID;
            troop.Name = dto.Name;
            troop.Status = (TroopStatus)dto.Status;
            
            // 🔥 根本修复：读档阶段直接设置字段，避免触发 Position setter
            // 日期：2026-03-12
            // 问题：Position setter 会调用 AtomicSetTroopPosition → CanTroopEnterPosition
            //       但此时 Army 尚未加载（Militaries 可能还未完全加载），导致 NullReferenceException
            // 解决：直接设置 internal 字段 position，跳过 setter 的验证逻辑
            //       等 LinkReferencesPhase 完成后，所有引用都会被正确链接
            // 
            // PositionX/Y 是独立属性，不会自动更新 Position 字段
            troop.PositionX = dto.PositionX;
            troop.PositionY = dto.PositionY;
            troop.position = new Point(dto.PositionX, dto.PositionY);  // 直接设置字段
            
            // 🔥 同步设置 PreviousPosition，避免首次移动时的异常
            troop.PreviousPosition = troop.position;
            troop.RealDestination = new Point(dto.RealDestinationX, dto.RealDestinationY);
            
            // Store ID references
            troop.BelongedFactionID = dto.BelongedFactionID;
            troop.BelongedLegionID = dto.BelongedLegionID;
            troop.BelongedArchitectureID = dto.BelongedArchitectureID;
            troop.StartingArchitectureID = dto.StartingArchitectureID;  // 🔥 修复：恢复出发城市ID
            troop.WillArchitectureID = dto.WillArchitectureID;
            troop.WillTroopID = dto.WillTroopID;
            troop.TargetArchitectureID = dto.TargetArchitectureID;
            troop.TargetTroopID = dto.TargetTroopID;
            troop.LeaderID = dto.LeaderID;
            troop.MilitaryID = dto.MilitaryID;
            
            // 🔥 根本修复：恢复控制状态，防止读档后AI错误控制玩家部队
            troop.Auto = dto.Auto;
            troop.Controllable = dto.Controllable;
            troop.ManualControl = dto.ManualControl;
            
            // 🔥 根本修复：恢复AI状态，防止读档后部队状态错误
            troop.CurrentAIState = (TroopAIState)dto.CurrentAIState;
            troop.IsRetreatLocked = dto.IsRetreatLocked;
            troop.CreationTurn = dto.CreationTurn;
            
            // 🔥 根本修复：恢复角色，防止读档后战术角色丢失
            troop.AssignedRole = dto.AssignedRole.HasValue ? (WorldOfTheThreeKingdoms.GameGlobal.TroopRole?)dto.AssignedRole.Value : null;
            // CurrentRole 是计算属性，不需要直接设置
            
            // 🔥 修复：不要设置委托给 Military 的属性（Quantity, Morale, Combativity, Experience）
            // 这些值会在 LinkReferencesPhase 链接 Army 后自动从 Military 获取
            // troop.Quantity = dto.Quantity;
            // troop.Morale = dto.Morale;
            // troop.Combativity = dto.Combativity;
            // troop.Experience = dto.Experience;
            
            // 🔥 新增：恢复部队增益字段
            troop.IncrementOfAvoidSurroundedChance = dto.IncrementOfAvoidSurroundedChance;
            troop.IncrementOfRumourDay = dto.IncrementOfRumourDay;
            troop.IncrementOfAttractDay = dto.IncrementOfAttractDay;
            troop.IncrementOfChaosAfterSurroundAttackChance = dto.IncrementOfChaosAfterSurroundAttackChance;
            troop.IncrementOfChaosDay = dto.IncrementOfChaosDay;
            troop.IncrementOfInjuryRate = dto.IncrementOfInjuryRate;
            troop.IncrementOfInvestigateRadius = dto.IncrementOfInvestigateRadius;
            troop.IncrementOfMovability = dto.IncrementOfMovability;
            troop.IncrementOfRationDays = dto.IncrementOfRationDays;
            troop.IncrementOfStratagemRadius = dto.IncrementOfStratagemRadius;
            troop.IncrementPerDayOfCombativity = dto.IncrementPerDayOfCombativity;
            troop.IncrementPerDayOfMorale = dto.IncrementPerDayOfMorale;
            troop.IncrementOfStuntDay = dto.IncrementOfStuntDay;
            troop.IncrementOfSpeed = dto.IncrementOfSpeed;
            
            troop.RateIncrementOfRateOnWater = dto.RateIncrementOfRateOnWater;
            troop.RateIncrementOfTerrainRateOnCliff = dto.RateIncrementOfTerrainRateOnCliff;
            troop.RateIncrementOfTerrainRateOnDesert = dto.RateIncrementOfTerrainRateOnDesert;
            troop.RateIncrementOfTerrainRateOnForrest = dto.RateIncrementOfTerrainRateOnForrest;
            troop.RateIncrementOfTerrainRateOnGrassland = dto.RateIncrementOfTerrainRateOnGrassland;
            troop.RateIncrementOfTerrainRateOnMarsh = dto.RateIncrementOfTerrainRateOnMarsh;
            troop.RateIncrementOfTerrainRateOnMountain = dto.RateIncrementOfTerrainRateOnMountain;
            troop.RateIncrementOfTerrainRateOnPlain = dto.RateIncrementOfTerrainRateOnPlain;
            troop.RateIncrementOfTerrainRateOnRidge = dto.RateIncrementOfTerrainRateOnRidge;
            troop.RateIncrementOfTerrainRateOnWasteland = dto.RateIncrementOfTerrainRateOnWasteland;
            troop.RateIncrementOfTerrainRateOnWater = dto.RateIncrementOfTerrainRateOnWater;
            
            troop.IncrementOffenceRate = dto.IncrementOffenceRate;
            troop.RateOfBoost = dto.RateOfBoost;
            troop.RateOfCriticalArchitectureDamage = dto.RateOfCriticalArchitectureDamage;
            troop.RateOfCriticalDamageReceived = dto.RateOfCriticalDamageReceived;
            troop.RateOfDefence = dto.RateOfDefence;
            troop.RateOfFireDamage = dto.RateOfFireDamage;
            troop.RateOfFireProtection = dto.RateOfFireProtection;
            troop.RateOfGongxin = dto.RateOfGongxin;
            troop.RateOfInjuryOnCriticalStrike = dto.RateOfInjuryOnCriticalStrike;
            troop.RateOfMovability = dto.RateOfMovability;
            troop.RateOfOffence = dto.RateOfOffence;
            troop.RateOfQibingDamage = dto.RateOfQibingDamage;
            troop.MarkInfluenceBuffNeedsBootstrapFromSave();
            
            troop.AttackRangeIncreaseByInfluence = dto.AttackRangeIncreaseByInfluence;
            troop.InCityOffenseRate = dto.InCityOffenseRate;
            troop.MovabilityByViewArea = dto.MovabilityByViewArea;
            
            // 🔥 根本修复：恢复粮食和特技状态，防止读档后粮食变0、特技自动触发
            // 日期：2026-02-13
            // 问题：LoadDataPhase 缺少恢复 Food/CurrentStuntIDString/StuntDayLeft 的代码
            // 结果：读档后这些字段保持默认值0，导致AI误判并自动触发新特技
            troop.Food = dto.Food;
            troop.CurrentStuntIDString = dto.CurrentStuntIDString;
            troop.StuntDayLeft = dto.StuntDayLeft;
            troop.CurrentCombatMethodID = dto.CurrentCombatMethodID;
            troop.CurrentStratagemID = dto.CurrentStratagemID;
            troop.AutoCombatMethodID = dto.AutoCombatMethodID;
            
            #if DEBUG
            if (dto.CurrentStuntIDString > 0 || dto.StuntDayLeft > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadTroopFromDTO] 部队 {troop.ID} ({troop.Name}) 恢复特技状态: StuntID={dto.CurrentStuntIDString}, DayLeft={dto.StuntDayLeft}");
            }
            #endif
            
            // 🔥 根本修复：清空路径数据，防止读档后使用旧路径导致瞬移
            // 日期：2026-03-13
            // 问题：_firstTierPath 和 _cachedPath 被序列化，读档后恢复了存档时的旧路径
            //       AI 部队的 UpdateMovement_Quick 使用旧路径，导致瞬移到不可通行的位置
            // 场景：张宝队存档时在 {X:163 Y:124}，路径指向 {X:162 Y:121}（峻岭）
            //       读档后路径被恢复，AI 立即执行移动，触发地形阻挡
            // 解决：读档时强制清空所有路径数据，让 AI 重新寻路
            // ANTI-BAND-AID：不修改 UpdateMovement_Quick 的逻辑，而是修复数据源
            troop.FirstTierPathInternal.Clear();
            troop.CachedPathInternal.Clear();  // 🔥 同时清空缓存路径
            troop.HasPath = false;
            troop.FirstIndex = 0;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadTroopFromDTO] 部队 {troop.ID} ({troop.Name}) 清空路径数据");
            #endif
            
            // 🔥 修复：恢复部队命令（修复存档/读档后攻击命令丢失）
            // 日期：2026-03-21
            // 问题：LoadTroopFromDTO 未恢复 mingling 字段，导致读档后攻击命令丢失
            // 原因：TroopDTO 缺少 Mingling 字段，导致 mingling 和 Command 未被保存/恢复
            // 解决：恢复 mingling 字段，并通过 SetCommand 同步 Command 枚举
            // 
            // 🔥 空检查合理性：向后兼容旧存档（旧存档没有 Mingling 字段）
            if (!string.IsNullOrEmpty(dto.Mingling))
            {
                troop.mingling = dto.Mingling;
                
                // 🔥 关键：通过 mingling 反向推导 Command 枚举
                // 🔥 C# 12：使用显式类型而不是 var
                TroopCommand command = dto.Mingling switch
                {
                    "Stratagem" => TroopCommand.Stratagem,
                    "Attack" => TroopCommand.Attack,
                    "Move" => TroopCommand.Move,
                    "Enter" => TroopCommand.Enter,
                    "攻击建筑" => TroopCommand.AttackArch,
                    "攻击军队" => TroopCommand.AttackTroop,
                    _ => TroopCommand.None
                };
                
                // 使用 SetCommand 确保 mingling 和 Command 同步
                troop.SetCommand(command);
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadTroopFromDTO] 部队 {troop.ID} ({troop.Name}) 恢复命令: mingling={dto.Mingling}, Command={command}");
                #endif
            }
            
            // Store collection IDs for later linking
            if (dto.PersonIDs != null && dto.PersonIDs.Count > 0)
            {
                troop.PersonIDs = dto.PersonIDs;
            }
            else if (!string.IsNullOrEmpty(dto.PersonsString))
            {
                troop.PersonIDs = dto.PersonsString
                    .Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(int.Parse)
                    .ToList();
            }
            else
            {
                troop.PersonIDs = new List<int>();
            }
            
            return troop;
        }
        
        /// <summary>
        /// Load Section collection from SectionDTO collection
        /// </summary>
        private void LoadSections(List<SectionDTO> sectionDTOs, GameScenario scenario)
        {
            if (sectionDTOs == null || sectionDTOs.Count == 0)
                return;
            
            foreach (var dto in sectionDTOs)
            {
                var section = LoadSectionFromDTO(dto);
                if (section != null)
                {
                    scenario.Sections.Add(section);
                }
            }
        }
        
        /// <summary>
        /// Load a single Section from SectionDTO
        /// </summary>
        public Section LoadSectionFromDTO(SectionDTO dto)
        {
            if (dto == null)
                return null;
            
            var section = new Section();
            
            // Basic properties
            section.ID = dto.ID;
            section.Name = dto.Name;
            
            // Store ID references
            // 🔥 向后兼容：如果 BelongedFactionID 无效，稍后通过 Architecture 推断
            // 日期：2026-03-16
            // 原因：旧剧本可能没有 BelongedFactionID 字段
            section.BelongedFactionID = dto.BelongedFactionID;
            section.AIDetailID = dto.AIDetailID;
            
            // 🔥 数据验证：AIDetailID 必须 >= 0
            // 日期：2026-03-17
            // 原因：LinkReferencesPhase 需要通过 AIDetailID 恢复 AIDetail 引用
            // 注意：ID=0 是有效的（参考 ID判断规范.md）
            if (section.AIDetailID < 0)
            {
                throw new InvalidOperationException(
                    $"数据损坏：军区 {section.ID} ({section.Name}) 的 AIDetailID={section.AIDetailID} 无效（< 0）。" +
                    $"这说明剧本数据有问题，需要检查 JSON 文件中的 AIDetailIDString 字段。");
            }
            
            // 🔥 修复：恢复都督ID
            // 日期：2026-02-17
            section.SectionLeaderID = dto.SectionLeaderID;
            
            // 🔥 修复：恢复方向目标ID
            // 日期：2026-02-17
            section.OrientationFactionID = dto.OrientationFactionID;
            section.OrientationSectionID = dto.OrientationSectionID;
            section.OrientationStateID = dto.OrientationStateID;
            section.OrientationArchitectureID = dto.OrientationArchitectureID;
            
            // Store collection IDs for later linking
            if (dto.ArchitectureIDs != null && dto.ArchitectureIDs.Count > 0)
            {
                section.ArchitectureIDs = dto.ArchitectureIDs;
            }
            else if (!string.IsNullOrEmpty(dto.ArchitecturesString))
            {
                // 🔥 关键修复：旧剧本用空格分隔，不是逗号
                // 日期：2026-03-16
                // 使用与 ParseIDString 相同的分隔符
                List<int> result = [];
                foreach (var part in dto.ArchitecturesString.Split([' ', ',', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part, out int id))
                        result.Add(id);
                }
                section.ArchitectureIDs = result;
            }
            else
            {
                section.ArchitectureIDs = [];
            }
            
            // 🔥 修复：恢复AI冷却状态（修复存档/读档后AI每回合都执行的问题）
            // 日期：2026-03-21
            // 问题：LoadSectionFromDTO 未恢复AI冷却字段，导致读档后冷却状态丢失，AI每回合都执行
            // 解决：恢复所有AI冷却相关字段
            section.SetAiCooldownCounter(dto.AiCooldownCounter);
            section.SetIsDirty(dto.IsDirty);
            section.SetIsInitialized(dto.IsInitialized);
            section.SetUrgentCooldown(dto.UrgentCooldown);
            section.SetCreationTurn(dto.CreationTurn);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadSectionFromDTO] 军区 {section.ID} ({section.Name}) 恢复AI冷却状态: Counter={dto.AiCooldownCounter}, Initialized={dto.IsInitialized}, CreationTurn={dto.CreationTurn}");
            #endif
            
            return section;
        }
        
        // Placeholder methods for other game object types
        // These will be implemented based on the actual DTO structures
        
        private void LoadTreasures(List<TreasureDTO> treasureDTOs, GameScenario scenario)
        {
            // 🔥 修复：实现 Treasure 加载
            // 日期：2026-02-10
            // 问题：LoadTreasures 是 TODO，导致 Treasure 根本没有被加载
            // 结果：Person 引用的 Treasure 找不到
            
            if (treasureDTOs == null || treasureDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadDataPhase] No treasures to load");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] Loading {treasureDTOs.Count} treasures...");
            
            foreach (var dto in treasureDTOs)
            {
                try
                {
                    var treasure = LoadTreasureFromDTO(dto);
                    if (treasure != null)
                    {
                        scenario.Treasures.Add(treasure);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] Failed to load Treasure {dto.ID}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] Loaded {scenario.Treasures.Count} treasures");
        }
        
        /// <summary>
        /// Load a Treasure from DTO
        /// </summary>
        private Treasure LoadTreasureFromDTO(TreasureDTO dto)
        {
            if (dto == null)
                return null;
            
            var treasure = new Treasure
            {
                ID = dto.ID,
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                Worth = dto.Worth,
                Available = dto.Available,
                // 🔥 2026-02-17 修复：加载图片和基础属性
                Pic = dto.Pic,
                AppearYear = dto.AppearYear,
                TreasureGroup = dto.TreasureGroup,
                Durability = dto.Durability,
                // 🔥 2026-03-18 修复：Reference fields (IDs for later linking)
                // JSON 中使用 "BelongedPersonIDString" 和 "HidePlaceIDString"
                BelongedPersonIDString = dto.BelongedPersonID,
                HidePlaceIDString = dto.HidePlaceID,
                // 🔥 2026-02-17 修复：加载 InfluencesString
                InfluencesString = dto.InfluencesString ?? string.Empty
            };
            
            // Note: Influences will be parsed from InfluencesString in LinkReferencesPhase
            // via GameScenario.LoadInfluences() which calls treasure.Influences.LoadFromString()
            
            return treasure;
        }
        
        private void LoadRegions(List<RegionDTO> regionDTOs, GameScenario scenario)
        {
            if (regionDTOs == null || regionDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadRegions] ⚠️ RegionDTOs 为空或数量为 0");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadRegions] 开始加载 {regionDTOs.Count} 个地域");
            
            foreach (var dto in regionDTOs)
            {
                var region = new global::GameObjects.ArchitectureDetail.Region
                {
                    ID = dto.ID,
                    Name = dto.Name
                };
                
                // 🔥 关键：初始化集合对象（Architectures, States）
                region.Init();
                scenario.Regions.Add(region);
                
                System.Diagnostics.Debug.WriteLine($"[LoadRegions] 加载地域: ID={region.ID}, Name={region.Name}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadRegions] ✅ 地域加载完成，共 {scenario.Regions.Count} 个");
        }
        
        /// <summary>
        /// Load States collection from StateDTO collection
        /// </summary>
        private void LoadStates(List<StateDTO> stateDTOs, GameScenario scenario)
        {
            if (stateDTOs == null || stateDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadStates] ⚠️ StateDTOs 为空或数量为 0");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadStates] 开始加载 {stateDTOs.Count} 个州域");
            
            foreach (var dto in stateDTOs)
            {
                var state = new global::GameObjects.ArchitectureDetail.State
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    StateAdminID = dto.StateAdminID,
                    // 🔥 修复：保存临时字段，用于 Phase 3 链接引用
                    LinkedRegionID = dto.LinkedRegionID,
                    ContactStateIDs = dto.ContactStateIDs ?? []
                };
                
                state.Init();
                scenario.States.Add(state);
                
                System.Diagnostics.Debug.WriteLine($"[LoadStates] 加载州域: ID={state.ID}, Name={state.Name}, LinkedRegionID={state.LinkedRegionID}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadStates] ✅ 州域加载完成，共 {scenario.States.Count} 个");
        }
        
        private void LoadRouteways(List<RoutewayDTO> routewayDTOs, GameScenario scenario)
        {
            if (routewayDTOs == null || routewayDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadRouteways] ⚠️ RoutewayDTO 列表为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadRouteways] 开始加载 {routewayDTOs.Count} 个路径");
            
            int count = 0;
            foreach (var dto in routewayDTOs)
            {
                var routeway = new Routeway
                {
                    ID = dto.ID,
                    StartArchitectureString = dto.StartArchitectureID,
                    EndArchitectureString = dto.EndArchitectureID,
                    LastActivePointIndex = dto.Level,
                    Building = dto.Building
                };
                
                scenario.Routeways.Add(routeway);
                count++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadRouteways] ✅ 完成，共加载 {count} 个路径");
        }
        
        private void LoadMilitaries(List<MilitaryDTO> militaryDTOs, GameScenario scenario)
        {
            if (militaryDTOs == null || militaryDTOs.Count == 0)
                return;
            
            foreach (var dto in militaryDTOs)
            {
                var military = LoadMilitaryFromDTO(dto);
                if (military != null)
                {
                    scenario.Militaries.Add(military);
                }
            }
        }
        
        /// <summary>
        /// Load a single Military from MilitaryDTO
        /// </summary>
        private Military LoadMilitaryFromDTO(MilitaryDTO dto)
        {
            if (dto == null)
                return null;
            
            var military = new Military();
            
            // 🔥 2026-02-11 关键修复：必须在设置任何属性之前清空循环引用
            // 问题：Quantity.set 会访问 ShelledMilitary.Quantity，如果存在循环引用会立即栈溢出
            // 解决：在设置 Quantity 之前强制清空 ShelledMilitary
            military.ShelledMilitary = null;
            
            // Basic properties
            military.ID = dto.ID;
            military.Name = dto.Name;  // 🔥 重要：设置Name
            military.KindID = dto.KindID;  // 使用属性（大写）会自动设置kindID字段
            
            // Store ID references for later linking
            military.BelongedArchitectureID = dto.BelongedArchitectureID;  // 🔥 修复：使用属性而不是字段
            // Note: BelongedFaction is set through BelongedArchitecture, not directly
            
            // 🔥 根本修复：加载 ShelledMilitaryID
            // 日期：2026-02-11
            // 问题：ShelledMilitaryID 从未被加载，导致默认为 0，形成意外的自引用
            // 解决：从 DTO 加载此字段，LinkReferencesPhase 会正确链接
            military.ShelledMilitaryID = dto.ShelledMilitaryID;
            
            // Military stats
            military.Quantity = dto.Quantity;
            military.Morale = dto.Morale;
            military.Combativity = dto.Combativity;
            military.Experience = dto.Experience;
            
            return military;
        }
        
        private void LoadFacilities(List<FacilityDTO> facilityDTOs, GameScenario scenario)
        {
            if (facilityDTOs == null || facilityDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadFacilities] ⚠️ FacilityDTO 列表为空或 null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadFacilities] 开始加载 {facilityDTOs.Count} 个设施");
            
            int count = 0;
            foreach (var dto in facilityDTOs)
            {
                var facility = LoadFacilityFromDTO(dto);
                if (facility != null)
                {
                    scenario.Facilities.Add(facility);
                    count++;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadFacilities] ✅ 成功加载 {count} 个设施到 scenario.Facilities");
        }
        
        private Facility LoadFacilityFromDTO(FacilityDTO dto)
        {
            if (dto == null)
                return null;
            
            var facility = new Facility();
            
            // Copy basic properties
            facility.ID = dto.ID;
            facility.KindID = dto.KindID;
            facility.Endurance = dto.Endurance;
            
            return facility;
        }
        
        private void LoadInformations(List<InformationDTO> informationDTOs, GameScenario scenario)
        {
            if (informationDTOs == null || informationDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadInformations] ⚠️ InformationDTO 列表为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadInformations] 开始加载 {informationDTOs.Count} 个情报");
            
            int count = 0;
            foreach (var dto in informationDTOs)
            {
                var info = new Information
                {
                    ID = dto.ID,
                    Position = new Microsoft.Xna.Framework.Point(dto.PositionX, dto.PositionY),
                    Level = (InformationLevel)dto.Level,
                    Oblique = dto.Oblique,           // 🔥 恢复斜向范围
                    Radius = dto.Radius,             // 🔥 恢复情报半径
                    DayCost = dto.DayCost,           // 🔥 恢复每日消耗
                    DaysLeft = dto.DaysLeft,         // 🔥 恢复剩余天数
                    DaysStarted = dto.DaysStarted    // 🔥 恢复开始天数
                };
                
                // 🔥 注意：BelongedFaction 和 BelongedArchitecture 需要在 LinkReferencesPhase 中恢复
                // 这里先添加到列表，稍后通过 ID 重新链接引用
                
                scenario.Informations.Add(info);
                count++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadInformations] ✅ 完成，共加载 {count} 个情报");
        }
        
        private void LoadTroopEvents(List<TroopEventDTO> troopEventDTOs, GameScenario scenario)
        {
            if (troopEventDTOs == null || troopEventDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadTroopEvents] ⚠️ TroopEventDTO 列表为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadTroopEvents] 开始加载 {troopEventDTOs.Count} 个部队事件");
            
            int count = 0;
            foreach (var dto in troopEventDTOs)
            {
                var evt = new TroopEvent
                {
                    ID = dto.ID,
                    Name = dto.Name ?? "",
                    
                    // 🔥 2026-03-16 修复：Happened 现在是 bool 类型，直接赋值
                    // 之前：Happened = dto.Happened != 0 (假设 int 类型)
                    // 现在：Happened = dto.Happened (bool 类型)
                    Happened = dto.Happened,
                    
                    Repeatable = dto.Repeatable,
                    
                    // 🔥 2026-03-06 修复：恢复所有字段
                    AfterEventHappened = dto.AfterEventHappened,
                    CheckArea = (EventCheckAreaKind)dto.CheckArea,
                    ConditionsString = dto.ConditionsString,
                    dialogString = dto.DialogString,
                    EffectAreasString = dto.EffectAreasString,
                    EffectPersonsString = dto.EffectPersonsString,
                    LaunchPersonString = dto.LaunchPersonString,
                    SelfEffectsString = dto.SelfEffectsString,
                    TargetPersonsString = dto.TargetPersonsString,
                    Image = dto.Image ?? "",
                    Sound = dto.Sound ?? "",
                    TryToShowString = dto.TryToShowString,
                    HappenChance = dto.HappenChance
                };
                
                // 🔥 关键修复：使用 AddTroopEventWithEvent 而不是 Add
                // 原因：AddTroopEventWithEvent 会订阅 OnApplyTroopEvent 事件，确保事件影响能够正确施加
                // 日期：2026-03-23
                // 问题：使用 Add 方法不会订阅事件，导致事件触发后影响不施加
                scenario.TroopEvents.AddTroopEventWithEvent(evt, true);
                count++;
                
                if (count <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadTroopEvents] 已添加TroopEvent {evt.Name}(ID:{evt.ID})");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadTroopEvents] ✅ 完成，共加载 {count} 个部队事件");
        }
        
        private void LoadCaptives(List<CaptiveDTO> captiveDTOs, GameScenario scenario)
        {
            if (captiveDTOs == null || captiveDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadCaptives] ⚠️ CaptiveDTO 列表为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadCaptives] 开始加载 {captiveDTOs.Count} 个俘虏");
            
            int count = 0;
            foreach (var dto in captiveDTOs)
            {
                var captive = new Captive
                {
                    ID = dto.ID,
                    CaptiveFactionID = dto.BelongedFactionID,
                    CaptivePersonID = dto.CaptivePersonID,
                    RansomArchitectureID = dto.RansomArchitectureID,
                    RansomArriveDays = dto.RansomArriveDays,
                    RansomFund = dto.RansomFund
                };
                
                scenario.Captives.Add(captive);
                count++;
                
                if (count <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadCaptives] 已添加Captive ID:{captive.ID}, PersonID:{captive.CaptivePersonID}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadCaptives] ✅ 完成，共加载 {count} 个俘虏");
        }
        
        private void LoadEvents(List<EventDTO> eventDTOs, GameScenario scenario)
        {
            if (eventDTOs == null || eventDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadEvents] ⚠️ EventDTO 列表为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadEvents] 开始加载 {eventDTOs.Count} 个事件");
            
            int count = 0;
            foreach (var dto in eventDTOs)
            {
                var evt = new Event
                {
                    ID = dto.ID,
                    Name = dto.Name ?? "",
                    
                    // 🔥 2026-03-16 修复：Happened 现在是 bool 类型，直接赋值
                    // 之前：happened = dto.Happened != 0 (假设 int 类型)
                    // 现在：happened = dto.Happened (bool 类型)
                    happened = dto.Happened,
                    
                    repeatable = dto.Repeatable,
                    StartYear = dto.LaunchYear,
                    
                    // 🔥 2026-03-06 修复：恢复所有字符串字段
                    AfterEventHappened = dto.AfterEventHappened,
                    happenChance = dto.HappenChance,
                    nextScenario = dto.NextScenario,
                    personString = dto.PersonString,
                    PersonCondString = dto.PersonCondString,
                    architectureString = dto.ArchitectureString,
                    architectureCondString = dto.ArchitectureCondString,
                    factionString = dto.FactionString,
                    factionCondString = dto.FactionCondString,
                    dialogString = dto.DialogString,
                    effectString = dto.EffectString,
                    yesdialogString = dto.YesDialogString,
                    nodialogString = dto.NoDialogString,
                    yesEffectString = dto.YesEffectString,
                    noEffectString = dto.NoEffectString,
                    architectureEffectString = dto.ArchitectureEffectString,
                    factionEffectIDString = dto.FactionEffectIDString,
                    yesArchitectureEffectString = dto.YesArchitectureEffectString,
                    noArchitectureEffectString = dto.NoArchitectureEffectString,
                    scenBiographyString = dto.ScenBiographyString,
                    Image = dto.Image ?? "",
                    Sound = dto.Sound ?? "",
                    GloballyDisplayed = dto.GloballyDisplayed,
                    StartMonth = dto.StartMonth,
                    EndYear = dto.EndYear,
                    EndMonth = dto.EndMonth,
                    Minor = dto.Minor,
                    TryToShowString = dto.TryToShowString
                };
                
                // 初始化Event（创建空集合）
                evt.Init();
                
                // 🔥 关键：只添加到列表，不订阅事件
                // 原因：OnApplyEvent 会在 AfterLoadGameScenario() 中统一订阅（GameScenario.cs 第 7043 行）
                // 流程：LoadDataPhase.LoadEvents() → AfterLoadGameScenario() → AddEventWithEvent(e, false)
                // 注意：读档时是在 AfterLoadSaveFile() 中订阅（GameScenario.cs 第 7043 行）
                // 日期：2026-03-23
                scenario.AllEvents.Add(evt);
                count++;
                
                if (count <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadEvents] 已添加Event {evt.Name}(ID:{evt.ID}), happened={evt.happened}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadEvents] ✅ 完成，共加载 {count} 个事件");
        }
        
        /// <summary>
        /// Load Biography collection from BiographyDTO collection
        /// 🔥 修复：添加Biography反序列化支持
        /// </summary>
        private void LoadBiographies(List<BiographyDTO> biographyDTOs, GameScenario scenario)
        {
            if (biographyDTOs == null || biographyDTOs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadBiographies] ⚠️ Biography数据为空");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadBiographies] 开始加载 {biographyDTOs.Count} 条Biography");
            
            int count = 0;
            foreach (var dto in biographyDTOs)
            {
                var biography = new Biography
                {
                    ID = dto.ID,
                    Name = dto.Name ?? "",  // 🔥 防止Name为null
                    Brief = dto.Brief ?? "",
                    FactionColor = dto.FactionColor,
                    History = dto.History ?? "",
                    Romance = dto.Romance ?? "",
                    InGame = dto.InGame ?? "",
                    MilitaryKindsString = dto.MilitaryKindsString
                };
                
                biography.Init();  // 初始化MilitaryKinds
                
                scenario.AllBiographies.AddBiography(biography);
                count++;
                
                if (count <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadBiographies] 已添加Biography {biography.Name}(ID:{biography.ID})");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadBiographies] 完成，共加载 {count} 条Biography");
        }
        
        /// <summary>
        /// Convert MapDTO to Map
        /// </summary>
        private global::GameObjects.Map ConvertDTOToMap(MapDTO dto)
        {
            if (dto == null)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] ⚠️ MapDTO 为 null");
                return new global::GameObjects.Map();
            }
            
            // 🔥 2026-03-16 修复：支持旧格式和新格式
            // 旧格式：MapDimensions 对象
            // 新格式：MapDimensionsX 和 MapDimensionsY 分开
            int dimensionsX = dto.MapDimensionsX;
            int dimensionsY = dto.MapDimensionsY;
            
            if (dimensionsX == 0 && dimensionsY == 0 && dto.MapDimensions.HasValue)
            {
                // 使用旧格式
                dimensionsX = dto.MapDimensions.Value.X;
                dimensionsY = dto.MapDimensions.Value.Y;
                System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] 使用旧格式 MapDimensions: {dimensionsX}x{dimensionsY}");
            }
            
            // 同样处理 JumpPosition
            int jumpX = dto.JumpPositionX;
            int jumpY = dto.JumpPositionY;
            
            if (jumpX == 0 && jumpY == 0 && dto.JumpPosition.HasValue)
            {
                jumpX = dto.JumpPosition.Value.X;
                jumpY = dto.JumpPosition.Value.Y;
                System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] 使用旧格式 JumpPosition: {jumpX},{jumpY}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] 开始加载地图数据");
            System.Diagnostics.Debug.WriteLine($"  - MapName: {dto.MapName ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"  - MapDataString 长度: {dto.MapDataString?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  - MapDimensions: {dimensionsX}x{dimensionsY}");
            
            var map = new global::GameObjects.Map
            {
                MapName = dto.MapName,
                MapDataString = dto.MapDataString,
                JumpPosition = new Microsoft.Xna.Framework.Point(jumpX, jumpY),
                TileWidthMin = dto.TileWidthMin,
                TileWidthMax = dto.TileWidthMax
            };
            
            // 🔥 关键：从 MapDataString 加载地图数据
            if (!string.IsNullOrEmpty(dto.MapDataString) && dimensionsX > 0 && dimensionsY > 0)
            {
                bool success = map.LoadMapData(dto.MapDataString, dimensionsX, dimensionsY);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadDataPhase] ✅ 地图数据加载成功，MapDimensions={map.MapDimensions}");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"[LoadDataPhase] 地图数据加载失败: {dimensionsX}×{dimensionsY}, " +
                        $"MapDataString长度={dto.MapDataString.Length}");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"[LoadDataPhase] MapDataString 为空或地图尺寸无效: " +
                    $"MapDataString={(dto.MapDataString == null ? "null" : $"长度{dto.MapDataString.Length}")}, " +
                    $"MapDimensions={dimensionsX}×{dimensionsY}");
            }
            
            return map;
        }
        
        // ========================================
        // 反向设置方法（旧格式兼容性）
        // ========================================
        
        /// <summary>
        /// 1. 反向设置 Person.LocationArchitectureID
        /// 从 Architecture.PersonIDs 反向设置 Person.LocationArchitectureID
        /// </summary>
        private void ReverseSetPersonLocation(GameScenario scenario)
        {
            int count = 0;
            int missingCount = 0;
            foreach (Architecture architecture in scenario.Architectures.GameObjects)
            {
                if (architecture.PersonIDs != null && architecture.PersonIDs.Count > 0)
                {
                    foreach (int personID in architecture.PersonIDs)
                    {
                        Person person = scenario.Persons.GetGameObject(personID) as Person;
                        if (person != null)
                        {
                            // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                            // 只有当 Person.LocationArchitectureID 未设置时才设置
                            if (person.LocationArchitectureID < 0)
                            {
                                person.LocationArchitectureID = architecture.ID;
                                count++;
                                
                                #if DEBUG
                                if (count <= 5)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"  [ReverseSetPersonLocation] Person {person.ID} ({person.Name}) " +
                                        $"LocationArchitectureID = {architecture.ID} ({architecture.Name})");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            missingCount++;
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ [ReverseSetPersonLocation] 建筑 {architecture.ID} ({architecture.Name}) " +
                                $"引用了不存在的武将 ID {personID}");
                            #endif
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetPersonLocation] 设置 {count} 个武将的 LocationArchitectureID" +
                (missingCount > 0 ? $"，跳过 {missingCount} 个缺失的武将引用" : ""));
        }
        
        /// <summary>
        /// 2. 反向设置 Architecture.BelongedFactionID (Critical)
        /// 从 Faction.ArchitectureIDs 反向设置 Architecture.BelongedFactionID
        /// </summary>
        private void ReverseSetArchitectureFaction(GameScenario scenario)
        {
            int count = 0;
            int missingCount = 0;
            foreach (Faction faction in scenario.Factions.GameObjects)
            {
                if (faction.ArchitectureIDs != null && faction.ArchitectureIDs.Count > 0)
                {
                    foreach (int archID in faction.ArchitectureIDs)
                    {
                        Architecture arch = scenario.Architectures.GetGameObject(archID) as Architecture;
                        if (arch != null)
                        {
                            // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                            // 只有当 BelongedFactionID 未设置时才设置
                            if (arch.BelongedFactionID < 0)
                            {
                                arch.BelongedFactionID = faction.ID;
                                count++;
                                
                                #if DEBUG
                                if (count <= 5)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"  [ReverseSetArchitectureFaction] Architecture {arch.ID} ({arch.Name}) " +
                                        $"BelongedFactionID = {faction.ID} ({faction.Name})");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            missingCount++;
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ [ReverseSetArchitectureFaction] 势力 {faction.ID} ({faction.Name}) " +
                                $"引用了不存在的建筑 ID {archID}");
                            #endif
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetArchitectureFaction] 设置 {count} 个建筑的 BelongedFactionID" +
                (missingCount > 0 ? $"，跳过 {missingCount} 个缺失的建筑引用" : ""));
        }
        
        /// <summary>
        /// 3. 反向设置 Person.BelongedFactionID (Critical)
        /// 通过 Architecture.BelongedFactionID 推断 Person.BelongedFactionID
        /// </summary>
        private void ReverseSetPersonFaction(GameScenario scenario)
        {
            int count = 0;
            int missingCount = 0;
            foreach (Architecture arch in scenario.Architectures.GameObjects)
            {
                // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                if (arch.BelongedFactionID >= 0 && arch.PersonIDs != null)
                {
                    foreach (int personID in arch.PersonIDs)
                    {
                        Person person = scenario.Persons.GetGameObject(personID) as Person;
                        if (person != null)
                        {
                            // 只有当 BelongedFactionID 未设置时才设置
                            if (person.BelongedFactionID < 0)
                            {
                                person.BelongedFactionID = arch.BelongedFactionID;
                                count++;
                                
                                #if DEBUG
                                if (count <= 5)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"  [ReverseSetPersonFaction] Person {person.ID} ({person.Name}) " +
                                        $"BelongedFactionID = {arch.BelongedFactionID} (通过建筑 {arch.Name} 推断)");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            missingCount++;
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ [ReverseSetPersonFaction] 建筑 {arch.ID} ({arch.Name}) " +
                                $"引用了不存在的武将 ID {personID}");
                            #endif
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetPersonFaction] 设置 {count} 个武将的 BelongedFactionID" +
                (missingCount > 0 ? $"，跳过 {missingCount} 个缺失的武将引用" : ""));
        }
        
        /// <summary>
        /// 4. 反向设置 Architecture.BelongedSectionID (High)
        /// 从 Section.ArchitectureIDs 反向设置 Architecture.BelongedSectionID
        /// </summary>
        private void ReverseSetArchitectureSection(GameScenario scenario)
        {
            int count = 0;
            int missingCount = 0;
            foreach (Section section in scenario.Sections.GameObjects)
            {
                if (section.ArchitectureIDs != null && section.ArchitectureIDs.Count > 0)
                {
                    foreach (int archID in section.ArchitectureIDs)
                    {
                        Architecture arch = scenario.Architectures.GetGameObject(archID) as Architecture;
                        if (arch != null)
                        {
                            // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                            if (arch.BelongedSectionID < 0)
                            {
                                arch.BelongedSectionID = section.ID;
                                count++;
                                
                                #if DEBUG
                                if (count <= 5)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"  [ReverseSetArchitectureSection] Architecture {arch.ID} ({arch.Name}) " +
                                        $"BelongedSectionID = {section.ID} ({section.Name})");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            missingCount++;
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ [ReverseSetArchitectureSection] 军区 {section.ID} ({section.Name}) " +
                                $"引用了不存在的建筑 ID {archID}");
                            #endif
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetArchitectureSection] 设置 {count} 个建筑的 BelongedSectionID" +
                (missingCount > 0 ? $"，跳过 {missingCount} 个缺失的建筑引用" : ""));
        }
        
        /// <summary>
        /// 5. 反向设置 Section.BelongedFactionID (High)
        /// 通过 Architecture.BelongedFactionID 推断 Section.BelongedFactionID
        /// </summary>
        private void ReverseSetSectionFaction(GameScenario scenario)
        {
            int count = 0;
            foreach (Section section in scenario.Sections.GameObjects)
            {
                // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                if (section.BelongedFactionID < 0 && section.ArchitectureIDs != null && section.ArchitectureIDs.Count > 0)
                {
                    // 从第一个建筑推断势力归属
                    int firstArchID = section.ArchitectureIDs[0];
                    Architecture arch = scenario.Architectures.GetGameObject(firstArchID) as Architecture;
                    if (arch != null && arch.BelongedFactionID >= 0)
                    {
                        section.BelongedFactionID = arch.BelongedFactionID;
                        count++;
                        
                        #if DEBUG
                        if (count <= 5)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"  [ReverseSetSectionFaction] Section {section.ID} ({section.Name}) " +
                                $"BelongedFactionID = {arch.BelongedFactionID} (通过建筑 {arch.Name} 推断)");
                        }
                        #endif
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetSectionFaction] 设置 {count} 个军区的 BelongedFactionID");
        }
        
        /// <summary>
        /// 6. 反向设置 Facility.BelongedArchitectureID (Medium)
        /// 从 Architecture.FacilityIDs 反向设置 Facility.BelongedArchitectureID
        /// </summary>
        private void ReverseSetFacilityArchitecture(GameScenario scenario)
        {
            int count = 0;
            int missingCount = 0;
            foreach (Architecture arch in scenario.Architectures.GameObjects)
            {
                if (arch.FacilityIDs != null && arch.FacilityIDs.Count > 0)
                {
                    foreach (int facilityID in arch.FacilityIDs)
                    {
                        Facility facility = scenario.Facilities.GetGameObject(facilityID) as Facility;
                        if (facility != null)
                        {
                            // 🔥 关键：ID=0 是有效的（洛阳的 ID=0）
                            // Facility 没有 BelongedArchitectureID 字段，需要检查是否存在
                            // 如果不存在，跳过此修复
                            // 注意：根据代码，Facility 类可能没有 BelongedArchitectureID 属性
                            // 这里先注释掉，等确认 Facility 类结构后再启用
                            
                            // if (facility.BelongedArchitectureID < 0)
                            // {
                            //     facility.BelongedArchitectureID = arch.ID;
                            //     count++;
                            // }
                            
                            count++;  // 临时：只统计数量
                        }
                        else
                        {
                            missingCount++;
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ [ReverseSetFacilityArchitecture] 建筑 {arch.ID} ({arch.Name}) " +
                                $"引用了不存在的设施 ID {facilityID}");
                            #endif
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"[ReverseSetFacilityArchitecture] 跳过 {count} 个设施（Facility 类可能没有 BelongedArchitectureID 字段）" +
                (missingCount > 0 ? $"，{missingCount} 个缺失的设施引用" : ""));
        }
    }
}
