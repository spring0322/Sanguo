using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 专门处理 Dictionary<int, int> 类型的转换器
    /// 解决 ProhibitedFactionID 等字段的反序列化问题
    /// </summary>
    public class IntDictionaryConverter : JsonConverter<Dictionary<int, int>>
    {
        public override Dictionary<int, int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<int, int>();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return result;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var keyString = reader.GetString();
                        if (int.TryParse(keyString, out var key))
                        {
                            reader.Read(); // 移动到值
                            if (reader.TokenType == JsonTokenType.Number)
                            {
                                var value = reader.GetInt32();
                                result[key] = value;
                            }
                            else if (reader.TokenType == JsonTokenType.String)
                            {
                                var valueString = reader.GetString();
                                if (int.TryParse(valueString, out var value))
                                {
                                    result[key] = value;
                                }
                            }
                        }
                        else
                        {
                            // 跳过无法解析的键
                            reader.Read();
                            reader.Skip();
                        }
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                // 处理数组格式 [{"Key": 1, "Value": 2}, ...]
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        int? key = null;
                        int? value = null;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                break;
                            }

                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                var propertyName = reader.GetString();
                                reader.Read();

                                if (propertyName == "Key" && reader.TokenType == JsonTokenType.Number)
                                {
                                    key = reader.GetInt32();
                                }
                                else if (propertyName == "Value" && reader.TokenType == JsonTokenType.Number)
                                {
                                    value = reader.GetInt32();
                                }
                                else if (propertyName == "Key" && reader.TokenType == JsonTokenType.String)
                                {
                                    if (int.TryParse(reader.GetString(), out var k))
                                    {
                                        key = k;
                                    }
                                }
                                else if (propertyName == "Value" && reader.TokenType == JsonTokenType.String)
                                {
                                    if (int.TryParse(reader.GetString(), out var v))
                                    {
                                        value = v;
                                    }
                                }
                            }
                        }

                        if (key.HasValue && value.HasValue)
                        {
                            result[key.Value] = value.Value;
                        }
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                // 处理字符串格式，可能是序列化的字典
                var stringValue = reader.GetString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    try
                    {
                        // 尝试解析类似 "1:30,2:60" 的格式
                        var pairs = stringValue.Split(',');
                        foreach (var pair in pairs)
                        {
                            var keyValue = pair.Split(':');
                            if (keyValue.Length == 2 && 
                                int.TryParse(keyValue[0].Trim(), out var key) && 
                                int.TryParse(keyValue[1].Trim(), out var value))
                            {
                                result[key] = value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[IntDictionaryConverter] 解析字符串格式失败: {ex.Message}");
                    }
                }
            }


            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<int, int> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WriteNumber(kvp.Key.ToString(), kvp.Value);
            }
            writer.WriteEndObject();
        }
    }
}