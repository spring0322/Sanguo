using System;
using System.Collections.Frozen; // 引入 .NET 8 的冻结集合命名空间
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using Platforms;
using WorldOfTheThreeKingdoms.GameManager;

namespace WorldOfTheThreeKingdoms.GameGlobal;

[DataContract]
public class Parameters
{
    // --- 性能优化核心：使用 .NET 8 FrozenDictionary ---
    // FrozenDictionary 专为只读查找优化，初始化较慢（只做一次），但 TryGetValue 极快
    private static readonly FrozenDictionary<string, FieldInfo> CachedFields;

    static Parameters()
    {
        // 缓存所有实例字段，并冻结为 FrozenDictionary
        CachedFields = typeof(Parameters)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .ToFrozenDictionary(f => f.Name, f => f);
    }

    // --- 数据成员定义 (保持不变) ---
    [DataMember] public float AIArchitectureDamageRate = 1f;
    [DataMember] public float AIFoodRate = 1f;
    [DataMember] public AIDifficulty AIDifficulty = AIDifficulty.Normal;
    [DataMember] public float AIFundRate = 1f;
    [DataMember] public float AIRecruitmentSpeedRate = 1f;
    [DataMember] public float AITrainingSpeedRate = 1f;
    [DataMember] public float AITroopDefenceRate = 1f;
    [DataMember] public float AITroopOffenceRate = 1f;
    [DataMember] public float ArchitectureDamageRate = 1f;
    [DataMember] public int AIAntiStratagem = 0;
    [DataMember] public int AIAntiSurround = 0;
    [DataMember] public int BuyFoodAgriculture = 500;
    [DataMember] public int ChangeCapitalCost = 0x1388;
    [DataMember] public int ConvincePersonCost = 200;
    [DataMember] public float DefaultPopulationDevelopingRate = 6E-05f;
    [DataMember] public int DestroyArchitectureCost = 200;
    [DataMember] public int FindTreasureChance = 10;
    [DataMember] public float FireDamageScale = 0.5f;
    [DataMember] public float FollowedLeaderDefenceRateIncrement = 0.2f;
    [DataMember] public float FollowedLeaderOffenceRateIncrement = 0.2f;
    [DataMember] public float FoodRate = 1f;
    [DataMember] public int FoodToFundDivisor = 200;
    [DataMember] public float FundRate = 1f;
    [DataMember] public int FundToFoodMultiple = 50;
    [DataMember] public int GossipArchitectureCost = 200;
    [DataMember] public int JailBreakArchitectureCost = 200;
    [DataMember] public int InstigateArchitectureCost = 200;
    [DataMember] public int InternalFundCost = 5;
    [DataMember] public float InternalRate = 1f;
    [DataMember] public int LearnSkillDays = 30;
    [DataMember] public int LearnStuntDays = 60;
    [DataMember] public int LearnTitleDays = 90;
    [DataMember] public int SearchDays = 10;
    [DataMember] public int RecruitmentDomination = 40;  // 🔥 FIX: 降低征兵统治度要求，避免49停滞
    [DataMember] public int RecruitmentFundCost = 20;
    [DataMember] public int RecruitmentMorale = 100;
    [DataMember] public float RecruitmentRate = 1f;
    [DataMember] public int RewardPersonCost = 100;
    [DataMember] public int SellFoodCommerce = 500;
    [DataMember] public int SurroundArchitectureDominationUnit = 2;
    [DataMember] public float TrainingRate = 1f;
    [DataMember] public float TroopDamageRate = 1f;
    [DataMember] public float AIArchitectureDamageYearIncreaseRate = 0f;
    [DataMember] public float AIFoodYearIncreaseRate = 0f;
    [DataMember] public float AIFundYearIncreaseRate = 0f;
    [DataMember] public float AIRecruitmentSpeedYearIncreaseRate = 0f;
    [DataMember] public float AITrainingSpeedYearIncreaseRate = 0f;
    [DataMember] public float AITroopDefenceYearIncreaseRate = 0f;
    [DataMember] public float AITroopOffenceYearIncreaseRate = 0f;
    [DataMember] public float AIArmyExperienceYearIncreaseRate = 0f;
    [DataMember] public float AIOfficerExperienceYearIncreaseRate = 0f;
    [DataMember] public float AIAntiStratagemIncreaseRate = 0f;
    [DataMember] public float AIAntiSurroundIncreaseRate = 0f;
    [DataMember] public float AIOfficerExperienceRate = 1f;
    [DataMember] public float AIArmyExperienceRate = 1f;

