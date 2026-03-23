using GameObjects;
using GameManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查器实现
    /// </summary>
    public class DataIntegrityChecker : IDataIntegrityChecker
    {
        private readonly List<IIntegrityRule> _rules = new List<IIntegrityRule>();
        private readonly object _rulesLock = new object();

        public DataIntegrityChecker()
        {
            // 注册默认规则
            RegisterDefaultRules();
        }

        /// <summary>
        /// 注册默认的完整性检查规则
        /// </summary>
        private void RegisterDefaultRules()
        {
            AddRule(new FactionLeaderRule());
            AddRule(new PersonIdealTendencyRule());
            AddRule(new ArchitectureBelongedFactionRule());
            AddRule(new TroopBelongedLegionRule());
        }

        public void AddRule(IIntegrityRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            lock (_rulesLock)
            {
                // 检查是否已存在同名规则
                if (_rules.Any(r => r.RuleName == rule.RuleName))
                {
                    Debug.WriteLine($"[DataIntegrityChecker] 规则 '{rule.RuleName}' 已存在，将被替换");
                    RemoveRule(rule.RuleName);
                }

                _rules.Add(rule);
                Debug.WriteLine($"[DataIntegrityChecker] 添加规则: {rule.RuleName}");
            }
        }

        public void RemoveRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return;

            lock (_rulesLock)
            {
                var rule = _rules.FirstOrDefault(r => r.RuleName == ruleName);
                if (rule != null)
                {
                    _rules.Remove(rule);
                    Debug.WriteLine($"[DataIntegrityChecker] 移除规则: {ruleName}");
                }
            }
        }

        public IReadOnlyList<IIntegrityRule> GetRules()
        {
            lock (_rulesLock)
            {
                return _rules.ToList().AsReadOnly();
            }
        }

        public async Task<IntegrityReport> CheckAsync(GameObject gameObject)
        {
            var report = new IntegrityReport();
            
            if (gameObject == null)
            {
                report.AddIssue(new IntegrityIssue
                {
                    ObjectType = "Unknown",
                    ObjectId = -1,
                    PropertyName = "Object",
                    IssueType = "NullReference",
                    Description = "游戏对象为null",
                    SuggestedFix = "确保对象已正确初始化",
                    Severity = IssueSeverity.Critical
                });
                return report;
            }

            report.TotalObjectsChecked = 1;

            // 获取当前启用的规则
            var enabledRules = GetRules().Where(r => r.IsEnabled && r.AppliesTo(gameObject)).ToList();

            Debug.WriteLine($"[DataIntegrityChecker] 检查对象 {gameObject.GetType().Name}[{gameObject.ID}]，应用 {enabledRules.Count} 个规则");

            foreach (var rule in enabledRules)
            {
                try
                {
                    var result = await rule.ValidateAsync(gameObject);
                    report.AddResult(result);

                    if (!result.IsValid)
                    {
                        Debug.WriteLine($"[DataIntegrityChecker] 规则 '{rule.RuleName}' 发现问题: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataIntegrityChecker] 规则 '{rule.RuleName}' 执行异常: {ex.Message}");
                    
                    report.AddIssue(new IntegrityIssue
                    {
                        ObjectType = gameObject.GetType().Name,
                        ObjectId = gameObject.ID,
                        PropertyName = "Rule",
                        IssueType = "RuleException",
                        Description = $"规则 '{rule.RuleName}' 执行异常: {ex.Message}",
                        SuggestedFix = "检查规则实现或对象状态",
                        Severity = IssueSeverity.Warning
                    });
                }
            }

            return report;
        }

        public async Task<IntegrityReport> CheckAllAsync()
        {
            var report = new IntegrityReport();
            
            try
            {
                // 检查当前场景中的所有游戏对象
                if (Session.Current?.Scenario == null)
                {
                    report.AddIssue(new IntegrityIssue
                    {
                        ObjectType = "Session",
                        ObjectId = -1,
                        PropertyName = "Scenario",
                        IssueType = "NullReference",
                        Description = "当前场景为null，无法执行全面检查",
                        SuggestedFix = "确保游戏场景已正确加载",
                        Severity = IssueSeverity.Critical
                    });
                    return report;
                }

                var scenario = Session.Current.Scenario;
                var allObjects = new List<GameObject>();

                // 收集所有游戏对象
                if (scenario.Persons != null)
                    allObjects.AddRange(scenario.Persons.GetList().Cast<GameObject>());
                
                if (scenario.Architectures != null)
                    allObjects.AddRange(scenario.Architectures.GetList().Cast<GameObject>());
                
                if (scenario.Factions != null)
                    allObjects.AddRange(scenario.Factions.GetList().Cast<GameObject>());
                
                if (scenario.Troops != null)
                    allObjects.AddRange(scenario.Troops.GetList().Cast<GameObject>());

                if (scenario.Legions != null)
                    allObjects.AddRange(scenario.Legions.GetList().Cast<GameObject>());

                Debug.WriteLine($"[DataIntegrityChecker] 开始全面检查，共 {allObjects.Count} 个对象");

                report.TotalObjectsChecked = allObjects.Count;

                // 检查每个对象
                foreach (var obj in allObjects)
                {
                    try
                    {
                        var objReport = await CheckAsync(obj);
                        
                        // 合并报告
                        foreach (var issue in objReport.Issues)
                        {
                            report.AddIssue(issue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DataIntegrityChecker] 检查对象 {obj?.GetType().Name}[{obj?.ID}] 时发生异常: {ex.Message}");
                        
                        report.AddIssue(new IntegrityIssue
                        {
                            ObjectType = obj?.GetType().Name ?? "Unknown",
                            ObjectId = obj?.ID ?? -1,
                            PropertyName = "Object",
                            IssueType = "CheckException",
                            Description = $"检查对象时发生异常: {ex.Message}",
                            SuggestedFix = "检查对象状态或完整性检查器实现",
                            Severity = IssueSeverity.Warning
                        });
                    }
                }

                Debug.WriteLine($"[DataIntegrityChecker] 全面检查完成，发现 {report.IssuesFound} 个问题");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataIntegrityChecker] 全面检查发生异常: {ex.Message}");
                
                report.AddIssue(new IntegrityIssue
                {
                    ObjectType = "System",
                    ObjectId = -1,
                    PropertyName = "CheckAll",
                    IssueType = "SystemException",
                    Description = $"全面检查发生异常: {ex.Message}",
                    SuggestedFix = "检查系统状态和数据完整性检查器配置",
                    Severity = IssueSeverity.Critical
                });
            }

            return report;
        }

        public async Task<bool> ValidateRelationshipsAsync(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            var report = await CheckAsync(gameObject);
            
            // 只关注关系相关的问题
            var relationshipIssues = report.Issues.Where(i => 
                i.IssueType == "NullReference" || 
                i.IssueType == "MissingRelation" ||
                i.IssueType == "InvalidRelation").ToList();

            return relationshipIssues.Count == 0;
        }
    }
}