using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.Serialization.DTOs;
using WorldOfTheThreeKingdoms.Serialization.Phases;
using WorldOfTheThreeKingdoms.Tools;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Serialization Manager - Coordinates the entire serialization/deserialization workflow
    /// Implements the three-phase architecture: Save Data → Load Data → Link References
    /// </summary>
    public class SerializationManager
    {
        private readonly SaveDataPhase _savePhase;
        private readonly LoadDataPhase _loadPhase;
        private readonly LinkReferencesPhase _linkPhase;
        private readonly ValidationPhase _validationPhase;
        
        /// <summary>
        /// Constructor - initializes all phase dependencies
        /// </summary>
        public SerializationManager()
        {
            _savePhase = new SaveDataPhase();
            _loadPhase = new LoadDataPhase();
            _linkPhase = new LinkReferencesPhase();
            _validationPhase = new ValidationPhase();
        }
        
        /// <summary>
        /// Constructor with dependency injection (for testing)
        /// </summary>
        /// <param name="savePhase">Save data phase</param>
        /// <param name="loadPhase">Load data phase</param>
        /// <param name="linkPhase">Link references phase</param>
        /// <param name="validationPhase">Validation phase</param>
        public SerializationManager(
            SaveDataPhase savePhase,
            LoadDataPhase loadPhase,
            LinkReferencesPhase linkPhase,
            ValidationPhase validationPhase)
        {
            _savePhase = savePhase ?? throw new ArgumentNullException(nameof(savePhase));
            _loadPhase = loadPhase ?? throw new ArgumentNullException(nameof(loadPhase));
            _linkPhase = linkPhase ?? throw new ArgumentNullException(nameof(linkPhase));
            _validationPhase = validationPhase ?? throw new ArgumentNullException(nameof(validationPhase));
        }
        
        /// <summary>
        /// Save game scenario to file
        /// Implements the complete save workflow: Convert to DTO → Serialize to JSON → Compress → Write to file
        /// </summary>
        /// <param name="scenario">The game scenario to save</param>
        /// <param name="filePath">The file path to save to (should end with .sav.gz)</param>
        /// <exception cref="ArgumentNullException">Thrown when scenario or filePath is null</exception>
        /// <exception cref="SerializationException">Thrown when serialization fails</exception>
        public void SaveGame(GameScenario scenario, string filePath)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            try
            {
                // Phase 1: Convert game objects to DTOs
                GameScenarioDTO dto = null;
                try
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] Phase 1: 开始转换 DTO");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] scenario.Factions: {(scenario.Factions == null ? "null" : $"Count={scenario.Factions.Count}")}");
                    #endif
                    
                    dto = _savePhase.ConvertToDTO(scenario);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] ✅ DTO 转换完成");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] dto.Factions: {(dto.Factions == null ? "null" : $"Count={dto.Factions.Count}")}");
                    #endif
                }
                catch (Exception ex)
                {
                    DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"Phase 1 失败 (DTO转换): {ex.GetType().Name} - {ex.Message}");
                    DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"堆栈: {ex.StackTrace}");
                    throw;
                }
                
                // Phase 2: Serialize to JSON
                string json = null;
                try
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] Phase 2: 开始 JSON 序列化");
                    #endif
                    
                    // 🔥 修复：启用格式化输出，方便调试和检查存档
                    // 日期：2026-03-20
                    // 原因：单行 JSON 难以阅读和调试，启用格式化后可以快速检查数据结构
                    JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: true);
                    
                    // 🔥 关键修复：使用泛型重载以支持 AOT
                    // 日期：2026-03-20
                    // 问题：JsonSerializer.Serialize(object, Type, options) 在 AOT 模式下需要反射
                    // 解决：使用泛型重载 JsonSerializer.Serialize<T>(T, options)，让 Source Generator 生成代码
                    json = JsonSerializer.Serialize<GameScenarioDTO>(dto, options);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] ✅ JSON 序列化完成，长度: {json.Length} 字符");
                    
                    // 🔥 诊断：检查 JSON 中是否包含 Factions 数据
                    if (json.Contains("\"Factions\""))
                    {
                        int factionsIndex = json.IndexOf("\"Factions\"");
                        int previewStart = factionsIndex;
                        int previewLength = Math.Min(500, json.Length - factionsIndex);
                        string factionsPreview = json.Substring(previewStart, previewLength);
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] JSON 中的 Factions 片段:");
                        System.Diagnostics.Debug.WriteLine(factionsPreview);
                        
                        // 🔥 统计 Factions 数组中的元素数量
                        // 修复：在 $values 数组中统计，而不是在整个 Factions 对象中
                        int valuesIndex = json.IndexOf("\"$values\"", factionsIndex);
                        if (valuesIndex > 0)
                        {
                            int valuesArrayStart = json.IndexOf("[", valuesIndex);
                            if (valuesArrayStart > 0)
                            {
                                // 找到对应的闭合括号（简化处理：假设 Factions 后面紧跟其他字段）
                                int depth = 0;
                                int valuesArrayEnd = valuesArrayStart;
                                for (int i = valuesArrayStart; i < json.Length; i++)
                                {
                                    if (json[i] == '[') depth++;
                                    else if (json[i] == ']')
                                    {
                                        depth--;
                                        if (depth == 0)
                                        {
                                            valuesArrayEnd = i;
                                            break;
                                        }
                                    }
                                }
                                
                                if (valuesArrayEnd > valuesArrayStart)
                                {
                                    string valuesArrayContent = json.Substring(valuesArrayStart, valuesArrayEnd - valuesArrayStart + 1);
                                    // 统计 "ID": 的出现次数（每个对象都有 ID 字段）
                                    int factionCount = 0;
                                    int searchIndex = 0;
                                    while ((searchIndex = valuesArrayContent.IndexOf("\"ID\":", searchIndex)) >= 0)
                                    {
                                        factionCount++;
                                        searchIndex += 5;
                                    }
                                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] JSON 中 Factions.$values 数组包含 {factionCount} 个对象");
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] ⚠️ JSON 中未找到 'Factions' 字段！");
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager.SaveGame] JSON 前 1000 字符: {json.Substring(0, Math.Min(1000, json.Length))}");
                    }
                    #endif
                }
                catch (Exception ex)
                {
                    DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"Phase 2 失败 (JSON序列化): {ex.GetType().Name} - {ex.Message}");
                    DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"堆栈: {ex.StackTrace}");
                    throw;
                }
                
                // Phase 3: Compress and write to file
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                using (FileStream fileStream = File.Create(filePath))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                using (StreamWriter writer = new StreamWriter(gzipStream, System.Text.Encoding.UTF8))
                {
                    writer.Write(json);
                }
                
                // Phase 4: Write metadata file
                string metaFilePath = filePath + ".meta";
                WriteMetadata(scenario, metaFilePath);
            }
            catch (JsonException ex)
            {
                // JSON serialization error
                string errorMessage = $"JSON序列化失败. 文件: {filePath}";
                DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"{errorMessage} - {ex.Message}");
                throw new SerializationException(errorMessage, ex);
            }
            catch (IOException ex)
            {
                // File I/O error
                string errorMessage = $"文件写入失败. 文件: {filePath}";
                DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"{errorMessage} - {ex.Message}");
                throw new SerializationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                // Unexpected error
                string errorMessage = $"保存操作异常. 文件: {filePath}";
                DebugLogger.Error(DebugLogger.LogCategory.Serialization, $"{errorMessage} - {ex.Message}");
                throw new SerializationException(errorMessage, ex);
            }
        }
        
        /// <summary>
        /// Load game scenario from file
        /// Implements the complete load workflow: Detect format → Decompress → Deserialize → Load objects → Link references → Validate
        /// </summary>
        /// <param name="filePath">The file path to load from (.sav.gz or .bin)</param>
        /// <returns>The loaded game scenario</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null</exception>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
        /// <exception cref="DeserializationException">Thrown when deserialization fails</exception>
        public GameScenario LoadGame(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Save file not found: {filePath}", filePath);
            
            try
            {
                // Log start of load operation
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Starting load from: {filePath}");
                
                var swTotal = Stopwatch.StartNew();
                // 阶段 1: I/O 与 反序列化
                var swStep = Stopwatch.StartNew();
                
                // Phase 1: Detect file format
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 1: Detecting file format...");
                FileFormat format = DetectFileFormat(filePath);
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Detected format: {format}");
                
                GameScenarioDTO dto;
                
                // Phase 2: Load DTO based on format
                if (format == FileFormat.Binary)
                {
                    // Binary format no longer supported
                    throw new NotSupportedException("Binary format is no longer supported. Please use JSON format (.sav.gz)");
                }
                else // FileFormat.JsonGzip
                {
                    // New JSON + GZip format
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[SerializationManager.LoadGame] Phase 2: 开始解压和反序列化 JSON");
                    #endif
                    
                    using (FileStream fileStream = File.OpenRead(filePath))
                    using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
                        dto = JsonSerializer.Deserialize<GameScenarioDTO>(gzipStream, options);
                        
                        if (dto == null)
                        {
                            throw new DeserializationException($"反序列化返回null，文件可能损坏: {filePath}");
                        }
                    }
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.LoadGame] ✅ JSON 反序列化完成");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager.LoadGame] dto.Factions: {(dto.Factions == null ? "null" : $"Count={dto.Factions.Count}")}");
                    if (dto.Factions != null && dto.Factions.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager.LoadGame] 前3个势力: {string.Join(", ", dto.Factions.Take(3).Select(f => $"{f.Name}(ID:{f.ID})"))}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager.LoadGame] ⚠️ dto.Factions 为空或 null！");
                    }
                    #endif
                }
                
                // 🔥 2026-03-17 判断是否是新开剧本
                // LoadGame 方法用于读取存档，所以 isNewScenario = false
                bool isNewScenario = false;
                
                // Phase 3: Load data phase - create game objects from DTOs
                GameScenario scenario = _loadPhase.LoadFromDTO(dto, isNewScenario);
                
                // 🔥 2026-02-11 关键修复：清理旧序列化系统的循环引用
                if (scenario.Militaries != null && scenario.Militaries.Count > 0)
                {
                    int cleanedCount = 0;
                    foreach (Military military in scenario.Militaries.GetList())
                    {
                        if (military != null && military.ShelledMilitary != null)
                        {
                            military.ShelledMilitary = null;
                            cleanedCount++;
                        }
                    }
                    DebugLogger.LogIf(cleanedCount > 0, DebugLogger.LogLevel.Info, DebugLogger.LogCategory.Serialization, 
                        $"清理了 {cleanedCount} 个 ShelledMilitary 循环引用");
                }
                
                // 🔥 修复：加载 CommonData
                EnsureCommonDataLoaded(scenario);
                
                // 🔥 Phase 4.5: 手动加载 Faction 的 BaseMilitaryKinds 和 AvailableTechniques
                // 原因：OnDeserialized 在 CommonData 加载前触发，LoadFromString() 被跳过
                // 日期：2026-03-20
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 4.5: Loading Faction MilitaryKinds and Techniques...");
                
                // 🔥 Fail Fast：EnsureCommonDataLoaded 之后，CommonData 必须已加载
                if (scenario.GameCommonData?.AllMilitaryKinds == null)
                {
                    throw new InvalidOperationException(
                        "数据加载失败：EnsureCommonDataLoaded 后 GameCommonData.AllMilitaryKinds 仍为 null");
                }
                
                if (scenario.GameCommonData.AllTechniques == null)
                {
                    throw new InvalidOperationException(
                        "数据加载失败：EnsureCommonDataLoaded 后 GameCommonData.AllTechniques 仍为 null");
                }
                
                // 🔥 Fail Fast：Factions 集合必须存在
                if (scenario.Factions == null)
                {
                    throw new InvalidOperationException(
                        "数据损坏：scenario.Factions 为 null");
                }
                
                // 🧊 Cold Path：使用 for 循环避免迭代器分配
                var factionList = scenario.Factions.GetList();
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Phase 4.5: 找到 {factionList.Count} 个势力");
                
                for (int i = 0; i < factionList.Count; i++)
                {
                    Faction faction = factionList[i] as Faction;
                    
                    // 🔥 Fail Fast：集合中不应该有 null 元素
                    if (faction == null)
                    {
                        throw new InvalidOperationException(
                            $"数据损坏：Factions 集合中索引 {i} 的元素为 null");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager] Phase 4.5: 处理势力 {faction.Name}(ID:{faction.ID})");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager]   BaseMilitaryKindsString = '{faction.BaseMilitaryKindsString}'");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager]   AvailableTechniquesString = '{faction.AvailableTechniquesString}'");
                    
                    // 加载 BaseMilitaryKinds
                    if (!string.IsNullOrEmpty(faction.BaseMilitaryKindsString))
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   开始加载 BaseMilitaryKinds: '{faction.BaseMilitaryKindsString}'");
                        
                        // 🔥 诊断：检查 AllMilitaryKinds 是否包含所需的兵种
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   AllMilitaryKinds.MilitaryKinds.Count = {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                        
                        // 解析 BaseMilitaryKindsString 中的 ID
                        // 🔥 C# 12：使用集合表达式
                        string[] idStrings = faction.BaseMilitaryKindsString.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
                        foreach (string idStr in idStrings)
                        {
                            if (int.TryParse(idStr, out int mkId))
                            {
                                bool exists = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.ContainsKey(mkId);
                                System.Diagnostics.Debug.WriteLine($"[SerializationManager]     兵种 ID={mkId}: {(exists ? "存在" : "❌ 不存在")}");
                            }
                        }
                        #endif
                        
                        List<string> errors = faction.BaseMilitaryKinds.LoadFromString(
                            scenario.GameCommonData.AllMilitaryKinds, 
                            faction.BaseMilitaryKindsString);
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"[SerializationManager]   ✅ 势力 {faction.Name}(ID:{faction.ID}) 加载了 {faction.BaseMilitaryKinds.MilitaryKinds.Count} 个基础兵种");
                        
                        if (errors.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerializationManager]   ⚠️ 加载错误: {string.Join(", ", errors)}");
                        }
                        
                        // 🔥 诊断：列出实际加载的兵种
                        if (faction.BaseMilitaryKinds.MilitaryKinds.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerializationManager]   实际加载的兵种:");
                            foreach (var mk in faction.BaseMilitaryKinds.MilitaryKinds.Values)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SerializationManager]     - ID={mk.ID}, Name={mk.Name}");
                            }
                        }
                        #endif
                    }
                    else
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   ⚠️ BaseMilitaryKindsString 为空，跳过加载");
                        #endif
                    }
                    
                    // 加载 AvailableTechniques
                    if (!string.IsNullOrEmpty(faction.AvailableTechniquesString))
                    {
                        faction.AvailableTechniques.LoadFromString(
                            scenario.GameCommonData.AllTechniques, 
                            faction.AvailableTechniquesString);
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"[SerializationManager]   ✅ 势力 {faction.Name}(ID:{faction.ID}) 加载了 {faction.AvailableTechniques.Techniques.Count} 个可用技巧");
                    }
                }

                swStep.Stop();
                DebugLogger.Info(DebugLogger.LogCategory.Performance, $"读取文件 & JSON反序列化耗时: {swStep.ElapsedMilliseconds} ms");
                
                // 🔥 2026-03-17 修复：在 LinkReferences 之前初始化 CurrentPlayer（Fail Fast）
                // 原因：CurrentPlayer 是游戏运行的必要条件，如果为 null 会导致渲染器崩溃
                // 策略：Fail Fast - 如果无法设置 CurrentPlayer，立即抛出异常
                if (string.IsNullOrEmpty(scenario.CurrentPlayerID))
                {
                    throw new InvalidOperationException(
                        $"存档数据损坏：CurrentPlayerID 为空。" +
                        $"Factions.Count={scenario.Factions.Count}, " +
                        $"PlayerFactions.Count={scenario.PlayerFactions.Count}");
                }
                
                if (!int.TryParse(scenario.CurrentPlayerID, out int currentPlayerId))
                {
                    throw new InvalidOperationException(
                        $"存档数据损坏：CurrentPlayerID '{scenario.CurrentPlayerID}' 无法解析为整数。");
                }
                
                scenario.CurrentPlayer = scenario.Factions.GetGameObject(currentPlayerId) as Faction;
                
                if (scenario.CurrentPlayer == null)
                {
                    throw new InvalidOperationException(
                        $"存档数据损坏：CurrentPlayerID={currentPlayerId} 对应的势力不存在。" +
                        $"可用势力: {scenario.Factions.Count}, " +
                        $"玩家势力: {scenario.PlayerFactions.Count}");
                }
                
                DebugLogger.Info(DebugLogger.LogCategory.Serialization, 
                    $"CurrentPlayer 初始化成功: {scenario.CurrentPlayer.Name} (ID={currentPlayerId})");
                
                // Phase 4: Link references phase
                swStep.Restart();
                var lookupTables = _linkPhase.BuildLookupTables(scenario);
                swStep.Stop();
                DebugLogger.Info(DebugLogger.LogCategory.Performance, $"构建查找字典耗时: {swStep.ElapsedMilliseconds} ms");

                swStep.Restart();
                _linkPhase.LinkReferences(scenario, lookupTables);
                swStep.Stop();
                DebugLogger.Info(DebugLogger.LogCategory.Performance, $"连接引用耗时: {swStep.ElapsedMilliseconds} ms");
                
                // 🎯 新开剧本时：为所有势力武将初始化登庸蜜月期
                // 日期：2026-03-18
                // 原因：必须在 LinkReferencesPhase 之后执行，确保所有影响忠诚度的因素都已加载
                //       包括：LocationArchitecture、BelongedArchitecture.Mayor、Spouse、Father、Mother、Brothers、Treasures 等
                if (isNewScenario)
                {
                    System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine("║  [蜜月期初始化] 🔥 开始为新剧本初始化蜜月期           ║");
                    System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

                    int honeymoonInitCount = 0;
                    int honeymoonActiveCount = 0;
                    int nullFactionCount = 0;
                    
                    foreach (Person person in scenario.Persons)
                    {
                        // 只为有势力的武将初始化蜜月期
                        if (person.BelongedFaction != null)
                        {
                            int beforeLoyalty = person.Loyalty;
                            int beforeHoneymoon = person.HoneymoonMonths;
                            int beforeTemp = person.TempLoyaltyChange;
                            
                            person.InitializeHoneymoonForNewScenario();
                            honeymoonInitCount++;
                            
                            if (person.HoneymoonMonths > 0)
                            {
                                honeymoonActiveCount++;
                                #if DEBUG
                                if (honeymoonActiveCount <= 20)  // 打印前20个获得蜜月期的武将
                                {
                                    System.Diagnostics.Debug.WriteLine($"[蜜月期] {person.Name}(ID:{person.ID}): 忠诚度 {beforeLoyalty}→{person.Loyalty}, 蜜月期 {beforeHoneymoon}→{person.HoneymoonMonths}月, 临时忠诚 {beforeTemp}→{person.TempLoyaltyChange}, 势力={person.BelongedFaction.Name}");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            nullFactionCount++;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[蜜月期初始化] ✅ 完成：处理 {honeymoonInitCount} 人，其中 {honeymoonActiveCount} 人获得蜜月期保护，{nullFactionCount} 人无势力");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[蜜月期初始化] ⏭️ 跳过（读取存档，不是新开剧本）");
                }
                
                // Phase 5: Validation phase
                swStep.Restart();
                ValidationReport report = _validationPhase.Validate(scenario);
                
                // 🔥 只在有问题时输出验证报告
                if (report.HasErrors || report.HasWarnings || report.HasFixes)
                {
                    DebugLogger.Warning(DebugLogger.LogCategory.Validation, $"数据验证发现问题:\n{report.GetSummary()}");
                }
                
                swStep.Stop();
                DebugLogger.Info(DebugLogger.LogCategory.Performance, $"数据验证耗时: {swStep.ElapsedMilliseconds} ms");
                swTotal.Stop();
                System.Diagnostics.Debug.WriteLine($"[性能分析] === 总耗时: {swTotal.ElapsedMilliseconds} ms ===");
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Load completed successfully: {filePath}");
                return scenario;
            }
            catch (JsonException ex)
            {
                // JSON deserialization error
                string errorMessage = $"Failed to deserialize game data from JSON. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                
                // Try to recover from backup
                if (TryRecoverFromBackup(filePath, out GameScenarioDTO recoveredDto))
                {
                    System.Diagnostics.Debug.WriteLine("[SerializationManager] Successfully recovered from backup, continuing with load...");
                    
                    // Continue with the recovered DTO
                    GameScenario recoveredScenario = _loadPhase.LoadFromDTO(recoveredDto, isNewScenario: false);  // 错误恢复路径，总是存档
                    _linkPhase.LinkReferences(recoveredScenario);
                    ValidationReport recoveredReport = _validationPhase.Validate(recoveredScenario);
                    
                    if (recoveredReport.HasErrors || recoveredReport.HasWarnings || recoveredReport.HasFixes)
                    {
                        System.Diagnostics.Debug.WriteLine("[SerializationManager] Validation Report (Recovered):");
                        System.Diagnostics.Debug.WriteLine(recoveredReport.GetSummary());
                    }
                    
                    return recoveredScenario;
                }
                
                throw new DeserializationException(errorMessage, ex);
            }
            catch (InvalidDataException ex)
            {
                // GZip decompression error (corrupted file)
                string errorMessage = $"Failed to decompress save file. File may be corrupted: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                
                // Try to read as uncompressed JSON (fallback)
                if (TryReadUncompressed(filePath, out string json))
                {
                    System.Diagnostics.Debug.WriteLine("[SerializationManager] Successfully read as uncompressed JSON, continuing with load...");
                    
                    try
                    {
                        // Deserialize the uncompressed JSON
                        JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
                        GameScenarioDTO uncompressedDto = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
                        
                        if (uncompressedDto != null)
                        {
                            // Continue with the uncompressed DTO
                            GameScenario uncompressedScenario = _loadPhase.LoadFromDTO(uncompressedDto, isNewScenario: false);  // 错误恢复路径，总是存档
                            _linkPhase.LinkReferences(uncompressedScenario);
                            ValidationReport uncompressedReport = _validationPhase.Validate(uncompressedScenario);
                            
                            if (uncompressedReport.HasErrors || uncompressedReport.HasWarnings || uncompressedReport.HasFixes)
                            {
                                System.Diagnostics.Debug.WriteLine("[SerializationManager] Validation Report (Uncompressed):");
                                System.Diagnostics.Debug.WriteLine(uncompressedReport.GetSummary());
                            }
                            
                            return uncompressedScenario;
                        }
                    }
                    catch (Exception uncompressedEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager] Failed to load uncompressed JSON: {uncompressedEx.Message}");
                        // Fall through to throw CorruptedFileException
                    }
                }
                
                throw new CorruptedFileException(errorMessage, ex);
            }
            catch (IOException ex)
            {
                // File I/O error
                string errorMessage = $"Failed to read save file. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                throw new DeserializationException(errorMessage, ex);
            }
            catch (Exception ex) when (ex is not DeserializationException && ex is not CorruptedFileException)
            {
                // Unexpected error
                string errorMessage = $"Unexpected error during load operation. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                throw new DeserializationException(errorMessage, ex);
            }
        }
        
        /// <summary>
        /// Load game scenario from JSON file (for scenario files, not save files)
        /// 🔥 新增：统一剧本和存档的加载逻辑
        /// 日期：2026-03-16
        /// 原因：剧本文件已转换为新格式，与存档格式一致
        /// </summary>
        /// <param name="filePath">Path to the scenario JSON file</param>
        /// <returns>Loaded game scenario</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
        /// <exception cref="DeserializationException">Thrown when deserialization fails</exception>
        public GameScenario LoadScenario(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Scenario file not found: {filePath}", filePath);
            
            try
            {
                System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║  [SerializationManager.LoadScenario] 🔥 开始加载剧本        ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Loading scenario from: {filePath}");
                
                // Phase 1: Read and deserialize JSON (uncompressed)
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 1: Reading JSON file...");
                string json = File.ReadAllText(filePath);
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] JSON length: {json.Length} characters");
                
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 2: Deserializing JSON to DTO...");
                JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
                GameScenarioDTO dto = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
                
                if (dto == null)
                {
                    throw new DeserializationException($"反序列化返回null，文件可能损坏: {filePath}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] DTO deserialized successfully");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] - Architectures count: {dto.Architectures?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] - Facilities count: {dto.Facilities?.Count ?? 0}");
                
                // 🔥 2026-03-17 判断是否是新开剧本
                // 规则：文件路径包含 "Scenario" 或 "Content/Data/Scenario" 表示新开剧本
                //       文件路径包含 "Save" 表示读取存档
                bool isNewScenario = filePath.Contains("Scenario", StringComparison.OrdinalIgnoreCase) && 
                                    !filePath.Contains("Save", StringComparison.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] 🔥 isNewScenario={isNewScenario} (路径: {filePath})");
                
                // Phase 2: Load data phase - create game objects from DTOs
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 3: Loading data from DTO...");
                GameScenario scenario = _loadPhase.LoadFromDTO(dto, isNewScenario);
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Data loaded successfully");
                
                // 🔥 2026-03-18 关键修复：临时设置 Session.Current.Scenario
                // 原因：InitializeHoneymoonForNewScenario() 会访问 Person.Loyalty
                //       Loyalty 的 getter 需要访问 Session.Current.Scenario.GameCommonData
                // 时机：必须在 EnsureCommonDataLoaded() 之后、InitializeHoneymoonForNewScenario() 之前
                // 注意：Session.StartScenario() 会再次赋值，这里只是临时设置以便加载流程能够访问
                Session.Current.Scenario = scenario;
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ✅ 临时设置 Session.Current.Scenario（用于 Loyalty 计算）");
                
                // 🔥 关键修复：在 LinkReferences 之前先加载 CommonData
                // 日期：2026-03-18
                // 原因：Faction.OnDeserialized 在 LoadFromDTO 中触发，此时 CommonData 未加载
                //       需要在 CommonData 加载后，手动调用 LoadFromString() 来加载 BaseMilitaryKinds
                // Phase 3.5: Load CommonData
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 4: Loading CommonData...");
                EnsureCommonDataLoaded(scenario);
                
                // 🔥 Phase 4.5: 手动加载 Faction 的 BaseMilitaryKinds 和 AvailableTechniques
                // 原因：OnDeserialized 在 CommonData 加载前触发，LoadFromString() 被跳过
                // 日期：2026-03-18
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 4.5: Loading Faction MilitaryKinds and Techniques...");
                
                // 🔥 Fail Fast：EnsureCommonDataLoaded 之后，CommonData 必须已加载
                if (scenario.GameCommonData?.AllMilitaryKinds == null)
                {
                    throw new InvalidOperationException(
                        "数据加载失败：EnsureCommonDataLoaded 后 GameCommonData.AllMilitaryKinds 仍为 null");
                }
                
                if (scenario.GameCommonData.AllTechniques == null)
                {
                    throw new InvalidOperationException(
                        "数据加载失败：EnsureCommonDataLoaded 后 GameCommonData.AllTechniques 仍为 null");
                }
                
                // 🔥 Fail Fast：Factions 集合必须存在
                if (scenario.Factions == null)
                {
                    throw new InvalidOperationException(
                        "数据损坏：scenario.Factions 为 null");
                }
                
                // 🧊 Cold Path：使用 for 循环避免迭代器分配
                var factionList = scenario.Factions.GetList();
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Phase 4.5: 找到 {factionList.Count} 个势力");
                
                for (int i = 0; i < factionList.Count; i++)
                {
                    Faction faction = factionList[i] as Faction;
                    
                    // 🔥 Fail Fast：集合中不应该有 null 元素
                    if (faction == null)
                    {
                        throw new InvalidOperationException(
                            $"数据损坏：Factions 集合中索引 {i} 的元素为 null");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager] Phase 4.5: 处理势力 {faction.Name}(ID:{faction.ID})");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager]   BaseMilitaryKindsString = '{faction.BaseMilitaryKindsString}'");
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager]   AvailableTechniquesString = '{faction.AvailableTechniquesString}'");
                    
                    // 加载 BaseMilitaryKinds
                    if (!string.IsNullOrEmpty(faction.BaseMilitaryKindsString))
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   开始加载 BaseMilitaryKinds: '{faction.BaseMilitaryKindsString}'");
                        
                        // 🔥 诊断：检查 AllMilitaryKinds 是否包含所需的兵种
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   AllMilitaryKinds.MilitaryKinds.Count = {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                        
                        // 解析 BaseMilitaryKindsString 中的 ID
                        // 🔥 C# 12：使用集合表达式
                        string[] idStrings = faction.BaseMilitaryKindsString.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
                        foreach (string idStr in idStrings)
                        {
                            if (int.TryParse(idStr, out int mkId))
                            {
                                bool exists = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.ContainsKey(mkId);
                                System.Diagnostics.Debug.WriteLine($"[SerializationManager]     兵种 ID={mkId}: {(exists ? "存在" : "❌ 不存在")}");
                            }
                        }
                        #endif
                        
                        List<string> errors = faction.BaseMilitaryKinds.LoadFromString(
                            scenario.GameCommonData.AllMilitaryKinds, 
                            faction.BaseMilitaryKindsString);
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"[SerializationManager]   ✅ 势力 {faction.Name}(ID:{faction.ID}) 加载了 {faction.BaseMilitaryKinds.MilitaryKinds.Count} 个基础兵种");
                        
                        if (errors.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerializationManager]   ⚠️ 加载错误: {string.Join(", ", errors)}");
                        }
                        
                        // 🔥 诊断：列出实际加载的兵种
                        if (faction.BaseMilitaryKinds.MilitaryKinds.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerializationManager]   实际加载的兵种:");
                            foreach (var mk in faction.BaseMilitaryKinds.MilitaryKinds.Values)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SerializationManager]     - ID={mk.ID}, Name={mk.Name}");
                            }
                        }
                        #endif
                    }
                    else
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager]   ⚠️ BaseMilitaryKindsString 为空，跳过加载");
                        #endif
                    }
                    
                    // 加载 AvailableTechniques
                    if (!string.IsNullOrEmpty(faction.AvailableTechniquesString))
                    {
                        faction.AvailableTechniques.LoadFromString(
                            scenario.GameCommonData.AllTechniques, 
                            faction.AvailableTechniquesString);
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"[SerializationManager]   ✅ 势力 {faction.Name}(ID:{faction.ID}) 加载了 {faction.AvailableTechniques.Techniques.Count} 个可用技巧");
                    }
                }
                
                // 🔥 Phase 4.6: 手动加载 Biography 的 MilitaryKinds
                // 原因：Biography.MilitaryKinds 需要从 MilitaryKindsString 加载
                // 日期：2026-03-18
                // 修复：移除防御性空检查，改为 Fail Fast
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 4.6: Loading Biography MilitaryKinds...");
                
                // 🔥 Fail Fast：如果 AllBiographies 为 null，说明数据加载流程有严重错误
                if (scenario.AllBiographies == null)
                {
                    throw new InvalidOperationException(
                        "[SerializationManager] Phase 4.6 失败：scenario.AllBiographies 为 null，数据加载流程错误");
                }
                
                // 🔥 Cold Path：可读性优先，直接遍历 Dictionary
                foreach (var kvp in scenario.AllBiographies.Biographys)
                {
                    var biography = kvp.Value;
                    
                    // 🔥 Fail Fast：如果集合中有 null 元素，说明数据损坏
                    if (biography == null)
                    {
                        throw new InvalidOperationException(
                            $"[SerializationManager] Phase 4.6 失败：Biography 集合中存在 null 元素（ID={kvp.Key}）");
                    }
                    
                    if (!string.IsNullOrEmpty(biography.MilitaryKindsString))
                    {
                        var errors = biography.MilitaryKinds.LoadFromString(
                            scenario.GameCommonData.AllMilitaryKinds, 
                            biography.MilitaryKindsString);
                        
                        // 🔥 Cold Path：可读性优先，使用 foreach
                        foreach (var error in errors)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[SerializationManager] ⚠️ Biography {biography.ID} ({biography.Name}) MilitaryKinds 解析错误: {error}");
                        }
                        
                        #if DEBUG
                        if (biography.ID <= 3)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[SerializationManager] Biography {biography.Name}(ID:{biography.ID}) 加载了 {biography.MilitaryKinds.MilitaryKinds.Count} 个兵种");
                        }
                        #endif
                    }
                }
                
                // Phase 4: Link references phase
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 5: Building lookup tables...");
                var lookupTables = _linkPhase.BuildLookupTables(scenario);
                
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 6: Linking references...");
                _linkPhase.LinkReferences(scenario, lookupTables);
                
                // 🎯 新开剧本时：为所有势力武将初始化登庸蜜月期
                // 日期：2026-03-18
                // 原因：必须在 LinkReferencesPhase 之后执行，确保所有影响忠诚度的因素都已加载
                //       包括：LocationArchitecture、BelongedArchitecture.Mayor、Spouse、Father、Mother、Brothers、Treasures 等
                if (isNewScenario)
                {
                    System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine("║  [蜜月期初始化] 🔥 开始为新剧本初始化蜜月期           ║");
                    System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

                    int honeymoonInitCount = 0;
                    int honeymoonActiveCount = 0;
                    int nullFactionCount = 0;
                    
                    foreach (Person person in scenario.Persons)
                    {
                        // 只为有势力的武将初始化蜜月期
                        if (person.BelongedFaction != null)
                        {
                            int beforeLoyalty = person.Loyalty;
                            int beforeHoneymoon = person.HoneymoonMonths;
                            int beforeTemp = person.TempLoyaltyChange;
                            
                            person.InitializeHoneymoonForNewScenario();
                            honeymoonInitCount++;
                            
                            if (person.HoneymoonMonths > 0)
                            {
                                honeymoonActiveCount++;
                                #if DEBUG
                                if (honeymoonActiveCount <= 20)  // 打印前20个获得蜜月期的武将
                                {
                                    System.Diagnostics.Debug.WriteLine($"[蜜月期] {person.Name}(ID:{person.ID}): 忠诚度 {beforeLoyalty}→{person.Loyalty}, 蜜月期 {beforeHoneymoon}→{person.HoneymoonMonths}月, 临时忠诚 {beforeTemp}→{person.TempLoyaltyChange}, 势力={person.BelongedFaction.Name}");
                                }
                                #endif
                            }
                        }
                        else
                        {
                            nullFactionCount++;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[蜜月期初始化] ✅ 完成：处理 {honeymoonInitCount} 人，其中 {honeymoonActiveCount} 人获得蜜月期保护，{nullFactionCount} 人无势力");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[蜜月期初始化] ⏭️ 跳过（读取存档，不是新开剧本）");
                }
                
                // Phase 5: Validation phase
                System.Diagnostics.Debug.WriteLine("[SerializationManager] Phase 7: Validating data...");
                ValidationReport report = _validationPhase.Validate(scenario);
                
                if (report.HasErrors || report.HasWarnings || report.HasFixes)
                {
                    DebugLogger.Warning(DebugLogger.LogCategory.Validation, $"剧本数据验证发现问题:\n{report.GetSummary()}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Scenario loaded successfully: {filePath}");
                System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║  [SerializationManager.LoadScenario] ✅ 加载完成            ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
                return scenario;
            }
            catch (JsonException ex)
            {
                string errorMessage = $"Failed to deserialize scenario from JSON. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                throw new DeserializationException(errorMessage, ex);
            }
            catch (IOException ex)
            {
                string errorMessage = $"Failed to read scenario file. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                throw new DeserializationException(errorMessage, ex);
            }
            catch (Exception ex) when (ex is not DeserializationException)
            {
                string errorMessage = $"Unexpected error during scenario load. File: {filePath}";
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                throw new DeserializationException(errorMessage, ex);
            }
        }
        
        /// <summary>
        /// Write metadata file for quick save list loading
        /// Creates a small .meta file with essential save information
        /// </summary>
        /// <param name="scenario">The game scenario to extract metadata from</param>
        /// <param name="metaFilePath">The path to write the metadata file to</param>
        private void WriteMetadata(GameScenario scenario, string metaFilePath)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            if (string.IsNullOrWhiteSpace(metaFilePath))
                throw new ArgumentNullException(nameof(metaFilePath));
            
            try
            {
                // Create metadata object
                SaveMetadata metadata = new SaveMetadata
                {
                    ScenarioName = scenario.ScenarioTitle ?? "Unknown Scenario",
                    SaveTime = DateTime.Now,
                    CurrentTurn = scenario.DaySince,
                    GameVersion = "1.0.0", // TODO: Get actual game version
                    PlayerFactionID = int.TryParse(scenario.CurrentPlayerID, out int playerId) ? playerId : -1,
                    MOD = scenario.MOD ?? string.Empty
                };
                
                // Get player faction name if available
                int playerFactionId = int.TryParse(scenario.CurrentPlayerID, out int parsedId) ? parsedId : -1;
                if (playerFactionId > 0 && scenario.Factions != null)
                {
                    Faction playerFaction = scenario.Factions.GetGameObject(playerFactionId) as Faction;
                    if (playerFaction != null)
                    {
                        metadata.PlayerFactionName = playerFaction.Name;
                    }
                }
                
                // Get current game date
                if (scenario.Date != null)
                {
                    metadata.Year = scenario.Date.Year;
                    metadata.Month = scenario.Date.Month;
                    metadata.Day = scenario.Date.Day;
                }
                
                // Get game time
                metadata.GameTime = scenario.GameTime;
                
                // 🔥 关键修复：使用 Source Generator 上下文（AOT 兼容）
                // 日期：2026-03-21
                // 原因：AOT 环境下禁用了反射序列化，必须使用 UnifiedSerializationContext
                string metadataJson = JsonSerializer.Serialize(metadata, UnifiedSerializationContext.GetMetadataOptions());
                
                // Ensure the metadata is small (< 1KB as per requirement)
                if (metadataJson.Length > 1024)
                {
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager] WARNING: Metadata file is larger than 1KB ({metadataJson.Length} bytes)");
                }
                
                // Write to file
                File.WriteAllText(metaFilePath, metadataJson, System.Text.Encoding.UTF8);
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Metadata written successfully: {metaFilePath} ({metadataJson.Length} bytes)");
            }
            catch (Exception ex)
            {
                // Log error but don't fail the save operation
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] WARNING: Failed to write metadata file: {metaFilePath}");
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Exception: {ex.Message}");
                // Don't throw - metadata is optional
            }
        }
        
        /// <summary>
        /// Detect the file format based on file extension
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>The detected file format</returns>
        private FileFormat DetectFileFormat(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Check for binary format
            if (extension == ".bin")
            {
                return FileFormat.Binary;
            }
            
            // Check for JSON + GZip format
            // Support both .sav.gz and .gz extensions
            if (extension == ".gz" || filePath.EndsWith(".sav.gz", StringComparison.OrdinalIgnoreCase))
            {
                return FileFormat.JsonGzip;
            }
            
            // Default to JSON + GZip for unknown extensions
            // This allows for flexibility in file naming
            System.Diagnostics.Debug.WriteLine($"[SerializationManager] WARNING: Unknown file extension '{extension}', assuming JSON + GZip format");
            return FileFormat.JsonGzip;
        }
        
        /// <summary>
        /// Try to recover from a backup file
        /// Looks for backup files with common naming patterns (.bak, .backup, etc.)
        /// </summary>
        /// <param name="filePath">The original file path that failed to load</param>
        /// <param name="dto">The recovered DTO if successful</param>
        /// <returns>True if recovery was successful, false otherwise</returns>
        private bool TryRecoverFromBackup(string filePath, out GameScenarioDTO dto)
        {
            dto = null;
            
            try
            {
                // Common backup file patterns
                string[] backupPatterns = new[]
                {
                    filePath + ".bak",
                    filePath + ".backup",
                    Path.ChangeExtension(filePath, ".bak"),
                    Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".backup" + Path.GetExtension(filePath))
                };
                
                foreach (string backupPath in backupPatterns)
                {
                    if (File.Exists(backupPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager] Found backup file: {backupPath}");
                        System.Diagnostics.Debug.WriteLine($"[SerializationManager] Attempting to recover from backup...");
                        
                        try
                        {
                            // Try to load the backup file
                            using (FileStream fileStream = File.OpenRead(backupPath))
                            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                            {
                                JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
                                dto = JsonSerializer.Deserialize<GameScenarioDTO>(gzipStream, options);
                                
                                if (dto != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SerializationManager] Successfully recovered from backup: {backupPath}");
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerializationManager] Failed to recover from backup {backupPath}: {ex.Message}");
                            // Continue to next backup pattern
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] No valid backup files found for: {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Error during backup recovery: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Try to read file as uncompressed JSON (fallback for corrupted GZip)
        /// This handles cases where the file might have been saved without compression
        /// </summary>
        /// <param name="filePath">The file path to read</param>
        /// <param name="json">The JSON string if successful</param>
        /// <returns>True if the file was successfully read as uncompressed JSON, false otherwise</returns>
        private bool TryReadUncompressed(string filePath, out string json)
        {
            json = null;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Attempting to read as uncompressed JSON: {filePath}");
                
                // Try to read the file as plain text
                json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                
                // Validate that it's actually JSON by trying to parse it
                JsonSerializerOptions options = GameJsonContext.GetDefaultOptions(indented: false);
                GameScenarioDTO testDto = JsonSerializer.Deserialize<GameScenarioDTO>(json, options);
                
                if (testDto != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SerializationManager] Successfully read as uncompressed JSON: {filePath}");
                    return true;
                }
                
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] File is not valid JSON: {filePath}");
                json = null;
                return false;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] File is not valid JSON: {ex.Message}");
                json = null;
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerializationManager] Failed to read as uncompressed JSON: {ex.Message}");
                json = null;
                return false;
            }
        }
        
        /// <summary>
        /// Ensure CommonData is loaded for the scenario
        /// </summary>
        private void EnsureCommonDataLoaded(GameScenario scenario)
        {
            // 🔥 关键修复：确保 CommonData 完整加载
            // 日期：2026-03-20
            // 问题：只检查 ArchitectureKinds 不够，必须检查所有关键集合
            // 原因：CommonData.Current 可能部分加载，导致某些集合为空
            
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario), "[EnsureCommonDataLoaded] scenario 不能为 null");
            }
            
            // 🔥 关键：检查 GameCommonData 是否完整加载（检查所有关键集合）
            bool needsCommonData = scenario.GameCommonData == null ||
                                   scenario.GameCommonData.AllArchitectureKinds?.ArchitectureKinds == null ||
                                   scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count == 0 ||
                                   scenario.GameCommonData.AllMilitaryKinds?.MilitaryKinds == null ||
                                   scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count == 0 ||
                                   scenario.GameCommonData.AllTechniques?.Techniques == null ||
                                   scenario.GameCommonData.AllTechniques.Techniques.Count == 0;
            
            if (needsCommonData)
            {
                System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] GameCommonData 缺失或不完整，从 CommonData.Current 加载");
                
                // 🔥 诊断日志：显示当前状态（允许使用 ?? 因为我们正在诊断问题）
                System.Diagnostics.Debug.WriteLine($"  - GameCommonData: {(scenario.GameCommonData != null ? "存在" : "null")}");
                if (scenario.GameCommonData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds: {scenario.GameCommonData.AllArchitectureKinds?.ArchitectureKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds: {scenario.GameCommonData.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllTechniques: {scenario.GameCommonData.AllTechniques?.Techniques?.Count ?? 0}");
                }
                
                // 确保 CommonData.Current 已加载
                if (CommonData.Current == null || !CommonData.CurrentReady)
                {
                    System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] CommonData.Current 未就绪，尝试同步加载");
                    
                    // 🔥 关键修复：直接同步加载，不等待异步初始化
                    // 原因：SerializationManager.LoadScenario 必须同步完成，不能依赖异步任务
                    string commonDataPath = @"Content\Data\Common\CommonData.json";
                    if (!System.IO.File.Exists(commonDataPath))
                    {
                        throw new System.IO.FileNotFoundException($"CommonData.json 文件不存在: {commonDataPath}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] 从文件加载: {commonDataPath}");
                    
                    // 🔥 关键修复：CommonData.json 不包含 $id/$ref 引用，必须使用不带 ReferenceHandler.Preserve 的选项
                    // 原因：类似剧本文件，CommonData.json 由编辑器生成，不含引用标记
                    //       使用 ReferenceHandler.Preserve 会导致反序列化失败（集合为空）
                    // 日期：2026-03-20
                    string json = System.IO.File.ReadAllText(commonDataPath);
                    System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] JSON 文件大小: {json.Length} 字符");
                    System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] JSON 前100字符: {(json.Length > 100 ? json.Substring(0, 100) : json)}");
                    
                    JsonSerializerOptions options = GameJsonContext.GetScenarioFileOptions(); // 使用剧本文件选项（无 ReferenceHandler.Preserve）
                    System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] 使用 GetScenarioFileOptions 进行反序列化");
                    
                    CommonData.Current = JsonSerializer.Deserialize<CommonData>(json, options);
                    
                    if (CommonData.Current == null)
                    {
                        throw new InvalidOperationException("CommonData 反序列化返回 null");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] 反序列化完成，检查集合...");
                    System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds: {CommonData.Current.AllArchitectureKinds?.ArchitectureKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds: {CommonData.Current.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllTechniques: {CommonData.Current.AllTechniques?.Techniques?.Count ?? 0}");
                    
                    // 🔥 关键：必须调用 ProcessCommonData 来初始化委托和关联
                    System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] 调用 ProcessCommonData...");
                    GameScenario.ProcessCommonData(CommonData.Current);
                    
                    CommonData.CurrentReady = true;
                    System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] ✅ CommonData 同步加载成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] CommonData.Current 已就绪");
                    System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds: {CommonData.Current.AllArchitectureKinds?.ArchitectureKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds: {CommonData.Current.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"  - AllTechniques: {CommonData.Current.AllTechniques?.Techniques?.Count ?? 0}");
                }
                
                // 🔥 Fail Fast：验证 CommonData.Current 完整性
                if (CommonData.Current == null)
                {
                    throw new InvalidOperationException("[EnsureCommonDataLoaded] CommonData.Current 为 null");
                }
                
                if (CommonData.Current.AllArchitectureKinds?.ArchitectureKinds == null ||
                    CommonData.Current.AllArchitectureKinds.ArchitectureKinds.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"[EnsureCommonDataLoaded] CommonData.Current.AllArchitectureKinds 为空 (Count={CommonData.Current.AllArchitectureKinds?.ArchitectureKinds?.Count ?? 0})");
                }
                
                if (CommonData.Current.AllMilitaryKinds?.MilitaryKinds == null ||
                    CommonData.Current.AllMilitaryKinds.MilitaryKinds.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"[EnsureCommonDataLoaded] CommonData.Current.AllMilitaryKinds 为空 (Count={CommonData.Current.AllMilitaryKinds?.MilitaryKinds?.Count ?? 0})");
                }
                
                // 直接赋值 CommonData.Current 到 scenario.GameCommonData
                scenario.GameCommonData = CommonData.Current;
                scenario.UsingOwnCommonData = false;
                
                System.Diagnostics.Debug.WriteLine($"[EnsureCommonDataLoaded] ✅ 已加载 CommonData");
                System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds.Count: {scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count}");
                System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds.Count: {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                System.Diagnostics.Debug.WriteLine($"  - AllTechniques.Count: {scenario.GameCommonData.AllTechniques.Techniques.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[EnsureCommonDataLoaded] GameCommonData 已存在且完整，无需加载");
                System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds.Count: {scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count}");
                System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds.Count: {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                System.Diagnostics.Debug.WriteLine($"  - AllTechniques.Count: {scenario.GameCommonData.AllTechniques.Techniques.Count}");
            }
        }
    }
    
    /// <summary>
    /// File format enumeration
    /// </summary>
    public enum FileFormat
    {
        /// <summary>
        /// Legacy binary format (.bin)
        /// </summary>
        Binary,
        
        /// <summary>
        /// New JSON + GZip format (.sav.gz)
        /// </summary>
        JsonGzip
    }
    
    /// <summary>
    /// Custom exception for serialization errors
    /// </summary>
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message)
        {
        }
        
        public SerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Custom exception for deserialization errors
    /// </summary>
    public class DeserializationException : Exception
    {
        public DeserializationException(string message) : base(message)
        {
        }
        
        public DeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Custom exception for corrupted file errors
    /// </summary>
    public class CorruptedFileException : Exception
    {
        public CorruptedFileException(string message) : base(message)
        {
        }
        
        public CorruptedFileException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
