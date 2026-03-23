using GameObjects;
using GameManager;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Faction.Leader关系重建策略
    /// </summary>
    public class FactionLeaderRebuildStrategy : RelationshipRebuildStrategy
    {
        public override string StrategyName => "FactionLeaderRebuild";
        public override string Description => "重建Faction.Leader关系";

        public override bool AppliesTo(GameObject gameObject)
        {
            return gameObject is Faction;
        }

        public override async Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject)
        {
            if (!(gameObject is Faction faction))
            {
                return CreateResult(gameObject, false, "对象不是Faction类型");
            }

            // 模拟异步势力领袖重建过程
            await Task.Delay(6).ConfigureAwait(false);

            var result = CreateResult(faction, true);

            try
            {
                // 情况1: Leader为null但LeaderID有效
                if (faction.Leader == null && faction.LeaderID > 0)
                {
                    Debug.WriteLine($"[FactionLeaderRebuildStrategy] 尝试从LeaderID={faction.LeaderID}重建Leader");
                    
                    if (Session.Current?.Scenario?.Persons != null)
                    {
                        var leader = Session.Current.Scenario.Persons.GetGameObject(faction.LeaderID) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(faction.LeaderID) : null;
                        if (leader != null)
                        {
                            faction.Leader = leader;
                            result.AddRepairedRelationship($"从LeaderID={faction.LeaderID}重建Leader: {leader.Name}");
                            Debug.WriteLine($"[FactionLeaderRebuildStrategy] 成功重建Leader: {leader.Name}");
                        }
                        else
                        {
                            result.AddFailedRelationship($"无法找到LeaderID={faction.LeaderID}对应的Person对象");
                            Debug.WriteLine($"[FactionLeaderRebuildStrategy] 无法找到LeaderID={faction.LeaderID}对应的Person对象");
                        }
                    }
                    else
                    {
                        result.AddFailedRelationship("Session.Current.Scenario.Persons为null");
                    }
                }

                // 情况2: Leader不为null但LeaderID无效
                if (faction.Leader != null && faction.LeaderID <= 0)
                {
                    Debug.WriteLine($"[FactionLeaderRebuildStrategy] 根据Leader对象更新LeaderID");
                    faction.LeaderID = faction.Leader.ID;
                    result.AddRepairedRelationship($"根据Leader对象更新LeaderID: {faction.Leader.ID}");
                }

                // 情况3: Leader和LeaderID都有效但不匹配
                if (faction.Leader != null && faction.LeaderID > 0 && faction.Leader.ID != faction.LeaderID)
                {
                    Debug.WriteLine($"[FactionLeaderRebuildStrategy] 同步Leader对象和LeaderID");
                    // 优先使用Leader对象的ID
                    faction.LeaderID = faction.Leader.ID;
                    result.AddRepairedRelationship($"同步LeaderID为Leader.ID: {faction.Leader.ID}");
                }

                // 情况4: Leader存在但不属于该势力
                if (faction.Leader != null && faction.Leader.BelongedFaction != faction)
                {
                    Debug.WriteLine($"[FactionLeaderRebuildStrategy] 修正Leader的BelongedFaction");
                    // 这里需要谨慎处理，可能需要更复杂的逻辑
                    // 暂时只记录问题，不自动修复
                    result.AddFailedRelationship($"Leader不属于该势力，需要手动处理");
                }

                // 情况5: 势力有人员但没有Leader
                if (faction.Leader == null && faction.LeaderID <= 0 && 
                    faction.Persons != null && faction.Persons.Count > 0)
                {
                    Debug.WriteLine($"[FactionLeaderRebuildStrategy] 从势力人员中选择Leader");
                    
                    // 选择功绩最高的人员作为Leader
                    Person bestPerson = null;
                    int maxMerit = -1;
                    
                    foreach (var obj in faction.Persons.GetList())
                    {
                        if (obj is Person person && person.Merit > maxMerit)
                        {
                            maxMerit = person.Merit;
                            bestPerson = person;
                        }
                    }
                    
                    if (bestPerson != null)
                    {
                        faction.Leader = bestPerson;
                        faction.LeaderID = bestPerson.ID;
                        result.AddRepairedRelationship($"选择功绩最高的人员作为Leader: {bestPerson.Name}");
                        Debug.WriteLine($"[FactionLeaderRebuildStrategy] 选择 {bestPerson.Name} 作为新Leader");
                    }
                    else
                    {
                        result.AddFailedRelationship("势力人员列表中没有有效的Person对象");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"重建异常: {ex.Message}";
                Debug.WriteLine($"[FactionLeaderRebuildStrategy] 重建异常: {ex.Message}");
            }

            return result;
        }

        public override async Task<bool> ValidateAsync(GameObject gameObject)
        {
            if (!(gameObject is Faction faction))
            {
                return true; // 不适用的对象认为验证通过
            }

            // 模拟异步验证过程
            await Task.Delay(3).ConfigureAwait(false);

            // 验证Leader关系的一致性
            if (faction.Leader == null && faction.LeaderID > 0)
                return false;

            if (faction.Leader != null && faction.LeaderID <= 0)
                return false;

            if (faction.Leader != null && faction.LeaderID > 0 && faction.Leader.ID != faction.LeaderID)
                return false;

            if (faction.Leader != null && faction.Leader.BelongedFaction != faction)
                return false;

            if (faction.Leader == null && faction.LeaderID <= 0 && 
                faction.Persons != null && faction.Persons.Count > 0)
                return false;

            return true;
        }
    }
}
