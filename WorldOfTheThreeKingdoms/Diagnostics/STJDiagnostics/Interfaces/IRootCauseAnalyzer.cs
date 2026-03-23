using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Systematically diagnoses STJ deserialization failures through comprehensive analysis.
    /// Validates Requirements 1.1, 1.2, 1.3, 1.4, 1.5
    /// </summary>
    public interface IRootCauseAnalyzer
    {
        /// <summary>
        /// Analyzes a deserialization failure to identify root causes
        /// </summary>
        /// <param name="targetType">The type that failed to deserialize</param>
        /// <param name="jsonData">The JSON data that caused the failure</param>
        /// <param name="options">The JsonSerializerOptions used during deserialization</param>
        /// <returns>Comprehensive diagnostic result with identified issues and recommendations</returns>
        Task<DiagnosticResult> AnalyzeDeserializationFailure(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, 
            string jsonData, 
            JsonSerializerOptions options);
        
        /// <summary>
        /// Validates the complete type registration chain for a given type
        /// Requirement 1.1: Examine complete type registration chain
        /// Requirement 1.2: Verify all dependent types are registered
        /// </summary>
        /// <param name="rootType">The root type to analyze</param>
        /// <returns>Report detailing type registration status and dependencies</returns>
        Task<TypeRegistrationReport> ValidateTypeRegistrationChain([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type rootType);
        
        /// <summary>
        /// Detects namespace conflicts that could cause type resolution ambiguity
        /// Requirement 1.3: Identify ambiguous type references between namespaces
        /// </summary>
        /// <param name="targetType">The type to check for namespace conflicts</param>
        /// <returns>Report detailing any namespace conflicts found</returns>
        Task<NamespaceConflictReport> DetectNamespaceConflicts(Type targetType);
        
        /// <summary>
        /// Validates JSON structure against expected class structure
        /// Requirement 1.4: Compare actual JSON data against expected class structure
        /// </summary>
        /// <param name="targetType">The expected target type</param>
        /// <param name="jsonData">The JSON data to validate</param>
        /// <returns>Report detailing structural mismatches</returns>
        Task<StructuralMismatchReport> ValidateJsonStructure([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, string jsonData);
    }
}