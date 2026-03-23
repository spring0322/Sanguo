using System;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Interface for implementing fixes based on diagnostic results
    /// Validates Requirements 5.1, 5.2, 5.3, 5.4, 5.5
    /// </summary>
    public interface IFixImplementationEngine
    {
        /// <summary>
        /// Generate a comprehensive fix plan based on diagnostic results
        /// Requirement 5.1: Address the identified root cause completely
        /// </summary>
        /// <param name="diagnosticResult">The diagnostic analysis result</param>
        /// <returns>A detailed fix plan with implementation steps</returns>
        Task<FixImplementationPlan> GenerateFixPlan(DiagnosticResult diagnosticResult);

        /// <summary>
        /// Automatically update GameJsonContext with required type registrations
        /// Requirement 5.2: Ensure all EventEffectKind-related types are properly registered
        /// </summary>
        /// <param name="fixPlan">The fix plan to implement</param>
        /// <returns>Result of the GameJsonContext update operation</returns>
        Task<FixImplementationResult> UpdateGameJsonContext(FixImplementationPlan fixPlan);

        /// <summary>
        /// Verify that the implemented fix resolves the original issue
        /// Requirement 5.3: Maintain backward compatibility with existing save data
        /// </summary>
        /// <param name="originalIssue">The original diagnostic result that triggered the fix</param>
        /// <param name="implementationResult">The result of the fix implementation</param>
        /// <returns>Verification result indicating success or remaining issues</returns>
        Task<FixVerificationResult> VerifyFixEffectiveness(DiagnosticResult originalIssue, FixImplementationResult implementationResult);

        /// <summary>
        /// Ensure backward compatibility with existing save data
        /// Requirement 5.4: Include comprehensive unit tests to prevent regression
        /// </summary>
        /// <param name="fixPlan">The fix plan to validate for compatibility</param>
        /// <returns>Compatibility assessment result</returns>
        Task<CompatibilityAssessment> EnsureBackwardCompatibility(FixImplementationPlan fixPlan);

        /// <summary>
        /// Generate comprehensive tests for the implemented fix
        /// Requirement 5.5: Pass all existing and new deserialization tests
        /// </summary>
        /// <param name="fixPlan">The fix plan to generate tests for</param>
        /// <returns>Generated test suite for regression prevention</returns>
        Task<TestSuiteGeneration> GenerateRegressionTests(FixImplementationPlan fixPlan);

        /// <summary>
        /// Execute the complete fix implementation workflow
        /// Integrates all fix implementation steps into a single operation
        /// </summary>
        /// <param name="diagnosticResult">The diagnostic result to fix</param>
        /// <returns>Complete fix implementation result</returns>
        Task<ComprehensiveFixResult> ImplementCompleteFix(DiagnosticResult diagnosticResult);
    }
}