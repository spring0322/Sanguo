using System;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace DiagnoseLoadedFacilities
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断读档后的设施数据");
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
                
                // 检查建筑的设施数据
                Console.WriteLine("========================================");
                Console.WriteLine("检查建筑的设施数据");
                Console.WriteLine("========================================");
                
                int totalArchitectures = 0;
                int archWithFacilities = 0;
                int archWithFacilityString = 0;
                int archWithIncrementBonus = 0;
                
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    totalArchitectures++;
                    
                    bool hasFacilities = arch.Facilities != null && arch.Facilities.Count > 0;
                    bool hasFacilityString = !string.IsNullOrEmpty(arch.FacilitiesString);
                    bool hasIncrementBonus = arch.IncrementOfDominationCeiling > 0;
                    
                    if (hasFacilities) archWithFacilities++;
                    if (hasFacilityString) archWithFacilityString++;
                    if (hasIncrementBonus) archWithIncrementBonus++;
                    
                    // 输出前5个有设施的建筑的详细信息
                    if (archWithFacilities <= 5 && hasFacilities)
                    {
                        Console.WriteLine($"\n建筑: {arch.Name} (ID: {arch.ID})");
                        Console.WriteLine($"  势力: {arch.BelongedFaction?.Name ?? "无"}");
                        Console.WriteLine($"  设施数量: {arch.Facilities.Count}");
                        Console.WriteLine($"  FacilitiesString: '{arch.FacilitiesString ?? "null"}'");
                        Console.WriteLine($"  IncrementOfDominationCeiling: {arch.IncrementOfDominationCeiling}");
                        Console.WriteLine($"  DominationCeiling: {arch.DominationCeiling}");
                        Console.WriteLine($"  Domination: {arch.Domination}");
                        Console.WriteLine($"  FacilityEnabled: {arch.FacilityEnabled}");
                        
                        Console.WriteLine($"  设施列表:");
                        foreach (Facility facility in arch.Facilities)
                        {
                            Console.WriteLine($"    - {facility.Name} (Kind ID: {facility.KindID})");
                            Console.WriteLine($"      影响数量: {facility.Influences?.Count ?? 0}");
                            Console.WriteLine($"      维护费用: {facility.MaintenanceCost}");
                            Console.WriteLine($"      Enabled: {facility.Enabled}");
                            
                            if (facility.Influences != null && facility.Influences.Count > 0)
                            {
                                foreach (var influence in facility.Influences.Influences.Values)
                                {
                                    Console.WriteLine($"        影响: {influence.Kind?.Name ?? "未知"}");
                                }
                            }
                        }
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("统计结果");
                Console.WriteLine("========================================");
                Console.WriteLine($"总建筑数: {totalArchitectures}");
                Console.WriteLine($"有设施集合的建筑: {archWithFacilities}");
                Console.WriteLine($"有FacilitiesString的建筑: {archWithFacilityString}");
                Console.WriteLine($"有统治度增益的建筑: {archWithIncrementBonus}");
                
                if (archWithFacilities == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️ 警告：没有建筑有设施数据！");
                    Console.WriteLine("这可能是因为：");
                    Console.WriteLine("1. 存档是旧版本，没有序列化设施数据");
                    Console.WriteLine("2. LoadFacilitiesFromString 没有被正确调用");
                    Console.WriteLine("3. 设施数据在序列化时丢失");
                }
                
                if (archWithIncrementBonus == 0 && archWithFacilities > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️ 警告：有设施但没有统治度增益！");
                    Console.WriteLine("这可能是因为：");
                    Console.WriteLine("1. ApplyInfluences 没有正确应用设施影响");
                    Console.WriteLine("2. FacilityEnabled 为 false");
                    Console.WriteLine("3. 设施影响数据丢失");
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
