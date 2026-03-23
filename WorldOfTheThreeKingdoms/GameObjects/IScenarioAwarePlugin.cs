

namespace GameObjects
{

    public interface IScenarioAwarePlugin
    {
        void SetScenario();
        
        /// <summary>
        /// 是否支持延迟初始化（默认false，立即初始化）
        /// </summary>
        bool SupportLazyInit => false;
        
        /// <summary>
        /// 延迟初始化方法（在后台线程执行）
        /// </summary>
        void SetScenarioLazy() => SetScenario();
    }

}
