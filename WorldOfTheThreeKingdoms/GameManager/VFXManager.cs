using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// 高性能VFX特效对象 - 基于您提供的优秀设计
    /// 🎯 核心特点：
    /// 1. 自动生命周期管理
    /// 2. 零GC的对象池重用
    /// 3. 统一的Update调度
    /// 4. 防坑的视觉残留清理
    /// </summary>
    public class VFXObject : IResettable
    {
        // === 基础属性 ===
        public bool IsActive { get; private set; }
        public string AssetName { get; private set; } // 用于归还时识别属于哪个池子
        
        // === 状态控制 ===
        private float _lifeTimer;
        private float _duration;
        private bool _autoReturn; // 是否时间到了自动归还
        
        // === 视觉组件引用 (根据MonoGame环境调整) ===
        // private ParticleEmitter _emitter; 
        // private AnimationPlayer _animPlayer;
        
        // 位置信息 (简化的Transform)
        public Vector2 Position;
        public float Rotation;
        public float Scale;
        public Color Tint = Color.White;
        public float Alpha = 1.0f;
        
        // 游戏相关属性
        public Troop AttachedTroop; // 附着的部队
        public bool FollowTroop; // 是否跟随部队移动
        
        public VFXObject(string assetName)
        {
            AssetName = assetName;
            // 在这里初始化 _emitter 等组件
        }
        
        // === 1. 激活方法 (代替构造函数) ===
        public void Play(Vector2 pos, float duration, float scale = 1.0f)
        {
            // 关键：先设置位置，再激活，防止第一帧出现在原点
            Position = pos;
            Scale = scale;
            Rotation = 0f; // 默认重置旋转
            Tint = Color.White;
            Alpha = 1.0f;
            
            _duration = duration;
            _lifeTimer = 0f;
            _autoReturn = true;
            IsActive = true;
            
            // 如果有粒子系统或动画，在这里调用 Play()
            // _emitter?.Play();
        }
        
        /// <summary>
        /// 播放附着到部队的特效
        /// </summary>
        public void PlayOnTroop(Troop troop, float duration, float scale = 1.0f)
        {
            AttachedTroop = troop;
            FollowTroop = true;
            
            if (troop != null)
            {
                Play(new Vector2(troop.Position.X, troop.Position.Y), duration, scale);
            }
            else
            {
                Play(Vector2.Zero, duration, scale);
            }
        }
        
        // === 2. 帧更新 (由Manager统一调度，比Unity Update更快) ===
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            // 跟随部队移动
            if (FollowTroop && AttachedTroop != null)
            {
                Position = new Vector2(AttachedTroop.Position.X, AttachedTroop.Position.Y);
            }
            
            // 更新视觉组件逻辑
            // _emitter?.Update(deltaTime);
            
            // 计时器逻辑
            if (_autoReturn)
            {
                _lifeTimer += deltaTime;
                if (_lifeTimer >= _duration)
                {
                    StopAndReturn();
                }
            }
        }
        
        // === 3. 停止并归还 ===
        public void StopAndReturn()
        {
            // 可以在这里做一些淡出效果，但为了性能通常直接回收
            VFXManager.Instance?.ReturnVFX(this);
        }
        
        // === 4. 重置接口 (核心) ===
        public void Reset()
        {
            IsActive = false;
            _lifeTimer = 0;
            _duration = 0;
            _autoReturn = false;
            
            // 清理游戏相关引用
            AttachedTroop = null;
            FollowTroop = false;
            
            // 🛑 核心防坑点：清理视觉残留
            // 1. 如果有拖尾 (Trail)，必须 Clear，否则会有一条线连到下一个出生点
            // _trailRenderer?.Clear(); 
            
            // 2. 如果是粒子，停止发射并移除所有现存粒子
            // _emitter?.ClearParticles();
            
            // 3. 重置透明度/颜色 (防止上一次是渐隐消失的，这一次出来是透明的)
            Alpha = 1.0f;
            Tint = Color.White;
            Scale = 1.0f;
            Rotation = 0f;
            Position = Vector2.Zero;
        }
        
        /// <summary>
        /// 手动设置生命周期
        /// </summary>
        public void SetLifetime(float newDuration)
        {
            _duration = newDuration;
            _lifeTimer = 0f;
        }
        
        /// <summary>
        /// 禁用自动归还 - 用于需要手动控制的特效
        /// </summary>
        public void DisableAutoReturn()
        {
            _autoReturn = false;
        }
    }
    
    /// <summary>
    /// 高性能VFX管理器 - 基于您提供的优秀架构
    /// 🎯 核心特点：
    /// 1. 字典池管理不同类型特效
    /// 2. 统一Update调度避免性能开销
    /// 3. 懒加载和预热机制
    /// 4. 安全的反向遍历移除
    /// </summary>
    public class VFXManager
    {
        // 单例模式 (为了方便全局调用，如 VFXManager.Play("Explosion", pos))
        public static VFXManager Instance { get; private set; }
        
        // 字典池：Key是特效名，Value是该类特效的堆栈
        private Dictionary<string, ObjectPool<VFXObject>> _pools;
        
        // 活跃列表：用于统一Update，避免每个特效自己Hook游戏循环
        private List<VFXObject> _activeVFX;
        
        // 性能统计
        public int ActiveVFXCount => _activeVFX.Count;
        public int TotalPoolCount => _pools.Count;
        
        public VFXManager()
        {
            Instance = this;
            _pools = new Dictionary<string, ObjectPool<VFXObject>>();
            _activeVFX = new List<VFXObject>(100);
        }
        
        // === 预热池子 (Loading时调用) ===
        public void Prewarm(string assetName, int count)
        {
            if (!_pools.ContainsKey(assetName))
            {
                // 使用工厂方法创建池子（AOT 安全）
                _pools[assetName] = new ObjectPool<VFXObject>(
                    () => new VFXObject(assetName),
                    count, 
                    count * 2);
                
                // 预热池子
                for (int i = 0; i < count; i++)
                {
                    var vfx = new VFXObject(assetName);
                    _pools[assetName].Return(vfx);
                }
                
                System.Diagnostics.Debug.WriteLine($"[VFXManager] 预热特效池 '{assetName}': {count}个对象");
            }
        }
        
        /// <summary>
        /// 批量预热常用特效
        /// </summary>
        public void PrewarmCommonVFX()
        {
            // 战斗特效
            Prewarm("HitSpark", 50);
            Prewarm("BloodSplash", 30);
            Prewarm("CriticalHit", 20);
            Prewarm("Explosion", 15);
            
            // 状态特效
            Prewarm("Heal", 20);
            Prewarm("Buff", 25);
            Prewarm("Debuff", 25);
            Prewarm("LevelUp", 10);
            
            // 环境特效
            Prewarm("Dust", 40);
            Prewarm("Smoke", 30);
            Prewarm("Fire", 20);
            
            System.Diagnostics.Debug.WriteLine("[VFXManager] 常用特效预热完成");
        }
        
        // === 核心API: 播放特效 ===
        public VFXObject Play(string assetName, Vector2 position, float duration = 1.0f)
        {
            // 1. 懒加载池子 (Safe check)
            if (!_pools.TryGetValue(assetName, out var pool))
            {
                // 如果没预加载，这里会有点卡，建议Warning
                System.Diagnostics.Debug.WriteLine($"[VFXManager] 警告: 特效 '{assetName}' 未预热，正在创建池子");
                pool = new ObjectPool<VFXObject>(
                    () => new VFXObject(assetName),
                    10, 
                    50);
                _pools[assetName] = pool;
            }
            
            // 2. 从池中取出 (已在Get内部Reset)
            var vfx = pool.Get();
            if (vfx == null)
            {
                vfx = new VFXObject(assetName);
            }
            
            // 3. 设置状态并激活
            vfx.Play(position, duration);
            
            // 4. 加入活跃列表 (以便Update)
            _activeVFX.Add(vfx);
            
            return vfx;
        }
        
        /// <summary>
        /// 播放附着到部队的特效
        /// </summary>
        public VFXObject PlayOnTroop(string assetName, Troop troop, float duration = 1.0f)
        {
            var vfx = Play(assetName, Vector2.Zero, duration);
            if (vfx != null)
            {
                vfx.PlayOnTroop(troop, duration);
            }
            return vfx;
        }
        
        /// <summary>
        /// 播放暴击特效
        /// </summary>
        public VFXObject PlayCriticalHit(Vector2 position, float scale = 1.5f)
        {
            var vfx = Play("CriticalHit", position, 0.8f);
            if (vfx != null)
            {
                vfx.Scale = scale;
                vfx.Tint = Color.Yellow;
            }
            return vfx;
        }
        
        /// <summary>
        /// 播放治疗特效
        /// </summary>
        public VFXObject PlayHeal(Troop target, int healAmount)
        {
            if (target == null) return null;
            
            var vfx = PlayOnTroop("Heal", target, 1.2f);
            if (vfx != null)
            {
                vfx.Tint = Color.Green;
                // 可以根据治疗量调整特效大小
                vfx.Scale = Math.Min(2.0f, 1.0f + healAmount / 100.0f);
            }
            return vfx;
        }
        
        // === 归还特效 ===
        public void ReturnVFX(VFXObject vfx)
        {
            if (vfx == null) return;
            
            if (_pools.TryGetValue(vfx.AssetName, out var pool))
            {
                // 标记为非活跃，在Update中会被移除并归还
                vfx.Reset();
                
                // 注意：这里不直接从_activeVFX中移除，因为可能正在遍历中
                // 移除操作在Update的反向遍历中安全进行
            }
        }
        
        // === 统一更新 ===
        public void Update(float deltaTime)
        {
            // 反向遍历，方便安全移除
            for (int i = _activeVFX.Count - 1; i >= 0; i--)
            {
                var vfx = _activeVFX[i];
                
                // 1. 执行逻辑
                if (vfx.IsActive)
                {
                    vfx.Update(deltaTime);
                }
                
                // 2. 检查是否失活 (由VFXObject内部决定何时Stop)
                if (!vfx.IsActive)
                {
                    // 归还到池子
                    if (_pools.TryGetValue(vfx.AssetName, out var pool))
                    {
                        pool.Return(vfx);
                    }
                    
                    // 从活跃表移除 (Swap Remove O(1))
                    int lastIdx = _activeVFX.Count - 1;
                    if (i != lastIdx)
                    {
                        _activeVFX[i] = _activeVFX[lastIdx];
                    }
                    _activeVFX.RemoveAt(lastIdx);
                }
            }
        }
        
        /// <summary>
        /// 停止所有特效 - 用于场景切换
        /// </summary>
        public void StopAllVFX()
        {
            // 归还所有活跃特效
            for (int i = _activeVFX.Count - 1; i >= 0; i--)
            {
                var vfx = _activeVFX[i];
                if (_pools.TryGetValue(vfx.AssetName, out var pool))
                {
                    vfx.Reset();
                    pool.Return(vfx);
                }
            }
            
            _activeVFX.Clear();
            System.Diagnostics.Debug.WriteLine("[VFXManager] 所有特效已停止");
        }
        
        /// <summary>
        /// 清理所有池子 - 用于游戏退出
        /// </summary>
        public void Dispose()
        {
            StopAllVFX();
            
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            
            System.Diagnostics.Debug.WriteLine("[VFXManager] 已清理所有特效池");
        }
        
        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== VFX性能统计 ===");
            stats.AppendLine($"活跃特效数量: {ActiveVFXCount}");
            stats.AppendLine($"特效池数量: {TotalPoolCount}");
            
            foreach (var kvp in _pools)
            {
                stats.AppendLine($"  {kvp.Key}: {kvp.Value.GetStats()}");
            }
            
            return stats.ToString();
        }
        
        /// <summary>
        /// 获取指定特效的池子统计
        /// </summary>
        public string GetPoolStats(string assetName)
        {
            if (_pools.TryGetValue(assetName, out var pool))
            {
                return pool.GetStats();
            }
            return $"特效池 '{assetName}' 不存在";
        }
    }
    
    /// <summary>
    /// VFX管理器扩展 - 游戏特定的便捷方法
    /// </summary>
    public static class VFXManagerExtensions
    {
        /// <summary>
        /// 为部队播放伤害特效
        /// </summary>
        public static void PlayDamageVFX(this VFXManager manager, TroopDamage damage)
        {
            if (damage?.DestinationTroop == null) return;
            
            Vector2 position = new Vector2(damage.DestinationTroop.Position.X, damage.DestinationTroop.Position.Y);
            
            // 根据伤害类型播放不同特效
            if (damage.IsCritical)
            {
                manager.PlayCriticalHit(position, 1.5f);
            }
            else
            {
                manager.Play("HitSpark", position, 0.5f);
            }
            
            // 血溅特效
            if (damage.Damage > 0)
            {
                var bloodVFX = manager.Play("BloodSplash", position, 0.8f);
                if (bloodVFX != null)
                {
                    // 根据伤害量调整血溅大小
                    bloodVFX.Scale = Math.Min(2.0f, 0.5f + damage.Damage / 50.0f);
                }
            }
            
            // 火焰伤害特效
            if (damage.FireDamage > 0)
            {
                manager.PlayOnTroop("Fire", damage.DestinationTroop, 2.0f);
            }
        }
        
        /// <summary>
        /// 播放战斗开始特效
        /// </summary>
        public static void PlayCombatStartVFX(this VFXManager manager, Troop attacker, Troop defender)
        {
            if (attacker != null)
            {
                manager.PlayOnTroop("CombatAura", attacker, 1.0f);
            }
            
            if (defender != null)
            {
                manager.PlayOnTroop("DefenseAura", defender, 1.0f);
            }
        }
    }
}