using System;
using System.Collections.Generic;

// 模拟相关类型
public class GameDate
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
    public Point(int x, int y) { X = x; Y = y; }
}

public class TestLoadFromString
{
    // 复制Form1中的LoadFromString方法
    public static void LoadFromString(GameDate gameDate, string gamedatestring)
    {
        char[] separator = new char[] { ' ', '年', '月', '日', '\n', '\r', '\t' };
        string[] strArray = gamedatestring.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (strArray.Length == 3)
        {
            gameDate.Year = int.Parse(strArray[0]);
            gameDate.Month = int.Parse(strArray[1]);
            gameDate.Day = int.Parse(strArray[2]);
        }
    }

    public static Point LoadFromString(Point p, string pointstring)
    {
        char[] separator = new char[] { ' ', '{', '}', 'X', 'Y', ':', '\n', '\r', '\t' };
        string[] strArray = pointstring.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (strArray.Length == 2)
        {
            return new Point(int.Parse(strArray[0]), int.Parse(strArray[1]));
        }
        return new Point();
    }

    public static List<Point> LoadFromString(List<Point> pointList, string pointstring)
    {
        char[] separator = new char[] { ' ', ',', '{', '}', 'X', 'Y', ':', '\n', '\r', '\t' };
        string[] strArray = pointstring.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        pointList = new List<Point>();
        for (int i = 0; i < strArray.Length; i += 2)
        {
            pointList.Add(new Point(int.Parse(strArray[i]), int.Parse(strArray[i + 1])));
        }
        return pointList;
    }

    public static int[] LoadFromString(int[] aaa, string 文本)
    {
        char[] separator = new char[] { ' ', '{', '}', ',', '\n', '\r', '\t' };
        string[] strArray = 文本.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        aaa = new int[strArray.Length];
        for (int i = 0; i < strArray.Length; i++)
        {
            aaa[i] = int.Parse(strArray[i]);
        }
        return aaa;
    }

    public static List<int> LoadFromString(List<int> aaa, string 文本)
    {
        char[] separator = new char[] { ' ', '{', '}', ',', '\n', '\r', '\t' };
        string[] strArray = 文本.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        aaa = new List<int>();
        for (int i = 0; i < strArray.Length; i++)
        {
            aaa.Add(int.Parse(strArray[i]));
        }
        return aaa;
    }

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

    public static void TestAllMethods()
    {
        // 测试所有可能的调用模式
        
        // 1. string[] = LoadFromString(string) - 应该工作
        string[] result1 = TestLoadFromString.LoadFromString("test string");
        
        // 2. int[] = LoadFromString(int[], string) - 应该工作
        int[] aaa = new int[] { };
        aaa = TestLoadFromString.LoadFromString(aaa, "1 2 3");
        
        // 3. Point = LoadFromString(Point, string) - 应该工作
        Point p = new Point();
        Point result3 = TestLoadFromString.LoadFromString(p, "10 20");
        
        // 4. void LoadFromString(GameDate, string) - 应该工作
        GameDate date = new GameDate();
        TestLoadFromString.LoadFromString(date, "2023 12 15");
        
        Console.WriteLine("All tests passed!");
    }
}