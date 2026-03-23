using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

namespace DiagnoseLoadFailure;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        string savePath = args.Length > 0 ? args[0] : @"Save\Save01.sav.gz";
        
        Console.WriteLine("=== 存档加载失败诊断工具 ===");
        Console.WriteLine($"目标文件: {savePath}");
        Console.WriteLine();
        
        // 步骤 1: 检查文件是否存在
        Console.WriteLine("【步骤 1】检查文件存在性...");
        if (!File.Exists(savePath))
        {
            Console.WriteLine($"❌ 文件不存在: {savePath}");
            Console.WriteLine($"   当前目录: {Directory.GetCurrentDirectory()}");
            return;
        }
        Console.WriteLine($"✅ 文件存在，大小: {new FileInfo(savePath).Length:N0} 字节");
        Console.WriteLine();
        
        // 步骤 2: 检查文件格式
        Console.WriteLine("【步骤 2】检查文件格式...");
        try
        {
            using FileStream fs = File.OpenRead(savePath);
            Span<byte> header = stackalloc byte[2]; // C# 12: 使用 Span 避免堆分配
            fs.Read(header);
            
            if (header[0] == 0x1F && header[1] == 0x8B)
            {
                Console.WriteLine("✅ 文件格式: GZip 压缩 (.sav.gz)");
            }
            else
            {
                Console.WriteLine($"⚠️ 文件头不是 GZip: {header[0]:X2} {header[1]:X2}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 读取文件头失败: {ex.Message}");
            return;
        }
        Console.WriteLine();
        
        // 步骤 3: 尝试解压缩
        Console.WriteLine("【步骤 3】尝试解压缩...");
        string jsonContent;
        try
        {
            using FileStream fs = File.OpenRead(savePath);
            using GZipStream gzip = new(fs, CompressionMode.Decompress);
            using StreamReader reader = new(gzip, System.Text.Encoding.UTF8);
            jsonContent = reader.ReadToEnd();
            
            Console.WriteLine($"✅ 解压成功，JSON 大小: {jsonContent.Length:N0} 字符");
            Console.WriteLine($"   前 100 字符: {jsonContent[..Math.Min(100, jsonContent.Length)]}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 解压失败:");
            Console.WriteLine($"   异常类型: {ex.GetType().FullName}");
            Console.WriteLine($"   异常消息: {ex.Message}");
            Console.WriteLine($"   堆栈跟踪:\n{ex.StackTrace}");
            return;
        }
        Console.WriteLine();
        
        // 步骤 4: 尝试反序列化为 DTO
        Console.WriteLine("【步骤 4】尝试反序列化为 GameScenarioDTO...");
        GameScenarioDTO? dto;
        try
        {
            JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
            dto = JsonSerializer.Deserialize<GameScenarioDTO>(jsonContent, options);
            
            if (dto == null)
            {
                Console.WriteLine("❌ 反序列化返回 null");
                return;
            }
            
            Console.WriteLine("✅ 反序列化成功");
            Console.WriteLine($"   Persons: {dto.Persons?.Count ?? 0}");
            Console.WriteLine($"   Architectures: {dto.Architectures?.Count ?? 0}");
            Console.WriteLine($"   Factions: {dto.Factions?.Count ?? 0}");
            Console.WriteLine($"   Troops: {dto.Troops?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 反序列化失败:");
            Console.WriteLine($"   异常类型: {ex.GetType().FullName}");
            Console.WriteLine($"   异常消息: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   内部异常: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"   内部消息: {ex.InnerException.Message}");
            }
            
            Console.WriteLine($"   堆栈跟踪:\n{ex.StackTrace}");
            return;
        }
        Console.WriteLine();
        
        // 步骤 5: 尝试使用 SerializationManager 完整加载
        Console.WriteLine("【步骤 5】使用 SerializationManager 完整加载...");
        try
        {
            SerializationManager manager = new(); // ✅ C# 12 目标类型 new
            GameScenario scenario = manager.LoadGame(savePath);
            
            Console.WriteLine("✅ SerializationManager 加载成功");
            Console.WriteLine($"   Persons: {scenario.Persons.Count}");
            Console.WriteLine($"   Architectures: {scenario.Architectures.Count}");
            Console.WriteLine($"   Factions: {scenario.Factions.Count}");
            Console.WriteLine($"   Troops: {scenario.Troops.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SerializationManager 加载失败:");
            Console.WriteLine($"   异常类型: {ex.GetType().FullName}");
            Console.WriteLine($"   异常消息: {ex.Message}");
            
            // 递归输出所有内部异常
            var innerEx = ex.InnerException;
            int depth = 1;
            while (innerEx != null)
            {
                Console.WriteLine($"   内部异常 [{depth}]:");
                Console.WriteLine($"     类型: {innerEx.GetType().FullName}");
                Console.WriteLine($"     消息: {innerEx.Message}");
                innerEx = innerEx.InnerException;
                depth++;
            }
            
            Console.WriteLine($"   完整堆栈跟踪:\n{ex}");
        }
        
        Console.WriteLine();
        Console.WriteLine("=== 诊断完成 ===");
    }
}
