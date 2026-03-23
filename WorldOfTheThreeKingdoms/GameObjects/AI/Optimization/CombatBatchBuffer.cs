using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameObjects;

namespace GameObjects.AI.Optimization
{
    /// <summary>
    /// 战斗评分批处理核心 (SoA 布局)
    /// 专为 .NET 8 SIMD 优化设计
    /// </summary>
    public unsafe class CombatBatchBuffer
    {
        // 预分配大数组，避免 GC。假设视野内最多 256 个敌人/目标
        public const int Capacity = 256;
        
        // 核心数据：位置 (X, Y)
        public readonly float[] TargetXs = new float[Capacity];
        public readonly float[] TargetYs = new float[Capacity];
        
        // 核心数据：基础分 (BaseScore)
        public readonly float[] BaseScores = new float[Capacity];
        
        // 结果数据：计算出的距离惩罚
        public readonly float[] Scores = new float[Capacity];

        // [Modification] Store references to retrieve the best target later
        public readonly Troop[] Targets = new Troop[Capacity];

        public int Count = 0;

        /// <summary>
        /// 重置并准备装填数据
        /// </summary>
        public void Reset()
        {
            // Clear references to avoid memory leaks if widely reused long-term
            Array.Clear(Targets, 0, Count);
            Count = 0;
        }

        /// <summary>
        /// 将敌军对象数据“压平”存入 SoA 数组
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTarget(Troop enemy)
        {
            if (Count >= Capacity) return;
            
            // 这里将 Point 转为 float，方便 SIMD 计算
            TargetXs[Count] = enemy.Position.X;
            TargetYs[Count] = enemy.Position.Y;
            
            // 预先计算好的基础分 (兵力/状态等)
            // 可以在此处直接计算，或者复用 CalculateBasicAttackScore 的结果
            BaseScores[Count] = (float)enemy.Quantity * 0.02f + (enemy.Destroyed ? 500f : 0f);
            
            Targets[Count] = enemy;

            Count++;
        }
    }

    public static unsafe class CombatScoreSIMD
    {
        /// <summary>
        /// 批量计算某一个地块(Tile)对所有敌人的评分
        /// </summary>
        /// <param name="tileX">当前评估的地块X</param>
        /// <param name="tileY">当前评估的地块Y</param>
        /// <param name="attackRange">我方射程</param>
        /// <param name="buffer">敌军SoA数据</param>
        /// <returns>该地块对所有敌人的最高评分和对应索引</returns>
        public static (float maxScore, int bestIndex) GetMaxScoreForTile(float tileX, float tileY, float attackRange, CombatBatchBuffer buffer)
        {
            // 广播地块坐标到向量
            Vector256<float> vTileX = Vector256.Create(tileX);
            Vector256<float> vTileY = Vector256.Create(tileY);
            Vector256<float> vRange = Vector256.Create(attackRange);
            Vector256<float> vMaxScore = Vector256.Create(float.MinValue);
            // 用于追踪最大值索引的辅助向量 (SIMD tracking index is complex, falling back to mixed approach)
            // Actually, for "Max Score", finding the index in SIMD is tricky without AVX512.
            // Simplified approach: Calculate scores in SIMD, store them, then find max scalar? 
            // OR: Run the standard logic user provided (which only creates max score) and then re-find index for the winner?
            // Given the user code didn't provide index tracking, let's stick closer to their code but fix the result.
            
            // To properly return index, we might need to store the calculated scores back to buffer.Scores
            // or just find the max.

            // The user's provided code computes vMaxScore but doesn't store individual scores.
            // Let's modify it to maximize performance while allowing index retrieval.
            
            // We will run the calculation and just find the best scalar at the end if we want, OR
            // we can stick to the user's request.
            // The user's request says: "If using Target, SIMD function needs to return Index".
            // Let's assume we want to do that.

            // 惩罚系数 (对应 CalculateDistancePenaltyEnhanced 中的 50.0f)
            Vector256<float> vPenaltyFactor = Vector256.Create(50.0f);

            int bestIdx = -1;
            float globalMax = float.MinValue;

            // Simple Scalar Fallback for index tracking correctness for now, or hybrid?
            // The user wants SIMD usage. 
            // Writing back to array and then finding max might be faster than scalar calculation?
            
            int i = 0;
            ReadOnlySpan<float> xs = buffer.TargetXs;
            ReadOnlySpan<float> ys = buffer.TargetYs;
            ReadOnlySpan<float> bases = buffer.BaseScores;
            // We need to output scores to check max index later, or check on the fly.
            // Checking on the fly inside vector loop is hard.
            // Let's write results to buffer.Scores (which was defined but unused in user snippet).
            
            fixed (float* pScores = buffer.Scores)
            {
                for (; i <= buffer.Count - 8; i += 8)
                {
                    Vector256<float> vTargetX = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(xs.Slice(i)));
                    Vector256<float> vTargetY = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(ys.Slice(i)));
                    Vector256<float> vBaseScore = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(bases.Slice(i)));

                    var vDiffX = Vector256.Abs(Avx.Subtract(vTileX, vTargetX));
                    var vDiffY = Vector256.Abs(Avx.Subtract(vTileY, vTargetY));
                    var vDist = Vector256.Max(vDiffX, vDiffY);

                    var vMaskOverRange = Avx.Compare(vDist, vRange, FloatComparisonMode.OrderedGreaterThanNonSignaling);
                    var vOverDist = Avx.Subtract(vDist, vRange);
                    var vPenalty = Avx.Multiply(vOverDist, vPenaltyFactor);
                    vPenalty = Avx.BlendVariable(Vector256<float>.Zero, vPenalty, vMaskOverRange);

                    var vFinalScore = Avx.Subtract(vBaseScore, vPenalty);
                    
                    // Store back
                    vFinalScore.Store(pScores + i);
                }
            }

            // Deal with remaining items
            for (; i < buffer.Count; i++)
            {
                float dist = Math.Max(Math.Abs(tileX - xs[i]), Math.Abs(tileY - ys[i]));
                float penalty = 0;
                if (dist > attackRange)
                {
                    penalty = (dist - attackRange) * 50.0f;
                }
                buffer.Scores[i] = bases[i] - penalty;
            }

            // Find Max in Scale (iterating the filled scores array) - Vectorizing this find-max is also possible but maybe overkill for <256
            // The user provided code did "vMaxScore = Avx.Max(vMaxScore, vFinalScore);" which is faster for just score.
            // But we need index.
            // Iterating 200 floats is very fast.
            
            for (int k = 0; k < buffer.Count; k++)
            {
                if (buffer.Scores[k] > globalMax)
                {
                    globalMax = buffer.Scores[k];
                    bestIdx = k;
                }
            }

            return (globalMax, bestIdx);
        }
    }
}
