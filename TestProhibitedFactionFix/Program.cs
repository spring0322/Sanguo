using System;
using System.Collections.Generic;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;
using WorldOfTheThreeKingdoms.Serialization.SystemTextJson;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== ProhibitedFactionID 字典转换修复测试 ===");
        
        // 测试不同格式的 JSON 数据
        TestDictionaryConversion();
        
        Console.WriteLine("\n✅ 所有测试完成！");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
    
    static void TestDictionaryConversion()
    {
        var options = GameJsonContext.GetDefaultOptions();
        
        // 测试1: 标准对象格式
        Console.WriteLine("\n1. 测试标准对象格式:");
        string json1 = @"{""1"": 30, ""2"": 60, ""3"": 90}";
        TestJsonConversion(json1, options, "标准对象格式");
        
        // 测试2: Key-Value 数组格式
        Console.WriteLine("\n2. 测试 Key-Value 数组格式:");
        string json2 = @"[{""Key"": 1, ""Value"": 30}, {""Key"": 2, ""Value"": 60}]";
        TestJsonConversion(json2, options, "Key-Value 数组格式");
        
        // 测试3: 字符串格式
        Console.WriteLine("\n3. 测试字符串格式:");
        string json3 = @"""1:30,2:60,3:90""";
        TestJsonConversion(json3, options, "字符串格式");
        
        // 测试4: 空值
        Console.WriteLine("\n4. 测试空值:");
        string json4 = "null";
        TestJsonConversion(json4, options, "空值");
        
        // 测试5: 空对象
        Console.WriteLine("\n5. 测试空对象:");
        string json5 = "{}";
        TestJsonConversion(json5, options, "空对象");
        
        // 测试6: 混合数字和字符串
        Console.WriteLine("\n6. 测试混合格式:");
        string json6 = @"{""1"": ""30"", ""2"": 60, ""3"": ""90""}";
        TestJsonConversion(json6, options, "混合格式");
    }
    
    static void TestJsonConversion(string json, JsonSerializerOptions options, string testName)
    {
        try
        {
            Console.WriteLine($"   输入: {json}");
            
            var result = JsonSerializer.Deserialize<Dictionary<int, int>>(json, options);
            
            if (result != null)
            {
                Console.WriteLine($"   ✅ {testName} 转换成功，包含 {result.Count} 个项目:");
                foreach (var kvp in result)
                {
                    Console.WriteLine($"      {kvp.Key} -> {kvp.Value}");
                }
            }
            else
            {
                Console.WriteLine($"   ⚠️ {testName} 转换结果为 null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ {testName} 转换失败: {ex.Message}");
        }
    }
}