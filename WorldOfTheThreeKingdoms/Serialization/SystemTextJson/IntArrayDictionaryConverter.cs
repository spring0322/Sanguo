using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 🔥 2026-02-12 根本修复：处理 Dictionary&lt;int, int[]&gt; 类型的转换器
    /// 问题：JSON 中键是字符串 "0", "1"，但 C# 定义是 int 键
    /// 解决：自动将字符串键转换为 int 键
    /// 用途：AiBattlingArchitectureStrings, BrotherIds, SuoshuIds, CloseIds, HatedIds 等
    /// </summary>
    public class IntArrayDictionaryConverter : JsonConverter<Dictionary<int, int[]>>
    {
        public override Dictionary<int, int[]> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<int, int[]>();

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
                        
                        // 🔥 关键：将字符串键转换为 int
                        if (int.TryParse(keyString, out var key))
                        {
                            reader.Read(); // 移动到值
                            
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                var list = new List<int>();
                                
                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndArray)
                                    {
                                        break;
                                    }
                                    
                                    if (reader.TokenType == JsonTokenType.Number)
                                    {
                                        list.Add(reader.GetInt32());
                                    }
                                    else if (reader.TokenType == JsonTokenType.String)
                                    {
                                        // 处理字符串形式的数字
                                        if (int.TryParse(reader.GetString(), out var intValue))
                                        {
                                            list.Add(intValue);
                                        }
                                    }
                                }
                                
                                result[key] = [.. list]; // C# 12 集合表达式
                            }
                            else if (reader.TokenType == JsonTokenType.Null)
                            {
                                result[key] = [];
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
                // 处理数组格式 [{"Key": 1, "Value": [1,2,3]}, ...]
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        int? key = null;
                        int[] value = null;

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

                                if (propertyName == "Key")
                                {
                                    if (reader.TokenType == JsonTokenType.Number)
                                    {
                                        key = reader.GetInt32();
                                    }
                                    else if (reader.TokenType == JsonTokenType.String && 
                                             int.TryParse(reader.GetString(), out var k))
                                    {
                                        key = k;
                                    }
                                }
                                else if (propertyName == "Value" && reader.TokenType == JsonTokenType.StartArray)
                                {
                                    var list = new List<int>();
                                    
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.EndArray)
                                        {
                                            break;
                                        }
                                        
                                        if (reader.TokenType == JsonTokenType.Number)
                                        {
                                            list.Add(reader.GetInt32());
                                        }
                                    }
                                    
                                    value = [.. list]; // C# 12 集合表达式
                                }
                            }
                        }

                        if (key.HasValue && value != null)
                        {
                            result[key.Value] = value;
                        }
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<int, int[]> value, JsonSerializerOptions options)
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
                        writer.WriteNumberValue(item);
                    }
                }
                writer.WriteEndArray();
            }
            
            writer.WriteEndObject();
        }
    }
}
