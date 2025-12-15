using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// AI军师任命系统 - 根据君主性格选择军师
    /// </summary>
    public static class AIAdvisorAppointmentSystem
    {
        /// <summary>
        /// AI任命军师的改进方法
        /// </summary>
        /// <param name="faction">AI势力</param>
        public static void AIAppointAdvisor(Faction faction)
        {
            if (faction == null || faction.Leader == null) return;
            
            // 只有非玩家势力才使用AI逻辑
            if (Session.Current.Scenario.IsPlayer(faction)) return;
            
            if (!faction.AppointAdvisorAvail()) return;
            
            PersonList candidates = faction.AIAdvisorCandicate;
            if (candidates.Count == 0) return;

            // System.Diagnostics.Debug.WriteLine($"[AI任命军师] {faction.Name} 开始选择军师，候选人数: {candidates.Count}");

            // 【核心改进】根据君主性格选择军师
            Person selectedCandidate = SelectAdvisorByPersonality(faction, candidates);
            
            if (selectedCandidate == null)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI任命军师] {faction.Name} 未找到合适的军师候选人");
                return;
            }

            // 决定是否需要更换军师
            bool shouldAppoint = ShouldAppointNewAdvisor(faction, selectedCandidate);
            
            if (shouldAppoint)
            {
                // System.Diagnostics.Debug.WriteLine($"[AI任命军师] {faction.LeaderName} 任命 {selectedCandidate.Name} 为军师");
                
                faction.AdvisorID = selectedCandidate.ID;
                faction.AppointAdvisor(selectedCandidate);
                
                // 记录任命原因（用于调试和玩家情报）
                string reason = GetAppointmentReason(faction.Leader, selectedCandidate);
                // System.Diagnostics.Debug.WriteLine($"[AI任命军师] 任命原因: {reason}");
            }
        }

        /// <summary>
        /// 根据君主性格选择军师
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="candidates">候选人列表</param>
        /// <returns>选中的军师</returns>
        private static Person SelectAdvisorByPersonality(Faction faction, PersonList candidates)
        {
            if (candidates.Count == 0) return null;
            
            Person leader = faction.Leader;
            int personalityId = leader.Character?.ID ?? 0;
            
            // System.Diagnostics.Debug.WriteLine($"[AI选择军师] {leader.Name} 的性格ID: {personalityId}");

            switch (personalityId)
            {
                case 0: // 仁德型 - 重视品德和忠诚
                    return SelectByVirtue(leader, candidates);
                    
                case 1: // 霸道型 - 重视能力，但也看重忠诚
                    return SelectByAbilityAndLoyalty(leader, candidates);
                    
                case 2: // 冷静型 - 理性选择，重视智力
                    return SelectByIntelligence(leader, candidates);
                    
                case 3: // 莽撞型/昏庸型 - 可能做出错误选择
                    return SelectByImpulse(leader, candidates);
                    
                case 4: // 狡诈型 - 重视智谋，但可能任人唯亲
                    return SelectByCunning(leader, candidates);
                    
                default:
                    // 默认选择智力最高的
                    return SelectByIntelligence(leader, candidates);
            }
        }

        /// <summary>
        /// 仁德型选择：重视品德和忠诚
        /// </summary>
        private static Person SelectByVirtue(Person leader, PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[仁德型选择] 重视品德和忠诚");
            
            // 优先选择忠诚度高且智力不错的
            var virtuous = candidates
                .Where(c => c.Loyalty >= 80 && c.Intelligence >= 70)
                .OrderByDescending(c => c.Loyalty)
                .ThenByDescending(c => c.Intelligence)
                .FirstOrDefault();
                
            if (virtuous != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[仁德型选择] 选择高忠诚候选人: {virtuous.Name} (忠诚{virtuous.Loyalty} 智力{virtuous.Intelligence})");
                return virtuous;
            }
            
            // 如果没有高忠诚的，选择智力最高的
            return candidates.OrderByDescending(c => c.Intelligence).First();
        }

        /// <summary>
        /// 霸道型选择：重视能力，但也看重忠诚
        /// </summary>
        private static Person SelectByAbilityAndLoyalty(Person leader, PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[霸道型选择] 重视能力和忠诚的平衡");
            
            // 综合评分：智力 * 0.7 + 忠诚度 * 0.3
            var best = candidates
                .Select(c => new { 
                    Person = c, 
                    Score = c.Intelligence * 0.7 + c.Loyalty * 0.3 
                })
                .OrderByDescending(x => x.Score)
                .First();
                
            // System.Diagnostics.Debug.WriteLine($"[霸道型选择] 选择综合最佳: {best.Person.Name} (智力{best.Person.Intelligence} 忠诚{best.Person.Loyalty} 综合分{best.Score:F1})");
            return best.Person;
        }

        /// <summary>
        /// 冷静型选择：理性选择，重视智力
        /// </summary>
        private static Person SelectByIntelligence(Person leader, PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[冷静型选择] 理性选择智力最高者");
            
            var smartest = candidates.OrderByDescending(c => c.Intelligence).First();
            // System.Diagnostics.Debug.WriteLine($"[冷静型选择] 选择最高智力: {smartest.Name} (智力{smartest.Intelligence})");
            return smartest;
        }

        /// <summary>
        /// 莽撞型/昏庸型选择：可能做出错误选择
        /// </summary>
        private static Person SelectByImpulse(Person leader, PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[莽撞型选择] 可能做出冲动或错误的选择");
            
            // 50% 概率做出错误选择
            if (Utility.Random(100) < 50 && candidates.Count > 1)
            {
                // 可能选择魅力高但智力不是最高的
                var charmingButNotSmartest = candidates
                    .Where(c => c != candidates.OrderByDescending(x => x.Intelligence).First()) // 排除智力最高的
                    .OrderByDescending(c => c.Charm)
                    .FirstOrDefault();
                    
                if (charmingButNotSmartest != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 冲动选择魅力高者: {charmingButNotSmartest.Name} (魅力{charmingButNotSmartest.Charm} 智力{charmingButNotSmartest.Intelligence})");
                    return charmingButNotSmartest;
                }
                
                // 或者随机选择前几名中的一个
                int randomIndex = Utility.Random(Math.Min(3, candidates.Count));
                var randomChoice = candidates.OrderByDescending(c => c.Intelligence).Skip(randomIndex).First();
                // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 随机选择: {randomChoice.Name} (智力{randomChoice.Intelligence})");
                return randomChoice;
            }
            
            // 50% 概率还是选择智力最高的
            var smartest = candidates.OrderByDescending(c => c.Intelligence).First();
            // System.Diagnostics.Debug.WriteLine($"[莽撞型选择] 偶然选对: {smartest.Name} (智力{smartest.Intelligence})");
            return smartest;
        }

        /// <summary>
        /// 狡诈型选择：重视智谋，但可能任人唯亲
        /// </summary>
        private static Person SelectByCunning(Person leader, PersonList candidates)
        {
            // System.Diagnostics.Debug.WriteLine("[狡诈型选择] 重视智谋，但可能任人唯亲");
            
            // 30% 概率任人唯亲（选择关系好的）
            if (Utility.Random(100) < 30)
            {
                // 寻找有特殊关系的候选人
                var related = candidates.FirstOrDefault(c => HasSpecialRelation(leader, c));
                if (related != null && related.Intelligence >= 60) // 至少要有基本智力
                {
                    // System.Diagnostics.Debug.WriteLine($"[狡诈型选择] 任人唯亲: {related.Name} (智力{related.Intelligence})");
                    return related;
                }
            }
            
            // 70% 概率选择智力高的
            var smartest = candidates
                .Where(c => c.Intelligence >= 75) // 狡诈型对智力有一定要求
                .OrderByDescending(c => c.Intelligence)
                .FirstOrDefault();
                
            if (smartest != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[狡诈型选择] 选择高智力: {smartest.Name} (智力{smartest.Intelligence})");
                return smartest;
            }
            
            // 如果没有高智力的，选择最好的
            return candidates.OrderByDescending(c => c.Intelligence).First();
        }

        /// <summary>
        /// 检查是否有特殊关系
        /// </summary>
        private static bool HasSpecialRelation(Person leader, Person candidate)
        {
            // 检查各种特殊关系
            if (leader.Father == candidate || candidate.Father == leader) return true; // 父子
            if (leader.Spouse == candidate || candidate.Spouse == leader) return true; // 配偶
            if (leader.Brothers.Contains(candidate)) return true; // 兄弟
            
            // 检查亲密关系
            if (leader.CheckRelation(candidate) == 1) return true; // 亲密关系
            
            // 检查同乡关系（如果有相关数据）
            // if (leader.Hometown == candidate.Hometown) return true;
            
            return false;
        }

        /// <summary>
        /// 判断是否应该任命新军师
        /// </summary>
        private static bool ShouldAppointNewAdvisor(Faction faction, Person candidate)
        {
            // 如果没有军师，直接任命
            if (faction.Advisor == null)
            {
                // System.Diagnostics.Debug.WriteLine("[任命判断] 无现任军师，直接任命");
                return true;
            }
            
            Person currentAdvisor = faction.Advisor;
            
            // 根据君主性格决定更换标准
            int personalityId = faction.Leader.Character?.ID ?? 0;
            
            switch (personalityId)
            {
                case 0: // 仁德型 - 不轻易更换，除非新人明显更好
                    bool shouldReplaceVirtuous = candidate.Intelligence > currentAdvisor.Intelligence + 15 ||
                                               (candidate.Loyalty > currentAdvisor.Loyalty + 20 && candidate.Intelligence >= currentAdvisor.Intelligence - 5);
                    // System.Diagnostics.Debug.WriteLine($"[仁德型判断] 是否更换: {shouldReplaceVirtuous}");
                    return shouldReplaceVirtuous;
                    
                case 1: // 霸道型 - 追求更强的能力
                    bool shouldReplaceAmbitious = candidate.Intelligence > currentAdvisor.Intelligence + 10;
                    // System.Diagnostics.Debug.WriteLine($"[霸道型判断] 是否更换: {shouldReplaceAmbitious}");
                    return shouldReplaceAmbitious;
                    
                case 2: // 冷静型 - 理性比较
                    bool shouldReplaceRational = candidate.Intelligence > currentAdvisor.Intelligence + 8;
                    // System.Diagnostics.Debug.WriteLine($"[冷静型判断] 是否更换: {shouldReplaceRational}");
                    return shouldReplaceRational;
                    
                case 3: // 莽撞型 - 可能冲动更换
                    // 30% 概率冲动更换（即使新人不一定更好）
                    if (Utility.Random(100) < 30)
                    {
                        // System.Diagnostics.Debug.WriteLine("[莽撞型判断] 冲动更换军师");
                        return true;
                    }
                    // 否则需要明显更好才换
                    bool shouldReplaceImpulsive = candidate.Intelligence > currentAdvisor.Intelligence + 20;
                    // System.Diagnostics.Debug.WriteLine($"[莽撞型判断] 理性判断是否更换: {shouldReplaceImpulsive}");
                    return shouldReplaceImpulsive;
                    
                case 4: // 狡诈型 - 可能因为关系更换
                    // 如果新候选人有特殊关系，可能更换
                    if (HasSpecialRelation(faction.Leader, candidate) && candidate.Intelligence >= currentAdvisor.Intelligence - 10)
                    {
                        // System.Diagnostics.Debug.WriteLine("[狡诈型判断] 因关系更换军师");
                        return true;
                    }
                    // 否则需要智力明显更高
                    bool shouldReplaceCunning = candidate.Intelligence > currentAdvisor.Intelligence + 12;
                    // System.Diagnostics.Debug.WriteLine($"[狡诈型判断] 能力判断是否更换: {shouldReplaceCunning}");
                    return shouldReplaceCunning;
                    
                default:
                    return candidate.Intelligence > currentAdvisor.Intelligence + 10;
            }
        }

        /// <summary>
        /// 获取任命原因说明
        /// </summary>
        private static string GetAppointmentReason(Person leader, Person advisor)
        {
            int personalityId = leader.Character?.ID ?? 0;
            
            switch (personalityId)
            {
                case 0: // 仁德型
                    if (advisor.Loyalty >= 80)
                        return $"{leader.Name} 看重 {advisor.Name} 的忠诚品德";
                    else
                        return $"{leader.Name} 认为 {advisor.Name} 德才兼备";
                        
                case 1: // 霸道型
                    return $"{leader.Name} 欣赏 {advisor.Name} 的才能，决定重用";
                    
                case 2: // 冷静型
                    return $"{leader.Name} 经过理性分析，认为 {advisor.Name} 最适合担任军师";
                    
                case 3: // 莽撞型
                    if (advisor.Intelligence < 80)
                        return $"{leader.Name} 冲动地选择了 {advisor.Name}，可能不是最佳选择";
                    else
                        return $"{leader.Name} 意外地选对了人才 {advisor.Name}";
                        
                case 4: // 狡诈型
                    if (HasSpecialRelation(leader, advisor))
                        return $"{leader.Name} 任人唯亲，提拔了关系密切的 {advisor.Name}";
                    else
                        return $"{leader.Name} 看中了 {advisor.Name} 的智谋";
                        
                default:
                    return $"{leader.Name} 任命 {advisor.Name} 为军师";
            }
        }

        /// <summary>
        /// 获取AI军师任命的详细分析
        /// </summary>
        public static string GetAppointmentAnalysis(Faction faction, PersonList candidates)
        {
            if (faction?.Leader == null || candidates.Count == 0)
                return "无法进行军师任命分析";
                
            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== {faction.Name} 军师任命分析 ===");
            analysis.AppendLine($"君主: {faction.Leader.Name}");
            
            string personalityName = GetPersonalityName(faction.Leader.Character?.ID ?? 0);
            analysis.AppendLine($"性格: {personalityName}");
            
            if (faction.Advisor != null)
            {
                analysis.AppendLine($"现任军师: {faction.Advisor.Name} (智力{faction.Advisor.Intelligence})");
            }
            else
            {
                analysis.AppendLine("现任军师: 无");
            }
            
            analysis.AppendLine();
            analysis.AppendLine("候选人分析:");
            
            foreach (var candidate in candidates.Take(5)) // 只显示前5个
            {
                analysis.AppendLine($"  {candidate.Name}: 智力{candidate.Intelligence} 忠诚{candidate.Loyalty} 魅力{candidate.Charm}");
                
                if (HasSpecialRelation(faction.Leader, candidate))
                {
                    analysis.AppendLine($"    ★ 与君主有特殊关系");
                }
            }
            
            // 预测选择
            Person predicted = SelectAdvisorByPersonality(faction, candidates);
            if (predicted != null)
            {
                analysis.AppendLine();
                analysis.AppendLine($"预测选择: {predicted.Name}");
                analysis.AppendLine($"选择原因: {GetAppointmentReason(faction.Leader, predicted)}");
            }
            
            return analysis.ToString();
        }

        /// <summary>
        /// 获取性格名称
        /// </summary>
        private static string GetPersonalityName(int personalityId)
        {
            switch (personalityId)
            {
                case 0: return "仁德型";
                case 1: return "霸道型";
                case 2: return "冷静型";
                case 3: return "莽撞型";
                case 4: return "狡诈型";
                default: return "未知";
            }
        }

        /// <summary>
        /// 模拟AI军师任命过程（用于测试）
        /// </summary>
        public static void SimulateAppointmentProcess(Faction faction, PersonList candidates)
        {
            Console.WriteLine($"=== 模拟 {faction.Name} 的军师任命过程 ===");
            
            string analysis = GetAppointmentAnalysis(faction, candidates);
            Console.WriteLine(analysis);
            
            Console.WriteLine("\n执行任命过程...");
            AIAppointAdvisor(faction);
            
            if (faction.Advisor != null)
            {
                Console.WriteLine($"✅ 任命结果: {faction.Advisor.Name} 被任命为军师");
            }
            else
            {
                Console.WriteLine("❌ 未任命军师");
            }
        }
    }
}