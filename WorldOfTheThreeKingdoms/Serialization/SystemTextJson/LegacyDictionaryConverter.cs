using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    public class LegacyDictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return [];
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadObjectFormat(ref reader, options);
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] Unexpected token {reader.TokenType}, returning empty dictionary.");
                return [];
            }

            return ReadArrayFormat(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var keyTypeInfo = JsonTypeInfoHelper.Resolve<TKey>(options);
            var valueTypeInfo = JsonTypeInfoHelper.Resolve<TValue>(options);
            bool writeAsObject = true;

            foreach (var kvp in value)
            {
                if (!TryFormatSimpleKey(kvp.Key, out _))
                {
                    writeAsObject = false;
                    break;
                }
            }

            if (writeAsObject)
            {
                writer.WriteStartObject();
                foreach (var kvp in value)
                {
                    TryFormatSimpleKey(kvp.Key, out var propertyName);
                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, kvp.Value, valueTypeInfo);
                }
                writer.WriteEndObject();
                return;
            }

            writer.WriteStartArray();
            foreach (var kvp in value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Key");
                JsonSerializer.Serialize(writer, kvp.Key, keyTypeInfo);
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, kvp.Value, valueTypeInfo);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private static Dictionary<TKey, TValue> ReadObjectFormat(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            try
            {
                using var document = JsonDocument.ParseValue(ref reader);
                var root = document.RootElement;
                Dictionary<TKey, TValue> dict = [];

                foreach (var property in root.EnumerateObject())
                {
                    try
                    {
                        var key = ConvertKey(property.Name);
                        var value = (TValue)JsonSerializer.Deserialize(
                            property.Value.GetRawText(),
                            JsonTypeInfoHelper.Resolve(options, typeof(TValue)));

                        if (key != null && value != null && !dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] Object property conversion failed: {property.Name} - {ex.Message}");
                    }
                }

                return dict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] Object format read failed: {ex.Message}");
                return [];
            }
        }

        private static Dictionary<TKey, TValue> ReadArrayFormat(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Dictionary<TKey, TValue> dict = [];

            using var document = JsonDocument.ParseValue(ref reader);
            var array = document.RootElement;
            int indexCounter = 0;

            foreach (var item in array.EnumerateArray())
            {
                try
                {
                    TKey key = default;
                    TValue value = default;
                    bool foundKey = false;

                    if (item.ValueKind == JsonValueKind.Array)
                    {
                        List<JsonElement> arrayItems = [];
                        foreach (var arrayItem in item.EnumerateArray())
                        {
                            arrayItems.Add(arrayItem);
                        }

                        if (arrayItems.Count == 2)
                        {
                            key = (TKey)JsonSerializer.Deserialize(
                                arrayItems[0].GetRawText(),
                                JsonTypeInfoHelper.Resolve(options, typeof(TKey)));
                            value = (TValue)JsonSerializer.Deserialize(
                                arrayItems[1].GetRawText(),
                                JsonTypeInfoHelper.Resolve(options, typeof(TValue)));
                            foundKey = true;
                        }
                    }

                    if (!foundKey && item.ValueKind == JsonValueKind.Object)
                    {
                        JsonElement keyElement = default;
                        JsonElement valueElement = default;
                        bool hasKey = item.TryGetProperty("Key", out keyElement) || item.TryGetProperty("key", out keyElement);
                        bool hasValue = item.TryGetProperty("Value", out valueElement) || item.TryGetProperty("value", out valueElement);

                        if (hasKey && hasValue)
                        {
                            key = (TKey)JsonSerializer.Deserialize(
                                keyElement.GetRawText(),
                                JsonTypeInfoHelper.Resolve(options, typeof(TKey)));
                            value = (TValue)JsonSerializer.Deserialize(
                                valueElement.GetRawText(),
                                JsonTypeInfoHelper.Resolve(options, typeof(TValue)));
                            foundKey = true;
                        }
                    }

                    if (!foundKey && item.ValueKind == JsonValueKind.Object)
                    {
                        value = (TValue)JsonSerializer.Deserialize(
                            item.GetRawText(),
                            JsonTypeInfoHelper.Resolve(options, typeof(TValue)));

                        JsonElement idElement = default;
                        bool hasId = item.TryGetProperty("ID", out idElement)
                                     || item.TryGetProperty("id", out idElement)
                                     || item.TryGetProperty("Id", out idElement)
                                     || item.TryGetProperty("KindID", out idElement)
                                     || item.TryGetProperty("KindId", out idElement);

                        if (hasId)
                        {
                            key = (TKey)JsonSerializer.Deserialize(
                                idElement.GetRawText(),
                                JsonTypeInfoHelper.Resolve(options, typeof(TKey)));
                            foundKey = true;
                        }
                        else if (typeof(TKey) == typeof(int))
                        {
                            key = (TKey)(object)indexCounter;
                            foundKey = true;
                        }
                    }

                    if (foundKey && key != null && !dict.ContainsKey(key))
                    {
                        dict.Add(key, value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LegacyDictionaryConverter] Array item conversion failed: {ex.Message}");
                }

                indexCounter++;
            }

            return dict;
        }

        private static TKey ConvertKey(string keyString)
        {
            try
            {
                if (typeof(TKey) == typeof(int))
                {
                    return (TKey)(object)int.Parse(keyString, CultureInfo.InvariantCulture);
                }

                if (typeof(TKey) == typeof(string))
                {
                    return (TKey)(object)keyString;
                }

                return (TKey)Convert.ChangeType(keyString, typeof(TKey), CultureInfo.InvariantCulture);
            }
            catch
            {
                return default;
            }
        }

        private static bool TryFormatSimpleKey(TKey key, out string propertyName)
        {
            propertyName = string.Empty;

            if (key == null)
            {
                return false;
            }

            if (key is string stringKey)
            {
                propertyName = stringKey;
                return true;
            }

            if (typeof(TKey).IsEnum || key is IFormattable)
            {
                propertyName = Convert.ToString(key, CultureInfo.InvariantCulture);
                return !string.IsNullOrEmpty(propertyName);
            }

            return false;
        }
    }
}
