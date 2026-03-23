using System;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.PersonDetail;

namespace GameObjects
{
    /// <summary>
    /// 战斗行动基类 - 临时存根
    /// </summary>
    public abstract class CombatAction
    {
        public float Score { get; set; }
        protected Troop SourceTroop;

        public CombatAction(Troop source = null)
        {
            SourceTroop = source;
        }

        public abstract void Execute();
        public abstract string GetDescription();
    }

    public class SkipTurnAction : CombatAction
    {
        public SkipTurnAction() : base(null) { }
        public override void Execute() { }
        public override string GetDescription() => "跳过回合";
    }

    public class MoveAndCastAction : CombatAction
    {
        private Point TargetPos;
        private Skill SkillToCast;
        private Troop TargetEnemy;

        private MoveAndCastAction() : base(null) { }
        public MoveAndCastAction(Troop source, Point pos, Skill skill, Troop target) : base(source)
        {
            TargetPos = pos;
            SkillToCast = skill;
            TargetEnemy = target;
        }

        public override void Execute() { }
        public override string GetDescription() => $"移动并释放 {SkillToCast?.Name}";
    }

    public class MoveAndAttackAction : CombatAction
    {
        private Point TargetPos;
        private Troop TargetEnemy;

        private MoveAndAttackAction() : base(null) { }
        public MoveAndAttackAction(Troop source, Point pos, Troop target) : base(source)
        {
            TargetPos = pos;
            TargetEnemy = target;
        }

        public override void Execute() { }
        public override string GetDescription() => "移动并攻击";
    }

    public class DefendAction : CombatAction
    {
        private DefendAction() : base(null) { }
        public DefendAction(Troop source) : base(source) { }
        public override void Execute() { }
        public override string GetDescription() => "防御";
    }

    public class RetreatAction : CombatAction
    {
        private Point Destination;
        private RetreatAction() : base(null) { }
        public RetreatAction(Troop source, Point dest) : base(source)
        {
            Destination = dest;
        }
        public override void Execute() { }
        public override string GetDescription() => "撤退";
    }

    public class WaitAction : CombatAction
    {
        private WaitAction() : base(null) { }
        public WaitAction(Troop source) : base(source) { }
        public override void Execute() { }
        public override string GetDescription() => "等待";
    }

    public class MoveAction : CombatAction
    {
        private Point Destination;
        private MoveAction() : base(null) { }
        public MoveAction(Troop source, Point dest) : base(source)
        {
            Destination = dest;
        }
        public override void Execute() { }
        public override string GetDescription() => "移动";
    }
}
