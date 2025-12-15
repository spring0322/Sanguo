using System;
using System.Collections.Generic;
using GameObjects.Influences;

namespace GameObjects
{
    /// <summary>
    /// 技能基类
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public int ID { get; set; }
        
        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 技能描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 消耗的气力值
        /// </summary>
        public int Cost { get; set; }
        
        /// <summary>
        /// 技能威力
        /// </summary>
        public float Power { get; set; }
        
        /// <summary>
        /// 技能范围（格数）
        /// </summary>
        public int Range { get; set; }
        
        /// <summary>
        /// AOE半径
        /// </summary>
        public int Radius { get; set; }
        
        /// <summary>
        /// 冷却时间（回合数）
        /// </summary>
        public int Cooldown { get; set; }
        
        /// <summary>
        /// 技能影响列表
        /// </summary>
        public List<Influence> Influences { get; set; }
        
        /// <summary>
        /// 是否为伤害技能
        /// </summary>
        public bool IsDamage => Influences.Exists(i => i.Kind == InfluenceKind.Damage);
        
        /// <summary>
        /// 是否为治疗技能
        /// </summary>
        public bool IsHealing => Influences.Exists(i => i.Kind == InfluenceKind.Purify);
        
        /// <summary>
        /// 是否为控制技能
        /// </summary>
        public bool IsControl => Influences.Exists(i => 
            i.Kind == InfluenceKind.Stun || 
            i.Kind == InfluenceKind.Confusion);
        
