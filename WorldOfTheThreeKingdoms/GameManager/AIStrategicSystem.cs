using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// AI的大战略姿态（决定了这回合它会干什么）
    /// </summary>
    public enum StrategicStance
    {
        Idle,           // 摸鱼/待机 (袁绍式发呆)
        Expansion,      // 全力扩张 (曹操式进攻)
        Stabilization,  // 休养生息 (内政回血/清洗内部)
        Defense,        // 龟缩防守 (被动防御)
        Crisis,         // 亡国危机 (求和/迁都)
        Opportunistic   // 机会主义 (偷袭/背刺)
    }

    /// <summary>
    /// 势力画像：将君主、军师、武将群的性格压缩在一起
    /// </summary>
    [System.Serializable]
    public class FactionProfile
    {
        // 君主倾向 (0保守 - 1激进)
        public float RulerAggression { get; set; }
        
        // 军师智力修正 (0昏庸 - 1神算)
        public float AdvisorWisdom { get; set; }
        
        // 派系压力 (武将好战度的总和，高则容易逼迫君主出兵)
        public float GeneralWarPressure { get; set; }
        
        // 决策稳定性 (袁绍低，曹操高。用于随机跳过回合)
        public float Decisiveness { get; set; }

        /// <summary>
        /// 根据势力数据计算画像
        /// </summary>
        public static FactionProfile CalculateProfile(Faction faction)
        {
            var profile = new FactionProfile();
            
            if (faction?.Leader == null)
                return profile;

            // 1. 君主倾向 = (统率 + 野心) / 200
            profile.RulerAggression = Math.Min(1.0f, 
                (faction.Leader.Command + faction.Leader.Ambition) / 200.0f);

            // 2. 军师智力修正
            if (faction.Advisor != null)
            {
                profile.AdvisorWisdom = Math.Min(1.0f, faction.Advisor.Intelligence / 100.0f);
            }
            else
            {
                profile.AdvisorWisdom = 0.3f; // 无军师时的基础值
            }

            // 3. 武将好战压力 = 平均(统率 + 野心 - 义理) / 100
            if (faction.Persons?.Count > 0)
            {
                float totalWarPressure = 0;
                int count = 0;
                
                foreach (Person person in faction.Persons.GetList().Cast<Person>())
                {
                    if (person != faction.Leader && person.Command > 60) // 只计算有能力的武将
                    {
                        float warPressure = (person.Command + person.Ambition - person.PersonalLoyalty * 20) / 100.0f;
                        totalWarPressure += Math.Max(0, warPressure);
                        count++;
                    }
                }
                
                profile.GeneralWarPressure = count > 0 ? totalWarPressure / count : 0.2f;
            }

            // 4. 决策稳定性 = (智力 + 政治 + 魅力) / 300
            profile.Decisiveness = Math.Min(1.0f,
                (faction.Leader.Intelligence + faction.Leader.Politics + faction.Leader.Glamour) / 300.0f);

            return profile;
        }
    }

    /// <summary>
    /// 资源快照：只存AI决策需要的关键数值，无需遍历所有对象
    /// </summary>
    public class ResourceSnapshot
    {
        public float TotalMilitaryPower { get; set; }  // 兵力
        public float AverageFatigue { get; set; }      // 全军平均疲劳度 (0-100)
        public float EconomicHealth { get; set; }      // 钱粮健康度
        public bool IsAtWar { get; set; }              // 是否正在交战
        public int ArchitectureCount { get; set; }     // 城池数量
        public float ThreatLevel { get; set; }         // 威胁等级 (0-1)
        public float OpportunityLevel { get; set; }    // 机会等级 (0-1)

        /// <summary>
        /// 根据势力数据计算资源快照
        /// </summary>
        public static ResourceSnapshot CalculateSnapshot(Faction faction)
        {
            var snapshot = new ResourceSnapshot();
            
            if (faction == null)
                return snapshot;

            // 1. 计算总军事力量
            float totalMilitary = 0;
            float totalFatigue = 0;
            int militaryCount = 0;

            if (faction.Troops?.Count > 0)
            {
                foreach (Troop troop in faction.Troops.GetList().Cast<Troop>())
                {
                    if (!troop.Destroyed)
                    {
                        totalMilitary += troop.Scales;
                        totalFatigue += troop.Morale; // 假设士气反映疲劳度
                        militaryCount++;
                    }
                }
            }

            snapshot.TotalMilitaryPower = totalMilitary;
            
            // 应用难度管理器的军事力量修正
            if (!Session.Current.Scenario.IsPlayer(faction))
            {
                float militaryModifier = DifficultyManager.Instance.GetFactionMilitaryModifier(faction);
                snapshot.TotalMilitaryPower *= militaryModifier;
            }
            
            snapshot.AverageFatigue = militaryCount > 0 ? (100 - totalFatigue / militaryCount) : 0;

            // 2. 经济健康度 = (资金 + 粮食) / (城池数 * 基准值)
            snapshot.ArchitectureCount = faction.Architectures?.Count ?? 0;
            if (snapshot.ArchitectureCount > 0)
            {
                float totalWealth = faction.Fund + faction.Food;
                float expectedWealth = snapshot.ArchitectureCount * 10000; // 每城期望1万资源
                snapshot.EconomicHealth = Math.Min(1.0f, totalWealth / expectedWealth);
            }

            // 3. 是否在战争中
            snapshot.IsAtWar = faction.Troops?.GetList()?.Cast<Troop>()?.Any(t => !t.Destroyed && t.Status != TroopStatus.无) ?? false;

            // 4. 威胁等级 - 检查周边敌军
            snapshot.ThreatLevel = CalculateThreatLevel(faction);

            // 5. 机会等级 - 检查可攻击的弱势邻居
            snapshot.OpportunityLevel = CalculateOpportunityLevel(faction);

            return snapshot;
        }

        private static float CalculateThreatLevel(Faction faction)
        {
            if (faction?.Architectures == null || Session.Current?.Scenario?.Troops == null)
                return 0;

            float threat = 0;
            int checkRadius = 3; // 检查半径

            foreach (Architecture arch in faction.Architectures.GetList().Cast<Architecture>())
            {
                var nearbyEnemyTroops = Session.Current.Scenario.Troops.GetList()
                    ?.Cast<Troop>()
                    ?.Where(t => t.BelongedFaction != faction && 
                               !t.Destroyed &&
                               Math.Abs(t.Position.X - arch.Position.X) <= checkRadius &&
                               Math.Abs(t.Position.Y - arch.Position.Y) <= checkRadius);

                if (nearbyEnemyTroops?.Any() == true)
                {
                    float enemyPower = nearbyEnemyTroops.Sum(t => t.Scales);
                    threat += enemyPower / 10000.0f; // 标准化
                }
            }

            return Math.Min(1.0f, threat);
        }

        private static float CalculateOpportunityLevel(Faction faction)
        {
            if (faction?.Architectures == null || Session.Current?.Scenario?.Factions == null)
                return 0;

            float opportunity = 0;
            
            // 检查邻近的弱势势力
            foreach (Faction otherFaction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
            {
                if (otherFaction == faction || otherFaction.IsAlive == false)
                    continue;

                // 简单的邻近检查和实力对比
                bool isNeighbor = IsNeighborFaction(faction, otherFaction);
                if (isNeighbor)
                {
                    float powerRatio = faction.TotalMilitaryPopulation / Math.Max(1, otherFaction.TotalMilitaryPopulation);
                    if (powerRatio > 1.5f) // 实力优势明显
                    {
                        opportunity += Math.Min(0.5f, (powerRatio - 1.0f) / 2.0f);
                    }
                }
            }

            return Math.Min(1.0f, opportunity);
        }

        private static bool IsNeighborFaction(Faction faction1, Faction faction2)
        {
            if (faction1?.Architectures == null || faction2?.Architectures == null)
                return false;

            int neighborRadius = 5;

            foreach (Architecture arch1 in faction1.Architectures.GetList().Cast<Architecture>())
            {
                foreach (Architecture arch2 in faction2.Architectures.GetList().Cast<Architecture>())
                {
                    int distance = Math.Abs(arch1.Position.X - arch2.Position.X) + 
                                 Math.Abs(arch1.Position.Y - arch2.Position.Y);
                    if (distance <= neighborRadius)
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 邻居势力信息：用于战略决策的邻居分析
    /// </summary>
    public class FactionNeighbors
    {
        public float StrongestNeighborPower { get; set; }  // 最强邻居的军事力量
        public float AveragePower { get; set; }            // 邻居平均军事力量
        public bool HasVulnerableTarget { get; set; }      // 是否有脆弱的攻击目标
        public int NeighborCount { get; set; }             // 邻居数量
        public float TotalNeighborThreat { get; set; }     // 邻居总威胁度
        public Faction BestVictim { get; set; }            // 最佳攻击目标
        public float BestVictimScore { get; set; }         // 最佳目标评分

        /// <summary>
        /// 分析势力的邻居情况
        /// </summary>
        public static FactionNeighbors AnalyzeNeighbors(Faction faction)
        {
            var neighbors = new FactionNeighbors();
            
            if (faction?.Architectures == null || Session.Current?.Scenario?.Factions == null)
                return neighbors;

            var neighborFactions = new List<Faction>();
            int neighborRadius = 8; // 邻居检测半径

            // 1. 找出所有邻居势力
            foreach (Faction otherFaction in Session.Current.Scenario.Factions.GetList().Cast<Faction>())
            {
                if (otherFaction == faction || !otherFaction.IsAlive)
                    continue;

                bool isNeighbor = false;
                foreach (Architecture arch1 in faction.Architectures.GetList().Cast<Architecture>())
                {
                    foreach (Architecture arch2 in otherFaction.Architectures.GetList().Cast<Architecture>())
                    {
                        int distance = Math.Abs(arch1.Position.X - arch2.Position.X) + 
                                     Math.Abs(arch1.Position.Y - arch2.Position.Y);
                        if (distance <= neighborRadius)
                        {
                            isNeighbor = true;
                            break;
                        }
                    }
                    if (isNeighbor) break;
                }

                if (isNeighbor)
                {
                    neighborFactions.Add(otherFaction);
                }
            }

            neighbors.NeighborCount = neighborFactions.Count;

            if (neighborFactions.Count == 0)
                return neighbors;

            // 2. 计算邻居军事力量
            var neighborPowers = new List<float>();
            foreach (var neighbor in neighborFactions)
            {
                float power = neighbor.TotalMilitaryPopulation;
                neighborPowers.Add(power);
            }

            neighbors.StrongestNeighborPower = neighborPowers.Max();
            neighbors.AveragePower = neighborPowers.Average();
            neighbors.TotalNeighborThreat = neighborPowers.Sum();

            // 3. 检查是否有脆弱目标并找出最佳攻击目标
            var victimAnalysis = FindBestVictim(faction, neighborFactions);
            neighbors.HasVulnerableTarget = victimAnalysis.HasValue && victimAnalysis.Value.Score > 0;
            if (victimAnalysis.HasValue)
            {
                neighbors.BestVictim = victimAnalysis.Value.Target;
                neighbors.BestVictimScore = victimAnalysis.Value.Score;
            }

            return neighbors;
        }

        /// <summary>
        /// 攻击目标评估结果
        /// </summary>
        private struct VictimAnalysis
        {
            public Faction Target;
            public float Score;
        }

        /// <summary>
        /// 找出最佳攻击目标
        /// </summary>
        private static VictimAnalysis? FindBestVictim(Faction attacker, List<Faction> neighbors)
        {
            if (neighbors.Count == 0) return null;

            Faction bestTarget = null;
            float bestScore = -1000f;

            foreach (var target in neighbors)
            {
                float score = CalculateVictimScore(attacker, target);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            if (bestTarget != null && bestScore > 0)
            {
                return new VictimAnalysis { Target = bestTarget, Score = bestScore };
            }

            return null;
        }

        /// <summary>
        /// 计算攻击目标评分 - 核心算法
        /// </summary>
        private static float CalculateVictimScore(Faction me, Faction target)
        {
            // 1. 距离衰减 (距离越远，分越低)
            float dist = CalculateDistance(me, target);
            if (dist > 5) return 0; // 太远了不打

            // 2. 实力对比 (欺负弱小)
            float myPower = me.TotalMilitaryPopulation;
            float targetPower = target.TotalMilitaryPopulation;
            float ratio = myPower / (targetPower + 1f);

            // 3. 基础分 - 目标财富除以距离
            float targetWealth = CalculateCityWealth(target);
            float score = targetWealth / Math.Max(1f, dist);

            // 4. 霸凌逻辑
            if (ratio > 3.0f)
            {
                score *= 5.0f; // 极度诱人，简直是送肉
            }
            else if (ratio < 0.8f)
            {
                score = -100f; // 打不过，绝对不打
            }

            // 5. 机会主义 (军师眼里的破绽)
            if (IsFightingOthers(target))
            {
                score += 500f; // 趁火打劫权重极大
            }

            // 6. 额外的战略考虑
            score += CalculateStrategicValue(me, target);

            return score;
        }

        /// <summary>
        /// 计算两个势力之间的距离
        /// </summary>
        private static float CalculateDistance(Faction faction1, Faction faction2)
        {
            if (faction1?.Architectures == null || faction2?.Architectures == null)
                return float.MaxValue;

            float minDistance = float.MaxValue;

            foreach (Architecture arch1 in faction1.Architectures.GetList().Cast<Architecture>())
            {
                foreach (Architecture arch2 in faction2.Architectures.GetList().Cast<Architecture>())
                {
                    float distance = Math.Abs(arch1.Position.X - arch2.Position.X) + 
                                   Math.Abs(arch1.Position.Y - arch2.Position.Y);
                    minDistance = Math.Min(minDistance, distance);
                }
            }

            return minDistance == float.MaxValue ? 10f : minDistance;
        }

        /// <summary>
        /// 计算势力的城市财富总和
        /// </summary>
        private static float CalculateCityWealth(Faction faction)
        {
            if (faction?.Architectures == null)
                return 0;

            float totalWealth = faction.Fund + faction.Food;
            int cityCount = faction.Architectures.Count;

            // 基础财富 + 城市数量加成
            return totalWealth + (cityCount * 5000f);
        }

        /// <summary>
        /// 检查势力是否正在与其他势力交战
        /// </summary>
        private static bool IsFightingOthers(Faction faction)
        {
            if (faction?.Troops == null)
                return false;

            // 检查是否有正在行军或战斗的部队
            foreach (Troop troop in faction.Troops.GetList().Cast<Troop>())
            {
                if (!troop.Destroyed && troop.Status != TroopStatus.无)
                {
                    return true;
                }
            }

            // 检查是否有敌军在附近
            if (faction.Architectures != null && Session.Current?.Scenario?.Troops != null)
            {
                foreach (Architecture arch in faction.Architectures.GetList().Cast<Architecture>())
                {
                    var nearbyEnemies = Session.Current.Scenario.Troops.GetList()
                        ?.Cast<Troop>()
                        ?.Where(t => t.BelongedFaction != faction && 
                                   !t.Destroyed &&
                                   Math.Abs(t.Position.X - arch.Position.X) <= 2 &&
                                   Math.Abs(t.Position.Y - arch.Position.Y) <= 2);

                    if (nearbyEnemies?.Any() == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 计算目标的战略价值
        /// </summary>
        private static float CalculateStrategicValue(Faction attacker, Faction target)
        {
            float strategicValue = 0;

            // 1. 地理位置价值
            if (target.Architectures != null)
            {
                foreach (Architecture arch in target.Architectures.GetList().Cast<Architecture>())
                {
                    // 港口城市价值更高
                    if (arch.HasPort)
                    {
                        strategicValue += 100f;
                    }

                    // 关隘要塞价值更高
                    if (arch.Endurance > 80) // 假设高耐久度代表要塞
                    {
                        strategicValue += 50f;
                    }

                    // 人口多的城市价值更高
                    strategicValue += arch.Population / 1000f;
                }
            }

            // 2. 外交关系
            if (Session.Current?.Scenario?.DiplomaticRelations != null)
            {
                try
                {
                    var relation = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(attacker.ID, target.ID);
                    if (relation != null)
                    {
                        // 仇敌关系增加攻击价值
                        if (relation.Relation < -500)
                        {
                            strategicValue += 200f;
                        }
                        // 盟友关系降低攻击价值
                        else if (relation.Relation > 500)
                        {
                            strategicValue -= 500f;
                        }
                    }
                }
                catch
                {
                    // 外交关系获取失败，忽略这个因素
                }
            }

            // 3. 势力威胁度
            // 如果目标势力很强大，消灭它的战略价值更高
            if (target.Architectures?.Count > 5)
            {
                strategicValue += target.Architectures.Count * 20f;
            }

            return strategicValue;
        }

        /// <summary>
        /// 检查是否有脆弱的攻击目标 (传统方法，保持兼容性)
        /// </summary>
        private static bool CheckForVulnerableTargets(Faction faction, List<Faction> neighbors)
        {
            var bestVictim = FindBestVictim(faction, neighbors);
            return bestVictim.HasValue && bestVictim.Value.Score > 0;
        }

        /// <summary>
        /// 获取攻击目标的详细分析
        /// </summary>
        public static string GetVictimAnalysisReport(Faction attacker, Faction target)
        {
            if (attacker == null || target == null)
                return "无效的势力数据";

            float score = CalculateVictimScore(attacker, target);
            float distance = CalculateDistance(attacker, target);
            float powerRatio = attacker.TotalMilitaryPopulation / Math.Max(1f, target.TotalMilitaryPopulation);
            float wealth = CalculateCityWealth(target);
            bool fighting = IsFightingOthers(target);

            return $"攻击目标分析 - {target.Name}:\n" +
                   $"  总评分: {score:F1}\n" +
                   $"  距离: {distance:F1}\n" +
                   $"  实力比: {powerRatio:F2}:1\n" +
                   $"  目标财富: {wealth:F0}\n" +
                   $"  正在交战: {(fighting ? "是" : "否")}\n" +
                   $"  攻击建议: {(score > 100 ? "强烈推荐" : score > 0 ? "可以考虑" : "不建议攻击")}";
        }
    }

    /// <summary>
    /// 战略大脑：核心决策引擎
    /// </summary>
    public class StrategicBrain
    {
        private int _factionId;
        
        public StrategicBrain(int factionId = 0)
        {
            _factionId = factionId;
        }

        /// <summary>
        /// 核心函数：输入当前数据，直接输出AI这回合要干嘛
        /// "跳过计算过程，直接出结果"
        /// </summary>
        public StrategicStance DetermineStance(FactionProfile profile, ResourceSnapshot resources, FactionNeighbors neighbors)
        {
            int currentTurn = DeterministicRNG.GetCurrentTurn();
            
            // 1. 疲劳熔断机制 (Fatigue Cutoff)
            // 只要全军疲劳过高，强制锁定为"休养"，无视君主性格
            if (resources.AverageFatigue > 70f)
            {
                return StrategicStance.Stabilization;
            }

            // 2. 危机判定 (Survival Logic)
            // 如果兵力只有最强邻居的 20%，进入危机模式
            if (resources.TotalMilitaryPower < neighbors.StrongestNeighborPower * 0.2f)
            {
                return StrategicStance.Crisis;
            }

            // 3. 机会主义判定 (The "Vulture" Check)
            // 军师智力越高，越容易发现邻居的弱点（如邻居正在打仗）
            if (profile.AdvisorWisdom > 0.7f && neighbors.HasVulnerableTarget)
            {
                return StrategicStance.Opportunistic;
            }

            // 4. 性格随机性 (The "Personality" Filter) - 使用确定性随机数
            // 袁绍逻辑：优柔寡断，即使没事干也可能发呆
            float randomValue = DeterministicRNG.GetValue(currentTurn, _factionId, "personality_check");
            if (randomValue > profile.Decisiveness)
            {
                return StrategicStance.Idle;
            }

            // 5. 常规扩张逻辑 (Expansion vs Defense)
            // 综合得分 = (君主野心 * 0.5) + (派系好战压力 * 0.3) + (经济健康度 * 0.2)
            float expansionScore = (profile.RulerAggression * 0.5f) + 
                                 (profile.GeneralWarPressure * 0.3f) + 
                                 (resources.EconomicHealth * 0.2f);

            // 军师修正：如果军师聪明，且兵力不足，会降低扩张分
            if (profile.AdvisorWisdom > 0.8f && resources.TotalMilitaryPower < neighbors.AveragePower)
            {
                expansionScore -= 0.3f;
            }

            return expansionScore > 0.6f ? StrategicStance.Expansion : StrategicStance.Defense;
        }

        /// <summary>
        /// 模拟战斗结果 - 使用确定性随机数
        /// </summary>
        public BattleSimulationResult SimulateBattle(Faction attacker, Faction defender)
        {
            int currentTurn = DeterministicRNG.GetCurrentTurn();
            
            // 计算基础实力对比
            float attackerPower = attacker.TotalMilitaryPopulation;
            float defenderPower = defender.TotalMilitaryPopulation;
            
            // 应用难度管理器的军事力量修正
            if (!Session.Current.Scenario.IsPlayer(attacker))
            {
                attackerPower *= DifficultyManager.Instance.GetFactionMilitaryModifier(attacker);
            }
            if (!Session.Current.Scenario.IsPlayer(defender))
            {
                defenderPower *= DifficultyManager.Instance.GetFactionMilitaryModifier(defender);
            }
            
            float powerRatio = attackerPower / Math.Max(1f, defenderPower);
            
            // 使用确定性随机数添加战斗的不确定性
            float battleRandom = DeterministicRNG.GetValue(currentTurn, attacker.ID, $"battle_vs_{defender.ID}");
            float randomFactor = 0.7f + (battleRandom * 0.6f); // 0.7 - 1.3 的随机因子
            
            float adjustedRatio = powerRatio * randomFactor;
            
            // 确定战斗结果
            bool attackerWins = adjustedRatio > 1.0f;
            Faction winner = attackerWins ? attacker : defender;
            Faction loser = attackerWins ? defender : attacker;
            
            // 生成战斗日志
            string battleLog = BattleLogGenerator.GenerateBattleLog(winner, loser, adjustedRatio, currentTurn);
            
            return new BattleSimulationResult
            {
                Winner = winner,
                Loser = loser,
                PowerRatio = adjustedRatio,
                BattleLog = battleLog,
                AttackerWins = attackerWins,
                CasualtyRate = CalculateCasualtyRate(adjustedRatio, currentTurn, attacker.ID)
            };
        }

        /// <summary>
        /// 计算伤亡率
        /// </summary>
        private float CalculateCasualtyRate(float powerRatio, int turn, int factionId)
        {
            // 基础伤亡率
            float baseCasualty = 0.1f; // 10%基础伤亡
            
            if (powerRatio > 3.0f)
            {
                // 碾压战，伤亡很小
                baseCasualty = 0.05f;
            }
            else if (powerRatio < 1.1f)
            {
                // 激战，伤亡惨重
                baseCasualty = 0.3f;
            }
            else
            {
                // 常规战斗
                baseCasualty = 0.15f;
            }
            
            // 添加确定性随机变化
            float randomFactor = DeterministicRNG.GetValue(turn, factionId, "casualty_rate");
            return baseCasualty * (0.5f + randomFactor); // 50%-150%的变化
        }

        /// <summary>
        /// 评估攻击成功概率
        /// </summary>
        public float EvaluateAttackSuccessRate(Faction attacker, Faction defender)
        {
            float attackerPower = attacker.TotalMilitaryPopulation;
            float defenderPower = defender.TotalMilitaryPopulation;
            float powerRatio = attackerPower / Math.Max(1f, defenderPower);
            
            // 基础成功率基于实力对比
            float baseSuccessRate = Math.Min(0.95f, powerRatio / 2.0f);
            
            // 地理因素修正（简化）
            if (defenderPower > 0)
            {
                // 防守方有地利优势
                baseSuccessRate *= 0.8f;
            }
            
            return Math.Max(0.05f, baseSuccessRate); // 最低5%成功率
        }

        /// <summary>
        /// 获取战略姿态的详细说明
        /// </summary>
        public string GetStanceReasoning(FactionProfile profile, ResourceSnapshot resources, FactionNeighbors neighbors, StrategicStance result)
        {
            var reasoning = new List<string>();
            int currentTurn = DeterministicRNG.GetCurrentTurn();

            // 分析决策过程
            if (resources.AverageFatigue > 70f)
            {
                reasoning.Add($"全军疲劳度过高({resources.AverageFatigue:F1}%)，强制休养");
            }
            else if (resources.TotalMilitaryPower < neighbors.StrongestNeighborPower * 0.2f)
            {
                reasoning.Add($"军力仅为最强邻居的{(resources.TotalMilitaryPower / neighbors.StrongestNeighborPower * 100):F1}%，进入危机模式");
            }
            else if (profile.AdvisorWisdom > 0.7f && neighbors.HasVulnerableTarget)
            {
                reasoning.Add($"军师智力{profile.AdvisorWisdom:F2}，发现脆弱目标，采取机会主义");
            }
            else
            {
                float randomValue = DeterministicRNG.GetValue(currentTurn, _factionId, "personality_check");
                if (randomValue > profile.Decisiveness)
                {
                    reasoning.Add($"君主决策力不足({profile.Decisiveness:F2})，随机值{randomValue:F2}，优柔寡断");
                }
                else
                {
                    float expansionScore = (profile.RulerAggression * 0.5f) + 
                                         (profile.GeneralWarPressure * 0.3f) + 
                                         (resources.EconomicHealth * 0.2f);
                    
                    if (profile.AdvisorWisdom > 0.8f && resources.TotalMilitaryPower < neighbors.AveragePower)
                    {
                        reasoning.Add($"军师建议谨慎(智力{profile.AdvisorWisdom:F2}，兵力不足)");
                        expansionScore -= 0.3f;
                    }

                    reasoning.Add($"扩张评分: {expansionScore:F2} (君主{profile.RulerAggression:F2} + 武将压力{profile.GeneralWarPressure:F2} + 经济{resources.EconomicHealth:F2})");
                    reasoning.Add(expansionScore > 0.6f ? "评分超过0.6，选择扩张" : "评分不足0.6，选择防御");
                }
            }

            return string.Join("; ", reasoning);
        }
    }

    /// <summary>
    /// 战斗模拟结果
    /// </summary>
    public class BattleSimulationResult
    {
        public GameObjects.Faction Winner { get; set; }
        public GameObjects.Faction Loser { get; set; }
        public float PowerRatio { get; set; }
        public string BattleLog { get; set; }
        public bool AttackerWins { get; set; }
        public float CasualtyRate { get; set; }
    }

    /// <summary>
    /// AI战略决策系统 - 整合版
    /// </summary>
    public static class AIStrategicDecisionSystem
    {
        private static Dictionary<int, StrategicBrain> _brains = new Dictionary<int, StrategicBrain>();

        /// <summary>
        /// 获取或创建势力的战略大脑
        /// </summary>
        private static StrategicBrain GetOrCreateBrain(int factionId)
        {
            if (!_brains.ContainsKey(factionId))
            {
                _brains[factionId] = new StrategicBrain(factionId);
            }
            return _brains[factionId];
        }

        /// <summary>
        /// 根据势力画像和资源快照决定战略姿态 - 使用新的StrategicBrain
        /// </summary>
        public static StrategicStance DetermineStrategicStance(FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors = null, int factionId = 0)
        {
            // 如果没有提供邻居信息，使用传统方法
            if (neighbors == null)
            {
                return DetermineStrategicStanceLegacy(profile, snapshot);
            }

            // 使用势力专属的StrategicBrain
            var brain = GetOrCreateBrain(factionId);
            return brain.DetermineStance(profile, snapshot, neighbors);
        }

        /// <summary>
        /// 传统的战略姿态决定方法（向后兼容）
        /// </summary>
        private static StrategicStance DetermineStrategicStanceLegacy(FactionProfile profile, ResourceSnapshot snapshot)
        {
            // 1. 危机模式 - 生存第一
            if (snapshot.ArchitectureCount <= 1 || snapshot.EconomicHealth < 0.2f || snapshot.ThreatLevel > 0.7f)
            {
                return StrategicStance.Crisis;
            }

            // 2. 防御模式 - 威胁较高或疲劳严重
            if (snapshot.ThreatLevel > 0.5f || snapshot.AverageFatigue > 70)
            {
                return StrategicStance.Defense;
            }

            // 3. 扩张模式 - 君主激进 + 实力强 + 有机会
            if (profile.RulerAggression > 0.7f && 
                snapshot.EconomicHealth > 0.6f && 
                snapshot.OpportunityLevel > 0.4f &&
                snapshot.AverageFatigue < 50)
            {
                return StrategicStance.Expansion;
            }

            // 4. 机会主义 - 军师聪明 + 有明显机会
            if (profile.AdvisorWisdom > 0.8f && snapshot.OpportunityLevel > 0.6f)
            {
                return StrategicStance.Opportunistic;
            }

            // 5. 休养生息 - 经济不佳或刚打完仗
            if (snapshot.EconomicHealth < 0.5f || snapshot.AverageFatigue > 60 || snapshot.IsAtWar)
            {
                return StrategicStance.Stabilization;
            }

            // 6. 决策不稳定时可能摸鱼
            if (profile.Decisiveness < 0.4f && GameObject.Random(100) < 30)
            {
                return StrategicStance.Idle;
            }

            // 7. 默认：根据武将压力决定
            if (profile.GeneralWarPressure > 0.6f)
            {
                return StrategicStance.Expansion;
            }
            else
            {
                return StrategicStance.Stabilization;
            }
        }

        /// <summary>
        /// 获取战略姿态的描述
        /// </summary>
        public static string GetStanceDescription(StrategicStance stance)
        {
            switch (stance)
            {
                case StrategicStance.Idle:
                    return "摸鱼待机 - 君主优柔寡断，无所作为";
                case StrategicStance.Expansion:
                    return "全力扩张 - 积极进攻，开疆拓土";
                case StrategicStance.Stabilization:
                    return "休养生息 - 发展内政，整顿军备";
                case StrategicStance.Defense:
                    return "龟缩防守 - 被动防御，保存实力";
                case StrategicStance.Crisis:
                    return "亡国危机 - 求和迁都，苟延残喘";
                case StrategicStance.Opportunistic:
                    return "机会主义 - 伺机而动，偷袭背刺";
                default:
                    return "未知姿态";
            }
        }

        /// <summary>
        /// 获取战略决策的详细推理过程
        /// </summary>
        public static string GetDecisionReasoning(FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors, int factionId = 0)
        {
            var brain = GetOrCreateBrain(factionId);
            var stance = brain.DetermineStance(profile, snapshot, neighbors);
            return brain.GetStanceReasoning(profile, snapshot, neighbors, stance);
        }

        /// <summary>
        /// 模拟战斗结果
        /// </summary>
        public static BattleSimulationResult SimulateBattle(GameObjects.Faction attacker, GameObjects.Faction defender)
        {
            var brain = GetOrCreateBrain(attacker.ID);
            return brain.SimulateBattle(attacker, defender);
        }

        /// <summary>
        /// 评估攻击成功概率
        /// </summary>
        public static float EvaluateAttackSuccessRate(GameObjects.Faction attacker, GameObjects.Faction defender)
        {
            var brain = GetOrCreateBrain(attacker.ID);
            return brain.EvaluateAttackSuccessRate(attacker, defender);
        }

        /// <summary>
        /// 执行战略姿态对应的行动
        /// </summary>
        public static void ExecuteStrategicActions(Faction faction, StrategicStance stance, FactionProfile profile, ResourceSnapshot snapshot)
        {
            if (faction == null) return;

            System.Diagnostics.Debug.WriteLine($"[AI战略] {faction.Name} 采取战略姿态: {GetStanceDescription(stance)}");

            switch (stance)
            {
                case StrategicStance.Expansion:
                    ExecuteExpansionActions(faction, profile, snapshot);
                    break;
                case StrategicStance.Stabilization:
                    ExecuteStabilizationActions(faction, profile, snapshot);
                    break;
                case StrategicStance.Defense:
                    ExecuteDefenseActions(faction, profile, snapshot);
                    break;
                case StrategicStance.Crisis:
                    ExecuteCrisisActions(faction, profile, snapshot);
                    break;
                case StrategicStance.Opportunistic:
                    ExecuteOpportunisticActions(faction, profile, snapshot);
                    break;
                case StrategicStance.Idle:
                    ExecuteIdleActions(faction, profile, snapshot);
                    break;
            }
        }

        private static void ExecuteExpansionActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] {faction.Name} 执行扩张行动");
            
            // 分析邻居并选择攻击目标
            var neighbors = FactionNeighbors.AnalyzeNeighbors(faction);
            
            if (neighbors.BestVictim != null && neighbors.BestVictimScore > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] {faction.Name} 锁定攻击目标: {neighbors.BestVictim.Name} (评分: {neighbors.BestVictimScore:F1})");
                
                // 显示详细的攻击分析
                var analysis = FactionNeighbors.GetVictimAnalysisReport(faction, neighbors.BestVictim);
                System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] 攻击分析:\n{analysis}");
                
                // 模拟战斗结果
                var battleResult = AIStrategicDecisionSystem.SimulateBattle(faction, neighbors.BestVictim);
                System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] 战斗模拟结果:");
                System.Diagnostics.Debug.WriteLine($"  胜利者: {battleResult.Winner.Name}");
                System.Diagnostics.Debug.WriteLine($"  实力比: {battleResult.PowerRatio:F2}:1");
                System.Diagnostics.Debug.WriteLine($"  预期伤亡率: {battleResult.CasualtyRate:F1}%");
                System.Diagnostics.Debug.WriteLine($"  战斗日志: {battleResult.BattleLog}");
                
                // 评估攻击成功率
                float successRate = AIStrategicDecisionSystem.EvaluateAttackSuccessRate(faction, neighbors.BestVictim);
                System.Diagnostics.Debug.WriteLine($"  攻击成功率: {successRate:P1}");
                
                // 根据模拟结果决定是否发动攻击
                if (battleResult.AttackerWins && successRate > 0.6f)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] {faction.Name} 决定发动攻击！");
                    
                    // 生成攻击决策日志
                    int currentTurn = DeterministicRNG.GetCurrentTurn();
                    string attackLog = BattleLogGenerator.GenerateFieldBattleLog(
                        faction, neighbors.BestVictim, "边境", currentTurn);
                    System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] 攻击日志: {attackLog}");
                    
                    // TODO: 实现具体的攻击逻辑
                    // - 集结军队
                    // - 制定攻击路线
                    // - 发动战争
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] {faction.Name} 评估后认为胜算不大，暂缓攻击");
                    System.Diagnostics.Debug.WriteLine($"  原因: 胜率{successRate:P1}，模拟结果{(battleResult.AttackerWins ? "胜利" : "失败")}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略-扩张] {faction.Name} 未找到合适的攻击目标，转为内政发展");
                
                // TODO: 实现扩张准备逻辑
                // - 优先招募武将
                // - 训练军队
                // - 发展经济
                // - 寻找更远的攻击目标
            }
        }

        private static void ExecuteStabilizationActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-稳定] {faction.Name} 执行休养生息");
            // TODO: 实现稳定化逻辑
            // - 发展经济
            // - 建设设施
            // - 整顿内政
            // - 恢复军队士气
        }

        private static void ExecuteDefenseActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-防御] {faction.Name} 执行防御行动");
            // TODO: 实现防御逻辑
            // - 召回外征军队
            // - 加强城防
            // - 储备粮草
            // - 寻求外交支援
        }

        private static void ExecuteCrisisActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-危机] {faction.Name} 进入危机模式");
            // TODO: 实现危机处理逻辑
            // - 求和谈判
            // - 迁都逃跑
            // - 释放俘虏换取支持
            // - 紧急征兵
        }

        private static void ExecuteOpportunisticActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-机会] {faction.Name} 寻找机会");
            
            // 机会主义专门寻找正在交战的目标
            var neighbors = FactionNeighbors.AnalyzeNeighbors(faction);
            
            if (neighbors.BestVictim != null)
            {
                var analysis = FactionNeighbors.GetVictimAnalysisReport(faction, neighbors.BestVictim);
                System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 发现机会目标: {neighbors.BestVictim.Name}");
                System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 机会分析:\n{analysis}");
                
                if (neighbors.BestVictimScore > 500) // 高分说明是趁火打劫的好机会
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略-机会] {faction.Name} 决定趁火打劫 {neighbors.BestVictim.Name}！");
                    
                    // 模拟趁火打劫的战斗结果
                    var battleResult = AIStrategicDecisionSystem.SimulateBattle(faction, neighbors.BestVictim);
                    System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 趁火打劫模拟结果:");
                    System.Diagnostics.Debug.WriteLine($"  胜利者: {battleResult.Winner.Name}");
                    System.Diagnostics.Debug.WriteLine($"  实力比: {battleResult.PowerRatio:F2}:1");
                    System.Diagnostics.Debug.WriteLine($"  预期伤亡率: {battleResult.CasualtyRate:F1}%");
                    
                    // 机会主义攻击的成功率更高（因为目标正在交战）
                    float successRate = AIStrategicDecisionSystem.EvaluateAttackSuccessRate(faction, neighbors.BestVictim);
                    successRate *= 1.5f; // 趁火打劫成功率提升50%
                    successRate = Math.Min(0.95f, successRate); // 最高95%
                    
                    System.Diagnostics.Debug.WriteLine($"  趁火打劫成功率: {successRate:P1}");
                    
                    if (battleResult.AttackerWins && successRate > 0.4f) // 机会主义门槛更低
                    {
                        // 生成趁火打劫日志
                        int currentTurn = DeterministicRNG.GetCurrentTurn();
                        string opportunisticLog = BattleLogGenerator.GenerateFieldBattleLog(
                            faction, neighbors.BestVictim, "敌后", currentTurn);
                        System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 趁火打劫日志: {opportunisticLog}");
                        
                        // TODO: 实现趁火打劫逻辑
                        // - 快速集结军队
                        // - 偷袭弱势城池
                        // - 抢夺资源后撤退
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 评估后认为风险太大，放弃趁火打劫");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AI战略-机会] 机会不够成熟，继续观望");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI战略-机会] {faction.Name} 暂无发现机会，保持警戒");
                
                // TODO: 实现机会主义待机逻辑
                // - 保持军队机动性
                // - 加强情报收集
                // - 外交试探
            }
        }

        private static void ExecuteIdleActions(Faction faction, FactionProfile profile, ResourceSnapshot snapshot)
        {
            System.Diagnostics.Debug.WriteLine($"[AI战略-摸鱼] {faction.Name} 无所作为");
            // TODO: 实现摸鱼逻辑
            // - 基本维持
            // - 随机小动作
            // - 可能被武将逼迫行动
        }
    }
}