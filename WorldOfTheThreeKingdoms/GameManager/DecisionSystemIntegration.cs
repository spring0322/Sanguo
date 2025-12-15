using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 决策系统集成类 - 将增强决策系统与游戏各模块整合
    /// 提供统一的决策接口和智能建议系统
    /// </summary>
    public static class DecisionSystemIntegration
    {
        /// <summary>
        /// 执行完整的AI决策流程
        /// 包含建议生成、决策判断、结果执行的完整链条
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <param name="decisionContext">决策上下文</param>
        /// <returns>决策结果</returns>
        public static DecisionResult ExecuteAIDecision(Faction faction, DecisionContext decisionContext)
        {
            if (faction?.Leader == null)
            {
                return new DecisionResult
                {
                    Decision = false,
                    Reason = "缺少君主，无法决策",
                    DecisionMaker = "系统",
                    Confidence = 0
                };
            }

            // 1. 生成建议内容
            string advice = GenerateAdviceContent(faction, decisionContext);
            
            // 2. 获取预测成功率（如果适用）
            int successRate = GetPredictedSuccessRate(faction, decisionContext);
            
            // 3. 执行决策判断
            bool decision = faction.AICheckDecision();
            
            // 4. 获取决策者信息
            var decisionMaker = GetDecisionMakerInfo(faction);
            
            // 5. 生成显示文本
            var displayInfo = AdviceDisplaySystem.GetAdviceDisplayInfo(
                faction, advice, decisionContext.AdviceType, successRate);
            
            // 6. 记录决策历史
            RecordDecisionHistory(faction, decisionContext, decision, displayInfo);
            
            // 7. 返回完整结果
            return new DecisionResult
            {
                Decision = decision,
                Reason = GetDecisionReason(faction, decision, decisionContext),
                DecisionMaker = decisionMaker.Name,
                DecisionType = decisionMaker.Type,
                DisplayText = displayInfo.GetFullDisplayText(),
                Confidence = CalculateConfidence(faction, decisionContext),
                SuccessRate = successRate,
                EffectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction),
                Context = decisionContext
            };
        }

        /// <summary>
        /// 生成建议内容
        /// </summary>
        private static string GenerateAdviceContent(Faction faction, DecisionContext context)
        {
            switch (context.DecisionType)
            {
                case DecisionType.Military:
                    return GenerateMilitaryAdvice(faction, context);
                case DecisionType.Diplomatic:
                    return GenerateDiplomaticAdvice(faction, context);
                case DecisionType.Recruitment:
                    return GenerateRecruitmentAdvice(faction, context);
                case DecisionType.Internal:
                    return GenerateInternalAdvice(faction, context);
                case DecisionType.Battle:
                    return GenerateBattleAdvice(faction, context);
                default:
                    return "建议谨慎行事，三思而后行。";
            }
        }

        /// <summary>
        /// 生成军事建议
        /// </summary>
        private static string GenerateMilitaryAdvice(Faction faction, DecisionContext context)
        {
            var adviceTemplates = new[]
            {
                "敌军{0}，正是{1}之良机！",
                "我军{2}，当{3}以制敌。",
                "观敌军阵势{4}，宜{5}应对。",
                "此时{6}，必能{7}！"
            };

            var enemyStates = new[] { "士气低落", "粮草不济", "远道而来", "立足未稳" };
            var actions = new[] { "进攻", "包围", "奇袭", "正面强攻" };
            var ourStates = new[] { "兵强马壮", "占据地利", "士气高昂", "准备充分" };
            var strategies = new[] { "速战速决", "持久消耗", "声东击西", "分兵合击" };
            var formations = new[] { "松散", "密集", "分散", "集中" };
            var responses = new[] { "火攻", "水攻", "正面冲击", "迂回包抄" };
            var timings = new[] { "天时地利", "敌疲我逸", "风向有利", "月黑风高" };
            var results = new[] { "大破敌军", "全歼来敌", "重创敌军", "一举成功" };

            var template = adviceTemplates[GameObject.Random(adviceTemplates.Length)];
            
            return string.Format(template,
                enemyStates[GameObject.Random(enemyStates.Length)],
                actions[GameObject.Random(actions.Length)],
                ourStates[GameObject.Random(ourStates.Length)],
                strategies[GameObject.Random(strategies.Length)],
                formations[GameObject.Random(formations.Length)],
                responses[GameObject.Random(responses.Length)],
                timings[GameObject.Random(timings.Length)],
                results[GameObject.Random(results.Length)]);
        }

        /// <summary>
        /// 生成外交建议
        /// </summary>
        private static string GenerateDiplomaticAdvice(Faction faction, DecisionContext context)
        {
            var templates = new[]
            {
                "与{0}结盟，可{1}，实为上策。",
                "当前{2}，宜{3}以观其变。",
                "彼方{4}，正是{5}之机。"
            };

            var targets = new[] { "东吴", "蜀汉", "曹魏", "袁氏", "刘表" };
            var benefits = new[] { "制衡强敌", "共抗曹操", "稳固后方", "互通有无" };
            var situations = new[] { "天下纷争", "群雄并起", "强敌环伺", "局势未明" };
            var actions = new[] { "静观其变", "暗中联络", "派遣使者", "示好修好" };
            var conditions = new[] { "内忧外患", "新败求和", "实力大损", "孤立无援" };
            var opportunities = new[] { "拉拢", "招降", "结盟", "和谈" };

            var template = templates[GameObject.Random(templates.Length)];
            
            return string.Format(template,
                targets[GameObject.Random(targets.Length)],
                benefits[GameObject.Random(benefits.Length)],
                situations[GameObject.Random(situations.Length)],
                actions[GameObject.Random(actions.Length)],
                conditions[GameObject.Random(conditions.Length)],
                opportunities[GameObject.Random(opportunities.Length)]);
        }

        /// <summary>
        /// 生成招募建议
        /// </summary>
        private static string GenerateRecruitmentAdvice(Faction faction, DecisionContext context)
        {
            if (context.TargetPerson != null)
            {
                var person = context.TargetPerson;
                var (predictedRate, comment) = StrategistManager.GetRecruitPrediction(faction, person);
                
                return $"关于招募 {person.Name}：{comment}";
            }

            return "当广纳贤才，以壮我军声势。";
        }

        /// <summary>
        /// 生成内政建议
        /// </summary>
        private static string GenerateInternalAdvice(Faction faction, DecisionContext context)
        {
            var templates = new[]
            {
                "当前{0}，宜{1}以固根本。",
                "民心{2}，需{3}以安民。",
                "府库{4}，应{5}以备不时之需。"
            };

            var situations = new[] { "百废待兴", "初定新地", "连年征战", "天下未定" };
            var actions = new[] { "发展农业", "减免赋税", "修建设施", "整顿吏治" };
            var morale = new[] { "思定", "不安", "向背", "归附" };
            var policies = new[] { "施行仁政", "轻徭薄赋", "开仓赈济", "安抚百姓" };
            var treasury = new[] { "充盈", "空虚", "紧张", "不足" };
            var measures = new[] { "开源节流", "发展商业", "增加税收", "节约开支" };

            var template = templates[GameObject.Random(templates.Length)];
            
            return string.Format(template,
                situations[GameObject.Random(situations.Length)],
                actions[GameObject.Random(actions.Length)],
                morale[GameObject.Random(morale.Length)],
                policies[GameObject.Random(policies.Length)],
                treasury[GameObject.Random(treasury.Length)],
                measures[GameObject.Random(measures.Length)]);
        }

        /// <summary>
        /// 生成战斗建议
        /// </summary>
        private static string GenerateBattleAdvice(Faction faction, DecisionContext context)
        {
            if (context.TargetTroop != null && context.SkillType.HasValue)
            {
                var prediction = StrategistManager.GetBattlePrediction(faction, context.TargetTroop, context.SkillType.Value);
                var skillName = GetSkillName(context.SkillType.Value);
                
                if (prediction >= 70)
                    return $"对 {context.TargetTroop.Leader?.Name} 部队使用{skillName}，成功率极高！";
                else if (prediction >= 50)
                    return $"对 {context.TargetTroop.Leader?.Name} 部队使用{skillName}，尚有胜算。";
                else if (prediction >= 30)
                    return $"对 {context.TargetTroop.Leader?.Name} 部队使用{skillName}，风险较大。";
                else
                    return $"对 {context.TargetTroop.Leader?.Name} 部队使用{skillName}，恐难成功。";
            }

            return "当审时度势，择机而动。";
        }

        /// <summary>
        /// 获取预测成功率
        /// </summary>
        private static int GetPredictedSuccessRate(Faction faction, DecisionContext context)
        {
            switch (context.DecisionType)
            {
                case DecisionType.Recruitment:
                    if (context.TargetPerson != null)
                    {
                        var (rate, _) = StrategistManager.GetRecruitPrediction(faction, context.TargetPerson);
                        return rate;
                    }
                    break;

                case DecisionType.Battle:
                    if (context.TargetTroop != null && context.SkillType.HasValue)
                    {
                        return StrategistManager.GetBattlePrediction(faction, context.TargetTroop, context.SkillType.Value);
                    }
                    break;

                case DecisionType.Diplomatic:
                    if (context.TargetFaction != null)
                    {
                        var result = StrategistManager.PredictDiplomacy(faction, context.TargetFaction, context.DiplomaticAction ?? "结盟");
                        return result.SuccessRate;
                    }
                    break;
            }

            return -1; // 无法预测
        }

        /// <summary>
        /// 获取决策者信息
        /// </summary>
        private static (string Name, DecisionMakerType Type) GetDecisionMakerInfo(Faction faction)
        {
            if (faction?.Leader == null || faction.Advisor == null)
                return ("未知", DecisionMakerType.Unknown);

            bool isWiseRuler = faction.Leader.Intelligence > faction.Advisor.Intelligence;
            
            if (isWiseRuler)
                return (faction.Leader.Name, DecisionMakerType.WiseRuler);
            else
                return (faction.Advisor.Name, DecisionMakerType.Advisor);
        }

        /// <summary>
        /// 记录决策历史
        /// </summary>
        private static void RecordDecisionHistory(Faction faction, DecisionContext context, bool decision, AdviceDisplayInfo displayInfo)
        {
            string historyEntry = $"{context.DecisionType}决策: {(decision ? "采纳" : "拒绝")} - {displayInfo.GetDecisionMakerLabel()}";
            faction.AddAdviceToLog(historyEntry);
            
            System.Diagnostics.Debug.WriteLine($"[DecisionHistory] {faction.Name}: {historyEntry}");
        }

        /// <summary>
        /// 获取决策理由
        /// </summary>
        private static string GetDecisionReason(Faction faction, bool decision, DecisionContext context)
        {
            bool isWiseRuler = faction.Leader?.Intelligence > faction.Advisor?.Intelligence;
            
            if (decision)
            {
                if (isWiseRuler)
                    return $"{faction.Leader.Name} 认为此策可行";
                else
                    return $"{faction.Leader.Name} 采纳了 {faction.Advisor.Name} 的建议";
            }
            else
            {
                if (isWiseRuler)
                {
                    if (faction.Advisor?.Loyalty < 80)
                        return $"{faction.Leader.Name} 识破了不良建议";
                    else
                        return $"{faction.Leader.Name} 认为此策不妥";
                }
                else
                {
                    return $"{faction.Leader.Name} 刚愎自用，拒绝了 {faction.Advisor.Name} 的建议";
                }
            }
        }

        /// <summary>
        /// 计算决策信心度
        /// </summary>
        private static int CalculateConfidence(Faction faction, DecisionContext context)
        {
            int effectiveInt = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
            int baseConfidence = Math.Min(95, Math.Max(30, effectiveInt));
            
            // 根据决策类型调整
            switch (context.DecisionType)
            {
                case DecisionType.Military:
                    baseConfidence += (faction.Leader?.Command ?? 70) - 70;
                    break;
                case DecisionType.Diplomatic:
                    baseConfidence += (faction.Advisor?.Politics ?? 70) - 70;
                    break;
                case DecisionType.Battle:
                    baseConfidence += (faction.Leader?.Strength ?? 70) - 70;
                    break;
            }
            
            return Math.Min(95, Math.Max(10, baseConfidence));
        }

        /// <summary>
        /// 获取技能名称
        /// </summary>
        private static string GetSkillName(StrategistManager.SkillType skillType)
        {
            switch (skillType)
            {
                case StrategistManager.SkillType.FirePlot: return "火计";
                case StrategistManager.SkillType.WaterPlot: return "水计";
                case StrategistManager.SkillType.Ambush: return "伏兵";
                case StrategistManager.SkillType.Provoke: return "挑衅";
                case StrategistManager.SkillType.Confuse: return "混乱";
                case StrategistManager.SkillType.Retreat: return "撤退";
                case StrategistManager.SkillType.Rally: return "鼓舞";
                default: return "未知技能";
            }
        }

        /// <summary>
        /// 批量执行AI决策
        /// 用于回合制游戏中处理多个AI势力的决策
        /// </summary>
        /// <param name="factions">AI势力列表</param>
        /// <param name="contexts">决策上下文列表</param>
        /// <returns>决策结果列表</returns>
        public static List<DecisionResult> ExecuteBatchDecisions(List<Faction> factions, List<DecisionContext> contexts)
        {
            var results = new List<DecisionResult>();
            
            for (int i = 0; i < Math.Min(factions.Count, contexts.Count); i++)
            {
                var result = ExecuteAIDecision(factions[i], contexts[i]);
                results.Add(result);
                
                // 添加延迟以避免过快的决策
                System.Threading.Thread.Sleep(10);
            }
            
            return results;
        }

        /// <summary>
        /// 获取势力决策统计
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>决策统计信息</returns>
        public static DecisionStatistics GetDecisionStatistics(Faction faction)
        {
            var stats = new DecisionStatistics
            {
                FactionName = faction.Name,
                EffectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction),
                DecisionMode = faction.Leader?.Intelligence > faction.Advisor?.Intelligence ? "明主决策" : "纳谏决策",
                RecentDecisions = faction.AdviceLog.Count,
                WiseRulerAnalysis = EffectiveIntelligenceSystem.GetWiseRulerAnalysis(faction)
            };

            return stats;
        }
    }

    /// <summary>
    /// 决策上下文 - 包含决策所需的所有信息
    /// </summary>
    public class DecisionContext
    {
        public DecisionType DecisionType { get; set; }
        public AdviceType AdviceType { get; set; }
        public Person TargetPerson { get; set; }
        public Troop TargetTroop { get; set; }
        public Faction TargetFaction { get; set; }
        public StrategistManager.SkillType? SkillType { get; set; }
        public string DiplomaticAction { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 决策结果 - 包含决策的完整信息
    /// </summary>
    public class DecisionResult
    {
        public bool Decision { get; set; }
        public string Reason { get; set; }
        public string DecisionMaker { get; set; }
        public DecisionMakerType DecisionType { get; set; }
        public string DisplayText { get; set; }
        public int Confidence { get; set; }
        public int SuccessRate { get; set; }
        public int EffectiveIntelligence { get; set; }
        public DecisionContext Context { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 决策类型枚举
    /// </summary>
    public enum DecisionType
    {
        Military,     // 军事决策
        Diplomatic,   // 外交决策
        Recruitment,  // 招募决策
        Internal,     // 内政决策
        Battle,       // 战斗决策
        Strategic     // 战略决策
    }

    /// <summary>
    /// 决策统计信息
    /// </summary>
    public class DecisionStatistics
    {
        public string FactionName { get; set; }
        public int EffectiveIntelligence { get; set; }
        public string DecisionMode { get; set; }
        public int RecentDecisions { get; set; }
        public string WiseRulerAnalysis { get; set; }
    }
}