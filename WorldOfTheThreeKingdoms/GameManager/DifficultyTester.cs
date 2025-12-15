using System;
using System.Text;

namespace GameManager
{
    /// <summary>
    /// 难度系统测试工具 - 用于验证和分析难度曲线
    /// </summary>
    public static class DifficultyTester
    {
        /// <summary>
        /// 生成难度曲线报告
        /// </summary>
        /// <param name="maxCities">要分析的最大城池数</param>
        /// <returns>难度曲线报告</returns>
        public static string GenerateDifficultyCurveReport(int maxCities = 50)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 难度曲线分析报告");
            sb.AppendLine();
            sb.AppendLine("## 系统参数");
            sb.AppendLine($"- 安全阈值: 8城");
            sb.AppendLine($"- 最大加成: 30%");
            sb.AppendLine($"- 曲线斜率: 0.07");
            sb.AppendLine();
            sb.AppendLine("## 难度曲线数据");
            sb.AppendLine();
            sb.AppendLine("| 玩家城池 | 游戏阶段 | 资源修正 | 军事修正 | 招募修正 | 外交修正 | 加成% |");
            sb.AppendLine("|---------|---------|---------|---------|---------|---------|-------|");

            for (int cities = 1; cities <= maxCities; cities++)
            {
                float resourceMod = DifficultyManager.GetAIResourceMultiplier(cities);
                float militaryMod = DifficultyManager.Instance.GetAIMilitaryModifier(cities, maxCities);
                float recruitmentMod = DifficultyManager.Instance.GetAIRecruitmentModifier(cities, maxCities);
                float diplomacyMod = DifficultyManager.Instance.GetAIDiplomacyModifier(cities, maxCities);
                string phase = DifficultyManager.Instance.GetGamePhaseDescription(cities, maxCities);
                float bonusPercent = (resourceMod - 1.0f) * 100f;

                sb.AppendLine($"| {cities,8} | {phase,9} | {resourceMod,7:F3} | {militaryMod,7:F3} | {recruitmentMod,7:F3} | {diplomacyMod,7:F3} | {bonusPercent,5:F1}% |");
            }

            sb.AppendLine();
            sb.AppendLine("## 关键节点分析");
            sb.AppendLine();

            // 分析关键节点
            var keyPoints = new[] { 5, 8, 10, 15, 20, 25, 30, 40, 50 };
            foreach (int cities in keyPoints)
            {
                float resourceMod = DifficultyManager.GetAIResourceMultiplier(cities);
                string phase = DifficultyManager.Instance.GetGamePhaseDescription(cities, maxCities);
                float bonusPercent = (resourceMod - 1.0f) * 100f;

                sb.AppendLine($"**{cities}城**: {phase} - AI获得{bonusPercent:F1}%资源加成");
            }

            sb.AppendLine();
            sb.AppendLine("## 曲线特性");
            sb.AppendLine();
            sb.AppendLine("- **新手保护**: 8城以下无任何AI加成");
            sb.AppendLine("- **平滑增长**: 使用对数曲线，避免突然的难度跳跃");
            sb.AppendLine("- **硬上限**: AI加成永远不超过30%，避免过度作弊");
            sb.AppendLine("- **差异化修正**: 不同类型的修正有不同的强度比例");

            return sb.ToString();
        }

        /// <summary>
        /// 对比新旧难度系统
        /// </summary>
        /// <returns>对比报告</returns>
        public static string CompareOldAndNewSystem()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 新旧难度系统对比");
            sb.AppendLine();
            sb.AppendLine("| 城池数 | 旧系统(按比例) | 新系统(对数) | 差异 | 说明 |");
            sb.AppendLine("|-------|---------------|-------------|------|------|");

            // 假设总城池数为50进行对比
            int totalCities = 50;
            
            for (int cities = 5; cities <= 40; cities += 5)
            {
                // 旧系统（按比例）
                float oldRatio = (float)cities / totalCities;
                float oldModifier = 1.0f;
                if (oldRatio >= 0.6f) oldModifier = 1.5f;
                else if (oldRatio >= 0.4f) oldModifier = 1.25f;
                else if (oldRatio >= 0.2f) oldModifier = 1.1f;

                // 新系统（对数）
                float newModifier = DifficultyManager.GetAIResourceMultiplier(cities);
                
                float difference = newModifier - oldModifier;
                string explanation = "";
                
                if (cities <= 8)
                    explanation = "新手保护期";
                else if (Math.Abs(difference) < 0.01f)
                    explanation = "基本相同";
                else if (difference > 0)
                    explanation = $"新系统更难 (+{difference:F2})";
                else
                    explanation = $"新系统更易 ({difference:F2})";

                sb.AppendLine($"| {cities,5} | {oldModifier,11:F3} | {newModifier,9:F3} | {difference,6:F3} | {explanation} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 主要改进");
            sb.AppendLine();
            sb.AppendLine("1. **新手保护**: 8城以下完全无AI加成");
            sb.AppendLine("2. **平滑过渡**: 对数曲线消除了阶梯式跳跃");
            sb.AppendLine("3. **合理上限**: 30%上限比旧系统的50%更平衡");
            sb.AppendLine("4. **精确控制**: 可以通过调整参数精确控制难度曲线");

            return sb.ToString();
        }

