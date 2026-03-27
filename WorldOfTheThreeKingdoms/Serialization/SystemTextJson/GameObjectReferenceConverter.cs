using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的游戏对象引用转换器
    /// 处理多态GameObject序列化和反序列化
    /// </summary>
    public class GameObjectReferenceConverter : JsonConverter<GameObject>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(GameObject).IsAssignableFrom(typeToConvert);
        }

        public override GameObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            var innerOptions = CreateInnerOptions(options);

            if (root.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Expected JSON object for GameObject");
            }

            // 尝试从$type属性获取类型信息
            if (root.TryGetProperty("$type", out var typeProperty))
            {
                var typeName = typeProperty.GetString();
                var actualType = GetGameObjectType(typeName);
                if (actualType != null)
                {
                    return JsonSerializer.Deserialize(root.GetRawText(), JsonTypeInfoHelper.Resolve(innerOptions, actualType)) as GameObject;
                }
            }

            // 如果没有$type属性，根据属性推断类型
            var inferredType = InferGameObjectType(root);
            if (inferredType != null)
            {
                return JsonSerializer.Deserialize(root.GetRawText(), JsonTypeInfoHelper.Resolve(innerOptions, inferredType)) as GameObject;
            }

            // 默认尝试反序列化为基类
            return JsonSerializer.Deserialize(root.GetRawText(), JsonTypeInfoHelper.Resolve<GameObject>(innerOptions));
        }

        public override void Write(Utf8JsonWriter writer, GameObject value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // 🔥 创建不包含此转换器的选项副本，避免无限递归
            var innerOptions = CreateInnerOptions(options);

            // 获取实际类型并序列化
            var actualType = value.GetType();
            
            // 直接序列化对象，使用内部选项
            JsonSerializer.Serialize(writer, value, JsonTypeInfoHelper.Resolve(innerOptions, actualType));
        }

        private static JsonSerializerOptions CreateInnerOptions(JsonSerializerOptions options)
        {
            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Clear();
            foreach (var converter in options.Converters)
            {
                if (converter.GetType() != typeof(GameObjectReferenceConverter))
                {
                    innerOptions.Converters.Add(converter);
                }
            }

            return innerOptions;
        }

        private Type GetGameObjectType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // 移除命名空间前缀，只保留类名
            var className = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;

            return className switch
            {
                "Person" => typeof(Person),
                "Architecture" => typeof(Architecture),
                "Faction" => typeof(Faction),
                "Troop" => typeof(Troop),
                "Legion" => typeof(Legion),
                "Military" => typeof(Military),
                "Treasure" => typeof(Treasure),
                "Information" => typeof(Information),
                "Routeway" => typeof(Routeway),
                "Facility" => typeof(Facility),
                "Captive" => typeof(Captive),
                "TroopEvent" => typeof(TroopEvent),
                "Event" => typeof(Event),
                _ => null
            };
        }

        private Type InferGameObjectType(JsonElement element)
        {
            // 根据特征属性推断GameObject类型
            
            // Person特征：有Ideal、Strain等属性
            if (element.TryGetProperty("Ideal", out _) || 
                element.TryGetProperty("Strain", out _) ||
                element.TryGetProperty("BelongedFactionID", out _))
            {
                return typeof(Person);
            }

            // Architecture特征：有Kind、Population等属性
            if (element.TryGetProperty("Kind", out _) || 
                element.TryGetProperty("Population", out _) ||
                element.TryGetProperty("Agriculture", out _))
            {
                return typeof(Architecture);
            }

            // Faction特征：有LeaderID、ColorIndex等属性
            if (element.TryGetProperty("LeaderID", out _) || 
                element.TryGetProperty("ColorIndex", out _) ||
                element.TryGetProperty("Architectures", out _))
            {
                return typeof(Faction);
            }

            // Troop特征：有Army、Quantity等属性
            if (element.TryGetProperty("Army", out _) || 
                element.TryGetProperty("Quantity", out _) ||
                element.TryGetProperty("Morale", out _))
            {
                return typeof(Troop);
            }

            // Legion特征：有Troops、StartArchitecture等属性
            if (element.TryGetProperty("Troops", out _) || 
                element.TryGetProperty("StartArchitectureString", out _) ||
                element.TryGetProperty("WillArchitectureString", out _))
            {
                return typeof(Legion);
            }

            // 默认返回null，让调用者处理
            return null;
        }
    }
}
