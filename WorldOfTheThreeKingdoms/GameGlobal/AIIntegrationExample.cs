using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// AI集成示例：展示如何将战术评估器集成到现有的AI系统中
    /// </summary>
    public class AIIntegrationExample
    {
        private readonly AIDecisionManager _decisionManager;
        private readonly Dictionary<int, DateTime> _lastSkillUse;

        public AIIntegrationExample()
        {
            _decisionManager = new AIDecisionManager();
            _lastSkillUse = new Dictionary<int, DateTime>();
        }

        /// <summary>
        /// 主要的AI回合处理方法
        /// </summary>
        public void ProcessAITurn(Troop aiTroop, GameScenario scenario)
        {
            try
            {
                // 1. 获取AI决策
                var decision = _decisionManager.MakeCombatDecision(aiTroop, scenario);

                // 2. 执行决策
                ExecuteDecision(aiTroop, decision, scenario);

                // 3. 记录行动
                LogAIAction(aiTroop, decision);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI] 处理AI回合时发生异常: {ex.Message}");
                // 异常时执行默认行为
                ExecuteDefaultBehavior(aiTroop, scenario);
            }
        }

        /// <summary>
        /// 执行AI决策
        /// </summary>
        private void ExecuteDecision(Troop aiTroop, AIDecision decision, GameScenario scenario)
        {
            switch (decision.Action)
            {
                case AIActionType.UseSkill:
                    ExecuteSkillAction(aiTroop, decision, scenario);
                    break;

                case AIActionType.Move:
                    ExecuteMoveAction(aiTroop, decision, scenario);
                    break;

                case AIActionType.Defend:
                    ExecuteDefendAction(aiTroop, scenario);
                    break;

                case AIActionType.Retreat:
                    ExecuteRetreatAction(aiTroop, scenario);
                    break;

                case AIActionType.Wait:
                default:
                    ExecuteWaitAction(aiTroop);
                    break;
            }
        }

        /// <summary>
        /// 执行技能行动
        /// </summary>
        private void ExecuteSkillAction(Troop aiTroop, AIDecision decision, GameScenario scenario)
        {
            if (decision.Skill == null || decision.Target == null)
            {
                System.Diagnostics.Debug.WriteLine("[AI] 技能或目标为空，跳过技能行动");
                return;
            }

            // 检查冷却时间
            var skillKey = aiTroop.ID * 1000 + decision.Skill.ID;
            if (_lastSkillUse.ContainsKey(skillKey))
            {
                var timeSinceLastUse = DateTime.Now - _lastSkillUse[skillKey];
                if (timeSinceLastUse.TotalSeconds < decision.Skill.Cooldown)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI] 技能 {decision.Skill.Name} 仍在冷却中");
                    return;
                }
            }

            // 验证资源
            if (aiTroop.CurrentPrestige < decision.Skill.Cost)
            {
                System.Diagnostics.Debug.WriteLine($"[AI] 气力不足，无法使用技能 {decision.Skill.Name}");
                return;
            }

            // 执行技能
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 对 {decision.Target.Name} 使用 {decision.Skill.Name}");
                
                // 消耗气力
                aiTroop.CurrentPrestige -= decision.Skill.Cost;
                
                // 应用技能效果
                ApplySkillEffects(aiTroop, decision.Skill, decision.Target, scenario);
                
                // 记录使用时间
                _lastSkillUse[skillKey] = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI] 执行技能时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用技能效果
        /// </summary>
        private void ApplySkillEffects(Troop caster, Skill skill, Troop primaryTarget, GameScenario scenario)
        {
            // 获取AOE范围内的所有目标
            var targets = scenario.GetTroopsInRadius(primaryTarget.Position, skill.Radius);
            if (targets == null || !targets.Any())
            {
                targets = new List<Troop> { primaryTarget };
            }

            foreach (var target in targets)
            {
                foreach (var influence in skill.Influences)
                {
                    ApplyInfluence(caster, influence, target, scenario);
                }
            }
        }

        /// <summary>
        /// 应用单个影响效果
        /// </summary>
        private void ApplyInfluence(Troop caster, Influence influence, Troop target, GameScenario scenario)
        {
            // 成功率检查
            if (influence.SuccessRate < 1.0f)
            {
                var random = new Random();
                if (random.NextDouble() > influence.SuccessRate)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI] 影响效果失败 (成功率: {influence.SuccessRate:P})");
                    return;
                }
            }

            switch (influence.Kind)
            {
                case InfluenceKind.Damage:
                    ApplyDamage(caster, influence, target, scenario);
                    break;

                case InfluenceKind.Purify:
                    ApplyHealing(caster, influence, target);
                    break;

                case InfluenceKind.Stun:
                case InfluenceKind.Confusion:
                    ApplyStatusEffect(influence, target);
                    break;

                case InfluenceKind.Buff:
                case InfluenceKind.Debuff:
                    ApplyStatusEffect(influence, target);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"[AI] 未处理的影响类型: {influence.Kind}");
                    break;
            }
        }

        /// <summary>
        /// 应用伤害效果
        /// </summary>
        private void ApplyDamage(Troop caster, Influence influence, Troop target, GameScenario scenario)
        {
            float baseDamage = GameGlobal.Parameters.ReferDamage(caster, target) * influence.Power;
            
            // 特殊效果处理
            if (influence.IsFire)
            {
                var terrainKind = scenario.GetTerrainKind(target.Position);
                if (terrainKind == TerrainKind.Forest)
                {
                    baseDamage *= 1.5f;
                    System.Diagnostics.Debug.WriteLine("[AI] 火计在森林中威力增强!");
                }
                
                if (target.HasTech("RattanArmor"))
                {
                    baseDamage *= 2.0f;
                    System.Diagnostics.Debug.WriteLine("[AI] 火计对藤甲兵造成额外伤害!");
                }
            }

            int finalDamage = Math.Max(1, (int)baseDamage);
            target.CurrentHP = Math.Max(0, target.CurrentHP - finalDamage);
            
            System.Diagnostics.Debug.WriteLine($"[AI] {caster.Name} 对 {target.Name} 造成 {finalDamage} 点伤害");
            
            // 检查是否击杀
            if (target.CurrentHP <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AI] {target.Name} 被击败!");
                HandleTroopDefeat(target, scenario);
            }
        }

        /// <summary>
        /// 应用治疗效果
        /// </summary>
        private void ApplyHealing(Troop caster, Influence influence, Troop target)
        {
            if (!caster.IsFriend(target)) return;

            int healAmount = (int)influence.Amount;
            int actualHeal = Math.Min(healAmount, target.MaxHP - target.CurrentHP);
            
            if (actualHeal > 0)
            {
                target.CurrentHP += actualHeal;
                System.Diagnostics.Debug.WriteLine($"[AI] {caster.Name} 为 {target.Name} 恢复 {actualHeal} 点生命值");
            }
        }

        /// <summary>
        /// 应用状态效果
        /// </summary>
        private void ApplyStatusEffect(Influence influence, Troop target)
        {
            // 这里需要根据实际的状态系统实现
            System.Diagnostics.Debug.WriteLine($"[AI] 对 {target.Name} 应用状态效果: {influence.Kind}");
            
            // 示例实现
            if (influence is StatusInfluence statusInfluence)
            {
                // target.AddStatus(statusInfluence.StatusType, statusInfluence.Duration);
            }
        }

        /// <summary>
        /// 执行移动行动
        /// </summary>
        private void ExecuteMoveAction(Troop aiTroop, AIDecision decision, GameScenario scenario)
        {
            if (decision.TargetPosition.HasValue)
            {
                var targetPos = decision.TargetPosition.Value;
                System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 移动到 ({targetPos.X}, {targetPos.Y})");
                
                // 执行移动逻辑
                // aiTroop.MoveTo(targetPos);
            }
        }

        /// <summary>
        /// 执行防御行动
        /// </summary>
        private void ExecuteDefendAction(Troop aiTroop, GameScenario scenario)
        {
            System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 进入防御状态");
            // aiTroop.SetDefending(true);
        }

        /// <summary>
        /// 执行撤退行动
        /// </summary>
        private void ExecuteRetreatAction(Troop aiTroop, GameScenario scenario)
        {
            System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 开始撤退");
            // 实现撤退逻辑
        }

        /// <summary>
        /// 执行等待行动
        /// </summary>
        private void ExecuteWaitAction(Troop aiTroop)
        {
            System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 等待中...");
            // 可能恢复少量气力或执行其他被动效果
        }

        /// <summary>
        /// 执行默认行为（异常时的后备方案）
        /// </summary>
        private void ExecuteDefaultBehavior(Troop aiTroop, GameScenario scenario)
        {
            System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name} 执行默认行为");
            
            // 简单的默认行为：寻找最近的敌人并攻击
            var nearestEnemy = scenario.GetNearestEnemy(aiTroop);
            if (nearestEnemy != null)
            {
                // 如果在攻击范围内，进行普通攻击
                var distance = CalculateDistance(aiTroop.Position, nearestEnemy.Position);
                if (distance <= aiTroop.AttackRange)
                {
                    PerformBasicAttack(aiTroop, nearestEnemy);
                }
                else
                {
                    // 否则向敌人移动
                    MoveTowards(aiTroop, nearestEnemy.Position);
                }
            }
        }

        /// <summary>
        /// 执行基础攻击
        /// </summary>
        private void PerformBasicAttack(Troop attacker, Troop target)
        {
            int damage = GameGlobal.Parameters.ReferDamage(attacker, target);
            target.CurrentHP = Math.Max(0, target.CurrentHP - damage);
            
            System.Diagnostics.Debug.WriteLine($"[AI] {attacker.Name} 对 {target.Name} 进行基础攻击，造成 {damage} 点伤害");
            
            if (target.CurrentHP <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AI] {target.Name} 被击败!");
            }
        }

        /// <summary>
        /// 向目标位置移动
        /// </summary>
        private void MoveTowards(Troop troop, Point targetPosition)
        {
            var dx = Math.Sign(targetPosition.X - troop.Position.X);
            var dy = Math.Sign(targetPosition.Y - troop.Position.Y);
            
            var newPosition = new Point(troop.Position.X + dx, troop.Position.Y + dy);
            System.Diagnostics.Debug.WriteLine($"[AI] {troop.Name} 向 ({newPosition.X}, {newPosition.Y}) 移动");
            
            // troop.MoveTo(newPosition);
        }

        /// <summary>
        /// 处理部队败北
        /// </summary>
        private void HandleTroopDefeat(Troop defeatedTroop, GameScenario scenario)
        {
            // 实现败北处理逻辑
            // scenario.RemoveTroop(defeatedTroop);
        }

        /// <summary>
        /// 记录AI行动日志
        /// </summary>
        private void LogAIAction(Troop aiTroop, AIDecision decision)
        {
            var actionDescription = decision.Action switch
            {
                AIActionType.UseSkill => $"使用技能 {decision.Skill?.Name} (评分: {decision.Score:F1})",
                AIActionType.Move => $"移动到 {decision.TargetPosition} (评分: {decision.Score:F1})",
                AIActionType.Defend => $"防御 (评分: {decision.Score:F1})",
                AIActionType.Retreat => $"撤退 (评分: {decision.Score:F1})",
                _ => $"等待 (评分: {decision.Score:F1})"
            };

            System.Diagnostics.Debug.WriteLine($"[AI] {aiTroop.Name}: {actionDescription}");
        }

        /// <summary>
        /// 计算距离
        /// </summary>
        private int CalculateDistance(Point pos1, Point pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _decisionManager?.CleanupCache();
            _lastSkillUse?.Clear();
        }
    }
}