using System;
using System.Collections.Generic;

namespace GameManager
{
    /// <summary>
    /// 决策系统配置类
    /// 提供决策系统的各种参数配置和调优选项
    /// </summary>
    public static class DecisionSystemConfig
    {
        #region 核心开关

        /// <summary>
        /// 是否启用明主效应
        /// </summary>
        public static bool EnableWiseRulerEffect { get; set; } = true;

        /// <summary>
        /// 是否启用军师识破机制
        /// </summary>
        public static bool EnableAdvisorDetection { get; set; } = true;

        /// <summary>
        /// 是否启用建议显示系统
        /// </summary>
        public static bool EnableAdviceDisplay { get; set; } = true;

        /// <summary>
        /// 是否启用决策历史记录
        /// </summary>
        public static bool EnableDecisionHistory { get; set; } = true;

        #endregion

        #region 明主效应参数

        /// <summary>
        /// 明主效应强度 (0.1 - 3.0)
        /// 影响有效智力的计算权重
        /// </summary>
        public static float WiseRulerEffectStrength { get; set; } = 1.0f;

        /// <summary>
        /// 明主识破敏感度 (0.5 - 2.0)
        /// 影响明主识破不良建议的概率
        /// </summary>
        public static float DetectionSensitivity { get; set; } = 1.0f;

        /// <summary>
        /// 军师忠诚度阈值
        /// 低于此值时可能触发明主识破机制
        /// </summary>
        public static int LoyaltyThreshold { get; set; } = 80;

        #endregion

        #region 预测系统参数

        /// <summary>
        /// 预测误差基数
        /// 影响军师预测的基础误差范围
        /// </summary>
        public static float PredictionErrorBase { get; set; } = 1.0f;

        /// <summary>
        /// 智力影响系数
        /// 智力对预测准确度的影响程度
        /// </summary>
        public static float IntelligenceImpactFactor { get; set; } = 1.0f;

        /// <summary>
        /// 最大预测误差
        /// 限制预测误差的上限
        /// </summary>
        public static int MaxPredictionError { get; set; } = 45;

        /// <summary>
        /// 最小预测误差
        /// 即使智力很高也保留的最小误差
        /// </summary>
        public static int MinPredictionError { get; set; } = 2;

        #endregion

        #region 决策行为参数

        /// <summary>
        /// 纳谏基础概率
        /// 普通情况下的纳谏基础概率
        /// </summary>
        public static int BaseListenChance { get; set; } = 50;

        /// <summary>
        /// 智力差值影响系数
        /// 智力差值对纳谏概率的影响程度
        /// </summary>
        public static float IntelligenceDiffImpact { get; set; } = 0.5f;

        /// <summary>
        /// 关系影响系数
        /// 人际关系对决策的影响程度
        /// </summary>
        public static float RelationshipImpact { get; set; } = 1.0f;

        /// <summary>
        /// 相性影响系数
        /// 理想相性对决策的影响程度
        /// </summary>
        public static float CompatibilityImpact { get; set; } = 1.0f;

        #endregion

        #region 显示系统参数

        /// <summary>
        /// 是否显示决策过程
        /// 在UI中显示详细的决策分析过程
        /// </summary>
        public static bool ShowDecisionProcess { get; set; } = true;

        /// <summary>
        /// 是否显示有效智力
        /// 在建议中显示有效智力信息
        /// </summary>
        public static bool ShowEffectiveIntelligence { get; set; } = true;

        /// <summary>
        /// 是否显示成功率预测
        /// 在建议中显示预测成功率
        /// </summary>
        public static bool ShowSuccessRatePrediction { get; set; } = true;

        /// <summary>
        /// 建议文本详细程度 (1-3)
        /// 1=简洁, 2=标准, 3=详细
        /// </summary>
        public static int AdviceDetailLevel { get; set; } = 2;

        #endregion

        #region 历史人物配置

        /// <summary>
        /// 黄金搭档配置
        /// 历史上著名的君主-军师组合
        /// </summary>
        public static Dictionary<string, List<string>> GoldenPairs { get; set; } = new Dictionary<string, List<string>>
        {
            ["刘备"] = new List<string> { "诸葛亮", "庞统", "法正" },
            ["曹操"] = new List<string> { "荀彧", "荀攸", "郭嘉", "程昱", "贾诩" },
            ["孙权"] = new List<string> { "周瑜", "鲁肃", "吕蒙", "陆逊" },
            ["袁绍"] = new List<string> { "田丰", "沮授" },
            ["刘表"] = new List<string> { "蒯越", "蔡瑁" },
            ["马腾"] = new List<string> { "庞德" },
            ["张鲁"] = new List<string> { "阎圃" }
        };

