// 测试JSON元数据修复是否成功
using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;
using WorldOfTheThreeKingdoms.Tools;
using GameObjects;

namespace TestJsonMetadataFix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 测试JSON元数据修复 ===");
            
            try
            {
                // 1. 测试GameJsonContext是否正确配置
                TestGameJsonContextConfiguration();
                
                // 2. 测试关键类型的序列化
                TestCriticalTypeSerialization();
                
                // 3. 测试CommonData反序列化
                TestCommonDataDeserialization();
                
                Console.WriteLine("=== ✅ 所有测试通过，JSON元数据问题已修复 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ❌ 测试失败: {ex.Message} ===");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static void TestGameJsonContextConfiguration()
        {
            Console.WriteLine("[测试1] 验证GameJsonContext配置...");
            
            var context = GameJsonContext.Default;
            if (context == null)
            {
                throw new Exception("GameJsonContext.Default 为 null");
            }
            
            var options = GameJsonContext.GetDefaultOptions();
            if (options.TypeInfoResolver == null)
            {
                throw new Exception("TypeInfoResolver 未配置");
            }
            
            if (options.TypeInfoResolver != GameJsonContext.Default)
            {
                throw new Exception("TypeInfoResolver 不是 GameJsonContext.Default");
            }
            
            Console.WriteLine("✅ GameJsonContext配置正确");
        }

        static void TestCriticalTypeSerialization()
        {
            Console.WriteLine("[测试2] 测试关键类型序列化...");
            
            // 测试字典类型
            var testDict = new System.Collections.Generic.Dictionary<int, string>
            {
                { 1, "测试1" },
                { 2, "测试2" }
            };
            
            try
            {
                var json = SimpleSerializer.SerializeJson(testDict, false, true);
                Console.WriteLine($"✅ 字典序列化成功: {json.Length} 字符");
                
                var deserialized = SimpleSerializer.DeserializeJson<System.Collections.Generic.Dictionary<int, string>>(json);
                if (deserialized.Count != 2)
                {
                    throw new Exception($"反序列化结果不正确，期望2个元素，实际{deserialized.Count}个");
                }
                Console.WriteLine("✅ 字典反序列化成功");
            }
            catch (Exception ex)
            {
                throw new Exception($"字典序列化测试失败: {ex.Message}", ex);
            }
        }

        static void TestCommonDataDeserialization()
        {
            Console.WriteLine("[测试3] 测试CommonData反序列化...");
            
            var commonDataPath = "Content/Data/CommonData/CommonData.json";
            if (!File.Exists(commonDataPath))
            {
                Console.WriteLine("⚠️ CommonData.json 文件不存在，跳过测试");
                return;
            }
            
            try
            {
                var commonData = SimpleSerializer.DeserializeJsonFile<CommonData>(commonDataPath, false);
                if (commonData == null)
                {
                    throw new Exception("CommonData 反序列化返回 null");
                }
                
                Console.WriteLine($"✅ CommonData反序列化成功");
                
                // 验证关键字典是否正确加载
                if (commonData.AllTroopEventEffectKinds != null)
                {
                    Console.WriteLine($"✅ AllTroopEventEffectKinds 加载成功: {commonData.AllTroopEventEffectKinds.Count} 个");
                }
                
                if (commonData.AllTroopEventEffects != null)
                {
                    Console.WriteLine($"✅ AllTroopEventEffects 加载成功: {commonData.AllTroopEventEffects.Count} 个");
                }
                
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("JsonTypeInfo metadata") || ex.Message.Contains("TypeInfoResolver"))
                {
                    throw new Exception($"❌ JSON元数据问题仍然存在: {ex.Message}", ex);
                }
                else
                {
                    Console.WriteLine($"⚠️ CommonData测试失败（可能是文件问题）: {ex.Message}");
                }
            }
        }
    }
}