    [DataMember] private float BasicAIArchitectureDamageRate = 1f;
    [DataMember] private float BasicAIFoodRate = 1f;
    [DataMember] private float BasicAIFundRate = 1f;
    [DataMember] private float BasicAIRecruitmentSpeedRate = 1f;
    [DataMember] private float BasicAITrainingSpeedRate = 1f;
    [DataMember] private float BasicAITroopDefenceRate = 1f;
    [DataMember] private float BasicAITroopOffenceRate = 1f;
    [DataMember] private float BasicAIArmyExperienceRate = 1f;
    [DataMember] private float BasicAIOfficerExperienceRate = 1f;
    [DataMember] private int BasicAIAntiStratagem = 0;
    [DataMember] private int BasicAIAntiSurround = 0;

    [DataMember] public float AIWeightUnificationAggression = 1.5f;
    [DataMember] public float AIWeightSelfPreservationDefense = 1.8f;
    [DataMember] public int AIWeightEliteTroopGradeThreshold = 3;
    [DataMember] public float AIWeightExpToPowerRatio = 0.001f;
    [DataMember] public int AIWeightHanLoyaltyAttackThreshold = 60;

    [DataMember] public bool AIUseNewFormationLogic = true;

    [DataMember] public float AIBackendArmyReserveCalmBraveDifferenceMultiply = 5;
    [DataMember] public float AIBackendArmyReserveAmbitionMultiply = 10;
    [DataMember] public float AIBackendArmyReserveAdd = 50;
    [DataMember] public float AIBackendArmyReserveMultiply = 1;
    [DataMember] public int AITradePeriod = 10;
    [DataMember] public int AITreasureChance = 10;
    [DataMember] public int AITreasureCountMax = 2;
    [DataMember] public float AITreasureCountCappedTitleLevelAdd = 0;
    [DataMember] public float AITreasureCountCappedTitleLevelMultiply = 1;
    [DataMember] public int AIGiveTreasureMaxWorth = 40;
    [DataMember] public float AIFacilityFundMonthWaitParam = 8;
    [DataMember] public float AIFacilityDestroyValueRate = 2;
    [DataMember] public float AIBuildHougongUnambitionProbWeight = 10;
    [DataMember] public float AIBuildHougongSpaceBuiltProbWeight = 5;
    [DataMember] public int AIBuildHougongMaxSizeAdd = 0;
    [DataMember] public int AIBuildHougongSkipSizeChance = 80;
    [DataMember] public int AINafeiUncreultyProbAdd = -1;
    [DataMember] public float AINafeiAbilityThresholdRate = 30000;
    [DataMember] public float AINafeiStealSpouseThresholdRateAdd = 0.5f;
    [DataMember] public float AINafeiStealSpouseThresholdRateMultiply = 1;
    [DataMember] public int AINafeiMaxAgeThresholdAdd = 30;
    [DataMember] public float AINafeiMaxAgeThresholdMultiply = 1;
    [DataMember] public float AINafeiSkipChanceAdd = 25;
    [DataMember] public float AINafeiSkipChanceMultiply = 15;
    [DataMember] public float AIRecruitPopulationCapMultiply = 90;
    [DataMember] public float AIRecruitPopulationCapBackendMultiply = 0.5f;
    [DataMember] public float AIRecruitPopulationCapHostilelineMultiply = 2.0f;
    [DataMember] public float AIRecruitPopulationCapStrategyTendencyMulitply = 0.2f;
    [DataMember] public float AIRecruitPopulationCapStrategyTendencyAdd = 0.2f;
    [DataMember] public int AINewMilitaryPopulationThresholdDivide = 30000;
    [DataMember] public int AINewMilitaryPersonThresholdDivide = 5;
    [DataMember] public int AIExecuteMaxUncreulty = 4;
    [DataMember] public float AIExecutePersonIdealToleranceMultiply = 15;
    [DataMember] public int FireStayProb = 20;
    [DataMember] public float FireSpreadProbMultiply = 1f;
    [DataMember] public int MinPregnantProb = 0;
    [DataMember] public float InternalExperienceRate = 1f;
    [DataMember] public float AbilityExperienceRate = 1f;
    [DataMember] public float ArmyExperienceRate = 1f;
    [DataMember] public float AIAttackChanceIfUnfull = 5;
    [DataMember] public int AIObeyStrategyTendencyChance = 90;
    [DataMember] public int AIOffendMaxDiplomaticRelationMultiply = 20;
    [DataMember] public float AIOffendDefendTroopAdd = 1.2f;
    [DataMember] public float AIOffendDefendTroopMultiply = 0.1f;
    [DataMember] public int AIOffendIgnoreReserveProbAmbitionMultiply = 5;
    [DataMember] public int AIOffendIgnoreReserveProbAmbitionAdd = -2;
    [DataMember] public int AIOffendIgnoreReserveProbBCDiffMultiply = 2;
    [DataMember] public int AIOffendIgnoreReserveProbBCDiffAdd = 10;
    [DataMember] public float AIOffendIgnoreReserveChanceTroopRatioAdd = -0.8f;
    [DataMember] public float AIOffendIgnoreReserveChanceTroopRatioMultiply = 100.0f;
    [DataMember] public int PrincessMaintainenceCost = 50;
    [DataMember] public int AIUniqueTroopFightingForceThreshold = 60000;
    [DataMember] public int LearnSkillSuccessRate = 0;
    [DataMember] public int LearnStuntSuccessRate = 75;
    [DataMember] public int LearnTitleSuccessRate = 0;
    [DataMember] public int AutoLearnSkillSuccessRate = 0;
    [DataMember] public int AutoLearnStuntSuccessRate = 0;
    [DataMember] public float MilitaryPopulationCap = 0.1f;
    [DataMember] public float MilitaryPopulationReloadQuantity = 1.0f;
    [DataMember] public int CloseThreshold = 500;
    [DataMember] public int HateThreshold = -500;
    [DataMember] public int VeryCloseThreshold = 2000;
    [DataMember] public int MaxAITroopCountCandidates = 1000;
    [DataMember] public float PopulationDevelopingRate = 1;
    [DataMember] public float CloseAbilityRate = 1.1F;
    [DataMember] public float VeryCloseAbilityRate = 1.2F;
    [DataMember] public int AIEncirclePlayerRate = 0;
    [DataMember] public float BasicAIExtraPerson = 0;
    [DataMember] public float AIExtraPerson = 0;
    [DataMember] public float AIExtraPersonIncreaseRate = 0;
    [DataMember] public int AITirednessDecrease = 0;
    [DataMember] public int InternalSurplusFactor = 10000000;
    [DataMember] public int MakeMarrigeIdealLimit = 5;
    [DataMember] public int MakeMarriageCost = 8000;
    [DataMember] public int NafeiCost = 50000;
    [DataMember] public int SelectPrinceCost = 50000;
    [DataMember] public int TransferCostPerMilitary = 2000;
    [DataMember] public int TransferFoodPerMilitary = 2000;
    [DataMember] public int AIEncircleRank = 0;
    [DataMember] public int AIEncircleVar = 0;
    [DataMember] public float RansomRate = 1.0f;
    [DataMember] public List<int> ExpandConditions = [];
    [DataMember] public float SearchPersonArchitectureCountPower = 0;
    [DataMember] public int DayInTurn = 1;
    [DataMember] public int MaxRelation = 10000;
    [DataMember] public float HougongRelationHateFactor = 3.0f;
    [DataMember] public int AIMaxFeizi = 3000;
    [DataMember] public int MaxReputationForRecruit = 3000000;
    [DataMember] public float TroopMoraleChange = 1.0f;
    [DataMember] public float RecruitPopualationDecreaseRate = 0.25f;
    [DataMember] public float AIOffensiveCampaignRequiredScaleFactor = 1.0f;
    [DataMember] public int PersonCap = 2000;
    [DataMember] public float PersonCapFalldown = 10;
    [DataMember] public int AlienTroopGain = 50;
    [DataMember] public int TrainAbilityCost = 50;
    [DataMember] public int TrainAbilityTiredness = 5;
    [DataMember] public int TrainAbilityAmount = 100;
    [DataMember] public int OfficerBaseSalary = 10;
    [DataMember] public float SalaryLoyaltyLoss = 0.2f;

