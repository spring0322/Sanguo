using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的Faction.Leader转换器
    /// 处理Faction.Leader和LeaderID之间的关系
    /// </summary>
    public class FactionLeaderConverter : JsonConverter<Person>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Person);
        }

        public override Person Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                // 如果是数字，表示这是一个ID引用
                var personId = reader.GetInt32();
                return ResolvePersonById(personId);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                // 如果是对象，正常反序列化
                // 🔥 修复无限递归：创建不包含 FactionLeaderConverter 的新选项
                var optionsWithoutThisConverter = new JsonSerializerOptions(options);
                optionsWithoutThisConverter.Converters.Clear();
                
                // 添加除了 FactionLeaderConverter 之外的所有转换器
                foreach (var converter in options.Converters)
                {
                    if (!(converter is FactionLeaderConverter))
                    {
                        optionsWithoutThisConverter.Converters.Add(converter);
                    }
                }
                
                // 🔥 2026-03-11 修复：容错处理 Glamour 字段的浮点数值
                // 问题：旧存档可能将 glamourExperience (float) 错误序列化到 Glamour 字段
                // 解决：预处理 JSON，将浮点数转换为整数
                using var document = JsonDocument.ParseValue(ref reader);
                var jsonString = document.RootElement.GetRawText();
                var personTypeInfo = JsonTypeInfoHelper.Resolve<Person>(optionsWithoutThisConverter);
                
                try
                {
                    return JsonSerializer.Deserialize(jsonString, personTypeInfo);
                }
                catch (JsonException ex) when (ex.Message.Contains("Glamour"))
                {
                    // 🔥 2026-03-11 容错：修复旧存档中 Glamour 字段的浮点数问题
                    // 原因：旧版本可能将 glamourExperience (float) 错误序列化到 Glamour 字段
                    System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 检测到 Glamour 字段格式错误，尝试修复: {ex.Message}");
                    
                    // 使用 JsonDocument 重新解析并修复
                    using var doc = JsonDocument.Parse(jsonString);
                    using var stream = new System.IO.MemoryStream();
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        writer.WriteStartObject();
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            if (property.Name == "Glamour")
                            {
                                // 尝试将浮点数转换为整数
                                if (property.Value.ValueKind == JsonValueKind.Number)
                                {
                                    if (property.Value.TryGetDouble(out double doubleValue))
                                    {
                                        writer.WriteNumber("Glamour", (int)doubleValue);
                                        System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 修复 Glamour: {doubleValue} -> {(int)doubleValue}");
                                        continue;
                                    }
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String)
                                {
                                    if (double.TryParse(property.Value.GetString(), out double parsedValue))
                                    {
                                        writer.WriteNumber("Glamour", (int)parsedValue);
                                        System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 修复 Glamour 字符串: {parsedValue} -> {(int)parsedValue}");
                                        continue;
                                    }
                                }
                                // 🔥 ANTI-BAND-AID：使用默认值 0，但记录警告
                                // 理由：魅力值为 0 不会破坏游戏逻辑，允许玩家继续游戏
                                writer.WriteNumber("Glamour", 0);
                                System.Diagnostics.Debug.WriteLine("[FactionLeaderConverter] ⚠️ Glamour 无法解析，使用默认值 0");
                            }
                            else
                            {
                                property.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    
                    byte[] fixedJsonBytes = stream.ToArray();
                    string fixedJson = System.Text.Encoding.UTF8.GetString(fixedJsonBytes);
                    return JsonSerializer.Deserialize(fixedJson, personTypeInfo);
                }
            }

            throw new JsonException($"Unexpected token type for Person: {reader.TokenType}");
        }
        


        public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // 🔥 修复 StackOverflow：只写入 Person ID 避免无限递归
            // 完整的 Person 对象会在 Persons 列表中序列化
            // 这里只需要保存引用关系
            writer.WriteNumberValue(value.ID);
        }

        private Person ResolvePersonById(int personId)
        {
            // 🔥 2026-03-11 修复：ID >= 0 才是有效引用（ID=0 是阿会喃）
            if (personId < 0)
            {
                return null;
            }

            try
            {
                // 尝试从当前场景中查找Person
                if (Session.Current?.Scenario?.Persons != null)
                {
                    foreach (var person in Session.Current.Scenario.Persons.GetList())
                    {
                        if (person is Person p && p.ID == personId)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 成功解析Person ID {personId} -> {p.Name}");
                            return p;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 未找到Person ID {personId}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FactionLeaderConverter] 解析Person ID {personId} 时发生异常: {ex.Message}");
                return null;
            }
        }
    }
}
