using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.Influences;

namespace GameGlobal
{
    /// <summary>
    /// 战术评估器：专门负责计算"这一发技能打下去值多少分"
    /// 基于数据驱动的AI决策系统，支持复杂的战术评估
    /// 优化版本：更精炼、更高效的评估算法
    /// </summary>
    public static class CombatEvaluator
    {
        // === 主入口：给某个技能打分 ===
        public static float EvaluateSkill(Troop source, Skill skill, Troop primaryTarget, GameScenario scenario)
        {
            float totalScore = 0;

            // 1. 基础检查
            if (source.CurrentPrestige < skill.Cost) return -1; // 气力/蓝量不足

            // 2. 确定所有受影响的目标 (AOE 计算)
            var targets = scenario.GetTroopsInRadius(primaryTarget.Position, skill.Radius);

            // 3. 遍历所有受影响单位，累加分数
            foreach (var target in targets)
            {
                float unitScore = 0;

                // 4. 遍历技能的所有"影响" (Data-Driven 核心)
                foreach (var influence in skill.Influences)
                {
                    unitScore += EvaluateInfluence(source, influence, target, scenario);
                }

                // 友军误伤修正 (如果是伤害技能，打到友军扣大分)
                if (source.IsFriend(target) && skill.IsDamage)
                {
                    totalScore -= unitScore * 2.0f; // 惩罚系数
                }
                else
                {
                    totalScore += unitScore;
                }
            }

            // 5. 战术修正：斩杀线检查
            if (skill.IsDamage && totalScore > primaryTarget.CurrentHP)
            {
                totalScore += 500; // 只要能杀，优先用
            }

            return totalScore;
        }

        /// <summary>
        /// 评估普通攻击的价值
        /// </summary>
        public static float EvaluateAttack(Troop source, Troop target)
        {
            if (target == null || source.IsFriend(target)) return -1;

            // 基础伤害
            float damage = CalculateBaseDamage(source, target);
            float effectiveDamage = Math.Min(damage, target.CurrentHP);
            
            float score = effectiveDamage;

            // 击杀奖励
            if (effectiveDamage >= target.CurrentHP)
            {
                score += 200; // 击杀奖励
                if (target.IsHero()) score += 300; // 英雄击杀奖励
            }

            // 目标价值修正
            if (target.IsHero()) score *= 1.5f;
            
            // 血量越少的敌人优先级越高
            if (target.HpRatio() < 0.3f) score *= 1.3f;

            return score;
        }

        /// <summary>
        /// 评估移动到指定位置的战略价值
        /// </summary>
        public static float EvaluateStrategicMove(Troop source, Point destination, GameScenario scenario)
        {
            float score = 0;

            // 1. 接近敌人的价值
            var nearbyEnemies = scenario.GetTroopsInRadius(destination, source.AttackRange + 2)
                                       ?.Where(t => source.IsEnemy(t))?.ToList() ?? new List<Troop>();
            
            foreach (var enemy in nearbyEnemies)
            {
                float distance = Math.Abs(destination.X - enemy.Position.X) + Math.Abs(destination.Y - enemy.Position.Y);
                if (distance <= source.AttackRange + 1)
                {
                    score += 50; // 能够威胁到敌人
                    if (enemy.IsHero()) score += 30; // 威胁英雄更有价值
                }
            }

            // 2. 远离危险的价值
            var threats = scenario.GetTroopsInRadius(destination, 3)
                                 ?.Where(t => source.IsEnemy(t) && t.Attack > source.Defense)?.ToList() ?? new List<Troop>();
            
            score -= threats.Count * 25; // 每个威胁扣分

            // 3. 地形优势
            var terrain = scenario.GetTerrain(destination);
            switch (terrain)
            {
                case TerrainKind.Mountain:
                    score += 20; // 山地防御优势
                    break;
                case TerrainKind.Forest:
                    score += 10; // 森林隐蔽优势
                    break;
                case TerrainKind.Water:
                    score -= 30; // 水域移动不便
                    break;
            }

            // 4. 与友军的协调
            var nearbyAllies = scenario.GetTroopsInRadius(destination, 2)
                                      ?.Where(t => source.IsFriend(t))?.Count() ?? 0;
            
            if (nearbyAllies >= 2)
                score += nearbyAllies * 15; // 协同作战加成
            else if (nearbyAllies == 0)
                score -= 20; // 孤军作战扣分

            return score;
        }

