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
    Point SourcePosition,
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
    public bool HasSourcePosition => SourcePosition.X >= 0 && SourcePosition.Y >= 0;
    public bool HasTargetPosition => TargetPosition.X >= 0 && TargetPosition.Y >= 0;
    public bool HasConflictPosition => ConflictPosition.X >= 0 && ConflictPosition.Y >= 0;
    public bool HasImmediateStep =>
        HasSourcePosition &&
        IsSpatialConflictAction &&
        TryResolveDirectedEdgeKey(out _);

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

    public bool TryResolveDirectedEdgeKey(out ulong edgeKey)
    {
        edgeKey = 0UL;

        if (!HasSourcePosition)
        {
            return false;
        }

        Point destination = ResolveSpatialPosition();
        if (destination.X < 0 || destination.Y < 0 || destination == SourcePosition)
        {
            return false;
        }

        edgeKey = PackEdgeKey(SourcePosition, destination);
        return true;
    }

    public bool IsReverseEdgeOf(in ExecutionCommand other)
    {
        if (!TryResolveDirectedEdgeKey(out _) || !other.TryResolveDirectedEdgeKey(out _))
        {
            return false;
        }

        return SourcePosition == other.ResolveSpatialPosition() &&
            ResolveSpatialPosition() == other.SourcePosition;
    }

    public static ulong PackEdgeKey(Point from, Point to)
    {
        return (PackPointKey(from) << 32) | PackPointKey(to);
    }

    private static ulong PackPointKey(Point position)
    {
        return ((ulong)(ushort)position.X << 16) | (ushort)position.Y;
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
