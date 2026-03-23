using System;
using System.IO;
using System.Linq;
using GameObjects;
using GameObjects.Influences;
using GameObjects.Influences.InfluenceKindPack;
using GameManager;
using GameGlobal;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("========================================");
        Console.WriteLine("诊断 InfluenceKind 类型问题");
        Console.WriteLine("========================================\n");

        try
        {
            // 初始化游戏
            Session.Current = new Session();
            Session.Current.Scenario = new GameScenario();
            Session.Current.Scenario.GameCommonData = new CommonData();
            
            // 加载CommonData
            string commonDataPath = Path.Combine(Environment.CurrentDirectory, "Content", "Data", "Common", "CommonData.json");
            if (!File.Exists(commonDataPath))
            {
                Console.WriteLine($"❌ 找不到 CommonData.json: {commonDataPath}");
                return;
            }

            Console.WriteLine($"✓ 加载 CommonData.json...");
            Session.Current.Scenario.GameCommonData.InitializeGameDelegates();
            Session.Current.Scenario.GameCommonData.LoadFromString(File.ReadAllText(commonDataPath));
            
            // 查找影响1014（行政中心）
            var influence = Session.Current.Scenario.GameCommonData.AllInfluences.GetInfluence(1014);
            if (influence == null)
            {
                Console.WriteLine("❌ 找不到影响 1014（行政中心）");
                return;
            }

            Console.WriteLine($"\n影响信息:");
            Console.WriteLine($"  ID: {influence.ID}");
            Console.WriteLine($"  Name: {influence.Name}");
            Console.WriteLine($"  Parameter: '{influence.Parameter}'");
            Console.WriteLine($"  Kind: {influence.Kind?.GetType().FullName ?? "null"}");
            Console.WriteLine($"  Kind.ID: {influence.Kind?.ID ?? -1}");
            Console.WriteLine($"  Kind.Name: {influence.Kind?.Name ?? "null"}");
            
            // 检查Kind是否是InfluenceKind1004
            if (influence.Kind != null)
            {
                bool isKind1004 = influence.Kind is InfluenceKind1004;
                Console.WriteLine($"  是 InfluenceKind1004? {isKind1004}");
                
                if (!isKind1004)
                {
                    Console.WriteLine($"\n❌ 问题：Kind 不是 InfluenceKind1004 实例！");
                    Console.WriteLine($"   实际类型: {influence.Kind.GetType().FullName}");
                    Console.WriteLine($"   这就是为什么 InitializeParameter 没有被调用！");
                }
                else
                {
                    Console.WriteLine($"\n✓ Kind 是正确的 InfluenceKind1004 实例");
                    
                    // 测试InitializeParameter
                    Console.WriteLine($"\n测试 InitializeParameter:");
                    influence.Kind.InitializeParameter(influence.Parameter);
                    Console.WriteLine($"  调用完成");
                }
            }
            
            // 检查所有统治上限相关的影响
            Console.WriteLine($"\n\n检查所有 Kind ID 为 1004 的影响:");
            var allInfluences = Session.Current.Scenario.GameCommonData.AllInfluences.GetInfluenceList();
            foreach (Influence inf in allInfluences)
            {
                if (inf.Kind?.ID == 1004)
                {
                    bool isCorrectType = inf.Kind is InfluenceKind1004;
                    Console.WriteLine($"  影响 {inf.ID} ({inf.Name}): {inf.Kind.GetType().Name}, 正确类型? {isCorrectType}");
                }
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