        /// <summary>
        /// 是否为增益技能
        /// </summary>
        public bool IsBuff => Influences.Exists(i => i.Kind == InfluenceKind.Buff);
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Skill()
        {
            Influences = new List<Influence>();
            Range = 1;
            Radius = 0;
            Cooldown = 0;
        }
    }

    /// <summary>
    /// 技能工厂类 - 创建预定义的技能
    /// </summary>
    public static class SkillFactory
    {
        /// <summary>
        /// 创建火计技能
        /// </summary>
        public static Skill CreateFireAttack()
        {
            return new Skill
            {
                ID = 1001,
                Name = "火计",
                Description = "对敌军造成火属性伤害，在森林地形威力翻倍",
                Cost = 30,
                Power = 1.5f,
                Range = 3,
                Radius = 2,
                Cooldown = 3,
                Influences = new List<Influence>
                {
                    new DamageInfluence(1.5f, isFire: true)
                }
            };
        }

        /// <summary>
        /// 创建治疗术
        /// </summary>
        public static Skill CreateHeal()
        {
            return new Skill
            {
                ID = 1002,
                Name = "治疗术",
                Description = "恢复友军生命值",
                Cost = 20,
                Power = 1.0f,
                Range = 2,
                Radius = 1,
                Cooldown = 2,
                Influences = new List<Influence>
                {
                    new HealInfluence(150)
                }
            };
        }

        /// <summary>
        /// 创建混乱术
        /// </summary>
        public static Skill CreateConfusion()
        {
            return new Skill
            {
                ID = 1003,
                Name = "混乱术",
                Description = "使敌军陷入混乱状态，无法正常行动",
                Cost = 25,
                Power = 1.0f,
                Range = 4,
                Radius = 0,
                Cooldown = 4,
                Influences = new List<Influence>
                {
                    new StatusInfluence(StatusKind.Confusion, 3, InfluenceKind.Confusion)
                }
            };
        }

        /// <summary>
        /// 创建雷击术
        /// </summary>
        public static Skill CreateThunderStrike()
        {
            return new Skill
            {
                ID = 1004,
                Name = "雷击术",
                Description = "召唤雷电攻击敌军，有概率造成晕眩",
                Cost = 35,
                Power = 2.0f,
                Range = 5,
                Radius = 1,
                Cooldown = 5,
                Influences = new List<Influence>
                {
                    new DamageInfluence(2.0f) { IsThunder = true },
                    new StatusInfluence(StatusKind.Stun, 2, InfluenceKind.Stun) { SuccessRate = 0.3f }
                }
            };
        }

        /// <summary>
        /// 创建鼓舞术
        /// </summary>
        public static Skill CreateInspire()
        {
            return new Skill
            {
                ID = 1005,
                Name = "鼓舞",
                Description = "提升友军的攻击力和士气",
                Cost = 40,
                Power = 1.0f,
                Range = 3,
                Radius = 2,
                Cooldown = 6,
                Influences = new List<Influence>
                {
                    new StatusInfluence(StatusKind.AttackBoost, 5, InfluenceKind.Buff),
                    new StatusInfluence(StatusKind.SpeedBoost, 5, InfluenceKind.Buff)
                }
            };
        }

        /// <summary>
        /// 创建冰冻术
        /// </summary>
        public static Skill CreateFreeze()
        {
            return new Skill
            {
                ID = 1006,
                Name = "冰冻术",
                Description = "冰冻敌军，降低其移动速度",
                Cost = 28,
                Power = 1.2f,
                Range = 4,
                Radius = 1,
                Cooldown = 4,
                Influences = new List<Influence>
                {
                    new DamageInfluence(1.2f) { IsIce = true },
                    new StatusInfluence(StatusKind.SpeedDebuff, 4, InfluenceKind.Debuff)
                }
            };
        }

        /// <summary>
        /// 创建群体治疗
        /// </summary>
        public static Skill CreateMassHeal()
        {
            return new Skill
            {
                ID = 1007,
                Name = "群体治疗",
                Description = "治疗大范围内的所有友军",
                Cost = 50,
                Power = 1.0f,
                Range = 2,
                Radius = 3,
                Cooldown = 8,
                Influences = new List<Influence>
                {
                    new HealInfluence(100),
                    new StatusInfluence(StatusKind.DefenseBoost, 3, InfluenceKind.Buff)
                }
            };
        }

        /// <summary>
        /// 创建谣言术 - 降低敌军士气
        /// </summary>
        public static Skill CreateRumor()
        {
            return new Skill
            {
                ID = 1008,
                Name = "谣言",
                Description = "散布谣言，大幅降低敌军士气，可能导致敌军逃跑",
                Cost = 35,
                Power = 1.0f,
                Range = 4,
                Radius = 2,
                Cooldown = 6,
                Influences = new List<Influence>
                {
                    new Influence(InfluenceKind.MoraleDown, 1.0f, 30, 0) // 降低30点士气
                }
            };
        }

        /// <summary>
        /// 创建诱敌术 - 强制移动敌人到指定位置
        /// </summary>
        public static Skill CreateLure()
        {
            return new Skill
            {
                ID = 1009,
                Name = "诱敌",
                Description = "诱惑敌军移动到指定位置，可配合包围战术",
                Cost = 30,
                Power = 1.0f,
                Range = 3,
                Radius = 1,
                Cooldown = 4,
                Influences = new List<Influence>
                {
                    new Influence(InfluenceKind.Lure, 1.0f, 1, 0) // 移动1格
                }
            };
        }

        /// <summary>
        /// 创建反间计 - 让敌人攻击自己的队友
        /// </summary>
        public static Skill CreateInternalStrife()
        {
            return new Skill
            {
                ID = 1010,
                Name = "反间计",
                Description = "挑拨敌军内部关系，使其攻击自己的队友",
                Cost = 40,
                Power = 1.0f,
                Range = 2,
                Radius = 1,
                Cooldown = 8,
                Influences = new List<Influence>
                {
                    new Influence(InfluenceKind.InternalStrife, 1.0f, 0, 2) // 持续2回合
                }
            };
        }

        /// <summary>
        /// 创建水攻术 - 水属性攻击，在水域威力增强
        /// </summary>
        public static Skill CreateWaterAttack()
        {
            return new Skill
            {
                ID = 1011,
                Name = "水攻",
                Description = "召唤洪水攻击敌军，在河流和沼泽地形威力翻倍",
                Cost = 35,
                Power = 1.3f,
                Range = 3,
                Radius = 2,
                Cooldown = 5,
                Influences = new List<Influence>
                {
                    new Influence(InfluenceKind.Damage, 1.3f, 0, 0) { IsWater = true }
                }
            };
        }

        /// <summary>
        /// 创建强化雷击术 - 天气敏感的雷属性攻击
        /// </summary>
        public static Skill CreateEnhancedThunderStrike()
        {
            return new Skill
            {
                ID = 1012,
                Name = "天雷",
                Description = "召唤天雷攻击敌军，雷雨天威力大增",
                Cost = 45,
                Power = 1.8f,
                Range = 4,
                Radius = 1,
                Cooldown = 6,
                Influences = new List<Influence>
                {
                    new Influence(InfluenceKind.Damage, 1.8f, 0, 0) { IsThunder = true },
                    new StatusInfluence(StatusKind.Stun, 1, InfluenceKind.Stun) { SuccessRate = 0.4f }
                }
            };
        }

        /// <summary>
        /// 获取所有预定义技能
        /// </summary>
        public static List<Skill> GetAllSkills()
        {
            return new List<Skill>
            {
                CreateFireAttack(),
                CreateHeal(),
                CreateConfusion(),
                CreateThunderStrike(),
                CreateInspire(),
                CreateFreeze(),
                CreateMassHeal(),
                CreateRumor(),
                CreateLure(),
                CreateInternalStrife(),
                CreateWaterAttack(),
                CreateEnhancedThunderStrike()
            };
        }

        /// <summary>
        /// 根据ID获取技能
        /// </summary>
        public static Skill GetSkillById(int id)
        {
            var allSkills = GetAllSkills();
            return allSkills.Find(s => s.ID == id);
        }
    }

    /// <summary>
    /// 技能学习系统
    /// </summary>
    public class SkillLearningSystem
    {
        private static readonly Dictionary<int, List<int>> PersonSkills = new Dictionary<int, List<int>>();

        /// <summary>
        /// 为人物添加技能
        /// </summary>
        public static void LearnSkill(int personId, int skillId)
        {
            if (!PersonSkills.ContainsKey(personId))
            {
                PersonSkills[personId] = new List<int>();
            }

            if (!PersonSkills[personId].Contains(skillId))
            {
                PersonSkills[personId].Add(skillId);
            }
        }

        /// <summary>
        /// 获取人物的所有技能
        /// </summary>
        public static List<Skill> GetPersonSkills(int personId)
        {
            if (!PersonSkills.ContainsKey(personId))
            {
                return new List<Skill>();
            }

            var skills = new List<Skill>();
            foreach (var skillId in PersonSkills[personId])
            {
                var skill = SkillFactory.GetSkillById(skillId);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }

            return skills;
        }

        /// <summary>
        /// 初始化默认技能配置
        /// </summary>
        public static void InitializeDefaultSkills()
        {
            // 诸葛亮 - 智力型技能
            LearnSkill(1, 1001); // 火计
            LearnSkill(1, 1003); // 混乱术
            LearnSkill(1, 1005); // 鼓舞

            // 华佗 - 治疗型技能
            LearnSkill(2, 1002); // 治疗术
            LearnSkill(2, 1007); // 群体治疗

            // 张角 - 法术型技能
            LearnSkill(3, 1004); // 雷击术
            LearnSkill(3, 1006); // 冰冻术
            LearnSkill(3, 1003); // 混乱术
        }
    }
}