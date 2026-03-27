using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using Tools;
using Platforms;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的序列化器
    /// 替换SimpleSerializer中的Newtonsoft.Json功能
    /// </summary>
    public static class SystemTextJsonSerializer
    {
        private static JsonSerializerOptions _serializerOptions;
        private static JsonSerializerOptions _deserializerOptions;

        static SystemTextJsonSerializer()
        {
            InitializeOptions();
        }

        private static void InitializeOptions()
        {
            // 🔥 关键修复：使用自定义 ReferenceHandler，对简单值类型集合（如 List<int>）禁用引用保留
            // 日期：2026-03-20
            // 问题：ReferenceHandler.Preserve 将 List<int> 序列化为 {"$id":"5","$values":[21]}
            //       反序列化时无法正确解析为 List<int>，导致 SkillIDs/StuntIDs/TitleIDs 变成空列表
            // 解决：使用 SimpleCollectionReferenceHandler，对简单集合不使用引用保留
            var referenceHandler = new SimpleCollectionReferenceHandler();
            
            // 序列化选项
            _serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = false, // 默认不缩进，可通过参数控制
                PropertyNamingPolicy = null, // 保持原始属性名
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                TypeInfoResolver = GameJsonContext.Default,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = referenceHandler, // 🔥 使用自定义 ReferenceHandler
                NumberHandling = JsonNumberHandling.AllowReadingFromString, // 允许从字符串读取数字（适配字典Key）
                Converters =
                {
                    new GameObjectListConverter(),
                    new GameObjectReferenceConverter(),
                    new FactionLeaderConverter(),
                    new PersonIdealTendencyConverter(),
                    new IdealTendencyKindConverter(),
                }
            };

            // 反序列化选项（更宽松）
            _deserializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                TypeInfoResolver = GameJsonContext.Default,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = referenceHandler, // 🔥 使用自定义 ReferenceHandler
                NumberHandling = JsonNumberHandling.AllowReadingFromString, // 允许从字符串读取数字（适配字典Key）
                Converters =
                {
                    new GameObjectListConverter(),
                    new GameObjectReferenceConverter(),
                    new FactionLeaderConverter(),
                    new PersonIdealTendencyConverter(),
                    new IdealTendencyKindConverter(),
                }
            };
        }

        /// <summary>
        /// 序列化对象到JSON字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="zip">是否GZip压缩结果</param>
        /// <param name="indented">是否格式化缩进</param>
        /// <returns>JSON字符串</returns>
        public static string SerializeJson<T>(T obj, bool zip = false, bool indented = false)
        {
            string result = null;
            
            lock (Platform.SerializerLock)
            {
                try
                {
                    var options = new JsonSerializerOptions(_serializerOptions)
                    {
                        WriteIndented = indented
                    };

                    result = JsonSerializer.Serialize(obj, JsonTypeInfoHelper.Resolve<T>(options));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] SerializeJson failed for type {typeof(T).Name}: {ex.Message}");
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
        /// 从JSON字符串反序列化对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="zip">字符串是否GZip压缩</param>
        /// <returns>反序列化的对象</returns>
        public static T DeserializeJson<T>(string json, bool zip = false)
        {
            if (zip)
            {
                json = json.GZipDecompressString();
            }

            T result;
            
            try
            {
                lock (Platform.SerializerLock)
                {
                    result = JsonSerializer.Deserialize(json, JsonTypeInfoHelper.Resolve<T>(_deserializerOptions));
                    
                    // 🔥【根本性修复】🔥
                    // 在锁内处理，确保线程安全，直接切断不该共享的引用
                    FixSharedReferences(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] DeserializeJson failed for type {typeof(T).Name}: {ex.Message}");
                
                // 尝试使用更宽松的选项
                try
                {
                    var fallbackOptions = GameJsonContext.GetLooseOptions();
                    result = JsonSerializer.Deserialize(json, JsonTypeInfoHelper.Resolve<T>(fallbackOptions));
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] Fallback deserialization succeeded for type {typeof(T).Name}");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] Fallback deserialization also failed: {fallbackEx.Message}");
                    throw;
                }
            }
            
            return result;
        }

        /// <summary>
        /// 序列化对象到JSON字符串（非泛型版本）
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="zip">是否GZip压缩结果</param>
        /// <param name="indented">是否格式化缩进</param>
        /// <returns>JSON字符串</returns>
        public static string SerializeJson(object obj, Type type, bool zip = false, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(type);
            string result = null;
            
            lock (Platform.SerializerLock)
            {
                try
                {
                    var options = new JsonSerializerOptions(_serializerOptions)
                    {
                        WriteIndented = indented
                    };

                    result = JsonSerializer.Serialize(obj, JsonTypeInfoHelper.Resolve(options, type));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] SerializeJson failed for type {type?.Name}: {ex.Message}");
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
        /// 从JSON字符串反序列化对象（非泛型版本）
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="type">目标类型</param>
        /// <param name="zip">字符串是否GZip压缩</param>
        /// <returns>反序列化的对象</returns>
        public static object DeserializeJson(string json, Type type, bool zip = false)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (zip)
            {
                json = json.GZipDecompressString();
            }

            object result;
            
            try
            {
                lock (Platform.SerializerLock)
                {
                    result = JsonSerializer.Deserialize(json, JsonTypeInfoHelper.Resolve(_deserializerOptions, type));
                    
                    // 🔥【根本性修复】🔥
                    // 在锁内处理，确保线程安全，直接切断不该共享的引用
                    FixSharedReferences(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] DeserializeJson failed for type {type?.Name}: {ex.Message}");
                
                // 尝试使用更宽松的选项
                try
                {
                    var fallbackOptions = GameJsonContext.GetLooseOptions();
                    result = JsonSerializer.Deserialize(json, JsonTypeInfoHelper.Resolve(fallbackOptions, type));
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] Fallback deserialization succeeded for type {type?.Name}");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemTextJsonSerializer] Fallback deserialization also failed: {fallbackEx.Message}");
                    throw;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 🔥 核心修复逻辑：强制切断 ArchitectureArea 的共享引用
        /// 此方法通过反射或动态类型检查，确保不仅 GameScenario，包含 Architectures 的任何对象都能被修复
        /// </summary>
        private static void FixSharedReferences(object root)
        {
            // 🔥 AOT 修复：移除 dynamic 和反射，直接强转 GameScenario/Architecture
            // 原实现使用 type.GetProperty() + dynamic，AOT 下两者均不可用，异常被 catch 吞掉导致静默失败。
            if (root is not GameScenario scenario) return;

            foreach (Architecture arch in scenario.Architectures.GetList())
            {
                arch.ArchitectureArea = new GameArea();
                arch.LoadFromString(arch.ArchitectureArea, arch.ArchitectureAreaString);
            }

            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableAnomalyDetection)
            {
                System.Diagnostics.Debug.WriteLine("[SystemTextJsonSerializer] GameArea 共享引用已强制修复。");
            }
        }


        /// <summary>
        /// 添加自定义转换器
        /// </summary>
        /// <param name="converter">转换器</param>
        public static void AddConverter(JsonConverter converter)
        {
            _serializerOptions.Converters.Add(converter);
            _deserializerOptions.Converters.Add(converter);
        }

        /// <summary>
        /// 获取序列化选项的副本
        /// </summary>
        /// <param name="indented">是否缩进</param>
        /// <returns>序列化选项</returns>
        public static JsonSerializerOptions GetSerializerOptions(bool indented = false)
        {
            return new JsonSerializerOptions(_serializerOptions)
            {
                WriteIndented = indented
            };
        }

        /// <summary>
        /// 获取反序列化选项的副本
        /// </summary>
        /// <returns>反序列化选项</returns>
        public static JsonSerializerOptions GetDeserializerOptions()
        {
            return new JsonSerializerOptions(_deserializerOptions);
        }
    }
}
