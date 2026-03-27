using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameObjects.Commands;

public enum ArbitrationConflictType : byte
{
    None = 0,
    Cell = 1,
    EdgeSwap = 2,
    EdgeCycle = 3
}

public readonly record struct ArbitrationConflictInfo(
    ArbitrationConflictType Type,
    Guid WinnerTroopId
);

public sealed class DeterministicArbitrator
{
    private readonly Dictionary<int, int> _winnerIndexByCell;
    private readonly Dictionary<ulong, int> _winnerIndexByDirectedEdge;
    private readonly Dictionary<Guid, ArbitrationConflictInfo> _conflictByTroopId;
    private readonly Dictionary<int, int> _sourceIndexByCell;
    private readonly List<ExecutionCommand> _cellAccepted;
    private readonly List<int> _pathStack;
    private readonly Dictionary<int, int> _pathOrderByIndex;
    private readonly List<byte> _visitState;
    private readonly List<byte> _cycleRejectFlags;

    public DeterministicArbitrator(int capacity = 256)
    {
        _winnerIndexByCell = new Dictionary<int, int>(Math.Max(64, capacity));
        _winnerIndexByDirectedEdge = new Dictionary<ulong, int>(Math.Max(64, capacity));
        _conflictByTroopId = new Dictionary<Guid, ArbitrationConflictInfo>(Math.Max(64, capacity));
        _sourceIndexByCell = new Dictionary<int, int>(Math.Max(64, capacity));
        _cellAccepted = new List<ExecutionCommand>(Math.Max(64, capacity));
        _pathStack = new List<int>(Math.Max(64, capacity));
        _pathOrderByIndex = new Dictionary<int, int>(Math.Max(64, capacity));
        _visitState = new List<byte>(Math.Max(64, capacity));
        _cycleRejectFlags = new List<byte>(Math.Max(64, capacity));
    }

    public void ResolveBatch(
        IReadOnlyList<ExecutionCommand> proposals,
        List<ExecutionCommand> accepted,
        List<ExecutionCommand> rejected)
    {
        if (proposals == null) throw new ArgumentNullException(nameof(proposals));
        if (accepted == null) throw new ArgumentNullException(nameof(accepted));
        if (rejected == null) throw new ArgumentNullException(nameof(rejected));

        accepted.Clear();
        rejected.Clear();
        _winnerIndexByCell.Clear();
        _winnerIndexByDirectedEdge.Clear();
        _conflictByTroopId.Clear();
        _sourceIndexByCell.Clear();
        _cellAccepted.Clear();

        for (int i = 0; i < proposals.Count; i++)
        {
            ExecutionCommand proposal = proposals[i];
            if (!proposal.IsSpatialConflictAction)
            {
                _cellAccepted.Add(proposal);
                continue;
            }

            Point spatialPosition = proposal.ResolveSpatialPosition();
            if (spatialPosition.X < 0 || spatialPosition.Y < 0)
            {
                _cellAccepted.Add(proposal);
                continue;
            }

            int cellKey = proposal.ResolveCellKey();
            if (!_winnerIndexByCell.TryGetValue(cellKey, out int winnerIndex))
            {
                _winnerIndexByCell[cellKey] = _cellAccepted.Count;
                _cellAccepted.Add(proposal);
                continue;
            }

            ExecutionCommand winner = _cellAccepted[winnerIndex];
            if (ShouldReplaceWinner(in proposal, in winner))
            {
                rejected.Add(winner);
                RegisterConflict(in winner, ArbitrationConflictType.Cell, in proposal);
                _cellAccepted[winnerIndex] = proposal;
            }
            else
            {
                rejected.Add(proposal);
                RegisterConflict(in proposal, ArbitrationConflictType.Cell, in winner);
            }
        }

        _winnerIndexByCell.Clear();

        for (int i = 0; i < _cellAccepted.Count; i++)
        {
            ExecutionCommand proposal = _cellAccepted[i];
            if (!proposal.HasImmediateStep || !proposal.TryResolveDirectedEdgeKey(out ulong directedEdgeKey))
            {
                accepted.Add(proposal);
                continue;
            }

            ulong reverseEdgeKey = ExecutionCommand.PackEdgeKey(
                proposal.ResolveSpatialPosition(),
                proposal.SourcePosition);

            if (!_winnerIndexByDirectedEdge.TryGetValue(reverseEdgeKey, out int winnerIndex))
            {
                _winnerIndexByDirectedEdge[directedEdgeKey] = accepted.Count;
                accepted.Add(proposal);
                continue;
            }

            ExecutionCommand winner = accepted[winnerIndex];
            if (ShouldReplaceWinner(in proposal, in winner))
            {
                rejected.Add(winner);
                RegisterConflict(in winner, ArbitrationConflictType.EdgeSwap, in proposal);
                RemoveDirectedEdgeClaim(in winner, winnerIndex);
                accepted[winnerIndex] = proposal;
                _winnerIndexByDirectedEdge[directedEdgeKey] = winnerIndex;
            }
            else
            {
                rejected.Add(proposal);
                RegisterConflict(in proposal, ArbitrationConflictType.EdgeSwap, in winner);
            }
        }

        RejectMultiNodeEdgeCycles(accepted, rejected);

        _winnerIndexByDirectedEdge.Clear();
        _sourceIndexByCell.Clear();
        _pathStack.Clear();
        _pathOrderByIndex.Clear();
        _cellAccepted.Clear();
    }

