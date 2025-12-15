using System;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // 内存监控器 - 简化版本
    public class MemoryMonitor
    {
        public MemoryMonitor()
        {
        }

        public void Update()
        {
            // 简化的内存监控逻辑
        }

        /// <summary>
        /// 获取内存报告
        /// </summary>
        public string GetMemoryReport()
        {
            var totalMemory = GC.GetTotalMemory(false);
            return $"内存使用: {totalMemory / 1024 / 1024} MB";
        }
    }
}