using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.GameObjects
{
    /// <summary>
    /// 军师说服对话管理器 - 简化版本
    /// </summary>
    public static class AdvisorConvinceDialogueManager
    {
        /// <summary>
        /// 根据成功率获取军师对话
        /// </summary>
        public static string GetAdvisorDialogue(int successRate)
        {
            // 简化实现，直接返回对话内容，避免XML解析的复杂性
            if (successRate <= 0)
            {
                return "主公，此人忠心耿耿，绝无可能被说服。建议换个目标或等待时机。";
            }
            else if (successRate <= 33)
            {
                return "主公，此人虽有些许不满，但说服成功的机会微乎其微。";
            }
            else if (successRate <= 66)
            {
                return "主公，此人似有动摇之意，说服有一定的成功几率。";
            }
            else if (successRate <= 99)
            {
                return "主公，此人心有不满，说服成功的概率很高。";
            }
            else
            {
                return "主公，此人早有归顺之心，说服必定成功，万无一失！";
            }
        }

        /// <summary>
        /// 获取带有目标人物信息的完整对话
        /// </summary>
        public static string GetFullDialogue(Person targetPerson, int successRate)
        {
            string baseDialogue = GetAdvisorDialogue(successRate);
            
            if (targetPerson != null)
            {
                return string.Format("关于说服{0}：{1}", targetPerson.Name, baseDialogue);
            }
            
            return baseDialogue;
        }
    }
}