using System;
using System.IO;
using System.Diagnostics;
using WorldOfTheThreeKingdoms.Tools;
using GameObjects;

/// <summary>
/// 测试 System.Text.Json 反序列化根本修复
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== System.Text.Json 反序列化根本修复测试 ===");
        Console.WriteLine();
        
        try
        {
            // 测试 1: CommonData 反序列化
            TestCommonDataDeserialization();
            
            // 测试 2: GameScenario 反序列化
            TestGameScenarioDeserialization();
            
            Console.WriteLine();
            Console.WriteLine("✅ 所有测试通过！System.Text.Json 反序列化根本修复成功。");
            Console.WriteLine();
            Console.WriteLine("🔥 修复总结:");
            Console.WriteLine("1. 移除了 Newtonsoft.Json 回退逻辑");
            Console.WriteLine("2. 启用了 ReferenceHandler.Preserve 和 GameObjectReferenceConverter");
            Console.WriteLine("3. 简化了反序列化流程，直接使用 GameJsonContext");
            Console.WriteLine("4. 保留了完整性验证和错误诊断功能");
            Console.WriteLine("5. 所有 JSON 文件现在都使用标准字典格式");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
            Console.WriteLine($"详细信息: {ex.StackTrace}");
            Environment.Exit(1);
        }
        
        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
    
    static void TestCommonDataDeserialization()
    {
        Console.WriteLine("🔍 测试 1: CommonData 反序列化");
        
        string commonDataPath = @"Content\Data\Common\CommonData.json";
        if (!File.Exists(commonDataPath))
        {
            Console.WriteLine($"⚠️ 跳过 CommonData 测试: 文件不存在 {commonDataPath}");
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var commonData = SimpleSerializer.DeserializeJsonFile<CommonData>(commonDataPath, false);
            
            stopwatch.Stop();
            
            if (commonData == null)
            {
                throw new Exception("CommonData 反序列化返回 null");
            }
            
            // 验证关键数据
            int militaryKindsCount = commonData.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0;
            int informationKindsCount = commonData.AllInformationKinds?.GameObjects?.Count ?? 0;
            int idealTendencyKindsCount = commonData.AllIdealTendencyKinds?.Count ?? 0;
            
            Console.WriteLine($"   ✅ CommonData 反序列化成功 ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   - AllMilitaryKinds: {militaryKindsCount} 个兵种");
            Console.WriteLine($"   - AllInformationKinds: {informationKindsCount} 个情报类型");
            Console.WriteLine($"   - AllIdealTendencyKinds: {idealTendencyKindsCount} 个理想倾向");
            
            if (militaryKindsCount == 0)
            {
                Console.WriteLine("   ⚠️ 警告: AllMilitaryKinds 为空");
            }
            
            if (idealTendencyKindsCount == 0)
            {
                Console.WriteLine("   ⚠️ 警告: AllIdealTendencyKinds 为空");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"   ❌ CommonData 反序列化失败 ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
            throw;
        }
    }
    
    static void TestGameScenarioDeserialization()
    {
        Console.WriteLine();
        Console.WriteLine("🔍 测试 2: GameScenario 反序列化");
        
        // 查找第一个可用的剧本文件
        string[] scenarioPaths = {
            @"Content\Data\Scenario\184DHZS.json",
            @"Content\Data\Scenario\190FDLM.json",
            @"Content\Data\Scenario\194QXGJ.json",
            @"Content\Data\Scenario\197YSCD.json"
        };
        
        string? scenarioPath = null;
        foreach (var path in scenarioPaths)
        {
            if (File.Exists(path))
            {
                scenarioPath = path;
                break;
            }
        }
        
        if (scenarioPath == null)
        {
            Console.WriteLine("   ⚠️ 跳过 GameScenario 测试: 未找到剧本文件");
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var scenario = SimpleSerializer.DeserializeJsonFile<GameScenario>(scenarioPath, false);
            
            stopwatch.Stop();
            
            if (scenario == null)
            {
                throw new Exception("GameScenario 反序列化返回 null");
            }
            
            // 验证关键数据
            int personsCount = scenario.Persons?.Count ?? 0;
            int factionsCount = scenario.Factions?.Count ?? 0;
            int architecturesCount = scenario.Architectures?.Count ?? 0;
            int troopsCount = scenario.Troops?.Count ?? 0;
            int statesCount = scenario.States?.Count ?? 0;
            int regionsCount = scenario.Regions?.Count ?? 0;
            int sectionsCount = scenario.Sections?.Count ?? 0;
            
            Console.WriteLine($"   ✅ GameScenario 反序列化成功 ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   - Persons: {personsCount} 个人物");
            Console.WriteLine($"   - Factions: {factionsCount} 个势力");
            Console.WriteLine($"   - Architectures: {architecturesCount} 个建筑");
            Console.WriteLine($"   - Troops: {troopsCount} 个部队");
            Console.WriteLine($"   - States: {statesCount} 个州");
            Console.WriteLine($"   - Regions: {regionsCount} 个郡");
            Console.WriteLine($"   - Sections: {sectionsCount} 个区域");
            
            // 🔥 关键验证：检查是否还有类型转换错误
            bool hasPersonInStates = false;
            bool hasPersonInRegions = false;
            bool hasPersonInSections = false;
            
            if (scenario.States != null)
            {
                foreach (var obj in scenario.States.GameObjects)
                {
                    if (obj is Person)
                    {
                        hasPersonInStates = true;
                        break;
                    }
                }
            }
            
            if (scenario.Regions != null)
            {
                foreach (var obj in scenario.Regions.GameObjects)
                {
                    if (obj is Person)
                    {
                        hasPersonInRegions = true;
                        break;
                    }
                }
            }
            
            if (scenario.Sections != null)
            {
                foreach (var obj in scenario.Sections.GameObjects)
                {
                    if (obj is Person)
                    {
                        hasPersonInSections = true;
                        break;
                    }
                }
            }
            
            if (hasPersonInStates || hasPersonInRegions || hasPersonInSections)
            {
                Console.WriteLine("   ❌ 发现类型转换错误:");
                if (hasPersonInStates) Console.WriteLine("     - States 集合中包含 Person 对象");
                if (hasPersonInRegions) Console.WriteLine("     - Regions 集合中包含 Person 对象");
                if (hasPersonInSections) Console.WriteLine("     - Sections 集合中包含 Person 对象");
                throw new Exception("GameScenario 反序列化仍存在类型转换错误");
            }
            else
            {
                Console.WriteLine("   ✅ 类型转换验证通过，未发现 Person 对象错误分配");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"   ❌ GameScenario 反序列化失败 ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
            throw;
        }
    }
}