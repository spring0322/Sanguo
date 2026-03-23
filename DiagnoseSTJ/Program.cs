using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Tools;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== System.Text.Json 反序列化问题诊断 ===");
        Console.WriteLine();
        
        string commonDataPath = @"Content\Data\Common\CommonData.json";
        
        if (!File.Exists(commonDataPath))
        {
            Console.WriteLine($"❌ 文件不存在: {commonDataPath}");
            return;
        }
        
        try
        {
            // 1. 检查文件内容
            string content = File.ReadAllText(commonDataPath);
            Console.WriteLine($"📄 文件大小: {content.Length} 字符");
            Console.WriteLine($"📄 文件前100字符: {content.Substring(0, Math.Min(100, content.Length))}");
            Console.WriteLine();
            
            // 2. 检查 JSON 格式
            bool hasTypeInfo = content.Contains("$type");
            bool hasReferenceInfo = content.Contains("$id") || content.Contains("$ref");
            bool hasValuesWrapper = content.Contains("$values");
            
            Console.WriteLine($"🔍 JSON 格式分析:");
            Console.WriteLine($"   - 包含 $type: {hasTypeInfo}");
            Console.WriteLine($"   - 包含 $id/$ref: {hasReferenceInfo}");
            Console.WriteLine($"   - 包含 $values: {hasValuesWrapper}");
            Console.WriteLine();
            
            // 3. 尝试基础 JSON 解析
            try
            {
                using var document = JsonDocument.Parse(content);
                Console.WriteLine("✅ 基础 JSON 解析成功");
                
                var root = document.RootElement;
                Console.WriteLine($"   - 根元素类型: {root.ValueKind}");
                
                if (root.ValueKind == JsonValueKind.Object)
                {
                    Console.WriteLine($"   - 属性数量: {root.EnumerateObject().Count()}");
                    
                    // 检查关键属性
                    var keyProperties = new[] { "AllMilitaryKinds", "AllInformationKinds", "AllIdealTendencyKinds", "AllArchitectureKinds" };
                    foreach (var prop in keyProperties)
                    {
                        bool exists = root.TryGetProperty(prop, out var propElement);
                        Console.WriteLine($"   - {prop}: {(exists ? "存在" : "缺失")} {(exists ? $"({propElement.ValueKind})" : "")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 基础 JSON 解析失败: {ex.Message}");
            }
            
            Console.WriteLine();
            
            // 4. 尝试使用 GameJsonContext 反序列化
            try
            {
                Console.WriteLine("🔍 尝试使用 GameJsonContext 反序列化...");
                var options = GameJsonContext.GetDefaultOptions();
                
                var result = JsonSerializer.Deserialize<CommonData>(content, options);
                
                if (result == null)
                {
                    Console.WriteLine("❌ GameJsonContext 反序列化返回 null");
                }
                else
                {
                    Console.WriteLine("✅ GameJsonContext 反序列化成功");
                    Console.WriteLine($"   - AllMilitaryKinds: {result.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0}");
                    Console.WriteLine($"   - AllInformationKinds: {result.AllInformationKinds?.GameObjects?.Count ?? 0}");
                    Console.WriteLine($"   - AllIdealTendencyKinds: {result.AllIdealTendencyKinds?.Count ?? 0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GameJsonContext 反序列化失败: {ex.Message}");
                Console.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            
            // 5. 尝试使用 SimpleSerializer
            try
            {
                Console.WriteLine("🔍 尝试使用 SimpleSerializer 反序列化...");
                var result = SimpleSerializer.DeserializeJson<CommonData>(content);
                
                if (result == null)
                {
                    Console.WriteLine("❌ SimpleSerializer 反序列化返回 null");
                }
                else
                {
                    Console.WriteLine("✅ SimpleSerializer 反序列化成功");
                    Console.WriteLine($"   - AllMilitaryKinds: {result.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0}");
                    Console.WriteLine($"   - AllInformationKinds: {result.AllInformationKinds?.GameObjects?.Count ?? 0}");
                    Console.WriteLine($"   - AllIdealTendencyKinds: {result.AllIdealTendencyKinds?.Count ?? 0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SimpleSerializer 反序列化失败: {ex.Message}");
                Console.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 诊断过程中发生异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}