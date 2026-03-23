using System;
using GameObjects;

namespace GameObjects.AI;

#if false

/// <summary>
/// 统一战术AI系统集成器
/// 🎯 目的：将新的统一战术AI系统正确集成到现有AI调用链中
/// 📍 调用位置：Legion.AI() 方法开始处
/// </summary>
public static class UnifiedTacticalAIIntegration
{
    /// <summary>
    /// 在Legion.AI()开始时调用，替代原有的分散AI逻辑
    /// </summary>
    /// <param name="legion">当前执行AI的军团</param>
    /// <returns>true=使用新系统，false=回退到旧系统</returns>
    public static bool TryExecuteUnifiedAI(Legion legion)
    {
        try
        {
            // 检查是否启用统一战术AI系统
            if (!AITacticalConfigManager.Instance.IsEnabled)
            {
                return false; // 回退到旧系统
            }

            // 执行统一战术AI
            UnifiedTacticalAI.ExecuteLegionTacticalTurn(legion);
            
            return true; // 成功执行新系统
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[统一战术AI集成] 错误: {ex.Message}");
            return false; // 出错时回退到旧系统
        }
    }
}
#endif