        /// <summary>
        /// 测试不同参数对难度曲线的影响
        /// </summary>
        /// <returns>参数影响分析</returns>
        public static string AnalyzeParameterEffects()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 参数影响分析");
            sb.AppendLine();

            // 测试不同的曲线斜率
            sb.AppendLine("## 曲线斜率影响 (20城时的AI加成)");
            sb.AppendLine();
            sb.AppendLine("| 斜率 | AI加成% | 说明 |");
            sb.AppendLine("|------|---------|------|");

            var slopes = new[] { 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.10f };
            foreach (float slope in slopes)
            {
                float bonus = CalculateBonusWithSlope(20, slope);
                string description = "";
                if (slope < 0.06f) description = "增长过慢";
                else if (slope > 0.09f) description = "增长过快";
                else description = "合理范围";

                sb.AppendLine($"| {slope:F2} | {bonus:F1}% | {description} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 安全阈值影响 (15城时的AI加成)");
            sb.AppendLine();
            sb.AppendLine("| 阈值 | AI加成% | 说明 |");
            sb.AppendLine("|------|---------|------|");

            var thresholds = new[] { 5, 6, 7, 8, 9, 10 };
            foreach (int threshold in thresholds)
            {
                float bonus = CalculateBonusWithThreshold(15, threshold);
                string description = "";
                if (threshold < 7) description = "保护期过短";
                else if (threshold > 9) description = "保护期过长";
                else description = "合理范围";

                sb.AppendLine($"| {threshold,4} | {bonus:F1}% | {description} |");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 使用指定斜率计算加成
        /// </summary>
        private static float CalculateBonusWithSlope(int cities, float slope)
        {
            const int threshold = 8;
            const float maxBonus = 0.3f;

            if (cities <= threshold) return 0f;

            int effectiveCount = cities - threshold;
            float logBonus = (float)(Math.Log(effectiveCount) * slope);
            float finalBonus = Math.Max(0f, Math.Min(logBonus, maxBonus));
            
            return finalBonus * 100f;
        }

        /// <summary>
        /// 使用指定阈值计算加成
        /// </summary>
        private static float CalculateBonusWithThreshold(int cities, int threshold)
        {
            const float slope = 0.07f;
            const float maxBonus = 0.3f;

            if (cities <= threshold) return 0f;

            int effectiveCount = cities - threshold;
            float logBonus = (float)(Math.Log(effectiveCount) * slope);
            float finalBonus = Math.Max(0f, Math.Min(logBonus, maxBonus));
            
            return finalBonus * 100f;
        }

        /// <summary>
        /// 生成完整的测试报告
        /// </summary>
        /// <returns>完整测试报告</returns>
        public static string GenerateFullTestReport()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(GenerateDifficultyCurveReport());
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(CompareOldAndNewSystem());
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(AnalyzeParameterEffects());

            return sb.ToString();
        }

        /// <summary>
        /// 输出测试报告到调试控制台
        /// </summary>
        public static void PrintTestReportToDebug()
        {
            try
            {
                string report = GenerateFullTestReport();
                string[] lines = report.Split('\n');
                
                System.Diagnostics.Debug.WriteLine("[难度测试] ========== 难度系统测试报告 ==========");
                foreach (string line in lines)
                {
                    System.Diagnostics.Debug.WriteLine($"[难度测试] {line}");
                }
                System.Diagnostics.Debug.WriteLine("[难度测试] ========== 报告结束 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[难度测试] 生成测试报告时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证难度系统的数学正确性
        /// </summary>
        /// <returns>验证结果</returns>
        public static bool ValidateDifficultySystem()
        {
            try
            {
                // 测试1: 安全阈值内应该无加成
                for (int i = 1; i <= 8; i++)
                {
                    float modifier = DifficultyManager.GetAIResourceMultiplier(i);
                    if (Math.Abs(modifier - 1.0f) > 0.001f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[难度验证] 失败: {i}城时应该无加成，但得到{modifier:F3}");
                        return false;
                    }
                }

                // 测试2: 加成应该单调递增
                float lastModifier = 1.0f;
                for (int i = 9; i <= 50; i++)
                {
                    float modifier = DifficultyManager.GetAIResourceMultiplier(i);
                    if (modifier < lastModifier)
                    {
                        System.Diagnostics.Debug.WriteLine($"[难度验证] 失败: {i}城时加成{modifier:F3}小于{i-1}城时的{lastModifier:F3}");
                        return false;
                    }
                    lastModifier = modifier;
                }

                // 测试3: 加成不应超过上限
                for (int i = 1; i <= 100; i++)
                {
                    float modifier = DifficultyManager.GetAIResourceMultiplier(i);
                    if (modifier > 1.3f + 0.001f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[难度验证] 失败: {i}城时加成{modifier:F3}超过上限1.3");
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[难度验证] 所有测试通过，难度系统数学正确性验证成功");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[难度验证] 验证过程中发生异常: {ex.Message}");
                return false;
            }
        }
    }
}