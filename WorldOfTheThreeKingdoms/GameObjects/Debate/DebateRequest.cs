using System;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateRequest
{
    public DebateMode Mode { get; init; } = DebateMode.Battle;

    public DebateParticipantSeed Left { get; init; }

    public DebateParticipantSeed Right { get; init; }

    public int Seed { get; init; }

    public static DebateRequest CreateBattle(in DebateParticipantSeed left, in DebateParticipantSeed right, int? seed = null)
    {
        return new DebateRequest
        {
            Mode = DebateMode.Battle,
            Left = left,
            Right = right,
            Seed = seed ?? Environment.TickCount
        };
    }

    public static DebateRequest CreateBattle(Person left, Person right, bool isLeftPlayerControlled, bool isRightPlayerControlled, int? seed = null)
    {
        if (left == null)
        {
            throw new ArgumentNullException(nameof(left));
        }

        if (right == null)
        {
            throw new ArgumentNullException(nameof(right));
        }

        return CreateBattle(
            DebateParticipantSeed.FromPerson(left, isLeftPlayerControlled),
            DebateParticipantSeed.FromPerson(right, isRightPlayerControlled),
            seed);
    }

    public static DebateRequest CreateDemo(in DebateParticipantSeed left, in DebateParticipantSeed right, int? seed = null)
    {
        return new DebateRequest
        {
            Mode = DebateMode.Demo,
            Left = left,
            Right = right,
            Seed = seed ?? Environment.TickCount
        };
    }
}

