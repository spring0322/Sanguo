using System;
using System.Collections.Generic;
using System.Linq;
using GameGlobal;

namespace GameObjects
{
    /// <summary>
    /// 武将状态枚举
    /// </summary>
    public enum OfficerState
    {
        Active,         // 活跃状态
        Captured,       // 被俘虏
        Wandering,      // 在野
        Retired,        // 退休
        Dead,           // 死亡
        Injured,        // 受伤
        Traveling       // 调动中
    }

    /// <summary>
    /// 调动命令类
    /// </summary>
    public class TransferOrder
    {
        public Officer Officer { get; set; }        // 被调动的武将
        public City Destination { get; set; }       // 目标城市
        public DateTime IssueDate { get; set; }     // 发布日期
        public int EstimatedDays { get; set; }      // 预计到达天数
        public bool IsCompleted { get; set; }       // 是否完成
        public string Reason { get; set; }          // 调动原因

        public TransferOrder(Officer officer, City destination, string reason = "")
        {
            Officer = officer;
            Destination = destination;
            IssueDate = DateTime.Now;
            EstimatedDays = CalculateTravelTime(officer.Location, destination);
            IsCompleted = false;
            Reason = reason;
        }

        private int CalculateTravelTime(City from, City to)
        {
            if (from == null || to == null) return 1;
            
            // 简化计算：基于距离
            float distance = Math.Abs(from.Coordinates.X - to.Coordinates.X) + 
                           Math.Abs(from.Coordinates.Y - to.Coordinates.Y);
            
            return Math.Max(1, (int)(distance / 2)); // 每2个单位距离需要1天
        }
    }

    /// <summary>
    /// 人事管理器 - 智能武将任命与调动系统
    /// </summary>
    public class PersonnelManager
    {
        private Faction _faction;
        private List<TransferOrder> _activeTransfers;
        private Dictionary<Officer, float> _performanceRatings;

        public PersonnelManager(Faction faction)
        {
            _faction = faction;
            _activeTransfers = new List<TransferOrder>();
            _performanceRatings = new Dictionary<Officer, float>();
        }

        /// <summary>
        /// 核心方法：执行自动任命与调动 - 每季度或每月调用一次
        /// </summary>
        public void UpdateAssignments()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 开始为 {_faction.Name} 更新人事安排");

                // 1. 清洗阶段：把危险分子踢下去
                PurgeDisloyalPrefects();

