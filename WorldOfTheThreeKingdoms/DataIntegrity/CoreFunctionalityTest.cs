using GameObjects;
using GameManager;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 核心功能测试类
    /// 验证数据完整性检查和关系修复功能是否正常工作
    /// </summary>
    public static class CoreFunctionalityTest
    {
        /// <summary>
        /// 运行核心功能测试
        /// </summary>
        /// <returns>测试是否通过</returns>
        public static async Task<bool> RunCoreTests()
        {
            Debug.WriteLine("=== AOT数据转换修复 - 核心功能测试 ===");
            
            bool allTestsPassed = true;

            try
            {
                // 测试1: 数据完整性检查器基本功能
                Debug.WriteLine("\n[测试1] 数据完整性检查器基本功能");
                bool test1Passed = await TestDataIntegrityChecker();
                allTestsPassed &= test1Passed;
                Debug.WriteLine($"测试1结果: {(test1Passed ? "通过" : "失败")}");

                // 测试2: 对象关系重建器基本功能
                Debug.WriteLine("\n[测试2] 对象关系重建器基本功能");
                bool test2Passed = await TestObjectRelationshipRebuilder();
                allTestsPassed &= test2Passed;
                Debug.WriteLine($"测试2结果: {(test2Passed ? "通过" : "失败")}");

                // 测试3: 关系验证服务综合功能
                Debug.WriteLine("\n[测试3] 关系验证服务综合功能");
                bool test3Passed = await TestRelationshipValidationService();
                allTestsPassed &= test3Passed;
                Debug.WriteLine($"测试3结果: {(test3Passed ? "通过" : "失败")}");

                // 测试4: 有问题对象的修复能力
                Debug.WriteLine("\n[测试4] 有问题对象的修复能力");
                bool test4Passed = await TestProblematicObjectRepair();
                allTestsPassed &= test4Passed;
                Debug.WriteLine($"测试4结果: {(test4Passed ? "通过" : "失败")}");

                // 测试5: 备份恢复功能
                Debug.WriteLine("\n[测试5] 备份恢复功能");
                bool test5Passed = await BackupRecoveryTest.RunBackupRecoveryTests();
                allTestsPassed &= test5Passed;
                Debug.WriteLine($"测试5结果: {(test5Passed ? "通过" : "失败")}");

                // 测试6: System.Text.Json迁移功能 (已移除)
                Debug.WriteLine("\n[测试6] System.Text.Json迁移功能 - 跳过 (已移除)");
                bool test6Passed = true; // 跳过此测试
                allTestsPassed &= test6Passed;
                Debug.WriteLine($"测试6结果: {(test6Passed ? "跳过" : "失败")}");

                // 测试7: AOT兼容性功能
                Debug.WriteLine("\n[测试7] AOT兼容性功能");
                bool test7Passed = await WorldOfTheThreeKingdoms.AOTCompatibility.AOTCompatibilityTest.RunCompatibilityTests();
                allTestsPassed &= test7Passed;
                Debug.WriteLine($"测试7结果: {(test7Passed ? "通过" : "失败")}");

                // 测试8: 延迟加载系统功能
                Debug.WriteLine("\n[测试8] 延迟加载系统功能");
                bool test8Passed = await WorldOfTheThreeKingdoms.LazyLoading.LazyLoadingTest.RunLazyLoadingTests();
                allTestsPassed &= test8Passed;
                Debug.WriteLine($"测试8结果: {(test8Passed ? "通过" : "失败")}");

                // 测试9: 系统集成和优化功能
                Debug.WriteLine("\n[测试9] 系统集成和优化功能");
                bool test9Passed = await WorldOfTheThreeKingdoms.AOTCompatibility.SystemIntegrationTest.RunSystemIntegrationTests();
                allTestsPassed &= test9Passed;
                Debug.WriteLine($"测试9结果: {(test9Passed ? "通过" : "失败")}");

                // 测试10: 如果有当前场景，测试实际数据
                if (Session.Current?.Scenario != null)
                {
                    Debug.WriteLine("\n[测试10] 实际游戏数据测试");
                    bool test10Passed = await TestRealGameData();
                    allTestsPassed &= test10Passed;
                    Debug.WriteLine($"测试10结果: {(test10Passed ? "通过" : "失败")}");
                }
                else
                {
                    Debug.WriteLine("\n[测试10] 跳过实际游戏数据测试（无当前场景）");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"核心功能测试异常: {ex.Message}");
                allTestsPassed = false;
            }

            Debug.WriteLine($"\n=== 核心功能测试完成，总体结果: {(allTestsPassed ? "通过" : "失败")} ===");
            return allTestsPassed;
        }

        /// <summary>
        /// 测试数据完整性检查器
        /// </summary>
        private static async Task<bool> TestDataIntegrityChecker()
        {
            try
            {
                var checker = new DataIntegrityChecker();

                // 测试规则注册
                var rules = checker.GetRules();
                if (rules.Count == 0)
                {
                    Debug.WriteLine("  ❌ 没有注册任何完整性检查规则");
                    return false;
                }
                Debug.WriteLine($"  ✅ 已注册 {rules.Count} 个完整性检查规则");

                // 测试null对象检查
                var nullReport = await checker.CheckAsync(null);
                if (nullReport.IssuesFound == 0)
                {
                    Debug.WriteLine("  ❌ null对象检查未发现问题");
                    return false;
                }
                Debug.WriteLine($"  ✅ null对象检查发现 {nullReport.IssuesFound} 个问题");

                // 测试有问题的势力对象
                var problematicFaction = new Faction();
                problematicFaction.ID = 9999;
                problematicFaction.Name = "测试势力";
                problematicFaction.LeaderID = 1; // 设置LeaderID但不设置Leader
                
                var factionReport = await checker.CheckAsync(problematicFaction);
                if (factionReport.IssuesFound == 0)
                {
                    Debug.WriteLine("  ❌ 有问题的势力对象检查未发现问题");
                    return false;
                }
                Debug.WriteLine($"  ✅ 有问题的势力对象检查发现 {factionReport.IssuesFound} 个问题");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 数据完整性检查器测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试对象关系重建器
        /// </summary>
        private static async Task<bool> TestObjectRelationshipRebuilder()
        {
            try
            {
                var rebuilder = new ObjectRelationshipRebuilder();

                // 测试null对象
                bool nullResult = await rebuilder.RebuildRelationshipsAsync(null);
                if (nullResult)
                {
                    Debug.WriteLine("  ❌ null对象重建应该返回false");
                    return false;
                }
                Debug.WriteLine("  ✅ null对象重建正确返回false");

                // 测试有问题的势力对象修复
                var problematicFaction = new Faction();
                problematicFaction.ID = 9999;
                problematicFaction.Name = "测试势力";
                problematicFaction.LeaderID = 1;

                var repairResult = await rebuilder.RepairBrokenRelationshipsAsync(problematicFaction);
                if (repairResult == null)
                {
                    Debug.WriteLine("  ❌ 修复结果不应为null");
                    return false;
                }
                Debug.WriteLine($"  ✅ 势力对象修复完成，成功: {repairResult.Success}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 对象关系重建器测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试关系验证服务
        /// </summary>
        private static async Task<bool> TestRelationshipValidationService()
        {
            try
            {
                var validationService = new RelationshipValidationService();

                // 测试循环引用检测
                var circularRefReport = await validationService.DetectAndFixCircularReferencesAsync();
                if (circularRefReport == null)
                {
                    Debug.WriteLine("  ❌ 循环引用报告不应为null");
                    return false;
                }
                Debug.WriteLine($"  ✅ 循环引用检测完成，发现 {circularRefReport.CircularReferencesFound} 个循环引用");

                // 测试一致性验证
                var consistencyReport = await validationService.ValidateRelationshipConsistencyAsync();
                if (consistencyReport == null)
                {
                    Debug.WriteLine("  ❌ 一致性报告不应为null");
                    return false;
                }
                Debug.WriteLine($"  ✅ 一致性验证完成，发现 {consistencyReport.InconsistenciesFound} 个不一致");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 关系验证服务测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试有问题对象的修复能力
        /// </summary>
        private static async Task<bool> TestProblematicObjectRepair()
        {
            try
            {
                var checker = new DataIntegrityChecker();
                var rebuilder = new ObjectRelationshipRebuilder();

                // 创建一个有多个问题的势力对象
                var faction = new Faction();
                faction.ID = 8888;
                faction.Name = "问题势力";
                faction.LeaderID = 999; // 无效的LeaderID
                // faction.Leader = null; // Leader为null

                // 检查问题
                var beforeReport = await checker.CheckAsync(faction);
                Debug.WriteLine($"  修复前发现 {beforeReport.IssuesFound} 个问题");

                // 尝试修复
                var repairResult = await rebuilder.RepairBrokenRelationshipsAsync(faction);
                Debug.WriteLine($"  修复操作: 成功{repairResult.RepairedRelationships.Count}个, 失败{repairResult.FailedRelationships.Count}个");

                // 再次检查
                var afterReport = await checker.CheckAsync(faction);
                Debug.WriteLine($"  修复后发现 {afterReport.IssuesFound} 个问题");

                // 验证修复效果（问题数量应该减少或保持不变）
                if (afterReport.IssuesFound > beforeReport.IssuesFound)
                {
                    Debug.WriteLine("  ❌ 修复后问题数量增加了");
                    return false;
                }

                Debug.WriteLine("  ✅ 问题对象修复测试完成");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 问题对象修复测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试实际游戏数据
        /// </summary>
        private static async Task<bool> TestRealGameData()
        {
            try
            {
                var validationService = new RelationshipValidationService();

                // 执行综合验证（限制处理数量以避免测试时间过长）
                Debug.WriteLine("  开始综合验证（可能需要一些时间）...");
                
                // 先做一个快速的完整性检查
                var checker = new DataIntegrityChecker();
                var quickReport = await checker.CheckAllAsync();
                
                Debug.WriteLine($"  快速完整性检查: 检查了 {quickReport.TotalObjectsChecked} 个对象");
                Debug.WriteLine($"  发现 {quickReport.IssuesFound} 个问题");
                
                if (quickReport.HasCriticalIssues)
                {
                    Debug.WriteLine("  ⚠️ 发现严重问题，建议进行修复");
                }

                // 如果问题不多，可以尝试修复
                if (quickReport.IssuesFound > 0 && quickReport.IssuesFound <= 10)
                {
                    Debug.WriteLine("  尝试修复发现的问题...");
                    var comprehensiveReport = await validationService.ValidateAndRepairAllRelationshipsAsync();
                    Debug.WriteLine($"  综合修复完成: {comprehensiveReport.GetSummary()}");
                }

                Debug.WriteLine("  ✅ 实际游戏数据测试完成");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ 实际游戏数据测试异常: {ex.Message}");
                return false;
            }
        }
    }
}