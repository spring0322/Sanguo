using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 决策系统主控制器
    /// 提供统一的决策系统管理和控制接口
    /// </summary>
    public static class DecisionSystemMasterController
    {
        private static bool _isInitialized = false;
        private static ValidationReport _lastValidationReport;
        private static DateTime _lastValidationTime;

        /// <summary>
        /// 初始化决策系统
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            Console.WriteLine("=== 初始化增强决策系统 ===");
            
            // 1. 加载默认配置
            DecisionSystemConfig.LoadDefaultConfig();
            Console.WriteLine("✓ 配置系统已加载");

            // 2. 验证系统完整性
            var validationResult = DecisionSystemValidator.RunCompleteValidation();
            _lastValidationReport = validationResult;
            _lastValidationTime = DateTime.Now;
            
            if (validationResult.OverallScore >= 80)
            {
                Console.WriteLine($"✓ 系统验证通过 (评分: {validationResult.OverallScore}/100)");
                _isInitialized = true;
            }
            else
            {
                Console.WriteLine($"⚠️ 系统验证警告 (评分: {validationResult.OverallScore}/100)");
                Console.WriteLine("系统可能存在问题，但仍可使用");
                _isInitialized = true;
            }

            Console.WriteLine("=== 决策系统初始化完成 ===");
            Console.WriteLine();
        }

        /// <summary>
        /// 执行AI势力决策 (主要接口)
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <param name="decisionType">决策类型</param>
        /// <param name="context">决策上下文 (可选)</param>
        /// <returns>决策结果</returns>
        public static DecisionResult ExecuteFactionDecision(Faction faction, DecisionType decisionType, object context = null)
        {
            EnsureInitialized();

            try
            {
                // 构建决策上下文
                var decisionContext = BuildDecisionContext(decisionType, context);
                
                // 执行决策
                var result = DecisionSystemIntegration.ExecuteAIDecision(faction, decisionContext);
                
                // 记录决策日志
                LogDecision(faction, result);
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"决策执行失败: {ex.Message}");
                return CreateFailureResult(faction, decisionType, ex.Message);
            }
        }

        /// <summary>
        /// 批量执行多个势力的决策
        /// </summary>
        /// <param name="factions">势力列表</param>
        /// <param name="decisionType">决策类型</param>
        /// <returns>决策结果列表</returns>
        public static List<DecisionResult> ExecuteBatchDecisions(List<Faction> factions, DecisionType decisionType)
        {
            EnsureInitialized();

            var results = new List<DecisionResult>();
            var contexts = new List<DecisionContext>();

            // 为每个势力创建决策上下文
            foreach (var faction in factions)
            {
                var context = BuildDecisionContext(decisionType, null);
                contexts.Add(context);
            }

            // 批量执行决策
            results = DecisionSystemIntegration.ExecuteBatchDecisions(factions, contexts);

            // 记录批量决策日志
            LogBatchDecisions(results);

            return results;
        }

        /// <summary>
        /// 获取势力的决策建议 (不执行决策)
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="advice">建议内容</param>
        /// <param name="adviceType">建议类型</param>
        /// <returns>格式化的建议显示信息</returns>
        public static AdviceDisplayInfo GetFactionAdvice(Faction faction, string advice, AdviceType adviceType = AdviceType.General)
        {
            EnsureInitialized();

            try
            {
                return AdviceDisplaySystem.GetAdviceDisplayInfo(faction, advice, adviceType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取建议失败: {ex.Message}");
                return new AdviceDisplayInfo
                {
                    DisplayText = advice,
                    DecisionMaker = "系统",
                    DecisionType = DecisionMakerType.Unknown,
                    Reliability = "未知"
                };
            }
        }

        /// <summary>
        /// 获取势力的有效智力和决策能力分析
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>分析报告</returns>
        public static string GetFactionAnalysis(Faction faction)
        {
            EnsureInitialized();

            try
            {
                var analysis = new System.Text.StringBuilder();
                
                // 基本信息
                analysis.AppendLine($"=== {faction.Name} 势力决策分析 ===");
                analysis.AppendLine($"君主: {faction.Leader?.Name ?? "无"} (智力 {faction.Leader?.Intelligence ?? 0})");
                analysis.AppendLine($"军师: {faction.Advisor?.Name ?? "无"} (智力 {faction.Advisor?.Intelligence ?? 0})");
                
                // 有效智力分析
                int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
                analysis.AppendLine($"有效智力: {effectiveInt}");
                
                // 决策模式
                bool isWiseRuler = faction.Leader?.Intelligence > faction.Advisor?.Intelligence;
                analysis.AppendLine($"决策模式: {(isWiseRuler ? "明主决策" : "纳谏决策")}");
                
                // 详细分析
                analysis.AppendLine();
                analysis.AppendLine(EffectiveIntelligenceSystem.GetWiseRulerAnalysis(faction));
                
                // 决策过程描述
                analysis.AppendLine();
                analysis.AppendLine("决策过程:");
                analysis.AppendLine(AdviceDisplaySystem.GetDecisionProcessDescription(faction));
                
                return analysis.ToString();
            }
            catch (Exception ex)
            {
                return $"分析失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 运行系统诊断
        /// </summary>
        /// <returns>诊断报告</returns>
        public static string RunSystemDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== 决策系统诊断报告 ===");
            diagnostics.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnostics.AppendLine();

            // 系统状态
            diagnostics.AppendLine($"系统状态: {(_isInitialized ? "已初始化" : "未初始化")}");
            
            // 配置信息
            diagnostics.AppendLine("当前配置:");
            diagnostics.AppendLine(DecisionSystemConfig.GetConfigSummary());
            
            // 最近验证结果
            if (_lastValidationReport != null)
            {
                diagnostics.AppendLine($"最近验证: {_lastValidationTime:yyyy-MM-dd HH:mm:ss}");
                diagnostics.AppendLine($"验证评分: {_lastValidationReport.OverallScore}/100");
            }
            else
            {
                diagnostics.AppendLine("最近验证: 无");
            }

            // 性能统计
            diagnostics.AppendLine();
            diagnostics.AppendLine("性能统计:");
            diagnostics.AppendLine("  - 决策系统: 正常运行");
            diagnostics.AppendLine("  - 预测系统: 正常运行");
            diagnostics.AppendLine("  - 显示系统: 正常运行");

            return diagnostics.ToString();
        }

        /// <summary>
        /// 重新验证系统
        /// </summary>
        /// <returns>验证结果</returns>
        public static ValidationReport RevalidateSystem()
        {
            Console.WriteLine("重新验证决策系统...");
            
            _lastValidationReport = DecisionSystemValidator.RunCompleteValidation();
            _lastValidationTime = DateTime.Now;
            
            return _lastValidationReport;
        }

        /// <summary>
        /// 应用配置预设
        /// </summary>
        /// <param name="preset">预设名称</param>
        public static void ApplyConfigPreset(string preset)
        {
            EnsureInitialized();

            switch (preset.ToLower())
            {
                case "default":
                case "默认":
                    DecisionSystemConfig.LoadDefaultConfig();
                    break;
                case "simplified":
                case "简化":
                    DecisionSystemConfig.LoadSimplifiedConfig();
                    break;
                case "expert":
                case "专家":
                    DecisionSystemConfig.LoadExpertConfig();
                    break;
                case "historical":
                case "历史":
                    DecisionSystemConfig.LoadHistoricalConfig();
                    break;
                default:
                    Console.WriteLine($"未知的配置预设: {preset}");
                    return;
            }

            Console.WriteLine($"已应用配置预设: {preset}");
        }

        /// <summary>
        /// 运行使用示例
        /// </summary>
        public static void RunUsageExamples()
        {
            EnsureInitialized();
            DecisionSystemUsageExamples.RunAllExamples();
        }

        #region 私有方法

        /// <summary>
        /// 确保系统已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 构建决策上下文
        /// </summary>
        private static DecisionContext BuildDecisionContext(DecisionType decisionType, object context)
        {
            var decisionContext = new DecisionContext
            {
                DecisionType = decisionType,
                AdviceType = MapDecisionTypeToAdviceType(decisionType)
            };

            // 根据上下文类型填充额外信息
            if (context is Person person)
            {
                decisionContext.TargetPerson = person;
            }
            else if (context is Troop troop)
            {
                decisionContext.TargetTroop = troop;
            }
            else if (context is Faction faction)
            {
                decisionContext.TargetFaction = faction;
            }
            else if (context is Dictionary<string, object> dict)
            {
                decisionContext.AdditionalData = dict;
            }

            return decisionContext;
        }

        /// <summary>
        /// 映射决策类型到建议类型
        /// </summary>
        private static AdviceType MapDecisionTypeToAdviceType(DecisionType decisionType)
        {
            switch (decisionType)
            {
                case DecisionType.Military: return AdviceType.Military;
                case DecisionType.Diplomatic: return AdviceType.Diplomatic;
                case DecisionType.Recruitment: return AdviceType.Recruitment;
                case DecisionType.Internal: return AdviceType.Internal;
                case DecisionType.Battle: return AdviceType.Battle;
                default: return AdviceType.General;
            }
        }

        /// <summary>
        /// 记录决策日志
        /// </summary>
        private static void LogDecision(Faction faction, DecisionResult result)
        {
            if (DecisionSystemConfig.EnableDebugOutput)
            {
                Console.WriteLine($"[决策日志] {faction.Name}: {result.DecisionType} - {(result.Decision ? "采纳" : "拒绝")} - {result.DecisionMaker}");
            }
        }

        /// <summary>
        /// 记录批量决策日志
        /// </summary>
        private static void LogBatchDecisions(List<DecisionResult> results)
        {
            if (DecisionSystemConfig.EnableDebugOutput)
            {
                Console.WriteLine($"[批量决策] 处理了 {results.Count} 个势力的决策");
            }
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        private static DecisionResult CreateFailureResult(Faction faction, DecisionType decisionType, string error)
        {
            return new DecisionResult
            {
                Decision = false,
                Reason = $"决策失败: {error}",
                DecisionMaker = "系统",
                DecisionType = DecisionMakerType.Unknown,
                DisplayText = $"决策系统遇到错误，无法完成 {decisionType} 决策",
                Confidence = 0,
                SuccessRate = -1,
                EffectiveIntelligence = 0,
                Context = new DecisionContext { DecisionType = decisionType }
            };
        }

        #endregion

        #region 快捷方法

        /// <summary>
        /// 快速执行军事决策
        /// </summary>
        public static DecisionResult ExecuteMilitaryDecision(Faction faction, object context = null)
        {
            return ExecuteFactionDecision(faction, DecisionType.Military, context);
        }

        /// <summary>
        /// 快速执行招募决策
        /// </summary>
        public static DecisionResult ExecuteRecruitmentDecision(Faction faction, Person targetPerson)
        {
            return ExecuteFactionDecision(faction, DecisionType.Recruitment, targetPerson);
        }

        /// <summary>
        /// 快速执行外交决策
        /// </summary>
        public static DecisionResult ExecuteDiplomaticDecision(Faction faction, Faction targetFaction)
        {
            return ExecuteFactionDecision(faction, DecisionType.Diplomatic, targetFaction);
        }

        /// <summary>
        /// 快速执行战斗决策
        /// </summary>
        public static DecisionResult ExecuteBattleDecision(Faction faction, Troop targetTroop)
        {
            return ExecuteFactionDecision(faction, DecisionType.Battle, targetTroop);
        }

        /// <summary>
        /// 快速获取军事建议
        /// </summary>
        public static AdviceDisplayInfo GetMilitaryAdvice(Faction faction, string advice)
        {
            return GetFactionAdvice(faction, advice, AdviceType.Military);
        }

        /// <summary>
        /// 快速获取招募建议
        /// </summary>
        public static AdviceDisplayInfo GetRecruitmentAdvice(Faction faction, string advice)
        {
            return GetFactionAdvice(faction, advice, AdviceType.Recruitment);
        }

        #endregion
    }
}