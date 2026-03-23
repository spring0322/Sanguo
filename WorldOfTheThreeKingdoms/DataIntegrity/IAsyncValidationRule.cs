using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Generic async validation rule interface for type-safe validation
    /// </summary>
    /// <typeparam name="T">The type of object to validate</typeparam>
    public interface IAsyncValidationRule<T>
    {
        /// <summary>
        /// Rule name
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Rule description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Validates the specified object asynchronously
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if this rule applies to the specified entity
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the rule applies to this entity</returns>
        bool AppliesTo(T entity);
    }
}