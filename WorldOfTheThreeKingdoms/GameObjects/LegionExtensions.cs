using System;

namespace GameObjects
{
    /// <summary>
    /// 军团扩展方法
    /// 日期：2026-03-09
    /// 用途：提供便捷方法判断军团类型和任务
    /// </summary>
    public static class LegionExtensions
    {
        /// <summary>
        /// 判断是否为进攻任务
        /// </summary>
        public static bool IsOffensive(this Legion legion)
        {
            return legion.Mission == LegionMission.Attack;
        }
        
        /// <summary>
        /// 判断是否为防守任务
        /// </summary>
        public static bool IsDefensive(this Legion legion)
        {
            return legion.Mission == LegionMission.Defend;
        }
        
        /// <summary>
        /// 判断是否为撤退任务
        /// </summary>
        public static bool IsRetreating(this Legion legion)
        {
            return legion.Mission == LegionMission.Retreat;
        }
        
        /// <summary>
        /// 判断是否为玩家军团
        /// </summary>
        public static bool IsPlayerManual(this Legion legion)
        {
            return legion.Kind == LegionKind.Player;
        }
        
        /// <summary>
        /// 判断是否为AI军团
        /// </summary>
        public static bool IsAI(this Legion legion)
        {
            return legion.Kind == LegionKind.AI;
        }
    }
}
