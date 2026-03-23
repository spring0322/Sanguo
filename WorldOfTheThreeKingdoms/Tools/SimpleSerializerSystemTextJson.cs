using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.Xna.Framework;
using Platforms;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;
using GameManager;

namespace Tools
{
    /// <summary>
    /// System.Text.Json版本的序列化器
    /// 替换SimpleSerializer中的Newtonsoft.Json功能，提供AOT兼容性
    /// </summary>
    public class SimpleSerializerSystemTextJson
    {
        private static readonly JsonSerializerOptions _defaultOptions;
        private static readonly JsonSerializerOptions _indentedOptions;

        static SimpleSerializerSystemTextJson()
        {
            _defaultOptions = CreateJsonOptions(false);
            _indentedOptions = CreateJsonOptions(true);
        }

        /// <summary>
        /// 创建JSON序列化选项
        /// </summary>
        /// <param name="indented">是否格式化输出</param>
        /// <returns>JSON序列化选项</returns>
        private static JsonSerializerOptions CreateJsonOptions(bool indented)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                PropertyNamingPolicy = null, // 保持原始属性名
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.Preserve, // 处理循环引用
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = GameJsonContext.Default,
                Converters =
                {
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter(),
                    // new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectReferenceConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.FactionLeaderConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.PersonIdealTendencyConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IdealTendencyKindConverter()
                }
            };

