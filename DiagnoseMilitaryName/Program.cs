using System;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace DiagnoseMilitaryName
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断编队名称问题");
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
                
                int totalMilitaries = 0;
                int militariesWithName = 0;
                int militariesWithoutName = 0;
                int militariesWithKind = 0;
                int militariesWithoutKind = 0;
                
                foreach (Military military in Session.Current.Scenario.Militaries)
                {
                    totalMilitaries++;
                    
                    bool hasName = !string.IsNullOrEmpty(military.Name);
                    bool hasKind = military.Kind != null;
                    
                    if (hasName) militariesWithName++;
                    else militariesWithoutName++;
                    
                    if (hasKind) militariesWithKind++;
                    else militariesWithoutKind++;
                    
                    // 输出前10个编队的详细信息
                    if (totalMilitaries <= 10)
                    {
                        Console.WriteLine($"\n编队 ID: {military.ID}");
                        Console.WriteLine($"  Name: '{military.Name ?? "null"}'");
                        Console.WriteLine($"  KindID: {military.KindID}");
                        Console.WriteLine($"  Kind: {(military.Kind != null ? military.Kind.Name : "null")}");
                        Console.WriteLine($"  BelongedArchitecture: {military.BelongedArchitecture?.Name ?? "null"}");
                        Console.WriteLine($"  Quantity: {military.Quantity}");
                        Console.WriteLine($"  Morale: {military.Morale}");
                        Console.WriteLine($"  Combativity: {military.Combativity}");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("统计结果");
                Console.WriteLine("========================================");
                Console.WriteLine($"总编队数: {totalMilitaries}");
                Console.WriteLine($"有名称的编队: {militariesWithName}");
                Console.WriteLine($"无名称的编队: {militariesWithoutName}");
                Console.WriteLine($"有Kind的编队: {militariesWithKind}");
                Console.WriteLine($"无Kind的编队: {militariesWithoutKind}");
                
                if (militariesWithoutName > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️ 警告：有编队没有名称！");
                    Console.WriteLine("可能的原因：");
                    Console.WriteLine("1. 序列化时Name字段没有被保存");
                    Console.WriteLine("2. 反序列化时Name字段没有被恢复");
                    Console.WriteLine("3. 读档后Name被某个地方覆盖为null或空字符串");
                }
                
                if (militariesWithoutKind > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️ 警告：有编队没有Kind！");
                    Console.WriteLine("可能的原因：");
                    Console.WriteLine("1. KindID无效");
                    Console.WriteLine("2. AllMilitaryKinds未正确加载");
                    Console.WriteLine("3. Kind属性的getter有问题");
                }
                
                // 检查建筑中的编队
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("检查建筑中的编队");
                Console.WriteLine("========================================");
                
                int archWithMilitaries = 0;
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    if (arch.Militaries != null && arch.Militaries.Count > 0)
                    {
                        archWithMilitaries++;
                        
                        if (archWithMilitaries <= 3)
                        {
                            Console.WriteLine($"\n建筑: {arch.Name} (ID: {arch.ID})");
                            Console.WriteLine($"  编队数量: {arch.Militaries.Count}");
                            
                            foreach (Military military in arch.Militaries)
                            {
                                Console.WriteLine($"    - ID: {military.ID}, Name: '{military.Name ?? "null"}', Kind: {military.Kind?.Name ?? "null"}");
                            }
                        }
                    }
                }
                
                Console.WriteLine($"\n有编队的建筑数: {archWithMilitaries}");
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
