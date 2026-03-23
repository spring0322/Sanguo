using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// 自定义 ReferenceHandler：对简单值类型集合（如 List&lt;int&gt;）禁用引用保留
    /// 
    /// 问题：ReferenceHandler.Preserve 会将 List&lt;int&gt; 序列化为 {"$id":"5","$values":[21]}
    ///       反序列化时无法正确解析为 List&lt;int&gt;，导致集合变成空列表
    /// 
    /// 解决：继承 ReferenceHandler.Preserve，但对简单值类型集合返回 null（不使用引用保留）
    /// 
    /// 日期：2026-03-20
    /// </summary>
    public class SimpleCollectionReferenceHandler : ReferenceHandler
    {
        private readonly ReferenceHandler _preserveHandler = ReferenceHandler.Preserve;
        private ReferenceResolver? _resolver;

        public override ReferenceResolver CreateResolver()
        {
            // 🔥 C# 12：使用 ??= 空合并赋值
            return _resolver ??= new SimpleCollectionReferenceResolver(_preserveHandler.CreateResolver());
        }

        private class SimpleCollectionReferenceResolver(ReferenceResolver innerResolver) : ReferenceResolver
        {
            private readonly ReferenceResolver _innerResolver = innerResolver;

            public override void AddReference(string referenceId, object value)
            {
                // 🔥 关键：对简单值类型集合不添加引用
                if (IsSimpleCollection(value?.GetType()))
                {
                    return;
                }
                _innerResolver.AddReference(referenceId, value);
            }

            public override string GetReference(object value, out bool alreadyExists)
            {
                // 🔥 关键：对简单值类型集合不使用引用
                if (IsSimpleCollection(value?.GetType()))
                {
                    alreadyExists = false;
                    return null;
                }
                return _innerResolver.GetReference(value, out alreadyExists);
            }

            public override object ResolveReference(string referenceId)
            {
                return _innerResolver.ResolveReference(referenceId);
            }

            /// <summary>
            /// 判断类型是否是简单值类型集合（不需要引用保留）
            /// </summary>
            private static bool IsSimpleCollection(Type type)
            {
                if (type == null) return false;

                // List<int>, List<float>, List<string> 等
                if (type.IsGenericType)
                {
                    var genericTypeDef = type.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(List<>) || genericTypeDef == typeof(IList<>))
                    {
                        var elementType = type.GetGenericArguments()[0];
                        // 值类型（int, float, bool 等）或 string
                        return elementType.IsValueType || elementType == typeof(string);
                    }
                }

                // int[], float[], string[] 等
                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    return elementType.IsValueType || elementType == typeof(string);
                }

                return false;
            }
        }
    }
}
