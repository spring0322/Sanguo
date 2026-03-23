using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.GameData;

namespace WorldOfTheThreeKingdoms.GameLogic.Config;

/// <summary>
/// 配置管理器协调器
/// 日期：2026-03-10
/// 用途：统一管理所有配置管理器的初始化和热重载更新
/// </summary>
public static class ConfigManagerCoordinator
{
    private static bool _isInitialized = false;

    /// <summary>
    /// 初始化所有配置管理器（游戏启动时调用一次）
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            System.Diagnostics.Debug.WriteLine("[ConfigManagerCoordinator] 配置管理器已初始化，跳过重复初始化");
            return;
        }

        System.Diagnostics.Debug.WriteLine("[ConfigManagerCoordinator] 开始初始化所有配置管理器");

        // 初始化所有 AI 配置管理器
        AITacticalConfigManager.Initialize();
        AIRoleConfigManager.Initialize();
        AIDecisionConfigManager.Initialize();
        
        // 🆕 初始化势力范围配置管理器（2026-03-21）
        InfluenceConfigManager.Initialize();

        _isInitialized = true;
        System.Diagnostics.Debug.WriteLine("[ConfigManagerCoordinator] 所有配置管理器初始化完成");
    }

    /// <summary>
    /// 更新所有配置管理器（在主线程 Update 中调用）
    /// 🔥 HOT PATH：每帧调用，必须高性能
    /// </summary>
    public static void Update()
    {
        if (!_isInitialized) return;

        // 更新所有配置管理器的热重载检查
        AITacticalConfigManager.Update();
        AIRoleConfigManager.Update();
        AIDecisionConfigManager.Update();
        
        // 🆕 更新势力范围配置管理器（2026-03-21）
        InfluenceConfigManager.Update();
    }

    /// <summary>
    /// 手动重新加载所有配置
    /// </summary>
    public static void ReloadAll()
    {
        System.Diagnostics.Debug.WriteLine("[ConfigManagerCoordinator] 手动重新加载所有配置");
        
        AITacticalConfigManager.ReloadConfig();
        AIRoleConfigManager.ReloadConfig();
        AIDecisionConfigManager.ReloadConfig();
        
        // 🆕 重新加载势力范围配置（2026-03-21）
        InfluenceConfigManager.ReloadConfig();
    }
}
