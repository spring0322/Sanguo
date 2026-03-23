using GameObjects;
using GameManager;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 缺失关系恢复策略
    /// 处理对象间关系缺失的问题
    /// </summary>
    public class MissingRelationRecoveryStrategy : IRecoveryStrategy
    {
        public async Task<RecoveryResult> RecoverAsync(IntegrityIssue issue)
        {
            var result = new RecoveryResult();

            try
            {
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] 处理缺失关系问题: {issue.ObjectType}[{issue.ObjectId}].{issue.PropertyName}");

                // 根据对象类型和属性名进行特定的恢复
                var recovered = issue.ObjectType switch
                {
                    "Faction" when issue.PropertyName == "Leader" => await RecoverFactionLeaderRelationAsync(issue),
                    "Architecture" when issue.PropertyName == "BelongedFaction" => await RecoverArchitectureFactionRelationAsync(issue),
                    "Person" when issue.PropertyName == "BelongedFaction" => await RecoverPersonFactionRelationAsync(issue),
                    "Troop" when issue.PropertyName == "BelongedLegion" => await RecoverTroopLegionRelationAsync(issue),
                    _ => await RecoverGenericMissingRelationAsync(issue)
                };

                result.Success = recovered;
                result.Message = recovered ? $"成功恢复 {issue.PropertyName} 缺失关系" : $"无法恢复 {issue.PropertyName} 缺失关系";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"恢复缺失关系时发生异常: {ex.Message}";
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] 异常: {ex.Message}");
                return result;
            }
        }

        private async Task<bool> RecoverFactionLeaderRelationAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步势力领袖关系恢复过程
                await Task.Delay(8).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Factions == null)
                    return false;

                var faction = (Session.Current.Scenario.Factions.GetGameObject(issue.ObjectId) is Faction ? (Faction)Session.Current.Scenario.Factions.GetGameObject(issue.ObjectId) : null);
                if (faction == null)
                    return false;

                // 如果势力有人员但没有Leader，选择一个合适的人员作为Leader
                if (faction.Persons != null && faction.Persons.Count > 0)
                {
                    Person bestCandidate = null;
                    int highestAbility = -1;

                    // 选择能力最高的人员作为Leader
                    foreach (var person in faction.Persons.GetList())
                    {
                        if (person is Person p)
                        {
                            // 计算综合能力（可以根据需要调整权重）
                            int totalAbility = p.Command + p.Strength + p.Intelligence + p.Politics + p.Glamour;
                            
                            if (totalAbility > highestAbility)
                            {
                                highestAbility = totalAbility;
                                bestCandidate = p;
                            }
                        }
                    }

                    if (bestCandidate != null)
                    {
                        faction.Leader = bestCandidate;
                        faction.LeaderID = bestCandidate.ID;
                        Debug.WriteLine($"[MissingRelationRecoveryStrategy] 选择最佳候选人作为Leader: {bestCandidate.Name} (能力值: {highestAbility})");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] RecoverFactionLeaderRelationAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverArchitectureFactionRelationAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步建筑势力关系恢复过程
                await Task.Delay(7).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Architectures == null)
                    return false;

                var architecture = Session.Current.Scenario.Architectures.GetGameObject(issue.ObjectId) is Architecture ? (Architecture)Session.Current.Scenario.Architectures.GetGameObject(issue.ObjectId) : null;
                if (architecture == null)
                    return false;

                // 尝试根据地理位置或其他线索找到合适的势力
                if (Session.Current.Scenario.Factions != null)
                {
                    // 简单策略：分配给第一个可用的势力
                    // 在实际应用中，可以根据地理位置、历史数据等进行更智能的分配
                    foreach (var faction in Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction is Faction f && f.Architectures != null)
                        {
                            // architecture.BelongedFaction = f;
                            // architecture.BelongedFactionID = f.ID;
                            
                            // 将建筑添加到势力的建筑列表中
                            if (!f.Architectures.HasGameObject(architecture.ID))
                            {
                                f.Architectures.Add(architecture);
                            }
                            
                            Debug.WriteLine($"[MissingRelationRecoveryStrategy] 将建筑分配给势力: {architecture.Name} -> {f.Name}");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] RecoverArchitectureFactionRelationAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverPersonFactionRelationAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步人物势力关系恢复过程
                await Task.Delay(6).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Persons == null)
                    return false;

                var person = Session.Current.Scenario.Persons.GetGameObject(issue.ObjectId) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(issue.ObjectId) : null;
                if (person == null)
                    return false;

                // 如果人物有BelongedFactionID但没有BelongedFaction，尝试恢复
                if (person.BelongedFactionID > 0 && Session.Current.Scenario.Factions != null)
                {
                    var faction = Session.Current.Scenario.Factions.GetGameObject(person.BelongedFactionID) is Faction ? (Faction)Session.Current.Scenario.Factions.GetGameObject(person.BelongedFactionID) : null;
                    if (faction != null)
                    {
                        // person.BelongedFaction = faction;
                        
                        // 确保人物在势力的人员列表中
                        if (faction.Persons != null && !faction.Persons.HasGameObject(person.ID))
                        {
                            faction.Persons.Add(person);
                        }
                        
                        Debug.WriteLine($"[MissingRelationRecoveryStrategy] 恢复人物势力关系: {person.Name} -> {faction.Name}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] RecoverPersonFactionRelationAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverTroopLegionRelationAsync(IntegrityIssue issue)
        {
            try
            {
                // 模拟异步部队军团关系恢复过程
                await Task.Delay(5).ConfigureAwait(false);
                
                if (Session.Current?.Scenario?.Troops == null)
                    return false;

                var troop = Session.Current.Scenario.Troops.GetGameObject(issue.ObjectId) is Troop ? (Troop)Session.Current.Scenario.Troops.GetGameObject(issue.ObjectId) : null;
                if (troop == null)
                    return false;

                // 如果部队有BelongedLegionID但没有BelongedLegion，尝试恢复
                // 如果部队有BelongedLegionID但没有BelongedLegion，尝试恢复
                /*
                if (troop.BelongedLegionID > 0 && Session.Current.Scenario.Legions != null)
                {
                    var legion = Session.Current.Scenario.Legions.GetGameObject(troop.BelongedLegionID) is Legion ? (Legion)Session.Current.Scenario.Legions.GetGameObject(troop.BelongedLegionID) : null;
                    if (legion != null)
                    {
                        troop.BelongedLegion = legion;
                        
                        // 确保部队在军团的部队列表中
                        if (legion.Troops != null && !legion.Troops.HasGameObject(troop.ID))
                        {
                            legion.Troops.Add(troop);
                        }
                        
                        Debug.WriteLine($"[MissingRelationRecoveryStrategy] 恢复部队军团关系: Troop[{troop.ID}] -> Legion[{legion.ID}]");
                        return true;
                    }
                }
                */
                Debug.WriteLine("[MissingRelationRecoveryStrategy] Troop.BelongedLegionID 不存在，跳过恢复");
                return false;

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissingRelationRecoveryStrategy] RecoverTroopLegionRelationAsync异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RecoverGenericMissingRelationAsync(IntegrityIssue issue)
        {
            // 模拟异步通用缺失关系恢复过程
            await Task.Delay(3).ConfigureAwait(false);
            
            // 通用的缺失关系恢复逻辑
            Debug.WriteLine($"[MissingRelationRecoveryStrategy] 通用缺失关系恢复: {issue.ObjectType}.{issue.PropertyName}");
            
            // 这里可以添加更多通用的恢复逻辑
            // 例如：根据ID字段恢复对象引用、建立双向关系等
            
            return false; // 暂时返回false，表示无法恢复
        }
    }
}
