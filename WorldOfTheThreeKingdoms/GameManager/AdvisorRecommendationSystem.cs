// ===================================================================
// 军师推荐系统 - 完整版本
// 文件位置: GameManager/AdvisorRecommendationSystem.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师推荐系统配置
    /// </summary>
    public static class AdvisorRecommendationConfig
    {
        // 基础配置
        public static readonly int MinIntelligenceThreshold = 50;        // 军师最低智力要求（降低门槛）
        public static readonly int SuperAdvisorIntelligenceThreshold = 100; // 超级军师智力门槛（智力100以上才能全图举荐）
        public static readonly int RecommendationCooldownYears = 1;      // 推荐冷却年数
        public static readonly bool EnableDebugLog = false;             // 是否启用调试日志

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

                // 招募方式影响
                switch (method)
                {
                    case RecruitMethod.Recommendation:
                        if (advisor != null)
                        {
                            baseLoyalty += advisor.Glamour / 10; // 军师推荐加成
                        }
                        break;
                    case RecruitMethod.Event:
                        baseLoyalty += 10; // 事件招募加成
                        break;
                }

                // 相性影响
                if (leader.Ideal == recruit.Ideal)
                {
                    baseLoyalty += 15;
                }
                else
                {
                    int idealDiff = Math.Abs(leader.Ideal - recruit.Ideal);
                    if (idealDiff > 75) idealDiff = 150 - idealDiff; // 环形距离
                    baseLoyalty -= idealDiff / 10;
                }

                return Math.Max(10, Math.Min(100, baseLoyalty));
            }
            catch
            {
                return 50; // 默认忠诚度
            }
        }
    }

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
        /// <param name="initialLoyalty">输出参数：推荐成功时的初始忠诚度</param>
        public RecommendationResult AttemptRecommendation(Faction faction, out Person foundPerson, out int initialLoyalty)
        {
            foundPerson = null;
            initialLoyalty = 0;
            Person advisor = faction.Advisor;
            int currentYear = Session.Current.Scenario.Date.Year;

            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 开始推荐流程，当前年份: {currentYear}");
            }

            // 1. 频率检查：今年是否已经执行过？
            if (faction.LastTalentRecommendYear >= currentYear - AdvisorRecommendationConfig.RecommendationCooldownYears + 1)
            {
                _globalStats.CooldownBlocked++;
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 推荐冷却中，上次推荐年份: {faction.LastTalentRecommendYear}");
                }
                return RecommendationResult.None;
            }

            // 记录尝试次数
            _globalStats.TotalAttempts++;

            // --- 标记今年已执行 (扣除次数) ---
            faction.LastTalentRecommendYear = currentYear;

            // 2. 基础检查
            if (advisor == null || !advisor.Alive)
            {
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine("[AdvisorRecommendationSystem] 无军师或军师已死亡");
                }
                return RecommendationResult.None;
            }

            // 3. 智力门槛检查
            if (advisor.Intelligence < AdvisorRecommendationConfig.MinIntelligenceThreshold)
            {
                _globalStats.FailedLowAbility++;
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 军师智力不足: {advisor.Intelligence} < {AdvisorRecommendationConfig.MinIntelligenceThreshold}");
                }
                ApplyFailureCompensation(advisor); // 发低保
                return RecommendationResult.Fail_LowAbility;
            }

            // 4. 搜寻范围：本势力已知区域的在野武将
            var candidates = new List<Person>();
            
            // 🔥 修复：将日志移到循环外，避免刷屏
            bool isSuperAdvisor = advisor.Intelligence >= AdvisorRecommendationConfig.SuperAdvisorIntelligenceThreshold;
            if (isSuperAdvisor && AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[AttemptRecommendation] 军师{advisor.Name}智力{advisor.Intelligence}>={AdvisorRecommendationConfig.SuperAdvisorIntelligenceThreshold}，可举荐全图人才");
            }
            
            // 🔥 2026-03-11 修复：避免在 foreach 中访问可能被修改的集合
            // 问题：Session.Current.Scenario.Persons 在异步环境中可能被修改，导致 IndexOutOfRangeException
            // 解决：先转换为数组快照，避免集合修改异常
            Person[] personsSnapshot;
            try
            {
                // 🔥 修复：GetList() 返回 GameObjectList，需要使用 OfType<Person>() 过滤类型
                personsSnapshot = Session.Current.Scenario.Persons.GetList().OfType<Person>().ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AttemptRecommendation] 获取人物列表快照失败: {ex.Message}");
                _globalStats.FailedNoTalent++;
                ApplyFailureCompensation(advisor);
                return RecommendationResult.Fail_NoTalent;
            }
            
            foreach (Person p in personsSnapshot)
            {
                // 智力100及以上的军师可以举荐全图人才，不受地域限制
                bool canRecommend = false;
                if (isSuperAdvisor)
                {
                    canRecommend = true; // 超级军师无视地域限制
                }
                else
                {
                    canRecommend = faction.IsPositionKnown(p.Position); // 普通军师受地域限制
                }
                
                if (p.Status == PersonStatus.NoFaction && 
                    p.Alive && 
                    !p.IsCaptive &&
                    p.Available && 
                    canRecommend) // 使用动态判定的地域限制
                {
                    candidates.Add(p);
                }
            }

            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 找到候选人数量: {candidates.Count}");
            }

            // 5. 判定：真的没人吗？
            if (candidates.Count == 0)
            {
                _globalStats.FailedNoTalent++;
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine("[AdvisorRecommendationSystem] 已知区域内无可用人才");
                }
                ApplyFailureCompensation(advisor); // 发低保
                return RecommendationResult.Fail_NoTalent;
            }

            // 6. 判定：智力检定 (军师是否发现了这些人)
            int successRate = AdvisorRecommendationConfig.CalculateSuccessRate(advisor.Intelligence);
            int roll = GameObject.Random(100);
            
            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 智力检定: {advisor.Intelligence}智力 -> {successRate}%成功率, 骰子: {roll}");
            }

            if (roll >= successRate)
            {
                _globalStats.FailedLowAbility++;
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine("[AdvisorRecommendationSystem] 智力检定失败");
                }
                ApplyFailureCompensation(advisor); // 发低保
                return RecommendationResult.Fail_LowAbility;
            }

            // 7. 成功：挑选最合适的一个
            foundPerson = SelectBestCandidate(advisor, candidates);
            _globalStats.SuccessfulRecommendations++;

            // 8. 计算初始忠诚度（使用军师举荐方式）
            if (foundPerson != null && faction.Leader != null)
            {
                initialLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                    faction.Leader,
                    advisor,
                    foundPerson,
                    RecruitMethod.Recommendation
                );
            }

            // 9. 新增：判定是"直接带回"还是"仅发现"
            RecommendationResult finalResult = DetermineRecruitmentOutcome(advisor, foundPerson);

            // 🔥 关键修复：AI势力自动执行招募
            if (finalResult == RecommendationResult.Success_DirectJoin && foundPerson != null)
            {
                bool isPlayer = Session.Current.Scenario.IsCurrentPlayer(faction);
                if (!isPlayer)
                {
                    // AI势力：自动执行招募
                    System.Diagnostics.Debug.WriteLine($"[AttemptRecommendation] AI势力自动招募: {foundPerson.Name}");
                    bool recruitSuccess = RecruitTalentStatic(faction, foundPerson);
                    if (!recruitSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AttemptRecommendation] AI自动招募失败，降级为仅发现");
                        finalResult = RecommendationResult.Success_FoundOnly;
                    }
                }
            }

            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 推荐结果: {finalResult}, 发现人才: {foundPerson?.Name}, 初始忠诚度: {initialLoyalty}");
                System.Diagnostics.Debug.WriteLine(_globalStats.GetSummary());
            }
            
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
                // 1. 计算劝说成功率
                // 基础分：军师魅力的一半
                int persuadeChance = advisor.Glamour / 2;

                // 相性修正：军师和目标相性越近，越容易说服
                int affinityDiff = GetAffinityDistance(advisor.Ideal, foundPerson.Ideal);
                persuadeChance += (50 - affinityDiff * 50 / 75); // 最高加50分

                // 义理修正：义理高的人比较难直接被说动（需要主公亲自去）
                // PersonalLoyalty 范围是 0-4
                persuadeChance -= foundPerson.PersonalLoyalty * AdvisorRecommendationConfig.PersonalLoyaltyPenalty;

                // 年龄修正：年轻人更容易被说服
                int age = Session.Current.Scenario.Date.Year - foundPerson.YearBorn;
                if (age < 25) persuadeChance += 10;      // 年轻人容易说服
                else if (age > 50) persuadeChance -= 10; // 老人比较固执

                // 能力修正：能力越高越难说服（有自己的想法）
                int totalAbility = foundPerson.Strength + foundPerson.Intelligence + foundPerson.Politics + foundPerson.Glamour;
                if (totalAbility > 300) persuadeChance -= 15; // 高能力人才更难说服
                else if (totalAbility < 200) persuadeChance += 10; // 低能力人才容易说服

                // 确保成功率在合理范围内
                persuadeChance = Math.Max(AdvisorRecommendationConfig.MinPersuadeChance, 
                                        Math.Min(AdvisorRecommendationConfig.MaxPersuadeChance, persuadeChance));

                // 2. 判定结果
                int roll = GameObject.Random(100);
                
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 劝说判定: 军师{advisor.Name}(魅力{advisor.Glamour}) vs {foundPerson.Name}");
                    System.Diagnostics.Debug.WriteLine($"  相性差距: {affinityDiff}, 义理: {foundPerson.PersonalLoyalty}, 年龄: {age}, 总能力: {totalAbility}");
                    System.Diagnostics.Debug.WriteLine($"  劝说成功率: {persuadeChance}%, 骰子: {roll}");
                }

                if (roll < persuadeChance)
                {
                    // 判定成功：军师直接把人带回来了
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 劝说成功！{foundPerson.Name}直接加入阵营");
                    }
                    return RecommendationResult.Success_DirectJoin;
                }
                else
                {
                    // 判定失败：只是发现了
                    // 关键操作：将该武将设为"已知"状态，方便玩家在城里看到他
                    foundPerson.Available = true; // 确保可见
                    
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 劝说失败，{foundPerson.Name}仅被发现，需要主公亲自招募");
                    }
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
                // 军师辛苦费 - 增加功绩
                advisor.IncreaseOfficerMerit(AdvisorRecommendationConfig.FailureCompensationMerit);

                // 城市治安略微提升 (使用 Math.Min 限制上限，无需 if 判断)
                if (advisor.LocationArchitecture != null)
                {
                    advisor.LocationArchitecture.Morale = Math.Min(
                        advisor.LocationArchitecture.MoraleCeiling, 
                        advisor.LocationArchitecture.Morale + AdvisorRecommendationConfig.FailureCompensationMorale
                    );
                }

                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 失败补偿已发放给军师 {advisor.Name}: 功绩+{AdvisorRecommendationConfig.FailureCompensationMerit}, 治安+{AdvisorRecommendationConfig.FailureCompensationMorale}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 发放补偿时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 挑选候选人逻辑 - 基于综合能力评分
        /// </summary>
        private Person SelectBestCandidate(Person advisor, List<Person> candidates)
        {
            try
            {
                if (candidates == null || candidates.Count == 0)
                    return null;

                // 如果只有一个候选人，直接返回
                if (candidates.Count == 1)
                {
                    Person single = candidates[0];
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 唯一候选人: {single.Name} (综合能力: {CalculatePersonValue(single)})");
                    }
                    return single;
                }

                // 多个候选人时，使用加权随机选择
                var weightedCandidates = new List<(Person person, int weight)>();
                
                foreach (Person candidate in candidates)
                {
                    int value = CalculatePersonValue(candidate);
                    // 将能力值转换为权重，能力越高权重越大
                    int weight = Math.Max(1, value / 10); // 最小权重为1
                    weightedCandidates.Add((candidate, weight));
                    
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 候选人: {candidate.Name}, 综合能力: {value}, 权重: {weight}");
                    }
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
                        if (AdvisorRecommendationConfig.EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AdvisorRecommendationSystem] 军师 {advisor.Name} 选中了人才 {person.Name} (权重选择: {randomValue}/{totalWeight})");
                        }
                        return person;
                    }
                }

                // 🔥 2026-03-11 修复：安全检查，避免 IndexOutOfRangeException
                // 问题：weightedCandidates 可能为空（虽然理论上不应该）
                // 原因：异步环境中可能存在竞态条件
                if (weightedCandidates.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[AdvisorRecommendationSystem] ⚠️ weightedCandidates 为空，返回原始候选人列表的第一个");
                    return candidates.FirstOrDefault();
                }
                
                // 备用方案：返回第一个
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

            // 基础能力权重
            int intelligence = person.Intelligence * 3;  // 智力权重最高
            int politics = person.Politics * 2;         // 政治次之
            int command = person.Command * 2;           // 统率
            int strength = person.Strength;             // 武力权重最低

            // 特殊技能加成
            int skillBonus = 0;
            if (person.Skills != null)
            {
                foreach (Skill skill in person.Skills.GetSkillList())
                {
                    skillBonus += skill.Merit; // 技能价值
                }
            }

            // 年龄因素（年轻人更有潜力）
            int age = Session.Current.Scenario.Date.Year - person.YearBorn;
            int ageBonus = 0;
            if (age < 25) ageBonus = 20;      // 年轻有潜力
            else if (age < 35) ageBonus = 10; // 正值壮年
            else if (age < 50) ageBonus = 0;  // 中年无加成
            else ageBonus = -10;              // 年老扣分

            int totalValue = intelligence + politics + command + strength + skillBonus + ageBonus;
            return Math.Max(1, totalValue); // 最小值为1
        }
        /// <summary>
        /// 获取推荐结果的描述文本
        /// </summary>
        public string GetResultDescription(RecommendationResult result)
        {
            switch (result)
            {
                case RecommendationResult.None:
                    return "未执行推荐（冷却中或无军师）";
                case RecommendationResult.Fail_NoTalent:
                    return "推荐失败：已知区域内无可用人才";
                case RecommendationResult.Fail_LowAbility:
                    return "推荐失败：军师能力不足";
                case RecommendationResult.Success_FoundOnly:
                    return "推荐成功：发现人才，需要主公亲自招募";
                case RecommendationResult.Success_DirectJoin:
                    return "推荐大成功：人才直接加入阵营";
                default:
                    return "未知推荐结果";
            }
        }

        /// <summary>
        /// 获取全局统计数据
        /// </summary>
        public static RecommendationStats GetGlobalStats()
        {
            return _globalStats;
        }

        /// <summary>
        /// 获取军师评估信息
        /// </summary>
        public static string GetAdvisorAssessment(Person advisor)
        {
            if (advisor == null || !advisor.Alive)
                return "无军师";

            if (advisor.Intelligence < AdvisorRecommendationConfig.MinIntelligenceThreshold)
                return $"军师智力不足（{advisor.Intelligence} < {AdvisorRecommendationConfig.MinIntelligenceThreshold}）";

            if (advisor.Intelligence >= AdvisorRecommendationConfig.SuperAdvisorIntelligenceThreshold)
                return $"超级军师（智力{advisor.Intelligence}，可举荐全图人才）";

            return $"合格军师（智力{advisor.Intelligence}）";
        }

        /// <summary>
        /// 处理推荐结果
        /// </summary>
        public static void HandleRecommendationResult(Faction faction, RecommendationResult result, Person foundPerson, int initialLoyalty)
        {
            if (faction == null) return;

            Person advisor = faction.Advisor;
            if (advisor == null) return;

            try
            {
                // 所有情况都只显示对话框，让玩家选择
                ShowRecommendationDialog(faction, result, foundPerson, initialLoyalty);

                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 推荐结果: {result}, 发现人才: {foundPerson?.Name ?? "无"}, 预计忠诚度: {initialLoyalty}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理推荐结果时发生异常: {ex.Message}");
                
                // 提供最后的备用消息
                try
                {
                    ShowRecommendationDialog(faction, RecommendationResult.None, null, 0);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[HandleRecommendationResult] 连备用消息显示都失败了");
                }
            }
        }

        /// <summary>
        /// 年度人才推荐系统 - 发现多个人才
        /// </summary>
        public DiscoveryResult DiscoverMultipleTalents(Faction faction, int maxCount = 3)
        {
            var result = new DiscoveryResult();
            Person advisor = faction.Advisor;
            int currentYear = Session.Current.Scenario.Date.Year;

            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscoverMultipleTalents] 开始年度推荐流程，当前年份: {currentYear}");
            }

            // 1. 频率检查：今年是否已经执行过？
            if (faction.LastTalentRecommendYear >= currentYear - AdvisorRecommendationConfig.RecommendationCooldownYears + 1)
            {
                result.IsSuccess = false;
                result.FailureReason = "今年已经进行过推荐";
                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiscoverMultipleTalents] 推荐冷却中，上次推荐年份: {faction.LastTalentRecommendYear}");
                }
                return result;
            }

            // 2. 基础检查
            if (advisor == null || !advisor.Alive)
            {
                result.IsSuccess = false;
                result.FailureReason = "无军师或军师已死亡";
                return result;
            }

            // 3. 智力门槛检查
            if (advisor.Intelligence < AdvisorRecommendationConfig.MinIntelligenceThreshold)
            {
                result.IsSuccess = false;
                result.FailureReason = "军师智力不足";
                ApplyFailureCompensation(advisor);
                return result;
            }

            // 4. 搜寻范围：智力>100的军师可以举荐全图人才，否则限制在已知区域
            var allCandidates = new List<Person>();
            
            // 🔥 修复：将日志移到循环外，避免刷屏
            bool isSuperAdvisor = advisor.Intelligence >= AdvisorRecommendationConfig.SuperAdvisorIntelligenceThreshold;
            if (isSuperAdvisor && AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscoverMultipleTalents] 军师{advisor.Name}智力{advisor.Intelligence}>={AdvisorRecommendationConfig.SuperAdvisorIntelligenceThreshold}，可举荐全图人才");
            }
            
            foreach (Person p in Session.Current.Scenario.Persons)
            {
                // 智力100及以上的军师可以举荐全图人才，不受地域限制
                bool canRecommend = false;
                if (isSuperAdvisor)
                {
                    canRecommend = true; // 超级军师无视地域限制
                }
                else
                {
                    canRecommend = faction.IsPositionKnown(p.Position); // 普通军师受地域限制
                }

                if (p.Status == PersonStatus.NoFaction && 
                    p.Alive && 
                    !p.IsCaptive &&
                    p.Available && 
                    canRecommend)
                {
                    allCandidates.Add(p);
                }
            }

            if (allCandidates.Count == 0)
            {
                result.IsSuccess = false;
                result.FailureReason = "已知区域内无可用人才";
                ApplyFailureCompensation(advisor);
                return result;
            }

            // 5. 智力检定
            int successRate = AdvisorRecommendationConfig.CalculateSuccessRate(advisor.Intelligence);
            int roll = GameObject.Random(100);
            
            if (roll >= successRate)
            {
                result.IsSuccess = false;
                result.FailureReason = "军师能力不足，未能发现人才";
                ApplyFailureCompensation(advisor);
                return result;
            }

            // 6. 选择最优秀的人才（按综合能力排序）
            var sortedCandidates = new List<Person>();
            foreach (Person candidate in allCandidates)
            {
                sortedCandidates.Add(candidate);
            }

            // 手动排序（按综合能力降序）
            for (int i = 0; i < sortedCandidates.Count - 1; i++)
            {
                for (int j = i + 1; j < sortedCandidates.Count; j++)
                {
                    if (CalculatePersonValue(sortedCandidates[j]) > CalculatePersonValue(sortedCandidates[i]))
                    {
                        Person temp = sortedCandidates[i];
                        sortedCandidates[i] = sortedCandidates[j];
                        sortedCandidates[j] = temp;
                    }
                }
            }

            // 7. 取前maxCount个
            int takeCount = Math.Min(maxCount, sortedCandidates.Count);
            for (int i = 0; i < takeCount; i++)
            {
                result.DiscoveredTalents.Add(sortedCandidates[i]);
            }

            result.IsSuccess = true;
            faction.LastTalentRecommendYear = currentYear;

            if (AdvisorRecommendationConfig.EnableDebugLog)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscoverMultipleTalents] 成功发现{result.DiscoveredTalents.Count}名人才");
                foreach (Person talent in result.DiscoveredTalents)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {talent.Name} (综合能力: {CalculatePersonValue(talent)})");
                }
            }

            return result;
        }

        /// <summary>
        /// 招募人才到势力 - 静态方法版本
        /// </summary>
        public static bool RecruitTalentStatic(Faction faction, Person talent)
        {
            if (faction == null || talent == null) return false;

            try
            {
                Architecture targetArch = faction.Capital;
                if (targetArch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RecruitTalent] 招募前状态 - {talent?.Name ?? "Unknown"}: BelongedFaction={talent.BelongedFaction?.Name ?? "None"}, Status={talent.Status}, LocationArchitecture={talent.LocationArchitecture?.Name ?? "None"}, Loyalty={talent.Loyalty}");
                    
                    // 步骤1: 使用正确的招募流程 - 先让武将加入势力
                    talent.MoveToArchitecture(targetArch);
                    talent.ArrivingDays = 0;           // 立即到达
                    talent.TargetArchitecture = null;  // 清除目标
                    talent.Status = PersonStatus.Normal; // 设置正常状态
                    
                    System.Diagnostics.Debug.WriteLine($"[RecruitTalent] 加入势力后状态 - {talent?.Name ?? "Unknown"}: BelongedFaction={talent.BelongedFaction?.Name ?? "None"}, Status={talent.Status}, Loyalty={talent.Loyalty}");
                    
                    // 步骤2: 计算并设置初始忠诚度 - 必须在加入势力之后
                    int initialLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                        faction.Leader, faction.Advisor, talent, RecruitMethod.Recommendation);
                    
                    // 步骤3: 使用TempLoyaltyChange设置忠诚度
                    talent.TempLoyaltyChange = initialLoyalty - talent.Loyalty;
                    
                    System.Diagnostics.Debug.WriteLine($"[RecruitTalent] 忠诚度设置 - 目标忠诚度: {initialLoyalty}, 当前忠诚度: {talent.Loyalty}, TempLoyaltyChange: {talent.TempLoyaltyChange}");
                    
                    // 更新推荐年份
                    faction.LastTalentRecommendYear = Session.Current.Scenario.Date.Year;
                    
                    System.Diagnostics.Debug.WriteLine($"[RecruitTalent] 招募后最终状态 - {talent?.Name ?? "Unknown"}: BelongedFaction={talent.BelongedFaction?.Name ?? "None"}, Status={talent.Status}, LocationArchitecture={talent.LocationArchitecture?.Name ?? "None"}, Loyalty={talent.Loyalty}, TempLoyaltyChange={talent.TempLoyaltyChange}");
                    System.Diagnostics.Debug.WriteLine($"[RecruitTalent] {talent?.Name ?? "Unknown"} 成功加入 {faction?.Name ?? "Unknown"}，忠诚度: {talent.Loyalty}");
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecruitTalent] 异常: {ex?.Message ?? "Unknown error"}");
            }
            return false;
        }

        /// <summary>
        /// 招募人才到势力 - 实例方法版本（保持兼容性）
        /// </summary>
        public bool RecruitTalent(Faction faction, Person talent)
        {
            // 调用静态方法版本
            return RecruitTalentStatic(faction, talent);
        }

        /// <summary>
        /// 处理人才选择
        /// </summary>
        public void HandleTalentSelection(Faction faction, Person selectedTalent)
        {
            System.Diagnostics.Debug.WriteLine($"[HandleTalentSelection] 开始处理人才选择");
            System.Diagnostics.Debug.WriteLine($"[HandleTalentSelection] 势力: {faction?.Name ?? "Unknown"}, 人才: {selectedTalent?.Name ?? "Unknown"}");

            if (faction == null || selectedTalent == null)
            {
                System.Diagnostics.Debug.WriteLine("[HandleTalentSelection] 参数无效");
                return;
            }

            bool success = RecruitTalent(faction, selectedTalent);
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleTalentSelection] 成功招募: {selectedTalent.Name}");
                
                // 🔥 修复：直接显示简单结果消息，不再调用ShowRecommendationDialog
                // ShowRecommendationDialog会触发两阶段对话，导致重复判定
                int initialLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                    faction.Leader, faction.Advisor, selectedTalent, RecruitMethod.Recommendation);
                ShowSimpleResultMessage(faction, selectedTalent, true, initialLoyalty);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[HandleTalentSelection] 招募失败: {selectedTalent.Name}");
                
                // 🔥 修复：直接显示简单结果消息
                ShowSimpleResultMessage(faction, selectedTalent, false, 0);
            }
        }

        /// <summary>
        /// 显示简单结果消息（不触发两阶段对话）
        /// </summary>
        private void ShowSimpleResultMessage(Faction faction, Person talent, bool success, int initialLoyalty)
        {
            try
            {
                var mainScreen = Session.MainGame?.mainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin == null) return;

                string message;
                if (success)
                {
                    message = AdvisorDialogueManager.GetDialogue(
                        faction, RecommendationResult.Success_DirectJoin, 
                        faction.Advisor, talent, initialLoyalty
                    );
                }
                else
                {
                    message = AdvisorDialogueManager.GetDialogue(
                        faction, RecommendationResult.Fail_LowAbility, 
                        faction.Advisor, talent, 0
                    );
                }

                // 直接显示消息，不设置确认回调
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    faction.Advisor, faction.Advisor, message, "", "", ""
                );
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowSimpleResultMessage] 异常: {ex.Message}");
            }
        }


        /// <summary>
        /// 年度推荐系统 - 自动选择最优人才并招募
        /// </summary>
        public void ExecuteYearlyRecommendation(Faction faction)
        {
            try
            {
                bool isPlayer = Session.Current.Scenario.IsPlayer(faction);
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] ===== 开始年度推荐 =====");
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 势力: {faction?.Name ?? "Unknown"}");
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 势力类型: {(isPlayer ? "玩家势力" : "AI势力")}");
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 军师: {faction?.Advisor?.Name ?? "无"}");
                
                if (faction?.Advisor == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 势力 {faction?.Name} 无军师，退出");
                    return;
                }

                // 1. 发现人才
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 开始发现人才阶段");
                var discoveryResult = DiscoverMultipleTalents(faction, 5);
                
                if (!discoveryResult.IsSuccess)
                {
                    // System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 人才发现失败: {discoveryResult.FailureReason}");
                    
                    // 显示失败消息 - 使用配置化对话系统
                    RecommendationResult failureResult = RecommendationResult.Fail_NoTalent;
                    if (discoveryResult.FailureReason.Contains("智力不足") || discoveryResult.FailureReason.Contains("能力不足"))
                    {
                        failureResult = RecommendationResult.Fail_LowAbility;
                    }
                    else if (discoveryResult.FailureReason.Contains("冷却") || discoveryResult.FailureReason.Contains("已经"))
                    {
                        failureResult = RecommendationResult.None;
                    }
                    
                    // System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 显示失败对话: {failureResult}");
                    if (isPlayer)
                    {
                        HandleRecommendationResult(faction, failureResult, null, 0);
                    }
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 成功发现 {discoveryResult.DiscoveredTalents.Count} 名人才");
                foreach (Person talent in discoveryResult.DiscoveredTalents)
                {
                    int value = CalculatePersonValue(talent);
                    System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 候选人才: {talent.Name} (综合能力: {value})");
                }

                // 2. 自动选择综合能力最高的人才
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 开始选择最优人才");
                Person bestTalent = null;
                int bestValue = 0;
                foreach (Person talent in discoveryResult.DiscoveredTalents)
                {
                    int value = CalculatePersonValue(talent);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestTalent = talent;
                    }
                }

                if (bestTalent != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 选择了最优人才: {bestTalent.Name} (综合能力: {bestValue})");
                    
                    // 3. 判定招募结果
                    RecommendationResult finalResult = DetermineRecruitmentOutcome(faction.Advisor, bestTalent);
                    int initialLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                        faction.Leader, faction.Advisor, bestTalent, RecruitMethod.Recommendation);
                    
                    if (isPlayer)
                    {
                        // 玩家势力：显示对话让玩家选择
                        System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 玩家势力，显示确认对话");
                        HandleRecommendationResult(faction, finalResult, bestTalent, initialLoyalty);
                    }
                    else
                    {
                        // AI势力：直接执行招募
                        System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] AI势力，直接执行招募 {bestTalent.Name}");
                        if (finalResult == RecommendationResult.Success_DirectJoin)
                        {
                            bool success = RecruitTalentStatic(faction, bestTalent);
                            if (success)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] AI招募成功！{bestTalent.Name} 加入 {faction.Name}，忠诚度: {initialLoyalty}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] AI招募失败！{bestTalent.Name}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] AI势力推荐结果: {finalResult}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 未找到合适的人才");
                    if (isPlayer)
                    {
                        HandleRecommendationResult(faction, RecommendationResult.Fail_NoTalent, null, 0);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] ===== 年度推荐完成 =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ExecuteYearlyRecommendation] 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 发现结果类
        /// </summary>
        public class DiscoveryResult
        {
            public bool IsSuccess { get; set; } = false;
            public string FailureReason { get; set; } = "";
            public List<Person> DiscoveredTalents { get; set; } = new List<Person>();
        }

        /// <summary>
        /// 显示推荐对话框 - 使用AdvisorDialogueConfig配置文件，两阶段对话流程
        /// </summary>
        private static void ShowRecommendationDialog(Faction faction, RecommendationResult result, Person talent, int initialLoyalty)
        {
            try
            {
                // 参数验证
                if (faction == null || faction.Advisor == null) return;

                // 如果是玩家势力，显示对话框
                if (Session.Current.Scenario.IsCurrentPlayer(faction))
                {
                    var mainScreen = Session.MainGame?.mainGameScreen;
                    if (mainScreen?.Plugins?.tupianwenziPlugin == null) return;

                    // 检查军师举荐开关状态
                    bool isAutoModeEnabled = faction.IsAdvisorRecommendationEnabled;
                    
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 军师举荐开关状态: {(isAutoModeEnabled ? "开启" : "关闭")}");
                        System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 推荐结果: {result}");
                    }

                    // 根据开关状态和推荐结果决定对话类型
                    if (isAutoModeEnabled)
                    {
                        // 开关开启：失败直接提示，成功才弹窗选择
                        if (result == RecommendationResult.Success_DirectJoin && talent != null)
                        {
                            // 成功情况：显示两阶段确认对话
                            ShowSuccessConfirmationDialog(faction, talent, initialLoyalty, mainScreen);
                        }
                        else
                        {
                            // 失败情况：直接显示失败消息，不需要确认
                            ShowDirectFailureMessage(faction, result, talent, initialLoyalty, mainScreen);
                        }
                    }
                    else
                    {
                        // 开关关闭：无论成功失败都弹窗让玩家选择
                        if (result == RecommendationResult.Success_DirectJoin && talent != null)
                        {
                            // 成功情况：显示两阶段确认对话
                            ShowSuccessConfirmationDialog(faction, talent, initialLoyalty, mainScreen);
                        }
                        else
                        {
                            // 失败情况：也显示确认对话，让玩家了解情况
                            ShowFailureConfirmationDialog(faction, result, talent, initialLoyalty, mainScreen);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 显示对话框时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示成功确认对话（两阶段，第二阶段仅显示信息）
        /// </summary>
        private static void ShowSuccessConfirmationDialog(Faction faction, Person talent, int initialLoyalty, MainGameScreen mainScreen)
        {
            try
            {
                // 第一阶段：询问是否前去查看发现的贤才
                string initialMessage = AdvisorDialogueManager.GetGenericDialogue(
                    faction, faction.Advisor, "Discovery_Initial", null
                );
                
                // 设置第一阶段确认对话框
                mainScreen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    mainScreen.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() => {
                        // 第一阶段确认回调：显示第二阶段信息对话（不需要确认）
                        ShowTalentInfoDialog(faction, talent, initialLoyalty, mainScreen);
                    }),
                    new GameDelegates.VoidFunction(() => {
                        // 第一阶段取消回调：什么都不做
                    })
                );

                mainScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示第一阶段确认对话框
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    faction.Advisor, faction.Advisor, initialMessage, "", "", ""
                );
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowSuccessConfirmationDialog] 显示成功确认对话时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示直接失败消息（不需要确认）
        /// </summary>
        private static void ShowDirectFailureMessage(Faction faction, RecommendationResult result, Person talent, int initialLoyalty, MainGameScreen mainScreen)
        {
            try
            {
                // 使用配置文件中的对话内容
                string message = AdvisorDialogueManager.GetDialogue(
                    faction, result, faction.Advisor, talent, initialLoyalty
                );

                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowDirectFailureMessage] 显示直接失败消息: {message}");
                }

                // 直接显示消息，不需要确认
                if (!string.IsNullOrEmpty(message))
                {
                    mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        faction.Advisor, faction.Advisor, message, "", "", ""
                    );
                    mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                    mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDirectFailureMessage] 显示直接失败消息时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示失败确认对话（让玩家了解情况）
        /// </summary>
        private static void ShowFailureConfirmationDialog(Faction faction, RecommendationResult result, Person talent, int initialLoyalty, MainGameScreen mainScreen)
        {
            try
            {
                // 使用配置文件中的对话内容
                string message = AdvisorDialogueManager.GetDialogue(
                    faction, result, faction.Advisor, talent, initialLoyalty
                );

                if (AdvisorRecommendationConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowFailureConfirmationDialog] 显示失败确认对话: {message}");
                }

                // 设置确认对话框（只有确认按钮）
                mainScreen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    mainScreen.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() => {
                        // 确认回调：什么都不做，只是让玩家知道情况
                        if (AdvisorRecommendationConfig.EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ShowFailureConfirmationDialog] 玩家确认了解失败情况");
                        }
                    }),
                    new GameDelegates.VoidFunction(() => {
                        // 取消回调：什么都不做
                    })
                );

                mainScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示确认对话框
                if (!string.IsNullOrEmpty(message))
                {
                    mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        faction.Advisor, faction.Advisor, message, "", "", ""
                    );
                    mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                    mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowFailureConfirmationDialog] 显示失败确认对话时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示人才信息对话（仅显示信息，不需要确认，自动执行招募）
        /// </summary>
        private static void ShowTalentInfoDialog(Faction faction, Person talent, int initialLoyalty, MainGameScreen mainScreen)
        {
            try
            {
                // 🔥 关键修复：第二阶段只显示人才信息，不需要确认，自动执行招募
                System.Diagnostics.Debug.WriteLine($"[ShowTalentInfoDialog] 显示人才信息并自动执行招募: {talent.Name}");
                
                // 自动执行招募
                bool success = RecruitTalentStatic(faction, talent);
                System.Diagnostics.Debug.WriteLine($"[ShowTalentInfoDialog] 招募结果: {(success ? "成功" : "失败")}");
                
                // 显示人才信息和招募结果
                string infoMessage;
                if (success)
                {
                    // 使用配置文件中的成功对话
                    infoMessage = AdvisorDialogueManager.GetDialogue(
                        faction, RecommendationResult.Success_DirectJoin, 
                        faction.Advisor, talent, initialLoyalty
                    );
                }
                else
                {
                    // 使用配置文件中的失败对话
                    infoMessage = AdvisorDialogueManager.GetGenericDialogue(
                        faction, faction.Advisor, "Recruitment_Failed", 
                        new Dictionary<string, string> { 
                            { "{Talent}", talent.Name } 
                        }
                    );
                }
                
                // 🔥 关键修复：只显示信息对话，不设置确认回调，避免第三个对话
                // 直接显示结果信息，用户点击后自动关闭，不会触发额外的结果判断
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    faction.Advisor, faction.Advisor, infoMessage, "", "", ""
                );
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowTalentInfoDialog] 显示人才信息对话时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示第二阶段对话：具体人才信息和招募确认
        /// 🚫 已废弃：替换为ShowTalentInfoDialog，避免重复对话
        /// </summary>
        /*
        private static void ShowSecondStageDialog(Faction faction, Person talent, int initialLoyalty, MainGameScreen mainScreen)
        {
            try
            {
                // 第二阶段：显示具体人才信息并询问是否招募
                string confirmMessage = AdvisorDialogueManager.GetGenericDialogue(
                    faction, faction.Advisor, "Discovery_Confirm", 
                    new Dictionary<string, string> { 
                        { "{Talent}", talent.Name } 
                    }
                );
                
                // 设置第二阶段确认对话框
                mainScreen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    mainScreen.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() => {
                        // 第二阶段确认回调：执行招募并显示结果
                        System.Diagnostics.Debug.WriteLine($"[ShowSecondStageDialog] 玩家确认招募 {talent.Name}");
                        bool success = RecruitTalentStatic(faction, talent);
                        System.Diagnostics.Debug.WriteLine($"[ShowSecondStageDialog] 招募结果: {(success ? "成功" : "失败")}");
                        
                        // 显示招募结果
                        string resultMessage;
                        if (success)
                        {
                            // 使用配置文件中的成功对话
                            resultMessage = AdvisorDialogueManager.GetDialogue(
                                faction, RecommendationResult.Success_DirectJoin, 
                                faction.Advisor, talent, initialLoyalty
                            );
                        }
                        else
                        {
                            // 使用配置文件中的失败对话
                            resultMessage = AdvisorDialogueManager.GetGenericDialogue(
                                faction, faction.Advisor, "Recruitment_Failed", 
                                new Dictionary<string, string> { 
                                    { "{Talent}", talent.Name } 
                                }
                            );
                        }
                        
                        // 显示结果对话框
                        mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            faction.Advisor, faction.Advisor, resultMessage, "", "", ""
                        );
                        mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                        mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }),
                    new GameDelegates.VoidFunction(() => {
                        // 第二阶段取消回调：什么都不做
                        System.Diagnostics.Debug.WriteLine($"[ShowSecondStageDialog] 玩家取消招募 {talent.Name}");
                    })
                );

                mainScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示第二阶段确认对话框
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    faction.Advisor, faction.Advisor, confirmMessage, "", "", ""
                );
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowSecondStageDialog] 显示第二阶段对话时发生异常: {ex.Message}");
            }
        }
        */

        /// <summary>
        /// 显示招募结果 - 使用AdvisorDialogueConfig配置文件
        /// </summary>
        private static void ShowRecruitmentResult(Faction faction, Person talent, bool success, int initialLoyalty, MainGameScreen screen)
        {
            if (screen?.Plugins?.tupianwenziPlugin == null) return;

            // 使用配置文件中的对话内容
            string message;
            if (success)
            {
                message = AdvisorDialogueManager.GetGenericDialogue(
                    faction, faction.Advisor, "Recruitment_Success", 
                    new Dictionary<string, string> { 
                        { "{Talent}", talent?.Name ?? "未知" },
                        { "{InitialLoyalty}", initialLoyalty.ToString() }
                    }
                );
            }
            else
            {
                message = AdvisorDialogueManager.GetGenericDialogue(
                    faction, faction.Advisor, "Recruitment_Failed", 
                    new Dictionary<string, string> { 
                        { "{Talent}", talent?.Name ?? "未知" }
                    }
                );
            }

            // 显示结果对话 - 使用配置文件的对话内容
            screen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                faction.Advisor,                   // 说话人
                faction.Advisor,                   // 对象
                message,                           // 使用配置文件的对话内容
                "",                                // 图片 - 使用空字符串
                "",                                // 声音 - 使用空字符串
                ""                                 // 空字符串
            );
            screen.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 执行实际的招募操作
        /// </summary>
        private static bool PerformRecruitment(Faction faction, Person talent, int initialLoyalty)
        {
            try
            {
                // 1. 检查首都是否存在
                if (faction.Capital == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformRecruitment] 势力没有首都");
                    return false;
                }

                // 2. 检查人才是否仍然在野
                if (talent.BelongedFaction != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformRecruitment] {talent.Name} 已不在野");
                    return false;
                }

                // 3. 执行招募 - 移动到首都
                talent.MoveToArchitecture(faction.Capital, null, true, true, null);
                
                // 4. 立即到达设置
                talent.ArrivingDays = 0;           // 跳过移动时间，立即到达
                talent.TargetArchitecture = null;  // 清除移动目标
                talent.Status = PersonStatus.Normal; // 设置正常状态

                // 5. 设置初始忠诚度
                if (initialLoyalty > 0)
                {
                    talent.TempLoyaltyChange = initialLoyalty - talent.Loyalty;
                    System.Diagnostics.Debug.WriteLine($"[PerformRecruitment] 设置忠诚度: {talent.Loyalty} -> {initialLoyalty}");
                }

                System.Diagnostics.Debug.WriteLine($"[PerformRecruitment] {talent.Name} 成功加入 {faction.Name}，立即到达首都 {faction.Capital.Name}");
                return true;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformRecruitment] 招募过程中发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 显示失败对话框 - 使用AdvisorDialogueConfig配置文件
        /// </summary>
        private static void ShowFailureDialog(Faction faction, RecommendationResult result)
        {
            try
            {
                Person advisor = faction.Advisor;
                if (advisor == null) return;

                // 使用配置文件中的对话内容
                string message = AdvisorDialogueManager.GetDialogue(
                    faction, result, advisor, null, 0
                );

                System.Diagnostics.Debug.WriteLine($"[ShowFailureDialog] 显示失败对话: {message}");

                // 参考说服功能的实现 - 只显示军师对话
                var mainScreen = Session.MainGame?.mainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin != null)
                {
                    mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        advisor,                           // 说话人
                        advisor,                           // 对象
                        message,                           // 使用配置文件的对话内容
                        "",                                // 图片 - 使用空字符串
                        "",                                // 声音 - 使用空字符串
                        ""                                 // 空字符串
                    );
                    mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowFailureDialog] 显示失败对话时发生异常: {ex.Message}");
            }
        }
    }
}