    public Parameters Clone() => (Parameters)this.MemberwiseClone();

    /// <summary>
    /// 初始化游戏参数
    /// </summary>
    public void InitializeGameParameters(string str = "")
    {
        Parameters target;
        string xmlPath;

        if (str == string.Empty)
        {
            target = this;
            xmlPath = "Content/Data/GameParameters.xml";
        }
        else
        {
            target = Session.Current.Scenario.Parameters;
            xmlPath = str;
        }

        string xmlContent = Platform.Current.LoadText(xmlPath);
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root;

        if (root == null) return;

        foreach (var attr in root.Attributes())
        {
            // TryGetValue 在 FrozenDictionary 上是高度优化的
            if (!CachedFields.TryGetValue(attr.Name.LocalName, out var field)) continue;

            try
            {
                var valueStr = attr.Value;

                if (field.Name == nameof(ExpandConditions))
                {
                    List<int> list = [];
                    StaticMethods.LoadFromString(list, valueStr);
                    field.SetValue(target, list);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[InitializeGameParameters] ✅ 加载 ExpandConditions: {valueStr}");
                    System.Diagnostics.Debug.WriteLine($"[InitializeGameParameters]   - 解析后数量: {list.Count}");
                    System.Diagnostics.Debug.WriteLine($"[InitializeGameParameters]   - 内容: {string.Join(", ", list)}");
                    #endif
                    
                    continue;
                }

                if (field.FieldType.IsEnum)
                {
                    if (Enum.TryParse(field.FieldType, valueStr, true, out var enumVal))
                    {
                        field.SetValue(target, enumVal);
                    }
                    continue;
                }

                var typeCode = Type.GetTypeCode(field.FieldType);
                switch (typeCode)
                {
                    case TypeCode.Int32:
                        if (valueStr.Contains('E', StringComparison.OrdinalIgnoreCase) && 
                            decimal.TryParse(valueStr, NumberStyles.Any, null, out var deInt))
                        {
                            field.SetValue(target, (int)deInt);
                        }
                        else
                        {
                            field.SetValue(target, int.Parse(valueStr));
                        }
                        break;

                    case TypeCode.Single: 
                        if (valueStr.Contains('E', StringComparison.OrdinalIgnoreCase) && 
                            decimal.TryParse(valueStr, NumberStyles.Any, null, out var deFloat))
                        {
                            field.SetValue(target, (float)deFloat);
                        }
                        else
                        {
                            field.SetValue(target, float.Parse(valueStr));
                        }
                        break;

                    case TypeCode.Boolean:
                        field.SetValue(target, bool.Parse(valueStr));
                        break;

                    case TypeCode.String:
                        field.SetValue(target, valueStr);
                        break;
                }
            }
            catch
            {
                // 静默失败
            }
        }
    }