        // === 针对具体"影响"的评分逻辑 ===
        private static float EvaluateInfluence(Troop source, Influence influence, Troop target, GameScenario scenario)
        {
            float score = 0;

            switch (influence.Kind)
            {
                case InfluenceKind.Damage: // 通用伤害
                    float damage = CalculateDamage(source, target, influence);
                    
                    // 天气和地形交互系统
                    score = ApplyWeatherAndTerrainModifiers(damage, influence, target, scenario);
                    break;

                case InfluenceKind.Purify: // 净化/治疗
                    if (source.IsFriend(target))
                    {
                        // 只有队友掉血了才加血
                        float missingHP = target.MaxHP - target.CurrentHP;
                        score += Math.Min(influence.Amount, missingHP) * 1.5f; // 治疗权重 > 伤害
                    }
                    break;

                case InfluenceKind.Confusion: // 混乱
                case InfluenceKind.Stun:      // 晕眩
                    if (source.IsEnemy(target))
                    {
                        score += 200; // 基础控制分
                        
                        // 针对性：打断施法
                        if (target.IsCasting) score += 300;
                        
                        // 针对性：控制高输出目标 (吕布)
                        if (target.Attack > source.Attack * 1.5f) score += 200;
                        
                        // 递减：如果已经晕了，就别再晕了
                        if (target.HasStatus(StatusKind.Stun)) score = 0;
                    }
                    break;

                case InfluenceKind.MoraleDown: // 降士气 (如：谣言)
                    if (source.IsEnemy(target))
                    {
                        score += influence.Amount * 2.0f;
                        
                        // 针对性：如果士气低到可以致残/逃跑
                        if (target.Morale - influence.Amount < 20) score += 500;
                    }
                    break;

                case InfluenceKind.Lure: // 诱敌/挑拨
                    if (source.IsEnemy(target))
                    {
                        // 算出敌人被拉过来后的位置
                        Point nextPos = PredictPullPosition(target, source);
                        
                        // 如果那个位置被我方 3 个人包围 (ZOC 夹击)
                        if (GetFriendlyNeighborCount(scenario, nextPos, source.BelongedFaction) >= 3) 
                            score += 400;
                        
                        // 如果那个位置是陷阱 (火种)
                        if (HasTrap(scenario, nextPos)) 
                            score += 600;
                        
                        // 基础诱敌价值
                        score += 150;
                    }
                    break;

                case InfluenceKind.InternalStrife: // 内讧/反间
                    if (source.IsEnemy(target))
                    {
                        // 获取目标周围的敌军队友
                        var neighbors = GetEnemyNeighbors(scenario, target, source.BelongedFaction);
                        foreach (var neighbor in neighbors)
                        {
                            score += neighbor.Attack * 0.5f; // 借刀杀人，把邻居攻击力算作我的收益
                        }
                        
                        // 基础反间价值
                        score += 200;
                    }
                    break;

                default:
                    // 其他影响类型的基础处理
                    score += influence.Power * 10;
                    break;
            }

            return score;
        }

