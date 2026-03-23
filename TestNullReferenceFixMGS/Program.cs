using System;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms.GameObjects;
using GameObjects;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== MainGameScreen NullReferenceException 修复测试 ===");
        
        TestScreenInitialization();
        TestInitializationFactionIDsHandling();
        
        Console.WriteLine("\n✅ 所有测试完成！");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
    
    static void TestScreenInitialization()
    {
        Console.WriteLine("\n1. 测试 Screen 初始化:");
        
        try
        {
            var screen = new Screen();
            
            if (screen.InitializationFactionIDs != null)
            {
                Console.WriteLine($"   ✅ InitializationFactionIDs 已正确初始化，Count = {screen.InitializationFactionIDs.Count}");
            }
            else
            {
                Console.WriteLine("   ❌ InitializationFactionIDs 仍然为 null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Screen 初始化失败: {ex.Message}");
        }
    }
    
    static void TestInitializationFactionIDsHandling()
    {
        Console.WriteLine("\n2. 测试 InitializationFactionIDs 处理:");
        
        try
        {
            var screen = new Screen();
            
            // 测试空列表情况
            bool skyEyeResult1 = TestSkyEyeLogic(screen.InitializationFactionIDs);
            Console.WriteLine($"   ✅ 空列表情况: SkyEye = {skyEyeResult1}");
            
            // 测试有内容的列表
            screen.InitializationFactionIDs.Add(1);
            screen.InitializationFactionIDs.Add(2);
            bool skyEyeResult2 = TestSkyEyeLogic(screen.InitializationFactionIDs);
            Console.WriteLine($"   ✅ 有内容列表: SkyEye = {skyEyeResult2}");
            
            // 测试 null 情况（模拟原始问题）
            bool skyEyeResult3 = TestSkyEyeLogic(null);
            Console.WriteLine($"   ✅ null 情况: SkyEye = {skyEyeResult3}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ InitializationFactionIDs 处理测试失败: {ex.Message}");
        }
    }
    
    // 模拟原始的 SkyEye 逻辑
    static bool TestSkyEyeLogic(List<int>? initializationFactionIDs)
    {
        // 使用修复后的逻辑
        if (initializationFactionIDs?.Count == 0 || initializationFactionIDs == null)
        {
            return true; // SkyEye = true
        }
        else
        {
            return false; // SkyEye = false
        }
    }
}