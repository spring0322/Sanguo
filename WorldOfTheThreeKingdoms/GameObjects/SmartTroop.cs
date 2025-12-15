using System;
using System.Collections.Generic;
using System.Linq;
using GameGlobal;
using GameObjects.Influences;
using Microsoft.Xna.Framework;

namespace GameObjects
{
    /// <summary>
    /// 智能AI部队类 - 集成了完整的战术决策系统
    /// </summary>
    public class SmartTroop : Troop
    {
        // AI记忆和势能系统
        private Dictionary<Point, float> _memoryMap;
        private Dictionary<Point, float> _potentialMap;
        private DateTime _lastMemoryUpdate;
        private readonly TimeSpan _memoryUpdateInterval = TimeSpan.FromSeconds(2);

        // 战术评估系统
        private readonly AIDecisionManager _aiDecisionManager;
        private CombatPlan _lastPlan;

        public SmartTroop() : base()
        {
            _memoryMap = new Dictionary<Point, float>();
            _potentialMap = new Dictionary<Point, float>();
            _aiDecisionManager = new AIDecisionManager();
            _lastMemoryUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// 智能回合执行 - 整合移动、技能、普攻的综合决策
        /// </summary>
        public void ExecuteSmartTurn()
        {
            try
            {
                // A. 记忆与势能更新
                this.UpdateMemory();

                // B. 寻找最佳行动 (遍历：我能走到的所有位置 + 我能用的所有技能)
                var bestPlan = GetBestCombatPlan();

                // C. 执行行动
                if (bestPlan.MoveDestination != this.Position)
                {
                    this.MoveTo(bestPlan.MoveDestination);
                }

                if (bestPlan.SkillToCast != null)
                {
                    this.CastSkill(bestPlan.SkillToCast, bestPlan.Target);
                }
                else if (bestPlan.Target != null)
                {
                    this.Attack(bestPlan.Target); // 普攻
                }

                // 记录执行的计划
                _lastPlan = bestPlan;
                LogAIAction(bestPlan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartTroop] {Name} 执行智能回合时发生异常: {ex.Message}");
                // 异常时执行基础行为
                ExecuteFallbackBehavior();
            }
        }

        /// <summary>
        /// 获取最佳战斗计划
        /// </summary>
        private CombatPlan GetBestCombatPlan()
        {
            CombatPlan bestPlan = new CombatPlan 
            { 
                MoveDestination = this.Position, 
                Score = -99999,
                ActionType = CombatActionType.Wait
            };

            // 1. 获取所有能走到的格子 (包含当前位置)
            var movableTiles = this.GetMovableNeighbors(this.Position);
            movableTiles.Add(this.Position);

            foreach (var tile in movableTiles)
            {
                // 假设我站在 tile 这个位置
                // 搜索射程内的敌人 (这里取最大射程简化搜索)
                var enemiesInRange = GetEnemiesInRange(tile, GetMaxSkillRange());

                foreach (var enemy in enemiesInRange)
                {
                    if (!this.IsEnemy(enemy)) continue;

                    // 2. 评估所有技能
                    foreach (var skill in this.AvailableSkills)
                    {
                        if (this.CurrentPrestige < skill.Cost) continue; // 气力不足

                        // 检查技能射程
                        float distance = CalculateDistance(tile, enemy.Position);
                        if (distance > skill.Range) continue;

                        // 用 CombatEvaluator 算分
                        float score = CombatEvaluator.EvaluateSkill(this, skill, enemy, GetCurrentScenario());

                        // 扣除移动成本 (防止为了多打一点血跑太远，导致掉队)
                        float moveCost = CalculateDistance(this.Position, tile) * 5.0f;
                        score -= moveCost;

                        // 添加位置优势评估
                        score += EvaluatePositionalAdvantage(tile, enemy.Position);

                        // 添加势能图影响
                        score += GetPotentialScore(tile);

                        if (score > bestPlan.Score)
                        {
                            bestPlan.MoveDestination = tile;
                            bestPlan.SkillToCast = skill;
                            bestPlan.Target = enemy;
                            bestPlan.Score = score;
                            bestPlan.ActionType = CombatActionType.UseSkill;
                        }
                    }

                    // 3. 评估普攻 (保底逻辑)
                    // 只有当距离符合普攻范围时才算
                    if (CalculateDistance(tile, enemy.Position) <= this.AttackRange)
                    {
                        float attackScore = CalculateBasicAttackScore(enemy);

                        // 扣除移动成本
                        attackScore -= CalculateDistance(this.Position, tile) * 5.0f;

                        // 添加位置和势能评估
                        attackScore += EvaluatePositionalAdvantage(tile, enemy.Position);
                        attackScore += GetPotentialScore(tile);

                        if (attackScore > bestPlan.Score)
                        {
                            bestPlan.MoveDestination = tile;
                            bestPlan.SkillToCast = null; // null 代表普攻
                            bestPlan.Target = enemy;
                            bestPlan.Score = attackScore;
                            bestPlan.ActionType = CombatActionType.BasicAttack;
                        }
                    }
                }
            }

            // 如果连普攻都打不到人（Score 依然很低），则执行纯移动逻辑 (Strategic Move)
            if (bestPlan.Score < 0)
            {
                // 调用之前的势能图移动逻辑
                bestPlan.MoveDestination = this.CalculateBestStrategicMove();
                bestPlan.Target = null;
                bestPlan.SkillToCast = null;
                bestPlan.ActionType = CombatActionType.Move;
                bestPlan.Score = 0; // 移动至少不是负分
            }

            return bestPlan;
        }

        /// <summary>
        /// 更新AI记忆和势能图
        /// </summary>
        private void UpdateMemory()
        {
            if (DateTime.Now - _lastMemoryUpdate < _memoryUpdateInterval)
                return;

            try
            {
                // 更新记忆图 - 记录敌人位置和威胁
                UpdateMemoryMap();

                // 更新势能图 - 计算战略价值
                UpdatePotentialMap();

                _lastMemoryUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartTroop] 更新记忆时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新记忆图
        /// </summary>
        private void UpdateMemoryMap()
        {
            _memoryMap.Clear();

            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return;

            // 记录敌人位置和威胁程度
            foreach (var enemy in scenario.Troops.Where(t => this.IsEnemy(t)))
            {
                var threat = CalculateThreatLevel(enemy);
                var influenceRadius = Math.Max(enemy.AttackRange, 3);

                // 在敌人周围区域标记威胁
                for (int dx = -influenceRadius; dx <= influenceRadius; dx++)
                {
                    for (int dy = -influenceRadius; dy <= influenceRadius; dy++)
                    {
                        var pos = new Point(enemy.Position.X + dx, enemy.Position.Y + dy);
                        var distance = Math.Abs(dx) + Math.Abs(dy);
                        
                        if (distance <= influenceRadius)
                        {
                            var threatValue = threat * (1.0f - distance / (float)influenceRadius);
                            
                            if (_memoryMap.ContainsKey(pos))
                                _memoryMap[pos] = Math.Max(_memoryMap[pos], threatValue);
                            else
                                _memoryMap[pos] = threatValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新势能图
        /// </summary>
        private void UpdatePotentialMap()
        {
            _potentialMap.Clear();

            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return;

            // 计算各个位置的战略价值
            var mapBounds = GetMapBounds();
            
            for (int x = mapBounds.Left; x <= mapBounds.Right; x++)
            {
                for (int y = mapBounds.Top; y <= mapBounds.Bottom; y++)
                {
                    var pos = new Point(x, y);
                    var potential = CalculatePositionPotential(pos);
                    _potentialMap[pos] = potential;
                }
            }
        }

        /// <summary>
        /// 计算位置势能
        /// </summary>
        private float CalculatePositionPotential(Point position)
        {
            float potential = 0;

            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return 0;

            // 1. 靠近友军加分
            foreach (var ally in scenario.Troops.Where(t => this.IsFriend(t) && t != this))
            {
                var distance = CalculateDistance(position, ally.Position);
                if (distance <= 3)
                {
                    potential += (4 - distance) * 10; // 距离越近分数越高
                }
            }

            // 2. 远离强敌加分
            foreach (var enemy in scenario.Troops.Where(t => this.IsEnemy(t)))
            {
                var distance = CalculateDistance(position, enemy.Position);
                var threatLevel = CalculateThreatLevel(enemy);
                
                if (distance <= enemy.AttackRange + 1)
                {
                    potential -= threatLevel * 20; // 在敌人攻击范围内扣分
                }
                else if (distance <= 5)
                {
                    potential += (distance - enemy.AttackRange) * 5; // 适当距离加分
                }
            }

            // 3. 地形优势
            potential += EvaluateTerrainAdvantage(position);

            // 4. 控制要点
            potential += EvaluateStrategicValue(position);

            return potential;
        }

        /// <summary>
        /// 计算最佳战略移动位置
        /// </summary>
        private Point CalculateBestStrategicMove()
        {
            var movableTiles = GetMovableNeighbors(this.Position);
            movableTiles.Add(this.Position);

            Point bestPosition = this.Position;
            float bestScore = GetPotentialScore(this.Position);

            foreach (var tile in movableTiles)
            {
                float score = GetPotentialScore(tile);
                
                // 考虑移动成本
                float moveCost = CalculateDistance(this.Position, tile) * 2.0f;
                score -= moveCost;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = tile;
                }
            }

            return bestPosition;
        }

        /// <summary>
        /// 获取可移动的相邻位置
        /// </summary>
        private List<Point> GetMovableNeighbors(Point center)
        {
            var neighbors = new List<Point>();
            var moveRange = this.Mobility; // 假设部队有移动力属性

            for (int dx = -moveRange; dx <= moveRange; dx++)
            {
                for (int dy = -moveRange; dy <= moveRange; dy++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) <= moveRange && (dx != 0 || dy != 0))
                    {
                        var pos = new Point(center.X + dx, center.Y + dy);
                        if (IsValidPosition(pos))
                        {
                            neighbors.Add(pos);
                        }
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 获取范围内的敌人
        /// </summary>
        private List<Troop> GetEnemiesInRange(Point position, int range)
        {
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return new List<Troop>();

            return scenario.Troops
                .Where(t => this.IsEnemy(t) && CalculateDistance(position, t.Position) <= range)
                .ToList();
        }

        /// <summary>
        /// 计算基础攻击评分
        /// </summary>
        private float CalculateBasicAttackScore(Troop target)
        {
            if (target == null) return 0;

            float damage = GameGlobal.Parameters.ReferDamage(this, target);
            float effectiveDamage = Math.Min(damage, target.CurrentHP);
            
            // 基础分数
            float score = effectiveDamage;

            // 击杀奖励
            if (effectiveDamage >= target.CurrentHP)
            {
                score += 200;
                if (target.IsHero) score += 300;
            }

            // 目标价值
            if (target.IsHero) score *= 1.5f;

            return score;
        }

        /// <summary>
        /// 评估位置优势
        /// </summary>
        private float EvaluatePositionalAdvantage(Point myPosition, Point targetPosition)
        {
            float advantage = 0;

            // 地形优势
            var myTerrain = GetTerrainKind(myPosition);
            var targetTerrain = GetTerrainKind(targetPosition);

            if (myTerrain == TerrainKind.Mountain && targetTerrain == TerrainKind.Plain)
                advantage += 50; // 居高临下

            // 包围优势
            var nearbyAllies = GetAlliesNearPosition(targetPosition, 2);
            if (nearbyAllies.Count >= 2)
                advantage += nearbyAllies.Count * 25; // 协同作战

            return advantage;
        }

        /// <summary>
        /// 计算威胁等级
        /// </summary>
        private float CalculateThreatLevel(Troop enemy)
        {
            if (enemy == null) return 0;

            float threat = enemy.Attack * 0.5f + enemy.Defense * 0.3f;
            
            if (enemy.IsHero) threat *= 1.5f;
            if (enemy.AvailableSkills?.Count > 0) threat *= 1.2f;

            return threat;
        }

        /// <summary>
        /// 获取势能分数
        /// </summary>
        private float GetPotentialScore(Point position)
        {
            return _potentialMap.ContainsKey(position) ? _potentialMap[position] : 0;
        }

        /// <summary>
        /// 评估地形优势
        /// </summary>
        private float EvaluateTerrainAdvantage(Point position)
        {
            var terrain = GetTerrainKind(position);
            
            return terrain switch
            {
                TerrainKind.Mountain => 20,  // 山地防御优势
                TerrainKind.Forest => 10,    // 森林隐蔽优势
                TerrainKind.Fortress => 30,  // 要塞防御优势
                TerrainKind.River => -10,    // 河流移动劣势
                TerrainKind.Swamp => -15,    // 沼泽移动劣势
                _ => 0
            };
        }

        /// <summary>
        /// 评估战略价值
        /// </summary>
        private float EvaluateStrategicValue(Point position)
        {
            float value = 0;

            // 靠近地图中心
            var mapCenter = GetMapCenter();
            var distanceToCenter = CalculateDistance(position, mapCenter);
            value += Math.Max(0, 10 - distanceToCenter); // 越靠近中心越有价值

            // 控制要道
            if (IsStrategicPosition(position))
                value += 50;

            return value;
        }

        /// <summary>
        /// 异常时的后备行为
        /// </summary>
        private void ExecuteFallbackBehavior()
        {
            try
            {
                // 简单的后备逻辑：寻找最近的敌人
                var nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    var distance = CalculateDistance(this.Position, nearestEnemy.Position);
                    
                    if (distance <= this.AttackRange)
                    {
                        this.Attack(nearestEnemy);
                    }
                    else
                    {
                        // 向敌人移动
                        var moveTarget = GetMoveTowardsTarget(nearestEnemy.Position);
                        if (moveTarget != this.Position)
                        {
                            this.MoveTo(moveTarget);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartTroop] 后备行为也失败了: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录AI行动日志
        /// </summary>
        private void LogAIAction(CombatPlan plan)
        {
            var action = plan.ActionType switch
            {
                CombatActionType.UseSkill => $"使用 {plan.SkillToCast?.Name} 攻击 {plan.Target?.Name}",
                CombatActionType.BasicAttack => $"普攻 {plan.Target?.Name}",
                CombatActionType.Move => $"移动到 ({plan.MoveDestination.X}, {plan.MoveDestination.Y})",
                _ => "等待"
            };

            System.Diagnostics.Debug.WriteLine($"[SmartTroop] {Name}: {action} (评分: {plan.Score:F1})");
        }

        // === 辅助方法 ===

        private GameScenario GetCurrentScenario()
        {
            // 这里需要根据实际的游戏架构来获取当前场景
            return Session.Current?.Scenario != null ? 
                GameObjectAdapter.ToAIScenario(Session.Current.Scenario) : null;
        }

        private int GetMaxSkillRange()
        {
            if (AvailableSkills == null || !AvailableSkills.Any())
                return this.AttackRange;
                
            return Math.Max(this.AttackRange, AvailableSkills.Max(s => s.Range));
        }

        private float CalculateDistance(Point pos1, Point pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        private bool IsValidPosition(Point position)
        {
            // 这里需要根据实际的地图系统来判断位置是否有效
            return true; // 简化实现
        }

        private TerrainKind GetTerrainKind(Point position)
        {
            // 这里需要根据实际的地形系统来获取地形类型
            return TerrainKind.Plain; // 简化实现
        }

        private List<Troop> GetAlliesNearPosition(Point position, int range)
        {
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return new List<Troop>();

            return scenario.Troops
                .Where(t => this.IsFriend(t) && t != this && CalculateDistance(position, t.Position) <= range)
                .ToList();
        }

        private Rectangle GetMapBounds()
        {
            // 这里需要根据实际的地图系统来获取地图边界
            return new Rectangle(0, 0, 100, 100); // 简化实现
        }

        private Point GetMapCenter()
        {
            var bounds = GetMapBounds();
            return new Point(bounds.Width / 2, bounds.Height / 2);
        }

        private bool IsStrategicPosition(Point position)
        {
            // 这里可以定义战略要点的判断逻辑
            return false; // 简化实现
        }

        private Troop FindNearestEnemy()
        {
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return null;

            return scenario.Troops
                .Where(t => this.IsEnemy(t))
                .OrderBy(t => CalculateDistance(this.Position, t.Position))
                .FirstOrDefault();
        }

        private Point GetMoveTowardsTarget(Point targetPosition)
        {
            var dx = Math.Sign(targetPosition.X - this.Position.X);
            var dy = Math.Sign(targetPosition.Y - this.Position.Y);
            
            var newPosition = new Point(this.Position.X + dx, this.Position.Y + dy);
            
            return IsValidPosition(newPosition) ? newPosition : this.Position;
        }

        // === 需要在基类Troop中实现的方法 ===
        
        public virtual void MoveTo(Point destination)
        {
            // 移动实现
            this.Position = destination;
            System.Diagnostics.Debug.WriteLine($"[SmartTroop] {Name} 移动到 ({destination.X}, {destination.Y})");
        }

        public virtual void CastSkill(Skill skill, Troop target)
        {
            // 技能施放实现
            if (this.CurrentPrestige >= skill.Cost)
            {
                this.CurrentPrestige -= skill.Cost;
                System.Diagnostics.Debug.WriteLine($"[SmartTroop] {Name} 对 {target.Name} 使用 {skill.Name}");
                
                // 应用技能效果
                ApplySkillEffects(skill, target);
            }
        }

        public virtual void Attack(Troop target)
        {
            // 普攻实现
            int damage = GameGlobal.Parameters.ReferDamage(this, target);
            target.CurrentHP = Math.Max(0, target.CurrentHP - damage);
            
            System.Diagnostics.Debug.WriteLine($"[SmartTroop] {Name} 攻击 {target.Name}，造成 {damage} 点伤害");
        }

        private void ApplySkillEffects(Skill skill, Troop target)
        {
            // 应用技能的各种影响效果
            foreach (var influence in skill.Influences)
            {
                ApplyInfluenceEffect(influence, target);
            }
        }

        private void ApplyInfluenceEffect(Influence influence, Troop target)
        {
            switch (influence.Kind)
            {
                case InfluenceKind.Damage:
                    var damage = GameGlobal.Parameters.ReferDamage(this, target) * influence.Power;
                    target.CurrentHP = Math.Max(0, target.CurrentHP - (int)damage);
                    break;
                    
                case InfluenceKind.Purify:
                    if (this.IsFriend(target))
                    {
                        var heal = Math.Min((int)influence.Amount, target.MaxHP - target.CurrentHP);
                        target.CurrentHP += heal;
                    }
                    break;
                    
                // 其他影响类型的处理...
            }
        }

        // 属性扩展
        public int Mobility { get; set; } = 2; // 移动力
        public bool IsHero => PersonId > 0;
        public List<Skill> AvailableSkills { get; set; } = new List<Skill>();
        public int Intelligence { get; set; } = 50; // 智力属性
        public float HpRatio => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
        
        public bool IsFriend(Troop other) => this.BelongedFaction == other.BelongedFaction;
        public bool IsEnemy(Troop other) => !IsFriend(other);

        /// <summary>
        /// 重置AI状态
        /// </summary>
        public void ResetAIState()
        {
            _memoryMap?.Clear();
            _potentialMap?.Clear();
            _lastMemoryUpdate = DateTime.MinValue;
            _lastPlan = new CombatPlan();
        }
    }

    /// <summary>
    /// 战斗计划结构
    /// </summary>
    public struct CombatPlan
    {
        public Point MoveDestination;
        public Skill SkillToCast; // 如果为 null 则普攻
        public Troop Target;
        public float Score;
        public CombatActionType ActionType;
    }

    /// <summary>
    /// 战斗行动类型
    /// </summary>
    public enum CombatActionType
    {
        Wait,           // 等待
        Move,           // 纯移动
        BasicAttack,    // 普通攻击
        UseSkill,       // 使用技能
        Defend          // 防御
    }

    /// <summary>
    /// 游戏对象适配器 - 连接新AI系统和现有游戏对象
    /// </summary>
    public static class GameObjectAdapter
    {
        public static GameScenario ToAIScenario(Scenario gameScenario)
        {
            // 这里需要根据实际的Scenario类来实现转换
            return new GameScenario
            {
                CurrentTurn = 1, // gameScenario.CurrentTurn,
                Factions = new List<Faction>(), // 转换势力列表
                Troops = new List<Troop>() // 转换部队列表
            };
        }
    }
}