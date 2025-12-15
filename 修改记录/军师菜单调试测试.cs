// 军师菜单调试测试代码
// 这个代码可以帮助诊断为什么军师菜单没有显示

using System;
using GameObjects;
using GameGlobal;

public class AdvisorMenuDebugTest
{
    public static void TestAdvisorMenuConditions(Architecture architecture)
    {
        Console.WriteLine("=== 军师菜单显示条件测试 ===");
        
        if (architecture == null)
        {
            Console.WriteLine("错误: 建筑为空");
            return;
        }
        
        if (architecture.BelongedFaction == null)
        {
            Console.WriteLine("错误: 建筑没有所属势力");
            return;
        }
        
        Faction faction = architecture.BelongedFaction;
        
        Console.WriteLine($"势力: {faction.Name}");
        Console.WriteLine($"君主: {faction.Leader?.Name ?? "无"}");
        Console.WriteLine($"当前军师: {faction.Advisor?.Name ?? "无"}");
        Console.WriteLine($"军师ID: {faction.AdvisorID}");
        
        // 测试CanAppointAdvisor条件
        bool canAppoint = architecture.CanAppointAdvisor();
        Console.WriteLine($"CanAppointAdvisor: {canAppoint}");
        
        // 测试HasAdvisor条件
        bool hasAdvisor = architecture.HasAdvisor();
        Console.WriteLine($"HasAdvisor: {hasAdvisor}");
        
        // 检查候选人
        var candidates = faction.AdvisorCandicate;
        Console.WriteLine($"候选人数量: {candidates.Count}");
        
        if (candidates.Count > 0)
        {
            Console.WriteLine("候选人列表:");
            foreach (Person p in candidates)
            {
                Console.WriteLine($"  - {p.Name} (智力: {p.Intelligence})");
            }
        }
        
        // 检查君主是否被俘
        bool leaderCaptured = faction.Leader?.BelongedCaptive != null;
        Console.WriteLine($"君主被俘: {leaderCaptured}");
        
        // 检查是否是玩家势力
        bool isPlayer = Session.Current.Scenario.IsPlayer(faction);
        Console.WriteLine($"是玩家势力: {isPlayer}");
        
        Console.WriteLine("=== 测试结束 ===");
    }
    
    // 在MainGameScreen中调用这个方法来测试
    public static void TestInGame(MainGameScreen screen)
    {
        if (screen.CurrentArchitecture != null)
        {
            TestAdvisorMenuConditions(screen.CurrentArchitecture);
        }
        else
        {
            Console.WriteLine("当前没有选中建筑");
        }
    }
}