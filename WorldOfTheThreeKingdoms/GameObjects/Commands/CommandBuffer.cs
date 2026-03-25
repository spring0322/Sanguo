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
    
    // 🔥 入城指令队列
    public ConcurrentQueue<EnterCommand> EnterQueue { get; } = new();
    
    // 🔥 攻击部队指令队列
    public ConcurrentQueue<AttackTroopCommand> AttackTroopQueue { get; } = new();
    
    // 🔥 攻击城池指令队列
    public ConcurrentQueue<AttackArchCommand> AttackArchQueue { get; } = new();
    
    // 🔥 计略指令队列
    public ConcurrentQueue<StratagemCommand> StratagemQueue { get; } = new();
    
    // 🔥 旧版战斗指令队列（保留用于兼容）
    public ConcurrentQueue<AttackCommand> AttackQueue { get; } = new();
    
    // 🔥 旧版攻城指令队列（保留用于兼容）
    public ConcurrentQueue<SiegeCommand> SiegeQueue { get; } = new();
    
    // 🔥 清空所有队列（每回合结束后调用）
    public void Clear()
    {
        MoveQueue.Clear();
        EnterQueue.Clear();
        AttackTroopQueue.Clear();
        AttackArchQueue.Clear();
        StratagemQueue.Clear();
        AttackQueue.Clear();
        SiegeQueue.Clear();
    }
    
    // 🔥 获取指令总数（用于调试）
    public int TotalCommandCount => 
        MoveQueue.Count + 
        EnterQueue.Count +
        AttackTroopQueue.Count +
        AttackArchQueue.Count +
        StratagemQueue.Count + 
        AttackQueue.Count + 
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
        
        foreach (var cmd in EnterQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Enter] {cmd.TroopId} -> 城池{cmd.ArchitectureId} @ {cmd.TargetPosition}");
        }
        
        foreach (var cmd in AttackTroopQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [AttackTroop] {cmd.AttackerId} -> {cmd.TargetTroopId} @ {cmd.OptimalPosition}");
        }
        
        foreach (var cmd in AttackArchQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [AttackArch] {cmd.AttackerId} -> 城池{cmd.ArchitectureId} @ {cmd.SiegePosition}");
        }
        
        foreach (var cmd in StratagemQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Stratagem] {cmd.CasterId} -> {cmd.TargetId} (计略ID:{cmd.StratagemId})");
        }
        
        foreach (var cmd in AttackQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Attack] {cmd.AttackerId} -> {cmd.TargetId} (伤害:{cmd.Damage})");
        }
        
        foreach (var cmd in SiegeQueue)
        {
            System.Diagnostics.Debug.WriteLine($"  [Siege] {cmd.AttackerId} -> 城池{cmd.ArchitectureId} (伤害:{cmd.Damage})");
        }
    }
    #endif
}
