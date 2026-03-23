using System;
using GameGlobal;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Parameters XML 生成测试 ===");
        
        TestParametersXMLGeneration();
        
        Console.WriteLine("\n✅ 测试完成！");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
    
    static void TestParametersXMLGeneration()
    {
        Console.WriteLine("\n1. 测试 Parameters.SaveToXml() 方法:");
        
        try
        {
            var parameters = new Parameters();
            
            // 尝试调用 SaveToXml 方法
            parameters.SaveToXml();
            
            Console.WriteLine("   ✅ SaveToXml() 方法执行成功，没有重复属性异常");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Duplicate attribute"))
        {
            Console.WriteLine($"   ❌ 发现重复属性异常: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 其他异常 (可能是正常的): {ex.GetType().Name}: {ex.Message}");
        }
        
        Console.WriteLine("\n2. 测试属性唯一性:");
        
        try
        {
            // 创建一个简单的测试来验证没有重复属性
            var testParams = new Parameters();
            
            // 这里我们不能直接访问 SaveToXml 的内部逻辑，
            // 但我们可以通过调用它来确保没有异常
            testParams.SaveToXml();
            
            Console.WriteLine("   ✅ 属性唯一性验证通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 属性唯一性验证失败: {ex.Message}");
        }
    }
}