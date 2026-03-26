using System;
using System.Xml;
using System.Collections.Generic;
using System.Collections.Frozen; // 引入 .NET 8 Frozen 集合命名空间
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;
using System.Globalization;
using Platforms;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameGlobal;

[DataContract]
public class GlobalVariables
{
    // ========================================================================
    // 字段定义 (保持原有 DataMember 契约)
    // ========================================================================

    [DataMember] public bool WujiangYoukenengDuli = true;
    [DataMember] public bool LiangdaoXitong = false;
    [DataMember] public bool ShowGrid = false;
    [DataMember] public bool ShowHitBoxes = false;
    [DataMember] public bool EnableAIDebug = true; 
    [DataMember] public bool IsSkippingTurn = false; // 🔥 NEW: 快速跳过标志 
    [DataMember] public bool AdditionalPersonAvailable = false;
    [DataMember] public float ArchitectureLayerDepth = 0.8f;
    [DataMember] public const float BackgroundDepthOffset = -1E-05f;
    [DataMember] public const float BackTileAnimationLayerDepth = 0.75f;
    [DataMember] public bool CalculateAverageCostOfTiers = false;
    [DataMember] public bool CommonPersonAvailable = true;
    [DataMember] public const float ConmentTextDepth = 0.15f;
    [DataMember] public const float ContextMenuDepth = 0.1f;
    [DataMember] public const float ControlDepthOffset = -0.001f;
    [DataMember] public MapLayerKind CurrentMapLayer;
    [DataMember] public const float DialogDepth = 0.2f;
    [DataMember] public bool DrawMapVeil = true;
    [DataMember] public bool DrawTroopAnimation = true;
    [DataMember] public long FactionRunningTicksLimitInOneFrame = 0x186a0;
    [DataMember] public int FastBattleSpeed = 1;
    [DataMember] public const float FloatingPartDepth = 0.25f;
    [DataMember] public const float FrameContentDepth = 0.35f;
    [DataMember] public const float FrontTileAnimationLayerDepth = 0.65f;
    [DataMember] public string GameDifficulty;
    [DataMember] public const float GameFrameDepth = 0.4f;
    [DataMember] public bool HintPopulation = true;
    [DataMember] public bool HintPopulationUnder1000 = true;
    [DataMember] public bool IdealTendencyValid = true;
    [DataMember] public const float LayerDepthOffset = -0.01f;
    [DataMember] public bool LoadBackGroundMapTexture = false;
    [DataMember] public const float MapLayerDepth = 0.9f;
    [DataMember] public float MapScrollSpeed = 0.8f;
    [DataMember] public const float MapVeilLayerDepth = 0.6f;
    [DataMember] public const float MapViewSelectorDepth = 0.18f;
    [DataMember] public int MaxCountOfKnownPaths = 0x3e8;
    [DataMember] public const float MaxDepth = 1f;
    [DataMember] public int MaxTimeOfAnimationFrame = 0x19;
    [DataMember] public bool MilitaryKindSpeedValid = true;
    [DataMember] public const float MinDepth = 0f;
    [DataMember] public const float MinDepthOffset = -1E-06f;
    [DataMember] public const float MovableControlDepthOffset = -0.0002f;
    [DataMember] public bool MultipleResource = false;
    [DataMember] public bool NoHintOnSmallFacility = true;
    [DataMember] public const float PersonBubbleDepth = 0.45f;
    [DataMember] public bool? PersonNaturalDeath = true;
    [DataMember] public bool PlayBattleSound = true;
    [DataMember] public bool PlayerPersonAvailable = true;
    [DataMember] public bool PlayMusic = true;
    [DataMember] public bool PlayNormalSound = true;
    [DataMember] public bool PopulationRecruitmentLimit = true;
    [DataMember] public InformationLevel RoutewayInformationLevel = InformationLevel.低;
    [DataMember] public const float RoutewayLayerDepth = 0.85f;
    [DataMember] public bool RunWhileNotFocused = true;
    [DataMember] public InformationLevel ScoutRoutewayInformationLevel = InformationLevel.高;
    [DataMember] public const float ScreenBlindDepth = 0.43f;
    [DataMember] public const float SelectingLayerDepth = 0.5f;
    [DataMember] public const float SelectorDepth = 0.72f;
    [DataMember] public bool SingleSelectionOneClick = true;
    [DataMember] public bool SkyEye = false;
    [DataMember] public const float SurveyDepth = 0.05f;
    [DataMember] public const float TextDepthOffset = -0.0001f;
    [DataMember] public const float ToolBarDepth = 0.1f;
    [DataMember] public const float TroopLayerDepth = 0.7f;
    [DataMember] public int TroopMoveFrameCount = 10;
    [DataMember] public int TroopMoveLimitOnce = 5;
    [DataMember] public int TroopMoveSpeed = 1;
    [DataMember] public const float TroopTitleDepth = 0.47f;
    [DataMember] public bool PinPointAtPlayer = false;
    [DataMember] public bool IgnoreStrategyTendency = false;
    [DataMember] public bool createChildren = true;
    [DataMember] public int zainanfashengjilv = 3000;
    [DataMember] public bool doAutoSave = true;
    [DataMember] public bool createChildrenIgnoreLimit = true;
    [DataMember] public bool internalSurplusRateForPlayer = true;
    [DataMember] public bool internalSurplusRateForAI = false;
    [DataMember] public int getChildrenRate = 90;
    [DataMember] public int hougongGetChildrenRate = 90;
    [DataMember] public bool hougongAlienOnly = false;
    [DataMember] public int getRaisedSoliderRate = 90;
    [DataMember] public int AIExecutionRate = 500;
    [DataMember] public bool AIExecuteBetterOfficer = false;
    [DataMember] public int maxExperience = 10000;
    [DataMember] public bool lockChildrenLoyalty = true;
    [DataMember] public bool AIAutoTakeNoFactionCaptives = false;
    [DataMember] public bool AIAutoTakeNoFactionPerson = false;
    [DataMember] public bool AIAutoTakePlayerCaptives = false;
    [DataMember] public bool AIAutoTakePlayerCaptiveOnlyUnfull = false;
    [DataMember] public float TechniquePointMultiple = 1.0f;
    [DataMember] public bool PermitFactionMerge = true;
    [DataMember] public float LeadershipOffenceRate = 0.0f;
    [DataMember] public int DialogShowTime = 10;
    [DataMember] public bool LandArmyCanGoDownWater = true;
    [DataMember] public bool EnableResposiveThreading = false;
    [DataMember] public bool EnableCheat = false;
    [DataMember] public bool HardcoreMode = false;
    [DataMember] public bool UseQuadtreeOptimization = true;
    [DataMember] public int MaxAbility = 150;
    [DataMember] public int TirednessIncrease = 1;
    [DataMember] public int TirednessDecrease = 1;
    [DataMember] public bool EnableAgeAbilityFactor = true;
    [DataMember] public int TabListDetailLevel = 3;
    [DataMember] public bool EnableExtensions = false;
    [DataMember] public bool EncryptSave = false;
    [DataMember] public int AutoSaveFrequency = 30;
    [DataMember] public bool ShowChallengeAnimation = true;
    [DataMember] public bool PersonDieInChallenge = true;
    [DataMember] public int OfficerDieInBattleRate = 10;
    [DataMember] public int OfficerChildrenLimit = 20;
    [DataMember] public bool StopToControlOnAttack = true;
    [DataMember] public int MaxMilitaryExperience = 3000;
    [DataMember] public int FactionMilitaryLimt = 9000;
    [DataMember] public float ZhaoXianSuccessRate = 30;
    [DataMember] public int TroopTirednessDecrease = 10;
    [DataMember] public float CreateRandomOfficerChance = 5;
    [DataMember] public int ChildrenAvailableAge = 12;
    [DataMember] public float CreatedOfficerAbilityFactor = 0.8f;
    [DataMember] public float ChildrenAbilityFactor = 1.0f;
    [DataMember] public bool EnablePersonRelations = true;
    [DataMember] public int FriendlyDiplomacyThreshold = 300;
    [DataMember] public int SurroundFactor = 5;
    [DataMember] public bool FullScreen = false;
    [DataMember] public bool PermitQuanXiang = true;
    [DataMember] public bool PermitManualAwardTitleAutoLearn = false;
    [DataMember] public int zhaoxianOfficerMax = 500;
    [DataMember] public bool AIZhaoxianFixIdeal = false;
    [DataMember] public bool PlayerZhaoxianFixIdeal = false;
    [DataMember] public int FixedUnnaturalDeathAge = 80;
    [DataMember] public bool AIQuickBattle = false;
    [DataMember] public bool EnableAIDebugLog = false;
    [DataMember] public bool PlayerAutoSectionHasAIResourceBonus = false;
    [DataMember] public float ProhibitFactionAgainstDestroyer = 1.0f;
    [DataMember] public float AIMergeAgainstPlayer = -1f;
    [DataMember] public bool RemoveSpouseIfNotAvailable = false;
    [DataMember] public bool SkyEyeSimpleNotification = false;
    [DataMember] public bool AutoMultipleMarriage = false;
    [DataMember] public bool BornHistoricalChildren = false;
    [DataMember] public float StartCircleTime = 30f;
    [DataMember] public float ScenarioMapPerTime = 3f;
    [DataMember] public int KeepSpousePersonalLoyalty = 4;
    [DataMember] public bool TroopVoice = true;
    [DataMember] public int MaxTupianwenzi = 50;
    [DataMember] public int ShowNumberAddTime = 0;
    [DataMember] public bool EnableLoyaltyAbilityFactor = true;  // 忠诚度影响能力开关，默认打开
    [DataMember] public bool EnableWeatherParticles = true;  // 🌧️ 2026-03-10 新增：天气粒子系统开关，默认打开
    [DataMember] public bool EnableWegoEngine = false;  // 🔥 2026-03-16 新增：WEGO 引擎开关，默认关闭（测试阶段）
    [DataMember] public bool EnableCommandBufferScheduler = false;  // 🔥 2026-03-23 新增：CommandBuffer 调度器开关，默认关闭（灰度测试）
    [DataMember] public bool EnableAIAuthorityPhase1 = true;  // 2026-03-26 新增：阶段1收权模式开关，默认开启

