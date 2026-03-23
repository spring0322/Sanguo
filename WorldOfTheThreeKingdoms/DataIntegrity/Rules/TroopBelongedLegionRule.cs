using GameObjects;
using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 检查Troop.BelongedLegion属性完整性的规则 - True Async Implementation
    /// </summary>
    public class TroopBelongedLegionRule : AsyncValidationRuleBase<Troop>, IIntegrityRule
    {
        private readonly IAsyncFileService _fileService;
        private readonly IAsyncSerializationService _serialization;

        public override string RuleName => "TroopBelongedLegionIntegrity";
        public override string Description => "检查部队的BelongedLegion属性关系完整性";

        /// <summary>
        /// Initializes a new instance of TroopBelongedLegionRule with async services
        /// </summary>
        /// <param name="fileService">Async file service for potential reference data access</param>
        /// <param name="serialization">Async serialization service for potential data validation</param>
        public TroopBelongedLegionRule(IAsyncFileService fileService = null, IAsyncSerializationService serialization = null)
        {
            _fileService = fileService;
            _serialization = serialization;
        }

        public override bool AppliesTo(Troop entity)
        {
            return entity != null;
        }

        public override async Task<ValidationResult> ValidateAsync(Troop troop, CancellationToken cancellationToken = default)
        {
            if (troop == null)
            {
                return CreateSuccess();
            }

            // 检查1: BelongedLegion不为null但军团的部队列表中没有该部队
            if (troop.BelongedLegion != null && 
                troop.BelongedLegion.Troops != null &&
                !troop.BelongedLegion.Troops.HasGameObject(troop.ID))
            {
                return CreateFailedResult(
                    troop,
                    "BelongedLegion",
                    "InvalidRelation",
                    "部队属于军团但不在军团的部队列表中",
                    "将部队添加到军团的部队列表或清除BelongedLegion",
                    IssueSeverity.Warning
                );
            }

            // 检查2: BelongedLegion不为null但军团属于不同的势力
            if (troop.BelongedLegion != null && 
                troop.BelongedFaction != null &&
                troop.BelongedLegion.BelongedFaction != troop.BelongedFaction)
            {
                return CreateFailedResult(
                    troop,
                    "BelongedLegion",
                    "InvalidRelation",
                    $"部队所属势力({troop.BelongedFaction.ID})与军团所属势力({troop.BelongedLegion.BelongedFaction?.ID})不匹配",
                    "修正部队或军团的势力归属",
                    IssueSeverity.Critical
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
            if (gameObject is Troop troop)
            {
                return await ValidateAsync(troop, CancellationToken.None).ConfigureAwait(false);
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
            if (gameObject is Troop troop)
            {
                return base.AppliesTo(troop);
            }
            return false;
        }

        #endregion
    }
}