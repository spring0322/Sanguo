using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using GameManager;
using Platforms;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// LRU 纹理缓存管理器
    /// 基于显存使用量和访问频率的智能缓存系统
    /// </summary>
    public class LRUTextureCache
    {
        // --- 配置 ---
        // 最大显存预算 (字节)，例如 1GB = 1024 * 1024 * 1024
        private long _maxMemoryBytes;

        // --- 状态 ---
        private long _currentMemoryUsage = 0;
        private Dictionary<string, LRUCacheNode> _cacheMap;

        // 双向链表的头尾指针
        private LRUCacheNode _head; // 最近使用的 (Most Recently Used)
        private LRUCacheNode _tail; // 最久未使用的 (Least Recently Used)

        private GraphicsDevice _graphicsDevice;

        public LRUTextureCache(GraphicsDevice graphics, long maxMemoryMB = 1024)
        {
            _graphicsDevice = graphics;
            _maxMemoryBytes = maxMemoryMB * 1024 * 1024;
            _cacheMap = new Dictionary<string, LRUCacheNode>();
        }

        /// <summary>
        /// 获取纹理（对外唯一接口）
        /// </summary>
        public Texture2D Get(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 1. 缓存命中
            if (_cacheMap.ContainsKey(path))
            {
                LRUCacheNode node = _cacheMap[path];
                
                // 检查纹理是否已被释放
                if (node.Texture != null && !node.Texture.IsDisposed)
                {
                    // 关键操作：既然访问了，它就是"最近使用的"，移到头部
                    MoveToHead(node);
                    return node.Texture;
                }
                else
                {
                    // 纹理已被释放，从缓存中移除
                    RemoveFromCache(node);
                }
            }

            // 2. 缓存未命中，需要加载
            Texture2D tex = LoadFromDisk(path);
            if (tex != null)
            {
                // 计算大小 (估算值：宽 * 高 * 4字节)
                // 针对 DXT 压缩纹理，这个计算会比实际大，作为预算是安全的
                long size = CalculateTextureSize(tex);

                // 创建新节点
                LRUCacheNode newNode = new LRUCacheNode(path, tex, size);

                // 加入缓存
                AddToHead(newNode);
                _cacheMap[path] = newNode;
                _currentMemoryUsage += size;

                // 3. 检查水位线，如果超标，执行清理
                Trim();

                return tex;
            }

            return null; // 文件不存在
        }

        /// <summary>
        /// 计算纹理占用的显存大小
        /// </summary>
        private long CalculateTextureSize(Texture2D texture)
        {
            if (texture == null) return 0;

            // 基础计算：宽 * 高 * 每像素字节数
            // 对于压缩格式，这是一个保守估算
            long baseSize = texture.Width * texture.Height * 4;

            // 考虑 mipmap 的额外开销（约 1.33 倍）
            return (long)(baseSize * 1.33);
        }

        /// <summary>
        /// 确保显存不超标
        /// </summary>
        private void Trim()
        {
            // 只要当前用量大于预算，且链表不为空，就一直删尾部
            while (_currentMemoryUsage > _maxMemoryBytes && _tail != null)
            {
                RemoveTail();
            }
        }

        /// <summary>
        /// 强制清理指定数量的最久未使用纹理
        /// </summary>
        public void ForceClean(int count = 10)
        {
            int cleaned = 0;
            while (cleaned < count && _tail != null)
            {
                RemoveTail();
                cleaned++;
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            while (_tail != null)
            {
                RemoveTail();
            }
            
            _cacheMap.Clear();
            _currentMemoryUsage = 0;
            _head = null;
            _tail = null;
        }

        // --- 链表操作 (Private) ---
        private void AddToHead(LRUCacheNode node)
        {
            node.Previous = null;
            node.Next = _head;

            if (_head != null)
                _head.Previous = node;

            _head = node;

            if (_tail == null)
                _tail = node;
        }

        private void RemoveNode(LRUCacheNode node)
        {
            if (node.Previous != null)
                node.Previous.Next = node.Next;
            else
                _head = node.Next; // 如果删的是头

            if (node.Next != null)
                node.Next.Previous = node.Previous;
            else
                _tail = node.Previous; // 如果删的是尾
        }

        private void MoveToHead(LRUCacheNode node)
        {
            if (node == _head) return; // 已经在头部

            RemoveNode(node);
            AddToHead(node);
        }

        private void RemoveTail()
        {
            if (_tail == null) return;

            LRUCacheNode temp = _tail;

            // 1. 从链表移除
            RemoveNode(temp);

            // 2. 从字典移除
            _cacheMap.Remove(temp.Key);

            // 3. 扣除占用计数
            _currentMemoryUsage -= temp.SizeInBytes;

            // 4. 【关键】真正释放显存
            if (temp.Texture != null && !temp.Texture.IsDisposed)
            {
                temp.Texture.Dispose();
                System.Diagnostics.Debug.WriteLine($"[LRU GC] 释放资源: {temp.Key}, 释放显存: {temp.SizeInBytes / 1024 / 1024} MB");
            }
        }

        private void RemoveFromCache(LRUCacheNode node)
        {
            RemoveNode(node);
            _cacheMap.Remove(node.Key);
            _currentMemoryUsage -= node.SizeInBytes;
        }

        // --- 磁盘加载逻辑 (集成之前的 DDS/PNG/JPG 兼容加载) ---
        private Texture2D LoadFromDisk(string path)
        {
            try
            {
                // 使用现有的 Platform.LoadTexture 方法，它已经支持 DDS 优先级
                return Platform.Current.LoadTexture(path, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LRU] 加载纹理失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }

        // --- 调试和监控接口 ---
        public string GetDebugInfo()
        {
            return $"Cached Items: {_cacheMap.Count}, Memory: {_currentMemoryUsage / 1024 / 1024} MB / {_maxMemoryBytes / 1024 / 1024} MB";
        }

        public long CurrentMemoryUsage => _currentMemoryUsage;
        public long MaxMemoryBytes => _maxMemoryBytes;
        public int CachedItemCount => _cacheMap.Count;

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStats GetStats()
        {
            return new CacheStats
            {
                CachedItems = _cacheMap.Count,
                CurrentMemoryMB = _currentMemoryUsage / 1024 / 1024,
                MaxMemoryMB = _maxMemoryBytes / 1024 / 1024,
                MemoryUsagePercent = _maxMemoryBytes > 0 ? (double)_currentMemoryUsage / _maxMemoryBytes * 100 : 0
            };
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public struct CacheStats
    {
        public int CachedItems;
        public long CurrentMemoryMB;
        public long MaxMemoryMB;
        public double MemoryUsagePercent;

        public override string ToString()
        {
            return $"Items: {CachedItems}, Memory: {CurrentMemoryMB}/{MaxMemoryMB} MB ({MemoryUsagePercent:F1}%)";
        }
    }

    // LRUCacheNode class is defined in a separate file: LRUCacheNode.cs
}