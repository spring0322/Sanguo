using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 🔥 2026-02-12 根本修复：专门处理 Dictionary<int, List<EventEffect>> 的转换器
    /// 问题：AOT 源生成器无法自动处理嵌套泛型类型 Dictionary<int, List<T>>
    /// 解决：创建专门的转换器来手动处理序列化和反序列化
    /// </summary>
    public class EventEffectListDictionaryConverter<TEventEffect> : JsonConverter<Dictionary<int, List<TEventEffect>>>
        where TEventEffect : class
    {
        private static TEventEffect DeserializeEventEffect(JsonElement element, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(
                element.GetRawText(),
                JsonTypeInfoHelper.Resolve<TEventEffect>(options));
        }

        public override Dictionary<int, List<TEventEffect>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 处理 null
            if (reader.TokenType == JsonTokenType.Null)
                return [];

            // 处理非对象类型
            if (reader.TokenType != JsonTokenType.StartObject && reader.TokenType != JsonTokenType.StartArray)
                return [];

            Dictionary<int, List<TEventEffect>> result = [];

            try
            {
                using var document = JsonDocument.ParseValue(ref reader);
                var root = document.RootElement;

                // 情况 1：标准对象格式 { "1": [...], "2": [...] }
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        if (int.TryParse(property.Name, out int key))
                        {
                            List<TEventEffect> list = [];
                            
                            if (property.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in property.Value.EnumerateArray())
                                {
                                    try
                                    {
                                        var effect = DeserializeEventEffect(item, options);
                                        if (effect != null)
                                        {
                                            list.Add(effect);
                                        }
                                    }
                                    catch
                                    {
                                        // 忽略单个元素的反序列化错误
                                    }
                                }
                            }
                            
                            if (!result.ContainsKey(key))
                            {
                                result[key] = list;
                            }
                        }
                    }
                }
                // 情况 2：数组格式 [ { "Key": 1, "Value": [...] }, ... ]
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            int key = 0;
                            List<TEventEffect> list = [];
                            bool hasKey = false;

                            if (item.TryGetProperty("Key", out var keyElement) || item.TryGetProperty("key", out keyElement))
                            {
                                if (keyElement.TryGetInt32(out key))
                                {
                                    hasKey = true;
                                }
                            }

                            if (item.TryGetProperty("Value", out var valueElement) || item.TryGetProperty("value", out valueElement))
                            {
                                if (valueElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var effectItem in valueElement.EnumerateArray())
                                    {
                                        try
                                        {
                                            var effect = DeserializeEventEffect(effectItem, options);
                                            if (effect != null)
                                            {
                                                list.Add(effect);
                                            }
                                        }
                                        catch
                                        {
                                            // 忽略单个元素的反序列化错误
                                        }
                                    }
                                }
                            }

                            if (hasKey && !result.ContainsKey(key))
                            {
                                result[key] = list;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 返回空字典而不是抛出异常
                return [];
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<int, List<TEventEffect>> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteStartArray();
                
                if (kvp.Value != null)
                {
                    foreach (var item in kvp.Value)
                    {
                        JsonSerializer.Serialize(writer, item, JsonTypeInfoHelper.Resolve<TEventEffect>(options));
                    }
                }
                
                writer.WriteEndArray();
            }
            
            writer.WriteEndObject();
        }
    }
}
