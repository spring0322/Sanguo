using GameObjects;
using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 检查Faction.Leader属性完整性的规则 - True Async Implementation
    /// </summary>
    public class FactionLeaderRule : AsyncValidationRuleBase<Faction>, IIntegrityRule
    {
        private readonly IAsyncFileService _fileService;
        private readonly IAsyncSerializationService _serialization;

        public override string RuleName => "FactionLeaderIntegrity";
        public override string Description => "检查势力的Leader属性关系完整性";

        /// <summary>
        /// Initializes a new instance of FactionLeaderRule with async services
        /// </summary>
        /// <param name="fileService">Async file service for potential reference data access</param>
        /// <param name="serialization">Async serialization service for potential data validation</param>
        public FactionLeaderRule(IAsyncFileService fileService = null, IAsyncSerializationService serialization = null)
        {
            _fileService = fileService;
            _serialization = serialization;
        }

        public override bool AppliesTo(Faction entity)
        {
            return entity != null;
        }

        public override async Task<ValidationResult> ValidateAsync(Faction faction, CancellationToken cancellationToken = default)
        {
            if (faction == null)
            {
                return CreateSuccess();
            }

            // 检查1: Leader为null但LeaderID有效
            if (faction.Leader == null && faction.LeaderID > 0)
            {
                return CreateFailedResult(
                    faction,
                    "Leader",
                    "NullReference",
                    $"Faction.Leader为null但LeaderID={faction.LeaderID}有效",
                    "从LeaderID重建Leader关系",
                    IssueSeverity.Critical
                );
            }

            // 检查2: Leader不为null但LeaderID无效
            if (faction.Leader != null && faction.LeaderID <= 0)
            {
                return CreateFailedResult(
                    faction,
                    "LeaderID",
                    "InvalidValue",
                    $"Faction.LeaderID={faction.LeaderID}无效但Leader不为null",
                    "从Leader对象更新LeaderID",
                    IssueSeverity.Warning
                );
            }

            // 检查3: Leader和LeaderID都有效但不匹配
            if (faction.Leader != null && faction.LeaderID > 0 && faction.Leader.ID != faction.LeaderID)
            {
                return CreateFailedResult(
                    faction,
                    "Leader",
                    "InvalidRelation",
                    $"Faction.Leader.ID={faction.Leader.ID}与LeaderID={faction.LeaderID}不匹配",
                    "同步Leader对象和LeaderID",
                    IssueSeverity.Warning
                );
            }

            // 检查4: Leader存在但Leader的BelongedFaction不是当前势力
            if (faction.Leader != null && faction.Leader.BelongedFaction != faction)
            {
                return CreateFailedResult(
                    faction,
                    "Leader",
                    "InvalidRelation",
                    $"Leader({faction.Leader.ID})的BelongedFaction不是当前势力({faction.ID})",
                    "修正Leader的BelongedFaction或选择其他Leader",
                    IssueSeverity.Critical
                );
            }

            // 检查5: 势力有人员但没有Leader
            if (faction.Leader == null && faction.LeaderID <= 0 && 
                faction.Persons != null && faction.Persons.Count > 0)
            {
                return CreateFailedResult(
                    faction,
                    "Leader",
                    "MissingRelation",
                    "势力有人员但没有指定Leader",
                    "从势力人员中选择一个作为Leader",
                    IssueSeverity.Warning
                );
            }

            // If async services are available, perform additional validation
            if (_fileService != null && _serialization != null)
            {
                // Example: Validate against reference data if needed
                // This demonstrates how to use async operations in validation rules
                await ValidateAgainstReferenceDataAsync(faction, cancellationToken)
                    .ConfigureAwait(false);
            }

            return CreateSuccess();
        }

        /// <summary>
        /// Example method showing how to use async file operations in validation
        /// This method demonstrates the pattern but doesn't perform actual validation
        /// </summary>
        /// <param name="faction">Faction to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        private async Task ValidateAgainstReferenceDataAsync(Faction faction, CancellationToken cancellationToken)
        {
            try
            {
                // Example: Check if reference data file exists
                // This is a placeholder to demonstrate async file operations
                // In a real scenario, this might validate against external configuration
                
                // Note: This is just an example - actual implementation would depend on requirements
                // await _fileService.ReadAllTextAsync("Data/FactionValidationRules.json", cancellationToken)
                //     .ConfigureAwait(false);
                
                // Placeholder - no actual validation needed for this rule
                await Task.CompletedTask.ConfigureAwait(false);
            }
            catch (System.IO.FileNotFoundException)
            {
                // Reference data not found - this is acceptable for this rule
                // Log if needed but don't fail validation
            }
            catch (System.OperationCanceledException)
            {
                // Re-throw cancellation exceptions
                throw;
            }
            catch
            {
                // Swallow other exceptions to prevent validation failures due to I/O issues
                // In production, you might want to log these
            }
        }

        #region IIntegrityRule Backward Compatibility

        /// <summary>
        /// Backward compatibility method for IIntegrityRule interface
        /// </summary>
        /// <param name="gameObject">Game object to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateAsync(GameObject gameObject)
        {
            if (gameObject is Faction faction)
            {
                return await ValidateAsync(faction, CancellationToken.None).ConfigureAwait(false);
            }
            return CreateSuccess();
        }

        /// <summary>
        /// Backward compatibility method for IIntegrityRule interface
        /// </summary>
        /// <param name="gameObject">Game object to check</param>
        /// <returns>True if rule applies to this object</returns>
        public bool AppliesTo(GameObject gameObject)
        {
            if (gameObject is Faction faction)
            {
                return base.AppliesTo(faction);
            }
            return false;
        }

        #endregion
    }
}