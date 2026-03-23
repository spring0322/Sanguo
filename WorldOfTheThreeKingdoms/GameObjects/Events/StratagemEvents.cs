#nullable enable
using System;
using GameObjects.TroopDetail;


namespace GameObjects.Events
{
    /// <summary>
    /// 计略事件系统（AOT 兼容的强类型事件）
    /// 用于替代 ExtensionInterface.call 的动态反射调用
    /// </summary>
    public static class StratagemEvents
    {
        /// <summary>
        /// 计略施放完成事件（对目标施放）
        /// 参数：(施法者, 目标, 计略)
        /// </summary>
        public static event Action<Troop, Troop, Stratagem>? OnStratagemCastCompleted;
        
        /// <summary>
        /// 自我计略施放完成事件
        /// 参数：(施法者, 计略)
        /// </summary>
        public static event Action<Troop, Stratagem>? OnSelfStratagemCastCompleted;
        
        /// <summary>
        /// 触发计略施放完成事件
        /// </summary>
        public static void RaiseStratagemCast(Troop caster, Troop target, Stratagem stratagem)
        {
            OnStratagemCastCompleted?.Invoke(caster, target, stratagem);
        }
        
        /// <summary>
        /// 触发自我计略施放完成事件
        /// </summary>
        public static void RaiseSelfStratagemCast(Troop caster, Stratagem stratagem)
        {
            OnSelfStratagemCastCompleted?.Invoke(caster, stratagem);
        }
        
        /// <summary>
        /// 清空所有事件订阅（用于场景切换或重置）
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnStratagemCastCompleted = null;
            OnSelfStratagemCastCompleted = null;
        }
    }
}
