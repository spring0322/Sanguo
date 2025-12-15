using System;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 军师预测系统
    /// 基于AdvisorDataHelper扩展，提供各种游戏行动的成功率预测
    /// 军师智力影响预测准确度，性格影响预测偏向
    /// </summary>
    public static class AdvisorPredictionSystem
    {
        /// <summary>
        /// 获取招募成功率显示值
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">招募目标</param>
        /// <returns>军师预测的招募成功率</returns>
        public static int GetRecruitChanceDisplay(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return 0;

                int realChance = CalculateRecruitChance(target); // 真实概率
                
                // 用同样的算法对"成功率"进行模糊化
                // 这样 100智力 军师看到的成功率是 100% 准的
                // 傻瓜军师可能会说"必成"(显示100%)，结果去了失败了
                int observedChance = AdvisorDataHelper.GetObservedValue(advisor, target, realChance, "RecruitChance");
                
                System.Diagnostics.Debug.WriteLine($"[招募预测] {advisor.Name} 预测招募 {target.Name}: 真实成功率{realChance}% → 预测成功率{observedChance}%");
                
                return observedChance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募预测] GetRecruitChanceDisplay 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取招募成功率的文字描述
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">招募目标</param>
        /// <returns>成功率的文字描述</returns>
        public static string GetRecruitChanceDescription(Person advisor, Person target)
        {
            try
            {
                int observedChance = GetRecruitChanceDisplay(advisor, target);
                
                // 根据军师智力决定描述精度
                if (advisor.Intelligence >= 95)
                {
                    return $"{observedChance}%"; // 精确百分比
                }
                else if (advisor.Intelligence >= 80)
                {
                    // 模糊区间
                    int low = Math.Max(0, observedChance - 10);
                    int high = Math.Min(100, observedChance + 10);
                    return $"{low}%-{high}%";
                }
                else
                {
                    // 文字描述
                    return observedChance switch
                    {
                        >= 90 => "十拿九稳",
                        >= 70 => "很有把握",
                        >= 50 => "五五开",
                        >= 30 => "希望不大",
                        _ => "几乎不可能"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募预测] GetRecruitChanceDescription 失败: {ex.Message}");
                return "未知";
            }
        }

        /// <summary>
        /// 获取外交成功率预测
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="targetFaction">目标势力</param>
        /// <param name="diplomacyType">外交类型</param>
        /// <returns>预测的外交成功率</returns>
        public static int GetDiplomacyChanceDisplay(Person advisor, Faction targetFaction, string diplomacyType)
        {
            try
            {
                if (advisor == null || targetFaction == null) return 0;

                int realChance = CalculateDiplomacyChance(targetFaction, diplomacyType);
                
                // 使用目标势力君主作为观测对象
                Person targetLeader = targetFaction.Leader;
                if (targetLeader == null) return realChance;

                int observedChance = AdvisorDataHelper.GetObservedValue(advisor, targetLeader, realChance, $"Diplomacy_{diplomacyType}");
                
                System.Diagnostics.Debug.WriteLine($"[外交预测] {advisor.Name} 预测对 {targetFaction.Name} 的 {diplomacyType}: 真实成功率{realChance}% → 预测成功率{observedChance}%");
                
                return observedChance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[外交预测] GetDiplomacyChanceDisplay 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取战斗胜率预测
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="ourTroop">我方部队</param>
        /// <param name="enemyTroop">敌方部队</param>
        /// <returns>预测的战斗胜率</returns>
        public static int GetBattleChanceDisplay(Person advisor, Troop ourTroop, Troop enemyTroop)
        {
            try
            {
                if (advisor == null || ourTroop == null || enemyTroop == null) return 50;

                int realChance = CalculateBattleChance(ourTroop, enemyTroop);
                
                // 使用敌方主将作为观测对象
                Person enemyLeader = enemyTroop.Leader;
                if (enemyLeader == null) return realChance;

                int observedChance = AdvisorDataHelper.GetObservedValue(advisor, enemyLeader, realChance, "BattleChance");
                
                System.Diagnostics.Debug.WriteLine($"[战斗预测] {advisor.Name} 预测对 {enemyLeader.Name} 部队的胜率: 真实胜率{realChance}% → 预测胜率{observedChance}%");
                
                return observedChance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战斗预测] GetBattleChanceDisplay 失败: {ex.Message}");
                return 50;
            }
        }

        /// <summary>
        /// 获取计谋成功率预测
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物或建筑</param>
        /// <param name="stratagemType">计谋类型</param>
        /// <returns>预测的计谋成功率</returns>
        public static int GetStratagemChanceDisplay(Person advisor, Person target, string stratagemType)
        {
            try
            {
                if (advisor == null || target == null) return 0;

                int realChance = CalculateStratagemChance(advisor, target, stratagemType);
                int observedChance = AdvisorDataHelper.GetObservedValue(advisor, target, realChance, $"Stratagem_{stratagemType}");
                
                System.Diagnostics.Debug.WriteLine($"[计谋预测] {advisor.Name} 预测对 {target.Name} 使用 {stratagemType}: 真实成功率{realChance}% → 预测成功率{observedChance}%");
                
                return observedChance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[计谋预测] GetStratagemChanceDisplay 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取技术研发成功率预测
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="technique">技术</param>
        /// <returns>预测的研发成功率</returns>
        public static int GetTechResearchChanceDisplay(Person advisor, Technique technique)
        {
            try
            {
                if (advisor == null || technique == null) return 0;

                int realChance = CalculateTechResearchChance(technique);
                
                // 使用军师自己作为观测对象，因为技术研发主要依赖军师能力
                int observedChance = AdvisorDataHelper.GetObservedValue(advisor, advisor, realChance, $"TechResearch_{technique.ID}");
                
                System.Diagnostics.Debug.WriteLine($"[技术预测] {advisor.Name} 预测研发 {technique.Name}: 真实成功率{realChance}% → 预测成功率{observedChance}%");
                
                return observedChance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[技术预测] GetTechResearchChanceDisplay 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取综合预测报告
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="target">目标对象</param>
        /// <returns>详细的预测报告</returns>
        public static string GetPredictionReport(Person advisor, string actionType, object target)
        {
            try
            {
                if (advisor == null) return "无军师，无法预测";

                var report = new System.Text.StringBuilder();
                report.AppendLine($"=== {advisor.Name} 的预测报告 ===");
                report.AppendLine($"行动类型: {actionType}");
                report.AppendLine($"军师智力: {advisor.Intelligence}");
                report.AppendLine($"预测可信度: {AdvisorDataHelper.GetAccuracyAssessment(advisor)}");
                report.AppendLine();

                switch (actionType.ToLower())
                {
                    case "recruit":
                        if (target is Person person)
                        {
                            int chance = GetRecruitChanceDisplay(advisor, person);
                            string desc = GetRecruitChanceDescription(advisor, person);
                            report.AppendLine($"招募 {person.Name}:");
                            report.AppendLine($"  预测成功率: {chance}%");
                            report.AppendLine($"  军师评价: {desc}");
                            report.AppendLine($"  建议: {GetRecruitAdvice(chance)}");
                        }
                        break;

                    case "diplomacy":
                        if (target is Faction faction)
                        {
                            var diplomacyTypes = new[] { "Alliance", "Trade", "NonAggression" };
                            report.AppendLine($"对 {faction.Name} 的外交预测:");
                            
                            foreach (string dipType in diplomacyTypes)
                            {
                                int chance = GetDiplomacyChanceDisplay(advisor, faction, dipType);
                                report.AppendLine($"  {dipType}: {chance}% - {GetDiplomacyAdvice(chance)}");
                            }
                        }
                        break;

                    case "battle":
                        if (target is Troop enemyTroop && advisor.BelongedFaction?.Troops?.Count > 0)
                        {
                            foreach (Troop ourTroop in advisor.BelongedFaction.Troops.GetList())
                            {
                                int chance = GetBattleChanceDisplay(advisor, ourTroop, enemyTroop);
                                report.AppendLine($"{ourTroop.Name} vs {enemyTroop.Name}:");
                                report.AppendLine($"  预测胜率: {chance}%");
                                report.AppendLine($"  建议: {GetBattleAdvice(chance)}");
                                break; // 只显示第一支部队的预测
                            }
                        }
                        break;
                }

                // 添加军师性格影响说明
                report.AppendLine();
                report.AppendLine("注意事项:");
                report.AppendLine(GetPersonalityWarning(advisor));

                return report.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测报告] GetPredictionReport 失败: {ex.Message}");
                return "预测报告生成失败";
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 计算真实招募成功率
        /// </summary>
        private static int CalculateRecruitChance(Person target)
        {
            // 简化的招募成功率计算
            // 实际游戏中应该考虑更多因素
            int baseChance = 50;
            
            // 基于忠诚度调整
            baseChance -= target.Loyalty / 2;
            
            // 基于魅力调整
            Faction currentFaction = Session.Current.Scenario.CurrentFaction;
            if (currentFaction?.Leader != null)
            {
                baseChance += (currentFaction.Leader.Glamour - 50) / 2;
            }
            
            return Math.Clamp(baseChance, 5, 95);
        }

        /// <summary>
        /// 计算真实外交成功率
        /// </summary>
        private static int CalculateDiplomacyChance(Faction targetFaction, string diplomacyType)
        {
            // 简化的外交成功率计算
            int baseChance = diplomacyType switch
            {
                "Alliance" => 30,
                "Trade" => 60,
                "NonAggression" => 45,
                _ => 40
            };
            
            // 基于势力关系调整
            // 这里需要实际的势力关系系统
            
            return Math.Clamp(baseChance, 5, 95);
        }

        /// <summary>
        /// 计算真实战斗胜率
        /// </summary>
        private static int CalculateBattleChance(Troop ourTroop, Troop enemyTroop)
        {
            // 简化的战斗胜率计算
            if (ourTroop.Quantity == 0 || enemyTroop.Quantity == 0) return 50;
            
            float ourPower = ourTroop.Quantity * (ourTroop.Leader?.Command ?? 50);
            float enemyPower = enemyTroop.Quantity * (enemyTroop.Leader?.Command ?? 50);
            
            float ratio = ourPower / (ourPower + enemyPower);
            int chance = (int)(ratio * 100);
            
            return Math.Clamp(chance, 5, 95);
        }

        /// <summary>
        /// 计算真实计谋成功率
        /// </summary>
        private static int CalculateStratagemChance(Person advisor, Person target, string stratagemType)
        {
            // 简化的计谋成功率计算
            int baseChance = 50;
            
            // 基于智力差调整
            baseChance += (advisor.Intelligence - target.Intelligence) / 2;
            
            // 基于计谋类型调整
            baseChance += stratagemType switch
            {
                "Confusion" => -10,  // 混乱较难
                "Persuade" => 0,     // 劝降中等
                "Sabotage" => -5,    // 破坏较难
                _ => 0
            };
            
            return Math.Clamp(baseChance, 5, 95);
        }

        /// <summary>
        /// 计算真实技术研发成功率
        /// </summary>
        private static int CalculateTechResearchChance(Technique technique)
        {
            // 简化的技术研发成功率计算
            Faction currentFaction = Session.Current.Scenario.CurrentFaction;
            if (currentFaction?.Advisor == null) return 30;
            
            int baseChance = 60;
            
            // 基于军师智力调整
            baseChance += (currentFaction.Advisor.Intelligence - 70) / 2;
            
            // 基于技术难度调整（假设技术有难度属性）
            // baseChance -= technique.Difficulty * 5;
            
            return Math.Clamp(baseChance, 10, 90);
        }

        /// <summary>
        /// 获取招募建议
        /// </summary>
        private static string GetRecruitAdvice(int chance)
        {
            return chance switch
            {
                >= 80 => "强烈建议招募",
                >= 60 => "可以尝试招募",
                >= 40 => "谨慎考虑",
                >= 20 => "不太建议",
                _ => "几乎不可能成功"
            };
        }

        /// <summary>
        /// 获取外交建议
        /// </summary>
        private static string GetDiplomacyAdvice(int chance)
        {
            return chance switch
            {
                >= 70 => "时机很好",
                >= 50 => "可以尝试",
                >= 30 => "需要准备",
                _ => "暂缓进行"
            };
        }

        /// <summary>
        /// 获取战斗建议
        /// </summary>
        private static string GetBattleAdvice(int chance)
        {
            return chance switch
            {
                >= 80 => "必胜之战",
                >= 60 => "胜算较大",
                >= 40 => "势均力敌",
                >= 20 => "劣势明显",
                _ => "避免交战"
            };
        }

        /// <summary>
        /// 获取性格警告
        /// </summary>
        private static string GetPersonalityWarning(Person advisor)
        {
            return advisor.CharacterKindID switch
            {
                0 => "仁德型军师可能过于乐观，实际成功率可能更低",
                1 => "多疑型军师倾向保守估计，实际成功率可能更高",
                2 => "莽撞型军师判断极端化，需要谨慎参考",
                3 => "狡诈型军师会根据关系调整预测，注意其主观性",
                _ => "该军师预测相对客观，可作为重要参考"
            };
        }

        #endregion
    }
}