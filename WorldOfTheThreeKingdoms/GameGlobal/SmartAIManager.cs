using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameObjects;
using GameManager;
using GameObjects.Influences;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    /// <summary>
    /// 智能AI管理器 - 管理所有智能部队的高级AI行为
    /// </summary>
    public class SmartAIManager
    {
        private readonly Dictionary<int, SmartTroop> _smartTroops;
        private readonly Dictionary<int, AIPersonality> _troopPersonalities;
        private readonly AICoordinator _coordinator;
        private readonly PerformanceMonitor _performanceMonitor;
        
        // 配置参数
        public AIConfiguration Config { get; set; }
        
        public SmartAIManager()
        {
            _smartTroops = new Dictionary<int, SmartTroop>();
            _troopPersonalities = new Dictionary<int, AIPersonality>();
            _coordinator = new AICoordinator();
            _performanceMonitor = new PerformanceMonitor();
            Config = new AIConfiguration();
        }

        /// <summary>
        /// 注册智能部队
        /// </summary>
        public void RegisterSmartTroop(SmartTroop troop, AIPersonality personality = null)
        {
            if (troop == null) return;

            _smartTroops[troop.ID] = troop;
            _troopPersonalities[troop.ID] = personality ?? AIPersonality.CreateDefault();
            
            // 初始化部队技能
            InitializeTroopSkills(troop);
            
            System.Diagnostics.Debug.WriteLine($"[SmartAIManager] 注册智能部队: {troop.Name}");
        }

        /// <summary>
        /// 移除智能部队
        /// </summary>
        public void UnregisterSmartTroop(int troopId)
        {
            _smartTroops.Remove(troopId);
            _troopPersonalities.Remove(troopId);
        }

        /// <summary>
        /// 执行所有AI部队的回合
        /// </summary>
        public void ExecuteAITurns(GameScenario scenario)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // 1. 更新全局战术态势
                _coordinator.UpdateGlobalSituation(scenario, _smartTroops.Values);
                
                // 2. 确定行动优先级
                var actionOrder = DetermineActionOrder();
                
                // 3. 执行AI回合
                if (Config.EnableParallelProcessing && actionOrder.Count > Config.ParallelThreshold)
                {
                    ExecuteParallelTurns(actionOrder);
                }
                else
                {
                    ExecuteSequentialTurns(actionOrder);
                }
                
                // 4. 协调后处理
                _coordinator.PostTurnCoordination(_smartTroops.Values);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartAIManager] 执行AI回合时发生异常: {ex.Message}");
            }
            finally
            {
                var elapsed = DateTime.Now - startTime;
                _performanceMonitor.RecordTurnTime(elapsed, _smartTroops.Count);
            }
        }

        /// <summary>
        /// 顺序执行AI回合
        /// </summary>
        private void ExecuteSequentialTurns(List<SmartTroop> actionOrder)
        {
            foreach (var troop in actionOrder)
            {
                try
                {
                    var personality = _troopPersonalities[troop.ID];
                    ApplyPersonalityModifiers(troop, personality);
                    
                    troop.ExecuteSmartTurn();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SmartAIManager] {troop.Name} 执行回合失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 并行执行AI回合
        /// </summary>
        private void ExecuteParallelTurns(List<SmartTroop> actionOrder)
        {
            // 将部队分组以避免冲突
            var groups = GroupTroopsForParallelExecution(actionOrder);
            
            // 🔥 技术性修复：设置 IsWorking = true，抑制背景线程触发画面更新引起的 VertexBuffer 崩溃
            bool originalIsWorking = global::GameManager.Session.Current.IsWorking;
            global::GameManager.Session.Current.IsWorking = true;
            
            try
            {
                foreach (var group in groups)
                {
                    Parallel.ForEach(group, troop =>
                    {
                        try
                        {
                            var personality = _troopPersonalities[troop.ID];
                            ApplyPersonalityModifiers(troop, personality);
                            
                            troop.ExecuteSmartTurn();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SmartAIManager] {troop.Name} 并行执行失败: {ex.Message}");
                        }
                    });
                }
            }
            finally
            {
                global::GameManager.Session.Current.IsWorking = originalIsWorking;
            }
        }

        /// <summary>
        /// 确定行动优先级
        /// </summary>
        private List<SmartTroop> DetermineActionOrder()
        {
            var troops = _smartTroops.Values.ToList();
            
            // 根据多个因素排序
            return troops.OrderByDescending(t => CalculateActionPriority(t)).ToList();
        }

        /// <summary>
        /// 计算行动优先级
        /// </summary>
        private float CalculateActionPriority(SmartTroop troop)
        {
            float priority = 0;
            
            // 1. 英雄优先
            if (troop.IsHero) priority += 100;
            
            // 2. 血量越少优先级越高（需要紧急行动）
            priority += (1.0f - troop.HpRatio) * 50;
            
            // 3. 攻击力高的优先
            priority += troop.Attack * 0.1f;
            
            // 4. 有技能可用的优先
            if (troop.AvailableSkills?.Any(s => troop.CurrentPrestige >= s.Cost) == true)
                priority += 30;
            
            // 5. 个性化因素
            var personality = _troopPersonalities[troop.ID];
            priority += personality.Aggressiveness * 20;
            
            return priority;
        }

        /// <summary>
        /// 应用个性化修正
        /// </summary>
        private void ApplyPersonalityModifiers(SmartTroop troop, AIPersonality personality)
        {
            // 这里可以根据个性调整AI行为
            // 例如：调整评分权重、改变决策倾向等
        }

        /// <summary>
        /// 初始化部队技能
        /// </summary>
        private void InitializeTroopSkills(SmartTroop troop)
        {
            if (troop.PersonId > 0)
            {
                // 从技能学习系统获取技能
                var learnedSkills = SkillLearningSystem.GetPersonSkills(troop.PersonId);
                troop.AvailableSkills = learnedSkills;
            }
            else
            {
                // 为普通部队分配基础技能
                AssignBasicSkills(troop);
            }
        }

        /// <summary>
        /// 为普通部队分配基础技能
        /// </summary>
        private void AssignBasicSkills(SmartTroop troop)
        {
            var basicSkills = new List<Skill>();
            
            // 根据部队类型分配技能
            if (troop.Attack > 70)
            {
                basicSkills.Add(SkillFactory.CreateFireAttack());
            }
            
            if (troop.Intelligence > 60)
            {
                basicSkills.Add(SkillFactory.CreateConfusion());
            }
            
            troop.AvailableSkills = basicSkills;
        }

        /// <summary>
        /// 将部队分组以支持并行执行
        /// </summary>
        private List<List<SmartTroop>> GroupTroopsForParallelExecution(List<SmartTroop> troops)
        {
            var groups = new List<List<SmartTroop>>();
            var processed = new HashSet<int>();
            
            foreach (var troop in troops)
            {
                if (processed.Contains(troop.ID)) continue;
                
                var group = new List<SmartTroop> { troop };
                processed.Add(troop.ID);
                
                // 找到可以并行执行的其他部队（距离较远，不会冲突）
                foreach (var other in troops)
                {
                    if (processed.Contains(other.ID)) continue;
                    
                    if (CanExecuteInParallel(troop, other))
                    {
                        group.Add(other);
                        processed.Add(other.ID);
                    }
                }
                
                groups.Add(group);
            }
            
            return groups;
        }

        /// <summary>
        /// 判断两个部队是否可以并行执行
        /// </summary>
        private bool CanExecuteInParallel(SmartTroop troop1, SmartTroop troop2)
        {
            // 距离足够远，不会产生冲突
            var distance = Math.Abs(troop1.Position.X - troop2.Position.X) + 
                          Math.Abs(troop1.Position.Y - troop2.Position.Y);
            
            return distance > Config.MinParallelDistance;
        }

        /// <summary>
        /// 获取性能统计
        /// </summary>
        public AIPerformanceStats GetPerformanceStats()
        {
            return _performanceMonitor.GetStats();
        }

        /// <summary>
        /// 重置所有AI状态
        /// </summary>
        public void ResetAllAI()
        {
            foreach (var troop in _smartTroops.Values)
            {
                // 重置AI状态
                troop.ResetAIState();
            }
            
            _coordinator.Reset();
            _performanceMonitor.Reset();
        }

        /// <summary>
        /// 设置AI难度
        /// </summary>
        public void SetDifficulty(AIDifficulty difficulty)
        {
            Config.ApplyDifficulty(difficulty);
            
            // 更新所有部队的个性
            foreach (var kvp in _troopPersonalities)
            {
                kvp.Value.ApplyDifficulty(difficulty);
            }
        }

        /// <summary>
        /// 获取AI统计信息
        /// </summary>
        public AIStatistics GetStatistics()
        {
            return new AIStatistics
            {
                TotalTroops = _smartTroops.Count,
                ActiveTroops = _smartTroops.Values.Count(t => t.CurrentHP > 0),
                AverageDecisionTime = _performanceMonitor.AverageDecisionTime,
                TotalDecisions = _performanceMonitor.TotalDecisions,
                SuccessfulActions = _performanceMonitor.SuccessfulActions
            };
        }
    }

    /// <summary>
    /// AI个性系统
    /// </summary>
    public class AIPersonality
    {
        public float Aggressiveness { get; set; } = 0.5f;    // 攻击性 0-1
        public float Caution { get; set; } = 0.5f;           // 谨慎度 0-1
        public float Teamwork { get; set; } = 0.5f;          // 团队合作 0-1
        public float Creativity { get; set; } = 0.5f;        // 创造性 0-1
        public float ResourceManagement { get; set; } = 0.5f; // 资源管理 0-1

        public static AIPersonality CreateDefault()
        {
            return new AIPersonality();
        }

        public static AIPersonality CreateAggressive()
        {
            return new AIPersonality
            {
                Aggressiveness = 0.8f,
                Caution = 0.2f,
                Teamwork = 0.4f,
                Creativity = 0.6f,
                ResourceManagement = 0.3f
            };
        }

        public static AIPersonality CreateDefensive()
        {
            return new AIPersonality
            {
                Aggressiveness = 0.2f,
                Caution = 0.8f,
                Teamwork = 0.7f,
                Creativity = 0.3f,
                ResourceManagement = 0.8f
            };
        }

        public static AIPersonality CreateBalanced()
        {
            return new AIPersonality
            {
                Aggressiveness = 0.5f,
                Caution = 0.5f,
                Teamwork = 0.6f,
                Creativity = 0.5f,
                ResourceManagement = 0.6f
            };
        }

        public void ApplyDifficulty(AIDifficulty difficulty)
        {
            var multiplier = difficulty switch
            {
                AIDifficulty.Easy => 0.7f,
                AIDifficulty.Normal => 1.0f,
                AIDifficulty.Hard => 1.3f,
                AIDifficulty.Nightmare => 1.5f,
                _ => 1.0f
            };

            Aggressiveness = Math.Min(1.0f, Aggressiveness * multiplier);
            Teamwork = Math.Min(1.0f, Teamwork * multiplier);
            Creativity = Math.Min(1.0f, Creativity * multiplier);
        }
    }

    /// <summary>
    /// AI协调器 - 处理多个AI单位之间的协调
    /// </summary>
    public class AICoordinator
    {
        private Dictionary<int, List<int>> _factionGroups;
        private Dictionary<int, Point> _strategicTargets;

        public AICoordinator()
        {
            _factionGroups = new Dictionary<int, List<int>>();
            _strategicTargets = new Dictionary<int, Point>();
        }

        public void UpdateGlobalSituation(GameScenario scenario, IEnumerable<SmartTroop> troops)
        {
            // 更新势力分组
            UpdateFactionGroups(troops);
            
            // 识别战略目标
            IdentifyStrategicTargets(scenario, troops);
        }

        public void PostTurnCoordination(IEnumerable<SmartTroop> troops)
        {
            // 协调后处理，例如：
            // - 调整队形
            // - 重新分配目标
            // - 更新战略计划
        }

        public void Reset()
        {
            _factionGroups.Clear();
            _strategicTargets.Clear();
        }

        private void UpdateFactionGroups(IEnumerable<SmartTroop> troops)
        {
            _factionGroups.Clear();
            
            foreach (var troop in troops)
            {
                var factionId = troop.BelongedFaction?.ID ?? 0;
                
                if (!_factionGroups.ContainsKey(factionId))
                    _factionGroups[factionId] = new List<int>();
                    
                _factionGroups[factionId].Add(troop.ID);
            }
        }

        private void IdentifyStrategicTargets(GameScenario scenario, IEnumerable<SmartTroop> troops)
        {
            // 识别重要的战略目标
            // 例如：敌方英雄、重要建筑、关键地形等
        }
    }

    /// <summary>
    /// 性能监控器
    /// </summary>
    public class PerformanceMonitor
    {
        private List<TimeSpan> _turnTimes;
        private List<int> _troopCounts;
        private DateTime _startTime;

        public double AverageDecisionTime { get; private set; }
        public int TotalDecisions { get; private set; }
        public int SuccessfulActions { get; private set; }

        public PerformanceMonitor()
        {
            _turnTimes = new List<TimeSpan>();
            _troopCounts = new List<int>();
            _startTime = DateTime.Now;
        }

        public void RecordTurnTime(TimeSpan elapsed, int troopCount)
        {
            _turnTimes.Add(elapsed);
            _troopCounts.Add(troopCount);
            
            // 保持最近100次记录
            if (_turnTimes.Count > 100)
            {
                _turnTimes.RemoveAt(0);
                _troopCounts.RemoveAt(0);
            }
            
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            if (_turnTimes.Count > 0)
            {
                AverageDecisionTime = _turnTimes.Average(t => t.TotalMilliseconds);
                TotalDecisions = _turnTimes.Count;
            }
        }

        public AIPerformanceStats GetStats()
        {
            return new AIPerformanceStats
            {
                AverageTurnTime = AverageDecisionTime,
                TotalTurns = TotalDecisions,
                MaxTurnTime = _turnTimes.Count > 0 ? _turnTimes.Max(t => t.TotalMilliseconds) : 0,
                MinTurnTime = _turnTimes.Count > 0 ? _turnTimes.Min(t => t.TotalMilliseconds) : 0
            };
        }

        public void Reset()
        {
            _turnTimes.Clear();
            _troopCounts.Clear();
            AverageDecisionTime = 0;
            TotalDecisions = 0;
            SuccessfulActions = 0;
            _startTime = DateTime.Now;
        }
    }

    /// <summary>
    /// AI配置
    /// </summary>
    public class AIConfiguration
    {
        public bool EnableParallelProcessing { get; set; } = true;
        public int ParallelThreshold { get; set; } = 10;
        public int MinParallelDistance { get; set; } = 5;
        public AIDifficulty Difficulty { get; set; } = AIDifficulty.Normal;
        public bool EnableAdvancedTactics { get; set; } = true;
        public bool EnableCoordination { get; set; } = true;

        public void ApplyDifficulty(AIDifficulty difficulty)
        {
            Difficulty = difficulty;
            
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    EnableAdvancedTactics = false;
                    EnableCoordination = false;
                    break;
                case AIDifficulty.Normal:
                    EnableAdvancedTactics = true;
                    EnableCoordination = false;
                    break;
                case AIDifficulty.Hard:
                case AIDifficulty.Nightmare:
                    EnableAdvancedTactics = true;
                    EnableCoordination = true;
                    break;
            }
        }
    }

    /// <summary>
    /// AI性能统计
    /// </summary>
    public class AIPerformanceStats
    {
        public double AverageTurnTime { get; set; }
        public int TotalTurns { get; set; }
        public double MaxTurnTime { get; set; }
        public double MinTurnTime { get; set; }
    }

    /// <summary>
    /// AI统计信息
    /// </summary>
    public class AIStatistics
    {
        public int TotalTroops { get; set; }
        public int ActiveTroops { get; set; }
        public double AverageDecisionTime { get; set; }
        public int TotalDecisions { get; set; }
        public int SuccessfulActions { get; set; }
    }
}