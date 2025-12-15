using System;
using System.Collections.Generic;
using System.Linq;
using GameGlobal;

namespace GameObjects
{
    /// <summary>
    /// 高级人事管理器 - 扩展PersonnelManager的智能功能
    /// </summary>
    public class AdvancedPersonnelManager : PersonnelManager
    {
        private Dictionary<Officer, List<float>> _performanceHistory;
        private Dictionary<City, List<float>> _cityStabilityHistory;
        private int _currentSeason;
        private Random _random;

        public AdvancedPersonnelManager(Faction faction) : base(faction)
        {
            _performanceHistory = new Dictionary<Officer, List<float>>();
            _cityStabilityHistory = new Dictionary<City, List<float>>();
            _currentSeason = 0;
            _random = new Random();
        }

        /// <summary>
        /// 季节性人事评估 - 每季度调用
        /// </summary>
        public void SeasonalAssessment()
        {
            _currentSeason++;
            System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] 开始第{_currentSeason}季度人事评估");

            // 1. 评估所有太守的季度表现
            EvaluateQuarterlyPerformance();

            // 2. 预测潜在风险
            PredictPotentialRisks();

            // 3. 动态调整任命策略
            AdjustAppointmentStrategy();

            // 4. 执行常规人事更新
            UpdateAssignments();

            // 5. 生成季度报告
            GenerateQuarterlyReport();
        }

