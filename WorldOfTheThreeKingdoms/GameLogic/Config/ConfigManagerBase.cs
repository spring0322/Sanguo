using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.GameLogic.Config;

/// <summary>
/// 配置管理器基类（提供统一的热重载机制）
/// 日期：2026-03-10
/// 用途：为所有配置管理器提供统一的加载、热重载和线程安全机制
/// </summary>
public abstract class ConfigManagerBase<T> where T : class, new()
{
    private T _config;
    private readonly object _lock = new();
    private FileSystemWatcher _watcher;
    
    // 防抖控制（Debounce）
    private volatile bool _isDirty = false;
    private DateTime _lastEventTime = DateTime.MinValue;
    private static readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(500);
    
    // 🔥 性能优化：缓存类型名称，避免 HOT PATH 中重复调用 GetType()
    private readonly string _typeName;

    protected ConfigManagerBase()
    {
        _typeName = GetType().Name;
    }

    /// <summary>
    /// 配置文件名（子类必须实现）
    /// </summary>
    protected abstract string ConfigFileName { get; }
    
    /// <summary>
    /// 创建默认配置（子类必须实现）
    /// </summary>
    protected abstract T CreateDefaultConfig();

    /// <summary>
    /// 获取配置实例（线程安全）
    /// </summary>
    public T Config
    {
        get
        {
            if (_config == null)
            {
                lock (_lock)
                {
                    _config ??= LoadConfig();
                }
            }
            return _config;
        }
    }

    /// <summary>
    /// 加载配置文件
    /// 🧊 COLD PATH：初始化阶段，可读性优先
    /// </summary>
    protected T LoadConfig()
    {
        try
        {
            string configPath = Path.Combine("Content", "Data", ConfigFileName);
            
            if (!File.Exists(configPath))
            {
                System.Diagnostics.Debug.WriteLine($"[{_typeName}] 配置文件不存在: {configPath}，使用默认配置");
                return CreateDefaultConfig();
            }

            string jsonContent = File.ReadAllText(configPath);
            
            // 🔥 2026-03-11 修复：配置文件使用简化的序列化选项
            // 问题：GameJsonContext.GetDefaultOptions() 启用了 ReferenceHandler.Preserve
            //       导致 JSON 中的 $schema 属性与元数据保留字冲突
            // 解决：配置文件不需要引用保留，使用简化选项
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // 🔥 关键：不启用 ReferenceHandler.Preserve，允许 $schema 等属性
                TypeInfoResolver = GameJsonContext.Default
            };
            
            var config = JsonSerializer.Deserialize<T>(jsonContent, options);

            // ANTI-BAND-AID：配置反序列化失败时使用断言
            System.Diagnostics.Debug.Assert(config != null, 
                $"[{_typeName}] 配置反序列化失败，检查 {ConfigFileName} 的 JSON 格式");

            if (config == null)
            {
                System.Diagnostics.Debug.WriteLine($"[{_typeName}] 配置解析失败，使用默认配置");
                return CreateDefaultConfig();
            }

            System.Diagnostics.Debug.WriteLine($"[{_typeName}] 配置加载成功");
            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{_typeName}] 加载配置失败: {ex.Message}，使用默认配置");
            return CreateDefaultConfig();
        }
    }

    /// <summary>
    /// 初始化配置管理器（游戏启动时调用一次）
    /// </summary>
    public void Initialize()
    {
        _config = LoadConfig();
        SetupWatcher();
    }

    /// <summary>
    /// 设置文件监听器（热重载）
    /// </summary>
    private void SetupWatcher()
    {
        try
        {
            string directory = Path.Combine("Content", "Data");
            
            if (!Directory.Exists(directory))
            {
                System.Diagnostics.Debug.WriteLine($"[{_typeName}] 配置目录不存在: {directory}");
                return;
            }
            
            _watcher = new FileSystemWatcher(directory, ConfigFileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            // 绑定多重事件（应对不同编辑器的保存策略）
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            
            System.Diagnostics.Debug.WriteLine($"[{_typeName}] 热重载监听已启动: {ConfigFileName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{_typeName}] 热重载监听失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 文件变更回调（后台线程）
    /// 🔥 注意：此方法运行在后台线程，只打标记，不做业务逻辑
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _isDirty = true;
        _lastEventTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 在主线程 Update 中调用（每帧检查是否需要热重载）
    /// 🔥 HOT PATH：每帧调用，必须高性能
    /// </summary>
    public void Update()
    {
        // 🔥 性能优化：早期短路，避免不必要的计算
        if (!_isDirty) return;
        
        // 防抖：距离最后一次文件事件已经过去了 500 毫秒
        if ((DateTime.UtcNow - _lastEventTime) > _debounceTime)
        {
            _isDirty = false;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{_typeName}] 检测到配置变更，执行热重载");
            #endif
            
            lock (_lock)
            {
                _config = LoadConfig();
            }
        }
    }

    /// <summary>
    /// 手动重新加载配置
    /// </summary>
    public void ReloadConfig()
    {
        lock (_lock)
        {
            _config = LoadConfig();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
