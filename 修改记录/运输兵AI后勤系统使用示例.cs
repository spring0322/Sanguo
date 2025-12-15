using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI;
using GameManager;

/// <summary>
/// 运输兵AI后勤系统使用示例
/// 展示如何使用AILogisticsHandler进行智能后勤管理
/// </summary>
public class TransportAIUsageExample
{
    /// <summary>
    /// 示例1: 手动执行运输兵AI
    /// </summary>
    public static void Example1_ManualTransportAI()
    {
        Console.WriteLine("=== 示例1: 手动执行运输兵AI ===");

        // 假设我们有一个运输兵部队
        Troop transportTroop = GetExampleTransportTroop();
        if (transportTroop == null)
        {
            Console.WriteLine("没有找到运输兵部队");
            return;
        }

        // 获取战场信息
        var enemies = GetNearbyEnemies(transportTroop);
        var allies = GetNearbyAllies(transportTroop);

        // 执行运输兵AI
        AILogisticsHandler.ExecuteTransportAI(transportTroop, enemies, allies);

        Console.WriteLine("运输兵AI执行完成");
    }

    /// <summary>
    /// 示例2: 自动创建运输兵进行资源调配
    /// </summary>
    public static void Example2_AutoCreateTransportTroops()
    {
        Console.WriteLine("=== 示例2: 自动创建运输兵 ===");

        // 获取当前玩家势力
        Faction playerFaction = Session.Current.Scenario.CurrentPlayer;
        if (playerFaction == null)
        {
            Console.WriteLine("没有找到玩家势力");
            return;
        }

        // 自动创建运输兵进行资源调配
        AILogisticsHandler.AutoCreateTransportTroops(playerFaction);

        Console.WriteLine("自动运输兵创建完成");
    }

    /// <summary>
    /// 示例3: 手动创建运输兵
    /// </summary>
    public static void Example3_ManualCreateTransportTroop()
    {
        Console.WriteLine("=== 示例3: 手动创建运输兵 ===");

        // 获取源据点和目标据点
        var sourceCity = GetExampleSourceCity();
        var targetCity = GetExampleTargetCity();

        if (sourceCity == null || targetCity == null)
        {
            Console.WriteLine("没有找到合适的据点");
            return;
        }

        // 创建运输兵
        int foodToTransport = 2000;
        int fundToTransport = 1000;

        var transportTroop = AILogisticsHandler.CreateTransportTroop(
            sourceCity, 
            targetCity, 
            foodToTransport, 
            fundToTransport
        );

        if (transportTroop != null)
        {
            Console.WriteLine($"成功创建运输兵: {sourceCity.Name} -> {targetCity.Name}");
            Console.WriteLine($"携带资源: 粮食 {foodToTransport}, 资金 {fundToTransport}");
        }
        else
        {
            Console.WriteLine("创建运输兵失败");
        }
    }

