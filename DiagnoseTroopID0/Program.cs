using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace DiagnoseTroopID0
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 诊断 Troop ID=0 问题 ===\n");
            
            // 尝试多个可能的存档位置
            string[] possiblePaths = 
            [
                @"WorldOfTheThreeKingdoms\bin\Debug\net8.0\Save\Save1.json",
                @"WorldOfTheThreeKingdoms\bin\Release\net8.0\Save\Save1.json",
                @"修改记录\temp_save.json",
                @"Content\Save\Save1.json",
                @"Save\Save1.json"
            ];
            
            string savePath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    savePath = path;
                    break;
                }
            }
            
            if (savePath == null)
            {
                Console.WriteLine($"❌ 找不到存档文件，尝试了以下位置:");
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"   - {path}");
                }
                
                // 尝试搜索 Save 目录
                Console.WriteLine("\n🔍 搜索 Save 目录...");
                var saveDir = Directory.GetDirectories(".", "Save", SearchOption.AllDirectories).FirstOrDefault();
                if (saveDir != null)
                {
                    Console.WriteLine($"   找到 Save 目录: {saveDir}");
                    var jsonFiles = Directory.GetFiles(saveDir, "*.json");
                    if (jsonFiles.Length > 0)
                    {
                        Console.WriteLine($"   包含 {jsonFiles.Length} 个 JSON 文件:");
                        foreach (var file in jsonFiles.Take(5))
                        {
                            Console.WriteLine($"     - {Path.GetFileName(file)}");
                        }
                    }
                }
                return;
            }
            
            try
            {
                Console.WriteLine($"📂 读取存档: {savePath}");
                string json = File.ReadAllText(savePath);
                
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var saveData = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
                
                if (saveData == null)
                {
                    Console.WriteLine("❌ 反序列化失败");
                    return;
                }
                
                Console.WriteLine($"\n✅ 存档加载成功");
                Console.WriteLine($"   - Troops 总数: {saveData.Troops?.Count ?? 0}");
                Console.WriteLine($"   - Militaries 总数: {saveData.Militaries?.Count ?? 0}");
                
                // 检查 ID=0 的 Troop
                Console.WriteLine($"\n🔍 检查 ID=0 的 Troop:");
                
                if (saveData.Troops != null)
                {
                    int id0Count = 0;
                    int invalidMilitaryIDCount = 0;
                    
                    foreach (var troop in saveData.Troops)
                    {
                        if (troop.ID == 0)
                        {
                            id0Count++;
                            Console.WriteLine($"\n⚠️ 发现 ID=0 的 Troop:");
                            Console.WriteLine($"   - Name: {troop.Name}");
                            Console.WriteLine($"   - MilitaryID: {troop.MilitaryID}");
                            Console.WriteLine($"   - LeaderID: {troop.LeaderID}");
                            Console.WriteLine($"   - BelongedFactionID: {troop.BelongedFactionID}");
                            Console.WriteLine($"   - Position: ({troop.PositionX}, {troop.PositionY})");
                        }
                        
                        if (troop.MilitaryID < 0)
                        {
                            invalidMilitaryIDCount++;
                        }
                    }
                    
                    Console.WriteLine($"\n📊 统计:");
                    Console.WriteLine($"   - ID=0 的 Troop: {id0Count}");
                    Console.WriteLine($"   - MilitaryID < 0 的 Troop: {invalidMilitaryIDCount}");
                }
                
                // 检查 Militaries
                Console.WriteLine($"\n🔍 检查 Militaries:");
                if (saveData.Militaries != null)
                {
                    int id0Count = 0;
                    int invalidKindIDCount = 0;
                    
                    foreach (var military in saveData.Militaries)
                    {
                        if (military.ID == 0)
                        {
                            id0Count++;
                            Console.WriteLine($"\n✅ Military ID=0:");
                            Console.WriteLine($"   - Name: {military.Name}");
                            Console.WriteLine($"   - KindID: {military.KindID}");
                            Console.WriteLine($"   - Quantity: {military.Quantity}");
                            Console.WriteLine($"   - Morale: {military.Morale}");
                        }
                        
                        if (military.KindID < 0)
                        {
                            invalidKindIDCount++;
                        }
                    }
                    
                    Console.WriteLine($"\n📊 统计:");
                    Console.WriteLine($"   - ID=0 的 Military: {id0Count}");
                    Console.WriteLine($"   - KindID < 0 的 Military: {invalidKindIDCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"   堆栈: {ex.StackTrace}");
            }
            
            Console.WriteLine($"\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
