using System;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 性能设置管理类
    /// </summary>
    public class PerformanceSettings
    {
        private static PerformanceSettings _current = new PerformanceSettings();
        
        /// <summary>
        /// 当前性能设置实例
        /// </summary>
        public static PerformanceSettings Current => _current;
        
        /// <summary>
        /// 性能模式枚举
        /// </summary>
        public enum PerformanceMode
        {
            Low,        // 低性能模式
            Balanced,   // 平衡模式
            High        // 高性能模式
        }
        
        private PerformanceMode _mode = PerformanceMode.Balanced;
        
        /// <summary>
        /// 当前性能模式
        /// </summary>
        public PerformanceMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnSettingsChanged?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// 设置变更事件
        /// </summary>
        public event Action OnSettingsChanged;
        
        /// <summary>
        /// AI逻辑切片数量
        /// </summary>
        public int AiLogicSliceCount
        {
            get
            {
                return Mode switch
                {
                    PerformanceMode.Low => 5,
                    PerformanceMode.Balanced => 10,
                    PerformanceMode.High => 20,
                    _ => 10
                };
            }
        }
        
        /// <summary>
        /// 最大可见部队数量
        /// </summary>
        public int MaxVisibleTroops
        {
            get
            {
                return Mode switch
                {
                    PerformanceMode.Low => 50,
                    PerformanceMode.Balanced => 100,
                    PerformanceMode.High => 200,
                    _ => 100
                };
            }
        }
        
        /// <summary>
        /// 设置为低性能模式
        /// </summary>
        public void SetLowPerformanceMode()
        {
            Mode = PerformanceMode.Low;
        }
        
        /// <summary>
        /// 设置为平衡模式
        /// </summary>
        public void SetBalancedMode()
        {
            Mode = PerformanceMode.Balanced;
        }
        
        /// <summary>
        /// 设置为高质量模式
        /// </summary>
        public void SetHighQualityMode()
        {
            Mode = PerformanceMode.High;
        }
    }
}