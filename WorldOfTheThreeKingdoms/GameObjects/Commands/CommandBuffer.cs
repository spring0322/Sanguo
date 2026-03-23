using System.Collections.Concurrent;

namespace GameObjects.Commands;

// 🔥 指令缓冲区（承载一回合内所有的行动意图）
// 日期：2026-03-16
// 设计原则：
// 1. 使用 ConcurrentQueue 实现无锁高并发写入
// 2. 决策阶段：多线程并发写入指令
// 3. 结算阶段：单线程顺序读取并执行指令
public class CommandBuffer
{
    // 🔥 移动指令队列
    public ConcurrentQueue<MoveCommand> MoveQueue { get; } = new();
    
    // 🔥 战斗指令队列
    public ConcurrentQueue<AttackCommand> AttackQueue { get; } = new();
    
    // 🔥 计略指令队列
    public ConcurrentQueue<StratagemCommand> StratagemQueue { get; } = new();
    
    // 🔥 攻城指令队列
    public ConcurrentQueue<SiegeCommand> SiegeQueue { get; } = new();
    
    // 🔥 清空所有队列（每回合结束后调用）
    public void Clear()
    {
        MoveQueue.Clear();
        AttackQueue.Clear();
        StratagemQueue.Clear();
        SiegeQueue.Clear();
    }
    
    // 🔥 获取指令总数（用于调试）
    public int TotalCommandCount => 
        MoveQueue.Count + 
        AttackQueue.Count + 
        StratagemQueue.Count + 
        SiegeQueue.Count;
    
    // 🔥 调试：记录所有指令
    #if DEBUG
    public void LogCommands()
    {
        System.Diagnostics.Debug.WriteLine($"[CommandBuffer] 指令总数: {TotalCommandCount}");
        
        foreach (var cmd in MoveQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Move] {cmd.TroopId} -> {cmd.TargetPosition} (优先级:{cmd.Priority})");
        }
        
        foreach (var cmd in AttackQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Attack] {cmd.AttackerId} -> {cmd.TargetId} (伤害:{cmd.Damage})");
        }
        
        foreach (var cmd in StratagemQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Stratagem] {cmd.CasterId} -> {cmd.TargetId} (计略ID:{cmd.StratagemId})");
        }
        
        foreach (var cmd in SiegeQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Siege] {cmd.AttackerId} -> 城池{cmd.ArchitectureId} (伤害:{cmd.Damage})");
        }
    }
    #endif
}
