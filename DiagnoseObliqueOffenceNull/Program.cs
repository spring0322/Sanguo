using GameObjects;
using GameManager;
using System.Diagnostics;

namespace DiagnoseObliqueOffenceNull;

/// <summary>
/// 诊断 Troop.ObliqueOffence 空引用的根本原因
/// 目标：找出为什么 Army.Kind 会是 null，而不是简单地添加空值检查
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 诊断 Troop.ObliqueOffence 空引用根本原因 ===\n");

        // 模拟游戏初始化
        if (!InitializeGame())
        {
            Console.WriteLine("❌ 游戏初始化失败");
            return;
        }

        var scenario = Session.Current.Scenario;
        if (scenario == null)
        {
            Console.WriteLine("❌ Scenario 未加载");
            return;
        }

        Console.WriteLine($"✅ 场景加载成功: {scenario.Title}");
        Console.WriteLine($"   部队数量: {scenario.Troops.Count}");
        Console.WriteLine($"   军队数量: {scenario.Militaries.Count}");
        Console.WriteLine($"   兵种数量: {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}\n");

        // 检查所有部队的 Army.Kind 状态
        DiagnoseTroopArmyKind(scenario);

        // 检查 Military 的 Kind 链接状态
        DiagnoseMilitaryKind(scenario);

        // 检查 MilitaryKind 表的完整性
        DiagnoseMilitaryKindTable(scenario);

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    static bool InitializeGame()
    {
        try
        {
            // 初始化 Session
            Session.Current = new Session();
            
            // 加载场景（使用最近的存档或默认场景）
            string savePath = @"Content\Save\AutoSave.sav";
            if (File.Exists(savePath))
            {
                Console.WriteLine($"正在加载存档: {savePath}");
                // 这里需要实际的加载逻辑
                // Session.Current.LoadGame(savePath);
                return false; // 暂时返回 false，因为需要实际的加载实现
            }
            else
            {
                Console.WriteLine("未找到存档文件");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化失败: {ex.Message}");
            return false;
        }
    }

    static void DiagnoseTroopArmyKind(GameScenario scenario)
    {
        Console.WriteLine("--- 检查 Troop.Army.Kind 状态 ---");

        int totalTroops = 0;
        int troopsWithNullArmy = 0;
        int troopsWithNullKind = 0;
        int troopsWithValidKind = 0;

        foreach (Troop troop in scenario.Troops)
        {
            totalTroops++;

            if (troop.Army == null)
            {
                troopsWithNullArmy++;
                Console.WriteLine($"  ⚠️ 部队 ID={troop.ID}, Name={troop.Name}: Army 为 null, militaryID={troop.militaryID}");
                continue;
            }

            if (troop.Army.Kind == null)
            {
                troopsWithNullKind++;
                Console.WriteLine($"  ⚠️ 部队 ID={troop.ID}, Name={troop.Name}: Army.Kind 为 null");
                Console.WriteLine($"     Army ID={troop.Army.ID}, KindID={troop.Army.KindID}");
                
                // 尝试查找对应的 MilitaryKind
                var kind = scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(troop.Army.KindID);
                if (kind == null)
                {
                    Console.WriteLine($"     ❌ MilitaryKind 表中找不到 KindID={troop.Army.KindID}");
                }
                else
                {
                    Console.WriteLine($"     ✅ MilitaryKind 表中存在 KindID={troop.Army.KindID}, Name={kind.Name}");
                    Console.WriteLine($"     🔥 问题：Kind 存在但未链接到 Army！");
                }
            }
            else
            {
                troopsWithValidKind++;
            }
        }

        Console.WriteLine($"\n统计结果:");
        Console.WriteLine($"  总部队数: {totalTroops}");
        Console.WriteLine($"  Army 为 null: {troopsWithNullArmy} ({(double)troopsWithNullArmy / totalTroops * 100:F1}%)");
        Console.WriteLine($"  Army.Kind 为 null: {troopsWithNullKind} ({(double)troopsWithNullKind / totalTroops * 100:F1}%)");
        Console.WriteLine($"  Army.Kind 正常: {troopsWithValidKind} ({(double)troopsWithValidKind / totalTroops * 100:F1}%)");
        Console.WriteLine();
    }

    static void DiagnoseMilitaryKind(GameScenario scenario)
    {
        Console.WriteLine("--- 检查 Military.Kind 链接状态 ---");

        int totalMilitaries = 0;
        int militariesWithNullKind = 0;
        int militariesWithValidKind = 0;

        foreach (Military military in scenario.Militaries)
        {
            totalMilitaries++;

            if (military.Kind == null)
            {
                militariesWithNullKind++;
                Console.WriteLine($"  ⚠️ Military ID={military.ID}, KindID={military.KindID}: Kind 为 null");
                
                // 检查 KindID 是否有效
                var kind = scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(military.KindID);
                if (kind == null)
                {
                    Console.WriteLine($"     ❌ MilitaryKind 表中找不到 KindID={military.KindID}");
                }
                else
                {
                    Console.WriteLine($"     ✅ MilitaryKind 表中存在，但未链接！");
                }
            }
            else
            {
                militariesWithValidKind++;
            }
        }

        Console.WriteLine($"\n统计结果:");
        Console.WriteLine($"  总军队数: {totalMilitaries}");
        Console.WriteLine($"  Kind 为 null: {militariesWithNullKind} ({(double)militariesWithNullKind / totalMilitaries * 100:F1}%)");
        Console.WriteLine($"  Kind 正常: {militariesWithValidKind} ({(double)militariesWithValidKind / totalMilitaries * 100:F1}%)");
        Console.WriteLine();
    }

    static void DiagnoseMilitaryKindTable(GameScenario scenario)
    {
        Console.WriteLine("--- 检查 MilitaryKind 表完整性 ---");

        var kindTable = scenario.GameCommonData.AllMilitaryKinds;
        Console.WriteLine($"  MilitaryKind 总数: {kindTable.MilitaryKinds.Count}");

        // 检查是否有重复的 ID
        var kindIds = kindTable.MilitaryKinds.Select(k => k.ID).ToList();
        var duplicates = kindIds.GroupBy(id => id).Where(g => g.Count() > 1).ToList();
        
        if (duplicates.Any())
        {
            Console.WriteLine($"  ⚠️ 发现重复的 KindID:");
            foreach (var dup in duplicates)
            {
                Console.WriteLine($"     ID={dup.Key}, 重复次数={dup.Count()}");
            }
        }
        else
        {
            Console.WriteLine($"  ✅ 没有重复的 KindID");
        }

        // 列出所有 KindID
        Console.WriteLine($"\n  所有 MilitaryKind ID:");
        foreach (var kind in kindTable.MilitaryKinds.OrderBy(k => k.ID))
        {
            Console.WriteLine($"     ID={kind.ID}, Name={kind.Name}");
        }
        Console.WriteLine();
    }
}
