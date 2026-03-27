using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GameObjects;
using GameObjects.Animations;
using GameObjects.ArchitectureDetail;
using GameObjects.Conditions;
using GameObjects.FactionDetail;
using GameObjects.Influences;
using GameObjects.MapDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using GameObjects.TroopDetail.EventEffect;
using ArchEventEffect = GameObjects.ArchitectureDetail.EventEffect;

namespace WorldOfTheThreeKingdoms.Serialization
{
    public static class CommonDataLoader
    {
        public static CommonData LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CommonData 文件不存在: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);
            return LoadFromJson(jsonContent);
        }

        public static CommonData LoadFromJson(string jsonContent)
        {
            using JsonDocument document = JsonDocument.Parse(jsonContent);
            JsonElement root = document.RootElement;
            CommonData commonData = new();
            JsonSerializerOptions options = GameJsonContext.GetScenarioFileOptions();

            foreach (JsonProperty property in root.EnumerateObject())
            {
                try
                {
                    switch (property.Name)
                    {
                        case "AllArchitectureKinds":
                            commonData.AllArchitectureKinds = new ArchitectureKindTable();
                            if (!property.Value.TryGetProperty("ArchitectureKinds", out JsonElement architectureKindsElement))
                            {
                                throw new InvalidDataException("AllArchitectureKinds 缺少 ArchitectureKinds 字段");
                            }

                            commonData.AllArchitectureKinds.ArchitectureKinds =
                                DeserializeRequired<Dictionary<int, ArchitectureKind>>(architectureKindsElement, options, "AllArchitectureKinds.ArchitectureKinds");
                            break;
                        case "AllMilitaryKinds":
                            commonData.AllMilitaryKinds = new MilitaryKindTable();
                            if (!property.Value.TryGetProperty("MilitaryKinds", out JsonElement militaryKindsElement))
                            {
                                throw new InvalidDataException("AllMilitaryKinds 缺少 MilitaryKinds 字段");
                            }

                            commonData.AllMilitaryKinds.MilitaryKinds =
                                DeserializeRequired<Dictionary<int, MilitaryKind>>(militaryKindsElement, options, "AllMilitaryKinds.MilitaryKinds");
                            break;
                        case "AllConditionKinds":
                            commonData.AllConditionKinds = new ConditionKindTable();
                            if (!property.Value.TryGetProperty("ConditionKinds", out JsonElement conditionKindsElement))
                            {
                                throw new InvalidDataException("AllConditionKinds 缺少 ConditionKinds 字段");
                            }

                            commonData.AllConditionKinds.ConditionKinds =
                                DeserializeRequired<Dictionary<int, ConditionKind>>(conditionKindsElement, options, "AllConditionKinds.ConditionKinds");
                            break;
                        case "AllIdealTendencyKinds":
                            commonData.AllIdealTendencyKinds = new IdealTendencyKindList();
                            if (!property.Value.TryGetProperty("GameObjects", out JsonElement idealTendencyKindsElement))
                            {
                                throw new InvalidDataException("AllIdealTendencyKinds 缺少 GameObjects 字段");
                            }

                            List<IdealTendencyKind> idealTendencyKinds =
                                DeserializeRequired<List<IdealTendencyKind>>(idealTendencyKindsElement, options, "AllIdealTendencyKinds.GameObjects");
                            for (int i = 0; i < idealTendencyKinds.Count; i++)
                            {
                                commonData.AllIdealTendencyKinds.Add(idealTendencyKinds[i]);
                            }
                            break;
                        case "FlankBonus":
                            commonData.FlankBonus = property.Value.GetSingle();
                            break;
                        case "AllAttackDefaultKinds":
                            commonData.AllAttackDefaultKinds = DeserializeRequired<AttackDefaultKindList>(property.Value, options, property.Name);
                            break;
                        case "AllAttackTargetKinds":
                            commonData.AllAttackTargetKinds = DeserializeRequired<AttackTargetKindList>(property.Value, options, property.Name);
                            break;
                        case "AllBiographyAdjectives":
                            commonData.AllBiographyAdjectives = DeserializeRequired<List<BiographyAdjectives>>(property.Value, options, property.Name);
                            break;
                        case "AllCastDefaultKinds":
                            commonData.AllCastDefaultKinds = DeserializeRequired<CastDefaultKindList>(property.Value, options, property.Name);
                            break;
                        case "AllCastTargetKinds":
                            commonData.AllCastTargetKinds = DeserializeRequired<CastTargetKindList>(property.Value, options, property.Name);
                            break;
                        case "AllCharacterKinds":
                            commonData.AllCharacterKinds = DeserializeRequired<List<CharacterKind>>(property.Value, options, property.Name);
                            break;
                        case "AllColors":
                            commonData.AllColors = DeserializeRequired<List<Microsoft.Xna.Framework.Color>>(property.Value, options, property.Name);
                            break;
                        case "AllCombatMethods":
                            commonData.AllCombatMethods = DeserializeRequired<CombatMethodTable>(property.Value, options, property.Name);
                            break;
                        case "AllConditions":
                            commonData.AllConditions = DeserializeRequired<ConditionTable>(property.Value, options, property.Name);
                            break;
                        case "AllEventEffectKinds":
                            commonData.AllEventEffectKinds = DeserializeRequired<ArchEventEffect.EventEffectKindTable>(property.Value, options, property.Name);
                            break;
                        case "AllEventEffects":
                            commonData.AllEventEffects = DeserializeRequired<ArchEventEffect.EventEffectTable>(property.Value, options, property.Name);
                            break;
                        case "AllFacilityKinds":
                            commonData.AllFacilityKinds = DeserializeRequired<FacilityKindTable>(property.Value, options, property.Name);
                            break;
                        case "AllInfluenceKinds":
                            commonData.AllInfluenceKinds = DeserializeRequired<InfluenceKindTable>(property.Value, options, property.Name);
                            break;
                        case "AllInfluences":
                            commonData.AllInfluences = DeserializeRequired<InfluenceTable>(property.Value, options, property.Name);
                            break;
                        case "AllInformationKinds":
                            commonData.AllInformationKinds = DeserializeRequired<InformationKindList>(property.Value, options, property.Name);
                            break;
                        case "AllSectionAIDetails":
                            commonData.AllSectionAIDetails = DeserializeRequired<SectionAIDetailTable>(property.Value, options, property.Name);
                            break;
                        case "AllSkills":
                            commonData.AllSkills = DeserializeRequired<SkillTable>(property.Value, options, property.Name);
                            break;
                        case "AllStratagems":
                            commonData.AllStratagems = DeserializeRequired<StratagemTable>(property.Value, options, property.Name);
                            break;
                        case "AllStunts":
                            commonData.AllStunts = DeserializeRequired<StuntTable>(property.Value, options, property.Name);
                            break;
                        case "AllTechniques":
                            commonData.AllTechniques = DeserializeRequired<TechniqueTable>(property.Value, options, property.Name);
                            break;
                        case "AllTerrainDetails":
                            commonData.AllTerrainDetails = DeserializeRequired<TerrainDetailTable>(property.Value, options, property.Name);
                            break;
                        case "AllTextMessages":
                            commonData.AllTextMessages = DeserializeRequired<TextMessageTable>(property.Value, options, property.Name);
                            break;
                        case "AllTileAnimations":
                            commonData.AllTileAnimations = DeserializeRequired<AnimationTable>(property.Value, options, property.Name);
                            break;
                        case "AllTitles":
                            commonData.AllTitles = DeserializeRequired<TitleTable>(property.Value, options, property.Name);
                            break;
                        case "AllTitleKinds":
                            commonData.AllTitleKinds = DeserializeRequired<TitleKindTable>(property.Value, options, property.Name);
                            break;
                        case "AllTroopAnimations":
                            commonData.AllTroopAnimations = DeserializeRequired<AnimationTable>(property.Value, options, property.Name);
                            break;
                        case "AllTroopEventEffectKinds":
                            commonData.AllTroopEventEffectKinds = DeserializeRequired<EventEffectKindTable>(property.Value, options, property.Name);
                            break;
                        case "AllTroopEventEffects":
                            commonData.AllTroopEventEffects = DeserializeRequired<EventEffectTable>(property.Value, options, property.Name);
                            break;
                        case "PersonGeneratorSetting":
                            commonData.PersonGeneratorSetting = DeserializeRequired<PersonGeneratorSetting>(property.Value, options, property.Name);
                            break;
                        case "AllPersonGeneratorTypes":
                            commonData.AllPersonGeneratorTypes = DeserializeRequired<PersonGeneratorTypeList>(property.Value, options, property.Name);
                            break;
                        case "AllTrainPolicies":
                            commonData.AllTrainPolicies = DeserializeRequired<TrainPolicyList>(property.Value, options, property.Name);
                            break;
                        case "AllTreasureCreationSettings":
                            commonData.AllTreasureCreationSettings = DeserializeRequired<TreasureCreationSettingList>(property.Value, options, property.Name);
                            break;
                        case "NumberGenerator":
                            commonData.NumberGenerator = DeserializeRequired<CombatNumberGenerator>(property.Value, options, property.Name);
                            break;
                        case "TroopAnimations":
                            commonData.TroopAnimations = DeserializeRequired<TroopAnimation>(property.Value, options, property.Name);
                            break;
                        case "suoyouzainanzhonglei":
                            commonData.suoyouzainanzhonglei = DeserializeRequired<zainanzhongleibiao>(property.Value, options, property.Name);
                            break;
                        case "suoyouguanjuezhonglei":
                            commonData.suoyouguanjuezhonglei = DeserializeRequired<guanjuezhongleibiao>(property.Value, options, property.Name);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"CommonData 字段反序列化失败: {property.Name}", ex);
                }
            }

            return commonData;
        }

        private static JsonTypeInfo<T> ResolveTypeInfo<T>(JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(T));
            if (typeInfo is not JsonTypeInfo<T> typedTypeInfo)
            {
                throw new InvalidDataException($"AOT metadata missing: {typeof(T).FullName}");
            }

            return typedTypeInfo;
        }

        private static T DeserializeRequired<T>(JsonElement element, JsonSerializerOptions options, string propertyName)
        {
            T? value = JsonSerializer.Deserialize(element, ResolveTypeInfo<T>(options));
            if (value == null)
            {
                throw new InvalidDataException($"字段 {propertyName} 反序列化返回 null");
            }

            return value;
        }
    }
}
