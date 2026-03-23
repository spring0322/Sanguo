using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using GameObjects.PersonDetail;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    public class TextMessageTableConverter : JsonConverter<TextMessageTable>
    {
        public override TextMessageTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var table = new TextMessageTable();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return table;
            }

            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("textMessages", out var dictToken))
                {
                    if (dictToken.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dictToken.EnumerateArray())
                        {
                            try
                            {
                                if (item.TryGetProperty("Key", out var keyToken) && item.TryGetProperty("Value", out var valueToken))
                                {
                                    // Serialize Key (KeyValuePair<int, TextMessageKind>)
                                    var keyPair = JsonSerializer.Deserialize<KeyValuePair<int, TextMessageKind>>(keyToken.GetRawText(), options);
                                    
                                    // Serialize Value (List<string>)
                                    var valueList = JsonSerializer.Deserialize<List<string>>(valueToken.GetRawText(), options);

                                    if (valueList != null)
                                    {
                                        table.textMessages[keyPair] = valueList;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[STJ_TextMessageTableConverter] Entry parsing failed: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return table;
        }

        public override void Write(Utf8JsonWriter writer, TextMessageTable value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("textMessages");
            writer.WriteStartArray();

            foreach (var kvp in value.textMessages)
            {
                writer.WriteStartObject();
                
                writer.WritePropertyName("Key");
                JsonSerializer.Serialize(writer, kvp.Key, options);

                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, kvp.Value, options);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
