using System;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI命令类型枚举
    /// </summary>
    public enum AICommandType
    {
        CreateTroop,    // 组建部队
        RecallPerson,   // 召回/停止工作
        TroopAction,    // 战斗/移动
        SpendResource,  // 花费资金
        DiplomaticAction // 外交行动
    }

    /// <summary>
    /// AI命令结构 - 用于安全执行AI操作
    /// </summary>
    public class AICommand
    {
        public AICommandType Type { get; set; }
        public object Target { get; set; }     // 操作对象
        public object Parameter { get; set; }  // 参数
        public string DebugInfo { get; set; }  // 来源说明

        public AICommand(AICommandType type, object target, object parameter, string debugInfo = "")
        {
            Type = type;
            Target = target;
            Parameter = parameter;
            DebugInfo = debugInfo;
        }

        public override string ToString()
        {
            return $"[AI命令] {Type} - {DebugInfo}";
        }
    }
}