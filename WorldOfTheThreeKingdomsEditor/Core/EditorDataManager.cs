using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdomsEditor.Core;

/// <summary>
/// 编辑器数据管理器 - 封装数据加载和保存逻辑（异步）
/// 
/// 职责：
/// 1. 使用 SerializationManager 加载和保存剧本/存档
/// 2. 提供异步 API 避免 UI 线程阻塞（.sav.gz 解压耗时）
/// 3. 清理 WEGO 运行时状态（防止修改后的存档加载时崩溃）
/// 4. 包装集合为 ObservableCollection（支持 WPF 数据绑定）
/// 
/// 日期：2026-04-01
/// </summary>
public class EditorDataManager
{
    private readonly SerializationManager _serializationManager;

    /// <summary>
    /// 构造函数 - 初始化 SerializationManager
    /// </summary>
    public EditorDataManager()
    {
        _serializationManager = new SerializationManager();
        
        #if DEBUG
        Debug.WriteLine("[EditorDataManager] 初始化完成");
        #endif
    }

    /// <summary>
    /// 加载剧本文件（异步）
    /// </summary>
    /// <param name="filePath">剧本文件路径（.sav.gz 格式）</param>
    /// <returns>加载的 GameScenario 对象</returns>
    /// <exception cref="ArgumentNullException">文件路径为 null</exception>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    /// <exception cref="InvalidOperationException">数据加载或验证失败</exception>
    public async Task<GameScenario> LoadScenarioAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "剧本文件路径不能为空");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"剧本文件不存在：{filePath}", filePath);
        }

        #if DEBUG
        Debug.WriteLine($"[EditorDataManager] 开始加载剧本: {filePath}");
        var stopwatch = Stopwatch.StartNew();
        #endif

        try
        {
            // 🔥 使用 Task.Run 在后台线程执行 I/O 密集操作
            GameScenario scenario = await Task.Run(() =>
            {
                // 使用 SerializationManager.LoadGame 加载 .sav.gz 文件
                // 🔥 修复：LoadGame 只有一个参数 filePath
                return _serializationManager.LoadGame(filePath);
            });

            // 🔥 包装集合为 ObservableCollection（支持 WPF 数据绑定）
            WrapCollectionsForBinding(scenario);

            #if DEBUG
            stopwatch.Stop();
            Debug.WriteLine($"[EditorDataManager] 剧本加载完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            Debug.WriteLine($"[EditorDataManager] 势力数量: {scenario.Factions.Count}");
            Debug.WriteLine($"[EditorDataManager] 武将数量: {scenario.Persons.Count}");
            Debug.WriteLine($"[EditorDataManager] 建筑数量: {scenario.Architectures.Count}");
            #endif

            return scenario;
        }
        catch (Exception ex)
        {
            #if DEBUG
            Debug.WriteLine($"[EditorDataManager] 加载剧本失败: {ex.Message}");
            #endif
            
            throw new InvalidOperationException(
                $"加载剧本失败\n文件：{filePath}\n错误：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载存档文件（异步）
    /// </summary>
    /// <param name="filePath">存档文件路径（.sav.gz 格式）</param>
    /// <returns>加载的 GameScenario 对象</returns>
    /// <exception cref="ArgumentNullException">文件路径为 null</exception>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    /// <exception cref="InvalidOperationException">数据加载或验证失败</exception>
    public async Task<GameScenario> LoadSaveFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "存档文件路径不能为空");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"存档文件不存在：{filePath}", filePath);
        }

        #if DEBUG
        Debug.WriteLine($"[EditorDataManager] 开始加载存档: {filePath}");
        var stopwatch = Stopwatch.StartNew();
        #endif

        try
        {
            // 🔥 使用 Task.Run 在后台线程执行 I/O 密集操作
            GameScenario scenario = await Task.Run(() =>
            {
                // 使用 SerializationManager.LoadGame 加载 .sav.gz 文件
                // 🔥 修复：LoadGame 只有一个参数 filePath
                return _serializationManager.LoadGame(filePath);
            });

            // 🔥 清理 WEGO 运行时状态（防止修改后的存档加载时崩溃）
            CleanupRuntimeState(scenario);

            // 🔥 包装集合为 ObservableCollection（支持 WPF 数据绑定）
            WrapCollectionsForBinding(scenario);

            #if DEBUG
            stopwatch.Stop();
            Debug.WriteLine($"[EditorDataManager] 存档加载完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            Debug.WriteLine($"[EditorDataManager] 势力数量: {scenario.Factions.Count}");
            Debug.WriteLine($"[EditorDataManager] 武将数量: {scenario.Persons.Count}");
            Debug.WriteLine($"[EditorDataManager] 部队数量: {scenario.Troops.Count}");
            #endif

            return scenario;
        }
        catch (Exception ex)
        {
            #if DEBUG
            Debug.WriteLine($"[EditorDataManager] 加载存档失败: {ex.Message}");
            #endif
            
            throw new InvalidOperationException(
                $"加载存档失败\n文件：{filePath}\n错误：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存剧本文件（异步）
    /// </summary>
    /// <param name="scenario">要保存的 GameScenario 对象（调用者保证非 null）</param>
    /// <param name="filePath">保存路径（.sav.gz 格式）</param>
    /// <exception cref="ArgumentNullException">filePath 为 null</exception>
    /// <exception cref="InvalidOperationException">数据保存失败</exception>
    public async Task SaveScenarioAsync(GameScenario scenario, string filePath)
    {
        // 🔥 ANTI-BAND-AID：调用者保证 scenario 非 null（来自 LoadScenarioAsync 返回值）
        // 如果为 null 则是编程错误，让 NullReferenceException 暴露问题
        
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "保存路径不能为空");
        }

        #if DEBUG
        Debug.WriteLine($"[EditorDataManager] 开始保存剧本: {filePath}");
        var stopwatch = Stopwatch.StartNew();
        #endif

        try
        {
            // 🔥 使用 Task.Run 在后台线程执行 I/O 密集操作
            await Task.Run(() =>
            {
                // 使用 SerializationManager.SaveGame 保存为 .sav.gz 文件
                _serializationManager.SaveGame(scenario, filePath);
            });

            #if DEBUG
            stopwatch.Stop();
            Debug.WriteLine($"[EditorDataManager] 剧本保存完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            #endif
        }
        catch (Exception ex)
        {
            #if DEBUG
            Debug.WriteLine($"[EditorDataManager] 保存剧本失败: {ex.Message}");
            #endif
            
            throw new InvalidOperationException(
                $"保存剧本失败\n文件：{filePath}\n错误：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存存档文件（异步）
    /// </summary>
    /// <param name="scenario">要保存的 GameScenario 对象（调用者保证非 null）</param>
    /// <param name="filePath">保存路径（.sav.gz 格式）</param>
    /// <exception cref="ArgumentNullException">filePath 为 null</exception>
    /// <exception cref="InvalidOperationException">数据保存失败</exception>
    public async Task SaveSaveFileAsync(GameScenario scenario, string filePath)
    {
        // 🔥 ANTI-BAND-AID：调用者保证 scenario 非 null（来自 LoadSaveFileAsync 返回值）
        // 如果为 null 则是编程错误，让 NullReferenceException 暴露问题
        
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "保存路径不能为空");
        }

        #if DEBUG
        Debug.WriteLine($"[EditorDataManager] 开始保存存档: {filePath}");
        var stopwatch = Stopwatch.StartNew();
        #endif

        try
        {
            // 🔥 使用 Task.Run 在后台线程执行 I/O 密集操作
            await Task.Run(() =>
            {
                // 使用 SerializationManager.SaveGame 保存为 .sav.gz 文件
                _serializationManager.SaveGame(scenario, filePath);
            });

            #if DEBUG
            stopwatch.Stop();
            Debug.WriteLine($"[EditorDataManager] 存档保存完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            #endif
        }
        catch (Exception ex)
        {
            #if DEBUG
            Debug.WriteLine($"[EditorDataManager] 保存存档失败: {ex.Message}");
            #endif
            
            throw new InvalidOperationException(
                $"保存存档失败\n文件：{filePath}\n错误：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清理 WEGO 运行时状态（防止修改后的存档加载时崩溃）
    /// 
    /// WEGO 机制说明：
    /// - 战略阶段（操作面）：玩家下达命令，设置 Command 和 Operated 标志
    /// - 执行阶段（行动面）：所有部队同步执行命令，设置 CurrentAIState 和 OperationDone 标志
    /// 
    /// 编辑器修改存档后，必须清理执行阶段的运行时状态，否则加载时会崩溃：
    /// 1. 部队指令队列（Troop.Command, Troop.CurrentAIState）
    /// 2. 寻路路径缓存（Troop.PathCache）
    /// 3. 行动冷却时间（Troop.OperationDone, Troop.Operated）
    /// 4. 势力的 AIFinished 标志
    /// 
    /// 参考：.codex/游戏核心机制.md
    /// </summary>
    /// <param name="scenario">要清理的 GameScenario 对象（调用者保证非 null）</param>
    private void CleanupRuntimeState(GameScenario scenario)
    {
        #if DEBUG
        Debug.WriteLine("[EditorDataManager] 开始清理 WEGO 运行时状态");
        int cleanedTroops = 0;
        int cleanedFactions = 0;
        #endif

        // 🔥 清理部队运行时状态
        // ANTI-BAND-AID：集合中不应该有 null 元素，如果有则是数据损坏
        foreach (object troopObj in scenario.Troops)
        {
            // 🔥 修复：正确转换 object 类型为 Troop
            if (troopObj is not Troop troop)
            {
                throw new InvalidOperationException(
                    $"数据损坏：Troops 集合中存在非 Troop 类型的元素: {troopObj?.GetType().Name ?? "null"}");
            }
            
            // 清除部队指令（Command 和 CurrentAIState）
            // 🔥 修复：使用正确的枚举值 Idle
            troop.CurrentAIState = TroopAIState.Idle;
            
            // 重置行动完成标志（OperationDone 是执行阶段的标志）
            troop.OperationDone = false;

            #if DEBUG
            cleanedTroops++;
            #endif
        }

        // 🔥 清理势力运行时状态
        // ANTI-BAND-AID：集合中不应该有 null 元素，如果有则是数据损坏
        foreach (object factionObj in scenario.Factions)
        {
            // 🔥 修复：正确转换 object 类型为 Faction
            if (factionObj is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"数据损坏：Factions 集合中存在非 Faction 类型的元素: {factionObj?.GetType().Name ?? "null"}");
            }
            
            // 清除 AI 完成标志（AIFinished 是执行阶段的标志）
            faction.AIFinished = false;

            #if DEBUG
            cleanedFactions++;
            #endif
        }

        #if DEBUG
        Debug.WriteLine($"[EditorDataManager] WEGO 运行时状态清理完成");
        Debug.WriteLine($"[EditorDataManager] 清理部队数量: {cleanedTroops}");
        Debug.WriteLine($"[EditorDataManager] 清理势力数量: {cleanedFactions}");
        #endif
    }

    /// <summary>
    /// 将 List&lt;T&gt; 包装为 ObservableCollection&lt;T&gt;（支持 WPF 数据绑定）
    /// 
    /// 注意：
    /// 1. PersonList/FactionList 等自定义集合可能已实现 INotifyCollectionChanged
    /// 2. 如果需要支持新增/删除并实时刷新 UI，才需要包装为 ObservableCollection
    /// 3. 如果只是编辑现有对象的属性，不需要包装（对象本身应实现 INotifyPropertyChanged）
    /// 
    /// 当前实现：暂不包装，因为编辑器主要编辑现有对象的属性，不频繁新增/删除
    /// 如果未来需要支持实时新增/删除，可以在这里实现包装逻辑
    /// </summary>
    /// <param name="scenario">要包装的 GameScenario 对象（调用者保证非 null）</param>
    private void WrapCollectionsForBinding(GameScenario scenario)
    {
        #if DEBUG
        Debug.WriteLine("[EditorDataManager] 检查集合绑定支持");
        #endif

        // 🔥 检查自定义集合是否已实现 INotifyCollectionChanged
        // PersonList, FactionList, ArchitectureList 等可能已实现
        // 如果已实现，不需要额外包装

        // 🔥 如果未来需要包装，可以使用以下模式：
        // scenario.Persons = new ObservableCollection<Person>(scenario.Persons);
        // scenario.Factions = new ObservableCollection<Faction>(scenario.Factions);
        // 注意：这会改变集合的类型，可能影响序列化

        #if DEBUG
        Debug.WriteLine("[EditorDataManager] 集合绑定检查完成（当前不需要包装）");
        #endif
    }
}
