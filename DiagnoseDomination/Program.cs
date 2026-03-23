using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace DiagnoseDomination
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断存档中的统治度数据");
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
                    int archsWithDomination = 0;
                    int archsWithZeroDomination = 0;
                    int archsWithoutDominationField = 0;

                    foreach (var arch in architectures.EnumerateArray())
                    {
                        totalArchs++;
                        
                        if (arch.TryGetProperty("Domination", out var domination))
                        {
                            archsWithDomination++;
                            int dominationValue = domination.GetInt32();
                            
                            if (dominationValue == 0)
                            {
                                archsWithZeroDomination++;
                            }

                            // 显示前5个建筑的详细信息
                            if (totalArchs <= 5)
                            {
                                string name = arch.TryGetProperty("Name", out var nameElem) 
                                    ? nameElem.GetString() ?? "未知"
                                    : "未知";
                                int id = arch.TryGetProperty("ID", out var idElem) 
                                    ? idElem.GetInt32() 
                                    : -1;
                                
                                Console.WriteLine($"建筑 #{totalArchs}: {name} (ID:{id})");
                                Console.WriteLine($"  统治度: {dominationValue}");
                                
                                if (arch.TryGetProperty("IncrementOfDominationCeiling", out var incCeiling))
                                {
                                    Console.WriteLine($"  统治上限增益: {incCeiling.GetInt32()}");
                                }
                                if (arch.TryGetProperty("IncrementOfDominationPerDay", out var incPerDay))
                                {
                                    Console.WriteLine($"  每日统治增益: {incPerDay.GetInt32()}");
                                }
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            archsWithoutDominationField++;
                        }
                    }

                    Console.WriteLine("========================================");
                    Console.WriteLine("统计结果：");
                    Console.WriteLine($"总建筑数: {totalArchs}");
                    Console.WriteLine($"有统治度字段: {archsWithDomination}");
                    Console.WriteLine($"统治度为0: {archsWithZeroDomination}");
                    Console.WriteLine($"缺少统治度字段: {archsWithoutDominationField}");
                    Console.WriteLine();

                    if (archsWithoutDominationField > 0)
                    {
                        Console.WriteLine("❌ 问题：存档中缺少 Domination 字段！");
                        Console.WriteLine("   这是导致读档后统治度变成0的根本原因。");
                        Console.WriteLine("   需要重新保存游戏以包含 Domination 字段。");
                    }
                    else if (archsWithZeroDomination > 0)
                    {
                        Console.WriteLine($"⚠️  警告：有 {archsWithZeroDomination} 个建筑的统治度为0");
                        Console.WriteLine("   这可能是正常的（新建筑），也可能是数据问题。");
                    }
                    else
                    {
                        Console.WriteLine("✅ 所有建筑都有统治度数据！");
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
