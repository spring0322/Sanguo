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
            // 🔥 Anti-Band-Aid: 不做空检查，让调用方保证数据有效性
            // 如果 data 为 null，应该崩溃暴露问题
            System.Diagnostics.Debug.WriteLine($"[EventManager] 🔥 触发军师事件: {data.Title}");
            System.Diagnostics.Debug.WriteLine($"[EventManager] 📊 当前订阅者数量: {OnStrategistInterrupt?.GetInvocationList().Length ?? 0}");
            
            OnStrategistInterrupt?.Invoke(data);
        }
        
        /// <summary>
        /// 测试方法：手动触发一个测试事件
        /// </summary>
        public static void TestTriggerEvent()
        {
            // 🔥 C# 12: 使用目标类型 new()
            AdviceData testData = new()
            {
                Level = RiskLevel.Critical,
                Title = "【测试事件】",
                Content = "这是一个测试军师建议，用于验证事件系统是否正常工作。",
                ButtonText = "知道了",
                Type = AdviceType.Emergency
            };
            
            System.Diagnostics.Debug.WriteLine("[EventManager] 🧪 触发测试事件...");
            TriggerStrategistEvent(testData);
        }
    }
}