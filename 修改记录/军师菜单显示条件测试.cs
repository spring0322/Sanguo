// 军师菜单显示条件测试代码
// 用于诊断为什么军师菜单不显示

using System;
using GameObjects;
using GameManager;

public class AdvisorMenuTest
{
    public static void TestAdvisorMenuConditions(Architecture architecture)
    {
        Console.WriteLine("=== 军师菜单显示条件测试 ===");
        Console.WriteLine($"测试城池: {architecture.Name}");
        
        if (architecture.BelongedFaction == null)
        {
            Console.WriteLine("❌ 城池没有所属势力");
            return;
        }
        
        var faction = architecture.BelongedFaction;
        Console.WriteLine($"所属势力: {faction.Name}");
        
        // 测试基础条件
        Console.WriteLine("\n--- 基础条件检查 ---");
        Console.WriteLine($"君主: {faction.Leader?.Name ?? "无"}");
        Console.WriteLine($"君主被俘: {faction.Leader?.BelongedCaptive != null}");
        Console.WriteLine($"是玩家势力: {Session.Current.Scenario.IsPlayer(faction)}");
        
        // 测试当前军师状态
        Console.WriteLine("\n--- 当前军师状态 ---");
        Console.WriteLine($"军师ID: {faction.AdvisorID}");
        Console.WriteLine($"军师对象: {faction.Advisor?.Name ?? "无"}");
        Console.WriteLine($"HasAdvisor(): {architecture.HasAdvisor()}");
        
        // 测试候选人
        Console.WriteLine("\n--- 候选人检查 ---");
        var candidates = faction.AdvisorCandicate;
        Console.WriteLine($"候选人数量: {candidates.Count}");
        
        if (candidates.Count > 0)
        {
            Console.WriteLine("候选人列表:");
            foreach (Person p in candidates)
            {
                Console.WriteLine($"  - {p.Name} (智力:{p.Intelligence}, 可用:{p.Available}, 存活:{p.Alive})");
            }
        }
        else
        {
            Console.WriteLine("没有合适的候选人，检查原因:");
            
            // 详细检查所有人物
            Console.WriteLine("\n所有人物状态:");
            foreach (Person p in faction.Persons)
            {
                bool isLeader = p == faction.Leader;
                bool isAdvisor = p == faction.Advisor;
                bool isAvailable = p.Available;
                bool isAlive = p.Alive;
                bool notCaptive = p.BelongedCaptive == null;
                bool notInTroop = p.LocationTroop == null;
                bool smartEnough = p.Intelligence >= 70;
                
                Console.WriteLine($"  {p.Name}:");
                Console.WriteLine($"    智力: {p.Intelligence} (需要>=70: {smartEnough})");
                Console.WriteLine($"    是君主: {isLeader}");
                Console.WriteLine($"    是军师: {isAdvisor}");
                Console.WriteLine($"    可用: {isAvailable}");
                Console.WriteLine($"    存活: {isAlive}");
                Console.WriteLine($"    未被俘: {notCaptive}");
                Console.WriteLine($"    不在部队: {notInTroop}");
                
                bool qualified = !isLeader && !isAdvisor && isAvailable && isAlive && 
                               notCaptive && notInTroop && smartEnough;
                Console.WriteLine($"    合格: {qualified}");
                Console.WriteLine();
            }
        }
        
        // 测试最终条件
        Console.WriteLine("\n--- 最终条件结果 ---");
        bool canAppoint = architecture.CanAppointAdvisor();
        Console.WriteLine($"CanAppointAdvisor(): {canAppoint}");
        
        if (!canAppoint)
        {
            Console.WriteLine("❌ 任命菜单不会显示");
            Console.WriteLine("可能原因:");
            if (faction.Leader?.BelongedCaptive != null)
                Console.WriteLine("  - 君主被俘虏");
            if (candidates.Count == 0)
                Console.WriteLine("  - 没有合适的候选人（智力>=70且可用）");
        }
        else
        {
            Console.WriteLine("✅ 任命菜单应该显示");
        }
        
        Console.WriteLine($"CanAppointAdvisorOrHasAdvisor(): {architecture.CanAppointAdvisorOrHasAdvisor()}");
    }
    
    public static void TestAllPlayerArchitectures()
    {
        Console.WriteLine("=== 测试所有玩家城池 ===");
        
        if (Session.Current?.Scenario?.CurrentPlayer == null)
        {
            Console.WriteLine("❌ 没有当前玩家");
            return;
        }
        
        var player = Session.Current.Scenario.CurrentPlayer;
        Console.WriteLine($"当前玩家: {player.Name}");
        
        foreach (Architecture arch in player.Architectures)
        {
            Console.WriteLine($"\n--- 测试城池: {arch.Name} ---");
            TestAdvisorMenuConditions(arch);
        }
    }
}

// 使用方法：
// 在游戏中调用 AdvisorMenuTest.TestAllPlayerArchitectures() 来诊断问题