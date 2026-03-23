using System;

namespace GameObjects
{
    /// <summary>
    /// 军团任务类型
    /// 日期：2026-03-09
    /// 说明：将军团类型和任务分离，同一军团可以执行不同任务
    /// </summary>
    public enum LegionMission
    {
        /// <summary>
        /// 无任务（玩家军团默认状态）
        /// </summary>
        None,
        
        /// <summary>
        /// 攻击任务（进攻敌方城池）
        /// </summary>
        Attack,
        
        /// <summary>
        /// 防守任务（防守己方城池）
        /// </summary>
        Defend,
        
        /// <summary>
        /// 撤退任务（撤回己方城池）
        /// </summary>
        Retreat,
        
        /// <summary>
        /// 巡逻任务（巡逻指定区域）
        /// </summary>
        Patrol
    }
}
