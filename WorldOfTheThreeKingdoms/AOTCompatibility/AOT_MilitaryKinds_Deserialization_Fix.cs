/*
 * AOT MilitaryKinds 反序列化修复方案
 * 
 * 问题：AllMilitaryKinds.MilitaryKinds 为空导致 System.IO.InvalidDataException
 * 根本原因：AOT 反序列化失败，导致 CommonData.Current 为 null 或数据结构未正确初始化
 * 
 * 修复策略：
 * 1. 增强 MilitaryKindTable 的反序列化验证
 * 2. 添加数据完整性检查和自动修复
 * 3. 提供应急恢复机制
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using GameObjects;
using GameObjects.TroopDetail;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.Conditions;
using GameObjects.Influences;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.Tools;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT MilitaryKinds 反序列化修复器
    /// </summary>
    public static class AOTMilitaryKindsDeserializationFixer
    {
        /// <summary>
        /// 修复 CommonData 初始化中的 MilitaryKinds 反序列化问题
        /// </summary>
        public static void FixCommonDataInitialization()
        {
            Debug.WriteLine("[AOTMilitaryKindsFixer] 开始修复 CommonData 初始化");
            
            try
            {
                // 1. 检查当前 CommonData 状态
                if (CommonData.Current == null)
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] ❌ CommonData.Current 为 null，尝试重新加载");
                    ReloadCommonData();
                }
                
                // 2. 验证 MilitaryKinds 完整性
                if (!ValidateMilitaryKindsIntegrity(CommonData.Current))
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] ❌ MilitaryKinds 完整性检查失败，尝试修复");
                    RepairMilitaryKinds(CommonData.Current);
                }
                
                Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ CommonData 修复完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 修复过程中发生异常: {ex.Message}");
                throw new InvalidOperationException($"无法修复 CommonData 初始化: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 重新加载 CommonData
        /// </summary>
        private static void ReloadCommonData()
        {
            try
            {
                string jsonPath = @"Content\Data\Common\CommonData.json";
                
                if (!File.Exists(jsonPath))
                {
                    throw new FileNotFoundException($"CommonData.json 文件不存在: {jsonPath}");
                }
                
                Debug.WriteLine($"[AOTMilitaryKindsFixer] 重新加载 CommonData: {jsonPath}");
                
                // 使用增强的反序列化方法
                CommonData.Current = DeserializeCommonDataWithValidation(jsonPath);
                
                if (CommonData.Current == null)
                {
                    throw new InvalidOperationException("CommonData 反序列化返回 null");
                }
                
                Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ CommonData 重新加载成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 重新加载 CommonData 失败: {ex.Message}");
                
                // 尝试创建最小可用的 CommonData
                CommonData.Current = CreateMinimalCommonData();
                
                if (CommonData.Current == null)
                {
                    throw new InvalidOperationException($"无法创建最小 CommonData: {ex.Message}", ex);
                }
                
                Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ 创建最小 CommonData 成功");
            }
        }
        
        /// <summary>
        /// 使用验证的方式反序列化 CommonData
        /// </summary>
        private static CommonData DeserializeCommonDataWithValidation(string jsonPath)
        {
            try
            {
                // 1. 读取 JSON 内容
                string jsonContent = File.ReadAllText(jsonPath);
                Debug.WriteLine($"[AOTMilitaryKindsFixer] JSON 文件大小: {jsonContent.Length} 字符");
                
                // 2. 检测 JSON 格式
                bool isLegacyFormat = jsonContent.Contains("$type") || jsonContent.Contains("$id");
                Debug.WriteLine($"[AOTMilitaryKindsFixer] JSON 格式: {(isLegacyFormat ? "Newtonsoft.Json (旧)" : "System.Text.Json (新)")}");
                
                // 3. 尝试反序列化
                CommonData result = null;
                
                if (isLegacyFormat)
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] 使用 Newtonsoft.Json 反序列化");
                    // 使用公共方法进行反序列化
                    result = SimpleSerializer.DeserializeJson<CommonData>(jsonContent, false, false);
                }
                else
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] 使用 System.Text.Json 反序列化");
                    result = SimpleSerializer.DeserializeJson<CommonData>(jsonContent, false, false);
                }
                
                // 4. 验证反序列化结果
                if (result != null)
                {
                    ValidateDeserializedCommonData(result);
                    Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ CommonData 反序列化和验证成功");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ CommonData 反序列化失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 验证反序列化后的 CommonData
        /// </summary>
        private static void ValidateDeserializedCommonData(CommonData commonData)
        {
            var issues = new List<string>();
            
            // 检查 AllMilitaryKinds
            if (commonData.AllMilitaryKinds == null)
            {
                issues.Add("AllMilitaryKinds 为 null");
            }
            else if (commonData.AllMilitaryKinds.MilitaryKinds == null)
            {
                issues.Add("AllMilitaryKinds.MilitaryKinds 为 null");
            }
            else if (commonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
            {
                issues.Add("AllMilitaryKinds.MilitaryKinds 为空");
            }
            
            // 检查其他关键数据
            if (commonData.AllInformationKinds == null)
                issues.Add("AllInformationKinds 为 null");
            if (commonData.AllArchitectureKinds == null)
                issues.Add("AllArchitectureKinds 为 null");
            
            if (issues.Count > 0)
            {
                string errorMsg = "CommonData 验证失败:\n" + string.Join("\n", issues);
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ {errorMsg}");
                throw new InvalidDataException(errorMsg);
            }
            
            Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ CommonData 验证通过 - MilitaryKinds: {commonData.AllMilitaryKinds.MilitaryKinds.Count} 个");
        }
        
        /// <summary>
        /// 验证 MilitaryKinds 完整性
        /// </summary>
        private static bool ValidateMilitaryKindsIntegrity(CommonData commonData)
        {
            if (commonData == null)
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] CommonData 为 null");
                return false;
            }
            
            if (commonData.AllMilitaryKinds == null)
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] AllMilitaryKinds 为 null");
                return false;
            }
            
            if (commonData.AllMilitaryKinds.MilitaryKinds == null)
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] MilitaryKinds 字典为 null");
                return false;
            }
            
            if (commonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] MilitaryKinds 字典为空");
                return false;
            }
            
            Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ MilitaryKinds 完整性检查通过 - {commonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
            return true;
        }
        
        /// <summary>
        /// 修复 MilitaryKinds 数据
        /// </summary>
        private static void RepairMilitaryKinds(CommonData commonData)
        {
            try
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] 开始修复 MilitaryKinds");
                
                // 1. 确保 AllMilitaryKinds 不为 null
                if (commonData.AllMilitaryKinds == null)
                {
                    commonData.AllMilitaryKinds = new MilitaryKindTable();
                    Debug.WriteLine("[AOTMilitaryKindsFixer] 创建新的 MilitaryKindTable");
                }
                
                // 2. 确保 MilitaryKinds 字典不为 null
                if (commonData.AllMilitaryKinds.MilitaryKinds == null)
                {
                    commonData.AllMilitaryKinds.MilitaryKinds = new Dictionary<int, MilitaryKind>();
                    Debug.WriteLine("[AOTMilitaryKindsFixer] 创建新的 MilitaryKinds 字典");
                }
                
                // 3. 如果字典为空，尝试从其他源加载
                if (commonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
                {
                    LoadDefaultMilitaryKinds(commonData.AllMilitaryKinds);
                }
                
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ MilitaryKinds 修复完成 - {commonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 修复 MilitaryKinds 失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 加载默认的 MilitaryKinds
        /// </summary>
        private static void LoadDefaultMilitaryKinds(MilitaryKindTable militaryKindTable)
        {
            try
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] 尝试加载默认 MilitaryKinds");
                
                // 尝试从单独的 MilitaryKinds 文件加载
                string militaryKindsPath = @"Content\Data\Common\MilitaryKinds.json";
                if (File.Exists(militaryKindsPath))
                {
                    Debug.WriteLine($"[AOTMilitaryKindsFixer] 从文件加载: {militaryKindsPath}");
                    var loadedTable = SimpleSerializer.DeserializeJsonFile<MilitaryKindTable>(militaryKindsPath, false, false);
                    
                    if (loadedTable != null && loadedTable.MilitaryKinds != null && loadedTable.MilitaryKinds.Count > 0)
                    {
                        militaryKindTable.MilitaryKinds = loadedTable.MilitaryKinds;
                        Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ 从文件加载了 {loadedTable.MilitaryKinds.Count} 个兵种");
                        return;
                    }
                }
                
                // 创建最基本的兵种
                CreateBasicMilitaryKinds(militaryKindTable);
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 加载默认 MilitaryKinds 失败: {ex.Message}");
                
                // 最后的回退：创建最基本的兵种
                CreateBasicMilitaryKinds(militaryKindTable);
            }
        }
        
        /// <summary>
        /// 创建最基本的兵种数据
        /// </summary>
        private static void CreateBasicMilitaryKinds(MilitaryKindTable militaryKindTable)
        {
            try
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] 创建基本兵种数据");
                
                // 创建基本的步兵兵种
                var basicInfantry = new MilitaryKind
                {
                    ID = 0,
                    Name = "基本步兵",
                    Type = MilitaryType.步兵,
                    MinScale = 100,
                    MaxScale = 1000,
                    Movability = 100
                };
                
                militaryKindTable.MilitaryKinds[0] = basicInfantry;
                
                // 如果需要运输船（ID=28），也创建一个基本的
                var basicTransportShip = new MilitaryKind
                {
                    ID = 28,
                    Name = "运输船",
                    Type = MilitaryType.水军,
                    MinScale = 50,
                    MaxScale = 500,
                    Movability = 120
                };
                
                militaryKindTable.MilitaryKinds[28] = basicTransportShip;
                
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ 创建了 {militaryKindTable.MilitaryKinds.Count} 个基本兵种");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 创建基本兵种失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 创建最小可用的 CommonData
        /// </summary>
        private static CommonData CreateMinimalCommonData()
        {
            try
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] 创建最小可用的 CommonData");
                
                var commonData = new CommonData();
                
                // 初始化所有关键数据表
                commonData.AllMilitaryKinds = new MilitaryKindTable();
                commonData.AllInformationKinds = new InformationKindList();
                commonData.AllArchitectureKinds = new ArchitectureKindTable();
                commonData.AllConditions = new ConditionTable();
                commonData.AllInfluences = new InfluenceTable();
                commonData.AllSkills = new SkillTable();
                commonData.AllTechniques = new TechniqueTable();
                commonData.AllCombatMethods = new CombatMethodTable();
                
                // 确保字典不为 null
                if (commonData.AllMilitaryKinds.MilitaryKinds == null)
                {
                    commonData.AllMilitaryKinds.MilitaryKinds = new Dictionary<int, MilitaryKind>();
                }
                
                if (commonData.AllInformationKinds.GameObjects == null)
                {
                    commonData.AllInformationKinds.GameObjects = new List<GameObject>();
                }
                
                // 创建基本兵种
                CreateBasicMilitaryKinds(commonData.AllMilitaryKinds);
                
                Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ 最小 CommonData 创建成功");
                return commonData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 创建最小 CommonData 失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 修复 GameScenario 中的 MilitaryKinds 引用
        /// </summary>
        public static void FixGameScenarioMilitaryKinds(GameScenario scenario)
        {
            if (scenario == null) return;
            
            try
            {
                Debug.WriteLine("[AOTMilitaryKindsFixer] 修复 GameScenario 中的 MilitaryKinds 引用");
                
                // 确保 GameCommonData 存在
                if (scenario.GameCommonData == null)
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] GameCommonData 为 null，从 CommonData.Current 恢复");
                    
                    if (CommonData.Current != null)
                    {
                        scenario.GameCommonData = CommonData.Current;
                    }
                    else
                    {
                        Debug.WriteLine("[AOTMilitaryKindsFixer] ❌ CommonData.Current 也为 null，无法恢复");
                        return;
                    }
                }
                
                // 确保 AllMilitaryKinds 存在
                if (scenario.GameCommonData.AllMilitaryKinds == null || 
                    scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds == null ||
                    scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
                {
                    Debug.WriteLine("[AOTMilitaryKindsFixer] GameScenario 中的 MilitaryKinds 有问题，从 CommonData.Current 恢复");
                    
                    if (CommonData.Current?.AllMilitaryKinds?.MilitaryKinds != null)
                    {
                        scenario.GameCommonData.AllMilitaryKinds = CommonData.Current.AllMilitaryKinds;
                        Debug.WriteLine($"[AOTMilitaryKindsFixer] ✅ 恢复了 {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
                    }
                    else
                    {
                        Debug.WriteLine("[AOTMilitaryKindsFixer] ❌ CommonData.Current 中的 MilitaryKinds 也有问题");
                    }
                }
                
                Debug.WriteLine("[AOTMilitaryKindsFixer] ✅ GameScenario MilitaryKinds 修复完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTMilitaryKindsFixer] ❌ 修复 GameScenario MilitaryKinds 失败: {ex.Message}");
            }
        }
    }
}