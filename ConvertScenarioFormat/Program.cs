using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

// 🔥 C# 12 顶级语句：移除 class Program 包装
Console.WriteLine("=== 剧本格式转换工具 ===");
Console.WriteLine("将旧格式（GameObjectList 包装）转换为新格式（直接数组）\n");

string scenarioDir = @"Content\Data\Scenario";

if (!Directory.Exists(scenarioDir))
{
    Console.WriteLine($"❌ 剧本目录不存在: {scenarioDir}");
    return;
}

string[] jsonFiles = Directory.GetFiles(scenarioDir, "*.json");

Console.WriteLine($"找到 {jsonFiles.Length} 个剧本文件\n");

int convertedCount = 0;
int skippedCount = 0;

foreach (string filePath in jsonFiles)
{
    string fileName = Path.GetFileName(filePath);
    Console.WriteLine($"处理: {fileName}");
    
    try
    {
        string json = File.ReadAllText(filePath);
        
        // 解析为 JsonNode
        JsonNode? root = JsonNode.Parse(json);
        
        if (root == null)
        {
            Console.WriteLine($"  ⚠️ 跳过：无法解析 JSON\n");
            skippedCount++;
            continue;
        }
        
        bool modified = false;
        
        // 需要转换的集合字段
        string[] collections = 
        [
            "Architectures", "Persons", "Factions", "Troops", 
            "Legions", "Treasures", "Sections", "Regions", 
            "States", "Routeways", "Militaries", "Facilities", 
            "Informations", "TroopEvents", "Captives", "AllEvents"
        ];
        
        foreach (string collectionName in collections)
        {
            if (root[collectionName] is JsonObject obj)
            {
                // 旧格式：{ "GameObjects": [...] }
                if (obj["GameObjects"] is JsonArray gameObjects)
                {
                    // 替换为直接数组
                    root[collectionName] = JsonNode.Parse(gameObjects.ToJsonString());
                    modified = true;
                    Console.WriteLine($"  ✅ 转换: {collectionName}");
                }
            }
        }
        
        if (modified)
        {
            // 备份原文件
            string backupPath = filePath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath);
                Console.WriteLine($"  💾 备份: {fileName}.backup");
            }
            
            // 写入新格式
            JsonSerializerOptions options = new()
            {
                WriteIndented = false,  // 保持紧凑格式
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string newJson = root.ToJsonString(options);
            File.WriteAllText(filePath, newJson);
            
            Console.WriteLine($"  ✅ 已转换为新格式\n");
            convertedCount++;
        }
        else
        {
            Console.WriteLine($"  ℹ️ 已是新格式，跳过\n");
            skippedCount++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ 错误: {ex.Message}\n");
        skippedCount++;
    }
}

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine($"转换完成:");
Console.WriteLine($"  - 已转换: {convertedCount} 个文件");
Console.WriteLine($"  - 已跳过: {skippedCount} 个文件");
Console.WriteLine($"  - 总计: {jsonFiles.Length} 个文件");
Console.WriteLine("\n备份文件保存为 *.json.backup");
Console.WriteLine("如需恢复，删除 .json 文件并重命名 .backup 文件");
            
Console.WriteLine("\n按任意键退出...");
Console.ReadKey();
