#nullable disable

using GameObjects;
using GameObjects.PersonDetail;
using System;
using System.Collections.Generic;

namespace GameObjects.PersonDetail
{
    /// <summary>
    /// List&lt;Title&gt; 扩展方法 - 提供序列化支持
    /// 日期：2026-03-17
    /// </summary>
    public static class TitleListExtensions
    {
        /// <summary>
        /// 从字符串加载称号列表
        /// </summary>
        /// <param name="list">目标称号列表</param>
        /// <param name="allTitles">所有可用称号的查找表</param>
        /// <param name="titleIDs">称号ID字符串（空格分隔）</param>
        /// <returns>错误消息列表</returns>
        public static List<string> LoadFromString(this List<Title> list, TitleTable allTitles, string titleIDs)
        {
            List<string> errorMsg = [];

            // 🔥 防止 STJ 反序列化后的 null 导致崩溃
            if (string.IsNullOrEmpty(titleIDs)) return errorMsg;
            
            // 🔥 确保列表不为 null
            if (list == null)
            {
                errorMsg.Add("称号列表为 null");
                return errorMsg;
            }

            // 🔥 关键修复：清空列表，避免重复追加
            // 日期：2026-03-17
            // 原因：如果列表中已有数据（如从旧代码加载），LoadFromString 会追加而不是替换
            //       导致称号重复显示
            list.Clear();

            char[] separator = [' ', '\n', '\r', '\t'];
            string[] strArray = titleIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            
            try
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (int.TryParse(strArray[i], out int titleId))
                    {
                        // 🔥 关键：ID=0 是有效的，必须使用 >= 0
                        if (titleId >= 0 && allTitles.Titles.TryGetValue(titleId, out Title? title))
                        {
                            list.Add(title);
                        }
                        else
                        {
                            errorMsg.Add($"称号ID {titleId} 不存在");
                        }
                    }
                    else
                    {
                        errorMsg.Add($"无效的称号ID格式: {strArray[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg.Add($"称号解析异常: {ex.Message}");
            }

            return errorMsg;
        }
    }
}
