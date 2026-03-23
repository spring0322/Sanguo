using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects.PersonDetail;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的IdealTendencyKind转换器
    /// 专门处理IdealTendencyKind的AOT序列化和反序列化
    /// </summary>
    public class IdealTendencyKindConverter : JsonConverter<IdealTendencyKind>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IdealTendencyKind);
        }

        public override IdealTendencyKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject token for IdealTendencyKind, got {reader.TokenType}");
            }

            var idealTendencyKind = new IdealTendencyKind();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "ID":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            idealTendencyKind.ID = reader.GetInt32();
                        }
                        break;
                    case "Name":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            idealTendencyKind.Name = reader.GetString();
                        }
                        break;
                    case "Offset":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            idealTendencyKind.Offset = reader.GetInt32();
                        }
                        break;
                    default:
                        // 跳过未知属性
                        reader.Skip();
                        break;
                }
            }

            return idealTendencyKind;
        }

        public override void Write(Utf8JsonWriter writer, IdealTendencyKind value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WriteNumber("ID", value.ID);
            
            if (!string.IsNullOrEmpty(value.Name))
            {
                writer.WriteString("Name", value.Name);
            }
            
            writer.WriteNumber("Offset", value.Offset);
            
            writer.WriteEndObject();
        }
    }
}