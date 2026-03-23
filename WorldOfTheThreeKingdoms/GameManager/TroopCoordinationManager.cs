using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 【兼容模式】部队协调管理器
    /// 注意：由于引入了基于异步寻路和软碰撞的新移动系统，原有的中央协调逻辑已废弃。
    /// 此类现在仅保留空壳，以防止其他代码引用报错，实际不再干预移动。
    /// </summary>
    public class TroopCoordinationManager
    {
        private static TroopCoordinationManager _instance;
        public static TroopCoordinationManager Instance => _instance ??= new TroopCoordinationManager();

        // 既然不再做协调，这些复杂的字典和列表都可以清理掉，节省内存
        // private Dictionary<Point, RegionCoordination> _regionCoordinations... (已移除)

        /// <summary>
        /// 每帧调用更新 (现在是空操作)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // [新系统接管] 
            // 不需要再做死锁检测或优先级排序。
            // Troop.cs 内部的 _stuckCounter 和 RequestPathAsync 会自动处理卡死问题。
        }

        /// <summary>
        /// 请求移动许可
        /// </summary>
        /// <returns>始终返回 true，将决策权完全交还给 Troop.cs</returns>
        public bool RequestMove(Troop troop, Point nextPos)
        {
            // 永远允许。
            // 因为 Troop.cs 在 ExecutePathMove 里已经自己判断了 Session.Current.Scenario.GetTroopByPosition(nextPos)
            // 如果前方有人，Troop 自己会选择 Wait 或 Re-path，不需要 Manager 干预。
            return true;
        }

        /// <summary>
        /// 注册移动请求 (空操作)
        /// </summary>
        public void RegisterMoveRequest(Troop troop, Point nextPos)
        {
            // 不需要记录，新系统是即时判定的。
        }

        /// <summary>
        /// 报告移动完成 (空操作)
        /// </summary>
        public void ReportMoveFinished(Troop troop)
        {
            // 不需要处理
        }

        /// <summary>
        /// 紧急解卡 (保留但留空，或者仅做日志)
        /// </summary>
        public void TrySolveDeadlock(Troop troop)
        {
            // 移除 SIMD 代码以避免兼容性问题，完全使用标量降级
            // if (Avx2.IsSupported) ...
            // Troop.cs 内部现在有 _stuckCounter > MAX_STUCK_TOLERANCE 的逻辑
            // 那才是真正的解卡机制（重寻路）。
            // 这里留空即可。
            // System.Diagnostics.Debug.WriteLine($"[Manager] Ignored deadlock solve request for {troop.DisplayName}, handled internally.");
        }
        
        // 如果你的代码中还有其他 public 方法被外部调用，
        // 请保留方法签名，但清空方法体。
        public void UpdateCoordination() {}
        public bool ShouldTroopWait(Troop t) => false;
        public void ResolveCircularDeadlock(Troop t) {}
        public void ResolveCircularDeadlock(List<Troop> troops) {}
        public void ResetTroopWaitState(int troopId) {}
        public Point? GetAlternativePath(Troop t) => null;
        public void ForceUnstuckTroop(Troop t) {}
        public List<Troop> FindCircularDeadlockGroup(Troop t, List<Troop> allTroops) => new List<Troop>();
        
    }
}