        /// <summary>
        /// 评估季度表现
        /// </summary>
        private void EvaluateQuarterlyPerformance()
        {
            foreach (var city in _faction.Cities)
            {
                if (city.Prefect == null) continue;

                float performance = CalculateQuarterlyPerformance(city, city.Prefect);
                
                // 记录表现历史
                if (!_performanceHistory.ContainsKey(city.Prefect))
                    _performanceHistory[city.Prefect] = new List<float>();
                
                _performanceHistory[city.Prefect].Add(performance);
                
                // 只保留最近8个季度的记录
                if (_performanceHistory[city.Prefect].Count > 8)
                    _performanceHistory[city.Prefect].RemoveAt(0);

                UpdateOfficerPerformance(city.Prefect, performance);
                
                System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] {city.Prefect.Name} 在 {city.Name} 的季度表现: {performance:F2}");
            }
        }

        /// <summary>
        /// 计算季度表现
        /// </summary>
        private float CalculateQuarterlyPerformance(City city, Officer prefect)
        {
            float performance = 0.5f; // 基础表现

            // 城市发展指标
            performance += city.Prosperity * 0.2f;
            performance += city.Security * 0.15f;
            performance += city.Loyalty * 0.15f;

            // 人口增长
            float populationGrowth = (float)city.Population / city.MaxPopulation;
            performance += populationGrowth * 0.1f;

            // 建设完成情况
            if (city.Buildings.Count > 3) performance += 0.1f;

            // 太守能力匹配度
            float threat = StrategicMap.GetThreatLevel(city);
            if (threat > 0.6f)
            {
                // 前线城市看军事能力
                float militaryFit = (prefect.Leadership + prefect.War) / 200f;
                performance += militaryFit * 0.2f;
            }
            else
            {
                // 后方城市看政治能力
                float politicalFit = (prefect.Politics + prefect.Intelligence) / 200f;
                performance += politicalFit * 0.2f;
            }

            // 随机事件影响
            float randomFactor = (float)(_random.NextDouble() - 0.5) * 0.1f;
            performance += randomFactor;

            return Math.Max(0f, Math.Min(1f, performance));
        }

        /// <summary>
        /// 预测潜在风险
        /// </summary>
        private void PredictPotentialRisks()
        {
            var riskPredictions = new List<string>();

            foreach (var officer in _faction.Officers.Where(o => o.State == OfficerState.Active))
            {
                float riskScore = PredictOfficerRisk(officer);
                
                if (riskScore > 0.7f)
                {
                    riskPredictions.Add($"{officer.Name}: 高风险 ({riskScore:F2})");
                }
                else if (riskScore > 0.5f)
                {
                    riskPredictions.Add($"{officer.Name}: 中等风险 ({riskScore:F2})");
                }
            }

            if (riskPredictions.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 风险预测:");
                foreach (var prediction in riskPredictions)
                {
                    System.Diagnostics.Debug.WriteLine($"  {prediction}");
                }
            }
        }

        /// <summary>
        /// 预测武将风险
        /// </summary>
        private float PredictOfficerRisk(Officer officer)
        {
            float risk = 0f;

            // 基础风险因子
            if (officer.Loyalty < 70) risk += 0.3f;
            if (officer.Ambition > 80) risk += 0.2f;
            if (officer.Righteousness < 50) risk += 0.2f;

            // 表现趋势分析
            if (_performanceHistory.ContainsKey(officer) && _performanceHistory[officer].Count >= 3)
            {
                var recent = _performanceHistory[officer].TakeLast(3).ToList();
                var trend = (recent[2] - recent[0]) / 2f; // 简单趋势计算
                
                if (trend < -0.1f) risk += 0.15f; // 表现下降趋势
            }

            // 关系网络风险
            if (_faction.Ruler != null)
            {
                var relation = officer.GetRelationWith(_faction.Ruler);
                if (relation == RelationType.Hated) risk += 0.4f;
                else if (relation == RelationType.SwornBrother || relation == RelationType.Spouse) risk -= 0.3f;
            }

            // 年龄和健康风险
            if (officer.Age > 65) risk += 0.1f;
            if (officer.Health < 50) risk += 0.1f;

            // 性格风险
            if (officer.Traits.Contains(Trait.Rebellious)) risk += 0.2f;
            if (officer.Traits.Contains(Trait.Greedy)) risk += 0.1f;

            return Math.Max(0f, Math.Min(1f, risk));
        }

        /// <summary>
        /// 动态调整任命策略
        /// </summary>
        private void AdjustAppointmentStrategy()
        {
            // 根据当前势力状态调整策略
            switch (_faction.State)
            {
                case FactionState.WarTime:
                    // 战时优先军事能力，降低忠诚度要求
                    System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 战时策略：优先军事能力");
                    break;
                    
                case FactionState.EconomicCrisis:
                    // 经济危机时优先政治能力
                    System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 经济危机策略：优先政治能力");
                    break;
                    
                case FactionState.Expansion:
                    // 扩张期平衡发展
                    System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 扩张策略：平衡发展");
                    break;
            }
        }

        /// <summary>
        /// 智能轮岗系统
        /// </summary>
        public void ImplementRotationSystem()
        {
            System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 执行智能轮岗");

            var rotationCandidates = new List<(Officer officer, City currentCity, float tenure)>();

            // 找出任职时间过长的太守
            foreach (var city in _faction.Cities)
            {
                if (city.Prefect == null) continue;

                // 模拟任职时间（实际游戏中应该记录真实时间）
                float tenure = _performanceHistory.ContainsKey(city.Prefect) 
                    ? _performanceHistory[city.Prefect].Count 
                    : 1;

                if (tenure >= 6) // 任职6个季度以上考虑轮岗
                {
                    rotationCandidates.Add((city.Prefect, city, tenure));
                }
            }

            // 执行轮岗
            foreach (var (officer, city, tenure) in rotationCandidates)
            {
                var newPosition = FindBetterPosition(officer, city);
                if (newPosition != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] 轮岗：{officer.Name} 从 {city.Name} 调往 {newPosition.Name}");
                    
                    city.RemovePrefect();
                    IssueTransferOrder(officer, newPosition, "智能轮岗");
                }
            }
        }

        /// <summary>
        /// 为武将寻找更好的职位
        /// </summary>
        private City FindBetterPosition(Officer officer, City currentCity)
        {
            var availableCities = _faction.Cities.Where(c => c.Prefect == null).ToList();
            
            if (!availableCities.Any()) return null;

            float currentScore = CalculateAppointmentScore(officer, StrategicMap.GetThreatLevel(currentCity) > 0.6f);
            
            return availableCities
                .Where(c => CalculateAppointmentScore(officer, StrategicMap.GetThreatLevel(c) > 0.6f) > currentScore * 1.1f)
                .OrderByDescending(c => CalculateAppointmentScore(officer, StrategicMap.GetThreatLevel(c) > 0.6f))
                .FirstOrDefault();
        }

        /// <summary>
        /// 人才培养系统
        /// </summary>
        public void DevelopTalent()
        {
            System.Diagnostics.Debug.WriteLine("[AdvancedPersonnelManager] 执行人才培养计划");

            var youngOfficers = _faction.Officers
                .Where(o => o.Age < 35 && o.State == OfficerState.Active)
                .OrderByDescending(o => GetOfficerPotential(o))
                .Take(5)
                .ToList();

            foreach (var officer in youngOfficers)
            {
                var developmentPlan = CreateDevelopmentPlan(officer);
                System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] {officer.Name} 培养计划: {developmentPlan}");
                
                // 实施培养计划（提升属性）
                ImplementDevelopmentPlan(officer, developmentPlan);
            }
        }

        /// <summary>
        /// 计算武将潜力
        /// </summary>
        private float GetOfficerPotential(Officer officer)
        {
            float potential = 0f;

            // 年龄因子（年轻人潜力更大）
            potential += Math.Max(0, (40 - officer.Age) / 40f) * 0.3f;

            // 基础能力
            float avgAbility = (officer.Politics + officer.Leadership + officer.Intelligence + officer.War) / 4f;
            potential += avgAbility / 100f * 0.4f;

            // 性格特质
            if (officer.Traits.Contains(Trait.Ambitious)) potential += 0.1f;
            if (officer.Traits.Contains(Trait.Scholar)) potential += 0.1f;
            if (officer.Traits.Contains(Trait.Pragmatic)) potential += 0.1f;

            return potential;
        }

        /// <summary>
        /// 创建发展计划
        /// </summary>
        private string CreateDevelopmentPlan(Officer officer)
        {
            var plans = new List<string>();

            if (officer.Politics < 70) plans.Add("政治培训");
            if (officer.Leadership < 70) plans.Add("军事指挥培训");
            if (officer.Intelligence < 70) plans.Add("策略学习");
            if (officer.War < 70) plans.Add("武艺训练");

            return plans.Any() ? string.Join(", ", plans) : "综合提升";
        }

        /// <summary>
        /// 实施发展计划
        /// </summary>
        private void ImplementDevelopmentPlan(Officer officer, string plan)
        {
            // 简化实现：随机提升1-3点属性
            int improvement = _random.Next(1, 4);

            if (plan.Contains("政治")) officer.Politics = Math.Min(100, officer.Politics + improvement);
            if (plan.Contains("军事")) officer.Leadership = Math.Min(100, officer.Leadership + improvement);
            if (plan.Contains("策略")) officer.Intelligence = Math.Min(100, officer.Intelligence + improvement);
            if (plan.Contains("武艺")) officer.War = Math.Min(100, officer.War + improvement);

            // 添加培训经历
            officer.AddExperience($"培训_{plan}_{_currentSeason}季度");
        }

        /// <summary>
        /// 生成季度报告
        /// </summary>
        private void GenerateQuarterlyReport()
        {
            var report = $"\n=== 第{_currentSeason}季度人事报告 ===\n";

            // 表现统计
            var performances = _performanceHistory.Values
                .Where(h => h.Any())
                .Select(h => h.Last())
                .ToList();

            if (performances.Any())
            {
                report += $"平均表现: {performances.Average():F2}\n";
                report += $"最佳表现: {performances.Max():F2}\n";
                report += $"最差表现: {performances.Min():F2}\n";
            }

            // 风险统计
            var highRiskOfficers = _faction.Officers
                .Where(o => PredictOfficerRisk(o) > 0.7f)
                .Count();

            report += $"高风险武将数量: {highRiskOfficers}\n";

            // 调动统计
            var activeTransfers = GetActiveTransfers().Count;
            report += $"进行中调动: {activeTransfers}\n";

            System.Diagnostics.Debug.WriteLine(report);
        }

        /// <summary>
        /// 应急响应系统
        /// </summary>
        public void EmergencyResponse(string emergencyType, City affectedCity = null)
        {
            System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] 应急响应: {emergencyType}");

            switch (emergencyType.ToLower())
            {
                case "rebellion":
                    HandleRebellion(affectedCity);
                    break;
                    
                case "invasion":
                    HandleInvasion(affectedCity);
                    break;
                    
                case "natural_disaster":
                    HandleNaturalDisaster(affectedCity);
                    break;
                    
                case "economic_crisis":
                    HandleEconomicCrisis();
                    break;
            }
        }

        /// <summary>
        /// 处理叛乱
        /// </summary>
        private void HandleRebellion(City city)
        {
            if (city?.Prefect != null)
            {
                // 立即撤换可疑太守
                var stability = CalculateStabilityScore(city.Prefect);
                if (stability < 0.5f)
                {
                    city.DismissPrefect();
                    System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] 应急撤换 {city.Name} 太守");
                }
            }

            // 派遣最忠诚的武将
            var loyalOfficer = _faction.Officers
                .Where(o => o.State == OfficerState.Active && o.Loyalty > 90)
                .OrderByDescending(o => o.Leadership)
                .FirstOrDefault();

            if (loyalOfficer != null && city != null)
            {
                IssueTransferOrder(loyalOfficer, city, "平定叛乱");
            }
        }

        /// <summary>
        /// 处理入侵
        /// </summary>
        private void HandleInvasion(City city)
        {
            // 派遣最强军事武将
            var militaryOfficer = _faction.Officers
                .Where(o => o.State == OfficerState.Active)
                .OrderByDescending(o => o.Leadership + o.War)
                .FirstOrDefault();

            if (militaryOfficer != null && city != null)
            {
                IssueTransferOrder(militaryOfficer, city, "抵御入侵");
            }
        }

        /// <summary>
        /// 处理自然灾害
        /// </summary>
        private void HandleNaturalDisaster(City city)
        {
            // 派遣政治能力强的武将处理救灾
            var administrativeOfficer = _faction.Officers
                .Where(o => o.State == OfficerState.Active)
                .OrderByDescending(o => o.Politics + o.Intelligence)
                .FirstOrDefault();

            if (administrativeOfficer != null && city != null)
            {
                IssueTransferOrder(administrativeOfficer, city, "灾后重建");
            }
        }

        /// <summary>
        /// 处理经济危机
        /// </summary>
        private void HandleEconomicCrisis()
        {
            // 重新评估所有后方城市的太守，优先政治能力
            var backCities = _faction.Cities.Where(c => StrategicMap.GetThreatLevel(c) <= 0.5f);
            
            foreach (var city in backCities)
            {
                if (city.Prefect == null || city.Prefect.Politics < 70)
                {
                    var economicExpert = _faction.Officers
                        .Where(o => o.State == OfficerState.Active && o.Politics > 80)
                        .OrderByDescending(o => o.Politics)
                        .FirstOrDefault();

                    if (economicExpert != null)
                    {
                        if (city.Prefect != null) city.RemovePrefect();
                        IssueTransferOrder(economicExpert, city, "经济危机应对");
                    }
                }
            }
        }
    }
}