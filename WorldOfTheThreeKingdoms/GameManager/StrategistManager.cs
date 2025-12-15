using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// 军师管理系统 - 提供战略预判和建议
    /// </summary>
    public static class StrategistManager
    {
        /// <summary>
        /// 定义预判结果结构
        /// </summary>
        public struct PredictionResult
        {
            public int SuccessRate;   // 成功率 (0-100)
            public string Comment;    // 军师评语
            public bool IsImpossible; // 是否完全不可能
        }

        // --- 模块：人才招募预判 ---

        /// <summary>
        /// 预测招募成功率
        /// 输入：我想招募 targetOfficer，我是 playerFaction
        /// 输出：推荐谁去？成功率多少？
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="targetOfficer">目标武将</param>
        /// <returns>最佳招募者和预测结果</returns>
        public static (Person bestRecruiter, PredictionResult result) PredictRecruitment(Faction playerFaction, Person targetOfficer)
        {
            // 1. 如果没有军师，玩家只能瞎猜
            if (playerFaction.Advisor == null)
            {
                return (null, new PredictionResult
                {
                    SuccessRate = 50,
                    Comment = "（未设军师，吉凶难测）",
                    IsImpossible = false
                });
            }

            // 2. 遍历我方所有武将，找成功率最高的
            Person bestOne = null;
            int maxRate = -1;

            foreach (var recruiter in playerFaction.Persons)
            {
                if (recruiter == null || recruiter.BelongedFaction != playerFaction) continue;

                // 简单的算法示例：成功率 = 魅力 + 政治 - 对方忠诚度 + 修正
                int baseRate = (recruiter.Charm + recruiter.Politics) / 2 - targetOfficer.Loyalty + 50;

                // 关系修正：如果招募者和目标有特殊关系
                int relationBonus = GetRelationBonus(recruiter, targetOfficer);
                baseRate += relationBonus;

                // 相性修正：理想相近的人更容易说服
                int compatibilityBonus = GetCompatibilityBonus(recruiter, targetOfficer);
                baseRate += compatibilityBonus;

                // 有效智力影响判断准确度：有效智力越高，算得越准
                int accuracyMod = GetAccuracyModifier(playerFaction, baseRate);
                int finalRate = baseRate + accuracyMod;

                finalRate = Math.Max(0, Math.Min(100, finalRate)); // 限制在0-100范围内

                if (finalRate > maxRate)
                {
                    maxRate = finalRate;
                    bestOne = recruiter;
                }
            }

            // 3. 生成评语 (很有 San11 的味道)
            string comment = "";
            bool impossible = false;

            if (maxRate >= 90) 
                comment = $"派 {bestOne?.Name} 前去，此事必成！";
            else if (maxRate >= 70) 
                comment = $"若派 {bestOne?.Name} 前去，成功在望。";
            else if (maxRate >= 50) 
                comment = $"派 {bestOne?.Name} 前去，或许有一线生机。";
            else if (maxRate > 20) 
                comment = $"即使是 {bestOne?.Name} 前去，恐怕也难如登天。";
            else
            {
                comment = "彼方心志坚定，我军无人能动摇其心。";
                impossible = true;
            }

            // 添加军师的个人风格评语
            comment = AddStrategistPersonality(playerFaction.Advisor, comment, maxRate);

            return (bestOne, new PredictionResult
            {
                SuccessRate = maxRate,
                Comment = comment,
                IsImpossible = impossible
            });
        }

        /// <summary>
        /// 获取关系加成
        /// </summary>
        private static int GetRelationBonus(Person recruiter, Person target)
        {
            int relationStatus = recruiter.CheckRelation(target);
            
            if (relationStatus == 1) // 亲密关系
                return 20;
            else if (relationStatus == -1) // 厌恶关系
                return -30;
            
            // 检查特殊关系（父子、兄弟、配偶等）
            if (recruiter.Father == target || target.Father == recruiter)
                return 25; // 父子关系
            if (recruiter.Spouse == target || target.Spouse == recruiter)
                return 30; // 配偶关系
            if (recruiter.Brothers.Contains(target))
                return 20; // 兄弟关系

            return 0;
        }

        /// <summary>
        /// 获取相性加成
        /// </summary>
        private static int GetCompatibilityBonus(Person recruiter, Person target)
        {
            int idealDiff = Math.Abs(recruiter.Ideal - target.Ideal);
            
            if (idealDiff <= 10) return 15;      // 理想非常相近
            else if (idealDiff <= 20) return 5;  // 理想比较相近
            else if (idealDiff >= 50) return -10; // 理想差异很大
            
            return 0;
        }

        /// <summary>
        /// 获取有效智力带来的准确度修正
        /// </summary>
        private static int GetAccuracyModifier(Faction faction, int baseRate)
        {
            if (faction?.Advisor == null) return 0;

            // 使用有效智力而非单纯军师智力 - 明主效应
            int effectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
            
            // 高有效智力能更准确地预测，但不会大幅改变基础成功率
            int intelligenceBonus = (effectiveIntelligence - 70) / 10; // 每10点有效智力提供1%修正
            
            // 限制修正范围，避免过度影响
            return Math.Max(-5, Math.Min(5, intelligenceBonus));
        }

        /// <summary>
        /// 根据军师性格添加个人风格的评语
        /// </summary>
        private static string AddStrategistPersonality(Person strategist, string baseComment, int successRate)
        {
            if (strategist == null) return baseComment;

            // 根据军师的性格ID添加不同风格的评语
            int personalityId = strategist.Character?.ID ?? 0;
            
            switch (personalityId)
            {
                case 0: // 仁德型军师
                    if (successRate >= 70)
                        return baseComment + " 以德服人，此乃上策。";
                    else
                        return baseComment + " 强求不得，当以诚待之。";
                        
                case 1: // 霸道型军师
                    if (successRate >= 70)
                        return baseComment + " 展现实力，令其折服！";
                    else
                        return baseComment + " 既然软的不行，或可考虑其他手段。";
                        
                case 2: // 冷静型军师
                    return baseComment + $" 据臣分析，成功概率约为 {successRate}%。";
                    
                case 3: // 莽撞型军师
                    if (successRate >= 50)
                        return baseComment + " 管他三七二十一，试试再说！";
                    else
                        return baseComment + " 这...臣也没什么好办法。";
                        
                case 4: // 狡诈型军师
                    if (successRate >= 70)
                        return baseComment + " 嘿嘿，此人必入我彀中。";
                    else
                        return baseComment + " 此人不好对付，需另想妙计。";
                        
                default:
                    return baseComment;
            }
        }

        /// <summary>
        /// 新增：带有有效智力误差的预测系统
        /// </summary>
        /// <param name="faction">玩家势力</param>
        /// <param name="target">目标武将</param>
        /// <returns>军师预测的成功率和评语</returns>
        public static (int predictedRate, string comment) GetRecruitPrediction(Faction faction, Person target)
        {
            // 1. 计算【真实概率】 (只有上帝知道)
            // 假设公式：成功率 = (我方魅力 - 敌方忠诚度 + 杂项)
            int realRate = CalculateRealRecruitRate(faction, target);

            // 2. 如果没军师，完全瞎蒙
            if (faction.Advisor == null)
            {
                return (50, "（无人分析，吉凶难测）"); // 默认显示 50%，其实是盲盒
            }

            // 3. 计算【有效智力误差】- 明主效应
            // 使用有效智力而非单纯军师智力
            // 有效智力 100 => 误差 0%
            // 有效智力 50  => 误差 ±25%
            // 有效智力 10  => 误差 ±45%
            int effectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(faction);
            int errorMargin = (100 - effectiveIntelligence) / 2;

            // 生成一个随机误差 (军师可能高估，也可能低估)
            Random random = new Random();
            int randomError = random.Next(-errorMargin, errorMargin + 1);

            // 最终军师告诉你的概率 (限制在 0-100)
            int predictedRate = Math.Max(0, Math.Min(100, realRate + randomError));

            // 4. 生成评语
            string comment = GetCommentByRate(predictedRate, faction.Advisor);

            // 特殊彩蛋：如果军师智力低 (比如 < 60)，且误差极大，增加一句迷惑性台词
            if (faction.Advisor.Intelligence < 60 && Math.Abs(randomError) > 20)
            {
                comment += "（军师似乎对此很有信心...？）";
            }

            // 调试信息：显示真实成功率 vs 预测成功率
            System.Diagnostics.Debug.WriteLine($"[StrategistManager] 真实成功率: {realRate}%, 军师预测: {predictedRate}%, 有效智力: {effectiveIntelligence}, 误差: {randomError}%");

            return (predictedRate, comment);
        }

        /// <summary>
        /// 计算真实的招募成功率（游戏内部使用，玩家不知道）
        /// </summary>
        private static int CalculateRealRecruitRate(Faction faction, Person target)
        {
            if (faction == null || target == null) return 0;

            // 找到最佳招募者
            var (bestRecruiter, _) = PredictRecruitment(faction, target);
            if (bestRecruiter == null) return 0;

            // 使用之前的算法计算真实成功率
            int baseRate = (bestRecruiter.Charm + bestRecruiter.Politics) / 2 - target.Loyalty + 50;
            int relationBonus = GetRelationBonus(bestRecruiter, target);
            int compatibilityBonus = GetCompatibilityBonus(bestRecruiter, target);
            
            int realRate = baseRate + relationBonus + compatibilityBonus;
            return Math.Max(0, Math.Min(100, realRate));
        }

        /// <summary>
        /// 根据预测成功率生成评语
        /// </summary>
        private static string GetCommentByRate(int predictedRate, Person strategist)
        {
            string baseComment = "";

            if (predictedRate >= 90)
                baseComment = "此事必成，主公可放心！";
            else if (predictedRate >= 70)
                baseComment = "成功在望，值得一试。";
            else if (predictedRate >= 50)
                baseComment = "或有一线生机，可以尝试。";
            else if (predictedRate >= 30)
                baseComment = "此事颇为困难，需谨慎考虑。";
            else if (predictedRate >= 10)
                baseComment = "成功希望渺茫，恐难如愿。";
            else
                baseComment = "此事几无可能，不如作罢。";

            // 根据军师性格添加个人风格
            return AddStrategistPersonality(strategist, baseComment, predictedRate);
        }

        /// <summary>
        /// 获取军师的表情状态（用于UI显示）
        /// </summary>
        /// <param name="predictedRate">预测成功率</param>
        /// <returns>表情状态描述</returns>
        public static string GetStrategistExpression(int predictedRate)
        {
            if (predictedRate >= 80) return "confident"; // 自信微笑
            else if (predictedRate >= 60) return "optimistic"; // 乐观
            else if (predictedRate >= 40) return "neutral"; // 中性
            else if (predictedRate >= 20) return "worried"; // 担忧
            else return "concerned"; // 摇头/流汗
        }

        /// <summary>
        /// 获取成功率显示颜色
        /// </summary>
        /// <param name="predictedRate">预测成功率</param>
        /// <returns>颜色</returns>
        public static Color GetSuccessRateColor(int predictedRate)
        {
            if (predictedRate >= 70) return Color.Green;      // 绿色：高成功率
            else if (predictedRate >= 50) return Color.Orange; // 橙色：中等成功率
            else if (predictedRate >= 30) return Color.Yellow; // 黄色：低成功率
            else return Color.Red;                             // 红色：极低成功率
        }

        /// <summary>
        /// 获取详细的招募分析报告
        /// </summary>
        public static string GetRecruitmentAnalysis(Faction playerFaction, Person targetOfficer)
        {
            if (playerFaction.Advisor == null)
                return "未设军师，无法提供详细分析。";

            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== 军师 {playerFaction.Advisor.Name} 的招募分析 ===");
            analysis.AppendLine($"目标武将: {targetOfficer.Name}");
            analysis.AppendLine($"  忠诚度: {targetOfficer.Loyalty}");
            analysis.AppendLine($"  理想: {targetOfficer.Ideal}");
            analysis.AppendLine();

            // 显示军师的分析能力
            int intelligence = playerFaction.Advisor.Intelligence;
            if (intelligence >= 90)
                analysis.AppendLine("军师分析能力: ★★★★★ (洞察秋毫)");
            else if (intelligence >= 80)
                analysis.AppendLine("军师分析能力: ★★★★☆ (智谋过人)");
            else if (intelligence >= 70)
                analysis.AppendLine("军师分析能力: ★★★☆☆ (颇有见地)");
            else if (intelligence >= 60)
                analysis.AppendLine("军师分析能力: ★★☆☆☆ (略有小智)");
            else
                analysis.AppendLine("军师分析能力: ★☆☆☆☆ (见识有限)");

            analysis.AppendLine();
            analysis.AppendLine("我方招募候选人分析:");
            
            foreach (var recruiter in playerFaction.Persons.Take(5)) // 只显示前5个
            {
                if (recruiter == null || recruiter.BelongedFaction != playerFaction) continue;

                int baseRate = (recruiter.Charm + recruiter.Politics) / 2 - targetOfficer.Loyalty + 50;
                int relationBonus = GetRelationBonus(recruiter, targetOfficer);
                int compatibilityBonus = GetCompatibilityBonus(recruiter, targetOfficer);
                int finalRate = Math.Max(0, Math.Min(100, baseRate + relationBonus + compatibilityBonus));

                analysis.AppendLine($"  {recruiter.Name}: {finalRate}% (魅力{recruiter.Charm} 政治{recruiter.Politics})");
                if (relationBonus != 0)
                    analysis.AppendLine($"    关系修正: {relationBonus:+#;-#;0}%");
                if (compatibilityBonus != 0)
                    analysis.AppendLine($"    相性修正: {compatibilityBonus:+#;-#;0}%");
            }

            // 添加军师的个人见解
            analysis.AppendLine();
            analysis.AppendLine($"军师 {playerFaction.Advisor.Name} 的个人见解:");
            
            int personalityId = playerFaction.Advisor.Character?.ID ?? 0;
            switch (personalityId)
            {
                case 0: // 仁德型
                    analysis.AppendLine("  「以诚待人，以德服人，方为长久之计。」");
                    break;
                case 1: // 霸道型
                    analysis.AppendLine("  「展现我军实力，令其心悦诚服！」");
                    break;
                case 2: // 冷静型
                    analysis.AppendLine("  「需理性分析各种因素，不可感情用事。」");
                    break;
                case 3: // 莽撞型
                    analysis.AppendLine("  「管他成不成功，试试再说！」");
                    break;
                case 4: // 狡诈型
                    analysis.AppendLine("  「此人心思，某已看透七八分...」");
                    break;
                default:
                    analysis.AppendLine("  「此事成败，还需主公定夺。」");
                    break;
            }

            return analysis.ToString();
        }

        // --- 模块：战斗预判 ---

        /// <summary>
        /// 预测战斗结果
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="playerTroop">我方部队</param>
        /// <param name="enemyTroop">敌方部队</param>
        /// <returns>战斗预测结果</returns>
        public static PredictionResult PredictBattle(Faction playerFaction, Troop playerTroop, Troop enemyTroop)
        {
            if (playerFaction.Advisor == null)
            {
                return new PredictionResult
                {
                    SuccessRate = 50,
                    Comment = "（未设军师，战况难料）",
                    IsImpossible = false
                };
            }

            // 计算双方战力对比
            int ourPower = CalculateTroopPower(playerTroop);
            int enemyPower = CalculateTroopPower(enemyTroop);
            
            // 基础胜率计算
            int winRate = 50 + (ourPower - enemyPower) / 100;
            
            // 地形修正（如果有地形系统）
            // int terrainBonus = GetTerrainBonus(playerTroop, enemyTroop);
            // winRate += terrainBonus;

            // 有效智力影响预测准确度 - 明主效应
            int effectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(playerFaction);
            int strategistBonus = (effectiveIntelligence - 70) / 5;
            winRate += strategistBonus;

            winRate = Math.Max(5, Math.Min(95, winRate));

            // 生成评语
            string comment = GenerateBattleComment(playerFaction.Advisor, winRate, ourPower, enemyPower);

            return new PredictionResult
            {
                SuccessRate = winRate,
                Comment = comment,
                IsImpossible = winRate < 10
            };
        }

        /// <summary>
        /// 计算部队战力
        /// </summary>
        private static int CalculateTroopPower(Troop troop)
        {
            if (troop == null || troop.Leader == null) return 0;

            // 基础战力 = 兵力 * (统率 + 武力) / 2
            int basePower = troop.Quantity * (troop.Leader.Command + troop.Leader.Strength) / 2;
            
            // 士气影响
            basePower = basePower * troop.Morale / 100;
            
            return basePower;
        }

        /// <summary>
        /// 生成战斗评语
        /// </summary>
        private static string GenerateBattleComment(Person strategist, int winRate, int ourPower, int enemyPower)
        {
            string baseComment = "";
            
            if (winRate >= 80)
                baseComment = "我军占尽优势，此战必胜！";
            else if (winRate >= 60)
                baseComment = "我军略占上风，胜算较大。";
            else if (winRate >= 40)
                baseComment = "双方实力相当，胜负难料。";
            else if (winRate >= 20)
                baseComment = "敌军实力强劲，此战凶险。";
            else
                baseComment = "敌强我弱，不宜硬战。";

            // 根据军师性格添加个人风格
            return AddStrategistPersonality(strategist, baseComment, winRate);
        }

        // --- 模块：外交预判 ---

        /// <summary>
        /// 预测外交成功率
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="targetFaction">目标势力</param>
        /// <param name="diplomaticAction">外交行动类型</param>
        /// <returns>外交预测结果</returns>
        public static PredictionResult PredictDiplomacy(Faction playerFaction, Faction targetFaction, string diplomaticAction)
        {
            if (playerFaction.Advisor == null)
            {
                return new PredictionResult
                {
                    SuccessRate = 50,
                    Comment = "（未设军师，外交成败难测）",
                    IsImpossible = false
                };
            }

            // 基础成功率
            int baseRate = 50;

            // 势力关系影响
            // int relationMod = GetFactionRelationModifier(playerFaction, targetFaction);
            // baseRate += relationMod;

            // 实力对比影响
            int powerDiff = GetFactionPowerDifference(playerFaction, targetFaction);
            if (diplomaticAction == "结盟")
            {
                // 实力相当更容易结盟
                baseRate += Math.Max(-20, Math.Min(20, -Math.Abs(powerDiff) / 5));
            }
            else if (diplomaticAction == "威胁")
            {
                // 我方越强，威胁越有效
                baseRate += powerDiff / 3;
            }

            // 有效智力影响外交判断 - 明主效应
            int effectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(playerFaction);
            // 结合军师政治能力和有效智力
            int diplomaticBonus = (playerFaction.Advisor.Politics + effectiveIntelligence) / 2 - 70;
            baseRate += diplomaticBonus / 3;

            baseRate = Math.Max(5, Math.Min(95, baseRate));

            string comment = GenerateDiplomacyComment(playerFaction.Advisor, diplomaticAction, baseRate);

            return new PredictionResult
            {
                SuccessRate = baseRate,
                Comment = comment,
                IsImpossible = baseRate < 10
            };
        }

        /// <summary>
        /// 获取势力实力差异
        /// </summary>
        private static int GetFactionPowerDifference(Faction faction1, Faction faction2)
        {
            // 简化的实力计算：城市数量 + 武将数量
            int power1 = faction1.Architectures.Count * 10 + faction1.Persons.Count;
            int power2 = faction2.Architectures.Count * 10 + faction2.Persons.Count;
            
            return power1 - power2;
        }

        /// <summary>
        /// 生成外交评语
        /// </summary>
        private static string GenerateDiplomacyComment(Person strategist, string action, int successRate)
        {
            string baseComment = "";
            
            switch (action)
            {
                case "结盟":
                    if (successRate >= 70)
                        baseComment = "彼方应会同意结盟之议。";
                    else if (successRate >= 40)
                        baseComment = "结盟一事，尚有可能。";
                    else
                        baseComment = "对方恐难接受结盟。";
                    break;
                    
                case "威胁":
                    if (successRate >= 70)
                        baseComment = "以我军威势，足以震慑对方。";
                    else if (successRate >= 40)
                        baseComment = "威胁或许有效，但需谨慎行事。";
                    else
                        baseComment = "对方实力不弱，威胁恐难奏效。";
                    break;
                    
                default:
                    baseComment = "此事成败，尚在未定之天。";
                    break;
            }

            return AddStrategistPersonality(strategist, baseComment, successRate);
        }

        // --- 模块：综合建议 ---

        // --- 模块：战斗技能预判 ---

        /// <summary>
        /// 战斗技能类型枚举
        /// </summary>
        public enum SkillType
        {
            FirePlot,      // 火计
            WaterPlot,     // 水计
            Ambush,        // 伏兵
            Provoke,       // 挑衅
            Confuse,       // 混乱
            Retreat,       // 撤退
            Rally          // 鼓舞
        }

        /// <summary>
        /// 获取战斗技能成功率预测（带有效智力误差）
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="targetTroop">目标部队</param>
        /// <param name="skillType">技能类型</param>
        /// <returns>军师预测的成功率</returns>
        public static int GetBattlePrediction(Faction playerFaction, Troop targetTroop, SkillType skillType)
        {
            // 1. 计算真实成功率
            int realRate = CalculateRealSkillSuccessRate(playerFaction, targetTroop, skillType);

            // 2. 如果没军师，返回未知
            if (playerFaction.Advisor == null)
            {
                return -1; // 返回-1表示未知，UI显示为 ??%
            }

            // 3. 使用有效智力计算误差 - 明主效应
            int effectiveIntelligence = EffectiveIntelligenceSystem.GetEffectiveIntelligence(playerFaction);
            int errorMargin = (100 - effectiveIntelligence) / 3; // 战斗预测误差稍小一些
            Random random = new Random();
            int randomError = random.Next(-errorMargin, errorMargin + 1);

            // 4. 最终预测成功率
            int predictedRate = Math.Max(5, Math.Min(95, realRate + randomError));

            System.Diagnostics.Debug.WriteLine($"[BattlePrediction] {skillType} vs {targetTroop.Leader?.Name}: 真实{realRate}% -> 预测{predictedRate}% (有效智力{effectiveIntelligence}, 误差{randomError})");

            return predictedRate;
        }

        /// <summary>
        /// 计算技能的真实成功率
        /// </summary>
        private static int CalculateRealSkillSuccessRate(Faction playerFaction, Troop targetTroop, SkillType skillType)
        {
            if (playerFaction?.Leader == null || targetTroop?.Leader == null) return 0;

            Person caster = playerFaction.Leader; // 假设是君主施展技能
            Person target = targetTroop.Leader;

            int baseRate = 50; // 基础成功率

            switch (skillType)
            {
                case SkillType.FirePlot:
                    // 火计成功率 = 施法者智力 - 目标智力 + 地形修正
                    baseRate = caster.Intelligence - target.Intelligence + 30;
                    
                    // 地形修正（这里简化处理）
                    // if (terrain == TerrainType.Forest) baseRate += 20;
                    // if (terrain == TerrainType.Grassland) baseRate += 10;
                    // if (terrain == TerrainType.Water) baseRate -= 30;
                    
                    // 季节修正
                    // if (season == Season.Summer) baseRate += 15;
                    // if (season == Season.Winter) baseRate -= 15;
                    break;

                case SkillType.WaterPlot:
                    // 水计成功率 = 施法者智力 - 目标智力 + 地形修正
                    baseRate = caster.Intelligence - target.Intelligence + 25;
                    // if (terrain == TerrainType.River) baseRate += 25;
                    // if (terrain == TerrainType.Mountain) baseRate -= 20;
                    break;

                case SkillType.Ambush:
                    // 伏兵成功率 = 施法者智力 + 统率 - 目标智力
                    baseRate = (caster.Intelligence + caster.Command) / 2 - target.Intelligence + 20;
                    break;

                case SkillType.Provoke:
                    // 挑衅成功率 = 施法者魅力 - 目标冷静度
                    baseRate = caster.Charm - target.Calmness + 40;
                    break;

                case SkillType.Confuse:
                    // 混乱成功率 = 施法者智力 - 目标智力 - 目标冷静度
                    baseRate = caster.Intelligence - target.Intelligence - target.Calmness / 2 + 35;
                    break;

                case SkillType.Retreat:
                    // 撤退成功率主要看自己的能力
                    baseRate = (caster.Command + caster.Intelligence) / 2 + 30;
                    break;

                case SkillType.Rally:
                    // 鼓舞成功率主要看魅力和统率
                    baseRate = (caster.Charm + caster.Command) / 2 + 25;
                    break;
            }

            // 士气影响
            if (targetTroop.Morale < 50) baseRate += 15; // 敌军士气低更容易成功
            if (targetTroop.Morale > 80) baseRate -= 10; // 敌军士气高更难成功

            // 兵力对比影响
            // int powerRatio = playerTroop.Quantity / Math.Max(1, targetTroop.Quantity);
            // if (powerRatio > 2) baseRate += 10; // 兵力优势
            // if (powerRatio < 0.5) baseRate -= 10; // 兵力劣势

            return Math.Max(5, Math.Min(95, baseRate));
        }

        /// <summary>
        /// 获取技能预测的详细分析
        /// </summary>
        public static string GetSkillPredictionAnalysis(Faction playerFaction, Troop targetTroop, SkillType skillType)
        {
            if (playerFaction.Advisor == null)
                return "未设军师，无法分析技能成功率。";

            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== 军师 {playerFaction.Advisor.Name} 的技能分析 ===");
            analysis.AppendLine($"技能: {GetSkillName(skillType)}");
            analysis.AppendLine($"目标: {targetTroop.Leader?.Name ?? "未知"} 部队");
            analysis.AppendLine();

            // 分析各种因素
            Person caster = playerFaction.Leader;
            Person target = targetTroop.Leader;

            if (caster != null && target != null)
            {
                analysis.AppendLine("能力对比:");
                analysis.AppendLine($"  我方智力: {caster.Intelligence}");
                analysis.AppendLine($"  敌方智力: {target.Intelligence}");
                analysis.AppendLine($"  敌方冷静: {target.Calmness}");
                analysis.AppendLine($"  敌军士气: {targetTroop.Morale}");
                analysis.AppendLine();

                // 技能特定分析
                switch (skillType)
                {
                    case SkillType.FirePlot:
                        analysis.AppendLine("火计分析:");
                        analysis.AppendLine("  - 需要考虑地形和季节因素");
                        analysis.AppendLine("  - 森林和草地有利，水域不利");
                        analysis.AppendLine("  - 夏季成功率更高");
                        break;

                    case SkillType.WaterPlot:
                        analysis.AppendLine("水计分析:");
                        analysis.AppendLine("  - 需要靠近水源");
                        analysis.AppendLine("  - 地势高低影响效果");
                        break;

                    case SkillType.Provoke:
                        analysis.AppendLine("挑衅分析:");
                        analysis.AppendLine("  - 目标冷静度越低越容易成功");
                        analysis.AppendLine("  - 我方魅力是关键因素");
                        break;
                }
            }

            // 军师建议
            int predictedRate = GetBattlePrediction(playerFaction, targetTroop, skillType);
            analysis.AppendLine($"预测成功率: {predictedRate}%");
            
            if (predictedRate >= 70)
                analysis.AppendLine("军师建议: 成功率很高，可以放心使用！");
            else if (predictedRate >= 50)
                analysis.AppendLine("军师建议: 有一定成功率，值得尝试。");
            else if (predictedRate >= 30)
                analysis.AppendLine("军师建议: 成功率较低，需谨慎考虑。");
            else
                analysis.AppendLine("军师建议: 成功率极低，不建议使用。");

            return analysis.ToString();
        }

        /// <summary>
        /// 获取技能中文名称
        /// </summary>
        private static string GetSkillName(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.FirePlot: return "火计";
                case SkillType.WaterPlot: return "水计";
                case SkillType.Ambush: return "伏兵";
                case SkillType.Provoke: return "挑衅";
                case SkillType.Confuse: return "混乱";
                case SkillType.Retreat: return "撤退";
                case SkillType.Rally: return "鼓舞";
                default: return "未知技能";
            }
        }

        /// <summary>
        /// 获取军师的综合战略建议
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <returns>战略建议</returns>
        public static string GetStrategicAdvice(Faction playerFaction)
        {
            if (playerFaction.Advisor == null)
                return "主公未设军师，无人可提供战略建议。";

            var advice = new System.Text.StringBuilder();
            advice.AppendLine($"=== 军师 {playerFaction.Advisor.Name} 的战略建议 ===");
            
            // 分析当前形势
            advice.AppendLine("当前形势分析：");
            
            // 实力评估
            int cityCount = playerFaction.Architectures.Count;
            int officerCount = playerFaction.Persons.Count;
            
            if (cityCount >= 5)
                advice.AppendLine("  我军已占据多座城池，实力雄厚。");
            else if (cityCount >= 2)
                advice.AppendLine("  我军初具规模，当稳步发展。");
            else
                advice.AppendLine("  我军根基尚浅，需韬光养晦。");

            // 人才状况
            if (officerCount >= 10)
                advice.AppendLine("  麾下人才济济，可图大业。");
            else if (officerCount >= 5)
                advice.AppendLine("  人才尚可，但仍需广纳贤士。");
            else
                advice.AppendLine("  急需招揽人才，充实实力。");

            advice.AppendLine();
            advice.AppendLine("建议方针：");
            
            // 根据军师性格给出不同风格的建议
            int personalityId = playerFaction.Advisor.Character?.ID ?? 0;
            switch (personalityId)
            {
                case 0: // 仁德型
                    advice.AppendLine("  以德治军，仁政安民，自然人心归附。");
                    break;
                case 1: // 霸道型
                    advice.AppendLine("  当展现实力，威震四方，令敌胆寒。");
                    break;
                case 2: // 冷静型
                    advice.AppendLine("  宜稳扎稳打，步步为营，不可冒进。");
                    break;
                case 3: // 莽撞型
                    advice.AppendLine("  机不可失，当速战速决，抢占先机！");
                    break;
                case 4: // 狡诈型
                    advice.AppendLine("  可用计谋分化敌军，坐收渔翁之利。");
                    break;
                default:
                    advice.AppendLine("  当审时度势，因势利导。");
                    break;
            }

            return advice.ToString();
        }
    }
}