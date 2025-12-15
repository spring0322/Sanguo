using System;
using System.Collections.Generic;
using System.Linq;
using GameGlobal;
using GameObjects.Influences;

namespace GameObjects
{
    /// <summary>
    /// 增强版智能部队 - 使用行动模式的新架构
    /// 基于你提供的ExecuteTurn()设计理念
    /// </summary>
    public class EnhancedSmartTroop : SmartTroop
    {
        // 视野和感知系统
        public int ViewRadius { get; set; } = 5; // 视野半径
        
        // 行动历史记录
        private List<CombatAction> _actionHistory;
        private int _maxHistorySize = 10;

        public EnhancedSmartTroop() : base()
        {
            _actionHistory = new List<CombatAction>();
        }

        /// <summary>
        /// 执行回合 - 使用行动模式的新架构
        /// </summary>
        public void ExecuteTurn()
        {
            try
            {
                // A. 记忆与势能更新 (之前已完成)
                this.UpdateMemory();

                // B. 获取所有可行方案
                var bestAction = GetBestAction();

                // C. 执行
                bestAction.Execute();

                // D. 记录行动历史
                RecordAction(bestAction);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnhancedSmartTroop] {Name} 执行回合时发生异常: {ex.Message}");
                // 异常时执行默认行为
                var fallbackAction = new WaitAction(this);
                fallbackAction.Execute();
            }
        }

        /// <summary>
        /// 获取最佳行动方案
        /// </summary>
        private CombatAction GetBestAction()
        {
            CombatAction bestAction = new SkipTurnAction();
            float maxScore = -9999;

            // 1. 遍历所有移动点 (解决"手短"问题)
            var movableTiles = GetMovableNeighbors(this.Position);
            movableTiles.Add(this.Position); // 原地不动也是一种选择

            foreach (var tile in movableTiles)
            {
                // 假设我站在这里
                var enemiesInRange = GetEnemiesFromPos(tile, this.ViewRadius);
                
                foreach (var enemy in enemiesInRange)
                {
                    // 2. 遍历所有技能 (CommonData 中的技能列表)
                    foreach (var skill in this.AvailableSkills)
                    {
                        // 检查技能是否可用
                        if (this.CurrentPrestige < skill.Cost) continue;
                        
                        // 检查射程
                        float distance = GetDistance(tile, enemy.Position);
                        if (distance > skill.Range) continue;

                        // 3. 调用评估器算分
                        // 注意：这里传入的是 tile 作为假设的源位置
                        float score = CombatEvaluator.EvaluateSkill(this, skill, enemy, GetCurrentScenario());

                        // 扣除移动成本 (防止为了多打1点血跑太远)
                        float moveCost = GetDistance(this.Position, tile) * 5.0f;
                        score -= moveCost;

                        // 添加战术修正
                        score += EvaluateTacticalBonus(tile, skill, enemy);

                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestAction = new MoveAndCastAction(this, tile, skill, enemy);
                            bestAction.Score = score;
                        }
                    }

                    // 别忘了普攻
                    float attackDistance = GetDistance(tile, enemy.Position);
                    if (attackDistance <= this.AttackRange)
                    {
                        float attackScore = CombatEvaluator.EvaluateAttack(this, enemy);
                        attackScore -= GetDistance(this.Position, tile) * 5.0f; // 移动成本

                        // 添加普攻的战术修正
                        attackScore += EvaluateAttackBonus(tile, enemy);

                        if (attackScore > maxScore)
                        {
                            maxScore = attackScore;
                            bestAction = new MoveAndAttackAction(this, tile, enemy);
                            bestAction.Score = attackScore;
                        }
                    }
                }
            }

            // 4. 评估其他类型的行动
            EvaluateAlternativeActions(ref bestAction, ref maxScore);

