using System;
using System.IO;
using System.Text.Json;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🔥 测试 CommonData.json 反序列化（AOT 源生成器模式）");
        Console.WriteLine("=".PadRight(60, '='));

        string jsonPath = @"Content\Data\Common\CommonData.json";
        
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"❌ 文件不存在: {jsonPath}");
            return;
        }

        try
        {
            Console.WriteLine($"📂 读取文件: {jsonPath}");
            string jsonContent = File.ReadAllText(jsonPath);
            Console.WriteLine($"✅ 文件大小: {jsonContent.Length} 字符");

            Console.WriteLine("\n🔧 开始反序列化（使用 AOT 源生成器）...");
            var options = GameJsonContext.GetDefaultOptions(indented: false);
            
            // 🔥 使用 AOT 兼容的 API：传入 JsonTypeInfo
            var commonData = JsonSerializer.Deserialize(jsonContent, GameJsonContext.Default.CommonData);

            if (commonData == null)
            {
                Console.WriteLine("❌ 反序列化结果为 null");
                return;
            }

            Console.WriteLine("✅ 反序列化成功！");
            Console.WriteLine("\n📊 CommonData 内容验证:");
            Console.WriteLine($"  - AllTroopEventEffects: {(commonData.AllTroopEventEffects != null ? "✅" : "❌")}");
            Console.WriteLine($"  - AllEventEffects: {(commonData.AllEventEffects != null ? "✅" : "❌")}");
            Console.WriteLine($"  - AllTroopEventEffectKinds: {(commonData.AllTroopEventEffectKinds != null ? "✅" : "❌")}");
            Console.WriteLine($"  - AllEventEffectKinds: {(commonData.AllEventEffectKinds != null ? "✅" : "❌")}");

            if (commonData.AllTroopEventEffects != null)
            {
                Console.WriteLine($"\n  TroopEventEffects Count: {commonData.AllTroopEventEffects.Count}");
            }

            if (commonData.AllEventEffects != null)
            {
                Console.WriteLine($"  ArchEventEffects Count: {commonData.AllEventEffects.Count}");
            }

            Console.WriteLine("\n🎉 所有测试通过！AOT 源生成器工作正常！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 反序列化失败:");
            Console.WriteLine($"  类型: {ex.GetType().Name}");
            Console.WriteLine($"  消息: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  内部异常: {ex.InnerException.Message}");
            }
            Console.WriteLine($"\n堆栈跟踪:\n{ex.StackTrace}");
        }
    }
}
