using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 错误恢复服务
    /// 提供智能数据修复功能
    /// </summary>
    public class ErrorRecoveryService
    {
        private readonly IDataIntegrityChecker _integrityChecker;
        private readonly IObjectRelationshipRebuilder _relationshipRebuilder;
        private readonly BackupDataRecoveryService _backupRecoveryService;
        private readonly Dictionary<string, IRecoveryStrategy> _recoveryStrategies;

        public ErrorRecoveryService(string backupDirectory = null)
        {
            _integrityChecker = new DataIntegrityChecker();
            _relationshipRebuilder = new ObjectRelationshipRebuilder();
            _backupRecoveryService = new BackupDataRecoveryService(backupDirectory);
            _recoveryStrategies = new Dictionary<string, IRecoveryStrategy>();
            
            RegisterDefaultRecoveryStrategies();
        }

        /// <summary>
        /// 注册默认的恢复策略
        /// </summary>
        private void RegisterDefaultRecoveryStrategies()
        {
            _recoveryStrategies["NullReference"] = new NullReferenceRecoveryStrategy();
            _recoveryStrategies["MissingRelation"] = new MissingRelationRecoveryStrategy();
            _recoveryStrategies["InvalidData"] = new InvalidDataRecoveryStrategy();
            _recoveryStrategies["InvalidRelation"] = new InvalidRelationRecoveryStrategy();
        }

        /// <summary>
        /// 尝试恢复完整性问题
        /// </summary>
        /// <param name="issue">完整性问题</param>
        /// <returns>恢复是否成功</returns>
        public async Task<bool> TryRecoverAsync(IntegrityIssue issue)
        {
            Debug.WriteLine($"[ErrorRecoveryService] 尝试恢复问题: {issue.IssueType} - {issue.Description}");

            if (_recoveryStrategies.TryGetValue(issue.IssueType, out var strategy))
            {
                try
                {
                    var result = await strategy.RecoverAsync(issue);
                    
                    if (result.Success)
                    {
                        Debug.WriteLine($"[ErrorRecoveryService] 成功恢复问题: {issue.IssueType}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"[ErrorRecoveryService] 恢复失败: {result.ErrorMessage}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 恢复异常: {ex.Message}");
                    return false;
                }
            }

            Debug.WriteLine($"[ErrorRecoveryService] 没有找到适用的恢复策略: {issue.IssueType}");
            return false;
        }

        /// <summary>
        /// 批量恢复完整性问题
        /// </summary>
        /// <param name="issues">问题列表</param>
        /// <returns>恢复结果报告</returns>
        public async Task<RecoveryReport> RecoverAllAsync(List<IntegrityIssue> issues)
        {
            var report = new RecoveryReport();
            
            Debug.WriteLine($"[ErrorRecoveryService] 开始批量恢复，共 {issues.Count} 个问题");

            foreach (var issue in issues)
            {
                try
                {
                    var success = await TryRecoverAsync(issue);
                    
                    if (success)
                    {
                        report.SuccessfulRecoveries.Add(issue);
                    }
                    else
                    {
                        report.FailedRecoveries.Add(issue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 恢复问题时发生异常: {ex.Message}");
                    report.FailedRecoveries.Add(issue);
                }
            }

            Debug.WriteLine($"[ErrorRecoveryService] 批量恢复完成: 成功 {report.SuccessfulRecoveries.Count}，失败 {report.FailedRecoveries.Count}");
            
            return report;
        }

        /// <summary>
        /// 智能恢复游戏对象
        /// </summary>
        /// <param name="gameObject">要恢复的游戏对象</param>
        /// <param name="useBackup">是否尝试使用备份数据恢复</param>
        /// <returns>恢复结果</returns>
        public async Task<ObjectRecoveryResult> SmartRecoverAsync(GameObject gameObject, bool useBackup = true)
        {
            var result = new ObjectRecoveryResult
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

            try
            {
                // 1. 检查完整性问题
                var integrityReport = await _integrityChecker.CheckAsync(gameObject);
                
                if (integrityReport.IssuesFound == 0)
                {
                    result.Success = true;
                    result.Message = "对象完整性良好，无需恢复";
                    return result;
                }

                // 2. 尝试关系重建
                var relationshipResult = await _relationshipRebuilder.RepairBrokenRelationshipsAsync(gameObject);
                
                // 3. 如果启用备份恢复且关系重建不完全成功，尝试从备份恢复
                if (useBackup && integrityReport.HasCriticalIssues)
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 尝试从备份恢复对象: {result.ObjectType}[{result.ObjectId}]");
                    var backupResult = await _backupRecoveryService.RecoverFromBackupAsync(gameObject);
                    
                    if (backupResult.Success)
                    {
                        Debug.WriteLine($"[ErrorRecoveryService] 备份恢复成功，恢复了 {backupResult.RestoredRelations.Count} 个关系");
                    }
                }

                // 4. 尝试恢复剩余问题
                var recoveryReport = await RecoverAllAsync(integrityReport.Issues);
                
                // 5. 再次检查完整性
                var finalReport = await _integrityChecker.CheckAsync(gameObject);
                
                result.Success = finalReport.IssuesFound < integrityReport.IssuesFound;
                result.InitialIssues = integrityReport.IssuesFound;
                result.RemainingIssues = finalReport.IssuesFound;
                result.ResolvedIssues = result.InitialIssues - result.RemainingIssues;
                result.Message = $"恢复完成：解决了 {result.ResolvedIssues} 个问题，剩余 {result.RemainingIssues} 个问题";

                Debug.WriteLine($"[ErrorRecoveryService] 智能恢复完成: {result.Message}");
                
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"智能恢复异常: {ex.Message}";
                Debug.WriteLine($"[ErrorRecoveryService] 智能恢复异常: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 添加自定义恢复策略
        /// </summary>
        /// <param name="issueType">问题类型</param>
        /// <param name="strategy">恢复策略</param>
        public void AddRecoveryStrategy(string issueType, IRecoveryStrategy strategy)
        {
            _recoveryStrategies[issueType] = strategy;
            Debug.WriteLine($"[ErrorRecoveryService] 添加恢复策略: {issueType}");
        }

        /// <summary>
        /// 移除恢复策略
        /// </summary>
        /// <param name="issueType">问题类型</param>
        public void RemoveRecoveryStrategy(string issueType)
        {
            if (_recoveryStrategies.Remove(issueType))
            {
                Debug.WriteLine($"[ErrorRecoveryService] 移除恢复策略: {issueType}");
            }
        }

        /// <summary>
        /// 获取所有已注册的恢复策略
        /// </summary>
        /// <returns>策略类型列表</returns>
        public List<string> GetRegisteredStrategies()
        {
            return _recoveryStrategies.Keys.ToList();
        }

        /// <summary>
        /// 创建对象备份
        /// </summary>
        /// <param name="gameObject">要备份的对象</param>
        /// <returns>备份是否成功</returns>
        public async Task<bool> CreateBackupAsync(GameObject gameObject)
        {
            try
            {
                var success = await _backupRecoveryService.CreateBackupAsync(gameObject);
                
                if (success)
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 成功创建备份: {gameObject.GetType().Name}[{gameObject.ID}]");
                }
                else
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 创建备份失败: {gameObject.GetType().Name}[{gameObject.ID}]");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ErrorRecoveryService] 创建备份异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从备份恢复对象
        /// </summary>
        /// <param name="gameObject">要恢复的对象</param>
        /// <param name="backupTimestamp">备份时间戳（可选）</param>
        /// <returns>恢复结果</returns>
        public async Task<BackupRecoveryResult> RecoverFromBackupAsync(GameObject gameObject, DateTime? backupTimestamp = null)
        {
            return await _backupRecoveryService.RecoverFromBackupAsync(gameObject, backupTimestamp);
        }

        /// <summary>
        /// 批量创建备份
        /// </summary>
        /// <param name="gameObjects">要备份的对象列表</param>
        /// <returns>备份报告</returns>
        public async Task<BatchBackupReport> CreateBatchBackupAsync(List<GameObject> gameObjects)
        {
            var report = new BatchBackupReport();
            
            Debug.WriteLine($"[ErrorRecoveryService] 开始批量备份，共 {gameObjects.Count} 个对象");

            foreach (var gameObject in gameObjects)
            {
                try
                {
                    var success = await CreateBackupAsync(gameObject);
                    
                    if (success)
                    {
                        report.SuccessfulBackups.Add(gameObject);
                    }
                    else
                    {
                        report.FailedBackups.Add(gameObject);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ErrorRecoveryService] 备份对象时发生异常: {ex.Message}");
                    report.FailedBackups.Add(gameObject);
                }
            }

            Debug.WriteLine($"[ErrorRecoveryService] 批量备份完成: 成功 {report.SuccessfulBackups.Count}，失败 {report.FailedBackups.Count}");
            
            return report;
        }
    }

    /// <summary>
    /// 恢复策略接口
    /// </summary>
    public interface IRecoveryStrategy
    {
        Task<RecoveryResult> RecoverAsync(IntegrityIssue issue);
    }

    /// <summary>
    /// 恢复结果
    /// </summary>
    public class RecoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime RecoveryTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 恢复报告
    /// </summary>
    public class RecoveryReport
    {
        public List<IntegrityIssue> SuccessfulRecoveries { get; set; } = new List<IntegrityIssue>();
        public List<IntegrityIssue> FailedRecoveries { get; set; } = new List<IntegrityIssue>();
        public DateTime ReportTime { get; set; } = DateTime.Now;

        public int TotalIssues => SuccessfulRecoveries.Count + FailedRecoveries.Count;
        public double SuccessRate => TotalIssues > 0 ? (double)SuccessfulRecoveries.Count / TotalIssues : 0.0;

        public string GetSummary()
        {
            return $"恢复报告: 总问题 {TotalIssues}，成功 {SuccessfulRecoveries.Count}，失败 {FailedRecoveries.Count}，成功率 {SuccessRate:P1}";
        }
    }

    /// <summary>
    /// 对象恢复结果
    /// </summary>
    public class ObjectRecoveryResult
    {
        public bool Success { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int InitialIssues { get; set; }
        public int RemainingIssues { get; set; }
        public int ResolvedIssues { get; set; }
        public DateTime RecoveryTime { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return Success ? Message : $"恢复失败: {ErrorMessage}";
        }
    }

    /// <summary>
    /// 批量备份报告
    /// </summary>
    public class BatchBackupReport
    {
        public List<GameObject> SuccessfulBackups { get; set; } = new List<GameObject>();
        public List<GameObject> FailedBackups { get; set; } = new List<GameObject>();
        public DateTime ReportTime { get; set; } = DateTime.Now;

        public int TotalObjects => SuccessfulBackups.Count + FailedBackups.Count;
        public double SuccessRate => TotalObjects > 0 ? (double)SuccessfulBackups.Count / TotalObjects : 0.0;

        public string GetSummary()
        {
            return $"备份报告: 总对象 {TotalObjects}，成功 {SuccessfulBackups.Count}，失败 {FailedBackups.Count}，成功率 {SuccessRate:P1}";
        }
    }
}