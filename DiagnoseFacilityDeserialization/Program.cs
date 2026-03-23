using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiagnoseFacilityDeserialization
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 设施反序列化诊断工具 ===\n");
            
            string scenarioPath = @"Content\Data\Scenario\40WJQXGJ7.json";
            
            if (!File.Exists(scenarioPath))
            {
                Console.WriteLine($"❌ 剧本文件不存在: {scenarioPath}");
                return;
            }
            
            Console.WriteLine($"📂 读取剧本文件: {scenarioPath}\n");
            
            try
            {
                string json = File.ReadAllText(scenarioPath);
                
                // 测试 1：使用 JsonDocument 直接读取
                Console.WriteLine("【测试 1】使用 JsonDocument 直接读取 JSON");
                Console.WriteLine("=".PadRight(60, '='));
                
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    
                    if (root.TryGetProperty("Architectures", out JsonElement architectures))
                    {
                        Console.WriteLine($"✅ 找到 Architectures 对象\n");
                        
                        int count = 0;
                        foreach (JsonProperty archProp in architectures.EnumerateObject())
                        {
                            JsonElement arch = archProp.Value;
                            
                            if (arch.TryGetProperty("FacilitiesString", out JsonElement facilitiesString))
                            {
                                string? facStr = facilitiesString.GetString();
                                if (!string.IsNullOrEmpty(facStr) && facStr.Trim() != "")
                                {
                                    string? name = arch.TryGetProperty("Name", out JsonElement nameElem) 
                                        ? nameElem.GetString() 
                                        : "未知";
                                    int id = arch.TryGetProperty("ID", out JsonElement idElem) 
                                        ? idElem.GetInt32() 
                                        : -1;
                                    
                                    Console.WriteLine($"  建筑: {name} (ID:{id})");
                                    Console.WriteLine($"    FacilitiesString: '{facStr}'");
                                    
                                    count++;
                                    if (count >= 5) break;
                                }
                            }
                        }
                        
                        Console.WriteLine($"\n✅ 找到 {count} 个有设施的建筑（仅显示前5个）\n");
                    }
                }
                
                // 测试 2：使用 System.Text.Json 反序列化（AOT 模式）
                Console.WriteLine("【测试 2】使用 System.Text.Json 反序列化（模拟 AOT）");
                Console.WriteLine("=".PadRight(60, '='));
                
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = false,
                    IncludeFields = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };
                
                var scenario = JsonSerializer.Deserialize<ScenarioDTO>(json, options);
                
                if (scenario?.Architectures != null)
                {
                    Console.WriteLine($"✅ 反序列化成功，共 {scenario.Architectures.Count} 个建筑\n");
                    
                    int withFacilityString = 0;
                    int withFacilityIDs = 0;
                    
                    foreach (var arch in scenario.Architectures.Values)
                    {
                        if (!string.IsNullOrEmpty(arch.FacilitiesString) && arch.FacilitiesString.Trim() != "")
                        {
                            withFacilityString++;
                            
                            if (withFacilityString <= 5)
                            {
                                Console.WriteLine($"  建筑: {arch.Name} (ID:{arch.ID})");
                                Console.WriteLine($"    FacilitiesString: '{arch.FacilitiesString}'");
                                Console.WriteLine($"    FacilityIDs: {(arch.FacilityIDs != null ? $"[{string.Join(", ", arch.FacilityIDs)}]" : "null")}");
                            }
                        }
                        
                        if (arch.FacilityIDs != null && arch.FacilityIDs.Count > 0)
                        {
                            withFacilityIDs++;
                        }
                    }
                    
                    Console.WriteLine($"\n统计结果:");
                    Console.WriteLine($"  - 有 FacilitiesString 的建筑: {withFacilityString}");
                    Console.WriteLine($"  - 有 FacilityIDs 的建筑: {withFacilityIDs}");
                    
                    if (withFacilityString > 0 && withFacilityIDs == 0)
                    {
                        Console.WriteLine($"\n⚠️ 警告：剧本使用旧格式（FacilitiesString），但反序列化后 FacilityIDs 为空");
                        Console.WriteLine($"   这说明 LoadArchitectureFromDTO 中的解析逻辑可能没有执行");
                    }
                    else if (withFacilityString > 0 && withFacilityIDs > 0)
                    {
                        Console.WriteLine($"\n✅ 正常：FacilitiesString 已被解析为 FacilityIDs");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 反序列化失败或 Architectures 为空");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"堆栈: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
    
    // DTO 类定义
    public class ScenarioDTO
    {
        public Dictionary<string, ArchitectureDTO>? Architectures { get; set; }
    }
    
    public class ArchitectureDTO
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        
        [JsonInclude]
        public string? FacilitiesString { get; set; }
        
        public List<int>? FacilityIDs { get; set; } = [];
    }
}
