using System;
using System.IO;
using System.Text.Json;

namespace TestFacilityBonusDirectly
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("直接测试设施增益应用");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 读取CommonData.json
            string commonDataPath = "Content/Data/Common/CommonData.json";
            if (!File.Exists(commonDataPath))
            {
                Console.WriteLine($"❌ 找不到CommonData.json");
                Console.ReadKey();
                return;
            }

            try
            {
                string json = File.ReadAllText(commonDataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 检查皇宫设施的影响
                Console.WriteLine("1. 检查皇宫设施（ID 200）的影响配置：");
                if (root.TryGetProperty("AllFacilityKinds", out var allFacilityKinds) &&
                    allFacilityKinds.TryGetProperty("FacilityKinds", out var facilityKinds) &&
                    facilityKinds.TryGetProperty("200", out var palace))
                {
                    string influencesString = palace.TryGetProperty("InfluencesString", out var infStr) 
                        ? infStr.GetString() ?? "" 
                        : "";
                    
                    Console.WriteLine($"  InfluencesString: {influencesString}");
                    
                    var influenceIds = influencesString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // 检查每个影响
                    if (root.TryGetProperty("AllInfluences", out var allInfluences) &&
                        allInfluences.TryGetProperty("Influences", out var influences))
                    {
                        bool foundDominationBonus = false;
                        int dominationBonusValue = 0;
                        
                        foreach (var infId in influenceIds)
                        {
                            if (influences.TryGetProperty(infId, out var influence))
                            {
                                int kindId = -1;
                                if (influence.TryGetProperty("Kind", out var kind))
                                {
                                    kindId = kind.TryGetProperty("ID", out var kindIdElem) 
                                        ? kindIdElem.GetInt32() 
                                        : -1;
                                }
                                
                                if (kindId == 1004) // 统治上限增加
                                {
                                    foundDominationBonus = true;
                                    string parameter = influence.TryGetProperty("Parameter", out var paramElem) 
                                        ? paramElem.GetString() ?? "0" 
                                        : "0";
                                    dominationBonusValue = int.Parse(parameter);
                                    
                                    string infName = influence.TryGetProperty("Name", out var infNameElem) 
                                        ? infNameElem.GetString() ?? "未知" 
                                        : "未知";
                                    
                                    Console.WriteLine($"  ✅ 找到统治上限增益：影响ID {infId} ({infName})");
                                    Console.WriteLine($"     增益值: +{dominationBonusValue}");
                                }
                            }
                        }
                        
                        if (!foundDominationBonus)
                        {
                            Console.WriteLine("  ❌ 皇宫没有统治上限增益影响");
                            Console.ReadKey();
                            return;
                        }
                        
                        Console.WriteLine();
                        Console.WriteLine("2. 模拟应用设施影响：");
                        Console.WriteLine($"  假设建筑基础统治上限: 100");
                        Console.WriteLine($"  应用皇宫影响后: 100 + {dominationBonusValue} = {100 + dominationBonusValue}");
                        Console.WriteLine();
                        Console.WriteLine("✅ 数据配置正确，设施影响应该能正常工作");
                        Console.WriteLine();
                        Console.WriteLine("3. 问题可能出在：");
                        Console.WriteLine("  a) FacilityEnabled标志为false（资金不足支付维护费用）");
                        Console.WriteLine("  b) ApplyInfluences()没有在正确的时机被调用");
                        Console.WriteLine("  c) 用户查看的是当前统治度而不是统治上限");
                        Console.WriteLine();
                        Console.WriteLine("4. 验证方法：");
                        Console.WriteLine("  - 在游戏中查看建筑详情");
                        Console.WriteLine("  - 确认统治上限（不是当前统治度）");
                        Console.WriteLine("  - 检查是否有足够资金支付设施维护费用");
                        Console.WriteLine("  - 查看调试输出（Debug模式下）");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 找不到皇宫设施");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 发生错误：{ex.Message}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
