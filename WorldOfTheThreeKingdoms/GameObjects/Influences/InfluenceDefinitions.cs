using System;

namespace GameObjects.Influences
{
    /// <summary>
    /// 技能影响类型枚举
    /// </summary>
    public enum InfluenceKind
    {
        /// <summary>
        /// 伤害类影响
        /// </summary>
        Damage,
        
        /// <summary>
        /// 治疗/净化类影响
        /// </summary>
        Purify,
        
        /// <summary>
        /// 混乱状态
        /// </summary>
        Confusion,
        
        /// <summary>
        /// 晕眩状态
        /// </summary>
        Stun,
        
        /// <summary>
        /// 增益状态
        /// </summary>
        Buff,
        
        /// <summary>
        /// 减益状态
        /// </summary>
        Debuff,
        
        /// <summary>
        /// 位移效果
        /// </summary>
        Displacement,
        
        /// <summary>
        /// 召唤效果
        /// </summary>
        Summon,
        
        /// <summary>
        /// 资源恢复
        /// </summary>
        Restore,
        
        /// <summary>
        /// 特殊效果
        /// </summary>
        Special,
        
        /// <summary>
        /// 降低士气
        /// </summary>
        MoraleDown,
        
        /// <summary>
        /// 诱敌/挑拨 - 强制移动敌人
        /// </summary>
        Lure,
        
        /// <summary>
        /// 内讧/反间 - 让敌人攻击自己人
        /// </summary>
        InternalStrife
    }

    /// <summary>
    /// 状态类型枚举
    /// </summary>
    public enum StatusKind
    {
        /// <summary>
        /// 晕眩状态
        /// </summary>
        Stun,
        
        /// <summary>
        /// 混乱状态
        /// </summary>
        Confusion,
        
        /// <summary>
        /// 中毒状态
        /// </summary>
        Poison,
        
        /// <summary>
        /// 燃烧状态
        /// </summary>
        Burn,
        
        /// <summary>
        /// 冰冻状态
        /// </summary>
        Freeze,
        
        /// <summary>
        /// 攻击力提升
        /// </summary>
        AttackBoost,
        
        /// <summary>
        /// 防御力提升
        /// </summary>
        DefenseBoost,
        
        /// <summary>
        /// 速度提升
        /// </summary>
        SpeedBoost,
        
        /// <summary>
        /// 攻击力降低
        /// </summary>
        AttackDebuff,
        
        /// <summary>
        /// 防御力降低
        /// </summary>
        DefenseDebuff,
        
        /// <summary>
        /// 速度降低
        /// </summary>
        SpeedDebuff
    }

    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainKind
    {
        /// <summary>
        /// 平原
        /// </summary>
        Plain,
        
        /// <summary>
        /// 森林
        /// </summary>
        Forest,
        
        /// <summary>
        /// 山地
        /// </summary>
        Mountain,
        
        /// <summary>
        /// 河流/水域
        /// </summary>
        Water,
        
        /// <summary>
        /// 河流
        /// </summary>
        River,
        
        /// <summary>
        /// 沙漠
        /// </summary>
        Desert,
        
        /// <summary>
        /// 沼泽
        /// </summary>
        Swamp,
        
        /// <summary>
        /// 草地
        /// </summary>
        Grassland,
        
        /// <summary>
        /// 城镇
        /// </summary>
        Town,
        
        /// <summary>
        /// 要塞
        /// </summary>
        Fortress
    }

    /// <summary>
    /// 天气类型枚举
    /// </summary>
    public enum WeatherKind
    {
        /// <summary>
        /// 晴天
        /// </summary>
        Clear,
        
        /// <summary>
        /// 雨天
        /// </summary>
        Rain,
        
        /// <summary>
        /// 雷雨
        /// </summary>
        Storm,
        
        /// <summary>
        /// 雪天
        /// </summary>
        Snow,
        
        /// <summary>
        /// 雾天
        /// </summary>
        Fog,
        
        /// <summary>
        /// 干旱
        /// </summary>
        Drought,
        
        /// <summary>
        /// 大风
        /// </summary>
        Wind
    }

    /// <summary>
    /// 技能影响基类
    /// </summary>
    public class Influence
    {
        /// <summary>
        /// 影响类型
        /// </summary>
        public InfluenceKind Kind { get; set; }
        
        /// <summary>
        /// 影响强度/威力
        /// </summary>
        public float Power { get; set; }
        
        /// <summary>
        /// 影响数值（治疗量、伤害量等）
        /// </summary>
        public float Amount { get; set; }
        
        /// <summary>
        /// 持续时间（回合数）
        /// </summary>
        public int Duration { get; set; }
        
        /// <summary>
        /// 是否为火属性
        /// </summary>
        public bool IsFire { get; set; }
        
        /// <summary>
        /// 是否为冰属性
        /// </summary>
        public bool IsIce { get; set; }
        
        /// <summary>
        /// 是否为雷属性
        /// </summary>
        public bool IsThunder { get; set; }
        
        /// <summary>
        /// 是否为水属性
        /// </summary>
        public bool IsWater { get; set; }
        
        /// <summary>
        /// 影响范围（格数）
        /// </summary>
        public int Range { get; set; }
        
        /// <summary>
        /// 成功率（0-1）
        /// </summary>
        public float SuccessRate { get; set; } = 1.0f;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Influence()
        {
            Kind = InfluenceKind.Damage;
            Power = 1.0f;
            Amount = 0;
            Duration = 0;
            Range = 1;
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Influence(InfluenceKind kind, float power, float amount = 0, int duration = 0)
        {
            Kind = kind;
            Power = power;
            Amount = amount;
            Duration = duration;
            Range = 1;
        }
    }

    /// <summary>
    /// 伤害影响
    /// </summary>
    public class DamageInfluence : Influence
    {
        public DamageInfluence(float power, bool isFire = false) : base(InfluenceKind.Damage, power)
        {
            IsFire = isFire;
        }
    }

    /// <summary>
    /// 治疗影响
    /// </summary>
    public class HealInfluence : Influence
    {
        public HealInfluence(float amount) : base(InfluenceKind.Purify, 1.0f, amount)
        {
        }
    }

    /// <summary>
    /// 状态影响
    /// </summary>
    public class StatusInfluence : Influence
    {
        public StatusKind StatusType { get; set; }
        
        public StatusInfluence(StatusKind statusType, int duration, InfluenceKind kind = InfluenceKind.Debuff) 
            : base(kind, 1.0f, 0, duration)
        {
            StatusType = statusType;
        }
    }
}