            return options;
        }

        #region XMLSerializer (保持不变)
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
                        T t = (T)xz.Deserialize(sr);
                        Platform.SessionActive = true;
                        return t;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeXML failed for type {typeof(T).Name}: {ex.Message}");
                    throw;
                }
            }
        }
        #endregion

        #region JsonSerializer (System.Text.Json版本)

        /// <summary>
        /// 使用System.Text.Json序列化对象为JSON字符串
        /// </summary>
        /// <param name="t">要序列化的对象</param>
        /// <param name="zip">是否GZip压缩结果</param>
        /// <param name="indented">是否格式化输出</param>
        /// <param name="net">遗留参数（兼容性）</param>
        public static string SerializeJson<T>(T t, bool zip = false, bool indented = false, bool net = false)
        {
            string result = null;
            lock (Platform.SerializerLock)
            {
                try
                {
                    var options = indented ? _indentedOptions : _defaultOptions;
                    result = JsonSerializer.Serialize(t, options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SerializeJson failed for type {typeof(T).Name}: {ex.Message}");
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
        /// 使用System.Text.Json反序列化JSON字符串为对象
        /// 🔥 修改：在反序列化后自动执行引用修复
        /// </summary>
        /// <param name="s">JSON字符串</param>
        /// <param name="zip">字符串是否GZip压缩</param>
        /// <param name="net">遗留参数（兼容性）</param>
        public static T DeserializeJson<T>(string s, bool zip = false, bool net = false)
        {
            if (zip)
            {
                s = s.GZipDecompressString();
            }

            T t;
            try
            {
                lock (Platform.SerializerLock)
                {
                    t = JsonSerializer.Deserialize<T>(s, _defaultOptions);
                    
                    // 🔥【根本性修复】🔥
                    // 在锁内处理，确保线程安全，直接切断不该共享的引用
                    FixSharedReferences(t);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeJson failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }

            return t;
        }

        /// <summary>
        /// 异步序列化对象为JSON字符串
        /// </summary>
        public static async Task<string> SerializeJsonAsync<T>(T t, bool zip = false, bool indented = false)
        {
            string result = null;
            
            try
            {
                var options = indented ? _indentedOptions : _defaultOptions;
                
                using (var stream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(stream, t, options).ConfigureAwait(false);
                    result = Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SerializeJsonAsync failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }
            
            if (zip)
            {
                result = await Task.Run(() => result.GZipCompressString()).ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// 异步反序列化JSON字符串为对象
        /// 🔥 修改：在反序列化后自动执行引用修复
        /// </summary>
        public static async Task<T> DeserializeJsonAsync<T>(string s, bool zip = false)
        {
            if (zip)
            {
                s = await Task.Run(() => s.GZipDecompressString()).ConfigureAwait(false);
            }

            T t;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                {
                    t = await JsonSerializer.DeserializeAsync<T>(stream, _defaultOptions).ConfigureAwait(false);
                }
                
                // 🔥【根本性修复】🔥
                // 确保异步加载也能享受到修复
                FixSharedReferences(t);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeJsonAsync failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }

            return t;
        }
        
        /// <summary>
        /// 🔥 核心修复逻辑：强制切断 ArchitectureArea 的共享引用
        /// 此方法通过反射或动态类型检查，确保不仅 GameScenario，包含 Architectures 的任何对象都能被修复
        /// </summary>
        private static void FixSharedReferences(object root)
        {
            if (root == null) return;

            try
            {
                // 1. 识别是否是 GameScenario (通过类型名判断，避免强依赖)
                var type = root.GetType();
                if (type.Name == "GameScenario")
                {
                    // 2. 获取 Architectures 列表
                    var archsProp = type.GetProperty("Architectures");
                    if (archsProp != null)
                    {
                        var architectures = archsProp.GetValue(root) as System.Collections.IEnumerable;
                        if (architectures != null)
                        {
                            foreach (var arch in architectures)
                            {
                                // 3. 针对每个 Architecture 执行"物理切断"
                                // 这里使用 dynamic 避免在该文件引用具体的 Architecture 类型导致循环依赖或找不到定义
                                // 如果你有 explicit reference 也可以直接强转
                                dynamic dArch = arch;
                                
                                // 强制创建新的 GameArea 实例，物理切断 JSON 的引用链
                                // 注意：这里假设 GameArea 构造函数是无参的
                                dArch.ArchitectureArea = new GameObjects.GameArea();
                                
                                // 重新从字符串加载数据 (这是数据的真实来源)
                                if (!string.IsNullOrEmpty(dArch.ArchitectureAreaString))
                                {
                                    dArch.LoadFromString(dArch.ArchitectureArea, dArch.ArchitectureAreaString);
                                }
                            }
                            
                            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection)
                            {
                                System.Diagnostics.Debug.WriteLine("[SimpleSerializer] GameArea 共享引用已强制修复。");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 仅仅记录日志，不阻断流程。这只是一个补丁，不应导致游戏崩溃。
                System.Diagnostics.Debug.WriteLine($"[FixSharedReferences] Warning: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证JSON序列化往返一致性
        /// </summary>
        /// <param name="obj">要测试的对象</param>
        /// <returns>往返测试是否成功</returns>
        public static bool ValidateSerializationRoundTrip<T>(T obj)
        {
            try
            {
                var json = SerializeJson(obj);
                var deserialized = DeserializeJson<T>(json);
                
                // 基本验证：确保反序列化成功且不为null
                if (deserialized == null && obj != null)
                {
                    return false;
                }
                
                if (deserialized != null && obj == null)
                {
                    return false;
                }
                
                // 对于GameObject类型，验证ID是否一致
                if (obj is GameObject gameObj && deserialized is GameObject deserializedGameObj)
                {
                    return gameObj.ID == deserializedGameObj.ID;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] Validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取序列化统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public static string GetSerializationStats()
        {
            return "System.Text.Json序列化器 - AOT兼容版本 (含GameArea引用热修复)";
        }

        #endregion

        #region 兼容性方法

        /// <summary>
        /// 兼容性方法：保存对象到文件
        /// </summary>
        public static void SaveToFile<T>(T obj, string filePath, bool indented = true)
        {
            try
            {
                var json = SerializeJson(obj, indented: indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SaveToFile failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 异步保存对象到文件
        /// </summary>
        public static async Task SaveToFileAsync<T>(T obj, string filePath, bool indented = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await SerializeJsonAsync(obj, indented: indented).ConfigureAwait(false);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SaveToFileAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 兼容性方法：从文件加载对象
        /// </summary>
        public static T LoadFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }
                
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return DeserializeJson<T>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] LoadFromFile failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 异步从文件加载对象
        /// </summary>
        public static async Task<T> LoadFromFileAsync<T>(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }
                
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                return await DeserializeJsonAsync<T>(json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] LoadFromFileAsync failed: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}