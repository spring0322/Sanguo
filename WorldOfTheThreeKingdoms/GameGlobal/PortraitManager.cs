using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public class PortraitManager
    {
        private static PortraitManager _instance;
        public static PortraitManager Instance => _instance ?? (_instance = new PortraitManager());

        // --- 配置 ---
        private const int MAX_CACHE_SIZE = 200; // 缓存上限，超过自动清理

        private GraphicsDevice _graphicsDevice;
        private string _portraitPath;
        private Texture2D _defaultLoadingTexture;

        // --- 核心存储结构 ---
        // 1. 缓存字典：快速查找 ID -> Texture
        private Dictionary<int, Texture2D> _cache;
        
        // 2. LRU 链表：记录访问顺序 (链表头 = 最近用的，链表尾 = 最久没用的)
        private LinkedList<int> _lruList; 

        // 3. 异步相关
        private HashSet<int> _loadingIds;
        private ConcurrentQueue<Tuple<int, MemoryStream>> _finishedStreams;

        private PortraitManager()
        {
            _cache = new Dictionary<int, Texture2D>();
            _lruList = new LinkedList<int>(); // 新增
            _loadingIds = new HashSet<int>();
            _finishedStreams = new ConcurrentQueue<Tuple<int, MemoryStream>>();
        }

        public void Initialize(GraphicsDevice graphicsDevice, string path, Texture2D defaultTexture)
        {
            // [修复] 如果已经初始化过，先清理旧缓存（可能持有旧设备的引用）
            if (_graphicsDevice != null && _graphicsDevice != graphicsDevice)
            {
                System.Diagnostics.Debug.WriteLine("[PortraitManager] Re-initializing with new GraphicsDevice, clearing cache.");
                foreach (var tex in _cache.Values)
                {
                    if (tex != null && !tex.IsDisposed) tex.Dispose();
                }
                _cache.Clear();
                _lruList.Clear();
                _loadingIds.Clear();
                // 清理队列
                while (_finishedStreams.TryDequeue(out _)) { }
            }

            _graphicsDevice = graphicsDevice;
            _portraitPath = path;
            _defaultLoadingTexture = defaultTexture;
        }

        /// <summary>
        /// 获取头像（带LRU刷新）
        /// </summary>
        public Texture2D GetPortrait(int personId)
        {
            // 情况A：缓存命中
            if (_cache.ContainsKey(personId))
            {
                // [关键点] 既然被访问了，就把它移动到"最近使用"列表的头部
                TouchCache(personId); 
                return _cache[personId];
            }

            // 情况B：未命中，且不在加载中
            if (!_loadingIds.Contains(personId))
            {
                _loadingIds.Add(personId);
                Task.Run(() => LoadPortraitAsync(personId));
            }

            // 情况C：正在加载
            return _defaultLoadingTexture;
        }

        // --- LRU 核心操作 ---
        
        /// <summary>
        /// 将指定ID提升为"最近使用"
        /// </summary>
        private void TouchCache(int id)
        {
            // 如果已经在链表里，先移除旧位置
            if (_lruList.Contains(id)) 
            {
                _lruList.Remove(id);
            }
            // 放到头部
            _lruList.AddFirst(id);
        }

        /// <summary>
        /// 检查是否超限，如果超限则清理最旧的图片
        /// </summary>
        private void EnforceCacheLimit()
        {
            while (_cache.Count > MAX_CACHE_SIZE)
            {
                // 1. 找到最久没用的那个ID (链表尾部)
                int idToRemove = _lruList.Last.Value;
                
                // 2. 释放显存资源 (XNA中这步最重要！)
                if (_cache.TryGetValue(idToRemove, out Texture2D tex))
                {
                    if (tex != null && !tex.IsDisposed)
                    {
                        tex.Dispose();
                    }
                }

                // 3. 从字典和链表中移除
                _cache.Remove(idToRemove);
                _lruList.RemoveLast();
                
                // 调试日志 (可选)
                // Console.WriteLine($"显存自动清理：释放了头像 ID {idToRemove}");
            }
        }

        // --- 异步加载逻辑 (保持原样，只在Update里增加缓存添加逻辑) ---

        private void LoadPortraitAsync(int id)
        {
            try
            {
                string filePath = Path.Combine(_portraitPath, id + ".jpg");
                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    MemoryStream stream = new MemoryStream(fileData);
                    _finishedStreams.Enqueue(new Tuple<int, MemoryStream>(id, stream));
                }
                else
                {
                    lock (_loadingIds) { _loadingIds.Remove(id); }
                }
            }
            catch
            {
                lock (_loadingIds) { _loadingIds.Remove(id); }
            }
        }

        public void Update()
        {
            int processCount = 0;
            while (processCount < 5 && _finishedStreams.TryDequeue(out var data))
            {
                int id = data.Item1;
                MemoryStream stream = data.Item2;

                try
                {
                    // 🔥 技术性修复：防御式检查
                    if (_graphicsDevice == null || _graphicsDevice.IsDisposed || !global::GameManager.CacheManager.TryRecoverDevice())
                    {
                        // 如果设备当前丢失，不尝试转换流到纹理，直接入队列等待。
                        // 或者这里简单的丢弃，反正以后还会重新请求加载。
                        continue; 
                    }

                    Texture2D tex = Texture2D.FromStream(_graphicsDevice, stream);
                    
                    // --- 只有这里变了 ---
                    
                    // 1. 存入字典
                    if (_cache.ContainsKey(id)) _cache[id].Dispose(); // 防御性清理
                    _cache[id] = tex;

                    // 2. 记录为最近使用
                    TouchCache(id);

                    // 3. 检查是否爆缓存，如果爆了自动删旧图
                    EnforceCacheLimit(); 
                    
                    // ---------------------
                }
                catch (Exception ex)
                {
                    // 🔥 检测GPU设备移除异常
                    if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                        ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                    {
                        global::GameManager.CacheManager.MarkDeviceLost(ex);
                    }
                    /* 忽略坏图 */ 
                }
                finally
                {
                    stream.Dispose();
                    _loadingIds.Remove(id);
                }
                processCount++;
            }
        }

        // 修改武将图片时手动调用的刷新
        public void RefreshPortrait(int personId)
        {
            if (_cache.ContainsKey(personId))
            {
                _cache[personId].Dispose();
                _cache.Remove(personId);
                _lruList.Remove(personId); // 记得也要从LRU链表移除
            }
        }
    }
}
