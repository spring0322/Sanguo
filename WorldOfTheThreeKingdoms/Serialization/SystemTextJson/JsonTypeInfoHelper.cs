using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson;

internal static class JsonTypeInfoHelper
{
    public static JsonTypeInfo Resolve(JsonSerializerOptions options, Type type)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(type);

        return options.GetTypeInfo(type) ?? throw new InvalidOperationException(
            $"Missing JsonTypeInfo metadata for {type.FullName}.");
    }

    public static JsonTypeInfo<T> Resolve<T>(JsonSerializerOptions options)
    {
        var typeInfo = Resolve(options, typeof(T));
        return typeInfo as JsonTypeInfo<T> ?? throw new InvalidOperationException(
            $"Missing JsonTypeInfo metadata for {typeof(T).FullName}.");
    }
}
