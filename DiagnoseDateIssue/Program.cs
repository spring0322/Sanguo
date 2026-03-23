using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;
using WorldOfTheThreeKingdoms.Serialization;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 测试 1：直接读取 JSON 文件
Console.WriteLine("=== 测试 1：读取剧本 JSON 文件 ===");
string jsonPath = "Content/Data/Scenario/184DHZS.json";
if (File.Exists(jsonPath))
{
    string json = File.ReadAllText(jsonPath);
    
    // 查找 Year 字段
    int yearIndex = json.IndexOf("\"Year\":");
    if (yearIndex >= 0)
    {
        string yearSection = json.Substring(yearIndex, Math.Min(100, json.Length - yearIndex));
        Console.WriteLine($"JSON 中的 Year 字段: {yearSection}");
    }
    
    // 反序列化
    var options = GameJsonContext.GetDefaultOptions(indented: false);
    var dto = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
    
    if (dto != null)
    {
        Console.WriteLine($"✅ 反序列化成功");
        Console.WriteLine($"   DTO.Year = {dto.Year}");
        Console.WriteLine($"   DTO.Month = {dto.Month}");
        Console.WriteLine($"   DTO.Day = {dto.Day}");
    }
    else
    {
        Console.WriteLine($"❌ 反序列化返回 null");
    }
}
else
{
    Console.WriteLine($"❌ 文件不存在: {jsonPath}");
}

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();
