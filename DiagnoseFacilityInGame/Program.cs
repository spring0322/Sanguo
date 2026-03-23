using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace DiagnoseFacilityInGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断存档中的设施增益问题");
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

                // 检查建筑
                if (!root.TryGetProperty("Architectures", out var architectures))
                {
                    Console.WriteLine("❌ 找不到Architectures数据");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"✅ 找到 {architectures.GetArrayLength()} 个建筑");
                Console.WriteLine();

                // 统计信息
                int totalArchs = 0;
                int archsWithFacilities = 0;
                int archsWithPalace = 0;
                int archsWithDominationBonus = 0;
                var palaceArchs = new List<(string name, int id, int facilityCount, int dominationBonus, int dominationCeiling)>();

                foreach (var arch in architectures.EnumerateArray())
                {
                    totalArchs++;
                    
                    string name = arch.TryGetProperty("Name", out var nameElem) 
                        ? nameElem.GetString() ?? "未知"
                        : "未知";
                    int id = arch.TryGetProperty("ID", out var idElem) 
                        ? idElem.GetInt32() 
                        : -1;
                    
                    // 检查设施
                    var facilityIds = new List<int>();
                    if (arch.TryGetProperty("FacilityIDs", out var facilityIDsElem))
                    {
                        foreach (var fid in facilityIDsElem.EnumerateArray())
                        {
                            facilityIds.Add(fid.GetInt32());
                        }
                    }
                    
                    if (facilityIds.Count > 0)
                    {
                        archsWithFacilities++;
                    }
                    
                    // 检查是否有皇宫（设施Kind ID 200）
                    // 注意：FacilityIDs存储的是Facility实例的ID，不是FacilityKind的ID
                    // 我们需要检查Facilities数组来找到Kind ID
                    
                    // 检查统治上限增益
                    int dominationBonus = 0;
                    if (arch.TryGetProperty("IncrementOfDominationCeiling", out var incCeiling))
                    {
                        dominationBonus = incCeiling.GetInt32();
                        if (dominationBonus > 0)
                        {
                            archsWithDominationBonus++;
                        }
                    }
                    
                    // 如果有设施，记录下来以便详细检查
                    if (facilityIds.Count > 0)
                    {
                        int dominationCeiling = arch.TryGetProperty("DominationCeiling", out var domCeil) 
                            ? domCeil.GetInt32() 
                            : 0;
                        
                        palaceArchs.Add((name, id, facilityIds.Count, dominationBonus, dominationCeiling));
                    }
                }

                Console.WriteLine("=== 统计信息 ===");
                Console.WriteLine($"总建筑数: {totalArchs}");
                Console.WriteLine($"有设施的建筑: {archsWithFacilities}");
                Console.WriteLine($"有统治上限增益的建筑: {archsWithDominationBonus}");
                Console.WriteLine();

                if (archsWithDominationBonus == 0)
                {
                    Console.WriteLine("❌ 问题确认：没有任何建筑有统治上限增益！");
                    Console.WriteLine();
                    Console.WriteLine("可能的原因：");
                    Console.WriteLine("  1. 读档后没有调用 ApplyInfluences()");
                    Console.WriteLine("  2. ApplyInfluences() 被调用了，但 IncrementOfDominationCeiling 没有被保存");
                    Console.WriteLine("  3. 设施的影响没有被正确应用");
                    Console.WriteLine();
                }

                // 显示有设施的建筑详情
                Console.WriteLine("=== 有设施的建筑详情（前10个）===");
                foreach (var (name, id, facilityCount, dominationBonus, dominationCeiling) in palaceArchs.Take(10))
                {
                    Console.WriteLine($"建筑: {name} (ID: {id})");
                    Console.WriteLine($"  设施数量: {facilityCount}");
                    Console.WriteLine($"  统治上限增益: {dominationBonus}");
                    Console.WriteLine($"  统治上限: {dominationCeiling}");
                    
                    if (facilityCount > 0 && dominationBonus == 0)
                    {
                        Console.WriteLine($"  ⚠️ 有设施但没有统治上限增益");
                    }
                    Console.WriteLine();
                }

                // 检查Facilities数组
                Console.WriteLine("=== 检查Facilities数组 ===");
                if (root.TryGetProperty("Facilities", out var facilities))
                {
                    Console.WriteLine($"✅ 找到 {facilities.GetArrayLength()} 个设施实例");
                    
                    int palaceCount = 0;
                    foreach (var facility in facilities.EnumerateArray())
                    {
                        int kindId = facility.TryGetProperty("KindID", out var kindIdElem) 
                            ? kindIdElem.GetInt32() 
                            : -1;
                        
                        if (kindId == 200) // 皇宫
                        {
                            palaceCount++;
                            int facilityId = facility.TryGetProperty("ID", out var fidElem) 
                                ? fidElem.GetInt32() 
                                : -1;
                            Console.WriteLine($"  找到皇宫设施 (Facility ID: {facilityId}, Kind ID: {kindId})");
                        }
                    }
                    
                    Console.WriteLine($"  皇宫数量: {palaceCount}");
                    
                    if (palaceCount > 0 && archsWithDominationBonus == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("❌ 严重问题：存在皇宫设施，但没有建筑有统治上限增益！");
                        Console.WriteLine();
                        Console.WriteLine("这说明：");
                        Console.WriteLine("  1. 设施数据已保存");
                        Console.WriteLine("  2. 但设施的影响没有被应用到建筑上");
                        Console.WriteLine("  3. 或者应用了但IncrementOfDominationCeiling没有被保存");
                        Console.WriteLine();
                        Console.WriteLine("解决方案：");
                        Console.WriteLine("  需要确保在保存游戏前调用 ApplyInfluences()");
                        Console.WriteLine("  或者在读档后立即调用 ApplyInfluences()");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 找不到Facilities数组");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 发生错误：{ex.Message}");
                Console.WriteLine($"堆栈跟踪：\n{ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
