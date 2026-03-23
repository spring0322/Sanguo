using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的字典转换器
    /// 替换Newtonsoft.Json的LegacyDictionaryConverter
    /// 处理旧版本的Key/Value格式和自动编号
    /// </summary>
    public class LegacyDictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 1. Handle null
            if (reader.TokenType == JsonTokenType.Null) 
                return [];
            
            // 🔥 关键修复：优先处理对象格式（CommonData.json 使用对象格式）
            // 日期：2026-03-20
            // 问题：CommonData.json 的字典是 {"1": {...}, "2": {...}} 格式（对象），不是数组
            // 原代码优先处理数组，导致对象格式被当作"非数组"异常处理，转换失败
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] 🔥 开始处理对象格式字典");
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter]    类型: Dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>");
                    
                    using var document = JsonDocument.ParseValue(ref reader);
                    var root = document.RootElement;
                    
                    Dictionary<TKey, TValue> dict = [];
                    
                    int propertyCount = 0;
                    foreach (var property in root.EnumerateObject())
                    {
                        propertyCount++;
                    }
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter]    JSON 属性数量: {propertyCount}");
                    
                    int successCount = 0;
                    int failCount = 0;
                    
                    foreach (var property in root.EnumerateObject())
                    {
                        try
                        {
                            var key = ConvertKey(property.Name);
                            
                            // 🔥 关键修复：直接使用传入的 options，不要创建新的
                            // 问题：创建新 options 会丢失 TypeInfoResolver（AOT 源生成器上下文）
                            // 解决：直接使用传入的 options，依赖 AOT 源生成器处理嵌套对象
                            // 日期：2026-03-20
                            var value = (TValue)JsonSerializer.Deserialize(property.Value.GetRawText(), typeof(TValue), options);
                            
                            if (key != null && value != null)
                            {
                                if (!dict.ContainsKey(key))
                                {
                                    dict.Add(key, value);
                                    successCount++;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ⚠️ 重复键: {key}");
                                    failCount++;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ⚠️ 键或值为 null: key={key}, value={value}");
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ❌ 对象属性转换失败: {property.Name}");
                            System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter]    错误: {ex.Message}");
                            failCount++;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ✅ 对象格式字典转换完成");
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter]    成功: {successCount} 项，失败: {failCount} 项，总计: {dict.Count} 项");
                    return dict;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ❌ 对象格式处理失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter]    堆栈: {ex.StackTrace}");
                    return [];
                }
            }
            
            // 2. Handle non-array start - 增强容错
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] ⚠️ 未知 TokenType: {reader.TokenType}，返回空字典");
                return [];
            }

            Dictionary<TKey, TValue> dict2 = [];
            using var document2 = JsonDocument.ParseValue(ref reader);
            var array = document2.RootElement;

            int indexCounter = 0;

            foreach (var item in array.EnumerateArray())
            {
                try
                {
                    TKey key = default;
                    TValue val = default;
                    bool foundKey = false;

                    // 方案 0: 复杂 Key 的情况 (Key 是对象或结构体)，序列化为 [Key, Value] 数组
                    if (item.ValueKind == JsonValueKind.Array)
                    {
                        List<JsonElement> arrayItems = [];
                        foreach (var arrayItem in item.EnumerateArray())
                        {
                            arrayItems.Add(arrayItem);
                        }
                        
                        if (arrayItems.Count == 2)
                        {
                            // 🔥 AOT 修复：使用非泛型方法
                            try 
                            {
                                key = (TKey)JsonSerializer.Deserialize(arrayItems[0].GetRawText(), typeof(TKey), options);
                                val = (TValue)JsonSerializer.Deserialize(arrayItems[1].GetRawText(), typeof(TValue), options);
                                foundKey = true;
                            }
                            catch (Exception ex)
                            { 
                                System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] 数组转换失败 {typeof(TKey).Name}: {ex.Message}");
                            }
                        }
                    }

                    // 方案 A: 找显式的 Key/Value 结构
                    if (!foundKey && item.ValueKind == JsonValueKind.Object)
                    {
                        JsonElement kElement = default;
                        JsonElement vElement = default;
                        bool hasKey = item.TryGetProperty("Key", out kElement) || item.TryGetProperty("key", out kElement);
                        bool hasValue = item.TryGetProperty("Value", out vElement) || item.TryGetProperty("value", out vElement);

                        if (hasKey && hasValue)
                        {
                            // 🔥 AOT 修复：使用非泛型方法
                            key = (TKey)JsonSerializer.Deserialize(kElement.GetRawText(), typeof(TKey), options);
                            val = (TValue)JsonSerializer.Deserialize(vElement.GetRawText(), typeof(TValue), options);
                            foundKey = true;
                        }
                    }

                    // 方案 B: 找对象里的 ID 字段 (Flattened Object)
                    if (!foundKey && item.ValueKind == JsonValueKind.Object)
                    {
                        // 🔥 AOT 修复：使用非泛型方法
                        val = (TValue)JsonSerializer.Deserialize(item.GetRawText(), typeof(TValue), options);

                        // 尝试在对象里找 ID
                        JsonElement idElement = default;
                        bool hasId = item.TryGetProperty("ID", out idElement) || 
                                   item.TryGetProperty("id", out idElement) || 
                                   item.TryGetProperty("Id", out idElement) || 
                                   item.TryGetProperty("KindID", out idElement) || 
                                   item.TryGetProperty("KindId", out idElement);

                        if (hasId)
                        {
                            // 🔥 AOT 修复：使用非泛型方法
                            key = (TKey)JsonSerializer.Deserialize(idElement.GetRawText(), typeof(TKey), options);
                            foundKey = true;
                        }
                        // 方案 C: 如果没 ID，就用数组下标作为 ID (限 TKey 为 int)
                        else if (typeof(TKey) == typeof(int))
                        {
                            key = (TKey)(object)indexCounter;
                            foundKey = true;
                        }
                    }

                    // 存入字典
                    if (foundKey && key != null)
                    {
                        if (!dict2.ContainsKey(key))
                            dict2.Add(key, val);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] 数组项处理失败: {ex.Message}");
                }
                indexCounter++;
            }
            return dict2;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            // 使用默认序列化
            JsonSerializer.Serialize(writer, value, options);
        }
        
        // 辅助方法：将字符串转换为TKey类型
        private TKey ConvertKey(string keyString)
        {
            try
            {
                if (typeof(TKey) == typeof(int))
                {
                    return (TKey)(object)int.Parse(keyString);
                }
                else if (typeof(TKey) == typeof(string))
                {
                    return (TKey)(object)keyString;
                }
                else
                {
                    // 尝试使用Convert
                    return (TKey)Convert.ChangeType(keyString, typeof(TKey));
                }
            }
            catch
            {
                return default(TKey);
            }
        }
    }
}