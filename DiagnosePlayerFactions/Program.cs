using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace DiagnosePlayerFactions
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PlayerFactions 诊断工具 ===\n");
            
            // 查找存档文件
            string saveDir = Path.Combine(Environment.CurrentDirectory, "GameData", "Save");
            if (!Directory.Exists(saveDir))
            {
                Console.WriteLine($"存档目录不存在: {saveDir}");
                return;
            }
            
            var saveFiles = Directory.GetFiles(saveDir, "*.json");
            if (saveFiles.Length == 0)
            {
                Console.WriteLine("未找到存档文件");
                return;
            }
            
            Console.WriteLine($"找到 {saveFiles.Length} 个存档文件:\n");
            
            foreach (var saveFile in saveFiles)
            {
                Console.WriteLine($"检查存档: {Path.GetFileName(saveFile)}");
                Console.WriteLine(new string('-', 60));
                
                try
                {
                    string json = File.ReadAllText(saveFile);
                    var dto = JsonSerializer.Deserialize<GameScenarioDTO>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (dto == null)
                    {
                        Console.WriteLine("  ❌ 无法反序列化存档");
                        continue;
                    }
                    
                    Console.WriteLine($"  PlayerList: {(dto.PlayerList == null ? "null" : $"Count={dto.PlayerList.Count}")}");
                    if (dto.PlayerList != null && dto.PlayerList.Count > 0)
                    {
                        Console.WriteLine($"  玩家势力ID: {string.Join(", ", dto.PlayerList)}");
                    }
                    else
                    {
                        Console.WriteLine("  ⚠️ PlayerList 为空或null!");
                    }
                    
                    Console.WriteLine($"  CurrentPlayerID: {dto.CurrentPlayerID ?? "null"}");
                    Console.WriteLine($"  PlayerInfo: {dto.PlayerInfo ?? "null"}");
                    Console.WriteLine($"  势力总数: {dto.Factions?.Count ?? 0}");
                    
                    if (dto.Factions != null && dto.Factions.Count > 0)
                    {
                        Console.WriteLine("\n  前5个势力:");
                        int count = 0;
                        foreach (var faction in dto.Factions)
                        {
                            if (count >= 5) break;
                            Console.WriteLine($"    ID={faction.ID}, Name={faction.Name}");
                            count++;
                        }
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ 错误: {ex.Message}");
                }
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
