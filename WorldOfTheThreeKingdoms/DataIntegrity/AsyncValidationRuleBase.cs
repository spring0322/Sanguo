using System.Threading;
using System.Threading.Tasks;
using GameObjects;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Abstract base class for async validation rules providing common functionality
    /// </summary>
    /// <typeparam name="T">The type of object to validate</typeparam>
    public abstract class AsyncValidationRuleBase<T> : IAsyncValidationRule<T> where T : GameObject
    {
        public abstract string RuleName { get; }
        public abstract string Description { get; }
        public bool IsEnabled { get; set; } = true;

        public abstract Task<ValidationResult> ValidateAsync(T entity, CancellationToken cancellationToken = default);

        public virtual bool AppliesTo(T entity)
        {
            // Default implementation: check if entity is not null
            return entity != null;
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>Successful validation result</returns>
        protected ValidationResult CreateSuccess()
        {
            return ValidationResult.Success();
        }

        /// <summary>
        /// Creates a failed validation result with basic information
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="suggestedFix">Suggested fix</param>
        /// <param name="severity">Issue severity</param>
        /// <returns>Failed validation result</returns>
        protected ValidationResult CreateFailure(string message, string suggestedFix = "", IssueSeverity severity = IssueSeverity.Warning)
        {
            return ValidationResult.Failed(message, suggestedFix, severity);
        }

        /// <summary>
        /// Creates a failed validation result with detailed information
        /// </summary>
        /// <param name="entity">The entity being validated</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="issueType">Issue type</param>
        /// <param name="message">Error message</param>
        /// <param name="suggestedFix">Suggested fix</param>
        /// <param name="severity">Issue severity</param>
        /// <returns>Failed validation result</returns>
        protected ValidationResult CreateFailedResult(T entity, string propertyName, 
            string issueType, string message, string suggestedFix = "", IssueSeverity severity = IssueSeverity.Warning)
        {
            // Extract object type and ID if the entity has these properties
            string objectType = entity?.GetType().Name ?? "Unknown";
            int objectId = entity?.ID ?? -1;

            return ValidationResult.Failed(
                objectType,
                objectId,
                propertyName,
                issueType,
                message,
                suggestedFix,
                severity
            );
        }
    }
}
