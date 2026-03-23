using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects.TroopDetail.EventEffect;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 🔥 AOT 友好的 EventEffect 字典转换器
    /// 不使用 JsonSerializer.Deserialize，完全手动解析 JSON
    /// </summary>
    public class EventEffectDictionaryConverter : JsonConverter<Dictionary<int, EventEffect>>
    {
        public override Dictionary<int, EventEffect> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine("[EventEffectDictionaryConverter] 开始解析");
            
            if (reader.TokenType == JsonTokenType.Null)
                return [];

            if (reader.TokenType != JsonTokenType.StartObject)
                return [];

            Dictionary<int, EventEffect> dict = [];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    System.Diagnostics.Debug.WriteLine($"[EventEffectDictionaryConverter] 解析完成，共 {dict.Count} 项");
                    return dict;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                // 读取 key（字符串形式的数字）
                string keyStr = reader.GetString();
                if (!int.TryParse(keyStr, out int key))
                    continue;

                // 移动到 value
                reader.Read();

                // 手动解析 EventEffect 对象
                EventEffect effect = ParseEventEffect(ref reader);
                if (effect != null)
                {
                    dict[key] = effect;
                }
            }

            return dict;
        }

        private EventEffect ParseEventEffect(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                return null;

            EventEffect effect = new();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return effect;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "ID":
                        effect.ID = reader.GetInt32();
                        break;
                    case "Name":
                        effect.Name = reader.GetString();
                        break;
                    case "Parameter":
                        effect.Parameter = reader.GetString() ?? "";
                        break;
                    case "Kind":
                        effect.Kind = ParseEventEffectKind(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return effect;
        }

        private EventEffectKind ParseEventEffectKind(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                return null;

            EventEffectKind kind = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return kind;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "ID":
                        kind.ID = reader.GetInt32();
                        break;
                    case "Name":
                        kind.Name = reader.GetString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return kind;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<int, EventEffect> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteStartObject();
                
                writer.WriteNumber("ID", kvp.Value.ID);
                writer.WriteString("Name", kvp.Value.Name);
                writer.WriteString("Parameter", kvp.Value.Parameter);
                
                if (kvp.Value.Kind != null)
                {
                    writer.WritePropertyName("Kind");
                    writer.WriteStartObject();
                    writer.WriteNumber("ID", kvp.Value.Kind.ID);
                    writer.WriteString("Name", kvp.Value.Kind.Name);
                    writer.WriteEndObject();
                }
                
                writer.WriteEndObject();
            }
            
            writer.WriteEndObject();
        }
    }
}
