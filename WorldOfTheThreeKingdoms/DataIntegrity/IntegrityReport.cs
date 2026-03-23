using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查报告
    /// </summary>
    public class IntegrityReport
    {
        public List<IntegrityIssue> Issues { get; set; } = new List<IntegrityIssue>();
        public DateTime CheckTime { get; set; } = DateTime.Now;
        public int TotalObjectsChecked { get; set; }
        public int IssuesFound => Issues.Count;
        public bool HasCriticalIssues => Issues.Any(i => i.Severity == IssueSeverity.Critical);
        public bool HasWarnings => Issues.Any(i => i.Severity == IssueSeverity.Warning);

        /// <summary>
        /// 添加检查结果
        /// </summary>
        /// <param name="result">验证结果</param>
        public void AddResult(ValidationResult result)
        {
            if (!result.IsValid)
            {
                Issues.Add(new IntegrityIssue
                {
                    ObjectType = result.ObjectType,
                    ObjectId = result.ObjectId,
                    PropertyName = result.PropertyName,
                    IssueType = result.IssueType,
                    Description = result.Message,
                    SuggestedFix = result.SuggestedFix,
                    Severity = result.Severity
                });
            }
        }

        /// <summary>
        /// 添加问题
        /// </summary>
        /// <param name="issue">完整性问题</param>
        public void AddIssue(IntegrityIssue issue)
        {
            Issues.Add(issue);
        }

        /// <summary>
        /// 获取按严重程度分组的问题
        /// </summary>
        /// <returns>按严重程度分组的问题字典</returns>
        public Dictionary<IssueSeverity, List<IntegrityIssue>> GetIssuesBySeverity()
        {
            return Issues.GroupBy(i => i.Severity)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// 获取按对象类型分组的问题
        /// </summary>
        /// <returns>按对象类型分组的问题字典</returns>
        public Dictionary<string, List<IntegrityIssue>> GetIssuesByObjectType()
        {
            return Issues.GroupBy(i => i.ObjectType)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// 生成报告摘要
        /// </summary>
        /// <returns>报告摘要字符串</returns>
        public string GetSummary()
        {
            var summary = $"数据完整性检查报告 - {CheckTime:yyyy-MM-dd HH:mm:ss}\n";
            summary += $"检查对象总数: {TotalObjectsChecked}\n";
            summary += $"发现问题总数: {IssuesFound}\n";

            var issuesBySeverity = GetIssuesBySeverity();
            foreach (var severity in Enum.GetValues<IssueSeverity>())
            {
                if (issuesBySeverity.ContainsKey(severity))
                {
                    summary += $"{GetSeverityDisplayName(severity)}: {issuesBySeverity[severity].Count}\n";
                }
            }

            return summary;
        }

        private string GetSeverityDisplayName(IssueSeverity severity)
        {
            return severity switch
            {
                IssueSeverity.Critical => "严重问题",
                IssueSeverity.Warning => "警告",
                IssueSeverity.Info => "信息",
                _ => severity.ToString()
            };
        }
    }

    /// <summary>
    /// 数据完整性问题
    /// </summary>
    public class IntegrityIssue
    {
        public string ObjectType { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SuggestedFix { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; } = IssueSeverity.Warning;
        public DateTime DetectedTime { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{Severity}] {ObjectType}[{ObjectId}].{PropertyName}: {Description}";
        }
    }

    /// <summary>
    /// 问题严重程度
    /// </summary>
    public enum IssueSeverity
    {
        Info,       // 信息
        Warning,    // 警告
        Critical    // 严重
    }
}