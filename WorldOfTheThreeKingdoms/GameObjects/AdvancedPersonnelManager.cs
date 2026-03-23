using System;
using System.Collections.Generic;
using System.Linq;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;

namespace GameObjects
{
    /// <summary>
    /// 高级人事管理器 - 扩展 Faction 的智能功能
    /// 包含：季度评估、智能轮岗、人才培养、风险预测
    /// </summary>
    public class AdvancedPersonnelManager
    {
        private Faction _faction;
        private Dictionary<Person, List<float>> _performanceHistory;
        private int _currentSeason;
        private Random _random;

        public AdvancedPersonnelManager(Faction faction)
        {
            _faction = faction;
            _performanceHistory = new Dictionary<Person, List<float>>();
            _currentSeason = 0;
            _random = new Random();
        }

        /// <summary>
        /// 季节性人事评估 - 建议每季度（1月/4月/7月/10月 1日）调用
        /// </summary>
        public void SeasonalAssessment()
        {
            _currentSeason++;
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvancedPersonnelManager] {_faction.Name} 开始第{_currentSeason}季度人事评估");
            }

            // 1. 评估所有太守的季度表现
            EvaluateQuarterlyPerformance();

            // 2. 预测潜在风险 (包含清洗不忠诚太守)
            PredictPotentialRisks();

            // 3. 智能轮岗 (防止太守长期霸占)
            ImplementRotationSystem();

            // 4. 人才培养
            DevelopTalent();
            
