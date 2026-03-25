using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects.TroopDetail.EventEffect;
using GameObjects.TroopDetail.EventEffect.EventEffectKindPack;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// EventEffectKind 专用转换器，解决 AOT 多态序列化问题
    /// </summary>
    public class EventEffectKindConverter : JsonConverter<EventEffectKind>
    {
        public override EventEffectKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            // 获取 ID 和 Name
            int id = 0;
            string name = "";

            if (root.TryGetProperty("ID", out var idElement))
                id = idElement.GetInt32();
            else if (root.TryGetProperty("id", out idElement))
                id = idElement.GetInt32();

            if (root.TryGetProperty("Name", out var nameElement))
                name = nameElement.GetString() ?? "";
            else if (root.TryGetProperty("name", out nameElement))
                name = nameElement.GetString() ?? "";

            // 根据 ID 创建对应的派生类实例
            EventEffectKind result = EventEffectKindFactory.CreateEventEffectKindByID(id);
            if (result == null)
            {
                throw new JsonException($"Unknown troop EventEffectKind ID: {id}");
            }

            result.ID = id;
            result.Name = name;
            return result;
        }

        public override void Write(Utf8JsonWriter writer, EventEffectKind value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteNumber("ID", value.ID);
            writer.WriteString("Name", value.Name);
            writer.WriteEndObject();
        }

        private EventEffectKind CreateEventEffectKindById(int id)
        {
            // 根据 ID 创建对应的 EventEffectKind 派生类
            // 这里使用简单的工厂模式，避免反射
            return id switch
            {
                0 => new EventEffectKind0(),
                1 => new EventEffectKind1(),
                10 => new EventEffectKind10(),
                15 => new EventEffectKind15(),
                20 => new EventEffectKind20(),
                25 => new EventEffectKind25(),
                30 => new EventEffectKind30(),
                35 => new EventEffectKind35(),
                40 => new EventEffectKind40(),
                45 => new EventEffectKind45(),
                50 => new EventEffectKind50(),
                60 => new EventEffectKind60(),
                80 => new EventEffectKind80(),
                100 => new EventEffectKind100(),
                _ => new EventEffectKind() // 默认基类实例
            };
        }
    }
}
