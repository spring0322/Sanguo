using System;

namespace GameObjects
{
    /// <summary>
    /// 军团类型转换器
    /// 日期：2026-03-09
    /// 用途：将旧的4种LegionKind转换为新的Kind+Mission组合
    /// </summary>
    public static class LegionKindConverter
    {
        /// <summary>
        /// 将旧的LegionKind转换为新的Kind
        /// </summary>
        public static LegionKind ToNewKind(LegionKind oldKind)
        {
            return oldKind switch
            {
                LegionKind.Player => LegionKind.Player,
                LegionKind.AI => LegionKind.AI,
                _ => LegionKind.AI  // 默认为AI
            };
        }
        
        /// <summary>
        /// 将旧的LegionKind转换为Mission
        /// </summary>
        public static LegionMission ToMission(LegionKind oldKind)
        {
            // 🔥 临时兼容：在完全迁移前，根据旧枚举值推断任务
            // 这个方法会在所有代码迁移完成后删除
            return oldKind switch
            {
                LegionKind.AI => LegionMission.None,      // AI军团默认无任务
                LegionKind.Player => LegionMission.None,  // 玩家军团默认无任务
                _ => LegionMission.None
            };
        }
        
        /// <summary>
        /// 生成军团名称
        /// </summary>
        public static string GenerateLegionName(LegionKind kind, LegionMission mission, Architecture target)
        {
            string prefix = kind == LegionKind.Player ? "Player_" : "AI_";
            string missionName = mission switch
            {
                LegionMission.Attack => "攻",
                LegionMission.Defend => "守",
                LegionMission.Retreat => "撤",
                LegionMission.Patrol => "巡",
                _ => ""
            };
            
            string targetName = target?.Name ?? "未知";
            return $"{prefix}{missionName}_{targetName}";
        }
    }
}
