using System;
using System.IO;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

namespace DiagnoseLoadingSequence
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== 诊断读档加载顺序 ==========");
            Console.WriteLine();
            
            // 查找存档文件
            string saveDir = @"GameData\Save";
            if (!Directory.Exists(saveDir))
            {
                Console.WriteLine($"❌ 存档目录不存在: {saveDir}");
                return;
            }
            
            var saveFiles = Directory.GetFiles(saveDir, "*.sav.gz");
            if (saveFiles.Length == 0)
            {
                Console.WriteLine($"❌ 没有找到存档文件");
                return;
            }
            
            string saveFile = saveFiles[0];
            Console.WriteLine($"✅ 找到存档文件: {saveFile}");
            Console.WriteLine();
            
            try
            {
                // 创建 SerializationManager
                var manager = new SerializationManager();
                
                Console.WriteLine("开始加载存档...");
                Console.WriteLine();
                
                // 加载存档
                GameScenario scenario = manager.LoadGame(saveFile);
                
                Console.WriteLine();
                Console.WriteLine("========== 加载完成，检查数据 ==========");
                Console.WriteLine();
                
                // 检查 GameCommonData
                Console.WriteLine($"scenario.GameCommonData: {(scenario.GameCommonData != null ? "存在" : "null")}");
                
                if (scenario.GameCommonData != null)
                {
                    Console.WriteLine($"  - AllMilitaryKinds: {(scenario.GameCommonData.AllMilitaryKinds != null ? "存在" : "null")}");
                    
                    if (scenario.GameCommonData.AllMilitaryKinds != null)
                    {
                        Console.WriteLine($"  - MilitaryKinds字典: {(scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds != null ? "存在" : "null")}");
                        
                        if (scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds != null)
                        {
                            Console.WriteLine($"  - MilitaryKinds.Count: {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                            
                            // 列出前5个兵种
                            int count = 0;
                            foreach (var kvp in scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds)
                            {
                                Console.WriteLine($"    [{kvp.Key}] {kvp.Value.Name}");
                                count++;
                                if (count >= 5) break;
                            }
                        }
                    }
                }
                
                Console.WriteLine();
                
                // 检查 Militaries
                Console.WriteLine($"scenario.Militaries.Count: {scenario.Militaries?.Count ?? 0}");
                
                if (scenario.Militaries != null && scenario.Militaries.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("检查前3个Military的Kind链接状态:");
                    
                    int checkCount = 0;
                    foreach (Military military in scenario.Militaries.GetList())
                    {
                        Console.WriteLine($"  Military ID={military.ID}, Name={military.Name}");
                        Console.WriteLine($"    - KindID: {military.KindID}");
                        Console.WriteLine($"    - Kind: {(military.Kind != null ? military.Kind.Name : "null")}");
                        
                        checkCount++;
                        if (checkCount >= 3) break;
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("✅ 诊断完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ 加载失败: {ex.GetType().Name}");
                Console.WriteLine($"   消息: {ex.Message}");
                Console.WriteLine($"   堆栈: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
