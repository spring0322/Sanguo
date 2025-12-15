using System;
using System.Collections.Generic;

namespace GameManager
{
    /// <summary>
    /// List扩展 - 为常用集合类型提供重置功能
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// 重置List到可重用状态
        /// </summary>
        public static void ResetForReuse<T>(this List<T> list)
        {
            if (list != null)
            {
                list.Clear();
                // 如果容量过大，适当收缩以节省内存
                if (list.Capacity > 1000)
                {
                    list.Capacity = 500; // 收缩到合理大小
                }
            }
        }
    }


}