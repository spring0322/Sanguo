#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.GamePlugins
{
    /// <summary>
    /// 支持异步加载的插件基类
    /// 提供统一的异步加载接口，将插件初始化拆分为"配置加载"和"资源加载"两个阶段
    /// </summary>
    public abstract class AsyncLoadablePlugin
    {
        protected bool _texturesLoaded = false;
        protected Task? _loadingTask;
        protected CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// 快速初始化（只加载配置，不加载纹理）
        /// 在游戏启动时同步调用，必须快速完成（< 100ms）
        /// </summary>
        public virtual void SetGraphicsDevice()
        {
            var sw = Stopwatch.StartNew();

            LoadConfigOnly();

            sw.Stop();
            Debug.WriteLine($"[{GetType().Name}] 配置加载耗时: {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// 只加载配置，不加载纹理（子类实现）
        /// 包括：解析 XML 配置、初始化数据结构、注册事件处理器
        /// 不包括：加载纹理资源、创建 UI 元素
        /// </summary>
        protected abstract void LoadConfigOnly();

        /// <summary>
        /// 异步加载纹理资源（子类实现）
        /// 在首次显示插件时调用，可以耗时较长
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        protected abstract Task LoadTexturesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 确保纹理已加载
        /// 如果纹理未加载，则启动异步加载任务
        /// 如果纹理正在加载，则等待加载完成
        /// 如果纹理已加载，则立即返回
        /// </summary>
        public async Task EnsureTexturesLoadedAsync()
        {
            if (_texturesLoaded) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _loadingTask ??= LoadTexturesWithProgressAsync(_cancellationTokenSource.Token);

            await _loadingTask;
            _texturesLoaded = true;
        }

        /// <summary>
        /// 带进度监控的纹理加载
        /// </summary>
        private async Task LoadTexturesWithProgressAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Debug.WriteLine($"[{GetType().Name}] 开始异步加载纹理...");

                await LoadTexturesAsync(cancellationToken);

                sw.Stop();
                Debug.WriteLine($"[{GetType().Name}] 纹理加载完成，耗时: {sw.ElapsedMilliseconds} ms");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{GetType().Name}] 纹理加载已取消");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetType().Name}] 纹理加载失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 取消加载
        /// </summary>
        public void CancelLoading()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 异步显示插件
        /// 确保资源已加载后再显示
        /// </summary>
        public virtual async Task ShowAsync()
        {
            if (!_texturesLoaded)
            {
                // TODO: 显示加载提示 UI
                await EnsureTexturesLoadedAsync();
                // TODO: 隐藏加载提示 UI
            }

            // 显示插件 UI
            Show();
        }

        /// <summary>
        /// 显示插件（子类实现）
        /// 当纹理资源已加载后调用
        /// </summary>
        protected abstract void Show();
    }
}
