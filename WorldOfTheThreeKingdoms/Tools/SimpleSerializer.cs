using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Platforms;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;
using GameManager;
using Tools;

namespace WorldOfTheThreeKingdoms.Tools
{
    /// <summary>
    /// 序列化对象器 - 完全迁移到 System.Text.Json 的 AOT 兼容版本
    /// </summary>
    public class SimpleSerializer
    {
        // 🔥 条件编译：Release 模式下禁用所有调试日志
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static JsonTypeInfo<T> ResolveTypeInfo<T>(JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(T));
            if (typeInfo is not JsonTypeInfo<T> typedTypeInfo)
            {
                throw new InvalidOperationException($"AOT metadata not registered for type: {typeof(T).FullName}");
            }

            return typedTypeInfo;
        }

        private static T DeserializeRequired<T>(string json, JsonSerializerOptions options)
        {
            T? result = System.Text.Json.JsonSerializer.Deserialize(json, ResolveTypeInfo<T>(options));
            if (result == null)
            {
                throw new InvalidOperationException($"System.Text.Json 反序列化 {typeof(T).Name} 返回 null");
            }

            return result;
        }
        
        #region XMLSerializer
        [RequiresUnreferencedCode("XML serialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML serialization may require runtime code generation and is not NativeAOT-safe.")]
        public static string SerializeXML<T>(T t)
        {
            using (StringWriter sw = new StringWriter())
            {
                lock (Platform.SerializerLock)
                {
                    Platform.SessionActive = false;
                    XmlSerializer xz = new XmlSerializer(t.GetType());
                    xz.Serialize(sw, t);
                    Platform.SessionActive = true;
                }
                return sw.ToString();
            }
        }

        [RequiresUnreferencedCode("XML deserialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML deserialization may require runtime code generation and is not NativeAOT-safe.")]
        public static T DeserializeXML<T>(string s)
        {
            using (StringReader sr = new StringReader(s))
            {
                XmlSerializer xz = new XmlSerializer(typeof(T));
                try
                {
                    lock (Platform.SerializerLock)
                    {
                        Platform.SessionActive = false;
                        var o = (T)xz.Deserialize(sr);
                        Platform.SessionActive = true;
                        return o;
                    }
                }
                catch (Exception ex)
                {
                    return default(T);
                }
            }
        }

        [RequiresUnreferencedCode("XML serialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML serialization may require runtime code generation and is not NativeAOT-safe.")]
        public static void SerializeXML<T>(T t, string file)
        {
            string xml = SerializeXML(t);
            Platform.Current.SaveUserFile(file, xml);
        }

        [RequiresUnreferencedCode("XML deserialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML deserialization may require runtime code generation and is not NativeAOT-safe.")]
        public static T DeserializeXMLFile<T>(string file, bool isUserFile)
        {
            string content = isUserFile ? Platform.Current.GetUserText(file) : Platform.Current.LoadText(file);
            return DeserializeXML<T>(content);
        }
        #endregion

        #region JsonSerializer - System.Text.Json Implementation

