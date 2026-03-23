using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace DiagnoseDominationBonus
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            string saveFile = args.Length > 0 ? args[0] : "temp_save.json";
            
            if (!File.Exists(saveFile))
            {
                Console.WriteLine($"❌ 找不到存档文件: {saveFile}");
                return;
            }
            
            Console.WriteLine($"📂 读取存档: {saveFile}");
            Console.WriteLine();
            
            try
            {
                string json = File.ReadAllText(saveFile);
                var saveInfo = JsonSerializer.Deserialize<GameScenarioDTO>(json);
                
                if (saveInfo?.Architectures == null)
                {
                    Console.WriteLine("❌ 存档数据无效");
                    return;
                }
                
                Console.WriteLine($"✅ 成功读取存档，共 {saveInfo.Architectures.Count} 个建筑");
                Console.WriteLine();
                Console.WriteLine("=" .PadRight(80, '='));
                Console.WriteLine("\n📊 统治上限增益检查：\n");
                
                int totalArchs = 0;
                int archsWithBonus = 0;
                int archsWithFacilities = 0;
                
                // 检查所有建筑的统治上限增益
                foreach (var arch in saveInfo.Architectures)
                {
                    totalArchs++;
                    
                    bool hasFacilities = arch.FacilityIDs != null && arch.FacilityIDs.Count > 0;
                    bool hasBonus = arch.IncrementOfDominationCeiling > 0;
                    
                    if (hasFacilities)
                    {
                        archsWithFacilities++;
                    }
                    
                    if (hasBonus)
                    {
                        archsWithBonus++;
                    }
                    
                    // 只显示有设施或有增益的建筑
                    if (hasFacilities || hasBonus)
                    {
                        Console.WriteLine($"🏰 {arch.Name} (ID: {arch.ID})");
                        Console.WriteLine($"   设施数量: {(hasFacilities ? arch.FacilityIDs.Count : 0)}");
                        Console.WriteLine($"   IncrementOfDominationCeiling: {arch.IncrementOfDominationCeiling}");
                        Console.WriteLine($"   Domination: {arch.Domination}");
                        
                        if (hasFacilities && !hasBonus)
                        {
                            Console.WriteLine($"   ⚠️ 有设施但没有统治上限增益！");
                        }
                        else if (hasBonus)
                        {
                            Console.WriteLine($"   ✅ 有统治上限增益");
                        }
                        
                        Console.WriteLine();
                    }
                }
                
                Console.WriteLine("=" .PadRight(80, '='));
                Console.WriteLine($"\n📈 统计：");
                Console.WriteLine($"   总建筑数: {totalArchs}");
                Console.WriteLine($"   有设施的建筑: {archsWithFacilities}");
                Console.WriteLine($"   有统治上限增益的建筑: {archsWithBonus}");
                
                if (archsWithFacilities > 0 && archsWithBonus == 0)
                {
                    Console.WriteLine($"\n❌ 问题：有 {archsWithFacilities} 个建筑有设施，但没有任何建筑有统治上限增益！");
                    Console.WriteLine($"   这说明设施的影响没有被正确应用或保存。");
                }
                else if (archsWithBonus < archsWithFacilities)
                {
                    Console.WriteLine($"\n⚠️ 警告：有设施的建筑 ({archsWithFacilities}) 多于有增益的建筑 ({archsWithBonus})");
                }
                else if (archsWithBonus > 0)
                {
                    Console.WriteLine($"\n✅ 统治上限增益已正确保存到存档中");
                }
                
                Console.WriteLine("\n✅ 诊断完成");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"   堆栈: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