                // 2. 任命阶段：为空缺城市寻找太守
                AppointNewPrefects();

                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 人事更新完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 人事更新异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清洗阶段：检查现有太守，解除危险分子职务
        /// </summary>
        private void PurgeDisloyalPrefects()
        {
            System.Diagnostics.Debug.WriteLine("[PersonnelManager] 开始清洗阶段，检查现有太守忠诚度");

            foreach (var city in _faction.Cities)
            {
                if (city.Prefect == null) continue;

                // 调用已经写好的忠诚度/风险补丁逻辑
                // 假设返回值 < 0.3 代表极度危险
                float stability = CalculateStabilityScore(city.Prefect);

                // 如果极度危险，且不是君主的死忠 (义兄弟/配偶)
                if (stability < 0.3f && !city.Prefect.IsSoulBoundTo(_faction.Ruler))
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 发现危险太守：{city.Prefect.Name} (稳定性:{stability:F2})");
                    
                    // 撤职！
                    city.DismissPrefect();
                    
                    // 可选：将武将强制召回首都监控
                    var capital = _faction.Cities.FirstOrDefault(c => c.IsCapital);
                    if (capital != null && city.Prefect?.Location != capital)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 将危险武将召回首都监控");
                    }
                }
            }
        }

        /// <summary>
        /// 任命阶段：为空缺城市寻找太守
        /// </summary>
        private void AppointNewPrefects()
        {
            var availableOfficers = _faction.Officers
                .Where(o => o.State == OfficerState.Active && o.Location != null) // 确保人在
                .ToList();

            // 按战略威胁度排序城市：前线优先挑人
            var citiesByPriority = _faction.Cities
                .OrderByDescending(c => StrategicMap.GetThreatLevel(c))
                .ToList();

            HashSet<Officer> assigned = new HashSet<Officer>();

            // 预先标记现任太守
            foreach (var c in _faction.Cities)
            {
                if (c.Prefect != null) assigned.Add(c.Prefect);
            }

            foreach (var city in citiesByPriority)
            {
                if (city.Prefect != null) continue; // 已有太守

                float threat = StrategicMap.GetThreatLevel(city);
                bool isFrontline = threat > 0.6f;

                // === 核心：综合评分筛选 ===
                var bestCandidate = availableOfficers
                    .Where(o => !assigned.Contains(o))
                    .Select(o => new { 
                        Officer = o, 
                        Score = CalculateAppointmentScore(o, isFrontline) 
                    })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (bestCandidate != null && bestCandidate.Score > 50) // 设定一个及格线
                {
                    city.AppointPrefect(bestCandidate.Officer);
                    assigned.Add(bestCandidate.Officer);
                    
                    System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 任命 {bestCandidate.Officer.Name} 为 {city.Name} 太守 (评分:{bestCandidate.Score:F1}, {'前线' if isFrontline else '后方'})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {city.Name} 无合适武将任命 (最高评分:{bestCandidate?.Score:F1 ?? 0})");
                }
            }
        }

        /// <summary>
        /// 整合后的终极评分公式 - 综合能力、关系、风险的全面评估
        /// </summary>
        private float CalculateAppointmentScore(Officer officer, bool isFrontline)
        {
            float score = 0;

            // A. 能力分 (Merit) - 基础能力评估
            if (isFrontline)
            {
                // 前线看统率和武力
                score = officer.Leadership * 2.0f + officer.War * 1.5f;
                
                // 胆小性格在前线扣分
                if (officer.Traits.Contains(Trait.Timid)) 
                    score *= 0.7f;
                
                // 莽撞性格在前线加分
                if (officer.Traits.Contains(Trait.Rash)) 
                    score *= 1.2f;
                
                // 谨慎性格在高威胁前线有优势
                if (officer.Traits.Contains(Trait.Cautious)) 
                    score *= 1.1f;
            }
            else
            {
                // 后方看政治
                score = officer.Politics * 2.5f + officer.Intelligence * 1.0f;
                
                // 贪婪性格在后方扣分 (怕贪污)
                if (officer.Traits.Contains(Trait.Greedy)) 
                    score *= 0.8f;
                
                // 学者性格适合内政
                if (officer.Traits.Contains(Trait.Scholar)) 
                    score *= 1.3f;
                
                // 实用主义者适合管理
                if (officer.Traits.Contains(Trait.Pragmatic)) 
                    score *= 1.1f;
            }

            // B. 关系加成 (Relations/Nepotism) - 关系网络影响
            if (_faction.Ruler != null && officer.Relations.ContainsKey(_faction.Ruler.ID))
            {
                var relation = officer.Relations[_faction.Ruler.ID];
                
                switch (relation)
                {
                    case RelationType.SwornBrother:
                    case RelationType.Spouse:
                        score *= 1.5f; // 自己人，放权！
                        System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {officer.Name} 因绝对忠诚关系获得1.5倍加成");
                        break;
                        
                    case RelationType.ParentChild:
                        score *= 1.3f; // 亲子关系高信任
                        break;
                        
                    case RelationType.Friend:
                        score *= 1.2f; // 友好关系加成
                        break;
                        
                    case RelationType.Hated:
                        score = 0; // 绝对不用死敌
                        System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {officer.Name} 因厌恶君主被排除");
                        return score;
                }
            }

            // C. 风险修正 (Stability) - 调用已有的补丁逻辑
            // 如果是义兄弟，CalculateStabilityScore 应该返回 1.0，否则根据忠诚/野心衰减
            float stabilityScore = CalculateStabilityScore(officer);
            score *= stabilityScore;

            // D. 经验加成 - 实战经验提升
            if (isFrontline)
            {
                if (officer.HasExperience("SiegeDefense")) score *= 1.2f;
                if (officer.HasExperience("FieldBattle")) score *= 1.15f;
            }
            else
            {
                if (officer.HasExperience("CityManagement")) score *= 1.25f;
                if (officer.HasExperience("EconomicReform")) score *= 1.2f;
            }

            // E. 年龄影响
            if (officer.Age > 60)
                score *= 0.9f; // 年老体衰
            else if (officer.Age < 30)
                score *= 1.05f; // 年轻有为

            System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {officer.Name} 评分详情: 基础能力={score / stabilityScore:F1}, 稳定性={stabilityScore:F2}, 最终={score:F1}");

            return score;
        }

        /// <summary>
        /// 处理单个城市的人事安排
        /// </summary>
        private void ProcessCityAssignment(City city, List<Officer> allOfficers, HashSet<Officer> assignedPrefects)
        {
            float threat = GetCityThreatLevel(city);
            Officer bestCandidate = null;
            string assignmentReason = "";

            if (threat > 0.5f)
            {
                // === 前线城市选拔标准 ===
                // 统率最重要(带兵)，武力次之(单挑/战法)，性格也要考虑(莽撞的适合前线)
                bestCandidate = allOfficers
                    .Where(o => !assignedPrefects.Contains(o)) // 还没被分配
                    .Where(o => IsSuitableForImportantPosition(o, city)) // 忠诚度检查
                    .OrderByDescending(o => CalculateMilitaryScore(o, threat))
                    .FirstOrDefault();

                assignmentReason = $"前线防务 (威胁等级: {threat:F2})";
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {city.Name} 为前线城市，威胁等级: {threat:F2}");
            }
            else
            {
                // === 后方城市选拔标准 ===
                // 政治最重要(搞钱)，智力次之(防计策)
                bestCandidate = allOfficers
                    .Where(o => !assignedPrefects.Contains(o))
                    .Where(o => IsSuitableForImportantPosition(o, city)) // 忠诚度检查
                    .OrderByDescending(o => CalculateDomesticScore(o, city))
                    .FirstOrDefault();

                assignmentReason = $"内政管理 (威胁等级: {threat:F2})";
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {city.Name} 为后方城市，威胁等级: {threat:F2}");
            }

            // 如果没有找到合适的高忠诚度武将，降低标准再试一次
            if (bestCandidate == null)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {city.Name} 无高忠诚度武将，降低标准重新选择");
                
                if (threat > 0.5f)
                {
                    bestCandidate = allOfficers
                        .Where(o => !assignedPrefects.Contains(o))
                        .Where(o => !IsOfficerRisky(o)) // 至少不能是危险分子
                        .OrderByDescending(o => CalculateMilitaryScore(o, threat))
                        .FirstOrDefault();
                }
                else
                {
                    bestCandidate = allOfficers
                        .Where(o => !assignedPrefects.Contains(o))
                        .Where(o => !IsOfficerRisky(o)) // 至少不能是危险分子
                        .OrderByDescending(o => CalculateDomesticScore(o, city))
                        .FirstOrDefault();
                }
                
                if (bestCandidate != null)
                {
                    assignmentReason += " (降低忠诚度标准)";
                }
            }

            // === 执行任命与调动 ===
            if (bestCandidate != null)
            {
                ExecuteAssignment(city, bestCandidate, assignmentReason);
                assignedPrefects.Add(bestCandidate);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {city.Name} 无可用武将任命");
            }
        }

        /// <summary>
        /// 执行具体的任命或调动
        /// </summary>
        private void ExecuteAssignment(City city, Officer candidate, string reason)
        {
            // 检查当前太守情况
            var currentPrefect = city.Prefect;
            
            // 如果已经是最佳人选，无需调动
            if (currentPrefect == candidate)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {candidate.Name} 已经是 {city.Name} 的太守，无需调动");
                return;
            }

            // 如果有现任太守，需要先解除职务
            if (currentPrefect != null)
            {
                city.RemovePrefect();
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 解除 {currentPrefect.Name} 在 {city.Name} 的太守职务");
            }

            // 1. 如果该武将已经在城里，直接上任
            if (candidate.Location == city)
            {
                city.AppointPrefect(candidate);
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 任命 {candidate.Name} 为 {city.Name} 太守 ({reason})");
            }
            // 2. 如果武将在别的城，发布调动命令 (Transfer Order)
            else
            {
                IssueTransferOrder(candidate, city, reason);
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 发布调动令：{candidate.Name} 从 {candidate.Location?.Name ?? "未知"} 调往 {city.Name} ({reason})");
            }
        }

        /// <summary>
        /// 发布调动命令
        /// </summary>
        private void IssueTransferOrder(Officer officer, City destination, string reason)
        {
            // 检查是否已有调动命令
            var existingOrder = _activeTransfers.FirstOrDefault(t => t.Officer == officer && !t.IsCompleted);
            if (existingOrder != null)
            {
                // 取消旧命令，发布新命令
                existingOrder.IsCompleted = true;
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 取消 {officer.Name} 的旧调动命令");
            }

            var transferOrder = new TransferOrder(officer, destination, reason);
            _activeTransfers.Add(transferOrder);

            // 设置武将状态为调动中
            officer.State = OfficerState.Traveling;
            
            System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 调动命令已发布：{officer.Name} → {destination.Name}，预计 {transferOrder.EstimatedDays} 天到达");
        }

        /// <summary>
        /// 处理正在进行的调动
        /// </summary>
        private void ProcessActiveTransfers()
        {
            var completedTransfers = new List<TransferOrder>();

            foreach (var transfer in _activeTransfers.Where(t => !t.IsCompleted))
            {
                // 简化处理：假设每次更新代表1天
                transfer.EstimatedDays--;

                if (transfer.EstimatedDays <= 0)
                {
                    // 调动完成
                    CompleteTransfer(transfer);
                    completedTransfers.Add(transfer);
                }
            }

            // 清理已完成的调动
            foreach (var completed in completedTransfers)
            {
                _activeTransfers.Remove(completed);
            }
        }

        /// <summary>
        /// 完成调动
        /// </summary>
        private void CompleteTransfer(TransferOrder transfer)
        {
            transfer.IsCompleted = true;
            transfer.Officer.State = OfficerState.Active;
            transfer.Officer.Location = transfer.Destination;
            
            // 任命为太守
            transfer.Destination.AppointPrefect(transfer.Officer);
            
            System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 调动完成：{transfer.Officer.Name} 到达 {transfer.Destination.Name} 并就任太守");
        }

        /// <summary>
        /// 优化现有任命
        /// </summary>
        private void OptimizeExistingAssignments(List<Officer> allOfficers, HashSet<Officer> assignedPrefects)
        {
            // 检查是否有更优秀的武将可以替换现有太守
            foreach (var city in _faction.Cities)
            {
                if (city.Prefect == null) continue;

                var currentPrefect = city.Prefect;
                var currentScore = GetPrefectScore(currentPrefect, city);

                // 寻找更优秀的候选人
                var betterCandidate = allOfficers
                    .Where(o => !assignedPrefects.Contains(o) && o != currentPrefect)
                    .Where(o => GetPrefectScore(o, city) > currentScore * 1.2f) // 必须明显更优秀才调动
                    .OrderByDescending(o => GetPrefectScore(o, city))
                    .FirstOrDefault();

                if (betterCandidate != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 发现更优人选：{betterCandidate.Name} (评分: {GetPrefectScore(betterCandidate, city):F1}) 可替换 {currentPrefect.Name} (评分: {currentScore:F1}) 管理 {city.Name}");
                    
                    // 执行替换
                    ExecuteAssignment(city, betterCandidate, "人事优化");
                    assignedPrefects.Add(betterCandidate);
                }
            }
        }

        /// <summary>
        /// 辅助评分：军事分
        /// </summary>
        private float CalculateMilitaryScore(Officer officer, float threatLevel)
        {
            // 原始能力分
            float rawScore = officer.Leadership * 2.0f + officer.War * 1.5f + officer.Intelligence * 0.8f;

            // 性格修正
            if (officer.Traits.Contains(Trait.Rash))
            {
                rawScore *= 1.2f; // 莽撞的适合前线，但要控制风险
            }
            if (officer.Traits.Contains(Trait.Cautious))
            {
                rawScore *= (threatLevel > 0.8f ? 1.3f : 0.9f); // 谨慎的在极高威胁时反而有优势
            }
            if (officer.HasTrait(Trait.Timid))
            {
                rawScore *= 0.6f; // 胆小的前线评分大打折扣
            }
            if (officer.Traits.Contains(Trait.Ambitious))
            {
                rawScore *= 1.1f; // 野心勃勃的喜欢前线建功
            }

            // 经验加成
            if (officer.HasExperience("SiegeDefense"))
            {
                rawScore *= 1.2f; // 有守城经验
            }
            if (officer.HasExperience("FieldBattle"))
            {
                rawScore *= 1.15f; // 有野战经验
            }

            // 年龄影响
            if (officer.Age > 60)
            {
                rawScore *= 0.9f; // 年老体衰
            }
            else if (officer.Age < 30)
            {
                rawScore *= 1.05f; // 年轻有为
            }

            // === 风险修正 ===
            float stability = CalculateStabilityScore(officer);

            // 最终得分 = 能力 * 稳定性
            // 这样，一个统率 100 但野心极高的人，得分可能会低于一个统率 70 但忠心耿耿的人
            return rawScore * stability;
        }

        /// <summary>
        /// 辅助评分：内政分
        /// </summary>
        private float CalculateDomesticScore(Officer officer, City city)
        {
            float rawScore = officer.Politics * 2.0f + officer.Intelligence * 1.0f + officer.Leadership * 0.5f;

            // 性格修正
            if (officer.Traits.Contains(Trait.Greedy))
            {
                rawScore *= 0.9f; // 贪婪的后方评分略微降低（可能会贪污，但还是比莽夫强）
            }
            if (officer.Traits.Contains(Trait.Scholar))
            {
                rawScore *= 1.3f; // 学者适合内政
            }
            if (officer.Traits.Contains(Trait.Pragmatic))
            {
                rawScore *= 1.1f; // 实用主义者适合管理
            }
            if (officer.Traits.Contains(Trait.Conservative))
            {
                rawScore *= 1.15f; // 保守的适合稳定发展
            }

            // 城市特性加成
            if (city.IsCapital && officer.Politics > 80)
            {
                rawScore *= 1.2f; // 高政治武将适合管理首都
            }
            if (city.IsPortCity && officer.Intelligence > 70)
            {
                rawScore *= 1.1f; // 高智力武将适合管理贸易城市
            }

            // 经验加成
            if (officer.HasExperience("CityManagement"))
            {
                rawScore *= 1.25f; // 有城市管理经验
            }
            if (officer.HasExperience("EconomicReform"))
            {
                rawScore *= 1.2f; // 有经济改革经验
            }

            // === 风险修正 ===
            // 内政太守如果独立，你会直接损失一个城的钱粮，风险同样很高
            return rawScore * CalculateStabilityScore(officer);
        }

        /// <summary>
        /// 获取太守综合评分
        /// </summary>
        private float GetPrefectScore(Officer officer, City city)
        {
            float threat = GetCityThreatLevel(city);
            
            if (threat > 0.5f)
            {
                return CalculateMilitaryScore(officer, threat);
            }
            else
            {
                return CalculateDomesticScore(officer, city);
            }
        }

        /// <summary>
        /// 获取城市威胁等级
        /// </summary>
        private float GetCityThreatLevel(City city)
        {
            float threat = 0.0f;

            // 基础威胁
            if (city.IsBorderCity) threat += 0.4f;
            if (city.HasRecentBattle) threat += 0.3f;

            // 周围敌军威胁
            var nearbyEnemies = GetNearbyEnemyCount(city);
            threat += Math.Min(nearbyEnemies * 0.1f, 0.4f);

            // 城市重要性
            if (city.IsCapital) threat += 0.2f; // 首都更重要，需要更好的防护

            return Math.Min(threat, 1.0f);
        }

        /// <summary>
        /// 获取附近敌军数量
        /// </summary>
        private int GetNearbyEnemyCount(City city)
        {
            // 简化实现，实际需要连接到游戏的军队系统
            if (city.HasRecentBattle) return 3;
            if (city.IsBorderCity) return 1;
            return 0;
        }

        /// <summary>
        /// 检查武将是否在调动中
        /// </summary>
        private bool IsInTransit(Officer officer)
        {
            return _activeTransfers.Any(t => t.Officer == officer && !t.IsCompleted);
        }

        /// <summary>
        /// 获取所有活跃的调动命令
        /// </summary>
        public List<TransferOrder> GetActiveTransfers()
        {
            return _activeTransfers.Where(t => !t.IsCompleted).ToList();
        }

        /// <summary>
        /// 获取武将绩效评级
        /// </summary>
        public float GetOfficerPerformance(Officer officer)
        {
            return _performanceRatings.ContainsKey(officer) ? _performanceRatings[officer] : 0.5f;
        }

        /// <summary>
        /// 更新武将绩效评级
        /// </summary>
        public void UpdateOfficerPerformance(Officer officer, float rating)
        {
            _performanceRatings[officer] = Math.Max(0f, Math.Min(1f, rating));
        }

        /// <summary>
        /// 生成人事报告
        /// </summary>
        public string GeneratePersonnelReport()
        {
            var report = $"=== {_faction.Name} 人事状况报告 ===\n\n";

            report += $"总武将数: {_faction.Officers.Count}\n";
            report += $"活跃武将: {_faction.Officers.Count(o => o.State == OfficerState.Active)}\n";
            report += $"在职太守: {_faction.Cities.Count(c => c.Prefect != null)}\n";
            report += $"调动中: {_activeTransfers.Count(t => !t.IsCompleted)}\n\n";

            // 忠诚度统计
            var loyalOfficers = _faction.Officers.Count(o => o.Loyalty >= 80);
            var riskyOfficers = _faction.Officers.Count(o => IsOfficerRisky(o));
            report += $"忠诚武将 (≥80): {loyalOfficers}\n";
            report += $"危险武将: {riskyOfficers}\n\n";

            report += "城市太守安排:\n";
            foreach (var city in _faction.Cities.OrderByDescending(c => GetCityThreatLevel(c)))
            {
                var threat = GetCityThreatLevel(c);
                var prefect = city.Prefect;
                
                report += $"  {city.Name} (威胁: {threat:F2}): ";
                if (prefect != null)
                {
                    var score = GetPrefectScore(prefect, city);
                    var stability = CalculateStabilityScore(prefect);
                    report += $"{prefect.Name} (评分: {score:F1}, 稳定性: {stability:F2})\n";
                }
                else
                {
                    report += "无太守\n";
                }
            }

            if (_activeTransfers.Any(t => !t.IsCompleted))
            {
                report += "\n进行中的调动:\n";
                foreach (var transfer in _activeTransfers.Where(t => !t.IsCompleted))
                {
                    report += $"  {transfer.Officer.Name} → {transfer.Destination.Name} ({transfer.EstimatedDays}天后到达)\n";
                }
            }

            // 危险武将警告
            if (riskyOfficers > 0)
            {
                report += "\n⚠ 危险武将警告:\n";
                foreach (var officer in _faction.Officers.Where(o => IsOfficerRisky(o)))
                {
                    report += $"  {officer.Name}: 忠诚{officer.Loyalty}, 野心{officer.Ambition}, 义理{officer.Righteousness}\n";
                }
            }

            return report;
        }

        /// <summary>
        /// 计算任命风险系数 (0.0 ~ 1.0)
        /// 1.0 代表绝对安全（如诸葛亮），0.1 代表极其危险（如吕布、魏延）
        /// </summary>
        private float CalculateStabilityScore(Officer officer)
        {
            // === 关系熔断机制 ===
            // 如果与君主有绝对忠诚关系（配偶、义兄弟），直接返回最高稳定性
            if (_faction.Ruler != null && officer.IsSoulBoundTo(_faction.Ruler))
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {officer.Name} 与君主 {_faction.Ruler.Name} 有绝对忠诚关系，稳定性最高");
                return 1.0f;
            }

            float stability = 1.0f;

            // 1. 基础忠诚度过滤 (Loyalty)
            // 忠诚度 < 90 开始产生微弱负面影响
            // 忠诚度 < 70 产生严重负面影响
            if (officer.Loyalty < 90)
            {
                // 例子：忠诚 60 -> 系数为 0.6
                stability *= (officer.Loyalty / 100f);
            }

            // 2. 野心惩罚 (Ambition)
            // 假设野心是 0-100。野心 > 70 的人如果不忠诚，极度危险
            if (officer.Ambition > 70)
            {
                // 野心越高，扣分越多。
                // 比如野心 100，这里扣除 (100-70)*0.01 = 0.3，系数变为 0.7
                float ambitionPenalty = (officer.Ambition - 70) * 0.01f;
                stability -= ambitionPenalty;
            }

            // 3. 义理修正 (Righteousness/Duty)
            // 义理低的人（< 40），容易被敌方登庸或独立
            if (officer.Righteousness < 40)
            {
                stability *= 0.7f; // 直接打 7 折
            }

            // 4. 关系网络影响 (Enhanced Relationship System)
            if (_faction.Ruler != null)
            {
                var relationWithRuler = officer.GetRelationWith(_faction.Ruler);
                
                switch (relationWithRuler)
                {
                    case RelationType.Hated:
                        stability *= 0.1f; // 厌恶君主，风险极高
                        System.Diagnostics.Debug.WriteLine($"[PersonnelManager] {officer.Name} 厌恶君主 {_faction.Ruler.Name}，风险极高");
                        break;
                        
                    case RelationType.Friend:
                        stability *= 1.3f; // 友好关系加成
                        break;
                        
                    case RelationType.ParentChild:
                        stability *= 1.5f; // 亲子关系高度信任
                        break;
                        
                    case RelationType.SwornBrother:
                    case RelationType.Spouse:
                        // 这些情况已经在熔断机制中处理了
                        stability *= 1.8f;
                        break;
                }
            }

            // 5. 关系网络稳定性 - 检查与其他重要武将的关系
            float relationshipBonus = CalculateRelationshipNetworkBonus(officer);
            stability *= (1.0f + relationshipBonus);

            // 6. 性格特质影响
            if (officer.Traits.Contains(Trait.Loyal))
            {
                stability *= 1.2f; // 忠诚特质加成
            }
            if (officer.Traits.Contains(Trait.Rebellious))
            {
                stability *= 0.5f; // 叛逆特质严重扣分
            }

            // 7. 年龄和健康影响稳定性
            if (officer.Age > 65)
            {
                stability *= 1.1f; // 年老的武将更稳重
            }
            if (officer.Health < 30)
            {
                stability *= 0.8f; // 健康不佳可能影响判断
            }

            return Math.Max(stability, 0.05f); // 也就是最低也会给个 0.05 分
        }

        /// <summary>
        /// 计算关系网络加成
        /// </summary>
        private float CalculateRelationshipNetworkBonus(Officer officer)
        {
            float bonus = 0.0f;
            int friendCount = 0;
            int enemyCount = 0;

            // 统计与势力内其他武将的关系
            foreach (var otherOfficer in _faction.Officers)
            {
                if (otherOfficer.ID == officer.ID) continue;

                var relation = officer.GetRelationWith(otherOfficer);
                switch (relation)
                {
                    case RelationType.Friend:
                    case RelationType.ParentChild:
                    case RelationType.SwornBrother:
                    case RelationType.Spouse:
                        friendCount++;
                        break;
                        
                    case RelationType.Hated:
                        enemyCount++;
                        break;
                }
            }

            // 朋友多的武将更稳定
            bonus += Math.Min(friendCount * 0.05f, 0.2f);
            
            // 敌人多的武将不稳定
            bonus -= Math.Min(enemyCount * 0.1f, 0.3f);

            return bonus;
        }

        /// <summary>
        /// 清洗阶段：检查现有太守，解除危险分子职务
        /// </summary>
        private void PurgeRiskyPrefects()
        {
            System.Diagnostics.Debug.WriteLine("[PersonnelManager] 开始清洗阶段，检查现有太守忠诚度");

            var dismissedPrefects = new List<Officer>();

            foreach (var city in _faction.Cities)
            {
                var prefect = city.Prefect;
                if (prefect == null) continue;

                // 检查是否为危险分子
                bool isRisky = IsOfficerRisky(prefect);

                if (isRisky)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 发现危险太守：{prefect.Name} (忠诚:{prefect.Loyalty}, 野心:{prefect.Ambition}, 义理:{prefect.Righteousness})");
                    
                    // 撤职！
                    city.RemovePrefect();
                    dismissedPrefects.Add(prefect);

                    // 可选：将该武将移动回首都（软禁）
                    var capital = _faction.Cities.FirstOrDefault(c => c.IsCapital);
                    if (capital != null && prefect.Location != capital)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 将 {prefect.Name} 召回首都软禁");
                        IssueTransferOrder(prefect, capital, "忠诚度问题，召回首都");
                    }
                }
            }

            if (dismissedPrefects.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonnelManager] 清洗完成，共解除 {dismissedPrefects.Count} 名危险太守职务");
            }
        }

        /// <summary>
        /// 判断武将是否为危险分子
        /// </summary>
        private bool IsOfficerRisky(Officer officer)
        {
            // 忠诚度跌破 80 且 野心高，或者忠诚度跌破 60 (无论野心)
            bool lowLoyaltyHighAmbition = (officer.Loyalty < 80 && officer.Ambition > 70);
            bool veryLowLoyalty = (officer.Loyalty < 60);
            bool lowRighteousness = (officer.Righteousness < 30);
            bool dislikesRuler = (_faction.Ruler != null && officer.Dislikes(_faction.Ruler));
            bool hasRebelliousTrait = officer.Traits.Contains(Trait.Rebellious);

            return lowLoyaltyHighAmbition || veryLowLoyalty || lowRighteousness || dislikesRuler || hasRebelliousTrait;
        }

        /// <summary>
        /// 检查武将是否适合重要职位
        /// </summary>
        private bool IsSuitableForImportantPosition(Officer officer, City city)
        {
            // 重要城市需要更高的忠诚度标准
            int requiredLoyalty = 70; // 基础要求
            
            if (city.IsCapital) requiredLoyalty = 85; // 首都要求更高
            if (city.IsBorderCity) requiredLoyalty = 75; // 边境城市要求较高
            
            // 稳定性评分必须达到一定标准
            float stabilityScore = CalculateStabilityScore(officer);
            float requiredStability = city.IsCapital ? 0.8f : 0.6f;

            return officer.Loyalty >= requiredLoyalty && stabilityScore >= requiredStability;
        }
    }
}