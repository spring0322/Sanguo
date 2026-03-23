using GameObjects;
using GameObjects.Conditions;
using GameObjects.Influences;
using System;
using GameObjects.Animations;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    [DataContract]
    public class CombatMethod : GameObject
    {
        private TileAnimationKind animationKind;
        
        public Influence AI;
        private bool architectureTarget;
        private AttackDefaultKind attackDefault;
        private AttackTargetKind attackTarget;

        public ConditionTable CastConditions = new ConditionTable();
        private int combativity;
        private string description;

        public InfluenceTable Influences = new InfluenceTable();
        private bool viewingHostile;

        public Dictionary<Condition, float> AIConditionWeightSelf = new Dictionary<Condition, float>();

        public Dictionary<Condition, float> AIConditionWeightEnemy = new Dictionary<Condition, float>();

        public void Init()
        {
            CastConditions = new ConditionTable();
            Influences = new InfluenceTable();
            AIConditionWeightSelf = new Dictionary<Condition, float>();
            AIConditionWeightEnemy = new Dictionary<Condition, float>();
        }

        [DataMember]
        public string InfluencesString
        {
            get;
            set;
        }

        [DataMember]
        public string CastConditionsString
        {
            get;
            set;
        }

        [DataMember]
        public string AIConditionWeightSelfString
        {
            get;
            set;
        }

        [DataMember]
        public string AIConditionWeightEnemyString
        {
            get;
            set;
        }

        public void Apply(Troop troop)
        {
            if ((troop.Combativity + troop.DecrementOfCombatMethodCombativityConsuming) >= this.Combativity)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[CombatMethod.Apply] {troop.DisplayName} 施放战法 {this.Name}(ID:{this.ID})");
                System.Diagnostics.Debug.WriteLine($"  - 战意消耗: {this.Combativity}, 当前战意: {troop.Combativity}");
                System.Diagnostics.Debug.WriteLine($"  - 影响数量: {this.Influences.Influences.Count}");
#endif
                
                troop.CombatMethodApplied = true;
                troop.DecreaseCombativity(this.Combativity - troop.DecrementOfCombatMethodCombativityConsuming);
                troop.ShowNumber = true;
                
                foreach (Influence influence in this.Influences.Influences.Values)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  - 应用影响: {influence.GetType().Name}");
#endif
                    influence.ApplyInfluence(troop.Leader, Applier.CombatMethod, 0, false);
                }
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[CombatMethod.Apply] 战法施放完成，CombatMethodApplied={troop.CombatMethodApplied}");
#endif
            }
#if DEBUG
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CombatMethod.Apply] {troop.DisplayName} 战意不足，无法施放战法 {this.Name}");
                System.Diagnostics.Debug.WriteLine($"  - 需要战意: {this.Combativity}, 当前战意: {troop.Combativity}");
            }
#endif
        }

        /// <summary>
        /// 计算战法对目标的收益评分
        /// 日期：2026-03-09
        /// </summary>
        public int GetCredit(Troop source, Troop destination)
        {
            // ⚠️ 数据完整性断言：Influences 应该在初始化时加载
            System.Diagnostics.Debug.Assert(this.Influences != null,
                $"[CombatMethod.GetCredit] 战法 {this.Name} 的 Influences 为 null，检查战法数据加载逻辑");
            System.Diagnostics.Debug.Assert(this.Influences.Influences != null,
                $"[CombatMethod.GetCredit] 战法 {this.Name} 的 Influences.Influences 为 null，检查战法数据加载逻辑");

            int num = 0;
            
            // 🔥 热路径优化：使用 for 循环替代 foreach
            var influences = this.Influences.Influences.Values;
            int count = influences.Count;
            
            // 注意：Dictionary.Values 返回 ValueCollection，需要转换为数组或使用 foreach
            // 这里保持 foreach 因为 ValueCollection 不支持索引访问
            foreach (Influence influence in influences)
            {
                num += influence.GetCredit(source, destination);
            }
            
            return num;
        }

        public bool IsCastable(Troop troop)
        {
            return Condition.CheckConditionList(this.CastConditions.Conditions.Values, troop);
        }

        public void Purify(Troop troop)
        {
            if (troop.CombatMethodApplied)
            {
                troop.CombatMethodApplied = false;
                foreach (Influence influence in this.Influences.Influences.Values)
                {
                    influence.PurifyInfluence(troop.Leader, Applier.CombatMethod, 0, false);
                }
            }
        }

        public bool SimulateApply(Troop troop)
        {
            foreach (Influence influence in this.Influences.Influences.Values)
            {
                influence.ApplyInfluence(troop.Leader, Applier.CombatMethod, 0, false);
            }
            return true;
        }

        public void SimulatePurify(Troop troop)
        {
            foreach (Influence influence in this.Influences.Influences.Values)
            {
                influence.PurifyInfluence(troop.Leader, Applier.CombatMethod, 0, false);
            }
        }

        [DataMember]
        public bool ArchitectureTarget
        {
            get
            {
                return this.architectureTarget;
            }
            set
            {
                this.architectureTarget = value;
            }
        }

        [DataMember]
        public int AttackDefaultString { get; set; }

        [DataMember]
        public int AttackTargetString { get; set; }
        
        //[DataMember]
        public AttackDefaultKind AttackDefault
        {
            get
            {
                return this.attackDefault;
            }
            set
            {
                this.attackDefault = value;
            }
        }

        //[DataMember]
        public AttackTargetKind AttackTarget
        {
            get
            {
                return this.attackTarget;
            }
            set
            {
                this.attackTarget = value;
            }
        }

        [DataMember]
        public int Combativity
        {
            get
            {
                return this.combativity;
            }
            set
            {
                this.combativity = value;
            }
        }
        [DataMember]
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }
        [DataMember]
        public bool ViewingHostile
        {
            get
            {
                return this.viewingHostile;
            }
            set
            {
                this.viewingHostile = value;
            }
        }
        [DataMember]
        public TileAnimationKind AnimationKind
        {
            get
            {
                return this.animationKind;
            }
            set
            {
                this.animationKind = value;
            }
        }
    }
}

