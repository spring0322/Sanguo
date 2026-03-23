using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 【已废弃】旧的单位移动管理器
    /// 移动逻辑已完全迁移至 Troop.Movement.cs 的 [NewMovementSystem]
    /// 保留此类仅为了兼容旧代码引用，实际不再执行逻辑
    /// </summary>
    public class UnitMovement
    {
        public enum MovementState
        {
            Idle,
            Moving,
            Blocked,
            Arrived
        }

        // 保留字段定义防止报错
        public MovementState State { get; set; } = MovementState.Idle;
    }

    public class UnitMovementManager
    {
        private static UnitMovementManager _instance;
        public static UnitMovementManager Instance => _instance ??= new UnitMovementManager();

        private Dictionary<int, UnitMovement> _unitMovements = new Dictionary<int, UnitMovement>();

        public void Update(GameTime gameTime)
        {
            // [NewMovementSystem] 接管
            // 这里不再驱动任何逻辑，防止冲突
        }

        public void RegisterUnit(Troop troop)
        {
            // 空操作
        }

        public void UnregisterUnit(Troop troop)
        {
            // 空操作
        }
        
        // 保留统计接口，但返回提示信息
        public string GetStats()
        {
            return "UnitMovement system is deprecated. Please check Troop.Movement.cs for status.";
        }
    }
}