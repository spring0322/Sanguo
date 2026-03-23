using System;

/// <summary>
/// 验证内政增长公式的测试脚本
/// </summary>
public class Program
{
    /// <summary>
    /// 计算新的增长公式
    /// </summary>
    public static float CalculateNewFormula(int currentValue, int ceiling, int baseIncrement)
    {
        if (ceiling == 0) return 0;
        
        float progress = (float)currentValue / ceiling;
        float actualIncrement;
        
        if (baseIncrement > 0)
        {
            // 在前70%时正常增长，后30%时才开始递减
            if (progress < 0.7f)
            {
                actualIncrement = baseIncrement; // 前70%全速增长
            }
            else
            {
                // 后30%时才开始递减，但保持最低30%的增长速度
                float reductionFactor = Math.Max(0.3f, 1.0f - (progress - 0.7f) / 0.3f);
                actualIncrement = baseIncrement * reductionFactor;
            }
        }
        else
        {
            actualIncrement = baseIncrement; // 负增长不受影响
        }
        
        return actualIncrement;
    }
    
    /// <summary>
    /// 计算旧的增长公式
    /// </summary>
    public static float CalculateOldFormula(int currentValue, int ceiling, int baseIncrement)
    {
        if (ceiling == 0) return 0;
        return baseIncrement > 0 ? baseIncrement * (1 - (float)currentValue / ceiling) : baseIncrement;
    }
    
    /// <summary>
    /// 主测试方法
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("内政增长公式对比验证");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        int ceiling = 100;
        int baseIncrement = 10;
        
        Console.WriteLine($"测试参数：上限={ceiling}, 基础增长={baseIncrement}");
        Console.WriteLine();
        Console.WriteLine("进度\t当前值\t旧公式\t新公式\t改进倍数");
        Console.WriteLine("----\t------\t------\t------\t--------");
        
        for (int progress = 0; progress <= 100; progress += 10)
        {
            int currentValue = progress;
            
            float oldIncrement = CalculateOldFormula(currentValue, ceiling, baseIncrement);
            float newIncrement = CalculateNewFormula(currentValue, ceiling, baseIncrement);
            
            float improvement = oldIncrement > 0 ? newIncrement / oldIncrement : 0;
            
            Console.WriteLine($"{progress}%\t{currentValue}\t{oldIncrement:F1}\t{newIncrement:F1}\t{improvement:F2}x");
        }
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("关键测试点验证");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        // 测试关键点
        int[] testPoints = { 49, 50, 69, 70, 71, 80, 90, 95, 99 };
        
        Console.WriteLine("测试点\t旧增长\t新增长\t改进");
        Console.WriteLine("------\t------\t------\t----");
        
        foreach (int point in testPoints)
        {
            float oldInc = CalculateOldFormula(point, ceiling, baseIncrement);
            float newInc = CalculateNewFormula(point, ceiling, baseIncrement);
            float improvement = oldInc > 0 ? newInc / oldInc : 0;
            
            Console.WriteLine($"{point}\t{oldInc:F1}\t{newInc:F1}\t{improvement:F1}x");
        }
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("统治度49停滞问题验证");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        // 专门测试统治度49的情况
        int domination49 = 49;
        float old49 = CalculateOldFormula(domination49, ceiling, baseIncrement);
        float new49 = CalculateNewFormula(domination49, ceiling, baseIncrement);
        
        Console.WriteLine($"统治度49时：");
        Console.WriteLine($"- 旧公式增长：{old49:F1} (增长率 {old49/baseIncrement:P1})");
        Console.WriteLine($"- 新公式增长：{new49:F1} (增长率 {new49/baseIncrement:P1})");
        Console.WriteLine($"- 改进倍数：{new49/old49:F1}x");
        Console.WriteLine($"- 问题解决：{(new49 > old49 * 1.5 ? "是" : "否")}");
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("公式分界点验证 (70%边界)");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        for (int i = 68; i <= 72; i++)
        {
            float newInc = CalculateNewFormula(i, ceiling, baseIncrement);
            float rate = newInc / baseIncrement;
            string phase = i < 70 ? "前70%" : "后30%";
            
            Console.WriteLine($"值{i}: {newInc:F1} ({rate:P1}) - {phase}");
        }
        
        Console.WriteLine();
        Console.WriteLine("验证完成！按任意键退出...");
        Console.ReadKey();
    }
}
