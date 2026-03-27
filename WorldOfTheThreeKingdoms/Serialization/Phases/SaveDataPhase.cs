using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.ArchitectureDetail;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace WorldOfTheThreeKingdoms.Serialization.Phases
{
    /// <summary>
    /// Phase 1: Save Data Phase
    /// Converts game objects to DTOs, replacing object references with ID references
    /// </summary>
    public class SaveDataPhase
    {
        /// <summary>
        /// Convert GameScenario to GameScenarioDTO
        /// This is the main entry point for the Save Data Phase
        /// </summary>
        public GameScenarioDTO ConvertToDTO(GameScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            var dto = new GameScenarioDTO
            {
                Version = 1,
                ScenarioTitle = scenario.ScenarioTitle,
                ScenarioDescription = scenario.ScenarioDescription,
                MOD = scenario.MOD,
                
                // Date information - 🔥 添加防御性检查
                Year = scenario.Date?.Year ?? 0,
                Month = scenario.Date?.Month ?? 0,
                Day = scenario.Date?.Day ?? 0,
                DaySince = scenario.DaySince,
                
                // Player information - 🔥 添加防御性检查
                PlayerList = scenario.PlayerList ?? new List<int>(),
                CurrentPlayerID = scenario.CurrentPlayerID ?? "-1",
                PlayerInfo = scenario.PlayerInfo ?? "",
                
                // Game time
                GameTime = scenario.GameTime,
                
                // Configuration
                UsingOwnCommonData = scenario.UsingOwnCommonData,
                
                // Relationship data - 🔥 添加防御性检查
                FatherIds = scenario.FatherIds ?? new Dictionary<int, int>(),
                MotherIds = scenario.MotherIds ?? new Dictionary<int, int>(),
                SpouseIds = scenario.SpouseIds ?? new Dictionary<int, int>(),
                BrotherIds = scenario.BrotherIds ?? new Dictionary<int, int[]>(),
                SuoshuIds = scenario.SuoshuIds ?? new Dictionary<int, int[]>(),
                CloseIds = scenario.CloseIds ?? new Dictionary<int, int[]>(),
                HatedIds = scenario.HatedIds ?? new Dictionary<int, int[]>(),
                MarriageGranterId = scenario.MarriageGranterId ?? new Dictionary<int, int>(),
                
                // 🔥 关键修复：保存地图数据
                ScenarioMap = ConvertMapToDTO(scenario.ScenarioMap)
            };
            
            #if DEBUG
            // 诊断输出
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] PlayerList 保存: Count={dto.PlayerList?.Count ?? 0}");
            if (dto.PlayerList != null && dto.PlayerList.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] PlayerList IDs: {string.Join(", ", dto.PlayerList)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] ⚠️ PlayerList 为空!");
            }
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] CurrentPlayerID: {dto.CurrentPlayerID ?? "null"}");
            #endif
            
            // Convert PersonIDRelation
            if (scenario.PersonRelationIds != null)
            {
                dto.PersonRelationIds = scenario.PersonRelationIds
                    .Select(r => new PersonIDRelationDTO
                    {
                        PersonID1 = r.PersonID1,
                        PersonID2 = r.PersonID2,
                        Relation = r.Relation
                    })
                    .ToList();
            }
            
            // Convert collections
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertToDTO] 开始转换游戏对象集合");
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertToDTO] scenario.Factions: {(scenario.Factions == null ? "null" : $"Count={scenario.Factions.Count}")}");
            #endif
            
            dto.Persons = ConvertPersons(scenario.Persons);
            dto.Factions = ConvertFactions(scenario.Factions);
            dto.Architectures = ConvertArchitectures(scenario.Architectures);
            dto.Treasures = ConvertTreasures(scenario.Treasures);
            dto.Legions = ConvertLegions(scenario.Legions);
            dto.Troops = ConvertTroops(scenario.Troops);
            dto.Sections = ConvertSections(scenario.Sections);
            dto.Regions = ConvertRegions(scenario.Regions);
            dto.States = ConvertStates(scenario.States);  // 🔥 修复：转换 States
            dto.Routeways = ConvertRouteways(scenario.Routeways);
            dto.Militaries = ConvertMilitaries(scenario.Militaries);
            dto.Facilities = ConvertFacilities(scenario.Facilities);
            dto.Informations = ConvertInformations(scenario.Informations);
            dto.TroopEvents = ConvertTroopEvents(scenario.TroopEvents);
            dto.Captives = ConvertCaptives(scenario.captiveData);
            dto.AllEvents = ConvertEvents(scenario.AllEvents);
            
            // 🔥 修复：添加Biography序列化
            dto.Biographies = ConvertBiographies(scenario.AllBiographies);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertToDTO] ✅ 集合转换完成");
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertToDTO] dto.Factions: {(dto.Factions == null ? "null" : $"Count={dto.Factions.Count}")}");
            #endif
            
            return dto;
        }
        
        /// <summary>
        /// Convert Person collection to PersonDTO collection
        /// </summary>
        private List<PersonDTO> ConvertPersons(PersonList persons)
        {
            if (persons == null || persons.Count == 0)
                return new List<PersonDTO>();
            
            return persons.GetList().GameObjects.Cast<Person>()
                .Select(p => ConvertPersonToDTO(p))
                .ToList();
        }
        
        /// <summary>
        /// Convert a single Person to PersonDTO
        /// Replaces object references with ID references
        /// </summary>
        public PersonDTO ConvertPersonToDTO(Person person)
        {
            if (person == null)
                return null;
            
            #if DEBUG
            // 🔥 诊断：检查前5个人物的技能、特技、称号数据（扩大范围）
            if (person.ID <= 4)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertPersonToDTO] {person.Name}(ID:{person.ID}):");
                System.Diagnostics.Debug.WriteLine($"  - Skills 对象: {(person.Skills == null ? "null" : "存在")}");
                System.Diagnostics.Debug.WriteLine($"  - Skills.Skills 集合: {(person.Skills?.Skills == null ? "null" : "存在")}");
                System.Diagnostics.Debug.WriteLine($"  - Skills.Skills.Count = {person.Skills?.Skills.Count ?? 0}");
                if (person.Skills?.Skills.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Skills.Skills.Keys = [{string.Join(", ", person.Skills.Skills.Keys)}]");
                    System.Diagnostics.Debug.WriteLine($"  - Skills.Skills.Values:");
                    foreach (var skill in person.Skills.Skills.Values)
                    {
                        System.Diagnostics.Debug.WriteLine($"      - {skill.Name}(ID:{skill.ID})");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"  - Stunts.Stunts.Count = {person.Stunts?.Stunts.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"  - RealTitles.Count = {person.RealTitles?.Count ?? 0}");
            }
            #endif
            
            var dto = new PersonDTO
            {
                ID = person.ID,
                Name = person.Name,
                SurName = person.SurName,  // 🔥 修复：保存姓氏
                GivenName = person.GivenName,
                CalledName = person.CalledName,
                Sex = person.Sex,
                PCharacter = person.PCharacter,
                Ideal = person.Ideal,
                IdealTendencyIDString = person.IdealTendencyIDString,  // 🔥 修复：保存出仕志向考虑ID
                
                // Basic attributes (保存基础值，不是计算值)
                Command = person.BaseCommand,
                Strength = person.BaseStrength,
                Intelligence = person.BaseIntelligence,
                Politics = person.BasePolitics,
                Glamour = person.BaseGlamour,
                Braveness = person.Braveness,
                Calmness = person.Calmness,
                Ambition = person.Ambition,
                
                // 🔥 忠诚度相关字段（不要序列化Loyalty，它是计算属性）
                PersonalLoyalty = person.PersonalLoyalty,
                TempLoyaltyChange = person.TempLoyaltyChange,
                HoneymoonMonths = person.HoneymoonMonths,
                
                // Experience values
                CommandExperience = person.CommandExperience,
                StrengthExperience = person.StrengthExperience,
                IntelligenceExperience = person.IntelligenceExperience,
                PoliticsExperience = person.PoliticsExperience,
                GlamourExperience = person.GlamourExperience,
                
                // 🔥 2026-02-18 修复：军事经验值
                BubingExperience = person.BubingExperience,
                NubingExperience = person.NubingExperience,
                QibingExperience = person.QibingExperience,
                ShuijunExperience = person.ShuijunExperience,
                QixieExperience = person.QixieExperience,
                TacticsExperience = person.TacticsExperience,
                StratagemExperience = person.StratagemExperience,
                InternalExperience = person.InternalExperience,
                
                // Status
                Status = (int)person.Status,
                Alive = person.Alive,
                Available = person.Available,
                Generation = person.Generation,
                
                // 🔥 2026-02-18 修复：出场年、出生年、死亡年
                YearAvailable = person.YearAvailable,
                YearBorn = person.YearBorn,
                YearDead = person.YearDead,
                
                // 🔥 2026-02-18 修复：基础属性
                Reputation = person.Reputation,
                Fund = person.Fund,
                Karma = person.Karma,
                ArrivingDays = person.ArrivingDays,
                AvailableLocation = person.AvailableLocation,
                DeadReason = (int)person.DeadReason,
                
                // 🔥 2026-02-18 修复：能力潜力
                CommandPotential = person.CommandPotential,
                StrengthPotential = person.StrengthPotential,
                IntelligencePotential = person.IntelligencePotential,
                PoliticsPotential = person.PoliticsPotential,
                GlamourPotential = person.GlamourPotential,
                
                // 🔥 2026-03-03 修复：训练方针ID
                TrainPolicyIDString = person.TrainPolicyIDString,
                
                // 🔥 2026-02-18 修复：状态字段
                Tiredness = person.Tiredness,
                InjureRate = person.InjureRate,
                OfficerMerit = person.OfficerMerit,
                WorkKind = (int)person.WorkKind,
                OutsideTask = (int)person.OutsideTask,
                OutsideDestination = person.OutsideDestination,
                TaskDays = person.TaskDays,
                Immortal = person.Immortal,
                NvGuan = person.NvGuan,
                IsGeneratedChildren = person.IsGeneratedChildren,
                DaySinceAvailable = person.DaySinceAvailable,
                
                // 🔥 2026-02-18 修复：统计数据
                YearJoin = person.YearJoin,
                TroopDamageDealt = person.TroopDamageDealt,
                TroopBeDamageDealt = person.TroopBeDamageDealt,
                ArchitectureDamageDealt = person.ArchitectureDamageDealt,
                RebelCount = person.RebelCount,
                ExecuteCount = person.ExecuteCount,
                OfficerKillCount = person.OfficerKillCount,
                FleeCount = person.FleeCount,
                HeldCaptiveCount = person.HeldCaptiveCount,
                CaptiveCount = person.CaptiveCount,
                StratagemSuccessCount = person.StratagemSuccessCount,
                StratagemFailCount = person.StratagemFailCount,
                StratagemBeSuccessCount = person.StratagemBeSuccessCount,
                StratagemBeFailCount = person.StratagemBeFailCount,
                RoutCount = person.RoutCount,
                RoutedCount = person.RoutedCount,
                
                // 🔥 2026-02-18 修复：人物特性
                BornRegion = (int)person.BornRegion,
                Strain = person.Strain,
                Qualification = (int)person.Qualification,
                LeaderPossibility = person.LeaderPossibility,
                StrategyTendency = (int)person.StrategyTendency,
                ValuationOnGovernment = (int)person.ValuationOnGovernment,
                ReturnedDaySince = person.ReturnedDaySince,
                LastOutsideTask = (int)person.LastOutsideTask,
                RewardFinished = person.RewardFinished,
                WaitForFeiZiPeriod = person.WaitForFeiZiPeriod,
                Tags = person.Tags,
                
                // 🔥 2026-02-18 修复：额外字段
                JoinFactionID = person.JoinFactionID ?? [],
                BattleSelfDamage = person.BattleSelfDamage,
                NumberOfChildren = person.NumberOfChildren,
                
                // Object references → ID references
                // 🔥 修复：不再保存 BelongedFactionID
                // 日期：2026-02-10
                // 问题：BelongedFactionID 是冗余字段，应该通过 LocationArchitecture 或 LocationTroop 计算
                // 保存计算属性的值会导致数据不一致
                BelongedFactionID = -1,  // 不再保存，读档时会通过 Location 计算
                LocationArchitectureID = person.LocationArchitecture?.ID ?? -1,
                LocationTroopID = person.LocationTroop?.ID ?? -1,
                ConvincingPersonID = person.ConvincingPerson?.ID ?? -1,
                BelongedCaptiveID = person.BelongedCaptiveID,  // 🔥 新增：保存俘虏对象 ID（2026-03-07）
                
                // Collection references → List<int> (performance optimization)
                // Using LINQ Select directly creates List<int> efficiently without intermediate allocations
                TreasureIDs = person.Treasures?.GetList().GameObjects.Cast<Treasure>().Select(t => t.ID).ToList() ?? [],
                SkillIDs = person.Skills?.Skills.Values.Select(s => s.ID).ToList() ?? [],
                StuntIDs = person.Stunts?.Stunts.Values.Select(s => s.ID).ToList() ?? [],
                TitleIDs = person.RealTitles?.Select(t => t.ID).ToList() ?? [],
                
                // Private members
                InformationKindID = person.InformationKindID,
                Huaiyun = person.huaiyun,
                HuaiyunTianshu = person.huaiyuntianshu,
                ManualStudy = person.ManualStudy,
                ClosePersons = person.ClosePersons ?? new List<int>(),
                HatedPersons = person.HatedPersons ?? new List<int>(),
                
                // 🔥 修复：头像和列传
                PictureIndex = person.PictureIndex,
                PersonBiographyID = person.PersonBiographyID,
                
                // 🔥 新增：能力增益字段（修复统治变成0的问题）
                IncrementOfAgricultureAbility = person.IncrementOfAgricultureAbility,
                IncrementOfChallengeWinningChance = person.IncrementOfChallengeWinningChance,
                IncrementOfCommerceAbility = person.IncrementOfCommerceAbility,
                IncrementOfControversyWinningChance = person.IncrementOfControversyWinningChance,
                IncrementOfDominationAbility = person.IncrementOfDominationAbility,
                IncrementOfEnduranceAbility = person.IncrementOfEnduranceAbility,
                IncrementOfMoraleAbility = person.IncrementOfMoraleAbility,
                IncrementOfRecruitmentAbility = person.IncrementOfRecruitmentAbility,
                IncrementOfSpyDays = person.IncrementOfSpyDays,
                IncrementOfTechnologyAbility = person.IncrementOfTechnologyAbility,
                IncrementOfTrainingAbility = person.IncrementOfTrainingAbility,
                
                // 🔥 新增：能力比例增益字段
                RadiusIncrementOfInformation = person.RadiusIncrementOfInformation,
                RateIncrementOfAgricultureAbility = person.RateIncrementOfAgricultureAbility,
                RateIncrementOfCommerceAbility = person.RateIncrementOfCommerceAbility,
                RateIncrementOfConvince = person.RateIncrementOfConvince,
                RateIncrementOfDestroy = person.RateIncrementOfDestroy,
                RateIncrementOfDominationAbility = person.RateIncrementOfDominationAbility,
                RateIncrementOfEnduranceAbility = person.RateIncrementOfEnduranceAbility,
                RateIncrementOfGossip = person.RateIncrementOfGossip,
                RateIncrementOfInstigate = person.RateIncrementOfInstigate,
                RateIncrementOfJailBreakAbility = person.RateIncrementOfJailBreakAbility,
                RateIncrementOfMoraleAbility = person.RateIncrementOfMoraleAbility,
                RateIncrementOfRecruitmentAbility = person.RateIncrementOfRecruitmentAbility,
                RateIncrementOfSearch = person.RateIncrementOfSearch,
                RateIncrementOfTechnologyAbility = person.RateIncrementOfTechnologyAbility,
                RateIncrementOfTrainingAbility = person.RateIncrementOfTrainingAbility,
                
                // 🔥 新增：能力倍数字段
                MultipleOfAgricultureReputation = person.MultipleOfAgricultureReputation,
                MultipleOfAgricultureTechniquePoint = person.MultipleOfAgricultureTechniquePoint,
                MultipleOfCommerceReputation = person.MultipleOfCommerceReputation,
                MultipleOfCommerceTechniquePoint = person.MultipleOfCommerceTechniquePoint,
                MultipleOfDominationReputation = person.MultipleOfDominationReputation,
                MultipleOfDominationTechniquePoint = person.MultipleOfDominationTechniquePoint,
                MultipleOfEnduranceReputation = person.MultipleOfEnduranceReputation,
                MultipleOfEnduranceTechniquePoint = person.MultipleOfEnduranceTechniquePoint,
                MultipleOfMoraleReputation = person.MultipleOfMoraleReputation,
                MultipleOfMoraleTechniquePoint = person.MultipleOfMoraleTechniquePoint,
                MultipleOfRecruitmentReputation = person.MultipleOfRecruitmentReputation,
                MultipleOfRecruitmentTechniquePoint = person.MultipleOfRecruitmentTechniquePoint,
                MultipleOfTechnologyReputation = person.MultipleOfTechnologyReputation,
                MultipleOfTechnologyTechniquePoint = person.MultipleOfTechnologyTechniquePoint,
                MultipleOfTrainingReputation = person.MultipleOfTrainingReputation,
                MultipleOfTrainingTechniquePoint = person.MultipleOfTrainingTechniquePoint
            };
            
            return dto;
        }
        
        /// <summary>
        /// Convert Faction collection to FactionDTO collection
        /// </summary>
        private List<FactionDTO> ConvertFactions(FactionListWithQueue factions)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertFactions] 开始转换势力");
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertFactions] factions 参数: {(factions == null ? "null" : $"Count={factions.Count}")}");
            #endif
            
            if (factions == null || factions.Count == 0)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertFactions] ⚠️ 势力列表为空，返回空列表");
                #endif
                return [];
            }
            
            var result = factions.GetList().GameObjects.Cast<Faction>()
                .Select(f => ConvertFactionToDTO(f))
                .ToList();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertFactions] ✅ 转换完成，共 {result.Count} 个势力");
            if (result.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase.ConvertFactions] 前3个势力: {string.Join(", ", result.Take(3).Select(f => $"{f.Name}(ID:{f.ID})"))}");
            }
            #endif
            
            return result;
        }
        
        /// <summary>
        /// Convert a single Faction to FactionDTO
        /// </summary>
        public FactionDTO ConvertFactionToDTO(Faction faction)
        {
            if (faction == null)
                return null;
            
            var dto = new FactionDTO
            {
                ID = faction.ID,
                Name = faction.Name,
                ColorIndex = faction.ColorIndex,
                
                // Object references → ID references
                LeaderID = faction.Leader?.ID ?? -1,
                AdvisorID = faction.AdvisorID,  // 🔥 保存军师ID（2026-03-18）
                CapitalID = faction.Capital?.ID ?? -1,
                
                // Collection references → List<int>
                // Using LINQ Select directly creates List<int> efficiently
                ArchitectureIDs = faction.Architectures?.GetList().GameObjects.Cast<Architecture>().Select(a => a.ID).ToList() ?? new List<int>(),
                PersonIDs = faction.Persons?.GetList().GameObjects.Cast<Person>().Select(p => p.ID).ToList() ?? new List<int>(),
                MilitaryIDs = faction.Militaries?.GetList().GameObjects.Cast<Military>().Select(m => m.ID).ToList() ?? new List<int>(),
                LegionIDs = faction.Legions?.GetList().GameObjects.Cast<Legion>().Select(l => l.ID).ToList() ?? new List<int>(),
                TroopIDs = faction.Troops?.GetList().GameObjects.Cast<Troop>().Select(t => t.ID).ToList() ?? [],
                SectionIDs = faction.Sections?.GetList().GameObjects.Cast<Section>().Select(s => s.ID).ToList() ?? [],
                
                // 🔥 根本修复：保存 AvailableTechniquesString
                // 日期：2026-02-17
                // 问题：读档后势力技巧不显示为已拥有状态
                // 原因：FactionDTO 使用了不存在的 TechniqueIDs 字段，应该使用 AvailableTechniquesString
                AvailableTechniquesString = faction.AvailableTechniquesString,
                
                // 🔥 根本修复：保存 BaseMilitaryKindsString
                // 日期：2026-03-20
                // 问题：读档后势力兵种丢失，导致无法创建新编队
                // 原因：SaveDataPhase 忘记保存 BaseMilitaryKindsString 到 DTO
                BaseMilitaryKindsString = faction.BaseMilitaryKindsString,
                
                // 🔥 根本修复：保存 InformationsString
                // 日期：2026-03-20
                // 问题：读档后势力情报数据丢失（已探索区域、已知敌军位置）
                // 原因：SaveDataPhase 忘记保存 InformationsString 到 DTO
                InformationsString = faction.InformationsString,
                
                // 🔥 根本修复：保存 TransferingMilitariesString
                // 日期：2026-03-20
                // 问题：读档后运输中的部队丢失
                // 原因：SaveDataPhase 忘记保存 TransferingMilitariesString 到 DTO
                TransferingMilitariesString = faction.TransferingMilitariesString,
                
                // 🔥 根本修复：保存 RoutewaysString
                // 日期：2026-03-20
                // 问题：读档后运输路线丢失
                // 原因：SaveDataPhase 忘记保存 RoutewaysString 到 DTO
                RoutewaysString = faction.RoutewaysString,
                
                // 🔥 根本修复：保存 GetGeneratorPersonCountString
                // 日期：2026-03-20
                GetGeneratorPersonCountString = faction.GetGeneratorPersonCountString,
                
                // 🔥 根本修复：保存技巧研究相关字段
                // 日期：2026-03-20
                PlanTechniqueString = faction.PlanTechniqueString,
                PreferredTechniqueKinds = faction.PreferredTechniqueKinds ?? [],
                
                // 🔥 根本修复：保存地图残差字段
                // 日期：2026-03-20
                SecondTierXResidue = faction.SecondTierXResidue,
                SecondTierYResidue = faction.SecondTierYResidue,
                ThirdTierXResidue = faction.ThirdTierXResidue,
                ThirdTierYResidue = faction.ThirdTierYResidue,
                
                // 🔥 根本修复：保存军师建议系统字段
                // 日期：2026-03-20
                CurrentRoundSuggestion = (int)faction.CurrentRoundSuggestion,
                LastSuggestionCheckTurn = faction.LastSuggestionCheckTurn,
                LastTalentRecommendYear = faction.LastTalentRecommendYear,
                
                // Faction state
                Reputation = faction.Reputation,
                TechniquePoint = faction.TechniquePoint,
                IsAlien = faction.IsAlien,
                
                // 🔥 根本修复：保存势力官爵和朝廷贡献度
                // 日期：2026-03-13
                Guanjue = faction.guanjue,
                Chaotinggongxiandu = faction.chaotinggongxiandu,
                
                // Private members
                PrinceID = faction.PrinceID,
                ZhaoxianFailureCount = faction.ZhaoxianFailureCount,
                YearOfficialLimit = faction.YearOfficialLimit,
                
                // 🔥 新增：势力增益字段
                IncrementOfAntiCriticalStrikeChance = faction.IncrementOfAntiCriticalStrikeChance,
                IncrementOfChaosDaysAfterPhisicalAttack = faction.IncrementOfChaosDaysAfterPhisicalAttack,
                IncrementOfCombativityCeiling = faction.IncrementOfCombativityCeiling,
                IncrementOfCriticalStrikeChance = faction.IncrementOfCriticalStrikeChance,
                IncrementOfResistStratagemChance = faction.IncrementOfResistStratagemChance,
                IncrementOfRoutewayRadius = faction.IncrementOfRoutewayRadius,
                IncrementOfRoutewayWorkforce = faction.IncrementOfRoutewayWorkforce,
                IncrementOfStratagemSuccessChance = faction.IncrementOfStratagemSuccessChance,
                IncrementOfViewRadius = faction.IncrementOfViewRadius,
                
                RateIncrementOfTerrainRate = faction.RateIncrementOfTerrainRate,
                RateOfCombativityRecoveryAfterAttacked = faction.RateOfCombativityRecoveryAfterAttacked,
                RateOfCombativityRecoveryAfterStratagemFail = faction.RateOfCombativityRecoveryAfterStratagemFail,
                RateOfCombativityRecoveryAfterStratagemSuccess = faction.RateOfCombativityRecoveryAfterStratagemSuccess,
                RateOfFoodTransportBetweenArchitectures = faction.RateOfFoodTransportBetweenArchitectures,
                RateOfRoutewayConsumption = faction.RateOfRoutewayConsumption
            };
            
            return dto;
        }
        
        /// <summary>
        /// Convert Architecture collection to ArchitectureDTO collection
        /// Handles polymorphic types (City, Port, Gate)
        /// </summary>
        private List<ArchitectureDTO> ConvertArchitectures(ArchitectureList architectures)
        {
            if (architectures == null || architectures.Count == 0)
                return new List<ArchitectureDTO>();
            
            return architectures.GetList().GameObjects.Cast<Architecture>()
                .Select(a => ConvertArchitectureToDTO(a))
                .ToList();
        }
        
        /// <summary>
        /// Convert a single Architecture to ArchitectureDTO
        /// Creates appropriate derived DTO based on actual type
        /// </summary>
        public ArchitectureDTO ConvertArchitectureToDTO(Architecture architecture)
        {
            if (architecture == null)
                return null;
            
            // Determine the actual type and create appropriate DTO
            ArchitectureDTO dto;
            
            // Check for derived types (simplified - adjust based on actual game logic)
            if (architecture.Kind?.Name?.Contains("城") == true || architecture.Kind?.Name?.Contains("City") == true)
            {
                dto = new CityDTO
                {
                    DevelopmentLevel = architecture.Agriculture + architecture.Commerce + architecture.Technology
                };
            }
            else if (architecture.Kind?.Name?.Contains("港") == true || architecture.Kind?.Name?.Contains("Port") == true)
            {
                dto = new PortDTO
                {
                    ShipCapacity = 100 // Placeholder - adjust based on actual property
                };
            }
            else if (architecture.Kind?.Name?.Contains("关") == true || architecture.Kind?.Name?.Contains("Gate") == true)
            {
                dto = new GateDTO
                {
                    DefenseBonus = architecture.Endurance / 10 // Placeholder
                };
            }
            else
            {
                dto = new ArchitectureDTO();
            }
            
            dto = CreateArchitectureDto(architecture);

            // Fill common properties
            dto.ID = architecture.ID;
            dto.Name = architecture.Name;
            dto.CaptionID = architecture.CaptionID;
            
            // 🔥 诊断：检查 Kind 是否为 null
            if (architecture.Kind == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [SaveDataPhase] Architecture {architecture.ID} ({architecture.Name}) Kind is NULL when saving! Will save KindID as -1");
            }
            if (architecture.Kind == null)
            {
                throw new InvalidOperationException(
                    $"[SaveDataPhase] Architecture {architecture.ID} ({architecture.Name}) has null Kind and cannot be serialized safely.");
            }
            dto.KindID = architecture.Kind.ID;
            
            // Location
             if (architecture.ArchitectureArea != null)
            {
                dto.AreaX = architecture.ArchitectureArea.TopLeft.X;
                dto.AreaY = architecture.ArchitectureArea.TopLeft.Y;
                dto.AreaWidth = 0; // GameArea does not expose Width
                dto.AreaHeight = 0; // GameArea does not expose Height
                
                // 🔥 关键修复：保存完整的 ArchitectureAreaString
                dto.ArchitectureAreaString = architecture.ArchitectureAreaString;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [SaveDataPhase] Architecture {architecture.ID} ({architecture.Name}) ArchitectureArea is NULL!");
            }
            
            // Object references → ID references
            dto.BelongedFactionID = architecture.BelongedFaction?.ID ?? -1;
            dto.BelongedSectionID = architecture.BelongedSection?.ID ?? -1;
            dto.MayorID = architecture.Mayor?.ID ?? -1;
            dto.StateID = architecture.LocationState?.ID ?? architecture.StateID;  // 🔥 修复：保存 StateID
            dto.huangdisuozai = architecture.huangdisuozai;
            
            // Resources
            dto.Agriculture = architecture.Agriculture;
            dto.Commerce = architecture.Commerce;
            dto.Technology = architecture.Technology;
            dto.Morale = architecture.Morale;
            dto.Endurance = architecture.Endurance;
            
            #if DEBUG
            // System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] 建筑: {architecture.Name}, architecture.Domination: {architecture.Domination}");
            #endif
            
            dto.Domination = architecture.Domination;  // 🔥 修复：保存统治度
            
            #if DEBUG
            // System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] 建筑: {architecture.Name}, dto.Domination: {dto.Domination}");
            #endif
            
            dto.Population = architecture.Population;
            dto.MilitaryPopulation = architecture.MilitaryPopulation;  // 🔥 修复：保存兵役人口
            dto.Fund = architecture.Fund;
            dto.Food = architecture.Food;
            
            // Collection references → List<int>
            // Using LINQ Select directly creates List<int> efficiently
            dto.PersonIDs = architecture.Persons?.GetList().GameObjects.Cast<Person>().Select(p => p.ID).ToList() ?? new List<int>();
            dto.MilitaryIDs = architecture.Militaries?.GetList().GameObjects.Cast<Military>().Select(m => m.ID).ToList() ?? new List<int>();
            dto.FacilityIDs = architecture.Facilities?.GetList().GameObjects.Cast<Facility>().Select(f => f.ID).ToList() ?? new List<int>();
            
            #if DEBUG
            // 🔥 诊断：检查编队ID是否为0
            if (dto.MilitaryIDs != null && dto.MilitaryIDs.Count > 0 && dto.MilitaryIDs.Any(id => id == 0))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [SaveDataPhase] 建筑 {architecture.Name}(ID:{architecture.ID}) 的编队ID包含0！");
                System.Diagnostics.Debug.WriteLine($"   MilitaryIDs: [{string.Join(", ", dto.MilitaryIDs)}]");
                System.Diagnostics.Debug.WriteLine($"   Militaries.Count: {architecture.Militaries?.Count ?? 0}");
                
                if (architecture.Militaries != null && architecture.Militaries.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"   编队详情:");
                    foreach (Military m in architecture.Militaries)
                    {
                        System.Diagnostics.Debug.WriteLine($"     - ID: {m.ID}, Name: '{m.Name ?? "null"}', KindID: {m.KindID}");
                    }
                }
            }
            #endif
            
            // 🔥 修复：复制字符串字段（向后兼容）
            dto.PersonsString = architecture.PersonsString;
            dto.MilitariesString = architecture.MilitariesString;
            dto.FacilitiesString = architecture.FacilitiesString;
            
            // 🔥 修复：复制 CharacteristicsString，避免读档后特色丢失
            // 日期：2026-02-16
            // 问题：ArchitectureDTO 缺少 CharacteristicsString 字段，导致特色数据无法序列化
            dto.CharacteristicsString = architecture.CharacteristicsString;
            
            // 🔥 根本修复：复制 AI 链接数据，避免每次读档重新生成（耗时54秒）
            // 日期：2026-02-15
            // 问题：ArchitectureDTO 缺少这两个字段，导致链接数据无法序列化
            dto.AILandLinksString = architecture.AILandLinksString;
            dto.AIWaterLinksString = architecture.AIWaterLinksString;
            
            // 🔥 完整性修复：复制所有缺失的字符串字段（2026-02-16）
            dto.FundPacksString = architecture.FundPacksString;
            dto.FoodPacksString = architecture.FoodPacksString;
            dto.InformationsString = architecture.InformationsString;
            dto.PopulationPacksString = architecture.PopulationPacksString;
            dto.MilitaryPopulationPacksString = architecture.MilitaryPopulationPacksString;
            dto.CaptivesString = architecture.CaptivesString;
            dto.MovingPersonsString = architecture.MovingPersonsString;
            dto.NoFactionPersonsString = architecture.NoFactionPersonsString;
            dto.NoFactionMovingPersonsString = architecture.NoFactionMovingPersonsString;
            dto.feiziliebiaoString = architecture.feiziliebiaoString;
            
            // Private members
            dto.AutoHiring = architecture.AutoHiring;
            dto.AutoRewarding = architecture.AutoRewarding;
            dto.AutoSearching = architecture.AutoSearching;
            dto.AutoWorking = architecture.AutoWorking;
            dto.FacilityEnabled = architecture.FacilityEnabled;  // 🔥 V15 修复：序列化 FacilityEnabled
            
            // 🔥 新增：建筑增益字段（修复统治变成0的问题）
            dto.IncrementOfAgricultureCeiling = architecture.IncrementOfAgricultureCeiling;
            dto.IncrementOfCommerceCeiling = architecture.IncrementOfCommerceCeiling;
            dto.IncrementOfDominationCeiling = architecture.IncrementOfDominationCeiling;
            dto.IncrementOfEnduranceCeiling = architecture.IncrementOfEnduranceCeiling;
            dto.IncrementOfMoraleCeiling = architecture.IncrementOfMoraleCeiling;
            dto.IncrementOfTechnologyCeiling = architecture.IncrementOfTechnologyCeiling;
            dto.IncrementOfFundCeiling = architecture.IncrementOfFundCeiling;
            dto.IncrementOfFoodCeiling = architecture.IncrementOfFoodCeiling;
            
            dto.IncrementOfAgriculturePerDay = architecture.IncrementOfAgriculturePerDay;
            dto.IncrementOfCommercePerDay = architecture.IncrementOfCommercePerDay;
            dto.IncrementOfDominationPerDay = architecture.IncrementOfDominationPerDay;
            dto.IncrementOfEndurancePerDay = architecture.IncrementOfEndurancePerDay;
            dto.IncrementOfMoralePerDay = architecture.IncrementOfMoralePerDay;
            dto.IncrementOfTechnologyPerDay = architecture.IncrementOfTechnologyPerDay;
            
            dto.IncrementOfMonthFood = architecture.IncrementOfMonthFood;
            dto.IncrementOfMonthFund = architecture.IncrementOfMonthFund;
            
            dto.IncrementOfCombativityInViewArea = architecture.IncrementOfCombativityInViewArea;
            dto.IncrementOfFacilityPositionCount = architecture.IncrementOfFacilityPositionCount;
            dto.IncrementOfFactionReputationPerDay = architecture.IncrementOfFactionReputationPerDay;
            dto.IncrementOfFactionTechniquePointPerDay = architecture.IncrementOfFactionTechniquePointPerDay;
            dto.IncrementOfViewRadius = architecture.IncrementOfViewRadius;
            
            return dto;
        }

        private static ArchitectureDTO CreateArchitectureDto(Architecture architecture)
        {
            ArchitectureDtoCategory category = ResolveArchitectureDtoCategory(architecture);
            return category switch
            {
                ArchitectureDtoCategory.City => new CityDTO
                {
                    DevelopmentLevel = architecture.Agriculture + architecture.Commerce + architecture.Technology
                },
                ArchitectureDtoCategory.Port => new PortDTO
                {
                    ShipCapacity = 100
                },
                ArchitectureDtoCategory.Gate => new GateDTO
                {
                    DefenseBonus = architecture.Endurance / 10
                },
                _ => throw new InvalidOperationException(
                    $"[SaveDataPhase] Unsupported architecture DTO category for architecture {architecture.ID} ({architecture.Name}).")
            };
        }

        private static ArchitectureDtoCategory ResolveArchitectureDtoCategory(Architecture architecture)
        {
            ArchitectureKind kind = architecture.Kind;
            if (kind == null)
            {
                throw new InvalidOperationException(
                    $"[SaveDataPhase] Architecture {architecture.ID} ({architecture.Name}) has null Kind and cannot be serialized safely.");
            }

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
                $"[SaveDataPhase] Architecture {architecture.ID} ({architecture.Name}) Kind {kind.ID} ({kind.Name}) cannot be classified to City/Port/Gate.");
        }

        private enum ArchitectureDtoCategory
        {
            City,
            Port,
            Gate
        }
        
        /// <summary>
        /// Convert Treasure collection to TreasureDTO collection
        /// </summary>
        private List<TreasureDTO> ConvertTreasures(TreasureList treasures)
        {
            if (treasures == null || treasures.Count == 0)
                return [];
            
            return treasures.GetList().GameObjects.Cast<Treasure>()
                .Select(t => new TreasureDTO
                {
                    ID = t.ID,
                    Name = t.Name,
                    Description = t.Description,
                    Worth = t.Worth,
                    Available = t.Available,
                    // 🔥 2026-02-17 修复：保存图片和基础属性
                    Pic = t.Pic,
                    AppearYear = t.AppearYear,
                    TreasureGroup = t.TreasureGroup,
                    Durability = t.Durability,
                    // Reference fields
                    // 🔥 2026-03-18 Person-Centric 模型：不再保存 BelongedPersonID
                    // 原因：宝物归属关系由 Person.TreasureIDs 管理，避免数据冗余
                    // 读档时通过 LinkReferencesPhase 从 Person.TreasureIDs 重建 Treasure.BelongedPerson
                    BelongedPersonID = -1,
                    HidePlaceID = t.HidePlace?.ID ?? -1,
                    // 🔥 2026-02-17 修复：保存 InfluencesString
                    InfluencesString = t.InfluencesString ?? string.Empty,
                    // Increments removed as they are not directly exposed in Treasure
                    CommandIncrement = 0,
                    StrengthIncrement = 0,
                    IntelligenceIncrement = 0,
                    PoliticsIncrement = 0,
                    GlamourIncrement = 0
                })
                .ToList();
        }
        
        /// <summary>
        /// Convert Legion collection to LegionDTO collection
        /// </summary>
        private List<LegionDTO> ConvertLegions(LegionList legions)
        {
            if (legions == null || legions.Count == 0)
                return new List<LegionDTO>();
            
            return legions.GetList().GameObjects.Cast<Legion>()
                .Select(l => ConvertLegionToDTO(l))
                .ToList();
        }
        
        /// <summary>
        /// Convert a single Legion to LegionDTO
        /// </summary>
        public LegionDTO ConvertLegionToDTO(Legion legion)
        {
            if (legion == null)
                return null;
            
            return new LegionDTO
            {
                ID = legion.ID,
                Name = legion.Name,
                BelongedFactionID = legion.BelongedFaction?.ID ?? -1,
                LeaderID = legion.Leader?.ID ?? -1,
                Kind = legion.Kind,
                Mission = legion.Mission,
                StartArchitectureID = legion.StartArchitecture?.ID ?? -1,
                WillArchitectureID = legion.WillArchitecture?.ID ?? -1,
                TargetArchitectureID = legion.TargetArchitectureID,
                // Using LINQ Select directly creates List<int> efficiently
                TroopIDs = legion.Troops?.GetList().GameObjects.Cast<Troop>().Select(t => t.ID).ToList() ?? new List<int>()
            };
        }
        
        /// <summary>
        /// Convert Troop collection to TroopDTO collection
        /// </summary>
        private List<TroopDTO> ConvertTroops(TroopListWithQueue troops)
        {
            if (troops == null || troops.Count == 0)
                return new List<TroopDTO>();
            
            return troops.GetList().GameObjects.Cast<Troop>()
                .Select(t => ConvertTroopToDTO(t))
                .ToList();
        }
        
        /// <summary>
        /// Convert a single Troop to TroopDTO
        /// </summary>
        public TroopDTO ConvertTroopToDTO(Troop troop)
        {
            if (troop == null)
                return null;
            
            return new TroopDTO
            {
                ID = troop.ID,
                Name = troop.Name,
                Status = (int)troop.Status,
                PositionX = troop.Position.X,
                PositionY = troop.Position.Y,
                RealDestinationX = troop.RealDestination.X,
                RealDestinationY = troop.RealDestination.Y,
                BelongedFactionID = troop.BelongedFaction?.ID ?? -1,
                BelongedLegionID = troop.BelongedLegion?.ID ?? -1,
                BelongedArchitectureID = troop.BelongedArchitecture?.ID ?? -1,
                StartingArchitectureID = troop.StartingArchitecture?.ID ?? -1,  // 🔥 修复：保存出发城市ID
                WillArchitectureID = troop.WillArchitectureID,
                WillTroopID = troop.WillTroopID,
                TargetArchitectureID = troop.TargetArchitectureID,
                TargetTroopID = troop.TargetTroopID,
                LeaderID = troop.Leader?.ID ?? -1,
                MilitaryID = troop.MilitaryID,  // 🔥 根本修复：使用 MilitaryID 字段而不是 Army?.ID，避免延迟加载问题
                
                // 🔥 根本修复：保存控制状态，防止读档后AI错误控制玩家部队
                Auto = troop.Auto,
                Controllable = troop.Controllable,
                ManualControl = troop.ManualControl,
                
                // 🔥 根本修复：保存AI状态，防止读档后部队状态错误
                CurrentAIState = (int)troop.CurrentAIState,
                IsRetreatLocked = troop.IsRetreatLocked,
                CreationTurn = troop.CreationTurn,
                
                // 🔥 根本修复：保存角色，防止读档后战术角色丢失
                AssignedRole = troop.AssignedRole.HasValue ? (int?)troop.AssignedRole.Value : null,
                CurrentRole = (int)troop.CurrentRole,
                
                Quantity = troop.Quantity,
                Morale = troop.Morale,
                Combativity = troop.Combativity,
                Experience = troop.Experience,
                
                // 🔥 根本修复：保存粮食、特技状态
                // 日期：2026-02-12
                // 问题：这三个字段缺失导致读档后粮食变0、特技自动触发
                Food = troop.Food,
                CurrentStuntIDString = troop.CurrentStuntIDString,
                StuntDayLeft = troop.StuntDayLeft,
                CurrentCombatMethodID = troop.CurrentCombatMethodID,
                CurrentStratagemID = troop.CurrentStratagemID,
                AutoCombatMethodID = troop.AutoCombatMethodID,
                
                // Using LINQ Select directly creates List<int> efficiently
                PersonIDs = troop.Persons?.GetList().GameObjects.Cast<Person>().Select(p => p.ID).ToList() ?? [],
                
                // 🔥 新增：部队增益字段
                IncrementOfAvoidSurroundedChance = troop.IncrementOfAvoidSurroundedChance,
                IncrementOfRumourDay = troop.IncrementOfRumourDay,
                IncrementOfAttractDay = troop.IncrementOfAttractDay,
                IncrementOfChaosAfterSurroundAttackChance = troop.IncrementOfChaosAfterSurroundAttackChance,
                IncrementOfChaosDay = troop.IncrementOfChaosDay,
                IncrementOfInjuryRate = troop.IncrementOfInjuryRate,
                IncrementOfInvestigateRadius = troop.IncrementOfInvestigateRadius,
                IncrementOfMovability = troop.IncrementOfMovability,
                IncrementOfRationDays = troop.IncrementOfRationDays,
                IncrementOfStratagemRadius = troop.IncrementOfStratagemRadius,
                IncrementPerDayOfCombativity = troop.IncrementPerDayOfCombativity,
                IncrementPerDayOfMorale = troop.IncrementPerDayOfMorale,
                IncrementOfStuntDay = troop.IncrementOfStuntDay,
                IncrementOfSpeed = troop.IncrementOfSpeed,
                
                RateIncrementOfRateOnWater = troop.RateIncrementOfRateOnWater,
                RateIncrementOfTerrainRateOnCliff = troop.RateIncrementOfTerrainRateOnCliff,
                RateIncrementOfTerrainRateOnDesert = troop.RateIncrementOfTerrainRateOnDesert,
                RateIncrementOfTerrainRateOnForrest = troop.RateIncrementOfTerrainRateOnForrest,
                RateIncrementOfTerrainRateOnGrassland = troop.RateIncrementOfTerrainRateOnGrassland,
                RateIncrementOfTerrainRateOnMarsh = troop.RateIncrementOfTerrainRateOnMarsh,
                RateIncrementOfTerrainRateOnMountain = troop.RateIncrementOfTerrainRateOnMountain,
                RateIncrementOfTerrainRateOnPlain = troop.RateIncrementOfTerrainRateOnPlain,
                RateIncrementOfTerrainRateOnRidge = troop.RateIncrementOfTerrainRateOnRidge,
                RateIncrementOfTerrainRateOnWasteland = troop.RateIncrementOfTerrainRateOnWasteland,
                RateIncrementOfTerrainRateOnWater = troop.RateIncrementOfTerrainRateOnWater,
                
                IncrementOffenceRate = troop.IncrementOffenceRate,
                RateOfBoost = troop.RateOfBoost,
                RateOfCriticalArchitectureDamage = troop.RateOfCriticalArchitectureDamage,
                RateOfCriticalDamageReceived = troop.RateOfCriticalDamageReceived,
                RateOfDefence = troop.RateOfDefence,
                RateOfFireDamage = troop.RateOfFireDamage,
                RateOfFireProtection = troop.RateOfFireProtection,
                RateOfGongxin = troop.RateOfGongxin,
                RateOfInjuryOnCriticalStrike = troop.RateOfInjuryOnCriticalStrike,
                RateOfMovability = troop.RateOfMovability,
                RateOfOffence = troop.RateOfOffence,
                RateOfQibingDamage = troop.RateOfQibingDamage,
                
                AttackRangeIncreaseByInfluence = troop.AttackRangeIncreaseByInfluence,
                InCityOffenseRate = troop.InCityOffenseRate,
                MovabilityByViewArea = troop.MovabilityByViewArea,
                
                // 🔥 修复：保存部队命令（修复存档/读档后攻击命令丢失）
                // 日期：2026-03-21
                // 问题：ConvertTroopToDTO 未保存 mingling 字段，导致攻击命令在保存时丢失
                // 解决：保存 mingling 字段到 DTO
                Mingling = troop.mingling ?? ""
            };
        }
        
        /// <summary>
        /// Convert Section collection to SectionDTO collection
        /// </summary>
        private List<SectionDTO> ConvertSections(SectionList sections)
        {
            if (sections == null || sections.Count == 0)
                return new List<SectionDTO>();
            
            return sections.GetList().GameObjects.Cast<Section>()
                .Select(s => ConvertSectionToDTO(s))
                .ToList();
        }
        
        /// <summary>
        /// Convert a single Section to SectionDTO
        /// </summary>
        public SectionDTO ConvertSectionToDTO(Section section)
        {
            if (section == null)
                return null;
            
            return new SectionDTO
            {
                ID = section.ID,
                Name = section.Name,
                BelongedFactionID = section.BelongedFaction?.ID ?? -1,
                AIDetailID = section.AIDetail?.ID ?? -1,
                // 🔥 修复：保存都督ID
                // 日期：2026-02-17
                SectionLeaderID = section.SectionLeaderID,
                // 🔥 修复：保存方向目标ID
                // 日期：2026-02-17
                OrientationFactionID = section.OrientationFactionID,
                OrientationSectionID = section.OrientationSectionID,
                OrientationStateID = section.OrientationStateID,
                OrientationArchitectureID = section.OrientationArchitectureID,
                // 🔥 修复：直接访问 GameObjects，避免不必要的 GetList() 调用
                // 日期：2026-02-16
                // 问题：原代码使用 GetList() 创建新列表并复制元素（不必要的开销）
                // 解决：直接访问 section.Architectures.GameObjects（永远不为 null）
                ArchitectureIDs = section.Architectures.GameObjects.Cast<Architecture>().Select(a => a.ID).ToList(),
                
                // 🔥 修复：保存AI冷却状态（修复存档/读档后AI每回合都执行的问题）
                // 日期：2026-03-21
                // 问题：ConvertSectionToDTO 未保存AI冷却字段，导致存档时冷却状态丢失
                // 解决：保存所有AI冷却相关字段
                AiCooldownCounter = section.GetAiCooldownCounter(),
                IsDirty = section.GetIsDirty(),
                IsInitialized = section.GetIsInitialized(),
                UrgentCooldown = section.GetUrgentCooldown(),
                CreationTurn = section.GetCreationTurn()
            };
        }
        
        // Simplified conversion methods for supporting types
        private List<RegionDTO> ConvertRegions(RegionList regions)
        {
            if (regions == null || regions.Count == 0)
                return new List<RegionDTO>();
            
            return regions.GetList().GameObjects.Cast<Region>()
                .Select(r => new RegionDTO { ID = r.ID, Name = r.Name })
                .ToList();
        }
        
        /// <summary>
        /// Convert States collection to StateDTO collection
        /// </summary>
        private List<StateDTO> ConvertStates(global::GameObjects.ArchitectureDetail.StateList states)
        {
            if (states == null || states.Count == 0)
                return [];
            
            return states.GetList().GameObjects.Cast<global::GameObjects.ArchitectureDetail.State>()
                .Select(s => new StateDTO
                {
                    ID = s.ID,
                    Name = s.Name,
                    StateAdminID = s.StateAdminID,
                    LinkedRegionID = s.LinkedRegion?.ID ?? -1,
                    ArchitectureIDs = s.Architectures?.GetList().GameObjects.Cast<Architecture>()
                        .Select(a => a.ID).ToList() ?? [],
                    ContactStateIDs = s.ContactStates?.GetList().GameObjects.Cast<global::GameObjects.ArchitectureDetail.State>()
                        .Select(cs => cs.ID).ToList() ?? []
                })
                .ToList();
        }
        
        private List<RoutewayDTO> ConvertRouteways(RoutewayList routeways)
        {
            if (routeways == null || routeways.Count == 0)
                return new List<RoutewayDTO>();
            
            return routeways.GetList().GameObjects.Cast<Routeway>()
                .Select(r => new RoutewayDTO
                {
                    ID = r.ID,
                    StartArchitectureID = r.StartArchitecture?.ID ?? -1,
                    EndArchitectureID = r.EndArchitecture?.ID ?? -1,
                    // Level removed as it does not exist in Routeway
                    Building = r.Building
                })
                .ToList();
        }
        
        private List<MilitaryDTO> ConvertMilitaries(MilitaryList militaries)
        {
            if (militaries == null || militaries.Count == 0)
                return [];
            
            return militaries.GetList().GameObjects.Cast<Military>()
                .Select(m => new MilitaryDTO
                {
                    ID = m.ID,
                    Name = m.Name,
                    KindID = m.Kind?.ID ?? -1,
                    BelongedArchitectureID = m.BelongedArchitecture?.ID ?? -1,
                    BelongedFactionID = m.BelongedFaction?.ID ?? -1,
                    Quantity = m.Quantity,
                    Morale = m.Morale,
                    Combativity = m.Combativity,
                    Experience = m.Experience,
                    // 🔥 根本修复：保存 ShelledMilitaryID
                    // 日期：2026-02-11
                    // 问题：ShelledMilitaryID 从未被保存，导致加载后丢失包裹关系
                    // 解决：保存此字段，确保包裹关系正确恢复
                    ShelledMilitaryID = m.ShelledMilitaryID
                })
                .ToList();
        }
        
        private List<FacilityDTO> ConvertFacilities(FacilityList facilities)
        {
            if (facilities == null || facilities.Count == 0)
                return new List<FacilityDTO>();
            
            return facilities.GetList().GameObjects.Cast<Facility>()
                .Select(f => new FacilityDTO
                {
                    ID = f.ID,
                    KindID = f.Kind?.ID ?? -1,
                    BelongedArchitectureID = f.location?.ID ?? -1,
                    Endurance = f.Endurance
                    // MainFacility removed as it does not exist in Facility
                })
                .ToList();
        }
        
        private List<InformationDTO> ConvertInformations(InformationList informations)
        {
            if (informations == null || informations.Count == 0)
                return [];
            
            return [.. informations.GetList().GameObjects.Cast<Information>()
                .Select(i => new InformationDTO
                {
                    ID = i.ID,
                    KindID = -1, // Information does not have Kind assignment
                    BelongedFactionID = i.BelongedFaction?.ID ?? -1,
                    BelongedArchitectureID = i.BelongedArchitecture?.ID ?? -1,  // 🔥 保存归属建筑
                    PositionX = i.Position.X,
                    PositionY = i.Position.Y,
                    Level = (int)i.Level,
                    Oblique = i.Oblique,           // 🔥 保存斜向范围
                    Radius = i.Radius,             // 🔥 保存情报半径
                    DayCost = i.DayCost,           // 🔥 保存每日消耗
                    DaysLeft = i.DaysLeft,         // 🔥 保存剩余天数
                    DaysStarted = i.DaysStarted    // 🔥 保存开始天数
                })];
        }
        
        private List<TroopEventDTO> ConvertTroopEvents(TroopEventList troopEvents)
        {
            if (troopEvents == null || troopEvents.Count == 0)
                return [];
            
            return [.. troopEvents.GetList().GameObjects.Cast<TroopEvent>()
                .Select(e => new TroopEventDTO
                {
                    ID = e.ID,
                    Name = e.Name,
                    
                    // 🔥 2026-03-16 修复：Happened 现在是 bool 类型，直接赋值
                    // 之前：Happened = e.Happened ? 1 : 0 (转换为 int)
                    // 现在：Happened = e.Happened (bool 类型)
                    Happened = e.Happened,
                    
                    Repeatable = e.Repeatable,
                    
                    // 🔥 2026-03-06 修复：保存所有字段
                    AfterEventHappened = e.AfterEventHappened,
                    CheckArea = (int)e.CheckArea,
                    ConditionsString = e.ConditionsString,
                    DialogString = e.dialogString,
                    EffectAreasString = e.EffectAreasString,
                    EffectPersonsString = e.EffectPersonsString,
                    LaunchPersonString = e.LaunchPersonString,
                    SelfEffectsString = e.SelfEffectsString,
                    TargetPersonsString = e.TargetPersonsString,
                    Image = e.Image,
                    Sound = e.Sound,
                    TryToShowString = e.TryToShowString,
                    HappenChance = e.HappenChance
                })];
        }
        
        private List<CaptiveDTO> ConvertCaptives(CaptiveList captives)
        {
            if (captives == null || captives.Count == 0)
                return [];
            
            return [.. captives.GetList().GameObjects.Cast<Captive>()
                .Select(c => new CaptiveDTO
                {
                    ID = c.ID,
                    CaptivePersonID = c.CaptivePerson?.ID ?? -1,
                    BelongedFactionID = c.BelongedFaction?.ID ?? -1,
                    LocationArchitectureID = c.LocationArchitecture?.ID ?? -1,
                    RansomArchitectureID = c.RansomArchitecture?.ID ?? -1,
                    // 🔥 2026-03-06 修复：添加缺失字段
                    RansomArriveDays = c.RansomArriveDays,
                    RansomFund = c.RansomFund
                })];
        }
        
        private List<EventDTO> ConvertEvents(EventList events)
        {
            if (events == null || events.Count == 0)
                return [];
            
            // 🔥 C# 12: 使用集合表达式和目标类型new
            return [.. events.GetList().GameObjects.Cast<Event>()
                .Select(e => new EventDTO
                {
                    ID = e.ID,
                    Name = e.Name,
                    
                    // 🔥 2026-03-16 修复：Happened 现在是 bool 类型，直接赋值
                    // 之前：Happened = e.happened ? 1 : 0 (转换为 int)
                    // 现在：Happened = e.happened (bool 类型)
                    Happened = e.happened,
                    
                    Repeatable = e.repeatable,
                    LaunchYear = e.StartYear,
                    
                    // 🔥 2026-03-06 修复：保存所有字符串字段
                    AfterEventHappened = e.AfterEventHappened,
                    HappenChance = e.happenChance,
                    NextScenario = e.nextScenario,
                    PersonString = e.personString,
                    PersonCondString = e.PersonCondString,
                    ArchitectureString = e.architectureString,
                    ArchitectureCondString = e.architectureCondString,
                    FactionString = e.factionString,
                    FactionCondString = e.factionCondString,
                    DialogString = e.dialogString,
                    EffectString = e.effectString,
                    YesDialogString = e.yesdialogString,
                    NoDialogString = e.nodialogString,
                    YesEffectString = e.yesEffectString,
                    NoEffectString = e.noEffectString,
                    ArchitectureEffectString = e.architectureEffectString,
                    FactionEffectIDString = e.factionEffectIDString,
                    YesArchitectureEffectString = e.yesArchitectureEffectString,
                    NoArchitectureEffectString = e.noArchitectureEffectString,
                    ScenBiographyString = e.scenBiographyString,
                    Image = e.Image,
                    Sound = e.Sound,
                    GloballyDisplayed = e.GloballyDisplayed,
                    StartMonth = e.StartMonth,
                    EndYear = e.EndYear,
                    EndMonth = e.EndMonth,
                    Minor = e.Minor,
                    TryToShowString = e.TryToShowString
                })];
        }
        
        /// <summary>
        /// Convert Biography collection to BiographyDTO collection
        /// 🔥 修复：添加Biography序列化支持
        /// </summary>
        private List<BiographyDTO> ConvertBiographies(global::GameObjects.PersonDetail.BiographyTable biographies)
        {
            if (biographies == null || biographies.Count == 0)
                return new List<BiographyDTO>();
            
            var result = new List<BiographyDTO>();
            foreach (var bio in biographies.Biographys.Values)
            {
                var dto = new BiographyDTO
                {
                    ID = bio.ID,
                    Name = bio.Name,
                    Brief = bio.Brief,
                    FactionColor = bio.FactionColor,
                    History = bio.History,
                    Romance = bio.Romance,
                    InGame = bio.InGame,
                    MilitaryKindsString = bio.MilitaryKindsString,
                    MilitaryKindIDs = bio.MilitaryKinds?.MilitaryKinds?.Values.Select(m => m.ID).ToList() ?? new List<int>()
                };
                result.Add(dto);
            }
            
            // System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] 保存了 {result.Count} 条Biography数据");
            return result;
        }
        
        /// <summary>
        /// Convert Map to MapDTO
        /// </summary>
        private MapDTO ConvertMapToDTO(global::GameObjects.Map map)
        {
            if (map == null)
                return null;
            
            // 🔥 关键修复：在保存前同步 MapDataString
            if (map.MapData != null)
            {
                map.MapDataString = map.SaveToString();
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] 地图数据已同步，MapDataString 长度: {map.MapDataString?.Length ?? 0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDataPhase] ⚠️ MapData 为 null，无法生成 MapDataString");
            }
            
            return new MapDTO
            {
                MapName = map.MapName,
                MapDataString = map.MapDataString,
                MapDimensionsX = map.MapDimensions.X,
                MapDimensionsY = map.MapDimensions.Y,
                JumpPositionX = map.JumpPosition.X,
                JumpPositionY = map.JumpPosition.Y,
                TileWidthMin = map.TileWidthMin,
                TileWidthMax = map.TileWidthMax
            };
        }
    }
}
