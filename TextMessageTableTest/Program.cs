using System;
using System.Collections.Generic;
using System.Text.Json;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.Serialization.SystemTextJson;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== TextMessageTable STJ 反序列化修复测试 ===");
        
        try
        {
            // 创建测试数据
            var table = new TextMessageTable();
            var key1 = new KeyValuePair<int, TextMessageKind>(1, TextMessageKind.Ability);
            var key2 = new KeyValuePair<int, TextMessageKind>(2, TextMessageKind.Ideal);
            
            table.textMessages[key1] = new List<string> { "测试消息1", "测试消息2" };
            table.textMessages[key2] = new List<string> { "理想消息1", "理想消息2" };
            
            Console.WriteLine($"✅ 创建测试数据成功，包含 {table.Count} 个条目");
            
            // 配置 STJ 选项
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TextMessageTableConverter() }
            };
            
            // 序列化测试
            string json = JsonSerializer.Serialize(table, options);
            Console.WriteLine("✅ STJ 序列化成功");
            Console.WriteLine($"JSON 长度: {json.Length} 字符");
            
            // 反序列化测试
            var deserializedTable = JsonSerializer.Deserialize<TextMessageTable>(json, options);
            Console.WriteLine($"✅ STJ 反序列化成功，恢复 {deserializedTable.Count} 个条目");
            
            // 验证数据完整性
            bool dataIntact = true;
            foreach (var kvp in table.textMessages)
            {
                if (!deserializedTable.textMessages.ContainsKey(kvp.Key))
                {
                    dataIntact = false;
                    Console.WriteLine($"❌ 缺失键: {kvp.Key}");
                }
                else
                {
                    var originalList = kvp.Value;
                    var deserializedList = deserializedTable.textMessages[kvp.Key];
                    if (originalList.Count != deserializedList.Count)
                    {
                        dataIntact = false;
                        Console.WriteLine($"❌ 列表长度不匹配: {kvp.Key}");
                    }
                }
            }
            
            if (dataIntact)
            {
                Console.WriteLine("✅ 数据完整性验证通过");
                Console.WriteLine("🎉 TextMessageTable STJ 转换器修复成功！");
            }
            else
            {
                Console.WriteLine("❌ 数据完整性验证失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
            Console.WriteLine($"异常类型: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
        }
        
        Console.WriteLine("\n测试完成。");
    }
}