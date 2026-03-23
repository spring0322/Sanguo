using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Comprehensive diagnostic and monitoring capabilities for STJ deserialization.
    /// Validates Requirements 6.1, 6.2, 6.3, 6.4, 6.5
    /// </summary>
    public interface IDiagnosticSystem
    {
        /// <summary>
        /// Validates all critical type registrations at system startup
        /// Requirement 6.1: Validate all critical type registrations at startup
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Validation result for startup checks</returns>
        Task<SystemHealthReport> ValidateStartupRegistrations(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Logs performance metrics and success rates during deserialization
        /// Requirement 6.2: Log performance metrics and success rates
        /// </summary>
        /// <param name="operationType">The type of operation being measured</param>
        /// <param name="duration">The duration of the operation</param>
        /// <param name="success">Whether the operation was successful</param>
        void LogPerformanceMetrics(string operationType, long duration, bool success);
        
        /// <summary>
        /// Captures detailed context when errors are detected
        /// Requirement 6.3: Capture detailed context including JSON data and stack traces
        /// </summary>
        /// <param name="errorContext">The error context to capture</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task CaptureErrorContextAsync(ErrorContext errorContext, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tracks which types are frequently deserialized
        /// Requirement 6.4: Track which types are frequently deserialized
        /// </summary>
        /// <param name="typeName">The name of the type being tracked</param>
        void TrackTypeUsage(string typeName);
        
        /// <summary>
        /// Generates actionable insights for preventing future issues
        /// Requirement 6.5: Provide actionable insights for preventing future issues
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Report with actionable insights and recommendations</returns>
        Task<DiagnosticInsightsReport> GenerateActionableInsights(CancellationToken cancellationToken = default);
    }
}