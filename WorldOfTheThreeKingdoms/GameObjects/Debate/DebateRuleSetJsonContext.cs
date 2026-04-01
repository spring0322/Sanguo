using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

[JsonSerializable(typeof(DebateRuleSet))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
internal partial class DebateRuleSetJsonContext : JsonSerializerContext
{
}