        /// <summary>
        /// 使用 System.Text.Json 序列化对象，提供 AOT 兼容性
        /// </summary>
        /// <param name="t">要序列化的对象</param>
        /// <param name="zip">是否压缩</param>
        /// <param name="Indented">是否格式化</param>
        /// <param name="Net">遗留参数（忽略）</param>
        public static string SerializeJson<T>(T t, bool zip = false, bool Indented = false, bool Net = false)
        {
            string result = null;
            lock (Platform.SerializerLock)
            {
                try
                {
                    // 获取序列化选项
                    var options = GameJsonContext.GetDefaultOptions(Indented);
                    
                    result = System.Text.Json.JsonSerializer.Serialize(t, ResolveTypeInfo<T>(options));
                    
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            
            if (zip)
            {
                result = result.GZipCompressString();
            }
            return result;
        }

        /// <summary>
        /// 🔥 根本修复：使用 System.Text.Json 反序列化对象，移除 Newtonsoft.Json 回退逻辑
        /// </summary>
        /// <param name="s">JSON 字符串</param>
        /// <param name="zip">是否压缩</param>
        /// <param name="Net">遗留参数（忽略）</param>
        /// <summary>
        /// 使用指定 options 反序列化，供需要覆盖默认选项的调用方使用（如剧本文件加载）
        /// </summary>
        public static T DeserializeJson<T>(string s, JsonSerializerOptions options, bool zip = false)
        {
            if (zip) s = s.GZipDecompressString();

            lock (Platform.SerializerLock)
            {
                T result = DeserializeRequired<T>(s, options);
                
                // 🔥 只对 GameScenario 调用 FixSharedReferences
                if (result is GameScenario)
                {
                    FixSharedReferences(result);
                }
                
                return result;
            }
        }

        public static T DeserializeJson<T>(string s, bool zip = false, bool Net = false)
        {
            if (zip)
            {
                s = s.GZipDecompressString();
            }

            T result = default(T);
            
            try
            {
                lock (Platform.SerializerLock)
                {
                    
                    // 🔥 根本修复：使用 GameJsonContext 的配置选项
                    var options = GameJsonContext.GetDefaultOptions();
                    
                    result = DeserializeRequired<T>(s, options);
                    
                    // 🔥 只对 GameScenario 调用 FixSharedReferences
                    if (result is GameScenario)
                    {
                        FixSharedReferences(result);
                    }
                    
                    if (result != null)
                    {
                        
                        // 🔥 根本修复：验证 CommonData 完整性
                        if (result is global::GameObjects.CommonData commonData)
                        {
                            ValidateDeserializedCommonData(commonData);
                        }
                        
                        return result;
                    }
                    else
                    {
                        throw new InvalidOperationException($"System.Text.Json 反序列化 {typeof(T).Name} 返回 null");
                    }
                }
            }
            catch (Exception ex)
            {
                
                // 🔥 如果是 AOT 类型注册问题，提供明确的错误信息
                if (ex.Message.Contains("JsonTypeInfo metadata") || ex.Message.Contains("TypeInfoResolver"))
                {
                    string missingType = ExtractMissingTypeFromError(ex.Message);
                    throw new InvalidOperationException($"AOT 类型注册缺失: {missingType}。请在 GameJsonContext 中添加 [JsonSerializable(typeof({missingType}))] 注册。", ex);
                }
                
                throw;
            }
        }

        /// <summary>
        /// 从错误信息中提取缺失的类型名称
        /// </summary>
        private static string ExtractMissingTypeFromError(string errorMessage)
        {
            try
            {
                // 提取类型信息，例如从 "JsonTypeInfo metadata for type 'System.Collections.Generic.Dictionary`2[...]'" 中提取类型
                var startIndex = errorMessage.IndexOf("for type '");
                if (startIndex >= 0)
                {
                    startIndex += "for type '".Length;
                    var endIndex = errorMessage.IndexOf("'", startIndex);
                    if (endIndex > startIndex)
                    {
                        return errorMessage.Substring(startIndex, endIndex - startIndex);
                    }
                }
                return "未知类型";
            }
            catch (Exception ex)
            {
                return "未知类型";
            }
        }

        public static bool SerializeJsonFile<T>(T t, string file, bool zip = false, bool Net = false, bool fullPathProvided = false)
        {
            try
            {
                // 🔥 在序列化前填充ID列表
                PrepareForSerialization(t);
                
                string json = SerializeJson(t, zip, true); // Always use indented for file output
                Platform.Current.SaveUserFile(file, json, fullPathProvided);
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                WebTools.TakeWarnMsg("序列用户对象失败:" + file, "SerializeJson:" + t.GetType(), ex);
                return false;
#endif
            }
        }
        
        /// <summary>
        /// 在序列化前填充所有ID列表
        /// </summary>
        private static void PrepareForSerialization<T>(T obj)
        {
            if (obj == null) return;
            
            // 如果是 GameScenario，填充所有集合的ID列表
            if (obj is global::GameObjects.GameScenario scenario)
            {
                
                // 填充 Faction 的ID列表
                if (scenario.Factions != null)
                {
                    foreach (global::GameObjects.Faction faction in scenario.Factions.GetList())
                    {
                        if (faction == null) continue;
                        
                        // ArchitectureIDs
                        if (faction.Architectures != null)
                        {
                            faction.ArchitectureIDs = faction.Architectures.GetList().GameObjects
                                .Cast<global::GameObjects.Architecture>()
                                .Select(a => a.ID)
                                .ToList();
                        }
                        
                        // TroopIDs
                        if (faction.Troops != null)
                        {
                            faction.TroopIDs = faction.Troops.GetList().GameObjects
                                .Cast<global::GameObjects.Troop>()
                                .Select(t => t.ID)
                                .ToList();
                        }
                        
                        // LegionIDs
                        if (faction.Legions != null)
                        {
                            faction.LegionIDs = faction.Legions.GetList().GameObjects
                                .Cast<global::GameObjects.Legion>()
                                .Select(l => l.ID)
                                .ToList();
                        }
                        
                        // SectionIDs
                        if (faction.Sections != null)
                        {
                            faction.SectionIDs = faction.Sections.GetList().GameObjects
                                .Cast<global::GameObjects.Section>()
                                .Select(s => s.ID)
                                .ToList();
                        }
                        
                        // PersonIDs
                        if (faction.Persons != null)
                        {
                            faction.PersonIDs = faction.Persons.GetList().GameObjects
                                .Cast<global::GameObjects.Person>()
                                .Select(p => p.ID)
                                .ToList();
                        }
                        
                        // MilitaryIDs
                        if (faction.Militaries != null)
                        {
                            faction.MilitaryIDs = faction.Militaries.GetList().GameObjects
                                .Cast<global::GameObjects.Military>()
                                .Select(m => m.ID)
                                .ToList();
                        }
                    }
                    
                }
                
                // 填充 Section 的ID列表
                if (scenario.Sections != null)
                {
                    foreach (global::GameObjects.Section section in scenario.Sections.GetList())
                    {
                        if (section == null) continue;
                        
                        // ArchitectureIDs
                        if (section.Architectures != null)
                        {
                            section.ArchitectureIDs = section.Architectures.GetList().GameObjects
                                .Cast<global::GameObjects.Architecture>()
                                .Select(a => a.ID)
                                .ToList();
                        }
                    }
                    
                }
                
                // 填充 Legion 的ID列表
                if (scenario.Legions != null)
                {
                    foreach (global::GameObjects.Legion legion in scenario.Legions.GetList())
                    {
                        if (legion == null) continue;
                        
                        // TroopIDs
                        if (legion.Troops != null)
                        {
                            legion.TroopIDs = legion.Troops.GetList().GameObjects
                                .Cast<global::GameObjects.Troop>()
                                .Select(t => t.ID)
                                .ToList();
                        }
                    }
                    
                }
                
            }
        }

        public static T DeserializeJsonFile<T>(string file, bool isUserFile)
        {
            return DeserializeJsonFile<T>(file, isUserFile, false, false);
        }

        public static T DeserializeJsonFile<T>(string file, bool isUserFile, bool zip)
        {
            return DeserializeJsonFile<T>(file, isUserFile, zip, false);
        }

        public static T DeserializeJsonFile<T>(string file, bool isUserFile, bool zip, bool Net)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 开始反序列化: {file}");
                
                // 🔥 修复：根据文件扩展名自动判断是否需要解压
                // .sav.gz 文件需要解压，其他 .json 文件不需要
                bool needsDecompression = file.EndsWith(".sav.gz", StringComparison.OrdinalIgnoreCase) || 
                                         file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
                
                if (needsDecompression && !zip)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 检测到压缩文件，自动启用解压: {file}");
                    zip = true;
                }
                
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 文件类型: {(isUserFile ? "用户文件" : "内容文件")}, 解压: {zip}");
                
                string content = isUserFile ? Platform.Current.GetUserText(file) : Platform.Current.LoadText(file);
                
                // 🔥 修复：检查原始内容是否为空
                if (string.IsNullOrEmpty(content))
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] ❌ 文件内容为空: {file}");
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] isUserFile={isUserFile}");
                    
                    // 尝试检查文件是否实际存在
                    string fullPath = file;
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 完整路径: {fullPath}");
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 文件存在: {System.IO.File.Exists(fullPath)}");
                    
                    // 🔥 修复：尝试直接读取文件以诊断问题
                    if (System.IO.File.Exists(fullPath))
                    {
                        try
                        {
                            string directContent = System.IO.File.ReadAllText(fullPath);
                            System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 直接读取文件长度: {directContent?.Length ?? 0}");
                            if (!string.IsNullOrEmpty(directContent))
                            {
                                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 使用直接读取的内容");
                                content = directContent;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 直接读取文件失败: {ex.Message}");
                        }
                    }
                    
                    if (string.IsNullOrEmpty(content))
                    {
                        throw new InvalidOperationException($"文件内容为空: {file}");
                    }
                }
                
                // 🔥 修复：只对非压缩的 JSON 文件清理换行符
                // 压缩文件的内容会在 DeserializeJson 中解压，不应该在这里处理
                if (!zip)
                {
                    content = content.Trim();
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] JSON 文件内容长度: {content.Length}");
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] JSON 文件前100字符: {(content.Length > 100 ? content.Substring(0, 100) : content)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 压缩文件原始长度: {content.Length}");
                }
                
                // 🔥 修复：再次检查处理后的内容
                if (string.IsNullOrWhiteSpace(content))
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] ❌ 处理后内容为空: {file}");
                    throw new InvalidOperationException($"处理后文件内容为空: {file}");
                }
                
                // 🔥 AOT修复：剧本文件和 CommonData（非压缩）不含 $id/$ref，
                // 必须用不带 ReferenceHandler.Preserve 的 options，否则 Release/AOT 崩溃。
                // 日期：2026-03-20
                // 问题：CommonData.json 使用 GetDefaultOptions() 导致集合为空
                // 🔥 版本标记：v2026-03-20-19:15
                T result;
                if (!zip && (typeof(T) == typeof(global::GameObjects.GameScenario) || 
                             typeof(T) == typeof(global::GameObjects.CommonData)))
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 🔥🔥🔥 使用 GetScenarioFileOptions 反序列化 {typeof(T).Name}（修复版本 v2026-03-20-19:15）");
                    result = DeserializeJson<T>(content, WorldOfTheThreeKingdoms.Serialization.GameJsonContext.GetScenarioFileOptions());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 使用 GetDefaultOptions 反序列化 {typeof(T).Name}（有 ReferenceHandler.Preserve）");
                    result = DeserializeJson<T>(content, zip, Net);
                }
                
                if (result == null)
                {
                    throw new InvalidOperationException($"反序列化 {typeof(T).Name} 返回 null");
                }
                
                // 🔥 根本修复：验证反序列化结果的完整性
                if (result is global::GameObjects.CommonData commonData)
                {
                    ValidateDeserializedCommonData(commonData);
                }
                
                // 确保 GameScenario 集合初始化
                if (result is global::GameObjects.GameScenario scenario)
                {
                    EnsureScenarioCollectionsInitialized(scenario);
                }
                
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] ✅ 反序列化成功: {typeof(T).Name}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] ❌ 反序列化失败: {file}");
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 错误类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 错误详情: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DeserializeJsonFile] 堆栈跟踪: {ex.StackTrace}");
                
                // 🔥 根本修复：提供详细的错误诊断
                if (ex.Message.Contains("JsonException") || ex.Message.Contains("JsonSerializer") || ex.Message.Contains("JSON tokens"))
                {
                    System.Diagnostics.Debug.WriteLine("[DeserializeJsonFile] 🔍 JSON 序列化器错误，可能的原因:");
                    System.Diagnostics.Debug.WriteLine("[DeserializeJsonFile]   1. JSON 格式损坏或不完整");
                    System.Diagnostics.Debug.WriteLine("[DeserializeJsonFile]   2. 类型未在 GameJsonContext 中注册");
                    System.Diagnostics.Debug.WriteLine("[DeserializeJsonFile]   3. 循环引用处理不当");
                    System.Diagnostics.Debug.WriteLine("[DeserializeJsonFile]   4. 文件路径错误或文件不存在");
                }
                

                WebTools.TakeWarnMsg("读取用户对象失败:" + file, "DeserializeJsonFile:" + file + " " + isUserFile, ex);
                throw; // 🔥 根本修复：重新抛出，暴露真正的 FormatException 根本原因，禁止吞异常返回 null
            }
        }

        /// <summary>
        /// 🔥 根本修复：验证反序列化后的 CommonData 完整性
        /// </summary>
        private static void ValidateDeserializedCommonData(global::GameObjects.CommonData commonData)
        {
            var issues = new List<string>();
            
            try
            {
                // 检查关键属性是否正确反序列化
                if (commonData.AllMilitaryKinds == null)
                    issues.Add("AllMilitaryKinds 反序列化为 null");
                else if (commonData.AllMilitaryKinds.MilitaryKinds == null)
                    issues.Add("AllMilitaryKinds.MilitaryKinds 反序列化为 null");
                    
                if (commonData.AllInformationKinds == null)
                    issues.Add("AllInformationKinds 反序列化为 null");
                else if (commonData.AllInformationKinds.GameObjects == null)
                    issues.Add("AllInformationKinds.GameObjects 反序列化为 null");
                    
                if (commonData.AllArchitectureKinds == null)
                    issues.Add("AllArchitectureKinds 反序列化为 null");
                
                // 🔥 关键修复：检查 AllIdealTendencyKinds
                if (commonData.AllIdealTendencyKinds == null)
                    issues.Add("AllIdealTendencyKinds 反序列化为 null");
                else if (commonData.AllIdealTendencyKinds.Count == 0)
                    issues.Add("AllIdealTendencyKinds 反序列化为空列表");
                    
                if (issues.Count > 0)
                {
                    string errorMsg = "CommonData 反序列化完整性检查失败:\n" + string.Join("\n", issues);
                    System.Diagnostics.Debug.WriteLine($"[ValidateDeserializedCommonData] ❌ {errorMsg}");
                    
                    // 🔥 尝试修复 AllIdealTendencyKinds
                    if (commonData.AllIdealTendencyKinds == null || commonData.AllIdealTendencyKinds.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[ValidateDeserializedCommonData] 尝试修复 AllIdealTendencyKinds...");
                        if (TryFixIdealTendencyKinds(commonData))
                        {
                            System.Diagnostics.Debug.WriteLine("[ValidateDeserializedCommonData] ✅ AllIdealTendencyKinds 修复成功");
                            // 重新检查，移除相关错误
                            issues.RemoveAll(i => i.Contains("AllIdealTendencyKinds"));
                        }
                    }
                    
                    // 如果还有其他问题，抛出异常
                    if (issues.Count > 0)
                    {
                        throw new InvalidDataException(errorMsg);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[ValidateDeserializedCommonData] ✅ CommonData 反序列化完整性检查通过");
                System.Diagnostics.Debug.WriteLine($"[ValidateDeserializedCommonData] - AllIdealTendencyKinds: {commonData.AllIdealTendencyKinds?.Count ?? 0} 个理想倾向");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateDeserializedCommonData] ❌ 验证过程中发生异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 🔥 修复 AllIdealTendencyKinds 反序列化问题
        /// </summary>
        private static bool TryFixIdealTendencyKinds(global::GameObjects.CommonData commonData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TryFixIdealTendencyKinds] 开始修复 AllIdealTendencyKinds");
                
                // 1. 确保 AllIdealTendencyKinds 不为 null
                if (commonData.AllIdealTendencyKinds == null)
                {
                    commonData.AllIdealTendencyKinds = new global::GameObjects.GameObjectList();
                    System.Diagnostics.Debug.WriteLine("[TryFixIdealTendencyKinds] 创建了新的 AllIdealTendencyKinds 实例");
                }
                
                // 2. 如果为空，尝试重新加载
                if (commonData.AllIdealTendencyKinds.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TryFixIdealTendencyKinds] AllIdealTendencyKinds 为空，尝试重新加载");
                    
                    // 尝试从原始 JSON 文件重新加载
                    string commonDataPath = @"Content\Data\Common\CommonData.json";
                    if (System.IO.File.Exists(commonDataPath))
                    {
                        string jsonContent = System.IO.File.ReadAllText(commonDataPath);
                        
                        // 使用 System.Text.Json 解析
                        using var document = JsonDocument.Parse(jsonContent);
                        if (document.RootElement.TryGetProperty("AllIdealTendencyKinds", out var idealTendencyElement))
                        {
                            if (idealTendencyElement.TryGetProperty("GameObjects", out var gameObjectsElement))
                            {
                                var options = GameJsonContext.GetLooseOptions();
                                var idealTendencyKinds = System.Text.Json.JsonSerializer.Deserialize(
                                    gameObjectsElement,
                                    ResolveTypeInfo<List<global::GameObjects.PersonDetail.IdealTendencyKind>>(options));
                                
                                if (idealTendencyKinds != null && idealTendencyKinds.Count > 0)
                                {
                                    commonData.AllIdealTendencyKinds.Clear();
                                    foreach (var itk in idealTendencyKinds)
                                    {
                                        commonData.AllIdealTendencyKinds.Add(itk);
                                    }
                                    
                                    System.Diagnostics.Debug.WriteLine($"[TryFixIdealTendencyKinds] ✅ 成功加载 {idealTendencyKinds.Count} 个 IdealTendencyKind");
                                    return true;
                                }
                            }
                        }
                    }
                    
                    // 3. 如果重新加载失败，创建默认数据
                    System.Diagnostics.Debug.WriteLine("[TryFixIdealTendencyKinds] 重新加载失败，创建默认 IdealTendencyKind 数据");
                    CreateDefaultIdealTendencyKinds(commonData.AllIdealTendencyKinds);
                    return true;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TryFixIdealTendencyKinds] ❌ 修复失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建默认的 IdealTendencyKind 数据
        /// </summary>
        private static void CreateDefaultIdealTendencyKinds(global::GameObjects.GameObjectList idealTendencyKinds)
        {
            var defaultData = new[]
            {
                new { ID = 0, Name = "0-完全不考虑", Offset = 75 },
                new { ID = 1, Name = "1-很随意", Offset = 70 },
                new { ID = 2, Name = "2-比较随意", Offset = 65 },
                new { ID = 3, Name = "3-普通", Offset = 50 },
                new { ID = 4, Name = "4-比较在意", Offset = 30 },
                new { ID = 5, Name = "5-非常在意", Offset = 10 }
            };
            
            foreach (var data in defaultData)
            {
                var itk = new global::GameObjects.PersonDetail.IdealTendencyKind
                {
                    ID = data.ID,
                    Name = data.Name,
                    Offset = data.Offset
                };
                idealTendencyKinds.Add(itk);
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateDefaultIdealTendencyKinds] 创建了 {defaultData.Length} 个默认 IdealTendencyKind");
        }

        /// <summary>
        /// 确保 GameScenario 的关键集合已初始化，防止 null 引用异常
        /// </summary>
        private static void EnsureScenarioCollectionsInitialized(global::GameObjects.GameScenario scenario)
        {
            if (scenario == null) return;

            // 🔥 诊断日志：写文件而不是 Debug.WriteLine（Release 下可见）
            string logPath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "load_progress.txt");
            void Log(string msg)
            {
                try
                {
                    System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}\n");
                }
                catch { }
            }

            Log("开始 EnsureScenarioCollectionsInitialized");


            // 检查并注入 CommonData
            if (scenario.GameCommonData == null || 
                scenario.GameCommonData.AllArchitectureKinds == null || 
                scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds == null ||
                scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count == 0)
            {
                Log("检测到 CommonData 缺失，开始加载");

                string root = Platform.Current.DirectoryName(Platform.Current.Location);
                string commonPath = Path.Combine(root, "Content", "Data", "Common", "CommonData.json");
                
                if (!File.Exists(commonPath)) 
                {
                    commonPath = Path.Combine("Content", "Data", "Common", "CommonData.json");
                }

                if (File.Exists(commonPath))
                {
                    try 
                    {
                        Log($"开始加载 CommonData: {commonPath}");
                        string commonJson = File.ReadAllText(commonPath);
                        Log($"CommonData JSON 长度: {commonJson.Length}");
                        var commonData = DeserializeJson<global::GameObjects.CommonData>(commonJson);
                        Log("CommonData 反序列化完成");
                        scenario.GameCommonData = commonData;
                        
                        Log("CommonData 加载成功");
                    }
                    catch (Exception ex)
                    {
                        Log($"CommonData 加载失败: {ex.Message}");
                        throw new FileNotFoundException($"Critical: CommonData.json load failed! {ex.Message}");
                    }
                }
                else
                {
                     Log($"CommonData 文件不存在: {commonPath}");
                     throw new FileNotFoundException("Critical: CommonData.json not found! Cannot verify IDs.");
                }
            }
            
            Log("设置 Session.Current.Scenario");
            // 全局设置
            Session.Current.Scenario = scenario;

            Log("开始初始化列表");
            // 初始化列表（防止 null 引用异常）
            if (scenario.Factions == null) 
            {
                scenario.Factions = new global::GameObjects.FactionListWithQueue();
            }
            
            if (scenario.Persons == null) 
            {
                scenario.Persons = new global::GameObjects.PersonList();
            }
            
            if (scenario.Architectures == null) 
            {
                scenario.Architectures = new global::GameObjects.ArchitectureList();
            }
            
            if (scenario.Troops == null) 
            {
                scenario.Troops = new global::GameObjects.TroopListWithQueue();
            }
            
            if (scenario.ScenarioMap == null)
            {
                scenario.ScenarioMap = new global::GameObjects.Map();
            }

            if (scenario.Militaries == null)
            {
                scenario.Militaries = new global::GameObjects.MilitaryList();
            }

            if (scenario.Informations == null)
            {
                scenario.Informations = new global::GameObjects.InformationList();
            }

            if (scenario.Legions == null)
            {
                scenario.Legions = new global::GameObjects.LegionList();
            }

            if (scenario.Sections == null)
            {
                scenario.Sections = new global::GameObjects.SectionList();
            }

            if (scenario.Treasures == null)
            {
                scenario.Treasures = new global::GameObjects.TreasureList();
            }

            if (scenario.States == null)
            {
                scenario.States = new global::GameObjects.ArchitectureDetail.StateList();
            }

            if (scenario.Regions == null)
            {
                scenario.Regions = new global::GameObjects.ArchitectureDetail.RegionList();
            }

            Log("列表初始化完成，开始构建映射");
            // 重新链接对象关系
            
            var pMap = new Dictionary<int, global::GameObjects.Person>();
            var fMap = new Dictionary<int, global::GameObjects.Faction>();
            var aMap = new Dictionary<int, global::GameObjects.Architecture>();
            var tMap = new Dictionary<int, global::GameObjects.Troop>();

            // 构建映射
            Log("开始构建 Persons 映射");
            if (scenario.Persons != null)
            {
                int personCount = 0;
                foreach (global::GameObjects.Person p in scenario.Persons.GetList())
                {
                    if (p != null && !pMap.ContainsKey(p.ID))
                    {
                        pMap[p.ID] = p;
                        personCount++;
                        if (personCount % 100 == 0)
                        {
                            Log($"Persons 映射进度: {personCount}");
                        }
                    }
                }
                Log($"Persons 映射完成: {personCount} 个");
            }

            Log("开始构建 Factions 映射");
            if (scenario.Factions != null)
            {
                foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
                {
                    if (f != null && !fMap.ContainsKey(f.ID))
                    {
                        fMap[f.ID] = f;
                    }
                }
                Log($"Factions 映射完成: {fMap.Count} 个");
            }

            Log("开始构建 Architectures 映射");
            if (scenario.Architectures != null)
            {
                int archMapCount = 0;
                foreach (global::GameObjects.Architecture a in scenario.Architectures.GetList())
                {
                    if (a != null && !aMap.ContainsKey(a.ID))
                    {
                        aMap[a.ID] = a;
                        archMapCount++;
                        if (archMapCount % 50 == 0)
                        {
                            Log($"Architectures 映射进度: {archMapCount}");
                        }
                    }
                }
                Log($"Architectures 映射完成: {archMapCount} 个");
            }

            Log("开始构建 Troops 映射");
            if (scenario.Troops != null)
            {
                int troopMapCount = 0;
                foreach (global::GameObjects.Troop t in scenario.Troops.GetList())
                {
                    if (t != null && !tMap.ContainsKey(t.ID))
                    {
                        tMap[t.ID] = t;
                        troopMapCount++;
                        if (troopMapCount % 50 == 0)
                        {
                            Log($"Troops 映射进度: {troopMapCount}");
                        }
                    }
                }
                Log($"Troops 映射完成: {troopMapCount} 个");
            }

            // 重新链接势力关系（第一阶段：只设置 Leader，Capital 稍后设置）
            Log("开始重新链接势力关系（第一阶段）");
            foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
            {
                if (f == null) continue;
                
                f.Scenario = scenario;
                
                // 重新链接 Leader
                if (f.LeaderID >= 0 && pMap.TryGetValue(f.LeaderID, out var leader))
                {
                    f.Leader = leader;
                }
                
                // ⚠️ Capital 稍后设置（必须在 Architectures 填充后）
            }

            // 重新链接人物关系
            Log("开始重新链接人物关系");
            int personLinkCount = 0;
            foreach (global::GameObjects.Person p in scenario.Persons.GetList())
            {
                if (p == null) continue;
                
                p.Scenario = scenario;
                
                // 🔥 修复：初始化人物的 Character 属性
                if (p.Character == null && scenario.GameCommonData != null && scenario.GameCommonData.AllCharacterKinds != null)
                {
                    if (p.PCharacter >= 0 && p.PCharacter < scenario.GameCommonData.AllCharacterKinds.Count)
                    {
                        p.Character = scenario.GameCommonData.AllCharacterKinds[p.PCharacter];
                        if (p.Character == null)
                        {
                        }
                    }
                }
                
                // 🔥 修复：初始化人物的 IdealTendency 属性
                if (p.IdealTendency == null && scenario.GameCommonData != null && scenario.GameCommonData.AllIdealTendencyKinds != null)
                {
                    p.IdealTendency = scenario.GameCommonData.AllIdealTendencyKinds.GetGameObject(p.IdealTendencyIDString) as global::GameObjects.PersonDetail.IdealTendencyKind;
                    if (p.IdealTendency == null)
                    {
                    }
                }
                
                personLinkCount++;
                if (personLinkCount % 100 == 0)
                {
                    Log($"人物关系链接进度: {personLinkCount}");
                }
            }
            Log($"人物关系链接完成: {personLinkCount} 个");

            // 重新链接建筑关系
            Log("开始重新链接建筑关系");
            int archCount = 0;
            int totalArchs = scenario.Architectures.Count;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (global::GameObjects.Architecture a in scenario.Architectures.GetList())
            {
                if (a == null) continue;
                
                archCount++;
                
                // 每10个建筑输出一次进度
                if (archCount % 10 == 0 || archCount == totalArchs)
                {
                    Log($"建筑关系链接进度: {archCount}/{totalArchs}");
                }
                
                try
                {
                    a.Scenario = scenario;
                
                    // 🔥 修复：初始化建筑的 Kind 属性
                    if (a.Kind == null && scenario.GameCommonData != null && scenario.GameCommonData.AllArchitectureKinds != null)
                    {
                        // 优先使用 KindID，如果不存在则使用 KindId
                        int kindId = a.KindID != 0 ? a.KindID : a.KindId;
                        if (kindId > 0)
                        {
                            a.Kind = scenario.GameCommonData.AllArchitectureKinds.GetArchitectureKind(kindId);
                            if (a.Kind == null)
                            {
                            }
                        }
                    }
                    
                    // 🔥 从 BelongedFactionID 恢复 BelongedFaction 引用（不填充Architectures）
                    if (a.BelongedFactionID >= 0 && fMap.TryGetValue(a.BelongedFactionID, out var faction))
                    {
                        a.BelongedFaction = faction;
                    }
                    
                    // 修复建筑区域数据
                    if (a.ArchitectureArea == null)
                    {
                        a.ArchitectureArea = new global::GameObjects.GameArea();
                    }
                    
                    if (a.ArchitectureArea.Area == null)
                    {
                        a.ArchitectureArea.Area = new System.Collections.Generic.List<Microsoft.Xna.Framework.Point>();
                    }
                    
                    if (a.ArchitectureArea.Area.Count == 0 && !string.IsNullOrEmpty(a.ArchitectureAreaString))
                    {
                        a.LoadFromString(a.ArchitectureArea, a.ArchitectureAreaString);
                    }
                }
                catch (Exception ex)
                {
                }
            }
            
            sw.Stop();
            Log($"建筑关系链接完成: {archCount} 个，耗时 {sw.ElapsedMilliseconds}ms");
            
            // 🔥 从 Faction.ArchitectureIDs 恢复 Faction.Architectures 集合
            Log("开始从 ArchitectureIDs 恢复势力建筑列表");
            foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
            {
                if (f == null) continue;
                
                // 初始化集合
                if (f.Architectures == null)
                {
                    f.Architectures = new global::GameObjects.ArchitectureList();
                }
                else
                {
                    f.Architectures.Clear();
                }
                
                // 从 ArchitectureIDs 恢复
                if (f.ArchitectureIDs != null && f.ArchitectureIDs.Count > 0)
                {
                    foreach (int archId in f.ArchitectureIDs)
                    {
                        if (aMap.TryGetValue(archId, out var arch))
                        {
                            f.Architectures.Add(arch);
                        }
                        else
                        {
                        }
                    }
                }
            }

            // 🔥 重新链接势力关系（第二阶段：设置 Capital）
            // 必须在 Faction.Architectures 填充后才能设置 Capital
            // 因为 Capital.getter 会检查 Architectures.Count
            Log("开始重新链接势力关系（第二阶段：设置 Capital）");
            foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
            {
                if (f == null) continue;
                
                // 重新链接 Capital
                if (f.CapitalID >= 0 && aMap.TryGetValue(f.CapitalID, out var capital))
                {
                    f.Capital = capital;
                }
                else if (f.CapitalID >= 0)
                {
                }
                
                // 验证 Capital 是否设置成功
                if (f.Capital == null && f.Architectures.Count > 0)
                {
                }
            }

            // 重新链接部队关系
            Log("开始重新链接部队关系");
            int troopLinkCount = 0;
            foreach (global::GameObjects.Troop t in scenario.Troops.GetList())
            {
                if (t == null) continue;
                
                t.Scenario = scenario;
                
                // 🔥 从 BelongedFactionID 恢复 BelongedFaction 引用（不填充Troops集合）
                if (t.BelongedFactionID >= 0)
                {
                    if (fMap.TryGetValue(t.BelongedFactionID, out var faction))
                    {
                        t.BelongedFaction = faction;
                    }
                    else
                    {
                        // 🚨 数据损坏：势力不存在
                        string errorMsg = $"[SimpleSerializer] ❌ 数据损坏：部队 {t.Name}(ID:{t.ID}) 引用了不存在的势力 ID={t.BelongedFactionID}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                
                // 🔥 根本修复：重新链接 StartingArchitecture
                // 🔥 关键修复：ID=0 是有效的（洛阳的 ID 就是 0），必须使用 >= 0
                // 如果 ID >= 0 但找不到建筑，说明存档数据损坏，应该抛出异常而不是静默处理
                if (t.StartingArchitectureID >= 0)
                {
                    if (aMap.TryGetValue(t.StartingArchitectureID, out var startArch))
                    {
                        t.StartingArchitecture = startArch;
                    }
                    else
                    {
                        // 🚨 数据损坏：建筑不存在
                        string errorMsg = $"[SimpleSerializer] ❌ 数据损坏：部队 {t.Name}(ID:{t.ID}) 引用了不存在的 StartingArchitecture ID={t.StartingArchitectureID}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                else if (t.StartingArchitectureID == -1)
                {
                    // ID = -1 是合法的（表示没有出发城市）
                    t.StartingArchitecture = null;
                }
                else
                {
                    // ID = 0 或其他无效值，数据异常
                    string errorMsg = $"[SimpleSerializer] ❌ 数据异常：部队 {t.Name}(ID:{t.ID}) 的 StartingArchitectureID={t.StartingArchitectureID} 无效";
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
                
                // 🔥 根本修复：重新链接 WillArchitecture
                // 🔥 关键：ID=0 是有效的（洛阳），必须使用 >= 0
                if (t.WillArchitectureID >= 0)
                {
                    if (aMap.TryGetValue(t.WillArchitectureID, out var willArch))
                    {
                        t.WillArchitecture = willArch;
                    }
                    else
                    {
                        // 🚨 数据损坏：建筑不存在
                        string errorMsg = $"[SimpleSerializer] ❌ 数据损坏：部队 {t.Name}(ID:{t.ID}) 引用了不存在的 WillArchitecture ID={t.WillArchitectureID}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                else
                {
                    // ID < 0 表示无目标城市
                    t.WillArchitecture = null;
                }
                
                troopLinkCount++;
                if (troopLinkCount % 50 == 0)
                {
                    Log($"部队关系链接进度: {troopLinkCount}");
                }
            }
            Log($"部队关系链接完成: {troopLinkCount} 个");
            
            // 🔥 从 Faction.TroopIDs 恢复 Faction.Troops 集合
            Log("开始从 TroopIDs 恢复势力部队列表");
            foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
            {
                if (f == null) continue;
                
                // 初始化集合
                if (f.Troops == null)
                {
                    f.Troops = new global::GameObjects.TroopList();
                }
                else
                {
                    f.Troops.Clear();
                }
                
                // 从 TroopIDs 恢复
                if (f.TroopIDs != null && f.TroopIDs.Count > 0)
                {
                    foreach (int troopId in f.TroopIDs)
                    {
                        if (tMap.TryGetValue(troopId, out var troop))
                        {
                            f.Troops.Add(troop);
                        }
                        else
                        {
                        }
                    }
                }
            }

            // 🔥 修复：从 Section.ArchitectureIDs 恢复 Section.Architectures 关系
            Log("开始从 Section.ArchitectureIDs 恢复区域建筑列表");
            if (scenario.Sections != null)
            {
                
                foreach (global::GameObjects.Section section in scenario.Sections.GetList())
                {
                    if (section == null) continue;
                    
                    section.Scenario = scenario;
                    
                    // 初始化集合
                    if (section.Architectures == null)
                    {
                        section.Architectures = new global::GameObjects.ArchitectureList();
                    }
                    else
                    {
                        section.Architectures.Clear();
                    }
                    
                    // 🔥 诊断：检查 ArchitectureIDs 状态
                    // System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs == null: {section.ArchitectureIDs == null}");
                    // if (section.ArchitectureIDs != null)
                    // {
                    //     System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs.Count: {section.ArchitectureIDs.Count}");
                    //     if (section.ArchitectureIDs.Count > 0)
                    //     {
                    //         System.Diagnostics.Debug.WriteLine($"  - ArchitectureIDs: [{string.Join(", ", section.ArchitectureIDs)}]");
                    //     }
                    // }
                    
                    // 从 ArchitectureIDs 恢复
                    if (section.ArchitectureIDs != null && section.ArchitectureIDs.Count > 0)
                    {
                        foreach (int archId in section.ArchitectureIDs)
                        {
                            if (aMap.TryGetValue(archId, out var arch))
                            {
                                section.Architectures.Add(arch);
                                
                                // 同时更新反向引用
                                if (arch.BelongedSection == null)
                                {
                                    arch.BelongedSection = section;
                                    arch.BelongedSectionID = section.ID;
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                    else
                    {
                    }
                }
            }
            else
            {
            }
            
            // 🔥 从 Faction.LegionIDs 恢复 Faction.Legions 集合
            Log("开始从 LegionIDs 恢复势力军团列表");
            if (scenario.Legions != null)
            {
                // 先构建Legion映射
                var lMap = new Dictionary<int, global::GameObjects.Legion>();
                foreach (global::GameObjects.Legion l in scenario.Legions.GetList())
                {
                    if (l != null && !lMap.ContainsKey(l.ID))
                    {
                        lMap[l.ID] = l;
                    }
                }
                
                foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
                {
                    if (f == null) continue;
                    
                    // 初始化集合
                    if (f.Legions == null)
                    {
                        f.Legions = new global::GameObjects.LegionList();
                    }
                    else
                    {
                        f.Legions.Clear();
                    }
                    
                    // 从 LegionIDs 恢复
                    if (f.LegionIDs != null && f.LegionIDs.Count > 0)
                    {
                        foreach (int legionId in f.LegionIDs)
                        {
                            if (lMap.TryGetValue(legionId, out var legion))
                            {
                                f.Legions.Add(legion);
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            
            // 🔥 从 Faction.SectionIDs 恢复 Faction.Sections 集合
            Log("开始从 SectionIDs 恢复势力区域列表");
            if (scenario.Sections != null)
            {
                // 先构建Section映射
                var sMap = new Dictionary<int, global::GameObjects.Section>();
                foreach (global::GameObjects.Section s in scenario.Sections.GetList())
                {
                    if (s != null && !sMap.ContainsKey(s.ID))
                    {
                        sMap[s.ID] = s;
                    }
                }
                
                foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
                {
                    if (f == null) continue;
                    
                    // 初始化集合
                    if (f.Sections == null)
                    {
                        f.Sections = new global::GameObjects.SectionList();
                    }
                    else
                    {
                        f.Sections.Clear();
                    }
                    
                    // 从 SectionIDs 恢复
                    if (f.SectionIDs != null && f.SectionIDs.Count > 0)
                    {
                        foreach (int sectionId in f.SectionIDs)
                        {
                            if (sMap.TryGetValue(sectionId, out var section))
                            {
                                f.Sections.Add(section);
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            
            // 🔥 从 Faction.PersonIDs 恢复 Faction.Persons 集合
            Log("开始从 PersonIDs 恢复势力人物列表");
            foreach (global::GameObjects.Faction f in scenario.Factions.GetList())
            {
                if (f == null) continue;
                
                // 注意：Faction.Persons 是属性，不是字段，需要特殊处理
                // 先清空现有的人物列表
                if (f.Persons != null)
                {
                    f.Persons.Clear();
                }
                
                // 从 PersonIDs 恢复
                if (f.PersonIDs != null && f.PersonIDs.Count > 0)
                {
                    foreach (int personId in f.PersonIDs)
                    {
                        if (pMap.TryGetValue(personId, out var person))
                        {
                            // 使用Add方法添加到Persons集合
                            if (f.Persons != null && !f.Persons.HasGameObject(personId))
                            {
                                f.Persons.Add(person);
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }

            // 🔥 数据清洗：移除 AllPersonGeneratorTypes 中的污染对象
            Log("开始清洗 AllPersonGeneratorTypes 数据污染");
            
            // 🔥 ANTI-BAND-AID：GameCommonData 在此阶段必定存在
            // 如果为 null，说明数据加载流程有严重问题，应该 Fail Fast
            if (scenario.GameCommonData == null)
            {
                throw new InvalidOperationException(
                    "数据损坏：GameCommonData 未初始化，无法清洗 AllPersonGeneratorTypes");
            }

            if (scenario.GameCommonData.AllPersonGeneratorTypes == null)
            {
                throw new InvalidOperationException(
                    "数据损坏：AllPersonGeneratorTypes 未初始化");
            }

            // 🧊 COLD PATH：初始化代码，可读性优先
            List<GameObject> validTypes = [];  // C# 12 集合表达式
            int invalidCount = 0;

            foreach (GameObject obj in scenario.GameCommonData.AllPersonGeneratorTypes.GameObjects)
            {
                if (obj is global::GameObjects.PersonDetail.PersonGeneratorType)
                {
                    validTypes.Add(obj);
                }
                else
                {
                    invalidCount++;
                    System.Diagnostics.Debug.WriteLine(
                        $"[SimpleSerializer] 移除污染对象: ID={obj.ID}, Type={obj.GetType().Name}, Name={obj.Name}");
                }
            }

            if (invalidCount > 0)
            {
                scenario.GameCommonData.AllPersonGeneratorTypes.Clear();
                foreach (GameObject validType in validTypes)
                {
                    scenario.GameCommonData.AllPersonGeneratorTypes.Add(validType);
                }
            }
            else
            {
            }

            // 强制玩家势力设置
            Log("开始确保玩家势力设置");
            if (scenario.CurrentPlayer == null && scenario.Factions.Count > 0)
            {
                scenario.CurrentPlayer = scenario.Factions.GetList()[0] as global::GameObjects.Faction;
            }

            Log("EnsureScenarioCollectionsInitialized 完成");
        }

        #region 异步序列化方法

        /// <summary>
        /// 异步序列化方法
        /// </summary>
        public static async Task<string> SerializeJsonAsync<T>(T t, bool zip = false, bool indented = false, CancellationToken cancellationToken = default)
        {
            var options = GameJsonContext.GetDefaultOptions(indented);
            var typeInfo = ResolveTypeInfo<T>(options);
            
            using var stream = new MemoryStream();

            await System.Text.Json.JsonSerializer.SerializeAsync(stream, t, typeInfo, cancellationToken).ConfigureAwait(false);
            
            var result = Encoding.UTF8.GetString(stream.ToArray());
            
            if (zip)
            {
                result = await Task.Run(() => result.GZipCompressString(), cancellationToken).ConfigureAwait(false);
            }
            
            return result;
        }

        /// <summary>
        /// 异步反序列化方法
        /// 🔥 修改：在反序列化后自动执行引用修复
        /// </summary>
        public static async Task<T> DeserializeJsonAsync<T>(string s, bool zip = false, CancellationToken cancellationToken = default)
        {
            if (zip)
            {
                s = await Task.Run(() => s.GZipDecompressString(), cancellationToken).ConfigureAwait(false);
            }
            
            var options = GameJsonContext.GetDefaultOptions();
            var typeInfo = ResolveTypeInfo<T>(options);
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
            
            T? deserialized = await System.Text.Json.JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
            if (deserialized == null)
            {
                throw new InvalidOperationException($"System.Text.Json 异步反序列化 {typeof(T).Name} 返回 null");
            }

            T result = deserialized;
            
            // 🔥 只对 GameScenario 调用 FixSharedReferences
            if (result is GameScenario)
            {
                FixSharedReferences(result);
            }
            
            return result;
        }
        
        /// <summary>
        /// 🔥 核心修复逻辑：强制切断 ArchitectureArea 的共享引用 + 触发 IJsonOnDeserialized 回调
        /// 此方法通过反射或动态类型检查，确保不仅 GameScenario，包含 Architectures 的任何对象都能被修复
        /// </summary>
        private static void FixSharedReferences(object root)
        {
            DebugLog($"[FixSharedReferences] 开始处理，对象类型: {root?.GetType().Name ?? "null"}");
            
            // 🔥 AOT 修复：移除 dynamic 和反射，直接强转 GameScenario/Architecture
            if (root is not GameScenario scenario)
            {
                DebugLog($"[FixSharedReferences] 不是 GameScenario，跳过");
                return;
            }

            DebugLog($"[FixSharedReferences] 是 GameScenario，开始修复 Architecture");
            
            var architectures = scenario.Architectures.GetList();
            DebugLog($"[FixSharedReferences] Architecture 数量: {architectures.Count}");

            // 🔥 修复 Architecture 的 ArchitectureArea 共享引用
            int fixedCount = 0;
            foreach (Architecture arch in architectures)
            {
                arch.ArchitectureArea = new GameArea();
                arch.LoadFromString(arch.ArchitectureArea, arch.ArchitectureAreaString);
                fixedCount++;
            }
            
            DebugLog($"[FixSharedReferences] ✅ 修复完成，处理了 {fixedCount} 个 Architecture");

            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection)
            {
                DebugLog("[SimpleSerializer] GameArea 共享引用已强制修复。");
            }
        }

        #endregion

        #endregion
    }
}
