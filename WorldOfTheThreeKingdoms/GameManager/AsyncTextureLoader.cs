using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameManager
{
    /// <summary>
    /// 异步纹理加载器，支持后台加载纹理资源
    /// </summary>
    public class AsyncTextureLoader(GraphicsDevice graphicsDevice)
    {
        private readonly GraphicsDevice _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        private readonly ConcurrentDictionary<string, Task<Texture2D>> _loadingTasks = [];
        
        /// <summary>
        /// 异步加载单个纹理
        /// </summary>
        public async Task<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
        {
            // 检查是否已在加载中（避免重复加载）
            if (_loadingTasks.TryGetValue(path, out Task<Texture2D> existingTask))
            {
                return await existingTask;
            }
            
            // 创建加载任务
            Task<Texture2D> loadTask = LoadTextureInternalAsync(path, cancellationToken);
            _loadingTasks[path] = loadTask;
            
            try
            {
                Texture2D texture = await loadTask;
                return texture;
            }
            finally
            {
                // 加载完成后移除任务
                _loadingTasks.TryRemove(path, out _);
            }
        }
        
        /// <summary>
        /// 批量异步加载纹理（容错版本）
        /// </summary>
        public async Task<Dictionary<string, Texture2D>> LoadTexturesAsync(
            IEnumerable<string> paths, 
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            List<string> pathList = paths.ToList();
            Dictionary<string, Texture2D> results = [];
            int completed = 0;
            int failed = 0;
            
            // 并行加载（限制并发数）
            SemaphoreSlim semaphore = new(4); // 最多 4 个并发加载
            List<Task> tasks = pathList.Select(async path =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    Texture2D texture = await LoadTextureAsync(path, cancellationToken);
                    lock (results)
                    {
                        results[path] = texture;
                        completed++;
                        progress?.Report((float)completed / pathList.Count);
                    }
                }
                catch (Exception ex)
                {
                    // 容错：单个纹理加载失败不影响其他纹理
                    System.Diagnostics.Debug.WriteLine($"[AsyncTextureLoader] 纹理加载失败: {path}, 错误: {ex.Message}");
                    lock (results)
                    {
                        failed++;
                        completed++;
                        progress?.Report((float)completed / pathList.Count);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();
            
            await Task.WhenAll(tasks);
            
            if (failed > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AsyncTextureLoader] 加载完成: 成功 {results.Count} 个, 失败 {failed} 个");
            }
            
            return results;
        }
        
        /// <summary>
        /// 内部加载实现
        /// </summary>
        private async Task<Texture2D> LoadTextureInternalAsync(string path, CancellationToken cancellationToken)
        {
            // 使用 Platform.Current.LoadTexture 直接加载纹理
            // 这个方法已经处理了所有平台兼容性问题
            Texture2D texture = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 使用 Platform.Current.LoadTexture 保持兼容性
                return Platforms.Platform.Current.LoadTexture(path, false);
            }, cancellationToken);
            
            return texture;
        }
        
        /// <summary>
        /// 取消所有加载任务
        /// </summary>
        public void CancelAll()
        {
            _loadingTasks.Clear();
        }
    }
}
