// ===================================================================
// 军师推荐系统 - 完整版本
// 文件位置: GameManager/AdvisorRecommendationSystem.cs
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace GameManager
{
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
            foreach (Person p in Session.Current.Scenario.Persons)
            {
                if (p.Status == GameObjects.PersonDetail.PersonStatus.NoFaction && 
                    p.Alive && 
                    !p.IsCaptive &&
                    p.Available && 
                    faction.IsPositionKnown(p.Position)) // 必须是已知区域
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
        public string GetResultDescription(RecommendationResult result, Person advisor, Person foundPerson)
        {
            switch (result)
            {
                case RecommendationResult.Success_DirectJoin:
                    return $"军师{advisor?.Name}成功说服了人才{foundPerson?.Name}，直接加入了我军！";
                
                case RecommendationResult.Success_FoundOnly:
                    return $"军师{advisor?.Name}发现了人才{foundPerson?.Name}，但未能说服其加入，需要主公亲自招募。";
                
                case RecommendationResult.Fail_NoTalent:
                    return $"军师{advisor?.Name}搜寻了已知区域，但未发现合适的人才。";
                
                case RecommendationResult.Fail_LowAbility:
                    return $"军师{advisor?.Name}能力有限，未能发现人才。";
                
                case RecommendationResult.None:
                    return "今年已经进行过举荐，请等待下一年。";
                
                default:
                    return "未知的推荐结果。";
            }
        }

        /// <summary>
        /// 处理推荐结果的统一接口
        /// </summary>
        /// <param name="faction">执行推荐的势力</param>
        /// <param name="result">推荐结果</param>
        /// <param name="talent">发现的人才</param>
        /// <param name="initialLoyalty">计算出的初始忠诚度</param>
        public void HandleRecommendationResult(Faction faction, RecommendationResult result, Person talent, int initialLoyalty)
        {
            if (faction?.Advisor == null) 
            {
                System.Diagnostics.Debug.WriteLine("[HandleRecommendationResult] 势力或军师为空，无法处理推荐结果");
                return;
            }

            Person advisor = faction.Advisor;
            
            try
            {
                switch (result)
                {
                    case RecommendationResult.Success_DirectJoin:
                        // --- 情况A：直接加入 ---
                        if (talent != null)
                        {
                            try
                            {
                                // 1. 执行加入逻辑 - 使用MoveToArchitecture确保正确加入势力
                                Architecture targetArchitecture = advisor.LocationArchitecture ?? faction.Capital;
                                if (targetArchitecture != null)
                                {
                                    talent.MoveToArchitecture(targetArchitecture);
                                    
                                    // 设置基础个人忠诚度（影响计算出的Loyalty值）
                                    if (talent.PersonalLoyalty < 2)
                                    {
                                        talent.PersonalLoyalty = 2;
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 警告：无法找到合适的目标建筑，{talent.Name}加入可能失败");
                                }
                                
                                // 2. 设置初始忠诚度变化
                                talent.TempLoyaltyChange = initialLoyalty - talent.Loyalty;
                                
                                // 3. 显示成功消息（大喜）
                                string successMessage = $"{advisor.Name}：主公！臣在巡查时偶遇贤才【{talent.Name}】，" +
                                                       $"经臣一番劝说，通过了相性判定，他已答应出仕，现正在殿外候命！" +
                                                       $"（初始忠诚度：{talent.Loyalty}）";
                                
                                ShowRecommendationDialog(faction, successMessage, talent, true);
                                
                                if (AdvisorRecommendationConfig.EnableDebugLog)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] {talent.Name}直接加入{faction.Name}，忠诚度{talent.Loyalty}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理直接加入时发生异常: {ex.Message}");
                                // 即使加入失败，也要显示一个消息
                                string errorMessage = $"{advisor.Name}：主公，臣虽找到了贤才【{talent?.Name ?? "未知"}】，但在安排其入仕时遇到了困难。";
                                ShowRecommendationDialog(faction, errorMessage, talent, false);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[HandleRecommendationResult] 警告：Success_DirectJoin但talent为null");
                        }
                        break;

                    case RecommendationResult.Success_FoundOnly:
                        // --- 情况B：仅发现 ---
                        if (talent != null)
                        {
                            try
                            {
                                // 1. 确保人才可见（在野状态）
                                talent.Available = true;
                                
                                // 2. 显示发现消息（引导玩家手动招募）
                                string foundMessage = $"{advisor.Name}：主公，臣在{talent.LocationArchitecture?.Name ?? "某地"}发现了一位名叫【{talent.Name}】的在野贤才。" +
                                                    $"臣虽极力邀请，但其似乎意在待价而沽，还请主公亲自出马（或派人）前往登庸。" +
                                                    $"（预计忠诚度：{initialLoyalty}）";
                                
                                ShowRecommendationDialog(faction, foundMessage, talent, false);
                                
                                if (AdvisorRecommendationConfig.EnableDebugLog)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 发现人才{talent.Name}，需手动招募");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理发现人才时发生异常: {ex.Message}");
                                // 提供备用消息
                                string errorMessage = $"{advisor.Name}：主公，臣在搜寻中似乎有所发现，但详情有些模糊，请主公留意。";
                                ShowRecommendationDialog(faction, errorMessage, null, false);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[HandleRecommendationResult] 警告：Success_FoundOnly但talent为null");
                        }
                        break;

                    case RecommendationResult.Fail_NoTalent:
                        // 失败：没有人才
                        string noTalentMessage = $"{advisor.Name}：主公，臣已搜遍已知区域，暂未发现合适的贤才。" +
                                               $"臣会继续留意，请主公耐心等待。";
                        ShowRecommendationDialog(faction, noTalentMessage, null, false);
                        break;

                    case RecommendationResult.Fail_LowAbility:
                        // 失败：能力不足
                        string lowAbilityMessage = $"{advisor.Name}：主公，臣才疏学浅，未能发现合适的人才。" +
                                                 $"或许需要提升臣的见识，方能为主公觅得良才。";
                        ShowRecommendationDialog(faction, lowAbilityMessage, null, false);
                        break;

                    case RecommendationResult.None:
                        // 今年已经推荐过
                        string cooldownMessage = $"{advisor.Name}：主公，臣今年已经进行过举荐，" +
                                               $"请待来年再行此事。";
                        ShowRecommendationDialog(faction, cooldownMessage, null, false);
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 未知的推荐结果类型: {result}");
                        string unknownMessage = $"{advisor.Name}：主公，臣的搜寻遇到了意外情况，请稍后再试。";
                        ShowRecommendationDialog(faction, unknownMessage, null, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 处理推荐结果时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[HandleRecommendationResult] 堆栈跟踪: {ex.StackTrace}");
                
                // 提供最后的备用消息
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
                // 参数验证
                if (faction == null || string.IsNullOrEmpty(message))
                {
                    System.Diagnostics.Debug.WriteLine("[ShowRecommendationDialog] 参数无效：faction或message为空");
                    return;
                }

                // 如果是玩家势力，显示对话框
                if (Session.Current.Scenario.IsCurrentPlayer(faction))
                {
                    // 验证Session和mainGameScreen是否可用
                    if (Session.MainGame?.mainGameScreen == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[ShowRecommendationDialog] Session.MainGame.mainGameScreen不可用");
                        return;
                    }

                    // 使用现有的事件图片系统显示消息
                    // 修复参数匹配问题：使用正确的方法签名
                    Session.MainGame.mainGameScreen.xianshishijiantupian(
                        faction.Advisor, 
                        message, 
                        TextMessageKind.SearchPersonFound, 
                        "AdvisorRecommendation", 
                        "", 
                        "", 
                        true
                    );

                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 成功显示推荐对话框：{message}");
                    }
                }
                else
                {
                    // AI势力的推荐结果只记录日志
                    if (AdvisorRecommendationConfig.EnableDebugLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI推荐] {faction.Name}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 显示对话框时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 堆栈跟踪: {ex.StackTrace}");
                
                // 提供备用的简单消息显示机制
                try
                {
                    if (faction != null && !string.IsNullOrEmpty(message))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ShowRecommendationDialog] 备用显示: {faction.Name} - {message}");
                    }
                }
                catch
                {
                    // 如果连备用机制都失败，至少记录一下
                    System.Diagnostics.Debug.WriteLine("[ShowRecommendationDialog] 备用显示机制也失败");
                }
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

        /// <summary>
        /// 获取军师推荐能力评估
        /// </summary>
        public static string GetAdvisorAssessment(Person advisor)
        {
            if (advisor == null || !advisor.Alive)
                return "无军师或军师已死亡";

            int intelligence = advisor.Intelligence;
            int successRate = AdvisorRecommendationConfig.CalculateSuccessRate(intelligence);

            string assessment;
            if (intelligence < AdvisorRecommendationConfig.MinIntelligenceThreshold)
                assessment = "智力不足，无法进行推荐";
            else if (successRate >= 90)
                assessment = "推荐大师，几乎必定成功";
            else if (successRate >= 70)
                assessment = "推荐专家，成功率很高";
            else if (successRate >= 50)
                assessment = "推荐能手，成功率中等";
            else
                assessment = "推荐新手，成功率较低";

            return $"{advisor.Name} (智力{intelligence}): {assessment} (成功率{successRate}%)";
        }
    }

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

    /// <summary>
    /// 招募方式枚举
    /// </summary>
    public enum RecruitMethod
    {
        Direct,         // 直接招募
        Recommendation, // 军师推荐
        Diplomatic,     // 外交招募
        Capture         // 俘虏招降
    }

    /// <summary>
    /// 招募计算器
    /// </summary>
    public static class RecruitmentCalculator
    {
        /// <summary>
        /// 计算初始忠诚度
        /// </summary>
        public static int CalculateInitialLoyalty(Person leader, Person advisor, Person target, RecruitMethod method)
        {
            if (leader == null || target == null) return 50; // 默认值

            int baseLoyalty = 50; // 基础忠诚度

            // 根据招募方式调整
            switch (method)
            {
                case RecruitMethod.Recommendation:
                    if (advisor != null)
                    {
                        // 军师推荐有额外加成
                        baseLoyalty += advisor.Glamour / 10; // 军师魅力影响
                        baseLoyalty += 5; // 推荐加成
                    }
                    break;
                case RecruitMethod.Direct:
                    baseLoyalty += leader.Glamour / 8; // 直接招募看君主魅力
                    break;
            }

            // 相性影响
            int affinityDiff = Math.Abs(leader.Ideal - target.Ideal);
            if (affinityDiff > 75) affinityDiff = 150 - affinityDiff; // 环形距离
            baseLoyalty -= affinityDiff / 3; // 相性差距影响忠诚度

            // 确保在合理范围内
            return Math.Max(20, Math.Min(90, baseLoyalty));
        }
    }
}

// ===================================================================
// 使用示例
// ===================================================================

/*
// 在MainGameScreen中使用军师推荐系统

public class MainGameScreen
{
    private AdvisorRecommendationSystem _advisorRecommendationSystem = new AdvisorRecommendationSystem();

    public void CheckAdvisorRecommendation()
    {
        var currentPlayer = Session.Current.Scenario.CurrentPlayer;
        if (currentPlayer?.Advisor == null) return;

        // 尝试推荐
        var result = _advisorRecommendationSystem.AttemptRecommendation(
            currentPlayer, 
            out Person foundPerson, 
            out int initialLoyalty
        );

        // 处理结果
        _advisorRecommendationSystem.HandleRecommendationResult(
            currentPlayer, 
            result, 
            foundPerson, 
            initialLoyalty
        );

        // 获取统计信息
        var stats = AdvisorRecommendationSystem.GetGlobalStats();
        System.Diagnostics.Debug.WriteLine(stats.GetSummary());
    }
}
*/