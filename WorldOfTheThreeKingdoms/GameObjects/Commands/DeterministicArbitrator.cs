using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameObjects.Commands;

public sealed class DeterministicArbitrator
{
    private readonly Dictionary<int, int> _winnerIndexByCell;

    public DeterministicArbitrator(int capacity = 256)
    {
        _winnerIndexByCell = new Dictionary<int, int>(Math.Max(64, capacity));
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

        for (int i = 0; i < proposals.Count; i++)
        {
            ExecutionCommand proposal = proposals[i];
            if (!proposal.IsSpatialConflictAction)
            {
                accepted.Add(proposal);
                continue;
            }

            Point spatialPosition = proposal.ResolveSpatialPosition();
            if (spatialPosition.X < 0 || spatialPosition.Y < 0)
            {
                accepted.Add(proposal);
                continue;
            }

            int cellKey = proposal.ResolveCellKey();
            if (!_winnerIndexByCell.TryGetValue(cellKey, out int winnerIndex))
            {
                _winnerIndexByCell[cellKey] = accepted.Count;
                accepted.Add(proposal);
                continue;
            }

            ExecutionCommand winner = accepted[winnerIndex];
            if (ShouldReplaceWinner(in proposal, in winner))
            {
                rejected.Add(winner);
                accepted[winnerIndex] = proposal;
            }
            else
            {
                rejected.Add(proposal);
            }
        }

        _winnerIndexByCell.Clear();
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
        uint hash = 2166136261u;
        hash = Hash(hash, unchecked((uint)command.CommitTick));
        hash = Hash(hash, unchecked((uint)command.SourceFactionId));
        hash = Hash(hash, unchecked((uint)command.SourceLegionId));
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
}
