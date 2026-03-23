using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GameObjects.AI.Pathfinding
{
    /// <summary>
    /// 异步寻路结果
    /// </summary>
    public readonly record struct AsyncPathResult(
        Guid TroopId,                    // 部队唯一标识
        ulong PathfindingVersion,        // 寻路版本号（防止时序错乱）
        List<Point> Path,                // 路径（从对象池租借，失败则为 null）
        bool IsSuccess,                  // 是否成功
        bool IsCancelled                 // 是否被取消
    );
}
