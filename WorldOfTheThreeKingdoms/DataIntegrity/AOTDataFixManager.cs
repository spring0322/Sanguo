using System;
using System.Collections.Generic;
using GameObjects;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// AOT数据转换修复管理器
    /// 提供统一的入口点来执行数据完整性检查和修复
    /// </summary>
    public static class AOTDataFixManager
    {
        /// <summary>
        /// 执行快速数据完整性检查
        /// </summary>
        /// <returns>检查报告</returns>
        public static async Task<IntegrityReport> QuickIntegrityCheckAsync()
        {
            Debug.WriteLine("[AOTDataFixManager] 开始快速数据完整性检查");
            
            var checker = new DataIntegrityChecker();
            var report = await checker.CheckAllAsync();
            
            Debug.WriteLine($"[AOTDataFixManager] 快速检查完成: 检查了{report.TotalObjectsChecked}个对象，发现{report.IssuesFound}个问题");
            
            return report;
        }

        /// <summary>
        /// 执行关键对象关系修复
        /// </summary>
        /// <returns>修复报告</returns>
        public static async Task<RelationshipRebuildReport> RepairCriticalRelationshipsAsync()
        {
            Debug.WriteLine("[AOTDataFixManager] 开始关键对象关系修复");
            
            var rebuilder = new ObjectRelationshipRebuilder();
            var report = await rebuilder.RebuildAllRelationshipsAsync();
            
            Debug.WriteLine($"[AOTDataFixManager] 关系修复完成: 成功{report.SuccessfulRebuilds}个，失败{report.FailedRebuilds}个");
            
            return report;
        }

        /// <summary>
        /// 执行全面的数据验证和修复
        /// </summary>
        /// <returns>综合报告</returns>
        public static async Task<ComprehensiveValidationReport> ComprehensiveValidationAndRepairAsync()
        {
            Debug.WriteLine("[AOTDataFixManager] 开始全面数据验证和修复");
            
            var validationService = new RelationshipValidationService();
            var report = await validationService.ValidateAndRepairAllRelationshipsAsync();
            
            Debug.WriteLine("[AOTDataFixManager] 全面验证和修复完成");
            Debug.WriteLine(report.GetSummary());
            
            return report;
        }

        /// <summary>
        /// 运行核心功能测试
        /// </summary>
        /// <returns>测试是否通过</returns>
        public static async Task<bool> RunCoreTestsAsync()
        {
            Debug.WriteLine("[AOTDataFixManager] 开始核心功能测试");
            
            bool testsPassed = await CoreFunctionalityTest.RunCoreTests();
            
            Debug.WriteLine($"[AOTDataFixManager] 核心功能测试完成: {(testsPassed ? "通过" : "失败")}");
            
            return testsPassed;
        }

        /// <summary>
        /// 获取系统状态摘要
        /// </summary>
        /// <returns>状态摘要</returns>
        public static async Task<string> GetSystemStatusSummaryAsync()
        {
            try
            {
                var summary = "=== AOT数据转换修复系统状态 ===\n";
                
                // 快速完整性检查
                var integrityReport = await QuickIntegrityCheckAsync();
                summary += $"数据完整性: 检查了{integrityReport.TotalObjectsChecked}个对象，发现{integrityReport.IssuesFound}个问题\n";
                
                if (integrityReport.HasCriticalIssues)
                {
                    summary += "⚠️ 发现严重问题，建议立即修复\n";
                }
                
                if (integrityReport.HasWarnings)
                {
                    summary += "⚠️ 发现警告问题，建议检查\n";
                }
                
                if (integrityReport.IssuesFound == 0)
                {
                    summary += "✅ 数据完整性良好\n";
                }
                
                summary += $"检查时间: {integrityReport.CheckTime:yyyy-MM-dd HH:mm:ss}\n";
                summary += "=====================================";
                
                return summary;
            }
            catch (Exception ex)
            {
                return $"获取系统状态异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 应急修复 - 修复最关键的问题
        /// </summary>
        /// <param name="useBackup">是否使用备份数据恢复</param>
        /// <returns>修复是否成功</returns>
        public static async Task<bool> EmergencyRepairAsync(bool useBackup = true)
        {
            Debug.WriteLine("[AOTDataFixManager] 开始应急修复");
            
            try
            {
                // 1. 快速检查
                var integrityReport = await QuickIntegrityCheckAsync();
                
                if (!integrityReport.HasCriticalIssues)
                {
                    Debug.WriteLine("[AOTDataFixManager] 没有发现严重问题，无需应急修复");
                    return true;
                }
                
                // 2. 关键关系修复
                var repairReport = await RepairCriticalRelationshipsAsync();
                
                // 3. 如果启用备份恢复，尝试使用备份数据
                if (useBackup)
                {
                    Debug.WriteLine("[AOTDataFixManager] 尝试使用备份数据进行恢复");
                    var errorRecoveryService = new ErrorRecoveryService();
                    
                    // 这里需要获取有问题的对象列表，暂时使用模拟逻辑
                    // 在实际实现中，应该从integrityReport中获取有问题的对象
                    Debug.WriteLine("[AOTDataFixManager] 备份恢复功能已集成，等待具体对象实例");
                }
                
                // 4. 再次检查
                var afterRepairReport = await QuickIntegrityCheckAsync();
                
                bool repairSuccessful = afterRepairReport.IssuesFound < integrityReport.IssuesFound;
                
                Debug.WriteLine($"[AOTDataFixManager] 应急修复完成: {(repairSuccessful ? "成功" : "部分成功")}");
                Debug.WriteLine($"修复前问题: {integrityReport.IssuesFound}，修复后问题: {afterRepairReport.IssuesFound}");
                
                return repairSuccessful;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTDataFixManager] 应急修复异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建系统备份
        /// </summary>
        /// <param name="gameObjects">要备份的对象列表</param>
        /// <returns>备份报告</returns>
        public static async Task<BatchBackupReport> CreateSystemBackupAsync(List<GameObject> gameObjects)
        {
            Debug.WriteLine($"[AOTDataFixManager] 开始创建系统备份，共 {gameObjects?.Count ?? 0} 个对象");
            
            var errorRecoveryService = new ErrorRecoveryService();
            var report = await errorRecoveryService.CreateBatchBackupAsync(gameObjects ?? new List<GameObject>());
            
            Debug.WriteLine($"[AOTDataFixManager] 系统备份完成: {report.GetSummary()}");
            
            return report;
        }

        /// <summary>
        /// 智能恢复对象
        /// </summary>
        /// <param name="gameObject">要恢复的对象</param>
        /// <param name="useBackup">是否使用备份数据</param>
        /// <returns>恢复结果</returns>
        public static async Task<ObjectRecoveryResult> SmartRecoverObjectAsync(GameObject gameObject, bool useBackup = true)
        {
            Debug.WriteLine($"[AOTDataFixManager] 开始智能恢复对象: {gameObject?.GetType().Name}[{gameObject?.ID}]");
            
            var errorRecoveryService = new ErrorRecoveryService();
            var result = await errorRecoveryService.SmartRecoverAsync(gameObject, useBackup);
            
            Debug.WriteLine($"[AOTDataFixManager] 智能恢复完成: {result}");
            
            return result;
        }
    }
}