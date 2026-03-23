using WorldOfTheThreeKingdoms.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameData;

/// <summary>
/// 势力范围配置管理器（支持热重载）
/// 日期：2026-03-21
/// 用途：为 InfluenceConfig 提供统一的热重载机制
/// </summary>
public static class InfluenceConfigManager
{
    // 内部管理器实例（继承 ConfigManagerBase）
    private static readonly InternalConfigManager _manager = new();
    
    /// <summary>
    /// 获取当前配置实例（线程安全）
    /// </summary>
    public static InfluenceConfig Current => _manager.Config;
    
    /// <summary>
    /// 初始化配置管理器（游戏启动时调用一次）
    /// </summary>
    public static void Initialize() => _manager.Initialize();
    
    /// <summary>
    /// 在主线程 Update 中调用（每帧检查是否需要热重载）
    /// </summary>
    public static void Update() => _manager.Update();
    
    /// <summary>
    /// 手动重新加载配置
    /// </summary>
    public static void ReloadConfig() => _manager.ReloadConfig();
    
    // ==================== 内部管理器类 ====================
    
    private class InternalConfigManager : ConfigManagerBase<InfluenceConfig>
    {
        /// <summary>
        /// 配置文件名
        /// </summary>
        protected override string ConfigFileName => "InfluenceConfig.json";
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        protected override InfluenceConfig CreateDefaultConfig() => new();
    }
}
