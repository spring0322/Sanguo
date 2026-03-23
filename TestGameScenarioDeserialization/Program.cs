using System;
using System.IO;
using System.Text.Json;
using GameObjects;
using WorldOfTheThreeKingdoms.Tools;

class Program
{
    static void Main()
    {
        Console.WriteLine("🔥 测试 GameScenario 反序列化");
        
        try
        {
            string scenarioPath = @"bin\Debug\net8.0\Content\Data\Scenario\184DHZS.json";
            
            if (!File.Exists(scenarioPath))
            {
                Console.WriteLine($"❌ 文件不存在: {scenarioPath}");
                return;
            }
            
            Console.WriteLine($"📁 文件存在: {scenarioPath}");
            Console.WriteLine($"📏 文件大小: {new FileInfo(scenarioPath).Length} 字节");
            
            // 尝试反序列化
            Console.WriteLine("🔄 开始反序列化...");
            
            // 先读取文件内容
            string jsonContent = File.ReadAllText(scenarioPath);
            Console.WriteLine($"📄 JSON 内容长度: {jsonContent.Length} 字符");
            
            // 🔥 调试：检查 Architectures 的 JSON 结构
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                if (document.RootElement.TryGetProperty("Architectures", out var archElement))
                {
                    Console.WriteLine($"📊 Architectures JSON 结构:");
                    Console.WriteLine($"  - ValueKind: {archElement.ValueKind}");
                    
                    if (archElement.ValueKind == JsonValueKind.Object)
                    {
                        Console.WriteLine($"  - 对象属性:");
                        foreach (var prop in archElement.EnumerateObject())
                        {
                            Console.WriteLine($"    - {prop.Name}: {prop.Value.ValueKind}");
                            if (prop.Name == "GameObjects" && prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                Console.WriteLine($"      - 数组长度: {prop.Value.GetArrayLength()}");
                            }
                        }
                    }
                    
                    // 显示前200个字符
                    var rawText = archElement.GetRawText();
                    var preview = rawText.Length > 200 ? rawText.Substring(0, 200) + "..." : rawText;
                    Console.WriteLine($"  - JSON 预览: {preview}");
                }
                else
                {
                    Console.WriteLine("❌ 未找到 Architectures 属性");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JSON 结构分析失败: {ex.Message}");
            }
            
            // 尝试直接调用 SimpleSerializer
            var scenario = SimpleSerializer.DeserializeJson<GameScenario>(jsonContent, false, false);
            
            if (scenario == null)
            {
                Console.WriteLine("❌ 反序列化返回 null");
                return;
            }
            
            Console.WriteLine("✅ 反序列化成功！");
            
            // 检查关键属性
            Console.WriteLine($"📊 数据检查:");
            Console.WriteLine($"  - Factions: {scenario.Factions?.Count ?? 0} 项");
            Console.WriteLine($"  - Persons: {scenario.Persons?.Count ?? 0} 项");
            Console.WriteLine($"  - Architectures: {scenario.Architectures?.Count ?? 0} 项");
            Console.WriteLine($"  - Troops: {scenario.Troops?.Count ?? 0} 项");
            
            Console.WriteLine("🎉 GameScenario 反序列化测试成功！");
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
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}