using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Comprehensive fix recommendation based on diagnostic analysis
    /// </summary>
    public class FixRecommendation
    {
        public FixStrategy Strategy { get; set; }
        public List<TypeRegistrationFix> TypeFixes { get; set; } = new List<TypeRegistrationFix>();
        public List<ConfigurationFix> ConfigurationFixes { get; set; } = new List<ConfigurationFix>();
        public List<CodeFix> CodeFixes { get; set; } = new List<CodeFix>();
        public EstimatedImpact Impact { get; set; }
    }

    /// <summary>
    /// Strategy for fixing identified issues
    /// </summary>
    public enum FixStrategy
    {
        AddMissingRegistrations,
        ResolveNamespaceConflicts,
        UpdatePolymorphicRegistrations,
        RepairJsonStructure,
        ComprehensiveOverhaul
    }

    /// <summary>
    /// Fix for type registration issues
    /// </summary>
    public class TypeRegistrationFix
    {
        public string TypeName { get; set; }
        public string Namespace { get; set; }
        public string RequiredAttribute { get; set; }
        public string CodeSnippet { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Fix for configuration issues
    /// </summary>
    public class ConfigurationFix
    {
        public string ConfigurationName { get; set; }
        public string CurrentValue { get; set; }
        public string RecommendedValue { get; set; }
        public string Justification { get; set; }
    }

    /// <summary>
    /// Fix requiring code changes
    /// </summary>
    public class CodeFix
    {
        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public string CurrentCode { get; set; }
        public string RecommendedCode { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Estimated impact of applying fixes
    /// </summary>
    public class EstimatedImpact
    {
        public ImpactLevel PerformanceImpact { get; set; }
        public ImpactLevel CompatibilityImpact { get; set; }
        public ImpactLevel MaintenanceImpact { get; set; }
        public string RiskAssessment { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
    }

    /// <summary>
    /// Levels of impact
    /// </summary>
    public enum ImpactLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }
}