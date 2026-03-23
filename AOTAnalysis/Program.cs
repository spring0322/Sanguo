using System;
using System.IO;
using System.Linq;
using WorldOfTheThreeKingdoms.AOTCompatibility;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== SimpleSerializer System.Text.Json 迁移 - AOT 兼容性分析 ===");
        Console.WriteLine($"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        try
        {
            var analyzer = new AOTSerializationCompatibilityAnalyzer();
            Console.WriteLine("✅ AOT 兼容性分析器已创建");
            
            var report = analyzer.AnalyzeCompatibility();
            Console.WriteLine("✅ 兼容性分析完成");
            Console.WriteLine();

            Console.WriteLine("=== 分析结果概览 ===");
            Console.WriteLine($"AOT 兼容性: {(report.IsAOTCompatible ? "✅ 通过" : "❌ 未通过")}");
            Console.WriteLine($"严重问题: {report.CriticalIssues} 个");
            Console.WriteLine($"警告问题: {report.WarningIssues} 个");
            Console.WriteLine($"信息问题: {report.InfoIssues} 个");
            Console.WriteLine($"总问题数: {report.Issues.Count} 个");
            Console.WriteLine();

            if (report.Issues.Count > 0)
            {
                Console.WriteLine("=== 详细问题列表 ===");
                int count = 0;
                foreach (var issue in report.Issues)
                {
                    count++;
                    var icon = issue.Severity switch
                    {
                        "Critical" => "🔴",
                        "Warning" => "🟡",
                        "Info" => "🔵",
                        _ => "⚪"
                    };

                    Console.WriteLine($"{count}. {icon} [{issue.Severity}] {issue.TypeName ?? "未知类型"}");
                    if (!string.IsNullOrEmpty(issue.PropertyName))
                    {
                        Console.WriteLine($"   属性: {issue.PropertyName}");
                    }
                    Console.WriteLine($"   问题: {issue.Description}");
                    Console.WriteLine($"   建议: {issue.Recommendation}");
                    Console.WriteLine();
                    
                    if (count >= 10)
                    {
                        Console.WriteLine($"... 还有 {report.Issues.Count - count} 个问题未显示");
                        break;
                    }
                }
            }

            Console.WriteLine("=== 修复建议 ===");
            var recommendations = analyzer.GenerateFixRecommendations(report);
            if (recommendations.Count > 0)
            {
                foreach (var rec in recommendations)
                {
                    Console.WriteLine(rec);
                }
            }
            else
            {
                Console.WriteLine("✅ 无需修复，AOT 兼容性良好！");
            }

            Console.WriteLine();
            Console.WriteLine("=== 下一步行动 ===");
            if (report.CriticalIssues > 0)
            {
                Console.WriteLine("🔴 请优先修复严重问题，这些问题会阻止 AOT 编译");
            }
            else if (report.WarningIssues > 0)
            {
                Console.WriteLine("🟡 建议修复警告问题以提升 AOT 兼容性");
            }
            else
            {
                Console.WriteLine("✅ 可以开始进行 SimpleSerializer 迁移");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 分析过程中发生错误: {ex.Message}");
            Console.WriteLine($"错误类型: {ex.GetType().Name}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}
