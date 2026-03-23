using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace DiagnoseTroopArmy;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 诊断 Troop.Army 空引用问题 ===\n");
        
        // 直接使用指定的存档路径
        string saveDir = @"G:\sanguo\net8\sanguo260211-2\WorldOfTheThreeKingdoms\bin\Debug\net8.0\Save";
        
        if (!Directory.Exists(saveDir))
        {
            Console.WriteLine($"❌ 存档目录不存在: {saveDir}");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"✅ 找到存档目录: {saveDir}\n");
        
        // 查找最新的存档文件
        var saveFiles = Directory.GetFiles(saveDir, "*.sav.gz");
        if (saveFiles.Length == 0)
        {
            Console.WriteLine($"❌ 未找到存档文件 (*.sav.gz) 在: {saveDir}");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
            return;
        }
        
        Array.Sort(saveFiles);
        string latestSave = saveFiles[^1];
        
        AnalyzeSaveFile(latestSave);
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
    
    static void AnalyzeSaveFile(string saveFilePath)
    {
        Console.WriteLine($"📁 分析存档: {Path.GetFileName(saveFilePath)}\n");
        
        try
        {
            // 解压并读取 JSON
            using var fileStream = File.OpenRead(saveFilePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            string json = reader.ReadToEnd();
            
            // 反序列化 - 使用 JsonDocument 手动解析
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            
            // 输出 JSON 结构信息
            Console.WriteLine("=== JSON 结构分析 ===");
            Console.WriteLine($"根元素类型: {root.ValueKind}");
            if (root.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("根元素属性:");
                foreach (JsonProperty prop in root.EnumerateObject())
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"    数组长度: {prop.Value.GetArrayLength()}");
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object && (prop.Name == "Troops" || prop.Name == "Militaries"))
                    {
                        // 输出对象的前几个属性
                        Console.WriteLine($"    对象属性:");
                        int count = 0;
                        foreach (JsonProperty subProp in prop.Value.EnumerateObject())
                        {
                            Console.WriteLine($"      - {subProp.Name}: {subProp.Value.ValueKind}");
                            if (++count >= 3) break;
                        }
                    }
                }
            }
            Console.WriteLine();
            
            JsonSerializerOptions jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            
            // 提取 Troops - 处理 JSON.NET 格式 ($values)
            List<TroopDTO>? troops = null;
            if (root.TryGetProperty("Troops", out JsonElement troopsElement))
            {
                try
                {
                    if (troopsElement.TryGetProperty("$values", out JsonElement troopsArray))
                    {
                        troops = JsonSerializer.Deserialize<List<TroopDTO>>(troopsArray.GetRawText(), jsonOptions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 无法解析 Troops: {ex.Message}");
                }
            }
            
            // 提取 Militaries - 处理 JSON.NET 格式 ($values)
            List<MilitaryDTO>? militaries = null;
            if (root.TryGetProperty("Militaries", out JsonElement militariesElement))
            {
                try
                {
                    if (militariesElement.TryGetProperty("$values", out JsonElement militariesArray))
                    {
                        militaries = JsonSerializer.Deserialize<List<MilitaryDTO>>(militariesArray.GetRawText(), jsonOptions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 无法解析 Militaries: {ex.Message}");
                }
            }
            
            Console.WriteLine($"✅ 存档加载成功");
            Console.WriteLine($"   - Troops: {troops?.Count ?? 0}");
            Console.WriteLine($"   - Militaries: {militaries?.Count ?? 0}\n");
            
            // 即使 Troops 解析失败，也分析 Militaries
            if (militaries != null && militaries.Count > 0)
            {
                Console.WriteLine("=== Military 数据分析 ===\n");
                foreach (var military in militaries)
                {
                    Console.WriteLine($"Military {military.ID} ({military.Name}):");
                    Console.WriteLine($"  - KindID: {military.KindID}");
                    Console.WriteLine($"  - BelongedArchitectureID: {military.BelongedArchitectureID}");
                    Console.WriteLine($"  - Quantity: {military.Quantity}");
                    Console.WriteLine($"  - ShelledMilitaryID: {military.ShelledMilitaryID}");
                    Console.WriteLine();
                }
            }
            
            // 分析 Troop 数据
            if (troops == null || troops.Count == 0)
            {
                Console.WriteLine("⚠️ 存档中没有 Troop 数据（可能是旧格式存档）");
                Console.WriteLine("\n建议：请创建一个新存档后再次运行诊断。");
                return;
            }
            
            Console.WriteLine("=== Troop.MilitaryID 分析 ===\n");
            
            int validCount = 0;
            int zeroCount = 0;
            int negativeCount = 0;
            
            foreach (var troop in troops)
            {
                if (troop.MilitaryID > 0)
                {
                    validCount++;
                    
                    // 检查引用的 Military 是否存在
                    bool exists = militaries?.Exists(m => m.ID == troop.MilitaryID) ?? false;
                    if (!exists)
                    {
                        Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID={troop.MilitaryID} 不存在！");
                    }
                }
                else if (troop.MilitaryID == 0)
                {
                    zeroCount++;
                    Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID=0");
                }
                else
                {
                    negativeCount++;
                    Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID={troop.MilitaryID}");
                }
            }
            
            int invalidCount = zeroCount + negativeCount;
            
            Console.WriteLine($"\n=== 统计结果 ===");
            Console.WriteLine($"有效 MilitaryID (> 0): {validCount}");
            Console.WriteLine($"无效 MilitaryID (= 0): {zeroCount}");
            Console.WriteLine($"无效 MilitaryID (< 0): {negativeCount}");
            Console.WriteLine($"总计无效: {invalidCount} / {troops.Count}");
            
            if (invalidCount > 0)
            {
                Console.WriteLine($"\n❌ 发现 {invalidCount} 个 Troop 的 MilitaryID 无效！");
                Console.WriteLine("这会导致 Troop.Army 为 null，引发空引用异常。");
            }
            else
            {
                Console.WriteLine("\n✅ 所有 Troop 的 MilitaryID 都有效");
            }
            
            // 列出所有 Military IDs
            if (militaries != null && militaries.Count > 0)
            {
                Console.WriteLine($"\n=== 可用的 Military IDs ===");
                var militaryIds = militaries.Select(m => m.ID).OrderBy(id => id).ToList();
                Console.WriteLine(string.Join(", ", militaryIds.Take(20)));
                if (militaryIds.Count > 20)
                {
                    Console.WriteLine($"... (共 {militaryIds.Count} 个)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
