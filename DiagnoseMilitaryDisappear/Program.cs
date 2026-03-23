using System;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace DiagnoseMilitaryDisappear
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断编队消失问题");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 初始化游戏
                Console.WriteLine("正在初始化游戏...");
                Session.Initialize();
                
                // 加载剧本
                Console.WriteLine("正在加载剧本...");
                string scenarioPath = "Content/Data/Scenario/Scenario1/Scenario.xml";
                Session.Current.LoadScenario(scenarioPath);
                
                if (Session.Current.Scenario == null)
                {
                    Console.WriteLine("❌ 剧本加载失败！");
                    return;
                }
                
                Console.WriteLine($"✅ 剧本加载成功: {Session.Current.Scenario.Title}");
                Console.WriteLine();
                
                // 加载存档
                Console.WriteLine("正在加载存档...");
                string savePath = "Save/Save01.json";
                bool loaded = Session.Current.Scenario.LoadSaveFile(savePath);
                
                if (!loaded)
                {
                    Console.WriteLine("❌ 存档加载失败！");
                    return;
                }
                
                Console.WriteLine($"✅ 存档加载成功");
                Console.WriteLine();
                
                // 检查编队数据
                Console.WriteLine("========================================");
                Console.WriteLine("检查编队数据");
                Console.WriteLine("========================================");
                
                int totalMilitaries = Session.Current.Scenario.Militaries.Count;
                Console.WriteLine($"总编队数（Scenario.Militaries）: {totalMilitaries}");
                Console.WriteLine();
                
                // 检查建筑中的编队
                Console.WriteLine("========================================");
                Console.WriteLine("检查建筑中的编队");
                Console.WriteLine("========================================");
                
                int totalArchitectures = 0;
                int archWithMilitaryIDs = 0;
                int archWithMilitaries = 0;
                int archWithMilitariesString = 0;
                
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    totalArchitectures++;
                    
                    bool hasMilitaryIDs = arch.MilitaryIDs != null && arch.MilitaryIDs.Count > 0;
                    bool hasMilitaries = arch.Militaries != null && arch.Militaries.Count > 0;
                    bool hasMilitariesString = !string.IsNullOrEmpty(arch.MilitariesString);
                    
                    if (hasMilitaryIDs) archWithMilitaryIDs++;
                    if (hasMilitaries) archWithMilitaries++;
                    if (hasMilitariesString) archWithMilitariesString++;
                    
                    // 输出前5个有编队的建筑的详细信息
                    if (archWithMilitaryIDs <= 5 && hasMilitaryIDs)
                    {
                        Console.WriteLine($"\n建筑: {arch.Name} (ID: {arch.ID})");
                        Console.WriteLine($"  MilitaryIDs.Count: {arch.MilitaryIDs.Count}");
                        Console.WriteLine($"  Militaries.Count: {arch.Militaries?.Count ?? 0}");
                        Console.WriteLine($"  MilitariesString: '{arch.MilitariesString ?? "null"}'");
                        
                        Console.WriteLine($"  MilitaryIDs:");
                        foreach (int id in arch.MilitaryIDs)
                        {
                            Console.WriteLine($"    - {id}");
                        }
                        
                        if (arch.Militaries != null && arch.Militaries.Count > 0)
                        {
                            Console.WriteLine($"  Militaries:");
                            foreach (Military military in arch.Militaries)
                            {
                                Console.WriteLine($"    - ID: {military.ID}, Name: '{military.Name ?? "null"}'");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  ⚠️ Militaries列表为空！");
                        }
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("统计结果");
                Console.WriteLine("========================================");
                Console.WriteLine($"总建筑数: {totalArchitectures}");
                Console.WriteLine($"有MilitaryIDs的建筑: {archWithMilitaryIDs}");
                Console.WriteLine($"有Militaries的建筑: {archWithMilitaries}");
                Console.WriteLine($"有MilitariesString的建筑: {archWithMilitariesString}");
                
                if (archWithMilitaryIDs > archWithMilitaries)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️ 警告：有MilitaryIDs但Militaries列表为空！");
                    Console.WriteLine("这说明编队ID被正确加载，但没有被添加到Militaries列表");
                    Console.WriteLine("可能的原因：");
                    Console.WriteLine("1. LinkMilitaries方法没有被调用");
                    Console.WriteLine("2. LinkMilitaries方法中的添加代码被注释或跳过");
                    Console.WriteLine("3. LoadMilitariesFromString清空了Militaries列表");
                }
                
                if (archWithMilitariesString > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"⚠️ 注意：有 {archWithMilitariesString} 个建筑有MilitariesString");
                    Console.WriteLine("这可能是旧存档，使用MilitariesString而不是MilitaryIDs");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
