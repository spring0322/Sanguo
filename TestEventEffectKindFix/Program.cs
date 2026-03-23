using System;
using System.Collections.Generic;
using System.Text.Json;
using GameObjects.TroopDetail.EventEffect;
using WorldOfTheThreeKingdoms.Serialization;

class Program
{
    static void Main()
    {
        Console.WriteLine("🔥 测试 EventEffectKind AOT 修复");
        
        try
        {
            // 获取 AOT 优化的序列化选项
            var options = GameJsonContext.GetDefaultOptions();
            
            // 创建测试字典 - 这是导致错误的确切类型
            var testDict = new System.Collections.Generic.Dictionary<System.Int32, GameObjects.TroopDetail.EventEffect.EventEffectKind>();
            
            // 添加测试数据
            testDict[1] = new GameObjects.TroopDetail.EventEffect.EventEffectKind 
            { 
                ID = 1, 
                Name = "测试效果1" 
            };
            testDict[2] = new GameObjects.TroopDetail.EventEffect.EventEffectKind 
            { 
                ID = 2, 
                Name = "测试效果2" 
            };
            
            Console.WriteLine($"✅ 创建字典成功: {testDict.Count} 项");
            
            // 测试序列化
            string json = JsonSerializer.Serialize(testDict, options);
            Console.WriteLine($"✅ 序列化成功: {json.Length} 字符");
            Console.WriteLine($"JSON 预览: {json.Substring(0, Math.Min(100, json.Length))}...");
            
            // 测试反序列化 - 这是失败的关键点
            var deserializedDict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<System.Int32, GameObjects.TroopDetail.EventEffect.EventEffectKind>>(json, options);
            Console.WriteLine($"✅ 反序列化成功: {deserializedDict.Count} 项");
            
            // 验证数据完整性
            foreach (var kvp in deserializedDict)
            {
                Console.WriteLine($"  - ID: {kvp.Key}, Name: {kvp.Value.Name}");
            }
            
            Console.WriteLine("🎉 AOT EventEffectKind 字典修复验证成功！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.GetType().Name}");
            Console.WriteLine($"错误详情: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}