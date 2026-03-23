using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Comprehensive plan for implementing fixes based on diagnostic results
    /// </summary>
    public class FixImplementationPlan
    {
        public string PlanId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DiagnosticResult SourceDiagnostic { get; set; }
        public FixStrategy Strategy { get; set; }
        public List<ImplementationStep> Steps { get; set; } = new List<ImplementationStep>();
        public EstimatedImpact EstimatedImpact { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Individual step in the fix implementation plan
    /// </summary>
    public class ImplementationStep
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ImplementationStepType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public bool IsRequired { get; set; } = true;
        public TimeSpan EstimatedDuration { get; set; }
    }

    /// <summary>
    /// Types of implementation steps
    /// </summary>
    public enum ImplementationStepType
    {
        AddTypeRegistration,
        UpdateConfiguration,
        ModifyCode,
        RunTests,
        ValidateCompatibility,
        BackupData,
        RestoreData
    }

    /// <summary>
    /// Result of fix implementation operations
    /// </summary>
    public class FixImplementationResult
    {
        public bool IsSuccessful { get; set; }
        public string PlanId { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public List<StepResult> StepResults { get; set; } = new List<StepResult>();
        public List<string> ModifiedFiles { get; set; } = new List<string>();
        public List<string> AddedTypeRegistrations { get; set; } = new List<string>();
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of an individual implementation step
    /// </summary>
    public class StepResult
    {
        public int StepOrder { get; set; }
        public string StepName { get; set; }
        public bool IsSuccessful { get; set; }
        public TimeSpan Duration { get; set; }
        public string Output { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of fix verification operations
    /// </summary>
    public class FixVerificationResult
    {
        public bool IsFixEffective { get; set; }
        public bool OriginalIssueResolved { get; set; }
        public bool NoNewIssuesIntroduced { get; set; }
        public List<DiagnosticIssue> RemainingIssues { get; set; } = new List<DiagnosticIssue>();
        public List<DiagnosticIssue> NewIssues { get; set; } = new List<DiagnosticIssue>();
        public TestResult VerificationTests { get; set; }
        public FixVerificationPerformanceMetrics PerformanceImpact { get; set; }
        public string VerificationSummary { get; set; }
    }

    /// <summary>
    /// Performance metrics for fix verification
    /// </summary>
    public class FixVerificationPerformanceMetrics
    {
        public TimeSpan DeserializationTime { get; set; }
        public long MemoryUsage { get; set; }
        public int SuccessfulDeserializations { get; set; }
        public int FailedDeserializations { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Assessment of backward compatibility
    /// </summary>
    public class CompatibilityAssessment
    {
        public bool IsBackwardCompatible { get; set; }
        public List<CompatibilityIssue> Issues { get; set; } = new List<CompatibilityIssue>();
        public List<string> TestedVersions { get; set; } = new List<string>();
        public List<string> TestedDataFormats { get; set; } = new List<string>();
        public CompatibilityLevel CompatibilityLevel { get; set; }
        public string AssessmentSummary { get; set; }
    }

    /// <summary>
    /// Individual compatibility issue
    /// </summary>
    public class CompatibilityIssue
    {
        public string Description { get; set; }
        public CompatibilityIssueSeverity Severity { get; set; }
        public string AffectedVersion { get; set; }
        public string AffectedDataFormat { get; set; }
        public string RecommendedAction { get; set; }
        public bool HasWorkaround { get; set; }
    }

    /// <summary>
    /// Severity levels for compatibility issues
    /// </summary>
    public enum CompatibilityIssueSeverity
    {
        Info,
        Warning,
        Error,
        Breaking
    }

    /// <summary>
    /// Levels of backward compatibility
    /// </summary>
    public enum CompatibilityLevel
    {
        FullyCompatible,
        MostlyCompatible,
        PartiallyCompatible,
        IncompatibleWithWorkarounds,
        Incompatible
    }

    /// <summary>
    /// Result of test suite generation
    /// </summary>
    public class TestSuiteGeneration
    {
        public bool IsSuccessful { get; set; }
        public List<GeneratedTest> GeneratedTests { get; set; } = new List<GeneratedTest>();
        public List<string> TestFiles { get; set; } = new List<string>();
        public TestCoverage Coverage { get; set; }
        public string TestFramework { get; set; }
        public Dictionary<string, object> TestConfiguration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Individual generated test
    /// </summary>
    public class GeneratedTest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TestType Type { get; set; }
        public string TestCode { get; set; }
        public List<string> TestData { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public TestPriority Priority { get; set; }
    }

    /// <summary>
    /// Types of generated tests
    /// </summary>
    public enum TestType
    {
        UnitTest,
        IntegrationTest,
        PropertyBasedTest,
        RegressionTest,
        PerformanceTest,
        CompatibilityTest
    }

    /// <summary>
    /// Priority levels for tests
    /// </summary>
    public enum TestPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Test coverage information
    /// </summary>
    public class TestCoverage
    {
        public double OverallCoverage { get; set; }
        public Dictionary<string, double> ComponentCoverage { get; set; } = new Dictionary<string, double>();
        public List<string> UncoveredAreas { get; set; } = new List<string>();
        public List<string> CriticalPathsCovered { get; set; } = new List<string>();
    }

    /// <summary>
    /// Comprehensive result of the complete fix implementation
    /// </summary>
    public class ComprehensiveFixResult
    {
        public bool IsSuccessful { get; set; }
        public FixImplementationPlan Plan { get; set; }
        public FixImplementationResult Implementation { get; set; }
        public FixVerificationResult Verification { get; set; }
        public CompatibilityAssessment Compatibility { get; set; }
        public TestSuiteGeneration GeneratedTests { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string Summary { get; set; }
        public List<string> NextSteps { get; set; } = new List<string>();
    }
}