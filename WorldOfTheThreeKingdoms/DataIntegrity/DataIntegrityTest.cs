using GameObjects;
using GameManager;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查器测试类
    /// 用于验证检查器的基本功能
    /// </summary>
    public static class DataIntegrityTest
    {
        /// <summary>
        /// 运行基本的数据完整性检查测试
        /// </summary>
        public static async Task RunBasicTests()
        {
            Debug.WriteLine("[DataIntegrityTest] 开始基本功能测试");

            var checker = new DataIntegrityChecker();
            
            // 测试1: 检查null对象
            Debug.WriteLine("[DataIntegrityTest] 测试1: 检查null对象");
            var nullReport = await checker.CheckAsync(null);
            Debug.WriteLine($"Null对象检查结果: {nullReport.IssuesFound}个问题");
            
            // 测试2: 检查规则注册
            Debug.WriteLine("[DataIntegrityTest] 测试2: 检查规则注册");
            var rules = checker.GetRules();
            Debug.WriteLine($"已注册规则数量: {rules.Count}");
            foreach (var rule in rules)
            {
                Debug.WriteLine($"  - {rule.RuleName}: {rule.Description}");
            }

            // 测试3: 如果有当前场景，进行实际检查
            if (Session.Current?.Scenario != null)
            {
                Debug.WriteLine("[DataIntegrityTest] 测试3: 检查当前场景数据");
                
                // 检查前几个势力
                if (Session.Current.Scenario.Factions != null && Session.Current.Scenario.Factions.Count > 0)
                {
                    int checkCount = Math.Min(3, Session.Current.Scenario.Factions.Count);
                    for (int i = 0; i < checkCount; i++)
                    {
                        var faction = Session.Current.Scenario.Factions[i] as Faction;
                        if (faction != null)
                        {
                            var factionReport = await checker.CheckAsync(faction);
                            Debug.WriteLine($"势力 {faction.Name}[{faction.ID}] 检查结果: {factionReport.IssuesFound}个问题");
                            
                            foreach (var issue in factionReport.Issues)
                            {
                                Debug.WriteLine($"  - [{issue.Severity}] {issue.PropertyName}: {issue.Description}");
                            }
                        }
                    }
                }

                // 检查前几个人物
                if (Session.Current.Scenario.Persons != null && Session.Current.Scenario.Persons.Count > 0)
                {
                    int checkCount = Math.Min(3, Session.Current.Scenario.Persons.Count);
                    for (int i = 0; i < checkCount; i++)
                    {
                        var person = Session.Current.Scenario.Persons[i] as Person;
                        if (person != null)
                        {
                            var personReport = await checker.CheckAsync(person);
                            Debug.WriteLine($"人物 {person.Name}[{person.ID}] 检查结果: {personReport.IssuesFound}个问题");
                            
                            foreach (var issue in personReport.Issues)
                            {
                                Debug.WriteLine($"  - [{issue.Severity}] {issue.PropertyName}: {issue.Description}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("[DataIntegrityTest] 当前没有加载场景，跳过实际数据检查");
            }

            Debug.WriteLine("[DataIntegrityTest] 基本功能测试完成");
        }

        /// <summary>
        /// 运行全面的数据完整性检查
        /// </summary>
        public static async Task RunFullCheck()
        {
            Debug.WriteLine("[DataIntegrityTest] 开始全面数据完整性检查");

            var checker = new DataIntegrityChecker();
            var report = await checker.CheckAllAsync();

            Debug.WriteLine($"[DataIntegrityTest] 全面检查完成");
            Debug.WriteLine($"检查对象总数: {report.TotalObjectsChecked}");
            Debug.WriteLine($"发现问题总数: {report.IssuesFound}");
            Debug.WriteLine($"严重问题: {(report.HasCriticalIssues ? "是" : "否")}");
            Debug.WriteLine($"警告问题: {(report.HasWarnings ? "是" : "否")}");

            // 显示问题摘要
            var issuesBySeverity = report.GetIssuesBySeverity();
            foreach (var severity in Enum.GetValues<IssueSeverity>())
            {
                if (issuesBySeverity.ContainsKey(severity))
                {
                    Debug.WriteLine($"{severity}: {issuesBySeverity[severity].Count}个");
                }
            }

            // 显示前10个问题的详细信息
            Debug.WriteLine("[DataIntegrityTest] 前10个问题详情:");
            int displayCount = Math.Min(10, report.Issues.Count);
            for (int i = 0; i < displayCount; i++)
            {
                var issue = report.Issues[i];
                Debug.WriteLine($"  {i + 1}. [{issue.Severity}] {issue.ObjectType}[{issue.ObjectId}].{issue.PropertyName}");
                Debug.WriteLine($"     问题: {issue.Description}");
                Debug.WriteLine($"     建议: {issue.SuggestedFix}");
            }

            Debug.WriteLine("[DataIntegrityTest] 全面检查测试完成");
        }

        /// <summary>
        /// 创建测试用的有问题的对象
        /// </summary>
        public static async Task TestWithProblematicObjects()
        {
            Debug.WriteLine("[DataIntegrityTest] 测试有问题的对象");

            var checker = new DataIntegrityChecker();

            // 创建一个有问题的势力对象
            var problematicFaction = new Faction();
            problematicFaction.ID = 9999;
            problematicFaction.Name = "测试势力";
            // 故意设置LeaderID但不设置Leader对象
            problematicFaction.LeaderID = 1;
            // problematicFaction.Leader = null; // 这会导致问题

            var factionReport = await checker.CheckAsync(problematicFaction);
            Debug.WriteLine($"有问题的势力检查结果: {factionReport.IssuesFound}个问题");
            foreach (var issue in factionReport.Issues)
            {
                Debug.WriteLine($"  - [{issue.Severity}] {issue.PropertyName}: {issue.Description}");
                Debug.WriteLine($"    建议修复: {issue.SuggestedFix}");
            }

            Debug.WriteLine("[DataIntegrityTest] 有问题对象测试完成");
        }
    }
}