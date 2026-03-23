using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace DiagnoseFacilityBonus
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断设施增益问题");
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

                // 检查 Architectures
                if (root.TryGetProperty("Architectures", out var architectures))
                {
                    Console.WriteLine($"✅ 找到 {architectures.GetArrayLength()} 个建筑");
                    Console.WriteLine();

                    int totalArchs = 0;
                    int archsWithFacilities = 0;
                    int archsWithDominationBonus = 0;

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
                        bool hasFacilities = false;
                        int facilityCount = 0;
                        if (arch.TryGetProperty("FacilityIDs", out var facilityIDs))
                        {
                            facilityCount = facilityIDs.GetArrayLength();
                            hasFacilities = facilityCount > 0;
                        }
                        else if (arch.TryGetProperty("FacilitiesString", out var facilitiesString))
                        {
                            string? facStr = facilitiesString.GetString();
                            hasFacilities = !string.IsNullOrEmpty(facStr) && facStr != "null";
                        }
                        
                        if (hasFacilities)
                        {
                            archsWithFacilities++;
                        }
                        
                        // 检查统治上限增益
                        int dominationCeilingBonus = 0;
                        if (arch.TryGetProperty("IncrementOfDominationCeiling", out var incCeiling))
                        {
                            dominationCeilingBonus = incCeiling.GetInt32();
                            if (dominationCeilingBonus > 0)
                            {
                                archsWithDominationBonus++;
                            }
                        }
                        
                        // 显示前5个有设施的建筑的详细信息
                        if (hasFacilities && archsWithFacilities <= 5)
                        {
                            Console.WriteLine($"建筑 #{totalArchs}: {name} (ID:{id})");
                            Console.WriteLine($"  设施数量: {facilityCount}");
                            
                            if (arch.TryGetProperty("Domination", out var domination))
                            {
                                Console.WriteLine($"  统治度: {domination.GetInt32()}");
                            }
                            
                            Console.WriteLine($"  统治上限增益: {dominationCeilingBonus}");
                            
                            if (arch.TryGetProperty("IncrementOfDominationPerDay", out var incPerDay))
                            {
                                Console.WriteLine($"  每日统治增益: {incPerDay.GetInt32()}");
                            }
                            
                            // 显示其他增益
                            if (arch.TryGetProperty("IncrementOfAgricultureCeiling", out var incAgri))
                            {
                                Console.WriteLine($"  农业上限增益: {incAgri.GetInt32()}");
                            }
                            if (arch.TryGetProperty("IncrementOfCommerceCeiling", out var incComm))
                            {
                                Console.WriteLine($"  商业上限增益: {incComm.GetInt32()}");
                            }
                            
                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine("========================================");
                    Console.WriteLine("统计结果：");
                    Console.WriteLine($"总建筑数: {totalArchs}");
                    Console.WriteLine($"有设施的建筑: {archsWithFacilities}");
                    Console.WriteLine($"有统治上限增益的建筑: {archsWithDominationBonus}");
                    Console.WriteLine();

                    if (archsWithFacilities > 0 && archsWithDominationBonus == 0)
                    {
                        Console.WriteLine("❌ 问题：有设施但没有统治上限增益！");
                        Console.WriteLine("   可能原因：");
                        Console.WriteLine("   1. 设施的影响没有被应用");
                        Console.WriteLine("   2. IncrementOfDominationCeiling 没有被保存");
                        Console.WriteLine("   3. 读档后没有调用 ApplyInfluences()");
                    }
                    else if (archsWithDominationBonus > 0)
                    {
                        Console.WriteLine("✅ 存档中包含统治上限增益数据");
                        Console.WriteLine("   如果游戏中看不到效果，可能是：");
                        Console.WriteLine("   1. 读档后没有正确应用增益");
                        Console.WriteLine("   2. 统治上限计算公式有问题");
                    }
                    else
                    {
                        Console.WriteLine("⚠️  没有建筑有设施或增益");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 存档中没有找到 Architectures 数据");
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
