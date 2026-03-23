using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json implementation of TypedListConverter.
    /// Ensures that lists are populated with the specific item type (TItem).
    /// </summary>
    public class TypedListConverter<TList, TItem> : JsonConverter<TList> 
        where TList : GameObjectList, new()
        where TItem : GameObject
    {
        public override TList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new TList();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return list;
            }

            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                
                // Case 1: Object containing "GameObjects"
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("GameObjects", out var gameObjectsToken))
                    {
                        ProcessGameObjectsToken(gameObjectsToken, list, options);
                    }
                    else
                    {
                        // Fallback: The object itself might be a TItem
                        TryAddSingleItem(root, list, options);
                    }
                }
                // Case 2: Array directly
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    ProcessGameObjectsToken(root, list, options);
                }
            }

            return list;
        }

        private void ProcessGameObjectsToken(JsonElement token, TList list, JsonSerializerOptions options)
        {
            if (token.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in token.EnumerateArray())
                {
                    TryAddSingleItem(item, list, options);
                }
            }
            else if (token.ValueKind == JsonValueKind.Object)
            {
                TryAddSingleItem(token, list, options);
            }
        }

        private void TryAddSingleItem(JsonElement element, TList list, JsonSerializerOptions options)
        {
            try
            {
                var item = JsonSerializer.Deserialize<TItem>(element.GetRawText(), options);
                if (item != null)
                {
                    list.GameObjects.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[STJ_TypedListConverter] Item conversion failed: {ex.Message}");
            }
        }

        public override void Write(Utf8JsonWriter writer, TList value, JsonSerializerOptions options)
        {
             // Verify if writing is needed or if default behavior suffices. 
             // Original converter threw NotImplementedException.
             throw new NotImplementedException("TypedListConverter does not support writing");
        }
    }
}
