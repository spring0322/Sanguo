using GameObjects;
using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelRequest
{
    public DuelMode Mode { get; init; } = DuelMode.Battle;

    public int LeftPersonId { get; init; } = -1;

    public int RightPersonId { get; init; } = -1;

    public TroopDamage Damage { get; init; }

    public int Seed { get; init; }

    public static DuelRequest CreateBattle(Person left, Person right, TroopDamage damage, int? seed = null)
    {
        return new DuelRequest
        {
            Mode = DuelMode.Battle,
            LeftPersonId = left?.ID ?? -1,
            RightPersonId = right?.ID ?? -1,
            Damage = damage,
            Seed = seed ?? Environment.TickCount
        };
    }

    public static DuelRequest CreateDemo(int leftPersonId, int rightPersonId, int? seed = null)
    {
        return new DuelRequest
        {
            Mode = DuelMode.Demo,
            LeftPersonId = leftPersonId,
            RightPersonId = rightPersonId,
            Seed = seed ?? Environment.TickCount
        };
    }
}
