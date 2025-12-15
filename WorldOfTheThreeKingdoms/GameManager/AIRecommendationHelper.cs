using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// AI势力人才推荐助手 - 为AI势力提供智能的人才招募决策
    /// </summary>
    public class AIRecommendationHelper
    {
        // 引用之前的系统
        private AdvisorRecommendationSystem advisorSystem;

        public AIRecommendationHelper()
        {
            advisorSystem = new AdvisorRecommendationSystem();
        }

        /// <summary>
        /// 执行AI回合的人才推荐逻辑
        /// </summary>
        public void ExecuteAITurn(Faction aiFaction)
        {
            try
            {
                if (aiFaction == null || aiFaction.Destroyed)
                {
                    return;
                }

                // 1. 时间检查：只在1月执行
                if (Session.Current.Scenario.Date.Month != 1) return;

                // 2. 资金检查：如果连赏赐的钱都没有（例如少于 200），就不折腾了
                if (aiFaction.Fund < AIRecommendationConfig.MinGoldThreshold) return;

                // 3. 调用核心举荐逻辑 (复用玩家的判定逻辑，保证公平)
                Person foundTalent;
                int initialLoyalty;
                var result = advisorSystem.AttemptRecommendation(aiFaction, out foundTalent, out initialLoyalty);

                if (AIRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] {aiFaction.Name} 推荐结果: {result}");
                    if (foundTalent != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  发现人才: {foundTalent.Name}, 预计忠诚度: {initialLoyalty}");
                    }
                }

                // 4. 使用统一的结果处理方法
                advisorSystem.HandleRecommendationResult(aiFaction, result, foundTalent, initialLoyalty);

                // 5. AI特殊处理：对于仅发现的人才，AI需要决定是否录用
                if (result == AdvisorRecommendationSystem.RecommendationResult.Success_FoundOnly && foundTalent != null)
                {
                    // AI 需要评估是否录用这个被发现的人才
                    if (ShouldAIEmploy(aiFaction, foundTalent))
                    {
                        EmployTalent(aiFaction, foundTalent, initialLoyalty);
                    }
                    else if (AIRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] {aiFaction.Name} 发现了 {foundTalent.Name} 但决定不录用");
                    }
                }

                // 如果失败，AI 也会自动获得低保（在 AttemptRecommendation 内部已经处理了）
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 执行AI回合异常: {ex.Message}");
            }
        }

        /// <summary>
        /// AI 决策核心：决定是否录用这个人
        /// </summary>
        private bool ShouldAIEmploy(Faction faction, Person target)
        {
            try
            {
                // 策略 A：缺人时饥不择食
                // 如果武将数 < 城池数 * 3，只要是活人都要
                if (faction.Persons.Count < faction.Architectures.Count * AIRecommendationConfig.PersonsPerArchitectureRatio) 
                    return true;

                // 策略 B：精英路线
                // 如果人才稍微充裕，排除掉纯废柴（四维之和 < 150 且 最高属性 < 65）
                int totalStats = target.Strength + target.Intelligence + target.Politics + target.Glamour;
                int maxStat = Math.Max(Math.Max(target.Strength, target.Intelligence), 
                                     Math.Max(target.Politics, target.Glamour));
                if (totalStats < AIRecommendationConfig.MinTotalStatsThreshold && 
                    maxStat < AIRecommendationConfig.MinMaxStatThreshold) 
                    return false;

                // 策略 C：相性极差规避（防止录用后立刻叛变）
                // 如果相性差 > 60 且 魅力 < 50，录进来也是祸害，AI 可能会放弃
                // (高难度 AI 可以无视此条，强行录用后砸钱)
                int affinityDiff = GetAffinityDistance(faction.Leader.Ideal, target.Ideal);
                if (affinityDiff > AIRecommendationConfig.MaxAffinityDifference && 
                    target.Glamour < AIRecommendationConfig.MinGlamourForBadAffinity)
                {
                    // 高难度AI可以无视相性问题 (暂时简化为总是允许)
                    return faction.Fund > AIRecommendationConfig.MinGoldForRiskyRecruitment;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 评估录用决策异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 录用人才
        /// </summary>
        private void EmployTalent(Faction faction, Person target, int initialLoyalty)
        {
            try
            {
                // 1. 变更状态和位置 (让人才加入派系的正确方法)
                target.Status = PersonStatus.Normal;
                target.LocationArchitecture = faction.Capital; // 通过设置LocationArchitecture来加入派系
                target.LocationTroop = null; // 确保不在部队中

                // 2. 设置初始忠诚度 (通过 TempLoyaltyChange 调整)
                target.TempLoyaltyChange = initialLoyalty - target.Loyalty;

                // 3. AI 的自动赏赐逻辑
                // 如果忠诚度太低，AI 会尝试花钱买忠诚，防止下回合跑路
                if (target.Loyalty < AIRecommendationConfig.LoyaltyThresholdForReward && 
                    faction.Fund > AIRecommendationConfig.MinGoldForReward)
                {
                    int rewardAmount = AIRecommendationConfig.BaseRewardAmount;
                    int loyaltyIncrease = AIRecommendationConfig.BaseLoyaltyIncrease;
                    
                    // AI更舍得花钱 (暂时简化)
                    rewardAmount = (int)(rewardAmount * 1.2f);
                    loyaltyIncrease = (int)(loyaltyIncrease * 1.1f);

                    target.TempLoyaltyChange += loyaltyIncrease;
                    // 从首都减少资金
                    if (faction.Capital != null)
                    {
                        faction.Capital.DecreaseFund(rewardAmount);
                    }

                    if (AIRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] {faction.Name} 赏赐 {target.Name} {rewardAmount}金，忠诚度提升至 {target.Loyalty}");
                    }
                }

                // 4. 记录日志 (让玩家知道 AI 变强了)
                // 只有当 AI 录用了名将（最高属性 > 80）时才显示全局日志，避免刷屏
                int maxStat = Math.Max(Math.Max(target.Strength, target.Intelligence), 
                                     Math.Max(target.Politics, target.Glamour));
                if (maxStat > AIRecommendationConfig.FamousPersonThreshold)
                {
                    string message = $"传闻{faction.Name}的军师举荐了贤才【{target.Name}】。";
                    
                    // 添加到游戏消息系统 (简化版本)
                    if (AIRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 全局消息: {message}");
                    }

                    if (AIRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 全局消息: {message}");
                    }
                }

                if (AIRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] {faction.Name} 成功录用 {target.Name}，忠诚度: {target.Loyalty}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 录用人才异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 为AI选择最佳候选人（如果需要自定义选择逻辑）
        /// </summary>
        private Person SelectBestForAI(Faction faction, List<Person> candidates)
        {
            try
            {
                if (candidates == null || candidates.Count == 0)
                    return null;

                // 找出当前 AI 最缺的属性 (比如武将虽多但全是文官)
                var persons = faction.Persons.GetList().Cast<Person>().ToList();
                bool needFighter = persons.Count == 0 || persons.Average(p => p.Strength) < AIRecommendationConfig.NeedFighterThreshold;
                bool needAdmin = persons.Count == 0 || persons.Average(p => p.Politics) < AIRecommendationConfig.NeedAdminThreshold;

                return candidates.OrderByDescending(p => {
                    float score = p.Strength + p.Intelligence + p.Politics + p.Glamour;
                    
                    // 缺啥补啥
                    if (needFighter) score += p.Strength * AIRecommendationConfig.FighterBonusMultiplier;
                    if (needAdmin) score += p.Politics * AIRecommendationConfig.AdminBonusMultiplier;
                    
                    // 优先选相性近的（维护成本低）
                    int affinityDiff = GetAffinityDistance(faction.Leader.Ideal, p.Ideal);
                    score -= affinityDiff * AIRecommendationConfig.AffinityPenaltyMultiplier; // 距离越远分越低
                    
                    return score;
                }).FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIRecommendationHelper] 选择最佳候选人异常: {ex.Message}");
                return candidates.FirstOrDefault();
            }
        }

        /// <summary>
        /// 计算环形相性距离
        /// </summary>
        private int GetAffinityDistance(int ideal1, int ideal2)
        {
            int diff = Math.Abs(ideal1 - ideal2);
            if (diff > 75) return 150 - diff;
            return diff;
        }

        /// <summary>
        /// 获取AI推荐统计信息
        /// </summary>
        public string GetAIRecommendationStats()
        {
            // 可以返回AI推荐的统计信息，用于调试或显示
            return "AI推荐系统运行正常";
        }
    }
}