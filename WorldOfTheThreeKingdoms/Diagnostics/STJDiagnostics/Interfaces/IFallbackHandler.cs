using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Graceful error recovery and system stability maintenance.
    /// Validates Requirements 4.1, 4.2, 4.3, 4.4, 4.5
    /// </summary>
    public interface IFallbackHandler
    {
        /// <summary>
        /// Handles deserialization failures with graceful recovery mechanisms
        /// Requirement 4.1: Provide graceful error recovery
        /// Requirement 4.5: Maintain system stability and continue operation
        /// </summary>
        /// <typeparam name="T">The target type for deserialization</typeparam>
        /// <param name="jsonData">The JSON data that failed to deserialize</param>
        /// <param name="primaryOptions">The primary JsonSerializerOptions that failed</param>
        /// <param name="originalException">The original exception that occurred</param>
        /// <returns>Recovered object instance or fallback value</returns>
        Task<T> HandleDeserializationFailure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string jsonData, 
            JsonSerializerOptions primaryOptions,
            Exception originalException);
        
        /// <summary>
        /// Attempts to repair corrupted JSON data
        /// Requirement 4.3: Attempt data repair or use default values
        /// </summary>
        /// <param name="jsonData">The potentially corrupted JSON data</param>
        /// <param name="targetType">The target type for deserialization</param>
        /// <returns>True if repair was successful, false otherwise</returns>
        Task<bool> AttemptDataRepair(string jsonData, Type targetType);
        
        /// <summary>
        /// Provides default values when deserialization fails completely
        /// Requirement 4.3: Use default values when data repair fails
        /// </summary>
        /// <typeparam name="T">The type to create default values for</typeparam>
        /// <returns>Default instance of the specified type</returns>
        Task<T> UseDefaultValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : new();
        
        /// <summary>
        /// Logs comprehensive diagnostic information for failures
        /// Requirement 4.2: Log detailed diagnostic information
        /// Requirement 4.4: Escalate with comprehensive error context
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="context">Additional context information</param>
        void LogDiagnosticInformation(Exception exception, string context);
    }
}