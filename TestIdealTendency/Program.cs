using System;
using System.IO;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;

class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("测试IdealTendencyKind反序列化...");
            
            // 读取CommonData.json文件
            string commonDataPath = "Content/Data/Common/CommonData.json";
            if (!File.Exists(commonDataPath))
            {
                Console.WriteLine($"❌ 找不到文件: {commonDataPath}");
                return;
            }
            
            string jsonContent = File.ReadAllText(commonDataPath);
            Console.WriteLine($"✅ 成功读取CommonData.json，大小: {jsonContent.Length} 字符");
            
            // 使用GameJsonContext反序列化
            var options = GameJsonContext.GetDefaultOptions();
            var commonData = JsonSerializer.Deserialize<CommonData>(jsonContent, options);
            
            if (commonData == null)
            {
                Console.WriteLine("❌ CommonData反序列化失败");
                return;
            }
            
            Console.WriteLine("✅ CommonData反序列化成功");
            
            // 检查AllIdealTendencyKinds
            if (commonData.AllIdealTendencyKinds == null)
            {
                Console.WriteLine("❌ AllIdealTendencyKinds为null");
                return;
            }
            
            Console.WriteLine($"✅ AllIdealTendencyKinds不为null，包含 {commonData.AllIdealTendencyKinds.Count} 个项目");
            
            // 检查每个IdealTendencyKind
            int validCount = 0;
            foreach (var item in commonData.AllIdealTendencyKinds.GetList())
            {
                if (item is GameObjects.PersonDetail.IdealTendencyKind itk)
                {
                    validCount++;
                    Console.WriteLine($"  IdealTendencyKind {itk.ID}: {itk.Name}, Offset: {itk.Offset}");
                }
                else
                {
                    Console.WriteLine($"  ❌ 项目不是IdealTendencyKind类型: {item?.GetType().Name ?? "null"}");
                }
            }
            
            Console.WriteLine($"✅ 有效的IdealTendencyKind数量: {validCount}");
            
            if (validCount > 0)
            {
                Console.WriteLine("🎉 IdealTendencyKind反序列化修复成功！");
            }
            else
            {
                Console.WriteLine("❌ IdealTendencyKind反序列化仍有问题");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}