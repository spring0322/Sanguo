using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 建议显示系统 - 根据明主效应调整建议文本显示
    /// 区分军师建议与明主决断的不同表现形式
    /// </summary>
    public static class AdviceDisplaySystem
    {
        /// <summary>
        /// 获取调整后的建议文本
        /// 根据君主与军师的智力关系，调整建议的表述方式
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="originalAdvice">原始建议内容</param>
        /// <param name="adviceType">建议类型</param>
        /// <returns>调整后的建议文本</returns>
        public static string GetAdviceText(Faction faction, string originalAdvice, AdviceType adviceType = AdviceType.General)
        {
            if (faction?.Leader == null || faction.Advisor == null)
            {
                return originalAdvice;
            }

            // 正常情况：军师智力 >= 君主智力
            if (faction.Leader.Intelligence <= faction.Advisor.Intelligence)
            {
                return GetAdvisorAdviceText(faction, originalAdvice, adviceType);
            }
            // 明主情况：君主智力 > 军师智力
            else
            {
                return GetWiseRulerAdviceText(faction, originalAdvice, adviceType);
            }
        }

        /// <summary>
        /// 获取军师建议文本（传统模式）
        /// </summary>
        private static string GetAdvisorAdviceText(Faction faction, string originalAdvice, AdviceType adviceType)
        {
            string prefix = GetAdvisorPrefix(faction.Advisor, adviceType);
            return $"{prefix}：\n{originalAdvice}";
        }

        /// <summary>
        /// 获取明主决断文本（明主模式）
        /// </summary>
        private static string GetWiseRulerAdviceText(Faction faction, string originalAdvice, AdviceType adviceType)
        {
            string rulerDecision = GetRulerDecisionText(faction, originalAdvice, adviceType);
            string attribution = GetWiseRulerAttribution(faction.Leader);
            
            return $"{rulerDecision}\n{attribution}";
        }

        /// <summary>
        /// 获取军师称谓前缀
        /// </summary>
        private static string GetAdvisorPrefix(Person advisor, AdviceType adviceType)
        {
            switch (adviceType)
            {
                case AdviceType.Military:
                    return $"军师 {advisor.Name} 献策";
                case AdviceType.Diplomatic:
                    return $"军师 {advisor.Name} 谏言";
                case AdviceType.Internal:
                    return $"军师 {advisor.Name} 建议";
                case AdviceType.Recruitment:
                    return $"军师 {advisor.Name} 分析";
                case AdviceType.Battle:
                    return $"军师 {advisor.Name} 料敌";
                default:
                    return $"军师 {advisor.Name} 谏言";
            }
        }

        /// <summary>
        /// 获取明主决断文本
        /// </summary>
        private static string GetRulerDecisionText(Faction faction, string originalAdvice, AdviceType adviceType)
        {
            string rulerName = faction.Leader.Name;
            
            // 根据建议类型调整表述
            switch (adviceType)
            {
                case AdviceType.Military:
                    return $"{rulerName} 审视战局：\n{originalAdvice}";
                case AdviceType.Diplomatic:
                    return $"{rulerName} 洞察外交：\n{originalAdvice}";
                case AdviceType.Internal:
                    return $"{rulerName} 治理内政：\n{originalAdvice}";
                case AdviceType.Recruitment:
                    return $"{rulerName} 识人用人：\n{originalAdvice}";
                case AdviceType.Battle:
                    return $"{rulerName} 料敌制胜：\n{originalAdvice}";
                default:
                    return $"{rulerName} 审视了局势：\n{originalAdvice}";
            }
        }

        /// <summary>
        /// 获取明主归属说明
        /// </summary>
        private static string GetWiseRulerAttribution(Person ruler)
        {
            var attributions = new string[]
            {
                $"(此乃 {ruler.Name} 之决断)",
                $"(明主 {ruler.Name} 亲自定夺)",
                $"({ruler.Name} 慧眼如炬)",
                $"(主公 {ruler.Name} 英明决策)"
            };
            
            return attributions[GameObject.Random(attributions.Length)];
        }

        /// <summary>
        /// 获取完整的建议显示信息
        /// 包含建议内容、决策者信息、可信度等
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="originalAdvice">原始建议</param>
        /// <param name="adviceType">建议类型</param>
        /// <param name="successRate">成功率（如果有）</param>
        /// <returns>完整建议信息</returns>
        public static AdviceDisplayInfo GetAdviceDisplayInfo(Faction faction, string originalAdvice, AdviceType adviceType = AdviceType.General, int successRate = -1)
        {
            if (faction?.Leader == null || faction.Advisor == null)
            {
                return new AdviceDisplayInfo
                {
                    DisplayText = originalAdvice,
                    DecisionMaker = "未知",
                    DecisionType = DecisionMakerType.Unknown,
                    EffectiveIntelligence = 0,
                    Reliability = "未知"
                };
            }

            bool isWiseRuler = faction.Leader.Intelligence > faction.Advisor.Intelligence;
            int effectiveIntelligence = Math.Max(faction.Leader.Intelligence, faction.Advisor.Intelligence);
            
            return new AdviceDisplayInfo
            {
                DisplayText = GetAdviceText(faction, originalAdvice, adviceType),
                DecisionMaker = isWiseRuler ? faction.Leader.Name : faction.Advisor.Name,
                DecisionType = isWiseRuler ? DecisionMakerType.WiseRuler : DecisionMakerType.Advisor,
                EffectiveIntelligence = effectiveIntelligence,
                Reliability = GetReliabilityDescription(effectiveIntelligence),
                SuccessRate = successRate,
                AdvisorName = faction.Advisor.Name,
                RulerName = faction.Leader.Name,
                IsWiseRulerMode = isWiseRuler
            };
        }

        /// <summary>
        /// 获取可信度描述
        /// </summary>
        private static string GetReliabilityDescription(int effectiveIntelligence)
        {
            if (effectiveIntelligence >= 95) return "料事如神";
            else if (effectiveIntelligence >= 90) return "深谋远虑";
            else if (effectiveIntelligence >= 80) return "颇有见地";
            else if (effectiveIntelligence >= 70) return "尚可参考";
            else if (effectiveIntelligence >= 60) return "见识有限";
            else return "难以信任";
        }

        /// <summary>
        /// 获取决策过程描述
        /// 用于显示决策的内部过程
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>决策过程描述</returns>
        public static string GetDecisionProcessDescription(Faction faction)
        {
            if (faction?.Leader == null || faction.Advisor == null)
                return "缺少决策者信息";

            bool isWiseRuler = faction.Leader.Intelligence > faction.Advisor.Intelligence;
            int intelligenceDiff = Math.Abs(faction.Leader.Intelligence - faction.Advisor.Intelligence);
            
            if (isWiseRuler)
            {
                if (intelligenceDiff >= 20)
                    return $"{faction.Leader.Name} 智力远超 {faction.Advisor.Name}，亲自把关所有决策";
                else if (intelligenceDiff >= 10)
                    return $"{faction.Leader.Name} 智力略胜 {faction.Advisor.Name}，能够识别并纠正错误";
                else
                    return $"{faction.Leader.Name} 与 {faction.Advisor.Name} 智力相当，但君主拥有最终决定权";
            }
            else
            {
                if (intelligenceDiff >= 30)
                    return $"{faction.Advisor.Name} 才华横溢，{faction.Leader.Name} 高度依赖其建议";
                else if (intelligenceDiff >= 20)
                    return $"{faction.Advisor.Name} 学识渊博，大幅提升 {faction.Leader.Name} 的决策水平";
                else if (intelligenceDiff >= 10)
                    return $"{faction.Advisor.Name} 颇有见地，有效辅佐 {faction.Leader.Name}";
                else
                    return $"{faction.Leader.Name} 与 {faction.Advisor.Name} 智力相当，君臣配合默契";
            }
        }
    }

    // AdviceType enum is defined in StrategistSystem.cs

    /// <summary>
    /// 决策者类型枚举
    /// </summary>
    public enum DecisionMakerType
    {
        Unknown,      // 未知
        Advisor,      // 军师决策
        WiseRuler,    // 明主决策
        Collaborative // 协作决策
    }

    /// <summary>
    /// 建议显示信息结构
    /// </summary>
    public class AdviceDisplayInfo
    {
        /// <summary>显示文本</summary>
        public string DisplayText { get; set; }
        
        /// <summary>决策者姓名</summary>
        public string DecisionMaker { get; set; }
        
        /// <summary>决策者类型</summary>
        public DecisionMakerType DecisionType { get; set; }
        
        /// <summary>有效智力</summary>
        public int EffectiveIntelligence { get; set; }
        
        /// <summary>可信度描述</summary>
        public string Reliability { get; set; }
        
        /// <summary>成功率（如果适用）</summary>
        public int SuccessRate { get; set; }
        
        /// <summary>军师姓名</summary>
        public string AdvisorName { get; set; }
        
        /// <summary>君主姓名</summary>
        public string RulerName { get; set; }
        
        /// <summary>是否为明主模式</summary>
        public bool IsWiseRulerMode { get; set; }
        
        /// <summary>
        /// 获取完整的显示信息
        /// </summary>
        public string GetFullDisplayText()
        {
            var result = DisplayText;
            
            if (SuccessRate >= 0)
            {
                result += $"\n\n成功率: {SuccessRate}%";
            }
            
            result += $"\n可信度: {Reliability} (有效智力 {EffectiveIntelligence})";
            
            return result;
        }
        
        /// <summary>
        /// 获取决策者标签
        /// </summary>
        public string GetDecisionMakerLabel()
        {
            switch (DecisionType)
            {
                case DecisionMakerType.WiseRuler:
                    return $"明主 {DecisionMaker}";
                case DecisionMakerType.Advisor:
                    return $"军师 {DecisionMaker}";
                default:
                    return DecisionMaker;
            }
        }
    }
}