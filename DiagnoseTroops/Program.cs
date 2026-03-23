using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace DiagnoseTroops
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断部队问题");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 查找最新的存档文件
            string saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games",
                "WorldOfTheThreeKingdoms",
                "Save"
            );

            if (!Directory.Exists(saveDir))
            {
                Console.WriteLine($"❌ 存档目录不存在: {saveDir}");
                Console.ReadKey();
                return;
            }

            var saveFiles = Directory.GetFiles(saveDir, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (saveFiles.Count == 0)
            {
                Console.WriteLine("❌ 没有找到存档文件");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"找到 {saveFiles.Count} 个存档文件");
            Console.WriteLine($"检查最新的存档: {Path.GetFileName(saveFiles[0])}");
            Console.WriteLine();

            try
            {
                string json = File.ReadAllText(saveFiles[0]);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 检查 Troops
                if (root.TryGetProperty("Troops", out var troops))
                {
                    int troopCount = troops.GetArrayLength();
                    Console.WriteLine($"✅ 找到 {troopCount} 个部队");
                    Console.WriteLine();

                    int troopsWithName = 0;
                    int troopsWithoutName = 0;
                    int troopsWithLeader = 0;
                    int troopsWithoutLeader = 0;

                    // 显示前10个部队的详细信息
                    int displayCount = Math.Min(10, troopCount);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var troop = troops[i];
                        
                        int id = troop.TryGetProperty("ID", out var idElem) 
                            ? idElem.GetInt32() 
                            : -1;
                        
                        string name = troop.TryGetProperty("Name", out var nameElem) 
                            ? nameElem.GetString() ?? ""
                            : "";
                        
                        int leaderID = troop.TryGetProperty("LeaderID", out var leaderElem) 
                            ? leaderElem.GetInt32() 
                            : -1;
                        
                        int quantity = troop.TryGetProperty("Quantity", out var quantityElem) 
                            ? quantityElem.GetInt32() 
                            : 0;
                        
                        int factionID = troop.TryGetProperty("BelongedFactionID", out var factionElem) 
                            ? factionElem.GetInt32() 
                            : -1;
                        
                        Console.WriteLine($"部队 #{i + 1}:");
                        Console.WriteLine($"  ID: {id}");
                        Console.WriteLine($"  Name: {(string.IsNullOrEmpty(name) ? "❌ 空" : name)}");
                        Console.WriteLine($"  LeaderID: {leaderID}");
                        Console.WriteLine($"  Quantity: {quantity}");
                        Console.WriteLine($"  FactionID: {factionID}");
                        Console.WriteLine();
                        
                        if (!string.IsNullOrEmpty(name))
                            troopsWithName++;
                        else
                            troopsWithoutName++;
                        
                        if (leaderID > 0)
                            troopsWithLeader++;
                        else
                            troopsWithoutLeader++;
                    }

                    // 统计所有部队
                    for (int i = displayCount; i < troopCount; i++)
                    {
                        var troop = troops[i];
                        
                        string name = troop.TryGetProperty("Name", out var nameElem) 
                            ? nameElem.GetString() ?? ""
                            : "";
                        
                        int leaderID = troop.TryGetProperty("LeaderID", out var leaderElem) 
                            ? leaderElem.GetInt32() 
                            : -1;
                        
                        if (!string.IsNullOrEmpty(name))
                            troopsWithName++;
                        else
                            troopsWithoutName++;
                        
                        if (leaderID > 0)
                            troopsWithLeader++;
                        else
                            troopsWithoutLeader++;
                    }

                    Console.WriteLine("========================================");
                    Console.WriteLine("统计结果：");
                    Console.WriteLine($"总部队数: {troopCount}");
                    Console.WriteLine($"有名称的部队: {troopsWithName}");
                    Console.WriteLine($"没有名称的部队: {troopsWithoutName}");
                    Console.WriteLine($"有领队的部队: {troopsWithLeader}");
                    Console.WriteLine($"没有领队的部队: {troopsWithoutLeader}");
                    Console.WriteLine();

                    if (troopsWithoutName > 0)
                    {
                        Console.WriteLine("❌ 问题：有部队没有名称！");
                        Console.WriteLine("   可能原因：");
                        Console.WriteLine("   1. 保存时 Name 字段为空");
                        Console.WriteLine("   2. Leader 为 null 且 base.Name 也为空");
                        Console.WriteLine("   3. 序列化时没有正确保存 Name");
                    }
                    
                    if (troopsWithoutLeader > 0)
                    {
                        Console.WriteLine("⚠️  有部队没有领队");
                        Console.WriteLine("   这可能导致部队名称显示为空");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 存档中没有找到 Troops 数据");
                }
                
                // 检查 Persons
                if (root.TryGetProperty("Persons", out var persons))
                {
                    Console.WriteLine();
                    Console.WriteLine($"✅ 找到 {persons.GetArrayLength()} 个人物");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 解析存档失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
