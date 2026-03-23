using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

/// <summary>
/// 诊断 ExecutionEngineException 的工具
/// 用于检测剧本 JSON 文件中可能导致崩溃的问题
/// </summary>
class DiagnoseExecutionEngineException
{
    static void Main(string[] args)
    {
        string scenarioPath = "Content/Data/Scenario/184DHZS.json";
        
        if (args.Length > 0)
        {
            scenarioPath = args[0];
        }
        
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ExecutionEngineException 诊断工具                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"剧本文件: {scenarioPath}");
        Console.WriteLine();
        
        if (!File.Exists(scenarioPath))
        {
            Console.WriteLine($"❌ 文件不存在: {scenarioPath}");
            return;
        }
        
        // 检查1：文件大小
        Console.WriteLine("检查1：文件大小");
        FileInfo fileInfo = new FileInfo(scenarioPath);
        Console.WriteLine($"  文件大小: {fileInfo.Length / 1024 / 1024:F2} MB");
        
        if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
        {
            Console.WriteLine("  ⚠️ 警告：文件过大，可能导致内存问题");
        }
        else
        {
            Console.WriteLine("  ✅ 文件大小正常");
        }
        Console.WriteLine();
        
        // 检查2：JSON 结构深度
        Console.WriteLine("检查2：JSON 结构深度");
        try
        {
            string json = File.ReadAllText(scenarioPath);
            int maxDepth = CalculateMaxDepth(json);
            Console.WriteLine($"  最大嵌套深度: {maxDepth}");
            
            if (maxDepth > 64)
            {
                Console.WriteLine("  ⚠️ 警告：嵌套深度过深，可能导致栈溢出");
                Console.WriteLine("  建议：检查是否存在循环引用");
            }
            else
            {
                Console.WriteLine("  ✅ 嵌套深度正常");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 检查失败: {ex.Message}");
        }
        Console.WriteLine();
        
        // 检查3：数组大小
        Console.WriteLine("检查3：大型数组检测");
        try
        {
            string json = File.ReadAllText(scenarioPath);
            CheckLargeArrays(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 检查失败: {ex.Message}");
        }
        Console.WriteLine();
        
        // 检查4：尝试部分反序列化
        Console.WriteLine("检查4：分阶段反序列化测试");
        try
        {
            TestPartialDeserialization(scenarioPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 反序列化失败: {ex.GetType().Name}");
            Console.WriteLine($"  消息: {ex.Message}");
            Console.WriteLine($"  堆栈: {ex.StackTrace}");
        }
        Console.WriteLine();
        
        // 检查5：内存使用情况
        Console.WriteLine("检查5：内存使用情况");
        Process currentProcess = Process.GetCurrentProcess();
        Console.WriteLine($"  工作集: {currentProcess.WorkingSet64 / 1024 / 1024:F2} MB");
        Console.WriteLine($"  私有内存: {currentProcess.PrivateMemorySize64 / 1024 / 1024:F2} MB");
        Console.WriteLine();
        
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  诊断建议                                                  ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine("1. 如果嵌套深度过深，检查剧本转换器是否生成了循环引用");
        Console.WriteLine("2. 如果数组过大，考虑分批加载或优化数据结构");
        Console.WriteLine("3. 如果反序列化失败，检查 JSON 格式是否正确");
        Console.WriteLine("4. 尝试使用更小的测试剧本验证问题");
        Console.WriteLine("5. 检查是否是 AOT 编译问题（反射/序列化）");
    }
    
    static int CalculateMaxDepth(string json)
    {
        int maxDepth = 0;
        int currentDepth = 0;
        
        foreach (char c in json)
        {
            if (c == '{' || c == '[')
            {
                currentDepth++;
                if (currentDepth > maxDepth)
                {
                    maxDepth = currentDepth;
                }
            }
            else if (c == '}' || c == ']')
            {
                currentDepth--;
            }
        }
        
        return maxDepth;
    }
    
    static void CheckLargeArrays(string json)
    {
        // 简单检测：查找大型数组的模式
        int architecturesStart = json.IndexOf("\"Architectures\":");
        if (architecturesStart > 0)
        {
            int arrayStart = json.IndexOf('[', architecturesStart);
            int arrayEnd = FindMatchingBracket(json, arrayStart);
            int count = CountArrayElements(json.Substring(arrayStart, arrayEnd - arrayStart + 1));
            Console.WriteLine($"  Architectures 数量: {count}");
        }
        
        int personsStart = json.IndexOf("\"Persons\":");
        if (personsStart > 0)
        {
            int arrayStart = json.IndexOf('[', personsStart);
            int arrayEnd = FindMatchingBracket(json, arrayStart);
            int count = CountArrayElements(json.Substring(arrayStart, arrayEnd - arrayStart + 1));
            Console.WriteLine($"  Persons 数量: {count}");
        }
        
        int troopsStart = json.IndexOf("\"Troops\":");
        if (troopsStart > 0)
        {
            int arrayStart = json.IndexOf('[', troopsStart);
            int arrayEnd = FindMatchingBracket(json, arrayStart);
            int count = CountArrayElements(json.Substring(arrayStart, arrayEnd - arrayStart + 1));
            Console.WriteLine($"  Troops 数量: {count}");
        }
        
        Console.WriteLine("  ✅ 数组大小检测完成");
    }
    
    static int FindMatchingBracket(string json, int start)
    {
        int depth = 0;
        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '[' || json[i] == '{') depth++;
            if (json[i] == ']' || json[i] == '}') depth--;
            if (depth == 0) return i;
        }
        return -1;
    }
    
    static int CountArrayElements(string arrayJson)
    {
        // 简单计数：统计顶层逗号数量 + 1
        int count = 0;
        int depth = 0;
        bool inString = false;
        
        for (int i = 0; i < arrayJson.Length; i++)
        {
            char c = arrayJson[i];
            
            if (c == '"' && (i == 0 || arrayJson[i - 1] != '\\'))
            {
                inString = !inString;
            }
            
            if (!inString)
            {
                if (c == '[' || c == '{') depth++;
                if (c == ']' || c == '}') depth--;
                if (c == ',' && depth == 1) count++;
            }
        }
        
        // 如果数组不为空，元素数 = 逗号数 + 1
        if (arrayJson.Contains("{") || arrayJson.Contains("["))
        {
            count++;
        }
        
        return count;
    }
    
    static void TestPartialDeserialization(string scenarioPath)
    {
        Console.WriteLine("  阶段1：读取 JSON 文件...");
        string json = File.ReadAllText(scenarioPath);
        Console.WriteLine($"    ✅ 文件读取成功，长度: {json.Length}");
        
        Console.WriteLine("  阶段2：解析 JSON 文档...");
        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            Console.WriteLine("    ✅ JSON 解析成功");
            
            JsonElement root = doc.RootElement;
            
            // 检查关键字段
            if (root.TryGetProperty("Architectures", out JsonElement architectures))
            {
                Console.WriteLine($"    Architectures: {architectures.GetArrayLength()} 个");
            }
            
            if (root.TryGetProperty("Persons", out JsonElement persons))
            {
                Console.WriteLine($"    Persons: {persons.GetArrayLength()} 个");
            }
            
            if (root.TryGetProperty("Factions", out JsonElement factions))
            {
                Console.WriteLine($"    Factions: {factions.GetArrayLength()} 个");
            }
            
            if (root.TryGetProperty("Troops", out JsonElement troops))
            {
                Console.WriteLine($"    Troops: {troops.GetArrayLength()} 个");
            }
        }
        
        Console.WriteLine("  ✅ 部分反序列化测试通过");
    }
}