    public void InitBaseRates()
    {
        BasicAIFundRate = AIFundRate;
        BasicAIFoodRate = AIFoodRate;
        BasicAITroopOffenceRate = AITroopOffenceRate;
        BasicAITroopDefenceRate = AITroopDefenceRate;
        BasicAIArchitectureDamageRate = AIArchitectureDamageRate;
        BasicAITrainingSpeedRate = AITrainingSpeedRate;
        BasicAIRecruitmentSpeedRate = AIRecruitmentSpeedRate;
        BasicAIArmyExperienceRate = AIArmyExperienceRate;
        BasicAIOfficerExperienceRate = AIOfficerExperienceRate;
        BasicAIAntiStratagem = AIAntiStratagem;
        BasicAIAntiSurround = AIAntiSurround;
        BasicAIExtraPerson = AIExtraPerson;
    }

    public void DayEvent(int year)
    {
        AIFundRate = year * AIFundYearIncreaseRate + BasicAIFundRate;
        AIFoodRate = year * AIFoodYearIncreaseRate + BasicAIFoodRate;
        AITroopOffenceRate = year * AITroopOffenceYearIncreaseRate + BasicAITroopOffenceRate;
        AITroopDefenceRate = year * AITroopDefenceYearIncreaseRate + BasicAITroopDefenceRate;
        AIArchitectureDamageRate = year * AIArchitectureDamageYearIncreaseRate + BasicAIArchitectureDamageRate;
        AITrainingSpeedRate = year * AITrainingSpeedYearIncreaseRate + BasicAITrainingSpeedRate;
        AIRecruitmentSpeedRate = year * AIRecruitmentSpeedYearIncreaseRate + BasicAIRecruitmentSpeedRate;
        AIOfficerExperienceRate = year * AIOfficerExperienceYearIncreaseRate + BasicAIOfficerExperienceRate;
        AIArmyExperienceRate = year * AIArmyExperienceYearIncreaseRate + BasicAIArmyExperienceRate;
        AIAntiSurround = (int)(year * AIAntiSurroundIncreaseRate + BasicAIAntiSurround);
        AIAntiStratagem = (int)(year * AIAntiStratagemIncreaseRate + BasicAIAntiStratagem);
        AIExtraPerson = year * AIExtraPersonIncreaseRate + BasicAIExtraPerson;
    }

