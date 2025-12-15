using System;

public class DebugTest
{
    // 测试可能有问题的调用模式
    public static string[] LoadFromString(string text)
    {
        return text.Split(' ');
    }
    
    public static void TestMethod()
    {
        // 这应该工作
        string ss = "test string";
        string[] ssss = DebugTest.LoadFromString(ss);
        
        // 检查是否有任何地方试图将string传递给期望string[]的参数
        Console.WriteLine(ssss[0]);
    }
}