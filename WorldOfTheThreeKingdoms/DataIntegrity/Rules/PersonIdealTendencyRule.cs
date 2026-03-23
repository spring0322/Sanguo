using GameObjects;
using GameManager;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 检查Person.IdealTendency属性完整性的规则 - True Async Implementation
    /// </summary>
    public class PersonIdealTendencyRule : AsyncValidationRuleBase<Person>, IIntegrityRule
    {
        private readonly IAsyncFileService _fileService;
        private readonly IAsyncSerializationService _serialization;

        public override string RuleName => "PersonIdealTendencyIntegrity";
        public override string Description => "检查人物的IdealTendency属性是否有效";

        /// <summary>
        /// Initializes a new instance of PersonIdealTendencyRule with async services
        /// </summary>
        /// <param name="fileService">Async file service for potential reference data access</param>
        /// <param name="serialization">Async serialization service for potential data validation</param>
        public PersonIdealTendencyRule(IAsyncFileService fileService = null, IAsyncSerializationService serialization = null)
        {
            _fileService = fileService;
            _serialization = serialization;
        }

        public override bool AppliesTo(Person entity)
        {
            return entity != null;
        }

        public override async Task<ValidationResult> ValidateAsync(Person person, CancellationToken cancellationToken = default)
        {
            if (person == null)
            {
                return CreateSuccess();
            }

            // 检查1: IdealTendency为null或无效
            if (person.IdealTendency == null)
            {
                // 检查是否有可用的IdealTendencyKinds
                var availableKinds = Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds?.Count ?? 0;
                var errorMessage = availableKinds > 0 
                    ? "Person.IdealTendency为null，但系统中有可用的IdealTendencyKinds"
                    : "Person.IdealTendency为null，且系统中没有可用的IdealTendencyKinds";
                var suggestion = availableKinds > 0 
                    ? "为人物分配一个IdealTendency"
                    : "检查游戏数据完整性，确保AllIdealTendencyKinds已正确加载";

                return CreateFailedResult(
                    person,
                    "IdealTendency",
                    "NullReference",
                    errorMessage,
                    suggestion,
                    IssueSeverity.Warning
                );
            }

            // 检查2: IdealTendency存在但不在系统的IdealTendencyKinds中
            if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null && 
                Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.Count > 0)
            {
                var isValidIdealTendency = Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.GameObjects
                    .Any(kind => kind.ID == person.IdealTendency.ID);

                if (!isValidIdealTendency)
                {
                    return CreateFailedResult(
                        person,
                        "IdealTendency",
                        "InvalidReference",
                        $"Person.IdealTendency.ID={person.IdealTendency.ID}不在系统的IdealTendencyKinds中",
                        "重新分配有效的IdealTendency或检查数据完整性",
                        IssueSeverity.Warning
                    );
                }
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
            if (gameObject is Person person)
            {
                return await ValidateAsync(person, CancellationToken.None).ConfigureAwait(false);
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
            if (gameObject is Person person)
            {
                return base.AppliesTo(person);
            }
            return false;
        }

        #endregion
    }
}