        /// <summary>
        /// 明主人物列表
        /// 历史上以智谋著称的君主
        /// </summary>
        public static HashSet<string> WiseRulers { get; set; } = new HashSet<string>
        {
            "曹操", "司马懿", "诸葛亮", "周瑜", "陆逊", "邓艾", "钟会", "姜维"
        };

        /// <summary>
        /// 刚愎自用人物列表
        /// 历史上不善纳谏的君主
        /// </summary>
        public static HashSet<string> StubbornRulers { get; set; } = new HashSet<string>
        {
            "袁绍", "袁术", "刘璋", "韩馥", "公孙瓒", "董卓", "吕布"
        };

        #endregion

        #region 平衡性参数

        /// <summary>
        /// 决策概率上限
        /// 防止决策概率过高
        /// </summary>
        public static int MaxDecisionChance { get; set; } = 95;

        /// <summary>
        /// 决策概率下限
        /// 防止决策概率过低
        /// </summary>
        public static int MinDecisionChance { get; set; } = 5;

        /// <summary>
        /// 随机性保留比例
        /// 保留一定的随机性以增加游戏趣味
        /// </summary>
        public static float RandomnessRetention { get; set; } = 0.1f;

        #endregion

        #region 性能参数

        /// <summary>
        /// 决策历史记录上限
        /// 限制内存使用
        /// </summary>
        public static int MaxDecisionHistoryCount { get; set; } = 100;

        /// <summary>
        /// 批量决策延迟 (毫秒)
        /// 避免批量决策时的性能问题
        /// </summary>
        public static int BatchDecisionDelay { get; set; } = 10;

        /// <summary>
        /// 是否启用决策缓存
        /// 缓存相似决策以提高性能
        /// </summary>
        public static bool EnableDecisionCache { get; set; } = false;

        #endregion

        #region 调试参数

        /// <summary>
        /// 是否启用调试输出
        /// 在控制台输出详细的决策过程
        /// </summary>
        public static bool EnableDebugOutput { get; set; } = false;

        /// <summary>
        /// 是否启用性能监控
        /// 监控决策系统的性能指标
        /// </summary>
        public static bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// 调试输出级别 (1-3)
        /// 1=基本, 2=详细, 3=完整
        /// </summary>
        public static int DebugLevel { get; set; } = 1;

        #endregion

        #region 配置方法

        /// <summary>
        /// 加载默认配置
        /// </summary>
        public static void LoadDefaultConfig()
        {
            EnableWiseRulerEffect = true;
            EnableAdvisorDetection = true;
            EnableAdviceDisplay = true;
            EnableDecisionHistory = true;

            WiseRulerEffectStrength = 1.0f;
            DetectionSensitivity = 1.0f;
            LoyaltyThreshold = 80;

            PredictionErrorBase = 1.0f;
            IntelligenceImpactFactor = 1.0f;
            MaxPredictionError = 45;
            MinPredictionError = 2;

            BaseListenChance = 50;
            IntelligenceDiffImpact = 0.5f;
            RelationshipImpact = 1.0f;
            CompatibilityImpact = 1.0f;

            ShowDecisionProcess = true;
            ShowEffectiveIntelligence = true;
            ShowSuccessRatePrediction = true;
            AdviceDetailLevel = 2;

            MaxDecisionChance = 95;
            MinDecisionChance = 5;
            RandomnessRetention = 0.1f;

            MaxDecisionHistoryCount = 100;
            BatchDecisionDelay = 10;
            EnableDecisionCache = false;

            EnableDebugOutput = false;
            EnablePerformanceMonitoring = false;
            DebugLevel = 1;
        }

        /// <summary>
        /// 加载简化配置 (适合新手)
        /// </summary>
        public static void LoadSimplifiedConfig()
        {
            LoadDefaultConfig();
            
            // 简化设置
            WiseRulerEffectStrength = 1.2f; // 增强明主效应
            DetectionSensitivity = 1.3f;    // 提高识破敏感度
            AdviceDetailLevel = 1;          // 简化建议文本
            ShowDecisionProcess = false;    // 隐藏复杂过程
        }

