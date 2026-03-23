using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects.Influences;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// InfluenceKind 多态转换器（AOT 兼容版本）
    /// 🔥 使用源生成器生成的工厂，完全无反射
    /// 🔥 2026-03-16：删除所有反射 API，使用 InfluenceKindFactory
    /// </summary>
    public class InfluenceKindConverter : JsonConverter<InfluenceKind>
    {
        
        public override InfluenceKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject, got {reader.TokenType}");
            }

            // 读取整个 JSON 对象
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // 🔥 ANTI-BAND-AID：ID 字段必须存在，否则数据损坏
            int id = root.GetProperty("ID").GetInt32();

            // 🔥 使用源生成器生成的工厂创建实例（无反射）
            var instance = InfluenceKindFactory.Create(id);

            // 🔥 ANTI-BAND-AID：这些字段在 CommonData.json 中始终存在，直接 GetProperty
            instance.ID = id;
            instance.Name = root.GetProperty("Name").GetString();
            instance.Type = (InfluenceType)root.GetProperty("Type").GetInt32();
            instance.TroopLeaderValid = root.GetProperty("TroopLeaderValid").GetBoolean();
            instance.AIPersonValue = root.GetProperty("AIPersonValue").GetSingle();
            instance.AIPersonValuePow = root.GetProperty("AIPersonValuePow").GetSingle();

            return instance;
        }


        public override void Write(Utf8JsonWriter writer, InfluenceKind value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            // 写入基类字段（从 GameObject 继承）
            writer.WriteNumber("ID", value.ID);

            if (value.Name != null)
                writer.WriteString("Name", value.Name);

            // 写入 InfluenceKind 自己的字段
            writer.WriteNumber("Type", (int)value.Type);
            writer.WriteBoolean("TroopLeaderValid", value.TroopLeaderValid);
            writer.WriteNumber("AIPersonValue", value.AIPersonValue);
            writer.WriteNumber("AIPersonValuePow", value.AIPersonValuePow);

            writer.WriteEndObject();
        }
    }
}