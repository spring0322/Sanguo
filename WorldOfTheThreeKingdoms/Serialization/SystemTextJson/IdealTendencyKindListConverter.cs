using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using GameObjects.PersonDetail;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 🔥 2026-03-13 根本修复：IdealTendencyKindList 专用转换器
    /// 问题：AllIdealTendencyKinds 字段类型是 GameObjectList，但需要反序列化为 IdealTendencyKindList
    /// 解决：创建专用转换器，强制返回 IdealTendencyKindList 类型，支持元素类型推断
    /// 
    /// 🔥 C# 12 语法：使用主构造函数简化代码
    /// </summary>
    public class IdealTendencyKindListConverter() : JsonConverter<GameObjectList>
    {
        // 🔥 C# 12 语法：字段初始化使用集合表达式（虽然这里是单个对象）
        private readonly GameObjectListConverter _baseConverter = new();

        public override bool CanConvert(Type typeToConvert)
        {
            // 只处理 GameObjectList 类型（用于 AllIdealTendencyKinds 字段）
            return typeToConvert == typeof(GameObjectList);
        }

        public override GameObjectList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 🔥 关键：强制使用 IdealTendencyKindList 类型进行反序列化
            // 这样 ProcessGameObjectsElement 方法就能正确推断元素类型为 IdealTendencyKind
            return _baseConverter.Read(ref reader, typeof(IdealTendencyKindList), options);
        }

        public override void Write(Utf8JsonWriter writer, GameObjectList value, JsonSerializerOptions options)
        {
            _baseConverter.Write(writer, value, options);
        }
    }
}