    public bool TryGetConflict(Guid troopId, out ArbitrationConflictInfo conflictInfo)
    {
        return _conflictByTroopId.TryGetValue(troopId, out conflictInfo);
    }

    private static bool ShouldReplaceWinner(in ExecutionCommand challenger, in ExecutionCommand incumbent)
    {
        int priorityDelta = challenger.Priority - incumbent.Priority;
        if (priorityDelta != 0)
        {
            return priorityDelta > 0;
        }

        int issuedTickDelta = incumbent.IssuedTick - challenger.IssuedTick;
        if (issuedTickDelta != 0)
        {
            return issuedTickDelta > 0;
        }

        uint challengerTieBreaker = BuildTieBreaker(challenger);
        uint incumbentTieBreaker = BuildTieBreaker(incumbent);
        if (challengerTieBreaker != incumbentTieBreaker)
        {
            return challengerTieBreaker > incumbentTieBreaker;
        }

        return challenger.TroopId.CompareTo(incumbent.TroopId) < 0;
    }

    private static uint BuildTieBreaker(in ExecutionCommand command)
    {
        Point spatialPosition = command.ResolveSpatialPosition();
        Point sourcePosition = command.SourcePosition;
        uint hash = 2166136261u;
        hash = Hash(hash, unchecked((uint)command.CommitTick));
        hash = Hash(hash, unchecked((uint)command.SourceFactionId));
        hash = Hash(hash, unchecked((uint)command.SourceLegionId));
        hash = Hash(hash, unchecked((uint)sourcePosition.X));
        hash = Hash(hash, unchecked((uint)sourcePosition.Y));
        hash = Hash(hash, unchecked((uint)spatialPosition.X));
        hash = Hash(hash, unchecked((uint)spatialPosition.Y));
        hash = Hash(hash, unchecked((uint)command.TroopId.GetHashCode()));
        hash = Hash(hash, unchecked((uint)command.ActionKind));
        return Mix(hash);
    }

    private static uint Hash(uint seed, uint value)
    {
        return (seed ^ value) * 16777619u;
    }

    private static uint Mix(uint x)
    {
        x ^= x >> 16;
        x *= 0x7FEB352Du;
        x ^= x >> 15;
        x *= 0x846CA68Bu;
        x ^= x >> 16;
        return x;
    }

    private void RemoveDirectedEdgeClaim(in ExecutionCommand command, int winnerIndex)
    {
        if (!command.TryResolveDirectedEdgeKey(out ulong directedEdgeKey))
        {
            return;
        }

        if (_winnerIndexByDirectedEdge.TryGetValue(directedEdgeKey, out int existingIndex) &&
            existingIndex == winnerIndex)
        {
            _winnerIndexByDirectedEdge.Remove(directedEdgeKey);
        }
    }

