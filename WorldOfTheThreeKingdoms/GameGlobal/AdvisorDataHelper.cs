using System;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 军师数据辅助类 - 通用的观测值计算系统
    /// 提供更简洁和通用的军师感知机制，支持多种属性观测
    /// </summary>
    public static class AdvisorDataHelper
    {
        /// <summary>
        /// 获取观测值（核心补丁）
        /// </summary>
        /// <param name="advisor">负责观测的军师</param>
        /// <param name="target">被观测的目标武将</param>
        /// <param name="realValue">真实数值 (如忠诚度、野心等)</param>
        /// <param name="propertyName">属性名 (用于生成不同的随机种子，防止所有属性偏差一致)</param>
        /// <returns>修正后的显示数值</returns>
        public static int GetObservedValue(Person advisor, Person target, int realValue, string propertyName)
        {
            try
            {
                // ---------------------------------------------------------
                // 1. 【绝对看穿】补丁：智力 >= 100 或 自己看自己 -> 必准
                // ---------------------------------------------------------
                if (advisor == null || advisor.Intelligence >= 100 || advisor == target)
                {
                    System.Diagnostics.Debug.WriteLine($"[观测系统] {advisor?.Name ?? "空军师"} 观测 {target?.Name ?? "空目标"} 的 {propertyName}: 绝对准确 = {realValue}");
                    return realValue;
                }

                // ---------------------------------------------------------
                // 2. 设定修正范围 (Blur Range)
                // ---------------------------------------------------------
                // 智力越低，误差越大
                // 智力 90: 误差 ±10%
                // 智力 70: 误差 ±30%
                // 智力 50: 误差 ±50%
                int intGap = 100 - advisor.Intelligence;
                
                // 限制最大误差范围，防止溢出太离谱 (比如智力10的人看到误差90)
                // 这里设定最大误差为 40 (即智力60以下误差就不再增加了，或者你可以按需调整)
                int maxError = Math.Clamp(intGap, 0, 40);

                // ---------------------------------------------------------
                // 3. 计算固定偏差 (Consistent Bias)
                // ---------------------------------------------------------
                // 使用 (军师ID + 目标ID + 属性名) 作为种子
                // 这样同一个军师看同一个人的同一个属性，每次算出来的偏差都是一样的
                int seed = advisor.ID * 10000 + target.ID * 100 + propertyName.GetHashCode();
                
                // 使用 System.Random 并不是为了真随机，而是为了取由种子决定的伪随机数
                Random stableRandom = new Random(seed);
                
                // 产生一个 -maxError 到 +maxError 之间的偏差
                int bias = stableRandom.Next(-maxError, maxError + 1);

                // ---------------------------------------------------------
                // 4. 性格修正 (可选补丁)
                // ---------------------------------------------------------
                // 如果军师性格多疑(假设Kind=1)，倾向于把数值往低了报
                switch (advisor.CharacterKindID)
                {
                    case 1: // 多疑型
                        bias -= 5;
                        System.Diagnostics.Debug.WriteLine($"[观测系统] {advisor.Name} 多疑性格修正: -5");
                        break;
                    case 0: // 仁德型
                        if (realValue < 60) // 对低数值容易高估
                        {
                            bias += 8;
                            System.Diagnostics.Debug.WriteLine($"[观测系统] {advisor.Name} 仁德性格修正: +8 (低值高估)");
                        }
                        break;
                    case 2: // 莽撞型
                        // 极端化倾向
                        if (bias > 0) bias += 5;
                        else if (bias < 0) bias -= 5;
                        System.Diagnostics.Debug.WriteLine($"[观测系统] {advisor.Name} 莽撞性格修正: 极端化 {(bias > 0 ? "+5" : "-5")}");
                        break;
                }

                // 5. 计算最终结果并截断
                int perceivedValue = realValue + bias;
                int finalValue = Math.Clamp(perceivedValue, 0, 100); // 限制在 0-100 之间

                System.Diagnostics.Debug.WriteLine($"[观测系统] {advisor.Name}(智力{advisor.Intelligence}) 观测 {target.Name} 的 {propertyName}: 真实值{realValue} → 偏差{bias} → 感知值{finalValue}");
                
                return finalValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[观测系统] GetObservedValue 失败: {ex.Message}");
                return realValue; // 异常时返回真实值
            }
        }

        /// <summary>
        /// 获取忠诚度显示文本 (UI专用)
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>忠诚度的显示文本</returns>
        public static string GetLoyaltyString(Person advisor, Person target)
        {
            try
            {
                if (advisor == null) return "???";
                if (target == null) return "未知";

                // 获取观测值
                int viewValue = GetObservedValue(advisor, target, target.Loyalty, "Loyalty");

                // 1. 智力100：显示精确值，且标金/变色提示"绝对精准"
                if (advisor.Intelligence >= 100)
                {
                    System.Diagnostics.Debug.WriteLine($"[显示系统] {advisor.Name} 神算级别，精确显示: {viewValue}");
                    return viewValue.ToString(); // UI层可以用金色字体渲染
                }

                // 2. 智力80-99：显示数值，但其实有误差
                if (advisor.Intelligence >= 80)
                {
                    System.Diagnostics.Debug.WriteLine($"[显示系统] {advisor.Name} 高智力，数值显示: {viewValue}");
                    return viewValue.ToString();
                }

                // 3. 智力60-79：显示模糊区间 (增加不确定感)
                if (advisor.Intelligence >= 60)
                {
                    int range = 5; // 上下浮动5
                    int low = Math.Max(0, viewValue - range);
                    int high = Math.Min(100, viewValue + range);
                    string rangeText = $"{low}~{high}";
                    System.Diagnostics.Debug.WriteLine($"[显示系统] {advisor.Name} 中等智力，区间显示: {rangeText}");
                    return rangeText;
                }

                // 4. 智力<60：完全瞎猜的文字描述
                string description;
                if (viewValue >= 90) description = "似乎很忠诚";
                else if (viewValue >= 60) description = "难以捉摸";
                else description = "心怀鬼胎";

                System.Diagnostics.Debug.WriteLine($"[显示系统] {advisor.Name} 低智力，文字描述: {description} (基于感知值{viewValue})");
                return description;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[显示系统] GetLoyaltyString 失败: {ex.Message}");
                return "未知";
            }
        }

        /// <summary>
        /// 获取野心度观测值
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <returns>观测到的野心度</returns>
        public static int GetObservedAmbition(Person advisor, Person target)
        {
            if (target == null) return 0;
            
            // 假设野心度基于某些属性计算，这里简化处理
            int realAmbition = Math.Max(0, target.Ambition - target.PersonalLoyalty * 10);
            return GetObservedValue(advisor, target, realAmbition, "Ambition");
        }

        /// <summary>
        /// 获取能力观测值（通用方法）
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="target">目标人物</param>
        /// <param name="abilityType">能力类型</param>
        /// <returns>观测到的能力值</returns>
        public static int GetObservedAbility(Person advisor, Person target, string abilityType)
        {
            if (target == null) return 0;

            int realValue = abilityType.ToLower() switch
            {
                "intelligence" => target.Intelligence,
                "command" => target.Command,
                "strength" => target.Strength,
                "politics" => target.Politics,
                "glamour" => target.Glamour,
                _ => 0
            };

            return GetObservedValue(advisor, target, realValue, abilityType);
        }

        /// <summary>
        /// 获取观测准确度评估
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <returns>准确度描述</returns>
        public static string GetAccuracyAssessment(Person advisor)
        {
            if (advisor == null) return "无法评估";

            return advisor.Intelligence switch
            {
                >= 100 => "神算无双 - 洞察一切",
                >= 90 => "极其精准 - 偶有小误差",
                >= 80 => "相当可靠 - 基本准确",
                >= 70 => "尚可信赖 - 有一定误差",
                >= 60 => "仅供参考 - 误差较大",
                _ => "不太可信 - 经常出错"
            };
        }

        /// <summary>
        /// 批量获取势力人员观测数据
        /// </summary>
        /// <param name="advisor">军师</param>
        /// <param name="faction">势力</param>
        /// <returns>人员观测数据字典</returns>
        public static System.Collections.Generic.Dictionary<Person, PersonObservationData> GetFactionObservationData(Person advisor, Faction faction)
        {
            var result = new System.Collections.Generic.Dictionary<Person, PersonObservationData>();

            try
            {
                if (advisor == null || faction?.Persons == null) return result;

                foreach (Person person in faction.Persons.GetList())
                {
                    if (person != null && person.Alive)
                    {
                        var data = new PersonObservationData
                        {
                            ObservedLoyalty = GetObservedValue(advisor, person, person.Loyalty, "Loyalty"),
                            LoyaltyDisplay = GetLoyaltyString(advisor, person),
                            ObservedIntelligence = GetObservedAbility(advisor, person, "Intelligence"),
                            ObservedCommand = GetObservedAbility(advisor, person, "Command"),
                            ObservedAmbition = GetObservedAmbition(advisor, person)
                        };

                        result[person] = data;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[批量观测] {advisor.Name} 完成对 {faction.Name} 全员观测，共 {result.Count} 人");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[批量观测] GetFactionObservationData 失败: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// 人员观测数据结构
    /// </summary>
    public class PersonObservationData
    {
        public int ObservedLoyalty { get; set; }
        public string LoyaltyDisplay { get; set; }
        public int ObservedIntelligence { get; set; }
        public int ObservedCommand { get; set; }
        public int ObservedAmbition { get; set; }
    }
}