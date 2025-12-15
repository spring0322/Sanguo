using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace GameManager
{
    /// <summary>
    /// 音频实例对象池 - 避免频繁创建和销毁 SoundEffectInstance
    /// 🎯 核心特点：
    /// 1. 直接用 SoundEffect 对象作为 Key，速度最快
    /// 2. 自动扩容和收缩
    /// 3. 性能监控
    /// </summary>
    public static class AudioPool
    {
        // 直接用 SoundEffect 对象作为 Key，速度最快
        private static Dictionary<SoundEffect, Stack<SoundEffectInstance>> _pool = new Dictionary<SoundEffect, Stack<SoundEffectInstance>>();
        
        // 性能统计
        private static Dictionary<SoundEffect, int> _totalCreated = new Dictionary<SoundEffect, int>();
        private static Dictionary<SoundEffect, int> _totalReused = new Dictionary<SoundEffect, int>();
        
        private const int INITIAL_POOL_SIZE = 4;
        private const int MAX_POOL_SIZE = 16;

        /// <summary>
        /// 获取音效实例
        /// </summary>
        public static SoundEffectInstance Get(SoundEffect sfx)
        {
            if (!_pool.TryGetValue(sfx, out var stack))
            {
                stack = new Stack<SoundEffectInstance>();
                _pool[sfx] = stack;
                _totalCreated[sfx] = 0;
                _totalReused[sfx] = 0;
            }

            if (stack.Count > 0)
            {
                _totalReused[sfx]++;
                return stack.Pop();
            }

            _totalCreated[sfx]++;
            return sfx.CreateInstance();
        }

        /// <summary>
        /// Return 时直接传 sfx 对象，不需要传 string name
        /// </summary>
        public static void Return(SoundEffect sfx, SoundEffectInstance inst)
        {
            if (sfx == null || inst == null) return;

            // 重置实例状态
            inst.Stop();
            inst.Volume = 1.0f;
            inst.Pan = 0.0f;
            inst.Pitch = 0.0f;

            if (_pool.TryGetValue(sfx, out var stack))
            {
                if (stack.Count < MAX_POOL_SIZE)
                {
                    stack.Push(inst);
                }
                else
                {
                    // 池子满了，直接释放
                    inst.Dispose();
                }
            }
            // else: 理论上不该发生，除非池子被重置了
        }

        /// <summary>
        /// 兼容旧接口 - 通过名称归还（性能较低，建议使用新接口）
        /// </summary>
        public static void ReturnSoundInstance(string soundName, SoundEffectInstance instance)
        {
            if (instance == null) return;

            // 重置实例状态
            instance.Stop();
            instance.Volume = 1.0f;
            instance.Pan = 0.0f;
            instance.Pitch = 0.0f;

            // 找到对应的SoundEffect
            SoundEffect foundSfx = null;
            foreach (var kvp in _pool)
            {
                if (kvp.Key.Name == soundName)
                {
                    foundSfx = kvp.Key;
                    break;
                }
            }

            if (foundSfx != null)
            {
                Return(foundSfx, instance);
            }
            else
            {
                // 找不到对应SoundEffect，直接释放
                instance.Dispose();
            }
        }

        /// <summary>
        /// 兼容旧接口 - 通过SoundEffect获取实例
        /// </summary>
        public static SoundEffectInstance GetSoundInstance(SoundEffect soundEffect)
        {
            return Get(soundEffect);
        }

        /// <summary>
        /// 预热指定音效的池子
        /// </summary>
        private static void PrewarmPool(SoundEffect soundEffect, int count)
        {
            if (!_pool.TryGetValue(soundEffect, out var stack))
            {
                stack = new Stack<SoundEffectInstance>();
                _pool[soundEffect] = stack;
                _totalCreated[soundEffect] = 0;
                _totalReused[soundEffect] = 0;
            }
            
            for (int i = 0; i < count; i++)
            {
                var instance = soundEffect.CreateInstance();
                stack.Push(instance);
                _totalCreated[soundEffect]++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[AudioPool] 预热音效池: {soundEffect.Name ?? "Unknown"}, 大小: {count}");
        }

        /// <summary>
        /// 预热所有已知音效的池子
        /// </summary>
        public static void PrewarmAll()
        {
            foreach (var kvp in _pool)
            {
                if (kvp.Value.Count < INITIAL_POOL_SIZE)
                {
                    int needed = INITIAL_POOL_SIZE - kvp.Value.Count;
                    PrewarmPool(kvp.Key, needed);
                }
            }
        }

        /// <summary>
        /// 清理所有池子
        /// </summary>
        public static void Clear()
        {
            foreach (var stack in _pool.Values)
            {
                while (stack.Count > 0)
                {
                    var instance = stack.Pop();
                    instance.Dispose();
                }
            }
            
            _pool.Clear();
            _totalCreated.Clear();
            _totalReused.Clear();
            
            System.Diagnostics.Debug.WriteLine("[AudioPool] 所有音频池已清理");
        }

        /// <summary>
        /// 收缩池子大小（释放多余的实例）
        /// </summary>
        public static void Shrink()
        {
            foreach (var kvp in _pool)
            {
                var stack = kvp.Value;
                
                // 保留最少数量，释放多余的
                while (stack.Count > INITIAL_POOL_SIZE)
                {
                    var instance = stack.Pop();
                    instance.Dispose();
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[AudioPool] 池子收缩完成");
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public static string GetStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 音频池统计 ===");
            
            foreach (var kvp in _pool)
            {
                var sfx = kvp.Key;
                int poolSize = kvp.Value.Count;
                int created = _totalCreated.ContainsKey(sfx) ? _totalCreated[sfx] : 0;
                int reused = _totalReused.ContainsKey(sfx) ? _totalReused[sfx] : 0;
                float reuseRate = created > 0 ? (float)reused / (created + reused) * 100f : 0f;
                
                stats.AppendLine($"{sfx.Name ?? "Unknown"}: 池大小={poolSize}, 创建={created}, 重用={reused}, 重用率={reuseRate:F1}%");
            }
            
            return stats.ToString();
        }

        /// <summary>
        /// 获取指定音效的池子信息
        /// </summary>
        public static (int poolSize, int totalCreated, int totalReused) GetPoolInfo(string soundName)
        {
            SoundEffect foundSfx = null;
            foreach (var kvp in _pool)
            {
                if (kvp.Key.Name == soundName)
                {
                    foundSfx = kvp.Key;
                    break;
                }
            }

            if (foundSfx != null && _pool.ContainsKey(foundSfx))
            {
                return (
                    _pool[foundSfx].Count,
                    _totalCreated.ContainsKey(foundSfx) ? _totalCreated[foundSfx] : 0,
                    _totalReused.ContainsKey(foundSfx) ? _totalReused[foundSfx] : 0
                );
            }

            return (0, 0, 0);
        }
    }
}