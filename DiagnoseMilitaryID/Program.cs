using System;
using System.Linq;
using GameObjects;
using GameManager;

namespace DiagnoseMilitaryID
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 诊断编队ID分配问题 ===\n");
            
            try
            {
                // 初始化游戏
                Session.GlobalVariables.InitializeGameFramework();
                Session.GlobalVariables.InitializeParameters();
                
                // 加载剧本
                string scenarioPath = @"Content\Data\Scenario\Save\一统天下.scn";
                if (!System.IO.File.Exists(scenarioPath))
                {
                    Console.WriteLine($"❌ 剧本文件不存在: {scenarioPath}");
                    return;
                }
                
                Console.WriteLine($"📂 加载剧本: {scenarioPath}");
                Session.Current.Scenario = new GameScenario();
                Session.Current.Scenario.LoadScenario(scenarioPath);
                
                Console.WriteLine($"✅ 剧本加载成功\n");
                
                // 检查洛阳的编队
                var luoyang = Session.Current.Scenario.Architectures.GetGameObject(0) as Architecture;
                if (luoyang == null)
                {
                    Console.WriteLine("❌ 找不到洛阳(ID=0)");
                    return;
                }
                
                Console.WriteLine($"🏰 建筑: {luoyang.Name} (ID={luoyang.ID})");
                Console.WriteLine($"   编队数量: {luoyang.Militaries.Count}");
                Console.WriteLine($"   全局编队数量: {Session.Current.Scenario.Militaries.Count}\n");
                
                // 检查GetFreeGameObjectID的行为
                Console.WriteLine("🔍 测试GetFreeGameObjectID:");
                Console.WriteLine($"   当前Militaries.Count = {Session.Current.Scenario.Militaries.Count}");
                
                // 显示现有的ID
                var existingIDs = Session.Current.Scenario.Militaries.GetList().GameObjects
                    .Cast<Military>()
                    .Select(m => m.ID)
                    .OrderBy(id => id)
                    .ToList();
                
                Console.WriteLine($"   现有ID: {string.Join(", ", existingIDs.Take(20))}...");
                
                // 测试GetFreeGameObjectID
                int freeID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
                Console.WriteLine($"   GetFreeGameObjectID返回: {freeID}");
                Console.WriteLine($"   ID {freeID} 是否已存在: {Session.Current.Scenario.Militaries.HasGameObject(freeID)}");
                
                // 检查洛阳的编队详情
                Console.WriteLine($"\n📋 洛阳的编队详情:");
                int index = 0;
                foreach (Military m in luoyang.Militaries.GetList().GameObjects.Cast<Military>())
                {
                    Console.WriteLine($"   [{index}] ID={m.ID}, Name='{m.Name}', KindID={m.KindID}, Kind={m.Kind?.Name ?? "null"}");
                    index++;
                }
                
                // 检查是否有ID冲突
                var duplicateIDs = existingIDs.GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { ID = g.Key, Count = g.Count() })
                    .ToList();
                
                if (duplicateIDs.Any())
                {
                    Console.WriteLine($"\n⚠️ 发现ID冲突:");
                    foreach (var dup in duplicateIDs)
                    {
                        Console.WriteLine($"   ID {dup.ID} 出现了 {dup.Count} 次");
                    }
                }
                else
                {
                    Console.WriteLine($"\n✅ 没有ID冲突");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 异常: {ex.Message}");
                Console.WriteLine($"堆栈: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
