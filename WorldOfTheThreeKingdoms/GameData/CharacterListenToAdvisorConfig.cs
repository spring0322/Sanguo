using System.Collections.Generic;
using GameObjects.PersonDetail;

namespace GameData
{
    /// <summary>
    /// 历史人物纳谏倾向配置
    /// 用于初始化CharacterKind的ListenToAdvisorChance属性
    /// </summary>
    public static class CharacterListenToAdvisorConfig
    {
        /// <summary>
        /// 获取历史人物的纳谏倾向配置
        /// </summary>
        /// <returns>人物名称到纳谏倾向的映射</returns>
        public static Dictionary<string, int> GetHistoricalListenChances()
        {
            return new Dictionary<string, int>
            {
                // === 言听计从型 (90-100%) ===
                { "刘备", 95 },     // 对诸葛亮言听计从，仁君典范
                { "刘璋", 90 },     // 性格软弱，容易被说服
                { "韩馥", 90 },     // 优柔寡断，易受影响
                { "刘表", 85 },     // 性格温和，较听从蒯越等建议
                
                // === 较易听从型 (70-89%) ===
                { "曹操", 75 },     // 早期较听荀彧建议，后期渐独断
                { "孙坚", 80 },     // 武将出身但会听程普等建议
                { "陶谦", 85 },     // 年老体弱，依赖幕僚
                { "公孙瓒", 70 },   // 有一定主见但会考虑建议
                
                // === 有主见型 (50-69%) ===
                { "孙权", 60 },     // 年轻时听从，后期更独立
                { "刘章", 55 },     // 有一定能力，选择性听从
                { "马腾", 65 },     // 西凉军阀，但会听取建议
                { "张鲁", 60 },     // 宗教领袖，有自己想法
                
                // === 较难说服型 (30-49%) ===
                { "吕布", 35 },     // 有勇无谋，但偶尔听陈宫建议
                { "马超", 40 },     // 年轻气盛，不太听劝
                { "张飞", 45 },     // 莽撞但对刘备诸葛亮会听
                { "关羽", 40 },     // 傲气十足，不易说服
                
                // === 固执己见型 (15-29%) ===
                { "袁绍", 20 },     // 刚愎自用，不听田丰沮授
                { "袁术", 25 },     // 骄奢淫逸，听不进劝告
                { "刘禅", 15 },     // 后期昏庸，不听姜维建议
                { "孙皓", 10 },     // 暴君，几乎不听任何建议
                
                // === 极度刚愎型 (5-14%) ===
                { "董卓", 8 },      // 狂妄自大，几乎不听建议
                { "何进", 12 },     // 外戚专权，刚愎自用
                
                // === 特殊情况 ===
                { "司马懿", 70 },   // 深谋远虑，会听取建议但有主见
                { "诸葛亮", 65 },   // 作为君主时会听取建议但很有主见
                { "周瑜", 60 },     // 年轻有为，有主见但会考虑
                { "陆逊", 75 },     // 谦逊好学，较易听从长者建议
                
                // === 女性角色 ===
                { "孙尚香", 50 },   // 有个性但会考虑建议
                { "貂蝉", 40 },     // 有自己的计划和想法
                
                // === 年轻将领 ===
                { "孙策", 55 },     // 年轻有为，有主见
                { "马岱", 70 },     // 较为谨慎，会听建议
                { "姜维", 65 },     // 继承诸葛亮衣钵，但有主见
                
                // === 文官型君主 ===
                { "荀彧", 80 },     // 作为领导者时会听取建议
                { "郭嘉", 60 },     // 有独特见解，选择性听从
                { "贾诩", 50 },     // 老谋深算，有自己判断
                
                // === 默认值 ===
                // 未配置的人物使用默认值50%
            };
        }

        /// <summary>
        /// 根据人物性格类型获取默认纳谏倾向
        /// </summary>
        /// <param name="characterTypeId">性格类型ID</param>
        /// <returns>默认纳谏倾向</returns>
        public static int GetDefaultListenChanceByType(int characterTypeId)
        {
            switch (characterTypeId)
            {
                case 0: // 仁德型
                    return 75; // 仁德君主通常善于纳谏
                    
                case 1: // 霸道型
                    return 55; // 有主见但会考虑建议
                    
                case 2: // 冷静型
                    return 70; // 理性分析，较易听从合理建议
                    
                case 3: // 莽撞型
                    return 35; // 冲动行事，不太听劝
                    
                case 4: // 狡诈型
                    return 45; // 有自己算计，选择性听从
                    
                default:
                    return 50; // 默认值
            }
        }

