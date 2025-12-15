// ================================================================
// 军师系统完整集成包 - 核心代码文件
// 包含所有军师系统相关的核心功能代码
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace GameManager
{
    // ================================================================
    // 1. 军师推荐系统核心类
    // ================================================================
    
    /// <summary>
    /// 军师举荐系统 - 处理自动人才推荐功能
    /// </summary>
    public class AdvisorRecommendationSystem
    {
        /// <summary>
        /// 推荐结果枚举
        /// </summary>
        public enum RecommendationResult
        {
            None,                // 未运行（如：冷却中、无军师）
            Fail_NoTalent,       // 失败：客观没人
            Fail_LowAbility,     // 失败：军师能力不足
            Success_FoundOnly,   // 成功：发现了，但没招募过来（需要玩家手动去招）
            Success_DirectJoin   // 大成功：直接加入阵营
        }

        /// <summary>
        /// 推荐统计数据
        /// </summary>
        public class RecommendationStats
        {
            public int TotalAttempts { get; set; } = 0;
            public int SuccessfulRecommendations { get; set; } = 0;
            public int FailedNoTalent { get; set; } = 0;
            public int FailedLowAbility { get; set; } = 0;
            public int CooldownBlocked { get; set; } = 0;

            public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulRecommendations / TotalAttempts * 100 : 0;

            public string GetSummary()
            {
                return $"推荐统计: 总计{TotalAttempts}次, 成功{SuccessfulRecommendations}次 ({SuccessRate:F1}%), " +
                       $"无人才{FailedNoTalent}次, 能力不足{FailedLowAbility}次, 冷却阻止{CooldownBlocked}次";
            }
        }

        // 全局统计数据
        private static RecommendationStats _globalStats = new RecommendationStats();

        /// <summary>
        /// 尝试执行举荐（包含失败补偿逻辑）
        /// </summary>
        public RecommendationResult AttemptRecommendation(Faction faction, out Person foundPerson, out int initialLoyalty)
        {
            foundPerson = null;
            initialLoyalty = 0;
            Person advisor = faction.Advisor;
            int currentYear = Session.Current.Scenario.Date.Year;

            // 1. 频率检查：今年是否已经执行过？
            if (faction.LastTalentRecommendYear >= currentYear - AdvisorRecommendationConfig.RecommendationCooldownYears + 1)
            {
                _globalStats.CooldownBlocked++;
                return RecommendationResult.None;
            }

            // 记录尝试次数
            _globalStats.TotalAttempts++;
            faction.LastTalentRecommendYear = currentYear;

            // 2. 基础检查
            if (advisor == null || !advisor.Alive)
            {
                return RecommendationResult.None;
            }

            // 3. 智力门槛检查
            if (advisor.Intelligence < AdvisorRecommendationConfig.MinIntelligenceThreshold)
            {
                _globalStats.FailedLowAbility++;
                ApplyFailureCompensation(advisor);
                return RecommendationResult.Fail_LowAbility;
            }

            // 4. 搜寻范围：本势力已知区域的在野武将
            var candidates = new List<Person>();
            foreach (Person p in Session.Current.Scenario.Persons)
            {
                if (p.Status == GameObjects.PersonDetail.PersonStatus.NoFaction && 
                    p.Alive && 
                    !p.IsCaptive &&
                    p.Available && 
                    faction.IsPositionKnown(p.Position))
                {
                    candidates.Add(p);
                }
            }

            // 5. 判定：真的没人吗？
            if (candidates.Count == 0)
            {
                _globalStats.FailedNoTalent++;
                ApplyFailureCompensation(advisor);
                return RecommendationResult.Fail_NoTalent;
            }

            // 6. 判定：智力检定
            int successRate = AdvisorRecommendationConfig.CalculateSuccessRate(advisor.Intelligence);
            int roll = GameObject.Random(100);

            if (roll >= successRate)
            {
                _globalStats.FailedLowAbility++;
                ApplyFailureCompensation(advisor);
                return RecommendationResult.Fail_LowAbility;
            }

            // 7. 成功：挑选最合适的一个
            foundPerson = SelectBestCandidate(advisor, candidates);
            _globalStats.SuccessfulRecommendations++;

            // 8. 计算初始忠诚度
            if (foundPerson != null && faction.Leader != null)
            {
                initialLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                    faction.Leader,
                    advisor,
                    foundPerson,
                    RecruitMethod.Recommendation
                );
            }

            // 9. 判定是"直接带回"还是"仅发现"
            RecommendationResult finalResult = DetermineRecruitmentOutcome(advisor, foundPerson);
            
            return finalResult;
        }

        /// <summary>
        /// 判定招募结果：直接加入还是仅发现
        /// </summary>
        private RecommendationResult DetermineRecruitmentOutcome(Person advisor, Person foundPerson)
        {
            if (advisor == null || foundPerson == null)
                return RecommendationResult.Success_FoundOnly;

            try
            {
                // 计算劝说成功率
                int persuadeChance = advisor.Glamour / 2;

                // 相性修正
                int affinityDiff = GetAffinityDistance(advisor.Ideal, foundPerson.Ideal);
                persuadeChance += (50 - affinityDiff * 50 / 75);

                // 义理修正
                persuadeChance -= foundPerson.PersonalLoyalty * AdvisorRecommendationConfig.PersonalLoyaltyPenalty;

                // 年龄修正
                int age = Session.Current.Scenario.Date.Year - foundPerson.YearBorn;
                if (age < 25) persuadeChance += 10;
                else if (age > 50) persuadeChance -= 10;

                // 能力修正
                int totalAbility = foundPerson.Strength + foundPerson.Intelligence + foundPerson.Politics + foundPerson.Glamour;
                if (totalAbility > 300) persuadeChance -= 15;
                else if (totalAbility < 200) persuadeChance += 10;

                // 确保成功率在合理范围内
                persuadeChance = Math.Max(AdvisorRecommendationConfig.MinPersuadeChance, 
                                        Math.Min(AdvisorRecommendationConfig.MaxPersuadeChance, persuadeChance));

                // 判定结果
                int roll = GameObject.Random(100);

                if (roll < persuadeChance)
                {
                    return RecommendationResult.Success_DirectJoin;
                }
                else
                {
                    foundPerson.Available = true;
                    return RecommendationResult.Success_FoundOnly;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 判定招募结果时发生异常: {ex.Message}");
                return RecommendationResult.Success_FoundOnly;
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
        /// 发放失败补偿（低保）
        /// </summary>
        private void ApplyFailureCompensation(Person advisor)
        {
            try
            {
                advisor.IncreaseOfficerMerit(AdvisorRecommendationConfig.FailureCompensationMerit);

                if (advisor.LocationArchitecture != null)
                {
                    advisor.LocationArchitecture.Morale = Math.Min(
                        advisor.LocationArchitecture.MoraleCeiling, 
                        advisor.LocationArchitecture.Morale + AdvisorRecommendationConfig.FailureCompensationMorale
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 发放补偿时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 挑选候选人逻辑
        /// </summary>
        private Person SelectBestCandidate(Person advisor, List<Person> candidates)
        {
            try
            {
                if (candidates == null || candidates.Count == 0)
                    return null;

                if (candidates.Count == 1)
                {
                    return candidates[0];
                }

                // 加权随机选择
                var weightedCandidates = new List<(Person person, int weight)>();
                
                foreach (Person candidate in candidates)
                {
                    int value = CalculatePersonValue(candidate);
                    int weight = Math.Max(1, value / 10);
                    weightedCandidates.Add((candidate, weight));
                }

                // 加权随机选择
                int totalWeight = weightedCandidates.Sum(x => x.weight);
                int randomValue = GameObject.Random(totalWeight);
                int currentWeight = 0;

                foreach (var (person, weight) in weightedCandidates)
                {
                    currentWeight += weight;
                    if (randomValue < currentWeight)
                    {
                        return person;
                    }
                }

                return weightedCandidates[0].person;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 选择候选人时发生异常: {ex.Message}");
                return candidates.FirstOrDefault();
            }
        }

        /// <summary>
        /// 计算人物综合能力值
        /// </summary>
        private int CalculatePersonValue(Person person)
        {
            if (person == null) return 0;

            int intelligence = person.Intelligence * 3;
            int politics = person.Politics * 2;
            int command = person.Command * 2;
            int strength = person.Strength;

            int skillBonus = 0;
            if (person.Skills != null)
            {
                foreach (Skill skill in person.Skills.GetSkillList())
                {
                    skillBonus += skill.Merit;
                }
            }

            int age = Session.Current.Scenario.Date.Year - person.YearBorn;
            int ageBonus = 0;
            if (age < 25) ageBonus = 20;
            else if (age < 35) ageBonus = 10;
            else if (age < 50) ageBonus = 0;
            else ageBonus = -10;

            int totalValue = intelligence + politics + command + strength + skillBonus + ageBonus;
            return Math.Max(1, totalValue);
        }

        /// <summary>
        /// 处理推荐结果的统一接口
        /// </summary>
        public void HandleRecommendationResult(Faction faction, RecommendationResult result, Person talent, int initialLoyalty)
        {
            if (faction?.Advisor == null) 
            {
                return;
            }

            Person advisor = faction.Advisor;
            
            try
            {
                switch (result)
                {
                    case RecommendationResult.Success_DirectJoin:
                        if (talent != null)
                        {
                            try
                            {
                                Architecture targetArchitecture = advisor.LocationArchitecture ?? faction.Capital;
                                if (targetArchitecture != null)
                                {
                                    talent.MoveToArchitecture(targetArchitecture);
                                    
                                    if (talent.PersonalLoyalty < 2)
                                    {
                                        talent.PersonalLoyalty = 2;
                                    }
                                }

                                talent.TempLoyaltyChange = initialLoyalty - talent.Loyalty;
                                
                                string successMessage = $"{advisor.Name}：主公！臣在巡查时偶遇贤才【{talent.Name}】，" +
                                                       $"经臣一番劝说，通过了相性判定，他已答应出仕，现正在殿外候命！" +
                                                       $"（初始忠诚度：{talent.Loyalty}）";
                                
                                ShowRecommendationDialog(faction, successMessage, talent, true);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理直接加入时发生异常: {ex.Message}");
                                string errorMessage = $"{advisor.Name}：主公，臣虽找到了贤才【{talent?.Name ?? "未知"}】，但在安排其入仕时遇到了困难。";
                                ShowRecommendationDialog(faction, errorMessage, talent, false);
                            }
                        }
                        break;

                    case RecommendationResult.Success_FoundOnly:
                        if (talent != null)
                        {
                            try
                            {
                                talent.Available = true;
                                
                                string foundMessage = $"{advisor.Name}：主公，臣在{talent.LocationArchitecture?.Name ?? "某地"}发现了一位名叫【{talent.Name}】的在野贤才。" +
                                                    $"臣虽极力邀请，但其似乎意在待价而沽，还请主公亲自出马（或派人）前往登庸。" +
                                                    $"（预计忠诚度：{initialLoyalty}）";
                                
                                ShowRecommendationDialog(faction, foundMessage, talent, false);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理发现人才时发生异常: {ex.Message}");
                                string errorMessage = $"{advisor.Name}：主公，臣在搜寻中似乎有所发现，但详情有些模糊，请主公留意。";
                                ShowRecommendationDialog(faction, errorMessage, null, false);
                            }
                        }
                        break;

                    case RecommendationResult.Fail_NoTalent:
                        string noTalentMessage = $"{advisor.Name}：主公，臣已搜遍已知区域，暂未发现合适的贤才。" +
                                               $"臣会继续留意，请主公耐心等待。";
                        ShowRecommendationDialog(faction, noTalentMessage, null, false);
                        break;

                    case RecommendationResult.Fail_LowAbility:
                        string lowAbilityMessage = $"{advisor.Name}：主公，臣才疏学浅，未能发现合适的人才。" +
                                                 $"或许需要提升臣的见识，方能为主公觅得良才。";
                        ShowRecommendationDialog(faction, lowAbilityMessage, null, false);
                        break;

                    case RecommendationResult.None:
                        string cooldownMessage = $"{advisor.Name}：主公，臣今年已经进行过举荐，" +
                                               $"请待来年再行此事。";
                        ShowRecommendationDialog(faction, cooldownMessage, null, false);
                        break;

                    default:
                        string unknownMessage = $"{advisor.Name}：主公，臣的搜寻遇到了意外情况，请稍后再试。";
                        ShowRecommendationDialog(faction, unknownMessage, null, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理推荐结果时发生异常: {ex.Message}");
                
                try
                {
                    string fallbackMessage = $"{advisor.Name}：主公，臣在执行任务时遇到了困难，请稍后再试。";
                    ShowRecommendationDialog(faction, fallbackMessage, null, false);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[HandleRecommendationResult] 连备用消息显示都失败了");
                }
            }
        }

        /// <summary>
        /// 显示推荐对话框
        /// </summary>
        private void ShowRecommendationDialog(Faction faction, string message, Person talent, bool isDirectJoin)
        {
            try
            {
                if (faction == null || string.IsNullOrEmpty(message))
                {
                    return;
                }

                if (Session.Current.Scenario.IsCurrentPlayer(faction))
                {
                    if (Session.MainGame?.mainGameScreen == null)
                    {
                        return;
                    }

                    Session.MainGame.mainGameScreen.xianshishijiantupian(
                        faction.Advisor, 
                        message, 
                        TextMessageKind.SearchPersonFound, 
                        "AdvisorRecommendation", 
                        "", 
                        "", 
                        true
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 显示对话框时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取全局推荐统计信息
        /// </summary>
        public static RecommendationStats GetGlobalStats()
        {
            return _globalStats;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public static void ResetStats()
        {
            _globalStats = new RecommendationStats();
        }
    }

    // ================================================================
    // 2. 军师推荐系统配置类
    // ================================================================
    
    /// <summary>
    /// 军师推荐系统配置
    /// </summary>
    public static class AdvisorRecommendationConfig
    {
        // 基础配置
        public static readonly int MinIntelligenceThreshold = 70;        // 军师最低智力要求
        public static readonly int RecommendationCooldownYears = 1;      // 推荐冷却年数
        public static readonly bool EnableDebugLog = true;               // 是否启用调试日志

        // 成功率计算
        public static readonly int BaseSuccessRate = 30;                 // 基础成功率
        public static readonly int IntelligenceBonus = 2;                // 每点智力的成功率加成

        // 劝说判定配置
        public static readonly int PersonalLoyaltyPenalty = 10;          // 每点义理的劝说成功率惩罚
        public static readonly int MinPersuadeChance = 5;                // 最低劝说成功率
        public static readonly int MaxPersuadeChance = 85;               // 最高劝说成功率

        // 失败补偿配置
        public static readonly int FailureCompensationMerit = 5;         // 失败补偿功绩
        public static readonly int FailureCompensationMorale = 2;        // 失败补偿治安

        /// <summary>
        /// 计算推荐成功率
        /// </summary>
        public static int CalculateSuccessRate(int intelligence)
        {
            int rate = BaseSuccessRate + (intelligence - MinIntelligenceThreshold) * IntelligenceBonus;
            return Math.Max(0, Math.Min(100, rate));
        }
    }

    // ================================================================
    // 3. 招募计算器（辅助类）
    // ================================================================
    
    /// <summary>
    /// 招募方式枚举
    /// </summary>
    public enum RecruitMethod
    {
        Direct,         // 直接招募
        Recommendation, // 军师推荐
        Event          // 事件招募
    }

    /// <summary>
    /// 招募计算器
    /// </summary>
    public static class RecruitmentCalculator
    {
        /// <summary>
        /// 计算初始忠诚度
        /// </summary>
        public static int CalculateInitialLoyalty(Person leader, Person advisor, Person recruit, RecruitMethod method)
        {
            try
            {
                if (leader == null || recruit == null) return 50;

                int baseLoyalty = 50;

                // 君主魅力影响
                baseLoyalty += leader.Glamour / 5;

                // 相性影响
                int affinityDiff = Math.Abs(leader.Ideal - recruit.Ideal);
                if (affinityDiff > 75) affinityDiff = 150 - affinityDiff;
                baseLoyalty += (75 - affinityDiff) / 3;

                // 招募方式影响
                switch (method)
                {
                    case RecruitMethod.Recommendation:
                        if (advisor != null)
                        {
                            baseLoyalty += advisor.Intelligence / 10;
                        }
                        break;
                    case RecruitMethod.Event:
                        baseLoyalty += 10;
                        break;
                }

                // 确保在合理范围内
                return Math.Max(30, Math.Min(90, baseLoyalty));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecruitmentCalculator] 计算初始忠诚度时发生异常: {ex.Message}");
                return 50;
            }
        }
    }
}

// ================================================================
// 使用示例和集成代码片段
// ================================================================

/*
// 在MainGameScreen中的集成示例：

public partial class MainGameScreen : Screen
{
    // 军师系统字段
    private GameManager.AdvisorRecommendationSystem _advisorRecommendationSystem = new GameManager.AdvisorRecommendationSystem();

    // 使用军师推荐系统的示例：
    public void CheckAdvisorRecommendation()
    {
        var currentPlayer = Session.Current.Scenario.CurrentPlayer;
        if (currentPlayer?.Advisor == null) return;

        var result = _advisorRecommendationSystem.AttemptRecommendation(
            currentPlayer, 
            out Person foundPerson, 
            out int initialLoyalty
        );

        _advisorRecommendationSystem.HandleRecommendationResult(
            currentPlayer, 
            result, 
            foundPerson, 
            initialLoyalty
        );
    }
}
*/