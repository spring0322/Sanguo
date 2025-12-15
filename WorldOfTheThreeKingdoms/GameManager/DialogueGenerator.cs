using System;
using GameObjects;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 对话生成器 - 为游戏事件生成动态对话
    /// </summary>
    public class DialogueGenerator
    {
        /// <summary>
        /// 获取任命军师的对话
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">新军师</param>
        /// <returns>DialogueInfo对象，包含谁说的和说什么</returns>
        public static DialogueInfo GetAppointAdvisorDialogue(Person leader, Person advisor)
        {
            DialogueInfo dialog = new DialogueInfo();

            try
            {
                // 从XML配置加载对话
                var config = DialogueConfigManager.LoadAppointmentDialogues();
                var entry = DialogueConfigManager.FindBestMatch(config, leader, advisor);

                if (entry != null)
                {
                    dialog.LeaderText = entry.LeaderText?.Replace("{advisor}", advisor.Name) ?? "";
                    dialog.AdvisorText = entry.AdvisorText?.Replace("{leader}", leader.Name) ?? "";
                    return dialog;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueGenerator] XML配置加载失败，使用默认对话: {ex.Message}");
            }

            // 回退到硬编码对话（保持向后兼容）
            return GetAppointAdvisorDialogueFallback(leader, advisor);
        }

        /// <summary>
        /// 获取罢免军师的对话
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">被罢免的军师</param>
        /// <returns>DialogueInfo对象</returns>
        public static DialogueInfo GetRecallAdvisorDialogue(Person leader, Person advisor)
        {
            DialogueInfo dialog = new DialogueInfo();

            try
            {
                // 从XML配置加载对话
                var config = DialogueConfigManager.LoadRecallDialogues();
                var entry = DialogueConfigManager.FindBestMatch(config, leader, advisor);

                if (entry != null)
                {
                    dialog.LeaderText = entry.LeaderText?.Replace("{advisor}", advisor.Name) ?? "";
                    dialog.AdvisorText = entry.AdvisorText?.Replace("{leader}", leader.Name) ?? "";
                    return dialog;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueGenerator] XML配置加载失败，使用默认对话: {ex.Message}");
            }

            // 回退到硬编码对话（保持向后兼容）
            return GetRecallAdvisorDialogueFallback(leader, advisor);
        }

        /// <summary>
        /// 回退的任命军师对话生成（硬编码版本）
        /// </summary>
        private static DialogueInfo GetAppointAdvisorDialogueFallback(Person leader, Person advisor)
        {
            DialogueInfo dialog = new DialogueInfo();

            // 1. 优先检查【专属羁绊】 (Tier 0)
            if (leader.ID == 100 && advisor.ID == 200) // 假设刘备ID 100, 孔明 200
            {
                dialog.LeaderText = "先生之于备，犹鱼之有水也！";
                dialog.AdvisorText = "亮感主公三顾之恩，敢不鞠躬尽瘁？";
                return dialog;
            }

            // 检查其他经典羁绊
            if (leader.ID == 101 && advisor.ID == 201) // 假设曹操和郭嘉
            {
                dialog.LeaderText = $"奉孝之智，孤之子房也！";
                dialog.AdvisorText = "主公雄才大略，嘉愿为主公筹谋天下。";
                return dialog;
            }

            if (leader.ID == 102 && advisor.ID == 202) // 假设孙权和周瑜
            {
                dialog.LeaderText = $"公瑾文武双全，正是江东之栋梁！";
                dialog.AdvisorText = "主公信任，瑜必不负所托，共创江东霸业。";
                return dialog;
            }

            // 2. 检查君主性格生成【君主台词】 (Tier 1)
            switch (leader.CharacterKindID) // 假设你有这个字段
            {
                case 0: // 仁德
                    dialog.LeaderText = $"备才疏学浅，欲请{advisor.Name}先生教我。望念苍生之苦，收下此印。";
                    break;
                case 1: // 霸道
                    dialog.LeaderText = $"孤欲平定四海，{advisor.Name}，你可愿助孤一臂之力？";
                    break;
                case 2: // 冷静
                    dialog.LeaderText = $"{advisor.Name}，以你之才，担任军师一职最为合适。";
                    break;
                case 3: // 莽撞
                    dialog.LeaderText = $"{advisor.Name}！这就封你做军师，给俺想个好计策出来！";
                    break;
                case 4: // 狡诈
                    dialog.LeaderText = $"哈哈，{advisor.Name}，你我联手，天下何愁不定？";
                    break;
                default:
                    dialog.LeaderText = $"今欲请{advisor.Name}担任军师一职，不知尊意如何？";
                    break;
            }

            // 3. 检查军师属性生成【军师台词】 (Tier 2)
            if (advisor.Intelligence >= 95)
            {
                dialog.AdvisorText = "主公既有此意，某敢不效死力？天下大势，尽在掌握中。";
            }
            else if (advisor.Intelligence >= 85)
            {
                dialog.AdvisorText = "承蒙主公厚爱，属下定当殚精竭虑，为主公分忧。";
            }
            else if (advisor.Intelligence >= 75)
            {
                dialog.AdvisorText = "承蒙主公错爱，属下定当竭尽所能。";
            }
            else if (advisor.Intelligence >= 60)
            {
                dialog.AdvisorText = "属下才疏学浅，恐难胜任，但既蒙主公信任，定当尽力而为。";
            }
            else
            {
                dialog.AdvisorText = "啊？军...军师？主公，俺是个粗人，这...这怎么使得？";
            }

            // 4. 根据军师性格调整回应
            if (advisor.CharacterKindID == 4) // 狡诈
            {
                dialog.AdvisorText = "嘿嘿，主公慧眼识人，某必为主公出谋划策。";
            }
            else if (advisor.CharacterKindID == 2) // 冷静
            {
                dialog.AdvisorText = "属下接受任命，必当以理智辅佐主公。";
            }
            else if (advisor.CharacterKindID == 0) // 仁德
            {
                dialog.AdvisorText = "既蒙主公信任，属下当以仁义之心辅佐主公。";
            }

            return dialog;
        }

        /// <summary>
        /// 回退的罢免军师对话生成（硬编码版本）
        /// </summary>
        private static DialogueInfo GetRecallAdvisorDialogueFallback(Person leader, Person advisor)
        {
            DialogueInfo dialog = new DialogueInfo();

            // 根据君主性格生成台词
            switch (leader.CharacterKindID)
            {
                case 0: // 仁德
                    dialog.LeaderText = $"{advisor.Name}，你辛苦了。现在让你回去休息吧。";
                    break;
                case 1: // 霸道
                    dialog.LeaderText = $"{advisor.Name}，孤另有安排，军师一职就此卸任。";
                    break;
                case 3: // 莽撞
                    dialog.LeaderText = $"{advisor.Name}，俺要换个军师，你先歇着吧！";
                    break;
                default:
                    dialog.LeaderText = $"{advisor.Name}，军师之职暂且卸任，望你理解。";
                    break;
            }

            // 根据军师性格生成回应
            if (advisor.CharacterKindID == 1) // 霸道
            {
                dialog.AdvisorText = "哼！既然主公如此决定，某告退！";
            }
            else if (advisor.CharacterKindID == 0) // 仁德
            {
                dialog.AdvisorText = "属下明白，一切听从主公安排。";
            }
            else
            {
                dialog.AdvisorText = "是，属下遵命。";
            }

            return dialog;
        }
    }

    /// <summary>
    /// 用于存储对话结果的简单类
    /// </summary>
    public class DialogueInfo
    {
        public string LeaderText { get; set; }
        public string AdvisorText { get; set; }

        public DialogueInfo()
        {
            LeaderText = "";
            AdvisorText = "";
        }
    }
}