        // === 伤害影响评估 ===
        private static float EvaluateDamageInfluence(Troop source, Influence influence, Troop target, GameScenario scenario)
        {
            float baseDamage = CalculateBaseDamage(source, target) * influence.Power;
            
            // ZHSan 特色：火计 + 森林/藤甲 = 暴击
            if (influence.IsFire)
            {
                var terrainKind = GetTerrainKind(scenario, target.Position);
                if (terrainKind == TerrainKind.Forest) 
                    baseDamage *= 1.5f;
                
                if (target.HasTech("RattanArmor")) 
                    baseDamage *= 2.0f; // 藤甲兵
            }

            // 防止分数溢出：只计算有效伤害
            float effectiveDamage = Math.Min(baseDamage, target.CurrentHP);
            
            // 对敌军造成伤害得分，对友军造成伤害扣分
            return source.IsEnemy(target) ? effectiveDamage : -effectiveDamage * 2;
        }

        // === 治疗影响评估 ===
        private static float EvaluateHealingInfluence(Troop source, Influence influence, Troop target)
        {
            if (!source.IsFriend(target)) return 0;

            float missingHP = target.MaxHP - target.CurrentHP;
            if (missingHP <= 0) return 0; // 满血不治

            float healAmount = Math.Min(influence.Amount, missingHP);
            float score = healAmount * 1.5f;

            // 救命分：血量越少，治疗价值越高
            if (target.HpRatio < 0.2f) score += 500;
            else if (target.HpRatio < 0.5f) score += 200;

            // 英雄优先治疗
            if (target.IsHero) score *= 1.5f;

            return score;
        }

        // === 控制影响评估 ===
        private static float EvaluateControlInfluence(Troop source, Influence influence, Troop target)
        {
            if (!source.IsEnemy(target)) return 0;

            float score = 200;

            // 如果目标正在攻击准备中，打断它！
            if (target.IsCasting) score += 300;

            // 已经晕了就别再晕了
            if (target.HasStatus(StatusKind.Stun)) score = -100;

            // 对强敌控制价值更高
            if (target.IsHero) score *= 1.5f;
            if (target.Attack > source.Attack) score *= 1.2f;

            return score;
        }

        // === 状态影响评估 ===
        private static float EvaluateStatusInfluence(Troop source, Influence influence, Troop target)
        {
            bool isPositive = (influence.Kind == InfluenceKind.Buff);
            bool shouldApply = source.IsFriend(target) ? isPositive : !isPositive;

            if (!shouldApply) return 0;

            float baseScore = influence.Power * influence.Duration * 5;
            
            // 英雄的状态效果更有价值
            if (target.IsHero) baseScore *= 1.3f;

            return baseScore;
        }

        // === 斩杀潜力评估 ===
        private static float EvaluateKillPotential(Troop source, Skill skill, Troop primaryTarget)
        {
            float estimatedDamage = CalculateBaseDamage(source, primaryTarget) * skill.Power;
            
            // 如果伤害溢出太多，稍微扣点分，鼓励用小技能
            if (estimatedDamage > primaryTarget.CurrentHP + 500)
            {
                return -50;
            }
            
            // 如果刚好能杀，或者是英雄，加分
            if (estimatedDamage >= primaryTarget.CurrentHP)
            {
                float killBonus = 200;
                if (primaryTarget.IsHero) killBonus += 300;
                if (primaryTarget.IsLeader) killBonus += 500; // 击杀主将
                return killBonus;
            }

            return 0;
        }

        // === 资源消耗惩罚计算 ===
        private static float CalculateCostPenalty(Troop source, Skill skill)
        {
            if (source.MaxPrestige <= 0) return 0;
            
            float costRatio = (float)skill.Cost / source.MaxPrestige;
            float penalty = costRatio * 50;

            // 如果气力不足50%，更加珍惜
            if (source.CurrentPrestige < source.MaxPrestige * 0.5f)
            {
                penalty *= 1.5f;
            }

            return penalty;
        }

