using System;
using System.Diagnostics;
using GameObjects;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// WegoEngine 单元测试工具类
/// 日期：2026-03-16
/// 用途：验证 Command Buffer 架构的基础功能
/// </summary>
public static class WegoEngineTests
{
    /// <summary>
    /// 测试 1：空场景测试
    /// 验证：无部队时不崩溃
    /// </summary>
    public static bool Test_EmptyScenario()
    {
        try
        {
            Debug.WriteLine("[WegoEngineTests] 测试 1：空场景测试");
            
            var engine = new WegoEngine();
            engine.Update();
            
            Debug.WriteLine($"[WegoEngineTests] ✅ 空场景测试通过");
            Debug.WriteLine($"  - 处理指令数: 移动={engine.ProcessedMoveCommands}, 战斗={engine.ProcessedAttackCommands}");
            
            return engine.ProcessedMoveCommands == 0 && 
                   engine.ProcessedAttackCommands == 0 &&
                   engine.ProcessedStratagemCommands == 0 &&
                   engine.ProcessedSiegeCommands == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WegoEngineTests] ❌ 空场景测试失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 测试 2：单部队移动测试
    /// 验证：单个部队的移动指令能正确处理
    /// </summary>
    public static bool Test_SingleTroopMove(Troop troop)
    {
        // 🔥 ANTI-BAND-AID：测试代码必须 Fail Fast
        // 日期：2026-03-16
        // 原因：troop 为 null 说明测试参数错误，应该抛出异常而不是静默跳过
        if (troop == null)
        {
            throw new ArgumentNullException(nameof(troop), "测试参数不能为 null");
        }
        
        try
        {
            Debug.WriteLine("[WegoEngineTests] 测试 2：单部队移动测试");
            Debug.WriteLine($"  - 部队: {troop.DisplayName}");
            Debug.WriteLine($"  - 初始位置: {troop.Position}");
            Debug.WriteLine($"  - 命令: {troop.Command}");
            Debug.WriteLine($"  - 已操作: {troop.Operated}");
            
            var engine = new WegoEngine();
            engine.AddTroop(troop);
            
            // 记录初始状态
            var initialPosition = troop.Position;
            var initialCommand = troop.Command;
            
            engine.Update();
            
            Debug.WriteLine($"[WegoEngineTests] ✅ 单部队移动测试完成");
            Debug.WriteLine($"  - 处理指令数: 移动={engine.ProcessedMoveCommands}, 战斗={engine.ProcessedAttackCommands}");
            Debug.WriteLine($"  - 最终位置: {troop.Position}（注意：测试模式不修改游戏状态）");
            
            // 验证：如果部队有移动命令且已操作，应该处理至少 1 条移动指令
            if (troop.Operated && troop.Command == TroopCommand.Move)
            {
                return engine.ProcessedMoveCommands >= 1;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WegoEngineTests] ❌ 单部队移动测试失败: {ex.Message}");
            Debug.WriteLine($"  堆栈: {ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// 测试 3：多部队场景测试
    /// 验证：多个部队能正确处理
    /// </summary>
    public static bool Test_MultipleTroops(GameObjectList troops)
    {
        // 🔥 ANTI-BAND-AID：测试代码必须 Fail Fast
        // 日期：2026-03-16
        if (troops == null)
        {
            throw new ArgumentNullException(nameof(troops), "测试参数不能为 null");
        }
        
        if (troops.Count == 0)
        {
            throw new ArgumentException("测试参数不能为空集合", nameof(troops));
        }
        
        try
        {
            Debug.WriteLine("[WegoEngineTests] 测试 3：多部队场景测试");
            Debug.WriteLine($"  - 部队数量: {troops.Count}");
            
            var engine = new WegoEngine();
            
            int operatedCount = 0;
            int moveCommandCount = 0;
            int attackCommandCount = 0;
            
            foreach (Troop troop in troops)
            {
                // 🔥 ANTI-BAND-AID：集合中不应该有 null 元素
                // 如果有，说明数据损坏，应该抛出异常
                if (troop == null)
                {
                    throw new InvalidOperationException("部队集合中存在 null 元素，数据损坏");
                }
                
                engine.AddTroop(troop);
                
                if (troop.Operated)
                {
                    operatedCount++;
                    
                    if (troop.Command == TroopCommand.Move)
                        moveCommandCount++;
                    else if (troop.Command == TroopCommand.Attack)
                        attackCommandCount++;
                }
            }
            
            Debug.WriteLine($"  - 已操作部队: {operatedCount}");
            Debug.WriteLine($"  - 移动命令: {moveCommandCount}");
            Debug.WriteLine($"  - 攻击命令: {attackCommandCount}");
            
            engine.Update();
            
            Debug.WriteLine($"[WegoEngineTests] ✅ 多部队场景测试完成");
            Debug.WriteLine($"  - 处理指令数: 移动={engine.ProcessedMoveCommands}, 战斗={engine.ProcessedAttackCommands}, 计略={engine.ProcessedStratagemCommands}, 攻城={engine.ProcessedSiegeCommands}");
            
            // 验证：处理的指令数应该 <= 已操作的部队数
            int totalProcessed = engine.ProcessedMoveCommands + 
                                engine.ProcessedAttackCommands + 
                                engine.ProcessedStratagemCommands + 
                                engine.ProcessedSiegeCommands;
            
            return totalProcessed <= operatedCount;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WegoEngineTests] ❌ 多部队场景测试失败: {ex.Message}");
            Debug.WriteLine($"  堆栈: {ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// 测试 4：性能测试
    /// 验证：大量部队时的性能
    /// </summary>
    public static bool Test_Performance(GameObjectList troops, int iterations = 10)
    {
        // 🔥 ANTI-BAND-AID：测试代码必须 Fail Fast
        // 日期：2026-03-16
        if (troops == null)
        {
            throw new ArgumentNullException(nameof(troops), "测试参数不能为 null");
        }
        
        if (troops.Count == 0)
        {
            throw new ArgumentException("测试参数不能为空集合", nameof(troops));
        }
        
        try
        {
            Debug.WriteLine("[WegoEngineTests] 测试 4：性能测试");
            Debug.WriteLine($"  - 部队数量: {troops.Count}");
            Debug.WriteLine($"  - 迭代次数: {iterations}");
            
            var engine = new WegoEngine();
            
            foreach (Troop troop in troops)
            {
                // 🔥 ANTI-BAND-AID：集合中不应该有 null 元素
                if (troop == null)
                {
                    throw new InvalidOperationException("部队集合中存在 null 元素，数据损坏");
                }
                
                engine.AddTroop(troop);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                engine.Update();
            }
            
            stopwatch.Stop();
            
            double avgMs = stopwatch.Elapsed.TotalMilliseconds / iterations;
            
            Debug.WriteLine($"[WegoEngineTests] ✅ 性能测试完成");
            Debug.WriteLine($"  - 总耗时: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            Debug.WriteLine($"  - 平均耗时: {avgMs:F2}ms/回合");
            Debug.WriteLine($"  - 每部队耗时: {avgMs / troops.Count:F4}ms");
            
            // 验证：平均耗时应该 < 16.67ms（60fps）
            return avgMs < 16.67;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WegoEngineTests] ❌ 性能测试失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests(GameScenario scenario)
    {
        // 🔥 ANTI-BAND-AID：测试代码必须 Fail Fast
        // 日期：2026-03-16
        if (scenario == null)
        {
            throw new ArgumentNullException(nameof(scenario), "测试参数不能为 null");
        }
        
        Debug.WriteLine("========================================");
        Debug.WriteLine("[WegoEngineTests] 开始运行所有测试");
        Debug.WriteLine("========================================");
        
        int passed = 0;
        int total = 0;
        
        // 测试 1：空场景
        total++;
        if (Test_EmptyScenario())
            passed++;
        
        // 测试 2：单部队（如果有）
        if (scenario.Troops != null && scenario.Troops.Count > 0)
        {
            total++;
            var firstTroop = scenario.Troops[0] as Troop;
            
            // 🔥 ANTI-BAND-AID：集合中不应该有 null 元素
            if (firstTroop == null)
            {
                throw new InvalidOperationException("部队集合中第一个元素为 null，数据损坏");
            }
            
            if (Test_SingleTroopMove(firstTroop))
                passed++;
        }
        
        // 测试 3：多部队
        if (scenario.Troops != null && scenario.Troops.Count > 0)
        {
            total++;
            if (Test_MultipleTroops(scenario.Troops))
                passed++;
        }
        
        // 测试 4：性能测试
        if (scenario.Troops != null && scenario.Troops.Count > 0)
        {
            total++;
            if (Test_Performance(scenario.Troops, 10))
                passed++;
        }
        
        Debug.WriteLine("========================================");
        Debug.WriteLine($"[WegoEngineTests] 测试完成: {passed}/{total} 通过");
        Debug.WriteLine("========================================");
    }
}
