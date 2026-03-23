using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

/// <summary>
/// 测试剧本反序列化，定位 ExecutionEngineException 的具体位置
/// </summary>
class TestScenarioDeserialization
{
    static void Main(string[] args)
    {
        string scenarioPath = "Content/Data/Scenario/184DHZS.json";
        
        if (args.Length > 0)
        {
            scenarioPath = args[0];
        }
        
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  剧本反序列化测试                                          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"剧本文件: {scenarioPath}");
        Console.WriteLine();
        
        if (!File.Exists(scenarioPath))
        {
            Console.WriteLine($"❌ 文件不存在: {scenarioPath}");
            return;
        }
        
        try
        {
            Console.WriteLine("步骤1：读取 JSON 文件...");
            string json = File.ReadAllText(scenarioPath);
            Console.WriteLine($"  ✅ 文件读取成功，长度: {json.Length}");
            Console.WriteLine();
            
            Console.WriteLine("步骤2：获取 JsonSerializerOptions...");
            JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
            Console.WriteLine($"  ✅ Options 创建成功");
            Console.WriteLine($"  - TypeInfoResolver: {options.TypeInfoResolver?.GetType().Name ?? "null"}");
            Console.WriteLine($"  - DefaultIgnoreCondition: {options.DefaultIgnoreCondition}");
            Console.WriteLine();
            
            Console.WriteLine("步骤3：反序列化 GameScenarioDTO...");
            Console.WriteLine("  ⚠️ 这一步可能触发 ExecutionEngineException");
            Console.WriteLine();
            
            GameScenarioDTO? dto = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
            
            if (dto == null)
            {
                Console.WriteLine("  ❌ 反序列化返回 null");
                return;
            }
            
            Console.WriteLine("  ✅ 反序列化成功！");
            Console.WriteLine();
            
            Console.WriteLine("步骤4：检查反序列化结果...");
            Console.WriteLine($"  - ScenarioTitle: {dto.ScenarioTitle}");
            Console.WriteLine($"  - Architectures: {dto.Architectures?.Count ?? 0} 个");
            Console.WriteLine($"  - Persons: {dto.Persons?.Count ?? 0} 个");
            Console.WriteLine($"  - Factions: {dto.Factions?.Count ?? 0} 个");
            Console.WriteLine($"  - Troops: {dto.Troops?.Count ?? 0} 个");
            Console.WriteLine();
            
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✅ 测试通过！反序列化没有问题                            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("结论：ExecutionEngineException 不是由反序列化引起的");
            Console.WriteLine("建议：检查 ProcessScenarioData 方法中的逻辑");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"  ❌ JSON 反序列化失败");
            Console.WriteLine($"  类型: {ex.GetType().Name}");
            Console.WriteLine($"  消息: {ex.Message}");
            Console.WriteLine($"  路径: {ex.Path}");
            Console.WriteLine($"  行号: {ex.LineNumber}");
            Console.WriteLine($"  位置: {ex.BytePositionInLine}");
            Console.WriteLine();
            Console.WriteLine($"  堆栈:");
            Console.WriteLine(ex.StackTrace);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 发生异常");
            Console.WriteLine($"  类型: {ex.GetType().FullName}");
            Console.WriteLine($"  消息: {ex.Message}");
            Console.WriteLine($"  HResult: 0x{ex.HResult:X8}");
            Console.WriteLine();
            Console.WriteLine($"  堆栈:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            
            // 递归输出内部异常
            var innerEx = ex.InnerException;
            int depth = 1;
            while (innerEx != null)
            {
                Console.WriteLine($"  内部异常 [{depth}]:");
                Console.WriteLine($"    类型: {innerEx.GetType().FullName}");
                Console.WriteLine($"    消息: {innerEx.Message}");
                Console.WriteLine($"    堆栈:");
                Console.WriteLine(innerEx.StackTrace);
                Console.WriteLine();
                innerEx = innerEx.InnerException;
                depth++;
            }
        }
    }
}
