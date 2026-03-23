using System;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics
{
    /// <summary>
    /// Configuration and initialization setup for the STJ diagnostic system
    /// Implements Requirements 1.1, 2.1, 3.1, 4.1 - Component integration and initialization
    /// </summary>
    public static class DiagnosticServiceConfiguration
    {
        /// <summary>
        /// Create a configured diagnostic system without dependency injection
        /// </summary>
        /// <param name="configuration">Optional configuration settings</param>
        /// <returns>Configured integrated diagnostic workflow</returns>
        public static IntegratedDiagnosticWorkflow CreateDiagnosticSystem(DiagnosticConfiguration configuration = null)
        {
            configuration ??= new DiagnosticConfiguration();
            
            // Create instances of all components
            var rootCauseAnalyzer = new RootCauseAnalyzer();
            var typeRegistrationValidator = new TypeRegistrationValidator();
            var deserializationTester = new DeserializationTester();
            var fallbackHandler = new FallbackHandler();
            var fileService = new AsyncFileService();
            var serializationService = new AsyncSerializationService(new WorldOfTheThreeKingdoms.Serialization.GameJsonContext());
            var diagnosticSystem = new DiagnosticSystem(
                rootCauseAnalyzer, 
                typeRegistrationValidator, 
                deserializationTester, 
                fallbackHandler,
                fileService,
                serializationService);
            
            var fixImplementationEngine = new FixImplementationEngine(
                rootCauseAnalyzer,
                typeRegistrationValidator,
                deserializationTester,
                fallbackHandler);
            
            // Create the integrated workflow
            return new IntegratedDiagnosticWorkflow(
                rootCauseAnalyzer,
                typeRegistrationValidator,
                deserializationTester,
                fallbackHandler,
                fixImplementationEngine,
                diagnosticSystem);
        }

        /// <summary>
        /// Initialize the diagnostic system and perform startup validation
        /// </summary>
        /// <param name="workflow">The diagnostic workflow to initialize</param>
        /// <returns>Initialization result</returns>
        public static async System.Threading.Tasks.Task<InitializationResult> InitializeDiagnosticSystem(
            IntegratedDiagnosticWorkflow workflow)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var healthReport = await workflow.InitializeAndValidateSystem();
                var endTime = DateTime.UtcNow;
                
                return new InitializationResult
                {
                    IsSuccessful = healthReport.IsHealthy,
                    HealthReport = healthReport,
                    Duration = endTime - startTime,
                    InitializedAt = endTime
                };
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                
                return new InitializationResult
                {
                    IsSuccessful = false,
                    Error = ex,
                    Duration = endTime - startTime,
                    InitializedAt = endTime
                };
            }
        }
    }

    /// <summary>
    /// Configuration settings for the diagnostic system
    /// </summary>
    public class DiagnosticConfiguration
    {
        /// <summary>
        /// Whether to enable automatic fix implementation
        /// </summary>
        public bool EnableAutoFix { get; set; } = false;

        /// <summary>
        /// Maximum number of errors to keep in history
        /// </summary>
        public int MaxErrorHistorySize { get; set; } = 1000;

        /// <summary>
        /// Whether to enable detailed performance logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Interval for automatic health checks (in minutes)
        /// </summary>
        public int HealthCheckIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to enable type usage tracking
        /// </summary>
        public bool EnableTypeUsageTracking { get; set; } = true;

        /// <summary>
        /// Threshold for slow operation detection (in milliseconds)
        /// </summary>
        public long SlowOperationThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Threshold for high failure rate detection (as percentage)
        /// </summary>
        public double HighFailureRateThreshold { get; set; } = 10.0;

        /// <summary>
        /// Whether to enable comprehensive error context capture
        /// </summary>
        public bool EnableComprehensiveErrorContext { get; set; } = true;
    }

    /// <summary>
    /// Result of diagnostic system initialization
    /// </summary>
    public class InitializationResult
    {
        public bool IsSuccessful { get; set; }
        public Models.SystemHealthReport HealthReport { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime InitializedAt { get; set; }
        public Exception Error { get; set; }
    }
}