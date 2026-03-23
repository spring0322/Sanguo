using System;

namespace GameObjects
{
    /// <summary>
    /// 军团类型（重构版）
    /// 日期：2026-03-09
    /// 说明：简化为2种类型，任务由LegionMission枚举表示
    /// </summary>
    public enum LegionKind
    {
        AI,      // AI控制的军团
        Player   // 玩家控制的军团
    }
}