    /// <summary>
    /// 示例4: 检查部队是否为运输兵
    /// </summary>
    public static void Example4_CheckTransportTroop()
    {
        Console.WriteLine("=== 示例4: 检查运输兵 ===");

        // 遍历所有部队，检查哪些是运输兵
        foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
        {
            if (troop != null && !troop.Destroyed)
            {
                bool isTransport = AILogisticsHandler.IsTransportTroop(troop);
                if (isTransport)
                {
                    Console.WriteLine($"发现运输兵: {troop.DisplayName} (ID: {troop.ID})");
                    Console.WriteLine($"  位置: ({troop.Position.X}, {troop.Position.Y})");
                    Console.WriteLine($"  携带粮食: {troop.Food}");
                    Console.WriteLine($"  携带资金: {troop.zijin}");
                    
                    if (troop.WillArchitecture != null)
                    {
                        Console.WriteLine($"  目标据点: {troop.WillArchitecture.Name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 示例5: 完整的势力回合集成
    /// </summary>
    public static void Example5_FullFactionTurnIntegration()
    {
        Console.WriteLine("=== 示例5: 完整势力回合集成 ===");

        // 获取AI势力
        Faction aiFaction = GetExampleAIFaction();
        if (aiFaction == null)
        {
            Console.WriteLine("没有找到AI势力");
            return;
        }

        // 执行完整的AI势力回合（包含运输兵AI）
        AIFactionTurnIntegration.RunFactionTurn(aiFaction);

        Console.WriteLine("AI势力回合执行完成");
    }

    // ================= 辅助方法 =================

    /// <summary>
    /// 获取示例运输兵部队
    /// </summary>
    private static Troop GetExampleTransportTroop()
    {
        foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
        {
            if (troop != null && !troop.Destroyed && AILogisticsHandler.IsTransportTroop(troop))
            {
                return troop;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取附近的敌军
    /// </summary>
    private static List<Troop> GetNearbyEnemies(Troop myTroop)
    {
        var enemies = new List<Troop>();
        
        foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
        {
            if (troop != null && !troop.Destroyed && 
                troop.BelongedFaction != myTroop.BelongedFaction &&
                !myTroop.BelongedFaction.IsFriendly(troop.BelongedFaction))
            {
                // 检查距离（这里简化为10格内）
                int distance = Math.Abs(troop.Position.X - myTroop.Position.X) + 
                              Math.Abs(troop.Position.Y - myTroop.Position.Y);
                if (distance <= 10)
                {
                    enemies.Add(troop);
                }
            }
        }
        
        return enemies;
    }

    /// <summary>
    /// 获取附近的友军
    /// </summary>
    private static List<Troop> GetNearbyAllies(Troop myTroop)
    {
        var allies = new List<Troop>();
        
        foreach (Troop troop in myTroop.BelongedFaction.Troops.GetList())
        {
            if (troop != null && !troop.Destroyed && troop != myTroop)
            {
                allies.Add(troop);
            }
        }
        
        return allies;
    }

    /// <summary>
    /// 获取示例源据点（资源丰富）
    /// </summary>
    private static Architecture GetExampleSourceCity()
    {
        Faction playerFaction = Session.Current.Scenario.CurrentPlayer;
        if (playerFaction == null) return null;

        foreach (Architecture arch in playerFaction.Architectures.GetList())
        {
            if (arch != null && !arch.Destroyed && 
                arch.Food > arch.FoodCeiling * 0.8 && 
                arch.Fund > arch.FundCeiling * 0.8)
            {
                return arch;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 获取示例目标据点（资源缺乏）
    /// </summary>
    private static Architecture GetExampleTargetCity()
    {
        Faction playerFaction = Session.Current.Scenario.CurrentPlayer;
        if (playerFaction == null) return null;

        foreach (Architecture arch in playerFaction.Architectures.GetList())
        {
            if (arch != null && !arch.Destroyed && 
                (arch.Food < arch.FoodCeiling * 0.3 || arch.Fund < arch.FundCeiling * 0.3))
            {
                return arch;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 获取示例AI势力
    /// </summary>
    private static Faction GetExampleAIFaction()
    {
        foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
        {
            if (faction != null && !faction.Destroyed && 
                !Session.Current.Scenario.IsPlayer(faction))
            {
                return faction;
            }
        }
        
        return null;
    }
}

/// <summary>
/// 运输兵AI系统测试类
/// </summary>
public class TransportAITester
{
    /// <summary>
    /// 运行所有测试示例
    /// </summary>
    public static void RunAllExamples()
    {
        Console.WriteLine("开始运行运输兵AI系统测试...");
        Console.WriteLine();

        try
        {
            TransportAIUsageExample.Example1_ManualTransportAI();
            Console.WriteLine();

            TransportAIUsageExample.Example2_AutoCreateTransportTroops();
            Console.WriteLine();

            TransportAIUsageExample.Example3_ManualCreateTransportTroop();
            Console.WriteLine();

            TransportAIUsageExample.Example4_CheckTransportTroop();
            Console.WriteLine();

            TransportAIUsageExample.Example5_FullFactionTurnIntegration();
            Console.WriteLine();

            Console.WriteLine("所有测试示例运行完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 性能测试
    /// </summary>
    public static void PerformanceTest()
    {
        Console.WriteLine("=== 运输兵AI性能测试 ===");

        var startTime = DateTime.Now;
        
        // 执行多次AI运算
        for (int i = 0; i < 100; i++)
        {
            TransportAIUsageExample.Example2_AutoCreateTransportTroops();
        }
        
        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        
        Console.WriteLine($"执行100次自动运输兵创建耗时: {duration.TotalMilliseconds}ms");
        Console.WriteLine($"平均每次耗时: {duration.TotalMilliseconds / 100}ms");
    }
}