        /// <summary>
        /// 加载专家配置 (适合高级玩家)
        /// </summary>
        public static void LoadExpertConfig()
        {
            LoadDefaultConfig();
            
            // 专家设置
            WiseRulerEffectStrength = 0.8f; // 减弱明主效应
            DetectionSensitivity = 0.9f;    // 降低识破敏感度
            AdviceDetailLevel = 3;          // 详细建议文本
            ShowDecisionProcess = true;     // 显示完整过程
            EnableDebugOutput = true;       // 启用调试输出
        }

        /// <summary>
        /// 加载历史还原配置 (最大化历史真实性)
        /// </summary>
        public static void LoadHistoricalConfig()
        {
            LoadDefaultConfig();
            
            // 历史还原设置
            WiseRulerEffectStrength = 1.5f; // 强化明主效应
            DetectionSensitivity = 1.8f;    // 高度敏感识破
            IntelligenceDiffImpact = 0.8f;  // 增强智力差值影响
            RelationshipImpact = 1.5f;      // 强化关系影响
            CompatibilityImpact = 1.3f;     // 强化相性影响
        }

        /// <summary>
        /// 验证配置参数
        /// </summary>
        public static bool ValidateConfig()
        {
            try
            {
                // 检查范围
                if (WiseRulerEffectStrength < 0.1f || WiseRulerEffectStrength > 3.0f) return false;
                if (DetectionSensitivity < 0.5f || DetectionSensitivity > 2.0f) return false;
                if (LoyaltyThreshold < 0 || LoyaltyThreshold > 100) return false;
                if (AdviceDetailLevel < 1 || AdviceDetailLevel > 3) return false;
                if (MaxDecisionChance < MinDecisionChance) return false;
                if (MaxPredictionError < MinPredictionError) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static string GetConfigSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 决策系统配置摘要 ===");
            summary.AppendLine($"明主效应: {(EnableWiseRulerEffect ? "启用" : "禁用")} (强度: {WiseRulerEffectStrength:F1})");
            summary.AppendLine($"识破机制: {(EnableAdvisorDetection ? "启用" : "禁用")} (敏感度: {DetectionSensitivity:F1})");
            summary.AppendLine($"建议显示: {(EnableAdviceDisplay ? "启用" : "禁用")} (详细度: {AdviceDetailLevel})");
            summary.AppendLine($"决策历史: {(EnableDecisionHistory ? "启用" : "禁用")} (上限: {MaxDecisionHistoryCount})");
            summary.AppendLine($"忠诚阈值: {LoyaltyThreshold}");
            summary.AppendLine($"预测误差: {MinPredictionError}-{MaxPredictionError} (基数: {PredictionErrorBase:F1})");
            summary.AppendLine($"决策概率: {MinDecisionChance}%-{MaxDecisionChance}%");
            summary.AppendLine($"调试模式: {(EnableDebugOutput ? "启用" : "禁用")} (级别: {DebugLevel})");
            
            return summary.ToString();
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public static void ResetToDefault()
        {
            LoadDefaultConfig();
        }

        #endregion

        #region 动态调整方法

        /// <summary>
        /// 根据游戏难度调整配置
        /// </summary>
        /// <param name="difficulty">难度等级 (1-5)</param>
        public static void AdjustForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 1: // 简单
                    WiseRulerEffectStrength = 1.5f;
                    DetectionSensitivity = 1.5f;
                    IntelligenceImpactFactor = 1.2f;
                    break;
                case 2: // 容易
                    WiseRulerEffectStrength = 1.2f;
                    DetectionSensitivity = 1.2f;
                    IntelligenceImpactFactor = 1.1f;
                    break;
                case 3: // 普通
                    LoadDefaultConfig();
                    break;
                case 4: // 困难
                    WiseRulerEffectStrength = 0.8f;
                    DetectionSensitivity = 0.8f;
                    IntelligenceImpactFactor = 0.9f;
                    break;
                case 5: // 专家
                    WiseRulerEffectStrength = 0.6f;
                    DetectionSensitivity = 0.6f;
                    IntelligenceImpactFactor = 0.8f;
                    break;
            }
        }

        /// <summary>
        /// 根据玩家偏好调整配置
        /// </summary>
        /// <param name="preference">偏好类型</param>
        public static void AdjustForPreference(string preference)
        {
            switch (preference.ToLower())
            {
                case "historical":
                case "历史":
                    LoadHistoricalConfig();
                    break;
                case "simplified":
                case "简化":
                    LoadSimplifiedConfig();
                    break;
                case "expert":
                case "专家":
                    LoadExpertConfig();
                    break;
                case "balanced":
                case "平衡":
                default:
                    LoadDefaultConfig();
                    break;
            }
        }

        #endregion
    }
}