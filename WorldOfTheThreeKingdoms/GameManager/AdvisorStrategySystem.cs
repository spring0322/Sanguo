using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 策略类型枚举
    /// </summary>
    public enum StrategyKind
    {
        Gossip,         // 流言 (需高智力)
        Arson,          // 放火 (需高智力)
        Destruction,    // 破坏 (需高统率/智力)
        Instigate,      // 离间 (需高智力+魅力)
        Alliance,       // 结盟 (需高魅力/政治)
        Search,         // 搜索 (需高魅力/智力) - 修正：魅力为主，智力为辅
        JailBreak,      // 劫牢 (需高武力/统率)
        Convince,       // 说服 (需高魅力/政治) - 新增智能说服系统
        Assassinate,    // 暗杀 (需高武力/智力) - 新增智能暗杀系统
        EnhanceDiplomatic, // 亲善 (需高魅力/政治)
        TruceDiplomatic, // 停战 (需高政治/魅力)
        InduceSurrender  // 劝降 (需高魅力/政治)
    }

    /// <summary>
    /// 军师策略建议结果包
    /// </summary>
    public struct StrategyAdviceResult
    {
        public Person BestCandidate;      // 推荐的人选
        public int PredictedChance;       // 预测成功率 (0-100)
        public string AdvisorComment;     // 军师的点评文本
    }

    /// <summary>
    /// 军师外交建议结果包
    /// </summary>
    public class AdvisorDiplomaticAdvice
    {
        public bool IsRecommended { get; set; }        // 是否推荐执行
        public int SuccessRate { get; set; }           // 成功率预估 (0-100)
        public string ImportanceLevel { get; set; }    // 重要性等级
        public string ImportanceReason { get; set; }   // 重要性原因
        public string DetailedAnalysis { get; set; }   // 详细分析
        public Person BestCandidate { get; set; }      // 推荐的最佳人选
    }

    /// <summary>
    /// 军师策略建议系统 - 静态类，无需实例化
    /// </summary>
    public static class AdvisorStrategySystem
    {
        /// <summary>
        /// 获取军师对某项策略的建议
        /// </summary>
        public static StrategyAdviceResult GetAdvice(Person advisor, Faction faction, object target, StrategyKind strategy)
        {
            var result = new StrategyAdviceResult();
            
            if (advisor == null)
            {
                result.AdvisorComment = "（当前无军师，无法提供建议）";
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"[军师建议] {advisor.Name} 正在分析 {strategy} 策略");

            // 1. 挑选最佳执行人选
            result.BestCandidate = RecommendBestOfficer(faction, strategy);

            // 2. 计算并预测成功率
            int realChance = CalculateRealSuccessChance(result.BestCandidate, target, strategy);
            result.PredictedChance = PredictSuccessRate(advisor, realChance);

            // 3. 生成军师台词
            result.AdvisorComment = GenerateAdvisorComment(advisor, faction, result.BestCandidate, result.PredictedChance, strategy);

            System.Diagnostics.Debug.WriteLine($"[军师建议] 推荐人选: {result.BestCandidate?.Name ?? "无"}, 预测成功率: {result.PredictedChance}%");
            
            return result;
        }

        /// <summary>
        /// 推荐最佳武将逻辑
        /// </summary>
        private static Person RecommendBestOfficer(Faction faction, StrategyKind strategy)
        {
            // 获取所有可用武将（排除俘虏、外出、执行任务中等）
            var availablePersons = new List<Person>();
            foreach (Person p in faction.Persons)
            {
                // 检查基础状态
                if (p.Alive && !p.IsCaptive && p.Available && 
                    p.Status == PersonStatus.Normal && // 正常状态（非移动中）
                    p.OutsideTask == OutsideTaskKind.无) // 无外出任务
                {
                    availablePersons.Add(p);
                }
            }

            if (availablePersons.Count == 0) return null;

            System.Diagnostics.Debug.WriteLine($"[人选推荐] 可用人员数量: {availablePersons.Count}");

            Person bestCandidate = null;
            double bestScore = -1;

            foreach (Person p in availablePersons)
            {
                double score = 0;
                switch (strategy)
                {
                    case StrategyKind.Gossip:
                    case StrategyKind.Arson:
                        // 智力优先：智力决定计策成功率
                        score = p.Intelligence;
                        break;

                    case StrategyKind.Destruction:
                        // 破坏：需要统率（带队隐蔽/指挥）和智力（找弱点）
                        score = p.Command * 0.6 + p.Intelligence * 0.4;
                        break;

                    case StrategyKind.Instigate:
                        // 离间：纯粹的心理博弈，智力为主，魅力为辅（让人信服）
                        score = p.Intelligence * 0.7 + p.Glamour * 0.3;
                        break;

                    case StrategyKind.Alliance:
                        // 结盟：外交看政治（利益交换）和魅力（好感度）
                        score = p.Politics * 0.5 + p.Glamour * 0.5;
                        break;

                    case StrategyKind.EnhanceDiplomatic:
                        // 亲善：主要看政治和魅力
                        score = p.Politics * 0.4 + p.Glamour * 0.6;
                        break;

                    case StrategyKind.TruceDiplomatic:
                        // 停战：政治为主，魅力为辅（需要谈判技巧和说服力）
                        score = p.Politics * 0.6 + p.Glamour * 0.4;
                        break;

                    case StrategyKind.InduceSurrender:
                        // 劝降：需要极高魅力和政治（说服敌方整体投降）
                        score = p.Glamour * 0.5 + p.Politics * 0.5;
                        break;

                    case StrategyKind.Search:
                        // 搜索：严格按照设定修正
                        // 魅力：容易得到当地百姓指引，容易吸引在野人才
                        // 智力：敏锐的观察力，发现隐藏物品
                        // 权重：魅力 60% + 智力 40%
                        score = p.Glamour * 0.6 + p.Intelligence * 0.4;
                        break;

                    default:
                        score = p.Intelligence;
                        break;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = p;
                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// 计算真实成功率 (基于属性对抗)
        /// </summary>
        private static int CalculateRealSuccessChance(Person executor, object target, StrategyKind strategy)
        {
            if (executor == null) return 0;

            int baseChance = 0;

            // 针对搜索单独处理，因为它通常没有特定的对抗目标(target可能是null或者地区对象)
            if (strategy == StrategyKind.Search)
            {
                // 搜索成功率计算公式：
                // 基础概率 20% + (魅力+智力综合分 / 2.5)
                // 例如：魅力80，智力80 -> 综合160 -> 加成64% -> 总成84%
                // 例如：魅力30，智力30 -> 综合60  -> 加成24% -> 总成44%
                float attrFactor = (executor.Glamour * 0.6f + executor.Intelligence * 0.4f);
                baseChance = 20 + (int)(attrFactor / 2.5f);
                return Math.Max(0, Math.Min(100, baseChance));
            }

            // 其他对抗性策略
            if (target is Person targetPerson)
            {
                // 对人策略
                int diff = 0;
                switch (strategy)
                {
                    case StrategyKind.Gossip:
                        // 流言看智力差
                        diff = executor.Intelligence - targetPerson.Intelligence;
                        break;

                    case StrategyKind.Instigate:
                        // 离间看义理(隐藏属性)和智力差，这里简化为智力对抗
                        diff = executor.Intelligence - targetPerson.Intelligence;
                        // 如果对方义理高，成功率大减 (假设 PersonalLoyalty 0-4，越高越忠诚)
                        diff -= targetPerson.PersonalLoyalty * 15;
                        break;

                    case StrategyKind.Alliance:
                        // 外交看魅力对抗
                        diff = executor.Glamour - targetPerson.Glamour;
                        break;

                    case StrategyKind.EnhanceDiplomatic:
                        // 亲善看魅力对抗，但更容易成功
                        diff = executor.Glamour - targetPerson.Glamour;
                        baseChance = 70 + diff; // 基础成功率较高
                        return Math.Max(0, Math.Min(100, baseChance));

                    case StrategyKind.TruceDiplomatic:
                        // 停战看政治和魅力对抗，基础成功率较低（需要说服对方放下敌意）
                        diff = (executor.Politics + executor.Glamour) / 2 - (targetPerson.Politics + targetPerson.Glamour) / 2;
                        baseChance = 40 + diff; // 停战基础成功率较低
                        return Math.Max(0, Math.Min(100, baseChance));

                    case StrategyKind.InduceSurrender:
                        // 劝降看魅力和政治对抗，基础成功率最低（需要说服对方投降）
                        diff = (executor.Glamour + executor.Politics) / 2 - (targetPerson.Glamour + targetPerson.Politics) / 2;
                        baseChance = 30 + diff; // 劝降基础成功率很低
                        return Math.Max(0, Math.Min(100, baseChance));

                    default:
                        diff = executor.Intelligence - targetPerson.Intelligence;
                        break;
                }
                baseChance = 50 + diff; // 基础50% + 差值
            }
            else if (target is Architecture targetArch)
            {
                // 对城策略 (破坏/放火)
                // 防御方看太守智力，如果没有太守则看防御度
                int defenseValue = targetArch.Endurance / 50; // 使用Endurance代替Defense
                
                if (targetArch.Mayor != null)
                {
                    defenseValue += targetArch.Mayor.Intelligence;
                }
                else
                {
                    defenseValue += 30; // 空城默认只有低智商防御
                }

                int attackValue = strategy == StrategyKind.Arson ? executor.Intelligence : executor.Command;
                baseChance = 50 + (attackValue - defenseValue);
            }

            return Math.Max(0, Math.Min(100, baseChance));
        }

        /// <summary>
        /// 军师预测成功率 (加入智力误差)
        /// </summary>
        private static int PredictSuccessRate(Person advisor, int realChance)
        {
            // 智力100的神算子误差极小
            // 智力<60的庸才误差很大
            int errorRange = (110 - advisor.Intelligence);
            if (errorRange < 5) errorRange = 5; // 即使是诸葛亮也保留5%的天意不可测

            int noise = GameObject.Random(errorRange) - (errorRange / 2);
            int predicted = realChance + noise;

            return Math.Max(0, Math.Min(100, predicted));
        }

        /// <summary>
        /// 生成军师点评文本
        /// </summary>
        private static string GenerateAdvisorComment(Person advisor, Faction faction, Person candidate, int chance, StrategyKind strategy)
        {
            // 判断是否是君主自己进行预测（无军师情况）
            bool isSelfPrediction = (faction != null && advisor == faction.Leader);

            // 获取对君主的称呼 (如果是自言自语，则不需要称呼，但保留变量用于非自言自语情况)
            string leaderAddress = "主公";
            if (!isSelfPrediction && faction != null && faction.Leader != null)
            {
                leaderAddress = AppellationSettings.GetAddress(faction, advisor, faction.Leader);
                if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";
            }
            
            if (candidate == null)
            {
                if (isSelfPrediction)
                    return "眼下军中无人可堪此任，还是暂缓为妙。";
                else
                    return $"{leaderAddress}，眼下军中无人可堪此任，还是暂缓为妙。";
            }

            // 获取对执行人的称呼
            string candidateAddress;
            if (candidate == advisor)
            {
                candidateAddress = isSelfPrediction ? "我" : "微臣";
            }
            else
            {
                if (isSelfPrediction)
                {
                    candidateAddress = candidate.Name;
                }
                else
                {
                    candidateAddress = AppellationSettings.GetAddress(faction, advisor, candidate);
                    if (string.IsNullOrEmpty(candidateAddress)) candidateAddress = candidate.Name;
                }
            }

            // 针对搜索的特殊文本
            if (strategy == StrategyKind.Search)
            {
                string searchComment = "";
                if (chance >= 80) searchComment = "此人福缘深厚且观察敏锐，定能满载而归。";
                else if (chance >= 50) searchComment = "此去应当会有所斩获。";
                else searchComment = "虽无十足把握，但也聊胜于无。";

                if (isSelfPrediction)
                {
                    string whoAction = (candidate == advisor) ? $"由【{candidateAddress}】亲自前往" : $"派【{candidateAddress}】前往";
                    return $"若要搜寻遗才宝物，{whoAction}最为合适。{searchComment}";
                }
                else
                {
                    return $"若要搜寻遗才宝物，派【{candidateAddress}】前往最为合适。{searchComment}";
                }
            }

            // 通用难度文本
            string difficultyText = "";
            if (chance >= 90) 
            {
                if (isSelfPrediction) difficultyText = "此计万无一失";
                else difficultyText = $"此计万无一失，{leaderAddress}尽可放心";
            }
            else if (chance >= 70) difficultyText = "此计颇有胜算";
            else if (chance >= 40) difficultyText = "此计胜负难料，需看天意";
            else if (chance >= 20) difficultyText = "此计凶险万分，恐难成功";
            else difficultyText = "此去犹如飞蛾扑火，断不可行";

            if (isSelfPrediction)
            {
                string whoAction = (candidate == advisor) ? $"由【{candidateAddress}】亲自执行" : $"派【{candidateAddress}】执行";
                return $"依我看，{whoAction}此计最为妥当。{difficultyText}。";
            }
            else
            {
                return $"依臣之见，派【{candidateAddress}】执行此计最为妥当。{difficultyText}。";
            }
        }

        /// <summary>
        /// 获取策略类型的中文名称
        /// </summary>
        public static string GetStrategyName(StrategyKind strategy)
        {
            switch (strategy)
            {
                case StrategyKind.Gossip: return "流言";
                case StrategyKind.Arson: return "放火";
                case StrategyKind.Destruction: return "破坏";
                case StrategyKind.Instigate: return "离间";
                case StrategyKind.Alliance: return "结盟";
                case StrategyKind.EnhanceDiplomatic: return "亲善";
                case StrategyKind.TruceDiplomatic: return "停战";
                case StrategyKind.InduceSurrender: return "劝降";
                case StrategyKind.Search: return "搜索";
                default: return "未知策略";
            }
        }

        /// <summary>
        /// 获取策略类型的详细说明
        /// </summary>
        public static string GetStrategyDescription(StrategyKind strategy)
        {
            switch (strategy)
            {
                case StrategyKind.Gossip: 
                    return "散布流言以降低敌方士气，需要高智力人员执行";
                case StrategyKind.Arson: 
                    return "纵火烧毁敌方设施，需要高智力人员潜入";
                case StrategyKind.Destruction: 
                    return "破坏敌方建筑或设备，需要统率和智力兼备的人员";
                case StrategyKind.Instigate: 
                    return "挑拨离间敌方内部关系，需要高智力和魅力";
                case StrategyKind.Alliance: 
                    return "与其他势力结成同盟，需要高政治和魅力";
                case StrategyKind.EnhanceDiplomatic:
                    return "赠送金钱或宝物改善外交关系，需要高魅力和政治";
                case StrategyKind.TruceDiplomatic:
                    return "与敌对势力谈判停战协议，需要高政治和魅力";
                case StrategyKind.InduceSurrender:
                    return "劝说敌方势力全体投降，需要极高魅力和政治";
                case StrategyKind.Search: 
                    return "搜寻在野人才和珍贵宝物，需要高魅力和智力";
                default: 
                    return "未知策略类型";
            }
        }
    }
}