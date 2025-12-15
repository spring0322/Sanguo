using System;

public class TestSyntax
{
    public static string[] LoadFromString(string 文本)
    {
        char[] separator = new char[] { ' ', '{', '}', ',', '\n', '\r', '\t' };
        string[] strArray = 文本.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (strArray.Length > 0)
        {
            return strArray;
        }
        else return new string[] { };
    }
    
    public static void TestMethod()
    {
        string ss = "test string";
        string[] ssss = TestSyntax.LoadFromString(ss);  // 这应该工作
        Console.WriteLine(ssss[0]);
    }
}