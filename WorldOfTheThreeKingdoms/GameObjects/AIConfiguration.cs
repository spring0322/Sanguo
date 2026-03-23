using System;

namespace GameObjects
{
    /// <summary>
    /// 定义人事调配的执行频率
    /// </summary>
    public enum PersonnelInterval
    {
        Monthly,    // 每月 (默认)
        Quarterly,  // 每季度 (1, 4, 7, 10月)
        HalfYearly, // 半年 (1, 7月)
        Yearly      // 每年 (1月)
    }

    /// <summary>
    /// AI系统全局配置
    /// </summary>
    public static class AIConfiguration
    {
        /// <summary>
        /// 全局配置：人事调配频率
        /// </summary>
        public static PersonnelInterval AllocationInterval = PersonnelInterval.Quarterly;

        /// <summary>
        /// 全局开关：是否开启智能脏标记检测（性能换取逻辑严密性）
        /// </summary>
        public static bool EnableSmartTrigger = true;

        /// <summary>
        /// 调试开关：是否输出人事调配相关的调试信息
        /// </summary>
        public static bool EnablePersonnelDebug = false;
    }
}