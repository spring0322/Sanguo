using System;
using System.Text;

namespace GameManager
{
    /// <summary>
    /// 腐败系统测试工具 - 用于验证和分析玩家腐败系统
    /// </summary>
    public static class CorruptionTester
    {
        /// <summary>
        /// 生成腐败系统分析报告
        /// </summary>
        /// <param name="maxCities">要分析的最大城池数</param>
        /// <returns>分析报告</returns>
        public static string GenerateCorruptionAnalysisReport(int maxCities = 30)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 玩家腐败系统分析报告");
            sb.AppendLine();
            sb.AppendLine("## 系统参数");
            sb.AppendLine($"- 直辖上限: 5城");
            sb.AppendLine($"- 腐败率: 每城0.5%");
            sb.AppendLine($"- 效率下限: 50%");
            sb.AppendLine($"- 太守抵抗: 最高20%");
            sb.AppendLine();
            sb.AppendLine("## 效率衰减曲线");
            sb.AppendLine();
            sb.AppendLine("| 城池数 | 超出数 | 腐败率 | 行政效率 | 效率损失 | 阶段描述 |");
            sb.AppendLine("|-------|-------|-------|---------|---------|---------|");

            for (int cities = 1; cities <= maxCities; cities++)
            {
                float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(cities);
                int excessCities = Math.Max(0, cities - 5);
                float corruptionRate = excessCities * 0.005f;
                float efficiencyLoss = (1.0f - efficiency) * 100f;
                string phase = GetEfficiencyPhase(efficiency);

                sb.AppendLine($"| {cities,6} | {excessCities,6} | {corruptionRate:P1} | {efficiency:P1} | {efficiencyLoss:F1}% | {phase} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 关键节点分析");
            sb.AppendLine();

            var keyPoints = new[] { 5, 8, 10, 15, 20, 25, 30 };
            foreach (int cities in keyPoints)
            {
                float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(cities);
                string phase = GetEfficiencyPhase(efficiency);
                float efficiencyLoss = (1.0f - efficiency) * 100f;

                sb.AppendLine($"**{cities}城**: {phase} - 行政效率{efficiency:P0} (损失{efficiencyLoss:F1}%)");
            }

            sb.AppendLine();
            sb.AppendLine("## 太守政治能力影响");
            sb.AppendLine();
            sb.AppendLine("| 太守政治 | 抵抗效果 | 15城时效率 | 25城时效率 |");
            sb.AppendLine("|---------|---------|-----------|-----------|");

            var politicsLevels = new[] { 60, 70, 80, 90, 100 };
            foreach (int politics in politicsLevels)
            {
                float resistance = PlayerCorruptionSystem.CalculateGovernorResistance(politics);
                float baseEfficiency15 = PlayerCorruptionSystem.GetPlayerEfficiency(15);
                float baseEfficiency25 = PlayerCorruptionSystem.GetPlayerEfficiency(25);
                
                float actualEfficiency15 = Math.Min(1.0f, baseEfficiency15 + (politics / 100f) * 0.2f);
                float actualEfficiency25 = Math.Min(1.0f, baseEfficiency25 + (politics / 100f) * 0.2f);

                sb.AppendLine($"| {politics,8} | {resistance:F1}% | {actualEfficiency15:P1} | {actualEfficiency25:P1} |");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 对比AI难度系统和玩家腐败系统
        /// </summary>
        /// <returns>对比报告</returns>
        public static string CompareAIBuffAndPlayerCorruption()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# AI难度系统 vs 玩家腐败系统对比");
            sb.AppendLine();
            sb.AppendLine("| 城池数 | AI资源倍率 | 玩家效率 | 实际差距 | 平衡状态 |");
            sb.AppendLine("|-------|-----------|---------|---------|---------|");

            for (int cities = 5; cities <= 30; cities += 5)
            {
                float aiMultiplier = DifficultyManager.GetAIResourceMultiplier(cities);
                float playerEfficiency = PlayerCorruptionSystem.GetPlayerEfficiency(cities);
                float actualGap = aiMultiplier / playerEfficiency;
                string balanceState = GetBalanceState(actualGap);

                sb.AppendLine($"| {cities,6} | {aiMultiplier,9:F3}x | {playerEfficiency,7:P1} | {actualGap,7:F2}x | {balanceState} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 平衡分析");
            sb.AppendLine();
            sb.AppendLine("- **实际差距 < 1.5x**: 平衡良好");
            sb.AppendLine("- **实际差距 1.5-2.0x**: 轻微偏向AI");
            sb.AppendLine("- **实际差距 > 2.0x**: AI优势明显");
            sb.AppendLine();
            sb.AppendLine("## 设计理念");
            sb.AppendLine();
            sb.AppendLine("1. **双向制衡**: AI获得加成的同时，玩家受到腐败限制");
            sb.AppendLine("2. **渐进平衡**: 随着扩张，双方的修正都逐渐增强");
            sb.AppendLine("3. **策略深度**: 玩家需要在扩张和效率之间做出选择");
            sb.AppendLine("4. **长期挑战**: 确保游戏后期仍有足够挑战性");

            return sb.ToString();
        }

        /// <summary>
        /// 分析扩张策略的收益
        /// </summary>
        /// <returns>策略分析</returns>
        public static string AnalyzeExpansionStrategies()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 扩张策略收益分析");
            sb.AppendLine();
            sb.AppendLine("## 不同扩张速度的收益对比");
            sb.AppendLine();
            sb.AppendLine("假设每城基础收入1000，分析不同扩张策略的实际收益：");
            sb.AppendLine();
            sb.AppendLine("| 城池数 | 基础总收入 | 实际收入 | 收入效率 | 边际收益 | 策略建议 |");
            sb.AppendLine("|-------|-----------|---------|---------|---------|---------|");

            int lastActualIncome = 0;
            for (int cities = 1; cities <= 25; cities++)
            {
                int baseIncome = cities * 1000;
                float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(cities);
                int actualIncome = (int)(baseIncome * efficiency);
                int marginalIncome = actualIncome - lastActualIncome;
                string strategy = GetExpansionStrategy(cities, efficiency, marginalIncome);

                sb.AppendLine($"| {cities,6} | {baseIncome,9} | {actualIncome,7} | {efficiency:P1} | {marginalIncome,7} | {strategy} |");
                
                lastActualIncome = actualIncome;
            }

            sb.AppendLine();
            sb.AppendLine("## 策略建议说明");
            sb.AppendLine();
            sb.AppendLine("- **积极扩张**: 边际收益高，应该继续扩张");
            sb.AppendLine("- **谨慎扩张**: 边际收益下降，需要权衡利弊");
            sb.AppendLine("- **控制规模**: 边际收益很低，应该专注内政");
            sb.AppendLine("- **停止扩张**: 边际收益过低，扩张得不偿失");

            return sb.ToString();
        }

        /// <summary>
        /// 获取效率阶段描述
        /// </summary>
        private static string GetEfficiencyPhase(float efficiency)
        {
            if (efficiency >= 1.0f) return "直辖高效";
            if (efficiency >= 0.9f) return "轻微腐败";
            if (efficiency >= 0.8f) return "中度腐败";
            if (efficiency >= 0.7f) return "严重腐败";
            if (efficiency >= 0.6f) return "极度腐败";
            return "效率下限";
        }

        /// <summary>
        /// 获取平衡状态描述
        /// </summary>
        private static string GetBalanceState(float gap)
        {
            if (gap < 1.2f) return "玩家优势";
            if (gap < 1.5f) return "基本平衡";
            if (gap < 2.0f) return "AI轻微优势";
            return "AI明显优势";
        }

        /// <summary>
        /// 获取扩张策略建议
        /// </summary>
        private static string GetExpansionStrategy(int cities, float efficiency, int marginalIncome)
        {
            if (cities <= 5) return "积极扩张";
            if (efficiency >= 0.9f && marginalIncome >= 800) return "积极扩张";
            if (efficiency >= 0.8f && marginalIncome >= 600) return "谨慎扩张";
            if (efficiency >= 0.7f && marginalIncome >= 400) return "控制规模";
            return "停止扩张";
        }

        /// <summary>
        /// 生成完整的测试报告
        /// </summary>
        /// <returns>完整测试报告</returns>
        public static string GenerateFullTestReport()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(GenerateCorruptionAnalysisReport());
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(CompareAIBuffAndPlayerCorruption());
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(AnalyzeExpansionStrategies());

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
                
                System.Diagnostics.Debug.WriteLine("[腐败测试] ========== 腐败系统测试报告 ==========");
                foreach (string line in lines)
                {
                    System.Diagnostics.Debug.WriteLine($"[腐败测试] {line}");
                }
                System.Diagnostics.Debug.WriteLine("[腐败测试] ========== 报告结束 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[腐败测试] 生成测试报告时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证腐败系统的数学正确性
        /// </summary>
        /// <returns>验证结果</returns>
        public static bool ValidateCorruptionSystem()
        {
            try
            {
                // 测试1: 直辖范围内应该无腐败
                for (int i = 1; i <= 5; i++)
                {
                    float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(i);
                    if (Math.Abs(efficiency - 1.0f) > 0.001f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[腐败验证] 失败: {i}城时应该无腐败，但效率为{efficiency:F3}");
                        return false;
                    }
                }

                // 测试2: 效率应该单调递减
                float lastEfficiency = 1.0f;
                for (int i = 6; i <= 50; i++)
                {
                    float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(i);
                    if (efficiency > lastEfficiency + 0.001f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[腐败验证] 失败: {i}城时效率{efficiency:F3}高于{i-1}城时的{lastEfficiency:F3}");
                        return false;
                    }
                    lastEfficiency = efficiency;
                }

                // 测试3: 效率不应低于下限
                for (int i = 1; i <= 200; i++)
                {
                    float efficiency = PlayerCorruptionSystem.GetPlayerEfficiency(i);
                    if (efficiency < 0.5f - 0.001f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[腐败验证] 失败: {i}城时效率{efficiency:F3}低于下限0.5");
                        return false;
                    }
                }

                // 测试4: 太守抵抗效果
                float baseEfficiency = PlayerCorruptionSystem.GetPlayerEfficiency(15);
                float resistedIncome = PlayerCorruptionSystem.GetCityActualIncome(1000f, baseEfficiency, 100);
                float expectedIncome = 1000f * Math.Min(1.0f, baseEfficiency + 0.2f);
                
                if (Math.Abs(resistedIncome - expectedIncome) > 1.0f)
                {
                    System.Diagnostics.Debug.WriteLine($"[腐败验证] 失败: 太守抵抗效果计算错误");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[腐败验证] 所有测试通过，腐败系统数学正确性验证成功");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[腐败验证] 验证过程中发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 模拟不同扩张策略的长期收益
        /// </summary>
        /// <param name="turns">模拟回合数</param>
        /// <returns>模拟结果</returns>
        public static string SimulateExpansionStrategies(int turns = 20)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 扩张策略长期收益模拟");
            sb.AppendLine();
            sb.AppendLine($"模拟{turns}回合，对比不同扩张策略的累计收益：");
            sb.AppendLine();
            sb.AppendLine("| 回合 | 保守策略 | 平衡策略 | 激进策略 |");
            sb.AppendLine("|------|---------|---------|---------|");

            int conservativeCities = 5;  // 保守：保持5城
            int balancedCities = 5;      // 平衡：每2回合+1城
            int aggressiveCities = 5;    // 激进：每回合+1城

            int conservativeTotalIncome = 0;
            int balancedTotalIncome = 0;
            int aggressiveTotalIncome = 0;

            for (int turn = 1; turn <= turns; turn++)
            {
                // 更新城池数
                if (turn > 1)
                {
                    if (turn % 2 == 0 && balancedCities < 20) balancedCities++;
                    if (aggressiveCities < 30) aggressiveCities++;
                }

                // 计算收入
                int conservativeIncome = (int)(conservativeCities * 1000 * PlayerCorruptionSystem.GetPlayerEfficiency(conservativeCities));
                int balancedIncome = (int)(balancedCities * 1000 * PlayerCorruptionSystem.GetPlayerEfficiency(balancedCities));
                int aggressiveIncome = (int)(aggressiveCities * 1000 * PlayerCorruptionSystem.GetPlayerEfficiency(aggressiveCities));

                conservativeTotalIncome += conservativeIncome;
                balancedTotalIncome += balancedIncome;
                aggressiveTotalIncome += aggressiveIncome;

                sb.AppendLine($"| {turn,4} | {conservativeTotalIncome,7} | {balancedTotalIncome,7} | {aggressiveTotalIncome,7} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 策略总结");
            sb.AppendLine();
            sb.AppendLine($"- **保守策略**: 保持5城，总收益{conservativeTotalIncome:N0}");
            sb.AppendLine($"- **平衡策略**: 适度扩张至{balancedCities}城，总收益{balancedTotalIncome:N0}");
            sb.AppendLine($"- **激进策略**: 快速扩张至{aggressiveCities}城，总收益{aggressiveTotalIncome:N0}");
            sb.AppendLine();
            
            string bestStrategy = "保守策略";
            int bestIncome = conservativeTotalIncome;
            if (balancedTotalIncome > bestIncome) { bestStrategy = "平衡策略"; bestIncome = balancedTotalIncome; }
            if (aggressiveTotalIncome > bestIncome) { bestStrategy = "激进策略"; bestIncome = aggressiveTotalIncome; }
            
            sb.AppendLine($"**最优策略**: {bestStrategy} (总收益: {bestIncome:N0})");

            return sb.ToString();
        }
    }
}