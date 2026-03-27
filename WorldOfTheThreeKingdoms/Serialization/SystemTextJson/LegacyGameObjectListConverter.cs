using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using GameObjects.PersonDetail;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 处理包含 __type 元数据的 GameObjectList 的转换器
    /// 专门用于处理从 Newtonsoft.Json 迁移过来的数据格式
    /// </summary>
    public class LegacyGameObjectListConverter : JsonConverter<GameObjectList>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(GameObjectList);
        }

        public override GameObjectList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject token for GameObjectList, got {reader.TokenType}");
            }

            var gameObjectList = new GameObjectList();
            
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
                    case "GameObjects":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            ReadGameObjectsArray(ref reader, gameObjectList, options);
                        }
                        break;
                    case "IsNumber":
                        if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                        {
                            gameObjectList.IsNumber = reader.GetBoolean();
                        }
                        break;
                    case "PropertyName":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            gameObjectList.PropertyName = reader.GetString();
                        }
                        else if (reader.TokenType == JsonTokenType.Null)
                        {
                            gameObjectList.PropertyName = null;
                        }
                        break;
                    case "SmallToBig":
                        if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                        {
                            gameObjectList.SmallToBig = reader.GetBoolean();
                        }
                        break;
                    default:
                        // 跳过未知属性
                        reader.Skip();
                        break;
                }
            }

            return gameObjectList;
        }

        private void ReadGameObjectsArray(ref Utf8JsonReader reader, GameObjectList gameObjectList, JsonSerializerOptions options)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var gameObject = ReadGameObjectWithType(ref reader, options);
                    if (gameObject != null)
                    {
                        gameObjectList.Add(gameObject);
                    }
                }
            }
        }

        private GameObject ReadGameObjectWithType(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            string typeName = null;
            var properties = new Dictionary<string, JsonElement>();

            // 首先读取所有属性，包括 __type
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "__type")
                {
                    typeName = property.Value.GetString();
                }
                else
                {
                    properties[property.Name] = property.Value.Clone();
                }
            }

            // 根据 __type 创建相应的对象
            GameObject gameObject = CreateGameObjectByType(typeName);
            if (gameObject == null)
            {
                System.Diagnostics.Debug.WriteLine($"[LegacyGameObjectListConverter] 无法创建类型: {typeName}");
                return null;
            }

            // 设置属性
            SetGameObjectProperties(gameObject, properties, options);

            return gameObject;
        }

        private GameObject CreateGameObjectByType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            // 解析类型名称，格式通常是 "TypeName:#Namespace"
            var parts = typeName.Split('#');
            if (parts.Length != 2)
            {
                return null;
            }

            var className = parts[0];
            var namespaceName = parts[1];

            // 根据类型名称创建对象
            switch (className)
            {
                case "IdealTendencyKind":
                    if (namespaceName == "GameObjects.PersonDetail")
                    {
                        return new IdealTendencyKind();
                    }
                    break;
                // 可以在这里添加其他类型的支持
                default:
                    System.Diagnostics.Debug.WriteLine($"[LegacyGameObjectListConverter] 不支持的类型: {className} in {namespaceName}");
                    break;
            }

            return null;
        }

        private void SetGameObjectProperties(GameObject gameObject, Dictionary<string, JsonElement> properties, JsonSerializerOptions options)
        {
            foreach (var property in properties)
            {
                try
                {
                    SetProperty(gameObject, property.Key, property.Value, options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LegacyGameObjectListConverter] 设置属性 {property.Key} 失败: {ex.Message}");
                }
            }
        }

        private void SetProperty(GameObject gameObject, string propertyName, JsonElement value, JsonSerializerOptions options)
        {
            switch (propertyName)
            {
                case "ID":
                    if (value.ValueKind == JsonValueKind.Number)
                    {
                        gameObject.ID = value.GetInt32();
                    }
                    break;
                case "Name":
                    if (value.ValueKind == JsonValueKind.String)
                    {
                        gameObject.Name = value.GetString();
                    }
                    break;
                case "Offset":
                    if (gameObject is IdealTendencyKind itk && value.ValueKind == JsonValueKind.Number)
                    {
                        itk.Offset = value.GetInt32();
                    }
                    break;
                // 可以在这里添加其他属性的支持
                default:
                    System.Diagnostics.Debug.WriteLine($"[LegacyGameObjectListConverter] 未处理的属性: {propertyName}");
                    break;
            }
        }

        public override void Write(Utf8JsonWriter writer, GameObjectList value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            // 写入 GameObjects 数组
            writer.WriteStartArray("GameObjects");
            foreach (var item in value.GetList())
            {
                if (item != null)
                {
                    JsonSerializer.Serialize(writer, item, JsonTypeInfoHelper.Resolve(options, item.GetType()));
                }
            }
            writer.WriteEndArray();
            
            // 写入其他属性
            writer.WriteBoolean("IsNumber", value.IsNumber);
            
            if (value.PropertyName != null)
            {
                writer.WriteString("PropertyName", value.PropertyName);
            }
            else
            {
                writer.WriteNull("PropertyName");
            }
            
            writer.WriteBoolean("SmallToBig", value.SmallToBig);
            
            writer.WriteEndObject();
        }
    }
}