    public void SaveToXml()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("GameParameters",
                new XAttribute("AIDifficulty", AIDifficulty.ToString()),
                new XAttribute("FindTreasureChance", FindTreasureChance),
                new XAttribute("LearnSkillDays", LearnSkillDays),
                new XAttribute("LearnStuntDays", LearnStuntDays),
                new XAttribute("LearnTitleDays", LearnTitleDays),
                new XAttribute("SearchDays", SearchDays),
                new XAttribute("LearnSkillSuccessRate", LearnSkillSuccessRate),
                new XAttribute("LearnStuntSuccessRate", LearnStuntSuccessRate),
                new XAttribute("LearnTitleSuccessRate", LearnTitleSuccessRate),
                new XAttribute("FollowedLeaderOffenceRateIncrement", FollowedLeaderOffenceRateIncrement),
                new XAttribute("FollowedLeaderDefenceRateIncrement", FollowedLeaderDefenceRateIncrement),
                new XAttribute("InternalRate", InternalRate),
                new XAttribute("TrainingRate", TrainingRate),
                new XAttribute("RecruitmentRate", RecruitmentRate),
                new XAttribute("FundRate", FundRate),
                new XAttribute("FoodRate", FoodRate),
                new XAttribute("TroopDamageRate", TroopDamageRate),
                new XAttribute("ArchitectureDamageRate", ArchitectureDamageRate),
                new XAttribute("DefaultPopulationDevelopingRate", DefaultPopulationDevelopingRate),
                new XAttribute("BuyFoodAgriculture", BuyFoodAgriculture),
                new XAttribute("SellFoodCommerce", SellFoodCommerce),
                new XAttribute("FundToFoodMultiple", FundToFoodMultiple),
                new XAttribute("FoodToFundDivisor", FoodToFundDivisor),
                new XAttribute("InternalFundCost", InternalFundCost),
                new XAttribute("RecruitmentFundCost", RecruitmentFundCost),
                new XAttribute("RecruitmentDomination", RecruitmentDomination),
                new XAttribute("RecruitmentMorale", RecruitmentMorale),
                new XAttribute("ChangeCapitalCost", ChangeCapitalCost),
                new XAttribute("ConvincePersonCost", ConvincePersonCost),
                new XAttribute("RewardPersonCost", RewardPersonCost),
                new XAttribute("DestroyArchitectureCost", DestroyArchitectureCost),
                new XAttribute("InstigateArchitectureCost", InstigateArchitectureCost),
                new XAttribute("GossipArchitectureCost", GossipArchitectureCost),
                new XAttribute("JailBreakArchitectureCost", JailBreakArchitectureCost),
                new XAttribute("SurroundArchitectureDominationUnit", SurroundArchitectureDominationUnit),
                new XAttribute("FireDamageScale", FireDamageScale),
                new XAttribute("AIFundRate", AIFundRate),
                new XAttribute("AIFoodRate", AIFoodRate),
                new XAttribute("AITroopOffenceRate", AITroopOffenceRate),
                new XAttribute("AITroopDefenceRate", AITroopDefenceRate),
                new XAttribute("AIArchitectureDamageRate", AIArchitectureDamageRate),
                new XAttribute("AITrainingSpeedRate", AITrainingSpeedRate),
                new XAttribute("AIRecruitmentSpeedRate", AIRecruitmentSpeedRate),
                new XAttribute("AIFundYearIncreaseRate", AIFundYearIncreaseRate),
                new XAttribute("AIFoodYearIncreaseRate", AIFoodYearIncreaseRate),
                new XAttribute("AITroopOffenceYearIncreaseRate", AITroopOffenceYearIncreaseRate),
                new XAttribute("AITroopDefenceYearIncreaseRate", AITroopDefenceYearIncreaseRate),
                new XAttribute("AIArchitectureDamageYearIncreaseRate", AIArchitectureDamageYearIncreaseRate),
                new XAttribute("AITrainingSpeedYearIncreaseRate", AITrainingSpeedYearIncreaseRate),
                new XAttribute("AIRecruitmentSpeedYearIncreaseRate", AIRecruitmentSpeedYearIncreaseRate),
                new XAttribute("AIArmyExperienceYearIncreaseRate", AIArmyExperienceYearIncreaseRate),
                new XAttribute("AIOfficerExperienceYearIncreaseRate", AIOfficerExperienceYearIncreaseRate),
                new XAttribute("AIOfficerExperienceRate", AIOfficerExperienceRate),
                new XAttribute("AIArmyExperienceRate", AIArmyExperienceRate),
                new XAttribute("AIBackendArmyReserveCalmBraveDifferenceMultiply", AIBackendArmyReserveCalmBraveDifferenceMultiply),
                new XAttribute("AIBackendArmyReserveAmbitionMultiply", AIBackendArmyReserveAmbitionMultiply),
                new XAttribute("AIUseNewFormationLogic", AIUseNewFormationLogic),
                new XAttribute("AIBackendArmyReserveAdd", AIBackendArmyReserveAdd),
                new XAttribute("AIBackendArmyReserveMultiply", AIBackendArmyReserveMultiply),
                new XAttribute("AITradePeriod", AITradePeriod),
                new XAttribute("AITreasureChance", AITreasureChance),
                new XAttribute("AITreasureCountMax", AITreasureCountMax),
                new XAttribute("AITreasureCountCappedTitleLevelAdd", AITreasureCountCappedTitleLevelAdd),
                new XAttribute("AITreasureCountCappedTitleLevelMultiply", AITreasureCountCappedTitleLevelMultiply),
                new XAttribute("AIGiveTreasureMaxWorth", AIGiveTreasureMaxWorth),
                new XAttribute("AIFacilityFundMonthWaitParam", AIFacilityFundMonthWaitParam),
                new XAttribute("AIFacilityDestroyValueRate", AIFacilityDestroyValueRate),
                new XAttribute("AIBuildHougongUnambitionProbWeight", AIBuildHougongUnambitionProbWeight),
                new XAttribute("AIBuildHougongSpaceBuiltProbWeight", AIBuildHougongSpaceBuiltProbWeight),
                new XAttribute("AIBuildHougongMaxSizeAdd", AIBuildHougongMaxSizeAdd),
                new XAttribute("AIBuildHougongSkipSizeChance", AIBuildHougongSkipSizeChance),
                new XAttribute("AINafeiUncreultyProbAdd", AINafeiUncreultyProbAdd),
                new XAttribute("AINafeiAbilityThresholdRate", AINafeiAbilityThresholdRate),
                new XAttribute("AINafeiStealSpouseThresholdRateAdd", AINafeiStealSpouseThresholdRateAdd),
                new XAttribute("AINafeiStealSpouseThresholdRateMultiply", AINafeiStealSpouseThresholdRateMultiply),
                new XAttribute("AINafeiMaxAgeThresholdAdd", AINafeiMaxAgeThresholdAdd),
                new XAttribute("AINafeiMaxAgeThresholdMultiply", AINafeiMaxAgeThresholdMultiply),
                new XAttribute("AINafeiSkipChanceAdd", AINafeiSkipChanceAdd),
                new XAttribute("AINafeiSkipChanceMultiply", AINafeiSkipChanceMultiply),
                new XAttribute("AIRecruitPopulationCapMultiply", AIRecruitPopulationCapMultiply),
                new XAttribute("AIRecruitPopulationCapBackendMultiply", AIRecruitPopulationCapBackendMultiply),
                new XAttribute("AIRecruitPopulationCapHostilelineMultiply", AIRecruitPopulationCapHostilelineMultiply),
                new XAttribute("AIRecruitPopulationCapStrategyTendencyMulitply", AIRecruitPopulationCapStrategyTendencyMulitply),
                new XAttribute("AIRecruitPopulationCapStrategyTendencyAdd", AIRecruitPopulationCapStrategyTendencyAdd),
                new XAttribute("AINewMilitaryPopulationThresholdDivide", AINewMilitaryPopulationThresholdDivide),
                new XAttribute("AINewMilitaryPersonThresholdDivide", AINewMilitaryPersonThresholdDivide),
                new XAttribute("AIExecuteMaxUncreulty", AIExecuteMaxUncreulty),
                new XAttribute("AIExecutePersonIdealToleranceMultiply", AIExecutePersonIdealToleranceMultiply),
                new XAttribute("FireStayProb", FireStayProb),
                new XAttribute("FireSpreadProbMultiply", FireSpreadProbMultiply),
                new XAttribute("MinPregnantProb", MinPregnantProb),
                new XAttribute("InternalExperienceRate", InternalExperienceRate),
                new XAttribute("AbilityExperienceRate", AbilityExperienceRate),
                new XAttribute("ArmyExperienceRate", ArmyExperienceRate),
                new XAttribute("AIAttackChanceIfUnfull", AIAttackChanceIfUnfull),
                new XAttribute("AIObeyStrategyTendencyChance", AIObeyStrategyTendencyChance),
                new XAttribute("AIOffendMaxDiplomaticRelationMultiply", AIOffendMaxDiplomaticRelationMultiply),
                new XAttribute("AIOffendDefendTroopAdd", AIOffendDefendTroopAdd),
                new XAttribute("AIOffendDefendTroopMultiply", AIOffendDefendTroopMultiply),
                new XAttribute("AIOffendIgnoreReserveProbAmbitionMultiply", AIOffendIgnoreReserveProbAmbitionMultiply),
                new XAttribute("AIOffendIgnoreReserveProbAmbitionAdd", AIOffendIgnoreReserveProbAmbitionAdd),
                new XAttribute("AIOffendIgnoreReserveProbBCDiffMultiply", AIOffendIgnoreReserveProbBCDiffMultiply),
                new XAttribute("AIOffendIgnoreReserveProbBCDiffAdd", AIOffendIgnoreReserveProbBCDiffAdd),
                new XAttribute("AIOffendIgnoreReserveChanceTroopRatioAdd", AIOffendIgnoreReserveChanceTroopRatioAdd),
                new XAttribute("AIOffendIgnoreReserveChanceTroopRatioMultiply", AIOffendIgnoreReserveChanceTroopRatioMultiply),
                new XAttribute("PrincessMaintainenceCost", PrincessMaintainenceCost),
                new XAttribute("AIUniqueTroopFightingForceThreshold", AIUniqueTroopFightingForceThreshold),
                new XAttribute("MilitaryPopulationCap", MilitaryPopulationCap),
                new XAttribute("MilitaryPopulationReloadQuantity", MilitaryPopulationReloadQuantity),
                new XAttribute("CloseThreshold", CloseThreshold),
                new XAttribute("HateThreshold", HateThreshold),
                new XAttribute("VeryCloseThreshold", VeryCloseThreshold),
                new XAttribute("MaxAITroopCountCandidates", MaxAITroopCountCandidates),
                new XAttribute("PopulationDevelopingRate", PopulationDevelopingRate),
                new XAttribute("AIAntiStratagem", AIAntiStratagem),
                new XAttribute("AIAntiSurround", AIAntiSurround),
                new XAttribute("AIAntiSurroundIncreaseRate", AIAntiSurroundIncreaseRate),
                new XAttribute("AIAntiStratagemIncreaseRate", AIAntiStratagemIncreaseRate),
                new XAttribute("CloseAbilityRate", CloseAbilityRate),
                new XAttribute("VeryCloseAbilityRate", VeryCloseAbilityRate),
                new XAttribute("AIEncirclePlayerRate", AIEncirclePlayerRate),
                new XAttribute("InternalSurplusFactor", InternalSurplusFactor),
                new XAttribute("AIExtraPerson", AIExtraPerson),
                new XAttribute("AIExtraPersonIncreaseRate", AIExtraPersonIncreaseRate),
                new XAttribute("ExpandConditions", StaticMethods.SaveToString(ExpandConditions)),
                new XAttribute("SearchPersonArchitectureCountPower", SearchPersonArchitectureCountPower),
                new XAttribute("AIEncircleRank", AIEncircleRank),
                new XAttribute("AIEncircleVar", AIEncircleVar),
                new XAttribute("SelectPrinceCost", SelectPrinceCost),
                new XAttribute("TransferCostPerMilitary", TransferCostPerMilitary),
                new XAttribute("TransferFoodPerMilitary", TransferFoodPerMilitary),
                new XAttribute("AutoLearnSkillSuccessRate", AutoLearnSkillSuccessRate),
                new XAttribute("AutoLearnStuntSuccessRate", AutoLearnStuntSuccessRate),
                new XAttribute("RansomRate", RansomRate),
                new XAttribute("DayInTurn", DayInTurn),
                new XAttribute("MaxRelation", MaxRelation),
                new XAttribute("HougongRelationHateFactor", HougongRelationHateFactor),
                new XAttribute("AIMaxFeizi", AIMaxFeizi),
                new XAttribute("MaxReputationForRecruit", MaxReputationForRecruit),
                new XAttribute("TroopMoraleChange", TroopMoraleChange),
                new XAttribute("RecruitPopualationDecreaseRate", RecruitPopualationDecreaseRate),
                new XAttribute("MakeMarriageCost", MakeMarriageCost),
                new XAttribute("NafeiCost", NafeiCost),
                new XAttribute("MakeMarrigeIdealLimit", MakeMarrigeIdealLimit),
                new XAttribute("PersonCap", PersonCap),
                new XAttribute("PersonCapFalldown", PersonCapFalldown),
                new XAttribute("AlienTroopGain", AlienTroopGain),
                new XAttribute("TrainAbilityCost", TrainAbilityCost),
                new XAttribute("TrainAbilityTiredness", TrainAbilityTiredness),
                new XAttribute("TrainAbilityAmount", TrainAbilityAmount),
                new XAttribute("OfficerBaseSalary", OfficerBaseSalary),
                new XAttribute("SalaryLoyaltyLoss", SalaryLoyaltyLoss),
                new XAttribute("AIWeightUnificationAggression", AIWeightUnificationAggression),
                new XAttribute("AIWeightSelfPreservationDefense", AIWeightSelfPreservationDefense),
                new XAttribute("AIWeightEliteTroopGradeThreshold", AIWeightEliteTroopGradeThreshold),
                new XAttribute("AIWeightExpToPowerRatio", AIWeightExpToPowerRatio),
                new XAttribute("AIWeightHanLoyaltyAttackThreshold", AIWeightHanLoyaltyAttackThreshold)
                // 🔥 修复重复属性：移除重复的 AIUseNewFormationLogic
            )
        );

        Platform.Current.SaveUserFile("Content/Data/GameParameters.xml", doc.ToString(), true);
    }

    public void MigrateData()
    {
        if (MaxRelation == 0)
        {
            MaxRelation = 10000;
        }
        if (MaxReputationForRecruit == 0)
        {
            MaxReputationForRecruit = 3000000;
        }
        if (TroopMoraleChange == 0)
        {
            TroopMoraleChange = 1;
        }
        if (Session.Parameters.AIOffensiveCampaignRequiredScaleFactor == 0)
        {
            Session.Parameters.AIOffensiveCampaignRequiredScaleFactor = 1.0f;
        }
        if (PersonCap == 0)
        {
            PersonCap = 2000;
        }
    }
}