using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.Animations;
using GameObjects.Conditions;
using GameObjects.FactionDetail;
using GameObjects.Influences;
using GameObjects.MapDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using GameObjects.TroopDetail.EventEffect;
using WorldOfTheThreeKingdoms.Serialization.SystemTextJson;
using WorldOfTheThreeKingdoms.Tools;
using ArchEventEffect = GameObjects.ArchitectureDetail.EventEffect;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// CommonData 专用加载器
    /// 
    /// 问题：CommonData.json 的字典使用字符串键（"1", "2"），需要转换为 int 键
    /// 但 System.Text.Json 的 AOT 源生成器会忽略全局转换器
    /// 
    /// 解决：手动反序列化 CommonData，逐个处理每个 Table 字段
    /// 
    /// 日期：2026-03-20
    /// </summary>
    public static class CommonDataLoader
    {
        /// <summary>
        /// 从文件加载 CommonData
        /// </summary>
        public static CommonData LoadFromFile(string filePath)
        {
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CommonData 文件不存在: {filePath}");
            }
            
            string jsonContent = File.ReadAllText(filePath);
            
            return LoadFromJson(jsonContent);
        }
        
        /// <summary>
        /// 从 JSON 字符串加载 CommonData
        /// </summary>
        public static CommonData LoadFromJson(string jsonContent)
        {
            
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;
            
            var commonData = new CommonData();
            
            // 🔥 关键：手动处理每个属性，使用专用转换器
            var options = GameJsonContext.GetScenarioFileOptions();
            
            foreach (var property in root.EnumerateObject())
            {
                try
                {
                    
                    switch (property.Name)
                    {
                        case "AllArchitectureKinds":
                            // 🔥 根本修复：直接反序列化嵌套的字典字段，而不是整个 Table 对象
                            // 问题：JsonSerializer.Deserialize<ArchitectureKindTable>() 不会触发字典转换器
                            // 原因：转换器是为 Dictionary<int, T> 注册的，不是为 ArchitectureKindTable 注册的
                            // 解决：手动提取 "ArchitectureKinds" 字段，直接反序列化字典
                            // 日期：2026-03-20
                            commonData.AllArchitectureKinds = new ArchitectureKindTable();
                            if (property.Value.TryGetProperty("ArchitectureKinds", out var archKindsElement))
                            {
                                commonData.AllArchitectureKinds.ArchitectureKinds = 
                                    JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<int, ArchitectureKind>>(
                                        archKindsElement.GetRawText(), options) ?? [];
                            }
                            break;
                            
                        case "AllMilitaryKinds":
                            commonData.AllMilitaryKinds = new MilitaryKindTable();
                            if (property.Value.TryGetProperty("MilitaryKinds", out var milKindsElement))
                            {
                                commonData.AllMilitaryKinds.MilitaryKinds = 
                                    JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<int, MilitaryKind>>(
                                        milKindsElement.GetRawText(), options) ?? [];
                            }
                            break;
                            
                        case "AllConditionKinds":
                            commonData.AllConditionKinds = new ConditionKindTable();
                            if (property.Value.TryGetProperty("ConditionKinds", out var condKindsElement))
                            {
                                commonData.AllConditionKinds.ConditionKinds = 
                                    JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<int, ConditionKind>>(
                                        condKindsElement.GetRawText(), options) ?? [];
                            }
                            break;
                            
                        case "AllIdealTendencyKinds":
                            // 🔥 根本修复：使用简单选项避免 StackOverflowException
                            // 问题：ReferenceHandler.Preserve + IdealTendencyKindConverter 导致无限递归
                            // 原因：ReferenceHandler 处理引用 → 调用转换器 → 转换器触发 ReferenceHandler → 循环
                            // 解决：使用不带 ReferenceHandler 的简单选项反序列化
                            // 日期：2026-03-20
                            commonData.AllIdealTendencyKinds = new IdealTendencyKindList();
                            if (property.Value.TryGetProperty("GameObjects", out var idealKindsElement))
                            {
                                // 🔥 创建简单选项（不带 ReferenceHandler.Preserve）
                                var simpleOptions = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    TypeInfoResolver = GameJsonContext.Default,
                                    IncludeFields = true
                                };
                                
                                var gameObjects = JsonSerializer.Deserialize<List<IdealTendencyKind>>(
                                    idealKindsElement.GetRawText(), simpleOptions);
                                
                                if (gameObjects == null)
                                {
                                    throw new InvalidDataException(
                                        $"AllIdealTendencyKinds 反序列化失败：JsonSerializer.Deserialize 返回 null");
                                }
                                
                                foreach (var obj in gameObjects)
                                {
                                    commonData.AllIdealTendencyKinds.Add(obj);
                                }
                            }
                            break;
                            
                        // 🔥 其他属性：使用标准反序列化（这些不包含字符串键字典）
                        case "FlankBonus":
                            commonData.FlankBonus = property.Value.GetSingle();
                            break;
                        case "AllAttackDefaultKinds":
                            commonData.AllAttackDefaultKinds = JsonSerializer.Deserialize<AttackDefaultKindList>(property.Value.GetRawText(), options);
                            break;
                        case "AllAttackTargetKinds":
                            commonData.AllAttackTargetKinds = JsonSerializer.Deserialize<AttackTargetKindList>(property.Value.GetRawText(), options);
                            break;
                        case "AllBiographyAdjectives":
                            commonData.AllBiographyAdjectives = JsonSerializer.Deserialize<List<BiographyAdjectives>>(property.Value.GetRawText(), options);
                            break;
                        case "AllCastDefaultKinds":
                            commonData.AllCastDefaultKinds = JsonSerializer.Deserialize<CastDefaultKindList>(property.Value.GetRawText(), options);
                            break;
                        case "AllCastTargetKinds":
                            commonData.AllCastTargetKinds = JsonSerializer.Deserialize<CastTargetKindList>(property.Value.GetRawText(), options);
                            break;
                        case "AllCharacterKinds":
                            commonData.AllCharacterKinds = JsonSerializer.Deserialize<List<CharacterKind>>(property.Value.GetRawText(), options);
                            break;
                        case "AllColors":
                            commonData.AllColors = JsonSerializer.Deserialize<List<Microsoft.Xna.Framework.Color>>(property.Value.GetRawText(), options);
                            break;
                        case "AllCombatMethods":
                            commonData.AllCombatMethods = JsonSerializer.Deserialize<CombatMethodTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllConditions":
                            commonData.AllConditions = JsonSerializer.Deserialize<ConditionTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllEventEffectKinds":
                            commonData.AllEventEffectKinds = JsonSerializer.Deserialize<ArchEventEffect.EventEffectKindTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllEventEffects":
                            commonData.AllEventEffects = JsonSerializer.Deserialize<ArchEventEffect.EventEffectTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllFacilityKinds":
                            commonData.AllFacilityKinds = JsonSerializer.Deserialize<FacilityKindTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllInfluenceKinds":
                            commonData.AllInfluenceKinds = JsonSerializer.Deserialize<InfluenceKindTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllInfluences":
                            commonData.AllInfluences = JsonSerializer.Deserialize<InfluenceTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllInformationKinds":
                            commonData.AllInformationKinds = JsonSerializer.Deserialize<InformationKindList>(property.Value.GetRawText(), options);
                            break;
                        case "AllSectionAIDetails":
                            commonData.AllSectionAIDetails = JsonSerializer.Deserialize<SectionAIDetailTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllSkills":
                            commonData.AllSkills = JsonSerializer.Deserialize<SkillTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllStratagems":
                            commonData.AllStratagems = JsonSerializer.Deserialize<StratagemTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllStunts":
                            commonData.AllStunts = JsonSerializer.Deserialize<StuntTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTechniques":
                            commonData.AllTechniques = JsonSerializer.Deserialize<TechniqueTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTerrainDetails":
                            commonData.AllTerrainDetails = JsonSerializer.Deserialize<TerrainDetailTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTextMessages":
                            commonData.AllTextMessages = JsonSerializer.Deserialize<TextMessageTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTileAnimations":
                            commonData.AllTileAnimations = JsonSerializer.Deserialize<AnimationTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTitles":
                            commonData.AllTitles = JsonSerializer.Deserialize<TitleTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTitleKinds":
                            commonData.AllTitleKinds = JsonSerializer.Deserialize<TitleKindTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTroopAnimations":
                            commonData.AllTroopAnimations = JsonSerializer.Deserialize<AnimationTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTroopEventEffectKinds":
                            commonData.AllTroopEventEffectKinds = JsonSerializer.Deserialize<EventEffectKindTable>(property.Value.GetRawText(), options);
                            break;
                        case "AllTroopEventEffects":
                            commonData.AllTroopEventEffects = JsonSerializer.Deserialize<EventEffectTable>(property.Value.GetRawText(), options);
                            break;
                        case "PersonGeneratorSetting":
                            commonData.PersonGeneratorSetting = JsonSerializer.Deserialize<PersonGeneratorSetting>(property.Value.GetRawText(), options);
                            break;
                        case "AllPersonGeneratorTypes":
                            commonData.AllPersonGeneratorTypes = JsonSerializer.Deserialize<PersonGeneratorTypeList>(property.Value.GetRawText(), options);
                            break;
                        case "AllTrainPolicies":
                            commonData.AllTrainPolicies = JsonSerializer.Deserialize<TrainPolicyList>(property.Value.GetRawText(), options);
                            break;
                        case "AllTreasureCreationSettings":
                            commonData.AllTreasureCreationSettings = JsonSerializer.Deserialize<TreasureCreationSettingList>(property.Value.GetRawText(), options);
                            break;
                        case "NumberGenerator":
                            commonData.NumberGenerator = JsonSerializer.Deserialize<CombatNumberGenerator>(property.Value.GetRawText(), options);
                            break;
                        case "TroopAnimations":
                            commonData.TroopAnimations = JsonSerializer.Deserialize<TroopAnimation>(property.Value.GetRawText(), options);
                            break;
                        case "suoyouzainanzhonglei":
                            commonData.suoyouzainanzhonglei = JsonSerializer.Deserialize<zainanzhongleibiao>(property.Value.GetRawText(), options);
                            break;
                        case "suoyouguanjuezhonglei":
                            commonData.suoyouguanjuezhonglei = JsonSerializer.Deserialize<guanjuezhongleibiao>(property.Value.GetRawText(), options);
                            break;
                            
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                }
            }
            
            return commonData;
        }
    }
}
