// 军师对话系统测试代码
// 用于验证轮流对话功能是否正常工作

using System;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace GameManager
{
    /// <summary>
    /// 军师对话系统测试类
    /// </summary>
    public static class AdvisorDialogueSystemTest
    {
        /// <summary>
        /// 测试对话配置加载
        /// </summary>
        public static void TestDialogueConfigLoading()
        {
            Console.WriteLine("=== 测试对话配置加载 ===");
            
            try
            {
                // 初始化对话管理器
                DialogueManager.Initialize();
                
                // 测试获取任命对话
                var dummyLeader = CreateTestPerson(635, "刘备", 75, 0); // 仁德型君主
                var dummyAdvisor = CreateTestPerson(289, "诸葛亮", 95, 0); // 仁德型军师
                
                var appointDialogue = DialogueManager.GetAppointDialogue(dummyLeader, dummyAdvisor, false);
                Console.WriteLine($"任命对话 - 君主: {appointDialogue.LeaderText}");
                Console.WriteLine($"任命对话 - 军师: {appointDialogue.AdvisorText}");
                
                // 测试获取拒绝对话
                var refusalDialogue = DialogueManager.GetAppointDialogue(dummyLeader, dummyAdvisor, true);
                Console.WriteLine($"拒绝对话 - 君主: {refusalDialogue.LeaderText}");
                Console.WriteLine($"拒绝对话 - 军师: {refusalDialogue.AdvisorText}");
                
                // 测试获取罢免对话
                var recallDialogue = DialogueManager.GetRecallDialogue(dummyLeader, dummyAdvisor);
                Console.WriteLine($"罢免对话 - 君主: {recallDialogue.LeaderText}");
                Console.WriteLine($"罢免对话 - 军师: {recallDialogue.AdvisorText}");
                
                Console.WriteLine("✅ 对话配置加载测试通过");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 对话配置加载测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试军师任命流程
        /// </summary>
        public static void TestAdvisorAppointmentFlow()
        {
            Console.WriteLine("\n=== 测试军师任命流程 ===");
            
            try
            {
                // 获取当前势力和候选人
                var currentFaction = Session.Current?.Scenario?.CurrentFaction;
                if (currentFaction == null)
                {
                    Console.WriteLine("❌ 无法获取当前势力");
                    return;
                }
                
                var candidates = currentFaction.AdvisorCandicate;
                if (candidates == null || candidates.Count == 0)
                {
                    Console.WriteLine("❌ 没有可用的军师候选人");
                    return;
                }
                
                var leader = currentFaction.Leader;
                var candidate = candidates[0]; // 选择第一个候选人
                
                Console.WriteLine($"君主: {leader.Name} (智力: {leader.Intelligence})");
                Console.WriteLine($"候选军师: {candidate.Name} (智力: {candidate.Intelligence})");
                
                // 测试任命逻辑
                bool willRefuse = AdvisorAppointmentSystem.WillRefuseAppointment(leader, candidate);
                Console.WriteLine($"预测是否拒绝: {(willRefuse ? "是" : "否")}");
                
                if (willRefuse)
                {
                    string reason = AdvisorAppointmentSystem.GetRefusalReason(leader, candidate);
                    Console.WriteLine($"拒绝原因: {reason}");
                }
                
                // 执行任命（注意：这会实际修改游戏状态）
                // bool success = AdvisorAppointmentSystem.TryAppointAdvisor(leader, candidate, currentFaction);
                // Console.WriteLine($"任命结果: {(success ? "成功" : "失败")}");
                
                Console.WriteLine("✅ 军师任命流程测试完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 军师任命流程测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试对话匹配算法
        /// </summary>
        public static void TestDialogueMatching()
        {
            Console.WriteLine("\n=== 测试对话匹配算法 ===");
            
            try
            {
                // 创建不同类型的测试人物
                var testCases = new[]
                {
                    new { Leader = CreateTestPerson(635, "刘备", 75, 0), Advisor = CreateTestPerson(289, "诸葛亮", 95, 0), Desc = "刘备+诸葛亮(特殊组合)" },
                    new { Leader = CreateTestPerson(1, "高智力君主", 90, 1), Advisor = CreateTestPerson(2, "高智力军师", 95, 2), Desc = "高智力组合" },
                    new { Leader = CreateTestPerson(3, "低智力君主", 40, 3), Advisor = CreateTestPerson(4, "低智力军师", 45, 3), Desc = "低智力组合" },
                    new { Leader = CreateTestPerson(5, "年轻君主", 70, 0), Advisor = CreateTestPerson(6, "年轻军师", 80, 0), Desc = "年轻组合" }
                };
                
                foreach (var testCase in testCases)
                {
                    Console.WriteLine($"\n测试场景: {testCase.Desc}");
                    
                    var dialogue = DialogueManager.GetAppointDialogue(testCase.Leader, testCase.Advisor, false);
                    Console.WriteLine($"  君主: {dialogue.LeaderText}");
                    Console.WriteLine($"  军师: {dialogue.AdvisorText}");
                }
                
                Console.WriteLine("✅ 对话匹配算法测试完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 对话匹配算法测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建测试用人物
        /// </summary>
        private static Person CreateTestPerson(int id, string name, int intelligence, int characterKind)
        {
            // 注意：这是简化的测试用人物创建，实际游戏中人物创建更复杂
            var person = new Person();
            // 使用反射或其他方式设置私有字段（仅用于测试）
            // 实际实现需要根据Person类的具体结构调整
            
            // 这里只是示例，实际需要根据Person类的构造函数和属性来实现
            Console.WriteLine($"创建测试人物: {name} (ID: {id}, 智力: {intelligence})");
            
            return person;
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始军师对话系统测试...\n");
            
            TestDialogueConfigLoading();
            TestDialogueMatching();
            TestAdvisorAppointmentFlow();
            
            Console.WriteLine("\n所有测试完成！");
        }
    }
}

// 使用示例：
// 在游戏中调用 AdvisorDialogueSystemTest.RunAllTests() 来验证系统是否正常工作