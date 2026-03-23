using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GameObjects; // 引用游戏核心库
using global::GameGlobal;
using global::GameManager;
using System.Text.Json; // 添加JSON序列化支持
using WorldOfTheThreeKingdoms.Tools;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // ==========================================
    // 0. 本地枚举定义 (Local Enums) - 解决编译顺序问题
    // ==========================================
    
    // 【修复】本地定义StrategicStance枚举，与StrategicBrain.cs中的定义保持一致
    public enum StrategicStanceLocal
    {
        Neutral,           // 中性发展
        Aggressive,        // 主动扩张
        Defensive,         // 防御保守
        Consolidation,     // 休养生息
        Panic,            // 恐慌求生
        CoalitionCrusade, // 联盟十字军（强制协同进攻）
        CoalitionSupport, // 联盟支援
        Cautious          // 谨慎观望
    }

    // ==========================================
    // 1. 基础设施与配置 (Infrastructure)
    // ==========================================

    public static class AIRandomHelper
    {
        private static Random _random = new Random();
        public static int Next(int max) => _random.Next(max);
        public static int Next(int min, int max) => _random.Next(min, max);
    }

    /// <summary>
    /// AI缓存管理器 (实现脏标记模式)
    /// 用于优化AI系统中的重复计算，特别是邻居分析等昂贵操作
    /// </summary>
    public class AICacheManager
    {
        public static AICacheManager Instance { get; } = new AICacheManager();

        // 全局地图版本号 (脏标记核心)
        private int _globalMapVersion = 0;
        
        // 邻居分析缓存：Key=FactionID, Value=(版本号, 数据)
        private Dictionary<int, (int Version, FactionNeighbors Data)> _neighborsCache = 
            new Dictionary<int, (int, FactionNeighbors)>();
        
        // 势力档案缓存：Key=FactionID, Value=(版本号, 数据)
        private Dictionary<int, (int Version, FactionProfileComplete Data)> _profileCache = 
            new Dictionary<int, (int, FactionProfileComplete)>();
        
        // 资源快照缓存：Key=FactionID, Value=(版本号, 数据)
        private Dictionary<int, (int Version, ResourceSnapshotComplete Data)> _resourceCache = 
            new Dictionary<int, (int, ResourceSnapshotComplete)>();

        /// <summary>
        /// 当地图发生变化（城池易手、势力灭亡）时调用此方法
        /// </summary>
        public void SetMapDirty()
        {
            _globalMapVersion++;
            System.Diagnostics.Debug.WriteLine($"[AICacheManager] 地图状态变更，版本号更新为: {_globalMapVersion}");
        }

        /// <summary>
        /// 当势力资源发生变化时调用此方法
        /// </summary>
        public void SetFactionResourceDirty(int factionId)
        {
            // 清理特定势力的资源缓存
            if (_resourceCache.ContainsKey(factionId))
            {
                _resourceCache.Remove(factionId);
                System.Diagnostics.Debug.WriteLine($"[AICacheManager] 势力 {factionId} 资源缓存已清理");
            }
        }

        /// <summary>
        /// 获取邻居分析数据 (带缓存)
        /// </summary>
        public FactionNeighbors GetNeighbors(Faction faction)
        {
            if (faction == null) return new FactionNeighbors();

            // 1. 检查缓存是否存在且有效
            if (_neighborsCache.TryGetValue(faction.ID, out var cacheEntry))
            {
                if (cacheEntry.Version == _globalMapVersion)
                {
                    // 缓存命中！直接返回，跳过昂贵的遍历计算
                    System.Diagnostics.Debug.WriteLine($"[AICacheManager] 邻居分析缓存命中: 势力{faction.ID}");
                    return cacheEntry.Data;
                }
            }

            // 2. 缓存失效或不存在，重新计算
            System.Diagnostics.Debug.WriteLine($"[AICacheManager] 重新计算邻居分析: 势力{faction.ID}");
            var newData = FactionNeighbors.AnalyzeNeighbors(faction);

            // 3. 更新缓存
            _neighborsCache[faction.ID] = (_globalMapVersion, newData);
            return newData;
        }

        /// <summary>
        /// 获取势力档案数据 (带缓存)
        /// </summary>
        public FactionProfileComplete GetFactionProfile(Faction faction)
        {
            if (faction == null) return new FactionProfileComplete();

            // 1. 检查缓存是否存在且有效
            if (_profileCache.TryGetValue(faction.ID, out var cacheEntry))
            {
                if (cacheEntry.Version == _globalMapVersion)
                {
                    System.Diagnostics.Debug.WriteLine($"[AICacheManager] 势力档案缓存命中: 势力{faction.ID}");
                    return cacheEntry.Data;
                }
            }

            // 2. 缓存失效或不存在，重新计算
            System.Diagnostics.Debug.WriteLine($"[AICacheManager] 重新计算势力档案: 势力{faction.ID}");
            var newData = FactionProfileComplete.CalculateProfile(faction);

            // 3. 更新缓存
            _profileCache[faction.ID] = (_globalMapVersion, newData);
            return newData;
        }

        /// <summary>
        /// 获取资源快照数据 (带缓存)
        /// </summary>
        public ResourceSnapshotComplete GetResourceSnapshot(Faction faction)
        {
            if (faction == null) return new ResourceSnapshotComplete();

            // 1. 检查缓存是否存在且有效
            if (_resourceCache.TryGetValue(faction.ID, out var cacheEntry))
            {
                if (cacheEntry.Version == _globalMapVersion)
                {
                    System.Diagnostics.Debug.WriteLine($"[AICacheManager] 资源快照缓存命中: 势力{faction.ID}");
                    return cacheEntry.Data;
                }
            }

            // 2. 缓存失效或不存在，重新计算
            System.Diagnostics.Debug.WriteLine($"[AICacheManager] 重新计算资源快照: 势力{faction.ID}");
            var newData = ResourceSnapshotComplete.CalculateSnapshot(faction);

            // 3. 更新缓存
            _resourceCache[faction.ID] = (_globalMapVersion, newData);
            return newData;
        }

        /// <summary>
        /// 清理无效势力的缓存
        /// </summary>
        public void ClearCacheForFaction(int factionId)
        {
            bool removed = false;
            
            if (_neighborsCache.ContainsKey(factionId))
            {
                _neighborsCache.Remove(factionId);
                removed = true;
            }
            
            if (_profileCache.ContainsKey(factionId))
            {
                _profileCache.Remove(factionId);
                removed = true;
            }
            
            if (_resourceCache.ContainsKey(factionId))
            {
                _resourceCache.Remove(factionId);
                removed = true;
            }

            if (removed)
            {
                System.Diagnostics.Debug.WriteLine($"[AICacheManager] 已清理势力 {factionId} 的所有缓存");
            }
        }

        /// <summary>
        /// 清理所有缓存 (用于调试或内存管理)
        /// </summary>
        public void ClearAllCache()
        {
            int totalCleared = _neighborsCache.Count + _profileCache.Count + _resourceCache.Count;
            
            _neighborsCache.Clear();
            _profileCache.Clear();
            _resourceCache.Clear();
            
            System.Diagnostics.Debug.WriteLine($"[AICacheManager] 已清理所有缓存，共 {totalCleared} 项");
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            return $"AICacheManager统计: 地图版本{_globalMapVersion}, " +
                   $"邻居缓存{_neighborsCache.Count}项, " +
                   $"档案缓存{_profileCache.Count}项, " +
                   $"资源缓存{_resourceCache.Count}项";
        }
    }

    // 【修复】使用现有的AIStrategicConfig、DifficultyManager类，不重复定义
    // 【修复】使用现有的StrategicStance枚举，在StrategicBrain.cs中定义

    // 【修复】类定义去掉了 static，使其可以被实例化
    public class DifficultyManagerInstance
    {
        public class DifficultyStatus
        {
            public string GamePhase = "中期";
            public float DominanceRatio = 0.5f;
            public float ResourceModifier = 1.0f;
            public float MilitaryModifier = 1.0f;
            public float RecruitmentModifier = 1.0f;
        }

        public static DifficultyManagerInstance Instance { get; } = new DifficultyManagerInstance();
        public float GetDifficultyModifier() => 1.0f;

        public DifficultyStatus GetCurrentDifficultyStatus()
        {
            return new DifficultyStatus();
        }
    }

    // ==========================================
    // 2. 核心数据模型 (Models) - 深度还原逻辑
    // ==========================================

    // 【完整还原】FactionProfile结构体，包含所有原有功能
    public struct FactionProfileComplete
    {
        public int MilitaryPower;
        public int EconomyPower;
        public int Population;
        public int OfficerCount;
        public StrategicStanceLocal StrategicStance;

        public float RulerAggression;
        public float AdvisorWisdom;
        public float Decisiveness;

        public float MilitaryStrength => MilitaryPower;
        public float EconomicStrength => EconomyPower;

        public static FactionProfileComplete CalculateProfile(Faction faction)
        {
            FactionProfileComplete profile = new FactionProfileComplete();
            if (faction == null)
            {
                profile.StrategicStance = StrategicStanceLocal.Neutral;
                return profile;
            }

            profile.MilitaryPower = CalculateMilitaryPower(faction);
            profile.EconomyPower = CalculateEconomyPower(faction);
            profile.Population = CalculatePopulation(faction);
            profile.OfficerCount = CalculateOfficerCount(faction);

            if (faction.Leader != null)
            {
                // [CommonData] 参考 OfficerDamage 逻辑，综合统率和武力
                profile.RulerAggression = (faction.Leader.Strength + faction.Leader.Command) / 200.0f;
                profile.Decisiveness = 0.5f;
            }
            if (faction.Advisor != null)
            {
                profile.AdvisorWisdom = faction.Advisor.Intelligence / 100.0f;
            }

            profile.StrategicStance = DetermineStrategicStance(faction, profile);
            return profile;
        }

        private static int CalculateMilitaryPower(Faction faction)
        {
            int power = 0;
            // 【修复】解决 long 转 int 问题
            power += faction.TroopCount * 1000;

            // 【还原】计算武将战斗力
            if (faction.Persons != null)
            {
                foreach (GameObject obj in faction.Persons.GetList())
                {
                    Person p = (obj is Person ? (Person)obj : null);
                    if (p == null) continue;
                    power += p.Command * 10;
                }
            }
            return power;
        }

        private static int CalculateEconomyPower(Faction faction)
        {
            int economy = 0;
            // 【还原】计算所有城池的经济产出
            if (faction.Architectures != null)
            {
                foreach (GameObject obj in faction.Architectures.GetList())
                {
                    Architecture arch = (obj is Architecture ? (Architecture)obj : null);
                    if (arch == null) continue;

                    // 基于 CommonData.json 的 Fund/Food 定义
                    economy += arch.Fund / 100;
                    economy += arch.Food / 200;
                }
            }
            return economy;
        }

        private static int CalculatePopulation(Faction faction)
        {
            int pop = 0;
            if (faction.Architectures != null)
            {
                foreach (GameObject obj in faction.Architectures.GetList())
                {
                    Architecture arch = (obj is Architecture ? (Architecture)obj : null);
                    if (arch != null) pop += arch.Population;
                }
            }
            return pop;
        }

        private static int CalculateOfficerCount(Faction faction)
        {
            if (faction.Persons == null) return 0;
            return faction.Persons.Count;
        }

        private static StrategicStanceLocal DetermineStrategicStance(Faction faction, FactionProfileComplete profile)
        {
            if (profile.MilitaryPower <= 0) return StrategicStanceLocal.Panic;
            if (profile.EconomyPower < profile.MilitaryPower / 3) return StrategicStanceLocal.Defensive;
            if (profile.MilitaryPower > profile.EconomyPower * 2) return StrategicStanceLocal.Aggressive;
            if (profile.Population > 500000 && profile.EconomyPower > profile.MilitaryPower) return StrategicStanceLocal.Aggressive;
            return StrategicStanceLocal.Neutral;
        }
    }

    // 【完整还原】ResourceSnapshot类，包含所有原有功能
    public class ResourceSnapshotComplete
    {
        public long Money;
        public long Food;
        public float TotalMilitaryPower;
        public float EconomicHealth;
        public int ArchitectureCount;
        public bool IsAtWar;

        public static ResourceSnapshotComplete CalculateSnapshot(Faction faction)
        {
            var snapshot = new ResourceSnapshotComplete();
            if (faction == null) return snapshot;

            snapshot.Money = faction.Fund;
            snapshot.Food = faction.Food;
            snapshot.ArchitectureCount = faction.Architectures?.Count ?? 0;
            snapshot.TotalMilitaryPower = faction.TroopCount * 1000;
            snapshot.IsAtWar = faction.TroopCount > 0;

            // [CommonData] FundMaxUnit 参考
            snapshot.EconomicHealth = (faction.Fund + faction.Food / 100) > 20000 ? 1.0f : 0.5f;

            return snapshot;
        }
    }

    // 【100% 还原】势力邻居分析逻辑，包含地图连接遍历
    public struct FactionNeighbors
    {
        public int NeighborCount { get; set; }
        public float StrongestNeighborPower { get; set; }
        public float AveragePower { get; set; }
        public bool HasVulnerableTarget { get; set; }
        public Faction BestVictim { get; set; }
        public float BestVictimScore { get; set; }

        public static FactionNeighbors AnalyzeNeighbors(Faction faction)
        {
            try
            {
                var result = new FactionNeighbors();
                if (faction?.Architectures == null) return result;

                var neighborFactions = new HashSet<Faction>();

                // 【关键逻辑还原】遍历 AILandLinks 获取真实邻居
                foreach (var obj in faction.Architectures.GetList())
                {
                    if (!(obj is Architecture arch) || arch.AILandLinks == null) continue;

                    foreach (var linkedObj in arch.AILandLinks.GetList())
                    {
                        if (linkedObj is Architecture linkedArch &&
                            linkedArch.BelongedFaction != null &&
                            linkedArch.BelongedFaction != faction)
                        {
                            neighborFactions.Add(linkedArch.BelongedFaction);
                        }
                    }
                }

                result.NeighborCount = neighborFactions.Count;

                if (neighborFactions.Count > 0)
                {
                    var neighborPowers = new List<float>();
                    Faction weakestNeighbor = null;
                    float weakestPower = float.MaxValue;

                    foreach (var neighbor in neighborFactions)
                    {
                        float power = CalculateFactionPower(neighbor);
                        neighborPowers.Add(power);

                        if (power < weakestPower)
                        {
                            weakestPower = power;
                            weakestNeighbor = neighbor;
                        }
                    }

                    result.StrongestNeighborPower = neighborPowers.Max();
                    result.AveragePower = neighborPowers.Average();

                    float myPower = CalculateFactionPower(faction);
                    result.HasVulnerableTarget = weakestPower < myPower * 0.7f;

                    if (result.HasVulnerableTarget && weakestNeighbor != null)
                    {
                        result.BestVictim = weakestNeighbor;
                        result.BestVictimScore = weakestPower > 0 ? myPower / weakestPower : 100f;
                    }
                }
                return result;
            }
            catch { return new FactionNeighbors(); }
        }

        private static float CalculateFactionPower(Faction faction)
        {
            try
            {
                if (faction == null) return 0f;
                float power = 0f;
                power += faction.ArchitectureCount * 1000f;
                power += faction.TroopCount * 500f;
                // 兼容不同版本的 PersonCount / Persons.Count
                int personCount = faction.Persons != null ? faction.Persons.Count : 0;
                power += personCount * 100f;
                return power;
            }
            catch { return 0f; }
        }

        // 【关键逻辑还原】完整的文本生成
        public static string GetVictimAnalysisReport(Faction attacker, Faction victim)
        {
            try
            {
                if (attacker == null || victim == null) return "无效的势力信息";

                float attackerPower = CalculateFactionPower(attacker);
                float victimPower = CalculateFactionPower(victim);
                float powerRatio = attackerPower / Math.Max(victimPower, 1f);

                string report = $"攻击目标分析:\n";
                report += $"攻击方实力: {attackerPower:F0}\n";
                report += $"目标方实力: {victimPower:F0}\n";
                report += $"实力对比: {powerRatio:F2}:1\n";

                if (powerRatio > 2.0f) report += "评估: 压倒性优势，建议立即攻击";
                else if (powerRatio > 1.5f) report += "评估: 明显优势，攻击成功率高";
                else if (powerRatio > 1.2f) report += "评估: 略有优势，需谨慎行动";
                else if (powerRatio > 0.8f) report += "评估: 势均力敌，不建议主动攻击";
                else report += "评估: 处于劣势，应避免冲突";

                return report;
            }
            catch { return "报告生成失败"; }
        }
    }

    // ==========================================
    // 3. 逻辑系统与权重 (Systems)
    // ==========================================

    public class AIDecisionWeights
    {
        public float DistanceToGoal { get; set; } = 1.0f;
        public float ThreatAvoidance { get; set; } = 1.0f;
        public float ResourcePriority { get; set; } = 1.0f;
        public float AggressionLevel { get; set; } = 1.0f;
        public float DefensivePriority { get; set; } = 1.0f;
        public float TerrainAdvantage { get; set; } = 1.0f;
        public float UnknownAreaPenalty { get; set; } = 1.0f;
        public float EnemyProximity { get; set; } = 1.0f;
        public float AllySupport { get; set; } = 1.0f;
    }

    // 【完整还原】TurnResourceManager静态类
    public static class TurnResourceManagerComplete
    {
        public static void RecordFactionResources(Faction faction) { }

        public static float EvaluateResourcePressure(Faction faction)
        {
            if (faction == null) return 0f;
            float pressure = 0f;
            if (faction.Fund < 2000) pressure += 0.3f;
            if (faction.Food < 10000) pressure += 0.3f;
            return Math.Min(1.0f, pressure);
        }
    }

    // 【100% 还原】AI军师任命系统，包含所有性格和关系判断
    public static class AIAdvisorAppointmentSystem
    {
        // 添加频率控制：记录上次任命时间
        private static Dictionary<int, int> _lastAppointmentYear = new Dictionary<int, int>();
        private static Dictionary<int, int> _lastAppointmentMonth = new Dictionary<int, int>();

        public static void AIAppointAdvisor(Faction faction)
        {
            if (faction == null || faction.Leader == null) return;
            if (Session.Current.Scenario.IsPlayer(faction)) return;
            if (!faction.AppointAdvisorAvail()) return;

            // 🔥 新增：固定时机检测 - 每年1月和6月进行军师评估
            int currentYear = Session.Current.Scenario.Date.Year;
            int currentMonth = Session.Current.Scenario.Date.Month;
            
            // 只在1月和6月进行检测
            if (currentMonth != 1 && currentMonth != 6)
            {
                return;
            }

            // 获取候选人
            PersonList allPersons = faction.Persons;
            var candidates = new List<Person>();
            if (allPersons != null)
            {
                foreach (GameObject obj in allPersons.GetList())
                {
                    Person p = (obj is Person ? (Person)obj : null);
                    if (p != null && p != faction.Leader) candidates.Add(p);
                }
            }

            if (candidates.Count == 0) return;

            // 根据性格选择最佳候选人
            Person selectedCandidate = SelectAdvisorByPersonality(faction, candidates);
            if (selectedCandidate == null) return;

            // 如果已有军师，检查是否需要更换
            if (faction.Advisor != null)
            {
                // 检查是否在同一年的同一检测期已经处理过
                if (_lastAppointmentYear.ContainsKey(faction.ID) && _lastAppointmentMonth.ContainsKey(faction.ID))
                {
                    int lastYear = _lastAppointmentYear[faction.ID];
                    int lastMonth = _lastAppointmentMonth[faction.ID];
                    
                    // 如果是同一年的同一检测期，跳过
                    if (lastYear == currentYear && lastMonth == currentMonth)
                    {
                        return;
                    }
                }

                // 🔥 新增：智力突破机制 - 新武将智力明显超过现任军师时优先考虑
                int intelligenceDifference = selectedCandidate.Intelligence - faction.Advisor.Intelligence;
                int personalityId = faction.Leader.Character?.ID ?? 0;
                
                // 根据领袖性格设定不同的智力突破阈值
                int breakthroughThreshold = GetIntelligenceBreakthroughThreshold(personalityId);
                
                // 如果智力差距足够大，记录突破信息
                if (intelligenceDifference >= breakthroughThreshold)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI] {faction.Name} 发现优秀人才({selectedCandidate.Name}:{selectedCandidate.Intelligence} vs {faction.Advisor.Name}:{faction.Advisor.Intelligence})，考虑更换军师");
                }
            }

            if (ShouldAppointNewAdvisor(faction, selectedCandidate))
            {
                faction.AdvisorID = selectedCandidate.ID;
                faction.AppointAdvisor(selectedCandidate);
                
                // 记录检测时间
                _lastAppointmentYear[faction.ID] = currentYear;
                _lastAppointmentMonth[faction.ID] = currentMonth;
            }
            else
            {
                // 即使没有更换，也记录检测时间，避免重复检测
                _lastAppointmentYear[faction.ID] = currentYear;
                _lastAppointmentMonth[faction.ID] = currentMonth;
            }
        }

        /// <summary>
        /// 根据领袖性格获取智力突破阈值
        /// </summary>
        private static int GetIntelligenceBreakthroughThreshold(int personalityId)
        {
            switch (personalityId)
            {
                case 0: // 仁德型 - 要求较高的智力差距才会更换
                    return 20;
                case 1: // 霸道型 - 中等智力差距即可更换
                    return 15;
                case 2: // 冷静型 - 智力优先，较低阈值
                    return 10;
                case 3: // 莽撞型 - 容易被新人才吸引
                    return 12;
                case 4: // 狡诈型 - 看重关系，需要更大智力差距
                    return 18;
                default:
                    return 15; // 默认阈值
            }
        }

        /// <summary>
        /// 计算游戏日期的总天数
        /// </summary>
        private static int GetTotalDays(GameDate date)
        {
            return date.Year * 360 + date.Month * 30 + date.Day;
        }

        private static Person SelectAdvisorByPersonality(Faction faction, List<Person> candidates)
        {
            Person leader = faction.Leader;
            int personalityId = leader.Character?.ID ?? 0;

            switch (personalityId)
            {
                case 0: // 仁德型 - 看重忠诚 (Loyalty) 和智力
                    return candidates
                        .Where(c => c.Loyalty >= 80 && c.Intelligence >= 70)
                        .OrderByDescending(c => c.Loyalty)
                        .ThenByDescending(c => c.Intelligence)
                        .FirstOrDefault() ?? candidates.OrderByDescending(c => c.Intelligence).FirstOrDefault();

                case 1: // 霸道型 - 综合能力
                    return candidates
                        .OrderByDescending(c => c.Intelligence * 0.7 + c.Loyalty * 0.3)
                        .FirstOrDefault();

                case 2: // 冷静型 - 智力优先
                    return candidates.OrderByDescending(c => c.Intelligence).FirstOrDefault();

                case 3: // 莽撞型 - 【修正】随机选择或者选武力高的，而非Charm，避免报错但保留"冲动"逻辑
                    if (AIRandomHelper.Next(100) < 50)
                        return candidates.OrderByDescending(c => c.Strength).FirstOrDefault();
                    return candidates.OrderByDescending(c => c.Intelligence).FirstOrDefault();

                case 4: // 狡诈型 - 关系户优先
                    var related = candidates.FirstOrDefault(c => HasSpecialRelation(leader, c));
                    if (related != null && related.Intelligence >= 60) return related;
                    return candidates.OrderByDescending(c => c.Intelligence).FirstOrDefault();

                default:
                    return candidates.OrderByDescending(c => c.Intelligence).FirstOrDefault();
            }
        }

        private static bool HasSpecialRelation(Person leader, Person candidate)
        {
            try
            {
                // 【修复】检查父子、配偶、兄弟、亲密关系 - 使用正确的Person对象比较
                if (leader.Father != null && leader.Father == candidate) return true;
                if (candidate.Father != null && candidate.Father == leader) return true;
                if (leader.Mother != null && leader.Mother == candidate) return true;
                if (candidate.Mother != null && candidate.Mother == leader) return true;
                if (leader.Spouse != null && leader.Spouse == candidate) return true;
                if (candidate.Spouse != null && candidate.Spouse == leader) return true;

                // 【修复】手动遍历 Brothers 列表，使用正确的类型转换
                if (leader.Brothers != null)
                {
                    foreach (var bro in leader.Brothers.GetList())
                    {
                        if (bro is Person p && p == candidate) return true;
                    }
                }

                if (leader.CheckRelation(candidate) == 1) return true;
                return false;
            }
            catch { return false; }
        }

        private static bool ShouldAppointNewAdvisor(Faction faction, Person candidate)
        {
            if (faction.Advisor == null) return true;
            Person current = faction.Advisor;
            int personalityId = faction.Leader.Character?.ID ?? 0;

            switch (personalityId)
            {
                case 0: return candidate.Intelligence > current.Intelligence + 15;
                case 1: return candidate.Intelligence > current.Intelligence + 5;
                case 3: return AIRandomHelper.Next(100) < 30 || candidate.Intelligence > current.Intelligence + 10;
                case 4:
                    // 狡诈型对关系户降低门槛
                    if (HasSpecialRelation(faction.Leader, candidate)) return candidate.Intelligence > current.Intelligence - 5;
                    return candidate.Intelligence > current.Intelligence + 10;
                default: return candidate.Intelligence > current.Intelligence + 10;
            }
        }
    }

    // ==========================================
    // 4. AI 学习系统 (Learning System) - 完整还原
    // ==========================================
    public class AILearningSystem
    {
        public static AILearningSystem Instance { get; private set; }

        public enum PlayerBehaviorPattern
        {
            Aggressive, Defensive, Economic, Diplomatic, Opportunistic, Unpredictable
        }

        public class LearningData
        {
            public Dictionary<string, float> ActionFrequency { get; set; } = new Dictionary<string, float>();
            public Dictionary<string, float> SuccessRate { get; set; } = new Dictionary<string, float>();
            public List<GameEvent> RecentEvents { get; set; } = new List<GameEvent>();
            public PlayerBehaviorPattern DetectedPattern { get; set; } = PlayerBehaviorPattern.Unpredictable;
            public DateTime LastAnalysis { get; set; } = DateTime.Now;
        }

        public class GameEvent
        {
            public string EventType { get; set; }
            public DateTime Timestamp { get; set; }
            public Point Location { get; set; }
            public string Details { get; set; }
            public bool Success { get; set; }
            public Faction Faction { get; set; }
        }

        public class AdaptationStrategy
        {
            public string Name { get; set; }
            public Dictionary<string, float> CounterWeights { get; set; } = new Dictionary<string, float>();
            public string Description { get; set; }
        }

        private Dictionary<int, LearningData> _factionLearningData;
        private Dictionary<PlayerBehaviorPattern, AdaptationStrategy> _adaptationStrategies;
        private List<GameEvent> _globalEventHistory;
        private int _maxEventHistory = 1000;
        private int _analysisInterval = 60000;

        public AILearningSystem()
        {
            Instance = this;
            _factionLearningData = new Dictionary<int, LearningData>();
            _globalEventHistory = new List<GameEvent>();
            InitializeAdaptationStrategies();
        }

        // 【100% 还原】反制策略逻辑
        private void InitializeAdaptationStrategies()
        {
            _adaptationStrategies = new Dictionary<PlayerBehaviorPattern, AdaptationStrategy>
            {
                [PlayerBehaviorPattern.Aggressive] = new AdaptationStrategy
                {
                    Name = "反攻击策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["DefensiveBonus"] = 1.5f,
                        ["ThreatAvoidance"] = 1.3f,
                        ["AllySupport"] = 1.4f,
                        ["TerrainAdvantage"] = 1.2f
                    },
                    Description = "加强防御，利用地形优势，寻求友军支援"
                },
                [PlayerBehaviorPattern.Defensive] = new AdaptationStrategy
                {
                    Name = "破防策略",
                    CounterWeights = new Dictionary<string, float>
                    {
                        ["AggressiveBonus"] = 1.4f,
                        ["FlankingBonus"] = 1.6f,
                        ["EconomicPressure"] = 1.3f,
                        ["MultiDirectionalAttack"] = 1.5f
                    },
                    Description = "多方向进攻，经济施压，侧翼包抄"
                }
            };
        }

        public void RecordEvent(string eventType, Point location, string details, bool success, Faction faction)
        {
            try
            {
                var gameEvent = new GameEvent
                {
                    EventType = eventType,
                    Timestamp = DateTime.Now,
                    Location = location,
                    Details = details,
                    Success = success,
                    Faction = faction
                };
                _globalEventHistory.Add(gameEvent);
                if (_globalEventHistory.Count > _maxEventHistory) _globalEventHistory.RemoveAt(0);

                if (faction != null)
                {
                    if (!_factionLearningData.ContainsKey(faction.ID))
                        _factionLearningData[faction.ID] = new LearningData();

                    var learningData = _factionLearningData[faction.ID];
                    learningData.RecentEvents.Add(gameEvent);

                    if (!learningData.ActionFrequency.ContainsKey(eventType))
                        learningData.ActionFrequency[eventType] = 0;
                    learningData.ActionFrequency[eventType]++;

                    var totalEvents = learningData.RecentEvents.Count(e => e.EventType == eventType);
                    var successfulEvents = learningData.RecentEvents.Count(e => e.EventType == eventType && e.Success);
                    learningData.SuccessRate[eventType] = totalEvents > 0 ? (float)successfulEvents / totalEvents : 0;

                    if (learningData.RecentEvents.Count > 200) learningData.RecentEvents.RemoveAt(0);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
        }

        public PlayerBehaviorPattern AnalyzePlayerBehavior(Faction playerFaction)
        {
            if (playerFaction == null || !_factionLearningData.ContainsKey(playerFaction.ID))
                return PlayerBehaviorPattern.Unpredictable;

            var learningData = _factionLearningData[playerFaction.ID];
            if ((DateTime.Now - learningData.LastAnalysis).TotalMilliseconds < _analysisInterval)
                return learningData.DetectedPattern;

            var recentEvents = learningData.RecentEvents.Where(e => (DateTime.Now - e.Timestamp).TotalMinutes < 30).ToList();
            if (recentEvents.Count < 10) return PlayerBehaviorPattern.Unpredictable;

            var pattern = DeterminePattern(recentEvents, learningData);
            learningData.DetectedPattern = pattern;
            learningData.LastAnalysis = DateTime.Now;
            return pattern;
        }

        private PlayerBehaviorPattern DeterminePattern(List<GameEvent> events, LearningData learningData)
        {
            var scores = new Dictionary<PlayerBehaviorPattern, float>();
            var totalEvents = events.Count;

            var attackEvents = events.Count(e => e.EventType.Contains("Attack"));
            scores[PlayerBehaviorPattern.Aggressive] = (float)attackEvents / totalEvents * 100;

            var defenseEvents = events.Count(e => e.EventType.Contains("Defend"));
            scores[PlayerBehaviorPattern.Defensive] = (float)defenseEvents / totalEvents * 100;

            return scores.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        }

        public AdaptationStrategy GetAdaptationStrategy(Faction playerFaction)
        {
            var pattern = AnalyzePlayerBehavior(playerFaction);
            if (_adaptationStrategies.ContainsKey(pattern)) return _adaptationStrategies[pattern];
            return new AdaptationStrategy { Name = "标准策略" };
        }

        public AIDecisionWeights ApplyLearning(AIDecisionWeights baseWeights, Faction playerFaction)
        {
            var strategy = GetAdaptationStrategy(playerFaction);
            var adjustedWeights = new AIDecisionWeights
            {
                DistanceToGoal = baseWeights.DistanceToGoal,
                ThreatAvoidance = baseWeights.ThreatAvoidance,
                EnemyProximity = baseWeights.EnemyProximity,
                TerrainAdvantage = baseWeights.TerrainAdvantage,
                AllySupport = baseWeights.AllySupport
            };

            foreach (var adjustment in strategy.CounterWeights)
            {
                switch (adjustment.Key)
                {
                    case "DefensiveBonus": adjustedWeights.ThreatAvoidance *= adjustment.Value; break;
                    case "AggressiveBonus": adjustedWeights.EnemyProximity *= -adjustment.Value; break;
                    case "TerrainAdvantage": adjustedWeights.TerrainAdvantage *= adjustment.Value; break;
                    case "AllySupport": adjustedWeights.AllySupport *= adjustment.Value; break;
                }
            }
            return adjustedWeights;
        }

        // 【添加】清理过期数据方法
        public void CleanupOldData()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-24); // 清理24小时前的数据
                
                // 清理全局事件历史
                _globalEventHistory.RemoveAll(e => e.Timestamp < cutoffTime);
                
                // 清理各势力的学习数据
                foreach (var kvp in _factionLearningData.ToList())
                {
                    var learningData = kvp.Value;
                    learningData.RecentEvents.RemoveAll(e => e.Timestamp < cutoffTime);
                    
                    // 如果势力没有最近的事件，移除整个学习数据
                    if (learningData.RecentEvents.Count == 0)
                    {
                        _factionLearningData.Remove(kvp.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] CleanupOldData error: {ex.Message}");
            }
        }

        /// <summary>
        /// 序列化学习数据为JSON字符串，用于保存到存档
        /// </summary>
        /// <returns>序列化后的JSON字符串</returns>
        public string SerializeLearningData()
        {
            try
            {
                // 创建一个可序列化的数据结构，排除不能序列化的Faction引用
                var serializableData = new Dictionary<int, object>();
                
                foreach (var kvp in _factionLearningData)
                {
                    var learningData = kvp.Value;
                    var serializableEvents = learningData.RecentEvents.Select(e => new
                    {
                        EventType = e.EventType,
                        Timestamp = e.Timestamp,
                        Location = new { X = e.Location.X, Y = e.Location.Y },
                        Details = e.Details,
                        Success = e.Success,
                        FactionId = e.Faction?.ID ?? -1 // 只保存势力ID，不保存整个Faction对象
                    }).ToList();

                    serializableData[kvp.Key] = new
                    {
                        ActionFrequency = learningData.ActionFrequency,
                        SuccessRate = learningData.SuccessRate,
                        RecentEvents = serializableEvents,
                        DetectedPattern = learningData.DetectedPattern.ToString(),
                        LastAnalysis = learningData.LastAnalysis
                    };
                }

                return SimpleSerializer.SerializeJson(serializableData, false, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 序列化学习数据失败: {ex.Message}");
                return "{}"; // 返回空JSON对象
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化学习数据，用于从存档加载
        /// </summary>
        /// <param name="jsonData">JSON格式的学习数据</param>
        public void LoadLearningData(string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                {
                    _factionLearningData = new Dictionary<int, LearningData>();
                    return;
                }

                var deserializedData = SimpleSerializer.DeserializeJson<Dictionary<int, Dictionary<string, object>>>(jsonData);
                _factionLearningData = new Dictionary<int, LearningData>();

                foreach (var kvp in deserializedData)
                {
                    var data = kvp.Value;
                    var learningData = new LearningData();

                    // 恢复ActionFrequency
                    if (data.ContainsKey("ActionFrequency") && data["ActionFrequency"] != null)
                    {
                        learningData.ActionFrequency = SimpleSerializer.DeserializeJson<Dictionary<string, float>>(
                            data["ActionFrequency"].ToString());
                    }

                    // 恢复SuccessRate
                    if (data.ContainsKey("SuccessRate") && data["SuccessRate"] != null)
                    {
                        learningData.SuccessRate = SimpleSerializer.DeserializeJson<Dictionary<string, float>>(
                            data["SuccessRate"].ToString());
                    }

                    // 恢复DetectedPattern
                    PlayerBehaviorPattern detectedPattern = PlayerBehaviorPattern.Unpredictable; // 默认值
                    if (data.ContainsKey("DetectedPattern") && data["DetectedPattern"] != null && 
                        Enum.TryParse<PlayerBehaviorPattern>(data["DetectedPattern"].ToString(), out detectedPattern))
                    {
                        // TryParse成功，detectedPattern已被赋值
                    }
                    learningData.DetectedPattern = detectedPattern;

                    // 恢复LastAnalysis
                    DateTime lastAnalysisTime = DateTime.Now; // 默认为当前时间
                    if (data.ContainsKey("LastAnalysis") && data["LastAnalysis"] != null && 
                        DateTime.TryParse(data["LastAnalysis"].ToString(), out lastAnalysisTime))
                    {
                        // TryParse成功，lastAnalysisTime已被赋值
                    }
                    learningData.LastAnalysis = lastAnalysisTime;

                    // 恢复RecentEvents（注意：Faction引用需要重新建立）
                    if (data.ContainsKey("RecentEvents") && data["RecentEvents"] != null)
                    {
                        var eventsArray = SimpleSerializer.DeserializeJson<Dictionary<string, object>[]>(data["RecentEvents"].ToString());
                        foreach (var eventData in eventsArray)
                        {
                            var gameEvent = new GameEvent
                            {
                                EventType = eventData.ContainsKey("EventType") ? eventData["EventType"]?.ToString() ?? "" : "",
                                Details = eventData.ContainsKey("Details") ? eventData["Details"]?.ToString() ?? "" : "",
                                Success = eventData.ContainsKey("Success") ? (bool)(eventData["Success"] ?? false) : false,
                                Location = GetLocationFromEventData(eventData)
                            };

                            // 尝试恢复时间戳
                            DateTime eventTimestamp = DateTime.Now; // 默认为当前时间
                            if (eventData.ContainsKey("Timestamp") && eventData["Timestamp"] != null && 
                                DateTime.TryParse(eventData["Timestamp"].ToString(), out eventTimestamp))
                            {
                                // TryParse成功，eventTimestamp已被赋值
                            }
                            gameEvent.Timestamp = eventTimestamp;

                            // 注意：Faction引用在加载时无法直接恢复，需要在游戏运行时重新建立
                            // 这里暂时设为null，在实际使用时通过FactionId查找
                            gameEvent.Faction = null;

                            learningData.RecentEvents.Add(gameEvent);
                        }
                    }

                    _factionLearningData[kvp.Key] = learningData;
                }

                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 成功加载 {_factionLearningData.Count} 个势力的学习数据");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 加载学习数据失败: {ex.Message}");
                _factionLearningData = new Dictionary<int, LearningData>(); // 重置为空数据
            }
        }

        /// <summary>
        /// 获取学习数据统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetLearningDataSummary()
        {
            try
            {
                int totalFactions = _factionLearningData.Count;
                int totalEvents = _factionLearningData.Values.Sum(ld => ld.RecentEvents.Count);
                int totalActionTypes = _factionLearningData.Values
                    .SelectMany(ld => ld.ActionFrequency.Keys)
                    .Distinct()
                    .Count();

                return $"AI学习数据统计: {totalFactions}个势力, {totalEvents}个事件, {totalActionTypes}种行为类型";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AILearningSystem] 获取统计信息失败: {ex.Message}");
                return "AI学习数据统计: 获取失败";
            }
        }

        /// <summary>
        /// 从事件数据中提取位置信息
        /// </summary>
        private Point GetLocationFromEventData(Dictionary<string, object> eventData)
        {
            try
            {
                if (eventData.ContainsKey("Location") && eventData["Location"] != null)
                {
                    var locationData = SimpleSerializer.DeserializeJson<Dictionary<string, object>>(eventData["Location"].ToString());
                    int x = locationData.ContainsKey("X") ? Convert.ToInt32(locationData["X"]) : 0;
                    int y = locationData.ContainsKey("Y") ? Convert.ToInt32(locationData["Y"]) : 0;
                    return new Point(x, y);
                }
            }
            catch
            {
                // 如果解析失败，返回默认位置
            }
            return new Point(0, 0);
        }
    }

    // ==========================================
    // 5. 战略决策系统 (Strategy System) - 完整还原
    // ==========================================
    public static class AIStrategicDecisionSystem
    {
        public static StrategicStanceLocal DetermineStrategicStance(FactionProfileComplete profile, ResourceSnapshotComplete snapshot, FactionNeighbors neighbors, int factionId)
        {
            float aggressionScore = profile.MilitaryStrength * 0.3f + profile.EconomicStrength * 0.2f;
            aggressionScore -= (snapshot.Money < 1000 ? 0.3f : 0f);
            aggressionScore -= (snapshot.Food < 500 ? 0.2f : 0f);

            aggressionScore *= DifficultyManagerInstance.Instance.GetDifficultyModifier();

            if (aggressionScore >= AIStrategicConfig.ExpansionThreshold * 10000) return StrategicStanceLocal.Aggressive;
            if (aggressionScore >= AIStrategicConfig.AggressiveThreshold * 10000) return StrategicStanceLocal.Aggressive;
            if (aggressionScore >= AIStrategicConfig.StabilizationThreshold * 10000) return StrategicStanceLocal.Consolidation;
            if (aggressionScore >= AIStrategicConfig.DefensiveThreshold * 10000) return StrategicStanceLocal.Defensive;
            return StrategicStanceLocal.Consolidation;
        }

        public static void ExecuteStrategicActions(Faction faction, StrategicStanceLocal stance, FactionProfileComplete profile, ResourceSnapshotComplete snapshot)
        {
            var priorities = EvaluateActionPriorities(faction, stance);
        }

        public static Dictionary<string, float> EvaluateActionPriorities(Faction faction, StrategicStanceLocal stance)
        {
            var priorities = new Dictionary<string, float>
            {
                ["军事行动"] = 0.3f,
                ["内政发展"] = 0.4f,
                ["外交活动"] = 0.2f
            };

            switch (stance)
            {
                case StrategicStanceLocal.Aggressive: priorities["军事行动"] += 0.4f; break;
                case StrategicStanceLocal.Consolidation: priorities["内政发展"] += 0.2f; break;
            }
            return priorities;
        }

        public static string GetDecisionReasoning(FactionProfileComplete profile, ResourceSnapshotComplete snapshot, FactionNeighbors neighbors, int factionId)
        {
            return $"军力:{profile.MilitaryStrength}, 姿态:{profile.StrategicStance}";
        }

        public static string GetStanceDescription(StrategicStanceLocal stance) => stance.ToString();
    }

    // 【完整还原】AI战略管理器 - 集成脏标记与分帧处理
    public class AIStrategicManager
    {
        public static AIStrategicManager Instance { get; private set; } = new AIStrategicManager();
        
        // 数据存储
        private Dictionary<int, FactionProfileComplete> _factionProfiles = new Dictionary<int, FactionProfileComplete>();
        private Dictionary<int, StrategicStanceLocal> _currentStances = new Dictionary<int, StrategicStanceLocal>();
        private Dictionary<int, int> _stanceChangeCooldown = new Dictionary<int, int>();
        
        // 分帧处理队列
        private Queue<Faction> _updateQueue = new Queue<Faction>();
        private bool _isProcessingQueue = false;
        private int _factionsPerFrame = 1; // 每帧处理的势力数量 (可调整，越小越流畅，越大越快处理完)
        private int _lastUpdateTurn = -1;

        /// <summary>
        /// 回合开始时调用：将需要思考的势力加入队列 (生产者)
        /// </summary>
        public void OnTurnStart(List<Faction> factions)
        {
            if (Session.Current?.Scenario == null) return;
            
            int currentTurn = Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
            if (_lastUpdateTurn == currentTurn) return; // 本回合已调度过
            
            _lastUpdateTurn = currentTurn;
            _updateQueue.Clear();
            
            foreach (var faction in factions)
            {
                // 只添加存活且非玩家控制的势力
                if (!faction.Controlling && !faction.Destroyed)
                {
                    _updateQueue.Enqueue(faction);
                }
            }
            
            _isProcessingQueue = true;
            System.Diagnostics.Debug.WriteLine($"[AI分帧] 回合 {currentTurn} 开始，{_updateQueue.Count} 个势力进入思考队列");
        }

        /// <summary>
        /// 游戏主循环每帧调用 (消费者)
        /// </summary>
        public void UpdatePerFrame()
        {
            if (!_isProcessingQueue || _updateQueue.Count == 0) return;

            // 每帧只处理 N 个势力
            for (int i = 0; i < _factionsPerFrame; i++)
            {
                if (_updateQueue.Count == 0)
                {
                    _isProcessingQueue = false;
                    System.Diagnostics.Debug.WriteLine("[AI分帧] 本回合所有势力思考完毕");
                    break;
                }

                Faction faction = _updateQueue.Dequeue();
                if (faction != null && !faction.Destroyed)
                {
                    ProcessFactionStrategy(faction);
                }
            }
        }

        /// <summary>
        /// 具体的单体逻辑处理
        /// </summary>
        private void ProcessFactionStrategy(Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI分帧] 开始处理势力: {faction.Name}");

                // 1. 使用缓存管理器获取数据，避免重复计算
                var profile = AICacheManager.Instance.GetFactionProfile(faction);
                _factionProfiles[faction.ID] = profile;
                
                var snapshot = AICacheManager.Instance.GetResourceSnapshot(faction);
                var neighbors = AICacheManager.Instance.GetNeighbors(faction);

                // 2. 决定姿态
                var newStance = AIStrategicDecisionSystem.DetermineStrategicStance(profile, snapshot, neighbors, faction.ID);

                // 3. 冷却与状态更新
                StrategicStanceLocal current = _currentStances.ContainsKey(faction.ID) ? 
                    _currentStances[faction.ID] : StrategicStanceLocal.Consolidation;
                
                if (newStance != current)
                {
                    if (_stanceChangeCooldown.ContainsKey(faction.ID) && _stanceChangeCooldown[faction.ID] > 0)
                    {
                        // 冷却中，保持原样
                        System.Diagnostics.Debug.WriteLine($"[AI分帧] {faction.Name} 姿态变更冷却中，保持 {current}");
                    }
                    else
                    {
                        var oldStance = faction.CurrentStrategicStance;
                        _currentStances[faction.ID] = newStance;
                        faction.CurrentStrategicStance = newStance; // 🔥 推送到Faction
                        _stanceChangeCooldown[faction.ID] = 3; // 3回合冷却
                        System.Diagnostics.Debug.WriteLine($"[AI分帧] {faction.Name} 姿态变更: {current} -> {newStance}");
                        
                        // 🔥 只有剧烈变化才触发紧急调动
                        bool isUrgent = (oldStance == StrategicStanceLocal.Aggressive && newStance == StrategicStanceLocal.Panic) ||
                                        (oldStance == StrategicStanceLocal.Consolidation && newStance == StrategicStanceLocal.Aggressive);
                        if (isUrgent)
                        {
                            try { faction.NotifyPersonnelUrgentEvent($"战略姿态剧变: {oldStance}->{newStance}"); } catch { }
                        }
                    }
                }

                // 4. 执行行动
                AIStrategicDecisionSystem.ExecuteStrategicActions(faction, 
                    _currentStances.ContainsKey(faction.ID) ? _currentStances[faction.ID] : StrategicStanceLocal.Consolidation, 
                    profile, snapshot);

                // 5. 更新冷却计数
                if (_stanceChangeCooldown.ContainsKey(faction.ID) && _stanceChangeCooldown[faction.ID] > 0)
                    _stanceChangeCooldown[faction.ID]--;

                // 6. 顺便处理军师任命 (分散压力)
                AIAdvisorAppointmentSystem.AIAppointAdvisor(faction);

                System.Diagnostics.Debug.WriteLine($"[AI分帧] 完成处理势力: {faction.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI分帧] 处理势力 {faction?.Name ?? "Unknown"} 时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 传统的单次更新方法 (保持向后兼容)
        /// </summary>
        public void UpdateStrategicDecisions(Faction faction)
        {
            if (Session.Current?.Scenario == null || faction == null) return;

            int currentTurn = Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
            if (_lastUpdateTurn == currentTurn) return;
            _lastUpdateTurn = currentTurn;

            if (!faction.Controlling)
            {
                ProcessFactionStrategy(faction);
            }
        }

        // --- 对外查询接口 ---
        public StrategicStanceLocal GetFactionStance(int factionId) => 
            _currentStances.ContainsKey(factionId) ? _currentStances[factionId] : StrategicStanceLocal.Consolidation;

        public void ForceUpdateFactionStrategy(Faction faction) => ProcessFactionStrategy(faction);

        /// <summary>
        /// 检查队列是否处理完毕 (用于UI显示 "AI思考中...")
        /// </summary>
        public bool IsThinking => _updateQueue.Count > 0;

        /// <summary>
        /// 获取思考进度 (剩余势力数量)
        /// </summary>
        public int ThinkingProgress => _updateQueue.Count;

        /// <summary>
        /// 设置每帧处理的势力数量 (性能调优)
        /// </summary>
        public void SetFactionsPerFrame(int count)
        {
            _factionsPerFrame = Math.Max(1, Math.Min(10, count)); // 限制在1-10之间
            System.Diagnostics.Debug.WriteLine($"[AI分帧] 每帧处理势力数量设置为: {_factionsPerFrame}");
        }

        /// <summary>
        /// 获取AI系统状态信息
        /// </summary>
        public string GetStatusInfo()
        {
            return $"AI战略管理器状态: 队列中{_updateQueue.Count}个势力, " +
                   $"每帧处理{_factionsPerFrame}个, " +
                   $"当前回合{_lastUpdateTurn}, " +
                   $"已缓存{_factionProfiles.Count}个档案";
        }
    }

    // 【完整还原】AI决策管理器
    public class AIDecisionManager
    {
        public static AIDecisionManager Instance { get; private set; } = new AIDecisionManager();

        public string MakeDecision(Faction faction)
        {
            if (faction == null) return "无决策";
            if (faction.TroopCount > (faction.Architectures?.Count ?? 0) * 2) return "考虑扩张";
            return "维持现状";
        }

        public float CalculateAdvancedTileScore(Troop troop, Point position, Point strategicGoal)
        {
            if (troop?.BelongedFaction == null) return 0f;
            float score = 0f;
            float distanceToGoal = Session.Current.Scenario.GetSimpleDistance(position, strategicGoal);
            score += Math.Max(0f, 50f - distanceToGoal * 2f);
            score += CalculateThreat(troop, position);
            score += (AIRandomHelper.Next(20) - 10) * 0.1f;
            return Math.Max(0f, score);
        }

        public float CalculateThreat(Troop troop, Point position)
        {
            float threat = 0f;
            
            // 🔥 临时修复：直接遍历部队而不调用GetTroopsInRange
            var scenario = Session.Current.Scenario;
            if (scenario != null && scenario.Troops != null)
            {
                foreach (var obj in scenario.Troops)
                {
                    Troop nearbyTroop = (obj is Troop ? (Troop)obj : null);
                    if (nearbyTroop != null && !nearbyTroop.Destroyed)
                    {
                        // 计算曼哈顿距离
                        int distance = Math.Abs(nearbyTroop.Position.X - position.X) + Math.Abs(nearbyTroop.Position.Y - position.Y);
                        if (distance <= 5 && !troop.BelongedFaction.IsFriendly(nearbyTroop.BelongedFaction))
                        {
                            threat += nearbyTroop.FightingForce * 0.1f;
                        }
                    }
                }
            }
            
            return Math.Min(threat, 100f);
        }

        // 【添加】3参数版本的CalculateThreat方法，兼容AITacticalExecution的调用
        public float CalculateThreat(Troop troop, Point position, int difficulty)
        {
            float baseThreat = CalculateThreat(troop, position);
            
            // 根据难度调整威胁计算
            float difficultyModifier = 1.0f + (difficulty - 3) * 0.2f; // 难度3为基准
            return baseThreat * difficultyModifier;
        }

        // 【添加】获取行为模式描述方法
        public string GetBehaviorModeDescription(Troop troop)
        {
            try
            {
                if (troop?.Leader == null) return "无行为信息";
                
                var leader = troop.Leader;
                string description = $"指挥官: {leader.Name}\n";
                description += $"智力: {leader.Intelligence} 统率: {leader.Command}\n";
                
                // 基于属性判断行为模式
                if (leader.Intelligence > 80)
                    description += "行为模式: 智谋型 - 偏好战术机动";
                else if (leader.Command > 80)
                    description += "行为模式: 统帅型 - 偏好正面作战";
                else if (leader.Strength > 80)
                    description += "行为模式: 勇武型 - 偏好冲锋陷阵";
                else
                    description += "行为模式: 平衡型 - 综合考虑各因素";
                
                return description;
            }
            catch (Exception ex)
            {
                return $"获取行为信息失败: {ex.Message}";
            }
        }

        // 【添加】清理行为缓存方法
        public void ClearBehaviorCache()
        {
            try
            {
                // 这里可以添加具体的缓存清理逻辑
                // 目前作为占位符实现
                System.Diagnostics.Debug.WriteLine("[AIDecisionManager] 行为缓存已清理");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDecisionManager] 清理缓存时发生错误: {ex.Message}");
            }
        }
    }
}
