using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Comprehensive validation of STJ type registrations and dependencies.
    /// Validates Requirements 2.1, 2.2, 2.3, 2.4, 2.5
    /// </summary>
    public interface ITypeRegistrationValidator
    {
        /// <summary>
        /// Validates EventEffectKindTable registrations for both namespaces
        /// Requirement 2.1: Verify EventEffectKindTable registrations for both namespaces
        /// </summary>
        /// <returns>Validation result for EventEffectKindTable registrations</returns>
        ValidationResult ValidateEventEffectKindTableRegistration();
        
        /// <summary>
        /// Validates all dependent types for a given root type
        /// Requirement 2.2: Ensure EventEffectKind and all its properties are registered
        /// </summary>
        /// <param name="rootType">The root type to validate dependencies for</param>
        /// <returns>Validation result for dependent types</returns>
        ValidationResult ValidateDependentTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.Interfaces)] Type rootType);
        
        /// <summary>
        /// Validates polymorphic type registrations
        /// Requirement 1.5: Verify all derived EventEffectKind types are registered
        /// </summary>
        /// <param name="baseType">The base type to validate polymorphic registrations for</param>
        /// <returns>Validation result for polymorphic types</returns>
        ValidationResult ValidatePolymorphicTypes(Type baseType);
        
        /// <summary>
        /// Validates JsonSerializerOptions configuration
        /// Requirement 2.5: Confirm the GameJsonContext is properly configured
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to validate</param>
        /// <returns>Validation result for JsonSerializerOptions</returns>
        ValidationResult ValidateJsonSerializerOptions(JsonSerializerOptions options);
        
        /// <summary>
        /// Generates a fix plan based on validation results
        /// </summary>
        /// <param name="result">The validation result to generate fixes for</param>
        /// <returns>A comprehensive fix plan</returns>
        Task<RegistrationFixPlan> GenerateFixPlan(ValidationResult result);
    }
}