        // === 位置优势评估 ===
        private static float EvaluatePositionalAdvantage(Troop source, Troop target, GameScenario scenario)
        {
            float score = 0;

            // 地形优势
            var sourceTerrain = GetTerrainKind(scenario, source.Position);
            var targetTerrain = GetTerrainKind(scenario, target.Position);

            if (sourceTerrain == TerrainKind.Mountain && targetTerrain == TerrainKind.Plain)
                score += 50; // 居高临下

            // 包围优势：如果目标被我方多个单位包围
            var nearbyEnemies = GetNearbyEnemies(scenario, target.Position, source.BelongedFaction);
            if (nearbyEnemies.Count >= 3)
                score += 100; // 围攻加成

            return score;
        }

        // === 辅助方法 ===
        
        /// <summary>
        /// 计算伤害值（考虑技能威力和特殊效果）
        /// </summary>
        private static float CalculateDamage(Troop source, Troop target, Influence influence)
        {
            // 基础伤害计算
            float baseDamage = GameGlobal.Parameters.ReferDamage(source, target);
            
            // 应用技能威力修正
            float skillDamage = baseDamage * influence.Power;
            
            // 防止过度伤害计算（只计算有效伤害）
            return Math.Min(skillDamage, target.CurrentHP);
        }

        private static List<Troop> GetTargetsInRange(GameScenario scenario, Point position, int radius)
        {
            // 根据实际的ZHSan API调整
            return scenario.GetTroopsInRadius(position, radius)?.ToList() ?? new List<Troop>();
        }

        private static float CalculateBaseDamage(Troop source, Troop target)
        {
            // 使用ZHSan现有的伤害计算系统
            return GameGlobal.Parameters.ReferDamage(source, target);
        }

        private static TerrainKind GetTerrainKind(GameScenario scenario, Point position)
        {
            // 根据实际的ZHSan地形系统调整
            return scenario.GetTerrainKind(position);
        }

        private static List<Troop> GetNearbyEnemies(GameScenario scenario, Point position, Faction faction)
        {
            return scenario.GetTroopsInRadius(position, 2)
                          ?.Where(t => t.BelongedFaction != faction)
                          ?.ToList() ?? new List<Troop>();
        }

        /// <summary>
        /// 应用天气和地形修正
        /// </summary>
        private static float ApplyWeatherAndTerrainModifiers(float baseDamage, Influence influence, Troop target, GameScenario scenario)
        {
            float score = baseDamage;
            var terrain = scenario.GetTerrain(target.Position);
            var weather = GetCurrentWeather(scenario);

            // 火系技能的天气和地形交互
            if (influence.IsFire)
            {
                // 天气影响
                if (weather == WeatherKind.Rain) 
                    score *= 0.5f; // 下雨不放火
                else if (weather == WeatherKind.Drought) 
                    score *= 1.3f; // 干旱火势更猛

                // 地形影响
                if (terrain == TerrainKind.Forest) 
                    score *= 1.5f; // 森林放火
                else if (terrain == TerrainKind.Grassland) 
                    score *= 1.2f; // 草地易燃
            }

            // 水系技能的地形交互
            if (influence.IsWater)
            {
                if (terrain == TerrainKind.Water || terrain == TerrainKind.Swamp) 
                    score *= 1.5f; // 水里用水计
                else if (terrain == TerrainKind.Desert) 
                    score *= 0.7f; // 沙漠缺水
            }

            // 雷系技能的天气交互
            if (influence.IsThunder)
            {
                if (weather == WeatherKind.Storm) 
                    score *= 1.4f; // 雷雨天雷电更强
                else if (weather == WeatherKind.Clear) 
                    score *= 0.9f; // 晴天雷电稍弱
            }

            // 藤甲兵怕火（原有逻辑保留）
            if (influence.IsFire && target.HasTech("RattanArmor"))
            {
                score *= 2.0f;
            }

            return score;
        }

        /// <summary>
        /// 预测被拉拽后的位置
        /// </summary>
        private static Point PredictPullPosition(Troop target, Troop source)
        {
            // 简化实现：目标向施法者方向移动一格
            int dx = source.Position.X - target.Position.X;
            int dy = source.Position.Y - target.Position.Y;
            
            // 标准化方向
            if (dx != 0) dx = dx > 0 ? 1 : -1;
            if (dy != 0) dy = dy > 0 ? 1 : -1;
            
            return new Point(target.Position.X + dx, target.Position.Y + dy);
        }

