using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateRandom(int seed)
{
    private ulong state = SeedState(seed);

    public int Seed { get; } = seed;

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        uint range = (uint)(maxExclusive - minInclusive);
        uint threshold = uint.MaxValue - (uint.MaxValue % range);

        uint value;
        do
        {
            value = NextUInt();
        }
        while (value >= threshold);

        return minInclusive + (int)(value % range);
    }

    public float NextSingle()
    {
        return (NextUInt() >> 8) * (1.0f / 16777216.0f);
    }

    private uint NextUInt()
    {
        ulong x = state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        state = x;
        ulong result = x * 2685821657736338717UL;
        return (uint)(result >> 32);
    }

    private static ulong SeedState(int seed)
    {
        ulong x = (uint)seed;
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return x == 0 ? 0x9E3779B97F4A7C15UL : x;
    }
}

