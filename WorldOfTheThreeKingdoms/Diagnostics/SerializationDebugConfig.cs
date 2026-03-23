using System;

namespace WorldOfTheThreeKingdoms.Diagnostics
{
    /// <summary>
    /// 序列化调试配置
    /// 用于控制调试输出的详细程度，避免性能问题
    /// </summary>
    public static class SerializationDebugConfig
    {
        /// <summary>
        /// 是否启用详细的GameArea创建日志
        /// 🔥 默认关闭，避免刷屏
        /// </summary>
        public static bool EnableGameAreaCreationLog = false;
        
        /// <summary>
        /// 是否启用详细的Area赋值日志
        /// 🔥 默认关闭，只在检测到异常时输出
        /// </summary>
        public static bool EnableAreaAssignmentLog = false;
        
        /// <summary>
        /// 是否启用详细的LoadFromString日志
        /// 🔥 默认关闭，只在检测到异常时输出
        /// </summary>
        public static bool EnableLoadFromStringLog = false;
        
        /// <summary>
        /// 是否启用异常检测和警告
        /// 🔥 默认开启，用于检测问题
        /// </summary>
        public static bool EnableAnomalyDetection = true;
        
        /// <summary>
        /// 是否启用GameObjectListConverter的详细日志
        /// 🔥 默认关闭，只在出现异常时输出
        /// </summary>
        public static bool EnableGameObjectListConverterLog = false;

        /// <summary>
        /// 是否在兵力减少异常时打印调用堆栈
        /// </summary>
        public static bool EnableMilitaryQuantityStackTrace = false;

        /// <summary>
        /// 是否在部队销毁时打印调用堆栈
        /// </summary>
        public static bool EnableTroopDestroyStackTrace = false;

        /// <summary>
        /// 是否在部队操作状态改变时打印调用堆栈
        /// </summary>
        public static bool EnableTroopOperatedStackTrace = false;

        /// <summary>
        /// 是否在部队目标建筑改变时打印调用堆栈
        /// </summary>
        public static bool EnableTroopWillArchStackTrace = false;
        
        /// <summary>
        /// 启用所有调试日志（用于深度调试）
        /// 注意：这会产生大量日志，影响性能
        /// </summary>
        public static void EnableAllLogs()
        {
            EnableGameAreaCreationLog = true;
            EnableAreaAssignmentLog = true;
            EnableLoadFromStringLog = true;
            EnableAnomalyDetection = true;
            EnableGameObjectListConverterLog = true;
            
            System.Diagnostics.Debug.WriteLine("[调试配置] 已启用所有序列化调试日志");
        }
        
        /// <summary>
        /// 禁用所有调试日志（用于生产环境）
        /// </summary>
        public static void DisableAllLogs()
        {
            EnableGameAreaCreationLog = false;
            EnableAreaAssignmentLog = false;
            EnableLoadFromStringLog = false;
            EnableAnomalyDetection = false;
            EnableGameObjectListConverterLog = false;
            
            System.Diagnostics.Debug.WriteLine("[调试配置] 已禁用所有序列化调试日志");
        }
        
        /// <summary>
        /// 只启用异常检测（推荐的默认设置）
        /// </summary>
        public static void EnableOnlyAnomalyDetection()
        {
            EnableGameAreaCreationLog = false;
            EnableAreaAssignmentLog = false;
            EnableLoadFromStringLog = false;
            EnableAnomalyDetection = true;
            EnableGameObjectListConverterLog = false;
            
            System.Diagnostics.Debug.WriteLine("[调试配置] 只启用异常检测，禁用详细日志");
        }
        
        /// <summary>
        /// 输出当前配置状态
        /// </summary>
        public static void ShowCurrentConfig()
        {
            System.Diagnostics.Debug.WriteLine("[调试配置] 当前序列化调试配置:");
            System.Diagnostics.Debug.WriteLine($"[调试配置] GameArea创建日志: {EnableGameAreaCreationLog}");
            System.Diagnostics.Debug.WriteLine($"[调试配置] Area赋值日志: {EnableAreaAssignmentLog}");
            System.Diagnostics.Debug.WriteLine($"[调试配置] LoadFromString日志: {EnableLoadFromStringLog}");
            System.Diagnostics.Debug.WriteLine($"[调试配置] 异常检测: {EnableAnomalyDetection}");
            System.Diagnostics.Debug.WriteLine($"[调试配置] GameObjectListConverter日志: {EnableGameObjectListConverterLog}");
        }
    }
}