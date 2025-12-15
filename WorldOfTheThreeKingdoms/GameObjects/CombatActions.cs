using System;
using System.Collections.Generic;
using GameObjects.Influences;

namespace GameObjects
{
    /// <summary>
    /// 战斗行动基类 - 使用行动模式(Action Pattern)
    /// </summary>
    public abstract class CombatAction
    {
        public float Score { get; set; }
        public abstract void Execute();
        public abstract string GetDescription();
    }

    /// <summary>
    /// 跳过回合行动
    /// </summary>
    public class SkipTurnAction : CombatAction
    {
        public override void Execute()
        {
            // 什么都不做，跳过回合
            System.Diagnostics.Debug.WriteLine("[CombatAction] 跳过回合");
        }

        public override string GetDescription()
        {
            return "跳过回合";
        }
    }

    /// <summary>
    /// 移动并施放技能行动
    /// </summary>
    public class MoveAndCastAction : CombatAction
    {
        public SmartTroop Actor { get; set; }
        public Point MoveDestination { get; set; }
        public Skill Skill { get; set; }
        public Troop Target { get; set; }

        public MoveAndCastAction(SmartTroop actor, Point destination, Skill skill, Troop target)
        {
            Actor = actor;
            MoveDestination = destination;
            Skill = skill;
            Target = target;
        }

        public override void Execute()
        {
            try
            {
                // 1. 移动到目标位置
                if (MoveDestination != Actor.Position)
                {
                    Actor.MoveTo(MoveDestination);
                }

                // 2. 施放技能
                if (Actor.CurrentPrestige >= Skill.Cost)
                {
                    Actor.CastSkill(Skill, Target);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CombatAction] {Actor.Name} 气力不足，无法使用 {Skill.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行移动并施法时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"移动到({MoveDestination.X}, {MoveDestination.Y})并对{Target.Name}使用{Skill.Name}";
        }
    }

    /// <summary>
    /// 移动并攻击行动
    /// </summary>
    public class MoveAndAttackAction : CombatAction
    {
        public SmartTroop Actor { get; set; }
        public Point MoveDestination { get; set; }
        public Troop Target { get; set; }

        public MoveAndAttackAction(SmartTroop actor, Point destination, Troop target)
        {
            Actor = actor;
            MoveDestination = destination;
            Target = target;
        }

        public override void Execute()
        {
            try
            {
                // 1. 移动到目标位置
                if (MoveDestination != Actor.Position)
                {
                    Actor.MoveTo(MoveDestination);
                }

                // 2. 攻击目标
                Actor.Attack(Target);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行移动并攻击时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"移动到({MoveDestination.X}, {MoveDestination.Y})并攻击{Target.Name}";
        }
    }

    /// <summary>
    /// 纯移动行动
    /// </summary>
    public class MoveAction : CombatAction
    {
        public SmartTroop Actor { get; set; }
        public Point MoveDestination { get; set; }

        public MoveAction(SmartTroop actor, Point destination)
        {
            Actor = actor;
            MoveDestination = destination;
        }

        public override void Execute()
        {
            try
            {
                Actor.MoveTo(MoveDestination);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行移动时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"移动到({MoveDestination.X}, {MoveDestination.Y})";
        }
    }

    /// <summary>
    /// 防御行动
    /// </summary>
    public class DefendAction : CombatAction
    {
        public SmartTroop Actor { get; set; }

        public DefendAction(SmartTroop actor)
        {
            Actor = actor;
        }

        public override void Execute()
        {
            try
            {
                // 进入防御状态，可能提升防御力或减少受到的伤害
                System.Diagnostics.Debug.WriteLine($"[CombatAction] {Actor.Name} 进入防御状态");
                
                // 这里可以添加防御状态的具体效果
                // 例如：Actor.AddStatus(StatusKind.DefenseBoost, 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行防御时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return "进入防御状态";
        }
    }

    /// <summary>
    /// 撤退行动
    /// </summary>
    public class RetreatAction : CombatAction
    {
        public SmartTroop Actor { get; set; }
        public Point RetreatDestination { get; set; }

        public RetreatAction(SmartTroop actor, Point destination)
        {
            Actor = actor;
            RetreatDestination = destination;
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] {Actor.Name} 撤退到({RetreatDestination.X}, {RetreatDestination.Y})");
                Actor.MoveTo(RetreatDestination);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行撤退时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"撤退到({RetreatDestination.X}, {RetreatDestination.Y})";
        }
    }

    /// <summary>
    /// 等待行动
    /// </summary>
    public class WaitAction : CombatAction
    {
        public SmartTroop Actor { get; set; }

        public WaitAction(SmartTroop actor)
        {
            Actor = actor;
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] {Actor.Name} 等待中...");
                
                // 等待可能有一些好处，比如恢复少量气力
                if (Actor.CurrentPrestige < Actor.MaxPrestige)
                {
                    Actor.CurrentPrestige = Math.Min(Actor.MaxPrestige, Actor.CurrentPrestige + 5);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行等待时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return "等待并恢复气力";
        }
    }

    /// <summary>
    /// 使用物品行动
    /// </summary>
    public class UseItemAction : CombatAction
    {
        public SmartTroop Actor { get; set; }
        public string ItemName { get; set; }
        public Troop Target { get; set; }

        public UseItemAction(SmartTroop actor, string itemName, Troop target = null)
        {
            Actor = actor;
            ItemName = itemName;
            Target = target ?? actor; // 默认对自己使用
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] {Actor.Name} 使用物品 {ItemName}");
                
                // 这里需要根据实际的物品系统来实现
                // 例如：Actor.UseItem(ItemName, Target);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 使用物品时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"使用物品 {ItemName}" + (Target != Actor ? $" 对 {Target.Name}" : "");
        }
    }

    /// <summary>
    /// 组合行动 - 可以包含多个子行动
    /// </summary>
    public class ComboAction : CombatAction
    {
        public List<CombatAction> SubActions { get; set; }
        public string ComboName { get; set; }

        public ComboAction(string comboName)
        {
            ComboName = comboName;
            SubActions = new List<CombatAction>();
        }

        public void AddAction(CombatAction action)
        {
            SubActions.Add(action);
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行组合行动: {ComboName}");
                
                foreach (var action in SubActions)
                {
                    action.Execute();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombatAction] 执行组合行动时发生异常: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"组合行动: {ComboName} (包含{SubActions.Count}个子行动)";
        }
    }
}