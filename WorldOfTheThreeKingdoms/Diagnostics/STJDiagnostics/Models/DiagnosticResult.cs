using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Comprehensive result of diagnostic analysis
    /// </summary>
    public class DiagnosticResult
    {
        public bool IsSuccessful { get; set; }
        public List<DiagnosticIssue> Issues { get; set; } = new List<DiagnosticIssue>();
        public TypeRegistrationReport TypeRegistrations { get; set; }
        public NamespaceConflictReport NamespaceConflicts { get; set; }
        public StructuralMismatchReport StructuralMismatches { get; set; }
        public FixRecommendation RecommendedFix { get; set; }
    }

    /// <summary>
    /// Individual diagnostic issue found during analysis
    /// </summary>
    public class DiagnosticIssue
    {
        public IssueSeverity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string Description { get; set; }
        public string DetailedAnalysis { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Severity levels for diagnostic issues
    /// </summary>
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Categories of diagnostic issues
    /// </summary>
    public enum IssueCategory
    {
        TypeRegistration,
        NamespaceConflict,
        JsonStructure,
        PolymorphicTypes,
        Configuration,
        Performance
    }
}