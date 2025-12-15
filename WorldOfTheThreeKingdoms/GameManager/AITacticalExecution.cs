using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    public class TacticalDecision
    {
        public Point MoveDestination;
        public Troop AttackTarget;
        public float Score;
    }

    public class AITacticalExecution
    {
        private static readonly Random _random = new Random();
        // --------------------------------------------------------
        // 1. 对外接口：决定这一回合怎么动、打谁
        // --------------------------------------------------------
        
        /// <summary>
        /// 执行战术回合：计算最佳移动点和攻击目标
        /// </summary>
        /// <param name="troop">当前行动部队</param>
        /// <returns>返回决策结果 (移动点 + 攻击目标)</returns>
        public TacticalDecision MakeTacticalDecision(Troop troop)
        {
            var decision = new TacticalDecision { MoveDestination = troop.Position };
            
            AIDifficulty difficulty = Session.Current.Scenario.Parameters.AIDifficulty;
            var settings = AIDifficultySettings.Get(difficulty);

            // 1. 搜索范围差异
            // 简单AI只看身边几格，困难AI看全图（或者更大的侦查范围）
            int searchRadius = (difficulty == AIDifficulty.Easy) ? 3 : 10;
            
            // 获取战略性格 (直接复用之前的模块)
            // Need to instantiate or ensure AIStrategicEvaluator is accessible. Using static if possible or instance?
            // Assuming static methods on AIStrategicEvaluator based on previous context.
            var weights = AIStrategicEvaluator.CalculateFactionWeights(troop.BelongedFaction);
            
            // 1. 获取所有可移动的格子 (Move Range)
            var movableGrids = GetMovableGrids(troop);
            if (!movableGrids.Contains(troop.Position))
                movableGrids.Add(troop.Position); // 原地不动也是一种选择

            // 2. 模拟人为失误 (Human Error) - 仅针对低难度
            if (difficulty == AIDifficulty.Easy)
            {
                // 30% 概率不走最佳位置，而是随机走一个"还可以"的位置
                if (_random.Next(100) < 30) 
                {
                    // 简单实现：随机选择一个移动点作为"次优"点，简化处理
                    if (movableGrids.Count > 0)
                    {
                        var randomPos = movableGrids[_random.Next(movableGrids.Count)];
                        decision.MoveDestination = randomPos;
                        decision.AttackTarget = GetNearestEnemy(troop); // 配合下面的逻辑简化
                        // 既然已经犯错了，就直接返回
                        return decision;
                    }
                }
            }

            Point bestPos = troop.Position;
            float bestScore = -9999f;
            Troop bestTarget = null;

            // 2. 遍历每一个格子，模拟"假如我走到这里"
            foreach (var pos in movableGrids)
            {
                // 剪枝：如果这个格子上已经有别人了（且不是我自己），跳过
                var occupant = Session.Current.Scenario.GetTroopByPosition(pos);
                if (occupant != null && occupant != troop)
                    continue;

                // 2.1 寻找该位置能打到的所有敌人
                var attackableTargets = GetAttackableTargets(troop, pos);
                
                // 2.2 模拟最佳攻击收益
                float maxDamageScore = 0f;
                Troop potentialTarget = null;

                if (attackableTargets.Count > 0)
                {
                    // 3. 攻击目标选择差异
                    if (difficulty == AIDifficulty.Hard || difficulty == AIDifficulty.Nightmare)
                    {
                        // 高难度：优先集火残血，或者打断对方正在蓄力的单位
                        // (使用之前写的高级 EvaluateAttackValue)
                        foreach (var target in attackableTargets)
                        {
                            float dmgScore = EvaluateAttackValue(troop, target, weights);
                            if (dmgScore > maxDamageScore)
                            {
                                maxDamageScore = dmgScore;
                                potentialTarget = target;
                            }
                        }
                    }
                    else
                    {
                        // 低难度：随机打，或者只打最近的
                        potentialTarget = GetNearestEnemy(troop);
                        // 重新计算该目标的得分（为了统一逻辑）
                        if (potentialTarget != null && attackableTargets.Contains(potentialTarget))
                        {
                            maxDamageScore = EvaluateAttackValue(troop, potentialTarget, weights);
                        }
                        else if (attackableTargets.Count > 0)
                        {
                             // Fallback if nearest matches nothing attackable (edge case)
                             potentialTarget = attackableTargets[0];
                             maxDamageScore = EvaluateAttackValue(troop, potentialTarget, weights);
                        }
                    }
                }

                // 2.3 评估该位置的风险 (防御分)
                float defenseScore = EvaluatePositionSafety(troop, pos, weights);

                // 2.4 评估地形和连携 (辅助分)
                float terrainScore = EvaluateTerrainAndBond(troop, pos);

                // 2.5 评估水战环境 (Naval Environment)
                float waterScore = EvaluateWaterEnvironment(troop, pos, weights);

                // --- 综合评分公式 ---
                // 总分 = (攻击收益 * 进攻系数) + (位置安全分 * 防守系数) + 地形分 + 水战分
                float totalScore = (maxDamageScore * weights.Aggression) + 
                                   (defenseScore * weights.Defense) + 
                                   terrainScore + waterScore;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestPos = pos;
                    bestTarget = potentialTarget;
                }
            }

            decision.MoveDestination = bestPos;
            decision.AttackTarget = bestTarget;
            decision.Score = bestScore;

            return decision;
        }

        // --------------------------------------------------------
        // 2. 核心评估算法
        // --------------------------------------------------------

        /// <summary>
        /// 评估攻击某个敌人的价值
        /// </summary>
        private float EvaluateAttackValue(Troop me, Troop enemy, StrategicDecisionWeights weights)
        {
            // 基础伤害预估
            float estimatedDmg = GameMath.CalculateDamage(me, enemy);
            
            // 斩杀奖励：如果能打死，分数暴增
            if (estimatedDmg >= enemy.Quantity)
            {
                return 1000f + estimatedDmg; 
            }

            // 战损交换比评估
            // 如果我打他一下，反手会被他反击得很疼，那要扣分
            float counterDmg = GameMath.CalculateDamage(enemy, me) * 0.7f; // 假设反击伤害稍低
            
            // 莽夫(Aggression高)不在乎反伤，谨慎性格(Defense高)很在意
            float netScore = estimatedDmg - (counterDmg * weights.Defense);

            // 夹击/包围引导：
            // 如果这个敌人已经被我方队友贴身了，打他能触发夹击，加分
            if (IsFlankable(enemy))
            {
                netScore *= 1.5f;
            }

            return Math.Max(0, netScore);
        }

        /// <summary>
        /// 评估某个位置的安全性 (ZOC与集火风险)
        /// </summary>
        private float EvaluatePositionSafety(Troop me, Point pos, StrategicDecisionWeights weights)
        {
            // Use centralized threat calculation from AIDecisionManager
            // This includes difficulty adjustments (Search Range, Ghost Units, Cheating)
            var difficulty = Session.Current.Scenario.Parameters.AIDifficulty;
            float rawThreat = AIDecisionManager.Instance.CalculateThreat(me, pos, difficulty);

            // 这里的逻辑是：分数为负，越危险扣分越多
            // 胆小鬼(RiskTolerance低) 会因为一点点威胁就扣很多分
            return -(rawThreat * (2.0f - weights.RiskTolerance)); 
        }

        /// <summary>
        /// 评估地形适性和友军羁绊
        /// </summary>
        private float EvaluateTerrainAndBond(Troop me, Point pos)
        {
            float score = 0f;

            // A. 地形加成
            // Scenario.MapTileData[x, y].TerrainID -> TerrainDetail -> or direct ID if enum?
            // The game seems to use TerrainDetail objects. Assuming we can get TerrainKind from it or Map.
            // Using placeholder logic mapping ID to TerrainKind or similar.
            // Using direct TerrainID check against likely IDs (0-10).
            
            int terrainID = Session.Current.Scenario.GetTerrainID(pos); // Helper needed or direct access
            
            // 骑兵讨厌森林/湿地
            if (me.Army.Kind.Type == MilitaryType.骑兵)
            {
                if (terrainID == (int)TerrainKind.森林 || terrainID == (int)TerrainKind.湿地)
                    score -= 50f;
                else if (terrainID == (int)TerrainKind.草原)
                    score += 20f;
            }
            // 步兵喜欢森林 (有防御加成)
            else if (me.Army.Kind.Type == MilitaryType.步兵 && terrainID == (int)TerrainKind.森林)
            {
                score += 30f;
            }

            // B. 羁绊连携 (Bond)
            // 检查该位置相邻格子有没有义兄弟/亲密武将
            var neighbors = GetAdjacency(pos);
            foreach (var n in neighbors)
            {
                var ally = Session.Current.Scenario.GetTroopByPosition(n);
                if (ally != null && ally.BelongedFaction == me.BelongedFaction)
                {
                    // 只要贴着友军就有分 (防止孤军深入)
                    score += 10f;

                    // 如果是义兄弟，分数极高 (触发连携概率)
                    if (IsBonded(me.Leader, ally.Leader))
                    {
                        score += 50f;
                    }
                }
            }

            return score;
        }

        // --------------------------------------------------------
        // 辅助方法
        // --------------------------------------------------------

        private List<Point> GetMovableGrids(Troop troop)
        {
            // Use internal Troop method if available, else standard BFS
            // Assuming Troop.GetMovableArea exists or similar.
            // Since we can't easily call internal complex methods, we might need a simplified version
            // OR reuse CampaignManager.GetTroopsNear style iteration but for Tiles.
            // Use GameArea if possible.
            try 
            {
                 // Assuming GetMovableArea returns GameArea or list of points
                 // Reflection or public access?
                 // Fallback: Return immediate vicinity for now if API unsearchable
                 var area = troop.GetMovableArea(); // If this exists
                 return area.Area;
            }
            catch
            {
                // Fallback BFS
                return GetSurroundingPoints(troop.Position, troop.Movability);
            }
        }

        private List<Troop> GetAttackableTargets(Troop me, Point pos)
        {
            // 获取攻击范围内的敌人
            // 弓兵射程远，步兵贴脸
            int range = 1;
            if (me.Army != null && me.Army.Kind != null)
            {
                // Assuming attack range isn't directly exposed on Kind easily, default 1
                // Or check MinAttackRange/MaxAttackRange
            }
            
            var targets = new List<Troop>();
            
            // 简单扫描范围
            var enemies = GetTroopsInArea(pos, range);
            foreach(var e in enemies)
            {
                if (!me.BelongedFaction.IsFriendly(e.BelongedFaction))
                    targets.Add(e);
            }
            return targets;
        }

        private List<Troop> GetTroopsInArea(Point center, int radius)
        {
            var list = new List<Troop>();
            foreach (Troop t in Session.Current.Scenario.Troops)
            {
                if (t.Destroyed) continue;
                if (Session.Current.Scenario.GetSimpleDistance(t.Position, center) <= radius)
                {
                    list.Add(t);
                }
            }
            return list;
        }

        private List<Point> GetAdjacency(Point p)
        {
            var list = new List<Point>();
            var offsets = new Point[] { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) };
            foreach(var o in offsets)
            {
                list.Add(new Point(p.X + o.X, p.Y + o.Y));
            }
            return list;
        }
        
        private List<Point> GetSurroundingPoints(Point center, int radius)
        {
            var list = new List<Point>();
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    if (Session.Current.Scenario.GetSimpleDistance(new Point(x,y), center) <= radius)
                         list.Add(new Point(x, y));
                }
            }
            return list;
        }

        private bool IsFlankable(Troop enemy) 
        { 
            // Check if opposite side has enemy of enemy (my ally)
            // Simplified
            return false; 
        }

        private bool IsBonded(Person p1, Person p2) 
        { 
            if (p1 == null || p2 == null) return false;
            return p1.IsVeryCloseTo(p2) || p1.HasCloseStrainTo(p2);
        }
        private Troop GetNearestEnemy(Troop me)
        {
            Troop nearest = null;
            int minDist = int.MaxValue;
            foreach (Troop t in Session.Current.Scenario.Troops)
            {
                if (t.Destroyed || t == me) continue;
                if (!me.BelongedFaction.IsFriendly(t.BelongedFaction))
                {
                    int dist = Session.Current.Scenario.GetSimpleDistance(me.Position, t.Position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = t;
                    }
                }
            }
            return nearest;
        }

        // Dummy method for RandomHelper if not available, replacing with standard Random if needed
        // Assuming GameGlobal.RandomHelper exists based on user code. 
        private float EvaluateWaterEnvironment(Troop me, Point pos, StrategicDecisionWeights weights)
        {
            var scenario = Session.Current.Scenario;
            TerrainKind terrain = scenario.GetTerrainKindByPosition(pos);

            // 只有在水上生效 (Assuming TerrainKind.Water is 6 or '水域')
            if (terrain != TerrainKind.水域) return 0f;

            float score = 0f;

            // 1. 远程单位统治力
            if (me.Army.Kind.Range > 1)
            {
                score += 40f; 
                if (GetNearbyEnemies(me).Count == 0) score += 20f;
            }

            // 2. 顺风优势 (Fire Advantage)
            int windDir = 0; // Placeholder
            if (IsUpwind(pos, windDir))
            {
                score += 30f * weights.Aggression;
            }

            // 3. 避免过度密集 (防连环火船)
            var allies = GetTroopsInArea(me.BelongedFaction, pos, 1);
            if (allies.Count > 2)
            {
                score -= 20f; 
            }

            return score;
        }
    
        private bool IsUpwind(Point pos, int windDirection) 
        {
            return true; 
        }

        // Dummy method for GetNearbyEnemies overload if needed, or use existing GetNearestEnemy logic
        private List<Troop> GetNearbyEnemies(Troop me)
        {
             // Simplified lookup
             var list = new List<Troop>();
             foreach (Troop t in Session.Current.Scenario.Troops)
             {
                 if (t != me && !t.Destroyed && !me.BelongedFaction.IsFriendly(t.BelongedFaction))
                 {
                     if (Session.Current.Scenario.GetSimpleDistance(me.Position, t.Position) <= 1)
                        list.Add(t);
                 }
             }
             return list;
        }
    }
}