        /// <summary>
        /// 获取指定位置周围的友军数量
        /// </summary>
        private static int GetFriendlyNeighborCount(GameScenario scenario, Point position, Faction faction)
        {
            return scenario.GetTroopsInRadius(position, 1)
                          ?.Where(t => t.BelongedFaction == faction)
                          ?.Count() ?? 0;
        }

        /// <summary>
        /// 检查指定位置是否有陷阱
        /// </summary>
        private static bool HasTrap(GameScenario scenario, Point position)
        {
            // 这里需要根据实际的陷阱系统来实现
            // 简化实现：检查是否有火种等陷阱
            return false; // 需要连接到实际的陷阱系统
        }

        /// <summary>
        /// 获取目标周围的敌军邻居
        /// </summary>
        private static List<Troop> GetEnemyNeighbors(GameScenario scenario, Troop target, Faction sourceFaction)
        {
            return scenario.GetTroopsInRadius(target.Position, 1)
                          ?.Where(t => t.BelongedFaction == target.BelongedFaction && t != target)
                          ?.ToList() ?? new List<Troop>();
        }

        /// <summary>
        /// 获取当前天气
        /// </summary>
        private static WeatherKind GetCurrentWeather(GameScenario scenario)
        {
            // 这里需要根据实际的天气系统来实现
            // 简化实现，实际使用时需要连接到真实的天气数据
            return WeatherKind.Clear; // 默认晴天
        }
    }

    // === 扩展方法 ===
    public static class TroopExtensions
    {
        public static bool IsFriend(this Troop source, Troop target)
        {
            return source.BelongedFaction == target.BelongedFaction;
        }

        public static bool IsEnemy(this Troop source, Troop target)
        {
            return !source.IsFriend(target);
        }

        public static float HpRatio(this Troop troop) => troop.MaxHP > 0 ? (float)troop.CurrentHP / troop.MaxHP : 0f;
        
        public static bool IsHero(this Troop troop) => troop.PersonId > 0;
        
        public static bool IsLeader(this Troop troop) => troop.IsHero() && troop.Attack > 70; // 简化判断
        
        public static bool IsCasting(this Troop troop) => false; // 需要根据实际状态系统实现
        
        public static bool HasStatus(this Troop troop, StatusKind status) => false; // 需要根据实际状态系统实现
        
        public static bool HasTech(this Troop troop, string techName) => false; // 需要根据实际科技系统实现
        
        public static int Morale(this Troop troop) => 50; // 需要根据实际士气系统实现，默认50
    }

    // === GameScenario扩展方法 ===
    public static class GameScenarioExtensions
    {
        /// <summary>
        /// 获取指定位置的地形类型
        /// </summary>
        public static TerrainKind GetTerrain(this GameScenario scenario, Point position)
        {
            // 这里需要根据实际的地形系统来实现
            // 简化实现，实际使用时需要连接到真实的地形数据
            return TerrainKind.Plain;
        }

        /// <summary>
        /// 验证目标是否有效
        /// </summary>
        public static bool IsTargetValid(this GameScenario scenario, Skill skill, Troop source, Troop target)
        {
            if (target == null) return false;
            
            // 检查射程
            var distance = Math.Abs(source.Position.X - target.Position.X) + 
                          Math.Abs(source.Position.Y - target.Position.Y);
            
            if (distance > skill.Range) return false;
            
            // 检查技能类型和目标关系
            if (skill.IsDamage && source.IsFriend(target)) return false; // 伤害技能不能对友军
            if (skill.IsHealing && source.IsEnemy(target)) return false; // 治疗技能不能对敌军
            
            return true;
        }
    }
}