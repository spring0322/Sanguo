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
    /// 对象关系重建器实现
    /// </summary>
    public class ObjectRelationshipRebuilder : IObjectRelationshipRebuilder
    {
        private readonly List<IRelationshipRebuildStrategy> _strategies = new List<IRelationshipRebuildStrategy>();
        private readonly object _strategiesLock = new object();

        public ObjectRelationshipRebuilder()
        {
            // 注册默认的重建策略
            RegisterDefaultStrategies();
        }

        /// <summary>
        /// 注册默认的关系重建策略
        /// </summary>
        private void RegisterDefaultStrategies()
        {
            AddRebuildStrategy(new FactionLeaderRebuildStrategy());
            AddRebuildStrategy(new PersonIdealTendencyRebuildStrategy());
            AddRebuildStrategy(new ArchitectureBelongedFactionRebuildStrategy());
            AddRebuildStrategy(new TroopBelongedLegionRebuildStrategy());
        }

        public void AddRebuildStrategy(IRelationshipRebuildStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            lock (_strategiesLock)
            {
                // 检查是否已存在同名策略
                if (_strategies.Any(s => s.StrategyName == strategy.StrategyName))
                {
                    Debug.WriteLine($"[ObjectRelationshipRebuilder] 策略 '{strategy.StrategyName}' 已存在，将被替换");
                    RemoveRebuildStrategy(strategy.StrategyName);
                }

                _strategies.Add(strategy);
                Debug.WriteLine($"[ObjectRelationshipRebuilder] 添加重建策略: {strategy.StrategyName}");
            }
        }

        public void RemoveRebuildStrategy(string strategyName)
        {
            if (string.IsNullOrEmpty(strategyName))
                return;

            lock (_strategiesLock)
            {
                var strategy = _strategies.FirstOrDefault(s => s.StrategyName == strategyName);
                if (strategy != null)
                {
                    _strategies.Remove(strategy);
                    Debug.WriteLine($"[ObjectRelationshipRebuilder] 移除重建策略: {strategyName}");
                }
            }
        }

        public async Task<bool> RebuildRelationshipsAsync(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            var result = await RepairBrokenRelationshipsAsync(gameObject);
            return result.Success;
        }

        public async Task<bool> ValidateRelationshipsAsync(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            // 获取适用的策略
            var applicableStrategies = GetApplicableStrategies(gameObject);

            foreach (var strategy in applicableStrategies)
            {
                try
                {
                    bool isValid = await strategy.ValidateAsync(gameObject);
                    if (!isValid)
                    {
                        Debug.WriteLine($"[ObjectRelationshipRebuilder] 策略 '{strategy.StrategyName}' 验证失败: {gameObject.GetType().Name}[{gameObject.ID}]");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ObjectRelationshipRebuilder] 策略 '{strategy.StrategyName}' 验证异常: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        public async Task<RelationshipRepairResult> RepairBrokenRelationshipsAsync(GameObject gameObject)
        {
            var result = new RelationshipRepairResult
            {
                ObjectType = gameObject?.GetType().Name ?? "Unknown",
                ObjectId = gameObject?.ID ?? -1
            };

            if (gameObject == null)
            {
                result.Success = false;
                result.ErrorMessage = "游戏对象为null";
                return result;
            }

            // 获取适用的策略
            var applicableStrategies = GetApplicableStrategies(gameObject);

            Debug.WriteLine($"[ObjectRelationshipRebuilder] 修复对象 {gameObject.GetType().Name}[{gameObject.ID}]，应用 {applicableStrategies.Count} 个策略");

            bool overallSuccess = true;

            foreach (var strategy in applicableStrategies)
            {
                try
                {
                    var strategyResult = await strategy.RebuildAsync(gameObject);
                    
                    // 合并结果
                    foreach (var repaired in strategyResult.RepairedRelationships)
                    {
                        result.AddRepairedRelationship($"{strategy.StrategyName}: {repaired}");
                    }
                    
                    foreach (var failed in strategyResult.FailedRelationships)
                    {
                        result.AddFailedRelationship($"{strategy.StrategyName}: {failed}");
                    }

                    if (!strategyResult.Success)
                    {
                        overallSuccess = false;
                        if (!string.IsNullOrEmpty(strategyResult.ErrorMessage))
                        {
                            result.ErrorMessage += $"{strategy.StrategyName}: {strategyResult.ErrorMessage}; ";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ObjectRelationshipRebuilder] 策略 '{strategy.StrategyName}' 执行异常: {ex.Message}");
                    result.AddFailedRelationship($"{strategy.StrategyName}: 执行异常");
                    overallSuccess = false;
                    result.ErrorMessage += $"{strategy.StrategyName}: {ex.Message}; ";
                }
            }

            result.Success = overallSuccess;
            return result;
        }

        public async Task<RelationshipRebuildReport> RebuildAllRelationshipsAsync()
        {
            var report = new RelationshipRebuildReport();

            try
            {
                // 检查当前场景
                if (Session.Current?.Scenario == null)
                {
                    var errorResult = new RelationshipRepairResult
                    {
                        Success = false,
                        ObjectType = "Session",
                        ObjectId = -1,
                        ErrorMessage = "当前场景为null，无法执行全面重建"
                    };
                    report.AddResult(errorResult);
                    return report;
                }

                var scenario = Session.Current.Scenario;
                var allObjects = new List<GameObject>();

                // 收集所有游戏对象
                if (scenario.Persons != null)
                    allObjects.AddRange(scenario.Persons.GetList().Cast<GameObject>());
                
                if (scenario.Architectures != null)
                    allObjects.AddRange(scenario.Architectures.GetList().Cast<GameObject>());
                
                if (scenario.Factions != null)
                    allObjects.AddRange(scenario.Factions.GetList().Cast<GameObject>());
                
                if (scenario.Troops != null)
                    allObjects.AddRange(scenario.Troops.GetList().Cast<GameObject>());

                if (scenario.Legions != null)
                    allObjects.AddRange(scenario.Legions.GetList().Cast<GameObject>());

                Debug.WriteLine($"[ObjectRelationshipRebuilder] 开始全面关系重建，共 {allObjects.Count} 个对象");

                // 重建每个对象的关系
                foreach (var obj in allObjects)
                {
                    try
                    {
                        var objResult = await RepairBrokenRelationshipsAsync(obj);
                        report.AddResult(objResult);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ObjectRelationshipRebuilder] 重建对象 {obj?.GetType().Name}[{obj?.ID}] 时发生异常: {ex.Message}");
                        
                        var errorResult = new RelationshipRepairResult
                        {
                            Success = false,
                            ObjectType = obj?.GetType().Name ?? "Unknown",
                            ObjectId = obj?.ID ?? -1,
                            ErrorMessage = $"重建异常: {ex.Message}"
                        };
                        report.AddResult(errorResult);
                    }
                }

                Debug.WriteLine($"[ObjectRelationshipRebuilder] 全面关系重建完成，成功 {report.SuccessfulRebuilds}，失败 {report.FailedRebuilds}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ObjectRelationshipRebuilder] 全面关系重建发生异常: {ex.Message}");
                
                var errorResult = new RelationshipRepairResult
                {
                    Success = false,
                    ObjectType = "System",
                    ObjectId = -1,
                    ErrorMessage = $"全面重建异常: {ex.Message}"
                };
                report.AddResult(errorResult);
            }

            return report;
        }

        /// <summary>
        /// 获取适用于指定游戏对象的策略
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>适用的策略列表</returns>
        private List<IRelationshipRebuildStrategy> GetApplicableStrategies(GameObject gameObject)
        {
            lock (_strategiesLock)
            {
                return _strategies.Where(s => s.IsEnabled && s.AppliesTo(gameObject)).ToList();
            }
        }
    }
}