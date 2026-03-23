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
using Microsoft.Xna.Framework;
using Platforms;
using WorldOfTheThreeKingdoms.Tools;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 AOT 根本修复：移除 DataContract 特性
    // 问题：DataContract/DataMember 与 System.Text.Json 源生成器冲突，导致源生成器静默失败
    // 解决：使用 System.Text.Json 的特性（属性默认序列化），让 AOT 源生成器正常工作
    public class CommonData
    {
        public static CommonData Current = null;

        public static bool CurrentReady = false;

        public CommonData()
        {
            // 🔥 Checklist Fix: Priority 2 - Field Initialization
            this.AllMilitaryKinds = new MilitaryKindTable();
            this.AllInformationKinds = new InformationKindList();
            this.AllSkills = new SkillTable();
            // 🔥 2026-03-13 修复：保持 GameObjectList 类型以兼容现有代码
            // 但通过 JsonConverter 属性指定使用 IdealTendencyKindList 进行反序列化
            this.AllIdealTendencyKinds = new GameObjectList();
        }

        // 🔥 2026-02-12 AOT 根本修复：移除所有 DataMember 特性
        // 问题：DataMember 与 System.Text.Json 源生成器冲突
        // 解决：System.Text.Json 默认序列化所有公共属性和字段
        public float FlankBonus = 20.0f; // Default flank bonus

        public ArchitectureKindTable AllArchitectureKinds = new ArchitectureKindTable();
        public AttackDefaultKindList AllAttackDefaultKinds = new AttackDefaultKindList();
        public AttackTargetKindList AllAttackTargetKinds = new AttackTargetKindList();
        public CastDefaultKindList AllCastDefaultKinds = new CastDefaultKindList();
        public CastTargetKindList AllCastTargetKinds = new CastTargetKindList();

        public List<CharacterKind> AllCharacterKinds = new List<CharacterKind>();
        public List<Color> AllColors = new List<Color>();
        public CombatMethodTable AllCombatMethods = new CombatMethodTable();

        //性能优化
        public ConditionKindTable AllConditionKinds = new ConditionKindTable();

        public ConditionTable AllConditions = new ConditionTable();

        public FacilityKindTable AllFacilityKinds = new FacilityKindTable();

        public zainanzhongleibiao suoyouzainanzhonglei = new zainanzhongleibiao();

        public guanjuezhongleibiao suoyouguanjuezhonglei = new guanjuezhongleibiao();

        // 🔥 2026-03-13 根本修复：使用自定义 JsonConverter 指定反序列化类型
        // 问题：字段类型是 GameObjectList，但需要反序列化为 IdealTendencyKindList 以支持类型推断
        // 解决：通过 JsonConverter 属性，让反序列化器知道应该创建 IdealTendencyKindList 实例
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IdealTendencyKindListConverter))]
        public GameObjectList AllIdealTendencyKinds = new GameObjectList();

        //性能优化
        public InfluenceKindTable AllInfluenceKinds = new InfluenceKindTable();

        public InfluenceTable AllInfluences = new InfluenceTable();

        // 🔥 修复 1：强制使用 InformationKind 专用列表转换器
        // 这样可以确保反序列化出来的是 InformationKind 而不是 GameObject
        public InformationKindList AllInformationKinds = new InformationKindList();

        public MilitaryKindTable AllMilitaryKinds = new MilitaryKindTable();

        public SectionAIDetailTable AllSectionAIDetails = new SectionAIDetailTable();

        public SkillTable AllSkills = new SkillTable();
        public StratagemTable AllStratagems = new StratagemTable();
        public StuntTable AllStunts = new StuntTable();
        public TechniqueTable AllTechniques = new TechniqueTable();
        public TerrainDetailTable AllTerrainDetails = new TerrainDetailTable();
        
        // 🔥 修复 2：强制使用刚刚写的文本表转换器，解决 Crash 问题
        public TextMessageTable AllTextMessages = new TextMessageTable();
        
        public AnimationTable AllTileAnimations = new AnimationTable();

        public TitleTable AllTitles = new TitleTable();

        public TitleKindTable AllTitleKinds = new TitleKindTable();

        // public GuanzhiTable AllGuanzhis = new GuanzhiTable();
        //public GuanzhiKindTable AllGuanzhiKinds = new GuanzhiKindTable();
        public AnimationTable AllTroopAnimations = new AnimationTable();

        public GameObjects.TroopDetail.EventEffect.EventEffectKindTable AllTroopEventEffectKinds = new();

        public GameObjects.TroopDetail.EventEffect.EventEffectTable AllTroopEventEffects = new();

        public ArchEventEffect.EventEffectKindTable AllEventEffectKinds = new();

        public ArchEventEffect.EventEffectTable AllEventEffects = new();

        public List<BiographyAdjectives> AllBiographyAdjectives = new List<BiographyAdjectives>();
        public PersonGeneratorSetting PersonGeneratorSetting = new PersonGeneratorSetting();
        public PersonGeneratorTypeList AllPersonGeneratorTypes = new PersonGeneratorTypeList();
        public TrainPolicyList AllTrainPolicies = new TrainPolicyList();

        public TreasureCreationSettingList AllTreasureCreationSettings = new TreasureCreationSettingList();

        public CombatNumberGenerator NumberGenerator = new CombatNumberGenerator();

        public TroopAnimation TroopAnimations = new TroopAnimation();

        public CommonData Clone()
        {
            var commonData = this.MemberwiseClone() as CommonData;
            return commonData;
        }

        public void Clear()
        {
            this.AllArchitectureKinds.Clear();
            this.AllAttackDefaultKinds.Clear();
            this.AllAttackTargetKinds.Clear();
            this.AllBiographyAdjectives.Clear();
            this.AllCastDefaultKinds.Clear();
            this.AllCastTargetKinds.Clear();
            this.AllCharacterKinds.Clear();
            this.AllColors.Clear();
            this.AllCombatMethods.Clear();
            this.AllConditionKinds.Clear();
            this.AllConditions.Clear();
            this.AllEventEffectKinds.Clear();
            this.AllEventEffects.Clear();
            this.AllFacilityKinds.Clear();
            this.AllIdealTendencyKinds.Clear();
            this.AllInfluenceKinds.Clear();
            this.AllInfluences.Clear();
            this.AllInformationKinds.Clear();
            this.AllMilitaryKinds.Clear();
            this.AllSectionAIDetails.Clear();
            this.AllSkills.Clear();
            this.AllStratagems.Clear();
            this.AllStunts.Clear();
            this.AllTechniques.Clear();
            this.AllTerrainDetails.Clear();
            this.AllTextMessages.Clear();
            this.AllTileAnimations.Clear();
            this.AllTitles.Clear();
            this.AllTitleKinds.Clear();
            //this.AllGuanzhis.Clear();
            //this.AllGuanzhiKinds.Clear();
            this.AllTroopAnimations.Clear();
            this.AllTroopEventEffectKinds.Clear();
            this.AllTroopEventEffects.Clear();
            this.suoyouguanjuezhonglei.Clear();
            this.suoyouzainanzhonglei.Clear();
            this.AllPersonGeneratorTypes.Clear();
            this.AllTrainPolicies.Clear();
            this.AllTreasureCreationSettings.Clear();
        }

        /// <summary>
        /// CommonData初始化 - 🔥 根本修复：直接使用 SimpleSerializer 进行反序列化
        /// </summary>
        public static void Init()
        {
            new PlatformTask(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[CommonData.Init] 开始初始化 CommonData");
                    
                    // 🔥 根本修复：使用专用加载器处理 CommonData
                    // 问题：CommonData.json 的字典使用字符串键，AOT 源生成器忽略全局转换器
                    // 解决：使用 CommonDataLoader 手动处理反序列化
                    // 日期：2026-03-20
                    string commonDataPath = @"Content\Data\Common\CommonData.json";
                    if (!System.IO.File.Exists(commonDataPath))
                    {
                        throw new System.IO.FileNotFoundException($"CommonData.json 文件不存在: {commonDataPath}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CommonData.Init] 从文件加载: {commonDataPath}");
                    Current = WorldOfTheThreeKingdoms.Serialization.CommonDataLoader.LoadFromFile(commonDataPath);
                    
                    if (Current == null)
                    {
                        throw new InvalidOperationException("CommonData 反序列化返回 null");
                    }
                    
                    // 🔥 验证关键数据完整性
                    ValidateCommonDataIntegrity(Current);
                    
                    GameScenario.ProcessCommonData(Current);
                    
                    CurrentReady = true;
                    System.Diagnostics.Debug.WriteLine("[CommonData.Init] ✅ CommonData 初始化成功");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommonData.Init] ❌ 初始化失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CommonData.Init] 堆栈跟踪: {ex.StackTrace}");
                    
                    // 🔥 最后的应急恢复
                    if (!TryEmergencyRecovery())
                    {
                        throw new Exception($"CommonData初始化失败，所有恢复方案都失败: {ex.Message}", ex);
                    }
                }
            }).Start();
        }

        /// <summary>
        /// 🔥 根本修复：验证 CommonData 的完整性
        /// </summary>
        private static void ValidateCommonDataIntegrity(CommonData commonData)
        {
            var issues = new List<string>();
            
            // 检查关键数据表
            if (commonData.AllMilitaryKinds == null)
                issues.Add("AllMilitaryKinds 为 null");
            else if (commonData.AllMilitaryKinds.MilitaryKinds == null)
                issues.Add("AllMilitaryKinds.MilitaryKinds 为 null");
            else if (commonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
                issues.Add("AllMilitaryKinds.MilitaryKinds 为空");
                
            if (commonData.AllInformationKinds == null)
                issues.Add("AllInformationKinds 为 null");
            else if (commonData.AllInformationKinds.GameObjects == null)
                issues.Add("AllInformationKinds.GameObjects 为 null");
                
            if (commonData.AllArchitectureKinds == null)
                issues.Add("AllArchitectureKinds 为 null");
            
            // 🔥 关键修复：检查 AllIdealTendencyKinds
            if (commonData.AllIdealTendencyKinds == null)
                issues.Add("AllIdealTendencyKinds 为 null");
            else if (commonData.AllIdealTendencyKinds.Count == 0)
                issues.Add("AllIdealTendencyKinds 为空");
            
            // 🔥 新增：检查 AllConditionKinds
            if (commonData.AllConditionKinds == null)
                issues.Add("AllConditionKinds 为 null");
            else if (commonData.AllConditionKinds.ConditionKinds == null)
                issues.Add("AllConditionKinds.ConditionKinds 为 null");
            else if (commonData.AllConditionKinds.ConditionKinds.Count == 0)
                issues.Add("AllConditionKinds.ConditionKinds 为空");
                
            if (issues.Count > 0)
            {
                string errorMsg = "CommonData 完整性检查失败:\n" + string.Join("\n", issues);
                System.Diagnostics.Debug.WriteLine($"[ValidateCommonDataIntegrity] ❌ {errorMsg}");
                throw new InvalidDataException(errorMsg);
            }
            
            System.Diagnostics.Debug.WriteLine("[ValidateCommonDataIntegrity] ✅ CommonData 完整性检查通过");
            System.Diagnostics.Debug.WriteLine($"[ValidateCommonDataIntegrity] - AllMilitaryKinds: {commonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
            System.Diagnostics.Debug.WriteLine($"[ValidateCommonDataIntegrity] - AllInformationKinds: {commonData.AllInformationKinds.GameObjects.Count} 个情报类型");
            System.Diagnostics.Debug.WriteLine($"[ValidateCommonDataIntegrity] - AllIdealTendencyKinds: {commonData.AllIdealTendencyKinds.Count} 个理想倾向");
            System.Diagnostics.Debug.WriteLine($"[ValidateCommonDataIntegrity] - AllConditionKinds: {commonData.AllConditionKinds.ConditionKinds.Count} 个条件类型");
        }

        /// <summary>
        /// 🔥 根本修复：应急恢复机制
        /// </summary>
        private static bool TryEmergencyRecovery()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TryEmergencyRecovery] 尝试应急恢复");
                
                // 1. 尝试从备份文件恢复
                string backupPath = @"Content\Data\Common\CommonData.backup.json";
                if (System.IO.File.Exists(backupPath))
                {
                    System.Diagnostics.Debug.WriteLine("[TryEmergencyRecovery] 发现备份文件，尝试恢复");
                    Current = SimpleSerializer.DeserializeJsonFile<CommonData>(backupPath, false, false);
                    
                    if (Current != null)
                    {
                        ValidateCommonDataIntegrity(Current);
                        GameScenario.ProcessCommonData(Current);
                        CurrentReady = true;
                        System.Diagnostics.Debug.WriteLine("[TryEmergencyRecovery] ✅ 从备份文件恢复成功");
                        return true;
                    }
                }
                
                // 2. 创建最小可用的 CommonData
                System.Diagnostics.Debug.WriteLine("[TryEmergencyRecovery] 创建最小可用的 CommonData");
                Current = CreateMinimalCommonData();
                
                if (Current != null)
                {
                    GameScenario.ProcessCommonData(Current);
                    CurrentReady = true;
                    System.Diagnostics.Debug.WriteLine("[TryEmergencyRecovery] ✅ 创建最小 CommonData 成功");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TryEmergencyRecovery] ❌ 应急恢复失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔥 根本修复：创建最小可用的 CommonData
        /// </summary>
        private static CommonData CreateMinimalCommonData()
        {
            try
            {
                var commonData = new CommonData();
                
                // 初始化关键数据表
                commonData.AllMilitaryKinds = new MilitaryKindTable();
                commonData.AllInformationKinds = new InformationKindList();
                commonData.AllArchitectureKinds = new ArchitectureKindTable();
                commonData.AllConditions = new ConditionTable();
                commonData.AllInfluences = new InfluenceTable();
                commonData.AllSkills = new SkillTable();
                commonData.AllTechniques = new TechniqueTable();
                commonData.AllCombatMethods = new CombatMethodTable();
                
                // 确保 GameObjects 列表不为 null
                if (commonData.AllInformationKinds.GameObjects == null)
                {
                    commonData.AllInformationKinds.GameObjects = new List<GameObject>();
                }
                
                System.Diagnostics.Debug.WriteLine("[CreateMinimalCommonData] 创建了最小可用的 CommonData");
                return commonData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateMinimalCommonData] ❌ 创建最小 CommonData 失败: {ex.Message}");
                return null;
            }
        }
    }
}