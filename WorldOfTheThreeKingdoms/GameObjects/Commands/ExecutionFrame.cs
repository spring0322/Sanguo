using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameObjects.Commands;

public enum ExecutionActionKind : byte
{
    None = 0,
    Move = 1,
    Enter = 2,
    AttackTroop = 3,
    AttackArchitecture = 4,
    Stratagem = 5
}

public readonly record struct ExecutionCommand(
    Guid TroopId,
    ExecutionActionKind ActionKind,
    Point TargetPosition,
    Point ConflictPosition,
    Guid TargetTroopId,
    int TargetArchitectureId,
    int Priority,
    int IssuedTick,
    int CommitTick,
    int RiskBudget,
    int SourceFactionId,
    int SourceLegionId)
{
    public bool HasTargetPosition => TargetPosition.X >= 0 && TargetPosition.Y >= 0;
    public bool HasConflictPosition => ConflictPosition.X >= 0 && ConflictPosition.Y >= 0;

    public bool IsSpatialConflictAction =>
        ActionKind is ExecutionActionKind.Move or
            ExecutionActionKind.Enter or
            ExecutionActionKind.AttackTroop or
            ExecutionActionKind.AttackArchitecture;

    public Point ResolveSpatialPosition()
    {
        if (HasConflictPosition)
        {
            return ConflictPosition;
        }

        return TargetPosition;
    }

    public int ResolveCellKey()
    {
        Point position = ResolveSpatialPosition();
        return (position.X << 16) ^ (position.Y & 0xFFFF);
    }
}

public sealed class ExecutionFrame
{
    private readonly List<ExecutionCommand> _commands;

    public int IssuedTick { get; private set; }
    public IReadOnlyList<ExecutionCommand> Commands => _commands;
    public int Count => _commands.Count;

    public ExecutionFrame(int capacity = 512)
    {
        _commands = new List<ExecutionCommand>(Math.Max(64, capacity));
    }

    public void Begin(int issuedTick)
    {
        IssuedTick = issuedTick;
        _commands.Clear();
    }

    public void Add(in ExecutionCommand command)
    {
        _commands.Add(command);
    }

    public void Clear()
    {
        _commands.Clear();
    }
}
