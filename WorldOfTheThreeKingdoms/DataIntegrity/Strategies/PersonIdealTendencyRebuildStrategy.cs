using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Person.IdealTendency关系重建策略
    /// </summary>
    public class PersonIdealTendencyRebuildStrategy : RelationshipRebuildStrategy
    {
        public override string StrategyName => "PersonIdealTendencyRebuild";
        public override string Description => "重建Person.IdealTendency关系";

        public override bool AppliesTo(GameObject gameObject)
        {
            return gameObject is Person;
        }

        public override async Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject)
        {
            if (!(gameObject is Person person))
            {
                return CreateResult(gameObject, false, "对象不是Person类型");
            }

            // 模拟异步理想倾向重建过程
            await Task.Delay(8).ConfigureAwait(false);

            var result = CreateResult(person, true);

            try
            {
                // 情况1: IdealTendency为null
                if (person.IdealTendency == null)
                {
                    Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] 为Person[{person.ID}] {person.Name} 分配IdealTendency");
                    
                    // 尝试从全局IdealTendencyKinds中分配一个
                    if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null &&
                        Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.Count > 0)
                    {
                        // 获取第一个可用的IdealTendencyKind作为默认值
                        var defaultTendency = Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds[0] as IdealTendencyKind;
                        if (defaultTendency != null)
                        {
                            person.IdealTendency = defaultTendency;
                            result.AddRepairedRelationship($"分配默认IdealTendency: {defaultTendency.Name}");
                            Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] 为 {person.Name} 分配默认IdealTendency: {defaultTendency.Name}");
                        }
                        else
                        {
                            result.AddFailedRelationship("AllIdealTendencyKinds[0]不是有效的IdealTendencyKind对象");
                        }
                    }
                    else
                    {
                        result.AddFailedRelationship("AllIdealTendencyKinds为null或为空，无法分配IdealTendency");
                        Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] AllIdealTendencyKinds不可用，无法为 {person.Name} 分配IdealTendency");
                    }
                }

                // 情况2: IdealTendency存在但可能无效
                if (person.IdealTendency != null)
                {
                    // 验证IdealTendency是否在全局列表中
                    if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null)
                    {
                        bool isValidIdealTendency = false;
                        foreach (var tendency in Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.GetList())
                        {
                            if (tendency is IdealTendencyKind itk && itk.ID == person.IdealTendency.ID)
                            {
                                isValidIdealTendency = true;
                                break;
                            }
                        }

                        if (!isValidIdealTendency)
                        {
                            Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] Person[{person.ID}] {person.Name} 的IdealTendency无效，重新分配");
                            
                            // 重新分配有效的IdealTendency
                            var defaultTendency = Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds[0] as IdealTendencyKind;
                            if (defaultTendency != null)
                            {
                                person.IdealTendency = defaultTendency;
                                result.AddRepairedRelationship($"重新分配有效的IdealTendency: {defaultTendency.Name}");
                            }
                            else
                            {
                                result.AddFailedRelationship("无法找到有效的IdealTendency进行重新分配");
                            }
                        }
                    }
                }

                // 情况3: 尝试根据人物特征智能分配IdealTendency
                if (person.IdealTendency != null && Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null)
                {
                    // 这里可以根据人物的属性（如统率、智力、政治等）来选择更合适的IdealTendency
                    // 暂时保持现有的IdealTendency，除非它无效
                    Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] Person[{person.ID}] {person.Name} 的IdealTendency有效: {person.IdealTendency.Name}");
                }
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"重建异常: {ex.Message}";
                Debug.WriteLine($"[PersonIdealTendencyRebuildStrategy] 重建异常: {ex.Message}");
            }

            return result;
        }

        public override async Task<bool> ValidateAsync(GameObject gameObject)
        {
            if (!(gameObject is Person person))
            {
                return true; // 不适用的对象认为验证通过
            }

            // 模拟异步验证过程
            await Task.Delay(3).ConfigureAwait(false);

            // 验证IdealTendency不为null
            if (person.IdealTendency == null)
                return false;

            // 验证IdealTendency是否在全局列表中
            if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null)
            {
                bool isValidIdealTendency = false;
                foreach (var tendency in Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.GetList())
                {
                    if (tendency is IdealTendencyKind itk && itk.ID == person.IdealTendency.ID)
                    {
                        isValidIdealTendency = true;
                        break;
                    }
                }

                if (!isValidIdealTendency)
                    return false;
            }

            return true;
        }
    }
}