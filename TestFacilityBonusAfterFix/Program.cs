using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

namespace TestFacilityBonusAfterFix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 测试设施增益修复 ===\n");
            
            string saveFile = @"G:\sanguo\net8\sanguo260210-2\Save\Save1\GameSurvey.xml";
            
            if (!File.Exists(saveFile))
            {
                Console.WriteLine($"❌ 存档文件不存在: {saveFile}");
                return;
            }
            
            Console.WriteLine($"📂 加载存档: {saveFile}\n");
            
            try
            {
                // 加载存档
                var manager = new SerializationManager();
                var scenario = manager.LoadGame(saveFile);
                
                if (scenario == null)
                {
                    Console.WriteLine("❌ 加载存档失败");
                    return;
                }
                
                Console.WriteLine($"✅ 存档加载成功\n");
                Console.WriteLine($"建筑数量: {scenario.Architectures.Count}");
                Console.WriteLine($"设施数量: {scenario.Facilities.Count}\n");
                
                // 查找有设施的建筑
                var architecturesWithFacilities = new List<Architecture>();
                foreach (Architecture arch in scenario.Architectures.GetList())
                {
                    if (arch.Facilities != null && arch.Facilities.Count > 0)
                    {
                        architecturesWithFacilities.Add(arch);
                    }
                }
                
                Console.WriteLine($"有设施的建筑数量: {architecturesWithFacilities.Count}\n");
                
                if (architecturesWithFacilities.Count == 0)
                {
                    Console.WriteLine("⚠️ 没有找到有设施的建筑");
                    return;
                }
                
                // 测试第一个有设施的建筑
                var testArch = architecturesWithFacilities[0];
                
                Console.WriteLine($"=== 测试建筑: {testArch.Name} (ID: {testArch.ID}) ===\n");
                Console.WriteLine($"设施数量: {testArch.Facilities.Count}");
                Console.WriteLine($"设施启用: {testArch.FacilityEnabled}\n");
                
                // 记录应用前的状态
                int beforeIncrement = testArch.IncrementOfDominationCeiling;
                int beforeCeiling = testArch.DominationCeiling;
                int currentDomination = testArch.Domination;
                
                Console.WriteLine($"应用前:");
                Console.WriteLine($"  IncrementOfDominationCeiling: {beforeIncrement}");
                Console.WriteLine($"  DominationCeiling: {beforeCeiling}");
                Console.WriteLine($"  Domination: {currentDomination}\n");
                
                // 列出所有设施
                Console.WriteLine("设施列表:");
                foreach (Facility facility in testArch.Facilities)
                {
                    Console.WriteLine($"  - {facility.Name} (Kind ID: {facility.KindID})");
                    Console.WriteLine($"    维护费用: {facility.MaintenanceCost}");
                    Console.WriteLine($"    影响数量: {facility.Influences.Count}");
                    
                    foreach (var influence in facility.Influences.Influences.Values)
                    {
                        Console.WriteLine($"      * {influence.Name} (ID: {influence.ID})");
                        Console.WriteLine($"        Kind: {influence.Kind?.Name ?? "null"} (ID: {influence.Kind?.ID ?? -1})");
                        Console.WriteLine($"        Parameter: '{influence.Parameter}'");
                    }
                }
                Console.WriteLine();
                
                // 应用设施影响
                Console.WriteLine("=== 应用设施影响 ===\n");
                testArch.ApplyFacilityInfluences(false);
                
                // 记录应用后的状态
                int afterIncrement = testArch.IncrementOfDominationCeiling;
                int afterCeiling = testArch.DominationCeiling;
                
                Console.WriteLine($"\n应用后:");
                Console.WriteLine($"  IncrementOfDominationCeiling: {afterIncrement} (变化: {afterIncrement - beforeIncrement})");
                Console.WriteLine($"  DominationCeiling: {afterCeiling} (变化: {afterCeiling - beforeCeiling})");
                Console.WriteLine($"  Domination: {testArch.Domination}\n");
                
                // 验证结果
                if (afterIncrement > beforeIncrement)
                {
                    Console.WriteLine($"✅ 成功！IncrementOfDominationCeiling 增加了 {afterIncrement - beforeIncrement}");
                    Console.WriteLine($"✅ 成功！DominationCeiling 增加了 {afterCeiling - beforeCeiling}");
                }
                else if (afterIncrement == 0 && beforeIncrement == 0)
                {
                    Console.WriteLine("⚠️ IncrementOfDominationCeiling 没有变化（可能没有统治上限增益的设施）");
                }
                else
                {
                    Console.WriteLine($"❌ 失败！IncrementOfDominationCeiling 没有正确增加");
                }
                
                // 测试多次应用是否会累加
                Console.WriteLine("\n=== 测试多次应用是否会累加 ===\n");
                
                int firstApply = testArch.IncrementOfDominationCeiling;
                Console.WriteLine($"第一次应用后: {firstApply}");
                
                testArch.ApplyFacilityInfluences(false);
                int secondApply = testArch.IncrementOfDominationCeiling;
                Console.WriteLine($"第二次应用后: {secondApply}");
                
                testArch.ApplyFacilityInfluences(false);
                int thirdApply = testArch.IncrementOfDominationCeiling;
                Console.WriteLine($"第三次应用后: {thirdApply}\n");
                
                if (firstApply == secondApply && secondApply == thirdApply)
                {
                    Console.WriteLine($"✅ 成功！多次应用不会累加，值保持为 {firstApply}");
                }
                else
                {
                    Console.WriteLine($"❌ 失败！多次应用会累加");
                    Console.WriteLine($"   第一次: {firstApply}");
                    Console.WriteLine($"   第二次: {secondApply} (累加了 {secondApply - firstApply})");
                    Console.WriteLine($"   第三次: {thirdApply} (累加了 {thirdApply - secondApply})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
