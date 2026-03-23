#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.Converters;

/// <summary>
/// 向后兼容转换器：支持旧格式（GameObjectList 包装）和新格式（直接数组）
/// 
/// 旧格式：{ "GameObjects": [...] }
/// 新格式：[...]
/// 
/// 日期：2026-03-16
/// 原因：剧本文件使用 GameObjectList 包装格式，DTO 期望直接数组
/// 
/// 🔥 关键修复：避免无限递归
/// 日期：2026-03-17
/// 问题：使用相同的 options 反序列化会导致转换器再次被调用，形成无限递归
/// 解决：创建不包含此转换器的 options 用于内部反序列化
/// </summary>
public class GameObjectListConverter<T> : JsonConverter<List<T>>
{
        // 🔥 缓存不包含转换器的 options，避免每次都创建
        private static JsonSerializerOptions? _innerOptions;
        private static readonly object _lock = new object();
        
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // 新格式：直接数组 [...]
                // 🔥 关键修复：使用不包含此转换器的 options
                var innerOptions = GetInnerOptions(options);
                List<T>? result = JsonSerializer.Deserialize<List<T>>(ref reader, innerOptions);
                return result ?? [];
            }
            
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                // 可能是以下格式之一：
                // 1. 旧格式：GameObjectList 包装 { "GameObjects": [...] }
                // 2. ReferenceHandler.Preserve 格式：{ "$id": "1", "$values": [...] }
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                
                // 🔥 关键修复：优先检查 $values 字段（ReferenceHandler.Preserve 格式）
                // 日期：2026-03-17
                // 原因：启用 ReferenceHandler.Preserve 后，集合被包装为 { "$id": "1", "$values": [...] }
                //       转换器未处理此格式，导致返回空列表
                if (doc.RootElement.TryGetProperty("$values", out JsonElement valuesElement))
                {
                    var innerOptions = GetInnerOptions(options);
                    List<T>? result = JsonSerializer.Deserialize<List<T>>(valuesElement.GetRawText(), innerOptions);
                    return result ?? [];
                }
                
                // 旧格式：GameObjectList 包装 { "GameObjects": [...] }
                if (doc.RootElement.TryGetProperty("GameObjects", out JsonElement gameObjects))
                {
                    var innerOptions = GetInnerOptions(options);
                    List<T>? result = JsonSerializer.Deserialize<List<T>>(gameObjects.GetRawText(), innerOptions);
                    return result ?? [];
                }
                
                // 空对象，返回空列表
                return [];
            }
            
            // 其他情况（null、字符串等），返回空列表
            return [];
        }
        
        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            // 写入新格式（直接数组）
            JsonSerializer.Serialize(writer, value, options);
        }
        
        /// <summary>
        /// 获取不包含 GameObjectListConverter 的 JsonSerializerOptions
        /// 用于避免无限递归
        /// </summary>
        private static JsonSerializerOptions GetInnerOptions(JsonSerializerOptions originalOptions)
        {
            // 🔥 使用缓存避免重复创建
            if (_innerOptions != null)
            {
                return _innerOptions;
            }
            
            lock (_lock)
            {
                if (_innerOptions != null)
                {
                    return _innerOptions;
                }
                
                // 🔥 创建新的 options，复制所有设置但移除 GameObjectListConverter
                var innerOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = originalOptions.DefaultIgnoreCondition,
                    IgnoreReadOnlyFields = originalOptions.IgnoreReadOnlyFields,
                    IgnoreReadOnlyProperties = originalOptions.IgnoreReadOnlyProperties,
                    IncludeFields = originalOptions.IncludeFields,
                    PropertyNameCaseInsensitive = originalOptions.PropertyNameCaseInsensitive,
                    PropertyNamingPolicy = originalOptions.PropertyNamingPolicy,
                    ReadCommentHandling = originalOptions.ReadCommentHandling,
                    WriteIndented = originalOptions.WriteIndented,
                    ReferenceHandler = originalOptions.ReferenceHandler,
                    TypeInfoResolver = originalOptions.TypeInfoResolver
                };
                
                // 🔥 复制所有转换器，但跳过 GameObjectListConverter
                foreach (var converter in originalOptions.Converters)
                {
                    // 检查是否是 GameObjectListConverter<T> 类型
                    var converterType = converter.GetType();
                    if (converterType.IsGenericType && 
                        converterType.GetGenericTypeDefinition() == typeof(GameObjectListConverter<>))
                    {
                        // 跳过 GameObjectListConverter
                        continue;
                    }
                    
                    innerOptions.Converters.Add(converter);
                }
                
                _innerOptions = innerOptions;
                return _innerOptions;
            }
        }
    }
