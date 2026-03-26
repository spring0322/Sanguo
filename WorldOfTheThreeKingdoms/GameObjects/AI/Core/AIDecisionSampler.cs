using System;

namespace GameObjects.AI;

public static class AIDecisionSampler
{
    private const uint FnvOffset = 2166136261u;
    private const uint FnvPrime = 16777619u;

    public static float Sample01(int tick, int factionId, int entityId, int streamId)
    {
        uint hash = FnvOffset;
        hash = Hash(hash, (uint)tick);
        hash = Hash(hash, (uint)factionId);
        hash = Hash(hash, (uint)entityId);
        hash = Hash(hash, (uint)streamId);

        uint mixed = Mix(hash);
        return (mixed & 0x00FFFFFFu) / 16777216f;
    }

    public static int FindBestIndex(ReadOnlySpan<int> scores)
    {
        int bestIndex = -1;
        int bestScore = int.MinValue;

        for (int i = 0; i < scores.Length; i++)
        {
            int score = scores[i];
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public static int SelectNearTieSoftmaxIndex(
        ReadOnlySpan<int> scores,
        int bestIndex,
        int tieGap,
        int temperaturePermille,
        float random01,
        Span<int> candidateBuffer,
        Span<float> weightBuffer)
    {
        if (bestIndex < 0 || bestIndex >= scores.Length)
        {
            return bestIndex;
        }

        int bestScore = scores[bestIndex];
        if (bestScore == int.MinValue)
        {
            return bestIndex;
        }

        int count = 0;
        int clampedGap = Math.Max(0, tieGap);
        float inverseTemperature = 1000f / Math.Max(1, temperaturePermille);

        for (int i = 0; i < scores.Length; i++)
        {
            int score = scores[i];
            if (score == int.MinValue) continue;
            if (bestScore - score > clampedGap) continue;

            float scaled = (score - bestScore) * inverseTemperature;
            float weight = MathF.Exp(scaled);
            if (!(weight > 0f) || float.IsNaN(weight) || float.IsInfinity(weight))
            {
                weight = 0.0001f;
            }

            candidateBuffer[count] = i;
            weightBuffer[count] = weight;
            count++;

            if (count >= candidateBuffer.Length)
            {
                break;
            }
        }

        if (count <= 1)
        {
            return bestIndex;
        }

        float totalWeight = 0f;
        for (int i = 0; i < count; i++)
        {
            totalWeight += weightBuffer[i];
        }

        if (!(totalWeight > 0f))
        {
            return bestIndex;
        }

        float normalizedRandom = Math.Clamp(random01, 0f, 0.999999f);
        float threshold = normalizedRandom * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < count; i++)
        {
            cumulative += weightBuffer[i];
            if (threshold <= cumulative)
            {
                return candidateBuffer[i];
            }
        }

        return candidateBuffer[count - 1];
    }

    private static uint Hash(uint seed, uint value)
    {
        return (seed ^ value) * FnvPrime;
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