    public const string cryptKey = "A3g0c3%2";

    // 优化点：使用 .NET 8 FrozenDictionary
    // FrozenDictionary 为只读场景优化，查找速度显著快于普通 Dictionary
    private static readonly FrozenDictionary<string, FieldInfo> _fieldCache;

    static GlobalVariables()
    {
        // 预先缓存所有公共字段，并冻结为不可变集合
        _fieldCache = typeof(GlobalVariables)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .ToFrozenDictionary(f => f.Name);
    }

    public GlobalVariables Clone()
    {
        return this.MemberwiseClone() as GlobalVariables;
    }

    public List<string> getFieldsExcludedFromSave()
    {
        return 
        [
            "MapScrollSpeed",
            "TroopMoveSpeed",
            "RunWhileNotFocused",
            "PlayMusic",
            "PlayNormalSound",
            "PlayBattleSound",
            "DrawMapVeil",
            "DrawTroopAnimation",
            "SingleSelectionOneClick",
            "NoHintOnSmallFacility",
            "HintPopulation",
            "HintPopulationUnder1000",
            "doAutoSave",
            "DialogShowTime",
            "FastBattleSpeed",
            "AutoSaveFrequency",
            "StopToControlOnAttack"
        ];
    }

    /// <summary>
    /// 初始化全局变量
    /// </summary>
    public bool InitialGlobalVariables(string str = "")
    {
        XmlDocument document = new XmlDocument();
        string xml;

        // Path A: 从默认文件加载
        if (string.IsNullOrEmpty(str))
        {
            xml = Platform.Current.LoadText("Content/Data/GlobalVariables.xml");
            document.LoadXml(xml);
            XmlNode nextSibling = document.FirstChild?.NextSibling ?? document.FirstChild;

            if (nextSibling == null) return false;

            ParseAttribute(nextSibling, "GameDifficulty", ref GameDifficulty);
            ParseAttribute(nextSibling, "MapScrollSpeed", ref MapScrollSpeed);
            ParseAttribute(nextSibling, "TroopMoveSpeed", ref TroopMoveSpeed);
            ParseAttribute(nextSibling, "RunWhileNotFocused", ref RunWhileNotFocused);
            ParseAttribute(nextSibling, "PlayMusic", ref PlayMusic);
            ParseAttribute(nextSibling, "PlayNormalSound", ref PlayNormalSound);
            ParseAttribute(nextSibling, "PlayBattleSound", ref PlayBattleSound);
            ParseAttribute(nextSibling, "DrawMapVeil", ref DrawMapVeil);
            ParseAttribute(nextSibling, "DrawTroopAnimation", ref DrawTroopAnimation);
            ParseAttribute(nextSibling, "SkyEye", ref SkyEye);
            ParseAttribute(nextSibling, "MultipleResource", ref MultipleResource);
            ParseAttribute(nextSibling, "SingleSelectionOneClick", ref SingleSelectionOneClick);
            ParseAttribute(nextSibling, "NoHintOnSmallFacility", ref NoHintOnSmallFacility);
            ParseAttribute(nextSibling, "HintPopulation", ref HintPopulation);
            ParseAttribute(nextSibling, "HintPopulationUnder1000", ref HintPopulationUnder1000);
            ParseAttribute(nextSibling, "PopulationRecruitmentLimit", ref PopulationRecruitmentLimit);
            ParseAttribute(nextSibling, "MilitaryKindSpeedValid", ref MilitaryKindSpeedValid);
            ParseAttribute(nextSibling, "CommonPersonAvailable", ref CommonPersonAvailable);
            ParseAttribute(nextSibling, "AdditionalPersonAvailable", ref AdditionalPersonAvailable);
            ParseAttribute(nextSibling, "PlayerPersonAvailable", ref PlayerPersonAvailable);
            ParseAttribute(nextSibling, "PersonNaturalDeath", ref PersonNaturalDeath);
            ParseAttribute(nextSibling, "IdealTendencyValid", ref IdealTendencyValid);
            ParseAttribute(nextSibling, "PinPointAtPlayer", ref PinPointAtPlayer);
            ParseAttribute(nextSibling, "IgnoreStrategyTendency", ref IgnoreStrategyTendency);
            ParseAttribute(nextSibling, "createChildren", ref createChildren);
            ParseAttribute(nextSibling, "zainanfashengjilv", ref zainanfashengjilv);
            ParseAttribute(nextSibling, "doAutoSave", ref doAutoSave);
            ParseAttribute(nextSibling, "createChildrenIgnoreLimit", ref createChildrenIgnoreLimit);
            ParseAttribute(nextSibling, "internalSurplusRateForPlayer", ref internalSurplusRateForPlayer);
            ParseAttribute(nextSibling, "internalSurplusRateForAI", ref internalSurplusRateForAI);
            ParseAttribute(nextSibling, "getChildrenRate", ref getChildrenRate);
            ParseAttribute(nextSibling, "hougongGetChildrenRate", ref hougongGetChildrenRate);
            ParseAttribute(nextSibling, "AIExecutionRate", ref AIExecutionRate);
            ParseAttribute(nextSibling, "AIExecuteBetterOfficer", ref AIExecuteBetterOfficer);
            ParseAttribute(nextSibling, "maxExperience", ref maxExperience);
            ParseAttribute(nextSibling, "lockChildrenLoyalty", ref lockChildrenLoyalty);
            ParseAttribute(nextSibling, "AIAutoTakeNoFactionCaptives", ref AIAutoTakeNoFactionCaptives);
            ParseAttribute(nextSibling, "AIAutoTakeNoFactionPerson", ref AIAutoTakeNoFactionPerson);
            ParseAttribute(nextSibling, "AIAutoTakePlayerCaptives", ref AIAutoTakePlayerCaptives);
            ParseAttribute(nextSibling, "AIAutoTakePlayerCaptiveOnlyUnfull", ref AIAutoTakePlayerCaptiveOnlyUnfull);
            ParseAttribute(nextSibling, "DialogShowTime", ref DialogShowTime);
            ParseAttribute(nextSibling, "TechniquePointMultiple", ref TechniquePointMultiple);
            ParseAttribute(nextSibling, "PermitFactionMerge", ref PermitFactionMerge);
            ParseAttribute(nextSibling, "LeadershipOffenceRate", ref LeadershipOffenceRate);
            ParseAttribute(nextSibling, "LiangdaoXitong", ref LiangdaoXitong);
            ParseAttribute(nextSibling, "WujiangYoukenengDuli", ref WujiangYoukenengDuli);
            ParseAttribute(nextSibling, "FastBattleSpeed", ref FastBattleSpeed);
            ParseAttribute(nextSibling, "HardcoreMode", ref HardcoreMode);
            ParseAttribute(nextSibling, "LandArmyCanGoDownWater", ref LandArmyCanGoDownWater);
            ParseAttribute(nextSibling, "MaxAbility", ref MaxAbility);
            ParseAttribute(nextSibling, "TirednessIncrease", ref TirednessIncrease);
            ParseAttribute(nextSibling, "TirednessDecrease", ref TirednessDecrease);
            ParseAttribute(nextSibling, "EnableAgeAbilityFactor", ref EnableAgeAbilityFactor);
            ParseAttribute(nextSibling, "TabListDetailLevel", ref TabListDetailLevel);
            ParseAttribute(nextSibling, "EnableExtensions", ref EnableExtensions);
            ParseAttribute(nextSibling, "EncryptSave", ref EncryptSave);
            ParseAttribute(nextSibling, "AutoSaveFrequency", ref AutoSaveFrequency);
            ParseAttribute(nextSibling, "ShowChallengeAnimation", ref ShowChallengeAnimation);
            ParseAttribute(nextSibling, "PersonDieInChallenge", ref PersonDieInChallenge);
            ParseAttribute(nextSibling, "OfficerDieInBattleRate", ref OfficerDieInBattleRate);
            ParseAttribute(nextSibling, "OfficerChildrenLimit", ref OfficerChildrenLimit);
            ParseAttribute(nextSibling, "StopToControlOnAttack", ref StopToControlOnAttack);
            ParseAttribute(nextSibling, "MaxMilitaryExperience", ref MaxMilitaryExperience);
            ParseAttribute(nextSibling, "CreateRandomOfficerChance", ref CreateRandomOfficerChance);
            ParseAttribute(nextSibling, "ZhaoXianSuccessRate", ref ZhaoXianSuccessRate);
            ParseAttribute(nextSibling, "CreatedOfficerAbilityFactor", ref CreatedOfficerAbilityFactor);
            ParseAttribute(nextSibling, "EnablePersonRelations", ref EnablePersonRelations);
            ParseAttribute(nextSibling, "ChildrenAvailableAge", ref ChildrenAvailableAge);
            ParseAttribute(nextSibling, "FullScreen", ref FullScreen);
            ParseAttribute(nextSibling, "FriendlyDiplomacyThreshold", ref FriendlyDiplomacyThreshold);
            ParseAttribute(nextSibling, "SurroundFactor", ref SurroundFactor);
            ParseAttribute(nextSibling, "PermitQuanXiang", ref PermitQuanXiang);
            ParseAttribute(nextSibling, "PermitManualAwardTitleAutoLearn", ref PermitManualAwardTitleAutoLearn);
            ParseAttribute(nextSibling, "zhaoxianOfficerMax", ref zhaoxianOfficerMax);
            ParseAttribute(nextSibling, "FactionMilitaryLimt", ref FactionMilitaryLimt);
            ParseAttribute(nextSibling, "FixedUnnaturalDeathAge", ref FixedUnnaturalDeathAge);
            ParseAttribute(nextSibling, "AIQuickBattle", ref AIQuickBattle);
            
            EnableAIDebugLog = ParseSafe(nextSibling, "EnableAIDebugLog", false);
            PlayerAutoSectionHasAIResourceBonus = ParseSafe(nextSibling, "PlayerAutoSectionHasAIResourceBonus", false);

            ParseAttribute(nextSibling, "ProhibitFactionAgainstDestroyer", ref ProhibitFactionAgainstDestroyer);
            ParseAttribute(nextSibling, "AIMergeAgainstPlayer", ref AIMergeAgainstPlayer);
            ParseAttribute(nextSibling, "RemoveSpouseIfNotAvailable", ref RemoveSpouseIfNotAvailable);
            ParseAttribute(nextSibling, "SkyEyeSimpleNotification", ref SkyEyeSimpleNotification);
            ParseAttribute(nextSibling, "AutoMultipleMarriage", ref AutoMultipleMarriage);
            ParseAttribute(nextSibling, "BornHistoricalChildren", ref BornHistoricalChildren);
            ParseAttribute(nextSibling, "hougongAlienOnly", ref hougongAlienOnly);

            TryParseAttribute(nextSibling, "StartCircleTime", ref StartCircleTime);
            TryParseAttribute(nextSibling, "ScenarioMapPerTime", ref ScenarioMapPerTime);
            TryParseAttribute(nextSibling, "KeepSpousePersonalLoyalty", ref KeepSpousePersonalLoyalty);
            TryParseAttribute(nextSibling, "ShowNumberAddTime", ref ShowNumberAddTime);
            TryParseAttribute(nextSibling, "TroopVoice", ref TroopVoice);
            TryParseAttribute(nextSibling, "MaxTupianwenzi", ref MaxTupianwenzi);
            TryParseAttribute(nextSibling, "EnableAIAuthorityPhase1", ref EnableAIAuthorityPhase1);
            TryParseAttribute(nextSibling, "EnableWeatherParticles", ref EnableWeatherParticles); // 🌧️ 2026-03-10 新增
        }
        // Path B: 从传入的 XML 字符串加载 (Reflection Optimized with FrozenDictionary)
        else
        {
            xml = Platform.Current.LoadText(str);
            document.LoadXml(xml);
            XmlNode nextSibling = document.FirstChild?.NextSibling ?? document.FirstChild;

            if (nextSibling != null && nextSibling.Attributes != null)
            {
                var target = Session.Current.Scenario.GlobalVariables;

                foreach (XmlAttribute attr in nextSibling.Attributes)
                {
                    // 使用 FrozenDictionary 进行超快速查找
                    if (_fieldCache.TryGetValue(attr.Name, out var fieldInfo))
                    {
                        var valStr = attr.Value;
                        var fieldType = fieldInfo.FieldType;

                        try
                        {
                            if (fieldType == typeof(int))
                            {
                                if (valStr.Contains('E')) 
                                {
                                    if (decimal.TryParse(valStr, NumberStyles.Any, null, out decimal de))
                                    {
                                        fieldInfo.SetValue(target, (int)de);
                                    }
                                }
                                else
                                {
                                    fieldInfo.SetValue(target, int.Parse(valStr));
                                }
                            }
                            else if (fieldType == typeof(float))
                            {
                                if (valStr.Contains('E'))
                                {
                                    if (decimal.TryParse(valStr, NumberStyles.Any, null, out decimal de))
                                    {
                                        fieldInfo.SetValue(target, (float)de);
                                    }
                                }
                                else
                                {
                                    fieldInfo.SetValue(target, float.Parse(valStr));
                                }
                            }
                            else if (fieldType == typeof(bool))
                            {
                                fieldInfo.SetValue(target, bool.Parse(valStr));
                            }
                            else if (fieldType == typeof(string))
                            {
                                fieldInfo.SetValue(target, valStr);
                            }

                            if (fieldInfo.Name == "PersonNaturalDeath")
                            {
                                target.PersonNaturalDeath = bool.Parse(valStr);
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }

        EnableCheat = false;

        return true;
    }

    public void SaveToXml()
    {
        XmlDocument document = new XmlDocument();

        XmlNode docNode = document.CreateXmlDeclaration("1.0", "utf-8", null);
        document.AppendChild(docNode);

        XmlElement element = document.CreateElement("GlobalVariables");
        
        void Set(string name, object val) => element.SetAttribute(name, val.ToString());

        Set(nameof(MapScrollSpeed), MapScrollSpeed);
        Set(nameof(TroopMoveSpeed), TroopMoveSpeed);
        Set(nameof(RunWhileNotFocused), RunWhileNotFocused);
        Set(nameof(PlayMusic), PlayMusic);
        Set(nameof(PlayNormalSound), PlayNormalSound);
        Set(nameof(PlayBattleSound), PlayBattleSound);
        Set(nameof(DrawMapVeil), DrawMapVeil);
        Set(nameof(DrawTroopAnimation), DrawTroopAnimation);
        Set(nameof(SkyEye), SkyEye);
        Set(nameof(MultipleResource), MultipleResource);
        Set(nameof(SingleSelectionOneClick), SingleSelectionOneClick);
        Set(nameof(NoHintOnSmallFacility), NoHintOnSmallFacility);
        Set(nameof(HintPopulation), HintPopulation);
        Set(nameof(HintPopulationUnder1000), HintPopulationUnder1000);
        Set(nameof(PopulationRecruitmentLimit), PopulationRecruitmentLimit);
        Set(nameof(MilitaryKindSpeedValid), MilitaryKindSpeedValid);
        Set(nameof(CommonPersonAvailable), CommonPersonAvailable);
        Set(nameof(AdditionalPersonAvailable), AdditionalPersonAvailable);
        Set(nameof(PlayerPersonAvailable), PlayerPersonAvailable);
        Set(nameof(PersonNaturalDeath), PersonNaturalDeath);
        Set(nameof(IdealTendencyValid), IdealTendencyValid);
        Set(nameof(PinPointAtPlayer), PinPointAtPlayer);
        Set(nameof(IgnoreStrategyTendency), IgnoreStrategyTendency);
        Set(nameof(createChildren), createChildren);
        Set(nameof(zainanfashengjilv), zainanfashengjilv);
        Set(nameof(doAutoSave), doAutoSave);
        Set(nameof(createChildrenIgnoreLimit), createChildrenIgnoreLimit);
        Set(nameof(internalSurplusRateForPlayer), internalSurplusRateForPlayer);
        Set(nameof(internalSurplusRateForAI), internalSurplusRateForAI);
        Set(nameof(getChildrenRate), getChildrenRate);
        Set(nameof(hougongGetChildrenRate), hougongGetChildrenRate);
        Set(nameof(AIExecutionRate), AIExecutionRate);
        Set(nameof(AIExecuteBetterOfficer), AIExecuteBetterOfficer);
        Set(nameof(maxExperience), maxExperience);
        Set(nameof(lockChildrenLoyalty), lockChildrenLoyalty);
        Set(nameof(AIAutoTakeNoFactionCaptives), AIAutoTakeNoFactionCaptives);
        Set(nameof(AIAutoTakeNoFactionPerson), AIAutoTakeNoFactionPerson);
        Set(nameof(AIAutoTakePlayerCaptives), AIAutoTakePlayerCaptives);
        Set(nameof(AIAutoTakePlayerCaptiveOnlyUnfull), AIAutoTakePlayerCaptiveOnlyUnfull);
        Set(nameof(DialogShowTime), DialogShowTime);
        Set(nameof(TechniquePointMultiple), TechniquePointMultiple);
        Set(nameof(PermitFactionMerge), PermitFactionMerge);
        Set(nameof(GameDifficulty), GameDifficulty);
        Set(nameof(LeadershipOffenceRate), LeadershipOffenceRate);
        Set(nameof(LiangdaoXitong), LiangdaoXitong);
        Set(nameof(WujiangYoukenengDuli), WujiangYoukenengDuli);
        Set(nameof(FastBattleSpeed), FastBattleSpeed);
        Set(nameof(EnableCheat), EnableCheat);
        Set(nameof(HardcoreMode), HardcoreMode);
        Set(nameof(LandArmyCanGoDownWater), LandArmyCanGoDownWater);
        Set(nameof(MaxAbility), MaxAbility);
        Set(nameof(TirednessIncrease), TirednessIncrease);
        Set(nameof(TirednessDecrease), TirednessDecrease);
        Set(nameof(EnableAgeAbilityFactor), EnableAgeAbilityFactor);
        Set(nameof(TabListDetailLevel), TabListDetailLevel);
        Set(nameof(EnableExtensions), EnableExtensions);
        Set(nameof(EncryptSave), EncryptSave);
        Set(nameof(AutoSaveFrequency), AutoSaveFrequency);
        Set(nameof(ShowChallengeAnimation), ShowChallengeAnimation);
        Set(nameof(PersonDieInChallenge), PersonDieInChallenge);
        Set(nameof(OfficerDieInBattleRate), OfficerDieInBattleRate);
        Set(nameof(OfficerChildrenLimit), OfficerChildrenLimit);
        Set(nameof(StopToControlOnAttack), StopToControlOnAttack);
        Set(nameof(MaxMilitaryExperience), MaxMilitaryExperience);
        Set(nameof(CreateRandomOfficerChance), CreateRandomOfficerChance);
        Set(nameof(ZhaoXianSuccessRate), ZhaoXianSuccessRate);
        Set(nameof(CreatedOfficerAbilityFactor), CreatedOfficerAbilityFactor);
        Set(nameof(EnablePersonRelations), EnablePersonRelations);
        Set(nameof(ChildrenAvailableAge), ChildrenAvailableAge);
        Set(nameof(FullScreen), FullScreen);
        Set(nameof(FriendlyDiplomacyThreshold), FriendlyDiplomacyThreshold);
        Set(nameof(SurroundFactor), SurroundFactor);
        Set(nameof(PermitQuanXiang), PermitQuanXiang);
        Set(nameof(PermitManualAwardTitleAutoLearn), PermitManualAwardTitleAutoLearn);
        Set(nameof(zhaoxianOfficerMax), zhaoxianOfficerMax);
        Set(nameof(FactionMilitaryLimt), FactionMilitaryLimt);
        Set(nameof(FixedUnnaturalDeathAge), FixedUnnaturalDeathAge);
        Set(nameof(AIQuickBattle), AIQuickBattle);
        Set(nameof(EnableAIDebugLog), EnableAIDebugLog);
        Set(nameof(PlayerAutoSectionHasAIResourceBonus), PlayerAutoSectionHasAIResourceBonus);
        Set(nameof(ChildrenAbilityFactor), ChildrenAbilityFactor);
        Set(nameof(ProhibitFactionAgainstDestroyer), ProhibitFactionAgainstDestroyer);
        Set(nameof(AIMergeAgainstPlayer), AIMergeAgainstPlayer);
        Set(nameof(RemoveSpouseIfNotAvailable), RemoveSpouseIfNotAvailable);
        Set(nameof(SkyEyeSimpleNotification), SkyEyeSimpleNotification);
        Set(nameof(AutoMultipleMarriage), AutoMultipleMarriage);
        Set(nameof(BornHistoricalChildren), BornHistoricalChildren);
        Set(nameof(hougongAlienOnly), hougongAlienOnly);
        Set(nameof(StartCircleTime), StartCircleTime);
        Set(nameof(ScenarioMapPerTime), ScenarioMapPerTime);
        Set(nameof(KeepSpousePersonalLoyalty), KeepSpousePersonalLoyalty);
        Set(nameof(ShowNumberAddTime), ShowNumberAddTime);
        Set(nameof(TroopVoice), TroopVoice);
        Set(nameof(MaxTupianwenzi), MaxTupianwenzi);
        Set(nameof(EnableAIAuthorityPhase1), EnableAIAuthorityPhase1);
        Set(nameof(EnableWeatherParticles), EnableWeatherParticles); // 🌧️ 2026-03-10 新增

        document.AppendChild(element);
        
        Platform.Current.SaveUserFile("Content/Data/GlobalVariables.xml", document.OuterXml, true);
    }

    // ========================================================================
    // 私有辅助方法
    // ========================================================================

    private void ParseAttribute<T>(XmlNode node, string attrName, ref T field) where T : IParsable<T>
    {
        try
        {
            var attr = node.Attributes?.GetNamedItem(attrName);
            if (attr != null && !string.IsNullOrEmpty(attr.Value))
            {
                field = T.Parse(attr.Value, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{attrName}:\n{ex}");
        }
    }

    private void ParseAttribute(XmlNode node, string attrName, ref string field)
    {
        try
        {
            var attr = node.Attributes?.GetNamedItem(attrName);
            if (attr != null)
            {
                field = attr.Value;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{attrName}:\n{ex}");
        }
    }

    private void ParseAttribute(XmlNode node, string attrName, ref bool? field)
    {
        try
        {
            var attr = node.Attributes?.GetNamedItem(attrName);
            if (attr != null && !string.IsNullOrEmpty(attr.Value))
            {
                field = bool.Parse(attr.Value);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{attrName}:\n{ex}");
        }
    }

    private T ParseSafe<T>(XmlNode node, string attrName, T defaultValue) where T : IParsable<T>
    {
        var valStr = SafeGetAttributeValue(node, attrName);
        if (!string.IsNullOrEmpty(valStr) && T.TryParse(valStr, CultureInfo.InvariantCulture, out T result))
        {
            return result;
        }
        return defaultValue;
    }

    private void TryParseAttribute<T>(XmlNode node, string attrName, ref T field) where T : IParsable<T>
    {
        try
        {
            var attr = node.Attributes?.GetNamedItem(attrName);
            if (attr != null && !string.IsNullOrEmpty(attr.Value))
            {
                field = T.Parse(attr.Value, CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // ignored
        }
    }

    private string SafeGetAttributeValue(XmlNode node, string attributeName)
    {
        var attribute = node?.Attributes?.GetNamedItem(attributeName);
        return attribute?.Value;
    }
}
