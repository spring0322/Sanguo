using System;
using System.Collections.Generic;
using System.Text;
using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms;

namespace GameObjects.AI;

/// <summary>
/// 混合AI集成辅助工具
/// 用于快速测试和调试混合AI系统
/// </summary>
public static class HybridAIIntegrationHelper
{
    /// <summary>
    /// 测试关键部队判定逻辑
    /// 🧊 Cold Path: 测试工具，允许防御性检查
    /// </summary>
    public static string TestKeyTroopDetection(List<Troop> troops)
    {
        // ✅ 测试工具允许防御性检查（避免测试崩溃）
        if (troops.Count == 0)
        {
            return "测试失败：部队列表为空";
        }

        StringBuilder report = new();
        report.AppendLine("=== 关键部队判定测试报告 ===\n");

        int keyTroopCount = 0;
        int normalTroopCount = 0;

        for (int i = 0; i < troops.Count; i++)
        {
            Troop troop = troops[i];
            
            // ✅ 测试工具：跳过无效数据
            if (troop.Leader == null)
            {
                report.AppendLine($"⚠️ 警告: 部队ID:{troop.ID} 没有Leader，跳过\n");
                continue;
            }

            // 模拟判定逻辑
            bool isKey = TestIsKeyTroop(troop, out string reason);

            if (isKey)
            {
                keyTroopCount++;
                report.AppendLine($"✅ 关键部队: {troop.Leader.Name} (ID:{troop.ID})");
                report.AppendLine($"   原因: {reason}");
                report.AppendLine($"   属性: 统{troop.Leader.Command} 智{troop.Leader.Intelligence} 武{troop.Leader.Strength}");
                report.AppendLine($"   兵力: {troop.Quantity}");
                if (troop.Army.Kind != null)
                {
                    report.AppendLine($"   兵种: {troop.Army.Kind.Name} (ID:{troop.Army.Kind.ID}, 上限:{troop.Army.Kind.MaxScale})");
                }
                report.AppendLine();
            }
            else
            {
                normalTroopCount++;
                report.AppendLine($"⚪ 普通部队: {troop.Leader.Name} (ID:{troop.ID})");
                report.AppendLine($"   原因: {reason}");
                report.AppendLine();
            }
        }

        report.AppendLine("=== 统计结果 ===");
        report.AppendLine($"总部队数: {troops.Count}");
        report.AppendLine($"关键部队: {keyTroopCount} ({(float)keyTroopCount / troops.Count * 100:F1}%)");
        report.AppendLine($"普通部队: {normalTroopCount} ({(float)normalTroopCount / troops.Count * 100:F1}%)");

        return report.ToString();
    }

    /// <summary>
    /// 测试单个部队的判定逻辑
    /// 🧊 Cold Path: 测试工具
    /// </summary>
    private static bool TestIsKeyTroop(Troop troop, out string reason)
    {
        AIDecisionConfig config = AIDecisionConfigManager.Config;
        KeyTroopCriteriaConfig criteria = config.KeyTroopCriteria;
        List<string> reasons = [];

        // 1. 属性阈值
        StatThresholdsConfig statThresholds = criteria.StatThresholds;
        if (troop.Leader.Command >= statThresholds.Command)
            reasons.Add($"统率{troop.Leader.Command}≥{statThresholds.Command}");
        if (troop.Leader.Intelligence >= statThresholds.Intelligence)
            reasons.Add($"智力{troop.Leader.Intelligence}≥{statThresholds.Intelligence}");
        if (troop.Leader.Strength >= statThresholds.Strength)
            reasons.Add($"武力{troop.Leader.Strength}≥{statThresholds.Strength}");

        // 2. 称号等级（业务逻辑：可以为null）
        TitleLevelThresholdConfig titleThreshold = criteria.TitleLevelThreshold;
        Title personalTitle = troop.Leader.getTitleOfKind(Session.Current.Scenario.GameCommonData.AllTitleKinds.GetTitleKind(troop.Leader.PersonalTitleString));
        if (personalTitle != null && personalTitle.Level >= titleThreshold.MinLevel)
            reasons.Add($"私有称号Lv{personalTitle.Level}");
        Title combatTitle = troop.Leader.getTitleOfKind(Session.Current.Scenario.GameCommonData.AllTitleKinds.GetTitleKind(troop.Leader.CombatTitleString));
        if (combatTitle != null && combatTitle.Level >= titleThreshold.MinLevel)
            reasons.Add($"战斗称号Lv{combatTitle.Level}");

        // 3. 兵力
        QuantityThresholdConfig quantityThreshold = criteria.QuantityThreshold;
        if (troop.Quantity > quantityThreshold.MinQuantity)
            reasons.Add($"兵力{troop.Quantity}");

        // 4. 精锐兵种
        int kindID = troop.Army.Kind.ID;
        if (AIDecisionConfigManager.IsEliteTroopKind(kindID))
        {
            string kindName = AIDecisionConfigManager.GetEliteTroopKindName(kindID);
            reasons.Add($"精锐兵种:{kindName}");
        }
        
        // 自动检测
        EliteTroopKindsConfig eliteConfig = criteria.EliteTroopKinds;
        if (eliteConfig.AutoDetect.Enabled)
        {
            int maxScale = troop.Army.Kind.MaxScale;
            string typeName = troop.Army.Kind.Type.ToString();
            
            if (maxScale <= eliteConfig.AutoDetect.MaxScaleThreshold &&
                !eliteConfig.AutoDetect.ExcludeTypes.Contains(typeName))
            {
                reasons.Add($"自动检测精锐(上限{maxScale})");
            }
        }

        if (reasons.Count > 0)
        {
            reason = string.Join(", ", reasons);
            return true;
        }

        reason = "不满足任何关键部队条件";
        return false;
    }