    private void RegisterConflict(
        in ExecutionCommand loser,
        ArbitrationConflictType conflictType,
        in ExecutionCommand winner)
    {
        _conflictByTroopId[loser.TroopId] = new ArbitrationConflictInfo(conflictType, winner.TroopId);
    }

    private void RegisterConflict(in ExecutionCommand loser, ArbitrationConflictType conflictType)
    {
        _conflictByTroopId[loser.TroopId] = new ArbitrationConflictInfo(conflictType, Guid.Empty);
    }

    private void RejectMultiNodeEdgeCycles(List<ExecutionCommand> accepted, List<ExecutionCommand> rejected)
    {
        if (accepted.Count == 0)
        {
            return;
        }

        _sourceIndexByCell.Clear();
        for (int i = 0; i < accepted.Count; i++)
        {
            ExecutionCommand command = accepted[i];
            if (!command.HasImmediateStep)
            {
                continue;
            }

            int sourceCellKey = PackCellKey(command.SourcePosition);
            if (!_sourceIndexByCell.ContainsKey(sourceCellKey))
            {
                _sourceIndexByCell[sourceCellKey] = i;
            }
        }

        if (_sourceIndexByCell.Count == 0)
        {
            return;
        }

        EnsureScratchState(accepted.Count);

        for (int i = 0; i < accepted.Count; i++)
        {
            if (_visitState[i] != 0)
            {
                continue;
            }

            if (!accepted[i].HasImmediateStep)
            {
                _visitState[i] = 2;
                continue;
            }

            TracePathForCycle(i, accepted);
        }

        bool hasCycleReject = false;
        for (int i = 0; i < accepted.Count; i++)
        {
            if (_cycleRejectFlags[i] != 0)
            {
                hasCycleReject = true;
                break;
            }
        }

        if (!hasCycleReject)
        {
            return;
        }

        for (int i = accepted.Count - 1; i >= 0; i--)
        {
            if (_cycleRejectFlags[i] == 0)
            {
                continue;
            }

            ExecutionCommand loser = accepted[i];
            rejected.Add(loser);
            RegisterConflict(in loser, ArbitrationConflictType.EdgeCycle);
            accepted.RemoveAt(i);
        }
    }

    private void TracePathForCycle(int startIndex, List<ExecutionCommand> accepted)
    {
        _pathStack.Clear();
        _pathOrderByIndex.Clear();

        int currentIndex = startIndex;
        while (currentIndex >= 0 && currentIndex < accepted.Count)
        {
            ExecutionCommand current = accepted[currentIndex];
            if (!current.HasImmediateStep)
            {
                break;
            }

            byte state = _visitState[currentIndex];
            if (state == 2)
            {
                break;
            }

            if (state == 1)
            {
                if (_pathOrderByIndex.TryGetValue(currentIndex, out int cycleStart))
                {
                    int cycleLength = _pathStack.Count - cycleStart;
                    if (cycleLength > 2)
                    {
                        for (int i = cycleStart; i < _pathStack.Count; i++)
                        {
                            _cycleRejectFlags[_pathStack[i]] = 1;
                        }
                    }
                }

                break;
            }

            _visitState[currentIndex] = 1;
            _pathOrderByIndex[currentIndex] = _pathStack.Count;
            _pathStack.Add(currentIndex);

            Point destination = current.ResolveSpatialPosition();
            int destinationCellKey = PackCellKey(destination);
            if (!_sourceIndexByCell.TryGetValue(destinationCellKey, out int nextIndex))
            {
                break;
            }

            currentIndex = nextIndex;
        }

        for (int i = 0; i < _pathStack.Count; i++)
        {
            _visitState[_pathStack[i]] = 2;
        }
    }

    private void EnsureScratchState(int count)
    {
        while (_visitState.Count < count)
        {
            _visitState.Add(0);
        }

        while (_cycleRejectFlags.Count < count)
        {
            _cycleRejectFlags.Add(0);
        }

        for (int i = 0; i < count; i++)
        {
            _visitState[i] = 0;
            _cycleRejectFlags[i] = 0;
        }
    }

    private static int PackCellKey(Point position)
    {
        return (position.X << 16) ^ (position.Y & 0xFFFF);
    }
}
