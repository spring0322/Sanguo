using System.Text.Json;
using System.Text.Json.Nodes;

namespace ScenarioFormatConverter;

/// <summary>
/// 剧本格式转换器 v4.0
/// 将旧格式（Architecture-Centric）转换为新格式（Person-Centric）
/// 
/// 日期：2026-03-17
/// 更新：添加 Architecture.BelongedSectionID 和 Section.BelongedFactionID 字段
/// 目的：修复军团、地域信息无法显示的问题
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  剧本格式转换器 v4.0");
        Console.WriteLine("  添加引用字段（Person/Architecture/Section）");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("用法：");
            Console.WriteLine("  ScenarioFormatConverter <剧本文件路径>");
            Console.WriteLine("  ScenarioFormatConverter <剧本目录路径> --batch");
            Console.WriteLine();
            Console.WriteLine("示例：");
            Console.WriteLine("  ScenarioFormatConverter Content/Data/Scenario/184DHZS.json");
            Console.WriteLine("  ScenarioFormatConverter Content/Data/Scenario --batch");
            return;
        }

        string path = args[0];
        bool batchMode = args.Length > 1 && args[1] == "--batch";

        if (batchMode)
        {
            BatchConvert(path);
        }
        else
        {
            ConvertSingleFile(path);
        }
    }

    static void BatchConvert(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"❌ 错误：目录不存在 - {directoryPath}");
            return;
        }

        var jsonFiles = Directory.GetFiles(directoryPath, "*.json")
            .Where(f => !Path.GetFileName(f).StartsWith("backup_"))
            .ToArray();
            
        Console.WriteLine($"找到 {jsonFiles.Length} 个剧本文件");
        Console.WriteLine();

        int successCount = 0;
        int skipCount = 0;
        int errorCount = 0;

        foreach (var file in jsonFiles)
        {
            Console.WriteLine($"处理: {Path.GetFileName(file)}");
            try
            {
                var result = ConvertScenario(file);
                if (result.IsNewFormat)
                {
                    Console.WriteLine($"  ⏭️  跳过（已是新格式）");
                    skipCount++;
                }
                else if (result.Success)
                {
                    Console.WriteLine($"  ✅ 转换成功");
                    successCount++;
                }
                else
                {
                    Console.WriteLine($"  ❌ 转换失败: {result.ErrorMessage}");
                    errorCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 异常: {ex.Message}");
                errorCount++;
            }
            Console.WriteLine();
        }

        Console.WriteLine("===========================================");
        Console.WriteLine($"批量转换完成:");
        Console.WriteLine($"  成功: {successCount}");
        Console.WriteLine($"  跳过: {skipCount}");
        Console.WriteLine($"  失败: {errorCount}");
        Console.WriteLine("===========================================");
    }

    static void ConvertSingleFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 错误：文件不存在 - {filePath}");
            return;
        }

        Console.WriteLine($"转换文件: {filePath}");
        Console.WriteLine();

        var result = ConvertScenario(filePath);

        if (result.IsNewFormat)
        {
            Console.WriteLine("⏭️  此剧本已是新格式，无需转换");
        }
        else if (result.Success)
        {
            Console.WriteLine("✅ 转换成功！");
            Console.WriteLine();
            Console.WriteLine("转换统计:");
            Console.WriteLine($"  Person.LocationArchitectureID: {result.Stats.PersonLocationSet}");
            Console.WriteLine($"  Person.BelongedFactionID: {result.Stats.PersonFactionSet}");
            Console.WriteLine($"  Architecture.BelongedFactionID: {result.Stats.ArchitectureFactionSet}");
            Console.WriteLine($"  Architecture.BelongedSectionID: {result.Stats.ArchitectureSectionSet}");
            Console.WriteLine($"  Section.BelongedFactionID: {result.Stats.SectionFactionSet}");
            Console.WriteLine($"  State.LinkedRegionID: {result.Stats.StateLinkedRegionSet}");
            Console.WriteLine($"  Biographies 转换: {result.Stats.BiographiesConverted}");
            Console.WriteLine($"  宝物有持有人: {result.Stats.TreasuresWithOwner}");
            Console.WriteLine($"  宝物无持有人: {result.Stats.TreasuresWithoutOwner}");
            Console.WriteLine($"  Person.TreasureIDs 设置: {result.Stats.PersonTreasureIDsSet}");
            Console.WriteLine($"  Person.SkillIDs 设置: {result.Stats.PersonSkillIDsSet}");
            Console.WriteLine($"  Person.StuntIDs 设置: {result.Stats.PersonStuntIDsSet}");
            Console.WriteLine($"  Person.TitleIDs 设置: {result.Stats.PersonTitleIDsSet}");
        }
        else
        {
            Console.WriteLine($"❌ 转换失败: {result.ErrorMessage}");
        }
    }

    static ConversionResult ConvertScenario(string filePath)
    {
        var result = new ConversionResult();

        try
        {
            // 1. 读取 JSON 文件
            string jsonText = File.ReadAllText(filePath);
            var jsonDoc = JsonNode.Parse(jsonText);
            if (jsonDoc == null)
            {
                result.ErrorMessage = "无法解析 JSON 文件";
                return result;
            }

            // 剧本文件的根节点必须是 JsonObject
            if (jsonDoc is not JsonObject jsonObj)
            {
                result.ErrorMessage = "非剧本格式（根节点不是对象）";
                result.IsNewFormat = true; // Skip error counting for Scenarios.json, etc.
                return result;
            }

            // 2. 检查是否需要转换
            bool needsPersonConversion = !HasPersonFields(jsonDoc);
            bool needsBiographyConversion = NeedsBiographyConversion(jsonDoc);
            bool needsSectionConversion = !HasSectionFields(jsonDoc);
            bool needsTreasureConversion = NeedsTreasureConversion(jsonDoc);  // 🔥 新增
            bool needsPersonCollectionConversion = NeedsPersonCollectionConversion(jsonDoc);  // 🔥 新增

            if (!needsPersonConversion && !needsBiographyConversion && !needsSectionConversion && !needsTreasureConversion && !needsPersonCollectionConversion)
            {
                result.IsNewFormat = true;
                return result;
            }

            // 3. 创建备份
            string backupPath = Path.Combine(
                Path.GetDirectoryName(filePath)!,
                "backup_" + Path.GetFileName(filePath) + "." + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            File.Copy(filePath, backupPath);
            Console.WriteLine($"  📦 备份: {Path.GetFileName(backupPath)}");

            // 4. 执行转换
            var converter = new ScenarioConverter(jsonDoc);
            converter.Convert();
            result.Stats = converter.Stats;

            // 5. 保存转换后的文件（保持格式化）
            // 使用 JsonWriterOptions 而非 JsonSerializerOptions，避免 AOT TypeInfoResolver 限制
            var writerOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            using var stream = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream, writerOptions))
            {
                jsonDoc.WriteTo(writer);
            }
            string convertedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(filePath, convertedJson);

            result.Success = true;
            Console.WriteLine($"  💾 已保存");
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.ToString();
        }

        return result;
    }

    static bool HasPersonFields(JsonNode jsonDoc)
    {
        // 🔥 关键修复:兼容两种格式
        // 新格式: {"Persons": {"GameObjects": [...]}}
        // 旧格式: {"Persons": [...]}
        // 日期: 2026-03-20
        // 原因: 旧剧本Persons直接是数组,导致检测失败
        
        // 🔥 ANTI-BAND-AID: 不使用 ?. 掩盖数据错误
        // 如果数据格式不符合预期,返回false让调用者处理
        JsonArray? personsArray = jsonDoc["Persons"] switch
        {
            JsonObject jsonObj => jsonObj["GameObjects"] as JsonArray,
            JsonArray jsonArr => jsonArr,
            _ => null
        };
        
        // 数据不存在或为空,返回false(不需要转换或数据损坏)
        if (personsArray == null || personsArray.Count == 0)
            return false;
            
        var firstPerson = personsArray[0];
        if (firstPerson == null)
            return false;  // 数据损坏,返回false
            
        return firstPerson["LocationArchitectureID"] != null;
    }

    static bool HasSectionFields(JsonNode jsonDoc)
    {
        // 🔥 关键修复:兼容两种格式
        // 日期: 2026-03-20
        JsonArray? archArray = jsonDoc["Architectures"] switch
        {
            JsonObject jsonObj => jsonObj["GameObjects"] as JsonArray,
            JsonArray jsonArr => jsonArr,
            _ => null
        };
        
        if (archArray == null || archArray.Count == 0)
            return false;
            
        var firstArch = archArray[0];
        if (firstArch == null)
            return false;
            
        return firstArch["BelongedSectionID"] != null;
    }

    static bool NeedsBiographyConversion(JsonNode jsonDoc)
    {
        // 检查是否有旧格式的 AllBiographies 但没有新格式的 Biographies
        return jsonDoc["AllBiographies"] != null && jsonDoc["Biographies"] == null;
    }

    static bool NeedsTreasureConversion(JsonNode jsonDoc)
    {
        // 🔥 关键修复:兼容两种格式
        // 日期: 2026-03-20
        JsonArray? personsArray = jsonDoc["Persons"] switch
        {
            JsonObject jsonObj => jsonObj["GameObjects"] as JsonArray,
            JsonArray jsonArr => jsonArr,
            _ => null
        };
        
        if (personsArray == null || personsArray.Count == 0)
            return false;
            
        var firstPerson = personsArray[0];
        if (firstPerson == null)
            return false;
            
        return firstPerson["TreasureIDs"] == null;
    }

    static bool NeedsPersonCollectionConversion(JsonNode jsonDoc)
    {
        // 🔥 关键修复:兼容两种格式
        // 日期: 2026-03-20
        JsonArray? personsArray = jsonDoc["Persons"] switch
        {
            JsonObject jsonObj => jsonObj["GameObjects"] as JsonArray,
            JsonArray jsonArr => jsonArr,
            _ => null
        };
        
        if (personsArray == null || personsArray.Count == 0)
            return false;
            
        var firstPerson = personsArray[0];
        if (firstPerson == null)
            return false;
            
        return firstPerson["SkillIDs"] == null;
    }}

class ConversionResult
{
    public bool Success { get; set; }
    public bool IsNewFormat { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ConversionStats Stats { get; set; } = new();
}

class ConversionStats
{
    public int PersonLocationSet { get; set; }
    public int PersonFactionSet { get; set; }
    public int ArchitectureFactionSet { get; set; }
    public int BiographiesConverted { get; set; }
    public int ArchitectureSectionSet { get; set; }
    public int SectionFactionSet { get; set; }
    public int StateLinkedRegionSet { get; set; }
    
    // 🔥 2026-03-18 新增：宝物归属转换统计
    public int TreasuresWithOwner { get; set; }
    public int TreasuresWithoutOwner { get; set; }
    public int PersonTreasureIDsSet { get; set; }
    
    // 🔥 2026-03-18 新增：Person 集合字段转换统计
    public int PersonSkillIDsSet { get; set; }
    public int PersonStuntIDsSet { get; set; }
    public int PersonTitleIDsSet { get; set; }
}
