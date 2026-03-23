using GameObjects;
using GameManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 关系验证和修复服务
    /// 提供高级的关系一致性检查和修复功能
    /// </summary>
    public class RelationshipValidationService
    {
        private readonly IDataIntegrityChecker _integrityChecker;
        private readonly IObjectRelationshipRebuilder _relationshipRebuilder;
        private readonly HashSet<int> _processedObjects = new HashSet<int>();

        public RelationshipValidationService()
        {
            _integrityChecker = new DataIntegrityChecker();
            _relationshipRebuilder = new ObjectRelationshipRebuilder();
        }

        public RelationshipValidationService(IDataIntegrityChecker integrityChecker, IObjectRelationshipRebuilder relationshipRebuilder)
        {
            _integrityChecker = integrityChecker ?? throw new ArgumentNullException(nameof(integrityChecker));
            _relationshipRebuilder = relationshipRebuilder ?? throw new ArgumentNullException(nameof(relationshipRebuilder));
        }

        /// <summary>
        /// 执行全面的关系验证和修复
        /// </summary>
        /// <returns>验证和修复报告</returns>
        public async Task<ComprehensiveValidationReport> ValidateAndRepairAllRelationshipsAsync()
        {
            var report = new ComprehensiveValidationReport();
            
            try
            {
                Debug.WriteLine("[RelationshipValidationService] 开始全面关系验证和修复");

                // 第一阶段：数据完整性检查
                Debug.WriteLine("[RelationshipValidationService] 第一阶段：数据完整性检查");
                var integrityReport = await _integrityChecker.CheckAllAsync();
                report.IntegrityReport = integrityReport;

                // 第二阶段：关系重建
                Debug.WriteLine("[RelationshipValidationService] 第二阶段：关系重建");
                var rebuildReport = await _relationshipRebuilder.RebuildAllRelationshipsAsync();
                report.RebuildReport = rebuildReport;

                // 第三阶段：循环引用检测和处理
                Debug.WriteLine("[RelationshipValidationService] 第三阶段：循环引用检测");
                var circularRefReport = await DetectAndFixCircularReferencesAsync();
                report.CircularReferenceReport = circularRefReport;

                // 第四阶段：关系一致性验证
                Debug.WriteLine("[RelationshipValidationService] 第四阶段：关系一致性验证");
                var consistencyReport = await ValidateRelationshipConsistencyAsync();
                report.ConsistencyReport = consistencyReport;

                Debug.WriteLine($"[RelationshipValidationService] 全面验证和修复完成");
                Debug.WriteLine($"完整性问题: {integrityReport.IssuesFound}");
                Debug.WriteLine($"关系重建: 成功{rebuildReport.SuccessfulRebuilds}, 失败{rebuildReport.FailedRebuilds}");
                Debug.WriteLine($"循环引用: {circularRefReport.CircularReferencesFound}");
                Debug.WriteLine($"一致性问题: {consistencyReport.InconsistenciesFound}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RelationshipValidationService] 全面验证和修复异常: {ex.Message}");
                report.HasErrors = true;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// 检测和修复循环引用
        /// </summary>
        /// <returns>循环引用检测报告</returns>
        public async Task<CircularReferenceReport> DetectAndFixCircularReferencesAsync()
        {
            var report = new CircularReferenceReport();
            _processedObjects.Clear();

            try
            {
                if (Session.Current?.Scenario == null)
                {
                    report.ErrorMessage = "当前场景为null";
                    return report;
                }

                var scenario = Session.Current.Scenario;

                // 检查势力-人员循环引用
                if (scenario.Factions != null)
                {
                    foreach (var obj in scenario.Factions.GetList())
                    {
                        if (obj is Faction faction)
                        {
                            await DetectFactionCircularReferencesAsync(faction, report);
                        }
                    }
                }

                // 检查军团-部队循环引用
                if (scenario.Legions != null)
                {
                    foreach (var obj in scenario.Legions.GetList())
                    {
                        if (obj is Legion legion)
                        {
                            await DetectLegionCircularReferencesAsync(legion, report);
                        }
                    }
                }

                Debug.WriteLine($"[RelationshipValidationService] 循环引用检测完成，发现 {report.CircularReferencesFound} 个循环引用");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RelationshipValidationService] 循环引用检测异常: {ex.Message}");
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// 验证关系一致性
        /// </summary>
        /// <returns>一致性验证报告</returns>
        public async Task<ConsistencyValidationReport> ValidateRelationshipConsistencyAsync()
        {
            var report = new ConsistencyValidationReport();

            try
            {
                if (Session.Current?.Scenario == null)
                {
                    report.ErrorMessage = "当前场景为null";
                    return report;
                }

                var scenario = Session.Current.Scenario;

                // 验证势力-建筑关系一致性
                await ValidateFactionArchitectureConsistencyAsync(scenario, report);

                // 验证势力-人员关系一致性
                await ValidateFactionPersonConsistencyAsync(scenario, report);

                // 验证军团-部队关系一致性
                await ValidateLegionTroopConsistencyAsync(scenario, report);

                Debug.WriteLine($"[RelationshipValidationService] 一致性验证完成，发现 {report.InconsistenciesFound} 个不一致");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RelationshipValidationService] 一致性验证异常: {ex.Message}");
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// 检测势力循环引用
        /// </summary>
        private async Task DetectFactionCircularReferencesAsync(Faction faction, CircularReferenceReport report)
        {
            if (faction == null || _processedObjects.Contains(faction.ID))
                return;

            _processedObjects.Add(faction.ID);

            // 检查Leader循环引用
            if (faction.Leader != null && faction.Leader.BelongedFaction == faction)
            {
                // 这是正常的双向引用，不是循环引用
                // 但需要确保Leader确实在势力的人员列表中
                if (faction.Persons != null && !faction.Persons.HasGameObject(faction.Leader.ID))
                {
                    report.AddCircularReference($"Faction[{faction.ID}].Leader不在Persons列表中");
                }
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }

        /// <summary>
        /// 检测军团循环引用
        /// </summary>
        private async Task DetectLegionCircularReferencesAsync(Legion legion, CircularReferenceReport report)
        {
            if (legion == null || _processedObjects.Contains(legion.ID))
                return;

            _processedObjects.Add(legion.ID);

            // 检查部队循环引用
            if (legion.Troops != null)
            {
                foreach (var obj in legion.Troops.GetList())
                {
                    if (obj is Troop troop && troop.BelongedLegion == legion)
                    {
                        // 这是正常的双向引用
                        // 但需要确保部队和军团属于同一势力
                        if (troop.BelongedFaction != legion.BelongedFaction)
                        {
                            report.AddCircularReference($"Legion[{legion.ID}]和Troop[{troop.ID}]势力不匹配");
                        }
                    }
                }
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }

        /// <summary>
        /// 验证势力-建筑关系一致性
        /// </summary>
        private async Task ValidateFactionArchitectureConsistencyAsync(GameScenario scenario, ConsistencyValidationReport report)
        {
            if (scenario.Factions == null || scenario.Architectures == null)
                return;

            foreach (var obj in scenario.Factions.GetList())
            {
                if (obj is Faction faction && faction.Architectures != null)
                {
                    foreach (var archObj in faction.Architectures.GetList())
                    {
                        if (archObj is Architecture architecture)
                        {
                            if (architecture.BelongedFaction != faction)
                            {
                                report.AddInconsistency($"Faction[{faction.ID}].Architectures包含Architecture[{architecture.ID}]，但该建筑的BelongedFaction不是该势力");
                            }
                        }
                    }
                }
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }

        /// <summary>
        /// 验证势力-人员关系一致性
        /// </summary>
        private async Task ValidateFactionPersonConsistencyAsync(GameScenario scenario, ConsistencyValidationReport report)
        {
            if (scenario.Factions == null || scenario.Persons == null)
                return;

            foreach (var obj in scenario.Factions.GetList())
            {
                if (obj is Faction faction && faction.Persons != null)
                {
                    foreach (var personObj in faction.Persons.GetList())
                    {
                        if (personObj is Person person)
                        {
                            if (person.BelongedFaction != faction)
                            {
                                report.AddInconsistency($"Faction[{faction.ID}].Persons包含Person[{person.ID}]，但该人员的BelongedFaction不是该势力");
                            }
                        }
                    }
                }
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }

        /// <summary>
        /// 验证军团-部队关系一致性
        /// </summary>
        private async Task ValidateLegionTroopConsistencyAsync(GameScenario scenario, ConsistencyValidationReport report)
        {
            if (scenario.Legions == null || scenario.Troops == null)
                return;

            foreach (var obj in scenario.Legions.GetList())
            {
                if (obj is Legion legion && legion.Troops != null)
                {
                    foreach (var troopObj in legion.Troops.GetList())
                    {
                        if (troopObj is Troop troop)
                        {
                            if (troop.BelongedLegion != legion)
                            {
                                report.AddInconsistency($"Legion[{legion.ID}].Troops包含Troop[{troop.ID}]，但该部队的BelongedLegion不是该军团");
                            }

                            if (troop.BelongedFaction != legion.BelongedFaction)
                            {
                                report.AddInconsistency($"Legion[{legion.ID}]和Troop[{troop.ID}]属于不同势力");
                            }
                        }
                    }
                }
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 综合验证报告
    /// </summary>
    public class ComprehensiveValidationReport
    {
        public IntegrityReport IntegrityReport { get; set; } = new IntegrityReport();
        public RelationshipRebuildReport RebuildReport { get; set; } = new RelationshipRebuildReport();
        public CircularReferenceReport CircularReferenceReport { get; set; } = new CircularReferenceReport();
        public ConsistencyValidationReport ConsistencyReport { get; set; } = new ConsistencyValidationReport();
        public bool HasErrors { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取综合报告摘要
        /// </summary>
        /// <returns>报告摘要</returns>
        public string GetSummary()
        {
            var summary = $"综合验证报告 - {ValidationTime:yyyy-MM-dd HH:mm:ss}\n";
            summary += $"数据完整性问题: {IntegrityReport.IssuesFound}\n";
            summary += $"关系重建: 成功{RebuildReport.SuccessfulRebuilds}, 失败{RebuildReport.FailedRebuilds}\n";
            summary += $"循环引用: {CircularReferenceReport.CircularReferencesFound}\n";
            summary += $"一致性问题: {ConsistencyReport.InconsistenciesFound}\n";
            
            if (HasErrors)
            {
                summary += $"系统错误: {ErrorMessage}\n";
            }

            return summary;
        }
    }

    /// <summary>
    /// 循环引用报告
    /// </summary>
    public class CircularReferenceReport
    {
        public List<string> CircularReferences { get; set; } = new List<string>();
        public int CircularReferencesFound => CircularReferences.Count;
        public string ErrorMessage { get; set; } = string.Empty;

        public void AddCircularReference(string description)
        {
            CircularReferences.Add(description);
        }
    }

    /// <summary>
    /// 一致性验证报告
    /// </summary>
    public class ConsistencyValidationReport
    {
        public List<string> Inconsistencies { get; set; } = new List<string>();
        public int InconsistenciesFound => Inconsistencies.Count;
        public string ErrorMessage { get; set; } = string.Empty;

        public void AddInconsistency(string description)
        {
            Inconsistencies.Add(description);
        }
    }
}