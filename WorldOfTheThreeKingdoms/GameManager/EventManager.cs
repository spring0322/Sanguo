// 📢 事件管理器 - 简化版军师事件系统
// 负责军师建议的事件触发和分发

using System;

namespace GameManager
{
    /// <summary>
    /// 📢 事件管理器 - 简化版军师事件系统
    /// </summary>
    public static class EventManager
    {
        // 定义一个事件：当军师有紧急情况时触发
        // 订阅者会收到一个 AdviceData 数据包
        public static event Action<AdviceData> OnStrategistInterrupt;
        
        // 触发事件的方法
        public static void TriggerStrategistEvent(AdviceData data)
        {
            OnStrategistInterrupt?.Invoke(data);
        }
    }
}