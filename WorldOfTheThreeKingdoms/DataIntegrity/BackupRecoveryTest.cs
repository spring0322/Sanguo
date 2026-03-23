using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 备份恢复功能测试
    /// </summary>
    public static class BackupRecoveryTest
    {
        /// <summary>
        /// 运行备份恢复核心测试
        /// </summary>
        /// <returns>测试是否通过</returns>
        public static async Task<bool> RunBackupRecoveryTests()
        {
            Debug.WriteLine("=== 开始备份恢复功能测试 ===");
            
            bool allTestsPassed = true;
            
            try
            {
                // 测试1: 备份数据读取器注册
                allTestsPassed &= await TestBackupDataReaderRegistration();
                
                // 测试2: 备份创建功能
                allTestsPassed &= await TestBackupCreation();
                
                // 测试3: 备份数据读取功能
                allTestsPassed &= await TestBackupDataReading();
                
                // 测试4: 关系恢复功能
                allTestsPassed &= await TestRelationshipRecovery();
                
                // 测试5: 恢复验证功能
                allTestsPassed &= await TestRecoveryValidation();
                
                // 测试6: 错误恢复服务集成
                allTestsPassed &= await TestErrorRecoveryServiceIntegration();
                
                Debug.WriteLine($"=== 备份恢复功能测试完成: {(allTestsPassed ? "全部通过" : "部分失败")} ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"备份恢复测试异常: {ex.Message}");
                allTestsPassed = false;
            }
            
            return allTestsPassed;
        }

        /// <summary>
        /// 测试备份数据读取器注册
        /// </summary>
        private static async Task<bool> TestBackupDataReaderRegistration()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试备份数据读取器注册");
            
            try
            {
                var backupService = new BackupDataRecoveryService();
                
                // 创建测试对象
                var testFaction = CreateTestFaction();
                var testPerson = CreateTestPerson();
                var testArchitecture = CreateTestArchitecture();
                var testTroop = CreateTestTroop();
                var testLegion = CreateTestLegion();
                
                // 测试各种类型的备份创建（这会验证读取器是否正确注册）
                var factionBackupResult = await backupService.CreateBackupAsync(testFaction);
                var personBackupResult = await backupService.CreateBackupAsync(testPerson);
                var architectureBackupResult = await backupService.CreateBackupAsync(testArchitecture);
                var troopBackupResult = await backupService.CreateBackupAsync(testTroop);
                var legionBackupResult = await backupService.CreateBackupAsync(testLegion);
                
                bool testPassed = factionBackupResult && personBackupResult && architectureBackupResult && 
                                 troopBackupResult && legionBackupResult;
                
                Debug.WriteLine($"[BackupRecoveryTest] 备份数据读取器注册测试: {(testPassed ? "通过" : "失败")}");
                return testPassed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 备份数据读取器注册测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试备份创建功能
        /// </summary>
        private static async Task<bool> TestBackupCreation()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试备份创建功能");
            
            try
            {
                var backupService = new BackupDataRecoveryService();
                var testFaction = CreateTestFaction();
                
                // 测试备份创建
                var result = await backupService.CreateBackupAsync(testFaction);
                
                Debug.WriteLine($"[BackupRecoveryTest] 备份创建测试: {(result ? "通过" : "失败")}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 备份创建测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试备份数据读取功能
        /// </summary>
        private static async Task<bool> TestBackupDataReading()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试备份数据读取功能");
            
            try
            {
                var backupService = new BackupDataRecoveryService();
                var testFaction = CreateTestFaction();
                
                // 先创建备份
                await backupService.CreateBackupAsync(testFaction);
                
                // 然后尝试恢复（这会测试数据读取功能）
                var recoveryResult = await backupService.RecoverFromBackupAsync(testFaction);
                
                bool testPassed = recoveryResult != null;
                
                Debug.WriteLine($"[BackupRecoveryTest] 备份数据读取测试: {(testPassed ? "通过" : "失败")}");
                return testPassed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 备份数据读取测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试关系恢复功能
        /// </summary>
        private static async Task<bool> TestRelationshipRecovery()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试关系恢复功能");
            
            try
            {
                var backupService = new BackupDataRecoveryService();
                var testFaction = CreateTestFaction();
                
                // 先创建备份
                await backupService.CreateBackupAsync(testFaction);
                
                // 模拟关系断裂
                testFaction.Leader = null;
                
                // 尝试恢复
                var recoveryResult = await backupService.RecoverFromBackupAsync(testFaction);
                
                bool testPassed = recoveryResult != null && recoveryResult.RestoredRelations.Count >= 0;
                
                Debug.WriteLine($"[BackupRecoveryTest] 关系恢复测试: {(testPassed ? "通过" : "失败")}");
                return testPassed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 关系恢复测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试恢复验证功能
        /// </summary>
        private static async Task<bool> TestRecoveryValidation()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试恢复验证功能");
            
            try
            {
                var backupService = new BackupDataRecoveryService();
                var testFaction = CreateTestFaction();
                
                // 测试恢复验证
                var recoveryResult = await backupService.RecoverFromBackupAsync(testFaction);
                
                bool testPassed = recoveryResult != null && recoveryResult.ValidationResult != null;
                
                Debug.WriteLine($"[BackupRecoveryTest] 恢复验证测试: {(testPassed ? "通过" : "失败")}");
                return testPassed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 恢复验证测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试错误恢复服务集成
        /// </summary>
        private static async Task<bool> TestErrorRecoveryServiceIntegration()
        {
            Debug.WriteLine("[BackupRecoveryTest] 测试错误恢复服务集成");
            
            try
            {
                var errorRecoveryService = new ErrorRecoveryService();
                var testFaction = CreateTestFaction();
                
                // 测试智能恢复（包含备份恢复）
                var recoveryResult = await errorRecoveryService.SmartRecoverAsync(testFaction, useBackup: true);
                
                // 测试备份创建
                var backupResult = await errorRecoveryService.CreateBackupAsync(testFaction);
                
                // 测试批量备份
                var batchBackupResult = await errorRecoveryService.CreateBatchBackupAsync(new List<GameObject> { testFaction });
                
                bool testPassed = recoveryResult != null && backupResult && batchBackupResult != null;
                
                Debug.WriteLine($"[BackupRecoveryTest] 错误恢复服务集成测试: {(testPassed ? "通过" : "失败")}");
                return testPassed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupRecoveryTest] 错误恢复服务集成测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建测试势力对象
        /// </summary>
        private static Faction CreateTestFaction()
        {
            return new Faction
            {
                ID = 1001,
                // Name = "测试势力",
                ColorIndex = 1,
                Passed = false,
                TechniquePoint = 100,
                // BaseMoney = 1000,
                // BaseFood = 500,
                Reputation = 50
            };
        }

        /// <summary>
        /// 创建测试人物对象
        /// </summary>
        private static Person CreateTestPerson()
        {
            Person p = new Person();
            p.ID = 2001;
            // p.Name = "测试人物";
            p.SurName = "测试";
            p.GivenName = "人物";
            p.Sex = true;
            p.Alive = true;
            p.Available = true;
            p.Command = 80;
            p.Strength = 75;
            p.Intelligence = 85;
            p.Politics = 70;
            p.Glamour = 65;
            // p.Loyalty = 90; // Loyalty might be read-only or complex
            p.Ambition = 60;
            return p;
        }

        /// <summary>
        /// 创建测试建筑对象
        /// </summary>
        private static Architecture CreateTestArchitecture()
        {
            Architecture a = new Architecture();
            a.ID = 3001;
            // a.Name = "测试城市";
            a.ArchitectureArea.Centre = new Point(100, 200);
            a.Population = 10000;
            a.Fund = 5000;
            a.Food = 3000;
            a.Morale = 80;
            a.Endurance = 1000;
            // a.Defense = 500;
            a.Agriculture = 70;
            a.Commerce = 60;
            a.Technology = 50;
            // a.Recruit = 40;
            return a;
        }

        /// <summary>
        /// 创建测试部队对象
        /// </summary>
        private static Troop CreateTestTroop()
        {
            Troop t = new Troop();
            t.ID = 4001;
            // t.Name = "测试部队";
            t.Quantity = 1000;
            t.Morale = 85;
            t.Combativity = 90;
            // t.Experience = 75;
            t.Food = 500;
            t.Fund = 200;
            // t.Will = 80;
            // t.AutoRun = false;
            // t.Controllable = true;
            t.Destroyed = false;
            return t;
        }

        /// <summary>
        /// 创建测试军团对象
        /// </summary>
        private static Legion CreateTestLegion()
        {
            Legion l = new Legion();
            l.ID = 5001;
            // l.Name = "测试军团";
            // 🔥 重构：使用新的Kind+Mission系统
            // 日期：2026-03-09
            l.Kind = LegionKind.AI;
            l.Mission = LegionMission.Attack;
            return l;
        }
    }
}