        /// <summary>
        /// 应用历史人物配置到CharacterKind
        /// </summary>
        /// <param name="characterKind">性格类型对象</param>
        /// <param name="personName">人物名称</param>
        public static void ApplyHistoricalConfig(CharacterKind characterKind, string personName)
        {
            if (characterKind == null || string.IsNullOrEmpty(personName))
                return;

            var historicalConfig = GetHistoricalListenChances();
            
            if (historicalConfig.ContainsKey(personName))
            {
                characterKind.ListenToAdvisorChance = historicalConfig[personName];
                System.Diagnostics.Debug.WriteLine($"[配置] {personName} 纳谏倾向设为 {characterKind.ListenToAdvisorChance}%");
            }
            else
            {
                // 使用基于性格类型的默认值
                characterKind.ListenToAdvisorChance = GetDefaultListenChanceByType(characterKind.ID);
                System.Diagnostics.Debug.WriteLine($"[配置] {personName} 使用默认纳谏倾向 {characterKind.ListenToAdvisorChance}%");
            }
        }

        /// <summary>
        /// 获取纳谏倾向的描述文本
        /// </summary>
        /// <param name="listenChance">纳谏倾向值</param>
        /// <returns>描述文本</returns>
        public static string GetListenChanceDescription(int listenChance)
        {
            if (listenChance >= 90)
                return "言听计从";
            else if (listenChance >= 70)
                return "较易听从";
            else if (listenChance >= 50)
                return "有主见";
            else if (listenChance >= 30)
                return "较难说服";
            else if (listenChance >= 15)
                return "固执己见";
            else
                return "刚愎自用";
        }

        /// <summary>
        /// 获取纳谏倾向的颜色编码
        /// </summary>
        /// <param name="listenChance">纳谏倾向值</param>
        /// <returns>颜色名称</returns>
        public static string GetListenChanceColor(int listenChance)
        {
            if (listenChance >= 80)
                return "Green";      // 绿色：善于纳谏
            else if (listenChance >= 60)
                return "Blue";       // 蓝色：较易听从
            else if (listenChance >= 40)
                return "Yellow";     // 黄色：有主见
            else if (listenChance >= 20)
                return "Orange";     // 橙色：较难说服
            else
                return "Red";        // 红色：刚愎自用
        }

        /// <summary>
        /// 生成配置报告
        /// </summary>
        /// <returns>配置报告字符串</returns>
        public static string GenerateConfigReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 历史人物纳谏倾向配置报告 ===");
            report.AppendLine();

            var config = GetHistoricalListenChances();
            var categories = new Dictionary<string, List<(string name, int chance)>>
            {
                { "言听计从型 (90-100%)", new List<(string, int)>() },
                { "较易听从型 (70-89%)", new List<(string, int)>() },
                { "有主见型 (50-69%)", new List<(string, int)>() },
                { "较难说服型 (30-49%)", new List<(string, int)>() },
                { "固执己见型 (15-29%)", new List<(string, int)>() },
                { "刚愎自用型 (5-14%)", new List<(string, int)>() }
            };

            // 分类统计
            foreach (var kvp in config)
            {
                if (kvp.Value >= 90)
                    categories["言听计从型 (90-100%)"].Add((kvp.Key, kvp.Value));
                else if (kvp.Value >= 70)
                    categories["较易听从型 (70-89%)"].Add((kvp.Key, kvp.Value));
                else if (kvp.Value >= 50)
                    categories["有主见型 (50-69%)"].Add((kvp.Key, kvp.Value));
                else if (kvp.Value >= 30)
                    categories["较难说服型 (30-49%)"].Add((kvp.Key, kvp.Value));
                else if (kvp.Value >= 15)
                    categories["固执己见型 (15-29%)"].Add((kvp.Key, kvp.Value));
                else
                    categories["刚愎自用型 (5-14%)"].Add((kvp.Key, kvp.Value));
            }

            // 生成报告
            foreach (var category in categories)
            {
                if (category.Value.Count > 0)
                {
                    report.AppendLine($"=== {category.Key} ===");
                    foreach (var person in category.Value)
                    {
                        report.AppendLine($"  {person.name}: {person.chance}%");
                    }
                    report.AppendLine();
                }
            }

            report.AppendLine($"总计配置人物: {config.Count} 人");
            report.AppendLine();
            report.AppendLine("配置说明:");
            report.AppendLine("- 基于历史记录和人物性格特点");
            report.AppendLine("- 体现明君善纳谏、昏君刚愎自用的差异");
            report.AppendLine("- 为AI决策系统提供个性化基础");
            report.AppendLine("- 增强游戏的历史真实感和策略深度");

            return report.ToString();
        }
    }
}