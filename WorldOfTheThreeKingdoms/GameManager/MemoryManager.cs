using System;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 内存管理器 - 简化版本
    /// </summary>
    public static class MemoryManager
    {
        /// <summary>
        /// 游戏退出时的内存清理
        /// </summary>
        public static void OnGameExit()
        {
            try
            {
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                System.Diagnostics.Debug.WriteLine("[MemoryManager] 游戏退出内存清理完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MemoryManager] 内存清理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前内存使用情况
        /// </summary>
        public static long GetMemoryUsage()
        {
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// 强制垃圾回收
        /// </summary>
        public static void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}