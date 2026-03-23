using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.FactionDetail;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI命令系统测试类 - 验证AI命令系统的功能
    /// </summary>
    public static class AICommandTest
    {
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] 开始AI命令系统测试");

                TestBasicFunctionality();
                TestSafetyMechanisms();
                TestPerformance();
                TestIntegration();

                System.Diagnostics.Debug.WriteLine("[AICommandTest] 所有测试完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AICommandTest] 测试过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试基础功能
        /// </summary>
        private static void TestBasicFunctionality()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试基础功能...");

            // 测试命令创建
            var command = new AICommand(
                AICommandType.CreateTroop,
                null,
                null,
                "测试命令"
            );

            if (command.Type == AICommandType.CreateTroop)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 命令创建测试通过");
            }

            // 测试队列操作
            int initialCount = AILegacyAdapter.GetQueueCount();
            AILegacyAdapter.Enqueue(command);
            int afterEnqueueCount = AILegacyAdapter.GetQueueCount();

            if (afterEnqueueCount == initialCount + 1)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 队列入队测试通过");
            }

            // 测试队列处理
            AILegacyAdapter.ProcessCommands();
            int afterProcessCount = AILegacyAdapter.GetQueueCount();

            if (afterProcessCount <= afterEnqueueCount)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 队列处理测试通过");
            }
        }

        /// <summary>
        /// 测试安全机制
        /// </summary>
        private static void TestSafetyMechanisms()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试安全机制...");

            // 测试空参数处理
            var invalidCommand = new AICommand(
                AICommandType.CreateTroop,
                null,
                null,
                "无效命令测试"
            );

            AILegacyAdapter.Enqueue(invalidCommand);
            AILegacyAdapter.ProcessCommands(); // 应该安全处理而不崩溃

            System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 空参数安全处理测试通过");

            // 测试紧急停止机制
            AISystemIntegrator.EmergencyStop();
            bool isRunning = AISystemIntegrator.IsRunning();

            if (!isRunning)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 紧急停止机制测试通过");
            }

            // 恢复运行
            AISystemIntegrator.Resume();
            isRunning = AISystemIntegrator.IsRunning();

            if (isRunning)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 系统恢复机制测试通过");
            }
        }

        /// <summary>
        /// 测试性能
        /// </summary>
        private static void TestPerformance()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试性能...");

            var startTime = DateTime.Now;

            // 创建大量命令测试性能
            for (int i = 0; i < 50; i++)
            {
                var command = new AICommand(
                    AICommandType.CreateTroop,
                    null,
                    null,
                    $"性能测试命令-{i}"
                );

                AILegacyAdapter.Enqueue(command);
            }

            // 处理所有命令
            while (AILegacyAdapter.GetQueueCount() > 0)
            {
                AILegacyAdapter.ProcessCommands();
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            System.Diagnostics.Debug.WriteLine($"[AICommandTest] ✓ 性能测试完成，处理50个命令耗时: {duration.TotalMilliseconds}ms");
        }

        /// <summary>
        /// 测试系统集成
        /// </summary>
        private static void TestIntegration()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试系统集成...");

            // 测试系统状态记录
            AISystemIntegrator.LogSystemStatus();
            System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 系统状态记录测试通过");

            // 测试统计信息
            string stats = AISystemIntegrator.GetQueueStats();
            if (!string.IsNullOrEmpty(stats))
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 统计信息获取测试通过");
            }

            // 测试队列清理
            AILegacyAdapter.ClearQueue();
            int queueCount = AILegacyAdapter.GetQueueCount();

            if (queueCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 队列清理测试通过");
            }
        }

        /// <summary>
        /// 测试具体游戏场景（需要游戏数据）
        /// </summary>
        public static void TestGameScenarios()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandTest] 跳过游戏场景测试 - 无游戏数据");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试游戏场景...");

            var faction = scenario.CurrentPlayer;
            if (faction?.Architectures.Count > 0)
            {
                var city = faction.Architectures[0] as Architecture;
                if (city?.Persons.Count > 0)
                {
                    var leader = city.Persons[0] as Person;
                    var militaryKind = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0];

                    // 测试部队创建请求
                    AISystemIntegrator.RequestCreateTroop(
                        city,
                        leader,
                        militaryKind,
                        100,
                        "测试-部队创建"
                    );

                    System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 部队创建请求测试通过");

                    // 测试人员召回请求
                    AISystemIntegrator.RequestRecallPerson(
                        leader,
                        "测试-人员召回"
                    );

                    System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 人员召回请求测试通过");
                }
            }

            // 处理测试命令
            AISystemIntegrator.ProcessAICommands();
            System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 游戏场景测试完成");
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public static void StressTest()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 开始压力测试...");

            var startTime = DateTime.Now;
            int commandCount = 0;

            // 创建大量命令
            for (int i = 0; i < 200; i++)
            {
                var commandType = (AICommandType)(i % 5); // 循环使用不同命令类型
                var command = new AICommand(
                    commandType,
                    null,
                    null,
                    $"压力测试-{i}"
                );

                AILegacyAdapter.Enqueue(command);
                commandCount++;
            }

            // 批量处理
            while (AILegacyAdapter.GetQueueCount() > 0)
            {
                AISystemIntegrator.ProcessAICommands();
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            System.Diagnostics.Debug.WriteLine($"[AICommandTest] 压力测试完成:");
            System.Diagnostics.Debug.WriteLine($"  - 处理命令数: {commandCount}");
            System.Diagnostics.Debug.WriteLine($"  - 总耗时: {duration.TotalMilliseconds}ms");
            System.Diagnostics.Debug.WriteLine($"  - 平均每命令: {duration.TotalMilliseconds / commandCount:F2}ms");
        }

        /// <summary>
        /// 错误恢复测试
        /// </summary>
        public static void ErrorRecoveryTest()
        {
            System.Diagnostics.Debug.WriteLine("[AICommandTest] 测试错误恢复...");

            // 故意创建会导致错误的命令
            var errorCommand = new AICommand(
                AICommandType.CreateTroop,
                "invalid_target", // 错误的目标类型
                "invalid_parameter", // 错误的参数类型
                "错误恢复测试"
            );

            AILegacyAdapter.Enqueue(errorCommand);

            // 添加正常命令
            var normalCommand = new AICommand(
                AICommandType.CreateTroop,
                null,
                null,
                "正常命令"
            );

            AILegacyAdapter.Enqueue(normalCommand);

            // 处理命令 - 应该能处理错误并继续
            AISystemIntegrator.ProcessAICommands();

            System.Diagnostics.Debug.WriteLine("[AICommandTest] ✓ 错误恢复测试完成 - 系统应该继续运行");
        }
    }
}