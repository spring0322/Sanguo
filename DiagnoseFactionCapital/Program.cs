using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

namespace DiagnoseFactionCapital
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🔍 诊断 Faction.Capital 为 null 问题");
            Console.WriteLine("=====================================\n");

            // 查找最新的存档文件
            string saveDir = "GameComponents/All/Save";
            if (!Directory.Exists(saveDir))
            {
                Console.WriteLine($"❌ 存档目录不存在: {saveDir}");
                return;
            }

            var saveFiles = Directory.GetFiles(saveDir, "*.sav")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (saveFiles.Count == 0)
            {
                Console.WriteLine("❌ 没有找到存档文件");
                return;
            }

            string latestSave = saveFiles[0];
            Console.WriteLine($"📁 加载存档: {Path.GetFileName(latestSave)}");
            Console.WriteLine($"   修改时间: {File.GetLastWriteTime(latestSave)}\n");

            try
            {
                // 加载存档
                var serializer = new GameScenarioSerializer();
                var scenario = serializer.LoadScenario(latestSave);

                if (scenario == null)
                {
                    Console.WriteLine("❌ 加载存档失败");
                    return;
                }

                Console.WriteLine($"✅ 存档加载成功\n");
                Console.WriteLine("=" + new string('=', 70));
                Console.WriteLine("势力数据诊断");
                Console.WriteLine("=" + new string('=', 70) + "\n");

                int totalFactions = 0;
                int nullCapitalCount = 0;
                int emptyArchitecturesCount = 0;
                int invalidCapitalIDCount = 0;

                foreach (var factionObj in scenario.Factions)
                {
                    if (factionObj is Faction faction)
                    {
                        totalFactions++;
                        
                        Console.WriteLine($"势力 #{totalFactions}: {faction.Name} (ID={faction.ID})");
                        Console.WriteLine($"  CapitalID: {faction.CapitalID}");
                        Console.WriteLine($"  Architectures.Count: {faction.Architectures?.Count ?? 0}");
                        
                        // 检查 Architectures 列表
                        if (faction.Architectures == null || faction.Architectures.Count == 0)
                        {
                            Console.WriteLine($"  ⚠️  Architectures 为空或 null");
                            emptyArchitecturesCount++;
                        }
                        else
                        {
                            Console.WriteLine($"  建筑列表:");
                            foreach (var archObj in faction.Architectures)
                            {
                                if (archObj is Architecture arch)
                                {
                                    Console.WriteLine($"    - {arch.Name} (ID={arch.ID}, BelongedFactionID={arch.BelongedFactionID})");
                                }
                            }
                        }

                        // 检查 CapitalID 是否有效
                        if (faction.CapitalID < 0)
                        {
                            Console.WriteLine($"  ⚠️  CapitalID 无效: {faction.CapitalID}");
                            invalidCapitalIDCount++;
                        }
                        else
                        {
                            // 尝试从 scenario.Architectures 查找
                            var capitalArch = scenario.Architectures.GetGameObject(faction.CapitalID) as Architecture;
                            if (capitalArch == null)
                            {
                                Console.WriteLine($"  ❌ 无法从 scenario.Architectures 找到 ID={faction.CapitalID} 的建筑");
                            }
                            else
                            {
                                Console.WriteLine($"  ✅ 找到首都建筑: {capitalArch.Name}");
                            }
                        }

                        // 检查 Capital 属性
                        try
                        {
                            var capital = faction.Capital;
                            if (capital == null)
                            {
                                Console.WriteLine($"  ❌ Capital 属性为 null");
                                nullCapitalCount++;
                            }
                            else
                            {
                                Console.WriteLine($"  ✅ Capital: {capital.Name} (ID={capital.ID})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ❌ 访问 Capital 属性时异常: {ex.Message}");
                            nullCapitalCount++;
                        }

                        Console.WriteLine();
                    }
                }

                // 统计摘要
                Console.WriteLine("=" + new string('=', 70));
                Console.WriteLine("诊断摘要");
                Console.WriteLine("=" + new string('=', 70));
                Console.WriteLine($"总势力数: {totalFactions}");
                Console.WriteLine($"Capital 为 null: {nullCapitalCount}");
                Console.WriteLine($"Architectures 为空: {emptyArchitecturesCount}");
                Console.WriteLine($"CapitalID 无效: {invalidCapitalIDCount}");
                Console.WriteLine();

                if (nullCapitalCount > 0)
                {
                    Console.WriteLine("🔥 问题分析:");
                    Console.WriteLine("  1. 检查 Architectures 列表是否正确恢复");
                    Console.WriteLine("  2. 检查 CapitalID 是否有效");
                    Console.WriteLine("  3. 检查建筑的 BelongedFactionID 是否正确");
                    Console.WriteLine("  4. 检查 Capital 属性的 getter 逻辑");
                }
                else
                {
                    Console.WriteLine("✅ 所有势力的 Capital 都正常");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 诊断过程中发生异常: {ex.Message}");
                Console.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