            return bestAction;
        }

        /// <summary>
        /// 评估其他类型的行动（防御、撤退、等待等）
        /// </summary>
        private void EvaluateAlternativeActions(ref CombatAction bestAction, ref float maxScore)
        {
            // 1. 评估防御行动
            if (ShouldConsiderDefending())
            {
                var defendAction = new DefendAction(this);
                float defendScore = EvaluateDefendAction();
                
                if (defendScore > maxScore)
                {
                    maxScore = defendScore;
                    bestAction = defendAction;
                    bestAction.Score = defendScore;
                }
            }

            // 2. 评估撤退行动
            if (ShouldConsiderRetreating())
            {
                var retreatDestination = FindBestRetreatPosition();
                if (retreatDestination != this.Position)
                {
                    var retreatAction = new RetreatAction(this, retreatDestination);
                    float retreatScore = EvaluateRetreatAction(retreatDestination);
                    
                    if (retreatScore > maxScore)
                    {
                        maxScore = retreatScore;
                        bestAction = retreatAction;
                        bestAction.Score = retreatScore;
                    }
                }
            }

            // 3. 评估等待行动
            var waitAction = new WaitAction(this);
            float waitScore = EvaluateWaitAction();
            
            if (waitScore > maxScore)
            {
                maxScore = waitScore;
                bestAction = waitAction;
                bestAction.Score = waitScore;
            }

            // 4. 评估纯移动行动（战略移动）
            var strategicMoveDestination = CalculateBestStrategicMove();
            if (strategicMoveDestination != this.Position)
            {
                var moveAction = new MoveAction(this, strategicMoveDestination);
                float moveScore = EvaluateStrategicMove(strategicMoveDestination);
                
                if (moveScore > maxScore)
                {
                    maxScore = moveScore;
                    bestAction = moveAction;
                    bestAction.Score = moveScore;
                }
            }
        }

        /// <summary>
        /// 从指定位置获取视野内的敌人
        /// </summary>
        private List<Troop> GetEnemiesFromPos(Point position, int viewRadius)
        {
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return new List<Troop>();

            return scenario.Troops
                .Where(t => this.IsEnemy(t) && GetDistance(position, t.Position) <= viewRadius)
                .ToList();
        }

        /// <summary>
        /// 计算两点间距离
        /// </summary>
        private float GetDistance(Point pos1, Point pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        /// <summary>
        /// 评估战术加成
        /// </summary>
        private float EvaluateTacticalBonus(Point position, Skill skill, Troop target)
        {
            float bonus = 0;

            // 1. 地形优势
            bonus += EvaluateTerrainAdvantage(position);

            // 2. 配合友军
            var nearbyAllies = GetAlliesNearPosition(position, 2);
            if (nearbyAllies.Count >= 2)
            {
                bonus += nearbyAllies.Count * 15; // 协同作战加成
            }

            // 3. 技能特殊加成
            if (skill.IsDamage && target.HpRatio() < 0.3f)
            {
                bonus += 50; // 补刀加成
            }

            // 4. 位置优势
            if (IsFlankingPosition(position, target.Position))
            {
                bonus += 30; // 侧翼攻击加成
            }

            return bonus;
        }

        /// <summary>
        /// 评估普攻加成
        /// </summary>
        private float EvaluateAttackBonus(Point position, Troop target)
        {
            float bonus = 0;

            // 地形优势
            bonus += EvaluateTerrainAdvantage(position);

            // 包围优势
            var nearbyAllies = GetAlliesNearPosition(target.Position, 2);
            if (nearbyAllies.Count >= 2)
            {
                bonus += 40; // 包围攻击加成
            }

            return bonus;
        }

        /// <summary>
        /// 是否应该考虑防御
        /// </summary>
        private bool ShouldConsiderDefending()
        {
            // 血量较低且有敌人接近时考虑防御
            if (this.HpRatio < 0.4f)
            {
                var nearbyEnemies = GetEnemiesFromPos(this.Position, 3);
                return nearbyEnemies.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// 评估防御行动
        /// </summary>
        private float EvaluateDefendAction()
        {
            float score = 0;

            // 血量越低，防御价值越高
            score += (1.0f - this.HpRatio) * 100;

            // 周围敌人越多，防御价值越高
            var nearbyEnemies = GetEnemiesFromPos(this.Position, 2);
            score += nearbyEnemies.Count * 20;

            return score;
        }

        /// <summary>
        /// 是否应该考虑撤退
        /// </summary>
        private bool ShouldConsiderRetreating()
        {
            // 血量很低或者被包围时考虑撤退
            if (this.HpRatio < 0.25f)
            {
                var nearbyEnemies = GetEnemiesFromPos(this.Position, 2);
                return nearbyEnemies.Count >= 2;
            }
            return false;
        }

        /// <summary>
        /// 寻找最佳撤退位置
        /// </summary>
        private Point FindBestRetreatPosition()
        {
            var movableTiles = GetMovableNeighbors(this.Position);
            Point bestPosition = this.Position;
            float bestScore = -9999;

            foreach (var tile in movableTiles)
            {
                float score = 0;

                // 远离敌人
                var nearbyEnemies = GetEnemiesFromPos(tile, 3);
                score -= nearbyEnemies.Count * 50;

                // 靠近友军
                var nearbyAllies = GetAlliesNearPosition(tile, 3);
                score += nearbyAllies.Count * 30;

                // 地形优势
                score += EvaluateTerrainAdvantage(tile);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = tile;
                }
            }

            return bestPosition;
        }

        /// <summary>
        /// 评估撤退行动
        /// </summary>
        private float EvaluateRetreatAction(Point destination)
        {
            float score = 100; // 基础撤退价值

            // 血量越低，撤退价值越高
            score += (1.0f - this.HpRatio) * 200;

            // 撤退到安全位置的价值
            var enemiesAtDestination = GetEnemiesFromPos(destination, 2);
            score -= enemiesAtDestination.Count * 100;

            return score;
        }

        /// <summary>
        /// 评估等待行动
        /// </summary>
        private float EvaluateWaitAction()
        {
            float score = 10; // 基础等待价值

            // 气力不足时，等待恢复气力有价值
            if (this.CurrentPrestige < this.MaxPrestige * 0.5f)
            {
                score += 30;
            }

            // 如果周围没有敌人，等待的价值较低
            var nearbyEnemies = GetEnemiesFromPos(this.Position, this.ViewRadius);
            if (nearbyEnemies.Count == 0)
            {
                score -= 20;
            }

            return score;
        }

        /// <summary>
        /// 评估战略移动
        /// </summary>
        private float EvaluateStrategicMove(Point destination)
        {
            float score = 0;

            // 势能图评分
            score += GetPotentialScore(destination);

            // 移动成本
            score -= GetDistance(this.Position, destination) * 3.0f;

            return score;
        }

        /// <summary>
        /// 判断是否为侧翼位置
        /// </summary>
        private bool IsFlankingPosition(Point attackerPos, Point targetPos)
        {
            // 简化的侧翼判断：攻击者不在目标的正前方
            int dx = attackerPos.X - targetPos.X;
            int dy = attackerPos.Y - targetPos.Y;
            
            // 如果不是直线攻击，认为是侧翼
            return dx != 0 && dy != 0;
        }

        /// <summary>
        /// 记录行动历史
        /// </summary>
        private void RecordAction(CombatAction action)
        {
            _actionHistory.Add(action);
            
            // 保持历史记录大小
            if (_actionHistory.Count > _maxHistorySize)
            {
                _actionHistory.RemoveAt(0);
            }

            // 输出行动日志
            System.Diagnostics.Debug.WriteLine($"[EnhancedSmartTroop] {Name}: {action.GetDescription()} (评分: {action.Score:F1})");
        }

        /// <summary>
        /// 获取行动历史
        /// </summary>
        public List<CombatAction> GetActionHistory()
        {
            return new List<CombatAction>(_actionHistory);
        }

        /// <summary>
        /// 分析行动模式
        /// </summary>
        public string AnalyzeActionPattern()
        {
            if (_actionHistory.Count < 3) return "数据不足";

            var actionTypes = _actionHistory.Select(a => a.GetType().Name).ToList();
            var mostCommon = actionTypes.GroupBy(x => x)
                                      .OrderByDescending(g => g.Count())
                                      .First();

            return $"最常用行动: {mostCommon.Key} ({mostCommon.Count()}/{_actionHistory.Count})";
        }

        /// <summary>
        /// 重置增强功能
        /// </summary>
        public new void ResetAIState()
        {
            base.ResetAIState();
            _actionHistory.Clear();
        }

        // === 辅助方法 ===

        /// <summary>
        /// 获取当前游戏场景
        /// </summary>
        private GameScenario GetCurrentScenario()
        {
            // 这里需要根据实际的ZHSan架构来获取当前场景
            // 简化实现，实际使用时需要连接到真实的场景数据
            return Session.Current?.Scenario;
        }

        /// <summary>
        /// 获取指定位置附近的友军
        /// </summary>
        private List<Troop> GetAlliesNearPosition(Point position, int radius)
        {
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return new List<Troop>();

            return scenario.Troops
                .Where(t => this.IsFriend(t) && t != this && GetDistance(position, t.Position) <= radius)
                .ToList();
        }

        /// <summary>
        /// 评估地形优势
        /// </summary>
        private float EvaluateTerrainAdvantage(Point position)
        {
            var scenario = GetCurrentScenario();
            if (scenario == null) return 0;

            var terrain = scenario.GetTerrain(position);
            switch (terrain)
            {
                case TerrainKind.Mountain:
                    return 25; // 山地防御优势
                case TerrainKind.Forest:
                    return 15; // 森林隐蔽优势
                case TerrainKind.Plain:
                    return 5;  // 平原移动优势
                case TerrainKind.Water:
                    return -20; // 水域移动不便
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取势能评分（基于之前的势能图系统）
        /// </summary>
        private float GetPotentialScore(Point position)
        {
            // 这里应该连接到之前实现的势能图系统
            // 简化实现：基于与敌人和友军的距离
            var scenario = GetCurrentScenario();
            if (scenario?.Troops == null) return 0;

            float score = 0;

            // 与敌人的距离影响
            var enemies = scenario.Troops.Where(t => this.IsEnemy(t)).ToList();
            foreach (var enemy in enemies)
            {
                float distance = GetDistance(position, enemy.Position);
                if (distance <= this.AttackRange + 1)
                    score += 30; // 能攻击到敌人
                else if (distance <= this.ViewRadius)
                    score += 10; // 在视野内
            }

            // 与友军的距离影响
            var allies = scenario.Troops.Where(t => this.IsFriend(t) && t != this).ToList();
            foreach (var ally in allies)
            {
                float distance = GetDistance(position, ally.Position);
                if (distance <= 3)
                    score += 5; // 与友军协调
            }

            return score;
        }
    }
}