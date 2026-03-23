using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Comprehensive error context for diagnostic analysis
    /// </summary>
    public class ErrorContext
    {
        public Exception Exception { get; set; }
        public string JsonData { get; set; }
        public Type TargetType { get; set; }
        public DateTime Timestamp { get; set; }
        public string OperationContext { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
        public string StackTrace { get; set; }
        public EnvironmentInfo Environment { get; set; }
    }

    /// <summary>
    /// Environment information at the time of error
    /// </summary>
    public class EnvironmentInfo
    {
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public long MemoryUsage { get; set; }
        public string RuntimeVersion { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Registration fix plan for addressing type registration issues
    /// </summary>
    public class RegistrationFixPlan
    {
        public List<TypeRegistrationFix> RequiredFixes { get; set; } = new List<TypeRegistrationFix>();
        public List<TypeRegistrationFix> RecommendedFixes { get; set; } = new List<TypeRegistrationFix>();
        public EstimatedImpact Impact { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public string Implementation { get; set; }
    }

    /// <summary>
    /// Diagnostic insights report with actionable recommendations
    /// </summary>
    public class DiagnosticInsightsReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<Insight> Insights { get; set; } = new List<Insight>();
        public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
        public TrendAnalysis Trends { get; set; }
        public RiskAssessment Risks { get; set; }
    }

    /// <summary>
    /// Individual diagnostic insight
    /// </summary>
    public class Insight
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public InsightType Type { get; set; }
        public InsightSeverity Severity { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Actionable recommendation
    /// </summary>
    public class Recommendation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public RecommendationType Type { get; set; }
        public int Priority { get; set; }
        public string ActionPlan { get; set; }
        public EstimatedImpact Impact { get; set; }
    }

    /// <summary>
    /// Trend analysis over time
    /// </summary>
    public class TrendAnalysis
    {
        public Dictionary<string, double> ErrorRateTrends { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> PerformanceTrends { get; set; } = new Dictionary<string, double>();
        public List<string> EmergingIssues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Risk assessment for the system
    /// </summary>
    public class RiskAssessment
    {
        public RiskLevel OverallRisk { get; set; }
        public List<Risk> IdentifiedRisks { get; set; } = new List<Risk>();
        public List<string> MitigationStrategies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Individual risk item
    /// </summary>
    public class Risk
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public RiskLevel Level { get; set; }
        public double Probability { get; set; }
        public string Impact { get; set; }
    }

    /// <summary>
    /// Types of insights
    /// </summary>
    public enum InsightType
    {
        Performance,
        Reliability,
        Security,
        Maintenance,
        Usage
    }

    /// <summary>
    /// Severity of insights
    /// </summary>
    public enum InsightSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Types of recommendations
    /// </summary>
    public enum RecommendationType
    {
        Immediate,
        ShortTerm,
        LongTerm,
        Preventive,
        Optimization
    }

    /// <summary>
    /// Risk levels
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}