using System.Diagnostics;

namespace youcelanPlugin;

/// <summary>
/// youcelanPlugin 渲染诊断工具
/// 使用条件编译确保零性能开销
/// </summary>
internal static class DrawDiagnostics
{
    /// <summary>
    /// 记录 TabListPlugin.Draw 调用
    /// </summary>
    [Conditional("DEBUG_YOUCELAN_DRAW")]
    public static void LogPluginDraw(bool isShowing)
    {
        Debug.WriteLine($"[TabListPlugin.Draw] 被调用, IsShowing={isShowing}");
    }

    /// <summary>
    /// 记录 TabListInFrame.Draw 入口
    /// </summary>
    [Conditional("DEBUG_YOUCELAN_DRAW")]
    public static void LogFrameDraw(bool jiancexianshi, bool xianshiyoucelan)
    {
        Debug.WriteLine($"[TabListInFrame.Draw] jiancexianshi={jiancexianshi}, xianshiyoucelan={xianshiyoucelan}");
    }

    /// <summary>
    /// 记录进入 jiancexianshi 分支
    /// </summary>
    [Conditional("DEBUG_YOUCELAN_DRAW")]
    public static void LogEnterJiancexianshi()
    {
        Debug.WriteLine("[TabListInFrame.Draw] ✅ 进入 jiancexianshi 分支");
    }

    /// <summary>
    /// 记录进入 xianshiyoucelan 分支
    /// </summary>
    [Conditional("DEBUG_YOUCELAN_DRAW")]
    public static void LogEnterXianshiyoucelan()
    {
        Debug.WriteLine("[TabListInFrame.Draw] ✅ 进入 xianshiyoucelan 分支,开始渲染");
    }
}
