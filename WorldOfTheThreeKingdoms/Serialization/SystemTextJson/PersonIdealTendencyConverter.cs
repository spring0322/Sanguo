using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects.PersonDetail;
using GameManager;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的Person.IdealTendency转换器
    /// 处理Person.IdealTendency的序列化和反序列化
    /// </summary>
    public class PersonIdealTendencyConverter : JsonConverter<IdealTendencyKind>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IdealTendencyKind);
        }

        public override IdealTendencyKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                // 🔥 修复：反序列化时不解析引用，返回 null
                // 引用将在 AfterLoadGameScenario() 中统一恢复
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                // 🔥 修复：读取 ID 但不解析引用，返回 null
                // ID 已经存储在 Person.IdealTendencyIDString 中
                // 引用将在 AfterLoadGameScenario() 中统一恢复
                _ = reader.GetInt32(); // 消费掉这个数字
                return null;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                // 如果是对象，正常反序列化（用于 AllIdealTendencyKinds 列表）
                using var document = JsonDocument.ParseValue(ref reader);
                return ReadIdealTendencyKind(document.RootElement);
            }

            throw new JsonException($"Unexpected token type for IdealTendencyKind: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, IdealTendencyKind value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // 🔥 修复 StackOverflow：只写入 ID 避免无限递归
            // 完整的 IdealTendencyKind 对象会在 AllIdealTendencyKinds 列表中序列化
            writer.WriteNumberValue(value.ID);
        }

        private static IdealTendencyKind ReadIdealTendencyKind(JsonElement element)
        {
            IdealTendencyKind idealTendencyKind = new IdealTendencyKind();

            if (element.TryGetProperty("ID", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.Number)
            {
                idealTendencyKind.ID = idElement.GetInt32();
            }

            if (element.TryGetProperty("Name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                idealTendencyKind.Name = nameElement.GetString();
            }

            if (element.TryGetProperty("Offset", out JsonElement offsetElement) && offsetElement.ValueKind == JsonValueKind.Number)
            {
                idealTendencyKind.Offset = offsetElement.GetInt32();
            }

            return idealTendencyKind;
        }

        private IdealTendencyKind ResolveIdealTendencyById(int idealTendencyId)
        {
            if (idealTendencyId <= 0)
            {
                return GetDefaultIdealTendency();
            }

            try
            {
                // 尝试从当前场景的CommonData中查找IdealTendencyKind
                if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null)
                {
                    foreach (var item in Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.GetList())
                    {
                        if (item is IdealTendencyKind itk && itk.ID == idealTendencyId)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PersonIdealTendencyConverter] 成功解析IdealTendency ID {idealTendencyId} -> {itk.Name}");
                            return itk;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[PersonIdealTendencyConverter] 未找到IdealTendency ID {idealTendencyId}，使用默认值");
                return GetDefaultIdealTendency();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonIdealTendencyConverter] 解析IdealTendency ID {idealTendencyId} 时发生异常: {ex.Message}");
                return GetDefaultIdealTendency();
            }
        }

        private IdealTendencyKind GetDefaultIdealTendency()
        {
            try
            {
                // 尝试获取第一个可用的IdealTendencyKind作为默认值
                if (Session.Current?.Scenario?.GameCommonData?.AllIdealTendencyKinds != null &&
                    Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds.Count > 0)
                {
                    var firstItem = Session.Current.Scenario.GameCommonData.AllIdealTendencyKinds[0];
                    if (firstItem is IdealTendencyKind defaultItk)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PersonIdealTendencyConverter] 使用默认IdealTendency: {defaultItk.Name}");
                        return defaultItk;
                    }
                }

                // 如果没有可用的IdealTendencyKind，创建一个临时的默认值
                System.Diagnostics.Debug.WriteLine("[PersonIdealTendencyConverter] 创建临时默认IdealTendency");
                return new IdealTendencyKind
                {
                    ID = 0,
                    Name = "默认理想倾向"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonIdealTendencyConverter] 获取默认IdealTendency时发生异常: {ex.Message}");
                
                // 最后的备用方案
                return new IdealTendencyKind
                {
                    ID = 0,
                    Name = "默认理想倾向"
                };
            }
        }
    }
}
