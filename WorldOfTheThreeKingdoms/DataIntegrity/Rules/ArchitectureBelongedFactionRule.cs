using GameObjects;
using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 检查Architecture.BelongedFaction属性完整性的规则 - True Async Implementation
    /// </summary>
    public class ArchitectureBelongedFactionRule : AsyncValidationRuleBase<Architecture>, IIntegrityRule
    {
        private readonly IAsyncFileService _fileService;
        private readonly IAsyncSerializationService _serialization;

        public override string RuleName => "ArchitectureBelongedFactionIntegrity";
        public override string Description => "检查建筑的BelongedFaction属性是否为null或无效";

        /// <summary>
        /// Initializes a new instance of ArchitectureBelongedFactionRule with async services
        /// </summary>
        /// <param name="fileService">Async file service for potential reference data access</param>
        /// <param name="serialization">Async serialization service for potential data validation</param>
        public ArchitectureBelongedFactionRule(IAsyncFileService fileService = null, IAsyncSerializationService serialization = null)
        {
            _fileService = fileService;
            _serialization = serialization;
        }

        public override bool AppliesTo(Architecture entity)
        {
            return entity != null;
        }

        public override async Task<ValidationResult> ValidateAsync(Architecture architecture, CancellationToken cancellationToken = default)
        {
            if (architecture == null)
            {
                return CreateSuccess();
            }

            // 检查1: BelongedFaction为null
            if (architecture.BelongedFaction == null)
            {
                return CreateFailedResult(
                    architecture,
                    "BelongedFaction",
                    "NullReference",
                    "Architecture.BelongedFaction为null",
                    "为建筑分配所属势力或设置为中立",
                    IssueSeverity.Critical
                );
            }

            // 检查2: BelongedFaction存在但建筑不在势力的建筑列表中
            if (architecture.BelongedFaction != null && 
                architecture.BelongedFaction.Architectures != null &&
                !architecture.BelongedFaction.Architectures.HasGameObject(architecture.ID))
            {
                return CreateFailedResult(
                    architecture,
                    "BelongedFaction",
                    "InvalidRelation",
                    "建筑属于势力但不在势力的建筑列表中",
                    "将建筑添加到势力的建筑列表或修正BelongedFaction",
                    IssueSeverity.Warning
                );
            }

            // Apply ConfigureAwait(false) for any potential async operations
            await Task.CompletedTask.ConfigureAwait(false);

            return CreateSuccess();
        }

        #region IIntegrityRule Backward Compatibility

        /// <summary>
        /// Backward compatibility method for IIntegrityRule interface
        /// </summary>
        /// <param name="gameObject">Game object to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateAsync(GameObject gameObject)
        {
            if (gameObject is Architecture architecture)
            {
                return await ValidateAsync(architecture, CancellationToken.None).ConfigureAwait(false);
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
            if (gameObject is Architecture architecture)
            {
                return base.AppliesTo(architecture);
            }
            return false;
        }

        #endregion
    }
}