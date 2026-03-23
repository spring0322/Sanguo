using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// Integrated diagnostic workflow that connects all diagnostic components
    /// Implements Requirements 1.1, 2.1, 3.1, 4.1 - Complete diagnostic to fix workflow
    /// </summary>
    public class IntegratedDiagnosticWorkflow
    {
        private readonly IRootCauseAnalyzer _rootCauseAnalyzer;
        private readonly ITypeRegistrationValidator _typeRegistrationValidator;
        private readonly IDeserializationTester _deserializationTester;
        private readonly IFallbackHandler _fallbackHandler;
        private readonly IFixImplementationEngine _fixImplementationEngine;
        private readonly IDiagnosticSystem _diagnosticSystem;

        public IntegratedDiagnosticWorkflow(
            IRootCauseAnalyzer rootCauseAnalyzer,
            ITypeRegistrationValidator typeRegistrationValidator,
            IDeserializationTester deserializationTester,
            IFallbackHandler fallbackHandler,
            IFixImplementationEngine fixImplementationEngine,
            IDiagnosticSystem diagnosticSystem)
        {
            _rootCauseAnalyzer = rootCauseAnalyzer ?? throw new ArgumentNullException(nameof(rootCauseAnalyzer));
            _typeRegistrationValidator = typeRegistrationValidator ?? throw new ArgumentNullException(nameof(typeRegistrationValidator));
            _deserializationTester = deserializationTester ?? throw new ArgumentNullException(nameof(deserializationTester));
            _fallbackHandler = fallbackHandler ?? throw new ArgumentNullException(nameof(fallbackHandler));
            _fixImplementationEngine = fixImplementationEngine ?? throw new ArgumentNullException(nameof(fixImplementationEngine));
            _diagnosticSystem = diagnosticSystem ?? throw new ArgumentNullException(nameof(diagnosticSystem));
        }

        /// <summary>
        /// Execute the complete diagnostic and fix workflow
        /// Requirement 1.1: Root cause analysis system
        /// Requirement 2.1: Type registration validation
        /// Requirement 3.1: Isolated deserialization testing
        /// Requirement 4.1: Comprehensive error handling
        /// </summary>
        /// <param name="targetType">The type that is experiencing deserialization issues</param>
        /// <param name="jsonData">The JSON data that failed to deserialize</param>
        /// <param name="options">The JsonSerializerOptions being used</param>
        /// <param name="autoFix">Whether to automatically apply fixes if possible</param>
        /// <returns>Complete workflow result including diagnosis and fix implementation</returns>
        public async Task<WorkflowResult> ExecuteCompleteWorkflow(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, 
            string jsonData, 
            System.Text.Json.JsonSerializerOptions options,
            bool autoFix = false)
        {
            var workflowResult = new WorkflowResult
            {
                StartTime = DateTime.UtcNow,
                TargetType = targetType,
                JsonData = jsonData,
                AutoFixEnabled = autoFix
            };

            try
            {
                // Step 1: Perform comprehensive root cause analysis
                _diagnosticSystem.LogPerformanceMetrics("WorkflowStart", 0, true);
                
                workflowResult.DiagnosticResult = await _rootCauseAnalyzer.AnalyzeDeserializationFailure(
                    targetType, jsonData, options);

                _diagnosticSystem.TrackTypeUsage(targetType.FullName);

                // Step 2: If issues are found and auto-fix is enabled, attempt to fix them
                if (!workflowResult.DiagnosticResult.IsSuccessful && autoFix)
                {
                    workflowResult.FixResult = await _fixImplementationEngine.ImplementCompleteFix(
                        workflowResult.DiagnosticResult);
                }

                // Step 3: Test the fix if one was applied
                if (workflowResult.FixResult?.IsSuccessful == true)
                {
                    workflowResult.PostFixValidation = await ValidateFixEffectiveness(
                        targetType, jsonData, options, workflowResult.FixResult);
                }

                // Step 4: If no fix was applied or fix failed, provide fallback handling
                if (workflowResult.FixResult?.IsSuccessful != true)
                {
                    try
                    {
                        workflowResult.FallbackResult = await _fallbackHandler.HandleDeserializationFailure<object>(
                            jsonData, options, new Exception("Deserialization failed"));
                    }
                    catch (Exception fallbackEx)
                    {
                        await _diagnosticSystem.CaptureErrorContextAsync(new ErrorContext
                        {
                            Exception = fallbackEx,
                            TargetType = targetType,
                            JsonData = jsonData,
                            OperationContext = "FallbackHandling",
                            Timestamp = DateTime.UtcNow
                        }).ConfigureAwait(false);
                    }
                }

                // Step 5: Generate actionable insights
                workflowResult.Insights = await _diagnosticSystem.GenerateActionableInsights();

                workflowResult.IsSuccessful = DetermineOverallSuccess(workflowResult);
                workflowResult.EndTime = DateTime.UtcNow;
                workflowResult.Duration = workflowResult.EndTime - workflowResult.StartTime;

                _diagnosticSystem.LogPerformanceMetrics(
                    "CompleteWorkflow", 
                    (long)workflowResult.Duration.TotalMilliseconds, 
                    workflowResult.IsSuccessful);

                return workflowResult;
            }
            catch (Exception ex)
            {
                workflowResult.IsSuccessful = false;
                workflowResult.EndTime = DateTime.UtcNow;
                workflowResult.Duration = workflowResult.EndTime - workflowResult.StartTime;
                workflowResult.WorkflowError = ex;

                await _diagnosticSystem.CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    TargetType = targetType,
                    JsonData = jsonData,
                    OperationContext = "IntegratedWorkflow",
                    Timestamp = DateTime.UtcNow
                }).ConfigureAwait(false);

                _diagnosticSystem.LogPerformanceMetrics(
                    "CompleteWorkflow", 
                    (long)workflowResult.Duration.TotalMilliseconds, 
                    false);

                return workflowResult;
            }
        }

        /// <summary>
        /// Initialize the diagnostic system and validate startup health
        /// </summary>
        /// <returns>System health report</returns>
        public async Task<SystemHealthReport> InitializeAndValidateSystem()
        {
            try
            {
                var healthReport = await _diagnosticSystem.ValidateStartupRegistrations();
                
                _diagnosticSystem.LogPerformanceMetrics(
                    "SystemInitialization", 
                    100, // Placeholder duration
                    healthReport.IsHealthy);

                return healthReport;
            }
            catch (Exception ex)
            {
                await _diagnosticSystem.CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    OperationContext = "SystemInitialization",
                    Timestamp = DateTime.UtcNow
                }).ConfigureAwait(false);

                _diagnosticSystem.LogPerformanceMetrics("SystemInitialization", 100, false);
                
                throw;
            }
        }

        /// <summary>
        /// Perform a quick diagnostic check for a specific type
        /// </summary>
        /// <param name="targetType">The type to check</param>
        /// <returns>Quick diagnostic result</returns>
        public async Task<QuickDiagnosticResult> PerformQuickDiagnostic([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.Interfaces)] Type targetType)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Quick validation of type registration
                var typeValidation = _typeRegistrationValidator.ValidateDependentTypes(targetType);
                
                // Quick deserialization test
                var testResult = await _deserializationTester.TestEventEffectKindTableDeserialization("{}");
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                var result = new QuickDiagnosticResult
                {
                    TargetType = targetType,
                    IsHealthy = typeValidation.IsValid && testResult.IsSuccessful,
                    TypeValidation = typeValidation,
                    TestResult = testResult,
                    Duration = duration,
                    Timestamp = endTime
                };

                _diagnosticSystem.LogPerformanceMetrics(
                    "QuickDiagnostic", 
                    (long)duration.TotalMilliseconds, 
                    result.IsHealthy);

                _diagnosticSystem.TrackTypeUsage(targetType.FullName);

                return result;
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                await _diagnosticSystem.CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    TargetType = targetType,
                    OperationContext = "QuickDiagnostic",
                    Timestamp = DateTime.UtcNow
                }).ConfigureAwait(false);

                _diagnosticSystem.LogPerformanceMetrics(
                    "QuickDiagnostic", 
                    (long)duration.TotalMilliseconds, 
                    false);

                return new QuickDiagnosticResult
                {
                    TargetType = targetType,
                    IsHealthy = false,
                    Error = ex,
                    Duration = duration,
                    Timestamp = endTime
                };
            }
        }

        /// <summary>
        /// Get comprehensive system status
        /// </summary>
        /// <returns>System status report</returns>
        public async Task<SystemStatusReport> GetSystemStatus()
        {
            try
            {
                var healthReport = await _diagnosticSystem.ValidateStartupRegistrations();
                var insights = await _diagnosticSystem.GenerateActionableInsights();

                return new SystemStatusReport
                {
                    Health = healthReport,
                    Insights = insights,
                    GeneratedAt = DateTime.UtcNow,
                    IsOperational = healthReport.IsHealthy
                };
            }
            catch (Exception ex)
            {
                await _diagnosticSystem.CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    OperationContext = "GetSystemStatus",
                    Timestamp = DateTime.UtcNow
                }).ConfigureAwait(false);

                return new SystemStatusReport
                {
                    IsOperational = false,
                    Error = ex,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        #region Private Helper Methods

        private async Task<PostFixValidationResult> ValidateFixEffectiveness(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, 
            string jsonData, 
            System.Text.Json.JsonSerializerOptions options,
            ComprehensiveFixResult fixResult)
        {
            try
            {
                // Re-run the original diagnostic to see if issues are resolved
                var postFixDiagnostic = await _rootCauseAnalyzer.AnalyzeDeserializationFailure(
                    targetType, jsonData, options);

                // Test deserialization with the fixed configuration
                var postFixTest = await _deserializationTester.TestEventEffectKindTableDeserialization(jsonData);

                return new PostFixValidationResult
                {
                    IsEffective = postFixDiagnostic.IsSuccessful && postFixTest.IsSuccessful,
                    PostFixDiagnostic = postFixDiagnostic,
                    PostFixTest = postFixTest,
                    OriginalFixResult = fixResult
                };
            }
            catch (Exception ex)
            {
                return new PostFixValidationResult
                {
                    IsEffective = false,
                    ValidationError = ex,
                    OriginalFixResult = fixResult
                };
            }
        }

        private bool DetermineOverallSuccess(WorkflowResult workflowResult)
        {
            // Success criteria:
            // 1. No workflow errors
            // 2. Either diagnostic shows no issues, or fix was successful, or fallback worked
            
            if (workflowResult.WorkflowError != null)
                return false;

            if (workflowResult.DiagnosticResult?.IsSuccessful == true)
                return true;

            if (workflowResult.FixResult?.IsSuccessful == true && 
                workflowResult.PostFixValidation?.IsEffective == true)
                return true;

            if (workflowResult.FallbackResult != null)
                return true;

            return false;
        }

        #endregion
    }

    #region Result Models

    /// <summary>
    /// Complete result of the integrated diagnostic workflow
    /// </summary>
    public class WorkflowResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Type TargetType { get; set; }
        public string JsonData { get; set; }
        public bool AutoFixEnabled { get; set; }
        public bool IsSuccessful { get; set; }
        
        public DiagnosticResult DiagnosticResult { get; set; }
        public ComprehensiveFixResult FixResult { get; set; }
        public PostFixValidationResult PostFixValidation { get; set; }
        public object FallbackResult { get; set; }
        public DiagnosticInsightsReport Insights { get; set; }
        public Exception WorkflowError { get; set; }
    }

    /// <summary>
    /// Result of post-fix validation
    /// </summary>
    public class PostFixValidationResult
    {
        public bool IsEffective { get; set; }
        public DiagnosticResult PostFixDiagnostic { get; set; }
        public TestResult PostFixTest { get; set; }
        public ComprehensiveFixResult OriginalFixResult { get; set; }
        public Exception ValidationError { get; set; }
    }

    /// <summary>
    /// Result of quick diagnostic check
    /// </summary>
    public class QuickDiagnosticResult
    {
        public Type TargetType { get; set; }
        public bool IsHealthy { get; set; }
        public ValidationResult TypeValidation { get; set; }
        public TestResult TestResult { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Comprehensive system status report
    /// </summary>
    public class SystemStatusReport
    {
        public bool IsOperational { get; set; }
        public SystemHealthReport Health { get; set; }
        public DiagnosticInsightsReport Insights { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Exception Error { get; set; }
    }

    #endregion
}