using System;
using System.Collections.Generic;
using GameManager;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameObjects
{
    /// <summary>
    /// 高性能伤害数据类 - 优化对象池使用
    /// 🎯 设计原则：
    /// 1. 基础数据 (Value Types) - 最快重置
    /// 2. 外部引用 (Reference Types) - 必须置空防止内存泄漏
    /// 3. 集合数据 (Collections) - 使用Clear()而非new，保留容量
    /// </summary>
    public class TroopDamage : WorldOfTheThreeKingdoms.GameManager.IResettable
    {
        // ==========================================
        // 1. 基础数据 (Value Types) - 最快重置
        // ==========================================
        
        // 核心伤害数据
        public float DamageAmount;
        public int Damage;
        public int FireDamage;
        public int Injury;
        public int CounterDamage;
        public int CounterInjury;
        public float OfficerInjury;
        public int InjuredDamage;
        
        // 战斗状态标志
        public bool IsCritical;
        public bool Critical;
        public bool OnFire;
        public bool Counter;
        public bool BeCountered;
        public bool Surround;
        public bool Waylay;
        public bool Chaos;
        
        // 攻击类型标志
        public bool AntiArrowAttack;
        public bool AntiAttack;
        public bool AntiCounterAttack;
        
        // 挑战系统
        public bool ChallengeHappened;
        public bool ChallengeStarted;
        public int ChallengeResult;
        
        // 数值变化
        public int DestinationCombativityChange;
        public int SourceCombativityChange;
        public int CounterCombativityDown;
        public int DestinationMoraleChange;
        public int SourceMoraleChange;
        public int SourceOffence;
        
        // 特殊效果
        public int StealTroop;
        public int StealInjured;
        public int TirednessIncrease;
        public int StealFood;
        
        // 伤害类型ID (使用ID代替Enum避免装箱)
        public int DamageTypeID;
        
        // ==========================================
        // 2. 外部引用 (Reference Types)
        // 🛑 危险：如果不置空，会导致SourceTroop即使死了也无法被GC回收
        // ==========================================
        
        public Troop SourceTroop;
        public Troop DestinationTroop;
        public Person ChallengeDestinationPerson;
        public Person ChallengeSourcePerson;
        
        // 委托也是引用类型！
        public System.Action OnHitCallback;
        
        // ==========================================
        // 3. 集合数据 (Collections)
        // 🛑 危险：绝对不要在Reset里设为null，也不要new
        // ==========================================
        
        // 预分配容量：假设大部分伤害包含的围攻部队不超过8个
        public TroopList SurroudingList { get; private set; } = new TroopList();
        
        // 预分配容量：假设大部分伤害包含的特效不超过4个
        public List<int> HitEffectIds { get; private set; } = new List<int>(4);
        
        // 如果有嵌套对象（例如伤害附带的Buff/Debuff数据类）
        public List<DamageModifier> Modifiers { get; private set; } = new List<DamageModifier>(4);
        
        // ==========================================
        // 核心Reset逻辑 - 按性能优先级排序
        // ==========================================
        
        /// <summary>
        /// 重置对象到初始状态 - 高性能实现
        /// 🎯 性能优化：按重置成本排序 (基础类型 → 引用类型 → 集合类型)
        /// </summary>
        public void Reset()
        {
            // A. 重置基础类型 (最快) - 直接内存赋值
            DamageAmount = 0f;
            Damage = 0;
            FireDamage = 0;
            Injury = 0;
            CounterDamage = 0;
            CounterInjury = 0;
            OfficerInjury = 0f;
            InjuredDamage = 0;
            
            IsCritical = false;
            Critical = false;
            OnFire = false;
            Counter = false;
            BeCountered = false;
            Surround = false;
            Waylay = false;
            Chaos = false;
            
            AntiArrowAttack = false;
            AntiAttack = false;
            AntiCounterAttack = false;
            
            ChallengeHappened = false;
            ChallengeStarted = false;
            ChallengeResult = 0;
            
            DestinationCombativityChange = 0;
            SourceCombativityChange = 0;
            CounterCombativityDown = 0;
            DestinationMoraleChange = 0;
            SourceMoraleChange = 0;
            SourceOffence = 0;
            
            StealTroop = 0;
            StealInjured = 0;
            TirednessIncrease = 0;
            StealFood = 0;
            
            DamageTypeID = 0;
            
            // B. 断开外部引用 (防止内存泄漏)
            // 🛑 必须置空！否则池子里的这个对象一直抓着Unit不放
            SourceTroop = null;
            DestinationTroop = null;
            ChallengeDestinationPerson = null;
            ChallengeSourcePerson = null;
            OnHitCallback = null;
            
            // C. 清理集合 (零GC)
            // 使用Clear()而不是new List() - 这样List内部的数组(Capacity)依然保留，下次Add不需扩容
            if (SurroudingList != null)
            {
                SurroudingList.Clear();
            }
            
            HitEffectIds.Clear();
            
            // D. 处理嵌套对象 (高级)
            // 如果Modifiers里的对象也是从池子里借来的，必须先还回去！
            if (Modifiers.Count > 0)
            {
                foreach (var mod in Modifiers)
                {
                    // 假设你有ModifierPool
                    // ModifierPool.Return(mod);
                }
                Modifiers.Clear();
            }
        }
        
        // ==========================================
        // 便捷初始化方法
        // ==========================================
        
        /// <summary>
        /// 快速初始化伤害数据 - 在Get()后调用
        /// 🎯 这里不再需要new，因为Get()拿到的一定是已经Reset过的干净对象
        /// </summary>
        /// <param name="source">攻击者</param>
        /// <param name="target">目标</param>
        /// <param name="amount">伤害数值</param>
        public void Init(Troop source, Troop target, float amount)
        {
            SourceTroop = source;
            DestinationTroop = target;
            DamageAmount = amount;
            Damage = (int)amount;
        }
        
        /// <summary>
        /// 完整初始化伤害数据
        /// </summary>
        /// <param name="source">攻击者</param>
        /// <param name="target">目标</param>
        /// <param name="amount">伤害数值</param>
        /// <param name="isCritical">是否暴击</param>
        /// <param name="damageType">伤害类型ID</param>
        public void Init(Troop source, Troop target, float amount, bool isCritical, int damageType = 0)
        {
            SourceTroop = source;
            DestinationTroop = target;
            DamageAmount = amount;
            Damage = (int)amount;
            IsCritical = isCritical;
            Critical = isCritical;
            DamageTypeID = damageType;
        }
        
        /// <summary>
        /// 添加命中特效ID
        /// </summary>
        /// <param name="effectId">特效ID</param>
        public void AddHitEffect(int effectId)
        {
            HitEffectIds.Add(effectId); // 这里直接Add，不用担心List是null
        }
        
        /// <summary>
        /// 添加围攻部队
        /// </summary>
        /// <param name="troop">围攻部队</param>
        public void AddSurroundingTroop(Troop troop)
        {
            if (troop != null)
            {
                SurroudingList.Add(troop);
                Surround = true;
            }
        }
        
        /// <summary>
        /// 设置回调函数
        /// </summary>
        /// <param name="callback">命中回调</param>
        public void SetHitCallback(System.Action callback)
        {
            OnHitCallback = callback;
        }
    }
    
    /// <summary>
    /// 伤害修正器 - 用于复杂伤害计算
    /// </summary>
    public class DamageModifier
    {
        public int ModifierType;
        public float Multiplier;
        public int FlatBonus;
        public bool IsActive;
        
        public void Reset()
        {
            ModifierType = 0;
            Multiplier = 1.0f;
            FlatBonus = 0;
            IsActive = false;
        }
    }
}