            // 注意：常规人事调动由 Faction.RunPersonnel_V81 接管，此处不再重复执行
        }

        /// <summary>
        /// 评估季度表现
        /// </summary>
        private void EvaluateQuarterlyPerformance()
        {
            foreach (Architecture arch in _faction.Architectures)
            {
                if (arch.Mayor == null) continue;

                float performance = CalculateQuarterlyPerformance(arch, arch.Mayor);
                
                // 记录表现历史
                if (!_performanceHistory.ContainsKey(arch.Mayor))
                    _performanceHistory[arch.Mayor] = new List<float>();
                
                _performanceHistory[arch.Mayor].Add(performance);
                
                // 只保留最近8个季度的记录
                if (_performanceHistory[arch.Mayor].Count > 8)
                    _performanceHistory[arch.Mayor].RemoveAt(0);

                // 更新 Person 对象的评价 (如果有对应属性，目前假设存储在 manager 内部)
                // arch.Mayor.InternalPerformance = performance; // 假设字段
                
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[APM] {arch.Mayor.Name} 在 {arch.Name} 的季度表现: {performance:F2}");
                }
            }
        }

        /// <summary>
        /// 计算季度表现
        /// </summary>
        private float CalculateQuarterlyPerformance(Architecture arch, Person mayor)
        {
            float performance = 0.5f; // 基础表现

            // 城市发展指标 (假设 Architecture 有这些属性)
            // performance += arch.Prosperity * 0.0001f; // 假设数值很大
            // 这里简化处理：根据治安和民心
            performance += arch.Domination / 2000f; // 假设上限1000？ 需确认
            performance += arch.Domination / 2000f; 

            // 人口增长 (此处无法获取增量，仅作静态评估)
            performance += (arch.Population / 100000f) * 0.1f;

            // 太守能力匹配度
            float threat = 0f; 
            // 简单计算威胁度: 
            if (arch.HasHostileTroopsInView()) threat = 1.0f;
            else if (arch.FrontLine) threat = 0.6f;

            if (threat > 0.6f)
            {
                // 前线城市看军事能力
                float militaryFit = (mayor.Command + mayor.Strength) / 200f;
                performance += militaryFit * 0.2f;
            }
            else
            {
                // 后方城市看政治能力
                float politicalFit = (mayor.Politics + mayor.Intelligence) / 200f;
                performance += politicalFit * 0.2f;
            }

            return Math.Max(0f, Math.Min(1f, performance));
        }

        /// <summary>
        /// 预测潜在风险 & 清洗
        /// </summary>
        private void PredictPotentialRisks()
        {
            List<Person> toDismiss = new List<Person>();

            foreach (Person p in _faction.Persons)
            {
                // 仅评估由 Faction 管理的、非俘虏的正常武将
                if (p.Status != PersonStatus.Normal || p.IsCaptive) continue;

                float risk = PredictOfficerRisk(p);
                
                if (risk > 0.8f) // 极高风险
                {
                    if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APM] ⚠️ 高风险警告: {p.Name} (风险值:{risk:F2})");
                    }

                    // 如果是太守，建议撤换
                    if (p.LocationArchitecture != null && p.LocationArchitecture.Mayor == p)
                    {
                        toDismiss.Add(p);
                    }
                }
            }

            // 执行清洗
            foreach (Person p in toDismiss)
            {
                if (p.LocationArchitecture != null)
                {
                     if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APM] 🛡️ 应急清洗: 撤换危险太守 {p.Name} ({p.LocationArchitecture.Name})");
                    }
                    // 撤销太守职务
                    p.LocationArchitecture.AppointMayor(null);
                }
            }
        }

        /// <summary>
        /// 预测武将风险
        /// </summary>
        private float PredictOfficerRisk(Person p)
        {
            float risk = 0f;

            // 基础风险
            if (p.Loyalty < 80) risk += 0.3f;
            if (p.Loyalty < 60) risk += 0.3f; // 叠加
            if (p.Ambition > 80) risk += 0.2f;
            if (p.PersonalLoyalty < 50) risk += 0.2f; // 义理/个人忠诚

            // 关系风险 (假设有 Ruler)
            if (_faction.Leader != null)
            {
                if (p.HatedPersons.Contains(_faction.Leader.ID)) risk += 0.5f;
                if (p.ClosePersons.Contains(_faction.Leader.ID)) risk -= 0.5f;
            }

            return Math.Max(0f, Math.Min(1f, risk));
        }

        /// <summary>
        /// 智能轮岗系统
        /// </summary>
        public void ImplementRotationSystem()
        {
            // 找出任职时间过长的太守 (由于缺乏精确任职时间记录，使用表现历史长度作为近似)
            foreach (Architecture arch in _faction.Architectures)
            {
                if (arch.Mayor == null) continue;
                
                var mayor = arch.Mayor;
                int tenure = _performanceHistory.ContainsKey(mayor) ? _performanceHistory[mayor].Count : 0;

                // 假设积累了6次季度评估 (1.5年) 且表现不佳 (<0.4) 或者 超过12次 (3年) 无论表现如何都考虑轮岗
                if (tenure >= 12 || (tenure >= 6 && GetAveragePerformance(mayor) < 0.4f))
                {
                    // 标记为需要轮岗 (简单处理：直接解除职务，让 V8.1 在下次运行时重新分配)
                    // 这样可以制造流动性
                     if (SectionAIHelper.EnableDebugOutput)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APM] 🔄 智能轮岗: 解除 {mayor.Name} 的太守职务 (任期评估:{tenure})");
                    }
                    arch.AppointMayor(null); 
                }
            }
        }
        
        private float GetAveragePerformance(Person p)
        {
             if (!_performanceHistory.ContainsKey(p) || _performanceHistory[p].Count == 0) return 0.5f;
             return _performanceHistory[p].Average();
        }

        /// <summary>
        /// 人才培养系统
        /// </summary>
        public void DevelopTalent()
        {
            // 挑选潜力股：年轻 (Age < 30) 且有某项属性 > 70
            var youngTalents = _faction.Persons.Cast<Person>()
                .Where(p => p.Status == PersonStatus.Normal && p.Age < 30)
                .Where(p => p.Command > 70 || p.Strength > 70 || p.Intelligence > 70 || p.Politics > 70)
                .Take(5)
                .ToList();

            foreach (Person p in youngTalents)
            {
                // 模拟培养：极小概率增加属性 (避免破坏平衡)
                bool improved = false;
                string stat = "";

                if (_random.NextDouble() < 0.1) // 10% 概率成长
                {
                    int roll = _random.Next(4);
                    switch (roll)
                    {
                        case 0: p.Command = Math.Min(100, p.Command + 1); stat = "统率"; break;
                        case 1: p.Strength = Math.Min(100, p.Strength + 1); stat = "武力"; break;
                        case 2: p.Intelligence = Math.Min(100, p.Intelligence + 1); stat = "智力"; break;
                        case 3: p.Politics = Math.Min(100, p.Politics + 1); stat = "政治"; break;
                    }
                    improved = true;
                }

                if (improved && SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[APM] 📚 人才培养: {p.Name} 的 {stat} 提升了 1 点");
                }
            }
        }
    }
}