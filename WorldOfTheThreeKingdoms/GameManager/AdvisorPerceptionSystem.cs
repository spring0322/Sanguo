using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师感知系统 - 处理军师对忠诚度的观测和判断
    /// 实现智力影响观测准确度，性格影响判断偏向的完整系统
    /// </summary>
    public static class AdvisorPerceptionSystem
    {
        /// <summary>
        /// 获取军师感知到的忠诚度（基础版本）
        /// 使用固定种子确保同一军师看同一人的偏差永远固定
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>军师感知到的忠诚度值</returns>
        public static int GetPerceivedLoyalty(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return 0;

                // 1. 如果是神算 (智力100) 或 自己看自己，直接返回真实值
                if (advisor.Intelligence >= 100 || advisor == target) 
                    return target.Loyalty;

                // 2. 基础误差范围 (智力越低，范围越大)
                int errorRange = 100 - advisor.Intelligence;

                // 3. 【核心优化】使用固定种子，确保同一个军师看同一个人，偏差永远固定
                // 只要 target.Loyalty 不变，这个偏差值就不变
                int seed = advisor.ID * 1000 + target.ID;
                Random stableRandom = new Random(seed);

                // 产生一个固定的偏差值
                int bias = stableRandom.Next(-errorRange, errorRange + 1);

                // 4. 计算显示值
                int perceived = target.Loyalty + bias;
                return Math.Clamp(perceived, 0, 100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetPerceivedLoyalty 失败: {ex.Message}");
                return target?.Loyalty ?? 0;
            }
        }

        /// <summary>
        /// 获取带有性格偏向的忠诚度感知
        /// 在基础感知的基础上，根据军师性格进行修正
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>经过性格修正的忠诚度感知值</returns>
        public static int GetBiasedLoyalty(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return 0;

                int baseValue = GetPerceivedLoyalty(advisor, target); // 上一步的稳定值

                // 【核心优化】根据性格修正
                switch (advisor.CharacterKindID) // 假设 CharacterKindID 对应性格
                {
                    case 0: // 仁德/老好人
                        // 容易把坏人看成好人
                        if (target.Loyalty < 80) 
                        {
                            baseValue += 10;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 仁德性格，对 {target.Name} 忠诚度+10 (原{baseValue-10}→{baseValue})");
                        }
                        break;

                    case 1: // 多疑/冷酷
                        // 容易把好人看成坏人
                        if (target.Loyalty > 90) 
                        {
                            baseValue -= 15;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 多疑性格，对 {target.Name} 忠诚度-15 (原{baseValue+15}→{baseValue})");
                        }
                        break;

                    case 2: // 莽撞/单纯
                        // 极端的两极分化，要么看成很高，要么看成很低
                        if (baseValue > 50) 
                        {
                            baseValue += 20;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 莽撞性格，极端化判断 {target.Name} 忠诚度+20 (原{baseValue-20}→{baseValue})");
                        }
                        else 
                        {
                            baseValue -= 20;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 莽撞性格，极端化判断 {target.Name} 忠诚度-20 (原{baseValue+20}→{baseValue})");
                        }
                        break;

                    case 3: // 狡诈/精明
                        // 狡诈型军师可能有更复杂的判断逻辑
                        // 对于关系好的人可能高估，对于陌生人可能低估
                        if (HasGoodRelationship(advisor, target))
                        {
                            baseValue += 5;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 狡诈性格，对关系好的 {target.Name} 忠诚度+5");
                        }
                        else
                        {
                            baseValue -= 5;
                            System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 狡诈性格，对陌生的 {target.Name} 忠诚度-5");
                        }
                        break;

                    default:
                        // 默认性格不做额外修正
                        System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 默认性格，无额外修正");
                        break;
                }

                return Math.Clamp(baseValue, 0, 100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetBiasedLoyalty 失败: {ex.Message}");
                return GetPerceivedLoyalty(advisor, target);
            }
        }

        /// <summary>
        /// 获取忠诚度的字符串表示
        /// 根据军师智力决定显示精确数值、模糊区间还是文字描述
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>忠诚度的字符串表示</returns>
        public static string GetLoyaltyString(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return "未知";

                if (advisor.Intelligence >= 95)
                {
                    // 高智力军师显示精确数值
                    return target.Loyalty.ToString();
                }
                else if (advisor.Intelligence >= 80)
                {
                    // 中等智力军师显示模糊区间
                    int perceivedLoyalty = GetBiasedLoyalty(advisor, target);
                    int low = Math.Max(0, perceivedLoyalty - 5);
                    int high = Math.Min(100, perceivedLoyalty + 5);
                    return $"{low}~{high}";
                }
                else
                {
                    // 低智力军师显示模糊评价
                    int perceivedLoyalty = GetBiasedLoyalty(advisor, target);
                    
                    if (perceivedLoyalty >= 90) return "誓死效忠";
                    if (perceivedLoyalty >= 70) return "尽忠职守";
                    if (perceivedLoyalty >= 50) return "心存疑虑";
                    return "心怀鬼胎";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetLoyaltyString 失败: {ex.Message}");
                return "未知";
            }
        }

        /// <summary>
        /// 获取详细的忠诚度分析报告
        /// 包含军师的判断过程和可信度评估
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>详细分析报告</returns>
        public static string GetLoyaltyAnalysis(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return "无法分析";

                var analysis = new System.Text.StringBuilder();
                
                analysis.AppendLine($"=== {advisor.Name} 对 {target.Name} 的忠诚度分析 ===");
                analysis.AppendLine();

                // 基础信息
                analysis.AppendLine($"军师智力: {advisor.Intelligence}");
                analysis.AppendLine($"军师性格: {GetCharacterTypeName(advisor.CharacterKindID)}");
                analysis.AppendLine();

                // 感知结果
                int perceivedLoyalty = GetPerceivedLoyalty(advisor, target);
                int biasedLoyalty = GetBiasedLoyalty(advisor, target);
                string loyaltyString = GetLoyaltyString(advisor, target);

                analysis.AppendLine("感知结果:");
                analysis.AppendLine($"  基础感知: {perceivedLoyalty}");
                analysis.AppendLine($"  性格修正后: {biasedLoyalty}");
                analysis.AppendLine($"  显示为: {loyaltyString}");
                analysis.AppendLine();

                // 可信度评估
                string reliability = GetReliabilityAssessment(advisor);
                analysis.AppendLine($"可信度评估: {reliability}");

                // 真实值对比（仅用于调试）
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    analysis.AppendLine();
                    analysis.AppendLine($"[调试] 真实忠诚度: {target.Loyalty}");
                    analysis.AppendLine($"[调试] 感知误差: {Math.Abs(perceivedLoyalty - target.Loyalty)}");
                }

                return analysis.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetLoyaltyAnalysis 失败: {ex.Message}");
                return "分析失败";
            }
        }

        /// <summary>
        /// 检查两个人物是否有良好关系
        /// </summary>
        private static bool HasGoodRelationship(Person advisor, Person target)
        {
            if (advisor == null || target == null) return false;

            // 检查各种关系
            if (advisor.Father == target || target.Father == advisor) return true; // 父子
            if (advisor.Spouse == target || target.Spouse == advisor) return true; // 配偶
            if (advisor.Brothers?.HasGameObject(target) == true) return true; // 兄弟
            
            // 检查亲密关系（如果有相关方法）
            try
            {
                if (advisor.CheckRelation != null && advisor.CheckRelation(target) == 1) return true;
            }
            catch
            {
                // 忽略方法调用错误
            }

            return false;
        }

        /// <summary>
        /// 获取性格类型名称
        /// </summary>
        private static string GetCharacterTypeName(int characterKindID)
        {
            return characterKindID switch
            {
                0 => "仁德型",
                1 => "多疑型", 
                2 => "莽撞型",
                3 => "狡诈型",
                _ => "普通型"
            };
        }

        /// <summary>
        /// 获取可信度评估
        /// </summary>
        private static string GetReliabilityAssessment(Person advisor)
        {
            if (advisor.Intelligence >= 95) return "极高 - 几乎不会出错";
            if (advisor.Intelligence >= 85) return "很高 - 偶有小误差";
            if (advisor.Intelligence >= 75) return "较高 - 基本可信";
            if (advisor.Intelligence >= 65) return "一般 - 需要参考";
            if (advisor.Intelligence >= 50) return "较低 - 仅供参考";
            return "很低 - 不太可信";
        }

        /// <summary>
        /// 检查军师对目标人物的信息是否准确
        /// 基于关系、地位和情报网络判断信息可信度
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>true表示信息准确，false表示可能有误差</returns>
        public static bool IsInfoAccurate(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return false;

                // 1. 如果是亲密武将，必准
                if (HasGoodRelationship(advisor, target)) 
                {
                    System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 与 {target.Name} 关系亲密，信息必准");
                    return true;
                }

                // 2. 如果目标是君主的直系亲属，必准
                if (advisor.BelongedFaction?.Leader != null && 
                    (target.Father == advisor.BelongedFaction.Leader || 
                     target == advisor.BelongedFaction.Leader.Father ||
                     target.Spouse == advisor.BelongedFaction.Leader ||
                     target == advisor.BelongedFaction.Leader.Spouse))
                {
                    System.Diagnostics.Debug.WriteLine($"[军师感知] {target.Name} 是君主直系亲属，信息必准");
                    return true;
                }

                // 3. 检查情报等级 - 由于GetInfoLevel方法不存在，使用替代逻辑
                // 如果目标在己方建筑内，情报准确
                if (target.LocationArchitecture != null && 
                    target.LocationArchitecture.BelongedFaction == advisor.BelongedFaction)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师感知] {target.Name} 在己方建筑内，信息准确");
                    return true;
                }

                // 4. 如果军师智力极高（95+），对同势力人员信息准确
                if (advisor.Intelligence >= 95 && target.BelongedFaction == advisor.BelongedFaction)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 智力极高，对同势力 {target.Name} 信息准确");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 对 {target.Name} 信息可能有误差");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] IsInfoAccurate 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取最终的忠诚度感知值
        /// 结合准确性判断和性格偏向，提供最终的感知结果
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>最终的忠诚度感知值</returns>
        public static int GetFinalPerceivedLoyalty(Person advisor, Person target)
        {
            try
            {
                if (advisor == null || target == null) return 0;

                // 如果信息准确，返回真实值
                if (IsInfoAccurate(advisor, target))
                {
                    System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 对 {target.Name} 信息准确，返回真实忠诚度 {target.Loyalty}");
                    return target.Loyalty;
                }

                // 否则返回带偏向的感知值
                int biasedValue = GetBiasedLoyalty(advisor, target);
                System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 对 {target.Name} 信息有偏差，感知忠诚度 {biasedValue} (真实值 {target.Loyalty})");
                return biasedValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetFinalPerceivedLoyalty 失败: {ex.Message}");
                return target?.Loyalty ?? 0;
            }
        }

        /// <summary>
        /// 批量获取势力内所有人员的忠诚度感知
        /// 用于军师建议和人事管理界面
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="faction">势力</param>
        /// <returns>人员忠诚度感知字典</returns>
        public static System.Collections.Generic.Dictionary<Person, (int perceived, string display)> GetFactionLoyaltyPerception(Person advisor, Faction faction)
        {
            var result = new System.Collections.Generic.Dictionary<Person, (int, string)>();
            
            try
            {
                if (advisor == null || faction?.Persons == null) return result;

                foreach (Person person in faction.Persons.GetList())
                {
                    if (person != null && person.Alive)
                    {
                        int perceived = GetFinalPerceivedLoyalty(advisor, person);
                        string display = GetLoyaltyString(advisor, person);
                        result[person] = (perceived, display);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[军师感知] {advisor.Name} 完成对 {faction.Name} 全员忠诚度感知，共 {result.Count} 人");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师感知] GetFactionLoyaltyPerception 失败: {ex.Message}");
            }

            return result;
        }
    }
}