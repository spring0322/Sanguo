using System;
using GameManager;
using Xunit;

namespace STJDiagnosticsTests;

/// <summary>
/// AsyncTextureLoader 测试。
///
/// 说明：
/// - 可在无图形设备环境中运行的行为，使用真实断言验证。
/// - 依赖 GraphicsDevice 的测试明确标记为 Skip，避免 CI 中出现“永远通过”的占位断言。
/// </summary>
public class AsyncTextureLoaderTests
{
    [Fact]
    public void Constructor_NullGraphicsDevice_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new AsyncTextureLoader(null!));
        Assert.Equal("graphicsDevice", ex.ParamName);
    }

    /// <summary>
    /// 该用例依赖真实 GraphicsDevice，当前测试项目不提供图形上下文。
    /// </summary>
    [Fact(Skip = "需要真实 GraphicsDevice 才能执行此用例。")]
    public void LoadTextureAsync_SamePath_ReusesSameTask()
    {
    }

    /// <summary>
    /// 该用例依赖真实 GraphicsDevice，当前测试项目不提供图形上下文。
    /// </summary>
    [Fact(Skip = "需要真实 GraphicsDevice 才能执行此用例。")]
    public void LoadTextureAsync_WithCancellation_ThrowsOperationCanceledException()
    {
    }

    /// <summary>
    /// 该用例依赖真实 GraphicsDevice，当前测试项目不提供图形上下文。
    /// </summary>
    [Fact(Skip = "需要真实 GraphicsDevice 才能执行此用例。")]
    public void LoadTexturesAsync_WithProgress_ReportsProgress()
    {
    }

    /// <summary>
    /// 该用例依赖真实 GraphicsDevice，当前测试项目不提供图形上下文。
    /// </summary>
    [Fact(Skip = "需要真实 GraphicsDevice 才能执行此用例。")]
    public void CancelAll_CancelsAllPendingTasks()
    {
    }
}
