using System.Collections.Generic;
using System.Text.Json.Serialization;
using GameObjects.Commands;

namespace WorldOfTheThreeKingdoms.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(FrameAuditRecord[]))]
[JsonSerializable(typeof(List<FrameAuditRecord>))]
public partial class FrameAuditJsonContext : JsonSerializerContext
{
}
