using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 空引用恢复策略
    /// 处理null引用问题的恢复
    /// </summary>
    public class NullReferenceRecoveryStrategy : IRecoveryStrategy
    {
        public async Task<RecoveryResult> RecoverAsync(IntegrityIssue issue)
        {
            var result = new RecoveryResult();

            try
            {
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] 处理空引用问题: {issue.ObjectType}[{issue.ObjectId}].{issue.PropertyName}");

                // 根据对象类型和属性名进行特定的恢复
                var recovered = issue.ObjectType switch
                {
                    "Faction" when issue.PropertyName == "Leader" => await RecoverFactionLeaderAsync(issue),
                    "Person" when issue.PropertyName == "IdealTendency" => await RecoverPersonIdealTendencyAsync(issue),
                    "Architecture" when issue.PropertyName == "BelongedFaction" => await RecoverArchitectureBelongedFactionAsync(issue),
                    "Troop" when issue.PropertyName == "BelongedLegion" => await RecoverTroopBelongedLegionAsync(issue),
                    _ => await RecoverGenericNullReferenceAsync(issue)
                };

                result.Success = recovered;
                result.Message = recovered ? $"成功恢复 {issue.PropertyName} 空引用" : $"无法恢复 {issue.PropertyName} 空引用";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"恢复空引用时发生异常: {ex.Message}";
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] 异常: {ex.Message}");
                return result;
            }
        }

        private async Task<bool> RecoverFactionLeaderAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步势力领袖恢复过程
                await Task.Delay(8).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Factions == null)
                    return false;

                var faction = Session.Current.Scenario.Factions.GetGameObject(issue.ObjectId) is Faction ? (Faction)Session.Current.Scenario.Factions.GetGameObject(issue.ObjectId) : null;
                if (faction == null)
                    return false;

                // 如果有LeaderID，尝试从ID恢复Leader
                if (faction.LeaderID > 0 && Session.Current.Scenario.Persons != null)
                {
                    var leader = Session.Current.Scenario.Persons.GetGameObject(faction.LeaderID) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(faction.LeaderID) : null;
                    if (leader != null)
                    {
                        faction.Leader = leader;
                        Debug.WriteLine($"[NullReferenceRecoveryStrategy] 从LeaderID恢复Faction.Leader: {leader.Name}");
                        return true;
                    }
                }

                // 如果没有LeaderID或找不到对应的Person，从势力人员中选择一个作为Leader
                if (faction.Persons != null && faction.Persons.Count > 0)
                {
                    // 选择第一个人员作为Leader
                    var firstPerson = faction.Persons[0] as Person;
                    if (firstPerson != null)
                    {
                        faction.Leader = firstPerson;
                        faction.LeaderID = firstPerson.ID;
                        Debug.WriteLine($"[NullReferenceRecoveryStrategy] 设置势力第一个人员为Leader: {firstPerson.Name}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] RecoverFactionLeaderAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverPersonIdealTendencyAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步人物理想倾向恢复过程
                await Task.Delay(6).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Persons == null)
                    return false;

                var person = Session.Current.Scenario.Persons.GetGameObject(issue.ObjectId) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(issue.ObjectId) : null;
                if (person == null)
                    return false;

                // 尝试获取默认的IdealTendency
                if (Session.Current.Scenario.GameCommonData?.AllIdealTendencyKinds != null &&
                    Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.Count > 0)
                {
                    var defaultIdealTendency = Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds[0] as IdealTendencyKind;
                    if (defaultIdealTendency != null)
                    {
                        person.IdealTendency = defaultIdealTendency;
                        Debug.WriteLine($"[NullReferenceRecoveryStrategy] 设置默认IdealTendency: {defaultIdealTendency.Name}");
                        return true;
                    }
                }

                // 如果没有可用的IdealTendency，创建一个临时的
                person.IdealTendency = new IdealTendencyKind
                {
                    ID = 0,
                    Name = "默认理想倾向"
                };
                Debug.WriteLine("[NullReferenceRecoveryStrategy] 创建临时默认IdealTendency");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] RecoverPersonIdealTendencyAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverArchitectureBelongedFactionAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步建筑所属势力恢复过程
                await Task.Delay(7).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Architectures == null)
                    return false;

                var architecture = Session.Current.Scenario.Architectures.GetGameObject(issue.ObjectId) is Architecture ? (Architecture)Session.Current.Scenario.Architectures.GetGameObject(issue.ObjectId) : null;
                if (architecture == null)
                    return false;

                // 如果有BelongedFactionID，尝试从ID恢复BelongedFaction
                if (architecture.BelongedFactionID > 0 && Session.Current.Scenario.Factions != null)
                {
                    var faction = Session.Current.Scenario.Factions.GetGameObject(architecture.BelongedFactionID) is Faction ? (Faction)Session.Current.Scenario.Factions.GetGameObject(architecture.BelongedFactionID) : null;
                    if (faction != null)
                    {
                        architecture.BelongedFaction = faction;
                        Debug.WriteLine($"[NullReferenceRecoveryStrategy] 从BelongedFactionID恢复Architecture.BelongedFaction: {faction.Name}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] RecoverArchitectureBelongedFactionAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverTroopBelongedLegionAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步部队所属军团恢复过程
                await Task.Delay(5).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Troops == null)
                    return false;

                var troop = Session.Current.Scenario.Troops.GetGameObject(issue.ObjectId) is Troop ? (Troop)Session.Current.Scenario.Troops.GetGameObject(issue.ObjectId) : null;
                if (troop == null)
                    return false;

                // 如果有BelongedLegionID，尝试从ID恢复BelongedLegion
                // 如果有BelongedLegionID，尝试从ID恢复BelongedLegion
                /*
                if (troop.BelongedLegionID > 0 && Session.Current.Scenario.Legions != null)
                {
                    var legion = Session.Current.Scenario.Legions.GetGameObject(troop.BelongedLegionID) is Legion ? (Legion)Session.Current.Scenario.Legions.GetGameObject(troop.BelongedLegionID) : null;
                    if (legion != null)
                    {
                        troop.BelongedLegion = legion;
                        Debug.WriteLine($"[NullReferenceRecoveryStrategy] 从BelongedLegionID恢复Troop.BelongedLegion: Legion[{legion.ID}]");
                        return true;
                    }
                }
                */

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NullReferenceRecoveryStrategy] RecoverTroopBelongedLegionAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverGenericNullReferenceAsync(IntegrityIssue issue)
        {
            // 模拟异步通用空引用恢复过程
            await Task.Delay(3).ConfigureAwait(false);
            
            // 通用的空引用恢复逻辑
            Debug.WriteLine($"[NullReferenceRecoveryStrategy] 通用空引用恢复: {issue.ObjectType}.{issue.PropertyName}");
            
            // 这里可以添加更多通用的恢复逻辑
            // 例如：设置默认值、从配置文件读取默认值等
            
            return false; // 暂时返回false，表示无法恢复
        }
    }
}
