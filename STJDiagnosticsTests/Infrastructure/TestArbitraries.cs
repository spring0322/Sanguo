using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;
using GameObjects;

namespace STJDiagnosticsTests.Infrastructure
{
    /// <summary>
    /// Custom arbitraries for property-based testing of STJ diagnostics
    /// </summary>
    public static class TestArbitraries
    {
        /// <summary>
        /// Generates arbitrary Type instances for testing - focused on game types
        /// </summary>
        public static Arbitrary<Type> ArbitraryGameType()
        {
            var gameTypes = new[]
            {
                typeof(string),
                typeof(int),
                typeof(Dictionary<int, string>),
                typeof(List<string>),
                typeof(object)
            };

            return Gen.Elements(gameTypes).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary Type instances that have deserialization errors
        /// </summary>
        public static Arbitrary<Type> ArbitraryTypeWithDeserializationError()
        {
            var problematicTypes = new[]
            {
                typeof(Dictionary<int, string>),
                typeof(Dictionary<string, object>),
                typeof(List<Dictionary<int, string>>),
                typeof(object)
            };

            return Gen.Elements(problematicTypes).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary JSON strings for testing
        /// </summary>
        public static Arbitrary<string> ArbitraryJsonString()
        {
            var validJsonGen = Gen.OneOf(
                Gen.Constant("{}"),
                Gen.Constant("[]"),
                Gen.Constant("{\"key\":\"value\"}"),
                Gen.Constant("{\"id\":1,\"name\":\"test\"}"),
                Gen.Constant("{\"items\":[1,2,3]}"),
                Gen.Constant("{\"EventEffectKinds\":{\"1\":{\"ID\":1,\"Name\":\"TestEffect\"}}}"),
                Gen.Constant("{\"EventEffectKinds\":{}}"),
                Gen.Constant("{\"EventEffectKinds\":null}")
            );

            var invalidJsonGen = Gen.OneOf(
                Gen.Constant("{"),
                Gen.Constant("invalid"),
                Gen.Constant("{\"key\":}"),
                Gen.Constant("null"),
                Gen.Constant("")
            );

            return Gen.OneOf(validJsonGen, invalidJsonGen).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary JsonSerializerOptions for testing
        /// </summary>
        public static Arbitrary<JsonSerializerOptions> ArbitraryJsonSerializerOptions()
        {
            return Gen.Fresh(() => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary diagnostic issues for testing
        /// </summary>
        public static Arbitrary<DiagnosticIssue> ArbitraryDiagnosticIssue()
        {
            return (from severity in Arb.Generate<IssueSeverity>()
                    from category in Arb.Generate<IssueCategory>()
                    from description in Arb.Default.String().Generator
                    from analysis in Arb.Default.String().Generator
                    select new DiagnosticIssue
                    {
                        Severity = severity,
                        Category = category,
                        Description = description ?? "Test issue",
                        DetailedAnalysis = analysis ?? "Test analysis",
                        RecommendedActions = new List<string> { "Test action" }
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary validation results for testing
        /// </summary>
        public static Arbitrary<ValidationResult> ArbitraryValidationResult()
        {
            return (from isValid in Arb.Default.Bool().Generator
                    from errorCount in Gen.Choose(0, 5)
                    from warningCount in Gen.Choose(0, 3)
                    select new ValidationResult
                    {
                        IsValid = isValid,
                        Errors = GenerateValidationErrors(errorCount),
                        Warnings = GenerateValidationWarnings(warningCount)
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary EventEffectKind-related types
        /// </summary>
        public static Arbitrary<Type> ArbitraryEventEffectKindType()
        {
            var eventEffectKindTypes = new[]
            {
                typeof(string),
                typeof(object),
                typeof(Dictionary<int, string>),
                typeof(List<object>)
            };

            return Gen.Elements(eventEffectKindTypes).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary DiagnosticResult instances for testing
        /// </summary>
        public static Arbitrary<DiagnosticResult> ArbitraryDiagnosticResult()
        {
            return (from isSuccessful in Arb.Default.Bool().Generator
                    from issueCount in Gen.Choose(0, 5)
                    select new DiagnosticResult
                    {
                        IsSuccessful = isSuccessful,
                        Issues = GenerateDiagnosticIssues(issueCount),
                        TypeRegistrations = GenerateTypeRegistrationReport(),
                        NamespaceConflicts = GenerateNamespaceConflictReport(),
                        StructuralMismatches = GenerateStructuralMismatchReport(),
                        RecommendedFix = GenerateFixRecommendation()
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary DiagnosticResult instances with guaranteed issues for testing
        /// </summary>
        public static Arbitrary<DiagnosticResult> ArbitraryDiagnosticResultWithIssues()
        {
            return (from issueCount in Gen.Choose(1, 5)
                    select new DiagnosticResult
                    {
                        IsSuccessful = false,
                        Issues = GenerateDiagnosticIssues(issueCount),
                        TypeRegistrations = GenerateTypeRegistrationReportWithMissing(),
                        NamespaceConflicts = GenerateNamespaceConflictReportWithConflicts(),
                        StructuralMismatches = GenerateStructuralMismatchReport(),
                        RecommendedFix = GenerateFixRecommendation()
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary FixImplementationPlan instances for testing
        /// </summary>
        public static Arbitrary<FixImplementationPlan> ArbitraryFixPlan()
        {
            return (from stepCount in Gen.Choose(1, 5)
                    from strategy in Arb.Generate<FixStrategy>()
                    select new FixImplementationPlan
                    {
                        Strategy = strategy,
                        Steps = GenerateImplementationSteps(stepCount),
                        EstimatedImpact = new EstimatedImpact
                        {
                            PerformanceImpact = ImpactLevel.Low,
                            CompatibilityImpact = ImpactLevel.Low,
                            MaintenanceImpact = ImpactLevel.Medium
                        },
                        SourceDiagnostic = new DiagnosticResult
                        {
                            IsSuccessful = false,
                            Issues = GenerateDiagnosticIssues(2)
                        }
                    }).ToArbitrary();
        }

        private static List<ValidationError> GenerateValidationErrors(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new ValidationError
                {
                    Code = $"ERR{i:D3}",
                    Message = $"Test error {i}",
                    Details = $"Test error details {i}",
                    RecommendedAction = $"Fix error {i}"
                })
                .ToList();
        }

        private static List<ValidationWarning> GenerateValidationWarnings(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new ValidationWarning
                {
                    Code = $"WARN{i:D3}",
                    Message = $"Test warning {i}",
                    Details = $"Test warning details {i}",
                    Recommendation = $"Consider fixing warning {i}"
                })
                .ToList();
        }

        private static List<DiagnosticIssue> GenerateDiagnosticIssues(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new DiagnosticIssue
                {
                    Severity = (IssueSeverity)(i % 4), // Cycle through severity levels
                    Category = (IssueCategory)(i % 6), // Cycle through categories
                    Description = $"Test diagnostic issue {i}",
                    DetailedAnalysis = $"Detailed analysis for issue {i}",
                    RecommendedActions = new List<string> { $"Action {i}.1", $"Action {i}.2" }
                })
                .ToList();
        }

        private static TypeRegistrationReport GenerateTypeRegistrationReport()
        {
            return new TypeRegistrationReport
            {
                TypeRegistrations = new Dictionary<Type, RegistrationStatus>
                {
                    [typeof(string)] = RegistrationStatus.Registered,
                    [typeof(int)] = RegistrationStatus.Registered,
                    [typeof(object)] = RegistrationStatus.Missing
                },
                MissingRegistrations = new List<Type> { typeof(object) },
                CircularDependencies = new List<Type>(),
                DependencyChain = new Dictionary<Type, List<Type>>
                {
                    [typeof(string)] = new List<Type> { typeof(object) },
                    [typeof(int)] = new List<Type>()
                }
            };
        }

        private static TypeRegistrationReport GenerateTypeRegistrationReportWithMissing()
        {
            return new TypeRegistrationReport
            {
                TypeRegistrations = new Dictionary<Type, RegistrationStatus>
                {
                    [typeof(string)] = RegistrationStatus.Registered,
                    [typeof(Dictionary<int, object>)] = RegistrationStatus.Missing
                },
                MissingRegistrations = new List<Type> { typeof(Dictionary<int, object>) },
                CircularDependencies = new List<Type>(),
                DependencyChain = new Dictionary<Type, List<Type>>
                {
                    [typeof(Dictionary<int, object>)] = new List<Type> { typeof(int), typeof(object) }
                }
            };
        }

        private static NamespaceConflictReport GenerateNamespaceConflictReport()
        {
            return new NamespaceConflictReport
            {
                HasConflicts = false,
                Conflicts = new List<NamespaceConflict>()
            };
        }

        private static NamespaceConflictReport GenerateNamespaceConflictReportWithConflicts()
        {
            return new NamespaceConflictReport
            {
                HasConflicts = true,
                Conflicts = new List<NamespaceConflict>
                {
                    new NamespaceConflict
                    {
                        TypeName = "EventEffectKind",
                        ConflictingNamespaces = new List<string>
                        {
                            "GameObjects.TroopDetail.EventEffect",
                            "GameObjects.ArchitectureDetail.EventEffect"
                        }
                    }
                }
            };
        }

        private static StructuralMismatchReport GenerateStructuralMismatchReport()
        {
            return new StructuralMismatchReport
            {
                HasMismatches = false,
                JsonAnalysis = new JsonStructureAnalysis { IsValid = true },
                TypeAnalysis = new TypeStructureAnalysis 
                { 
                    Properties = new Dictionary<string, string> { ["TestProperty"] = "string" },
                    RequiredProperties = new List<string> { "TestProperty" },
                    OptionalProperties = new List<string>()
                }
            };
        }

        private static FixRecommendation GenerateFixRecommendation()
        {
            return new FixRecommendation
            {
                Strategy = FixStrategy.AddMissingRegistrations,
                TypeFixes = new List<TypeRegistrationFix>
                {
                    new TypeRegistrationFix
                    {
                        TypeName = "TestType",
                        Namespace = "Test.Namespace",
                        RequiredAttribute = "[JsonSerializable(typeof(TestType))]",
                        Priority = 1
                    }
                },
                ConfigurationFixes = new List<ConfigurationFix>(),
                CodeFixes = new List<CodeFix>(),
                Impact = new EstimatedImpact
                {
                    PerformanceImpact = ImpactLevel.Low,
                    CompatibilityImpact = ImpactLevel.Low,
                    MaintenanceImpact = ImpactLevel.Medium
                }
            };
        }

        private static List<ImplementationStep> GenerateImplementationSteps(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new ImplementationStep
                {
                    Order = i + 1,
                    Name = $"Test Step {i + 1}",
                    Description = $"Description for test step {i + 1}",
                    Type = (ImplementationStepType)(i % 7), // Cycle through step types
                    Parameters = new Dictionary<string, object>
                    {
                        ["TestParam"] = $"Value{i}"
                    },
                    IsRequired = i < count / 2, // First half are required
                    EstimatedDuration = TimeSpan.FromMinutes(i + 1)
                })
                .ToList();
        }
    }
}