    /// <summary>
    /// 生成配置诊断报告
    /// 🧊 Cold Path: 诊断工具
    /// </summary>
    public static string GenerateConfigDiagnostics()
    {
        StringBuilder report = new();
        report.AppendLine("=== AI决策配置诊断报告 ===\n");

        try
        {
            AIDecisionConfig config = AIDecisionConfigManager.Config;
            KeyTroopCriteriaConfig criteria = config.KeyTroopCriteria;

            report.AppendLine($"决策模式: {config.DecisionMode}");
            report.AppendLine();

            // 关键部队标准
            report.AppendLine("【关键部队判定标准】");
            report.AppendLine($"  属性阈值:");
            report.AppendLine($"    统率 ≥ {criteria.StatThresholds.Command}");
            report.AppendLine($"    智力 ≥ {criteria.StatThresholds.Intelligence}");
            report.AppendLine($"    武力 ≥ {criteria.StatThresholds.Strength}");

            report.AppendLine($"  称号等级 ≥ {criteria.TitleLevelThreshold.MinLevel}");
            report.AppendLine($"  兵力 > {criteria.QuantityThreshold.MinQuantity}");
            report.AppendLine($"  精锐兵种数量: {criteria.EliteTroopKinds.TroopKindIDs.Count}");
            
            if (criteria.EliteTroopKinds.AutoDetect.Enabled)
            {
                report.AppendLine($"  自动检测: 启用 (上限≤{criteria.EliteTroopKinds.AutoDetect.MaxScaleThreshold})");
            }

            report.AppendLine();

            // 性能限制
            report.AppendLine("【性能限制】");
            report.AppendLine($"  每回合最大UtilityAI使用次数: {config.PerformanceLimits.MaxUtilityAITroopsPerTurn}");
            report.AppendLine($"  超时时间: {config.PerformanceLimits.UtilityAITimeoutMs}ms");
            report.AppendLine($"  超时降级: {(config.PerformanceLimits.FallbackToTraditionalOnTimeout ? "启用" : "禁用")}");

            report.AppendLine();

            // 调试设置
            report.AppendLine("【调试设置】");
            report.AppendLine($"  日志记录: {(config.DebugSettings.EnableLogging ? "启用" : "禁用")}");
            report.AppendLine($"  关键部队决策日志: {(config.DebugSettings.LogKeyTroopDecisions ? "启用" : "禁用")}");
            report.AppendLine($"  性能指标日志: {(config.DebugSettings.LogPerformanceMetrics ? "启用" : "禁用")}");

            report.AppendLine();
            report.AppendLine("✅ 配置加载成功");
        }
        catch (Exception ex)
        {
            report.AppendLine($"❌ 配置加载失败: {ex.Message}");
            report.AppendLine($"堆栈跟踪:\n{ex.StackTrace}");
        }

        return report.ToString();
    }

    /// <summary>
    /// 快速测试：打印所有精锐兵种
    /// 🧊 Cold Path: 测试工具
    /// </summary>
    public static string ListEliteTroopKinds()
    {
        StringBuilder report = new();
        report.AppendLine("=== 精锐兵种列表 ===\n");

        AIDecisionConfig config = AIDecisionConfigManager.Config;
        EliteTroopKindsConfig eliteConfig = config.KeyTroopCriteria.EliteTroopKinds;

        report.AppendLine($"配置数量: {eliteConfig.TroopKindIDs.Count}\n");

        for (int i = 0; i < eliteConfig.TroopKindIDs.Count; i++)
        {
            int kindID = eliteConfig.TroopKindIDs[i];
            string kindName = AIDecisionConfigManager.GetEliteTroopKindName(kindID);
            report.AppendLine($"  {kindID}: {kindName}");
        }

        return report.ToString();
    }

    /// <summary>
    /// 性能测试：模拟AI决策
    /// 🧊 Cold Path: 测试工具
    /// </summary>
    public static string PerformanceTest(List<Troop> troops, int iterations = 10)
    {
        // ✅ 测试工具允许防御性检查
        if (troops.Count == 0)
        {
            return "测试失败：部队列表为空";
        }

        StringBuilder report = new();
        report.AppendLine("=== 性能测试报告 ===\n");
        report.AppendLine($"测试部队数: {troops.Count}");
        report.AppendLine($"测试迭代次数: {iterations}\n");

        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        long totalMs = 0;
        int keyTroopCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            timer.Restart();
            
            for (int j = 0; j < troops.Count; j++)
            {
                Troop troop = troops[j];
                
                // ✅ 测试工具：跳过无效数据
                if (troop.Leader == null) continue;
                
                bool isKey = TestIsKeyTroop(troop, out _);
                if (isKey) keyTroopCount++;
            }
            
            timer.Stop();
            totalMs += timer.ElapsedMilliseconds;
        }

        long avgMs = totalMs / iterations;
        float avgPerTroop = (float)totalMs / (troops.Count * iterations);

        report.AppendLine($"总耗时: {totalMs}ms");
        report.AppendLine($"平均每次迭代: {avgMs}ms");
        report.AppendLine($"平均每个部队: {avgPerTroop:F2}ms");
        report.AppendLine($"关键部队比例: {(float)keyTroopCount / (troops.Count * iterations) * 100:F1}%");

        return report.ToString();
    }
}
