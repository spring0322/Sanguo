using System;
using System.IO;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.Tools;
using GameObjects;

/// <summary>
/// 测试当前修复状态 - 验证打开文件中提到的问题是否已解决
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🔍 测试当前修复状态");
        Console.WriteLine("=====================================");
        
        try
        {
            // 1. 测试 CommonData 反序列化
            TestCommonDataDeserialization();
            
            // 2. 测试 EventEffectKind 字典反序列化
            TestEventEffectKindDeserialization();
            
            // 3. 测试 MilitaryKinds 反序列化
            TestMilitaryKindsDeserialization();
            
            // 4. 测试 TextMessageTable 转换器
            TestTextMessageTableConverter();
            
            Console.WriteLine("\n✅ 所有测试完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
    
    static void TestCommonDataDeserialization()
    {
        Console.WriteLine("\n1. 测试 CommonData 反序列化");
        Console.WriteLine("----------------------------");
        
        string commonDataPath = @"Content\Data\Common\CommonData.json";
        
        if (!File.Exists(commonDataPath))
        {
            Console.WriteLine($"❌ 文件不存在: {commonDataPath}");
            return;
        }
        
        try
        {
            var commonData = SimpleSerializer.DeserializeJsonFile<CommonData>(commonDataPath, false);
            
            if (commonData == null)
            {
                Console.WriteLine("❌ CommonData 反序列化返回 null");
                return;
            }
            
            Console.WriteLine("✅ CommonData 反序列化成功");
            
            // 检查关键属性
            if (commonData.AllMilitaryKinds?.MilitaryKinds != null)
            {
                Console.WriteLine($"✅ AllMilitaryKinds: {commonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
            }
            else
            {
                Console.WriteLine("❌ AllMilitaryKinds.MilitaryKinds 为 null");
            }
            
            if (commonData.AllInformationKinds?.GameObjects != null)
            {
                Console.WriteLine($"✅ AllInformationKinds: {commonData.AllInformationKinds.GameObjects.Count} 个情报类型");
            }
            else
            {
                Console.WriteLine("❌ AllInformationKinds.GameObjects 为 null");
            }
            
            if (commonData.AllEventEffectKinds?.EventEffectKinds != null)
            {
                Console.WriteLine($"✅ AllEventEffectKinds: {commonData.AllEventEffectKinds.EventEffectKinds.Count} 个事件效果类型");
            }
            else
            {
                Console.WriteLine("❌ AllEventEffectKinds.EventEffectKinds 为 null");
            }
            
            if (commonData.AllTroopEventEffectKinds?.EventEffectKinds != null)
            {
                Console.WriteLine($"✅ AllTroopEventEffectKinds: {commonData.AllTroopEventEffectKinds.EventEffectKinds.Count} 个部队事件效果类型");
            }
            else
            {
                Console.WriteLine("❌ AllTroopEventEffectKinds.EventEffectKinds 为 null");
            }
            
            if (commonData.AllIdealTendencyKinds != null)
            {
                Console.WriteLine($"✅ AllIdealTendencyKinds: {commonData.AllIdealTendencyKinds.Count} 个理想倾向类型");
            }
            else
            {
                Console.WriteLine("❌ AllIdealTendencyKinds 为 null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CommonData 反序列化失败: {ex.Message}");
            
            // 检查是否是 AOT 类型注册问题
            if (ex.Message.Contains("JsonTypeInfo metadata") || ex.Message.Contains("TypeInfoResolver"))
            {
                Console.WriteLine("🔍 这是 AOT 类型注册问题，需要在 GameJsonContext 中添加类型注册");
            }
        }
    }
    
    static void TestEventEffectKindDeserialization()
    {
        Console.WriteLine("\n2. 测试 EventEffectKind 字典反序列化");
        Console.WriteLine("------------------------------------");
        
        try
        {
            // 创建测试数据
            string testJson = @"[
                {""Key"": 1, ""Value"": {""ID"": 1, ""Name"": ""测试效果1""}},
                {""Key"": 2, ""Value"": {""ID"": 2, ""Name"": ""测试效果2""}}
            ]";
            
            var dict = SimpleSerializer.DeserializeJson<Dictionary<int, GameObjects.TroopDetail.EventEffect.EventEffectKind>>(testJson);
            
            if (dict != null && dict.Count > 0)
            {
                Console.WriteLine($"✅ EventEffectKind 字典反序列化成功: {dict.Count} 个项目");
                foreach (var kvp in dict)
                {
                    Console.WriteLine($"   - ID {kvp.Key}: {kvp.Value?.Name ?? "null"}");
                }
            }
            else
            {
                Console.WriteLine("❌ EventEffectKind 字典反序列化失败或为空");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ EventEffectKind 字典反序列化失败: {ex.Message}");
        }
    }
    
    static void TestMilitaryKindsDeserialization()
    {
        Console.WriteLine("\n3. 测试 MilitaryKinds 反序列化");
        Console.WriteLine("------------------------------");
        
        try
        {
            // 创建测试数据
            string testJson = @"[
                {""Key"": 0, ""Value"": {""ID"": 0, ""Name"": ""步兵"", ""Type"": 0}},
                {""Key"": 1, ""Value"": {""ID"": 1, ""Name"": ""骑兵"", ""Type"": 1}}
            ]";
            
            var dict = SimpleSerializer.DeserializeJson<Dictionary<int, GameObjects.TroopDetail.MilitaryKind>>(testJson);
            
            if (dict != null && dict.Count > 0)
            {
                Console.WriteLine($"✅ MilitaryKind 字典反序列化成功: {dict.Count} 个兵种");
                foreach (var kvp in dict)
                {
                    Console.WriteLine($"   - ID {kvp.Key}: {kvp.Value?.Name ?? "null"}");
                }
            }
            else
            {
                Console.WriteLine("❌ MilitaryKind 字典反序列化失败或为空");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MilitaryKind 字典反序列化失败: {ex.Message}");
        }
    }
    
    static void TestTextMessageTableConverter()
    {
        Console.WriteLine("\n4. 测试 TextMessageTable 转换器");
        Console.WriteLine("-------------------------------");
        
        try
        {
            // 创建测试 TextMessageTable
            var textMessageTable = new GameObjects.PersonDetail.TextMessageTable();
            
            // 序列化测试
            string json = SimpleSerializer.SerializeJson(textMessageTable, false, true);
            
            if (!string.IsNullOrEmpty(json))
            {
                Console.WriteLine("✅ TextMessageTable 序列化成功");
                
                // 反序列化测试
                var deserializedTable = SimpleSerializer.DeserializeJson<GameObjects.PersonDetail.TextMessageTable>(json);
                
                if (deserializedTable != null)
                {
                    Console.WriteLine("✅ TextMessageTable 反序列化成功");
                }
                else
                {
                    Console.WriteLine("❌ TextMessageTable 反序列化返回 null");
                }
            }
            else
            {
                Console.WriteLine("❌ TextMessageTable 序列化失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TextMessageTable 转换器测试失败: {ex.Message}");
            
            if (ex.Message.Contains("converter") && ex.Message.Contains("not compatible"))
            {
                Console.WriteLine("🔍 这是转换器兼容性问题，需要检查转换器应用位置");
            }
        }
    }
}