// 测试EventEffectTable类型注册修复
using System;
using System.Collections.Generic;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;
using WorldOfTheThreeKingdoms.Tools;
using GameObjects.TroopDetail.EventEffect;

namespace TestEventEffectTableFix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 测试EventEffectTable类型注册修复 ===");
            
            try
            {
                // 1. 测试EventEffectTable类型是否正确注册
                TestEventEffectTableRegistration();
                
                // 2. 测试EventEffectTable序列化
                TestEventEffectTableSerialization();
                
                Console.WriteLine("=== ✅ EventEffectTable类型注册修复成功 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ❌ 测试失败: {ex.Message} ===");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static void TestEventEffectTableRegistration()
        {
            Console.WriteLine("[测试1] 验证EventEffectTable类型注册...");
            
            // 直接测试序列化，而不是检查类型注册
            try
            {
                var eventEffectTable = new EventEffectTable();
                var options = GameJsonContext.GetDefaultOptions();
                var json = JsonSerializer.Serialize(eventEffectTable, typeof(EventEffectTable), options);
                Console.WriteLine("✅ EventEffectTable类型可以正常序列化");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("JsonTypeInfo metadata"))
                {
                    throw new Exception($"EventEffectTable类型未正确注册: {ex.Message}", ex);
                }
                throw new Exception($"EventEffectTable类型注册验证失败: {ex.Message}", ex);
            }
        }

        static void TestEventEffectTableSerialization()
        {
            Console.WriteLine("[测试2] 测试EventEffectTable序列化...");
            
            // 创建一个测试的EventEffectTable
            var eventEffectTable = new EventEffectTable();
            
            try
            {
                // 测试序列化
                var json = SimpleSerializer.SerializeJson(eventEffectTable, false, true);
                Console.WriteLine($"✅ EventEffectTable序列化成功: {json.Length} 字符");
                
                // 测试反序列化
                var deserialized = SimpleSerializer.DeserializeJson<EventEffectTable>(json);
                if (deserialized == null)
                {
                    throw new Exception("EventEffectTable反序列化返回null");
                }
                Console.WriteLine("✅ EventEffectTable反序列化成功");
                
                // 验证EventEffects字典
                if (deserialized.EventEffects == null)
                {
                    throw new Exception("EventEffects字典为null");
                }
                Console.WriteLine($"✅ EventEffects字典正常: {deserialized.EventEffects.Count} 个元素");
                
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("JsonTypeInfo metadata") || ex.Message.Contains("TypeInfoResolver"))
                {
                    throw new Exception($"❌ EventEffectTable类型注册问题仍然存在: {ex.Message}", ex);
                }
                else
                {
                    throw new Exception($"EventEffectTable序列化测试失败: {ex.Message}", ex);
                }
            }
        }
    }
}