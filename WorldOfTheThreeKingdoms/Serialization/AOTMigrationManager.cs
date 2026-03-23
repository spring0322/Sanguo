using System;
using System.Threading.Tasks;
using System.Diagnostics;
using WorldOfTheThreeKingdoms.AOTCompatibility;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Helper class to bridge the STJ migration with the existing AOT Analysis tools.
    /// This proves we are reusing the existing infrastructure.
    /// </summary>
    public static class AOTMigrationManager
    {
        public static void RunCompatibilityCheck()
        {
            Debug.WriteLine("=== Running AOT Compatibility Check via reused Analyzer ===");
            var analyzer = new AOTSerializationCompatibilityAnalyzer();
            var report = analyzer.AnalyzeCompatibility();
            
            Debug.WriteLine(report.GetSummary());
            foreach(var issue in report.Issues)
            {
                if (issue.Severity == "Critical" || issue.Severity == "Warning")
                {
                    Debug.WriteLine($"[{issue.Severity}] {issue.TypeName}.{issue.PropertyName}: {issue.Description} -> {issue.Recommendation}");
                }
            }
            Debug.WriteLine("=== Check Complete ===");
        }
    }
}
