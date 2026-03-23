using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace TestPalaceFacility
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("检查皇宫设施的统治上限增益配置");
            Console.WriteLine("========================================");
            Console.WriteLine();

            string commonDataPath = "Content/Data/Common/CommonData.json";
            
            if (!File.Exists(commonDataPath))
            {
                Console.WriteLine($"❌ 找不到CommonData.json: {commonDataPath}");
                Console.ReadKey();
                return;
            }

            try
            {
                string json = File.ReadAllText(commonDataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 1. 检查皇宫设施（ID 200）
                Console.WriteLine("1. 检查皇宫设施（ID 200）：");
                if (root.TryGetProperty("AllFacilityKinds", out var allFacilityKinds) &&
                    allFacilityKinds.TryGetProperty("FacilityKinds", out var facilityKinds) &&
                    facilityKinds.TryGetProperty("200", out var palace))
                {
                    string name = palace.TryGetProperty("Name", out var nameElem) 
                        ? nameElem.GetString() ?? "未知" 
                        : "未知";
                    string influencesString = palace.TryGetProperty("InfluencesString", out var infStr) 
                        ? infStr.GetString() ?? "" 
                        : "";
                    
                    Console.WriteLine($"  ✓ 找到皇宫: {name}");
                    Console.WriteLine($"  InfluencesString: {influencesString}");
                    
                    var influenceIds = influencesString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"  影响数量: {influenceIds.Length}");
                    Console.WriteLine();
                    
                    // 2. 检查每个影响
                    Console.WriteLine("2. 检查皇宫的每个影响：");
                    if (root.TryGetProperty("AllInfluences", out var allInfluences) &&
                        allInfluences.TryGetProperty("Influences", out var influences))
                    {
                        bool foundDominationBonus = false;
                        
                        foreach (var infId in influenceIds)
                        {
                            if (influences.TryGetProperty(infId, out var influence))
                            {
                                string infName = influence.TryGetProperty("Name", out var infNameElem) 
                                    ? infNameElem.GetString() ?? "未知" 
                                    : "未知";
                                
                                int kindId = -1;
                                string kindName = "未知";
                                if (influence.TryGetProperty("Kind", out var kind))
                                {
                                    kindId = kind.TryGetProperty("ID", out var kindIdElem) 
                                        ? kindIdElem.GetInt32() 
                                        : -1;
                                    kindName = kind.TryGetProperty("Name", out var kindNameElem) 
                                        ? kindNameElem.GetString() ?? "未知" 
                                        : "未知";
                                }
                                
                                Console.WriteLine($"  - 影响 {infId}: {infName}");
                                Console.WriteLine($"    Kind ID: {kindId}, Kind Name: {kindName}");
                                
                                // 检查是否是统治上限增加（Kind ID 1004）
                                if (kindId == 1004)
                                {
                                    foundDominationBonus = true;
                                    string parameter = influence.TryGetProperty("Parameter", out var paramElem) 
                                        ? paramElem.GetString() ?? "0" 
                                        : "0";
                                    Console.WriteLine($"    ⭐ 这是统治上限增加影响！参数: {parameter}");
                                }
                            }
                        }
                        
                        Console.WriteLine();
                        if (foundDominationBonus)
                        {
                            Console.WriteLine("✅ 皇宫包含统治上限增加影响");
                        }
                        else
                        {
                            Console.WriteLine("❌ 皇宫不包含统治上限增加影响");
                            Console.WriteLine("\n问题分析：");
                            Console.WriteLine("  皇宫的InfluencesString中没有包含Kind ID为1004的影响");
                            Console.WriteLine("  需要在CommonData.json中为皇宫添加统治上限增加的影响");
                        }
                    }
                    
                    // 3. 检查InfluenceKind 1004的定义
                    Console.WriteLine("\n3. 检查InfluenceKind 1004（统治上限增加）的定义：");
                    if (root.TryGetProperty("AllInfluenceKinds", out var allInfluenceKinds) &&
                        allInfluenceKinds.TryGetProperty("InfluenceKinds", out var influenceKinds) &&
                        influenceKinds.TryGetProperty("1004", out var influenceKind1004))
                    {
                        string kindName = influenceKind1004.TryGetProperty("Name", out var kindNameElem) 
                            ? kindNameElem.GetString() ?? "未知" 
                            : "未知";
                        int type = influenceKind1004.TryGetProperty("Type", out var typeElem) 
                            ? typeElem.GetInt32() 
                            : -1;
                        
                        Console.WriteLine($"  ✓ 找到InfluenceKind 1004: {kindName}");
                        Console.WriteLine($"    Type: {type} (5=建筑)");
                        
                        if (type == 5)
                        {
                            Console.WriteLine("  ✅ Type正确，是建筑类型");
                        }
                        else
                        {
                            Console.WriteLine($"  ❌ Type错误，应该是5（建筑），实际是{type}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  ❌ 找不到InfluenceKind 1004");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 找不到皇宫设施（ID 200）");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 发生错误：{ex.Message}");
                Console.WriteLine($"堆栈跟踪：\n{ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
